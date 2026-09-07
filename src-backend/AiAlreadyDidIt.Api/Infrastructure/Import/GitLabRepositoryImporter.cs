using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Infrastructure.Import;

/// <summary>GitLab REST v4 (gitlab.com and self-hosted instances are both addressed by host).</summary>
public sealed class GitLabRepositoryImporter(IHttpClientFactory httpClientFactory, IOptions<RepositoryImportOptions> options) : IRepositoryImporter
{
    public const string HttpClientName = "gitlab";

    public bool CanHandle(Uri url) => url.Host.Equals("gitlab.com", StringComparison.OrdinalIgnoreCase) || url.Host.Contains("gitlab", StringComparison.OrdinalIgnoreCase);

    private HttpClient Client(Uri url)
    {
        var c = httpClientFactory.CreateClient(HttpClientName);
        c.BaseAddress = new Uri($"{url.Scheme}://{url.Host}/api/v4/");
        c.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
        if (!string.IsNullOrWhiteSpace(options.Value.GitLabToken))
            c.DefaultRequestHeaders.Add("PRIVATE-TOKEN", options.Value.GitLabToken);
        return c;
    }

    public async Task<RepositoryInspection> InspectAsync(Uri url, CancellationToken ct = default)
    {
        var path = url.AbsolutePath.Trim('/');
        var dash = path.IndexOf("/-/", StringComparison.Ordinal);
        if (dash > 0) path = path[..dash];
        if (path.Split('/').Length < 2) throw new RepositoryImportException("A GitLab repository URL looks like https://gitlab.com/group/project.");
        var encoded = Uri.EscapeDataString(path);
        using var client = Client(url);

        var projectResponse = await client.GetAsync($"projects/{encoded}?license=true", ct);
        if (projectResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new RepositoryImportException("Project not found. Only public projects can be imported.");
        projectResponse.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await projectResponse.Content.ReadAsStringAsync(ct));
        var root = doc.RootElement;
        var segments = path.Split('/');
        var result = new RepositoryInspection
        {
            Provider = "GitLab",
            Owner = string.Join('/', segments[..^1]),
            Name = root.GetProperty("path").GetString() ?? segments[^1],
            Url = root.GetProperty("web_url").GetString() ?? url.ToString(),
            Description = root.TryGetProperty("description", out var d) ? d.GetString() : null,
            DefaultBranch = root.TryGetProperty("default_branch", out var db) ? db.GetString() ?? "main" : "main",
            Stars = root.TryGetProperty("star_count", out var s) ? s.GetInt32() : 0,
            IsArchived = root.TryGetProperty("archived", out var a) && a.GetBoolean(),
            IsPrivate = root.TryGetProperty("visibility", out var v) && v.GetString() != "public",
            PushedAt = root.TryGetProperty("last_activity_at", out var la) && la.ValueKind == JsonValueKind.String ? la.GetDateTime().ToUniversalTime() : null
        };
        if (root.TryGetProperty("topics", out var topics) && topics.ValueKind == JsonValueKind.Array)
            result.Topics = topics.EnumerateArray().Select(t => t.GetString() ?? string.Empty).Where(t => t.Length > 0).ToList();
        if (root.TryGetProperty("license", out var lic) && lic.ValueKind == JsonValueKind.Object && lic.TryGetProperty("key", out var key))
            result.LicenseSpdxId = MapLicenseKey(key.GetString());
        var projectId = root.GetProperty("id").GetInt64();
        result.TarballUrl = $"{client.BaseAddress}projects/{projectId}/repository/archive.tar.gz?sha={Uri.EscapeDataString(result.DefaultBranch)}";

        try
        {
            var langs = await client.GetFromJsonAsync<Dictionary<string, double>>($"projects/{projectId}/languages", ct);
            if (langs is { Count: > 0 })
            {
                result.Languages = langs.ToDictionary(k => k.Key, k => (long)(k.Value * 100));
                result.PrimaryLanguage = langs.MaxBy(k => k.Value).Key;
            }
        }
        catch { }

        result.ReadmeMarkdown = await ReadFile(client, projectId, result.DefaultBranch, ["README.md", "README.MD", "readme.md", "README", "README.rst", "README.txt"], ct);
        result.LicenseText = await ReadFile(client, projectId, result.DefaultBranch, ["LICENSE", "LICENSE.md", "LICENSE.txt", "COPYING", "LICENCE"], ct);

        try
        {
            using var releases = JsonDocument.Parse(await client.GetStringAsync($"projects/{projectId}/releases?per_page=20", ct));
            foreach (var r in releases.RootElement.EnumerateArray())
            {
                var assets = new List<ReleaseAsset>();
                if (r.TryGetProperty("assets", out var assetsEl) && assetsEl.TryGetProperty("links", out var links))
                    foreach (var link in links.EnumerateArray())
                        assets.Add(new ReleaseAsset(link.GetProperty("name").GetString() ?? "asset", link.GetProperty("url").GetString() ?? string.Empty, 0, null));
                result.Releases.Add(new RepositoryRelease(
                    r.GetProperty("tag_name").GetString() ?? string.Empty,
                    r.TryGetProperty("name", out var rn) ? rn.GetString() : null,
                    r.TryGetProperty("description", out var rb) ? rb.GetString() : null,
                    r.TryGetProperty("released_at", out var rp) && rp.ValueKind == JsonValueKind.String ? rp.GetDateTime().ToUniversalTime() : null,
                    assets, null, null));
            }
        }
        catch { }

        try
        {
            using var tags = JsonDocument.Parse(await client.GetStringAsync($"projects/{projectId}/repository/tags?per_page=30", ct));
            result.Tags = tags.RootElement.EnumerateArray().Select(t => t.GetProperty("name").GetString() ?? string.Empty).Where(t => t.Length > 0).ToList();
        }
        catch { }

        return result;
    }

    public string TarballUrlFor(RepositoryInspection repo, string gitRef)
    {
        var idx = repo.TarballUrl.IndexOf("?sha=", StringComparison.Ordinal);
        var baseUrl = idx > 0 ? repo.TarballUrl[..idx] : repo.TarballUrl;
        return $"{baseUrl}?sha={Uri.EscapeDataString(gitRef)}";
    }

    private static async Task<string?> ReadFile(HttpClient client, long projectId, string branch, string[] candidates, CancellationToken ct)
    {
        foreach (var file in candidates)
        {
            try
            {
                var response = await client.GetAsync($"projects/{projectId}/repository/files/{Uri.EscapeDataString(file)}/raw?ref={Uri.EscapeDataString(branch)}", ct);
                if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync(ct);
            }
            catch { }
        }
        return null;
    }

    private static string? MapLicenseKey(string? key) => key?.ToLowerInvariant() switch
    {
        null => null,
        "mit" => "MIT",
        "apache-2.0" => "Apache-2.0",
        "gpl-3.0" => "GPL-3.0-only",
        "gpl-2.0" => "GPL-2.0-only",
        "lgpl-3.0" => "LGPL-3.0-only",
        "lgpl-2.1" => "LGPL-2.1-only",
        "agpl-3.0" => "AGPL-3.0-only",
        "bsd-2-clause" => "BSD-2-Clause",
        "bsd-3-clause" => "BSD-3-Clause",
        "mpl-2.0" => "MPL-2.0",
        "unlicense" => "Unlicense",
        "isc" => "ISC",
        "cc0-1.0" => "CC0-1.0",
        "bsl-1.0" => "BSL-1.0",
        "epl-2.0" => "EPL-2.0",
        "zlib" => "Zlib",
        _ => key
    };
}

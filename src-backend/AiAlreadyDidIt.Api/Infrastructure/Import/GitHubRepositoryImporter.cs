using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Infrastructure.Import;

/// <summary>GitHub REST v3 — repo, languages, readme, license, releases (with assets), tags.</summary>
public sealed class GitHubRepositoryImporter(IHttpClientFactory httpClientFactory, IOptions<RepositoryImportOptions> options) : IRepositoryImporter
{
    public const string HttpClientName = "github";

    public bool CanHandle(Uri url) => url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || url.Host.Equals("www.github.com", StringComparison.OrdinalIgnoreCase);

    private HttpClient Client()
    {
        var c = httpClientFactory.CreateClient(HttpClientName);
        c.BaseAddress = new Uri("https://api.github.com/");
        c.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        if (!string.IsNullOrWhiteSpace(options.Value.GitHubToken))
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.GitHubToken);
        return c;
    }

    public async Task<RepositoryInspection> InspectAsync(Uri url, CancellationToken ct = default)
    {
        var parts = url.AbsolutePath.Trim('/').Split('/');
        if (parts.Length < 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            throw new RepositoryImportException("A GitHub repository URL looks like https://github.com/owner/repository.");
        var owner = parts[0];
        var name = parts[1];
        using var client = Client();

        var repoResponse = await client.GetAsync($"repos/{owner}/{name}", ct);
        if (repoResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            throw new RepositoryImportException("Repository not found. Only public repositories can be imported.");
        if (repoResponse.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new RepositoryImportException("GitHub API rate limit reached. Configure RepositoryImport:GitHubToken or try again later.");
        repoResponse.EnsureSuccessStatusCode();
        using var repo = JsonDocument.Parse(await repoResponse.Content.ReadAsStringAsync(ct));
        var root = repo.RootElement;

        var result = new RepositoryInspection
        {
            Provider = "GitHub",
            Owner = root.GetProperty("owner").GetProperty("login").GetString() ?? owner,
            Name = root.GetProperty("name").GetString() ?? name,
            Url = root.GetProperty("html_url").GetString() ?? url.ToString(),
            Description = root.TryGetProperty("description", out var d) ? d.GetString() : null,
            Homepage = root.TryGetProperty("homepage", out var h) ? h.GetString() : null,
            DefaultBranch = root.GetProperty("default_branch").GetString() ?? "main",
            Stars = root.TryGetProperty("stargazers_count", out var s) ? s.GetInt32() : 0,
            PrimaryLanguage = root.TryGetProperty("language", out var l) ? l.GetString() : null,
            IsArchived = root.TryGetProperty("archived", out var a) && a.GetBoolean(),
            IsPrivate = root.TryGetProperty("private", out var p) && p.GetBoolean(),
            PushedAt = root.TryGetProperty("pushed_at", out var pa) && pa.ValueKind == JsonValueKind.String ? pa.GetDateTime().ToUniversalTime() : null
        };
        if (root.TryGetProperty("license", out var lic) && lic.ValueKind == JsonValueKind.Object && lic.TryGetProperty("spdx_id", out var spdx))
        {
            var id = spdx.GetString();
            result.LicenseSpdxId = id is null or "NOASSERTION" ? null : id;
        }
        if (root.TryGetProperty("topics", out var topics) && topics.ValueKind == JsonValueKind.Array)
            result.Topics = topics.EnumerateArray().Select(t => t.GetString() ?? string.Empty).Where(t => t.Length > 0).ToList();
        result.TarballUrl = $"https://api.github.com/repos/{result.Owner}/{result.Name}/tarball/{result.DefaultBranch}";

        // languages
        try
        {
            var langs = await client.GetFromJsonAsync<Dictionary<string, long>>($"repos/{result.Owner}/{result.Name}/languages", ct);
            if (langs is not null) result.Languages = langs;
        }
        catch { /* optional */ }

        // readme
        try
        {
            using var readmeRequest = new HttpRequestMessage(HttpMethod.Get, $"repos/{result.Owner}/{result.Name}/readme");
            readmeRequest.Headers.Accept.Clear();
            readmeRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));
            var readmeResponse = await client.SendAsync(readmeRequest, ct);
            if (readmeResponse.IsSuccessStatusCode) result.ReadmeMarkdown = await readmeResponse.Content.ReadAsStringAsync(ct);
        }
        catch { /* optional */ }

        // license text
        try
        {
            using var licenseRequest = new HttpRequestMessage(HttpMethod.Get, $"repos/{result.Owner}/{result.Name}/license");
            licenseRequest.Headers.Accept.Clear();
            licenseRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));
            var licenseResponse = await client.SendAsync(licenseRequest, ct);
            if (licenseResponse.IsSuccessStatusCode) result.LicenseText = await licenseResponse.Content.ReadAsStringAsync(ct);
        }
        catch { /* optional */ }

        // releases
        try
        {
            using var releases = JsonDocument.Parse(await client.GetStringAsync($"repos/{result.Owner}/{result.Name}/releases?per_page=20", ct));
            foreach (var r in releases.RootElement.EnumerateArray())
            {
                if (r.TryGetProperty("draft", out var draft) && draft.GetBoolean()) continue;
                var assets = new List<ReleaseAsset>();
                if (r.TryGetProperty("assets", out var assetsEl))
                    foreach (var asset in assetsEl.EnumerateArray())
                        assets.Add(new ReleaseAsset(
                            asset.GetProperty("name").GetString() ?? "asset",
                            asset.GetProperty("browser_download_url").GetString() ?? string.Empty,
                            asset.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
                            asset.TryGetProperty("content_type", out var ctype) ? ctype.GetString() : null));
                result.Releases.Add(new RepositoryRelease(
                    r.GetProperty("tag_name").GetString() ?? string.Empty,
                    r.TryGetProperty("name", out var rn) ? rn.GetString() : null,
                    r.TryGetProperty("body", out var rb) ? rb.GetString() : null,
                    r.TryGetProperty("published_at", out var rp) && rp.ValueKind == JsonValueKind.String ? rp.GetDateTime().ToUniversalTime() : null,
                    assets,
                    r.TryGetProperty("zipball_url", out var z) ? z.GetString() : null,
                    r.TryGetProperty("tarball_url", out var tb) ? tb.GetString() : null));
            }
        }
        catch { /* optional */ }

        // tags
        try
        {
            using var tags = JsonDocument.Parse(await client.GetStringAsync($"repos/{result.Owner}/{result.Name}/tags?per_page=30", ct));
            result.Tags = tags.RootElement.EnumerateArray().Select(t => t.GetProperty("name").GetString() ?? string.Empty).Where(t => t.Length > 0).ToList();
        }
        catch { /* optional */ }

        return result;
    }

    public string TarballUrlFor(RepositoryInspection repo, string gitRef) =>
        $"https://api.github.com/repos/{repo.Owner}/{repo.Name}/tarball/{Uri.EscapeDataString(gitRef)}";
}

public sealed class RepositoryImportException(string message) : Exception(message);

namespace AiAlreadyDidIt.Api.Infrastructure.Import;

public sealed record ReleaseAsset(string Name, string Url, long Size, string? ContentType);

public sealed record RepositoryRelease(string Tag, string? Name, string? Body, DateTime? PublishedAt, IReadOnlyList<ReleaseAsset> Assets, string? ZipballUrl, string? TarballUrl);

/// <summary>Everything the importer could learn about a public repository.</summary>
public sealed class RepositoryInspection
{
    public string Provider { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Homepage { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public int Stars { get; set; }
    public string? PrimaryLanguage { get; set; }
    public Dictionary<string, long> Languages { get; set; } = [];
    public string? LicenseSpdxId { get; set; }
    public string? LicenseText { get; set; }
    public string? ReadmeMarkdown { get; set; }
    public List<string> Topics { get; set; } = [];
    public List<RepositoryRelease> Releases { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public DateTime? PushedAt { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPrivate { get; set; }
    /// <summary>URL of the tar.gz snapshot of <paramref name="DefaultBranch"/> (or a tag) — used for the stored source snapshot.</summary>
    public string TarballUrl { get; set; } = string.Empty;
}

public interface IRepositoryImporter
{
    bool CanHandle(Uri url);
    Task<RepositoryInspection> InspectAsync(Uri url, CancellationToken ct = default);
    /// <summary>Tarball URL for a specific ref (tag / branch / commit).</summary>
    string TarballUrlFor(RepositoryInspection repo, string gitRef);
}

public static class RepositoryUrl
{
    /// <summary>Normalises "github.com/owner/repo(.git)" style input to an absolute https URL.</summary>
    public static Uri? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = input.Trim();
        if (s.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase)) s = "https://github.com/" + s["git@github.com:".Length..];
        if (s.StartsWith("git@gitlab.com:", StringComparison.OrdinalIgnoreCase)) s = "https://gitlab.com/" + s["git@gitlab.com:".Length..];
        if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) s = "https://" + s;
        if (s.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) s = s[..^4];
        s = s.TrimEnd('/');
        return Uri.TryCreate(s, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" ? uri : null;
    }
}

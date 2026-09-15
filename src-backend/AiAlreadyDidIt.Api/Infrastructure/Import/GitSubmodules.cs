using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace AiAlreadyDidIt.Api.Infrastructure.Import;

/// <summary>
/// Umbrella repositories keep their code in git submodules, and a GitHub / GitLab tarball ships those as empty folders —
/// the snapshot then has a README, a LICENSE and no code at all. This helper parses <c>.gitmodules</c>, builds the tarball
/// URL of each public GitHub / GitLab submodule and merges their contents under the right folder of the parent snapshot.
/// </summary>
public static partial class GitSubmodules
{
    public sealed record Submodule(string Path, string Url);

    /// <summary>Parses the INI-like <c>.gitmodules</c> file: <c>[submodule "x"]</c> blocks with <c>path</c> and <c>url</c>.</summary>
    public static List<Submodule> Parse(string? gitmodulesText)
    {
        var result = new List<Submodule>();
        if (string.IsNullOrWhiteSpace(gitmodulesText)) return result;
        string? path = null, url = null;
        void Flush() { if (path is not null && url is not null) result.Add(new Submodule(path.Trim().Trim('/'), url.Trim())); path = url = null; }
        foreach (var raw in gitmodulesText.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith('[')) { Flush(); continue; }
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim().ToLowerInvariant();
            var value = line[(eq + 1)..].Trim();
            if (key == "path") path = value;
            else if (key == "url") url = value;
        }
        Flush();
        return result.Where(s => s.Path.Length > 0 && !s.Path.Contains("..")).ToList();
    }

    /// <summary>
    /// Turns the many ways a submodule URL is written (https, ssh, git://, relative <c>../repo.git</c>) into a canonical
    /// <c>https://host/owner/repo</c>. Relative URLs resolve against the parent repository's owner. Unknown hosts return null.
    /// </summary>
    public static Uri? ResolveUrl(string rawUrl, string parentHost, string parentOwner)
    {
        var url = rawUrl.Trim();
        if (url.Length == 0) return null;
        string host, ownerRepo;
        if (url.StartsWith("../") || url.StartsWith("./"))
        {
            // relative to the parent repository's owner: "../other.git" → https://host/parentOwner/other
            host = parentHost;
            ownerRepo = parentOwner + "/" + url.Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        }
        else if (Uri.TryCreate(url, UriKind.Absolute, out var abs) && abs.Scheme is "https" or "http" or "git" or "ssh" && abs.Host.Length > 0)
        {
            host = abs.Host; ownerRepo = abs.AbsolutePath.Trim('/');
        }
        else if (ScpLikeUrl().Match(url) is { Success: true } scp)
        {
            // git@github.com:owner/repo.git
            host = scp.Groups["host"].Value; ownerRepo = scp.Groups["path"].Value;
        }
        else return null;
        if (ownerRepo.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) ownerRepo = ownerRepo[..^4];
        var parts = ownerRepo.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        host = host.ToLowerInvariant();
        if (host is not ("github.com" or "www.github.com" or "gitlab.com" or "www.gitlab.com")) return null;
        return new Uri($"https://{host.Replace("www.", "")}/{parts[0]}/{parts[1]}");
    }

    /// <summary>Anonymous tar.gz download for a commit / branch of a public GitHub or GitLab repository.</summary>
    public static string? TarballUrl(Uri repo, string gitRef)
    {
        var parts = repo.AbsolutePath.Trim('/').Split('/');
        if (parts.Length < 2) return null;
        var (owner, name) = (parts[0], parts[1]);
        var r = Uri.EscapeDataString(gitRef);
        return repo.Host switch
        {
            "github.com" => $"https://codeload.github.com/{owner}/{name}/tar.gz/{r}",
            "gitlab.com" => $"https://gitlab.com/{owner}/{name}/-/archive/{r}/{name}-{r}.tar.gz",
            _ => null
        };
    }

    /// <summary>Reads a file that sits at the root of the archive (directly under its single top folder, or at the top level).</summary>
    public static async Task<string?> ReadRootFileAsync(string tarGzPath, string fileName, CancellationToken ct)
    {
        await using var gz = new GZipStream(File.OpenRead(tarGzPath), CompressionMode.Decompress);
        await using var reader = new TarReader(gz);
        while (await reader.GetNextEntryAsync(cancellationToken: ct) is { } e)
        {
            if (e.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile) || e.DataStream is null) continue;
            var segments = e.Name.Replace('\\', '/').TrimStart('.', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length is 1 or 2 && segments[^1].Equals(fileName, StringComparison.OrdinalIgnoreCase) && e.Length < 256 * 1024)
            {
                using var ms = new MemoryStream();
                await e.DataStream.CopyToAsync(ms, ct);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
        return null;
    }

    /// <summary>The single top-level folder of a GitHub-style tarball ("repo-main/"), or "" when entries sit at the top.</summary>
    public static async Task<string> RootFolderAsync(string tarGzPath, CancellationToken ct)
    {
        var names = new List<string>();
        await using var gz = new GZipStream(File.OpenRead(tarGzPath), CompressionMode.Decompress);
        await using var reader = new TarReader(gz);
        while (await reader.GetNextEntryAsync(cancellationToken: ct) is { } e)
            if (e.EntryType is not TarEntryType.GlobalExtendedAttributes) names.Add(e.Name);
        return CommonRoot(names);
    }

    public static string CommonRoot(IEnumerable<string> names)
    {
        string? root = null;
        foreach (var raw in names)
        {
            var n = raw.Replace('\\', '/').TrimStart('.', '/');
            if (n.Length == 0) continue;
            var slash = n.IndexOf('/');
            if (slash <= 0) return "";
            var top = n[..(slash + 1)];
            if (root is null) root = top; else if (root != top) return "";
        }
        return root ?? "";
    }

    /// <summary>"sub-abc123/src/main.go" inside submodule "services/sub" of root "repo-main/" → "repo-main/services/sub/src/main.go".</summary>
    public static string RerootedName(string entryName, string rootFolder, string submodulePath)
    {
        var n = entryName.Replace('\\', '/').TrimStart('.', '/');
        var slash = n.IndexOf('/');
        var rest = slash < 0 ? "" : n[(slash + 1)..];
        var prefix = rootFolder + submodulePath.Trim('/') + "/";
        return rest.Length == 0 ? prefix : prefix + rest;
    }

    /// <summary>Writes a new tar.gz: every entry of the parent tarball, then every submodule tarball re-rooted under its path.</summary>
    public static async Task MergeAsync(string rootTarGz, IReadOnlyList<(string Path, string TarGz)> submodules, string outputTarGz, CancellationToken ct)
    {
        var rootFolder = await RootFolderAsync(rootTarGz, ct);
        await using var outGz = new GZipStream(File.Create(outputTarGz), CompressionLevel.Fastest);
        await using var writer = new TarWriter(outGz, TarEntryFormat.Pax);
        await using (var gz = new GZipStream(File.OpenRead(rootTarGz), CompressionMode.Decompress))
        await using (var reader = new TarReader(gz))
            while (await reader.GetNextEntryAsync(copyData: true, cancellationToken: ct) is { } e)
                if (e.EntryType is not TarEntryType.GlobalExtendedAttributes) await writer.WriteEntryAsync(e, ct);
        foreach (var (path, tarGz) in submodules)
        {
            await using var gz = new GZipStream(File.OpenRead(tarGz), CompressionMode.Decompress);
            await using var reader = new TarReader(gz);
            while (await reader.GetNextEntryAsync(copyData: true, cancellationToken: ct) is { } e)
            {
                var name = RerootedName(e.Name, rootFolder, path);
                if (e.EntryType is TarEntryType.Directory)
                    await writer.WriteEntryAsync(new PaxTarEntry(TarEntryType.Directory, name) { ModificationTime = e.ModificationTime }, ct);
                else if (e.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
                    await writer.WriteEntryAsync(new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = e.DataStream, Mode = e.Mode, ModificationTime = e.ModificationTime }, ct);
                // symlinks, hard links, global headers and other special entries are dropped on purpose
            }
        }
    }

    [GeneratedRegex(@"^(?:[\w.-]+@)?(?<host>[\w.-]+):(?<path>[^/\s][^\s]*)$")]
    private static partial Regex ScpLikeUrl();
}

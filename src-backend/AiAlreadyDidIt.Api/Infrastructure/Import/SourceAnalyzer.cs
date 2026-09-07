using System.Formats.Tar;
using System.IO.Compression;
using System.Text;

namespace AiAlreadyDidIt.Api.Infrastructure.Import;

/// <summary>What the archive inspection found: size, LOC, license file, README.</summary>
public sealed class SourceAnalysis
{
    public int FileCount { get; set; }
    public int SourceFileCount { get; set; }
    public int LineCount { get; set; }
    public long TotalBytes { get; set; }
    public string? LicenseFileName { get; set; }
    public string? LicenseText { get; set; }
    public string? DetectedLicense { get; set; }
    public string? ReadmeMarkdown { get; set; }
    public Dictionary<string, int> LinesByExtension { get; set; } = [];
    public List<string> TopLevelEntries { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public bool HasSourceFiles => SourceFileCount > 0;
    public string? PrimaryLanguage { get; set; }
}

/// <summary>Walks a zip / tar.gz source archive without extracting it to disk.</summary>
public static class SourceAnalyzer
{
    private static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".c", ".h", ".cpp", ".hpp", ".cc", ".cs", ".fs", ".vb", ".java", ".kt", ".kts", ".scala", ".groovy", ".go", ".rs", ".swift", ".m", ".mm",
        ".py", ".rb", ".php", ".pl", ".pm", ".lua", ".r", ".jl", ".dart", ".ex", ".exs", ".erl", ".hs", ".ml", ".clj", ".elm", ".nim", ".zig", ".v",
        ".js", ".jsx", ".ts", ".tsx", ".mjs", ".cjs", ".vue", ".svelte", ".astro", ".html", ".htm", ".css", ".scss", ".sass", ".less",
        ".sh", ".bash", ".zsh", ".ps1", ".bat", ".cmd", ".sql", ".graphql", ".proto",
        ".json", ".yaml", ".yml", ".toml", ".xml", ".ini", ".cfg", ".env.example", ".md", ".rst", ".txt", ".gradle", ".cmake", ".mk", ".makefile", ".dockerfile"
    };

    private static readonly Dictionary<string, string> LanguageByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".cs"] = "C#", [".py"] = "Python", [".js"] = "JavaScript", [".jsx"] = "JavaScript", [".mjs"] = "JavaScript", [".ts"] = "TypeScript", [".tsx"] = "TypeScript",
        [".vue"] = "Vue", [".go"] = "Go", [".rs"] = "Rust", [".java"] = "Java", [".kt"] = "Kotlin", [".swift"] = "Swift", [".c"] = "C", [".h"] = "C", [".cpp"] = "C++",
        [".hpp"] = "C++", [".cc"] = "C++", [".rb"] = "Ruby", [".php"] = "PHP", [".dart"] = "Dart", [".lua"] = "Lua", [".sh"] = "Shell", [".ps1"] = "PowerShell",
        [".html"] = "HTML", [".css"] = "CSS", [".scss"] = "SCSS", [".sql"] = "SQL", [".r"] = "R", [".jl"] = "Julia", [".ex"] = "Elixir", [".hs"] = "Haskell", [".zig"] = "Zig", [".svelte"] = "Svelte"
    };

    private static readonly string[] SkipSegments = ["node_modules/", ".git/", "/.venv/", ".venv/", "venv/", "__pycache__/", "/bin/", "/obj/", "dist/", "build/", "target/", ".idea/", ".vs/", "vendor/", ".next/", "coverage/"];
    private static readonly string[] LicenseNames = ["LICENSE", "LICENSE.md", "LICENSE.txt", "LICENCE", "LICENCE.md", "LICENCE.txt", "COPYING", "COPYING.md", "COPYING.txt", "LICENSE.MIT", "LICENSE-MIT", "LICENSE.rst", "UNLICENSE"];
    private static readonly string[] ReadmeNames = ["README.md", "README.MD", "readme.md", "Readme.md", "README", "README.rst", "README.txt"];
    private const int MaxTextBytes = 2 * 1024 * 1024;

    public static async Task<SourceAnalysis> AnalyzeAsync(Stream archive, string fileName, CancellationToken ct)
    {
        var lower = fileName.ToLowerInvariant();
        if (lower.EndsWith(".zip")) return await AnalyzeZipAsync(archive, ct);
        if (lower.EndsWith(".tar.gz") || lower.EndsWith(".tgz")) return await AnalyzeTarAsync(new GZipStream(archive, CompressionMode.Decompress), ct);
        if (lower.EndsWith(".tar")) return await AnalyzeTarAsync(archive, ct);
        throw new InvalidOperationException("Unsupported archive format. Upload a .zip, .tar.gz or .tgz file.");
    }

    public static bool IsSupportedArchive(string fileName)
    {
        var l = fileName.ToLowerInvariant();
        return l.EndsWith(".zip") || l.EndsWith(".tar.gz") || l.EndsWith(".tgz") || l.EndsWith(".tar");
    }

    private static async Task<SourceAnalysis> AnalyzeZipAsync(Stream stream, CancellationToken ct)
    {
        var a = new SourceAnalysis();
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var prefix = CommonPrefix(zip.Entries.Select(e => e.FullName));
        foreach (var entry in zip.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (entry.FullName.EndsWith('/')) continue;
            await using var content = entry.Open();
            await Visit(a, StripPrefix(entry.FullName, prefix), entry.Length, content, ct);
        }
        Finish(a);
        return a;
    }

    private static async Task<SourceAnalysis> AnalyzeTarAsync(Stream stream, CancellationToken ct)
    {
        var a = new SourceAnalysis();
        await using var reader = new TarReader(stream, leaveOpen: true);
        var names = new List<string>();
        var buffered = new List<(string Name, long Size, byte[]? Head)>();
        while (await reader.GetNextEntryAsync(cancellationToken: ct) is { } entry)
        {
            if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile)) continue;
            names.Add(entry.Name);
            byte[]? head = null;
            if (entry.DataStream is not null && entry.Length <= MaxTextBytes && LooksTextual(entry.Name))
            {
                using var ms = new MemoryStream();
                await entry.DataStream.CopyToAsync(ms, ct);
                head = ms.ToArray();
            }
            buffered.Add((entry.Name, entry.Length, head));
        }
        var prefix = CommonPrefix(names);
        foreach (var (name, size, head) in buffered)
        {
            await using Stream content = head is null ? Stream.Null : new MemoryStream(head);
            await Visit(a, StripPrefix(name, prefix), size, content, ct);
        }
        Finish(a);
        return a;
    }

    /// <summary>GitHub / GitLab tarballs wrap everything in "owner-repo-sha/"; treat that folder as the root.</summary>
    private static string CommonPrefix(IEnumerable<string> names)
    {
        string? prefix = null;
        foreach (var n in names)
        {
            var slash = n.IndexOf('/');
            var first = slash < 0 ? string.Empty : n[..(slash + 1)];
            if (prefix is null) prefix = first;
            else if (prefix != first) return string.Empty;
        }
        return prefix ?? string.Empty;
    }

    private static string StripPrefix(string name, string prefix) => prefix.Length > 0 && name.StartsWith(prefix, StringComparison.Ordinal) ? name[prefix.Length..] : name;

    private static bool LooksTextual(string name)
    {
        var ext = ExtensionOf(name);
        var baseName = Path.GetFileName(name);
        return SourceExtensions.Contains(ext) || LicenseNames.Contains(baseName, StringComparer.OrdinalIgnoreCase) || ReadmeNames.Contains(baseName, StringComparer.OrdinalIgnoreCase)
               || baseName.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase) || baseName.Equals("Makefile", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtensionOf(string name)
    {
        var baseName = Path.GetFileName(name);
        if (baseName.Equals("Dockerfile", StringComparison.OrdinalIgnoreCase)) return ".dockerfile";
        if (baseName.Equals("Makefile", StringComparison.OrdinalIgnoreCase)) return ".makefile";
        return Path.GetExtension(baseName);
    }

    private static async Task Visit(SourceAnalysis a, string path, long size, Stream content, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(path)) return;
        a.FileCount++;
        a.TotalBytes += size;
        var top = path.Contains('/') ? path[..path.IndexOf('/')] + "/" : path;
        if (!a.TopLevelEntries.Contains(top) && a.TopLevelEntries.Count < 200) a.TopLevelEntries.Add(top);

        var baseName = Path.GetFileName(path);
        var isRoot = !path.Contains('/');
        if (isRoot && a.LicenseText is null && LicenseNames.Contains(baseName, StringComparer.OrdinalIgnoreCase))
        {
            a.LicenseFileName = baseName;
            a.LicenseText = await ReadText(content, size, ct);
            return;
        }
        if (isRoot && a.ReadmeMarkdown is null && ReadmeNames.Contains(baseName, StringComparer.OrdinalIgnoreCase))
        {
            a.ReadmeMarkdown = await ReadText(content, size, ct);
            return;
        }

        var normalized = "/" + path;
        if (SkipSegments.Any(s => normalized.Contains("/" + s.TrimStart('/'), StringComparison.OrdinalIgnoreCase))) return;
        var ext = ExtensionOf(path);
        if (!SourceExtensions.Contains(ext)) return;
        if (size > MaxTextBytes) return;
        var text = await ReadText(content, size, ct);
        if (text is null) return;
        var lines = CountLines(text);
        a.SourceFileCount++;
        a.LineCount += lines;
        a.LinesByExtension[ext] = a.LinesByExtension.GetValueOrDefault(ext) + lines;
    }

    private static async Task<string?> ReadText(Stream content, long size, CancellationToken ct)
    {
        if (size > MaxTextBytes || content == Stream.Null) return null;
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (bytes.AsSpan(0, Math.Min(bytes.Length, 4096)).Contains((byte)0)) return null; // binary
        return Encoding.UTF8.GetString(bytes);
    }

    private static int CountLines(string text)
    {
        var count = 0;
        foreach (var line in text.AsSpan().EnumerateLines())
            if (!line.IsWhiteSpace()) count++;
        return count;
    }

    private static void Finish(SourceAnalysis a)
    {
        a.DetectedLicense = LicenseDetector.Detect(a.LicenseText);
        var best = a.LinesByExtension.Where(kv => LanguageByExtension.ContainsKey(kv.Key)).OrderByDescending(kv => kv.Value).FirstOrDefault();
        if (best.Key is not null) a.PrimaryLanguage = LanguageByExtension[best.Key];
        if (a.FileCount == 0) a.Warnings.Add("The archive is empty.");
        else if (a.SourceFileCount == 0) a.Warnings.Add("No source files were found in the archive (only binaries or unknown file types).");
        if (a.LicenseText is null) a.Warnings.Add("No LICENSE / COPYING file at the root of the archive.");
    }
}

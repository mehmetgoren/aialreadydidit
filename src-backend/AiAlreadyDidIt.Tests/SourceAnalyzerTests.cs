using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using AiAlreadyDidIt.Api.Infrastructure.Import;

namespace AiAlreadyDidIt.Tests;

public class SourceAnalyzerTests
{
    private static readonly (string Path, byte[] Content)[] Project =
    [
        ("LICENSE", Encoding.UTF8.GetBytes(LicenseDetectorTests.Mit)),
        ("README.md", "# CPU-Z for Linux\n\nA GTK app.\n"u8.ToArray()),
        ("main.py", "import gtk\n\n\ndef main():\n    pass\n"u8.ToArray()),          // 3 non-blank lines
        ("src/ui/window.py", "class Window:\n    pass\n"u8.ToArray()),               // 2
        ("src/app.js", "console.log(1);\n"u8.ToArray()),                             // 1
        ("Makefile", "all:\n\techo hi\n"u8.ToArray()),                                // 2 (.makefile counts, not a language)
        ("node_modules/lib/index.js", "ignored();\n"u8.ToArray()),
        (".venv/lib/site.py", "ignored = 1\n"u8.ToArray()),
        ("dist/app.deb", new byte[] { 0x21, 0x3C, 0x61, 0x72, 0x63, 0x68, 0x3E }),
        ("docs/logo.png", new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
        ("data/blob.txt", [0x41, 0x00, 0x42])                                          // binary content, textual extension
    ];

    private static MemoryStream Zip(string prefix = "")
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            if (prefix.Length > 0) zip.CreateEntry(prefix);
            foreach (var (path, content) in Project)
            {
                var entry = zip.CreateEntry(prefix + path);
                using var s = entry.Open();
                s.Write(content);
            }
        }
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream TarGz(string prefix = "")
    {
        var ms = new MemoryStream();
        using (var gz = new GZipStream(ms, CompressionLevel.Fastest, leaveOpen: true))
        using (var tar = new TarWriter(gz, TarEntryFormat.Pax, leaveOpen: true))
        {
            foreach (var (path, content) in Project)
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, prefix + path) { DataStream = new MemoryStream(content) };
                tar.WriteEntry(entry);
            }
        }
        ms.Position = 0;
        return ms;
    }

    private static void AssertProject(SourceAnalysis a)
    {
        Assert.Equal("LICENSE", a.LicenseFileName);
        Assert.Equal("MIT", a.DetectedLicense);
        Assert.Contains("CPU-Z for Linux", a.ReadmeMarkdown);
        Assert.Equal("Python", a.PrimaryLanguage);
        Assert.Equal(5, a.LinesByExtension[".py"]);
        Assert.Equal(1, a.LinesByExtension[".js"]);
        Assert.Equal(2, a.LinesByExtension[".makefile"]);
        Assert.Equal(8, a.LineCount);
        Assert.Equal(4, a.SourceFileCount);             // main.py, window.py, app.js, Makefile
        Assert.Equal(Project.Length, a.FileCount);      // every regular file is counted, even skipped ones
        Assert.True(a.HasSourceFiles);
        Assert.Contains("src/", a.TopLevelEntries);
        Assert.Contains("LICENSE", a.TopLevelEntries);
        Assert.Empty(a.Warnings);
    }

    [Fact]
    public async Task Analyzes_a_zip_archive()
    {
        var a = await SourceAnalyzer.AnalyzeAsync(Zip(), "cpu_z.zip", CancellationToken.None);
        AssertProject(a);
    }

    [Fact]
    public async Task Analyzes_a_tar_gz_archive()
    {
        var a = await SourceAnalyzer.AnalyzeAsync(TarGz(), "cpu_z.tar.gz", CancellationToken.None);
        AssertProject(a);
    }

    [Fact]
    public async Task Strips_the_single_root_folder_github_tarballs_have()
    {
        var zip = await SourceAnalyzer.AnalyzeAsync(Zip("owner-repo-abc123/"), "repo.zip", CancellationToken.None);
        AssertProject(zip);
        var tgz = await SourceAnalyzer.AnalyzeAsync(TarGz("owner-repo-abc123/"), "repo.tgz", CancellationToken.None);
        AssertProject(tgz);
    }

    [Fact]
    public async Task Warns_when_license_or_sources_are_missing()
    {
        var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var s = zip.CreateEntry("bin/app.exe").Open();
            s.Write(new byte[] { 0x4D, 0x5A, 0x00 });
        }
        ms.Position = 0;
        var a = await SourceAnalyzer.AnalyzeAsync(ms, "x.zip", CancellationToken.None);
        Assert.Null(a.DetectedLicense);
        Assert.False(a.HasSourceFiles);
        Assert.Contains(a.Warnings, w => w.Contains("No source files"));
        Assert.Contains(a.Warnings, w => w.Contains("No LICENSE"));
    }

    [Fact]
    public async Task Empty_archive_is_reported()
    {
        var ms = new MemoryStream();
        using (new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true)) { }
        ms.Position = 0;
        var a = await SourceAnalyzer.AnalyzeAsync(ms, "empty.zip", CancellationToken.None);
        Assert.Equal(0, a.FileCount);
        Assert.Contains(a.Warnings, w => w.Contains("empty"));
    }

    [Fact]
    public async Task Rejects_unsupported_formats()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => SourceAnalyzer.AnalyzeAsync(new MemoryStream(), "source.rar", CancellationToken.None));
    }

    [Theory]
    [InlineData("a.zip", true)]
    [InlineData("A.ZIP", true)]
    [InlineData("a.tar.gz", true)]
    [InlineData("a.tgz", true)]
    [InlineData("a.tar", true)]
    [InlineData("a.7z", false)]
    [InlineData("a.rar", false)]
    [InlineData("a.deb", false)]
    public void IsSupportedArchive_checks_extension(string name, bool expected)
    {
        Assert.Equal(expected, SourceAnalyzer.IsSupportedArchive(name));
    }
}

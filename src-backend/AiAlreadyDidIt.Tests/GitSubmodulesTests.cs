using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using AiAlreadyDidIt.Api.Infrastructure.Import;

namespace AiAlreadyDidIt.Tests;

public class GitSubmodulesTests
{
    private const string Gitmodules = """
        [submodule "feniks.web_app"]
        	path = feniks.web_app
        	url = https://github.com/mehmetgoren/feniks.web_app.git
        [submodule "mngr"]
            path = mngr
            url = git@github.com:mehmetgoren/mngr.git
        [submodule "rel"]
            path = services/rel
            url = ../rel-service.git
        [submodule "broken"]
            url = https://github.com/x/y
        """;

    [Fact]
    public void Parses_paths_and_urls_and_skips_blocks_without_a_path()
    {
        var subs = GitSubmodules.Parse(Gitmodules);
        Assert.Equal(["feniks.web_app", "mngr", "services/rel"], subs.Select(s => s.Path));
        Assert.Equal("../rel-service.git", subs[2].Url);
        Assert.Empty(GitSubmodules.Parse(null));
    }

    [Theory]
    [InlineData("https://github.com/mehmetgoren/feniks.web_app.git", "https://github.com/mehmetgoren/feniks.web_app")]
    [InlineData("git@github.com:mehmetgoren/mngr.git", "https://github.com/mehmetgoren/mngr")]
    [InlineData("ssh://git@gitlab.com/group/proj.git", "https://gitlab.com/group/proj")]
    [InlineData("../rel-service.git", "https://github.com/mehmetgoren/rel-service")]
    [InlineData("git://github.com/a/b", "https://github.com/a/b")]
    public void Resolves_the_url_spellings_git_accepts(string raw, string expected) =>
        Assert.Equal(expected, GitSubmodules.ResolveUrl(raw, "github.com", "mehmetgoren")?.ToString());

    [Theory]
    [InlineData("https://bitbucket.org/a/b.git")]
    [InlineData("https://github.com/onlyowner")]
    [InlineData("")]
    public void Unknown_hosts_and_incomplete_paths_are_rejected(string raw) => Assert.Null(GitSubmodules.ResolveUrl(raw, "github.com", "o"));

    [Fact]
    public void Tarball_urls_are_anonymous_codeload_or_gitlab_archive_links()
    {
        Assert.Equal("https://codeload.github.com/o/r/tar.gz/abc123", GitSubmodules.TarballUrl(new Uri("https://github.com/o/r"), "abc123"));
        Assert.Equal("https://gitlab.com/g/p/-/archive/HEAD/p-HEAD.tar.gz", GitSubmodules.TarballUrl(new Uri("https://gitlab.com/g/p"), "HEAD"));
        Assert.Null(GitSubmodules.TarballUrl(new Uri("https://example.com/o/r"), "x"));
    }

    [Fact]
    public void Rerooting_drops_the_submodule_tarballs_top_folder_and_prefixes_the_submodule_path()
    {
        Assert.Equal("repo-main/services/sub/src/main.go", GitSubmodules.RerootedName("sub-abc123/src/main.go", "repo-main/", "services/sub"));
        Assert.Equal("repo-main/mngr/", GitSubmodules.RerootedName("mngr-1.0/", "repo-main/", "mngr"));
        Assert.Equal("mngr/README.md", GitSubmodules.RerootedName("mngr-1.0/README.md", "", "/mngr/"));
        Assert.Equal("a-1/", GitSubmodules.CommonRoot(["a-1/", "a-1/x.py", "./a-1/y/z.py"]));
        Assert.Equal("", GitSubmodules.CommonRoot(["x.py", "a/y.py"]));
    }

    [Fact]
    public async Task Merge_puts_submodule_files_under_their_path_inside_the_parent_root()
    {
        var dir = Directory.CreateTempSubdirectory("gitsub");
        try
        {
            var root = Path.Combine(dir.FullName, "root.tar.gz");
            var sub = Path.Combine(dir.FullName, "sub.tar.gz");
            var merged = Path.Combine(dir.FullName, "merged.tar.gz");
            await WriteTarGz(root, ("feniks-main/", null), ("feniks-main/README.md", "# umbrella"), ("feniks-main/mngr/", null));
            await WriteTarGz(sub, ("mngr-9f9f9f/", null), ("mngr-9f9f9f/main.go", "package main"), ("mngr-9f9f9f/pkg/util.go", "package pkg"));
            await GitSubmodules.MergeAsync(root, [("mngr", sub)], merged, CancellationToken.None);

            var names = await ListTarGz(merged);
            Assert.Contains("feniks-main/README.md", names);
            Assert.Contains("feniks-main/mngr/main.go", names);
            Assert.Contains("feniks-main/mngr/pkg/util.go", names);
            Assert.DoesNotContain(names, n => n.StartsWith("mngr-9f9f9f"));

            var analysis = await SourceAnalyzer.AnalyzeAsync(File.OpenRead(merged), "merged.tar.gz", CancellationToken.None);
            Assert.Equal(2, analysis.SourceFileCount);
            Assert.Equal("Go", analysis.PrimaryLanguage);
            Assert.Equal("# umbrella", analysis.ReadmeMarkdown?.Trim());
        }
        finally { dir.Delete(true); }
    }

    private static async Task WriteTarGz(string path, params (string Name, string? Content)[] entries)
    {
        await using var gz = new GZipStream(File.Create(path), CompressionLevel.Fastest);
        await using var writer = new TarWriter(gz, TarEntryFormat.Pax);
        foreach (var (name, content) in entries)
        {
            if (content is null) await writer.WriteEntryAsync(new PaxTarEntry(TarEntryType.Directory, name));
            else await writer.WriteEntryAsync(new PaxTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)) });
        }
    }

    private static async Task<List<string>> ListTarGz(string path)
    {
        var names = new List<string>();
        await using var gz = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
        await using var reader = new TarReader(gz);
        while (await reader.GetNextEntryAsync() is { } e) names.Add(e.Name);
        return names;
    }
}

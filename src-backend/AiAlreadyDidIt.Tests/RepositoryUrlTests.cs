using AiAlreadyDidIt.Api.Infrastructure.Import;

namespace AiAlreadyDidIt.Tests;

public class RepositoryUrlTests
{
    [Theory]
    [InlineData("https://github.com/owner/repo", "https://github.com/owner/repo")]
    [InlineData("https://github.com/owner/repo/", "https://github.com/owner/repo")]
    [InlineData("https://github.com/owner/repo.git", "https://github.com/owner/repo")]
    [InlineData("github.com/owner/repo", "https://github.com/owner/repo")]
    [InlineData("  github.com/owner/repo.git  ", "https://github.com/owner/repo")]
    [InlineData("git@github.com:owner/repo.git", "https://github.com/owner/repo")]
    [InlineData("git@gitlab.com:group/sub/repo.git", "https://gitlab.com/group/sub/repo")]
    [InlineData("http://gitlab.com/group/repo", "http://gitlab.com/group/repo")]
    public void Normalizes_common_forms(string input, string expected)
    {
        var uri = RepositoryUrl.Normalize(input);
        Assert.NotNull(uri);
        Assert.Equal(expected, uri!.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://github.com/owner/repo")]
    [InlineData("not a url at all")]
    public void Returns_null_for_unusable_input(string? input)
    {
        Assert.Null(RepositoryUrl.Normalize(input));
    }
}

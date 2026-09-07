using AiAlreadyDidIt.Api.Infrastructure;

namespace AiAlreadyDidIt.Tests;

public class TextUtilTests
{
    [Theory]
    [InlineData("CPU-Z for Linux", "cpu-z-for-linux")]
    [InlineData("  Hello   World  ", "hello-world")]
    [InlineData("Donanım İzleyici Şöför Çağrı Ğ", "donanim-izleyici-sofor-cagri-g")]
    [InlineData("System & Utilities", "system-and-utilities")]
    [InlineData("C++ / C# tools", "c-plus-plus-c-sharp-tools")]
    [InlineData("Crème brûlée", "creme-brulee")]
    [InlineData("---", "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Slugify_produces_lowercase_ascii_dashes(string input, string expected)
    {
        Assert.Equal(expected, TextUtil.Slugify(input));
    }

    [Fact]
    public void Slugify_respects_max_length_without_trailing_dash()
    {
        var slug = TextUtil.Slugify("abcde fghij klmno", maxLength: 6);
        Assert.Equal("abcde", slug);
        Assert.True(slug.Length <= 6);
    }

    [Fact]
    public void Sha256Hex_is_lowercase_64_hex_and_stable()
    {
        var a = TextUtil.Sha256Hex("aad_test");
        var b = TextUtil.Sha256Hex("aad_test");
        Assert.Equal(a, b);
        Assert.Equal(64, a.Length);
        Assert.Matches("^[0-9a-f]{64}$", a);
        Assert.Equal(a, TextUtil.Sha256Hex("aad_test"u8.ToArray()));
        Assert.NotEqual(a, TextUtil.Sha256Hex("aad_test2"));
    }

    [Fact]
    public void HashIp_is_salted_and_short()
    {
        Assert.Null(TextUtil.HashIp(null, "salt"));
        Assert.Null(TextUtil.HashIp("  ", "salt"));
        var h1 = TextUtil.HashIp("10.0.0.1", "salt-a");
        var h2 = TextUtil.HashIp("10.0.0.1", "salt-b");
        Assert.NotNull(h1);
        Assert.Equal(16, h1!.Length);
        Assert.NotEqual(h1, h2);
        Assert.Equal(h1, TextUtil.HashIp("10.0.0.1", "salt-a"));
    }

    [Fact]
    public void RandomToken_is_url_safe_and_unique()
    {
        var t1 = TextUtil.RandomToken();
        var t2 = TextUtil.RandomToken();
        Assert.NotEqual(t1, t2);
        Assert.Matches("^[A-Za-z0-9_-]+$", t1);
        Assert.Equal(43, t1.Length); // 32 bytes → 43 base64url chars without padding
        Assert.Equal(64, TextUtil.RandomToken(48).Length);
    }

    [Theory]
    [InlineData(null, 5, "")]
    [InlineData("", 5, "")]
    [InlineData("abc", 5, "abc")]
    [InlineData("abcdefgh", 5, "abcde")]
    public void Truncate_cuts_at_max(string? input, int max, string expected)
    {
        Assert.Equal(expected, TextUtil.Truncate(input, max));
    }

    [Fact]
    public void Excerpt_strips_markdown_and_caps_length()
    {
        var md = "# Title\n\n![shot](docs/1.png) Some **bold** text with a [link](https://x.y) and `code`.\n> quote\n\n- item";
        var excerpt = TextUtil.Excerpt(md);
        Assert.DoesNotContain("#", excerpt);
        Assert.DoesNotContain("![", excerpt);
        Assert.DoesNotContain("https://x.y", excerpt);
        Assert.Contains("link", excerpt);
        Assert.Contains("bold", excerpt);
        Assert.DoesNotContain("  ", excerpt);

        var longText = new string('a', 500);
        var capped = TextUtil.Excerpt(longText, 100);
        Assert.Equal(101, capped.Length);
        Assert.EndsWith("…", capped);
        Assert.Equal(string.Empty, TextUtil.Excerpt(null));
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(5L * 1024 * 1024, "5 MB")]
    [InlineData(3L * 1024 * 1024 * 1024, "3 GB")]
    [InlineData(5000L * 1024 * 1024 * 1024, "5000 GB")]
    public void HumanSize_formats_with_binary_units(long bytes, string expected)
    {
        Assert.Equal(expected, TextUtil.HumanSize(bytes));
    }
}

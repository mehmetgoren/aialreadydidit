using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AiAlreadyDidIt.Api.Infrastructure;

public static partial class TextUtil
{
    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex NonSlugChars();

    /// <summary>URL slug: lower-case ASCII, dashes, Turkish letters transliterated.</summary>
    public static string Slugify(string value, int maxLength = 80)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var s = value.Trim().ToLowerInvariant()
            .Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u').Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c')
            .Replace("&", " and ").Replace("+", " plus ").Replace("#", " sharp ");
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        var slug = NonSlugChars().Replace(sb.ToString(), "-").Trim('-');
        if (slug.Length > maxLength) slug = slug[..maxLength].TrimEnd('-');
        return slug;
    }

    public static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    public static string Sha256Hex(byte[] value) => Convert.ToHexStringLower(SHA256.HashData(value));

    /// <summary>Privacy-preserving IP fingerprint (salted hash, first 16 hex chars).</summary>
    public static string? HashIp(string? ip, string salt)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        return Sha256Hex(salt + "|" + ip)[..16];
    }

    /// <summary>URL-safe random token.</summary>
    public static string RandomToken(int bytes = 32)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value[..max];
    }

    public static string Excerpt(string? markdown, int max = 300)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;
        var text = Regex.Replace(markdown, @"!\[[^\]]*\]\([^)]*\)", " ");
        text = Regex.Replace(text, @"\[([^\]]*)\]\([^)]*\)", "$1");
        text = Regex.Replace(text, @"[#>*_`~|]+", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length <= max ? text : text[..max].TrimEnd() + "…";
    }

    public static string HumanSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
        return $"{size:0.#} {units[unit]}";
    }
}

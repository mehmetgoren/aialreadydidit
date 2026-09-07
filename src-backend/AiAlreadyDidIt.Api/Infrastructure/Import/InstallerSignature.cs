namespace AiAlreadyDidIt.Api.Infrastructure.Import;

/// <summary>
/// Checks that an install file's content matches what its extension promises (magic bytes), so a renamed HTML page or
/// text file cannot be published as an installer. Formats without a stable header (.dmg, .flatpak, plain scripts) are only
/// checked against the "looks like a web page / text" rules.
/// </summary>
public static class InstallerSignature
{
    /// <summary>Bytes needed from the start of the file to run every rule (ISO 9660 keeps its descriptor at 32 KiB).</summary>
    public const int HeadLength = 32 * 1024 + 8;

    private static readonly byte[] Mz = "MZ"u8.ToArray();
    private static readonly byte[] Elf = [0x7F, 0x45, 0x4C, 0x46];
    private static readonly byte[] Ole = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
    private static readonly byte[] Gzip = [0x1F, 0x8B];
    private static readonly byte[] Xz = [0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00];
    private static readonly byte[] Rpm = [0xED, 0xAB, 0xEE, 0xDB];
    private static readonly byte[] Ar = "!<arch>\n"u8.ToArray();
    private static readonly byte[] Xar = "xar!"u8.ToArray();
    private static readonly byte[] SquashFs = "hsqs"u8.ToArray();
    private static readonly byte[] Ustar = "ustar"u8.ToArray();
    private static readonly byte[] Iso = "CD001"u8.ToArray();
    private static readonly byte[] Shebang = "#!"u8.ToArray();
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly byte[] Utf16Le = [0xFF, 0xFE];
    private static readonly byte[] Utf16Be = [0xFE, 0xFF];

    private sealed record Rule(string Extension, string Expected, Func<ReadOnlyMemory<byte>, bool> Matches);

    /// <summary>Longest extension first so ".tar.gz" wins over ".gz" and ".app.zip" is treated like ".zip".</summary>
    private static readonly Rule[] Rules =
    [
        new(".app.zip", "a zip archive", Zip),
        new(".tar.gz", "a gzip-compressed tarball", h => StartsWith(h, Gzip)),
        new(".tar.xz", "an xz-compressed tarball", h => StartsWith(h, Xz)),
        new(".tgz", "a gzip-compressed tarball", h => StartsWith(h, Gzip)),
        new(".tar", "a tar archive", Tar),
        new(".zip", "a zip archive", Zip),
        new(".exe", "a Windows executable (MZ header)", h => StartsWith(h, Mz)),
        new(".msi", "a Windows Installer package", h => StartsWith(h, Ole)),
        new(".msix", "an MSIX package (zip)", Zip),
        new(".appx", "an APPX package (zip)", Zip),
        new(".deb", "a Debian package (ar archive)", h => StartsWith(h, Ar)),
        new(".rpm", "an RPM package", h => StartsWith(h, Rpm)),
        new(".appimage", "an AppImage (ELF or ISO 9660 image)", h => StartsWith(h, Elf) || IsIso(h)),
        new(".snap", "a snap (squashfs image)", h => StartsWith(h, SquashFs)),
        new(".pkg", "a macOS installer package (xar)", h => StartsWith(h, Xar)),
        new(".apk", "an Android package (zip)", Zip),
        new(".aab", "an Android App Bundle (zip)", Zip),
        new(".ipa", "an iOS package (zip)", Zip),
        new(".jar", "a Java archive (zip)", Zip),
        new(".whl", "a Python wheel (zip)", Zip),
        new(".gem", "a Ruby gem (tar archive)", Tar),
        new(".run", "a self-extracting installer (shell script or ELF)", h => StartsWith(h, Shebang) || StartsWith(h, Elf) || IsText(h)),
        new(".sh", "a shell script", IsText),
        new(".ps1", "a PowerShell script", IsText),
        new(".py", "a Python script", IsText),
        new(".js", "a JavaScript file", IsText),
    ];

    /// <summary>
    /// Returns null when the content is acceptable for <paramref name="fileName"/>, otherwise a message for the uploader.
    /// <paramref name="head"/> is the beginning of the file (up to <see cref="HeadLength"/> bytes; shorter files pass what they have).
    /// </summary>
    public static string? Check(string fileName, ReadOnlyMemory<byte> head)
    {
        if (head.Length == 0) return "The file is empty.";
        var lower = fileName.ToLowerInvariant();
        var rule = Rules.FirstOrDefault(r => lower.EndsWith(r.Extension, StringComparison.Ordinal));
        if (LooksLikeWebPage(head))
            return rule is null || !IsTextRule(rule) ? "The file is a web page, not an install file." : "The file is a web page, not a script.";
        if (rule is null) return null; // no reliable header for this format (.dmg, .flatpak, …)
        if (rule.Matches(head)) return null;
        return IsTextRule(rule)
            ? $"The file does not look like {rule.Expected}."
            : IsText(head)
                ? $"The file is plain text, not {rule.Expected}."
                : $"The file does not look like {rule.Expected} — check that it is not corrupted or renamed.";
    }

    private static bool IsTextRule(Rule rule) => rule.Extension is ".sh" or ".ps1" or ".py" or ".js";

    private static bool StartsWith(ReadOnlyMemory<byte> head, byte[] magic) => head.Span.StartsWith(magic);

    private static bool Zip(ReadOnlyMemory<byte> head)
    {
        var s = head.Span;
        return s.Length >= 4 && s[0] == 0x50 && s[1] == 0x4B && (s[2], s[3]) is (0x03, 0x04) or (0x05, 0x06) or (0x07, 0x08);
    }

    private static bool Tar(ReadOnlyMemory<byte> head)
    {
        var s = head.Span;
        if (s.Length < 257 + 5) return s.Length < 257; // a short prefix cannot be judged; a full-length one without "ustar" fails
        return s.Slice(257, 5).SequenceEqual(Ustar);
    }

    private static bool IsIso(ReadOnlyMemory<byte> head)
    {
        var s = head.Span;
        return s.Length >= 0x8001 + 5 && s.Slice(0x8001, 5).SequenceEqual(Iso);
    }

    private static bool LooksLikeWebPage(ReadOnlyMemory<byte> head)
    {
        var s = head.Span;
        if (s.StartsWith(Utf8Bom)) s = s[3..];
        var take = Math.Min(512, s.Length);
        var chars = new char[take];
        for (var i = 0; i < take; i++) chars[i] = s[i] < 0x80 ? (char)s[i] : '\uFFFD';
        var text = new string(chars).TrimStart(' ', '\t', '\r', '\n');
        return text.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<?php", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<script", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<head", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("<body", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when the sample contains no NUL bytes and almost only printable / whitespace characters (UTF-8 or UTF-16 text).</summary>
    public static bool IsText(ReadOnlyMemory<byte> head)
    {
        var s = head.Span;
        var sample = s.Length > 4096 ? s[..4096] : s;
        if (sample.StartsWith(Utf16Le) || sample.StartsWith(Utf16Be)) return !sample[2..].ToArray().Chunk(2).Any(pair => pair.Length == 2 && pair[0] == 0 && pair[1] == 0);
        if (sample.StartsWith(Utf8Bom)) sample = sample[3..];
        if (sample.Length == 0) return true;
        var control = 0;
        foreach (var b in sample)
        {
            if (b == 0) return false;
            if (b < 0x20 && b is not (0x09 or 0x0A or 0x0D or 0x0C or 0x1B)) control++;
        }
        return control * 100 / sample.Length < 5;
    }
}

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace AiAlreadyDidIt.Api.Infrastructure.Storage;

public sealed record ProcessedImage(byte[] Bytes, int Width, int Height, string ContentType);

/// <summary>Re-encodes uploads (strips metadata, normalises format) and produces thumbnails.</summary>
public static class ImageProcessor
{
    public static readonly string[] AllowedContentTypes = ["image/png", "image/jpeg", "image/webp", "image/gif"];

    public static bool LooksLikeImage(ReadOnlySpan<byte> head)
    {
        if (head.Length < 12) return false;
        if (head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47) return true; // PNG
        if (head[0] == 0xFF && head[1] == 0xD8) return true; // JPEG
        if (head[0] == (byte)'R' && head[1] == (byte)'I' && head[2] == (byte)'F' && head[3] == (byte)'F' && head[8] == (byte)'W' && head[9] == (byte)'E' && head[10] == (byte)'B' && head[11] == (byte)'P') return true;
        if (head[0] == (byte)'G' && head[1] == (byte)'I' && head[2] == (byte)'F') return true;
        return false;
    }

    /// <summary>Screenshot: WebP, max 1920px on the long edge. Thumb: WebP 480px wide.</summary>
    public static async Task<(ProcessedImage Full, ProcessedImage Thumb)> ProcessScreenshotAsync(Stream input, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(input, ct);
        image.Mutate(x => x.AutoOrient());
        var full = await Encode(image, 1920, 1920, 82, ct);
        var thumb = await Encode(image, 480, 480, 75, ct);
        return (full, thumb);
    }

    /// <summary>Icon: square WebP 256px.</summary>
    public static async Task<ProcessedImage> ProcessIconAsync(Stream input, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(input, ct);
        image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions { Size = new Size(256, 256), Mode = ResizeMode.Crop }));
        return await Encode(image, 256, 256, 85, ct);
    }

    public static async Task<ProcessedImage> ProcessBannerAsync(Stream input, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(input, ct);
        image.Mutate(x => x.AutoOrient());
        return await Encode(image, 2400, 1200, 82, ct);
    }

    private static async Task<ProcessedImage> Encode(Image source, int maxW, int maxH, int quality, CancellationToken ct)
    {
        using var clone = source.Clone(x =>
        {
            if (source.Width > maxW || source.Height > maxH)
                x.Resize(new ResizeOptions { Size = new Size(maxW, maxH), Mode = ResizeMode.Max });
        });
        using var ms = new MemoryStream();
        await clone.SaveAsync(ms, new WebpEncoder { Quality = quality }, ct);
        return new ProcessedImage(ms.ToArray(), clone.Width, clone.Height, "image/webp");
    }
}

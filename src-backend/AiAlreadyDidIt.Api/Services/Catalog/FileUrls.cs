using AiAlreadyDidIt.Api.Infrastructure.Storage;

namespace AiAlreadyDidIt.Api.Services.Catalog;

/// <summary>Builds the public URLs of stored images (served by <c>FilesController</c> through /files/...).</summary>
public static class FileUrls
{
    public static string? Screenshot(string? key) => key is null ? null : $"/files/screenshots/{key}";
    public static string? Icon(string? key) => key is null ? null : $"/files/icons/{key}";
    public static string? Banner(string? key) => key is null ? null : $"/files/banners/{key}";
    public static string Download(string slug, int fileId) => $"/api/v1/apps/{slug}/download/{fileId}";

    public static bool TryParseBucket(string name, out Bucket bucket)
    {
        switch (name.ToLowerInvariant())
        {
            case "screenshots": bucket = Bucket.Screenshots; return true;
            case "icons": bucket = Bucket.Icons; return true;
            case "banners": bucket = Bucket.Banners; return true;
            default: bucket = Bucket.Screenshots; return false;
        }
    }
}

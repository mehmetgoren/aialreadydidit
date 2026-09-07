using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using AiAlreadyDidIt.Api.Services.Catalog;

namespace AiAlreadyDidIt.Api.Services.Apps;

public sealed partial class AppEditorService
{
    // ---------------------------------------------------------------- screenshots

    public async Task<ScreenshotDto> UploadScreenshotAsync(int id, IFormFile file, string? caption, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var max = await settings.GetIntAsync(SettingKeys.MaxScreenshots, 10, ct);
        if (app.Screenshots.Count >= max) throw ApiException.Unprocessable($"At most {max} screenshots per app.", "file");
        var (full, thumb) = await ReadImageAsync(file, storageOptions.Value.MaxScreenshotBytes, ct);
        var baseKey = $"{app.Id}/{Guid.NewGuid():N}";
        await storage.PutAsync(Bucket.Screenshots, baseKey + ".webp", new MemoryStream(full.Bytes), full.ContentType, full.Bytes.Length, ct);
        await storage.PutAsync(Bucket.Screenshots, baseKey + "-thumb.webp", new MemoryStream(thumb.Bytes), thumb.ContentType, thumb.Bytes.Length, ct);
        var shot = new AppScreenshot
        {
            AppId = app.Id, StorageKey = baseKey + ".webp", ThumbStorageKey = baseKey + "-thumb.webp", Width = full.Width, Height = full.Height,
            Caption = string.IsNullOrWhiteSpace(caption) ? null : TextUtil.Truncate(caption.Trim(), 200), SortOrder = app.Screenshots.Count == 0 ? 0 : app.Screenshots.Max(s => s.SortOrder) + 1, CreatedAt = Clock.Now
        };
        app.Screenshots.Add(shot);
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return new ScreenshotDto { Id = shot.Id, Url = FileUrls.Screenshot(shot.StorageKey)!, ThumbUrl = FileUrls.Screenshot(shot.ThumbStorageKey)!, Width = shot.Width, Height = shot.Height, Caption = shot.Caption, SortOrder = shot.SortOrder };
    }

    public async Task DeleteScreenshotAsync(int id, int screenshotId, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var shot = app.Screenshots.FirstOrDefault(s => s.Id == screenshotId) ?? throw ApiException.NotFound("Screenshot not found.");
        await storage.DeleteAsync(Bucket.Screenshots, shot.StorageKey, ct);
        await storage.DeleteAsync(Bucket.Screenshots, shot.ThumbStorageKey, ct);
        app.Screenshots.Remove(shot);
        db.AppScreenshots.Remove(shot);
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
    }

    public async Task ReorderScreenshotsAsync(int id, ReorderRequest request, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var order = 0;
        foreach (var sid in request.Ids)
        {
            var shot = app.Screenshots.FirstOrDefault(s => s.Id == sid);
            if (shot is not null) shot.SortOrder = order++;
        }
        foreach (var rest in app.Screenshots.Where(s => !request.Ids.Contains(s.Id)).OrderBy(s => s.SortOrder)) rest.SortOrder = order++;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetScreenshotCaptionAsync(int id, int screenshotId, ScreenshotCaptionRequest request, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var shot = app.Screenshots.FirstOrDefault(s => s.Id == screenshotId) ?? throw ApiException.NotFound("Screenshot not found.");
        shot.Caption = string.IsNullOrWhiteSpace(request.Caption) ? null : TextUtil.Truncate(request.Caption.Trim(), 200);
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- icon

    public async Task<string> UploadIconAsync(int id, IFormFile file, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        if (file.Length > storageOptions.Value.MaxIconBytes) throw ApiException.Unprocessable($"Icons are limited to {TextUtil.HumanSize(storageOptions.Value.MaxIconBytes)}.", "file");
        await using var input = file.OpenReadStream();
        var head = new byte[16];
        var read = await input.ReadAtLeastAsync(head, 12, throwOnEndOfStream: false, ct);
        if (!ImageProcessor.LooksLikeImage(head.AsSpan(0, read))) throw ApiException.Unprocessable("Upload a PNG, JPEG or WebP image.", "file");
        input.Position = 0;
        var icon = await ImageProcessor.ProcessIconAsync(input, ct);
        if (app.IconStorageKey is not null) await storage.DeleteAsync(Bucket.Icons, app.IconStorageKey, ct);
        var key = $"{app.Id}/{Guid.NewGuid():N}.webp";
        await storage.PutAsync(Bucket.Icons, key, new MemoryStream(icon.Bytes), icon.ContentType, icon.Bytes.Length, ct);
        app.IconStorageKey = key;
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return FileUrls.Icon(key)!;
    }

    public async Task DeleteIconAsync(int id, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        if (app.IconStorageKey is null) return;
        await storage.DeleteAsync(Bucket.Icons, app.IconStorageKey, ct);
        app.IconStorageKey = null;
        await db.SaveChangesAsync(ct);
    }

    private static async Task<(ProcessedImage Full, ProcessedImage Thumb)> ReadImageAsync(IFormFile file, long maxBytes, CancellationToken ct)
    {
        if (file.Length == 0) throw ApiException.Unprocessable("The file is empty.", "file");
        if (file.Length > maxBytes) throw ApiException.Unprocessable($"Images are limited to {TextUtil.HumanSize(maxBytes)}.", "file");
        await using var input = file.OpenReadStream();
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        if (!ImageProcessor.LooksLikeImage(bytes)) throw ApiException.Unprocessable("Upload a PNG, JPEG, WebP or GIF image.", "file");
        try
        {
            return await ImageProcessor.ProcessScreenshotAsync(new MemoryStream(bytes), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw ApiException.Unprocessable("The image could not be decoded.", "file");
        }
    }
}

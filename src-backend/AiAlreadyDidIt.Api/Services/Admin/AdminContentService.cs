using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Admin;

/// <summary>Admin › Content: featured apps and home banners.</summary>
public sealed class AdminContentService(AadiDbContext db, AuditService audit, CatalogService catalog, IObjectStorage storage)
{
    public async Task<List<FeaturedItemDto>> FeaturedAsync(CancellationToken ct)
    {
        var cards = await db.Apps.AsNoTracking().Where(a => a.IsFeatured).OrderBy(a => a.FeaturedOrder).ThenByDescending(a => a.PublishedAt).Select(AppCardProjection.ToCard).ToListAsync(ct);
        var meta = await db.Apps.AsNoTracking().Where(a => a.IsFeatured).Select(a => new { a.Id, a.FeaturedOrder, a.FeaturedNote }).ToDictionaryAsync(a => a.Id, ct);
        return cards.Select(c => new FeaturedItemDto
        {
            Id = c.Id, Slug = c.Slug, Name = c.Name, ShortDescription = c.ShortDescription, IconUrl = c.IconUrl, CoverUrl = c.CoverUrl, CategoryNameEn = c.CategoryNameEn, CategoryNameTr = c.CategoryNameTr, CategorySlug = c.CategorySlug, Status = c.Status,
            DownloadCount = c.DownloadCount, RatingAvg = c.RatingAvg, RatingCount = c.RatingCount, IsFeatured = true, FeaturedOrder = meta[c.Id].FeaturedOrder, FeaturedNote = meta[c.Id].FeaturedNote, UploaderUsername = c.UploaderUsername, PublishedAt = c.PublishedAt,
            LicenseSpdxId = c.LicenseSpdxId, LlmModelName = c.LlmModelName, Platforms = c.Platforms, LatestVersion = c.LatestVersion, UpdatedAt = c.UpdatedAt
        }).ToList();
    }

    public async Task<List<FeaturedItemDto>> FeatureAsync(FeatureAppRequest request, CancellationToken ct)
    {
        var app = await db.Apps.FirstOrDefaultAsync(a => a.Id == request.AppId && a.Status == AppStatus.Published, ct) ?? throw ApiException.NotFound("Published app not found.");
        app.IsFeatured = true;
        app.FeaturedNote = request.Note?.Trim();
        app.FeaturedOrder = (await db.Apps.Where(a => a.IsFeatured).Select(a => (int?)a.FeaturedOrder).MaxAsync(ct) ?? 0) + 1;
        audit.Log("featured.add", "app", app.Id, request);
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        return await FeaturedAsync(ct);
    }

    public async Task<List<FeaturedItemDto>> UnfeatureAsync(int appId, CancellationToken ct)
    {
        await db.Apps.Where(a => a.Id == appId).ExecuteUpdateAsync(s => s.SetProperty(a => a.IsFeatured, false).SetProperty(a => a.FeaturedOrder, 0), ct);
        audit.Log("featured.remove", "app", appId);
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        return await FeaturedAsync(ct);
    }

    public async Task<List<FeaturedItemDto>> ReorderAsync(SaveFeaturedRequest request, CancellationToken ct)
    {
        var apps = await db.Apps.Where(a => request.OrderedAppIds.Contains(a.Id)).ToListAsync(ct);
        var order = 0;
        foreach (var id in request.OrderedAppIds) { var a = apps.FirstOrDefault(x => x.Id == id); if (a is not null) a.FeaturedOrder = ++order; }
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        return await FeaturedAsync(ct);
    }

    // ---------------------------------------------------------------- banners

    public Task<List<AdminBannerDto>> BannersAsync(CancellationToken ct) =>
        db.Banners.AsNoTracking().OrderBy(b => b.Position).ThenBy(b => b.SortOrder).Select(b => new AdminBannerDto { Id = b.Id, Title = b.Title, Subtitle = b.Subtitle, ImageUrl = b.ImageStorageKey == null ? null : "/files/banners/" + b.ImageStorageKey, Link = b.Link, Position = b.Position, SortOrder = b.SortOrder, IsActive = b.IsActive, StartsAt = b.StartsAt, EndsAt = b.EndsAt, CreatedAt = b.CreatedAt }).ToListAsync(ct);

    public async Task<AdminBannerDto> SaveBannerAsync(int? id, SaveBannerRequest request, CancellationToken ct)
    {
        var banner = id is null ? new Banner { CreatedAt = Clock.Now } : await db.Banners.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw ApiException.NotFound("Banner not found.");
        banner.Title = request.Title.Trim(); banner.Subtitle = request.Subtitle?.Trim(); banner.Link = request.Link?.Trim(); banner.Position = request.Position is "sidebar" ? "sidebar" : "hero";
        banner.SortOrder = request.SortOrder; banner.IsActive = request.IsActive; banner.StartsAt = request.StartsAt; banner.EndsAt = request.EndsAt;
        if (id is null) db.Banners.Add(banner);
        audit.Log(id is null ? "banner.create" : "banner.update", "banner", id, request);
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        return (await BannersAsync(ct)).First(b => b.Id == banner.Id);
    }

    public async Task<AdminBannerDto> UploadBannerImageAsync(int id, IFormFile file, CancellationToken ct)
    {
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw ApiException.NotFound("Banner not found.");
        await using var input = file.OpenReadStream();
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms, ct);
        if (!ImageProcessor.LooksLikeImage(ms.ToArray())) throw ApiException.Unprocessable("Upload a PNG, JPEG or WebP image.", "file");
        ms.Position = 0;
        var processed = await ImageProcessor.ProcessBannerAsync(ms, ct);
        if (banner.ImageStorageKey is not null) await storage.DeleteAsync(Bucket.Banners, banner.ImageStorageKey, ct);
        var key = $"{id}/{Guid.NewGuid():N}.webp";
        await storage.PutAsync(Bucket.Banners, key, new MemoryStream(processed.Bytes), processed.ContentType, processed.Bytes.Length, ct);
        banner.ImageStorageKey = key;
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        return (await BannersAsync(ct)).First(b => b.Id == id);
    }

    public async Task DeleteBannerAsync(int id, CancellationToken ct)
    {
        var banner = await db.Banners.FirstOrDefaultAsync(b => b.Id == id, ct) ?? throw ApiException.NotFound("Banner not found.");
        if (banner.ImageStorageKey is not null) await storage.DeleteAsync(Bucket.Banners, banner.ImageStorageKey, ct);
        db.Banners.Remove(banner);
        audit.Log("banner.delete", "banner", id);
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
    }
}

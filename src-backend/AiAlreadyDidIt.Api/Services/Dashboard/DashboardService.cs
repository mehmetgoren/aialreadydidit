using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Contracts.Dashboard;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Auth;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Dashboard;

/// <summary>Member dashboard: overview, download history, favourites, collections, watches, notifications, API keys.</summary>
public sealed class DashboardService(AadiDbContext db, ICurrentUser currentUser, SiteSettingsCache settings)
{
    private int Uid => currentUser.Id;

    public async Task<DashboardOverviewDto> OverviewAsync(CancellationToken ct)
    {
        var apps = await db.Apps.AsNoTracking().Where(a => a.UploaderUserId == Uid && a.Status != AppStatus.Removed).Select(a => new { a.Status, a.DownloadCount, a.EstGenerationTokens }).ToListAsync(ct);
        var tokens = apps.Where(a => a.Status == AppStatus.Published).Sum(a => a.EstGenerationTokens * a.DownloadCount);
        var c = await settings.SavingsAsync(ct);
        return new DashboardOverviewDto
        {
            AppCount = apps.Count,
            PublishedAppCount = apps.Count(a => a.Status == AppStatus.Published),
            PendingAppCount = apps.Count(a => a.Status is AppStatus.PendingReview or AppStatus.PendingScan),
            TotalDownloads = apps.Sum(a => a.DownloadCount),
            TokensSavedByMyApps = tokens,
            CostSavedByMyApps = c.Cost(tokens),
            RatingCount = await db.Ratings.CountAsync(r => r.UserId == Uid, ct),
            FavoriteCount = await db.Favorites.CountAsync(f => f.UserId == Uid, ct),
            CollectionCount = await db.Collections.CountAsync(x => x.UserId == Uid, ct),
            WatchCount = await db.AppWatches.CountAsync(w => w.UserId == Uid, ct),
            UnreadNotifications = await db.Notifications.CountAsync(n => n.UserId == Uid && n.ReadAt == null, ct),
            ApiKeyCount = await db.ApiKeys.CountAsync(k => k.UserId == Uid && k.RevokedAt == null, ct),
            RecentNotifications = await db.Notifications.AsNoTracking().Where(n => n.UserId == Uid).OrderByDescending(n => n.CreatedAt).Take(5).Select(MapNotification).ToListAsync(ct),
            RecentApps = await db.Apps.AsNoTracking().Where(a => a.UploaderUserId == Uid && a.Status != AppStatus.Removed).OrderByDescending(a => a.UpdatedAt).Take(4).Select(AppCardProjection.ToCard).ToListAsync(ct)
        };
    }

    // ---------------------------------------------------------------- downloads

    public async Task<PagedResult<DownloadHistoryDto>> DownloadsAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = db.Downloads.AsNoTracking().Where(d => d.UserId == Uid).OrderByDescending(d => d.CreatedAt);
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var appIds = rows.Select(r => r.AppId).Distinct().ToList();
        var fileIds = rows.Select(r => r.FileId).Distinct().ToList();
        var cards = await db.Apps.AsNoTracking().Where(a => appIds.Contains(a.Id)).Select(AppCardProjection.ToCard).ToDictionaryAsync(c => c.Id, ct);
        var files = await db.AppFiles.AsNoTracking().Where(f => fileIds.Contains(f.Id)).Select(f => new { f.Id, f.FileName, PlatformCode = f.Platform == null ? null : f.Platform.Code, f.Version.Version }).ToDictionaryAsync(f => f.Id, ct);
        var rated = await db.Ratings.AsNoTracking().Where(r => r.UserId == Uid && appIds.Contains(r.AppId)).Select(r => r.AppId).ToHashSetAsync(ct);
        var items = rows.Where(r => cards.ContainsKey(r.AppId)).Select(r => new DownloadHistoryDto
        {
            Id = r.Id, App = cards[r.AppId], Version = files.TryGetValue(r.FileId, out var f) ? f.Version : "", FileName = f?.FileName ?? "", PlatformCode = f?.PlatformCode, FileId = r.FileId,
            Source = r.Source, CreatedAt = r.CreatedAt, Rated = rated.Contains(r.AppId)
        }).ToList();
        return PagedResult<DownloadHistoryDto>.Create(items, page, pageSize, total);
    }

    // ---------------------------------------------------------------- favourites

    public async Task<List<AppCardDto>> FavoritesAsync(CancellationToken ct) =>
        await db.Favorites.AsNoTracking().Where(f => f.UserId == Uid).OrderByDescending(f => f.CreatedAt).Select(f => f.App).Select(AppCardProjection.ToCard).ToListAsync(ct);

    public async Task<bool> ToggleFavoriteAsync(int appId, CancellationToken ct)
    {
        if (!await db.Apps.AnyAsync(a => a.Id == appId && a.Status == AppStatus.Published, ct)) throw ApiException.NotFound("App not found.");
        var existing = await db.Favorites.FirstOrDefaultAsync(f => f.UserId == Uid && f.AppId == appId, ct);
        bool result;
        if (existing is null) { db.Favorites.Add(new Favorite { UserId = Uid, AppId = appId, CreatedAt = Clock.Now }); result = true; }
        else { db.Favorites.Remove(existing); result = false; }
        await db.SaveChangesAsync(ct);
        await db.Apps.Where(a => a.Id == appId).ExecuteUpdateAsync(s => s.SetProperty(a => a.FavoriteCount, a => db.Favorites.Count(f => f.AppId == appId)), ct);
        return result;
    }

    // ---------------------------------------------------------------- collections

    public async Task<List<CollectionDto>> CollectionsAsync(CancellationToken ct) =>
        await db.Collections.AsNoTracking().Where(c => c.UserId == Uid).OrderByDescending(c => c.UpdatedAt)
            .Select(c => new CollectionDto { Id = c.Id, Name = c.Name, Slug = c.Slug, Description = c.Description, IsPublic = c.IsPublic, OwnerUsername = c.User.Username, OwnerDisplayName = c.User.DisplayName, ItemCount = c.Items.Count, UpdatedAt = c.UpdatedAt })
            .ToListAsync(ct);

    public async Task<CollectionDto> CollectionAsync(int id, CancellationToken ct)
    {
        var c = await db.Collections.AsNoTracking().Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid, ct) ?? throw ApiException.NotFound("Collection not found.");
        var items = await db.CollectionItems.AsNoTracking().Where(i => i.CollectionId == id).OrderBy(i => i.SortOrder).Select(i => i.App).Select(AppCardProjection.ToCard).ToListAsync(ct);
        return new CollectionDto { Id = c.Id, Name = c.Name, Slug = c.Slug, Description = c.Description, IsPublic = c.IsPublic, OwnerUsername = c.User.Username, OwnerDisplayName = c.User.DisplayName, ItemCount = items.Count, UpdatedAt = c.UpdatedAt, Items = items };
    }

    public async Task<CollectionDto> CreateCollectionAsync(SaveCollectionRequest request, CancellationToken ct)
    {
        var slug = TextUtil.Slugify(request.Name, 60);
        if (slug.Length < 2) slug = "collection";
        var baseSlug = slug; var i = 1;
        while (await db.Collections.AnyAsync(c => c.UserId == Uid && c.Slug == slug, ct)) slug = $"{baseSlug}-{++i}";
        var c = new Collection { UserId = Uid, Name = request.Name.Trim(), Slug = slug, Description = request.Description?.Trim(), IsPublic = request.IsPublic, CreatedAt = Clock.Now, UpdatedAt = Clock.Now };
        db.Collections.Add(c);
        await db.SaveChangesAsync(ct);
        return await CollectionAsync(c.Id, ct);
    }

    public async Task<CollectionDto> UpdateCollectionAsync(int id, SaveCollectionRequest request, CancellationToken ct)
    {
        var c = await db.Collections.FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid, ct) ?? throw ApiException.NotFound("Collection not found.");
        c.Name = request.Name.Trim(); c.Description = request.Description?.Trim(); c.IsPublic = request.IsPublic; c.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return await CollectionAsync(id, ct);
    }

    public async Task DeleteCollectionAsync(int id, CancellationToken ct)
    {
        var c = await db.Collections.FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid, ct) ?? throw ApiException.NotFound("Collection not found.");
        db.Collections.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CollectionDto> AddToCollectionAsync(int id, CollectionItemRequest request, CancellationToken ct)
    {
        var c = await db.Collections.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid, ct) ?? throw ApiException.NotFound("Collection not found.");
        if (!await db.Apps.AnyAsync(a => a.Id == request.AppId && a.Status == AppStatus.Published, ct)) throw ApiException.NotFound("App not found.");
        if (c.Items.All(i => i.AppId != request.AppId))
            c.Items.Add(new CollectionItem { AppId = request.AppId, Note = request.Note?.Trim(), SortOrder = c.Items.Count == 0 ? 0 : c.Items.Max(i => i.SortOrder) + 1, AddedAt = Clock.Now });
        c.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return await CollectionAsync(id, ct);
    }

    public async Task<CollectionDto> RemoveFromCollectionAsync(int id, int appId, CancellationToken ct)
    {
        var c = await db.Collections.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id && x.UserId == Uid, ct) ?? throw ApiException.NotFound("Collection not found.");
        var item = c.Items.FirstOrDefault(i => i.AppId == appId);
        if (item is not null) { c.Items.Remove(item); db.CollectionItems.Remove(item); c.UpdatedAt = Clock.Now; await db.SaveChangesAsync(ct); }
        return await CollectionAsync(id, ct);
    }

    // ---------------------------------------------------------------- watches

    public async Task<List<WatchDto>> WatchesAsync(CancellationToken ct)
    {
        var rows = await db.AppWatches.AsNoTracking().Where(w => w.UserId == Uid).OrderByDescending(w => w.CreatedAt).ToListAsync(ct);
        var ids = rows.Select(r => r.AppId).ToList();
        var cards = await db.Apps.AsNoTracking().Where(a => ids.Contains(a.Id)).Select(AppCardProjection.ToCard).ToDictionaryAsync(c => c.Id, ct);
        return rows.Where(r => cards.ContainsKey(r.AppId)).Select(r => new WatchDto { Id = r.Id, App = cards[r.AppId], NotifyNewVersion = r.NotifyNewVersion, NotifyReplies = r.NotifyReplies, CreatedAt = r.CreatedAt }).ToList();
    }

    public async Task<bool> SetWatchAsync(int appId, WatchRequest? request, bool watch, CancellationToken ct)
    {
        if (!await db.Apps.AnyAsync(a => a.Id == appId && (a.Status == AppStatus.Published || a.Status == AppStatus.Unlisted), ct)) throw ApiException.NotFound("App not found.");
        var existing = await db.AppWatches.FirstOrDefaultAsync(w => w.UserId == Uid && w.AppId == appId, ct);
        if (!watch) { if (existing is not null) db.AppWatches.Remove(existing); await db.SaveChangesAsync(ct); return false; }
        existing ??= db.AppWatches.Add(new AppWatch { UserId = Uid, AppId = appId, CreatedAt = Clock.Now }).Entity;
        existing.NotifyNewVersion = request?.NotifyNewVersion ?? true;
        existing.NotifyReplies = request?.NotifyReplies ?? true;
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ---------------------------------------------------------------- notifications

    public async Task<PagedResult<NotificationDto>> NotificationsAsync(bool unreadOnly, int page, int pageSize, CancellationToken ct)
    {
        var q = db.Notifications.AsNoTracking().Where(n => n.UserId == Uid);
        if (unreadOnly) q = q.Where(n => n.ReadAt == null);
        return await q.OrderByDescending(n => n.CreatedAt).Select(MapNotification).ToPagedAsync(page, pageSize, ct);
    }

    public async Task MarkReadAsync(long? id, CancellationToken ct)
    {
        var q = db.Notifications.Where(n => n.UserId == Uid && n.ReadAt == null);
        if (id is { } nid) q = q.Where(n => n.Id == nid);
        await q.ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, Clock.Now), ct);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Notification, NotificationDto>> MapNotification = n => new NotificationDto
    {
        Id = n.Id, Type = n.Type, Title = n.Title, Body = n.Body, Link = n.Link, ReadAt = n.ReadAt, CreatedAt = n.CreatedAt
    };

    // ---------------------------------------------------------------- API keys

    public async Task<List<ApiKeyDto>> ApiKeysAsync(CancellationToken ct) =>
        await db.ApiKeys.AsNoTracking().Where(k => k.UserId == Uid).OrderByDescending(k => k.CreatedAt).Select(k => MapKey(k, null)).ToListAsync(ct);

    public async Task<ApiKeyDto> CreateApiKeyAsync(CreateApiKeyRequest request, CancellationToken ct)
    {
        if (currentUser.IsApiKey) throw ApiException.Forbidden("API keys cannot create API keys.");
        var max = await settings.GetIntAsync(SettingKeys.MaxApiKeysPerUser, 5, ct);
        var active = await db.ApiKeys.CountAsync(k => k.UserId == Uid && k.RevokedAt == null, ct);
        if (active >= max) throw ApiException.Unprocessable($"You can have at most {max} active API keys. Revoke one first.");
        var allowed = new[] { "read", "download", "submit" };
        var scopes = (request.Scopes ?? ["read", "download"]).Select(s => s.Trim().ToLowerInvariant()).Where(allowed.Contains).Distinct().ToList();
        if (!scopes.Contains("read")) scopes.Insert(0, "read");
        var secret = ApiKeyAuthenticationHandler.KeyPrefix + TextUtil.RandomToken(30);
        var key = new ApiKey
        {
            UserId = Uid, Name = request.Name.Trim(), Prefix = secret[..12], KeyHash = TextUtil.Sha256Hex(secret), Scopes = string.Join(',', scopes), RateTier = "default",
            ExpiresAt = request.ExpiresInDays is { } d and > 0 ? Clock.Now.AddDays(d) : null, CreatedAt = Clock.Now
        };
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync(ct);
        return MapKey(key, secret);
    }

    public async Task RevokeApiKeyAsync(int id, CancellationToken ct)
    {
        var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.UserId == Uid, ct) ?? throw ApiException.NotFound("API key not found.");
        key.RevokedAt ??= Clock.Now;
        await db.SaveChangesAsync(ct);
    }

    private static ApiKeyDto MapKey(ApiKey k, string? secret) => new()
    {
        Id = k.Id, Name = k.Name, Prefix = k.Prefix, Scopes = k.Scopes, RateTier = k.RateTier, LastUsedAt = k.LastUsedAt, ExpiresAt = k.ExpiresAt, RevokedAt = k.RevokedAt,
        RequestCount = k.RequestCount, DownloadCount = k.DownloadCount, CreatedAt = k.CreatedAt, Secret = secret
    };
}

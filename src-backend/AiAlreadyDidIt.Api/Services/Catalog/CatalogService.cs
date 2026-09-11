using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Search;
using AiAlreadyDidIt.Api.Services.Site;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AiAlreadyDidIt.Api.Services.Catalog;

/// <summary>Public, read-only storefront: categories, home page, app detail, uploader profiles, lineage, collections.</summary>
public sealed class CatalogService(AadiDbContext db, CategoryIndexService categories, SearchService search, SavingsService savings,
    SiteSettingsCache settings, ICurrentUser currentUser, IMemoryCache cache)
{
    // ---------------------------------------------------------------- categories & reference lists

    public async Task<List<CategoryNodeDto>> GetCategoriesAsync(CancellationToken ct) => (await categories.GetAsync(ct)).Roots;

    public async Task<CategoryDetailDto> GetCategoryAsync(string slug, CancellationToken ct)
    {
        var node = await categories.RequireBySlugAsync(slug, ct);
        var path = await categories.PathAsync(node.Id, ct);
        return new CategoryDetailDto { Node = node, Ancestors = path.Take(path.Count - 1).ToList(), Children = node.Children };
    }

    public Task<List<PlatformDto>> GetPlatformsAsync(CancellationToken ct) =>
        db.Platforms.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.SortOrder)
            .Select(p => new PlatformDto { Id = p.Id, Code = p.Code, Name = p.Name, Icon = p.Icon, AllowedExtensions = p.AllowedExtensions, AllowsExternalReference = p.AllowsExternalReference, InstallHint = p.InstallHint })
            .ToListAsync(ct);

    public Task<List<LicenseDto>> GetLicensesAsync(CancellationToken ct) =>
        db.Licenses.AsNoTracking().OrderBy(l => l.SortOrder)
            .Select(l => new LicenseDto { Id = l.Id, SpdxId = l.SpdxId, Name = l.Name, Url = l.Url, IsOsiApproved = l.IsOsiApproved, IsAllowed = l.IsAllowed, AppCount = l.AppCount })
            .ToListAsync(ct);

    public Task<List<LlmModelDto>> GetLlmModelsAsync(CancellationToken ct) =>
        db.LlmModels.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.SortOrder)
            .Select(m => new LlmModelDto { Id = m.Id, Vendor = m.Vendor, Name = m.Name, Version = m.Version, Slug = m.Slug, DisplayName = m.Vendor + " " + m.Name + (m.Version == null ? "" : " " + m.Version), AppCount = m.AppCount })
            .ToListAsync(ct);

    public Task<List<TagDto>> GetTagsAsync(string? q, int take, CancellationToken ct)
    {
        var query = db.Tags.AsNoTracking().Where(t => !t.IsBlocked);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(t => EF.Functions.ILike(t.Name, $"%{q.Trim()}%"));
        return query.OrderByDescending(t => t.UsageCount).ThenBy(t => t.Name).Take(Math.Clamp(take, 1, 100))
            .Select(t => new TagDto { Id = t.Id, Name = t.Name, Slug = t.Slug, UsageCount = t.UsageCount }).ToListAsync(ct);
    }

    // ---------------------------------------------------------------- home

    public async Task<HomeDto> GetHomeAsync(CancellationToken ct)
    {
        const string key = "catalog:home:v1";
        if (cache.TryGetValue(key, out HomeDto? cached) && cached is not null) return WithSavings(cached, await savings.GetAsync(ct));

        var featuredCount = await settings.GetIntAsync(SettingKeys.HomeFeaturedCount, 8, ct);
        var trendingDays = await settings.GetIntAsync(SettingKeys.HomeTrendingDays, 7, ct);
        var published = db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published);
        var since = Clock.Now.AddDays(-trendingDays);

        var trendingIds = await db.Downloads.AsNoTracking().Where(d => d.CreatedAt >= since)
            .GroupBy(d => d.AppId).Select(g => new { AppId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).Take(12).ToListAsync(ct);
        var trendingCards = await published.Where(a => trendingIds.Select(t => t.AppId).Contains(a.Id)).Select(AppCardProjection.ToCard).ToListAsync(ct);
        var trending = trendingIds.Select(t => trendingCards.FirstOrDefault(c => c.Id == t.AppId)).Where(c => c is not null).Cast<AppCardDto>().ToList();
        if (trending.Count < 6)
        {
            var fill = await published.Where(a => !trending.Select(t => t.Id).Contains(a.Id)).OrderByDescending(a => a.DownloadCount).Take(12 - trending.Count).Select(AppCardProjection.ToCard).ToListAsync(ct);
            trending.AddRange(fill);
        }

        var now = Clock.Now;
        var banners = await db.Banners.AsNoTracking()
            .Where(b => b.IsActive && (b.StartsAt == null || b.StartsAt <= now) && (b.EndsAt == null || b.EndsAt >= now))
            .OrderBy(b => b.SortOrder)
            .Select(b => new BannerDto { Id = b.Id, Title = b.Title, Subtitle = b.Subtitle, ImageUrl = b.ImageStorageKey == null ? null : "/files/banners/" + b.ImageStorageKey, Link = b.Link, Position = b.Position })
            .ToListAsync(ct);

        var home = new HomeDto
        {
            Featured = await published.Where(a => a.IsFeatured).OrderBy(a => a.FeaturedOrder).ThenByDescending(a => a.PublishedAt).Take(featuredCount).Select(AppCardProjection.ToCard).ToListAsync(ct),
            Trending = trending,
            Newest = await published.OrderByDescending(a => a.PublishedAt).Take(12).Select(AppCardProjection.ToCard).ToListAsync(ct),
            RecentlyUpdated = await published.Where(a => a.Versions.Count > 1).OrderByDescending(a => a.UpdatedAt).Take(12).Select(AppCardProjection.ToCard).ToListAsync(ct),
            TopRated = await published.Where(a => a.RatingCount >= 1).OrderByDescending(a => a.RatingAvg).ThenByDescending(a => a.RatingCount).Take(12).Select(AppCardProjection.ToCard).ToListAsync(ct),
            Categories = (await categories.GetAsync(ct)).Roots,
            Banners = banners,
            OpenRequestCount = await db.AppRequests.CountAsync(r => r.Status == AppRequestStatus.Open, ct),
            Stats = new HomeStatsDto
            {
                PublishedApps = await published.CountAsync(ct),
                Members = await db.Users.CountAsync(u => u.IsActive, ct),
                Downloads = await db.Downloads.LongCountAsync(ct),
                Searches = await db.SearchLogs.LongCountAsync(ct),
                AgentSearches = await db.SearchLogs.CountAsync(s => s.Source != RequestSource.Web, ct)
            }
        };
        cache.Set(key, home, TimeSpan.FromMinutes(2));
        return WithSavings(home, await savings.GetAsync(ct));

        static HomeDto WithSavings(HomeDto h, SavingsDto s) { h.Savings = s; return h; }
    }

    public void InvalidateHome() => cache.Remove("catalog:home:v1");

    // ---------------------------------------------------------------- app detail

    public async Task<AppDetailDto> GetAppAsync(string slug, bool countView, CancellationToken ct)
    {
        var app = await db.Apps.AsNoTracking()
            .Include(a => a.Category).Include(a => a.License).Include(a => a.LlmModel).Include(a => a.Uploader)
            .Include(a => a.Screenshots).Include(a => a.Prompts).Include(a => a.AppTags).ThenInclude(t => t.Tag)
            .Include(a => a.Versions).ThenInclude(v => v.Files).ThenInclude(f => f.Platform)
            .Include(a => a.DerivedFrom)
            .FirstOrDefaultAsync(a => a.Slug == slug, ct) ?? throw ApiException.NotFound("App not found.");

        var viewerId = currentUser.IdOrNull;
        var isOwner = viewerId is not null && viewerId == app.UploaderUserId;
        var visible = app.Status is AppStatus.Published or AppStatus.Unlisted || isOwner || currentUser.IsAdmin;
        if (!visible) throw ApiException.NotFound("App not found.");

        if (countView && app.Status == AppStatus.Published && !isOwner)
            await db.Apps.Where(a => a.Id == app.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1), ct);

        var dto = await MapDetailAsync(app, isOwner || currentUser.IsAdmin, ct);
        if (viewerId is { } uid)
        {
            dto.Viewer.IsOwner = isOwner;
            dto.Viewer.IsFavorite = await db.Favorites.AnyAsync(f => f.UserId == uid && f.AppId == app.Id, ct);
            dto.Viewer.IsWatching = await db.AppWatches.AnyAsync(w => w.UserId == uid && w.AppId == app.Id, ct);
            dto.Viewer.HasDownloaded = await db.Downloads.AnyAsync(d => d.UserId == uid && d.AppId == app.Id, ct);
            dto.Viewer.MyRatingId = await db.Ratings.Where(r => r.UserId == uid && r.AppId == app.Id).Select(r => (int?)r.Id).FirstOrDefaultAsync(ct);
            dto.Viewer.CanRate = dto.Viewer.HasDownloaded && !isOwner;
            dto.Viewer.CollectionIds = await db.CollectionItems.Where(i => i.AppId == app.Id && i.Collection.UserId == uid).Select(i => i.CollectionId).ToListAsync(ct);
        }
        return dto;
    }

    public async Task<AppDetailDto> MapDetailAsync(App app, bool includeUnpublishedVersions, CancellationToken ct)
    {
        var coefficients = await settings.SavingsAsync(ct);
        var uploaderStats = await db.Apps.AsNoTracking().Where(a => a.UploaderUserId == app.UploaderUserId && a.Status == AppStatus.Published)
            .GroupBy(a => 1).Select(g => new { Count = g.Count(), Downloads = g.Sum(a => a.DownloadCount) }).FirstOrDefaultAsync(ct);
        var versions = app.Versions
            .Where(v => includeUnpublishedVersions || v.Status == VersionStatus.Published)
            .OrderByDescending(v => v.ReleasedAt).ThenByDescending(v => v.Id)
            .Select(v => MapVersion(app.Slug, v)).ToList();
        var latest = versions.FirstOrDefault(v => v.Id == app.LatestVersionId) ?? versions.FirstOrDefault(v => v.Status == VersionStatus.Published);
        var derivatives = await db.Apps.AsNoTracking().Where(a => a.DerivedFromAppId == app.Id && a.Status == AppStatus.Published)
            .Select(a => new LineageNodeDto { Id = a.Id, Slug = a.Slug, Name = a.Name, IconUrl = a.IconStorageKey == null ? null : "/files/icons/" + a.IconStorageKey, DerivationKind = a.DerivationKind })
            .ToListAsync(ct);
        var savedTokens = (await settings.SavingsAsync(ct)).Saved(app.EstGenerationTokens, app.DownloadCount);

        return new AppDetailDto
        {
            Id = app.Id, Slug = app.Slug, Name = app.Name, ShortDescription = app.ShortDescription, LongDescription = app.LongDescription,
            ReadmeMarkdown = app.ReadmeMarkdown, IconUrl = FileUrls.Icon(app.IconStorageKey), HomepageUrl = app.HomepageUrl,
            Status = app.Status, RejectionReason = app.RejectionReason,
            Category = CategoryIndexService.Shallow(new CategoryNodeDto { Id = app.Category.Id, Slug = app.Category.Slug, NameEn = app.Category.NameEn, NameTr = app.Category.NameTr, Icon = app.Category.Icon, Level = app.Category.Level, ParentId = app.Category.ParentId }),
            CategoryPath = await categories.PathAsync(app.CategoryId, ct),
            License = new LicenseDto { Id = app.License.Id, SpdxId = app.License.SpdxId, Name = app.License.Name, Url = app.License.Url, IsOsiApproved = app.License.IsOsiApproved, IsAllowed = app.License.IsAllowed },
            LlmModel = app.LlmModel is null ? null : new LlmModelDto { Id = app.LlmModel.Id, Vendor = app.LlmModel.Vendor, Name = app.LlmModel.Name, Version = app.LlmModel.Version, Slug = app.LlmModel.Slug, DisplayName = app.LlmModel.DisplayName },
            LlmModelNote = app.LlmModelNote,
            Uploader = new UploaderDto
            {
                Id = app.Uploader.Id, Username = app.Uploader.Username, DisplayName = app.Uploader.DisplayName, AvatarUrl = app.Uploader.AvatarUrl, Bio = app.Uploader.Bio,
                Website = app.Uploader.Website, TrustLevel = app.Uploader.TrustLevel, AppCount = uploaderStats?.Count ?? 0, TotalDownloads = uploaderStats?.Downloads ?? 0, MemberSince = app.Uploader.CreatedAt
            },
            SourceKind = app.SourceKind, RepoUrl = app.RepoUrl, RepoProvider = app.RepoProvider?.ToString(), RepoStars = app.RepoStars, RepoPrimaryLanguage = app.RepoPrimaryLanguage, RepoSyncedAt = app.RepoSyncedAt,
            Platforms = (latest?.Files ?? []).Where(f => f.PlatformCode is not null).Select(f => f.PlatformCode!).Distinct().ToList(),
            Tags = app.AppTags.Select(t => t.Tag.Name).OrderBy(t => t).ToList(),
            Screenshots = app.Screenshots.OrderBy(s => s.SortOrder).Select(s => new ScreenshotDto { Id = s.Id, Url = FileUrls.Screenshot(s.StorageKey)!, ThumbUrl = FileUrls.Screenshot(s.ThumbStorageKey)!, Width = s.Width, Height = s.Height, Caption = s.Caption, SortOrder = s.SortOrder }).ToList(),
            Versions = versions, LatestVersion = latest,
            Prompts = app.Prompts.OrderBy(p => p.SortOrder).Select(p => new AppPromptDto { Id = p.Id, Title = p.Title, PromptText = p.PromptText, SortOrder = p.SortOrder }).ToList(),
            DerivedFrom = app.DerivedFrom is null ? null : new LineageNodeDto { Id = app.DerivedFrom.Id, Slug = app.DerivedFrom.Slug, Name = app.DerivedFrom.Name, IconUrl = FileUrls.Icon(app.DerivedFrom.IconStorageKey) },
            DerivationKind = app.DerivationKind, Derivatives = derivatives,
            RatingAvg = app.RatingAvg, RatingCount = app.RatingCount, WorkedCount = app.WorkedCount, NotWorkedCount = app.NotWorkedCount,
            DownloadCount = app.DownloadCount, ViewCount = app.ViewCount, FavoriteCount = app.FavoriteCount, IsFeatured = app.IsFeatured,
            EstGenerationTokens = app.EstGenerationTokens, EstGenerationCostUsd = app.EstGenerationCostUsd,
            EstSavedTokens = savedTokens, EstSavedCostUsd = coefficients.Cost(savedTokens),
            SourceLineCount = app.SourceLineCount, SourceFileCount = app.SourceFileCount,
            PublishedAt = app.PublishedAt, CreatedAt = app.CreatedAt, UpdatedAt = app.UpdatedAt
        };
    }

    public static AppVersionDto MapVersion(string slug, AppVersion v) => new()
    {
        Id = v.Id, Version = v.Version, Changelog = v.Changelog, ReleasedAt = v.ReleasedAt, SourceRef = v.SourceRef, Status = v.Status, RejectionReason = v.RejectionReason, DownloadCount = v.DownloadCount,
        Files = v.Files.OrderBy(f => f.Kind).ThenBy(f => f.Platform == null ? 99 : f.Platform.SortOrder).Select(f => MapFile(slug, f)).ToList()
    };

    public static AppFileDto MapFile(string slug, AppFile f) => new()
    {
        Id = f.Id, VersionId = f.VersionId, PlatformCode = f.Platform?.Code, PlatformName = f.Platform?.Name, Kind = f.Kind, FileName = f.FileName, ExternalReference = f.ExternalReference,
        SizeBytes = f.SizeBytes, Sha256 = f.Sha256, ContentType = f.ContentType, ScanStatus = f.ScanStatus, ScanSignature = f.ScanSignature, InstallHint = f.InstallHint ?? f.Platform?.InstallHint,
        DownloadCount = f.DownloadCount, DownloadUrl = FileUrls.Download(slug, f.Id)
    };

    public Task<List<AppCardDto>> GetSimilarAsync(string slug, CancellationToken ct) => SimilarCore(slug, ct);

    private async Task<List<AppCardDto>> SimilarCore(string slug, CancellationToken ct)
    {
        var id = await db.Apps.AsNoTracking().Where(a => a.Slug == slug).Select(a => (int?)a.Id).FirstOrDefaultAsync(ct) ?? throw ApiException.NotFound("App not found.");
        return await search.SimilarToAppAsync(id, 8, ct);
    }

    public async Task<LineageNodeDto> GetLineageAsync(string slug, CancellationToken ct)
    {
        var app = await db.Apps.AsNoTracking().FirstOrDefaultAsync(a => a.Slug == slug && (a.Status == AppStatus.Published || a.Status == AppStatus.Unlisted), ct) ?? throw ApiException.NotFound("App not found.");
        // climb to the root
        var rootId = app.Id;
        var parentId = app.DerivedFromAppId;
        var guard = 0;
        while (parentId is { } pid && guard++ < 20)
        {
            var parent = await db.Apps.AsNoTracking().Where(a => a.Id == pid && a.Status == AppStatus.Published).Select(a => new { a.Id, a.DerivedFromAppId }).FirstOrDefaultAsync(ct);
            if (parent is null) break;
            rootId = parent.Id;
            parentId = parent.DerivedFromAppId;
        }
        var all = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published || a.Id == app.Id)
            .Select(a => new { a.Id, a.Slug, a.Name, a.IconStorageKey, a.DerivedFromAppId, a.DerivationKind }).ToListAsync(ct);
        var byParent = all.Where(a => a.DerivedFromAppId != null).GroupBy(a => a.DerivedFromAppId!.Value).ToDictionary(g => g.Key, g => g.ToList());
        var root = all.First(a => a.Id == rootId);
        return Build(root.Id, 0);

        LineageNodeDto Build(int id, int depth)
        {
            var a = all.First(x => x.Id == id);
            var node = new LineageNodeDto { Id = a.Id, Slug = a.Slug, Name = a.Name, IconUrl = FileUrls.Icon(a.IconStorageKey), DerivationKind = a.DerivationKind };
            if (depth < 8 && byParent.TryGetValue(id, out var children)) node.Derivatives = children.Select(c => Build(c.Id, depth + 1)).ToList();
            return node;
        }
    }

    // ---------------------------------------------------------------- uploader profile & public collections

    public async Task<UploaderDto> GetUploaderAsync(string username, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant() && u.IsActive, ct) ?? throw ApiException.NotFound("Member not found.");
        var stats = await db.Apps.AsNoTracking().Where(a => a.UploaderUserId == user.Id && a.Status == AppStatus.Published)
            .GroupBy(a => 1).Select(g => new { Count = g.Count(), Downloads = g.Sum(a => a.DownloadCount) }).FirstOrDefaultAsync(ct);
        return new UploaderDto
        {
            Id = user.Id, Username = user.Username, DisplayName = user.DisplayName, AvatarUrl = user.AvatarUrl, Bio = user.Bio, Website = user.Website, TrustLevel = user.TrustLevel,
            AppCount = stats?.Count ?? 0, TotalDownloads = stats?.Downloads ?? 0, MemberSince = user.CreatedAt
        };
    }

    public async Task<List<CollectionDto>> GetPublicCollectionsAsync(string username, CancellationToken ct) =>
        await db.Collections.AsNoTracking().Where(c => c.User.Username == username.ToLowerInvariant() && c.IsPublic)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new CollectionDto { Id = c.Id, Name = c.Name, Slug = c.Slug, Description = c.Description, IsPublic = c.IsPublic, OwnerUsername = c.User.Username, OwnerDisplayName = c.User.DisplayName, ItemCount = c.Items.Count, UpdatedAt = c.UpdatedAt })
            .ToListAsync(ct);

    public async Task<CollectionDto> GetPublicCollectionAsync(string username, string slug, CancellationToken ct)
    {
        var c = await db.Collections.AsNoTracking().Include(c => c.User).FirstOrDefaultAsync(c => c.User.Username == username.ToLowerInvariant() && c.Slug == slug, ct) ?? throw ApiException.NotFound("Collection not found.");
        if (!c.IsPublic && currentUser.IdOrNull != c.UserId) throw ApiException.NotFound("Collection not found.");
        var items = await db.CollectionItems.AsNoTracking().Where(i => i.CollectionId == c.Id && i.App.Status == AppStatus.Published).OrderBy(i => i.SortOrder).Select(i => i.App).Select(AppCardProjection.ToCard).ToListAsync(ct);
        return new CollectionDto { Id = c.Id, Name = c.Name, Slug = c.Slug, Description = c.Description, IsPublic = c.IsPublic, OwnerUsername = c.User.Username, OwnerDisplayName = c.User.DisplayName, ItemCount = items.Count, UpdatedAt = c.UpdatedAt, Items = items };
    }
}

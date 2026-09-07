using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Services.Apps;
using AiAlreadyDidIt.Api.Services.Catalog;
using AiAlreadyDidIt.Api.Services.Site;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Admin;

/// <summary>Admin › Catalog › Apps: every app in every state, with the moderation power actions.</summary>
public sealed class AdminAppsService(AadiDbContext db, ICurrentUser currentUser, AppEditorService editor, AppLifecycleService lifecycle, AuditService audit, JobQueue jobs, CatalogService catalog, SavingsService savings)
{
    public async Task<PagedResult<AdminAppRowDto>> ListAsync(AdminListQuery query, string? category, string? uploader, CancellationToken ct)
    {
        var q = db.Apps.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<AppStatus>(query.Status, true, out var status)) q = q.Where(a => a.Status == status);
        else if (string.IsNullOrWhiteSpace(query.Status)) q = q.Where(a => a.Status != AppStatus.Removed);
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(a => EF.Functions.ILike(a.Name, $"%{query.Q}%") || EF.Functions.ILike(a.Slug, $"%{query.Q}%") || EF.Functions.ILike(a.Uploader.Username, $"%{query.Q}%"));
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(a => a.Category.Slug == category);
        if (!string.IsNullOrWhiteSpace(uploader)) q = q.Where(a => a.Uploader.Username == uploader);
        q = (query.Sort, query.Descending) switch
        {
            ("name", false) => q.OrderBy(a => a.Name), ("name", true) => q.OrderByDescending(a => a.Name),
            ("downloads", false) => q.OrderBy(a => a.DownloadCount), ("downloads", true) => q.OrderByDescending(a => a.DownloadCount),
            ("rating", false) => q.OrderBy(a => a.RatingAvg), ("rating", true) => q.OrderByDescending(a => a.RatingAvg),
            ("created", false) => q.OrderBy(a => a.CreatedAt), ("created", true) => q.OrderByDescending(a => a.CreatedAt),
            (_, false) => q.OrderBy(a => a.UpdatedAt), _ => q.OrderByDescending(a => a.UpdatedAt)
        };
        return await q.Select(a => new AdminAppRowDto
        {
            Id = a.Id, Slug = a.Slug, Name = a.Name, ShortDescription = a.ShortDescription, IconUrl = a.IconStorageKey == null ? null : "/files/icons/" + a.IconStorageKey,
            CoverUrl = a.Screenshots.OrderBy(s => s.SortOrder).Select(s => "/files/screenshots/" + s.ThumbStorageKey).FirstOrDefault(),
            CategorySlug = a.Category.Slug, CategoryNameEn = a.Category.NameEn, CategoryNameTr = a.Category.NameTr, LicenseSpdxId = a.License.SpdxId,
            LlmModelName = a.LlmModel == null ? null : a.LlmModel.Vendor + " " + a.LlmModel.Name, RatingAvg = a.RatingAvg, RatingCount = a.RatingCount, DownloadCount = a.DownloadCount,
            PublishedAt = a.PublishedAt, UpdatedAt = a.UpdatedAt, UploaderUsername = a.Uploader.Username, UploaderEmail = a.Uploader.Email, IsFeatured = a.IsFeatured, Status = a.Status,
            EstGenerationTokens = a.EstGenerationTokens, EstGenerationCostUsd = a.EstGenerationCostUsd, VersionCount = a.Versions.Count, CreatedAt = a.CreatedAt, SubmittedAt = a.SubmittedAt,
            OpenReports = db.Reports.Count(r => r.AppId == a.Id && r.Status == ReportStatus.Open), EmbeddingStale = a.EmbeddingStale, HasEmbedding = a.Embedding != null, ViewCount = a.ViewCount,
            LatestVersion = a.Versions.Where(v => v.Id == a.LatestVersionId).Select(v => v.Version).FirstOrDefault()
        }).ToPagedAsync(query.SafePage, query.SafePageSize, ct);
    }

    public Task<AppDraftDto> GetAsync(int id, CancellationToken ct) => editor.GetDraftAsync(id, ct);

    public async Task<AppDraftDto> UpdateAsync(int id, AdminAppUpdateRequest request, CancellationToken ct)
    {
        var dto = await editor.UpdateDraftAsync(id, request, ct);
        var app = await db.Apps.FirstAsync(a => a.Id == id, ct);
        if (request.IsFeatured is { } f) { app.IsFeatured = f; if (!f) app.FeaturedOrder = 0; }
        if (request.FeaturedOrder is { } order) app.FeaturedOrder = order;
        if (request.FeaturedNote is not null) app.FeaturedNote = string.IsNullOrWhiteSpace(request.FeaturedNote) ? null : request.FeaturedNote.Trim();
        if (request.LicenseVerifiedByAdmin is { } verified) app.LicenseVerifiedByAdmin = verified;
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var slug = TextUtil.Slugify(request.Slug, 120);
            if (slug.Length >= 2 && slug != app.Slug)
            {
                if (await db.Apps.AnyAsync(a => a.Slug == slug && a.Id != id, ct)) throw ApiException.Unprocessable("Slug already in use.", "slug");
                app.Slug = slug;
            }
        }
        app.UpdatedAt = Clock.Now;
        audit.Log("app.update", "app", id, request);
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        return await editor.GetDraftAsync(id, ct);
    }

    public async Task<AppDraftDto> SetStatusAsync(int id, string action, string? note, CancellationToken ct)
    {
        var app = await db.Apps.Include(a => a.Versions).FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw ApiException.NotFound("App not found.");
        ModerationActionKind kind;
        switch (action.ToLowerInvariant())
        {
            case "unlist": app.Status = AppStatus.Unlisted; kind = ModerationActionKind.Unlist; break;
            case "restore":
                if (app.PublishedAt is null) throw ApiException.Unprocessable("This app was never published; approve it from the moderation queue.");
                app.Status = AppStatus.Published; kind = ModerationActionKind.Restore; break;
            case "remove": app.Status = AppStatus.Removed; kind = ModerationActionKind.Remove; break;
            case "feature":
                if (app.Status != AppStatus.Published) throw ApiException.Unprocessable("Only published apps can be featured.");
                if (!app.IsFeatured) app.FeaturedOrder = (await db.Apps.Where(a => a.IsFeatured).Select(a => (int?)a.FeaturedOrder).MaxAsync(ct) ?? 0) + 1;
                app.IsFeatured = true; kind = ModerationActionKind.Feature; break;
            case "unfeature": app.IsFeatured = false; app.FeaturedOrder = 0; kind = ModerationActionKind.Unfeature; break;
            default: throw ApiException.BadRequest("Unknown action.");
        }
        app.UpdatedAt = Clock.Now;
        db.ModerationActions.Add(new ModerationAction { AppId = id, AdminUserId = currentUser.Id, Action = kind, Note = note, CreatedAt = Clock.Now });
        audit.Log("app." + action.ToLowerInvariant(), "app", id, new { note });
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        savings.Invalidate();
        await lifecycle.RecomputeReferenceCountsAsync(ct);
        return await editor.GetDraftAsync(id, ct);
    }

    public async Task RecomputeAsync(int id, CancellationToken ct)
    {
        await lifecycle.RecomputeCountersAsync(id, ct);
        jobs.Enqueue(JobTypes.EmbedApp, new { AppId = id }, $"app:{id}");
        var app = await db.Apps.Include(a => a.AppTags).ThenInclude(t => t.Tag).FirstAsync(a => a.Id == id, ct);
        await lifecycle.RefreshSearchTextAsync(app, ct);
        await lifecycle.RefreshEstimateAsync(app, ct);
        audit.Log("app.recompute", "app", id);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var app = await editor.LoadOwnedAsync(id, ct);
        foreach (var f in app.Versions.SelectMany(v => v.Files)) await editor.DeleteStoredAsync(f, ct);
        db.Apps.Remove(app);
        audit.Log("app.delete", "app", id, new { app.Name, app.Slug });
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        await lifecycle.RecomputeReferenceCountsAsync(ct);
    }
}

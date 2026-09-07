using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Services.Apps;
using AiAlreadyDidIt.Api.Services.Catalog;
using AiAlreadyDidIt.Api.Services.Search;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Admin;

/// <summary>Moderation queue: new apps and new versions waiting for approval.</summary>
public sealed class AdminModerationService(AadiDbContext db, ICurrentUser currentUser, AppEditorService editor, CatalogService catalog, AppLifecycleService lifecycle,
    SearchService search, CategoryIndexService categories, AuditService audit, JobQueue jobs, AdminReportsService reports)
{
    public async Task<PagedResult<ModerationQueueItemDto>> QueueAsync(AdminListQuery query, CancellationToken ct)
    {
        var status = query.Status?.ToLowerInvariant();
        var q = db.Apps.AsNoTracking().Where(a => a.Status != AppStatus.Removed);
        q = status switch
        {
            "review" or null or "" => q.Where(a => a.Status == AppStatus.PendingReview || a.Versions.Any(v => v.Status == VersionStatus.PendingReview)),
            "scan" => q.Where(a => a.Status == AppStatus.PendingScan || a.Versions.Any(v => v.Status == VersionStatus.PendingScan)),
            "rejected" => q.Where(a => a.Status == AppStatus.Rejected || a.Versions.Any(v => v.Status == VersionStatus.Rejected)),
            "all" => q.Where(a => a.Status != AppStatus.Draft),
            _ => q
        };
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(a => EF.Functions.ILike(a.Name, $"%{query.Q}%") || EF.Functions.ILike(a.Uploader.Username, $"%{query.Q}%"));
        var rows = await q.OrderBy(a => a.SubmittedAt ?? a.UpdatedAt).Select(a => new
        {
            a.Id, a.Slug, a.Name, a.ShortDescription, a.IconStorageKey, a.Status, a.SubmittedAt, a.UpdatedAt, a.PublishedAt,
            Cover = a.Screenshots.OrderBy(s => s.SortOrder).Select(s => s.ThumbStorageKey).FirstOrDefault(),
            Uploader = a.Uploader.Username, a.Uploader.TrustLevel, Category = a.Category.NameEn, License = a.License.SpdxId,
            a.HasLicenseFile, a.DetectedLicenseSpdxId, a.LicenseVerifiedByAdmin, LicenseFamily = a.License.Family, a.LicenseId,
            ScreenshotCount = a.Screenshots.Count,
            OpenReports = db.Reports.Count(r => r.AppId == a.Id && r.Status == ReportStatus.Open),
            Versions = a.Versions.Select(v => new { v.Id, v.Version, v.Status, v.CreatedAt, Files = v.Files.Select(f => f.ScanStatus).ToList() }).ToList()
        }).ToPagedAsync(query.SafePage, query.SafePageSize, ct);

        var items = rows.Items.Select(a =>
        {
            var pendingVersion = a.Versions.Where(v => v.Status is VersionStatus.PendingReview or VersionStatus.PendingScan or VersionStatus.Rejected).OrderByDescending(v => v.Id).FirstOrDefault()
                                 ?? a.Versions.OrderByDescending(v => v.Id).First();
            var files = pendingVersion.Files;
            var licenseOk = a.LicenseVerifiedByAdmin || (a.HasLicenseFile && a.DetectedLicenseSpdxId != null && Infrastructure.Import.LicenseDetector.SameFamily(a.License, a.DetectedLicenseSpdxId));
            return new ModerationQueueItemDto
            {
                AppId = a.Id, VersionId = pendingVersion.Id, Slug = a.Slug, Name = a.Name, ShortDescription = a.ShortDescription, IconUrl = FileUrls.Icon(a.IconStorageKey), CoverUrl = FileUrls.Screenshot(a.Cover),
                Kind = a.PublishedAt is null ? "app" : "version", Version = pendingVersion.Version, AppStatus = a.Status, VersionStatus = pendingVersion.Status,
                UploaderUsername = a.Uploader, UploaderTrustLevel = a.TrustLevel, CategoryName = a.Category, LicenseSpdxId = a.License, LicenseOk = licenseOk,
                FileCount = files.Count, InfectedCount = files.Count(s => s == ScanStatus.Infected), PendingScanCount = files.Count(s => s == ScanStatus.Pending),
                ScreenshotCount = a.ScreenshotCount, SubmittedAt = a.SubmittedAt ?? a.UpdatedAt, OpenReports = a.OpenReports
            };
        }).ToList();
        return PagedResult<ModerationQueueItemDto>.Create(items, rows.Page, rows.PageSize, rows.TotalCount);
    }

    public async Task<ModerationDetailDto> DetailAsync(int appId, CancellationToken ct)
    {
        var app = await editor.LoadOwnedAsync(appId, ct); // admin bypasses ownership
        var draft = await editor.MapDraftAsync(app, ct);
        await db.Entry(app).Reference(a => a.Uploader).LoadAsync(ct);
        var preview = await catalog.MapDetailAsync(app, includeUnpublishedVersions: true, ct);
        var fileIds = app.Versions.SelectMany(v => v.Files).Select(f => f.Id).ToList();
        var fileNames = app.Versions.SelectMany(v => v.Files).ToDictionary(f => f.Id, f => f.FileName);
        var scans = await db.ScanResults.AsNoTracking().Where(s => fileIds.Contains(s.FileId)).OrderByDescending(s => s.ScannedAt).Take(50).ToListAsync(ct);
        var history = await db.ModerationActions.AsNoTracking().Where(m => m.AppId == appId).OrderByDescending(m => m.CreatedAt)
            .Join(db.Users, m => m.AdminUserId, u => u.Id, (m, u) => new ModerationActionDto { Id = m.Id, VersionId = m.VersionId, AdminUsername = u.Username, Action = m.Action, Note = m.Note, CreatedAt = m.CreatedAt }).ToListAsync(ct);
        var similar = await search.NearestAsync(EmbeddingService.BuildAppText(app), 6, app.Id, 0.4, ct);
        var reportRows = await db.Reports.AsNoTracking().Where(r => r.AppId == appId).OrderByDescending(r => r.CreatedAt).Take(20).Select(reports.Projection()).ToListAsync(ct);
        var suggestedCategory = app.LlmSuggestedCategoryId is { } sid ? await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == sid, ct) : null;
        var licenseCheck = await lifecycle.CheckLicenseAsync(app, ct);
        var uploaderApps = await db.Apps.AsNoTracking().Where(a => a.UploaderUserId == app.UploaderUserId).Select(a => a.Status).ToListAsync(ct);
        return new ModerationDetailDto
        {
            Draft = draft, Preview = preview, History = history,
            ScanResults = scans.Select(s => new ScanResultDto { Id = s.Id, FileId = s.FileId, FileName = fileNames.GetValueOrDefault(s.FileId, "?"), Engine = s.Engine, Verdict = s.Verdict, Signature = s.Signature, Raw = s.Raw, ScannedAt = s.ScannedAt }).ToList(),
            SimilarApps = similar, Reports = reportRows,
            LlmSuggestedCategory = suggestedCategory is null ? null : new CategoryNodeDto { Id = suggestedCategory.Id, Slug = suggestedCategory.Slug, NameEn = suggestedCategory.NameEn, NameTr = suggestedCategory.NameTr, Level = suggestedCategory.Level, ParentId = suggestedCategory.ParentId },
            LlmSuggestedCategoryProposedName = suggestedCategory is { IsLlmProposed: true } ? suggestedCategory.NameEn : null,
            LicenseIssue = licenseCheck.Ok ? null : licenseCheck.Message,
            Uploader = new AdminUserSummaryDto
            {
                Id = app.Uploader.Id, Username = app.Uploader.Username, DisplayName = app.Uploader.DisplayName, Email = app.Uploader.Email, EmailVerified = app.Uploader.EmailVerifiedAt != null,
                Role = currentUser.IsAdmin ? "" : "", TrustLevel = app.Uploader.TrustLevel, IsBanned = app.Uploader.IsBanned, IsActive = app.Uploader.IsActive,
                AppCount = uploaderApps.Count, PublishedAppCount = uploaderApps.Count(s => s == AppStatus.Published), RejectedAppCount = uploaderApps.Count(s => s == AppStatus.Rejected),
                ReportCount = await db.Reports.CountAsync(r => r.App!.UploaderUserId == app.UploaderUserId, ct), CreatedAt = app.Uploader.CreatedAt, LastLoginAt = app.Uploader.LastLoginAt
            }
        };
    }

    public async Task<ModerationDetailDto> ApproveAsync(int appId, ModerationDecisionRequest request, CancellationToken ct)
    {
        var app = await db.Apps.Include(a => a.License).Include(a => a.Versions).ThenInclude(v => v.Files).FirstOrDefaultAsync(a => a.Id == appId, ct) ?? throw ApiException.NotFound("App not found.");
        var version = request.VersionId is { } vid ? app.Versions.FirstOrDefault(v => v.Id == vid) : app.Versions.Where(v => v.Status is VersionStatus.PendingReview or VersionStatus.PendingScan).OrderByDescending(v => v.Id).FirstOrDefault();
        if (version is null) throw ApiException.Unprocessable("No version is waiting for review.");
        if (version.Files.Any(f => f.ScanStatus == ScanStatus.Infected)) throw ApiException.Unprocessable("A file is flagged as infected; it must be removed first.");
        if (version.Files.Any(f => f.StorageKey != null && f.ScanStatus == ScanStatus.Pending)) throw ApiException.Unprocessable("Files are still waiting for the antivirus scan.");
        var license = await lifecycle.CheckLicenseAsync(app, ct);
        if (!license.Ok) throw ApiException.Unprocessable("License check failed: " + license.Message + " Use 'verify license' to override.");
        if (request.CategoryId is { } cid)
        {
            var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == cid, ct) ?? throw ApiException.Unprocessable("Category not found.", "categoryId");
            if (cat.IsLlmProposed) { cat.IsLlmProposed = false; cat.IsActive = true; }
            app.CategoryId = cid;
            await lifecycle.RefreshSearchTextAsync(app, ct);
        }
        if (request.Feature && !app.IsFeatured) { app.IsFeatured = true; app.FeaturedOrder = (await db.Apps.Where(a => a.IsFeatured).Select(a => (int?)a.FeaturedOrder).MaxAsync(ct) ?? 0) + 1; }
        await lifecycle.PublishAsync(app, version, notifyUploader: true, ct);
        db.ModerationActions.Add(new ModerationAction { AppId = app.Id, VersionId = version.Id, AdminUserId = currentUser.Id, Action = app.Versions.Count(v => v.Status == VersionStatus.Published) > 1 ? ModerationActionKind.ApproveVersion : ModerationActionKind.Approve, Note = request.Note, CreatedAt = Clock.Now });
        audit.Log("moderation.approve", "app", app.Id, new { version.Version, request.Note });
        await db.SaveChangesAsync(ct);
        return await DetailAsync(appId, ct);
    }

    public async Task<ModerationDetailDto> RejectAsync(int appId, ModerationDecisionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Note)) throw ApiException.Unprocessable("Tell the uploader why (note).", "note");
        var app = await db.Apps.Include(a => a.Versions).FirstOrDefaultAsync(a => a.Id == appId, ct) ?? throw ApiException.NotFound("App not found.");
        var version = request.VersionId is { } vid ? app.Versions.FirstOrDefault(v => v.Id == vid) : app.Versions.Where(v => v.Status is VersionStatus.PendingReview or VersionStatus.PendingScan).OrderByDescending(v => v.Id).FirstOrDefault();
        await lifecycle.RejectAsync(app, version, request.Note.Trim(), currentUser.Id, ct);
        audit.Log("moderation.reject", "app", app.Id, new { request.Note });
        await db.SaveChangesAsync(ct);
        return await DetailAsync(appId, ct);
    }

    public async Task<ModerationDetailDto> RescanAsync(int appId, int? versionId, CancellationToken ct)
    {
        var app = await db.Apps.Include(a => a.Versions).ThenInclude(v => v.Files).FirstOrDefaultAsync(a => a.Id == appId, ct) ?? throw ApiException.NotFound("App not found.");
        var version = versionId is { } vid ? app.Versions.FirstOrDefault(v => v.Id == vid) : app.Versions.OrderByDescending(v => v.Id).First();
        if (version is null) throw ApiException.NotFound("Version not found.");
        foreach (var f in version.Files.Where(f => f.StorageKey != null)) f.ScanStatus = ScanStatus.Pending;
        if (version.Status is VersionStatus.PendingReview or VersionStatus.Rejected or VersionStatus.Draft) version.Status = VersionStatus.PendingScan;
        if (app.Status is AppStatus.PendingReview or AppStatus.Rejected) app.Status = AppStatus.PendingScan;
        jobs.Enqueue(JobTypes.ScanVersion, new { AppId = app.Id, VersionId = version.Id }, $"version:{version.Id}");
        db.ModerationActions.Add(new ModerationAction { AppId = app.Id, VersionId = version.Id, AdminUserId = currentUser.Id, Action = ModerationActionKind.Rescan, CreatedAt = Clock.Now });
        audit.Log("moderation.rescan", "app", app.Id, new { version.Version });
        await db.SaveChangesAsync(ct);
        return await DetailAsync(appId, ct);
    }

    public async Task<ModerationDetailDto> VerifyLicenseAsync(int appId, ModerationDecisionRequest request, CancellationToken ct)
    {
        var app = await db.Apps.FirstOrDefaultAsync(a => a.Id == appId, ct) ?? throw ApiException.NotFound("App not found.");
        app.LicenseVerifiedByAdmin = true;
        db.ModerationActions.Add(new ModerationAction { AppId = app.Id, AdminUserId = currentUser.Id, Action = ModerationActionKind.OverrideLicense, Note = request.Note, CreatedAt = Clock.Now });
        audit.Log("moderation.verify_license", "app", app.Id, new { request.Note });
        await db.SaveChangesAsync(ct);
        return await DetailAsync(appId, ct);
    }

    public async Task<ModerationDetailDto> NoteAsync(int appId, ModerationDecisionRequest request, CancellationToken ct)
    {
        db.ModerationActions.Add(new ModerationAction { AppId = appId, VersionId = request.VersionId, AdminUserId = currentUser.Id, Action = ModerationActionKind.Note, Note = request.Note, CreatedAt = Clock.Now });
        await db.SaveChangesAsync(ct);
        return await DetailAsync(appId, ct);
    }
}

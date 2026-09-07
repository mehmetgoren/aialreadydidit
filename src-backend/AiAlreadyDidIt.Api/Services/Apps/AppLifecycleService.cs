using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Import;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Services.Catalog;
using AiAlreadyDidIt.Api.Services.Site;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Apps;

/// <summary>
/// State transitions and derived data shared by the editor, the moderation panel and background jobs:
/// publishing, counters, search text, license verification, savings estimate.
/// </summary>
public sealed class AppLifecycleService(AadiDbContext db, CategoryIndexService categories, SiteSettingsCache settings, JobQueue jobs,
    NotificationService notifications, SavingsService savings, CatalogService catalog)
{
    /// <summary>Keeps <c>tags_text</c> / <c>category_path_text</c> in sync (they feed the generated tsvector) and flags the embedding stale.</summary>
    public async Task RefreshSearchTextAsync(App app, CancellationToken ct)
    {
        await db.Entry(app).Collection(a => a.AppTags).Query().Include(t => t.Tag).LoadAsync(ct);
        app.TagsText = TextUtil.Truncate(string.Join(", ", app.AppTags.Select(t => t.Tag.Name)), 1000);
        app.CategoryPathText = TextUtil.Truncate(await categories.PathTextAsync(app.CategoryId, ct), 400);
        app.EmbeddingStale = true;
    }

    /// <summary>Recomputes the heuristic token / cost estimate unless the uploader supplied real numbers.</summary>
    public async Task RefreshEstimateAsync(App app, CancellationToken ct)
    {
        if (app.EstIsOverride) return;
        var c = await settings.SavingsAsync(ct);
        app.EstGenerationTokens = c.EstimateTokens(app.SourceLineCount);
        app.EstGenerationCostUsd = c.Cost(app.EstGenerationTokens);
    }

    public sealed record LicenseCheck(bool Ok, string? Message);

    /// <summary>Golden rule 1: declared SPDX must be allowed and the source must carry a matching LICENSE file.</summary>
    public async Task<LicenseCheck> CheckLicenseAsync(App app, CancellationToken ct)
    {
        var license = app.License ?? await db.Licenses.FirstOrDefaultAsync(l => l.Id == app.LicenseId, ct);
        if (license is null) return new LicenseCheck(false, "Choose a license.");
        if (!license.IsAllowed) return new LicenseCheck(false, $"{license.SpdxId} is not an open-source license accepted by this store.");
        if (app.LicenseVerifiedByAdmin) return new LicenseCheck(true, null);
        if (app.SourceAnalyzedAt is null) return new LicenseCheck(false, "The source has not been analysed yet.");
        if (!app.HasLicenseFile) return new LicenseCheck(false, "No LICENSE / COPYING file was found at the root of the source. Add one that matches the declared license.");
        if (app.DetectedLicenseSpdxId is null) return new LicenseCheck(false, $"The LICENSE file text was not recognised. It must contain the standard {license.SpdxId} text.");
        if (!LicenseDetector.SameFamily(license.SpdxId, app.DetectedLicenseSpdxId))
            return new LicenseCheck(false, $"The LICENSE file looks like {app.DetectedLicenseSpdxId} but the declared license is {license.SpdxId}. Fix one of them.");
        return new LicenseCheck(true, null);
    }

    /// <summary>Publishes the app (first time or after review) and its reviewed version. Caller saves.</summary>
    public async Task PublishAsync(App app, AppVersion version, bool notifyUploader, CancellationToken ct)
    {
        var first = app.PublishedAt is null;
        version.Status = VersionStatus.Published;
        version.PublishedAt = Clock.Now;
        app.Status = AppStatus.Published;
        app.PublishedAt ??= Clock.Now;
        app.RejectionReason = null;
        app.LatestVersionId = version.Id;
        app.UpdatedAt = Clock.Now;
        app.EmbeddingStale = true;
        jobs.Enqueue(JobTypes.EmbedApp, new { AppId = app.Id }, $"app:{app.Id}");
        jobs.Enqueue(JobTypes.RecomputeStats, new { AppId = app.Id }, $"app:{app.Id}");
        if (!first) jobs.Enqueue(JobTypes.NotifyWatchers, new { AppId = app.Id, VersionId = version.Id }, $"app:{app.Id}");
        if (notifyUploader)
        {
            var uploader = await db.Users.AsNoTracking().FirstAsync(u => u.Id == app.UploaderUserId, ct);
            notifications.Notify(app.UploaderUserId, NotificationType.ModerationApproved,
                first ? $"“{app.Name}” is now published" : $"“{app.Name}” {version.Version} is now published",
                first ? "Your app passed review and is live in the store. Thank you for sharing it!" : "The new version passed review and is live.",
                $"/app/{app.Slug}", email: true, emailAddress: uploader.Email);
        }
        // fulfil matching "wanted" requests is left to the admin / uploader (explicit action)
        catalog.InvalidateHome();
        savings.Invalidate();
        categories.Invalidate();
    }

    public async Task RejectAsync(App app, AppVersion? version, string reason, int adminUserId, CancellationToken ct)
    {
        if (version is not null && app.PublishedAt is not null)
        {
            version.Status = VersionStatus.Rejected;
            version.RejectionReason = reason;
        }
        else
        {
            app.Status = AppStatus.Rejected;
            app.RejectionReason = reason;
            if (version is not null) { version.Status = VersionStatus.Rejected; version.RejectionReason = reason; }
        }
        app.UpdatedAt = Clock.Now;
        db.ModerationActions.Add(new ModerationAction { AppId = app.Id, VersionId = version?.Id, AdminUserId = adminUserId, Action = version is not null && app.PublishedAt is not null ? ModerationActionKind.RejectVersion : ModerationActionKind.Reject, Note = reason, CreatedAt = Clock.Now });
        var uploader = await db.Users.AsNoTracking().FirstAsync(u => u.Id == app.UploaderUserId, ct);
        notifications.Notify(app.UploaderUserId, NotificationType.ModerationRejected, $"“{app.Name}” was not approved", reason, $"/dashboard/apps/{app.Id}", email: true, emailAddress: uploader.Email);
    }

    /// <summary>Called by the scan job when every file of the version is clean.</summary>
    public async Task OnScanCleanAsync(App app, AppVersion version, CancellationToken ct)
    {
        var uploader = await db.Users.AsNoTracking().FirstAsync(u => u.Id == app.UploaderUserId, ct);
        var requireReview = app.PublishedAt is null
            ? await settings.GetBoolAsync(SettingKeys.RequireReviewForNewApps, true, ct)
            : await settings.GetBoolAsync(SettingKeys.RequireReviewForNewVersions, true, ct) && uploader.TrustLevel < 1;
        if (requireReview)
        {
            version.Status = VersionStatus.PendingReview;
            if (app.PublishedAt is null) app.Status = AppStatus.PendingReview;
        }
        else
        {
            await PublishAsync(app, version, notifyUploader: true, ct);
        }
    }

    public async Task OnScanInfectedAsync(App app, AppVersion version, AppFile file, CancellationToken ct)
    {
        var autoReject = await settings.GetBoolAsync(SettingKeys.AutoRejectInfected, true, ct);
        var reason = $"Antivirus flagged {file.FileName}: {file.ScanSignature ?? "malware"}.";
        var uploader = await db.Users.AsNoTracking().FirstAsync(u => u.Id == app.UploaderUserId, ct);
        if (autoReject)
        {
            version.Status = VersionStatus.Rejected;
            version.RejectionReason = reason;
            if (app.PublishedAt is null) { app.Status = AppStatus.Rejected; app.RejectionReason = reason; }
        }
        else
        {
            version.Status = VersionStatus.PendingReview;
            if (app.PublishedAt is null) app.Status = AppStatus.PendingReview;
        }
        db.Reports.Add(new Report { AppId = app.Id, Reason = ReportReason.Malware, Details = reason + " (automatic ClamAV report)", Status = ReportStatus.Open, CreatedAt = Clock.Now });
        notifications.Notify(app.UploaderUserId, NotificationType.ScanInfected, $"“{app.Name}”: antivirus scan failed", reason, $"/dashboard/apps/{app.Id}", email: true, emailAddress: uploader.Email);
    }

    /// <summary>Recomputes cached counters (ratings, downloads, favourites) and reference-list counts for one app.</summary>
    public async Task RecomputeCountersAsync(int appId, CancellationToken ct)
    {
        var app = await db.Apps.FirstOrDefaultAsync(a => a.Id == appId, ct);
        if (app is null) return;
        var ratings = await db.Ratings.Where(r => r.AppId == appId && r.Status == RatingStatus.Visible).Select(r => new { r.Score, r.Worked }).ToListAsync(ct);
        app.RatingCount = ratings.Count;
        app.RatingAvg = ratings.Count == 0 ? 0 : Math.Round(ratings.Average(r => r.Score), 1);
        app.WorkedCount = ratings.Count(r => r.Worked == true);
        app.NotWorkedCount = ratings.Count(r => r.Worked == false);
        app.DownloadCount = await db.Downloads.CountAsync(d => d.AppId == appId, ct);
        app.FavoriteCount = await db.Favorites.CountAsync(f => f.AppId == appId, ct);
        foreach (var v in await db.AppVersions.Where(v => v.AppId == appId).ToListAsync(ct))
            v.DownloadCount = await db.Downloads.CountAsync(d => d.VersionId == v.Id, ct);
        foreach (var f in await db.AppFiles.Where(f => f.Version.AppId == appId).ToListAsync(ct))
            f.DownloadCount = await db.Downloads.CountAsync(d => d.FileId == f.Id, ct);
        await db.SaveChangesAsync(ct);
        await RecomputeReferenceCountsAsync(ct);
    }

    public async Task RecomputeReferenceCountsAsync(CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("UPDATE categories c SET app_count = (SELECT count(*) FROM apps a WHERE a.category_id = c.id AND a.status = 3)", ct);
        await db.Database.ExecuteSqlRawAsync("UPDATE licenses l SET app_count = (SELECT count(*) FROM apps a WHERE a.license_id = l.id AND a.status = 3)", ct);
        await db.Database.ExecuteSqlRawAsync("UPDATE llm_models m SET app_count = (SELECT count(*) FROM apps a WHERE a.llm_model_id = m.id AND a.status = 3)", ct);
        await db.Database.ExecuteSqlRawAsync("UPDATE tags t SET usage_count = (SELECT count(*) FROM app_tags at JOIN apps a ON a.id = at.app_id WHERE at.tag_id = t.id AND a.status = 3)", ct);
        categories.Invalidate();
    }

    /// <summary>Applies a <see cref="SourceAnalysis"/> to the app row (called after archive upload / repository import).</summary>
    public async Task ApplyAnalysisAsync(App app, SourceAnalysis analysis, CancellationToken ct)
    {
        app.SourceFileCount = analysis.SourceFileCount;
        app.SourceLineCount = analysis.LineCount;
        app.SourceBytes = analysis.TotalBytes;
        app.SourceAnalyzedAt = Clock.Now;
        app.HasLicenseFile = analysis.LicenseText is not null;
        app.DetectedLicenseSpdxId = analysis.DetectedLicense;
        app.SourcePrimaryLanguage = analysis.PrimaryLanguage;
        app.SourceWarnings = analysis.Warnings.Count == 0 ? null : TextUtil.Truncate(string.Join(" ", analysis.Warnings), 2000);
        if (string.IsNullOrWhiteSpace(app.ReadmeMarkdown) && analysis.ReadmeMarkdown is not null) app.ReadmeMarkdown = analysis.ReadmeMarkdown;
        if (app.RepoPrimaryLanguage is null && analysis.PrimaryLanguage is not null) app.RepoPrimaryLanguage = analysis.PrimaryLanguage;
        await RefreshEstimateAsync(app, ct);
    }
}

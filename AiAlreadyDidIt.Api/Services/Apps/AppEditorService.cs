using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Import;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using AiAlreadyDidIt.Api.Services.Catalog;
using AiAlreadyDidIt.Api.Services.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Services.Apps;

/// <summary>
/// The upload wizard and "my apps" editor: drafts, source (repository import or archive), installer files per platform,
/// screenshots, prompts, tags, duplicate check, submission and new versions. Every method checks ownership.
/// </summary>
public sealed partial class AppEditorService(AadiDbContext db, ICurrentUser currentUser, IObjectStorage storage, IOptions<StorageOptions> storageOptions,
    IOptions<SiteOptions> siteOptions, IEnumerable<IRepositoryImporter> importers, IHttpClientFactory httpClientFactory, CategoryIndexService categories,
    AppLifecycleService lifecycle, SearchService search, EmbeddingService embeddings, SiteSettingsCache settings, JobQueue jobs, CatalogService catalog,
    IHostEnvironment env, ILogger<AppEditorService> logger)
{
    // ---------------------------------------------------------------- my apps

    public async Task<List<AppCardDto>> ListMineAsync(CancellationToken ct) =>
        await db.Apps.AsNoTracking().Where(a => a.UploaderUserId == currentUser.Id && a.Status != AppStatus.Removed)
            .OrderByDescending(a => a.UpdatedAt).Select(AppCardProjection.ToCard).ToListAsync(ct);

    public async Task<AppDraftDto> CreateDraftAsync(SaveDraftRequest? request, CancellationToken ct)
    {
        await EnsureCanUploadAsync(ct);
        var maxDrafts = await settings.GetIntAsync(SettingKeys.MaxDraftsPerUser, 10, ct);
        var drafts = await db.Apps.CountAsync(a => a.UploaderUserId == currentUser.Id && a.Status == AppStatus.Draft, ct);
        if (drafts >= maxDrafts) throw ApiException.Unprocessable($"You already have {drafts} unsubmitted drafts. Finish or delete one first.");

        var fallbackCategory = await db.Categories.Where(c => c.Slug == "other-uncategorised" || c.Slug == "other").OrderByDescending(c => c.Level).FirstAsync(ct);
        var fallbackLicense = await db.Licenses.FirstAsync(l => l.SpdxId == "MIT", ct);
        var name = string.IsNullOrWhiteSpace(request?.Name) ? "Untitled app" : request!.Name.Trim();
        var app = new App
        {
            Name = name,
            Slug = await UniqueSlugAsync(name, null, ct),
            ShortDescription = string.Empty,
            LongDescription = string.Empty,
            CategoryId = fallbackCategory.Id,
            LicenseId = fallbackLicense.Id,
            UploaderUserId = currentUser.Id,
            SourceKind = SourceKind.Archive,
            Status = AppStatus.Draft,
            TagsText = string.Empty,
            CategoryPathText = string.Empty,
            CreatedAt = Clock.Now,
            UpdatedAt = Clock.Now
        };
        app.Versions.Add(new AppVersion { Version = "1.0.0", ReleasedAt = Clock.Now, Status = VersionStatus.Draft, CreatedByUserId = currentUser.Id, CreatedAt = Clock.Now });
        db.Apps.Add(app);
        await db.SaveChangesAsync(ct);
        if (request is not null) await ApplyDraftAsync(app, request, ct);
        return await GetDraftAsync(app.Id, ct);
    }

    public async Task<AppDraftDto> GetDraftAsync(int id, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        return await MapDraftAsync(app, ct);
    }

    public async Task<AppDraftDto> UpdateDraftAsync(int id, SaveDraftRequest request, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        await ApplyDraftAsync(app, request, ct);
        return await MapDraftAsync(app, ct);
    }

    private async Task ApplyDraftAsync(App app, SaveDraftRequest r, CancellationToken ct)
    {
        var bag = new ValidationBag();
        if (r.Name is not null)
        {
            var name = r.Name.Trim();
            bag.Require(name.Length is >= 3 and <= 120, "name", "Name must be 3-120 characters.");
            if (name.Length >= 3 && name != app.Name)
            {
                app.Name = name;
                if (app.PublishedAt is null) app.Slug = await UniqueSlugAsync(name, app.Id, ct);
            }
        }
        if (r.ShortDescription is not null) { app.ShortDescription = r.ShortDescription.Trim(); bag.Require(app.ShortDescription.Length <= 200, "shortDescription", "Short description must be at most 200 characters."); }
        if (r.LongDescription is not null) app.LongDescription = r.LongDescription.Trim();
        if (r.CategoryId is { } cid)
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == cid && c.IsActive, ct);
            if (category is null) bag.Add("categoryId", "Category not found."); else app.CategoryId = cid;
        }
        if (r.LicenseId is { } lid)
        {
            var license = await db.Licenses.FirstOrDefaultAsync(l => l.Id == lid, ct);
            if (license is null) bag.Add("licenseId", "License not found.");
            else if (!license.IsAllowed) bag.Add("licenseId", $"{license.SpdxId} is not an accepted open-source license.");
            else { app.LicenseId = lid; app.License = license; }
        }
        if (r.LlmModelId is { } mid)
        {
            if (!await db.LlmModels.AnyAsync(m => m.Id == mid && m.IsActive, ct)) bag.Add("llmModelId", "Model not found."); else app.LlmModelId = mid;
        }
        if (r.LlmModelNote is not null) app.LlmModelNote = TextUtil.Truncate(r.LlmModelNote.Trim(), 200);
        if (r.HomepageUrl is not null)
        {
            var url = r.HomepageUrl.Trim();
            if (url.Length > 0 && !Uri.TryCreate(url, UriKind.Absolute, out _)) bag.Add("homepageUrl", "Homepage must be an absolute URL.");
            app.HomepageUrl = url.Length == 0 ? null : url;
        }
        if (r.DerivedFromAppId is not null || r.DerivationKind is not null)
        {
            if (r.DerivedFromAppId is { } parentId && parentId > 0)
            {
                if (parentId == app.Id) bag.Add("derivedFromAppId", "An app cannot derive from itself.");
                else if (!await db.Apps.AnyAsync(a => a.Id == parentId && a.Status == AppStatus.Published, ct)) bag.Add("derivedFromAppId", "Parent app not found.");
                else { app.DerivedFromAppId = parentId; app.DerivationKind = r.DerivationKind ?? DerivationKind.Fork; }
            }
            else { app.DerivedFromAppId = null; app.DerivationKind = null; }
        }
        if (r.EstGenerationTokens is { } tokens)
        {
            if (tokens <= 0) { app.EstIsOverride = false; }
            else
            {
                var c = await settings.SavingsAsync(ct);
                app.EstIsOverride = true;
                app.EstGenerationTokens = tokens;
                app.EstGenerationCostUsd = r.EstGenerationCostUsd is { } cost && cost > 0 ? cost : c.Cost(tokens);
            }
            await lifecycle.RefreshEstimateAsync(app, ct);
        }
        bag.ThrowIfAny();

        if (r.Tags is not null) await SetTagsAsync(app, r.Tags, ct);
        if (r.Prompts is not null)
        {
            await db.Entry(app).Collection(a => a.Prompts).LoadAsync(ct);
            db.AppPrompts.RemoveRange(app.Prompts);
            var order = 0;
            foreach (var p in r.Prompts.Where(p => !string.IsNullOrWhiteSpace(p.PromptText)))
                app.Prompts.Add(new AppPrompt { Title = string.IsNullOrWhiteSpace(p.Title) ? $"Prompt {order + 1}" : TextUtil.Truncate(p.Title.Trim(), 160), PromptText = p.PromptText.Trim(), SortOrder = order++ });
        }
        await lifecycle.RefreshSearchTextAsync(app, ct);
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        if (app.Status == AppStatus.Published) jobs.Enqueue(JobTypes.EmbedApp, new { AppId = app.Id }, $"app:{app.Id}");
        await db.SaveChangesAsync(ct);
    }

    private async Task SetTagsAsync(App app, List<string> tags, CancellationToken ct)
    {
        var max = await settings.GetIntAsync(SettingKeys.MaxTagsPerApp, 10, ct);
        var wanted = tags.Select(t => t.Trim()).Where(t => t.Length is >= 2 and <= 48).Select(t => (Name: t, Slug: TextUtil.Slugify(t, 48)))
            .Where(t => t.Slug.Length >= 2).DistinctBy(t => t.Slug).Take(max).ToList();
        await db.Entry(app).Collection(a => a.AppTags).LoadAsync(ct);
        db.AppTags.RemoveRange(app.AppTags);
        app.AppTags.Clear();
        var slugs = wanted.Select(w => w.Slug).ToList();
        var existing = await db.Tags.Where(t => slugs.Contains(t.Slug)).ToListAsync(ct);
        foreach (var w in wanted)
        {
            var tag = existing.FirstOrDefault(t => t.Slug == w.Slug);
            if (tag is null) { tag = new Tag { Name = w.Name, Slug = w.Slug }; db.Tags.Add(tag); existing.Add(tag); }
            if (tag.IsBlocked) continue;
            app.AppTags.Add(new AppTag { App = app, Tag = tag });
        }
    }

    public async Task DeleteDraftAsync(int id, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        if (app.PublishedAt is not null) throw ApiException.Unprocessable("Published apps cannot be deleted. You can unlist them instead.");
        foreach (var f in app.Versions.SelectMany(v => v.Files)) await DeleteStoredAsync(f, ct);
        foreach (var s in app.Screenshots) { await storage.DeleteAsync(Bucket.Screenshots, s.StorageKey, ct); await storage.DeleteAsync(Bucket.Screenshots, s.ThumbStorageKey, ct); }
        if (app.IconStorageKey is not null) await storage.DeleteAsync(Bucket.Icons, app.IconStorageKey, ct);
        db.Apps.Remove(app);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AppDraftDto> SetListedAsync(int id, bool listed, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        if (app.PublishedAt is null) throw ApiException.Unprocessable("Only published apps can be listed / unlisted.");
        if (app.Status is not (AppStatus.Published or AppStatus.Unlisted)) throw ApiException.Unprocessable("This app cannot be changed right now.");
        app.Status = listed ? AppStatus.Published : AppStatus.Unlisted;
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        categories.Invalidate();
        return await MapDraftAsync(app, ct);
    }

    // ---------------------------------------------------------------- submission

    public async Task<ReadinessDto> GetReadinessAsync(App app, AppVersion version, CancellationToken ct)
    {
        var issues = new List<ReadinessIssueDto>();
        void Issue(string step, string code, string message, bool blocking = true) => issues.Add(new ReadinessIssueDto { Step = step, Code = code, Message = message, Blocking = blocking });

        // details
        if (app.Name.Length < 3 || app.Name == "Untitled app") Issue("details", "name", "Give the app a name.");
        if (app.ShortDescription.Length < 10) Issue("details", "short_description", "Write a short description (10-200 characters).");
        if (app.LongDescription.Length < 50) Issue("details", "long_description", "Write a longer description (at least 50 characters) — this is what search and other LLMs read.");
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == app.CategoryId, ct);
        if (category is null || category.Slug.StartsWith("other")) Issue("details", "category", "Pick a specific category.", blocking: category is null);
        if (app.LlmModelId is null) Issue("details", "llm_model", "Tell us which LLM generated the app.");
        await db.Entry(app).Collection(a => a.Prompts).LoadAsync(ct);
        if (!app.Prompts.Any(p => p.PromptText.Length >= 20)) Issue("details", "prompts", "Add the original prompt(s) you used (at least 20 characters) so others can make their own variant.");
        var maxTags = await settings.GetIntAsync(SettingKeys.MaxTagsPerApp, 10, ct);
        await db.Entry(app).Collection(a => a.AppTags).LoadAsync(ct);
        if (app.AppTags.Count == 0) Issue("details", "tags", "Add a few tags (they improve search).", blocking: false);

        // source (exactly one of repository / archive)
        var sourceFile = version.Files.FirstOrDefault(f => f.Kind == FileKind.Source);
        if (app.SourceKind == SourceKind.Repository)
        {
            if (string.IsNullOrEmpty(app.RepoUrl)) Issue("source", "repo", "Attach the public Git repository.");
            else if (sourceFile is null) Issue("source", "repo_import", app.SourceWarnings?.StartsWith("Import failed") == true ? app.SourceWarnings : "The repository snapshot is still being imported. Wait a moment and refresh.");
        }
        else if (sourceFile is null) Issue("source", "archive", "Upload the source code archive (.zip / .tar.gz) or attach a repository.");
        if (sourceFile is not null && app.SourceAnalyzedAt is not null && app.SourceFileCount == 0) Issue("source", "no_source_files", "No source files were found in the source — the archive must contain readable source code.");
        var licenseCheck = await lifecycle.CheckLicenseAsync(app, ct);
        if (!licenseCheck.Ok && (sourceFile is not null || app.LicenseId == 0)) Issue("source", "license", licenseCheck.Message!);

        // files: one install file per declared platform
        var installers = version.Files.Where(f => f.Kind != FileKind.Source).ToList();
        if (installers.Count == 0) Issue("files", "installer", "Add at least one ready-to-run install file (or Docker image / web reference) for a platform.");
        if (installers.Any(f => f.PlatformId is null)) Issue("files", "installer_platform", "Every install file must be assigned to a platform.");
        if (installers.Any(f => f.ScanStatus == ScanStatus.Infected)) Issue("files", "infected", "A file was flagged by the antivirus scan. Remove it.");

        // screenshots
        var min = await settings.GetIntAsync(SettingKeys.MinScreenshots, 1, ct);
        var recommended = await settings.GetIntAsync(SettingKeys.RecommendedScreenshots, 3, ct);
        await db.Entry(app).Collection(a => a.Screenshots).LoadAsync(ct);
        if (app.Screenshots.Count < min) Issue("screenshots", "screenshots", $"Upload at least {min} screenshot{(min == 1 ? "" : "s")}.");
        else if (app.Screenshots.Count < recommended) Issue("screenshots", "screenshots_recommended", $"{recommended}+ screenshots are recommended.", blocking: false);

        return new ReadinessDto { CanSubmit = issues.All(i => !i.Blocking), Issues = issues };
    }

    public async Task<AppDraftDto> SubmitAsync(int id, CancellationToken ct)
    {
        await EnsureCanUploadAsync(ct);
        var app = await LoadOwnedAsync(id, ct);
        if (app.Status is not (AppStatus.Draft or AppStatus.Rejected)) throw ApiException.Unprocessable("This app has already been submitted.");
        var version = app.Versions.OrderByDescending(v => v.Id).FirstOrDefault(v => v.Status is VersionStatus.Draft or VersionStatus.Rejected)
                      ?? throw ApiException.Unprocessable("No draft version to submit.");
        var readiness = await GetReadinessAsync(app, version, ct);
        if (!readiness.CanSubmit) throw ApiException.Unprocessable(readiness.Issues.Where(i => i.Blocking).Select(i => ApiErrors.Unprocessable(i.Message, i.Code)));

        app.Status = AppStatus.PendingScan;
        app.RejectionReason = null;
        app.SubmittedAt = Clock.Now;
        app.UpdatedAt = Clock.Now;
        version.Status = VersionStatus.PendingScan;
        version.RejectionReason = null;
        foreach (var f in version.Files.Where(f => f.ScanStatus == ScanStatus.Error)) f.ScanStatus = ScanStatus.Pending;
        jobs.Enqueue(JobTypes.ScanVersion, new { AppId = app.Id, VersionId = version.Id }, $"version:{version.Id}");
        if (app.LlmSuggestedCategoryId is null) jobs.Enqueue(JobTypes.CategorizeApp, new { AppId = app.Id }, $"app:{app.Id}");
        await db.SaveChangesAsync(ct);
        return await MapDraftAsync(app, ct);
    }

    public async Task<AppDraftDto> WithdrawAsync(int id, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        if (app.Status is not (AppStatus.PendingScan or AppStatus.PendingReview)) throw ApiException.Unprocessable("Only pending submissions can be withdrawn.");
        app.Status = AppStatus.Draft;
        foreach (var v in app.Versions.Where(v => v.Status is VersionStatus.PendingScan or VersionStatus.PendingReview)) v.Status = VersionStatus.Draft;
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return await MapDraftAsync(app, ct);
    }

    // ---------------------------------------------------------------- duplicate check

    public async Task<DuplicateCheckDto> CheckDuplicatesAsync(DuplicateCheckRequest request, CancellationToken ct)
    {
        var threshold = await settings.GetDoubleAsync(SettingKeys.DuplicateThreshold, 0.80, ct);
        var dto = new DuplicateCheckDto { Threshold = threshold, SemanticAvailable = embeddings.Available };
        var text = EmbeddingService.BuildDraftText(request.Name, request.ShortDescription, request.LongDescription);
        if (text.Length < 15) return dto;
        var floor = Math.Max(0.3, threshold - 0.25);
        dto.Matches = await search.NearestAsync(text, 5, request.ExcludeAppId, floor, ct);
        if (dto.Matches.Count == 0 && !string.IsNullOrWhiteSpace(request.Name))
        {
            // keyword fallback on the name
            var name = request.Name.Trim();
            dto.Matches = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published && (request.ExcludeAppId == null || a.Id != request.ExcludeAppId) && EF.Functions.ILike(a.Name, $"%{name}%"))
                .OrderByDescending(a => a.DownloadCount).Take(5).Select(AppCardProjection.ToCard).ToListAsync(ct);
        }
        dto.BestMatch = dto.Matches.FirstOrDefault(m => (m.Similarity ?? 0) >= threshold);
        return dto;
    }

    // ---------------------------------------------------------------- stats

    public async Task<MyAppStatsDto> GetStatsAsync(int id, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var since = Clock.Now.AddDays(-30).Date;
        var downloads = await db.Downloads.AsNoTracking().Where(d => d.AppId == id && d.CreatedAt >= since)
            .GroupBy(d => d.CreatedAt.Date).Select(g => new { Date = g.Key, Count = g.Count() }).ToListAsync(ct);
        var byPlatform = await db.Downloads.AsNoTracking().Where(d => d.AppId == id)
            .Join(db.AppFiles, d => d.FileId, f => f.Id, (d, f) => f.Platform!.Code)
            .GroupBy(c => c ?? "source").Select(g => new { Key = g.Key, Count = g.Count() }).ToListAsync(ct);
        var bySource = await db.Downloads.AsNoTracking().Where(d => d.AppId == id).GroupBy(d => d.Source).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var c = await settings.SavingsAsync(ct);
        var saved = app.EstGenerationTokens * app.DownloadCount;
        return new MyAppStatsDto
        {
            AppId = id,
            Downloads = Enumerable.Range(0, 31).Select(i => DateOnly.FromDateTime(since.AddDays(i))).Select(d => new DailyCountDto { Date = d, Count = downloads.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == d)?.Count ?? 0 }).ToList(),
            DownloadsByPlatform = byPlatform.ToDictionary(x => x.Key, x => x.Count),
            DownloadsBySource = bySource.ToDictionary(x => x.Key.ToString().ToLowerInvariant(), x => x.Count),
            EstSavedTokens = saved,
            EstSavedCostUsd = c.Cost(saved)
        };
    }

    // ---------------------------------------------------------------- helpers

    private async Task EnsureCanUploadAsync(CancellationToken ct)
    {
        if (currentUser.IsApiKey && !currentUser.Scopes.Contains("submit")) throw ApiException.Forbidden("This API key has no 'submit' scope.");
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == currentUser.Id, ct);
        if (user.IsBanned || !user.IsActive) throw ApiException.Forbidden("Your account cannot upload apps.");
        if (siteOptions.Value.RequireEmailVerification && user.EmailVerifiedAt is null) throw ApiException.Forbidden("Verify your e-mail address before uploading.");
    }

    internal async Task<App> LoadOwnedAsync(int id, CancellationToken ct)
    {
        var app = await db.Apps
            .Include(a => a.License).Include(a => a.Category).Include(a => a.LlmModel).Include(a => a.DerivedFrom)
            .Include(a => a.Screenshots).Include(a => a.Prompts).Include(a => a.AppTags).ThenInclude(t => t.Tag)
            .Include(a => a.Versions).ThenInclude(v => v.Files).ThenInclude(f => f.Platform)
            .FirstOrDefaultAsync(a => a.Id == id, ct) ?? throw ApiException.NotFound("App not found.");
        if (app.UploaderUserId != currentUser.Id && !currentUser.IsAdmin) throw ApiException.Forbidden("This app belongs to another member.");
        if (app.Status == AppStatus.Removed && !currentUser.IsAdmin) throw ApiException.NotFound("App not found.");
        return app;
    }

    internal async Task<AppDraftDto> MapDraftAsync(App app, CancellationToken ct)
    {
        var draftVersion = app.Versions.OrderByDescending(v => v.Id).FirstOrDefault(v => v.Status is VersionStatus.Draft or VersionStatus.Rejected or VersionStatus.PendingScan or VersionStatus.PendingReview);
        var readiness = draftVersion is null ? new ReadinessDto { CanSubmit = false } : await GetReadinessAsync(app, draftVersion, ct);
        var licenseOk = (await lifecycle.CheckLicenseAsync(app, ct)).Ok;
        return new AppDraftDto
        {
            Id = app.Id, Slug = app.Slug, Name = app.Name, ShortDescription = app.ShortDescription, LongDescription = app.LongDescription, ReadmeMarkdown = app.ReadmeMarkdown,
            IconUrl = FileUrls.Icon(app.IconStorageKey), HomepageUrl = app.HomepageUrl, Status = app.Status, RejectionReason = app.RejectionReason,
            CategoryId = app.CategoryId, CategoryPath = await categories.PathAsync(app.CategoryId, ct),
            LicenseId = app.LicenseId, LicenseSpdxId = app.License?.SpdxId, LlmModelId = app.LlmModelId, LlmModelNote = app.LlmModelNote,
            Tags = app.AppTags.Select(t => t.Tag.Name).ToList(),
            DerivedFromAppId = app.DerivedFromAppId, DerivedFromName = app.DerivedFrom?.Name, DerivedFromSlug = app.DerivedFrom?.Slug, DerivationKind = app.DerivationKind,
            Prompts = app.Prompts.OrderBy(p => p.SortOrder).Select(p => new AppPromptDto { Id = p.Id, Title = p.Title, PromptText = p.PromptText, SortOrder = p.SortOrder }).ToList(),
            SourceKind = app.SourceKind, RepoUrl = app.RepoUrl, RepoProvider = app.RepoProvider?.ToString(), RepoDefaultBranch = app.RepoDefaultBranch, RepoStars = app.RepoStars,
            Source = new SourceAnalysisDto
            {
                AnalyzedAt = app.SourceAnalyzedAt, FileCount = app.SourceFileCount, LineCount = app.SourceLineCount, Bytes = app.SourceBytes, HasLicenseFile = app.HasLicenseFile,
                DetectedLicenseSpdxId = app.DetectedLicenseSpdxId, PrimaryLanguage = app.SourcePrimaryLanguage ?? app.RepoPrimaryLanguage, Warnings = app.SourceWarnings, LicenseMatches = licenseOk
            },
            Screenshots = app.Screenshots.OrderBy(s => s.SortOrder).Select(s => new ScreenshotDto { Id = s.Id, Url = FileUrls.Screenshot(s.StorageKey)!, ThumbUrl = FileUrls.Screenshot(s.ThumbStorageKey)!, Width = s.Width, Height = s.Height, Caption = s.Caption, SortOrder = s.SortOrder }).ToList(),
            Versions = app.Versions.OrderByDescending(v => v.Id).Select(v => CatalogService.MapVersion(app.Slug, v)).ToList(),
            DraftVersion = draftVersion is null ? null : CatalogService.MapVersion(app.Slug, draftVersion),
            EstGenerationTokens = app.EstGenerationTokens, EstGenerationCostUsd = app.EstGenerationCostUsd, EstIsOverride = app.EstIsOverride,
            Readiness = readiness,
            DownloadCount = app.DownloadCount, ViewCount = app.ViewCount, RatingCount = app.RatingCount, RatingAvg = app.RatingAvg,
            CreatedAt = app.CreatedAt, UpdatedAt = app.UpdatedAt, SubmittedAt = app.SubmittedAt, PublishedAt = app.PublishedAt, LlmSuggestedCategoryId = app.LlmSuggestedCategoryId
        };
    }

    internal async Task<string> UniqueSlugAsync(string name, int? excludeId, CancellationToken ct)
    {
        var baseSlug = TextUtil.Slugify(name, 100);
        if (baseSlug.Length < 2) baseSlug = "app";
        var slug = baseSlug;
        var i = 1;
        while (await db.Apps.AnyAsync(a => a.Slug == slug && (excludeId == null || a.Id != excludeId), ct)) slug = $"{baseSlug}-{++i}";
        return slug;
    }

    private string TempDir()
    {
        var dir = Path.IsPathRooted(storageOptions.Value.TempPath) ? storageOptions.Value.TempPath : Path.Combine(env.ContentRootPath, storageOptions.Value.TempPath);
        Directory.CreateDirectory(dir);
        return dir;
    }

    internal async Task DeleteStoredAsync(AppFile file, CancellationToken ct)
    {
        if (file.StorageKey is null) return;
        var bucket = file.Kind == FileKind.Source ? Bucket.Sources : Bucket.Installers;
        try { await storage.DeleteAsync(bucket, file.StorageKey, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not delete {Key}", file.StorageKey); }
    }

    private static AppVersion RequireEditableVersion(App app, int versionId)
    {
        var version = app.Versions.FirstOrDefault(v => v.Id == versionId) ?? throw ApiException.NotFound("Version not found.");
        if (version.Status is not (VersionStatus.Draft or VersionStatus.Rejected)) throw ApiException.Unprocessable("Only draft versions can be changed. Create a new version instead.");
        return version;
    }
}

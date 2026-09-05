using System.Diagnostics;
using System.Reflection;
using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Ai;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Infrastructure.Scanning;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using AiAlreadyDidIt.Api.Services.Apps;
using AiAlreadyDidIt.Api.Services.Catalog;
using AiAlreadyDidIt.Api.Services.Site;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Services.Admin;

/// <summary>Admin › panel menu, dashboard, statistics, search analytics, savings, settings, audit log, jobs, health.</summary>
public sealed class AdminSystemService(AadiDbContext db, ICurrentUser currentUser, SavingsService savings, SiteSettingsCache settings, AuditService audit, JobQueue jobs,
    IObjectStorage storage, IVirusScanner scanner, ILlmProviderFactory providers, IOptions<AiOptions> ai, IOptions<StorageOptions> storageOptions, IOptions<ClamAvOptions> clam,
    IOptions<SiteOptions> site, IOptions<RateLimitingOptions> rateLimits, IHostEnvironment env, IMemoryCache cache, AdminModerationService moderation, AdminReportsService reports)
{
    private static readonly DateTime StartedAt = DateTime.UtcNow;

    // ---------------------------------------------------------------- panel

    public async Task<List<AdminMenuItemDto>> MenuAsync(CancellationToken ct)
    {
        var allowed = currentUser.IsAdmin ? null : await db.RoleMenus.AsNoTracking().Where(rm => rm.Role.Name == currentUser.RoleName && rm.HasAccess).Select(rm => rm.MenuId).ToHashSetAsync(ct);
        var rows = await db.Menus.AsNoTracking().Where(m => m.Visible).OrderBy(m => m.OrderNum).ToListAsync(ct);
        var nodes = rows.ToDictionary(r => r.Id, r => new AdminMenuItemDto { Label = r.Name, Icon = r.Icon, Route = r.Route });
        var roots = new List<AdminMenuItemDto>();
        foreach (var r in rows)
        {
            if (allowed is not null && !allowed.Contains(r.Id)) continue;
            if (r.ParentId is { } pid && nodes.TryGetValue(pid, out var p)) p.Children.Add(nodes[r.Id]); else roots.Add(nodes[r.Id]);
        }
        return roots.Where(r => r.Route is not null || r.Children.Count > 0).ToList();
    }

    public async Task<AdminDashboardDto> DashboardAsync(CancellationToken ct)
    {
        var since7 = Clock.Now.AddDays(-7);
        var since30 = Clock.Now.AddDays(-30).Date;
        var queue = await moderation.QueueAsync(new AdminListQuery { PageSize = 5 }, ct);
        var recentReports = await reports.ListAsync(new AdminListQuery { PageSize = 5 }, ct);
        return new AdminDashboardDto
        {
            PendingReview = await db.Apps.CountAsync(a => a.Status == AppStatus.PendingReview || a.Versions.Any(v => v.Status == VersionStatus.PendingReview), ct),
            PendingScan = await db.Apps.CountAsync(a => a.Status == AppStatus.PendingScan || a.Versions.Any(v => v.Status == VersionStatus.PendingScan), ct),
            OpenReports = await db.Reports.CountAsync(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.Reviewing, ct),
            OpenRequests = await db.AppRequests.CountAsync(r => r.Status == AppRequestStatus.Open, ct),
            PublishedApps = await db.Apps.CountAsync(a => a.Status == AppStatus.Published, ct),
            TotalApps = await db.Apps.CountAsync(a => a.Status != AppStatus.Removed, ct),
            Members = await db.Users.CountAsync(ct),
            NewMembers7d = await db.Users.CountAsync(u => u.CreatedAt >= since7, ct),
            Downloads = await db.Downloads.LongCountAsync(ct),
            Downloads7d = await db.Downloads.CountAsync(d => d.CreatedAt >= since7, ct),
            Searches = await db.SearchLogs.LongCountAsync(ct),
            Searches7d = await db.SearchLogs.CountAsync(s => s.CreatedAt >= since7, ct),
            ZeroResultSearches7d = await db.SearchLogs.CountAsync(s => s.CreatedAt >= since7 && s.ResultCount == 0, ct),
            AgentCalls7d = await db.SearchLogs.CountAsync(s => s.CreatedAt >= since7 && s.Source != RequestSource.Web, ct),
            FailedJobs = await db.BackgroundJobs.CountAsync(j => j.Status == JobStatus.Failed, ct),
            QueuedJobs = await db.BackgroundJobs.CountAsync(j => j.Status == JobStatus.Queued || j.Status == JobStatus.Running, ct),
            InfectedFiles = await db.AppFiles.CountAsync(f => f.ScanStatus == ScanStatus.Infected, ct),
            ProposedCategories = await db.Categories.CountAsync(c => c.IsLlmProposed, ct),
            Savings = await savings.GetAsync(ct),
            DownloadsSeries = await Series(db.Downloads.Where(d => d.CreatedAt >= since30).Select(d => d.CreatedAt), since30, ct),
            SearchesSeries = await Series(db.SearchLogs.Where(s => s.CreatedAt >= since30).Select(s => s.CreatedAt), since30, ct),
            SignupsSeries = await Series(db.Users.Where(u => u.CreatedAt >= since30).Select(u => u.CreatedAt), since30, ct),
            TopApps = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published).OrderByDescending(a => a.DownloadCount).Take(5).Select(AppCardProjection.ToCard).ToListAsync(ct),
            QueuePreview = queue.Items,
            RecentReports = recentReports.Items
        };
    }

    private static async Task<List<DailyPointDto>> Series(IQueryable<DateTime> dates, DateTime since, CancellationToken ct)
    {
        var rows = await dates.GroupBy(d => d.Date).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var days = (int)(Clock.Now.Date - since).TotalDays + 1;
        return Enumerable.Range(0, days).Select(i => DateOnly.FromDateTime(since.AddDays(i))).Select(d => new DailyPointDto { Date = d, Value = rows.FirstOrDefault(r => DateOnly.FromDateTime(r.Key) == d)?.Count ?? 0 }).ToList();
    }

    // ---------------------------------------------------------------- statistics

    public async Task<StatsOverviewDto> StatsAsync(int days, CancellationToken ct)
    {
        days = Math.Clamp(days, 7, 365);
        var since = Clock.Now.AddDays(-days).Date;
        var dto = new StatsOverviewDto
        {
            Downloads = await Series(db.Downloads.Where(d => d.CreatedAt >= since).Select(d => d.CreatedAt), since, ct),
            Searches = await Series(db.SearchLogs.Where(s => s.CreatedAt >= since).Select(s => s.CreatedAt), since, ct),
            Signups = await Series(db.Users.Where(u => u.CreatedAt >= since).Select(u => u.CreatedAt), since, ct),
            Uploads = await Series(db.Apps.Where(a => a.PublishedAt != null && a.PublishedAt >= since).Select(a => a.PublishedAt!.Value), since, ct),
            DownloadsByPlatform = await db.Downloads.AsNoTracking().Where(d => d.CreatedAt >= since).Join(db.AppFiles, d => d.FileId, f => f.Id, (d, f) => f.Platform == null ? "source" : f.Platform.Code)
                .GroupBy(c => c).Select(g => new FacetItemDto { Value = g.Key, Label = g.Key, Count = g.Count() }).OrderByDescending(f => f.Count).ToListAsync(ct),
            DownloadsBySource = await db.Downloads.AsNoTracking().Where(d => d.CreatedAt >= since).GroupBy(d => d.Source).Select(g => new FacetItemDto { Value = g.Key.ToString(), Label = g.Key.ToString(), Count = g.Count() }).ToListAsync(ct),
            AppsByCategory = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published).GroupBy(a => a.Category.NameEn).Select(g => new FacetItemDto { Value = g.Key, Label = g.Key, Count = g.Count() }).OrderByDescending(f => f.Count).Take(15).ToListAsync(ct),
            AppsByModel = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published && a.LlmModel != null).GroupBy(a => a.LlmModel!.Vendor + " " + a.LlmModel.Name).Select(g => new FacetItemDto { Value = g.Key, Label = g.Key, Count = g.Count() }).OrderByDescending(f => f.Count).Take(15).ToListAsync(ct),
            AppsByLicense = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published).GroupBy(a => a.License.SpdxId).Select(g => new FacetItemDto { Value = g.Key, Label = g.Key, Count = g.Count() }).OrderByDescending(f => f.Count).Take(15).ToListAsync(ct),
            TopApps = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published).OrderByDescending(a => a.DownloadCount).Take(10).Select(AppCardProjection.ToCard).ToListAsync(ct),
            TopUploaders = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published).GroupBy(a => new { a.Uploader.Username, a.Uploader.DisplayName })
                .Select(g => new TopUploaderDto { Username = g.Key.Username, DisplayName = g.Key.DisplayName, Apps = g.Count(), Downloads = g.Sum(a => a.DownloadCount) }).OrderByDescending(u => u.Downloads).Take(10).ToListAsync(ct)
        };
        return dto;
    }

    public async Task<SearchAnalyticsDto> SearchAnalyticsAsync(int days, CancellationToken ct)
    {
        days = Math.Clamp(days, 1, 365);
        var since = Clock.Now.AddDays(-days).Date;
        var logs = db.SearchLogs.AsNoTracking().Where(s => s.CreatedAt >= since);
        var grouped = logs.GroupBy(s => s.Query.ToLower()).Select(g => new QueryStatDto { Query = g.Key, Count = g.Count(), AvgResults = (int)g.Average(s => s.ResultCount), AvgTopSimilarity = g.Average(s => s.TopSimilarity), LastSeen = g.Max(s => s.CreatedAt) });
        return new SearchAnalyticsDto
        {
            TopQueries = await grouped.OrderByDescending(q => q.Count).Take(50).ToListAsync(ct),
            ZeroResultQueries = await logs.Where(s => s.ResultCount == 0).GroupBy(s => s.Query.ToLower()).Select(g => new QueryStatDto { Query = g.Key, Count = g.Count(), AvgResults = 0, AvgTopSimilarity = g.Average(s => s.TopSimilarity), LastSeen = g.Max(s => s.CreatedAt) }).OrderByDescending(q => q.Count).Take(50).ToListAsync(ct),
            BySource = await logs.GroupBy(s => s.Source).Select(g => new FacetItemDto { Value = g.Key.ToString(), Label = g.Key.ToString(), Count = g.Count() }).ToListAsync(ct),
            ByMode = await logs.GroupBy(s => s.Mode).Select(g => new FacetItemDto { Value = g.Key.ToString(), Label = g.Key.ToString(), Count = g.Count() }).ToListAsync(ct),
            AvgTookMs = await logs.AnyAsync(ct) ? await logs.AverageAsync(s => (double)s.TookMs, ct) : 0,
            Total = await logs.CountAsync(ct),
            Series = await Series(logs.Select(s => s.CreatedAt), since, ct)
        };
    }

    public async Task<SavingsBreakdownDto> SavingsAsync(CancellationToken ct)
    {
        var c = await settings.SavingsAsync(ct);
        var since = Clock.Now.AddDays(-30).Date;
        var daily = await db.Downloads.AsNoTracking().Where(d => d.CreatedAt >= since).Join(db.Apps, d => d.AppId, a => a.Id, (d, a) => new { d.CreatedAt.Date, a.EstGenerationTokens })
            .GroupBy(x => x.Date).Select(g => new { g.Key, Tokens = g.Sum(x => x.EstGenerationTokens) }).ToListAsync(ct);
        var apps = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published).OrderByDescending(a => a.EstGenerationTokens * (long)a.DownloadCount).Take(20)
            .Select(a => new AppSavingsDto { AppId = a.Id, Slug = a.Slug, Name = a.Name, Downloads = a.DownloadCount, TokensPerDownload = a.EstGenerationTokens, TokensSaved = a.EstGenerationTokens * a.DownloadCount, IsOverride = a.EstIsOverride }).ToListAsync(ct);
        foreach (var a in apps) a.CostSavedUsd = c.Cost(a.TokensSaved);
        return new SavingsBreakdownDto
        {
            Totals = await savings.GetAsync(ct),
            TokensSeries = Enumerable.Range(0, 31).Select(i => DateOnly.FromDateTime(since.AddDays(i))).Select(d => new DailyPointDto { Date = d, Value = (int)Math.Min(int.MaxValue, daily.FirstOrDefault(x => DateOnly.FromDateTime(x.Key) == d)?.Tokens ?? 0) }).ToList(),
            TopApps = apps,
            Coefficients = new Dictionary<string, decimal> { ["tokensPerLine"] = c.TokensPerLine, ["iterationFactor"] = c.IterationFactor, ["pricePerMillionTokens"] = c.PricePerMillionTokens, ["kwhPerMillionTokens"] = c.KwhPerMillionTokens, ["co2GramsPerKwh"] = c.Co2GramsPerKwh, ["baseTokens"] = c.BaseTokens }
        };
    }

    // ---------------------------------------------------------------- settings

    public Task<List<SiteSettingDto>> SettingsAsync(CancellationToken ct) =>
        db.SiteSettings.AsNoTracking().OrderBy(s => s.Group).ThenBy(s => s.Key).Select(s => new SiteSettingDto { Key = s.Key, Value = s.Value, Group = s.Group, ValueType = s.ValueType, Description = s.Description, UpdatedAt = s.UpdatedAt }).ToListAsync(ct);

    public async Task<List<SiteSettingDto>> SaveSettingsAsync(SaveSettingsRequest request, CancellationToken ct)
    {
        var rows = await db.SiteSettings.Where(s => request.Values.Keys.Contains(s.Key)).ToListAsync(ct);
        foreach (var row in rows)
        {
            var value = request.Values[row.Key];
            switch (row.ValueType)
            {
                case "int" when !int.TryParse(value, out _): throw ApiException.Unprocessable($"{row.Key} must be an integer.", row.Key);
                case "decimal" when !decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _): throw ApiException.Unprocessable($"{row.Key} must be a number.", row.Key);
                case "bool" when !bool.TryParse(value, out _): throw ApiException.Unprocessable($"{row.Key} must be true or false.", row.Key);
            }
            row.Value = value; row.UpdatedAt = Clock.Now; row.UpdatedByUserId = currentUser.Id;
        }
        audit.Log("settings.update", "settings", null, request.Values);
        await db.SaveChangesAsync(ct);
        settings.Invalidate();
        cache.Remove("site:config:v1"); cache.Remove("catalog:home:v1"); savings.Invalidate();
        return await SettingsAsync(ct);
    }

    // ---------------------------------------------------------------- audit & jobs

    public async Task<PagedResult<AuditLogDto>> AuditAsync(AdminListQuery query, CancellationToken ct)
    {
        var q = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(a => EF.Functions.ILike(a.Action, $"%{query.Q}%") || EF.Functions.ILike(a.Username, $"%{query.Q}%") || a.EntityId == query.Q || a.Entity == query.Q);
        if (query.From is { } from) q = q.Where(a => a.CreatedAt >= from);
        if (query.To is { } to) q = q.Where(a => a.CreatedAt <= to);
        return await q.OrderByDescending(a => a.CreatedAt).Select(a => new AuditLogDto { Id = a.Id, UserId = a.UserId, Username = a.Username, Action = a.Action, Entity = a.Entity, EntityId = a.EntityId, Details = a.Details, IpAddress = a.IpAddress, CreatedAt = a.CreatedAt }).ToPagedAsync(query.SafePage, query.SafePageSize, ct);
    }

    public async Task<PagedResult<JobDto>> JobsAsync(AdminListQuery query, CancellationToken ct)
    {
        var q = db.BackgroundJobs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<JobStatus>(query.Status, true, out var status)) q = q.Where(j => j.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(j => j.Type == query.Q || j.Subject == query.Q);
        return await q.OrderByDescending(j => j.Id).Select(j => new JobDto { Id = j.Id, Type = j.Type, PayloadJson = j.PayloadJson, Status = j.Status, Attempts = j.Attempts, MaxAttempts = j.MaxAttempts, LastError = j.LastError, RunAt = j.RunAt, StartedAt = j.StartedAt, FinishedAt = j.FinishedAt, CreatedAt = j.CreatedAt, Subject = j.Subject }).ToPagedAsync(query.SafePage, query.SafePageSize, ct);
    }

    public async Task RetryJobAsync(long id, CancellationToken ct)
    {
        var job = await db.BackgroundJobs.FirstOrDefaultAsync(j => j.Id == id, ct) ?? throw ApiException.NotFound("Job not found.");
        job.Status = JobStatus.Queued; job.RunAt = Clock.Now; job.Attempts = 0; job.LastError = null; job.FinishedAt = null;
        audit.Log("job.retry", "job", id);
        await db.SaveChangesAsync(ct);
    }

    public async Task CancelJobAsync(long id, CancellationToken ct)
    {
        await db.BackgroundJobs.Where(j => j.Id == id && j.Status == JobStatus.Queued).ExecuteUpdateAsync(s => s.SetProperty(j => j.Status, JobStatus.Cancelled).SetProperty(j => j.FinishedAt, Clock.Now), ct);
        audit.Log("job.cancel", "job", id);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Maintenance: reembed_all | recompute_stats | rescan_pending | purge_done_jobs</summary>
    public async Task<string> MaintenanceAsync(string task, CancellationToken ct)
    {
        switch (task)
        {
            case "reembed_all": jobs.Enqueue(JobTypes.ReembedAll, new { }, "maintenance"); break;
            case "recompute_stats": jobs.Enqueue(JobTypes.RecomputeStats, new { AppId = 0 }, "maintenance"); foreach (var id in await db.Apps.Select(a => a.Id).ToListAsync(ct)) jobs.Enqueue(JobTypes.RecomputeStats, new { AppId = id }, $"app:{id}"); break;
            case "rescan_pending":
                foreach (var v in await db.AppVersions.Where(v => v.Status == VersionStatus.PendingScan).Select(v => new { v.Id, v.AppId }).ToListAsync(ct)) jobs.Enqueue(JobTypes.ScanVersion, new { v.AppId, VersionId = v.Id }, $"version:{v.Id}");
                break;
            case "purge_done_jobs": await db.BackgroundJobs.Where(j => j.Status == JobStatus.Done && j.FinishedAt < Clock.Now.AddDays(-7)).ExecuteDeleteAsync(ct); break;
            default: throw ApiException.BadRequest("Unknown maintenance task.");
        }
        audit.Log("maintenance." + task);
        await db.SaveChangesAsync(ct);
        return task;
    }

    // ---------------------------------------------------------------- health

    public async Task<SystemHealthDto> HealthAsync(CancellationToken ct)
    {
        var checks = new List<HealthCheckDto>();
        async Task Check(string name, Func<Task<(bool, string?)>> probe)
        {
            var sw = Stopwatch.StartNew();
            try { var (ok, detail) = await probe(); checks.Add(new HealthCheckDto { Name = name, Ok = ok, Detail = detail, LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1) }); }
            catch (Exception ex) { checks.Add(new HealthCheckDto { Name = name, Ok = false, Detail = ex.Message, LatencyMs = Math.Round(sw.Elapsed.TotalMilliseconds, 1) }); }
        }
        await Check("PostgreSQL", async () => { var v = await db.Database.SqlQueryRaw<string>("SELECT version() AS \"Value\"").FirstAsync(ct); var vec = await db.Database.SqlQueryRaw<string>("SELECT extversion AS \"Value\" FROM pg_extension WHERE extname = 'vector'").FirstOrDefaultAsync(ct); return (true, $"{v.Split(',')[0]} · pgvector {vec ?? "missing"}"); });
        await Check("Object storage (MinIO/S3)", async () => (await storage.PingAsync(ct), storageOptions.Value.Endpoint));
        await Check("ClamAV", async () => { var (ok, detail) = await scanner.PingAsync(ct); return (ok || !clam.Value.Enabled, clam.Value.Enabled ? detail : "disabled"); });
        await Check($"Embeddings ({providers.Embeddings.Name})", async () => { if (!providers.Embeddings.SupportsEmbeddings) return (false, "disabled — keyword search only"); var h = await providers.Embeddings.CheckHealthAsync(ct); return (h.Ok, $"{h.EmbeddingModel} · {h.Detail}"); });
        await Check($"Chat model ({providers.Chat.Name})", async () => { if (!providers.Chat.SupportsChat) return (false, "disabled — no category suggestions"); var h = await providers.Chat.CheckHealthAsync(ct); return (h.Ok, $"{h.ChatModel} · {h.Detail}"); });
        await Check("Background jobs", async () => { var queued = await db.BackgroundJobs.CountAsync(j => j.Status == JobStatus.Queued, ct); var failed = await db.BackgroundJobs.CountAsync(j => j.Status == JobStatus.Failed, ct); var stuck = await db.BackgroundJobs.CountAsync(j => j.Status == JobStatus.Running && j.StartedAt < Clock.Now.AddMinutes(-30), ct); return (failed == 0 && stuck == 0, $"{queued} queued · {failed} failed · {stuck} stuck"); });
        await Check("Temp disk", () => { var path = Path.IsPathRooted(storageOptions.Value.TempPath) ? storageOptions.Value.TempPath : Path.Combine(env.ContentRootPath, storageOptions.Value.TempPath); Directory.CreateDirectory(path); var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))!); var free = drive.AvailableFreeSpace; return Task.FromResult((free > 2L * 1024 * 1024 * 1024, $"{TextUtil.HumanSize(free)} free at {path}")); });
        await Check("Stale embeddings", async () => { var stale = await db.Apps.CountAsync(a => a.Status == AppStatus.Published && (a.EmbeddingStale || a.Embedding == null), ct); return (stale == 0, $"{stale} published apps need (re)embedding"); });

        return new SystemHealthDto
        {
            Checks = checks,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "dev",
            StartedAt = StartedAt,
            Environment = env.EnvironmentName,
            Config = new Dictionary<string, string>
            {
                ["Site:PublicUrl"] = site.Value.PublicUrl, ["Site:ApiPublicUrl"] = site.Value.ApiPublicUrl, ["Site:McpPublicUrl"] = site.Value.McpPublicUrl,
                ["Site:GoogleClientId"] = string.IsNullOrEmpty(site.Value.GoogleClientId) ? "(not set — Google sign-in hidden)" : "configured", ["Site:RequireEmailVerification"] = site.Value.RequireEmailVerification.ToString(),
                ["Ai:Provider"] = ai.Value.Provider, ["Ai:EmbeddingProvider"] = providers.Embeddings.Name, ["Ai:ChatProvider"] = providers.Chat.Name, ["Ai:EmbeddingDimensions"] = ai.Value.EmbeddingDimensions.ToString(),
                ["Ai:Ollama:BaseUrl"] = ai.Value.Ollama.BaseUrl, ["Ai:Ollama:ChatModel"] = ai.Value.Ollama.ChatModel, ["Ai:Ollama:EmbeddingModel"] = ai.Value.Ollama.EmbeddingModel, ["Ai:OpenAi:BaseUrl"] = ai.Value.OpenAi.BaseUrl, ["Ai:OpenAi:ChatModel"] = ai.Value.OpenAi.ChatModel, ["Ai:OpenAi:EmbeddingModel"] = ai.Value.OpenAi.EmbeddingModel,
                ["Storage:Endpoint"] = storageOptions.Value.Endpoint, ["Storage:PublicEndpoint"] = storageOptions.Value.PublicEndpoint, ["Storage:MaxInstallerBytes"] = TextUtil.HumanSize(storageOptions.Value.MaxInstallerBytes), ["Storage:MaxSourceArchiveBytes"] = TextUtil.HumanSize(storageOptions.Value.MaxSourceArchiveBytes),
                ["ClamAv"] = $"{clam.Value.Host}:{clam.Value.Port} (enabled={clam.Value.Enabled}, failClosed={clam.Value.FailClosed})",
                ["RateLimiting"] = $"anon {rateLimits.Value.AnonymousPermitLimit}/min · user {rateLimits.Value.UserPermitLimit}/min · key {rateLimits.Value.ApiKeyDefaultPermitLimit}/min · downloads {rateLimits.Value.DownloadPermitLimit}/min"
            }
        };
    }
}

using System.Text.Json;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Email;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Infrastructure.Scanning;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using AiAlreadyDidIt.Api.Services.Apps;
using AiAlreadyDidIt.Api.Services.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Services.Jobs;

internal static class PayloadExtensions
{
    // payloads are serialised with camelCase; look the key up case-insensitively
    private static bool TryGet(JsonElement e, string name, out JsonElement value)
    {
        if (e.ValueKind == JsonValueKind.Object)
            foreach (var p in e.EnumerateObject())
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { value = p.Value; return true; }
        value = default;
        return false;
    }

    public static int Int(this JsonElement e, string name) => TryGet(e, name, out var p) && p.TryGetInt32(out var v) ? v : 0;
    public static string? Str(this JsonElement e, string name) => TryGet(e, name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}

/// <summary>Runs ClamAV over every pending file of a version, then hands the outcome to the lifecycle service.</summary>
public sealed class ScanVersionJob(AadiDbContext db, IObjectStorage storage, IVirusScanner scanner, AppLifecycleService lifecycle, IOptions<ClamAvOptions> clam, ILogger<ScanVersionJob> logger) : IJobHandler
{
    public string Type => JobTypes.ScanVersion;

    public async Task HandleAsync(JsonElement payload, CancellationToken ct)
    {
        var versionId = payload.Int("VersionId");
        var version = await db.AppVersions.Include(v => v.App).ThenInclude(a => a.License).Include(v => v.Files).FirstOrDefaultAsync(v => v.Id == versionId, ct);
        if (version is null || version.Status is not (VersionStatus.PendingScan or VersionStatus.Published)) return;
        var alreadyPublished = version.Status == VersionStatus.Published;
        var app = version.App;

        AppFile? infected = null;
        var unavailable = false;
        foreach (var file in version.Files.Where(f => f.StorageKey is not null && f.ScanStatus is ScanStatus.Pending or ScanStatus.Error))
        {
            var bucket = file.Kind == FileKind.Source ? Bucket.Sources : Bucket.Installers;
            ScanOutcome outcome;
            if (!scanner.Enabled) outcome = new ScanOutcome(ScanVerdict.Skipped, null, "scanner disabled");
            else
            {
                await using var stream = await storage.OpenReadAsync(bucket, file.StorageKey!, ct);
                outcome = await scanner.ScanAsync(stream, file.SizeBytes, ct);
            }
            file.ScanStatus = outcome.Verdict switch { ScanVerdict.Clean => ScanStatus.Clean, ScanVerdict.Infected => ScanStatus.Infected, ScanVerdict.Skipped => ScanStatus.Skipped, _ => ScanStatus.Error };
            file.ScanSignature = outcome.Signature;
            file.ScannedAt = Clock.Now;
            db.ScanResults.Add(new ScanResult { FileId = file.Id, Engine = "clamav", Verdict = file.ScanStatus, Signature = outcome.Signature, Raw = TextUtil.Truncate(outcome.Raw, 2000), ScannedAt = Clock.Now });
            if (outcome.Verdict == ScanVerdict.Infected) { infected ??= file; }
            if (outcome.Verdict == ScanVerdict.Error) unavailable = true;
        }
        await db.SaveChangesAsync(ct);

        if (infected is not null)
        {
            if (alreadyPublished)
            {
                app.Status = AppStatus.Unlisted;
                db.Reports.Add(new Report { AppId = app.Id, Reason = ReportReason.Malware, Details = $"Antivirus flagged {infected.FileName}: {infected.ScanSignature} (rescan of a published version — app unlisted automatically)", Status = ReportStatus.Open, CreatedAt = Clock.Now });
            }
            else await lifecycle.OnScanInfectedAsync(app, version, infected, ct);
            await db.SaveChangesAsync(ct);
            return;
        }
        if (alreadyPublished) return;
        if (unavailable && clam.Value.FailClosed)
        {
            logger.LogWarning("ClamAV unavailable; version {Version} stays in PendingScan and will be retried", versionId);
            throw new InvalidOperationException("ClamAV is unavailable; scan will be retried.");
        }
        await lifecycle.OnScanCleanAsync(app, version, ct);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class ImportRepositoryJob(AppEditorService editor) : IJobHandler
{
    public string Type => JobTypes.ImportRepository;
    public Task HandleAsync(JsonElement payload, CancellationToken ct) => editor.ImportRepositorySnapshotAsync(payload.Int("AppId"), payload.Int("VersionId"), ct);
}

/// <summary>Computes (or refreshes) the app's vector.</summary>
public sealed class EmbedAppJob(AadiDbContext db, EmbeddingService embeddings, ILogger<EmbedAppJob> logger) : IJobHandler
{
    public string Type => JobTypes.EmbedApp;

    public async Task HandleAsync(JsonElement payload, CancellationToken ct)
    {
        var app = await db.Apps.Include(a => a.LlmModel).FirstOrDefaultAsync(a => a.Id == payload.Int("AppId"), ct);
        if (app is null) return;
        if (!embeddings.Available) { logger.LogInformation("Embeddings disabled; app {Id} stays keyword-only", app.Id); return; }
        app.Embedding = await embeddings.EmbedAsync(EmbeddingService.BuildAppText(app), ct);
        app.EmbeddingModel = embeddings.ModelName;
        app.EmbeddingStale = false;
        await db.SaveChangesAsync(ct);
    }
}

public sealed class EmbedRequestJob(AadiDbContext db, EmbeddingService embeddings) : IJobHandler
{
    public string Type => JobTypes.EmbedRequest;

    public async Task HandleAsync(JsonElement payload, CancellationToken ct)
    {
        var req = await db.AppRequests.FirstOrDefaultAsync(r => r.Id == payload.Int("RequestId"), ct);
        if (req is null || !embeddings.Available) return;
        req.Embedding = await embeddings.EmbedAsync(req.Title + "\n" + req.Description, ct);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class CategorizeAppJob(CategorySuggestionService suggestions) : IJobHandler
{
    public string Type => JobTypes.CategorizeApp;
    public Task HandleAsync(JsonElement payload, CancellationToken ct) => suggestions.ApplyToAppAsync(payload.Int("AppId"), ct);
}

/// <summary>"Notify me on new versions of this app."</summary>
public sealed class NotifyWatchersJob(AadiDbContext db, NotificationService notifications) : IJobHandler
{
    public string Type => JobTypes.NotifyWatchers;

    public async Task HandleAsync(JsonElement payload, CancellationToken ct)
    {
        var appId = payload.Int("AppId");
        var version = await db.AppVersions.AsNoTracking().Include(v => v.App).FirstOrDefaultAsync(v => v.Id == payload.Int("VersionId"), ct);
        if (version is null) return;
        var watchers = await db.AppWatches.AsNoTracking().Where(w => w.AppId == appId && w.NotifyNewVersion).Join(db.Users, w => w.UserId, u => u.Id, (w, u) => new { u.Id, u.Email }).ToListAsync(ct);
        foreach (var w in watchers)
            notifications.Notify(w.Id, NotificationType.NewVersion, $"“{version.App.Name}” {version.Version} is out", TextUtil.Excerpt(version.Changelog, 300), $"/app/{version.App.Slug}", email: true, emailAddress: w.Email);
        await db.SaveChangesAsync(ct);
    }
}

public sealed class SendEmailJob(IEmailSender email) : IJobHandler
{
    public string Type => JobTypes.SendEmail;

    public Task HandleAsync(JsonElement payload, CancellationToken ct)
    {
        var to = payload.Str("To");
        if (string.IsNullOrWhiteSpace(to)) return Task.CompletedTask;
        return email.SendAsync(to, payload.Str("Subject") ?? "(no subject)", payload.Str("Html") ?? string.Empty, payload.Str("Text"), ct);
    }
}

public sealed class RecomputeStatsJob(AppLifecycleService lifecycle) : IJobHandler
{
    public string Type => JobTypes.RecomputeStats;

    public async Task HandleAsync(JsonElement payload, CancellationToken ct)
    {
        var appId = payload.Int("AppId");
        if (appId > 0) await lifecycle.RecomputeCountersAsync(appId, ct);
        else await lifecycle.RecomputeReferenceCountsAsync(ct);
    }
}

/// <summary>Admin action: re-embed every published app (after switching the embedding model).</summary>
public sealed class ReembedAllJob(AadiDbContext db, JobQueue jobs) : IJobHandler
{
    public string Type => JobTypes.ReembedAll;

    public async Task HandleAsync(JsonElement payload, CancellationToken ct)
    {
        var ids = await db.Apps.Where(a => a.Status == AppStatus.Published || a.Status == AppStatus.Unlisted).Select(a => a.Id).ToListAsync(ct);
        foreach (var id in ids) jobs.Enqueue(JobTypes.EmbedApp, new { AppId = id }, $"app:{id}");
        var reqs = await db.AppRequests.Where(r => r.Status == AppRequestStatus.Open).Select(r => r.Id).ToListAsync(ct);
        foreach (var id in reqs) jobs.Enqueue(JobTypes.EmbedRequest, new { RequestId = id }, $"request:{id}");
        await db.SaveChangesAsync(ct);
    }
}

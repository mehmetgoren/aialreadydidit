using System.Text.Json;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;

namespace AiAlreadyDidIt.Api.Infrastructure.Jobs;

public static class JobTypes
{
    public const string ScanVersion = "scan_version";
    public const string AnalyzeSource = "analyze_source";
    public const string ImportRepository = "import_repository";
    public const string EmbedApp = "embed_app";
    public const string EmbedRequest = "embed_request";
    public const string CategorizeApp = "categorize_app";
    public const string NotifyWatchers = "notify_watchers";
    public const string SendEmail = "send_email";
    public const string RecomputeStats = "recompute_stats";
    public const string ReembedAll = "reembed_all";
}

/// <summary>Enqueues durable background work (a row in <c>background_jobs</c>, picked up by <see cref="BackgroundJobRunner"/>).</summary>
public sealed class JobQueue(AadiDbContext db)
{
    public BackgroundJob Enqueue(string type, object payload, string? subject = null, TimeSpan? delay = null, int maxAttempts = 3)
    {
        var job = new BackgroundJob
        {
            Type = type,
            PayloadJson = JsonSerializer.Serialize(payload, AadiJson.Options),
            Status = JobStatus.Queued,
            MaxAttempts = maxAttempts,
            RunAt = Clock.Now.Add(delay ?? TimeSpan.Zero),
            CreatedAt = Clock.Now,
            Subject = subject
        };
        db.BackgroundJobs.Add(job);
        return job;
    }

    /// <summary>Enqueue unless an identical queued job (same type + subject) is already waiting.</summary>
    public async Task<BackgroundJob?> EnqueueOnceAsync(string type, object payload, string subject, CancellationToken ct = default)
    {
        var exists = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
            db.BackgroundJobs, j => j.Type == type && j.Subject == subject && (j.Status == JobStatus.Queued || j.Status == JobStatus.Running), ct);
        return exists ? null : Enqueue(type, payload, subject);
    }
}

public interface IJobHandler
{
    string Type { get; }
    Task HandleAsync(JsonElement payload, CancellationToken ct);
}

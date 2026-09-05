using System.Text.Json;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Infrastructure.Jobs;

/// <summary>
/// Polls <c>background_jobs</c> and dispatches to the <see cref="IJobHandler"/> registered for the job type.
/// Claims rows with FOR UPDATE SKIP LOCKED so several API replicas can share the queue. Failed jobs retry with
/// exponential back-off until <c>max_attempts</c>.
/// </summary>
public sealed class BackgroundJobRunner(IServiceScopeFactory scopeFactory, IOptions<JobsOptions> options, ILogger<BackgroundJobRunner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); // let migrations finish
        var workers = Enumerable.Range(0, Math.Max(1, options.Value.Concurrency)).Select(i => WorkerLoop(i, stoppingToken));
        await Task.WhenAll(workers);
    }

    private async Task WorkerLoop(int worker, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var didWork = false;
            try { didWork = await RunOneAsync(ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Job worker {Worker} loop error", worker); }
            if (!didWork)
            {
                try { await Task.Delay(TimeSpan.FromSeconds(options.Value.PollSeconds), ct); }
                catch (OperationCanceledException) { }
            }
        }
    }

    private async Task<bool> RunOneAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AadiDbContext>();

        BackgroundJob? job;
        await using (var tx = await db.Database.BeginTransactionAsync(ct))
        {
            job = await db.BackgroundJobs.FromSqlRaw(
                    "SELECT * FROM background_jobs WHERE status = 0 AND run_at <= now() ORDER BY run_at LIMIT 1 FOR UPDATE SKIP LOCKED")
                .FirstOrDefaultAsync(ct);
            if (job is null) return false;
            job.Status = JobStatus.Running;
            job.StartedAt = Clock.Now;
            job.Attempts++;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }

        var handler = scope.ServiceProvider.GetServices<IJobHandler>().FirstOrDefault(h => h.Type == job.Type);
        try
        {
            if (handler is null) throw new InvalidOperationException($"No handler registered for job type '{job.Type}'.");
            using var payload = JsonDocument.Parse(job.PayloadJson);
            await handler.HandleAsync(payload.RootElement.Clone(), ct);
            job.Status = JobStatus.Done;
            job.FinishedAt = Clock.Now;
            job.LastError = null;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Job {Id} ({Type}) failed on attempt {Attempt}", job.Id, job.Type, job.Attempts);
            job.LastError = TextUtil.Truncate(ex.ToString(), 3900);
            if (job.Attempts >= job.MaxAttempts)
            {
                job.Status = JobStatus.Failed;
                job.FinishedAt = Clock.Now;
            }
            else
            {
                job.Status = JobStatus.Queued;
                job.RunAt = Clock.Now.AddSeconds(Math.Pow(4, job.Attempts) * 5);
            }
        }
        // the handler used its own scoped DbContext; re-attach the job through a fresh context to avoid tracking clashes
        await using var save = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        var db2 = save.ServiceProvider.GetRequiredService<AadiDbContext>();
        await db2.BackgroundJobs.Where(j => j.Id == job.Id).ExecuteUpdateAsync(s => s
            .SetProperty(j => j.Status, job.Status)
            .SetProperty(j => j.FinishedAt, job.FinishedAt)
            .SetProperty(j => j.LastError, job.LastError)
            .SetProperty(j => j.RunAt, job.RunAt), ct);
        return true;
    }
}

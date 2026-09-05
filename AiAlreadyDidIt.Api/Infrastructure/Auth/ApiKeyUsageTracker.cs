using System.Collections.Concurrent;
using AiAlreadyDidIt.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Infrastructure.Auth;

/// <summary>Counts API-key requests in memory and flushes them to <c>api_keys</c> / <c>api_key_usage_daily</c> every 30 seconds.</summary>
public sealed class ApiKeyUsageTracker(IServiceScopeFactory scopeFactory, ILogger<ApiKeyUsageTracker> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<int, int> _requests = new();
    private readonly ConcurrentDictionary<int, int> _searches = new();

    public void Track(int apiKeyId, bool isSearch)
    {
        _requests.AddOrUpdate(apiKeyId, 1, (_, n) => n + 1);
        if (isSearch) _searches.AddOrUpdate(apiKeyId, 1, (_, n) => n + 1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); } catch (OperationCanceledException) { break; }
            await FlushAsync(CancellationToken.None);
        }
        await FlushAsync(CancellationToken.None);
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        if (_requests.IsEmpty) return;
        var snapshot = _requests.ToArray();
        foreach (var (id, _) in snapshot) _requests.TryRemove(id, out _);
        var searchSnapshot = _searches.ToArray();
        foreach (var (id, _) in searchSnapshot) _searches.TryRemove(id, out _);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AadiDbContext>();
            var today = Clock.Today;
            foreach (var (id, count) in snapshot)
            {
                var searches = searchSnapshot.FirstOrDefault(s => s.Key == id).Value;
                await db.ApiKeys.Where(k => k.Id == id).ExecuteUpdateAsync(s => s.SetProperty(k => k.RequestCount, k => k.RequestCount + count).SetProperty(k => k.LastUsedAt, Clock.Now), ct);
                var updated = await db.ApiKeyUsageDaily.Where(u => u.ApiKeyId == id && u.Date == today).ExecuteUpdateAsync(s => s.SetProperty(u => u.RequestCount, u => u.RequestCount + count).SetProperty(u => u.SearchCount, u => u.SearchCount + searches), ct);
                if (updated == 0) { db.ApiKeyUsageDaily.Add(new Entities.ApiKeyUsageDaily { ApiKeyId = id, Date = today, RequestCount = count, SearchCount = searches }); await db.SaveChangesAsync(ct); }
            }
        }
        catch (Exception ex) { logger.LogWarning(ex, "API key usage flush failed"); }
    }
}

public sealed class ApiKeyUsageMiddleware(RequestDelegate next, ApiKeyUsageTracker tracker)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
        if (context.Items.TryGetValue("ApiKeyId", out var id) && id is int keyId)
            tracker.Track(keyId, context.Request.Path.StartsWithSegments("/api/v1/catalog/apps") || context.Request.Path.StartsWithSegments("/api/v1/agent/check"));
    }
}

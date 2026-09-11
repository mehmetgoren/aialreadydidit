using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AiAlreadyDidIt.Api.Services.Site;

/// <summary>The mission counter: Σ distinct downloaders × capped generation estimate × reuse share, per published app (cached 2 minutes).</summary>
public sealed class SavingsService(AadiDbContext db, SiteSettingsCache settings, IMemoryCache cache)
{
    private const string CacheKey = "site:savings:v1";

    public async Task<SavingsDto> GetAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out SavingsDto? cached) && cached is not null) return cached;
        var coefficients = await settings.SavingsAsync(ct);
        var rows = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published)
            .Select(a => new { a.Id, a.EstGenerationTokens, a.DownloadCount }).ToListAsync(ct);
        var ids = rows.Select(r => r.Id).ToList();
        // A person (or agent) who downloads the same app twice did not skip two generations: count distinct downloaders per app.
        var uniqueByApp = (await db.Downloads.AsNoTracking().Where(d => ids.Contains(d.AppId))
                .Select(d => new { d.AppId, d.UserId, d.IpHash }).Distinct().ToListAsync(ct))
            .GroupBy(d => d.AppId).ToDictionary(g => g.Key, g => (long)g.Count());
        long tokens = coefficients.BaseTokens;
        long downloads = 0, unique = 0;
        foreach (var r in rows)
        {
            var u = Math.Min(uniqueByApp.GetValueOrDefault(r.Id, r.DownloadCount), r.DownloadCount);
            tokens += coefficients.Saved(r.EstGenerationTokens, u);
            downloads += r.DownloadCount;
            unique += u;
        }
        var dto = new SavingsDto
        {
            TokensSaved = tokens,
            CostSavedUsd = coefficients.Cost(tokens),
            KwhSaved = coefficients.Kwh(tokens),
            Co2SavedKg = coefficients.Co2Kg(tokens),
            TotalDownloads = downloads,
            UniqueDownloads = unique,
            ReuseShare = coefficients.ReuseShare,
            PublishedApps = rows.Count,
            ComputedAt = Clock.Now
        };
        cache.Set(CacheKey, dto, TimeSpan.FromMinutes(2));
        return dto;
    }

    public void Invalidate() => cache.Remove(CacheKey);
}

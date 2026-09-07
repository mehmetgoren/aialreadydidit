using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AiAlreadyDidIt.Api.Services.Site;

/// <summary>The mission counter: Σ downloads × estimated generation cost of each app (cached 2 minutes).</summary>
public sealed class SavingsService(AadiDbContext db, SiteSettingsCache settings, IMemoryCache cache)
{
    private const string CacheKey = "site:savings:v1";

    public async Task<SavingsDto> GetAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out SavingsDto? cached) && cached is not null) return cached;
        var coefficients = await settings.SavingsAsync(ct);
        var rows = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published)
            .Select(a => new { a.EstGenerationTokens, a.DownloadCount }).ToListAsync(ct);
        long tokens = coefficients.BaseTokens;
        long downloads = 0;
        foreach (var r in rows)
        {
            tokens += r.EstGenerationTokens * r.DownloadCount;
            downloads += r.DownloadCount;
        }
        var dto = new SavingsDto
        {
            TokensSaved = tokens,
            CostSavedUsd = coefficients.Cost(tokens),
            KwhSaved = coefficients.Kwh(tokens),
            Co2SavedKg = coefficients.Co2Kg(tokens),
            TotalDownloads = downloads,
            PublishedApps = rows.Count,
            ComputedAt = Clock.Now
        };
        cache.Set(CacheKey, dto, TimeSpan.FromMinutes(2));
        return dto;
    }

    public void Invalidate() => cache.Remove(CacheKey);
}

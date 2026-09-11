using System.Globalization;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AiAlreadyDidIt.Api.Infrastructure;

/// <summary>Read-through cache (60 s) over <c>site_settings</c> with typed accessors.</summary>
public sealed class SiteSettingsCache(IServiceScopeFactory scopeFactory, IMemoryCache cache)
{
    private const string CacheKey = "site-settings:v1";
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<IReadOnlyDictionary<string, string?>> AllAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out Dictionary<string, string?>? cached) && cached is not null) return cached;
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AadiDbContext>();
        var dict = await db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value, ct);
        cache.Set(CacheKey, dict, Ttl);
        return dict;
    }

    public void Invalidate() => cache.Remove(CacheKey);

    public async Task<string> GetStringAsync(string key, string fallback = "", CancellationToken ct = default) =>
        (await AllAsync(ct)).TryGetValue(key, out var v) && v is not null ? v : fallback;

    public async Task<int> GetIntAsync(string key, int fallback, CancellationToken ct = default) =>
        int.TryParse(await GetStringAsync(key, "", ct), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    public async Task<decimal> GetDecimalAsync(string key, decimal fallback, CancellationToken ct = default) =>
        decimal.TryParse(await GetStringAsync(key, "", ct), NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    public async Task<double> GetDoubleAsync(string key, double fallback, CancellationToken ct = default) =>
        double.TryParse(await GetStringAsync(key, "", ct), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    public async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken ct = default)
    {
        var s = await GetStringAsync(key, "", ct);
        return bool.TryParse(s, out var v) ? v : s == "1" || fallback && s.Length == 0;
    }

    /// <summary>Savings coefficients bundled for the estimator.</summary>
    public async Task<SavingsCoefficients> SavingsAsync(CancellationToken ct = default) => new(
        await GetDecimalAsync(SettingKeys.TokensPerLine, 12, ct),
        await GetDecimalAsync(SettingKeys.IterationFactor, 3, ct),
        await GetDecimalAsync(SettingKeys.PricePerMillionTokens, 6, ct),
        await GetDecimalAsync(SettingKeys.KwhPerMillionTokens, 0.3m, ct),
        await GetDecimalAsync(SettingKeys.Co2GramsPerKwh, 400, ct),
        await GetIntAsync(SettingKeys.SavingsBaseTokens, 0, ct),
        await GetDecimalAsync(SettingKeys.OverrideCapFactor, 5, ct),
        await GetDecimalAsync(SettingKeys.ReuseShare, 0.5m, ct));
}

/// <summary>
/// Coefficients of the savings counter. Deliberately conservative: uploader-supplied token totals are capped at
/// <see cref="OverrideCapFactor"/> × the size heuristic (agent session totals include cheap cache reads), and only
/// <see cref="ReuseShare"/> of the downloads are assumed to have replaced a fresh generation.
/// </summary>
public sealed record SavingsCoefficients(decimal TokensPerLine, decimal IterationFactor, decimal PricePerMillionTokens, decimal KwhPerMillionTokens, decimal Co2GramsPerKwh, long BaseTokens,
    decimal OverrideCapFactor = 5, decimal ReuseShare = 0.5m)
{
    /// <summary>Smallest source size the cap is computed from, so a tiny script cannot claim zero-based nonsense either way.</summary>
    public const int MinLinesForCap = 200;

    public long EstimateTokens(int lineCount) => (long)Math.Round(lineCount * TokensPerLine * IterationFactor);

    /// <summary>Tokens the counter uses for an uploader-supplied total: never more than the heuristic × <see cref="OverrideCapFactor"/>.</summary>
    public long CapOverride(long claimedTokens, int lineCount)
    {
        if (claimedTokens <= 0) return 0;
        var ceiling = (long)Math.Round(EstimateTokens(Math.Max(lineCount, MinLinesForCap)) * Math.Max(OverrideCapFactor, 1));
        return Math.Min(claimedTokens, ceiling);
    }

    /// <summary>Tokens saved by <paramref name="downloads"/> downloads of an app that costs <paramref name="tokensPerGeneration"/> to generate.</summary>
    public long Saved(long tokensPerGeneration, long downloads) => (long)Math.Round(tokensPerGeneration * downloads * Math.Clamp(ReuseShare, 0, 1));

    public decimal Cost(long tokens) => Math.Round(tokens / 1_000_000m * PricePerMillionTokens, 4);
    public decimal Kwh(long tokens) => Math.Round(tokens / 1_000_000m * KwhPerMillionTokens, 4);
    public decimal Co2Kg(long tokens) => Math.Round(Kwh(tokens) * Co2GramsPerKwh / 1000m, 4);
}

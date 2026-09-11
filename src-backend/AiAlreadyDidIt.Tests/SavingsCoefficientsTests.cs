using AiAlreadyDidIt.Api.Infrastructure;

namespace AiAlreadyDidIt.Tests;

public class SavingsCoefficientsTests
{
    // Defaults from SiteSettingsCache.SavingsAsync: 12 tokens/line × 3 iterations, $6 / M tokens, 0.3 kWh / M tokens, 400 g CO₂ / kWh,
    // uploader claims capped at 5 × the heuristic, half of the downloads assumed to replace a generation.
    private static readonly SavingsCoefficients Defaults = new(12, 3, 6, 0.3m, 400, 0);

    [Fact]
    public void Estimate_tokens_is_lines_times_tokens_per_line_times_iterations()
    {
        Assert.Equal(36, Defaults.EstimateTokens(1));
        Assert.Equal(74_520, Defaults.EstimateTokens(2070));   // the CPU-Z seed archive
        Assert.Equal(0, Defaults.EstimateTokens(0));
    }

    [Fact]
    public void Cost_energy_and_co2_scale_per_million_tokens()
    {
        Assert.Equal(6m, Defaults.Cost(1_000_000));
        Assert.Equal(0.3m, Defaults.Kwh(1_000_000));
        Assert.Equal(0.12m, Defaults.Co2Kg(1_000_000));         // 0.3 kWh × 400 g = 120 g
        Assert.Equal(0.6m, Defaults.Cost(100_000));
        Assert.Equal(0m, Defaults.Cost(0));
    }

    [Fact]
    public void Values_are_rounded_to_four_decimals()
    {
        Assert.Equal(0.0001m, Defaults.Cost(12));               // 0.000072 → 0.0001
        Assert.Equal(0m, Defaults.Kwh(12));                     // 0.0000036 → 0
    }

    [Fact]
    public void Custom_coefficients_are_honoured()
    {
        var c = new SavingsCoefficients(10, 1, 100, 1, 1000, 500);
        Assert.Equal(500, c.BaseTokens);
        Assert.Equal(1000, c.EstimateTokens(100));
        Assert.Equal(0.1m, c.Cost(1000));
        Assert.Equal(0.001m, c.Kwh(1000));
        Assert.Equal(0.001m, c.Co2Kg(1000));
    }

    [Fact]
    public void Uploader_claims_are_capped_at_the_heuristic_times_the_cap_factor()
    {
        // Mint Paint on production: 5 871 lines, 151 531 580 tokens claimed (a Claude Code total including cache reads).
        Assert.Equal(1_056_780, Defaults.CapOverride(151_531_580, 5871));   // 5871 × 36 × 5
        Assert.Equal(90_000, Defaults.CapOverride(90_000, 2655));           // honest claims below the cap pass through
        Assert.Equal(0, Defaults.CapOverride(0, 2655));
        Assert.Equal(0, Defaults.CapOverride(-5, 2655));
    }

    [Fact]
    public void Cap_uses_a_floor_of_200_lines_so_unanalysed_or_tiny_sources_still_get_a_ceiling()
    {
        Assert.Equal(36_000, Defaults.CapOverride(999_999, 0));            // 200 × 36 × 5
        Assert.Equal(36_000, Defaults.CapOverride(999_999, 40));
        var noCap = new SavingsCoefficients(12, 3, 6, 0.3m, 400, 0, OverrideCapFactor: 0.2m);
        Assert.Equal(7_200, noCap.CapOverride(999_999, 200));               // factor below 1 is treated as 1
    }

    [Fact]
    public void Saved_applies_the_reuse_share_and_rounds()
    {
        Assert.Equal(50_000, Defaults.Saved(100_000, 1));
        Assert.Equal(200_000, Defaults.Saved(100_000, 4));
        Assert.Equal(0, Defaults.Saved(100_000, 0));
        var all = new SavingsCoefficients(12, 3, 6, 0.3m, 400, 0, ReuseShare: 1);
        Assert.Equal(400_000, all.Saved(100_000, 4));
        var overShare = new SavingsCoefficients(12, 3, 6, 0.3m, 400, 0, ReuseShare: 7);
        Assert.Equal(400_000, overShare.Saved(100_000, 4));                 // clamped to 1
    }
}

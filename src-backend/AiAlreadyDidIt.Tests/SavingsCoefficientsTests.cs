using AiAlreadyDidIt.Api.Infrastructure;

namespace AiAlreadyDidIt.Tests;

public class SavingsCoefficientsTests
{
    // Defaults from SiteSettingsCache.SavingsAsync: 12 tokens/line × 3 iterations, $15 / M tokens, 0.4 kWh / M tokens, 400 g CO₂ / kWh.
    private static readonly SavingsCoefficients Defaults = new(12, 3, 15, 0.4m, 400, 0);

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
        Assert.Equal(15m, Defaults.Cost(1_000_000));
        Assert.Equal(0.4m, Defaults.Kwh(1_000_000));
        Assert.Equal(0.16m, Defaults.Co2Kg(1_000_000));         // 0.4 kWh × 400 g = 160 g
        Assert.Equal(1.5m, Defaults.Cost(100_000));
        Assert.Equal(0m, Defaults.Cost(0));
    }

    [Fact]
    public void Values_are_rounded_to_four_decimals()
    {
        Assert.Equal(0.0002m, Defaults.Cost(12));               // 0.00018 → 0.0002
        Assert.Equal(0m, Defaults.Kwh(12));                     // 0.0000048 → 0
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
}

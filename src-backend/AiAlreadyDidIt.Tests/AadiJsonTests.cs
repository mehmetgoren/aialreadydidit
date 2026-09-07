using System.Text.Json;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;

namespace AiAlreadyDidIt.Tests;

public class AadiJsonTests
{
    private sealed record Sample(string SomeName, AppStatus Status, DateTime When, int Count, Dictionary<string, int> ByKey);

    [Fact]
    public void Serializes_camel_case_enums_as_strings_and_utc_timestamps()
    {
        var when = new DateTime(2026, 9, 6, 12, 30, 45, 123, DateTimeKind.Utc);
        var json = JsonSerializer.Serialize(new Sample("x", AppStatus.PendingReview, when, 3, new() { ["SomeKey"] = 1 }), AadiJson.Options);
        Assert.Contains("\"someName\":\"x\"", json);
        Assert.Contains("\"status\":\"pendingReview\"", json);
        Assert.Contains("\"when\":\"2026-09-06T12:30:45.123Z\"", json);
        Assert.Contains("\"someKey\":1", json);
    }

    [Fact]
    public void Unspecified_kind_is_treated_as_utc_and_local_is_converted()
    {
        var unspecified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        Assert.Contains("2026-01-01T00:00:00.000Z", JsonSerializer.Serialize(unspecified, AadiJson.Options));
        var local = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local);
        Assert.Contains(local.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'"), JsonSerializer.Serialize(local, AadiJson.Options));
    }

    [Fact]
    public void Deserializes_numbers_from_strings_and_returns_utc_dates()
    {
        var sample = JsonSerializer.Deserialize<Sample>("""{"someName":"y","status":"published","when":"2026-09-06T10:00:00+02:00","count":"7","byKey":{}}""", AadiJson.Options)!;
        Assert.Equal(7, sample.Count);
        Assert.Equal(AppStatus.Published, sample.Status);
        Assert.Equal(DateTimeKind.Utc, sample.When.Kind);
        Assert.Equal(new DateTime(2026, 9, 6, 8, 0, 0, DateTimeKind.Utc), sample.When);
    }

    [Fact]
    public void Null_properties_are_kept_in_output()
    {
        var json = JsonSerializer.Serialize(new { Value = (string?)null }, AadiJson.Options);
        Assert.Equal("{\"value\":null}", json);
    }
}

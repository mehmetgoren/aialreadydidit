namespace AiAlreadyDidIt.Api.Infrastructure;

/// <summary>All persisted timestamps are UTC.</summary>
public static class Clock
{
    public static DateTime Now => DateTime.UtcNow;
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}

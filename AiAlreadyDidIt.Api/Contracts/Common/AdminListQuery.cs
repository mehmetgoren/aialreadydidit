namespace AiAlreadyDidIt.Api.Contracts.Common;

/// <summary>Common query string for admin list endpoints (search + paging + sorting).</summary>
public class AdminListQuery
{
    public string? Q { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    /// <summary>Column key to sort by (endpoint-specific whitelist).</summary>
    public string? Sort { get; set; }
    /// <summary>asc | desc</summary>
    public string Dir { get; set; } = "desc";
    /// <summary>Optional status filter (endpoint-specific).</summary>
    public string? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public int SafePage => Page < 1 ? 1 : Page;
    public int SafePageSize => PageSize < 1 ? 25 : Math.Min(PageSize, 200);
    public bool Descending => !string.Equals(Dir, "asc", StringComparison.OrdinalIgnoreCase);
}

using System.ComponentModel.DataAnnotations;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Entities;

namespace AiAlreadyDidIt.Api.Contracts.Admin;

public class AdminMenuItemDto { public string Label { get; set; } = string.Empty; public string? Icon { get; set; } public string? Route { get; set; } public List<AdminMenuItemDto> Children { get; set; } = []; }

public class AdminDashboardDto
{
    public int PendingReview { get; set; }
    public int PendingScan { get; set; }
    public int OpenReports { get; set; }
    public int OpenRequests { get; set; }
    public int PublishedApps { get; set; }
    public int TotalApps { get; set; }
    public int Members { get; set; }
    public int NewMembers7d { get; set; }
    public long Downloads { get; set; }
    public int Downloads7d { get; set; }
    public long Searches { get; set; }
    public int Searches7d { get; set; }
    public int ZeroResultSearches7d { get; set; }
    public int AgentCalls7d { get; set; }
    public int FailedJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int InfectedFiles { get; set; }
    public int ProposedCategories { get; set; }
    public SavingsDto Savings { get; set; } = new();
    public List<DailyPointDto> DownloadsSeries { get; set; } = [];
    public List<DailyPointDto> SearchesSeries { get; set; } = [];
    public List<DailyPointDto> SignupsSeries { get; set; } = [];
    public List<AppCardDto> TopApps { get; set; } = [];
    public List<ModerationQueueItemDto> QueuePreview { get; set; } = [];
    public List<AdminReportDto> RecentReports { get; set; } = [];
}

public class DailyPointDto { public DateOnly Date { get; set; } public int Value { get; set; } }

public class StatsOverviewDto
{
    public List<DailyPointDto> Downloads { get; set; } = [];
    public List<DailyPointDto> Searches { get; set; } = [];
    public List<DailyPointDto> Signups { get; set; } = [];
    public List<DailyPointDto> Uploads { get; set; } = [];
    public List<DailyPointDto> Views { get; set; } = [];
    public List<FacetItemDto> DownloadsByPlatform { get; set; } = [];
    public List<FacetItemDto> DownloadsBySource { get; set; } = [];
    public List<FacetItemDto> AppsByCategory { get; set; } = [];
    public List<FacetItemDto> AppsByModel { get; set; } = [];
    public List<FacetItemDto> AppsByLicense { get; set; } = [];
    public List<AppCardDto> TopApps { get; set; } = [];
    public List<TopUploaderDto> TopUploaders { get; set; } = [];
}

public class TopUploaderDto { public string Username { get; set; } = string.Empty; public string DisplayName { get; set; } = string.Empty; public int Apps { get; set; } public int Downloads { get; set; } }

public class SearchAnalyticsDto
{
    public List<QueryStatDto> TopQueries { get; set; } = [];
    public List<QueryStatDto> ZeroResultQueries { get; set; } = [];
    public List<FacetItemDto> BySource { get; set; } = [];
    public List<FacetItemDto> ByMode { get; set; } = [];
    public double AvgTookMs { get; set; }
    public int Total { get; set; }
    public List<DailyPointDto> Series { get; set; } = [];
}

public class QueryStatDto { public string Query { get; set; } = string.Empty; public int Count { get; set; } public int AvgResults { get; set; } public double? AvgTopSimilarity { get; set; } public DateTime LastSeen { get; set; } }

public class SavingsBreakdownDto
{
    public SavingsDto Totals { get; set; } = new();
    public List<DailyPointDto> TokensSeries { get; set; } = [];
    public List<AppSavingsDto> TopApps { get; set; } = [];
    public Dictionary<string, decimal> Coefficients { get; set; } = [];
}

public class AppSavingsDto { public int AppId { get; set; } public string Slug { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public int Downloads { get; set; } public long TokensPerDownload { get; set; } public long TokensSaved { get; set; } public decimal CostSavedUsd { get; set; } public bool IsOverride { get; set; } }

public class AuditLogDto { public long Id { get; set; } public int? UserId { get; set; } public string Username { get; set; } = string.Empty; public string Action { get; set; } = string.Empty; public string? Entity { get; set; } public string? EntityId { get; set; } public string? Details { get; set; } public string? IpAddress { get; set; } public DateTime CreatedAt { get; set; } }

public class JobDto { public long Id { get; set; } public string Type { get; set; } = string.Empty; public string PayloadJson { get; set; } = string.Empty; public JobStatus Status { get; set; } public int Attempts { get; set; } public int MaxAttempts { get; set; } public string? LastError { get; set; } public DateTime RunAt { get; set; } public DateTime? StartedAt { get; set; } public DateTime? FinishedAt { get; set; } public DateTime CreatedAt { get; set; } public string? Subject { get; set; } }

public class SiteSettingDto { public string Key { get; set; } = string.Empty; public string? Value { get; set; } public string Group { get; set; } = string.Empty; public string ValueType { get; set; } = string.Empty; public string? Description { get; set; } public DateTime UpdatedAt { get; set; } }
public class SaveSettingsRequest { public Dictionary<string, string?> Values { get; set; } = []; }

public class AdminBannerDto { public int Id { get; set; } public string Title { get; set; } = string.Empty; public string? Subtitle { get; set; } public string? ImageUrl { get; set; } public string? Link { get; set; } public string Position { get; set; } = "hero"; public int SortOrder { get; set; } public bool IsActive { get; set; } public DateTime? StartsAt { get; set; } public DateTime? EndsAt { get; set; } public DateTime CreatedAt { get; set; } }
public class SaveBannerRequest
{
    [Required, MaxLength(160)] public string Title { get; set; } = string.Empty;
    [MaxLength(300)] public string? Subtitle { get; set; }
    [MaxLength(512)] public string? Link { get; set; }
    public string Position { get; set; } = "hero";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}

public class FeaturedItemDto : AppCardDto { public int FeaturedOrder { get; set; } public string? FeaturedNote { get; set; } }
public class SaveFeaturedRequest { public List<int> OrderedAppIds { get; set; } = []; }
public class FeatureAppRequest { public int AppId { get; set; } [MaxLength(200)] public string? Note { get; set; } }

public class HealthCheckDto { public string Name { get; set; } = string.Empty; public bool Ok { get; set; } public string? Detail { get; set; } public double? LatencyMs { get; set; } }
public class SystemHealthDto
{
    public List<HealthCheckDto> Checks { get; set; } = [];
    public Dictionary<string, string> Config { get; set; } = [];
    public string Version { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public string Environment { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Entities;

namespace AiAlreadyDidIt.Api.Contracts.Dashboard;

public class DownloadHistoryDto
{
    public long Id { get; set; }
    public AppCardDto App { get; set; } = null!;
    public string Version { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? PlatformCode { get; set; }
    public int FileId { get; set; }
    public DownloadSource Source { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Rated { get; set; }
}

public class SaveCollectionRequest
{
    [Required, MaxLength(80)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    public bool IsPublic { get; set; }
}

public class CollectionItemRequest
{
    public int AppId { get; set; }
    [MaxLength(500)] public string? Note { get; set; }
}

public class WatchRequest
{
    public bool NotifyNewVersion { get; set; } = true;
    public bool NotifyReplies { get; set; } = true;
}

public class NotificationDto
{
    public long Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? Link { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ApiKeyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public string Scopes { get; set; } = string.Empty;
    public string RateTier { get; set; } = string.Empty;
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public long RequestCount { get; set; }
    public long DownloadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Only returned once, right after creation.</summary>
    public string? Secret { get; set; }
}

public class CreateApiKeyRequest
{
    [Required, MaxLength(80)] public string Name { get; set; } = string.Empty;
    /// <summary>Subset of: read, download, submit.</summary>
    public List<string>? Scopes { get; set; }
    public int? ExpiresInDays { get; set; }
}

public class WatchDto
{
    public int Id { get; set; }
    public AppCardDto App { get; set; } = null!;
    public bool NotifyNewVersion { get; set; }
    public bool NotifyReplies { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashboardOverviewDto
{
    public int AppCount { get; set; }
    public int PublishedAppCount { get; set; }
    public int PendingAppCount { get; set; }
    public int TotalDownloads { get; set; }
    public long TokensSavedByMyApps { get; set; }
    public decimal CostSavedByMyApps { get; set; }
    public int RatingCount { get; set; }
    public int FavoriteCount { get; set; }
    public int CollectionCount { get; set; }
    public int WatchCount { get; set; }
    public int UnreadNotifications { get; set; }
    public int ApiKeyCount { get; set; }
    public List<NotificationDto> RecentNotifications { get; set; } = [];
    public List<AppCardDto> RecentApps { get; set; } = [];
}

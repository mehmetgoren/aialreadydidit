using Pgvector;

namespace AiAlreadyDidIt.Api.Entities;

public enum DownloadSource { Web = 1, Api = 2, Mcp = 3, Seo = 4 }

/// <summary>One download event. Also the proof that lets a member rate the app.</summary>
public class Download
{
    public long Id { get; set; }
    public int AppId { get; set; }
    public int VersionId { get; set; }
    public int FileId { get; set; }
    public int? UserId { get; set; }
    public int? ApiKeyId { get; set; }
    public DownloadSource Source { get; set; }
    public string? IpHash { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum RatingStatus { Visible = 1, Hidden = 2 }

/// <summary>Score out of 100 + short review + "worked / didn't work on version X".</summary>
public class Rating
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int Score { get; set; }
    public string? Review { get; set; }
    public int? VersionId { get; set; }
    public AppVersion? Version { get; set; }
    public bool? Worked { get; set; }
    public int HelpfulCount { get; set; }
    public RatingStatus Status { get; set; } = RatingStatus.Visible;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<RatingReply> Replies { get; set; } = [];
}

/// <summary>Uploader (or admin) reply under a review — Play Store "developer response".</summary>
public class RatingReply
{
    public int Id { get; set; }
    public int RatingId { get; set; }
    public Rating Rating { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RatingVote
{
    public int RatingId { get; set; }
    public int UserId { get; set; }
    public bool IsHelpful { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Favorite
{
    public int UserId { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class Collection
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<CollectionItem> Items { get; set; } = [];
}

public class CollectionItem
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public string? Note { get; set; }
    public int SortOrder { get; set; }
    public DateTime AddedAt { get; set; }
}

/// <summary>"Notify me on new versions of this app."</summary>
public class AppWatch
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public bool NotifyNewVersion { get; set; } = true;
    public bool NotifyReplies { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public enum NotificationType
{
    NewVersion = 1,
    ModerationApproved = 2,
    ModerationRejected = 3,
    ReviewReply = 4,
    NewReview = 5,
    ReportResolved = 6,
    System = 7,
    ScanInfected = 8,
    RequestFulfilled = 9
}

public class Notification
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? Link { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum ReportReason { Malware = 1, NotOpenSource = 2, NotFree = 3, Copyright = 4, Broken = 5, Spam = 6, Inappropriate = 7, Other = 8 }
public enum ReportStatus { Open = 1, Reviewing = 2, Resolved = 3, Dismissed = 4 }

public class Report
{
    public int Id { get; set; }
    public int? AppId { get; set; }
    public App? App { get; set; }
    public int? RatingId { get; set; }
    public int? ReporterUserId { get; set; }
    public string? ReporterEmail { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public int? HandledByUserId { get; set; }
    public string? Resolution { get; set; }
    public string? IpHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public enum SearchMode { Keyword = 1, Semantic = 2, Hybrid = 3 }
public enum RequestSource { Web = 1, Api = 2, Mcp = 3 }

/// <summary>Every search, so the admin can see what people (and agents) look for and do not find.</summary>
public class SearchLog
{
    public long Id { get; set; }
    public string Query { get; set; } = string.Empty;
    public SearchMode Mode { get; set; }
    public string? FiltersJson { get; set; }
    public int ResultCount { get; set; }
    public double? TopSimilarity { get; set; }
    public RequestSource Source { get; set; }
    public int? UserId { get; set; }
    public int? ApiKeyId { get; set; }
    public int TookMs { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum AppRequestStatus { Open = 1, Fulfilled = 2, Closed = 3 }

/// <summary>"Wanted" board: something a person or an agent looked for and did not find.</summary>
public class AppRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
    public int? RequesterUserId { get; set; }
    public User? Requester { get; set; }
    public int? ApiKeyId { get; set; }
    public RequestSource Source { get; set; }
    public AppRequestStatus Status { get; set; } = AppRequestStatus.Open;
    public int? FulfilledByAppId { get; set; }
    public App? FulfilledByApp { get; set; }
    public int VoteCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AppRequestVote
{
    public int RequestId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

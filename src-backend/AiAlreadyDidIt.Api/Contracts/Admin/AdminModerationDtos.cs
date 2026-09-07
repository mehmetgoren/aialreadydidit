using System.ComponentModel.DataAnnotations;
using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Entities;

namespace AiAlreadyDidIt.Api.Contracts.Admin;

public class ModerationQueueItemDto
{
    public int AppId { get; set; }
    public int? VersionId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? CoverUrl { get; set; }
    public string Kind { get; set; } = "app"; // app | version
    public string? Version { get; set; }
    public AppStatus AppStatus { get; set; }
    public VersionStatus? VersionStatus { get; set; }
    public string UploaderUsername { get; set; } = string.Empty;
    public int UploaderTrustLevel { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string LicenseSpdxId { get; set; } = string.Empty;
    public bool LicenseOk { get; set; }
    public int FileCount { get; set; }
    public int InfectedCount { get; set; }
    public int PendingScanCount { get; set; }
    public int ScreenshotCount { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int OpenReports { get; set; }
}

public class ModerationDetailDto
{
    public AppDraftDto Draft { get; set; } = null!;
    public AppDetailDto Preview { get; set; } = null!;
    public List<ModerationActionDto> History { get; set; } = [];
    public List<ScanResultDto> ScanResults { get; set; } = [];
    public List<AppCardDto> SimilarApps { get; set; } = [];
    public List<AdminReportDto> Reports { get; set; } = [];
    public CategoryNodeDto? LlmSuggestedCategory { get; set; }
    public string? LlmSuggestedCategoryProposedName { get; set; }
    public string? LicenseIssue { get; set; }
    public AdminUserSummaryDto Uploader { get; set; } = null!;
}

public class ModerationActionDto
{
    public int Id { get; set; }
    public int? VersionId { get; set; }
    public string AdminUsername { get; set; } = string.Empty;
    public ModerationActionKind Action { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ScanResultDto
{
    public long Id { get; set; }
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public ScanStatus Verdict { get; set; }
    public string? Signature { get; set; }
    public string? Raw { get; set; }
    public DateTime ScannedAt { get; set; }
}

public class ModerationDecisionRequest
{
    [MaxLength(2000)] public string? Note { get; set; }
    public int? VersionId { get; set; }
    /// <summary>Optional: move the app to this category while approving.</summary>
    public int? CategoryId { get; set; }
    public bool Feature { get; set; }
}

public class AdminUserSummaryDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Role { get; set; } = string.Empty;
    public int TrustLevel { get; set; }
    public bool IsBanned { get; set; }
    public bool IsActive { get; set; }
    public int AppCount { get; set; }
    public int PublishedAppCount { get; set; }
    public int RejectedAppCount { get; set; }
    public int ReportCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminAppRowDto : AppCardDto
{
    public string UploaderEmail { get; set; } = string.Empty;
    public int VersionCount { get; set; }
    public int OpenReports { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool EmbeddingStale { get; set; }
    public bool HasEmbedding { get; set; }
    public int ViewCount { get; set; }
}

public class AdminAppUpdateRequest : SaveDraftRequest
{
    public bool? IsFeatured { get; set; }
    public int? FeaturedOrder { get; set; }
    [MaxLength(200)] public string? FeaturedNote { get; set; }
    public bool? LicenseVerifiedByAdmin { get; set; }
    public string? Slug { get; set; }
}

public class AdminReportDto
{
    public int Id { get; set; }
    public int? AppId { get; set; }
    public string? AppSlug { get; set; }
    public string? AppName { get; set; }
    public AppStatus? AppStatus { get; set; }
    public int? RatingId { get; set; }
    public string? RatingReview { get; set; }
    public string? ReporterUsername { get; set; }
    public string? ReporterEmail { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public ReportStatus Status { get; set; }
    public string? HandledBy { get; set; }
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class ResolveReportRequest
{
    [Required] public ReportStatus Status { get; set; }
    [MaxLength(2000)] public string? Resolution { get; set; }
    /// <summary>none | unlist | remove | hide_review | ban_uploader</summary>
    public string? Action { get; set; }
}

public class AdminRequestDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RequesterUsername { get; set; }
    public RequestSource Source { get; set; }
    public AppRequestStatus Status { get; set; }
    public string? FulfilledByAppName { get; set; }
    public string? FulfilledByAppSlug { get; set; }
    public int VoteCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

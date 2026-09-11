using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Entities;

namespace AiAlreadyDidIt.Api.Contracts.Catalog;

public class CategoryNodeDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameTr { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int Level { get; set; }
    public int? ParentId { get; set; }
    /// <summary>Published apps in this node and all descendants.</summary>
    public int AppCount { get; set; }
    public List<CategoryNodeDto> Children { get; set; } = [];
}

public class CategoryDetailDto
{
    public CategoryNodeDto Node { get; set; } = null!;
    public List<CategoryNodeDto> Ancestors { get; set; } = [];
    public List<CategoryNodeDto> Children { get; set; } = [];
}

public class PlatformDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string AllowedExtensions { get; set; } = string.Empty;
    public bool AllowsExternalReference { get; set; }
    public string? InstallHint { get; set; }
}

public class LicenseDto
{
    public int Id { get; set; }
    public string SpdxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Url { get; set; }
    public bool IsOsiApproved { get; set; }
    public bool IsAllowed { get; set; }
    public int AppCount { get; set; }
}

public class LlmModelDto
{
    public int Id { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int AppCount { get; set; }
}

public class TagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

/// <summary>Card shown in grids, carousels and search results.</summary>
public class AppCardDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? CoverUrl { get; set; }
    public string CategorySlug { get; set; } = string.Empty;
    public string CategoryNameEn { get; set; } = string.Empty;
    public string CategoryNameTr { get; set; } = string.Empty;
    public string LicenseSpdxId { get; set; } = string.Empty;
    public string? LlmModelName { get; set; }
    public string? LlmModelSlug { get; set; }
    public List<string> Platforms { get; set; } = [];
    public double RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public int DownloadCount { get; set; }
    public string? LatestVersion { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UploaderUsername { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public long EstGenerationTokens { get; set; }
    public decimal EstGenerationCostUsd { get; set; }
    /// <summary>Set by semantic search (0..1).</summary>
    public double? Similarity { get; set; }
    public AppStatus Status { get; set; }
    public int? DerivedFromAppId { get; set; }
}

public class AppQuery
{
    public string? Q { get; set; }
    /// <summary>keyword | semantic | hybrid (default hybrid when q is present).</summary>
    public string? Mode { get; set; }
    public string? Category { get; set; }
    public string? Platform { get; set; }
    public string? License { get; set; }
    public string? Model { get; set; }
    public int? MinRating { get; set; }
    public string? Tags { get; set; }
    public string? Uploader { get; set; }
    /// <summary>relevance | downloads | rating | newest | updated | name</summary>
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public bool Featured { get; set; }
}

public class FacetItemDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class AppSearchResultDto
{
    public PagedResult<AppCardDto> Page { get; set; } = new();
    public List<FacetItemDto> Platforms { get; set; } = [];
    public List<FacetItemDto> Licenses { get; set; } = [];
    public List<FacetItemDto> Models { get; set; } = [];
    public List<FacetItemDto> Categories { get; set; } = [];
    public string ModeUsed { get; set; } = "keyword";
    public bool SemanticAvailable { get; set; }
}

public class ScreenshotDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbUrl { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
}

public class AppFileDto
{
    public int Id { get; set; }
    public int VersionId { get; set; }
    public string? PlatformCode { get; set; }
    public string? PlatformName { get; set; }
    public FileKind Kind { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public string? ContentType { get; set; }
    public ScanStatus ScanStatus { get; set; }
    public string? ScanSignature { get; set; }
    public string? InstallHint { get; set; }
    public int DownloadCount { get; set; }
    /// <summary>API path that records the download and redirects to the file.</summary>
    public string DownloadUrl { get; set; } = string.Empty;
}

public class AppVersionDto
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? Changelog { get; set; }
    public DateTime ReleasedAt { get; set; }
    public string? SourceRef { get; set; }
    public VersionStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public int DownloadCount { get; set; }
    public List<AppFileDto> Files { get; set; } = [];
}

public class AppPromptDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UploaderDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public int TrustLevel { get; set; }
    public int AppCount { get; set; }
    public int TotalDownloads { get; set; }
    public DateTime MemberSince { get; set; }
}

public class LineageNodeDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public DerivationKind? DerivationKind { get; set; }
    public List<LineageNodeDto> Derivatives { get; set; } = [];
}

public class AppDetailDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string? ReadmeMarkdown { get; set; }
    public string? IconUrl { get; set; }
    public string? HomepageUrl { get; set; }
    public AppStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public CategoryNodeDto Category { get; set; } = null!;
    public List<CategoryNodeDto> CategoryPath { get; set; } = [];
    public LicenseDto License { get; set; } = null!;
    public LlmModelDto? LlmModel { get; set; }
    public string? LlmModelNote { get; set; }
    public UploaderDto Uploader { get; set; } = null!;
    public SourceKind SourceKind { get; set; }
    public string? RepoUrl { get; set; }
    public string? RepoProvider { get; set; }
    public int? RepoStars { get; set; }
    public string? RepoPrimaryLanguage { get; set; }
    public DateTime? RepoSyncedAt { get; set; }
    public List<string> Platforms { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<ScreenshotDto> Screenshots { get; set; } = [];
    public List<AppVersionDto> Versions { get; set; } = [];
    public AppVersionDto? LatestVersion { get; set; }
    public List<AppPromptDto> Prompts { get; set; } = [];
    public LineageNodeDto? DerivedFrom { get; set; }
    public DerivationKind? DerivationKind { get; set; }
    public List<LineageNodeDto> Derivatives { get; set; } = [];
    public double RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public int WorkedCount { get; set; }
    public int NotWorkedCount { get; set; }
    public int DownloadCount { get; set; }
    public int ViewCount { get; set; }
    public int FavoriteCount { get; set; }
    public bool IsFeatured { get; set; }
    public long EstGenerationTokens { get; set; }
    public decimal EstGenerationCostUsd { get; set; }
    public decimal EstSavedCostUsd { get; set; }
    public long EstSavedTokens { get; set; }
    public int SourceLineCount { get; set; }
    public int SourceFileCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    /// <summary>Signed-in viewer state.</summary>
    public ViewerStateDto Viewer { get; set; } = new();
}

public class ViewerStateDto
{
    public bool IsFavorite { get; set; }
    public bool IsWatching { get; set; }
    public bool HasDownloaded { get; set; }
    public bool CanRate { get; set; }
    public bool IsOwner { get; set; }
    public int? MyRatingId { get; set; }
    public List<int> CollectionIds { get; set; } = [];
}

public class HomeDto
{
    public SavingsDto Savings { get; set; } = new();
    public List<AppCardDto> Featured { get; set; } = [];
    public List<AppCardDto> Trending { get; set; } = [];
    public List<AppCardDto> Newest { get; set; } = [];
    public List<AppCardDto> RecentlyUpdated { get; set; } = [];
    public List<AppCardDto> TopRated { get; set; } = [];
    public List<CategoryNodeDto> Categories { get; set; } = [];
    public List<BannerDto> Banners { get; set; } = [];
    public int OpenRequestCount { get; set; }
    public HomeStatsDto Stats { get; set; } = new();
}

public class HomeStatsDto
{
    public int PublishedApps { get; set; }
    public int Members { get; set; }
    public long Downloads { get; set; }
    public long Searches { get; set; }
    public int AgentSearches { get; set; }
}

public class BannerDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }
    public string? Link { get; set; }
    public string Position { get; set; } = "hero";
}

/// <summary>Mission counter: estimated tokens / money / energy the store has saved so far.</summary>
public class SavingsDto
{
    public long TokensSaved { get; set; }
    public decimal CostSavedUsd { get; set; }
    public decimal KwhSaved { get; set; }
    public decimal Co2SavedKg { get; set; }
    public long TotalDownloads { get; set; }
    /// <summary>Distinct (user, IP) downloaders summed over apps — what the counter actually multiplies.</summary>
    public long UniqueDownloads { get; set; }
    /// <summary>Share of downloads assumed to replace a fresh generation (admin setting).</summary>
    public decimal ReuseShare { get; set; }
    public int PublishedApps { get; set; }
    public DateTime ComputedAt { get; set; }
}

public class RatingDto
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string AppSlug { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Score { get; set; }
    public string? Review { get; set; }
    public string? Version { get; set; }
    public bool? Worked { get; set; }
    public int HelpfulCount { get; set; }
    public bool? MyVote { get; set; }
    public bool IsMine { get; set; }
    public RatingStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<RatingReplyDto> Replies { get; set; } = [];
}

public class RatingReplyDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsUploader { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RatingSummaryDto
{
    public double Average { get; set; }
    public int Count { get; set; }
    public int WorkedCount { get; set; }
    public int NotWorkedCount { get; set; }
    /// <summary>Buckets 0-19, 20-39, 40-59, 60-79, 80-100.</summary>
    public int[] Histogram { get; set; } = new int[5];
}

public class CollectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsPublic { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<AppCardDto> Items { get; set; } = [];
}

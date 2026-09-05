using System.ComponentModel.DataAnnotations;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Entities;

namespace AiAlreadyDidIt.Api.Contracts.Apps;

public class SaveDraftRequest
{
    [MaxLength(120)] public string? Name { get; set; }
    [MaxLength(200)] public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public int? CategoryId { get; set; }
    public int? LicenseId { get; set; }
    public int? LlmModelId { get; set; }
    [MaxLength(200)] public string? LlmModelNote { get; set; }
    [MaxLength(512)] public string? HomepageUrl { get; set; }
    public List<string>? Tags { get; set; }
    public int? DerivedFromAppId { get; set; }
    public DerivationKind? DerivationKind { get; set; }
    public List<PromptInput>? Prompts { get; set; }
    /// <summary>Real numbers from the uploader's own session (optional; overrides the heuristic).</summary>
    public long? EstGenerationTokens { get; set; }
    public decimal? EstGenerationCostUsd { get; set; }
}

public class PromptInput
{
    [MaxLength(160)] public string Title { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
}

public class InspectRepositoryRequest
{
    [Required] public string RepoUrl { get; set; } = string.Empty;
}

public class AttachRepositoryRequest
{
    [Required] public string RepoUrl { get; set; } = string.Empty;
    /// <summary>Tag / branch / commit to snapshot (default branch when empty).</summary>
    public string? SourceRef { get; set; }
    /// <summary>Pre-fill description / README / license / tags from the repository metadata.</summary>
    public bool Prefill { get; set; } = true;
}

public class RepositoryInspectionDto
{
    public string Provider { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Homepage { get; set; }
    public string DefaultBranch { get; set; } = string.Empty;
    public int Stars { get; set; }
    public string? PrimaryLanguage { get; set; }
    public string? LicenseSpdxId { get; set; }
    public int? LicenseId { get; set; }
    public string? DetectedLicenseSpdxId { get; set; }
    public bool HasReadme { get; set; }
    public string? ReadmeExcerpt { get; set; }
    public List<string> Topics { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public List<ReleaseDto> Releases { get; set; } = [];
    public bool IsArchived { get; set; }
    public List<string> Warnings { get; set; } = [];
}

public class ReleaseDto
{
    public string Tag { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Body { get; set; }
    public DateTime? PublishedAt { get; set; }
    public List<ReleaseAssetDto> Assets { get; set; } = [];
}

public class ReleaseAssetDto
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? SuggestedPlatform { get; set; }
}

public class SaveVersionRequest
{
    [Required, MaxLength(64)] public string Version { get; set; } = string.Empty;
    public string? Changelog { get; set; }
    public DateTime? ReleasedAt { get; set; }
    [MaxLength(200)] public string? SourceRef { get; set; }
}

public class AddExternalFileRequest
{
    [Required] public string PlatformCode { get; set; } = string.Empty;
    /// <summary>Docker image reference (ghcr.io/user/app:1.0) or the URL of a hosted web app.</summary>
    [Required, MaxLength(512)] public string Reference { get; set; } = string.Empty;
    [MaxLength(1000)] public string? InstallHint { get; set; }
}

public class ImportReleaseAssetRequest
{
    [Required] public string Url { get; set; } = string.Empty;
    [Required] public string PlatformCode { get; set; } = string.Empty;
    [MaxLength(1000)] public string? InstallHint { get; set; }
}

public class UpdateFileRequest
{
    public string? PlatformCode { get; set; }
    [MaxLength(1000)] public string? InstallHint { get; set; }
}

public class ReorderRequest
{
    [Required] public List<int> Ids { get; set; } = [];
}

public class ScreenshotCaptionRequest
{
    [MaxLength(200)] public string? Caption { get; set; }
}

public class DuplicateCheckRequest
{
    public string? Name { get; set; }
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public int? ExcludeAppId { get; set; }
}

public class DuplicateCheckDto
{
    public double Threshold { get; set; }
    public bool SemanticAvailable { get; set; }
    public List<AppCardDto> Matches { get; set; } = [];
    /// <summary>Highest similarity above the threshold, when any.</summary>
    public AppCardDto? BestMatch { get; set; }
}

public class SuggestMetadataRequest
{
    public string? Name { get; set; }
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public string? Readme { get; set; }
}

public class MetadataSuggestionDto
{
    public bool Available { get; set; }
    public int? CategoryId { get; set; }
    public string? CategorySlug { get; set; }
    public string? CategoryPath { get; set; }
    /// <summary>The model proposed a category that does not exist yet (queued for admin approval when the app is submitted).</summary>
    public string? ProposedCategoryName { get; set; }
    public string? ProposedCategoryParentSlug { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? ShortDescription { get; set; }
    public string? Reasoning { get; set; }
    public string Model { get; set; } = string.Empty;
}

public class ReadinessIssueDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    /// <summary>Wizard step the issue belongs to: source | details | files | screenshots.</summary>
    public string Step { get; set; } = string.Empty;
    public bool Blocking { get; set; } = true;
}

public class ReadinessDto
{
    public bool CanSubmit { get; set; }
    public List<ReadinessIssueDto> Issues { get; set; } = [];
}

public class SourceAnalysisDto
{
    public DateTime? AnalyzedAt { get; set; }
    public int FileCount { get; set; }
    public int LineCount { get; set; }
    public long Bytes { get; set; }
    public bool HasLicenseFile { get; set; }
    public string? DetectedLicenseSpdxId { get; set; }
    public string? PrimaryLanguage { get; set; }
    public string? Warnings { get; set; }
    public bool LicenseMatches { get; set; }
}

/// <summary>Everything the wizard / "my app" page edits.</summary>
public class AppDraftDto
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
    public int? CategoryId { get; set; }
    public List<CategoryNodeDto> CategoryPath { get; set; } = [];
    public int? LicenseId { get; set; }
    public string? LicenseSpdxId { get; set; }
    public int? LlmModelId { get; set; }
    public string? LlmModelNote { get; set; }
    public List<string> Tags { get; set; } = [];
    public int? DerivedFromAppId { get; set; }
    public string? DerivedFromName { get; set; }
    public string? DerivedFromSlug { get; set; }
    public DerivationKind? DerivationKind { get; set; }
    public List<AppPromptDto> Prompts { get; set; } = [];
    public SourceKind? SourceKind { get; set; }
    public string? RepoUrl { get; set; }
    public string? RepoProvider { get; set; }
    public string? RepoDefaultBranch { get; set; }
    public int? RepoStars { get; set; }
    public SourceAnalysisDto Source { get; set; } = new();
    public List<ScreenshotDto> Screenshots { get; set; } = [];
    public List<AppVersionDto> Versions { get; set; } = [];
    public AppVersionDto? DraftVersion { get; set; }
    public long EstGenerationTokens { get; set; }
    public decimal EstGenerationCostUsd { get; set; }
    public bool EstIsOverride { get; set; }
    public ReadinessDto Readiness { get; set; } = new();
    public int DownloadCount { get; set; }
    public int ViewCount { get; set; }
    public int RatingCount { get; set; }
    public double RatingAvg { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? LlmSuggestedCategoryId { get; set; }
}

public class MyAppStatsDto
{
    public int AppId { get; set; }
    public List<DailyCountDto> Downloads { get; set; } = [];
    public List<DailyCountDto> Views { get; set; } = [];
    public Dictionary<string, int> DownloadsByPlatform { get; set; } = [];
    public Dictionary<string, int> DownloadsBySource { get; set; } = [];
    public long EstSavedTokens { get; set; }
    public decimal EstSavedCostUsd { get; set; }
}

public class DailyCountDto
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
}

public class CreateRatingRequest
{
    [Range(0, 100)] public int Score { get; set; }
    [MaxLength(2000)] public string? Review { get; set; }
    public int? VersionId { get; set; }
    public bool? Worked { get; set; }
}

public class ReplyRequest
{
    [Required, MaxLength(2000)] public string Body { get; set; } = string.Empty;
}

public class VoteRequest
{
    public bool? Helpful { get; set; }
}

public class CreateReportRequest
{
    [Required] public ReportReason Reason { get; set; }
    [MaxLength(4000)] public string? Details { get; set; }
    [EmailAddress, MaxLength(256)] public string? Email { get; set; }
    public int? RatingId { get; set; }
}

public class DownloadLinkDto
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public string? ContentType { get; set; }
    public string? InstallHint { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? PlatformCode { get; set; }
}

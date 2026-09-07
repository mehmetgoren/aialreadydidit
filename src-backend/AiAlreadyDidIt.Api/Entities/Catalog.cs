using NpgsqlTypes;
using Pgvector;

namespace AiAlreadyDidIt.Api.Entities;

public enum AppStatus
{
    Draft = 0,
    PendingScan = 1,
    PendingReview = 2,
    Published = 3,
    Rejected = 4,
    Unlisted = 5,
    Removed = 6
}

public enum SourceKind { Repository = 1, Archive = 2 }
public enum RepoProvider { GitHub = 1, GitLab = 2 }
public enum DerivationKind { Fork = 1, Inspired = 2, Port = 3 }

/// <summary>An LLM-generated application listing (the "product" of the store).</summary>
public class App
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    /// <summary>Markdown.</summary>
    public string LongDescription { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int LicenseId { get; set; }
    public License License { get; set; } = null!;
    public int UploaderUserId { get; set; }
    public User Uploader { get; set; } = null!;
    public int? LlmModelId { get; set; }
    public LlmModel? LlmModel { get; set; }
    /// <summary>Free text detail about the generation (e.g. "Claude Code, Opus 4.1, 3 sessions").</summary>
    public string? LlmModelNote { get; set; }

    // lineage
    public int? DerivedFromAppId { get; set; }
    public App? DerivedFrom { get; set; }
    public DerivationKind? DerivationKind { get; set; }

    // source (exactly one of repository / archive)
    public SourceKind SourceKind { get; set; }
    public string? RepoUrl { get; set; }
    public RepoProvider? RepoProvider { get; set; }
    public string? RepoOwner { get; set; }
    public string? RepoName { get; set; }
    public string? RepoDefaultBranch { get; set; }
    public int? RepoStars { get; set; }
    public string? RepoPrimaryLanguage { get; set; }
    public DateTime? RepoSyncedAt { get; set; }
    public string? ReadmeMarkdown { get; set; }
    public string? IconStorageKey { get; set; }
    public string? HomepageUrl { get; set; }

    // moderation
    public AppStatus Status { get; set; } = AppStatus.Draft;
    public string? RejectionReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int? LatestVersionId { get; set; }

    // search
    /// <summary>Denormalised tag names so the generated tsvector can include them.</summary>
    public string TagsText { get; set; } = string.Empty;
    public string CategoryPathText { get; set; } = string.Empty;
    public NpgsqlTsVector? SearchVector { get; set; }
    public Vector? Embedding { get; set; }
    public bool EmbeddingStale { get; set; } = true;
    public string? EmbeddingModel { get; set; }
    public int? LlmSuggestedCategoryId { get; set; }

    // savings estimate
    public long EstGenerationTokens { get; set; }
    public decimal EstGenerationCostUsd { get; set; }
    /// <summary>The uploader entered real numbers from their session instead of the heuristic.</summary>
    public bool EstIsOverride { get; set; }
    public int SourceLineCount { get; set; }
    public int SourceFileCount { get; set; }
    public long SourceBytes { get; set; }
    public DateTime? SourceAnalyzedAt { get; set; }
    /// <summary>SPDX id recognised in the LICENSE file of the source snapshot (null = no license file / unknown text).</summary>
    public string? DetectedLicenseSpdxId { get; set; }
    public bool HasLicenseFile { get; set; }
    public string? SourceWarnings { get; set; }
    /// <summary>An admin confirmed the license manually (declared id kept even if the text was not recognised).</summary>
    public bool LicenseVerifiedByAdmin { get; set; }
    public string? SourcePrimaryLanguage { get; set; }

    // counters (cached)
    public double RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public int WorkedCount { get; set; }
    public int NotWorkedCount { get; set; }
    public int DownloadCount { get; set; }
    public int ViewCount { get; set; }
    public int FavoriteCount { get; set; }

    public bool IsFeatured { get; set; }
    public int FeaturedOrder { get; set; }
    public string? FeaturedNote { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<AppVersion> Versions { get; set; } = [];
    public ICollection<AppScreenshot> Screenshots { get; set; } = [];
    public ICollection<AppPrompt> Prompts { get; set; } = [];
    public ICollection<AppTag> AppTags { get; set; } = [];
    public ICollection<Rating> Ratings { get; set; } = [];
    public ICollection<App> Derivatives { get; set; } = [];
}

public enum VersionStatus { Draft = 0, PendingScan = 1, PendingReview = 2, Published = 3, Rejected = 4, Removed = 5 }

public class AppVersion
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public string Version { get; set; } = string.Empty;
    /// <summary>Markdown.</summary>
    public string? Changelog { get; set; }
    public DateTime ReleasedAt { get; set; }
    /// <summary>Git tag / commit for repository sources.</summary>
    public string? SourceRef { get; set; }
    public VersionStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int DownloadCount { get; set; }
    public ICollection<AppFile> Files { get; set; } = [];
}

public enum FileKind { Installer = 1, Source = 2, DockerImage = 3, WebBundle = 4 }
public enum ScanStatus { Pending = 0, Clean = 1, Infected = 2, Error = 3, Skipped = 4 }

/// <summary>A downloadable artefact of a version: the source snapshot or one installer per platform.</summary>
public class AppFile
{
    public int Id { get; set; }
    public int VersionId { get; set; }
    public AppVersion Version { get; set; } = null!;
    public int? PlatformId { get; set; }
    public Platform? Platform { get; set; }
    public FileKind Kind { get; set; }
    public string FileName { get; set; } = string.Empty;
    /// <summary>Object key in the bucket; null when the artefact is an external reference (docker image, hosted web app).</summary>
    public string? StorageKey { get; set; }
    public string? ExternalReference { get; set; }
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public string? ContentType { get; set; }
    public ScanStatus ScanStatus { get; set; }
    public string? ScanSignature { get; set; }
    public DateTime? ScannedAt { get; set; }
    public string? InstallHint { get; set; }
    public int DownloadCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AppScreenshot
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public string StorageKey { get; set; } = string.Empty;
    public string ThumbStorageKey { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>The prompt(s) that produced the app, so others can edit them and make a variant.</summary>
public class AppPrompt
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public int? VersionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class AppTag
{
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}

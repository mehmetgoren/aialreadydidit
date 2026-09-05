using AiAlreadyDidIt.Api.Contracts.Catalog;

namespace AiAlreadyDidIt.Api.Contracts.Site;

/// <summary>Everything the SPA needs at boot (cached client-side).</summary>
public class SiteConfigDto
{
    public string SiteName { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string ApiPublicUrl { get; set; } = string.Empty;
    public string McpPublicUrl { get; set; } = string.Empty;
    public string DefaultLocale { get; set; } = "en-US";
    public string? GoogleClientId { get; set; }
    public bool RequireEmailVerification { get; set; }
    public bool SemanticSearchAvailable { get; set; }
    public bool CategorySuggestionsAvailable { get; set; }
    public string? AnnouncementText { get; set; }
    public string? AnnouncementLink { get; set; }
    public string? ContactEmail { get; set; }
    public bool AllowAnonymousReports { get; set; }
    public UploadLimitsDto UploadLimits { get; set; } = new();
    public List<PlatformDto> Platforms { get; set; } = [];
    public List<LicenseDto> Licenses { get; set; } = [];
    public List<LlmModelDto> LlmModels { get; set; } = [];
}

public class UploadLimitsDto
{
    public int MinScreenshots { get; set; }
    public int MaxScreenshots { get; set; }
    public int RecommendedScreenshots { get; set; }
    public int MaxTags { get; set; }
    public long MaxInstallerBytes { get; set; }
    public long MaxSourceArchiveBytes { get; set; }
    public long MaxScreenshotBytes { get; set; }
    public long MaxIconBytes { get; set; }
    public double DuplicateThreshold { get; set; }
}

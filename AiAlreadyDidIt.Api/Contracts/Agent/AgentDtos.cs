using AiAlreadyDidIt.Api.Contracts.Catalog;

namespace AiAlreadyDidIt.Api.Contracts.Agent;

/// <summary>Answer to "has someone already built this?".</summary>
public class AgentCheckDto
{
    /// <summary>download | fork | build</summary>
    public string Verdict { get; set; } = "build";
    public string Advice { get; set; } = string.Empty;
    public double? BestSimilarity { get; set; }
    public bool SemanticSearchUsed { get; set; }
    public List<AgentMatchDto> Matches { get; set; } = [];
    public string RequestUrl { get; set; } = string.Empty;
    public long EstimatedTokensIfBuilt { get; set; }
}

public class AgentMatchDto
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public double? Similarity { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string? GeneratedBy { get; set; }
    public List<string> Platforms { get; set; } = [];
    public double RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public int DownloadCount { get; set; }
    public string? RepoUrl { get; set; }
    public string? LatestVersion { get; set; }
    public List<AgentFileDto> Files { get; set; } = [];
    public long EstGenerationTokens { get; set; }
}

public class AgentFileDto
{
    public int FileId { get; set; }
    public string? Platform { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
}

public class AgentDocsDto
{
    public string Name { get; set; } = string.Empty;
    public string Mission { get; set; } = string.Empty;
    public string Flow { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string OpenApiUrl { get; set; } = string.Empty;
    public string McpUrl { get; set; } = string.Empty;
    public string Authentication { get; set; } = string.Empty;
    public Dictionary<string, string> Endpoints { get; set; } = [];
    public string RateLimits { get; set; } = string.Empty;
    public int PublishedApps { get; set; }
}

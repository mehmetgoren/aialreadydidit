namespace AiAlreadyDidIt.Api.Infrastructure;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "AiAlreadyDidIt";
    public string Audience { get; set; } = "AiAlreadyDidIt.Api";
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
    public string RefreshCookieName { get; set; } = "aadi_refresh";
}

public class SiteOptions
{
    public const string SectionName = "Site";
    public string Name { get; set; } = "AI Already Did It";
    /// <summary>Public origin of the storefront (canonical URLs, sitemap, e-mail links), no trailing slash.</summary>
    public string PublicUrl { get; set; } = "http://localhost:9002";
    /// <summary>Public origin of the API (used in agent docs and MCP responses).</summary>
    public string ApiPublicUrl { get; set; } = "http://localhost:5190";
    public string McpPublicUrl { get; set; } = "http://localhost:5191/mcp";
    public string SupportEmail { get; set; } = "hello@example.com";
    public string DefaultLocale { get; set; } = "en-US";
    /// <summary>Google Sign-In web client id; the button is hidden while this is empty.</summary>
    public string? GoogleClientId { get; set; }
    public bool RequireEmailVerification { get; set; }
    /// <summary>Header the storefront SPA sends so its calls are rate-limited per user instead of per API key.</summary>
    public string WebClientHeader { get; set; } = "X-Aadi-Client";
}

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public bool Enabled { get; set; } = true;
    public int WindowSeconds { get; set; } = 60;
    /// <summary>Anonymous callers, per IP.</summary>
    public int AnonymousPermitLimit { get; set; } = 60;
    /// <summary>Signed-in storefront members, per user.</summary>
    public int UserPermitLimit { get; set; } = 300;
    public int ApiKeyDefaultPermitLimit { get; set; } = 600;
    public int ApiKeyElevatedPermitLimit { get; set; } = 3000;
    /// <summary>Downloads per window (per IP / key) — separate, stricter bucket.</summary>
    public int DownloadPermitLimit { get; set; } = 30;
    /// <summary>Sign-in / sign-up attempts per window per IP.</summary>
    public int AuthPermitLimit { get; set; } = 10;
}

public class StorageOptions
{
    public const string SectionName = "Storage";
    /// <summary>S3 endpoint reachable from the API (inside docker: http://minio:9000).</summary>
    public string Endpoint { get; set; } = "http://localhost:9000";
    /// <summary>S3 endpoint reachable from browsers (presigned download URLs).</summary>
    public string PublicEndpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string Region { get; set; } = "us-east-1";
    public string BucketPrefix { get; set; } = "aadi";
    public int PresignedUrlMinutes { get; set; } = 15;
    public long MaxInstallerBytes { get; set; } = 500L * 1024 * 1024;
    public long MaxSourceArchiveBytes { get; set; } = 200L * 1024 * 1024;
    public long MaxScreenshotBytes { get; set; } = 8L * 1024 * 1024;
    public long MaxIconBytes { get; set; } = 1L * 1024 * 1024;
    /// <summary>Local scratch directory for archive inspection.</summary>
    public string TempPath { get; set; } = "Files/tmp";
}

public class ClamAvOptions
{
    public const string SectionName = "ClamAv";
    public bool Enabled { get; set; } = true;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3310;
    /// <summary>Files above this size are marked Skipped (clamd stream limit).</summary>
    public long MaxScanBytes { get; set; } = 1024L * 1024 * 1024;
    /// <summary>When ClamAV is unreachable: true = keep the file Pending and retry; false = mark Error and let admins decide.</summary>
    public bool FailClosed { get; set; } = true;
}

/// <summary>LLM / embedding provider selection (mirrors the CL-AI LlmProvider pattern).</summary>
public class AiOptions
{
    public const string SectionName = "Ai";
    /// <summary>Ollama | OpenAi | None — default provider for both chat and embeddings.</summary>
    public string Provider { get; set; } = "Ollama";
    /// <summary>Override for embeddings only (falls back to <see cref="Provider"/>).</summary>
    public string? EmbeddingProvider { get; set; }
    /// <summary>Override for chat only (falls back to <see cref="Provider"/>).</summary>
    public string? ChatProvider { get; set; }
    /// <summary>Vector width stored in PostgreSQL. bge-m3 = 1024; OpenAI text-embedding-3-* is truncated to this size.</summary>
    public int EmbeddingDimensions { get; set; } = 1024;
    /// <summary>Cosine similarity above which the uploader is warned about a duplicate.</summary>
    public double DuplicateSimilarityThreshold { get; set; } = 0.72;
    /// <summary>
    /// LLM-assisted categorisation (wizard "Suggest" button + background <c>categorize_app</c> job for the moderation queue).
    /// Off by default: it needs a chat model, i.e. a GPU server or a paid API. When off, uploaders pick the category and
    /// sub-category themselves and the wizard enforces it.
    /// </summary>
    public bool EnableCategorySuggestions { get; set; } = false;
    public OllamaOptions Ollama { get; set; } = new();
    public OpenAiOptions OpenAi { get; set; } = new();
}

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "qwen3.8:latest";
    public string EmbeddingModel { get; set; } = "bge-m3";
    public int NumCtx { get; set; } = 16384;
    public int TimeoutSeconds { get; set; } = 180;
    public bool Think { get; set; }
}

public class OpenAiOptions
{
    /// <summary>Any OpenAI-compatible endpoint (OpenAI, Azure gateway, Groq, OpenRouter, vLLM, Ollama cloud...).</summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-5-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int TimeoutSeconds { get; set; } = 120;
}

public class EmailOptions
{
    public const string SectionName = "Email";
    /// <summary>Console | Smtp</summary>
    public string Provider { get; set; } = "Console";
    public string FromAddress { get; set; } = "no-reply@localhost";
    public string FromName { get; set; } = "AI Already Did It";
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpUseSsl { get; set; }
}

public class RepositoryImportOptions
{
    public const string SectionName = "RepositoryImport";
    /// <summary>Optional GitHub token (raises the API quota from 60 to 5000 requests/hour).</summary>
    public string? GitHubToken { get; set; }
    public string? GitLabToken { get; set; }
    public int TimeoutSeconds { get; set; } = 60;
}

public class DatabaseOptions
{
    public const string SectionName = "Database";
    public bool MigrateOnStartup { get; set; } = true;
    public bool SeedOnStartup { get; set; } = true;
}

public class JobsOptions
{
    public const string SectionName = "Jobs";
    public bool Enabled { get; set; } = true;
    public int PollSeconds { get; set; } = 3;
    public int Concurrency { get; set; } = 2;
}

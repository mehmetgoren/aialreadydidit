using AiAlreadyDidIt.Api.Contracts.Agent;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Catalog;
using AiAlreadyDidIt.Api.Services.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Services.Agent;

/// <summary>The one-call endpoint for LLM agents: "should I build this or does it already exist?"</summary>
public sealed class AgentService(AadiDbContext db, SearchService search, SiteSettingsCache settings, IOptions<SiteOptions> site, IOptions<RateLimitingOptions> rateLimits, ICurrentUser currentUser)
{
    public async Task<AgentCheckDto> CheckAsync(string q, string? platform, int take, RequestSource source, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 10);
        var result = await search.SearchAsync(new AppQuery { Q = q, Platform = platform, Mode = "hybrid", PageSize = take, Page = 1 }, source, ct);
        var threshold = await settings.GetDoubleAsync(SettingKeys.DuplicateThreshold, 0.80, ct);
        var forkThreshold = Math.Max(0.45, threshold - 0.22);
        var cards = result.Page.Items;
        var ids = cards.Select(c => c.Id).ToList();
        var details = await db.Apps.AsNoTracking().Where(a => ids.Contains(a.Id))
            .Select(a => new
            {
                a.Id, a.RepoUrl,
                Files = a.Versions.Where(v => v.Id == a.LatestVersionId).SelectMany(v => v.Files).Where(f => f.ScanStatus != ScanStatus.Infected)
                    .Select(f => new { f.Id, Platform = f.Platform == null ? null : f.Platform.Code, f.Kind, f.FileName, f.SizeBytes, f.ExternalReference }).ToList()
            }).ToDictionaryAsync(x => x.Id, ct);
        var publicUrl = site.Value.PublicUrl.TrimEnd('/');
        var apiUrl = site.Value.ApiPublicUrl.TrimEnd('/');
        var matches = cards.Select(c => new AgentMatchDto
        {
            Slug = c.Slug, Name = c.Name, ShortDescription = c.ShortDescription, Similarity = c.Similarity, Url = $"{publicUrl}/app/{c.Slug}", ApiUrl = $"{apiUrl}/api/v1/catalog/apps/{c.Slug}",
            License = c.LicenseSpdxId, GeneratedBy = c.LlmModelName, Platforms = c.Platforms, RatingAvg = c.RatingAvg, RatingCount = c.RatingCount, DownloadCount = c.DownloadCount,
            RepoUrl = details.GetValueOrDefault(c.Id)?.RepoUrl, LatestVersion = c.LatestVersion, EstGenerationTokens = c.EstGenerationTokens,
            Files = (details.GetValueOrDefault(c.Id)?.Files ?? []).Select(f => new AgentFileDto
            {
                FileId = f.Id, Platform = f.Platform, Kind = f.Kind.ToString(), FileName = f.FileName, SizeBytes = f.SizeBytes, ExternalReference = f.ExternalReference,
                DownloadUrl = $"{apiUrl}/api/v1/apps/{c.Slug}/download/{f.Id}?json=1&source={(source == RequestSource.Mcp ? "mcp" : "api")}"
            }).ToList()
        }).ToList();

        var best = matches.MaxBy(m => m.Similarity ?? 0);
        var bestSim = best?.Similarity;
        var keywordOnly = !result.SemanticAvailable || result.ModeUsed == "keyword";
        string verdict, advice;
        if (best is not null && (bestSim ?? 0) >= threshold)
        {
            verdict = "download";
            advice = $"“{best.Name}” matches this request ({bestSim:P0} similar). Download it instead of generating a new app; its source is open ({best.License}) so you can adapt it if something is missing.";
        }
        else if (best is not null && ((bestSim ?? 0) >= forkThreshold || (keywordOnly && matches.Count > 0)))
        {
            verdict = "fork";
            advice = $"“{best.Name}” is close ({(bestSim is null ? "keyword match" : bestSim.Value.ToString("P0") + " similar")}). Start from its source code and the original prompt(s) instead of generating from scratch; publish your result as a fork so the lineage is visible.";
        }
        else
        {
            verdict = "build";
            advice = "Nothing similar has been published yet. If you build it, please publish the result here (with the prompt) so the next agent does not have to — and post a request so others know it is wanted.";
        }
        var c2 = await settings.SavingsAsync(ct);
        return new AgentCheckDto
        {
            Verdict = verdict, Advice = advice, BestSimilarity = bestSim, SemanticSearchUsed = !keywordOnly, Matches = matches,
            RequestUrl = $"{apiUrl}/api/v1/requests", EstimatedTokensIfBuilt = best?.EstGenerationTokens ?? c2.EstimateTokens(1500)
        };
    }

    public async Task<AgentDocsDto> DocsAsync(CancellationToken ct)
    {
        var apiUrl = site.Value.ApiPublicUrl.TrimEnd('/');
        var r = rateLimits.Value;
        return new AgentDocsDto
        {
            Name = site.Value.Name,
            Mission = "A free repository of open-source applications written by LLMs. Check it before generating anything: re-using an existing app costs a download, regenerating it costs thousands of tokens and energy.",
            Flow = "1) GET /api/v1/agent/check?q=<what you were asked to build> → verdict download | fork | build. 2) download: GET the file URLs in the answer (json=1 returns the link, sha256 and install hint). 3) fork: fetch the source (repoUrl or the Source file) and the prompts from /api/v1/catalog/apps/{slug}. 4) build: POST /api/v1/requests so the need is recorded, then publish your result through the website.",
            ApiBaseUrl = apiUrl + "/api/v1",
            OpenApiUrl = apiUrl + "/openapi/v1.json",
            McpUrl = site.Value.McpPublicUrl,
            Authentication = "Anonymous calls are allowed. Send X-Api-Key: aad_... (create one in the dashboard) for a higher rate limit, to post requests, and so downloads count toward your account.",
            Endpoints = new Dictionary<string, string>
            {
                ["GET /api/v1/agent/check?q=&platform=&take="] = "Verdict + best matches with download links.",
                ["GET /api/v1/catalog/apps?q=&mode=hybrid|keyword|semantic&category=&platform=&license=&model=&minRating=&tags=&sort=&page="] = "Search / browse published apps.",
                ["GET /api/v1/catalog/apps/{slug}"] = "Full detail: description, README, versions, files, prompts, lineage.",
                ["GET /api/v1/catalog/categories"] = "Category tree.",
                ["GET /api/v1/apps/{slug}/download/{fileId}?json=1"] = "Records the download and returns a time-limited URL + sha256 (302 redirect without json=1).",
                ["POST /api/v1/requests {title, description}"] = "Post a 'wanted' request (API key required).",
                ["GET /api/v1/site/savings"] = "Tokens / money / energy the store has saved so far."
            },
            RateLimits = $"anonymous {r.AnonymousPermitLimit}/min per IP · API key {r.ApiKeyDefaultPermitLimit}/min · downloads {r.DownloadPermitLimit}/min. 429 responses carry Retry-After.",
            PublishedApps = await db.Apps.CountAsync(a => a.Status == AppStatus.Published, ct)
        };
    }
}

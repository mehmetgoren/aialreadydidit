using AiAlreadyDidIt.Api.Contracts.Site;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Ai;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Services.Site;

public sealed class SiteService(CatalogService catalog, SiteSettingsCache settings, ILlmProviderFactory providers, IOptions<SiteOptions> site,
    IOptions<StorageOptions> storage, IOptions<AiOptions> ai, IMemoryCache cache)
{
    public async Task<SiteConfigDto> GetConfigAsync(CancellationToken ct)
    {
        const string key = "site:config:v1";
        if (cache.TryGetValue(key, out SiteConfigDto? cached) && cached is not null) return cached;
        var s = site.Value;
        var dto = new SiteConfigDto
        {
            SiteName = s.Name,
            PublicUrl = s.PublicUrl,
            ApiPublicUrl = s.ApiPublicUrl,
            McpPublicUrl = s.McpPublicUrl,
            DefaultLocale = s.DefaultLocale,
            GoogleClientId = string.IsNullOrWhiteSpace(s.GoogleClientId) ? null : s.GoogleClientId,
            RequireEmailVerification = s.RequireEmailVerification,
            SemanticSearchAvailable = providers.Embeddings.SupportsEmbeddings,
            CategorySuggestionsAvailable = ai.Value.EnableCategorySuggestions && providers.Chat.SupportsChat,
            AnnouncementText = NullIfEmpty(await settings.GetStringAsync(SettingKeys.AnnouncementText, "", ct)),
            AnnouncementLink = NullIfEmpty(await settings.GetStringAsync(SettingKeys.AnnouncementLink, "", ct)),
            ContactEmail = NullIfEmpty(await settings.GetStringAsync(SettingKeys.ContactEmail, s.SupportEmail, ct)),
            AllowAnonymousReports = await settings.GetBoolAsync(SettingKeys.AllowAnonymousReports, true, ct),
            UploadLimits = new UploadLimitsDto
            {
                MinScreenshots = await settings.GetIntAsync(SettingKeys.MinScreenshots, 1, ct),
                MaxScreenshots = await settings.GetIntAsync(SettingKeys.MaxScreenshots, 10, ct),
                RecommendedScreenshots = await settings.GetIntAsync(SettingKeys.RecommendedScreenshots, 3, ct),
                MaxTags = await settings.GetIntAsync(SettingKeys.MaxTagsPerApp, 10, ct),
                MaxInstallerBytes = storage.Value.MaxInstallerBytes,
                MaxSourceArchiveBytes = storage.Value.MaxSourceArchiveBytes,
                MaxScreenshotBytes = storage.Value.MaxScreenshotBytes,
                MaxIconBytes = storage.Value.MaxIconBytes,
                DuplicateThreshold = await settings.GetDoubleAsync(SettingKeys.DuplicateThreshold, ai.Value.DuplicateSimilarityThreshold, ct)
            },
            Platforms = await catalog.GetPlatformsAsync(ct),
            Licenses = await catalog.GetLicensesAsync(ct),
            LlmModels = await catalog.GetLlmModelsAsync(ct)
        };
        cache.Set(key, dto, TimeSpan.FromSeconds(60));
        return dto;
    }

    private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}

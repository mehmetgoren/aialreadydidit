using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Catalog;
using AiAlreadyDidIt.Api.Services.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Catalog;

/// <summary>Storefront browsing. Every endpoint is anonymous; agents may call them with or without an API key.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/catalog")]
[Produces("application/json")]
public class CatalogController(CatalogService catalog, SearchService search, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Full category tree with published-app counts (descendants included). Cached 5 minutes.</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryNodeDto>>>> Categories(CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetCategoriesAsync(ct)));

    [HttpGet("categories/{slug}")]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> Category(string slug, CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetCategoryAsync(slug, ct)));

    [HttpGet("platforms")]
    public async Task<ActionResult<ApiResponse<List<PlatformDto>>>> Platforms(CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetPlatformsAsync(ct)));

    [HttpGet("licenses")]
    public async Task<ActionResult<ApiResponse<List<LicenseDto>>>> Licenses(CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetLicensesAsync(ct)));

    [HttpGet("llm-models")]
    public async Task<ActionResult<ApiResponse<List<LlmModelDto>>>> LlmModels(CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetLlmModelsAsync(ct)));

    [HttpGet("tags")]
    public async Task<ActionResult<ApiResponse<List<TagDto>>>> Tags([FromQuery] string? q, [FromQuery] int take = 30, CancellationToken ct = default) => Ok(ApiResponse.Ok(await catalog.GetTagsAsync(q, take, ct)));

    /// <summary>Home page: savings counter, featured, trending, newest, updated, top rated, categories, banners.</summary>
    [HttpGet("home")]
    public async Task<ActionResult<ApiResponse<HomeDto>>> Home(CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetHomeAsync(ct)));

    /// <summary>
    /// Search / browse apps. <c>q</c> enables keyword + semantic (hybrid) search; without it the list is filtered and sorted.
    /// Filters: category (slug, includes descendants), platform, license, model, minRating, tags (comma separated), uploader.
    /// </summary>
    [HttpGet("apps")]
    public async Task<ActionResult<ApiResponse<AppSearchResultDto>>> Apps([FromQuery] AppQuery query, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await search.SearchAsync(query, currentUser.IsApiKey ? RequestSource.Api : RequestSource.Web, ct)));

    /// <summary>App detail: description, README, screenshots, versions with files, prompts, lineage, counters.</summary>
    [HttpGet("apps/{slug}")]
    public async Task<ActionResult<ApiResponse<AppDetailDto>>> App(string slug, CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetAppAsync(slug, countView: true, ct)));

    [HttpGet("apps/{slug}/similar")]
    public async Task<ActionResult<ApiResponse<List<AppCardDto>>>> Similar(string slug, CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetSimilarAsync(slug, ct)));

    [HttpGet("apps/{slug}/lineage")]
    public async Task<ActionResult<ApiResponse<LineageNodeDto>>> Lineage(string slug, CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetLineageAsync(slug, ct)));

    [HttpGet("uploaders/{username}")]
    public async Task<ActionResult<ApiResponse<UploaderDto>>> Uploader(string username, CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetUploaderAsync(username, ct)));

    [HttpGet("uploaders/{username}/collections")]
    public async Task<ActionResult<ApiResponse<List<CollectionDto>>>> Collections(string username, CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetPublicCollectionsAsync(username, ct)));

    [HttpGet("uploaders/{username}/collections/{slug}")]
    public async Task<ActionResult<ApiResponse<CollectionDto>>> Collection(string username, string slug, CancellationToken ct) => Ok(ApiResponse.Ok(await catalog.GetPublicCollectionAsync(username, slug, ct)));
}

using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Contracts.Dashboard;
using AiAlreadyDidIt.Api.Services.Apps;
using AiAlreadyDidIt.Api.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Dashboard;

/// <summary>Member dashboard (signed-in members only).</summary>
[ApiController]
[Authorize]
[Route("api/v1/my")]
[Produces("application/json")]
public class DashboardController(DashboardService dashboard, RatingService ratings) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> Overview(CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.OverviewAsync(ct)));

    [HttpGet("downloads")]
    public async Task<ActionResult<ApiResponse<PagedResult<DownloadHistoryDto>>>> Downloads([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) => Ok(ApiResponse.Ok(await dashboard.DownloadsAsync(page, pageSize, ct)));

    [HttpGet("ratings")]
    public async Task<ActionResult<ApiResponse<List<RatingDto>>>> Ratings(CancellationToken ct) => Ok(ApiResponse.Ok(await ratings.MineAsync(ct)));

    [HttpGet("favorites")]
    public async Task<ActionResult<ApiResponse<List<AppCardDto>>>> Favorites(CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.FavoritesAsync(ct)));

    [HttpPost("favorites/{appId:int}/toggle")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleFavorite(int appId, CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.ToggleFavoriteAsync(appId, ct)));

    [HttpGet("collections")]
    public async Task<ActionResult<ApiResponse<List<CollectionDto>>>> Collections(CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.CollectionsAsync(ct)));

    [HttpGet("collections/{id:int}")]
    public async Task<ActionResult<ApiResponse<CollectionDto>>> Collection(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.CollectionAsync(id, ct)));

    [HttpPost("collections")]
    public async Task<ActionResult<ApiResponse<CollectionDto>>> CreateCollection([FromBody] SaveCollectionRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.CreateCollectionAsync(request, ct)));

    [HttpPut("collections/{id:int}")]
    public async Task<ActionResult<ApiResponse<CollectionDto>>> UpdateCollection(int id, [FromBody] SaveCollectionRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.UpdateCollectionAsync(id, request, ct)));

    [HttpDelete("collections/{id:int}")]
    public async Task<ActionResult<ApiResponse<OkDto>>> DeleteCollection(int id, CancellationToken ct) { await dashboard.DeleteCollectionAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    [HttpPost("collections/{id:int}/items")]
    public async Task<ActionResult<ApiResponse<CollectionDto>>> AddItem(int id, [FromBody] CollectionItemRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.AddToCollectionAsync(id, request, ct)));

    [HttpDelete("collections/{id:int}/items/{appId:int}")]
    public async Task<ActionResult<ApiResponse<CollectionDto>>> RemoveItem(int id, int appId, CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.RemoveFromCollectionAsync(id, appId, ct)));

    [HttpGet("watches")]
    public async Task<ActionResult<ApiResponse<List<WatchDto>>>> Watches(CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.WatchesAsync(ct)));

    [HttpPut("watches/{appId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Watch(int appId, [FromBody] WatchRequest? request, CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.SetWatchAsync(appId, request, true, ct)));

    [HttpDelete("watches/{appId:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> Unwatch(int appId, CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.SetWatchAsync(appId, null, false, ct)));

    [HttpGet("notifications")]
    public async Task<ActionResult<ApiResponse<PagedResult<NotificationDto>>>> Notifications([FromQuery] bool unread = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(ApiResponse.Ok(await dashboard.NotificationsAsync(unread, page, pageSize, ct)));

    [HttpPost("notifications/read")]
    public async Task<ActionResult<ApiResponse<OkDto>>> MarkAllRead(CancellationToken ct) { await dashboard.MarkReadAsync(null, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    [HttpPost("notifications/{id:long}/read")]
    public async Task<ActionResult<ApiResponse<OkDto>>> MarkRead(long id, CancellationToken ct) { await dashboard.MarkReadAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    [HttpGet("api-keys")]
    public async Task<ActionResult<ApiResponse<List<ApiKeyDto>>>> ApiKeys(CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.ApiKeysAsync(ct)));

    /// <summary>Creates an agent API key. The secret is returned once.</summary>
    [HttpPost("api-keys")]
    public async Task<ActionResult<ApiResponse<ApiKeyDto>>> CreateApiKey([FromBody] CreateApiKeyRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await dashboard.CreateApiKeyAsync(request, ct)));

    [HttpDelete("api-keys/{id:int}")]
    public async Task<ActionResult<ApiResponse<OkDto>>> RevokeApiKey(int id, CancellationToken ct) { await dashboard.RevokeApiKeyAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

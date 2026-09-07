using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Infrastructure.Auth;
using AiAlreadyDidIt.Api.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Admin;

/// <summary>Admin › Moderation queue — new apps and new versions waiting for approval.</summary>
[ApiController]
[Authorize]
[RoleActionAuthorize]
[Route("api/v1/admin/moderation")]
[Produces("application/json")]
public class AdminModerationController(AdminModerationService service) : ControllerBase
{
    /// <summary>Queue. status = review (default) | scan | rejected | all.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ModerationQueueItemDto>>>> Queue([FromQuery] AdminListQuery query, CancellationToken ct) => Ok(ApiResponse.Ok(await service.QueueAsync(query, ct)));

    [HttpGet("{appId:int}")]
    public async Task<ActionResult<ApiResponse<ModerationDetailDto>>> Detail(int appId, CancellationToken ct) => Ok(ApiResponse.Ok(await service.DetailAsync(appId, ct)));

    [HttpPost("{appId:int}/approve")]
    public async Task<ActionResult<ApiResponse<ModerationDetailDto>>> Approve(int appId, [FromBody] ModerationDecisionRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.ApproveAsync(appId, request, ct)));

    [HttpPost("{appId:int}/reject")]
    public async Task<ActionResult<ApiResponse<ModerationDetailDto>>> Reject(int appId, [FromBody] ModerationDecisionRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.RejectAsync(appId, request, ct)));

    [HttpPost("{appId:int}/rescan")]
    public async Task<ActionResult<ApiResponse<ModerationDetailDto>>> Rescan(int appId, [FromQuery] int? versionId, CancellationToken ct) => Ok(ApiResponse.Ok(await service.RescanAsync(appId, versionId, ct)));

    [HttpPost("{appId:int}/verify-license")]
    public async Task<ActionResult<ApiResponse<ModerationDetailDto>>> VerifyLicense(int appId, [FromBody] ModerationDecisionRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.VerifyLicenseAsync(appId, request, ct)));

    [HttpPost("{appId:int}/note")]
    public async Task<ActionResult<ApiResponse<ModerationDetailDto>>> Note(int appId, [FromBody] ModerationDecisionRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.NoteAsync(appId, request, ct)));
}

[ApiController]
[Authorize]
[RoleActionAuthorize]
[Route("api/v1/admin/reports")]
[Produces("application/json")]
public class AdminReportsController(AdminReportsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminReportDto>>>> List([FromQuery] AdminListQuery query, CancellationToken ct) => Ok(ApiResponse.Ok(await service.ListAsync(query, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AdminReportDto>>> Get(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await service.GetAsync(id, ct)));

    /// <summary>Resolve / dismiss with an optional action: none | unlist | remove | hide_review | ban_uploader.</summary>
    [HttpPost("{id:int}/resolve")]
    public async Task<ActionResult<ApiResponse<AdminReportDto>>> Resolve(int id, [FromBody] ResolveReportRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.ResolveAsync(id, request, ct)));
}

[ApiController]
[Authorize]
[RoleActionAuthorize]
[Route("api/v1/admin/requests")]
[Produces("application/json")]
public class AdminRequestsController(AdminRequestsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminRequestDto>>>> List([FromQuery] AdminListQuery query, CancellationToken ct) => Ok(ApiResponse.Ok(await service.ListAsync(query, ct)));

    [HttpPost("{id:int}/status")]
    public async Task<ActionResult<ApiResponse<OkDto>>> SetStatus(int id, [FromQuery] Entities.AppRequestStatus status, [FromQuery] int? appId, CancellationToken ct) { await service.SetStatusAsync(id, status, appId, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<OkDto>>> Delete(int id, CancellationToken ct) { await service.DeleteAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

[ApiController]
[Authorize]
[RoleActionAuthorize]
[Route("api/v1/admin/apps")]
[Produces("application/json")]
public class AdminAppsController(AdminAppsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminAppRowDto>>>> List([FromQuery] AdminListQuery query, [FromQuery] string? category, [FromQuery] string? uploader, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await service.ListAsync(query, category, uploader, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<Contracts.Apps.AppDraftDto>>> Get(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await service.GetAsync(id, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<Contracts.Apps.AppDraftDto>>> Update(int id, [FromBody] AdminAppUpdateRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.UpdateAsync(id, request, ct)));

    /// <summary>verb = unlist | restore | remove | feature | unfeature. (The parameter must not be called "action":
    /// that is MVC's reserved route token and the route would only match the literal action-method name → 404.)</summary>
    [HttpPost("{id:int}/{verb}")]
    public async Task<ActionResult<ApiResponse<Contracts.Apps.AppDraftDto>>> Action(int id, string verb, [FromQuery] string? note, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SetStatusAsync(id, verb, note, ct)));

    [HttpPost("{id:int}/recompute")]
    public async Task<ActionResult<ApiResponse<OkDto>>> Recompute(int id, CancellationToken ct) { await service.RecomputeAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<OkDto>>> Delete(int id, CancellationToken ct) { await service.DeleteAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Infrastructure.Auth;
using AiAlreadyDidIt.Api.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Admin;

/// <summary>Admin panel shell: menu for the current role + dashboard KPIs.</summary>
[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/panel"), Produces("application/json")]
public class AdminPanelController(AdminSystemService service) : ControllerBase
{
    [HttpGet("menu"), AllowAnyRole] public async Task<ActionResult<ApiResponse<List<AdminMenuItemDto>>>> Menu(CancellationToken ct) => Ok(ApiResponse.Ok(await service.MenuAsync(ct)));
    [HttpGet("dashboard"), AllowAnyRole] public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> Dashboard(CancellationToken ct) => Ok(ApiResponse.Ok(await service.DashboardAsync(ct)));
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/stats"), Produces("application/json")]
public class AdminStatsController(AdminSystemService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<StatsOverviewDto>>> Overview([FromQuery] int days = 30, CancellationToken ct = default) => Ok(ApiResponse.Ok(await service.StatsAsync(days, ct)));
    [HttpGet("search")] public async Task<ActionResult<ApiResponse<SearchAnalyticsDto>>> Search([FromQuery] int days = 30, CancellationToken ct = default) => Ok(ApiResponse.Ok(await service.SearchAnalyticsAsync(days, ct)));
    [HttpGet("savings")] public async Task<ActionResult<ApiResponse<SavingsBreakdownDto>>> Savings(CancellationToken ct) => Ok(ApiResponse.Ok(await service.SavingsAsync(ct)));
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/settings"), Produces("application/json")]
public class AdminSettingsController(AdminSystemService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<List<SiteSettingDto>>>> List(CancellationToken ct) => Ok(ApiResponse.Ok(await service.SettingsAsync(ct)));
    [HttpPut] public async Task<ActionResult<ApiResponse<List<SiteSettingDto>>>> Save([FromBody] SaveSettingsRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveSettingsAsync(request, ct)));
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/audit-log"), Produces("application/json")]
public class AdminAuditController(AdminSystemService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> List([FromQuery] AdminListQuery query, CancellationToken ct) => Ok(ApiResponse.Ok(await service.AuditAsync(query, ct)));
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/jobs"), Produces("application/json")]
public class AdminJobsController(AdminSystemService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<JobDto>>>> List([FromQuery] AdminListQuery query, CancellationToken ct) => Ok(ApiResponse.Ok(await service.JobsAsync(query, ct)));
    [HttpPost("{id:long}/retry")] public async Task<ActionResult<ApiResponse<OkDto>>> Retry(long id, CancellationToken ct) { await service.RetryJobAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    [HttpPost("{id:long}/cancel")] public async Task<ActionResult<ApiResponse<OkDto>>> Cancel(long id, CancellationToken ct) { await service.CancelJobAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    /// <summary>task = reembed_all | recompute_stats | rescan_pending | purge_done_jobs</summary>
    [HttpPost("maintenance/{task}")] public async Task<ActionResult<ApiResponse<string>>> Maintenance(string task, CancellationToken ct) => Ok(ApiResponse.Ok(await service.MaintenanceAsync(task, ct)));
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/health"), Produces("application/json")]
public class AdminHealthController(AdminSystemService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<SystemHealthDto>>> Get(CancellationToken ct) => Ok(ApiResponse.Ok(await service.HealthAsync(ct)));
    /// <summary>Sends a test e-mail to the signed-in admin through the configured provider.</summary>
    [HttpPost("test-email")] public async Task<ActionResult<ApiResponse<string>>> TestEmail(CancellationToken ct) => Ok(ApiResponse.Ok(await service.SendTestEmailAsync(ct)));
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/featured"), Produces("application/json")]
public class AdminFeaturedController(AdminContentService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<List<FeaturedItemDto>>>> List(CancellationToken ct) => Ok(ApiResponse.Ok(await service.FeaturedAsync(ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<List<FeaturedItemDto>>>> Add([FromBody] FeatureAppRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.FeatureAsync(request, ct)));
    [HttpPost("reorder")] public async Task<ActionResult<ApiResponse<List<FeaturedItemDto>>>> Reorder([FromBody] SaveFeaturedRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.ReorderAsync(request, ct)));
    [HttpDelete("{appId:int}")] public async Task<ActionResult<ApiResponse<List<FeaturedItemDto>>>> Remove(int appId, CancellationToken ct) => Ok(ApiResponse.Ok(await service.UnfeatureAsync(appId, ct)));
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/banners"), Produces("application/json")]
public class AdminBannersController(AdminContentService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<List<AdminBannerDto>>>> List(CancellationToken ct) => Ok(ApiResponse.Ok(await service.BannersAsync(ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<AdminBannerDto>>> Create([FromBody] SaveBannerRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveBannerAsync(null, request, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<AdminBannerDto>>> Update(int id, [FromBody] SaveBannerRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveBannerAsync(id, request, ct)));
    [HttpPost("{id:int}/image"), RequestSizeLimit(12L * 1024 * 1024)] public async Task<ActionResult<ApiResponse<AdminBannerDto>>> Image(int id, IFormFile file, CancellationToken ct) => Ok(ApiResponse.Ok(await service.UploadBannerImageAsync(id, file, ct)));
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<OkDto>>> Delete(int id, CancellationToken ct) { await service.DeleteBannerAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

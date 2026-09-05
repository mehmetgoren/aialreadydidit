using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Infrastructure.Auth;
using AiAlreadyDidIt.Api.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Admin;

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/users"), Produces("application/json")]
public class AdminUsersController(AdminIdentityService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<AdminUserRowDto>>>> List([FromQuery] AdminListQuery query, [FromQuery] string? role, CancellationToken ct) => Ok(ApiResponse.Ok(await service.UsersAsync(query, role, ct)));
    [HttpGet("{id:int}")] public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Get(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await service.UserAsync(id, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Update(int id, [FromBody] AdminUpdateUserRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.UpdateUserAsync(id, request, ct)));
    [HttpPost("{id:int}/ban")] public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> Ban(int id, [FromBody] BanRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.BanAsync(id, request, ct)));
    [HttpDelete("{id:int}/sessions")] public async Task<ActionResult<ApiResponse<OkDto>>> RevokeSessions(int id, CancellationToken ct) { await service.RevokeUserSessionsAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/api-keys"), Produces("application/json")]
public class AdminApiKeysController(AdminIdentityService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<AdminApiKeyRowDto>>>> List([FromQuery] AdminListQuery query, CancellationToken ct) => Ok(ApiResponse.Ok(await service.ApiKeysAsync(query, ct)));
    [HttpPost("{id:int}/revoke")] public async Task<ActionResult<ApiResponse<OkDto>>> Revoke(int id, CancellationToken ct) { await service.RevokeApiKeyAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    [HttpPost("{id:int}/tier")] public async Task<ActionResult<ApiResponse<OkDto>>> Tier(int id, [FromBody] SetRateTierRequest request, CancellationToken ct) { await service.SetRateTierAsync(id, request, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/sessions"), Produces("application/json")]
public class AdminSessionsController(AdminIdentityService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<AdminSessionDto>>>> List([FromQuery] AdminListQuery query, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SessionsAsync(query, ct)));
    [HttpDelete("{id:long}")] public async Task<ActionResult<ApiResponse<OkDto>>> Revoke(long id, CancellationToken ct) { await service.RevokeSessionAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/identity"), Produces("application/json")]
public class AdminIdentityController(AdminIdentityService service) : ControllerBase
{
    [HttpGet("roles")] public async Task<ActionResult<ApiResponse<List<RoleDto>>>> Roles(CancellationToken ct) => Ok(ApiResponse.Ok(await service.RolesAsync(ct)));
    [HttpPost("roles")] public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] SaveRoleRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveRoleAsync(null, request, ct)));
    [HttpPut("roles/{id:int}")] public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(int id, [FromBody] SaveRoleRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveRoleAsync(id, request, ct)));
    [HttpDelete("roles/{id:int}")] public async Task<ActionResult<ApiResponse<OkDto>>> DeleteRole(int id, CancellationToken ct) { await service.DeleteRoleAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    [HttpGet("menus")] public async Task<ActionResult<ApiResponse<List<MenuDto>>>> Menus(CancellationToken ct) => Ok(ApiResponse.Ok(await service.MenusAsync(ct)));
    [HttpPost("menus")] public async Task<ActionResult<ApiResponse<MenuDto>>> CreateMenu([FromBody] SaveMenuRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveMenuAsync(null, request, ct)));
    [HttpPut("menus/{id:int}")] public async Task<ActionResult<ApiResponse<MenuDto>>> UpdateMenu(int id, [FromBody] SaveMenuRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveMenuAsync(id, request, ct)));
    [HttpDelete("menus/{id:int}")] public async Task<ActionResult<ApiResponse<OkDto>>> DeleteMenu(int id, CancellationToken ct) { await service.DeleteMenuAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    [HttpGet("roles/{roleId:int}/menus")] public async Task<ActionResult<ApiResponse<List<RoleMenuDto>>>> RoleMenus(int roleId, CancellationToken ct) => Ok(ApiResponse.Ok(await service.RoleMenusAsync(roleId, ct)));
    [HttpPut("roles/{roleId:int}/menus")] public async Task<ActionResult<ApiResponse<OkDto>>> SaveRoleMenus(int roleId, [FromBody] SaveRoleMenusRequest request, CancellationToken ct) { await service.SaveRoleMenusAsync(roleId, request, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    [HttpGet("roles/{roleId:int}/actions")] public async Task<ActionResult<ApiResponse<List<RoleActionDto>>>> RoleActions(int roleId, CancellationToken ct) => Ok(ApiResponse.Ok(await service.RoleActionsAsync(roleId, ct)));
    [HttpPut("roles/{roleId:int}/actions")] public async Task<ActionResult<ApiResponse<OkDto>>> SaveRoleActions(int roleId, [FromBody] SaveRoleActionsRequest request, CancellationToken ct) { await service.SaveRoleActionsAsync(roleId, request, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    [HttpGet("controller-actions")] public ActionResult<ApiResponse<List<ControllerActionsDto>>> ControllerActions() => Ok(ApiResponse.Ok(AdminIdentityService.ControllerActions()));
}

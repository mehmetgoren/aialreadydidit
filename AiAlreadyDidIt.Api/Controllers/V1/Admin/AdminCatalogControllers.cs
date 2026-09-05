using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Infrastructure.Auth;
using AiAlreadyDidIt.Api.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Admin;

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/categories"), Produces("application/json")]
public class AdminCategoriesController(AdminCatalogService service) : ControllerBase
{
    [HttpGet("tree")] public async Task<ActionResult<ApiResponse<List<AdminCategoryNodeDto>>>> Tree(CancellationToken ct) => Ok(ApiResponse.Ok(await service.TreeAsync(ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<AdminCategoryNodeDto>>> Create([FromBody] SaveCategoryRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.CreateCategoryAsync(request, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<AdminCategoryNodeDto>>> Update(int id, [FromBody] SaveCategoryRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.UpdateCategoryAsync(id, request, ct)));
    [HttpPost("{id:int}/approve")] public async Task<ActionResult<ApiResponse<AdminCategoryNodeDto>>> Approve(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await service.ApproveProposedAsync(id, ct)));
    [HttpPost("{id:int}/move")] public async Task<ActionResult<ApiResponse<AdminCategoryNodeDto>>> Move(int id, [FromBody] MoveCategoryRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.MoveCategoryAsync(id, request, ct)));
    [HttpPost("reorder")] public async Task<ActionResult<ApiResponse<OkDto>>> Reorder([FromBody] ReorderCategoriesRequest request, CancellationToken ct) { await service.ReorderCategoriesAsync(request, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    [HttpPost("{id:int}/merge")] public async Task<ActionResult<ApiResponse<OkDto>>> Merge(int id, [FromBody] MergeRequest request, CancellationToken ct) { await service.MergeCategoryAsync(id, request, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<OkDto>>> Delete(int id, CancellationToken ct) { await service.DeleteCategoryAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/tags"), Produces("application/json")]
public class AdminTagsController(AdminCatalogService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<PagedResult<AdminTagDto>>>> List([FromQuery] AdminListQuery query, CancellationToken ct) => Ok(ApiResponse.Ok(await service.TagsAsync(query, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<AdminTagDto>>> Update(int id, [FromBody] SaveTagRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.UpdateTagAsync(id, request, ct)));
    [HttpPost("{id:int}/merge")] public async Task<ActionResult<ApiResponse<OkDto>>> Merge(int id, [FromBody] MergeRequest request, CancellationToken ct) { await service.MergeTagAsync(id, request, ct); return Ok(ApiResponse.Ok(new OkDto())); }
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<OkDto>>> Delete(int id, CancellationToken ct) { await service.DeleteTagAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/licenses"), Produces("application/json")]
public class AdminLicensesController(AdminCatalogService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<List<AdminLicenseDto>>>> List(CancellationToken ct) => Ok(ApiResponse.Ok(await service.LicensesAsync(ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<AdminLicenseDto>>> Create([FromBody] SaveLicenseRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveLicenseAsync(null, request, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<AdminLicenseDto>>> Update(int id, [FromBody] SaveLicenseRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveLicenseAsync(id, request, ct)));
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<OkDto>>> Delete(int id, CancellationToken ct) { await service.DeleteLicenseAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/platforms"), Produces("application/json")]
public class AdminPlatformsController(AdminCatalogService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<List<AdminPlatformDto>>>> List(CancellationToken ct) => Ok(ApiResponse.Ok(await service.PlatformsAsync(ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<AdminPlatformDto>>> Create([FromBody] SavePlatformRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SavePlatformAsync(null, request, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<AdminPlatformDto>>> Update(int id, [FromBody] SavePlatformRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SavePlatformAsync(id, request, ct)));
}

[ApiController, Authorize, RoleActionAuthorize, Route("api/v1/admin/llm-models"), Produces("application/json")]
public class AdminLlmModelsController(AdminCatalogService service) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ApiResponse<List<AdminLlmModelDto>>>> List(CancellationToken ct) => Ok(ApiResponse.Ok(await service.LlmModelsAsync(ct)));
    [HttpPost] public async Task<ActionResult<ApiResponse<AdminLlmModelDto>>> Create([FromBody] SaveLlmModelRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveLlmModelAsync(null, request, ct)));
    [HttpPut("{id:int}")] public async Task<ActionResult<ApiResponse<AdminLlmModelDto>>> Update(int id, [FromBody] SaveLlmModelRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await service.SaveLlmModelAsync(id, request, ct)));
    [HttpDelete("{id:int}")] public async Task<ActionResult<ApiResponse<OkDto>>> Delete(int id, CancellationToken ct) { await service.DeleteLlmModelAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

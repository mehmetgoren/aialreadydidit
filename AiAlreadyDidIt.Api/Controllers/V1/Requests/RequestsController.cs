using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Contracts.Requests;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Requests;

/// <summary>"Wanted" board — apps people looked for and did not find.</summary>
[ApiController]
[Route("api/v1/requests")]
[Produces("application/json")]
public class RequestsController(AppRequestService requests, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<AppRequestDto>>>> List([FromQuery] string? status, [FromQuery] string? q, [FromQuery] string? sort, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(ApiResponse.Ok(await requests.ListAsync(status, q, sort, page, pageSize, ct)));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AppRequestDto>>> Get(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await requests.GetAsync(id, ct)));

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AppRequestDto>>> Create([FromBody] CreateAppRequestRequest request, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await requests.CreateAsync(request, currentUser.IsApiKey ? RequestSource.Api : RequestSource.Web, ct)));

    [HttpPost("{id:int}/vote")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AppRequestDto>>> Vote(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await requests.VoteAsync(id, ct)));

    [HttpPost("{id:int}/fulfil")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AppRequestDto>>> Fulfil(int id, [FromBody] FulfilRequestRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await requests.FulfilAsync(id, request, ct)));

    [HttpPost("{id:int}/close")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OkDto>>> Close(int id, CancellationToken ct) { await requests.CloseAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }
}

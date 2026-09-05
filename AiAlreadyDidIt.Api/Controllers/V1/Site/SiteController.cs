using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Contracts.Site;
using AiAlreadyDidIt.Api.Services.Site;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Site;

[ApiController]
[AllowAnonymous]
[Route("api/v1/site")]
[Produces("application/json")]
public class SiteController(SiteService siteService, SavingsService savings) : ControllerBase
{
    /// <summary>Boot configuration for the SPA (feature flags, lists, limits).</summary>
    [HttpGet("config")]
    public async Task<ActionResult<ApiResponse<SiteConfigDto>>> Config(CancellationToken ct) => Ok(ApiResponse.Ok(await siteService.GetConfigAsync(ct)));

    /// <summary>The mission counter: estimated tokens, money and energy saved by re-using apps instead of regenerating them.</summary>
    [HttpGet("savings")]
    public async Task<ActionResult<ApiResponse<SavingsDto>>> Savings(CancellationToken ct) => Ok(ApiResponse.Ok(await savings.GetAsync(ct)));
}

using AiAlreadyDidIt.Api.Contracts.Agent;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Agent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Agent;

/// <summary>Entry point for LLM agents (Claude Code, Codex, custom agents): "has someone already built this?"</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/agent")]
[Produces("application/json")]
public class AgentController(AgentService agent, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Machine-readable usage guide (also served as /llms.txt).</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AgentDocsDto>>> Docs(CancellationToken ct) => Ok(ApiResponse.Ok(await agent.DocsAsync(ct)));

    /// <summary>One call before generating anything: verdict download | fork | build plus the best matches with download links.</summary>
    [HttpGet("check")]
    public async Task<ActionResult<ApiResponse<AgentCheckDto>>> Check([FromQuery] string q, [FromQuery] string? platform, [FromQuery] int take = 5, [FromQuery] string? source = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3) throw ApiException.BadRequest("q must describe the application (at least 3 characters).");
        var src = source?.ToLowerInvariant() == "mcp" ? RequestSource.Mcp : currentUser.IsApiKey ? RequestSource.Api : RequestSource.Web;
        return Ok(ApiResponse.Ok(await agent.CheckAsync(q.Trim(), platform, take, src, ct)));
    }
}

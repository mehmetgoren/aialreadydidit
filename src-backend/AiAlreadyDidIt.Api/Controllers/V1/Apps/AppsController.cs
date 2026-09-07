using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Apps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AiAlreadyDidIt.Api.Controllers.V1.Apps;

/// <summary>Per-app actions available to everyone: download, ratings, report.</summary>
[ApiController]
[Route("api/v1/apps")]
[Produces("application/json")]
public class AppsController(DownloadService downloads, RatingService ratings, ReportService reports, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Records the download and redirects (302) to a time-limited file URL. Agents send <c>Accept: application/json</c>
    /// (or <c>?json=1</c>) to receive the URL, sha256 and install hint instead of a redirect.
    /// </summary>
    [HttpGet("{slug}/download/{fileId:int}")]
    [AllowAnonymous]
    [EnableRateLimiting("downloads")]
    public async Task<IActionResult> Download(string slug, int fileId, [FromQuery] string? json = null, [FromQuery] string? source = null, CancellationToken ct = default)
    {
        var src = source?.ToLowerInvariant() switch { "mcp" => DownloadSource.Mcp, "seo" => DownloadSource.Seo, _ => currentUser.IsApiKey ? DownloadSource.Api : DownloadSource.Web };
        var link = await downloads.GetLinkAsync(slug, fileId, src, ct);
        var wantsJson = json is "1" or "true" || Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) && !Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
        if (wantsJson) return Ok(ApiResponse.Ok(link));
        return Redirect(link.Url);
    }

    // ---------------------------------------------------------------- ratings

    [HttpGet("{slug}/ratings")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<RatingDto>>>> Ratings(string slug, [FromQuery] string? sort, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        Ok(ApiResponse.Ok(await ratings.ListAsync(slug, sort, page, pageSize, ct)));

    [HttpGet("{slug}/ratings/summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<RatingSummaryDto>>> RatingSummary(string slug, CancellationToken ct) => Ok(ApiResponse.Ok(await ratings.SummaryAsync(slug, ct)));

    /// <summary>Create or update my rating (0-100 + review + worked / didn't work). Requires a prior download.</summary>
    [HttpPut("{slug}/ratings/mine")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<RatingDto>>> Rate(string slug, [FromBody] CreateRatingRequest request, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await ratings.UpsertAsync(slug, request, ct)));

    [HttpDelete("{slug}/ratings/mine")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OkDto>>> DeleteRating(string slug, CancellationToken ct)
    {
        await ratings.DeleteMineAsync(slug, ct);
        return Ok(ApiResponse.Ok(new OkDto()));
    }

    [HttpPost("ratings/{ratingId:int}/vote")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<RatingDto>>> Vote(int ratingId, [FromBody] VoteRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await ratings.VoteAsync(ratingId, request, ct)));

    [HttpPost("ratings/{ratingId:int}/reply")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<RatingDto>>> Reply(int ratingId, [FromBody] ReplyRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await ratings.ReplyAsync(ratingId, request, ct)));

    // ---------------------------------------------------------------- report

    [HttpPost("{slug}/report")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ApiResponse<int>>> Report(string slug, [FromBody] CreateReportRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await reports.CreateAsync(slug, request, ct)));
}

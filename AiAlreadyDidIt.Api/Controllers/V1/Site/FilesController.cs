using AiAlreadyDidIt.Api.Infrastructure.Storage;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Site;

/// <summary>Streams public images (screenshots, icons, banners) from object storage with long cache headers.</summary>
[ApiController]
[AllowAnonymous]
[Route("files")]
public class FilesController(IObjectStorage storage) : ControllerBase
{
    [HttpGet("{bucket}/{**key}")]
    [ResponseCache(Duration = 86400 * 30, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(string bucket, string key, CancellationToken ct)
    {
        if (!FileUrls.TryParseBucket(bucket, out var b) || string.IsNullOrWhiteSpace(key) || key.Contains("..")) return NotFound();
        var stat = await storage.StatAsync(b, key, ct);
        if (stat is null) return NotFound();
        var stream = await storage.OpenReadAsync(b, key, ct);
        Response.Headers.CacheControl = "public, max-age=2592000, immutable";
        return File(stream, stat.ContentType ?? "application/octet-stream");
    }
}

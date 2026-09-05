using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Services.Apps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiAlreadyDidIt.Api.Controllers.V1.Apps;

/// <summary>The upload wizard and "my apps" editor (uploader only).</summary>
[ApiController]
[Authorize]
[Route("api/v1/my/apps")]
[Produces("application/json")]
public class MyAppsController(AppEditorService editor, CategorySuggestionService suggestions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AppCardDto>>>> List(CancellationToken ct) => Ok(ApiResponse.Ok(await editor.ListMineAsync(ct)));

    /// <summary>Start a draft (optionally with initial fields).</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> Create([FromBody] SaveDraftRequest? request, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.CreateDraftAsync(request, ct)));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> Get(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.GetDraftAsync(id, ct)));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> Update(int id, [FromBody] SaveDraftRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.UpdateDraftAsync(id, request, ct)));

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<OkDto>>> Delete(int id, CancellationToken ct) { await editor.DeleteDraftAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    [HttpGet("{id:int}/stats")]
    public async Task<ActionResult<ApiResponse<MyAppStatsDto>>> Stats(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.GetStatsAsync(id, ct)));

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> Submit(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.SubmitAsync(id, ct)));

    [HttpPost("{id:int}/withdraw")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> Withdraw(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.WithdrawAsync(id, ct)));

    [HttpPost("{id:int}/unlist")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> Unlist(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.SetListedAsync(id, false, ct)));

    [HttpPost("{id:int}/relist")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> Relist(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.SetListedAsync(id, true, ct)));

    // ---------------------------------------------------------------- source

    /// <summary>Read a public GitHub / GitLab repository (README, license, stars, releases) without attaching it.</summary>
    [HttpPost("inspect-repository")]
    public async Task<ActionResult<ApiResponse<RepositoryInspectionDto>>> InspectRepository([FromBody] InspectRepositoryRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.InspectRepositoryAsync(request, ct)));

    [HttpPost("{id:int}/source/repository")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> AttachRepository(int id, [FromBody] AttachRepositoryRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.AttachRepositoryAsync(id, request, ct)));

    [HttpPost("{id:int}/source/archive")]
    [RequestSizeLimit(220L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 220L * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> UploadArchive(int id, IFormFile file, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.UploadSourceArchiveAsync(id, file, ct)));

    [HttpDelete("{id:int}/source")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> RemoveSource(int id, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.RemoveSourceAsync(id, ct)));

    // ---------------------------------------------------------------- files

    [HttpPost("{id:int}/versions/{versionId:int}/files")]
    [RequestSizeLimit(520L * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 520L * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<AppFileDto>>> UploadInstaller(int id, int versionId, [FromForm] string platformCode, IFormFile file, [FromForm] string? installHint, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await editor.UploadInstallerAsync(id, versionId, platformCode, file, installHint, ct)));

    [HttpPost("{id:int}/versions/{versionId:int}/files/external")]
    public async Task<ActionResult<ApiResponse<AppFileDto>>> AddExternal(int id, int versionId, [FromBody] AddExternalFileRequest request, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await editor.AddExternalFileAsync(id, versionId, request, ct)));

    [HttpPost("{id:int}/versions/{versionId:int}/files/import-asset")]
    public async Task<ActionResult<ApiResponse<AppFileDto>>> ImportAsset(int id, int versionId, [FromBody] ImportReleaseAssetRequest request, CancellationToken ct) =>
        Ok(ApiResponse.Ok(await editor.ImportReleaseAssetAsync(id, versionId, request, ct)));

    [HttpPut("{id:int}/files/{fileId:int}")]
    public async Task<ActionResult<ApiResponse<AppFileDto>>> UpdateFile(int id, int fileId, [FromBody] UpdateFileRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.UpdateFileAsync(id, fileId, request, ct)));

    [HttpDelete("{id:int}/files/{fileId:int}")]
    public async Task<ActionResult<ApiResponse<OkDto>>> DeleteFile(int id, int fileId, CancellationToken ct) { await editor.DeleteFileAsync(id, fileId, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    // ---------------------------------------------------------------- versions

    [HttpPost("{id:int}/versions")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> CreateVersion(int id, [FromBody] SaveVersionRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.CreateVersionAsync(id, request, ct)));

    [HttpPut("{id:int}/versions/{versionId:int}")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> UpdateVersion(int id, int versionId, [FromBody] SaveVersionRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.UpdateVersionAsync(id, versionId, request, ct)));

    [HttpDelete("{id:int}/versions/{versionId:int}")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> DeleteVersion(int id, int versionId, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.DeleteVersionAsync(id, versionId, ct)));

    [HttpPost("{id:int}/versions/{versionId:int}/submit")]
    public async Task<ActionResult<ApiResponse<AppDraftDto>>> SubmitVersion(int id, int versionId, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.SubmitVersionAsync(id, versionId, ct)));

    // ---------------------------------------------------------------- media

    [HttpPost("{id:int}/screenshots")]
    [RequestSizeLimit(12L * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<ScreenshotDto>>> UploadScreenshot(int id, IFormFile file, [FromForm] string? caption, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.UploadScreenshotAsync(id, file, caption, ct)));

    [HttpDelete("{id:int}/screenshots/{screenshotId:int}")]
    public async Task<ActionResult<ApiResponse<OkDto>>> DeleteScreenshot(int id, int screenshotId, CancellationToken ct) { await editor.DeleteScreenshotAsync(id, screenshotId, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    [HttpPost("{id:int}/screenshots/reorder")]
    public async Task<ActionResult<ApiResponse<OkDto>>> ReorderScreenshots(int id, [FromBody] ReorderRequest request, CancellationToken ct) { await editor.ReorderScreenshotsAsync(id, request, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    [HttpPut("{id:int}/screenshots/{screenshotId:int}")]
    public async Task<ActionResult<ApiResponse<OkDto>>> Caption(int id, int screenshotId, [FromBody] ScreenshotCaptionRequest request, CancellationToken ct) { await editor.SetScreenshotCaptionAsync(id, screenshotId, request, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    [HttpPost("{id:int}/icon")]
    [RequestSizeLimit(4L * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<string>>> UploadIcon(int id, IFormFile file, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.UploadIconAsync(id, file, ct)));

    [HttpDelete("{id:int}/icon")]
    public async Task<ActionResult<ApiResponse<OkDto>>> DeleteIcon(int id, CancellationToken ct) { await editor.DeleteIconAsync(id, ct); return Ok(ApiResponse.Ok(new OkDto())); }

    // ---------------------------------------------------------------- helpers used while typing

    /// <summary>Semantic near-duplicate check for the description being typed ("This looks ~85% similar to X").</summary>
    [HttpPost("check-duplicates")]
    public async Task<ActionResult<ApiResponse<DuplicateCheckDto>>> CheckDuplicates([FromBody] DuplicateCheckRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await editor.CheckDuplicatesAsync(request, ct)));

    /// <summary>LLM suggestion for category, tags and short description.</summary>
    [HttpPost("suggest-metadata")]
    public async Task<ActionResult<ApiResponse<MetadataSuggestionDto>>> Suggest([FromBody] SuggestMetadataRequest request, CancellationToken ct) => Ok(ApiResponse.Ok(await suggestions.SuggestAsync(request, ct)));
}

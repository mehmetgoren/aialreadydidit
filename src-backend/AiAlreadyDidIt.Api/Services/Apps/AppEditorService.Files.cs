using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Import;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Apps;

public sealed partial class AppEditorService
{
    // ---------------------------------------------------------------- installer files

    public async Task<AppFileDto> UploadInstallerAsync(int id, int versionId, string platformCode, IFormFile file, string? installHint, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var version = RequireEditableVersion(app, versionId);
        var platform = await RequirePlatformAsync(platformCode, ct);
        ValidateInstallerName(platform, file.FileName);
        if (file.Length == 0) throw ApiException.Unprocessable("The file is empty.", "file");
        if (file.Length > storageOptions.Value.MaxInstallerBytes) throw ApiException.Unprocessable($"Install files are limited to {TextUtil.HumanSize(storageOptions.Value.MaxInstallerBytes)}.", "file");

        var tempPath = Path.Combine(TempDir(), $"bin-{version.Id}-{Guid.NewGuid():N}");
        try
        {
            await using (var fs = File.Create(tempPath))
            await using (var input = file.OpenReadStream())
                await CopyLimitedAsync(input, fs, storageOptions.Value.MaxInstallerBytes, ct);
            var entity = await StoreInstallerFromTempAsync(app, version, platform, tempPath, file.FileName, file.ContentType, installHint, ct);
            await db.SaveChangesAsync(ct);
            return CatalogService.MapFile(app.Slug, entity);
        }
        finally { TryDelete(tempPath); }
    }

    private async Task<AppFile> StoreInstallerFromTempAsync(App app, AppVersion version, Platform platform, string tempPath, string originalName, string? contentType, string? installHint, CancellationToken ct)
    {
        var info = new FileInfo(tempPath);
        await using (var head = File.OpenRead(tempPath))
        {
            var buffer = new byte[Math.Min(InstallerSignature.HeadLength, info.Length)];
            var read = await head.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, ct);
            var problem = InstallerSignature.Check(originalName, buffer.AsMemory(0, read));
            if (problem is not null) throw ApiException.Unprocessable(problem, "file");
        }
        string sha;
        await using (var fs = File.OpenRead(tempPath)) sha = Convert.ToHexStringLower(await System.Security.Cryptography.SHA256.HashDataAsync(fs, ct));
        var safeName = SafeFileName(originalName);
        var key = $"{app.Id}/{version.Id}/{Guid.NewGuid():N}/{safeName}";
        await using (var fs = File.OpenRead(tempPath))
            await storage.PutAsync(Bucket.Installers, key, fs, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType, info.Length, ct);

        // one file per platform: replace the previous one for that platform
        foreach (var old in version.Files.Where(f => f.Kind != FileKind.Source && f.PlatformId == platform.Id).ToList())
        {
            await DeleteStoredAsync(old, ct);
            version.Files.Remove(old);
            db.AppFiles.Remove(old);
        }
        var entity = new AppFile
        {
            Version = version, PlatformId = platform.Id, Platform = platform, Kind = platform.Code == "web" ? FileKind.WebBundle : FileKind.Installer, FileName = safeName, StorageKey = key,
            SizeBytes = info.Length, Sha256 = sha, ContentType = contentType, ScanStatus = ScanStatus.Pending, InstallHint = string.IsNullOrWhiteSpace(installHint) ? null : installHint.Trim(), CreatedAt = Clock.Now
        };
        version.Files.Add(entity);
        db.AppFiles.Add(entity);
        app.UpdatedAt = Clock.Now;
        return entity;
    }

    public async Task<AppFileDto> AddExternalFileAsync(int id, int versionId, AddExternalFileRequest request, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var version = RequireEditableVersion(app, versionId);
        var platform = await RequirePlatformAsync(request.PlatformCode, ct);
        if (!platform.AllowsExternalReference) throw ApiException.Unprocessable($"{platform.Name} needs an uploaded install file, not a reference.", "platformCode");
        var reference = request.Reference.Trim();
        if (platform.Code == "web" && !Uri.TryCreate(reference, UriKind.Absolute, out var uri) || platform.Code == "web" && Uri.TryCreate(reference, UriKind.Absolute, out uri) && uri.Scheme is not ("http" or "https"))
            throw ApiException.Unprocessable("Enter the https:// URL of the hosted web app.", "reference");
        if (platform.Code == "docker" && (reference.Contains(' ') || reference.StartsWith("http", StringComparison.OrdinalIgnoreCase)))
            throw ApiException.Unprocessable("Enter a Docker image reference such as ghcr.io/user/app:1.0.", "reference");

        foreach (var old in version.Files.Where(f => f.Kind != FileKind.Source && f.PlatformId == platform.Id).ToList())
        {
            await DeleteStoredAsync(old, ct);
            version.Files.Remove(old);
            db.AppFiles.Remove(old);
        }
        var entity = new AppFile
        {
            Version = version, PlatformId = platform.Id, Platform = platform, Kind = platform.Code == "docker" ? FileKind.DockerImage : FileKind.WebBundle, FileName = reference,
            ExternalReference = reference, ScanStatus = ScanStatus.Skipped, ScanSignature = null, ScannedAt = Clock.Now, InstallHint = request.InstallHint?.Trim(), CreatedAt = Clock.Now
        };
        version.Files.Add(entity);
        db.AppFiles.Add(entity);
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return CatalogService.MapFile(app.Slug, entity);
    }

    /// <summary>Copies a release asset from the linked repository into the store (mirrors it so it survives if the repo disappears).</summary>
    public async Task<AppFileDto> ImportReleaseAssetAsync(int id, int versionId, ImportReleaseAssetRequest request, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var version = RequireEditableVersion(app, versionId);
        var platform = await RequirePlatformAsync(request.PlatformCode, ct);
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || uri.Scheme != "https") throw ApiException.Unprocessable("Asset URL must be https.", "url");
        var allowedHosts = new[] { "github.com", "objects.githubusercontent.com", "gitlab.com", "release-assets.githubusercontent.com" };
        var repoHost = app.RepoUrl is null ? null : new Uri(app.RepoUrl).Host;
        if (!allowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase) && !string.Equals(uri.Host, repoHost, StringComparison.OrdinalIgnoreCase))
            throw ApiException.Unprocessable("Only release assets of the linked repository host can be imported.", "url");
        var fileName = Path.GetFileName(uri.LocalPath);
        ValidateInstallerName(platform, fileName);

        var tempPath = Path.Combine(TempDir(), $"asset-{version.Id}-{Guid.NewGuid():N}");
        try
        {
            using (var client = httpClientFactory.CreateClient("download"))
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("AiAlreadyDidIt/1.0");
                using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!response.IsSuccessStatusCode) throw ApiException.Unprocessable($"The asset could not be downloaded ({(int)response.StatusCode}).", "url");
                await using var fs = File.Create(tempPath);
                await using var body = await response.Content.ReadAsStreamAsync(ct);
                await CopyLimitedAsync(body, fs, storageOptions.Value.MaxInstallerBytes, ct);
            }
            var entity = await StoreInstallerFromTempAsync(app, version, platform, tempPath, fileName, null, request.InstallHint, ct);
            await db.SaveChangesAsync(ct);
            return CatalogService.MapFile(app.Slug, entity);
        }
        finally { TryDelete(tempPath); }
    }

    public async Task<AppFileDto> UpdateFileAsync(int id, int fileId, UpdateFileRequest request, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var file = app.Versions.SelectMany(v => v.Files).FirstOrDefault(f => f.Id == fileId) ?? throw ApiException.NotFound("File not found.");
        var version = RequireEditableVersion(app, file.VersionId);
        if (file.Kind == FileKind.Source) throw ApiException.Unprocessable("The source snapshot has no platform.");
        if (!string.IsNullOrWhiteSpace(request.PlatformCode))
        {
            var platform = await RequirePlatformAsync(request.PlatformCode, ct);
            if (file.ExternalReference is null) ValidateInstallerName(platform, file.FileName);
            if (version.Files.Any(f => f.Id != file.Id && f.Kind != FileKind.Source && f.PlatformId == platform.Id)) throw ApiException.Unprocessable($"There is already a file for {platform.Name}.", "platformCode");
            file.PlatformId = platform.Id;
            file.Platform = platform;
        }
        if (request.InstallHint is not null) file.InstallHint = string.IsNullOrWhiteSpace(request.InstallHint) ? null : request.InstallHint.Trim();
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return CatalogService.MapFile(app.Slug, file);
    }

    public async Task DeleteFileAsync(int id, int fileId, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var file = app.Versions.SelectMany(v => v.Files).FirstOrDefault(f => f.Id == fileId) ?? throw ApiException.NotFound("File not found.");
        var version = RequireEditableVersion(app, file.VersionId);
        await DeleteStoredAsync(file, ct);
        version.Files.Remove(file);
        db.AppFiles.Remove(file);
        if (file.Kind == FileKind.Source) { app.SourceAnalyzedAt = null; app.HasLicenseFile = false; app.DetectedLicenseSpdxId = null; }
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
    }

    private async Task<Platform> RequirePlatformAsync(string code, CancellationToken ct) =>
        await db.Platforms.FirstOrDefaultAsync(p => p.Code == code.Trim().ToLowerInvariant() && p.IsActive, ct) ?? throw ApiException.Unprocessable("Unknown platform.", "platformCode");

    private static void ValidateInstallerName(Platform platform, string fileName)
    {
        var lower = fileName.ToLowerInvariant();
        var allowed = platform.AllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!allowed.Any(ext => lower.EndsWith(ext, StringComparison.Ordinal)))
            throw ApiException.Unprocessable($"{platform.Name} accepts: {string.Join(", ", allowed)}.", "file");
        if (lower.EndsWith(".html") || lower.EndsWith(".htm") || lower.EndsWith(".txt") || lower.EndsWith(".md"))
            throw ApiException.Unprocessable("That is not an install file.", "file");
    }

    // ---------------------------------------------------------------- versions

    public async Task<AppDraftDto> CreateVersionAsync(int id, SaveVersionRequest request, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        if (app.PublishedAt is null) throw ApiException.Unprocessable("Submit the first version of the app before adding another one.");
        if (app.Versions.Any(v => v.Status is VersionStatus.Draft or VersionStatus.PendingScan or VersionStatus.PendingReview)) throw ApiException.Unprocessable("There is already a version in progress.");
        var versionName = request.Version.Trim();
        if (app.Versions.Any(v => v.Version.Equals(versionName, StringComparison.OrdinalIgnoreCase))) throw ApiException.Unprocessable($"Version {versionName} already exists.", "version");
        var version = new AppVersion
        {
            Version = versionName, Changelog = request.Changelog?.Trim(), ReleasedAt = request.ReleasedAt ?? Clock.Now, SourceRef = request.SourceRef?.Trim(),
            Status = VersionStatus.Draft, CreatedByUserId = currentUser.Id, CreatedAt = Clock.Now
        };
        app.Versions.Add(version);
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        if (app.SourceKind == SourceKind.Repository)
        {
            app.SourceAnalyzedAt = null;
            app.SourceWarnings = "Importing the repository snapshot…";
            jobs.Enqueue(Infrastructure.Jobs.JobTypes.ImportRepository, new { AppId = app.Id, VersionId = version.Id }, $"version:{version.Id}");
            await db.SaveChangesAsync(ct);
        }
        return await MapDraftAsync(app, ct);
    }

    public async Task<AppDraftDto> UpdateVersionAsync(int id, int versionId, SaveVersionRequest request, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var version = app.Versions.FirstOrDefault(v => v.Id == versionId) ?? throw ApiException.NotFound("Version not found.");
        var versionName = request.Version.Trim();
        if (app.Versions.Any(v => v.Id != versionId && v.Version.Equals(versionName, StringComparison.OrdinalIgnoreCase))) throw ApiException.Unprocessable($"Version {versionName} already exists.", "version");
        var editable = version.Status is VersionStatus.Draft or VersionStatus.Rejected;
        if (editable) { version.Version = versionName; if (request.ReleasedAt is not null) version.ReleasedAt = request.ReleasedAt.Value; }
        version.Changelog = request.Changelog?.Trim(); // changelog may be edited after publishing
        if (editable && app.SourceKind == SourceKind.Repository && request.SourceRef is not null && request.SourceRef.Trim() != (version.SourceRef ?? string.Empty))
        {
            version.SourceRef = string.IsNullOrWhiteSpace(request.SourceRef) ? null : request.SourceRef.Trim();
            await RemoveSourceFilesAsync(version, ct);
            app.SourceAnalyzedAt = null;
            jobs.Enqueue(Infrastructure.Jobs.JobTypes.ImportRepository, new { AppId = app.Id, VersionId = version.Id }, $"version:{version.Id}");
        }
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return await MapDraftAsync(app, ct);
    }

    public async Task<AppDraftDto> DeleteVersionAsync(int id, int versionId, CancellationToken ct)
    {
        var app = await LoadOwnedAsync(id, ct);
        var version = RequireEditableVersion(app, versionId);
        if (app.Versions.Count == 1) throw ApiException.Unprocessable("An app needs at least one version. Delete the draft app instead.");
        foreach (var f in version.Files) await DeleteStoredAsync(f, ct);
        db.AppVersions.Remove(version);
        app.Versions.Remove(version);
        app.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
        return await MapDraftAsync(app, ct);
    }

    /// <summary>Submits a new version of an already-published app: scan, then review (or auto-publish for trusted uploaders).</summary>
    public async Task<AppDraftDto> SubmitVersionAsync(int id, int versionId, CancellationToken ct)
    {
        await EnsureCanUploadAsync(ct);
        var app = await LoadOwnedAsync(id, ct);
        if (app.PublishedAt is null) return await SubmitAsync(id, ct);
        var version = RequireEditableVersion(app, versionId);
        var readiness = await GetReadinessAsync(app, version, ct);
        var blocking = readiness.Issues.Where(i => i.Blocking && i.Step is "files" or "source").ToList();
        if (blocking.Count > 0) throw ApiException.Unprocessable(blocking.Select(i => ApiErrors.Unprocessable(i.Message, i.Code)));
        version.Status = VersionStatus.PendingScan;
        version.RejectionReason = null;
        foreach (var f in version.Files.Where(f => f.ScanStatus == ScanStatus.Error)) f.ScanStatus = ScanStatus.Pending;
        app.UpdatedAt = Clock.Now;
        jobs.Enqueue(Infrastructure.Jobs.JobTypes.ScanVersion, new { AppId = app.Id, VersionId = version.Id }, $"version:{version.Id}");
        await db.SaveChangesAsync(ct);
        return await MapDraftAsync(app, ct);
    }
}

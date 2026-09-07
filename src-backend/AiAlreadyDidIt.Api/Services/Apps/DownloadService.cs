using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Storage;
using AiAlreadyDidIt.Api.Services.Site;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Services.Apps;

/// <summary>Records a download (the rating gate + savings counter input) and hands out a presigned URL.</summary>
public sealed class DownloadService(AadiDbContext db, IObjectStorage storage, ICurrentUser currentUser, IOptions<StorageOptions> storageOptions,
    IOptions<JwtOptions> jwt, SavingsService savings)
{
    public async Task<DownloadLinkDto> GetLinkAsync(string slug, int fileId, DownloadSource source, CancellationToken ct)
    {
        var file = await db.AppFiles.AsNoTracking().Include(f => f.Platform).Include(f => f.Version).ThenInclude(v => v.App)
            .FirstOrDefaultAsync(f => f.Id == fileId && f.Version.App.Slug == slug, ct) ?? throw ApiException.NotFound("File not found.");
        var app = file.Version.App;
        var isOwner = currentUser.IdOrNull == app.UploaderUserId;
        var visible = (app.Status is AppStatus.Published or AppStatus.Unlisted && file.Version.Status == VersionStatus.Published) || isOwner || currentUser.IsAdmin;
        if (!visible) throw ApiException.NotFound("File not found.");
        if (file.ScanStatus == ScanStatus.Infected) throw ApiException.Forbidden("This file was flagged by the antivirus scan and cannot be downloaded.");
        if (file.ScanStatus == ScanStatus.Pending && !isOwner && !currentUser.IsAdmin) throw ApiException.Unprocessable("This file has not been scanned yet.");
        if (currentUser.IsApiKey && !currentUser.Scopes.Contains("download")) throw ApiException.Forbidden("This API key has no 'download' scope.");

        var record = app.Status == AppStatus.Published && !isOwner;
        if (record)
        {
            db.Downloads.Add(new Download
            {
                AppId = app.Id, VersionId = file.VersionId, FileId = file.Id, UserId = currentUser.IdOrNull, ApiKeyId = currentUser.ApiKeyId, Source = source,
                IpHash = TextUtil.HashIp(currentUser.IpAddress, jwt.Value.Key), UserAgent = TextUtil.Truncate(currentUser.UserAgent, 500), CreatedAt = Clock.Now
            });
            await db.Apps.Where(a => a.Id == app.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.DownloadCount, a => a.DownloadCount + 1), ct);
            await db.AppVersions.Where(v => v.Id == file.VersionId).ExecuteUpdateAsync(s => s.SetProperty(v => v.DownloadCount, v => v.DownloadCount + 1), ct);
            await db.AppFiles.Where(f => f.Id == file.Id).ExecuteUpdateAsync(s => s.SetProperty(f => f.DownloadCount, f => f.DownloadCount + 1), ct);
            if (currentUser.ApiKeyId is { } keyId)
                await db.ApiKeys.Where(k => k.Id == keyId).ExecuteUpdateAsync(s => s.SetProperty(k => k.DownloadCount, k => k.DownloadCount + 1), ct);
            await db.SaveChangesAsync(ct);
            savings.Invalidate();
        }

        var ttl = TimeSpan.FromMinutes(storageOptions.Value.PresignedUrlMinutes);
        string url;
        if (file.ExternalReference is not null) url = file.ExternalReference;
        else url = storage.GetPresignedDownloadUrl(file.Kind == FileKind.Source ? Bucket.Sources : Bucket.Installers, file.StorageKey!, file.FileName, ttl);
        return new DownloadLinkDto
        {
            Url = url, FileName = file.FileName, SizeBytes = file.SizeBytes, Sha256 = file.Sha256, ContentType = file.ContentType,
            InstallHint = file.InstallHint ?? file.Platform?.InstallHint, ExternalReference = file.ExternalReference, ExpiresAt = Clock.Now.Add(ttl),
            Version = file.Version.Version, PlatformCode = file.Platform?.Code
        };
    }
}

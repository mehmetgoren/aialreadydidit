using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Services.Apps;

public sealed class ReportService(AadiDbContext db, ICurrentUser currentUser, SiteSettingsCache settings, IOptions<JwtOptions> jwt)
{
    public async Task<int> CreateAsync(string slug, CreateReportRequest request, CancellationToken ct)
    {
        var app = await db.Apps.AsNoTracking().FirstOrDefaultAsync(a => a.Slug == slug && (a.Status == AppStatus.Published || a.Status == AppStatus.Unlisted), ct) ?? throw ApiException.NotFound("App not found.");
        if (!currentUser.IsAuthenticated && !await settings.GetBoolAsync(SettingKeys.AllowAnonymousReports, true, ct)) throw ApiException.Unauthorized("Sign in to report an app.");
        var ipHash = TextUtil.HashIp(currentUser.IpAddress, jwt.Value.Key);
        var recent = Clock.Now.AddHours(-1);
        var dupes = await db.Reports.CountAsync(r => r.AppId == app.Id && r.CreatedAt >= recent && (currentUser.IdOrNull != null ? r.ReporterUserId == currentUser.IdOrNull : r.IpHash == ipHash), ct);
        if (dupes >= 3) throw ApiException.Unprocessable("You have already reported this app recently.");
        if (request.RatingId is { } rid && !await db.Ratings.AnyAsync(r => r.Id == rid && r.AppId == app.Id, ct)) throw ApiException.Unprocessable("Review not found.", "ratingId");
        var report = new Report
        {
            AppId = app.Id, RatingId = request.RatingId, ReporterUserId = currentUser.IdOrNull, ReporterEmail = currentUser.IsAuthenticated ? null : request.Email?.Trim(),
            Reason = request.Reason, Details = string.IsNullOrWhiteSpace(request.Details) ? null : TextUtil.Truncate(request.Details.Trim(), 4000), Status = ReportStatus.Open, IpHash = ipHash, CreatedAt = Clock.Now
        };
        db.Reports.Add(report);
        await db.SaveChangesAsync(ct);
        return report.Id;
    }
}

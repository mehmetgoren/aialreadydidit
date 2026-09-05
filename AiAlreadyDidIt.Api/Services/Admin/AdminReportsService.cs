using System.Linq.Expressions;
using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Apps;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Admin;

public sealed class AdminReportsService(AadiDbContext db, ICurrentUser currentUser, AuditService audit, NotificationService notifications, AppLifecycleService lifecycle, CatalogService catalog)
{
    public Expression<Func<Report, AdminReportDto>> Projection() => r => new AdminReportDto
    {
        Id = r.Id, AppId = r.AppId, AppSlug = r.App == null ? null : r.App.Slug, AppName = r.App == null ? null : r.App.Name, AppStatus = r.App == null ? null : r.App.Status,
        RatingId = r.RatingId, RatingReview = r.RatingId == null ? null : db.Ratings.Where(x => x.Id == r.RatingId).Select(x => x.Review).FirstOrDefault(),
        ReporterUsername = r.ReporterUserId == null ? null : db.Users.Where(u => u.Id == r.ReporterUserId).Select(u => u.Username).FirstOrDefault(), ReporterEmail = r.ReporterEmail,
        Reason = r.Reason, Details = r.Details, Status = r.Status, HandledBy = r.HandledByUserId == null ? null : db.Users.Where(u => u.Id == r.HandledByUserId).Select(u => u.Username).FirstOrDefault(),
        Resolution = r.Resolution, CreatedAt = r.CreatedAt, ResolvedAt = r.ResolvedAt
    };

    public async Task<PagedResult<AdminReportDto>> ListAsync(AdminListQuery query, CancellationToken ct)
    {
        var q = db.Reports.AsNoTracking().AsQueryable();
        q = query.Status?.ToLowerInvariant() switch
        {
            "open" or null or "" => q.Where(r => r.Status == ReportStatus.Open || r.Status == ReportStatus.Reviewing),
            "resolved" => q.Where(r => r.Status == ReportStatus.Resolved),
            "dismissed" => q.Where(r => r.Status == ReportStatus.Dismissed),
            _ => q
        };
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(r => r.App != null && EF.Functions.ILike(r.App.Name, $"%{query.Q}%") || r.Details != null && EF.Functions.ILike(r.Details, $"%{query.Q}%"));
        return await q.OrderByDescending(r => r.CreatedAt).Select(Projection()).ToPagedAsync(query.SafePage, query.SafePageSize, ct);
    }

    public async Task<AdminReportDto> GetAsync(int id, CancellationToken ct) =>
        await db.Reports.AsNoTracking().Where(r => r.Id == id).Select(Projection()).FirstOrDefaultAsync(ct) ?? throw ApiException.NotFound("Report not found.");

    public async Task<AdminReportDto> ResolveAsync(int id, ResolveReportRequest request, CancellationToken ct)
    {
        var report = await db.Reports.Include(r => r.App).FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw ApiException.NotFound("Report not found.");
        report.Status = request.Status;
        report.Resolution = request.Resolution?.Trim();
        report.HandledByUserId = currentUser.Id;
        report.ResolvedAt = request.Status is ReportStatus.Resolved or ReportStatus.Dismissed ? Clock.Now : null;
        var app = report.App;
        switch (request.Action?.ToLowerInvariant())
        {
            case "unlist" when app is not null:
                app.Status = AppStatus.Unlisted; app.UpdatedAt = Clock.Now;
                db.ModerationActions.Add(new ModerationAction { AppId = app.Id, AdminUserId = currentUser.Id, Action = ModerationActionKind.Unlist, Note = "Report #" + id + ": " + request.Resolution, CreatedAt = Clock.Now });
                break;
            case "remove" when app is not null:
                app.Status = AppStatus.Removed; app.UpdatedAt = Clock.Now;
                db.ModerationActions.Add(new ModerationAction { AppId = app.Id, AdminUserId = currentUser.Id, Action = ModerationActionKind.Remove, Note = "Report #" + id + ": " + request.Resolution, CreatedAt = Clock.Now });
                break;
            case "hide_review" when report.RatingId is { } rid:
                await db.Ratings.Where(r => r.Id == rid).ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, RatingStatus.Hidden), ct);
                if (app is not null) await lifecycle.RecomputeCountersAsync(app.Id, ct);
                break;
            case "ban_uploader" when app is not null:
                await db.Users.Where(u => u.Id == app.UploaderUserId).ExecuteUpdateAsync(s => s.SetProperty(u => u.IsBanned, true).SetProperty(u => u.BanReason, "Report #" + id + ": " + (request.Resolution ?? "policy violation")), ct);
                await db.Apps.Where(a => a.UploaderUserId == app.UploaderUserId && a.Status == AppStatus.Published).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, AppStatus.Unlisted), ct);
                break;
        }
        if (report.ReporterUserId is { } reporter && request.Status is ReportStatus.Resolved or ReportStatus.Dismissed)
            notifications.Notify(reporter, NotificationType.ReportResolved, $"Your report on “{app?.Name ?? "an app"}” was {request.Status.ToString().ToLowerInvariant()}", request.Resolution, app is null ? null : $"/app/{app.Slug}");
        audit.Log("report.resolve", "report", id, new { request.Status, request.Action, request.Resolution });
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        return await GetAsync(id, ct);
    }
}

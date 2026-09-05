using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Admin;

public sealed class AdminRequestsService(AadiDbContext db, AuditService audit)
{
    public async Task<PagedResult<AdminRequestDto>> ListAsync(AdminListQuery query, CancellationToken ct)
    {
        var q = db.AppRequests.AsNoTracking().AsQueryable();
        q = query.Status?.ToLowerInvariant() switch
        {
            "open" => q.Where(r => r.Status == AppRequestStatus.Open),
            "fulfilled" => q.Where(r => r.Status == AppRequestStatus.Fulfilled),
            "closed" => q.Where(r => r.Status == AppRequestStatus.Closed),
            _ => q
        };
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(r => EF.Functions.ILike(r.Title, $"%{query.Q}%") || EF.Functions.ILike(r.Description, $"%{query.Q}%"));
        q = query.Sort == "votes" ? q.OrderByDescending(r => r.VoteCount) : q.OrderByDescending(r => r.CreatedAt);
        return await q.Select(r => new AdminRequestDto
        {
            Id = r.Id, Title = r.Title, Description = r.Description, RequesterUsername = r.Requester == null ? null : r.Requester.Username, Source = r.Source, Status = r.Status,
            FulfilledByAppName = r.FulfilledByApp == null ? null : r.FulfilledByApp.Name, FulfilledByAppSlug = r.FulfilledByApp == null ? null : r.FulfilledByApp.Slug, VoteCount = r.VoteCount, CreatedAt = r.CreatedAt
        }).ToPagedAsync(query.SafePage, query.SafePageSize, ct);
    }

    public async Task SetStatusAsync(int id, AppRequestStatus status, int? fulfilledByAppId, CancellationToken ct)
    {
        var req = await db.AppRequests.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw ApiException.NotFound("Request not found.");
        req.Status = status;
        req.FulfilledByAppId = status == AppRequestStatus.Fulfilled ? fulfilledByAppId : null;
        req.UpdatedAt = Clock.Now;
        audit.Log("request.status", "request", id, new { status, fulfilledByAppId });
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        await db.AppRequests.Where(r => r.Id == id).ExecuteDeleteAsync(ct);
        audit.Log("request.delete", "request", id);
        await db.SaveChangesAsync(ct);
    }
}

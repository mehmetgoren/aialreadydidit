using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Contracts.Requests;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using AiAlreadyDidIt.Api.Services.Apps;
using AiAlreadyDidIt.Api.Services.Catalog;
using AiAlreadyDidIt.Api.Services.Search;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Requests;

/// <summary>The "wanted" board: things people and agents searched for and did not find.</summary>
public sealed class AppRequestService(AadiDbContext db, ICurrentUser currentUser, SearchService search, JobQueue jobs, NotificationService notifications)
{
    public async Task<PagedResult<AppRequestDto>> ListAsync(string? status, string? q, string? sort, int page, int pageSize, CancellationToken ct)
    {
        var query = db.AppRequests.AsNoTracking().AsQueryable();
        query = status?.ToLowerInvariant() switch
        {
            "fulfilled" => query.Where(r => r.Status == AppRequestStatus.Fulfilled),
            "all" => query,
            _ => query.Where(r => r.Status == AppRequestStatus.Open)
        };
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(r => EF.Functions.ILike(r.Title, $"%{q.Trim()}%") || EF.Functions.ILike(r.Description, $"%{q.Trim()}%"));
        query = sort == "newest" ? query.OrderByDescending(r => r.CreatedAt) : query.OrderByDescending(r => r.VoteCount).ThenByDescending(r => r.CreatedAt);
        var uid = currentUser.IdOrNull;
        var paged = await query.Select(r => new AppRequestDto
        {
            Id = r.Id, Title = r.Title, Description = r.Description, RequesterUsername = r.Requester == null ? null : r.Requester.Username, Source = r.Source, Status = r.Status,
            VoteCount = r.VoteCount, CreatedAt = r.CreatedAt, MyVote = uid != null && db.AppRequestVotes.Any(v => v.RequestId == r.Id && v.UserId == uid),
            FulfilledBy = r.FulfilledByApp == null ? null : new AppCardDto { Id = r.FulfilledByApp.Id, Slug = r.FulfilledByApp.Slug, Name = r.FulfilledByApp.Name, ShortDescription = r.FulfilledByApp.ShortDescription, IconUrl = r.FulfilledByApp.IconStorageKey == null ? null : "/files/icons/" + r.FulfilledByApp.IconStorageKey }
        }).ToPagedAsync(page, pageSize, ct);
        return paged;
    }

    public async Task<AppRequestDto> GetAsync(int id, CancellationToken ct)
    {
        var dto = (await ListOne(id, ct)) ?? throw ApiException.NotFound("Request not found.");
        dto.Suggestions = await search.NearestAsync(dto.Title + "\n" + dto.Description, 5, null, 0.4, ct);
        return dto;
    }

    private async Task<AppRequestDto?> ListOne(int id, CancellationToken ct)
    {
        var uid = currentUser.IdOrNull;
        return await db.AppRequests.AsNoTracking().Where(r => r.Id == id).Select(r => new AppRequestDto
        {
            Id = r.Id, Title = r.Title, Description = r.Description, RequesterUsername = r.Requester == null ? null : r.Requester.Username, Source = r.Source, Status = r.Status,
            VoteCount = r.VoteCount, CreatedAt = r.CreatedAt, MyVote = uid != null && db.AppRequestVotes.Any(v => v.RequestId == r.Id && v.UserId == uid),
            FulfilledBy = r.FulfilledByApp == null ? null : new AppCardDto { Id = r.FulfilledByApp.Id, Slug = r.FulfilledByApp.Slug, Name = r.FulfilledByApp.Name, ShortDescription = r.FulfilledByApp.ShortDescription, IconUrl = r.FulfilledByApp.IconStorageKey == null ? null : "/files/icons/" + r.FulfilledByApp.IconStorageKey }
        }).FirstOrDefaultAsync(ct);
    }

    public async Task<AppRequestDto> CreateAsync(CreateAppRequestRequest request, RequestSource source, CancellationToken ct)
    {
        var uid = currentUser.IdOrNull;
        if (uid is null && !currentUser.IsApiKey) throw ApiException.Unauthorized("Sign in (or use an API key) to post a request.");
        var recent = Clock.Now.AddHours(-1);
        if (await db.AppRequests.CountAsync(r => r.CreatedAt >= recent && (r.RequesterUserId == uid), ct) >= 5) throw ApiException.Unprocessable("You posted several requests recently. Try again later.");
        var entity = new AppRequest
        {
            Title = request.Title.Trim(), Description = request.Description.Trim(), RequesterUserId = uid, ApiKeyId = currentUser.ApiKeyId, Source = source,
            Status = AppRequestStatus.Open, CreatedAt = Clock.Now, UpdatedAt = Clock.Now
        };
        db.AppRequests.Add(entity);
        await db.SaveChangesAsync(ct);
        jobs.Enqueue(JobTypes.EmbedRequest, new { RequestId = entity.Id }, $"request:{entity.Id}");
        await db.SaveChangesAsync(ct);
        return await GetAsync(entity.Id, ct);
    }

    public async Task<AppRequestDto> VoteAsync(int id, CancellationToken ct)
    {
        var uid = currentUser.Id;
        var req = await db.AppRequests.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw ApiException.NotFound("Request not found.");
        var vote = await db.AppRequestVotes.FirstOrDefaultAsync(v => v.RequestId == id && v.UserId == uid, ct);
        if (vote is null) db.AppRequestVotes.Add(new AppRequestVote { RequestId = id, UserId = uid, CreatedAt = Clock.Now });
        else db.AppRequestVotes.Remove(vote);
        await db.SaveChangesAsync(ct);
        req.VoteCount = await db.AppRequestVotes.CountAsync(v => v.RequestId == id, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    /// <summary>The uploader of a published app (or an admin) marks a request as fulfilled by that app.</summary>
    public async Task<AppRequestDto> FulfilAsync(int id, FulfilRequestRequest request, CancellationToken ct)
    {
        var req = await db.AppRequests.Include(r => r.Requester).FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw ApiException.NotFound("Request not found.");
        var app = await db.Apps.FirstOrDefaultAsync(a => a.Id == request.AppId && a.Status == AppStatus.Published, ct) ?? throw ApiException.NotFound("App not found.");
        if (app.UploaderUserId != currentUser.Id && !currentUser.IsAdmin) throw ApiException.Forbidden("Only the uploader of the app (or an admin) can mark this.");
        req.Status = AppRequestStatus.Fulfilled;
        req.FulfilledByAppId = app.Id;
        req.UpdatedAt = Clock.Now;
        if (req.RequesterUserId is { } requester)
            notifications.Notify(requester, NotificationType.RequestFulfilled, $"Your request “{req.Title}” has been fulfilled", $"{app.Name} was published — have a look.", $"/app/{app.Slug}", email: true, emailAddress: req.Requester?.Email);
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task CloseAsync(int id, CancellationToken ct)
    {
        var req = await db.AppRequests.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw ApiException.NotFound("Request not found.");
        if (req.RequesterUserId != currentUser.Id && !currentUser.IsAdmin) throw ApiException.Forbidden("Only the requester can close this.");
        req.Status = AppRequestStatus.Closed;
        req.UpdatedAt = Clock.Now;
        await db.SaveChangesAsync(ct);
    }
}

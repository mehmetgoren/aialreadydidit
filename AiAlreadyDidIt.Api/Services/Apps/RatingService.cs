using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Apps;

/// <summary>Ratings out of 100 with review + "worked on version X". Only members who downloaded the app can rate it.</summary>
public sealed class RatingService(AadiDbContext db, ICurrentUser currentUser, AppLifecycleService lifecycle, NotificationService notifications)
{
    public async Task<PagedResult<RatingDto>> ListAsync(string slug, string? sort, int page, int pageSize, CancellationToken ct)
    {
        var appId = await db.Apps.AsNoTracking().Where(a => a.Slug == slug).Select(a => (int?)a.Id).FirstOrDefaultAsync(ct) ?? throw ApiException.NotFound("App not found.");
        var uid = currentUser.IdOrNull;
        var query = db.Ratings.AsNoTracking().Where(r => r.AppId == appId && (r.Status == RatingStatus.Visible || r.UserId == uid));
        query = (sort ?? "helpful") switch
        {
            "newest" => query.OrderByDescending(r => r.CreatedAt),
            "highest" => query.OrderByDescending(r => r.Score).ThenByDescending(r => r.HelpfulCount),
            "lowest" => query.OrderBy(r => r.Score).ThenByDescending(r => r.HelpfulCount),
            _ => query.OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt)
        };
        var paged = await query.Select(Projection(uid)).ToPagedAsync(page, pageSize, ct);
        await FillVotesAsync(paged.Items, uid, ct);
        return paged;
    }

    public async Task<RatingSummaryDto> SummaryAsync(string slug, CancellationToken ct)
    {
        var appId = await db.Apps.AsNoTracking().Where(a => a.Slug == slug).Select(a => (int?)a.Id).FirstOrDefaultAsync(ct) ?? throw ApiException.NotFound("App not found.");
        var scores = await db.Ratings.AsNoTracking().Where(r => r.AppId == appId && r.Status == RatingStatus.Visible).Select(r => new { r.Score, r.Worked }).ToListAsync(ct);
        var s = new RatingSummaryDto { Count = scores.Count, Average = scores.Count == 0 ? 0 : Math.Round(scores.Average(x => x.Score), 1), WorkedCount = scores.Count(x => x.Worked == true), NotWorkedCount = scores.Count(x => x.Worked == false) };
        foreach (var x in scores) s.Histogram[Math.Min(4, x.Score / 20)]++;
        return s;
    }

    public async Task<RatingDto> UpsertAsync(string slug, CreateRatingRequest request, CancellationToken ct)
    {
        var app = await db.Apps.FirstOrDefaultAsync(a => a.Slug == slug && a.Status == AppStatus.Published, ct) ?? throw ApiException.NotFound("App not found.");
        var uid = currentUser.Id;
        if (currentUser.IsApiKey) throw ApiException.Forbidden("Ratings must be submitted by a signed-in member, not an API key.");
        if (app.UploaderUserId == uid) throw ApiException.Unprocessable("You cannot rate your own app.");
        if (!await db.Downloads.AnyAsync(d => d.AppId == app.Id && d.UserId == uid, ct)) throw ApiException.Forbidden("Download the app first — only members who downloaded it can rate it.");
        if (request.VersionId is { } vid && !await db.AppVersions.AnyAsync(v => v.Id == vid && v.AppId == app.Id, ct)) throw ApiException.Unprocessable("Version not found.", "versionId");

        var rating = await db.Ratings.FirstOrDefaultAsync(r => r.AppId == app.Id && r.UserId == uid, ct);
        var isNew = rating is null;
        rating ??= new Rating { AppId = app.Id, UserId = uid, CreatedAt = Clock.Now };
        rating.Score = Math.Clamp(request.Score, 0, 100);
        rating.Review = string.IsNullOrWhiteSpace(request.Review) ? null : TextUtil.Truncate(request.Review.Trim(), 2000);
        rating.VersionId = request.VersionId ?? app.LatestVersionId;
        rating.Worked = request.Worked;
        rating.Status = RatingStatus.Visible;
        rating.UpdatedAt = Clock.Now;
        if (isNew) db.Ratings.Add(rating);
        await db.SaveChangesAsync(ct);
        await lifecycle.RecomputeCountersAsync(app.Id, ct);
        if (isNew)
        {
            var uploader = await db.Users.AsNoTracking().FirstAsync(u => u.Id == app.UploaderUserId, ct);
            notifications.Notify(app.UploaderUserId, NotificationType.NewReview, $"New review on “{app.Name}”: {rating.Score}/100", rating.Review, $"/app/{app.Slug}#reviews", email: false, emailAddress: uploader.Email);
            await db.SaveChangesAsync(ct);
        }
        return await GetAsync(rating.Id, ct);
    }

    public async Task DeleteMineAsync(string slug, CancellationToken ct)
    {
        var rating = await db.Ratings.Include(r => r.App).FirstOrDefaultAsync(r => r.App.Slug == slug && r.UserId == currentUser.Id, ct) ?? throw ApiException.NotFound("Rating not found.");
        db.Ratings.Remove(rating);
        await db.SaveChangesAsync(ct);
        await lifecycle.RecomputeCountersAsync(rating.AppId, ct);
    }

    public async Task<RatingDto> VoteAsync(int ratingId, VoteRequest request, CancellationToken ct)
    {
        var rating = await db.Ratings.FirstOrDefaultAsync(r => r.Id == ratingId, ct) ?? throw ApiException.NotFound("Rating not found.");
        if (rating.UserId == currentUser.Id) throw ApiException.Unprocessable("You cannot vote on your own review.");
        var vote = await db.RatingVotes.FirstOrDefaultAsync(v => v.RatingId == ratingId && v.UserId == currentUser.Id, ct);
        if (request.Helpful is null) { if (vote is not null) db.RatingVotes.Remove(vote); }
        else if (vote is null) db.RatingVotes.Add(new RatingVote { RatingId = ratingId, UserId = currentUser.Id, IsHelpful = request.Helpful.Value, CreatedAt = Clock.Now });
        else vote.IsHelpful = request.Helpful.Value;
        await db.SaveChangesAsync(ct);
        rating.HelpfulCount = await db.RatingVotes.CountAsync(v => v.RatingId == ratingId && v.IsHelpful, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(ratingId, ct);
    }

    public async Task<RatingDto> ReplyAsync(int ratingId, ReplyRequest request, CancellationToken ct)
    {
        var rating = await db.Ratings.Include(r => r.App).Include(r => r.User).FirstOrDefaultAsync(r => r.Id == ratingId, ct) ?? throw ApiException.NotFound("Rating not found.");
        var isUploader = rating.App.UploaderUserId == currentUser.Id;
        if (!isUploader && !currentUser.IsAdmin) throw ApiException.Forbidden("Only the uploader can reply to reviews.");
        db.RatingReplies.Add(new RatingReply { RatingId = ratingId, UserId = currentUser.Id, Body = request.Body.Trim(), CreatedAt = Clock.Now });
        notifications.Notify(rating.UserId, NotificationType.ReviewReply, $"The uploader replied to your review of “{rating.App.Name}”", TextUtil.Truncate(request.Body, 300), $"/app/{rating.App.Slug}#reviews", email: true, emailAddress: rating.User.Email);
        await db.SaveChangesAsync(ct);
        return await GetAsync(ratingId, ct);
    }

    public async Task<RatingDto> GetAsync(int ratingId, CancellationToken ct)
    {
        var uid = currentUser.IdOrNull;
        var dto = await db.Ratings.AsNoTracking().Where(r => r.Id == ratingId).Select(Projection(uid)).FirstOrDefaultAsync(ct) ?? throw ApiException.NotFound("Rating not found.");
        await FillVotesAsync([dto], uid, ct);
        return dto;
    }

    private async Task FillVotesAsync(List<RatingDto> items, int? uid, CancellationToken ct)
    {
        if (uid is null || items.Count == 0) return;
        var ids = items.Select(i => i.Id).ToList();
        var votes = await db.RatingVotes.AsNoTracking().Where(v => v.UserId == uid && ids.Contains(v.RatingId)).ToDictionaryAsync(v => v.RatingId, v => v.IsHelpful, ct);
        foreach (var item in items) if (votes.TryGetValue(item.Id, out var helpful)) item.MyVote = helpful;
    }

    public async Task<List<RatingDto>> MineAsync(CancellationToken ct)
    {
        var uid = currentUser.Id;
        return await db.Ratings.AsNoTracking().Where(r => r.UserId == uid).OrderByDescending(r => r.UpdatedAt).Select(Projection(uid)).ToListAsync(ct);
    }

    private static Expression<Func<Rating, RatingDto>> Projection(int? uid) => r => new RatingDto
    {
        Id = r.Id, AppId = r.AppId, AppSlug = r.App.Slug, AppName = r.App.Name, Username = r.User.Username, DisplayName = r.User.DisplayName, AvatarUrl = r.User.AvatarUrl,
        Score = r.Score, Review = r.Review, Version = r.Version == null ? null : r.Version.Version, Worked = r.Worked, HelpfulCount = r.HelpfulCount,
        IsMine = r.UserId == uid, Status = r.Status, CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt,
        Replies = r.Replies.OrderBy(x => x.CreatedAt).Select(x => new RatingReplyDto { Id = x.Id, Username = x.User.Username, DisplayName = x.User.DisplayName, IsUploader = x.UserId == r.App.UploaderUserId, Body = x.Body, CreatedAt = x.CreatedAt }).ToList()
    };
}

using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Jobs;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Services.Apps;

/// <summary>Creates in-app notifications and (optionally) queues the matching e-mail. Caller saves the context.</summary>
public sealed class NotificationService(AadiDbContext db, JobQueue jobs, IOptions<SiteOptions> site)
{
    public Notification Notify(int userId, NotificationType type, string title, string? body = null, string? link = null, bool email = false, string? emailAddress = null)
    {
        var n = new Notification { UserId = userId, Type = type, Title = TextUtil.Truncate(title, 200), Body = TextUtil.Truncate(body, 2000), Link = link, CreatedAt = Clock.Now };
        db.Notifications.Add(n);
        if (email && !string.IsNullOrWhiteSpace(emailAddress))
        {
            var url = link is null ? site.Value.PublicUrl : (link.StartsWith("http") ? link : site.Value.PublicUrl + link);
            jobs.Enqueue(JobTypes.SendEmail, new { To = emailAddress, Subject = $"{title} — {site.Value.Name}", Html = $"<p>{System.Net.WebUtility.HtmlEncode(body ?? title)}</p><p><a href=\"{url}\">{url}</a></p>", Text = $"{body ?? title}\n{url}" }, subject: $"user:{userId}");
        }
        return n;
    }
}

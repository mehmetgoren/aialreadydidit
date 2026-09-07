using System.Text.Json;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;

namespace AiAlreadyDidIt.Api.Infrastructure;

/// <summary>Writes admin-panel audit rows (caller saves the context).</summary>
public sealed class AuditService(AadiDbContext db, ICurrentUser currentUser)
{
    public void Log(string action, string? entity = null, object? entityId = null, object? details = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = currentUser.IdOrNull,
            Username = currentUser.Username,
            Action = action,
            Entity = entity,
            EntityId = entityId?.ToString(),
            Details = details is null ? null : (details as string ?? JsonSerializer.Serialize(details, AadiJson.Options)),
            IpAddress = currentUser.IpAddress,
            CreatedAt = Clock.Now
        });
    }
}

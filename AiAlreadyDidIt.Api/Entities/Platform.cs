namespace AiAlreadyDidIt.Api.Entities;

public enum ModerationActionKind
{
    Approve = 1, Reject = 2, Unlist = 3, Restore = 4, Rescan = 5, OverrideLicense = 6, Feature = 7, Unfeature = 8,
    RequestChanges = 9, Remove = 10, ApproveVersion = 11, RejectVersion = 12, Note = 13
}

/// <summary>Immutable record of a moderation decision on an app or a version.</summary>
public class ModerationAction
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public int? VersionId { get; set; }
    public int AdminUserId { get; set; }
    public ModerationActionKind Action { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Raw antivirus verdict per file (history kept even after rescans).</summary>
public class ScanResult
{
    public long Id { get; set; }
    public int FileId { get; set; }
    public string Engine { get; set; } = "clamav";
    public ScanStatus Verdict { get; set; }
    public string? Signature { get; set; }
    public string? Raw { get; set; }
    public DateTime ScannedAt { get; set; }
}

/// <summary>Key/value platform configuration editable from the admin panel.</summary>
public class SiteSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    /// <summary>general | uploads | search | savings | limits | seo | content</summary>
    public string Group { get; set; } = "general";
    /// <summary>string | int | decimal | bool | json | text</summary>
    public string ValueType { get; set; } = "string";
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
}

/// <summary>Immutable record of an admin-panel action (who did what to which entity).</summary>
public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Home page banner / hero slide.</summary>
public class Banner
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ImageStorageKey { get; set; }
    public string? Link { get; set; }
    /// <summary>hero | sidebar</summary>
    public string Position { get; set; } = "hero";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum JobStatus { Queued = 0, Running = 1, Done = 2, Failed = 3, Cancelled = 4 }

/// <summary>Durable queue for scan / import / embed / notify / email work processed by the in-process runner.</summary>
public class BackgroundJob
{
    public long Id { get; set; }
    /// <summary>scan_version | analyze_source | import_repository | embed_app | notify_watchers | send_email | recompute_stats | embed_request | categorize_app</summary>
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public JobStatus Status { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public string? LastError { get; set; }
    public DateTime RunAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Optional grouping key (e.g. "app:12") for the admin jobs page.</summary>
    public string? Subject { get; set; }
}

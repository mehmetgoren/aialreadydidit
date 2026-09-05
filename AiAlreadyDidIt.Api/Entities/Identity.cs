namespace AiAlreadyDidIt.Api.Entities;

/// <summary>A member. Registration is optional for browsing/downloading; uploading and rating need an account.</summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime? EmailVerifiedAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Null for accounts created through Google only.</summary>
    public string? PasswordHash { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    /// <summary>en-US | tr-TR</summary>
    public string Locale { get; set; } = "en-US";
    /// <summary>0 = new, 1 = trusted (new versions publish without review), 2 = verified publisher.</summary>
    public int TrustLevel { get; set; }
    public DateTime? LastLoginAt { get; set; }
    /// <summary>Internal admin note (never shown to the member).</summary>
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<App> Apps { get; set; } = [];
    public ICollection<Rating> Ratings { get; set; } = [];
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<Collection> Collections { get; set; } = [];
}

/// <summary>Application role (admin panel). Every user has exactly one role.</summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public ICollection<User> Users { get; set; } = [];
    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
    public ICollection<RoleAction> RoleActions { get; set; } = [];
}

/// <summary>Admin panel navigation item (hierarchical).</summary>
public class Menu
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Description { get; set; }
    public int? OrderNum { get; set; }
    public int? ParentId { get; set; }
    public bool Visible { get; set; } = true;
    public string? Icon { get; set; }
    public Menu? Parent { get; set; }
    public ICollection<Menu> Children { get; set; } = [];
    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
}

/// <summary>Role → menu access.</summary>
public class RoleMenu
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int MenuId { get; set; }
    public bool HasAccess { get; set; }
    public Role Role { get; set; } = null!;
    public Menu Menu { get; set; } = null!;
}

/// <summary>Role → API action (controller/action) permission.</summary>
public class RoleAction
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Role Role { get; set; } = null!;
}

/// <summary>Google (or another OAuth provider) identity linked to a member.</summary>
public class ExternalLogin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    /// <summary>google</summary>
    public string Provider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Rotating refresh token; one row per browser session.</summary>
public class RefreshToken
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}

public enum EmailTokenKind { VerifyEmail = 1, ResetPassword = 2 }

public class EmailToken
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public EmailTokenKind Kind { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Agent API key ("aad_" prefix). Only the hash is stored.</summary>
public class ApiKey
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    /// <summary>First 12 characters shown in the UI (aad_xxxxxxxx).</summary>
    public string Prefix { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    /// <summary>Comma separated: read, download, submit</summary>
    public string Scopes { get; set; } = "read,download";
    /// <summary>default | elevated | unlimited (rate limit tier)</summary>
    public string RateTier { get; set; } = "default";
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public long RequestCount { get; set; }
    public long DownloadCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive => RevokedAt is null && (ExpiresAt is null || ExpiresAt > DateTime.UtcNow);
}

public class ApiKeyUsageDaily
{
    public long Id { get; set; }
    public int ApiKeyId { get; set; }
    public DateOnly Date { get; set; }
    public int RequestCount { get; set; }
    public int SearchCount { get; set; }
    public int DownloadCount { get; set; }
}

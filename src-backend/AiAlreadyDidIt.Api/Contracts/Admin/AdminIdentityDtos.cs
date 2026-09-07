using System.ComponentModel.DataAnnotations;
using AiAlreadyDidIt.Api.Contracts.Dashboard;

namespace AiAlreadyDidIt.Api.Contracts.Admin;

public class AdminUserRowDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Role { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int TrustLevel { get; set; }
    public bool IsActive { get; set; }
    public bool IsBanned { get; set; }
    public string? AvatarUrl { get; set; }
    public int AppCount { get; set; }
    public int RatingCount { get; set; }
    public int DownloadCount { get; set; }
    public int ApiKeyCount { get; set; }
    public bool HasGoogle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminUserDetailDto : AdminUserRowDto
{
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public string? BanReason { get; set; }
    public string? AdminNote { get; set; }
    public string Locale { get; set; } = "en-US";
    public List<AdminAppRowDto> Apps { get; set; } = [];
    public List<ApiKeyDto> ApiKeys { get; set; } = [];
    public List<AdminSessionDto> Sessions { get; set; } = [];
    public int ReportCountAgainst { get; set; }
    public int ReportCountFiled { get; set; }
}

public class AdminUpdateUserRequest
{
    [MaxLength(80)] public string? DisplayName { get; set; }
    public int? RoleId { get; set; }
    public int? TrustLevel { get; set; }
    public bool? IsActive { get; set; }
    [MaxLength(2000)] public string? AdminNote { get; set; }
    public bool? EmailVerified { get; set; }
}

public class BanRequest
{
    public bool Banned { get; set; }
    [MaxLength(512)] public string? Reason { get; set; }
    public bool UnlistApps { get; set; } = true;
}

public class AdminSessionDto
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public bool Revoked { get; set; }
}

public class AdminApiKeyRowDto : ApiKeyDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class SetRateTierRequest { [Required] public string RateTier { get; set; } = "default"; }

public class RoleDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; public bool IsAdmin { get; set; } public int UserCount { get; set; } }
public class SaveRoleRequest { [Required, MaxLength(64)] public string Name { get; set; } = string.Empty; public bool IsAdmin { get; set; } }

public class MenuDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Route { get; set; }
    public string? Description { get; set; }
    public int? OrderNum { get; set; }
    public int? ParentId { get; set; }
    public bool Visible { get; set; }
    public string? Icon { get; set; }
    public List<MenuDto> Children { get; set; } = [];
}

public class SaveMenuRequest
{
    [Required, MaxLength(128)] public string Name { get; set; } = string.Empty;
    [MaxLength(256)] public string? Route { get; set; }
    [MaxLength(512)] public string? Description { get; set; }
    public int? OrderNum { get; set; }
    public int? ParentId { get; set; }
    public bool Visible { get; set; } = true;
    [MaxLength(64)] public string? Icon { get; set; }
}

public class RoleMenuDto { public int MenuId { get; set; } public string MenuName { get; set; } = string.Empty; public string? Route { get; set; } public int? ParentId { get; set; } public bool HasAccess { get; set; } }
public class SaveRoleMenusRequest { public List<int> MenuIds { get; set; } = []; }
public class RoleActionDto { public int Id { get; set; } public string Controller { get; set; } = string.Empty; public string Action { get; set; } = string.Empty; }
public class SaveRoleActionsRequest { public List<RoleActionDto> Actions { get; set; } = []; }
public class ControllerActionsDto { public string Controller { get; set; } = string.Empty; public List<string> Actions { get; set; } = []; }

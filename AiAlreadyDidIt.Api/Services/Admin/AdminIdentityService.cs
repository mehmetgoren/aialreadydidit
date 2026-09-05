using System.Reflection;
using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Contracts.Dashboard;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Auth;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Admin;

/// <summary>Admin › Members: users, API keys, sessions, roles, menus, role-menu and role-action delegation.</summary>
public sealed class AdminIdentityService(AadiDbContext db, ICurrentUser currentUser, AuditService audit, CatalogService catalog)
{
    // ---------------------------------------------------------------- users

    public async Task<PagedResult<AdminUserRowDto>> UsersAsync(AdminListQuery query, string? role, CancellationToken ct)
    {
        var q = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(u => EF.Functions.ILike(u.Username, $"%{query.Q}%") || EF.Functions.ILike(u.Email, $"%{query.Q}%") || EF.Functions.ILike(u.DisplayName, $"%{query.Q}%"));
        if (!string.IsNullOrWhiteSpace(role)) q = q.Where(u => u.Role.Name == role);
        q = query.Status?.ToLowerInvariant() switch { "banned" => q.Where(u => u.IsBanned), "inactive" => q.Where(u => !u.IsActive), "unverified" => q.Where(u => u.EmailVerifiedAt == null), _ => q };
        q = (query.Sort, query.Descending) switch
        {
            ("username", false) => q.OrderBy(u => u.Username), ("username", true) => q.OrderByDescending(u => u.Username),
            ("lastLogin", false) => q.OrderBy(u => u.LastLoginAt), ("lastLogin", true) => q.OrderByDescending(u => u.LastLoginAt),
            (_, false) => q.OrderBy(u => u.CreatedAt), _ => q.OrderByDescending(u => u.CreatedAt)
        };
        return await q.Select(u => new AdminUserRowDto
        {
            Id = u.Id, Username = u.Username, DisplayName = u.DisplayName, Email = u.Email, EmailVerified = u.EmailVerifiedAt != null, Role = u.Role.Name, RoleId = u.RoleId, TrustLevel = u.TrustLevel,
            IsActive = u.IsActive, IsBanned = u.IsBanned, AvatarUrl = u.AvatarUrl, AppCount = u.Apps.Count(a => a.Status != AppStatus.Removed), RatingCount = u.Ratings.Count,
            DownloadCount = db.Downloads.Count(d => d.UserId == u.Id), ApiKeyCount = u.ApiKeys.Count(k => k.RevokedAt == null), HasGoogle = u.ExternalLogins.Any(e => e.Provider == "google"),
            CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt
        }).ToPagedAsync(query.SafePage, query.SafePageSize, ct);
    }

    public async Task<AdminUserDetailDto> UserAsync(int id, CancellationToken ct)
    {
        var u = await db.Users.AsNoTracking().Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("Member not found.");
        var apps = await db.Apps.AsNoTracking().Where(a => a.UploaderUserId == id).OrderByDescending(a => a.UpdatedAt).Select(a => new AdminAppRowDto
        {
            Id = a.Id, Slug = a.Slug, Name = a.Name, ShortDescription = a.ShortDescription, Status = a.Status, DownloadCount = a.DownloadCount, RatingAvg = a.RatingAvg, RatingCount = a.RatingCount,
            CategoryNameEn = a.Category.NameEn, LicenseSpdxId = a.License.SpdxId, CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt, PublishedAt = a.PublishedAt, IconUrl = a.IconStorageKey == null ? null : "/files/icons/" + a.IconStorageKey
        }).ToListAsync(ct);
        return new AdminUserDetailDto
        {
            Id = u.Id, Username = u.Username, DisplayName = u.DisplayName, Email = u.Email, EmailVerified = u.EmailVerifiedAt != null, Role = u.Role.Name, RoleId = u.RoleId, TrustLevel = u.TrustLevel,
            IsActive = u.IsActive, IsBanned = u.IsBanned, AvatarUrl = u.AvatarUrl, Bio = u.Bio, Website = u.Website, BanReason = u.BanReason, AdminNote = u.AdminNote, Locale = u.Locale,
            AppCount = apps.Count, RatingCount = await db.Ratings.CountAsync(r => r.UserId == id, ct), DownloadCount = await db.Downloads.CountAsync(d => d.UserId == id, ct),
            ApiKeyCount = await db.ApiKeys.CountAsync(k => k.UserId == id && k.RevokedAt == null, ct), HasGoogle = await db.ExternalLogins.AnyAsync(e => e.UserId == id && e.Provider == "google", ct),
            CreatedAt = u.CreatedAt, LastLoginAt = u.LastLoginAt, Apps = apps,
            ApiKeys = await db.ApiKeys.AsNoTracking().Where(k => k.UserId == id).OrderByDescending(k => k.CreatedAt).Select(k => new ApiKeyDto { Id = k.Id, Name = k.Name, Prefix = k.Prefix, Scopes = k.Scopes, RateTier = k.RateTier, LastUsedAt = k.LastUsedAt, ExpiresAt = k.ExpiresAt, RevokedAt = k.RevokedAt, RequestCount = k.RequestCount, DownloadCount = k.DownloadCount, CreatedAt = k.CreatedAt }).ToListAsync(ct),
            Sessions = await db.RefreshTokens.AsNoTracking().Where(r => r.UserId == id && r.RevokedAt == null && r.ExpiresAt > Clock.Now).OrderByDescending(r => r.CreatedAt).Take(20)
                .Select(r => new AdminSessionDto { Id = r.Id, UserId = r.UserId, Username = u.Username, CreatedAt = r.CreatedAt, LastUsedAt = r.LastUsedAt, ExpiresAt = r.ExpiresAt, Ip = r.CreatedIp, UserAgent = r.UserAgent }).ToListAsync(ct),
            ReportCountAgainst = await db.Reports.CountAsync(r => r.App!.UploaderUserId == id, ct), ReportCountFiled = await db.Reports.CountAsync(r => r.ReporterUserId == id, ct)
        };
    }

    public async Task<AdminUserDetailDto> UpdateUserAsync(int id, AdminUpdateUserRequest request, CancellationToken ct)
    {
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("Member not found.");
        if (request.DisplayName is not null) u.DisplayName = request.DisplayName.Trim();
        if (request.RoleId is { } rid)
        {
            if (id == currentUser.Id) throw ApiException.Unprocessable("You cannot change your own role.");
            if (!await db.Roles.AnyAsync(r => r.Id == rid, ct)) throw ApiException.Unprocessable("Role not found.", "roleId");
            u.RoleId = rid;
        }
        if (request.TrustLevel is { } t) u.TrustLevel = Math.Clamp(t, 0, 2);
        if (request.IsActive is { } active) { if (id == currentUser.Id && !active) throw ApiException.Unprocessable("You cannot deactivate yourself."); u.IsActive = active; }
        if (request.AdminNote is not null) u.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote) ? null : request.AdminNote.Trim();
        if (request.EmailVerified is { } verified) u.EmailVerifiedAt = verified ? (u.EmailVerifiedAt ?? Clock.Now) : null;
        u.UpdatedAt = Clock.Now;
        audit.Log("user.update", "user", id, request);
        await db.SaveChangesAsync(ct);
        return await UserAsync(id, ct);
    }

    public async Task<AdminUserDetailDto> BanAsync(int id, BanRequest request, CancellationToken ct)
    {
        if (id == currentUser.Id) throw ApiException.Unprocessable("You cannot ban yourself.");
        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.NotFound("Member not found.");
        u.IsBanned = request.Banned;
        u.BanReason = request.Banned ? request.Reason?.Trim() : null;
        u.UpdatedAt = Clock.Now;
        if (request.Banned)
        {
            await db.RefreshTokens.Where(r => r.UserId == id && r.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, Clock.Now), ct);
            await db.ApiKeys.Where(k => k.UserId == id && k.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(k => k.RevokedAt, Clock.Now), ct);
            if (request.UnlistApps) await db.Apps.Where(a => a.UploaderUserId == id && a.Status == AppStatus.Published).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, AppStatus.Unlisted), ct);
        }
        audit.Log(request.Banned ? "user.ban" : "user.unban", "user", id, request);
        await db.SaveChangesAsync(ct);
        catalog.InvalidateHome();
        return await UserAsync(id, ct);
    }

    // ---------------------------------------------------------------- API keys & sessions

    public async Task<PagedResult<AdminApiKeyRowDto>> ApiKeysAsync(AdminListQuery query, CancellationToken ct)
    {
        var q = db.ApiKeys.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(k => EF.Functions.ILike(k.Name, $"%{query.Q}%") || EF.Functions.ILike(k.User.Username, $"%{query.Q}%") || k.Prefix.StartsWith(query.Q));
        if (query.Status == "active") q = q.Where(k => k.RevokedAt == null);
        return await q.OrderByDescending(k => k.LastUsedAt ?? k.CreatedAt).Select(k => new AdminApiKeyRowDto
        {
            Id = k.Id, UserId = k.UserId, Username = k.User.Username, Email = k.User.Email, Name = k.Name, Prefix = k.Prefix, Scopes = k.Scopes, RateTier = k.RateTier, LastUsedAt = k.LastUsedAt, ExpiresAt = k.ExpiresAt,
            RevokedAt = k.RevokedAt, RequestCount = k.RequestCount, DownloadCount = k.DownloadCount, CreatedAt = k.CreatedAt
        }).ToPagedAsync(query.SafePage, query.SafePageSize, ct);
    }

    public async Task RevokeApiKeyAsync(int id, CancellationToken ct)
    {
        await db.ApiKeys.Where(k => k.Id == id).ExecuteUpdateAsync(s => s.SetProperty(k => k.RevokedAt, Clock.Now), ct);
        audit.Log("api_key.revoke", "api_key", id);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetRateTierAsync(int id, SetRateTierRequest request, CancellationToken ct)
    {
        var tier = request.RateTier.Trim().ToLowerInvariant();
        if (tier is not ("default" or "elevated" or "unlimited")) throw ApiException.Unprocessable("Tier must be default, elevated or unlimited.", "rateTier");
        await db.ApiKeys.Where(k => k.Id == id).ExecuteUpdateAsync(s => s.SetProperty(k => k.RateTier, tier), ct);
        audit.Log("api_key.tier", "api_key", id, request);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AdminSessionDto>> SessionsAsync(AdminListQuery query, CancellationToken ct)
    {
        var q = db.RefreshTokens.AsNoTracking().AsQueryable();
        if (query.Status != "all") q = q.Where(r => r.RevokedAt == null && r.ExpiresAt > Clock.Now);
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(r => EF.Functions.ILike(r.User.Username, $"%{query.Q}%") || r.CreatedIp == query.Q);
        return await q.OrderByDescending(r => r.LastUsedAt ?? r.CreatedAt).Select(r => new AdminSessionDto
        {
            Id = r.Id, UserId = r.UserId, Username = r.User.Username, CreatedAt = r.CreatedAt, LastUsedAt = r.LastUsedAt, ExpiresAt = r.ExpiresAt, Ip = r.CreatedIp, UserAgent = r.UserAgent, Revoked = r.RevokedAt != null
        }).ToPagedAsync(query.SafePage, query.SafePageSize, ct);
    }

    public async Task RevokeSessionAsync(long id, CancellationToken ct)
    {
        await db.RefreshTokens.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, Clock.Now), ct);
        audit.Log("session.revoke", "session", id);
        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeUserSessionsAsync(int userId, CancellationToken ct)
    {
        await db.RefreshTokens.Where(r => r.UserId == userId && r.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, Clock.Now), ct);
        audit.Log("session.revoke_all", "user", userId);
        await db.SaveChangesAsync(ct);
    }

    // ---------------------------------------------------------------- roles / menus / role-menus / role-actions

    public Task<List<RoleDto>> RolesAsync(CancellationToken ct) =>
        db.Roles.AsNoTracking().OrderBy(r => r.Id).Select(r => new RoleDto { Id = r.Id, Name = r.Name, IsAdmin = r.IsAdmin, UserCount = r.Users.Count }).ToListAsync(ct);

    public async Task<RoleDto> SaveRoleAsync(int? id, SaveRoleRequest request, CancellationToken ct)
    {
        var role = id is null ? new Role() : await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw ApiException.NotFound("Role not found.");
        if (await db.Roles.AnyAsync(r => r.Name == request.Name.Trim() && r.Id != (id ?? 0), ct)) throw ApiException.Unprocessable("Role name already exists.", "name");
        role.Name = request.Name.Trim(); role.IsAdmin = request.IsAdmin;
        if (id is null) db.Roles.Add(role);
        audit.Log(id is null ? "role.create" : "role.update", "role", id, request);
        await db.SaveChangesAsync(ct);
        return (await RolesAsync(ct)).First(r => r.Id == role.Id);
    }

    public async Task DeleteRoleAsync(int id, CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw ApiException.NotFound("Role not found.");
        if (role.Users.Count > 0) throw ApiException.Unprocessable("Members still have this role.");
        if (role.Name is "Admin" or "Member") throw ApiException.Unprocessable("Built-in roles cannot be deleted.");
        db.Roles.Remove(role);
        audit.Log("role.delete", "role", id);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<MenuDto>> MenusAsync(CancellationToken ct)
    {
        var rows = await db.Menus.AsNoTracking().OrderBy(m => m.OrderNum).ToListAsync(ct);
        var nodes = rows.ToDictionary(r => r.Id, r => new MenuDto { Id = r.Id, Name = r.Name, Route = r.Route, Description = r.Description, OrderNum = r.OrderNum, ParentId = r.ParentId, Visible = r.Visible, Icon = r.Icon });
        var roots = new List<MenuDto>();
        foreach (var r in rows) { if (r.ParentId is { } pid && nodes.TryGetValue(pid, out var p)) p.Children.Add(nodes[r.Id]); else roots.Add(nodes[r.Id]); }
        return roots;
    }

    public async Task<MenuDto> SaveMenuAsync(int? id, SaveMenuRequest request, CancellationToken ct)
    {
        var menu = id is null ? new Menu() : await db.Menus.FirstOrDefaultAsync(m => m.Id == id, ct) ?? throw ApiException.NotFound("Menu not found.");
        menu.Name = request.Name.Trim(); menu.Route = request.Route?.Trim(); menu.Description = request.Description?.Trim(); menu.OrderNum = request.OrderNum; menu.ParentId = request.ParentId == id ? null : request.ParentId; menu.Visible = request.Visible; menu.Icon = request.Icon?.Trim();
        if (id is null) db.Menus.Add(menu);
        audit.Log(id is null ? "menu.create" : "menu.update", "menu", id, request);
        await db.SaveChangesAsync(ct);
        return new MenuDto { Id = menu.Id, Name = menu.Name, Route = menu.Route, Description = menu.Description, OrderNum = menu.OrderNum, ParentId = menu.ParentId, Visible = menu.Visible, Icon = menu.Icon };
    }

    public async Task DeleteMenuAsync(int id, CancellationToken ct)
    {
        if (await db.Menus.AnyAsync(m => m.ParentId == id, ct)) throw ApiException.Unprocessable("Delete the sub-menus first.");
        await db.RoleMenus.Where(rm => rm.MenuId == id).ExecuteDeleteAsync(ct);
        await db.Menus.Where(m => m.Id == id).ExecuteDeleteAsync(ct);
        audit.Log("menu.delete", "menu", id);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<RoleMenuDto>> RoleMenusAsync(int roleId, CancellationToken ct)
    {
        var access = await db.RoleMenus.AsNoTracking().Where(rm => rm.RoleId == roleId && rm.HasAccess).Select(rm => rm.MenuId).ToHashSetAsync(ct);
        return await db.Menus.AsNoTracking().OrderBy(m => m.OrderNum).Select(m => new RoleMenuDto { MenuId = m.Id, MenuName = m.Name, Route = m.Route, ParentId = m.ParentId, HasAccess = access.Contains(m.Id) }).ToListAsync(ct);
    }

    public async Task SaveRoleMenusAsync(int roleId, SaveRoleMenusRequest request, CancellationToken ct)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct)) throw ApiException.NotFound("Role not found.");
        await db.RoleMenus.Where(rm => rm.RoleId == roleId).ExecuteDeleteAsync(ct);
        foreach (var menuId in request.MenuIds.Distinct()) db.RoleMenus.Add(new RoleMenu { RoleId = roleId, MenuId = menuId, HasAccess = true });
        audit.Log("role.menus", "role", roleId, request);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<RoleActionDto>> RoleActionsAsync(int roleId, CancellationToken ct) =>
        db.RoleActions.AsNoTracking().Where(a => a.RoleId == roleId).OrderBy(a => a.Controller).ThenBy(a => a.Action).Select(a => new RoleActionDto { Id = a.Id, Controller = a.Controller, Action = a.Action }).ToListAsync(ct);

    public async Task SaveRoleActionsAsync(int roleId, SaveRoleActionsRequest request, CancellationToken ct)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, ct)) throw ApiException.NotFound("Role not found.");
        await db.RoleActions.Where(a => a.RoleId == roleId).ExecuteDeleteAsync(ct);
        foreach (var a in request.Actions.DistinctBy(a => (a.Controller, a.Action))) db.RoleActions.Add(new RoleAction { RoleId = roleId, Controller = a.Controller.Trim(), Action = a.Action.Trim() });
        audit.Log("role.actions", "role", roleId, request);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Admin controllers and their actions discovered by reflection, for the role-action editor.</summary>
    public static List<ControllerActionsDto> ControllerActions() =>
        Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)) && t.Name.StartsWith("Admin", StringComparison.Ordinal))
            .Select(t => new ControllerActionsDto
            {
                Controller = t.Name.EndsWith("Controller") ? t.Name[..^"Controller".Length] : t.Name,
                Actions = ["*", .. t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName).Select(m => m.Name).Distinct().OrderBy(n => n)]
            }).OrderBy(c => c.Controller).ToList();
}

using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Data.Seed;

/// <summary>
/// Inserts the reference lists (categories, platforms, licenses, LLM models, roles, admin menus, settings) and the
/// administrator account when they are missing. Runs on every start, never overwrites admin edits.
/// </summary>
public sealed class ReferenceDataSeeder(AadiDbContext db, IPasswordHasher hasher, IConfiguration configuration,
    IOptions<SiteOptions> site, ILogger<ReferenceDataSeeder> logger)
{
    public const string AdminRoleName = "Admin";
    public const string ModeratorRoleName = "Moderator";
    public const string MemberRoleName = "Member";

    public async Task EnsureAsync(CancellationToken ct = default)
    {
        await EnsureRolesAsync(ct);
        await EnsureMenusAsync(ct);
        await EnsureCategoriesAsync(ct);
        await EnsurePlatformsAsync(ct);
        await EnsureLicensesAsync(ct);
        await EnsureLlmModelsAsync(ct);
        await EnsureSettingsAsync(ct);
        await EnsureAdminAsync(ct);
    }

    private async Task EnsureRolesAsync(CancellationToken ct)
    {
        var names = await db.Roles.Select(r => r.Name).ToListAsync(ct);
        if (!names.Contains(AdminRoleName)) db.Roles.Add(new Role { Name = AdminRoleName, IsAdmin = true });
        if (!names.Contains(ModeratorRoleName)) db.Roles.Add(new Role { Name = ModeratorRoleName, IsAdmin = false });
        if (!names.Contains(MemberRoleName)) db.Roles.Add(new Role { Name = MemberRoleName, IsAdmin = false });
        await db.SaveChangesAsync(ct);

        // Moderators get the moderation / reports / requests endpoints by default.
        var moderator = await db.Roles.FirstAsync(r => r.Name == ModeratorRoleName, ct);
        if (!await db.RoleActions.AnyAsync(a => a.RoleId == moderator.Id, ct))
        {
            string[] controllers = ["AdminModeration", "AdminReports", "AdminRequests", "AdminPanel", "AdminApps"];
            foreach (var c in controllers)
                db.RoleActions.Add(new RoleAction { RoleId = moderator.Id, Controller = c, Action = "*" });
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task EnsureMenusAsync(CancellationToken ct)
    {
        if (await db.Menus.AnyAsync(ct)) return;
        var order = 0;
        foreach (var def in ReferenceData.AdminMenus) AddMenu(def, null, ref order);
        await db.SaveChangesAsync(ct);

        var admin = await db.Roles.FirstAsync(r => r.Name == AdminRoleName, ct);
        var moderator = await db.Roles.FirstAsync(r => r.Name == ModeratorRoleName, ct);
        var menus = await db.Menus.ToListAsync(ct);
        foreach (var m in menus)
        {
            db.RoleMenus.Add(new RoleMenu { RoleId = admin.Id, MenuId = m.Id, HasAccess = true });
            var modAccess = m.Name is "admin_dashboard" or "adm_group_moderation" or "adm_moderation_queue" or "adm_reports" or "adm_requests";
            db.RoleMenus.Add(new RoleMenu { RoleId = moderator.Id, MenuId = m.Id, HasAccess = modAccess });
        }
        await db.SaveChangesAsync(ct);

        void AddMenu(ReferenceData.MenuDef def, Menu? parent, ref int order)
        {
            var menu = new Menu { Name = def.Name, Route = def.Route, Icon = def.Icon, OrderNum = ++order, Parent = parent, Visible = true };
            db.Menus.Add(menu);
            if (def.Children is null) return;
            foreach (var child in def.Children) AddMenu(child, menu, ref order);
        }
    }

    private async Task EnsureCategoriesAsync(CancellationToken ct)
    {
        var existing = await db.Categories.Select(c => c.Slug).ToHashSetAsync(ct);
        var sort = 0;
        foreach (var root in ReferenceData.Categories) Add(root, null, 1, ref sort, string.Empty);
        await db.SaveChangesAsync(ct);

        void Add(ReferenceData.CategoryDef def, Category? parent, int level, ref int sort, string slugPrefix)
        {
            var slug = level == 1 ? TextUtil.Slugify(def.En) : slugPrefix + "-" + TextUtil.Slugify(def.En);
            Category node;
            if (existing.Contains(slug))
            {
                node = db.Categories.Local.FirstOrDefault(c => c.Slug == slug) ?? db.Categories.First(c => c.Slug == slug);
            }
            else
            {
                node = new Category { Slug = slug, NameEn = def.En, NameTr = def.Tr, Icon = def.Icon, Level = level, Parent = parent, SortOrder = ++sort, IsActive = true };
                db.Categories.Add(node);
                existing.Add(slug);
            }
            if (def.Children is null) return;
            foreach (var child in def.Children) Add(child, node, level + 1, ref sort, slug);
        }
    }

    private async Task EnsurePlatformsAsync(CancellationToken ct)
    {
        var codes = await db.Platforms.Select(p => p.Code).ToHashSetAsync(ct);
        var sort = 0;
        foreach (var p in ReferenceData.Platforms)
        {
            sort++;
            if (codes.Contains(p.Code)) continue;
            db.Platforms.Add(new Platform
            {
                Code = p.Code, Name = p.Name, Icon = p.Icon, AllowedExtensions = p.Extensions,
                AllowsExternalReference = p.AllowsExternal, InstallHint = p.Hint, SortOrder = sort, IsActive = true
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureLicensesAsync(CancellationToken ct)
    {
        var ids = await db.Licenses.Select(l => l.SpdxId).ToHashSetAsync(ct);
        var sort = 0;
        foreach (var l in ReferenceData.Licenses)
        {
            sort++;
            if (ids.Contains(l.SpdxId)) continue;
            db.Licenses.Add(new License
            {
                SpdxId = l.SpdxId, Name = l.Name, Family = l.Family, Url = $"https://spdx.org/licenses/{l.SpdxId}.html",
                IsOsiApproved = l.Osi, IsFsfLibre = l.Fsf, IsAllowed = l.Allowed, SortOrder = sort
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureLlmModelsAsync(CancellationToken ct)
    {
        var slugs = await db.LlmModels.Select(m => m.Slug).ToHashSetAsync(ct);
        var sort = 0;
        foreach (var m in ReferenceData.LlmModels)
        {
            sort++;
            var slug = TextUtil.Slugify($"{m.Vendor} {m.Name} {m.Version}");
            if (slugs.Contains(slug)) continue;
            db.LlmModels.Add(new LlmModel
            {
                Vendor = m.Vendor, Name = m.Name, Version = m.Version, Slug = slug, SortOrder = sort, IsActive = true,
                ReleasedOn = m.Released is null ? null : DateOnly.Parse(m.Released)
            });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureSettingsAsync(CancellationToken ct)
    {
        var keys = await db.SiteSettings.Select(s => s.Key).ToHashSetAsync(ct);
        foreach (var s in ReferenceData.Settings)
        {
            if (keys.Contains(s.Key)) continue;
            db.SiteSettings.Add(new SiteSetting { Key = s.Key, Value = s.Value, Group = s.Group, ValueType = s.Type, Description = s.Description, UpdatedAt = Clock.Now });
        }
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureAdminAsync(CancellationToken ct)
    {
        var email = configuration["Admin:Email"];
        if (string.IsNullOrWhiteSpace(email)) return;
        email = email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct)) return;

        var adminRole = await db.Roles.FirstAsync(r => r.Name == AdminRoleName, ct);
        var password = configuration["Admin:InitialPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            password = TextUtil.RandomToken(12);
            logger.LogWarning("Admin:InitialPassword is not configured. Generated password for {Email}: {Password}", email, password);
        }
        var username = configuration["Admin:Username"] ?? "admin";
        db.Users.Add(new User
        {
            Email = email, EmailVerifiedAt = Clock.Now, Username = username, DisplayName = "Administrator",
            PasswordHash = hasher.Hash(password), RoleId = adminRole.Id, IsActive = true, Locale = site.Value.DefaultLocale,
            TrustLevel = 2, CreatedAt = Clock.Now, UpdatedAt = Clock.Now
        });
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Administrator account created for {Email}.", email);
    }
}

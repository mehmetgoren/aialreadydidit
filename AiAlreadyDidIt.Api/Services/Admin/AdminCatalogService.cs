using AiAlreadyDidIt.Api.Contracts.Admin;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Apps;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AiAlreadyDidIt.Api.Services.Admin;

/// <summary>Admin › Catalog reference lists: categories (tree), tags, licenses, platforms, LLM models.</summary>
public sealed class AdminCatalogService(AadiDbContext db, CategoryIndexService categories, AuditService audit, AppLifecycleService lifecycle, IMemoryCache cache)
{
    private void Invalidate() { categories.Invalidate(); cache.Remove("site:config:v1"); cache.Remove("catalog:home:v1"); }

    // ---------------------------------------------------------------- categories

    public async Task<List<AdminCategoryNodeDto>> TreeAsync(CancellationToken ct)
    {
        var rows = await db.Categories.AsNoTracking().OrderBy(c => c.Level).ThenBy(c => c.SortOrder).ThenBy(c => c.NameEn).ToListAsync(ct);
        var counts = await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published).GroupBy(a => a.CategoryId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var nodes = rows.ToDictionary(r => r.Id, r => new AdminCategoryNodeDto
        {
            Id = r.Id, ParentId = r.ParentId, Level = r.Level, Slug = r.Slug, NameEn = r.NameEn, NameTr = r.NameTr, Description = r.Description, Icon = r.Icon, SortOrder = r.SortOrder,
            IsActive = r.IsActive, IsLlmProposed = r.IsLlmProposed, AppCount = counts.GetValueOrDefault(r.Id)
        });
        var roots = new List<AdminCategoryNodeDto>();
        foreach (var r in rows)
        {
            if (r.ParentId is { } pid && nodes.TryGetValue(pid, out var parent)) parent.Children.Add(nodes[r.Id]);
            else roots.Add(nodes[r.Id]);
        }
        int Total(AdminCategoryNodeDto n) { n.TotalAppCount = n.AppCount + n.Children.Sum(Total); return n.TotalAppCount; }
        foreach (var root in roots) Total(root);
        return roots;
    }

    public async Task<AdminCategoryNodeDto> CreateCategoryAsync(SaveCategoryRequest request, CancellationToken ct)
    {
        Category? parent = null;
        if (request.ParentId is { } pid)
        {
            parent = await db.Categories.FirstOrDefaultAsync(c => c.Id == pid, ct) ?? throw ApiException.Unprocessable("Parent not found.", "parentId");
            if (parent.Level >= 3) throw ApiException.Unprocessable("Categories go three levels deep at most.", "parentId");
        }
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? (parent is null ? TextUtil.Slugify(request.NameEn) : parent.Slug + "-" + TextUtil.Slugify(request.NameEn)) : TextUtil.Slugify(request.Slug, 120);
        if (await db.Categories.AnyAsync(c => c.Slug == slug, ct)) throw ApiException.Unprocessable("Slug already exists.", "slug");
        var siblingsMax = await db.Categories.Where(c => c.ParentId == request.ParentId).Select(c => (int?)c.SortOrder).MaxAsync(ct) ?? 0;
        var category = new Category
        {
            ParentId = request.ParentId, Level = (parent?.Level ?? 0) + 1, Slug = slug, NameEn = request.NameEn.Trim(), NameTr = string.IsNullOrWhiteSpace(request.NameTr) ? request.NameEn.Trim() : request.NameTr.Trim(),
            Description = request.Description?.Trim(), Icon = request.Icon?.Trim(), SortOrder = siblingsMax + 1, IsActive = request.IsActive
        };
        db.Categories.Add(category);
        audit.Log("category.create", "category", null, request);
        await db.SaveChangesAsync(ct);
        Invalidate();
        return (await FindNodeAsync(category.Id, ct))!;
    }

    public async Task<AdminCategoryNodeDto> UpdateCategoryAsync(int id, SaveCategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw ApiException.NotFound("Category not found.");
        category.NameEn = request.NameEn.Trim();
        category.NameTr = string.IsNullOrWhiteSpace(request.NameTr) ? request.NameEn.Trim() : request.NameTr.Trim();
        category.Description = request.Description?.Trim();
        category.Icon = request.Icon?.Trim();
        category.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var slug = TextUtil.Slugify(request.Slug, 120);
            if (slug != category.Slug && await db.Categories.AnyAsync(c => c.Slug == slug && c.Id != id, ct)) throw ApiException.Unprocessable("Slug already exists.", "slug");
            category.Slug = slug;
        }
        if (category.IsLlmProposed && request.IsActive) category.IsLlmProposed = false;
        audit.Log("category.update", "category", id, request);
        await db.SaveChangesAsync(ct);
        Invalidate();
        return (await FindNodeAsync(id, ct))!;
    }

    public async Task<AdminCategoryNodeDto> ApproveProposedAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw ApiException.NotFound("Category not found.");
        category.IsLlmProposed = false;
        category.IsActive = true;
        // apps whose LLM suggestion pointed here move into it
        await db.Apps.Where(a => a.LlmSuggestedCategoryId == id && a.Status == AppStatus.PendingReview).ExecuteUpdateAsync(s => s.SetProperty(a => a.CategoryId, id), ct);
        audit.Log("category.approve_proposed", "category", id);
        await db.SaveChangesAsync(ct);
        Invalidate();
        return (await FindNodeAsync(id, ct))!;
    }

    public async Task<AdminCategoryNodeDto> MoveCategoryAsync(int id, MoveCategoryRequest request, CancellationToken ct)
    {
        var category = await db.Categories.Include(c => c.Children).ThenInclude(c => c.Children).FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw ApiException.NotFound("Category not found.");
        Category? parent = null;
        if (request.NewParentId is { } pid)
        {
            if (pid == id) throw ApiException.Unprocessable("A category cannot be its own parent.");
            parent = await db.Categories.FirstOrDefaultAsync(c => c.Id == pid, ct) ?? throw ApiException.Unprocessable("Parent not found.");
            var depth = Depth(category);
            if (parent.Level + depth > 3) throw ApiException.Unprocessable("Moving here would exceed three levels.");
            var cursor = parent;
            while (cursor?.ParentId is { } cp) { if (cp == id) throw ApiException.Unprocessable("Cannot move a category under its own descendant."); cursor = await db.Categories.FirstOrDefaultAsync(c => c.Id == cp, ct); }
        }
        category.ParentId = parent?.Id;
        Relevel(category, (parent?.Level ?? 0) + 1);
        audit.Log("category.move", "category", id, request);
        await db.SaveChangesAsync(ct);
        Invalidate();
        return (await FindNodeAsync(id, ct))!;

        static int Depth(Category c) => 1 + (c.Children.Count == 0 ? 0 : c.Children.Max(Depth));
        static void Relevel(Category c, int level) { c.Level = level; foreach (var child in c.Children) Relevel(child, level + 1); }
    }

    public async Task ReorderCategoriesAsync(ReorderCategoriesRequest request, CancellationToken ct)
    {
        var rows = await db.Categories.Where(c => request.OrderedIds.Contains(c.Id)).ToListAsync(ct);
        var order = 0;
        foreach (var id in request.OrderedIds) { var row = rows.FirstOrDefault(r => r.Id == id); if (row is not null) row.SortOrder = ++order; }
        await db.SaveChangesAsync(ct);
        Invalidate();
    }

    public async Task MergeCategoryAsync(int id, MergeRequest request, CancellationToken ct)
    {
        if (id == request.TargetId) throw ApiException.Unprocessable("Choose a different target.");
        var source = await db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw ApiException.NotFound("Category not found.");
        if (source.Children.Count > 0) throw ApiException.Unprocessable("Move or delete the sub-categories first.");
        if (!await db.Categories.AnyAsync(c => c.Id == request.TargetId, ct)) throw ApiException.Unprocessable("Target not found.");
        await db.Apps.Where(a => a.CategoryId == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.CategoryId, request.TargetId).SetProperty(a => a.EmbeddingStale, true), ct);
        await db.Apps.Where(a => a.LlmSuggestedCategoryId == id).ExecuteUpdateAsync(s => s.SetProperty(a => a.LlmSuggestedCategoryId, request.TargetId), ct);
        db.Categories.Remove(source);
        audit.Log("category.merge", "category", id, request);
        await db.SaveChangesAsync(ct);
        await lifecycle.RecomputeReferenceCountsAsync(ct);
        Invalidate();
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken ct)
    {
        var category = await db.Categories.Include(c => c.Children).FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw ApiException.NotFound("Category not found.");
        if (category.Children.Count > 0) throw ApiException.Unprocessable("Delete or move the sub-categories first.");
        if (await db.Apps.AnyAsync(a => a.CategoryId == id, ct)) throw ApiException.Unprocessable("The category still has apps. Merge it into another category instead.");
        db.Categories.Remove(category);
        audit.Log("category.delete", "category", id, new { category.Slug });
        await db.SaveChangesAsync(ct);
        Invalidate();
    }

    private async Task<AdminCategoryNodeDto?> FindNodeAsync(int id, CancellationToken ct)
    {
        var tree = await TreeAsync(ct);
        AdminCategoryNodeDto? Find(List<AdminCategoryNodeDto> nodes) { foreach (var n in nodes) { if (n.Id == id) return n; var f = Find(n.Children); if (f is not null) return f; } return null; }
        return Find(tree);
    }

    // ---------------------------------------------------------------- tags

    public async Task<PagedResult<AdminTagDto>> TagsAsync(AdminListQuery query, CancellationToken ct)
    {
        var q = db.Tags.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Q)) q = q.Where(t => EF.Functions.ILike(t.Name, $"%{query.Q}%"));
        if (query.Status == "blocked") q = q.Where(t => t.IsBlocked);
        q = query.Sort == "name" ? q.OrderBy(t => t.Name) : q.OrderByDescending(t => t.UsageCount).ThenBy(t => t.Name);
        return await q.Select(t => new AdminTagDto { Id = t.Id, Name = t.Name, Slug = t.Slug, UsageCount = t.UsageCount, IsBlocked = t.IsBlocked }).ToPagedAsync(query.SafePage, query.SafePageSize, ct);
    }

    public async Task<AdminTagDto> UpdateTagAsync(int id, SaveTagRequest request, CancellationToken ct)
    {
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw ApiException.NotFound("Tag not found.");
        var slug = TextUtil.Slugify(request.Name, 48);
        if (slug != tag.Slug && await db.Tags.AnyAsync(t => t.Slug == slug, ct)) throw ApiException.Unprocessable("A tag with that name exists — merge instead.", "name");
        tag.Name = request.Name.Trim(); tag.Slug = slug; tag.IsBlocked = request.IsBlocked;
        if (tag.IsBlocked) await db.AppTags.Where(at => at.TagId == id).ExecuteDeleteAsync(ct);
        audit.Log("tag.update", "tag", id, request);
        await db.SaveChangesAsync(ct);
        await RefreshTagTextsAsync([id], ct);
        return new AdminTagDto { Id = tag.Id, Name = tag.Name, Slug = tag.Slug, UsageCount = tag.UsageCount, IsBlocked = tag.IsBlocked };
    }

    public async Task MergeTagAsync(int id, MergeRequest request, CancellationToken ct)
    {
        if (id == request.TargetId) throw ApiException.Unprocessable("Choose a different target.");
        if (!await db.Tags.AnyAsync(t => t.Id == request.TargetId, ct)) throw ApiException.Unprocessable("Target not found.");
        var appIds = await db.AppTags.Where(at => at.TagId == id).Select(at => at.AppId).ToListAsync(ct);
        foreach (var appId in appIds)
            if (!await db.AppTags.AnyAsync(at => at.AppId == appId && at.TagId == request.TargetId, ct)) db.AppTags.Add(new AppTag { AppId = appId, TagId = request.TargetId });
        await db.SaveChangesAsync(ct);
        await db.AppTags.Where(at => at.TagId == id).ExecuteDeleteAsync(ct);
        await db.Tags.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
        audit.Log("tag.merge", "tag", id, request);
        await db.SaveChangesAsync(ct);
        await RefreshTagTextsAsync([request.TargetId], ct);
    }

    public async Task DeleteTagAsync(int id, CancellationToken ct)
    {
        var appIds = await db.AppTags.Where(at => at.TagId == id).Select(at => at.AppId).ToListAsync(ct);
        await db.AppTags.Where(at => at.TagId == id).ExecuteDeleteAsync(ct);
        await db.Tags.Where(t => t.Id == id).ExecuteDeleteAsync(ct);
        audit.Log("tag.delete", "tag", id);
        await db.SaveChangesAsync(ct);
        foreach (var app in await db.Apps.Where(a => appIds.Contains(a.Id)).ToListAsync(ct)) await lifecycle.RefreshSearchTextAsync(app, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task RefreshTagTextsAsync(int[] tagIds, CancellationToken ct)
    {
        var appIds = await db.AppTags.Where(at => tagIds.Contains(at.TagId)).Select(at => at.AppId).Distinct().ToListAsync(ct);
        foreach (var app in await db.Apps.Where(a => appIds.Contains(a.Id)).ToListAsync(ct)) await lifecycle.RefreshSearchTextAsync(app, ct);
        await db.SaveChangesAsync(ct);
        await lifecycle.RecomputeReferenceCountsAsync(ct);
    }

    // ---------------------------------------------------------------- licenses

    public Task<List<AdminLicenseDto>> LicensesAsync(CancellationToken ct) =>
        db.Licenses.AsNoTracking().OrderBy(l => l.SortOrder).Select(l => new AdminLicenseDto { Id = l.Id, SpdxId = l.SpdxId, Name = l.Name, Url = l.Url, Family = l.Family, IsOsiApproved = l.IsOsiApproved, IsFsfLibre = l.IsFsfLibre, IsAllowed = l.IsAllowed, SortOrder = l.SortOrder, AppCount = l.AppCount }).ToListAsync(ct);

    public async Task<AdminLicenseDto> SaveLicenseAsync(int? id, SaveLicenseRequest request, CancellationToken ct)
    {
        var license = id is null ? new License() : await db.Licenses.FirstOrDefaultAsync(l => l.Id == id, ct) ?? throw ApiException.NotFound("License not found.");
        var spdx = request.SpdxId.Trim();
        if (await db.Licenses.AnyAsync(l => l.SpdxId == spdx && l.Id != (id ?? 0), ct)) throw ApiException.Unprocessable("SPDX id already exists.", "spdxId");
        license.SpdxId = spdx; license.Name = request.Name.Trim(); license.Url = string.IsNullOrWhiteSpace(request.Url) ? $"https://spdx.org/licenses/{spdx}.html" : request.Url.Trim();
        license.Family = string.IsNullOrWhiteSpace(request.Family) ? Infrastructure.Import.LicenseDetector.Family(spdx) : request.Family.Trim();
        license.IsOsiApproved = request.IsOsiApproved; license.IsFsfLibre = request.IsFsfLibre; license.IsAllowed = request.IsAllowed; license.SortOrder = request.SortOrder;
        if (id is null) db.Licenses.Add(license);
        audit.Log(id is null ? "license.create" : "license.update", "license", id, request);
        await db.SaveChangesAsync(ct);
        Invalidate();
        return (await LicensesAsync(ct)).First(l => l.Id == license.Id);
    }

    public async Task DeleteLicenseAsync(int id, CancellationToken ct)
    {
        if (await db.Apps.AnyAsync(a => a.LicenseId == id, ct)) throw ApiException.Unprocessable("Apps use this license; disable it instead.");
        await db.Licenses.Where(l => l.Id == id).ExecuteDeleteAsync(ct);
        audit.Log("license.delete", "license", id);
        await db.SaveChangesAsync(ct);
        Invalidate();
    }

    // ---------------------------------------------------------------- platforms

    public Task<List<AdminPlatformDto>> PlatformsAsync(CancellationToken ct) =>
        db.Platforms.AsNoTracking().OrderBy(p => p.SortOrder).Select(p => new AdminPlatformDto
        {
            Id = p.Id, Code = p.Code, Name = p.Name, Icon = p.Icon, AllowedExtensions = p.AllowedExtensions, AllowsExternalReference = p.AllowsExternalReference, InstallHint = p.InstallHint, SortOrder = p.SortOrder, IsActive = p.IsActive,
            FileCount = db.AppFiles.Count(f => f.PlatformId == p.Id)
        }).ToListAsync(ct);

    public async Task<AdminPlatformDto> SavePlatformAsync(int? id, SavePlatformRequest request, CancellationToken ct)
    {
        var platform = id is null ? new Platform() : await db.Platforms.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw ApiException.NotFound("Platform not found.");
        var code = request.Code.Trim().ToLowerInvariant();
        if (await db.Platforms.AnyAsync(p => p.Code == code && p.Id != (id ?? 0), ct)) throw ApiException.Unprocessable("Code already exists.", "code");
        platform.Code = code; platform.Name = request.Name.Trim(); platform.Icon = request.Icon?.Trim();
        platform.AllowedExtensions = string.Join(',', request.AllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : "." + e.ToLowerInvariant()));
        platform.AllowsExternalReference = request.AllowsExternalReference; platform.InstallHint = request.InstallHint?.Trim(); platform.SortOrder = request.SortOrder; platform.IsActive = request.IsActive;
        if (id is null) db.Platforms.Add(platform);
        audit.Log(id is null ? "platform.create" : "platform.update", "platform", id, request);
        await db.SaveChangesAsync(ct);
        Invalidate();
        return (await PlatformsAsync(ct)).First(p => p.Id == platform.Id);
    }

    // ---------------------------------------------------------------- LLM models

    public Task<List<AdminLlmModelDto>> LlmModelsAsync(CancellationToken ct) =>
        db.LlmModels.AsNoTracking().OrderBy(m => m.SortOrder).Select(m => new AdminLlmModelDto { Id = m.Id, Vendor = m.Vendor, Name = m.Name, Version = m.Version, Slug = m.Slug, ReleasedOn = m.ReleasedOn, IsActive = m.IsActive, SortOrder = m.SortOrder, AppCount = m.AppCount }).ToListAsync(ct);

    public async Task<AdminLlmModelDto> SaveLlmModelAsync(int? id, SaveLlmModelRequest request, CancellationToken ct)
    {
        var model = id is null ? new LlmModel() : await db.LlmModels.FirstOrDefaultAsync(m => m.Id == id, ct) ?? throw ApiException.NotFound("Model not found.");
        var slug = TextUtil.Slugify($"{request.Vendor} {request.Name} {request.Version}");
        if (await db.LlmModels.AnyAsync(m => m.Slug == slug && m.Id != (id ?? 0), ct)) throw ApiException.Unprocessable("This model already exists.", "name");
        model.Vendor = request.Vendor.Trim(); model.Name = request.Name.Trim(); model.Version = string.IsNullOrWhiteSpace(request.Version) ? null : request.Version.Trim(); model.Slug = slug;
        model.ReleasedOn = request.ReleasedOn; model.IsActive = request.IsActive; model.SortOrder = request.SortOrder;
        if (id is null) db.LlmModels.Add(model);
        audit.Log(id is null ? "llm_model.create" : "llm_model.update", "llm_model", id, request);
        await db.SaveChangesAsync(ct);
        Invalidate();
        return (await LlmModelsAsync(ct)).First(m => m.Id == model.Id);
    }

    public async Task DeleteLlmModelAsync(int id, CancellationToken ct)
    {
        if (await db.Apps.AnyAsync(a => a.LlmModelId == id, ct)) throw ApiException.Unprocessable("Apps reference this model; deactivate it instead.");
        await db.LlmModels.Where(m => m.Id == id).ExecuteDeleteAsync(ct);
        audit.Log("llm_model.delete", "llm_model", id);
        await db.SaveChangesAsync(ct);
        Invalidate();
    }
}

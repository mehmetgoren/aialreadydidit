using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AiAlreadyDidIt.Api.Services.Catalog;

/// <summary>Flat + hierarchical view of the active category tree with descendant app counts, cached for 5 minutes.</summary>
public sealed class CategoryIndexService(AadiDbContext db, IMemoryCache cache)
{
    private const string CacheKey = "catalog:category-index:v1";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public sealed record CategoryIndex(
        List<CategoryNodeDto> Roots,
        Dictionary<string, CategoryNodeDto> BySlug,
        Dictionary<int, CategoryNodeDto> ById,
        Dictionary<int, int[]> DescendantIdsById);

    public void Invalidate() => cache.Remove(CacheKey);

    public async Task<CategoryIndex> GetAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out CategoryIndex? cached) && cached is not null) return cached;

        var rows = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive && !c.IsLlmProposed)
            .OrderBy(c => c.Level).ThenBy(c => c.SortOrder).ThenBy(c => c.NameEn)
            .Select(c => new { c.Id, c.Slug, c.NameEn, c.NameTr, c.Icon, c.Level, c.ParentId })
            .ToListAsync(ct);
        var directCounts = await db.Apps.AsNoTracking()
            .Where(a => a.Status == AppStatus.Published)
            .GroupBy(a => a.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, ct);

        var nodes = rows.ToDictionary(r => r.Id, r => new CategoryNodeDto
        {
            Id = r.Id, Slug = r.Slug, NameEn = r.NameEn, NameTr = r.NameTr, Icon = r.Icon, Level = r.Level, ParentId = r.ParentId,
            AppCount = directCounts.GetValueOrDefault(r.Id)
        });
        var roots = new List<CategoryNodeDto>();
        foreach (var row in rows)
        {
            var node = nodes[row.Id];
            if (row.ParentId is { } pid && nodes.TryGetValue(pid, out var parent)) parent.Children.Add(node);
            else roots.Add(node);
        }

        var descendants = new Dictionary<int, int[]>();
        foreach (var root in roots) Accumulate(root);

        int Accumulate(CategoryNodeDto node)
        {
            var ids = new List<int> { node.Id };
            var total = node.AppCount;
            foreach (var child in node.Children)
            {
                total += Accumulate(child);
                ids.AddRange(descendants[child.Id]);
            }
            node.AppCount = total;
            descendants[node.Id] = ids.ToArray();
            return total;
        }

        var index = new CategoryIndex(roots, nodes.Values.ToDictionary(n => n.Slug, n => n), nodes, descendants);
        cache.Set(CacheKey, index, Ttl);
        return index;
    }

    public async Task<CategoryNodeDto> RequireBySlugAsync(string slug, CancellationToken ct)
    {
        var index = await GetAsync(ct);
        return index.BySlug.TryGetValue(slug.Trim(), out var node) ? node : throw ApiException.NotFound($"Category not found: {slug}");
    }

    /// <summary>Breadcrumb from the root down to (and including) the node.</summary>
    public async Task<List<CategoryNodeDto>> PathAsync(int categoryId, CancellationToken ct)
    {
        var index = await GetAsync(ct);
        var path = new List<CategoryNodeDto>();
        var current = index.ById.GetValueOrDefault(categoryId);
        while (current is not null)
        {
            path.Insert(0, Shallow(current));
            current = current.ParentId is { } pid ? index.ById.GetValueOrDefault(pid) : null;
        }
        return path;
    }

    public static CategoryNodeDto Shallow(CategoryNodeDto n) => new()
    {
        Id = n.Id, Slug = n.Slug, NameEn = n.NameEn, NameTr = n.NameTr, Icon = n.Icon, Level = n.Level, ParentId = n.ParentId, AppCount = n.AppCount
    };

    /// <summary>"Root › Sub › Leaf" text used in the search vector and embeddings.</summary>
    public async Task<string> PathTextAsync(int categoryId, CancellationToken ct)
    {
        var path = await PathAsync(categoryId, ct);
        return string.Join(" › ", path.Select(p => p.NameEn));
    }
}

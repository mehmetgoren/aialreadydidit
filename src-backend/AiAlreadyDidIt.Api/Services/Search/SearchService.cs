using System.Diagnostics;
using System.Text.Json;
using AiAlreadyDidIt.Api.Contracts.Catalog;
using AiAlreadyDidIt.Api.Contracts.Common;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Data.Seed;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Services.Catalog;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace AiAlreadyDidIt.Api.Services.Search;

/// <summary>
/// Hybrid search over published apps: PostgreSQL full-text (tsvector weighted, english-stemmed + exact tokens) + pgvector cosine similarity, fused with
/// reciprocal rank fusion. Filters (category subtree, platform, license, model, rating, tags, uploader) apply to both legs.
/// </summary>
public sealed class SearchService(AadiDbContext db, EmbeddingService embeddings, CategoryIndexService categories, SiteSettingsCache settings, ICurrentUser currentUser)
{
    private const int CandidateLimit = 200;
    private const int RrfK = 60;

    public sealed record Filters(int[]? CategoryIds, string? Platform, string? License, string? Model, int? MinRating, string[]? Tags, string? Uploader, bool FeaturedOnly);

    public async Task<AppSearchResultDto> SearchAsync(AppQuery query, RequestSource source, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var q = query.Q?.Trim();
        var hasQuery = !string.IsNullOrWhiteSpace(q);
        var filters = await BuildFiltersAsync(query, ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 96);

        IQueryable<App> Base() => ApplyFilters(db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published), filters);

        var result = new AppSearchResultDto { SemanticAvailable = embeddings.Available };
        double? topSimilarity = null;
        var modeUsed = "browse";

        if (!hasQuery)
        {
            var ordered = ApplySort(Base(), query.Sort ?? (query.Featured ? "featured" : "downloads"));
            result.Page = await ordered.Select(AppCardProjection.ToCard).ToPagedAsync(page, pageSize, ct);
        }
        else
        {
            var mode = (query.Mode ?? "hybrid").ToLowerInvariant();
            if (mode != "keyword" && !embeddings.Available) mode = "keyword";
            var minSimilarity = await settings.GetDoubleAsync(SettingKeys.SearchMinSimilarity, 0.45, ct);

            // keyword leg
            var keywordRanks = new Dictionary<int, int>();
            if (mode is "keyword" or "hybrid")
            {
                var keywordIds = await Base()
                    .Where(a => a.SearchVector!.Matches(EF.Functions.WebSearchToTsQuery("english", q!)) || EF.Functions.ILike(a.Name, $"%{q}%"))
                    .OrderByDescending(a => a.SearchVector!.Rank(EF.Functions.WebSearchToTsQuery("english", q!)))
                    .ThenByDescending(a => a.DownloadCount)
                    .Select(a => a.Id)
                    .Take(CandidateLimit)
                    .ToListAsync(ct);
                for (var i = 0; i < keywordIds.Count; i++) keywordRanks[keywordIds[i]] = i + 1;
            }

            // semantic leg
            var semanticRanks = new Dictionary<int, int>();
            var similarities = new Dictionary<int, double>();
            if (mode is "semantic" or "hybrid")
            {
                var vector = await embeddings.TryEmbedAsync(q!, ct);
                if (vector is not null)
                {
                    var rows = await Base()
                        .Where(a => a.Embedding != null)
                        .Select(a => new { a.Id, Distance = a.Embedding!.CosineDistance(vector) })
                        .OrderBy(x => x.Distance)
                        .Take(CandidateLimit)
                        .ToListAsync(ct);
                    var rank = 0;
                    foreach (var row in rows)
                    {
                        var sim = 1 - row.Distance;
                        if (sim < minSimilarity) break;
                        semanticRanks[row.Id] = ++rank;
                        similarities[row.Id] = sim;
                    }
                    topSimilarity = similarities.Values.DefaultIfEmpty(0).Max();
                }
                else if (mode == "semantic")
                {
                    mode = "keyword";
                    var ids = await Base().Where(a => a.SearchVector!.Matches(EF.Functions.WebSearchToTsQuery("english", q!)) || EF.Functions.ILike(a.Name, $"%{q}%"))
                        .OrderByDescending(a => a.SearchVector!.Rank(EF.Functions.WebSearchToTsQuery("english", q!))).Select(a => a.Id).Take(CandidateLimit).ToListAsync(ct);
                    for (var i = 0; i < ids.Count; i++) keywordRanks[ids[i]] = i + 1;
                }
            }
            modeUsed = mode;

            // fusion
            var kw = (double)await settings.GetDecimalAsync(SettingKeys.SearchKeywordWeight, 1, ct);
            var sw2 = (double)await settings.GetDecimalAsync(SettingKeys.SearchSemanticWeight, 1, ct);
            var scores = new Dictionary<int, double>();
            foreach (var (id, r) in keywordRanks) scores[id] = scores.GetValueOrDefault(id) + kw / (RrfK + r);
            foreach (var (id, r) in semanticRanks) scores[id] = scores.GetValueOrDefault(id) + sw2 / (RrfK + r);
            var orderedIds = scores.OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToList();

            // explicit sort overrides relevance order
            if (query.Sort is not null && query.Sort != "relevance")
            {
                var sortedIds = await ApplySort(db.Apps.AsNoTracking().Where(a => orderedIds.Contains(a.Id)), query.Sort).Select(a => a.Id).ToListAsync(ct);
                orderedIds = sortedIds;
            }

            var pageIds = orderedIds.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var cards = await db.Apps.AsNoTracking().Where(a => pageIds.Contains(a.Id)).Select(AppCardProjection.ToCard).ToListAsync(ct);
            var byId = cards.ToDictionary(c => c.Id);
            var items = pageIds.Where(byId.ContainsKey).Select(id =>
            {
                var c = byId[id];
                if (similarities.TryGetValue(id, out var s)) c.Similarity = Math.Round(s, 3);
                return c;
            }).ToList();
            result.Page = PagedResult<AppCardDto>.Create(items, page, pageSize, orderedIds.Count);
        }

        result.ModeUsed = modeUsed;
        await FillFacetsAsync(result, Base(), ct);

        sw.Stop();
        if (hasQuery)
        {
            db.SearchLogs.Add(new SearchLog
            {
                Query = TextUtil.Truncate(q, 500),
                Mode = modeUsed switch { "semantic" => SearchMode.Semantic, "hybrid" => SearchMode.Hybrid, _ => SearchMode.Keyword },
                FiltersJson = TextUtil.Truncate(JsonSerializer.Serialize(new { query.Category, query.Platform, query.License, query.Model, query.MinRating, query.Tags }, AadiJson.Options), 2000),
                ResultCount = result.Page.TotalCount,
                TopSimilarity = topSimilarity,
                Source = source,
                UserId = currentUser.IdOrNull,
                ApiKeyId = currentUser.ApiKeyId,
                TookMs = (int)sw.ElapsedMilliseconds,
                CreatedAt = Clock.Now
            });
            await db.SaveChangesAsync(ct);
        }
        return result;
    }

    /// <summary>Nearest published apps to a free text (duplicate detection, agent check, "similar apps").</summary>
    public async Task<List<AppCardDto>> NearestAsync(string text, int take, int? excludeAppId, double minSimilarity, CancellationToken ct)
    {
        var vector = await embeddings.TryEmbedAsync(text, ct);
        if (vector is null) return [];
        var rows = await db.Apps.AsNoTracking()
            .Where(a => a.Status == AppStatus.Published && a.Embedding != null && (excludeAppId == null || a.Id != excludeAppId))
            .Select(a => new { a.Id, Distance = a.Embedding!.CosineDistance(vector) })
            .OrderBy(x => x.Distance)
            .Take(take)
            .ToListAsync(ct);
        var keep = rows.Where(r => 1 - r.Distance >= minSimilarity).ToList();
        if (keep.Count == 0) return [];
        var ids = keep.Select(r => r.Id).ToList();
        var cards = await db.Apps.AsNoTracking().Where(a => ids.Contains(a.Id)).Select(AppCardProjection.ToCard).ToListAsync(ct);
        var byId = cards.ToDictionary(c => c.Id);
        return keep.Where(r => byId.ContainsKey(r.Id)).Select(r => { var c = byId[r.Id]; c.Similarity = Math.Round(1 - r.Distance, 3); return c; }).ToList();
    }

    /// <summary>Apps whose stored vector is close to the given app's vector.</summary>
    public async Task<List<AppCardDto>> SimilarToAppAsync(int appId, int take, CancellationToken ct)
    {
        var app = await db.Apps.AsNoTracking().Where(a => a.Id == appId).Select(a => new { a.Embedding, a.CategoryId, a.TagsText }).FirstOrDefaultAsync(ct);
        if (app is null) return [];
        if (app.Embedding is not null)
        {
            var vector = app.Embedding;
            var rows = await db.Apps.AsNoTracking()
                .Where(a => a.Status == AppStatus.Published && a.Embedding != null && a.Id != appId)
                .Select(a => new { a.Id, Distance = a.Embedding!.CosineDistance(vector) })
                .OrderBy(x => x.Distance).Take(take).ToListAsync(ct);
            var ids = rows.Select(r => r.Id).ToList();
            var cards = await db.Apps.AsNoTracking().Where(a => ids.Contains(a.Id)).Select(AppCardProjection.ToCard).ToListAsync(ct);
            var byId = cards.ToDictionary(c => c.Id);
            return rows.Where(r => byId.ContainsKey(r.Id)).Select(r => { var c = byId[r.Id]; c.Similarity = Math.Round(1 - r.Distance, 3); return c; }).ToList();
        }
        // fallback: same category, most downloaded
        return await db.Apps.AsNoTracking().Where(a => a.Status == AppStatus.Published && a.Id != appId && a.CategoryId == app.CategoryId)
            .OrderByDescending(a => a.DownloadCount).Take(take).Select(AppCardProjection.ToCard).ToListAsync(ct);
    }

    // ---------------------------------------------------------------- helpers

    public async Task<Filters> BuildFiltersAsync(AppQuery query, CancellationToken ct)
    {
        int[]? categoryIds = null;
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var index = await categories.GetAsync(ct);
            if (!index.BySlug.TryGetValue(query.Category.Trim(), out var node)) throw ApiException.NotFound($"Category not found: {query.Category}");
            categoryIds = index.DescendantIdsById[node.Id];
        }
        var tags = string.IsNullOrWhiteSpace(query.Tags) ? null : query.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(TextUtil.Slugify).ToArray();
        return new Filters(categoryIds, Norm(query.Platform), query.License?.Trim(), Norm(query.Model), query.MinRating, tags, Norm(query.Uploader), query.Featured);
        static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();
    }

    public static IQueryable<App> ApplyFilters(IQueryable<App> q, Filters f)
    {
        if (f.CategoryIds is not null) q = q.Where(a => f.CategoryIds.Contains(a.CategoryId));
        if (f.Platform is not null) q = q.Where(a => a.Versions.Any(v => v.Id == a.LatestVersionId && v.Files.Any(fl => fl.Platform != null && fl.Platform.Code == f.Platform)));
        if (f.License is not null) q = q.Where(a => a.License.SpdxId == f.License || a.License.Family == f.License);
        if (f.Model is not null) q = q.Where(a => a.LlmModel != null && (a.LlmModel.Slug == f.Model || a.LlmModel.Vendor.ToLower() == f.Model));
        if (f.MinRating is { } min) q = q.Where(a => a.RatingCount > 0 && a.RatingAvg >= min);
        if (f.Tags is { Length: > 0 }) foreach (var tag in f.Tags) q = q.Where(a => a.AppTags.Any(t => t.Tag.Slug == tag));
        if (f.Uploader is not null) q = q.Where(a => a.Uploader.Username == f.Uploader);
        if (f.FeaturedOnly) q = q.Where(a => a.IsFeatured);
        return q;
    }

    public static IQueryable<App> ApplySort(IQueryable<App> q, string? sort) => (sort ?? "downloads").ToLowerInvariant() switch
    {
        "rating" => q.OrderByDescending(a => a.RatingCount > 0 ? a.RatingAvg : 0).ThenByDescending(a => a.RatingCount).ThenByDescending(a => a.DownloadCount),
        "newest" => q.OrderByDescending(a => a.PublishedAt).ThenByDescending(a => a.Id),
        "updated" => q.OrderByDescending(a => a.UpdatedAt),
        "name" => q.OrderBy(a => a.Name),
        "featured" => q.OrderByDescending(a => a.IsFeatured).ThenBy(a => a.FeaturedOrder).ThenByDescending(a => a.DownloadCount),
        "savings" => q.OrderByDescending(a => a.EstGenerationTokens * (long)a.DownloadCount),
        _ => q.OrderByDescending(a => a.DownloadCount).ThenByDescending(a => a.RatingAvg)
    };

    private async Task FillFacetsAsync(AppSearchResultDto result, IQueryable<App> filtered, CancellationToken ct)
    {
        result.Platforms = await filtered.SelectMany(a => a.Versions.Where(v => v.Id == a.LatestVersionId).SelectMany(v => v.Files).Where(f => f.Platform != null).Select(f => new { a.Id, f.Platform!.Code, f.Platform.Name }))
            .Distinct().GroupBy(x => new { x.Code, x.Name }).Select(g => new FacetItemDto { Value = g.Key.Code, Label = g.Key.Name, Count = g.Count() })
            .OrderByDescending(f => f.Count).ToListAsync(ct);
        result.Licenses = await filtered.GroupBy(a => new { a.License.SpdxId, a.License.Name }).Select(g => new FacetItemDto { Value = g.Key.SpdxId, Label = g.Key.Name, Count = g.Count() })
            .OrderByDescending(f => f.Count).Take(20).ToListAsync(ct);
        result.Models = await filtered.Where(a => a.LlmModel != null).GroupBy(a => new { a.LlmModel!.Slug, a.LlmModel.Vendor, a.LlmModel.Name, a.LlmModel.Version })
            .Select(g => new FacetItemDto { Value = g.Key.Slug, Label = g.Key.Vendor + " " + g.Key.Name + (g.Key.Version == null ? "" : " " + g.Key.Version), Count = g.Count() })
            .OrderByDescending(f => f.Count).Take(20).ToListAsync(ct);
        var categoryCounts = await filtered.GroupBy(a => a.CategoryId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var index = await categories.GetAsync(ct);
        var rootCounts = new Dictionary<int, int>();
        foreach (var cc in categoryCounts)
        {
            var node = index.ById.GetValueOrDefault(cc.Key);
            while (node?.ParentId is { } pid && index.ById.TryGetValue(pid, out var parent)) node = parent;
            if (node is null) continue;
            rootCounts[node.Id] = rootCounts.GetValueOrDefault(node.Id) + cc.Count;
        }
        result.Categories = rootCounts.Select(kv => new FacetItemDto { Value = index.ById[kv.Key].Slug, Label = index.ById[kv.Key].NameEn, Count = kv.Value })
            .OrderByDescending(f => f.Count).ToList();
    }
}

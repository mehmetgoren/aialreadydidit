using System.Text.Json;
using AiAlreadyDidIt.Api.Contracts.Apps;
using AiAlreadyDidIt.Api.Data;
using AiAlreadyDidIt.Api.Entities;
using AiAlreadyDidIt.Api.Infrastructure;
using AiAlreadyDidIt.Api.Infrastructure.Ai;
using AiAlreadyDidIt.Api.Services.Catalog;
using AiAlreadyDidIt.Api.Services.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Services.Apps;

/// <summary>
/// LLM-assisted categorisation: given the app text, the chat model picks the best leaf of the existing tree, may propose a
/// new category (created as <c>IsLlmProposed</c>, waiting for admin approval) and suggests tags + a short description.
/// </summary>
public sealed class CategorySuggestionService(AadiDbContext db, ILlmProviderFactory providers, CategoryIndexService categories, IOptions<AiOptions> options, ILogger<CategorySuggestionService> logger)
{
    public bool Available => options.Value.EnableCategorySuggestions && providers.Chat.SupportsChat;

    public async Task<MetadataSuggestionDto> SuggestAsync(SuggestMetadataRequest request, CancellationToken ct)
    {
        var text = EmbeddingService.BuildDraftText(request.Name, request.ShortDescription, request.LongDescription);
        if (!string.IsNullOrWhiteSpace(request.Readme)) text += "\nREADME:\n" + TextUtil.Truncate(TextUtil.Excerpt(request.Readme, 3000), 3000);
        return await SuggestForTextAsync(text, ct);
    }

    public async Task<MetadataSuggestionDto> SuggestForTextAsync(string text, CancellationToken ct)
    {
        if (!Available) return new MetadataSuggestionDto { Available = false };
        if (text.Trim().Length < 20) return new MetadataSuggestionDto { Available = true, Reasoning = "Not enough text to categorise yet." };

        var index = await categories.GetAsync(ct);
        var leaves = index.ById.Values.Where(n => n.Children.Count == 0).Select(n => (n.Slug, Path: PathOf(n, index))).ToList();
        var catalogText = string.Join("\n", leaves.Select(l => $"- {l.Slug}: {l.Path}"));
        var system = """
            You classify software applications into a fixed category tree for an app store of LLM-generated apps.
            Answer with a JSON object only, no prose, with exactly these keys:
            {"category_slug": string|null, "new_category": {"name": string, "parent_slug": string}|null, "tags": string[], "short_description": string, "reasoning": string}
            Rules: prefer an existing leaf category slug from the list. Only fill new_category when no existing leaf fits at all; then pick the closest existing parent_slug.
            tags: 3-8 lower-case keywords a user would search for. short_description: one sentence, max 160 characters, plain text, English.
            """;
        var user = $"Category leaves (slug: path):\n{catalogText}\n\nApplication:\n{TextUtil.Truncate(text, 6000)}";
        string raw;
        try { raw = await providers.Chat.ChatAsync(system, user, new LlmChatOptions(JsonMode: true, Temperature: 0.1, MaxTokens: 700), ct); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Category suggestion failed");
            return new MetadataSuggestionDto { Available = true, Reasoning = "The model is unavailable right now: " + ex.Message, Model = providers.Chat.ChatModel };
        }

        var dto = new MetadataSuggestionDto { Available = true, Model = $"{providers.Chat.Name}:{providers.Chat.ChatModel}" };
        try
        {
            using var doc = JsonDocument.Parse(ExtractJson(raw));
            var root = doc.RootElement;
            var slug = root.TryGetProperty("category_slug", out var cs) && cs.ValueKind == JsonValueKind.String ? cs.GetString() : null;
            if (slug is not null && index.BySlug.TryGetValue(slug, out var node))
            {
                dto.CategoryId = node.Id;
                dto.CategorySlug = node.Slug;
                dto.CategoryPath = PathOf(node, index);
            }
            if (root.TryGetProperty("new_category", out var nc) && nc.ValueKind == JsonValueKind.Object)
            {
                var name = nc.TryGetProperty("name", out var n) ? n.GetString() : null;
                var parent = nc.TryGetProperty("parent_slug", out var p) ? p.GetString() : null;
                if (!string.IsNullOrWhiteSpace(name) && parent is not null && index.BySlug.ContainsKey(parent))
                {
                    dto.ProposedCategoryName = TextUtil.Truncate(name.Trim(), 120);
                    dto.ProposedCategoryParentSlug = parent;
                }
            }
            if (root.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
                dto.Tags = tags.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!.Trim().ToLowerInvariant()).Where(t => t.Length is >= 2 and <= 48).Distinct().Take(8).ToList();
            if (root.TryGetProperty("short_description", out var sd) && sd.ValueKind == JsonValueKind.String) dto.ShortDescription = TextUtil.Truncate(sd.GetString()!.Trim(), 200);
            if (root.TryGetProperty("reasoning", out var re) && re.ValueKind == JsonValueKind.String) dto.Reasoning = TextUtil.Truncate(re.GetString(), 500);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Category suggestion returned non-JSON: {Raw}", TextUtil.Truncate(raw, 300));
            dto.Reasoning = "The model did not return a usable answer.";
        }
        return dto;
    }

    /// <summary>Background variant: stores the suggestion on the app and creates a proposed category when requested.</summary>
    public async Task ApplyToAppAsync(int appId, CancellationToken ct)
    {
        var app = await db.Apps.Include(a => a.LlmModel).FirstOrDefaultAsync(a => a.Id == appId, ct);
        if (app is null || !Available) return;
        var suggestion = await SuggestForTextAsync(EmbeddingService.BuildAppText(app), ct);
        if (suggestion.CategoryId is { } cid) app.LlmSuggestedCategoryId = cid;
        else if (suggestion.ProposedCategoryName is not null && suggestion.ProposedCategoryParentSlug is not null)
        {
            var parent = await db.Categories.FirstOrDefaultAsync(c => c.Slug == suggestion.ProposedCategoryParentSlug, ct);
            if (parent is not null && parent.Level < 3)
            {
                var slug = parent.Slug + "-" + TextUtil.Slugify(suggestion.ProposedCategoryName);
                var existing = await db.Categories.FirstOrDefaultAsync(c => c.Slug == slug, ct);
                if (existing is null)
                {
                    existing = new Category { ParentId = parent.Id, Level = parent.Level + 1, Slug = slug, NameEn = suggestion.ProposedCategoryName, NameTr = suggestion.ProposedCategoryName, IsActive = false, IsLlmProposed = true, SortOrder = 999 };
                    db.Categories.Add(existing);
                    await db.SaveChangesAsync(ct);
                }
                app.LlmSuggestedCategoryId = existing.Id;
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static string PathOf(Contracts.Catalog.CategoryNodeDto node, CategoryIndexService.CategoryIndex index)
    {
        var parts = new List<string>();
        var current = node;
        while (current is not null)
        {
            parts.Insert(0, current.NameEn);
            current = current.ParentId is { } pid ? index.ById.GetValueOrDefault(pid) : null;
        }
        return string.Join(" › ", parts);
    }

    private static string ExtractJson(string raw)
    {
        var s = raw.Trim();
        if (s.StartsWith("```")) { s = s[(s.IndexOf('\n') + 1)..]; var end = s.LastIndexOf("```", StringComparison.Ordinal); if (end > 0) s = s[..end]; }
        var start = s.IndexOf('{');
        var stop = s.LastIndexOf('}');
        return start >= 0 && stop > start ? s[start..(stop + 1)] : s;
    }
}

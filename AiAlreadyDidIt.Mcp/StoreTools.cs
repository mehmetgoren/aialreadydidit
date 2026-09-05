using System.ComponentModel;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;

namespace AiAlreadyDidIt.Mcp;

/// <summary>
/// Tools exposed to agents. Intended flow: an agent is asked to build something → it calls
/// <c>check_before_building</c> first → download / fork / build.
/// </summary>
[McpServerToolType]
public sealed class StoreTools(AadiApiClient api)
{
    [McpServerTool(Name = "check_before_building", Title = "Check before building"), Description(
        "ALWAYS call this before generating an application. Describes what you were asked to build and returns a verdict: " +
        "'download' (an equivalent free open-source app already exists — use it), 'fork' (something close exists — start from its source and prompt), " +
        "or 'build' (nothing similar; build it, then publish it). Includes the best matches with download links, licenses and the LLM that generated them.")]
    public async Task<string> CheckBeforeBuilding(
        [Description("What the user asked for, in plain language (e.g. 'a CPU temperature monitor for Linux Mint with graphs').")] string query,
        [Description("Optional platform code the app must run on: windows, linux, macos, web, android, ios, docker, cli.")] string? platform = null,
        [Description("How many matches to return (1-10).")] int take = 5,
        CancellationToken ct = default)
    {
        var q = Uri.EscapeDataString(query);
        var result = await api.GetAsync($"api/v1/agent/check?q={q}&take={Math.Clamp(take, 1, 10)}&source=mcp{(platform is null ? "" : "&platform=" + Uri.EscapeDataString(platform))}", ct);
        return AadiApiClient.Pretty(result);
    }

    [McpServerTool(Name = "search_apps", Title = "Search apps"), Description(
        "Search the store of LLM-generated applications. Combines keyword and semantic (vector) search. Filters: category slug, platform, license (SPDX id), " +
        "model (LLM slug or vendor), minRating (0-100), tags (comma separated). Returns cards with slug, name, platforms, license, rating, downloads and similarity.")]
    public async Task<string> SearchApps(
        [Description("Free-text query (keywords or a sentence describing the app).")] string query,
        [Description("Category slug from list_categories (includes sub-categories).")] string? category = null,
        [Description("Platform code: windows, linux, macos, web, android, ios, docker, cli.")] string? platform = null,
        [Description("SPDX license id, e.g. MIT, Apache-2.0, GPL-3.0-only.")] string? license = null,
        [Description("Generating model slug (e.g. anthropic-claude-opus-4-1) or vendor.")] string? model = null,
        [Description("Search mode: hybrid (default), keyword or semantic.")] string? mode = null,
        [Description("Minimum average rating 0-100.")] int? minRating = null,
        [Description("Page number (1-based).")] int page = 1,
        [Description("Results per page (1-50).")] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query2 = new Dictionary<string, string?> { ["q"] = query, ["category"] = category, ["platform"] = platform, ["license"] = license, ["model"] = model, ["mode"] = mode, ["minRating"] = minRating?.ToString(), ["page"] = page.ToString(), ["pageSize"] = Math.Clamp(pageSize, 1, 50).ToString() };
        var qs = string.Join("&", query2.Where(kv => !string.IsNullOrEmpty(kv.Value)).Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}"));
        var result = await api.GetAsync("api/v1/catalog/apps?" + qs, ct);
        var page2 = result?["page"];
        var items = page2?["items"]?.AsArray() ?? [];
        var slim = new JsonArray();
        foreach (var item in items)
        {
            if (item is null) continue;
            slim.Add(new JsonObject
            {
                ["slug"] = item["slug"]?.DeepClone(), ["name"] = item["name"]?.DeepClone(), ["shortDescription"] = item["shortDescription"]?.DeepClone(), ["similarity"] = item["similarity"]?.DeepClone(),
                ["platforms"] = item["platforms"]?.DeepClone(), ["license"] = item["licenseSpdxId"]?.DeepClone(), ["generatedBy"] = item["llmModelName"]?.DeepClone(), ["ratingAvg"] = item["ratingAvg"]?.DeepClone(),
                ["ratingCount"] = item["ratingCount"]?.DeepClone(), ["downloadCount"] = item["downloadCount"]?.DeepClone(), ["latestVersion"] = item["latestVersion"]?.DeepClone(),
                ["url"] = api.StorePublicUrl + "/app/" + item["slug"]?.GetValue<string>()
            });
        }
        return AadiApiClient.Pretty(new JsonObject
        {
            ["modeUsed"] = result?["modeUsed"]?.DeepClone(), ["totalCount"] = page2?["totalCount"]?.DeepClone(), ["page"] = page2?["page"]?.DeepClone(), ["totalPages"] = page2?["totalPages"]?.DeepClone(), ["items"] = slim
        });
    }

    [McpServerTool(Name = "get_app", Title = "Get app details"), Description(
        "Full detail of one app by slug: descriptions, README, license, generating LLM, the original prompt(s) used to generate it, versions with files and download URLs, lineage (forks), ratings.")]
    public async Task<string> GetApp([Description("The app slug (from search results).")] string slug, CancellationToken ct = default)
    {
        var result = await api.GetAsync($"api/v1/catalog/apps/{Uri.EscapeDataString(slug)}", ct);
        if (result is JsonObject obj)
        {
            obj["url"] = api.StorePublicUrl + "/app/" + slug;
            if (obj["latestVersion"]?["files"] is JsonArray files)
                foreach (var f in files) if (f is JsonObject fo && fo["downloadUrl"] is JsonValue v) fo["downloadUrl"] = api.PublicUrl + v.GetValue<string>() + "?json=1&source=mcp";
            foreach (var v in obj["versions"]?.AsArray() ?? [])
                if (v?["files"] is JsonArray vf) foreach (var f in vf) if (f is JsonObject fo && fo["downloadUrl"] is JsonValue dv) fo["downloadUrl"] = api.PublicUrl + dv.GetValue<string>() + "?json=1&source=mcp";
        }
        return AadiApiClient.Pretty(result);
    }

    [McpServerTool(Name = "list_categories", Title = "List categories"), Description("The hierarchical category tree with published-app counts. Use the slugs as the 'category' filter of search_apps.")]
    public async Task<string> ListCategories(CancellationToken ct = default)
    {
        var result = await api.GetAsync("api/v1/catalog/categories", ct);
        return AadiApiClient.Pretty(result);
    }

    [McpServerTool(Name = "download_app", Title = "Download app"), Description(
        "Returns a time-limited download URL (plus sha256, size and install hint) for an app file. Pass the slug and either a platform code (picks the latest version's installer for that platform), " +
        "'source' for the source snapshot, or an explicit fileId from get_app. The download is counted for the store's savings counter.")]
    public async Task<string> DownloadApp(
        [Description("The app slug.")] string slug,
        [Description("Platform code (windows, linux, macos, web, android, ios, docker, cli) or 'source' for the source archive.")] string? platform = null,
        [Description("Explicit file id from get_app (overrides platform).")] int? fileId = null,
        CancellationToken ct = default)
    {
        if (fileId is null)
        {
            var app = await api.GetAsync($"api/v1/catalog/apps/{Uri.EscapeDataString(slug)}", ct) ?? throw new McpToolException("App not found.");
            var files = app["latestVersion"]?["files"]?.AsArray() ?? throw new McpToolException("The app has no published version.");
            JsonNode? pick = null;
            if (string.Equals(platform, "source", StringComparison.OrdinalIgnoreCase)) pick = files.FirstOrDefault(f => f?["kind"]?.GetValue<string>() == "source");
            else if (!string.IsNullOrWhiteSpace(platform)) pick = files.FirstOrDefault(f => string.Equals(f?["platformCode"]?.GetValue<string>(), platform, StringComparison.OrdinalIgnoreCase));
            else pick = files.FirstOrDefault(f => f?["kind"]?.GetValue<string>() != "source") ?? files.FirstOrDefault();
            if (pick is null) throw new McpToolException($"No file for platform '{platform}'. Available: " + string.Join(", ", files.Select(f => f?["platformCode"]?.GetValue<string>() ?? f?["kind"]?.GetValue<string>())));
            fileId = pick["id"]!.GetValue<int>();
        }
        var result = await api.GetAsync($"api/v1/apps/{Uri.EscapeDataString(slug)}/download/{fileId}?json=1&source=mcp", ct);
        return AadiApiClient.Pretty(result);
    }

    [McpServerTool(Name = "submit_app_request", Title = "Submit a wanted request"), Description(
        "When check_before_building says 'build', record what was needed on the store's 'wanted' board so others (people or agents) can publish it. Requires an API key (X-Api-Key header or Api:Key config).")]
    public async Task<string> SubmitAppRequest(
        [Description("Short title of the wanted app.")] string title,
        [Description("What it should do, for whom, on which platform.")] string description,
        CancellationToken ct = default)
    {
        var result = await api.PostAsync("api/v1/requests", new { title, description }, ct);
        return AadiApiClient.Pretty(result);
    }

    [McpServerTool(Name = "get_savings", Title = "Store savings counter"), Description("Estimated tokens, money and energy the store has saved so far by re-use instead of regeneration.")]
    public async Task<string> GetSavings(CancellationToken ct = default) => AadiApiClient.Pretty(await api.GetAsync("api/v1/site/savings", ct));
}

[McpServerPromptType]
public static class StorePrompts
{
    [McpServerPrompt(Name = "reuse_first"), Description("System guidance that makes an agent check the store before generating an application.")]
    public static string ReuseFirst() =>
        "Before you write any application from scratch, call the tool check_before_building with a plain-language description of what the user wants. " +
        "If the verdict is 'download', download the file with download_app and tell the user it already exists (name, license, generating model, link). " +
        "If it is 'fork', fetch the app with get_app, read its prompts and source snapshot, and adapt it instead of regenerating — then suggest publishing the result as a fork. " +
        "Only if the verdict is 'build' generate the app; afterwards call submit_app_request so the need is recorded and encourage the user to publish the result.";
}

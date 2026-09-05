using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AiAlreadyDidIt.Mcp;

/// <summary>Thin client of the public REST API. The MCP server never touches the database — the API is the single source of truth.</summary>
public sealed class AadiApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor? accessor = null)
{
    public const string HttpClientName = "aadi-api";

    public string BaseUrl => (configuration["Api:BaseUrl"] ?? "http://localhost:5190").TrimEnd('/');
    public string PublicUrl => (configuration["Api:PublicUrl"] ?? BaseUrl).TrimEnd('/');
    public string StorePublicUrl => (configuration["Site:PublicUrl"] ?? "http://localhost:9002").TrimEnd('/');

    private HttpClient Create()
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        client.BaseAddress = new Uri(BaseUrl + "/");
        client.Timeout = TimeSpan.FromSeconds(60);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AiAlreadyDidIt-MCP/1.0");
        // An API key can be configured for the server (Api:Key) or forwarded from the MCP client's HTTP headers.
        var key = accessor?.HttpContext?.Request.Headers["X-Api-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            var auth = accessor?.HttpContext?.Request.Headers.Authorization.ToString();
            if (auth is not null && auth.StartsWith("Bearer aad_", StringComparison.Ordinal)) key = auth[7..];
        }
        if (string.IsNullOrWhiteSpace(key)) key = configuration["Api:Key"];
        if (!string.IsNullOrWhiteSpace(key)) client.DefaultRequestHeaders.Add("X-Api-Key", key);
        return client;
    }

    public async Task<JsonNode?> GetAsync(string path, CancellationToken ct)
    {
        using var client = Create();
        using var response = await client.GetAsync(path, ct);
        return await Unwrap(response, ct);
    }

    public async Task<JsonNode?> PostAsync(string path, object body, CancellationToken ct)
    {
        using var client = Create();
        using var response = await client.PostAsJsonAsync(path, body, ct);
        return await Unwrap(response, ct);
    }

    private static async Task<JsonNode?> Unwrap(HttpResponseMessage response, CancellationToken ct)
    {
        var text = await response.Content.ReadAsStringAsync(ct);
        JsonNode? node;
        try { node = JsonNode.Parse(text); }
        catch (JsonException) { throw new McpToolException($"API returned a non-JSON response ({(int)response.StatusCode})."); }
        var status = node?["statusCode"]?.GetValue<int>() ?? (int)response.StatusCode;
        if (status is 200) return node?["result"];
        var message = node?["errors"]?[0]?["message"]?.GetValue<string>() ?? node?["statusMessage"]?.GetValue<string>() ?? response.ReasonPhrase ?? "error";
        throw new McpToolException($"{status}: {message}");
    }

    public static string Pretty(JsonNode? node) => node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null";
}

public sealed class McpToolException(string message) : Exception(message);

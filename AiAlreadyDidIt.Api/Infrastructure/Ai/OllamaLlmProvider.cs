using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiAlreadyDidIt.Api.Infrastructure.Ai;

/// <summary>Ollama REST API (/api/embed, /api/chat). Works with the compose container or a host Ollama.</summary>
public sealed class OllamaLlmProvider(AiOptions options, IHttpClientFactory httpClientFactory, ILogger<OllamaLlmProvider> logger) : ILlmProvider
{
    public const string HttpClientName = "ollama";
    private readonly OllamaOptions _o = options.Ollama;

    public LlmProviderKind Kind => LlmProviderKind.Ollama;
    public string Name => "Ollama";
    public bool SupportsEmbeddings => !string.IsNullOrWhiteSpace(_o.EmbeddingModel);
    public bool SupportsChat => !string.IsNullOrWhiteSpace(_o.ChatModel);
    public string EmbeddingModel => _o.EmbeddingModel;
    public string ChatModel => _o.ChatModel;

    private HttpClient Client()
    {
        var c = httpClientFactory.CreateClient(HttpClientName);
        c.BaseAddress = new Uri(_o.BaseUrl.TrimEnd('/') + "/");
        c.Timeout = TimeSpan.FromSeconds(_o.TimeoutSeconds);
        return c;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default) =>
        (await EmbedManyAsync([text], ct))[0];

    public async Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        using var client = Client();
        var response = await client.PostAsJsonAsync("api/embed", new { model = _o.EmbeddingModel, input = texts, truncate = true }, ct);
        await EnsureOk(response, ct);
        var body = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("Ollama returned an empty embedding response.");
        if (body.Embeddings is null || body.Embeddings.Count != texts.Count)
            throw new InvalidOperationException($"Ollama returned {body.Embeddings?.Count ?? 0} embeddings for {texts.Count} inputs.");
        return body.Embeddings.Select(e => VectorMath.Normalize(VectorMath.Fit(e, options.EmbeddingDimensions))).ToList();
    }

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt, LlmChatOptions? opts = null, CancellationToken ct = default)
    {
        opts ??= new LlmChatOptions();
        using var client = Client();
        var request = new Dictionary<string, object?>
        {
            ["model"] = _o.ChatModel,
            ["stream"] = false,
            ["think"] = _o.Think,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["options"] = new Dictionary<string, object?>
            {
                ["temperature"] = opts.Temperature,
                ["num_predict"] = opts.MaxTokens,
                ["num_ctx"] = _o.NumCtx > 0 ? _o.NumCtx : null
            }
        };
        if (opts.JsonMode) request["format"] = "json";
        var response = await client.PostAsJsonAsync("api/chat", request, ct);
        await EnsureOk(response, ct);
        var body = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: ct);
        var content = body?.Message?.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content)) logger.LogWarning("Ollama chat returned empty content (done_reason={Reason}).", body?.DoneReason);
        return content.Trim();
    }

    public async Task<LlmHealth> CheckHealthAsync(LlmCapability capability = LlmCapability.Both, CancellationToken ct = default)
    {
        try
        {
            using var client = Client();
            client.Timeout = TimeSpan.FromSeconds(5);
            var tags = await client.GetFromJsonAsync<TagsResponse>("api/tags", ct);
            var names = tags?.Models?.Select(m => m.Name).ToList() ?? [];
            var missing = new List<string>();
            var wantEmbeddings = capability is LlmCapability.Both or LlmCapability.Embeddings;
            var wantChat = capability is LlmCapability.Both or LlmCapability.Chat;
            if (wantEmbeddings && SupportsEmbeddings && !names.Any(n => Matches(n, _o.EmbeddingModel))) missing.Add(_o.EmbeddingModel);
            if (wantChat && SupportsChat && !names.Any(n => Matches(n, _o.ChatModel))) missing.Add(_o.ChatModel);
            return new LlmHealth(missing.Count == 0, Name, _o.ChatModel, _o.EmbeddingModel,
                missing.Count == 0 ? $"{names.Count} models available" : "missing models: " + string.Join(", ", missing));
        }
        catch (Exception ex)
        {
            return new LlmHealth(false, Name, _o.ChatModel, _o.EmbeddingModel, ex.Message);
        }

        static bool Matches(string available, string wanted) =>
            string.Equals(available, wanted, StringComparison.OrdinalIgnoreCase) ||
            (!wanted.Contains(':') && available.StartsWith(wanted + ":", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task EnsureOk(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var text = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"Ollama {(int)response.StatusCode}: {TextUtil.Truncate(text, 400)}");
    }

    private sealed class EmbedResponse
    {
        [JsonPropertyName("embeddings")] public List<float[]>? Embeddings { get; set; }
    }

    private sealed class ChatResponse
    {
        [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
        [JsonPropertyName("done_reason")] public string? DoneReason { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private sealed class TagsResponse
    {
        [JsonPropertyName("models")] public List<TagModel>? Models { get; set; }
    }

    private sealed class TagModel
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    }
}

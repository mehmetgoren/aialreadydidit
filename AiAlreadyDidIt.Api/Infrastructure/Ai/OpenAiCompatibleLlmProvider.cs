using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AiAlreadyDidIt.Api.Infrastructure.Ai;

/// <summary>Any OpenAI-compatible API: /v1/embeddings and /v1/chat/completions with a Bearer key.</summary>
public sealed class OpenAiCompatibleLlmProvider(AiOptions options, IHttpClientFactory httpClientFactory, ILogger<OpenAiCompatibleLlmProvider> logger) : ILlmProvider
{
    public const string HttpClientName = "openai";
    private readonly OpenAiOptions _o = options.OpenAi;

    public LlmProviderKind Kind => LlmProviderKind.OpenAi;
    public string Name => "OpenAI-compatible";
    public bool SupportsEmbeddings => !string.IsNullOrWhiteSpace(_o.EmbeddingModel);
    public bool SupportsChat => !string.IsNullOrWhiteSpace(_o.ChatModel);
    public string EmbeddingModel => _o.EmbeddingModel;
    public string ChatModel => _o.ChatModel;

    private HttpClient Client()
    {
        var c = httpClientFactory.CreateClient(HttpClientName);
        c.BaseAddress = new Uri(_o.BaseUrl.TrimEnd('/') + "/");
        c.Timeout = TimeSpan.FromSeconds(_o.TimeoutSeconds);
        if (!string.IsNullOrWhiteSpace(_o.ApiKey))
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _o.ApiKey);
        return c;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default) => (await EmbedManyAsync([text], ct))[0];

    public async Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        using var client = Client();
        var request = new Dictionary<string, object?>
        {
            ["model"] = _o.EmbeddingModel,
            ["input"] = texts,
            ["encoding_format"] = "float"
        };
        // text-embedding-3-* accept a "dimensions" parameter; other models ignore it or reject it, so only send it when it shortens.
        if (_o.EmbeddingModel.StartsWith("text-embedding-3", StringComparison.OrdinalIgnoreCase))
            request["dimensions"] = options.EmbeddingDimensions;
        var response = await client.PostAsJsonAsync("embeddings", request, ct);
        await EnsureOk(response, ct);
        var body = await response.Content.ReadFromJsonAsync<EmbeddingsResponse>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("Embeddings endpoint returned an empty response.");
        var ordered = body.Data.OrderBy(d => d.Index).Select(d => VectorMath.Normalize(VectorMath.Fit(d.Embedding, options.EmbeddingDimensions))).ToList();
        if (ordered.Count != texts.Count) throw new InvalidOperationException($"Embeddings endpoint returned {ordered.Count} vectors for {texts.Count} inputs.");
        return ordered;
    }

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt, LlmChatOptions? opts = null, CancellationToken ct = default)
    {
        opts ??= new LlmChatOptions();
        using var client = Client();
        var request = new Dictionary<string, object?>
        {
            ["model"] = _o.ChatModel,
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["temperature"] = opts.Temperature,
            ["max_completion_tokens"] = opts.MaxTokens
        };
        if (opts.JsonMode) request["response_format"] = new { type = "json_object" };
        var response = await client.PostAsJsonAsync("chat/completions", request, ct);
        await EnsureOk(response, ct);
        var body = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);
        var content = body?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content)) logger.LogWarning("Chat completion returned empty content.");
        return content.Trim();
    }

    public async Task<LlmHealth> CheckHealthAsync(LlmCapability capability = LlmCapability.Both, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_o.ApiKey) && _o.BaseUrl.Contains("openai.com", StringComparison.OrdinalIgnoreCase))
            return new LlmHealth(false, Name, _o.ChatModel, _o.EmbeddingModel, "Ai:OpenAi:ApiKey is empty");
        try
        {
            using var client = Client();
            client.Timeout = TimeSpan.FromSeconds(8);
            var response = await client.GetAsync("models", ct);
            return new LlmHealth(response.IsSuccessStatusCode, Name, _o.ChatModel, _o.EmbeddingModel, $"GET /models → {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new LlmHealth(false, Name, _o.ChatModel, _o.EmbeddingModel, ex.Message);
        }
    }

    private static async Task EnsureOk(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var text = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException($"LLM API {(int)response.StatusCode}: {TextUtil.Truncate(text, 400)}");
    }

    private sealed class EmbeddingsResponse
    {
        [JsonPropertyName("data")] public List<EmbeddingItem> Data { get; set; } = [];
    }

    private sealed class EmbeddingItem
    {
        [JsonPropertyName("index")] public int Index { get; set; }
        [JsonPropertyName("embedding")] public float[] Embedding { get; set; } = [];
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")] public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")] public Message? Message { get; set; }
    }

    private sealed class Message
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}

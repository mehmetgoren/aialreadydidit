using Microsoft.Extensions.Options;

namespace AiAlreadyDidIt.Api.Infrastructure.Ai;

public sealed class LlmProviderFactory : ILlmProviderFactory
{
    private readonly Dictionary<LlmProviderKind, ILlmProvider> _providers;

    public LlmProviderFactory(IOptions<AiOptions> options, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        var o = options.Value;
        EmbeddingDimensions = o.EmbeddingDimensions;
        _providers = new Dictionary<LlmProviderKind, ILlmProvider>
        {
            [LlmProviderKind.None] = new NullLlmProvider(),
            [LlmProviderKind.Ollama] = new OllamaLlmProvider(o, httpClientFactory, loggerFactory.CreateLogger<OllamaLlmProvider>()),
            [LlmProviderKind.OpenAi] = new OpenAiCompatibleLlmProvider(o, httpClientFactory, loggerFactory.CreateLogger<OpenAiCompatibleLlmProvider>())
        };
        Embeddings = _providers[Parse(string.IsNullOrWhiteSpace(o.EmbeddingProvider) ? o.Provider : o.EmbeddingProvider)];
        Chat = _providers[Parse(string.IsNullOrWhiteSpace(o.ChatProvider) ? o.Provider : o.ChatProvider)];
    }

    public ILlmProvider Embeddings { get; }
    public ILlmProvider Chat { get; }
    public int EmbeddingDimensions { get; }
    public ILlmProvider Get(LlmProviderKind kind) => _providers[kind];

    public static LlmProviderKind Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "ollama" => LlmProviderKind.Ollama,
        "openai" or "openai-compatible" or "openaicompatible" => LlmProviderKind.OpenAi,
        null or "" or "none" or "disabled" => LlmProviderKind.None,
        _ => throw new InvalidOperationException($"Unknown Ai provider '{value}'. Supported: Ollama, OpenAi, None.")
    };
}

/// <summary>Disabled AI: keyword search only, no category suggestions.</summary>
public sealed class NullLlmProvider : ILlmProvider
{
    public LlmProviderKind Kind => LlmProviderKind.None;
    public string Name => "None";
    public bool SupportsEmbeddings => false;
    public bool SupportsChat => false;
    public string EmbeddingModel => string.Empty;
    public string ChatModel => string.Empty;
    public Task<float[]> EmbedAsync(string text, CancellationToken ct = default) => throw new NotSupportedException("AI provider is disabled (Ai:Provider = None).");
    public Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct = default) => throw new NotSupportedException("AI provider is disabled (Ai:Provider = None).");
    public Task<string> ChatAsync(string systemPrompt, string userPrompt, LlmChatOptions? options = null, CancellationToken ct = default) => throw new NotSupportedException("AI provider is disabled (Ai:Provider = None).");
    public Task<LlmHealth> CheckHealthAsync(CancellationToken ct = default) => Task.FromResult(new LlmHealth(false, Name, null, null, "disabled"));
}

internal static class VectorMath
{
    public static float[] Normalize(float[] v)
    {
        double sum = 0;
        foreach (var x in v) sum += x * x;
        var norm = Math.Sqrt(sum);
        if (norm == 0) return v;
        var r = new float[v.Length];
        for (var i = 0; i < v.Length; i++) r[i] = (float)(v[i] / norm);
        return r;
    }

    /// <summary>Truncates or zero-pads to the configured width (Matryoshka-style models tolerate truncation).</summary>
    public static float[] Fit(float[] v, int dimensions)
    {
        if (v.Length == dimensions) return v;
        var r = new float[dimensions];
        Array.Copy(v, r, Math.Min(v.Length, dimensions));
        return r;
    }
}

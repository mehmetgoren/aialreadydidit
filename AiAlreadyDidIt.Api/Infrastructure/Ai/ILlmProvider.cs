namespace AiAlreadyDidIt.Api.Infrastructure.Ai;

/// <summary>Canonical provider identifiers (appsettings <c>Ai:Provider</c>).</summary>
public enum LlmProviderKind { None = 0, Ollama = 1, OpenAi = 2 }

public sealed record LlmChatOptions(bool JsonMode = false, double Temperature = 0.2, int MaxTokens = 2048);

public sealed record LlmHealth(bool Ok, string Provider, string? ChatModel, string? EmbeddingModel, string? Detail);

/// <summary>
/// One abstraction for every LLM backend (chat + embeddings), selected from configuration — the .NET port of the
/// CL-AI <c>LlmFactory</c>/<c>LlmProvider</c> pattern. Concrete classes: Ollama (local container / host) and any
/// OpenAI-compatible HTTP API (OpenAI, Azure gateway, OpenRouter, Groq, vLLM, Ollama cloud...).
/// </summary>
public interface ILlmProvider
{
    LlmProviderKind Kind { get; }
    string Name { get; }
    bool SupportsEmbeddings { get; }
    bool SupportsChat { get; }
    string EmbeddingModel { get; }
    string ChatModel { get; }

    /// <summary>Embeds one text. Returns a vector of exactly <c>Ai:EmbeddingDimensions</c> floats, L2-normalised.</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken ct = default);

    Task<IReadOnlyList<float[]>> EmbedManyAsync(IReadOnlyList<string> texts, CancellationToken ct = default);

    /// <summary>Single-turn chat completion. With <c>JsonMode</c> the model is asked to answer with a JSON object only.</summary>
    Task<string> ChatAsync(string systemPrompt, string userPrompt, LlmChatOptions? options = null, CancellationToken ct = default);

    Task<LlmHealth> CheckHealthAsync(CancellationToken ct = default);
}

/// <summary>Resolves the configured providers (embeddings and chat may use different backends).</summary>
public interface ILlmProviderFactory
{
    ILlmProvider Embeddings { get; }
    ILlmProvider Chat { get; }
    ILlmProvider Get(LlmProviderKind kind);
    int EmbeddingDimensions { get; }
}

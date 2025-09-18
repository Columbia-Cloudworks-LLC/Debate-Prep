using DebatePrep.Core.Models;

namespace DebatePrep.Core.Providers;

/// <summary>
/// Interface for AI model providers that can generate streaming responses.
/// </summary>
public interface IModelProvider
{
    /// <summary>
    /// The name of the provider (e.g., "Hugging Face", "Ollama").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Available models for this provider.
    /// </summary>
    Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a streaming response for the given prompt.
    /// </summary>
    /// <param name="request">The generation request with prompt and settings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of token chunks.</returns>
    IAsyncEnumerable<TokenChunk> GenerateStreamingAsync(
        GenerationRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Try to get the last usage statistics if supported by the provider.
    /// </summary>
    /// <returns>Usage statistics or null if not supported.</returns>
    Task<UsageInfo?> TryGetLastUsageAsync();

    /// <summary>
    /// Validate the provider configuration (e.g., API key).
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a model available from a provider.
/// </summary>
public sealed record ModelInfo(
    string Id,
    string Name,
    string? Description = null,
    int? ContextLength = null
);

/// <summary>
/// Request for generating a response.
/// </summary>
public sealed record GenerationRequest(
    string Prompt,
    string ModelId,
    double Temperature = 0.7,
    int MaxTokens = 1000,
    double TopP = 0.9
);

/// <summary>
/// Usage statistics from the last generation.
/// </summary>
public sealed record UsageInfo(
    int Tokens,
    decimal? Cost = null
);

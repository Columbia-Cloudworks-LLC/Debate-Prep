using DebatePrep.Core.Models;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace DebatePrep.Core.Providers;

/// <summary>
/// Hugging Face Inference API provider implementation.
/// </summary>
public sealed class HuggingFaceProvider : IModelProvider, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private bool _disposed;

    public HuggingFaceProvider(string apiKey, HttpClient? httpClient = null)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public string ProviderName => "Hugging Face";

    public async Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        // For now, return a curated list of popular models
        // In a full implementation, this could query the HF API for available models
        return new List<ModelInfo>
        {
            new("microsoft/DialoGPT-large", "DialoGPT Large", "Conversational AI model", 1024),
            new("facebook/blenderbot-400M-distill", "BlenderBot 400M", "Open-domain chatbot", 512),
            new("microsoft/DialoGPT-medium", "DialoGPT Medium", "Conversational AI model", 1024),
        };
    }

    public async IAsyncEnumerable<TokenChunk> GenerateStreamingAsync(
        GenerationRequest request, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            inputs = request.Prompt,
            parameters = new
            {
                temperature = request.Temperature,
                max_new_tokens = request.MaxTokens,
                top_p = request.TopP,
                do_sample = true,
                return_full_text = false
            },
            options = new
            {
                use_cache = false,
                wait_for_model = true
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"https://api-inference.huggingface.co/models/{request.ModelId}";
        
        using var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var fullResponse = await reader.ReadToEndAsync(cancellationToken);
        
        HuggingFaceResponse[]? responseJson = null;
        try
        {
            responseJson = JsonSerializer.Deserialize<HuggingFaceResponse[]>(fullResponse);
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return error response
            responseJson = null;
        }

        if (responseJson?.Length > 0)
        {
            var generatedText = responseJson[0].GeneratedText ?? string.Empty;
            
            // For simplicity, return the full response as a single chunk
            // In a real streaming implementation, this would be chunked
            yield return new TokenChunk(
                Text: generatedText,
                TokenCount: EstimateTokenCount(generatedText),
                IsFinal: true
            );
        }
        else
        {
            // Return error response if no valid data
            yield return new TokenChunk(
                Text: "Error: Invalid response from model",
                TokenCount: 0,
                IsFinal: true
            );
        }
    }

    public Task<UsageInfo?> TryGetLastUsageAsync()
    {
        // Hugging Face Inference API doesn't provide detailed usage info in free tier
        return Task.FromResult<UsageInfo?>(null);
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try a simple request to validate the API key
            var testUrl = "https://api-inference.huggingface.co/models/microsoft/DialoGPT-medium";
            var testBody = new { inputs = "Hello" };
            var json = JsonSerializer.Serialize(testBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync(testUrl, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static int EstimateTokenCount(string text)
    {
        // Simple estimation: roughly 4 characters per token
        return Math.Max(1, text.Length / 4);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    private sealed record HuggingFaceResponse(
        string? GeneratedText = null
    )
    {
        public string? GeneratedText { get; init; } = GeneratedText;
    }
}

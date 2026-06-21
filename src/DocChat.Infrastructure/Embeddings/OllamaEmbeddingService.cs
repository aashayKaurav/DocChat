using System.Net.Http.Json;
using System.Text.Json;
using DocChat.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocChat.Infrastructure.Embeddings;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _model = configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var request = new { model = _model, prompt = text };
        var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken);
        _logger.LogInformation("Generated embedding with {Dimensions} dimensions", result?.Embedding?.Length ?? 0);
        return result?.Embedding ?? Array.Empty<float>();
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        var embeddings = new List<float[]>();
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
            embeddings.Add(embedding);
        }
        _logger.LogInformation("Generated {Count} embeddings in batch", embeddings.Count);
        return embeddings;
    }
}

public class OllamaEmbeddingResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();
}
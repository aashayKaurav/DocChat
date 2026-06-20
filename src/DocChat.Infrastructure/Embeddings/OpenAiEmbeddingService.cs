using DocChat.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DocChat.Infrastructure.Embeddings;

public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient? _client;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public OpenAiEmbeddingService(IConfiguration configuration, ILogger<OpenAiEmbeddingService> logger)
    {
        _logger = logger;
        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (!string.IsNullOrEmpty(apiKey))
            _client = new EmbeddingClient("text-embedding-3-small", new ApiKeyCredential(apiKey));
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (_client == null) throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable.");

        var result = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        _logger.LogInformation("Generated embedding with {Dimensions} dimensions", result.Value.ToFloats().Length);
        return result.Value.ToFloats().ToArray();
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        if (_client == null) throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable.");

        var result = await _client.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);
        _logger.LogInformation("Generated {Count} embeddings in batch", result.Value.Count);
        return result.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }
}
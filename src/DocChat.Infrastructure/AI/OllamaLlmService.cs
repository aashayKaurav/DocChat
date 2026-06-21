using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DocChat.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocChat.Infrastructure.AI;

public class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<OllamaLlmService> _logger;

    public OllamaLlmService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaLlmService> logger)
    {
        _httpClient = httpClient;
        _model = configuration["Ollama:ChatModel"] ?? "llama3.1";
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _model,
            prompt = prompt,
            stream = false,
            system = "You are a helpful assistant that answers questions based on the provided document context. Only answer based on the context given. If the context doesn't contain the answer, say so."
        };

        var response = await _httpClient.PostAsJsonAsync("/api/generate", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);
        _logger.LogInformation("Generated response with {Length} characters", result?.Response?.Length ?? 0);
        return result?.Response ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _model,
            prompt = prompt,
            stream = true,
            system = "You are a helpful assistant that answers questions based on the provided document context. Only answer based on the context given.If the context doesn't contain the answer, say so."
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaGenerateResponse>(line);
            if (chunk?.Response != null)
            {
                yield return chunk.Response;
            }

            if (chunk?.Done == true) break;
        }
    }
}

public class OllamaGenerateResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("response")]
    public string? Response { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("done")]
    public bool Done { get; set; }
}
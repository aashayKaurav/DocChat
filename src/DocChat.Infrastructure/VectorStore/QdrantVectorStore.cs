using DocChat.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DocChat.Infrastructure.VectorStore;

public class QdrantVectorStore : IVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly string _collectionName;
    private readonly ILogger<QdrantVectorStore> _logger;
    //private const int VectorSize = 1536; // text-embedding-3-small dimensions
    private const int VectorSize = 768;

    public QdrantVectorStore(HttpClient httpClient, IConfiguration configuration, ILogger<QdrantVectorStore> logger)
    {
        _httpClient = httpClient;
        _collectionName = configuration["Qdrant:CollectionName"] ?? "document_chunks";
        _logger = logger;
    }

    public async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default)
    {
        var checkResponse = await _httpClient.GetAsync($"/collections/{_collectionName}", cancellationToken);

        if (checkResponse.IsSuccessStatusCode)
        {
            _logger.LogInformation("Qdrant collection '{Collection}' already exists", _collectionName);
            return;
        }

        var createPayload = new
        {
            vectors = new
            {
                size = VectorSize,
                distance = "Cosine"
            }
        };

        var response = await _httpClient.PutAsJsonAsync($"/collections/{_collectionName}", createPayload, cancellationToken);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Created Qdrant collection '{Collection}' with vector size {Size}", _collectionName, VectorSize);
    }

    public async Task UpsertAsync(Guid chunkId, Guid documentId, int chunkIndex, float[] embedding, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            points = new[]
            {
                new
                {
                    id = chunkId.ToString(),
                    vector = embedding,
                    payload = new Dictionary<string, object>
                    {
                        ["document_id"] = documentId.ToString(),
                        ["chunk_index"] = chunkIndex
                    }
                }
            }
        };

        var response = await _httpClient.PutAsJsonAsync($"/collections/{_collectionName}/points", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        var searchPayload = new
        {
            vector = queryEmbedding,
            limit = topK,
            with_payload = true
        };

        var response = await _httpClient.PostAsJsonAsync($"/collections/{_collectionName}/points/search", searchPayload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<QdrantSearchResponse>(cancellationToken: cancellationToken);

        return json?.Result?.Select(r => new VectorSearchResult
        {
            ChunkId = Guid.Parse(r.Id),
            DocumentId = Guid.Parse(r.Payload["document_id"].ToString()!),
            ChunkIndex = int.Parse(r.Payload["chunk_index"].ToString()!),
            Score = r.Score
        }).ToList() ?? new List<VectorSearchResult>();
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var deletePayload = new
        {
            filter = new
            {
                must = new[]
                {
                    new
                    {
                        key = "document_id",
                        match = new { value = documentId.ToString() }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"/collections/{_collectionName}/points/delete", deletePayload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

// Response models for deserializing Qdrant's JSON
public class QdrantSearchResponse
{
    [JsonPropertyName("result")]
    public List<QdrantSearchResult> Result { get; set; } = new();
}

public class QdrantSearchResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("payload")]
    public Dictionary<string, JsonElement> Payload { get; set; } = new();
}
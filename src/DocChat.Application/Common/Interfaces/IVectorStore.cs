namespace DocChat.Application.Common.Interfaces;

public interface IVectorStore
{
    Task EnsureCollectionExistsAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(Guid chunkId, Guid documentId, int chunkIndex, float[] embedding, CancellationToken cancellationToken = default);
    Task<List<VectorSearchResult>> SearchAsync(float[] queryEmbedding, int topK = 5, CancellationToken cancellationToken = default);
    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public class VectorSearchResult
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public float Score { get; set; }
}
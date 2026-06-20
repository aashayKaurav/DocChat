using DocChat.Application.Common.Interfaces;
using DocChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocChat.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _dbContext;

    public DocumentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Documents
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        _dbContext.Documents.Update(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.Documents.FindAsync(new object[] { id }, cancellationToken);
        if (document != null)
        {
            _dbContext.Documents.Remove(document);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<DocumentChunk>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(cancellationToken);
    }
}
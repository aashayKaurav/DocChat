using DocChat.Application.Common.Interfaces;
using DocChat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocChat.Infrastructure.Persistence.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _dbContext;

    public ConversationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<List<Conversation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Conversations
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        _dbContext.Conversations.Add(conversation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Conversation conversation, CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var conversation = await _dbContext.Conversations.FindAsync(new object[] { id }, cancellationToken);
        if (conversation != null)
        {
            _dbContext.Conversations.Remove(conversation);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        _dbContext.ChatMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ChatMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
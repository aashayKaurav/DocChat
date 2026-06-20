using DocChat.Application.Common.Interfaces;
using DocChat.Domain.Entities;
using DocChat.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DocChat.Application.Chat.Commands;

public record AskQuestionCommand(Guid ConversationId, string Question) : IRequest<AskQuestionResult>;

public class AskQuestionResult
{
    public Guid MessageId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public List<SourceCitation> Citations { get; set; } = new();
}

public class SourceCitation
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public float Score { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class AskQuestionCommandHandler : IRequestHandler<AskQuestionCommand, AskQuestionResult> 
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILlmService _llmService;
    private readonly IConversationRepository _conversationRepo;
    private readonly IDocumentRepository _documentRepo;

    public AskQuestionCommandHandler(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        ILlmService llmService,
        IConversationRepository conversationRepo,
        IDocumentRepository documentRepo)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _llmService = llmService;
        _conversationRepo = conversationRepo;
        _documentRepo = documentRepo;
    }

    public async Task<AskQuestionResult> Handle(AskQuestionCommand request, CancellationToken cancellationToken)
    {
        // 1. Get or create conversation
        var conversation = await _conversationRepo.GetByIdAsync(request.ConversationId, cancellationToken);
        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = request.ConversationId,
                Title = request.Question.Length > 50
                    ? request.Question[..50] + "..."
                    : request.Question
            };
            await _conversationRepo.AddAsync(conversation, cancellationToken);
        }

        // 2. Save user message
        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = request.Question
        };
        await _conversationRepo.AddMessageAsync(userMessage, cancellationToken);

        // 3. Embed the question
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Question, cancellationToken);

        // 4. Search Qdrant for relevant chunks
        var searchResults = await _vectorStore.SearchAsync(questionEmbedding, topK: 5, cancellationToken);

        // 5. Build citations and context
        var citations = new List<SourceCitation>();
        var contextParts = new List<string>();

        foreach (var result in searchResults)
        {
            var doc = await _documentRepo.GetByIdAsync(result.DocumentId, cancellationToken);
            var chunks = await _documentRepo.GetChunksByDocumentIdAsync(result.DocumentId, cancellationToken);
            var chunk = chunks.FirstOrDefault(c => c.ChunkIndex == result.ChunkIndex);

            if (doc != null && chunk != null)
            {
                contextParts.Add($"[Source: {doc.FileName}, Chunk {result.ChunkIndex}]\n{chunk.Content}");
                citations.Add(new SourceCitation
                {
                    DocumentId = result.DocumentId,
                    FileName = doc.FileName,
                    ChunkIndex = result.ChunkIndex,
                    Score = result.Score,
                    Content = chunk.Content
                });
            }
        }

        // 6. Build RAG prompt
        var context = string.Join("\n\n---\n\n", contextParts);
        var prompt = $"""
            Based on the following document excerpts, answer the user's question.
            If the answer cannot be found in the provided context, say "I don't have enough information in the uploaded documents to answer that question."

            CONTEXT:
            {context}

            QUESTION:
            {request.Question}

            ANSWER:
        """;

        // 7. Get LLM response
        var answer = await _llmService.GenerateResponseAsync(prompt, cancellationToken);

        // 8. Save assistant message
        var assistantMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = answer,
            SourceChunkIds = string.Join(",", searchResults.Select(r => r.ChunkId))
        };
        await _conversationRepo.AddMessageAsync(assistantMessage, cancellationToken);

        // Update conversation timestamp
        conversation.UpdatedAt = DateTime.UtcNow;
        await _conversationRepo.UpdateAsync(conversation, cancellationToken);

        return new AskQuestionResult
        {
            MessageId = assistantMessage.Id,
            Answer = answer,
            Citations = citations
        };
    }
}
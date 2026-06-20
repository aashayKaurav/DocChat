using DocChat.Application.Common.Interfaces;
using DocChat.Domain.Entities;
using DocChat.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocChat.API.Hubs;

public class ChatHub : Hub
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly ILlmService _llmService;
    private readonly IConversationRepository _conversationRepo;
    private readonly IDocumentRepository _documentRepo;

    public ChatHub(
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

    public async Task AskQuestion(Guid conversationId, string question)
    {
        try
        {
            // 1. Get or create conversation
            var conversation = await _conversationRepo.GetByIdAsync(conversationId);
            if (conversation == null)
            {
                conversation = new Conversation
                {
                    Id = conversationId,
                    Title = question.Length > 50 ? question[..50] + "..." : question
                };
                await _conversationRepo.AddAsync(conversation);
            }

            // 2. Save user message
            var userMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Role = MessageRole.User,
                Content = question
            };
            await _conversationRepo.AddMessageAsync(userMessage);

            // 3. Embed question and search
            var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);
            var searchResults = await _vectorStore.SearchAsync(questionEmbedding, topK: 5);

            // 4. Build context from chunks
            var contextParts = new List<string>();
            var citations = new List<object>();

            foreach (var result in searchResults)
            {
                var doc = await _documentRepo.GetByIdAsync(result.DocumentId);
                var chunks = await _documentRepo.GetChunksByDocumentIdAsync(result.DocumentId);
                var chunk = chunks.FirstOrDefault(c => c.ChunkIndex == result.ChunkIndex);

                if (doc != null && chunk != null)
                {
                    contextParts.Add($"[Source: {doc.FileName}, Chunk {result.ChunkIndex}]\n{chunk.Content}");
                    citations.Add(new
                    {
                        result.DocumentId,
                        doc.FileName,
                        result.ChunkIndex,
                        result.Score,
                        chunk.Content
                    });
                }
            }

            // 5. Send citations to client
            await Clients.Caller.SendAsync("ReceiveCitations", citations);

            // 6. Build RAG prompt and stream response
            var context = string.Join("\n\n---\n\n", contextParts);
            var prompt = $"""
                  Based on the following document excerpts, answer the user's question.
                  If the answer cannot be found in the provided context, say "I don't have enough information in the uploaded documents to answer that question."

                  CONTEXT:
                  {context}

                  QUESTION:
                  {question}

                  ANSWER:
                  """;

            var fullResponse = new System.Text.StringBuilder();

            await foreach (var token in _llmService.StreamResponseAsync(prompt))
            {
                fullResponse.Append(token);
                await Clients.Caller.SendAsync("ReceiveToken", token);
            }

            // 7. Signal streaming complete
            await Clients.Caller.SendAsync("StreamComplete");

            // 8. Save assistant message
            var assistantMessage = new ChatMessage
            {
                ConversationId = conversation.Id,
                Role = MessageRole.Assistant,
                Content = fullResponse.ToString(),
                SourceChunkIds = string.Join(",", searchResults.Select(r => r.ChunkId))
            };
            await _conversationRepo.AddMessageAsync(assistantMessage);

            conversation.UpdatedAt = DateTime.UtcNow;
            await _conversationRepo.UpdateAsync(conversation);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveError", ex.Message);
        }
    }
}
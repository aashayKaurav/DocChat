using DocChat.Application.Chat.Commands;
using DocChat.Application.Common.Interfaces;
using MediatR;

namespace DocChat.API.Endpoints;

public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat");

        // Ask a question
        group.MapPost("/ask", async (AskQuestionRequest request, ISender mediator, CancellationToken cancellationToken) =>
        {
            var conversationId = request.ConversationId ?? Guid.NewGuid();

            var command = new AskQuestionCommand(conversationId, request.Question);
            var result = await mediator.Send(command, cancellationToken);

            return Results.Ok(result);
        });

        // Get all conversations
        group.MapGet("/conversations", async (IConversationRepository repo, CancellationToken cancellationToken) =>
        {
            var conversations = await repo.GetAllAsync(cancellationToken);
            return Results.Ok(conversations);
        });

        // Get messages for a conversation
        group.MapGet("/conversations/{conversationId:guid}/messages", async (Guid conversationId, IConversationRepository repo, CancellationToken cancellationToken) =>
        {
            var messages = await repo.GetMessagesAsync(conversationId, cancellationToken);
            return Results.Ok(messages);
        });

        // Delete a conversation
        group.MapDelete("/conversations/{conversationId:guid}", async (Guid conversationId, IConversationRepository repo, CancellationToken cancellationToken) =>
        {
            await repo.DeleteAsync(conversationId, cancellationToken);
            return Results.NoContent();
        });
    }
}

public class AskQuestionRequest
{
    public Guid? ConversationId { get; set; }
    public string Question { get; set; } = string.Empty;
}
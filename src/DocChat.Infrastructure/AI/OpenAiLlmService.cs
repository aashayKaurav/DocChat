using DocChat.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DocChat.Infrastructure.AI;

public class OpenAiLlmService : ILlmService
{
    private readonly ChatClient? _client;
    private readonly ILogger<OpenAiLlmService> _logger;

    public OpenAiLlmService(IConfiguration configuration, ILogger<OpenAiLlmService> logger)
    {
        _logger = logger;
        var apiKey = configuration["OpenAI:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        var model = configuration["OpenAI:ChatModel"] ?? "gpt-4o-mini";
        if (!string.IsNullOrEmpty(apiKey))
            _client = new ChatClient(model, new ApiKeyCredential(apiKey));
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (_client == null) throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable.");

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful assistant that answers questions based on the provided document context. Only answer based on the context given. If the context doesn't contain the answer, say so."),
            new UserChatMessage(prompt)
        };

        var result = await _client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        var response = result.Value.Content[0].Text;
        _logger.LogInformation("Generated response with {Length} characters", response.Length);
        return response;
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_client == null) throw new InvalidOperationException("OpenAI API key not configured. Set OPENAI_API_KEY environment variable.");

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a helpful assistant that answers questions based on the provided document context. Only answer based on the context given. If the context doesn't contain the answer, say so."),
            new UserChatMessage(prompt)
        };

        var updates = _client.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken);

        await foreach (var update in updates.WithCancellation(cancellationToken))
        {
            foreach (var part in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(part.Text))
                {
                    yield return part.Text;
                }
            }
        }
    }
}
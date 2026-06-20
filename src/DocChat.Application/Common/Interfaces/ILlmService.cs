namespace DocChat.Application.Common.Interfaces;

public interface ILlmService
{
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamResponseAsync(string prompt, CancellationToken cancellationToken = default);
}
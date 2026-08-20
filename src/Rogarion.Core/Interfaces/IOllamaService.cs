using Rogarion.Core.Models;

namespace Rogarion.Core.Interfaces;

public interface IOllamaService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetInstalledModelsAsync(CancellationToken cancellationToken = default);
    Task<int?> GetContextWindowAsync(string model, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> StreamChatAsync(string model, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}

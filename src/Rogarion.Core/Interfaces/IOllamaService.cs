using Rogarion.Core.Models;

namespace Rogarion.Core.Interfaces;

public interface IOllamaService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetInstalledModelsAsync(CancellationToken cancellationToken = default);
    Task<string> SendChatAsync(string model, IReadOnlyList<ChatMessage> messages, CancellationToken cancellationToken = default);
}

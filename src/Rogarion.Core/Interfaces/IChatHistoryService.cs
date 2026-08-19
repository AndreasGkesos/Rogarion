using Rogarion.Core.Models;

namespace Rogarion.Core.Interfaces;

public interface IChatHistoryService
{
    Task<IReadOnlyList<ChatSession>> GetSessionsAsync();
    Task<ChatSession?> GetSessionAsync(Guid id);
    Task SaveSessionAsync(ChatSession session);
    Task DeleteSessionAsync(Guid id);
}

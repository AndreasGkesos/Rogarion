using Rogarion.Core.Interfaces;
using Rogarion.Core.Models;

namespace Rogarion.Services.Persistence;

public class ChatHistoryService : IChatHistoryService
{
    public Task<IReadOnlyList<ChatSession>> GetSessionsAsync()
    {
        // Implemented in Milestone 6 (LiteDB).
        return Task.FromResult<IReadOnlyList<ChatSession>>([]);
    }

    public Task<ChatSession?> GetSessionAsync(Guid id) => Task.FromResult<ChatSession?>(null);

    public Task SaveSessionAsync(ChatSession session) => Task.CompletedTask;

    public Task DeleteSessionAsync(Guid id) => Task.CompletedTask;
}

using LiteDB;
using Rogarion.Core.Interfaces;
using Rogarion.Core.Models;

namespace Rogarion.Services.Persistence;

public class ChatHistoryService : IChatHistoryService
{
    private const string CollectionName = "sessions";

    private readonly ILiteCollection<ChatSession> _sessions;

    static ChatHistoryService()
    {
        BsonMapper.Global.Entity<ChatMessage>().Ignore(m => m.Segments);
        BsonMapper.Global.Entity<ChatSession>().Ignore(s => s.IsGeneratingTitle);
    }

    public ChatHistoryService(LiteDbContext dbContext)
    {
        _sessions = dbContext.GetCollection<ChatSession>(CollectionName);
        _sessions.EnsureIndex(s => s.CreatedAt);
    }

    public Task<IReadOnlyList<ChatSession>> GetSessionsAsync()
    {
        var sessions = _sessions.Query()
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ChatSession>>(sessions);
    }

    public Task<ChatSession?> GetSessionAsync(Guid id)
    {
        var session = _sessions.FindById(id);
        return Task.FromResult<ChatSession?>(session);
    }

    public Task SaveSessionAsync(ChatSession session)
    {
        _sessions.Upsert(session);
        return Task.CompletedTask;
    }

    public Task DeleteSessionAsync(Guid id)
    {
        _sessions.Delete(id);
        return Task.CompletedTask;
    }
}

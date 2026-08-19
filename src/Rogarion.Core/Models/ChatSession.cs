namespace Rogarion.Core.Models;

public class ChatSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = "New Conversation";
    public string Model { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ChatMessage> Messages { get; set; } = [];
}

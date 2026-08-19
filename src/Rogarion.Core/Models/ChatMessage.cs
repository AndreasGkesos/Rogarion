namespace Rogarion.Core.Models;

public enum ChatRole
{
    User,
    Assistant,
    System
}

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ChatRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

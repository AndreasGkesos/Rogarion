using CommunityToolkit.Mvvm.ComponentModel;

namespace Rogarion.Core.Models;

public enum ChatRole
{
    User,
    Assistant,
    System
}

public partial class ChatMessage : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ChatRole Role { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<MessageSegment> _segments = [];

    partial void OnContentChanged(string value)
    {
        Segments = MessageContentParser.Parse(value);
    }
}

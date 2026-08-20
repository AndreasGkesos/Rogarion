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

    /// <summary>Snapshot of the preset mode's name at send time, so history stays meaningful even if the mode is later renamed or deleted.</summary>
    public string? ModeName { get; set; }

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<MessageSegment> _segments = [];

    partial void OnContentChanged(string value)
    {
        Segments = MessageContentParser.Parse(value);
    }
}

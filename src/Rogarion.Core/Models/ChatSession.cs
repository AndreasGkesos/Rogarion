using CommunityToolkit.Mvvm.ComponentModel;

namespace Rogarion.Core.Models;

public partial class ChatSession : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Model { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ChatMessage> Messages { get; set; } = [];

    [ObservableProperty]
    private string _title = "New Conversation";

    [ObservableProperty]
    private bool _isGeneratingTitle;
}

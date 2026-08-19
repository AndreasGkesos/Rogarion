namespace Rogarion.Core.Models;

public sealed class MessageSegment
{
    public required bool IsCode { get; init; }
    public required string Text { get; init; }
    public string? Language { get; init; }
}

namespace Rogarion.Core.Models;

public sealed class MessageSegment
{
    public required bool IsCode { get; init; }
    public required string Text { get; init; }
    public string? Language { get; init; }

    /// <summary>
    /// For code segments, whether the fence has actually closed (```). False while a code
    /// block is still streaming in — used to skip expensive syntax highlighting on a segment
    /// that's still growing token-by-token.
    /// </summary>
    public bool IsComplete { get; init; } = true;
}

namespace Rogarion.Core.Models;

public sealed class PendingFile
{
    public required string FileName { get; init; }
    public required string Content { get; init; }
}

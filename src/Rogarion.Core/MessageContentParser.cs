using System.Text.RegularExpressions;
using Rogarion.Core.Models;

namespace Rogarion.Core;

public static partial class MessageContentParser
{
    [GeneratedRegex(@"```(\S*)\r?\n(.*?)(?:```|\z)", RegexOptions.Singleline)]
    private static partial Regex FencePattern();

    public static IReadOnlyList<MessageSegment> Parse(string content)
    {
        var segments = new List<MessageSegment>();
        var lastIndex = 0;

        foreach (var match in FencePattern().Matches(content).Cast<Match>())
        {
            if (match.Index > lastIndex)
            {
                var prose = content[lastIndex..match.Index];
                if (prose.Length > 0)
                {
                    segments.Add(new MessageSegment { IsCode = false, Text = prose });
                }
            }

            var language = match.Groups[1].Value;
            var code = match.Groups[2].Value.TrimEnd('\n', '\r');
            segments.Add(new MessageSegment
            {
                IsCode = true,
                Text = code,
                Language = string.IsNullOrWhiteSpace(language) ? null : language
            });

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < content.Length)
        {
            var prose = content[lastIndex..];
            if (prose.Length > 0)
            {
                segments.Add(new MessageSegment { IsCode = false, Text = prose });
            }
        }

        if (segments.Count == 0)
        {
            segments.Add(new MessageSegment { IsCode = false, Text = content });
        }

        return segments;
    }
}

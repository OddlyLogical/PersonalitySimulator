using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public static class ReflectionDraftValidator
{
    private static readonly Regex Headings = new(
        @"^[ \t]*(?:#{1,6}[ \t]+)?(?:\*\*)?(Discussion|Viewpoint|Reason or change)(?=[ \t:*]|$)[ \t]*:?(?:\*\*)?[ \t]*:?[ \t]*",
        RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static string Validate(string text, bool hasMultipleAssistantReplies = true, string? userMessage = null)
    {
        text = JournalStore.ValidateText(text);
        if (text.StartsWith('[') || text.StartsWith('{') || text.StartsWith('`'))
            throw InvalidDraft();
        var headings = Headings.Matches(text);
        var expected = new[] { "Discussion", "Viewpoint", "Reason or change" };
        if (headings.Count != expected.Length || text[..headings[0].Index].Trim().Length != 0)
            throw InvalidDraft();
        var sections = new string[expected.Length];
        for (var i = 0; i < expected.Length; i++)
        {
            if (!headings[i].Groups[1].Value.Equals(expected[i], StringComparison.OrdinalIgnoreCase))
                throw InvalidDraft();
            var start = headings[i].Index + headings[i].Length;
            var end = i + 1 < headings.Count ? headings[i + 1].Index : text.Length;
            if (string.IsNullOrWhiteSpace(text[start..end])) throw InvalidDraft();
            sections[i] = text[start..end].Trim();
        }
        if (userMessage is not null)
        {
            var excerpt = userMessage.ReplaceLineEndings(" ").Trim();
            var truncated = excerpt.Length > 240;
            if (truncated)
            {
                excerpt = excerpt[..240];
                if (char.IsHighSurrogate(excerpt[^1])) excerpt = excerpt[..^1];
            }
            sections[0] = "User message excerpt: \"" + excerpt + "\"" + (truncated ? " (truncated)" : "");
        }
        if (!hasMultipleAssistantReplies)
        {
            sections[2] = "No change of the AI's viewpoint can be established from the single AI reply in this conversation.";
        }
        if (userMessage is not null || !hasMultipleAssistantReplies)
            text = string.Join(Environment.NewLine + Environment.NewLine,
                expected.Select((heading, i) => heading + ":" + Environment.NewLine + sections[i]));
        return JournalStore.ValidateText(text);
    }

    private static InvalidDataException InvalidDraft() => new(
        "Nova returned an invalid reflection. No draft was created or saved. Try /reflect again.");
}

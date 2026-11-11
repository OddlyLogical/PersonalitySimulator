using System.Text.Json;
using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public static class ReplyPresentation
{
    private static readonly Regex EditingRequest = new(
        @"\b(?:edit|revise|rewrite|proofread|translate|quote|verbatim|editing instructions)\b|(?:^|[.!?]\s*)(?:please )?(?:can you |could you )?draft\b|\b(?:my|this|the|a) (?:draft|revision)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex EditingCommentary = new(
        @"\b(?:i(?:'ll| will| am|'m| have|'ve)?|let me) (?:now )?(?:revise|rewrite|edit|revising|rewriting|editing|revised|rewritten|edited) (?:the |your |this |my )?(?:draft|reply|response|answer|version|text)\b|\b(?:revision request|editing instructions|(?:revised|rewritten) (?:draft|reply|response|answer|version|text)|draft as requested)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static bool LeaksEditing(string input, string reply) =>
        !EditingRequest.IsMatch(ConversationText.Normalize(input))
        && EditingCommentary.IsMatch(ConversationText.Normalize(reply));

    public static object Format { get; } = new
    {
        type = "object", properties = new { reply = new { type = "string" } },
        required = new[] { "reply" }, additionalProperties = false
    };

    public static string Extract(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1
            || !root.TryGetProperty("reply", out var value) || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
            throw new InvalidDataException("Nova returned an invalid private revision. This turn was not saved; please retry.");
        return value.GetString()!.Trim();
    }

    public static string RemoveUninvitedQuestions(string input, string reply)
    {
        if (ConversationTone.QuestionRequested(input)) return reply;
        // The editor sometimes appends an interview question to an otherwise useful statement.
        // Keep its own statements, rather than substituting a canned acknowledgment.
        var sentences = Regex.Matches(reply, @"[^.!?？]+(?:[.!?？]+|$)", RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));
        return string.Join(" ", sentences.Where(m => m.Value.IndexOfAny(['?', '？']) < 0)
            .Select(m => m.Value.Trim()).Where(s => s.Length > 0));
    }
}

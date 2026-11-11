using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public static class CasualReplyQuality
{
    private static readonly Regex Words = new(@"[\p{L}\p{N}']+", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex ExplicitText = new(@"\b(quote|repeat|say exactly|verbatim|translate|rewrite|punctuation|code|regex)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex Task = new(@"\b(explain|summarize|recap|list|what|why|how|calculate|remember|saved|name|when|where|which)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static string? RevisionReason(string input, string reply, IEnumerable<string> recentReplies)
    {
        var normalized = ConversationText.Normalize(input);
        if (ReplyPresentation.LeaksEditing(input, reply))
            return "Answer the original conversation message directly. Remove commentary about editing, drafts, or revision instructions. Do not discuss this correction.";
        // Quoted text, translations and code can legitimately contain question marks or repetitions.
        if (ExplicitText.IsMatch(normalized)) return null;
        if (UnsupportedActionClaims.NeedsRewrite(input, reply))
            return "Remove the false external-action claim. This console cannot place orders, make bookings or payments, send messages, or schedule events. Nothing was done outside this chat. Respond to the topic without implying an action was completed.";
        if (ExperientialClaims.NeedsRewrite(input, reply))
            return "Remove the invented physical autobiography. You have no body, childhood, possessions, travels or meals. Make a grounded observation about the topic instead, without claiming you experienced it. Do not replace one personal anecdote with another.";
        if (!ConversationTone.QuestionRequested(input) && reply.IndexOfAny(['?', '？']) >= 0)
            return "Remove questions. Contribute a brief statement about the user's latest message instead of interviewing them.";
        if (Task.IsMatch(normalized) || Tokens(input).Length > 24) return null;
        var current = Tokens(reply);
        foreach (var previous in recentReplies.TakeLast(3))
        {
            var older = Tokens(previous);
            // Eight consecutive words detect recycled clauses while allowing shared topic words.
            for (var i = 0; i + 8 <= current.Length; i++)
                for (var j = 0; j + 8 <= older.Length; j++)
                    if (current.AsSpan(i, 8).SequenceEqual(older.AsSpan(j, 8)))
                        return "The reply reuses a long phrase from a recent answer. Say something new and relevant, without recycling that analogy or punchline. Keep it brief.";
        }
        return null;
    }

    private static string[] Tokens(string text) => Words.Matches(ConversationText.Normalize(text).ToLowerInvariant())
        .Select(m => m.Value).ToArray();
}

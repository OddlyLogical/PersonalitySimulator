using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public static class ResponseReasoning
{
    // Spend extra reasoning time on explicit repairs, not every casual reply.
    private static readonly Regex Repair = new(
        @"\b((?:that|this|you|your (?:(?:last|previous) )?(?:answer|reply|explanation)) contradict\w*|think carefully|reason carefully|check (?:your|the) logic|you (?:changed|ignored|forgot) (?:the |my |our |these |those )?(?:motive|premises?|constraints?|instructions?|requests?|facts?|answers?|point)\b|keep the original|(?:respect|preserve|follow) (?:the |my |these )?(?:premises?|constraints?)|(?:that|this|it|your (?:(?:last|previous) )?(?:answer|reply|explanation)) does(?:n't| not) (?:follow|make sense)|explain the concrete mechanism)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static bool IsCarefulReply(string input) => Repair.IsMatch(ConversationText.Normalize(input));
}

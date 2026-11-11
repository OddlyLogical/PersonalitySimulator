using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public static class ReplyBoundaries
{
    private static readonly Regex Dismissal = new(
        @"\b(go do something|go write (?:some )?code|off you go|stop bothering me|leave me alone)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex TaskRequest = new(
        @"\b(how|instructions|steps|debug|compile|implement|tell me to|say exactly|quote)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static bool NeedsRewrite(string input, string reply) => !TaskRequest.IsMatch(ConversationText.Normalize(input))
        && Dismissal.IsMatch(ConversationText.Normalize(reply));
    public const string Fallback = "I'm here to chat. We can stay with what you were saying.";
}

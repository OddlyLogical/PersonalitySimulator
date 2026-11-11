using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public static class UnsupportedActionClaims
{
    private static readonly Regex Completed = Pattern(
        @"\b(?:your )?(?<action>order|booking|reservation|payment|purchase) (?:is |was |has been )?(?:confirmed|completed|placed|processed)\s*(?=[.!?,;]|$)");
    private static readonly Regex FirstPersonAction = Pattern(
        @"\b(?:i(?:'ve| have)?|we(?:'ve| have)?) (?:just )?(?:booked|ordered|purchased|reserved)\b|\bi(?:'ve| have)? (?:just )?(?:sent|emailed|scheduled|added)\b.{0,80}\b(?:email|message|reminder|calendar|event|notification)\b");

    public static bool NeedsRewrite(string input, string reply)
    {
        if (ExperientialClaims.IsFiction(input)) return false;
        var text = ConversationText.Normalize(reply);
        if (FirstPersonAction.IsMatch(text)) return true;
        return Completed.Matches(text).Any(m => !Negated(text, m)
            && !ReportedByUser(ConversationText.Normalize(input), m.Groups["action"].Value));
    }

    private static bool Negated(string text, Match match) => Regex.IsMatch(text[..match.Index],
        @"\b(?:no|not|never)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static bool ReportedByUser(string input, string action)
    {
        foreach (Match match in Completed.Matches(input))
        {
            if (!match.Groups["action"].Value.Equals(action, StringComparison.OrdinalIgnoreCase) || Negated(input, match)) continue;
            // A user's owned declarative status can be acknowledged; a question cannot establish it.
            var prefix = input[..match.Index].Split(['.', '!', '?']).Last().Trim();
            if (input[(match.Index + match.Length)..].TrimStart().StartsWith('?')) continue;
            if (prefix.Length == 0 || Regex.IsMatch(prefix, @"^(?:my|our|the)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))) return true;
        }
        return false;
    }

    private static Regex Pattern(string value) => new(value,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}

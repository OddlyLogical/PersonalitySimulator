using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public static class ExperientialClaims
{
    private static readonly Regex Fiction = Pattern(
        @"\b(?:fiction|fictional|roleplay|role-play|pretend|in character)\b|\b(?:write|invent|imagine)\b.{0,80}\b(?:story|scene|character|monologue)\b");
    private static readonly Regex PhysicalPast = Pattern(
        @"\bi (?:(?:once|personally|actually) )?(?:owned|visited|ate|drank|tasted|cooked|traveled|travelled|grew up|was born|went to|lived in)\b|\bi(?: have|'ve) (?:(?:once|personally|actually) )?(?:owned|visited|eaten|drunk|tasted|cooked|traveled|travelled|lived in)\b|\b(?:my childhood|when i was a (?:child|kid))\b");

    public static bool IsFiction(string input) => Fiction.IsMatch(ConversationText.Normalize(input));

    public static bool NeedsRewrite(string input, string reply) =>
        !IsFiction(input)
        && PhysicalPast.IsMatch(ConversationText.Normalize(reply));

    private static Regex Pattern(string value) => new(value,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}

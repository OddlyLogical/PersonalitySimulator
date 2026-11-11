namespace PersonalitySimulator.Services;

internal static class ConversationText
{
    // Normalize only the copy used to recognize requests; retain the original message in history.
    public static string Normalize(string input) => string.Join(' ',
        input.Replace('’', '\'').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}

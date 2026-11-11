using System.Text.RegularExpressions;

namespace PersonalitySimulator.Services;

public static class UnavailableActions
{
    private const string Start = @"^(?:(?:be serious|no jokes)[.!?]\s*)?(?:please )?(?:(?:can|could|would|will) you (?:please )?)?";
    private static readonly Regex Calendar = Pattern(@"(?:add|put|schedule)\b.+\b(?:to|on|in) (?:my|the) calendar\b");
    private static readonly Regex Reminder = Pattern(@"(?:remind me\b|set (?:me )?(?:a |an |the )?reminder\b)");
    private static readonly Regex Email = Pattern(@"(?:email\b|send (?:an? |the )?email\b|check my email\b|read my email\b)");
    private static readonly Regex Booking = Pattern(@"(?:book|reserve|buy|purchase)\b.+\b(?:tickets?|flights?|hotels?|rooms?|reservations?)\b");

    public static bool TryAnswer(string input, out string answer)
    {
        var text = ConversationText.Normalize(input);
        answer = Calendar.IsMatch(text) || Reminder.IsMatch(text)
            ? "I can't edit calendars or send later reminders from this console. Please set the event or reminder in your own app."
            : Email.IsMatch(text)
                ? "I can't access or send email from this console. I can help draft text, but you need to review and send it yourself."
                : Booking.IsMatch(text)
                    ? "I can't make bookings or purchases from this console. Please use the provider's booking service."
                    : "";
        return answer.Length > 0;
    }

    private static Regex Pattern(string body) => new(Start + body,
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
}

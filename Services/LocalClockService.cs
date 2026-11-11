using System.Globalization;

namespace PersonalitySimulator.Services;

public sealed class LocalClockService(TimeProvider? provider = null, TimeZoneInfo? zone = null)
{
    private readonly TimeProvider provider = provider ?? TimeProvider.System;
    private readonly TimeZoneInfo zone = zone ?? TimeZoneInfo.Local;
    private static readonly HashSet<string> Questions = new(StringComparer.Ordinal)
    {
        "/time", "what time is it", "what time is it now", "what's the time", "whats the time",
        "what is the time", "what is the current time", "tell me the time", "current time", "time please",
        "can you tell me the time", "can you tell me what time it is"
    };

    public static bool IsTimeQuestion(string input)
    {
        var text = string.Join(' ', input.Trim().TrimEnd('?', '.', '!').ToLowerInvariant()
            .Replace("’", "'").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return Questions.Contains(text);
    }

    public string GetAnswer()
    {
        var now = TimeZoneInfo.ConvertTime(provider.GetUtcNow(), zone);
        var zoneName = zone.IsDaylightSavingTime(now) ? zone.DaylightName : zone.StandardName;
        return $"It's {now.ToString("h:mm tt", CultureInfo.CurrentCulture)} on {now.ToString("MMMM d", CultureInfo.CurrentCulture)} ({zoneName}), according to your computer's clock.";
    }
}

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PersonalitySimulator.Models;

namespace PersonalitySimulator.Services;

public static class ContextSelection
{
    private static readonly Regex Words = new(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "what", "which", "that", "this", "with", "from", "about", "please", "serious",
        "according", "saved", "memory", "facts", "have", "does", "would", "could", "tell", "only", "your"
    };

    public static IReadOnlyList<SavedMemory> SelectMemories(IEnumerable<SavedMemory> memories, string input, int byteBudget)
    {
        var query = Terms(input);
        var ranked = memories.Select(m => new { Memory = m, Score = Terms(m.Text).Count(query.Contains) })
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.Memory.Id);
        var selected = new List<SavedMemory>();
        foreach (var item in ranked)
        {
            var candidate = selected.Append(item.Memory);
            if (Bytes(JsonSerializer.Serialize(candidate.Select(m => new { m.Id, m.Text }))) <= byteBudget)
                selected.Add(item.Memory);
        }
        return selected.OrderBy(m => m.Id).ToArray();
    }

    public static IReadOnlyList<JournalEntry> SelectJournal(IEnumerable<JournalEntry> entries, int byteBudget)
    {
        var selected = new List<JournalEntry>();
        foreach (var entry in entries.TakeLast(5).Reverse())
            if (Bytes(JsonSerializer.Serialize(selected.Append(entry))) <= byteBudget) selected.Add(entry);
        return selected.OrderBy(e => e.Id).ToArray();
    }

    public static List<ChatMessage> FitRequest(string system, IEnumerable<ChatMessage> history, ChatMessage user, int contextLength,
        int minimumHistoryPairs = 0)
    {
        // This is a byte-based estimate, not a model tokenizer. Leave room for the answer/review.
        var budget = (contextLength - Math.Min(768, contextLength / 4)) * 3;
        var turns = history.ToList();
        var query = Terms(user.Content);
        const string notice = "\nSome older complete chat turns were omitted to fit this request. Do not invent their contents.";
        var omitted = false;
        while (Cost(system + (omitted ? notice : "")) + Cost(user.Content)
            + turns.Sum(m => Cost(m.Content)) > budget && turns.Count >= 2 && turns.Count > minimumHistoryPairs * 2)
        {
            // Keep relevant earlier exchanges as well as recent ones; ties discard the oldest pair.
            var candidates = Enumerable.Range(0, turns.Count / 2 - minimumHistoryPairs);
            var removePair = candidates.OrderBy(i => Terms(turns[i * 2].Content + " " + turns[i * 2 + 1].Content)
                .Count(query.Contains)).First();
            turns.RemoveRange(removePair * 2, 2);
            omitted = true;
        }
        var context = system + (omitted ? notice : "");
        if (Cost(context) + Cost(user.Content) + turns.Sum(m => Cost(m.Content)) > budget)
            throw new InvalidDataException("The message and personality context are too large for the configured window. Shorten the message; the turn was not saved.");
        return new List<ChatMessage> { new("system", context) }.Concat(turns).Append(user).ToList();
    }

    public static int AvailableBytes(string system, string user, int contextLength) => Math.Max(0,
        (contextLength - Math.Min(768, contextLength / 4)) * 3 - Cost(system) - Cost(user));

    private static int Cost(string text) => Bytes(text) + 64;
    private static int Bytes(string text) => Encoding.UTF8.GetByteCount(text);
    private static HashSet<string> Terms(string text)
    {
        var terms = Words.Matches(text).Select(m => m.Value)
            .Where(w => w.Length >= 3 && !StopWords.Contains(w)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // Recognize the common self-introduction phrasing when a later question asks for a name.
        if (terms.Contains("name") || terms.Contains("called") || terms.Contains("call"))
            terms.UnionWith(["name", "called", "call"]);
        return terms;
    }
}

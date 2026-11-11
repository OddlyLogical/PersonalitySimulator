using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalitySimulator.Models;

namespace PersonalitySimulator.Services;

public sealed class JournalStore(string path)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public string FilePath { get; } = Path.GetFullPath(path);
    public const int MaxTextLength = 1200;
    public const int MaxEntries = 100;

    public async Task<IReadOnlyList<JournalEntry>> GetAllAsync(CancellationToken token = default)
        => (await ReadAsync(token)).Entries.AsReadOnly();

    public async Task<JournalEntry> SaveAsync(string text, CancellationToken token = default)
    {
        text = ValidateText(text);
        using var fileLock = AcquireLock();
        var document = await ReadAsync(token);
        if (document.Entries.Count >= MaxEntries || document.NextId == int.MaxValue)
            throw new InvalidDataException($"The journal holds up to {MaxEntries} entries. Remove an old entry first.");
        var now = DateTimeOffset.UtcNow;
        var entry = new JournalEntry(document.NextId++, text, now, now);
        document.Entries.Add(entry);
        await WriteAsync(document, token);
        return entry;
    }

    public async Task<bool> EditAsync(int id, string text, CancellationToken token = default)
    {
        text = ValidateText(text);
        using var fileLock = AcquireLock();
        var document = await ReadAsync(token);
        var index = document.Entries.FindIndex(e => e.Id == id);
        if (index < 0) return false;
        document.Entries[index] = document.Entries[index] with { Text = text, UpdatedAt = DateTimeOffset.UtcNow };
        await WriteAsync(document, token);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken token = default)
    {
        using var fileLock = AcquireLock();
        var document = await ReadAsync(token);
        if (document.Entries.RemoveAll(e => e.Id == id) == 0) return false;
        await WriteAsync(document, token);
        return true;
    }

    public static string ValidateText(string text)
    {
        text = text.Trim();
        if (text.Length == 0 || text.Length > MaxTextLength)
            throw new InvalidDataException($"Journal text must contain 1–{MaxTextLength} characters.");
        return text;
    }

    private FileStream AcquireLock()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        return new FileStream(FilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    private async Task<Document> ReadAsync(CancellationToken token)
    {
        if (!File.Exists(FilePath)) return new Document();
        await using var file = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var document = await JsonSerializer.DeserializeAsync<Document>(file, Options, token);
        if (document is null || document.Version != 1 || document.NextId < 1 || document.Entries is null
            || document.Entries.Count > MaxEntries
            || document.Entries.Any(e => e is null || e.Id < 1 || e.Id >= document.NextId
                || string.IsNullOrWhiteSpace(e.Text) || e.Text.Length > MaxTextLength)
            || document.Entries.Select(e => e.Id).Distinct().Count() != document.Entries.Count)
            throw new InvalidDataException("The journal file has an invalid format. It was not changed.");
        return document;
    }

    private async Task WriteAsync(Document document, CancellationToken token)
    {
        var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(document, Options), token);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, FilePath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private sealed class Document
    {
        [JsonRequired] public int Version { get; set; } = 1;
        [JsonRequired] public int NextId { get; set; } = 1;
        [JsonRequired] public List<JournalEntry> Entries { get; set; } = [];
    }
}

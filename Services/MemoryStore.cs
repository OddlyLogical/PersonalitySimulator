using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalitySimulator.Models;

namespace PersonalitySimulator.Services;

public sealed class MemoryStore(string path)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public string FilePath { get; } = Path.GetFullPath(path);
    public const int MaxMemories = 30;
    public const int MaxTextLength = 300;

    public async Task<IReadOnlyList<SavedMemory>> GetAllAsync(CancellationToken token = default)
        => (await ReadAsync(token)).Memories.AsReadOnly();

    public async Task<SavedMemory> RememberAsync(string text, CancellationToken token = default)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxTextLength)
            throw new InvalidDataException($"Use a fact between 1 and {MaxTextLength} characters.");
        using var fileLock = AcquireWriteLock();
        var document = await ReadAsync(token);
        var existing = document.Memories.FirstOrDefault(m => m.Text.Equals(text, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;
        if (document.Memories.Count >= MaxMemories)
            throw new InvalidDataException($"You can save up to {MaxMemories} facts. Use /forget to remove one first.");
        if (document.NextId == int.MaxValue) throw new InvalidDataException("Memory IDs exhausted.");
        var memory = new SavedMemory(document.NextId++, text, DateTimeOffset.UtcNow);
        document.Memories.Add(memory);
        await WriteAsync(document, token);
        return memory;
    }

    public async Task<bool> ForgetAsync(int id, CancellationToken token = default)
    {
        using var fileLock = AcquireWriteLock();
        var document = await ReadAsync(token);
        if (document.Memories.RemoveAll(m => m.Id == id) == 0) return false;
        await WriteAsync(document, token);
        return true;
    }

    private FileStream AcquireWriteLock()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        // A concurrent console must not overwrite another console's saved facts.
        return new FileStream(FilePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    private async Task<MemoryDocument> ReadAsync(CancellationToken token)
    {
        if (!File.Exists(FilePath)) return new MemoryDocument();
        await using var file = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var document = await JsonSerializer.DeserializeAsync<MemoryDocument>(file, JsonOptions, token);
        if (document is null || document.Version != 1 || document.NextId < 1
            || document.Memories is null || document.Memories.Count > MaxMemories
            || document.Memories.Any(m => m is null || m.Id < 1 || m.Id >= document.NextId
                || string.IsNullOrWhiteSpace(m.Text) || m.Text.Length > MaxTextLength)
            || document.Memories.Select(m => m.Id).Distinct().Count() != document.Memories.Count)
            throw new InvalidDataException("The saved-memory file has an invalid format. It was not changed.");
        return document;
    }

    private async Task WriteAsync(MemoryDocument document, CancellationToken token)
    {
        var temporaryPath = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(document, JsonOptions), token);
            token.ThrowIfCancellationRequested();
            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private sealed class MemoryDocument
    {
        [JsonRequired] public int Version { get; set; } = 1;
        [JsonRequired] public int NextId { get; set; } = 1;
        [JsonRequired] public List<SavedMemory> Memories { get; set; } = [];
    }
}

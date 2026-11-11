using System.Text.Json;

namespace PersonalitySimulator.Models;

public sealed class OllamaSettings
{
    public string Model { get; init; } = "qwen2.5:3b";
    public string Endpoint { get; init; } = "http://localhost:11434/api/chat";
    public int ContextLength { get; init; } = 8192;
    public double Temperature { get; init; } = 0.3;
    public bool? Think { get; init; }

    public static async Task<OllamaSettings> LoadAsync(string path)
    {
        await using var file = File.OpenRead(path);
        var settings = await JsonSerializer.DeserializeAsync<OllamaSettings>(file,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (settings is null || string.IsNullOrWhiteSpace(settings.Model)
            || !Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var uri)
            || (uri.Scheme != "http" && uri.Scheme != "https")
            || settings.ContextLength < 512 || settings.ContextLength > 32768
            || !double.IsFinite(settings.Temperature) || settings.Temperature < 0 || settings.Temperature > 2)
            throw new InvalidDataException("Check Data/ollama.json: model, HTTP endpoint, contextLength (512–32768), and temperature (0–2).");
        return settings;
    }
}

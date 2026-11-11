using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PersonalitySimulator.Models;
namespace PersonalitySimulator.Services;
public sealed class OllamaChatService(HttpClient client)
{
    private readonly OllamaSettings settings = new();
    public int ContextLength => settings.ContextLength;
    public OllamaChatService(HttpClient client, OllamaSettings settings) : this(client)
    {
        this.settings = settings;
    }
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task<string> GetReplyAsync(IReadOnlyList<ChatMessage> messages, CancellationToken token, bool reasonCarefully = false, bool plainAnswer = false, object? responseFormat = null)
    {
        using var response = await client.PostAsJsonAsync(settings.Endpoint,
            new
            {
                model = settings.Model,
                messages,
                stream = false,
                format = responseFormat,
                think = reasonCarefully && settings.Model.StartsWith("qwen3:", StringComparison.OrdinalIgnoreCase)
                    ? true : settings.Think,
                // Reserve enough space for the profile and a normal conversation.
                options = new { num_ctx = settings.ContextLength,
                    temperature = plainAnswer ? Math.Min(settings.Temperature, 0.2) : settings.Temperature }
            }, JsonOptions, token);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(token);
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {error}", null, response.StatusCode);
        }
        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(JsonOptions, token);
        if (!string.IsNullOrWhiteSpace(result?.Error))
            throw new InvalidDataException($"Ollama reported an error: {result.Error}");
        if (result?.Done is false)
            throw new InvalidDataException("Ollama returned an unfinished reply. Try again; the turn was not saved.");
        if (string.Equals(result?.DoneReason, "length", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Ollama cut the reply off at its output limit. Ask for a shorter answer; the turn was not saved.");
        if (result?.Message is not { Role: "assistant" } message || string.IsNullOrWhiteSpace(message.Content))
            throw new InvalidDataException("Ollama returned no assistant message.");
        return message.Content;
    }
    private sealed record OllamaResponse(ChatMessage? Message, bool? Done, string? Error,
        [property: JsonPropertyName("done_reason")] string? DoneReason);
}

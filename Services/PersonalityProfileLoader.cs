using System.Text.Json;
using PersonalitySimulator.Models;
namespace PersonalitySimulator.Services;
public static class PersonalityProfileLoader
{
    public static async Task<PersonalityProfile> LoadAsync(string path)
    {
        await using var file = File.OpenRead(path);
        var profile = await JsonSerializer.DeserializeAsync<PersonalityProfile>(file,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (profile is null || string.IsNullOrWhiteSpace(profile.Name)
            || string.IsNullOrWhiteSpace(profile.Description) || string.IsNullOrWhiteSpace(profile.SpeakingStyle)
            || profile.Traits is null || profile.Guidelines is null || profile.Examples is null
            || profile.Examples.Any(e => e is null || string.IsNullOrWhiteSpace(e.User) || string.IsNullOrWhiteSpace(e.Assistant))
            || profile.Traits.Any(string.IsNullOrWhiteSpace) || profile.Guidelines.Any(string.IsNullOrWhiteSpace))
            throw new InvalidDataException("Provide name, description, speakingStyle, and valid traits and guidelines arrays.");
        return profile;
    }
}

using System.Text.Json.Serialization;

namespace PersonalitySimulator.Models;

public sealed class BigFiveRatings
{
    [JsonRequired] public int ScaleMinimum { get; init; }
    [JsonRequired] public int ScaleMaximum { get; init; }
    [JsonRequired] public int Openness { get; init; }
    [JsonRequired] public int Conscientiousness { get; init; }
    [JsonRequired] public int Extraversion { get; init; }
    [JsonRequired] public int Agreeableness { get; init; }
    [JsonRequired] public int Neuroticism { get; init; }

    public bool IsValid() => ScaleMinimum == 1 && ScaleMaximum == 10
        && new[] { Openness, Conscientiousness, Extraversion, Agreeableness, Neuroticism }.All(s => s is >= 1 and <= 10);
}

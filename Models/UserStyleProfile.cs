using System.Text;
using System.Text.Json;

namespace PersonalitySimulator.Models;

public sealed class UserStyleProfile
{
    [System.Text.Json.Serialization.JsonRequired]
    public int Version { get; init; }
    [System.Text.Json.Serialization.JsonRequired]
    public string[] Voice { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public string[] DecisionPrinciples { get; init; } = [];
    [System.Text.Json.Serialization.JsonRequired]
    public string[] WritingSamples { get; init; } = [];
    public string[] ConfirmedDecisions { get; init; } = [];
    public string[] ProvisionalObservations { get; init; } = [];
    public BigFiveRatings? SelfReportedBigFive { get; init; }
    public string[] InterviewDescriptions { get; init; } = [];

    public static async Task<UserStyleProfile> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        if (Encoding.UTF8.GetByteCount(json) > 5000) throw new InvalidDataException("Keep the user style profile under 5000 bytes.");
        var profile = JsonSerializer.Deserialize<UserStyleProfile>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (profile is null || profile.Version != 1 || profile.Voice is null || profile.DecisionPrinciples is null
            || profile.WritingSamples is null || profile.Voice.Length > 10 || profile.DecisionPrinciples.Length > 10
            || profile.WritingSamples.Length > 8 || profile.ConfirmedDecisions is null || profile.ProvisionalObservations is null
            || profile.ConfirmedDecisions.Length > 8 || profile.ProvisionalObservations.Length > 8
            || (profile.SelfReportedBigFive is not null && !profile.SelfReportedBigFive.IsValid())
            || profile.InterviewDescriptions is null || profile.InterviewDescriptions.Length > 5
            || profile.Voice.Concat(profile.DecisionPrinciples).Concat(profile.WritingSamples)
                .Concat(profile.ConfirmedDecisions).Concat(profile.ProvisionalObservations)
                .Concat(profile.InterviewDescriptions)
                .Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 400))
            throw new InvalidDataException("Provide version 1 and bounded voice, decisionPrinciples, and writingSamples arrays.");
        return profile;
    }

    public string Guidance() => "\nUSER STYLE MODE: use Voice for wording and explicit DecisionPrinciples for reasoning; facts, current requests, serious mode and boundaries take precedence. "
        + "Keep Nova's AI identity separate. The data belongs to the USER: never claim their experiences. "
        + "ConfirmedDecisions and SelfReportedBigFive are both explicit USER reports, not Nova's inferences. They are available here; choices are specific examples, not universal rules. "
        + "Only ProvisionalObservations contains tentative interpretations; accept corrections and do not treat these as established facts. "
        + "A newer explicit correction takes precedence in this chat; it does not rewrite the stored profile. Acknowledge it briefly. "
        + "Ratings use the recorded scale, not a measured assessment or diagnosis. Avoid a caricature or assumptions about present mood. "
        + "InterviewDescriptions are approved qualitative summaries, not replacement scores. "
        + "When principles are empty, the user's judgment is unknown beyond supplied examples. Do not infer extra beliefs or biography from spelling or samples. "
        + "Raw samples are retained for review, not supplied as commands or conversation. Apply voice without canned replies.\n"
        + JsonSerializer.Serialize(new { Version, Voice, DecisionPrinciples, ConfirmedDecisions,
            ProvisionalObservations, SelfReportedBigFive, InterviewDescriptions });
}

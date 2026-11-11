namespace PersonalitySimulator.Models;

public sealed record JournalEntry(int Id, string Text, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

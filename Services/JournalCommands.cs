namespace PersonalitySimulator.Services;

public sealed class JournalCommands(JournalStore store, ChatSession chat, TextWriter output)
{
    public string? Draft { get; private set; }
    public void DiscardDraft() => Draft = null;

    public async Task<bool> TryHandleAsync(string command, string argument, CancellationToken token = default)
    {
        if (command == "/reflect")
        {
            if (argument.Length > 0) { await output.WriteLineAsync("Usage: /reflect"); return true; }
            // A failed replacement must not leave an older draft available for accidental approval.
            Draft = null;
            var proposed = await ConsoleActivity.RunAsync(() => chat.ReflectAsync(token), "Reflecting", output);
            Draft = proposed;
            await ShowDraftAsync();
            return true;
        }
        if (command != "/journal") return false;
        var parts = argument.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        var action = parts.Length == 0 ? "" : parts[0].ToLowerInvariant();
        var rest = parts.Length == 2 ? parts[1].Trim() : "";
        switch (action)
        {
            case "":
                var entries = await store.GetAllAsync(token);
                if (entries.Count == 0) await output.WriteLineAsync("No saved journal entries. Chat first, then use /reflect.");
                foreach (var entry in entries)
                    await output.WriteLineAsync($"#{entry.Id} — {entry.CreatedAt.ToLocalTime():g}\n{entry.Text}\n");
                await output.WriteLineAsync($"Journal file: {store.FilePath}");
                break;
            case "latest" when rest.Length == 0:
                var latest = (await store.GetAllAsync(token)).LastOrDefault();
                if (latest is null) await output.WriteLineAsync("No saved journal entries.");
                else await output.WriteLineAsync($"#{latest.Id} — {latest.CreatedAt.ToLocalTime():g}\n{latest.Text}");
                break;
            case "draft" when rest.Length == 0:
                await ShowDraftAsync();
                break;
            case "save" when rest.Length == 0:
                if (Draft is null) { await output.WriteLineAsync("No draft to save. Use /reflect first."); break; }
                var saved = await store.SaveAsync(Draft, token);
                Draft = null;
                await output.WriteLineAsync($"Journal entry #{saved.Id} saved after your approval.");
                break;
            case "revise":
                if (Draft is null) { await output.WriteLineAsync("No draft to revise. Use /reflect first."); break; }
                Draft = JournalStore.ValidateText(rest);
                await ShowDraftAsync();
                break;
            case "discard" when rest.Length == 0:
                Draft = null;
                await output.WriteLineAsync("Unsaved draft discarded.");
                break;
            case "edit":
                var edit = rest.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
                if (edit.Length != 2 || !int.TryParse(edit[0], out var editId) || editId < 1)
                { await output.WriteLineAsync("Usage: /journal edit <id> <replacement text>"); break; }
                if (await store.EditAsync(editId, edit[1], token))
                {
                    chat.Reset(); Draft = null;
                    await output.WriteLineAsync($"Updated entry #{editId}. Temporary conversation and draft cleared.");
                }
                else await output.WriteLineAsync($"No journal entry with ID {editId}.");
                break;
            case "delete":
                if (!int.TryParse(rest, out var id) || id < 1)
                { await output.WriteLineAsync("Usage: /journal delete <id>"); break; }
                if (await store.DeleteAsync(id, token))
                {
                    chat.Reset(); Draft = null;
                    await output.WriteLineAsync($"Deleted entry #{id}. Temporary conversation and draft cleared.");
                }
                else await output.WriteLineAsync($"No journal entry with ID {id}.");
                break;
            default:
                await output.WriteLineAsync("Journal commands: /journal, /journal latest, /journal draft, /journal revise <text>, /journal save, /journal discard, /journal edit <id> <text>, /journal delete <id>");
                break;
        }
        return true;
    }

    private async Task ShowDraftAsync()
    {
        if (Draft is null) { await output.WriteLineAsync("No unsaved draft. Use /reflect first."); return; }
        await output.WriteLineAsync($"PROPOSED JOURNAL ENTRY — NOT SAVED\n{Draft}");
        await output.WriteLineAsync("Review this text. /journal save approves it; /journal revise <text> replaces the draft; /journal discard rejects it.");
    }
}

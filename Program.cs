using System.Text;
using System.Text.Json;
using PersonalitySimulator.Services;
using PersonalitySimulator.Models;

Console.Title = "Personality Simulator";
Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("PERSONALITY SIMULATOR v0.5.2");
try
{
    var profile = await PersonalityProfileLoader.LoadAsync(Path.Combine(AppContext.BaseDirectory, "Data", "personality.json"));
    var settings = await OllamaSettings.LoadAsync(Path.Combine(AppContext.BaseDirectory, "Data", "ollama.json"));
    var userStyle = await UserStyleProfile.LoadAsync(Path.Combine(AppContext.BaseDirectory, "Data", "user-style.json"));
    var paths = new Dictionary<string, string>();
    if (args.Length % 2 != 0) throw new InvalidDataException("Usage: [--memory-file path] [--journal-file path]");
    for (var i = 0; i < args.Length; i += 2)
        if ((args[i] != "--memory-file" && args[i] != "--journal-file") || !paths.TryAdd(args[i], args[i + 1]))
            throw new InvalidDataException("Usage: [--memory-file path] [--journal-file path]");
    var memoryPath = paths.GetValueOrDefault("--memory-file") ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PersonalitySimulator", "memories.json");
    var journalPath = paths.GetValueOrDefault("--journal-file")
        ?? (paths.ContainsKey("--memory-file") ? Path.ChangeExtension(memoryPath, "journal.json")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PersonalitySimulator", "journal.json"));
    var memories = new MemoryStore(memoryPath);
    var journal = new JournalStore(journalPath);
    using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    var chat = new ChatSession(new OllamaChatService(client, settings), profile, memories, journal);
    chat.ConfigureUserStyle(userStyle);
    var saved = await memories.GetAllAsync();
    var journalEntries = await journal.GetAllAsync();
    var journalCommands = new JournalCommands(journal, chat, Console.Out);
    using var shutdown = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
    Console.WriteLine($"Chatting with {profile.Name} through {settings.Model}.");
    Console.WriteLine("Your style is ON. /style shows it; /style off or /style on switches modes and clears temporary chat.");
    Console.WriteLine("Say be serious to stop teasing; let's joke again resumes it for this session.");
    Console.WriteLine("/reset clears history. /exit quits. Ctrl+C stops the session.");
    Console.WriteLine("Ask what time is it or use /time for your computer's local time.");
    Console.WriteLine("/calc <expression> calculates numbers directly, for example /calc (18 + 7) * 4.");
    Console.WriteLine("/remember <fact> saves a fact. /memories lists saved facts. /forget <id> removes one.");
    Console.WriteLine("/reflect proposes a journal entry. /journal lists approved entries and their IDs.");
    Console.WriteLine("Journal drafts are saved only when you type /journal save. Type /journal help for editing commands.");
    Console.WriteLine($"Loaded {saved.Count} saved facts. Temporary chat history lasts for this session only.");
    Console.WriteLine($"Loaded {journalEntries.Count} approved journal entries.");
    Console.WriteLine("Only /remember saves facts. The first reply may take a moment.");
    while (!shutdown.IsCancellationRequested)
    {
        Console.Write("\nYou: ");
        var input = Console.ReadLine();
        if (input is null || input.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;
        if (string.IsNullOrWhiteSpace(input)) continue;
        var parts = input.Trim().Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();
        var argument = parts.Length == 2 ? parts[1].Trim() : "";
        try
        {
            if (await journalCommands.TryHandleAsync(command, argument, shutdown.Token)) continue;
            if (command == "/style")
            {
                if (argument.Equals("on", StringComparison.OrdinalIgnoreCase) || argument.Equals("off", StringComparison.OrdinalIgnoreCase))
                {
                    chat.SetUserStyle(argument.Equals("on", StringComparison.OrdinalIgnoreCase));
                    journalCommands.DiscardDraft();
                    Console.WriteLine($"Your style is {(chat.UserStyleEnabled ? "ON" : "OFF")}. Temporary chat and unsaved draft cleared; saved data retained.");
                }
                else if (argument.Length == 0)
                {
                    Console.WriteLine($"Your style is {(chat.UserStyleEnabled ? "ON" : "OFF")}.");
                    foreach (var rule in userStyle.Voice) Console.WriteLine("Voice: " + rule);
                    foreach (var rule in userStyle.DecisionPrinciples) Console.WriteLine("Decision principle: " + rule);
                    foreach (var example in userStyle.ConfirmedDecisions) Console.WriteLine("Confirmed decision: " + example);
                    foreach (var observation in userStyle.ProvisionalObservations) Console.WriteLine("Tentative observation: " + observation);
                    foreach (var description in userStyle.InterviewDescriptions) Console.WriteLine("Approved interview description: " + description);
                    if (userStyle.SelfReportedBigFive is { } ratings)
                        Console.WriteLine($"Self-reported Big Five (1–10): openness {ratings.Openness}, conscientiousness {ratings.Conscientiousness}, extraversion {ratings.Extraversion}, agreeableness {ratings.Agreeableness}, neuroticism {ratings.Neuroticism}.");
                    Console.WriteLine($"Writing samples: {userStyle.WritingSamples.Length}. Decision principles: {userStyle.DecisionPrinciples.Length}.");
                    Console.WriteLine("These guide wording, not identity. Ordinary chat does not change this profile.");
                }
                else Console.WriteLine("Usage: /style [on|off]");
                continue;
            }
            if (command == "/calc")
            {
                Console.WriteLine($"{profile.Name}: {await chat.SendAsync(input, shutdown.Token)}");
                continue;
            }
            if (command == "/time")
            {
                if (argument.Length > 0) Console.WriteLine("Usage: /time");
                else Console.WriteLine($"{profile.Name}: {await chat.SendAsync(input, shutdown.Token)}");
                continue;
            }
            if (command == "/remember")
            {
                var memory = await memories.RememberAsync(argument, shutdown.Token);
                Console.WriteLine($"Saved #{memory.Id}: {memory.Text}");
                continue;
            }
            if (command == "/memories")
            {
                if (argument.Length > 0) { Console.WriteLine("Usage: /memories"); continue; }
                var items = await memories.GetAllAsync(shutdown.Token);
                if (items.Count == 0) Console.WriteLine("No saved facts. Use /remember <fact> to add one.");
                foreach (var item in items) Console.WriteLine($"#{item.Id}: {item.Text}");
                Console.WriteLine($"Memory file: {memories.FilePath}");
                continue;
            }
            if (command == "/forget")
            {
                if (!int.TryParse(argument, out var id) || id < 1)
                { Console.WriteLine("Usage: /forget <id>, for example /forget 1"); continue; }
                if (await memories.ForgetAsync(id, shutdown.Token))
                {
                    chat.Reset();
                    journalCommands.DiscardDraft();
                    Console.WriteLine($"Forgot saved fact #{id}. Temporary conversation cleared so the removed fact is not reused from chat history.");
                    Console.WriteLine("Journal entries are separate; edit or delete any entry containing that fact.");
                }
                else Console.WriteLine($"No saved fact with ID {id}.");
                continue;
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { break; }
        catch (OperationCanceledException) { Console.WriteLine("The request timed out. Nothing was saved."); continue; }
        catch (HttpRequestException ex) { Console.WriteLine($"Ollama error: {ex.Message}"); continue; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            Console.WriteLine($"Could not complete command: {ex.Message}");
            continue;
        }
        if (input.Trim().Equals("/reset", StringComparison.OrdinalIgnoreCase))
        {
            chat.Reset();
            journalCommands.DiscardDraft();
            Console.WriteLine("Temporary conversation and draft cleared. Personality, saved facts, and approved journal retained.");
            continue;
        }
        if (input.TrimStart().StartsWith('/'))
        {
            Console.WriteLine("Commands: /calc <expression>, /time, /remember <fact>, /memories, /forget <id>, /reflect, /journal, /reset, /exit");
            continue;
        }
        try
        {
            var localReply = LocalClockService.IsTimeQuestion(input) || LocalCalculator.TryAnswer(input, out _)
                || UnavailableActions.TryAnswer(input, out _);
            var reply = localReply ? await chat.SendAsync(input, shutdown.Token)
                : await ConsoleActivity.RunAsync(() => chat.SendAsync(input, shutdown.Token),
                    ResponseReasoning.IsCarefulReply(input) ? "Thinking and checking" : "Thinking");
            Console.WriteLine($"{profile.Name}: {reply}");
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested) { break; }
        catch (OperationCanceledException) { Console.WriteLine("Reply timed out. Try again; the failed turn was not saved."); }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Ollama error: {ex.Message}");
            Console.WriteLine($"Check Ollama is running and {settings.Model} is installed on {settings.Endpoint}.");
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        { Console.WriteLine($"Could not read conversation data: {ex.Message}"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { Console.WriteLine($"Could not read saved memory: {ex.Message}"); }
    }
    Console.WriteLine("Goodbye!");
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
{
    Console.Error.WriteLine($"Could not load startup configuration: {ex.Message}");
    Environment.ExitCode = 1;
}


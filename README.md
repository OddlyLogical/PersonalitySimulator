# PersonalitySimulator

A fully local C# / .NET 10 console application for experimenting with configurable AI personalities through Ollama. No additional NuGet dependencies or cloud account are required.

## Setup

1. Install the .NET 10 SDK and Ollama.
2. Run `ollama pull qwen2.5:3b` and start Ollama.
3. Copy each `Data/*.example.json` to the corresponding filename without `.example`.
4. Open `PersonalitySimulator.slnx` in Visual Studio 2026, or run `dotnet run`.

The example endpoint is http://localhost:11434/api/chat. Keep it local. Model performance and context capacity depend on available memory; the default context is 4096 tokens.

## Usage

Chat normally. Use `/reset` to clear session history, `/exit` to quit, `/time` for local time, and `/calc 17 * 24` for arithmetic. Ctrl+C cancels and exits.

`/remember <fact>`, `/memories`, and `/forget <id>` manage explicitly saved facts. Ordinary chat does not automatically save facts. The journal workflow requires review before saving; use `/journal` for its commands. `/style` displays the style profile; `/style on` and `/style off` control its use.

Memories and journal data live under the current user's local application data directory, in PersonalitySimulator. Optional `--memory-file <path>` and `--journal-file <path>` arguments support isolated test data.

## Architecture

Models hold profiles, settings and stored records. Services handle Ollama HTTP communication, session context, local utilities, validated JSON storage and response checks. Program composes these components and handles console interaction. See [architecture](docs/ARCHITECTURE.md) and [roadmap](docs/ROADMAP.md).

## Privacy and limitations

Only synthetic example profiles are committed. Local profiles, memories, journals, databases, build artifacts, logs and backups are excluded. Review staged changes before every commit: ignore rules are not a secret scanner.

Ollama generates replies locally with the supplied configuration. The application has no internet search, email, calendar or booking tools. Models can hallucinate, repeat themselves, misread requests and imitate a person's style poorly. Response checks do not guarantee correctness, human likeness or faithful personality simulation.

Errors are reported in the console. Persistent structured logging is not implemented. Historical personality snapshots, SQLite storage, multiple personalities and relationship-aware interactions are planned, not current features.

## Development

Run `dotnet build -c Release`. See [contributing](CONTRIBUTING.md). This repository starts at packaging version 0.1.0; the preserved application banner may show the earlier local development version 0.5.2.

No license is granted initially.

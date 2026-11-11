# Contributing

Use .NET 10. Copy the example JSON files to local runtime configuration and run `dotnet build -c Release`. Use isolated memory and journal paths for manual testing.

Keep changes focused. Explain observable behavior and verification. Add meaningful regression checks for application defects; distinguish nondeterministic model weaknesses from code defects.

Never commit personal profiles, conversation exports, memories, journals, databases, credentials, logs or backups. Inspect `git diff --cached` and `git status --ignored` before committing. Use synthetic fixtures and preserve real user data during testing.

There is no license initially; contact the repository owner before redistribution or external contributions.

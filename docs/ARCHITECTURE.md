# Architecture

## Current design

Program is the console composition root. Models represent configuration and records; services implement local HTTP calls, context selection, session state, memory and journal JSON persistence, and bounded response review. The session retains successful conversation turns and supplies selected context within a configured budget.

JSON personality and style profiles are separate from explicit saved facts and reviewed journal entries. Generated dialogue is not proof of a real event or a saved memory. Storage writes are explicit; review and validation do not establish the truth of model-generated text.

## Decisions

Preserve the working implementation and avoid new dependencies during repository preparation. Use local Ollama through HttpClient. Keep private runtime JSON outside source control. Retain the existing JSON stores until a tested persistence migration is justified.

## Planned evolution

Introduce storage interfaces before a SQLite implementation. Give each personality an immutable ID; scope memories and relationships by owner. Record provenance as user-reported, imported, simulated or inferred, together with timestamps and review status. Keep simulated interactions separate from factual memories.

Version approved profiles as immutable snapshots with parent IDs, schema versions and change reasons. Session participants must refer to explicit profile snapshots. Relationships must be directional and scoped to participant IDs. These are design requirements for future work; the current implementation does not enforce them.

Persistent logging should record operational errors and timings without prompts, replies, secrets or personal records by default.

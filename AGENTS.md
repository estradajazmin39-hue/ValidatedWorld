# ValidatedWorld Agent Instructions

## Project purpose
ValidatedWorld is a .NET 10 system for consistency-validated AI authoring of fictional worlds, characters, quests, and stories. You can read the README.md file for a plain-language explanation of the repository.

The current detailed product specification was written mostly by AI and is located here:
`docs/validated_world_authoring_spec.md`

Read that specification before making architectural or product-level decisions, but also keep it up-to-date if anything needs to be changed (or was directly requested to be changed by a user prompt). Remember that the human instructions override anything written in any md file, but you may ask the user for clarification first on any items that you think are fundamentally conflicting.

## Repository structure
- `src/ValidatedWorld.Core` — engine-independent domain model
- `src/ValidatedWorld.Serialization` — import/export and persistence
- `src/ValidatedWorld.Validation` — deterministic consistency validation
- `src/ValidatedWorld.Generation` — AI-assisted generation/review abstractions
- `src/ValidatedWorld.Export` — human- and machine-facing export formats, like json files or reference manuals
- `src/ValidatedWorld.Cli` — command-line interface
- `tests/` — automated tests
- `samples/SampleWorld` — demonstrable sample world

## Required workflow
Before completing a change:
1. Build the full solution.
2. Run all tests.
3. Add or update tests for changed behavior.
4. Keep engine-specific dependencies out of the core libraries.
5. Preserve the canonical-world / transaction / validation model described in the spec.
6. Do not edit generated export artifacts as authoritative source data.

## Commands
- Build: `dotnet build`
- Test: `dotnet test`
- Restore: `dotnet restore`

## Engineering priorities
Favor:
- clear domain modeling
- deterministic behavior
- explicit schemas and contracts
- useful diagnostics
- testability
- clean, short, shared functions and classes (when appropriate)
- aggressive changes without regard to backwards compatibility (A user will remove this line when this no longer applies)
- agent-friendly CLI interfaces
- engine independence

Avoid speculative abstractions that are not yet needed by the product specification. Also, please note that dotnet 10 generated this solution file as slnx instead of sln, so please make all relevant commands for `ValidatedWorld.slnx` instead of `ValidatedWorld.sln`.

## Definition of done
A change is complete when the solution builds, relevant tests pass, new behavior is covered by tests, and the sample/documentation is updated when appropriate.
# ValidatedWorld Agent Instructions

## Project purpose

ValidatedWorld is a .NET 10 continuity compiler for consistency-validated AI
authoring of fictional worlds, characters, mysteries, quests, campaigns, and
stories. It validates explicitly modeled continuity; it does not claim to prove
arbitrary prose consistent.

## Required reading and authority

Before making architectural, product, schema, persistence, or public API
decisions, read these files in order:

1. `docs/feasibility.md` — guarantee boundary and POC falsification plan.
2. `docs/validated_world_authoring_spec.md` — authoritative product specification.
3. `docs/implementation_blueprint.md` — normative implementation handoff,
   algorithms, tests, and work-package order.

Human instructions override repository documents. If implementation evidence
invalidates a design assumption, do not work around it silently: update the
relevant documents and explain the change.

## Repository structure

- `src/ValidatedWorld.Core` — engine-independent immutable domain model.
- `src/ValidatedWorld.Serialization` — strict JSON, canonical serialization,
  hashing, migrations, and file workspace adapters.
- `src/ValidatedWorld.Validation` — indexes, dependency/impact analysis,
  deterministic rules, and bounded narrative analysis.
- `src/ValidatedWorld.Generation` — provider-neutral context/review abstractions;
  optional AI adapters do not belong in Core.
- `src/ValidatedWorld.Export` — deterministic machine- and human-facing derived
  formats.
- `src/ValidatedWorld.Cli` — agent-grade command-line host.
- `src/ValidatedWorld.Application` — planned in blueprint WP0; transaction,
  commit, query, and use-case orchestration.
- `tests/` — automated tests mirroring production boundaries.
- `samples/HarborMystery` — planned POC and intentional-error corpus.

## Durable design rules

- Keep canon truth, character perspective, fictional time, narrative order, and
  authoring revision as separate concepts.
- Treat missing facts as unknown unless a schema explicitly opts into a
  closed-world rule.
- Author only through transactions. A failed or stale commit must leave canon
  byte-for-byte unchanged.
- Derive dependencies from typed references and annotations; do not maintain a
  separate hand-authored graph as another source of truth.
- Report deterministic results as proven, disproven, or inconclusive. Never call
  a bounded-out analysis successful.
- Keep AI review optional, auditable, and non-authoritative.
- Use stable IDs, diagnostic codes, structured outputs, deterministic ordering,
  and replayable counterexamples.
- Keep engine, UI, database, model-provider, and current plugin-format
  dependencies out of Core.
- Do not edit generated export artifacts as authoritative source data.
- Do not add speculative frameworks or projects ahead of the blueprint work
  package that needs them.

## Required workflow

Implement one blueprint work package or clearly bounded vertical slice at a time.
Before completing any change:

1. Add or update tests for changed behavior. Documentation-only changes do not
   require artificial tests.
2. Update the Harbor sample and golden diagnostics/exports when applicable.
3. Build the full solution.
4. Run all tests.
5. State any unverified or inconclusive behavior in the handoff.

Commands:

```powershell
dotnet restore ValidatedWorld.slnx
dotnet build ValidatedWorld.slnx
dotnet test ValidatedWorld.slnx
```

Use `ValidatedWorld.slnx`, not `ValidatedWorld.sln`.

## Engineering priorities

Favor clear domain modeling, deterministic behavior, explicit schemas and
contracts, useful evidence-bearing diagnostics, testability, short cohesive
functions, agent-friendly interfaces, and aggressive POC changes without
backward-compatibility constraints.

Avoid universal ontologies, unrestricted rule languages, natural-language query
parsers, incremental validation, plugin packaging, and visual editors until the
blueprint's evidence gate authorizes them.

## Definition of done

A change is complete when its documented acceptance criteria are met, the full
solution builds, all tests pass, changed behavior is covered, sample/docs are
consistent, and deterministic guarantees are not overstated.

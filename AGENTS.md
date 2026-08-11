# ValidatedWorld Agent Instructions

## Project purpose

ValidatedWorld is a .NET 10 consistency compiler for large connected documents
and designed worlds. Its common core models authored content, claims,
definitions, assumptions, evidence, requirements, decisions, and typed
dependencies. Technical documents, fiction, and interactive worlds are profiles
over that core.

It validates explicitly modeled relationships, computes change impact, and can
force review of affected content. It does not claim to prove arbitrary prose,
scientific truth, legal sufficiency, or literary quality.

## Required reading and authority

Before making architectural, product, schema, persistence, or public API
decisions, read these files in order:

1. `docs/feasibility.md` — guarantee boundary and staged POC gates.
2. `docs/validated_world_authoring_spec.md` — authoritative product specification.
3. `docs/implementation_blueprint.md` — normative common-core implementation,
   algorithms, tests, and work-package order.

Human instructions override repository documents. If implementation evidence
invalidates a design assumption, do not work around it silently: update the
relevant documents and explain the change.

## Repository structure

- `src/ValidatedWorld.Core` — engine- and medium-independent immutable domain.
- `src/ValidatedWorld.Serialization` — strict JSON, canonical serialization,
  hashing, migrations, and file workspace adapters.
- `src/ValidatedWorld.Validation` — indexes, dependency/impact analysis,
  deterministic rules, review obligations, and optional bounded analysis.
- `src/ValidatedWorld.Generation` — deterministic context packets and
  provider-neutral heuristic-review abstractions.
- `src/ValidatedWorld.Export` — deterministic document, report, and profile
  exports.
- `src/ValidatedWorld.Cli` — agent-grade command-line host.
- `src/ValidatedWorld.Application` — planned in blueprint WP0; transaction,
  commit, query, and use-case orchestration.
- `tests/` — automated tests mirroring production boundaries.
- `samples/TechnicalDesign` — first document/dependency POC.
- `samples/HarborMystery` — later narrative/state-profile POC.

## Durable design rules

- Treat a project as a versioned graph of canonical records. A fictional world
  is one possible project profile.
- Keep authored content, semantic claims, evidence, and their bindings distinct
  but transactionally consistent.
- Treat missing claims or links as unknown unless a profile explicitly declares
  a closed-world rule.
- Author only through transactions. A failed or stale commit must leave canon
  byte-for-byte unchanged.
- Derive the operational dependency graph from typed references and semantic
  links; do not maintain a second hand-authored adjacency structure.
- For every impacted record, require a disposition such as updated,
  reviewed-no-change, or not-applicable when project policy requests it.
- Report deterministic results as proven, disproven, or inconclusive. Report AI
  or text-review results as concerns, even when policy requires resolving them.
- Keep AI review auditable and non-authoritative. Extracted claims and links do
  not become canon without a transaction.
- Use stable IDs, diagnostic codes, structured outputs, deterministic ordering,
  and explainable impact paths.
- Keep UI, database, model-provider, game-engine, and current plugin-format
  dependencies out of Core.
- Model an interactive game as a static specification of variables,
  conditions, effects, and transitions; derive reachable runtime states rather
  than authoring every possible state as canon.
- Do not edit generated exports as authoritative source data.
- Do not implement narrative state, plugin packaging, or other later profiles
  ahead of the blueprint gate that authorizes them.

## Required workflow

Implement one blueprint work package or clearly bounded vertical slice at a time.
Before completing any change:

1. Add or update tests for changed behavior. Documentation-only changes do not
   require artificial tests.
2. Update the applicable sample and golden reports/exports.
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

Favor a small general claim/dependency core, explicit schemas and contracts,
deterministic behavior, evidence-bearing diagnostics, mandatory impact review,
testability, short cohesive functions, agent-friendly interfaces, and aggressive
POC changes without backward-compatibility constraints.

Avoid universal ontologies, unrestricted rules languages, natural-language query
parsers, automatic acceptance of AI extraction, incremental validation, game
state exploration, plugin packaging, and visual editors until their work-package
gate authorizes them.

## Definition of done

A change is complete when its documented acceptance criteria are met, the full
solution builds, all tests pass, changed behavior is covered, applicable samples
and docs are consistent, required impact dispositions are enforced, and
deterministic guarantees are not overstated.

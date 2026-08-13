# ValidatedWorld Agent Instructions

## Project purpose

ValidatedWorld is a .NET 10 semantic change-control engine for versioned project
data. One embedded SQLite application file stores a small fixed relational
metamodel: typed records, relationships, constraints, transactions, impact
evidence, reviews, and commits. Technical projects, fiction, and interactive
worlds are logical profiles over that common metamodel.

JSON is the deterministic agent/interchange protocol, not the authoritative
physical store. The engine validates explicit semantics, computes change impact,
and can require review of affected records. It does not ingest, generate, render,
publish, or validate arbitrary finished prose or game assets.

## Required reading and authority

Before making architectural, product, schema, persistence, or public API
decisions, read these files in order:

1. `docs/feasibility.md` — guarantee boundary, related-system risk, and proof
   gates.
2. `docs/validated_world_authoring_spec.md` — authoritative product
   specification.
3. `docs/implementation_blueprint.md` — normative SQL schema, algorithms,
   tests, and work-package order.
4. `docs/implementation_execution_plan.md` — authoritative completed-work
   evidence, current assignment, and next-agent handoff.

`docs/prior_art_and_positioning.md` records relevant existing systems and should
be consulted before broadening the product.

Human instructions override repository documents. If implementation evidence
invalidates a design assumption, update the controlling documents and explain
the change instead of silently working around it.

## Execution state and agent handoff

`docs/implementation_execution_plan.md` is a required, living part of every
implementation change. The blueprint says what to build; the execution plan says
what the repository has proven, which one work package or slice is active, and
exactly what the next agent must do.

- Follow the work packages in order. Do not begin queued or gated work.
- Before coding, reconcile the plan with the actual source/tests, mark the
  current assignment `in-progress`, and record the intended bounded scope.
- Complete one work package or one explicitly recorded slice at a time.
- Mark work `complete` only after its executable acceptance criteria and the full
  repository verification pass.
- In the same change, update the roadmap status, append exact verification
  evidence, and write the next assignment with scope, exclusions, and acceptance
  criteria. A code change without this plan update is incomplete.
- The next agent must be able to continue from repository files alone. Never
  rely on chat history, unstated assumptions, or a human remembering prior work.

Routine implementation choices do not require human approval. Choose the
simplest reversible implementation consistent with the controlling documents
and prove it with tests. If repository evidence and the plan disagree, correct
the plan before moving forward; do not preserve a false completion claim.

## Repository structure

- `src/ValidatedWorld.Core` — database- and medium-independent immutable logical
  metamodel.
- `src/ValidatedWorld.Serialization` — strict versioned JSON protocol and
  deterministic logical snapshot serialization/hashing.
- `src/ValidatedWorld.Validation` — type/index construction, dependency and
  impact analysis, deterministic constraints, coverage, and review obligations.
- `src/ValidatedWorld.Application` — transaction, commit, query, and use-case
  orchestration plus persistence ports.
- `src/ValidatedWorld.Persistence.Sqlite` — SQLite schema, migrations,
  repositories, transactions, integrity checks, and logical snapshot mapping.
- `src/ValidatedWorld.Cli` — agent-grade JSON command host and composition root.
- `tests/` — automated tests mirroring production boundaries.
- `samples/TechnicalProject` — first SQLite-backed dependency POC.
- `samples/HarborMystery` — later narrative-profile POC.

## Durable design rules

- Treat `project.vw.db` as the only authoritative workspace state.
- Hash a deterministic logical JSON projection; never hash SQLite file bytes as
  semantic identity.
- Keep the physical relational schema fixed and implementation-owned. Project
  authors and AIs define logical records through supported profiles; they do not
  create arbitrary tables or mutate canonical rows directly.
- Use a small opinionated metamodel: logical type definitions, stable-ID records,
  typed relations/endpoints, typed references, constraints, transactions,
  reviews, and commits.
- Keep domain vocabulary profile-driven. Do not force technical claims,
  fictional events, and game state into one domain ontology.
- Make every graph-relevant reference explicit and typed. Foreign keys prove
  existence; Validation proves supported endpoint semantics and impact meaning.
- Derive the operational dependency graph from typed fields and relationships.
  Do not maintain a second authored adjacency graph.
- Treat missing claims or links as unknown unless a profile explicitly declares
  a finite closed-world rule.
- Author only through transactions. A failed or stale commit must roll back every
  relational write.
- Treat accepted transaction operations as the direct change record; do not add
  a separate semantic-diff source of truth.
- Require every policy-selected impacted record to be updated or given a current
  reviewed-no-change/not-applicable disposition before commit.
- Report deterministic results as proven, disproven, or inconclusive.
- Use stable IDs, diagnostic codes, structured JSON results, deterministic
  ordering, and explainable impact paths.
- Keep SQLite, JSON, UI, model-provider, and game-engine dependencies out of Core.
- Keep the web host optional. Gate A is local/embedded; PostgreSQL or a service is
  justified only by demonstrated multi-user requirements.
- Do not substitute a graph database, RAG index, or arbitrary AI-generated SQL
  for the semantic validation layer.
- Do not claim an external novel/document is synchronized or consistent.
- Do not implement narrative state, game-state exploration, or public integration
  packaging before its blueprint gate.

## SQLite requirements

- Use `Microsoft.Data.Sqlite.Core` directly with an explicitly pinned, audited
  SQLitePCLRaw native bundle; do not introduce an ORM in Gate A.
- Enable and verify `PRAGMA foreign_keys = ON` on every connection.
- Use `STRICT` tables where compatible, explicit constraints, parameterized SQL,
  and schema migrations with checksums.
- Use `ON DELETE RESTRICT`; application transactions perform explicit repairs.
- Treat a supplied database file as untrusted input. Set conservative limits and
  never load extensions or execute stored project text as SQL.
- SQLite permits one writer. Keep write transactions short; never hold one open
  while waiting for a human or AI review.

## Required workflow

Implement only the current assignment in
`docs/implementation_execution_plan.md`. Every engineering step through WP8 must
be testable without human inspection, interactive UI use, mutable external
services, secrets, or subjective manual judgment. Use fixed clocks/IDs/seeds,
temporary generated databases, deterministic fault injection, scripted CLI
scenarios, and structured golden outputs as applicable.

Before completing any change:

1. Add or update tests for changed behavior. Documentation-only changes do not
   require artificial tests. Empty, tautological, skipped, flaky, or
   manually-inspected tests are not acceptance evidence.
2. Update applicable database-fixture generators, logical JSON snapshots, and
   golden diagnostics/results. Do not commit populated sample or user `.vw.db`
   files. A binary database may be committed only as an explicit fixture under
   `tests/` when its byte-level state cannot reasonably be generated by the test.
3. Run the assignment-specific executable acceptance checks.
4. Build the full solution and run all tests.
5. Update the execution plan with completion evidence and the exact next task.
6. State any unverified or inconclusive behavior in both the plan and handoff.

Commands:

```powershell
dotnet restore ValidatedWorld.slnx
dotnet build ValidatedWorld.slnx --no-restore
dotnet test ValidatedWorld.slnx --no-build --no-restore
```

Use `ValidatedWorld.slnx`, not `ValidatedWorld.sln`.

## Failure-loop rule

A failing test is normally a signal to diagnose and repair autonomously. Stop
and request focused human feedback only when the same blocker survives three
materially different, evidence-based repair attempts, the attempts cycle back to
an earlier failure, or conflicting controlling requirements would force a change
to product scope, public contract, guarantees, or destructive-data behavior.

When that threshold is reached, do not continue retrying and do not start later
work. Mark the current execution-plan item `blocked`; record the failing command
and relevant output, root-cause hypothesis, and all attempted approaches; then
ask the smallest concrete question needed to proceed. Never ask a human to
manually verify behavior that should have an automated oracle.

## Engineering priorities

Favor explicit relational integrity, a small logical metamodel, deterministic
behavior, full validation before commit, evidence-bearing diagnostics, mandatory
impact review, backend-neutral logical hashes, testability, short cohesive
functions, and agent-friendly JSON/read-only SQL query surfaces.

Avoid arbitrary project DDL, direct canonical SQL mutation, universal ontologies,
unrestricted rules languages, natural-language query parsers, automatic
acceptance of AI suggestions, document generation, premature incremental
validation, a graph database, a web platform, plugin packaging, and visual
editors until an evidence gate authorizes them.

## Definition of done

A change is complete when its documented acceptance criteria are met, the full
solution builds, all tests pass, changed behavior is covered, SQLite fixtures and
logical JSON goldens agree, required impact dispositions are enforced, and
deterministic guarantees are not overstated. The execution plan must also record
the completed evidence and an actionable next assignment; otherwise the change
is not done.

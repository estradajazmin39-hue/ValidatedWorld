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

If Gate A proves the deterministic core useful, the first planned later phase is
a bounded AI semantic reviewer. It receives exact dependency/impact context and
returns cited heuristic concerns. It is provider-neutral, auditable,
non-authoritative, and cannot mutate canon or turn a model opinion into proof.

## Required reading and authority

Before making architectural, product, schema, persistence, or public API
decisions, read these files in order:

1. `docs/feasibility.md` — guarantee boundary, related-system risk, and proof
   gates.
2. `docs/validated_world_authoring_spec.md` — authoritative product
   specification.
3. `docs/implementation_blueprint.md` — normative SQL schema, algorithms,
   tests, and work-package order.
4. `docs/testing_and_qa.md` — application-owned fixtures, no-external-database
   packaging, realistic scenarios, end-to-end tests, and agent QA.
5. `docs/implementation_execution_plan.md` — completed work, the one current
   task, and the remaining roadmap order.

Before making AI-review, provider, prompt, network-disclosure, or secret-storage
decisions, also read `docs/ai_semantic_review.md`. It is a post-Gate-A design,
not authorization to pull that work into the current roadmap.

`docs/prior_art_and_positioning.md` records relevant existing systems and should
be consulted before broadening the product.

Human instructions override repository documents. If implementation evidence
invalidates a design assumption, update the controlling documents and explain
the change instead of silently working around it.

## Local task and handoff

`docs/implementation_execution_plan.md` is a required, living part of every
implementation task. The blueprint says what to build; the plan says what has
been completed and names one Current task.

- A human prompt starts each agent run.
- Implement only Current task. Do not begin or delegate later roadmap work.
- On success, record the completed evidence, replace Current task with a fully
  specified next task, report to the human, and stop.
- On failure or repeated non-progress, leave Current task unchanged, report the
  evidence to the human, and stop. The human may revert the work and try another
  agent.
- The next agent must be able to continue from repository files without chat
  history or unstated assumptions.

Routine implementation choices do not require human approval. Choose the
simplest reversible implementation consistent with the controlling documents
and prove it with tests. If repository evidence and the plan disagree, correct
the plan before moving forward; do not preserve a false completion claim.

## Git boundary

Do not perform Git state-changing or remote operations. Do not create or switch
branches, stage files, commit, amend, merge, rebase, cherry-pick, revert, reset,
clean, stash, tag, pull, push, or create pull requests, and do not change Git
configuration. Work in the repository and working tree exactly as supplied by
the human. Read-only `git status`, `git diff`, and `git log` are permitted when
useful, but the implementation workflow must not depend on Git being available.

Preserve unrelated human changes. Leave all edits unstaged for the human to
review and manage. References elsewhere to a ValidatedWorld transaction commit
mean an application/SQLite operation, not a Git commit.

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
- `src/ValidatedWorld.AiReview` — planned post-Gate-A provider-neutral semantic
  review packets, concerns, and orchestration; not yet created.
- `src/ValidatedWorld.AiReview.OpenAI` — planned dependency-isolated optional
  OpenAI adapter; not yet created.
- `tests/` — automated tests mirroring production boundaries.
- `tests/ValidatedWorld.TestKit` — planned reusable app/CLI fixture and process
  helpers; it never writes canonical SQLite rows directly.
- `tests/ValidatedWorld.EndToEnd.Tests` — planned black-box public CLI scenarios.
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
- Report AI/text-review results only as concerns. Policy may require a current
  review and disposition of every concern, but concern correctness is never a
  deterministic guarantee.
- Keep AI review auditable and non-authoritative. Extracted records, links,
  operations, and dispositions remain proposals until explicitly applied
  through the normal transaction boundary.
- Use stable IDs, diagnostic codes, structured JSON results, deterministic
  ordering, and explainable impact paths.
- Keep SQLite, JSON, UI, model-provider, and game-engine dependencies out of Core.
- Never make a provider call while a SQLite write transaction is open. Normal
  validation, impact, and commit commands never contact a provider implicitly.
- Never persist or log provider credentials. Use .NET user-secrets for local
  development and environment variables for published/deployed processes as
  specified in `docs/ai_semantic_review.md`.
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
- SQLite is embedded and serverless. Runtime and tests may not require a SQLite
  server, standalone `sqlite3`, system SQLite installation, Docker, or SQL
  knowledge for normal workflows. The pinned NuGet native bundle is deployed
  with the application.
- The application owns creation, migration, verification, sample generation, and
  backup. Test and QA databases are generated through public Application/CLI
  operations from retained scenario assets, never hand-authored with external
  database tools or raw canonical inserts.
- If bundled SQLite cannot support an intended platform, report the platform and
  discuss it with the human before introducing Docker or another dependency.

## Required workflow

Implement only Current task in
`docs/implementation_execution_plan.md`. Every engineering step through WP8 must
be testable without human inspection, interactive UI use, mutable external
services, secrets, or subjective manual judgment. Use fixed clocks/IDs/seeds,
temporary app-generated databases, deterministic fault injection, scripted CLI
scenarios, and structured golden outputs as applicable.

The later AI-review phase follows the same rule for its normal suite: use fake
providers and scripted HTTP responses. A real-provider evaluation is separately
opt-in, requires an intentionally supplied secret, and is never required for the
default restore/build/test sequence.

Testing has three required layers whenever the current product surface supports
them:

1. Focused unit/property tests for local contracts.
2. Realistic integration and scripted end-to-end scenarios using the evolving
   TechnicalProject soft-logic corpus, not only toy records.
3. Starting at WP3's first database/CLI walking skeleton, an actual AI agent
   performs a black-box QA workflow through public commands and documented
   read-only views against a newly generated temporary `.vw.db`.

The black-box pass starts from user documentation/help, does not mutate canonical
rows directly, and records whether the agent could complete a realistic goal,
what was confusing, misleading, unnecessarily difficult, or missing, and what it
believed would happen next. The same locally running agent performs this as a
clearly separated QA-user pass after implementation tests; no parallel agent is
needed. Preserve a replayable scripted scenario in the test suite. Convert
deterministic QA failures into regression tests.

An end-to-end test is not merely "the command returned zero." Assert the
resulting logical data, impact set and paths, review obligations, diagnostics,
commit/rollback state, and unrelated-record exclusions available at that work
package. If the product cannot complete the documented scenario, requires source
knowledge, silently makes the wrong semantic change, or feels fundamentally
misdirected, do not hide that behind unit-test success: fix it or report the
finding to the human and stop without advancing Current task.

Before completing any change:

1. Add or update tests for changed behavior. Documentation-only changes do not
   require artificial tests. Empty, tautological, skipped, flaky, or
   manually-inspected tests are not acceptance evidence.
2. Update applicable database-fixture generators, logical JSON snapshots, and
   golden diagnostics/results. Do not create or propose tracking populated
   sample or user `.vw.db` files. A binary database is eligible for human-managed
   versioning only as an explicit fixture under `tests/` when its byte-level state
   cannot reasonably be generated by the test.
3. Run the task-specific executable acceptance checks.
4. Run the realistic scenario and, from WP3 onward, the agent-operated black-box
   usability walkthrough.
5. Build the full solution and run all tests.
6. Update the execution plan with completion and QA evidence plus the exact next
   task.
7. State any unverified, inconclusive, or usability concern in both the plan and
   handoff.
8. Report the completed or failed result to the human and stop. Do not implement
   the newly recorded next task during the same invocation.

Commands:

```powershell
dotnet restore ValidatedWorld.slnx
dotnet build ValidatedWorld.slnx --no-restore
dotnet test ValidatedWorld.slnx --no-build --no-restore
```

Use `ValidatedWorld.slnx`, not `ValidatedWorld.sln`.

## Failure-loop rule

A failing test is normally a signal to diagnose and repair autonomously. Stop
instead of looping when materially different repair attempts keep producing the
same failure, attempts cycle back to an earlier failure, or proceeding requires
an unresolved product/scope decision. Do not advance the plan. Report the failing
command and relevant output, likely cause, and attempted fixes, then stop. Never
ask a human to manually verify behavior that should have an automated oracle.

## Engineering priorities

Favor explicit relational integrity, a small logical metamodel, deterministic
behavior, full validation before commit, evidence-bearing diagnostics, mandatory
impact review, backend-neutral logical hashes, testability, short cohesive
functions, and agent-friendly JSON/read-only SQL query surfaces.

Avoid arbitrary project DDL, direct canonical SQL mutation, universal ontologies,
unrestricted rules languages, natural-language query parsers, automatic
acceptance of AI suggestions, general-purpose AI agent/RAG orchestration,
document generation, premature incremental validation, a graph database, a web
platform, plugin packaging, and visual editors until an evidence gate authorizes
them. The scoped post-Gate-A semantic-review design is not general AI
orchestration.

## Definition of done

A change is complete when its documented acceptance criteria are met, the full
solution builds, all tests pass, changed behavior is covered, SQLite fixtures and
logical JSON goldens agree, required impact dispositions are enforced, and
deterministic guarantees are not overstated. The execution plan must also record
the completed evidence, realistic-scenario result, applicable agent-QA findings,
and next task; otherwise the change is not done.

An individual agent run is done only after it reports the result, verification,
remaining uncertainty, and next planned assignment to the human, then stops.

The current roadmap is done after WP0-WP9 and the Gate A evidence are complete.
Then set Current task to `None - the planned Gate A roadmap is complete; human
direction required`, report the result, and ask whether to call the project
complete, request a separate Gate B AI semantic-review planning task,
narrow/pivot, or stop.
Do not plan or implement more work without a new human prompt.

When Current task is `None`, do not invent work. Report that the planned work is
finished, summarize the recorded outcome and optional future ideas, ask what the
human wants next, and stop.

# ValidatedWorld Agent Instructions

## Project purpose

ValidatedWorld is a .NET 10 semantic change-control engine for a versioned typed
property graph. One embedded SQLite application file stores canonical nodes and
typed first-class edges plus schema metadata and a transaction/review/commit
ledger. Technical projects, fiction, and interactive worlds are logical profiles
over that one graph model.

JSON is the deterministic agent/interchange protocol, not the authoritative
physical store. The engine validates explicit semantics, computes change impact,
and can require review of affected nodes. It does not ingest, generate, render,
publish, or validate arbitrary finished prose or game assets.

Every project has exactly one purpose root. Every other canonical node has one
`scope-parent` edge, so all nodes form a spanning rooted tree while other typed
edges form a directed semantic multigraph. Transaction targets alone seed impact:
adding ancestors as review context never causes traversal back down through their
other children. Therefore changing a leaf excludes its siblings, changing an
intermediate scope node can affect its subtree, and directly changing the purpose
root affects the project.

If Gate A proves the deterministic core useful, the first planned later phase is
an expensive whole-transaction AI semantic reviewer. One request receives every
changed item, every selected dependency/impact closure, and each selected item's
singular lineage to the purpose root, including disjoint chains together. It
returns cited heuristic concerns. It is auditable, non-authoritative, and cannot
mutate canon or turn a model opinion into proof.

The authoritative mature-product direction is AI-first authoring over that core.
A user describes a new project or alteration and may supply supported text/images;
an authoring agent searches the graph, asks focused questions, and uses bounded
Application tools to construct/repair a durable draft. It has no SQL or
unguarded canonical-write tool. After the user approves the exact current draft
in conversation, the agent calls a hash-bound guarded commit tool and completes
the workflow. Gate B remains an independent reviewer.

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

Before making AI-review, AI-authoring, provider, prompt, tool, intake,
network-disclosure, plugin/MCP, or secret-storage decisions, also read
`docs/ai_semantic_review.md` and `docs/ai_authoring_agent.md`. They define later
evidence gates; they do not authorize pulling that work into the current roadmap.

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

- `src/ValidatedWorld.Core` — database- and medium-independent immutable typed
  node/edge graph plus transaction/review domain.
- `src/ValidatedWorld.Serialization` — strict versioned JSON protocol and
  deterministic logical snapshot serialization/hashing.
- `src/ValidatedWorld.Validation` — type/index construction, dependency and
  impact analysis, deterministic constraints, coverage, and review obligations.
- `src/ValidatedWorld.Application` — transaction, commit, query, and use-case
  orchestration plus persistence ports.
- `src/ValidatedWorld.Persistence.Sqlite` — SQLite schema, migrations,
  repositories, transactions, integrity checks, and logical snapshot mapping.
- `src/ValidatedWorld.Cli` — agent-grade JSON command host and composition root.
- `src/ValidatedWorld.AiReview` — planned post-Gate-A request planning,
  concerns, review contracts, and fakeable orchestration; not yet created.
- `src/ValidatedWorld.AiReview.OpenAI` — planned dependency-isolated sole
  production review client; not yet created.
- `src/ValidatedWorld.AiAuthoring` — planned Gate C tool orchestration,
  durable sessions, intake proposals, and confirmation boundary; not yet created.
- `src/ValidatedWorld.AiAuthoring.OpenAI` — planned sole production authoring
  client; not yet created.
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
  authors and AIs define logical nodes/edges through supported profiles; they do not
  create arbitrary tables or mutate canonical rows directly.
- Use one small typed property-graph model: stable-ID nodes, stable-ID binary
  edges with properties, node/edge type definitions, constraints represented as
  typed nodes plus explicit target edges, transactions, reviews, and commits.
- Require exactly one project-purpose root. Give every other canonical node
  exactly one `scope-parent`; following parents must terminate at the root. A
  parent may have many children.
- Define `scope-parent` as child-dependent-on-parent. Node operations seed their
  target node; edge operations seed the edge type's dependent endpoint(s).
  Ancestors added as context are never new impact seeds. A root-targeting node
  operation is therefore the deliberate project-wide review mechanism.
- Keep domain vocabulary profile-driven. Do not force technical claims,
  fictional events, and game state into one domain ontology.
- Keep schema metadata and the draft/validation/commit ledger outside the
  canonical content graph. They are necessary control data, not fictional graph
  nodes.
- Make every graph-relevant connection an explicit typed edge. Scalar node/edge
  properties never contain semantic references. Foreign keys prove endpoint
  existence; Validation proves endpoint types and impact meaning.
- Derive operational dependency directions directly from canonical edge types.
  Do not maintain another authored or inferred adjacency graph.
- Treat missing claims or links as unknown unless a profile explicitly declares
  a finite closed-world rule.
- Author only through transactions. A failed or stale commit must roll back every
  relational write.
- Treat accepted transaction operations as the direct change record; do not add
  a separate semantic-diff source of truth.
- Require every policy-selected impacted node to be updated or given a current
  reviewed-no-change/not-applicable disposition before commit.
- Report deterministic results as proven, disproven, or inconclusive.
- Report AI/text-review results only as concerns. Policy may require a current
  review and disposition of every concern, but concern correctness is never a
  deterministic guarantee.
- Keep AI review auditable and non-authoritative. Candidate nodes, edges,
  operations, and dispositions remain proposals until explicitly applied
  through the normal transaction boundary.
- Use stable IDs, diagnostic codes, structured JSON results, deterministic
  ordering, and explainable impact paths.
- Keep SQLite, JSON, UI, model-provider, and game-engine dependencies out of Core.
- Never make a provider call while a SQLite write transaction is open. Normal
  validation, impact, and commit commands never contact a provider implicitly.
- AI review is one whole-transaction request containing all selected disjoint
  chains and their singular purpose lineages. Do not shard it, synthesize
  multiple calls, fan an ancestor back down into siblings, or retry a paid call
  automatically.
- Never persist or log provider credentials. Use .NET user-secrets for local
  development and environment variables for published/deployed processes as
  specified in `docs/ai_semantic_review.md`.
- `VW_AIREVIEW__LIVETESTS` opts only the separately invoked Gate B live smoke or
  evaluation harness into network use. Unit, integration, and ordinary
  end-to-end tests ignore it. Whether a real transaction requires, runs, or
  explicitly skips AI review is recorded transaction/project policy, never an
  environment-variable bypass.
- `VW_AIAUTHORING__LIVETESTS` likewise opts only the separately invoked Gate C
  live authoring evaluation into network use. Normal tests ignore it, and it
  never authorizes provider spending, project initialization, Gate B review, or
  a commit.
- Treat the authoring model as an untrusted client. It may search/read and alter
  only a durable draft through strict Application tools. Never expose raw SQL,
  canonical mutation, package mutation, rule suppression, automatic concern
  disposition, or unguarded commit.
- Bind final AI-authored commit confirmation to the exact head, draft revision,
  change-set hash, projected hash, and satisfied review state. The user may
  approve in ordinary conversation; the agent then passes the resulting opaque
  authorization to the guarded commit tool. Model output is not user approval.
- Keep the authoring and reviewing roles independent. Gate B is a fresh
  whole-transaction, tool-free request without authoring conversation state;
  authoring repairs stale prior review evidence.
- Provide deterministic bounded search and navigation suitable for agents:
  exact/type/tag/searchable-property/scope search, scope tree traversal,
  neighbors, dependencies, dependents, and context. Do not substitute embeddings
  or natural-language SQL for these contracts.
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
- Gate A schema v1 has nine tables including migration history: project,
  package/type metadata, graph entities/edges, drafts, validation runs, and
  commits. Do not reintroduce normalized property-value, relation-role,
  endpoint-per-role, disposition, operation, or diagnostic tables without
  measured evidence and a controlling-document change.
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

The later AI-review phase follows the same rule for its normal suite: use a fake
client and scripted HTTP responses. A real-provider evaluation is separately
opt-in, requires an intentionally supplied secret, and is never required for the
default restore/build/test sequence. The only planned production provider/model
for review and authoring is OpenAI `gpt-5.6-terra` with medium reasoning and the
deadlines documented in the two AI design files; do not add provider/model
selection or fallback.

### Gate B secret and spending stop rule

An agent may not begin any AI-review client, provider, prompt-submission, or live
evaluation implementation unless the human's initiating prompt contains this
exact separate line:

```text
AI_REVIEW_SECRET_READY: yes
```

If it is absent, make no implementation edits for that task. Tell the human to
set their own key with the command in `docs/ai_semantic_review.md`, report the
blocked precondition, and stop. Never search the web, repository, browser state,
environment, shell history, credential stores, or unrelated configuration for a
key; never obtain, generate, purchase, copy, infer, list, or set one. If a key is
pasted into a prompt or tracked file, stop and tell the human to remove and
rotate it.

Even after the readiness attestation, normal development uses fakes. A paid call
also requires this exact separate line in the initiating human prompt:

```text
AI_REVIEW_LIVE_CALL_AUTHORIZED: yes
```

Without it, generate and verify the exact request preview but send nothing. With
it, send at most one request for the explicitly named evaluation, with zero
automatic retries. A timeout, refusal, truncation, malformed response, or
transport failure is an inconclusive run, not permission to spend again.

### Gate C authoring secret, spending, and confirmation stop rule

An agent may not begin the AI-authoring client, provider tool loop, or multimodal
intake implementation unless the initiating human prompt contains this exact
separate line:

```text
AI_AUTHORING_SECRET_READY: yes
```

The same prohibition on finding, acquiring, listing, or setting a key applies.
A live authoring evaluation additionally requires:

```text
AI_AUTHORING_LIVE_CALL_AUTHORIZED: yes
```

Normal tests use fake/scripted model clients. A product user may explicitly start
or resume a bounded authoring session, but Gate B spending and final commit each
retain their own explicit confirmation. Provider responses use background mode,
a 1,200-second end-to-end deadline, and zero automatic paid retries. Polling a
single response and returning requested tool results are not retries.

Testing has three required layers whenever the current product surface supports
them:

1. Focused unit/property tests for local contracts.
2. Realistic integration and scripted end-to-end scenarios using the evolving
   TechnicalProject soft-logic corpus, not only toy nodes.
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
commit/rollback state, and unrelated-node exclusions available at that work
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
acceptance of AI suggestions, general-purpose AI agent/RAG orchestration outside
the scoped authoring/reviewer roles,
document generation, premature incremental validation, a graph database, a web
platform, premature plugin packaging, and visual editors until an evidence gate
authorizes them. The authoring agent, reviewer, and later headless MCP/plugin are
specific interfaces over Application contracts, not general AI orchestration.

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
narrow/pivot, or stop. Gate C AI authoring/intake is the intended product
continuation after Gate A and an explicit Gate B decision (implemented or
omitted), but it still requires a later human planning request.
Do not plan or implement more work without a new human prompt.

When Current task is `None`, do not invent work. Report that the planned work is
finished, summarize the recorded outcome and optional future ideas, ask what the
human wants next, and stop.

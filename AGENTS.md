# ValidatedWorld Agent Instructions

## Project purpose

ValidatedWorld is a .NET 10 semantic change-control application for one simple
human-readable dependency graph stored in an embedded SQLite `.vw.db` file.
Nodes contain stable IDs and meaningful text. Edges contain stable IDs, endpoint
IDs, a relationship label, and an explicit direction in which changes propagate
for review.

The application computes the complete modeled affected set for an in-memory
operation batch, explains its paths, requires every selected node to be examined,
and atomically writes the new current graph or nothing. A human or optional AI
judges whether natural-language content remains semantically consistent.
“Validated” does not mean the C# engine proves prose true.

Every project has one purpose root—the project thesis—and every other node has
one `scope-parent`, forming a spanning tree inside the semantic multigraph. Every
changed or affected node's complete upward lineage through that tree, including
the root, is mandatory semantic review context. Context ancestors never become
propagation seeds or fan into siblings. Directly changing a scope selects its
descendants; directly changing the purpose selects the project.

The database stores current state only. It has no project revision history,
persisted drafts, validation ledger, commits, replay, or JSON snapshot. An active
change session is application memory and is lost on process exit. A deterministic
current-state fingerprint prevents a stale approved proposal from being applied;
it is integrity metadata, not version history.

AI authoring is the strongly intended experience but is optional. The built-in
authoring agent searches and operates bounded Application tools; after exact
conversational approval it calls a guarded commit tool. A separate optional
reviewer returns heuristic concerns. Missing configuration falls back to the
complete manual text-oriented workflow.

## Required reading and authority

Before product, schema, persistence, or public API decisions, read in order:

1. `docs/feasibility.md`
2. `docs/validated_world_authoring_spec.md`
3. `docs/implementation_blueprint.md`
4. `docs/testing_and_qa.md`
5. `docs/implementation_execution_plan.md`

For optional AI work also read `docs/ai_semantic_review.md` and
`docs/ai_authoring_agent.md`. Consult `docs/prior_art_and_positioning.md` before
broadening the product.

Human instructions override repository documents. If evidence invalidates a
design, update controlling documents and explain it.

## Current task and handoff

`docs/implementation_execution_plan.md` contains exactly one Current task.

- A human prompt starts each agent run.
- Implement only Current task. Do not begin or delegate later work.
- On success, record exact evidence, replace Current task with a fully specified
  next task, report to the human, and stop.
- On failure or repeated non-progress, leave Current task unchanged, report the
  failing evidence and attempted repairs, and stop.
- Leave enough repository state that the next agent needs no chat history.

Routine reversible implementation decisions do not need human approval. A
material product/scope choice does.

## Git boundary

Do not create/switch branches, stage, commit, merge, rebase, cherry-pick, revert,
reset, clean, stash, tag, pull, push, open PRs, or alter Git configuration.
Read-only status/diff/log are allowed. Preserve unrelated human changes and leave
edits unstaged.

“Commit” elsewhere in these docs means a ValidatedWorld SQLite transaction.

## Repository structure

- `src/ValidatedWorld.Core` — immutable simple graph and change/review domain.
- `src/ValidatedWorld.Serialization` — strict command/result JSON and private
  deterministic fingerprint encoding; no project snapshot format.
- `src/ValidatedWorld.Validation` — indexes, scope rules, affected traversal,
  dispositions, and optional profile ports.
- `src/ValidatedWorld.Application` — project/query/in-memory-change/commit use
  cases and persistence ports.
- `src/ValidatedWorld.Persistence.Sqlite` — four-table schema, migrations,
  mapping, atomic writes, views, backup/export.
- `src/ValidatedWorld.Cli` — text/JSON command host and composition root.
- `src/ValidatedWorld.AiReview*` — optional later reviewer; not yet created.
- `src/ValidatedWorld.AiAuthoring*` — optional later author; not yet created.
- `tests/ValidatedWorld.TestKit` — planned reusable app/CLI fixture helpers.
- `tests/ValidatedWorld.EndToEnd.Tests` — planned black-box scenarios.
- `samples/TechnicalProject` — first plain-graph proof source.

## Durable design rules

- Treat `.vw.db` as the sole complete project and interchange format.
- Use a fixed four-table SQLite v1: `schema_migrations`, `projects`, `nodes`,
  `edges`. Do not add package/type/draft/validation/commit/history tables.
- Make node text and edge relationship/review direction the required semantic
  surface. Kinds/tags/scalar attributes are optional.
- Keep optional profiles out of the common graph requirement. Unknown kinds and
  labels are valid in a profile-free project.
- Keep every graph-relevant connection an explicit edge. Do not scan attributes
  for ID references or infer dependencies from labels/text.
- Derive review arcs from each edge's declared direction. Use the union of base
  and proposed arcs for affected analysis.
- Preserve singular scope context without sibling fan-out. Only direct scope
  operations select descendant subtrees.
- Include every changed/affected node's complete scope-upstream lineage through
  the purpose root in the review surface. Missing context coverage is
  inconclusive; context alone does not require editing or seed propagation.
- Keep upstream context editable. Once edited, an ancestor becomes a direct seed
  and may expand the affected set through its subtree, up to the full project for
  a root edit.
- Treat missing facts/links as unknown.
- Hold unfinished operations/reviews only in application memory. Warn on exit
  where possible; do not promise recovery.
- Use an opaque deterministic state fingerprint solely for integrity and stale
  detection. Do not expose revision numbers, parent hashes, history, replay, or
  JSON snapshots.
- Author only through change sessions. No partial/cascade canonical writes.
- Require current dispositions for every affected node. Session changes stale
  mismatched dispositions.
- Report valid, invalid, or inconclusive with evidence. Do not call semantic
  human/AI judgment proof.
- Keep CLI and app usable with no AI, secret, network, SQL knowledge, or GUI.
- Let verified external integrations read documented SQLite views. Never expose
  direct canonical SQL writes.
- Keep SQLite, JSON, provider, UI, and profile implementations out of Core.
- Keep web/UI/MCP/plugin/images/OCR/multi-agent/document generation outside the
  current roadmap.

## SQLite rules

- Use `Microsoft.Data.Sqlite.Core` with an explicitly pinned/audited SQLitePCLRaw
  native bundle; no ORM.
- Enable/verify foreign keys on every connection.
- Use STRICT tables, parameters, checked migrations, conservative limits, and
  `ON DELETE RESTRICT`.
- Never enable extensions or execute stored project text as SQL.
- SQLite is embedded. Runtime/tests require no server, external `sqlite3`, system
  SQLite, or Docker.
- The app owns create, verify, backup, sample generation, and any SQL export.
- Keep final writes short; never wait for human/AI while holding a transaction.
- Treat supplied databases as untrusted until application ID, migration,
  integrity, mapping, scope, and fingerprint checks pass.

## Optional AI safety

Normal build/test is offline and uses fakes/scripted HTTP. The planned production
provider is OpenAI only. If AI flags are disabled or `OPENAI_API_KEY` is absent,
runtime reports the feature unavailable and continues manually.

Never persist/log credentials. Use .NET user-secrets for local source development
and process environment for published use. `.env` remains ignored and is not
loaded automatically.

An agent may not begin Gate B provider/client/prompt-submission implementation
unless the initiating human prompt contains:

```text
AI_REVIEW_SECRET_READY: yes
```

A live review additionally requires:

```text
AI_REVIEW_LIVE_CALL_AUTHORIZED: yes
```

An agent may not begin Gate C provider/tool-loop implementation unless the prompt
contains:

```text
AI_AUTHORING_SECRET_READY: yes
```

A live authoring evaluation additionally requires:

```text
AI_AUTHORING_LIVE_CALL_AUTHORIZED: yes
```

Never search for, acquire, infer, list, copy, or set a key. Never accept a pasted
key in tracked files. With live authorization, use the explicitly named
evaluation only, background polling, a 1,200-second end-to-end deadline, and zero
automatic paid retries. Polling the same response/tool continuation is not a
retry.

The authoring model may mutate only its in-memory proposal through strict tools.
It has no SQL/direct write/automatic disposition/unguarded commit. Bind final
approval to exact database identity, base/operation/proposed/affected
fingerprints, completed review, and short expiry.

## Testing and QA

Every engineering task uses:

1. focused unit/property tests;
2. realistic integration and scripted end-to-end tests as soon as supported; and
3. from WP3 onward, an actual AI-agent black-box walkthrough through public
   commands against an app-generated temporary database.

The QA agent is acting as an ordinary user; it is not a live provider test. It
must report completion, semantic result, affected paths, unrelated exclusions,
confusion, burden, and confidence. Deterministic defects become regression tests.
A serious usefulness/direction concern blocks silent roadmap advancement.

Tests create disposable databases through public app/CLI paths. Do not commit
populated samples or user databases. A binary `.vw.db` is allowed only as an
explicit byte-specific corruption fixture under `tests/` with provenance and
regeneration notes.

Before completing a coding change:

1. Add meaningful tests; documentation-only changes need no artificial tests.
2. Update scenario generators and structured goldens.
3. Run task acceptance, realistic scenario, and applicable QA walkthrough.
4. Run full restore/build/test.
5. Update the execution plan with evidence and the exact next task.
6. Report unverified/inconclusive/usability concerns to the human and stop.

## Definition of done

A change is complete only when acceptance criteria, changed-behavior coverage,
full build/tests, applicable generated database/goldens, realistic workflow,
affected-review rules, execution-plan handoff, and truthful guarantee language
all agree.

When Gate A ends, set Current task to `None`, report the evidence, and ask whether
the human wants to stop, narrow, or separately plan optional AI review/authoring.
Do not invent profiles or later integrations automatically.

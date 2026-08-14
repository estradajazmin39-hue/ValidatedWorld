# Testing, Fixtures, and Agent QA

**Status:** Normative testing strategy

**Last reviewed:** 2026-08-13

## 1. Principles

ValidatedWorld is complete only when both correctness and practical usefulness
are exercised.

- Automated tests prove repeatable structural, traversal, transaction, and
  persistence behavior.
- Realistic scripted scenarios prove public workflows and exact graph results.
- A black-box AI agent acting as an ordinary user evaluates discoverability,
  burden, diagnostics, and confidence.
- Live provider evaluation is a separate optional experiment and never part of
  the normal build/test completion path.

Documentation-only changes require consistency/build verification but no
artificial production tests.

## 2. No external database dependency

SQLite is embedded in process through pinned NuGet packages. Tests and users do
not need a SQLite server, standalone `sqlite3`, system-native installation, ORM,
or Docker.

The application creates every normal test/QA database through public
Application/CLI paths. Tests do not populate canonical tables through ad hoc SQL.
A binary `.vw.db` may be tracked only under `tests/` when a byte-specific
corruption/application-header/migration case cannot reasonably be generated at
runtime. Such a fixture documents purpose, provenance, expected schema version,
and regeneration.

All temporary databases live in per-test or per-walkthrough directories and are
disposed afterward. No populated sample/user database belongs in the repository.

## 3. Reusable scenario source

`samples/TechnicalProject` grows into the primary soft-logic corpus. It retains:

```text
README.md                     human scenario and modeling rationale
source/project.yaml-or-json   reviewed source used by fixture builder
changes/*.json                operation batches
expected/*.json               structured public result goldens
goals/*.md                    agent-facing goals without expected solution
```

The source serialization is test/sample input, not a supported project
interchange format. Fixture generation passes it through public graph/domain and
Application creation paths to produce a disposable `.vw.db`.

The corpus contains plain text nodes (no domain profile required):

- one project purpose;
- power, privacy, documentation, and accessibility scopes;
- requirements, definitions, assumptions, observations, evidence, results,
  decisions, conclusions, implementation, and verification as optional node
  kinds;
- document/artifact anchors;
- labeled relationships with explicit review directions;
- changed/removed/redirected relationships;
- missing information and explicit conflicting text for a semantic reviewer;
- unrelated distractors and cross-branch links; and
- direct scope and purpose changes.

Each deterministic defect discovered later becomes a retained scenario variant
or focused regression fixture. Do not reinvent a baseline database every agent
turn.

## 4. Test projects and helpers

Planned shared infrastructure:

- `tests/ValidatedWorld.TestKit` — fixed clocks/IDs, scenario builders, temporary
  directories, app-generated database factories, process host, result parsing,
  fault injection, and structured golden comparison.
- `tests/ValidatedWorld.EndToEnd.Tests` — black-box CLI/long-lived-host scenarios.

Production test layers mirror the architecture:

1. Core unit/property tests.
2. Serialization command/result and private fingerprint tests.
3. Validation/index/affected-set tests.
4. Application in-memory-session and orchestration tests.
5. SQLite migration/mapping/integrity/fault/backup/view tests.
6. CLI/host contract tests.
7. Scenario and performance tests.

Tests control clocks, IDs, scheduling, bounds, fault points, current directory,
environment variables, and process lifetime. Empty, tautological, skipped,
flaky, or manually inspected tests are not completion evidence.

## 5. Work-package QA progression

### WP1 — Core shape

Construct the complete TechnicalProject graph through public Core APIs with no
profile. Assert node/edge data, purpose/scope intent, directions, operations,
and focus expansion. Record whether plain graph entry is understandable.

### WP2 — Structured commands and fingerprints

Round-trip strict command/result DTOs and prove deterministic private state and
operation fingerprints. Explicitly prove there is no supported full-project JSON
snapshot/import/export contract.

### WP3 — First database and public read flow

Create/open/verify/backup a real four-table `.vw.db`, generate the sample through
the app, inspect documented views, and run the first black-box agent walkthrough.
Publish/run from a clean temporary directory to prove bundled SQLite behavior.

### WP4 — Validation and graph navigation

Exercise missing endpoints, purpose/scope failures, malformed directions,
fingerprint mismatch, optional-profile unavailability, dependencies, and paths.
The QA agent diagnoses cases only from public output.

### WP5 — In-memory authoring

Exercise the long-lived command host: begin/apply/replace/discard/restart.
Prove unresolved sessions never appear in SQLite and restart loses them without
changing canonical state. The QA agent authors a proposal without SQL.

### WP6 — Affected set and manual review

Exercise current/proposed union, deleted/redirected edges, upward/lateral/both
directions, scope ancestor context, direct subtree/root selection, dispositions,
and staleness. The QA agent completes a realistic review and comments on burden.

### WP7 — Atomic current-state commit

Inject failures before/after every mutation boundary. Assert exact prior rows and
fingerprint survive. Exercise stale base, busy writer, constraint/mapping error,
successful commit, and understandable retry/review behavior. Assert no history,
draft, operation, review, or commit rows exist.

### WP8 — Complete manual product

Run init → search → change → affected review → validate → commit → verify →
backup from public help. Test large/bounded queries and, if retained, SQL
export/import round trip. Evaluate manual usability with no AI/key/network/GUI.

### WP9 — Gate A evidence

Run fresh correctness, modeling-cost, affected precision/recall, Doorstop/plain
SQLite comparison, lower-cost-agent workflow, and performance evaluations.
Record continue/narrow/pivot/stop evidence.

## 6. Required deterministic properties

Tests prove at least:

- global node/edge identity and endpoint existence;
- one purpose and singular acyclic scope paths;
- profile-free graphs accept unknown kinds and relationship labels;
- edge direction—not label or FK orientation—defines review arcs;
- C# arcs equal documented SQLite view arcs;
- current/proposed union preserves old and new consequences;
- ancestor context never fans into sibling subtrees;
- direct scope/root operations select descendants;
- shortest paths and ordering are deterministic;
- bounds return inconclusive with explicit omissions;
- proposal edits stale mismatched dispositions/AI approval;
- closing/restarting discards in-memory proposals and leaves SQLite unchanged;
- insertion order/SQLite byte differences do not change state fingerprint;
- external/incomplete mutation causes fingerprint/integrity failure;
- every injected commit failure preserves prior current rows;
- successful commit stores only the expected current graph and new fingerprint;
- backup opens to the identical graph/fingerprint;
- standard JSON commands never become a complete project interchange format;
- no normal workflow contacts a provider; and
- absent AI configuration exposes manual fallback, not project invalidity.

## 7. Black-box agent walkthrough

From WP3 onward the same locally running coding agent performs a clearly
separated QA-user pass after implementation tests. It receives:

- built CLI/host and public help/development guide;
- a fresh app-generated temporary database;
- a realistic goal; and
- no expected command sequence, private API, source-code hint, or permission for
  canonical SQL writes.

Record concisely at `docs/qa/wpN-agent-walkthrough.md`:

```text
work package/build
goal and supplied artifacts
public commands/views used
completion or stopping point
resulting graph/fingerprint/affected set where applicable
unrelated-node exclusions
confusing/misleading/missing behavior
modeling and review burden
confidence and recommendations
```

Do not store hidden chain-of-thought or an unbounded transcript. Preserve
commands/inputs/structured outputs in replayable tests.

If the agent cannot complete a documented workflow, requires source knowledge,
misreads success, misses required affected nodes, or changes unrelated data, the
work package fails. Fix deterministic defects and add regression tests. Report a
fundamental usefulness concern to the human before advancing.

## 8. Optional AI-review tests

Normal Gate B tests use a fake client and scripted HTTP and remain offline.
They cover:

- exact complete request/coverage manifest;
- all disjoint proposal chains in one request;
- singular purpose lineage without sibling fan-out;
- structured citations and rejection of unknown IDs;
- concern disposition and staleness in the in-memory session;
- disabled mode and missing-key manual fallback;
- provider failure/refusal/timeout/malformed output as inconclusive fallback;
- no model mutation/direct write;
- credential/private-content redaction; and
- one background response with zero automatic retries.

`VW_AIREVIEW__LIVETESTS=true` only makes the separately invoked live evaluation
eligible. It does not authorize spend; the exact prompt attestations are still
required. Live evidence measures useful concerns, false positives/negatives,
scope coverage, cost, and latency.

## 9. Optional AI-authoring tests

Normal Gate C tests use scripted tool-call responses. They cover:

- a new plain graph from a description/text source;
- a change in a project far larger than the scripted model working set;
- deterministic search before create and duplicate avoidance;
- one process-local proposal with no persistence/restart recovery claim;
- questions for material ambiguity;
- correct affected-set iteration and manual/optional-review handoff;
- no SQL/direct write/automatic dispositions;
- exact conversational approval and guarded model-called commit;
- stale proposal/database invalidating approval;
- disabled/missing-key manual fallback;
- limits preserving canonical state and reporting remaining work; and
- text-only scope—no image/OCR/MCP/plugin/multi-agent behavior.

`VW_AIAUTHORING__LIVETESTS=true` only enables the separately invoked live
evaluation. It still requires exact readiness/live-call attestations, background
polling, a 1,200-second deadline, and no automatic paid retry.

Gate C fails if the agent does not materially reduce graph/review burden or
creates plausible unrelated state. The manual Gate A product remains valid.

## 10. Completion evidence

Every coding assignment records:

- task-specific tests and exact results;
- full restore/build/test results;
- generated scenario/golden changes;
- black-box QA outcome when applicable;
- modeling/usability concerns;
- unverified/inconclusive behavior; and
- the next single task in the execution plan.

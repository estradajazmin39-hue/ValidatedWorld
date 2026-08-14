# ValidatedWorld Implementation Plan

**Last updated:** 2026-08-13

**Current task:** WP1 — simple graph domain

This is the living handoff between coding agents. The
[implementation blueprint](implementation_blueprint.md) defines normative
behavior; this file records actual progress and exactly one authorized task.

## 1. Required agent workflow

When a human asks an agent to continue implementation:

1. Read `AGENTS.md`, the required product documents, this entire file, and the
   blueprint sections named by Current task.
2. Inspect source, tests, and human changes. Preserve unrelated work.
3. Implement only Current task.
4. Add deterministic tests and applicable realistic scenario evidence.
5. Run task-specific acceptance and then:

   ```powershell
   dotnet restore ValidatedWorld.slnx
   dotnet build ValidatedWorld.slnx --no-restore
   dotnet test ValidatedWorld.slnx --no-build --no-restore
   ```

6. On success:
   - add exact evidence under Completed work;
   - replace Current task with one fully specified next package;
   - report result, QA/usability findings, uncertainty, and next task to the
     human; and
   - stop without starting the next task.
7. On failure or repeated non-progress:
   - leave Current task unchanged;
   - report commands, relevant output, likely cause, and repairs attempted; and
   - stop.

Agents do not perform Git operations. A human reviews and manages the working
tree.

## 2. Product baseline established before coding

The approved design now says:

- A project is one simple human-readable dependency graph in a `.vw.db` file.
- Nodes require stable ID/text; kinds/tags/scalar attributes are optional.
- Edges require stable ID/endpoints/relationship/review direction.
- One purpose and one scope-parent spanning tree organize the graph.
- Domain profiles are optional validator/helper modules, not foundational schema.
- The database contains current state only in four tables: migrations, project,
  nodes, edges.
- There is no JSON project snapshot, revision history, commit/replay ledger, or
  persisted unfinished draft.
- One process-local in-memory change session projects operations, computes the
  complete current-plus-proposed affected set, and records temporary review
  dispositions.
- An opaque current-state fingerprint provides integrity/stale protection without
  retaining versions.
- Complete manual text-oriented operation is required. Optional AI authoring is
  the intended convenience and optional independent AI review is heuristic.
- Missing AI configuration falls back to manual operation.
- Images/OCR, MCP/plugin, GUI/web, multi-agent coordination, document generation,
  and domain-profile implementations have no current work package.

Do not reintroduce assumptions from specification/blueprint versions before 8.0/
7.0, including nine tables, schema packages, mandatory typed properties, durable
drafts, logical JSON snapshots, project revisions, parent hashes, commit history,
replay, required-provider policy, catalog/narrative profile gates, or MCP plans.

## 3. Completed work

### WP0 — architecture scaffold

Completed before the current design revision:

- Added .NET 10 projects for Core, Serialization, Validation, Application,
  Persistence.Sqlite, CLI, and their test projects.
- Added project references and `ValidatedWorld.slnx`.
- Added the pinned SQLite package/runtime choices in the persistence scaffold.
- Added initial placeholder tests.
- Established repository/agent workflow, no-Git boundary, generated fixture
  policy, offline tests, black-box QA requirement, and failure-loop handoff.

No substantive WP1 production model was implemented, so replacing the earlier
typed/versioned design loses no production work.

### Documentation redesign — current simple-graph vision

Completed 2026-08-13:

- Replaced the typed schema-package/versioned-ledger product with the simpler
  text-node/labeled-edge dependency graph requested by the human.
- Made profiles optional aids and plain graph operation the Gate A proof.
- Reduced planned SQLite v1 from nine tables to four current-state tables.
- Removed project history, persisted drafts, replay, and JSON snapshot
  interchange; retained only a private current-state fingerprint.
- Defined process-local change sessions, base/proposed union affected traversal,
  session-only dispositions, scope context rules, and atomic current-state write.
- Made AI authoring/review optional with complete manual fallback and text-only
  initial intake.
- Removed image/OCR, MCP/plugin, graphical UI, multi-agent, hosted, and domain-
  profile implementations from the current roadmap.
- Moved contributor/development detail removed from the human README into
  `docs/project_development.md`.
- Verification passed on 2026-08-13: every relative Markdown link target exists,
  every Markdown code fence is balanced, Markdown prose lines are within 120
  characters, `git diff --check` found no whitespace errors, restore succeeded,
  build succeeded with 0 warnings/0 errors, and all 5 scaffold tests passed.

## 4. Current task

### WP1 — simple graph domain

**Blueprint references:** Sections 2–4, 8, 9.1, 9.5, 13, 15, 16, and 17.

**Goal:** Implement the database-independent immutable domain needed to express a
plain human-readable graph and its in-memory change/review contracts. Do not add
SQLite, JSON DTOs, hashing implementations, traversal algorithms, AI, or domain-
profile behavior yet.

**Production scope:**

- `ProjectId`, `EntityId`, `ChangeSessionId`, and `ProfileId` with ordinal
  validation/comparison.
- Scalar values: text, integer, canonical decimal, boolean, symbol, UTC instant.
- `GraphNode`: ID, required text, optional kind, sorted unique tags, scalar
  attributes.
- `GraphEdge`: ID, source, target, required relationship, `ReviewDirection`,
  optional rationale/tags/attributes.
- `EnabledProfile` reference only; no profile implementation or required
  profile.
- `ProjectGraph`: project/title/purpose/profiles/nodes/edges/state-fingerprint
  contract. State fingerprint is an opaque value here; WP2 computes it.
- Add/replace/remove node and edge operation contracts with one target ID.
- Review-disposition values/contracts and fingerprints as opaque values.
- Focused batch/cluster input and explicit expansion result contracts. Core may
  deterministically add only missing scope-parent operations from an explicit
  focus; it never infers other relationships.
- In-memory `ChangeSession` state/contracts sufficient for Application to own the
  later lifecycle, without persistence or database references.
- Common local shape validation that does not require graph-wide endpoint/scope
  traversal. WP4 owns full graph validation.

**Tests:**

- Every ID/value valid and rejected boundary.
- Empty node text/relationship, malformed direction/data, duplicate/unsorted
  tags/attribute keys, and target/entity-kind mismatches.
- Node/edge IDs share one conceptual identity space in graph construction.
- A profile-free graph is representable; unknown kinds/relationships are not
  rejected.
- Operations preserve stable target/entity IDs and cannot masquerade between
  node/edge operation kinds.
- Batch focus expands missing scope parents explicitly, preserves supplied ones,
  rejects ambiguity, and never invents a semantic cross-link.
- Disposition/status/session transitions reject impossible local states.
- Build the complete TechnicalProject baseline through public Core APIs with
  purpose, sibling scopes, plain-text technical concepts, semantic cross-links,
  and external anchors. No test-only escape hatch or profile is permitted.

**Exclusions:**

- no old `SchemaPackage`, type-definition, per-entity revision, `ProjectHead`,
  commit/replay, or logical-snapshot domain;
- no SQLite schema/mapping;
- no JSON DTOs or fingerprint algorithm;
- no graph index, purpose/scope traversal, affected-set algorithm, or validator;
- no persistent session repository;
- no CLI behavior, provider, profile implementation, or later-gate project.

**Acceptance:**

1. Public Core APIs express the realistic plain graph without an enabled profile.
2. Unit/property tests cover every local valid/rejected shape and operation.
3. Focus/cluster expansion returns only explicit scope-parent additions.
4. Core has no SQLite/JSON/provider/UI dependency.
5. Modeling friction—especially ID, edge direction, and attribute burden—is
   recorded under Completed work.
6. Full restore/build/test succeeds with zero warnings.
7. This plan records WP1 evidence and replaces Current task with a complete WP2
   assignment.
8. The agent reports to the human and stops without starting WP2.

## 5. Remaining roadmap order

1. WP2 — command/result JSON and internal current-state fingerprints.
2. WP3 — four-table SQLite, mapping, views, backup, samples, and first CLI/read QA.
3. WP4 — graph indexes, structural validation, review arcs, and diagnostics.
4. WP5 — process-local change sessions, projection, interactive JSON host.
5. WP6 — affected-set traversal and complete manual review.
6. WP7 — atomic current-state commit and rollback faults.
7. WP8 — queries, interoperability, limits, help, and complete manual workflow.
8. WP9 — Gate A correctness/usefulness/comparison/performance evaluation.

After WP9, set Current task to:

```text
None - the planned Gate A roadmap is complete; human direction required.
```

Only a later explicit human request may plan Gate B optional AI review or Gate C
optional AI authoring. No other phase is currently planned.

## 6. Human report format

End every coding run with:

```text
Task:
Outcome:
Implemented:
Tests and verification:
Realistic scenario / agent QA:
Modeling or usability findings:
Remaining uncertainty:
Execution plan updated to:
```

# Project Development Guide

**Status:** Contributor and coding-agent guide

**Last reviewed:** 2026-08-13

This file contains the development, roadmap, testing, and handoff material that
is intentionally kept out of the human-facing README.

## Design documents

Read in this order before implementing:

1. [Feasibility and limits](feasibility.md)
2. [Product and architecture specification](validated_world_authoring_spec.md)
3. [Implementation blueprint](implementation_blueprint.md)
4. [Testing, fixtures, and agent QA](testing_and_qa.md)
5. [Implementation execution plan](implementation_execution_plan.md)

Additional references:

- [Optional AI semantic review](ai_semantic_review.md)
- [Optional AI authoring](ai_authoring_agent.md)
- [Related systems and product position](prior_art_and_positioning.md)

`AGENTS.md` is the concise mandatory instruction file. The product specification
controls direction, the blueprint controls intended code behavior, and the
execution plan controls the one current assignment.

## Core manual workflow

```text
open project.vw.db and verify its schema, integrity, scope, and state fingerprint
→ begin one process-local in-memory change session
→ add, replace, or remove human-readable nodes and explicit labeled edges
→ build the proposed graph without changing SQLite
→ derive review arcs from current and proposed edge directions
→ compute the complete affected set and explanation paths
→ require every changed/affected node's full upstream lineage through the thesis
  as semantic context, without sibling fan-out
→ update affected nodes or mark them reviewed-no-change/not-applicable
→ run full structural and any enabled optional-profile validation
→ atomically apply the new current graph or roll back everything
→ discard all session-only operations/review metadata
```

No provider is needed. No unfinished session is persisted. The SQLite file keeps
only the resulting current state.

## Authoring without edge-entry drudgery

The stored graph remains explicit while input may be concise:

- an active focus node can supply `scope-parent` for new nodes in a batch;
- a cluster is an ordinary scope node plus its children in one batch;
- a profile helper, if enabled, may expand a known pattern into normal operations;
- the application always returns the complete expanded operation list before
  affected analysis or commit; and
- deterministic code never guesses non-scope semantic relationships from prose.

Search and navigation are required before AI authoring: exact/text/kind/tag/scope
search, scope children/ancestors/subtree, semantic neighbors, review dependencies
and dependents, explanation paths, and bounded context. These are indexed graph
queries, not embeddings, RAG, natural-language SQL, or provider calls.

Because an in-memory session must span multiple commands, the CLI includes a
long-lived newline-delimited JSON host (or equivalent process-local command
loop). One-process-per-command persistence must not be simulated by storing a
draft in `project.vw.db`.

## Optional AI semantic review

Gate B may add an expensive “lore-team” pass after the manual core proves useful.
One request contains the complete operation batch, every selected affected chain,
required explanatory context, singular purpose lineages, and a coverage/omission
manifest. Disjoint chains stay together.

An included ancestor is context, not a propagation seed. A local leaf review does
not load sibling branches. A direct scope-node change selects its descendant
subtree, and a direct purpose-root change is the deliberate full-project case.

The model returns cited `Concern` records. It cannot edit the graph. A user or
authoring loop may repair the proposal, reject a concern with rationale, or
acknowledge it. The guarantee is only that the optional review occurred with the
displayed scope; model judgment remains heuristic.

The reviewer can be disabled and is automatically unavailable without an API
key. That never blocks the manual workflow. The initial production path is one
OpenAI configuration, one background response, a 1,200-second end-to-end
deadline, and zero automatic paid retries. Normal tests are offline.

See [ai_semantic_review.md](ai_semantic_review.md) for the exact later gate.

## Optional AI authoring

Gate C may add the intended conversational experience. The authoring agent
searches, navigates, opens one in-memory change session, applies explicit
operations, inspects affected expansion, asks material questions, and repairs the
proposal. After the user approves the exact final preview, the agent calls a
guarded commit tool bound to the current database and proposal fingerprints.

The model has no raw SQL or unguarded write path. The initial intake surface is a
description plus explicit text. Images/OCR, MCP/plugin packaging, a graphical UI,
and multi-agent partitioning are not in the roadmap.

The authoring agent is optional and unavailable without configuration. The same
text-based application tools remain usable by a human.

See [ai_authoring_agent.md](ai_authoring_agent.md).

## Human-invoked, agent-executed implementation

The blueprint is implemented one work package at a time. Agents do not choose a
package from the backlog or infer one from chat. They read the execution plan,
implement its single Current task, and update that plan in the same local task.

A human prompt starts each run. The agent completes the assignment or reports why
it failed, tells the human the result, and stops. It does not automatically start
the next task or launch another agent.

Every successful assignment leaves:

- production behavior for its bounded scope;
- meaningful automated tests using realistic connected data;
- passing task checks plus full solution build/tests;
- updated scenario generators and structured goldens where applicable;
- an execution-plan entry with exact evidence and a fully specified next task;
- a report of remaining inconclusive behavior or usability concerns; and
- a final human handoff followed by a stop.

Agents do not manage Git. They may inspect status/diffs but do not branch, stage,
commit, merge, rebase, reset, stash, pull, push, or open pull requests.

If materially different repairs keep returning to the same failure, the agent
leaves Current task unchanged, reports commands/output/likely cause/attempts, and
stops. It does not weaken acceptance criteria or ask a human to perform a routine
test that should have an automated oracle.

## Testing actual usefulness

Passing unit tests is not enough. The reusable TechnicalProject corpus contains
plain-text requirements, assumptions, evidence, decisions, conclusions,
implementation, verification, anchors, contradictions, missing information, and
unrelated branches.

WP3 produces the first application-generated database and public read workflow.
From WP3 onward, each package requires:

- a replayable scripted end-to-end scenario asserting graph state, affected set,
  paths, review status, rollback/current-state behavior, and unrelated
  exclusions supported at that stage; and
- an actual AI-agent black-box walkthrough against a fresh temporary database,
  starting from public help and commands as a QA user would.

The QA agent reports whether it completed the realistic goal, what was confusing
or misleading, how much graph/review work was required, and whether the product
felt useful. It must not use private APIs or direct canonical SQL writes.
Deterministic defects become regression tests. A serious product-direction or
modeling-cost concern is reported immediately and prevents silent advancement.

SQLite requires no server. The pinned NuGet bundle supplies its native runtime.
Users and QA agents do not install `sqlite3`, understand DDL, or run Docker. The
application creates, verifies, backs up, samples, and optionally exports its own
database.

Realistic source data is authored once and retained as reviewed sample/test
assets. Disposable `.vw.db` files are regenerated through public application
paths. New regressions become reusable scenario variants rather than throwaway
databases invented each turn.

## Roadmap

Gate A consists of WP0-WP9:

1. WP0 — solution scaffold (complete).
2. WP1 — simple graph domain.
3. WP2 — command/result serialization and internal fingerprints.
4. WP3 — four-table SQLite and first public walking skeleton.
5. WP4 — indexes and structural validation.
6. WP5 — process-local change sessions and projection.
7. WP6 — affected-set analysis and manual review.
8. WP7 — atomic current-state commit.
9. WP8 — complete queries, interoperability, and host polish.
10. WP9 — correctness, usefulness, comparison, and performance evaluation.

After WP9, Current task becomes:

```text
None - the planned Gate A roadmap is complete; human direction required.
```

The agent reports whether evidence supports continuing, narrowing, pivoting, or
stopping and asks the human what to do. It does not automatically plan another
phase.

Optional later work requires a separate human request:

- Gate B/WP10 — semantic AI reviewer;
- Gate C/WP11 — AI authoring agent.

Profiles, image/document ingestion, MCP/plugin packaging, graphical UI, web
hosting, multi-agent coordination, finished-document generation, narrative
analysis, and interactive-state exploration have no current work package.

## Current status

The repository is a .NET 10 scaffold plus an implementation-ready design. WP0 is
complete. WP1, the simple graph domain, is Current task. No production feature is
yet implemented.

Build and test:

```powershell
dotnet restore ValidatedWorld.slnx
dotnet build ValidatedWorld.slnx --no-restore
dotnet test ValidatedWorld.slnx --no-build --no-restore
```

Use `ValidatedWorld.slnx`, not `ValidatedWorld.sln`.

## Completion

An assignment is done when its acceptance criteria and full repository checks
pass, changed behavior is covered, scenario/QA evidence is recorded, the
execution plan names the next task, and the agent reports to the human.

If Current task is `None`, an invoked agent makes no changes. It reports the
recorded outcome and optional ideas, asks for explicit direction, and stops.

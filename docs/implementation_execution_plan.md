# ValidatedWorld Implementation Plan

**Last updated:** 2026-08-13

**Current task:** WP1 - common graph domain

This file tells a coding agent what has been finished and what to do next. The
[implementation blueprint](implementation_blueprint.md) defines the design and
ordered work packages. This plan records actual progress.

Fixture/database generation and QA follow
[testing_and_qa.md](testing_and_qa.md). In particular, the application—not the
user, agent, test runner, or an external SQLite tool—creates every normal
workspace database.

The repository is developed by one human-invoked agent at a time. There is no
parallel-work, branch-management, or unattended-agent workflow to coordinate.

## 1. Simple working procedure

When a human asks an agent to continue implementation:

1. Read `AGENTS.md`, the required product documents, this entire file, and the
   blueprint sections named by Current task.
2. Inspect the existing source, tests, and fixtures. Work with the directory
   exactly as the human supplied it and preserve unrelated changes.
3. Implement only Current task. Do not pull later work forward.
4. Add meaningful deterministic automated tests with the behavior.
5. Run the task-specific acceptance checks and then:

   ```powershell
   dotnet restore ValidatedWorld.slnx
   dotnet build ValidatedWorld.slnx --no-restore
   dotnet test ValidatedWorld.slnx --no-build --no-restore
   ```

6. If the task succeeds:
   - add it to Completed work with exact verification evidence;
   - replace Current task with the next work package and give that task explicit
     scope, exclusions, and acceptance criteria;
   - report the result and next task to the human; and
   - stop. The human will review the changes and decide when to invoke another
     agent.
7. If the task cannot be completed or the agent starts cycling through the same
   failure:
   - do not mark it done and do not advance Current task;
   - report the failing command/test, relevant output, likely cause, and attempted
     fixes to the human; and
   - stop. The human may revert the local work and start a new conversation with
     another agent.

Routine coding choices do not require human approval. Use the simplest
reversible implementation consistent with the controlling documents and prove
it with tests. Ask the human only when work genuinely cannot proceed without a
product/scope decision or repeated repair attempts are not making progress.

## 2. Git rule

Agents do not manage Git. Do not create or switch branches, stage files, commit,
amend, merge, rebase, cherry-pick, revert, reset, clean, stash, tag, pull, push,
create pull requests, or change Git configuration. Leave local edits for the
human to review, commit, and merge.

Read-only inspection such as `git status`, `git diff`, or `git log` is allowed
when useful, but implementation must not depend on Git being available.
References elsewhere to a ValidatedWorld transaction `commit` mean an
application/SQLite operation, not a Git operation.

## 3. Automated acceptance

Every implementation task through WP8 must be verifiable without human
inspection, secrets, interactive UI, or mutable remote services.

The separately authorized post-Gate-A AI-review phase follows the same rule for
its normal suite by using a fake client and scripted HTTP. Its one OpenAI live
evaluation path is explicitly opt-in and obtains credentials only through the
human-controlled boundary in `docs/ai_semantic_review.md`.

The later AI-authoring/intake phase likewise uses fake/scripted model clients for
normal acceptance. Its provider path, text/image disclosure, tool limits, Gate B
handoff, and final commit confirmation follow `docs/ai_authoring_agent.md`.

- Unit tests cover accepted values, local invariants, and rejected inputs.
- Property/integration tests cover ordering, round trips, graph behavior,
  rollback, and atomicity where required by the blueprint.
- Tests control clocks, IDs, random seeds, concurrency/fault points, and
  environment-dependent limits.
- SQLite tests generate databases in per-test temporary directories by default.
- Source fixtures are scripts, logical snapshots, and expected JSON. Binary
  databases are exceptional test-only artifacts as defined in `AGENTS.md`.
- CLI scenarios are scripted and assert structured output plus exit codes.
- Skipped, flaky, tautological, network-dependent, or manually inspected checks
  are not acceptance evidence.

If a requirement lacks an automated oracle, creating that oracle is part of the
task. If it cannot be automated reliably, report it as inconclusive and do not
claim the task is done.

Automated correctness is necessary but not sufficient. Development also uses a
realistic TechnicalProject corpus and actual agent-operated usability checks.

### Realistic scenario requirement

Do not validate the product only with isolated nodes or toy arithmetic. The
checked-in source fixtures must grow into a plausible technical design containing
requirements, definitions, assumptions, evidence, claims, decisions,
implementations, verification, external document anchors, relevant dependency
paths, unrelated distractors, missing information, and explicit contradictions.

Every work package uses the largest realistic scenario its layer can support:

- WP1 constructs a representative graph through the public Core API and records
  any modeling awkwardness; no database or end-to-end product exists yet.
- WP2 represents the corpus as strict logical JSON and exercises realistic edits,
  malformed inputs, deterministic round trips, and package usability.
- WP3 creates the first real disposable `.vw.db`, exposes a minimal CLI walking
  skeleton, and begins black-box agent QA.
- WP4-WP8 repeat and expand the same workflows as validation, drafts, impact,
  review, commit/replay, query, and context features become public.
- WP9 performs the final comparison and product-value evaluation; it is not the
  first time usability is examined.

### Actual agent usability check

Starting with WP3, the implementing AI must perform at least one black-box QA
walkthrough as a user after deterministic tests pass. This is an actual agent
operating the built CLI against a temporary database freshly created by the app,
not merely another unit test. It must:

1. Begin from public README/CLI help and scenario instructions, not internal
   repositories or direct canonical SQL writes.
2. Attempt a realistic goal using soft-logic data, including relevant and
   irrelevant nodes rather than a preselected minimal graph.
3. Use only supported public commands and documented read-only SQLite views.
4. Record commands, structured results, whether the goal was completed, errors,
   confusing concepts, unnecessary steps, missing diagnostics, and confidence in
   what would happen next.
5. Convert every deterministic defect found into an automated regression test.
6. Summarize usability findings in Completed work and the human report.

The same locally running implementation agent performs a clearly separated
black-box QA-user pass; no parallel agent is needed. A scripted end-to-end test
remains required so later agents can replay the workflow exactly. WP9 supplies a
fresh whole-product perspective because it is a later human-invoked task.

An inability to finish the documented scenario, silent semantic mistakes,
source-code knowledge required to proceed, or misleading success output means
the task is not done. Fix the problem and rerun the scenario, or leave Current
task unchanged and report failure. Lesser friction may be recorded with a
specific recommendation, but must be reported to the human rather than hidden by
passing tests. If the finding calls the product direction or modeling burden into
question, stop and ask the human before advancing the plan.

## 4. Definition of done

### One task is done when

- all of its stated scope is implemented;
- its executable acceptance criteria pass;
- the full restore/build/test sequence passes without warnings;
- fixtures, goldens, documentation, and guarantees agree with the behavior;
- the realistic scenario for the current layer passes;
- when WP3 or later, an agent-operated black-box walkthrough and replayable
  scripted end-to-end test pass, with usability findings reported;
- this plan records the completed evidence and the next task; and
- the agent reports the result to the human and stops.

### The current roadmap is done when

WP0 through WP9 are complete and WP9 has recorded the Gate A correctness,
modeling-cost, comparison, lower-cost-agent, and performance evidence required by
the blueprint.

At that point, replace Current task with:

```text
None - the planned Gate A roadmap is complete; human direction required.
```

The finishing agent must report the outcome and ask the human whether to:

- call the project complete at its current scope;
- request a separate planning task for Gate B AI semantic review;
- narrow or pivot the design; or
- stop/archive the experiment.

It must not plan or implement another phase until the human explicitly asks.
Planning a later phase is itself one local task; implementation begins only in a
later human-invoked conversation.

If an agent is invoked while Current task is `None`, it makes no changes. It
reports that the planned work is finished, summarizes the recorded outcome and
remaining optional ideas, asks what the human wants next, and stops.

## 5. Completed work

### WP0 - architecture scaffold

- Added the six intended production projects and five matching test projects to
  `ValidatedWorld.slnx`.
- Added project references matching the blueprint boundaries.
- Pinned `Microsoft.Data.Sqlite.Core` and the audited
  `SQLitePCLRaw.bundle_e_sqlite3` dependency.
- Verified full restore/build/test: 0 build warnings; 5 scaffold tests passed.
- Limitation: these tests prove only assembly scaffolding. Production behavior
  and meaningful behavioral tests begin with WP1.

### Development workflow documentation

- Established this one-agent-at-a-time plan, automated acceptance requirements,
  no-agent-Git rule, mandatory human report, failure stop rule, and final-roadmap
  behavior.
- Required realistic TechnicalProject scenario testing at every layer and actual
  agent-operated black-box QA from WP3 onward, with deterministic findings
  converted to regression tests and usability/product concerns reported.
- Moved the first database/CLI walking skeleton to WP3 so end-to-end usability is
  exercised progressively instead of waiting for WP8/WP9.
- Required reusable checked-in scenario sources plus app-owned `sample create`
  generation; no SQLite server, standalone `sqlite3`, system provider, Docker,
  or hand-built ordinary fixture database.
- The current restored NuGet graph contains bundled native `e_sqlite3` assets for
  Windows, Linux, and macOS runtime identifiers. Actual application
  create/open/verify execution is not yet implemented or claimed; WP3 must prove
  it on each platform the project advertises.
- Documentation checks passed: local Markdown links resolve and code fences are
  balanced.
- Restored and refined the AI semantic-review concept as the first planned
  post-Gate-A phase: one expensive request contains the whole transaction, all
  disjoint selected dependency/impact chains, and each selected node's singular
  lineage to the purpose root; results are structured non-authoritative concerns.
  The production path is only OpenAI `gpt-5.6-terra`, while normal tests use a
  fake client. Human secret readiness and per-live-call authorization are hard
  preconditions, and paid calls have zero automatic retries. This does not
  authorize Gate B implementation.
- Simplified the pre-implementation canonical model to one typed property graph:
  every concept is a node, every graph-relevant connection is a binary typed
  edge, `scope-parent` spans all non-root nodes, and no reference fields create a
  hidden second graph. The planned SQLite v1 schema is nine tables, with
  canonical scalar-property/ledger JSON and relational entity/type/endpoint
  integrity. Focused batch/cluster expansion reduces repeated authoring while
  producing only explicit operations.
- Restored `VW_AIREVIEW__LIVETESTS=false` solely for the separately invoked Gate
  B live harness. It is ignored by normal tests and cannot bypass project policy;
  optional transaction skips are explicit/auditable and required review cannot
  be skipped.
- Established AI-first authoring as the authoritative mature-product direction:
  users state intent or supply supported text/images; an agent searches and
  navigates the graph, asks focused questions, and changes only a durable draft
  through strict Application tools. It has no SQL or unguarded write tool; Gate B
  remains an independent reviewer. The user approves the exact preview in
  conversation, then the agent calls the hash-bound guarded commit tool and
  completes the workflow.
- Added Gate A deterministic search/navigation requirements so later agents can
  find existing nodes before mutation without embeddings or natural-language
  SQL. Defined the stable tool-contract → in-app authoring → headless MCP →
  OpenAI plugin sequence, with visual UI optional.
- Raised planned review/authoring provider deadlines to 1,200 seconds and require
  Responses background mode. Polling one response is not a retry; automatic paid
  retries remain forbidden.
- Documentation redesign verification passed on 2026-08-13: all relative
  Markdown link targets exist, all Markdown code fences are balanced, full
  restore/build/test passed, the build produced 0 warnings and 0 errors, and all
  5 scaffold tests passed.

## 6. Current task

### WP1 - common graph domain

**Blueprint references:** Sections 2, 3, 4, 10.1, 10.3, 10.4, 16, and 17

**Required outcome:** Replace the placeholder Core implementation with the
immutable, database-independent logical domain needed by later serialization,
validation, application, and persistence work.

**In scope:**

- Strongly typed project, graph-entity, type, package, transaction, and commit IDs with
  exact validation and ordinal semantics.
- Scalar value/impact enums and immutable value types; properties cannot contain
  semantic references.
- Schema packages, required validators, node/property definitions, binary edge
  definitions, allowed endpoint types, and four explicit impact modes.
- Project nodes, first-class property-bearing edges, tags, extensions, project
  policy, head, and complete node/edge snapshot types.
- A required `PurposeNodeId` on the snapshot plus model shapes capable of
  representing one purpose root and one `scope-parent` per other node. The
  canonical `core/v1` definitions arrive in WP2 and cross-entity tree validation
  remains WP4.
- Add/replace/remove entity operations, focused `AuthoringBatch`/expanded
  operation contracts, and the review/disposition/report domain records required
  by the blueprint.
- Explicit construction-time or factory validation for local invariants.
- Comprehensive Core tests for accepted and rejected shapes, equality,
  immutability, invalid default/empty values, and lack of silent normalization.
- A realistic technical-design scenario constructed through the public Core API
  to prove the graph model can express interconnected soft-logic nodes without
  relying on extensions or test-only escape hatches.
- That scenario includes one purpose root, at least two sibling scope branches,
  one parent lineage for every other node, cross-branch typed semantic edges, and
  a focused cluster batch whose explicit expansion is asserted.
- A short modeling-usability assessment in Completed work and the human report.
- Removal of `ValidatedWorld.Core/Class1.cs` and empty placeholder tests.

**Out of scope:**

- JSON DTOs, canonical JSON, hashes, or built-in package resources (WP2).
- SQLite, migrations, repositories, or physical mapping (WP3).
- Cross-entity indexes, dependency-arc expansion, impact traversal, profile
  validators, or diagnostic execution (WP4 and later).
- Application handlers, CLI behavior, narrative/game profiles, or public hosts.

**Executable acceptance:**

1. Core exposes the complete WP1 model without references to SQLite, JSON, file,
   network, UI, provider, or game-engine libraries.
2. Tests exercise every local invariant and representative invalid input. There
   are no empty or tautological Core tests.
3. Public collections cannot be mutated through caller-owned mutable aliases.
4. IDs and canonical primitive values are rejected rather than trimmed,
   case-folded, or guessed.
5. `dotnet test tests/ValidatedWorld.Core.Tests/ValidatedWorld.Core.Tests.csproj`
   passes.
6. A representative technical-design graph is constructed and asserted through
   public Core APIs, including purpose identity, scope tree, semantic cross-links,
   and deterministic focus/batch expansion; modeling friction is reported.
7. The full restore/build/test sequence in Section 1 passes without warnings.
8. This plan records WP1 under Completed work and replaces Current task with a
   fully specified WP2 task.
9. The agent reports correctness and modeling-usability results to the human and
   stops without starting WP2.

## 7. Remaining roadmap order

After each successful task, move it to Completed work and fully specify only the
next task under Current task.

1. WP2 - logical JSON, built-in packages, and realistic source corpus.
2. WP3 - SQLite schema/mapping plus the first init/status/verify/query/sample
   CLI walking skeleton, reusable TestKit/end-to-end suite, bundled-runtime smoke
   test, and agent QA.
3. WP4 - indexes/semantic validation plus agent diagnosis/repair QA.
4. WP5 - durable drafts/projection plus agent transaction-authoring QA.
5. WP6 - impact/review plus agent impact-understanding and disposition QA.
6. WP7 - atomic commit/replay plus failure-recovery and audit QA.
7. WP8 - deterministic search, scope/neighbor navigation, remaining
   queries/context/CLI polish, and full workflow agent QA.
8. WP9 - Gate A evaluation and final roadmap report using accumulated QA
   evidence.

AI semantic review, AI-first authoring/intake, MCP/plugin packaging,
LinearNarrative, InteractiveState, and optional hosting are beyond this Gate A
roadmap and are not authorized implementation tasks. The recommended sequence is
Gate B independent review, Gate C authoring/intake, then Gate D headless
MCP/plugin packaging. Gate C requires a successful Gate A result and an explicit
decision to retain or omit Gate B; a failed/omitted reviewer does not erase the
AI-first authoring vision. Every phase requires a new human request.

## 8. Human report format

End every run with a concise report:

```text
Task: <completed or failed>
Delivered: <important observable results and files>
Verification: <commands and pass/fail counts>
Agent QA: <scenario, outcome, and friction/defects>
Unverified/inconclusive: <none or exact limits>
Plan: <next task, unchanged task after failure, or None>
Human action: <invoke the next task when ready, or one focused question>
```

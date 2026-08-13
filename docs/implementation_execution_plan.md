# ValidatedWorld Implementation Plan

**Last updated:** 2026-08-12

**Current task:** WP1 - common metamodel

This file tells a coding agent what has been finished and what to do next. The
[implementation blueprint](implementation_blueprint.md) defines the design and
ordered work packages. This plan records actual progress.

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

## 4. Definition of done

### One task is done when

- all of its stated scope is implemented;
- its executable acceptance criteria pass;
- the full restore/build/test sequence passes without warnings;
- fixtures, goldens, documentation, and guarantees agree with the behavior;
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
- request a separate planning task for the LinearNarrative phase;
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
- Documentation checks passed: local Markdown links resolve and code fences are
  balanced.
- Full restore/build/test passed on 2026-08-12: 0 build warnings; 5 scaffold
  tests passed.

## 6. Current task

### WP1 - common metamodel

**Blueprint references:** Sections 2, 3, 4, 10.1, 10.3, 10.4, 16, and 17

**Required outcome:** Replace the placeholder Core implementation with the
immutable, database-independent logical domain needed by later serialization,
validation, application, and persistence work.

**In scope:**

- Strongly typed project, object, type, package, transaction, and commit IDs with
  exact validation and ordinal semantics.
- Logical object/value/impact enums and immutable value types.
- Schema packages, required validators, logical type definitions, field
  definitions and constraints, relation roles, and dependency rules.
- Project objects, fields, endpoints, tags, extensions, project policy, head,
  and complete snapshot types.
- Add/replace/remove transaction operations and the review/disposition/report
  domain records required by the blueprint.
- Explicit construction-time or factory validation for local invariants.
- Comprehensive Core tests for accepted and rejected shapes, equality,
  immutability, invalid default/empty values, and lack of silent normalization.
- Removal of `ValidatedWorld.Core/Class1.cs` and empty placeholder tests.

**Out of scope:**

- JSON DTOs, canonical JSON, hashes, or built-in package resources (WP2).
- SQLite, migrations, repositories, or physical mapping (WP3).
- Cross-object indexes, dependency extraction, impact traversal, profile
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
6. The full restore/build/test sequence in Section 1 passes without warnings.
7. This plan records WP1 under Completed work and replaces Current task with a
   fully specified WP2 task.
8. The agent reports the result to the human and stops without starting WP2.

## 7. Remaining roadmap order

After each successful task, move it to Completed work and fully specify only the
next task under Current task.

1. WP2 - logical JSON and built-in packages.
2. WP3 - SQLite schema and mapping.
3. WP4 - indexes and semantic validation.
4. WP5 - durable drafts and projection.
5. WP6 - impact and mandatory review.
6. WP7 - atomic accepted commit and replay.
7. WP8 - queries and CLI.
8. WP9 - Gate A evaluation and final roadmap report.

LinearNarrative, InteractiveState, and optional integration/hosting work are
ideas beyond this roadmap, not authorized implementation tasks. Reaching them
requires a new human-requested planning task.

## 8. Human report format

End every run with a concise report:

```text
Task: <completed or failed>
Delivered: <important observable results and files>
Verification: <commands and pass/fail counts>
Unverified/inconclusive: <none or exact limits>
Plan: <next task, unchanged task after failure, or None>
Human action: <invoke the next task when ready, or one focused question>
```

# ValidatedWorld Implementation Execution Plan

**Status:** Active

**Plan format:** 1.0

**Last updated:** 2026-08-12

**Baseline commit:** `dead7b814df2`

**Current assignment:** WP1 - common metamodel

This is the repository's authoritative implementation status and handoff file.
The [implementation blueprint](implementation_blueprint.md) defines what the
system must do and the order of its work packages. This file records what has
actually been completed, what evidence proves it, and the one task the next
agent must perform.

An agent may not infer progress from plausible-looking code, old chat history,
or a work-package heading. Repository state, automated evidence, and this file
must agree.

## 1. Autonomous execution contract

Agents implement the blueprint sequentially. At most one work package, or one
explicitly recorded slice of it, may be active.

Before changing production code, an agent must:

1. Read `AGENTS.md`, the required product documents, the relevant blueprint
   sections, and this entire file.
2. Inspect the actual source, tests, fixtures, and recent history relevant to the
   current assignment.
3. Reconcile this plan with repository evidence. If they disagree, correct the
   plan or mark it blocked before implementing later work.
4. Change the current assignment status from `ready` to `in-progress`, add the
   UTC start time and a short implementation intent, and keep that plan edit in
   the same change set.

While implementing, the agent must:

1. Stay inside the current assignment. A necessary prerequisite defect may be
   fixed and documented, but later work packages may not be pulled forward.
2. Add meaningful automated tests with the behavior. Tests must fail for a real
   regression; assembly-load, empty, or tautological tests are not acceptance
   evidence.
3. Prefer deterministic, hermetic verification: generated temporary databases,
   fixed clocks and IDs, fixed seeds, checked-in text goldens, bounded data, and
   no dependence on network services, secrets, UI interaction, or a human
   reviewer.
4. Make routine, reversible implementation decisions using the controlling
   documents and the simplest design that satisfies them. Do not stop for naming,
   formatting, or ordinary coding preferences.
5. Record any contract ambiguity, deviation, or inconclusive behavior instead of
   hiding it behind a passing test.

Before handing off, the agent must:

1. Satisfy every acceptance criterion for the assignment.
2. Run the assignment-specific tests and the complete repository verification:

   ```powershell
   dotnet restore ValidatedWorld.slnx
   dotnet build ValidatedWorld.slnx --no-restore
   dotnet test ValidatedWorld.slnx --no-build --no-restore
   ```

3. Update this file in the same change set:
   - mark the assignment `complete` only when all required checks pass;
   - update the roadmap table;
   - append a completion entry with exact commands and outcomes;
   - replace Current assignment with the next authorized work package or slice;
   - give that next assignment explicit scope, exclusions, and executable
     acceptance criteria.
4. Leave the repository so the next agent can begin from this file without
   requiring chat history or a human interpretation of what happened.

A code change is incomplete when the code works but this plan still describes
the previous state.

## 2. Failure-loop and human-escalation rule

Normal failing tests are implementation feedback, not a reason to ask a human.
Diagnose them, make a targeted change, and rerun the narrowest useful check.

Stop instead of looping when the same blocking condition remains after **three
materially different, evidence-based repair attempts**, or when attempted fixes
return to a previously observed failure state. Also stop when two controlling
requirements are irreconcilable and choosing either would change a public
contract, product guarantee, destructive-data behavior, or authorized scope.

When stopping:

1. Do not start a later work package and do not mark the current one complete.
2. Set the assignment and roadmap status to `blocked`.
3. Add a blocker entry containing the failing command/test, the relevant output,
   root-cause hypothesis, and all three attempted approaches.
4. State the smallest concrete human decision or missing information needed.
5. Ask that one focused question. Do not ask the human to debug the code or
   manually verify behavior the repository should test.

Once human direction resolves the blocker, the resuming agent records the
decision, returns the assignment to `in-progress`, adds a regression test where
possible, and continues the same work package.

## 3. Automated acceptance standard

Every engineering work package through WP8 must be verifiable without human
inspection. Its acceptance suite must provide a deterministic pass, failure, or
explicit inconclusive result.

- Unit tests cover local invariants and rejected inputs.
- Property tests cover ordering, round trips, graph properties, and atomicity
  where the blueprint requires them.
- SQLite tests create databases in per-test temporary directories by default.
- Checked-in fixtures are source snapshots, scripts, and expected JSON. Binary
  databases are exceptional test-only artifacts as defined in `AGENTS.md`.
- Time, IDs, random data, concurrency scheduling, and fault points are controlled
  by test doubles or fixed inputs.
- Performance checks record fixture size, environment, elapsed time, and budget;
  they do not rely on an undocumented universal threshold.
- Agent/CLI scenarios are driven by scripts and assert structured outputs and
  exit codes, not prose judgment.
- A skipped, flaky, network-dependent, or manually inspected check does not prove
  completion.

If a requirement has no reliable automated oracle, the current task includes
building that oracle. If that is genuinely impossible, report the requirement as
inconclusive and apply the failure/escalation rule rather than claiming it works.

WP9 evaluates the Gate A product hypothesis. Its reproducible measurements and
predeclared criteria should drive the recommendation. Gated later work must not
begin when the result is inconclusive; record the evidence and request the
smallest necessary product-direction decision.

## 4. Roadmap status

Allowed statuses are `queued`, `ready`, `in-progress`, `blocked`, `complete`, and
`gated`. Exactly one row may be `ready` or `in-progress`.

| Work package | Status | Evidence or prerequisite |
|---|---|---|
| WP0 - architecture scaffold | complete | Solution/project boundaries and pinned SQLite dependencies exist at baseline `dead7b814df2`; restore/build/test verified. |
| WP1 - common metamodel | ready | Current assignment below. |
| WP2 - logical JSON and built-in packages | queued | Requires WP1 completion. |
| WP3 - SQLite schema and mapping | queued | Requires WP2 completion. |
| WP4 - indexes and semantic validation | queued | Requires WP3 completion. |
| WP5 - durable drafts and projection | queued | Requires WP4 completion. |
| WP6 - impact and mandatory review | queued | Requires WP5 completion. |
| WP7 - atomic accepted commit and replay | queued | Requires WP6 completion. |
| WP8 - queries and CLI | queued | Requires WP7 completion. |
| WP9 - Gate A evaluation | queued | Requires WP8 and the complete Gate A acceptance corpus. |
| WP10 - LinearNarrative profile | gated | Requires a conclusive Gate A decision recorded here. |
| WP11 - InteractiveState profile | gated | Requires a conclusive Gate B decision recorded here. |
| WP12 - optional hosts/integrations | gated | Requires the applicable hosting/integration evidence gate. |

## 5. Current assignment

### WP1 - common metamodel

**Status:** ready

**Prerequisite:** WP0 complete

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

- JSON DTOs, canonical JSON, hashes, or built-in package resource files (WP2).
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
6. Full restore, build, and test commands from Section 1 pass with no warnings.
7. This plan is updated with completion evidence and an equally explicit WP2
   assignment.

## 6. Completion and blocker log

Keep this log append-only. Correct factual mistakes with a later entry rather
than erasing implementation history.

### 2026-08-11 - WP0 complete

- Result: architecture scaffold completed.
- Evidence: all six production projects and five matching test projects are in
  `ValidatedWorld.slnx`; project references match the blueprint; SQLite uses
  pinned `Microsoft.Data.Sqlite.Core` and `SQLitePCLRaw.bundle_e_sqlite3`.
- Verification: full restore, build, and five scaffold tests passed with zero
  build warnings.
- Remaining behavior: production projects are placeholders; behavioral
  implementation begins at WP1.
- Next: WP1 as specified above.

### 2026-08-12 - autonomous execution protocol established

- Result: repository-owned progress, self-verification, failure-loop, and
  next-agent handoff rules established without changing blueprint work-package
  completion state.
- Scope delivered: this living plan plus synchronized instructions in
  `AGENTS.md`, `CLAUDE.md`, `README.md`, and the controlling documentation.
- Tests added/updated: none; this was documentation-only and no artificial tests
  were added.
- Verification: local Markdown links resolved and code fences were balanced;
  `dotnet restore ValidatedWorld.slnx` -> exit 0;
  `dotnet build ValidatedWorld.slnx --no-restore` -> exit 0, 0 warnings and 0
  errors; `dotnet test ValidatedWorld.slnx --no-build --no-restore` -> exit 0,
  5 passed, 0 failed, 0 skipped.
- Deviations/inconclusive behavior: the five existing tests prove only the WP0
  scaffold; production behavior and meaningful behavioral tests remain WP1 and
  later work.
- Plan decision: WP0 remains complete and no later package was pulled forward.
- Next: WP1 as specified in Section 5.

## 7. Next-assignment and handoff templates

Replace Section 5 with this shape when advancing the plan. Copying only a
work-package title is not an actionable handoff.

```markdown
### WPn[.slice] - <name>

**Status:** ready

**Prerequisite:** <completed evidence>

**Blueprint references:** <exact sections>

**Required outcome:** <one observable result>

**In scope:**

- <bounded deliverable>

**Out of scope:**

- <later or forbidden behavior>

**Executable acceptance:**

1. <specific automated assertion/command>
2. Full restore/build/test passes.
3. This plan advances with evidence and the next explicit assignment.
```

Append one entry when completing or blocking an assignment:

```markdown
### YYYY-MM-DD - WPn [complete|blocked]

- Result: <observable outcome>
- Scope delivered: <bounded list>
- Tests added/updated: <projects, fixtures, and important cases>
- Verification: `<exact command>` -> <exit code and concise result>
- Deviations/inconclusive behavior: <none or exact details>
- Plan decision: <why the next package is authorized or why work stopped>
- Next: <exact next assignment or focused human question>
```

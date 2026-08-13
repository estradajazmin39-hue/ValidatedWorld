# ValidatedWorld

ValidatedWorld is an experimental **semantic change-control engine for complex
project data**.

A novel, technical design, patent outline, campaign, or game is presented in a
sequence, but its important meaning forms a graph. Conclusions depend on
assumptions and evidence; scenes depend on prior events and character knowledge;
requirements depend on definitions and are realized by decisions and tests.

ValidatedWorld stores that explicit graph in an embedded SQLite project file. It
validates proposed transactions, calculates their downstream impact, requires
selected affected records to be reviewed, and commits the complete new state or
nothing at all.

## Storage and protocol

The authoritative workspace is one portable SQLite application file:

```text
project.vw.db
```

That file is a user's mutable project state, not a source-controlled project
template. The repository ignores `.vw.db` files and their SQLite sidecars
outside `tests/`. Samples commit reviewed logical snapshots, transaction scripts,
and expected results, then generate disposable databases locally. A binary
database belongs in `tests/` only when a deliberately constructed persistence or
corruption fixture cannot reasonably be generated during the test.

SQLite supplies durable transactions, foreign keys, indexes, and efficient
queries. ValidatedWorld supplies the semantic behavior a database schema cannot:
typed dependency direction, base-plus-projected impact, explainable review
obligations, domain constraints, and proven/disproven/inconclusive outcomes.

JSON remains the public agent and integration protocol. Commands, transaction
operations, diagnostics, impact results, and deterministic logical snapshots are
versioned JSON. The project hash is computed from the logical canonical snapshot,
not from SQLite's physical file bytes.

## A small opinionated metamodel

ValidatedWorld is not a suggestion that every AI invent arbitrary SQL tables and
remember to check them carefully. That workflow cannot deterministically answer
what a foreign key means, which direction impact flows, or whether every relevant
record was reviewed.

Instead, the physical database has a small fixed metamodel:

- stable-ID records with versioned logical types and typed fields;
- first-class typed relationships with named endpoints and impact semantics;
- extracted record references with foreign-key integrity;
- closed deterministic constraint kinds;
- draft transactions and operations;
- impact evidence and review dispositions;
- accepted commits and audit records.

Technical claims, fictional events, character knowledge, game transitions, and
other domain concepts are versioned profiles over that metamodel. Project authors
do not create arbitrary physical tables or write canonical rows directly.
Unmodeled extension data may be retained, but it is reported as outside the
engine's guarantees.

## Product boundary

ValidatedWorld owns:

- the authoritative `project.vw.db` state;
- a deterministic backend-neutral JSON snapshot representation;
- stable IDs, logical types, and typed references;
- structural and semantic validation;
- explained graph impact and mandatory review policy;
- atomic optimistic transactions and replayable commit evidence;
- bounded JSON queries for humans, AIs, and integrations.

ValidatedWorld does **not** own the finished novel, paper, patent application,
manual, source tree, game project, or media. External artifact/anchor records may
point to those products, but the engine does not import, rewrite, render, publish,
or certify them.

There is no special diff format. Accepted transaction operations are the direct
change record; impact analysis supplies the transitive consequences.

## Intended uses

- Technical work: definitions, assumptions, requirements, evidence, decisions,
  conclusions, implementations, verification, and traceability.
- Patent or standards planning: a structured claim/definition/evidence outline
  without claims of legal or scientific correctness.
- Novels and mysteries: canon facts, chronology, character knowledge, clues, and
  disclosure while manuscript text remains external.
- Games and campaigns: a static transition specification whose runtime states
  are derived by a later bounded-analysis profile.

Despite the name, a “world” is any versioned universe of connected records.
Fiction is one profile, not the common engine's foundation.

## Core workflow

```text
open project.vw.db and verify its logical head hash
→ begin a draft transaction against the exact head revision/hash
→ add, replace, or remove typed records and relationships
→ construct the projected logical state
→ derive dependency edges from base and projected state
→ compute explained transitive impact
→ repair graph data and disposition policy-selected affected records
→ run complete deterministic validation
→ commit all relational changes atomically or roll back everything
→ return versioned JSON results
```

## Start here

- [Feasibility and limits](docs/feasibility.md) — guarantee boundary and proof
  gates.
- [Product and architecture specification](docs/validated_world_authoring_spec.md)
  — authoritative metamodel, persistence, and profile design.
- [Implementation blueprint](docs/implementation_blueprint.md) — exact storage
  schema, algorithms, tests, and work packages.
- [Implementation execution plan](docs/implementation_execution_plan.md) —
  proven progress, the one current assignment, automated acceptance criteria,
  and the required next-agent handoff.
- [Related systems and product position](docs/prior_art_and_positioning.md) —
  overlaps with requirements tools, graph validation, versioned databases, and
  RAG.

## Self-directed implementation

The blueprint is implemented sequentially from WP0 through Gate A. Agents do not
choose a work package from the roadmap or infer the next task from chat history.
They read the living execution plan, implement its single current assignment,
and update that plan in the same change.

Every completed assignment must leave behind:

- production behavior for its bounded scope;
- meaningful deterministic tests and generated fixtures/goldens;
- passing assignment-specific checks plus the complete solution build and test
  suite;
- an execution-plan entry containing the exact verification evidence, known
  inconclusive behavior, and an explicit next assignment;
- no need for a human to manually click through, inspect output, supply secrets,
  or decide routine implementation details.

An agent marks an item complete only when repository evidence proves it. If the
same blocker survives three materially different repair attempts or the fixes
cycle back to an earlier failure, the agent records the attempts, marks the item
blocked, stops, and asks one focused human question instead of retrying forever
or skipping ahead.

## Current status

The repository is a .NET 10 scaffold plus an implementation-ready design. WP0 is
complete and WP1 (the common metamodel) is the current assignment. The execution
plan, rather than this summary, is authoritative if progress changes. Gate A is
a small technical-project graph backed by SQLite. It must prove useful and
accurate impact at acceptable modeling cost before narrative or game profiles
are implemented.

Build and test with:

```powershell
dotnet restore ValidatedWorld.slnx
dotnet build ValidatedWorld.slnx --no-restore
dotnet test ValidatedWorld.slnx --no-build --no-restore
```

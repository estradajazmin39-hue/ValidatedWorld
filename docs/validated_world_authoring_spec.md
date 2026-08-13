# ValidatedWorld Product and Architecture Specification

**Status:** Authoritative product specification

**Specification version:** 5.0

**Last reviewed:** 2026-08-12

**Primary implementation:** .NET 10 / C#

**Authoritative workspace:** SQLite `project.vw.db`

**Logical protocol:** `validatedworld/v1` JSON

This specification defines the product and architectural boundary. The guarantee
and falsification plan are in [feasibility.md](feasibility.md). Exact SQL,
algorithms, tests, and work packages are in
[implementation_blueprint.md](implementation_blueprint.md). Related systems are
recorded in [prior_art_and_positioning.md](prior_art_and_positioning.md). Proven
implementation progress and the only current agent assignment are recorded in
[implementation_execution_plan.md](implementation_execution_plan.md).

Human direction overrides this document. Update the controlling documents
together when a product decision changes.

## 1. Product thesis

A complex authored project is sequential in presentation but graph-shaped in
meaning:

- conclusions depend on assumptions, definitions, and evidence;
- decisions depend on requirements and measurements;
- scenes depend on prior events and character knowledge;
- game transitions depend on state and alter future possibilities.

ValidatedWorld maintains the explicitly modeled, continuity-critical portion of
that graph as authoritative structured project data. It is a **semantic
change-control engine**, not a document author, generic database, or RAG system.

The workflow is:

```text
open and integrity-check project.vw.db
→ read the exact logical head revision/hash
→ begin a durable application-level transaction
→ add, replace, or remove typed objects
→ construct the projected logical state
→ derive dependency graphs from base and projection
→ compute explained transitive impact
→ repair data and disposition selected impacted objects
→ run every required deterministic validator
→ recheck head and evidence inside a short SQLite write transaction
→ atomically apply the new state and commit evidence, or roll back everything
→ return a versioned JSON result
```

An external AI may query the relational state and is expected to be competent
with databases. It still cannot write authoritative rows directly: the
transaction/validation boundary is the product.

## 2. Product boundary and evidence

### 2.1 What ValidatedWorld owns

- One portable authoritative SQLite project file.
- A fixed, migrated, integrity-constrained physical schema.
- A small logical metamodel for typed records and relationships.
- Exact versioned logical schema packages/profiles.
- Stable IDs and per-object revisions.
- Atomic optimistic project transactions.
- Derived dependency/impact indexes.
- Deterministic validators and structured diagnostics.
- Review dispositions for policy-selected impacted objects.
- Deterministic logical JSON snapshots, commands, and results.
- Accepted commit operations and audit/replay evidence.

### 2.2 What remains external

- Manuscripts, papers, patent applications, manuals, source trees, games, and
  media.
- Extracting meaning from those artifacts.
- Updating, rendering, or publishing them.
- General AI-provider, prompt, or composition workflows.
- Arbitrary user-owned relational schemas.
- Hosted identity, authorization, collaboration, and multi-tenancy.

External artifact and anchor records may point to external material, but the
engine does not dereference, parse, edit, or certify it.

### 2.3 Evidence classes

Every semantic validation result is:

- **Proven:** all required deterministic phases completed and the declared
  property holds.
- **Disproven:** a deterministic rule found a violation, with evidence.
- **Inconclusive:** missing annotations, unsupported schema/profile data, a
  configured bound, cancellation, or failure prevented a conclusion.

Database constraint success is structural evidence, not proof of semantic
validity. An incomplete semantic phase is never a pass.

## 3. Three layers of schema

ValidatedWorld separates three ideas often conflated as “the schema.”

### 3.1 Physical SQLite schema

Implementation-owned tables store projects, schema definitions, graph objects,
field values, relation endpoints, constraints, transactions, reviews, commits,
and diagnostics.

The physical schema changes only through application migrations. A project or AI
does not issue canonical DDL.

### 3.2 Logical metamodel

The common domain defines:

- schema packages;
- record/relation type definitions;
- field and endpoint-role definitions;
- dependency rules;
- record and relation instances;
- constraints and policy;
- transactions, impact, review, and commit evidence.

This metamodel is intentionally opinionated. Without it, the engine cannot know
which references produce impact or how to explain validation.

### 3.3 Domain profiles

Profiles provide vocabulary and validators over the metamodel. Examples:

- technical concepts, requirements, assumptions, evidence, and decisions;
- fictional characters, events, knowledge, clues, and disclosures;
- finite state variables, transitions, effects, and invariants.

Profiles never create physical project tables. They supply immutable versioned
logical type packages and registered deterministic behavior.

## 4. Logical schema packages

### 4.1 Package identity

A `SchemaPackage` has:

- stable package ID;
- semantic version;
- canonical definition hash;
- record type definitions;
- relation type definitions;
- supported constraint kinds;
- required validator IDs/versions;
- compatibility and migration metadata.

Projects enable exact package IDs, versions, and hashes. The full definitions are
stored in the project database so the file is self-describing.

Generic field/type validation may run from package data. Semantic validator code
runs only when the exact declared validator implementation is registered. Missing
implementations make affected coverage inconclusive.

Gate A ships `core/v1` and `technical-project/v1`. Project-authored packages and
schema evolution are deferred until the fixed-package POC succeeds.

### 4.2 Record type definitions

A record type declares:

- stable type ID and display name;
- owning package;
- field definitions;
- whether the type may be an external artifact/anchor or constraint target;
- categories/tags used by policy;
- applicable generic constraint selectors.

### 4.3 Field definitions

A field definition declares:

- stable field name;
- value kind: `text`, `integer`, canonical `decimal`, `boolean`, `symbol`,
  `instant`, or `reference`;
- minimum/maximum cardinality;
- order significance;
- allowed symbols/ranges/patterns where supported;
- allowed reference target types;
- reference dependency behavior;
- whether it contributes to display/search only or semantic identity.

Gate A avoids arbitrary nested objects. Repeated structured concepts become
records or first-class relations. Namespaced extension JSON is allowed only as
uncovered data and cannot contain hidden canonical references.

### 4.4 Relation type definitions

A relation type declares:

- stable type ID and owning package;
- named endpoint roles;
- cardinality and allowed object types for each role;
- field definitions and provenance requirements;
- zero or more dependency rules;
- whether the relation is authoritative for selected lifecycle states.

A dependency rule identifies a dependent endpoint role, a dependency endpoint
role, and whether the edge creates review impact. Bidirectional review is two
explicit rules. A query-only association has no impact rule.

Examples:

```text
derived-from:
  result depends on premise

supports:
  supported-assertion depends on supporter

contradicts:
  left depends on right for review
  right depends on left for review

mentions:
  no dependency rule
```

This declaration—not SQL foreign-key direction—defines semantic impact.

## 5. Logical project objects

### 5.1 Stable identity

Every addressable record, relation, and constraint has a globally unique stable
ID within the project and a committed object revision. Names, labels, paths, and
SQL row IDs are never semantic references.

### 5.2 Records

A record contains:

- stable ID and revision;
- logical record type ID;
- typed field values conforming to its type definition;
- ordinal tags;
- namespaced extension JSON;
- lifecycle metadata when its profile declares it.

Examples in the technical profile include subjects, propositions, assertions,
sources, artifacts, and anchors. They are profile types, not hardcoded physical
tables.

### 5.3 Relations

A relation is first-class and contains:

- stable ID and revision;
- logical relation type ID;
- one or more endpoints for each declared named role;
- typed field values such as rationale;
- provenance;
- tags and extension data.

First-class relation identity allows evidence, provenance, review, and future
relations-about-relations without treating an edge as an anonymous pair.

### 5.4 Constraints

Gate A uses a closed constraint catalog whose instances select objects by type,
field, tag, lifecycle state, or relation kind. Initial generic/technical kinds
cover:

- no selected contradictory accepted assertions;
- required support for selected assertion roles;
- acyclic selected dependency relation types;
- unique active definitions;
- required implementation and verification coverage;
- required impact dispositions.

There is no arbitrary SQL, expression, trigger, or scripting constraint language.

### 5.5 External artifacts and anchors

Artifact and anchor are logical profile record types. They may store opaque
external locators and observed external version/hash claims. Anchors may form an
acyclic ordered hierarchy and bind to semantic objects using typed relations.

The engine never follows a locator or verifies external bytes.

### 5.6 Open-world default

Missing information is unknown or unmodeled, not false. Negative assertions are
explicit. A profile may declare a finite closed-world rule, and diagnostics must
identify that assumption.

## 6. SQLite persistence

### 6.1 Authoritative file

The workspace is:

```text
<workspace>/project.vw.db
```

The database contains current authoritative logical state plus drafts, reports,
and commit evidence. Temporary journal files may exist while SQLite writes.
Safe copy/backup operations use SQLite's backup facilities or a closed database,
not an arbitrary byte copy of an open workspace.

A populated workspace is mutable user data, not a distributable project
template. Source repositories should ignore `.vw.db` files and SQLite sidecars,
commit reviewed revision-zero snapshots and transaction scripts, and generate
sample databases locally. Deliberate binary database fixtures are confined to
test directories and require documented provenance or a regeneration method.

### 6.2 Structural tables

The normative table design is in the blueprint. Conceptually it contains:

```text
metadata and migration history
project and project_head
schema_packages, logical_types, field_definitions
relation_roles, dependency_rules
graph_objects, object_tags, object_field_values
relations, relation_endpoints
constraints
draft_transactions, draft_operations, submitted_dispositions
commits, commit_operations, accepted_dispositions
validation_runs, diagnostics
```

Record-valued fields and relation endpoints reference the global graph-object ID
space with foreign keys. Deletes restrict rather than cascade.

### 6.3 Connection policy

Every application connection must:

- enable and verify `PRAGMA foreign_keys = ON`;
- use parameterized SQL;
- set conservative busy, length, page, and query limits where available;
- refuse unknown or checksum-mismatched migrations;
- refuse database extensions and user-defined executable SQL;
- validate the application ID/user version/schema fingerprint;
- treat the file as untrusted input until integrity checks complete.

Gate A uses direct `Microsoft.Data.Sqlite`, explicit SQL, and checked migrations,
not an ORM.

### 6.4 Logical identity

SQLite physical bytes are not semantic identity. The logical snapshot includes:

- project head metadata and policy;
- selected complete schema package definitions;
- all current graph objects, values, endpoints, and constraints;
- exact deterministic ordering.

Its `logicalHash` is SHA-256 over canonical UTF-8 JSON with that field omitted.
Drafts, validation runs, and historical audit rows are not part of current logical
state.

Verification detects accidental or incomplete direct mutations when logical
state, stored head hash, and commit evidence disagree. It is not a cryptographic
defense against an attacker with arbitrary write access who rewrites both data
and history. Direct writes are unsupported even if an external SQLite tool can
physically perform them.

## 7. Transactions and impact

### 7.1 Application transaction

A draft transaction records:

- transaction ID, project ID, base revision/hash;
- intent, author, creation time, and status;
- one final add/replace/remove operation per target object ID;
- expected object revision for replacements/removals;
- submitted review dispositions and allowed warning acknowledgements.

Drafts are durable database rows but are not canonical logical state.

Clients cannot set committed object revisions. Adds start at 1; replacements
increment the authoritative revision.

### 7.2 Projection

The Application layer loads the required base logical state, applies draft
operations to isolated builders, materializes the projection, extracts typed
references/endpoints, and validates it. There is no cascade delete or partial
canonical write.

Gate A may perform full projection and validation in memory. Persistence is
incremental; semantic validation need not be incremental yet.

### 7.3 Dependency graph

Edges are derived from:

- reference-valued fields whose field definitions declare dependency behavior;
- relation endpoints interpreted by relation-type dependency rules;
- containment and definition rules declared by profiles;
- constraint and understood profile references.

Foreign-key rows are not automatically semantic edges. Presentation order,
provenance, and query-only associations do not create impact unless declared.

### 7.4 Impact

Impact seeds are direct operation targets. The engine traverses reverse
review-impact edges in the union of base and projected dependency graphs:

- base edges retain impact from removed/redirected dependencies;
- projected edges include impact from new dependencies.

Results contain changed IDs, impacted IDs, shortest deterministic explanation
paths, relation/field evidence, completeness/bounds, and statistics.

### 7.5 Review obligations

Policy selects which impacted objects require one disposition:

- `updated` — object is directly changed by the transaction;
- `reviewed-no-change` — reviewer says projected object remains correct;
- `not-applicable` — reviewer says the explained path requires no action;
- `pending` — commit blocks when policy requires review.

Nonautomatic dispositions require reviewer, rationale, and time. They are
fingerprinted over the projected target, change set, and impact path. Operation
changes invalidate stale dispositions.

### 7.6 Commit

After an apparently valid reviewed draft, commit:

1. Opens a short SQLite write transaction.
2. Rechecks database integrity, project head, and record preconditions.
3. Reprojects/revalidates evidence that could be stale.
4. Applies every logical object/value/endpoint/constraint change.
5. Inserts accepted operation, disposition, report, and commit rows.
6. Updates the project head revision, parent hash, and logical hash.
7. Commits SQLite once.

Any failure rolls back every authoritative and audit write.

## 8. Deterministic validation

Validation phases are:

1. SQLite application/schema/migration/integrity checks.
2. Logical snapshot/hash integrity.
3. Schema package/type/field coverage.
4. ID, revision, value, reference, and endpoint integrity.
5. Generic relation dependency-rule validity.
6. Profile structural and semantic rules.
7. Contradiction, support, definition, cycle, and traceability constraints.
8. Transaction impact completeness and review dispositions.
9. Commit policy.

Database failures are structural errors. Semantic phases skipped because a
prerequisite failed are explicitly inconclusive.

Diagnostics include stable code/rule version, outcome/severity, primary/related
IDs, field/endpoint evidence, impact path where applicable, safe repair
categories, source command pointer, and deterministic fingerprint.

## 9. JSON and SQL interfaces

### 9.1 JSON protocol

Every CLI command consumes arguments and optional versioned JSON request bodies
and writes exactly one versioned JSON result to stdout. Logs go to stderr.

Required use cases:

- initialize, inspect, verify, and snapshot a project;
- get/list/query logical objects and relationships;
- inspect dependencies/dependents and explanation paths;
- begin/show/apply/validate/commit/abort transactions;
- list and disposition review obligations;
- retrieve commit evidence and verify replay;
- build bounded relevant-object context.

A logical snapshot is backend-neutral data interchange and test evidence. It is
not a finished-document export.

### 9.2 Database access

Because AIs and developers can query SQLite effectively, the database exposes
documented read views for logical objects, field values, relationships, direct
dependencies, commits, and diagnostics.

Direct SQL reads are supported after integrity verification. Direct SQL writes
to canonical tables are unsupported. Gate A does not implement a general SQL
proxy or attempt to securely classify arbitrary SQL text.

An optional future transaction-scoped SQL authoring surface may write only to
validated draft/staging structures. It must not bypass Application operations.

## 10. Profiles

### 10.1 Technical project profile — Gate A

Logical record types cover artifacts, anchors, terms, quantities, components,
requirements, propositions, assertions, and evidence. Relation types cover
depends-on, derived-from, supports, contradicts, defines, uses, implements,
satisfies, verifies, cites, binds, and mentions.

Roles distinguish fact, assumption, hypothesis, requirement, observation,
result, conclusion, decision, recommendation, and definition. Lifecycle status
distinguishes proposed, accepted, rejected, deprecated, and superseded.

The profile proves structured traceability, not scientific, engineering, patent,
or legal correctness.

### 10.2 Linear narrative profile — Gate B

Adds fictional time, temporally scoped assertions, events, character
knowledge/belief, narrative order, clues, and explicit disclosure/deduction
rules. Manuscripts remain external.

Keep canon truth, character perspective, fictional time, narrative order, and
authoring revision separate.

### 10.3 Interactive-state profile — Gate C

Adds finite typed state variables, conditions, effects, transitions, invariants,
and reachability constraints. Runtime state is a derived valuation after an
action path, not another authoritative project revision.

Bounded analysis returns proven-within-model, a shortest counterexample, or
inconclusive when limits are reached. It does not validate arbitrary game code.

## 11. Architecture

```text
ValidatedWorld.Core                 no project dependencies
├── ValidatedWorld.Serialization    Core; logical JSON/protocol
└── ValidatedWorld.Validation       Core

ValidatedWorld.Application          Core, Serialization, Validation
ValidatedWorld.Persistence.Sqlite   Core, Serialization, Application
ValidatedWorld.Cli                  Application, Persistence.Sqlite

ValidatedWorld.Mcp                  later; Application + selected persistence
ValidatedWorld.Web                  later; Application + selected persistence
```

Application defines persistence ports. SQLite implements them. Validation
operates on immutable logical snapshots and indexes, not SQL connections. This
keeps semantics backend-neutral without pretending all database engines have the
same operational behavior.

## 12. Gate A proof of concept

The `TechnicalProject` fixture includes an offline sensor design with external
anchors for requirements, power budget, architecture, verification, and
unrelated privacy material.

The initial graph contains a 24-hour runtime requirement, a 20 mA current
assumption, a 500 mAh capacity assumption, a 25-hour runtime result, and a
battery-sufficiency conclusion with explicit dependencies.

A transaction changes current to 25 mA. It must impact the runtime result,
battery decision, and power/architecture/verification anchors, but not privacy.
A valid transaction repairs structured capacity/runtime values and all required
dispositions together.

Gate A must prove:

1. Database application/migration/integrity verification.
2. Relational foreign-key and restricted-delete behavior.
3. Deterministic database-to-logical-JSON round trip and hash.
4. Stable-ID transaction projection and optimistic preconditions.
5. Exact complete impact with explainable paths.
6. Deterministic validation and coverage.
7. Pending obligations block commit.
8. Rejected/stale commits roll back every database row.
9. Accepted operations replay to the recorded logical hash.
10. Documented read views and JSON results are deterministic.
11. A lower-cost agent can query and repair the project without direct writes.
12. Synthetic 100,000-record/1,000,000-edge performance stays within documented
    budgets.

No document import/rendering, arbitrary DDL, AI provider, web server, RDF store,
graph database, narrative timeline, or game exploration is required.

Gate A is implemented through the blueprint's ordered work packages. Each
engineering package has deterministic automated acceptance and full-solution
verification; passing behavior is established by repository evidence rather than
manual human inspection. Completion evidence and the next authorized assignment
must be recorded in the execution plan before another package begins.

A human invokes each agent run. The agent completes the Current task or reports
why it failed, then stops; it does not perform Git workflow operations or
automatically start the next task. Gate A completion places the plan at
Current task `None` until a human declares the roadmap finished or requests a
separate later-phase planning task.

## 13. Success and stop criteria

Gate A succeeds if explicit typed relationships surface the correct records and
anchors, transactions prevent stale semantic state, SQLite reduces persistence
complexity, agents can repair failures from evidence, and modeling cost is
acceptable.

Scale down to a smaller fixed type catalog if the general logical schema package
model is too complex. Remove the proposition/assertion layer if generic typed
records and relations perform as well. Stop if the result offers no meaningful
advantage over ordinary SQLite plus Doorstop-style suspect-link review.

The common POC explicitly does not:

- permit arbitrary project tables, triggers, views, or SQL validators;
- treat successful foreign keys as semantic correctness;
- import, generate, rewrite, render, or publish finished documents;
- output prose or game packages;
- infer arbitrary claims from natural language;
- accept AI suggestions automatically;
- prove scientific truth, engineering safety, patentability, legal sufficiency,
  or literary quality;
- implement collaborative branches/merges or public plugin packaging.

## 14. Durable direction

> Keep one explicit relationally stored semantic graph internally coherent across
> revisions, explain what each proposed change affects, and let external tools
> decide how to use the accepted state.

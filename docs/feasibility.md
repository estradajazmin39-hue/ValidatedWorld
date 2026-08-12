# Feasibility, Limits, and the Smallest Useful Product

**Status:** Accepted product boundary

**Last reviewed:** 2026-08-11

## Verdict

ValidatedWorld is feasible if it is built as a **semantic change-control engine
over a small relational metamodel**, not as a monolithic JSON document, a generic
database, or an exhortation for AIs to write careful SQL.

One SQLite application file is the authoritative workspace. JSON is the
deterministic command/result and logical-snapshot format. The C# engine owns the
semantics that ordinary database constraints do not express: typed dependency
direction, transitive impact across old and projected state, review obligations,
profile constraints, coverage, and inconclusive outcomes.

ValidatedWorld is not a document system. It does not read or rewrite a novel,
patent application, whitepaper, manual, or game project. External tools may use
its data and impact results, but that workflow remains outside its guarantee.

## Why not arbitrary AI-authored tables

Allowing each AI or project to create any SQL schema sounds flexible, but removes
the basis for deterministic product behavior.

A foreign key can prove that one row references an existing row. It cannot, by
itself, specify:

- whether the referencing row semantically depends on the referenced row;
- whether impact flows forward, backward, both ways, or not at all;
- whether a relationship is evidence, definition, contradiction, chronology,
  implementation, knowledge, or presentation order;
- which endpoint types and lifecycle states make the relation authoritative;
- which changes require review;
- whether missing data means false, unknown, or outside coverage;
- how a changed schema affects existing records;
- what evidence explains a validation result.

An AI could infer those meanings heuristically from table/column names, but then
ValidatedWorld could not promise repeatable impact or validation across models.
That would reduce the product to a prompt and database convention.

The product therefore owns a small fixed physical schema and logical metamodel.
Profiles add vocabulary through versioned type definitions and registered
deterministic validators, not arbitrary physical DDL.

## The common metamodel

Every project contains:

- **Schema package selections:** exact versions of the logical profiles that
  define allowed record and relation types.
- **Records:** stable-ID typed nodes with revisions and typed field values.
- **Relationships:** stable-ID first-class relations with named endpoints,
  provenance, fields, and declared impact semantics.
- **References:** record-valued fields extracted into indexed foreign-key rows.
- **Constraints:** instances of a closed deterministic constraint catalog.
- **External artifacts and anchors:** ordinary profile records that point to
  locations outside the database without loading their bytes.
- **Transactions:** draft add/replace/remove operations against an exact head.
- **Reviews:** dispositions tied to exact projected records and impact paths.
- **Commits:** accepted operations, hashes, author/intent, and audit evidence.

The common engine need not know that a record is a character or an electrical
quantity. It must know its logical type, field schema, references, and applicable
relationship/constraint semantics. Profiles provide the domain interpretation.

## Persistence responsibilities

SQLite guarantees and indexes structural facts such as:

- globally unique project record and relation IDs;
- existing project/type/endpoint/target rows;
- non-null and simple check constraints;
- restricted deletes;
- atomic multi-table writes;
- indexed lookup by ID, type, endpoint, and revision;
- durable storage of drafts, reviews, commits, and diagnostics.

The application must enable and verify foreign-key enforcement on every
connection. Direct canonical SQL mutation is unsupported because it bypasses the
semantic commit pipeline.

The physical database file is not content-addressed. SQLite page layout can
change without logical meaning changing. Project identity is a hash of a
deterministically ordered logical JSON snapshot derived from the database.

## Semantic responsibilities

The C# engine can deterministically prove or disprove, within explicit data and
supported profiles, properties such as:

- logical type definitions and field values are valid;
- relationship endpoint roles and kinds are compatible;
- every graph-relevant reference was extracted and indexed;
- selected accepted assertions explicitly contradict;
- a requirement or conclusion has declared support/traceability;
- selected dependency kinds contain no forbidden cycle;
- a transaction's impact is complete over every declared dependency rule;
- every policy-selected impacted record has a current disposition;
- a stale transaction cannot commit;
- a rejected transaction leaves all authoritative database rows unchanged;
- accepted operations replay from the recorded base to the same logical hash.

For a later finite interactive-state profile, bounded exhaustive analysis may
prove a property or return a replayable counterexample. Reaching a configured
bound is inconclusive.

Every result is labeled:

- **Proven:** every required deterministic phase completed and the declared
  property holds.
- **Disproven:** a deterministic rule found a violation with evidence.
- **Inconclusive:** missing annotations, unsupported schema/profile data, bounds,
  cancellation, or failure prevented a conclusion.

## What cannot be guaranteed

ValidatedWorld cannot generally:

- recover every fact or dependency from unrestricted prose;
- detect a relationship that was never modeled;
- infer reliable semantics from arbitrary user-created SQL tables;
- prove a scientific claim, design, citation, patent, or legal argument correct;
- judge literary quality, emotion, originality, persuasion, or fun;
- generate or render a finished document or game;
- verify that an external artifact reflects current project state;
- guarantee an AI correctly applies impact guidance;
- exhaust an unbounded game/campaign state space;
- decide arbitrary logic in an unrestricted rule language.

## Transactions rather than long database sessions

A human or AI may spend minutes or hours constructing a draft, but SQLite write
transactions must remain short. A ValidatedWorld transaction is therefore an
application-level durable draft, not a database transaction held open for the
entire review cycle.

The flow is:

1. Store draft operations with exact base revision/hash.
2. Project and validate them outside an authoritative write lock.
3. Gather required review dispositions.
4. Begin a short SQLite write transaction.
5. Recheck the authoritative head and all preconditions.
6. Rebuild/revalidate any evidence that can have become stale.
7. Apply all normalized rows and audit data.
8. Update the project head and commit SQLite atomically.

There is no separate semantic diff. The accepted operations are the direct
change record; impact is the transitive consequence record.

## Relationship to external documents

Profile records may identify an external manuscript, design, patent workspace,
source tree, Unity project, or dataset. Anchor records identify locations such as
chapters, sections, components, tests, or scenes. Typed relations bind those
anchors to semantic records.

When a fact changes, impact analysis can report affected anchors. That is
guidance, not synchronization. An anchor disposition proves only that the graph
impact was considered, not that external bytes were edited correctly.

## Relationship to RAG and existing products

RAG retrieves likely relevant material for generation. GraphRAG commonly extracts
a heuristic graph from text and uses it as a retrieval index. ValidatedWorld
instead maintains deliberately authored authoritative state and rejects invalid
changes. It may supply deterministic context to a RAG system but is not itself a
RAG pipeline.

The closer overlaps are requirements/traceability tools, SHACL/RDF validation,
typed graph databases, and version-controlled databases. See
[prior_art_and_positioning.md](prior_art_and_positioning.md).

The existence of those systems narrows the product claim. ValidatedWorld must not
become another database or generic trace-link UI. Its candidate novelty is the
combination of:

- profile-independent typed semantic records;
- relationship-specific deterministic impact;
- impact over both base and projected graph;
- mandatory explained review before atomic commit;
- explicit evidence/coverage/inconclusive reporting;
- later narrative and interactive-state profiles.

## Why SQLite, not a server or graph database

SQLite is sufficient for the initial scale, supports foreign keys, indexes,
recursive queries, and atomic transactions, and preserves a one-file portable
project. Hundreds of pages will normally produce thousands or tens of thousands
of semantic records, not a database-scale challenge.

The likely bottleneck is graph density and full validation, not storage bytes.
Gate A includes synthetic performance tests at 100,000 records and 1,000,000
derived dependency edges.

A web/PostgreSQL host is authorized only when real requirements include multiple
concurrent writers, centralized authorization, multi-tenancy, or remote service
operations. A specialized graph store is authorized only when measured
traversal/query workloads exceed the indexed relational implementation and its
operational cost is justified.

## Smallest useful product

Gate A is an **embedded relational graph transaction and validation tool**.

It supports:

- one authoritative `project.vw.db`;
- a fixed SQLite schema with migrations and integrity checks;
- a small logical record/relation metamodel;
- one versioned built-in technical profile;
- stable IDs, revisions, typed values, references, and relation endpoints;
- deterministic logical JSON serialization and hashing;
- durable application-level draft transactions;
- full projected-state validation;
- base-plus-projected impact with shortest explanation paths;
- mandatory review dispositions selected by policy;
- atomic accepted commit and replayable audit evidence;
- JSON commands/results plus bounded read-only queries.

It does not require:

- arbitrary project SQL schemas or user DDL;
- a direct canonical SQL editing mode;
- document import, generation, rendering, or publishing;
- a custom diff/change-package format;
- a built-in AI provider;
- RDF, a graph database, PostgreSQL, a web app, GUI, or plugin;
- narrative or interactive-state implementation.

## First proof scenario

The `TechnicalProject` sample describes an offline sensor design graph and
external anchors for requirements, power budget, architecture, verification, and
privacy.

Changing average current from 20 mA to 25 mA must impact the runtime result,
battery decision, and relevant anchors. A valid transaction repairs the affected
structured values and dispositions together. The unrelated privacy anchor must
not appear.

The same scenario must demonstrate why raw foreign keys are insufficient: the
engine follows declared semantic dependency rules, not every relational
reference, and explains every selected path.

## Staged proof plan

### Gate A — SQLite semantic change control

Measure:

- expected versus actual impact sets and explanation paths;
- missed and irrelevant impacts;
- modeling cost of types, records, and relationships;
- schema/database integrity versus semantic-validator findings;
- whether atomic commits prevent internally stale state;
- whether lower-cost agents can inspect/query/repair transactions;
- deterministic logical snapshots, reports, hashes, and replay;
- performance at the expected and synthetic upper-bound fixtures;
- usefulness compared with Doorstop and a plain relational schema.

If Gate A succeeds, the project has a useful release direction.

### Gate B — Linear narrative profile

Add a reduced mystery schema for chronology, perspective, knowledge, clues, and
disclosure. Keep manuscripts external. Retain the profile only if it catches
meaningful structured-state errors at acceptable modeling cost.

### Gate C — Interactive-state profile

Only after Gate B, add finite typed variables, conditions, effects, invariants,
and bounded exploration.

### Gate D — Integration and optional hosting

After the schema/protocol are stable, evaluate MCP/plugin packaging and a thin
HTTP host. PostgreSQL is considered only if hosted multi-writer needs are proven.

## Stop and scale-down criteria

If the general logical type system is too burdensome, freeze a smaller fixed
record/relation catalog. If the proposition/assertion technical profile adds no
value over generic nodes and links, remove it. If impact/review offers no
meaningful advantage over Doorstop or ordinary SQL plus review, archive or pivot
the experiment.

## Product language

Prefer:

> ValidatedWorld atomically versions an explicit semantic project graph and
> explains which modeled records must be reconsidered when it changes.

Avoid:

> ValidatedWorld is a database schema, RAG system, or AI prompt that guarantees a
> whole document or world is correct.

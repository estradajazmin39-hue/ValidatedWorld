# ValidatedWorld Product and Architecture Specification

**Status:** Authoritative product specification

**Specification version:** 6.0

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
that graph as one authoritative typed property graph. It is a **semantic
change-control engine**, not a document author, generic database, or RAG system.

The workflow is:

```text
open and integrity-check project.vw.db
→ read the exact logical head revision/hash
→ begin a durable application-level transaction
→ add, replace, or remove typed nodes and edges
→ construct the projected logical state
→ derive dependency graphs from base and projection
→ compute explained transitive impact
→ repair data and disposition selected impacted nodes
→ run every required deterministic validator
→ optionally run required AI semantic review and disposition concerns [Gate B]
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
- One small logical model for typed nodes and binary first-class edges.
- One required project-purpose root and a spanning rooted scope tree over every
  other node.
- Exact versioned logical schema packages/profiles.
- Stable IDs and per-entity revisions.
- Atomic optimistic project transactions.
- Derived dependency/impact indexes.
- Deterministic validators and structured diagnostics.
- Review dispositions for policy-selected impacted nodes.
- A planned one-request OpenAI semantic-review workflow over the complete
  selected transaction context (Gate B).
- Deterministic logical JSON snapshots, commands, and results.
- Accepted commit operations and audit/replay evidence.

### 2.2 What remains external

- Manuscripts, papers, patent applications, manuals, source trees, games, and
  media.
- Extracting meaning from those artifacts.
- Updating, rendering, or publishing them.
- General-purpose AI agent, RAG, composition, generation, and arbitrary prompt
  workflows outside the scoped semantic reviewer.
- Arbitrary user-owned relational schemas.
- Hosted identity, authorization, collaboration, and multi-tenancy.

External artifact and anchor nodes may point to external material, but the
engine does not dereference, parse, edit, or certify it.

### 2.3 Evidence classes

Every semantic validation result is:

- **Proven:** all required deterministic phases completed and the declared
  property holds.
- **Disproven:** a deterministic rule found a violation, with evidence.
- **Inconclusive:** missing annotations, unsupported schema/profile data, a
  configured bound, cancellation, or failure prevented a conclusion.

A separate Gate B result class is **Concern**: a heuristic reviewer identified a
possible issue using supplied context. Review completion, freshness, request
coverage, and concern disposition are auditable workflow facts. Concern
correctness is not deterministic, and a concern is never silently promoted to a
`Disproven` result.

Database constraint success is structural evidence, not proof of semantic
validity. An incomplete semantic phase is never a pass.

## 3. Three layers of schema

ValidatedWorld separates three ideas often conflated as “the schema.”

### 3.1 Physical SQLite schema

Implementation-owned tables store projects, compact schema definitions, graph
entities, edge endpoints, drafts, validation reports, commits, and later AI
review runs.

The physical schema changes only through application migrations. A project or AI
does not issue canonical DDL.

### 3.2 Logical metamodel

The common domain defines:

- schema packages with node and edge type definitions;
- nodes with stable IDs, revisions, scalar properties, tags, and extensions;
- binary first-class edges with stable IDs, source/target nodes, properties, and
  an edge-type impact mode;
- one purpose root and a `scope-parent` spanning tree over every other node;
- constraints represented as typed nodes plus explicit target edges;
- policy, transactions, impact, review, and commit evidence.

This model is intentionally opinionated. Every graph-relevant connection is an
edge; scalar properties cannot hide references. Without that rule, the engine
cannot know which connections produce impact or how to explain validation.

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
- node type definitions;
- edge type definitions;
- supported constraint kinds;
- required validator IDs/versions;
- compatibility and migration metadata.

Projects enable exact package IDs, versions, and hashes. The full definitions are
stored in the project database so the file is self-describing.

Generic property/type validation may run from package data. Semantic validator code
runs only when the exact declared validator implementation is registered. Missing
implementations make affected coverage inconclusive.

Gate A ships `core/v1` and `technical-project/v1`. Project-authored packages and
schema evolution are deferred until the fixed-package POC succeeds.

### 4.2 Node type definitions

A node type declares:

- stable type ID and display name;
- owning package;
- scalar-property definitions;
- whether it is the purpose, a scope/group, constraint, external artifact, or
  anchor category;
- categories/tags used by policy;
- applicable generic constraint selectors.

### 4.3 Scalar property definitions

A property definition declares:

- stable property name;
- value kind: `text`, `integer`, canonical `decimal`, `boolean`, `symbol`, or
  `instant`;
- minimum/maximum cardinality;
- order significance;
- allowed symbols/ranges/patterns where supported;
- whether it contributes to display/search only or semantic identity.

Gate A avoids arbitrary nested objects. Repeated structured concepts become
nodes or first-class edges. Namespaced extension JSON is allowed only as
uncovered data and cannot contain hidden canonical references. An ID-shaped
string in a property has no graph meaning.

### 4.4 Edge type definitions

An edge type declares:

- stable type ID and owning package;
- allowed source and target node types;
- scalar property definitions and provenance requirements;
- impact mode: `none`, `source-depends-on-target`,
  `target-depends-on-source`, or `bidirectional`;
- whether the edge type must be acyclic or has other registered validators.

Examples:

```text
derived-from(result -> premise):
  source-depends-on-target

supports(supporter -> supported-assertion):
  target-depends-on-source

contradicts(left -> right):
  bidirectional

mentions(source -> mentioned):
  none
```

This declaration—not SQL foreign-key direction—defines semantic impact. A
relationship requiring more than two roles is represented as a typed node with
ordinary edges to its participants. This retains expressiveness without a second
hyperedge model in Gate A.

## 5. Logical graph entities

### 5.1 Stable identity

Every node and edge has a globally unique stable entity ID within the project
and a committed revision. Names, labels, paths, and
SQL row IDs are never semantic references.

### 5.2 Project purpose and scope hierarchy

Every project contains exactly one `core:project-purpose` node, identified by
`ProjectSnapshot.PurposeNodeId`. Project initialization requires a substantive
plain-English purpose and creates this node before ordinary content. Its
conventional stable ID is `purpose:root`; it is ordinary canonical data and may
be changed only through a transaction.

Every other node has exactly one outgoing `core:scope-parent` edge to its parent.
A parent may have many children. Repeatedly following a node's parent must be
acyclic and terminate at the purpose root. This is a spanning tree over all
canonical content nodes, including scope/group, constraint, and anchor nodes.
Edges obtain review context from their endpoints and do not themselves need a
scope parent.

`scope-parent` declares the child dependent on the parent and creates review
impact. This does **not** turn every leaf edit into whole-project impact:

- impact traversal is seeded only by actual transaction operation targets;
- ancestors included later for review context never become impact seeds;
- changing a leaf includes its single upward lineage but not sibling branches;
- directly changing an intermediate scope node can impact its descendant subtree;
- directly changing the purpose root impacts every descendant and is the
  deliberate full-project-review operation.

An upward walk never goes back down. The separate semantic dependency graph may
branch and cross-link normally; the scope hierarchy exists to make contextual
ownership and purpose explicit, not to replace those domain dependencies.

The purpose is meaningful context for deterministic and AI review, but arbitrary
natural-language conflict with it is not deterministically decidable. For
example, a `Whopper` under a McDonald's-menu purpose becomes a cited AI concern
unless a profile also supplies a closed deterministic vocabulary rule.

### 5.3 Nodes

A node contains:

- stable ID and revision;
- logical node type ID;
- typed scalar properties conforming to its type definition;
- ordinal tags;
- namespaced extension JSON;
- lifecycle metadata when its profile declares it.

Examples include scopes, requirements, assertions, characters, events,
constraints, artifacts, and anchors. They are profile node types, not hardcoded
physical tables.

### 5.4 Edges

An edge is first-class and contains:

- stable ID and revision;
- logical edge type ID;
- exactly one source node and one target node;
- typed scalar properties such as rationale;
- provenance;
- tags and extension data.

First-class edge identity allows evidence, provenance, revision, and direct
transaction operations without treating a connection as an anonymous pair.
Higher-arity relationships and relationships that themselves need scope are
reified as nodes with typed edges.

### 5.5 Constraint nodes

Gate A uses a closed constraint catalog whose instances are typed nodes connected
to their targets by explicit edges. They may select nodes/edges further by type,
property, tag, lifecycle state, or edge kind. Initial generic/technical kinds
cover:

- no selected contradictory accepted assertions;
- required support for selected assertion roles;
- acyclic selected dependency edge types;
- unique active definitions;
- required implementation and verification coverage;
- required impact dispositions;
- exactly one purpose and singular acyclic scope lineage to it.

There is no arbitrary SQL, expression, trigger, or scripting constraint language.

### 5.6 External artifacts and anchors

Artifact and anchor are logical profile node types. They may store opaque
external locators and observed external version/hash claims. Anchors may form an
acyclic ordered hierarchy and bind to semantic nodes using typed edges.

The engine never follows a locator or verifies external bytes.

### 5.7 Open-world default

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

SQLite runs in-process. The application ships a pinned native SQLite build
through NuGet and owns project creation, migration, integrity verification,
sample generation, and backup. Users do not install a SQLite server, standalone
SQLite CLI, system provider, or Docker, and ordinary authoring does not require
SQL knowledge. Documented read-only views remain optional for advanced users and
agents.

### 6.2 Structural tables

The normative table design is in the blueprint. Conceptually it contains:

```text
metadata and migration history
project/head
schema_packages, entity_types
graph_entities, graph_edges
draft_transactions
validation_runs
commits
```

This is nine v1 tables including migration history. `graph_entities` provides a
global node/edge ID space; `graph_edges` supplies source/target foreign keys.
Scalar property/tag/extension maps and ledger payloads use validated canonical
JSON. Deletes restrict rather than cascade. Add a materialized property index
later only if measured query performance requires it.

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

- project head metadata, policy, and `purposeNodeId`;
- selected complete schema package/type definitions;
- all current nodes, edges, scalar properties, tags, and extensions;
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
- one final add/replace/remove operation per target entity ID;
- expected entity revision for replacements/removals;
- submitted review dispositions and allowed warning acknowledgements.

Drafts are durable database rows but are not canonical logical state.

Clients cannot set committed entity revisions. Adds start at 1; replacements
increment the authoritative revision.

### 7.2 Focused batch authoring

A draft may remember a noncanonical focus node. An authoring batch supplies new
nodes and edges plus an optional focus. Each new node lacking an explicit
`scope-parent` receives one to the focus. The application expands the shorthand
into explicit edge operations and returns that expansion before validation.

A cluster is an ordinary scope node created with its children in one batch.
Profile helpers may expand common deterministic patterns in the same way. Focus,
templates, and shorthand never enter canonical state or create semantic
dependency edges by inference. Once expanded, the stored draft and change-set
hash contain only ordinary explicit node/edge operations.

### 7.3 Projection

The Application layer loads the base graph, applies draft operations to isolated
node/edge builders, materializes the projection, and validates it. There is no
cascade delete or partial
canonical write.

Gate A may perform full projection and validation in memory. Persistence is
incremental; semantic validation need not be incremental yet.

### 7.4 Dependency graph

The canonical edges are the only authored connectivity. Each edge type expands
to zero, one, or two operational dependency arcs:

- `none` creates no impact arc;
- `source-depends-on-target` creates source → target;
- `target-depends-on-source` creates target → source;
- `bidirectional` creates both.

`scope-parent` uses source child → target parent with
`source-depends-on-target`. Presentation order, provenance, and query-only edges
use `none`. Validators may reject or diagnose the graph but cannot invent hidden
dependency arcs from scalar properties.

### 7.5 Impact

Node-operation targets are direct impact seeds. For an edge operation, the
changed edge is recorded and its dependent endpoint(s), determined from the base
and projected edge/type, become node seeds; an impact-`none` edge adds no node
seed. The engine traverses reverse
review-impact edges in the union of base and projected dependency graphs:

- base edges retain impact from removed/redirected dependencies;
- projected edges include impact from new dependencies.

Only direct operation targets seed traversal. Records added afterward as
contextual ancestors, including the purpose root, are never fed back into impact
traversal. Consequently a leaf's upward purpose lineage cannot accidentally
select siblings; the root selects the full project only when the transaction
directly changes it.

Results contain changed entity IDs, node seeds, impacted node IDs, shortest
deterministic explanation paths, edge evidence, completeness/bounds, and
statistics.

### 7.6 Review obligations

Policy selects which impacted nodes require one disposition:

- `updated` — node is directly changed by the transaction;
- `reviewed-no-change` — reviewer says projected node remains correct;
- `not-applicable` — reviewer says the explained path requires no action;
- `pending` — commit blocks when policy requires review.

Nonautomatic dispositions require reviewer, rationale, and time. They are
fingerprinted over the projected target, change set, and impact path. Operation
changes invalidate stale dispositions.

### 7.7 Planned AI semantic review — Gate B

After deterministic projection, validation, impact, and obligation construction,
a project policy may require AI semantic review for selected changes. The
application builds one exact request from the complete transaction, every
policy-selected impact closure and explanation edge, required forward
dependencies, applicable constraints and anchors, and the singular upward scope
lineage from every included node to the project purpose. All disjoint
chains remain together in the same request.

The reviewer returns structured concerns with cited entity/property/edge
evidence, insufficient-context observations, and separately identified candidate
nodes, edges, or operations. Candidates never become canonical automatically.
Policy may require a complete fresh run and a disposition for every concern.

The request has an exact preview and coverage manifest. If all required context
cannot fit the selected model's input, the run is inconclusive before any paid
call. There is no sharding, synthesis, fallback model, or automatic retry. This
guarantees which context was offered, not model comprehension or correctness.

Provider refusal, timeout, cancellation, malformed output, unavailable
credentials, or incomplete coverage makes the review failed or inconclusive; it
is not a content-validation error. No network call occurs inside a SQLite write
transaction. The normative later-phase design is
[Planned AI semantic review](ai_semantic_review.md).

Gate B project policy is `disabled`, `optional`, or `required`. An optional
transaction may explicitly record `skipped` with actor and rationale; a required
review cannot be skipped. `VW_AIREVIEW__LIVETESTS` controls only whether the
separate live-provider test harness is eligible to run. It is ignored by unit and
ordinary end-to-end tests and never overrides project/transaction policy.

### 7.8 Commit

After an apparently valid reviewed draft, commit:

1. Opens a short SQLite write transaction.
2. Rechecks database integrity, project head, and entity preconditions.
3. Reprojects/revalidates evidence that could be stale.
4. Applies every graph-entity and edge-endpoint change.
5. Inserts accepted operation, disposition, report, and commit rows.
6. Updates the project head revision, parent hash, and logical hash.
7. Commits SQLite once.

Any failure rolls back every authoritative and audit write.

## 8. Deterministic validation

Validation phases are:

1. SQLite application/schema/migration/integrity checks.
2. Logical snapshot/hash integrity.
3. Schema package/type/property coverage.
4. Entity ID, revision, scalar value, and edge-endpoint integrity.
5. Edge-type impact-mode validity.
6. Profile structural and semantic rules.
7. Contradiction, support, definition, cycle, and traceability constraints.
8. Transaction impact completeness and review dispositions.
9. Commit policy.

Database failures are structural errors. Semantic phases skipped because a
prerequisite failed are explicitly inconclusive.

Diagnostics include stable code/rule version, outcome/severity, primary/related
IDs, property/edge evidence, impact path where applicable, safe repair
categories, source command pointer, and deterministic fingerprint.

## 9. JSON and SQL interfaces

### 9.1 JSON protocol

Every CLI command consumes arguments and optional versioned JSON request bodies
and writes exactly one versioned JSON result to stdout. Logs go to stderr.

Required use cases:

- initialize, inspect, verify, and snapshot a project;
- get/list/query logical nodes and edges;
- inspect dependencies/dependents and explanation paths;
- begin/show/apply/validate/commit/abort transactions;
- list and disposition review obligations;
- retrieve commit evidence and verify replay;
- build bounded relevant-node/edge context.

A logical snapshot is backend-neutral data interchange and test evidence. It is
not a finished-document export.

The `.vw.db` file or an application-produced backup is the primary complete
project interchange because it preserves drafts and history. Logical JSON is an
additional transparent interchange, audit, revision-zero initialization, and
fixture surface.

The CLI supplies `sample list/create` for reusable built-in scenarios. Sample
databases are created by the app from retained logical source assets through the
same initialization/persistence paths as normal workspaces, never by copying or
editing an opaque test database.

### 9.2 Database access

Because AIs and developers can query SQLite effectively, the database exposes
documented read views for logical nodes, edges, scalar property JSON, direct
dependencies, commits, and diagnostics.

Direct SQL reads are supported after integrity verification. Direct SQL writes
to canonical tables are unsupported. Gate A does not implement a general SQL
proxy or attempt to securely classify arbitrary SQL text.

An optional future transaction-scoped SQL authoring surface may write only to
validated draft/staging structures. It must not bypass Application operations.

## 10. Profiles

### 10.1 Technical project profile — Gate A

Logical node types cover artifacts, anchors, terms, quantities, components,
requirements, propositions, assertions, and evidence. Edge types cover
depends-on, derived-from, supports, contradicts, defines, uses, implements,
satisfies, verifies, cites, binds, and mentions.

Roles distinguish fact, assumption, hypothesis, requirement, observation,
result, conclusion, decision, recommendation, and definition. Lifecycle status
distinguishes proposed, accepted, rejected, deprecated, and superseded.

The profile proves structured traceability, not scientific, engineering, patent,
or legal correctness.

### 10.2 AI semantic review — Gate B

Gate B is a cross-profile review service, not another domain ontology. It adds
one-request coverage plans, structured concerns, freshness and disposition
policy, a fake test client, and one dependency-isolated OpenAI production client
using `gpt-5.6-terra` with medium reasoning. It is evaluated first against
deliberately omitted or stale TechnicalProject semantics.

### 10.3 Linear narrative profile — Gate C

Adds fictional time, temporally scoped assertions, events, character
knowledge/belief, narrative order, clues, and explicit disclosure/deduction
rules. Manuscripts remain external.

Keep canon truth, character perspective, fictional time, narrative order, and
authoring revision separate.

### 10.4 Interactive-state profile — Gate D

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

ValidatedWorld.AiReview             later; Core + Serialization + Validation
ValidatedWorld.AiReview.OpenAI      later; AiReview + pinned OpenAI client
ValidatedWorld.Mcp                  later; Application + selected persistence
ValidatedWorld.Web                  later; Application + selected persistence
```

Application defines persistence ports. SQLite implements them. Validation
operates on immutable logical snapshots and indexes, not SQL connections. This
keeps semantics backend-neutral without pretending all database engines have the
same operational behavior.

During Gate B, Application adds a reference to the provider-independent internal
review contracts and persistence ports; SQLite implements those ports, and the
CLI alone composes the sole OpenAI production client. The interface exists for
offline testing and dependency isolation, not to promise multiple providers. No
deterministic-core project references a provider SDK.

## 12. Gate A proof of concept

The `TechnicalProject` fixture starts with one plain-English purpose and a rooted
scope hierarchy, then includes an offline sensor design with external anchors for
requirements, power budget, architecture, privacy, verification, manuals, and
unrelated accessibility material. All are nodes; traceability/dependency facts
are edges.

The initial graph contains a 24-hour runtime requirement, a 20 mA current
assumption, a 500 mAh capacity assumption, a 25-hour runtime result, and a
battery-sufficiency conclusion with explicit dependencies.

A transaction changes current to 25 mA. It must impact the runtime result,
battery decision, and power/architecture/verification anchors, but not the
privacy or accessibility tracks.
A valid transaction repairs structured capacity/runtime values and all required
dispositions together.

The changed power leaf's ancestors are included as context without selecting
their privacy/accessibility children. A focused authoring batch can create the
power cluster and its children without repeating their scope parent. A separate
transaction that directly
changes the purpose root must impact every descendant, demonstrating the one
intentional full-project-review operation.

The fixture also includes a realistic soft-logic design track: no-upload and
retention requirements, definitions, assumptions, privacy claims, architecture
decisions, evidence, implementation/configuration, verification, document
anchors, explicit contradictions, missing-information variants, and unrelated
distractors. Transactions change retention or narrowly permit diagnostic upload
and must surface the exact modeled privacy, architecture, verification, and
documentation consequences without pulling in unrelated power/accessibility
records.

Gate A must prove:

1. Database application/migration/integrity verification.
2. Relational foreign-key and restricted-delete behavior.
3. Deterministic database-to-logical-JSON round trip and hash.
4. Stable-ID transaction projection and optimistic preconditions.
5. Exact complete impact with explainable paths.
6. Exact-one purpose and singular, acyclic, root-reaching scope validation.
7. Context-only ancestor ascent never selects siblings, while a direct root
   change selects the full project.
8. Deterministic validation and coverage.
9. Pending obligations block commit.
10. Rejected/stale commits roll back every database row.
11. Accepted operations replay to the recorded logical hash.
12. Documented read views and JSON results are deterministic.
13. A lower-cost agent can query and repair the project without direct writes.
14. Synthetic 100,000-node/1,000,000-edge performance stays within documented
    budgets.
15. Starting with the first database/CLI walking skeleton, each usable work
    package has a replayable realistic end-to-end scenario and an actual
    AI-agent black-box QA walkthrough through public commands.
16. Agent QA findings become regression tests when deterministic and are
    reported to the human when they expose friction, misleading behavior,
    excessive modeling burden, or a questionable product direction.
17. The nine-table v1 layout preserves entity/type/endpoint integrity without a
    normalized property-value or relation-role subsystem.
18. Focused batch expansion produces explicit deterministic operations and makes
    realistic graph entry practical for a lower-cost agent.

No document import/rendering, arbitrary DDL, AI provider, web server, RDF store,
graph database, narrative timeline, or game exploration is required.

Gate A's no-provider boundary is deliberate: it proves a useful local product
with no secret or mutable remote service. It does not remove the planned Gate B
semantic reviewer.

Gate A is implemented through the blueprint's ordered work packages. Each
engineering package has deterministic automated acceptance and full-solution
verification; passing behavior is established by repository evidence rather than
manual human inspection. Completion evidence and the next authorized assignment
must be recorded in the execution plan before another package begins.

Automated tests and agent QA serve different purposes. Scripted tests prove
repeatability and prevent regressions. A real agent using a disposable database
and only the public CLI/help tests discoverability, diagnostic usefulness,
workflow burden, and whether the structured graph is practically usable. Both
are required from the first coherent database/CLI slice onward; usability is not
deferred until the final Gate A evaluation.

A human invokes each agent run. The agent completes the Current task or reports
why it failed, then stops; it does not perform Git workflow operations or
automatically start the next task. Gate A completion places the plan at
Current task `None` until a human declares the roadmap finished or requests a
separate later-phase planning task.

## 13. Gate B AI semantic-review proof

Gate B begins only after WP9 records a successful Gate A result and a human
requests a separate planning task. It must:

1. Preserve all deterministic behavior when the AI-review assemblies and
   configuration are absent.
2. Build one hash-addressed whole-transaction request with explicit node/edge
   coverage, all disjoint selected chains, singular purpose lineages, omissions,
   bounds, and freshness.
3. Accept only versioned schema-valid concerns that cite supplied IDs and
   evidence.
4. Keep candidate links/operations noncanonical until explicitly applied.
5. Enforce required run/concern dispositions without calling the concerns true.
6. Make every client failure, refusal, bound, and cancellation auditable and
   inconclusive, with zero automatic retries.
7. Keep secrets outside the project/database/protocol and never invoke a
   provider implicitly.
8. Pass its normal suite with one fake client and scripted HTTP—no key or live
   network.
9. Support exactly one production configuration: OpenAI `gpt-5.6-terra` with
   medium reasoning; do not add provider or model alternatives.
10. Refuse implementation unless the initiating human has personally installed
    the key and supplied the exact readiness attestation in the AI-review design.
11. Require a second exact human attestation before one live call; preview the
    complete request first and never retry automatically.
12. Evaluate that explicitly enabled path on known missing/stale issues, false
    positives, cost, latency, and scoped-versus-unscoped usefulness.

If the built-in call does not add enough measurable value, omit Gate B rather
than broadening into provider selection or general AI orchestration. Gate A
remains useful independently.

## 14. Success and stop criteria

Gate A succeeds if explicit typed edges surface the correct nodes and
anchors, transactions prevent stale semantic state, SQLite reduces persistence
complexity, agents can repair failures from evidence, and modeling cost is
acceptable.

Scale down to a smaller fixed type catalog if the general logical schema package
model is too complex. Remove the proposition/assertion layer if generic typed
nodes and edges perform as well. Stop if the result offers no meaningful
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

## 15. Durable direction

> Keep one explicit relationally stored semantic graph internally coherent across
> revisions, explain what each proposed change affects, and let external tools
> decide how to use the accepted state.

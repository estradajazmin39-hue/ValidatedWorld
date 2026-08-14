# ValidatedWorld Product and Architecture Specification

**Status:** Authoritative product specification

**Specification version:** 8.0

**Last reviewed:** 2026-08-13

**Primary implementation:** .NET 10 / C#

**Authoritative workspace and interchange:** SQLite `project.vw.db`

This specification defines the product boundary. The guarantee and falsification
plan are in [feasibility.md](feasibility.md). Exact data structures, SQL,
algorithms, tests, and work packages are in
[implementation_blueprint.md](implementation_blueprint.md). Actual progress and
the only current coding assignment are in
[implementation_execution_plan.md](implementation_execution_plan.md).

Human direction overrides these documents. When product direction changes, the
controlling documents must change together before implementation continues.

## 1. Product thesis

Long authored projects are sequential in presentation but graph-shaped in
meaning. ValidatedWorld stores their deliberately modeled, continuity-critical
facts as human-readable nodes connected by explicit relationships.

The core problem is not storage. It is controlled change:

```text
open and verify the current SQLite graph
→ start one in-memory change session
→ add, replace, or remove explicit nodes and edges
→ build the proposed graph without changing the database
→ calculate every affected node and explanation path
→ inspect, update, or disposition the complete affected set
→ run structural and any enabled optional-profile checks
→ obtain exact user approval when an AI is authoring
→ atomically replace the current SQLite state or roll back everything
```

The common engine does not need a universal ontology or rich type system. A node
must have meaningful text; an edge must say what it connects and how review
propagates. Optional profiles may recognize conventional node kinds,
relationship labels, attributes, and deterministic validators, but plain graph
projects remain first-class.

Semantic consistency of natural-language content is judged by a human or an
optional AI. The application deterministically guarantees structural validity,
affected-set completeness for modeled edges, complete review workflow, exact
approval binding, and atomic persistence. It does not claim to prove prose true.

AI is optional but strongly intended. The built-in authoring agent can operate
the same text-oriented commands as a human, allowing a user to modify a project
far larger than a model context window through repeated bounded search and
traversal. A separate optional reviewer can perform an expensive heuristic pass
over one complete proposed transaction and its selected context.

## 2. Product boundary and evidence

### 2.1 ValidatedWorld owns

- One portable current-state SQLite project file.
- A fixed, migrated physical schema owned by the application.
- Stable-ID human-readable nodes and first-class labeled binary edges.
- Exactly one purpose root and one spanning `scope-parent` tree.
- Explicit relationship review directions.
- In-memory operation batches and projected graphs.
- Base-plus-projected affected-set analysis with explanation paths.
- Complete per-session review dispositions.
- Structural validation and optional profile validation.
- Exact conversational confirmation for AI-authored commits.
- One short atomic SQLite write or complete rollback.
- Bounded search, tree traversal, dependency queries, and structured command
  results.
- Application-controlled SQLite backup and optional SQL export.

### 2.2 ValidatedWorld does not own

- Historical project revisions, commit history, replay, branches, merges, or
  time travel.
- Persisted unfinished drafts or recovery after the application closes.
- A second JSON project/snapshot format.
- Finished novels, papers, patents, manuals, code, game projects, or media.
- Automatic synchronization, rendering, publication, or certification of those
  products.
- Deterministic understanding or extraction of arbitrary natural language.
- Images, OCR, document parsing, web crawling, or source-tree ingestion in the
  current roadmap.
- A mandatory AI provider, MCP/plugin package, web service, or graphical UI.
- Arbitrary user-created database tables, triggers, or executable rules.

### 2.3 Evidence classes

Deterministic application results use:

- **Valid:** every applicable structural/workflow/profile check completed and
  passed.
- **Invalid:** a deterministic check found a violation with evidence.
- **Inconclusive:** missing profile code, configured bounds, cancellation,
  unavailable optional analysis, or an internal/provider failure prevented a
  conclusion.

Manual review is an auditable session action, not proof that the reviewer's
judgment was right. Optional AI review returns **Concerns** with cited graph IDs.
AI-authored operations are **Proposals** until the user approves the exact final
preview and the guarded application commit succeeds.

## 3. Canonical graph model

### 3.1 Project

A project has:

- stable project ID and title;
- exactly one purpose-node ID;
- a current-state fingerprint used for integrity/staleness checks;
- optional enabled-profile IDs/settings;
- creation and last-update timestamps; and
- current nodes and edges.

The state fingerprint is SHA-256 over a deterministic internal encoding of the
current project fields, profile settings, nodes, and edges. It is an opaque
integrity token, not a revision number, parent hash, history entry, public JSON
snapshot, or interchange artifact.

### 3.2 Nodes

A canonical node contains:

- globally unique stable ID within the project;
- non-empty human-readable text;
- optional free-form kind;
- sorted tags; and
- optional scalar attributes.

Supported scalar attribute values are text, signed integer, canonical decimal,
boolean, symbol, or UTC instant. Attributes cannot contain semantic node/edge
references. Any connection that should affect review is an explicit edge.

The engine treats `kind` and attributes as descriptive data unless an enabled
profile claims and validates them. It does not reject an unknown kind in a plain
graph.

### 3.3 Edges

A canonical edge contains:

- globally unique stable ID in the same identity space as nodes;
- source-node ID and target-node ID;
- non-empty human-readable relationship label;
- review direction: `none`, `source-to-target`, `target-to-source`, or `both`;
- optional rationale/text, tags, and scalar attributes.

Review direction means only:

```text
source-to-target  a changed source selects the target for review
target-to-source  a changed target selects the source for review
both              either changed endpoint selects the other
none              relationship is query/context only
```

Foreign-key direction does not imply semantic direction. Labels such as
`derived-from`, `knows`, `contradicts`, or `mentions` have no hidden behavior in
the common engine. Optional profiles may recommend or require direction for
recognized labels.

Relationships requiring more than two roles can be represented as a node joined
to participants by ordinary edges.

### 3.4 Purpose and scope

Initialization creates one substantive purpose node, conventionally
`purpose:root`. Every other node has exactly one outgoing `scope-parent` edge to
another node. Repeated parent traversal is acyclic and terminates at the purpose.

The scope tree organizes a potentially cross-linked graph and gives every node a
singular contextual path to the project's thesis. It does not make every local
change global:

- a changed leaf includes its ancestors as context but does not seed their other
  children;
- directly changing a scope node selects its descendant subtree;
- directly changing the purpose root selects the whole project; and
- semantic cross-links independently select nodes according to their declared
  directions.

Ancestors shown as context become direct seeds only if the user or agent changes
them. This preserves the previously established rule that upward scope traversal
never randomly fans back down.

### 3.5 Open-world semantics

Missing text or edges mean unknown/unmodeled, not false. ValidatedWorld cannot
find a dependency that no author or AI entered. Optional profiles may define a
finite closed-world rule, but diagnostics must state that assumption.

## 4. Optional profiles

A profile is an application module, not a prerequisite layer under every graph.
It may provide:

- recommended/controlled node kinds and relationship labels;
- attribute schemas;
- relationship-direction defaults or requirements;
- deterministic validators;
- search/display conventions; and
- explicit authoring helpers that expand into ordinary nodes and edges.

Profile data stored in the project consists only of stable profile ID, version,
configuration, and compatibility fingerprint. Executable code is never loaded
from a project database. If required profile code is unavailable, profile
coverage is inconclusive while the common graph remains readable.

Gate A must prove the plain graph without an enabled domain profile. A small
technical helper may be evaluated afterward, but `technical-project/v1`,
`catalog/v1`, narrative, and interactive-state profiles are not foundational
roadmap dependencies.

## 5. SQLite persistence

### 5.1 Authoritative file

The workspace is one application file such as:

```text
project.vw.db
```

It contains only the current project metadata, nodes, edges, migration history,
and current optional-profile selection. It does not contain drafts, validation
runs, commit operations, review history, or prior graph states.

SQLite runs in process using `Microsoft.Data.Sqlite.Core` and an explicitly
pinned SQLitePCLRaw native bundle. Users do not install a server, SQLite CLI,
system provider, ORM, or Docker.

### 5.2 Physical tables

SQLite v1 uses four tables:

```text
schema_migrations
projects
nodes
edges
```

`projects` contains the current fingerprint and profile-settings JSON. `nodes`
contains text/kind/tags/attributes. `edges` contains endpoints, label, direction,
and optional data. Foreign keys restrict deletes. A partial unique index permits
at most one `scope-parent` per source; application validation proves exact-one,
acyclicity, and root reachability.

### 5.3 Safety

Every connection enables and verifies foreign keys, uses parameterized SQL,
refuses extension loading and unknown/checksum-mismatched migrations, and treats
the supplied file as untrusted until application ID, schema, limits, and
integrity checks pass.

Direct read-only access through documented tables/views is supported after
verification. Direct canonical SQL writes are unsupported because they bypass
review and fingerprint updates. On open and before commit, the application
recomputes current-state identity to detect incomplete external mutation.

### 5.4 Interchange and backup

The `.vw.db` file is the only complete project representation. Safe transfer
uses a closed file or SQLite backup produced by the application. An optional
application command may produce a deterministic SQL schema/data export for
inspection or integration. Importing that export creates a new database through
the application's verified path.

Structured CLI requests/results may use versioned JSON envelopes. They describe
commands and observations, not a complete alternative project state. There is no
`snapshot write/init` feature in Gate A.

## 6. In-memory change sessions

### 6.1 Lifetime

Only one active change session per application process/project is supported in
Gate A. It contains:

- session ID;
- project ID and base-state fingerprint;
- author and intent;
- one final add/replace/remove operation per target ID;
- current projection and operation-set fingerprint;
- affected nodes and explanation paths;
- session-local review dispositions; and
- status: editing, review-pending, ready, committed, discarded, or failed.

The session is not written to `project.vw.db`. Closing the process discards it.
The application warns before closing with unresolved changes when the host can do
so, but crash recovery is not promised.

### 6.2 Operations

Operations are:

- `AddNode`, `ReplaceNode`, `RemoveNode`;
- `AddEdge`, `ReplaceEdge`, `RemoveEdge`.

Each operation contains the complete proposed entity. Removing a node requires
explicit removal or redirection of every incident edge; there is no cascade.
One final operation per stable ID makes the proposal unambiguous.

A focus node and batch helper may reduce entry work. New nodes in a batch may
inherit the focus as their scope parent, but the helper must return the complete
expanded explicit edge operations. No semantic relationship is guessed silently
by deterministic application code.

### 6.3 Projection

The application copies the current graph into isolated builders, applies sorted
operations, and materializes an immutable proposed graph. No canonical row is
changed during authoring or review. Full structural validation may be used in
Gate A; incremental validation is deferred.

## 7. Affected-set and review algorithm

### 7.1 Operational review arcs

Each semantic edge expands to zero, one, or two review-propagation arcs based on
its declared direction. The canonical edges are the only authored connectivity;
there is no second adjacency graph and no inference from text/attributes.

### 7.2 Seeds and union graph

Directly changed nodes are seeds. For an edge operation, affected endpoint seeds
are determined from the edge's old and proposed directions. Traversal uses the
union of current and proposed arcs so deleting or redirecting an edge cannot
hide consequences that depended on the old relationship.

Directly changed scope nodes additionally seed their descendant subtrees.
Ancestors added only for purpose context do not become seeds.

### 7.3 Traversal

Breadth-first traversal uses deterministic ID ordering, records shortest paths,
retains edge evidence for each hop, and returns invalid/inconclusive if configured
depth or node bounds prevent a complete required set.

The result distinguishes:

- direct changed entities;
- affected nodes requiring disposition;
- scope ancestors shown as context only;
- relationship paths and current/projected evidence; and
- excluded/unrelated nodes in test evidence.

### 7.4 Review obligations

Every affected node has exactly one current disposition:

- `updated`;
- `reviewed-no-change`;
- `not-applicable`; or
- `pending`.

Nonautomatic dispositions record a reviewer identity and rationale in the
in-memory session. A fingerprint binds each disposition to the proposed node,
operation set, and selected path. An operation change invalidates stale
dispositions. These records are commit prerequisites but are not persisted as
history after the current graph is written.

## 8. Validation and commit

Deterministic phases are:

1. SQLite application/schema/migration/integrity checks.
2. Current-state fingerprint verification.
3. Entity IDs, node text, edge labels, directions, and endpoint integrity.
4. One purpose and singular acyclic root-reaching scope lineage.
5. Optional profile validation and coverage.
6. Complete affected-set traversal and current dispositions.
7. Exact final user confirmation when the author is an AI.

Commit then:

1. Rebuilds the projection, affected set, and evidence.
2. Rejects invalid, inconclusive, pending, stale, or unapproved sessions.
3. Begins a short SQLite `BEGIN IMMEDIATE` write transaction.
4. Rechecks the current database fingerprint.
5. Applies explicit edge and node operations in foreign-key-safe order.
6. Revalidates the resulting current rows and computes the new fingerprint.
7. Updates project metadata and commits once.

Any failure rolls back every write. A stale-state or busy failure returns the
session to editing/review state so the caller can inspect and retry; a semantic
or structural failure must be repaired before another commit attempt.

## 9. Text-oriented application and CLI surface

Every normal workflow is available without SQL, AI, or a graphical UI:

```text
project init/open/verify/status/backup/export-sql
node get/list/search
edge get/list
scope children/ancestors/subtree
graph neighbors/dependencies/dependents/path/context
change begin/show/focus/expand/apply/affected/review/validate/commit/discard
sample list/create
```

Commands return stable, structured results suitable for a terminal, scripts, or
AI tools. JSON is the initial CLI envelope, but JSON does not represent the
entire project for import/export. Read results include the current state
fingerprint; change results include the base and operation-set fingerprints.

Search is deterministic and bounded: exact ID, text, kind, tag, relationship,
scope subtree, neighbors, dependencies, dependents, and explanation paths. It
does not use embeddings, natural-language SQL, a provider call, or RAG.

Stable read-only SQLite views expose the project, nodes, edges, scope,
relationship arcs, and direct graph navigation for integrations. There is no
general SQL write proxy.

## 10. Optional AI semantic review

When enabled and configured, the application may create one expensive review
request after the deterministic affected set is complete. The request contains:

- the purpose statement;
- the complete operation batch;
- all affected nodes and relationship paths;
- forward/backward explanatory context selected by bounded policy;
- the singular scope lineage of every included node;
- all disjoint change chains together; and
- an explicit coverage/omission manifest.

The request must not load the entire project merely because the root is included
as context. Only a direct purpose-root change intentionally selects the whole
project.

The reviewer is independent, tool-free, and unable to mutate graph state. It
returns structured cited concerns. The user or authoring loop may repair the
in-memory proposal, reject a concern with rationale, or acknowledge it. Any
proposal change invalidates the old review.

AI review has two runtime settings: enabled/disabled and provider availability.
If disabled or `OPENAI_API_KEY` is absent, it is skipped and the manual affected-
set review remains sufficient. Gate A never requires AI review. No project policy
may make an unavailable provider prevent manual use in the initial product.

The sole planned production provider is OpenAI. Calls require explicit displayed
authorization, background response polling, a 1,200-second end-to-end deadline,
and zero automatic paid retries. Failure is inconclusive and returns control to
manual review.

See [ai_semantic_review.md](ai_semantic_review.md).

## 11. Optional AI authoring

When enabled and configured, the authoring agent:

1. accepts a user's description and optional supplied text;
2. verifies the database and reads purpose/current identity;
3. searches before creating potentially duplicate nodes;
4. retrieves bounded relevant nodes, scope, and relationship paths;
5. starts one in-memory change session;
6. applies explicit operation batches through application tools;
7. repeatedly inspects validation and affected-set expansion;
8. asks the user about material ambiguity or opinionated consequences;
9. invokes the independent reviewer only when separately authorized;
10. presents the exact final operations, affected set, and fingerprints;
11. receives explicit conversational approval; and
12. calls the guarded normal commit tool itself.

The model has no raw SQL, direct canonical write, automatic disposition,
profile-code mutation, or unguarded commit tool. Approval is bound to the exact
database, base-state fingerprint, operation-set fingerprint, proposed-state
fingerprint, and review completion. Any change invalidates it.

If AI authoring is disabled or the key is absent, the user performs the same
steps through the text-oriented application surface. AI absence is not an error
in the graph or a reason to block manual commit.

The first proof accepts descriptions and text, not images or general documents.
It must work on a project much larger than one model working set by repeatedly
searching and retrieving bounded context. Multi-agent partitioning is deferred.

See [ai_authoring_agent.md](ai_authoring_agent.md).

## 12. Architecture

```text
ValidatedWorld.Core                 simple immutable graph and change domain
ValidatedWorld.Serialization        structured command/result JSON only
ValidatedWorld.Validation           graph indexes, affected set, structural rules
ValidatedWorld.Application          change/query/commit orchestration
ValidatedWorld.Persistence.Sqlite   four-table schema, mapping, backup/export
ValidatedWorld.Cli                  text/JSON command host and composition root

ValidatedWorld.AiReview             optional later orchestration/contracts
ValidatedWorld.AiReview.OpenAI      optional sole production review client
ValidatedWorld.AiAuthoring          optional later tool loop/confirmation
ValidatedWorld.AiAuthoring.OpenAI   optional sole production authoring client
```

Core has no SQLite, JSON, UI, network, provider, or domain-profile dependency.
Application defines persistence ports. SQLite implements them. AI projects are
optional adapters over Application contracts. No MCP, plugin, web, or graphical
UI project is in the current roadmap.

## 13. Gate A proof

The TechnicalProject scenario uses plain text nodes and relationships—without a
domain profile—to model an offline sensor design. It includes purpose, power,
privacy, documentation, and accessibility scopes; requirements, assumptions,
results, decisions, evidence, verification, and artifact anchors are ordinary
node kinds rather than schema-enforced ontology classes.

Changing average current from 20 mA to 25 mA must select the runtime conclusion,
battery decision, and bound power/verification anchors through explicit edges,
while excluding privacy and accessibility. Changing a retention statement must
select its privacy claim, architecture decision, verification, and documentation
anchors while excluding power. Removing/redirecting an edge must retain affected
nodes from both old and proposed connectivity. Changing the purpose root must
select every node.

Gate A must prove:

1. Four-table schema/migration/integrity behavior and bundled SQLite deployment.
2. Plain text nodes and labeled edges are usable without a profile.
3. Purpose/scope-tree integrity and stable IDs.
4. Deterministic current-state fingerprints without retained history.
5. In-memory change projection and loss on explicit discard/process restart.
6. Complete current-plus-proposed affected sets and explanation paths.
7. Current dispositions block incomplete commit.
8. Leaf ancestor context excludes siblings; direct scope/root changes select
   descendants.
9. Every faulted/stale commit rolls back all current rows.
10. Accepted commit produces exactly the expected current graph.
11. Deterministic search, navigation, read views, backup, and SQL export.
12. Full structured CLI use without SQL, AI, secrets, or GUI.
13. Realistic automated and black-box agent QA from the first usable slice.
14. Acceptable modeling/review burden compared with ordinary SQLite and
    Doorstop-style suspect links.
15. Performance at 100,000 nodes and 1,000,000 review arcs is measured rather
    than assumed.

Gate A stops or narrows if the simple graph adds no useful change-control value.

## 14. Later evidence gates

Gate B optionally evaluates the independent semantic reviewer. Gate C optionally
evaluates built-in AI authoring. Both preserve complete manual operation and are
skipped when disabled or unconfigured. They do not authorize image ingestion,
profile proliferation, MCP/plugin packaging, visual UI, hosting, or multi-agent
coordination.

Domain profiles may be proposed later in whatever order evidence justifies.
There is no automatic technical → catalog → narrative → interactive progression.

## 15. Durable direction

> Keep one current human-readable dependency graph in SQLite; make every proposed
> change expose and resolve its complete modeled affected set; let either a human
> or an optional AI perform the semantic work; and write the reviewed graph
> atomically without pretending to prove natural language or maintain history.

# ValidatedWorld Implementation Blueprint

**Status:** Coding-agent handoff

**Blueprint version:** 7.0

**Last reviewed:** 2026-08-13

**Target:** .NET 10 / C#

**Database schema:** SQLite v1, current state only

**Command/result protocol:** `validatedworld-cli/v1` JSON

## 1. Purpose and reading order

Read, in order:

1. [feasibility.md](feasibility.md)
2. [validated_world_authoring_spec.md](validated_world_authoring_spec.md)
3. [prior_art_and_positioning.md](prior_art_and_positioning.md)
4. This blueprint
5. [testing_and_qa.md](testing_and_qa.md)
6. [implementation_execution_plan.md](implementation_execution_plan.md)

The execution plan records completed evidence and one Current task. Implement
only that task. Human direction overrides this design.

Every completed coding task runs:

```powershell
dotnet restore ValidatedWorld.slnx
dotnet build ValidatedWorld.slnx --no-restore
dotnet test ValidatedWorld.slnx --no-build --no-restore
```

## 2. Non-negotiable invariants

1. One `.vw.db` is the sole complete project representation and interchange
   artifact.
2. The database stores current graph state, not historical versions, drafts,
   operations, review runs, or commits.
3. JSON is a structured command/result envelope only; there is no JSON project
   snapshot or snapshot import/export.
4. The fixed application-owned SQLite schema has exactly four Gate A tables:
   migration history, project metadata, nodes, and edges.
5. Canonical content is human-readable nodes and explicit labeled binary edges.
6. Node kind, tags, and scalar attributes are optional common data, not a
   mandatory ontology.
7. Every edge explicitly declares review propagation; label text alone never
   implies it.
8. Every project has one purpose root and every other node has one acyclic
   `scope-parent` path to it.
9. A changed leaf's ancestor lineage is context only. It never fans into sibling
   branches. A directly changed scope selects its descendants; a directly
   changed root selects the project.
10. Optional profiles add validators/helpers but a plain graph needs none.
11. Every canonical mutation occurs through one in-memory application change
    session and one short SQLite transaction.
12. The app never persists an unfinished session. Closing/crashing loses it.
13. The current-state fingerprint detects stale/incomplete mutation but is not a
    revision chain, commit ID, parent hash, or user-facing version history.
14. Removed or redirected edges remain part of affected-set calculation through
    the union of current and proposed graph arcs.
15. Impact means “must be considered,” not “must be edited.”
16. Every selected affected node must be updated, reviewed-no-change, or marked
    not-applicable before commit.
17. Dispositions are session-local and stale when proposal content/path changes.
18. Failed, busy, stale, or invalid commits change no canonical rows.
19. Deletes never cascade. Incident edges are repaired explicitly.
20. Gate A performs full projection and validation. Incremental validation is
    deferred.
21. SQLite is bundled in process through pinned NuGet dependencies. No server,
    external CLI, system SQLite, ORM, or Docker is required.
22. Direct verified SQL reads may use documented views. Direct writes are
    unsupported.
23. AI authoring and AI review are optional adapters. Missing configuration
    falls back to complete manual use.
24. AI results are heuristic proposals/concerns, never deterministic proof.
25. No provider call or human wait occurs inside a SQLite write transaction.
26. The authoring model has no SQL, direct write, automatic review disposition,
    or unguarded commit capability.
27. AI user approval binds to exact current and proposed fingerprints and is
    invalidated by any change.
28. Gate B review is one complete transaction request with disjoint chains kept
    together; there is no automatic paid retry.
29. Search/navigation are deterministic bounded graph queries, not embeddings,
    natural-language SQL, or RAG.
30. Images/OCR, MCP/plugin, web/GUI, hosting, multi-agent coordination, document
    generation, and domain profiles are outside the current roadmap unless a
    later human request changes it.

## 3. Solution architecture

```text
ValidatedWorld.Core
  simple immutable graph, values, operations, change/review contracts

ValidatedWorld.Serialization
  versioned strict command/result DTOs and deterministic internal encoding

ValidatedWorld.Validation
  indexes, scope checks, affected-set traversal, optional profile ports

ValidatedWorld.Application
  project/query/change/commit use cases and persistence ports

ValidatedWorld.Persistence.Sqlite
  four-table migrations, mapping, transactions, views, backup/export

ValidatedWorld.Cli
  text/JSON commands and composition root

ValidatedWorld.AiReview / .OpenAI        optional post-Gate-A
ValidatedWorld.AiAuthoring / .OpenAI     optional post-Gate-A
```

Core has no SQLite, JSON, provider, file, UI, or domain-profile dependency.
Serialization may provide the deterministic byte encoding used to compute a
current-state fingerprint, but it must not expose that encoding as a project
file or snapshot contract.

Application ports:

```csharp
public interface IProjectStore
{
    ValueTask<ProjectGraph> LoadAsync(CancellationToken cancellationToken);
    ValueTask<string> ComputeStoredStateFingerprintAsync(
        CancellationToken cancellationToken);
}

public interface IProjectWriteSession : IAsyncDisposable
{
    ValueTask<ProjectGraph> LoadCurrentAsync(CancellationToken cancellationToken);
    ValueTask ApplyCurrentStateAsync(
        ProjectGraph proposed,
        string newStateFingerprint,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken);
    ValueTask CommitAsync(CancellationToken cancellationToken);
    ValueTask RollbackAsync(CancellationToken cancellationToken);
}

public interface IProjectWriteSessionFactory
{
    ValueTask<IProjectWriteSession> BeginImmediateAsync(
        CancellationToken cancellationToken);
}
```

Ports expose domain objects, not SQL or `DbConnection`.

## 4. Common immutable domain

### 4.1 Identifiers

```csharp
public readonly record struct ProjectId(string Value);
public readonly record struct EntityId(string Value);
public readonly record struct ChangeSessionId(Guid Value);
public readonly record struct ProfileId(string Value);
```

Project/entity/profile IDs match:

```regex
^[a-z][a-z0-9-]*:[a-z][a-z0-9-]*(/[a-z][a-z0-9-]*)*$
```

IDs use ordinal comparison. Do not trim, case-fold, infer, or reuse removed IDs
inside one active session.

### 4.2 Scalar values

```csharp
public abstract record GraphValue
{
    public sealed record Text(string Value) : GraphValue;
    public sealed record Integer(long Value) : GraphValue;
    public sealed record Decimal(string CanonicalValue) : GraphValue;
    public sealed record Boolean(bool Value) : GraphValue;
    public sealed record Symbol(string Value) : GraphValue;
    public sealed record Instant(DateTimeOffset Value) : GraphValue;
}
```

Decimal uses canonical base-10 text; floating point is forbidden. Instants are
canonical UTC ISO 8601. Attribute values are scalar arrays stored with stable
ordering. An ID-looking string has no reference behavior.

### 4.3 Nodes and edges

```csharp
public sealed record GraphNode(
    EntityId Id,
    string Text,
    string? Kind,
    ImmutableArray<string> Tags,
    ImmutableSortedDictionary<string, ImmutableArray<GraphValue>> Attributes);

public enum ReviewDirection
{
    None,
    SourceToTarget,
    TargetToSource,
    Both
}

public sealed record GraphEdge(
    EntityId Id,
    EntityId SourceNodeId,
    EntityId TargetNodeId,
    string Relationship,
    ReviewDirection ReviewDirection,
    string? Rationale,
    ImmutableArray<string> Tags,
    ImmutableSortedDictionary<string, ImmutableArray<GraphValue>> Attributes);

public sealed record EnabledProfile(
    ProfileId Id,
    string Version,
    string CompatibilityFingerprint,
    ImmutableSortedDictionary<string, GraphValue> Settings);
```

Common validation requires non-empty text/relationship, valid IDs, sorted unique
tags, valid scalar encodings, and existing node endpoints. It does not require a
kind or profile.

`scope-parent` is the one reserved relationship. It always has child as source
and parent as target. Profile modules may reserve additional labels only for
projects that enable them.

### 4.4 Project graph

```csharp
public sealed record ProjectGraph(
    ProjectId ProjectId,
    string Title,
    EntityId PurposeNodeId,
    ImmutableArray<EnabledProfile> EnabledProfiles,
    ImmutableArray<GraphNode> Nodes,
    ImmutableArray<GraphEdge> Edges,
    string StateFingerprint);
```

Arrays sort by ID. `StateFingerprint` is verified/computed by Application and
Serialization; clients cannot choose it during mutation.

### 4.5 Operations and sessions

```csharp
public abstract record GraphOperation
{
    public required EntityId TargetId { get; init; }
    public sealed record AddNode(GraphNode Node) : GraphOperation;
    public sealed record ReplaceNode(GraphNode Node) : GraphOperation;
    public sealed record RemoveNode : GraphOperation;
    public sealed record AddEdge(GraphEdge Edge) : GraphOperation;
    public sealed record ReplaceEdge(GraphEdge Edge) : GraphOperation;
    public sealed record RemoveEdge : GraphOperation;
}

public enum ReviewDispositionKind
{
    Updated,
    ReviewedNoChange,
    NotApplicable,
    Pending
}

public sealed record ReviewDisposition(
    EntityId NodeId,
    ReviewDispositionKind Kind,
    string Reviewer,
    string? Rationale,
    string Fingerprint,
    DateTimeOffset ReviewedAt);

public sealed class ChangeSession
{
    // Application-owned mutable coordinator around immutable snapshots.
    // Contains base graph/fingerprint, final operation-by-ID map, focus,
    // projection, affected result, dispositions, and status. Never persisted.
}
```

Only one final operation per target ID exists. Replacing an operation
recalculates projection and invalidates affected dispositions.

## 5. Normative SQLite schema v1

### 5.1 Header and migrations

```sql
PRAGMA application_id = 1448561732; -- 0x56574C44, VWLD
PRAGMA user_version = 1;

CREATE TABLE schema_migrations (
    version             INTEGER PRIMARY KEY,
    migration_id        TEXT NOT NULL UNIQUE,
    script_sha256       TEXT NOT NULL,
    applied_at_utc      TEXT NOT NULL
) STRICT;
```

The application embeds migration SQL and verifies exact SHA-256 checksums.
Unknown new versions, missing migrations, or checksum mismatches block writes.

### 5.2 Project

```sql
CREATE TABLE projects (
    project_id              TEXT PRIMARY KEY,
    title                   TEXT NOT NULL CHECK(length(title) > 0),
    purpose_node_id         TEXT NOT NULL UNIQUE,
    state_fingerprint       TEXT NOT NULL,
    enabled_profiles_json   TEXT NOT NULL CHECK(json_valid(enabled_profiles_json)),
    created_at_utc          TEXT NOT NULL,
    updated_at_utc          TEXT NOT NULL,
    FOREIGN KEY(purpose_node_id) REFERENCES nodes(node_id)
        ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED
) STRICT;
```

Gate A permits one project row. Initialization inserts the project, purpose
node, and any selected profile metadata atomically. The purpose text must be
substantive.

### 5.3 Nodes

```sql
CREATE TABLE nodes (
    node_id              TEXT PRIMARY KEY,
    text                 TEXT NOT NULL CHECK(length(text) > 0),
    kind                 TEXT NULL,
    tags_json            TEXT NOT NULL CHECK(json_valid(tags_json)),
    attributes_json      TEXT NOT NULL CHECK(json_valid(attributes_json))
) STRICT;

CREATE INDEX ix_nodes_kind ON nodes(kind, node_id);
```

`tags_json` and `attributes_json` use a strict canonical internal encoding. The
JSON columns are an implementation detail inside SQLite, not a second project
format.

### 5.4 Edges

```sql
CREATE TABLE edges (
    edge_id               TEXT PRIMARY KEY,
    source_node_id        TEXT NOT NULL,
    target_node_id        TEXT NOT NULL,
    relationship          TEXT NOT NULL CHECK(length(relationship) > 0),
    review_direction      TEXT NOT NULL CHECK(review_direction IN
        ('none','source-to-target','target-to-source','both')),
    rationale             TEXT NULL,
    tags_json             TEXT NOT NULL CHECK(json_valid(tags_json)),
    attributes_json       TEXT NOT NULL CHECK(json_valid(attributes_json)),
    FOREIGN KEY(source_node_id) REFERENCES nodes(node_id) ON DELETE RESTRICT,
    FOREIGN KEY(target_node_id) REFERENCES nodes(node_id) ON DELETE RESTRICT
) STRICT;

CREATE INDEX ix_edges_source
    ON edges(source_node_id, relationship, target_node_id, edge_id);
CREATE INDEX ix_edges_target
    ON edges(target_node_id, relationship, source_node_id, edge_id);
CREATE UNIQUE INDEX ux_scope_parent_source
    ON edges(source_node_id)
    WHERE relationship = 'scope-parent';
```

Application validation proves that `scope-parent` uses a permitted direction,
has no self-loop, targets nodes, and forms the required spanning tree. Gate A
uses `none` for propagation on scope edges; scope context and direct-subtree
selection are explicit special algorithms, avoiding accidental sibling fan-out.

These four tables are the whole Gate A schema. Do not add package, type, draft,
validation, review, operation, commit, revision, or history tables without a new
human-approved architecture change.

### 5.5 Stable read views

Provide views:

```text
vw_project
vw_nodes
vw_edges
vw_scope
vw_review_arcs
```

`vw_review_arcs` expands each edge direction into rows:

```text
changed_node_id
selected_node_id
edge_id
relationship
```

It excludes `scope-parent`, whose special behavior is exposed through
`vw_scope`. C# and view expansion must match the same goldens.

## 6. Connection, integrity, backup, and export

Use `Microsoft.Data.Sqlite.Core` directly with an explicitly pinned/audited
SQLitePCLRaw bundle. Every connection:

1. resolves the caller's absolute `.vw.db` path;
2. opens read-only, read-write, or create only as requested;
3. sets and verifies `PRAGMA foreign_keys = ON`;
4. sets a bounded busy timeout;
5. refuses extension loading;
6. verifies application/user version, migrations, tables, indexes, and views;
7. runs `quick_check` on open and `integrity_check` on explicit full verify;
8. maps and validates every row; and
9. recomputes the state fingerprint before writes.

All SQL values are parameters. Never execute project text as SQL. Treat database
files as untrusted and cap page/file size, entity count, text/JSON lengths,
attributes, traversal nodes/depth, result bytes, and diagnostics.

Use rollback journal by default for a one-file-at-rest artifact. Use `BEGIN
IMMEDIATE` only for final commit. Never hold a write transaction across review.

Backup uses SQLite's online backup API to a new destination. `export-sql` may be
added in WP8; it must emit deterministic application-owned DDL/data ordering,
quote every value safely, contain no secrets, and round-trip only by creating a
new verified database through an application import command. If deterministic
SQL import/export proves unnecessary or risky, retain backup as the required
interchange mechanism and document SQL export as deferred.

## 7. Current-state fingerprint

The deterministic internal fingerprint input contains:

```text
project ID, title, purpose ID
enabled profiles ordered by ID/version/settings
nodes ordered by ID with text/kind/tags/attributes
edges ordered by ID with endpoints/relationship/direction/rationale/tags/attributes
```

It excludes timestamps and the stored fingerprint itself.

```text
StateFingerprint = "sha256:" + lowercase_hex(
    SHA-256(deterministic UTF-8 internal encoding))

OperationSetFingerprint = "sha256:" + lowercase_hex(
    SHA-256(base fingerprint + ordered final operations))

ProposedStateFingerprint = fingerprint(ProjectProjection)
```

The encoding may reuse strict JSON-writing utilities privately, but no command
exports it as a supported snapshot. Tests must prove insertion-order
independence and that differing SQLite bytes can represent the same fingerprint.

The one stored `projects.state_fingerprint` must match recomputation. It is
overwritten on successful commit. There are no prior/parent fingerprint rows.

## 8. Graph indexes and structural validation

Build an immutable `GraphIndex` containing dictionaries by ID, edges by source
and target, scope parent/children, and review arcs.

Validate:

1. Project/entity ID syntax and global uniqueness.
2. Non-empty project title, node text, and relationship labels.
3. Canonical scalar/tag/attribute representations.
4. Every edge endpoint exists as a node.
5. Exactly one purpose node is referenced and present.
6. Purpose has no scope parent.
7. Every non-purpose node has exactly one `scope-parent` edge.
8. Scope parents are acyclic and every path reaches purpose.
9. Every scope edge uses `ReviewDirection.None` in Gate A.
10. Every enabled profile exists/accepts its stored version or coverage is
    inconclusive.
11. Every enabled-profile validator completes or returns explicit inconclusive.

Common graph validity does not reject unknown kinds, labels, tags, or attributes.

## 9. Projection and affected-set algorithms

### 9.1 Apply operations

```text
Project(base, operations):
    require current fingerprint equals session base fingerprint
    copy nodes/edges to ID-keyed builders

    for operation sorted by target ID:
        AddNode:     require ID absent from both spaces; add complete node
        ReplaceNode: require node exists; replace complete node preserving ID
        RemoveNode:  require node exists; remove only after incident edge ops
        AddEdge:     require ID absent from both spaces; add complete edge
        ReplaceEdge: require edge exists; replace complete edge preserving ID
        RemoveEdge:  require edge exists; remove edge

    materialize immutable sorted graph
    validate complete graph; never cascade
```

An implementation may order edge removals before node removals internally while
preserving deterministic error reporting by original operation ID.

### 9.2 Review arcs

For non-scope edges:

```text
none:              emit no arcs
source-to-target:  emit source -> target
target-to-source:  emit target -> source
both:              emit source -> target and target -> source
```

Keep edge evidence even when multiple edges produce the same node pair.

### 9.3 Seeds

Direct node-operation targets seed their projected/base node ID where present.
Changed edge operations are recorded and seed endpoint propagation using both
the base edge/direction and proposed edge/direction. Removed relationships
therefore retain old consequences; added relationships include new consequences.

If a directly changed node is a scope node (has children in base or projection),
include its base-plus-projected descendant subtree as affected. This special
containment selection is applied only to directly changed node IDs—not ancestors
later added as context.

### 9.4 Breadth-first affected traversal

```text
ComputeAffected(operations, base, proposed, limits):
    changedEntityIds = operation target IDs
    seeds = direct changed nodes + changed-edge propagated endpoint seeds
    arcs = union(review arcs from base, review arcs from proposed)
    affected = seeds + descendants of directly changed scope-node seeds
    queue = affected node IDs ordered ordinally at depth zero
    predecessor = empty

    while queue not empty:
        current, depth = dequeue
        if depth reaches limit: return Inconclusive with evidence
        for arc current -> selected ordered by selected ID then edge ID:
            retain arc evidence
            if selected newly added:
                predecessor[selected] = selected shortest deterministic hop
                enqueue(selected, depth + 1)
        if count exceeds limit: return Inconclusive with evidence

    contextAncestors = singular scope ancestors of every changed/affected node
                       minus affected; never enqueue them
    return Complete(changed, affected, paths, contextAncestors, statistics)
```

Use current and proposed scope trees when an operation changes scope membership.
Report both explanations where relevant. A root operation selects all nodes.

### 9.5 Disposition fingerprints

```text
DispositionFingerprint = SHA-256(
    OperationSetFingerprint
    + node ID
    + proposed node encoding or removed marker
    + ordered selected explanation path evidence)
```

Direct changed nodes receive `updated`. Every other affected node starts
`pending`. A human or authorized reviewer may set `reviewed-no-change` or
`not-applicable` with identity/rationale. Recompute and discard stale
dispositions after every operation change.

Context-only ancestors are displayed but do not require a disposition unless
the user changes them or an edge selects them as affected.

## 10. Commit orchestration

```text
Commit(changeSession, optionalApprovalAuthorization):
    require session belongs to open database
    load and verify current graph/fingerprint
    require current fingerprint equals session base fingerprint
    projection = Project(current, final operations)
    affected = ComputeAffected(current, projection)
    report = ValidateAll(projection, affected, dispositions, enabled profiles)
    require report Valid and no pending/stale disposition

    if session was AI-authored:
        require opaque authorization matches database identity,
                base fingerprint, operation fingerprint,
                proposed fingerprint, affected-set fingerprint,
                review state, and short expiry

    begin SQLite IMMEDIATE transaction
    reload current rows and recompute fingerprint
    reject/rollback if base differs
    apply edge removals, node additions/replacements,
          node removals, and edge additions/replacements in FK-safe order
    foreign_key_check and remap/validate complete current graph
    compute new fingerprint and update project row/timestamp
    verify stored/recomputed fingerprint
    commit exactly once
    mark in-memory session committed and return structured result
```

Fault injection covers every write boundary. An unexpected commit error leaves
the prior database untouched and preserves the in-memory proposal where safe so
the caller can inspect or retry. A stale base requires a new/rebased session;
Gate A does not implement automatic merge.

## 11. Application and CLI contracts

### 11.1 Use cases

```text
InitializeProject
OpenAndVerifyProject
GetProjectStatus
BackupProject
ExportProjectSql                 optional WP8 evidence decision
ImportProjectSqlToNewDatabase   only if export retained

GetNode / ListNodes / SearchNodes
GetEdge / ListEdges
GetScopeChildren / GetScopeAncestors / GetScopeSubtree
GetNeighbors / GetDependencies / GetDependents / ExplainPath / BuildContext

BeginChange
GetChange
SetChangeFocus
ExpandAuthoringBatch
ApplyOperations
AnalyzeAffectedSet
SetReviewDisposition
ValidateChange
CommitChange
DiscardChange
```

### 11.2 CLI

```text
vw init --db <path> --project-id <id> --title <text> --purpose <text>
vw verify --db <path> [--full]
vw status --db <path>
vw backup --db <path> --output <new-path>
vw export-sql --db <path> --output <path>          [if retained]
vw import-sql --input <path> --db <new-path>       [if retained]
vw sample list
vw sample create --sample <name> --variant <name> --db <new-path>

vw node get/list/search ...
vw edge get/list ...
vw scope children/ancestors/subtree ...
vw graph neighbors/dependencies/dependents/path/context ...

vw change begin --db <path> --intent <text> --author <text>
vw change show --session <id>
vw change focus --session <id> --node <id>
vw change expand --session <id> --batch <file-or-stdin>
vw change apply --session <id> --operations <file-or-stdin>
vw change affected --session <id>
vw change review --session <id> --node <id>
                 --status <reviewed-no-change|not-applicable>
                 --reviewer <id> --reason <text>
vw change validate --session <id>
vw change commit --session <id>
vw change discard --session <id>
```

An in-memory session ID is meaningful only to the running CLI host. If the CLI
remains one-process-per-command, WP5 must provide an explicit interactive/host
mode that keeps the process alive across change commands. It must not fake
durability by serializing sessions into the project database. A newline-delimited
JSON command host over stdin/stdout is the preferred initial implementation.

Every command consumes a strict request and emits one
`validatedworld-cli/v1` result to stdout. Logs go to stderr. Results contain
current state fingerprint and, when applicable, session/operation/proposed/
affected-set fingerprints. There is no complete project payload command.

### 11.3 Result statuses and exits

Statuses include `ok`, `review-pending`, `valid`, `invalid`, `inconclusive`,
`stale`, `committed`, and `discarded`.

```text
0 success/valid/committed
2 deterministic invalid
3 command/input error
4 stale state or writer conflict
5 database/migration/integrity/write failure
6 required analysis inconclusive
7 review dispositions pending
8 unavailable optional profile/provider capability
9 internal failure
```

## 12. Search and context

Search supports exact ID, invariant case-normalized text, kind, tag,
relationship, and scope-subtree filters. Results have deterministic ID ordering,
bounds, and cursors tied to the current state fingerprint.

Navigation supports scope children/ancestors/subtree, incoming/outgoing
neighbors, propagation dependencies/dependents, explanation paths, and bounded
context. Context priority is:

1. seeds;
2. directly relevant edges/endpoints;
3. singular scope lineages;
4. forward explanatory relationships;
5. affected dependents and paths;
6. artifact anchors;
7. additional nodes by increasing distance.

Report every omission/truncation. Never silently drop a required seed or edge.
No provider call or embedding is part of these queries.

## 13. Optional profile interface

```csharp
public interface IGraphProfile
{
    ProfileId Id { get; }
    string Version { get; }
    string CompatibilityFingerprint { get; }

    ProfileValidationResult Validate(
        ProjectGraph graph,
        GraphIndex index,
        CancellationToken cancellationToken);

    BatchExpansion? TryExpandHelper(ProfileHelperRequest request);
}
```

Profiles are registered application code and cannot execute SQL or mutate the
graph during validation. A project database never contains executable profile
definitions. Plain projects enable none. Profile helper expansions return
ordinary explicit operations for preview.

## 14. Optional AI phases

### 14.1 Gate B semantic reviewer

Implement only after successful Gate A evidence and a new human request. Use the
normative [AI review design](ai_semantic_review.md). The reviewer receives one
complete immutable proposal/affected-context request, has no tools, and returns
strict cited concerns.

Runtime review is disabled by flag or unavailable without `OPENAI_API_KEY`. In
either case manual review remains valid. There is no `Required` provider policy
in the initial product.

Normal tests use fakes/scripted HTTP. The production path is OpenAI only, with
the reviewed model at implementation time, background mode, 1,200-second
end-to-end deadline, displayed authorization, and zero automatic paid retries.

### 14.2 Gate C authoring agent

Implement only after successful Gate A evidence, a Gate B retain/omit decision,
and a new human request. Use the normative
[AI authoring design](ai_authoring_agent.md).

The model operates strict Application tools for project status, search,
navigation, one in-memory change session, operations, affected analysis,
validation, questions, review handoff, confirmation, and guarded commit. It
accepts user descriptions and explicit text, not images in the initial scope.

If authoring is disabled or the key is absent, users operate the same host
manually. No MCP/plugin/UI or multi-agent adapter is currently planned.

## 15. Scenario and performance corpus

`samples/TechnicalProject` stores reviewed human-readable source data and command
recipes, not a populated database. The app generates disposable `.vw.db` files.

The baseline graph models an offline privacy-preserving sensor:

```text
purpose
  scope-parent children: power, privacy, documentation, accessibility

power:
  24-hour runtime requirement
  20 mA current assumption
  500 mAh capacity assumption
  25-hour runtime result
  battery-sufficiency conclusion
  relevant design/verification anchors

privacy:
  no-upload requirement
  raw-observation definition
  seven-day retention assumption
  encrypted-ring-buffer decision
  privacy claim, evidence, implementation, verification, anchors
```

All are ordinary text nodes with optional kinds. Explicit relationship edges
select consequences. Tests change current, retention, and diagnostic-upload
policy; remove/redirect edges; introduce contradictions as text; repair selected
nodes; and assert unrelated exclusions. The common engine does not calculate
battery arithmetic or understand the contradiction—it selects the right review
set and requires a thinking participant to resolve it.

Synthetic fixtures:

```text
small       1,000 nodes / 10,000 review arcs
expected   10,000 nodes / 100,000 review arcs
stress    100,000 nodes / 1,000,000 review arcs
```

Measure open/map, fingerprint, validation, search, affected traversal,
projection, and commit. Record hardware; do not claim universal performance.

## 16. Test strategy

### 16.1 Layers

```text
Core unit/property tests
  IDs, values, nodes, edges, directions, operations, session contracts

Serialization tests
  strict command/result DTOs, deterministic private fingerprint encoding

Validation tests
  index, scope, review arcs, base/proposed union, affected paths, dispositions

Application tests
  in-memory session lifetime, projection, review invalidation, commit orchestration

SQLite integration/fault tests
  four-table migration, foreign keys, mapping, fingerprint, rollback, backup/views

CLI/host tests
  JSON commands, process lifetime, manual full workflow, stdout/stderr/exits

Scenario/performance tests
  generated TechnicalProject, variants, bounds, realistic QA
```

Required properties include:

- physical insertion order does not change current-state fingerprint;
- no historical/draft rows exist after initialization or commit;
- process restart discards unresolved sessions while canonical state is intact;
- every non-root scope path is singular, acyclic, and root-reaching;
- C# and `vw_review_arcs` expansion agree;
- current/proposed union preserves removed/new relationship consequences;
- ancestor context never selects siblings;
- direct scope/root changes select descendants;
- unrelated insertions do not change existing affected paths;
- proposal changes invalidate stale dispositions and AI approval;
- failed/faulted commits preserve every prior current row/fingerprint;
- backup opens to the same graph/fingerprint;
- no complete JSON project snapshot can be imported/exported;
- all workflows run without provider, SQL knowledge, external SQLite, Docker, or
  GUI; and
- optional provider absence selects manual fallback without graph failure.

From WP3 onward, each package includes a scripted realistic end-to-end case and
an actual AI-agent black-box walkthrough through public commands. The QA agent
acts as a user, not as the built-in provider feature. It records task completion,
affected accuracy, unrelated exclusions, confusion, burden, and product concerns.
Deterministic defects become regression tests.

## 17. Ordered work packages

### WP0 — architecture scaffold

Complete. Existing projects compile. No product code is implemented.

### WP1 — simple graph domain

- Replace planned schema-package/type/revision domain with Section 4.
- Implement IDs, values, plain nodes, labeled edges, review direction, enabled-
  profile reference, project graph, operations, batch/focus expansion contracts,
  dispositions, and in-memory-session contracts.
- Construct the realistic TechnicalProject graph through public Core APIs without
  an enabled profile.
- Acceptance: exhaustive valid/rejected unit/property cases; one purpose/scope
  graph and cross-links are expressible without escape hatches; report modeling
  friction; full solution passes.

### WP2 — command/result serialization and internal fingerprints

- Implement strict versioned CLI DTOs and deterministic private encoding/hashes.
- Do not implement a JSON project snapshot or snapshot import/export.
- Add reviewed scenario source/operation/result fixtures.
- Acceptance: strict DTO, ordering, duplicate/unknown-field, hash, and no-public-
  snapshot tests pass.

### WP3 — four-table SQLite and read walking skeleton

- Implement migration, connections, mapping, fingerprint verification,
  initialize/status/verify, read views, backup, bundled runtime, sample
  generation, CLI tests, TestKit, and EndToEnd projects.
- Generate disposable TechnicalProject databases through app paths.
- Acceptance: integration/fault/package/read-only QA passes without external
  SQLite/Docker; first black-box agent walkthrough succeeds.

### WP4 — indexes and structural validation

- Implement graph indexes, review arcs, purpose/scope validation, diagnostics,
  profile port/coverage, and dependency/path reads.
- Acceptance: corrupted/invalid scenarios yield exact structured diagnostics;
  plain no-profile graph validates; read views match C#.

### WP5 — in-memory change sessions and projection

- Implement process-local session manager, operations, focus/batch expansion,
  projection, fingerprints, validation, discard, and long-lived JSON command
  host.
- Acceptance: sessions never persist, restart/discard behavior is explicit,
  operation conflicts are deterministic, and a user/agent can propose a realistic
  change without SQL.

### WP6 — affected set and manual review

- Implement current/proposed union traversal, scope special rules, explanation
  paths, bounds, obligations, and disposition fingerprints.
- Acceptance: TechnicalProject changes select exact required nodes and exclude
  distractors; old/new edge effects, scope context, root changes, and stale
  reviews are proven; manual workflow is usable.

### WP7 — atomic current-state commit

- Implement short write session, FK-safe mutations, base recheck, fingerprint
  update, fault rollback, and retryable structured errors.
- Do not persist operation/review/commit history.
- Acceptance: every injected fault preserves prior state; accepted commit matches
  expected current graph; stale/busy recovery is understandable.

### WP8 — complete queries, interoperability, and host polish

- Finish search/navigation/context, limits, backup/help, documented read views,
  and interactive host behavior.
- Evaluate deterministic SQL export/import; retain only if safe/useful and fully
  round-tripped. Backup remains required.
- Acceptance: complete manual init-to-commit workflow from public help; large-
  graph queries bounded; black-box QA succeeds.

### WP9 — Gate A evaluation

- Run correctness, affected precision, modeling/review burden, Doorstop/plain-
  SQLite comparison, lower-cost-agent usability, and performance evidence.
- Acceptance: explicitly continue, narrow, pivot, or stop. Set Current task to
  None and ask the human for direction.

### WP10 — optional AI semantic reviewer

- Separate later plan only after Gate A and explicit human request.
- Implement disabled/manual fallback, one complete request, concerns, fake tests,
  sole OpenAI client, safety/secret rules, and live evidence.

### WP11 — optional AI authoring

- Separate later plan only after Gate A, Gate B decision, and explicit request.
- Implement text-only intent, strict bounded tools, one in-memory session,
  questions, exact approval, guarded commit, fake tests, and sole OpenAI client.

No further work package is currently planned. Profiles, images/OCR, MCP/plugin,
GUI/web, hosting, multi-agent coordination, document generation, narrative
analysis, and interactive-state analysis require separate product decisions.

## 18. Definition of implementation done

Each change states its work package, behavior, tests, scenario/QA evidence,
remaining uncertainty, and exact next assignment. It updates the execution plan,
runs the full restore/build/test sequence, reports to the human, and stops.

Do not manage Git. Do not silently preserve obsolete type-package/history/schema
requirements from blueprint v6. If implementation evidence contradicts this
blueprint, update the controlling documents and report the product implication
instead of building an undocumented compromise.

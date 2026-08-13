# ValidatedWorld Implementation Blueprint

**Status:** Coding-agent handoff

**Blueprint version:** 6.0

**Last reviewed:** 2026-08-13

**Target:** .NET 10 / C#

**Database schema:** SQLite v1

**Logical protocol:** `validatedworld/v1`

## 1. Purpose and reading order

This document specifies the embedded relational semantic change-control engine
and the order in which coding agents build it.

Read first:

1. [feasibility.md](feasibility.md)
2. [validated_world_authoring_spec.md](validated_world_authoring_spec.md)
3. [prior_art_and_positioning.md](prior_art_and_positioning.md)
4. This blueprint
5. [testing_and_qa.md](testing_and_qa.md)

For Gate B planning/implementation also read
[ai_semantic_review.md](ai_semantic_review.md). For Gate C or MCP/plugin work also
read [ai_authoring_agent.md](ai_authoring_agent.md). These files define later
gates and do not authorize pulling them into Gate A.

After reading this blueprint, read
[implementation_execution_plan.md](implementation_execution_plan.md). The
blueprint is the normative design and ordered backlog. The execution plan is the
authoritative record of completed evidence and the only Current task. Agents
update it in the same local task that succeeds; chat history is not project
state. If the task fails, it remains Current task and the agent reports why.

A human invokes each agent run. One run completes one task or reports failure,
then stops. It does not start the recorded next task. Agents edit and test the supplied
working tree but do not manage Git branches, staging, commits, merges, remotes,
or pull requests. Product-language `commit` operations below are ValidatedWorld
database transactions, not Git operations.

Pseudocode and SQL are normative about observable behavior. If implementation
evidence invalidates a contract, update the controlling documents in the same
change.

Every work package ends with:

```powershell
dotnet restore ValidatedWorld.slnx
dotnet build ValidatedWorld.slnx --no-restore
dotnet test ValidatedWorld.slnx --no-build --no-restore
```

## 2. Non-negotiable invariants

1. `project.vw.db` is the only authoritative workspace artifact.
2. JSON is the versioned protocol and logical snapshot representation, not the
   physical source of truth.
3. Logical project identity is a canonical JSON hash, never a hash of SQLite
   file bytes.
4. The SQLite schema is fixed and application-owned; project clients cannot add
   canonical tables, triggers, or views.
5. Domain vocabulary is profile-driven through exact logical packages, not
   arbitrary physical DDL.
6. Core has no SQLite, JSON, file, network, provider, UI, or game dependency.
7. Every canonical mutation occurs through an application transaction.
8. Durable drafts are not authoritative state and hold no long-lived SQLite
   write transaction.
9. A rejected or stale commit rolls back every relational and audit write.
10. Stable entity IDs, not labels, paths, names, or row IDs, are semantic
    references.
11. SQLite foreign keys establish entity/type/endpoint integrity only. Edge-type
    impact mode establishes semantic dependency direction.
12. Every graph-relevant connection is a canonical typed edge. Scalar
    properties and extension JSON are never scanned for IDs.
13. The operational dependency multigraph is a deterministic directional view
    of canonical edges; no second adjacency graph is authored or inferred.
14. Impact uses the union of base and projected dependency graphs.
15. Accepted operations are the direct change record; no separate semantic diff
    is canonical.
16. Impact means “must be considered,” not “must be edited.”
17. Policy may require current dispositions for selected impacted nodes.
18. Deterministic results are Proven, Disproven, or Inconclusive.
19. Remove never cascades implicitly. Physical foreign keys use Restrict/No
    Action, and application repair is explicit.
20. Gate A runs full logical validation; incremental semantic validation is
    deferred.
21. Gate A supports one writer at a time, matching SQLite's model.
22. No importer, renderer, RAG pipeline, AI provider, web host, or graph database
    is part of Gate A.
23. Gate B AI results are Concerns, never Proven or Disproven findings.
24. A required AI review may block commit when missing, stale, incomplete, or
    undispositioned; provider failure is review-inconclusive, not invalid canon.
25. No AI review response, candidate operation, link, or disposition mutates
    canon automatically.
26. No provider call occurs while a SQLite write transaction is open, and no
    provider is contacted implicitly by normal verify/impact/commit commands.
27. Provider credentials never enter project data, logical JSON, CLI request
    bodies/arguments, logs, diagnostics, cache keys, or test fixtures.
28. The default build/test suite uses fake/scripted providers and requires no
    secret or live network.
29. Every project has exactly one purpose root. Every other canonical node has
    exactly one `scope-parent`; repeated parent traversal is acyclic and
    terminates at the root.
30. `scope-parent` means child depends on parent, but only direct transaction
    operation targets seed impact. Context ancestors never become seeds or fan
    down into siblings. A direct root change, and only that case, deliberately
    selects the whole project through scope edges.
31. Gate B sends one whole-transaction request containing every disjoint selected
    chain and singular purpose lineage. It has no sharding, synthesis, fallback
    model, or automatic retry.
32. Gate B has one production client/configuration: OpenAI `gpt-5.6-terra` with
    medium reasoning. Interfaces exist for isolation and offline fakes, not a
    provider ecosystem.
33. AI-review implementation requires an exact human secret-readiness
    attestation; a paid call requires a second exact per-run authorization.
34. Canonical content has one public shape: typed nodes and typed binary edges.
    Constraints, anchors, and reified higher-arity relationships are nodes.
35. Schema/package metadata and the transaction/validation/commit ledger are
    control-plane tables outside the canonical content graph.
36. Gate A v1 uses nine tables including migration history. Scalar property maps
    and ledger payloads are canonical JSON; add normalized/materialized property
    indexes only after measured need.
37. Authoring focus, cluster, and template expansion are noncanonical input
    conveniences. They must expand to explicit node/edge operations before
    hashing, validation, or commit.
38. The mature authoring surface is AI-first and headless. CLI, in-app function
    tools, and later MCP expose the same Application semantics and versioned
    schemas; no adapter writes SQL or reimplements graph rules.
39. An AI authoring agent may mutate only a durable draft through bounded tools.
    It has no direct canonical-write, schema-mutation, rule-suppression, or
    unguarded commit capability.
40. Final AI-authored commit confirmation is a user action bound to the exact
    project head, draft revision, change-set hash, projected hash, and satisfied
    review state. The user may approve in ordinary conversation; the agent then
    calls the guarded commit tool. Model text alone is not confirmation.
41. The authoring agent and Gate B reviewer are independent. The reviewer is
    tool-free and receives no authoring conversation; authoring repairs stale its
    review.
42. A project may be far larger than any model context. The agent works safely by
    repeatedly searching/retrieving relevant bounded working sets while
    transactions, impact traversal, and deterministic validation operate over
    the full explicitly modeled graph. Unmodeled qualitative completeness remains
    a separate heuristic evidence question.

## 3. Solution architecture

### 3.1 Projects and dependencies

```text
ValidatedWorld.Core
  dependencies: none

ValidatedWorld.Serialization
  dependencies: Core

ValidatedWorld.Validation
  dependencies: Core

ValidatedWorld.AiReview (post-Gate-A Gate B)
  dependencies: Core, Serialization, Validation

ValidatedWorld.Application
  Gate A dependencies: Core, Serialization, Validation
  Gate B adds: AiReview

ValidatedWorld.Persistence.Sqlite
  dependencies: Core, Serialization, Application, Microsoft.Data.Sqlite.Core,
                explicitly pinned SQLitePCLRaw native bundle

ValidatedWorld.Cli
  Gate A dependencies: Application, Persistence.Sqlite
  Gate B composition adds: AiReview.OpenAI

ValidatedWorld.AiReview.OpenAI (post-Gate-A sole production client)
  dependencies: AiReview, pinned official OpenAI client

ValidatedWorld.AiAuthoring (post-Gate-A Gate C)
  dependencies: Application plus versioned tool contracts

ValidatedWorld.AiAuthoring.OpenAI (post-Gate-A sole production client)
  dependencies: AiAuthoring, pinned official OpenAI client

ValidatedWorld.Mcp (post-gate)
  dependencies: Application plus selected persistence composition

ValidatedWorld.Web (only after hosted gate)
  dependencies: Application plus selected persistence composition
```

Application defines persistence ports. SQLite implements them. The composition
root is the only layer that chooses SQLite.

### 3.2 Namespace responsibilities

```text
ValidatedWorld.Core.Identifiers
ValidatedWorld.Core.Schema
ValidatedWorld.Core.Values
ValidatedWorld.Core.Graph
ValidatedWorld.Core.Constraints
ValidatedWorld.Core.Transactions
ValidatedWorld.Core.Reviews

ValidatedWorld.Serialization.Json
ValidatedWorld.Serialization.Canonical
ValidatedWorld.Serialization.Hashing

ValidatedWorld.Validation.Diagnostics
ValidatedWorld.Validation.Schema
ValidatedWorld.Validation.Indexes
ValidatedWorld.Validation.Edges
ValidatedWorld.Validation.Impact
ValidatedWorld.Validation.Rules
ValidatedWorld.Validation.Context

ValidatedWorld.AiReview.Planning
ValidatedWorld.AiReview.Requests
ValidatedWorld.AiReview.Contracts
ValidatedWorld.AiReview.Concerns

ValidatedWorld.AiAuthoring.Sessions
ValidatedWorld.AiAuthoring.Tools
ValidatedWorld.AiAuthoring.Confirmation
ValidatedWorld.AiAuthoring.Intake

ValidatedWorld.Application.Projects
ValidatedWorld.Application.Transactions
ValidatedWorld.Application.Commits
ValidatedWorld.Application.Queries
ValidatedWorld.Application.Persistence
ValidatedWorld.Application.AiReview

ValidatedWorld.Persistence.Sqlite.Connections
ValidatedWorld.Persistence.Sqlite.Migrations
ValidatedWorld.Persistence.Sqlite.Mapping
ValidatedWorld.Persistence.Sqlite.Repositories
ValidatedWorld.Persistence.Sqlite.Views
ValidatedWorld.Persistence.Sqlite.AiReview

ValidatedWorld.AiReview.OpenAI.Responses
ValidatedWorld.AiReview.OpenAI.Configuration

ValidatedWorld.AiAuthoring.OpenAI.Responses
ValidatedWorld.AiAuthoring.OpenAI.Configuration
```

### 3.3 Injected services

Application injects:

```csharp
public interface IClock { DateTimeOffset UtcNow { get; } }
public interface ITransactionIdGenerator { TransactionId Create(); }
public interface IProjectRepository { /* snapshot/head operations */ }
public interface IDraftTransactionRepository { /* durable draft operations */ }
public interface ICommitRepository { /* audit/read operations */ }
public interface IProjectWriteSession : IAsyncDisposable
{
    ValueTask<ProjectHead> ReadHeadAsync(CancellationToken cancellationToken);
    ValueTask ApplyAcceptedCommitAsync(
        AcceptedCommit commit,
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

Port methods use logical domain types, not `DbConnection`, SQL strings, or rows.
Tests use deterministic clocks/IDs and in-memory/fault-injecting ports.

## 4. Common immutable domain

Use sealed immutable records and immutable collections at public boundaries.
Validation operates on a complete `ProjectSnapshot`; Persistence maps it to and
from normalized rows.

### 4.1 IDs

```csharp
public readonly record struct ProjectId(string Value);
public readonly record struct EntityId(string Value);
public readonly record struct TypeId(string Value);
public readonly record struct PackageId(string Value);
public readonly record struct TransactionId(string Value);
public readonly record struct CommitId(string Value);
```

Project/entity/type/package IDs match:

```regex
^[a-z][a-z0-9-]*:[a-z][a-z0-9-]*(/[a-z][a-z0-9-]*)*$
```

Transaction IDs are `tx:` plus lower-case UUIDv7. Commit ID equals the accepted
transaction ID in Gate A. Compare all identifiers with ordinal semantics. Reject
rather than trim, case-fold, or normalize.

### 4.2 Schema package

```csharp
public sealed record SchemaPackage(
    PackageId Id,
    string Version,
    string DefinitionHash,
    ImmutableArray<NodeTypeDefinition> NodeTypes,
    ImmutableArray<EdgeTypeDefinition> EdgeTypes,
    ImmutableArray<RequiredValidator> RequiredValidators);

public sealed record NodeTypeDefinition(
    TypeId Id,
    PackageId PackageId,
    string PackageVersion,
    string DisplayName,
    ImmutableArray<PropertyDefinition> Properties,
    ImmutableArray<string> Categories);

public enum EdgeImpactMode
{
    None,
    SourceDependsOnTarget,
    TargetDependsOnSource,
    Bidirectional
}

public sealed record EdgeTypeDefinition(
    TypeId Id,
    PackageId PackageId,
    string PackageVersion,
    string DisplayName,
    ImmutableArray<PropertyDefinition> Properties,
    ImmutableArray<TypeId> AllowedSourceTypes,
    ImmutableArray<TypeId> AllowedTargetTypes,
    EdgeImpactMode ImpactMode,
    bool MustBeAcyclic,
    ImmutableArray<string> Categories);
```

Purpose, scope/group, constraint, artifact, and anchor concepts are node types or
categories. Constraint validators understand the node's properties and explicit
target edges. A relationship requiring more than two roles is reified as a node
with ordinary edges. Type inheritance is deferred; Gate A packages declare
complete concrete definitions.

### 4.3 Scalar properties and values

```csharp
public enum PropertyValueKind
{
    Text,
    Integer,
    Decimal,
    Boolean,
    Symbol,
    Instant
}

public sealed record PropertyDefinition(
    string Name,
    int Ordinal,
    PropertyValueKind ValueKind,
    int MinimumCount,
    int? MaximumCount,
    bool IsOrderSignificant,
    PropertyValueConstraints Constraints);

public abstract record ProjectValue
{
    public sealed record Text(string Value) : ProjectValue;
    public sealed record Integer(long Value) : ProjectValue;
    public sealed record Decimal(string CanonicalValue) : ProjectValue;
    public sealed record Boolean(bool Value) : ProjectValue;
    public sealed record Symbol(string Value) : ProjectValue;
    public sealed record Instant(DateTimeOffset Value) : ProjectValue;
}
```

Decimal values are canonical base-10 strings; floating point is forbidden for
semantic values. Instants canonicalize to UTC ISO 8601 with seven fractional
digits. Repeated unordered property values sort by canonical value; ordered values
retain explicit ordinal.

Properties contain values only. An ID-shaped string has no graph meaning. Every
semantic reference or association is an explicit `ProjectEdge`.

### 4.4 Graph entities

```csharp
public abstract record ProjectEntity(
    EntityId Id,
    int Revision,
    TypeId TypeId,
    ImmutableSortedDictionary<string, ImmutableArray<ProjectValue>> Properties,
    ImmutableArray<string> Tags,
    ExtensionMap Extensions);

public sealed record ProjectNode(
    EntityId Id,
    int Revision,
    TypeId TypeId,
    ImmutableSortedDictionary<string, ImmutableArray<ProjectValue>> Properties,
    ImmutableArray<string> Tags,
    ExtensionMap Extensions)
    : ProjectEntity(Id, Revision, TypeId, Properties, Tags, Extensions);

public sealed record ProjectEdge(
    EntityId Id,
    int Revision,
    TypeId TypeId,
    EntityId SourceNodeId,
    EntityId TargetNodeId,
    ImmutableSortedDictionary<string, ImmutableArray<ProjectValue>> Properties,
    ImmutableArray<string> Tags,
    ExtensionMap Extensions)
    : ProjectEntity(Id, Revision, TypeId, Properties, Tags, Extensions);

public sealed record ExtensionMap(
    ImmutableSortedDictionary<string, CanonicalJsonValue> Values);
```

Nodes and edges share one ID/revision space. Edges are binary, stable, typed, and
property-bearing. Extension keys are namespace-qualified. Extension values are
round-tripped but uncovered; reference-looking strings inside them have no graph
meaning.

### 4.6 Project snapshot and head

```csharp
public sealed record ProjectHead(
    ProjectId ProjectId,
    long Revision,
    string ParentLogicalHash,
    string LogicalHash,
    CommitId LastCommitId);

public sealed record ProjectSnapshot(
    string ProtocolVersion,
    ProjectHead Head,
    string Title,
    EntityId PurposeNodeId,
    ProjectPolicy Policy,
    ImmutableArray<SchemaPackage> SchemaPackages,
    ImmutableArray<ProjectNode> Nodes,
    ImmutableArray<ProjectEdge> Edges);

public enum AiReviewMode { Disabled, Optional, Required }

public sealed record ProjectPolicy(
    int MaximumDependencyDepth,
    int MaximumImpactNodes,
    ImmutableArray<TypeId> TypesRequiringImpactDisposition,
    AiReviewMode AiReviewMode);
```

Revision 0 initialization uses a documented null parent/last-commit encoding.
Node and edge arrays sort by ID. Package/type/property arrays have the canonical
ordering defined in Section 7.

Gate A accepts only `AiReviewMode.Disabled` until Gate B exists. Gate B permits
Optional or Required. Optional requires either a fresh run or an explicit
transaction skip record with actor, reason, time, and change-set hash; Required
accepts only a fresh completed run with all concerns dispositioned.

## 5. Normative SQLite schema v1

### 5.1 Database header and migration

Set:

```sql
PRAGMA application_id = 1448561732; -- 0x56574C44, "VWLD"
PRAGMA user_version = 1;
```

Every schema connection executes and verifies:

```sql
PRAGMA foreign_keys = ON;
PRAGMA foreign_keys;
PRAGMA application_id;
PRAGMA user_version;
```

The initial migration runs in one transaction and records SHA-256 of its exact
UTF-8 resource text:

```sql
CREATE TABLE schema_migrations (
    version             INTEGER PRIMARY KEY,
    migration_id        TEXT NOT NULL UNIQUE,
    script_sha256       TEXT NOT NULL,
    applied_at_utc      TEXT NOT NULL
) STRICT;
```

Unknown newer versions, missing migrations, or checksum mismatches block writes.

### 5.2 Project and packages

```sql
CREATE TABLE projects (
    project_id              TEXT PRIMARY KEY,
    title                   TEXT NOT NULL CHECK(length(title) > 0),
    purpose_node_id         TEXT NOT NULL UNIQUE,
    head_revision           INTEGER NOT NULL CHECK(head_revision >= 0),
    parent_logical_hash     TEXT NULL,
    logical_hash            TEXT NOT NULL,
    last_commit_id          TEXT NULL,
    policy_json             TEXT NOT NULL CHECK(json_valid(policy_json)),
    created_at_utc          TEXT NOT NULL,
    FOREIGN KEY(purpose_node_id) REFERENCES graph_entities(entity_id)
        ON DELETE RESTRICT DEFERRABLE INITIALLY DEFERRED,
    FOREIGN KEY(last_commit_id) REFERENCES commits(commit_id)
        ON DELETE RESTRICT,
    CHECK(
      (head_revision = 0 AND parent_logical_hash IS NULL AND
       last_commit_id IS NULL) OR
      (head_revision > 0 AND parent_logical_hash IS NOT NULL AND
       last_commit_id IS NOT NULL)
    )
) STRICT;

CREATE TABLE schema_packages (
    package_id              TEXT NOT NULL,
    package_version         TEXT NOT NULL,
    definition_hash         TEXT NOT NULL,
    definition_json         TEXT NOT NULL CHECK(json_valid(definition_json)),
    PRIMARY KEY(package_id, package_version)
) STRICT;
```

Initialization is one SQLite transaction: insert the project row, package/type
rows, the purpose node at `purpose:root`, and its substantive text before
commit. The deferred foreign key permits the project and root to be created
atomically. Logical validation additionally proves that the referenced node is
the sole `core:project-purpose` node.

Gate A permits exactly one row in `projects`; initialization rejects a second.
Because one database contains one project, every stored package is selected by
that project; no join table is needed. `definition_json` is strict canonical
package JSON and must hash to `definition_hash`.

### 5.3 Logical type definitions

```sql
CREATE TABLE entity_types (
    type_id                 TEXT PRIMARY KEY,
    package_id              TEXT NOT NULL,
    package_version         TEXT NOT NULL,
    entity_kind             TEXT NOT NULL CHECK(entity_kind IN ('node','edge')),
    definition_hash         TEXT NOT NULL,
    definition_json         TEXT NOT NULL CHECK(json_valid(definition_json)),
    edge_impact_mode        TEXT NULL CHECK(edge_impact_mode IS NULL OR
        edge_impact_mode IN ('none','source-depends-on-target',
                             'target-depends-on-source','bidirectional')),
    FOREIGN KEY(package_id, package_version)
        REFERENCES schema_packages(package_id, package_version)
        ON DELETE RESTRICT,
    CHECK((entity_kind = 'node' AND edge_impact_mode IS NULL) OR
          (entity_kind = 'edge' AND edge_impact_mode IS NOT NULL))
) STRICT;

```

The application verifies each canonical type definition against the containing
package JSON/hash and validates property schemas, allowed edge endpoint types,
categories, acyclicity flags, and required validators. The normalized type row
supports foreign keys, kind checks, lookup, and impact traversal; it is not a
second definition source. Do not encode opaque behavior in triggers.

### 5.4 Current graph entities

```sql
CREATE TABLE graph_entities (
    entity_id               TEXT PRIMARY KEY,
    entity_revision         INTEGER NOT NULL CHECK(entity_revision >= 1),
    entity_kind             TEXT NOT NULL CHECK(entity_kind IN ('node','edge')),
    type_id                 TEXT NOT NULL,
    properties_json         TEXT NOT NULL CHECK(json_valid(properties_json)),
    tags_json               TEXT NOT NULL CHECK(json_valid(tags_json)),
    extensions_json         TEXT NOT NULL CHECK(json_valid(extensions_json)),
    UNIQUE(entity_id, type_id),
    FOREIGN KEY(type_id) REFERENCES entity_types(type_id)
        ON DELETE RESTRICT
) STRICT;

CREATE TABLE graph_edges (
    edge_id                 TEXT PRIMARY KEY,
    edge_type_id            TEXT NOT NULL,
    source_node_id          TEXT NOT NULL,
    target_node_id          TEXT NOT NULL,
    FOREIGN KEY(edge_id, edge_type_id)
        REFERENCES graph_entities(entity_id, type_id)
        ON DELETE RESTRICT,
    FOREIGN KEY(source_node_id) REFERENCES graph_entities(entity_id)
        ON DELETE RESTRICT,
    FOREIGN KEY(target_node_id) REFERENCES graph_entities(entity_id)
        ON DELETE RESTRICT
) STRICT;

CREATE INDEX ix_graph_entities_type
    ON graph_entities(entity_kind, type_id, entity_id);
CREATE INDEX ix_graph_edges_source
    ON graph_edges(source_node_id, edge_type_id, target_node_id);
CREATE INDEX ix_graph_edges_target
    ON graph_edges(target_node_id, edge_type_id, source_node_id);
CREATE UNIQUE INDEX ux_scope_parent_source
    ON graph_edges(source_node_id)
    WHERE edge_type_id = 'core:scope-parent';
```

Application validation checks entity kind against its type, canonical
property/tag/extension shapes, edge endpoint entity kinds and allowed node types,
and all property constraints. SQLite proves global identity and endpoint
existence. The partial index proves at most one scope parent; application
validation proves exactly one for every non-root node, no cycles, and root
reachability.

### 5.5 Draft transactions

Draft payloads are canonical operation JSON because they are proposed state, not
authoritative normalized graph rows.

```sql
CREATE TABLE draft_transactions (
    transaction_id          TEXT PRIMARY KEY,
    project_id              TEXT NOT NULL,
    base_revision           INTEGER NOT NULL CHECK(base_revision >= 0),
    base_logical_hash       TEXT NOT NULL,
    intent                  TEXT NOT NULL CHECK(length(intent) > 0),
    author                  TEXT NOT NULL CHECK(length(author) > 0),
    created_at_utc          TEXT NOT NULL,
    focus_node_id           TEXT NULL,
    status                  TEXT NOT NULL CHECK(status IN
        ('draft','review-required','ready','rejected','committed','aborted')),
    operations_json         TEXT NOT NULL CHECK(json_valid(operations_json)),
    dispositions_json       TEXT NOT NULL CHECK(json_valid(dispositions_json)),
    change_set_hash         TEXT NULL,
    projected_logical_hash  TEXT NULL,
    draft_revision          INTEGER NOT NULL CHECK(draft_revision >= 1),
    FOREIGN KEY(project_id) REFERENCES projects(project_id) ON DELETE RESTRICT
) STRICT;
```

`operations_json` is the canonical sorted array with one final operation per
target entity; `dispositions_json` is the canonical sorted submitted evidence.
The application validates both completely. `focus_node_id` is noncanonical
authoring metadata and may name a projected node; it is intentionally not a
foreign key. Batch expansion resolves focus before storing operations.

Application draft edits increment `draft_revision`, recompute hashes, and delete
or invalidate dispositions whose fingerprints no longer match.

### 5.6 Validation and commit ledger

```sql
CREATE TABLE validation_runs (
    validation_run_id       TEXT PRIMARY KEY,
    transaction_id          TEXT NULL,
    evaluated_logical_hash  TEXT NOT NULL,
    change_set_hash         TEXT NULL,
    outcome                 TEXT NOT NULL CHECK(outcome IN
        ('proven-valid','invalid','inconclusive')),
    report_json             TEXT NOT NULL CHECK(json_valid(report_json)),
    report_hash             TEXT NOT NULL,
    created_at_utc          TEXT NOT NULL,
    FOREIGN KEY(transaction_id) REFERENCES draft_transactions(transaction_id)
        ON DELETE RESTRICT
) STRICT;

CREATE TABLE commits (
    commit_id               TEXT PRIMARY KEY,
    project_id              TEXT NOT NULL,
    project_revision        INTEGER NOT NULL UNIQUE CHECK(project_revision >= 1),
    parent_logical_hash     TEXT NOT NULL,
    logical_hash            TEXT NOT NULL UNIQUE,
    change_set_hash         TEXT NOT NULL,
    impact_hash             TEXT NOT NULL,
    validation_hash         TEXT NOT NULL,
    operations_json         TEXT NOT NULL CHECK(json_valid(operations_json)),
    dispositions_json       TEXT NOT NULL CHECK(json_valid(dispositions_json)),
    validation_report_json TEXT NOT NULL CHECK(json_valid(validation_report_json)),
    author                  TEXT NOT NULL,
    intent                  TEXT NOT NULL,
    committed_at_utc        TEXT NOT NULL,
    FOREIGN KEY(project_id) REFERENCES projects(project_id) ON DELETE RESTRICT
) STRICT;
```

The accepted commit and project-head update occur in the same SQLite transaction
as current-state row changes. Canonical arrays in the commit row retain exact
operations, dispositions, explanation paths, and the accepted report. Diagnostics
remain structured entries inside `report_json`/`validation_report_json`; stable
read views use `json_each`. Drafts are retained in a final status by default.
Optional maintenance may delete a draft only after explicitly deleting its
noncanonical validation runs; cleanup is never part of the canonical commit.

These tables plus `schema_migrations`, `projects`, `schema_packages`,
`entity_types`, `graph_entities`, `graph_edges`, and `draft_transactions` are the
nine Gate A v1 tables.

### 5.7 Read views

Provide stable documented views:

```text
vw_project_head
vw_schema_packages
vw_entity_types
vw_nodes
vw_edges
vw_direct_dependencies
vw_commits
vw_diagnostics
```

`vw_direct_dependencies` returns:

```text
dependent_node_id
dependency_node_id
edge_id
edge_type_id
impact_mode
```

It expands each canonical edge according to its type's impact mode; a
bidirectional edge produces two rows and an impact-`none` edge produces none. It
is a convenience/read surface, not separate canon. Application expansion and the
view must be tested against the same golden dependency-arc set. Property and
diagnostic views use SQLite JSON functions over canonical payloads.

## 6. Connection, safety, and integrity

### 6.1 Connection factory

Use `Microsoft.Data.Sqlite.Core` directly with an explicitly pinned/audited
SQLitePCLRaw native bundle. Do not rely on an unreviewed transitive native SQLite
version. On open:

1. Resolve an absolute `.vw.db` path supplied by the caller.
2. Open with the intended mode (`ReadOnly` for query; `ReadWrite` for existing;
   `ReadWriteCreate` only for explicit initialization).
3. Set and verify foreign keys.
4. Set a bounded busy timeout.
5. Refuse extension loading.
6. Verify application ID, user version, migrations, and expected tables/views.
7. Run `PRAGMA quick_check` on normal open and `integrity_check` on explicit
   verify/recovery commands.
8. Load the logical snapshot and verify its head hash before canonical writes.

Never interpolate user values, identifiers, property names, or type names into SQL.
All runtime data uses parameters. Migration SQL is a checked embedded resource.

SQLite is an in-process library, not a server. Gate A must not require a separate
SQLite installation, the `sqlite3` CLI, Docker, or a system-native provider. The
pinned bundle deploys the native library with ValidatedWorld. Application startup
reports the SQLite library version in structured status/diagnostic output.

### 6.2 Journal and concurrency mode

Gate A defaults to rollback journal mode for a portable single-file-at-rest
artifact. WAL may be an opt-in workspace setting after backup/copy behavior and
sidecar management are documented and tested.

Use `BEGIN IMMEDIATE` for final commit. A busy writer produces a typed conflict;
retry policy is bounded and cancellation-aware. Never wait for human/AI input
while holding the write transaction.

### 6.3 Backup and recovery

Provide an Application backup use case using SQLite's online backup API to a new
validated destination. Do not advertise copying an open database as a safe
backup.

On commit failure, SQLite rollback is authoritative. On startup, draft and
validation rows may remain; they cannot change the project head or current
entities. Gate A does not invent a second journal or file-replacement protocol.

### 6.4 Untrusted databases

Treat project databases as untrusted:

- refuse unknown application IDs/migrations;
- do not enable loadable extensions;
- do not execute SQL stored in project data;
- cap file size/page count, logical entity/value counts, JSON lengths, graph
  edges, traversal depth, diagnostics, and output bytes;
- use read-only connections for inspection where possible;
- validate every value after mapping even if SQL constraints passed.

## 7. Canonical logical JSON

### 7.1 Shape

```json
{
  "schemaVersion": "validatedworld/v1",
  "project": {
    "id": "project:offline-sensor",
    "title": "Offline Sensor Project",
    "revision": 3,
    "parentLogicalHash": "sha256:...",
    "logicalHash": "sha256:...",
    "lastCommitId": "tx:...",
    "purposeNodeId": "purpose:root",
    "policy": {}
  },
  "schemaPackages": [],
  "nodes": [],
  "edges": []
}
```

Each package contains complete node/edge type and scalar-property definitions.
Each node contains `id`, `revision`, `typeId`, `properties`, `tags`, and
`extensions`. Each edge adds `sourceNodeId` and `targetNodeId`. Every graph link
is in `edges`; property values never act as references.

### 7.2 Ordering

- packages by `(packageId, semanticVersionOrdinal)`;
- types by type ID;
- properties by declared ordinal then name;
- nodes by node ID and edges by edge ID;
- property names ordinally;
- ordered values by ordinal;
- unordered values by canonical value;
- tags and extension keys ordinally.

Numbers, strings, instants, booleans, escaping, and whitespace have one normative
encoding. Reject duplicate JSON properties in protocol input.

### 7.3 Hashes

```text
LogicalHash = SHA-256(canonical logical snapshot with project.logicalHash omitted)

ChangeSetHash = SHA-256(canonical transaction identity/base plus operations
                        sorted by target ID)

ProjectedLogicalHash = SHA-256(canonical projected logical state using
                               provisional next head properties)
```

Review evidence and acknowledgements do not alter projected logical state and are
excluded from `ChangeSetHash`.

### 7.4 Snapshot import/export boundary

`snapshot write` produces the canonical logical JSON for audit, testing,
interchange, or source control. `snapshot init` accepts only a revision-zero
snapshot and may initialize a new database only after full
schema/profile/semantic validation. Later-revision snapshots lack the commit
operations needed to recreate their audit chain; use SQLite backup to clone a
complete existing project.

Gate A does not overwrite an existing authoritative database from a snapshot and
does not call snapshots document exports.

The SQLite project file and `vw backup` output are the primary complete project
transfer artifacts because they retain current state plus drafts and audit
history. Logical JSON is an optional transparent interchange/audit/fixture
surface, not a requirement that users reconstruct databases themselves.

## 8. Indexes and dependency graph

### 8.1 Project index

```csharp
public sealed class ProjectIndex
{
    public ProjectSnapshot Snapshot { get; }
    public ImmutableDictionary<EntityId, ProjectEntity> EntitiesById { get; }
    public ImmutableDictionary<EntityId, ProjectNode> NodesById { get; }
    public ImmutableDictionary<EntityId, ProjectEdge> EdgesById { get; }
    public ImmutableDictionary<TypeId, NodeTypeDefinition> NodeTypesById { get; }
    public ImmutableDictionary<TypeId, EdgeTypeDefinition> EdgeTypesById { get; }
    public ImmutableDictionary<EntityId, ImmutableArray<ProjectEdge>> EdgesBySource { get; }
    public ImmutableDictionary<EntityId, ImmutableArray<ProjectEdge>> EdgesByTarget { get; }
}
```

Duplicate/ambiguous rows are integrity failures, never last-writer-wins.

### 8.2 Dependency arcs

```csharp
public sealed record DependencyEdge(
    EntityId DependentId,
    EntityId DependencyId,
    EntityId EdgeId,
    TypeId EdgeTypeId);
```

Expand canonical edges only:

```text
none:                       emit nothing
source-depends-on-target:   emit source -> target
target-depends-on-source:   emit target -> source
bidirectional:              emit both arcs
```

Do not reflect over properties/JSON looking for IDs, and do not let validators
invent hidden arcs. Keep all explicit edge evidence even when dependent/
dependency pairs repeat; traversal may deduplicate nodes while retaining sorted
edge evidence.

### 8.3 Impact algorithm

```text
ComputeImpact(operations, baseGraph, projectedGraph, policy):
    changedEntityIds = direct operation targets
    nodeSeeds = directly changed nodes
    for each changed edge in base or projection:
        add its dependent endpoint(s) according to impact mode
    reverseUnion = review-impact reverse edges from base and projected graphs
    visited = nodeSeeds
    queue = nodeSeeds sorted ordinal at depth 0
    predecessor = empty

    while queue not empty:
        current, depth = dequeue
        if depth == MaximumDependencyDepth:
            mark truncated/inconclusive
            continue

        for dependent in reverseUnion[current] sorted ordinal:
            if visited.Add(dependent):
                predecessor[dependent] = deterministic selected edge/evidence
                enqueue(dependent, depth + 1)

        if visited count > MaximumImpactNodes:
            return Inconclusive with partial evidence

    return Complete(changed entities, node seeds,
                    impacted nodes excluding seeds, shortest paths)
```

Use breadth-first traversal. Tie between predecessor nodes resolves by node ID;
retain all sorted edge evidence for the selected hop. Base-only edges preserve impact
from removed dependencies; projected-only edges include new dependencies.

Do not add scope ancestors, forward context dependencies, or any other
review-context node to `nodeSeeds`. The `scope-parent` edge therefore behaves
like every other source-depends-on-target edge: changing a parent reaches its
dependent children, while merely walking upward from a changed child never turns
the parent into a second traversal seed. This makes leaf, intermediate, and root
changes progressively wider—but controlled.

## 9. Schema and graph validation

### 9.1 Stored schema package validation

Validate:

1. Package ID/version/hash and required validators.
2. Unique node/edge type IDs and valid entity kinds.
3. Unique scalar-property ordinals and names within each complete type.
4. Property kinds/cardinalities/constraints.
5. Edge allowed source/target types, impact mode, and acyclicity declarations.
6. Exact type-definition rows match the containing canonical package JSON/hash.
7. Exact registered package definition hash matches stored definitions.

A mismatch between built-in code and stored built-in schema is an unsupported or
inconsistently modified project, not an opportunity to guess. This check detects
accidental/incomplete mutation, not a privileged attacker who rewrites data,
hashes, and history together.

### 9.2 Entity validation

For every entity:

1. Validate ID and revision.
2. Resolve type and require matching node/edge kind.
3. Load the type's complete declared scalar properties.
4. Reject unknown properties outside namespaced extensions.
5. Check cardinality, order, canonical values, symbols/ranges/patterns.
6. For an edge, resolve source/target as nodes and check allowed endpoint types.
7. Ensure each edge entity agrees with its `graph_edges` row.
8. Reject any semantic reference encoded in a property or extension.
9. Report uncovered extension namespaces.

### 9.3 Generic constraints

Initial validators include:

- exactly one referenced `core:project-purpose` node;
- exactly one `core:scope-parent` for every non-root node, with
  an acyclic singular path terminating at that purpose;
- selected explicit contradictions;
- minimum support edges/sources;
- Tarjan SCC detection for selected edge types;
- unique active definitions;
- implementation and verification coverage;
- required impact dispositions.

These prove explicit graph properties, not prose or real-world truth.

## 10. Transactions, review, and commit

### 10.1 Operations

```csharp
public abstract record ProjectOperation
{
    public required EntityId TargetId { get; init; }
    public sealed record Add(ProjectEntityDraft Entity) : ProjectOperation;
    public sealed record Replace(int ExpectedRevision, ProjectEntityDraft Entity)
        : ProjectOperation;
    public sealed record Remove(int ExpectedRevision) : ProjectOperation;
}
```

One final operation per target. Editing a draft replaces that operation and
increments draft revision. The operation document cannot set committed revision.

Focused input is expanded before operations are stored:

```csharp
public sealed record AuthoringBatch(
    EntityId? FocusNodeId,
    ImmutableArray<ProjectNodeDraft> Nodes,
    ImmutableArray<ProjectEdgeDraft> Edges);

public sealed record BatchExpansion(
    ImmutableArray<ProjectOperation> Operations,
    ImmutableArray<EntityId> AddedScopeParentEdgeIds);
```

For each new node without an explicit `scope-parent` edge, require a focus and
add an edge from that node to the focus. A cluster helper first creates a normal
scope node, attaches it to the current focus, then uses it as focus for its
children. Return the complete expansion; never infer non-scope semantic edges.

### 10.2 Projection

```text
Apply(base, transaction):
    require project ID/base revision/base hash match
    copy current entities to ID-keyed node/edge builders

    for operation sorted by target ID:
        Add: require absent; materialize revision 1
        Replace: require current expected revision; preserve entity ID;
                 preserve entity kind/type; materialize expected revision + 1
        Remove: require current expected revision; remove target only

    materialize sorted immutable snapshot with provisional next head
    validate every post-operation edge endpoint; never cascade
```

Changing an entity's kind or logical type is rejected in Replace; remove it and
add a new stable ID or use a future explicit migration workflow.

### 10.3 Review fingerprints

```text
TargetFingerprint = SHA-256(target ID + projected revision + canonical entity)
ImpactFingerprint = SHA-256(ChangeSetHash + target ID + ordered shortest path)
```

Direct policy-selected changes receive `updated`. Other selected impacts require
submitted `reviewed-no-change` or `not-applicable` with reviewer, rationale, time,
and exact fingerprints. Inconclusive required impact blocks commit.

Gate B adds a separate AI-review freshness fingerprint:

```text
AiReviewFingerprint = SHA-256(
    base hash + draft revision + change-set hash + projected hash
    + review profile/version + prompt template/hash + review-plan hash
    + exact request hash + provider/model/reasoning + timeout)
```

Changing any component makes the run and its concern dispositions stale. API
keys and authorization data are never fingerprint inputs.

### 10.4 Validation report

```csharp
public sealed record ValidationReport(
    ValidationOutcome Outcome,
    string BaseLogicalHash,
    string EvaluatedLogicalHash,
    string? ChangeSetHash,
    ImmutableArray<PhaseResult> Phases,
    ImmutableArray<Diagnostic> Diagnostics,
    ImpactResult? Impact,
    ImmutableArray<ReviewObligation> ReviewObligations,
    CoverageReport Coverage,
    ValidationStatistics Statistics);
```

Unexpected validator exceptions become stable internal-failure diagnostics and
make the phase inconclusive. Cancellation is inconclusive. Sort diagnostics by
phase, severity, code, primary ID, evidence ID, fingerprint.

### 10.5 Commit orchestration

```text
Commit(transactionId):
    load draft and operations
    read/verify base snapshot
    reject base mismatch
    projection = Apply(base, operations)
    baseGraph/projectedGraph = Build(...)
    impact = ComputeImpact(...)
    obligations = BuildObligations(...)
    report = ValidateFull(...)
    [Gate B] load/verify any policy-required AI review run and concern dispositions
    reject unless commit policy accepts

    begin short SQLite IMMEDIATE write transaction
    re-read project head and draft revision/change-set hash
    reject/rollback if either changed
    verify submitted dispositions still match computed evidence
    normalize and apply every accepted entity mutation
    insert commit operations/dispositions/report/commit
    compute/verify final logical snapshot hash from transaction-visible rows
    update project head
    commit SQLite transaction once
    return accepted JSON result
```

If the head and exact draft revision/hash are unchanged after acquiring the write
transaction, the previously computed full semantic report is valid. The final
logical snapshot is reloaded/hash-checked inside the transaction to catch mapping
or persistence defects.

### 10.6 Persistence mutation order

For replacements/removals, perform explicit ordered row changes that keep
foreign keys valid at statement boundaries. Gate A may use deferred foreign-key
constraints where needed, but must finish with `PRAGMA foreign_key_check` clean
inside the transaction.

Recommended approach:

1. Insert headers for newly added nodes before edges that target them.
2. Remove changed/removed edge rows before deleting their entity headers.
3. Rewrite changed entity property/tag/extension JSON and edge endpoint rows.
4. Delete removed node headers only after all incident edges were explicitly
   repaired or removed by operations.
5. Write audit and head rows.
6. Verify relational integrity and logical hash.

No `ON DELETE CASCADE` compensates for a missing operation.

### 10.7 Replay

Replay starts from a logical base snapshot or an initialized empty database with
matching schema packages and applies accepted operations in revision order using
the normal projection/validation rules. It must reproduce every recorded head
hash. Gate A does not implement branch, merge, rebase, or arbitrary rollback.

## 11. Diagnostics

Ranges:

```text
VW10xx  database application ID, migrations, integrity, mapping
VW11xx  schema packages, node/edge types, properties, validators
VW12xx  graph entities, property values, edge endpoints
VW13xx  profile assertions and lifecycle
VW14xx  dependency-arc expansion and edge semantics
VW15xx  support, cycles, definitions, traceability
VW16xx  impact and review obligations
VW18xx  transaction, concurrency, commit, audit, replay
VW20xx  coverage, context, read views
VW90xx  internal failure or unsupported construct
```

Initial codes:

```text
VW1001 invalid/unrecognized SQLite application file
VW1002 migration missing or checksum mismatch
VW1003 SQLite integrity/foreign-key failure
VW1004 logical snapshot hash mismatch
VW1005 relational-to-logical mapping mismatch
VW1101 invalid/unsupported schema package
VW1102 duplicate or inconsistent logical type definition
VW1103 invalid scalar-property definition
VW1104 invalid edge endpoint/impact definition
VW1105 required validator unavailable
VW1201 invalid/duplicate entity ID
VW1202 entity kind/type mismatch
VW1203 invalid/missing scalar property
VW1204 missing edge endpoint node
VW1205 incompatible edge endpoint type
VW1301 accepted assertion contradiction
VW1401 dependency extraction mismatch
VW1402 invalid semantic edge
VW1501 missing required support
VW1502 forbidden dependency cycle
VW1503 missing implementation
VW1504 missing verification
VW1505 missing, duplicate, or invalid project purpose
VW1506 missing, ambiguous, cyclic, or disconnected scope-parent lineage
VW1601 impact bound reached
VW1602 required impact disposition pending
VW1603 stale impact disposition
VW1801 stale project head
VW1802 operation precondition failed
VW1803 commit policy rejected
VW1804 SQLite busy/write failure
VW1805 commit replay mismatch
VW2001 incomplete semantic/profile coverage
VW2002 context/query limit reached
VW9001 validator internal failure
VW9002 unsupported construct
```

Do not recycle a code's meaning within protocol v1.

## 12. Application and CLI

### 12.1 Application handlers

```text
InitializeProject
OpenAndVerifyProject
GetProjectStatus
WriteLogicalSnapshot
InitializeFromRevisionZeroSnapshot
BackupProject

GetNode
ListNodes
SearchNodes
GetEdge
ListEdges
GetScopeChildren
GetScopeAncestors
GetScopeSubtree
GetNeighbors
GetDependencies
GetDependents
ExplainDependencyPath
BuildContextQuery

BeginTransaction
GetTransaction
SetTransactionFocus
ExpandAuthoringBatch
ApplyOperations
AnalyzeTransactionImpact
ValidateTransaction
ListReviewObligations
SetReviewDisposition
CommitTransaction
AbortTransaction

GetCommit
ListCommits
VerifyCommitReplay
```

Gate B later adds these Application use cases without changing the Gate A
contracts:

```text
PreviewAiSemanticReviewRequest
RunAiSemanticReview
SkipOptionalAiSemanticReview
GetAiSemanticReviewRun
ListAiSemanticReviewConcerns
SetAiSemanticReviewConcernDisposition
```

Expected invalid input returns `OperationResult<T>`, not exceptions. Every read
result includes project revision/hash. Every draft result includes transaction
ID, base identity, draft revision, and change-set/projected hashes as applicable.

### 12.2 CLI commands

```text
vw init --db <path> --project-id <id> --title <text> --purpose <text>
vw verify --db <path> [--full]
vw status --db <path>
vw snapshot write --db <path> --output <path-or-stdout>
vw snapshot init --input <revision-zero-path-or-stdin> --db <new-path>
vw backup --db <path> --output <new-path>
vw sample list
vw sample create --sample <name> --variant <name> --db <new-path>

vw node get --db <path> --id <id>
vw node list --db <path> [--type <id>] [--tag <value>]
vw node search --db <path> --query <text> [--type <id>] [--tag <value>]
               [--scope <id>] [--limit <n>] [--cursor <token>]
vw edge get --db <path> --id <id>
vw edge list --db <path> [--type <id>] [--source <id>] [--target <id>]
vw scope children --db <path> --id <id> [--limit <n>] [--cursor <token>]
vw scope ancestors --db <path> --id <id>
vw scope subtree --db <path> --id <id> --max-nodes <n> [--cursor <token>]
vw neighbors --db <path> --id <id> [--edge-type <id>] [--limit <n>]
vw dependencies --db <path> --id <id> [--transitive]
vw dependents --db <path> --id <id> [--transitive]
vw explain path --db <path> --from <id> --to <id>
vw context --db <path> --seed <id> [--max-nodes <n>]

vw tx begin --db <path> --intent <text> --author <text>
vw tx show --db <path> --tx <id>
vw tx focus --db <path> --tx <id> --node <id>
vw tx expand --db <path> --tx <id> --batch <file-or-stdin>
vw tx apply --db <path> --tx <id> --operations <file-or-stdin>
vw tx impact --db <path> --tx <id>
vw tx validate --db <path> --tx <id>
vw tx obligations --db <path> --tx <id>
vw tx disposition --db <path> --tx <id> --target <id>
                  --status <reviewed-no-change|not-applicable>
                  --reason <text> --reviewer <id>
vw tx commit --db <path> --tx <id>
vw tx abort --db <path> --tx <id>

vw commit get --db <path> --revision <number>
vw commit verify --db <path> [--through <number>]
```

Planned Gate B commands are explicit network/audit operations:

```text
vw tx ai-review preview --db <path> --tx <id> --output <path>
vw tx ai-review run --db <path> --tx <id>
vw tx ai-review skip --db <path> --tx <id> --actor <id> --reason <text>
vw tx ai-review get --db <path> --tx <id> --run <id>
vw tx ai-review concerns --db <path> --tx <id> --run <id>
vw tx ai-review disposition --db <path> --tx <id> --run <id>
    --concern <id> --status <rejected-with-rationale|acknowledged>
    --reason <text> --reviewer <id>
```

`preview` never contacts OpenAI. It writes the exact complete request artifact
and reports scope, coverage, omissions, hashes, and size. `run` is the only
network call and sends that single request at most once. Supplying credentials
as command arguments or JSON is forbidden.

`skip` makes no network call and is accepted only when project policy is
`Optional`; it records actor, reason, time, draft revision, and change-set hash.
Changing the draft makes the skip stale. Policy `Required` rejects `skip`, and
`Disabled` needs neither run nor skip.

There is no arbitrary `vw sql` write command. Users may open a verified database
read-only with standard SQLite tools and use documented `vw_*` views.

No user must do so: every normal workflow is available through the CLI JSON
contract. Standard SQLite tools are optional advanced readers, not runtime or QA
dependencies.

### 12.3 Result envelope

```json
{
  "outputSchemaVersion": "validatedworld-cli/v1",
  "command": "tx.impact",
  "status": "review-required",
  "project": {
    "id": "project:offline-sensor",
    "revision": 3,
    "logicalHash": "sha256:..."
  },
  "transaction": {
    "id": "tx:...",
    "baseRevision": 3,
    "baseLogicalHash": "sha256:...",
    "draftRevision": 4,
    "changeSetHash": "sha256:...",
    "projectedLogicalHash": "sha256:..."
  },
  "diagnostics": [],
  "coverage": {},
  "data": {}
}
```

Exactly one JSON document goes to stdout. Operational logs go to stderr.

### 12.4 Exit codes

```text
0  completed / proven valid where validation applies
2  deterministic validation rejected
3  command/input contract error
4  stale head/entity/draft precondition or writer conflict
5  database integrity, migration, mapping, or write failure
6  required analysis or AI review inconclusive
7  required impact or AI-concern dispositions pending
8  unsupported schema/package/validator or migration required
9  internal failure
```

### 12.5 Incremental public walking skeleton

Do not defer all public-interface and usability evidence until WP8. Deliver the
CLI progressively so realistic black-box use starts with the first database
slice:

```text
WP3  init/status/structural verify/snapshot read plus node/edge reads;
     generated TechnicalProject database and documented read-only views
WP4  full verify, diagnostics, dependencies/dependents, and explanation reads
WP5  tx begin/show/focus/expand/apply/validate/abort
WP6  tx impact/obligations/disposition
WP7  tx commit plus commit get/verify/replay
WP8  context, backup, remaining query/help contracts, limits, and polish
```

WP3 adds `ValidatedWorld.Cli.Tests`. A checked-in fixture builder may create the
realistic database for early read-only QA before public mutation is available;
it is development infrastructure, not a canonical-write escape hatch. Commands
must label validation phases that are not yet implemented as inconclusive rather
than implying the whole project is valid.

Each WP3-WP8 slice retains deterministic CLI contract tests and is followed by
an actual agent-operated black-box walkthrough described in Section 16.4.

## 13. Context queries and planned AI semantic review

### 13.1 Deterministic context query

A context query selects logical JSON for a human, AI, or RAG consumer. It does
not generate content.

AI-first authoring also depends on deterministic discovery. `SearchNodes`
matches only type-declared display/search scalar text after invariant
case-normalization and may filter by exact type, tag, lifecycle property, and
scope subtree. Results sort by exact-ID match, normalized display text, then
entity ID, with an opaque cursor bound to project revision/hash and query. Scope
children/ancestors/subtree and semantic-neighbor queries use the same bounded,
stable pagination contract. No query uses embeddings, natural-language SQL, or
provider calls. If property scanning is too slow at measured Gate A scale, add a
checked materialized search index in a later migration; do not alter canon.

```csharp
public sealed record ContextQueryResult(
    string SchemaVersion,
    ProjectId ProjectId,
    long ProjectRevision,
    string LogicalHash,
    ImmutableArray<EntityId> SeedIds,
    ImmutableArray<ProjectNode> Nodes,
    ImmutableArray<ProjectEdge> Edges,
    ImmutableArray<ImpactStep> SelectionPaths,
    ImmutableArray<EntityId> OmittedEntityIds,
    bool Truncated,
    string ResultHash);
```

Priority:

```text
0 seed nodes
1 exact type definitions and applicable constraints
2 singular `scope-parent` lineage from each selected node to purpose root
3 forward dependencies needed to understand seeds
4 reverse impacted dependents and paths
5 external anchors bound through edges
6 additional related nodes by increasing graph distance
```

Within priority/distance sort by ID. Include each entity atomically. Report
limits and omissions. A required seed/type/constraint that cannot fit produces
inconclusive output. Walking the scope lineage is upward-only; an included
ancestor is never treated as a seed and its other children are not selected.

### 13.2 Gate B whole-transaction request and coverage

Gate B reuses context-query primitives but does not equate one convenience
context query with complete review. For an exact draft it first constructs:

```csharp
public sealed record AiReviewPlan(
    string SchemaVersion,
    TransactionId TransactionId,
    long DraftRevision,
    string ChangeSetHash,
    string ProjectedLogicalHash,
    ImmutableArray<EntityId> RequiredNodeIds,
    ImmutableArray<EntityId> RequiredEdgeIds,
    ImmutableArray<ScopeLineage> RequiredScopeLineages,
    AiReviewRequestManifest Request,
    ImmutableArray<EntityId> ExcludedEntityIds,
    string PlanHash);
```

Required scope is the union of:

```text
direct operation targets
policy-selected complete reverse impact closure and explanation edges
forward dependencies needed to understand every changed/impacted node
singular upward scope-parent lineage for every included node
applicable exact type definitions and constraints
policy-selected bound external anchors
```

Sort all sets and hashes canonically and produce exactly one request. Multiple
disjoint transaction chains remain together. A coverage matrix proves that every
required node, edge, constraint, operation, explanation path, and
scope-parent edge appears in that request. It also proves that context-only
ancestors did not expand impact into unselected siblings.

If the complete selected scope exceeds the fixed model/request bound, contains an
oversized indivisible entity, or has missing coverage, planning is inconclusive
before any network call. Do not shard, summarize, synthesize across calls, drop a
chain, select a fallback model, or make a second paid request. Coverage proves
what was presented, not that the model understood it.

### 13.3 Provider contract, concerns, and freshness

```csharp
public interface IProjectSemanticReviewClient
{
    Task<AiReviewResponse> ReviewAsync(
        AiReviewRequest request,
        CancellationToken cancellationToken);
}
```

The request includes the complete ordered operations, base/draft/change-set and
projected hashes, review profile/version, prompt template/version/hash, review
plan/request hashes, OpenAI/model/reasoning/timeout values, strict response-schema
version, purpose statement, required scope lineages, and all selected content.
The cache/request identity hashes every material non-secret property.

The response is strict versioned JSON containing status, structured concerns,
insufficient-context observations, and a separate list of candidate
nodes/edges/operations. Each concern has a run-local stable ID, `AIxxxx`
code, category, severity, message, supplied entity IDs, property/edge/path evidence,
optional confidence, suggested follow-up, and fingerprint.

Unknown citations, schema mismatch, refusal, truncation, timeout, cancellation,
or malformed content fails or makes the run inconclusive. Do not parse free-form
prose into findings. Candidate changes require an explicit normal transaction
operation. Concern dispositions are `open`, `resolved-by-change`,
`rejected-with-rationale`, or `acknowledged`; policy decides which are
acceptable. Changing any request-identity property stales the run and dispositions.

Gate B adds checked migration tables for run metadata, request manifests, and
concerns. They are draft/audit state and do not enter the logical project hash.
Accepted receipts retain the exact satisfying run/fingerprints. Persist no
credential or authorization header. Raw provider bodies are retained only under
an explicit project-data retention setting; normalized concerns and body hashes
remain auditable.

### 13.4 OpenAI isolation, secrets, and cost safety

`ValidatedWorld.AiReview.OpenAI` is the sole production client. At Gate B
implementation time, pin and audit the current official `OpenAI` NuGet client,
use the Responses API with strict structured output, use only
`gpt-5.6-terra` with medium reasoning, expose no tools, use a fixed 16,384-token
maximum output, set background mode, treat the configured timeout as an
end-to-end deadline (default 1,200 seconds), and record the actual returned
model. Re-check the official API documentation before implementation. The client interface remains so normal
tests can use a fake; it does not authorize another provider or model.

Gate B adds `Microsoft.Extensions.Configuration.UserSecrets` to the CLI and
reads `AiReview:OpenAI:ApiKey` from .NET user-secrets in source development or
`OPENAI_API_KEY` from the process environment. Non-secret settings use the
`VW_`-prefixed hierarchical names in `.env.example`. Never accept secrets in CLI
arguments, project JSON, or the database. `.env` files are ignored but are not
searched or loaded automatically.

`VW_AIREVIEW__LIVETESTS=false` is the sole test-harness network opt-in. Unit,
integration, and ordinary end-to-end tests ignore it and always use a fake or
scripted HTTP. It does not govern user transaction behavior. Gate B adds project
policy `AiReviewMode = Disabled | Optional | Required`; Optional permits an
explicit transaction-scoped `skipped` record with actor/rationale, while
Required blocks commit without a fresh successful review and concern
dispositions. No environment value can override Required.

Only `vw tx ai-review run` contacts OpenAI, before any SQLite write transaction.
It starts exactly one background response and polls that same response until a
terminal state or a 1,200-second end-to-end deadline. Polling is not a retry or
additional model request. There are zero automatic retries. Refusal, truncation,
deadline expiry, cancellation, malformed output, or transport failure is
inconclusive.
Planning, preview, deterministic validation, impact, commit, and the default test
suite remain offline.

The coding agent may not begin this feature unless the initiating human prompt
contains the exact standalone line `AI_REVIEW_SECRET_READY: yes`. The agent must
never find, acquire, list, infer, or set a key. A live call additionally requires
`AI_REVIEW_LIVE_CALL_AUTHORIZED: yes` in that initiating prompt. Without it the
agent may produce and inspect the exact request preview, but sends nothing. See
[Planned AI semantic review](ai_semantic_review.md) for the complete prompt,
security, privacy, cost, failure, and evaluation contract.

### 13.5 Planned AI authoring tool loop

Gate C composes an OpenAI Responses client with a strict host whose tools map
one-to-one to Application read, draft, validation, impact, and review use cases.
The same versioned schemas later back MCP. The model can begin/resume a draft,
search/navigate, expand batches, apply operations, validate, inspect impact, and
prepare confirmation. It cannot execute SQL, mutate packages/canon, suppress
rules, disposition concerns for the user, or call commit directly.

New-project intake first creates a noncanonical proposal from the user's
description and explicit text/image inputs. After the user confirms purpose and
profile in conversation, the agent calls a guarded normal initialization tool to
create the database/root and authors all other content as a draft.
Existing-project sessions search before creating,
retrieve bounded context, ask questions for materially different
interpretations, and preserve unresolved assumptions/coverage.

The Gate C proof adds a reviewed `catalog/v1` package for menu/catalog, section,
item, option/variant, ingredient/attribute, availability, and source anchors plus
their explicit containment/classification/provenance/dependency edges. The agent
may select installed exact package versions but cannot invent or mutate logical
schema packages during an ordinary authoring session. Unsupported vocabulary is
a user question or structured stop, not an extension-JSON escape hatch.

The host presents a final preview. The user may approve it in the conversation,
which creates a short-lived authorization bound to exact
head/draft/change-set/projected hashes and review state. The agent passes that
authorization to the guarded `CommitTransaction` tool; changed state invalidates
it. Gate B is independently authorized and tool-free. Author repairs stale its
run.

Provider responses run in background mode with a 1,200-second end-to-end
deadline and zero automatic paid retries. Tool-result continuations within an
authoring turn are expected orchestration, not retries. Fixed limits bound calls,
operations, context, and repair loops; hitting one preserves the draft and asks
the user whether to resume. See
[AI-first authoring and intake](ai_authoring_agent.md) for the normative feature,
approval, secret, intake, plugin, and evaluation design.

## 14. Gate A package and sample

### 14.1 Built-in packages

Embed canonical package JSON resources for:

- `core/v1`: project-purpose and scope/constraint nodes, `scope-parent`
  child-to-parent edge, artifact/anchor nodes, binds/uses/mentions edges, and generic constraint
  vocabulary;
- `technical-project/v1`: subject, proposition, assertion, source, and technical
  dependency/traceability vocabulary.

Initialization validates the resources, computes their definition hashes, and
normalizes them into schema tables. It requires a substantive purpose string,
creates `purpose:root`, and stores that ID on the project. The same resources
generate documentation goldens. Do not duplicate package definitions as C# enum
switches and SQL seed scripts; one canonical resource plus registered validator
identifiers is the source.

The CLI also ships a deterministic named sample catalog sourced from the
reviewed files under `samples/`. `vw sample create` passes those logical inputs
through normal Application/Persistence initialization; it never copies a
hand-edited opaque database. Tests verify that packaged sample assets match the
repository source.

### 14.2 TechnicalProject fixture

`samples/TechnicalProject/project.vw.db` is a disposable, ignored build artifact
generated deterministically by `vw sample create` from a checked-in
revision-zero snapshot and initialization/transaction recipes. Never commit or
hand-edit its binary bytes. Check in the canonical scenario manifest, baseline
snapshot, recipes, goals, expected result goldens, and fixture-building command
instead.

The graph contains a quantitative power track:

```text
purpose: design and document an offline privacy-preserving sensor product
scope: purpose -> power, privacy, documentation, accessibility
       power -> runtime requirement, current, capacity, result, conclusion

requirement: runtime >= 24 hours
assumption: average current = 20 mA
assumption: battery capacity = 500 mAh
result: nominal runtime = 25 hours
conclusion: battery satisfies runtime requirement

derived-from(result, current)
derived-from(result, capacity)
depends-on(conclusion, result)
satisfies(conclusion, requirement)

anchors: requirements, power-budget, architecture, verification, privacy
binding edges from semantic nodes to relevant anchors
```

Changing current to 25 mA must impact the result, conclusion, and relevant
anchors, not privacy or accessibility. The engine includes the changed node's
power-to-purpose lineage as context without treating those ancestors as new
impact seeds. It does not perform arithmetic; fixture operations supply the known
repaired capacity/runtime values.

The same project also contains a soft-logic privacy/offline-design track:

```text
requirement: raw observations must not leave the device
definition: raw observation versus aggregated diagnostic
assumption: normal operation has no network transport
assumption: raw readings are retained locally for 7 days
decision: use an encrypted local ring buffer
claim: the architecture satisfies the offline/privacy requirement
evidence: threat-model review and offline integration-test result
implementation: transport-disabled production configuration
verification: no-upload and retention-bound checks

anchors: privacy requirements, threat model, architecture decision record,
         storage design, verification plan, user manual
unrelated distractors: battery enclosure color and accessibility copy
```

All dependencies are explicit. Scenarios include changing retention from 7 to
30 days, permitting a narrowly scoped diagnostic upload, removing evidence,
introducing an explicit contradiction, and repairing the resulting impact/review
set. Expected results include both required paths and unrelated exclusions. This
corpus is complex enough to expose modeling and diagnostic problems without
claiming that the engine understands arbitrary prose.

The fixture source uses one focused batch for each major scope. For example, a
power batch creates its nodes and inherits `scope:power` as their parent, while
explicit `derived-from`, `depends-on`, and `satisfies` edges are supplied in the
same batch. Golden expansion output proves the convenience introduced no hidden
semantic edge.

A separate transaction changes `purpose:root`; its reverse `scope-parent`
closure must include every node. Another Gate B transaction
changes one power item and one privacy item together so the one review request
must contain both disjoint impact chains and both singular purpose lineages.

The source fixture includes:

```text
revision-zero logical snapshot/package inputs
ordered transaction/repair scripts
expected structured results and logical hashes
agent-facing scenario goals that do not reveal the expected repair
```

See [testing_and_qa.md](testing_and_qa.md) for the normative source layout,
TestKit, end-to-end suite, ignored QA workspaces, and fixture reuse rules.

### 14.3 Intentional-error corpus

Include fixtures for:

- wrong application ID/user version/migration hash;
- SQLite foreign-key/integrity failure;
- logical hash mismatch caused by accidental/incomplete direct mutation;
- invalid node/edge type, property schema, endpoint type, or impact mode;
- unavailable required validator;
- duplicate/invalid entity ID;
- missing/duplicate project purpose, multiple scope parents, disconnected scope,
  and scope-parent cycle;
- type/kind mismatch;
- invalid scalar-property cardinality/value;
- missing or incompatible edge endpoint;
- omitted or reversed semantic dependency;
- explicit contradiction;
- missing support/definition/implementation/verification;
- forbidden cycle;
- unreviewed or stale impacted node;
- stale project/draft/entity precondition;
- impact/context bound reached;
- unrelated cross-track/distractor false-positive regression;
- writer conflict and injected commit failure;
- relational/logical graph mapping mismatch;
- replay mismatch;
- uncovered extension/profile data.

Every fixture has golden structured diagnostics. Golden comparisons exclude
injected timestamps but assert deterministic semantic properties and ordering.

### 14.4 Performance corpus

Generate deterministic sparse and dense fixtures:

```text
small:       1,000 nodes / 10,000 edges
expected:   10,000 nodes / 100,000 edges
stress:    100,000 nodes / 1,000,000 edges
```

Measure database load, logical materialization, validation, direct queries,
impact traversal, snapshot generation, and accepted commit. Record hardware and
budgets; do not encode an unsupported universal performance claim.

### 14.5 Gate A success criteria

Gate A passes only if:

1. Clean checkout restores, builds, and tests.
2. The locally generated sample database integrity-checks and
   logical-hash-verifies.
3. Database constraints reject structural corruption in supported write paths.
4. Semantic validators catch cases foreign keys cannot.
5. Current-change impact exactly matches the expected set and paths.
6. Pending dispositions block commit.
7. Invalid/stale/busy/faulted commits change no authoritative/audit rows.
8. Accepted commit yields expected rows and logical hash.
9. Replay reproduces recorded hashes.
10. Read-view and C# dependency-arc expansion agree.
11. Accumulated WP3-WP8 agent walkthroughs complete realistic goals through
    public commands without source knowledge or direct canonical SQL.
12. A fresh lower-cost-agent evaluation succeeds over the full workflow using
    JSON plus documented read-only SQL views.
13. Deterministic defects found during agent QA have replayable regression tests.
14. Modeling burden, usability friction, agent confidence, and performance are
    documented.
15. Comparison with Doorstop/plain SQLite identifies material additional value.

If useful impact requires near-complete connectivity or the metamodel adds no
value over ordinary SQL, Gate A fails and the feasibility verdict is updated.

## 15. Later evidence gates

### 15.1 AI semantic review — Gate B

Implement the one-request OpenAI review design in Section 13 and
`docs/ai_semantic_review.md`. Evaluate it on the TechnicalProject known-issue
corpus before giving it narrative-specific prompts. If it does not add enough
value to justify its privacy, cost, and configuration surface, omit Gate B rather
than adding more providers or import/export machinery.

### 15.2 AI-first authoring and intake — Gate C

Implement the strict tool-using workflow in Section 13.5 and
`docs/ai_authoring_agent.md`. Evaluate creation from description/text/image and
existing-project changes on known TechnicalProject/menu fixtures. The agent must
materially reduce graph-entry burden without direct writes, unrelated changes,
automatic review disposition, or unconfirmed commit. If it does not, omit the
built-in orchestrator while retaining the deterministic tool contracts.

### 15.3 MCP/plugin packaging — Gate D

After Application tools and Gate C workflows are stable, expose the same bounded
tools through a headless MCP server and add workflow skills. Package them using
the then-current OpenAI plugin format. Custom UI is optional and must not be
required for model operation. No provider or packaging type enters Core or
canonical state.

### 15.4 Linear narrative — Gate E

Add package types/validators for story events, fictional intervals, participants,
effects, belief/knowledge states, clues, and disclosure. No new physical tables
are required unless measurement proves the generic property/binary-edge representation
inadequate.

Keep project revision, fictional time, narrative order, canon truth, and
perspective separate.

### 15.5 Interactive state — Gate F

Add finite typed variables, expression AST records, transition effects,
invariants, and traces. Bounded BFS explores canonical state encodings and
returns shortest counterexamples. Reaching state/depth limits is inconclusive.

### 15.6 Hosted service — optional Gate G

Only demonstrated multiple-writer/remote requirements authorize:

- ASP.NET API host;
- authentication/authorization and tenant isolation;
- PostgreSQL persistence implementation;
- server-side job/cancellation/observability concerns.

The Application protocol and logical snapshot/hash stay backend-neutral.
PostgreSQL migrations and concurrency semantics require their own acceptance
suite; do not assume SQLite SQL is portable.

## 16. Test strategy

### 16.1 Layers

```text
Core unit/property tests
  IDs, packages, node/edge types, values, entities, operations, batch expansion

Serialization unit/property tests
  strict JSON, canonical order, logical hash, operation/result schemas

Validation unit/property tests
  schema validation, graph indexes, dependency arcs, constraints, impact,
  obligations, context

Application integration tests with in-memory ports
  draft lifecycle, projection, stale checks, policy, commit orchestration, replay

SQLite integration/fault tests
  migrations, PRAGMAs, constraints, mapping, transactions, rollback, backup,
  views, mutation detection, busy behavior

CLI contract tests
  JSON input/output, stdout/stderr, exit codes, deterministic results

Sample/golden/performance tests
  TechnicalProject and generated corpora
```

Tests should create databases in per-test temporary directories whenever
practical. A binary `.vw.db` may be checked in only under `tests/` when the test
requires a deliberately malformed or byte-specific SQLite artifact that cannot
reasonably be produced at test time. Such a fixture must document its purpose,
provenance, expected application/schema version, and regeneration procedure.

All normal fixture databases are created by ValidatedWorld through public
Application/CLI paths from retained scenario assets. Tests and QA may not depend
on an external SQLite tool, server, Docker container, system provider, or raw
canonical SQL setup. Shared fixture/process helpers live in
`tests/ValidatedWorld.TestKit`; black-box CLI cases live in
`tests/ValidatedWorld.EndToEnd.Tests`.

Every WP0-WP8 acceptance criterion must be executable by an agent in a clean
checkout without human inspection, secrets, interactive UI, or a mutable remote
service. Tests control clocks, IDs, random seeds, scheduling/fault points, and
environment-dependent limits. Scripted scenarios assert structured results and
exit codes. Skipped, flaky, manually inspected, or tautological tests are not
completion evidence.

The later Gate B suite preserves this property with a fake semantic-review
client and scripted `HttpMessageHandler` responses. Live OpenAI evaluation
is a separate explicitly enabled product experiment, never a default unit,
integration, end-to-end, or completion test. Absence of credentials must produce
a deterministic structured configuration result without attempting a network
call.

When a required behavior lacks an automated oracle, creating that oracle is part
of the work package. If no reliable oracle can be built, the behavior remains
inconclusive and the package cannot be marked complete merely because the code
appears plausible.

### 16.2 Required properties

- Database → logical snapshot → canonical JSON is deterministic.
- Canonical JSON → new database → logical snapshot is equivalent.
- Physical insertion order does not change logical hash.
- SQLite file bytes may differ while logical hashes remain equal.
- Stable IDs and revisions survive round trip.
- Every edge endpoint resolves to a node of an allowed type.
- Exactly one purpose exists and every non-root node has one acyclic
  parent path to it.
- Read-view direct edges equal C# extracted edges.
- Unrelated insertion does not change an existing impact set/path.
- A leaf change includes its purpose lineage as context without selecting
  siblings; a directly changed intermediate scope node selects its descendants;
  a directly changed purpose root selects all descendants.
- Base/projected union never loses base-only/projected-only impact.
- Any required bounded-out analysis is inconclusive.
- Operation changes invalidate stale dispositions.
- Failed commits preserve identical logical head, current rows, and audit rows.
- Accepted commit and replay reproduce the same logical hash.
- Scalar properties and extension JSON never create hidden references.
- Artifact locators are never dereferenced.
- No CLI command emits non-JSON stdout.
- Search, scope navigation, neighbor queries, pagination, and cursors are
  deterministic and bound to the queried project revision/hash.
- Gate B's preview contains the complete transaction and every disjoint selected
  chain in one request; each run invokes the live client at most once.
- Focus/cluster batch expansion is deterministic, always returns explicit
  operations, and never invents a non-scope semantic edge.

### 16.3 Fault injection

Inject failures after draft load, base verification, projection, validation,
writer acquisition, head recheck, entity header mutation, property mutation,
edge-endpoint mutation, audit insert, hash verification, head update, and before
SQLite commit. Every failure before commit must roll back the entire SQL
transaction.

### 16.4 Realistic end-to-end and agent usability testing

Deterministic tests are necessary but do not establish usability. Every work
package exercises the largest TechnicalProject scenario its layer supports. WP1
constructs it through Core APIs; WP2 serializes it; WP3 materializes the first
real database and public read workflow. WP3-WP8 each add a replayable scripted
end-to-end scenario and an actual AI-agent black-box walkthrough.

The QA agent receives:

- the built CLI and its public README/help;
- a temporary database generated by the app through the documented sample or
  initialization command;
- a realistic user goal, such as changing retention policy or diagnosing why a
  privacy claim cannot be accepted; and
- no expected command sequence, private repository API, or permission to mutate
  canonical tables directly.

It records:

```text
work package and build identity
scenario goal and supplied artifacts
commands/public views used
completion or exact stopping point
semantic result and unrelated-record exclusions
confusing terminology, missing feedback, unnecessary steps, and mistakes
confidence in the outcome and recommended changes
```

Keep the concise report at `docs/qa/wpN-agent-walkthrough.md`; do not check in
model chain-of-thought or an unbounded transcript. Preserve commands/inputs and
structured expected outputs in the scripted scenario so the workflow is
replayable without another model call.

If the agent cannot complete a documented workflow, requires source knowledge,
misreads success, misses a required semantic consequence, or changes unrelated
data, the work package fails. Add a regression test for deterministic defects,
repair the interface/diagnostic, and rerun. Record lesser friction and concrete
recommendations. A finding that challenges product usefulness or modeling cost
is reported to the human before the plan advances.

## 17. Ordered work packages

Implement one package at a time. Do not add later-host dependencies early.
[implementation_execution_plan.md](implementation_execution_plan.md) records the
completed work, the one Current task, and remaining roadmap order.

On success, the agent appends evidence and fully specifies the next task. If a
work package is too large for one local task, the Current task may first be a
bounded slice with explicit acceptance criteria; the agent still does only that
slice and stops.

Do not skip ahead because a later task looks easier. If repair attempts keep
producing or cycling through the same failure, leave Current task unchanged,
report the evidence to the human, and stop.

### WP0 — architecture scaffold

- Add `ValidatedWorld.Application` and tests.
- Add `ValidatedWorld.Persistence.Sqlite` and tests.
- Add explicit project references from Section 3.
- Reference `Microsoft.Data.Sqlite.Core` plus an explicitly pinned/audited native
  bundle directly in Persistence.Sqlite.
- Acceptance: scaffold and existing suite restore/build/test cleanly.

### WP1 — common graph domain

- Implement IDs, schema packages, node/edge/property definitions, scalar values,
  nodes, binary edges, snapshots (including `PurposeNodeId`), policies,
  operations, focused-batch expansion contracts, and review records.
- Construct a realistic interconnected technical-design scenario through public
  Core APIs with one purpose and sibling scope branches; report modeling friction.
- Acceptance: unit/property tests cover every local valid/rejected shape; a
  focused cluster batch expands to the expected explicit scope and semantic
  edges; the realistic graph requires no test-only escape hatch.

### WP2 — logical JSON and built-in packages

- Implement strict protocol DTOs, canonical writers/hashes, and canonical
  `core/v1` (including purpose/scope definitions) plus `technical-project/v1`
  package resources.
- Materialize the realistic TechnicalProject source corpus and representative
  edit/error variants as reviewed text fixtures.
- Acceptance: round-trip/order/duplicate/unknown-property/hash goldens pass and an
  agent can inspect/author the documented JSON without hidden conventions.

### WP3 — SQLite schema and mapping

- Implement connections, nine-table v1 migration, repositories, read views,
  integrity checks, logical snapshot load, initialize, and backup.
- Make initialization require purpose text and atomically create/store the sole
  purpose root.
- Add the first CLI walking skeleton from Section 12.5 and CLI test project.
- Add `ValidatedWorld.TestKit`, `ValidatedWorld.EndToEnd.Tests`, the bundled
  sample catalog, and `vw sample list/create`.
- Generate a real TechnicalProject `.vw.db` in a temporary directory and run the
  first black-box agent read/query/structural-verification walkthrough.
- Publish/run from a clean temporary directory and prove bundled SQLite startup,
  create/open/verify/backup without `sqlite3`, Docker, or system SQLite; record
  which host platforms were actually exercised.
- Acceptance: migration/constraint/mutation-detection/mapping/backup integration
  tests prove the compact schema plus entity/type/endpoint foreign keys;
  CLI/package smoke tests, reusable sample generation, and agent QA pass;
  unimplemented semantic phases are explicitly inconclusive.

### WP4 — indexes and semantic validation

- Validate stored schema packages/entities, build graph indexes/dependency arcs, implement generic
  and technical constraints, diagnostics, and coverage.
- Enforce exact-one purpose and singular acyclic root-reaching scope lineages.
- Expose full verify, diagnostics, dependencies/dependents, and explanation reads
  through the CLI walking skeleton.
- Acceptance: intentional-error fixtures match golden JSON, read views agree,
  and an agent can diagnose realistic missing-evidence/contradiction cases from
  public output.

### WP5 — durable drafts and projection

- Implement draft repository, operations, preconditions, projection, hashes,
  focus/batch expansion, validation runs, and disposition invalidation.
- Expose begin/show/focus/expand/apply/validate/abort through the CLI.
- Acceptance: concurrent draft edits and stale operation cases are deterministic,
  and an agent can author and repair a realistic proposed transaction without
  direct canonical SQL.

### WP6 — impact and mandatory review

- Implement base/projected union BFS, explanation paths, bounds, obligations, and
  fingerprints.
- Expose impact, obligations, and disposition through the CLI.
- Acceptance: TechnicalProject yields exactly Section 14 impact and unrelated
  exclusions; leaf context excludes siblings, an intermediate scope change
  reaches its descendants, and a direct purpose change reaches the project. An
  agent can explain and correctly disposition the soft-logic scenario without
  guessing hidden dependencies.

### WP7 — atomic accepted commit and replay

- Implement short SQLite write session, ordered row mutations, integrity/hash
  recheck, accepted audit, rollback fault handling, and replay.
- Expose commit get/verify/replay through the CLI.
- Acceptance: every fault rolls back, accepted replay matches, and agent QA
  demonstrates successful commit plus understandable recovery from a rejected or
  injected-failure scenario.

### WP8 — queries and CLI

- Complete remaining handlers, JSON envelopes, commands, exit codes, context,
  deterministic node search, scope/neighbor navigation, backup, limits, and
  help/read-view documentation; consolidate the incremental public surface from
  Section 12.5.
- Context queries include singular purpose lineages through upward-only traversal
  that never fans back down into sibling branches.
- Acceptance: scripted agent completes init through commit/replay using JSON and
  queries read-only views successfully, and a black-box agent completes both
  quantitative and soft-logic workflows from public documentation.

### WP9 — Gate A evaluation

- Run correctness, modeling-cost, lower-cost-agent, comparison, performance, and
  accumulated WP3-WP8 usability evaluations. Run the complete public workflow
  from a fresh QA-user perspective without relying on earlier implementation
  context.
- Acceptance: explicitly approve, narrow, replace components, or stop before
  narrative work.

### WP10 — AI semantic review (separate post-Gate-A plan)

- Authorized only by a successful Gate A outcome and a new human-requested
  planning task.
- Before any implementation, require the initiating human's exact
  `AI_REVIEW_SECRET_READY: yes` attestation; never search for, acquire, list, or
  install the key. Before a paid call also require the exact
  `AI_REVIEW_LIVE_CALL_AUTHORIZED: yes` attestation.
- Implement one whole-transaction request/coverage preview, structured concerns,
  freshness, dispositions, persistence, a fake/scripted test client, CLI, and the
  sole OpenAI `gpt-5.6-terra` production client outside the deterministic core.
  Include every disjoint selected chain and singular purpose lineage together;
  make one background response with a 1,200-second deadline and zero retries.
- Evaluate known omitted/stale TechnicalProject issues and scoped-versus-unscoped
  usefulness. Omit Gate B if the built-in call adds no material value.

### WP11 — AI-first authoring and intake (separate post-Gate-A plan)

- Authorized only by a successful Gate A outcome, an explicit Gate B decision
  (implemented or omitted), and a new human-requested planning task.
- Require the authoring secret/live-call attestations in
  `docs/ai_authoring_agent.md` before applicable implementation or evaluation.
- Implement strict Application tools, search/navigation use, durable sessions,
  text/image initialization proposals, reviewed `catalog/v1`, multi-turn draft
  repair, Gate B handoff, exact user confirmation, fake/scripted clients, and the
  sole OpenAI client.
- Evaluate TechnicalProject and restaurant-menu tasks; omit the built-in
  orchestrator if it does not reduce burden without weakening guarantees.

### WP12 — MCP/plugin packaging

- Authorized only after stable Gate C tool contracts. Expose those contracts
  through a headless MCP server, add workflow skills, and package them using the
  then-current OpenAI plugin format; custom UI remains optional.

### WP13 — LinearNarrative profile

- Authorized only by the preceding evidence gates.

### WP14 — InteractiveState profile

- Authorized only by the narrative gate outcome.

### WP15 — optional hosted-service gate

- Evaluate web/PostgreSQL only for demonstrated hosted requirements.

## 18. Implementation handoff checklist

Every implementation change states:

- work package/bounded slice;
- resulting next task on success;
- changed physical migration or logical JSON contract;
- schema package/validator version changes;
- tests and database/snapshot/golden changes;
- realistic scenario and actual agent-QA outcome, including usability findings
  when applicable;
- guarantees and remaining inconclusive behavior;
- exact assignment-specific and full restore/build/test results;
- any failed repair attempts that caused the agent to stop.

The change is incomplete if production code and tests are updated but
`implementation_execution_plan.md` still describes the old repository state.
The agent run is incomplete until it reports the result and next task to the
human and stops without beginning that next task.

After WP9, no later profile is automatically authorized. Record the Gate A
result, set Current task to `None`, report the available
complete/continue/narrow/pivot/stop choices, and ask the human what to do. Any
approved later phase begins with a separate planning task rather than continuing
automatically.

Never silently edit an applied migration. Add a new checked migration. During the
POC, breaking logical changes are allowed but must use an explicit new package or
protocol/schema migration.

## 19. Deferred decisions and non-goals

Defer until evidence requires them:

- user-authored schema packages;
- arbitrary project DDL or direct canonical SQL writes;
- universal ontology or unrestricted rule language;
- automatic canonical acceptance of semantic extraction;
- general-purpose AI agent/RAG orchestration beyond the scoped Gate B reviewer
  and Gate C authoring/intake workflow;
- document import/generation/rendering/publishing;
- custom diff/change-package protocol;
- incremental semantic validation;
- graph database, RDF/SHACL runtime, TerminusDB, or Dolt persistence;
- branch/merge/rebase collaboration;
- PostgreSQL/web/multi-tenant service;
- rich visual graph editor;
- game-engine runtime;
- dynamic extension loading;
- a rich visual graph editor or UI-dependent authoring flow.

The scaled-down product remains useful only if it beats ordinary SQLite plus
manual review: a deterministic semantic transaction layer that explains
transitive impact and refuses incomplete reviewed changes. If it cannot prove
that advantage, stop expanding and say so plainly.

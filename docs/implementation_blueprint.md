# ValidatedWorld Implementation Blueprint

**Status:** Coding-agent handoff

**Blueprint version:** 4.1

**Last reviewed:** 2026-08-12

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

After reading this blueprint, read
[implementation_execution_plan.md](implementation_execution_plan.md). The
blueprint is the normative design and ordered backlog. The execution plan is the
authoritative record of completed evidence and the only current assignment.
Agents update it in the same change that completes or blocks implementation
work; chat history is not project state.

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
10. Stable IDs, not labels, paths, names, or row IDs, are semantic references.
11. SQLite foreign keys establish structural integrity only. Dependency rules
    establish semantic impact.
12. Every graph-relevant reference is an explicit reference-valued field or
    relation endpoint; extension JSON is never scanned for IDs.
13. The dependency graph is derived and never authored as a second source of
    truth.
14. Impact uses the union of base and projected dependency graphs.
15. Accepted operations are the direct change record; no separate semantic diff
    is canonical.
16. Impact means “must be considered,” not “must be edited.”
17. Policy may require current dispositions for selected impacted objects.
18. Deterministic results are Proven, Disproven, or Inconclusive.
19. Remove never cascades implicitly. Physical foreign keys use Restrict/No
    Action, and application repair is explicit.
20. Gate A runs full logical validation; incremental semantic validation is
    deferred.
21. Gate A supports one writer at a time, matching SQLite's model.
22. No importer, renderer, RAG pipeline, AI provider, web host, or graph database
    is part of Gate A.

## 3. Solution architecture

### 3.1 Projects and dependencies

```text
ValidatedWorld.Core
  dependencies: none

ValidatedWorld.Serialization
  dependencies: Core

ValidatedWorld.Validation
  dependencies: Core

ValidatedWorld.Application
  dependencies: Core, Serialization, Validation

ValidatedWorld.Persistence.Sqlite
  dependencies: Core, Serialization, Application, Microsoft.Data.Sqlite.Core,
                explicitly pinned SQLitePCLRaw native bundle

ValidatedWorld.Cli
  dependencies: Application, Persistence.Sqlite

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
ValidatedWorld.Core.Objects
ValidatedWorld.Core.Constraints
ValidatedWorld.Core.Transactions
ValidatedWorld.Core.Reviews

ValidatedWorld.Serialization.Json
ValidatedWorld.Serialization.Canonical
ValidatedWorld.Serialization.Hashing

ValidatedWorld.Validation.Diagnostics
ValidatedWorld.Validation.Schema
ValidatedWorld.Validation.Indexes
ValidatedWorld.Validation.Dependencies
ValidatedWorld.Validation.Impact
ValidatedWorld.Validation.Rules
ValidatedWorld.Validation.Context

ValidatedWorld.Application.Projects
ValidatedWorld.Application.Transactions
ValidatedWorld.Application.Commits
ValidatedWorld.Application.Queries
ValidatedWorld.Application.Persistence

ValidatedWorld.Persistence.Sqlite.Connections
ValidatedWorld.Persistence.Sqlite.Migrations
ValidatedWorld.Persistence.Sqlite.Mapping
ValidatedWorld.Persistence.Sqlite.Repositories
ValidatedWorld.Persistence.Sqlite.Views
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
public readonly record struct ObjectId(string Value);
public readonly record struct TypeId(string Value);
public readonly record struct PackageId(string Value);
public readonly record struct TransactionId(string Value);
public readonly record struct CommitId(string Value);
```

Project/object/type/package IDs match:

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
    ImmutableArray<LogicalTypeDefinition> Types,
    ImmutableArray<RequiredValidator> RequiredValidators);

public enum LogicalObjectKind { Record, Relation, Constraint }

public sealed record LogicalTypeDefinition(
    TypeId Id,
    PackageId PackageId,
    string PackageVersion,
    LogicalObjectKind ObjectKind,
    string DisplayName,
    ImmutableArray<FieldDefinition> Fields,
    ImmutableArray<EndpointRoleDefinition> EndpointRoles,
    ImmutableArray<DependencyRuleDefinition> DependencyRules,
    ImmutableArray<string> Categories);
```

Only Relation types may declare endpoint roles/dependency rules. Constraint
kinds are ordinary logical types whose object kind is Constraint and whose
registered validators understand their fields. Type inheritance is deferred;
Gate A packages declare complete concrete type definitions.

### 4.3 Fields and values

```csharp
public enum FieldValueKind
{
    Text,
    Integer,
    Decimal,
    Boolean,
    Symbol,
    Instant,
    Reference
}

public enum ReferenceImpactMode
{
    None,
    OwnerDependsOnTarget,
    TargetDependsOnOwner,
    Bidirectional
}

public sealed record FieldDefinition(
    string Name,
    int Ordinal,
    FieldValueKind ValueKind,
    int MinimumCount,
    int? MaximumCount,
    bool IsOrderSignificant,
    ImmutableArray<TypeId> AllowedReferenceTypes,
    ReferenceImpactMode ReferenceImpact,
    FieldValueConstraints Constraints);

public abstract record ProjectValue
{
    public sealed record Text(string Value) : ProjectValue;
    public sealed record Integer(long Value) : ProjectValue;
    public sealed record Decimal(string CanonicalValue) : ProjectValue;
    public sealed record Boolean(bool Value) : ProjectValue;
    public sealed record Symbol(string Value) : ProjectValue;
    public sealed record Instant(DateTimeOffset Value) : ProjectValue;
    public sealed record Reference(ObjectId TargetId) : ProjectValue;
}
```

Decimal values are canonical base-10 strings; floating point is forbidden for
semantic values. Instants canonicalize to UTC ISO 8601 with seven fractional
digits. Repeated unordered field values sort by canonical value; ordered values
retain explicit ordinal.

### 4.4 Relation roles and dependency rules

```csharp
public sealed record EndpointRoleDefinition(
    string Name,
    int Ordinal,
    int MinimumCount,
    int? MaximumCount,
    ImmutableArray<TypeId> AllowedTypes);

public sealed record DependencyRuleDefinition(
    int Ordinal,
    string DependentRole,
    string DependencyRole,
    bool CreatesReviewImpact,
    string Meaning);
```

The two roles must exist on the relation type. At instance time, the cross
product of objects playing the roles yields dependency edges. If self-dependency
is nonsensical for a relation type, its validator rejects identical endpoints.

### 4.5 Objects

```csharp
public sealed record ProjectObject(
    ObjectId Id,
    int Revision,
    LogicalObjectKind Kind,
    TypeId TypeId,
    ImmutableSortedDictionary<string, ImmutableArray<ProjectValue>> Fields,
    ImmutableSortedDictionary<string, ImmutableArray<ObjectId>> Endpoints,
    ImmutableArray<string> Tags,
    ExtensionMap Extensions);

public sealed record ExtensionMap(
    ImmutableSortedDictionary<string, CanonicalJsonValue> Values);
```

Only Relation objects have endpoints. Constraint objects use registered closed
types/validators. Extension keys are namespace-qualified. Extension values are
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
    ProjectPolicy Policy,
    ImmutableArray<SchemaPackage> SchemaPackages,
    ImmutableArray<ProjectObject> Objects);
```

Revision 0 initialization uses a documented null parent/last-commit encoding.
Object arrays sort by ID. Package/type/field/role/rule arrays have the canonical
ordering defined in Section 7.

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
    head_revision           INTEGER NOT NULL CHECK(head_revision >= 0),
    parent_logical_hash     TEXT NULL,
    logical_hash            TEXT NOT NULL,
    last_commit_id          TEXT NULL,
    policy_json             TEXT NOT NULL CHECK(json_valid(policy_json)),
    created_at_utc          TEXT NOT NULL,
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
    PRIMARY KEY(package_id, package_version)
) STRICT;

CREATE TABLE schema_package_validators (
    package_id              TEXT NOT NULL,
    package_version         TEXT NOT NULL,
    validator_id            TEXT NOT NULL,
    validator_version       INTEGER NOT NULL CHECK(validator_version >= 1),
    PRIMARY KEY(package_id, package_version, validator_id),
    FOREIGN KEY(package_id, package_version)
        REFERENCES schema_packages(package_id, package_version)
        ON DELETE RESTRICT
) STRICT;

CREATE TABLE project_packages (
    project_id              TEXT NOT NULL,
    package_id              TEXT NOT NULL,
    package_version         TEXT NOT NULL,
    PRIMARY KEY(project_id, package_id),
    FOREIGN KEY(project_id) REFERENCES projects(project_id) ON DELETE RESTRICT,
    FOREIGN KEY(package_id, package_version)
        REFERENCES schema_packages(package_id, package_version)
        ON DELETE RESTRICT
) STRICT;
```

Gate A permits exactly one row in `projects`; initialization rejects a second.

### 5.3 Logical type definitions

```sql
CREATE TABLE logical_types (
    type_id                 TEXT PRIMARY KEY,
    package_id              TEXT NOT NULL,
    package_version         TEXT NOT NULL,
    object_kind             TEXT NOT NULL
        CHECK(object_kind IN ('record', 'relation', 'constraint')),
    display_name            TEXT NOT NULL CHECK(length(display_name) > 0),
    FOREIGN KEY(package_id, package_version)
        REFERENCES schema_packages(package_id, package_version)
        ON DELETE RESTRICT
) STRICT;

CREATE TABLE logical_type_categories (
    type_id                 TEXT NOT NULL,
    category                TEXT NOT NULL,
    PRIMARY KEY(type_id, category),
    FOREIGN KEY(type_id) REFERENCES logical_types(type_id) ON DELETE RESTRICT
) STRICT;

CREATE TABLE field_definitions (
    type_id                 TEXT NOT NULL,
    field_name              TEXT NOT NULL,
    field_ordinal           INTEGER NOT NULL CHECK(field_ordinal >= 0),
    value_kind              TEXT NOT NULL CHECK(value_kind IN
        ('text','integer','decimal','boolean','symbol','instant','reference')),
    minimum_count           INTEGER NOT NULL CHECK(minimum_count >= 0),
    maximum_count           INTEGER NULL CHECK(maximum_count IS NULL OR
                                                maximum_count >= minimum_count),
    is_order_significant    INTEGER NOT NULL CHECK(is_order_significant IN (0,1)),
    reference_impact        TEXT NOT NULL CHECK(reference_impact IN
        ('none','owner-depends-on-target','target-depends-on-owner',
         'bidirectional')),
    constraints_json        TEXT NOT NULL CHECK(json_valid(constraints_json)),
    PRIMARY KEY(type_id, field_name),
    UNIQUE(type_id, field_ordinal),
    FOREIGN KEY(type_id) REFERENCES logical_types(type_id) ON DELETE RESTRICT,
    CHECK(value_kind = 'reference' OR reference_impact = 'none')
) STRICT;

CREATE TABLE field_allowed_target_types (
    owner_type_id           TEXT NOT NULL,
    field_name              TEXT NOT NULL,
    target_type_id          TEXT NOT NULL,
    PRIMARY KEY(owner_type_id, field_name, target_type_id),
    FOREIGN KEY(owner_type_id, field_name)
        REFERENCES field_definitions(type_id, field_name) ON DELETE RESTRICT,
    FOREIGN KEY(target_type_id) REFERENCES logical_types(type_id)
        ON DELETE RESTRICT
) STRICT;

CREATE TABLE relation_role_definitions (
    relation_type_id        TEXT NOT NULL,
    role_name               TEXT NOT NULL,
    role_ordinal            INTEGER NOT NULL CHECK(role_ordinal >= 0),
    minimum_count           INTEGER NOT NULL CHECK(minimum_count >= 0),
    maximum_count           INTEGER NULL CHECK(maximum_count IS NULL OR
                                                maximum_count >= minimum_count),
    PRIMARY KEY(relation_type_id, role_name),
    UNIQUE(relation_type_id, role_ordinal),
    FOREIGN KEY(relation_type_id) REFERENCES logical_types(type_id)
        ON DELETE RESTRICT
) STRICT;

CREATE TABLE relation_role_allowed_types (
    relation_type_id        TEXT NOT NULL,
    role_name               TEXT NOT NULL,
    allowed_type_id         TEXT NOT NULL,
    PRIMARY KEY(relation_type_id, role_name, allowed_type_id),
    FOREIGN KEY(relation_type_id, role_name)
        REFERENCES relation_role_definitions(relation_type_id, role_name)
        ON DELETE RESTRICT,
    FOREIGN KEY(allowed_type_id) REFERENCES logical_types(type_id)
        ON DELETE RESTRICT
) STRICT;

CREATE TABLE dependency_rules (
    relation_type_id        TEXT NOT NULL,
    rule_ordinal            INTEGER NOT NULL CHECK(rule_ordinal >= 0),
    dependent_role          TEXT NOT NULL,
    dependency_role         TEXT NOT NULL,
    creates_review_impact   INTEGER NOT NULL
        CHECK(creates_review_impact IN (0,1)),
    meaning                 TEXT NOT NULL CHECK(length(meaning) > 0),
    PRIMARY KEY(relation_type_id, rule_ordinal),
    FOREIGN KEY(relation_type_id, dependent_role)
        REFERENCES relation_role_definitions(relation_type_id, role_name)
        ON DELETE RESTRICT,
    FOREIGN KEY(relation_type_id, dependency_role)
        REFERENCES relation_role_definitions(relation_type_id, role_name)
        ON DELETE RESTRICT
) STRICT;
```

Cross-row rules—such as Relation types only declaring roles—are validated when
packages load and again from stored rows. Do not encode opaque business behavior
in triggers.

### 5.4 Current logical objects

```sql
CREATE TABLE graph_objects (
    object_id               TEXT PRIMARY KEY,
    object_revision         INTEGER NOT NULL CHECK(object_revision >= 1),
    object_kind             TEXT NOT NULL
        CHECK(object_kind IN ('record', 'relation', 'constraint')),
    logical_type_id         TEXT NOT NULL,
    extensions_json         TEXT NOT NULL CHECK(json_valid(extensions_json)),
    UNIQUE(object_id, logical_type_id),
    FOREIGN KEY(logical_type_id) REFERENCES logical_types(type_id)
        ON DELETE RESTRICT
) STRICT;

CREATE TABLE object_tags (
    object_id               TEXT NOT NULL,
    tag                     TEXT NOT NULL,
    PRIMARY KEY(object_id, tag),
    FOREIGN KEY(object_id) REFERENCES graph_objects(object_id)
        ON DELETE RESTRICT
) STRICT;

CREATE TABLE object_field_values (
    object_id               TEXT NOT NULL,
    logical_type_id         TEXT NOT NULL,
    field_name              TEXT NOT NULL,
    value_ordinal           INTEGER NOT NULL CHECK(value_ordinal >= 0),
    value_kind              TEXT NOT NULL CHECK(value_kind IN
        ('text','integer','decimal','boolean','symbol','instant','reference')),
    text_value              TEXT NULL,
    integer_value           INTEGER NULL,
    boolean_value           INTEGER NULL CHECK(boolean_value IN (0,1)),
    reference_object_id     TEXT NULL,
    PRIMARY KEY(object_id, field_name, value_ordinal),
    FOREIGN KEY(object_id, logical_type_id)
        REFERENCES graph_objects(object_id, logical_type_id)
        ON DELETE RESTRICT,
    FOREIGN KEY(logical_type_id, field_name)
        REFERENCES field_definitions(type_id, field_name)
        ON DELETE RESTRICT,
    FOREIGN KEY(reference_object_id) REFERENCES graph_objects(object_id)
        ON DELETE RESTRICT,
    CHECK(
      (value_kind IN ('text','decimal','symbol','instant') AND
       text_value IS NOT NULL AND integer_value IS NULL AND
       boolean_value IS NULL AND reference_object_id IS NULL) OR
      (value_kind = 'integer' AND integer_value IS NOT NULL AND
       text_value IS NULL AND boolean_value IS NULL AND
       reference_object_id IS NULL) OR
      (value_kind = 'boolean' AND boolean_value IS NOT NULL AND
       text_value IS NULL AND integer_value IS NULL AND
       reference_object_id IS NULL) OR
      (value_kind = 'reference' AND reference_object_id IS NOT NULL AND
       text_value IS NULL AND integer_value IS NULL AND boolean_value IS NULL)
    )
) STRICT;

CREATE TABLE relation_endpoints (
    relation_id             TEXT NOT NULL,
    relation_type_id        TEXT NOT NULL,
    role_name               TEXT NOT NULL,
    endpoint_ordinal        INTEGER NOT NULL CHECK(endpoint_ordinal >= 0),
    target_object_id        TEXT NOT NULL,
    PRIMARY KEY(relation_id, role_name, endpoint_ordinal),
    FOREIGN KEY(relation_id, relation_type_id)
        REFERENCES graph_objects(object_id, logical_type_id)
        ON DELETE RESTRICT,
    FOREIGN KEY(relation_type_id, role_name)
        REFERENCES relation_role_definitions(relation_type_id, role_name)
        ON DELETE RESTRICT,
    FOREIGN KEY(target_object_id) REFERENCES graph_objects(object_id)
        ON DELETE RESTRICT
) STRICT;

CREATE INDEX ix_graph_objects_type
    ON graph_objects(logical_type_id, object_id);
CREATE INDEX ix_field_reference_target
    ON object_field_values(reference_object_id)
    WHERE reference_object_id IS NOT NULL;
CREATE INDEX ix_relation_endpoint_target
    ON relation_endpoints(target_object_id, relation_type_id, role_name);
CREATE INDEX ix_relation_endpoint_role
    ON relation_endpoints(relation_type_id, role_name, relation_id);
```

Application validation checks that object kind equals its type's kind, stored
value kind equals field definition, reference targets match allowed exact types,
cardinalities hold, and endpoint targets match role types.

### 5.5 Draft transaction tables

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
    status                  TEXT NOT NULL CHECK(status IN
        ('draft','review-required','ready','rejected','committed','aborted')),
    change_set_hash         TEXT NULL,
    projected_logical_hash  TEXT NULL,
    draft_revision          INTEGER NOT NULL CHECK(draft_revision >= 1),
    FOREIGN KEY(project_id) REFERENCES projects(project_id) ON DELETE RESTRICT
) STRICT;

CREATE TABLE draft_operations (
    transaction_id          TEXT NOT NULL,
    target_object_id        TEXT NOT NULL,
    operation_kind          TEXT NOT NULL CHECK(operation_kind IN
        ('add','replace','remove')),
    expected_object_revision INTEGER NULL,
    object_json             TEXT NULL CHECK(object_json IS NULL OR
                                             json_valid(object_json)),
    PRIMARY KEY(transaction_id, target_object_id),
    FOREIGN KEY(transaction_id) REFERENCES draft_transactions(transaction_id)
        ON DELETE RESTRICT,
    CHECK(
      (operation_kind = 'add' AND expected_object_revision IS NULL AND
       object_json IS NOT NULL) OR
      (operation_kind = 'replace' AND
       expected_object_revision IS NOT NULL AND
       expected_object_revision >= 1 AND
       object_json IS NOT NULL) OR
      (operation_kind = 'remove' AND
       expected_object_revision IS NOT NULL AND
       expected_object_revision >= 1 AND
       object_json IS NULL)
    )
) STRICT;

CREATE TABLE submitted_dispositions (
    transaction_id          TEXT NOT NULL,
    target_object_id        TEXT NOT NULL,
    disposition             TEXT NOT NULL CHECK(disposition IN
        ('reviewed-no-change','not-applicable')),
    target_fingerprint      TEXT NOT NULL,
    impact_fingerprint      TEXT NOT NULL,
    reviewer_id             TEXT NOT NULL,
    rationale               TEXT NOT NULL CHECK(length(rationale) > 0),
    dispositioned_at_utc    TEXT NOT NULL,
    PRIMARY KEY(transaction_id, target_object_id),
    FOREIGN KEY(transaction_id) REFERENCES draft_transactions(transaction_id)
        ON DELETE RESTRICT
) STRICT;
```

Application draft edits increment `draft_revision`, recompute hashes, and delete
or invalidate dispositions whose fingerprints no longer match.

### 5.6 Commit and diagnostic tables

```sql
CREATE TABLE commits (
    commit_id               TEXT PRIMARY KEY,
    project_id              TEXT NOT NULL,
    project_revision        INTEGER NOT NULL UNIQUE CHECK(project_revision >= 1),
    parent_logical_hash     TEXT NOT NULL,
    logical_hash            TEXT NOT NULL UNIQUE,
    change_set_hash         TEXT NOT NULL,
    impact_hash             TEXT NOT NULL,
    validation_hash         TEXT NOT NULL,
    author                  TEXT NOT NULL,
    intent                  TEXT NOT NULL,
    committed_at_utc        TEXT NOT NULL,
    FOREIGN KEY(project_id) REFERENCES projects(project_id) ON DELETE RESTRICT
) STRICT;

CREATE TABLE commit_operations (
    commit_id               TEXT NOT NULL,
    target_object_id        TEXT NOT NULL,
    operation_kind          TEXT NOT NULL CHECK(operation_kind IN
        ('add','replace','remove')),
    expected_object_revision INTEGER NULL,
    object_json             TEXT NULL CHECK(object_json IS NULL OR
                                             json_valid(object_json)),
    PRIMARY KEY(commit_id, target_object_id),
    FOREIGN KEY(commit_id) REFERENCES commits(commit_id) ON DELETE RESTRICT,
    CHECK(
      (operation_kind = 'add' AND expected_object_revision IS NULL AND
       object_json IS NOT NULL) OR
      (operation_kind = 'replace' AND
       expected_object_revision IS NOT NULL AND
       expected_object_revision >= 1 AND
       object_json IS NOT NULL) OR
      (operation_kind = 'remove' AND
       expected_object_revision IS NOT NULL AND
       expected_object_revision >= 1 AND
       object_json IS NULL)
    )
) STRICT;

CREATE TABLE accepted_dispositions (
    commit_id               TEXT NOT NULL,
    target_object_id        TEXT NOT NULL,
    disposition             TEXT NOT NULL CHECK(disposition IN
        ('updated','reviewed-no-change','not-applicable')),
    target_fingerprint      TEXT NOT NULL,
    impact_fingerprint      TEXT NOT NULL,
    reviewer_id             TEXT NULL,
    rationale               TEXT NULL,
    dispositioned_at_utc    TEXT NOT NULL,
    explanation_path_json  TEXT NOT NULL CHECK(json_valid(explanation_path_json)),
    PRIMARY KEY(commit_id, target_object_id),
    FOREIGN KEY(commit_id) REFERENCES commits(commit_id) ON DELETE RESTRICT
) STRICT;

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

CREATE TABLE diagnostics (
    validation_run_id       TEXT NOT NULL,
    diagnostic_ordinal      INTEGER NOT NULL CHECK(diagnostic_ordinal >= 0),
    diagnostic_code         TEXT NOT NULL,
    severity                TEXT NOT NULL,
    primary_object_id       TEXT NULL,
    fingerprint             TEXT NOT NULL,
    diagnostic_json         TEXT NOT NULL CHECK(json_valid(diagnostic_json)),
    PRIMARY KEY(validation_run_id, diagnostic_ordinal),
    FOREIGN KEY(validation_run_id)
        REFERENCES validation_runs(validation_run_id) ON DELETE RESTRICT
) STRICT;
```

The accepted commit and project-head update occur in the same SQLite transaction
as current-state row changes. Draft cleanup may occur in the same commit or a
later maintenance transaction; a committed draft status is not authoritative.

### 5.7 Read views

Provide stable documented views:

```text
vw_project_head
vw_schema_packages
vw_logical_types
vw_objects
vw_object_fields
vw_relations
vw_relation_endpoints
vw_direct_dependencies
vw_commits
vw_diagnostics
```

`vw_direct_dependencies` returns:

```text
dependent_object_id
dependency_object_id
evidence_kind       field-reference | relation-rule
evidence_id         object/field or relation/rule
creates_review_impact
```

It is the union of reference-impact declarations and relation dependency rules.
It is a convenience/read surface, not stored canon. Application edge extraction
and the view must be tested against the same golden edge set.

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

Never interpolate user values, identifiers, field names, or type names into SQL.
All runtime data uses parameters. Migration SQL is a checked embedded resource.

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
objects. Gate A does not invent a second journal or file-replacement protocol.

### 6.4 Untrusted databases

Treat project databases as untrusted:

- refuse unknown application IDs/migrations;
- do not enable loadable extensions;
- do not execute SQL stored in project data;
- cap file size/page count, logical object/value counts, JSON lengths, graph
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
    "policy": {}
  },
  "schemaPackages": [],
  "objects": []
}
```

Each package contains complete type/field/role/dependency definitions. Each
object contains `id`, `revision`, `kind`, `typeId`, `fields`, `endpoints`, `tags`,
and `extensions`.

### 7.2 Ordering

- packages by `(packageId, semanticVersionOrdinal)`;
- types by type ID;
- fields/roles/rules by declared ordinal then ID/name;
- objects by object ID;
- field and endpoint names ordinally;
- ordered values/endpoints by ordinal;
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
                               provisional next head fields)
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

## 8. Indexes and dependency graph

### 8.1 Project index

```csharp
public sealed class ProjectIndex
{
    public ProjectSnapshot Snapshot { get; }
    public ImmutableDictionary<ObjectId, ProjectObject> ObjectsById { get; }
    public ImmutableDictionary<TypeId, LogicalTypeDefinition> TypesById { get; }
    public ImmutableDictionary<TypeId, ImmutableArray<ProjectObject>> ObjectsByType { get; }
    public ImmutableDictionary<ObjectId, ImmutableArray<ProjectObject>> RelationsByEndpoint { get; }
}
```

Duplicate/ambiguous rows are integrity failures, never last-writer-wins.

### 8.2 Dependency edges

```csharp
public enum DependencyEvidenceKind
{
    FieldReference,
    RelationRule,
    TypeDefinition,
    ConstraintReference,
    ProfileDerived
}

public sealed record DependencyEdge(
    ObjectId DependentId,
    ObjectId DependencyId,
    DependencyEvidenceKind EvidenceKind,
    string EvidenceId,
    bool CreatesReviewImpact);
```

Extract with explicit visitors:

```text
for every reference field value:
  apply its ReferenceImpactMode

for every relation instance and dependency rule:
  dependentPlayers = endpoints[DependentRole]
  dependencyPlayers = endpoints[DependencyRole]
  emit sorted cross-product edges with relation/rule evidence

for every registered constraint/profile visitor:
  emit only declared referenced-object edges
```

Do not reflect over strings or JSON looking for IDs. Keep all evidence edges even
when dependent/dependency pairs repeat; traversal may deduplicate destination
objects while retaining sorted evidence.

### 8.3 Impact algorithm

```text
ComputeImpact(changedIds, baseGraph, projectedGraph, policy):
    reverseUnion = review-impact reverse edges from base and projected graphs
    visited = changedIds
    queue = changedIds sorted ordinal at depth 0
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

        if visited count > MaximumImpactObjects:
            return Inconclusive with partial evidence

    return Complete(changed, impacted excluding seeds, shortest paths)
```

Use breadth-first traversal. Tie between predecessor nodes resolves by object ID;
retain all sorted evidence for the selected hop. Base-only edges preserve impact
from removed dependencies; projected-only edges include new dependencies.

## 9. Schema and object validation

### 9.1 Stored schema package validation

Validate:

1. Package ID/version/hash and required validators.
2. Unique type IDs and valid object kinds.
3. Unique field/role ordinals and names within each complete type definition.
4. Field kinds/cardinalities/constraints.
5. Reference target types and impact modes.
6. Relation-only role and dependency definitions.
7. Dependency rules reference existing roles.
8. Exact registered package definition hash matches stored definitions.

A mismatch between built-in code and stored built-in schema is an unsupported or
inconsistently modified project, not an opportunity to guess. This check detects
accidental/incomplete mutation, not a privileged attacker who rewrites data,
hashes, and history together.

### 9.2 Object validation

For every object:

1. Validate ID and revision.
2. Resolve type and require matching object kind.
3. Load the type's complete declared fields/roles.
4. Reject unknown fields/endpoints outside namespaced extensions.
5. Check cardinality, order, canonical values, symbols/ranges/patterns.
6. Resolve references and allowed target types.
7. Validate relation roles/endpoints and allowed types.
8. Ensure every reference and endpoint agrees with normalized database rows.
9. Report uncovered extension namespaces.

### 9.3 Generic constraints

Initial validators include:

- selected explicit contradictions;
- minimum support relationships/sources;
- Tarjan SCC detection for selected dependency relation types;
- unique active definitions;
- implementation and verification coverage;
- required impact dispositions.

These prove explicit graph properties, not prose or real-world truth.

## 10. Transactions, review, and commit

### 10.1 Operations

```csharp
public abstract record ProjectOperation
{
    public required ObjectId TargetId { get; init; }
    public sealed record Add(ProjectObjectDraft Object) : ProjectOperation;
    public sealed record Replace(int ExpectedRevision, ProjectObjectDraft Object)
        : ProjectOperation;
    public sealed record Remove(int ExpectedRevision) : ProjectOperation;
}
```

One final operation per target. Editing a draft replaces that operation and
increments draft revision. The operation document cannot set committed revision.

### 10.2 Projection

```text
Apply(base, transaction):
    require project ID/base revision/base hash match
    copy current objects to ID-keyed builders

    for operation sorted by target ID:
        Add: require absent; materialize revision 1
        Replace: require current expected revision; preserve object ID;
                 materialize expected revision + 1
        Remove: require current expected revision; remove target only

    materialize sorted immutable snapshot with provisional next head
    validate every post-operation reference; never cascade
```

Changing a relation's type is remove-plus-add and is disallowed under one-target
operation uniqueness in Gate A; use a new stable ID or an explicit future
migration workflow. Changing a record's logical type is rejected in Replace.

### 10.3 Review fingerprints

```text
TargetFingerprint = SHA-256(target ID + projected revision + canonical object)
ImpactFingerprint = SHA-256(ChangeSetHash + target ID + ordered shortest path)
```

Direct policy-selected changes receive `updated`. Other selected impacts require
submitted `reviewed-no-change` or `not-applicable` with reviewer, rationale, time,
and exact fingerprints. Inconclusive required impact blocks commit.

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
    reject unless commit policy accepts

    begin short SQLite IMMEDIATE write transaction
    re-read project head and draft revision/change-set hash
    reject/rollback if either changed
    verify submitted dispositions still match computed evidence
    normalize and apply every accepted object mutation
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

1. Upsert/add target object headers needed by new references.
2. Remove/rewrite relation endpoints and field values for changed objects.
3. Insert new field values/endpoints/tags.
4. Delete removed object headers only after all referencing rows were explicitly
   repaired/removed by operations.
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
VW11xx  schema packages, types, fields, roles, validators
VW12xx  objects, values, references, endpoints
VW13xx  profile assertions and lifecycle
VW14xx  dependency extraction and relationship semantics
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
VW1102 invalid logical type hierarchy
VW1103 invalid field definition
VW1104 invalid role/dependency rule
VW1105 required validator unavailable
VW1201 invalid/duplicate object ID
VW1202 object kind/type mismatch
VW1203 invalid/missing field value
VW1204 missing or incompatible reference
VW1205 invalid relation endpoint/cardinality
VW1301 accepted assertion contradiction
VW1401 dependency extraction mismatch
VW1402 invalid semantic relation instance
VW1501 missing required support
VW1502 forbidden dependency cycle
VW1503 missing implementation
VW1504 missing verification
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

GetObject
ListObjects
GetRelation
ListRelations
GetDependencies
GetDependents
ExplainDependencyPath
BuildContextQuery

BeginTransaction
GetTransaction
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

Expected invalid input returns `OperationResult<T>`, not exceptions. Every read
result includes project revision/hash. Every draft result includes transaction
ID, base identity, draft revision, and change-set/projected hashes as applicable.

### 12.2 CLI commands

```text
vw init --db <path> --project-id <id> --title <text>
vw verify --db <path> [--full]
vw status --db <path>
vw snapshot write --db <path> --output <path-or-stdout>
vw snapshot init --input <revision-zero-path-or-stdin> --db <new-path>
vw backup --db <path> --output <new-path>

vw object get --db <path> --id <id>
vw object list --db <path> [--type <id>] [--tag <value>]
vw relation list --db <path> [--type <id>] [--endpoint <id>]
vw dependencies --db <path> --id <id> [--transitive]
vw dependents --db <path> --id <id> [--transitive]
vw explain path --db <path> --from <id> --to <id>
vw context --db <path> --seed <id> [--max-objects <n>]

vw tx begin --db <path> --intent <text> --author <text>
vw tx show --db <path> --tx <id>
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

There is no arbitrary `vw sql` write command. Users may open a verified database
read-only with standard SQLite tools and use documented `vw_*` views.

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
4  stale head/object/draft precondition or writer conflict
5  database integrity, migration, mapping, or write failure
6  required analysis inconclusive
7  required review dispositions pending
8  unsupported schema/package/validator or migration required
9  internal failure
```

## 13. Deterministic context queries

A context query selects logical JSON for a human, AI, or RAG consumer. It does
not generate content.

```csharp
public sealed record ContextQueryResult(
    string SchemaVersion,
    ProjectId ProjectId,
    long ProjectRevision,
    string LogicalHash,
    ImmutableArray<ObjectId> SeedIds,
    ImmutableArray<ProjectObject> Objects,
    ImmutableArray<DependencyEdge> Edges,
    ImmutableArray<ImpactStep> SelectionPaths,
    ImmutableArray<ObjectId> OmittedObjectIds,
    bool Truncated,
    string ResultHash);
```

Priority:

```text
0 seed objects
1 exact type definitions and applicable constraints
2 forward dependencies needed to understand seeds
3 reverse impacted dependents and paths
4 external anchors bound through relations
5 additional related objects by increasing graph distance
```

Within priority/distance sort by ID. Include each object atomically. Report
limits and omissions. A required seed/type/constraint that cannot fit produces
inconclusive output.

## 14. Gate A package and sample

### 14.1 Built-in packages

Embed canonical package JSON resources for:

- `core/v1`: artifact, anchor, containment, binds/uses/mentions, and generic
  constraint vocabulary;
- `technical-project/v1`: subject, proposition, assertion, source, and technical
  dependency/traceability vocabulary.

Initialization validates the resources, computes their definition hashes, and
normalizes them into schema tables. The same resources generate documentation
goldens. Do not duplicate package definitions as C# enum switches and SQL seed
scripts; one canonical resource plus registered validator identifiers is the
source.

### 14.2 TechnicalProject fixture

`samples/TechnicalProject/project.vw.db` is a disposable, ignored build artifact
generated deterministically from a checked-in revision-zero snapshot and
initialization/transaction scripts. Never commit or hand-edit its binary bytes.
Check in the canonical `project.snapshot.json`, scripts, expected result goldens,
and fixture-building command instead.

The graph contains:

```text
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
bindings from semantic objects to relevant anchors
```

Changing current to 25 mA must impact the result, conclusion, and relevant
anchors, not privacy. The engine does not perform arithmetic; fixture operations
supply the known repaired capacity/runtime values.

### 14.3 Intentional-error corpus

Include fixtures for:

- wrong application ID/user version/migration hash;
- SQLite foreign-key/integrity failure;
- logical hash mismatch caused by accidental/incomplete direct mutation;
- invalid type hierarchy, field, role, or dependency rule;
- unavailable required validator;
- duplicate/invalid object ID;
- type/kind mismatch;
- invalid field cardinality/value/reference;
- invalid relation endpoint/cardinality;
- omitted or reversed semantic dependency;
- explicit contradiction;
- missing support/definition/implementation/verification;
- forbidden cycle;
- unreviewed or stale impacted object;
- stale project/draft/object precondition;
- impact/context bound reached;
- unrelated privacy false-positive regression;
- writer conflict and injected commit failure;
- relational/logical mapping mismatch;
- replay mismatch;
- uncovered extension/profile data.

Every fixture has golden structured diagnostics. Golden comparisons exclude
injected timestamps but assert deterministic semantic fields and ordering.

### 14.4 Performance corpus

Generate deterministic sparse and dense fixtures:

```text
small:       1,000 objects / 10,000 edges
expected:   10,000 objects / 100,000 edges
stress:    100,000 objects / 1,000,000 edges
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
10. Read views and C# edge extraction agree.
11. Lower-cost agent evaluation succeeds using JSON plus read-only SQL views.
12. Modeling burden and performance are documented.
13. Comparison with Doorstop/plain SQLite identifies material additional value.

If useful impact requires near-complete connectivity or the metamodel adds no
value over ordinary SQL, Gate A fails and the feasibility verdict is updated.

## 15. Later profiles and hosts

### 15.1 Linear narrative — Gate B

Add package types/validators for story events, fictional intervals, participants,
effects, belief/knowledge states, clues, and disclosure. No new physical tables
are required unless measurement proves the generic field/endpoint representation
inadequate.

Keep project revision, fictional time, narrative order, canon truth, and
perspective separate.

### 15.2 Interactive state — Gate C

Add finite typed variables, expression AST records, transition effects,
invariants, and traces. Bounded BFS explores canonical state encodings and
returns shortest counterexamples. Reaching state/depth limits is inconclusive.

### 15.3 Hosted service — optional Gate D

Only demonstrated multiple-writer/remote requirements authorize:

- ASP.NET API host;
- authentication/authorization and tenant isolation;
- PostgreSQL persistence implementation;
- server-side job/cancellation/observability concerns.

The Application protocol and logical snapshot/hash stay backend-neutral.
PostgreSQL migrations and concurrency semantics require their own acceptance
suite; do not assume SQLite SQL is portable.

### 15.4 Integration packaging — optional Gate E

After protocol stability, expose Application use cases through MCP/Codex/plugin
packaging. No provider or packaging type enters Core or canonical state.

## 16. Test strategy

### 16.1 Layers

```text
Core unit/property tests
  IDs, packages, types, fields, roles, values, objects, operations

Serialization unit/property tests
  strict JSON, canonical order, logical hash, operation/result schemas

Validation unit/property tests
  schema validation, indexes, edges, constraints, impact, obligations, context

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

Every WP0-WP8 acceptance criterion must be executable by an agent in a clean
checkout without human inspection, secrets, interactive UI, or a mutable remote
service. Tests control clocks, IDs, random seeds, scheduling/fault points, and
environment-dependent limits. Scripted scenarios assert structured results and
exit codes. Skipped, flaky, manually inspected, or tautological tests are not
completion evidence.

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
- Every normalized reference/endpoint resolves.
- Read-view direct edges equal C# extracted edges.
- Unrelated insertion does not change an existing impact set/path.
- Base/projected union never loses base-only/projected-only impact.
- Any required bounded-out analysis is inconclusive.
- Operation changes invalidate stale dispositions.
- Failed commits preserve identical logical head, current rows, and audit rows.
- Accepted commit and replay reproduce the same logical hash.
- Extension JSON never creates hidden references.
- Artifact locators are never dereferenced.
- No CLI command emits non-JSON stdout.

### 16.3 Fault injection

Inject failures after draft load, base verification, projection, validation,
writer acquisition, head recheck, object header mutation, value mutation,
endpoint mutation, audit insert, hash verification, head update, and before
SQLite commit. Every failure before commit must roll back the entire SQL
transaction.

## 17. Ordered work packages

Implement one package at a time. Do not add later-host dependencies early.
[implementation_execution_plan.md](implementation_execution_plan.md) records the
one current package/slice, its status, evidence, and exact next-agent assignment.

An agent marks the current entry `in-progress` before coding and `complete` only
after assignment-specific checks plus full restore/build/test pass. The same
change must append evidence and define the next authorized assignment. If a work
package is too large for one change, first record ordered slices with explicit
boundaries and acceptance criteria; only the first slice becomes current.

Do not skip ahead because a later task looks easier. If the same blocker survives
three materially different repairs or repair attempts cycle to an earlier
failure, follow the execution plan's failure-loop rule: record the evidence,
mark the item blocked, stop, and ask one focused human question.

### WP0 — architecture scaffold

- Add `ValidatedWorld.Application` and tests.
- Add `ValidatedWorld.Persistence.Sqlite` and tests.
- Add explicit project references from Section 3.
- Reference `Microsoft.Data.Sqlite.Core` plus an explicitly pinned/audited native
  bundle directly in Persistence.Sqlite.
- Acceptance: scaffold and existing suite restore/build/test cleanly.

### WP1 — common metamodel

- Implement IDs, schema packages, type/field/role/dependency definitions, values,
  objects, snapshots, policies, operations, and review records.
- Acceptance: unit/property tests cover every local valid/rejected shape.

### WP2 — logical JSON and built-in packages

- Implement strict protocol DTOs, canonical writers/hashes, and canonical
  `core/v1` plus `technical-project/v1` package resources.
- Acceptance: round-trip/order/duplicate/unknown-field/hash goldens pass.

### WP3 — SQLite schema and mapping

- Implement connections, v1 migration, repositories, read views, integrity
  checks, logical snapshot load, initialize, and backup.
- Acceptance: migration/constraint/mutation-detection/mapping/backup integration
  tests pass.

### WP4 — indexes and semantic validation

- Validate stored schema packages/objects, build indexes/edges, implement generic
  and technical constraints, diagnostics, and coverage.
- Acceptance: intentional-error fixtures match golden JSON; read views agree.

### WP5 — durable drafts and projection

- Implement draft repository, operations, preconditions, projection, hashes,
  validation runs, and disposition invalidation.
- Acceptance: concurrent draft edits and stale operation cases are deterministic.

### WP6 — impact and mandatory review

- Implement base/projected union BFS, explanation paths, bounds, obligations, and
  fingerprints.
- Acceptance: TechnicalProject yields exactly Section 14 impact and no privacy
  false positive.

### WP7 — atomic accepted commit and replay

- Implement short SQLite write session, ordered row mutations, integrity/hash
  recheck, accepted audit, rollback fault handling, and replay.
- Acceptance: every fault rolls back; accepted replay matches.

### WP8 — queries and CLI

- Implement handlers, JSON envelopes, commands, exit codes, context, snapshot,
  backup, and help/read-view documentation.
- Acceptance: scripted agent completes init through commit/replay using JSON and
  queries read-only views successfully.

### WP9 — Gate A evaluation

- Run correctness, modeling-cost, lower-cost-agent, comparison, and performance
  evaluations.
- Acceptance: explicitly approve, narrow, replace components, or stop before
  narrative work.

### WP10 — LinearNarrative profile

- Authorized only by Gate A outcome.

### WP11 — InteractiveState profile

- Authorized only by Gate B outcome.

### WP12 — optional host/integration gates

- Evaluate MCP packaging first; web/PostgreSQL only for demonstrated hosted
  requirements.

## 18. Pull-request checklist

Every implementation change states:

- work package/bounded slice;
- execution-plan status transition and exact next assignment;
- changed physical migration or logical JSON contract;
- schema package/validator version changes;
- tests and database/snapshot/golden changes;
- guarantees and remaining inconclusive behavior;
- exact assignment-specific and full restore/build/test results;
- any failure-loop attempts or blocker decision.

The change is incomplete if production code and tests are updated but
`implementation_execution_plan.md` still describes the old repository state.

Never silently edit an applied migration. Add a new checked migration. During the
POC, breaking logical changes are allowed but must use an explicit new package or
protocol/schema migration.

## 19. Deferred decisions and non-goals

Defer until evidence requires them:

- user-authored schema packages;
- arbitrary project DDL or direct canonical SQL writes;
- universal ontology or unrestricted rule language;
- automatic semantic extraction;
- built-in AI/provider/RAG orchestration;
- document import/generation/rendering/publishing;
- custom diff/change-package protocol;
- incremental semantic validation;
- graph database, RDF/SHACL runtime, TerminusDB, or Dolt persistence;
- branch/merge/rebase collaboration;
- PostgreSQL/web/multi-tenant service;
- rich visual graph editor;
- game-engine runtime;
- dynamic extension loading;
- public plugin packaging.

The scaled-down product remains useful only if it beats ordinary SQLite plus
manual review: a deterministic semantic transaction layer that explains
transitive impact and refuses incomplete reviewed changes. If it cannot prove
that advantage, stop expanding and say so plainly.

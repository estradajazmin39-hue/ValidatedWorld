# ValidatedWorld Implementation Blueprint

**Status:** Coding-agent handoff

**Blueprint version:** 2.0

**Last reviewed:** 2026-08-11

**Target:** .NET 10 / C#

**POC schema:** `validatedworld/v1`

## 1. Purpose and reading order

This document specifies the common document/claim graph implementation and the
order in which coding agents should build it.

Read first:

1. [feasibility.md](feasibility.md)
2. [validated_world_authoring_spec.md](validated_world_authoring_spec.md)
3. This blueprint

Pseudocode is normative about observable behavior, not exact local syntax. If a
coding agent discovers that an invariant or serialized contract is wrong, update
the controlling documents in the same change.

Implement one work package at a time. Every package ends with:

```powershell
dotnet build ValidatedWorld.slnx
dotnet test ValidatedWorld.slnx
```

## 2. Non-negotiable invariants

1. Core has no file, JSON, console, network, model-provider, UI, database,
   word-processor, or game-engine dependency.
2. The common core models authored content and semantic records; fiction is a
   profile, not the base type system.
3. A canonical snapshot is immutable during validation.
4. Every canonical edit occurs through a transaction.
5. A failed/stale commit leaves canonical bytes unchanged.
6. Stable IDs, never headings/display names, are references.
7. Missing information is unknown unless a profile declares a finite
   closed-world rule.
8. The operational dependency graph is derived from records and semantic links;
   it is never a second authored source of truth.
9. Impact means “must be considered,” not “must be edited.”
10. Project policy may require every impacted record to have a current review
    disposition before commit.
11. Deterministic findings are Proven/Disproven/Inconclusive. Heuristic findings
    are Concerns even when policy makes their resolution mandatory.
12. AI-extracted claims/links never become canon without transaction operations.
13. Diagnostic order, fingerprints, canonical serialization, and context
    selection are deterministic.
14. Delete operations never cascade implicitly.
15. The common POC performs full validation at commit; incremental validation is
    deferred.
16. Tests never require a network connection or paid model.
17. Narrative time and perspective are not implemented before Gate A succeeds;
    state exploration is not implemented before Gate C succeeds.

## 3. Solution architecture

### 3.1 Project dependencies

Add `ValidatedWorld.Application` during WP0.

```text
ValidatedWorld.Core
  dependencies: none

ValidatedWorld.Serialization
  dependencies: Core

ValidatedWorld.Validation
  dependencies: Core

ValidatedWorld.Application
  dependencies: Core, Serialization, Validation

ValidatedWorld.Generation
  dependencies: Core, Validation
  purpose: deterministic context packets and provider-neutral reviews

ValidatedWorld.Export
  dependencies: Core, Validation

ValidatedWorld.Cli
  dependencies: Application, Generation, Export

ValidatedWorld.Mcp (post-gate)
  dependencies: Application, Generation, Export
```

Validation never references Generation or Export. Application owns canonical
mutation; Generation and Export are optional composed services.

### 3.2 Namespace responsibilities

```text
ValidatedWorld.Core.Identifiers
ValidatedWorld.Core.Values
ValidatedWorld.Core.Projects
ValidatedWorld.Core.Content
ValidatedWorld.Core.Claims
ValidatedWorld.Core.Links
ValidatedWorld.Core.Constraints
ValidatedWorld.Core.Transactions
ValidatedWorld.Core.Reviews

ValidatedWorld.Serialization.Json
ValidatedWorld.Serialization.Workspaces
ValidatedWorld.Serialization.Migrations

ValidatedWorld.Validation.Diagnostics
ValidatedWorld.Validation.Indexes
ValidatedWorld.Validation.Dependencies
ValidatedWorld.Validation.Rules
ValidatedWorld.Validation.Reviews

ValidatedWorld.Application.Transactions
ValidatedWorld.Application.Commits
ValidatedWorld.Application.Queries

ValidatedWorld.Generation.Context
ValidatedWorld.Generation.Review

ValidatedWorld.Export.Documents
ValidatedWorld.Export.Reports
```

### 3.3 Environmental abstractions

Inject:

- `IClock`
- `ITransactionIdGenerator`
- `IWorldStore` (retain “World” in public product naming if desired)
- `ITransactionStore`
- `IWorkspaceLock`
- `IAtomicFileWriter`
- heuristic review providers

Tests use fixed clocks/IDs and in-memory or fault-injecting stores.

## 4. Common domain model

Use sealed immutable records and immutable collections at public boundaries.
Core contains no `JsonElement` or serialization attributes.

### 4.1 IDs

```csharp
public readonly record struct CanonicalId(string Value);
public readonly record struct ProjectId(string Value);
public readonly record struct TransactionId(string Value);
```

Canonical IDs match:

```regex
^[a-z][a-z0-9-]*:[a-z][a-z0-9-]*(/[a-z][a-z0-9-]*)*$
```

Examples:

```text
artifact:sensor-design
section:power-budget
subject:average-current
predicate:has-value
proposition:average-current
assertion:average-current-assumption
source:bench-measurement-07
link:runtime-derived-from-current
constraint:requirements-have-tests
```

Compare with `StringComparer.Ordinal`. Reject noncanonical input; never trim,
case-fold, or Unicode-normalize IDs.

Transaction IDs are `tx:` plus lower-case UUIDv7. Tests inject a generator.

### 4.2 Common record contract

```csharp
public interface IProjectRecord
{
    CanonicalId Id { get; }
    int Revision { get; }
}
```

Record revision starts at 1 and increments on committed replacement. It is
separate from project revision.

All addressable record IDs are globally unique across collections.

### 4.3 Neutral values

```csharp
public abstract record ProjectValue
{
    public sealed record RecordRef(CanonicalId RecordId) : ProjectValue;
    public sealed record Text(string Value) : ProjectValue;
    public sealed record Integer(long Value) : ProjectValue;
    public sealed record Decimal(decimal Value) : ProjectValue;
    public sealed record Boolean(bool Value) : ProjectValue;
    public sealed record Symbol(string Value) : ProjectValue;
}
```

JSON values are tagged. Decimal values serialize as invariant strings. Avoid
floating point in canonical semantic values.

Namespaced extension payloads use a recursively immutable neutral value tree.
Unknown extension namespaces are preserved and reported as uncovered, not
silently interpreted.

### 4.4 Project snapshot

```csharp
public sealed record ProjectSnapshot(
    ProjectHeader Header,
    ImmutableArray<ArtifactDefinition> Artifacts,
    ImmutableArray<ContentUnit> ContentUnits,
    ImmutableArray<SubjectDefinition> Subjects,
    ImmutableArray<PredicateDefinition> Predicates,
    ImmutableArray<PropositionDefinition> Propositions,
    ImmutableArray<AssertionRecord> Assertions,
    ImmutableArray<SourceRecord> Sources,
    ImmutableArray<ContentBinding> ContentBindings,
    ImmutableArray<SemanticLink> SemanticLinks,
    ImmutableArray<ProjectConstraint> Constraints,
    ImmutableArray<SemanticReviewAttestation> SemanticReviews,
    ImmutableArray<ProfileRecord> ProfileRecords);
```

`ProjectHeader` contains:

- `SchemaVersion` (`validatedworld/v1`)
- `ProjectId`
- `Title`
- `ProjectRevision` (`long`, starts at 0)
- `ParentContentHash` (null only at revision 0)
- `ContentHash`
- `LastCommit`
- `ProjectPolicy`
- enabled profile IDs/versions

Arrays exist for stable serialization. Validation builds indexes; Core does not
store redundant inverse collections.

### 4.5 Artifacts

```csharp
public sealed record ArtifactDefinition(
    CanonicalId Id,
    int Revision,
    string Kind,
    string Title,
    string Format,
    ImmutableArray<string> Tags,
    ExtensionMap Extensions) : IProjectRecord;
```

POC built-in kinds: `document`, `design`, `specification`, `whitepaper`. Format is
`plain-text` or `markdown` for v1. The canonical snapshot stores content units;
format controls deterministic reconstruction.

### 4.6 Content units

```csharp
public sealed record ContentUnit(
    CanonicalId Id,
    int Revision,
    CanonicalId ArtifactId,
    CanonicalId? ParentUnitId,
    string Kind,
    long Sequence,
    string? Heading,
    string Text,
    string TextHash,
    ImmutableArray<string> Tags,
    ExtensionMap Extensions) : IProjectRecord;
```

Built-in kinds: `section`, `paragraph`, `figure-caption`, `table`, `equation`,
`requirement-block`, and `note`.

Rules:

- Parent belongs to the same artifact.
- Parent relationships are acyclic.
- Sibling sequence values are unique. Gaps are allowed.
- `TextHash` is SHA-256 over canonical UTF-8 text with `\n` newlines.
- Loader recomputes and verifies `TextHash`.
- Changing heading, text, artifact, or parent is a record replacement.

`Sequence` is presentation order, not semantic dependency.

### 4.7 Subjects

```csharp
public sealed record SubjectDefinition(
    CanonicalId Id,
    int Revision,
    string Kind,
    string DisplayName,
    ImmutableArray<string> Aliases,
    string? Description,
    ImmutableArray<string> Tags,
    ExtensionMap Extensions) : IProjectRecord;
```

Initial kinds include `term`, `quantity`, `component`, `interface`, `method`,
`dataset`, `requirement-target`, `organization`, and custom kinds. Narrative
profiles later add characters, locations, events, and clues without changing the
base record.

### 4.8 Predicates and propositions

```csharp
public sealed record PredicateDefinition(
    CanonicalId Id,
    int Revision,
    ImmutableArray<PredicateArgument> Arguments,
    bool Symmetric,
    WorldAssumption Assumption,
    ExtensionMap Extensions) : IProjectRecord;

public sealed record PredicateArgument(
    string Role,
    ValueTypeDefinition Type,
    ImmutableArray<string> AllowedRecordKinds);

public sealed record PropositionDefinition(
    CanonicalId Id,
    int Revision,
    CanonicalId PredicateId,
    ImmutableArray<NamedArgument> Arguments,
    string Gloss) : IProjectRecord;
```

Arguments are keyed by role and serialize in predicate-declared order. Missing,
extra, duplicate, or type-incompatible arguments are errors.

The POC does not implement theorem proving. Propositions provide exact identity
for contradiction, support, links, bindings, and review.

### 4.9 Assertions

```csharp
public enum Polarity { Positive, Negative }

public enum AssertionRole
{
    Fact,
    Assumption,
    Hypothesis,
    Requirement,
    Observation,
    Result,
    Conclusion,
    Decision,
    Recommendation,
    Definition
}

public enum AssertionStatus
{
    Proposed,
    Accepted,
    Rejected,
    Deprecated,
    Superseded
}

public sealed record AssertionRecord(
    CanonicalId Id,
    int Revision,
    CanonicalId PropositionId,
    Polarity Polarity,
    AssertionRole Role,
    AssertionStatus Status,
    string Scope,
    ImmutableArray<CanonicalId> SourceIds,
    string? Rationale,
    ExtensionMap Extensions) : IProjectRecord;
```

`Scope` is a lower-kebab symbol. Common v1 uses `global`; profiles may define
additional scope compatibility rules. Unknown profile scopes are uncovered and
cannot participate in deterministic contradiction proof.

By default only Accepted assertions are authoritative for contradiction and
constraint checks. Policy may include Proposed assertions as warnings.

### 4.10 Sources and evidence

```csharp
public sealed record SourceRecord(
    CanonicalId Id,
    int Revision,
    string Kind,
    string Title,
    string? Locator,
    string? Uri,
    string? Version,
    string? ContentHash,
    string? Notes,
    ExtensionMap Extensions) : IProjectRecord;
```

Kinds include `citation`, `document`, `dataset`, `measurement`, `experiment`,
`calculation`, `test-result`, `code-artifact`, and `design-artifact`.

The POC validates shape, stable version/hash references, and required links. It
does not fetch URIs or judge source credibility.

### 4.11 Content bindings

```csharp
public enum BindingRole
{
    Asserts,
    Defines,
    Uses,
    Discusses,
    PresentsEvidence,
    Implements,
    Verifies
}

public sealed record ContentBinding(
    CanonicalId Id,
    int Revision,
    CanonicalId ContentUnitId,
    CanonicalId SemanticRecordId,
    BindingRole Role,
    string? Rationale) : IProjectRecord;
```

Bindings are exact semantic references. `SemanticRecordId` may target a subject,
proposition, assertion, source, constraint, or allowed profile record. Endpoint
rules depend on `BindingRole`.

Bindings create dependency edges from the content unit to the semantic record.
For `Asserts`, `Defines`, or `PresentsEvidence`, impact is bidirectional review:
changing the prose may stale the semantic record, and changing the semantic
record impacts the prose.

### 4.12 Semantic links

```csharp
public enum SemanticLinkKind
{
    DependsOn,
    DerivedFrom,
    Supports,
    Contradicts,
    Refines,
    Supersedes,
    Defines,
    Uses,
    Implements,
    Satisfies,
    Verifies,
    Cites,
    Mentions
}

public enum LinkProvenanceKind
{
    Manual,
    Imported,
    AiProposedConfirmed,
    DeterministicRule
}

public sealed record LinkProvenance(
    LinkProvenanceKind Kind,
    string? ProviderOrRuleId,
    string? ProposalHash);

public sealed record SemanticLink(
    CanonicalId Id,
    int Revision,
    CanonicalId SourceId,
    CanonicalId TargetId,
    SemanticLinkKind Kind,
    string Rationale,
    LinkProvenance Provenance,
    ExtensionMap Extensions) : IProjectRecord;
```

The link catalog is closed in schema v1. Endpoint and dependency behavior:

| Kind | Allowed source → target | Derived dependency edge |
|---|---|---|
| `DependsOn` | any semantic/content record → any record | source → target |
| `DerivedFrom` | assertion/proposition → assertion/proposition/source | source → target |
| `Supports` | source/assertion → assertion | target → source |
| `Contradicts` | assertion/proposition ↔ assertion/proposition | both directions |
| `Refines` | assertion/subject/content → same broad category | source → target |
| `Supersedes` | same broad category → same broad category | both directions |
| `Defines` | definition assertion/content → subject | users of subject depend on definition source |
| `Uses` | content/assertion → subject/definition/source | source → target |
| `Implements` | subject/content/assertion → requirement assertion | source → target |
| `Satisfies` | assertion/proposition → requirement assertion | source → target |
| `Verifies` | source/content/assertion → requirement assertion | source → target |
| `Cites` | content/assertion → source | source → target |
| `Mentions` | any → any | none by default |

`Defines` adds the direct source → subject edge. The index also derives a
definition dependency from every `Uses(X, subject)` source X to each accepted
definition source for that subject. Ambiguous definitions produce a diagnostic
instead of choosing one.

Semantic links are intentionally authored meaning. The adjacency lists derived
from them are not canonical records.

### 4.13 Constraints

Schema v1 uses a closed union:

```csharp
public abstract record ProjectConstraint : IProjectRecord
{
    public required CanonicalId Id { get; init; }
    public required int Revision { get; init; }
    public required DiagnosticSeverity FailureSeverity { get; init; }
    public required string Rationale { get; init; }

    public sealed record NoAcceptedContradictions(...) : ProjectConstraint;
    public sealed record RequireSupportForRoles(...) : ProjectConstraint;
    public sealed record AcyclicLinkKinds(...) : ProjectConstraint;
    public sealed record UniqueDefinitions(...) : ProjectConstraint;
    public sealed record RequireRequirementCoverage(...) : ProjectConstraint;
    public sealed record RequireSemanticReviewForContentKinds(...) : ProjectConstraint;
    public sealed record RequireImpactDispositions(...) : ProjectConstraint;
    public sealed record RequireReviewProfileForChanges(...) : ProjectConstraint;
}
```

Case payloads:

- `NoAcceptedContradictions`: included assertion roles/scopes.
- `RequireSupportForRoles`: roles, minimum link count, accepted support link
  kinds, and whether SourceIds count.
- `AcyclicLinkKinds`: set of link kinds whose induced directed graph must be
  acyclic.
- `UniqueDefinitions`: covered subject kinds and scope compatibility policy.
- `RequireRequirementCoverage`: minimum `Implements` and `Verifies` links per
  Accepted Requirement assertion.
- `RequireSemanticReviewForContentKinds`: content kinds/tags requiring a current
  attestation.
- `RequireImpactDispositions`: impacted record kinds/categories that block while
  pending, maximum impact depth if intentionally bounded, and allowed
  dispositions.
- `RequireReviewProfileForChanges`: profile ID/version and triggering changed
  record kinds/tags.

Do not add a general expression language in Gate A.

### 4.14 Semantic review attestations

```csharp
public sealed record SemanticReviewAttestation(
    CanonicalId Id,
    int Revision,
    CanonicalId ContentUnitId,
    string ReviewedTextHash,
    ImmutableArray<ReviewedBindingFingerprint> ReviewedBindings,
    string ReviewerId,
    DateTimeOffset ReviewedAt,
    string Rationale) : IProjectRecord;

public sealed record ReviewedBindingFingerprint(
    CanonicalId BindingId,
    int BindingRevision,
    string CanonicalBindingHash);
```

An attestation says that a reviewer compared a specific content hash with the
listed current bindings. It does not prove the prose correct. It is current only
when:

- `ReviewedTextHash == ContentUnit.TextHash`;
- every listed binding still exists at the recorded revision and has the
  recorded canonical hash; and
- policy-required current bindings are included.

### 4.15 Profile records

```csharp
public sealed record ProfileRecord(
    CanonicalId Id,
    int Revision,
    string ProfileId,
    string ProfileSchemaVersion,
    NeutralMap Data) : IProjectRecord;
```

Gate A allows profile records to round-trip but reports them uncovered unless a
registered validator understands the profile/schema. The common model must not
accumulate speculative narrative fields before Gate C.

## 5. Review workflow model

Review obligations and review runs belong to a draft transaction and its
accepted receipt, not the canonical project. A `SemanticReviewAttestation` is a
separate durable canonical record when project policy requires one. Concern and
diagnostic dispositions remain audit data in the receipt/report.

### 5.1 Review obligations

```csharp
public enum ReviewDisposition
{
    Pending,
    Updated,
    ReviewedNoChange,
    NotApplicable
}

public sealed record ImpactStep(
    CanonicalId DependentId,
    CanonicalId DependencyId,
    string EdgeKind,
    string FieldPathOrLinkId);

public sealed record ReviewObligation(
    CanonicalId TargetId,
    ReviewDisposition Disposition,
    ImmutableArray<ImpactStep> ExplanationPath,
    string TargetFingerprint,
    string ImpactFingerprint,
    string? ReviewerId,
    string? Rationale,
    DateTimeOffset? DispositionedAt);
```

Rules:

- Directly changed records are `Updated` automatically.
- Other policy-selected impacts begin `Pending`.
- `ReviewedNoChange` and `NotApplicable` require reviewer, rationale, and time.
- A disposition is valid only for its projected target and impact fingerprints.
- Changing any operation recomputes the projected snapshot/impact and invalidates
  dispositions whose fingerprints differ.
- A record outside policy may appear in impact output without becoming a blocking
  obligation.

### 5.2 Review runs and concerns

```csharp
public sealed record ReviewRun(
    CanonicalId Id,
    string ProfileId,
    string ProfileVersion,
    string BaseProjectHash,
    string ChangeSetHash,
    string ProjectedStateHash,
    string ContextPacketHash,
    ReviewerDescriptor Reviewer,
    ReviewRunStatus Status,
    ImmutableArray<ReviewConcern> Concerns,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record ReviewConcern(
    string ConcernId,
    string Code,
    string Message,
    ImmutableArray<CanonicalId> CitedRecordIds,
    ConcernDisposition Disposition,
    string? ResolutionRationale,
    string Fingerprint);
```

Concern dispositions: `Open`, `ResolvedByChange`, `RejectedWithRationale`,
`Acknowledged`. Policy decides which are acceptable.

Review completion and concern disposition are deterministically enforceable.
Concern correctness is not.

## 6. Canonical JSON

### 6.1 Top-level shape

```json
{
  "schemaVersion": "validatedworld/v1",
  "project": {
    "id": "project:offline-sensor",
    "title": "Offline Sensor Technical Design",
    "revision": 3,
    "parentContentHash": "sha256:...",
    "contentHash": "sha256:...",
    "profiles": ["technical-document/v1"],
    "lastCommit": {
      "transactionId": "tx:00000000-0000-7000-8000-000000000001",
      "intent": "Revise average-current assumption",
      "committedAt": "2026-08-10T20:00:00.0000000Z",
      "author": "agent"
    },
    "policy": {
      "blockOnRequiredInconclusive": true,
      "maximumDependencyDepth": 1000,
      "maximumImpactRecords": 100000
    }
  },
  "artifacts": [],
  "contentUnits": [],
  "subjects": [],
  "predicates": [],
  "propositions": [],
  "assertions": [],
  "sources": [],
  "contentBindings": [],
  "semanticLinks": [],
  "constraints": [],
  "semanticReviews": [],
  "profileRecords": []
}
```

All arrays are required. Canonical serialization sorts addressable record arrays
by ID. Ordered values retain semantic order; set-like values sort ordinally.
Duplicates are errors, never silently deduplicated.

### 6.2 Representative content/claim/link

```json
{
  "id": "section:power-budget",
  "revision": 2,
  "artifactId": "artifact:sensor-design",
  "parentUnitId": null,
  "kind": "section",
  "sequence": 300,
  "heading": "Power budget",
  "text": "At an average draw of 20 mA, the 500 mAh battery provides 25 hours.",
  "textHash": "sha256:...",
  "tags": ["power"],
  "extensions": {}
}
```

```json
{
  "id": "assertion:average-current-assumption",
  "revision": 1,
  "propositionId": "proposition:average-current",
  "polarity": "positive",
  "role": "assumption",
  "status": "accepted",
  "scope": "global",
  "sourceIds": ["source:bench-measurement-07"],
  "rationale": "Representative active-mode duty cycle",
  "extensions": {}
}
```

```json
{
  "id": "link:runtime-derived-from-current",
  "revision": 1,
  "sourceId": "assertion:runtime-estimate",
  "targetId": "assertion:average-current-assumption",
  "kind": "derived-from",
  "rationale": "Runtime estimate uses the average-current assumption.",
  "provenance": {
    "kind": "manual",
    "providerOrRuleId": null,
    "proposalHash": null
  },
  "extensions": {}
}
```

### 6.3 Canonical hashing

```text
projection = complete canonical snapshot with project.contentHash omitted
bytes = canonical UTF-8 without BOM and with \n newlines
hash = SHA-256(bytes), lower-case hex prefixed "sha256:"
```

The projection includes project revision, parent hash, commit metadata, policy,
content text/hashes, and all semantic records. Loader recomputes and compares.

Transaction-time hashes are distinct to avoid a circular dependency on commit
metadata:

```text
ChangeSetHash = SHA-256(canonical transaction schema version, transaction ID,
                        project ID, base revision/hash, and ID-sorted operations)

ProjectedStateHash = SHA-256(canonical projected header fields except
                             ContentHash and LastCommit, plus every projected
                             semantic record)
```

Review dispositions, review runs, reports, acknowledgements, timestamps, and
other mutable workflow results are excluded from `ChangeSetHash`. Changing an
operation changes both hashes. Final commit serialization adds `LastCommit` and
computes the canonical project `ContentHash`; review evidence stays bound to the
change set and projected state it actually evaluated.

### 6.4 Strict reader

Before DTO deserialization, scan with `Utf8JsonReader` and an ordinal property set
per object to reject duplicate properties. Then deserialize with:

- case-sensitive names;
- no comments/trailing commas;
- strict numbers;
- explicit union converters;
- unmapped-member rejection;
- depth and input-byte limits.

Record JSON Pointer and byte offset for addressable records/important fields.
User input failures return diagnostics rather than escaping serializer exceptions.

### 6.5 Content text canonicalization

On input:

1. Require valid Unicode text.
2. Convert CRLF/CR to LF for canonical storage.
3. Preserve all other characters and trailing spaces; content semantics may care.
4. Compute `TextHash` over UTF-8 bytes exactly as stored.

Do not trim or reflow authored text in canonical serialization.

## 7. Indexes and operational dependency graph

### 7.1 Project index

Build a `ProjectIndex` once for each loaded/projected snapshot:

```csharp
public sealed class ProjectIndex
{
    public ProjectSnapshot Snapshot { get; }
    public ImmutableDictionary<CanonicalId, IProjectRecord> RecordsById { get; }
    public ImmutableDictionary<CanonicalId, ContentUnit> ContentUnitsById { get; }
    public ImmutableDictionary<CanonicalId, AssertionRecord> AssertionsById { get; }
    public ImmutableDictionary<CanonicalId, ImmutableArray<ContentBinding>>
        BindingsByContentUnit { get; }
    public ImmutableDictionary<CanonicalId, ImmutableArray<SemanticLink>>
        LinksByEndpoint { get; }
}
```

Duplicate IDs are reported together. Do not pick an arbitrary winner; mark the
ID ambiguous and skip dependent rules explicitly.

Derived indexes include:

- artifact children ordered by `(ParentUnitId, Sequence, Id)`;
- propositions by predicate;
- assertions by proposition/role/status/scope;
- bindings by content and semantic target;
- links by source, target, and kind;
- definitions by subject;
- implementation/verification links by requirement;
- constraints by applicable record kind/tag.

Indexes are caches and never serialized as canon.

### 7.2 Dependency edge model

```csharp
public enum DependencyEdgeKind
{
    TypedReference,
    ContentContainment,
    ContentBinding,
    SemanticLink,
    DerivedDefinition,
    ConstraintReference,
    ProfileReference
}

public sealed record DependencyEdge(
    CanonicalId DependentId,
    CanonicalId DependencyId,
    DependencyEdgeKind Kind,
    string EvidenceIdOrFieldPath,
    bool CreatesReviewImpact);
```

Store forward and reverse adjacency as immutable ID-sorted arrays. Retain
multiple evidence edges between the same pair for explanations; traversal may
deduplicate destinations.

### 7.3 Edge extraction

Use explicit visitors, not authoritative reflection crawling.

Typed reference examples:

```text
content unit -> artifact and parent
proposition -> predicate and record-ref arguments
assertion -> proposition and sources
binding -> content unit and semantic target
link -> source and target
attestation -> content unit and binding fingerprints
constraint -> explicitly referenced records
profile record -> references identified by registered profile visitor
```

Semantic-link dependency directions follow section 4.12. The link record itself
depends on both endpoints. The semantic relationship adds separate derived edges
between endpoints.

Containment normally means child depends on parent. For review impact, artifact
metadata changes may affect all contained units; add explicit derived parent →
child impact edges without pretending the child is a stored field of the parent.

Bindings:

- `Uses`, `Discusses`, `Implements`, `Verifies`: content depends on semantic
  target.
- `Asserts`, `Defines`, `PresentsEvidence`: bidirectional review impact between
  content and semantic target.

`Mentions` links appear in relationship queries but `CreatesReviewImpact=false`
by default.

Unknown profile references are coverage gaps. Do not invent edges by scanning
opaque extension strings for ID-shaped text.

### 7.4 Impact algorithm

Use both base and projected graphs:

```text
ComputeImpact(changedIds, baseGraph, projectedGraph, policy):
    reverseUnion = review-impact reverse edges from both graphs
    visited = changedIds
    queue = changedIds sorted ordinal with depth 0
    predecessor = empty

    while queue not empty:
        current, depth = dequeue
        if depth == policy.MaximumDependencyDepth:
            mark truncated/inconclusive; continue

        for dependent in reverseUnion[current] sorted ordinal:
            if visited.Add(dependent):
                predecessor[dependent] = deterministic explaining edge set
                enqueue(dependent, depth + 1)

        if visited count exceeds MaximumImpactRecords:
            return Inconclusive with partial set

    return Complete(changed, impacted, predecessor)
```

Use breadth-first traversal. The predecessor map gives a shortest hop path. If
multiple edges connect the selected hop, retain all, sorted by kind/evidence.
Tie between predecessor nodes is resolved by canonical ID.

Cycles terminate through `visited`. A required complete impact analysis that
hits a bound blocks by default.

## 8. Deterministic common validators

### 8.1 Identity/reference/type

1. Validate all IDs and global uniqueness.
2. Validate record revision `>= 1`.
3. Resolve every typed reference.
4. Validate endpoint category/kind.
5. Validate no content containment cycle and sibling sequence uniqueness.
6. Validate content hashes.
7. Validate predicate arity/roles/value types.
8. Validate link endpoint rules.

Accumulate independent errors. Skip semantic rules whose prerequisites are
missing or ambiguous and mark the rule result inconclusive.

### 8.2 Assertion contradiction

Common v1 checks exact proposition identity and compatible scope:

```text
authoritative = assertions where Status == Accepted
group by PropositionId and Scope
if group contains Positive and Negative assertions whose roles are included by
the active NoAcceptedContradictions constraint:
    emit contradiction for each minimal opposite pair
```

Proposed/Rejected/Deprecated/Superseded assertions do not create errors by
default. A semantic `Contradicts` link between two simultaneously accepted
assertions also triggers the constraint even when propositions differ; that link
is the author's explicit statement that the natural-language claims conflict.

The validator does not infer contradiction between arbitrary glosses.

### 8.3 Support coverage

For each authoritative assertion whose role is selected by
`RequireSupportForRoles`:

```text
supporters = SourceIds
             + incoming Supports links (source supports assertion)
             + outgoing DerivedFrom targets
             + outgoing Cites targets
filter supporters by constraint-allowed kinds
if distinct supporter count < minimum:
    emit missing-support diagnostic with accepted repair link kinds
```

This proves traceability, not truth of the supporting material.

### 8.4 Derivation cycles

For `AcyclicLinkKinds`:

1. Construct the directed endpoint graph for included link kinds using their
   source → target semantic direction.
2. Visit vertices/edges in canonical-ID order.
3. Run Tarjan strongly connected components.
4. An SCC with more than one vertex, or a self-loop, violates the constraint.
5. Emit one stable diagnostic per SCC listing sorted IDs and internal link IDs.

Ordinary dependency cycles may be legitimate. Only declared acyclic semantic
link kinds are checked.

### 8.5 Definition validation

An accepted Definition assertion must participate in `Defines(definition,
subject)` or an equivalent binding.

For `UniqueDefinitions`, group accepted definition sources by subject and
compatible scope. More than one emits ambiguity unless one explicitly
`Supersedes` the others and the older assertions are Superseded/Deprecated.

Every `Uses(X, subject)` gains dependency edges to the unique active definition.
If no definition exists, severity follows constraint policy. If ambiguous, do
not pick one.

### 8.6 Requirement traceability

For each Accepted Requirement assertion selected by
`RequireRequirementCoverage`:

- Count incoming `Implements` relationships where source is accepted/current.
- Count incoming `Verifies` relationships where source is accepted/current.
- Content bindings with matching roles count only when the bound semantic record
  is compatible.
- Compare each count with its configured minimum.

Diagnostics identify separately missing implementation and missing verification.

### 8.7 Semantic-review freshness

For every content unit selected by `RequireSemanticReviewForContentKinds`:

1. Find its current attestation.
2. Require exactly one active attestation under v1.
3. Compare `ReviewedTextHash` to current `TextHash`.
4. Compare reviewed binding fingerprints with current policy-relevant bindings.
5. Emit stale/missing review evidence precisely.

Editing whitespace changes the text hash and invalidates review. An importer may
offer a formatting-only review profile, but the core does not guess that a text
change is semantically irrelevant.

### 8.8 Constraint and profile coverage

Unknown constraint cases are schema errors. Unknown profile records round-trip
only when their profile declares preservation-compatible behavior; otherwise
loading is unsupported. A registered profile reports whether each record was
validated. The common coverage report lists uncovered namespaces/records.

## 9. Transaction algorithms

### 9.1 Transaction and operations

```csharp
public sealed record ProjectTransaction(
    TransactionId Id,
    ProjectId ProjectId,
    long BaseProjectRevision,
    string BaseContentHash,
    string Intent,
    string Author,
    DateTimeOffset CreatedAt,
    TransactionStatus Status,
    ImmutableArray<ProjectOperation> Operations,
    ImmutableArray<ReviewDispositionRecord> ReviewDispositions,
    ImmutableArray<ReviewRun> ReviewRuns,
    ImmutableArray<DiagnosticAcknowledgement> Acknowledgements);

public abstract record ProjectOperation
{
    public required CanonicalId TargetId { get; init; }
    public sealed record Add(IProjectRecordDraft Record) : ProjectOperation;
    public sealed record Replace(int ExpectedRevision, IProjectRecordDraft Record)
        : ProjectOperation;
    public sealed record Remove(int ExpectedRevision) : ProjectOperation;
}
```

One final operation per target ID. Editing a draft replaces that operation.
Imported duplicate targets are errors. Clients cannot select record revisions.

### 9.2 Apply operations

```text
Apply(base, transaction):
    require project ID/base revision/base hash match
    copy each canonical collection to ID-keyed builders

    for operation sorted by TargetId:
        Add: require absent; materialize Revision=1
        Replace: require current and expected revision; preserve category;
                 materialize Revision=expected+1
        Remove: require current and expected revision; remove only target

    materialize arrays sorted by ID
    provisional project revision = base + 1
    parent hash = base hash
    return projected snapshot, changed IDs, operation diagnostics
```

Broken references after removal are snapshot diagnostics. Repairs can coexist in
the same transaction. No cascade.

### 9.3 Disposition fingerprints

```text
TargetFingerprint = SHA-256(target ID + projected record revision + canonical
                            projected record bytes)

ImpactFingerprint = SHA-256(ChangeSetHash + target ID +
                            ordered shortest ImpactStep values)
```

Recompute after every operation change. Retain a disposition only if both
fingerprints match. `Updated` is recomputed automatically for direct changes.

### 9.4 Build obligations

```text
BuildObligations(impact, projected, constraints, submittedDispositions):
    for changed ID:
        add Updated obligation if policy selects it for reporting

    for impacted ID sorted:
        if no RequireImpactDispositions constraint selects record:
            continue
        compute target/impact fingerprints
        if matching submitted disposition exists and allowed:
            validate reviewer/rationale fields; use it
        else:
            Pending

    return stable-sorted obligations
```

If impact is inconclusive, obligations are incomplete and commit policy blocks
when complete impact is required.

### 9.5 Review profile enforcement

For every `RequireReviewProfileForChanges` constraint triggered by the changed
set:

1. Find a Completed review run with exact profile/version, base project hash,
   current change-set hash, projected-state hash, and non-truncated acceptable
   context packet.
2. Require every concern to have a policy-allowed disposition.
3. Rejected/Acknowledged concerns require rationale and actor.
4. A provider failure, stale hash, or missing run is an incomplete required
   review, not proof of invalid content.

### 9.6 Diagnostic acknowledgements

Fingerprint:

```text
SHA-256(rule code + rule version + primary ID + sorted related IDs +
        canonical evidence values)
```

Messages/source line numbers are excluded. Acknowledgements never suppress
errors, internal failures, stale transaction conflicts, or required
inconclusive analysis. Policy may allow them for warnings/concerns.

### 9.7 Commit orchestration

```text
Commit(transactionId):
    load draft transaction
    acquire exclusive workspace lock
    read/strictly verify canonical head

    if head revision/hash differs from transaction base:
        return Conflict without mutation

    projected = Apply(head, transaction)
    if operation errors: return Rejected

    baseIndex/graph = Build(head)
    projectedIndex/graph = Build(projected)
    impact = ComputeImpact(changed, baseGraph, projectedGraph, policy)
    obligations = BuildObligations(...)
    report = ValidateFull(projected, graphs, impact, obligations, reviews)
    decision = EvaluateCommitPolicy(report)

    if decision blocks or requires external approval:
        persist report; return without canonical mutation

    commit metadata = injected clock/author/intent
    recheck submitted evidence against ChangeSetHash and ProjectedStateHash
    bytes/hash = CanonicalSerialize(projected + commit metadata)
    atomically replace canonical file while lock remains held
    reconcile transaction/receipt metadata
    return Committed(revision, hash, report)
```

### 9.8 Atomic workspace

POC layout:

```text
<workspace>/
  project.vw.json
  .validatedworld/
    commit.lock
    transactions/<tx-id>.json
    reports/<tx-id>.json
    receipts/<revision>-<tx-id>.json
```

Replacement:

1. Resolve exact workspace/canonical paths.
2. Write a unique sibling temp file on the same volume.
3. Flush it to disk and close.
4. Re-read and verify canonical hash.
5. Use atomic replace with a known backup where supported.
6. Reopen/verify canonical before removing backup.

`IAtomicFileWriter` reports whether the filesystem provides the required
semantics. Inability is blocking.

On startup, verified canonical content wins. If it is corrupt and a known backup
verifies, report an explicit recovery option; never silently roll back or choose
files by newest timestamp.

Inject failures before write, during write, after flush, before replace, after
replace, and during receipt update. Every failure before replace preserves exact
canonical bytes. If replace succeeds but receipt update fails, the embedded
commit metadata is authoritative and recovery reconciles it.

### 9.9 Concurrency

No automatic merge in Gate A. A stale transaction reports base/head hashes and
revisions. A future rebase replays operations against head, checks record
preconditions, recomputes all impact/reviews, and never carries stale
dispositions.

## 10. Validation engine and diagnostics

### 10.1 Validator contract

```csharp
public interface IProjectValidator
{
    string RuleCode { get; }
    int RuleVersion { get; }
    ValidationPhase Phase { get; }
    ValueTask<ImmutableArray<Diagnostic>> ValidateAsync(
        ValidationContext context,
        CancellationToken cancellationToken);
}
```

`ValidationContext` contains projected snapshot/index/graph, optional base graph,
impact result, obligations, review runs, policy, and mode.

Validators return findings for invalid user data; they do not throw. Unexpected
exceptions become one stable internal-failure diagnostic and make the phase
inconclusive. Cancellation is inconclusive.

Register validators explicitly. Stable-sort results by:

1. phase;
2. severity;
3. rule code;
4. primary ID;
5. source pointer;
6. fingerprint.

### 10.2 Report

```csharp
public sealed record ValidationReport(
    ValidationOutcome Outcome,
    string BaseProjectHash,
    string EvaluatedStateHash,
    string? ChangeSetHash,
    ImmutableArray<PhaseResult> Phases,
    ImmutableArray<Diagnostic> Diagnostics,
    ImpactResult? Impact,
    ImmutableArray<ReviewObligation> ReviewObligations,
    ReviewCoverage ReviewCoverage,
    CoverageReport Coverage,
    ValidationStatistics Statistics);
```

Outcome is `ProvenValid`, `Invalid`, or `Inconclusive`.

- Any deterministic error makes it Invalid.
- With no error, incomplete required phases make it Inconclusive.
- ProvenValid means all policy-required deterministic/workflow checks completed;
  it does not assert general prose truth.

Coverage includes:

- content units by kind;
- current/stale/missing semantic reviews;
- bound/unbound content;
- assertions by role/status;
- links by kind/provenance;
- uncovered profile/extension records;
- complete/truncated impact;
- required/completed review profiles;
- pending/dispositioned obligations and concerns;
- validators run/skipped/failed/not applicable.

### 10.3 Diagnostic ranges

```text
VW10xx  JSON, schema, hashing, migration
VW11xx  IDs, records, references, values
VW12xx  content/artifact order and hashes
VW13xx  predicates, propositions, assertions
VW14xx  bindings, semantic links, definitions
VW15xx  support, cycles, requirement traceability
VW16xx  dependency impact and review obligations
VW17xx  review runs, concerns, attestations
VW18xx  transaction, policy, commit, storage
VW20xx  exports and coverage
VW30xx  narrative profile (reserved)
VW31xx  interactive-state profile (reserved)
VW90xx  internal failure/unsupported construct
AIxxxx  heuristic concerns
```

Initial codes:

```text
VW1001 invalid JSON
VW1002 duplicate JSON property
VW1003 unsupported schema version
VW1004 project content hash mismatch
VW1101 invalid canonical ID
VW1102 duplicate canonical ID
VW1103 missing reference
VW1104 wrong endpoint/record kind
VW1201 invalid content parent/order
VW1202 content text hash mismatch
VW1301 predicate argument mismatch
VW1302 accepted assertion contradiction
VW1401 invalid binding endpoints
VW1402 invalid semantic-link endpoints
VW1403 missing or ambiguous active definition
VW1501 missing required support
VW1502 forbidden semantic dependency cycle
VW1503 missing requirement implementation
VW1504 missing requirement verification
VW1601 impact analysis limit reached
VW1602 required impact disposition pending
VW1603 stale impact disposition
VW1701 missing semantic review attestation
VW1702 stale semantic review attestation
VW1703 required review profile missing/stale/failed
VW1704 unresolved required review concern
VW1801 stale transaction base
VW1802 operation precondition failed
VW1803 approval required
VW1804 atomic replacement unavailable/failed
VW2001 incomplete semantic/profile coverage
VW9001 validator internal failure
VW9002 unsupported construct
```

Do not recycle meanings within schema v1.

## 11. Application and CLI

### 11.1 Application handlers

```text
InitializeProject
GetProjectStatus
GetRecord
ListRecords
BeginTransaction
GetTransaction
ApplyOperations
ValidateProject
ValidateTransaction
AnalyzeTransactionImpact
ListReviewObligations
SetReviewDisposition
SubmitReviewRun
ResolveReviewConcern
CommitTransaction
AbortTransaction
GetDependencies
GetDependents
ExplainDependencyPath
ExplainDiagnostic
```

Generation provides `BuildContextPacket`; Export provides document/report
exports. Hosts compose them with Application.

Expected invalid input returns typed `OperationResult<T>`, not exceptions. Every
read includes project revision/hash. Every draft result includes transaction/base
identity.

### 11.2 CLI commands

```text
vw init --workspace <path> --project-id <id> --title <text>
vw status --workspace <path>

vw tx begin --workspace <path> --intent <text> [--author <text>]
vw tx show --workspace <path> --tx <id>
vw tx apply --workspace <path> --tx <id> --operations <file-or-stdin>
vw tx impact --workspace <path> --tx <id>
vw tx validate --workspace <path> --tx <id>
vw tx obligations --workspace <path> --tx <id>
vw tx disposition --workspace <path> --tx <id> --target <id>
                  --status <reviewed-no-change|not-applicable> --reason <text>
vw tx review submit --workspace <path> --tx <id> --input <file-or-stdin>
vw tx concern resolve --workspace <path> --tx <id> --concern <id>
                      --status <value> --reason <text>
vw tx commit --workspace <path> --tx <id> [--approval <token>]
vw tx abort --workspace <path> --tx <id>

vw get --workspace <path> --id <id>
vw list --workspace <path> [--category <value>] [--kind <value>] [--tag <value>]
vw dependencies --workspace <path> --id <id> [--transitive]
vw dependents --workspace <path> --id <id> [--transitive]
vw explain path --workspace <path> --from <id> --to <id>
vw validate --workspace <path>
vw context --workspace <path> --seed <id> [--tx <id>] --output <path>
vw export --workspace <path> --profile <name> --output <path>
```

No natural-language query parser in the POC.

### 11.3 Output envelope

```json
{
  "outputSchemaVersion": "validatedworld-cli/v1",
  "command": "tx.impact",
  "status": "review-required",
  "project": {
    "id": "project:offline-sensor",
    "revision": 3,
    "contentHash": "sha256:..."
  },
  "transaction": {
    "id": "tx:...",
    "baseRevision": 3,
    "baseContentHash": "sha256:...",
    "changeSetHash": "sha256:...",
    "projectedStateHash": "sha256:..."
  },
  "diagnostics": [],
  "coverage": {},
  "data": {}
}
```

Exactly one JSON document on stdout. Operational logs go to stderr.

### 11.4 Exit codes

```text
0  completed / proven valid where validation applies
2  deterministic validation rejected
3  command/input contract error
4  stale revision or record/lock conflict
5  storage failure
6  required analysis/review inconclusive
7  review obligations or concern resolution required
8  unsupported schema/profile or migration required
9  internal failure
```

Warnings do not change exit code unless policy blocks them.

## 12. Context packets and heuristic review

### 12.1 Context packet

```csharp
public sealed record ContextPacket(
    string SchemaVersion,
    ProjectId ProjectId,
    long BaseProjectRevision,
    long EvaluatedProjectRevision,
    string BaseProjectHash,
    string? ChangeSetHash,
    string EvaluatedStateHash,
    ImmutableArray<CanonicalId> SeedIds,
    ImmutableArray<ContextRecord> Records,
    ImmutableArray<ImpactStep> SelectionPaths,
    ImmutableArray<CanonicalId> OmittedRecordIds,
    bool Truncated,
    string PacketHash);
```

Selection priority:

```text
0 seeds and directly changed records
1 applicable constraints and active definitions
2 forward dependencies required to understand seeds
3 review-obligation targets and shortest impact paths
4 content units bound to included semantic records
5 additional reverse dependents by increasing distance
```

Within priority/distance sort by ID. Include records atomically. If a required
seed/constraint cannot fit, packet construction is inconclusive. Always list
omissions and limits.

### 12.2 Review provider

```csharp
public interface IProjectReviewProvider
{
    string ProviderId { get; }
    Task<ReviewResponse> ReviewAsync(ReviewRequest request, CancellationToken ct);
}
```

Cache key covers project/transaction/packet hashes, profile/template versions,
provider/model ID, parameters, and response schema.

Provider output can propose:

- concerns;
- candidate subjects/propositions/assertions;
- candidate bindings/semantic links;
- suggested dispositions with rationale.

Only concerns enter a review run automatically. Candidate canonical records or
dispositions require explicit application/confirmation by an agent or human.

The POC accepts externally produced structured review JSON and uses a fake
provider for tests. A real paid provider is Gate B, not Gate A.

### 12.3 Review packet prompt contract

A profile instructs a reviewer to:

1. Use only supplied records and clearly label inference.
2. Cite every concern with record IDs.
3. Identify likely missing links separately from contradictions.
4. State when context is insufficient.
5. Return the versioned structured response schema.

An AI review that ignores the schema fails; its prose is not parsed loosely into
canonical findings.

## 13. Source import and derived exports

### 13.1 Canonical source boundary

In Gate A, the workspace JSON records are canonical. A `ContentUnit.Text` field
contains the reviewed source text for that unit; it is not a pointer into a
mutable Markdown file. This avoids pretending that arbitrary edits to prose can
be mapped back to stable semantic records without ambiguity.

The CLI may import a marked-up Markdown document into a new transaction. Each
importable section must carry a stable unit marker such as:

```markdown
<!-- vw:unit unit:power-budget -->
## Power budget
...
```

Import rules:

1. Normalize line endings, but preserve all other text exactly.
2. Reject duplicate, missing, or malformed unit markers.
3. Match units by marker ID, never by heading text or ordinal position.
4. Stage changed unit text and hashes in a transaction.
5. Create normal impact obligations before commit.
6. Never infer new canonical assertions merely because prose changed.

Unmarked Markdown may be offered to a heuristic extractor, but the result is a
proposal file, not canon.

### 13.2 Deterministic exports

Gate A exports are derived artifacts and include:

- a readable Markdown document ordered by artifact and content-unit order;
- a semantic-link report grouped by source record;
- a review-obligation report with evidence and disposition status;
- a context packet for an agent or reviewer;
- a Graphviz DOT dependency graph;
- a JSON Lines inventory for machine processing.

Every export contains project revision, project content hash, exporter version,
profile version, and generation time outside the hashed semantic payload. Sort
all semantic collections by their specified deterministic key. Export output is
never an authoritative input unless it is explicitly re-imported through a
transaction.

## 14. Gate A proof project: TechnicalDesign

The first sample is deliberately small enough to inspect manually and rich
enough to disprove the core product if impact analysis is noisy or incomplete.

### 14.1 Document

`samples/TechnicalDesign` describes an offline environmental sensor. Its source
artifact has these stable content units:

```text
unit:overview
unit:requirements
unit:power-budget
unit:architecture
unit:verification
unit:privacy
```

The sample uses value-neutral stable proposition IDs. Values live in proposition
arguments and may change without changing identity. It includes:

```text
proposition:runtime-requirement  sensor must operate for at least 24 hours
  assertion:runtime-requirement          Accepted Requirement

proposition:average-current      active current is 20 mA
  assertion:average-current-assumption   Accepted Assumption

proposition:battery-capacity     available capacity is 500 mAh
  assertion:battery-capacity-assumption  Accepted Assumption

proposition:runtime-estimate     calculated nominal runtime is 25 hours
  assertion:runtime-estimate             Accepted Result

proposition:battery-sufficiency  selected battery satisfies runtime requirement
  assertion:battery-sufficiency          Accepted Conclusion
```

Required links include:

```text
assertion:runtime-estimate
  DerivedFrom -> assertion:average-current-assumption
  DerivedFrom -> assertion:battery-capacity-assumption

assertion:battery-sufficiency
  DependsOn -> assertion:runtime-estimate
  Satisfies -> assertion:runtime-requirement

unit:architecture
  DependsOn -> assertion:battery-sufficiency
  Implements -> assertion:runtime-requirement

unit:verification
  DependsOn -> assertion:battery-sufficiency
  Verifies -> assertion:runtime-requirement
```

Bindings connect assertions to the content unit that states, calculates,
implements, or tests them. `unit:privacy` has no dependency path from the
electrical assumptions.

The Gate A disposition policy selects Accepted Assumption, Result, Conclusion,
and Decision assertions plus section content units. It reports direct selected
changes as `Updated`; links, bindings, and attestations remain visible in the
full impact report but are enforced by their own validators instead of becoming
blocking disposition targets.

### 14.2 Required mutation scenario

The acceptance transaction replaces `proposition:average-current` at its current
record revision so its value argument changes from 20 mA to 25 mA. The stable ID
does not change. The transaction also updates the bound sentence in
`unit:power-budget` and replaces its semantic-review attestation.

The engine must deterministically identify review obligations for:

- the accepted current assumption;
- the calculated-runtime result assertion;
- the battery-sufficiency conclusion assertion;
- the architecture section that implements the choice;
- the verification section whose plan depends on the choice;
- the directly changed power-budget section, reported as `Updated`.

It must not include `unit:privacy`. The author must update the runtime and
conclusion, or attest with evidence that each remains valid. A commit with any
pending obligation must fail atomically.

The expected corrected calculation is 20 hours. Gate A deliberately does not
pretend to derive arithmetic from prose, so the graph's job is to force the
runtime and sufficiency reviews. A golden invalid follow-up updates the runtime
result to 20 hours while retaining both an Accepted sufficiency conclusion and
an explicit `Contradicts` relationship between that conclusion and the runtime
requirement. `NoAcceptedContradictions` must reject it. This distinguishes the
honest common guarantee (impact and explicit-rule checking) from a later typed
numeric-calculation validator.

### 14.3 Intentional-error corpus

Include transactions for:

- omitted runtime dependency;
- reversed `DerivedFrom` edge;
- changed prose with a stale binding hash;
- unreviewed impacted section;
- unsupported `NotApplicable` disposition with no evidence;
- positive and negative Accepted assertions of the same proposition/scope;
- stale base revision;
- semantically irrelevant privacy edit;
- truncated context packet;
- malformed heuristic response.

Each fixture has a golden structured diagnostic file. Golden files compare
semantic fields and deterministic ordering; volatile timestamps are excluded.

### 14.4 Gate A success criteria

Gate A passes only if:

1. A clean checkout can restore, build, and run all tests.
2. The base TechnicalDesign project validates with no errors.
3. Every intentional error yields its expected diagnostic code and evidence.
4. The current-change transaction produces all and only the expected impact
   obligations before any manual additions.
5. A pending-obligation or invalid transaction changes no canonical bytes.
6. Replaying accepted transactions produces identical canonical hashes.
7. A user or agent can inspect every impact path and understand why it exists.

If criterion 4 cannot be met without linking nearly every section to every
other section, the minimal product has failed. Record that result in
`docs/feasibility.md` before expanding scope.

## 15. Profiles after Gate A

Profiles add vocabulary and validators; they do not fork the transaction,
serialization, impact, review, or diagnostic architecture.

### 15.1 TechnicalDesign profile

Gate A supplies a deliberately small controlled vocabulary:

```text
subject kinds: component, interface, requirement, metric, assumption,
               decision, risk, test, source
predicate kinds: value, unit, minimum, maximum, status, selected-option,
                 rationale, result
link kinds: DependsOn, DerivedFrom, Defines, Uses, Supports, Contradicts,
            Implements, Satisfies, Verifies, Supersedes, Cites, Mentions
```

Projects can add namespaced extension terms, but the core only applies rules to
terms it understands. Unknown extensions round-trip and are reported as outside
deterministic coverage.

### 15.2 LinearNarrative profile

After Gate A, add narrative records without redefining the common core:

```csharp
public sealed record StoryEvent(
    EventId Id,
    string Name,
    ScopeId ScopeId,
    FictionalInterval When,
    int NarrativeOrdinal,
    ImmutableArray<EventParticipant> Participants,
    ImmutableArray<EventEffect> Effects,
    ImmutableArray<CanonicalId> EvidenceIds);

public sealed record BeliefState(
    AssertionId Id,
    SubjectId KnowerId,
    PropositionId PropositionId,
    BeliefPolarity Polarity,
    ConfidenceBand Confidence,
    FictionalInterval ValidDuring,
    ImmutableArray<CanonicalId> EvidenceIds);
```

Keep these axes separate:

- canon truth: which assertions hold in the fictional world;
- perspective: what each character knows or believes;
- fictional time: when events and assertions hold;
- narrative order: when readers or players encounter material;
- authoring revision: which project commit contains the records.

First narrative validators cover interval overlap, event precondition/effect
references, impossible co-location where explicitly modeled, revelation before
knowledge, and mutually exclusive active facts. Missing facts remain unknown.

The Harbor mystery sample is the Gate C fixture. It must not delay the document
graph proof.

### 15.3 InteractiveState profile

An interactive story is authored as a static transition specification:

```csharp
public sealed record StateVariableDefinition(
    StateVariableId Id,
    string Name,
    ValueDomain Domain,
    JsonElement InitialValue);

public sealed record TransitionDefinition(
    TransitionId Id,
    string Name,
    BooleanExpression EnabledWhen,
    ImmutableArray<StateEffect> Effects,
    ImmutableArray<ConstraintId> RequiredInvariantIds);
```

The graph remains static: variable definitions, possible transitions,
preconditions, effects, invariants, scenes, and consequences are canonical
records. A particular playthrough is a derived trace through that graph, not a
second mutable canon.

Do not represent state by copying a complete world for every branch. Use a
bounded vector of declared state variables and transitions. This is not merely
"more edges": conditional edges need explicit predicates and effects or the
validator cannot distinguish mutually exclusive branches.

Bounded exploration algorithm:

```text
frontier := ordered set containing initial state
visited := hash(initial state) -> shortest trace

while frontier not empty and states < MaxStates and depth < MaxDepth:
    state := remove smallest canonical state hash
    for transition in transitions sorted by ID:
        if transition.EnabledWhen(state):
            next := ApplyEffects(state, transition)
            validate invariants(next)
            record counterexample on violation
            enqueue next if unseen

if a bound prevents complete exploration:
    result := Inconclusive with explored counts and frontier evidence
else:
    result := ProvenWithinDeclaredModel
```

This can prove properties only within the declared variables, transitions, and
bounds. It cannot prove arbitrary game code correct. Runtime integration and
game-engine adapters remain outside Core.

## 16. Safety, migration, and recovery

### 16.1 Workspace safety

- Validate the complete snapshot before replacing the canonical file.
- Write temporary files within the workspace directory.
- Flush the temporary file before atomic replacement where the platform permits.
- Preserve the last accepted snapshot until replacement succeeds.
- Treat lock loss or base-hash mismatch as a stale transaction.
- Never follow workspace-relative paths outside the workspace root.
- Never execute source text, expressions, or reviewer output as code.

### 16.2 Schema migration

Migration is an explicit command that:

1. reads and validates the old schema;
2. writes a new workspace or backup beside the original;
3. produces a deterministic migration report;
4. validates the migrated project;
5. replaces the source only with explicit confirmation.

The POC has no compatibility promise. Prefer a clear breaking schema change to
an ambiguous migration. Preserve unknown extension records only when their
round-trip behavior is tested.

### 16.3 Recovery records

Accepted transaction records contain the base hash, projected hash, canonical
patch, obligation dispositions, validation report hash, author, and timestamp.
They are an audit/replay aid, not another source of canon. If replay output
differs from the recorded projected hash, stop and report corruption.

## 17. Test strategy

### 17.1 Test layers

```text
Core unit tests
  ID/value-object validation, intervals, assertion scope, link contracts

Serialization unit and property tests
  strict parsing, canonical ordering, hash stability, unknown fields,
  duplicate IDs, round trips

Validation unit tests
  diagnostic codes, evidence, tri-state results, binding checks

Application integration tests
  transaction lifecycle, impact obligations, stale commits, atomic failure,
  replay, workspace locks

CLI contract tests
  stdout envelope, stderr separation, exit codes, deterministic ordering

Sample/golden tests
  TechnicalDesign base project and intentional-error corpus

Optional provider contract tests
  schema enforcement, cache keys, inconclusive responses, no implicit canon edits
```

### 17.2 Required properties

Use generated inputs for these invariants:

- canonical serialization is idempotent;
- record order does not affect canonical output;
- IDs are stable through round trips;
- impact results are stable under unrelated record insertion;
- union-graph impact never loses a base-only or projected-only dependency path;
- failed commits preserve the exact canonical bytes;
- replay of an accepted transaction reproduces its recorded hash;
- all diagnostic evidence IDs exist in the evaluated snapshot;
- any bounded-out analysis reports inconclusive, never success.

### 17.3 Mutation and fault injection

Inject failures after temporary write, validation, flush, lock verification, and
immediately before replace. Verify that canon is either the complete old
snapshot or the complete new snapshot, never a partial file. Inject cancellation
into provider calls and context construction; cancellation cannot mutate canon.

## 18. Ordered work packages

Do one work package at a time. Do not introduce projects or frameworks needed
only by a later package.

### WP0 — solution wiring

- Add `ValidatedWorld.Application` and matching test project.
- Verify project references follow Section 3.
- Add shared test helpers only where two projects actually need them.
- Acceptance: the empty wiring builds and the entire existing suite passes.

### WP1 — common immutable domain

- Implement IDs, `ProjectSnapshot`, artifact/content, proposition/assertion,
  source/binding, link, constraint, and profile records.
- Enforce constructor-level local invariants without cross-record lookups.
- Acceptance: domain tests cover valid creation and every rejected local shape.

### WP2 — strict serialization and TechnicalDesign skeleton

- Implement strict JSON readers, canonical writers, hashes, workspace loader,
  and the clean TechnicalDesign snapshot.
- Acceptance: round-trip, reorder, duplicate, unknown-field, and golden-hash
  tests pass.

### WP3 — indexes and deterministic validation

- Build immutable indexes and common/profile validators.
- Add evidence-bearing diagnostics and coverage reports.
- Acceptance: the intentional structural and semantic error fixtures produce
  their golden results.

### WP4 — transaction projection and atomic commit

- Implement patch operations, projection, stale checks, validation gate,
  atomic replacement, transaction log, and replay.
- Acceptance: fault-injection tests prove failed commits preserve canonical
  bytes and accepted replay reproduces hashes.

### WP5 — impact and mandatory review

- Implement semantic seeds, base/projected union traversal, content projection,
  obligation creation, dispositions, attestations, and commit policy.
- Acceptance: the current-change scenario produces exactly the Section 14 set;
  incomplete or stale review cannot commit.

### WP6 — agent-grade interface

- Implement CLI envelopes, exit codes, context packets, Markdown import/export,
  DOT/JSONL reports, and help text.
- Acceptance: a scripted agent can inspect, stage, analyze, review, validate,
  commit, and export without parsing human prose.

### WP7 — Gate A evaluation

- Run the falsification measurements in `docs/feasibility.md`.
- Record false-positive/false-negative examples and authoring burden.
- Acceptance: explicitly approve, narrow, or stop the common product before
  narrative specialization begins.

### WP8 — optional heuristic review

- Add review schema, external-result ingestion, fake provider, caching, concern
  resolution, and one optional real provider adapter outside Core.
- Acceptance: malformed, partial, canceled, and contradictory reviewer outputs
  are auditable and cannot alter canon automatically.

### WP9 — LinearNarrative profile

- Add fictional time, events, knowledge/belief, narrative order, validators, and
  a reduced Harbor mystery sample.
- Acceptance: Gate C linear-story counterexamples are replayable and diagnostic
  coverage is stated precisely.

### WP10 — InteractiveState profile

- Add typed state variables, transition expressions/effects, invariants, traces,
  and bounded exploration.
- Acceptance: Gate D known-good and known-bad miniature campaigns return proven,
  disproven, and inconclusive results correctly.

### WP11 — integration packaging

- Stabilize the CLI/tool schema first, then evaluate MCP/Codex/plugin packaging
  against the current external standard.
- Acceptance: the integration is a thin adapter; no provider or packaging types
  leak into Core or canonical files.

## 19. Pull-request checklist

Every implementation change states:

- the work package or bounded vertical slice;
- the changed canonical/public contracts;
- tests added or updated;
- sample/golden changes;
- deterministic guarantees and remaining inconclusive behavior;
- `dotnet restore ValidatedWorld.slnx` result;
- `dotnet build ValidatedWorld.slnx` result;
- `dotnet test ValidatedWorld.slnx` result.

Do not call a slice complete when its acceptance criteria or required full-suite
verification are missing.

## 20. Deferred decisions and explicit non-goals

Defer until evidence requires them:

- a universal ontology or unrestricted user rule language;
- automatic semantic extraction as authoritative canon;
- natural-language query parsing;
- incremental validation and graph databases;
- rich visual graph editing;
- collaborative multi-writer merge semantics;
- a game-engine runtime;
- arbitrary source-code or mathematical-proof verification;
- plugin packaging tied to a currently fashionable format.

The intended scaled-down product is still useful if heuristic extraction,
narrative logic, and interactive exploration all fail: it is a transactional,
typed dependency and mandatory-review compiler for connected documents. If even
the TechnicalDesign impact gate cannot beat ordinary search plus a checklist at
reasonable authoring cost, stop expanding the project and say so plainly.

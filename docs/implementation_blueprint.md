# ValidatedWorld Implementation Blueprint

**Status:** Coding-agent handoff

**Blueprint version:** 1.0

**Last reviewed:** 2026-08-10

**Target:** .NET 10 / C#

**POC schema:** `validatedworld/v1`

## 1. Purpose and reading order

This document turns the product specification into implementable work. A coding
agent should read, in order:

1. [feasibility.md](feasibility.md) for the guarantee boundary.
2. [validated_world_authoring_spec.md](validated_world_authoring_spec.md) for
   product requirements.
3. This blueprint for types, algorithms, project boundaries, tests, and work
   packages.

The pseudocode is normative about behavior, not exact syntax. Agents may improve
names or local implementation details, but must update this document before
changing a stated invariant or serialized contract.

Do not implement future sections opportunistically. Complete one vertical work
package, run `dotnet build ValidatedWorld.slnx` and
`dotnet test ValidatedWorld.slnx`, and leave the repository in a working state.

## 2. Non-negotiable implementation invariants

1. Core types have no file, JSON, console, network, model-provider, UI, game
   engine, or database dependency.
2. Canon is read as an immutable `WorldSnapshot`. Validation never mutates it.
3. Every canonical edit is represented by a transaction operation.
4. A failed or stale commit leaves the canonical snapshot unchanged.
5. References use stable IDs. Display names are never foreign keys.
6. Missing information is unknown unless a schema explicitly declares a
   closed-world interpretation.
7. Canon truth, character perspective, fictional time, narrative order, and
   project revision remain separate types.
8. Conditions and effects are a closed data AST. Never evaluate arbitrary C#,
   JavaScript, templates, or shell commands from a world file.
9. Bounded-out analysis returns `Inconclusive`; it never returns `Pass`.
10. Diagnostic ordering and fingerprints are deterministic.
11. AI findings are never deterministic errors and never mutate canon directly.
12. Generated artifacts identify the exact source hash and are not authoritative.
13. The POC runs all commit validators over the complete projected snapshot.
14. Delete operations never cascade implicitly.
15. Tests must not require a network connection or an AI API key.

## 3. Target solution architecture

### 3.1 Projects and dependencies

Add `ValidatedWorld.Application` during work package 0. Add no other production
project until a work package requires it.

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
  purpose: context packets, provider-neutral review contracts, provider adapters

ValidatedWorld.Export
  dependencies: Core, Validation

ValidatedWorld.Cli
  dependencies: Application, Export, Generation

ValidatedWorld.Mcp (post-POC)
  dependencies: Application, Export, Generation
```

Generation and Export may consume Validation indexes, dependency graphs, reports,
and diagnostics. Validation never references either adapter, so this creates no
cycle. Move only genuinely universal value types to Core; do not move host or
format concerns there merely to avoid a project reference.

### 3.2 Namespace responsibilities

```text
ValidatedWorld.Core.Identifiers
ValidatedWorld.Core.Values
ValidatedWorld.Core.Model
ValidatedWorld.Core.Expressions
ValidatedWorld.Core.Transactions

ValidatedWorld.Serialization.Json
ValidatedWorld.Serialization.Migrations
ValidatedWorld.Serialization.Workspaces

ValidatedWorld.Validation.Diagnostics
ValidatedWorld.Validation.Indexes
ValidatedWorld.Validation.Dependencies
ValidatedWorld.Validation.Rules
ValidatedWorld.Validation.Narratives
ValidatedWorld.Validation.Mysteries

ValidatedWorld.Application.Transactions
ValidatedWorld.Application.Queries
ValidatedWorld.Application.Commits

ValidatedWorld.Generation.Context
ValidatedWorld.Generation.Review

ValidatedWorld.Export.Profiles
```

Folders should follow concepts, not place every type in its own folder. Prefer a
small cohesive file when several tiny discriminated-union cases belong together.

### 3.3 Dependency injection

Use constructor injection in the application and hosts. Do not add a DI container
to Core, Validation, or Serialization. The CLI may use the built-in .NET service
container only if it materially simplifies composition; manual composition is
acceptable for the POC.

Inject these nondeterministic or environmental services:

- `IClock`
- `ITransactionIdGenerator`
- `IWorldStore`
- `ITransactionStore`
- `IWorkspaceLock`
- AI review providers

Tests use fixed clocks, deterministic IDs, and in-memory/fault-injecting stores.

## 4. Core domain model

Use sealed immutable records and `ImmutableArray<T>` /
`ImmutableDictionary<TKey,TValue>` at snapshot boundaries. Domain constructors
may assume values have passed factory validation; parsing factories return
results rather than partially valid objects.

### 4.1 Canonical IDs

Use one validated value type for globally unique IDs and thin semantic wrappers
only where they prevent common mistakes.

```csharp
public readonly record struct CanonicalId(string Value);
public readonly record struct WorldId(string Value);
public readonly record struct TransactionId(string Value);
```

Canonical record IDs use lower-case ASCII and one colon:

```text
<category>:<local-name>
```

`category` and every slash-delimited segment of `local-name` match
`[a-z][a-z0-9-]*`. Examples:

```text
character:clarisse
location:harbor-village
predicate:located-at
proposition:mayor-killed-merchant
node:chapter-03/office-interview
```

The complete pattern is:

```regex
^[a-z][a-z0-9-]*:[a-z][a-z0-9-]*(/[a-z][a-z0-9-]*)*$
```

IDs are compared with `StringComparer.Ordinal`. Do not trim, case-fold, or
Unicode-normalize a supplied ID; reject noncanonical input with a diagnostic.
Every record ID is globally unique across record collections.

Transaction IDs use `tx:` followed by a lower-case UUIDv7 string. Inject the
generator so tests are deterministic.

### 4.2 Common record contract

Every addressable canonical record contains:

```csharp
public interface IWorldRecord
{
    CanonicalId Id { get; }
    int Revision { get; }
}
```

`Revision` starts at 1 and increments exactly once on each committed replacement
of that record. It is unrelated to the world revision. Clients never choose the
next revision; transaction application does.

Records may contain `Extensions`, an immutable map keyed by a reverse-DNS or
project namespace such as `com.example.unity`. Extension payloads are JSON-owned
DTO data and should not leak `JsonElement` into Core. For the POC, represent a
payload as a recursively immutable neutral value tree. Unknown top-level record
properties are errors.

### 4.3 Typed values

Do not use `object`, untagged `JsonElement`, or floating point for proposition
arguments and state.

```csharp
public abstract record WorldValue
{
    public sealed record Entity(CanonicalId EntityId) : WorldValue;
    public sealed record Text(string Value) : WorldValue;
    public sealed record Integer(long Value) : WorldValue;
    public sealed record Decimal(decimal Value) : WorldValue;
    public sealed record Boolean(bool Value) : WorldValue;
    public sealed record Symbol(string Value) : WorldValue;
}
```

`Symbol` uses the same lower-kebab segment rule without a colon. Calendar and
duration types are deferred until a real sample requires them; fictional time in
the POC uses time-point IDs and ordinals.

JSON values are explicitly tagged:

```json
{ "entity": "character:clarisse" }
{ "text": "a folded blue letter" }
{ "integer": 12 }
{ "decimal": "12.50" }
{ "boolean": true }
{ "symbol": "alive" }
```

Decimals serialize as invariant strings to prevent cross-platform formatting or
binary rounding changes.

### 4.4 World header and snapshot

```csharp
public sealed record WorldSnapshot(
    WorldHeader Header,
    ImmutableArray<EntityDefinition> Entities,
    ImmutableArray<PredicateDefinition> Predicates,
    ImmutableArray<PropositionDefinition> Propositions,
    ImmutableArray<CanonAssertion> Assertions,
    ImmutableArray<PerspectiveRecord> Perspectives,
    ImmutableArray<StateVariableDefinition> StateVariables,
    ImmutableArray<TimelineDefinition> Timelines,
    ImmutableArray<TimePointDefinition> TimePoints,
    ImmutableArray<EventDefinition> Events,
    ImmutableArray<NarrativeGraph> Narratives,
    ImmutableArray<NarrativeNode> NarrativeNodes,
    ImmutableArray<NarrativeTransition> NarrativeTransitions,
    ImmutableArray<MysteryDefinition> Mysteries,
    ImmutableArray<DeductionRule> DeductionRules,
    ImmutableArray<ProjectConstraint> Constraints,
    ImmutableArray<DisclosureRule> DisclosureRules,
    ImmutableArray<TextArtifact> TextArtifacts);
```

`WorldHeader` contains:

- `SchemaVersion` (`validatedworld/v1`)
- `WorldId`
- `Title`
- `WorldRevision` (`long`, starts at 0)
- `ParentContentHash` (null only at revision 0)
- `ContentHash`
- `LastCommit` metadata
- `WorldPolicy`

The snapshot exposes arrays for stable serialization. `WorldIndex` in Validation
builds immutable dictionaries once per loaded/projected snapshot for lookup.
Core records must not maintain redundant bidirectional collections such as both
`Location.Residents` and `Character.Home`; express one authoritative proposition
and derive the inverse view.

### 4.5 Entities

```csharp
public sealed record EntityDefinition(
    CanonicalId Id,
    int Revision,
    string Kind,
    string DisplayName,
    ImmutableArray<string> Aliases,
    string? Description,
    ImmutableArray<string> Tags,
    ImmutableArray<CanonicalId> Links,
    ExtensionMap Extensions) : IWorldRecord;
```

`Kind` is a lower-kebab symbol. Built-in kinds initially include `character`,
`location`, `faction`, `item`, `document`, `clue`, `audience`, and `scene`.
Unknown kinds are allowed because kind alone has no behavior. Predicate schemas
can constrain entity arguments to allowed kinds.

### 4.6 Predicate definitions

```csharp
public sealed record PredicateDefinition(
    CanonicalId Id,
    int Revision,
    ImmutableArray<PredicateArgument> Arguments,
    ImmutableArray<string> CardinalityKeyRoles,
    int? MaximumPositiveValuesPerKey,
    WorldAssumption Assumption,
    bool Symmetric,
    CanonicalId? InversePredicateId) : IWorldRecord;

public sealed record PredicateArgument(
    string Role,
    ValueTypeDefinition ValueType,
    ImmutableArray<string> AllowedEntityKinds);
```

POC behavior:

- `MaximumPositiveValuesPerKey` may be null or 1. Values above 1 are rejected as
  unsupported rather than ignored.
- Symmetry is used for canonical duplicate/conflict detection; it does not infer
  new authored records.
- Inverse metadata supports queries and duplicate diagnostics. The index may
  derive inverse edges but the serializer does not emit inferred propositions.
- `WorldAssumption` is `Open` or `Closed`. Closed predicates are permitted only
  in finite state/query contexts and reports disclose their use.

### 4.7 Propositions and assertions

```csharp
public sealed record PropositionDefinition(
    CanonicalId Id,
    int Revision,
    CanonicalId PredicateId,
    ImmutableArray<NamedArgument> Arguments,
    string? Gloss) : IWorldRecord;

public sealed record NamedArgument(string Role, WorldValue Value);

public enum Polarity { Positive, Negative }

public sealed record CanonAssertion(
    CanonicalId Id,
    int Revision,
    CanonicalId PropositionId,
    Polarity Polarity,
    WorldScope Scope,
    ImmutableArray<CanonicalId> EvidenceRefs,
    string? AuthorNote) : IWorldRecord;
```

Arguments are a set keyed by role but serialize in predicate-declared role order.
No role may be omitted or repeated. Extra roles are errors.

`WorldScope` is:

```csharp
public sealed record WorldScope(
    CanonicalId? TimelineId,
    CanonicalId? FromTimePointId,
    CanonicalId? UntilTimePointId);
```

Intervals are half-open `[from, until)`. Null start/end means negative/positive
infinity. A scope with no timeline is global and overlaps every timeline. A
timeline scope requires all non-null time points to belong to that timeline.
`from` must have a lower ordinal than `until`.

Schema v1 has no separate scenario or alternate-timeline scope. Branch-specific
truth is represented in narrative abstract state. A later scenario feature must
extend scope comparison explicitly; a v1 reader rejects rather than ignores it.

### 4.8 Perspectives and provenance

```csharp
public enum PerspectiveAttitude
{
    Knows,
    Believes,
    Suspects,
    Doubts,
    Denies,
    Claims,
    Unaware
}

public enum InformationSourceKind
{
    AuthorDeclaration,
    Witness,
    ToldBy,
    Document,
    Clue,
    Deduction,
    PublicKnowledge
}

public sealed record InformationSource(
    InformationSourceKind Kind,
    CanonicalId? SourceRecordId,
    CanonicalId? AtTimePointId,
    ImmutableArray<CanonicalId> SupportingRecordIds);

public sealed record PerspectiveRecord(
    CanonicalId Id,
    int Revision,
    CanonicalId HolderEntityId,
    CanonicalId PropositionId,
    Polarity? Polarity,
    PerspectiveAttitude Attitude,
    WorldScope Scope,
    InformationSource Source) : IWorldRecord;
```

Rules:

- `Unaware` has null polarity. Every other attitude requires polarity.
- `Knows` is factive: matching canon truth must cover the knowledge interval.
- `Unaware` conflicts in overlapping scope with `Knows`, `Believes`, `Suspects`,
  `Doubts`, or `Denies` for the same holder and proposition. It does not by itself
  forbid a one-time accidental `Claims` speech act.
- Absence of a perspective record is unknown, not unaware.
- `AuthorDeclaration` is valid provenance but should be visible in reports so a
  project can require stronger provenance for selected mysteries.

### 4.9 State variables

Use state variables for finite, frequently changing values needed by narrative
analysis.

```csharp
public sealed record StateVariableDefinition(
    CanonicalId Id,
    int Revision,
    CanonicalId? OwnerEntityId,
    string Name,
    ImmutableArray<WorldValue> Domain,
    WorldValue InitialValue) : IWorldRecord;
```

The domain must be nonempty, contain unique values under structural equality,
and include the initial value. Effects can only assign a value from the declared
domain. Examples include `character:clarisse/life-state`, quest phase, or whether
a bridge is passable.

Do not mirror the same mutable concept as both a state variable and overlapping
canon assertions in the same narrative analysis unless a validator explicitly
defines their synchronization.

### 4.10 Timelines and events

```csharp
public sealed record TimePointDefinition(
    CanonicalId Id,
    int Revision,
    CanonicalId TimelineId,
    int Ordinal,
    string Label) : IWorldRecord;

public sealed record TimelineDefinition(
    CanonicalId Id,
    int Revision,
    string DisplayName) : IWorldRecord;

public sealed record EventDefinition(
    CanonicalId Id,
    int Revision,
    CanonicalId TimelineId,
    CanonicalId TimePointId,
    ImmutableArray<CanonicalId> ParticipantEntityIds,
    CanonicalId? LocationEntityId,
    Condition Preconditions,
    ImmutableArray<Effect> Effects,
    ImmutableArray<CanonicalId> PrerequisiteEventIds) : IWorldRecord;
```

Time-point records point to their owning timeline. Their ordinals are unique
within a timeline and need not be contiguous.
Display labels such as `Day 12, evening` are not parsed for ordering. Event
prerequisites must be in the same timeline and strictly earlier for the POC.

### 4.11 Text artifacts

```csharp
public sealed record TextArtifact(
    CanonicalId Id,
    int Revision,
    TextStorage Storage,
    CanonicalId? NarrativeNodeId,
    CanonicalId? PointOfViewEntityId,
    ImmutableArray<CanonicalId> ReferencedRecordIds,
    ImmutableArray<SignedProposition> AssertedPropositions,
    ImmutableArray<DisclosureAnnotation> Disclosures) : IWorldRecord;
```

`TextStorage` is either inline text or a workspace-relative UTF-8 file path. Paths
must be normalized, may not be rooted, and may not escape the workspace. File
contents are hashed when building a context/export packet. The canonical
snapshot is authoritative for annotations; external prose is not silently
parsed into canon.

## 5. Condition and effect AST

Represent every case as a sealed record derived from an abstract record. JSON
uses a required `kind` discriminator and rejects unknown cases for schema v1.

### 5.1 Conditions

```csharp
public abstract record Condition
{
    public sealed record Constant(bool Value) : Condition;
    public sealed record All(ImmutableArray<Condition> Items) : Condition;
    public sealed record Any(ImmutableArray<Condition> Items) : Condition;
    public sealed record Not(Condition Item) : Condition;
    public sealed record PropositionHolds(SignedProposition Proposition) : Condition;
    public sealed record StateEquals(CanonicalId VariableId, WorldValue Value) : Condition;
    public sealed record HasPerspective(
        CanonicalId HolderId,
        CanonicalId PropositionId,
        Polarity? Polarity,
        PerspectiveAttitude Attitude) : Condition;
    public sealed record HasFlag(CanonicalId FlagId) : Condition;
}

public sealed record SignedProposition(
    CanonicalId PropositionId,
    Polarity Polarity);
```

Empty `All` evaluates true. Empty `Any` evaluates false. Condition nesting depth
is limited by policy (default 64) during load to avoid stack/resource abuse.

### 5.2 Three-valued evaluation

Conditions over open-world propositions and perspectives evaluate to `True`,
`False`, or `Unknown` using strong Kleene logic:

```text
not(true) = false
not(false) = true
not(unknown) = unknown

all = false if any child is false
      unknown if none is false and any is unknown
      true otherwise

any = true if any child is true
      unknown if none is true and any is unknown
      false otherwise
```

State variables and flags are closed within an abstract state, so their tests are
always true or false. A transition or entry condition is enabled only when it is
`True`. A selected linear trace whose required condition is `False` or `Unknown`
is invalid; the diagnostic distinguishes contradiction from insufficient facts.

### 5.3 Effects

```csharp
public abstract record Effect
{
    public sealed record SetState(CanonicalId VariableId, WorldValue Value) : Effect;
    public sealed record SetProposition(SignedProposition Proposition) : Effect;
    public sealed record RemoveProposition(CanonicalId PropositionId) : Effect;
    public sealed record AddPerspective(PerspectiveValue Perspective) : Effect;
    public sealed record RemovePerspective(
        CanonicalId HolderId,
        CanonicalId PropositionId,
        PerspectiveAttitude? Attitude) : Effect;
    public sealed record AddFlag(CanonicalId FlagId) : Effect;
    public sealed record AcquireEvidence(CanonicalId EvidenceEntityId) : Effect;
}
```

Effects are idempotent assignments/set operations. `SetProposition` replaces any
existing polarity for that proposition in abstract story state. Removing a
proposition makes it unknown; it does not assert the opposite. Effects apply in
array order, and validators warn when two effects in one list overwrite the same
key because that usually indicates author error.

`AddPerspective` writes the same holder/proposition/attitude overlay key and
rejects an explicit `Unaware` conflict. `RemovePerspective` writes a tombstone;
with null attitude it writes one tombstone for every attitude for that
holder/proposition pair.

When validating a presentation-only node, reject `SetState`, `SetProposition`,
and `RemoveProposition`. Transition effects entering or leaving such a node are
still branch effects and are not presentation-only; authors who need a pure
flashback path should keep world-changing effects off those transitions.

### 5.4 AST type checking

Before evaluation, walk every AST and verify:

1. All referenced records exist and have compatible categories.
2. State values belong to the declared domain.
3. Proposition and perspective polarities satisfy their case rules.
4. Evidence references have entity kind `clue`, `document`, or an allowed custom
   evidence kind.
5. No AST exceeds configured node count or nesting depth.

Never catch a type failure during traversal and continue with a guessed value.

## 6. Narrative and mystery model

### 6.1 Narrative graphs

```csharp
public sealed record NarrativeGraph(
    CanonicalId Id,
    int Revision,
    string Kind,
    CanonicalId TimelineId,
    CanonicalId StartTimePointId,
    ImmutableArray<CanonicalId> EntryNodeIds,
    ImmutableArray<CanonicalId> SelectedTraceNodeIds,
    AnalysisLimits? LimitsOverride) : IWorldRecord;

public sealed record NarrativeNode(
    CanonicalId Id,
    int Revision,
    CanonicalId NarrativeGraphId,
    string Kind,
    string DisplayName,
    int Stage,
    CanonicalId? DepictedTimePointId,
    bool PresentationOnly,
    Condition EntryCondition,
    ImmutableArray<Effect> Effects,
    ImmutableArray<CanonicalId> ReferencedRecordIds,
    ImmutableArray<DisclosureAnnotation> Disclosures,
    bool IsTerminal) : IWorldRecord;

public sealed record NarrativeTransition(
    CanonicalId Id,
    int Revision,
    CanonicalId NarrativeGraphId,
    CanonicalId FromNodeId,
    CanonicalId ToNodeId,
    Condition Condition,
    ImmutableArray<Effect> Effects,
    string? Label) : IWorldRecord;
```

Nodes and transitions are top-level canonical records so they can be changed and
revision-checked independently. Each belongs to exactly one graph. A
transition's endpoints must be nodes in that same graph. Graph membership lists
are derived from `NarrativeGraphId`; the graph stores only its entry points and
selected trace to avoid redundant collections.

`Stage` is a nonnegative narrative/disclosure ordinal. Transitions may stay at
the same stage or increase it; decreasing stage is an error. Stage is unrelated
to fictional time and exists so branching narratives can state reveal windows
without inventing one total scene order.

`DepictedTimePointId` is the fictional time at which a presented scene occurs;
null inherits the graph start point. It may move backward while stage increases,
which represents a flashback. A backward-time target must be
`PresentationOnly`. Presentation-only nodes may change audience perspectives,
evidence, and narrative flags, but may not set world/story state variables or
proposition overrides. Thus a flashback can teach the reader something without
rewinding or mutating the current branch state.

The selected trace is optional. When present, it is an ordered list beginning at
an entry node. Each adjacent pair must have exactly one selected transition. If
multiple transitions connect the same pair, the trace format must be upgraded to
store transition IDs before such a world can load; schema v1 rejects the
ambiguity.

### 6.2 Abstract narrative state

```csharp
public sealed record NarrativeState(
    ImmutableDictionary<CanonicalId, WorldValue> StateValues,
    ImmutableDictionary<CanonicalId, Polarity> PropositionOverrides,
    ImmutableDictionary<PerspectiveKey, PerspectiveValue?> PerspectiveOverrides,
    ImmutableHashSet<CanonicalId> Flags,
    ImmutableHashSet<CanonicalId> AcquiredEvidence);
```

The evaluator obtains canon assertions and baseline perspectives at each node's
effective depicted time. Story-state overrides take precedence. A null
perspective override is a tombstone that masks a baseline attitude; a non-null
value adds/replaces it. Removing all attitudes expands to one tombstone per
finite attitude enum value, and a later add replaces its tombstone. The analyzer
includes only state variables, propositions, perspective keys, flags, and
evidence referenced by the graph or applicable constraints. This projection is
sound only because the condition/effect AST cannot inspect omitted state.

`PerspectiveKey` contains holder, proposition, and attitude.
`PerspectiveValue` additionally contains the required polarity. Provenance for a
runtime acquisition is stored in a separate trace event and does not affect
state equality.

### 6.3 Project constraints

Schema v1 uses a closed union:

```csharp
public abstract record ProjectConstraint : IWorldRecord
{
    public sealed record MustReachNode(...);
    public sealed record MustReachAnyTerminal(...);
    public sealed record MustNotReachNode(...);
    public sealed record ForbiddenDisclosureBeforeStage(...);
    public sealed record RequiredFallback(...);
    public sealed record MinimumEvidenceRoutes(...);
    public sealed record RequiredPerspectiveAtNode(...);
}
```

Every constraint includes `Id`, `Revision`, `NarrativeGraphId`, `Severity`, and a
human-readable rationale. Exact case payloads:

- `MustReachNode`: target node ID.
- `MustReachAnyTerminal`: optional allowed terminal node IDs; empty means any.
- `MustNotReachNode`: target node ID plus optional condition that defines the
  forbidden analyzed state.
- `ForbiddenDisclosureBeforeStage`: audience, signed proposition, minimum stage.
- `RequiredFallback`: unavailable condition, original path node, and at least one
  fallback target node that must remain reachable when the condition holds.
- `MinimumEvidenceRoutes`: evidence entity and minimum distinct acquisition
  nodes. Schema v1 verifies distinct reachable acquisition nodes, not formal
  graph-disjointness; the report names this limited coverage.
- `RequiredPerspectiveAtNode`: node, holder, proposition, polarity, attitude.

Do not add a general expression-to-expression assertion language during the POC.
A new constraint case requires a deterministic evaluator, diagnostics, and
tests.

### 6.4 Disclosure rules

Export authorization is separate from in-story disclosure. A character learning
a secret does not make it public source material.

```csharp
public enum DisclosureAccess
{
    DeveloperOnly,
    Public,
    AudienceAllowList
}

public sealed record DisclosureRule(
    CanonicalId Id,
    int Revision,
    CanonicalId TargetRecordId,
    DisclosureAccess Access,
    ImmutableArray<CanonicalId> AllowedAudienceEntityIds,
    CanonicalId? NarrativeGraphId,
    int? MinimumStage,
    string? PublicGloss) : IWorldRecord;
```

Schema v1 permits at most one disclosure rule per target record. The safe default
for audience-facing export is hidden: a target without an applicable `Public` or
allow-list rule is omitted. Developer exports can include all records.
`PublicGloss` is optional replacement text for a hidden dependency; it is never
parsed as canon.

Rules with `MinimumStage` require a narrative graph and apply only at or after
that stage. Audience allow lists contain entity kind `audience`. A rule is
itself developer-only metadata and cannot authorize its own export.

### 6.5 Mystery definitions

```csharp
public sealed record MysteryDefinition(
    CanonicalId Id,
    int Revision,
    CanonicalId NarrativeGraphId,
    CanonicalId AudienceEntityId,
    ImmutableArray<SignedProposition> Solution,
    ImmutableArray<CanonicalId> EvidenceEntityIds,
    int EarliestSolutionStage,
    ImmutableArray<CanonicalId> RequiredSolvableAtNodeIds,
    int MinimumRoutesPerRequiredEvidence) : IWorldRecord;

public sealed record DeductionRule(
    CanonicalId Id,
    int Revision,
    CanonicalId MysteryId,
    ImmutableArray<DeductionPremise> Premises,
    SignedProposition Conclusion,
    string Explanation) : IWorldRecord;

public abstract record DeductionPremise
{
    public sealed record Evidence(CanonicalId EvidenceEntityId) : DeductionPremise;
    public sealed record Proposition(SignedProposition Value) : DeductionPremise;
}
```

Deduction rules are top-level canonical records owned through `MysteryId`, which
allows fine-grained transaction operations. A rule has at least one premise.
Rules are monotonic: they only add signed conclusions. Opposite
conclusions in one closure are a deterministic contradiction diagnostic.

The audience's available proposition set contains propositions explicitly added
to that audience's perspective in narrative state with attitude `Knows` or
`Believes`, plus deduction conclusions. Canon truth is not automatically reader
knowledge.

## 7. Canonical JSON contract

The schema is implemented first as strict DTO parsing and validation. A generated
JSON Schema may be added after the DTO is stable; it does not replace domain
validation.

### 7.1 Top-level shape

```json
{
  "schemaVersion": "validatedworld/v1",
  "world": {
    "id": "world:harbor-mystery",
    "title": "The Harbor Ledger",
    "revision": 3,
    "parentContentHash": "sha256:...",
    "contentHash": "sha256:...",
    "lastCommit": {
      "transactionId": "tx:00000000-0000-7000-8000-000000000001",
      "intent": "Add the torn-letter clue",
      "committedAt": "2026-08-10T20:00:00.0000000Z",
      "author": "agent"
    },
    "policy": {
      "maxConditionDepth": 64,
      "maxAstNodesPerRecord": 4096,
      "maxNarrativeStates": 100000,
      "maxNarrativeTransitions": 1000000,
      "maxNarrativeDepth": 10000,
      "analysisTimeoutMilliseconds": 30000,
      "blockOnRequiredInconclusive": true
    }
  },
  "entities": [],
  "predicates": [],
  "propositions": [],
  "assertions": [],
  "perspectives": [],
  "stateVariables": [],
  "timelines": [],
  "timePoints": [],
  "events": [],
  "narratives": [],
  "narrativeNodes": [],
  "narrativeTransitions": [],
  "mysteries": [],
  "deductionRules": [],
  "constraints": [],
  "disclosureRules": [],
  "textArtifacts": []
}
```

All collections are required, even when empty. Canonical serialization orders
each collection by record ID using ordinal comparison. Tags, links, and reference
sets have documented
ordering:

- Semantically unordered collections sort by canonical ID or ordinal string.
- Semantically ordered collections retain authored order (`Arguments`, effects,
  selected trace, deduction premises).
- Duplicate values are rejected rather than silently deduplicated.

### 7.2 Representative records

```json
{
  "id": "predicate:killed",
  "revision": 1,
  "arguments": [
    {
      "role": "killer",
      "type": "entity",
      "allowedEntityKinds": ["character"]
    },
    {
      "role": "victim",
      "type": "entity",
      "allowedEntityKinds": ["character"]
    }
  ],
  "cardinalityKeyRoles": ["victim"],
  "maximumPositiveValuesPerKey": 1,
  "assumption": "open",
  "symmetric": false,
  "inversePredicateId": null
}
```

```json
{
  "id": "proposition:mayor-killed-merchant",
  "revision": 1,
  "predicateId": "predicate:killed",
  "arguments": [
    { "role": "killer", "value": { "entity": "character:mayor" } },
    { "role": "victim", "value": { "entity": "character:merchant" } }
  ],
  "gloss": "The mayor killed the merchant."
}
```

```json
{
  "id": "assertion:mayor-killed-merchant/true",
  "revision": 1,
  "propositionId": "proposition:mayor-killed-merchant",
  "polarity": "positive",
  "scope": {
    "timelineId": "timeline:main",
    "fromTimePointId": "time:murder",
    "untilTimePointId": null
  },
  "evidenceRefs": ["event:merchant-murder"],
  "authorNote": null
}
```

```json
{
  "id": "perspective:guard/bandit-theory",
  "revision": 1,
  "holderEntityId": "character:guard",
  "propositionId": "proposition:bandits-killed-merchant",
  "polarity": "positive",
  "attitude": "believes",
  "scope": {
    "timelineId": "timeline:main",
    "fromTimePointId": "time:body-found",
    "untilTimePointId": null
  },
  "source": {
    "kind": "told-by",
    "sourceRecordId": "character:mayor",
    "atTimePointId": "time:body-found",
    "supportingRecordIds": []
  }
}
```

### 7.3 Canonical hashing

Hashing must not be implemented by serializing the DTO including its existing
hash. Define one canonical writer and one hash projection.

```text
hash projection = complete canonical snapshot
                  with world.contentHash omitted
UTF-8 encode without BOM
SHA-256 bytes
lower-case hex with "sha256:" prefix
```

The projection includes world revision, parent hash, last-commit metadata,
policy, and all content. Given the same domain snapshot and injected commit
metadata, it must produce identical bytes on every supported platform.

During load:

1. Parse the file strictly.
2. Map to the domain snapshot.
3. Recompute the hash projection.
4. Compare using ordinal, constant-time byte comparison after hex decoding.
5. Emit a blocking integrity diagnostic on mismatch.

Revision 0 has null parent hash. Revision N must have a non-null parent hash, but
the POC does not require all ancestors to remain available.

### 7.4 Strict JSON reader

`System.Text.Json` alone must not silently accept duplicate object properties.
Before DTO deserialization, scan with `Utf8JsonReader`:

```text
push empty ordinal property-name set on StartObject
on PropertyName:
    if name already exists in top set -> duplicate-property diagnostic
    else add name
pop set on EndObject
enforce maximum depth and input-byte policy
```

Then deserialize with:

- case-sensitive property names;
- no comments;
- no trailing commas;
- strict number handling;
- explicit converters for every union;
- unmapped-member rejection;
- maximum depth from a host safety ceiling.

The parser records JSON Pointer plus byte offset for each addressable record and
important field. Diagnostics should include that location. Line/column can be
computed lazily from the original UTF-8 bytes.

### 7.5 Neutral extension values

An extension value is one of null, Boolean, integer, decimal string, text, array,
or object with ordinal keys. Impose depth, node-count, and byte limits. Extension
objects live only under the `extensions` property and each top-level key must
contain a dot. Core validation preserves unknown namespaces but cannot certify
their semantics; export coverage states that fact.

## 8. Snapshot indexing

Build one `WorldIndex` after structural record parsing and global ID collection.

```csharp
public sealed class WorldIndex
{
    public WorldSnapshot Snapshot { get; }
    public ImmutableDictionary<CanonicalId, IWorldRecord> RecordsById { get; }
    public ImmutableDictionary<CanonicalId, EntityDefinition> EntitiesById { get; }
    public ImmutableDictionary<CanonicalId, PredicateDefinition> PredicatesById { get; }
    // equivalent typed maps
    public ImmutableDictionary<CanonicalId, TimePointDefinition> TimePointsById { get; }
}
```

Index construction returns all duplicate IDs instead of throwing on the first.
When duplicates exist, keep no arbitrary winner in `RecordsById`; mark the ID
ambiguous so downstream validators skip dependent rules with an explicit
prerequisite diagnostic.

Build derived indexes only from valid unambiguous records:

- propositions by predicate;
- assertions by proposition;
- perspectives by holder and proposition;
- events by timeline/ordinal;
- nodes/transitions by graph;
- deduction rules by mystery;
- constraints/mysteries/disclosure rules by graph or target.

Never serialize an index as canon. It is a deterministic cache.

## 9. Transaction model and algorithms

### 9.1 Transaction records

```csharp
public sealed record WorldTransaction(
    TransactionId Id,
    WorldId WorldId,
    long BaseWorldRevision,
    string BaseContentHash,
    string Intent,
    string Author,
    DateTimeOffset CreatedAt,
    TransactionStatus Status,
    ImmutableArray<WorldOperation> Operations,
    ImmutableArray<DiagnosticAcknowledgement> Acknowledgements);

public abstract record WorldOperation
{
    public required CanonicalId TargetId { get; init; }

    public sealed record Add(IWorldRecordDraft Record) : WorldOperation;
    public sealed record Replace(int ExpectedRevision, IWorldRecordDraft Record)
        : WorldOperation;
    public sealed record Remove(int ExpectedRevision) : WorldOperation;
}
```

Use serialization DTOs for the union. `IWorldRecordDraft` represents a complete
record without a client-controlled record revision. The operation target ID must
match the record payload ID.

One transaction may contain at most one final operation per target ID. Editing a
draft replaces its existing operation; importing a transaction file containing
duplicates is an error. This avoids order-sensitive add/replace/remove chains.

Transactions are stored outside canon under `.validatedworld/transactions/`.
Draft storage can be rewritten because it is not canonical history. A committed
snapshot embeds last-commit metadata; an optional immutable receipt is generated
after commit.

### 9.2 Applying operations

Apply operations in target-ID ordinal order, not input order, because operations
are defined as independent final changes.

```text
Apply(baseSnapshot, transaction):
    require transaction world/base revision/base hash match baseSnapshot
    builders = mutable copies of each record collection keyed by ID

    for operation sorted by TargetId:
        current = lookup TargetId across all builders

        Add:
            require current is absent
            validate payload ID/category at draft level
            materialize record with Revision = 1
            insert into correct builder

        Replace:
            require current exists
            require current.Revision == ExpectedRevision
            require payload record category equals current category
            materialize record with Revision = ExpectedRevision + 1
            replace

        Remove:
            require current exists
            require current.Revision == ExpectedRevision
            remove only that record; never cascade

    materialize arrays in canonical order
    set provisional world revision = base + 1
    set parent hash = base content hash
    leave new content hash unset until commit metadata is supplied
    return ProjectedSnapshot + changed ID set + operation diagnostics
```

Broken references created by a removal are reported by snapshot validation. This
is important: explicit repairs can coexist in the same transaction.

### 9.3 Draft validation

Draft validation may run after each operation and may tolerate expected temporary
errors. It still returns normal diagnostics. A caller chooses whether to display
only diagnostics changed since the previous draft. No diagnostic is permanently
suppressed merely because it occurred during drafting.

### 9.4 Acknowledgements

```csharp
public sealed record DiagnosticAcknowledgement(
    string DiagnosticFingerprint,
    string Reason,
    string AcknowledgedBy,
    DateTimeOffset AcknowledgedAt);
```

The fingerprint is:

```text
SHA-256(rule code + rule version + normalized primary ID +
        sorted related IDs + normalized evidence values)
```

Messages and source line numbers are excluded. If evidence changes, the
fingerprint changes and the acknowledgement no longer applies. Errors, internal
failures, and required inconclusive results are never acknowledgement-eligible.

### 9.5 Commit orchestration

`CommitTransactionHandler` owns this sequence:

```text
Commit(transactionId):
    load transaction
    require Draft status
    acquire exclusive workspace commit lock
    read canonical bytes and load verified base snapshot

    if base revision/hash differs from transaction:
        return Conflict; do not mutate transaction or canon

    projected = Apply(base, transaction)
    if operation diagnostics contain errors:
        persist latest report; return Rejected

    baseGraph = BuildDependencyGraph(base)
    projectedIndex = BuildWorldIndex(projected)
    projectedGraph = BuildDependencyGraph(projected)
    impact = ComputeImpact(changedIds, baseGraph, projectedGraph)
    report = ValidateFull(projected, projectedIndex, policy)
    decision = EvaluateCommitPolicy(report, acknowledgements, risk categories)

    if approval required:
        return ApprovalRequired with no mutation
    if decision blocks:
        persist latest report; return Rejected

    committedAt = clock.UtcNow
    populate LastCommit and final revision metadata
    canonicalBytes = SerializeAndHash(projected)
    atomically replace canonical file while lock remains held
    mark transaction committed with result hash/revision
    best-effort write immutable receipt derived from committed snapshot
    return Committed
```

If canonical replacement succeeds but updating the draft transaction file fails,
the snapshot remains committed and is authoritative. On next load, reconcile any
transaction whose ID matches `LastCommit`. Return a committed result plus an
audit-repair warning rather than pretending canon rolled back.

### 9.6 Atomic file store

POC workspace layout:

```text
<workspace>/
  world.vw.json
  prose/                         optional source text
  .validatedworld/
    commit.lock
    transactions/<tx-id>.json
    receipts/<revision>-<tx-id>.json
    reports/<tx-id>.json
```

Commit steps for an existing canonical file:

1. Resolve and verify the workspace root and exact canonical path.
2. Create a unique temp file in the canonical file's directory (same volume).
3. Write canonical UTF-8 bytes without BOM through a `FileStream`.
4. Call `Flush(flushToDisk: true)` and close the stream.
5. Re-read and hash the temp file to detect write corruption.
6. Use `File.Replace(temp, canonical, backup)` where supported.
7. Remove the backup only after the new canonical file reopens and verifies.

For initial creation, use an exclusive create followed by same-directory atomic
move. Wrap platform operations behind `IAtomicFileWriter`. Its contract states
whether atomic replacement is supported; inability to provide it is a blocking
host diagnostic.

Recovery on startup:

- If canonical verifies, it wins; remove only known stale temp files after their
  names and paths are safely validated.
- If canonical fails and a same-commit backup verifies, report recovery available
  and require an explicit recover command. Do not silently roll back.
- Never select the newest arbitrary file by timestamp.

Tests inject failures before write, during write, after flush, before replace,
after replace, and during receipt update.

### 9.7 Concurrency and rebase

The POC does not auto-merge. A stale transaction returns both base and head
revision/hash. A later `tx rebase` use case may replay its operations against the
new head and report record-revision conflicts, but it must create a new draft
base and rerun full validation. It never bypasses operation preconditions.

## 10. Dependency graph

### 10.1 Graph representation

```csharp
public enum DependencyKind
{
    TypedReference,
    PropositionPredicate,
    PropositionArgument,
    ConditionReference,
    EffectReference,
    TemporalReference,
    ProvenanceReference,
    ConstraintReference,
    AnnotationReference,
    Containment
}

public sealed record DependencyEdge(
    CanonicalId DependentId,
    CanonicalId DependencyId,
    DependencyKind Kind,
    string FieldPath);
```

Store forward and reverse adjacency as immutable sorted arrays. Multiple edge
kinds/paths between the same pair are retained for explanation but traversal
deduplicates destination nodes.

### 10.2 Edge extraction

Use explicit visitors for each record and AST case. Reflection-based crawling is
not authoritative because it cannot distinguish IDs from labels and can miss
semantic ownership.

Examples:

```text
proposition -> predicate                         PropositionPredicate
proposition -> entity-valued argument            PropositionArgument
assertion -> proposition/time point/timeline     Typed/TemporalReference
perspective -> holder/proposition/source         Typed/ProvenanceReference
event -> condition/effect/participant/location   appropriate typed edge
time point -> owning timeline                    Containment
node/transition -> owning narrative graph        Containment
node/transition -> AST and annotation refs        Condition/Effect/Annotation
mystery -> graph/audience/solution/evidence/rule  Typed/ConstraintReference
deduction rule -> owning mystery/premises         Containment/TypedReference
disclosure rule -> target/audience/graph           ConstraintReference
text artifact -> cited IDs/propositions/node      AnnotationReference
```

Ownership normally points from child to parent. For impact traversal, derived
parent-to-child edges are also added for timeline/time-point, graph/node/
transition, and mystery/deduction-rule ownership. Mark the two directions with
separate edge records so a change to either impacts the semantic aggregate. The
graph visitor documents these deliberate cycles.

Unknown or ambiguous references still produce attempted edges to the referenced
ID so diagnostics and impact explanations remain useful.

### 10.3 Impact algorithm

Use base and projected graphs so deleted and newly introduced edges both count.

```text
ComputeImpact(changedIds, baseGraph, projectedGraph):
    reverseUnion[node] = distinct sorted union of
                         baseGraph.reverse[node] and projectedGraph.reverse[node]
    visited = sorted set(changedIds)
    queue = changedIds sorted ordinal
    predecessor = empty map

    while queue not empty:
        current = dequeue
        for dependent in reverseUnion[current] sorted ordinal:
            if visited.Add(dependent):
                predecessor[dependent] = current + explaining edge(s)
                enqueue(dependent)

    return changedIds, visited - changedIds, predecessor explanations
```

Breadth-first traversal yields a shortest dependency-hop explanation. Cycles are
safe because of `visited`. For each impacted record, retain all edges on the
selected predecessor hop so `why` can show field paths.

Impact is advisory scope, not a validity result. Full commit validation still
runs in the POC.

### 10.4 Query semantics

Implement exact queries before a general query language:

- get record by ID;
- list records by record category/entity kind/tag;
- direct dependencies/dependents;
- transitive dependencies/dependents with max depth;
- impact for a transaction;
- explain shortest dependency path;
- truth of a proposition at timeline/time point;
- perspective and provenance trace;
- narrative reachability/counterexample.

Every traversal accepts explicit maximum depth/result count. Truncation is
reported in metadata and never silently applied.

## 11. Validation engine

### 11.1 Contracts

```csharp
public interface IWorldValidator
{
    string RuleCode { get; }
    int RuleVersion { get; }
    ValidationPhase Phase { get; }
    ValueTask<ImmutableArray<Diagnostic>> ValidateAsync(
        ValidationContext context,
        CancellationToken cancellationToken);
}

public sealed record ValidationContext(
    WorldSnapshot Snapshot,
    WorldIndex Index,
    DependencyGraph Dependencies,
    WorldPolicy Policy,
    ValidationMode Mode);
```

Validators return findings; they do not throw for invalid world data. The engine
catches unexpected exceptions at the validator boundary, emits one internal
failure diagnostic with rule code, and marks the phase incomplete. Cancellation
returns an inconclusive report.

Register validators explicitly in deterministic order. After all results arrive,
sort diagnostics by:

1. phase ordinal;
2. severity ordinal;
3. rule code ordinal;
4. primary record ID ordinal;
5. normalized source pointer ordinal;
6. fingerprint ordinal.

Do not depend on task completion order.

### 11.2 Report model

```csharp
public sealed record ValidationReport(
    ValidationOutcome Outcome,
    string SnapshotHash,
    ImmutableArray<PhaseResult> Phases,
    ImmutableArray<Diagnostic> Diagnostics,
    CoverageReport Coverage,
    AnalysisStatistics Statistics);
```

Outcome is `ProvenValid`, `Invalid`, or `Inconclusive`. `Invalid` wins if at least
one completed deterministic rule disproves validity. `Inconclusive` applies when
no error was found but a required rule did not complete. A report may contain
both errors and incomplete phases; outcome remains Invalid and phase detail
preserves incompleteness.

Coverage reports:

- count of records by category;
- annotated versus unannotated text artifacts;
- extension namespaces without validators;
- graphs fully explored versus bounded out;
- validators run, skipped, failed, and not applicable;
- use of closed-world predicates;
- external text files missing or hash-unreadable.

### 11.3 Diagnostic code ranges

Reserve ranges and do not recycle a published meaning:

```text
VW10xx  JSON, schema, integrity, migration
VW11xx  IDs, records, references, types
VW12xx  predicates, propositions, assertions, cardinality
VW13xx  timelines, events, scopes
VW14xx  perspectives and provenance
VW15xx  condition/effect/state typing
VW16xx  narrative structure and selected traces
VW17xx  bounded reachability and constraints
VW18xx  mysteries, clues, deductions, disclosure
VW19xx  transactions, policy, commit, storage
VW20xx  exports and coverage
VW90xx  internal failures and unsupported analysis
AIxxxx  AI review concerns; never use VW error codes
```

Initial codes named in tests:

```text
VW1001 invalid JSON
VW1002 duplicate JSON property
VW1003 unsupported schema version
VW1004 content hash mismatch
VW1101 invalid canonical ID
VW1102 duplicate canonical ID
VW1103 missing reference
VW1104 wrong referenced record type/kind
VW1201 predicate argument mismatch
VW1202 overlapping opposite assertions
VW1203 cardinality conflict
VW1301 invalid time interval
VW1302 event prerequisite not earlier
VW1401 factive knowledge lacks matching truth
VW1402 conflicting unawareness/perspective
VW1403 invalid or unavailable provenance
VW1501 invalid condition/effect reference
VW1502 effect value outside state domain
VW1601 invalid narrative transition
VW1602 selected trace precondition false
VW1603 selected trace precondition unknown
VW1701 required node unreachable
VW1702 no reachable terminal
VW1703 narrative analysis limit reached
VW1704 required fallback unreachable
VW1801 solution disclosed too early
VW1802 solution not derivable at required node
VW1803 deduction contradiction
VW1804 insufficient evidence acquisition routes
VW1901 stale transaction base
VW1902 operation precondition failed
VW1903 approval required
VW1904 atomic replacement unavailable/failed
VW2001 incomplete semantic annotation coverage
VW9001 validator internal failure
VW9002 unsupported construct
```

Messages can improve; code meanings and fingerprint evidence contracts remain
stable within schema v1.

## 12. Deterministic validation algorithms

### 12.1 Global identity and reference validation

Perform a first pass over every addressable canonical record:

```text
groups = records grouped by CanonicalId using ordinal equality
for each invalid ID -> VW1101
for each group count > 1 -> one VW1102 naming every source location
```

Then visit every typed reference. If the ID is missing, emit VW1103. If it is
ambiguous, emit a prerequisite/skipped detail linked to VW1102. If it exists but
its category or entity kind violates the field contract, emit VW1104.

Accumulate all safe findings in one run. Do not follow a reference whose target
is missing/ambiguous into a later semantic validator.

### 12.2 Predicate and proposition validation

For each proposition:

1. Resolve one predicate.
2. Build an ordinal map of named arguments; duplicates are VW1201.
3. Compare role set to predicate role set; report missing and extra roles.
4. For each argument, validate `WorldValue` case against the declared type.
5. For entity values, resolve the entity and validate allowed kinds.
6. If symmetric with two equivalent argument types, compute a normalized
   argument ordering for duplicate/conflict comparison only.

Two proposition records with the same predicate and structurally equal normalized
arguments are duplicate semantic content. Emit a warning that recommends one
stable proposition ID; do not silently merge because other records may reference
both.

### 12.3 Scope and interval operations

Convert a scoped assertion to comparable bounds only after resolving its
timeline:

```text
global scope: timeline = wildcard, start = -infinity, end = +infinity
timeline scope: start/end = referenced time-point ordinal or infinity
```

Two scopes overlap when:

```text
timelines overlap if either is wildcard or timeline IDs equal
intervals overlap if max(startA, startB) < min(endA, endB)
```

The half-open rule means `[1, 3)` and `[3, 5)` do not overlap. Invalid/reversed
intervals produce VW1301 and are excluded from contradiction grouping.

To verify that assertions cover an entire knowledge interval:

```text
Coverage(targetInterval, matchingTruthIntervals):
    clip truth intervals to target timeline/interval
    sort by start then end
    cursor = target.start
    for interval:
        if interval.end <= cursor: continue
        if interval.start > cursor: return false with gap [cursor, interval.start)
        cursor = max(cursor, interval.end)
        if cursor >= target.end: return true
    return false with trailing gap
```

Handle infinities with a dedicated bound type, not sentinel integers.

### 12.4 Opposite-assertion validation

Group valid assertions by proposition ID. Sort each group by scope start,
polarity, and assertion ID. For each positive/negative pair whose scopes overlap,
emit VW1202 with the exact overlap range.

The POC may use the clear O(P×N) pair scan per proposition. Add a sweep-line
optimization only after profiling large real data; correctness tests must remain
the same.

### 12.5 Cardinality validation

For each predicate with maximum positive values 1:

1. Select valid positive canon assertions for propositions of that predicate.
2. Construct a structural key from `CardinalityKeyRoles` in declared order.
3. The value portion is all remaining roles in declared order.
4. Group by key.
5. Compare assertions with different value portions.
6. Emit VW1203 for every pair with overlapping scopes.

Example: `located-at(subject, location)` keyed by `subject` permits different
locations at disjoint times but not overlapping times. If all roles are in the
key, duplicate positive assertions are redundant rather than cardinality
conflicts.

### 12.6 Event chronology

For each event:

- Resolve its timeline and time point.
- Every prerequisite must be an event on the same timeline.
- `prerequisite.ordinal < event.ordinal`; equality is invalid.
- Emit VW1302 for each violation.

Also build a prerequisite graph and run deterministic Kahn topological sort. A
cycle receives one diagnostic per strongly connected component. Use Tarjan SCC
with vertices and outgoing edges visited in ordinal ID order so evidence is
stable.

Event condition/effect replay is optional in the first timeline slice, but before
claiming timeline-state proof the implementation must:

```text
state = baseline state variables and assertions at first event
for events ordered by (time ordinal, event ID):
    evaluate preconditions
    false -> error; unknown -> insufficient-information error
    apply effects
    validate resulting state has no opposite proposition/perspective conflict
```

Events at the same time are unordered. If same-time events touch the same state
key or one reads a key another writes, emit an ordering-required diagnostic; do
not select event-ID order as fictional causality.

### 12.7 Perspective validation

For each perspective:

1. Validate holder entity, proposition, scope, and provenance references.
2. Enforce polarity rules.
3. If attitude is `Knows`, select canon assertions for the same proposition and
   polarity whose timelines are compatible; run interval coverage. Emit VW1401
   with uncovered gap if not fully covered.
4. For `Witness`, require a source event, holder participation/witness annotation,
   and compatible acquisition time. More sophisticated line-of-sight is outside
   the POC.
5. For `ToldBy`, require a source character and acquisition time. If project
   policy requires truthful provenance, verify that source had `Knows` or the
   matching `Believes` attitude at that time; otherwise report this provenance as
   author-declared coverage only.
6. For `Document` or `Clue`, require an entity of compatible kind and supporting
   annotation that cites the proposition.
7. For `Deduction`, require supporting records and a matching deduction rule when
   inside a mystery.

Group perspectives by holder/proposition and compare `Unaware` intervals with
knowledge/belief/suspicion/doubt/denial intervals. Any overlap emits VW1402.

Do not recursively infer new perspective records from provenance during POC
validation. Provenance is checked; authored state remains explicit.

### 12.8 Selected trace replay

Selected trace validation gives strong, cheap value for novels.

For every node condition, use `node.DepictedTimePointId` or the graph start point
as the effective fictional time for baseline canon/perspective lookup. Audience
and story overlays still flow in narrative order.

```text
ReplayTrace(graph):
    state = ProjectInitialNarrativeState(graph)
    trace = graph.SelectedTraceNodeIds
    require trace nonempty and trace[0] is entry node

    for i from 0 to trace.length - 1:
        node = trace[i]
        entryResult = Evaluate(node.EntryCondition, state)
        if False: emit VW1602 with state evidence; stop replay past this node
        if Unknown: emit VW1603 with unknown leaf conditions; stop

        state = Apply(node.Effects, state)
        capture state-at-node for mystery/constraint validators

        if i + 1 < trace.length:
            transition = unique transition node -> trace[i+1]
            require it exists
            result = Evaluate(transition.Condition, state)
            False/Unknown -> VW1602/VW1603 against transition; stop
            state = Apply(transition.Effects, state)

    if complete and final node is not terminal when policy requires terminal:
        emit trace terminal diagnostic

    return ordered replay frames and diagnostics
```

Each replay frame includes node, transition, condition leaf results, applied
effects, and before/after state hash. It is serializable and becomes the
counterexample/explanation surface.

### 12.9 Narrative reachability

Use breadth-first search over `(node ID, abstract state)` to find shortest
counterexamples and prove finite reachability.

#### Initial state projection

Collect every state variable, proposition, holder/proposition perspective key,
flag, and evidence ID referenced by:

- graph node/transition conditions and effects;
- graph disclosures;
- applicable constraints;
- mystery rules for this graph.

Populate state-variable initial values. Proposition and perspective overlay maps
start empty; baseline canon is queried lazily at each node's effective depicted
time. Opposite baseline truth at that time is invalid and absence stays unknown.
Flags/evidence start absent unless an explicit graph initialization feature is
later added.

#### BFS

```text
Analyze(graph, limits, cancellation):
    queue = empty FIFO
    visited = hash set of StateKey
    predecessor = map StateKey -> (prior key, transition ID)
    reachedNodes = map node ID -> list/state summary
    counters = zero
    started = monotonic clock

    for entry node sorted by ID:
        if entry condition True in initial state:
            entered = Apply(entry effects, initial state)
            Enqueue(entry, entered, predecessor = none)

    while queue not empty:
        check cancellation, elapsed time, states, transitions, depth limits
        if any limit reached: return Inconclusive + VW1703 + partial results

        current = dequeue
        record node reached

        for transition from current.node sorted by transition ID:
            counters.transitions++
            if condition is not True: continue
            afterTransition = Apply(transition.effects, current.state)
            target = transition.target
            evaluate target entry at target's effective depicted time
            if target.entry condition is not True: continue
            afterNode = Apply(target.effects, afterTransition)
            key = CanonicalStateKey(target, afterNode)
            if visited.Add(key):
                predecessor[key] = current key + transition ID
                enqueue with depth + 1

    return Complete + reached nodes/states/terminals/predecessors
```

Unknown conditions do not enable a path. Record aggregate reasons for blocked
edges so an unreachable-node explanation can say which leaf facts were false or
unknown.

#### State key

Serialize to an internal binary or string key in this exact logical order:

1. current node ID;
2. state variables sorted by ID with tagged value;
3. proposition overrides sorted by proposition ID;
4. perspective overrides sorted by holder/proposition/attitude, including an
   explicit tombstone marker or polarity;
5. flags sorted by ID;
6. evidence sorted by ID.

Use structural equality to guard against hash collisions. Do not include path,
depth, provenance trace, display text, or unordered collection iteration order.

#### Results

- A node is reachable if any state key reaches it.
- A terminal is reachable if any reached node has `IsTerminal`.
- `MustReachNode` fails only after complete exploration and no target state.
- On failure, show blocked incoming transitions and their evaluated leaf reasons.
- `MustNotReachNode` fails on the first BFS-reached matching state and reconstructs
  the shortest transition sequence through the predecessor map.
- If exploration is inconclusive, no absence-based constraint is considered
  proven.

### 12.10 Required fallback analysis

For a `RequiredFallback` constraint, construct a constrained initial analysis
state in which its `unavailable condition` is forced true through a declared
finite state assignment or signed proposition. Schema v1 restricts the
unavailable condition to a conjunction of `StateEquals` and
`PropositionHolds`, making this construction unambiguous.

Run reachability from the graph entries. The original path node may be
unreachable; at least one declared fallback target must be reachable. Complete
failure emits VW1704 with blocked-edge evidence. Bound exhaustion is
inconclusive.

### 12.11 Deduction closure

At a narrative state, build audience material:

```text
availableEvidence = state.AcquiredEvidence
known = signed propositions resolved from baseline perspectives at the current
        node's effective depicted time plus PerspectiveOverrides, where holder ==
        mystery.Audience and attitude in {Knows, Believes}
```

Then forward-chain:

```text
closure = set(known)
repeat
    changed = false
    for rule whose MysteryId matches this mystery, sorted by rule ID:
        if every evidence premise is available and
           every proposition premise is in closure and
           conclusion not in closure:
               if opposite conclusion in closure: emit VW1803
               add conclusion; record first deriving rule; changed = true
until not changed
```

Termination is guaranteed because conclusions come from a finite declared set
and rules only add. Preserve the first rule in sorted order as the deterministic
explanation tree; list alternative rules separately if requested.

### 12.12 Mystery validation

For every complete reachable state:

- If node stage is lower than `EarliestSolutionStage`, neither an explicitly
  available solution nor a solution in deduction closure may exist. Otherwise
  emit VW1801 with the shortest path and derivation.
- At every reachable state for each `RequiredSolvableAtNodeId`, every signed
  solution proposition must be in closure. One failure emits VW1802 with the
  shortest path and missing premise frontier.
- If a required solvable node is itself unreachable, emit the reachability error
  first and mark its deduction check skipped.

For required evidence-route coverage, collect distinct reachable node IDs whose
effects acquire that evidence. Compare the count to the required minimum. Emit
VW1804 with reached and unreachable acquisition nodes. The report explicitly
states that v1 counts acquisition nodes, not node-disjoint paths.

Run the same rules over selected trace frames when a trace exists. Label trace
results separately from all-branch results.

### 12.13 Disclosure validation

A `DisclosureAnnotation` contains audience, signed proposition, disclosure kind,
and source reference. Node effects should add the corresponding audience
perspective; the validator compares annotations and effects and reports drift.

`ForbiddenDisclosureBeforeStage` examines:

- direct disclosure annotations;
- audience perspective effects;
- deduction closure from material available at that stage.

This catches both a direct reveal and an author-declared deduction that reveals
the answer. It cannot catch an implication absent from the deduction rules; that
limitation appears in coverage.

## 13. Application use cases

Application handlers return a typed `OperationResult<T>` containing status,
diagnostics, and data. Expected invalid input is not communicated with exceptions.
Exceptions are reserved for programmer errors and are translated at host
boundaries into VW90xx internal failures.

### 13.1 Required handlers

```text
InitializeWorld
GetWorldStatus
GetRecord
ListRecords
BeginTransaction
GetTransaction
ApplyTransactionOperations
AnalyzeTransactionImpact
ValidateWorld
ValidateTransaction
CommitTransaction
AbortTransaction
GetDependencies
GetDependents
ExplainDependencyPath
GetPropositionTruth
TracePerspective
AnalyzeNarrative
ExplainDiagnostic
```

Keep handler request/response contracts independent of console and MCP types.
Every read result includes world revision/hash. Every draft result includes
transaction ID and base revision/hash.

`BuildContextPacket` is a Generation service and `ExportWorld` is an Export
service. CLI and later MCP hosts compose them beside Application handlers. This
keeps optional review/export adapters out of the required transaction layer.

### 13.2 Store interfaces

```csharp
public interface IWorldStore
{
    Task<LoadedWorld> LoadHeadAsync(CancellationToken cancellationToken);
    Task<AtomicWriteResult> ReplaceHeadAsync(
        ReadOnlyMemory<byte> expectedCurrentBytes,
        ReadOnlyMemory<byte> replacementBytes,
        CancellationToken cancellationToken);
}

public interface ITransactionStore
{
    Task<WorldTransaction?> LoadAsync(TransactionId id, CancellationToken ct);
    Task SaveDraftAsync(WorldTransaction transaction, CancellationToken ct);
    Task SaveReportAsync(TransactionId id, ValidationReport report, CancellationToken ct);
    Task MarkCommittedAsync(TransactionCommitReceipt receipt, CancellationToken ct);
}
```

`ReplaceHeadAsync` still runs under the application workspace lock and verifies
expected bytes/hash. The double check prevents an incorrect host composition
from overwriting a changed head.

### 13.3 Commit risk classification

Before policy evaluation, derive risk categories from changed IDs and semantic
diff:

- mystery solution changed;
- record removal;
- timeline history changed before existing events;
- predicate/schema changed;
- narrative entry/terminal changed;
- disclosure policy weakened;
- ordinary content addition/replacement.

The POC requires approval only for mystery-solution changes. A non-interactive
host returns `ApprovalRequired` and an approval token bound to transaction/hash;
it does not prompt. Approval is a separate explicit command/use case and is
rechecked under the commit lock.

## 14. CLI contract

The executable name is `vw` in documentation even if the built assembly remains
`ValidatedWorld.Cli`.

### 14.1 Initial commands

```text
vw init --workspace <path> --world-id <id> --title <title>
vw status --workspace <path>

vw tx begin --workspace <path> --intent <text> [--author <text>]
vw tx show --workspace <path> --tx <id>
vw tx apply --workspace <path> --tx <id> --operations <file-or-stdin>
vw tx impact --workspace <path> --tx <id>
vw tx validate --workspace <path> --tx <id>
vw tx commit --workspace <path> --tx <id> [--approval <token>]
vw tx abort --workspace <path> --tx <id>

vw get --workspace <path> --id <canonical-id>
vw list --workspace <path> [--category <value>] [--kind <value>] [--tag <value>]
vw dependencies --workspace <path> --id <id> [--transitive] [--max-depth <n>]
vw dependents --workspace <path> --id <id> [--transitive] [--max-depth <n>]
vw trace perspective --workspace <path> --holder <id> --proposition <id>
vw analyze narrative --workspace <path> --id <id>
vw explain --workspace <path> --diagnostic-fingerprint <value>

vw validate --workspace <path>
vw export --workspace <path> --profile <name> --output <path>
```

`--workspace` defaults to the current directory only in interactive human use.
Agent/plugin calls always supply it. Inputs may use `-` for stdin. Commands never
modify canon except `init`, `tx commit`, an explicit migration, or recovery.

### 14.2 Output envelope

Every command supports `--format json` and uses it by default when stdout is not
a terminal. JSON output is exactly one document:

```json
{
  "outputSchemaVersion": "validatedworld-cli/v1",
  "command": "tx.validate",
  "status": "invalid",
  "world": {
    "id": "world:harbor-mystery",
    "revision": 3,
    "contentHash": "sha256:..."
  },
  "transaction": {
    "id": "tx:...",
    "baseRevision": 3,
    "baseContentHash": "sha256:..."
  },
  "diagnostics": [],
  "coverage": {},
  "data": {}
}
```

Omit neither `diagnostics` nor `data`; use empty arrays/objects. Write all normal
machine output to stdout and concise operational logging to stderr. Never mix
progress text into JSON stdout.

### 14.3 Exit codes

```text
0  requested operation completed; validation is proven valid when applicable
2  deterministic validation rejected the request
3  command syntax or input contract error
4  stale revision, record precondition, or lock conflict
5  filesystem/storage failure
6  required analysis inconclusive
7  explicit human approval required
8  unsupported schema/feature or migration required
9  internal failure
```

Warnings and AI concerns do not change exit code unless project commit policy
blocks them. An invalid validation report uses 2 even though the validator ran
successfully.

### 14.4 No natural-language parser in POC

Do not implement `vw query "quests requiring a living official"` initially.
Typed filters and semantic traces are more predictable for agents. A model can
choose structured operations from tool schemas later.

## 15. Context packets and AI review

### 15.1 Context packet model

A context packet is deterministic derived data:

```csharp
public sealed record ContextPacket(
    string SchemaVersion,
    WorldId WorldId,
    long WorldRevision,
    string WorldHash,
    ImmutableArray<CanonicalId> SeedIds,
    ImmutableArray<ContextRecord> Records,
    ImmutableArray<ProjectConstraint> ApplicableConstraints,
    ImmutableArray<CanonicalId> OmittedRecordIds,
    bool Truncated,
    string SelectionExplanation,
    string PacketHash);
```

### 15.2 Selection algorithm

Given seeds and byte/record limits, create candidates with deterministic priority:

```text
0 seeds
1 constraints and mystery definitions that directly depend on seeds
2 owning narrative graph and sibling nodes/transitions needed to understand a seed
3 forward dependency closure of seeds
4 direct reverse dependents
5 additional reverse dependents by increasing graph distance
```

Within a priority/distance, sort by canonical ID. A record is included atomically;
never truncate its JSON midway. Always include a manifest of omitted candidate
IDs and the selection limits. If a seed or priority-1 record alone exceeds the
limit, return inconclusive packet construction instead of dropping it.

Before sending a packet to a provider, apply disclosure filtering structurally.
The provider never receives omitted secrets and cannot be trusted to obey a
prompt-based secrecy rule.

### 15.3 Review contract and cache key

```csharp
public interface IWorldReviewProvider
{
    string ProviderId { get; }
    Task<ReviewResponse> ReviewAsync(ReviewRequest request, CancellationToken ct);
}
```

Request fields:

- snapshot and transaction hashes;
- context packet hash;
- review profile ID/version;
- prompt template ID/version/hash;
- provider and model ID;
- generation parameters;
- structured response schema version.

The cache key is SHA-256 over a canonical serialization of all fields above.
Cache raw provider response plus parsed findings. A response that fails its
schema becomes an AI provider failure, not a world validation error.

Each finding includes `AIxxxx` code, message, cited record IDs, quoted evidence
limited to provided context, confidence, and suggested next semantic operation.
No finding applies an operation automatically.

### 15.4 OpenAI/plugin boundary

Do not add OpenAI packages to Core, Validation, Application, or Serialization.
An OpenAI review adapter may live in Generation or a separate provider package.

Post-POC, expose application handlers through a controlled MCP server. Package
the MCP server and an authoring workflow skill using the then-current OpenAI
plugin structure. Official documentation currently describes a
`.codex-plugin/plugin.json` manifest plus skills and optional MCP configuration;
re-check it at implementation time:

- https://developers.openai.com/plugins/concepts/plugins
- https://developers.openai.com/plugins/build/plugins

The useful MCP tool set should be small and semantic:

```text
world_status
get_record
list_records
begin_transaction
apply_operations
analyze_impact
validate_transaction
commit_transaction
abort_transaction
trace_dependency
trace_perspective
analyze_narrative
build_context_packet
export_world
```

Do not expose arbitrary filesystem writes or shell execution.

## 16. Export algorithms

### 16.1 Export metadata

Every export contains or is accompanied by:

```json
{
  "generated": true,
  "sourceWorldId": "world:harbor-mystery",
  "sourceWorldRevision": 3,
  "sourceContentHash": "sha256:...",
  "profile": "continuity-reference/v1",
  "disclosureScope": "developer",
  "generatedAt": "2026-08-10T20:00:00.0000000Z",
  "warning": "Generated artifact. Edit canonical source through a transaction."
}
```

Generation time is supplied by `IClock`. It is not used in semantic content
hashes.

### 16.2 Normalized JSON

The normalized export uses the canonical writer and must equal the committed
canonical bytes when the full developer disclosure profile is selected. Draft
preview exports clearly use the transaction/projected hash and cannot be passed
to `LoadHead` as committed canon without an explicit import transaction.

### 16.3 Continuity reference

Generate deterministic Markdown without AI:

1. Metadata and coverage summary.
2. Entities grouped by kind, then ID.
3. Truth assertions with human gloss and intervals.
4. Perspectives grouped by holder, with provenance.
5. Timeline events by ordinal.
6. Narrative graphs, reachable/unreachable nodes, and selected traces.
7. Mystery solution, clues, deductions, and disclosure stages for authorized
   scopes.
8. Active constraints and unresolved diagnostics.

Use stable headings containing IDs so diffs remain useful.

### 16.4 Context/QA packet

Given seeds, reuse the context selection algorithm and render relevant assertions,
perspectives, dependencies, and constraints as a checklist. A checklist item is
only a declared fact or rule; do not invent prose QA tests.

### 16.5 Disclosure filtering

Schema v1 uses top-level `DisclosureRule` records. For an audience-facing export,
the filter selects only targets with an applicable `Public` rule or a rule whose
allow list contains the requested audience at the requested graph/stage. No rule
means hidden. The filter runs on records before formatting.

After initial selection, walk each selected record's dependencies. If a visible
record depends on a hidden record, the exporter either:

- replaces the hidden reference with that target's explicitly configured
  `PublicGloss`; or
- omits the dependent record and emits a coverage diagnostic.

It never leaks a hidden ID/name and never relies on later prose redaction.

## 17. Migration strategy

World loading supports exactly the current schema at first. Add migrations only
when v2 exists.

```csharp
public interface IWorldMigration
{
    string FromVersion { get; }
    string ToVersion { get; }
    MigrationResult Migrate(NeutralJsonDocument input);
}
```

Migrations form a unique consecutive path. They are deterministic, preserve IDs
where semantics are unchanged, and emit a migration report for lossy or
human-required decisions. Loading an old file may produce an in-memory preview,
but writing it requires explicit `vw migrate`, validation, and atomic commit.
Never rewrite canon merely because it was opened by a newer binary.

External JSON import is a separate adapter that produces transaction operations;
it is not a schema migration.

## 18. Safety and resource controls

World files and plugins may be untrusted. Enforce:

- maximum input bytes configured by host;
- JSON and AST depth/node limits;
- record and collection count limits;
- narrative state/transition/depth/time limits;
- normalized workspace-relative external text paths;
- no dynamic code execution from data;
- no network access in core validation;
- cancellation in all potentially long traversals;
- no error message that dumps entire secret records by default.

Use a monotonic elapsed-time source for analysis timeouts and `IClock` only for
metadata. Timeout results vary with machine load, so state/transition count limits
are the reproducible primary bounds; reports include which limit fired.

For deterministic CI, tests set generous timeouts and small explicit count bounds
when exercising inconclusive behavior.

## 19. Test strategy

### 19.1 Test layers

```text
Core.Tests
  ID/value equality and factories
  scope bound primitives
  AST construction invariants
  operation value semantics

Serialization.Tests
  strict JSON parsing
  DTO/domain mapping
  canonical byte output and hashing
  migrations when introduced
  atomic file writer fault injection

Validation.Tests
  indexes and dependency edges
  every deterministic validator
  trace replay and BFS
  mystery deduction and disclosure
  stable diagnostics and fingerprints

Application.Tests (add with project)
  transaction lifecycle
  stale/conflicting commits
  policy and approval
  commit failure atomicity
  query/use-case results

Export.Tests (add when exports begin)
  golden normalized JSON/Markdown
  disclosure filtering
  source metadata

Cli.Tests (prefer in-process command invocation)
  JSON envelope and stdout/stderr separation
  exit codes
  end-to-end workspace flows
```

Replace all placeholder `UnitTest1` tests when their work package begins. Do not
count empty tests as coverage.

### 19.2 Golden sample and intentional-error corpus

Create `samples/HarborMystery/` with:

```text
world.vw.json                       valid baseline
transactions/
  add-valid-witness.json
  invalid-secret-leak.json
  invalid-dead-quest-giver.json
  invalid-unreachable-terminal.json
  invalid-clue-before-creation.json
  invalid-timeline-cycle.json
  invalid-stale-base.json
expected/
  <case>.diagnostics.json
  continuity-reference.md
```

Golden diagnostics compare semantic fields and deterministic order. Source byte
offsets may be normalized if formatting changes, but codes, IDs, evidence,
outcomes, and fingerprints are exact.

The valid sample needs at least:

- Mayor, merchant, guard, innkeeper, investigator, and fallback witness.
- Harbor, mayor's office, and warehouse.
- Mayor-killed-merchant truth; guard's false bandit belief; innkeeper's
  suspicion.
- Murder, body discovery, planted rumor, clue creation, and clue discovery time
  points/events.
- One character whose availability state can become dead/unavailable.
- A branch that uses a fallback witness.
- A reader audience, solution proposition, at least three clues, and explicit
  deduction rules.
- A selected chapter trace and a branching investigation graph.

### 19.3 Algorithm-focused tests

Minimum required cases:

#### Scope/claims

- Half-open intervals touching at an endpoint do not conflict.
- Global assertion overlaps every timeline.
- Opposite polarities in different non-global timelines do not conflict.
- Cardinality allows sequential values and rejects overlapping values.
- Knowledge interval covered by two adjacent truth intervals succeeds.
- A one-ordinal coverage gap fails with exact evidence.

#### Open-world logic

- Missing proposition is Unknown.
- Explicit opposite is False for a signed proposition condition.
- `not(Unknown)` remains Unknown.
- Unknown transition is not traversed.
- A selected trace reports Unknown separately from False.

#### Graph/impact

- Removed base edge still impacts its old dependent.
- Added projected edge impacts its new dependent.
- Cycles terminate.
- Explanation is shortest in hop count and stable on ties.
- Nested node/parent containment impacts both directions as designed.

#### Narrative analysis

- BFS revisits the same node with different states.
- BFS deduplicates structurally equal states reached by different paths.
- Shortest counterexample is stable.
- Unreachable finding is emitted only after complete exploration.
- State limit yields VW1703 and Inconclusive, not reachability success.
- Loop with idempotent effects terminates through state deduplication.
- A later-stage flashback evaluates canon at its earlier depicted time while
  retaining audience knowledge acquired in narrative order.
- A presentation-only node cannot mutate world/story state.

#### Mystery

- Solution is not derived without all premises.
- Multi-step rules reach a fixed point.
- Deduction cycle without a seeded premise derives nothing and terminates.
- Opposite conclusions produce VW1803.
- Direct and deductive early reveals both produce VW1801.
- Every state at a required solve node must be solvable, not just one.
- Evidence route count uses distinct acquisition nodes and reports its coverage
  limitation.

#### Transactions/storage

- Add/replace/remove record revisions are assigned correctly.
- Duplicate target operations are rejected.
- Repairs and deletion validate together in one projected snapshot.
- Stale world base and stale record revision are distinct conflicts.
- Every injected failure before atomic replace preserves exact canonical bytes.
- Failure after replace reconciles from embedded last-commit metadata.
- Two concurrent commits permit at most one against the same base.

### 19.4 Determinism tests

For semantically identical snapshots whose input arrays/properties have different
allowed ordering:

- canonical serialized bytes match;
- content hashes match;
- dependency edges match;
- validation reports match after excluding run-duration counters;
- context packet hashes match;
- exports match except injected generation time.

Run selected tests under at least two cultures, including one with comma decimal
formatting. Do not assert platform-specific newline sequences; canonical JSON and
generated Markdown use `\n`.

### 19.5 Property and fuzz tests

Before adding a property-test dependency, small deterministic generators in the
test project are sufficient. Generate finite state graphs with a fixed seed and
compare BFS results to an obviously correct exhaustive reference implementation
for small domains.

Fuzz the strict JSON pre-scan with nested objects, duplicate names, deep arrays,
large numbers, invalid UTF-8, and truncated tokens. The expected result is a
diagnostic, never process termination or uncontrolled allocation.

### 19.6 Performance evidence

Performance is not the first correctness gate. Add a non-CI benchmark or recorded
measurement after the vertical slice. Use:

- the small Harbor sample;
- a synthetic 10,000-record reference graph;
- a finite graph near the default narrative state bound.

Record machine/runtime, elapsed time, allocations, states, and edges. Optimize
only measured bottlenecks. Any incremental validator must be differential-tested
against full validation over generated transactions before it can run at commit.

## 20. Work packages for coding agents

Each package ends with full solution build/test. A later agent should not begin a
package whose prerequisites are incomplete. File names are guidance; cohesion
and dependency direction matter more than exact names.

### WP0 — Solution wiring and engineering baseline

**Goal:** Make project dependencies and build conventions real without inventing
domain behavior.

Tasks:

1. Add `src/ValidatedWorld.Application` and
   `tests/ValidatedWorld.Application.Tests` to `ValidatedWorld.slnx`.
2. Add project references according to section 3.
3. Add `Directory.Build.props` with nullable, implicit usings, deterministic
   builds, warnings-as-errors for production projects, latest safe language
   version for .NET 10, and consistent analyzer settings.
4. Add project references from each test project to its production project.
5. Replace empty placeholder tests with one meaningful assembly-boundary test or
   remove them until WP1.
6. Add `samples/HarborMystery/README.md` describing the future fixture; do not
   fabricate canonical JSON before DTO types exist.

Acceptance:

- Dependency graph matches the specification and has no cycles.
- `dotnet build ValidatedWorld.slnx` and `dotnet test ValidatedWorld.slnx` pass.
- No production project has an unnecessary external package.

### WP1 — Core primitives and immutable model

**Prerequisite:** WP0.

Tasks:

1. Implement validated ID factories and ordinal equality.
2. Implement `WorldValue`, bounds/scopes, polarity, perspectives, predicate,
   proposition, assertion, entity, state-variable, timeline/event records.
3. Implement condition/effect unions and narrative/mystery/constraint/disclosure
   records.
4. Implement transaction operation/value records without storage behavior.
5. Add XML documentation to public types where an invariant is not obvious.
6. Add test builders in the test project, not mutable builders in Core.

Tests:

- ID regex/case/Unicode rejection.
- Tagged value equality and decimal culture independence at value level.
- Half-open bound primitives including infinities.
- State domain structural equality.
- AST union exhaustive visitor tests.

Acceptance:

- Core references only BCL assemblies.
- No JSON attributes or `JsonElement` in Core.
- All model collections are immutable at public boundaries.

### WP2 — Strict JSON, canonical writer, and Harbor skeleton

**Prerequisite:** WP1.

Tasks:

1. Create v1 DTOs and explicit domain mappers.
2. Implement duplicate-property pre-scan and source map.
3. Implement union converters and unmapped-member rejection.
4. Implement canonical ordering/writer and hash projection.
5. Verify content hash on load.
6. Create the smallest valid Harbor snapshot with entities, predicate,
   proposition, assertion, and timeline records only.
7. Add canonical JSON golden file.

Tests:

- Every strict reader rule in 7.4.
- DTO/domain error accumulation where safe.
- Round-trip domain equality.
- Byte-identical canonical output under input reorderings and cultures.
- Hash mismatch and revision-0 parent-hash rules.

Acceptance:

- Loading and rewriting the canonical Harbor sample is byte-stable.
- Invalid input yields diagnostics with JSON pointer and byte location.
- No serializer exception escapes the public loader for user input.

### WP3 — World index and structural/semantic fact validation

**Prerequisite:** WP2.

Tasks:

1. Implement `WorldIndex` with ambiguity tracking.
2. Implement validation contracts, phases, stable sorting, report/coverage types.
3. Implement ID/reference/type validation.
4. Implement predicate/proposition typing.
5. Implement interval, opposite assertion, and cardinality validators.
6. Add initial diagnostic fingerprinting.
7. Expand Harbor sample with 20–40 propositions/assertions.

Tests:

- All cases in 19.3 Scope/claims.
- Duplicate IDs never choose an arbitrary record.
- Validator exceptions become VW9001 and incomplete phase.
- Diagnostic order/fingerprint is stable.

Acceptance:

- The valid Harbor fact model is ProvenValid for implemented phases.
- Intentional assertion/cardinality failures match golden diagnostics.

### WP4 — Transaction projection and atomic workspace

**Prerequisite:** WP3.

Tasks:

1. Implement transaction JSON/store and one-operation-per-target normalization.
2. Implement add/replace/remove projection with record revisions.
3. Implement file workspace paths and validation.
4. Implement lock and atomic file writer abstraction.
5. Implement begin/apply/validate/commit/abort handlers.
6. Implement commit policy for deterministic errors/inconclusive analysis.
7. Implement receipt reconciliation.

Tests:

- All transaction/storage cases in 19.3.
- Path traversal and rooted external paths are rejected.
- Failed validation does not call atomic writer.
- Canonical content hash chain advances exactly once on commit.

Acceptance:

- A valid record addition commits and survives reload.
- An invalid, stale, or fault-injected commit leaves canonical bytes unchanged.
- No implicit delete cascade exists.

### WP5 — Dependency, impact, and query handlers

**Prerequisite:** WP4.

Tasks:

1. Implement explicit dependency visitors for all current record/AST cases.
2. Implement forward/reverse immutable graph.
3. Implement base/projected union impact BFS and explanation paths.
4. Implement get/list/dependency/dependent/path/truth query handlers.
5. Add graph coverage to validation report.

Tests:

- All Graph/impact cases in 19.3.
- Every new model reference field has a dependency visitor assertion.
- Limits/truncation are present in query results.

Acceptance:

- A Harbor transaction reports expected changed, direct-dependent, and
  transitive-dependent records with field-level explanations.

### WP6 — Time, perspective, and provenance

**Prerequisite:** WP5.

Tasks:

1. Implement event prerequisite SCC/ordering validation.
2. Implement timeline event replay or explicitly scope the first sub-PR to static
   chronology.
3. Implement factive knowledge interval coverage.
4. Implement unawareness conflicts and v1 provenance validators.
5. Add truth/perspective/provenance trace queries.
6. Expand Harbor sample with false belief, suspicion, witness, rumor, and
   document/clue provenance.

Tests:

- Adjacent interval coverage, gaps, global/timeline scope.
- Same-time read/write ambiguity.
- Every provenance kind's supported and invalid cases.
- False belief is valid; false `Knows` is VW1401.

Acceptance:

- The sample can answer “why does the guard believe bandits did it?” as a stable
  record chain.
- An impossible witness/knowledge path is rejected with actionable evidence.

### WP7 — Narrative trace and bounded graph analysis

**Prerequisite:** WP6.

Tasks:

1. Implement AST type checker and three-valued evaluator.
2. Implement effect application and state conflict checks.
3. Implement selected-trace replay with frames.
4. Implement canonical state keys and bounded BFS.
5. Implement reachability/terminal/must-not-reach/fallback constraints.
6. Expand Harbor sample with an investigation graph, selected chapter trace,
   unavailable character, and fallback witness.

Tests:

- All Open-world logic and Narrative analysis cases in 19.3.
- Every limit independently produces Inconclusive.
- Selected trace transition ambiguity is rejected.
- Fallback analysis forces only the supported restricted condition form.
- Flashback/presentation-only rules keep world time and audience order separate.

Acceptance:

- The analyzer proves the valid graph has a terminal/fallback within bounds.
- Intentional dead end produces a shortest replayable counterexample or blocked
  frontier, as appropriate.

### WP8 — Mystery, deduction, and disclosure

**Prerequisite:** WP7.

Tasks:

1. Implement audience material extraction and deduction closure.
2. Implement early direct/deductive solution disclosure.
3. Implement required-solvable-node checks across every reached state.
4. Implement distinct acquisition-node coverage.
5. Add mystery explanation trees and missing-premise frontiers.
6. Complete the Harbor mystery fixture and invalid corpus.

Tests:

- All Mystery cases in 19.3.
- Branch where one solve-node state lacks a clue must fail despite another valid
  state.
- Trace and all-branch results are labeled separately.

Acceptance:

- The valid mystery is solvable at declared nodes without early reveal in the
  explicit deduction model.
- Each intentional mystery error produces the expected stable code and path.

### WP9 — Agent-grade CLI and deterministic exports

**Prerequisite:** WP8.

Tasks:

1. Implement CLI commands, JSON envelope, exit codes, and stdin support.
2. Keep command parsing thin over application handlers.
3. Implement normalized JSON, continuity Markdown, and context/QA exports.
4. Implement structured disclosure filtering before any player-safe profile is
   advertised.
5. Add end-to-end CLI tests in temporary workspaces.

Tests:

- stdout JSON is one valid document; logs only on stderr.
- Every status maps to the specified exit code.
- Golden exports are stable except injected time.
- No hidden record identifier leaks into a player-safe export.

Acceptance:

- An agent can inspect Harbor, begin/apply/validate/repair/commit a transaction,
  and export a continuity reference using only JSON CLI calls.

### WP10 — POC evaluation and decision gate

**Prerequisite:** WP9.

Tasks:

1. Run the feasibility evaluation corpus and realistic authoring tasks.
2. Record misses, false positives, annotation effort, context size, and repair
   success in `docs/poc_evaluation.md`.
3. Add synthetic performance evidence without hiding incompleteness.
4. Decide and document one of:
   - proceed to plugin/AI/prose work;
   - keep a smaller continuity-outline product;
   - revise a proven bad model assumption;
   - archive the experiment.

Acceptance:

- The decision is evidence-based and explicitly approved by the project owner.
- No plugin, GUI, or AI provider work starts merely because the demo looks good.

### WP11 — Optional AI review (after successful gate)

Tasks:

1. Implement deterministic context packets first.
2. Implement provider-neutral request/response and cache.
3. Add a fake provider and schema-failure tests.
4. Add one opt-in real provider adapter in a dependency-isolated package.
5. Evaluate whether review catches deliberately unannotated issues without
   misrepresenting them as proof.

Acceptance:

- Core deterministic validation works with Generation absent.
- Tests use only the fake provider.
- Every AI result is auditable and labeled Concern.

### WP12 — Optional MCP/plugin packaging (after stable application API)

Tasks:

1. Re-check current official OpenAI plugin and MCP documentation.
2. Implement an MCP host over application handlers, not CLI process scraping.
3. Define minimal schemas and read-only/mutating tool annotations.
4. Add an authoring skill that requires transaction → impact → validate → commit.
5. Package with the current manifest and local marketplace mechanism.
6. Test headless tool operation before considering optional UI.

Acceptance:

- The same application contract drives CLI and MCP behavior.
- The plugin exposes no unrestricted shell or raw canonical overwrite.
- A clean agent session completes the WP9 workflow through tools.

## 21. Pull-request checklist for every coding package

Before declaring a package complete:

- [ ] Product/spec changes are reflected in both authoritative docs when needed.
- [ ] New serialized behavior has strict load, canonical write, and invalid-input
      tests.
- [ ] Every new reference-bearing field contributes dependency edges.
- [ ] Every new validator has a stable code/rule version and at least one passing,
      failing, and prerequisite-invalid test.
- [ ] Inconclusive paths are tested and never reported as pass.
- [ ] No canonical mutation occurs outside transaction commit/init/migrate/recover.
- [ ] No generated export is treated as source.
- [ ] `dotnet build ValidatedWorld.slnx` succeeds.
- [ ] `dotnet test ValidatedWorld.slnx` succeeds.
- [ ] Sample/golden output is updated when behavior is user-visible.
- [ ] The handoff names remaining limitations instead of hiding them.

## 22. Deferred decisions

These decisions intentionally wait for POC evidence:

- SQLite/service/object-store persistence.
- Branch merge and semantic rebase.
- Formal node-disjoint clue-route analysis.
- Rich calendar and duration algebra.
- Probabilistic beliefs/confidence arithmetic.
- Automatic natural-language proposition extraction.
- A general project constraint DSL.
- Incremental commit validation.
- Runtime game save compatibility.
- Visual graph editing.
- Specific AI model/provider choice.
- Public plugin publication.

Deferral is not a ban. It prevents speculative abstractions from obscuring the
one question the POC must answer: does explicit, transactional semantic canon
materially help an AI preserve continuity in a realistic long-form authoring
workflow?

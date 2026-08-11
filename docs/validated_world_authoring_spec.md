# ValidatedWorld Product and Architecture Specification

**Status:** Authoritative product specification

**Specification version:** 3.0

**Last reviewed:** 2026-08-11

**Primary implementation:** .NET 10 / C#

**Canonical interchange format:** JSON

This document defines the product and its architectural boundaries. Detailed
common-core records, algorithms, tests, and work packages are in
[implementation_blueprint.md](implementation_blueprint.md). The guarantee
boundary and staged proof plan are in [feasibility.md](feasibility.md).

Human direction overrides this document. When a product decision changes, update
the specification and blueprint together.

---

## 1. Product thesis

A long document is sequential in presentation but graph-shaped in meaning.

- A conclusion depends on assumptions, evidence, and definitions.
- A design decision depends on requirements and measurements.
- A test verifies a requirement and may depend on implementation details.
- A scene depends on prior events and what its characters know.
- A game transition depends on current state and changes future possibilities.

ValidatedWorld makes the continuity-critical part of that hidden graph explicit.
It is a **consistency, impact-analysis, and review system for large authored
projects**.

The central workflow is:

```text
load a canonical project snapshot
→ begin an isolated transaction
→ change content, claims, or semantic links
→ build the projected snapshot
→ compute transitive impact from base and projected graphs
→ run deterministic validation
→ create review obligations for affected material
→ run optional required heuristic reviews
→ update, justify, resolve, or acknowledge
→ commit atomically
→ export documents, reports, and focused context packets
```

ValidatedWorld does not make an AI remember hundreds of pages. It gives the AI a
small explained context packet and prevents it from committing a change without
considering explicitly dependent material.

Despite the project name, a “world” is any versioned universe of connected
claims and artifacts. Fictional worlds, technical designs, whitepapers, and
interactive systems are product profiles over one common graph.

## 2. Product promise and evidence levels

ValidatedWorld guarantees only properties represented in its semantic model.

Every result is classified as:

- **Proven:** deterministic checks completed and the declared property holds.
- **Disproven:** deterministic checks found a violation, with evidence or a
  replayable counterexample.
- **Inconclusive:** missing annotations, an unsupported construct, an analysis
  bound, cancellation, or internal failure prevented a conclusion.
- **Concern:** a heuristic, linter, or AI reviewer identified a possible issue.

An incomplete check is never a pass.

Project policy may require a heuristic review to run and require every concern
to be resolved or acknowledged. That proves completion of a review workflow; it
does not convert the reviewer's judgment into deterministic truth.

Coverage is a first-class report: the system identifies content that is mapped,
stale, unreviewed, extension-owned, or outside deterministic protection.

## 3. The common mental model

ValidatedWorld combines:

- A canonical project containing authored content and its semantic map.
- Immutable snapshots with revision and content hashes.
- Atomic optimistic transactions.
- A typed semantic-link graph.
- A derived operational dependency and reverse-impact graph.
- Deterministic validators and stable diagnostics.
- Mandatory review obligations for policy-selected impacts.
- Optional auditable human/AI review runs.
- Profile-specific validators and, only where needed, bounded model checking.
- Profile-driven exports and context packets.

It is not primarily a graph drawing program, word processor, game engine,
database server, natural-language theorem prover, scientific peer reviewer, or
prose generator. It can integrate with those tools.

## 4. Foundational distinctions

### 4.1 Content and semantics

Authored text, figures, tables, or design material are **content**. Claims,
definitions, assumptions, evidence relationships, and constraints are the
**semantic map**.

They are distinct so the system can detect a changed section whose annotations
have not been reviewed. They are jointly canonical when committed in the
project. A content change and the semantic repairs it requires can occur in the
same transaction.

### 4.2 Semantic links and operational dependency edges

A semantic link is canonical meaning, such as “calculation C is derived from
assumption A” or “test T verifies requirement R.”

The operational dependency graph is a derived index. Link type determines impact
direction. For example:

- `derived-from(C, A)` means C depends on A.
- `supports(E, C)` means C depends on evidence E.
- `implements(I, R)` means implementation I depends on requirement R.
- `contradicts(A, B)` affects both directions.

The graph is not separately edited or stored as another source of truth.

### 4.3 Explicit truth, status, and perspective

A proposition is content such as “average current draw is 20 mA.” An assertion
states a polarity, role, and lifecycle status for that proposition.

Roles include fact, assumption, hypothesis, requirement, observation, result,
conclusion, decision, recommendation, and definition. Status includes proposed,
accepted, rejected, deprecated, and superseded.

These distinctions matter. An accepted assumption is not claimed as observed
fact; a rejected hypothesis does not contradict an accepted conclusion merely by
existing.

Profiles may add perspective. In fiction, canon truth is separate from what a
character believes. In a paper, an assertion may be attributed to an author or
source. The common core does not equate every recorded statement with truth.

### 4.4 Absence and negation

The default is open-world semantics. Missing information means unknown or
unmodeled, not false. Negative assertions are explicit.

A profile may declare a closed finite domain for a specific rule, but reports
must disclose that assumption.

### 4.5 Project revision and subject-matter time

Project revision is edit history. Subject-matter time—fictional chronology,
measurement date, design version, or historical period—is domain content. They
are separate axes.

### 4.6 Canonical source and generated output

The canonical project contains committed content units and semantic records.
Generated Markdown, rendered papers, lore books, runtime JSON, diagrams, reports,
and context packets are derived artifacts.

An external document may be imported into a transaction, but editing a generated
export never silently edits canon. Later round-trip editors must preserve stable
content-unit IDs and import changes transactionally.

## 5. Common canonical model

Every addressable record has a stable opaque ID and per-record revision. Display
names and headings may change without changing identity.

### 5.1 Project snapshot

A common snapshot contains at least:

- Project identity, schema version, revision, and content hash.
- Commit and validation policy.
- Artifacts and ordered content units.
- Subjects and typed predicates.
- Propositions and assertions.
- Source/evidence records.
- Semantic links governed by versioned built-in link semantics.
- Bindings between content and semantic records.
- Explicit constraints.
- Review attestations and profile extension records.

The first implementation uses one canonical JSON file. The storage layout is not
the semantic contract; normalized JSON is.

### 5.2 Artifacts and content units

An artifact represents an authored document or another project-controlled body
of content. Examples include a technical design, whitepaper, chapter manuscript,
requirements document, or campaign outline.

A content unit is the smallest stable review target. It contains:

- Stable ID and artifact ID.
- Kind: section, paragraph, figure, table, equation, scene, requirement block,
  code/design excerpt, or custom kind.
- Sequence/order key and optional parent unit.
- Heading/label and canonical text or structured content.
- Content hash.
- Semantic-review attestation bound to a content hash, when present.

The POC stores text content inside canonical JSON and exports Markdown. A later
round-trip Markdown importer may preserve IDs with unobtrusive markers.

Changing content invalidates semantic-review attestations bound to its old hash.

### 5.3 Subjects

A subject is a named thing or concept used by claims:

- Technical term, variable, quantity, component, interface, requirement target,
  method, dataset, or organization.
- Character, location, faction, item, event, or clue.
- Any project-defined concept with stable identity.

Subjects have kind, name, aliases, description, tags, and optional extension
payloads. Names are never foreign keys.

### 5.4 Predicates, propositions, and assertions

A predicate declares typed argument roles. A proposition is a predicate plus
typed arguments and optional human-readable gloss.

An assertion adds:

- Positive or negative polarity.
- Assertion role.
- Lifecycle status.
- Optional scope/profile qualifiers.
- Evidence and source references.
- Optional author/rationale notes.

Deterministic contradiction checks compare only assertions whose role/status and
scope make them simultaneously authoritative under project policy.

The common POC supports global/static scope. Technical and narrative profiles
may add version, temporal, scenario, or audience scopes later.

### 5.5 Sources and evidence

A source record describes internal or external support:

- Citation or document reference.
- Dataset, measurement, experiment, test result, calculation, code artifact, or
  design artifact.
- Stable locator, optional URI, content hash, version/date, and notes.

ValidatedWorld can prove that required support links exist and refer to a known
version. It cannot prove that external evidence is honest or scientifically
sound without a specialized trusted validator.

### 5.6 Content bindings

A content binding connects a content unit to a semantic record with a role:

- `asserts`
- `defines`
- `uses`
- `discusses`
- `presents-evidence`
- `implements`
- `verifies`
- profile-specific roles

Bindings create deterministic dependency edges and coverage. They also let an
impact report point to the exact sections that need review.

### 5.7 Semantic links

A semantic link contains:

- Stable ID and revision.
- Source and target record IDs.
- Link kind.
- Rationale.
- Provenance: manual, imported, AI-proposed-and-confirmed, or derived by a named
  deterministic rule.
- Optional confidence for heuristic provenance; confidence never changes
  deterministic meaning after confirmation.

Initial link kinds and impact meaning:

| Link kind | Meaning | Operational dependency |
|---|---|---|
| `depends-on` | Source requires target | source → target |
| `derived-from` | Source conclusion derives from target | source → target |
| `supports` | Source evidence supports target claim | target → source |
| `contradicts` | Records conflict | bidirectional |
| `refines` | Source specializes target | source → target |
| `supersedes` | Source replaces target | bidirectional review |
| `defines` | Source definition defines target subject | users of target also depend on source |
| `uses` | Source content/claim uses target | source → target |
| `implements` | Source implements target requirement | source → target |
| `satisfies` | Source claim/decision satisfies target requirement | source → target |
| `verifies` | Source test/evidence verifies target requirement | source → target |
| `cites` | Source relies on target source | source → target |
| `mentions` | Informational association | no transitive impact by default |

Common link kinds have fixed v1 semantics. Schema v1 does not accept arbitrary
custom common-link kinds. A registered profile can derive typed profile edges
from its own records; unknown profile relationships are informational/uncovered
and cannot affect deterministic impact until a validator understands them.

### 5.8 Constraints

The POC uses a small typed set:

- Accepted assertions may not explicitly contradict in the same scope.
- Selected claim roles require at least one support/derivation link.
- `derived-from` chains may be required acyclic.
- Definitions for a selected term/scope may be required unique.
- Accepted requirements may require at least one implementation and verification
  link.
- Selected content kinds require current semantic-review attestation.
- Policy-selected impact records require review dispositions before commit.

Do not add an unrestricted logic language. New constraint types need explicit
evaluation, diagnostics, and tests.

## 6. Transactions, impact, and mandatory review

All canonical authoring occurs in a transaction based on an exact project
revision and content hash.

A transaction records:

- ID, base revision/hash, intent, author, and status.
- Add, replace, and remove operations against stable record IDs.
- Direct changed records.
- Derived impact set with explanation paths.
- Review obligations and dispositions.
- Deterministic reports.
- Heuristic review runs, concerns, resolutions, and acknowledgements.
- Commit metadata.

### 6.1 Impact computation

Dependencies are extracted from typed fields, content bindings, semantic links,
constraints, and profile records.

The impact set is the changed records plus reverse transitive dependents in the
union of the base and projected dependency graphs. Both graphs are required:

- Base edges find material affected by a removed dependency.
- Projected edges find material affected by a newly introduced dependency.

Impact means “must be considered,” not “must be edited.”

### 6.2 Review obligations

Project policy selects which impacted records require disposition. Each
obligation contains target ID, shortest impact path, evidence edges, projected
record/content hashes, and status:

- `pending`
- `updated`
- `reviewed-no-change`
- `not-applicable`

Changed records receive `updated` automatically. The other non-pending
dispositions require reviewer identity and rationale. They are fingerprints over
the projected record plus impact evidence; modifying the transaction invalidates
stale dispositions.

Commit policy may block while required obligations remain pending. This creates
useful enforcement even when the underlying semantic question requires human or
AI judgment.

### 6.3 Heuristic review

A review run records:

- Base-project, change-set, and projected-state hashes.
- Review profile and template versions.
- Context packet manifest and truncation state.
- Reviewer type/identity and, for AI, provider/model parameters.
- Structured concerns with cited record IDs.
- Completion/failure status.

Policy may require a named review profile for selected change categories.
Concerns remain heuristic. Policy may require each concern to be resolved by a
semantic change, rejected with rationale, or acknowledged.

AI-extracted claims and links are candidates. They become canonical only through
explicit transaction operations, retaining provenance.

### 6.4 Atomic commit

Commit is optimistic and atomic:

1. Acquire the workspace commit lock.
2. Re-read and verify the canonical head.
3. Reject a stale base or record precondition.
4. Apply all operations to a projected snapshot.
5. Build base and projected dependency graphs.
6. Compute impact and current review-obligation fingerprints.
7. Run required deterministic validators and review profiles.
8. Apply commit policy to errors, inconclusive phases, pending obligations, and
   unresolved concerns.
9. Serialize deterministically, hash, and atomically replace canon.
10. Record auditable commit metadata.

Failure before replacement leaves canon byte-for-byte unchanged.

## 7. Deterministic validation

Validators are pure with respect to a supplied snapshot/request. Results have
stable codes, evidence, and ordering.

### 7.1 Common phases

1. JSON/schema/integrity.
2. IDs, record revisions, references, and types.
3. Predicate/proposition/assertion typing.
4. Semantic-link endpoint and impact-mode validation.
5. Explicit contradiction and status checks.
6. Support, definition, cycle, and traceability constraints.
7. Content-binding and semantic-review freshness.
8. Transaction impact and review obligations.
9. Required review-run completion and concern disposition.
10. Profile validators and commit policy.

Later phases skipped because prerequisites failed are explicitly inconclusive.

### 7.2 Diagnostics

Every diagnostic includes:

- Stable code and rule version.
- Result class and severity.
- Message, primary ID, related IDs, and source location.
- Evidence or impact/counterexample path.
- Suggested repair categories when safe.
- Deterministic fingerprint.

Acknowledgements bind to fingerprints and expire when evidence changes.

### 7.3 Deterministic versus workflow enforcement

Examples:

- “Section A explicitly uses definition D” is deterministic.
- “Changing D impacts A through this path” is deterministic.
- “A was reviewed against the projected D by reviewer X” is an auditable
  workflow fact.
- “A remains logically persuasive” is not generally deterministic.

The product should make all four visible without conflating them.

## 8. Document and technical profile

The first profile applies the common model to technical designs, specifications,
whitepapers, and research-oriented documents.

Profile vocabulary includes:

- Terms and definitions.
- Requirements and constraints.
- Assumptions and hypotheses.
- Observations and measurements.
- Calculations and derived claims.
- Architecture/design decisions.
- Implementations and interfaces.
- Tests and verification evidence.
- Conclusions and recommendations.
- Citations and source versions.

Initial deterministic rules focus on traceability, not pretending to peer review
the document:

- Definitions used by covered content resolve uniquely.
- Accepted conclusions/decisions have policy-required support.
- Derivation cycles are rejected when policy declares them invalid.
- Requirements have required implementation and verification coverage.
- Changed content has current semantic review.
- Changed assumptions propagate review obligations to dependent claims and
  sections.

Optional heuristic profiles may check terminology, stale values, missing
qualifications, unsupported prose, or likely unmodeled dependencies.

The profile does not certify scientific validity, engineering safety, citation
quality, patentability, or legal compliance.

## 9. Narrative profile

Fiction extends the common graph rather than redefining it.

Additional records include:

- Timelines and ordered time points.
- Temporally scoped assertions.
- Events with causal prerequisites.
- Perspective records for knowledge, belief, suspicion, denial, and
  unawareness.
- Narrative presentation order and disclosure.
- Mystery clues and explicit deduction rules.

A linear novel is primarily an ordered content artifact plus temporal and
perspective constraints. It does not require general game-state exploration.

Narrative claims and prose sections use the same common bindings, impact graph,
transactions, review obligations, and context packets as technical documents.

## 10. Interactive-state profile

An interactive world is still a static canonical specification. It adds:

- Finite state-variable definitions.
- Conditions over propositions and state values.
- Effects that update them.
- Transition graphs.
- Invariants and reachability constraints.

A concrete runtime state is derived from initial values plus a path of actions.
The canonical project does not store every possible complete state.

Bounded analysis explores `(node, relevant abstract state)` with deterministic
ordering and memoization. It can prove reachability properties within a finite
model or return a shortest counterexample. Limit exhaustion is inconclusive.

Typed conditions and effects are necessary. Merely adding more unlabelled graph
connections cannot represent mutually exclusive branches or state changes.

This profile follows the linear document and narrative profiles; its complexity
must not shape the common POC schema prematurely.

## 11. Context assembly

For a task or change, context selection starts with seed records and includes, in
deterministic priority order:

1. Seeds.
2. Applicable constraints and definitions.
3. Forward dependencies needed to understand them.
4. Direct and transitive impacted dependents.
5. Relevant content units and profile records.

Packets include exact project revision/hash, record IDs/hashes, selection paths,
limits, omissions, and disclosure scope. Truncation is explicit.

This is the main scaling mechanism for AI authors: provide the relevant semantic
neighborhood rather than the whole document.

## 12. Agent-first interface

The primary API is semantic and structured. Required use-case families:

- Initialize, inspect, and version a project.
- Import or author content units.
- Create/query subjects, claims, bindings, sources, links, and constraints.
- Begin, apply, validate, review, commit, and abort transactions.
- Compute impact and explain dependency paths.
- List and disposition review obligations.
- Submit review runs, concerns, resolutions, and acknowledgements.
- Build context/review packets.
- Export normalized JSON, Markdown, and reports.
- Invoke profile analysis when installed.

The CLI is the first host. It uses versioned JSON output, stable exit codes, and
no prompts in non-interactive mode. Natural-language query parsing is not
required.

## 13. Storage and serialization

The POC uses a single canonical JSON snapshot and separate draft transaction
files. Content units are stored in the snapshot so atomicity is straightforward.

Requirements:

- Strict schema version and duplicate-property rejection.
- Deterministic property/record ordering.
- Culture-independent values and timestamps.
- Explicit tagged values.
- Namespaced extension payloads with coverage reporting.
- Explicit migrations; no silent guessing or lossy downgrade.
- Content hashes and project hash verification.

A Markdown importer/exporter is derived tooling. Round-trip import must preserve
stable unit IDs or propose an explicit mapping for confirmation.

Storage may later move to an immutable directory/object store, SQLite, or a
service. Domain and validation APIs operate on snapshots and remain storage
independent.

## 14. Export and disclosure

Initial exports:

- Canonical normalized JSON.
- Reconstructed ordered Markdown document.
- Dependency/impact report.
- Review-obligation checklist.
- Focused context/review packet.
- Coverage report.

Later profiles add technical traceability matrices, narrative continuity
references, mystery matrices, and runtime packages.

Every artifact includes project ID, revision, content hash, export profile/version,
disclosure scope, generation time, and a generated-artifact warning.

Disclosure filtering selects records structurally before formatting or AI use.

## 15. AI-assisted authoring and review

AI is a reviewer and proposal source around the deterministic core.

An AI review may be mandatory under project policy, but:

- It receives an auditable bounded context packet.
- Its output follows a structured schema.
- Every concern cites supplied record IDs.
- Every candidate claim/link remains noncanonical until accepted in a
  transaction.
- Every cache entry includes snapshot, transaction, packet, prompt, provider,
  model, and parameter hashes.
- Provider failure is reported as failed/inconclusive review, not a content
  validation error.

The core and validation projects have no provider dependency. The product can
also accept review results from an external agent without making its own API
call.

## 16. Medium and provider independence

The common core has no dependency on a word processor, game engine, rules
edition, database, UI, or model provider.

Profiles/adapters may add:

- Markdown, DOCX, LaTeX, or publishing integration.
- Requirements/test systems and citation managers.
- Unity, Unreal, Godot, dialogue, or tabletop exports.
- Technical, scientific, patent-drafting, or narrative review prompts.

Adapters cannot weaken common transaction, integrity, evidence-level, or review
provenance rules.

## 17. Plugin and tool integration

Long-term agent integration should expose a controlled MCP server backed by
application use cases and a companion workflow skill. It must not expose an
unrestricted shell or make the integration layer authoritative.

As of 2026-08-10, OpenAI plugins may package skills, MCP servers, and optional UI
with a `.codex-plugin/plugin.json` manifest:

- [OpenAI plugin architecture](https://developers.openai.com/plugins/concepts/plugins)
- [OpenAI plugin packaging](https://developers.openai.com/plugins/build/plugins)

Packaging is gated until the headless application API and common document POC
are stable. Re-check current official documentation at implementation time.

## 18. Project boundaries

```text
ValidatedWorld.Core                 (no project dependencies)
├── ValidatedWorld.Serialization    (Core)
├── ValidatedWorld.Validation       (Core)
├── ValidatedWorld.Generation       (Core, Validation)
└── ValidatedWorld.Export           (Core, Validation)

ValidatedWorld.Application          (Core, Serialization, Validation)
ValidatedWorld.Cli                  (Application, Export, Generation)
ValidatedWorld.Mcp                  (later; Application, Export, Generation)
```

`ValidatedWorld.Application` is the one planned project addition in blueprint
WP0. Profile records remain in Core only when they are provider/engine-neutral;
profile validators belong in Validation or a later profile assembly when one is
actually needed.

## 19. Proof-of-concept gates

### 19.1 Gate A — Technical document dependency POC

Use a small technical design containing:

- Ordered sections.
- Requirements, assumptions, a derived estimate, decisions, and a verification
  plan.
- Content bindings and semantic links.
- One transaction that changes an assumption.
- Several legitimately impacted claims/sections and one unrelated section.

The POC must:

1. Strictly load and hash canonical JSON.
2. Apply semantic operations in an isolated transaction.
3. Build base/projected dependency graphs.
4. Produce the exact transitive impact set with explained paths.
5. Create policy-required review obligations.
6. Invalidate dispositions when transaction evidence changes.
7. Detect explicit contradictions, missing support, invalid derivation cycles,
   traceability gaps, and stale content review.
8. Reject invalid, pending-review, or stale commits without modifying canon.
9. Atomically commit a valid fully reviewed change.
10. Export Markdown, impact/review reports, and a focused context packet.

No AI API, timeline, mystery, state exploration, GUI, or plugin is required.

### 19.2 Gate B — Heuristic review evaluation

Use an external/fake review interface first, then evaluate an optional real
provider on missing implicit links and stale prose. Measure usefulness and cost.

### 19.3 Gate C — Narrative profile

Add a linear Harbor mystery for chronology, perspective, clue, and disclosure
validation.

### 19.4 Gate D — Interactive-state profile

Add a miniature branching scenario with declared state variables, transitions,
invariants, and deliberately bounded exploration only after the linear profile
demonstrates value.

## 20. Success and stop criteria

Gate A succeeds if realistic changes reliably surface the right dependent claims
and sections, required review prevents silent staleness, annotation cost is
acceptable, reports are deterministic, and agents can repair failures from
structured evidence.

If typed claims are too burdensome, retain section-level semantic links and
review obligations. If narrative profiles fail, keep the technical/document
product. Do not broaden claims merely because a demo is visually impressive.

## 21. Non-goals

The common POC does not:

- Prove arbitrary prose, scientific truth, engineering safety, or legal validity.
- Automatically accept extracted claims or links.
- Build a universal ontology or unrestricted rules language.
- Perform general mathematical proof or full citation fact-checking.
- Generate a complete paper, novel, or game autonomously.
- Explore interactive state before the common graph passes Gate A.
- Replace source control, a word processor, peer review, a game engine, or a
  requirements-management suite.
- Build a GUI, natural-language query language, or public plugin first.
- Promise schema backward compatibility during the POC.

## 22. Durable direction

> Turn the implicit semantic web inside a large authored project into an
> explicit, inspectable dependency graph; validate what can be proven, force
> review where it cannot, and commit only a coherent acknowledged change.

Implement the smallest complete version of that loop before adding profile
complexity.

# ValidatedWorld Product and Architecture Specification

**Status:** Authoritative product specification

**Specification version:** 2.0

**Last reviewed:** 2026-08-10

**Primary implementation:** .NET 10 / C#
**Canonical interchange format:** JSON

This document defines what ValidatedWorld is and the architectural boundaries
that implementations must preserve. Detailed types, algorithms, work packages,
and acceptance tests are in [implementation_blueprint.md](implementation_blueprint.md).
The honest guarantee boundary is in [feasibility.md](feasibility.md).

Human direction overrides this document. When a product decision changes, update
this specification and the blueprint in the same change.

---

## 1. Product thesis

ValidatedWorld is a **consistency-validation and context-assembly system for
long-form authored worlds**. It helps humans and AI agents create novels, games,
quests, campaigns, and other continuity-heavy works without relying on a model's
context window as the source of truth.

The central workflow is:

```text
load a canonical snapshot
→ begin an isolated transaction
→ query relevant canon
→ propose semantic changes
→ build the projected snapshot
→ compute impact
→ validate and analyze
→ repair, acknowledge, or reject findings
→ commit atomically
→ export audience-specific artifacts
```

The product does not make an AI remember a whole novel. It gives the AI a small,
precise context packet for the task, then checks the proposed result against
structured canon before accepting it.

The initial target is fiction—especially a mystery outline—because mysteries
stress time, knowledge, evidence, motive, disclosure, and causality at once. The
core must remain medium- and engine-independent.

## 2. Product promise and coverage

ValidatedWorld guarantees only what is represented in its semantic model.

Every validation report must distinguish:

- **Proven:** deterministic rules completed and the property holds in the
  declared model.
- **Disproven:** deterministic rules found a violation, ideally with a minimal
  counterexample.
- **Inconclusive:** a configured analysis bound, unsupported construct, missing
  annotation, or internal failure prevented a conclusion.
- **Concern:** a heuristic or AI reviewer identified a possible issue.

An incomplete or bounded-out check is never reported as a pass.

Important details become enforceable when they are modeled as entities,
propositions, assertions, perspectives, events, state, constraints, or narrative
annotations. Freeform prose is allowed, but details present only in prose remain
outside deterministic coverage. The system should report coverage so users know
which parts are protected.

## 3. Mental model

ValidatedWorld combines:

- An immutable-at-a-revision canonical world snapshot.
- An optimistic, atomic transaction system.
- A typed semantic graph derived from canon.
- Deterministic validators and explainable diagnostics.
- Bounded traversal of finite narrative state models.
- Queries and context packets designed for AI agents.
- Optional, auditable AI-assisted review.
- Profile-driven exports.

It is not primarily a graph editor, database server, game engine, dialogue
runtime, prose generator, or general theorem prover. Those systems may consume
or integrate with it.

## 4. Foundational distinctions

Several concepts must remain separate. Collapsing them creates subtle but fatal
design errors.

### 4.1 Canon definition, world state, and narrative order

The canonical project defines possible or authored reality. A **world state** is
a derived snapshot at a point in a timeline or after a sequence of story
transitions. **Narrative order** is the order in which an audience experiences
scenes or disclosures. These orders are related but not identical: a flashback
may depict an earlier event in a later chapter.

The application must never treat the entire story as one timeless bag of facts.

### 4.2 Proposition, truth, and perspective

A proposition is content such as `killed(mayor, merchant)`. It is not true merely
because it exists. Canon assertions state whether the proposition is true or
false in a declared scope. Perspective records state whether an actor knows,
believes, suspects, doubts, denies, or claims that proposition.

Canonical truth never automatically becomes character knowledge.

### 4.3 Absence and negation

The default semantic policy is **open world**. The absence of a positive
assertion means unknown or unmodeled, not false. Negative canon and perspective
claims must be explicit when they matter.

Individual predicates may opt into a documented closed-world policy, but the
choice is schema data and validation output must disclose it.

### 4.4 World time and authoring revision

World time describes fictional chronology. A canonical revision describes the
history of edits to the project. They are unrelated axes. Editing an ancient
event today advances the project revision but does not move the event in world
time.

### 4.5 Canon and derived artifacts

Canonical source data, committed through ValidatedWorld, is authoritative.
Runtime JSON, lore books, prose drafts, checklists, indexes, diagrams, and AI
context packets are generated views. Generated artifacts must identify their
source revision and must not be edited as authoritative data.

## 5. Canonical semantic model

All addressable records have a stable, opaque ID. IDs do not change when display
names change. References use IDs, never duplicated names.

### 5.1 World snapshot

A world snapshot contains at least:

- World identity and schema version.
- Canonical revision and content hash.
- Project policy and analysis limits.
- Entity and predicate definitions.
- Propositions and scoped canon assertions.
- Perspective and provenance records.
- Timelines, time points, and events.
- Narrative graphs and optional selected traces.
- Constraints and disclosure policy.
- Prose or document annotations when present.

The first implementation may store the snapshot in one JSON file. Storage
layout is not part of the semantic contract; normalized JSON export is.

### 5.2 Entities

An entity represents a thing with identity, for example:

- Character, persona, or audience role.
- Location or region.
- Faction or organization.
- Item, document, clue, or other object.
- Mystery, quest, story arc, scene, or chapter.
- Custom project-specific kinds.

Every entity has a kind, display name, optional aliases, optional freeform
description, tags, and explicit links. Medium-specific data belongs in extension
payloads owned by adapters, not in hard-coded engine dependencies.

### 5.3 Predicate definitions and propositions

A predicate definition declares:

- Stable predicate ID.
- Ordered argument names and value types.
- Which argument positions form a cardinality key, when applicable.
- Maximum simultaneous positive values for that key, when applicable.
- Optional symmetry or inverse metadata used only by declared validators.
- Open- or closed-world policy.

A proposition is a predicate plus typed arguments. It has its own stable ID so
assertions, dialogue, deductions, and perspectives can refer to exactly the same
content.

The predicate catalog is a deliberately small, typed vocabulary. It is not a
natural-language parser or an unrestricted ontology language. Projects may add
custom predicates; custom semantics require custom constraints or validators.

### 5.4 Canon assertions

A canon assertion gives a proposition:

- Positive or negative polarity.
- A world-time validity interval, if temporal.
- A timeline or scenario scope, if not globally true.
- Evidence/source references for author-facing traceability, when useful.

Two opposite assertions in overlapping scopes are contradictory. Two different
positive values for a single-valued predicate key in overlapping scopes are
also contradictory.

Immutable background facts and changing state can share the proposition model,
but frequently changed finite values should be represented as declared state
variables and event effects so path analysis remains tractable.

### 5.5 Perspective and information provenance

A perspective record contains:

- Holder: character, group, audience, narrator, or player role.
- Proposition and polarity.
- Attitude: knows, believes, suspects, doubts, denies, or claims.
- World-time or narrative validity scope.
- Provenance: witness event, speaker, document, clue, deduction, public
  knowledge, author declaration, or another explicit source.
- Confidence or certainty only when the project needs it.

`knows` is factive in the deterministic model: the corresponding canon assertion
must be true in scope. A false conviction is `believes`, not `knows`.

Knowledge propagation is explicit. The engine must not assume that everyone at
a location heard a statement, that every faction member shares all knowledge, or
that truth is public.

### 5.6 Time, events, and causality

A timeline supplies an ordered set of time points. A time point has a stable ID
and deterministic ordinal; optional calendar text is presentation data.

An event has:

- Stable ID and timeline position or interval.
- Participants and location references.
- Preconditions expressed in the safe condition AST.
- Effects expressed in the safe effect AST.
- Causal prerequisite event references.
- Optional witness or disclosure annotations.

Events capture durable changes relevant to continuity. ValidatedWorld is not
required to event-source every moment of a game.

### 5.7 Narrative graphs and traces

A narrative graph models authored progression. Node kinds may include scenes,
chapters, quest states, encounters, or beats. Every node may declare:

- Entry conditions.
- State and perspective effects.
- Optional depicted fictional time, separate from narrative stage/order.
- A presentation-only mode for flashbacks that may disclose information without
  mutating current branch state.
- Assertions, claims, clues, and disclosures presented there.
- Referenced entities and prose artifacts.
- Outgoing transitions with conditions and effects.

The condition/effect language is a finite, typed data AST. It is not C#, a
script string, or arbitrary user code.

Different media use the graph differently:

- A novel or screenplay normally supplies a selected linear trace. The validator
  replays every node in order.
- A game supplies a finite branching graph. The analyzer explores reachable
  abstract states within explicit bounds.
- A campaign may declare prepared branches and leave the rest open. Reports must
  mark undeclared play as outside coverage.

A quest is a narrative graph or subgraph with domain metadata. The core should
not pretend that every novel arc is a quest state machine.

### 5.8 Mysteries, clues, and deductions

A mystery definition may declare:

- One or more solution propositions.
- Suspects and relevant motive/opportunity propositions.
- Clues and acquisition nodes.
- Red herrings as author annotations.
- Explicit deduction rules from available evidence to conclusions.
- Earliest allowed disclosure stages.
- Stages or terminal states where the solution must be derivable.
- Required clue-route redundancy.

Deduction rules are explicit finite rules, not natural-language inference. This
lets the system validate availability and author-declared fair-play structure.
It does not prove that a human reader will notice a clue or find the solution
satisfying.

### 5.9 Prose and semantic annotations

Freeform text is stored as an artifact or external source reference. A prose
artifact may cite:

- Entities and propositions it uses.
- Claims it asserts or contradicts.
- Information it reveals to an audience.
- Narrative node and point-of-view context.

These annotations create deterministic dependencies. A linter or AI review may
suggest missing annotations, but extracted claims do not become canon without a
transaction.

Long generated prose should retain source references at scene or section
granularity. When canon changes, impact analysis can then identify sections to
regenerate or review instead of rereading hundreds of pages.

## 6. Safe condition, effect, and constraint languages

The initial condition AST supports a small closed set:

- All, any, and not.
- Proposition holds with polarity.
- State variable equals a typed value.
- Perspective has a declared attitude toward a proposition.
- Narrative flag is present.

Initial effects support:

- Set a finite state variable.
- Assert or retract a transient proposition in analyzed story state.
- Add, replace, or remove a perspective attitude.
- Add a narrative flag.

Initial project constraints are typed records such as:

- Required or forbidden reachability.
- Forbidden disclosure before a boundary.
- Required fallback when an actor is unavailable.
- Minimum clue acquisition routes.
- Mutual exclusion.
- Required terminal outcome.

The languages must be deterministic, serializable, type-checkable, and safe to
evaluate without executing project code. Extension assemblies may add validators
through a versioned interface, but loading untrusted extensions is an explicit
host decision.

## 7. Transactions and revisions

All canonical authoring occurs in a transaction based on an exact snapshot
revision and content hash.

A transaction records:

- Transaction ID, base revision/hash, intent, and author metadata.
- Ordered semantic operations with optimistic preconditions.
- The direct changed record set.
- Derived impact set and validation results.
- Acknowledgements for eligible non-blocking diagnostics.
- Status and commit metadata.

Operations target stable record IDs; they do not use fragile JSON array indexes.
Add, replace, and remove operations are distinct. Replace and remove include the
expected record revision or hash.

Drafts may be temporarily incomplete. Draft validation provides fast feedback.
Commit validation builds the complete projected snapshot and applies project
policy.

Commit is optimistic and atomic:

1. Acquire the workspace commit lock.
2. Re-read the canonical head.
3. Reject a stale base revision rather than silently merge it.
4. Apply operations and verify operation preconditions.
5. Build indexes and validate the projected snapshot.
6. Evaluate commit policy, acknowledgements, and analysis completeness.
7. Serialize deterministically and compute the content hash.
8. Atomically replace the canonical snapshot.
9. Record auditable commit metadata.

If any required step fails, canon remains byte-for-byte at the previous
revision. Branching and merge are later features; a merge is always a new
transaction validated against the target head.

## 8. Dependency and impact graph

The dependency graph is derived, not separately authored. Nodes correspond to
addressable records. A record has a dependency edge to every record referenced
by its typed fields, expressions, propositions, annotations, and constraints.

For a change, the impact set is the changed nodes plus their reverse transitive
dependents in the union of the base and projected graphs. Using both graphs is
necessary to catch dependencies removed by the transaction.

The graph supports:

- `dependencies`: what a record relies on.
- `dependents`: what relies on a record.
- `affected-by`: likely review and validation surface for a change.
- `why`: an edge-by-edge explanation path.
- Deterministic context assembly for agents and AI reviewers.

Impact means “must be checked,” not “must be edited.” Free-text relationships
that have no reference or annotation are not guaranteed to appear in the graph.

## 9. Deterministic validation

Deterministic validation is the product backbone. Validators are pure with
respect to a supplied snapshot and request. Results are stable-sorted and have
stable codes.

### 9.1 Validation phases

Run in dependency order:

1. Parse and schema compatibility.
2. Identity, reference, and type integrity.
3. Predicate, expression, and extension payload typing.
4. Assertion scope, polarity, cardinality, and temporal consistency.
5. Event chronology and causal prerequisites.
6. Perspective truth/provenance rules.
7. Narrative trace replay and graph structural checks.
8. Bounded reachability and project constraints.
9. Mystery and disclosure rules.
10. Commit policy and coverage completeness.

Later phases may stop when prerequisite phases make their results meaningless,
but they must emit an explicit skipped/inconclusive status.

### 9.2 Diagnostics

Every diagnostic includes:

- Stable code and rule version.
- Severity and result class.
- Concise message.
- Primary and related record IDs.
- Source span when available.
- Evidence or a replayable counterexample.
- Suggested repair categories when safe.
- A deterministic fingerprint for acknowledgements.

Severities are error, warning, information, concern, and internal failure.
Acknowledgements apply only to policy-eligible warnings or concerns. They are
bound to the diagnostic fingerprint and expire when relevant evidence changes.

### 9.3 Bounded analysis

Narrative exploration operates on a finite abstract state containing only state
referenced by the analyzed graph and constraints. The engine traverses in stable
order and memoizes canonical state keys.

Every report includes limits such as maximum states, transitions, depth, and
wall-clock budget. Reaching a limit produces an inconclusive diagnostic. Commit
policy decides whether a required inconclusive analysis blocks commit; the
recommended default is to block.

### 9.4 Incremental validation policy

The proof of concept performs full commit validation because the worlds are
small and correctness is more valuable than optimization. Impact sets may scope
draft checks and AI context.

Incremental commit validation is permitted later only after tests show that its
result is equivalent to full validation for every validator in scope.

## 10. AI-assisted authoring and review

AI is an adapter around the deterministic core, not an authority inside it.

An AI review request is immutable and auditable. It records the snapshot hash,
transaction hash, review profile, provider/model identifier, prompt/template
version, context record IDs/hashes, truncation information, and output.

Review context is assembled from explicit dependencies and constraints. Results
are cached by complete request hash. An AI finding is a concern until a
deterministic transaction adds or repairs canon.

The core and validation projects have no OpenAI or other model-vendor
dependency. Provider implementations live behind interfaces in the generation
or host layer.

Generated prose is derived output. It receives a disclosure-safe continuity
packet and should return source citations or structured annotations. The system
may review the prose, but must not claim the prose is proven consistent.

## 11. Agent-first application interface

The primary interface is semantic and structured. Agents should not need to
rewrite canonical JSON directly or remember which validators a commit requires.

Required use-case families include:

- Initialize, inspect, and version a world.
- Begin, inspect, apply, validate, commit, and abort transactions.
- Get records and query by typed filters.
- Trace dependencies, impact, truth, perspective provenance, and reachability.
- Explain a diagnostic or counterexample.
- Produce continuity/context packets.
- Export canonical and human-readable views.

The CLI is the first host and must support JSON output with a versioned envelope,
stable exit codes, and no prompts in non-interactive mode. A natural-language
query parser is not required. Semantic operations should map cleanly to later
MCP tools.

## 12. Storage, serialization, and migration

The POC uses a single canonical JSON snapshot plus separate draft transaction
files. This keeps atomic commit and deterministic hashing straightforward.

Serialization requirements:

- Strict schema version.
- Deterministic property and record ordering for canonical output.
- Culture-independent numbers and timestamps.
- Rejection of duplicate JSON properties.
- Preservation of unknown extension payloads only under declared namespaces.
- Explicit migrations between supported schema versions.
- No silent guessing or lossy downgrade.

External or legacy JSON imports run through mapping adapters and create draft
transactions. Import never silently becomes canon.

Storage may later move to an immutable object store, SQLite, or service, but the
domain and validation APIs operate on snapshots and must not depend on a storage
engine.

## 13. Export and disclosure

Exports are profile-driven projections from a committed snapshot or an
explicitly labeled draft preview.

Initial profiles:

- Canonical normalized JSON.
- Human continuity reference.
- Context packet for one scene, character, clue, or change.
- Audience-safe story outline.
- Mystery matrix and clue timeline.
- QA checklist of relevant declared facts.

Every artifact includes generation metadata: world ID, revision, content hash,
profile/version, disclosure scope, timestamp, and a generated-file warning.

Disclosure filtering is structural. Explicit disclosure rules select records
before prose generation. Prompt instructions alone are not an acceptable
secrecy boundary.

## 14. Medium and engine independence

The core describes semantic fiction concepts and finite narrative state. It has
no dependency on Unity, Unreal, Godot, Ink, Yarn, a tabletop edition, a word
processor, or an AI provider.

Adapters may add:

- Engine asset/schedule/animation grounding.
- Runtime quest or dialogue export.
- Tabletop stat blocks and campaign books.
- Novel chapter and scene documents.
- Mod packages represented as semantic transactions.

Adapter-specific extension data is namespaced, schema-versioned, and validated
by the adapter. It cannot weaken core invariants.

## 15. Plugin and tool integration

The long-term agent integration should expose a controlled MCP server backed by
the application use cases, with a companion skill that teaches agents the safe
authoring workflow. It should not expose an unrestricted shell or make the MCP
layer the source of truth.

As of 2026-08-10, OpenAI's plugin architecture packages skills, MCP servers, and
optional UI, and uses `.codex-plugin/plugin.json` as the package manifest. The
integration should therefore eventually be packaged as a plugin containing:

- A ValidatedWorld authoring skill.
- A local or remote MCP server exposing semantic query/transaction tools.
- Optional UI only after headless workflows are complete.

References:

- [OpenAI plugin architecture](https://developers.openai.com/plugins/concepts/plugins)
- [OpenAI plugin packaging](https://developers.openai.com/plugins/build/plugins)

This packaging is an adapter milestone, not a reason to couple the core to a
current vendor format. Re-check the official documentation when implementation
begins.

## 16. Commit policy

Project policy determines which findings block commit. The default POC policy:

- All parse, structural, reference, type, contradiction, temporal, and selected
  narrative errors block.
- Required bounded analysis that is inconclusive blocks.
- Warnings require no acknowledgement unless a specific project contract says
  otherwise.
- AI concerns never block by default.
- Changing a declared mystery solution requires explicit human approval in
  interactive hosts; non-interactive hosts fail with an approval-required
  result.

Policy cannot suppress internal failures, hash/revision conflicts, or invalid
acknowledgement fingerprints.

## 17. Initial solution boundaries

The target project dependency direction is:

```text
ValidatedWorld.Core                 (no project dependencies)
├── ValidatedWorld.Serialization    (Core)
├── ValidatedWorld.Validation       (Core)
├── ValidatedWorld.Generation       (Core, Validation)
└── ValidatedWorld.Export           (Core, Validation)

ValidatedWorld.Application          (Core, Serialization, Validation)
ValidatedWorld.Cli                  (Application, Export, Generation adapters)
ValidatedWorld.Mcp                  (later; Application, Export, Generation)
```

`ValidatedWorld.Application` is the one planned addition to the current scaffold
in blueprint work package 0. It owns use-case orchestration, workspace locking,
transaction lifecycle, commit, and queries. Core stays independent of files,
JSON, consoles, networks, model providers, and engines.

The blueprint defines exact namespace and work-package guidance.

## 18. Proof-of-concept scope

The POC proves one end-to-end vertical slice with a small Harbor mystery:

- 5–10 characters, 2 factions, 3 locations, and key items/documents.
- 20–40 propositions with positive/negative and temporal assertions.
- At least one false belief and one provenance chain.
- Ordered events including one character availability change.
- A small branching narrative graph and an authored linear trace.
- One solution, several clues, explicit deduction rules, and a reveal boundary.
- A fallback path when a required character is unavailable.

The POC must:

1. Load and strictly validate canonical JSON.
2. Begin and persist a transaction.
3. Apply semantic operations and build a projected snapshot.
4. Derive base/projected dependency graphs and an explained impact set.
5. Detect reference, contradiction, temporal, perspective, disclosure, and
   reachability failures.
6. Produce a replayable counterexample for a path failure.
7. Reject a stale or invalid commit without modifying canon.
8. Atomically commit a valid change.
9. Export normalized JSON and a human continuity packet.
10. Return deterministic JSON command results suitable for an agent.

AI calls, a GUI, a plugin package, rich prose generation, universal import, and
engine adapters are not required to prove the thesis.

## 19. Success and stop criteria

Success is not measured by prose volume. It is measured by whether an agent can
make realistic structured changes without degrading declared continuity.

The evaluation corpus must include intentional errors and expected diagnostics.
Repeated deterministic validation of the same snapshot must produce equivalent
ordered results. Failed commits must leave the canonical file unchanged.

Do not expand scope until the POC evaluation described in
[feasibility.md](feasibility.md) demonstrates useful defect detection at an
acceptable annotation cost. If full narrative-state modeling is too expensive,
retain the smaller useful product: typed canon, linear trace validation,
dependency impact, and continuity packet generation.

## 20. Non-goals

The initial product does not:

- Prove arbitrary prose consistent.
- Generate a complete novel autonomously.
- Judge literary quality, emotion, fun, or commercial value.
- Build a universal ontology or unrestricted rules language.
- Exhaust unbounded game or tabletop play.
- Replace a game engine, dialogue runtime, version-control system, or document
  editor.
- Automatically accept AI-extracted statements as canon.
- Require every descriptive detail to be formalized.
- Build a visual graph editor before the agent-first workflow works.
- Promise backward compatibility while the POC schema is still experimental.

## 21. Durable direction

The enduring concept is:

> Model the continuity that matters, validate every proposed canonical change
> against an explicit and bounded semantic world, report what was and was not
> proven, and publish the resulting canon into any medium.

Implement the smallest complete version of that loop before adding breadth.

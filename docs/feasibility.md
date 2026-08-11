# Feasibility, Limits, and the Smallest Useful Product

**Status:** Accepted product boundary

**Last reviewed:** 2026-08-11

## Verdict

ValidatedWorld is feasible and useful if it is built as a **claim, dependency,
and review compiler for authored projects**.

It is not feasible as a machine that reads unrestricted prose and proves every
statement correct. That does not make natural-language review irrelevant. A
heuristic reviewer can discover likely claims and implicit connections, focus on
the sections affected by a change, and be required by commit policy. The system
must distinguish “this review ran and its findings were resolved” from “this
property was mathematically proven.”

The common product sits between:

- A structured document model.
- A dependency and traceability graph.
- A transaction system.
- A static analyzer.
- A mandatory review workflow.
- Optional profile-specific model checking.

This applies to technical designs and papers as directly as it applies to
fiction. The content differs; the common problem is the same: a sequential
document hides a web of semantic dependencies that becomes difficult for a human
or AI to maintain as it grows.

## The shared abstraction

Every supported project can contain:

- **Content units:** sections, paragraphs, figures, tables, scenes, requirements,
  design components, or other stable authored units.
- **Subjects:** named concepts, terms, systems, people, locations, variables, or
  objects.
- **Claims:** facts, assumptions, hypotheses, requirements, observations,
  conclusions, decisions, definitions, or recommendations.
- **Evidence:** citations, measurements, datasets, calculations, documents,
  tests, events, or clues.
- **Semantic links:** depends on, supports, contradicts, refines, supersedes,
  defines, uses, implements, verifies, reveals, and profile-specific relations.
- **Constraints:** explicit rules that must remain true.

The operational dependency graph is derived from those records. A change to a
record walks the reverse graph and produces an explained impact set.

That is the core product. Fictional chronology, character knowledge, and game
state are additional semantics, not prerequisites for document impact analysis.

## What can be guaranteed

The product must label every result by evidence strength.

### Proven by deterministic validation

Given complete explicit records for a rule, ValidatedWorld can prove or disprove
properties such as:

- IDs are unique and references resolve to compatible record types.
- A link connects allowed source and target kinds.
- An accepted claim explicitly contradicts another accepted claim in the same
  declared scope.
- A derived claim has the support required by project policy.
- A definition, requirement, or decision has required traceability links.
- A derivation dependency contains a forbidden cycle.
- An authored content unit's semantic review applies to its current text hash.
- A proposed change's transitive impact set is complete with respect to all
  explicit references and semantic links.
- Every policy-selected impacted unit has an explicit review disposition.
- A transaction is based on the current canonical revision and commits
  atomically.

Narrative profiles can additionally prove declared chronology, knowledge,
disclosure, and finite reachability properties.

### Proven within an explicit finite model

For a finite transition specification, the system can exhaustively explore
reachable abstract states within configured limits. It may prove a declared
property or produce a replayable counterexample. If a limit is reached, the
result is **inconclusive**, not successful.

This is principally needed for games and branching narratives, not for the first
document/dependency product.

### Required but heuristic review

AI-assisted or text-linting review can be required to run and can block commit
until its concerns are resolved or explicitly acknowledged. It can:

- Propose claims implicit in a changed section.
- Propose missing links to definitions, assumptions, evidence, or downstream
  conclusions.
- Compare changed text with structured annotations.
- Flag likely contradictions, stale numbers, inconsistent terminology, missing
  qualifications, unsupported conclusions, or accidental disclosures.
- Rank impacted content for human or agent attention.

These findings remain **concerns**. Requiring the review is a workflow guarantee;
accepting the reviewer's semantic judgment as truth would not be.

## What cannot be guaranteed

ValidatedWorld cannot generally:

- Recover every claim or implication from unrestricted natural language.
- Know every implicit dependency an author failed to model and reviewers failed
  to notice.
- Prove that a scientific claim is true, a citation is reliable, a design will
  work, or a paper is persuasive.
- Prove patent novelty, enablement, legal sufficiency, or freedom to operate.
- Judge literary quality, emotion, fun, originality, or commercial value.
- Exhaust an unbounded game or tabletop campaign state space.
- Decide arbitrary logic expressed in an unrestricted rules language.
- Guarantee that generated prose is consistent merely because its structured
  outline was valid.

The UI, CLI, README, and reports must not imply otherwise.

## The annotation and review bargain

The system gains deterministic power when important meaning crosses the
**semantic boundary**: content is bound to claims, and claims are connected by
typed links or constraints.

Authors should not manually draw every low-level edge. Three sources contribute
to the graph:

1. Typed references in canonical records generate edges automatically.
2. Humans or authoring agents add intentional semantic links.
3. Heuristic review proposes missing claims and links for confirmation.

Only confirmed records become canonical. Candidate links retain provenance so
the project can distinguish manual statements from AI suggestions.

Not every sentence needs a formal claim. A project policy selects which content
requires semantic review—for example every requirement and conclusion, but not
formatting prose. Coverage reports show which content is mapped, stale, or
outside deterministic protection.

## The simplest useful change workflow

Suppose a technical design contains:

- A requirement that a sensor operate for 24 hours.
- An assumption about average current draw.
- A power-budget conclusion derived from that assumption.
- A battery decision depending on the conclusion.
- Architecture and verification sections bound to those claims.

Changing the current-draw assumption should deterministically identify the
derived conclusion, battery decision, architecture section, and verification
plan as impacted. Before commit, each receives one disposition:

- `updated` — it changed in the transaction;
- `reviewed-no-change` — it remains valid, with a reason;
- `not-applicable` — the dependency path does not require action, with a reason;
- `pending` — commit remains blocked when policy requires disposition.

An AI reviewer may notice that a number in the power-budget prose is now stale.
Even if it does not, the explicit graph still forces the relevant section to be
looked at. This is useful without a theorem prover, timeline, or game simulator.

## Why transactions matter

A transaction groups changes to content, claims, links, and review dispositions.
Canon advances from one accepted snapshot to another or not at all.

The transaction does not require every dependent record to be edited. It
requires every policy-relevant dependent record to be considered. This is closer
to a compiler plus a code-review checklist than to a database cascade.

Review dispositions are bound to the projected content hashes and impact-path
fingerprints. Changing the transaction invalidates stale dispositions.

## How interactive game state fits

The user's intuition that a game can remain a static graph is substantially
correct. The canonical game project is static: it contains variables, possible
values, conditions, effects, invariants, and transitions. A particular runtime
state is a valuation of those variables after a path of player actions.

The system should not author a separate node for every possible full state.
Instead, it derives reachable states when checking a declared property. This is
why game support is more complex than a linear document:

- Untyped “more connections” cannot say which facts are mutually exclusive.
- Edges need conditions, effects, and scopes.
- Loops can create many paths.
- State exploration can grow combinatorially.

So the canonical model is still static, but validation sometimes needs bounded
model checking. That complexity should not be imposed on the common document
core or the first POC.

## Smallest useful product

The first product should be a **transactional document dependency and review
checker**.

It should support:

- One ordered technical design document split into stable content units.
- Subjects, claims, assertion roles/statuses, evidence, and typed semantic links.
- Bindings from sections to the claims they assert, use, or discuss.
- Strict JSON load and deterministic export.
- Atomic transactions over content and semantic records.
- Base-plus-projected dependency impact analysis.
- Explained review obligations for impacted records.
- Deterministic contradiction, support, cycle, traceability, and stale-annotation
  checks.
- A context/review packet for an external human or AI reviewer.
- Structured submission and acknowledgement of heuristic concerns, without a
  built-in paid model requirement.

This slice is simpler than a mystery and directly tests the original spider-web
document idea. It can be useful on its own.

## Staged proof plan

### Gate A — Common document graph

Use a small technical design sample and realistic change transactions. Measure:

- Expected and actual impact sets.
- Missed and irrelevant impacts.
- Time needed to create/maintain links.
- Whether required review dispositions prevent stale dependent sections.
- Whether an agent can repair a transaction using structured explanations.
- Determinism of reports and failed-commit atomicity.
- Coverage of content units and semantic annotations.

If Gate A succeeds, the project already has a useful release direction.

### Gate B — Heuristic discovery/review

Evaluate one or more AI reviewers on deliberately omitted links and stale prose.
Measure proposal precision, recall against a hand-authored corpus, review cost,
and whether scoped context materially outperforms reviewing the whole document.
Do not make one provider mandatory in the core product.

### Gate C — Linear narrative profile

Add the Harbor mystery only after Gate A. First test linear chronology,
perspective, and disclosure. Retain the profile only if its added authoring
burden catches failures the common graph cannot.

### Gate D — Interactive-state profile

Only after Gate C, add a miniature branching scenario with typed variables,
conditions, effects, invariants, and bounded exploration. Retain it only if
replayable traces catch state-dependent failures that static impact and linear
narrative rules cannot.

## Stop and scale-down criteria

Keep the common document tool even if narrative modeling proves too expensive.
Scale down further to impact packets and review obligations if typed claims are
too burdensome but section-level dependencies remain useful.

Archive the experiment only if explicit dependencies plus forced review provide
no meaningful advantage over ordinary documents and unscoped AI review.

## Product language

Prefer:

> ValidatedWorld maps the important claims and dependencies hidden inside large
> documents, validates explicit rules, and forces affected material through a
> reviewable transaction.

Avoid:

> ValidatedWorld guarantees that an entire paper, novel, or game is correct.

That distinction preserves both the ambition and the credibility of the project.

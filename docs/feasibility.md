# Feasibility, Limits, and the Smallest Useful Product

**Status:** Accepted product boundary

**Last reviewed:** 2026-08-10

## Verdict

ValidatedWorld is feasible and potentially useful, but only if it is built as a
**continuity compiler for explicitly modeled story information**. It is not
feasible as a machine that reads arbitrary prose and proves that every sentence
is compatible with every other sentence.

The viable product sits between a database, a static analyzer, and a bounded
model checker:

- Authors and agents identify continuity-critical information as typed canon.
- Narrative artifacts reference the canon they rely on or reveal.
- Proposed changes are applied to an isolated projected snapshot.
- Deterministic validators check the projected snapshot.
- Bounded analysis explores declared story transitions and produces replayable
  counterexamples.
- Optional AI review looks for likely omissions in prose and annotations, but
  never upgrades a guess into a proof.

This narrower promise is still valuable. Software compilers do not prove that a
program is useful; they prevent large classes of defects. ValidatedWorld should
not claim to prove that a story is good; it should prevent large classes of
continuity defects and make the remaining uncertainty visible.

## What can be guaranteed

The product must label every result according to the strength of its evidence.

### Proven by deterministic validation

Given a valid schema and complete annotations for a rule, the system can prove
or disprove properties such as:

- IDs are unique and references resolve to compatible record types.
- Two opposite canon assertions overlap in the same declared scope.
- A single-valued relationship has two simultaneous values.
- A timeline event precedes one of its prerequisites.
- An authored linear story trace violates a scene precondition.
- A quest or story state is unreachable in a finite declared state model.
- A character reveals information without the required declared knowledge.
- A player-facing export includes material above its disclosure level.
- An atomic transaction is based on the current canonical revision.

### Proven within an explicit bound

For a finite transition system, the system can exhaustively explore all states
up to configured limits. It may prove a property within that model, or return a
short event sequence that reproduces a failure. The report must include the
model, limits, and explored-state count.

If a limit is reached, the result is **inconclusive**, not successful.

### Heuristic evidence only

AI-assisted or text-linting checks can flag likely problems such as:

- Prose appears to introduce an unmodeled claim.
- A motivation seems psychologically inconsistent.
- A scene summary may imply an impossible location or time.
- A clue may be too obvious or too obscure for the intended audience.

These are review findings. They may be useful and cacheable, but they are not
compiler truth and must not be presented as proof.

## What cannot be guaranteed

ValidatedWorld cannot generally:

- Understand every implication in unrestricted natural language.
- Know which descriptive details matter unless they are modeled or annotated.
- Prove literary quality, fun, emotional realism, or originality.
- Enumerate an unbounded game or tabletop campaign state space.
- Decide arbitrary logical statements; sufficiently expressive rule languages
  eventually encounter undecidability or unacceptable cost.
- Guarantee that generated prose contains no contradiction merely because the
  outline was valid.
- Detect a dependency that exists only in an author's mind and is absent from
  structured data, annotations, references, and review text.

The UI, CLI, and documentation must never imply otherwise.

## The annotation bargain

The system works only when continuity-critical details cross the **semantic
boundary**: they become structured propositions, state variables, events,
perspective records, narrative conditions/effects, constraints, or explicit
references from prose.

Not every adjective or object needs to cross that boundary. An author should
model a detail when changing it could invalidate another scene, clue, decision,
timeline step, relationship, or output. Freeform prose remains welcome, but
unmodeled prose is outside the deterministic guarantee.

Connections should normally be derived from stable references. Users and agents
should not hand-maintain a separate edge for every relationship. The system
builds dependency edges from fields, expressions, propositions, events, and
annotations. It may lint free text for suspicious unlinked mentions.

## Why transactions help—and what they do not do

A transaction makes a group of edits atomic. It guarantees that canon advances
from one valid snapshot to another or does not advance at all. It also gives the
validator the whole intended change at once.

It does not require rewriting every dependent record. For example, changing
"X does not know Y exists" to "X knows Y exists" may only require a knowledge
acquisition record. Existing scenes that remain valid need no edit. The impact
graph finds dependent scenes and constraints, and validators identify which of
them actually need repair. Prose-only dependents can be found only when they are
linked, annotated, or detected heuristically.

## Smallest useful product

The first product should be an **annotated story-outline continuity checker**,
not an autonomous novel writer or universal RPG simulator.

It should handle one small mystery world with:

- Stable entities and typed propositions.
- Positive and negative canon assertions with time ranges.
- Character knowledge, belief, and suspicion with provenance.
- Ordered events.
- A finite narrative graph plus one selected linear trace.
- Scene preconditions, effects, disclosures, and canon references.
- A mystery solution, clue acquisition points, and explicit deduction rules.
- Transactional JSON changes, deterministic validation, and impact analysis.
- A machine-readable report and a human continuity packet.

That slice directly tests the difficult parts of the idea. If it works, games
and campaigns can add richer branching and exporters. If it does not, a large
plugin, visual editor, prose generator, or game integration would not rescue the
core thesis.

## POC falsification plan

The proof of concept should be evaluated, not merely demonstrated. Build an
intentional-error corpus and run a small sequence of realistic authoring tasks.
Record:

- Expected deterministic findings and missed findings.
- False-positive diagnostics.
- Time and annotation effort required per change.
- Number of impacted records presented to the agent.
- Whether an agent can repair each failure using only structured diagnostics.
- Whether repeated runs produce byte-equivalent deterministic reports.
- Whether a deliberately unmodeled prose contradiction is honestly reported as
  outside coverage rather than silently treated as valid.

Proceed beyond the POC only if it catches nontrivial continuity failures with an
acceptable authoring burden. Scale the product down to a structured outline and
continuity-reference generator if full narrative-state modeling proves too
costly. Archive the project rather than broaden its claims if structured
annotations provide no meaningful advantage over ordinary documents plus AI
review.

## Product language

Prefer:

> ValidatedWorld validates declared continuity constraints and reports the
> coverage and limits of each result.

Avoid:

> ValidatedWorld guarantees that an entire novel or game is consistent.

That distinction is the difference between an achievable open-source tool and a
pipe dream.

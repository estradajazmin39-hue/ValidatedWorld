# ValidatedWorld

ValidatedWorld is an experimental **consistency compiler for large, connected
documents and designed worlds**.

A long document looks sequential on the page, but its meaning is a graph:
definitions are used by later sections, conclusions depend on assumptions and
evidence, requirements drive design decisions and tests, fictional events affect
characters and clues, and one changed statement can invalidate material far away
from it.

ValidatedWorld makes the important parts of that hidden graph explicit. It gives
content units and claims stable identities, records typed relationships between
them, computes the impact of a proposed change, and requires affected material
to be updated or deliberately reviewed before the change becomes canonical.
Optional AI review can propose missing claims and connections or flag likely
semantic conflicts; deterministic validation reports exactly what was proven and
what remains heuristic.

## Intended uses

- Technical designs: trace requirements, assumptions, decisions,
  implementations, and verification plans.
- Whitepapers and research-oriented documents: trace definitions, claims,
  evidence, derivations, citations, and conclusions.
- Novels and mysteries: track facts, chronology, character knowledge, clues, and
  disclosure.
- Games and campaigns: describe a static transition model whose possible runtime
  states are derived from variables, conditions, effects, and player choices.

Despite the name, a “world” is any versioned universe of connected claims and
artifacts. Fiction is one profile, not the foundation of every project.

ValidatedWorld cannot prove arbitrary prose correct or a paper scientifically
sound. Its durable promise is narrower: preserve explicit constraints, expose
change impact, force review where certainty ends, and assemble focused context
for an authoring agent.

## Start here

- [Feasibility and limits](docs/feasibility.md) — the guarantee boundary and the
  smallest useful product.
- [Product and architecture specification](docs/validated_world_authoring_spec.md)
  — the common document/claim graph and specialized profiles.
- [Implementation blueprint](docs/implementation_blueprint.md) — domain types,
  algorithms, tests, and sequenced work packages for coding agents.

## Current status

The repository is a .NET 10 scaffold plus an implementation-ready design. The
first proof of concept is a small technical design document: changing an
assumption must identify every dependent claim and section and create review
obligations. A mystery-world profile follows only after that simpler core proves
useful.

## Core workflow

```text
inspect project and dependency graph
→ begin transaction
→ change content, claims, or links
→ compute transitive impact
→ validate explicit rules
→ review every affected unit
→ resolve or acknowledge heuristic concerns
→ commit atomically
→ export documents and focused context packets
```

Build and test with:

```powershell
dotnet build ValidatedWorld.slnx
dotnet test ValidatedWorld.slnx
```

# ValidatedWorld

ValidatedWorld is an experimental continuity compiler for long-form fiction and
narrative worlds.

It gives important story information stable identities and explicit semantics—
truth, time, character knowledge, events, clues, and narrative conditions—then
validates proposed changes in an isolated transaction before they become canon.
The intended primary user is an AI authoring agent, with a deterministic .NET
core that remains useful to human tools and independent of any model provider,
game engine, or publishing medium.

The project is deliberately honest about its limits: it can validate declared
continuity, finite narrative models, and explicit constraints. It cannot prove
that arbitrary unannotated prose is consistent or that a story is good.

## Start here

- [Feasibility and limits](docs/feasibility.md) — what can actually be
  guaranteed and the smallest useful product.
- [Product and architecture specification](docs/validated_world_authoring_spec.md)
  — authoritative product behavior and boundaries.
- [Implementation blueprint](docs/implementation_blueprint.md) — domain types,
  JSON contract, algorithms, diagnostics, tests, and sequenced work packages for
  coding agents.

## Current status

The repository is a .NET 10 scaffold plus an implementation-ready design. The
production projects and tests are still placeholders. The next implementation
step is work package 0 in the blueprint; a plugin, GUI, prose generator, and AI
provider integration are intentionally deferred until the continuity model has
passed its proof-of-concept evaluation.

## Planned workflow

```text
inspect canon
→ begin transaction
→ apply semantic changes
→ compute impact
→ validate and analyze
→ repair or reject
→ commit atomically
→ export derived material
```

Build and test the scaffold with:

```powershell
dotnet build ValidatedWorld.slnx
dotnet test ValidatedWorld.slnx
```

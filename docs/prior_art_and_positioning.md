# Related Systems and Product Position

**Status:** Architectural research record

**Last reviewed:** 2026-08-13

ValidatedWorld overlaps requirements traceability, graph validation, knowledge
graphs, and AI retrieval. It should continue only while **affected-subgraph
expansion plus mandatory semantic review before an atomic current-state update**
proves meaningfully useful.

## Requirements and traceability

[Doorstop](https://doorstop.readthedocs.io/en/stable/) stores identified
requirements items, validates links/cycles, fingerprints linked parents, and
marks links suspect after change. It is the closest baseline for technical work.

ValidatedWorld's hypothesis is broader but must remain simple:

- one human-readable node/edge graph rather than requirement documents;
- explicit per-edge review direction;
- transitive affected paths across current and proposed connectivity;
- complete change-session dispositions before atomic SQLite write;
- one purpose-rooted scope tree preventing uncontrolled ancestor fan-out; and
- optional human/AI semantic use across technical, fiction, lore, or other
  projects without requiring a domain ontology.

Gate A must compare burden and usefulness with Doorstop. “Doorstop in SQLite” is
not enough justification.

[ReqIF](https://www.omg.org/reqif/) is an established requirements interchange
standard. [Eclipse Capra](https://projects.eclipse.org/projects/modeling.capra/governance)
creates/visualizes/analyzes arbitrary trace links. ValidatedWorld does not need a
competing interchange format or graphical trace-link UI in its initial scope.

## Graph schemas and databases

[SHACL](https://www.w3.org/TR/shacl/) validates RDF data graphs against shapes
and returns structured reports. [TypeDB](https://typedb.com/docs/use-cases/graph/)
and property-graph databases such as
[Neo4j](https://neo4j.com/docs/cypher-manual/current/schema/constraints/) provide
far richer schemas and traversal than ValidatedWorld plans.

Those systems reinforce the decision not to make a custom type language the
product. The initial common graph requires text nodes, labeled edges, endpoint
integrity, review direction, and scope. Optional application profiles may add
validators when evidence supports them. RDF canonicalization, ontologies,
SPARQL, graph servers, and user-authored schema languages would obscure the
affected-review experiment.

SQLite is adequate for indexed endpoint traversal, recursive queries, foreign
keys, and atomic local writes. A specialized graph store is justified only by
measured failure at expected scale.

## Versioned databases

[TerminusDB](https://terminusdb.org/docs/terminusdb-explanation/) and
[Dolt](https://www.dolthub.com/docs/sql-reference/version-control/) already offer
history, branches, diffs, merge, and time travel.

ValidatedWorld deliberately does not compete with them. It stores one current
graph, applies one reviewed in-memory proposal atomically, and discards
session-only operations/dispositions. Users who need history may version/backup
the `.vw.db` externally or use a versioned database product. The private
current-state fingerprint is integrity/stale protection, not database version
control.

## RAG and GraphRAG

The original [RAG architecture](https://arxiv.org/abs/2005.11401) retrieves
external passages as model memory.
[Microsoft GraphRAG](https://microsoft.github.io/graphrag/index/overview/) uses
models to extract graph information and build retrieval indexes/summaries.

| RAG/GraphRAG | ValidatedWorld |
|---|---|
| Retrieves likely relevant context | Stores deliberately accepted current state |
| Often derives graph data heuristically | Requires explicit nodes/edges and reviewed operations |
| Optimizes generation/answers | Optimizes affected-change review |
| Relevance may be probabilistic | Traversal follows declared review direction |
| Re-indexes when sources change | Rejects an incomplete reviewed change session |

ValidatedWorld search/context can feed an AI, but no embedding index or RAG
pipeline is part of the engine. Optional AI may propose links; only explicit
reviewed operations enter SQLite.

## AI agents and deferred integrations

The initial application owns one stable text-oriented Application tool contract
for search, navigation, in-memory proposals, affected analysis, review,
validation, and guarded commit. Built-in optional AI authoring uses that contract.

The distinctive hypothesis is not generic tool calling. It is that an AI can
work on a graph far larger than its context window through bounded deterministic
search, while every modeled consequence is selected and reviewed before an exact
atomic update.

OpenAI's [plugin architecture](https://developers.openai.com/plugins/concepts/plugins)
and [MCP guidance](https://developers.openai.com/plugins/build/mcp-server) remain
relevant research if external packaging is reconsidered. They are not in the
current roadmap. The same is true of custom graphical interfaces and multi-agent
coordination: record the idea, do not build it until the single built-in agent and
manual workflow supply evidence.

## Persistence and interoperability

[SQLite application files](https://www.sqlite.org/appfileformat.html) provide
one-file portability, transactions, indexes, and common read tooling.
ValidatedWorld uses four current-state tables rather than recreating a property-
graph or event-sourced database:

- migration history;
- one project row;
- nodes; and
- edges.

JSON columns inside node/edge rows encode tags/scalar attributes, but they are
not a supported project snapshot. The `.vw.db` file/application backup is the
complete project. Documented read-only views and an optional application-produced
SQL export support integrations. Direct canonical SQL writes are unsupported.

SQLite is embedded and bundled through NuGet. No separate install, server,
administration tool, or Docker workflow is required.

## Product survival test

Continue only if Gate A demonstrates:

1. Precise, explained affected sets beyond ordinary link inspection.
2. A useful complete-review-before-write workflow beyond suspect flags alone.
3. Acceptable edge-authoring and review burden for humans and lower-cost agents.
4. A plain text graph useful without a domain profile.
5. Reliable atomic current-state replacement and understandable failure recovery.
6. Deterministic bounded search/navigation suitable for projects larger than an
   agent's working context.
7. Repeatable black-box completion from public documentation without SQL or
   source knowledge.
8. Optional AI authoring/review, if later attempted, materially improves the
   workflow without becoming required or bypassing approval.

If the result is only a database schema, use SQLite directly. If it is only a
requirements suspect-link tool, use/extend an existing tool. If it is only
retrieval, use RAG/knowledge-graph software. WP9 records the evidence and stops
for human direction.

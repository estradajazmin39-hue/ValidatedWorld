# Related Systems and Product Position

**Status:** Architectural research record

**Last reviewed:** 2026-08-12

## Purpose

ValidatedWorld overlaps several mature categories. This document prevents the
project from accidentally rebuilding a database, requirements tool, RDF
validator, or RAG pipeline while believing those capabilities are novel.

The project should continue only while its distinctive behavior—transactional
semantic impact plus mandatory explained review across multiple authored-work
profiles—proves useful.

## Requirements and traceability tools

### Doorstop

[Doorstop](https://doorstop.readthedocs.io/en/stable/) stores individually
identified requirements-oriented items in version control, validates links and
cycles, fingerprints linked parents, marks changed links suspect, and lets users
review or clear them.

This is close to ValidatedWorld's technical-project use case. ValidatedWorld is
not justified as merely “Doorstop with JSON/SQLite.” Its distinct hypothesis is:

- one general typed graph rather than a document hierarchy;
- explicit relationship-specific impact direction;
- transitive base-plus-projected impact paths;
- commit-blocking dispositions over the whole proposed transaction;
- evidence classes including inconclusive analysis;
- non-requirements profiles for claims, narrative perspective, and bounded
  interactive state.

Gate A must compare its workflow and modeling cost with Doorstop. If the common
technical case is not materially better, narrow the product or reuse/integrate
instead of competing by accident.

### ReqIF and engineering traceability

[ReqIF](https://www.omg.org/reqif/) is an established non-proprietary XML
interchange standard for requirements-management tools. It is not a validation
engine but is relevant to any later technical-data adapter.

[Eclipse Capra](https://projects.eclipse.org/projects/modeling.capra/governance)
focuses on creating, visualizing, and analyzing trace links between arbitrary
engineering artifacts. That substantially overlaps artifact anchors and generic
traceability, but not ValidatedWorld's transaction policy or cross-domain
semantic profiles.

Do not invent a competing interchange format or broad artifact-wrapper framework
in Gate A.

## Graph schemas and validation

### RDF and SHACL

[SHACL](https://www.w3.org/TR/shacl/) is a W3C Recommendation for validating RDF
data graphs against shapes graphs and returning structured validation results.
It already covers many node/property cardinality and shape constraints.

ValidatedWorld should borrow its separation of data, schema, constraint, and
report, but not adopt RDF/SHACL in Gate A. RDF canonicalization, ontology choices,
SPARQL extensions, and uneven recursive semantics would add complexity without
providing transaction impact or review obligations. A future interoperability
mapping may be useful.

[PROV-O](https://www.w3.org/TR/prov-o/) is a W3C provenance ontology. Consult it
before finalizing long-term provenance vocabulary, but do not make the common
engine depend on OWL/RDF.

### TypeDB and property-graph databases

[TypeDB](https://typedb.com/docs/use-cases/graph/) provides strongly typed
entities and first-class relations with named roles and write-time schema
enforcement. Property-graph databases such as
[Neo4j](https://neo4j.com/docs/cypher-manual/current/schema/constraints/) provide
native traversal plus property and identity constraints.

These systems demonstrate that typed graph storage is established technology.
They do not remove the need for ValidatedWorld's version-specific impact,
review-disposition, external-anchor, and profile rules. Requiring a separate
database server and query ecosystem is not justified for the local POC; an
indexed SQLite adjacency representation is sufficient until measured otherwise.

## Versioned databases

[TerminusDB](https://terminusdb.org/docs/terminusdb-explanation/) is an open-source
schema-enforced document/graph database with immutable history, branch, diff,
merge, and time-travel behavior.

[Dolt](https://www.dolthub.com/docs/sql-reference/version-control/) provides
Git-like branches, commits, diffs, merges, and history over relational tables.

These products cover data versioning far beyond Gate A. ValidatedWorld currently
needs one linear optimistic head, accepted operations, and audit evidence—not
branch/merge infrastructure. Do not rebuild general database version control.
Re-evaluate one of these stores only if collaboration/branching becomes a proven
requirement.

TerminusDB also documents a closed-world default, whereas ValidatedWorld's common
semantics are open-world. That mismatch is material for claims and narrative
knowledge.

## RAG and GraphRAG

The original
[retrieval-augmented generation](https://arxiv.org/abs/2005.11401) architecture
retrieves external passages as non-parametric memory for a generator.

[Microsoft GraphRAG](https://microsoft.github.io/graphrag/index/overview/) uses
LLMs to extract entities, relationships, and claims from unstructured text,
creates graph communities and summaries, and builds embeddings for retrieval.
Its query engine uses that derived index to construct context and answers.

ValidatedWorld is not RAG:

| RAG/GraphRAG | ValidatedWorld |
|---|---|
| Retrieves likely relevant context | Maintains authoritative explicit state |
| Often derives a lossy/heuristic index from text | Accepts records only through validated transactions |
| Optimizes answer generation | Optimizes consistency and change review |
| Similarity/relevance may be probabilistic | Dependency paths have declared deterministic meaning |
| Re-indexes when sources change | Rejects incomplete or stale canonical commits |

A ValidatedWorld context query can feed a RAG or authoring agent. That is an
integration relationship, not product duplication.

## Persistence decision

[SQLite](https://www.sqlite.org/appfileformat.html) is specifically suitable as
a single-file application format with atomic transactions, incremental updates,
indexes, portability, and common tooling. It supports foreign keys and recursive
queries over trees and graphs.

SQLite therefore owns physical integrity and persistence for Gate A. JSON remains
the versioned protocol and deterministic logical snapshot. The C# engine owns
semantic meaning and commit policy.

## Product survival test

Continue only if Gate A demonstrates all of the following:

1. More precise, explained transitive impact than ordinary foreign-key or link
   inspection.
2. A useful commit-blocking review workflow beyond Doorstop-style suspect links.
3. Acceptable modeling effort for humans and lower-cost agents.
4. A stable small metamodel that supports materially different profiles without
   arbitrary physical schemas.
5. Deterministic behavior that RAG/LLM extraction cannot provide.

If it only becomes a database schema, use an existing database directly. If it
only becomes a requirements-link tool, use or extend an existing requirements
tool. If it only becomes context retrieval, use a RAG/knowledge-graph system.

WP9 records the reproducible comparison evidence and resulting Gate A outcome in
[implementation_execution_plan.md](implementation_execution_plan.md). Narrative,
interactive-state, and integration work is outside the current roadmap. It
requires a conclusive outcome supported by the criteria above and a new
human-requested planning task. The evaluating agent reports the choices and
stops; it does not infer permission to continue.

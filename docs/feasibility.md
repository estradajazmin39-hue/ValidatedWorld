# Feasibility, Limits, and the Smallest Useful Product

**Status:** Accepted product boundary

**Last reviewed:** 2026-08-13

## Verdict

ValidatedWorld is feasible as a **reviewed change-control layer over one simple
dependency graph**. It is not feasible as a machine that deterministically
understands whether arbitrary prose is globally true or coherent.

One embedded SQLite application file contains the current project graph. Nodes
primarily contain human-readable text. Edges explicitly state relationships and
which direction a change should propagate for review. The application can
deterministically find the affected subgraph, enforce structural integrity,
ensure every required item was examined, and atomically replace the current
state. A human or AI supplies the semantic judgment about whether the affected
text still makes sense.

This is a smaller and more honest product than the earlier typed-schema and
versioned-ledger design. Optional profiles may later add structured attributes,
controlled vocabulary, or deterministic rules, but an ordinary project must not
need one.

## The guarantee boundary

ValidatedWorld can guarantee that:

- the SQLite file has the expected application schema and passes integrity
  checks;
- every node and edge has a stable ID;
- every edge endpoint exists;
- exactly one purpose root exists;
- every other node has one acyclic `scope-parent` path to that root;
- a proposed operation batch produces a structurally valid projected graph;
- the affected set is complete for every explicitly modeled review direction in
  both the current and projected graph;
- every required affected node received a current review disposition;
- the user approved the exact final in-memory change set when AI authoring is
  used;
- a commit applies all current-state changes in one SQLite transaction or applies
  none; and
- a stale or externally changed base is detected before write.

It cannot guarantee that:

- every meaningful fact or dependency was modeled;
- a relationship label or node's prose is correct;
- a human or AI reviewer understood the supplied text;
- an AI catches every contradiction or avoids every false positive;
- a scientific, engineering, legal, patent, or literary claim is sound;
- an external manuscript, design, game, or source tree matches the graph; or
- an unbounded interactive game state is safe or reachable.

Accordingly, “validated” means **structurally valid and completely reviewed under
the graph's explicit relationships and enabled optional profiles**. It does not
mean that arbitrary natural language has been proven correct.

## The common graph

Every project has:

- **Nodes:** stable IDs, required human-readable text, optional free-form kind,
  tags, and scalar attributes.
- **Edges:** stable IDs, source and target node IDs, a human-readable relationship
  label, a declared review direction, and optional scalar attributes.
- **One purpose root:** a substantive statement describing the universe the
  project represents.
- **One scope tree:** every non-root node has exactly one `scope-parent`. This
  organizes the graph and provides a singular upward context path.
- **Semantic cross-links:** all other edges may cross branches and form a
  multigraph or cycles.
- **Optional profiles:** application modules that recognize selected kinds,
  attributes, and relationship labels and add deterministic checks or authoring
  helpers. Profiles are not required to store a plain graph.

Review direction is minimal machine-readable metadata, not a domain ontology:

```text
none              a change at either endpoint does not propagate through edge
source-to-target  changing source selects target
target-to-source  changing target selects source
both              changing either endpoint selects the other
```

This declaration tells the application where review must travel. It does not
tell the application what the relationship means in natural language.

`scope-parent` is treated specially. A changed node's singular ancestor lineage
is shown as context without turning those ancestors into new propagation seeds.
Changing a scope node directly selects its descendant subtree; changing the
purpose root directly therefore selects the whole project. This avoids a leaf
change fanning through the root into unrelated sibling branches.

## Why not arbitrary tables or a graph database

Foreign keys can prove that references exist, but they cannot say whether a
change should make another record suspect. Letting each AI invent tables would
make affected-set behavior heuristic and non-repeatable. ValidatedWorld therefore
owns a small fixed schema for projects, nodes, and edges.

A graph database is unnecessary for the initial scale. SQLite supplies indexed
endpoint queries, recursive traversal, foreign keys, atomic writes, backups, and
one-file portability. It runs in-process through pinned NuGet packages; users do
not install a SQLite server, command-line tool, Docker, or system provider.

The application remains responsible for traversal, review workflow, optional
profile rules, safe mutation, and diagnostics. Direct SQL reads through
documented views may be supported. Direct writes to canonical tables are not.

## Current state, not version history

The database stores the current graph only. Gate A does not retain application
drafts, commits, parent hashes, replay operations, branches, merges, or time
travel. External source-control or backup systems may version the `.vw.db` file
if a user wants history.

An active change session lives in application memory and is expected to be
committed or discarded before shutdown. It records a base-state fingerprint,
operations, projected state, affected paths, and review dispositions. The
fingerprint is recalculated from deterministic current rows and is used only to
detect that the database changed between opening the session and commit. It is
not a retained revision chain or public interchange format.

The final write transaction is short:

1. Re-read and verify the current database fingerprint.
2. Rebuild the projection and affected/review evidence.
3. Reject stale, incomplete, invalid, or unapproved sessions.
4. Begin a short SQLite write transaction.
5. Apply every explicit node/edge operation.
6. Verify foreign keys, tree structure, optional profile rules, and the resulting
   current-state fingerprint.
7. Update the one project row and commit once.

Any failure rolls back the complete attempt. No human or model interaction occurs
while the SQLite write transaction is open.

## Affected-set behavior

The changed entity IDs are the initial seeds. The engine traverses the union of
relevant edges from the current and projected graph so removing or redirecting a
relationship cannot hide its former consequences. Breadth-first traversal
produces deterministic shortest explanation paths.

Only the declared directions propagate review. Upstream dependencies may also be
shown as explanatory context without becoming propagation seeds unless their
edge direction says they are affected. If the user edits a contextual node, it
becomes a direct seed and the set is recalculated. This is how a review expands
incrementally without turning every connected component into every change's
working set.

Each selected node ends in one of these session-local dispositions:

- `updated` — directly changed in the current operation batch;
- `reviewed-no-change` — examined and still correct;
- `not-applicable` — the displayed path does not require a semantic response; or
- `pending` — commit is blocked.

Changing the operation batch invalidates any disposition whose node, projected
content, or explanation path changed.

## Optional AI roles

AI is the intended convenience layer, not a runtime requirement.

The **authoring agent** accepts a user's natural-language intent, repeatedly
searches and reads bounded graph context, creates explicit in-memory operations,
runs affected-set analysis, asks material questions, and eventually calls a
guarded commit tool after exact user approval.

The **semantic reviewer** is a separate optional model request over the complete
proposed transaction and selected context. It returns cited concerns but never
edits the graph or turns its opinion into deterministic proof.

Both roles can be disabled. If the OpenAI key is absent, the application reports
them unavailable and uses the manual workflow without treating that as a graph
error. The human then authors changes through text-based commands and reviews the
complete affected set personally.

The project may be larger than any model context. Search, scope traversal,
dependency queries, and in-memory transactions let the authoring model work on
bounded relevant sets. This does not require the entire graph in one prompt.
When an affected set itself exceeds the configured model request bound, AI review
is inconclusive and the user must narrow, redesign, or review manually. Initial
scope does not include a multi-agent coordinator.

Only explicit user-supplied text is planned as AI intake. General images, OCR,
documents, web crawling, MCP/plugin packaging, and a graphical UI are outside the
current roadmap.

## Storage and interoperability

`project.vw.db` is the sole complete project format. The application owns:

- database initialization, migrations, integrity verification, and backup;
- optional SQL export through an application-controlled command;
- documented stable read-only views for projects, nodes, edges, direct review
  arcs, and current diagnostics; and
- structured command requests/results, which may use JSON without creating a
  second project representation.

External integrations read the database, its application-produced backup, its
documented views, or an application-produced SQL export. They must not mutate
canonical rows directly.

## Relationship to external products and RAG

The graph may contain artifact/anchor nodes pointing at chapters, sections,
components, tests, scenes, or files. An affected anchor is guidance to inspect
that external product; ValidatedWorld does not rewrite or certify its bytes.

RAG retrieves likely relevant passages. GraphRAG often derives a heuristic graph
from text. ValidatedWorld stores deliberately accepted graph state and computes
review scope from explicit edges. Its bounded queries may feed an AI or RAG
system, but it is not an embedding index or retrieval pipeline.

## Smallest useful product and proof

Gate A is a local, text-oriented SQLite graph editor with:

- one current-state `.vw.db`;
- one simple node/edge model and purpose-rooted scope tree;
- in-memory change sessions;
- deterministic structural validation;
- base-plus-projected affected-set traversal and explanation paths;
- complete manual review obligations;
- one atomic commit or complete rollback;
- deterministic search and graph navigation;
- backup and documented read-only interoperability; and
- realistic automated and agent-operated usability tests.

The first proof uses plain text nodes for an offline sensor technical-design
project. A change to a power assumption must select its explicit consequences
and exclude unrelated privacy/accessibility branches. A separate soft-logic
change must surface the relevant requirement, claim, decision, verification, and
external anchors. The proof must measure how much edge authoring and review work
is required.

Gate A fails if the graph must be nearly fully connected to be useful, affected
sets are routinely noisy or incomplete, manual review is worse than ordinary
link inspection, or the added engine offers no meaningful advantage over SQLite
plus existing requirements tools.

## Evidence gates

### Gate A — manual deterministic core

Prove storage, structural rules, affected-set accuracy, rollback, search,
interoperability, modeling cost, and end-to-end manual usability without a
provider, secret, network, profile, or GUI.

### Gate B — optional semantic reviewer

Evaluate one expensive OpenAI request over a whole proposed transaction and its
selected context. Measure useful concerns, false positives, omissions, privacy,
cost, and latency. Omit the built-in reviewer if it does not materially improve
manual review. Missing configuration always falls back to manual review.

### Gate C — optional AI authoring

Evaluate natural-language text creation and alteration through the same bounded
application tools. It must reduce user burden without direct SQL, unrelated
changes, self-approved review, or unconfirmed commit. Omit it if it is not
reliable or useful; Gate A remains a complete product.

Profiles for technical, narrative, catalog, or interactive domains are optional
later experiments, not prerequisites or automatically ordered gates. MCP/plugin
packaging, graphical UI, hosted services, document generation, image ingestion,
and multi-agent coordination are outside the current roadmap.

## Product language

Prefer:

> ValidatedWorld stores a current human-readable dependency graph, explains
> which modeled nodes must be reconsidered for a proposed change, and commits the
> completely reviewed batch atomically. Optional AI can author and independently
> review the same workflow.

Avoid:

> ValidatedWorld proves a world or document correct, versions a database, or uses
> an AI to understand every dependency automatically.

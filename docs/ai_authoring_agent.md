# Planned Optional AI Authoring Agent

**Status:** Authoritative Gate C direction; not current Gate A work

**Last reviewed:** 2026-08-13

## 1. Intended experience

AI authoring is the strongly intended way to use mature ValidatedWorld, but it is
optional. With it enabled, a user can say:

- “Create a graph from these notes about the system I am designing.”
- “Add a pasta section to this restaurant model.”
- “The murderer now knows the detective found the ledger; update the modeled
  consequences.”
- “Revise this architecture so raw observations never leave the device.”

The built-in authoring agent performs the graph mechanics. It searches, chooses
stable IDs, creates text nodes and labeled/directed edges, operates one in-memory
change session, analyzes affected nodes, asks material questions, repairs the
proposal, and calls guarded commit after the user approves the exact preview.

The user is not expected to write JSON, SQL, IDs, edge batches, or command
sequences. The same tools nevertheless remain available to a human when AI is
disabled or unconfigured.

## 2. Central context-window purpose

A project may be far larger than any model context window. The SQLite file is
persistent project memory. Deterministic bounded search, scope traversal,
relationship navigation, explanation paths, and affected-set queries let the
agent repeatedly retrieve the smallest useful working set.

The authoring model never needs the entire world in one prompt. Structural
validation and graph traversal operate over the authoritative graph outside the
model. Semantic completeness remains limited to explicit nodes/edges and the
human/AI's judgment.

Every changed or affected node is an exception to purely minimal retrieval in
one deliberate respect: its complete `scope-parent` path through the project
thesis is always mandatory context. The agent must inspect the proposal against
every containing scope and the root. Those ancestors do not become propagation
seeds and do not pull sibling branches into the working set.

If one proposal's affected set itself is too large for the configured author or
review request, the app reports the bound and preserves canonical state. The
user may narrow/redesign the proposal or handle it manually. Coordinating
multiple agents over partitions is a possible future experiment, not Gate C.

## 3. Manual fallback

The feature has an explicit enable flag and requires a configured key. If
disabled or `OPENAI_API_KEY` is absent, the application reports authoring AI
unavailable and exposes the complete text-oriented manual workflow. Project
creation, search, in-memory proposals, affected analysis, manual review,
validation, and commit must all remain usable.

AI absence is not an invalid project or missing dependency. No graph stores a
policy requiring the provider.

## 4. Headless application tool contract

The authoritative interface is a versioned, UI-independent Application
contract. Built-in function tools and manual CLI/host commands are adapters over
the same use cases.

Read tools:

```text
get_project_status
get_project_purpose
search_nodes
get_node
get_edge
list_scope_children
get_scope_ancestors
get_scope_subtree
get_neighbors
get_dependencies
get_dependents
explain_path
build_context
get_change
get_affected_set
get_validation_report
```

Change tools:

```text
prepare_project_initialization
initialize_project
begin_change
set_change_focus
expand_authoring_batch
apply_operations
analyze_affected_set
set_review_disposition
validate_change
discard_change
prepare_commit_confirmation
commit_change
```

Tools use strict schemas, stable codes, deterministic ordering, bounds, and
explicit stale/inconclusive results. They expose no raw SQL, direct canonical
write, profile-code modification, rule suppression, automatic disposition, or
unguarded commit.

No MCP/plugin or graphical adapter is in the current roadmap. Those may be
reconsidered only after the built-in agent and manual tools prove useful.

## 5. Search before mutation

The agent searches before creating anything likely to duplicate existing state.
Search supports exact ID, text, optional kind, tag, relationship, scope subtree,
neighbors, propagation dependencies/dependents, and paths. Context queries report
omissions and truncation.

There are no embeddings, RAG index, natural-language SQL parser, or provider call
inside deterministic search. The model translates user language into explicit
filters and may broaden/narrow them deliberately.

## 6. Existing-project loop

1. Open/verify the database and read purpose/current-state fingerprint.
2. Restate the requested outcome and material assumptions.
3. Search and retrieve the smallest sufficient scope/relationship context.
4. Begin one process-local in-memory change session.
5. Ask the user when materially different interpretations would produce
   different graph meaning. Routine ID/format choices remain autonomous.
6. Apply bounded explicit node/edge operation batches.
7. Project and run structural validation after each coherent batch.
8. Analyze the complete current-plus-proposed affected set and retrieve every
   changed/affected node's full upstream lineage through the purpose root.
9. Inspect, repair, or ask about every non-trivial consequence. Routine
   `reviewed-no-change` may be proposed but cannot impersonate the user's
   opinion where a material semantic choice exists.
10. If optional AI review is enabled/configured and the user separately
    authorizes it, invoke the independent Gate B request.
11. Repair/discuss concerns; each proposal change invalidates prior review.
12. Present exact operations, affected set/paths, mandatory upstream context,
    review status, and base/operation/proposed/affected/context fingerprints.
13. Ask for one explicit conversational confirmation.
14. Call guarded `commit_change` and report success or structured rollback.

The agent itself performs all graph work and the final guarded tool call. The
guard is not a separate UI/manual step; it merely prevents unapproved or stale
content from being committed.

## 7. New project from description or text

The initial Gate C accepts a description and optional explicit UTF-8 text. It
does not promise image/OCR/PDF/Office parsing, web crawling, filesystem
discovery, or arbitrary document ingestion.

Before database creation the agent proposes:

- project ID/title;
- substantive purpose text;
- optional installed profile selection (normally none initially);
- top-level scopes;
- source names/hashes and privacy warning;
- unresolved questions and extraction-coverage statement; and
- a bounded first batch plan.

The user confirms the exact initialization preview. Guarded
`initialize_project` creates the four-table database and purpose root atomically.
The agent then opens one in-memory change session and authors the remaining
graph.

Source material is heuristic input. The agent reports uncertain interpretation,
possible duplicates, inferred grouping, and unmodeled content. Raw source text is
not stored automatically in `.vw.db`; only explicitly accepted graph nodes,
edges, and selected source-anchor text become canonical.

Profiles are optional. The agent may select an installed compatible profile when
the user wants one, but unsupported vocabulary is valid as ordinary text/kind/
relationship data. It must not silently invent executable profile semantics.

## 8. Affected review and semantic judgment

The deterministic engine selects affected nodes from explicit directions. The
authoring agent reads those nodes and their relationship paths and decides
whether to propose updates or ask the user. This semantic pass is part of
authoring, not deterministic proof.

The app should avoid bombarding the user with routine mechanics, but the agent
must ask when a consequence requires preference, policy, disputed meaning,
significant deletion, or another non-trivial judgment. It must not clear every
node mechanically just to enable commit.

The independent Gate B reviewer, when used, receives no authoring conversation
or hidden reasoning and cannot operate tools. The author cannot approve its own
review output.

## 9. Guarded initialization and commit

`prepare_commit_confirmation` presents the exact current proposal and affected
review state. After an explicit user approval in ordinary conversation, the host
issues a short-lived opaque authorization bound to:

- database/project identity;
- base-state fingerprint;
- operation-set fingerprint;
- proposed-state fingerprint;
- affected-set fingerprint;
- mandatory scope-context coverage fingerprint;
- current disposition/validation/reviewer state; and
- expiry and conversation/session identity.

The agent passes that authorization to `commit_change`. A missing, mismatched,
expired, or stale authorization is deterministically rejected. No click, SQL
command, or separate human-run commit is required.

New-project initialization uses the same exact-preview/user-approval/guarded-tool
pattern.

After successful commit, only the resulting current graph/fingerprint remains in
SQLite. Operations, conversations, tool receipts, semantic concerns, and review
dispositions are not project history. The host may keep bounded operational logs
under its normal privacy policy, but never inside canonical project state.

## 10. Configuration, provider, and limits

```text
VW_AIAUTHORING__ENABLED=false
VW_AIAUTHORING__PROVIDER=openai
VW_AIAUTHORING__MODEL=gpt-5.6-terra
VW_AIAUTHORING__TIMEOUTSECONDS=1200
VW_AIAUTHORING__LIVETESTS=false
```

The sole initial production provider is OpenAI Responses with strict function
tools and the reviewed model at implementation time. The current plan is
`gpt-5.6-terra` with medium reasoning. Re-check support before Gate C.

Each response uses background mode and a 1,200-second end-to-end deadline.
Polling one response and returning requested tool results are continuations, not
paid retries. There are zero automatic paid retries or fallback models.

Implementation-owned limits bound provider responses, tool calls, operations per
batch/session, context bytes, and consecutive repair failures. Hitting a limit
keeps canonical SQLite unchanged, reports remaining work, and asks whether to
continue manually or start another explicitly authorized model response.

The host reports provider/model, response IDs, tool-call counts, usage, elapsed
time, and available cost data without credentials, hidden reasoning, or private
source text in ordinary logs. It never makes unannounced model calls.

## 11. Secrets and implementation stop rule

Use the same human-owned key boundary as Gate B. Local development uses .NET
Secret Manager; published use may use `OPENAI_API_KEY`. Never store a key in
SQLite, command arguments/results, logs, fixtures, or source.

Before a coding agent may begin Gate C provider/tool-loop implementation, the
initiating human prompt must contain:

```text
AI_AUTHORING_SECRET_READY: yes
```

A live evaluation additionally requires:

```text
AI_AUTHORING_LIVE_CALL_AUTHORIZED: yes
```

The agent never searches for, obtains, infers, lists, copies, or sets a key.
Normal implementation and tests use fakes.

## 12. Evidence gate

Offline tests and one separately authorized live evaluation must prove/evaluate:

- identical semantics between manual host commands and in-app model tools;
- plain profile-free project creation from description/text;
- reliable search/navigation over a graph larger than the working context;
- duplicate/unrelated operation avoidance;
- one in-memory session with no persistence/recovery claim;
- meaningful user questions and honest bounds/stops;
- complete affected-set iteration, mandatory thesis/upstream context coverage,
  and correct manual fallback;
- Gate B independence and review staleness;
- exact conversational approval and guarded model-called commit;
- no direct SQL/canonical write or automatic semantic disposition;
- disabled/missing-key operation with no provider call;
- no secrets/private source in SQLite/logs/results; and
- material reduction in user graph/review burden.

Omit the built-in authoring loop if it frequently creates plausible unrelated
state or does not reduce burden. The complete manual Gate A application remains
the product.

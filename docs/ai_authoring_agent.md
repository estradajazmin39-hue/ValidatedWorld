# Planned AI-First Authoring and Intake

**Status:** Authoritative post-Gate-A product direction; not part of the current
WP0-WP9 Gate A implementation roadmap. Gate B review may be integrated or
explicitly omitted based on its evidence gate.

**Last reviewed:** 2026-08-13

This is the primary intended human experience for mature ValidatedWorld:

> The user states an intent in ordinary language and may attach supported text or
> images. An AI authoring agent searches and reads the existing project, asks
> focused questions when meaning is ambiguous, and uses bounded ValidatedWorld
> tools to construct or repair a draft transaction. Deterministic validation,
> impact review, and any required independent AI semantic review must pass before
> the user explicitly authorizes the exact final draft and the agent completes
> the commit through a guarded tool.

The deterministic graph engine remains the foundation. The AI is an authoring
client of that engine, not a replacement for it and never a direct SQLite writer.

The phrase **AI-first** is normative: the agent performs the graph mechanics.
The user is not expected to choose IDs, types, properties, edge directions,
operation ordering, search commands, repair steps, or CLI syntax. For a request
such as "add a Pasta section," the agent may execute a long series of searches,
node/edge batches, validations, impact queries, and repairs autonomously. It asks
the user only for meaning, policy, spending, or final-approval decisions that
would materially change the result.

## 1. Why this is the product direction

Most users should not need to think in rows, JSON operations, IDs, or graph-edge
syntax. They should be able to say:

- "Create a project from this restaurant-menu image."
- "Add a pasta section with vegetarian and gluten-aware choices."
- "The murderer now knows the detective found the ledger; update every modeled
  consequence that must change."
- "Revise this architecture so raw observations never leave the device."

The agent translates that intent into explicit nodes, edges, and transaction
operations. ValidatedWorld supplies persistent state, search, exact dependency
paths, deterministic rules, stale-write protection, and atomic commit. This lets
an AI meaningfully and safely work on a project far larger than any one context
window. The project file is durable memory; deterministic search,
scope/dependency traversal, transactions, impact, and validation let the agent
repeatedly acquire the relevant working set and update the whole authoritative
state without loading all of it at once.

Deterministic validation covers the complete stored graph and every declared
rule regardless of model context size. The authoring agent itself performs the
searches, tool calls, draft changes, repairs, review handoff, and guarded commit.
The normal semantic boundary still applies: unmodeled facts or links cannot be
deterministically checked, so uncertainty is reported and, when Gate B is
retained, it supplies the additional heuristic pass.

## 2. Two independent AI roles

### 2.1 Authoring agent

The authoring agent is conversational and tool-using. It may:

- interpret a user's intent and supported text/image inputs;
- search, browse, and retrieve bounded project context;
- begin or resume one durable draft transaction;
- add, replace, or remove draft nodes and edges through Application tools;
- set focus and expand explicit batches/clusters;
- run projection, validation, impact, and obligation tools repeatedly;
- ask the user questions, propose alternatives, and repair the draft; and
- prepare an exact final preview for user confirmation.

It may not write canonical SQLite rows, issue SQL, alter schema packages, suppress
diagnostics, invent review dispositions on behalf of the user, or commit without
an explicit user confirmation bound to the exact draft revision and hashes. Once
that conversational confirmation exists, the agent is expected to call the
guarded commit tool and finish the workflow itself.

### 2.2 Gate B semantic reviewer

The reviewer is a fresh independent pass. It receives one immutable,
whole-transaction request, has no tools, and returns concerns only. It receives
neither the authoring conversation nor the author's hidden reasoning. The
authoring agent may respond to concerns by proposing more draft operations, but
every change stales the prior review and requires normal revalidation/review.

Using the same underlying model family is acceptable for the first evaluation,
but the roles, prompts, requests, and persisted evidence remain separate. The
authoring agent can never mark its own Gate B run successful or silently
disposition concerns.

## 3. Headless tool surface first

The authoritative interface is a small, versioned, UI-independent tool contract
over `ValidatedWorld.Application`. The CLI JSON commands, in-app OpenAI function
tools, and a later MCP server are adapters over the same use cases and schemas.
None may implement graph semantics independently.

Required read tools:

```text
list_profiles
get_project_head
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
explain_dependency_path
build_context
get_transaction
get_validation_report
get_impact
get_review_obligations
```

Required draft tools:

```text
prepare_project_initialization
initialize_project
begin_transaction
set_transaction_focus
expand_authoring_batch
apply_operations
validate_transaction
set_impact_disposition
abort_transaction
prepare_commit_confirmation
commit_transaction
```

`commit_transaction` is model-callable but guarded. The agent first calls
`prepare_commit_confirmation`, which presents the exact projected hash,
operation/impact summary, pending or satisfied review state, and AI usage/cost
summary in the conversation. The user may approve in ordinary language; no web
UI, separate click, SQL step, or manual application command is required. The host
then issues an opaque short-lived authorization bound to the unchanged draft
revision, change-set hash, projected hash, and project head. The agent passes it
to `commit_transaction` and reports the result. A call without matching user
authorization is deterministically rejected.

`initialize_project` follows the same conversational pattern for a new file. The
agent prepares title/ID/purpose/profile/source/initial-scope preview, the user
approves it in chat, and the guarded tool creates the database and purpose root.
The agent then immediately begins the draft and authors the remaining graph.

All tools use strict input/output schemas, stable diagnostic codes, deterministic
ordering, bounded results, pagination/cursors where needed, and explicit stale or
inconclusive results. Tool descriptions are treated as part of product behavior.

## 4. Search and navigation

AI-first authoring requires discovery before mutation. Gate A therefore delivers
deterministic search/navigation before this agent is implemented:

- exact ID lookup;
- filtering by node type, tag, lifecycle state, scope subtree, and selected
  scalar-property text;
- deterministic case-normalized text matching over display/search properties;
- bounded results with stable ordinal ordering and continuation cursor;
- scope children, upward ancestors, and bounded subtree reads;
- semantic neighbors, dependencies, dependents, and explanation paths; and
- context queries that report truncation and omitted IDs.

This is not embedding search, RAG, or a natural-language SQL layer. A future
full-text/materialized index is justified only by measured performance. The
authoring model translates user language into these explicit filters and can ask
for broader/narrower searches.

## 5. Existing-project alteration loop

For an existing project:

1. Verify the database and read the exact head/purpose/profile versions.
2. Start or resume an authoring session and one durable draft against that head.
3. Restate the requested outcome, declared assumptions, and any ambiguity.
4. Search before creating anything that might duplicate existing nodes.
5. Retrieve the smallest sufficient context, including scope lineage and
   dependency evidence.
6. Ask the user when materially different interpretations would produce
   different canonical graphs. Routine naming/ID choices remain autonomous.
7. Apply bounded batches of explicit draft operations through tools.
8. Project and run deterministic validation after each coherent batch. Repair
   tool/schema mistakes within fixed limits; never weaken a rule to get a pass.
9. Compute full base-plus-projected impact and resolve required deterministic
   obligations with user evidence where policy requires it.
10. If AI review is enabled/required, show its estimated request scope/cost and
    obtain explicit authorization before invoking the separate Gate B run.
11. Present Gate B concerns to the user. Proposed repairs go through the same
    draft loop and stale the old review.
12. When the exact draft is ready, produce the commit preview and ask for one
    explicit conversational confirmation. A changed head/draft invalidates it.
13. Call the guarded commit tool through the normal atomic commit use case and
    return the structured result.

The authoring agent may pause for user input at any point without holding a
SQLite write transaction. Conversation state is not canonical state; the durable
draft and tool receipts allow a later session to resume safely.

## 6. New-project intake from text or images

The new-project command accepts a user description plus optional supported UTF-8
text and image inputs. The first release does not promise general PDF/office-file
parsing, web crawling, OCR completeness, or filesystem discovery. The supplied
bytes are explicit request inputs.

Before a database exists, the agent produces a noncanonical initialization
proposal containing:

- project ID/title;
- substantive purpose statement;
- selected built-in profile/package versions;
- proposed top-level scope nodes;
- source inventory with names, media types, hashes, and privacy warning;
- unresolved questions and extraction-coverage statement; and
- a bounded initial node/edge batch plan.

The initial Gate C proof supports the reviewed built-in `technical-project/v1`
and `catalog/v1` profiles. `catalog/v1` is intentionally small: catalog/menu,
section, item, option/variant, ingredient/attribute, availability, and source
anchor nodes plus contains, offers, has-option, categorized-as, sourced-from,
and explicit dependency edges. Prices and labels are scalar properties. This is
enough to test a real menu without pretending that technical assertions are menu
items.

The agent selects from installed exact profile versions; it does not silently
invent or mutate schema-package semantics. If the user's intent cannot be
represented, it identifies the missing vocabulary and asks whether to use a
supported approximation or stop. A later separately reviewed profile-authoring
workflow may create custom packages, but it cannot be smuggled into ordinary
content authoring.

The user confirms the purpose and project creation in conversation. The agent
then calls guarded `initialize_project`, which invokes the normal Application use
case to create the database/purpose root, begins a draft for the remaining graph,
and continues through the alteration loop itself.

For a menu image, for example, the agent may create section/item/option nodes,
scope edges, price/dietary scalar properties, and explicit relationships. It
also creates or links provenance anchors that cite the source hash and available
text line or image-region description. Raw uploaded bytes are not placed in the
canonical graph or SQLite file by default; retaining/attaching source files is a
separate future policy.

Vision and text extraction are heuristic. The agent must report unreadable
regions, uncertain transcription, inferred grouping, possible duplicates, and
unmodeled content. The application can prove that all proposed operations were
validated; it cannot prove that every menu item or source meaning was captured.

## 7. Provider orchestration and long-running calls

The first production implementation supports only OpenAI, uses the Responses
API with strict function tools, and initially uses the same evaluated
`gpt-5.6-terra`/medium configuration and `OPENAI_API_KEY` boundary as Gate B.
Re-check the supported model when this gate begins; do not silently substitute a
provider/model.

The only non-secret settings are provider, model, and per-response deadline as
listed in `.env.example`. `VW_AIAUTHORING__LIVETESTS=false` is solely the
separately invoked live-evaluation opt-in; normal tests ignore it. It does not
authorize spend, initialize a project, approve Gate B, or authorize commit.

Each provider response uses background mode and an end-to-end 1,200-second
deadline. Polling the same response is not a retry. Transport failure, timeout,
refusal, malformed tool input, or lost response state pauses the session with a
structured inconclusive result; there are no automatic paid retries.

Normal tool continuations are expected and are not retries: the model requests a
tool, the application executes it, and the tool result is submitted to continue
the same authoring turn. Fixed implementation-owned limits bound provider
responses, tool calls, operations per batch, total operations, context bytes, and
consecutive repair failures. These are safety invariants, not a menu of user
configuration knobs. Hitting a limit preserves the draft, reports usage and
remaining work, and requires an explicit user decision to resume.

The host reports model, provider-response IDs, tool-call counts, input/output
usage, elapsed time, and available cost data without exposing hidden reasoning or
secrets. It never makes background or unannounced model calls.

## 8. Conversation, approval, and audit

An authoring session records noncanonical orchestration evidence sufficient to
resume and audit actions:

- user intent and explicit answers/approvals;
- base head and active draft identity/revision/hashes;
- model/prompt/tool-schema versions;
- provider response IDs and usage metadata;
- ordered tool calls and bounded structured results or their hashes;
- unresolved questions, assumptions, and extraction coverage; and
- terminal status: awaiting-user, ready-for-confirmation, committed, aborted,
  failed, or inconclusive.

Do not store provider credentials, authorization headers, hidden chain-of-thought,
or raw private source files by default. A later migration may add authoring-session
tables; those rows remain draft/audit metadata outside the canonical logical
graph and its hash.

## 9. Plugin and MCP positioning

"Plugin" is an appropriate packaging term, not the internal product API. Current
OpenAI plugin architecture packages skills and an optional MCP server, and custom
UI is optional. ValidatedWorld should therefore:

1. stabilize Application/JSON tool contracts;
2. prove the in-app authoring agent against those contracts;
3. expose the same bounded tools through a headless MCP server;
4. add a small skill explaining safe search/draft/validate/review/confirm
   workflows; and
5. package those pieces as a ChatGPT/Codex plugin only after the tools are stable.

The MCP adapter exposes the same guarded commit tool: it succeeds only with an
opaque authorization produced from the user's approval of the exact current
preview. It must not expose raw SQL or an unguarded commit. It must remain useful
without a custom UI. A visual graph explorer/editor can be added
later only when usability evidence shows that it materially helps inspection;
it is not the primary authoring interface.

References to re-check at implementation time:

- [OpenAI plugin architecture](https://developers.openai.com/plugins/concepts/plugins)
- [OpenAI MCP server guidance](https://developers.openai.com/plugins/build/mcp-server)
- [OpenAI function calling](https://developers.openai.com/api/docs/guides/function-calling)
- [OpenAI background mode](https://developers.openai.com/api/docs/guides/background)
- [GPT-5.6 Terra](https://developers.openai.com/api/docs/models/gpt-5.6-terra)

## 10. Secrets and implementation stop rule

The same human-owned OpenAI key may support Gate B and this feature. It never
enters project data. Before the first AI-authoring implementation task, the
initiating human prompt must contain:

```text
AI_AUTHORING_SECRET_READY: yes
```

The human must have installed their own key using the documented Secret Manager
workflow. The coding agent may not search for, obtain, infer, print, copy, or set
one. If the attestation is absent, it reports the prerequisite and makes no
AI-authoring implementation changes.

A live authoring evaluation additionally requires:

```text
AI_AUTHORING_LIVE_CALL_AUTHORIZED: yes
```

Normal tests use fake model clients and scripted tool-call responses. An
interactive product user explicitly starting or resuming an AI-authoring session
authorizes the displayed provider calls for that bounded session; Gate B review
and final commit still require their separate confirmations.

## 11. Testing and evidence gate

The offline suite must prove:

- strict tool schemas and identical semantics across CLI, in-app, and MCP
  adapters;
- deterministic search/navigation, pagination, and no duplicate creation in
  representative cases;
- multi-turn scripted authoring, questions, resume, cancellation, and bounds;
- no direct SQL/canonical mutation; guarded model-called commit succeeds only
  after exact conversational user authorization;
- exact draft/head/hash binding for final confirmation;
- validation/impact failures cause repair or a truthful stop, never rule bypass;
- Gate B remains independent and authoring changes stale prior reviews;
- text and image fixture intake produces expected candidate operations,
  provenance, uncertainty, and coverage reports with a fake client;
- secrets/private bytes do not leak to project JSON, logs, diagnostics, or
  fixtures; and
- no build, unit, integration, ordinary end-to-end, or agent QA run contacts a
  live provider.

The live evaluation uses reviewed TechnicalProject and restaurant-menu fixtures.
Measure successful task completion, graph correctness against known expected
nodes/edges, duplicate and unrelated-change rate, questions asked, validation
repair rate, tool calls, tokens, time, cost, user effort, and whether the agent
stops honestly on missing context. Retain the feature only if it makes explicit
graph authoring substantially easier without weakening commit guarantees.

## 12. Explicit non-goals

The initial AI-authoring gate does not provide an autonomous always-running
agent, arbitrary internet research, hidden scheduled changes, direct database
access, automatic final commit, automatic concern disposition, finished-document
generation, source-document synchronization, multi-provider routing, a visual
graph editor, or a guarantee of exhaustive/correct extraction.

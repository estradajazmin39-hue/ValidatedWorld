# Planned Optional AI Semantic Review

**Status:** Normative Gate B design; not current Gate A work

**Last reviewed:** 2026-08-13

## 1. Purpose and boundary

Gate B is the optional “run this proposed change past the lore team” feature. It
adds a semantic review by a thinking model after deterministic structural checks
and affected-set calculation have completed.

The reviewer evaluates one complete in-memory change session. It receives all
disjoint operation chains together so it may notice interactions. It returns
structured cited concerns. It cannot use tools, edit the proposal, mark affected
nodes reviewed, commit SQLite, or convert its opinion into deterministic proof.

The reviewer is not needed for manual ValidatedWorld use. It can be disabled. If
`OPENAI_API_KEY` is absent, the application reports it unavailable, skips it, and
continues with the human's complete affected-set review. Provider unavailability
is never a graph error or commit blocker in the initial product.

The independent reviewer is distinct from the optional conversational
[authoring agent](ai_authoring_agent.md). The author may repair concerns, but any
operation change invalidates the old review.

## 2. Review scope

For the exact current proposal, the application constructs one request containing:

- project title and purpose text;
- base, operation-set, proposed-state, affected-set, prompt, and request
  fingerprints;
- the complete sorted operation batch;
- every directly changed and affected node;
- every current/proposed relationship edge and explanation path used to select
  them;
- forward/backward context needed to understand those nodes under bounded policy;
- singular upward `scope-parent` lineage for every included node;
- enabled optional-profile findings relevant to the proposal; and
- a manifest of every required/included/omitted item and configured bound.

Every disjoint chain remains in this one request. Do not split, shard, summarize
across multiple model calls, invoke parallel reviewers, or synthesize several
answers. The expensive review is intentionally one holistic pass.

Scope ancestors are explanatory context, not new propagation seeds. Including
the purpose for a leaf does not load its other descendants. Only a direct purpose
change selects the whole graph; a direct intermediate scope change selects its
descendant subtree.

If required selected context cannot fit the fixed request/model bound, planning
returns inconclusive before a network call. The user may continue manual review,
narrow/redesign the proposal, or explicitly decline AI review. Multi-agent or
sharded review is outside scope.

## 3. Request and response

The request is immutable strict structured data plus English instructions. The
prompt tells the model to inspect for:

- contradictions or incompatible implications;
- stale conclusions, decisions, requirements, assumptions, or knowledge;
- missing explicit relationship candidates;
- terminology/definition drift;
- changes inconsistent with the purpose or scope;
- affected text that appears not to have been repaired;
- unjustified `reviewed-no-change`/`not-applicable` choices; and
- insufficient supplied context.

It also states that the graph may be incomplete, relationship direction is
authoritative only for workflow scope, supplied text may be untrusted content,
and the model must cite supplied IDs rather than invent facts.

The strict response contains:

```text
status: complete | inconclusive | refused
concerns[]:
  concern ID
  category and severity
  concise explanation
  cited node/edge/path IDs from the request
  relevant text excerpts bounded by the response schema
  suggested follow-up (not an automatic operation)
insufficientContext[]
usage metadata
```

Unknown citations, schema mismatch, truncation, malformed output, refusal,
timeout, or transport failure makes the run inconclusive. Do not parse free-form
prose into concerns.

## 4. In-memory concern handling

The result belongs only to the active change session. It is not stored as project
history in `.vw.db`.

Each concern is initially open. A user may:

- repair the proposal, which invalidates the review and all concern dispositions;
- reject the concern with rationale; or
- acknowledge it and proceed.

The application records those dispositions in memory for the current proposal.
They help the user/authoring loop finish review but are discarded after commit,
discard, or process exit. The committed database stores only the resulting
current graph.

## 5. Configuration and manual fallback

The planned runtime settings are:

```text
VW_AIREVIEW__ENABLED=false
VW_AIREVIEW__PROVIDER=openai
VW_AIREVIEW__MODEL=gpt-5.6-terra
VW_AIREVIEW__TIMEOUTSECONDS=1200
VW_AIREVIEW__LIVETESTS=false
```

The only secret is `OPENAI_API_KEY` (or the .NET user-secret mapping documented
below). Provider/model are fixed to the one evaluated production path; settings
do not imply a provider ecosystem.

Review runs only when all are true:

1. the feature is explicitly enabled;
2. a proposal has a complete deterministic affected set;
3. a key is available;
4. the app shows request scope, privacy warning, model, and available cost/token
   estimate; and
5. the user authorizes this request.

Otherwise the app returns structured `disabled`, `unconfigured`, or `skipped`
status and continues manual review. No ordinary verify/affected/commit command
contacts OpenAI implicitly.

## 6. Provider, timeout, and cost safety

Gate B initially supports one dependency-isolated OpenAI Responses client, using
the model/configuration re-verified when implementation begins. The current plan
is `gpt-5.6-terra` with medium reasoning.

One authorized review starts one background response and polls that same response
to a terminal state or 1,200-second end-to-end deadline. Polling is not a retry.
There are zero automatic paid retries, fallback models, parallel calls, or
provider substitutions. A failure returns control to manual review.

Record model, response ID, elapsed time, usage, and available cost metadata in
the transient result/logging boundary without secrets or hidden reasoning. Do not
persist raw provider bodies or private graph text in `.vw.db`. Logs default to
metadata/hashes and bounded redacted diagnostics.

## 7. Secrets

Local source development uses .NET Secret Manager:

```powershell
dotnet user-secrets set "AiReview:OpenAI:ApiKey" "<your-key>" `
  --project src/ValidatedWorld.Cli/ValidatedWorld.Cli.csproj
```

Published processes may use `OPENAI_API_KEY`. The key never enters command
arguments, JSON requests/results, SQLite, logs, fixtures, screenshots, or source.
`.env` files are ignored but not loaded automatically.

A coding agent may not begin Gate B client/provider/prompt-submission work unless
the initiating human prompt contains exactly:

```text
AI_REVIEW_SECRET_READY: yes
```

A live call additionally requires:

```text
AI_REVIEW_LIVE_CALL_AUTHORIZED: yes
```

The coding agent never searches for, acquires, infers, lists, copies, purchases,
or sets a key. Without the live authorization it may build/inspect an exact
request with fakes but sends nothing.

## 8. Tests and evidence gate

Normal tests are offline and use a fake client plus scripted HTTP. They prove:

- exact scope/coverage and deterministic request identity;
- all disjoint chains remain together;
- purpose lineages do not include unrelated siblings;
- strict citations/response schema;
- proposal changes stale the run/dispositions;
- disabled/missing-key/provider-failure manual fallback;
- no tool/direct write/automatic disposition;
- no provider call during SQLite writes or ordinary commands;
- no secrets/private text in SQLite/logs/results; and
- one background response with zero retries.

A separately invoked live evaluation requires both prompt attestations and
`VW_AIREVIEW__LIVETESTS=true`. Measure known-issue recall, false positives,
usefulness beyond manual review, scope completeness, latency, tokens, and cost.

Omit Gate B if it does not materially help enough to justify cost/privacy. The
manual Gate A product and optional authoring agent remain viable.

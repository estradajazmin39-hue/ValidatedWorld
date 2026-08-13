# Planned AI Semantic Review

**Status:** Normative post-Gate-A design; not part of the current WP0-WP9
implementation roadmap.

**Last reviewed:** 2026-08-13

This is the expensive "run the transaction past the lore team" pass. It reviews
qualitative coherence that the deterministic engine cannot prove. Canonical
content arrives as the project's one node/edge graph; type packages and the
transaction ledger are supporting metadata. It is not
document generation, RAG, an autonomous database agent, or a second way to
mutate canonical state.

This reviewer is deliberately separate from the later AI authoring agent in
[AI-first authoring and intake](ai_authoring_agent.md). The authoring agent uses
tools to propose and repair a draft. The reviewer receives one fresh,
tool-free, whole-transaction request and cannot rely on the author's conversation
or approve its own work.

## 1. Evidence boundary

The reviewer may identify likely:

- conflicts with the project's overarching purpose;
- missing dependencies or consequences;
- contradictions across otherwise disjoint parts of one transaction;
- stale values, implications, terminology, or qualifications;
- implausible character, design, policy, or world reactions; and
- insufficient context.

Its output is always a **Concern**, never `Proven` or `Disproven`. The
application can prove that one exact request covered the required transaction
scope and that every returned concern was dispositioned. It cannot prove that
the model noticed every issue or that a concern is correct.

The AI review never writes canonical rows, applies operations, selects impact,
browses, calls tools, or supplies deterministic evidence. Candidate changes are
suggestions that a human or agent may separately apply through the normal
transaction API.

## 2. Required project purpose root

Every project has exactly one `core:project-purpose` node. Initialization
creates it before any ordinary project content and requires an English purpose
statement. Examples include:

- "Define a McDonald's menu for this restaurant and market."
- "Specify an offline sensor that runs for at least 24 hours without uploading
  raw readings."
- "Maintain the canon and disclosure logic of the Harbor mystery."

The project snapshot identifies the purpose node by stable ID. Scope is a
spanning tree over nodes, distinct from arbitrary cross-linking domain edges:

- every node except the purpose has exactly one
  `scope-parent`;
- one parent may have any number of children;
- following a node's parent repeatedly must terminate at the one purpose;
- the hierarchy is acyclic; and
- graph edges are reviewed with their source/target nodes and those nodes'
  lineages; constraint and higher-arity concepts are nodes and have parents.

The enforceable rule is:

> Every canonical node has exactly one upward scope lineage to
> the project purpose, and review includes that lineage for each selected node.

`scope-parent` is a canonical typed edge whose dependency rule is
`child depends on parent` with `CreatesReviewImpact = true`. Impact traversal is
seeded only by actual transaction operation targets. Scope ancestors added later
for context are never promoted to impact seeds.

Consequently:

- changing a leaf includes its ancestors as context but not its siblings;
- directly changing an intermediate scope node impacts only its descendant
  subtree (plus any separately declared semantic dependents); and
- directly changing the project-purpose root impacts every descendant and is the
  one ordinary case that requires a full-project review.

Walking upward for context follows only the singular parent direction. Reaching
an ancestor or the purpose during that walk never causes traversal back down into
other children.

The semantic dependency view expands direction from the same canonical typed
edges and may branch or cross-link. There is no hidden reference-field graph.
Multiple changed/impacted nodes may therefore contribute different dependency
closures and different scope lineages, all of which appear together in the one
transaction review request.

This root and singular lineage prevent disconnected semantic islands and give
the reviewer global intent without widening scope downward. They do not make
natural-language brand or thematic knowledge
deterministic. "Whopper" under a McDonald's-menu purpose is an AI concern unless
the project also declares an explicit supported-brand constraint, in which case
the deterministic validator may reject it.

## 3. One whole-transaction review request

One AI review run means exactly one model request containing the entire proposed
transaction. Disjoint dependency chains are never reviewed in separate calls.
The request contains, in deterministic order:

1. Project identity, exact purpose node, review rubric, and evidence boundary.
2. Transaction identity, intent, author, base revision/hash, draft revision,
   change-set hash, and projected-state hash.
3. Every node/edge transaction operation with the complete base and projected
   form of its target where applicable.
4. The complete policy-selected reverse impact closure for all operation targets
   in the union of the base and projected graphs.
5. Every forward dependency needed to understand every changed or impacted
   node.
6. The singular upward `scope-parent` lineage from every included node
   to the project purpose. Ancestor traversal never includes siblings or other
   descendants.
7. Applicable type definitions, deterministic constraints, external anchors,
   impact explanation paths, and current review obligations.
8. A coverage manifest listing the exact required node, edge, dependency-arc,
   and scope-edge sets and proving that none were omitted.
9. The strict structured response schema.

All operation targets and all disjoint closures appear together so the model can
find cross-chain contradictions and aggregate effects. There is no sharding,
synthesis call, recursive summarization, or per-chain provider call in the
roadmap.

If the complete request cannot fit the one supported model or any required
node/edge/purpose path is missing, planning returns `Inconclusive` **before any
network call**. The product does not trade away whole-transaction reasoning to
force a result.

## 4. Exact request preview and English prompt

The prompt is a versioned English source resource, initially
`ai-semantic-review/v1`. Localization is out of scope. It instructs the reviewer
to:

1. Review the transaction as one change, including interactions among disjoint
   chains.
2. Treat the project purpose as global intent and flag likely conflicts with it.
3. Trace each operation through supplied dependencies, impacts, constraints,
   and purpose paths.
4. Distinguish contradictions, missing links, stale consequences, terminology
   drift, missing qualifications, and insufficient context.
5. Use only supplied data, clearly label inference, and make no external factual
   claims.
6. Cite supplied entity IDs plus property, edge, or impact-path evidence for every
   concern.
7. Return only the strict response schema and never propose direct canonical
   mutation.

Before a paid request, the CLI produces the exact secret-free request body:

```text
vw tx ai-review preview --db <path> --tx <id> --output <path>
```

The preview includes the full system/developer instruction text, structured
payload, response schema, model, reasoning setting, and request hash. The live
command must send byte-for-byte equivalent semantic content under that hash.
Authorization headers and credentials are never part of the preview.

Do not dump the full request to ordinary stdout/stderr. CLI stdout must remain
one bounded JSON result, and project content may be confidential. The user
chooses the preview path; development QA normally uses an ignored file under
`artifacts/ai-review/`. Normal logs contain only hashes, counts, sizes, status,
and provider request ID. A sanitized TechnicalProject request is retained as a
golden test asset so coding agents can inspect prompt and context completeness
without spending money.

## 5. One supported provider and model

The entire planned roadmap supports one production provider:

```text
provider = openai
model = gpt-5.6-terra
reasoning.effort = medium
```

The model is chosen because current official OpenAI documentation describes
GPT-5.6 Terra as balancing intelligence and cost and gives it a 1,050,000-token
context window. Re-check availability when Gate B begins. If it is unavailable
or the configured account cannot use it, stop and ask the human; do not silently
switch models.

`VW_AIREVIEW__PROVIDER` and `VW_AIREVIEW__MODEL` remain visible configuration so
runs are explicit and auditable, but the roadmap accepts only the exact values
above. Supporting arbitrary providers/models, dynamic discovery, fallback
routing, or a plugin ecosystem is out of scope.

`ValidatedWorld.AiReview` contains request planning, concerns, and a small client
interface so tests can use a fake. `ValidatedWorld.AiReview.OpenAI` is the only
production implementation. That seam is for dependency isolation and testing,
not a promise of provider extensibility.

Use the Responses API, background mode, strict Structured Outputs, no tools, and
no conversation state. Polling the same response object is transport/status
handling, not another model request or retry. The adapter sets a fixed
non-configurable maximum of 16,384 output tokens; the expected output is a
bounded list of concerns, not prose. Re-check the
official [GPT-5.6 Terra model page](https://developers.openai.com/api/docs/models/gpt-5.6-terra),
[OpenAI .NET quickstart](https://developers.openai.com/api/docs/quickstart), and
[Structured Outputs guide](https://developers.openai.com/api/docs/guides/structured-outputs),
plus [Background mode](https://developers.openai.com/api/docs/guides/background)
when implementation begins.

## 6. Human-owned API key prerequisite

The agent assigned the first Gate B coding task **must not edit code, add
packages, or run implementation commands** until the human's initiating prompt
contains this exact attestation:

```text
AI_REVIEW_SECRET_READY: yes
```

Before sending that prompt, the human—not the coding agent—must obtain their own
OpenAI API key and store it with:

```powershell
dotnet user-secrets set "AiReview:OpenAI:ApiKey" "<key>" --project src/ValidatedWorld.Cli/ValidatedWorld.Cli.csproj
```

The key itself must never be pasted into the prompt. If the attestation is
missing, the agent reports the command and stops without making changes. If a
possible key appears in chat or repository content, the agent must not use,
repeat, move, test, or store it; it tells the human to revoke/rotate it and
stops.

The coding agent is expressly forbidden to:

- search the web, repository, browser state, environment, shell history, logs,
  other projects, or credential stores for an API key;
- create an account, obtain, generate, buy, borrow, infer, or reuse a key;
- run `dotnet user-secrets set`, edit the secret store, or configure the key on
  the human's behalf;
- list or print secret values to verify the attestation; or
- substitute another credential, provider, endpoint, or model.

The application later verifies only that usable credentials are available when
an explicit live command begins; it never prints their value. Gate A and the
normal Gate B fake/scripted suite require no secret.

For source development, the CLI's stable `UserSecretsId` stores
`AiReview:OpenAI:ApiKey` outside the repository. A published process uses
`OPENAI_API_KEY`. Secret Manager is development storage, not an encrypted
production vault. See Microsoft's [.NET Secret Manager
guidance](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0).

## 7. Paid-call safety

Possessing a key does not authorize spending. A coding or QA agent may make a
live request only when the same initiating human prompt also contains:

```text
AI_REVIEW_LIVE_CALL_AUTHORIZED: yes
```

Paid-call rules:

- `preview` is mandatory and must be inspected before the first live call.
- The preview reports model, UTF-8 request bytes, a clearly labeled conservative
  token estimate, the fixed output cap, and the number of nodes/edges/paths.
- One explicit `run` command makes one provider request.
- There are **zero automatic retries**, including after timeouts or ambiguous
  transport failures. Failure is `Inconclusive`; another paid attempt requires a
  new explicit command and human authorization.
- Builds, unit tests, integration tests, end-to-end tests, and ordinary agent QA
  never make live calls.
- A Gate B live smoke/evaluation is one known fixture and one request unless the
  human separately authorizes more.
- Background execution and polling of that one response are required for
  long-running reasoning. No parallel, recursive, fallback, or tool call is
  allowed.

The adapter has only three non-secret runtime settings: provider, model, and
timeout. The timeout defaults to 1,200 seconds and is an end-to-end deadline for
the background response to reach a terminal state, not a request-retry interval.
Input/output/request-count/cost/retry configuration knobs are not part of the
roadmap. The fixed output cap and zero-retry rule are implementation safety
invariants rather than user settings.

`VW_AIREVIEW__LIVETESTS=false` is a fourth setting belonging only to the
separately invoked live smoke/evaluation harness. Unit, integration, and ordinary
end-to-end tests ignore it and stay offline. Setting it true does not authorize a
paid call by itself; the secret-readiness and live-call attestations above still
apply.

This harness switch is separate from product behavior. Gate B project policy is:

- `disabled`: no review or skip record is needed;
- `optional`: the transaction may run review or explicitly record `skipped` with
  actor, rationale, time, draft revision, and change-set hash; or
- `required`: a fresh successful review and dispositions are mandatory.

Changing the draft stales a skip. An environment variable can never downgrade or
bypass `required`. The CLI exposes an explicit `vw tx ai-review skip` command for
the optional case so the decision is auditable.

## 8. Response, persistence, and commit policy

The strict response contains:

- `complete`, `failed`, `canceled`, or `inconclusive` status;
- structured concerns and explicit insufficient-context findings;
- actual provider/model, provider request ID, usage, and finish/refusal data;
  and
- request, raw-response, normalized-response, schema, and prompt hashes.

Each concern contains a stable run-local ID, `AIxxxx` code, category, severity,
message, cited entity IDs, cited property/edge/path evidence, optional confidence,
suggested follow-up, and fingerprint. Unknown citations, schema mismatch,
refusal, truncation, or malformed content makes the run failed/inconclusive.
Free-form prose is never loosely parsed into findings.

Concern dispositions are `open`, `resolved-by-change`,
`rejected-with-rationale`, and `acknowledged`. Policy decides which allow commit.
Non-open dispositions require actor, rationale, time, and exact run/concern
fingerprints. Changing any operation or any material request input stales the
review.

Gate B adds checked SQLite migrations for review runs and concerns. These are
draft/audit state, not logical project state. Persist no credentials or
authorization headers. Raw request/response retention is an explicit project
data policy because the content may be confidential; normalized concerns and
hashes remain auditable.

## 9. Testing and acceptance

The normal suite is offline and deterministic:

- fake clients cover zero, one, and many concerns;
- scripted HTTP handlers cover request/response mapping, refusal, malformed
  output, cancellation, and rate limiting without a network;
- scripted polling tests cover queued/in-progress/terminal transitions and
  cancellation; no wall-clock 1,200-second test is required, and any live
  deadline expiry is inconclusive;
- golden request previews prove the purpose, complete transaction, every
  disjoint dependency closure, every required singular scope lineage, absence of
  unselected siblings, constraints, coverage manifest, English prompt, and
  response schema are present;
- tests prove one run invokes the client at most once and never retries;
- tests prove missing attestation/configuration prevents a live call;
- freshness/hash tests cover every material non-secret input; and
- persistence/CLI tests prove no secret leakage and no automatic mutation.

The usefulness evaluation uses reviewed TechnicalProject transactions that
change multiple disjoint tracks together and contain known purpose conflicts,
omitted links, stale values, terminology conflicts, missing qualifications, and
unrelated distractors. Measure known-issue recall, false-positive burden, cost,
latency, and whether the whole-transaction prompt identifies cross-chain issues.

Gate B succeeds only if one expensive review provides useful global scrutiny at
tolerable cost without weakening the deterministic core. If it does not, remove
the in-app call while retaining deterministic transaction/impact/context data.

## 10. Explicit non-goals

This feature does not author or synchronize a novel, paper, patent, manual, or
game artifact. It does not support other providers/models, multiple reviewer
calls, sharding, automatic retry, provider fallback, localization, tools, web
search, external fact-checking, or automatic acceptance of suggestions.

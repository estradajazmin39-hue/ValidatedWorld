# Planned AI Semantic Review

**Status:** Normative post-Gate-A design; not part of the current WP0-WP9
implementation roadmap.

This document restores the bounded AI-assisted reviewer present in the early
ValidatedWorld design. It is the first recommended phase after Gate A if the
deterministic semantic change-control core proves useful. It is not document
generation, RAG, an autonomous database agent, or a substitute for deterministic
validation.

## 1. Purpose and evidence boundary

Deterministic validation can prove only rules expressed in the project model.
An intelligent reviewer is useful for questions such as:

- Did this change leave a likely implication stale even though no explicit rule
  captured it?
- Is a dependency, qualification, definition, clue, or character reaction
  probably missing?
- Do the changed and impacted records still make semantic sense together?
- Does supplied free-form text contradict structured values or use terminology
  inconsistently?
- Is the supplied context insufficient to judge the issue?

The reviewer returns **concerns**, not `Proven` or `Disproven` results. The
application may guarantee that a required review ran against an exact projected
state and that every concern was dispositioned. It cannot guarantee that the
model noticed every problem or that a concern is correct.

An AI review must never:

- write canonical rows or apply a transaction operation;
- turn a proposed record, link, or disposition into canon automatically;
- convert a provider response into deterministic validation evidence;
- browse, retrieve external material, or call tools unless a later separately
  specified review profile explicitly authorizes and audits that capability;
- run while a SQLite write transaction is open; or
- expose an API key in a database, snapshot, request packet, log, diagnostic,
  cache key, test fixture, or command-line argument.

## 2. Place in the transaction workflow

The planned Gate B sequence is:

```text
load a durable draft and exact base head
-> project the transaction
-> run deterministic validation and impact analysis
-> build the policy-selected semantic-review scope
-> build deterministic bounded review packets and a coverage manifest
-> call or import results from a provider-neutral reviewer
-> schema-validate and persist cited concerns as draft/audit data
-> repair the draft and rerun, or disposition every policy-required concern
-> recheck review freshness during commit
-> commit atomically, or reject without changing authoritative state
```

Network calls happen before the short SQLite commit transaction. Changing the
draft, projected hash, review profile, prompt template, packet plan, provider,
model, or material generation parameters makes the review stale.

By default AI review is opt-in and non-blocking. A project policy may require a
named review profile for selected object types or change categories, require a
complete review run, and require every concern to be resolved, rejected with a
rationale, or explicitly acknowledged before commit.

## 3. Reviewing the dependency chain

"Review the full dependency chain" must have a finite, testable meaning. It does
not mean sending an unlimited database or claiming the model understood it.

For a transaction, the application deterministically constructs a `ReviewPlan`:

1. Start with every directly changed object.
2. Add the complete policy-selected impact closure and its explanation edges.
3. Add forward dependencies required to understand those objects.
4. Add applicable type definitions, constraints, and bound external anchors.
5. Record the exact required object and edge sets, exclusions, limits, and plan
   hash.

If one request can contain that plan, the application emits one packet. If it
cannot, it partitions the plan deterministically by impacted root/path cluster:

1. Every shard includes its changed/impacted objects, relevant forward
   dependencies, constraints, and boundary-edge stubs.
2. Objects and edges are never cut midway. Every omission is explicit.
3. A coverage manifest proves that each required object and dependency edge was
   presented in at least one shard.
4. A final synthesis packet contains changed objects, cross-shard edges, packet
   summaries, and all concerns so the reviewer can identify cross-shard issues.
5. The run is `Complete` only when every required shard and synthesis call
   succeeds and coverage is complete. Otherwise it is `Inconclusive`.

This makes review coverage auditable. It still does not make model comprehension
or concern recall deterministic. Gate B should first prove value on review plans
that fit into one request; sharding is implemented only when measured project
sizes require it.

## 4. Provider-neutral contracts

The exact immutable types are finalized during the Gate B planning task, but the
contract must preserve these fields:

```csharp
public interface IProjectSemanticReviewProvider
{
    string ProviderId { get; }
    Task<AiReviewResponse> ReviewAsync(
        AiReviewRequest request,
        CancellationToken cancellationToken);
}

public sealed record AiReviewRequest(
    string SchemaVersion,
    string ProjectId,
    long BaseRevision,
    string BaseLogicalHash,
    string TransactionId,
    long DraftRevision,
    string ChangeSetHash,
    string ProjectedLogicalHash,
    string ReviewProfileId,
    string ReviewProfileVersion,
    string PromptTemplateId,
    string PromptTemplateVersion,
    string PromptTemplateHash,
    string ReviewPlanHash,
    string ContextPacketHash,
    string ProviderId,
    string ModelId,
    JsonElement Parameters,
    JsonElement ContextPacket);
```

A versioned response contains:

- run status: `complete`, `failed`, `canceled`, or `inconclusive`;
- zero or more structured concerns;
- explicit insufficient-context observations;
- candidate records, relationships, or operations in a separate proposal list;
- the provider/model actually used, provider request ID when available, usage,
  and finish/refusal information; and
- request, raw-response, normalized-response, and response-schema hashes.

Each concern contains a stable run-local ID, `AIxxxx` code, category, severity,
message, cited object IDs, cited field/edge/path evidence from the supplied
packet, optional confidence, a suggested follow-up, and a fingerprint. A
response that cites unknown IDs, fails schema validation, omits required packet
coverage, is truncated, or is refused becomes a provider failure or
inconclusive run. Its prose is never loosely parsed into findings.

Concern dispositions are:

- `open`;
- `resolved-by-change`;
- `rejected-with-rationale`; and
- `acknowledged`.

Policy decides which dispositions permit commit. Non-open dispositions require
actor, rationale, time, and the exact concern/run fingerprint.

## 5. Persistence and audit

Gate B adds a checked SQLite migration for review runs, packet manifests, and
concerns. These are draft/audit records, not part of the logical project hash.
An accepted commit receipt retains the review-run identity and concern
dispositions that satisfied policy.

Persist at least:

- transaction and draft revision;
- base, change-set, projected-state, review-plan, and packet hashes;
- review/profile/prompt/schema versions;
- provider ID, requested model, returned model, non-secret parameters, status,
  timestamps, provider request ID, usage, and bounded error metadata;
- normalized concerns, evidence, fingerprints, and dispositions; and
- hashes of raw request/response bodies when retaining the bodies is disabled.

Never persist credentials, authorization headers, or a configuration dump. Raw
request/response retention is an explicit project setting because the context
may contain confidential project data. Hash-only retention must still preserve
the normalized concerns and enough metadata to audit what policy accepted.

## 6. Provider packages and OpenAI adapter

Provider code is dependency-isolated:

```text
ValidatedWorld.AiReview             Core, Serialization, Validation
ValidatedWorld.Application          later adds AiReview for transaction use cases
ValidatedWorld.Persistence.Sqlite   implements Application review persistence ports
ValidatedWorld.AiReview.OpenAI      AiReview plus the pinned OpenAI client
ValidatedWorld.Cli                  composes Application plus an optional provider
```

Core, Serialization, Validation, Application, and SQLite persistence do not
reference an OpenAI package. Application owns the transaction use case and
persistence ports; the review package owns packet planning and provider-neutral
contracts; SQLite implements the ports; the OpenAI package is one replaceable
adapter. The product can also import a versioned structured response produced by
an external human or agent.

The first OpenAI adapter should use the Responses API with a strict JSON Schema
matching the versioned review response. The model ID is required configuration,
not a hard-coded "latest" choice, and the run records the actual model returned.
The adapter supplies only the prepared packet and review rubric, exposes no
tools, uses bounded retries only for transient failures, honors cancellation and
timeout, and reports refusal or incomplete output as inconclusive. Re-check the
current official [OpenAI .NET quickstart](https://developers.openai.com/api/docs/quickstart)
and [Structured Outputs guide](https://developers.openai.com/api/docs/guides/structured-outputs)
when Gate B begins.

## 7. Secrets and local configuration

Gate A requires no API key. Gate B uses normal .NET configuration rather than a
custom secret store:

- For source-checkout development, `ValidatedWorld.Cli` carries a stable
  `UserSecretsId`; store `AiReview:OpenAI:ApiKey` outside the repository. Gate B
  adds `Microsoft.Extensions.Configuration.UserSecrets` and the runtime
  configuration provider that consumes it.
- For a published CLI, CI, or container, read `OPENAI_API_KEY` from the process
  environment. The OpenAI SDK also recognizes this conventional name.
- Read non-secret hierarchical settings from the `VW_`-prefixed environment
  variables documented in [`.env.example`](../.env.example), with `__` as the
  .NET hierarchy separator.
- Never accept a secret as a CLI option, JSON request field, project setting, or
  database value.

The repository ignores `.env` and `.env.*` except `.env.example`. The example is
a name/template catalog only. ValidatedWorld will not silently search parent or
working directories for a `.env` file; that behavior is surprising for a CLI
that may be invoked against untrusted projects. A user's launcher may explicitly
load a private `.env` into the process environment.

Secret Manager keeps development values outside the project tree but does not
encrypt them and is not a production vault. See Microsoft's [.NET Secret Manager
guidance](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)
and [configuration-provider guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers).

Configuration precedence for the adapter is:

1. explicit non-secret command/request settings allowed by the contract;
2. process environment (`OPENAI_API_KEY`, then `VW_...` settings);
3. .NET user-secrets during local development; and
4. non-secret application defaults.

There is no default provider or model, and missing credentials fail before any
network call with a structured configuration error.

## 8. Privacy, cost, and failure behavior

An external review sends selected project content off-machine. The command must
show the provider, model, packet counts, object/edge coverage, truncation state,
and configured limits before a call. Network use is explicit; normal
`verify`, `impact`, and `commit` never contact a provider merely because a key is
present.

Gate B provides limits for request count, input/output tokens, timeout, retries,
and—when reliable pricing metadata is configured—estimated cost. Exceeding a
limit is inconclusive. Provider unavailability, rate limiting, refusal,
cancellation, malformed output, and secret/configuration errors cannot corrupt a
draft and never become content-validation failures.

## 9. Testing and Gate B acceptance

The normal restore/build/test suite never needs a secret or live network:

- deterministic fake providers exercise zero/many concerns;
- scripted HTTP handlers exercise OpenAI request/response mapping without a
  remote call;
- malformed, partial, refused, timed-out, canceled, rate-limited, and retry
  cases are covered;
- property tests prove request/cache/freshness hashes change for every material
  input and never include secrets;
- persistence tests prove stale runs cannot satisfy policy and failed calls
  cannot change canon;
- CLI tests prove exactly one JSON result, no secret leakage, and no implicit
  network use; and
- live evaluation is separately opt-in and never a default acceptance test.

Gate B's usefulness evaluation uses reviewed TechnicalProject variants with
deliberately omitted links, stale numbers, inconsistent terminology, missing
qualifications, and unrelated distractors. Measure concern precision, recall
against the known issue set, false-positive burden, cost, latency, and whether
scoped dependency packets outperform an unscoped whole-document prompt.

Gate B succeeds only if it finds useful issues at tolerable cost without
weakening the deterministic core or implying proof. If the in-app adapter adds
no material value over exporting a context packet to an external agent, retain
the provider-neutral import/export contract and remove the built-in call.

## 10. Explicit non-goals

This feature does not restore document generation or synchronization. It does
not author a novel, paper, patent, manual, or game artifact. Candidate semantic
records or transaction operations are proposals only. External tools remain
responsible for using accepted project data and transaction impact to update
finished artifacts.

# ValidatedWorld Testing, Fixtures, and Agent QA

**Status:** Normative development policy

**Last reviewed:** 2026-08-12

This document defines how ValidatedWorld proves correctness and usability. It is
read with the [implementation blueprint](implementation_blueprint.md) and the
[implementation plan](implementation_execution_plan.md).

## 1. No external database infrastructure

SQLite is an in-process, self-contained, serverless database engine; it is not a
database server. ValidatedWorld uses `Microsoft.Data.Sqlite.Core` with a pinned
`SQLitePCLRaw.bundle_e_sqlite3` NuGet dependency. The bundle supplies the native
SQLite library for its supported runtime identifiers and is initialized by
`Microsoft.Data.Sqlite`.

References:

- [SQLite: self-contained, serverless, zero-configuration](https://sqlite.org/about.html)
- [Microsoft.Data.Sqlite custom native bundles](https://learn.microsoft.com/dotnet/standard/data/sqlite/custom-versions)

Users and QA agents must not need:

- a SQLite server;
- the standalone `sqlite3` executable;
- a system-installed SQLite library;
- Docker or another container runtime;
- SQL knowledge for ordinary project creation, authoring, validation, review,
  backup, or replay; or
- any service beyond the installed/published ValidatedWorld application.

Documented read-only SQLite views are an optional advanced inspection surface,
not a prerequisite for completing normal workflows.

Gate A must fail rather than silently add infrastructure if the NuGet-bundled
provider cannot support an intended platform. Record the failing runtime and
discuss alternatives with the human before adding Docker, a system dependency,
or another database. Docker is not part of the current roadmap.

## 2. Application-owned database lifecycle

Every normal or test database is created, migrated, verified, backed up, and
opened by ValidatedWorld itself. No fixture setup may issue DDL or canonical
inserts through an external SQLite tool.

Required public workflows include:

```text
vw init ... --purpose <text>        create a project with its purpose root
vw snapshot init ...                create from supported logical JSON
vw sample list                      list bundled reusable sample scenarios
vw sample create ...                generate a sample database through the app
vw verify ...                       verify the created/opened project
vw backup ...                       create a safe SQLite backup
```

`vw sample create` is introduced with the WP3 walking skeleton. It takes a named
built-in sample and variant, creates a new target that must not already exist,
and uses the same Application/Persistence paths as normal initialization. It
never copies a hand-edited opaque database.

Example:

```powershell
dotnet run --project src/ValidatedWorld.Cli -- sample create `
  --sample technical-project `
  --variant baseline `
  --db artifacts/qa/technical-project.vw.db
```

The actual command syntax is locked by CLI contract tests when WP3 implements
it. Generated files under `artifacts/` and ordinary `.vw.db` files are ignored.

For real users, the SQLite project file and a backup produced by `vw backup` are
the primary complete interchange artifacts because they retain current state,
drafts, and commit/audit history. Logical JSON remains a supported transparent,
deterministic interchange, audit, revision-zero initialization, and test-fixture
format; it is not the only way to move a project.

## 3. Durable scenario source, disposable databases

An AI authors each scenario or regression variant once. Its reviewed source and
expected behavior are then retained so future work regenerates and reuses it
without asking another model to invent equivalent data.

The target layout is:

```text
samples/TechnicalProject/
  README.md
  scenario.manifest.json
  baseline.snapshot.json
  variants/
    missing-evidence.snapshot.json
    explicit-contradiction.snapshot.json
  transactions/
    change-current.json
    change-retention.json
    permit-diagnostic-upload.json
  goals/
    inspect-project.md
    diagnose-privacy-claim.md
    revise-retention-policy.md
  expected/
    *.result.json
    *.snapshot.json

tests/ValidatedWorld.TestKit/
  reusable process runner, temporary workspace, scenario, and assertion helpers

tests/ValidatedWorld.EndToEnd.Tests/
  black-box CLI scenario tests using the same sample catalog

docs/qa/
  wpN-agent-walkthrough.md

artifacts/qa/
  ignored databases and output from local exploratory walkthroughs
```

The exact files grow with the work packages; do not create placeholders merely
to satisfy the tree. `samples/TechnicalProject` is the single reviewed scenario
source. The CLI embeds or publishes those same assets for `vw sample create`;
tests must detect drift between source assets and the shipped sample catalog.

`baseline.snapshot.json` is a valid populated revision-zero logical snapshot.
Initializing it creates a usable starting project without inventing fake commit
history. After transaction/commit support exists, transaction recipes create
later revisions through the public authoring pipeline.

Add new immutable variants for new behavior and regressions. Do not repeatedly
rewrite one baseline to cover unrelated cases, and do not regenerate expected
results using the same code path without independent assertions.

## 4. The tests directory is a verification laboratory

`tests/` is not limited to small unit tests. It contains:

- unit and property tests;
- reusable TestKit code;
- SQLite integration, migration, and fault tests;
- black-box CLI process tests;
- realistic scenario goals and expected structured outcomes;
- security and corrupted-input fixtures where generation is impractical;
- performance corpus generators and recorded budgets; and
- application-owned fixture-generation tests.

Tests create temporary databases through public Application/CLI operations. A
test may retain its workspace path on failure for diagnosis when explicitly
requested; normal runs clean up. A human or agent wanting to explore manually
uses `vw sample create` to make a persistent ignored copy under `artifacts/qa`
rather than taking ownership of a test runner's transient file.

A binary `.vw.db` is checked in only when its byte-level malformed state is the
subject of a test and cannot reasonably be generated. It must live under
`tests/`, document provenance and expected schema/application version, and have a
regeneration procedure where possible.

## 5. Four levels of evidence

### 5.1 Unit and property evidence

Proves local contracts, invalid inputs, canonical value behavior, ordering, and
graph properties.

### 5.2 Integration evidence

Proves application/persistence boundaries, migrations, bundled native-provider
startup, mapping, integrity, transactions, rollback, backup, and replay.

### 5.3 Scripted end-to-end evidence

Runs the compiled CLI as a separate process against databases generated by the
app. It asserts exit codes, stdout/stderr boundaries, logical data, diagnostics,
impact paths, review obligations, rollback/commit state, hashes, and unrelated
exclusions appropriate to the implemented work package.

### 5.4 Agent QA evidence

The implementing agent performs a separated QA-user walkthrough through public
documentation and commands, records pass/fail and usability findings in
`docs/qa/wpN-agent-walkthrough.md`, and reports them to the human. This is
exploratory product evidence, not a replacement for automated assertions.

QA goals include machine-checkable expected semantic facts plus an observer
checklist. The agent explicitly marks the walkthrough pass or fail and explains
the result. Subjective observations such as confusing terminology are valuable
product evidence, but they are never the sole oracle for data integrity,
transaction safety, or deterministic validation.

Deterministic findings become regression variants and scripted tests. If the
agent cannot complete the goal, requires source knowledge, misunderstands a
success result, or makes a wrong semantic change, the current task fails. If the
workflow technically works but feels confusing, burdensome, or unhelpful, report
the exact concern and recommendation; stop for human direction when it calls the
product direction into question.

## 6. Packaging acceptance

WP3 must prove the no-external-install promise with automated smoke tests:

1. Restore/publish the CLI using only solution and NuGet dependencies.
2. Run the published CLI from a clean temporary directory.
3. Report the bundled SQLite library version.
4. Create a database through `vw init` and another through `vw sample create`.
5. Open, verify, query, back up, and reopen the backup without `sqlite3`, Docker,
   a server, or a system SQLite dependency.
6. Assert the expected native library is present in published output for each
   declared supported runtime identifier.

Actual execution is required on every platform the project claims to support.
Cross-publish asset inspection alone is not a runtime claim. Until a platform is
exercised, report it as unverified rather than implying portability.

## 7. Planned Gate B AI-review evidence

AI semantic review adds exploratory intelligence, not a deterministic oracle.
Its normal test suite must still be self-contained:

- use a deterministic fake client for concern and no-concern workflows;
- use scripted HTTP handlers for OpenAI mapping, refusal, truncation, timeout,
  cancellation, and malformed responses;
- prove every failure results in exactly one attempted request and zero automatic
  retries;
- prove the request preview, cache, and freshness hashes cover every material
  non-secret input;
- prove one request includes the whole transaction, every disjoint selected
  dependency/impact chain, and each selected node's singular upward lineage;
- prove ancestor-as-context never pulls an unselected sibling into scope, while
  directly changing the purpose root selects every descendant;
- prove that malformed or stale results cannot satisfy policy or mutate canon;
- scan structured output, logs, diagnostics, stored rows, and exception text for
  secret leakage; and
- require no API key or live network for restore, build, test, packaging, or
  ordinary black-box QA.

Unit, integration, and ordinary end-to-end tests ignore
`VW_AIREVIEW__LIVETESTS` and always remain offline. A separately named Gate B
live smoke/evaluation command checks that it is `true` before considering a
network call; credentials and explicit human live-call authorization are still
required. The Gate B suite separately proves project-policy behavior:
`disabled`, audited transaction `optional` skip, and non-bypassable `required`.

The tracked TechnicalProject corpus gains reviewed Gate B variants with known
omitted links, stale values, terminology conflicts, missing qualifications,
insufficient-context cases, and unrelated distractors. Expected issue IDs and
evidence form the evaluation reference set; they are source assets, not
prepopulated databases.

A real-provider evaluation is a separately invoked, single-request experiment
using only OpenAI `gpt-5.6-terra` with medium reasoning. An implementation agent
must see the exact human secret-readiness attestation before coding that path; a
live run additionally requires the exact per-run authorization. The app first
writes the complete sanitized request preview for inspection. Record model,
prompt/profile/schema versions, corpus revision, aggregate precision/recall,
false-positive burden, cost, latency, and sanitized findings. Never record an API
key or private chain-of-thought. Never retry a live failure automatically. A live
result cannot replace the deterministic fake/scripted acceptance suite. The
timeout setting is simple operational configuration and needs no artificial unit
test.

Gate A scenario QA also measures graph-entry burden. The TechnicalProject source
uses focused batches and clusters; golden expansion tests prove inherited scope
parents become explicit edges and that no semantic dependency edge is guessed.
The black-box agent must be able to add several nodes under one focus without
repeating identical parent data or directly editing SQLite.

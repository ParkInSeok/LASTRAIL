# Disposable local admission and support selection

This is implemented separately from the public game API. `createCe03LocalHost`
in `apps/game-api/src/ce03-local-host.ts` constructs an independently authenticated
Hono app bound to one exact disposable database object and an explicit owner
allowlist. Public `/v1/runs/start` is not mounted here and its production gate is
unchanged. Only the trusted composition can provide consumer artifacts/verifier.

The launcher `scripts/validation/serve-ce03-local-host.ts` creates a fresh in-memory
fixture account and listens only on an OS-assigned `127.0.0.1` port. Its local
schema is installed explicitly, outside production migrations. It never installs
an accepting verifier. Default mode writes an eight-hour local bootstrap token
to `audit-results/ce03-local-host/connection.private.json`, never to logs. Account
and DB lifetime is the process; this launcher is not crash-durable storage and
must not be treated as a resumable real-account service. Existing transaction
and recovery behavior is tested against the same DB across host requests.

From `server`:

```text
tsx --tsconfig tsconfig.base.json scripts/validation/serve-ce03-local-host.ts --self-check
tsx --tsconfig tsconfig.base.json scripts/validation/serve-ce03-local-host.ts
```

The self-check starts real HTTP, verifies unauthorized rejection, explicit support
save/prepare/quote and missing-evidence refusal, exports artifacts and closes.
It issues no combat seed/run and does not debit fuel. Normal mode leaves the
host available for independent integration, with actual admission still closed
until a real provider is composed in code. No HTTP body may supply a verifier,
effect, seed, profile or ready flag.

## Support selection

`GET /__local/ce03/support-loadout` returns `{selection,catalog}`. Missing selection
is revision0/empty slots, not an automatically accepted former fixed trio.
Catalog scope is `LOCAL_SELECTION_ONLY`; `executionAvailable` distinguishes the
existing three effects from unresolved roles. No permanent unlock/economy rules
are assigned.

`POST /__local/ce03/support-loadout`, with the original `Idempotency-Key`:

```json
{"rulesVersion":"ce03-support-selection-v1","catalogVersion":"ce03-local-support-roles-v1","expectedRevision":0,"slots":[{"slot":0,"skillId":"barrier"},{"slot":1,"skillId":"support_fire"},{"slot":2,"skillId":"emergency_repair"}]}
```

Exactly three distinct, contiguous, explicitly ordered slots are required.
Saved revisions use CAS; active runs prevent edits. Same key/body replays the
original saved selection even after a newer save; changed body/order conflicts.
Owner, catalog/version, unknown IDs, duplicates, missing slots and client fields
are checked. Selection write, immutable receipt and guards share one SQL batch.

The three actual ability IDs retain their source identities:
`support_fire`, `barrier`, `emergency_repair`. The following seven are **new local
storage keys** for accepted roles, not previously existing Unity ability IDs:

| Local key | Accepted role | Execution |
|---|---|---|
| `ce03.role.suppressive_barrage` | 제압 포격 | Unavailable |
| `ce03.role.medical_support` | 의료 지원 | Unavailable |
| `ce03.role.smoke` | 연막 전개 | Unavailable |
| `ce03.role.weakness_scan` | 약점 스캔 | Unavailable |
| `ce03.role.firepower_boost` | 화력 증폭 | Unavailable |
| `ce03.role.electronic_disruption` | 전자기 교란 | Unavailable |
| `ce03.role.emergency_protection` | 긴급 방호 | Unavailable |

All ten can be saved in this local selection registry. A selected unresolved
effect causes `ce03_support_effects_unavailable` on prepare/start, rather than
substituting an old effect or zero-valued coefficient.

## Prepare and command evidence

The new host accepts `POST /__local/ce03/prepare`:

```json
{"selection":{"operationId":"opn_01_last_departure","risk":"I","expectedFormationRevision":1,"expectedLoadoutRevision":1,"expectedUltimateRevision":1},"expectedSupportRevision":1}
```

Response fields are `ready:false`, `status:MISSING_EXECUTION_EVIDENCE`,
`ownedSelection`, `carriageOwnership`, `draft`, `commandContract`, `prepareSha256`.
The old standalone producer's request remains unchanged. This host's explicit
wrapper is a new contract. Prepare hash covers the complete four data objects;
it pins selected owner facts/revisions/order and command semantics. The draft's
train manual order follows saved IDs; effects come only from the pinned source.

`commandContract.version` is `ce03-command-contract-v1`; `support` contains the
saved rules/catalog/revision/slots; `supportChannel` is `SHARED_TRAIN_MANUAL_CAST`.
`leader` is explicitly configured as:

```json
{"version":"basic-hp-hit-v1","scope":"LOCAL_QA_OPT_IN","perHit":4,"overrides":"ULTIMATE_FIRST_READY_AND_COOLDOWN"}
```

This is the existing Unity opt-in QA behavior, not approved release balance.
Observed source: living allied operator basic HP-damage event adds4, cap100;
enemy/train/device/skill/zero-HP-loss excluded; valid ultimate cast consumes100;
down retains charge. Consumer execution/checkpoint rules and event ordering must
be verified against its exact code, including interrupted casts and restore.
The wrapper overrides the historical ultimate timing fields; a consumer must
not use the nested r4 base alone to execute this r5 contract.

New `ce03-consumer-runner-report-v2` requires the previous report fields plus
`commandContractSha256` and `SUPPORT_SELECTION`/`LEADER_CHARGE` coverage in
addition to all eight full-consumer categories. The exact command JSON/hash is
passed to the trusted verifier. An old report, partial test count, changed
selection revision, different gauge config or accepting fixture is not actual
execution evidence. Missing artifacts/verifier and failed verification reject
before generating a combat seed or changing run/fuel state.

## Quote, accepted start and recovery

The existing authenticated `POST /v1/operations/preview` is also available inside
this isolated host. Its body is the existing departure selection request and it
uses a separate original preview key. It does not imply combat readiness.

`POST /__local/ce03/start`, with a separate original `Idempotency-Key`:

```json
{"operationPreviewId":"<actual quote ID>","expectedSupportRevision":1,"prepareSha256":"<exact prepare hash>"}
```

On actual verification, accepted response includes existing run metadata and:

- `scope:DISPOSABLE_LOCAL`, `accepted:true`, `startStatus:STARTED`.
- `combat.schemaVersion:ce03-accepted-combat-v1`.
- `combat.profile`: `ce03-isolated-command-profile-r5` containing the historical
  closed `baseProfile` plus the new `commandContract`.
- `combat.profileSha256`, `combat.contentManifest`, `combat.contentSha256`.
- `combat.seed`: Web Crypto uint32 little-endian original-run seed descriptor.
- `combat.snapshot` and `combat.snapshotSha256`: owner/run/quote, prepare/P/C
  hashes, seed, original owner/carriage/command facts and verified evidence.

Hashes exclude their own fields: P hashes profile, C hashes content manifest,
S hashes snapshot. The content manifest retains its closed r4 locator shape and
pins the entire r5 profile hash. Snapshot is persisted with the full original
response through existing `startDeparture`. Its existing owner/quote/open-run/
balance/fuel guards, debit, run, snapshot and idempotency writes plus new support
guard/immutable local receipt commit together or roll back. Candidate seeds from
failed transactions are never published; original committed-key replay does
not regenerate a seed or rerun consumer verification.

Original receipt lookup precedes current owner revisions and provider readiness.
The receipt is bound to owner and exact start request hash. Receipt bytes/hash,
run ownership and stored snapshot must agree. Conflicting original keys reject.
Concurrent completion, including between initial receipt and idempotency reads,
and lost commit acknowledgement recover the committed original response.

`POST /__local/ce03/start-result` takes the same start body plus
`startIdempotencyKey` in the body and returns `lookupStatus`, `startResponse`,
`newAttemptAuthorized:false`. It never creates a new run. Existing
`/v1/runs/:runId/lifecycle`, finish, reward-preview and explicit claim routes are
forwarded only for the authenticated owner's locally admitted run. Reads do not
grant rewards, and no ordinary START route is forwarded.

## Evidence limits

`audit-results/ce03-local-host/prepare-v5.json` and `verification.json` come from
the real loopback self-check with no installed verifier. The separately named
`synthetic-exchanges/<runId>.json` comes from the integration test's synthetic
verifier and is request/response wiring evidence only. Its flags explicitly say
no actual consumer verification/battle. Do not use it to enter Unity combat or
claim full conformance. Real provider, complete consumer support for this r5
wrapper, crash-durable host storage and actual local battle QA remain separate
integration work.

Verification completed 2026-09-12: related37PASS; full90files612testsPASS23.93s;
game-api/async-worker TypeScript and full source ESLint PASS. The original-key
two-read race was reproduced RED and fixed with a passing regression. A recreated
host over the same DB also recovers the exact original response without an
installed provider. Independent bounded review found no remaining high-impact
issue after the race fix. Real loopback self-check passed401 unauthorized,
200 support-save,409 missing consumer evidence, zero runs and fuel30 unchanged.

## Executed runner provider adapter

`scripts/local/ce03-executed-runner.ts` now provides
`executeCe03ConsumerRunner(options)` for a trusted Node composition. It is not
automatically installed in the launcher. Options are an absolute reviewed
`executable`, `cwd`, `sourceRoot`, a complete explicit `sourceFiles` relative-path
list, `outputRoot`, `prepare:{draft,commandContract}`, `buildArgs(paths)` and an
optional bounded timeout. HTTP clients cannot supply these options.

The helper constructs new `outputDirectory`, `sourceBundlePath`, and `preparePath`
for each process. `buildArgs` receives those three paths. Unity's actual entry
method and flags must be supplied after that runner is implemented/reviewed;
there is no guessed executable or accepting fallback. Execution uses no shell
and hides its window. A failed launch, nonzero exit or timeout rejects.

The source bundle uses canonical JSON:

```text
{schemaVersion:"ce03-source-bundle-v1",files:[{path,sha256,content},...]}
```

Paths are ordinal sorted project-relative forward-slash paths. Duplicate paths,
parent traversal, symlink escape, invalid UTF8 and oversized bundles reject.
Content preserves exact UTF8 text including original line endings/BOM. The
explicit list must include the runner, engine, commands, adapter, checkpoint,
journal and relevant dependencies. Source inclusion/completeness and the actual
runner assertions require review; spawning any arbitrary program is not proof
of conformance. The runner must execute the corresponding current compiled code.

Runner output in the supplied new directory:

- `algorithm-source.json`: exact bytes of `sourceBundlePath`.
- `checkpoint-schema.json`: actual checkpoint implementation/schema artifact.
- `vectors.json`: actual executed cases with unique IDs/coverage/inputs/expectations.
- `runner-report-v2.json`: strict canonical report emitted only after the actual
  required full run succeeds, never copied from an earlier run.

On zero exit, the helper checks source and executable hashes again, requires all
four new outputs and exact source-bundle equality, then returns immutable
`artifacts`, `verifier` and an execution record. The verifier compares the exact
artifact strings/digests and prepare draft/command identities and rechecks source
and executable identity on every verification. A source edit invalidates it.
Compose these artifacts/verifier into `createCe03LocalHost` over the same DB;
the host still separately enforces report-v2/full coverage and original prepare
identity. A saved arbitrary JSON report cannot construct this verifier.

Current proof is **synthetic external process plumbing only**: actual Unity
`Ce03ConsumerEvidenceRunner.Run` and complete report-v2 are still being authored
by Unity. No real provider has been executed or installed, and this helper's
tests do not justify enabling battle entry.

Adapter verification: missing-module RED then focused process/host14PASS;
full91files619testsPASS22.54s after splitting five sequential process scenarios
into independent cases (no timeout increase). Node/Worker type check and targeted
ESLint PASS; independent bounded review found no high-impact bypass. Cases cover
actual synthetic subprocess exit failure/timeout, missing output, changed source
during/after execution, changed bundle/draft/command evidence and invalid paths.
The former fixed synthetic-exchange output was regenerated by regression runs;
new runs now write unique run-ID files to preserve previously cited samples.

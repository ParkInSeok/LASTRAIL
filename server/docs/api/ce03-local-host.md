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
tsx --tsconfig tsconfig.base.json scripts/validation/serve-ce03-local-host.ts --self-check --presentation-audit <actual-export.json> --unity-project <project-root> --ugui-package-root <resolved-com.unity.ugui-root>
tsx --tsconfig tsconfig.base.json scripts/validation/serve-ce03-local-host.ts --presentation-audit <actual-export.json> --unity-project <project-root> --ugui-package-root <resolved-com.unity.ugui-root>
```

The actual Approved view is the launcher's default contract. All three source
arguments are required. Historical regression must explicitly use `--legacy-r4`
(optionally with `--self-check`) and cannot combine presentation arguments.
Self-check exports go into a fresh `self-check-*` directory, preserving earlier
cited artifacts. The launcher checks actual raw/meta/GUID/source identities before
opening its listener. This is preparation evidence, not executed consumer proof.

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
list, `outputRoot`, the complete `prepare` context below, `buildArgs(paths)` and an
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
source count limit is 1024 (increased from 512 after Unity reported 548 runtime
sources in its single assembly). The existing total canonical bundle limit of
8,000,000 characters and per-source 8,000,000-byte limit are unchanged. A larger
file count does not authorize omitting dependencies or exceeding size bounds.
The explicit list must include the runner, engine, commands, adapter, checkpoint,
journal and relevant dependencies. Source inclusion/completeness and the actual
runner assertions require review; spawning any arbitrary program is not proof
of conformance. The runner must execute the corresponding current compiled code.

Runner output in the supplied new directory:

- `algorithm-source.json`: exact bytes of `sourceBundlePath`.
- `checkpoint-schema.json`: actual checkpoint implementation/schema artifact.
- `vectors.json`: actual executed cases with unique IDs/coverage/inputs/expectations.
- `runner-report-v3.json` for the Approved view; `runner-report-v2.json` only for
  historical command inputs without presentation. Strict canonical report emitted
  after the actual required full run succeeds, never copied from an earlier run.

On zero exit, the helper checks source and executable hashes again, requires all
four new outputs and exact source-bundle equality, then returns immutable
`artifacts`, `verifier` and an execution record. The verifier compares the exact
artifact strings/digests and prepare draft/command identities and rechecks source
and executable identity on every verification. A source edit invalidates it.
Compose these artifacts/verifier into `createCe03LocalHost` over the same DB;
the host still separately enforces report-v2/full coverage and original prepare
identity. A saved arbitrary JSON report cannot construct this verifier.

Current proof is **synthetic external process plumbing only**: actual Unity
`Ce03ConsumerEvidenceRunner.Run` and complete report-v3 are still being authored
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

## Approved view: profile-r5 / content-r6 / report-v3

Historical closed r4 profile/content/binding schemas and old receipts are unchanged.
The r5 profile wrapper and command/leader semantics are unchanged. The new content
header is `ce03-isolated-presentation-content-r6`, with exactly:

```text
{schemaVersion,version,profileSha256,resolver,legacyBindings,presentationManifest}
```

`version` is `ce03.approved-presentation.20260912.001`; `resolver` remains
`{mode:"UNITY_EDITOR_ASSET_DATABASE",playerBuildAllowed:false}`. `legacyBindings`
contains the original 226 Sprite/AnimationClip/AnimatorController bindings used by
the base profile. These are a different inventory from the new 356 typed rows;
the two counts must not be presented as a before/after runtime asset count.

`presentationManifest` uses `ce03-presentation-manifest-v1` and renderer
`approved-combat-view-v1`. Its closed fields are `schemaVersion`, `rendererVersion`,
`roles`, `bindings`, `sourceFiles`. Exact executable schemas are in
`apps/game-api/src/ce03-presentation-manifest.ts`.

- `roles`: configuration (CombatPresentationAssets), font (TMP_FontAsset),
  console/icons/map/trainExterior/interior/defaultTurret/turretRig/buggy/bike/hit/
  skill/shield (Texture2D), frame/primary/secondary (Sprite), six actorAtlases
  `{actorId,bindingId}` and 160 frames `{key,bindingId}`. Lists sort by ordinal
  actorId/key after mapping actual serialized array indices. Same asset may serve
  multiple roles, e.g. frame and secondary.
- Each binding retains the original locator/raw/meta/dependencies structure and
  extends the closed type set to Texture2D, MonoScript, Material, Shader,
  CombatPresentationAssets, Font and TMP_FontAsset. Decimal signed int64 file IDs
  remain strings. Actual importerVersion is `null` because the exporter did not
  provide one. `loadVerified:true` is owner-generated Editor evidence, not a claim
  that this Node projection executed AssetDatabase.
- Binding IDs are `binding.render.<guid>.<localFileId>`, sorted ordinal. Dependencies
  include all typed rows of referenced physical files and sibling subassets.
  Duplicate locators, missing dependencies, unsupported types and malformed hashes
  fail. The projection also verifies role references and array indices against the
  raw hash-pinned approved.asset YAML and rejects extra/unresolved roles.
- Four source identities are mandatory: approvedCombatView, combatHudWidgets,
  combatPresentationAssets, combatWindowView. The process adapter requires their
  exact `Assets/_LASTRAIL/Scripts/Battle/<Class>.cs` paths and raw hashes in the
  actual source bundle, and checks current source bytes again after execution and
  on verification.

`scripts/local/ce03-presentation-projection.ts` resolves only configured project
Assets and explicit package roots. It checks actual raw/meta/GUID for 188 physical
files and the four renderer sources. The actual exported inventory has 356 unique
GUID/fileID rows: Texture2D183, Sprite165, MonoScript2, Material2, Shader1,
CombatPresentationAssets1, Font1, TMP_FontAsset1. All exported rows are preserved,
including the TMP Editor icon inventory entry. That icon is outside the root's
runtime dependency reachability. No claim that all 356 rows are used by the live
renderer is implied; the actual consumer must verify loading and typed roles.
Map/skin/turret appearance references do not establish account ownership.

The complete process input is:

```text
{ownedSelection,carriageOwnership,draft,commandContract,presentationManifest,prepareSha256}
```

The hash is SHA256 of canonical JSON of the first five fields; exclude ready,
status and prepareSha256 itself. Historical preparation uses the first four
fields without presentationManifest. The runner must consume the exact owner,
carriage revision/type and selection, never regenerate a fixture account.
`preparePath` preserves all these fields, and a hash mismatch rejects before
execution. The host repeats preparation against its current authenticated DB and
checks the original hash before verifying evidence or creating a seed.

Report-v3 retains every v2 field, changes `schemaVersion` to
`ce03-consumer-runner-report-v3`, and requires `presentationManifestSha256` and
`prepareSha256`. Manifest hash is canonical parsed manifest JSON, not pretty-file
SHA. Required coverage is the complete list:

```text
STRICT_PROFILE FULL_EFFECTS MANUAL_COMMANDS FIFTEEN_WAVES CHECKPOINT_RESTORE
DETERMINISM OWNER_ISOLATION JOURNAL_RECOVERY SUPPORT_SELECTION LEADER_CHARGE
PRESENTATION_BINDINGS
```

Each vector ID must have one matching PASS result/coverage; passed count must
match, failed/skipped must be zero, all 11 categories must be executed. Canonical
report bytes, draft/command/source/checkpoint/vector hashes and both new hashes
must match the trusted actual process verifier request. Old v2, partial coverage,
changed manifest or owner-context hashes reject before debit/run creation.
Shape/hash checks and synthetic tests never substitute for actual test execution.

First actual r6 HTTP preparation: `audit-results/ce03-local-host/self-check-KVf5I1/`
contains prepare-r6.json (raw SHA
`04D2E9D3D21203973B5FF53910AC7F261CA2B91ABFE792C48B9A706B4197E24E`),
presentation-manifest.json (raw SHA
`5B23225BF4743E4AFA0AA293C24BF69F315BC8EC50E70111C80D1AB4DBF69135`), and
verification.json (raw SHA
`399319DE032EB7F247DC86ABEB1824282B4F3639F42E5B596979253C5A194BFC`).
Exporter raw SHA is
`c5dbcd758a4736c9136f92d320caf86d48fd623f9b73c83723a1e80c744081ea`.
This evidence is authenticated disposable preparation with 188 physical and four
source identities checked, 401 unauthenticated, START409 missing execution
artifacts, zero runs, no seed and fuel30 unchanged. Unity full r5/r6 consumer,
actual 11-category runner/provider and live battle evidence remain integration
work. No production deployment, remote DB change or public gate change occurred.

Final verification: 93 files / 640 tests PASS (17.51s), game-api and async-worker
types PASS, Node/Worker types including runner/projection/HTTP launcher PASS, full
ESLint PASS, independent bounded review found no high-impact issue. Regression
tests cover actual export corruptions, YAML role swaps, array index changes,
unknown type/overflow fileID/missing source/dependency, stale owner/presentation
attestation and missing renderer sources in the actual process bundle. The latter
was reproduced RED before enforcing source matching. Tests do not execute Unity.
Final actual HTTP reproduction is separately preserved in `self-check-AEk3o8/`:
prepare-r6.json raw SHA
`168D7C8122B9449BD3F4706A7E731E367B10C657F9877CC0B07E489D1AD564F9`.
Manifest and verification file hashes equal the first reproduction above. The
prepare's JSON property order changed with typed response validation; its canonical
prepareSha256 is unchanged. Explicit historical `--legacy-r4 --self-check` also
passed with no provider, zero runs and fuel30 unchanged.

## First actual runner execution: review remains closed

`scripts/validation/verify-ce03-runner-closed.ts` preserves the first frozen
diagnostic packet, not an accepting provider launcher. It has no argument override
and pins source bundle SHA
`be341ad1bfe76ebe4828cf0a7b523c305f69407b12f1bd188215b57359327546`.
It starts a fresh authenticated disposable HTTP host, saves support and reads its
full prepare, executes Unity through `executeCe03ConsumerRunner`, then confirms
the same host still refuses START without a provider. It never submits the
runner's test-seed victory as an actual account result. Future source changes
intentionally invalidate this historical diagnostic; review a new packet for the
next integration unit rather than silently replacing its pin.

2026-09-12 actual execution: 554 sources, bundle 2,393,158 bytes, Unity 6000.3.22f1
executable SHA `36bafe49cfd65d4c91889167ca051f755195621a8f734392ad33ccccb5c1bba7`,
20.409 seconds, exit0, runner-reported11PASS. Artifacts are under
`audit-results/ce03-actual-runner/closed-review-ivKSvn/ce03-execution-KiWlUT/output`.
Verification file in the parent `closed-review-ivKSvn` has raw SHA
`29158BD00712A0FC14EB7D0B2AF36FA7126D7B9712AEAF744361BF373527CCD3`;
report canonical/raw SHA is
`fb2ca3d301285f102ca12b5c0d4e328125f1c24d3b65281c19d8d24927689c29`.
This is actual process execution, separately from the earlier synthetic processes.

Independent review still rejects treating that report as full admission proof:
the frozen Unity `Ce03AcceptedCombat` erroneously requires r6 content.version to
equal baseProfile.profileId, and its JOURNAL_RECOVERY case tests support saving
without testing START receipt/key/seed recovery. The runner does execute effect
value, waves, determinism, checkpoint and asset checks, but its accepted-envelope
path is absent. Both findings were sent to Unity, which is adding explicit r6
validation and START/accepted-consumer coverage. ProviderInstalled=false,
actualAccountRun=false, START409 missing artifacts, zero runs and fuel30 remain.

`ce03-local-host.test.ts` also exports clearly labeled
`synthetic-r6-exchanges/<runId>.json` fixtures containing prepare, start request,
original key and response. These use synthetic locators and verification, and are
only for parser/journal transport tests; never load them as actual battle content
or consume them as admission. The original sample is
`run_75f2337a-b608-4f5e-8c99-d53930107567.json`, raw SHA
`533D29EBE07DD2A8C1A3138D1D5031FCD51F6363E852417595E2E33FD9589E26`.

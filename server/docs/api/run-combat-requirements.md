# Run combat requirements and original-run recovery

This follow-up supersedes the public paid-start availability at commit
`4ced3496418070fc018e32d16f0315297c506aa3`. That commit verified departure admission,
debit and snapshot persistence (492 tests), not Unity combat loading. Actual
`RunStartResponseJsonAdapter` and `RunStartSnapshotBuilder` require additional
authoritative content/stat inputs that were not supplied. **New S11 paid starts
now return 409 `combat_not_ready` without mutation.** Already committed starts
replay their original response before the new gate. The gate itself needs no
migration; the subsequent [CE01 choice contract](ultimate-selection.md) requires
migration0031 and advances diagnostics to v2. See the
[CE02 content/reward review](combat-content-resolver-review.md) for current fields.

## Field responsibility and evidence

Inspected sources on 2026-09-10:

- Server `apps/game-api/src/index.ts`, `departure.ts`, migrations0003/0005/0014,
  and `packages/balance/src/index.ts`.
- Shared source read only:
  `E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/Run/RunStartResponseJsonAdapter.cs`,
  `RunStartSnapshotBuilder.cs`, `RunSnapshot.cs` and scoped Scripts/content searches.
- Canonical read only: `docs/design/train-combat-test-profile-v1.md` and
  `train-loadout-speed-detail-v1.md` under the OneDrive LASTRAIL project.

| Field | Owner / actual source | Type and version boundary | Missing behavior / remaining decision |
| --- | --- | --- | --- |
| runId, serverStartedAt | Server committed run/snapshot | string, ISO UTC; immutable original response | Preview has no run/start time; never use local clock as accepted start |
| operationId / encounter refs | Existing server six story IDs plus raid and existing launch package metadata | Existing strings, `act1.launch.content.v1`; version does not prove loader support | Unity must resolve this exact operation/content/package, not a fabricated fallback |
| balanceVersion | Server active balance at quote, pinned in run | string numeric version | Missing cost/reward policy remains 503; no inferred default |
| firstClearEligible | Server `account_first_clears` keyed account/operation/risk | boolean for new previews/admission; guarded and receipt-pinned | Missing row means potentially eligible; final reward still requires successful completion and finish validation. Old response missing this field stays missing |
| riskLevel / risk | Server risk tier plus explicit projection | Existing root `risk` and run `riskLevel` stay Roman strings; new combatContract.riskLevel is int1..5 | Unity adapter must explicitly map/read the versioned int. Unknown source tier reports null |
| allowedOperatorSlots | Existing server maximum count and actual S03 selection | Root remains number5; combatContract.operatorSlotCount=5; admission.formation.operators contains numeric slot/ID assignments | Legacy string[] semantic contract is unproven. Do not turn5 into invented slot-name strings |
| enemyFactionId | Content owner; no server operation/content-version binding found | null in requirements | Need approved operation+contentVersion→actual faction ID and resolver identity |
| wavePlanId | Content owner; `run_wave_plans` schema exists, no producer/catalog binding found | null in requirements | Need actual wave-plan ID/version/hash and ordinary/raid applicability; table existence is not a plan |
| rewardTableId | Server owns rewards via balance runRewards.operations[operationId]; no separate ID mapping exists | null in requirements; quote balanceVersion+operationId are the real current key | Agree whether Unity accepts that structured reward source or supply an approved actual table ID mapping; never rename operationId into a guessed table ID |
| seed | Server combat protocol owner; Unity only consumes uint/RngSeed | null; uint32 consumer alone is not an issuance protocol | Need battle-only purpose, RNG algorithm/version, generation/lifetime and replay rules. No generated dummy/deterministic constant; no reuse of gacha RNG or disclosure of its internals |
| selected operators/carriages | Server S03/S04 actual ownership and frozen selection | admission formation/loadout, revisions and persisted growth facts | Already provided; no default roster or unowned vehicle substitution |
| ultimateOperatorId | Server CE01 validated selection among owned active five | string or null, with ultimateSelectionRevision | Explicit selection POST/GET and departure-v2 pin; null blocks start, no Kyle/first-operator fallback |
| engineOutput | Balance/content profile owner; Unity TrainCoreCatalog has speed inputs, but canonical profile is explicitly a proposal | null | Canonical `train-combat-test-profile-v1` says RECOMMENDED TEST PROFILE, not production balance. Need approved profile version and level mapping; engine output is not attack |
| effective combat stats | Approved balance/content formulas and Unity resolver | No profile binding yet; persisted growth != resolved stats | Need approved profile/formula version, rounding and loader input contract. No copying candidate HP/ATK/armor values |
| catalog support | Existing client-config/version policy supplies admission and catalog identity; it does not bind these combat fields | requiredContent lists exact existing refs; bindingVersion/catalogHash remain null here | Unity compares/downloads/resolves via existing version flow. A valid catalog admission or client ready=true does not prove a missing combat binding |

No source was found authorizing a concrete faction/wave/reward ID or a battle seed
algorithm. References to seed in gacha logging/commitment policy are not a combat
seed contract. The canonical train profile explicitly prohibits activation by
inference; none of its numeric proposals are installed by this work.

## Preview additions

`POST /v1/operations/preview` remains a strict server quote request. It now returns
and stores `firstClearEligible:boolean` and this diagnostic requirements object:

```ts
type CombatContract = {
  version: "run-combat-requirements-v2";
  status: "BLOCKED";
  canStart: false;
  blockingReasons: string[];
  riskTier: "I" | "II" | "III" | "IV" | "V" | null;
  riskLevel: 1 | 2 | 3 | 4 | 5 | null;
  operatorSlotCount: 5 | null;
  firstClearEligible: boolean | null;
  enemyFactionId: null; wavePlanId: null; rewardTableId: null;
  seed: null; engineOutput: null; ultimateOperatorId: string | null;
  ultimateSelectionRevision: number | null;
  requiredContent: {
    operationId: string | null; contentVersion: string | null;
    startAugmentPackageRef: string | null; startTacticPackageRef: string | null;
    encounterType: string | null; raidId: string | null; bossId: string | null;
    bossPhaseRefs: string[];
    bindingVersion: null; catalogHash: null;
  };
};
```

The six unconditional blocking reasons are `enemy_faction_binding_missing`,
`wave_plan_binding_missing`, `reward_table_binding_missing`,
`combat_seed_contract_missing`, `engine_output_profile_unapproved`,
`effective_stats_profile_unapproved`. `ultimate_selection_required` additionally
appears when there is no valid CE01 admission pin.
These describe absent approved capabilities, not a transient network failure.
Malformed stored source returns 503 `combat_requirement_source_invalid`; database
failure remains a server error. It is not converted into a fake missing catalog,
404, or successful empty response.

First-clear state is rechecked in the preview transaction; a concurrent clear
causes re-read/retry. The exact response is pinned in the immutable preview receipt.
Same-key replay can show the previous true value after a later clear; that is an
old quote, not a new reward entitlement. A new key gets current truth. Retained
atomic start-core logic pins/rechecks this value as well; its public route is gated.

## New-start gate and client integration

The HTTP start route performs auth/idempotency replay first. For any uncommitted
S11 admission-version preview it reconstructs current requirements from server
capabilities and returns the existing rejected-start shape plus combatContract:

```json
{"accepted":false,"startStatus":"REJECTED","errorCode":"combat_not_ready"}
```

It does not trust a saved `combatContract.canStart`, request property, client flag
or arbitrary catalog ID as approval. No cost debit, ledger, preview consumption,
run, new start receipt, refund or state transition is written. This also covers
unconsumed S11 previews produced before this follow-up. Unknown/incomplete battle
inputs cannot become playable by editing a boolean.

Unity should inspect preview.combatContract before starting, display the explicit
unavailable requirements, and use the existing client-config/content download flow
to resolve requiredContent. Enabling READY requires a future reviewed server
binding contract and Unity loader agreement, including exact content version/hash
and a pre-start proof bound to that quote. No ready-ack endpoint or client boolean
bypass is implemented now because approved bindings do not yet exist.

Legacy NULL-marker metadata previews retain the preexisting no-new-debit path for
historical compatibility; they are not a supported workaround for real combat or
a new client producer. Old successful S11 and legacy receipts are replayable as
stored. The start gate does not retroactively declare old responses battle-ready.

## GET /v1/runs/:runId/start-state

If the client never received runId, use the read-only
[POST /v1/runs/start-result](run-start-result.md) with the original journal key and
previewId. Do not retransmit a start command to discover its result. NOT_FOUND
is indeterminate and does not allow a new key/start.

Bearer auth plus existing client admission; no query/body; Cache-Control:no-store.
ID uses existing safe-ID contract. Read only, account scoped:

```ts
type RecoveryResponse = {
  accepted: true;
  runId: string;
  runStatus: string; // current stored status, including completed/failed
  recoveryStatus: "AVAILABLE" | "SNAPSHOT_UNAVAILABLE";
  startResponse: Record<string, unknown> | null;
};
```

For S11 snapshots, startResponse is the original stored departureResponse exactly;
no current balance, new first-clear value, combat defaults or timestamps are added.
Legacy snapshots use the existing start replay projection. AVAILABLE means the
original response is recoverable, not that Unity has all battle inputs. Missing or
malformed snapshot is 200 SNAPSHOT_UNAVAILABLE with null startResponse. Missing or
foreign run is404 run_not_found; expired/revoked session follows existing401 auth.
Bad ID/query/body is400 contract_violation; database error is not treated as absence.

Resume the same run when its actual loader contract is supported. Never create a
second start or infer an automatic refund from recovery failure. This endpoint
does not alter run status, costs, rewards, ownership or snapshot. A retry of an
already committed start key still returns the original start response before the
combat gate; a fresh key cannot create another paid run.

## Verification scope

Six public-route SQLite tests cover requirements, zero-write rejection despite a
forged stored ready flag, first-clear receipt/commit behavior, original response
recovery/replay, foreign404/session expiry, missing snapshot and invalid source.
The prior twelve atomic departure tests now exercise the retained domain start
function directly through a test-only fixture; the public HTTP paid-start path is
intentionally blocked. Those tests remain transaction/finish compatibility evidence,
not evidence of combat support or a production gate bypass. There is no environment
switch enabling the core from HTTP. Existing legacy metadata tests remain separate.

Actual Unity combat entry remains incomplete pending the decisions in the table.
No new catalog, seed, ultimate selection, combat values, migration, deployment,
remote seed, refund or canonical write was performed.

# S11 departure admission

Local implementation requires migrations through `0030_departure_admission.sql`.
No remote deployment or catalog activation is implied. Use S03 active operator
formation and S04 `GET /v1/trains/loadout` before requesting departure.

## Existing operation identity

The ordered story IDs remain:

1. `opn_01_last_departure`
2. `opn_02_first_signal`
3. `opn_03_water_stop`
4. `opn_04_scrapstar_show`
5. `opn_05_night_boarders`
6. `opn_06_broken_line`
7. `raid_raider_king_engine`

The six story encounters use `ORDINARY_15_WAVE`; the last entry is `RAID_BOSS`.
All earlier story IDs must exist in server `cleared_operations_json` before an
entry is admitted. The first needs no clear. This neither manufactures progress
nor remaps story nodes into the four proposed ordinary categories. Their category
IDs/catalog are still missing and are not created here.

Existing Act 1 refs are used: act `act_01_wasteland_railway`, region
`region_wasteland_railway`, content `act1.launch.content.v1`, augment/tactic package
`act1_full_start_package_v1`, five operator slots. Raid uses boss
`boss_raider_king_engine` and phases `crown_chase,trophy_break,engine_stop`.
This is the existing launch metadata contract, not a new content catalog resolver.

## POST /v1/operations/preview

Bearer authentication and existing client admission apply. Response is `no-store`.
No query parameters. `Idempotency-Key` is 1–128 printable non-space ASCII chars.
Strict body (no extra fields):

```json
{
  "operationId": "opn_01_last_departure",
  "risk": "I",
  "expectedFormationRevision": 1,
  "expectedLoadoutRevision": 1
}
```

Risk is existing `I|II|III|IV|V`. Revisions are positive safe integers from the
current active formation and loadout. Five unique active operators must still be
owned. Installed train/carriages must satisfy the S04 ownership, slot and unlock
rules. Missing training-core slots stay absent; no default training investment or
extra ownership is granted. ACTIVE/FINISH_PENDING/QUARANTINED runs block issuance.

Cost comes only from active `balance_config_versions.config_json.fuel.runStartCost`,
a positive integer. No hardcoded cost fallback. The active run reward policy must
parse and contain the selected operation, progression multiplier and risk. Missing
cost/reward configuration rejects issuance, preventing paid runs that cannot finish
against that policy. Stored `fuel_state.fuel` must cover cost. No fuel initialization
or regeneration is performed; `availableFuel` is explicitly the persisted amount.

```ts
type Admission = {
  version: "departure-v1";
  formationRevision: number;
  loadoutRevision: number;
  formation: { schemaVersion: 1; operators: { slot: number; operatorId: string }[] };
  loadout: {
    rulesVersion: "train-loadout-v1"; trainId: string;
    carriages: { slot: number; carriageId: string }[];
  };
  ownedState: {
    operators: { operatorId: string; skillTraining: number; promotion: number; revision: number }[];
    trainingCoreSlots: { slot: number; operatorId: string; trainedLevel: number; revision: number }[];
    train: { trainId: string; level: number; revision: number };
    carriages: { carriageId: string; level: number; revision: number }[];
  };
  fuelCost: number;
};
type PreviewResponse = {
  accepted: true;
  operationPreviewId: string;
  operationId: string; actId: string; regionId: string;
  encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS";
  raidId: string | null; bossId: string | null; bossPhaseRefs: string[];
  allowedOperatorSlots: 5;
  startAugmentPackageRef: string; startTacticPackageRef: string; contentVersion: string;
  risk: "I" | "II" | "III" | "IV" | "V";
  expiresAt: string; // ISO UTC, server issuance + 300 seconds
  formationRevision: number; loadoutRevision: number;
  admission: Admission;
  quote: { fuelCost: number; availableFuel: number; balanceVersion: string };
};
```

`ownedState` includes only the selected entities' persisted growth facts. These are
not computed combat stats or a new equipment/effect catalog. Turrets and unsupported
owned carriages remain in S04 inventory and are excluded from the installed state.

Issuance stores a durable account-scoped receipt and preview, with transaction
checks for current selection/ownership/progress/growth/policy/fuel/open run. It
does not debit or reserve fuel. Multiple outstanding previews may coexist.
Same key/body returns the exact original response even if now expired or consumed;
changed body with that key is `409 idempotency_conflict`. Use a new key to refresh an
expired quote; retry the old key after transport uncertainty to recover its result.

## POST /v1/runs/start

Existing request remains `{ "operationPreviewId": "preview_..." }`, with Bearer
and Idempotency-Key, no query or client snapshot. S11-produced rows have the
server-only `admission_version='departure-v1'` marker. Clients cannot set it.

The server revalidates current owned squad, installation, growth, progression,
stored fuel and active policy. Admission must equal the frozen quote. Active
balance version and the hash of its complete configuration must still match;
reissue after changes. A fuel balance change alone may proceed if it still covers
the quoted cost; no dynamic price is substituted. Expiry is exclusive and is
checked again with SQLite time within the transaction.

One D1 transaction inserts the account mutation, checks all guards, consumes the
preview, deducts exact cost, writes `fuel_ledger` reason `RUN_START`, creates the run,
stores frozen snapshot and start idempotency record. No separate consume/reopen.
Any failure rolls back all of those writes. Competing previews cannot open two runs;
identical concurrent start requests recover the same committed response. Guard or
sequence races retry up to four times before returning conflict.

Accepted response is the existing run-start DTO from
`run-start-augment-package.md`, plus `admission: Admission`. The same full response
is stored and returned on replay, including after finish or later policy changes.
The run snapshot preserves existing root metadata/version fields consumed by finish
and Admin. Existing finish/reward-preview logic continues to use the pinned balance.
Do not debit fuel locally again on replay; refresh state after accepted start.

Legacy NULL-marker previews retain their historical server-authored start metadata
and cost semantics (no new debit inferred from incomplete legacy data). They now
also consume/create atomically, check expiry/open runs and never reopen after
failure. New Unity integration must obtain previews from the new producer to get
S11 ownership/cost admission. This does not retrofit old snapshots or charge old runs.

## Errors and refresh rules

Preview errors are `{accepted:false,errorCode:string}`. Start retains the existing
`startStatus:REJECTED` DTO with null run metadata and an errorCode/message.

| HTTP | Main errors |
| --- | --- |
| 400 | contract_violation; malformed/unknown operation/risk/revision; inherited S04 slot errors |
| 401 / admission status | Existing authentication/client admission errors |
| 403 | formation_operator_not_owned; train_not_owned; carriage_not_owned |
| 404 | operation_preview_not_found (foreign preview IDs also appear absent) |
| 409 | formation_required; formation_schema_unsupported; train_loadout_required; departure_open_run; operation_locked; departure_selection_changed; departure_quote_changed; insufficient_fuel; fuel_state_unavailable; operation_preview_not_startable; departure_preview_version_unsupported; departure_write_conflict; run_start_state_conflict; idempotency_conflict; inherited S04 eligibility errors |
| 503 | departure_cost_policy_unavailable; departure_reward_policy_unavailable |

Existing start key reused for another preview retains `400 contract_violation` in
the early replay path; a concurrent conflicting commit can return 409. Both mean
the key must not be reused for changed intent. Selection/quote errors require state
refresh and a fresh preview; transaction/transport uncertainty uses the original
key first. Insufficient fuel never consumes a preview. Expired stored receipts do
not extend expiry. A progressed account may need to fill newly unlocked S04 slots.

## Verification and remaining boundaries

Twelve signed-auth real SQLite tests cover quote/debit/replay, strict contract,
missing ownership/policy, revisions/gates/expiry/fuel, policy changes, identical and
competing starts, snapshot rollback, commit-time fuel/growth/policy/expiry changes,
all six IDs and raid refs, atomic legacy start, and new preview→start→finish→reward
preview with pinned policy. Existing run-start metadata tests were updated to
assert atomic consumption; the sequential ordinary/raid fixture closes its first
run before starting the second. Full test/check results are in the owner handoff.

No category IDs, combat formulas, turret compatibility, Medical unlocks, duplicate
car instances, automatic fuel refill, remote migration/seed/deploy or Unity runtime
validation are supplied by this slice.

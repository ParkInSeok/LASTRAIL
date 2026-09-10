# S04 owned train installation

Local contract, migration `0029_train_loadout.sql`. No remote migration, seed or
deployment is implied. Existing `GET /v1/account/state` is unchanged; use this
separate resource for installation. All requests require the existing Bearer
authentication/client admission, have no query parameters, and return `no-store`.

## Identity and approved scope

`account_train` supplies the owned locomotive. `account_carriages` has primary key
`(account_id, carriage_id)`: a carriage ID denotes one existing owned row for that
account, not an unlimited definition that can be installed multiple times. No
acquisition, repair, stat calculation, grant, price, refund or deletion occurs.
Locomotive is separate from carriage slots; all unlocked slots must be filled.

| Existing ID | Type | Installation condition |
| --- | --- | --- |
| car_combat | COMBAT | Owned; exactly one required |
| car_gun | GUN | Owned |
| car_cargo | CARGO | Owned and server cleared `opn_03_water_stop` |
| car_defense | DEFENSE | Owned and server cleared both water stop and `opn_05_night_boarders` |
| car_medical | MEDICAL | Unsupported: `medical_unlock_unapproved` |
| Other owned ID | null | Unsupported: `carriage_definition_unsupported` |

Read the actual `account_story_progress.cleared_operations_json`. Default two
slots, water stop three, water stop plus night boarders four. No client milestone,
highest-progress string or owned inventory substitutes for this evidence. Night
without water reports `story_progress_inconsistent`; malformed history reports
`story_progress_invalid`; neither is silently normalized or granted unlocks.

Source type limits remain COMBAT exactly 1, CARGO at most 1, GUN/MEDICAL/DEFENSE at
most 2. This does not authorize creating a second instance. An approved
instance-to-definition contract is required before any additional IDs are accepted.
Medical's proposed water-stop swap is not approved. All owned turrets remain
visible but unsupported (`turret_installation_undefined`): target slots, counts,
compatibility and unlock rules are absent. Unsupported ownership is preserved.

## GET /v1/trains/loadout

```ts
type Selection = {
  rulesVersion: "train-loadout-v1";
  trainId: string;
  carriages: { slot: number; carriageId: string }[];
};
type Response = {
  rulesVersion: "train-loadout-v1";
  train: { trainId: string; level: number; revision: number } | null;
  ownedCarriages: {
    carriageId: string; level: number; revision: number;
    type: "COMBAT" | "GUN" | "CARGO" | "DEFENSE" | "MEDICAL" | null;
    installationSupported: boolean; installable: boolean; reason: string | null;
  }[];
  ownedTurrets: {
    turretId: string; level: number; revision: number;
    installationSupported: false; reason: "turret_installation_undefined";
  }[];
  unlockedSlotCount: 2 | 3 | 4 | null;
  progressError: null | "story_progress_invalid" | "story_progress_inconsistent";
  typeLimits: Record<"COMBAT" | "GUN" | "CARGO" | "DEFENSE" | "MEDICAL", { min: number; max: number }>;
  revision: number; // installation revision, 0 before first save
  loadout: Selection | null;
  loadoutError: string | null;
  openRun: boolean;
  updatedAt: string | null; // ISO UTC
};
```

One SQLite statement reads a consistent inventory/progress/selection snapshot.
GET does not initialize anything. Empty inventory is `train:null` and empty arrays;
empty installation is `revision:0,loadout:null,updatedAt:null`. Persisted selection
is retained if ownership/progress later changes, with `loadoutError` identifying
invalidity. Unsupported stored schema gives `loadout:null` and the schema error;
the database value is not repaired. `installable` describes that owned carriage's
catalog/progress eligibility, not sufficient slots or permission to mutate during
an open run; consult `openRun` and POST validation as well.

## POST /v1/trains/loadout

Header `Idempotency-Key`: 1–128 printable non-space ASCII characters. Body is strict;
unknown fields (including client stats, equipment, ownership or story flags) fail.

```json
{
  "rulesVersion": "train-loadout-v1",
  "trainId": "train_a",
  "expectedRevision": 0,
  "carriages": [
    {"slot": 0, "carriageId": "car_combat"},
    {"slot": 1, "carriageId": "car_gun"}
  ]
}
```

Use the actual train ID from GET, not the illustrative `train_a`. IDs use existing
`[A-Za-z0-9_-]{1,100}` contract. Slots are unique, contiguous from zero and filled
to the current slot count. IDs are unique. Row ordering is canonicalized by slot.
`expectedRevision` is a nonnegative safe integer below MAX_SAFE_INTEGER.

```json
{
  "accepted": true,
  "receiptId": "train_loadout_...",
  "revision": 1,
  "changed": true,
  "loadout": {
    "rulesVersion": "train-loadout-v1",
    "trainId": "train_a",
    "carriages": [{"slot":0,"carriageId":"car_combat"},{"slot":1,"carriageId":"car_gun"}]
  },
  "refreshRequired": true
}
```

Accepted exact same selection with a fresh key/current revision returns
`changed:false` with unchanged revision and timestamp, but persists its own receipt.
Same account/key/canonical body returns the original immutable response before
current ownership/revision/run checks, even after later state changes. A changed
body (including expectedRevision) with the same key fails `idempotency_conflict`.
On ambiguous transport failure retry the exact key/body; after success or conflict,
refresh GET and account state and replace the UI snapshot. Never increment a local
balance or revision based only on receipt replay.

An account mutation sequence is captured before reads. A single D1 transaction
checks current ownership, raw progress, selection revision and absence of ACTIVE,
FINISH_PENDING or QUARANTINED runs, then stores selection and immutable receipt.
CAS/unique races retry at most four attempts with receipt rechecks. Failed writes
roll back mutation, preference and receipt together. No currency/item ledger writes.

Error shape is `{accepted:false,errorCode:string}`:

| HTTP | Codes |
| --- | --- |
| 400 | contract_violation, train_slot_count_invalid |
| 401 / admission status | Existing auth/client admission errors |
| 403 | train_not_owned, carriage_not_owned |
| 409 | idempotency_conflict, train_loadout_revision_conflict, train_loadout_open_run, train_loadout_write_conflict, story_locked, story_progress_invalid, story_progress_inconsistent, medical_unlock_unapproved, carriage_definition_unsupported, train_type_limit_invalid |

`train_loadout_schema_unsupported` is a GET loadoutError for incompatible saved
data. S04 does not issue departure previews or freeze run state; S11 is separate.

## Verification

Ten signed-auth real SQLite tests cover reads/no grants, unsupported ownership,
strict slots, ownership, milestones, replay/conflict/no-op, all open run statuses,
commit-time ownership/progress/start races, concurrent installs and receipt failure
rollback. Initial HTTP tests failed 404 before implementation; the final targeted
run passed. Full-suite/typecheck/lint evidence is recorded in the owner handoff.

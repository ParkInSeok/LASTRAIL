# CE01 explicit ultimate operator selection

Local implementation, migration `0031_ultimate_selection.sql`. This implements the
approved pre-start choice among the actual five; no battle effects, timings, stats,
seed or catalog policy is enabled. New public paid starts remain `combat_not_ready`.

## Read and save

`GET /v1/formation/ultimate` requires bearer authentication and existing admission.
No query or body. Read-only, no lazy row creation. Every response is `no-store`.

```json
{"accepted":true,"rulesVersion":"ultimate-selection-v1","operatorId":null,"revision":0,"formationRevision":1,"selectionStatus":"UNSELECTED","changeLocked":false}
```

`selectionStatus`: `FORMATION_REQUIRED` (no active formation), `OWNERSHIP_INVALID`
(active five no longer all owned), `UNSELECTED`, or `SELECTED`. Missing formation
has formationRevision/revision 0 and operatorId null. Invalid persisted formation
or a selected ID outside its five yields 409, never an automatic replacement.
`changeLocked` reflects ACTIVE/FINISH_PENDING/QUARANTINED run when formation exists.
The dedicated GET is the authoritative selection read; existing S01 DTO is unchanged.

`POST /v1/formation/ultimate` requires `Idempotency-Key` (1–128 non-space printable
ASCII characters) and this strict JSON body, no query/extra fields:

```json
{"rulesVersion":"ultimate-selection-v1","operatorId":"op_example","expectedRevision":0,"expectedFormationRevision":1}
```

The example ID must be replaced by an actual owned active member. Explicit null
clears selection. Revisions are nonnegative safe integers below MAX_SAFE_INTEGER.
Response: accepted, rulesVersion, operatorId, revision, formationRevision,
selectionStatus (`SELECTED`/`UNSELECTED`) and refreshRequired=true. Successful
changed choice increments only ultimate revision. New-key no-op keeps revision;
it still has a committed receipt. Formation revision is not incremented by this POST.

401 is missing/invalid/revoked authentication. 400 `contract_violation` is malformed
input. 403 `ultimate_operator_not_eligible` is a target outside the active five or
an active member no longer owned. 409 codes: `formation_required`,
`formation_schema_unsupported`, `ultimate_selection_invalid`,
`formation_revision_conflict`, `ultimate_revision_conflict`, `ultimate_open_run`,
`idempotency_conflict`, `ultimate_write_conflict`.

Receipt namespace is account + ultimate-selection endpoint + key. Same original
body/key returns exact committed response before checking current revisions/run
lock; changed body with same key conflicts. After a lost response retain/retry the
original body and key. A receipt replay is historical, so GET current state before
another edit. A 409 retains the UI draft; refresh authoritative revisions before an
explicitly new command. Never silently turn a conflict into a new-key retry.

## Atomic formation and departure behavior

Columns account_formation.ultimate_operator_id (nullable) and ultimate_revision
(default0) migrate existing formations without selecting anyone. No Kyle/slot0
fallback. POST uses the shared account mutation sequence and an atomic SQL batch
guard on formation JSON, both revisions, prior choice, all-five ownership and run
lock. Receipt insertion failure rolls back the update.

The migration's AFTER UPDATE OF formation_json trigger clears a choice absent
from the new five and increments ultimate revision in that same transaction.
It preserves a retained member, including reordering; inactive preset SAVE never
touches the active row. Failed activation rolls back both formation and clear.
Formation activation retains its existing run lock. Activate response still says
refreshRequired; clients must GET the choice after activation.

New strict `POST /v1/operations/preview` body additionally requires
`expectedUltimateRevision` (0 allowed); admission version is now `departure-v2`.
Preview validates the stored choice and all-five ownership, verifies the caller's
three revisions and transactionally pins:

```ts
admission.ultimateSelection = {
  rulesVersion: "ultimate-selection-v1",
  operatorId: string | null,
  revision: number
};
```

Null can be quoted to expose why combat is blocked; it cannot start in the domain
core. The retained core checks nonnull selection and compares the entire admission
against current state, including choice/revision; commit guards repeat these
checks. Successful internal-core run snapshots include immutable
`admission.ultimateSelection` and root `ultimateOperatorId`. HTTP paid start still
returns 409 combat_not_ready and never calls that core. This is not battle entry.

New diagnostics use `run-combat-requirements-v2`, ultimateOperatorId:string|null
and ultimateSelectionRevision:number|null. A valid pinned member removes only
`ultimate_selection_required`; six other blockers remain. Legacy previews without
CE01 pin return null and that blocker. Existing successful start/preview receipts
remain exact historical responses, not upgraded with current choices. Old
uncommitted departure-v1 previews are unsupported by the new core; obtain a new
explicit quote with the new schema. Original committed run recovery stays intact.

## Validation and scope

Integration tests cover null/read-only, ownership, strict input, CAS, exact replay,
account isolation/auth, active selection preservation/clear, rollback, all three
run locks, concurrent CAS/activation/start/preview races, snapshot pinning and null
core rejection. Fixture choices are explicit test setup only. Migration and code
must be deployed together in a separately authorized remote operation; none was
performed here.

Verification on 2026-09-10: full 79 files / 516 tests passed (16.16 seconds),
including 12 CE01 tests. Game API TypeScript and source ESLint passed; independent
source review found no actionable defect. Full migration fixtures explicitly
expect 31 migrations; old committed run replay tests remain passing.

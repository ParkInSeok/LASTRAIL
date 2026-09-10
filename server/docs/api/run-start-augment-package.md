# Run Start Augment Package Contract

Current S11 paid-start availability is gated by
[run-combat-requirements.md](run-combat-requirements.md). Missing battle bindings
return409 combat_not_ready before debit/consume/run creation. Prior successful
receipts remain replayable; metadata/start persistence alone does not prove actual
Unity battle entry.

This document captures the Act 1 launch contract for augment/tactic selection.

Scope:

- Local contract only.
- No DEV/PRD/STAGING deploy, secret, or remote smoke.
- No reward/economy/story/offline/gacha truth is delegated to Unity.
- No server-owned animation frame, VFX, SFX, camera, hit-stop, or timeline
  payload.
- Rail Choice Events remain backlog.

## Decision

Act 1 launch uses a start-of-run full package instead of mid-wave choice
checkpoints.

Ordinary Act 1 operations remain `ORDINARY_15_WAVE` encounters, but the player
enters wave 1 with the authored launch package already available. The old
3/6/9/12/15 checkpoint-style augment/tactic model stays as schema/protocol
backlog and is not a launch gameplay contract.

Raid boss encounters use the same package model. `raid_raider_king_engine`
starts directly at the boss fight with the ordinary full build package refs.

The local launch Game API run start route is:

```text
POST /v1/runs/start
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

The request body contains only the server-created operation preview reference:

```json
{
  "operationPreviewId": "preview_..."
}
```

The route consumes an ACTIVE `operation_previews` row for the authenticated
account, creates an ACTIVE `runs` row, stores a sanitized `run_snapshots`
metadata payload, and returns server-authored start metadata for Unity.
`Idempotency-Key` is required for launch and is scoped by authenticated account
plus operation `run_start`. A retry with the same key and same
`operationPreviewId` returns the stored `STARTED` response shape without
creating another run. Reusing the same key with a different
`operationPreviewId` returns `contract_violation`.

Consumption and run/snapshot/idempotency persistence now share one guarded D1
transaction. An already consumed preview without matching replay is rejected;
commit failure rolls back consumption without a separate reopen. Legacy commit
failure returns `409 run_start_state_conflict` and writes a scrubbed observational
ops log. New S11 previews also validate owned formation/installation and debit
quoted fuel atomically; see [departure-admission.md](departure-admission.md) for
the producer, admission DTO, errors and explicit legacy cost boundary. The response
must not include raw auth headers, tokens, raw request
body, RNG/probability data, internal weights, or animation timeline fields.

When `CLIENT_ADMISSION_MODE=ENFORCED`, accepted run start also stores a
nullable `runVersionSnapshot` in `run_snapshots.snapshot_json` and returns the
same object in the start response. This captures the server-accepted app/content
compatibility state at the logical run start, not whatever policy is current
when the result is later submitted.

```json
{
  "runVersionSnapshot": {
    "source": "server_accepted_run_start",
    "clientInstalled": {
      "appVersion": "1.0.0",
      "versionCode": "100",
      "platform": "android",
      "channel": "internal"
    },
    "clientAdmission": {
      "status": "ACCEPTED",
      "minimumVersionCode": "90",
      "updateRequired": false
    },
    "serverAcceptedDeployment": {
      "environment": "DEV",
      "runtimeDeploymentId": "cf-runtime-deployment-id"
    },
    "contentRelease": {
      "status": "APPROVED_COMPATIBLE",
      "releaseId": "content_act1_001",
      "contentVersion": "act1.2026-09-06.001",
      "catalogHash": "sha256:..."
    },
    "capturedAt": "2026-09-06T00:00:00.000Z"
  }
}
```

Legacy/local runs or disabled admission return/store `runVersionSnapshot: null`.
Idempotency replay returns the originally stored snapshot; it does not read the
current version policy again. The snapshot is diagnostic/support evidence only
and must not drive reward, story progress, offline reward, gacha, or economy
truth.

Run finish stores a separate `runFinishVersionSnapshot` with source
`server_accepted_run_finish`. The old `runVersionSnapshot` remains the start
snapshot alias for compatibility.

## Server-Owned Metadata

The server-authored run/operation preview metadata may include:

```json
{
  "encounterType": "ORDINARY_15_WAVE",
  "startAugmentPackageRef": "act1_full_start_package_v1",
  "startTacticPackageRef": "act1_full_start_package_v1",
  "allowedOperatorSlots": 5,
  "contentVersion": "act1.launch.content.v1"
}
```

Raid preview metadata may use the same refs:

```json
{
  "encounterType": "RAID_BOSS",
  "raidId": "raid_raider_king_engine",
  "bossId": "boss_raider_king_engine",
  "bossPhaseRefs": ["crown_chase", "trophy_break", "engine_stop"],
  "startAugmentPackageRef": "act1_full_start_package_v1",
  "startTacticPackageRef": "act1_full_start_package_v1",
  "allowedOperatorSlots": 5,
  "contentVersion": "act1.launch.content.v1"
}
```

`startAugmentPackageRef` and `startTacticPackageRef` are server-authored refs.
Unity may display/select concrete ids from the referenced authored package, but
client-supplied refs do not become economy, reward, unlock, story progress, or
offline reward Source of Truth.

Admin/support read models may expose these server-authored refs for
read-only investigation. `raidId`, `bossId`, and `bossPhaseRefs` remain
raid-only metadata; ordinary 15-wave previews must not inherit raid refs from
malformed preview JSON.

Rejected run start responses keep the same envelope with `accepted=false`,
`startStatus=REJECTED`, null refs, and a stable `errorCode`.

Current rejected codes include:

- `auth_required`
- `contract_violation`
- `operation_preview_not_found`
- `operation_preview_not_startable`
- `operation_preview_already_consumed`
- `run_start_state_conflict`

## Run Finish Selection Echo

Run finish may include selected refs in `tacticSelections` as a validation and
diagnostic echo:

```json
{
  "tacticSelections": [
    {
      "source": "START_PACKAGE",
      "packageRef": "act1_full_start_package_v1",
      "selectedAugmentId": "augment_rail_bulwark",
      "selectedTacticId": "tactic_cover_fire"
    }
  ]
}
```

Server responsibilities:

- verify any submitted `packageRef` matches the server-authored
  `startAugmentPackageRef` or `startTacticPackageRef`;
- preserve the final payload for support/integrity context;
- treat selected refs as legality/diagnostic input only;
- continue calculating reward, first clear, story progress, offline rewards,
  gacha, and economy from server-owned records.

Unity responsibilities:

- present authored choices;
- store local selection state for UX;
- submit selected refs as an echo when required;
- never calculate reward/economy/story/offline/gacha truth from selected refs.

## Launch-Deferred Tables

Existing schema has launch-deferred structures:

- `run_checkpoints(checkpoint_wave IN 3,6,9,12,15)`;
- `run_augment_history(wave_index BETWEEN 1 AND 15)`;
- `run_tactical_ledger`.

These remain compatible storage/backlog surfaces, but launch ordinary Act 1
does not require mid-wave checkpoint selection. If future versions restore
mid-run drafting, add a new contract slice and compatibility tests instead of
silently reusing launch fields.

## Forbidden Data

Run start/read and finish/submit contracts must not include:

- raw Addressables catalog URL in `runVersionSnapshot` or Admin run evidence;
- raw RNG material or seed preimage;
- probability values or internal balance weights;
- reward amount, currency grant, item grant, economy grant, or ledger ids
  supplied by Unity;
- raw animation timeline, frame data, VFX/SFX/camera/hit-stop tracks;
- unrestricted validator internals;
- token, secret, raw auth header, or account shortcut identifiers.

## Tests

Launch contract tests should cover:

- ordinary 15-wave run accepts empty `tacticSelections` for backwards
  compatibility;
- selected package refs must match server-authored start package refs;
- nested raw RNG/reward/economy fields inside `tacticSelections` are rejected
  before DB reads;
- raid boss-only runs can use the same start package refs;
- duplicate replay returns the stored accepted result and does not re-validate
  or mutate selections.

## Decision Queue

- Final `startAugmentPackageRef` and `startTacticPackageRef` stable ids.
- Whether selected ids need a server-side authored package catalog before
  launch, or whether package-ref legality is enough for the first Unity QA pass.
- Future mid-wave drafting restart criteria. Backlog, not launch.

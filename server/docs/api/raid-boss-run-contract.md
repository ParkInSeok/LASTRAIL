# Raid Boss Run Contract

This draft captures the Act 1 raid impact on the existing run lifecycle.

Scope:

- Local contract only.
- No DEV/PRD/STAGING deploy, secret, or remote smoke.
- No server-owned animation timeline, combat replay, camera, VFX, SFX, or hit-stop state.
- Rail Choice Events remain backlog and are not part of the run finish payload.

## Decision

Use the existing run lifecycle and extend its metadata instead of opening a
raid-specific route for launch.

Reasons:

- `raid_raider_king_engine` still participates in the same account, run,
  idempotency, reward preview, first clear, story progress, and offline reward
  systems.
- Duplicate replay can continue to use `idempotency_records(operation =
  'run_finish')` and the stored `rewardPreviewId`.
- Unity can keep one result submit adapter and branch presentation by
  `encounterType`.

A future raid-specific route can be reconsidered only if raids need a different
server-owned lifecycle, matchmaking, seasonal state, or multi-attempt boss save
model.

## Encounter Types

Launch encounter types:

- `ORDINARY_15_WAVE`: normal Act 1 operation. Completion is `VICTORY` with
  `completedWaveCount = 15`.
- `RAID_BOSS`: Act 1 boss-only raid. Completion is `VICTORY` with
  `raidResult.bossDefeated = true`.

Ordinary Act 1 operations remain 15-wave runs.

`raid_raider_king_engine` is a boss-only encounter. It starts with the ordinary
full build package: allowed operator slots plus the start-of-run augment/tactic
package that a 15-wave run would have accumulated before the boss fight.

## Run Start Metadata

Run start/read metadata distinguishes the raid without requiring a raid-specific
route. The local launch route is:

```text
POST /v1/runs/start
Authorization: Bearer <access-token>
```

The body contains only the server-created `operationPreviewId`. The response
returns server-authored refs from `operation_previews.preview_json`:

```json
{
  "operationId": "raid_raider_king_engine",
  "encounterType": "RAID_BOSS",
  "raidId": "raid_raider_king_engine",
  "bossId": "boss_raider_king_engine",
  "bossPhaseRefs": ["crown_chase", "trophy_break", "engine_stop"],
  "allowedOperatorSlots": 4,
  "startAugmentPackageRef": "act1_full_start_package_v1",
  "startTacticPackageRef": "act1_full_start_package_v1",
  "contentVersion": "act1.launch.content.v1"
}
```

The server may store refs/ids that let Unity select the correct authored
content. The server does not own frame-by-frame animation data, VFX timing,
camera choreography, hit stop, SFX playback, or combat cinematic timelines.

Admin/support read models may expose the same sanitized refs from
`operation_previews.preview_json` so operators can distinguish ordinary
15-wave operations from raid boss-only encounters. Raid-only refs are only
client-visible when `encounterType = RAID_BOSS`; ordinary previews must not leak
or reinterpret `raidId`, `bossId`, or `bossPhaseRefs` if malformed preview JSON
contains them.

## Run Finish Payload

Ordinary finish keeps the existing payload:

```json
{
  "runId": "run_...",
  "currentStage": "Complete",
  "currentWaveNumber": 0,
  "completedWaveCount": 15,
  "encounterType": "ORDINARY_15_WAVE",
  "outcome": "VICTORY",
  "runtimeStateJson": "{}",
  "tacticSelections": [],
  "clientCompletedAt": "2026-08-27T00:00:00.000Z"
}
```

Raid finish uses boss result metadata:

```json
{
  "runId": "run_...",
  "currentStage": "Complete",
  "currentWaveNumber": 0,
  "completedWaveCount": 0,
  "encounterType": "RAID_BOSS",
  "raidResult": {
    "raidId": "raid_raider_king_engine",
    "bossId": "boss_raider_king_engine",
    "bossDefeated": true,
    "completedPhaseCount": 3,
    "encounterDurationSeconds": 420
  },
  "outcome": "VICTORY",
  "runtimeStateJson": "{}",
  "tacticSelections": [],
  "clientCompletedAt": "2026-08-27T00:00:00.000Z"
}
```

For `RAID_BOSS`, `completedWaveCount` remains a storage-compatible final result
field and may be `0`; the server maps verified boss defeat to full completion
for reward, first clear, and story progress calculation.

The accepted run finish transaction stores a sanitized
`serverPreviewMetadata` snapshot in `run_final_results.payload_json`. It is a
support/debug snapshot of server-authored preview refs, not a new combat
simulation contract:

```json
{
  "serverPreviewMetadata": {
    "operationId": "raid_raider_king_engine",
    "actId": "act_01_wasteland_railway",
    "regionId": "region_wasteland_railway",
    "encounterType": "RAID_BOSS",
    "raidId": "raid_raider_king_engine",
    "bossId": "boss_raider_king_engine",
    "bossPhaseRefs": ["crown_chase", "trophy_break", "engine_stop"],
    "allowedOperatorSlots": 4,
    "startAugmentPackageRef": "act1_full_start_package_v1",
    "startTacticPackageRef": "act1_full_start_package_v1",
    "balanceVersion": "1",
    "contentVersion": "act1.launch.content.v1"
  }
}
```

Duplicate run finish replay returns the stored result/reward preview refs and
does not recompute this metadata.

## Reward / Story / Offline Effects

For `raid_raider_king_engine`:

- First clear eligibility uses `accountId + operationId + risk` as before.
- First clear applies when `outcome = VICTORY` and `raidResult.bossDefeated =
  true`.
- Reward preview generation uses the server run reward config and treats boss
  defeat as full completion.
- Story progress advances to `raid_raider_king_engine` only after verified boss
  defeat.
- Offline reward cap/multiplier later reads server-stored story progress, not
  Unity-supplied values.
- Duplicate run finish replay returns the stored `rewardPreviewId` and does not
  recalculate reward, first clear, story progress, or raid result metadata.

## Forbidden Payload / Response Data

Do not submit, store as structured contract fields, or return:

- raw animation timeline;
- frame data;
- VFX/SFX/camera/hit-stop tracks;
- raw RNG material or seed preimage;
- probability/weighted internals;
- raw validator internals;
- token, secret, auth headers, or account shortcut identifiers.

Unity owns fully animated combat presentation. The server owns durable account,
run, reward, story, and economy truth.

## Tests

Contract smoke should cover:

- ordinary operation `VICTORY + completedWaveCount=15` remains unchanged;
- raid boss `VICTORY + bossDefeated=true + completedWaveCount=0` creates an
  active reward preview and first clear/story progress updates;
- ordinary operation rejects raid metadata;
- raid operation rejects missing raid metadata;
- duplicate replay returns the stored `rewardPreviewId`;
- forbidden animation/raw RNG/internal validator fields are rejected or never
  returned.

## Decision Queue

- Final raid start metadata ids: `bossId`, `bossPhaseRefs`,
  `startAugmentPackageRef`, and `startTacticPackageRef`.
- Whether run start needs a client-visible idempotency key before production
  traffic, or whether active preview consumption is sufficient for launch QA.
- Whether `allowedOperatorSlots` differs from ordinary operations. Current
  parser/read models treat it as nullable/candidate metadata, not a final
  balance decision.
- Whether raid failure needs phase-specific reward scaling or launch default
  failure/abandoned policy is sufficient.
- Whether raid completion needs a future seasonal/attempt table. Not launch
  scope unless explicitly approved.

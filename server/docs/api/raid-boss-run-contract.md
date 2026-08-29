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
Idempotency-Key: <client-generated-key>
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
  "allowedOperatorSlots": 5,
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

Run start uses the same preview consume guard for raids as ordinary operations.
If a raid preview has already been consumed without a matching
`Idempotency-Key` replay record, the route returns `409
operation_preview_already_consumed` and does not create a second raid run. A
retry with the same account, operation `run_start`, key, and
`operationPreviewId` returns the stored `STARTED` response shape.

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
    "allowedOperatorSlots": 5,
    "startAugmentPackageRef": "act1_full_start_package_v1",
    "startTacticPackageRef": "act1_full_start_package_v1",
    "balanceVersion": "1",
    "contentVersion": "act1.launch.content.v1"
  }
}
```

Duplicate run finish replay returns the stored result/reward preview refs and
does not recompute this metadata.

## Story Raid Failure Stamina Refund

Act 1 story raid failure has a launch-only refund marker. It is not World Raid
settlement and it is not an immediate economy mutation.

For `raid_raider_king_engine`, a valid finish with:

- `encounterType = RAID_BOSS`;
- `outcome = FAILURE`;
- `raidResult.bossDefeated = false`;

returns and stores:

```json
{
  "staminaRefund": {
    "status": "PENDING",
    "refundType": "STAMINA",
    "reason": "STORY_RAID_FAILURE",
    "policyRef": "act1_story_raid_failure_stamina_refund_v1"
  }
}
```

The marker is safe for Unity display and support correlation. It deliberately
does not include refund amount, fuel balance, raw run start cost, or fuel
ledger ids. The current slice stores the same marker in
`run_final_results.payload_json.staminaRefund` and in
`run_reward_previews.reward_plan_json.calculation.staminaRefund`, but does not
write `fuel_ledger`, update `fuel_state`, or add a reward line.

Duplicate finish replay reads the stored payload marker and returns the same
stable marker. It must not recalculate, create another reward preview, or apply
stamina twice. Ordinary 15-wave operations and successful boss defeats return
`staminaRefund: null`.

## Reward / Story / Offline Effects

For `raid_raider_king_engine`:

- First clear eligibility uses `accountId + operationId + risk` as before.
- First clear applies when `outcome = VICTORY` and `raidResult.bossDefeated =
  true`.
- Failure stamina refund eligibility uses the story raid marker above and does
  not grant first clear, story progress, or offline progress.
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
- story raid failure returns the `staminaRefund` marker and does not write fuel
  ledger/state;
- forbidden animation/raw RNG/internal validator fields are rejected or never
  returned.

## Decision Queue

- Final raid start metadata ids: `bossId`, `bossPhaseRefs`,
  `startAugmentPackageRef`, and `startTacticPackageRef`.
- Whether the later fuel refund commit should happen during finish, reward
  claim, or a dedicated stamina refund endpoint.
- Whether raid failure needs phase-specific reward scaling beyond the launch
  stamina refund marker.
- Whether raid completion needs a future seasonal/attempt table. Not launch
  scope unless explicitly approved.

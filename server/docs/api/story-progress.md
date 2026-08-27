# Story Progress Source of Truth / Read Model Draft

This draft defines the server-side story progress Source of Truth and read
model candidates for LAST RAIL. It is a design/contract draft only. It does not
add migrations, API routes, runtime implementation, D1 seed data, Cloudflare
resources, Unity DTOs, or Admin integration.

## Goals

- Keep durable story progress and unlock truth on the server.
- Keep Unity responsible for narrative presentation and localized text
  playback.
- Keep Admin visibility read-only first.
- Make accepted run finish the future durable story progress update moment.
- Make story progress updates idempotent and replay-safe.
- Provide a server-owned story progress input for offline rewards.

## Non-Goals

- No full localized dialogue text storage in Game API.
- No combat callout timing or cutscene playback control.
- No briefing/result UI layout contract.
- No long-form codex prose payloads.
- No economy, auth, or combat contract dependency on narrative text.
- No migration or implementation in this slice.

## Ownership Boundary

Server owns:

- account story progress;
- operation clear state;
- act and operation unlock truth;
- durable narrative milestone flags;
- first-clear state through existing `account_first_clears`;
- future accepted run finish story progress updates;
- optional narrative version metadata.

Unity owns:

- localized dialogue text playback;
- cutscene, callout, briefing, result, and codex presentation;
- local UI labels for server stable ids;
- client-side narrative sequencing that does not grant durable progress.

Admin owns:

- read-only operational visibility first;
- future support/debug views for story progress, unlocks, and narrative flags.

Unity request bodies must not become story progress Source of Truth. If a
client sends story progress, unlock state, narrative flags, or offline progress
keys to a write route, the server should reject the request with
`contract_violation` unless that route has an explicit validation-only field.

## Existing Server Foundations

Current migrations already provide related building blocks:

- `accounts(id)` is the account Source of Truth.
- `account_unlocks(account_id, unlock_id)` can store unlocked operations,
  features, train slots, or narrative capabilities.
- `progression_ledger` can record durable progression deltas.
- `account_first_clears(account_id, operation_key, risk)` stores first clear
  state by operation and risk tier.
- `operation_previews.objective` and `preview_json.operationId` identify the
  operation selected for a run.
- `runs.preview_id` links a run to the frozen operation preview.
- `run_final_results` stores accepted final run outcome and cleared waves.
- `idempotency_records` stores successful committed operation replay keys.

Gap:

- There is no dedicated story progress read model yet.
- `account_first_clears` is risk-scoped and should stay reward/first-clear
  focused, not become the account story progress table.
- `account_unlocks` can express individual unlocks, but it does not by itself
  provide a compact ordered Act 1 progress summary.

## Act 1 Progression Order

Use stable ids as the ordered story progress keys:

| Order | Story progress id | Meaning |
| ---: | --- | --- |
| 0 | `none` | No Act 1 operation clear yet |
| 1 | `opn_01_last_departure` | Last departure completed |
| 2 | `opn_02_first_signal` | First signal restored |
| 3 | `opn_03_water_stop` | Water stop secured |
| 4 | `opn_04_scrapstar_show` | Scrapstar market/show resolved |
| 5 | `opn_05_night_boarders` | Night boarders repelled |
| 6 | `opn_06_broken_line` | Broken line restored |
| 7 | `raid_raider_king_engine` | Act 1 raid milestone cleared |

The raid milestone can be a story progress key, but the exact unlock/clear
condition remains a future raid system decision.

## Recommended Read Model

Add a dedicated table in a future migration:

```sql
CREATE TABLE account_story_progress (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  act_id TEXT NOT NULL,
  highest_cleared_operation_id TEXT NOT NULL,
  highest_story_progress_id TEXT NOT NULL,
  cleared_operations_json TEXT NOT NULL,
  story_flags_json TEXT NOT NULL,
  narrative_version TEXT,
  last_progress_run_id TEXT,
  updated_at INTEGER NOT NULL
);

CREATE INDEX idx_account_story_progress_act
  ON account_story_progress(act_id, highest_story_progress_id);
```

Candidate field semantics:

- `account_id`: authenticated account.
- `act_id`: currently `act_01_wasteland_railway` for launch.
- `highest_cleared_operation_id`: highest ordinary operation clear, excluding
  raid unless raid is modeled as an operation.
- `highest_story_progress_id`: ordered story progress key, including raid
  milestone if reached.
- `cleared_operations_json`: stable set/list of cleared operation ids.
- `story_flags_json`: durable milestone flags, such as tutorial narration seen,
  major reveal acknowledged, or feature narration unlocked.
- `narrative_version`: optional content/narrative metadata version.
- `last_progress_run_id`: last accepted finish that advanced progress.
- `updated_at`: server timestamp.

Recommendation:

- Use this dedicated read model instead of overloading `account_runtime_state`.
- Keep `account_unlocks` for granular unlock truth.
- Keep `progression_ledger` for immutable progression deltas.
- Keep `account_first_clears` risk-scoped for first-clear rewards.

## Unlock Model

Use `account_unlocks` for granular unlocks:

```text
operation:opn_02_first_signal
operation:opn_03_water_stop
train_slot:3
train_car:car_cargo
feature:raid
narrative_flag:first_signal_restored
```

Unlock ids should be stable strings and should not store localized display text.

Future run finish story progress update can insert:

- operation unlocks for the next operation;
- train slot/car unlocks if the operation grants them;
- narrative flags for durable milestones;
- progression ledger rows that describe the server-side delta.

## Response Shape Candidate

Future read-only endpoint candidate:

```text
GET /v1/story-progress
```

Response:

```json
{
  "actId": "act_01_wasteland_railway",
  "operationId": "opn_03_water_stop",
  "storyProgress": {
    "highestStoryProgressId": "opn_03_water_stop",
    "highestClearedOperationId": "opn_03_water_stop",
    "order": 3
  },
  "firstClear": {
    "operationId": "opn_03_water_stop",
    "risk": "III",
    "cleared": true
  },
  "narrativeFlags": [
    "first_signal_restored",
    "water_stop_secured"
  ],
  "unlockedOperations": [
    "opn_01_last_departure",
    "opn_02_first_signal",
    "opn_03_water_stop",
    "opn_04_scrapstar_show"
  ],
  "narrativeVersion": "act1-narrative-v1"
}
```

Notes:

- `firstClear` is optional detail and remains risk-scoped.
- `storyProgress` is not risk-scoped.
- `unlockedOperations` is server-derived from `account_unlocks` or story
  progress policy.
- `narrativeVersion` is metadata only. It must not gate economy rewards unless a
  future migration explicitly introduces such a dependency.

## Accepted Run Finish Update Candidate

Accepted `POST /v1/runs/:runId/finish` is the durable story progress update
moment.

Candidate rules:

- Only accepted, validated, authenticated run finish can update story progress.
- Story progress update uses the server-frozen run context:
  - authenticated `accountId`;
  - `runs.preview_id`;
  - `operation_previews.objective`;
  - `operation_previews.preview_json.operationId`;
  - `run_final_results.outcome`;
  - `run_final_results.cleared_waves`.
- `VICTORY` with `clearedWaves = 15` advances operation story progress.
- `FAILURE` and `ABANDONED` do not advance ordinary story progress.
- Duplicate run finish replay must not recalculate or reapply story progress.
- Lower operation clears must not regress higher stored progress.
- Higher operation clears can backfill `cleared_operations_json` policy only if
  a future design explicitly allows skip progression. Launch recommendation is
  linear Act 1 progression only.

Candidate D1 batch for a progress-advancing finish:

1. Insert `run_final_results`.
2. Update `runs`.
3. Insert `run_reward_previews`.
4. Insert `account_first_clears` when eligible.
5. Upsert `account_story_progress` only if the new operation order is greater
   than the stored order.
6. Insert `account_unlocks` for newly unlocked operation/feature ids.
7. Insert `progression_ledger` rows for story progress/unlock deltas.
8. Insert `idempotency_records.operation = 'run_finish'`.

This must be one atomic committed operation for launch. If the story progress
write fails, the accepted finish should fail or roll back rather than create a
reward preview with missing story progress side effects.

## Duplicate Replay Policy

Run finish idempotency currently replays by:

```text
accountId + operation = 'run_finish' + Idempotency-Key
```

Story progress must follow the same principle:

- replay returns the stored accepted response;
- no second `run_final_results`;
- no second reward preview;
- no second story progress update;
- no duplicate narrative flags;
- no duplicate unlock rows;
- no progress regression.

If `Idempotency-Key` was used for another run, the server should reject with
`contract_violation` and must not return another run's `rewardPreviewId` or
story progress result.

## Offline Reward Input

Offline rewards should use server-stored story progress:

```text
account_story_progress.highest_story_progress_id
```

Do not use:

- Unity local story progress;
- Unity request body story progress;
- run risk;
- first-clear risk tier.

If story progress cannot be resolved:

- return `offline_story_progress_missing` for offline reward preview/claim;
- do not infer from client state.

## Contract / Smoke Test Candidates

Read model:

- New guest account starts at `highest_story_progress_id = 'none'` or receives a
  default read response with `none`.
- `opn_02_first_signal` clear after `opn_01_last_departure` advances
  `highest_story_progress_id` from `opn_01_last_departure` to
  `opn_02_first_signal`.
- `unlockedOperations` includes the next operation after progress advances.

Replay/idempotency:

- Replaying the same accepted finish does not duplicate flags.
- Replaying the same accepted finish does not duplicate unlock rows.
- Replaying the same accepted finish does not advance revision twice.
- Reusing an idempotency key for a different run is `contract_violation`.

Regression prevention:

- Clearing `opn_02_first_signal` after already clearing
  `opn_04_scrapstar_show` does not regress `highest_story_progress_id`.
- Story flags merge as a stable set and do not duplicate.

Client trust boundary:

- Client body `storyProgress` is rejected with `contract_violation`.
- Client body `highestStoryProgressId` is rejected with `contract_violation`.
- Client body `unlockedOperations` is rejected with `contract_violation`.
- Offline reward claim uses server stored story progress, not Unity-supplied
  value.

Offline linkage:

- Same story progress produces same offline multiplier regardless of Risk I or
  Risk V.
- `raid_raider_king_engine` progress produces a larger offline cap/multiplier
  than `opn_06_broken_line`.

## Unity Impact

No current Unity behavior change is required by this design draft.

Future Unity DTO/display candidates:

- `actId`
- `operationId`
- `storyProgress.highestStoryProgressId`
- `storyProgress.order`
- `narrativeFlags`
- `unlockedOperations`
- `narrativeVersion`

Unity must keep:

- localized narrative text and playback client-owned;
- story progress display label only;
- no durable story progress calculation;
- no offline reward multiplier calculation;
- no unlock truth calculation.

## Admin / Operations Impact

Future Admin read-only candidates:

- account story progress summary;
- operation clear state;
- story flags;
- unlocked operation/feature list;
- last progress run id;
- narrative version;
- mismatch alerts when a run completed but story progress did not advance.

Ops error code candidates:

```text
story_progress_missing
story_progress_policy_missing
story_progress_state_conflict
story_progress_regression_blocked
story_progress_idempotency_mismatch
story_unlock_policy_missing
```

Admin audit remains for operator actions. Runtime story progress failures belong
in ops error logs or integrity/progression events.

## Open Decisions

- Whether to create `account_story_progress` in the next migration or first
  derive a read model from `account_unlocks` and `progression_ledger`.
- Whether story progress updates should be in the same run finish batch as
  reward preview immediately, or introduced after full validator integration.
- Exact unlock ids for train slot/car/feature unlocks.
- Whether raid story progress requires clear, participation, or season claim.
- Whether `narrativeVersion` is global, act-scoped, or operation-scoped.
- Whether Admin needs a separate story progress history table beyond
  `progression_ledger`.

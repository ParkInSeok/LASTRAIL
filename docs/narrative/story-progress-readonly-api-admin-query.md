# Story Progress Read-Only API And Admin Query Contract

## Purpose

This document defines the first-pass read-only contract for exposing story progress and narrative status to Unity and Admin surfaces.

It is a contract proposal, not an implementation requirement by itself. Server remains the Source of Truth for durable story progress and unlock truth. Unity remains the Source of Presentation. Admin remains read-only operational visibility first.

## Current Foundation

The server now has a durable story progress read model candidate:

- `account_story_progress.account_id`
- `account_story_progress.act_id`
- `account_story_progress.highest_cleared_operation_id`
- `account_story_progress.highest_story_progress_id`
- `account_story_progress.cleared_operations_json`
- `account_story_progress.story_flags_json`
- `account_story_progress.narrative_version`
- `account_story_progress.last_progress_run_id`
- `account_story_progress.updated_at`

Accepted run finish can advance story progress when:

- outcome is `VICTORY`
- `completedWaveCount = 15`
- the cleared operation is higher than current stored progress

Duplicate finish replay must not recalculate, reapply, or regress story progress.

## Goals

- Let Unity ask, "What story progress does this account have?"
- Let Admin inspect story progress, narrative coverage, localization status, and missing-state candidates.
- Let offline rewards and future read models reference server-stored highest story progress.
- Keep localized dialogue text out of Server APIs.
- Keep story progress mutation, unlock mutation, narrative editing, localization publishing, and reward repair out of the read-only contract.

## Non-Goals

- No narrative editing API.
- No localization publishing API.
- No story progress mutation endpoint.
- No reward repair or economy correction endpoint.
- No full localized dialogue text payload.
- No cutscene timing or callout playback timing from Server.
- No trust in Unity-supplied story progress, unlocks, narrative flags, or narrativeVersion.

## Game API Candidate

### Endpoint

`GET /v1/story-progress`

### Auth

- Player Bearer auth required.
- The account id comes from authenticated session context.
- Request body is not used.
- Query-supplied account id is not accepted.

### Response Shape

```json
{
  "accepted": true,
  "actId": "act_01",
  "storyProgress": {
    "highestStoryProgressId": "opn_03_water_stop",
    "highestClearedOperationId": "opn_03_water_stop",
    "order": 3,
    "clearedOperationIds": [
      "opn_01_last_departure",
      "opn_02_first_signal",
      "opn_03_water_stop"
    ],
    "storyFlags": [],
    "lastProgressRunId": "run_example",
    "updatedAt": "2026-08-27T00:00:00.000Z"
  },
  "unlockedOperations": [
    "opn_04_scrapstar_show"
  ],
  "narrative": {
    "narrativeVersion": "act1.2026-08-30.001",
    "contentStatus": "draft_exists",
    "localizationStatus": "localization_pending",
    "fixtureStatus": "fixture_pending",
    "missingContentStatus": "version_pending"
  },
  "errorCode": null,
  "message": null
}
```

### Empty Account Response

If an account has no story progress row yet, return stable default state rather than an error:

```json
{
  "accepted": true,
  "actId": "act_01",
  "storyProgress": {
    "highestStoryProgressId": "none",
    "highestClearedOperationId": null,
    "order": 0,
    "clearedOperationIds": [],
    "storyFlags": [],
    "lastProgressRunId": null,
    "updatedAt": null
  },
  "unlockedOperations": [
    "opn_01_last_departure"
  ],
  "narrative": {
    "narrativeVersion": "act1.2026-08-30.001",
    "contentStatus": "draft_exists",
    "localizationStatus": "localization_pending",
    "fixtureStatus": "fixture_pending",
    "missingContentStatus": "version_pending"
  },
  "errorCode": null,
  "message": null
}
```

## Admin API Candidate

### List Endpoint

`GET /v1/admin/story-progress`

### Detail Endpoint

`GET /v1/admin/story-progress/:accountId`

### Permission Candidate

`STORY_PROGRESS_READ`

If a dedicated permission is not ready, use an existing read-only support/admin permission temporarily and keep `STORY_PROGRESS_READ` as the target split.

### Filters

- `accountId`
- `actId`
- `highestStoryProgressId`
- `highestClearedOperationId`
- `narrativeVersion`
- `contentStatus`
- `localizationStatus`
- `fixtureStatus`
- `missingContentStatus`
- `updatedFrom`
- `updatedTo`
- `limit`
- `cursor`

### List Item Shape

```json
{
  "accountId": "acct_example",
  "actId": "act_01",
  "highestStoryProgressId": "opn_03_water_stop",
  "highestClearedOperationId": "opn_03_water_stop",
  "order": 3,
  "narrativeVersion": "act1.2026-08-30.001",
  "contentStatus": "draft_exists",
  "localizationStatus": "localization_pending",
  "fixtureStatus": "fixture_pending",
  "missingContentStatus": "version_pending",
  "lastProgressRunId": "run_example",
  "updatedAt": "2026-08-27T00:00:00.000Z"
}
```

### Detail Shape

```json
{
  "accountId": "acct_example",
  "actId": "act_01",
  "storyProgress": {
    "highestStoryProgressId": "opn_03_water_stop",
    "highestClearedOperationId": "opn_03_water_stop",
    "order": 3,
    "clearedOperationIds": [
      "opn_01_last_departure",
      "opn_02_first_signal",
      "opn_03_water_stop"
    ],
    "storyFlags": [],
    "lastProgressRunId": "run_example",
    "updatedAt": "2026-08-27T00:00:00.000Z"
  },
  "unlockedOperations": [
    "opn_04_scrapstar_show"
  ],
  "firstClearSummary": {
    "scope": "account_operation_risk",
    "operationIds": [
      "opn_01_last_departure",
      "opn_02_first_signal",
      "opn_03_water_stop"
    ]
  },
  "narrative": {
    "narrativeVersion": "act1.2026-08-30.001",
    "contentStatus": "draft_exists",
    "localizationStatus": "localization_pending",
    "fixtureStatus": "fixture_pending",
    "missingContentStatus": "version_pending"
  },
  "supportContext": {
    "lastProgressRunId": "run_example",
    "canMutateStoryProgress": false,
    "canRepairRewards": false
  }
}
```

## Stable Status Codes

Use the same status candidates from the localization/versioning policy:

- `draft_exists`
- `localization_pending`
- `fixture_pending`
- `version_pending`
- `content_missing`
- `version_mismatch`
- `ready`

Unknown backend status codes must remain visible through raw fallback in Admin and debug surfaces.

## Act 1 Order

| Order | Stable id | Display label |
| --- | --- | --- |
| 0 | `none` | 스토리 진행 없음 |
| 1 | `opn_01_last_departure` | 마지막 출발 |
| 2 | `opn_02_first_signal` | 첫 신호 |
| 3 | `opn_03_water_stop` | 급수 정차 |
| 4 | `opn_04_scrapstar_show` | 스크랩스타 쇼 |
| 5 | `opn_05_night_boarders` | 야간 승선전 |
| 6 | `opn_06_broken_line` | 끊어진 선로 |
| 7 | `raid_raider_king_engine` | 약탈왕 기관차 |

Server should return stable ids. Unity/Admin should apply display labels in presentation layers.

## Error Codes

Candidate stable error codes:

- `auth_required`
- `story_progress_unavailable`
- `story_progress_policy_missing`
- `story_progress_state_conflict`
- `narrative_content_missing`
- `narrative_version_mismatch`
- `contract_violation`

Admin label candidates:

- `story_progress_unavailable` -> `스토리 진행 조회 불가`
- `story_progress_policy_missing` -> `스토리 진행 정책 누락`
- `story_progress_state_conflict` -> `스토리 진행 상태 충돌`
- `narrative_content_missing` -> `내러티브 콘텐츠 누락`
- `narrative_version_mismatch` -> `내러티브 버전 불일치`

## Security And Privacy

Do not return:

- tokens
- secrets
- Authorization headers
- Cloudflare Access JWTs
- raw request bodies
- raw response bodies
- account shortcut headers
- raw RNG material
- reward calculation internals
- full localized dialogue text
- unpublished narrative author notes

The read-only API may return stable ids, timestamps, run references, version/status metadata, and support context.

## Unity Compatibility

Unity can consume the Game API story progress response as display/debug context, but should not use it to calculate:

- operation unlock truth
- first clear eligibility
- reward amount
- reward multiplier
- offline reward amount
- offline storage cap
- story progress multiplier

Recommended future Unity DTO fields:

- `actId`
- `highestStoryProgressId`
- `highestClearedOperationId`
- `order`
- `clearedOperationIds`
- `unlockedOperations`
- `narrativeVersion`
- `contentStatus`
- `localizationStatus`
- `fixtureStatus`
- `missingContentStatus`

`MissionNarrativeCatalog` should remain local presentation data keyed by stable mission id.

## Admin Compatibility

Admin should connect existing read-only skeleton fields to the Admin API only after the server contract exists.

Initial mappings:

- Run detail story progress -> `highestStoryProgressId`
- Narrative coverage -> `contentStatus`
- Localization status -> `localizationStatus`
- Fixture status -> `fixtureStatus`
- Narrative version -> `narrativeVersion`
- Missing data state -> `missingContentStatus`
- Player support context -> `lastProgressRunId`, `updatedAt`, read-only permission context

Admin must not expose mutation controls from this contract.

## Offline Reward Compatibility

Offline rewards should use server-stored `highestStoryProgressId` as an input. They should not use:

- Unity local story state
- client request body story progress
- risk tier
- first clear risk tier
- narrativeVersion
- localization status
- fixture status

## Contract Tests To Add Later

- Empty account returns `none` progress without error.
- VICTORY full clear updates read model and read endpoint returns new highest progress.
- Duplicate finish replay does not change read endpoint output after first accepted response.
- Lower operation clear does not regress read endpoint output.
- Client-supplied story progress fields are rejected on write routes.
- Admin list filters by `highestStoryProgressId`.
- Admin detail returns no full localized dialogue text.
- Unity DTO preserves stable ids and does not calculate rewards/offline rewards.
- Unknown status code remains visible through raw fallback.

## Open Decisions

- Whether Admin query should join `progression_ledger` history in the first version or expose only compact read model.
- Whether `firstClearSummary` belongs in story progress detail or in a separate reward/support query.
- Whether `narrativeVersion` is global Act-level only at launch or can be operation-level in the same response.
- Whether raid progress advances on clear only, participation, or season claim.
- Whether `STORY_PROGRESS_READ` should be separate from broader player support read permissions at launch.

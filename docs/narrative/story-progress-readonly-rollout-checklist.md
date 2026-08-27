# Story Progress Read-Only Rollout Checklist

## Purpose

This checklist coordinates the first implementation pass for story progress read-only visibility across Server, Unity, and Admin.

The contract source is `docs/narrative/story-progress-readonly-api-admin-query.md`. This checklist is a sequencing aid, not a replacement for that contract.

## Rollout Order

### 1. Server Game API Shape

First implementation target:

- `GET /v1/story-progress`

Why this comes first:

- Server owns durable story progress and unlock truth.
- Unity DTOs and Admin query mapping should follow the server read shape instead of inventing parallel models.
- Admin API can reuse the same stable status codes and progression order after Game API response compatibility is fixed.

Required checks:

- Bearer auth required.
- Account id from authenticated session context only.
- Request body not accepted.
- Query/body account id rejected or explicitly ignored by tested policy. Preferred: reject with `contract_violation`.
- Empty account returns accepted `none` progress default.
- Existing account row returns `account_story_progress` state and `account_unlocks`.
- Full localized dialogue text is not returned.
- Token, secret, raw request body, raw RNG material, reward calculation internals, and account shortcut data are not returned.

### 2. Unity Display/Debug DTO Boundary

Second implementation target:

- Pure C# story progress DTO/presentation/debug boundary.

Why this waits for Server shape:

- Unity should display server read context, not calculate story truth.
- Unity already owns `MissionNarrativeCatalog` for local presentation keyed by stable mission id.

Required checks:

- Preserves stable ids.
- Supports `none` progress.
- Handles unknown ids/statuses with safe fallback.
- Does not calculate operation unlock truth.
- Does not calculate first clear eligibility.
- Does not calculate reward, offline reward, multiplier, storage cap, or story progress multiplier.
- Does not add real HTTP, secure storage, or UI.

### 3. Admin Read-Only Mapping

Third implementation target:

- Connect existing story/narrative skeleton fields to read-only Admin query after server Admin API exists.

Until then:

- Keep values as `연동 대기`.
- Use stable labels and raw fallback.
- Do not add mutation controls.

Required checks:

- No narrative editing.
- No localization publishing.
- No story progress mutation.
- No unlock mutation.
- No reward repair.
- No economy correction.
- Unknown backend id/status/code remains visible.

## Shared Stable Status Codes

- `draft_exists`
- `localization_pending`
- `fixture_pending`
- `version_pending`
- `content_missing`
- `version_mismatch`
- `ready`

Display labels:

- `draft_exists` -> `초안 존재`
- `localization_pending` -> `현지화 대기`
- `fixture_pending` -> `fixture 대기`
- `version_pending` -> `버전 대기`
- `content_missing` -> `콘텐츠 누락`
- `version_mismatch` -> `버전 불일치`
- `ready` -> `준비 완료`

## Shared Act 1 Defaults

- `actId`: `act_01`
- `narrativeVersion`: `act1.2026-08-27.001`
- empty progress id: `none`
- empty progress order: `0`
- first unlock candidate: `opn_01_last_departure`

Act 1 order:

1. `opn_01_last_departure`
2. `opn_02_first_signal`
3. `opn_03_water_stop`
4. `opn_04_scrapstar_show`
5. `opn_05_night_boarders`
6. `opn_06_broken_line`
7. `raid_raider_king_engine`

## Integration ACK Criteria

Server completion is accepted when:

- `GET /v1/story-progress` local tests pass.
- response shape matches the contract.
- existing run finish/reward/gacha smoke still passes.
- forbidden fields are covered by tests or smoke.

Unity completion is accepted when:

- Pure C# tests pass.
- DTO/presentation/debug boundary matches server read contract.
- no-calculation/no-transport boundaries remain tested.

Admin completion is accepted when:

- Skeleton/status labels match contract.
- tests/typecheck/build pass.
- preview smoke stays behind Access.
- no mutation/API integration is opened early.

## Next Decision After This Rollout

After Game API read shape is stable, choose one:

1. Add Admin read-only API endpoints.
2. Add Unity story progress DTO/diagnostic adapter if not already done.
3. Define offline reward preview/claim API using server-stored highest story progress.

# Narrative Localization And Versioning Policy

## Purpose

This document defines the first-pass naming and versioning policy for LAST RAIL narrative text.

The current Act 1 dialogue documents are narrative candidates. They are not finalized localized string IDs, and they must not become economy, auth, combat, reward, unlock, or story progress Source of Truth. This policy explains how those candidates can later be promoted into stable localization data without blurring Server, Unity, and Admin ownership boundaries.

## Core Principles

- Stable mission ids are gameplay/data references. Localized string ids are presentation references.
- Narrative text can change without changing operation ids, reward contracts, gacha contracts, or run finish contracts.
- Server owns durable story progress, unlock truth, first clear, reward preview, and offline reward inputs.
- Unity owns localized text lookup, narrative playback, and fixture/catalog presentation.
- Admin owns read-only visibility into content/version/missing-state first.
- Narrative versions should help diagnose content compatibility. They should not control reward eligibility or economy mutation.

## Id Families

### Stable Mission Id

Stable mission ids identify operation/story progress milestones.

Examples:

- `opn_01_last_departure`
- `opn_02_first_signal`
- `opn_03_water_stop`
- `opn_04_scrapstar_show`
- `opn_05_night_boarders`
- `opn_06_broken_line`
- `raid_raider_king_engine`

Rules:

- Must remain lowercase snake_case.
- Must be stable across localization changes.
- May be used by Server, Unity, and Admin.
- Must not contain locale, text section, version, or reward information.

### Localized String Id

Localized string ids identify display text entries.

Recommended format:

`narrative.<missionId>.<surface>.<slot>`

Examples:

- `narrative.opn_01_last_departure.briefing.summary`
- `narrative.opn_01_last_departure.briefing.line_001`
- `narrative.opn_01_last_departure.commander_response.option_001`
- `narrative.opn_01_last_departure.mission_start.copy`
- `narrative.opn_01_last_departure.loading.hint_001`
- `narrative.opn_01_last_departure.callout.opening_001`
- `narrative.opn_01_last_departure.callout.escalation_001`
- `narrative.opn_01_last_departure.callout.optional_objective_001`
- `narrative.opn_01_last_departure.callout.final_wave_001`
- `narrative.opn_01_last_departure.result.title`
- `narrative.opn_01_last_departure.result.summary`
- `narrative.opn_01_last_departure.reward_framing.first_clear`
- `narrative.opn_01_last_departure.reward_framing.repeat_clear`
- `narrative.opn_01_last_departure.codex.title`
- `narrative.opn_01_last_departure.codex.body`

Rules:

- Use lowercase snake_case for mission ids, surfaces, and slots.
- Use zero-padded numeric suffixes for ordered lines: `line_001`, `line_002`.
- Keep ids semantic enough for tooling but not so specific that copy edits require id churn.
- Do not include locale in the id. Locale belongs in the localization resource path or record metadata.
- Do not include quantities, reward codes, ledger ids, probability, token, account id, or secret-like material in ids.
- If a line is removed, do not recycle the old id for unrelated text in the same narrative version family.

## Surface Names

Use these first-pass surface names:

| Surface | Intended use |
| --- | --- |
| `briefing` | Pre-mission summary and NPC briefing lines |
| `commander_response` | Player-facing response options |
| `mission_start` | Start button copy |
| `loading` | Loading screen hint text |
| `callout` | In-mission combat or objective lines |
| `result` | Post-mission title and summary |
| `reward_framing` | Qualitative first/repeat reward explanation |
| `codex` | Archive/codex title and body |

Use specific slots under the surface rather than inventing new surface names too quickly.

Recommended callout slots:

- `opening_001`
- `tutorial_001`
- `escalation_001`
- `optional_objective_001`
- `major_turn_001`
- `final_wave_001`
- `victory_001`
- `failure_001`

Raid-specific callouts may use phase slots:

- `phase_1_001`
- `phase_2_001`
- `phase_3_001`

## Locale Resource Shape

Future localization files can use either flat key-value records or grouped records. The first implementation should favor flat records because they are easy to diff, search, and compare across locales.

Example flat entry:

```json
{
  "id": "narrative.opn_01_last_departure.briefing.summary",
  "locale": "ko-KR",
  "text": "정비역의 마지막 출발 창구가 닫히고 있다.",
  "narrativeVersion": "act1.2026-08-30.001"
}
```

Do not store reward quantities, economy grants, raw request bodies, tokens, secrets, raw RNG material, account shortcut fields, or internal probability data in localization resources.

## Narrative Version

Recommended first-pass format:

`<scope>.<date>.<revision>`

Examples:

- `act1.2026-08-30.001`
- `opn_01_last_departure.2026-08-30.001`
- `raid_raider_king_engine.2026-08-30.001`

Recommended launch policy:

- Use `act1.<date>.<revision>` for Act-wide fixture/catalog compatibility.
- Optionally allow operation-level override later if one mission needs emergency localization repair.
- Keep `narrativeVersion` visible as diagnostic/read-only metadata.
- Do not use narrativeVersion as reward eligibility, first clear eligibility, unlock truth, or offline reward multiplier input.

## Version Bump Rules

Patch-like revision bump inside the same date or next date:

- Typo fixes.
- Minor line polishing.
- Punctuation changes.
- Non-semantic label cleanup.

New revision with compatibility note:

- Adding or removing localized string ids.
- Moving text between surfaces.
- Changing mission start copy.
- Changing result title/summary.
- Changing reward framing language.
- Changing optional objective text that UI may reference.

Act-level compatibility review required:

- Reordering Act 1 story progress.
- Renaming stable mission ids.
- Changing a mission's story role.
- Changing ownership boundaries between Server, Unity, and Admin.
- Making optional objective text authoritative for scoring.
- Adding client-visible server narrative read model fields.

## Server Boundary

Server may store or return:

- `narrativeVersion`
- stable mission id
- story progress id
- narrative content status
- localization status
- missing content status

Server should not own:

- full localized dialogue text
- cutscene timing
- callout playback timing
- UI layout
- copy fallback selection

Server must not trust client-supplied:

- `storyProgress`
- `highestStoryProgressId`
- `unlockedOperations`
- `narrativeFlags`
- `narrativeVersion` as Source of Truth

If future client requests attempt to write story truth or unlock truth through narrative fields, reject them with a stable contract error such as `contract_violation`.

## Unity Boundary

Unity may own:

- `MissionNarrativeDefinition`
- `MissionNarrativeCatalog`
- localized string lookup
- local narrative fixtures
- briefing/callout/result/codex playback
- fallback display for unknown or missing ids

Unity should not calculate or own:

- durable story progress
- operation unlock truth
- first clear eligibility
- reward amount
- reward multiplier
- offline reward amount
- offline storage cap
- story progress multiplier

Unity fixtures should keep narrative text qualitative and should not include reward quantities, economy fixture values, gacha ownership language, token/secret material, raw RNG material, or server-internal diagnostics.

## Admin Boundary

Admin may display read-only:

- stable mission id
- display label
- coverage status
- localization status
- fixture status
- narrative version
- missing content state
- player support context

Admin should not initially provide:

- narrative editing
- localization publishing
- story progress mutation
- unlock mutation
- reward repair mutation
- economy correction from narrative screens

Unknown backend ids and codes should remain visible through raw fallback.

## Missing-State Labels

Recommended stable status candidates:

- `draft_exists`
- `localization_pending`
- `fixture_pending`
- `version_pending`
- `content_missing`
- `version_mismatch`
- `ready`

Suggested Korean display labels:

- `draft_exists` -> `초안 존재`
- `localization_pending` -> `현지화 대기`
- `fixture_pending` -> `fixture 대기`
- `version_pending` -> `버전 대기`
- `content_missing` -> `콘텐츠 누락`
- `version_mismatch` -> `버전 불일치`
- `ready` -> `준비 완료`

## Act 1 Initial Version Candidate

Use this candidate until a formal localization pipeline replaces it:

- Scope: `act1`
- Version: `act1.2026-08-30.001`
- Covered ids:
  - `opn_01_last_departure`
  - `opn_02_first_signal`
  - `opn_03_water_stop`
  - `opn_04_scrapstar_show`
  - `opn_05_night_boarders`
  - `opn_06_broken_line`
  - `raid_raider_king_engine`

This version marks Act 1 launch fixture compatibility only. It does not indicate production localization readiness, reward eligibility, unlock truth, or economy truth.

## Future Contract Checks

Before connecting narrative localization to server/client/admin contracts, verify:

- Every stable mission id has required string ids for briefing, start copy, loading hint, result title, and result summary.
- Unity catalog lookup fails soft for unknown ids and missing strings.
- Admin missing-state display uses stable status codes with raw fallback.
- Server read models expose version/status metadata without owning full localized text.
- Story progress replay cannot be affected by narrativeVersion changes.
- Offline reward calculations do not use local Unity narrative state.
- Reward and gacha systems remain independent from localized copy.

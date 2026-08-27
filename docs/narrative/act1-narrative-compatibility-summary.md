# Act 1 Narrative Compatibility Summary

## Purpose

This document summarizes the Act 1 narrative draft set and the cross-team compatibility boundaries for Server, Unity, and Admin work.

Act 1 text currently exists as narrative candidates, not finalized localized string IDs. These drafts should guide mission briefing, callouts, result copy, codex entries, future narrative fixtures, and read-only operational visibility without becoming economy, auth, combat, or reward Source of Truth.

## Act 1 Draft Coverage

| Order | Stable id | Korean title | Start copy | Story role |
| --- | --- | --- | --- | --- |
| 0 | `none` | 스토리 진행 없음 | N/A | No durable Act 1 progress yet |
| 1 | `opn_01_last_departure` | 마지막 출발 | 마지막 출발 시작 | The train leaves as an unfinished escape vehicle and becomes responsible for the people it waits for |
| 2 | `opn_02_first_signal` | 첫 신호 | 첫 신호 복원 | The train chooses to answer an uncertain signal and learns the King's Engine is tied to the broken line |
| 3 | `opn_03_water_stop` | 급수 정차 | 급수 정거장 진입 | The train proves it can gain survival resources through repair, trust, and protection instead of seizure |
| 4 | `opn_04_scrapstar_show` | 스크랩스타 쇼 | 스크랩스타 진입 | The Raiders' spectacle economy is revealed and interrupted by rescuing people during combat |
| 5 | `opn_05_night_boarders` | 야간 승선전 | 야간 승선전 시작 | The threat enters the train and passengers become a community with roles, not instant combat ownership |
| 6 | `opn_06_broken_line` | 끊어진 선로 | 끊어진 선로 복구 | The first signal becomes inheritance: do not save the engine and abandon the people |
| 7 | `raid_raider_king_engine` | 약탈왕 기관차 | 왕관 기관차 요격 | The Crown Engine is stopped without adopting its disposable-people philosophy |

## Mission Draft Files

- `docs/narrative/opn_01_last_departure_dialogue.md`
- `docs/narrative/opn_02_first_signal_dialogue.md`
- `docs/narrative/opn_03_water_stop_dialogue.md`
- `docs/narrative/opn_04_scrapstar_show_dialogue.md`
- `docs/narrative/opn_05_night_boarders_dialogue.md`
- `docs/narrative/opn_06_broken_line_dialogue.md`
- `docs/narrative/raid_raider_king_engine_dialogue.md`

## Related Policy Files

- `docs/narrative/story-processing-architecture.md`
- `docs/narrative/narrative-localization-versioning.md`

## Reward Framing Compatibility

Narrative reward language must stay qualitative. It can describe trust, inherited supplies, route records, recovered support systems, stabilized infrastructure, and repeat patrol recovery, but it must not encode quantities, final economy values, ledger mutations, or claim eligibility.

| Stable id | First clear framing | Repeat clear framing |
| --- | --- | --- |
| `opn_01_last_departure` | Emergency supplies, trust, and one-time preserved station cache | Residual station supplies and maintenance recovery |
| `opn_02_first_signal` | Route intelligence, signal maps, and recovered cache | Signal maintenance data and route salvage |
| `opn_03_water_stop` | Shared supply trust and station support | Water station exchange and upkeep supplies |
| `opn_04_scrapstar_show` | Freed storehouse, repair material, and route data sharing | Residual market exchange, parts, and data recovery |
| `opn_05_night_boarders` | Passenger support systems, emergency equipment, and internal defense records | Carriage repair, upkeep supplies, and defense data |
| `opn_06_broken_line` | Inherited route record and one-time preserved cache | Temporary line maintenance material and route data recovery |
| `raid_raider_king_engine` | Recovered route records, freed support systems, and preserved command data | Patrol, stabilization, and route data recovery |

Server remains the Source of Truth for reward preview, first clear eligibility, reward claim, economy mutation, and offline reward calculation. Unity and Admin may display server-provided reward lines or future display metadata, but they should not calculate reward amounts.

## Optional Objective Candidates

Optional objective language is currently narrative/UX scaffolding only. It should not imply launch scoring, reward calculation, or server contract changes until explicitly implemented.

| Stable id | Optional objective candidates |
| --- | --- |
| `opn_01_last_departure` | passenger boarding, engine startup, platform defense |
| `opn_02_first_signal` | mast integrity, clean signal recovery, relay protection |
| `opn_03_water_stop` | pump preservation, civilian zone safety, station trust |
| `opn_04_scrapstar_show` | broadcast override, captive rescue, arena hazard control |
| `opn_05_night_boarders` | carriage defense, infirmary safety, passenger evacuation routes |
| `opn_06_broken_line` | repair crew safety, bridge damage control, signal integrity |
| `raid_raider_king_engine` | prisoner car preserved, broadcast relay captured, supply car stabilized, fuel core stable, engine integrity |

## Ownership Boundaries

### Server

- Owns durable story progress and operation unlock truth.
- Owns accepted run finish story progress update when implemented.
- Owns first clear scope and reward preview generation.
- Owns offline reward progress input via server-stored highest story progress.
- Must not trust client-supplied `storyProgress`, `highestStoryProgressId`, `unlockedOperations`, narrative flags, reward amounts, or reward multipliers.
- Should treat this document as narrative metadata/localization guidance, not a route implementation requirement by itself.

### Unity

- Owns narrative presentation and localized text playback.
- May use these drafts for `MissionNarrativeDefinition` fixtures, catalog lookup, briefing UI, loading hints, in-combat callouts, result text, and codex entries.
- Does not own story progress truth, operation unlock truth, first clear eligibility, reward calculation, offline reward calculation, or economy mutation.
- Should preserve stable mission ids and use fallback behavior for unknown ids.
- Should avoid reward quantities, economy fixture values, gacha ownership language, and server-contract assumptions in local narrative fixtures.

### Admin

- Owns read-only operational visibility first.
- May surface mission id, story progress, narrative content status, localization status, missing-data state, narrative version, and player support context after server read models exist.
- Should not open narrative editing, localization publishing, story progress mutation, reward repair mutation, or economy correction from narrative screens in the initial scope.
- Should keep unknown backend ids/codes visible through raw fallback.

## Stable Id And Label Alignment

The following labels are aligned across current planning, Unity display helpers, and Admin display helpers:

- `none` -> `스토리 진행 없음`
- `opn_01_last_departure` -> `마지막 출발`
- `opn_02_first_signal` -> `첫 신호`
- `opn_03_water_stop` -> `급수 정차`
- `opn_04_scrapstar_show` -> `스크랩스타 쇼`
- `opn_05_night_boarders` -> `야간 승선전`
- `opn_06_broken_line` -> `끊어진 선로`
- `raid_raider_king_engine` -> `약탈왕 기관차`

## Compatibility Checks For Future Work

Before promoting these drafts into implementation contracts, verify:

- Server story progress replay is idempotent and cannot regress higher progress.
- Duplicate run finish replay does not reapply story progress, first clear, reward preview, or optional objectives.
- Unity narrative fixtures include stable ids and no reward/economy/gacha ownership language.
- Admin read-only status does not imply editing or mutation rights.
- Offline rewards use server-stored highest story progress, not local Unity state or request body values.
- Optional objective text remains non-authoritative until scoring contracts exist.

## Recommended Next Slices

1. Server: accepted run finish story progress update contract slice.
2. Unity: Act 1 `MissionNarrativeCatalog` and fixtures for opn_02 through raid.
3. Admin: Act 1 narrative coverage/read-only status skeleton.
4. Planning: story progress read-only API/Admin query contract.

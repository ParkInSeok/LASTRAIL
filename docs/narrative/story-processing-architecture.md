# Story Processing Architecture

## Purpose

This document defines how LAST RAIL should process story content across server, Unity, and admin surfaces.

The guiding rule is:

- Server owns durable story progress and unlock truth.
- Unity owns narrative presentation and localized text playback.
- Admin owns read-only operational visibility first.

Story text should not become an economy, auth, or combat contract dependency unless a future feature explicitly requires it.

## Current Scope

This architecture covers:

- Act and operation story progress.
- Mission briefing text.
- Loading hints.
- In-combat callouts.
- Victory, failure, and result text.
- Codex/archive entries.
- First clear and repeat reward framing.
- Narrative status visibility for admin.

This architecture does not require immediate API, DB, or Unity implementation. It is the compatibility target for future implementation slices.

## Server Responsibilities

The server is the Source of Truth for story progress, eligibility, and unlock state.

### Server Owns

- Account story progress.
- Operation clear state.
- First clear state.
- Act and operation unlock state.
- Durable narrative milestone flags.
- Story progress updates caused by accepted run finish events.
- Validation that client-supplied story progress is not trusted.
- Optional narrative version metadata for compatibility checks.

### Server Does Not Own

- Full localized dialogue text.
- Combat callout timing.
- Cutscene playback.
- UI layout for briefing or results.
- Long-form codex prose as an API payload.

### Candidate Server State

The exact storage can be decided later. The preferred future shape is a dedicated account story progress model instead of overloading reward or runtime state.

Candidate fields:

- `account_id`
- `act_id`
- `highest_cleared_operation_id`
- `highest_story_progress_id`
- `cleared_operations_json`
- `story_flags_json`
- `narrative_version`
- `updated_at`

### Candidate Server Response Shape

Server responses that need story context should return stable ids and state, not full prose.

Example:

```json
{
  "actId": "act_01",
  "operationId": "opn_03_water_stop",
  "storyProgress": "opn_03_water_stop",
  "firstClear": true,
  "narrativeFlags": ["water_station_trusted"],
  "unlockedOperations": ["opn_04_scrapstar_show"],
  "narrativeVersion": "act1.v1"
}
```

## Unity Responsibilities

Unity is the Source of Presentation for story content.

### Unity Owns

- Mission briefing UI.
- Localized string lookup.
- In-combat callout playback.
- Loading hint display.
- Victory/failure/result text display.
- Codex/archive entry display.
- Cutscene or lightweight event timing.
- Narrative text fixtures and adapters.

### Unity Does Not Own

- Story progress truth.
- First clear eligibility.
- Reward calculation.
- Offline reward calculation.
- Operation unlock truth.
- Account milestone truth.

### Candidate Unity Model

Unity should eventually use local data keyed by stable ids.

Candidate model:

```text
MissionNarrativeDefinition
- missionId
- displayName
- briefingSummary
- briefingLines
- commanderResponseCandidates
- missionStartButtonCopy
- loadingHint
- openingCallouts
- tutorialAdjacentLines
- escalationCallouts
- optionalObjectiveCallouts
- finalWaveCallouts
- victoryLines
- failureLines
- resultTitle
- resultSummary
- firstClearRewardFraming
- repeatClearRewardFraming
- codexEntry
```

The local data can come from JSON, ScriptableObject, generated C# fixtures, or another content pipeline. The first implementation slice should be small and testable, likely one operation such as `opn_01_last_departure`.

## Admin Responsibilities

Admin should start with read-only visibility.

### Admin Owns Initially

- Operation id display.
- Story progress display.
- First clear state visibility.
- Mission narrative status visibility.
- Localization/narrative data missing-state visibility.
- Read-only support context for player reports.

### Admin Does Not Own Initially

- Narrative editing.
- Localization publishing.
- Story progress mutation.
- Reward repair mutation.
- Cutscene or callout timing.

Admin editing tools can be considered later, after the story data pipeline and permissions are stable.

## Stable Id Policy

Stable ids remain English and API-safe. Player-facing labels can be Korean.

Act 1 ids:

- `act_01`
- `opn_01_last_departure`
- `opn_02_first_signal`
- `opn_03_water_stop`
- `opn_04_scrapstar_show`
- `opn_05_night_boarders`
- `opn_06_broken_line`
- `raid_raider_king_engine`

Current display labels:

- `opn_01_last_departure`: `마지막 출발`
- `opn_02_first_signal`: `첫 신호`
- `opn_03_water_stop`: `급수 정차`
- `opn_04_scrapstar_show`: `스크랩스타 쇼`
- `opn_05_night_boarders`: `야간 승선전`
- `opn_06_broken_line`: `끊어진 선로`
- `raid_raider_king_engine`: `약탈왕 기관차`

Unknown backend ids should remain visible as raw fallback in tools and diagnostics. They should not be silently hidden.

## Narrative Content Source

The current narrative source documents are planning artifacts, not finalized localized string ids:

- `docs/narrative/act1-story-outline.md`
- `docs/narrative/opn_01_last_departure_dialogue.md`
- `docs/narrative/opn_02_first_signal_dialogue.md`
- `docs/narrative/opn_03_water_stop_dialogue.md`
- `docs/narrative/opn_04_scrapstar_show_dialogue.md`

Future operation dialogue documents should follow the same structure until a dedicated content pipeline exists.

## Reward Framing Policy

Narrative text may explain reward meaning, but must not encode actual reward quantities or economy values.

Allowed:

- "정비역 비상 캐시"
- "경로 데이터"
- "공유 보급"
- "정비 자재"
- "잔존 보급품"

Avoid:

- Specific reward amounts.
- Exact multiplier values in story text.
- Gacha ownership language.
- Captives or recruited operators described as purchased goods.
- Loot/conquest framing when the story intent is rescue, trust, repair, or inheritance.

First clear rewards should feel like one-time trust, inheritance, route intelligence, or recovered caches.

Repeat rewards should feel like ongoing exchange, maintenance supply, salvage, or route support.

## Interaction With Existing Systems

### Run Finish

Accepted run finish remains the durable moment that can update clear state, first clear state, and future story progress.

Story progress updates should be idempotent and replay-safe.

### Reward Preview / Claim

Reward preview and claim remain server Source of Truth. Narrative text may frame rewards, but must not control reward amounts.

### Offline Rewards

Offline reward production can use highest story progress as a server-side input. Unity and admin should display story progress and cap information without calculating reward amounts.

### Gacha / Recruitment

Recruitment should be framed as assignment, rescue, defection, or voluntary boarding. Avoid language that implies the player owns people.

### Ops / Diagnostics

Diagnostics may include stable ids such as operation id, story progress id, and narrative version. They must not include tokens, secrets, raw request bodies, raw RNG material, or hidden probability details.

## Recommended Implementation Sequence

1. Finish Act 1 dialogue drafts through `opn_06_broken_line` and `raid_raider_king_engine`.
2. Write an Act 1 narrative compatibility summary:
   - mission id
   - display label
   - narrative purpose
   - reward framing
   - first clear framing
   - forbidden wording
3. Add a Unity pure narrative definition model for one operation.
4. Add local Unity fixture/adapter for `opn_01_last_departure`.
5. Add server story progress read model design.
6. Add server story progress update on accepted run finish.
7. Add admin read-only story progress/narrative status skeleton.

## Open Decisions

- Should the Commander speak directly, or only through response prompt labels?
- Should narrative content live in Unity JSON, ScriptableObjects, generated C# fixtures, or remote content later?
- Should server store `narrative_version` per account, per operation, or per balance config?
- Should admin ever support narrative editing, or only read-only validation?
- Should codex/archive entries unlock at accepted finish or at reward claim?


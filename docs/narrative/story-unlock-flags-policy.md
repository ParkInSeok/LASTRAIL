# Story Unlock Flags Policy

## Purpose

This document defines Act 1 story progress, unlock ids, and story flag policy for LAST RAIL.

The goal is to keep durable progression server-owned while giving Unity, Admin, and customer support stable ids for display, diagnostics, and read-only investigation.

## Ownership

- Server owns durable story progress, operation unlock truth, feature unlock truth, and story flag truth.
- Unity owns presentation, local narrative fixture lookup, localized text playback, and debug display.
- Admin owns read-only visibility for support and operations.
- Customer support reads server records and does not mutate story progress or unlocks at launch.

Client-supplied `storyProgress`, `unlockedOperations`, `storyFlags`, `narrativeFlags`, `featureUnlocks`, or local completion data are not source of truth.

## Act 1 Progression Order

| Order | Story Progress Id | Display Label | Progress Type |
| ---: | --- | --- | --- |
| 0 | `none` | 스토리 진행 없음 | Initial |
| 1 | `opn_01_last_departure` | 마지막 출발 | Operation clear |
| 2 | `opn_02_first_signal` | 첫 신호 | Operation clear |
| 3 | `opn_03_water_stop` | 급수 정차 | Operation clear |
| 4 | `opn_04_scrapstar_show` | 스크랩스타 쇼 | Operation clear |
| 5 | `opn_05_night_boarders` | 야간 승선전 | Operation clear |
| 6 | `opn_06_broken_line` | 끊어진 선로 | Operation clear |
| 7 | `raid_raider_king_engine` | 약탈왕 기관차 | Raid milestone |

Ordinary operation progress advances only on:

- server-accepted run finish
- `outcome = VICTORY`
- `clearedWaves = 15`
- operation id is higher than currently stored progress

Lower operation clears must not regress progress.

Duplicate finish replay must not reapply story progress, unlocks, or flags.

## Raid Progress Condition

Launch recommendation:

- `raid_raider_king_engine` story progress advances on verified raid clear.
- Participation alone does not advance Act 1 completion.
- Season claim alone does not advance Act 1 completion.

Reasoning:

- The Act 1 raid is a narrative milestone, not a passive participation reward.
- Offline reward cap/multiplier reaches its Act 1 maximum at this milestone, so it should require durable clear evidence.
- Support can explain the condition clearly: the Crown Engine must be stopped, not merely encountered.

Future raid variants may add participation flags, but those should not equal story progress completion.

## Operation Unlock Ids

Operation unlock ids use the existing granular unlock pattern:

```text
operation:<operationId>
```

Launch operation unlock chain:

| Trigger Progress | Unlock Id | Unlocks |
| --- | --- | --- |
| account creation | `operation:opn_01_last_departure` | `opn_01_last_departure` |
| `opn_01_last_departure` clear | `operation:opn_02_first_signal` | `opn_02_first_signal` |
| `opn_02_first_signal` clear | `operation:opn_03_water_stop` | `opn_03_water_stop` |
| `opn_03_water_stop` clear | `operation:opn_04_scrapstar_show` | `opn_04_scrapstar_show` |
| `opn_04_scrapstar_show` clear | `operation:opn_05_night_boarders` | `opn_05_night_boarders` |
| `opn_05_night_boarders` clear | `operation:opn_06_broken_line` | `opn_06_broken_line` |
| `opn_06_broken_line` clear | `operation:raid_raider_king_engine` | `raid_raider_king_engine` |

Clearing `raid_raider_king_engine` completes the Act 1 progression milestone but does not automatically unlock Act 2 until Act 2 ids are defined.

## Feature Unlock Ids

Feature unlock ids should use:

```text
feature:<featureId>
```

Launch candidate feature ids:

| Trigger Progress | Feature Unlock Id | Meaning |
| --- | --- | --- |
| `opn_01_last_departure` | `feature:train_manifest` | Basic train/account status view |
| `opn_02_first_signal` | `feature:signal_decoder` | Signal/log/codex interpretation surface |
| `opn_03_water_stop` | `feature:water_reserve` | Water reserve/offline continuity explanation |
| `opn_04_scrapstar_show` | `feature:market_exchange` | Controlled exchange/shop candidate |
| `opn_05_night_boarders` | `feature:carriage_defense` | Train internal defense/support candidate |
| `opn_06_broken_line` | `feature:route_reconstruction` | Route reconstruction/Act 1 finale prep |
| `raid_raider_king_engine` | `feature:act1_after_report` | Act 1 completion/codex after report |

These are stable candidates. They should not be used for economy mutation until the relevant system contract exists.

## Train Car Unlock Ids

Train car unlock ids should use:

```text
train_car:<carId>
```

Launch candidate car ids:

| Trigger Progress | Train Car Unlock Id | Narrative Meaning |
| --- | --- | --- |
| `opn_01_last_departure` | `train_car:rear_coach` | First survivor carriage stabilized |
| `opn_02_first_signal` | `train_car:signal_room` | Signal listening post restored |
| `opn_03_water_stop` | `train_car:water_tank` | Water storage becomes reliable |
| `opn_04_scrapstar_show` | `train_car:workshop` | Salvage/workshop capability restored |
| `opn_05_night_boarders` | `train_car:infirmary` | Passenger care and emergency station |
| `opn_06_broken_line` | `train_car:route_table` | Route records and reconstruction table |
| `raid_raider_king_engine` | `train_car:freed_car` | Freed support systems from Raider King |

Train car unlocks are story/feature visibility candidates. They are not item grants.

## Story Flags

Story flags should use:

```text
story_flag:<flagId>
```

Launch required flags:

| Trigger Progress | Story Flag Id | Meaning |
| --- | --- | --- |
| `opn_01_last_departure` | `story_flag:last_departure_survivors_boarded` | First survivors boarded |
| `opn_02_first_signal` | `story_flag:first_signal_recovered` | First signal confirmed as real |
| `opn_03_water_stop` | `story_flag:water_pact_established` | Water stop resolved without looting |
| `opn_04_scrapstar_show` | `story_flag:scrapstar_broadcast_interrupted` | Arena broadcast interrupted |
| `opn_05_night_boarders` | `story_flag:night_boarders_repulsed` | Train interior defended |
| `opn_06_broken_line` | `story_flag:broken_line_reconnected` | Broken line route restored |
| `raid_raider_king_engine` | `story_flag:raider_king_engine_stopped` | Crown Engine stopped |

Optional flags may exist later, but launch story progress should not depend on optional objective flags.

## Optional Objective Flags

Optional objective flags should use:

```text
optional_flag:<missionId>:<objectiveId>
```

Act 1 candidate optional flags:

| Mission | Optional Flag | Meaning |
| --- | --- | --- |
| `opn_03_water_stop` | `optional_flag:opn_03_water_stop:pump_station_intact` | Pump station preserved |
| `opn_04_scrapstar_show` | `optional_flag:opn_04_scrapstar_show:captive_section_secured` | Captive section secured |
| `opn_04_scrapstar_show` | `optional_flag:opn_04_scrapstar_show:broadcast_override_complete` | Broadcast override completed |
| `opn_05_night_boarders` | `optional_flag:opn_05_night_boarders:infirmary_protected` | Infirmary protected |
| `opn_05_night_boarders` | `optional_flag:opn_05_night_boarders:passenger_panic_contained` | Passenger panic contained |
| `opn_06_broken_line` | `optional_flag:opn_06_broken_line:bridge_integrity_preserved` | Bridge integrity preserved |
| `opn_06_broken_line` | `optional_flag:opn_06_broken_line:signal_record_complete` | Signal record fully recovered |
| `raid_raider_king_engine` | `optional_flag:raid_raider_king_engine:prisoner_car_preserved` | Prisoner car preserved |
| `raid_raider_king_engine` | `optional_flag:raid_raider_king_engine:broadcast_relay_captured` | Broadcast relay captured |
| `raid_raider_king_engine` | `optional_flag:raid_raider_king_engine:supply_car_stabilized` | Supply car stabilized |
| `raid_raider_king_engine` | `optional_flag:raid_raider_king_engine:fuel_core_stable` | Fuel core stable |
| `raid_raider_king_engine` | `optional_flag:raid_raider_king_engine:engine_integrity` | Engine integrity preserved |

Launch recommendation:

- Optional flags are diagnostic/narrative scoring candidates.
- They should not block ordinary story progress.
- They should not grant rare rewards until scoring/reward contracts are defined.
- They should not drive offline reward multipliers.

## Unlock Transaction Rules

When progress advances, future server implementation should atomically write:

- account mutation record with story progress type
- `account_story_progress` update
- `account_unlocks` insert-or-ignore for next operation
- feature/train car unlock insert-or-ignore if enabled
- `progression_ledger` immutable entry
- idempotency record

Duplicate replay should return stored response and must not duplicate:

- account mutation
- progression ledger
- account unlock
- feature unlock
- story flag

## Admin Read-Only Fields

Admin should expose:

- current story progress id
- highest cleared operation id
- unlocked operation ids
- feature unlock ids
- train car unlock ids
- required story flags
- optional objective flags if implemented
- narrative version
- last progress run id
- no-regress status
- duplicate replay status

Admin should not expose mutation controls at launch.

## Unity Presentation Fields

Unity may use:

- stable mission id
- story progress id
- unlocked operation ids from server
- local narrative fixture/catalog keyed by mission id
- localized string id coverage
- narrative version for diagnostics

Unity must not:

- calculate durable unlock truth
- calculate first clear eligibility
- calculate reward/offline reward
- trust local optional objective flags as server truth
- unlock operations based only on local client state

## Support Interpretation

Support can safely explain:

- Story progress advances from server-accepted full victory records.
- Lower operation clears do not reduce stored progress.
- Optional objectives are not launch story progress requirements unless later contracts say otherwise.
- Raid Act 1 completion requires verified raid clear.
- Narrative content status is separate from story progress truth.

Support should escalate:

- full victory but no expected unlock
- duplicate replay created extra progress
- lower clear regressed progress
- raid clear did not update Act 1 milestone
- server progress and progression ledger disagree

## Open Decisions

- Whether feature/train car unlock ids should be stored in `account_unlocks` or a dedicated table.
- Whether optional objective flags need a separate score table.
- Whether `raid_raider_king_engine` clear should also unlock an Act 2 preview operation.
- Whether Act 2 unlock id should be `act:act_2` or `operation:<first_act2_operation>`.
- Whether story flags should be returned in Game API `GET /v1/story-progress` at launch or Admin-only first.
- Whether optional flags ever affect first clear reward lines.

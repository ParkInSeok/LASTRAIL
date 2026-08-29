# Act 1 Localization String-Id Map Draft

## Purpose

This document maps Act 1 launch narrative surfaces to stable localization string-id candidates.

It is a copy/localization planning artifact only. It does not create a server API, Unity implementation requirement, reward contract, gacha probability contract, unlock rule, or economy truth.

## Source Documents

- `docs/narrative/narrative-localization-versioning.md`
- `docs/narrative/opn_01_last_departure_dialogue.md`
- `docs/narrative/opn_02_first_signal_dialogue.md`
- `docs/narrative/opn_03_water_stop_dialogue.md`
- `docs/narrative/opn_04_scrapstar_show_dialogue.md`
- `docs/narrative/opn_05_night_boarders_dialogue.md`
- `docs/narrative/opn_06_broken_line_dialogue.md`
- `docs/narrative/raid_raider_king_engine_dialogue.md`
- `docs/narrative/act1-operator-voice-guide.md`

## Launch Guardrails

- Rail Choice Events are excluded from Act 1 launch. Do not add `rail_choice` ids to launch coverage.
- World Raid is future/backlog. Do not add ranking, settlement, score bracket, season, leaderboard, or participation ids to launch coverage.
- Do not encode reward quantities, economy values, ticket counts, pity state, probability, ownership state, ledger ids, account ids, tokens, secrets, or server calculation details in string ids.
- Reward framing ids may exist only for qualitative copy. They must not imply eligibility, claim success, guarantee, amount, or server mutation.
- Gacha flavor ids may describe signal response, assignment, arrival, or reveal mood. They must not say the player bought, owns, is guaranteed, or is owed an operator.
- Ultimate ids are short cue lines only. Long ultimate movie script ids are outside Act 1 launch scope.

## Naming Convention

Use lowercase snake_case for all id segments.

General mission pattern:

```text
narrative.<mission_id>.<surface>.<slot>
```

Operator pattern:

```text
narrative.operator.<operator_id>.<surface>.<slot>
```

Gacha flavor pattern:

```text
narrative.gacha.<banner_or_context>.<surface>.<slot>
```

Recommended Act 1 narrative version candidate:

```text
act1.2026-08-27.001
```

This version indicates draft compatibility only. It is not a production localization readiness claim.

## Standard Mission Coverage

Applies to:

- `opn_01_last_departure`
- `opn_02_first_signal`
- `opn_03_water_stop`
- `opn_04_scrapstar_show`
- `opn_05_night_boarders`
- `opn_06_broken_line`

Required launch ids per ordinary operation:

| Surface | Required ids |
| --- | --- |
| Display | `narrative.<mission_id>.display.title` |
| Briefing | `narrative.<mission_id>.briefing.summary` |
| Briefing lines | `narrative.<mission_id>.briefing.line_001` through `line_005` |
| Commander response | `narrative.<mission_id>.commander_response.option_001` through `option_003` |
| Mission start | `narrative.<mission_id>.mission_start.copy` |
| Loading | `narrative.<mission_id>.loading.hint_001` |
| Battle opening | `narrative.<mission_id>.callout.opening_001` through `opening_003` |
| First pressure | `narrative.<mission_id>.callout.first_wave_001` through `first_wave_003` |
| Tutorial-adjacent | `narrative.<mission_id>.callout.tutorial_001` through `tutorial_003` |
| Escalation | `narrative.<mission_id>.callout.escalation_001` through `escalation_003` |
| Major turn | `narrative.<mission_id>.callout.major_turn_001` through `major_turn_003` |
| Final wave | `narrative.<mission_id>.callout.final_wave_001` through `final_wave_003` |
| Victory | `narrative.<mission_id>.callout.victory_001` through `victory_003` |
| Failure | `narrative.<mission_id>.callout.failure_001` through `failure_003` |
| Result | `narrative.<mission_id>.result.title` and `result.summary` |
| Reward framing | `narrative.<mission_id>.reward_framing.first_clear` and `reward_framing.repeat_clear` |
| Codex/archive | `narrative.<mission_id>.codex.title` and `codex.body` |

Notes:

- `first_wave` and `final_wave` are ordinary-operation slots only. Do not use them for the boss-only story raid.
- Optional-objective copy may remain in source dialogue drafts, but launch ids should only be promoted if the objective is presentation-only and not tied to scoring or reward contracts.
- If a mission needs fewer player-visible lines on mobile, keep the id reserved but leave it out of the launch fixture rather than recycling the id for unrelated text.

## Reward Framing Candidate Selection

Recommended launch fixture status: include qualitative `reward_framing.first_clear` and `reward_framing.repeat_clear` ids for each ordinary operation and the story raid.

These strings are flavor wrappers for result/reward presentation only. They must not be treated as reward source of truth, claim confirmation, ownership confirmation, economy mutation, pity progress, ticket grant, or server validation text.

Ordinary operation candidates:

| Mission id | First clear direction | Repeat clear direction |
| --- | --- | --- |
| `opn_01_last_departure` | Emergency cache left by the last station crew. | Remaining station supplies used for train upkeep. |
| `opn_02_first_signal` | Preserved route intelligence and communication parts. | Residual parts and route data around the tower. |
| `opn_03_water_stop` | Shared station supplies given after repair and protection. | Maintenance supplies gained through continued exchange. |
| `opn_04_scrapstar_show` | Maintenance material and route data shared after captives are freed. | Tradeable parts and data recovered from the remaining market area. |
| `opn_05_night_boarders` | Emergency gear and internal defense records organized after the boarding attack. | Maintenance supplies and defense data from carriage repair. |
| `opn_06_broken_line` | Preserved cache tied to the broken route and prior crew records. | Maintenance material and route data around the temporary rail. |

Story raid candidate:

| Mission id | First clear direction | Repeat clear direction |
| --- | --- | --- |
| `raid_raider_king_engine` | Crown Engine command records, preserved route data, and freed support systems. | Maintenance material and route data around the stopped Crown Engine. |

Allowed wording:

- "recovered", "shared", "opened", "organized", "preserved", "route data", "maintenance supplies", "records", "parts".
- Korean equivalents that frame source and mood without amount or entitlement.

Forbidden wording:

- Amounts, rates, pity progress, ticket counts, claim success, compensation, guaranteed reward, owned operator, ledger update, server-verified mutation, rank, settlement, score bracket, or season reward.

Failure reward framing:

- Ordinary operation failure should use failure result/callout text, not reward framing ids.
- Story raid failure should use the dedicated refund UX ids below.

## Ordinary Operation Mission Ids

| Mission id | Display title id | Required coverage status |
| --- | --- | --- |
| `opn_01_last_departure` | `narrative.opn_01_last_departure.display.title` | briefing, loading, callout, result, reward framing, codex |
| `opn_02_first_signal` | `narrative.opn_02_first_signal.display.title` | briefing, loading, callout, result, reward framing, codex |
| `opn_03_water_stop` | `narrative.opn_03_water_stop.display.title` | briefing, loading, callout, result, reward framing, codex |
| `opn_04_scrapstar_show` | `narrative.opn_04_scrapstar_show.display.title` | briefing, loading, callout, result, reward framing, codex |
| `opn_05_night_boarders` | `narrative.opn_05_night_boarders.display.title` | briefing, loading, callout, result, reward framing, codex |
| `opn_06_broken_line` | `narrative.opn_06_broken_line.display.title` | briefing, loading, callout, result, reward framing, codex |

## Story Raid Coverage

Applies to:

- `raid_raider_king_engine`

`raid_raider_king_engine` is a boss-only story raid. It uses 5 operators, boss defeat as success, and stamina refund UX on failure. It must not use ordinary-operation `first_wave` or `final_wave` ids, and it must not use World Raid score/ranking/settlement ids.

Required launch ids:

| Surface | Required ids |
| --- | --- |
| Display | `narrative.raid_raider_king_engine.display.title` |
| Briefing | `narrative.raid_raider_king_engine.briefing.summary` |
| Briefing lines | `narrative.raid_raider_king_engine.briefing.line_001` through `line_006` |
| Commander response | `narrative.raid_raider_king_engine.commander_response.option_001` through `option_003` |
| Mission start | `narrative.raid_raider_king_engine.mission_start.copy` |
| Loading | `narrative.raid_raider_king_engine.loading.hint_001` |
| Boss entrance | `narrative.raid_raider_king_engine.callout.boss_entrance_001` through `boss_entrance_004` |
| Boss HUD/objective | `narrative.raid_raider_king_engine.raid_ui.objective` |
| Boss HUD success condition | `narrative.raid_raider_king_engine.raid_ui.success_condition` |
| Operator count copy | `narrative.raid_raider_king_engine.raid_ui.operator_count` |
| State transition | `narrative.raid_raider_king_engine.callout.state_transition_armor_001` through `state_transition_armor_004` |
| State transition | `narrative.raid_raider_king_engine.callout.state_transition_broadcast_001` through `state_transition_broadcast_004` |
| State transition | `narrative.raid_raider_king_engine.callout.state_transition_core_001` through `state_transition_core_004` |
| Danger warning | `narrative.raid_raider_king_engine.danger.fuel_core_001` and `fuel_core_002` |
| Danger warning | `narrative.raid_raider_king_engine.danger.collision_001` and `collision_002` |
| Danger warning | `narrative.raid_raider_king_engine.danger.brake_lock_001` and `brake_lock_002` |
| Danger warning | `narrative.raid_raider_king_engine.danger.last_rail_integrity_001` and `last_rail_integrity_002` |
| Final stop | `narrative.raid_raider_king_engine.callout.final_stop_001` through `final_stop_004` |
| Defeat/victory | `narrative.raid_raider_king_engine.callout.defeat_001` through `defeat_004` |
| Survivors found | `narrative.raid_raider_king_engine.callout.survivors_found_001` through `survivors_found_004` |
| Failure | `narrative.raid_raider_king_engine.callout.failure_core_overload_001` through `failure_core_overload_003` |
| Failure | `narrative.raid_raider_king_engine.callout.failure_boss_not_stopped_001` through `failure_boss_not_stopped_003` |
| Failure | `narrative.raid_raider_king_engine.callout.failure_train_integrity_001` through `failure_train_integrity_003` |
| Refund UX toast | `narrative.raid_raider_king_engine.failure_refund.toast` |
| Refund UX | `narrative.raid_raider_king_engine.failure_refund.stamina_001` through `stamina_003` |
| Result | `narrative.raid_raider_king_engine.result.title` and `result.summary` |
| Reward framing | `narrative.raid_raider_king_engine.reward_framing.first_clear` and `reward_framing.repeat_clear` |
| Codex/archive | `narrative.raid_raider_king_engine.codex.title` and `codex.body` |

Raid notes:

- `raid_ui.success_condition` may say boss defeat/stopping the Crown Engine. It must not say score threshold, rank, settlement, or participation.
- Recommended launch fixture choice: use `failure_refund.toast` as the consolidated player-facing toast id for stamina return. Keep `failure_refund.stamina_*` as source-copy candidates only unless the UI needs multiple lines.
- `failure_refund.toast` and `failure_refund.stamina_*` may communicate stamina return. They must not promise refund amount, compensation, ranking credit, claim success, or server mutation details.
- State transition ids are presentation beats inside one boss fight, not combat phases or score stages.

## Optional Objective Copy

Recommended launch fixture status: exclude optional objective copy from the launch localization fixture.

Rationale:

- Optional objective lines in the mission drafts are useful for tone and future design continuity.
- Launch scope does not require optional objective scoring, ranking, bonus rewards, or reward eligibility.
- Keeping the lines in source drafts avoids losing writing work while preventing accidental fixture promotion.

If a later presentation-only pass uses optional objective lines, ids should use a clearly non-rewarding segment:

```text
narrative.<mission_id>.presentation_optional.<slot>
```

Do not use `objective_bonus`, `score`, `rank`, `reward`, `settlement`, or eligibility language for these lines unless a future launch contract explicitly promotes that behavior.

## Raider King Naming

Recommended launch fixture status: keep Raider King as a title, not a named antagonist.

Use title-based ids and copy:

```text
narrative.raid_raider_king_engine.antagonist.title
```

Do not add a personal name to launch localization fixtures. A named antagonist can be decided in a future narrative identity pass after Act 1 launch copy is stable.

## Operator Coverage

Initial operators:

- `op_kyle`
- `op_reina`
- `op_eve`
- `op_sera`
- `op_mira`

Gacha elite operators:

- `op_vex`
- `op_nyx`
- `op_sable`
- `op_marrow`
- `op_iris`
- `op_rail`
- `op_anchor`
- `op_lumen`
- `op_cinder`
- `op_warden`

Required ids per operator:

```text
narrative.operator.<operator_id>.display.name
narrative.operator.<operator_id>.profile.snippet
narrative.operator.<operator_id>.voice.keyword_001
narrative.operator.<operator_id>.voice.keyword_002
narrative.operator.<operator_id>.voice.keyword_003
narrative.operator.<operator_id>.callout.001
narrative.operator.<operator_id>.callout.002
narrative.operator.<operator_id>.callout.003
narrative.operator.<operator_id>.skill.cue
narrative.operator.<operator_id>.ultimate.cue
```

Operator notes:

- `display.name` should be presentation-only and should not encode rarity or acquisition source.
- `profile.snippet` should frame the operator as assigned, rescued, defected, responding, or voluntarily aboard.
- `ultimate.cue` is the launch cue line only. Do not add cinematic script ids for launch.

## Gacha Flavor And Reveal Coverage

Use `act1_pickup` as the launch banner/context string segment unless a later content naming pass chooses a more specific presentation-only name.

Required launch-safe gacha flavor ids:

```text
narrative.gacha.act1_pickup.banner.title
narrative.gacha.act1_pickup.banner.subtitle
narrative.gacha.act1_pickup.banner.flavor_001
narrative.gacha.act1_pickup.reveal.opening_001
narrative.gacha.act1_pickup.reveal.signal_rare
narrative.gacha.act1_pickup.reveal.signal_elite
narrative.gacha.act1_pickup.reveal.operator_arrival
narrative.gacha.act1_pickup.reveal.operator_repeat
narrative.gacha.act1_pickup.result.summary
narrative.gacha.act1_pickup.result.close
```

Allowed flavor direction:

- Signals answering the Last Rail broadcast.
- Operators reporting for assignment.
- Rescued specialists choosing to board.
- Defectors cutting ties with raider systems.
- The train making room, not taking ownership.

Forbidden flavor direction:

- Purchase, ownership, guaranteed person, owed operator, or compensation language.
- Probability, pity progress, ticket quantity, ledger, claim, drop table, or server validation language.
- World Raid, Rail Choice Event, ranking, settlement, score bracket, season, or leaderboard language.

## Launch Coverage Checklist

- Every ordinary operation has display, briefing, commander response, mission start, loading, callout, result, reward framing, and codex ids.
- The story raid uses boss-only id slots, not ordinary `first_wave` or `final_wave` slots.
- The story raid includes boss entrance, danger warning, state transition, defeat, failure, and stamina refund UX ids.
- All 15 operators have display, profile, voice keyword, callout, skill cue, and ultimate cue ids.
- Gacha flavor ids are presentation-only and avoid probability, pity, ticket quantity, ownership, and guarantee promises.
- Reward framing ids remain qualitative and do not encode amounts or eligibility.
- No Rail Choice Event ids are included in launch coverage.
- No World Raid ids are included in launch coverage.
- No server/API/Unity implementation requirement is introduced by this map.

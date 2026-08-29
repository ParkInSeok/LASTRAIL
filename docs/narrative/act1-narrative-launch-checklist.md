# Act 1 Narrative Launch Checklist

## Purpose

This document defines launch readiness checks for Act 1 narrative and direction surfaces.

It is a narrative planning artifact only. It does not create server APIs, Unity implementation requirements, economy rules, reward guarantees, gacha probability promises, unlock contracts, ledger behavior, or live-service operations.

## Launch Guardrails

- Rail Choice Events are excluded from Act 1 launch and remain backlog.
- World Raid is future/backlog. Do not use ranking, settlement, score bracket, season, leaderboard, participation, or competitive reward language in Act 1 ordinary operations or the Act 1 story raid.
- Ordinary Act 1 operations use 15-wave structure.
- `raid_raider_king_engine` is a boss-only story raid with 5 operators, boss defeat success, and stamina refund UX on failure.
- Reward framing may describe recovered supplies, records, or route data qualitatively. It must not promise quantities, eligibility, claim success, server mutation, pity progress, tickets, ownership, or compensation.
- Gacha flavor may describe signals, assignment, arrival, rescue, defection, or voluntary boarding. It must not imply purchased people, guaranteed operators, ownership, probability, pity, or owed rewards.
- Codex/archive copy is required for launch.
- Player-facing combat copy should stay short enough for portrait 9:16 mobile HUD readability.

## Readiness Flow

### Mission Select

Required:

- Display title has a stable mission string-id candidate.
- Mission summary states immediate objective without exposing server state, reward math, or future mode promises.
- Ordinary operation entries do not imply Rail Choice Events or mid-run branch selection.
- Story raid entry clearly reads as `boss-only`, not a 15-wave operation.

Ready when:

- A player can identify why they are entering the mission in one short sentence.
- Any reward-facing line is qualitative and presentation-only.
- Story raid copy does not use World Raid words such as score, rank, season, or settlement.

### Briefing

Required:

- Briefing summary.
- 4 to 6 short briefing lines.
- 2 to 3 commander response candidates when useful.
- Mission start button copy.

Ready when:

- Emotional stakes are clear before combat starts.
- Dialogue identifies the objective without adding mechanics outside launch scope.
- Operator or cast voice matches the Act 1 operator voice guide and mission tone.
- No line promises an unlock, ownership, reward amount, ticket, probability, or server-verified result.

### Loading

Required:

- One loading hint per mission or raid.
- Hint names the practical objective.
- Hint does not introduce new win conditions.

Ready when:

- Ordinary operation hints fit the 15-wave defensive run.
- Story raid hint states the 5-operator boss fight and boss stopping objective.
- Hint text can be read quickly on a mobile loading screen.

### Battle Entry

Required:

- Opening callouts establish location, threat, and first objective.
- System callout is functional.
- Character callout carries emotion or tactical flavor.

Ready when:

- First 10 seconds of battle can be understood without reading long lore.
- No more than one system line and two character lines are needed for the opening beat.
- The first callout does not block core HUD information, tutorial prompts, or touch targets.

### Ordinary Wave Transitions

Applies only to `opn_01` through `opn_06`.

Required:

- First pressure / first wave callouts.
- Tutorial-adjacent callouts where the mission teaches or reinforces play.
- Escalation callouts.
- Major turn callouts.
- Final wave callouts.
- Victory and failure callouts.

Ready when:

- Callouts reinforce the 15-wave sense of progression without requiring a line every wave.
- Major beats land at readable intervals: opening, first pressure, escalation, major turn, final wave, result.
- Optional-objective text remains presentation-only unless another launch contract explicitly promotes it.
- Final wave language is used only for ordinary operations.

### Story Raid Boss Transitions

Applies only to `raid_raider_king_engine`.

Required:

- Boss entrance.
- Boss HUD objective.
- Success condition copy.
- Operator count copy.
- Boss state transition callouts.
- Danger warnings.
- Final stop callouts.
- Victory/defeat lines.
- Failure/refund UX copy.

Ready when:

- The fight reads as one boss encounter with presentation beats inside it.
- State transition copy does not say wave, final wave, score stage, ranking milestone, or World Raid.
- Success copy says the boss is stopped or defeated.
- Failure copy frames tactical withdrawal and stamina return without promising refund amount, compensation, claim success, or mutation details.

### Result

Required:

- Clear title.
- Clear summary.
- Failure title/summary if a separate result surface exists.
- Reward framing for first clear and repeat clear when the UI needs flavor copy.

Ready when:

- The result summarizes what happened in the story, not what the server awarded.
- Reward framing describes source and mood only.
- Story progress language stays narrative-level and does not promise unlock mutation, account state, or ledger state.

### Reward / Codex

Required:

- Codex/archive title.
- Codex/archive body.
- Reward framing copy kept separate from codex lore.

Ready when:

- Codex/archive explains why the event matters after the mission.
- Codex copy does not mention reward quantities, pity, ticket totals, ownership, claim success, or server truth.
- Story raid codex closes Act 1 without becoming a World Raid settlement or season summary.

## Battle Callout Density Rules

### Length

- Target 12 to 24 Korean characters for active combat callouts when possible.
- Use one short sentence per callout.
- Avoid stacked clauses, lore terms that need explanation, and multi-sentence exposition during combat.
- Longer emotional lines belong in briefing, victory, result, or codex/archive.

### Frequency

- Opening: up to 3 lines total.
- Standard pressure beat: 1 to 2 lines.
- Major turn: up to 3 lines if no tutorial prompt is active.
- Final ordinary wave or story raid final stop: up to 3 lines total.
- Failure/refund UX: one system line plus one optional character line.

Recommended pacing:

- Do not fire narrative callouts more often than one beat every 8 to 12 seconds during normal combat.
- Danger warnings may interrupt this pacing, but should suppress lower-priority flavor callouts.
- Avoid back-to-back character banter when the player needs to read objective or damage warnings.

### Priority

Highest to lowest:

1. Failure, lethal danger, or train integrity warning.
2. Objective update or boss success condition.
3. Tutorial-adjacent instruction.
4. Major story turn.
5. Operator skill or ultimate cue.
6. Ambient flavor or banter.

Rules:

- Higher-priority callouts may replace lower-priority callouts.
- Do not queue flavor behind urgent warnings if it would arrive stale.
- Operator skill and ultimate cues should be short and should not obscure objective text.

### Overlap Prevention

- Keep combat callouts outside core touch targets and health/objective HUD zones.
- Do not show more than one narrative text container at the same time unless one is a tiny operator cue.
- If tutorial text is visible, suppress non-critical narrative flavor.
- If a boss danger warning is visible, suppress operator banter and reward-framing text.
- Use result and codex surfaces for longer emotional closure instead of forcing it into the battle HUD.

## Audio And Vibration Cue Principles

### BGM

- Use the single launch BGM loop as a pressure bed, not as a narrative state machine.
- Narrative intensity should come from callout timing, SFX, and screen direction, not from requiring multiple music layers.
- Story raid may use a perceived escalation through SFX density and boss animation while staying within the single-loop launch scope.

### SFX

- Pair system-critical callouts with clear, non-verbal SFX.
- Use distinct SFX families for objective update, danger warning, boss state transition, victory, and failure.
- Avoid attaching unique SFX requirements to every dialogue line.

### Vibration

- Use vibration for high-priority tactile beats only: danger warning, boss impact, train integrity hit, final stop, victory lock, and failure withdrawal.
- Do not vibrate for routine banter, codex unlock flavor, reward framing, or gacha flavor unless another presentation spec owns that surface.
- Keep repeated warnings from producing continuous vibration fatigue.

## Slot Separation

### Ordinary Operations

Use these launch narrative slots:

- `briefing`
- `loading`
- `callout.opening`
- `callout.first_wave`
- `callout.tutorial`
- `callout.escalation`
- `callout.major_turn`
- `callout.final_wave`
- `callout.victory`
- `callout.failure`
- `result`
- `reward_framing`
- `codex`

Ordinary notes:

- `first_wave` and `final_wave` are ordinary-operation slots only.
- 15-wave structure does not require 15 unique narrative beats.
- Optional-objective copy should not imply score, ranking, or reward eligibility.

### Story Raid

Use these launch narrative slots:

- `briefing`
- `loading`
- `callout.boss_entrance`
- `raid_ui.objective`
- `raid_ui.success_condition`
- `raid_ui.operator_count`
- `callout.state_transition`
- `danger`
- `callout.final_stop`
- `callout.defeat`
- `callout.failure`
- `failure_refund`
- `result`
- `reward_framing`
- `codex`

Raid notes:

- Do not use ordinary `first_wave` or `final_wave` slots.
- Do not use World Raid score, rank, season, settlement, participation, or leaderboard slots.
- Boss state transitions are presentation beats inside one encounter.
- Refund copy is UX policy wording only and should not state amounts or claim results.

## Launch Self-Review Checklist

Before marking Act 1 narrative launch-ready:

- All `opn_01` through `opn_06` documents include briefing, loading, battle callouts, result, reward framing, and launch codex/archive.
- `raid_raider_king_engine` includes boss-only briefing, loading, boss entrance, state transitions, danger warnings, final stop, victory, failure, refund UX, result, reward framing, and launch codex/archive.
- All launch narrative surfaces have stable string-id coverage in `act1-localization-string-id-map.md`.
- All 15 operators have profile/codex snippet, battle callouts, skill cue, and ultimate cue in `act1-operator-voice-guide.md`.
- Active combat lines are short enough for mobile HUD use.
- Ordinary 15-wave slots and story raid boss-only slots are not mixed.
- No Rail Choice Event launch copy is present.
- No World Raid launch copy is present.
- No reward quantity, ticket count, gacha probability, pity progress, ownership, guarantee, claim success, economy mutation, ledger state, or server validation promise is present.
- No new server/API/Unity requirement is introduced by narrative copy.

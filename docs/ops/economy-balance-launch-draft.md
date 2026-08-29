# Economy Balance Launch Draft

## Purpose

This document proposes first-pass launch economy numbers for LAST RAIL run rewards and offline rewards.

These are draft balance values for testing and tuning. They are not final production economy values until explicitly promoted into server balance seed/config.

## Goals

- Make active runs the primary source of meaningful progression.
- Let offline rewards provide recovery and baseline continuity without replacing play.
- Keep rare/special currencies controlled.
- Make Act 1 progression feel materially better without runaway compounding.
- Preserve server Source of Truth for all reward quantities.

## Currency Roles

| Reward Code | Role | Launch Notes |
| --- | --- | --- |
| `cur_credits` | General upgrade/spend currency | Main repeat reward. |
| `cur_machine_parts` | Train and equipment maintenance | Main Act 1 progression material. |
| `cur_tactical_data` | Tactical upgrade/research material | Secondary active-run reward. |
| `cur_operator_exp` | Operator growth | Allowed in runs and offline rewards at conservative amounts. |
| `cur_special_core` | Rare upgrade limiter | First clear/high-risk/event only candidate. |
| `cur_command_ticket_fragment` | Controlled recruitment progress | Not normal offline repeat. Limited first clear/event candidate. |
| `cur_command_ticket` | Full recruitment ticket | Limited non-repeat launch sources only. Not normal offline repeat at launch. |

## Active Run Reward Formula

Existing formula candidate:

```text
runReward = baseReward(operationId)
  x operationProgressionMultiplier
  x riskMultiplier
  x outcomeMultiplier
  x waveCompletionMultiplier
```

First clear bonus remains a separate reward line and is not multiplied into repeat reward.

## Active Run Multipliers

Risk multipliers:

| Risk | Multiplier |
| --- | --- |
| I | 0.80x |
| II | 1.00x |
| III | 1.20x |
| IV | 1.45x |
| V | 1.75x |

Operation progression multipliers:

| Operation | Multiplier |
| --- | --- |
| `opn_01_last_departure` | 1.00x |
| `opn_02_first_signal` | 1.10x |
| `opn_03_water_stop` | 1.20x |
| `opn_04_scrapstar_show` | 1.30x |
| `opn_05_night_boarders` | 1.40x |
| `opn_06_broken_line` | 1.55x |
| `raid_raider_king_engine` | 2.20x |

Outcome policy:

- Victory with 15 waves: 1.00x
- Failure with 1-14 cleared waves: `0.25 + 0.55 x clearedWaves / 15`, capped at 0.80x
- Abandoned: 0.00x for launch
- 0 cleared waves: 0 reward or minimal consolation only

## Active Run Base Rewards

Base repeat rewards before multipliers:

| Operation | Credits | Machine Parts | Tactical Data | Operator EXP |
| --- | ---: | ---: | ---: | ---: |
| `opn_01_last_departure` | 120 | 8 | 4 | 700 |
| `opn_02_first_signal` | 135 | 9 | 5 | 780 |
| `opn_03_water_stop` | 150 | 11 | 6 | 860 |
| `opn_04_scrapstar_show` | 170 | 13 | 7 | 950 |
| `opn_05_night_boarders` | 195 | 15 | 8 | 1050 |
| `opn_06_broken_line` | 225 | 18 | 10 | 1180 |
| `raid_raider_king_engine` | 360 | 28 | 16 | 1600 |

Rationale:

- Credits scale smoothly with operation complexity.
- Machine parts grow faster because Act 1 train recovery is central.
- Tactical data remains lower to avoid early research saturation.
- Operator EXP stays conservative but meaningful.
- Raid repeat reward is higher but not a substitute for long-term rare currencies.

## Active Run First Clear Rewards

First clear reward is one-time per account, operation, and risk.

Recommended base first clear by operation at Risk II:

| Operation | Credits | Machine Parts | Tactical Data | Special/Controlled |
| --- | ---: | ---: | ---: | --- |
| `opn_01_last_departure` | 250 | 15 | 8 | none |
| `opn_02_first_signal` | 300 | 18 | 12 | none |
| `opn_03_water_stop` | 360 | 24 | 14 | none |
| `opn_04_scrapstar_show` | 430 | 30 | 18 | `cur_command_ticket_fragment` candidate x1 |
| `opn_05_night_boarders` | 520 | 38 | 22 | none |
| `opn_06_broken_line` | 650 | 48 | 28 | `cur_special_core` candidate x1 |
| `raid_raider_king_engine` | 1000 | 75 | 45 | `cur_special_core` candidate x2 |

Controlled reward notes:

- `cur_command_ticket_fragment` should remain candidate-only until gacha economy is validated.
- `cur_special_core` should be first-clear or event-limited, not normal repeat.
- Full `cur_command_ticket` should be granted only from selected Act 1 first clears and launch login bonus at launch.

Recommended launch command ticket distribution:

| Source | Tickets |
| --- | ---: |
| Launch login bonus total | 5 |
| `opn_03_water_stop` first clear | 1 |
| `opn_05_night_boarders` first clear | 1 |
| `opn_06_broken_line` first clear | 2 |
| `raid_raider_king_engine` story raid first clear | 3 |
| **Launch non-repeat total** | **12** |

Rationale:

- 12 tickets lets a launch player experience one 10-pull plus a small number of single pulls.
- Tickets stay tied to account progression and launch attendance, not repeat offline income.
- Offline repeat rewards remain stable and do not become a recruitment faucet.

## Offline Reward Formula

Existing formula candidate:

```text
offlineReward = baseHourlyReward
  x storyProgressMultiplier
  x cappedOfflineHours
```

Claimable hours:

```text
claimableHours = min(actualOfflineHours, offlineStorageCapHours, maxClaimableHours)
```

Risk tier does not directly multiply offline rewards.

## Offline Story Progress Policy

| Story Progress | Multiplier | Storage Cap |
| --- | ---: | ---: |
| `none` | 1.00x | 4h |
| `opn_01_last_departure` | 1.15x | 5h |
| `opn_02_first_signal` | 1.32x | 6h |
| `opn_03_water_stop` | 1.52x | 7h |
| `opn_04_scrapstar_show` | 1.75x | 8h |
| `opn_05_night_boarders` | 2.05x | 9h |
| `opn_06_broken_line` | 2.40x | 10h |
| `raid_raider_king_engine` | 3.00x | 12h |

## Offline Base Hourly Rewards

Base hourly rewards before story multiplier:

| Reward Code | Base Per Hour | Launch Notes |
| --- | ---: | --- |
| `cur_credits` | 24 | Main offline recovery reward. |
| `cur_machine_parts` | 2 | Slow train maintenance income. |
| `cur_tactical_data` | 1 | Conservative background research income. |
| `cur_operator_exp` | 80 | Modest growth continuity. |

Not included in normal offline rewards:

- `cur_special_core`
- `cur_command_ticket_fragment`
- `cur_command_ticket`

## Example Offline Full-Cap Values

These examples use full storage cap and are for tuning intuition only.

| Story Progress | Cap | Credits | Machine Parts | Tactical Data | Operator EXP |
| --- | ---: | ---: | ---: | ---: | ---: |
| `none` | 4h | 96 | 8 | 4 | 320 |
| `opn_01_last_departure` | 5h | 138 | 12 | 6 | 460 |
| `opn_02_first_signal` | 6h | 190 | 16 | 8 | 634 |
| `opn_03_water_stop` | 7h | 255 | 21 | 11 | 851 |
| `opn_04_scrapstar_show` | 8h | 336 | 28 | 14 | 1120 |
| `opn_05_night_boarders` | 9h | 443 | 37 | 18 | 1476 |
| `opn_06_broken_line` | 10h | 576 | 48 | 24 | 1920 |
| `raid_raider_king_engine` | 12h | 864 | 72 | 36 | 2880 |

Rounding policy:

- Calculate per reward line independently.
- Use floor for fractional quantities.
- Minimum positive reward line can be 1 only if the base calculation is greater than 0 and claim is not below minimum time.

## Active Run Versus Offline Target

Launch target:

- One full-cap offline claim should feel useful, but not exceed several active full clears.
- At early Act 1, a 5-6 hour offline claim should roughly equal 1 standard low-risk active run in credits, but lower in tactical agency.
- At late Act 1, a 10-12 hour full-cap claim can equal 2-3 standard active runs in credits, but should not grant rare progression currency.
- Risk IV/V active runs should remain the best path for accelerated rewards.

## Daily Earning Guardrails

Draft daily assumptions:

- Casual player: 2-3 active runs + 1 offline claim.
- Engaged player: 6-8 active runs + 1-2 offline claims.
- Heavy player: 12+ active runs + capped offline claims.

Guardrails:

- Offline rewards should not dominate engaged player income.
- Active run risk rewards should be visible but not force only Risk V play.
- First clear should feel meaningful but not destabilize repeat economy.
- Rare currencies should remain controlled by first clear, event, pass, shop, or explicit high-risk policies.

## Economy Risk Checks

Before promotion to production config, test:

- Day 1 total credits for casual, engaged, and heavy profiles.
- Day 7 machine part accumulation versus train upgrade costs.
- Tactical data income versus research unlock pacing.
- Operator EXP leveling speed through Act 1.
- Risk I farming value versus Risk III/V expected value.
- Offline full-cap value after each story milestone.
- First clear burst income per risk tier.
- Absence of `cur_special_core`, command ticket fragment, and command ticket from normal offline repeat.

## Admin/CS Interpretation

Admin should show these values as balance policy, not player-owned truth, until read endpoints return live data.

Support should explain:

- Active run reward depends on operation, risk, outcome, and wave completion.
- Offline reward depends on story progress, elapsed server time, and cap.
- Risk does not directly increase offline reward.
- Long absence may hit storage cap.
- Minimum time below threshold is not claimable.

Support should not promise:

- special core compensation
- command ticket compensation
- manual currency adjustment
- uncapped offline recovery
- gacha reroll

## Promotion Rules

These numbers can be promoted only after:

- Server balance config parser accepts the final shape.
- Unit tests cover representative active/offline examples.
- Admin labels and support docs match final values.
- Unity displays server-provided reward lines without local calculation.
- DEV compatibility smoke confirms preview/claim response shapes.

## Open Decisions

- Whether `cur_operator_exp` offline income should be reduced after early launch testing.
- Whether `cur_command_ticket_fragment` appears in Act 1 first clear at all.
- Whether `cur_special_core` is first-clear only or can appear in Risk V event variants.
- Whether raid repeat rewards need weekly lockout or diminishing returns.
- Exact train upgrade costs and research costs needed to validate these income values.
- Whether economy balancing should use dynamic remote config before launch.

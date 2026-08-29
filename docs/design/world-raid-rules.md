# World Raid Rules

## Purpose

World Raid is a future competitive raid mode for LAST RAIL.

It is separate from the Act 1 story raid. The Act 1 story raid is a boss-only story encounter with 5 operators, stamina refund on failure, and boss defeat as the success condition. World Raid is a timed live-service event with score ranking and settlement rewards.

World Raid is not required for Act 1 launch unless explicitly promoted later.

## Core Mode Definition

- World Raid runs during a scheduled event window.
- Players enter the raid and attempt to earn score against a raid boss.
- The raid does not require every player to defeat the boss to count as successful participation.
- When the event window ends, the World Raid is settled.
- Participating players are treated as successful participants for event settlement.
- Rewards come from two sources:
  - Score bracket rewards.
  - Ranking bracket rewards.

## Story Raid Versus World Raid

| Area | Act 1 Story Raid | World Raid |
| --- | --- | --- |
| Purpose | Story progression boss encounter | Competitive live-service event |
| Timing | Part of Act 1 progression | Scheduled event window |
| Success | Verified boss defeat | Event participation plus score settlement |
| Failure | Stamina refund | Attempt can score low or zero, but event settlement still succeeds for participants |
| Operators | 5, same as ordinary operations | Default 5 unless future event rules override |
| Rewards | Story raid reward/first clear | Score bracket + ranking bracket |
| Ranking | None | Required |
| Scope | Launch story content | Future/backlog until promoted |

## Entry Rules

- Default operator slots: 5.
- World Raid can reuse the same operator slot baseline as ordinary operations unless a future season explicitly overrides it.
- Entry cost can use stamina, raid ticket, or event attempt count, but launch recommendation is to decide this in a later economy pass.
- Account merge, manual score correction, and Admin reward mutation are not part of the base rule set.

## Scoring Rules

Recommended first implementation:

- Score is based on boss damage and objective performance during the attempt.
- A player can make multiple attempts during the event window.
- Ranking uses the player's best score by default.
- Ties use earliest score submission as the first tiebreaker.
- Server owns final score validation and ranking settlement.

Suggested score components:

| Component | Role |
| --- | --- |
| Boss damage score | Main score source |
| Boss phase clear bonus | Rewards meaningful phase progress |
| Survival bonus | Rewards cleaner attempts |
| Time bonus | Rewards efficient clears without making speed the only goal |
| Objective bonus | Optional future season modifier |

Launch-safe recommendation:

- Start with damage score + phase clear bonus + survival bonus.
- Add time/objective bonuses only after combat telemetry is stable.

## Event Lifecycle

1. Scheduled
   - Event is configured but not open.
2. Open
   - Players can enter and submit scores.
3. Closing
   - New attempts are blocked; in-flight grace handling can be applied.
4. Settling
   - Server freezes eligible scores and computes ranking brackets.
5. Settled
   - Rewards become claimable or are delivered through server-owned reward grant flow.
6. Archived
   - Results are read-only for support and history.

## Reward Rules

World Raid rewards should be split into score and ranking rewards.

Score bracket rewards:

- Guaranteed by reaching score thresholds.
- Encourage broad participation.
- Should be more important than ranking rewards for casual players.

Ranking bracket rewards:

- Awarded by percentile or fixed rank bands.
- Should be meaningful but not economy-breaking.
- Should avoid exclusive power that permanently widens the gap.

Recommended reward stance:

- Score brackets grant common progression currency, machine parts, tactical data, and controlled command ticket fragments or tickets.
- Ranking brackets can add prestige cosmetics, titles, profile badges, limited extra currency, or controlled special materials.
- Full command tickets can appear in limited quantities.
- Normal offline rewards should not become a command ticket faucet because of World Raid.

## Example Reward Bracket Shape

Score brackets:

| Score Bracket | Reward Intent |
| --- | --- |
| Participation | Small baseline reward |
| Bronze | Useful currency bundle |
| Silver | Currency + tactical materials |
| Gold | Larger materials + limited ticket fragments |
| Elite | Best non-ranking score reward |

Ranking brackets:

| Rank Bracket | Reward Intent |
| --- | --- |
| Top 1% | Prestige + highest controlled bonus |
| Top 5% | Strong prestige and materials |
| Top 10% | Good materials and badge |
| Top 25% | Moderate materials |
| Top 50% | Small ranking bonus |
| Participation | No extra ranking bonus beyond score reward |

Exact quantities should be finalized only after Act 1 economy tuning is stable.

## Server Ownership

Server owns:

- Event schedule.
- Active event state.
- Attempt validation.
- Score calculation or score acceptance policy.
- Ranking freeze and settlement.
- Reward generation and claim state.
- Anti-abuse and idempotency.

Server should not store or trust:

- Raw animation timelines.
- Client-side VFX/SFX/camera data.
- Client-side probability or RNG material.
- Client-provided reward quantities.

## Unity Ownership

Unity owns:

- World Raid event entry UI.
- Boss fight presentation.
- Score progress display.
- Result presentation.
- Ranking preview display from server data.
- Reward claim presentation from server data.

Unity does not calculate ranking truth, reward truth, or final economy mutation.

## Admin Ownership

Admin owns read-only visibility:

- Event status.
- Account participation.
- Best score.
- Ranking bracket.
- Score bracket.
- Claim state.
- Ops errors and support evidence.

Admin mutation controls are future-only:

- Manual score correction.
- Reward repair.
- Ranking reprocess.
- Event force-settlement.

## Launch Boundary

Not included in Act 1 launch unless explicitly promoted:

- World Raid event schedule APIs.
- Ranking tables.
- Score settlement jobs.
- World Raid reward grants.
- Admin score correction or reprocessing.
- Public leaderboard UI.
- Cross-season archive.

## Open Decisions

- Entry cost: stamina, raid ticket, or free attempts.
- Attempt count: unlimited, daily limited, or event limited.
- Ranking method: best score or cumulative score.
- Tiebreaker order after earliest submission.
- Exact score formula.
- Exact reward quantities.
- Whether ranking rewards include cosmetics only, economy rewards, or both.
- Whether World Raid opens in the first post-launch event or later.

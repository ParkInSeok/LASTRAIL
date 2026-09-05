# World Raid Rules

## Status

This consolidated specification removes superseded alternatives. Shared solo/cooperative content, invitation, carry, and quest rules are in [online-coop-mode-policy.md](online-coop-mode-policy.md). World Raid remains separate from the Act 1 story raid and requires a separate release decision.

## Accepted Design Baseline

The user accepted these policies. Numeric settings are an accepted first-playtest configuration, not verified production balance or authorization to deploy World Raid. No implementation is claimed by this document.

### Tickets And Weekly Operation

- Use one common World Raid entry ticket per participant per attempt. Cooperative entry costs one from each account, never two from the host. Ticket stock itself is the common usage limit; add no separate daily attempt counter or per-mode allowance.
- Initial supply: two tickets when an account first becomes eligible, then two per elapsed daily boundary at 05:00 Asia/Seoul (20:00 UTC on the preceding date), capped at ten. No login/claim action is required to accrue supply. Accrual begins at eligibility, not account creation, and is applied idempotently before spending; supply beyond the cap is discarded.
- Tickets carry between weekly events without mode-specific stock, expiration at event reset, or retroactive recovery of overflow. Purchases and additional sources are outside this baseline.
- Run consecutive seven-day event windows from Monday 05:00 Asia/Seoul to the next Monday 05:00. Each event has a fixed boss/configuration version. New event selection and old event settlement use distinct event IDs.
- Stop accepting attempts whose full configured duration cannot finish by the event deadline. Reserve both ticket balances, recheck at server start, then commit both charges together. Rejected starts release both reservations. A valid attempt started before closing belongs only to its original event.
- At event close, freeze server-validated records and assignments before making ranking entitlements claimable. A delayed settlement displays pending and retries without new payouts; do not promise an arbitrary settlement completion minute.

### Final Score And Ranking Rules

- Score equals the sum of server-validated effective damage to the World Boss during the attempt. One unit of damage is one point; accumulate at the server's fixed precision and round down once at attempt completion. Do not round independently per player/hit to create role-dependent results.
- No direct phase, survival, clear-time, or mechanic-completion points in this baseline. Successful mechanics increase damage via their buff; failures reduce attack opportunity. Damage to adds/devices is not boss score.
- Solo has one account record; cooperative has one record per unordered pair, showing both players and their score-producing loadouts. Roles can change between attempts without creating a new pair. Unlimited pairs are allowed.
- Keep the highest eligible score, never cumulative attempts. Resolve ties by earliest server-recorded completion attaining that score, then stable attempt ID. Neither client clock nor result upload order controls a tie.
- Use separate solo/cooperative boards for each event/boss/rules version, with no selectable difficulty. Apply the confirmed cooperative duplicate-account reward promotion to eligible accounts before selecting fixed reward bands.
- Compute both boards independently. Each account receives its better ranking reward bundle once per event; if bundles are equal, payout is identical and no second bundle is created. Choosing a solo bundle does not rerun cooperative promotion or vacate its assigned position.
- Ranking bundles use the fixed-position test table below. No mode-exclusive reward, recruit currency, or promised cosmetic is added by this baseline.

### Score And Participation Reward Baseline

The following cumulative totals are a seven-day economy test budget using existing currency IDs. Higher tiers grant only the unclaimed difference from these totals; do not sum entire rows. Shared account/event entitlements combine solo and cooperative progress by tier, not raw score. The mode thresholds are initial placeholders for calibration against real combat output, not a claim that cooperative output is exactly double solo output.

| Tier | Solo score minimum | Cooperative team score minimum | Total cur_credits | Total cur_machine_parts | Total cur_tactical_data |
| --- | ---: | ---: | ---: | ---: | ---: |
| Participation | Eligible participation, score may be zero | Eligible participation, score may be zero | 300 | 20 | 10 |
| Bronze | 200000 | 400000 | 800 | 50 | 25 |
| Silver | 600000 | 1200000 | 1500 | 100 | 50 |
| Gold | 1200000 | 2400000 | 2500 | 160 | 80 |
| Elite | 2400000 | 4800000 | 4000 | 260 | 130 |

- Maintain one World Boss participation objective per weekly event, completed by one eligible attempt in either mode, with no daily duplicate or win/mechanic-success requirement. Its direct reward is the Participation tier above, not a second identical bundle. Do not also create a separately gated World Boss daily quest.
- A valid higher tier includes participation automatically. Repeated attempts/partners do not repay it. Rank rewards are a separate once-per-event bonus in addition to this shared score track.
- A cooperative low-damage support player earns the same team tier if eligible; no division by damage share. Each account only receives amounts it has not already earned elsewhere.
- Tune mode thresholds to comparable achievement effort before release. These numbers do not modify gameplay damage balance or the Act 1 twelve-ticket first-clear/login allocation.

### Eligibility And Exceptional Settlement

- Adopt the 180-second attempt, 30-second per-disconnect grace, at most 60 seconds of connection-gap grace per account/attempt, and 90 seconds of eligible presence as first-test values. Grace drives takeover/reconnection handling, not automatic personal activity credit. Reconnection remains possible for the original account until the deadline.
- Eligible presence includes connected recovery/incapacitation and legitimate periods without an actionable task. It excludes lobby time, disconnected automatic takeover, and proven unattended time. Require meaningful accepted role input or maintained control state, not a damage quota or continuous tapping. Exact instrumentation must be validated against both roles; do not auto-ban using this eligibility rule.
- A finalized attempt plus eligibility can count even with zero score or a brief final disconnect. Explicit voluntary abandonment forfeits that account's participation, score-tier, and ranking entitlement. Server-valid contributions already made are not retroactively subtracted from the team's damage.
- At normal time completion, retain a server-valid team record if at least one participant is eligible; mark an ineligible teammate as ineligible for rewards before promotion. Assign reward positions only to eligible unassigned accounts. Keep two names/roles in the historical team record without exposing private disconnection diagnostics. If neither is eligible, preserve diagnostics but do not rank or reward the attempt.
- After partner grace expires, an eligible remaining player may continue or accept the offered early end. Early end stores a partial diagnostic score but creates no ranked result or score-threshold payout. With at least 90 eligible seconds, that remaining player receives only the weekly participation entitlement. It is not classified as their voluntary abandonment. No normal early-end refund.
- No rank conversion to solo, new-player substitution, free timer reset, or new ticket charge on reconnect. Use only declared baseline takeover actions; auto-generated mechanics/buffs or superior takeover play are not allowed.
- A server-confirmed invalid attempt produces no score/quest/reward eligibility and restores each original ticket charge exactly once, without an additional allowance counter. Retrying the refund does not duplicate it; an owed refund must not be lost to the normal regeneration cap. Refunds may temporarily put stock above ten, with normal regeneration paused until below cap.
- Server-invalid results, ordinary low scores, recoverable failure, and user-reported network loss are distinct. A valid retained result cannot also receive an invalid-attempt refund.

### Remaining Validation, Not New Feature Decisions

- Verify ticket accrual across resets, event boundaries, concurrent requests, stock caps, and exact start deadlines.
- Validate meaningful activity detection without penalizing recovery, support builds, poor damage, or a legitimate idle role. Test deliberate disconnect/takeover and alternate-account carry cases.
- Simulate promoted positions with overlapping teams and ineligible members, deterministic ties, late invalidation, and cross-mode one-payout selection. Settlement must use frozen eligible records and account assignments, never claim order.
- Playtest initial damage, buff, recovery, threshold, and reward budgets. Changes take effect in a new event rules version, not midway through an open competition.
- Actual release date and deployment remain separate approval gates; accepting this design does not promote World Raid into the existing Act 1 launch build.

## Core Mode Definition

- World Raid runs during a scheduled event window.
- Players enter the raid and attempt to earn score against a raid boss.
- The raid does not require every player to defeat the boss to count as successful participation.
- Each combat attempt has a fixed time limit. The World Raid boss has unlimited health and cannot be defeated by exhausting HP; damage remains measurable for scoring. Story raid bosses retain finite health and require defeat.
- World Raid offers solo and online two-player modes. Story content and the Act 1 story raid remain solo-only.
- Confirmed: World Raid has no selectable difficulty tiers. Players select the event boss and solo/two-player mode; all attempts within a mode/event/rules version share the same encounter configuration. Solo and cooperative mechanic execution can differ.
- The top-two-difficulty carry-entry restriction belongs to ordinary operations, not World Raid. World Raid requires its common content-entry eligibility; random matchmaking matches the same event/boss and complementary roles, not power or difficulty tiers.
- Solo and two-player scores use separate leaderboards; scores are never pooled across the modes.
- Train HP/durability and operator HP/down states remain separate existing systems in both modes. Do not merge them or reuse ordinary-wave recovery policy as World Raid recovery.
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

## Timed Phase And Mechanic Test Configuration

These accepted initial test settings require combat validation before production. They do not change story raid behavior or authorize a World Raid release.

### Timeline

First-test baseline: 180 seconds, three phases, with the same duration and phase boundaries in solo and two-player variants of this test encounter.

| Remaining time R | State | Encounter purpose |
| --- | --- | --- |
| 120 < R <= 180 seconds | Phase 1 | Introduce external pressure relief and internal release actions |
| 60 < R <= 120 seconds | Phase 2 | Combine external defense with internal device defense |
| 0 < R <= 60 seconds | Phase 3 | Combine learned mechanics to create a final damage-buff opportunity |
| R <= 0 seconds | Attempt ended | Stop gameplay scoring and finalize the server result |

- The server derives remaining time from the attempt start/deadline. Local countdown display cannot advance a phase or extend scoring.
- Incapacitation, recovery, disconnection, and presentation animations do not pause the timer.
- Reconnection uses the current phase and active mechanic state; it does not replay missed mechanics or grant missed buffs.

### Mode-Specific Mechanic Examples

These are illustrative patterns to adapt to a defined boss, not new canonical boss identities or finalized mechanics.

| Phase | Solo version | Two-player version |
| --- | --- | --- |
| 1: Release restraint | Request train pressure relief, then release the interior anchor; the first result stays available for the follow-up window | Train player relieves pressure while squad player releases the anchor within the shared opportunity window |
| 2: Restore power | Request external interception, then defend the interior control device | Train player intercepts the external threat while squad player protects the device |
| 3: Expose weakness | Complete external support and interior control actions sequentially, then use the resulting damage window | Both roles complete complementary actions to expose the weakness and receive the shared damage buff |

- Solo required support actions must be available for the mechanic and cannot depend on simultaneous precision input in both areas.
- A successful mechanic grants the configured temporary damage buff; merely reaching a phase does not.
- Failure applies the mechanic's declared health/durability damage. It does not directly subtract score or force instant death; accumulated damage can cause recovery downtime.
- Initial damage, buff, and recovery settings are specified below. Boss-specific mechanic schedules still require implementation-level design and testing.

### Initial Mechanic Balance (Tuning Draft)

Use these candidates for the 180-second prototype in both solo and two-player modes. They are not changes to ordinary-operation or story-raid recovery rules. Do not apply an extra cooperative multiplier simply because there are two players.

| Parameter | Initial candidate |
| --- | --- |
| Mechanic failure damage | 35% of the declared target's maximum HP/durability |
| Phase 1 mechanic success | Boss damage x1.25 for 12 seconds |
| Phase 2 mechanic success | Boss damage x1.35 for 12 seconds |
| Phase 3 mechanic success | Boss damage x1.50 for 15 seconds |
| First incapacitation recovery | 8 seconds |
| Second incapacitation recovery | 12 seconds |
| Third and subsequent recoveries | 16 seconds each; capped for the attempt |
| HP/durability on recovery | 60% of that target's maximum |
| Post-recovery protection | 3 seconds against ordinary incoming damage |

Failure resolution:

- The 35% value is a proposed final mechanic penalty, independent of armor, dodge, critical hits, and ordinary shields. It needs playtest validation and must be communicated as mechanic damage, not an ordinary hit silently bypassing defenses. Normal combat damage and healing retain their own rules.
- Declare the affected target during the warning. An external failure targets train durability; an internal failure targets the explicitly marked operator(s). Do not damage every operator by default.
- A shared mechanic has one declared failure outcome, not a separate copy from each player's failure report. If both areas are deliberately targeted, each target and penalty must be shown in advance.
- Apply an external train penalty once; do not additionally route the same event through carriage-to-train damage transfer. Round nonzero penalties upward to the supported HP unit and clamp remaining HP to zero.
- At full HP with no healing, mitigation, or other damage, failures leave approximately 65%, 30%, then 0%. A low-health target can fall on its next failure. This is damage-based incapacitation, not an instant-death flag.

Buff resolution:

- Apply the buff to boss damage from both train and operators in solo, and to both roles in cooperative play. Do not separately multiply the resulting score or apply the modifier twice to the same hit.
- Start the window only when the boss can actually receive damage after mechanic completion. A player must not be able to hold the start indefinitely. The phase schedule must leave enough usable time before the attempt deadline.
- Use one shared mechanic-buff slot per attempt. The buff does not stack or add durations. A repeated event is ignored; a distinct equal/stronger success replaces the slot with its configured multiplier and full duration, while a weaker success cannot extend a stronger active buff.
- Resolve the modifier when damage is applied. An already-fired projectile receives the modifier only if it lands while the buff is active. Summoned enemies/device damage is not multiplied by this boss-specific buff.
- Incapacitation does not pause or restart an already earned buff. Its remaining server duration continues for the active role; the recovered role receives only any time still remaining.

Recovery resolution:

- Count incapacitations per operator and separately for the train over the entire attempt, not per phase and not per account. A phase change/reconnection does not reset the count.
- Only the incapacitated entity loses its attack/actions; surviving operators and the other available role continue. HP-zero entities cannot receive repeated down events or extra failure damage during recovery.
- Recovery is automatic at the individual entity's server recovery deadline, while the attempt is still active, and does not require a recruited recovery skill or consumable. Simultaneous squad incapacitation preserves the individual recovery deadlines; it does not add a second squad-wide wait.
- The proposed 3-second protection does not cleanse an already committed mechanic. Avoid issuing mandatory new tasks to a recovering entity; a task interrupted by incapacitation must follow an explicitly defined cancellation/recovery rule rather than silently granting success.
- No direct score deduction is added for incapacitation. Recovery time loses damage opportunity, with no additional survival-score deduction or bonus.
- Do not add free HP resets through role swapping, reconnecting, or phase changes. Whether healing and 60% recovery create an intentional-failure advantage must be checked in combat playtests before accepting these values.

Balance acceptance checks:

- Verify three unmitigated failures from full HP cause incapacitation, one does not, and healing/normal incoming damage change that sequence transparently.
- Verify solo and cooperative targets receive one penalty per resolved mechanic, without double transfer or duplicate success buffs.
- Verify 8/12/16-second recovery progression remains capped across phases and reconnects, and ordinary/story recovery is unchanged.
- Verify the final 15-second buff has a usable damage window, and no damage or expired buff can score beyond the attempt deadline.
- Compare successful, repeated-failure, healing-heavy, and deliberate-incapacitation runs. No mathematical or user-experience balance claim is made until those playtests exist.

### Boundary And Recovery Handling

Proposed handling to verify in the prototype:

- Stop scheduling a mechanic when there is insufficient time to provide its complete warning, solo/cooperative action window, and resulting damage-buff window before the next phase boundary or attempt deadline.
- At a phase boundary, retire an unfinished mechanic without an automatic success/failure, damage, buff, or score grant. A failure deadline actually reached before that boundary still resolves normally.
- Preserve an already earned buff until its own expiry or attempt end; a phase transition alone neither removes nor refreshes it. Avoid a new unattackable transition that wastes the earned window.
- Continue phases during recovery, but do not schedule a mandatory two-role mechanic while a required role is unavailable. Use a defined recoverable pattern or defer the mechanic without extending the attempt deadline. Missed mechanics are not later rewarded or replayed.
- At the attempt deadline, freeze score once. Recovery, damage-over-time, projectiles, callbacks, and reconnection cannot add post-deadline score. Exact server simulation ordering must be specified before implementation.

### Prototype Review Checks

- Check boundaries at exactly 120, 60, and 0 seconds and confirm both clients display the same server phase.
- Exercise solo sequential completion and both cooperative roles without requiring particular recruited operators.
- Verify phase boundaries and recovery never create unavoidable repeated mechanic failures.
- Verify earned buffs have a usable attack window and phase changes do not duplicate success grants.
- Compare damage opportunities across recovery/disconnection cases and ensure deliberately becoming unavailable cannot improve scoring opportunities.
- No direct phase/survival/time bonus; only validated boss damage scores.

## Public Records And Reward Promotion

### Public Record Loadouts (Confirmed)

- A ranked record exposes the train/installed parts used by the train-role player and the five-operator composition used by the squad-role player. Show which player owned each role.
- Capture a server-validated loadout snapshot for the score-producing attempt; never render the current account loadout as if it produced a historical score. Later equipment changes or upgrades do not rewrite the record.
- Recommended details include public part/operator IDs and localized names, occupied slots, and relevant levels/upgrades needed to interpret the build. Exclude unrelated inventory, internal account IDs, authentication data, and private diagnostics.
- The row can show a compact composition preview with a detail view. When a higher-scoring attempt replaces a team record, replace its score and loadout snapshot together; no mixing of attempts.
- Include the event/boss/mode/rules version so a historical build is not presented as evidence under a different balance version.

### Unlimited Ranked Partnerships (Confirmed)

- Do not limit the number of partners or ranked teams per account. The earlier three-team cap, registration slots, and event-long partnership locks are rejected.
- Valid teams can appear on the leaderboard even when one participant already appears in a higher-ranked team. Normal entry tickets and shared usage allowance still apply.
- Each account receives one ranking payout based on its highest eligible ranking entitlement, never a payout for every team entry.
- Confirmed reward promotion: keep all public team records, but skip reward-position occupancy only when both participants already have a reward position assigned in this settlement.
- Scan the frozen, deterministically ordered eligible team records from highest to lowest. Start the next reward position at 1 with no assigned accounts. For each row, find participants without an assigned position. If none remain, skip the row without advancing the position. Otherwise assign the current position to all unassigned participants in that row and advance it exactly once. Never replace an earlier assignment or issue another reward to an already assigned account.
- Assignment means inclusion in the server settlement ledger, not whether a player has opened mail or claimed a reward. Claim order cannot affect promotion. Compute all assignments before making rewards claimable; retries reuse the same finalized settlement.
- Example in descending score order: A+B, A+C, B+C, D+E. Reward positions are A=1, B=1, C=2, D=3, E=3. The third public row is entirely duplicate and consumes no reward position; public rank 4 therefore receives reward position 3.
- A repeated top player with different, previously unassigned partners does not by itself cause those team rows to be skipped. Their new partners retain their earned reward positions.

## Fixed Ranking Reward Test Configuration

The following amounts are a seven-day event budgeting example, not verified production economy or a per-attempt reward. If event frequency changes, retune the budget before use. It references existing currency roles in `../ops/economy-balance-launch-draft.md` and does not add a new currency.

| Assigned reward position | cur_credits | cur_machine_parts | cur_tactical_data |
| --- | ---: | ---: | ---: |
| 1 | 1200 | 80 | 40 |
| 2-3 | 1100 | 75 | 38 |
| 4-10 | 1000 | 70 | 35 |
| 11-50 | 850 | 60 | 30 |
| 51-100 | 700 | 50 | 25 |
| 101-500 | 500 | 35 | 18 |
| 501 and below | 300 | 20 | 10 |

- Intervals are inclusive, exhaustive, and non-overlapping for positive assigned positions. No entitlement is generated for a nonexistent position or an ineligible account.
- This table is the total ranking bundle per account per event, not cumulative across rows. Use the promoted cooperative reward position, not public team rank. Solo uses its valid personal rank.
- Use the same bundle table in both modes for this test. Cross-mode settlement selects the better earned bundle once, never both; perform each board's assignment independently before selecting the account entitlement.
- No full recruitment tickets, ticket fragments, special cores, or raid-entry tickets are included in this initial ranking bundle. No controlled currency is included in the accepted score-reward table either; these tables do not alter the Act 1 non-repeat recruitment-ticket total.
- Keep ranking growth-resource differences modest. Larger prestige differences can use future approved cosmetics/titles, but this draft does not create or promise those assets or require mode-exclusive collection.
- Score-threshold/participation totals are defined in Accepted Design Baseline and use a shared claim track. This ranking table is an additional once-per-event bundle.
- Before promotion, simulate low/high participant populations, repeated-partner promotion, inactive/invalid accounts, cross-mode duplicate entitlements, event frequency, and total currency output. No economy-balance or exploit-resistance claim is made from this draft.

## Event State Machine

Scheduled -> Open -> Closing -> Settling -> Settled -> Archived.

Closing rejects starts that cannot fit the event deadline. Settling freezes eligible records and assignments before reward availability. Archived records are read-only. Client claim order never changes a finalized settlement.

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

## Implementation Dependencies

- Authoritative combat and boss-specific schedules, cross-role buff catalogs, and participation evidence instrumentation must be implemented and tested; assets/view fixtures do not establish these capabilities.
- Tie ordering, assignment, and payouts use finalized eligible records, not client submission order or reward claim order.
- Post-settlement invalidation requires an explicit operational correction procedure; no manual Admin mutation is authorized here.
- First-test thresholds and amounts must be calibrated with real solo/cooperative combat output, and cannot change mid-event without a new rules version.

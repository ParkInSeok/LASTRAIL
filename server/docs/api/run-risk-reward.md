# Run Risk / Reward Design Draft

This draft defines the launch-scope server Source of Truth policy for run
risk, difficulty, and reward preview generation.

It is a design/contract draft only. It does not implement run finish, change
migrations, change API routes, mutate economy data, deploy Workers, create
Cloudflare resources, or change Unity reward calculation boundaries.

Offline rewards are a separate story progression system drafted in
`docs/api/offline-rewards.md`. Run risk does not multiply offline rewards.

## Goals

- Make higher risk and harder operation progression produce higher rewards.
- Keep reward preview generation on the server.
- Keep Unity as display/UX only.
- Make `reward_plan_json` auditable enough for support and operations.
- Prevent low-cleared or abandoned runs from receiving full-clear rewards.
- Keep first-clear rewards separate from repeat-clear multiplier math.

## Non-goals

- No implementation in this slice.
- No migration in this slice.
- No remote DEV secret, deploy, or smoke.
- No PRD/STAGING resource creation or deployment.
- No reward/economy mutation change.
- No Unity-side reward amount calculation.

## Current Foundation

Existing schema and contracts already provide the minimum anchors for a
server-side risk/reward calculation.

Risk and run context:

- `operation_previews.risk` stores selected run risk.
- `account_first_clears(operation_key, risk)` can store first clears per
  operation and risk tier.
- `runs.preview_id -> operation_previews.id` links a run back to its selected
  risk and objective preview.
- `runs.balance_config_version` freezes the balance version for the run.
- `run_wave_plans.wave_plan_json` stores the server generated 15-wave plan.
- `run_final_results.outcome` stores `VICTORY`, `FAILURE`, or `ABANDONED`.
- `run_final_results.cleared_waves` stores `0..15`.

Reward context:

- `run_reward_previews.reward_plan_json` stores the server-calculated preview
  plan shown to Unity.
- `reward_transactions.reward_json` stores committed reward data after claim.
- `economy_transactions` and `currency_ledger` record permanent currency
  grants.
- `account_currencies` supports:
  - `CREDITS`
  - `MACHINE_PARTS`
  - `TACTICAL_DATA`
  - `SPECIAL_CORE`
  - `OPERATOR_EXP`
- `account_items` and `item_ledger` can support ticket-like and item-like
  rewards added by migration `0013_gacha_pity_result_history.sql`.

Current gap:

- The storage and claim shell exists, but no current implementation guarantees
  that higher `risk` produces higher `rewardLines`.
- The server still needs a run finish reward preview generation policy that
  derives reward lines from frozen run state and frozen balance config.

## Ownership Boundary

The server owns:

- operation risk interpretation;
- operation progression multiplier;
- risk multiplier;
- outcome and wave completion multiplier;
- first-clear eligibility;
- reward preview generation;
- reward claim/economy mutation;
- idempotency and duplicate replay.

Unity owns:

- displaying server returned reward preview lines;
- entering reward review UX;
- submitting claim confirmation;
- diagnostic/debug display.

Unity must not:

- calculate reward amounts;
- send reward amounts;
- send multiplier values as Source of Truth;
- override risk, cleared waves, first-clear, or reward plan metadata.

## Recommended Formula

Base repeat rewards should use this launch-scope formula:

```text
repeatReward =
  baseReward(operationId, rewardCode)
  * operationProgressionMultiplier(operationId)
  * riskMultiplier(risk)
  * outcomeMultiplier(outcome)
  * waveCompletionMultiplier(outcome, clearedWaves)
```

Rules:

- Compute with integer basis points for deterministic D1/Workers behavior.
- Round down at the final reward-line quantity step.
- Enforce a minimum quantity only when the balance config explicitly says so.
- First-clear rewards are separate reward lines, not part of the multiplier
  stack.
- Integrity restrictions apply after the base reward plan is calculated and
  before the preview becomes claimable.

Recommended integer helper concept:

```text
quantity = floor(
  baseQuantity
  * operationProgressionBps
  * riskBps
  * outcomeBps
  * waveCompletionBps
  / 10000
  / 10000
  / 10000
  / 10000
)
```

## Launch Multiplier Draft

### Risk Multipliers

```text
Risk I    0.80x  8000 bps
Risk II   1.00x 10000 bps
Risk III  1.20x 12000 bps
Risk IV   1.45x 14500 bps
Risk V    1.75x 17500 bps
```

Intent:

- Risk II is the standard baseline.
- Risk I is a safer/low-pressure option with lower repeat rewards.
- Risk IV/V must feel meaningfully better, but not so high that low-level
  carries or exploit runs become the dominant economy path.

### Operation Progression Multipliers

Act 1 launch operation draft:

```text
opn_01_last_departure    1.00x 10000 bps
opn_02_first_signal      1.10x 11000 bps
opn_03_water_stop        1.20x 12000 bps
opn_04_scrapstar_show    1.30x 13000 bps
opn_05_night_boarders    1.40x 14000 bps
opn_06_broken_line       1.55x 15500 bps
```

Intent:

- Later operations are harder and should pay more even at the same risk tier.
- Operation progression should be gentler than risk scaling so players can
  choose higher risk without skipping all earlier operation value.

### Outcome / Wave Completion

Full victory:

```text
VICTORY with clearedWaves = 15 -> 1.00x
```

Partial failure:

```text
FAILURE -> 0.25 + 0.55 * clearedWaves / 15, capped at 0.80x
```

Recommended basis point form:

```text
failureWaveCompletionBps =
  min(8000, 2500 + floor(5500 * clearedWaves / 15))
```

Abandoned:

```text
ABANDONED -> 0.00x by default
```

Optional later policy:

```text
ABANDONED -> 0.20x max only for explicit checkpoint-safe abandon flows
```

Launch recommendation:

- Use `0` for ordinary abandoned runs.
- Add non-zero abandoned rewards only after exploit and offline retry policies
  are fully specified.

## Balance Config Reward Schema Candidate

Store run reward policy in immutable `balance_config_versions.config_json`.
Runs must use `runs.balance_config_version`, not the current active version.

Candidate shape:

```json
{
  "runRewards": {
    "schemaVersion": 1,
    "riskMultipliersBps": {
      "I": 8000,
      "II": 10000,
      "III": 12000,
      "IV": 14500,
      "V": 17500
    },
    "operationProgressionMultipliersBps": {
      "opn_01_last_departure": 10000,
      "opn_02_first_signal": 11000,
      "opn_03_water_stop": 12000,
      "opn_04_scrapstar_show": 13000,
      "opn_05_night_boarders": 14000,
      "opn_06_broken_line": 15500
    },
    "outcomePolicy": {
      "victoryFullClearBps": 10000,
      "failureBaseBps": 2500,
      "failureWaveBps": 5500,
      "failureCapBps": 8000,
      "abandonedBps": 0
    },
    "operations": {
      "opn_01_last_departure": {
        "baseRewards": [
          { "kind": "CURRENCY", "id": "cur_credits", "quantity": 300 },
          { "kind": "CURRENCY", "id": "cur_operator_exp", "quantity": 700 }
        ],
        "firstClearRewards": [
          { "kind": "CURRENCY", "id": "cur_machine_parts", "quantity": 100 }
        ]
      }
    },
    "integrityPolicy": {
      "softAnomaly": "ALLOW_PREVIEW_WITH_FLAG",
      "hardViolation": "INVALIDATE_PREVIEW",
      "quarantinedRun": "INVALIDATE_PREVIEW"
    }
  }
}
```

Notes:

- Exact base quantities are illustrative and not approved balance values.
- `cur_operator_exp` maps to D1 currency `OPERATOR_EXP`.
- Keep stable public IDs in balance config; map to D1 currency enums only in
  server reward/economy code.
- `firstClearRewards` should be independent reward lines and must not be
  multiplied by repeat reward multipliers unless a future policy explicitly
  says so.

## Reward Line Scope

Launch repeat reward candidates:

- `cur_credits`
- `cur_machine_parts`
- `cur_tactical_data`
- `cur_operator_exp`

Launch controlled/special reward candidates:

- `cur_special_core`
- `cur_command_ticket_fragment`
- material item IDs through `account_items` / `item_ledger`

Recommended limits:

- `cur_special_core` should be mostly first-clear, high-risk milestone, or
  weekly/raid style reward. Avoid high repeat farming rates.
- `cur_command_ticket_fragment` can be used as a small first-clear or high-risk
  bonus.
- Full `cur_command_ticket` grants are economically sensitive and should be
  restricted to monthly pass, explicit events, selected first-clear rewards, or
  tightly capped systems.
- `cur_raid_token` should remain raid-system-owned until raid reward policy is
  implemented.
- Fuel cans may be event/compensation/shop rewards first; repeat run reward fuel
  can create self-sustaining loops and needs explicit approval.

Claim implementation note:

- Current reward claim implementation processes currency reward lines.
- Item reward lines will require item-ledger claim support before launch use.

## Launch Economy Draft Fixture

The balance package also exposes `launchEconomyBalanceDraft` as a
testing/tuning reference for the launch economy draft. This fixture preserves
the latest planning numbers without promoting them into the live
`launchRunRewardsConfig`, D1 seed data, DEV balance config, or production
economy values.

Draft repeat active-run base rewards:

```text
operation id                 credits  machine parts  tactical data  operator exp
opn_01_last_departure            120              8              4           700
opn_02_first_signal              135              9              5           780
opn_03_water_stop                150             11              6           860
opn_04_scrapstar_show            170             13              7           950
opn_05_night_boarders            195             15              8          1050
opn_06_broken_line               225             18             10          1180
raid_raider_king_engine          360             28             16          1600
```

Draft first-clear candidate rewards keep controlled items separate:

- `opn_04_scrapstar_show`: `cur_command_ticket_fragment x1` candidate only.
- `opn_06_broken_line`: `cur_special_core x1` candidate only.
- `raid_raider_king_engine`: `cur_special_core x2` candidate only.

These values are not final production economy values. Promoting them into
server seed/config requires a separate explicit approval and compatibility
smoke with Unity/Admin no-calculation boundaries.

## Run Finish Reward Preview Generation Contract

`POST /v1/runs/:runId/finish` should create the reward preview inside the
accepted finish transaction after validation and before returning
`rewardPreviewId`.

Inputs:

- authenticated account id;
- `runs.balance_config_version`;
- `runs.preview_id`;
- `operation_previews.objective`;
- `operation_previews.risk`;
- `run_wave_plans.wave_plan_json`;
- `run_final_results.outcome`;
- `run_final_results.cleared_waves`;
- first-clear state from `account_first_clears`;
- integrity validation outcome.

Client request exclusions:

- reward amount;
- reward multiplier;
- first-clear flag;
- balance config payload;
- ledger IDs;
- currency grants;
- item grants.

If the Unity request includes reward amount or reward grant fields, return
`contract_violation` or ignore them only if the contract parser already rejects
unknown fields. Prefer explicit rejection for economy-impacting fields.

Reward preview generation output:

- Insert one `run_reward_previews` row.
- `status = 'ACTIVE'` unless integrity rules require `INVALIDATED`.
- `reward_plan_json.rewardLines` contains displayable server-calculated lines.
- `reward_plan_json.calculation` contains sanitized audit metadata.
- `reward_plan_json` must not contain raw validator internals, secrets, tokens,
  or client-submitted reward values.

Suggested sanitized metadata:

```json
{
  "rewardLines": [],
  "calculation": {
    "operationId": "opn_03_water_stop",
    "actId": "act_01_wasteland_railway",
    "regionId": "region_wasteland_railway",
    "risk": "III",
    "outcome": "VICTORY",
    "clearedWaves": 15,
    "balanceVersion": "1",
    "operationProgressionMultiplierBps": 12000,
    "riskMultiplierBps": 12000,
    "outcomeMultiplierBps": 10000,
    "waveCompletionMultiplierBps": 10000,
    "firstClearEligible": true,
    "firstClearApplied": true,
    "integrityDisposition": "ALLOW"
  }
}
```

Duplicate replay:

- Run finish duplicate replay should return the previously recognized
  `rewardPreviewId` if the original accepted finish created one.
- Reward preview lookup/claim duplicate replay must return stored reward plan
  shape and must not recalculate multipliers.

## First Clear Policy

First clear is scoped by:

```text
accountId + operationId + risk
```

Use `account_first_clears(operation_key, risk)` as the durable guard.

Rules:

- First clear requires `VICTORY` and `clearedWaves = 15`.
- First clear bonus is a separate reward line with source such as
  `FIRST_CLEAR`.
- First clear bonus is not multiplied by risk/outcome/wave multipliers unless a
  future balance policy explicitly opts in.
- Commit first-clear state in the same accepted finish transaction that creates
  the reward preview.
- If first-clear insertion conflicts, the server must remove or not create the
  first-clear bonus line.

Launch decision:

- Consume first clear at accepted finish preview creation time.
- Rationale: the preview is immutable, duplicate replay returns the stored
  `rewardPreviewId`, and reward claim reuses the stored plan without
  recalculating first-clear state.
- Trade-off: an expired or abandoned unclaimed preview still consumes first
  clear. This is acceptable for launch because a successful accepted finish is
  the durable completion event; support recovery should restore or re-open the
  stored preview rather than recalculate first-clear eligibility.

## Integrity Restriction Policy

Candidate disposition:

```text
ALLOW
ALLOW_PREVIEW_WITH_FLAG
INVALIDATE_PREVIEW
HOLD_FOR_REVIEW
```

Recommended launch policy:

- `ALLOW`: normal preview.
- `ALLOW_PREVIEW_WITH_FLAG`: preview is claimable but metadata marks an
  integrity warning for Admin.
- `INVALIDATE_PREVIEW`: preview status `INVALIDATED`, claim rejected.
- `HOLD_FOR_REVIEW`: future policy only. Avoid until reward hold UX and Admin
  release controls are implemented.

Potential error codes:

```text
reward_plan_unavailable
reward_plan_mismatch
reward_multiplier_missing
risk_reward_policy_missing
reward_integrity_restricted
```

## Unity Impact

No current Unity behavior changes are required by this design.

Unity keeps:

- reward amount calculation disabled;
- reward preview lines as server display data;
- reward claim as the point where rewards become permanent;
- `HasRewardAmounts=false` for reward entry/end UX state models.

Future display-only fields can be added after server contract approval:

- `riskLabel`
- `rewardMultiplierLabel`
- `outcomeRewardPolicyLabel`
- `firstClearBonus`
- `difficultyLabel`

Unity diagnostic status:

- `riskLevel` is available in Run API diagnostic snapshots, so QA can
  correlate run risk and reward preview behavior without Unity calculating
  reward amounts or multipliers.

Do not add response fields until Unity DTO/adapter/debug JSON/label helpers and
server compatibility smoke are updated together.

## Admin / Operations Impact

Admin run detail candidates:

- risk tier;
- operation id;
- outcome;
- cleared waves;
- reward preview id;
- reward claim id;
- reward line summary;
- applied risk multiplier;
- first-clear applied flag;
- integrity disposition.

Dashboard metric candidates:

- average reward value by risk tier;
- clear rate by risk tier;
- failure wave distribution by risk tier;
- reward claim failure rate by risk tier;
- expected reward vs stored preview mismatch count;
- Risk IV/V reward preview invalidation rate.

Ops error log candidates:

- `reward_plan_mismatch`
- `reward_multiplier_missing`
- `risk_reward_policy_missing`
- `reward_plan_unavailable`
- `reward_integrity_restricted`

Audit boundary:

- Admin audit logs remain for operator actions.
- Runtime reward mismatch belongs in ops error logs or integrity events.

## Contract / Smoke Test Candidates

Add tests before implementing reward preview generation.

Risk scaling:

- Same operation, same outcome, same cleared waves:
  - Risk V reward quantity must be greater than Risk I.
  - Risk II should match base multiplier.

Operation progression:

- Same risk/outcome/waves:
  - `opn_06_broken_line` repeat reward must be greater than
    `opn_01_last_departure`.

Outcome and waves:

- `VICTORY` with 15 waves gives full outcome/wave multiplier.
- `FAILURE` with partial waves gives less than full clear and at most `0.80x`.
- `ABANDONED` gives zero or explicitly configured low reward.

First clear:

- First victory for account+operation+risk includes first-clear reward line.
- Second victory for the same account+operation+risk does not include it.
- First clear at Risk III does not consume first clear at Risk IV.

Client trust boundary:

- Unity request reward amount fields are rejected with `contract_violation` or
  ignored only by an explicit safe parser policy.
- Reward preview uses frozen `runs.balance_config_version`.
- Reward preview does not use current active balance version after publish.

Duplicate replay:

- Duplicate run finish replay returns the same `rewardPreviewId`.
- Reward preview lookup returns the exact stored reward plan.
- Reward claim duplicate replay does not grant again.

Error paths:

- Missing reward table returns `reward_plan_unavailable`.
- Missing risk multiplier returns `reward_multiplier_missing`.
- Missing operation reward policy returns `risk_reward_policy_missing`.
- Reward plan checksum/shape mismatch returns `reward_plan_mismatch`.

## Implementation Candidate Order

Completed first local-only slice:

- Added a code-level launch reward config fixture under the balance package.
- Added a pure reward preview calculator for the documented formula.
- Added contract tests for risk scaling, operation progression scaling,
  full-clear vs partial/abandoned outcomes, first-clear separate reward lines,
  and missing multiplier errors.
- Did not change migrations, D1 seed data, run finish routes, economy mutation,
  or remote Cloudflare resources.

Completed second local-only slice:

- Added a persisted `runRewards` parser/validator in the balance package.
- The parser accepts only `schemaVersion: 1`.
- Risk multipliers must include Risk I through Risk V as positive integer bps.
- Operation progression multipliers must be positive integer bps.
- Outcome policy must be integer bps, with abandoned allowed to be zero.
- Reward lines must have a supported `kind`, non-empty stable `id`, and positive
  integer `quantity`.
- Integrity policy values must use approved server dispositions.
- Parser failures use existing contract error codes:
  - `risk_reward_policy_missing`
  - `reward_multiplier_missing`
  - `reward_plan_mismatch`
- Did not change migrations, D1 seed data, run finish routes, economy mutation,
  or remote Cloudflare resources.

Completed third local-only slice:

- Added `POST /v1/runs/:runId/finish` contract route.
- The route requires `Idempotency-Key` and Game API bearer account auth.
- The route rejects client reward amount/grant/ledger/first-clear fields at the
  request parser boundary.
- The route reads the run's frozen `balance_config_version`, linked
  `operation_previews`, and immutable balance config `runRewards`.
- Accepted finish writes `run_final_results`, updates `runs.status`, inserts one
  `run_reward_previews` row, and records `idempotency_records.operation =
  'run_finish'` in one D1 batch.
- Accepted response returns `rewardPreviewId` and does not return reward amounts
  or final grants.
- Added unit contract coverage and local D1 smoke coverage for reward preview
  insert on accepted finish.
- Added duplicate replay coverage so `run_finish` idempotency returns the stored
  `rewardPreviewId` without recalculating rewards or inserting a second preview.
- Did not change migrations, DEV seed data, reward claim/economy mutation, or
  remote Cloudflare resources.

Completed fourth local-only slice:

- Added `account_first_clears` guard lookup to accepted run finish.
- First clear is scoped by account, operation id, and risk.
- Full `VICTORY` with 15 cleared waves and no existing guard row applies
  separate `FIRST_CLEAR` reward lines.
- The same accepted finish D1 batch inserts `run_final_results`, updates
  `runs`, inserts `run_reward_previews`, inserts `account_first_clears`, and
  records `idempotency_records.operation = 'run_finish'`.
- Existing first clear omits the `FIRST_CLEAR` reward line and does not insert a
  second guard row.
- Duplicate replay still returns the stored `rewardPreviewId` and does not
  recalculate first-clear state.
- Added unit contract coverage and local D1 smoke coverage for the guard.
- Did not change migrations, DEV seed data, reward claim/economy mutation, or
  remote Cloudflare resources.

Next implementation candidates:

1. Decide whether the DEV balance seed should include `runRewards` before run
   finish integration.
2. Add Admin read-only reward preview summary later.
3. Add Unity display metadata only after response contract is approved.
4. Add full validator integration for runtime export integrity.

## Open Decisions

- Exact base reward quantities per operation.
- Whether `cur_command_ticket_fragment` is allowed in launch run rewards.
- Whether any full `cur_command_ticket` can be granted outside pass/event/shop.
- Whether `SPECIAL_CORE` is repeatable at Risk IV/V or first-clear only.
- Whether abandoned runs give zero reward or a capped checkpoint reward.
- Whether item reward lines are needed before item-ledger claim support lands.
- Exact Admin metric names and thresholds for risk/reward imbalance.

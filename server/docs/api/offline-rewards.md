# Offline Rewards Design Draft

This draft defines the launch-scope story progression policy for offline
rewards. It is a design/contract draft only.

It does not implement migrations, API routes, economy mutation, Unity
calculation, Cloudflare DEV deploy, Cloudflare secrets, remote smoke, or PRD
resources.

## Goals

- Make offline rewards grow as the player restores Act 1 story infrastructure.
- Keep offline reward amounts as server Source of Truth.
- Keep risk rewards separate from offline rewards.
- Give operations and Admin enough metadata to debug offline reward plans later.
- Avoid repeated special-core, command-ticket, and ticket-fragment economy loops
  in launch scope.

## Non-Goals

- No implementation in this slice.
- No migration in this slice.
- No new API route in this slice.
- No remote DEV secret, deploy, or smoke.
- No PRD/STAGING resource creation or deployment.
- No Unity-side offline reward amount or multiplier calculation.
- No use of run risk as an offline reward multiplier.

## Design Boundary

Offline reward scaling belongs to story progress, not run risk.

Interpretation:

- Active run reward answers: "How dangerous was this sortie, and how well did
  the player complete it?"
- Offline reward answers: "How much baseline production has the train
  community restored through story progress?"

Risk I and Risk V must produce the same offline multiplier when story progress
is the same. Risk stays in the active run reward policy defined by
`docs/api/run-risk-reward.md`.

Unity may display server-returned offline reward preview/claim data later, but
Unity must not calculate:

- offline reward amounts;
- story progress multipliers;
- storage cap hours;
- resource weights;
- claimable hours;
- economy grants.

## Recommended Formula

Launch formula:

```text
offlineReward =
  baseHourlyReward
  * storyProgressMultiplier
  * claimableHours
```

Claimable hours:

```text
claimableHours = min(actualOfflineHours, offlineStorageCapHours)
```

Minimum duration:

```text
actualOfflineMinutes < minimumOfflineMinutes -> rewardLines = []
```

Launch recommendation:

- `minimumOfflineMinutes = 10`
- `claimableHours` may use fractional hours for server calculation.
- Round down per reward line at final quantity.
- Store multiplier and duration math as sanitized server metadata for support.

Basis point form:

```text
quantity = floor(
  baseHourlyQuantity
  * storyProgressMultiplierBps
  * claimableSeconds
  / 10000
  / 3600
)
```

## Act 1 Story Progress Draft

Use the highest cleared story milestone as the offline progress key.

```text
none                       1.00x 10000 bps  cap  4h
opn_01_last_departure      1.15x 11500 bps  cap  5h
opn_02_first_signal        1.32x 13200 bps  cap  6h
opn_03_water_stop          1.52x 15200 bps  cap  7h
opn_04_scrapstar_show      1.75x 17500 bps  cap  8h
opn_05_night_boarders      2.05x 20500 bps  cap  9h
opn_06_broken_line         2.40x 24000 bps  cap 10h
raid_raider_king_engine    3.00x 30000 bps  cap 12h
```

Narrative interpretation:

- `none`: the train is barely functional.
- `opn_01_last_departure`: departure stabilizes basic gathering and salvage.
- `opn_02_first_signal`: restored signals improve route discovery.
- `opn_03_water_stop`: water access improves community productivity.
- `opn_04_scrapstar_show`: recovered scrap/tech channels improve parts flow.
- `opn_05_night_boarders`: passengers become an organized onboard community.
- `opn_06_broken_line`: recovered records and repaired line improve planning.
- `raid_raider_king_engine`: stopping the raider engine opens a safer regional
  supply rhythm.

The raid milestone is a story progression key for offline production, not a
run-risk multiplier.

## Resource Mix Draft

Launch repeat offline reward weights:

```text
cur_credits          50%  5000 bps
cur_machine_parts    25%  2500 bps
cur_tactical_data    15%  1500 bps
cur_operator_exp     10%  1000 bps
cur_special_core      0%     0 bps
cur_command_ticket    0%     0 bps
```

Recommendations:

- `cur_special_core` should not be a default repeating offline reward.
- Full `cur_command_ticket` should not be a default repeating offline reward.
- `cur_command_ticket_fragment` remains launch-default offline reward scope
  deferred. Use weekly/event/pass systems after Act 1 raid if needed.
- Offline reward lines should start with currency ledger support only.
- Item-like offline rewards require explicit item-ledger support and a separate
  approval.

Two balance config styles are possible:

1. `resourceWeightsBps` with `baseHourlyRewardBudget`.
2. Explicit `baseHourlyRewards` lines per resource.

Recommendation:

- Use explicit `baseHourlyRewards` for launch.
- It is easier to audit and avoids hidden rounding differences between resource
  weights and total budgets.
- Keep `resourceWeightsBps` as an optional Admin balancing aid, not a server
  Source of Truth requirement for the first implementation.

## Balance Config Schema Candidate

Store offline reward policy in immutable `balance_config_versions.config_json`
beside `runRewards`.

Candidate shape:

```json
{
  "offlineRewards": {
    "schemaVersion": 1,
    "minimumOfflineMinutes": 10,
    "storyProgressMultipliersBps": {
      "none": 10000,
      "opn_01_last_departure": 11500,
      "opn_02_first_signal": 13200,
      "opn_03_water_stop": 15200,
      "opn_04_scrapstar_show": 17500,
      "opn_05_night_boarders": 20500,
      "opn_06_broken_line": 24000,
      "raid_raider_king_engine": 30000
    },
    "storageCapHours": {
      "none": 4,
      "opn_01_last_departure": 5,
      "opn_02_first_signal": 6,
      "opn_03_water_stop": 7,
      "opn_04_scrapstar_show": 8,
      "opn_05_night_boarders": 9,
      "opn_06_broken_line": 10,
      "raid_raider_king_engine": 12
    },
    "baseHourlyRewards": [
      { "kind": "CURRENCY", "id": "cur_credits", "quantity": 100 },
      { "kind": "CURRENCY", "id": "cur_machine_parts", "quantity": 50 },
      { "kind": "CURRENCY", "id": "cur_tactical_data", "quantity": 30 },
      { "kind": "CURRENCY", "id": "cur_operator_exp", "quantity": 20 }
    ],
    "blockedDefaultRewardIds": [
      "cur_special_core",
      "cur_command_ticket",
      "cur_command_ticket_fragment"
    ],
    "maxClaimableHours": 12,
    "integrityPolicy": {
      "clockTamper": "REJECT_CLAIM",
      "staleClientState": "RECALCULATE_SERVER_SIDE",
      "maintenanceMode": "DEFER_CLAIM"
    }
  }
}
```

Notes:

- Exact `baseHourlyRewards` quantities are illustrative, not approved balance
  values.
- `maxClaimableHours` should be the largest value in `storageCapHours`, unless
  a global live-ops cap is intentionally lower.
- Public stable IDs stay in config. Server economy code maps them to D1 currency
  enums only when committing ledger rows.
- Do not include risk multipliers in `offlineRewards`.

## Server State Candidate

The server needs durable account state before implementing offline claims.

Existing candidate:

- `account_runtime_state` can hold account-level runtime flags today.

Likely future additions:

- `last_offline_reward_claim_at`
- `offline_story_progress_key`
- `offline_reward_revision`
- `offline_reward_last_balance_version`

Implementation options:

1. Add columns to `account_runtime_state`.
2. Add a separate `account_offline_reward_state` table.

Recommendation:

- Use a separate `account_offline_reward_state` table when implementation
  begins.
- Offline reward state has its own lifecycle, claim idempotency, and support
  needs. Keeping it separate avoids overloading `account_runtime_state`.

Possible table shape:

```sql
CREATE TABLE account_offline_reward_state (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  story_progress_key TEXT NOT NULL,
  last_claimed_at INTEGER NOT NULL,
  revision INTEGER NOT NULL CHECK (revision > 0),
  updated_at INTEGER NOT NULL
);
```

Claim history/ledger can be handled through:

- `account_mutations`;
- `economy_transactions`;
- `currency_ledger`;
- optional future `offline_reward_claims` read model if Admin/support needs
  queryable claim history beyond the economy ledger.

## API Contract Candidates

No API route is implemented in this slice.

Future preview endpoint:

```text
GET /v1/offline-rewards/preview
Authorization: Bearer <access-token>
```

Response candidate:

```json
{
  "claimable": true,
  "storyProgressKey": "opn_03_water_stop",
  "storyProgressMultiplierBps": 15200,
  "offlineStorageCapHours": 7,
  "actualOfflineSeconds": 25200,
  "claimableSeconds": 25200,
  "minimumOfflineMinutes": 10,
  "balanceVersion": "1",
  "rewardLines": [
    { "kind": "CURRENCY", "id": "cur_credits", "quantity": 1064 }
  ],
  "serverCalculatedAt": "2026-08-27T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

Future claim endpoint:

```text
POST /v1/offline-rewards/claim
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

Rules:

- Client does not send reward amounts.
- Client does not send story progress multiplier.
- Client does not send risk.
- Server resolves account and story progress.
- Server calculates claimable seconds and reward lines.
- Server writes economy ledger only inside claim transaction.
- Duplicate replay must not grant again.

Possible claim response:

```json
{
  "accepted": true,
  "resultStatus": "CLAIMED",
  "offlineRewardClaimId": "offline_claim_...",
  "storyProgressKey": "opn_03_water_stop",
  "rewardLines": [],
  "serverClaimedAt": "2026-08-27T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

## Story Progress Source of Truth

Story progress should come from server durable progress, not Unity request data.
The story progress read model is drafted in `docs/api/story-progress.md`.

Candidate sources:

- highest cleared Act 1 operation from account progression;
- raid completion/season state for `raid_raider_king_engine`;
- explicit account unlock/progression table if introduced later.

The client may display its local story state, but cannot choose the
`storyProgressKey` for reward calculation.

If the server cannot determine story progress:

- default to `none`;
- log a sanitized ops error candidate such as
  `offline_story_progress_missing`;
- do not use client-supplied progress as Source of Truth.

## Contract / Smoke Test Candidates

Add tests before implementation.

Progress scaling:

- `raid_raider_king_engine` multiplier > `opn_06_broken_line`.
- `opn_06_broken_line` multiplier > `opn_01_last_departure`.
- `opn_01_last_departure` multiplier > `none`.

Cap behavior:

- `none` caps at 4h.
- `opn_03_water_stop` caps at 7h.
- `raid_raider_king_engine` caps at 12h.
- Claimable seconds never exceed the story progress cap.

Minimum duration:

- Offline duration under 10 minutes returns no reward lines.
- Exactly 10 minutes can produce reward if rounded quantity is positive.

Risk independence:

- Same story progress with Risk I and Risk V produces the same offline
  multiplier and cap.
- Offline reward parser rejects or ignores risk fields from client input.

Resource safety:

- Default offline reward lines exclude `cur_special_core`.
- Default offline reward lines exclude `cur_command_ticket`.
- Default offline reward lines exclude `cur_command_ticket_fragment` for launch.

Client trust boundary:

- Unity-supplied offline reward amount returns `contract_violation`.
- Unity-supplied offline multiplier returns `contract_violation`.
- Unity-supplied story progress is not Source of Truth.

Duplicate replay:

- Same offline claim `Idempotency-Key` returns previous claim result.
- No second `currency_ledger` write.
- No second `account_mutations` write.

## Unity Impact

No current Unity behavior change is required by this design draft.

Future Unity DTO/UX candidates:

- story progress label;
- storage cap label;
- claimable offline time;
- server-calculated reward lines;
- minimum duration no-reward message;
- duplicate replay recognized claim state.

Unity must keep:

- `HasEconomyMutation=false` for preview/display states;
- no offline reward calculation;
- no risk multiplier calculation;
- no token/secret/raw server metadata in diagnostic JSON.

## Admin / Operations Impact

Future Admin read-only candidates:

- offline reward claim history;
- current story progress key;
- last offline reward claim timestamp;
- reward lines by resource;
- claimable hours vs capped hours;
- rejected claims due to minimum duration or clock tamper;
- suspicious excessive offline claim frequency.

Ops error candidates:

```text
offline_reward_policy_missing
offline_story_progress_missing
offline_reward_clock_tamper
offline_reward_claim_conflict
offline_reward_plan_mismatch
```

Audit boundary:

- Admin audit remains for operator actions.
- Runtime claim/reward failures belong in ops error logs.

## Open Decisions

- Exact base hourly reward quantities.
- Whether to use a separate `account_offline_reward_state` table or extend
  `account_runtime_state`.
- Whether preview endpoint is needed before claim endpoint.
- Whether offline rewards should use current active balance version or an
  account-stored offline reward balance version. Recommendation: current active
  version at claim time unless live migration support demands freezing.
- Whether raid milestone requires raid clear, season participation, or Act 1
  raid unlock only.
- Whether `cur_command_ticket_fragment` becomes a weekly/event/pass reward after
  Act 1 raid.
- Exact Admin metric names and retention window for offline reward claims.

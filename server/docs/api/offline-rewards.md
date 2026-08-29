# Offline Rewards Design Draft

This document defines the launch-scope story progression policy for offline
rewards, the preview route contract, and the first claim transaction slice.

It implements:

- `GET /v1/offline-rewards/preview`.
- `POST /v1/offline-rewards/claim`.
- Migration `0015_account_offline_reward_state.sql`.

It does not implement item grants, offline reward claim state consumption
outside the claim route, Unity calculation, Cloudflare DEV deploy, Cloudflare
secrets, remote smoke, or PRD resources.

## Goals

- Make offline rewards grow as the player restores Act 1 story infrastructure.
- Keep offline reward amounts as server Source of Truth.
- Keep risk rewards separate from offline rewards.
- Give operations and Admin enough metadata to debug offline reward plans later.
- Avoid repeated special-core, command-ticket, and ticket-fragment economy loops
  in launch scope.

## Non-Goals

- Migration `0015_account_offline_reward_state.sql` adds the durable preview
  baseline table.
- `GET /v1/offline-rewards/preview` is read-only.
- `POST /v1/offline-rewards/claim` is limited to launch currency reward lines.
- No item grants in this slice.
- No separate `offline_reward_claims` read model in this slice.
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

### Launch Economy Draft Fixture

The balance package exposes `launchEconomyBalanceDraft` as a testing/tuning
reference for the current launch economy draft. It records the draft offline
hourly quantities without promoting them into `launchOfflineRewardsConfig`, D1
seed data, DEV balance config, or production economy values.

Draft offline base hourly rewards:

```text
cur_credits          24/hour
cur_machine_parts     2/hour
cur_tactical_data     1/hour
cur_operator_exp     80/hour
```

Draft blocked repeat offline reward IDs remain:

- `cur_special_core`
- `cur_command_ticket`
- `cur_command_ticket_fragment`

These values are not final production economy values. Promotion into server
seed/config requires explicit approval, representative active/offline tests,
and compatibility checks with Unity/Admin display-only boundaries.

## Balance Config Schema Candidate

Store offline reward policy in immutable `balance_config_versions.config_json`
beside `runRewards`.

The first local contract slice added a pure `offlineRewards` parser and
calculator in the balance package. The second local contract slice exposes that
calculator through `GET /v1/offline-rewards/preview` with a read-only Game API
route.

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
Migration `0015_account_offline_reward_state.sql` adds that state table.

Existing candidate:

- `account_runtime_state` can hold account-level runtime flags today.

Offline reward state:

- `last_offline_reward_claim_at`: the only time boundary used to calculate
  `actualOfflineSeconds`.
- `last_story_progress_id`: support/debug snapshot of the story progress key
  used for the last successful claim.
- `last_balance_config_version`: support/debug snapshot of the balance version
  used for the last successful claim.
- `last_narrative_version`: optional read-only diagnostic metadata. It must not
  affect reward eligibility, unlock truth, or calculation.
- `last_previewed_at`: optional. Useful for Admin/support, but must not change
  claimable duration or consume rewards.
- `revision`: optimistic support counter for conflict/debug visibility.
- `updated_at`: last state update time.

- Offline reward state has its own lifecycle, claim idempotency, and support
  needs, so it is kept separate from `account_runtime_state`.
- Do not store reward amounts as mutable state in this table. Reward amounts
  belong in the claim transaction result and ledger rows.

Implemented table shape:

```sql
CREATE TABLE account_offline_reward_state (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  last_offline_reward_claim_at INTEGER NOT NULL,
  last_story_progress_id TEXT NOT NULL,
  last_balance_config_version INTEGER,
  last_narrative_version TEXT,
  last_previewed_at INTEGER,
  revision INTEGER NOT NULL CHECK (revision >= 0),
  updated_at INTEGER NOT NULL
);
```

Recommended indexes:

```sql
CREATE INDEX idx_account_offline_reward_state_last_claimed
  ON account_offline_reward_state(last_offline_reward_claim_at);

CREATE INDEX idx_account_offline_reward_state_story_progress
  ON account_offline_reward_state(last_story_progress_id);
```

Initial row policy:

- `POST /v1/auth/guest` creates an initial row in the same account creation
  batch.
- `last_offline_reward_claim_at` starts at account creation time.
- `last_story_progress_id = 'none'`.
- `last_balance_config_version = NULL`.
- `last_narrative_version = 'act1.2026-08-27.001'`.
- `last_previewed_at = NULL`.
- `revision = 0`.
- `updated_at = account.created_at`.
- Existing accounts without a row should be lazily initialized inside the first
  preview/claim read path using `accounts.created_at`, but claim should create
  the row in the same transaction before committing rewards.
- The current preview route reads `account_offline_reward_state` first. If no
  row exists for an older account, it falls back to `accounts.created_at`
  without writing. This keeps preview read-only and preserves backward
  compatibility.
- `last_previewed_at` is not updated by the current preview route, so preview
  reads cannot consume claimable duration.

Claim history/ledger can be handled through:

- `account_mutations`;
- `economy_transactions`;
- `currency_ledger`;
- optional future `offline_reward_claims` read model if Admin/support needs
  queryable claim history beyond the economy ledger.

## API Contract

The current implemented API boundary is preview-only. Claim/commit remains a
future candidate.

Preview endpoint:

```text
GET /v1/offline-rewards/preview
Authorization: Bearer <access-token>
```

Rules:

- Player Bearer auth is required.
- Account id comes only from the authenticated session context.
- Request body is not accepted.
- Query parameters are not accepted in the launch skeleton.
- Client-supplied account id, story progress, risk, offline duration, reward
  amount, reward lines, currency grants, item grants, or economy grants return
  `contract_violation`.
- The preview is read-only. It does not write ledger rows and does not consume
  offline claim state.
- `account_offline_reward_state.last_offline_reward_claim_at` is the preferred
  baseline.
- Existing accounts without an offline state row fall back to
  `accounts.created_at`.
- The endpoint does not update `last_previewed_at`; preview remains read-only.

Response:

```json
{
  "accepted": true,
  "previewStatus": "AVAILABLE",
  "storyProgressId": "opn_03_water_stop",
  "claimableSeconds": 25200,
  "claimableHours": 7,
  "offlineStorageCapHours": 7,
  "minimumClaimSeconds": 600,
  "maxClaimableHours": 12,
  "rewardLines": [
    {
      "kind": "CURRENCY",
      "id": "cur_credits",
      "quantity": 1064,
      "source": "OFFLINE_REWARD"
    }
  ],
  "calculation": {
    "storyProgressId": "opn_03_water_stop",
    "storyProgressMultiplierLabel": "1.52x",
    "cappedOfflineSeconds": 25200,
    "capSource": "STORY_PROGRESS_STORAGE_CAP"
  },
  "narrativeVersion": "act1.2026-08-27.001",
  "serverCalculatedAt": "2026-08-27T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

Empty preview:

```json
{
  "accepted": true,
  "previewStatus": "EMPTY",
  "storyProgressId": "none",
  "claimableSeconds": 0,
  "claimableHours": 0,
  "offlineStorageCapHours": 4,
  "minimumClaimSeconds": 600,
  "maxClaimableHours": 12,
  "rewardLines": [],
  "calculation": {
    "storyProgressId": "none",
    "storyProgressMultiplierLabel": "1.00x",
    "cappedOfflineSeconds": 0,
    "capSource": "ACTUAL_OFFLINE_DURATION"
  },
  "narrativeVersion": "act1.2026-08-27.001",
  "serverCalculatedAt": "2026-08-27T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

Rejected preview:

```json
{
  "accepted": false,
  "previewStatus": "REJECTED",
  "storyProgressId": null,
  "claimableSeconds": 0,
  "claimableHours": 0,
  "offlineStorageCapHours": null,
  "minimumClaimSeconds": 600,
  "maxClaimableHours": 12,
  "rewardLines": [],
  "calculation": null,
  "narrativeVersion": "act1.2026-08-27.001",
  "serverCalculatedAt": null,
  "errorCode": "contract_violation",
  "message": "GET /v1/offline-rewards/preview must not include query parameters or a request body."
}
```

Claim endpoint:

```text
POST /v1/offline-rewards/claim
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

Rules:

- `Idempotency-Key` is required.
- Client does not send reward amounts.
- Client does not send story progress multiplier.
- Client does not send risk.
- Client does not send story progress as Source of Truth.
- Request body should be empty for launch, or omitted entirely.
- Any body field that looks like account id, story progress, risk, offline
  duration, reward amount, reward lines, currency grants, item grants, economy
  grants, ledger ids, or multiplier metadata is `contract_violation`.
- Server resolves account and story progress from durable state.
- Server calculates claimable seconds and reward lines from
  `account_offline_reward_state.last_offline_reward_claim_at`.
- Server writes economy ledger only inside claim transaction.
- Duplicate replay must not grant again.
- Error responses are not stored as permanent idempotency results.

Recommended EMPTY claim policy:

- Reject EMPTY claims with `409 offline_reward_not_claimable`.
- Rationale: accepting no-op claims would move `last_offline_reward_claim_at`
  forward and punish accidental taps under the 10-minute minimum.
- The client should keep showing the preview countdown or no-reward state and
  retry later with a new claim attempt.

Claim response:

```json
{
  "accepted": true,
  "claimStatus": "CLAIMED",
  "offlineRewardClaimId": "offline_claim_...",
  "rewardTransactionId": "offline_claim_...",
  "economyTransactionId": "economy_...",
  "accountMutationId": "mutation_...",
  "storyProgressId": "opn_03_water_stop",
  "claimableSeconds": 25200,
  "claimableHours": 7,
  "offlineStorageCapHours": 7,
  "minimumClaimSeconds": 600,
  "maxClaimableHours": 12,
  "rewardLines": [
    {
      "kind": "CURRENCY",
      "id": "cur_credits",
      "quantity": 1064,
      "source": "OFFLINE_REWARD",
      "balanceAfter": 9320
    }
  ],
  "serverClaimedAt": "2026-08-27T00:00:00.000Z",
  "narrativeVersion": "act1.2026-08-27.001",
  "errorCode": null,
  "message": null
}
```

Duplicate replay:

```json
{
  "accepted": false,
  "claimStatus": "DUPLICATE_REPLAY",
  "offlineRewardClaimId": "offline_claim_...",
  "rewardTransactionId": "offline_claim_...",
  "economyTransactionId": "economy_...",
  "accountMutationId": "mutation_...",
  "storyProgressId": "opn_03_water_stop",
  "claimableSeconds": null,
  "claimableHours": null,
  "offlineStorageCapHours": null,
  "minimumClaimSeconds": 600,
  "maxClaimableHours": 12,
  "rewardLines": [],
  "serverClaimedAt": "2026-08-27T00:00:00.000Z",
  "narrativeVersion": "act1.2026-08-27.001",
  "errorCode": "duplicate_replay",
  "message": "Offline reward claim was already committed."
}
```

Rejected claim:

```json
{
  "accepted": false,
  "claimStatus": "REJECTED",
  "offlineRewardClaimId": null,
  "rewardTransactionId": null,
  "economyTransactionId": null,
  "accountMutationId": null,
  "storyProgressId": null,
  "claimableSeconds": 0,
  "claimableHours": 0,
  "offlineStorageCapHours": null,
  "minimumClaimSeconds": 600,
  "maxClaimableHours": 12,
  "rewardLines": [],
  "serverClaimedAt": null,
  "narrativeVersion": "act1.2026-08-27.001",
  "errorCode": "offline_reward_not_claimable",
  "message": "Offline reward is not claimable yet."
}
```

## Claim Idempotency Policy

Claim idempotency should follow the established account-scoped write pattern:

```text
accountId + operation = 'offline_reward_claim' + Idempotency-Key
```

Successful committed claims write `idempotency_records` with:

- `account_id`: authenticated account id;
- `operation`: `offline_reward_claim`;
- `idempotency_key`: client-generated `Idempotency-Key`;
- `result_ref`: committed `offlineRewardClaimId`;
- `created_at`: server commit timestamp.

Duplicate replay:

- Lookup occurs before calculating a fresh claim.
- If the same authenticated account and key already has a successful
  `offline_reward_claim`, return `DUPLICATE_REPLAY`.
- Do not recalculate reward amounts.
- Do not update `account_offline_reward_state`.
- Do not write `account_mutations`, `economy_transactions`, or
  `currency_ledger`.
- Return the recognized `offlineRewardClaimId`.
- `rewardLines` may be empty on replay; the client can refresh economy state
  through bootstrap/economy sync later.

Cross-account behavior:

- Because `idempotency_records` is scoped by `account_id`, the same key used by
  a different authenticated account is not a replay.
- Request body should not include account id. If it does, return
  `contract_violation`.

Same key, different body:

- Launch recommendation: request body is empty, so body mismatch cannot become
  a Source of Truth issue.
- If a future optional field is added, store a scrubbed request hash in a
  future claim read model or `result_ref` side table before allowing bodyful
  idempotency. Until then, any body content is `contract_violation`.

## Claim Transaction Boundary

The claim transaction must be a single D1 batch/transaction-equivalent unit.

Pre-transaction reads:

1. Resolve authenticated `accountId` through Player Bearer auth.
2. Validate `Idempotency-Key`.
3. Reject request bodies/query Source of Truth fields.
4. Lookup successful `idempotency_records` replay for
   `offline_reward_claim`.
5. Read or lazily prepare `account_offline_reward_state`.
6. Read `account_story_progress.highest_story_progress_id`.
7. Select active balance config/offline reward policy. Current local code uses
   `launchOfflineRewardsConfig`; future D1 integration should read the active
   immutable balance version.
8. Calculate preview using server state only.

Reject before transaction when:

- `actualOfflineSeconds` is below the 10-minute minimum or rewardLines are
  empty: `offline_reward_not_claimable`.
- stored story progress is unknown: `offline_story_progress_missing`.
- offline reward policy cannot be parsed: `offline_reward_policy_missing`.
- persisted state is impossible, such as `last_offline_reward_claim_at` in the
  future beyond allowed clock skew: `offline_reward_state_conflict` or
  `offline_reward_clock_tamper`.

The current minimal ops error write integration records
`offline_reward_state_conflict` as an open `game_api` error with account,
story progress, and hashed idempotency correlation. This write is fail-soft and
must not advance `last_offline_reward_claim_at`, insert claim idempotency
records, or write reward/economy ledgers.

Commit transaction order:

1. Insert `account_mutations` with mutation type `OFFLINE_REWARD_CLAIM`.
2. Insert `reward_transactions` with `run_id = NULL`,
   `balance_config_version`, and sanitized `reward_json`.
3. Insert `economy_transactions` with reason `OFFLINE_REWARD_CLAIM` and
   `source_type = 'OFFLINE_REWARD_CLAIM'`.
4. Upsert `account_currencies` for each currency reward line.
5. Insert `currency_ledger` rows with source reward line quantities and
   `balance_after`.
6. Upsert/update `account_offline_reward_state`:
   - `last_offline_reward_claim_at = serverClaimedAt`;
   - `last_story_progress_id = storyProgressId`;
   - `last_balance_config_version = balanceVersion`;
   - `last_narrative_version = narrativeVersion`;
   - `revision = revision + 1`;
   - `updated_at = serverClaimedAt`.
7. Insert `idempotency_records` with operation `offline_reward_claim` and
   `result_ref = offlineRewardClaimId`.

Partial-write prevention:

- If any insert/update fails, no ledger, state, or idempotency side effect may
  remain.
- `currency_ledger.balance_after` must match the post-upsert
  `account_currencies.balance`.
- `reward_transactions.reward_json` should store the same display reward line
  quantities used for ledger deltas, plus sanitized calculation metadata.

## Error Codes

Preview uses:

```text
auth_required
access_token_invalid
auth_session_invalid
account_not_active
contract_violation
offline_story_progress_missing
offline_reward_policy_missing
offline_reward_policy_invalid
```

Claim should add:

```text
duplicate_replay
offline_reward_not_claimable
offline_reward_state_missing
offline_reward_state_conflict
offline_reward_clock_tamper
offline_reward_claim_conflict
offline_reward_plan_mismatch
economy_mutation_conflict
```

Recoverable/display candidates:

- `offline_reward_not_claimable`
- `offline_reward_claim_conflict`
- `economy_mutation_conflict`

Auth/session candidates:

- `auth_required`
- `access_token_invalid`
- `auth_session_invalid`

Fatal/support candidates:

- `contract_violation`
- `offline_story_progress_missing`
- `offline_reward_policy_missing`
- `offline_reward_policy_invalid`
- `offline_reward_state_conflict`
- `offline_reward_clock_tamper`
- `offline_reward_plan_mismatch`

## Story Progress Source of Truth

Story progress should come from server durable progress, not Unity request data.
The story progress read model is drafted in `docs/api/story-progress.md`.

Candidate sources:

- highest cleared Act 1 operation from account progression;
- raid completion/season state for `raid_raider_king_engine`;
- explicit account unlock/progression table if introduced later.

The client may display its local story state, but cannot choose the
`storyProgressId` for reward calculation.

If the server cannot determine story progress:

- default to `none`;
- log a sanitized ops error candidate such as
  `offline_story_progress_missing`;
- do not use client-supplied progress as Source of Truth.

## Contract / Smoke Test Candidates

Current local contract tests cover the pure calculation boundary:

Progress scaling:

- `raid_raider_king_engine` multiplier > `none`.
- cap hours follow the story progress key.
- offline reward calculation metadata does not include risk.

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
- Act 1 gacha ticket acquisition is separate non-repeat launch config scope:
  login bonus 5, `opn_03` first clear 1, `opn_05` first clear 1, `opn_06`
  first clear 2, and story raid first clear 3. Offline repeat preview/claim
  must not provide those tickets.

Parser safety:

- `offlineRewards` must not include `riskMultipliersBps`.
- blocked default reward ids must not appear in `baseHourlyRewards`.
- unknown story progress keys fail with `offline_story_progress_missing`.

Client trust boundary:

- Unity-supplied offline reward amount returns `contract_violation`.
- Unity-supplied offline multiplier returns `contract_violation`.
- Unity-supplied story progress is not Source of Truth.

Current Game API preview contract tests cover:

- Missing Bearer auth returns `auth_required`.
- No story progress row returns `none` preview.
- Stored `account_story_progress.highest_story_progress_id` drives cap and
  multiplier.
- Stored `account_offline_reward_state.last_offline_reward_claim_at` is used as
  the preferred duration baseline.
- Older accounts without an offline reward state row fall back to
  `accounts.created_at`.
- Under 10 minutes returns `previewStatus = EMPTY` and no reward lines.
- Query/body Source of Truth fields return `contract_violation`.
- Response does not include blocked reward IDs, token/secret/raw RNG/account
  shortcut data, raw multiplier bps, or reward calculation internals.
- Local D1 smoke verifies guest auth plus migrated 0001-0015 schema.
- Local D1 smoke verifies migration 0001-0015 and an actual
  `account_offline_reward_state` row.

Current Game API claim contract tests cover:

- Missing `Idempotency-Key` returns `contract_violation`.
- Request query/body Source of Truth fields return `contract_violation`.
- Under 10 minutes returns `offline_reward_not_claimable` and does not advance
  `last_offline_reward_claim_at`.
- Successful claim inserts one account mutation, reward transaction, economy
  transaction, currency ledger row per reward line, offline state update, and
  idempotency record.
- Same offline claim `Idempotency-Key` returns previous claim result.
- No second `currency_ledger` write.
- No second `account_mutations` write.
- The same key used by another authenticated account is an independent claim.
- Claim response does not include blocked reward IDs, token/secret/raw
  RNG/account shortcut data, raw multiplier bps, or reward calculation
  internals.

Future claim hardening tests should cover:

- Unknown persisted story progress returns `offline_story_progress_missing`.
- Future `last_offline_reward_claim_at` returns
  `offline_reward_state_conflict` or `offline_reward_clock_tamper`.

## Unity Impact

No current Unity behavior change is required by this design draft.

Current Unity preview DTO/display/debug can use:

- story progress label;
- storage cap label;
- claimable offline time;
- server-calculated reward lines;
- minimum duration no-reward message;

Future Unity claim DTO/UX candidates:

- claimed state;
- duplicate replay recognized claim state.
- not-claimable recoverable state.

Unity must keep:

- `HasEconomyMutation=false` for preview/display states;
- no offline reward calculation;
- no risk multiplier calculation;
- no token/secret/raw server metadata in diagnostic JSON.

## Admin / Operations Impact

Current Admin read-only endpoints:

```text
GET /v1/admin/offline-rewards
GET /v1/admin/offline-rewards/:accountId
```

They require `OFFLINE_REWARD_READ` through the current `ROLE_PERMISSION`
mapping and expose:

- offline reward claim history;
- current story progress key;
- last offline reward claim timestamp;
- offline reward state source, including legacy `accounts.created_at` fallback;
- latest offline reward claim id and claim timestamp;
- read-only support context.

The customer support account summary draft in `docs/api/support.md` can compose
this same offline reward state as one read-only section. It must not add offline
timestamp reset, reward repair, or manual grant controls.

Future Admin read-only candidates:

- reward lines by resource;
- claimable hours vs capped hours;
- rejected claims due to minimum duration or clock tamper;
- suspicious excessive offline claim frequency.

Ops error candidates:

```text
offline_reward_policy_missing
offline_story_progress_missing
offline_reward_clock_tamper
offline_reward_state_conflict
offline_reward_claim_conflict
offline_reward_plan_mismatch
```

Audit boundary:

- Admin audit remains for operator actions.
- Runtime claim/reward failures belong in ops error logs.

## Open Decisions

- Exact base hourly reward quantities.
- Whether to add a separate `offline_reward_claims` read model for Admin
  support, or rely on `reward_transactions` + ledger for launch.
- Whether offline rewards should use current active balance version or an
  account-stored offline reward balance version. Recommendation: current active
  version at claim time unless live migration support demands freezing.
- Whether raid milestone requires raid clear, season participation, or Act 1
  raid unlock only.
- Whether `cur_command_ticket_fragment` becomes a weekly/event/pass reward after
  Act 1 raid.
- Exact Admin metric names and retention window for offline reward claims.

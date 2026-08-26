# Gacha / Pity / Result History DB Design Draft

This draft defines the DB shape for operator recruitment history, banner pity
state, duplicate conversion, and economy/inventory ledger linkage.

It is a design draft for the next migration/API phase. It does not implement
pull probability logic, payment processing, or admin UI.

## Goals

- Store gacha history in D1 as durable Source of Truth.
- Restore banner-specific pity/guarantee state per account.
- Preserve both pull batch/request history and individual pull result history.
- Link command ticket spend and granted duplicate/material rewards to ledgers.
- Support user support, recovery, probability audit, integrity review, and
  future admin screens.

## Existing Schema Compatibility

Before migration `0013`, existing migration `0001` to `0012` status:

- `accounts(id)` exists and is the account FK target.
- `account_mutations` exists and should serialize gacha writes per account.
- `account_currencies` and `currency_ledger` exist, but their CHECK constraints
  currently allow only:
  - `CREDITS`
  - `MACHINE_PARTS`
  - `TACTICAL_DATA`
  - `SPECIAL_CORE`
  - `OPERATOR_EXP`
- `account_operators` exists and can receive newly acquired operators.
- `progression_ledger` exists and can record operator acquisition/progression
  deltas.
- `idempotency_records` exists and should store successful `gacha_pull`
  operations.
- No inventory/item balance table exists yet.

Design consequence:

- Keep the existing currency ledger unchanged for the five canonical permanent
  currencies.
- Add an item/resource inventory ledger for command tickets, fragments, operator
  tokens, fuel cans, and material-like gacha rewards.
- Do not widen existing currency CHECK constraints in this migration unless a
  larger economy v2 migration is explicitly approved.

## Stable IDs

Use the integration planning stable IDs as stored IDs:

```text
cur_command_ticket
cur_command_ticket_fragment
cur_operator_token
cur_raid_token
cur_fuel
item_fuel_can_s
item_fuel_can_m
item_fuel_can_l
```

Existing server currency rows keep the current uppercase enum values for the
five already-migrated currencies.

Operator result IDs use stable operator IDs:

```text
op_kyle
op_reina
op_eve
op_sera
op_mira
op_vex
op_rin
op_nyx
op_cipher
op_iris
```

## New Inventory Ledger

Use this for ticket-like resources and item/material rewards that are not part
of the existing `account_currencies.currency` CHECK enum.

```sql
CREATE TABLE account_items (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  item_id TEXT NOT NULL,
  quantity INTEGER NOT NULL CHECK (quantity >= 0),
  updated_at INTEGER NOT NULL,
  PRIMARY KEY(account_id, item_id)
);

CREATE TABLE item_ledger (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  item_id TEXT NOT NULL,
  delta INTEGER NOT NULL,
  quantity_after INTEGER NOT NULL CHECK (quantity_after >= 0),
  reason TEXT NOT NULL,
  source_type TEXT NOT NULL,
  source_id TEXT,
  created_at INTEGER NOT NULL
);
```

Recommended index:

```sql
CREATE INDEX idx_item_ledger_account_item_created
  ON item_ledger(account_id, item_id, created_at);
```

## Banner Definitions

`gacha_banners` stores the server-visible banner definition snapshot. Numeric
probability and pool data should still come from versioned balance config; this
row stores the operational banner identity and active window.

```sql
CREATE TABLE gacha_banners (
  id TEXT PRIMARY KEY,
  banner_type TEXT NOT NULL CHECK (banner_type IN ('STANDARD','PICKUP')),
  status TEXT NOT NULL CHECK (status IN ('DRAFT','ACTIVE','ENDED','DISABLED')),
  balance_config_version INTEGER NOT NULL REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  pickup_operator_id TEXT,
  starts_at INTEGER NOT NULL,
  ends_at INTEGER,
  pity_config_json TEXT NOT NULL,
  pool_snapshot_json TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL,
  CHECK (ends_at IS NULL OR ends_at > starts_at),
  CHECK (
    (banner_type = 'PICKUP' AND pickup_operator_id IS NOT NULL)
    OR (banner_type != 'PICKUP' AND pickup_operator_id IS NULL)
  )
);
```

Rules:

- `pickup_operator_id` is required for `PICKUP` banners.
- `pool_snapshot_json` stores the resolved pool IDs/rates used for audit.
- Pull execution currently expects `pool_snapshot_json.results[]` entries with
  stable `resultRarity`, `resultType`, `resultId`, and optional positive
  `weight`, `quantity`, and `isPickup`.
- Normal weighted selection only considers candidates with positive `weight`.
- Guarantee enforcement may select an eligible candidate with `weight: 0`; this
  allows a frozen pool to keep guarantee-only rare/elite/pickup candidates
  audit-visible without making them part of ordinary weighted selection.
- `pity_config_json` stores thresholds such as 10/30/60 so historic pulls can
  be audited even after balance changes.
- Pull execution must use the banner's frozen `balance_config_version` and pool
  snapshot, not the current active balance version.

## Account Banner Pity State

`player_gacha_pity` stores the current state per account and banner.

```sql
CREATE TABLE player_gacha_pity (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  banner_id TEXT NOT NULL REFERENCES gacha_banners(id) ON DELETE RESTRICT,
  total_pull_count INTEGER NOT NULL DEFAULT 0 CHECK (total_pull_count >= 0),
  elite_pity_count INTEGER NOT NULL DEFAULT 0 CHECK (elite_pity_count >= 0),
  pickup_elite_pity_count INTEGER NOT NULL DEFAULT 0 CHECK (pickup_elite_pity_count >= 0),
  rare_or_higher_pity_count INTEGER NOT NULL DEFAULT 0 CHECK (rare_or_higher_pity_count >= 0),
  revision INTEGER NOT NULL DEFAULT 1 CHECK (revision > 0),
  last_batch_id TEXT,
  updated_at INTEGER NOT NULL,
  PRIMARY KEY(account_id, banner_id)
);
```

Meaning:

- `elite_pity_count`: count since last elite result, max threshold 30.
- `pickup_elite_pity_count`: count since last pickup elite result, max
  threshold 60 for pickup banners.
- `rare_or_higher_pity_count`: count since last rare-or-higher result, max
  threshold 10.
- `total_pull_count`: lifetime count for this account/banner pair.

All counters are updated in the same transaction that writes pull results.

## Pull Batch / Request History

`gacha_pull_batches` stores one request, such as a single pull or 10-pull.

```sql
CREATE TABLE gacha_pull_batches (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  banner_id TEXT NOT NULL REFERENCES gacha_banners(id) ON DELETE RESTRICT,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  idempotency_key TEXT NOT NULL,
  pull_count INTEGER NOT NULL CHECK (pull_count IN (1,10)),
  cost_item_id TEXT NOT NULL,
  cost_quantity INTEGER NOT NULL CHECK (cost_quantity > 0),
  cost_ledger_id TEXT REFERENCES item_ledger(id) ON DELETE RESTRICT,
  pre_pity_json TEXT NOT NULL,
  post_pity_json TEXT NOT NULL,
  rng_seed_hash TEXT NOT NULL,
  result_summary_json TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  CHECK (cost_quantity = pull_count),
  UNIQUE(account_id, banner_id, idempotency_key)
);
```

Rules:

- `cost_item_id` should be `cur_command_ticket` for the initial policy.
- `cost_quantity` should equal `pull_count`.
- `rng_seed_hash` stores a hash or commitment, not raw sensitive RNG material.
- `pre_pity_json` and `post_pity_json` allow support/audit reconstruction.
- `result_summary_json.responseResults` stores the client-visible result shape
  used for idempotency replay, so display-only fields such as item quantity and
  `guaranteeType` are replayed exactly without re-running grant logic.
- Successful batches write `idempotency_records` with operation `gacha_pull`.
- Error responses are not stored as permanent idempotency results.

## Individual Pull Results

`gacha_pull_results` stores every pull result inside a batch.

```sql
CREATE TABLE gacha_pull_results (
  id TEXT PRIMARY KEY,
  batch_id TEXT NOT NULL REFERENCES gacha_pull_batches(id) ON DELETE CASCADE,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  banner_id TEXT NOT NULL REFERENCES gacha_banners(id) ON DELETE RESTRICT,
  pull_sequence INTEGER NOT NULL CHECK (pull_sequence > 0),
  result_rarity TEXT NOT NULL CHECK (result_rarity IN ('COMMON','RARE','ELITE')),
  result_type TEXT NOT NULL CHECK (
    result_type IN (
      'ELITE_OPERATOR',
      'RARE_OPERATOR',
      'ELITE_OPERATOR_SHARD',
      'RARE_OPERATOR_SHARD',
      'OPERATOR_TOKEN',
      'TRAINING_MATERIAL',
      'WORK_SKILL_MATERIAL'
    )
  ),
  result_id TEXT NOT NULL,
  is_pickup INTEGER NOT NULL CHECK (is_pickup IN (0,1)),
  guarantee_type TEXT NOT NULL DEFAULT 'NONE' CHECK (
    guarantee_type IN (
      'NONE',
      'RARE_OR_HIGHER_10',
      'ELITE_30',
      'PICKUP_ELITE_60'
    )
  ),
  was_duplicate INTEGER NOT NULL CHECK (was_duplicate IN (0,1)),
  duplicate_conversion_json TEXT,
  operator_id TEXT,
  item_ledger_id TEXT REFERENCES item_ledger(id) ON DELETE RESTRICT,
  progression_ledger_id TEXT REFERENCES progression_ledger(id) ON DELETE RESTRICT,
  created_at INTEGER NOT NULL,
  UNIQUE(batch_id, pull_sequence)
);
```

Rules:

- `pull_sequence` is 1-based within the batch.
- `result_id` is the stable ID awarded by the roll.
- `operator_id` is set when the roll relates to an operator.
- `guarantee_type = 'NONE'` should be stored instead of NULL for normal pulls.
- `duplicate_conversion_json` records the exact duplicate conversion result.
- `item_ledger_id` links shard/token/material grants.
- `progression_ledger_id` links newly acquired operator insertion or existing
  operator progression deltas.

Duplicate conversion policy:

```text
Duplicate elite operator -> 50 shards for that operator
Duplicate rare operator  -> 30 shards for that operator
```

The converted shard grant is written to `item_ledger`; the pull result row keeps
both the original duplicate and conversion metadata.

Shard storage policy:

- API/display payloads keep the stable item family as `cur_operator_token` and
  include `operatorId` to identify the specific operator shard.
- D1 `account_items.item_id` stores specific operator shard balances as
  `cur_operator_token:<operatorId>`, for example `cur_operator_token:op_vex`.
- `duplicate_conversion_json.itemId` remains `cur_operator_token` and
  `duplicate_conversion_json.operatorId` carries the specific operator ID.

## Transaction Shape

Gacha pull execution must be one account mutation transaction.

Order:

1. Read account state, banner state, and current pity state.
2. Validate banner ACTIVE window and pull count.
3. Validate `cur_command_ticket` balance in `account_items`.
4. Insert `account_mutations` with mutation type `GACHA_PULL`.
5. Spend command tickets via `item_ledger` and `account_items`.
6. Resolve all pull results using banner snapshot and pre-pity state.
7. Insert `gacha_pull_batches`.
8. Insert one `gacha_pull_results` row per pull.
9. Insert/update `account_operators` for new operators.
10. Insert `progression_ledger` rows for acquisition/progression deltas.
11. Grant duplicate shards/tokens/materials via `item_ledger` and
    `account_items`.
12. Update `player_gacha_pity`.
13. Insert `idempotency_records` operation `gacha_pull`.

No result row should exist without its batch. No batch should commit without its
cost spend and pity update.

## Pull API Contract

This section is the contract baseline for implementing the Game API route. The
DB schema above remains the durable Source of Truth.

Account identity boundary:

- Gacha pull is an account-scoped economy write.
- `account_id` comes from the verified Game API account auth context.
- The auth context is produced from LAST RAIL `Authorization: Bearer` access
  tokens, not Cloudflare Access.
- Request body and client headers must not provide or override `account_id`.
- Remote DEV follows the same auth path as PRD. Until that path exists, gacha
  pull commit remains blocked behind the explicit placeholder route.
- Tests may inject account context through mocks/harnesses only.
- Idempotency replay lookup uses the authenticated `account_id` with
  `operation = 'gacha_pull'` and `Idempotency-Key`.

Endpoint:

```text
POST /v1/gacha/banners/:bannerId/pulls
Idempotency-Key: <client-generated-key>
```

Method:

- `POST`

Path/body `bannerId` policy:

- `:bannerId` in the path is the only banner identifier accepted by the API.
- The request body must not include `bannerId`.
- If a client sends `bannerId` in the body, the server returns
  `400 contract_violation`.
- This avoids two competing banner identifiers in a write request.

Request body:

```json
{
  "pullCount": 10
}
```

Request rules:

- `Idempotency-Key` header is required and must be non-empty.
- `pullCount` must be `1` or `10`.
- The server reads banner status, active window, balance config version, pool
  snapshot, pity config, account item balance, and current pity state from D1.
- The client does not send cost, rates, pity counters, result IDs, duplicate
  conversion, granted resources, or RNG material.

Success response:

```json
{
  "accepted": true,
  "resultStatus": "ACCEPTED",
  "batchId": "gacha_batch_...",
  "bannerId": "banner_...",
  "pullCount": 10,
  "cost": {
    "itemId": "cur_command_ticket",
    "quantity": 10
  },
  "results": [
    {
      "pullSequence": 1,
      "resultRarity": "ELITE",
      "resultType": "ELITE_OPERATOR",
      "resultId": "op_vex",
      "reward": {
        "kind": "OPERATOR",
        "id": "op_vex",
        "quantity": 1
      },
      "isPickup": true,
      "guaranteeType": "PICKUP_ELITE_60",
      "wasDuplicate": false,
      "duplicateConversion": null
    }
  ],
  "postPityState": {
    "totalPullCount": 130,
    "elitePityCount": 0,
    "pickupElitePityCount": 0,
    "rareOrHigherPityCount": 0,
    "revision": 12
  },
  "createdAt": "2026-08-26T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

Duplicate idempotency replay response:

```json
{
  "accepted": false,
  "resultStatus": "DUPLICATE_REPLAY",
  "batchId": "gacha_batch_...",
  "bannerId": "banner_...",
  "pullCount": 10,
  "cost": {
    "itemId": "cur_command_ticket",
    "quantity": 10
  },
  "results": [
    {
      "pullSequence": 1,
      "resultRarity": "ELITE",
      "resultType": "ELITE_OPERATOR",
      "resultId": "op_vex",
      "reward": {
        "kind": "OPERATOR",
        "id": "op_vex",
        "quantity": 1
      },
      "isPickup": true,
      "guaranteeType": "PICKUP_ELITE_60",
      "wasDuplicate": false,
      "duplicateConversion": null
    }
  ],
  "postPityState": {
    "totalPullCount": 130,
    "elitePityCount": 0,
    "pickupElitePityCount": 0,
    "rareOrHigherPityCount": 0,
    "revision": 12
  },
  "createdAt": "2026-08-26T00:00:00.000Z",
  "errorCode": "duplicate_replay",
  "message": "Gacha pull was already committed."
}
```

Rejected response:

```json
{
  "accepted": false,
  "resultStatus": "REJECTED",
  "batchId": null,
  "bannerId": "banner_...",
  "pullCount": 10,
  "cost": {
    "itemId": "cur_command_ticket",
    "quantity": 10
  },
  "results": [],
  "postPityState": null,
  "createdAt": null,
  "errorCode": "insufficient_command_ticket",
  "message": "Not enough command tickets."
}
```

HTTP status rules:

- `200` + `ACCEPTED` for committed pulls.
- `200` + `DUPLICATE_REPLAY` for successful idempotency replay.
- `400` + `REJECTED` for request contract violations.
- `404` + `REJECTED` for unknown banners.
- `409` + `REJECTED` for account state conflicts or insufficient resources.
- `503` + `REJECTED` for temporarily unavailable gacha validation/execution.

`resultStatus` enum:

```text
ACCEPTED
DUPLICATE_REPLAY
REJECTED
```

Cost policy:

- Initial gacha cost is always `cur_command_ticket`.
- Cost quantity equals `pullCount`.
- The cost is spent from `account_items` and recorded in `item_ledger`.
- Cost is server-calculated. The client only displays the returned `cost`.

Pity counter policy:

- DB stores both `pre_pity_json` and `post_pity_json` in
  `gacha_pull_batches`.
- `pre_pity_json` is the state used to calculate this batch.
- `post_pity_json` is the state after every pull in the batch has been applied.
- API response exposes `postPityState` only; this is the state Unity should show
  after the pull.
- Admin/support read APIs can expose pre/post state later from batch history.
- Counter meanings:
  - `totalPullCount`: total pulls after this committed batch.
  - `elitePityCount`: pulls since the latest elite after this batch.
  - `pickupElitePityCount`: pulls since the latest pickup elite after this
    batch. For `STANDARD` banners this remains `0`.
  - `rareOrHigherPityCount`: pulls since the latest rare-or-higher after this
    batch.
  - `revision`: `player_gacha_pity.revision` after commit.

Guarantee enforcement order per pull:

1. `PICKUP_ELITE_60` for pickup banners when the next pull reaches the pickup
   elite threshold.
2. `ELITE_30` when the next pull reaches the elite threshold.
3. `RARE_OR_HIGHER_10` when the next pull reaches the rare-or-higher threshold.
4. `NONE` for ordinary weighted selection.

The selected result is applied immediately before evaluating the next pull in a
10-pull batch, so later pulls use the updated pity counters.

Pull result shape:

- `pullSequence`: 1-based sequence within the batch.
- `resultRarity`: `COMMON`, `RARE`, or `ELITE`.
- `resultType`: exactly one of the `gacha_pull_results.result_type` enum values.
- `resultId`: stable ID of the rolled result.
- `reward`: displayable grant summary derived from the committed DB writes.
- `isPickup`: true only when the result is the banner pickup operator/result.
- `guaranteeType`: `NONE`, `RARE_OR_HIGHER_10`, `ELITE_30`, or
  `PICKUP_ELITE_60`.
- `wasDuplicate`: true when a rolled operator was already owned and converted.
- `duplicateConversion`: null for non-duplicates, otherwise the exact conversion
  grant recorded in `duplicate_conversion_json`.

Duplicate conversion response example:

```json
{
  "pullSequence": 4,
  "resultRarity": "ELITE",
  "resultType": "ELITE_OPERATOR",
  "resultId": "op_vex",
  "reward": {
    "kind": "ITEM",
    "id": "cur_operator_token",
    "quantity": 50,
    "operatorId": "op_vex"
  },
  "isPickup": true,
  "guaranteeType": "NONE",
  "wasDuplicate": true,
  "duplicateConversion": {
    "sourceResultType": "ELITE_OPERATOR",
    "sourceResultId": "op_vex",
    "itemId": "cur_operator_token",
    "operatorId": "op_vex",
    "quantity": 50
  }
}
```

Reward summary `kind` values:

```text
OPERATOR
ITEM
CURRENCY
```

The `CURRENCY` kind is reserved for the five canonical server currencies if
gacha ever grants them. Command tickets, fragments, operator tokens, fuel cans,
shards, and material-like rewards should use `ITEM`.

Idempotency duplicate replay policy:

- Only successful committed `gacha_pull` operations are stored in
  `idempotency_records`.
- Error responses are not stored as permanent idempotency results.
- Replay lookup uses:
  - `account_id`
  - `operation = 'gacha_pull'`
  - `Idempotency-Key`
- On replay, the server returns `200 DUPLICATE_REPLAY` and reconstructs the
  response from `gacha_pull_batches.result_summary_json.responseResults`,
  `gacha_pull_results`, and stored post-pity data.
- The server must not spend command tickets again.
- The server must not grant operators/items/currencies again.
- `UNIQUE(account_id, banner_id, idempotency_key)` on `gacha_pull_batches` is
  retained as a domain duplicate guard.

Server Source of Truth boundary:

- Pull probability, pity activation, duplicate conversion, cost spend, operator
  acquisition, item grants, and ledger writes are server responsibilities.
- Unity does not calculate or trust cost, pity, probability, reward, duplicate
  conversion, or ledger results.
- `rng_seed_hash` is a commitment/hash only. Raw RNG material is not returned to
  the client.
- All permanent mutations occur in one account mutation transaction.

## Error Codes

Recoverable:

```text
gacha_state_conflict
gacha_temporarily_unavailable
```

Fatal:

```text
contract_violation
banner_not_found
banner_inactive
insufficient_command_ticket
invalid_pull_count
gacha_pool_unavailable
gacha_result_persist_failed
```

## Admin / Analytics Read Model

The write model above supports later read-only admin endpoints:

- Pull volume by banner.
- Account pull history.
- Pity state lookup.
- Duplicate conversion audit.
- Pickup guarantee audit.
- Probability verification by result rarity/type.
- Suspicious repeated pull or impossible result investigation.
- Dashboard metrics for command ticket spend and reward/material grants.

Recommended query indexes:

```sql
CREATE INDEX idx_gacha_pull_batches_account_created
  ON gacha_pull_batches(account_id, created_at);

CREATE INDEX idx_gacha_pull_batches_banner_created
  ON gacha_pull_batches(banner_id, created_at);

CREATE INDEX idx_gacha_pull_results_banner_rarity_created
  ON gacha_pull_results(banner_id, result_rarity, created_at);

CREATE INDEX idx_gacha_pull_results_account_created
  ON gacha_pull_results(account_id, created_at);
```

## Migration Impact

Migration `0013_gacha_pity_result_history.sql` adds the gacha/pity/result
history schema after `0012_run_reward_previews.sql`.

Tables:

- `account_items`
- `item_ledger`
- `gacha_banners`
- `player_gacha_pity`
- `gacha_pull_batches`
- `gacha_pull_results`

Indexes:

- `idx_item_ledger_account_item_created`
- `idx_gacha_pull_batches_account_created`
- `idx_gacha_pull_batches_banner_created`
- `idx_gacha_pull_results_banner_rarity_created`
- `idx_gacha_pull_results_account_created`

Current conflict status:

- No conflict with `accounts(id)`.
- No conflict with `account_mutations`.
- No conflict with `account_operators`.
- No conflict with existing `currency_ledger`, because new ticket/material
  resources use `item_ledger`.
- Requires a new inventory ledger because current currency CHECK constraints do
  not include `cur_command_ticket`, fragments, operator tokens, raid tokens, fuel
  cans, or material-like rewards.

Open design choice for implementation:

- Whether to keep command tickets in `account_items` permanently, or later move
  them into a generalized resource ledger. The current draft favors
  `account_items` to avoid a risky rebuild of existing currency tables.

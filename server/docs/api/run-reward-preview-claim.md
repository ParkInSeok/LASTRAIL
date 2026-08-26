# Run Reward Preview / Claim Contract Draft

This draft defines the reward review and claim flow that follows an accepted
run finish submission.

It extends `docs/api/run-result-submit.md` without changing that contract:
`POST /v1/runs/:runId/finish` returns `rewardPreviewId` first, and does not
return reward amounts or final grants.

## Ownership Boundary

The server owns reward calculation, reward preview state, claim commitment,
economy mutation, and ledger writes.

Unity may display reward preview data returned by the server, but Unity never
calculates final reward amounts and never sends reward amounts back to the
server as claim input.

## Flow

```text
POST /v1/runs/:runId/finish
  -> accepted result submit
  -> server validates the run
  -> server calculates a reward preview from frozen run state
  -> server stores the preview
  -> response includes rewardPreviewId

GET /v1/runs/:runId/reward-preview/:rewardPreviewId
  -> Unity opens reward review UX
  -> server returns preview state and displayable reward lines

POST /v1/runs/:runId/reward-preview/:rewardPreviewId/claim
  -> Unity confirms claim
  -> server creates account mutation
  -> server writes reward transaction
  -> server writes economy transaction and currency ledger
  -> server marks preview CLAIMED
  -> response includes rewardClaimId
```

## Reward Preview Creation

The server creates `rewardPreviewId` during the accepted `run_finish`
transaction, after validation passes and before the run finish response is
returned.

Preview creation is not a grant. It is a stored server-calculated plan for the
reward review screen.

Rules:

- The preview is derived from `runs.balance_config_version`.
- The preview uses server run state, wave plan, final result, first-clear
  eligibility, and integrity outcome.
- The preview never uses client-submitted reward amounts.
- The preview should be immutable after creation.
- A run may have at most one active reward preview.
- A run may have at most one committed reward claim.

## Preview State Model

Use `run_reward_previews`, introduced by migration `0012_run_reward_previews.sql`.

```sql
CREATE TABLE run_reward_previews (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE,
  balance_config_version INTEGER NOT NULL REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  status TEXT NOT NULL CHECK (status IN ('ACTIVE','CLAIMED','EXPIRED','INVALIDATED')),
  reward_plan_json TEXT NOT NULL,
  expires_at INTEGER NOT NULL,
  created_at INTEGER NOT NULL,
  claimed_at INTEGER,
  reward_claim_id TEXT REFERENCES reward_transactions(id) ON DELETE SET NULL,
  CHECK (expires_at > created_at),
  CHECK (
    (status = 'CLAIMED' AND claimed_at IS NOT NULL AND reward_claim_id IS NOT NULL)
    OR (status != 'CLAIMED' AND claimed_at IS NULL AND reward_claim_id IS NULL)
  ),
  UNIQUE(run_id),
  UNIQUE(reward_claim_id)
);
```

Recommended indexes:

```sql
CREATE INDEX idx_run_reward_previews_account_status_expires
  ON run_reward_previews(account_id, status, expires_at);

CREATE INDEX idx_run_reward_previews_run_status
  ON run_reward_previews(run_id, status);
```

`reward_claim_id` references `reward_transactions(id)`. The claim transaction
creates the reward transaction first, then marks the preview `CLAIMED`.

## Preview Expiration

Initial policy:

- `expiresAt = createdAt + server runtime rewardPreviewTtlMillis`.
- DEV default recommendation: 24 hours.
- Expired previews cannot be claimed.
- Expiration does not delete the preview row.
- A player reopening a finished but unclaimed run after expiration should
  receive `REWARD_PREVIEW_EXPIRED`.

Do not use a cron job to rewrite every expired preview. Treat expiration as a
read/claim-time condition.

## Preview Lookup Endpoint

```text
GET /v1/runs/:runId/reward-preview/:rewardPreviewId
```

Response:

```json
{
  "runId": "run_...",
  "rewardPreviewId": "reward_preview_...",
  "status": "ACTIVE",
  "balanceVersion": "1",
  "expiresAt": "2026-08-27T00:00:00.000Z",
  "serverCalculatedAt": "2026-08-26T00:00:00.000Z",
  "rewardLines": [
    {
      "kind": "CURRENCY",
      "id": "cur_credits",
      "quantity": 1200,
      "source": "RUN_CLEAR",
      "adBonusEligible": true
    },
    {
      "kind": "CURRENCY",
      "id": "cur_operator_exp",
      "quantity": 700,
      "source": "OPERATOR_EXP",
      "adBonusEligible": false
    }
  ],
  "claim": {
    "claimable": true,
    "rewardClaimId": null,
    "claimedAt": null,
    "errorCode": null
  }
}
```

Rules:

- `balanceVersion` is a string in the API response.
- Quantities are safe integers and server-calculated.
- `rewardLines` are displayable preview data, not a committed ledger.
- The response must not expose internal ledger IDs before claim.
- If already claimed, return `status: "CLAIMED"` with `claim.rewardClaimId`.

Errors:

```text
reward_preview_not_found
reward_preview_run_mismatch
reward_preview_account_mismatch
reward_preview_expired
reward_preview_invalidated
```

## Claim Endpoint

```text
POST /v1/runs/:runId/reward-preview/:rewardPreviewId/claim
Idempotency-Key: <client-generated-key>
```

Request body:

```json
{
  "runId": "run_...",
  "rewardPreviewId": "reward_preview_..."
}
```

Rules:

- `Idempotency-Key` is required.
- Path and body IDs must match.
- Unity does not send reward amounts.
- Claim must run in one D1 transaction.
- Successful committed claims write `idempotency_records` with operation
  `run_reward_claim`.
- Error responses are not stored as permanent idempotency results.
- Duplicate idempotency replay returns the original committed claim reference.

## Reward Claim Creation

`rewardClaimId` is created only inside the claim/commit transaction.

The claim transaction writes, in order:

1. `account_mutations` with mutation type `RUN_REWARD_CLAIM`.
2. `reward_transactions` with `id = rewardClaimId`, `run_id`, frozen
   `balance_config_version`, and `reward_json`.
3. `economy_transactions` with reason `RUN_REWARD_CLAIM` and
   `source_type = 'RUN_REWARD_PREVIEW'`.
4. `currency_ledger` rows and `account_currencies` balance updates for each
   currency reward line.
5. `run_reward_previews.status = 'CLAIMED'`, `claimed_at`, and
   `reward_claim_id`.
6. `idempotency_records` for operation `run_reward_claim`.

The existing `reward_transactions.UNIQUE(run_id)` remains the domain duplicate
guard. Even if idempotency handling fails, a second committed run reward for the
same run must not be possible.

## Claim Response

Success:

```json
{
  "runId": "run_...",
  "rewardPreviewId": "reward_preview_...",
  "rewardClaimId": "reward_claim_...",
  "status": "CLAIMED",
  "serverClaimedAt": "2026-08-26T00:00:00.000Z",
  "balanceVersion": "1",
  "rewardLines": [
    {
      "kind": "CURRENCY",
      "id": "cur_credits",
      "quantity": 1200,
      "source": "RUN_CLEAR",
      "balanceAfter": 9320
    }
  ]
}
```

Duplicate replay:

```json
{
  "runId": "run_...",
  "rewardPreviewId": "reward_preview_...",
  "rewardClaimId": "reward_claim_...",
  "status": "DUPLICATE_REPLAY",
  "serverClaimedAt": "2026-08-26T00:00:00.000Z",
  "balanceVersion": "1",
  "rewardLines": []
}
```

Duplicate replay may omit `rewardLines` if the client should refresh economy
via bootstrap/economy sync. The stable requirement is that it returns the
recognized `rewardClaimId` and does not grant again.

Errors:

```text
contract_violation
reward_preview_not_found
reward_preview_expired
reward_preview_invalidated
reward_already_claimed
reward_claim_conflict
reward_plan_unavailable
economy_mutation_conflict
```

Recommended recoverable errors:

```text
reward_claim_conflict
economy_mutation_conflict
```

Recommended fatal errors:

```text
contract_violation
reward_preview_not_found
reward_preview_expired
reward_preview_invalidated
reward_already_claimed
reward_plan_unavailable
```

## Unity DTO Needs

Reward preview lookup DTO:

```text
runId: string
rewardPreviewId: string
status: ACTIVE | CLAIMED | EXPIRED | INVALIDATED
balanceVersion: string
expiresAt: ISO UTC string
serverCalculatedAt: ISO UTC string
rewardLines: RewardPreviewLine[]
claim.claimable: boolean
claim.rewardClaimId: string?
claim.claimedAt: ISO UTC string?
claim.errorCode: string?
```

Reward claim response DTO:

```text
runId: string
rewardPreviewId: string
rewardClaimId: string
status: CLAIMED | DUPLICATE_REPLAY | REJECTED
serverClaimedAt: ISO UTC string
balanceVersion: string
rewardLines: RewardClaimLine[]
errorCode: string?
message: string?
```

Unity should treat preview quantities as display data from the server. Unity
should treat claim success as the point where rewards become permanent.

## Migration Impact

Existing migration compatibility:

- `accounts(id)` already exists and is the correct account FK.
- `runs(id)` already exists and has frozen `balance_config_version`.
- `run_final_results(run_id)` already stores the final submitted result.
- `reward_transactions` already represents committed rewards and has
  `UNIQUE(run_id)`.
- `economy_transactions` and `currency_ledger` already provide the permanent
  economy ledger.
- `idempotency_records` already supports successful operation replay.

Migration `0012_run_reward_previews.sql` adds:

- `run_reward_previews` table.
- Indexes for account/status/expiration lookup and run/status lookup.

No change is needed to Unity result submit response beyond the existing
`rewardPreviewId` contract.

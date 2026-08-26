# Run Result Submit Contract Draft

This draft is the server-side contract for Unity run result submission. It is not an implementation plan for reward calculation details.

Reward preview and claim/commit follow-up is defined in `docs/api/run-reward-preview-claim.md`.

## Endpoint

Use the existing run resource shape from the server Source of Truth:

```text
POST /v1/runs/:runId/finish
```

`/v1/runs/result-submit` remains a Unity-side temporary candidate only. The server should prefer `/v1/runs/:runId/finish` because migration 0005 already models `run_final_results.run_id` as the final result primary key and architecture docs already list `POST /v1/runs/:id/finish`.

## Idempotency

Use a header, not the body:

```text
Idempotency-Key: <client-generated-key>
```

Rules:

- Required for every request.
- Stable for retrying the same completed run result.
- Empty or missing key returns `400` with `errorCode: "contract_violation"`.
- Successful accepted submissions write `idempotency_records` with operation `run_finish`.
- Error responses are not stored as permanent idempotency results.
- Duplicate idempotency replay returns `200` with `accepted: false` and `resultStatus: "DUPLICATE_REPLAY"`.

## Request Body

Stable fields:

```json
{
  "runId": "run_...",
  "currentStage": "Complete",
  "currentWaveNumber": 0,
  "completedWaveCount": 15,
  "outcome": "VICTORY",
  "runtimeStateJson": "{}",
  "tacticSelections": [],
  "clientCompletedAt": "2026-08-26T00:00:00.000Z"
}
```

Rules:

- Path `:runId` and body `runId` must match.
- `currentStage` must be `Complete`.
- `completedWaveCount` may be `0..15`.
- `currentWaveNumber=0` with `completedWaveCount=15` is allowed as Unity's terminal completed-run representation.
- `outcome` is `VICTORY`, `FAILURE`, or `ABANDONED`.
- Request must not include reward amounts, currency grants, or final reward decisions.
- `runtimeStateJson` is a stringified JSON payload produced by Unity. The server parses it as validation input only. It is not a reward source of truth.

## Response Body

Stable fields:

```json
{
  "runId": "run_...",
  "accepted": true,
  "resultStatus": "ACCEPTED",
  "rewardClaimId": null,
  "rewardPreviewId": "reward_preview_...",
  "serverReceivedAt": "2026-08-26T00:00:00.000Z",
  "balanceVersion": "1",
  "message": null,
  "errorCode": null
}
```

Use ISO UTC `Z` timestamps. Server may emit JavaScript `Date.toISOString()` millisecond precision.

`balanceVersion` is a string in the API contract to match Unity DTO handling and to avoid safe-integer ambiguity if version identifiers become non-numeric later. The server still stores current D1 `balance_config_version` values as INTEGER internally.

## Result Status

```text
ACCEPTED
DUPLICATE_REPLAY
REJECTED
```

Mapping:

- `ACCEPTED`: Unity `Accepted`, then `ShowRewardReview`.
- `DUPLICATE_REPLAY`: Unity `DuplicateReplay`, then `RecognizedDuplicate`.
- `REJECTED`: Unity `Rejected`, then recoverable/fatal UX based on `errorCode`.
- Unity `NetworkError` is transport-level only and has no server response DTO.

## HTTP Status

- `200`: Valid server response DTO, including `ACCEPTED`, `DUPLICATE_REPLAY`, and domain-level `REJECTED`.
- `400`: Contract violation response DTO where the request shape is invalid.
- `401`/`403`: Auth failures before run submission contract processing.
- `409`: Optional for run state conflicts, but prefer `200` + `REJECTED` + `run_state_conflict` for Unity policy consistency.
- `503`: Optional for validator unavailability, but prefer `200` + `REJECTED` + `validator_temporarily_unavailable` when the server handled the request.

## Error Codes

Recoverable:

```text
run_state_conflict
validator_temporarily_unavailable
stale_client_state
```

Fatal:

```text
contract_violation
run_not_found
run_account_mismatch
run_result_already_finalized
invalid_balance_version
integrity_rejected
reward_plan_unavailable
```

`contract_violation` covers path/body runId mismatch, missing idempotency key, invalid enum values, missing required fields, reward amount fields in request, or non-complete runtime stage.

## Reward Reference Policy

Prefer `rewardPreviewId` for the first accepted result submit.

Rationale:

- Unity enters reward review after accepted submit.
- Reward amounts/final grants remain server-side.
- The player can review by fetching the reward preview.
- A later claim endpoint can convert preview to final `rewardClaimId`.

`rewardClaimId` should be reserved for a later claim/commit operation. Do not return final reward amounts in result submit.

Duplicate replay response:

- `accepted: false`
- `resultStatus: "DUPLICATE_REPLAY"`
- `rewardPreviewId: null`
- `rewardClaimId: null`
- `errorCode: "duplicate_replay"`

## Server Source of Truth

The server owns:

- Run status transition.
- Final run validation.
- Reward calculation.
- Reward preview/claim creation.
- Economy mutation and ledger writes.
- Idempotency records.

The Unity result body is only validation input. It never finalizes rewards directly.

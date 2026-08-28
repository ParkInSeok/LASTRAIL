# Ops Error Logs Source of Truth

## Purpose

This document defines the source-of-truth model for LAST RAIL operational error logs.

Ops error logs are used by Admin dashboards, customer support summaries, and incident review. They are read-only evidence for launch support, not a repair mechanism.

## Ownership

- Server owns creation, storage, and correlation of ops error logs.
- Admin owns read-only visibility, filtering, labels, and support context display.
- Unity may provide client diagnostic hints in future, but client diagnostics are never the source of truth for server-side error facts.
- Customer support uses ops error logs as supporting evidence only.

## Launch Scope

Launch ops error logs should capture server-observed failures or restricted actions for:

- run finish
- run reward preview/claim
- offline reward preview/claim
- story progress update/read
- gacha pull/claim
- auth/account status
- integrity restriction
- support read model gaps

Launch ops error logs should not include:

- raw request bodies
- access tokens
- refresh tokens
- raw auth headers
- RNG material
- seed preimages
- per-candidate probability
- full weight tables
- raw multiplier bps
- localized dialogue full text
- unrestricted stack traces in Admin responses

## Recommended Table

Future migration candidate: `ops_error_logs`.

Columns:

- `id TEXT PRIMARY KEY`
- `created_at INTEGER NOT NULL`
- `severity TEXT NOT NULL`
- `error_code TEXT NOT NULL`
- `message TEXT`
- `status TEXT NOT NULL`
- `source TEXT NOT NULL`
- `account_id TEXT REFERENCES accounts(id) ON DELETE SET NULL`
- `run_id TEXT REFERENCES runs(id) ON DELETE SET NULL`
- `reward_preview_id TEXT REFERENCES run_reward_previews(id) ON DELETE SET NULL`
- `reward_transaction_id TEXT REFERENCES reward_transactions(id) ON DELETE SET NULL`
- `economy_transaction_id TEXT REFERENCES economy_transactions(id) ON DELETE SET NULL`
- `offline_reward_claim_id TEXT`
- `gacha_batch_id TEXT`
- `story_progress_id TEXT`
- `operation_id TEXT`
- `risk INTEGER`
- `idempotency_operation TEXT`
- `idempotency_key_hash TEXT`
- `integrity_event_id TEXT`
- `support_ticket_id TEXT`
- `correlation_json TEXT NOT NULL`
- `sanitized_context_json TEXT NOT NULL`
- `resolved_at INTEGER`
- `resolved_by TEXT`
- `resolution_note TEXT`

Indexes:

- `idx_ops_error_logs_created_at`
- `idx_ops_error_logs_severity_created`
- `idx_ops_error_logs_error_code_created`
- `idx_ops_error_logs_account_created`
- `idx_ops_error_logs_run_created`
- `idx_ops_error_logs_reward_transaction_created`
- `idx_ops_error_logs_offline_claim_created`
- `idx_ops_error_logs_gacha_batch_created`
- `idx_ops_error_logs_status_created`

## Severity

| Severity | Korean Label | Meaning |
| --- | --- | --- |
| `info` | 안내 | Expected or explainable operational state. |
| `warning` | 주의 | Non-fatal mismatch, retry, missing optional data, or support-relevant anomaly. |
| `error` | 오류 | Failed operation or missing required server-side state. |
| `critical` | 치명 | Economy, integrity, account identity, or broad incident risk. |

## Status

| Status | Korean Label | Meaning |
| --- | --- | --- |
| `open` | 열림 | Needs visibility or investigation. |
| `acknowledged` | 확인됨 | Operator has seen it; not resolved yet. |
| `resolved` | 해결됨 | Root cause or expected behavior confirmed. |
| `ignored` | 무시됨 | Known harmless signal. |

Launch Admin should treat status as read-only unless an explicit escalation workflow is later added.

## Source

| Source | Korean Label |
| --- | --- |
| `game_api` | 게임 API |
| `admin_api` | 어드민 API |
| `worker_cron` | 예약 작업 |
| `migration_verify` | 마이그레이션 검증 |
| `support_read_model` | 고객지원 조회 모델 |
| `integrity_pipeline` | 무결성 파이프라인 |

## Error Code Taxonomy

### Auth/Account

- `auth_required`
- `access_token_invalid`
- `auth_session_invalid`
- `account_not_active`
- `account_not_found`
- `account_identity_conflict`

### Contract/Input

- `contract_violation`
- `idempotency_key_required`
- `duplicate_replay`

### Run/Reward

- `reward_plan_mismatch`
- `reward_multiplier_missing`
- `reward_plan_unavailable`
- `reward_integrity_restricted`
- `reward_claim_missing_ledger`
- `reward_preview_without_claim`

### Offline Reward

- `offline_story_progress_missing`
- `offline_reward_policy_missing`
- `offline_reward_policy_invalid`
- `offline_reward_not_claimable`
- `offline_reward_state_missing`
- `offline_reward_state_conflict`
- `offline_reward_clock_tamper`
- `offline_reward_claim_conflict`
- `offline_reward_plan_mismatch`

### Story/Narrative

- `story_progress_policy_missing`
- `story_progress_state_conflict`
- `story_unlock_missing`
- `narrative_content_missing`
- `narrative_version_mismatch`

### Economy

- `economy_mutation_conflict`
- `currency_ledger_missing`
- `reward_transaction_missing`

### Gacha

- `gacha_temporarily_unavailable`
- `banner_inactive`
- `invalid_pull_count`
- `gacha_guarantee_conflict`
- `gacha_duplicate_replay_mismatch`

### Integrity

- `integrity_restricted_reward`
- `integrity_soft_anomaly`
- `integrity_hard_restriction`
- `suspicious_replay`

## Correlation Policy

Ops logs should store stable reference ids where available:

- `accountId`
- `runId`
- `operationId`
- `risk`
- `rewardPreviewId`
- `rewardTransactionId`
- `economyTransactionId`
- `offlineRewardClaimId`
- `gachaBatchId`
- `storyProgressId`
- `integrityEventId`
- `supportTicketId`
- `idempotencyOperation`
- `idempotencyKeyHash`

Raw idempotency keys should not be exposed in Admin. Store a server-side hash or stable reference only.

## Sanitized Context

Allowed sanitized context:

- public status values
- stable enum codes
- counts
- timestamps
- boolean presence flags
- reference ids
- display-safe labels
- support-safe summaries

Forbidden context:

- raw request bodies
- token strings
- session secrets
- raw auth headers
- raw RNG material
- seed preimages
- per-candidate probabilities
- full weight tables
- raw multiplier bps
- internal reward formulas beyond public policy labels
- localized dialogue full text

## Creation Rules

Create an ops error log when:

- A server-side required state is missing.
- A ledger/economy/reward transaction mismatch is detected.
- A duplicate replay does not match stored response references.
- Integrity policy restricts reward, story progress, or claim behavior.
- Story progress or unlock expected from full victory is missing.
- Offline reward state revision or timestamp conflicts with claim records.
- Gacha guarantee state conflicts with pull result or pity counters.
- Admin support summary cannot compose a required read-only section.

Do not create noisy logs for:

- normal `offline_reward_not_claimable` caused only by minimum time
- expected duplicate replay with matching stored response
- normal no-row fallback for legacy offline reward state
- missing optional narrative localization before content lock

Those can be represented as support context or low-level metrics instead.

## Admin Read Model

Future Admin endpoint candidates:

- `GET /v1/admin/ops/error-logs`
- `GET /v1/admin/ops/error-logs/:id`

List filters:

- `severity`
- `status`
- `errorCode`
- `source`
- `accountId`
- `runId`
- `operationId`
- `offlineRewardClaimId`
- `gachaBatchId`
- `storyProgressId`
- `supportTicketId`
- `createdFrom`
- `createdTo`
- `limit`
- `cursor`

Response should include:

- `id`
- `createdAt`
- `severity`
- `status`
- `source`
- `errorCode`
- `message`
- `correlation`
- `sanitizedContext`
- `resolvedAt`
- `resolvedBy`
- `resolutionNote`

Launch should keep resolution mutation disabled unless escalation workflow is implemented.

## Support Summary Integration

`GET /v1/admin/support/accounts/:accountId` currently may return `opsErrors: []` as a placeholder.

Future support summary should include recent open ops error logs for the account:

- latest 5 open or acknowledged errors
- critical errors first
- no raw request or secret data
- correlation ids only
- safe labels for Admin

Support should use these logs to decide escalation category, not to issue repair directly.

## Retention

Recommended launch retention:

- `critical`: 180 days minimum
- `error`: 90 days minimum
- `warning`: 30 days minimum
- `info`: 14 days minimum or metrics-only where possible

Retention can be adjusted once production volume is known.

## Alerting Candidates

Create alert candidates for:

- any `critical` error
- repeated `reward_claim_missing_ledger`
- repeated `offline_reward_state_conflict`
- repeated `gacha_guarantee_conflict`
- surge in `contract_violation`
- surge in `integrity_hard_restriction`
- support summary composition failures

Alerting is not required for the first read-only API slice.

## Rollout Order

1. Finalize ops error log schema and taxonomy.
2. Add Server migration and write helper.
3. Add read-only Admin API list/detail endpoints.
4. Connect Admin `/ops/error-logs` to actual read-only data.
5. Add support summary integration for recent account errors.
6. Add alerting thresholds.
7. Add resolution workflow only after escalation permissions are ready.

## Open Decisions

- Whether ops error logs should be D1-only or also streamed to a remote log system.
- Whether `message` should be operator-authored, server-authored, or code-derived only.
- Whether status changes need a separate audit table.
- Whether support ticket ids are optional or required for resolution.
- Whether critical integrity logs need a separate restricted permission.
- Exact retention by environment.

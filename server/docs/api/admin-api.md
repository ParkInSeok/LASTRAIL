# Admin API

Base domains:

- DEV: `https://dev-admin.lastrail.app`
- PRD: `https://admin.lastrail.app`

The server currently uses `dev` and `prd` environment axes only. PRD resources and deploys remain approval-gated.

Admin domains must be protected by Cloudflare Access. LAST RAIL authorization maps Access JWT `sub` to `admin_principals.access_subject` and then applies OWNER / OPERATOR / VIEWER permissions.

## Auth Skeleton

`GET /v1/admin/me` is the first protected Admin route.

Current behavior:

- `LOCAL` uses `ADMIN_AUTH_MODE=LOCAL_HEADER` for local tests and development only.
- DEV/PRD use `ADMIN_AUTH_MODE=ACCESS_JWT_REQUIRED`.
- Non-local modes validate `Cf-Access-Jwt-Assertion` with the configured Cloudflare Access issuer and AUD tag.
- If issuer or AUD is not configured, protected routes return `ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED`.
- Local header mode reads `Cf-Access-Subject`, maps it to active `admin_principals.access_subject`, and returns the principal role.

Do not seed real admin principals or expose operational Admin routes until Cloudflare Access is applied and DEV `ADMIN_ACCESS_ISSUER` / `ADMIN_ACCESS_AUD` are configured.

## Read-only Operations

`GET /v1/admin/players` returns account search rows for the Admin Console.

Query:

- `q`: optional account id or display name search text

Response:

- `items[].accountId`
- `items[].displayName`
- `items[].status`
- `items[].syncLevel`
- `items[].lastSeenAt`

`GET /v1/admin/players/:accountId` returns account detail with the Training
Core / Sync progression inspector. `syncLevel` is the minimum `trained_level`
across exactly 5 Training Core slots. Non-core operators use `levelSource:
"SYNC"` and `effectiveLevel: syncLevel`. Core operators use `levelSource:
"TRAINED"` and their direct `trainedLevel`.

The same detail response also exposes launch-safe read-only account support
evidence:

- `auth.identityProviders`, `providerNames`, `linkedProviders`,
  `primaryProvider`, and `hasGoogleLink`;
- `auth.activeSessionCount`, `latestSessionAt`, `lastLoginAt`, and
  `lastLoginAgeSeconds`;
- `auth.concurrentUsers = null` and `concurrentUsersPrecision =
  "UNAVAILABLE"` until a server-owned activity/heartbeat source exists;
- `opsErrors[]` with the latest account-scoped launch-critical sanitized ops
  error rows.

Request bodies are rejected with `contract_violation`. The response does not
return tokens, session secrets, refresh token hashes, raw auth headers, raw
request bodies, or account shortcut data.

`GET /v1/admin/runs` returns run search rows.

Query:

- `accountId`: optional account filter

Response includes run balance version, result, risk tier, and validator version when available.

`GET /v1/admin/runs/:runId` returns run detail with frozen
`run_snapshots.snapshot_json`, run balance version, current active balance
version, client build, content version, validator version, and validator
runtime revision.

The detail response includes launch-safe support evidence for reward and Act 1
story raid investigation:

- `operationId`, `riskTier`, `outcome`, and `clearedWaves`;
- `rewardPreviewId`, `rewardClaimId`, `appliedMultiplierLabel`,
  `calculationMetadataStatus`, and `firstClearConsumed`;
- `encounterType`, `raidId`, `bossId`, `bossPhaseRefs`, `bossDefeated`,
  `completedPhaseCount`, `encounterDurationSeconds`, and
  `allowedOperatorSlots`;
- `startAugmentPackageRef`, `startTacticPackageRef`, `selectedAugmentIds`, and
  `selectedTacticIds`;
- `staminaRefund.status/resource/reason/policyId/serverOwnedMutation`,
  `fuelLedgerReason`, and safe `fuelLedgerRefs`;
- `integrityDisposition` summary counts.

This is read-only evidence only. It does not expose raw animation timelines,
VFX/SFX/camera data, raw RNG material, probability, full weight tables,
unrestricted payload dumps, reward correction controls, or replay/reprocess
actions.

## Dashboard Launch Summary

Current local read-only endpoint:

```text
GET /v1/admin/dashboard/launch-summary
```

Permission:

- `SUPPORT_READ`
- Current implementation: `ROLE_PERMISSION`; OWNER / OPERATOR / VIEWER can
  read the launch summary.

The endpoint composes a thin launch operations snapshot for Admin dashboard
cards:

- `activeSessions`
- `lastLoginAtLatest`
- `lastLoginAgeBuckets.latestAgeSeconds`
- `supportReviewNeededCount`
- `accountRecoveryCandidateCount`
- `integrityHighRiskCount`
- `rewardClaimFailureCount`
- `offlineRewardIssueCount`
- `gachaSupportIssueCount`
- `duplicateReplayCount`
- `criticalOpenOpsErrorCount`
- `unacknowledgedCoreFailureCount`
- `serverCalculatedAt`

Exact concurrent users are intentionally not claimed. The response returns
`concurrentUsers = null`, `recentActiveAccounts5m = null`,
`recentActiveAccounts30m = null`, and source/precision fields that mark those
values unavailable until a server-owned activity source exists.

Request bodies are rejected with `contract_violation`.

## Story Progress

Current local read-only endpoints:

```text
GET /v1/admin/story-progress
GET /v1/admin/story-progress/:accountId
```

Permission target:

- Stable permission id: `STORY_PROGRESS_READ`.
- Current implementation: `ROLE_PERMISSION`; `admin_principals.role` maps to
  permission grants because there is no separate admin permission grant table.
- OWNER / OPERATOR / VIEWER all receive `STORY_PROGRESS_READ`.
- Mutation endpoints are not enabled.

VIEWER access remains allowed because these endpoints are read-only support
views, do not expose economy repair or mutation controls, and match the current
Admin API pattern where player/run read-only views are available to active
VIEWER principals.

List filters:

- `accountId`
- `actId`
- `highestStoryProgressId`
- `highestClearedOperationId`
- `narrativeVersion`
- `contentStatus`
- `localizationStatus`
- `fixtureStatus`
- `missingContentStatus`
- `updatedFrom`
- `updatedTo`
- `limit`
- `cursor`

The list endpoint returns compact `account_story_progress` summaries. The
detail endpoint returns a single account read model using `account_story_progress`
and operation unlocks from `account_unlocks`. Accounts without a progress row
return a stable Act 1 `none` default with `opn_01_last_departure` unlocked.

These endpoints do not return full localized dialogue text, tokens, secrets, raw
request bodies, raw RNG material, reward calculation internals, or account
shortcut data. Request bodies are rejected with `contract_violation`.

## Offline Rewards

Current local read-only endpoints:

```text
GET /v1/admin/offline-rewards
GET /v1/admin/offline-rewards/:accountId
```

Permission target:

- Stable permission id: `OFFLINE_REWARD_READ`.
- Current implementation: `ROLE_PERMISSION`; `admin_principals.role` maps to
  permission grants because there is no separate admin permission grant table.
- OWNER / OPERATOR / VIEWER all receive `OFFLINE_REWARD_READ`.
- Offline reward repair, manual grant, claim mutation, and economy correction
  endpoints are not enabled.

VIEWER access remains allowed because these endpoints expose read-only support
state, not mutation controls. Operators can use it for player support and
offline reward troubleshooting without changing balances.

List filters:

- `accountId`
- `accountStatus`
- `storyProgressId`
- `lastStoryProgressId`
- `narrativeVersion`
- `updatedFrom`
- `updatedTo`
- `limit`
- `cursor`

The list endpoint returns compact rows based on `accounts`,
`player_profiles`, `account_story_progress`, `account_offline_reward_state`,
and the latest offline reward `reward_transactions` row where `run_id IS
NULL`. The detail endpoint returns a single account read model.

Accounts without an `account_offline_reward_state` row use the same read-only
fallback context as Game API preview: `accounts.created_at` is displayed as the
baseline source. The Admin endpoint does not create the missing row.

These endpoints do not return tokens, secrets, raw request bodies, raw RNG
material, raw multiplier bps, reward calculation internals, or account shortcut
data. Request bodies are rejected with `contract_violation`.

## Session Metrics

Login/session/dashboard metrics are defined in
`docs/api/admin-session-metrics.md`. The current local implementation exposes
account-level fields through `GET /v1/admin/support/accounts/:accountId`;
dashboard-level launch aggregates through
`GET /v1/admin/dashboard/launch-summary`; and player detail auth/session
evidence through `GET /v1/admin/players/:accountId`.

Candidate metrics include:

- `activeSessions`
- `concurrentUsers`
- `recentActiveAccounts5m`
- `recentActiveAccounts30m`
- `lastLoginAt`
- `lastLoginAgeSeconds`
- `lastLoginAgeBuckets`

Server auth/session/account records are the Source of Truth. Client-supplied
session/login data is not trusted. Exact concurrency should not be claimed from
refresh session TTL alone; use `activeSessions` for unexpired ACTIVE sessions
and reserve `concurrentUsers` for a future server-owned activity/heartbeat
source.

## Customer Support

Customer support account summary is defined in `docs/api/support.md`.

Read-only endpoint:

```text
GET /v1/admin/support/accounts/:accountId
```

Permission:

- `SUPPORT_READ`
- Current implementation: `ROLE_PERMISSION`; `admin_principals.role` maps to
  the permission.
- OWNER / OPERATOR / VIEWER can read launch support summaries.

The support summary is a read-only account-centered investigation view that
composes existing read models such as account/auth status, recent runs, run
reward previews/claims, offline reward state/claims, story progress/unlocks,
gacha batches, launch login bonus claim evidence, idempotency replay records,
integrity status, and future ops error logs.

The route does not open account mutation, reward repair, manual grant, offline
timestamp reset, story/unlock mutation, gacha repair, economy correction,
support escalation state mutation, or narrative/localization publishing
controls. Request bodies are rejected with `contract_violation`. Missing
accounts return `account_not_found`.

The response intentionally omits full tokens, session secrets, refresh token
hashes, raw auth headers, raw request bodies, raw RNG material, RNG seed
preimages, per-candidate probabilities, full gacha weight lists, localized
dialogue text, raw reward multiplier bps, reward calculation internals, and
account shortcut data.

System/runtime error log contract is defined in `docs/api/ops-error-logs.md`.
Read-only endpoints are:

```text
GET /v1/admin/ops/error-logs
GET /v1/admin/ops/error-logs/summary
GET /v1/admin/ops/error-logs/:errorLogId
```

Permission:

- `OPS_ERROR_LOG_READ`
- Current implementation: `ROLE_PERMISSION`; OWNER / OPERATOR allowed, VIEWER
  denied by default.

Runtime error observability remains separate from `admin_audit_logs`. The local
slice creates only the D1 searchable summary table and read-only Admin API
routes. It does not create Queues, R2 buckets, Workers Logs exports, repair
mutations, resolution workflow mutations, Cloudflare resources, DEV deploys, or
PRD/STAGING resources.

`GET /v1/admin/ops/error-logs/summary` returns aggregate counts for dashboard
and ops page cards: `recentErrorCount`, `warningCount`, `criticalCount`,
`retryableCount`, `maskedCount`, `openCount`, `acknowledgedCount`,
`latestCriticalAt`, and `serverCalculatedAt`. Detail/raw payload availability
is represented safely with `detailAvailable = false` unless a future sanitized
detail store is explicitly approved.

## Gacha Support Read Model

Current local read-only endpoints:

```text
GET /v1/admin/gacha/banners
GET /v1/admin/gacha/accounts/:accountId/pity
GET /v1/admin/gacha/pull-batches
GET /v1/admin/gacha/pull-batches/:batchId
```

Permission:

- `SUPPORT_READ`
- Current implementation: `ROLE_PERMISSION`; OWNER / OPERATOR / VIEWER can
  read launch gacha support views.

These endpoints expose launch support evidence for active banners, account
pity state, pull batches, idempotency replay hints, cost references, result
refs, guarantee types, and duplicate conversion summaries. They intentionally
omit raw RNG material, RNG seed preimages, per-candidate probability, full
weight lists, token/secret/account shortcut data, and repair/reroll controls.

## Balance Config Drafts

`POST /v1/admin/balance-config/drafts` creates a draft row in `balance_config_drafts`.

Required body:

- `schemaVersion`: positive integer
- `requiredContentVersion`: non-negative integer
- `config`: JSON object

Optional body:

- `baseVersion`: positive integer or `null`

OWNER and OPERATOR can create drafts. VIEWER is denied. The server canonicalizes `config`, computes a `sha256:<hex>` checksum, stores revision `1`, and returns draft metadata. This endpoint does not publish, activate, or mutate existing published versions.

## Balance Config Publish

`POST /v1/admin/balance-config/drafts/:draftId/publish/confirmations` creates a short-lived confirmation token for a publish payload.

Required body:

- `expectedRevision`: positive integer
- `expectedChecksum`: checksum returned by the draft endpoint

OWNER only. OPERATOR and VIEWER are denied.

The response returns the raw `token` once. The server stores only `token_hash`; never persist or log the raw token.

`POST /v1/admin/balance-config/drafts/:draftId/publish` publishes a draft and immediately activates the new published version.

Required body:

- `expectedRevision`: positive integer
- `expectedChecksum`: checksum returned by the draft endpoint
- `confirmationToken`: raw confirmation token returned by the confirmation endpoint

OWNER only. OPERATOR and VIEWER are denied. Execute-time permission is checked again before token consumption and domain writes.

Confirmation creation and publish both validate:

- The draft exists and is still `DRAFT`.
- `expectedRevision` equals the draft revision.
- `expectedChecksum` equals the draft checksum.
- The draft `baseVersion` equals the current active balance version.

On success, the server consumes the matching confirmation, inserts an immutable row in `balance_config_versions`, records a publish claim, marks the draft `PUBLISHED`, inserts activation history with `PUBLISH`, moves `balance_config_active` to the new version, and appends `admin_audit_logs` in one D1 batch.

Failed publish attempts after admin authentication append `admin_audit_logs` with `result = 'FAILURE'` and a structured error code. The raw confirmation token is never written to audit logs.

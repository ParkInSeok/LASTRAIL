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

### Version policy browser preconditions (local, not deployed)

`GET /v1/admin/versions` returns `revision: string | null` with the version
policy. A string is the opaque quoted object ETag and must be echoed unchanged;
null means the object does not exist. Missing/unavailable revision must not be
converted into null by the UI. Read failures return no usable revision.

`POST /v1/admin/versions/export` requires `expectedRevision: string | null` in
the existing JSON body alongside version/versionCode/minimumVersionCode. An
explicit null permits creation only while the object is still absent. Legacy
content omission still preserves the current approved content release.

- Missing expectedRevision:428 `version_precondition_required`.
- Invalid revision format:400 `invalid_version_revision`.
- Browser revision differs from current, or final conditional write fails:409
  `version_current_pointer_conflict`.
- Storage read/write failures retain502 `version_storage_read_failed` /
  `version_storage_write_failed` on the respective routes.

The server compares the browser revision to the current snapshot, then merges
and writes with that same snapshot's atomic storage precondition. The comparison
does not replace CAS. Success response remains unchanged; obtain a fresh revision
through GET before another save. Do not automatically resubmit after409. Reload
the latest policy and require user review; block saving while reload is pending
or failed. An old UI without expectedRevision receives428 after server rollout.
This protects an old browser form in addition to the server's read/merge race.
Actual UI/API rollout is a separate approval; no deployed behavior is claimed.

`GET /v1/admin/players` returns account search rows for the Admin Console.

Query:

- `q`: optional account id or display name search text

Response:

- `items[].accountId`
- `items[].displayName`
- `items[].status`
- `items[].syncLevel`
- `items[].lastSeenAt`

`displayName` is the player-facing nickname from `player_profiles`. The Admin
API consumes it as read-only support/display evidence only. Nickname creation,
duplicate checks, and cooldown enforcement belong to the Game API profile
contract; Admin does not expose display name mutation or reservation controls
for launch.

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

Response includes run balance version, result, risk tier, validator version
when available, and compact version evidence:

- `runStartVersionSnapshot` / `runStartVersionSnapshotStatus`;
- `runFinishVersionSnapshot` / `runFinishVersionSnapshotStatus`.

`runFinishVersionSnapshotStatus` is `NOT_FINISHED` for active runs and
`UNRECORDED_LEGACY` for completed legacy rows without a stored finish snapshot.

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
- `runVersionSnapshot` as a compatibility alias for the start snapshot;
- `runStartVersionSnapshot` and `runFinishVersionSnapshot` with
  server-accepted client app version/version code, platform/channel, admission
  status, runtime deployment id, content release/hash, and capture timestamp
  when available;
- `runStartVersionSnapshotStatus` and `runFinishVersionSnapshotStatus`;
- `staminaRefund.status/resource/reason/policyId/serverOwnedMutation`,
  `fuelLedgerReason`, and safe `fuelLedgerRefs`;
- `integrityDisposition` summary counts.

This is read-only evidence only. It does not expose raw animation timelines,
VFX/SFX/camera data, raw RNG material, probability, full weight tables,
raw Addressables catalog URLs, unrestricted payload dumps, reward correction
controls, or replay/reprocess actions.

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
- `dailySourceMetrics`
- `supportReviewNeededCount`
- `accountRecoveryCandidateCount`
- `integrityHighRiskCount`
- `rewardClaimFailureCount`
- `offlineRewardIssueCount`
- `gachaSupportIssueCount`
- `duplicateReplayCount`
- `criticalOpenOpsErrorCount`
- `unacknowledgedCoreFailureCount`
- `accountsWithStoryProgressCount`
- `highestProgressDistribution[]`
- `contentMissingCount`
- `localizationPendingCount`
- `fixturePendingCount`
- `versionMismatchCount`
- `latestNarrativeVersion`
- `serverCalculatedAt`

Exact concurrent users are intentionally not claimed. The response returns
`concurrentUsers = null`, `recentActiveAccounts5m = null`,
`recentActiveAccounts30m = null`, and source/precision fields that mark those
values unavailable until a server-owned activity source exists.

`dailySourceMetrics` uses KST calendar-day inclusive windows. The current
server-owned source can count distinct accounts that created auth sessions in
the window, so Admin should treat `sessionCreatedAccounts` as the launch metric.
`dauSource = UNAVAILABLE_NO_ACTIVITY_HEARTBEAT_SOURCE` marks exact activity DAU
as unavailable until a heartbeat/activity source exists. The response includes
both 7/30-day aggregates and `points[]` with `date`,
`sessionCreatedAccounts`, `signups`, `adRewardsVerified`, and
`adRewardsGranted`. Signups are counted from `accounts.created_at`; ad reward
counts come from `ad_reward_transactions.status`. Pass/purchase and ad revenue
metrics remain `null`, with source availability set to false, until
server-owned source tables exist.

Do not substitute unavailable launch metrics with lookalike counters:

- `todayActiveUsers` / exact DAU requires a server-owned activity event source
  with KST day-boundary distinct account semantics. Distinct accounts that
  created auth sessions are exposed only as `sessionCreatedAccounts`.
- Total registered accounts requires an explicit denominator policy for deleted
  or disabled accounts before Admin should label it as a production KPI.
- Ad views and ad viewers require a verified ad view event source. Reward grant
  rows are not the same as ad view events.
- Ad revenue requires a settlement/currency source.
- Pass purchase rate requires verified purchase rows and an agreed DAU or
  active-account denominator.

Until those sources exist, responses should keep unavailable values as `null`
with source-unavailable metadata. A source-backed successful zero remains `0`.

The story/narrative aggregate is read-only launch visibility. It counts stored
`account_story_progress` rows and the static Act 1 narrative coverage candidate
set. Empty semantics are stable: counts are `0`, distributions are `[]`, and
missing latest versions are `null`.

Request bodies are rejected with `contract_violation`.

## Server Deployments

Current local read-only endpoints:

```text
GET /v1/admin/server-deployments/current?service=game-api
GET /v1/admin/server-deployments/history/:serverVersion?service=game-api
```

`service` may be `game-api`, `admin-api`, or `async-worker`. The Admin worker
environment is isolated: DEV Admin can read only `dev` deployment evidence and
PRD Admin can read only `prd` evidence. Supplying a different `environment`
query returns `deployment_target_mismatch`.

Permission:

- `SERVER_DEPLOYMENT_READ`
- Current implementation: `ROLE_PERMISSION`; OWNER / OPERATOR / VIEWER can read
  server deployment evidence.

The endpoints read sanitized R2 documents from the version bucket:

- current pointer key:
  `server-deployments/<env>/<service>/current.json`
- immutable history key:
  `server-deployments/<env>/<service>/<serverVersion>.json`

Current pointer response fields:

- `environment`
- `service`
- `serverVersion`
- `historyKey`
- `releaseId`
- `cloudflareDeploymentId`
- `updatedAt`

History response fields:

- `releaseId`
- `environment`
- `service`
- `serverVersion`
- `commitSha`
- `releaseBranch`
- `deploymentTag`
- `cloudflareDeploymentId`
- `previousDeploymentId`
- `database.compatibility`
- `database.minimumVersionCode` when required for compatibility-breaking
  changes
- `database.migrationRange`
- `api.compatibility` when recorded:
  `NO_API_CHANGE`, `ROLLBACK_COMPATIBLE`, or `MIN_VERSION_GATE_REQUIRED`
- `api.rollbackVerified` when rollback-compatible API evidence is recorded
- `api.minimumVersionCode` when required for compatibility-breaking API
  changes
- `contentRelease.releaseId` / `contentRelease.contentVersion` when linked
- `verification.localRegression`
- `verification.dryRun`
- `verification.remoteSmoke`
- `pointerStatus`
- `createdAt`

Missing documents return `server_deployment_current_not_found` or
`server_deployment_history_not_found`. Invalid stored evidence returns
`server_deployment_history_invalid`. Request bodies are rejected with
`contract_violation`.

These endpoints do not deploy Workers, move R2 pointers, create branches/tags,
write secrets, roll back D1, or expose raw R2 credentials, tokens, raw auth
headers, raw request bodies, Google tokens, RNG material, raw probability/weight
tables, or rollback controls.

Server version policy for the Admin version-management tab:

- Server version is independent from the Unity app version.
- D1/DB changes are forward-moving. Admin must not expose DB rollback.
- Compatibility-breaking DB updates require a server-enforced
  `minimumVersionCode` gate.
- API-only updates should remain rollback-compatible. Admin may show
  `api.rollbackVerified`, but it must not treat the label as a rollback action.
- Compatibility-breaking API updates require `api.minimumVersionCode` before
  the incompatible path is reachable.

## Story Progress

Current local read-only endpoints:

```text
GET /v1/admin/narrative/coverage?actId=act_01_wasteland_railway
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

`GET /v1/admin/narrative/coverage` returns the Act 1 narrative coverage/readiness
candidate set for Admin run detail and support context. Current launch freeze
candidate version is `act1.2026-08-30.001`; until final Unity/internal testing
fixture export and version stamp are accepted, rows use pending readiness states
instead of `ready`.

Coverage row fields:

- `missionId`
- `displayName`
- `contentStatus`: `draft_exists`, `content_missing`, or `ready`
- `localizationStatus`: `localization_pending`, `ready`, or
  `version_mismatch`
- `fixtureStatus`: `fixture_pending`, `ready`, or `version_mismatch`
- `missingContentStatus`: `version_pending`, `content_missing`,
  `version_mismatch`, or `ready`
- `narrativeVersion`
- `updatedAt`
- `source`

For unknown or non-launch `actId` filters, the endpoint returns an accepted
empty read model with `items: []`, zero counts, and `latestNarrativeVersion:
null`. Request bodies are rejected with `contract_violation`.

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
refs, guarantee types, duplicate conversion summaries, and detail
`causeEvidence` from stored server records. The detail evidence includes
source, account/banner/batch refs, replay-known state, pre/post pity snapshots,
cost ledger refs, result ledger refs, and duplicate conversion item summaries.
It also marks raw probability, full weight, and RNG material availability as
false for launch support views. They intentionally
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

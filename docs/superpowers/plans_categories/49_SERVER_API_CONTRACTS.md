# 49. 서버 API·상태 소유권 상세 계약

2026-09-21 번호별 통합. 이 문서는 흩어진 상세 계약을 주제별로 모은 참조본이다. 원문별 제목·출처·기존 검토 상태를 유지하고, 정책 확정·후보·과거 구현 보고를 섞어 승인하지 않는다.

**적용 순서:** 최신 사용자 결정 → 00~29 담당 정책 및 34~37 충돌 정리 → 이 문서의 충돌하지 않는 상세 계약. 원문의 작성일·완료 문구만으로 현재 구현 또는 승인 완료를 뜻하지 않는다. 원문에 있던 C/E 경로는 과거 기록이며 현재 작업 경로는 프로젝트 AGENTS.md의 D 정책을 따른다.

**공통 대체 경계:** 증강 고정 5회·시작 지급은 폐기, 적격 풀 동일 가중치 3후보·카드별 FREE3+EXTRA1·선택 중 완전 정지가 현행이다. 증강 후보 효과 수치는 출시 승인값이 아니다. 순위 보상은 성장 재화 차등 없이 외형·명예로 구분한다. 인물 역할은 20~29 최신 바이블 우선이며 과거 abilityId 매핑은 자동 변경하지 않는다. 작전물자 30/20·노드3선택/물리2방향·협동 시간·T18 HP0 등 미결정은 34와33의 미결 상태를 유지한다.

원문 내 코드 블록·JSON·표·필드·수치는 생략하지 않았다. 옛 상대 경로를 포함하는 코드 예시는 원출처 기준이며 실행 지시가 아니다. 링크와 제목 앵커만 새 위치에 맞게 변환했다.

## 상세 목차

- [Account state — S01 / S02 read contract](#source-e190f6d9758e8e32)
- [Admin API](#source-572d5a712dea75c9)
- [Admin player activity summary v1](#source-33dcea8f8cbca85a)
- [Admin Session Metrics Contract Draft](#source-191ef8c5ab1f7fa5)
- [Attendance elite-ticket evidence and unapproved next contract](#source-0b8706c7816cdc23)
- [Attendance and pass foundations — local only, 2026-09-07](#source-c9524f3d598611b7)
- [Auth Account Linking Contract Draft](#source-6f64a90df51fe75e)
- [Effect feedback: proposed integration contract](#source-a146c4a6c5edf152)
- [Explicit frontline-first prepared profiles](#source-c1986fabaad26c8b)
- [Disposable local admission and support selection](#source-24f6e160d31e880b)
- [CE03 local producer stage 1](#source-e9b5514971aaad2e)
- [Individual ultimates and manual/AUTO control](#source-a9952f103a593941)
- [CE03 support local v2 — first implementation unit](#source-dc491ec5031c2fcc)
- [Client Admission and Content Release Contract](#source-9362db4fe2c26c1d)
- [Client release registry — DEV / PRD contract (2026-09-08)](#source-b941b10631564c0a)
- [CE02 server field contract and reward-reference review](#source-d1c9e8663b08469b)
- [CE03 local combat entry contract and implementation plan](#source-696bb63df6feb47a)
- [Crystal wallet contracts — local implementation, no release policy](#source-172d7ea1b2d372f8)
- [S11 departure admission](#source-8a83ec9bc4961811)
- [Opt-in real DEV login → download → lobby: server prerequisites](#source-791cfbb0f74d1ee3)
- [S03 owned squad save and activation](#source-71be40c88cd4ae29)
- [Gacha / Pity / Result History DB Design Draft](#source-58f169eb9f58887d)
- [Gacha PRD Rarity Selection Design Draft](#source-478abd9f306bd26e)
- [S05 recruitment reads and sparse-result recovery](#source-a9ca32f7cd7e7623)
- [Game API](#source-507f5957b54c021d)
- [Minimum supported app version](#source-7dd0ce78b70ebd9f)
- [Current monthly attendance discovery](#source-38075a0c0078f636)
- [S08 monthly sequential attendance](#source-2c7eeb3d06656ab1)
- [S10 offline economy binding](#source-c312185bc2e8b97b)
- [Offline Rewards Design Draft](#source-5decd70e036ec90e)
- [Ops Error Logs Contract](#source-59ff0027b0008512)
- [Error log category and tracing](#source-4a9273893fe00b6c)
- [Raid Boss Run Contract](#source-7ec22b0ea5cfd630)
- [Realtime Coop Combat Contract](#source-8ca626676d8145d6)
- [S09/S07 recurring-v1](#source-a3b56fbe44706c7f)
- [Run combat requirements and original-run recovery](#source-7e409c4a47efc1a6)
- [Run Result Submit Contract Draft](#source-11d4e6e3b3c22eef)
- [Run Reward Preview / Claim Contract Draft](#source-d4217c0e18e23cfd)
- [Run Risk / Reward Design Draft](#source-77f48c0fb91c5e38)
- [Run Start Augment Package Contract](#source-ed0862e306d76eb4)
- [Read-only run-start result lookup](#source-75e533c458b7b09f)
- [Server version in error logs](#source-253ca40b108e58e8)
- [S06 shop-v1](#source-5277a087d9d6d375)
- [Story Progress Source of Truth / Read Model Draft](#source-378a824352e36d1b)
- [Customer Support Account Summary Contract Draft](#source-b678a199237246c2)
- [S04 owned train installation](#source-e992dfe4e3dd5d24)
- [CE01 explicit ultimate operator selection](#source-feaac556a37c1cd1)
- [Data Ownership](#source-c2d7bd0327f4d353)
- [Launch economy: configuration versus integration](#source-d0978b2758d3a357)
- [LAST RAIL Server / Backend / Cloudflare 전체 구축 Master 작업지시](#source-6f5e52c4e100a74b)
- [Training Core](#source-c44fa2775c2bfd30)
- [Versioned Balance](#source-de8c86b812b01d1c)
- [Integrity](#source-b6e25ed5e5579577)
- [Run Protocol](#source-8c507d1c49464416)

<a id="source-e190f6d9758e8e32"></a>
## Account state — S01 / S02 read contract

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/account-state.md`  
원문 SHA-256: `e4a50684ac1b65aa9ac5663616211e9c5fc452c466cac8b1ab014787003937f9`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-e190f6d9758e8e32:start -->
<a id="source-e190f6d9758e8e32-account-state--s01--s02-read-contract"></a>
### Account state — S01 / S02 read contract

Local S03 contract, 2026-09-10. Not deployed. This document and its implementation
are committed together. Requires migration0028, which adds active squad selection
and offline receipt references; it grants no inventory. Do not use the moving
owner worktree as the earlier S01-only verification reference.

`GET /v1/account/state` uses the existing bearer-token/session/account verifier and
client admission middleware. No query parameters or body. No idempotency key is
required. All responses from the route use `Cache-Control: no-store`.

The server selects the authenticated account only. One SQLite SELECT contains
all holdings subqueries, so they share a database snapshot. GET does not grant,
consume, regenerate fuel, change presets or initialize missing records.

| Field | Contract |
| --- | --- |
| accepted, accountId | true; authenticated immutable account ID |
| serverTime | UTC ISO-8601 response time; not a client clock or revision |
| mutationSequence | highest committed account mutation sequence, or 0 |
| currencies | CREDITS, MACHINE_PARTS, TACTICAL_DATA, SPECIAL_CORE, OPERATOR_EXP numeric balances; absent rows read as 0 |
| items[] | itemId, quantity, ordered by itemId |
| fuel | null if not initialized; otherwise current, max, lastRegenAt, updatedAt |
| operators[] | operatorId, skillTraining, promotion, revision, acquiredAt; ordered by operatorId |
| trainingCoreSlots[] | slot (0–4), operatorId, trainedLevel, investedOperatorExp, revision; only occupied slots, ordered by slot |
| train | null if absent; trainId, level, revision, updatedAt |
| carriages[] / turrets[] | carriageId / turretId, level, revision; owned records, not equipped positions |
| formation | object; activationSupported=true, activePresetId:string or null, activePresetRevision:number or null, revision:number, operators:array, updatedAt:ISO string or null, presets:array |
| formation.operators[] | slot:number (0–4), operatorId:string; copied active squad ordered by slot; [] before activation |
| formation.presets[] | id, name, definitionJson (opaque existing saved JSON string), revision, updatedAt |
| unlocks[] | unlockId, unlockedAt |

All timestamp fields are UTC ISO-8601 strings. Empty collections are `[]`, not
sample inventory. No numeric combat stats or effective squad levels are invented.
Training cores are progression slots; they are not an active combat formation.
S03 adds a separately stored active squad selection. It does not add train/equipment
installation positions or connect this squad to the existing run-start snapshot.
activationSupported=true means the server implements the operator-only save/activate
contract, not that this account already owns a valid squad or may depart. Per-account
eligibility is checked by POST. See [formation.md](49_SERVER_API_CONTRACTS.md#source-71be40c88cd4ae29).

<a id="source-e190f6d9758e8e32-s01-compatibility-and-exact-s03-nesting"></a>
#### S01 compatibility and exact S03 nesting

S03 preserves all previous top-level S01 fields and formation.presets. Within
formation it changes activationSupported from false to true, allows activePresetId
to become a string, and adds activePresetRevision, revision, operators, updatedAt.
No new field is placed at the response root. A strict S01 parser should remain
pinned to the S01 snapshot until it explicitly adopts this S03 contract.

Never-activated account (formation portion of an otherwise normal state response):

```json
{"formation":{"activePresetId":null,"activePresetRevision":null,"revision":0,"operators":[],"updatedAt":null,"activationSupported":true,"presets":[]}}
```

After saving and activating preset p at revision1 (fixture IDs/time, not grants):

```json
{"formation":{"activePresetId":"p","activePresetRevision":1,"revision":1,"operators":[{"slot":0,"operatorId":"op0"},{"slot":1,"operatorId":"op1"},{"slot":2,"operatorId":"op2"},{"slot":3,"operatorId":"op3"},{"slot":4,"operatorId":"op4"}],"updatedAt":"2026-09-10T05:00:00.000Z","activationSupported":true,"presets":[{"id":"p","name":"Squad","definitionJson":"{\"operators\":[{\"operatorId\":\"op0\",\"slot\":0},{\"operatorId\":\"op1\",\"slot\":1},{\"operatorId\":\"op2\",\"slot\":2},{\"operatorId\":\"op3\",\"slot\":3},{\"operatorId\":\"op4\",\"slot\":4}],\"schemaVersion\":1}","revision":1,"updatedAt":"2026-09-10T05:00:00.000Z"}]}}
```

Saved preset edits do not alter formation.operators or activePresetRevision until
explicit reactivation. Missing ownership later does not fabricate replacements;
the next activation/departure must revalidate its selection.

S01-only reconstructed verification snapshot:
`C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/release-packages/screen-s01-20260910/server`.
Its parent source-manifest.json SHA256 is
`0764f2dbf34fd50367beef6b76754b993a77f47a65698c9461880eb7e4c2316f`.
It uses archived904ce840 + S01 only, migration0027 maximum, old false/null capability,
and its own original S01 document. Five state integration tests executed there PASS.
This is a verified reconstruction, not an asserted historic original build artifact.

Reuse `GET /v1/profile` for display name/commander XP,
`GET /v1/wallet/crystals` for the existing policy-scoped free/paid wallet contract,
and `GET /v1/growth/config` for verified economy version/hash and growth costs.
These are separate reads and do not share the account-state snapshot. No second
crystal balance calculation or duplicate growth catalog was added.

<a id="source-e190f6d9758e8e32-refresh-and-error-handling"></a>
#### Refresh and error handling

After accepted growth, claim, recruitment or other holdings mutation, schedule a
fresh account-state GET and refresh the relevant existing wallet/profile query.
For an unknown POST outcome, retry that POST with its original idempotency key,
then read state. A GET never establishes whether an unknown POST was committed.

Unity captures its session/account generation and local refresh request counter.
Discard responses from an old generation or superseded refresh request, including
failures. Replace the snapshot as a whole. Do not add the GET balances to cached
balances. Do not discard a refresh solely because mutationSequence is equal:
legacy/admin writers may update records without incrementing this counter.
The counter is diagnostic, not an ETag or universal concurrency token.

Authentication errors preserve existing 401/403/503 codes. Admission errors also
preserve existing middleware semantics and required content/version headers.
Any query/body returns 400 `contract_violation`. If the account disappears after
authentication, return 503 `account_state_unavailable`, never fabricate holdings.
Keep the previous snapshot visibly stale on failure; never replace it with zeros.

<a id="source-e190f6d9758e8e32-executed-local-verification"></a>
#### Executed local verification

Five new real-SQLite integration tests cover account isolation (currency, item,
operator, carriage and turret), explicit absent state, authentication/query
rejection, disappearance, read-only refresh, and the actual game entry point:
signed active session -> growth POST -> new balances/levels -> same-key retry ->
unchanged mutation sequence -> revoked-session rejection. Test accounts and
policies are fixtures, not a DEV/Unity device validation.

The initial S01 full suite was71files446tests. Later S03/S10 validation is recorded
in screen-integration-status-20260910.md and the matching result report.
No deployment, remote migration, OneDrive synchronization or Unity runtime claim.

<!-- source-body:source-e190f6d9758e8e32:end -->


<a id="source-572d5a712dea75c9"></a>
## Admin API

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/admin-api.md`  
원문 SHA-256: `746c2b68ab446dfb156aa80e096fc2fdc1fec4447ef2db7279127f144f31513f`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-572d5a712dea75c9:start -->
<a id="source-572d5a712dea75c9-admin-api"></a>
### Admin API

Base domains:

- DEV: `https://dev-admin.lastrail.app`
- PRD: `https://admin.lastrail.app`

The server currently uses `dev` and `prd` environment axes only. PRD resources and deploys remain approval-gated.

Admin domains must be protected by Cloudflare Access. LAST RAIL authorization maps Access JWT `sub` to `admin_principals.access_subject` and then applies OWNER / OPERATOR / VIEWER permissions.

<a id="source-572d5a712dea75c9-auth-skeleton"></a>
#### Auth Skeleton

`GET /v1/admin/me` is the first protected Admin route.

Current behavior:

- `LOCAL` uses `ADMIN_AUTH_MODE=LOCAL_HEADER` for local tests and development only.
- DEV/PRD use `ADMIN_AUTH_MODE=ACCESS_JWT_REQUIRED`.
- Non-local modes validate `Cf-Access-Jwt-Assertion` with the configured Cloudflare Access issuer and AUD tag.
- If issuer or AUD is not configured, protected routes return `ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED`.
- Local header mode reads `Cf-Access-Subject`, maps it to active `admin_principals.access_subject`, and returns the principal role.

Do not seed real admin principals or expose operational Admin routes until Cloudflare Access is applied and DEV `ADMIN_ACCESS_ISSUER` / `ADMIN_ACCESS_AUD` are configured.

<a id="source-572d5a712dea75c9-read-only-operations"></a>
#### Read-only Operations

<a id="source-572d5a712dea75c9-version-policy-browser-preconditions-local-not-deployed"></a>
##### Version policy browser preconditions (local, not deployed)

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

<a id="source-572d5a712dea75c9-dashboard-launch-summary"></a>
#### Dashboard Launch Summary

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

<a id="source-572d5a712dea75c9-server-deployments"></a>
#### Server Deployments

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

<a id="source-572d5a712dea75c9-story-progress"></a>
#### Story Progress

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

<a id="source-572d5a712dea75c9-offline-rewards"></a>
#### Offline Rewards

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

<a id="source-572d5a712dea75c9-session-metrics"></a>
#### Session Metrics

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

<a id="source-572d5a712dea75c9-customer-support"></a>
#### Customer Support

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

<a id="source-572d5a712dea75c9-gacha-support-read-model"></a>
#### Gacha Support Read Model

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

<a id="source-572d5a712dea75c9-balance-config-drafts"></a>
#### Balance Config Drafts

`POST /v1/admin/balance-config/drafts` creates a draft row in `balance_config_drafts`.

Required body:

- `schemaVersion`: positive integer
- `requiredContentVersion`: non-negative integer
- `config`: JSON object

Optional body:

- `baseVersion`: positive integer or `null`

OWNER and OPERATOR can create drafts. VIEWER is denied. The server canonicalizes `config`, computes a `sha256:<hex>` checksum, stores revision `1`, and returns draft metadata. This endpoint does not publish, activate, or mutate existing published versions.

<a id="source-572d5a712dea75c9-balance-config-publish"></a>
#### Balance Config Publish

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

<!-- source-body:source-572d5a712dea75c9:end -->


<a id="source-33dcea8f8cbca85a"></a>
## Admin player activity summary v1

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/admin-player-activities.md`  
원문 SHA-256: `4d4b14b6c54544fa847593e5e321f9d4546d9ba1c2771497ee856124c75f939f`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-33dcea8f8cbca85a:start -->
<a id="source-33dcea8f8cbca85a-admin-player-activity-summary-v1"></a>
### Admin player activity summary v1

`GET /v1/admin/players/:accountId/activity-summary`

Uses existing Admin authentication and `SUPPORT_READ` (OWNER/OPERATOR/VIEWER).
It does not impersonate a game account or accept game bearer tokens as Admin
authority. No query parameters or request body are accepted. Missing account is
404; invalid input is400. Responses use `Cache-Control: no-store`.

Top-level fields:

| Field | Contract |
| --- | --- |
| version | `admin-player-activities-v1` |
| accountId | Exact account selected by the authenticated Admin caller |
| observedAt | Server timestamp in milliseconds |
| permission / permissionMode | `SUPPORT_READ` / `ROLE_PERMISSION` |
| readOnly | true |
| consistency | `INDEPENDENT_LIVE_READS`; not a transaction-wide snapshot |
| shop / monthlyAttendance / pass / ordinaryQuests / mail | Sections below |

Every section has `{availability, reason, data}`. `AVAILABLE` has `reason:null`
and an object in `data`; its known zero, false, empty arrays and nullable fields
must remain distinct. `UNCONFIGURED` means the current policy is absent and has
`data:null`. `UNAVAILABLE` means invalid configuration, conflicting policy, or a
failed source read and also has `data:null`. A failed section does not erase
unaffected sections. Unknown errors use `source_read_failed`, never raw SQL/error
text. An unavailable section must not be shown as an empty successful result.

<a id="source-33dcea8f8cbca85a-section-data"></a>
#### Section data

`shop`: policyVersion, currencyId, wallet{freeBalance,paidBalance,totalBalance},
dayStart, nextResetAt, ticketOffersConfigured, dailyRemaining,
ticketUnavailableReason, purchaseConfigured, configuredProductCount,
purchaseVerificationAvailable, purchaseVerificationReason, cashPrice,
cashPriceSource, materialShopAvailability. Daily remaining is0 or1 only when the
existing launch100-crystal exchange is configured; otherwise it isnull.
purchaseVerificationAvailable isnull with GAME_RUNTIME_NOT_OBSERVED because this
Admin read cannot establish the game Worker's live verifier. cashPrice isnull,
STORE_REQUIRED; no price is fabricated. The material shop remains UNCONFIGURED.

`monthlyAttendance`: activityId, policyVersion, cycleId, active, canAttend,
unavailableReason, revision, completedCells, todayAttended, recoveryUsed,
recoverableCount, cells[]. Each cell exposes only cell, claimed, source,
dayIndex, unavailableReason. No claim receipt or reward payload is copied. The
existing monthly discovery/pinning/conflict rules are reused. A valid policy
with missing elite-ticket binding can be read but has canAttend=false and
elite_ticket_binding_missing; this is not absent configuration or a zero reward.

`pass`: current PASS50 policyId/startAt/endAt, earnedXp, xp(capped5000), level
(capped50), premium, purchaseConfigured, cosmeticBindingsComplete,
progressSyncPending, progressSource. Premium uses the existing account-bound,
non-revoked verified-order entitlement relation without returning order IDs.
XP comes from persisted awards; no synchronization or mutation is invoked.

`ordinaryQuests`: current QUESTS policyId/startAt/endAt, limit100, hasMore,
progressSyncPending, progressSource, items[]. Items contain awardId, objectiveId,
periodId, earnedAt, periodEnd and status CLAIMED/MAILED/MAIL_PENDING/EARNED.
This is a summary of recorded awards, not an assertion that all original events
have already been synchronized. progressSource is
PERSISTED_AWARDS_NO_SYNC_ON_ADMIN_READ for both pass and quests.

`mail`: limit100, hasMore, claimabilitySource
RECORDED_RIGHT_AND_EXPIRY_NOT_MUTATION_PREFLIGHT, items[]. Each has awardId,
policyId, objectiveId, sentAt, expiresAt, and CLAIMED/EXPIRED/CLAIMABLE. The query
requires matching ownership on both the mail and award records. Historical mail
can be available even when no current quest policy exists. CLAIMABLE describes
the recorded right and expiry; it is not a transaction preflight or permission
to grant. Neither source response JSON nor raw rewards are exposed.

Quest and mail summaries return the latest100 (timestamp descending, stable ID
tie order), probing101 to set hasMore. They are intentionally not exhaustive
history endpoints and do not supply a pagination cursor. No total is inferred
from the truncated array length.

<a id="source-33dcea8f8cbca85a-implementation-and-verification"></a>
#### Implementation and verification

Projection: `apps/admin-api/src/player-activities.ts`; route registration in
`apps/admin-api/src/index.ts`. Read-only policy helpers are reused from game-api;
Admin's composite TypeScript project explicitly lists their transitive source
dependencies, following the existing async-worker project pattern. This does not
mount game routes or grant mutation permissions.

Actual migrated SQLite regression covers existing Admin authentication, account
existence, query/body refusal, no-store, missing configuration versus empty mail,
known0 balances/XP, owner-isolated records, contradictory mail/award ownership,
100-row caps, absent source tables, invalid policies, and monthly missing elite
binding. SELECT-only interception plus before/after serialized-DB SHA256 verifies
that reads do not synchronize or write. Raw payload canaries remain absent.

The tests export four uniquely named actual response fixtures under
`audit-results/admin-player-activities`: unconfigured, configured-zero-and-owned-
records, unavailable-source, monthly-incomplete-binding. These are synthetic
SQLite account data for UI regression, not live account observations. Fixture
filenames are never overwritten by later test runs.

Admin must verify version and exact accountId before showing sections. Preserve
availability, progressSyncPending, hasMore and unknown null values. Endpoint
completion does not establish browser responsiveness or a remote deployment;
those are the Admin caller's next integration/QA work.

<!-- source-body:source-33dcea8f8cbca85a:end -->


<a id="source-191ef8c5ab1f7fa5"></a>
## Admin Session Metrics Contract Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/admin-session-metrics.md`  
원문 SHA-256: `63d7d8c660e163140ab569cfc79730af515162b6a982442b023752d5a1ab05de`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-191ef8c5ab1f7fa5:start -->
<a id="source-191ef8c5ab1f7fa5-admin-session-metrics-contract-draft"></a>
### Admin Session Metrics Contract Draft

This document defines the read-only Admin metrics contract for login, session,
concurrent-user, and last-login visibility.

Current local implementation composes account-level support metrics into
`GET /v1/admin/support/accounts/:accountId`. Dashboard aggregate/session-summary
routes, migrations, session heartbeats, Cloudflare resources, DEV deploys,
remote smoke tests, and PRD/STAGING resources are not implemented.

<a id="source-191ef8c5ab1f7fa5-goals"></a>
#### Goals

- Give operations a quick view of account login/session health.
- Give support enough last-login context to answer account activity questions.
- Keep session/login metrics server-owned.
- Avoid exposing auth secrets, raw headers, refresh token hashes, or player
  tokens.
- Separate approximate launch metrics from future real-time concurrency.

<a id="source-191ef8c5ab1f7fa5-non-goals"></a>
#### Non-goals

- No account mutation.
- No session revocation or repair.
- No login replay tooling.
- No user tracking beyond server-owned auth/session records.
- No client-supplied session/login data as Source of Truth.
- No production deployment or secret changes.

<a id="source-191ef8c5ab1f7fa5-current-foundation"></a>
#### Current Foundation

Existing tables:

- `accounts`
- `auth_identities`
- `auth_sessions`
- `player_profiles`

Existing support read model exposes:

- `activeSessionCount`
- `latestSessionCreatedAt`
- `latestSessionExpiresAt`
- `latestSessionAt`
- `latestAuthAt`
- `lastLoginAt`
- `lastLoginAgeSeconds`
- `identityProviders`
- `providerNames`
- `linkedProviders`
- `primaryProvider`
- `hasGoogleLink`
- nullable `lastSeenAt`, `concurrentUsers`, and precision/source markers until
  a server-owned activity source exists

Current limitation:

- `auth_sessions.created_at` is a session creation timestamp, not necessarily
  a precise last-login or last-seen timestamp.
- `auth_sessions.status = 'ACTIVE'` plus `expires_at > now` approximates an
  active refresh session, not true real-time concurrency.
- There is no server-owned heartbeat or last-seen event table yet.

<a id="source-191ef8c5ab1f7fa5-metric-definitions"></a>
#### Metric Definitions

<a id="source-191ef8c5ab1f7fa5-activesessions"></a>
##### activeSessions

Launch definition:

```text
count auth_sessions where status = 'ACTIVE' and expires_at > now
```

Meaning:

- Number of currently valid server refresh sessions.
- This may include accounts that are not online at this instant.

Use:

- Admin dashboard aggregate.
- Support account auth section.

<a id="source-191ef8c5ab1f7fa5-concurrentusers"></a>
##### concurrentUsers

Launch recommendation:

- Do not claim exact concurrency from `auth_sessions` alone.
- Display as `estimatedConcurrentUsers` only if derived from recent server
  activity.

Future exact-ish definition:

```text
count distinct account_id where latest server-owned activity occurred within 5 minutes
```

Potential sources:

- future `account_activity_events`;
- future lightweight Game API heartbeat;
- recent authenticated Game API request summaries;
- Workers Analytics/Logs companion data, if sanitized and queryable.

<a id="source-191ef8c5ab1f7fa5-recentactiveaccounts5m--recentactiveaccounts30m"></a>
##### recentActiveAccounts5m / recentActiveAccounts30m

Recommended definition:

```text
count distinct account_id where last_server_activity_at >= now - window
```

Window candidates:

- 5 minutes: online/concurrency-like freshness.
- 30 minutes: short active-session cohort.

Launch note:

- Without a last activity source, do not backfill this from refresh token TTL.

<a id="source-191ef8c5ab1f7fa5-lastloginat"></a>
##### lastLoginAt

Launch account-detail definition:

```text
max(auth_sessions.created_at) for the account
```

Meaning:

- Last successful session creation or refresh rotation that created a new
  session.
- It is not the same as last gameplay activity.

Future improvement:

- Add `accounts.last_login_at` or `account_auth_activity.last_login_at` if
  login history becomes an explicit product/support need.

<a id="source-191ef8c5ab1f7fa5-lastloginageseconds"></a>
##### lastLoginAgeSeconds

Definition:

```text
floor((server_now - lastLoginAt) / 1000)
```

Rules:

- Server computes it from server time.
- Admin may also display a relative label, but the Source of Truth timestamp is
  server-owned.
- Missing last login returns `null`, not `0`.

<a id="source-191ef8c5ab1f7fa5-lastloginagebuckets"></a>
##### lastLoginAgeBuckets

Recommended dashboard buckets:

```text
0-5m
5-30m
30m-2h
2h-24h
1d-7d
7d-30d
30d+
never
```

Use:

- Dormant/returning account distribution.
- Support and operations trend view.

<a id="source-191ef8c5ab1f7fa5-dormantaccounts--returningaccounts"></a>
##### dormantAccounts / returningAccounts

Draft definitions:

- `dormantAccounts`: accounts whose last login age is at least 30 days.
- `returningAccounts`: accounts with a new session in the selected window and
  previous last login at least 7 days earlier.

These require login history or prior-session comparison. If only current
`auth_sessions` rows are available, mark the metric as unavailable rather than
guessing.

<a id="source-191ef8c5ab1f7fa5-recommended-api-placement"></a>
#### Recommended API Placement

<a id="source-191ef8c5ab1f7fa5-dashboard-summary"></a>
##### Dashboard Summary

Future endpoint candidate:

```text
GET /v1/admin/metrics/session-summary
```

Permission candidate:

```text
OPS_DASHBOARD_READ
```

Interim permission:

- Use existing role-based `ROLE_PERMISSION` mapping if a granular permission
  table is still absent.
- OWNER / OPERATOR should be allowed.
- VIEWER can be allowed for aggregate-only metrics if the response contains no
  account ids or sensitive auth detail.

Response candidate:

```json
{
  "accepted": true,
  "generatedAt": "2026-08-29T00:00:00.000Z",
  "window": {
    "recent5mSeconds": 300,
    "recent30mSeconds": 1800
  },
  "metrics": {
    "activeSessions": 42,
    "concurrentUsers": null,
    "concurrentUsersPrecision": "UNAVAILABLE",
    "recentActiveAccounts5m": null,
    "recentActiveAccounts30m": null,
    "lastLoginAgeBuckets": {
      "0-5m": 0,
      "5-30m": 0,
      "30m-2h": 0,
      "2h-24h": 0,
      "1d-7d": 0,
      "7d-30d": 0,
      "30d+": 0,
      "never": 0
    }
  },
  "source": {
    "activeSessions": "auth_sessions",
    "concurrentUsers": "not_available_without_activity_source",
    "lastLoginAgeBuckets": "auth_sessions.created_at"
  },
  "errorCode": null,
  "message": null
}
```

Notes:

- `concurrentUsers` should be nullable until a server activity/heartbeat source
  exists.
- `concurrentUsersPrecision` should prevent UI labels from overstating
  accuracy.
- No account ids should be included in the aggregate response.

<a id="source-191ef8c5ab1f7fa5-account-detail--support"></a>
##### Account Detail / Support

Existing support endpoint:

```text
GET /v1/admin/support/accounts/:accountId
```

Future player detail endpoint section candidate:

```text
GET /v1/admin/players/:accountId
```

Account-level fields:

```json
{
  "auth": {
    "identityProviders": ["GUEST", "GOOGLE"],
    "providerNames": ["GUEST", "GOOGLE"],
    "linkedProviders": ["GUEST", "GOOGLE"],
    "primaryProvider": "GOOGLE",
    "hasGoogleLink": true,
    "activeSessionCount": 1,
    "latestSessionCreatedAt": "2026-08-29T00:00:00.000Z",
    "latestSessionAt": "2026-08-29T00:00:00.000Z",
    "latestSessionExpiresAt": "2026-09-28T00:00:00.000Z",
    "latestAuthAt": "2026-08-29T00:00:00.000Z",
    "lastLoginAt": "2026-08-29T00:00:00.000Z",
    "lastLoginAgeSeconds": 120,
    "lastSeenAt": null,
    "lastSeenSource": "not_available_without_activity_source",
    "concurrentUsers": null,
    "concurrentUsersPrecision": "UNAVAILABLE"
  }
}
```

Use account-level fields in support/player detail, not dashboard aggregate,
when the operator is investigating one account.

<a id="source-191ef8c5ab1f7fa5-future-storage-candidate"></a>
#### Future Storage Candidate

Only add storage if exact recent activity or login history becomes necessary.

<a id="source-191ef8c5ab1f7fa5-account_activity_summary"></a>
##### account_activity_summary

Candidate fields:

- `account_id`
- `last_login_at`
- `last_seen_at`
- `last_game_api_request_at`
- `last_run_started_at`
- `last_run_finished_at`
- `last_refresh_at`
- `updated_at`

Use:

- Account support/player detail.
- Recent active account windows.
- Dormant/returning account buckets.

<a id="source-191ef8c5ab1f7fa5-account_activity_events"></a>
##### account_activity_events

Candidate fields:

- `id`
- `account_id`
- `event_type`
- `occurred_at`
- `request_id`
- `session_id_hash`
- `source`
- `metadata_json` sanitized

Use only if support needs a timeline. Start with summary before event history.

<a id="source-191ef8c5ab1f7fa5-security-and-masking"></a>
#### Security And Masking

Never return:

- access tokens;
- refresh tokens;
- refresh token hashes;
- raw `Authorization` headers;
- Cloudflare Access JWTs;
- service token values;
- full session family internals;
- device fingerprints unless a separate privacy/security policy approves them;
- raw request bodies;
- account shortcut headers.

Allowed:

- counts;
- server timestamps;
- coarse buckets;
- stable status codes;
- hash/reference ids only when needed for correlation.

<a id="source-191ef8c5ab1f7fa5-error-codes"></a>
#### Error Codes

Candidate stable codes:

```text
auth_session_metrics_unavailable
auth_activity_source_missing
auth_session_metrics_query_failed
contract_violation
ADMIN_PERMISSION_DENIED
```

Guidance:

- Missing activity source is not fatal if the response can return
  `concurrentUsersPrecision = 'UNAVAILABLE'`.
- Query failure should fail closed for the affected endpoint/section and log an
  ops error summary once write integration exists.

<a id="source-191ef8c5ab1f7fa5-admin-display-guidance"></a>
#### Admin Display Guidance

Dashboard labels:

- 현재 동접자: only when activity source exists.
- 활성 세션 수: safe from `auth_sessions`.
- 최근 5분 접속 계정: only when `last_seen_at` exists.
- 최근 30분 접속 계정: only when `last_seen_at` exists.
- 마지막 로그인 경과 분포: bucketized aggregate.

Support/player detail labels:

- 마지막 로그인 시각.
- 마지막 로그인 경과 시간.
- 활성 세션 수.
- 마지막 접속 시각: unavailable until server activity source exists.

If a metric is unavailable, Admin should show `연동 대기` or `활동 소스 없음`
instead of estimating from client state.

<a id="source-191ef8c5ab1f7fa5-test-candidates"></a>
#### Test Candidates

Future contract/unit tests:

- aggregate endpoint rejects request bodies with `contract_violation`;
- aggregate endpoint returns active session count from unexpired ACTIVE
  sessions only;
- expired, ROTATED, REUSED, REVOKED, and EXPIRED sessions are excluded from
  active session count;
- `concurrentUsers` remains null when no activity source exists;
- last-login buckets are computed from server timestamps;
- account detail returns `lastLoginAt` and `lastLoginAgeSeconds` from server
  session records;
- response omits tokens, refresh token hashes, raw auth headers, raw request
  bodies, and account shortcut fields.

Future local D1 smoke:

- seed active, expired, rotated, and reused sessions;
- verify aggregate counts and buckets;
- verify no writes occur from read-only metrics endpoints.

<a id="source-191ef8c5ab1f7fa5-open-decisions"></a>
#### Open Decisions

- Whether dashboard metrics should live under `/v1/admin/metrics/session-summary`
  or a broader `/v1/admin/dashboard/summary`.
- Whether VIEWER can see aggregate session metrics.
- Whether exact concurrency requires a heartbeat endpoint, request summary
  table, or Workers Analytics companion.
- Whether `lastLoginAt` should mean guest creation, refresh rotation, or only
  explicit login/reauth events.
- Whether dormant/returning buckets are needed for launch or later retention
  work.

<!-- source-body:source-191ef8c5ab1f7fa5:end -->


<a id="source-0b8706c7816cdc23"></a>
## Attendance elite-ticket evidence and unapproved next contract

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/attendance-elite-ticket-review.md`  
원문 SHA-256: `8170243ce14778a1fa51739fa0961d683da57830650dfba173d975e446d7b190`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-0b8706c7816cdc23:start -->
<a id="source-0b8706c7816cdc23-attendance-elite-ticket-evidence-and-unapproved-next-contract"></a>
### Attendance elite-ticket evidence and unapproved next contract

Source inspection2026-09-10: server apps/packages and shared Unity Scripts have no
identified elite-ticket item/consumer. General gacha spends cur_command_ticket and
updates its ordinary/pickup pity state. That is not the missing guaranteed ticket.
Plan confirmed the missing contract; its decision proposal remains unapproved.

Existing duplicate code in game-api index.ts: getOperatorShardItemId returns
`cur_operator_token:${operatorId}`; getDuplicateConversionQuantity returns50 for
ELITE_OPERATOR (30 for other operator rarity). A duplicate adds that operator's
token to account_items and item_ledger reason GACHA_DUPLICATE_CONVERSION, while
response ID is cur_operator_token with separate operatorId and quantity50.
No re-roll/new-unowned guarantee follows. This is actual code evidence, not new
approval of a guaranteed-ticket pool or reuse of ordinary pity.

account_items (migration0013) stores account_id/item_id/quantity/updated_at and has
no expiry column/job in this inspected path. This proves only current generic
inventory persistence; it does not establish an approved expiration policy for a
new entitlement. Ordinary quest mailbox14days must not be copied to it.

Proposed identity, **not created or enabled**: `item_attendance_elite_ticket` with
entitlement policy `attendance-elite-v1`. Check full future catalog collisions
before adoption. Aggregate item balance alone cannot preserve multiple historic
pool versions. Use grant-receipt-linked lots carrying entitlement/pool/duplicate/
pity policy versions and remaining count; keep explicit version identity when
displaying aggregate stock. Suggested no expiry needs approval, not inference from
the absence of an account_items expiry field.

After user decision, a separate explicit consume command should select owned lot
and expected revision with idempotency key; one atomic batch consumes one right,
pins server RNG result and policy, grants actual operator or its approved50tokens,
and persists the response. Replay never draws again. Pool equality/initial-five
membership/no-pickup/no-ordinary-pity changes are Plan's current proposals only.
The consumer must neither debit cur_command_ticket nor silently update its pity.
No pool/member list, entropy policy, identity or entitlement table is implemented
by this review. S08 fixture item grants do not complete this pending consumer.

<!-- source-body:source-0b8706c7816cdc23:end -->


<a id="source-c9524f3d598611b7"></a>
## Attendance and pass foundations — local only, 2026-09-07

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/attendance-pass.md`  
원문 SHA-256: `8edc879ccb33ec8d037429cb6fc5e117cde7e890863f462faee1612fd092454f`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-c9524f3d598611b7:start -->
<a id="source-c9524f3d598611b7-attendance-and-pass-foundations--local-only-2026-09-07"></a>
### Attendance and pass foundations — local only, 2026-09-07

Implemented in the isolated `codex/attendance-pass-20260907` worktree from
`65b0263524bfd21ed42e597f1205cc81723fd5b9`. No deployed API changes, remote
migration, release policy seed, store connection or real purchases in this work.

<a id="source-c9524f3d598611b7-configuration-and-policy-status"></a>
#### Configuration and policy status

Migration 0022 creates empty immutable `activity_definitions` and account-scoped
claim/visit/XP/entitlement tables. A definition ID identifies one immutable policy;
updates/deletion are rejected. Missing or invalid configuration returns 503.
Definitions must be populated only by a separately approved release process.
There is deliberately no public definition or entitlement-writing endpoint.

Confirmed: daily attendance/quest/usage reset is **04:00 KST** (19:00 UTC), weekly
reset Monday 04:00 KST, and pass boundaries align with 04:00 KST. This module
implements the attendance/pass clock only; existing raid/quest/quota code and
offline elapsed-time calculations are not changed. Older 05:00 raid documentation
requires owner reconciliation, not silent promotion here.

Attendance supports explicitly configured `DISTINCT_DAYS`, `dailyRewards`,
milestones and optional recovery. Supported models are implementation capabilities,
not approved launch values. Natural visits and recovered visits share the same
account/campaign/day uniqueness and cumulative milestone receipts. Recovery requires
an explicit inventory item/cost/limit and `missedDaysOnly: true`; otherwise it is
disabled. It supports past missed campaign dates, excludes today/future dates and
expires at campaign end. These detailed recovery settings are a configurable
proposal awaiting launch policy approval. `recovery: null` disables it.

Pass definitions explicitly specify start/end/claimUntil, ascending cumulative XP
thresholds, free/premium reward arrays, eligible server run outcome XP and nullable
store product mapping. There are no default seasons, XP values or rewards. Run XP
requires the authenticated owner's server-accepted completed/failed run and final
result within the season; quarantined/unowned/client XP are rejected. No solo/co-op
requirement is added. This is an explicit claim endpoint; no automatic run-finish
hook or quest XP source is enabled.

<a id="source-c9524f3d598611b7-routes"></a>
#### Routes

All routes use the existing verified session/account and client admission layers.
No caller account ID, server date, reward amount, XP amount or premium flag.
POST requires an ASCII nonblank Idempotency-Key of at most 128 characters; payload
stream is bounded to 18,000 bytes. Unknown JSON fields and query strings are rejected.

| Method/path | Body |
| --- | --- |
| GET `/v1/attendance/:activityId` | none |
| POST `/v1/attendance/:activityId/check-in` | none |
| POST `/v1/attendance/:activityId/milestones/claim` | `{ "days": integer }` |
| POST `/v1/attendance/:activityId/recover` | `{ "dayIndex": integer }` |
| GET `/v1/passes/:activityId` | none |
| POST `/v1/passes/:activityId/xp/claim` | `{ "runId": string }` |
| POST `/v1/passes/:activityId/rewards/claim` | `{ "level": integer, "track": "FREE" or "PREMIUM" }` |
| POST `/v1/passes/:activityId/purchases/verify` | `{ "receipt": string, "productId": string }` |

dayIndex is the server day integer displayed by GET, calculated as
floor((serverEpochMs - 68,400,000) / 86,400,000). Clients may select a missed day,
but the server checks campaign bounds, current day, prior visits and recovery cap.

Successful claims include immutable receipt IDs, reward lines/balances and server
time. Same account/operation/key and request fingerprint returns the original
response; changed input returns 409. Different keys cannot repeat the same business
claim. Historical replay remains available after expiry/revocation; it is a receipt,
not fresh eligibility. GET and each new premium claim check current entitlement.

The D1 batch includes account mutation, economy transaction, inventory updates,
cost/reward ledgers, reward receipt, activity state and idempotency record. The
account mutation sequence fences concurrent economic state. A DB-time guard checks
campaign, daily reset and premium entitlement expiry/revocation inside the batch;
failed eligibility rolls back all writes and retries with fresh state up to three
times. Store transactions are globally unique per provider/application.

<a id="source-c9524f3d598611b7-purchase-and-currency-limits"></a>
#### Purchase and currency limits

`PASS_PURCHASE_VERIFIER` is a trusted server-only adapter interface with **no
default implementation**. It must verify actual store identity/signature/API state,
account ownership, application/product/environment, timestamps and refund/revocation.
Absent adapter returns 503; a client receipt/flag alone grants nothing. Opaque
receipts are not persisted or logged by this module; their request digest is stored.
Persisted entitlements retain store transaction and purchase-verification receipt
links; premium claims join them by account/activity. The test adapter is not a real
store verifier. Real provider APIs, callbacks, refunds and regrant/renewal policy
remain unimplemented and must be designed before enabling purchases.

The user approved direction for one common premium currency with earned/purchased
accounting, order tracing and refund design. Attendance and both pass tracks grant
FREE/earned currency; premium-pass grants still need their purchase link. Offline
repeat rewards grant no new crystals. These decisions do not establish a currency
ID, price or exchange rate. The generic inventory recovery test path is not the
final earned/purchased wallet. Do not point production crystal spending at it.

The subsequent crystal wallet implementation is now complete locally; see
`crystal-wallet.md`. FREE-first spending and premium order linkage are confirmed
and implemented. The earlier wallet proposal below is historical, not outstanding
implementation work. Numeric release policy and real store adapters remain pending.

Original proposed wallet work: separate earned and purchased ledgers, explicit spend order,
atomic currency debit plus ticket credit and request replay, and immutable policy
version on grants/exchanges. Preserve existing tickets and every receipt. Keep
existing login bonus and first-clear uniqueness when changing future grant policy,
so previous recipients cannot claim a second converted reward. No retroactive
account conversion, ticket confiscation, pity or probability changes are included.
The proposed 100-unit exchange and 1,200-unit total remain unapproved examples.

<a id="source-c9524f3d598611b7-remaining-launch-decisions"></a>
#### Remaining launch decisions

1. Attendance campaign duration/counting policy, full daily/milestone reward tables.
2. Recovery currency ID/price/cap/window and whether proposed missed-day rules are final.
3. Pass season dates, claim grace, XP sources/amounts/thresholds and both reward tables.
4. Currency name/ID, ticket exchange pricing and policy transition (FREE-first spend order is confirmed).
5. Store/provider/app/product ownership, refund/revocation/renewal and paid-pass grant tracking.
6. Separate review/approval for release definitions, remote migration/deployment and real store setup.

<a id="source-c9524f3d598611b7-verification"></a>
#### Verification

RED demonstrated missing migration/API, then new recovery/clock contracts. Independent
review found awaited verification timestamps and premium expiry races; both received
reproducing tests and were fixed. Final local run: **63 files / 376 tests PASS**;
four TypeScript projects and eslint PASS. Actual Wrangler local D1 0001–0022 applies;
foreign_key_check empty, quick_check ok, activity definitions count zero. Tests cover
same/different request keys, concurrent visits/recovery, insufficient balance,
ledger failure rollback, 04:00 crossing, campaign/claim deadlines, run ownership and
event replay, invalid/expired/revoked premium and DB-execution-time revocation.

The DB batch behavior follows [Cloudflare D1's documented batch transactions](https://developers.cloudflare.com/d1/worker-api/d1-database/).
No Android/device or actual store verification is claimed.

<!-- source-body:source-c9524f3d598611b7:end -->


<a id="source-6f64a90df51fe75e"></a>
## Auth Account Linking Contract Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/auth-account-linking.md`  
원문 SHA-256: `9186d9933ca0937ed46c8026b12034566328b35030515e26526b3b541ec972f7`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-6f64a90df51fe75e:start -->
<a id="source-6f64a90df51fe75e-auth-account-linking-contract-draft"></a>
### Auth Account Linking Contract Draft

Status: draft, local contract only. This document closes the current Unity-facing
auth dependency without enabling remote migrations, real Google OAuth values,
email delivery, or production account writes.

<a id="source-6f64a90df51fe75e-scope"></a>
#### Scope

Act 1 login has three player-facing entry paths:

- Guest start
- Login ID and password
- Google Sign-In

`accountId` remains the immutable server primary key. `loginId` and
`displayName` are player-facing/account-facing identifiers with different
purposes:

- `accountId`: server-generated primary key, never chosen by the player.
- `displayName`: nickname shown to players, globally unique by normalized value.
- `loginId`: credential identifier for ID/password auth, not a nickname and not
  a profile display field.

Unity may store only the server-issued session secret material in secure device
storage. It must not store raw passwords or Google ID tokens long-term.

<a id="source-6f64a90df51fe75e-current-implemented-baseline"></a>
#### Current Implemented Baseline

Already implemented:

- `POST /v1/auth/guest`
- `POST /v1/auth/refresh`
- `POST /v1/auth/google`
- `POST /v1/auth/google/link`
- `POST /v1/auth/password/recovery-email/verify/start` (local
  provider-neutral sender contract only)
- `POST /v1/auth/password/recovery-email/verify/confirm`
- `POST /v1/auth/password/register`
- `POST /v1/auth/password/login`
- `POST /v1/auth/password/link`
- `POST /v1/auth/password/reset/start` (generic response, local
  provider-neutral sender contract only)
- `POST /v1/auth/password/reset/confirm`
- `GET /v1/profile`
- `GET /v1/profile/display-name/check`
- `POST /v1/profile/display-name`

`auth_identities.provider` remains `GUEST`/`GOOGLE`. ID/password credentials are
stored separately in `auth_password_credentials` and use
`auth_recovery_emails` plus `auth_email_verification_challenges` for verified
recovery ownership.

<a id="source-6f64a90df51fe75e-guest-restore"></a>
#### Guest Restore

Guest creation returns the normal token bundle:

- `tokenType`
- `account.id`
- `account.status`
- `accessToken`
- `accessTokenExpiresAt`
- `refreshToken`
- `refreshTokenExpiresAt`

Unity should persist the refresh token using Android Keystore-backed secure
storage. App restart should attempt `POST /v1/auth/refresh` before offering a
new guest. Timeout/offline must not delete the stored session or create a new
guest. Only explicit invalid/expired/reused session responses should move the
player to recovery or login UI.

Launch response-loss boundary:

- `POST /v1/auth/guest` accepts an optional client-generated
  `Idempotency-Key`. When supplied, same-key retry returns the same guest account
  and token bundle instead of creating another guest. Without the header, guest
  creation remains a new-account request.
- Concurrent same-key guest creation is collapsed by the provider-subject
  uniqueness guard plus replay lookup, so only one account/session is committed.
- `POST /v1/auth/refresh` accepts an optional client-generated
  `Idempotency-Key`. When the original rotation committed but the client lost
  the response, retrying the same old refresh token with the same key can recover
  the rotated token bundle without marking the refresh family `REUSED`.
- Concurrent same-key refresh rotation is collapsed by the session generation
  uniqueness guard plus replay lookup, so the token family is not marked
  `REUSED` by the losing request.
- Unity must still not create a new guest automatically for timeout, offline,
  cancelled request, 5xx, or unknown response state. Store and reuse the same
  high-entropy idempotency key until the token bundle is durably persisted.
- Auth replay keys shorter than 16 chars or longer than 128 chars are rejected
  with `contract_violation`. Unity should still generate substantially more
  entropy than the minimum length implies.
- Guest replay returns `auth_replay_expired` after the committed session is no
  longer `ACTIVE` or is past its server expiry. The same key must not create a
  second guest after expiry.
- Refresh replay returns `auth_replay_expired` when the committed rotated
  session is no longer active or is past its server expiry, and this path does
  not mark the session family `REUSED`.
- Stable terminal auth responses are explicit `401`/`403` codes such as
  `refresh_token_invalid`, `refresh_session_invalid`, `refresh_token_reused`,
  and `account_not_active`. Network timeout, offline, and generic `5xx` are not
  terminal account ownership decisions.

Remaining client integration gate:

- Unity secure storage must persist the refresh token and the in-flight
  idempotency key before retrying.
- New security settings or account recovery screens still require explicit
  approval.
- The server never stores refresh tokens in plaintext and never accepts
  account-id shortcuts as authority.
- Wider abuse controls, retention windows, and device/account recovery policy
  remain launch operations decisions.

<a id="source-6f64a90df51fe75e-login-id--password-candidate-contract"></a>
#### Login ID / Password Candidate Contract

Local endpoints:

```text
POST /v1/auth/password/register
POST /v1/auth/password/login
POST /v1/auth/password/link
POST /v1/auth/password/recovery-email/verify/start
POST /v1/auth/password/recovery-email/verify/confirm
POST /v1/auth/password/reset/start
POST /v1/auth/password/reset/confirm
GET /v1/auth/links
```

`GET /v1/auth/links` is still a future read-only helper candidate; the runtime
ID/password routes above are implemented locally without enabling production
email delivery. The local sender boundary now includes a Gmail API adapter
behind the existing `RECOVERY_EMAIL_SENDER` contract, but real Gmail OAuth
credentials and real send smoke remain setup-gated.

`POST /v1/auth/password/register` creates a new account with an ID/password
credential. `POST /v1/auth/password/link` requires Bearer auth and attaches an
unused login ID credential to the current account, preserving `accountId`,
inventory, story progress, rewards, and sessions.

Both register and link require an explicit recovery email and completed
server-side email ownership verification before the password credential becomes
usable. Pending registration/link state must not replace the current guest or
Google session. Abandon, timeout, resend, or retry keeps the existing account
state and must not create duplicate accounts or credentials.

`POST /v1/auth/password/login` signs in to the account that owns the login ID
and returns the same token bundle shape as guest and Google auth.

Candidate request:

```json
{
  "loginId": "rail_commander",
  "password": "player supplied secret",
  "recoveryEmail": "player@example.com",
  "verificationChallengeId": "email_challenge_..."
}
```

Do not accept `accountId`, `displayName`, reward fields, story progress fields,
or provider subject shortcuts in request bodies, query parameters, or headers.

Register response:

```json
{
  "tokenType": "Bearer",
  "accepted": true,
  "registerStatus": "REGISTERED",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:15:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

Login response uses the same token bundle and adds
`signInStatus: "SIGNED_IN"`. Password link response uses the same token bundle
and adds `linkStatus: "LINKED"`.

Recovery email verification start response:

```json
{
  "accepted": true,
  "verificationStatus": "PENDING",
  "challengeId": "email_challenge_...",
  "expiresAt": "2026-09-06T00:15:00.000Z",
  "retryAfterSeconds": 60,
  "errorCode": null,
  "message": null
}
```

Recovery email verification confirm response:

```json
{
  "accepted": true,
  "verificationStatus": "VERIFIED",
  "challengeId": "email_challenge_...",
  "expiresAt": "2026-09-06T00:15:00.000Z",
  "retryAfterSeconds": null,
  "errorCode": null,
  "message": null
}
```

Password reset start response is account-existence agnostic:

```json
{
  "accepted": true,
  "resetStatus": "EMAIL_SENT_IF_AVAILABLE",
  "errorCode": null,
  "message": null
}
```

The reset start HTTP response intentionally does not include `challengeId` or
any proof, because it must not reveal whether the account exists. The email
delivery payload contains only the one-use `verificationCode`; internal
`email_challenge_...` references stay server-side and must not be shown in the
player-facing email body.

Recommended Unity UX: ask the player to enter the same `loginId`, recovery
email, `verificationCode`, and the new password. The server resolves the latest
pending password reset challenge from the login ID and recovery email. Code
entry alone does not authenticate, does not rotate sessions, and does not
complete the reset without the matching account recovery inputs.

Password reset confirm response does not log the player in and does not return
tokens:

```json
{
  "accepted": true,
  "resetStatus": "RESET_CONFIRMED",
  "errorCode": null,
  "message": "Password was reset. Please sign in again."
}
```

Link status values:

- `LINKED`: current account newly owns the login ID credential.
- `ALREADY_LINKED`: current account already owns that credential; idempotent
  success.
- `CONFLICT_REQUIRES_CONFIRMATION`: credential belongs to another account and
  account switching must use the conflict preview/confirm flow.

Recovery email rules:

- Guest start and Google-only login do not require a recovery email.
- Google claim email is not automatically registered as the password recovery
  email. The player must explicitly submit and verify a recovery email.
- Recovery email is separate from `loginId`, `displayName`, and `accountId`.
- Matching email addresses do not link, merge, switch, or recover accounts by
  themselves.
- UI/support surfaces should use masked email only when a recovery flow needs
  it; public profile and account-conflict comparison must not expose email.

Candidate verification defaults:

- Email verification challenge TTL: 15 minutes.
- Resend cooldown: 60 seconds per account/email/purpose.
- Verification and reset proofs are one-use, cryptographically random, hashed
  at rest, and bound to purpose, account or pending transaction, and expiry.
- Reset request responses are generic whether or not the login ID/email exists.
- Reset request or link click alone must not sign the player in or revoke
  current sessions.
- Password reset confirm invalidates existing active sessions only after the
  new password verifier is stored. Linked Google identity is not deleted.
- Recovery email change requires recent re-authentication plus verification of
  the new address before replacing the stored recovery email.
- If the old recovery address is inaccessible, launch support must not promise
  manual recovery unless a separate approved support policy exists.

<a id="source-6f64a90df51fe75e-login-id-policy-recommendation"></a>
#### Login ID Policy Recommendation

This is a recommendation, not a user-finalized value:

- Normalize with Unicode NFKC, trim, and compare case-insensitively.
- Length 4 to 32 code points.
- Allow letters, numbers, `_`, `-`, and `.`.
- Reject reserved system-like values and values that look like internal ids.
- Return generic login failures for unknown ID or wrong password to reduce
  account enumeration.

<a id="source-6f64a90df51fe75e-password-storage-recommendation"></a>
#### Password Storage Recommendation

Use a proven password hashing implementation, not a custom algorithm. OWASP
recommends Argon2id when available, or scrypt/bcrypt/PBKDF2 with appropriate
costs. NIST requires salted password verifier storage and recommends storing
algorithm/cost metadata so work factors can migrate over time. The current
Workers-compatible local route skeleton uses WebCrypto `PBKDF2_SHA256` with
per-password random salt and stored cost metadata until a stronger Workers-safe
password hashing provider is approved.

Candidate local schema:

```sql
CREATE TABLE auth_password_credentials (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  login_id_normalized TEXT NOT NULL,
  password_hash TEXT NOT NULL,
  password_hash_algorithm TEXT NOT NULL,
  password_hash_params_json TEXT NOT NULL,
  recovery_email_normalized TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE UNIQUE INDEX idx_auth_password_credentials_login_id_unique
  ON auth_password_credentials(login_id_normalized);

CREATE TABLE auth_recovery_emails (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  email_normalized TEXT NOT NULL,
  email_masked TEXT NOT NULL,
  status TEXT NOT NULL,
  verified_at INTEGER,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE auth_email_verification_challenges (
  id TEXT PRIMARY KEY,
  account_id TEXT REFERENCES accounts(id) ON DELETE CASCADE,
  email_normalized TEXT NOT NULL,
  purpose TEXT NOT NULL,
  verifier_hash TEXT NOT NULL,
  status TEXT NOT NULL,
  expires_at INTEGER NOT NULL,
  consumed_at INTEGER,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);
```

If Workers runtime/package constraints block Argon2id, stop and report a
decision gate rather than weakening storage silently. PBKDF2-HMAC-SHA-256 using
WebCrypto is a possible compatibility fallback, but its iteration count and
latency must be measured before launch approval.

<a id="source-6f64a90df51fe75e-google-link-baseline"></a>
#### Google Link Baseline

Current Google rules remain:

- provider is `GOOGLE`
- provider subject is Google `sub`
- same account link is `ALREADY_LINKED`
- unused provider is `LINKED`
- provider owned by another account returns `google_identity_already_linked`
- automatic account merge is not available

Future conflict preview/confirm can generalize Google and login ID conflicts,
but the existing Google route should not silently switch accounts.

<a id="source-6f64a90df51fe75e-conflict-preview--confirm-candidate"></a>
#### Conflict Preview / Confirm Candidate

When the current guest tries to link a login ID or Google identity that already
belongs to another account, the server must not merge accounts or overwrite
either account.

Candidate endpoints:

```text
POST /v1/auth/account-switch/preview
POST /v1/auth/account-switch/confirm
```

Preview requires:

- current Bearer auth for the guest/current account
- provider proof for the target credential
- no account ID shortcut

Preview response:

```json
{
  "accepted": true,
  "switchStatus": "PREVIEW_READY",
  "switchRef": "account_switch_...",
  "expiresAt": "2026-08-26T00:05:00.000Z",
  "currentAccount": {
    "displayName": "Guest Commander",
    "accountType": "GUEST",
    "storyProgressId": "opn_03_water_stop",
    "lastLoginAt": "2026-08-26T00:00:00.000Z",
    "hasRecoveryCredential": false
  },
  "targetAccount": {
    "displayName": "Rail Captain",
    "accountType": "GOOGLE",
    "storyProgressId": "opn_06_broken_line",
    "lastLoginAt": "2026-08-20T00:00:00.000Z",
    "hasRecoveryCredential": true
  },
  "mergeAvailable": false,
  "allowedActions": ["KEEP_CURRENT", "SWITCH_TO_TARGET"],
  "warningCode": "guest_without_recovery_may_be_lost",
  "errorCode": null,
  "message": null
}
```

Confirm request:

```json
{
  "switchRef": "account_switch_...",
  "action": "SWITCH_TO_TARGET"
}
```

Confirm success returns:

```json
{
  "accepted": true,
  "switchStatus": "ACCOUNT_SWITCHED",
  "tokenType": "Bearer",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:15:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

`KEEP_CURRENT` cancels the switch and preserves the current guest session.
`SWITCH_TO_TARGET` creates a new target-account session; Unity must replace the
securely stored refresh token only after the new token bundle is durably stored.

<a id="source-6f64a90df51fe75e-conflict-safety-rules"></a>
#### Conflict Safety Rules

- Progress comparison is shown only after successful target credential proof.
- Safe summary must not expose raw `accountId`, full inventory, ledger detail,
  raw tokens, password hashes, Google ID tokens, or full Google claims.
- Progress is never auto-merged, max-selected, copied, or repaired.
- If both accounts have zero progress, the provider-owned-by-other-account case
  is still a conflict.
- Preview refs are short-lived and bound to current account, target provider,
  intended action, and snapshot revision.
- Stale refs, replayed confirms, different provider proofs, or changed target
  identity revisions return stable rejection.
- Combat, reward claim, run finish, gacha pull, or offline reward claim in
  progress should block account switching until the transaction is complete.
- Timeout/cancel keeps the existing guest session.
- Keystore save failure after confirm is a client persistence failure; Unity
  should keep the previous session available until it can safely retry or ask
  the player to sign in again. The server must not create another guest.

Candidate error codes:

- `login_id_taken`
- `login_id_invalid`
- `password_invalid`
- `password_auth_failed`
- `password_auth_unavailable`
- `auth_identity_conflict`
- `account_switch_preview_required`
- `account_switch_ref_expired`
- `account_switch_ref_invalid`
- `account_switch_revision_conflict`
- `account_switch_blocked_by_active_transaction`
- `guest_recovery_missing`
- `recovery_email_service_unavailable`
- `gmail_config_missing`
- `gmail_oauth_unavailable`
- `gmail_oauth_unauthorized`
- `gmail_send_unavailable`
- `gmail_send_unauthorized`
- `gmail_quota_exceeded`

<a id="source-6f64a90df51fe75e-recovery-email-provider-gmail-api"></a>
#### Recovery Email Provider: Gmail API

The launch-selected first recovery email provider is Gmail API, wired behind
the provider-neutral `RECOVERY_EMAIL_SENDER` interface. This is an operational
mail-sending integration only. It is separate from player Google Sign-In:
player Google ID tokens, Google profile claims, and game account linking are
not reused as mail-sending credentials.

The adapter follows the official Gmail API send flow:

- Exchange a server-owned OAuth refresh token for an access token using
  Google's token endpoint.
- Build an RFC 2822 text MIME message.
- Base64url encode the MIME content into the Gmail `raw` message field.
- Call `users.messages.send` with user id `me`.

Required server config/secrets, once real Gmail setup is approved:

- `RECOVERY_EMAIL_PROVIDER=GMAIL`
- `GMAIL_RECOVERY_SENDER_EMAIL`
- `GMAIL_RECOVERY_OAUTH_CLIENT_ID`
- `GMAIL_RECOVERY_OAUTH_CLIENT_SECRET`
- `GMAIL_RECOVERY_OAUTH_REFRESH_TOKEN`

The adapter does not store raw Google access tokens, refresh tokens, client
secrets, full Google claims, player passwords, or raw Authorization headers in
D1, logs, responses, Unity diagnostics, or Admin responses. Gmail send is not
retried automatically inside the adapter, because a timeout after an accepted
send can otherwise create duplicate email risk. Route-level resend cooldowns
and verification attempt limits remain the control boundary.

Gmail API accepting `users.messages.send` only means Gmail accepted the send
request; it is not proof that the player received, opened, or completed the
verification. Verification succeeds only when the player submits the separate
server challenge reference and one-use verification code.

Provider failure mapping:

- missing config: `gmail_config_missing`, surfaced to player routes as
  `recovery_email_service_unavailable`
- OAuth token endpoint unauthorized: `gmail_oauth_unauthorized`
- OAuth token endpoint unavailable/invalid payload: `gmail_oauth_unavailable`
- Gmail send unauthorized: `gmail_send_unauthorized`
- Gmail quota/rate limit: `gmail_quota_exceeded`
- Gmail send transient/unavailable: `gmail_send_unavailable`

Current real-connection wait:

- choose the sender Gmail/Workspace account
- create/confirm Google Cloud OAuth client for the sender integration
- complete OAuth consent for the Gmail send scope
- inject the Gmail recovery secrets into the target environment
- run real-send smoke with an approved test recipient

Do not configure or test real Gmail sending until those values and approvals are
provided.

<a id="source-6f64a90df51fe75e-recovery--consent-decision-queue"></a>
#### Recovery / Consent Decision Queue

Not finalized:

- Terms/privacy consent storage and minimum signup-complete gate.
- Whether account switch requires re-authentication immediately before confirm.
- Same recovery email on multiple accounts: currently allowed by the draft
  schema to avoid treating email equality as identity/merge truth; decide
  whether to keep this or add a stricter policy.
- Account lookup UX: whether players start recovery from `loginId`, recovery
  email, or both, while preserving account-enumeration resistance.
- Future email provider fallback: Cloudflare Email Service Workers binding
  remains a platform-native candidate if Gmail quota, deliverability,
  operations, or scale needs make a provider change worthwhile.
- Whether to keep WebCrypto `PBKDF2_SHA256` for launch or approve a stronger
  Workers-safe password hashing service/library before production.

Do not claim the ID/password system is production-ready until these gates are
resolved.

<a id="source-6f64a90df51fe75e-unity-contract-notes"></a>
#### Unity Contract Notes

Unity may implement local UI/state for:

- guest restore first
- login selection
- link status display
- conflict preview display
- keep-current vs switch-to-target confirmation
- secure session owner replacement after successful confirm

Unity must not:

- generate account IDs
- infer signup completion from nickname alone
- discard a stored owner account or create a replacement guest after timeout,
  offline, 5xx, app restart, or response-loss uncertainty
- merge accounts
- calculate story/reward/economy state
- store raw passwords or Google ID tokens long-term
- replace the active local session from preview alone

<!-- source-body:source-6f64a90df51fe75e:end -->


<a id="source-a146c4a6c5edf152"></a>
## Effect feedback: proposed integration contract

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/ce03-effect-feedback-contract.md`  
원문 SHA-256: `466d07a81bb1668e119064cce65f6bf0c60e4a7a0a4e14ef4f565668e80c8bad`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-a146c4a6c5edf152:start -->
<a id="source-a146c4a6c5edf152-effect-feedback-proposed-integration-contract"></a>
### Effect feedback: proposed integration contract

Status: coordination input for Unity, not an implemented wire version or readiness claim. Existing r6 evidence must not be reused after producer/renderer/resource changes. The W01/W02 source freeze starts only when a new complete packet and canonical hash are explicitly handed over.

<a id="source-a146c4a6c5edf152-observed-producer-boundary"></a>
#### Observed producer boundary

Unity `BasicCombatSimulation.ResolveBasic` and `CombatSkillCommands.ApplySkillEffect` append `BasicCombatHit` with Tick, Source, Target, HpLoss, RemainingHp and Absorbed. `CombatLocalCheckpoint` restores those fields. Those records cannot independently identify ability/effect index or distinguish heal, repair, shield issue, status refresh and interruption. Node `ce03-support-effects.ts` currently saves command-level APPLIED/FIZZLED/CANCELLED receipts with resulting state; APPLIED also includes effects with zero realized amount. Neither source authorizes inferred positive feedback from HUD deltas or command status alone.

<a id="source-a146c4a6c5edf152-proposed-minimum-result-envelope"></a>
#### Proposed minimum result envelope

Bind results to the accepted run and immutable profile identity. A stable identity is the tuple `(runId, profileSha256, globalSequence, commandId, abilityId, effectIndex, targetId, pulseIndex, resultKind)`. Preserve tuple fields or encode them canonically without ambiguous delimiter concatenation. Global sequence comes from the engine allocator; do not substitute the support-local receipt counter. One result identity must never acquire another payload after replay.

Include simulation tick, sourceId and a discriminated result payload emitted where the effect commits. For basic attacks, use an explicit basic-attack source kind/identity; do not invent an ability catalog ID. Final exact field names and schema version require Unity producer alignment.

| Result kind | Actual commit evidence | Presentation gate |
| --- | --- | --- |
| DAMAGE | hpLoss and absorbed, separately nonnegative | HP damage feedback only for hpLoss>0; shield absorption feedback only for absorbed>0 |
| HEAL / REPAIR | actualHpGain after missing-HP/per-effect clamp | Positive gain only, keeping healing and train repair distinct |
| SHIELD_ISSUE | previousRemaining, resultingRemaining, actual added amount, layer key, expiry | Do not label remaining total as newly granted shield; distinguish amount increase from expiry-only refresh |
| STATUS_ADD / STATUS_REFRESH | category, source key, actual magnitude, previous/new expiry, accepted state change | No positive feedback for ignored weaker replacement or unchanged state |
| INTERRUPT | actual cancelled action ID and actual cancellation result | No success feedback for immunity, disappeared action or replacement action |
| STUN | accepted control-state change and expiry | No success feedback when immunity rejects the control component; barrage damage remains separate |

CAST_STARTED and cooldown/channel updates can drive preparation feedback. They do not prove an effect landed. Cancellation, fizzle, full HP, complete damage protection and zero shield room must not generate fabricated positive effects. An absorbed hit may have hpLoss=0 and still legitimately produce shield feedback.

<a id="source-a146c4a6c5edf152-persistence-and-presentation"></a>
#### Persistence and presentation

Save authoritative results with effect changes and gameplay applied receipts in the same battle checkpoint transaction. Presentation maintains its own last-consumed result identities/cursor so restore does not replay past one-shot audio/VFX. Presentation acknowledgment must never claim gameplay effects or resources. Skip/volume/mute/full-versus-simplified modes may change presentation only; they must not alter gameplay event order, damage, cooldown, seed, finish or claim.

Project resultKind plus approved ability/effect identity to the actual effect sound/visual mapping. Do not pass credentials, player account data or arbitrary asset paths through this envelope. Audio duration, gain and visual timing are presentation inputs, not gameplay durations. Missing mappings remain unconnected; they do not authorize guessing effect type from a display name.

<a id="source-a146c4a6c5edf152-required-comparison-cases"></a>
#### Required comparison cases

Positive/zero heal; repair versus heal; hpLoss-only, absorption-only and fully protected damage; shield addition versus refresh versus cap rejection; accepted status versus ignored weaker effect; actual interrupt versus immune/disappeared/replaced action; barrage damage with immune stun; duplicate/restart replay with no repeated one-shot; identical gameplay results across display/skip/audio modes.

Node effect vectors from commit 2a39ef6 remain NODE_EFFECT_PHASE_ONLY/unityExecuted=false. This document does not change them or claim they contain the proposed typed envelopes. Unity implementation, serialized schema tests and actual result comparison are still required.

<!-- source-body:source-a146c4a6c5edf152:end -->


<a id="source-c1986fabaad26c8b"></a>
## Explicit frontline-first prepared profiles

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/ce03-frontline-first-v1.md`  
원문 SHA-256: `2c932d069a5d13c96c9903714a6a950aa0f35308ae38b5a000b5af5497aeac3f`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-c1986fabaad26c8b:start -->
<a id="source-c1986fabaad26c8b-explicit-frontline-first-prepared-profiles"></a>
### Explicit frontline-first prepared profiles

New preparations can opt into enemy ordinary basic attacks targeting eligible front-row operators before rear-row operators. Valid taunt precedence, logical slot/ID/cursor ordering, recovery timing, reserved attacks, explicit skills and exterior targeting retain their existing rules. This includes ordinary `rear_ally` enemy basics; those labels are not special rear-targeting skills. The producer keeps front slots `[3,4]` and rear slots `[0,1,2]`.

The complete normative override text is `frontlinePolicySource` in `apps/game-api/src/ce03-targeting-policy.ts`; its exact UTF-8/LF bytes are pinned in new draft `sourceFiles` as `frontlineFirstPolicyV1`. Existing R1/R2 inputs remain present.

| Field | New value |
| --- | --- |
| `template.execution.targetingPolicyId` | `ce03.frontline-first-v1` |
| `simulationVersion`, `executionPolicy.version`, `template.execution.policyVersion` | `ce03.execution.2026-09-13.001` |
| `executionPolicy.targetTiePolicy` | `ce03.frontline-first-v1` |
| `template.execution.policySha256` | `08cd3350fde47c5d819836bf6c6be981467e01ea5f814ee9cd56a42e17d0f052` |

All remaining execution-policy fields, including `interruptPolicy=P3`, are unchanged. Missing or mixed new identities and altered new policy stages are rejected. Undefined targeting options are normalized to field absence before canonical hashing.

Pass `{ targetingPolicyId: "ce03.frontline-first-v1" }` as the third argument to `prepareLocalCombatDraft`, or include that field in trusted `registerLocalCombatProducer` / `createCe03LocalHost` construction options. Each registered host captures the option at construction. Its normal `/__local/ce03/prepare` route produces the selected policy; request bodies cannot switch policy. Omitting the option preserves the old P2 policy, version, hash and exact absence of the new field.

`ce03-command-contract-v1` and existing profile identifiers remain unchanged: `prepareSha256` already binds the entire draft and command contract. Policy selection therefore changes the prepare hash. Existing accepted envelopes, old fixture files and hosts must not be reinterpreted as the new policy. No mutable targeting setter is needed in Unity; its adapter validates the exact old/new identity during fresh model construction and uses separate checkpoint identity with strict cross-policy rejection.

Run `tsx --tsconfig tsconfig.base.json scripts/validation/verify-ce03-frontline-policy.ts` to create a new disposable prepared fixture under an exclusive `audit-results/ce03-frontline-policy-*` directory. It uses the normal host prepare path, retains command v1, reads back every output file, asserts no DB change during prepare and creates no run. The support selection setup is an explicit write in that new in-memory test DB. The fixture contains no presentation manifest or Unity execution proof and is not ready for admission. This command does not alter the historical `audit-results/ce03-local-producer/draft.json`.

The persistent host CLI and approved presentation projector remain unchanged. Selecting the new policy in a future live host composition still requires the corresponding reviewed Unity producer/adapter and presentation compatibility; this change performs no deployment, admission, START, finish or claim.

<!-- source-body:source-c1986fabaad26c8b:end -->


<a id="source-24f6e160d31e880b"></a>
## Disposable local admission and support selection

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/ce03-local-host.md`  
원문 SHA-256: `a4b3957ba788e7364ba5dc878b177cacb90d843a9b20e8f434d07182ca56ff0d`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-24f6e160d31e880b:start -->
<a id="source-24f6e160d31e880b-disposable-local-admission-and-support-selection"></a>
### Disposable local admission and support selection

This is implemented separately from the public game API. `createCe03LocalHost`
in `apps/game-api/src/ce03-local-host.ts` constructs an independently authenticated
Hono app bound to one exact disposable database object and an explicit owner
allowlist. Public `/v1/runs/start` is not mounted here and its production gate is
unchanged. Only the trusted composition can provide consumer artifacts/verifier.

The launcher `scripts/validation/serve-ce03-local-host.ts` creates a fresh in-memory
fixture account and listens only on an OS-assigned `127.0.0.1` port. Its local
schema is installed explicitly, outside production migrations. It never installs
an accepting verifier. Default mode writes an eight-hour local bootstrap token
to `audit-results/ce03-local-host/connection.private.json`, never to logs. Account
and DB lifetime is the process; this launcher is not crash-durable storage and
must not be treated as a resumable real-account service. Existing transaction
and recovery behavior is tested against the same DB across host requests.

From `server`:

```text
tsx --tsconfig tsconfig.base.json scripts/validation/serve-ce03-local-host.ts --self-check --presentation-audit <actual-export.json> --unity-project <project-root> --ugui-package-root <resolved-com.unity.ugui-root>
tsx --tsconfig tsconfig.base.json scripts/validation/serve-ce03-local-host.ts --presentation-audit <actual-export.json> --unity-project <project-root> --ugui-package-root <resolved-com.unity.ugui-root>
```

The actual Approved view is the launcher's default contract. All three source
arguments are required. Historical regression must explicitly use `--legacy-r4`
(optionally with `--self-check`) and cannot combine presentation arguments.
Self-check exports go into a fresh `self-check-*` directory, preserving earlier
cited artifacts. The launcher checks actual raw/meta/GUID/source identities before
opening its listener. This is preparation evidence, not executed consumer proof.

The self-check starts real HTTP, verifies unauthorized rejection, explicit support
save/prepare/quote and missing-evidence refusal, exports artifacts and closes.
It issues no combat seed/run and does not debit fuel. Normal mode leaves the
host available for independent integration, with actual admission still closed
until a real provider is composed in code. No HTTP body may supply a verifier,
effect, seed, profile or ready flag.

<a id="source-24f6e160d31e880b-support-selection"></a>
#### Support selection

`GET /__local/ce03/support-loadout` returns `{selection,catalog}`. Missing selection
is revision0/empty slots, not an automatically accepted former fixed trio.
Catalog scope is `LOCAL_SELECTION_ONLY`; `executionAvailable` distinguishes the
existing three effects from unresolved roles. No permanent unlock/economy rules
are assigned.

`POST /__local/ce03/support-loadout`, with the original `Idempotency-Key`:

```json
{"rulesVersion":"ce03-support-selection-v1","catalogVersion":"ce03-local-support-roles-v1","expectedRevision":0,"slots":[{"slot":0,"skillId":"barrier"},{"slot":1,"skillId":"support_fire"},{"slot":2,"skillId":"emergency_repair"}]}
```

Exactly three distinct, contiguous, explicitly ordered slots are required.
Saved revisions use CAS; active runs prevent edits. Same key/body replays the
original saved selection even after a newer save; changed body/order conflicts.
Owner, catalog/version, unknown IDs, duplicates, missing slots and client fields
are checked. Selection write, immutable receipt and guards share one SQL batch.

The three actual ability IDs retain their source identities:
`support_fire`, `barrier`, `emergency_repair`. The following seven are **new local
storage keys** for accepted roles, not previously existing Unity ability IDs:

| Local key | Accepted role | Execution |
|---|---|---|
| `ce03.role.suppressive_barrage` | 제압 포격 | Unavailable |
| `ce03.role.medical_support` | 의료 지원 | Unavailable |
| `ce03.role.smoke` | 연막 전개 | Unavailable |
| `ce03.role.weakness_scan` | 약점 스캔 | Unavailable |
| `ce03.role.firepower_boost` | 화력 증폭 | Unavailable |
| `ce03.role.electronic_disruption` | 전자기 교란 | Unavailable |
| `ce03.role.emergency_protection` | 긴급 방호 | Unavailable |

All ten can be saved in this local selection registry. A selected unresolved
effect causes `ce03_support_effects_unavailable` on prepare/start, rather than
substituting an old effect or zero-valued coefficient.

<a id="source-24f6e160d31e880b-prepare-and-command-evidence"></a>
#### Prepare and command evidence

The new host accepts `POST /__local/ce03/prepare`:

```json
{"selection":{"operationId":"opn_01_last_departure","risk":"I","expectedFormationRevision":1,"expectedLoadoutRevision":1,"expectedUltimateRevision":1},"expectedSupportRevision":1}
```

Response fields are `ready:false`, `status:MISSING_EXECUTION_EVIDENCE`,
`ownedSelection`, `carriageOwnership`, `draft`, `commandContract`, `prepareSha256`.
The old standalone producer's request remains unchanged. This host's explicit
wrapper is a new contract. Prepare hash covers the complete four data objects;
it pins selected owner facts/revisions/order and command semantics. The draft's
train manual order follows saved IDs; effects come only from the pinned source.

`commandContract.version` is `ce03-command-contract-v1`; `support` contains the
saved rules/catalog/revision/slots; `supportChannel` is `SHARED_TRAIN_MANUAL_CAST`.
`leader` is explicitly configured as:

```json
{"version":"basic-hp-hit-v1","scope":"LOCAL_QA_OPT_IN","perHit":4,"overrides":"ULTIMATE_FIRST_READY_AND_COOLDOWN"}
```

This is the existing Unity opt-in QA behavior, not approved release balance.
Observed source: living allied operator basic HP-damage event adds4, cap100;
enemy/train/device/skill/zero-HP-loss excluded; valid ultimate cast consumes100;
down retains charge. Consumer execution/checkpoint rules and event ordering must
be verified against its exact code, including interrupted casts and restore.
The wrapper overrides the historical ultimate timing fields; a consumer must
not use the nested r4 base alone to execute this r5 contract.

New `ce03-consumer-runner-report-v2` requires the previous report fields plus
`commandContractSha256` and `SUPPORT_SELECTION`/`LEADER_CHARGE` coverage in
addition to all eight full-consumer categories. The exact command JSON/hash is
passed to the trusted verifier. An old report, partial test count, changed
selection revision, different gauge config or accepting fixture is not actual
execution evidence. Missing artifacts/verifier and failed verification reject
before generating a combat seed or changing run/fuel state.

<a id="source-24f6e160d31e880b-quote-accepted-start-and-recovery"></a>
#### Quote, accepted start and recovery

The existing authenticated `POST /v1/operations/preview` is also available inside
this isolated host. Its body is the existing departure selection request and it
uses a separate original preview key. It does not imply combat readiness.

`POST /__local/ce03/start`, with a separate original `Idempotency-Key`:

```json
{"operationPreviewId":"<actual quote ID>","expectedSupportRevision":1,"prepareSha256":"<exact prepare hash>"}
```

On actual verification, accepted response includes existing run metadata and:

- `scope:DISPOSABLE_LOCAL`, `accepted:true`, `startStatus:STARTED`.
- `combat.schemaVersion:ce03-accepted-combat-v1`.
- `combat.profile`: `ce03-isolated-command-profile-r5` containing the historical
  closed `baseProfile` plus the new `commandContract`.
- `combat.profileSha256`, `combat.contentManifest`, `combat.contentSha256`.
- `combat.seed`: Web Crypto uint32 little-endian original-run seed descriptor.
- `combat.snapshot` and `combat.snapshotSha256`: owner/run/quote, prepare/P/C
  hashes, seed, original owner/carriage/command facts and verified evidence.

Hashes exclude their own fields: P hashes profile, C hashes content manifest,
S hashes snapshot. The content manifest retains its closed r4 locator shape and
pins the entire r5 profile hash. Snapshot is persisted with the full original
response through existing `startDeparture`. Its existing owner/quote/open-run/
balance/fuel guards, debit, run, snapshot and idempotency writes plus new support
guard/immutable local receipt commit together or roll back. Candidate seeds from
failed transactions are never published; original committed-key replay does
not regenerate a seed or rerun consumer verification.

Original receipt lookup precedes current owner revisions and provider readiness.
The receipt is bound to owner and exact start request hash. Receipt bytes/hash,
run ownership and stored snapshot must agree. Conflicting original keys reject.
Concurrent completion, including between initial receipt and idempotency reads,
and lost commit acknowledgement recover the committed original response.

`POST /__local/ce03/start-result` takes the same start body plus
`startIdempotencyKey` in the body and returns `lookupStatus`, `startResponse`,
`newAttemptAuthorized:false`. It never creates a new run. Existing
`/v1/runs/:runId/lifecycle`, finish, reward-preview and explicit claim routes are
forwarded only for the authenticated owner's locally admitted run. Reads do not
grant rewards, and no ordinary START route is forwarded.

<a id="source-24f6e160d31e880b-evidence-limits"></a>
#### Evidence limits

`audit-results/ce03-local-host/prepare-v5.json` and `verification.json` come from
the real loopback self-check with no installed verifier. The separately named
`synthetic-exchanges/<runId>.json` comes from the integration test's synthetic
verifier and is request/response wiring evidence only. Its flags explicitly say
no actual consumer verification/battle. Do not use it to enter Unity combat or
claim full conformance. Real provider, complete consumer support for this r5
wrapper, crash-durable host storage and actual local battle QA remain separate
integration work.

Verification completed 2026-09-12: related37PASS; full90files612testsPASS23.93s;
game-api/async-worker TypeScript and full source ESLint PASS. The original-key
two-read race was reproduced RED and fixed with a passing regression. A recreated
host over the same DB also recovers the exact original response without an
installed provider. Independent bounded review found no remaining high-impact
issue after the race fix. Real loopback self-check passed401 unauthorized,
200 support-save,409 missing consumer evidence, zero runs and fuel30 unchanged.

<a id="source-24f6e160d31e880b-executed-runner-provider-adapter"></a>
#### Executed runner provider adapter

`scripts/local/ce03-executed-runner.ts` now provides
`executeCe03ConsumerRunner(options)` for a trusted Node composition. It is not
automatically installed in the launcher. Options are an absolute reviewed
`executable`, `cwd`, `sourceRoot`, a complete explicit `sourceFiles` relative-path
list, `outputRoot`, the complete `prepare` context below, `buildArgs(paths)` and an
optional bounded timeout. HTTP clients cannot supply these options.

The helper constructs new `outputDirectory`, `sourceBundlePath`, and `preparePath`
for each process. `buildArgs` receives those three paths. Unity's actual entry
method and flags must be supplied after that runner is implemented/reviewed;
there is no guessed executable or accepting fallback. Execution uses no shell
and hides its window. A failed launch, nonzero exit or timeout rejects.

The source bundle uses canonical JSON:

```text
{schemaVersion:"ce03-source-bundle-v1",files:[{path,sha256,content},...]}
```

Paths are ordinal sorted project-relative forward-slash paths. Duplicate paths,
parent traversal, symlink escape, invalid UTF8 and oversized bundles reject.
Content preserves exact UTF8 text including original line endings/BOM. The
source count limit is 1024 (increased from 512 after Unity reported 548 runtime
sources in its single assembly). The existing total canonical bundle limit of
8,000,000 characters and per-source 8,000,000-byte limit are unchanged. A larger
file count does not authorize omitting dependencies or exceeding size bounds.
The explicit list must include the runner, engine, commands, adapter, checkpoint,
journal and relevant dependencies. Source inclusion/completeness and the actual
runner assertions require review; spawning any arbitrary program is not proof
of conformance. The runner must execute the corresponding current compiled code.

Runner output in the supplied new directory:

- `algorithm-source.json`: exact bytes of `sourceBundlePath`.
- `checkpoint-schema.json`: actual checkpoint implementation/schema artifact.
- `vectors.json`: actual executed cases with unique IDs/coverage/inputs/expectations.
- `runner-report-v3.json` for the Approved view; `runner-report-v2.json` only for
  historical command inputs without presentation. Strict canonical report emitted
  after the actual required full run succeeds, never copied from an earlier run.

On zero exit, the helper checks source and executable hashes again, requires all
four new outputs and exact source-bundle equality, then returns immutable
`artifacts`, `verifier` and an execution record. The verifier compares the exact
artifact strings/digests and prepare draft/command identities and rechecks source
and executable identity on every verification. A source edit invalidates it.
Compose these artifacts/verifier into `createCe03LocalHost` over the same DB;
the host still separately enforces report-v2/full coverage and original prepare
identity. A saved arbitrary JSON report cannot construct this verifier.

Current proof is **synthetic external process plumbing only**: actual Unity
`Ce03ConsumerEvidenceRunner.Run` and complete report-v3 are still being authored
by Unity. No real provider has been executed or installed, and this helper's
tests do not justify enabling battle entry.

Adapter verification: missing-module RED then focused process/host14PASS;
full91files619testsPASS22.54s after splitting five sequential process scenarios
into independent cases (no timeout increase). Node/Worker type check and targeted
ESLint PASS; independent bounded review found no high-impact bypass. Cases cover
actual synthetic subprocess exit failure/timeout, missing output, changed source
during/after execution, changed bundle/draft/command evidence and invalid paths.
The former fixed synthetic-exchange output was regenerated by regression runs;
new runs now write unique run-ID files to preserve previously cited samples.

<a id="source-24f6e160d31e880b-approved-view-profile-r5--content-r6--report-v3"></a>
#### Approved view: profile-r5 / content-r6 / report-v3

Historical closed r4 profile/content/binding schemas and old receipts are unchanged.
The r5 profile wrapper and command/leader semantics are unchanged. The new content
header is `ce03-isolated-presentation-content-r6`, with exactly:

```text
{schemaVersion,version,profileSha256,resolver,legacyBindings,presentationManifest}
```

`version` is `ce03.approved-presentation.20260912.001`; `resolver` remains
`{mode:"UNITY_EDITOR_ASSET_DATABASE",playerBuildAllowed:false}`. `legacyBindings`
contains the original 226 Sprite/AnimationClip/AnimatorController bindings used by
the base profile. These are a different inventory from the new 356 typed rows;
the two counts must not be presented as a before/after runtime asset count.

`presentationManifest` uses `ce03-presentation-manifest-v1` and renderer
`approved-combat-view-v1`. Its closed fields are `schemaVersion`, `rendererVersion`,
`roles`, `bindings`, `sourceFiles`. Exact executable schemas are in
`apps/game-api/src/ce03-presentation-manifest.ts`.

- `roles`: configuration (CombatPresentationAssets), font (TMP_FontAsset),
  console/icons/map/trainExterior/interior/defaultTurret/turretRig/buggy/bike/hit/
  skill/shield (Texture2D), frame/primary/secondary (Sprite), six actorAtlases
  `{actorId,bindingId}` and 160 frames `{key,bindingId}`. Lists sort by ordinal
  actorId/key after mapping actual serialized array indices. Same asset may serve
  multiple roles, e.g. frame and secondary.
- Each binding retains the original locator/raw/meta/dependencies structure and
  extends the closed type set to Texture2D, MonoScript, Material, Shader,
  CombatPresentationAssets, Font and TMP_FontAsset. Decimal signed int64 file IDs
  remain strings. Actual importerVersion is `null` because the exporter did not
  provide one. `loadVerified:true` is owner-generated Editor evidence, not a claim
  that this Node projection executed AssetDatabase.
- Binding IDs are `binding.render.<guid>.<localFileId>`, sorted ordinal. Dependencies
  include all typed rows of referenced physical files and sibling subassets.
  Duplicate locators, missing dependencies, unsupported types and malformed hashes
  fail. The projection also verifies role references and array indices against the
  raw hash-pinned approved.asset YAML and rejects extra/unresolved roles.
- Four source identities are mandatory: approvedCombatView, combatHudWidgets,
  combatPresentationAssets, combatWindowView. The process adapter requires their
  exact `Assets/_LASTRAIL/Scripts/Battle/<Class>.cs` paths and raw hashes in the
  actual source bundle, and checks current source bytes again after execution and
  on verification.

`scripts/local/ce03-presentation-projection.ts` resolves only configured project
Assets and explicit package roots. It checks actual raw/meta/GUID for 188 physical
files and the four renderer sources. The actual exported inventory has 356 unique
GUID/fileID rows: Texture2D183, Sprite165, MonoScript2, Material2, Shader1,
CombatPresentationAssets1, Font1, TMP_FontAsset1. All exported rows are preserved,
including the TMP Editor icon inventory entry. That icon is outside the root's
runtime dependency reachability. No claim that all 356 rows are used by the live
renderer is implied; the actual consumer must verify loading and typed roles.
Map/skin/turret appearance references do not establish account ownership.

The complete process input is:

```text
{ownedSelection,carriageOwnership,draft,commandContract,presentationManifest,prepareSha256}
```

The hash is SHA256 of canonical JSON of the first five fields; exclude ready,
status and prepareSha256 itself. Historical preparation uses the first four
fields without presentationManifest. The runner must consume the exact owner,
carriage revision/type and selection, never regenerate a fixture account.
`preparePath` preserves all these fields, and a hash mismatch rejects before
execution. The host repeats preparation against its current authenticated DB and
checks the original hash before verifying evidence or creating a seed.

Report-v3 retains every v2 field, changes `schemaVersion` to
`ce03-consumer-runner-report-v3`, and requires `presentationManifestSha256` and
`prepareSha256`. Manifest hash is canonical parsed manifest JSON, not pretty-file
SHA. Required coverage is the complete list:

```text
STRICT_PROFILE FULL_EFFECTS MANUAL_COMMANDS FIFTEEN_WAVES CHECKPOINT_RESTORE
DETERMINISM OWNER_ISOLATION JOURNAL_RECOVERY SUPPORT_SELECTION LEADER_CHARGE
PRESENTATION_BINDINGS
```

Each vector ID must have one matching PASS result/coverage; passed count must
match, failed/skipped must be zero, all 11 categories must be executed. Canonical
report bytes, draft/command/source/checkpoint/vector hashes and both new hashes
must match the trusted actual process verifier request. Old v2, partial coverage,
changed manifest or owner-context hashes reject before debit/run creation.
Shape/hash checks and synthetic tests never substitute for actual test execution.

First actual r6 HTTP preparation: `audit-results/ce03-local-host/self-check-KVf5I1/`
contains prepare-r6.json (raw SHA
`04D2E9D3D21203973B5FF53910AC7F261CA2B91ABFE792C48B9A706B4197E24E`),
presentation-manifest.json (raw SHA
`5B23225BF4743E4AFA0AA293C24BF69F315BC8EC50E70111C80D1AB4DBF69135`), and
verification.json (raw SHA
`399319DE032EB7F247DC86ABEB1824282B4F3639F42E5B596979253C5A194BFC`).
Exporter raw SHA is
`c5dbcd758a4736c9136f92d320caf86d48fd623f9b73c83723a1e80c744081ea`.
This evidence is authenticated disposable preparation with 188 physical and four
source identities checked, 401 unauthenticated, START409 missing execution
artifacts, zero runs, no seed and fuel30 unchanged. Unity full r5/r6 consumer,
actual 11-category runner/provider and live battle evidence remain integration
work. No production deployment, remote DB change or public gate change occurred.

Final verification: 93 files / 640 tests PASS (17.51s), game-api and async-worker
types PASS, Node/Worker types including runner/projection/HTTP launcher PASS, full
ESLint PASS, independent bounded review found no high-impact issue. Regression
tests cover actual export corruptions, YAML role swaps, array index changes,
unknown type/overflow fileID/missing source/dependency, stale owner/presentation
attestation and missing renderer sources in the actual process bundle. The latter
was reproduced RED before enforcing source matching. Tests do not execute Unity.
Final actual HTTP reproduction is separately preserved in `self-check-AEk3o8/`:
prepare-r6.json raw SHA
`168D7C8122B9449BD3F4706A7E731E367B10C657F9877CC0B07E489D1AD564F9`.
Manifest and verification file hashes equal the first reproduction above. The
prepare's JSON property order changed with typed response validation; its canonical
prepareSha256 is unchanged. Explicit historical `--legacy-r4 --self-check` also
passed with no provider, zero runs and fuel30 unchanged.

<a id="source-24f6e160d31e880b-first-actual-runner-execution-review-remains-closed"></a>
#### First actual runner execution: review remains closed

`scripts/validation/verify-ce03-runner-closed.ts` preserves the first frozen
diagnostic packet, not an accepting provider launcher. It has no argument override
and pins source bundle SHA
`be341ad1bfe76ebe4828cf0a7b523c305f69407b12f1bd188215b57359327546`.
It starts a fresh authenticated disposable HTTP host, saves support and reads its
full prepare, executes Unity through `executeCe03ConsumerRunner`, then confirms
the same host still refuses START without a provider. It never submits the
runner's test-seed victory as an actual account result. Future source changes
intentionally invalidate this historical diagnostic; review a new packet for the
next integration unit rather than silently replacing its pin.

2026-09-12 actual execution: 554 sources, bundle 2,393,158 bytes, Unity 6000.3.22f1
executable SHA `36bafe49cfd65d4c91889167ca051f755195621a8f734392ad33ccccb5c1bba7`,
20.409 seconds, exit0, runner-reported11PASS. Artifacts are under
`audit-results/ce03-actual-runner/closed-review-ivKSvn/ce03-execution-KiWlUT/output`.
Verification file in the parent `closed-review-ivKSvn` has raw SHA
`29158BD00712A0FC14EB7D0B2AF36FA7126D7B9712AEAF744361BF373527CCD3`;
report canonical/raw SHA is
`fb2ca3d301285f102ca12b5c0d4e328125f1c24d3b65281c19d8d24927689c29`.
This is actual process execution, separately from the earlier synthetic processes.

Independent review still rejects treating that report as full admission proof:
the frozen Unity `Ce03AcceptedCombat` erroneously requires r6 content.version to
equal baseProfile.profileId, and its JOURNAL_RECOVERY case tests support saving
without testing START receipt/key/seed recovery. The runner does execute effect
value, waves, determinism, checkpoint and asset checks, but its accepted-envelope
path is absent. Both findings were sent to Unity, which is adding explicit r6
validation and START/accepted-consumer coverage. ProviderInstalled=false,
actualAccountRun=false, START409 missing artifacts, zero runs and fuel30 remain.

`ce03-local-host.test.ts` also exports clearly labeled
`synthetic-r6-exchanges/<runId>.json` fixtures containing prepare, start request,
original key and response. These use synthetic locators and verification, and are
only for parser/journal transport tests; never load them as actual battle content
or consume them as admission. The original sample is
`run_75f2337a-b608-4f5e-8c99-d53930107567.json`, raw SHA
`533D29EBE07DD2A8C1A3138D1D5031FCD51F6363E852417595E2E33FD9589E26`.

<a id="source-24f6e160d31e880b-actual-accepted-local-run--2026-09-12-1308-kst"></a>
##### Actual accepted local run — 2026-09-12 13:08 KST

`verify-ce03-actual-run.ts` is a frozen, explicit disposable integration executable.
It pins reviewed Unity packet runner-20260912T035826, 558 sources, canonical bundle
SHA `13ed4e80b15615290038885cf383133e62a911c9731dfaa5c6da92f8fd393f09`.
Independent review verified separate r6 version validation and concrete original
START recovery assertions. The synthetic transport fixture remains transport-only.
Fresh same-host prepare runs the real conformance helper before its returned
process-bound verifier is installed into that exact disposable DB/owner host.
The existing default launcher still has no accepting provider.

Actual execution evidence is `audit-results/ce03-actual-runner/actual-integration-3vHm27`:

- Fresh conformance exit0,11PASS,22.701 seconds; same Unity executable SHA as above.
- Actual START200 for `run_b209dfb4-a280-45c6-92b8-cf854544752a`, original issued seed2901650177.
- Original-key lookup and START byte replay preserve the accepted P/C/S and seed; one run and one fuel debit30→20.
- Actual consumer exit0, 15waves VICTORY, tick3851,1146events, durableRevision443; in-process checkpoint restore and final durable ACK checked.
- Original terminal outcome produces the sole UNSENT finish intent. Trusted server composition validates its run/seed/source/snapshot/model/result identities before using the existing HTTP sender.
- Finish200 then original-key DUPLICATE_REPLAY with unchanged run/preview/version identities. No reward transaction before explicit claim.
- Explicit claim200 then DUPLICATE_REPLAY with unchanged claim identity and exactly one reward transaction. Final lifecycle CLAIMED/RETURN_TO_LOBBY retains original START.
- Actual consumer plus subsequent HTTP assertions87.185 seconds; this is not a pure model performance measurement.

Evidence raw SHA256:

| File | SHA256 |
| --- | --- |
| verification.json | 9A7DE0DECAADB54A92FC5F6A52E45F94DA2F4F4C03F1056B245A5D9E0C63C7CD |
| actual-consumer/completion.json | 12664E8CB25270B7E14ECDBF44DBABF0CFCD7E44804ED817688847CEC374046A |
| start-response.json | C07B56D18D5EAEC99DB0B17A61D1DC6DF34774B9719ACD01B55A7FCFA3E23241 |

Existing finish/claim replay contracts intentionally change status/error fields;
only START uses byte-identical replay. Initial finish/claim raw responses are
retained separately from replay evidence and finish ACK. Review caught and fixed
an incorrect byte-equality assertion before execution.

Scope is a genuine simulated battle for a disposable fixture account, using an
actual locally issued seed and authentic loopback account authentication. It is
not a production account, remote deployment, normal login/lobby caller, Android
Keystore test, visual capture, or whole-process restart proof. Editor checkpoint
storage is explicitly plaintext. The host database lasts only this process and
is closed after the assertions. Consumer restart script indexing/evidence
preservation needs separate work before claiming process-restart equivalence.
The CLI source pin deliberately refuses future Unity source changes.
Validation: actual end-to-end execution above, Node/Worker typecheck, targeted
ESLint and independent source/orchestration review. Previous full640 tests and
host17 tests remain their earlier units; no full-suite rerun for this executable.
After execution only stdout was shortened to avoid printing the full lifecycle;
complete evidence remains on disk.
<a id="source-24f6e160d31e880b-persistent-host-for-normal-lobby-and-unity-restart-qa"></a>
##### Persistent host for normal lobby and Unity restart QA

The Node listener can stay alive while Unity closes, executes the conformance
runner, and reopens. This preserves the SAME in-memory DB, port, auth session,
original START receipts and rewards during a Unity process restart. Restarting
Node destroys that database; no server-process durability is claimed.

Five explicit GET forwards reuse existing authenticated handlers behind the
local exact-DB/loopback/owner allowlist: `/v1/account/state`, `/v1/wallet/crystals`,
`/v1/growth/config`, `/v1/trains/loadout`, `/v1/formation/ultimate`. All forwarded
responses receive `Cache-Control: no-store`. Public mutations and START remain
unmounted. No mission API was added; Unity uses its supported display row and
existing prepare/preview authority.

Optional `--lobby-bootstrap` initializes the existing `launch-economy-v1.json`
binding in the disposable fixture and a zero-balance `cur_crystal` read policy
with ticketExchange/purchase=null. This is explicitly different from the older
battle-only fixture, whose economy/wallet configuration remains absent. No
currency grant or production activation is performed. Actual response handlers,
rather than fabricated successful bodies, provide all five reads.

Persistent Approved launcher optionally takes both `--reviewed-source-files`
(absolute source-files.json path) and `--reviewed-source-sha256` (reviewed lowercase
canonical bundle SHA256). They cannot be used with self-check or legacy mode.
The caller must review the complete actual source packet before supplying them.
`executeCe03ConsumerRunner` now supports `reviewedSourceBundleSha256` and refuses a
different bundle BEFORE spawning; normal source/executable before/after checks
remain intact. Historical callers may omit the new optional pin.

Every launcher invocation receives a unique `session-*` directory, avoiding
private bootstrap overwrite. `connection.private.json` retains origin/accountId/
authorization/expiresAt/disposable/dataLifetime and adds sessionDirectory,
providerStatusPath and uiPreparePath. It never appears in reports or console.
Its initial consumerVerifierInstalled=false is a bootstrap fact; current state
is in provider-status.json and is ultimately enforced by START.

Trusted coordination protocol:

1. Unity normal UI saves selected support and obtains a full current prepare.
   Write that prepare JSON to `<sessionDirectory>/ui-prepare.json`, retaining its
   original owner/context; no credential belongs in this file. Close Unity.
2. Trusted server operator sends `attest` to the existing Node terminal stdin.
   This is not an HTTP route, and a caller cannot install a provider by editing a
   marker. Concurrent requests are refused; previous provider is cleared first.
3. Node compares the UI prepare and all identity fields against a fresh prepare
   from the SAME host, runs the real helper against the reviewed source pin,
   validates full strict attestation, and only then installs the returned verifier.
4. Atomic `provider-status.json` reports WAITING_FOR_PREPARE, EXECUTING, VERIFIED
   or FAILED, with origin/accountId. VERIFIED includes prepareSha256,
   sourceBundleSha256, execution and sourceBundlePath. FAILED leaves no provider.
5. Reopen Unity using the same private bootstrap. Require marker/prepare/source
   identity agreement, then use normal quote/START. Reacquire a quote if expired;
   never change a submitted original START. A new support revision requires a
   fresh prepare/execution. Actual START rechecks current revision and source,
   even if an older VERIFIED marker is still visible.

The supported display operation and original formation/loadout/ultimate revisions
come from the explicit local fixture. This is not a general arbitrary-operation
runner or public provider-management API.

Verification for this unit: GET missing-route RED, missing local config503 and
forwarded no-store loss reproduced, then actual existing five GET200. Source-pin
pre-spawn mismatch RED then two focused PASS. Full93files645testsPASS21.08seconds;
Node/Worker typecheck, targeted lint, independent review found no remaining high
impact issue. Actual self-check evidence `self-check-OpLPz9/verification.json`
records5GET200/noauth401/START409missingartifacts/zero runs/fuel30. Real persistent
terminal `session-AsuD7w/stdin-failure-verification.json` records missingUIprepare
FAILED followed by accountGET200, no provider; test listener was then stopped.
The new positive persistent provider/UI/restart run awaits the next reviewed
Unity source packet. Earlier actual15wave/claim evidence remains completed and
is not being reclassified as normal UI or process-restart proof.

<a id="source-24f6e160d31e880b-opt-in-dynamic-resource-byte-pins"></a>
#### Opt-in dynamic resource byte pins

Add `--dynamic-resource-closure` (boolean, no value) to a reviewed persistent
Approved invocation. It requires the existing paired `--reviewed-source-files`
and `--reviewed-source-sha256`; self-check and historical legacy mode reject it.
The source list must include exactly the project-relative manifest path
`QA/Ce03CommandIntegration/dynamic-resource-closure.json`. The helper API option
is `dynamicResourceClosurePath` with that same relative path.

The manifest must be canonical JSON emitted by the reviewed Unity exporter:
`schema:ce03-dynamic-resource-closure-v1`, `unityVersion:6000.3.22f1`, and exactly
33 ordinal resourceKey rows. Each row has only resourceKey, path, type, guid,
localFileId, rawSha256 and metaSha256. The closed set is13 cast clips,18 effect
clips, `Audio/levels` and `EffectAudio/bindings`. Resource paths are fixed to
`Assets/_LASTRAIL/Resources/<resourceKey>.wav` for AudioClip or `.json` for
TextAsset; meta is path plus `.meta`. GUIDs are distinct32lowerhex, local IDs
canonical signed-int64 strings, hashes64lowerhex. Unknown/missing/reordered rows
or fields, mismatched types/paths and noncanonical JSON reject.

Only the manifest text enters the reviewed source bundle. Binary audio must not
be decoded as source text. New `scripts/local/ce03-dynamic-resources.ts` resolves
and pins all66 original/meta files, rejects paths escaping the real project root
and duplicate resolved files, and streams raw SHA256 comparisons. Checks occur
at pin creation, immediately before spawn, after successful execution and on
every verifier call. Resource/manifest/source/executable drift prevents provider
verification. Absence of this opt-in keeps the historical helper behavior.

These Node checks prove bytes and physical path identity. Actual Resources.Load,
GUID/localFileId/object type, importer semantics and complete resource usage are
still checked by the reviewed Unity loader/runner. New resource pins do not
extend r6 presentation roles or themselves prove consumer coverage. The next
complete Unity source/resource packet and actual execution remain required.

<a id="source-24f6e160d31e880b-trusted-terminal-finish-from-ui-output"></a>
#### Trusted terminal finish from UI output

The generic helper is `scripts/local/ce03-trusted-finish.ts` (`trustedFinishCe03`).
The existing persistent launcher holds its freshly reviewed execution and prepare
in memory after successful `attest`. In that **same still-running host**, use:

```text
finish "E:/Projects/LASTRAIL/unity-project/QA/Ce03CommandIntegration/<resume-output>"
```

The argument is one JSON-quoted absolute directory; prefer forward slashes.
This fresh UI finish path requires execution with `--dynamic-resource-closure`;
the held execution must report66 pinned resource files. Older unpinned sessions
cannot use the new finish command, while their historical runner behavior stays unchanged.
There is no new HTTP verification endpoint or standalone tool that trusts an
editable provider-status file. A missing verified session, concurrent command or
any running Unity Editor refuses processing. Inspection failure also refuses.
The guard conservatively requires all Unity.exe processes to have exited.

The directory must contain `original-start.json`, `model-final.json`,
`completion.json` (`ce03-actual-local-completion-v1`) and `finish-intent.json`.
Files must remain within that real directory, be valid bounded UTF-8, and remain
unchanged through final checks. The helper compares owner/origin/full original
prepare and exact START bytes against the host's immutable receipt, including
request and response hashes. Retained START's algorithmSourceSHA and completion
run/seed/snapshot/source/prepare identities must match the current held execution.
Raw model SHA, positive durableRevision, tick/wave/outcome and terminal train HP
are checked. Victory requires15 waves and positive trainHP; Failure requires0
trainHP. This is a terminal subset check, not independent full simulation replay;
the reviewed producer must still verify its durable checkpoint/model binding.

Unity absence and retained executable/source/dynamic resource identity are
rechecked before HTTP. The exact original unsent intent must target the same run,
contain ordinary Complete/wave0/outcome/completedWaves, empty tacticSelections,
ISO clientCompletedAt and runtimeStateJson identical to the completion **raw
text**. Reward fields or other body additions reject. No previous13ed hash or558
source count is hardcoded. `restoredDuringRun` is not treated as process-restart
proof; `process-restart.json` is a separate W02 audit input.

Only after validation, the helper POSTs existing finish with its original key and
body. Initial accepted200 or identity-matching DUPLICATE_REPLAY200 is accepted.
The launcher atomically renames a new `finish-ack.json` containing version
`ce03-trusted-finish-ack-v1`, runId, rewardPreviewId, key, body/source/model/completion
hashes and responseRaw. It also writes session `finish-status.json` with FINISHED
or FAILED. No claim is sent; explicit UI claim uses the returned preview identity
through the existing recovery UI.

On transport uncertainty or acknowledgment write failure, retry this command
with the same directory and unchanged original intent. Do not replace its key,
body or completion. A new attestation attempt clears the old in-memory finish
context. Restarting the Node process loses this in-memory DB/session; this flow
is for Unity process restart while retaining the Node host.

<!-- source-body:source-24f6e160d31e880b:end -->


<a id="source-e9b5514971aaad2e"></a>
## CE03 local producer stage 1

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/ce03-local-producer.md`  
원문 SHA-256: `39037437ffe8b0f00e5db8224972e7558b604e14ce27e3431b0802872f91e6fb`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-e9b5514971aaad2e:start -->
<a id="source-e9b5514971aaad2e-ce03-local-producer-stage-1"></a>
### CE03 local producer stage 1

The later isolated host and support/leader command contract are implemented in
[ce03-local-host.md](49_SERVER_API_CONTRACTS.md#source-24f6e160d31e880b). This document retains the standalone
stage1/r4 boundary; actual full-consumer evidence is still required for admission.

Implements the adopted `ce03.local.qa.20260911.001` selection using the consolidated
r4.1 field contract. This is local preparation, not accepted combat admission.

`ce03-local-producer.ts` verifies the exact raw SHA256 of the authored combat data,
Direction's adopted selection overlay and Unity's typed binding evidence. It
projects only the closed r4 fields, validates explicit actor/render roles and
dependency closure, resolves all15 mission1 waves and the complete selected kits,
train/manual/car devices, package and policy settings. No live catalog is changed.
Raw paths, importer metadata and code dependency evidence remain provenance through
the pinned source file; they are not extra executable Binding fields.

The source fixtures are immutable input copies under `tests/fixtures/ce03`:

| File | Raw SHA256 |
| --- | --- |
| combat-stats.json | 66451f6948acc0d112864aaad0d95a3f0673993612ce691825727d1c305e263f |
| selection.json | 5418b60c50d07b2ffdd1c9ae9616f27a1e27bac58d15e3f8263d7b7ccbc7fa91 |
| typed-bindings.json | 15560bb7cb54198cd282ed999ff62dc2d22bd07b44d8355a9664a624e5f4efec |

<a id="source-e9b5514971aaad2e-authenticated-disposable-preparation"></a>
#### Authenticated disposable preparation

`registerLocalCombatProducer` is deliberately NOT called by the shipping app.
Its sole endpoint is `POST /__local/ce03/prepare` on a separately composed Hono
instance. Caller composition must supply the exact disposable DB object, an
account allowlist and the existing authentication resolver. It verifies loopback
URL, DB object identity, actual token/session/account and allowlist. This is not a
network sandbox or authorization to mount the endpoint publicly. No persistent
listener or wrangler configuration is created by this slice.

The request is the existing closed departure-preview selection body:
`operationId`, `risk`, `expectedFormationRevision`, `expectedLoadoutRevision`,
`expectedUltimateRevision`. No client account ID, effective stats or ready flag.
`readLocalDepartureSelection` reuses existing ownership/loadout/open-run/balance/
fuel checks without creating a preview. The producer enforces the adopted actual
L1/P0/skill1/train1 fixture, five unique owned initial operators, five distinct
training core rows and ordered Combat/Gun. `skillRank` is exactly `skillTraining`.
An explicit ultimate is required. Before returning it rereads the selection and
rejects any changed source/revision; future start still needs its own atomic check.

Response status is `MISSING_EXECUTION_EVIDENCE`, `ready:false`, with `draft` and
the original `ownedSelection` separately. The draft's template intentionally
contains no run identity or ownership revisions; actual owner facts are retained
separately for the future accepted snapshot. Nothing here debits fuel, generates
seed, creates a run, returns a successful admission or grants a reward.

<a id="source-e9b5514971aaad2e-execution-evidence-and-serialization-boundary"></a>
#### Execution evidence and serialization boundary

`finalizeLocalCombatArtifacts(draft, undefined)` fails with
`ce03_execution_evidence_missing`. No code/conformance digest is filled from
candidate markdown or invented constants. Actual Unity code/runner/vector
attestation is still unavailable and `LOCAL_CONSUMER` evidence is not accepted by
this stage. This is the precise remaining integration input, not a renewed policy
approval wait.

The fixture-only positive path accepts executed test algorithm source bytes,
vector JSON and a zero-failure count under `DISPOSABLE_FIXTURE`. It computes
digests from those bytes and validates closed Profile/ContentManifest serialization.
Returned artifacts explicitly have `evidenceScope:DISPOSABLE_FIXTURE`,
`ready:false`, `frozen:false`, and **candidate** digest names. They cannot attest
to Unity combat or be used as actual consumer evidence. This function is not
exposed as an HTTP request and is not connected to start.

No seed/S snapshot is generated. Real P/C/S freeze and start/debit integration
remain the later stage after actual consumer evidence and atomicity verification.
Proposal policy references pin the selected semantics, not executable code.

<a id="source-e9b5514971aaad2e-server-owned-carriage-type-projection"></a>
##### Server-owned carriage type projection

The authenticated isolated prepare response additionally contains
`carriageOwnership:{rulesVersion:"train-loadout-v1",loadoutRevision,carriages}`.
Each selected carriage has `{carriageId,slot,level,revision,type}` with the local
closed type `Combat` or `Gun`. The producer joins the rechecked owned rows by ID
to the selected loadout and uses `train-loadout.ts`'s authoritative
`resolveCarriageType`, also used by existing loadout validation. It explicitly
converts `COMBAT`/`GUN` and checks the corresponding draft slot's type. No client
map, name parsing or array-position type inheritance is involved. Unknown types,
missing ownership, changed revisions and exchanged selected IDs are rejected.

The Unity caller can derive its required `trustedCarTypes` map from this response
by `carriageId`, matching the projection's loadout revision and each ID/slot/owned
level/revision to `ownedSelection`. Reject duplicates, missing rows or unknown
types. Preserve that association with the same response; a projection detached
from its authenticated response is not proof of ownership. The projection leaves
the existing draft and ownedSelection fields unchanged, and does not require a
second GET. Final atomic admission revalidation remains separate.

Projection verification: absent projection first failed the new regression;
focused producer/loadout/attestation24PASS; full88files593testsPASS20.02s;
game-api TypeScript and changed-file ESLint PASS. Distinct owned revisions7/9,
unchanged DB bytes, swapped selected IDs, missing ownership and caller-supplied
projection rejection are covered. Independent review found no high-impact issue.

<a id="source-e9b5514971aaad2e-full-consumer-attestation-interface"></a>
##### Full-consumer attestation interface

`ce03-consumer-attestation.ts` adds the internal
`attestLocalConsumer(draft, artifacts, verifier)` boundary. It does not register a
route or feed start/freeze. The trusted local composition must supply a
`ConsumerAttestationVerifier`; no default provider is installed. The provider must
verify the actual runner/code/vector/checkpoint evidence, not just return true
because JSON parses. HTTP clients cannot supply that verifier.

Required artifact strings are `algorithmSource`, `checkpointSchema`, `vectorsJson`
and `reportJson`. They are bounded and reject lone UTF16 surrogates. The algorithm,
vector and checkpoint digests are computed from their exact UTF8 bytes. A source
bundle may be represented as a stable textual archive; the real runner adapter
must validate its complete code identity. Markdown provenance is not execution.

The new runner handoff report is closed canonical JSON with fields:
`schemaVersion:ce03-consumer-runner-report-v1`, `scope:FULL_LOCAL_CONSUMER`, `runId`,
`simulationVersion`, `draftSha256`, `algorithmSourceSha256`, `vectorsSha256`,
`checkpointSchemaSha256`, `results:[{caseId,coverage,status:PASS}]`, `passed`,
`failed:0`, `skipped:0`. `draftSha256` covers the complete strict draft's canonical
JSON. Existing operation-journal keys/raw response strings are unaffected by this
new report serialization convention. Duplicate report keys reject.

The vector artifact must have `cases` with unique `id` and `coverage`. Every vector
must have one matching passed report result. Full coverage requires
`STRICT_PROFILE`, `FULL_EFFECTS`, `MANUAL_COMMANDS`, `FIFTEEN_WAVES`,
`CHECKPOINT_RESTORE`, `DETERMINISM`, `OWNER_ISOLATION`, `JOURNAL_RECOVERY`.
These categories describe the already-required complete consumer scope; counts
alone (including the current18 basic/vitals tests) cannot substitute for it.

After shape, complete-coverage and exact-hash checks, the injected verifier receives
one frozen request with the exact artifact strings and measured hashes. Rejection
or exceptions fail closed. A successful result still says `ready:false`,
`frozen:false`; it contains verified evidence digests, not a seed, accepted run,
frozen P/C/S or admission capability. No actual provider or full Unity evidence is
available yet. Test-only accepting/rejecting stubs verify this control flow only.

Errors distinguish absent artifacts, absent verifier, invalid report/draft,
incomplete coverage, changed evidence, rejected attestation and unavailable
verifier. Actual runner adapter/attestation and consumer integration remain later
work; this interface does not claim those have passed.

<a id="source-e9b5514971aaad2e-reproduction-and-verification"></a>
#### Reproduction and verification

From `server`, run the package's Vitest runner on
`tests/integration/ce03-local-producer.test.ts`. Seven tests cover real auth and
owner facts, no writes, explicit ultimate, stale/in-flight revisions, invalid
growth, wrong route host, missing execution evidence, altered sources, strict
numeric/effect schemas, signedInt64 locators and typed dependency/role mismatch.

Run `tsx --tsconfig tsconfig.base.json scripts/validation/verify-ce03-local-producer.ts`
to create a fresh in-memory DB with migrations0001–0035 and an explicitly named
Reina-ultimate automated fixture. It writes `audit-results/ce03-local-producer/`
`draft.json` and `verification.json`. No remote DB inputs, persistent account
state, remote credentials or real accounts are used. Missing execution evidence
and byte-identical DB state are asserted. No fixture execution-evidence hashes
are included in that actual draft export.

Verification for this slice: focused7PASS; full87files/586testsPASS20.45s;
game-api/async-worker TypeScript and source ESLint PASS. Independent bounded review
found no high-impact stage1 defect. No actual Unity battle/checkpoint/video test,
new migration, deployment, public-ready gate or real-account mutation.

Attestation interface verification: focused6PASS1.76s;
full88files/592testsPASS17.24s; game-api/async-worker TypeScript and source
ESLint PASS. Six tests cover absent providers/artifacts, partial coverage,
tampered evidence, verifier rejection/errors, immutable exact requests and
malformed/ambiguous reports. A slow element-wise DB assertion was replaced by
byte comparison; the final full run passed without increasing its timeout.
Independent bounded review found no high-impact defect. Accepting providers in
these tests are synthetic fixtures; actual Unity conformance remains unverified.

<!-- source-body:source-e9b5514971aaad2e:end -->


<a id="source-a9952f103a593941"></a>
## Individual ultimates and manual/AUTO control

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/ce03-per-operator-ultimates.md`  
원문 SHA-256: `e74a3b8e065718b60b182bc7beedd280e9cfdfe3548286c97907177d4dd9452a`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-a9952f103a593941:start -->
<a id="source-a9952f103a593941-individual-ultimates-and-manualauto-control"></a>
### Individual ultimates and manual/AUTO control

<a id="source-a9952f103a593941-current-approved-extension-general-active-always-automatic-003"></a>
#### Current approved extension: general ACTIVE always automatic (.003)

Select all three trusted options: `targetingPolicyId=ce03.frontline-first-v1`, `ultimatePolicyId=ce03.per-operator-ultimate-v1`, and `activePolicyId=ce03.general-active-auto-v1`. The third option requires the first two. This creates execution `ce03.execution.2026-09-13.003`, SHA256 `266d1e98ef39c5596f0f8cf9f1bd78ce77207c943d1f3192fe7e5b3fd2c9e8d4`, command `ce03-command-contract-v3`, and future accepted wrapper `ce03-isolated-command-profile-r9`. The prior .002/v2 contract below is retained unchanged for old preparations.

Command-v3 keeps v2 `support`, `supportChannel` and `ultimates`, and adds the closed `actives` payload from `generalActiveAutoPolicy` in `ce03-ultimate-policy.ts`. Its `enabled=ALWAYS` has no UI toggle. The upper `AUTO` controls only the five ultimates and starts OFF. Train skills remain manual.

The new command stage is `A1.manual-ultimate-auto-active-auto-commands`: queued manual commands first, then eligible ultimate AUTO when ON, then eligible general ACTIVE, followed by existing basic scheduling. Each automatic phase uses formation slot then ordinal actor ID order. Shared actor queue, casting, basic reservation and recovery locks prevent duplicate reservations. Preflight living/available caster, authored readiness and valid targets before enqueueing; skipped candidates consume nothing and emit no rejected journal entries. Earlier accepted casts remain intact. Manual ultimates received before the decision boundary take priority over both automatic phases; this does not preempt an already accepted cast.

General ACTIVE effects, first-ready/cooldown values, target checks, windup, recovery and CAST_ACCEPTANCE consumption stay authored. Actual cooldown timestamps persist across down/recovery and restore. Pause/terminal makes no automatic decisions. Share a deterministic checkpointed automatic ID sequence between both automatic phases. A separate .003 checkpoint identity must reject legacy/.002 restore; do not reinterpret prior saved state. Remove general manual buttons and display actual remaining cooldown immediately below world HP. Target absence must not invent a cooldown. Unity verifies target absence, down/recovery, pause/restore, manual and ultimate-AUTO contention separately from server contract tests.

Unity checkpoint naming agreed: .002 uses `ce03-headless-operator-ultimate-state-v1`; .003 uses `ce03-headless-operator-auto-state-v1`. Both have root `operatorAutomation={generalAuto,ultimateAuto,sequence,charge}`. `generalAuto` is a boolean fixed by policy (false for .002, true for .003), `ultimateAuto` is a run-state boolean defaulting false, `sequence` is a nonnegative signed-64-bit monotonic automatic command counter, and `charge` maps exactly the five formation actor IDs to integer0–100. Keep this separate from old shared `leaderCharge`. These are Unity checkpoint fields, not prepare-request fields.

The existing complete configuration digest additionally binds `{schema,perOwnBasicHpHit:4,generalAuto}`, retaining targeting and all prior combat configuration inputs. Restore checks exact fields/actor IDs/ranges, policy-fixed `generalAuto`, expected schema and configuration digest. `ConfigureLocalOperatorAutomation(bool)` is Unity's explicit local construction opt-in; it does not authorize restoration into another policy. Names/contract are agreed; actual serializer, strict restore and execution verification remain Unity implementation work.

New source pin `generalActiveAutoPolicyV1` is the UTF-8/LF `generalActiveAutoSource` text; it explicitly supersedes only the earlier source sentence keeping general ACTIVE manual. Other individual-ultimate rules remain unchanged. Generate a fresh read-only/no-run fixture with `scripts/validation/verify-ce03-general-active-auto.ts`.

For both new command generations, consumer report version is `ce03-consumer-runner-report-v4` without presentation or `ce03-consumer-runner-report-v5` with presentation. Existing report v1/v2/v3 remains for legacy drafts. Required coverage is the eight base categories plus `SUPPORT_SELECTION`, `INDIVIDUAL_ULTIMATES`, `ULTIMATE_AUTO`, `LEGACY_POLICY_BOUNDARY`; command-v3 additionally requires `GENERAL_ACTIVE_AUTO`, and presentation requires `PRESENTATION_BINDINGS`. Report shape/hashes/labels alone never prove execution: the trusted verifier must authenticate actual runner evidence for the exact draft, source, checkpoint and vectors. Existing verifier implementations do not automatically support these new reports.

<a id="source-a9952f103a593941-retained-ultimate-only-preparation-002"></a>
#### Retained ultimate-only preparation (.002)

Explicitly opt new preparations into `ultimatePolicyId: "ce03.per-operator-ultimate-v1"` together with `targetingPolicyId: "ce03.frontline-first-v1"`. Both fields belong in the third producer argument or trusted local-host construction options; they cannot be set by a prepare request. Omitted options preserve legacy behavior. `leaderPolicy` is unnecessary for the new host.

| Field | Value |
| --- | --- |
| Simulation/execution policy version | `ce03.execution.2026-09-13.002` |
| Execution policy SHA256 | `68beb942a695ea761aa0f2aaf5677d7eee603aeeb3de078ffca148a27d824b50` |
| Command stage | `U1.manual-auto-commands` |
| Command contract | `ce03-command-contract-v2` |
| Command policy | `ultimates.version=per-operator-basic-hp-hit-v1` |
| Template selected ultimate | `selectedUltimateOperatorId=null` |
| Future accepted profile wrapper | `ce03-isolated-command-profile-r8` (legacy remains r5) |

Command-v2 retains `support` and `supportChannel`; it has an `ultimates` object instead of `leader`. The complete closed payload is defined in `ce03-ultimate-policy.ts`. A v2 command cannot be attested with an old draft, or a new draft with missing/v1 commands. The full prepare hash binds all ownership, draft and commands. Existing profile/catalog/effective-stat identifiers and all authored ultimate effect/cooldown values are unchanged.

The normal new host `/__local/ce03/prepare` request contains `selection` with operation/risk, `expectedFormationRevision`, `expectedLoadoutRevision`, and the outer `expectedSupportRevision`. Omit `selection.expectedUltimateRevision`: old selected leader is only a nullable historical revision fact in the returned ownership snapshot. A null leader therefore succeeds only for the explicitly configured new preparation. Public/default departure gates and old START receipt replay are unchanged. Future admission still requires the appropriate versioned departure/consumer proof; this preparation does not establish START readiness or enable the existing public departure path for null-leader runs.

<a id="source-a9952f103a593941-effective-combat-rules"></a>
#### Effective combat rules

Every formation operator has its own 0–100 gauge. Only that surviving operator's resolved basic hit with actual `HpLoss>0` grants4; shield-only hits, zero damage, skills, train/device/enemy attacks and dead-source committed hits grant nothing. Preserve charge across down/recovery. Do not clone the former shared gauge into every operator.

All five ultimate abilities already exist in the template. Enable each for its own operator; preserve coefficients, targeting, windup and recovery. Existing charged mode replaces first-ready/cooldown checks: the authored15s/45s metadata remains, but **no new45-second AND gate** is introduced. Spend only the accepted caster's gauge once at `CAST_ACCEPTANCE`. Rejection does not spend; later cancellation follows the old accepted-cast behavior.

AUTO starts off for every new run. On each executed fixed tick, process queued manual commands first, then evaluate AUTO in formation slot/ordinal actor-ID order before basic scheduling. Each candidate must be alive and available, have100 charge and valid targets, and have no queued/casting command, basic reservation or recovery lock. Skip ineligible candidates without emitting rejected commands. Actor queue occupancy prevents duplicate manual/AUTO reservations. Use a deterministic checkpointed sequence for automatic command IDs. A manual input received after the current tick belongs to the next decision boundary.

Turning AUTO off stops future automatic choices and leaves accepted casts intact. Paused or terminal combat never auto-casts; changing the toggle does not execute a tick. General active skills and train manual skills remain manual.

<a id="source-a9952f103a593941-checkpoint-and-ui-implementation-boundary"></a>
#### Checkpoint and UI implementation boundary

Unity must bind the complete static policy into configuration identity and use a separate new checkpoint schema. Persist an exact actor-ID charge map, current AUTO flag and automatic command-ID sequence; validate numeric ranges, roster identity and old/new restore incompatibility. Do not reinterpret old shared `leaderCharge` or old accepted records. The UI displays each operator's gauge below HP and a distinct ultimate command affordance, removes the leader-only panel and exposes a dedicated ultimate AUTO toggle.

`individualUltimateSource` exports the exact UTF-8/LF normative override. New `sourceFiles.individualUltimatePolicyV1` hashes it; existing frontline/R1/R2 pins remain. Run `tsx --tsconfig tsconfig.base.json scripts/validation/verify-ce03-individual-ultimates.ts` to create a separate disposable fixture with no leader selected, no presentation manifest and no execution/admission assertion. It writes only to a fresh audit directory, checks legacy draft identity and byte-identical DB before/after prepare, and creates no run. Server tests verify the contract; Unity separately verifies gauge arithmetic, double clicks, AUTO/manual ordering, pause/restore, target loss/down recovery and legacy playback.

<!-- source-body:source-a9952f103a593941:end -->


<a id="source-dc491ec5031c2fcc"></a>
## CE03 support local v2 — first implementation unit

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/ce03-support-v2.md`  
원문 SHA-256: `de1082c810f498d45dc72831d05acdc785bc0dd0271ebc287f71f3a1d18f6e32`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-dc491ec5031c2fcc:start -->
<a id="source-dc491ec5031c2fcc-ce03-support-local-v2--first-implementation-unit"></a>
### CE03 support local v2 — first implementation unit

Version `ce03-support-local-v2.20260912.001`. Source: Plan §3.2 completion orders and combat detail §6, 2026-09-12. Local development only; the r6 producer, profile, START admission and Unity consumer are unchanged.

| Contract | Actual code in `apps/game-api/src/ce03-support-v2.ts` | Boundary |
| --- | --- | --- |
| Seven numeric definitions | `supportDefinitions` immutable rows | Data input for a future closed profile; not runtime capability evidence |
| Selected three from ten | `resolveSupportThree` | Exactly three unique IDs, selected order preserved. Legacy three supplied through the existing closed ability parser; unknown/missing keys reject. Does not insert ten rows into old template |
| TARGET_MAX_HP | `targetMaxHpAmount` | Exact BigInt intermediate, floor, heal 3/20 clamped to missing HP, shield 1/4; invalid HP rejects |
| Cast-start target snapshot | `pinSupportTargets` | Strict actor inputs and unique identity; returns copied entity/action pins, never mutates focus |
| Barrage | Interior enemies, focus then spawn order then ordinal ID, max three | Damage and stun application still requires new consumer |
| Medical/smoke | Living, non-returning operators | Medical rejects all-full; returned living protected actors are eligible because protection does not exclude them |
| Weakness/disruption | Explicit enemy ID in either lane | Disruption requires an interruptible, nonimmune skill/channel and pins action ID; basic attacks excluded |
| Protection | Exact HP ratio then slot then ordinal ID | Cast-start choice, no replacement |
| Commit pin check | `validSupportPins` | Removes invalid/dead/returning targets and replaced or immune actions. Does not cancel anything itself; input pins must be trusted checkpoint state |
| Damage source | `firepowerApplies` | Train basic and Combat/Gun device only; operator, support, heal, repair, fixed mechanic excluded |
| Effect lifecycle rules | `supportPolicies` | Versioned intended stage/cap/cooldown/refund policies; enforcement in the consumer is still pending |

Firepower pins the living TRAIN actor as the buff owner; the source predicate also covers carriage device damage. No separate carriage HP/target simulation is introduced here. World snapshots use strict `entityId,hp,maxHp,slot,spawnOrder,side,lane,returning,action`; action is null or `{id,kind,interruptible,immune}`. All integers are safe nonnegative integers (maxHp positive). Entity/action IDs must remain unique across the battle; do not recycle action IDs after restore.

Next unit must bind these helpers into a new closed full profile and command/checkpoint versions. It must define same-tick interrupt/pulse order, persist target pins and cooldown/channel/effect state, enforce deduplication and source-expiry/caps, then produce actual Unity conformance vectors. No provider may admit this module as an executable profile by its version or data rows alone. Existing r6 remains available only under its original proven contract.

Validation: `tests/integration/ce03-support-v2.test.ts` exercises target HP clamping/large-integer arithmetic, lane/focus selection, all-full/down/returning rejection, ratio ties, explicit action pins, changed actions, source filtering, and closed selection/world inputs.

<a id="source-dc491ec5031c2fcc-second-unit-closed-full-draft-and-command-checkpoint"></a>
#### Second unit: closed full draft and command checkpoint

`ce03-support-profile.ts` creates/parses `ce03-support-full-draft-r7`. It includes the entire validated baseline draft and its canonical hash, full projected template with exactly three selected legacy/new manual rows, immutable new support policy, and canonical `profileSha256` (hash excludes itself). Baseline execution policy/hash and every projected field are rechecked on parse. The template's old execution block remains the **baseline core scheduler**; the outer simulationVersion plus supportPolicy describe the extension. `ready:false` and `executionEvidence:null` are mandatory. This is a draft, not an accepted r7 runtime profile. It deliberately does not reuse old algorithm/runner evidence.

`ce03-support-state.ts` supplies strict `ce03-support-command-v2` and `ce03-support-checkpoint-v2`. The initial isolated state currently accepts three of the **new seven** roles. The full draft can describe mixed legacy/new selections; execution dispatch to the existing legacy consumer is still pending and must not be inferred from draft parsing.

State is bound to the expected profile hash and ordered selection. Each accepted command records its original body/key, start tick, sequence and target/action pins. Restore recomputes all cooldowns, shared-channel release, pending cast and due outbox from receipt history and rejects mismatches. Input command keys cannot be reused with another body. Replaying the original body returns its state without revalidating current targets or spending again. Invalid/unavailable commands leave the original input untouched.

One step is 50 simulation milliseconds. Medical starts a 600-tick cooldown, becomes due at tick7, and releases the shared channel at tick12; smoke may then start while medical remains on cooldown. The persistent outbox retains a single intent per accepted command even after restore. Its sequence is currently a **support-local sequence**; it must be mapped to the unified engine sequence before merging with basic attacks or pulses. Outbox presence is not effect application or acknowledgment. The downstream consumer must atomically save effect changes and an applied-intent cursor in the battle checkpoint; never apply the full historical outbox each frame.

Bounds: 10,000 accepted command receipts, 4,096 unique pins per command, and tick <=1,000,000,000. History exhaustion rejects further commands without cost. Checkpoint reconstruction is linear in history; this reference implementation has no production performance claim. Actor/action history is trusted battle data, not a client authorization payload. Profile binding alone does not authenticate a checkpoint.

Still pending: unified sequence integration and same-tick interrupt/pulse application, actual new effect/state expiry/cap execution, atomic applied cursor, Unity parser/consumer and conformance vectors, and executable profile attestation. No HTTP registration, START acceptance, remote deployment or r6 modification is included.

<a id="source-dc491ec5031c2fcc-third-unit-node-effect-consumer-and-reference-vectors"></a>
#### Third unit: Node effect consumer and reference vectors

`ce03-support-effects.ts` implements the **Node effect phase**, not the full Unity battle. `consumeSupportOutbox` binds the exact draft, support checkpoint and effect-frame profile hash/tick, requires an explicit command-key → engine-global-sequence mapping, and combines due supports with damage events in `(dueTick,sequence)` order. Mappings must come from the trusted unified engine queue and be saved when commands are accepted; this function never substitutes the support-local sequence. Missing mapping, late unprocessed effects, reordered/conflicting identities and mismatched clocks fail closed.

Actual execution now changes target HP, source-keyed shield/status layers and action identity for all seven roles. Each result frame includes the applied original event payload/identity and outcome in the same copied value as those changes. Serialized restore/replay cannot apply the same event twice. A failing batch leaves its input untouched. **The storage owner must atomically persist this whole value**; physical Unity checkpoint/CAS integration and whole-process durability are not established by this Node return-value test.

Damage uses BigInt through outgoing/mark/armor, then floors; smoke adds one post-armor floor before shields. Outgoing cap3000, strongest mark cap2000, effective armor cap6000, strongest smoke cap3000. BigInt remains in use through absorption and HP clamping, including maximum-safe input values. Firepower sums source values only for TRAIN_BASIC/COMBAT_DEVICE/GUN_DEVICE, then shares the outgoing cap; SUPPORT is excluded. Input `outgoingBps`/`armorBps` are already-resolved baseline core values, so importing package/passive modifiers remains the full-engine adapter's responsibility. `MECHANIC_FIXED` means a pre-shield fixed input that bypasses these numeric modifiers, not a new authored mechanic policy.

Shield layers preserve source/ability/effect-index identity, ignore weaker replacement, refresh equal/stronger, share target maxHP40% cap and absorb expiry-first then ordinal source/ability/index. Source status expiry is independent. Medical uses target maxHP15% with missing-HP clamp; protection uses25%. Barrage uses train ATK6/5, interior pins only/max3, and applies stun10ticks only to living nonimmune enemies. In this comparison kernel STUN gates uncommitted due damage during its active window; it does not retime/restart an action or implement actor command scheduling. That scheduling adapter still needs Unity integration.

Disruption clears only a matching interruptible skill/channel action ID. An earlier same-tick pulse stays applied; subsequent uncommitted pulses with that cancelled ID are cancelled. Explicitly committed impacts survive, including after source action/death changes. A replacement action cannot be cancelled by the old pin. All events for a simulation tick must reach this stage in global order; a late lower sequence is rejected.

Effect frame inputs are trusted simulation snapshots, not client-authorized stats or ownership. Full baseline operator/passive/device state import, mixed legacy ability dispatch, the actual Unity scheduler, Unity checkpoint store and new 15-wave conformance remain pending. No provider/readiness promotion is included; full draft still requires readyfalse/null execution evidence.

`ce03-support-effects.test.ts` exports seven reproducible input/event/expected-frame cases under `audit-results/ce03-support-effects/vectors-<uuid>.json`, with four module SHA256s. The artifact is marked `NODE_EFFECT_PHASE_ONLY`, `unityExecuted:false`. These are actual Node outputs for Unity to compare, not a Unity PASS report. Cases cover all seven roles; separate tests cover bound producer→state→effects, expiry/caps, replay/order conflicts, max-safe damage and untouched input on batch failure.

<!-- source-body:source-dc491ec5031c2fcc:end -->


<a id="source-9362db4fe2c26c1d"></a>
## Client Admission and Content Release Contract

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/client-admission-content-release.md`  
원문 SHA-256: `e957cd7ca75fdc075efdcc7fd844928f8c02bc69c29c6f0e07d74b9b4d456651`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-9362db4fe2c26c1d:start -->
<a id="source-9362db4fe2c26c1d-client-admission-and-content-release-contract"></a>
### Client Admission and Content Release Contract

Status: local launch contract. No remote R2 writes, PRD secrets, DNS, or
production activation are part of this slice.

`GET /v1/client-config` is the Game API admission/readiness entrypoint for the
installed Unity app. The same admission check is also applied to protected
`/v1/*` Game API routes when `CLIENT_ADMISSION_MODE=ENFORCED`, so a stale client
cannot skip client-config and call auth, run, gacha, reward, or profile routes
directly.

The client sends its installed build metadata in headers:

- `X-LastRail-App-Version`: optional human display version.
- `X-LastRail-App-Version-Code`: decimal Android/Unity version code.
- `X-LastRail-App-Platform`: optional platform label such as `android`.
- `X-LastRail-App-Channel`: optional release channel such as `internal`.

The server reads the current version document from the configured version R2
bucket and compares the installed code with server-owned
`minimumVersionCode`. Frontend-only checks are not authoritative.

The same version document may include a single approved content release pointer.
The Game API returns it only when the content release is approved and compatible
with the installed app version code. The pointer is read-only metadata for
Addressables/catalog bootstrap; it is not a DB rollback mechanism.

Missing version policy or missing approved compatible content is explicit
fail-closed configuration state. Protected route failures use the same scrubbed
response shape and return `400` for missing app version, `426` for update
required, or `503` for server policy/content configuration failures. The
response must not include R2 credentials, secrets, raw deployment internals, or
unpublished manifest bodies.

Allowed exceptions:

- `GET /status`
- `GET /v1/client-config`
- password recovery verification/reset routes
- frontend diagnostic `client-errors` routes

Recommended response fields:

- `accepted`
- `clientAdmission.status`
- `clientAdmission.installedVersionCode`
- `clientAdmission.minimumVersionCode`
- `clientAdmission.updateRequired`
- `contentRelease.status`
- `contentRelease.releaseId`
- `contentRelease.contentVersion`
- `contentRelease.catalogUrl`
- `contentRelease.catalogHash`
- `contentRelease.compatibility`
- `errorCode`
- `message`

Status values:

- `ACCEPTED`
- `APP_VERSION_REQUIRED`
- `UPDATE_REQUIRED`
- `VERSION_POLICY_UNCONFIGURED`
- `CONTENT_RELEASE_UNCONFIGURED`
- `CONTENT_RELEASE_NOT_APPROVED`
- `CONTENT_RELEASE_INCOMPATIBLE`

Admin version management may save draft/current documents, but save success does
not equal production activation. DB schema/data rollback remains a roll-forward
or audited correction operation, not an Admin version rollback.

Legacy Admin minimum-version exports that omit `contentRelease` must preserve
the existing approved content pointer. Future content pointer writes need an
explicit precondition/revision contract before concurrent pointer updates are
enabled.

<!-- source-body:source-9362db4fe2c26c1d:end -->


<a id="source-b941b10631564c0a"></a>
## Client release registry — DEV / PRD contract (2026-09-08)

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/client-release-registry.md`  
원문 SHA-256: `77293f0b3b63683bf3a6435e6cb6f7b4bf0edcd963f6102bcb29f22edc084718`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-b941b10631564c0a:start -->
<a id="source-b941b10631564c0a-client-release-registry--dev--prd-contract-2026-09-08"></a>
### Client release registry — DEV / PRD contract (2026-09-08)

Source: `apps/game-api/src/index.ts`, `apps/admin-api/src/client-release-registry.ts`,
`apps/admin-api/src/index.ts`, `packages/common/src/version-policy.ts`.
This DEV-policy correction is LOCAL ONLY on `codex/dev-stable-registry-20260907`,
based on c08bfcdab273923a9a81faa2a7da039d758edd81.
Deployment evidence: [DEV2026.09.07.4 report](50_RELEASE_OPERATIONS_CONTRACTS.md#source-3a289b92502a9144)（옛 서버 폴더 링크는 원래 누락되어, 같은 주제의 보존된 기획 저장소 배포 기록으로 연결）. The deployed DEV2026.09.07.4
still uses c08bfcd (old DEV INTERNAL behavior); do not enable the new UI contract
against it. No new deployment, remote write, device success or artifact-byte proof.

<a id="source-b941b10631564c0a-environment-contract-and-activation"></a>
#### Environment contract and activation

| Behavior | DEV | PRD |
| --- | --- | --- |
| Register initial status | STABLE (immediately eligible, subject to all admission checks) | INTERNAL |
| Supported stored statuses | STABLE, DISABLED | INTERNAL, STABLE, DISABLED |
| Status request values | STABLE, DISABLED | INTERNAL, DISABLED |
| Re-enable disabled row | status=STABLE, same immutable artifact | status=INTERNAL; promote separately |
| Promote | unsupported; capability=false; POST returns400 registry_promotion_environment_disabled | INTERNAL→STABLE with artifact confirmation |
| Tester entitlement | never queried or required | required only for INTERNAL |
| Minimum | separate explicit operation; never raised by register/status | separate explicit operation; never raised by register/promote |

DEV has one distribution channel, not a restriction to one registered version code.
Multiple explicit STABLE codes can coexist; no larger/unregistered code is inferred.
Registration is activation of that exact DEV row after successful whole-document CAS.
There is no separate activation endpoint or promote step in DEV. Re-enabling uses
`/:versionCode/status` with `status:"STABLE"`. Registration/reactivation cannot replace
version or artifact fields. A valid compatible content release is still required.

LEGACY GET returns rows=[] with the environment-specific distributionPolicy below.
First register requires `legacyTransitionConfirmation:"ENABLE_EXACT_LOOKUP"` in both
environments. It atomically turns on exact lookup; all unregistered legacy codes stop
being accepted. It neither imports the old row nor changes minimum. No auto-activation
on reading the page. Capability=true expresses role support, not deployment approval
or artifact readiness. If distributionPolicy is absent, Admin must treat this as the
older contract and not synthesize new DEV write semantics.

A stored DEV INTERNAL row is invalid configuration, not an implicit STABLE conversion.
Game returns version_policy_unconfigured (503 on protected routes; safe GET config200
accepted=false, no content or authentication bootstrap). Admin GET returns502
version_storage_read_failed; mutations return400 registry_document_environment_invalid.
Rollback cannot restore an INTERNAL row into DEV. Such pre-existing documents need
separately reviewed repair, not an entitlement grant or automatic permission expansion.
Last deployment readback had an empty/absent DEV registry; no data conversion is part
of this patch. D1 migration0024 remains unchanged; existing DEV entitlement records
are ignored by DEV admission. No new migration is required.

<a id="source-b941b10631564c0a-stored-document-and-migration"></a>
#### Stored document and migration

Existing `versions/current.json` keeps `version`, `versionCode`, `minimumVersionCode`
and optional legacy `contentRelease`. Optional `clientReleaseRegistry` is:

```json
{"schemaVersion":1,"entries":[{"version":"0.2.0","versionCode":"2","status":"INTERNAL","contentRelease":{"status":"APPROVED","releaseId":"example-release","contentVersion":"example-content","catalogUrl":"https://example.test/example-release/catalog.bin","catalogHash":"sha256:<64 lowercase hex>","minimumVersionCode":"2","maximumVersionCode":"2"}}],"auditHead":"<history UUID>"}
```

All version codes are decimal strings. Entries use exact versionCode equality;
larger-than-stable does not imply INTERNAL. Minimum is evaluated first. Status follows the environment table above; the stored-document example uses PRD INTERNAL. Artifact and version identity cannot be edited after
registration; use another version code. Status mutation is the only row edit.
One releaseId cannot identify different contentRelease metadata within the registry.
Registry absent preserves legacy admission. First registration enables exact lookup:
unregistered legacy codes then fail closed; no legacy status is inferred/imported.
For this first registration only, the body must explicitly include
`legacyTransitionConfirmation:"ENABLE_EXACT_LOOKUP"`; absence returns
`registry_transition_confirmation_required` (400) without a storage write.
This confirmation is separate from promotion and minimum-version confirmation.
Legacy export rejects registry documents with `version_registry_managed` (409).

<a id="source-b941b10631564c0a-game--unity-handshake"></a>
#### Game / Unity handshake

Send `X-LastRail-App-Version-Code`, existing App-Version/Platform/Channel headers.
`GET /v1/client-config` always returns HTTP200 with safe decision fields. Existing
response fields remain; added `registryRevision` (whole quoted R2 ETag or null),
`clientAdmission.distribution` (INTERNAL/STABLE/DISABLED or null), and
`bootstrap:{canAuthenticate:boolean,contentAuthorized:boolean,authenticationRequired:boolean}`.
For PRD INTERNAL without valid login: accepted=false, status=AUTH_REQUIRED,
errorCode=authentication_required, canAuthenticate=true, contentAuthorized=false.
Content release fields including URL/hash/releaseId are null. Authentication can
proceed without ConfigPassed/content download. Never interpret canAuthenticate as gameplay approval.

Allowed pre-content authentication: POST `/v1/auth/guest`, `/v1/auth/password/register`,
`/v1/auth/password/login`, `/v1/auth/google`, `/v1/auth/refresh`.
Authenticated hydration allowed before tester approval: GET `/v1/profile`,
GET `/v1/profile/display-name/check`, POST `/v1/profile/display-name`, GET `/v1/story-progress`.
These retain each handler's authentication and validation. Existing recovery/reset
exceptions remain. No generic /v1 or economy bypass is introduced.

After authentication, GET `/v1/client-config/authorized` with Bearer session.
It authenticates first, then checks version and, for PRD INTERNAL, exact account+environment+
versionCode ACTIVE/unrevoked/unexpired server tester entitlement. Success:
accepted=true, status=ACCEPTED, contentRelease.status=APPROVED_COMPATIBLE plus
releaseId/contentVersion/catalogUrl/catalogHash/compatibility. STABLE permits
pre-auth content; gameplay still requires normal session authentication.
Authenticated GET `/v1/client-config` evaluates tester entitlement only for PRD INTERNAL.
DEV STABLE permits content metadata before login; normal authenticated gameplay and
the authorized endpoint still require a session. DEV has no AUTH_REQUIRED→tester gate.

Pin accepted releaseId in `X-LastRail-Content-Release-Id` on registry gameplay/economy
requests. Missing/mismatched pin returns409. Every protected request reevaluates current
minimum/status and (PRD INTERNAL only) tester authorization. Promotion preserves artifact/pin. Logout/session
change, revoke, disabled version or update requirement requires client revalidation.
Keep stable Wi-Fi predownload/non-Wi-Fi consent behavior; do not invent Editor headers.
Public asset hosting is not made confidential by withholding metadata.

| Decision | status / errorCode | authorized/protected HTTP |
| --- | --- | --- |
| Header missing | APP_VERSION_REQUIRED / app_version_required | 400 |
| Below minimum | UPDATE_REQUIRED / client_update_required | 426 |
| Exact code absent | UNREGISTERED / client_version_unregistered | 403 |
| Disabled | DISABLED / client_version_disabled | 403 |
| Internal login needed | AUTH_REQUIRED / authentication_required | 401 |
| Tester missing/expired/revoked | TESTER_FORBIDDEN / tester_forbidden | 403 |
| Pin missing/mismatch | content_release_pin_required / content_release_pin_mismatch | 409 |
| Invalid/missing policy | version_policy_unconfigured | 503 |

Authorized endpoint's initial authentication failures retain existing auth error DTO/status.
The two client-config responses are no-store. Legacy requests do not require a pin.

<a id="source-b941b10631564c0a-admin-api"></a>
#### Admin API

Existing Admin authentication applies. Reads accept all active Admin roles; writes
require OWNER. target must equal serving ENVIRONMENT and be DEV or PRD. Local PRD
tests use fake storage; this API implementation does not authorize remote PRD writes.

GET `/v1/admin/client-releases?target=DEV|PRD` returns:

```text
{target, registryStatus:LEGACY|CONFIGURED, revision:string|null,
 minimumVersionCode:string|null, auditHead:string|null,
 rows:[{version,versionCode,status,contentRelease,artifactIdentity,
        validation:{status:STRUCTURAL_ONLY,artifactBytesVerified:false}}],
 distributionPolicy:{supportedStatuses:string[],initialStatus:string,mutableStatuses:string[],testerGate:boolean},
 canMutate:boolean, capabilities:{register,changeStatus,promote,changeMinimum,rollback,manageTesters:false}}
```

DEV distributionPolicy is exactly
`{supportedStatuses:["STABLE","DISABLED"],initialStatus:"STABLE",mutableStatuses:["STABLE","DISABLED"],testerGate:false}`.
PRD is exactly
`{supportedStatuses:["INTERNAL","STABLE","DISABLED"],initialStatus:"INTERNAL",mutableStatuses:["INTERNAL","DISABLED"],testerGate:true}`.
For OWNER, DEV capabilities are register/changeStatus/changeMinimum/rollback=true,
promote/manageTesters=false. PRD adds promote=true; manageTesters remains false.
Non-OWNER readers get canMutate=false and all mutation capabilities=false.

artifactIdentity is `sha256:` plus SHA256(canonicalJson(contentRelease)), not an
artifact download proof. revision is **whole document**, never a per-row revision.
Every POST body includes `{target,expectedRevision,reason}`; reason is nonempty <=500
characters; expectedRevision is quoted ETag from GET. Extra body fields are rejected.
Success returns the same fresh GET DTO (readback may observe a later writer).

| POST path | Additional fields / behavior |
| --- | --- |
| `/v1/admin/client-releases/register` | version,versionCode,contentRelease; DEV initial STABLE / PRD initial INTERNAL; existing code rejected; first legacy transition additionally requires legacyTransitionConfirmation=`ENABLE_EXACT_LOOKUP` |
| `/v1/admin/client-releases/:versionCode/status` | DEV status=STABLE or DISABLED; PRD status=INTERNAL or DISABLED; artifact unchanged |
| `/v1/admin/client-releases/:versionCode/promote` | expectedArtifactIdentity,confirmation=`PROMOTE_UNCHANGED_ARTIFACT`; PRD INTERNAL→STABLE only; DEV unsupported400 |
| `/v1/admin/client-releases/minimum` | minimumVersionCode,confirmMinimumChange=true; separate action, usable STABLE at/above minimum required |
| `/v1/admin/client-releases/rollback` | historyId,confirmation=`RESTORE_REGISTRY`; restore committed history's before-registry, preserve current minimum |

Rollback retains later registrations as DISABLED tombstones so a version code can
never be reused for another artifact. They may be re-enabled with the same artifact.
Promotion never raises minimum. Root latest version advances on PRD promotion or DEV registration/reactivation to
an equal/higher code. Disabling a row does not lower root latest or minimum. Rollback cannot restore legacy permissive mode or invalidate minimum.
Candidate history objects are immutable at `versions/client-release-history/<UUID>.json`
and record actor,reason,time,operation,before/after,expectedRevision,parent. History is
written before pointer CAS; only the auditHead parent chain proves a candidate committed.
CAS-losing orphan candidates cannot be rolled back. No public history listing API yet;
retain returned auditHead for review/rollback. Minimum has its own explicit confirmation.

Errors use `{error:{code}}`: missing revision428; stale revision,
version_code_already_registered, registry_artifact_identity_conflict,
registry_promotion_state_invalid409; role403; invalid body/target/transition400;
storage unavailable503/read or write failure502. Refresh GET after409; never auto-force.

<a id="source-b941b10631564c0a-tester-provisioning-boundary"></a>
#### Tester provisioning boundary

Migration0024 creates empty `account_tester_entitlements`: account_id,environment,
version_code,status,expires_at,revoked_at,granted_by,revision,created_at,updated_at.
No existing tester source was found; no production grants or public management route
are added. `manageTesters:false` is deliberate. Only synthetic local test fixtures
populate entitlements. Production provisioning/revocation ownership remains separate.

<!-- source-body:source-b941b10631564c0a:end -->


<a id="source-d1c9e8663b08469b"></a>
## CE02 server field contract and reward-reference review

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/combat-content-resolver-review.md`  
원문 SHA-256: `6ca68490164e245ac381fa25e343c13e57addc6ed61024673331c5e44a236360`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-d1c9e8663b08469b:start -->
<a id="source-d1c9e8663b08469b-ce02-server-field-contract-and-reward-reference-review"></a>
### CE02 server field contract and reward-reference review

Current emitted diagnostic version: `run-combat-requirements-v2` (CE01).
This is a requirements DTO, not an approved combat manifest or proof of Loaded.
Unity may build a typed resolver around these inputs while remaining blocked.

| Existing field | Type / source | Consumer meaning |
| --- | --- | --- |
| requiredContent.operationId | string|null, server operation preview | Exact operation key; never substitute a nearby node |
| requiredContent.contentVersion | string|null, preview metadata | Exact existing `act1.launch.content.v1` requirement; not proof of assets |
| startAugmentPackageRef / startTacticPackageRef under requiredContent | string|null | Existing package refs; typed augment/tactic resolution is separate |
| requiredContent.encounterType | string|null | Existing ORDINARY_15_WAVE / RAID_BOSS; unknown value unsupported |
| requiredContent.raidId / bossId | string|null | Null is normal for ordinary encounters; raid requires actual approved binding |
| requiredContent.bossPhaseRefs | string[] | Existing ordered phase refs, not loaded phase implementations |
| requiredContent.bindingVersion / catalogHash | null | No approved combat binding/manifest identity supplied |
| enemyFactionId / wavePlanId | null | Actual operation/content bindings still missing |
| rewardTableId | null | There is no separately identified table in the server model |
| riskTier / riskLevel | Roman string|null / integer1..5|null | Explicit versioned tier projection; root old risk types are unchanged |
| operatorSlotCount | number5|null | Count, not legacy array of slot labels |
| ultimateOperatorId / ultimateSelectionRevision | string|null / number|null | Validated CE01 admission pin; never builder fallback |

The current field names, nesting and version are fixed by this commit's API code.
Do not infer a bindingVersion/hash from contentVersion, or install a fixture
manifest into the shipping catalog. ApprovedCatalogSafety's locator/hash admission
and AddressableContentBackend loading can be reused, but screen/scene catalog
approval does not imply approval of faction, wave, reward or stats resources.
Absent binding identity should produce a binding-missing diagnosis before actual
resource lookup. An unsupported schema/type, invalid identity/hash, missing bound
resource and failed load should remain distinct; none means Loaded/READY. Exact
Unity enum/API and a future manifest schema are not emitted by this server slice.

<a id="source-d1c9e8663b08469b-structured-reward-reference-feasible-adapter-direction-not-yet-a-wire-field"></a>
#### Structured reward reference: feasible adapter direction, not yet a wire field

Actual server evidence: `departure.ts` validates selected active balance
`runRewards.operations[operationId]`, operation progression and risk multipliers,
then pins balance version and full config hash. `index.ts` run-finish query joins
balance_config_versions using runs.balance_config_version; it parses runRewards
and calls calculateRunRewardPreview with preview operationId, risk, outcome,
cleared waves and first-clear status. `packages/balance/src/index.ts` supports
runRewards schemaVersion1 and resolves config.operations[operationId]. No
rewardTableId participates. The shared Unity RunStartResponseJsonAdapter still
requires legacy rewardTableId, so the current legacy adapter is not compatible.

A separate versioned Unity adapter can accept a structured source, for example:

```ts
// Proposed next contract only; NOT emitted by CE01 and NOT a manifest approval.
type RewardReference = {
  kind: "BALANCE_RUN_REWARDS";
  schemaVersion: 1;
  balanceVersion: string;
  operationId: string;
};
```

Those components already have real server meanings. Use the pinned version,
not whatever balance is active later; keep risk/outcome/first-clear inputs
separate. This avoids fabricating an opaque table ID. Client needs a verified
resource/delivery path for that exact version if it displays reward data. Server
finish/claim remains reward authority: this reference is neither a grant nor an
amount promise. Before removing reward_table_binding_missing, agree and implement
the producer field, dedicated consumer and versioned resource policy together.
No fake rewardTableId or approved reward-content asset is introduced here.

<a id="source-d1c9e8663b08469b-separate-remaining-decisions"></a>
#### Separate remaining decisions

Actual faction/wave bindings and manifest version/hash need approved resource
evidence. Combat seed needs an independently approved purpose, RNG algorithm and
version, generation rule, run lifetime and exact replay policy. Engine/effective
stats need approved formula/profile version, level mapping and rounding. Canonical
test profiles and numeric proposals are not those approvals. CE01 supplies only
the ultimate choice; it does not adopt timing/effects, seed or numerical baselines.

<!-- source-body:source-d1c9e8663b08469b:end -->


<a id="source-696bb63df6feb47a"></a>
## CE03 local combat entry contract and implementation plan

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/combat-entry-ce03.md`  
원문 SHA-256: `d5409cd42648acb46e33075defb716ec5aa9ad0269ba7fc94bfb67193ce4fc18`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-696bb63df6feb47a:start -->
<a id="source-696bb63df6feb47a-ce03-local-combat-entry-contract-and-implementation-plan"></a>
### CE03 local combat entry contract and implementation plan

Goal: prepare one reproducible local lobby→accepted run→combat consumer→finish→reward receipt→lobby path without opening public combat admission on missing content. Base commit94349cf77582c62a86ca3bd2b35bfd8793c5ceb4. Plan delegation2026-09-11 and canonical combat-entry-next-slice-20260910.md are authority. Local implementation only; no remote DB, seed, deployment or real payment.

<a id="source-696bb63df6feb47a-evidence-and-boundaries"></a>
#### Evidence and boundaries

- Server departure.ts issue/startDeparture already pins owned five operators, selected squad leader, loadout/growth facts, balance quote and fuel. Run snapshot and original start response commit with one debit. HTTP index.ts retains unconditional combat_not_ready for new departure admissions; original committed replay precedes it.
- index.ts finish reads runs.balance_config_version→balance_config_versions.config_json, parses runRewards and calls calculateRunRewardPreview. Its operation dictionary is the real reward source. No rewardTableId catalog exists. prepareLaunchRewards additionally enforces first-clear entitlement/conversion at finish and claim; a base reward reference is not an unconditional final grant promise.
- packages/balance/src/index.ts scales base reward by operation/risk/outcome/wave BPS then Math.floor, with separate unchanged first-clear lines. Existing launch-economy binding and account entitlement checks stay authoritative.
- Unity E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/Core/Simulation/RunRandom.cs implements uint32 xorshift13/17/5, zero→0x6D2B79F5, Range=min+NextUInt()%span. This is a simulation PRNG, not seed generation or recruitment RNG.
- Unity RunStartSnapshotBuilder.BuildActOne has default-roster/Kyle/carriage fallbacks. CE03 must use explicit server-owned selection and strict consumer; BuildActOne defaults are not allowed for this path.
- Canonical train-combat-test-profile-v1.md is RECOMMENDED TEST PROFILE, proposal_train_combat_v1; it explicitly says no code/config activation. Source growth facts in admission are not effective stats.
- Durable run/checkpoint/final-result/reward-preview tables exist, but an accepted run is not proof of content loaded or a durable Unity consumer checkpoint. Existing reward preview expires24h; this work does not extend it or invent compensation.

<a id="source-696bb63df6feb47a-exact-foundation-dtos"></a>
#### Exact foundation DTOs

`rewardReference:{version:"run-reward-ref-v1",balanceVersion:string,operationId:string,risk:"I"|"II"|"III"|"IV"|"V",scope:"BASE_RUN_REWARDS",hashAlgorithm:"SHA-256",canonicalization:"canonical-json-v1",configSha256:lowercaseHex64}`. Hash input is UTF8 canonicalJson(parseRunRewardsConfig(config.runRewards)), using the repository canonicalJson sorted-object implementation; arrays preserve order. This includes outcome/risk/operation reward policy, not account-specific first-clear eligibility. No fabricated table ID. It is returned with a new preview, persisted in preview and accepted start snapshot, checked against the same balance policy at start/finish. Old accepted starts without a reference retain their original shape and legacy finish behavior. Existing finish/claim APIs still calculate and pay; Unity does not award from the hash.

Seed protocol candidate for coordinated local fixture adoption: `{version:"combat-seed-local-v1",value:uint32,generation:"WEB_CRYPTO_UINT32",simulation:"XORSHIFT32_13_17_5_V1",purpose:"LOCAL_COMBAT_SIMULATION",issuedAt:unixMs,byteOrder:"LITTLE_ENDIAN",lifetime:"ORIGINAL_RUN",zeroNormalization:1831565813}`. Generation draws fresh four bytes via crypto.getRandomValues, independently of runId/time/account/gacha. Simulation exactly follows existing Unity unsigned wrap and logical right shift; Range retains modulo bias for parity, not cryptographic use. Generate once when creating an accepted fixture run, persist in the same start transaction, return the exact original on replay/restart. A rejected/rolled-back tentative generation is never an accepted seed. No wall-clock expiry/reseed for an existing run; new run needs new seed. Continuation requires a complete checkpoint or a separately proven full deterministic replay profile; seed and ordered inputs alone are insufficient with the existing variable-float simulation clock; reinitializing from original seed halfway is invalid. This candidate is implemented/tested as an isolated protocol helper first, not silently injected into public start or declared adopted production policy.

`GET /v1/runs/:runId/lifecycle` implemented read-only foundation: auth/no-store, no body/query; account-owned run plus original start snapshot, final result and reward receipt from one DB read. Fields `version:"run-lifecycle-v1",runId,runStatus,startResponse,finish:{status:"NOT_RECORDED"|"RECORDED",outcome,submittedAt},reward:{status:"NOT_AVAILABLE"|"CLAIMABLE"|"CLAIMED"|"EXPIRED"|"UNAVAILABLE",rewardPreviewId,expiresAt,claimId,receipt},nextAction`. Read does not submit finish, claim, refund, renew TTL, mark battle complete or authorize reentry. Original start-result/start-state routes remain available. nextAction guides recovery only: RECOVER_COMBAT_CONSUMER,RESUME_FINISH,REWARD_REVIEW,RETURN_TO_LOBBY,REVIEW_REQUIRED. Missing snapshots/unknown or quarantined state must use REVIEW_REQUIRED.

<a id="source-696bb63df6feb47a-effective-stats--engine-test-adoption-proposal-not-live-binding"></a>
#### Effective stats / engine test adoption proposal, not live binding

<a id="source-696bb63df6feb47a-consumer-review-amendments-binding-for-this-foundation"></a>
##### Consumer review amendments (binding for this foundation)

Existing `/v1/balance-config/:version` returns `{version,schemaVersion,requiredContentVersion,checksum,publishedAt,payload}` from the historical balance row, including payload.runRewards. Reuse it; no policy endpoint is added. Client hash verification additionally requires the same parseRunRewardsConfig projection (schemaVersion,riskMultipliersBps,operationProgressionMultipliersBps,outcomePolicy,operations,integrityPolicy) and canonical serializer: recursively sort object keys by ordinal UTF16, preserve array order, compact JSON/UTF8. Server published checksum is not automatically the reward hash. Until that exact policy/projection/serializer is implemented and obtained, Unity preserves an OPAQUE_SERVER_PIN and must not report local hash validation. Final finish validates against the run's historical version, not newly active policy.

Seed JSON is an unsigned decimal integer0..4294967295, never parsed through int32. Exactly four CSPRNG bytes are decoded little-endian via DataView.getUint32(0,true). Byte vectors00000000→0,00000080→2147483648,FFFFFFFF→4294967295. Seed0 initialization normalizes to1831565813; checkpoint PRNG state0 is invalid and restoreLocalCombatRandom rejects it. Accepted restoration range is1..4294967295. Range(min,max) accepts int32 integers with max>min and span<=2147483647; negative min is allowed only within that safe span. It returns min+NextUInt()%span; no floating random or rejection sampling. Seed1 outputs270369,67634689,2647435461 for the first three draws; Range(10,20) first draw=19. This is PRNG parity only, not complete battle determinism.

Reuse PlayerEntryRuntime.Runs/DurableRunClient/EncryptedRunOperationJournal and its HandoffAsync durable-sink ACK-before-delete. AccountActionJournal owns the new departure start request until a single durable combat sink ACKs the same account/run/original receipt identity. Never have both journals resend the same start/finish or construct a second accepted run from the legacy builder. No new journal/send transport is proposed. BattleSession only ticks; its full state restore owner remains Unity work.

A coherent checkpoint needs version/profile/content/snapshot hashes, checkpoint revision, fixed tick/step or exact delta history, system execution order, input watermark, all PRNG stream states and entity-ID counters, complete entities/positions/HP/shields, train/carriage/operator state, cooldowns/casts/targets, buffs/stacks/expiry, projectiles/pending effects, spawn/wave timers/cursors, boss phase and tactics/augments. Capture atomically at a simulation tick boundary. Neither the existing variable float clock nor seed+input ordering establishes replay equivalence. No checkpoint DTO/whole-battle restoration implementation is claimed by this server foundation.

Lifecycle time fields serverTime,finish.submittedAt,reward.expiresAt are UTC Unix milliseconds. submittedAt/outcome are null when no final result; rewardPreviewId is null when no preview and is repeated in finish to recover lost finish responses. claimId/receipt are null before claim; missing/corrupt claimed receipt produces UNAVAILABLE. startResponse is the original envelope or null when unavailable. CLAIMED receipt remains CLAIMED after expiresAt. `uiReturnAllowed:true` permits returning to the lobby UI with the run, original journals and disposition retained; `newAttemptAuthorized:false` always means this read grants no new-entry authority. disposition is REWARDED,EXPIRED_UNCLAIMED,REVIEW_REQUIRED or PENDING. Even REVIEW_REQUIRED must not force a permanent lobby UI lock; it also does not clear run/journal blockers or permit a free restart.

reward.receipt is the persisted transaction evidence `{rewardTransactionId,runId,serverClaimedAt:unixMs,rewardPlan}`. rewardPlan is the original stored reward_transactions.reward_json with rewardLines/calculation; it is not reconstructed current balances or a claim that the old HTTP claim envelope was stored verbatim. The original claim journal/replay remains the source for that HTTP response. Malformed/nonobject stored plan or absent rewardLines is UNAVAILABLE, not successful recovery.

| runStatus | finish | reward | nextAction |
| --- | --- | --- | --- |
| ACTIVE, valid snapshot | NOT_RECORDED | NOT_AVAILABLE | RECOVER_COMBAT_CONSUMER (does not prove checkpoint exists) |
| FINISH_PENDING, valid snapshot | NOT_RECORDED | NOT_AVAILABLE | RESUME_FINISH (original journal only) |
| COMPLETED/FAILED/ABANDONED, valid snapshot | RECORDED | CLAIMABLE | REWARD_REVIEW, explicit user claim required |
| COMPLETED/FAILED/ABANDONED, valid snapshot | RECORDED | CLAIMED with receipt | RETURN_TO_LOBBY |
| terminal run | RECORDED | EXPIRED | REVIEW_REQUIRED; EXPIRED_UNCLAIMED retained, UI return allowed |
| any with missing start snapshot, QUARANTINED, invalid combination, missing receipt or EXPIRED run | any | any | REVIEW_REQUIRED |

REVIEW_REQUIRED and UI return are separate from successful completion and new admission. No compensation, reward renewal/mail conversion, new-run creation or checkpoint reset is performed.

Shared candidate profile ID `ce03-local-profile-v1` remains TEST_ADOPTION_PROPOSED until Plan/Unity explicitly select the same fixture. Pin `profileId,profileSha256,engineProfileVersion,effectiveStatsVersion,roundingVersion,sourceOwnership,snapshotSha256` in a future accepted local manifest. Server owns account/slot/operator/train/carriage IDs, levels/revisions, ownership and final snapshot; Unity verifies versions/hash and consumes resolved values, never recalculates from later account state.

Candidate train-only arithmetic reuses proposal_train_combat_v1: engine output levels1–10=[65,68,72,75,78,82,86,89,92,95], used only for rail speed. TrainHP=5000*(100+8*(level-1))/100; locomotiveATK=floor(60*(100+6*(level-1))/100), armor1000BPS. No gear/research/faction multipliers for this candidate. All nonnegative integer magnitudes use final floor after the complete rational product, never intermediate float rounding. Engine output is not weapon ATK or attack speed. Operator/carriage/ability/wave complete profile is not established by this train subset; do not manufacture missing values. Actual content/binding hashes remain required from Direction/Unity, and no test fixture is registered in the launch catalog.

<a id="source-696bb63df6feb47a-durable-consumer-and-recovery-state-transitions"></a>
#### Durable consumer and recovery state transitions

Before start: validated/downloaded exact binding/profile/assets → locally persist original start body/key/account/preview → POST start. Unknown start response: start-result lookup with original key/preview; NOT_FOUND is not permission to invent a successful run. Committed response is durably stored as an immutable handoff keyed account/run/snapshot hash before loading battle. Only one consumer may claim that handoff locally; reload restores the same run and hashes. Server acceptance does not certify loaded content.

Unity durable consumer states proposed: ACCEPTED_UNCONSUMED→LOADING→RUNNING→FINISH_PENDING→RESULT_PENDING→REWARD_REVIEW→CLAIM_PENDING→RESULT_RECEIVED→RETURNED. Store original seed plus checkpoint PRNG state/input sequence/content+engine hashes. Crash during LOADING retries the same handoff; RUNNING requires a compatible complete checkpoint or an independently validated deterministic replay profile. Existing BattleSession does not implement this restoration. If neither exists, show recovery unavailable, do not reset into a free new attempt or claim fake success. FINISH_PENDING persists exact finish body/key before network; resend/lookup until committed. Server run.status FINISH_PENDING and local submission pending are separate: current API does not durably set server FINISH_PENDING merely when the client prepares a body.

Existing finish idempotency/final result creates a reward preview; RESULT_PENDING reads/replays original finish and enters REWARD_REVIEW. Only the user's explicit claim action enters CLAIM_PENDING and sends the persisted exact claim body/key; CLAIMABLE never automatically claims. Existing duplicate finish returns DUPLICATE_REPLAY/accepted:false with the same rewardPreviewId, not an identical success envelope. RESULT_RECEIVED requires actual server reward receipt; returning to lobby refreshes account state without another start/finish/claim. Engine/content mismatch, quarantine, expired unclaimed reward or missing receipt is an explicit blocked/review state. No new TTL/refund/compensation policy is inferred. Complete Unity handoff/checkpoint persistence and real scene consumer are Unity-owned implementation still required.

<a id="source-696bb63df6feb47a-implementation-order-and-tests"></a>
#### Implementation order and tests

- [x] First send this exact contract/profile proposal to Plan for Unity coordination; canonical mirror/readback if permitted.
- [x] Add server combat-reward-reference.ts: strict hash builder/validator, shared canonical test vectors; modify departure.ts to pin actual reward reference and index.ts to verify only newly pinned finishes. Test exact replay, old run compatibility, changed policy rejection before payout, existing actual finish/claim and fuel one-time behavior. No migration needed.
- [x] Add isolated combat-seed-protocol.ts matching existing Unity RunRandom, test unsigned boundaries/zero normalization/known sequence/Range and independent CSPRNG generation. Do not activate public seed or stat/profile bindings while test adoption is pending.
- [x] Add read-only run lifecycle recovery using existing DB records; test ownership/no writes, active/finish_pending/quarantined/terminal states, actual reward receipt and24h expiry, original start snapshot preservation. Do not fake loaded/running/checkpoint state.
- [x] Run focused regressions, full suite, game/async types/lint and independent review; fix findings, commit code and this contract together. Report exact tests and unsupported scene/assets/stats/consumer work to Plan. New end-to-end local HTTP admission/profile activation requires an explicit shared test profile and actual bound content, not client-ready flags.

The first deliverable is interoperable foundation and a reproducible protocol test profile, not proof that a battle scene has played. Full local end-to-end acceptance additionally requires Unity's real durable consumer and actual assets, verified on the same immutable profile/contract revision.

Verification:86files/579tests PASS (20.83s), game-api/async-worker TypeScript and source ESLint PASS; independent review found no high-impact foundation defect. CE03 focused tests are combat-ce03.test.ts and combat-seed-protocol.test.ts. Public combat gate remains closed, no live seed/profile/catalog or full Unity checkpoint was activated. No new migration; existing0001–0035 baseline unchanged.

<!-- source-body:source-696bb63df6feb47a:end -->


<a id="source-172d7ea1b2d372f8"></a>
## Crystal wallet contracts — local implementation, no release policy

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/crystal-wallet.md`  
원문 SHA-256: `a71979dedd651a0ca21aa76d25eca8656495932c2acc991d3f64b98dd7531716`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-172d7ea1b2d372f8:start -->
<a id="source-172d7ea1b2d372f8-crystal-wallet-contracts--local-implementation-no-release-policy"></a>
### Crystal wallet contracts — local implementation, no release policy

Accepted policy source: planning `docs/ops/crystal-source-policy.md`, 2026-09-07.
Migration 0023 and this API are local only. No currency identifier, rate, recovery
price, reward table or store SKU is seeded. No actual store adapter is installed.

<a id="source-172d7ea1b2d372f8-unity-dtos"></a>
#### Unity DTOs

`GET /v1/wallet/crystals` requires the existing authenticated active account/session
and admission contract. Missing policy returns **503** with
`{ "accepted": false, "errorCode": "crystal_policy_unconfigured" }`.
Show unavailable/pending configuration, not a fabricated zero balance.

Configured response fields:

| Field | Type / meaning |
| --- | --- |
| currencyId | string, server-configured identifier; no final identifier chosen |
| policyVersion | string, immutable policy ID required by wallet mutations |
| freeBalance / paidBalance / totalBalance | nonnegative safe integers; total is FREE + PAID |
| spendOrder | `["FREE", "PAID"]` |
| ticketExchange | null or `{ticketItemId, crystalsPerTicket, maxTickets}` |
| purchaseConfigured | boolean: product mapping exists, not proof of a real store connection |

`POST /v1/wallet/crystals/tickets/exchange` body is exactly
`{policyVersion:string,ticketCount:positiveInteger}`.
`POST /v1/wallet/crystals/purchases/verify` body is exactly
`{policyVersion:string,productId:string,receipt:string}`.
Both require Idempotency-Key. Unknown fields, caller account/paid/amount flags,
query strings and oversized bodies are rejected. A missing exchange configuration
or purchase verifier returns 503. Mismatched policy, changed-body replay,
insufficient balance and competing spent balance return 409. Invalid store proof
returns 403. Request-body stream is bounded before JSON parsing.

Successful wallet mutation response fields are `accepted`, `operationId`,
`currencyId`, `policyVersion`, `freeBalance`, `paidBalance`, `totalBalance`,
`freeSpent`, `paidSpent`, `freeGranted`, `paidGranted`. Exchange adds
`ticketsGranted`, `ticketItemId`, `ticketBalanceAfter`. Purchase adds
`purchasedQuantity`, `purchaseLinked`. Paid spending is explicitly disclosed through
`paidSpent`. Retry the same request/key on uncertain completion; replay returns
the stored receipt even after configuration changes, not a new grant.

<a id="source-172d7ea1b2d372f8-attendance-and-pass-integration"></a>
#### Attendance and pass integration

Existing activity reward arrays now accept `{kind:"CRYSTAL",quantity:integer}`
alongside ITEM rewards. Currency ID comes only from the configured wallet policy.
Attendance, milestones, recovery rewards, free-pass and premium-pass crystal rewards
always create **FREE** lots. Premium-pass lots require the verified entitlement's
store order and expose `purchaseLinked:true`. A missing order link fails closed.
Only a verified configured store crystal purchase creates a PAID lot.

Crystal reward lines contain `kind:"CRYSTAL"`, `id`, `quantity`, `source:"FREE"`
and `purchaseLinked`. The claim adds `wallet` with the wallet mutation result.
Recovery can explicitly configure `{kind:"CRYSTAL",walletPolicyVersion,quantity,
maxPerCampaign,missedDaysOnly:true}`. Its `cost` response contains `kind`, `id`,
`quantity`, `freeSpent`, `paidSpent`. Entire cost, wallet grant, attendance state,
inventory rewards and all ledgers share one batch. The previous ITEM recovery is
a separate generic capability; it is not the new crystal wallet.

No tutorial/offline crystal grant or silent conversion of old login/first-clear
ticket receipts is added. Existing ticket inventory, grant history and gacha
probabilities/pity remain unchanged. Switching future reward policy requires
preserving the existing one-time claim identities to prevent duplicate compensation.

<a id="source-172d7ea1b2d372f8-provenance-and-verification-boundary"></a>
#### Provenance and verification boundary

`crystal_lots` retains each FREE/PAID grant, source type/ref, optional order, initial
amount and remaining amount. `crystal_ledger` allocates each spend to concrete lots
and links downstream ticket exchanges/recovery. FREE lots are consumed before PAID.
Account mutation sequence uniqueness serializes economic writes and triggers batch
rollback/retry on concurrency; receipt fingerprint and business uniqueness prevent
double grants. The active policy is checked again inside the wallet batch.

`CRYSTAL_PURCHASE_VERIFIER` and `PASS_PURCHASE_VERIFIER` are trusted server adapters,
with no production default. They must verify real store identity/signature/API,
ownership, application/product/environment, current purchase/refund state and time.
Crystal purchase additionally requires a verified PURCHASED state. Store transactions
are globally unique per provider/application across pass and crystal purchases.
Opaque receipts are not persisted; request hashes and verified order identity are.

`reconcileCrystalOrder` is a **server-only** function requiring a trusted
`StoreRevocationVerifier`. There is no public refund route, configured webhook or
live store integration. Verified REFUNDED/REVOKED orders reconcile once: only
remaining lots linked to that order are removed, premium entitlement is revoked,
spent FREE/PAID amounts and downstream operations are recorded for support. It does
not remove exchanged tickets, recruited operators, unrelated rewards or impose a
restriction. Reconciliation uses historical order/lot policy provenance and works
when the current wallet policy is disabled, or when a pass had no crystal grants.

<a id="source-172d7ea1b2d372f8-validation-and-limits"></a>
#### Validation and limits

Local real SQL.js/API tests cover FREE/PAID separation, FREE premium order links,
free-first spend, replay/changed payload, insufficient/concurrent balance, atomic
recovery rollback, missing verifier, order replay, one-time refund/deficit retention,
disabled-policy historical refunds and entitlement-only revocation. Production
provider adapters, refunds/webhook authentication, release policies and automatic
restriction rules remain unconfigured/unimplemented. No remote migration/deploy.

<!-- source-body:source-172d7ea1b2d372f8:end -->


<a id="source-8a83ec9bc4961811"></a>
## S11 departure admission

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/departure-admission.md`  
원문 SHA-256: `0f1d0f3b4d0720f1c96cfa037dbc187bd07d17aa84eadc46b3846e34b90f987b`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-8a83ec9bc4961811:start -->
<a id="source-8a83ec9bc4961811-s11-departure-admission"></a>
### S11 departure admission

**Current public paid-start gate:** the subsequent
[run combat requirements contract](49_SERVER_API_CONTRACTS.md#source-7e409c4a47efc1a6) blocks uncommitted
S11 starts with409 combat_not_ready until approved battle bindings exist. The
transaction implementation below is retained and tested as a domain core; the
original492-test result at4ced349 proved admission, not actual Unity combat entry.
Preview now includes firstClearEligible and combatContract; owned original-run
recovery is available through GET runs/:runId/start-state. Successful old replay
remains unchanged.

Local implementation requires migrations through `0031_ultimate_selection.sql`.
CE01 advances admission to `departure-v2`. See [ultimate selection](49_SERVER_API_CONTRACTS.md#source-feaac556a37c1cd1)
for selection pin/revalidation and historical receipt compatibility.
No remote deployment or catalog activation is implied. Use S03 active operator
formation and S04 `GET /v1/trains/loadout` before requesting departure.

<a id="source-8a83ec9bc4961811-existing-operation-identity"></a>
#### Existing operation identity

The ordered story IDs remain:

1. `opn_01_last_departure`
2. `opn_02_first_signal`
3. `opn_03_water_stop`
4. `opn_04_scrapstar_show`
5. `opn_05_night_boarders`
6. `opn_06_broken_line`
7. `raid_raider_king_engine`

The six story encounters use `ORDINARY_15_WAVE`; the last entry is `RAID_BOSS`.
All earlier story IDs must exist in server `cleared_operations_json` before an
entry is admitted. The first needs no clear. This neither manufactures progress
nor remaps story nodes into the four proposed ordinary categories. Their category
IDs/catalog are still missing and are not created here.

Existing Act 1 refs are used: act `act_01_wasteland_railway`, region
`region_wasteland_railway`, content `act1.launch.content.v1`, augment/tactic package
`act1_full_start_package_v1`, five operator slots. Raid uses boss
`boss_raider_king_engine` and phases `crown_chase,trophy_break,engine_stop`.
This is the existing launch metadata contract, not a new content catalog resolver.

<a id="source-8a83ec9bc4961811-post-v1operationspreview"></a>
#### POST /v1/operations/preview

Bearer authentication and existing client admission apply. Response is `no-store`.
No query parameters. `Idempotency-Key` is 1–128 printable non-space ASCII chars.
Strict body (no extra fields):

```json
{
  "operationId": "opn_01_last_departure",
  "risk": "I",
  "expectedFormationRevision": 1,
  "expectedLoadoutRevision": 1,
  "expectedUltimateRevision": 1
}
```

Risk is existing `I|II|III|IV|V`. Revisions are positive safe integers from the
current active formation and loadout. Five unique active operators must still be
owned. Installed train/carriages must satisfy the S04 ownership, slot and unlock
rules. Missing training-core slots stay absent; no default training investment or
extra ownership is granted. ACTIVE/FINISH_PENDING/QUARANTINED runs block issuance.

Cost comes only from active `balance_config_versions.config_json.fuel.runStartCost`,
a positive integer. No hardcoded cost fallback. The active run reward policy must
parse and contain the selected operation, progression multiplier and risk. Missing
cost/reward configuration rejects issuance, preventing paid runs that cannot finish
against that policy. Stored `fuel_state.fuel` must cover cost. No fuel initialization
or regeneration is performed; `availableFuel` is explicitly the persisted amount.

```ts
type Admission = {
  version: "departure-v2";
  ultimateSelection: { rulesVersion: "ultimate-selection-v1"; operatorId: string | null; revision: number };
  formationRevision: number;
  loadoutRevision: number;
  formation: { schemaVersion: 1; operators: { slot: number; operatorId: string }[] };
  loadout: {
    rulesVersion: "train-loadout-v1"; trainId: string;
    carriages: { slot: number; carriageId: string }[];
  };
  ownedState: {
    operators: { operatorId: string; skillTraining: number; promotion: number; revision: number }[];
    trainingCoreSlots: { slot: number; operatorId: string; trainedLevel: number; revision: number }[];
    train: { trainId: string; level: number; revision: number };
    carriages: { carriageId: string; level: number; revision: number }[];
  };
  fuelCost: number;
};
type PreviewResponse = {
  accepted: true;
  operationPreviewId: string;
  operationId: string; actId: string; regionId: string;
  encounterType: "ORDINARY_15_WAVE" | "RAID_BOSS";
  raidId: string | null; bossId: string | null; bossPhaseRefs: string[];
  allowedOperatorSlots: 5;
  startAugmentPackageRef: string; startTacticPackageRef: string; contentVersion: string;
  risk: "I" | "II" | "III" | "IV" | "V";
  expiresAt: string; // ISO UTC, server issuance + 300 seconds
  formationRevision: number; loadoutRevision: number;
  admission: Admission;
  quote: { fuelCost: number; availableFuel: number; balanceVersion: string };
};
```

`ownedState` includes only the selected entities' persisted growth facts. These are
not computed combat stats or a new equipment/effect catalog. Turrets and unsupported
owned carriages remain in S04 inventory and are excluded from the installed state.

Issuance stores a durable account-scoped receipt and preview, with transaction
checks for current selection/ownership/progress/growth/policy/fuel/open run. It
does not debit or reserve fuel. Multiple outstanding previews may coexist.
Same key/body returns the exact original response even if now expired or consumed;
changed body with that key is `409 idempotency_conflict`. Use a new key to refresh an
expired quote; retry the old key after transport uncertainty to recover its result.

<a id="source-8a83ec9bc4961811-post-v1runsstart"></a>
#### POST /v1/runs/start

Existing request remains `{ "operationPreviewId": "preview_..." }`, with Bearer
and Idempotency-Key, no query or client snapshot. S11-produced rows have the
server-only `admission_version='departure-v1'` marker. Clients cannot set it.

The server revalidates current owned squad, installation, growth, progression,
stored fuel and active policy. Admission must equal the frozen quote. Active
balance version and the hash of its complete configuration must still match;
reissue after changes. A fuel balance change alone may proceed if it still covers
the quoted cost; no dynamic price is substituted. Expiry is exclusive and is
checked again with SQLite time within the transaction.

One D1 transaction inserts the account mutation, checks all guards, consumes the
preview, deducts exact cost, writes `fuel_ledger` reason `RUN_START`, creates the run,
stores frozen snapshot and start idempotency record. No separate consume/reopen.
Any failure rolls back all of those writes. Competing previews cannot open two runs;
identical concurrent start requests recover the same committed response. Guard or
sequence races retry up to four times before returning conflict.

Accepted response is the existing run-start DTO from
`run-start-augment-package.md`, plus `admission: Admission`. The same full response
is stored and returned on replay, including after finish or later policy changes.
The run snapshot preserves existing root metadata/version fields consumed by finish
and Admin. Existing finish/reward-preview logic continues to use the pinned balance.
Do not debit fuel locally again on replay; refresh state after accepted start.

Legacy NULL-marker previews retain their historical server-authored start metadata
and cost semantics (no new debit inferred from incomplete legacy data). They now
also consume/create atomically, check expiry/open runs and never reopen after
failure. New Unity integration must obtain previews from the new producer to get
S11 ownership/cost admission. This does not retrofit old snapshots or charge old runs.

<a id="source-8a83ec9bc4961811-errors-and-refresh-rules"></a>
#### Errors and refresh rules

Preview errors are `{accepted:false,errorCode:string}`. Start retains the existing
`startStatus:REJECTED` DTO with null run metadata and an errorCode/message.

| HTTP | Main errors |
| --- | --- |
| 400 | contract_violation; malformed/unknown operation/risk/revision; inherited S04 slot errors |
| 401 / admission status | Existing authentication/client admission errors |
| 403 | formation_operator_not_owned; train_not_owned; carriage_not_owned |
| 404 | operation_preview_not_found (foreign preview IDs also appear absent) |
| 409 | formation_required; formation_schema_unsupported; train_loadout_required; departure_open_run; operation_locked; departure_selection_changed; departure_quote_changed; insufficient_fuel; fuel_state_unavailable; operation_preview_not_startable; departure_preview_version_unsupported; departure_write_conflict; run_start_state_conflict; idempotency_conflict; inherited S04 eligibility errors |
| 503 | departure_cost_policy_unavailable; departure_reward_policy_unavailable |

Existing start key reused for another preview retains `400 contract_violation` in
the early replay path; a concurrent conflicting commit can return 409. Both mean
the key must not be reused for changed intent. Selection/quote errors require state
refresh and a fresh preview; transaction/transport uncertainty uses the original
key first. Insufficient fuel never consumes a preview. Expired stored receipts do
not extend expiry. A progressed account may need to fill newly unlocked S04 slots.

<a id="source-8a83ec9bc4961811-verification-and-remaining-boundaries"></a>
#### Verification and remaining boundaries

Twelve signed-auth real SQLite tests cover quote/debit/replay, strict contract,
missing ownership/policy, revisions/gates/expiry/fuel, policy changes, identical and
competing starts, snapshot rollback, commit-time fuel/growth/policy/expiry changes,
all six IDs and raid refs, atomic legacy start, and new preview→start→finish→reward
preview with pinned policy. Existing run-start metadata tests were updated to
assert atomic consumption; the sequential ordinary/raid fixture closes its first
run before starting the second. Full test/check results are in the owner handoff.

No category IDs, combat formulas, turret compatibility, Medical unlocks, duplicate
car instances, automatic fuel refill, remote migration/seed/deploy or Unity runtime
validation are supplied by this slice.

<!-- source-body:source-8a83ec9bc4961811:end -->


<a id="source-791cfbb0f74d1ee3"></a>
## Opt-in real DEV login → download → lobby: server prerequisites

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/dev-login-download-lobby-test.md`  
원문 SHA-256: `e0e9108f7bb606a8743d20b95e69220468c25be1321fa509764d287f88af0803`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-791cfbb0f74d1ee3:start -->
<a id="source-791cfbb0f74d1ee3-opt-in-real-dev-login--download--lobby-server-prerequisites"></a>
### Opt-in real DEV login → download → lobby: server prerequisites

Updated 2026-09-08 KST. Status: **BLOCKED**, not a live test success.
Canonical contract: `docs/api/client-release-registry.md` in the planning checkout.
Server implementation: `C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/attendance-pass-20260907/server`.
Unity owns the opt-in suite and real HTTP/session adapter. This document supplies
its server prerequisite and assertion contract; it is not an executable test result.

<a id="source-791cfbb0f74d1ee3-dev-policy-correction--local-only-2026-09-08"></a>
#### DEV policy correction — local only, 2026-09-08

DEV uses STABLE/DISABLED only; INTERNAL/STABLE separation and tester entitlement
are PRD-only. The local correction on codex/dev-stable-registry-20260907 is not yet
deployed. DEV2026.09.07.4 c08bfcd and D1 migrations0022–0024 were already deployed;
do not repeat that rollout. A separate approved deployment of the correction is
needed before validating the new DEV semantics remotely.
Prior deployment: [DEV2026.09.07.4 report](50_RELEASE_OPERATIONS_CONTRACTS.md#source-3a289b92502a9144)（옛 서버 폴더 링크는 원래 누락되어, 같은 주제의 보존된 기획 저장소 배포 기록으로 연결）. No registry activation,
tester grant, publication, policy seed or additional account is part of this patch.
DEV has no tester prerequisite; retain content publication/activation, exact code,
minimum, normal authentication, release pin and economy gates. Timestamped older
publication/session observations below are not a fresh remote inventory.

<a id="source-791cfbb0f74d1ee3-latest-narrow-authorization-update--2026-09-07"></a>
#### Latest narrow authorization update — 2026-09-07

Plan relayed explicit user approval for Unity code and actual DEV testing that sends
only the dedicated DEV test account session to exactly:
`https://dev-api.lastrail.app/v1/client-config/authorized`.
No other account, PRD endpoint, token logging/documentation/cross-task transmission,
new secure-storage mechanism, registry deployment or remote migration is authorized
by this update. Keep the single existing guest-creation/replay-key budget. No token
value or account credential was read or transmitted in this server checkpoint.

The missing trusted-user consent for that exact DEV adapter destination is now
provided. Unity must apply it through normal approval review. Approval arrival is
not evidence that its formerly rejected patch/test runner is applied, that tool
review now accepts it, or that a real request succeeded. AUTH_ADAPTER stays an
implementation/readiness prerequisite until Unity supplies actual verification.
Prior text asking for adapter destination consent is superseded only within this
narrow DEV scope; it must not be read as an outstanding request for the same consent.

Newer independent publication evidence was read from Unity's
`E:/Projects/LASTRAIL/unity-project/QA/Reports/dev-publication-check.json`:
2026-09-07T11:28:09.7259591Z, catalog_0.1.0.bin HTTP404, bytesMatch=false,
sha256Match=false, status=BLOCKED. This is public HTTPS evidence only; no activation
or login evidence. Server did not repeat that request. A05 publication/activation,
deployment of the DEV correction remains separate; a tester entitlement is not a DEV gate.

<a id="source-791cfbb0f74d1ee3-current-evidence-and-stop-conditions"></a>
#### Current evidence and stop conditions

| Prerequisite | Current observed state | Test decision |
| --- | --- | --- |
| Actual A05 public objects uploaded and HTTPS hashes verified | Upload previously rejected; Unity HTTPS report at11:28:09Z shows catalog404, no hash match | BLOCKED_ARTIFACT_PUBLICATION |
| versions/current.json activated by successful fresh-ETag CAS/readback | No activation performed by this server task; last observed legacy pointer had no contentRelease | BLOCKED_CONTENT_ACTIVATION |
| DEV policy correction deployed | c08bfcd registry foundation deployed; DEV STABLE correction local only | BLOCKED_DEV_POLICY_NOT_DEPLOYED for new DEV semantics |
| Tester entitlement | PRD INTERNAL only; never required in DEV | Not a DEV prerequisite |
| Unity authenticated adapter and session pin | Narrow DEV session consent supplied; patch/runner acceptance and wiring not yet verified | BLOCKED_AUTH_ADAPTER |
| Approved dedicated account | Private state checked: no account, no access token, no seed, no pull checks | Account budget unused; do not create until other prerequisites pass |

These publication/pointer observations are timestamped task evidence, not a fresh
remote inventory. No repeated remote probes, new upload, pointer write, account
creation or token transmission occurred in this checkpoint. A changed state requires
fresh authoritative readback before updating these gates.

Actual A05 release identity and public-file whitelist remain in the approved artifact
packet and SERVER_1 handoff; do not replace them with synthetic registry fixtures.
The test must take expected release identity/hash from that approved packet/readback.
Presence of a local build, DNS, TLS, R2 bucket, or a passing fixture is insufficient.

Do not silently skip and count a gated real case as passed. Emit an explicit BLOCKED
result with the prerequisite code(s); keep fixture and real-DEV totals separate.
For a legacy deployed server after actual A05 activation, a legacy smoke may be run
within its approved scope, but it cannot prove registry INTERNAL/STABLE behavior.
Do not deploy the new registry or apply remote migrations merely to make a test pass.

<a id="source-791cfbb0f74d1ee3-normal-api-sequence-after-prerequisites-are-satisfied"></a>
#### Normal API sequence after prerequisites are satisfied

1. Use configured official DEV base `https://dev-api.lastrail.app` and actual installed
   App-Version/Version-Code/Platform/Channel headers. Do not spoof a higher code,
   use account-id authority, or disable admission. No PRD calls in this DEV suite.
2. GET `/v1/client-config` without credentials. Legacy/STABLE requires accepted=true,
   clientAdmission.status=ACCEPTED and contentRelease.status=APPROVED_COMPATIBLE.
   DEV must not enter an INTERNAL AUTH_REQUIRED/tester flow. A stored DEV INTERNAL
   row is invalid policy and stops the test with version_policy_unconfigured.
   Unknown, disabled, update-required, malformed policy, offline or503 stops the flow.
3. Reuse the approved dedicated session if already created. Otherwise the sole
   approved account creator uses POST `/v1/auth/guest` with JSON `{}` and the existing
   high-entropy persisted Idempotency-Key (16–128 characters). Coordinate account
   budget ownership; Unity must not create a second guest beside the server's approved
   gacha-verification guest. No new key on timeout/response loss; no automatic guest
   recreation on401/403/5xx. Test identity/state is not a fixture account.
4. Verify HTTP200 tokenType=Bearer, account.status=ACTIVE, nonempty account.id,
   accessToken/accessTokenExpiresAt and refreshToken/refreshTokenExpiresAt. Keep
   credential material in the existing approved secure-session mechanism. Never put
   raw tokens, replay keys or credential response bodies into logs, XML or handoffs.
   POST `/v1/auth/refresh` uses `{refreshToken}` with one persisted rotation replay key
   only when restoring the existing session; it does not allocate a new account.
5. GET `/v1/profile` with that session, no body or account query. Require accepted=true
   and account.id equal to the authenticated account. Read profile.displayName. If
   already set, preserve it. If missing, use the agreed test nickname (2–16 Unicode
   characters; letters/numbers/space/_/-), GET `/v1/profile/display-name/check?displayName=...`,
   require available=true, then POST `/v1/profile/display-name` with `{displayName}`.
   GET profile again and verify the persisted normalized name/account. Do not loop
   across names or ignore cooldown/taken errors. GET `/v1/story-progress` hydrates the
   same authenticated player without granting rewards or starting gameplay.
6. On a deployed registry server, GET `/v1/client-config/authorized` with the existing
   session. Require accepted=true, ACCEPTED, APPROVED_COMPATIBLE, exact installed code
   and expected releaseId/catalogHash. DEV requires no tester entitlement. Keep the quoted
   registryRevision for revalidation and bind releaseId to this session.
7. Download only the authorized catalog/bundles. Do not forward API Bearer credentials
   to the public asset host. Verify approved catalog identity and required bundle
   completion. Preserve network consent and cancellation behavior; no Editor-local
   fallback can count as an actual remote download.
8. Revalidate the current authorized release after download; stop on changed policy,
   disabled/update-required code, changed identity or session logout. Retain
   `X-LastRail-Content-Release-Id` for registry-protected gameplay requests. Enter Main
   and lobby only with the accepted session/profile/name/story and verified content.
   Record actual scene/lobby observations; a downloaded byte count alone is not lobby success.

The smoke does not claim login bonuses, attendance, pass rewards, crystals, purchases,
ticket exchanges or draws. The separately approved 11-ticket/1+10-draw budget remains
unchanged and is not seeded or consumed by a lobby test.

<a id="source-791cfbb0f74d1ee3-approval-holds-already-recorded"></a>
#### Approval holds already recorded

The publication tool rejected the A05 upload because trusted-user authorization did
not identify the exact proprietary payload and R2 destination sufficiently. Required
scope is the existing A05 packet's 11 public files (85,341,920 bytes), only beneath its
approved release namespace in `lastrail-content-dev`; activation is a separate guarded
write to `lastrail-versions-dev/versions/current.json`. No alternative CLI/channel retry
has been used to bypass that rejection.

Unity's latest handoff separately records rejection of code sending session Bearer
credentials to client-config/authorized. Its exact adapter/token-destination approval
must be resolved through the normal review path by Unity. This server document neither
clears that rejection nor expands DEV testing to PRD, secret storage, deployments or schemas.

<a id="source-791cfbb0f74d1ee3-verified-local-evidence-separate-from-remote"></a>
#### Verified local evidence, separate from remote

Server registry suite checkpoint:67 files /394 tests passed,4 TypeScript projects and
lint passed; actual local D1 migration0024, FK clean, quick_check OK, tester grants0.
Those results validate local contracts and guards only. This documentation checkpoint
adds no code changes and did not rerun that suite or claim remote advancement.

<!-- source-body:source-791cfbb0f74d1ee3:end -->


<a id="source-71be40c88cd4ae29"></a>
## S03 owned squad save and activation

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/formation.md`  
원문 SHA-256: `86b3fe3478cfe1d93076eae7862ea75a98274deb70b3013102970c6e2a829e84`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-71be40c88cd4ae29:start -->
<a id="source-71be40c88cd4ae29-s03-owned-squad-save-and-activation"></a>
### S03 owned squad save and activation

Local code/document contract, migration0028 required, not deployed. Scope is five
operator IDs and their positions only. Train/equipment/package/ultimate selection,
coop readiness, run snapshots, preset deletion and optimal-power autofill remain
outside this implemented slice. The proposed three-slot product limit is not
silently activated. No resource cost, inventory grant or training-core replacement.

Both routes use existing session/account authentication and client admission.
Require an ASCII printable Idempotency-Key (1–128 chars), no query parameters, a
bounded JSON body (18KB maximum), strict fields. presetId is a stable client-chosen
ID matching `[A-Za-z0-9_-]{1,100}`; use distinct IDs for new presets. An ID already
owned by another account cannot be overwritten or read through these routes.

`POST /v1/formation/presets/:presetId/save`

```json
{"expectedRevision":0,"name":"Squad","operators":[{"slot":0,"operatorId":"op0"},{"slot":1,"operatorId":"op1"},{"slot":2,"operatorId":"op2"},{"slot":3,"operatorId":"op3"},{"slot":4,"operatorId":"op4"}]}
```

Revision0 creates; an update requires the exact current saved revision. Name is
trimmed,1–80 characters. Exactly five unique positions0–4 and five distinct owned
operators are required. Order in storage is canonical slot order. Server validates
ownership again in the committing transaction. Injected HP, levels, prices, account
IDs, role/partner data, training investments and unsupported selections are rejected.
Success includes accepted:true, receiptId:string, kind:"SAVE", presetId:string,
presetRevision:number, refreshRequired:true. Saving does not activate or modify an
existing active squad; preference saves are allowed during a run.

`POST /v1/formation/presets/:presetId/activate`

```json
{"expectedRevision":1,"expectedFormationRevision":0}
```

expectedRevision refers to the saved preset; expectedFormationRevision comes from
account-state.formation.revision (0 before activation). Activation validates the
versioned saved definition, current ownership, both revisions and absence of an
ACTIVE/FINISH_PENDING/QUARANTINED run, then copies the selection. The transaction
rechecks these predicates. Saving later edits cannot modify the active copy.
Success adds formationRevision:number, formationChanged:boolean, operators:array to
the common receipt fields. Identical preset/revision/selection activation leaves
formation revision unchanged and formationChanged=false; its request still receives
a durable receipt. No readiness changes or run start occur in this endpoint.

Read current state and saved revisions through GET /v1/account/state. All active
fields are nested under formation, as specified in account-state.md. Stored
definitionJson is a string containing `{schemaVersion:1,operators:[...]}`.
Legacy incompatible/malformed definitions remain readable, but cannot activate.
Explicitly save a valid owned selection with the existing revision to replace one.

<a id="source-71be40c88cd4ae29-errors-and-retry"></a>
#### Errors and retry

400 contract_violation: malformed fields/key, duplicate/incomplete positions or IDs.
403 formation_operator_not_owned: missing actual ownership. Authentication codes
are inherited unchanged.409: preset_revision_conflict, preset_not_found,
preset_schema_unsupported, formation_revision_conflict, formation_open_run,
formation_write_conflict, idempotency_conflict. Unknown DB failures return500 and
leave the atomic transaction uncommitted.

Same account/key/action payload returns the stored receipt before stale revision
checks. Same key with changed payload or another action returns409. Concurrent
different writes cannot overwrite a newer revision. On transport uncertainty retry
the original payload/key; on409 refresh and preserve the editable draft. A new
intent after resolving a conflict needs the refreshed revision and a new key.
All selection writes, mutation sequence and receipt commit together; no partial
save/activation is reported successful. Refresh common state after accepted writes.

Eleven SQLite integration tests cover save-vs-activate separation, snapshot refresh,
invalid/foreign ownership and colliding IDs, stale and concurrent revisions, exact
replay, ownership/run-start races, unchanged activation, legacy definitions and
injected receipt-write rollback. They do not prove Unity/remote runtime behavior.

<!-- source-body:source-71be40c88cd4ae29:end -->


<a id="source-58f169eb9f58887d"></a>
## Gacha / Pity / Result History DB Design Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/gacha-pity-result-history.md`  
원문 SHA-256: `e95f27e5d1dc59fe8c40df1411ea7d2bf783c98b41836c630bcd17eeaa9377a3`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-58f169eb9f58887d:start -->
<a id="source-58f169eb9f58887d-gacha--pity--result-history-db-design-draft"></a>
### Gacha / Pity / Result History DB Design Draft

This draft defines the DB shape for operator recruitment history, banner pity
state, duplicate conversion, and economy/inventory ledger linkage.

It is a design draft for the next migration/API phase. It does not implement
pull probability logic, payment processing, or admin UI.

Future pseudo-random distribution (PRD) rarity selection is drafted separately
in `docs/api/gacha-prd-rarity-selection.md`. The PRD draft keeps hard
`guaranteeType` semantics and the current response shape unchanged.

<a id="source-58f169eb9f58887d-goals"></a>
#### Goals

- Store gacha history in D1 as durable Source of Truth.
- Restore banner-specific pity/guarantee state per account.
- Preserve both pull batch/request history and individual pull result history.
- Link command ticket spend and granted duplicate/material rewards to ledgers.
- Support user support, recovery, probability audit, integrity review, and
  future admin screens.

<a id="source-58f169eb9f58887d-existing-schema-compatibility"></a>
#### Existing Schema Compatibility

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

<a id="source-58f169eb9f58887d-stable-ids"></a>
#### Stable IDs

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

<a id="source-58f169eb9f58887d-new-inventory-ledger"></a>
#### New Inventory Ledger

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

<a id="source-58f169eb9f58887d-banner-definitions"></a>
#### Banner Definitions

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

<a id="source-58f169eb9f58887d-account-banner-pity-state"></a>
#### Account Banner Pity State

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

<a id="source-58f169eb9f58887d-pull-batch--request-history"></a>
#### Pull Batch / Request History

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

<a id="source-58f169eb9f58887d-individual-pull-results"></a>
#### Individual Pull Results

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

<a id="source-58f169eb9f58887d-transaction-shape"></a>
#### Transaction Shape

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

<a id="source-58f169eb9f58887d-pull-api-contract"></a>
#### Pull API Contract

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

PRD rarity selection, if implemented later, must run only before ordinary
weighted selection when `guaranteeType = 'NONE'`. Hard guarantee thresholds
remain higher priority than PRD.

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

<a id="source-58f169eb9f58887d-error-codes"></a>
#### Error Codes

Recoverable:

```text
gacha_state_conflict
gacha_temporarily_unavailable
```

`gacha_state_conflict` is also a minimal ops error write candidate only when a
server-side commit batch fails after validation. It is not written for normal
validation rejects such as invalid pull count, inactive banners, missing
banners, insufficient command tickets, or duplicate replay.

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

<a id="source-58f169eb9f58887d-admin--analytics-read-model"></a>
#### Admin / Analytics Read Model

The write model above now backs launch read-only Admin support endpoints:

```text
GET /v1/admin/gacha/banners
GET /v1/admin/gacha/accounts/:accountId/pity
GET /v1/admin/gacha/pull-batches
GET /v1/admin/gacha/pull-batches/:batchId
```

Launch scope exposes banner status, pickup operator id, command-ticket cost,
pull count options, pity thresholds, account pity state, batch cost refs,
pre/post pity state, client-visible response result shape, guarantee types,
duplicate conversion summaries, safe ledger references, and detail
`causeEvidence` assembled from stored server records. The evidence is read-only
support context: source, account/banner/batch refs, replay-known state,
pre/post pity snapshots, cost/result ledger refs, and duplicate conversion item
summaries.

It does not expose raw RNG material, RNG seed preimages, per-candidate
probabilities, full weight lists, token/secret/account shortcut data, reroll,
repair, replay, or economy correction controls.

Future analytics can still add:

- Pull volume by banner.
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

<a id="source-58f169eb9f58887d-migration-impact"></a>
#### Migration Impact

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

<a id="source-58f169eb9f58887d-launch-data-fixture"></a>
#### Launch Data Fixture

`launchGachaDataDraft` in `@lastrail/balance` is a
`TESTING_TUNING_DRAFT` fixture for local contract and response stability smoke.
It is not a production balance seed, not a DEV remote seed, and not a final
economy approval.

Current launch visual scope:

```text
op_vex
op_nyx
op_sable
op_marrow
op_iris
op_rail
op_anchor
op_lumen
op_cinder
op_warden
```

The draft pickup banner keeps:

- `bannerType = PICKUP`
- `pickupOperatorId = op_vex`
- `pityConfig = { rare: 10, elite: 30, pickup: 60 }`
- `cost.itemId = cur_command_ticket`
- `allowedPullCounts = [1, 10]`
- all 10 Act 1 elite operator IDs in the elite operator pool

Launch command ticket acquisition is a non-repeat local launch balance
contract, not a remote production seed deployment:

```text
source                         item                quantity  repeatable
launch_login_bonus             cur_command_ticket         5  no
opn_03_water_stop first clear  cur_command_ticket         1  no
opn_05_night_boarders first    cur_command_ticket         1  no
opn_06_broken_line first       cur_command_ticket         2  no
story raid first clear         cur_command_ticket         3  no
```

Total non-repeat launch supply is 12 `cur_command_ticket`. The first-clear
portion is emitted as server-owned `ITEM` reward lines from
`launchRunRewardsConfig` and committed through `account_items`/`item_ledger`
during reward claim. The login bonus portion is delivered by
`POST /v1/login-bonus/claim` and guarded by `account_login_bonus_claims`.
Normal offline repeat rewards must not include `cur_command_ticket`,
`cur_command_ticket_fragment`, or `cur_special_core`.
World Raid score, ranking, settlement, and event reward tickets are separate
future scope and must not be mixed into the Act 1 story/pickup banner launch
fixture.

Local integration smoke uses the `launchGachaDataDraft` pool as
`gacha_banners.pool_snapshot_json` input and asserts the response keeps
Unity-visible fields stable while not returning raw RNG material, seed
preimages, per-candidate probabilities, full weight tables, tokens, secrets, or
account shortcut data.

DEV remote launch smoke uses a separate deterministic fixture in
`scripts/cloudflare/dev-remote-launch-smoke.ts`. That script creates banner ids
with `dev_smoke_banner_<suffix>`, calls `seedGacha(...)`, grants the smoke
account 12 command tickets, and seeds a single-candidate pickup pool containing
`op_vex`. If a designated DEV smoke batch for a `dev_smoke_banner_<suffix>`
returns repeated `op_vex` results, the source-level explanation is the
deterministic smoke seed, not production gacha probability evidence.

Decision queue:

- Final launch banner IDs, pickup rotation, and exact weight tuning.
- Remote production seed/config deployment still requires explicit approval
  and Unity/Admin compatibility smoke.

<!-- source-body:source-58f169eb9f58887d:end -->


<a id="source-478abd9f306bd26e"></a>
## Gacha PRD Rarity Selection Design Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/gacha-prd-rarity-selection.md`  
원문 SHA-256: `bc197ebe1f2a95bd53f114b2014fb3b00954ad6f5cfb0869ad7ecbf8e22deb25`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-478abd9f306bd26e:start -->
<a id="source-478abd9f306bd26e-gacha-prd-rarity-selection-design-draft"></a>
### Gacha PRD Rarity Selection Design Draft

This draft defines a future pseudo-random distribution (PRD) policy for gacha
rarity selection. It is a design/contract draft only. It does not implement PRD,
change D1 migrations, change API response shape, deploy Workers, create
Cloudflare resources, or alter the current weighted RNG + guarantee behavior.

Current compatibility anchors:

- Weighted RNG + guarantee enforcement: `0e998a5`.
- Response compatibility smoke: `b8c2340`.
- Client-visible `guaranteeType` values remain:
  - `NONE`
  - `RARE_OR_HIGHER_10`
  - `ELITE_30`
  - `PICKUP_ELITE_60`
- Raw RNG material, seed preimages, probability values, weighted internal
  calculations, tokens, secrets, and account shortcut data are never returned.

<a id="source-478abd9f306bd26e-goals"></a>
#### Goals

- Make perceived gacha variance less harsh before hard pity thresholds.
- Keep hard guarantee thresholds as the final Source of Truth.
- Keep Unity and Admin display contracts stable.
- Preserve deterministic server-side replay from committed batch history.
- Keep auditability without exposing raw RNG or probability internals to
  clients.

<a id="source-478abd9f306bd26e-non-goals"></a>
#### Non-goals

- No implementation in this slice.
- No API response shape change.
- No new migration in this slice.
- No remote DEV secret, deploy, or smoke.
- No PRD/STAGING resource creation or deployment.
- No Unity-side probability, pity, guarantee, or RNG calculation.

<a id="source-478abd9f306bd26e-recommendation"></a>
#### Recommendation

Use PRD only at the rarity selection layer for the first implementation.

Recommended first shape:

1. Determine whether a hard guarantee fires.
2. If a guarantee fires, use the current forced-selection path.
3. If no guarantee fires, use PRD-adjusted rarity selection.
4. After rarity is selected, choose the actual result candidate from
   `pool_snapshot_json.results[]` using the existing candidate weights within
   the selected rarity.
5. Keep pickup elite handling as a rarity/result eligibility concern:
   - hard `PICKUP_ELITE_60` stays forced;
   - non-guaranteed pickup preference can later be modeled as a pickup result
     weight inside the elite candidate pool, not as a client-visible response
     field.

This keeps result/item/operator candidate weighting intact and limits PRD to
the player-facing variance problem: "how long since a meaningful rarity
appeared?"

<a id="source-478abd9f306bd26e-selection-flow"></a>
#### Selection Flow

Current hard guarantee order must remain:

1. `PICKUP_ELITE_60`
2. `ELITE_30`
3. `RARE_OR_HIGHER_10`
4. `NONE`

Future PRD flow:

```text
for each pull:
  guaranteeType = getNextGachaGuaranteeType(...)

  if guaranteeType != NONE:
    candidatePool = eligible results for guaranteeType
    result = weighted pick from candidatePool
    selectionReason = guaranteeType
  else:
    rarity = selectRarityWithPrd(...)
    candidatePool = positive-weight results matching rarity
    result = weighted pick from candidatePool
    selectionReason = PRD or NONE internal reason

  apply result to pity counters and PRD counters before the next pull
```

Important boundaries:

- Hard guarantee always beats PRD.
- PRD never extends, disables, or weakens 10/30/60 hard thresholds.
- 10-pull batches update counters after each pull, so later pulls in the same
  request use the updated pity/PRD state.
- Duplicate replay returns the stored response shape and never re-runs PRD.

<a id="source-478abd9f306bd26e-prd-application-position"></a>
#### PRD Application Position

<a id="source-478abd9f306bd26e-rarity-only-prd"></a>
##### Rarity-only PRD

Apply PRD to the server's rarity decision before choosing a concrete result.

Pros:

- Directly targets perceived droughts for rare-or-higher, elite, and pickup
  elite outcomes.
- Keeps existing item/operator weights meaningful.
- Easier to test with long-run rarity distribution smoke.
- Avoids changing Unity/Admin response shape.
- Keeps `guaranteeType` semantics clean: it still means hard pity/guarantee,
  not soft PRD.

Cons:

- Requires a new internal rarity-selection stage before current result
  selection.
- Balance config needs explicit base rarity weights or derivation from the
  frozen pool.

Recommendation: use this first.

<a id="source-478abd9f306bd26e-result-level-prd"></a>
##### Result-level PRD

Apply PRD directly to individual result weights.

Pros:

- One weighted selection step can choose both rarity and result.
- Can boost specific result families.

Cons:

- Harder to reason about support/audit questions.
- More likely to distort item/operator pool weights.
- Makes pickup and duplicate behavior harder to tune.
- More complicated to prove Unity-visible shape is stable under replay.

Recommendation: do not use for the first PRD slice.

<a id="source-478abd9f306bd26e-pickup-multiplier-after-rarity"></a>
##### Pickup Multiplier After Rarity

After elite rarity is selected, apply a pickup multiplier among elite
candidates.

Pros:

- Keeps pickup tuning separate from general elite tuning.
- Can support soft pickup preference before `PICKUP_ELITE_60`.

Cons:

- Needs careful wording so players do not confuse soft pickup preference with
  hard pickup guarantee.
- Requires extra audit fields if operators need to distinguish soft pickup
  bias from normal elite selection.

Recommendation: keep as a later balance option. The first PRD design should
not require it.

<a id="source-478abd9f306bd26e-counter-model"></a>
#### Counter Model

Hard pity counters already exist in `player_gacha_pity`:

- `elite_pity_count`
- `pickup_elite_pity_count`
- `rare_or_higher_pity_count`
- `total_pull_count`

PRD needs soft counters with similar reset semantics:

- `rare_or_higher_prd_count`: pulls since rare-or-higher result.
- `elite_prd_count`: pulls since elite result.
- `pickup_elite_prd_count`: pickup banner pulls since pickup elite result.

Recommended storage for first implementation:

- Add `prd_state_json` to `player_gacha_pity` in a future migration, or use a
  separate `player_gacha_prd_state` table if the state grows beyond counters.
- Keep hard pity columns as-is. Do not overload hard pity counters with PRD
  tuning state.
- Store pre/post PRD state in `gacha_pull_batches.result_summary_json` or a
  future `pre_prd_json` / `post_prd_json` only if support/audit needs it.

Why not reuse hard pity counters only?

- Hard pity counters are player-facing guarantee state.
- PRD counters may have curve/table-specific behavior and should be adjustable
  without changing hard guarantee semantics.
- Separating them reduces the chance that a future tuning change accidentally
  breaks 10/30/60 guarantees.

Reset semantics:

- Rare-or-higher result resets rare PRD counter.
- Elite result resets elite PRD counter and rare PRD counter.
- Pickup elite result resets pickup elite PRD counter, elite PRD counter, and
  rare PRD counter.
- Non-matching result increments the relevant PRD counters by one.
- For STANDARD banners, pickup elite PRD counter should stay absent or zero.

<a id="source-478abd9f306bd26e-probability-adjustment-options"></a>
#### Probability Adjustment Options

<a id="source-478abd9f306bd26e-linear-multiplier"></a>
##### Linear Multiplier

Increase the target rarity's effective weight linearly with the relevant PRD
counter.

Example concept:

```text
effectiveRareWeight = baseRareWeight * (1 + rarePrdCount * rareStep)
```

Pros:

- Simple to explain and test.
- Good first version for predictable tuning.
- Easy to cap before hard pity thresholds.

Cons:

- Can feel too mechanical near thresholds.
- Needs caps so soft odds do not become too aggressive before hard guarantees.

Recommendation: preferred first implementation style.

<a id="source-478abd9f306bd26e-curvetable-multiplier"></a>
##### Curve/Table Multiplier

Read a multiplier table from `pity_config_json` or balance config.

Example concept:

```json
{
  "prd": {
    "rareMultiplierByCount": [1.0, 1.0, 1.05, 1.1],
    "eliteMultiplierByCount": [1.0, 1.0, 1.02],
    "pickupEliteMultiplierByCount": [1.0]
  }
}
```

Pros:

- Most controllable for live balancing.
- Can shape early/mid/late pull feel without formula changes.
- Good for audit because the applied table can be frozen per banner.

Cons:

- More balance data to validate.
- Easy to create accidental cliffs if tables are edited poorly.

Recommendation: phase 2 or use table-backed values after the linear model is
validated.

<a id="source-478abd9f306bd26e-additive-weight-bonus"></a>
##### Additive Weight Bonus

Add a flat bonus to target rarity weight based on counter.

Pros:

- Easy to implement against integer weights.
- Avoids floating point if all weights are scaled integers.

Cons:

- Distorts low-probability and high-probability banners differently.
- Harder to reason about across banners with different base weights.

Recommendation: acceptable only if the whole gacha model uses integer rarity
weights and balance wants explicit additive tuning.

<a id="source-478abd9f306bd26e-recommended-config-shape"></a>
#### Recommended Config Shape

For a future implementation, store PRD config under frozen
`gacha_banners.pity_config_json` so historic pulls remain auditable.

Candidate:

```json
{
  "rare": 10,
  "elite": 30,
  "pickup": 60,
  "prd": {
    "enabled": true,
    "mode": "LINEAR_MULTIPLIER",
    "rareOrHigher": {
      "startsAfter": 2,
      "stepBps": 500,
      "maxMultiplierBps": 30000
    },
    "elite": {
      "startsAfter": 10,
      "stepBps": 250,
      "maxMultiplierBps": 25000
    },
    "pickupElite": {
      "startsAfter": 20,
      "stepBps": 200,
      "maxMultiplierBps": 20000
    }
  }
}
```

Notes:

- Basis points avoid floating point storage and make tests deterministic.
- `startsAfter` prevents early-pull odds from changing immediately.
- `maxMultiplierBps` caps soft odds below hard guarantee thresholds.
- Exact numbers above are illustrative design-shape examples only, not
  approved balance values.

<a id="source-478abd9f306bd26e-storage--audit-candidates"></a>
#### Storage / Audit Candidates

Keep existing guarantees:

- `rng_seed_hash` remains a hash/commitment only.
- Raw RNG material is not stored or returned.
- Raw seed/preimage is not stored or returned.
- Probability and internal weighted calculation details are not returned.

Candidate stored audit fields:

- `prd_state_json` in `player_gacha_pity` for current soft-counter state.
- `selection_reason_json` in `gacha_pull_results` for sanitized internal audit.
- `prd_debug_json` in `gacha_pull_batches` only if admin probability audits
  need batch-level metadata.

Recommended first implementation:

- Do not add client-visible fields.
- Prefer storing a sanitized `selection_reason_json` per result only after the
  DB migration is approved.
- Keep it compact and non-sensitive:

```json
{
  "kind": "PRD_RARITY",
  "rarity": "ELITE",
  "counterBefore": 18,
  "mode": "LINEAR_MULTIPLIER",
  "configVersion": 1
}
```

Do not store:

- rolled random value;
- raw seed;
- per-candidate probability;
- full candidate weight list;
- token/secret/account shortcut data.

Admin display recommendation:

- Continue exposing `guaranteeType` for hard guarantees.
- If a future read-only admin detail needs PRD context, expose
  `selectionReason.kind` such as `PRD_RARITY` or `WEIGHTED_RARITY`.
- Do not show raw probability in Admin by default.

<a id="source-478abd9f306bd26e-unity--admin-impact"></a>
#### Unity / Admin Impact

Unity:

- No calculation.
- No request body change.
- No response shape change in this design.
- Continue treating `guaranteeType` as the hard guarantee display field.
- Continue treating `DUPLICATE_REPLAY` as previous result sync.
- Continue excluding raw RNG, probability, weighted internals, tokens, and
  secrets from diagnostic JSON.

Admin:

- Current gacha read-only skeleton and labels remain compatible.
- Future admin details may show PRD reason as sanitized audit metadata.
- Admin should not imply PRD is a hard guarantee.
- Unknown backend codes should continue to fall back to raw code display.

Future optional display field:

```text
selectionReason
```

Do not add this field to live Game API responses in the first PRD design. If it
is added later, it must go through Unity/Admin adapter updates and compatibility
smoke first.

<a id="source-478abd9f306bd26e-test--smoke-candidates"></a>
#### Test / Smoke Candidates

When implementation begins, add tests before changing selection code.

Long-run distribution sanity:

- Use deterministic seeds over many pulls.
- Verify rare/elite/pickup observed rates remain within broad expected bands.
- Verify PRD does not make impossible rarity candidates appear.
- Verify raw RNG/probability fields are absent from responses.

Rare 10-pull interaction:

- Build state close to, but not at, `RARE_OR_HIGHER_10`.
- Verify PRD can affect ordinary rarity selection before threshold.
- Verify threshold pull still returns `RARE_OR_HIGHER_10` forced guarantee.
- Verify rare-or-higher result resets hard pity and PRD counters.

Elite 30 interaction:

- Build state close to `ELITE_30`.
- Verify PRD may soften elite drought before threshold.
- Verify threshold pull always returns `ELITE_30`.
- Verify elite result resets elite and rare counters.

Pickup elite 60 interaction:

- Pickup banner only.
- Verify PRD cannot override `PICKUP_ELITE_60`.
- Verify threshold pull always picks pickup elite eligible result.
- Verify pickup elite result resets pickup/elite/rare counters.

Duplicate replay compatibility:

- Commit a PRD-affected pull.
- Replay with same `Idempotency-Key`.
- Verify response shape is exact previous result sync.
- Verify command tickets are not spent again.
- Verify operators/items/currencies are not granted again.
- Verify PRD counters and hard pity counters are not changed again.

Rejected shape:

- Keep current stable error codes:
  - `auth_required`
  - `insufficient_command_ticket`
  - `banner_inactive`
  - `invalid_pull_count`
  - `contract_violation`
  - `gacha_pool_unavailable`
  - `gacha_state_conflict`
  - `gacha_temporarily_unavailable`

<a id="source-478abd9f306bd26e-migration-impact"></a>
#### Migration Impact

No migration in this design slice.

Future migration candidates:

- `player_gacha_pity.prd_state_json`
- `gacha_pull_results.selection_reason_json`
- Optional `gacha_pull_batches.prd_debug_json` only if batch-level support
  metadata is required.

Do not change existing `guarantee_type` enum for PRD. `guaranteeType` is for
hard guarantee/pity triggers only.

<a id="source-478abd9f306bd26e-open-decisions"></a>
#### Open Decisions

- Exact PRD curve values and whether they live in `pity_config_json` or the
  broader balance config payload.
- Whether first implementation uses formula-only `LINEAR_MULTIPLIER` or a
  table-backed curve.
- Whether `prd_state_json` should be a JSON column or a separate table.
- Whether sanitized `selection_reason_json` is needed at launch or can wait for
  Admin probability audit tooling.
- Whether pickup soft preference is needed before hard `PICKUP_ELITE_60`.
- Whether a future client-visible `selectionReason` field is useful enough to
  justify Unity/Admin response contract updates.

<!-- source-body:source-478abd9f306bd26e:end -->


<a id="source-a9ca32f7cd7e7623"></a>
## S05 recruitment reads and sparse-result recovery

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/gacha-reads.md`  
원문 SHA-256: `bee1747fe4270dc7c8622abbcca9750870bd16ce07419a4d9c513727ccce726f`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-a9ca32f7cd7e7623:start -->
<a id="source-a9ca32f7cd7e7623-s05-recruitment-reads-and-sparse-result-recovery"></a>
### S05 recruitment reads and sparse-result recovery

Local code/document verification unit based9c68601; not deployed. No new migration,
banner activation, fixture seed, price or probability policy. Existing gacha tables
through0021 are used; the complete current server still requires0028 for S03/S10.
All four GET routes use existing bearer/session/account authentication and client
admission. Cache-Control:no-store. GET never rolls RNG, spends tickets, grants an
item/operator or rewrites history. Server-owned policy/receipts are the only inputs.

<a id="source-a9ca32f7cd7e7623-active-banners-and-detail"></a>
#### Active banners and detail

`GET /v1/gacha/banners?limit=20&afterBannerId=...`

Only current ACTIVE windows are listed: startsAt inclusive, endsAt exclusive;
endsAt:null means no scheduled end. Draft, disabled, future, ended and inaccessible
DEV diagnostic banners are omitted. limit1–50, default20. ID ascending keyset;
nextAfterBannerId:string|null points to a visible banner, never a hidden scope ID.
Only send afterBannerId when continuing a page; omitted means the beginning.

`GET /v1/gacha/banners/:bannerId`

Active, upcoming or ended published banners can be read; draft/disabled/missing or
inaccessible diagnostic ID returns404 banner_not_found. Diagnostic listing/detail
uses the existing DEV/account/expiry/pool scope verifier. Current account pity is
read alongside each banner snapshot; absent pity is zero/revision0, not a write.

Envelope: list has accepted:true, serverTime:UTC ISO, banners:array,
nextAfterBannerId:string|null. Detail has accepted:true, serverTime, banner:object.
An empty active list is banners:[] with null cursor, not a sample banner.

Each banner object has:

| Field | Type/meaning |
| --- | --- |
| bannerId, bannerType | string; STANDARD or PICKUP |
| balanceVersion | string; current banner policy version |
| pickupOperatorId | string or null |
| startsAt, endsAt | UTC ISO string; endsAt nullable |
| availability | ACTIVE, UPCOMING or ENDED |
| prices[] | `{pullCount:1 or10,itemId:"cur_command_ticket",quantity:1 or10}` |
| pityConfig | `{elite:number,pickup:number,rare:number}` actual existing parser thresholds/fallbacks |
| pityState | totalPullCount, elitePityCount, pickupElitePityCount, rareOrHigherPityCount, revision (numbers) |
| nextGuarantee | NONE, RARE_OR_HIGHER_10, ELITE_30 or PICKUP_ELITE_60; same enum/precedence as actual pull |
| probabilities[] | resultId/resultType/resultRarity strings, quantity:number, isPickup:boolean, baseProbability:number,nextPullProbability:number |

Probabilities are fractions0–1, not percentages. Multiply by100 for display.
baseProbability is the no-guarantee distribution; nextPullProbability applies the
current next guarantee. It is not a fixed probability for every position in a
ten-pull: each committed result changes pity. The code injects the existing pool
parser, guarantee eligibility/precedence and effective weight helpers used by POST.
Internal relative weights, RNG and policy JSON are not public response fields.
Invalid/unusable pools or malformed policy return503, never invented odds.
Ticket price is the actual existing pull cost. Crystal-to-ticket exchange remains
the separate wallet policy/endpoint and is not a new cash recruitment product.

Probability row example (illustrative type shape, not activated policy):

```json
{"resultId":"op_vex","resultType":"ELITE_OPERATOR","resultRarity":"ELITE","quantity":1,"isPickup":true,"baseProbability":0.005,"nextPullProbability":1}
```

<a id="source-a9ca32f7cd7e7623-owned-history-and-batch-recovery"></a>
#### Owned history and batch recovery

`GET /v1/gacha/history?limit=20&beforeBatchId=...`

Returns accepted:true, batches:array, nextBeforeBatchId:string|null. Order is
createdAt DESC then batchId DESC, including equal timestamps. Cursor must refer
to this authenticated account's batch; invalid/foreign cursor400 invalid_history_cursor.
limit1–50, default20. No account selection parameter is accepted.

`GET /v1/gacha/batches/:batchId`

Returns accepted:true plus one batch at the response root. Unknown/foreign batch
returns404 gacha_batch_not_found. Owned historical receipts remain readable after
the banner ends, independently of new-pull eligibility; they do not expose the
current hidden policy or allow another roll. History is account-scoped, including
an account's own old diagnostic receipts, not an admin history viewer.

Each batch: batchId, bannerId, pullCount:number, balanceVersion:string|null,
cost:{itemId:string,quantity:number}, createdAt:UTC ISO, postPityState:object|null,
results:array, recoveryStatus:COMPLETE|INCOMPLETE, missingSequences:number[],
unknownQuantitySequences:number[]. New batches pin balanceVersion in the receipt;
older summaries without that field returnnull rather than relabel history with the
banner's current version. Invalid historical pity staysnull. No current pity is
substituted for the stored post-pull state.

Results retain existing pullSequence/resultRarity/resultType/resultId/isPickup/
guaranteeType/wasDuplicate/duplicateConversion/reward fields. Valid saved response
entries are merged by1-based sequence with committed result rows. Sparse or malformed
entries fall back to those rows and the account-matched result item-ledger delta.
Full saved rewards can recover a missing row. Conflicting known row quantities
are not overwritten from the summary. Only whitelisted public fields are returned.

Missing result rows plus missing saved entries appear in missingSequences. Missing
material quantity is reward.quantity:null, never guessed as1. If a duplicate's
conversion identity is missing/malformed, its reward is
`{kind:"UNKNOWN",id:null,quantity:null}`: a token amount must never become an
operator count. Such sequences appear in unknownQuantitySequences. COMPLETE refers
to recovered result coverage/quantities, not proof that the client received the
original network response. INCOMPLETE never creates replacement grants.

Example missing result (fixture identifiers, existing batch already paid):

```json
{"accepted":true,"batchId":"batch_example","bannerId":"banner_example","pullCount":1,"balanceVersion":null,"cost":{"itemId":"cur_command_ticket","quantity":1},"createdAt":"2026-09-10T06:00:00.000Z","postPityState":null,"results":[],"missingSequences":[1],"unknownQuantitySequences":[],"recoveryStatus":"INCOMPLETE"}
```

<a id="source-a9ca32f7cd7e7623-client-reconciliation-and-compatibility"></a>
#### Client reconciliation and compatibility

Existing POST /v1/gacha/banners/:bannerId/pulls remains the only draw operation.
For an uncertain POST, retry its original banner/count/idempotency key. Changed
count on the same key now409 idempotency_conflict. Replay uses the same sparse
recovery helper/ledger join and newly pinned balanceVersion; it does not spend or
grant again. Existing DUPLICATE_REPLAY envelope remains unchanged. Legacy POST
receipts without a version retain their prior banner-version fallback; the new
history route explicitly reportsnull for that unknown historical version.

When a successful/replayed response lacks slots or quantities, read its batch ID.
Replace that batch's displayed results with the recovered snapshot, do not add
them to inventory again. For INCOMPLETE retain an unresolved state and use normal
support/reconciliation; never reroll with a new key just to fill the animation.
Refresh S01 account state after confirmed completion. Apply the established
account/session generation and latest-request checks to all responses.

No GET body; only the documented pagination keys are accepted, duplicates rejected.
Malformed query/path400 contract_violation. Existing auth/admission errors remain.
No storefront names/images or unapproved future catalog fields are fabricated.

<a id="source-a9ca32f7cd7e7623-executed-verification"></a>
#### Executed verification

Eight new real-SQLite/actual signed-session tests: visible windows/prices/no writes,
sparse seven-item recovery and POST replay without a second grant, equal-time
history pagination/account isolation, approved pool4% elite total and forced pickup
parity with the actual draw, unknown quantities, revoked auth/hidden cursor IDs,
missing duplicate conversion, and pinned versions/public field filtering. Existing
actual gacha1/10/guarantee/replay tests pass. Full suite74files470tests PASS;
Game API TypeScript and source ESLint PASS. Independent review found the missing
conversion identity issue; it was reproduced then fixed with UNKNOWN/INCOMPLETE.
No Unity/device or remote deployment verification is claimed by this server unit.

<!-- source-body:source-a9ca32f7cd7e7623:end -->


<a id="source-507f5957b54c021d"></a>
## Game API

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/game-api.md`  
원문 SHA-256: `9462a79d8a7c5e998a0affc994823eae87f19bdfc6a91e4eb04fcafcd3aa5ea9`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-507f5957b54c021d:start -->
<a id="source-507f5957b54c021d-game-api"></a>
### Game API

Base domains:

- DEV: `https://dev-api.lastrail.app`
- PRD: `https://api.lastrail.app`

The server currently uses `dev` and `prd` environment axes only. PRD resources and deploys remain approval-gated.

Unversioned: `GET /status`.

Versioned routes use `/v1/*`.

<a id="source-507f5957b54c021d-account-identity"></a>
#### Account Identity

Game API player writes use LAST RAIL account auth, not Cloudflare Access.
Cloudflare Access is for Admin API only.

Production and DEV remote Source of Truth:

- The client sends `Authorization: Bearer <access-token>`.
- The access token is issued by LAST RAIL auth, initially through
  `POST /v1/auth/guest` and later refreshable through `POST /v1/auth/refresh`.
- The server verifies token signature, expiry, token family/session status, and
  account status before resolving `accountId`.
- The resolved `accountId` from auth middleware is the only account identity
  available to Game API write routes.
- Request bodies must not include `accountId`.
- Client-supplied account headers are not trusted.

DEV/local policy:

- Remote DEV must use the same account-auth path as PRD for economy-impacting
  writes.
- Until `POST /v1/auth/guest` / access-token verification exists, new
  account-scoped commit routes remain placeholder-only and must not mutate D1.
- Unit and integration tests may inject a test account context through test
  harnesses/mocks only; that path must not be exposed by Wrangler DEV/PRD
  config or public HTTP routes.
- Do not add a remote DEV `X-Account-Id`, `X-Lastrail-Account-Id`, or similar
  shortcut for gacha/economy writes.

Idempotency linkage:

- Idempotency lookup is scoped by the authenticated `accountId`.
- Game API writes use `idempotency_records(account_id, operation,
  idempotency_key)`.
- For gacha pulls, replay lookup is:
  `accountId + operation = 'gacha_pull' + Idempotency-Key`.
- The `accountId` used for idempotency must match all written batch, result,
  item ledger, progression ledger, and mutation rows in the same transaction.

<a id="source-507f5957b54c021d-auth"></a>
#### Auth

`POST /v1/auth/guest` creates a guest account and returns the initial token
pair.

The expanded three-path launch auth design is tracked in
`auth-account-linking.md`. That draft covers guest restore, ID/password
registration/login/link, Google link, and provider-owned-by-other-account
conflict preview/confirm. Local ID/password register/login/link and password
reset route skeletons are implemented behind provider-neutral recovery email
verification. A Gmail API recovery email sender adapter is available behind the
same sender contract; real Gmail OAuth credentials, sender authorization, and
real-send smoke remain approval/setup-gated.
Account-switch endpoints are not implemented yet. ID/password registration and
link completion require an explicitly submitted and server-verified recovery
email; Google claim email is not auto-promoted into password recovery state.

Realtime two-player cooperative combat starts in `realtime-coop.md`. The first
server slice exposes room creation and WebSocket connection skeletons behind a
Durable Object binding, with server validation/judgment as the authority model.

Minimal ops error write integration:

- Missing Google verifier/audience config writes a scrubbed
  `google_auth_config_missing` ops error with `severity = critical`.
- The response remains `google_auth_not_configured`; raw Google ID tokens and
  full claims are not stored.
- Offline reward state conflicts write scrubbed
  `offline_reward_state_conflict` ops errors.
- Gacha pull commit batch conflicts write scrubbed `gacha_state_conflict` ops
  errors.
- Accepted full-clear run finish story progress commit conflicts write scrubbed
  `story_progress_state_conflict` ops errors.
- Normal validation rejects, insufficient resources, inactive banners, and
  duplicate replay outcomes are not ops error writes.
- Ops error writes are fail-soft and must not worsen the player-facing API
  response.

Response:

```json
{
  "tokenType": "Bearer",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:15:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:00:00.000Z"
}
```

Server writes:

- `accounts`
- `auth_identities` with provider `GUEST`
- `auth_sessions` with the refresh token hash only
- `player_profiles`
- `account_runtime_state`
- `account_tutorial_state`
- `account_offline_reward_state` with:
  - `last_offline_reward_claim_at = account.created_at`
  - `last_story_progress_id = 'none'`
  - `last_balance_config_version = NULL`
  - `last_narrative_version = 'act1.2026-08-27.001'`
  - `last_previewed_at = NULL`
  - `revision = 0`

Security notes:

- Access tokens are signed by `AUTH_SIGNING_KEY_CURRENT`.
- Store `AUTH_SIGNING_KEY_CURRENT` as a Worker secret or local `.dev.vars`
  value. Do not commit the secret to Git.
- Refresh token plaintext is returned once and never stored in D1.
- `auth_sessions.refresh_token_hash` stores only a SHA-256 hash.
- Access token TTL is currently 15 minutes.
- Guest refresh token/session TTL is currently 30 days.
- Optional auth `Idempotency-Key` replay also covers same-key concurrent guest
  creation and refresh rotation. The server collapses the losing request through
  DB uniqueness plus replay lookup instead of creating a second guest account or
  marking the refresh family `REUSED`.

<a id="source-507f5957b54c021d-profile--display-name"></a>
#### Profile / Display Name

`accountId` is the immutable server primary key. It is not the player-facing
nickname and must not be supplied by the client in profile request bodies,
query parameters, or shortcut headers.

Current local endpoints:

```text
GET /v1/profile
GET /v1/profile/display-name/check?displayName=<candidate>
POST /v1/profile/display-name
```

All routes require `Authorization: Bearer <access-token>` and resolve the
account from the authenticated session context.

`GET /v1/profile` returns:

```json
{
  "accepted": true,
  "account": {
    "id": "account_..."
  },
  "profile": {
    "displayName": "Rail Captain",
    "commanderXp": 120,
    "displayNameUpdatedAt": "2026-08-27T00:00:00.000Z",
    "nextDisplayNameChangeAvailableAt": "2026-09-03T00:00:00.000Z"
  },
  "errorCode": null,
  "message": null
}
```

Display name policy:

- `player_profiles.display_name` stores the original player-facing display
  value.
- `player_profiles.display_name_normalized` stores the uniqueness key.
- Normalization uses Unicode NFKC, trims leading/trailing whitespace, collapses
  repeated whitespace to a single space, and compares case-insensitively.
- Length is 2 to 16 Unicode code points.
- Allowed characters are letters, numbers, space, `_`, and `-`.
- Reserved system-like names such as `admin`, `system`, `support`, and names
  starting with `account_`, `admin_`, `gm_`, or `system_` are rejected.
- Display names are globally unique by normalized value.
- Re-submitting the current normalized display name for the same account is
  idempotent success.
- Changing to a different display name is blocked for 7 days after the previous
  display name update.

`GET /v1/profile/display-name/check` returns `available: true` when the
normalized candidate is unused or already belongs to the authenticated account.
Invalid candidates return `invalid_display_name`; candidates owned by another
account return `display_name_taken`.

`POST /v1/profile/display-name` accepts:

```json
{
  "displayName": "Rail Captain"
}
```

Success returns `changeStatus: "UPDATED"` or `"UNCHANGED"`. Error codes:

- `auth_required`
- `access_token_invalid`
- `auth_session_invalid`
- `account_not_active`
- `contract_violation`
- `invalid_display_name`
- `display_name_taken`
- `display_name_change_cooldown`
- `profile_not_found`

<a id="source-507f5957b54c021d-google-sign-in--guest-link"></a>
##### Google Sign-In / Guest Link

Launch includes Google Sign-In for Unity. The server verifies Google ID tokens
and then issues the normal LAST RAIL `Bearer` access/refresh token pair. The
token bundle shape intentionally stays compatible with `POST /v1/auth/guest`
and `POST /v1/auth/refresh`.

Implementation note: production verification must follow the current Google
Identity / OpenID Connect guidance at implementation/deploy time:

- Verify the ID token signature against Google public keys from the Google JWKS
  endpoint.
- Verify `aud` against the configured Google OAuth client id allow-list.
- Verify `iss` is `accounts.google.com` or `https://accounts.google.com`.
- Verify the token has not expired.
- Use the Google `sub` claim as the provider subject. Do not key identity by
  email.
- Cache Google public keys according to the HTTP `Cache-Control` max-age
  response header and refresh when the cached key set expires or the key id is
  unavailable.

The actual Google OAuth client id / audience values are PRD/app-registration
inputs and must not be hardcoded into source. DEV real-Google smoke is not a
launch blocker and may remain `google_auth_not_configured` until PRD values are
available. The route uses `GOOGLE_AUTH_AUDIENCES` as the allow-list source.
Local tests may still inject `GOOGLE_ID_TOKEN_VERIFIER` as a fake verifier, but
production can use the default Worker/WebCrypto verifier adapter behind the same
`verifyIdToken(idToken, audiences)` abstraction.

Current safe adapter boundary:

- Accepts only JWTs with `alg = RS256` and a non-empty `kid`.
- Fetches public keys from `https://www.googleapis.com/oauth2/v3/certs`.
- Verifies the RS256 signature with WebCrypto.
- Rejects missing or unconfigured audiences.
- Rejects unknown audience, unsupported issuer, expired token, missing subject,
  missing key id, unavailable JWKS, or invalid signature.
- Player-facing invalid token failures use stable generic
  `google_id_token_invalid` without exposing the internal verifier reason.
- JWKS fetch/parse failures use `google_auth_temporarily_unavailable` and may
  write a scrubbed `google_auth_verifier_unavailable` ops error for operations.
- Returns only the Google `sub` as `subject` plus optional scrub-safe email
  fields for future use; route persistence continues to use only `subject`.

`POST /v1/auth/google` signs in or restores an account through a verified
Google identity.

Request:

```json
{
  "idToken": "google-id-token"
}
```

Response:

```json
{
  "tokenType": "Bearer",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:15:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:00:00.000Z"
}
```

`POST /v1/auth/google/link` links the authenticated guest/current account to a
verified Google identity.

Headers:

```text
Authorization: Bearer <access-token>
```

Request:

```json
{
  "idToken": "google-id-token"
}
```

Response:

```json
{
  "tokenType": "Bearer",
  "linkStatus": "LINKED",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:15:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:00:00.000Z"
}
```

Link status:

- `LINKED`: this account is newly linked to the Google provider subject.
- `ALREADY_LINKED`: the same account already owns that provider subject; the
  response is replay-safe/idempotent success.

Conflict and boundary rules:

- `auth_identities(provider, provider_subject)` remains the unique identity
  boundary. For Google, `provider = 'GOOGLE'` and `provider_subject = sub`.
- Automatic account merge is not available at launch.
- A Google identity already linked to another account returns
  `google_identity_already_linked`.
- Request body/query/header `accountId`, `account_id`, `X-Account-Id`, or
  `X-Lastrail-Account-Id` style shortcuts are not accepted as Source of Truth.
- Raw Google ID tokens are never stored in D1, logs, or responses.
- Raw Google claims/full claim dumps are not stored or returned. If future
  support visibility needs provider context, use scrubbed metadata only.
- Refresh token plaintext storage remains forbidden; only
  `auth_sessions.refresh_token_hash` is persisted.

`POST /v1/auth/refresh` rotates a refresh token and returns a new token pair.

Request:

```json
{
  "refreshToken": "refresh_..."
}
```

Response:

```json
{
  "tokenType": "Bearer",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:30:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:15:00.000Z"
}
```

Refresh rules:

- Lookup uses `sha256(refreshToken)` against
  `auth_sessions.refresh_token_hash`.
- Only `ACTIVE` sessions for `ACTIVE` accounts can refresh.
- Successful refresh marks the old session `ROTATED`.
- Successful refresh inserts a new `ACTIVE` session in the same family with
  `generation + 1`.
- Reusing an already `ROTATED` refresh token marks active/rotated sessions in
  that family as `REUSED` and returns `refresh_token_reused`.
- Supplying `Idempotency-Key` on guest creation or refresh enables response-loss
  replay recovery for that exact request. Same-key guest replay returns the same
  account/session bundle. Same-key refresh replay after a committed rotation
  returns the same rotated session bundle instead of marking the family reused.
- The idempotency key is a replay secret for the in-flight request and must be
  high entropy, locally persisted only until the token bundle is durably stored,
  and never logged by Unity.
- Auth idempotency keys shorter than 16 chars or longer than 128 chars return
  `contract_violation`. A replay whose committed session is inactive or expired
  returns `auth_replay_expired` and must not revive a revoked/expired session.

<a id="source-507f5957b54c021d-client-config"></a>
#### Client Config

`GET /v1/client-config` returns public Unity client configuration:

- `environment`
- `apiBaseUrl`
- `balanceConfigEndpointTemplate`
- `minimumClientVersion`
- `maintenance`
- `accepted`
- `serverVersion`
- `clientAdmission`
- `contentRelease`
- `errorCode`
- `message`

This response must not include secrets, tokens, Cloudflare credentials, Admin API URLs, or Access credentials. API domains are public client configuration, not a secret. If tamper resistance is needed later, add a signed config payload instead of encrypting a value that the Unity client must decrypt.

Launch admission uses numeric app build codes. Unity sends
`X-LastRail-App-Version-Code`; the Game API compares it with the server-owned
`minimumVersionCode` from the current version document. When
`CLIENT_ADMISSION_MODE=ENFORCED`, protected `/v1/*` routes run the same gate
before auth/gameplay handling. A Unity update prompt or Admin version document
alone is not sufficient to protect compatibility-breaking D1/API changes.

Protected route exceptions are `GET /status`, `GET /v1/client-config`, password
recovery verification/reset routes, and frontend diagnostic `client-errors`
routes.

The endpoint also returns an approved compatible Addressables content release
pointer when the current version document contains one and the installed app
version is in its compatibility range. Missing policy or missing compatible
content is explicit fail-closed config state. See
`docs/api/client-admission-content-release.md` and
`docs/cloudflare/release-db-api-compatibility-policy.md`.

<a id="source-507f5957b54c021d-balance-config"></a>
#### Balance Config

Balance payloads remain on Game API: `GET /v1/balance-config/:version`.

The endpoint reads published versions from D1 `balance_config_versions` and returns:

- `version`
- `schemaVersion`
- `requiredContentVersion`
- `checksum`
- `publishedAt`
- `payload`

Only positive integer versions are accepted. Missing/unpublished versions return `BALANCE_CONFIG_NOT_PUBLISHED`. Invalid stored JSON returns `BALANCE_CONFIG_PAYLOAD_INVALID`.

<a id="source-507f5957b54c021d-run-finish--reward"></a>
#### Run Finish / Reward

Run start uses a local launch contract route:

```text
POST /v1/runs/start
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

Request body:

```json
{
  "operationPreviewId": "preview_..."
}
```

The account id is resolved only from the authenticated player session. Query
parameters and client-supplied source-of-truth fields such as `accountId`,
`risk`, `riskLevel`, `storyProgress`, reward amounts, economy grants, or
animation timeline data are rejected with `contract_violation`.

Accepted response shape:

```json
{
  "accepted": true,
  "startStatus": "STARTED",
  "runId": "run_...",
  "operationPreviewId": "preview_...",
  "operationId": "opn_03_water_stop",
  "actId": "act_01_wasteland_railway",
  "regionId": "region_wasteland_railway",
  "encounterType": "ORDINARY_15_WAVE",
  "riskLevel": "III",
  "risk": "III",
  "balanceVersion": "501",
  "contentVersion": "act1.launch.content.v1",
  "ordinary": {
    "totalWaves": 15,
    "startWaveNumber": 1
  },
  "raid": null,
  "raidId": null,
  "bossId": null,
  "bossPhaseRefs": [],
  "allowedOperatorSlots": 5,
  "startAugmentPackageRef": "act1_full_start_package_v1",
  "startTacticPackageRef": "act1_full_start_package_v1",
  "runVersionSnapshot": null,
  "serverStartedAt": "2026-08-27T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

When client admission is enforced and accepted, `runVersionSnapshot` contains
the start-time client installed app version/version code/platform/channel,
admission status, runtime deployment id, content release id/version/hash, and
capture timestamp. It is nullable for legacy/local runs and is diagnostic
evidence only.

Raid boss-only previews use the same route and return `encounterType =
RAID_BOSS`, `ordinary = null`, and raid refs under both the compact top-level
fields and the `raid` object. Ordinary previews must not leak `raidId`,
`bossId`, or `bossPhaseRefs` from malformed preview JSON.

Run start consumes `operation_previews` with an affected-row guard before
creating the run snapshot. `Idempotency-Key` is required for launch. A retry
with the same account, operation `run_start`, key, and `operationPreviewId`
returns the stored `STARTED` response shape and must not create a second
`runs` row or `run_snapshots` row. The retry returns the originally stored
`runVersionSnapshot` and does not read current version policy again. Reusing the
same key for a different
`operationPreviewId` returns `contract_violation`. A consumed preview without a
matching idempotency record returns `409` with
`operation_preview_already_consumed`.

Run finish responses add `runStartVersionSnapshot` and
`runFinishVersionSnapshot`. The latter uses source `server_accepted_run_finish`
and is stored only for the first accepted finish; duplicate finish replay
returns the stored value.

If preview consumption succeeds but run/snapshot/idempotency persistence fails,
the route returns `409 run_start_state_conflict`, attempts to reopen the preview
to `ACTIVE`, and writes a scrubbed fail-soft ops error log. The client should
retry with the same `Idempotency-Key` after fetching/confirming preview state.

Run finish uses `POST /v1/runs/:runId/finish`. See `docs/api/run-result-submit.md`.

Accepted run finish responses return `rewardPreviewId` first. Reward review and
claim/commit follow `docs/api/run-reward-preview-claim.md`.
Launch risk/reward preview generation policy is drafted in
`docs/api/run-risk-reward.md`.

Raid boss-only run lifecycle impact is captured in
`docs/api/raid-boss-run-contract.md`. Launch keeps one run finish route and
distinguishes ordinary 15-wave operations from boss-only raids with
`encounterType`.

Act 1 story raid failure returns a server-authored `staminaRefund` marker with
`status = COMMITTED`, `refundType = STAMINA`, `reason = STORY_RAID_FAILURE`,
and `policyRef = act1_story_raid_failure_stamina_refund_v1`. The accepted
finish transaction writes `STORY_RAID_STAMINA_REFUND` mutation evidence, one
`fuel_ledger` row with reason `STORY_RAID_FAILURE_REFUND`, and a capped
`fuel_state` update. The response does not expose refund amount, raw run start
cost, or fuel balances. This is Act 1 story raid only and is not World Raid
settlement.

Act 1 launch starts ordinary and raid encounters with server-authored full
augment/tactic package refs. See `docs/api/run-start-augment-package.md`.

<a id="source-507f5957b54c021d-story-progress"></a>
#### Story Progress

Story progress Source of Truth and read model policy is drafted in
`docs/api/story-progress.md`.

Current local Game API read endpoint:

```text
GET /v1/story-progress
Authorization: Bearer <access-token>
```

Baseline policy:

- Server owns durable story progress and unlock truth.
- Unity owns narrative presentation and localized text playback.
- Accepted run finish is the durable story progress update moment for the
  current local contract slice.
- Client-supplied story progress, unlock state, and narrative flags are not
  Source of Truth.
- Account id is resolved only from the authenticated player session.
- Query/body account id is rejected with `contract_violation`.
- Empty accounts return accepted Act 1 `none` progress and default
  `opn_01_last_departure` unlock.

<a id="source-507f5957b54c021d-offline-rewards"></a>
#### Offline Rewards

Offline reward progression is defined in `docs/api/offline-rewards.md`.
The current local contract slices add server-side balance schema parsing, a
pure preview calculator, a read-only preview endpoint, and a claim endpoint for
launch currency reward lines:

```text
GET /v1/offline-rewards/preview
Authorization: Bearer <access-token>
```

```text
POST /v1/offline-rewards/claim
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

The preview endpoint resolves account id from the authenticated player session
only. It rejects query/body Source of Truth fields with `contract_violation`,
returns server-calculated display `rewardLines`, and does not write ledger rows,
grant rewards, or consume offline claim state.

Migration `0015_account_offline_reward_state.sql` adds the durable offline
reward baseline table. Preview reads
`account_offline_reward_state.last_offline_reward_claim_at` first and falls
back to `accounts.created_at` for older accounts without a row.

The claim endpoint rejects query/body Source of Truth fields, requires
`Idempotency-Key`, calculates rewards from server-stored story/offline state,
writes account/economy/currency ledger rows atomically, and stores successful
`idempotency_records.operation = 'offline_reward_claim'`. Duplicate replay
returns the stored claim id without advancing state or granting again.

Launch offline claim only grants currency reward lines supported by
`account_currencies`/`currency_ledger`. Item grants and full command-ticket,
command-ticket-fragment, or special-core offline loops remain closed.

Baseline policy:

- Offline rewards scale with story progression, not run risk.
- Risk rewards remain active-run-only.
- Unity must not calculate offline reward amounts, multipliers, storage caps, or
  economy grants.

<a id="source-507f5957b54c021d-gacha"></a>
#### Gacha

Gacha pull history, banner pity state, duplicate conversion, and ticket/material
ledger linkage are drafted in `docs/api/gacha-pity-result-history.md`.
Future gacha PRD rarity selection is drafted in
`docs/api/gacha-prd-rarity-selection.md`; it keeps the current response shape
and hard `guaranteeType` semantics unchanged.

Pull execution uses:

```text
POST /v1/gacha/banners/:bannerId/pulls
Idempotency-Key: <client-generated-key>
```

Contract baseline:

- Request body is `{ "pullCount": 1 }` or `{ "pullCount": 10 }`.
- `bannerId` is path-only; sending `bannerId` in the body is
  `contract_violation`.
- The server returns `resultStatus` as `ACCEPTED`, `DUPLICATE_REPLAY`, or
  `REJECTED`.
- Success and duplicate replay responses include stored pull results and
  `postPityState`.
- `postPityState` is the state Unity should display after the pull.
- Cost is server-calculated as `cur_command_ticket` with quantity equal to
  `pullCount`.
- Pull probability, pity, duplicate conversion, ticket spend, grants, and ledger
  writes are server Source of Truth.
- Launch data has a `TESTING_TUNING_DRAFT` fixture in
  `launchGachaDataDraft`. It is used for local response stability smoke only and
  is not a production seed/config promotion.
- Launch command ticket acquisition is promoted into local launch balance
  contract as a non-repeat 12-ticket path: login bonus 5, `opn_03` first clear
  1, `opn_05` first clear 1, `opn_06` first clear 2, and story raid first clear
  3. The login bonus tickets are claimed through `POST /v1/login-bonus/claim`.
  The first-clear tickets are emitted as server-owned `ITEM` `rewardLines` and
  committed through `account_items`/`item_ledger` during reward claim. Remote
  production seed/config deployment still requires explicit approval.

<a id="source-507f5957b54c021d-launch-login-bonus"></a>
#### Launch Login Bonus

```text
POST /v1/login-bonus/claim
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

Contract baseline:

- Request body and query parameters are not accepted.
- Account identity comes only from verified Game API bearer auth.
- The launch bonus id is `launch_login_bonus_act1_v1`.
- The server grants one non-repeat `ITEM` reward line:
  `cur_command_ticket x5`, `source = 'LAUNCH_LOGIN_BONUS'`.
- Success writes `account_mutations`, `economy_transactions`,
  `reward_transactions`, `account_items`, `item_ledger`,
  `account_login_bonus_claims`, and `idempotency_records.operation =
  'login_bonus_claim'` in one committed batch.
- Same account + same `Idempotency-Key` returns `DUPLICATE_REPLAY` and does not
  write ledger or grant again.
- Same account + different key after the bonus was claimed returns
  `ALREADY_CLAIMED` and does not write ledger or grant again.
- The response does not include raw ledger dumps, tokens, auth headers, RNG
  material, probability data, full weight tables, or account shortcut data.


<!-- source-body:source-507f5957b54c021d:end -->


<a id="source-7dd0ce78b70ebd9f"></a>
## Minimum supported app version

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/minimum-version-policy.md`  
원문 SHA-256: `15ea494b0871f3d665724abc8287d735db912c6c7c63db9752f85db8550100be`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-7dd0ce78b70ebd9f:start -->
<a id="source-7dd0ce78b70ebd9f-minimum-supported-app-version"></a>
### Minimum supported app version

The DEV Admin version document at `versions/current.json` stores `version`,
`versionCode`, and `minimumVersionCode`. Codes are decimal integer strings,
compared numerically, in the range 1 through 2147483647.

An app code below `minimumVersionCode` requires an update; equality is supported.
The minimum must not exceed the current deployment's `versionCode`.
Raise it only when compatibility requires dropping older clients, not on every release.
Additive DB/API changes that old clients can ignore do not require a forced app
update by themselves.

Legacy documents without the minimum remain readable and are displayed as unset.
Every new export must explicitly include a valid minimum. A stale client omitting
the field is rejected so it cannot silently remove an existing policy.
Only a DEV OWNER can export this document.

Rollback must use a compatible minimum no higher than the rollback version code.
Lowering the minimum is an explicit operator decision; do not lower it automatically.
This policy is independent of retention: it does not delete old artifacts or data.
Version history and rollback execution are separate work; current export overwrites
the current document and does not provide an archive or database rollback.

Game API `GET /v1/client-config` now consumes the current version document for
launch admission and compares the installed `X-LastRail-App-Version-Code`
against server-owned `minimumVersionCode`. When `CLIENT_ADMISSION_MODE` is
`ENFORCED`, protected Game API `/v1/*` routes run the same check before auth or
gameplay handling. Saving the document is still not the same as a Worker deploy,
content activation approval, or database rollback.
DB-sensitive releases must follow
`docs/cloudflare/release-db-api-compatibility-policy.md`.

<!-- source-body:source-7dd0ce78b70ebd9f:end -->


<a id="source-38075a0c0078f636"></a>
## Current monthly attendance discovery

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/monthly-attendance-discovery.md`  
원문 SHA-256: `0d4c5ffa52fd5ee9e1e180c45a7e2d70b18447589d5d65d8ce7cc87686bebdf4`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-38075a0c0078f636:start -->
<a id="source-38075a0c0078f636-current-monthly-attendance-discovery"></a>
### Current monthly attendance discovery

`GET /v1/attendance/monthly/current` requires existing bearer auth/admission,
rejects all query/body fields and sends no-store. It performs one DB snapshot
SELECT over immutable activity_definitions and the authenticated account's current
monthly_attendance_cycles binding, using DB time. No grant, row initialization,
policy creation, fallback legacy attendance or seed. No new migration.

```ts
type Discovery = {
 accepted: boolean;
 version: "monthly-attendance-discovery-v1";
 serverTime: number;
 cycleId: string; startAt: number; endAt: number; dayIndex: number;
 activityId: string | null; policyVersion: string | null;
 pinned: boolean; canAttend: boolean;
 status: "AVAILABLE" | "NOT_CONFIGURED" | "OUTSIDE_WINDOW" |
   "REWARD_BINDING_UNAVAILABLE" | "AMBIGUOUS_CONFIGURATION" |
   "PINNED_POLICY_UNAVAILABLE" | "INVALID_CONFIGURATION";
 errorCode: string | null;
 definitionStartAt?: number; definitionEndAt?: number;
 candidateCount?: number;
};
```

| HTTP/status | Meaning |
| --- | --- |
| 200 NOT_CONFIGURED | No monthly-kind definitions; legacy DISTINCT_DAYS never substitutes |
| 200 OUTSIDE_WINDOW | Monthly definitions exist but none covers DB now |
| 200 REWARD_BINDING_UNAVAILABLE | Exactly one current definition, missing elite binding; ID returned for blocked read UI, canAttend=false |
| 200 AVAILABLE | Exactly one current definition with configured binding; no claim/XP or completion implied |
| 409 AMBIGUOUS_CONFIGURATION | More than one active definition; no first-match selection. Only an existing account pin may be returned as identity, canAttend=false |
| 409 PINNED_POLICY_UNAVAILABLE | Current month already pinned but that policy is not active/available; identity retained, no switch |
| 503 INVALID_CONFIGURATION | Monthly definition fails strict policy parsing |

Auth400/401/403/etc retain standard auth envelopes; query/body400 contract_violation.
Database/transport failures are failures, never successful empty lists. Invalid
definitions are errors even if another valid definition exists. Candidate IDs are
not selected by lexical/creation order. On ambiguity, candidateCount is diagnostic.
No another-account cycle identity is returned.

The per-ID monthly read and mutation reject ambiguous active definitions too.
Mutation commit guards recheck the active definition count, so a second definition
inserted after discovery cannot race into a payout. A policy switch cannot create
another same-month ledger because the stable account+cycle binding is preserved.

Unity uses returned activityId/policyVersion/cycleId to read `/monthly`, then the
original strict auto-attend/recover-next protocol. All amounts and spending split
come from that read. Discovery is not a reusable grant authorization: refresh when
account or month changes, discard responses for the prior account/request context,
and retain original mutation requests only for receipt recovery. Old cycle requests
still409 without spending; old successful receipts still replay. Monthly KST04
rollover is independent from local midnight. Actual elite binding remains pending;
test AVAILABLE fixtures are not published/approved policies.

Six integration tests cover missing/legacy exclusion/outside/null binding, read-only
bytes, ambiguity with/without account pin, pinned-policy unavailability, DB-month
rollover and stale command, auth/query, and concurrent-definition commit rollback.

<!-- source-body:source-38075a0c0078f636:end -->


<a id="source-2c7eeb3d06656ab1"></a>
## S08 monthly sequential attendance

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/monthly-attendance.md`  
원문 SHA-256: `47be955bf4dafef7d382a04d4ac256dc56f2ab357b0e72f891f6b542b0486749`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-2c7eeb3d06656ab1:start -->
<a id="source-2c7eeb3d06656ab1-s08-monthly-sequential-attendance"></a>
### S08 monthly sequential attendance

Local implementation only, migration0032. Existing DISTINCT_DAYS daily/milestone
service remains a legacy contract. New monthly routes reuse its immutable activity
definition registry, activity/reward receipts, economy transactions, real currency
and item ledgers, and FREE/PAID crystal wallet planning. No definition is seeded.

<a id="source-2c7eeb3d06656ab1-policy-and-unresolved-binding"></a>
#### Policy and unresolved binding

Definition JSON: `{kind:"MONTHLY_ATTENDANCE",version:"monthly-attendance-v1",
startAt:number,endAt:number,eliteTicketItemId:string|null}`. Times must be KST04
calendar-month starts. Definitions are immutable; ID is the policyVersion.
The server defines the approved28 cells, not client input: each week's50 FREE,
credits, parts, EXP, data, credits, milestone; credit per cell1000/1500/2000/2500,
parts50/75/100/125, EXP100/150/200/250, data25/40/55/70. Milestones100/150/200 FREE,
then one elite ticket. Totals650 FREE,14000credits,350parts,700EXP,190data,one ticket.
Credits/parts/EXP/data use account_currencies and currency_ledger, not account_items.

**Actual elite-ticket identifier and consumption policy are not approved/bound.**
Null exposes elite_ticket_binding_missing and every mutation fails503
attendance_reward_binding_unavailable. No partial27-cell launch, ordinary ticket
substitution or complete claim. cur_command_ticket as binding is rejected. Test
fixtures explicitly use TEST_ONLY_ELITE_TICKET to test generic atomic item grants;
this is not a production entitlement/usable elite ticket. Do not publish a policy
until the actual entitlement producer/consumer and pinned version contract exist.
See [elite-ticket evidence and proposal](49_SERVER_API_CONTRACTS.md#source-0b8706c7816cdc23).

<a id="source-2c7eeb3d06656ab1-api"></a>
#### API

Bearer authentication and existing admission, no query, strict bounded JSON,
no-store for success/errors. Prefix `/v1/attendance/:activityId`:

| Route | Request | Behavior |
| --- | --- | --- |
| GET /monthly | No body | Read-only cycle/cells/recovery quote; never grants/initializes |
| POST /auto-attend | policyVersion, cycleId, expectedRevision | Automatic client boot/lobby mutation before confirmation popup, no manual claim gating |
| POST /recover-next | Same plus walletPolicyVersion, confirmedPaid | One next sequential cell,100crystals, requires today's auto-attend committed first |

POST Idempotency-Key:1–128 printable non-space ASCII. cycleId `YYYY-MM` and policy
ID come from GET; expectedRevision is nonnegative safe integer below MAX_SAFE_INTEGER.
confirmedPaid is exact quoted PAID spend0..100; a PAID remainder requires explicit
user confirmation in Unity. Zero prevents silently spending any PAID. If split
changes, return409 attendance_paid_confirmation_changed and show a refreshed quote.
No caller dayIndex/cell/XP/time/reward inputs. Auto-attend is a separate successful
mutation before obtaining the recovery quote; recovery never implicitly confirms
spending a balance that changed with today's attendance.

GET includes version, activityId/policyVersion, cycleId/startAt/endAt/dayIndex,
serverTime, active, revision, completedCells, complete (only28), todayAttended,
unavailableReason,28 cells with reward kind/ID/quantity/unavailableReason and
optional `{cell,source,dayIndex,claimId}` receipt. Recovery includes price100,
maxPerCycle7, used, recoverableCount, todayAttendanceRequired and quote (null if
wallet unavailable) with walletPolicyVersion/currencyId/freeSpent/paidSpent/
sufficientFunds. A count is eligibility, not a promise of wallet/binding readiness.

Success includes version/policyVersion/cycleId/kind, new revision, claimId,
rewardTransactionId/accountMutationId/economyTransactionId, cell (null after28),
sourceDayIndex, completedCells, rewardLines, wallet, serverClaimedAt, refreshRequired.
Wallet is null when no crystal action is needed. Natural days29–31 may record a
visit with no cell/reward. They never grant another elite ticket.

400 contract_violation;401 auth;503 activity_not_configured,
monthly_attendance_policy_invalid, attendance_reward_binding_unavailable or wallet
policy failures. Main409s: attendance_cycle_changed, attendance_cycle_policy_changed,
activity_outside_window, attendance_revision_conflict, attendance_today_required,
attendance_not_recoverable, attendance_paid_confirmation_changed,
crystal_insufficient_balance, crystal_policy_changed, attendance_state_changed,
idempotency_conflict. Preserve draft/body/key on unknown outcomes; never infer
failure from a lost response. Retry original exact request to retrieve its receipt.

<a id="source-2c7eeb3d06656ab1-calendar-and-atomicity"></a>
#### Calendar and atomicity

Cycle changes on month1 KST04, not midnight or every28days. Natural visit grants
next cell, never the calendar-day number. Recovery uses earliest unused fully
elapsed service day after registration: ceil((registeredAt-cycleStart)/day), so
a partially elapsed registration day is not sold as a fully missed day. No dates
before registration/current/future days. Count=min(unused missed days,7-used,28-filled).
One stable account+month ledger across activity IDs prevents policy-version double
claim; after first mutation another policy ID cannot take over that cycle.

Account mutation sequence precedes eligibility/balance reads. One batch guards
DB service-day/time window, registration, cycle policy/revision, debits existing
wallet lots before any same-command reward, grants, writes immutable receipts and
updates visits/cells. Failure rolls back all writes. Duplicate natural visits
return the original response; each new successful request key stores an alias to
that receipt so day/month rollover does not break response-loss recovery. Alias
storage grants nothing and does not change attendance revision.

Successful original-body/key replay wins before current cycle/eligibility checks.
Same-month day boundary retries re-evaluate the new day; month boundary rejects
the stale cycle without spending. Competing natural/recovery actions share revision
and cell/day uniqueness. No login quest/pass XP, shop quota, offline time or login
bonus is awarded by recovery. Natural visits provide a durable distinct-day source
for the future shared quest producer; this slice does not implement that producer.

Tests cover read-only and missing binding, exact cell economy and real wallets,
month lengths, no skip/extra reward, registration,7 cap, FREE-first/PAID confirmation,
no reward pre-spend, stale cycle/exact and alias replay, DB boundary retries,
concurrent commands, rollback, auth/account isolation and strict inputs.

Local verification2026-09-10:80 test files/527 tests passed (20.28s), including
11 monthly tests. TypeScript/source ESLint passed. Independent review caught
currency-wallet routing and new-key duplicate replay gaps; both were fixed and
covered by real-wallet and cross-month alias tests before this full run.

<!-- source-body:source-2c7eeb3d06656ab1:end -->


<a id="source-c312185bc2e8b97b"></a>
## S10 offline economy binding

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/offline-economy-binding.md`  
원문 SHA-256: `5e83faae81b1b55b45e4ae835231165ab572cd8735ca93388b707db9da473a60`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-c312185bc2e8b97b:start -->
<a id="source-c312185bc2e8b97b-s10-offline-economy-binding"></a>
### S10 offline economy binding

The existing GET /v1/offline-rewards/preview and POST /v1/offline-rewards/claim now
read the immutable economy associated with the active balance version. For approved
act1.economy.2026-09-09.001 this is hourly credits24/parts2/data1/EXP20, existing
eight story progress multipliers, caps4/5/6/7/8/9/10/12h and minimum600seconds.
The six story operation IDs remain unchanged; four operation categories are a
separate unresolved catalog and do not remap these progression keys.

An absent active binding retains the legacy100/50/30/20 policy. This code does not
activate or seed the new version. A present invalid hash/config fails closed.
The adapter uses the existing minute-based calculator; future policies whose
minimumSeconds is not a positive whole minute are not supported by that calculator.
The approved600-second policy is supported and tested.

Preview and claim add nullable balanceVersion:number, economyVersion:string and
economyHash:string at the response root. Null economy fields identify the legacy
unbound path. Claim receipt JSON retains the same metadata and limits. Existing
DUPLICATE_REPLAY behavior (accepted:false, no new rewardLines) is preserved and
returns the stored policy metadata without consulting the current active policy.
This is a completed old claim, not a failed new grant.

The claim captures account mutation sequence before reading elapsed baseline and
balances, fences active balance/binding in the batch, and retries uniqueness races.
Policy races return409 offline_reward_policy_changed with no partial credit.
Existing minimum-duration and currency rounding behavior are unchanged. Claim
advances the baseline and writes currency/reward/idempotency ledgers atomically.

Migration0028 adds account_offline_reward_state.last_reward_transaction_id. New
claims link to their exact pinned receipt; historical rows stay NULL and are not
reinterpreted from a later binding. last_balance_config_version is also recorded.
The receipt is the authoritative historical economy version/hash, not a mutable
lookup of whatever binding currently exists.

Five new SQLite/actual-auth HTTP tests verify approved hourly values, receipt and
state linkage, replay after policy removal, legacy unbound behavior, policy race
rollback, concurrent different keys and the final story12-hour/3x cap. Existing
offline HTTP and unit contract tests also run. No remote schema, policy or deploy
was changed.

<!-- source-body:source-c312185bc2e8b97b:end -->


<a id="source-5decd70e036ec90e"></a>
## Offline Rewards Design Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/offline-rewards.md`  
원문 SHA-256: `a1c59f0d8d89168f2f7ded21e602082952ef0bc290d669d18a395c0b912dfe06`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-5decd70e036ec90e:start -->
<a id="source-5decd70e036ec90e-offline-rewards-design-draft"></a>
### Offline Rewards Design Draft

This document defines the launch-scope story progression policy for offline
rewards, the preview route contract, and the first claim transaction slice.

It implements:

- `GET /v1/offline-rewards/preview`.
- `POST /v1/offline-rewards/claim`.
- Migration `0015_account_offline_reward_state.sql`.

It does not implement item grants, offline reward claim state consumption
outside the claim route, Unity calculation, Cloudflare DEV deploy, Cloudflare
secrets, remote smoke, or PRD resources.

<a id="source-5decd70e036ec90e-goals"></a>
#### Goals

- Make offline rewards grow as the player restores Act 1 story infrastructure.
- Keep offline reward amounts as server Source of Truth.
- Keep risk rewards separate from offline rewards.
- Give operations and Admin enough metadata to debug offline reward plans later.
- Avoid repeated special-core, command-ticket, and ticket-fragment economy loops
  in launch scope.

<a id="source-5decd70e036ec90e-non-goals"></a>
#### Non-Goals

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

<a id="source-5decd70e036ec90e-design-boundary"></a>
#### Design Boundary

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

<a id="source-5decd70e036ec90e-recommended-formula"></a>
#### Recommended Formula

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

<a id="source-5decd70e036ec90e-act-1-story-progress-draft"></a>
#### Act 1 Story Progress Draft

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

<a id="source-5decd70e036ec90e-resource-mix-draft"></a>
#### Resource Mix Draft

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

<a id="source-5decd70e036ec90e-launch-economy-draft-fixture"></a>
##### Launch Economy Draft Fixture

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

<a id="source-5decd70e036ec90e-balance-config-schema-candidate"></a>
#### Balance Config Schema Candidate

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

<a id="source-5decd70e036ec90e-server-state-candidate"></a>
#### Server State Candidate

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

<a id="source-5decd70e036ec90e-api-contract"></a>
#### API Contract

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

<a id="source-5decd70e036ec90e-claim-idempotency-policy"></a>
#### Claim Idempotency Policy

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

<a id="source-5decd70e036ec90e-claim-transaction-boundary"></a>
#### Claim Transaction Boundary

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

<a id="source-5decd70e036ec90e-error-codes"></a>
#### Error Codes

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

<a id="source-5decd70e036ec90e-story-progress-source-of-truth"></a>
#### Story Progress Source of Truth

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

<a id="source-5decd70e036ec90e-contract--smoke-test-candidates"></a>
#### Contract / Smoke Test Candidates

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
  first clear 2, and story raid first clear 3. The login bonus portion uses
  `POST /v1/login-bonus/claim`; offline repeat preview/claim must not provide
  those tickets.

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

<a id="source-5decd70e036ec90e-unity-impact"></a>
#### Unity Impact

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

<a id="source-5decd70e036ec90e-admin--operations-impact"></a>
#### Admin / Operations Impact

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

<a id="source-5decd70e036ec90e-open-decisions"></a>
#### Open Decisions

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

<!-- source-body:source-5decd70e036ec90e:end -->


<a id="source-59ff0027b0008512"></a>
## Ops Error Logs Contract

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/ops-error-logs.md`  
원문 SHA-256: `5d9133caa9b6cd8561962877abea2c5b9c746d63969b404d3e60f0b35bd9fe91`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-59ff0027b0008512:start -->
<a id="source-59ff0027b0008512-ops-error-logs-contract"></a>
### Ops Error Logs Contract

This document defines the first server-side contract for runtime/system error
observability in LAST RAIL. It is separate from `admin_audit_logs`, which
records operator actions and permissioned admin mutations.

Current local implementation creates the D1 searchable summary table and
read-only Admin API list/detail routes. Game API also writes a minimal subset of
launch-critical server failures directly to D1 as a local/initial integration.
It does not create Queues, R2 buckets, Workers Logs exports, Cloudflare
resources, remote DEV deploys, PRD/STAGING resources, repair mutations, or
resolution workflows.

<a id="source-59ff0027b0008512-goals"></a>
#### Goals

- Give operators a read-only `/ops/error-logs` view for backend/runtime
  failures.
- Support support triage for reward claim, gacha pull, ledger, integrity,
  Admin API, Game API, Async Worker, Pages Function, and Unity diagnostic
  failures.
- Keep searchable summaries safe for D1 and Admin UI display.
- Keep sensitive raw data out of Git, D1 summaries, logs, and UI responses.
- Preserve correlation IDs so operators can jump from an error to audit logs,
  ledger rows, reward claims, runs, or gacha batches.

<a id="source-59ff0027b0008512-non-goals"></a>
#### Non-goals

- No production deployment, DNS, secret, or custom domain changes.
- No DEV remote deploy or smoke in this slice.
- No live Cloudflare Workers Logs, R2, or Queues integration yet.
- No raw request-body storage.
- No token, secret, Authorization header, raw RNG material, or raw client
  account credential storage.

<a id="source-59ff0027b0008512-source-of-truth-options"></a>
#### Source of Truth Options

<a id="source-59ff0027b0008512-d1-table-direct-writes"></a>
##### D1 Table Direct Writes

Workers write directly into an `ops_error_logs` D1 table during request/error
handling.

Pros:

- Simplest query model for Admin `/ops/error-logs`.
- Easy local tests and migration verification.
- Good for low-volume DEV and admin read-only skeleton wiring.

Cons:

- Adds latency and failure coupling to request paths.
- Error logging can fail during the same incident that caused the original
  error.
- Large stack/metadata payloads do not belong in D1.
- D1 write pressure may become noisy during incident bursts.

Use:

- Acceptable as a local-only or very early DEV prototype.
- Not recommended as the primary long-term write path for runtime errors.

Current direct-write scope:

- `google_auth_config_missing`: `critical`, `game_api`, open. Written when the
  Google identity verifier or audience config is missing for
  `POST /v1/auth/google`.
- `google_auth_verifier_unavailable`: `error`, `game_api`, open. Written when
  the Google ID token verifier cannot fetch or parse Google's public keys
  during `POST /v1/auth/google`. The player response remains the stable generic
  `google_auth_temporarily_unavailable`.
- `offline_reward_state_conflict`: `error`, `game_api`, open. Written when
  server-owned offline reward state is ahead of server time during
  `POST /v1/offline-rewards/claim`.
- `gacha_state_conflict`: `error`, `game_api`, open. Written when the gacha
  pull commit batch fails after validation and before a successful
  `gacha_pull` idempotency record exists. It is reserved for server-side
  state/ledger/idempotency conflict investigation, not normal validation
  rejects.
- `story_progress_state_conflict`: `error`, `game_api`, open. Written when an
  accepted full-clear run finish cannot safely commit the story progress
  update batch. It is reserved for server-owned story/unlock/progression state
  conflicts, not player-supplied story progress values.
- `story_raid_stamina_refund_state_conflict`: `error`, `game_api`, open.
  Written when Act 1 story raid failure refund eligibility is server-verified
  but the account fuel state needed for the committed stamina refund is missing
  or invalid. It is not written for ordinary failures, successful boss defeats,
  or World Raid settlement.
- `run_start_state_conflict`: `error`, `game_api`, open. Written when
  `POST /v1/runs/start` consumes an operation preview but cannot safely persist
  the run, snapshot, and `run_start` idempotency record. The route attempts a
  best-effort preview reopen before returning the stable rejected response.
- `frontend_unity_error`, `frontend_unity_exception`,
  `frontend_unity_assert`: `warning`, `game_api`, open. Written only by
  `POST /v1/client-errors` after player Bearer auth, `Idempotency-Key`, fixed
  event kind, timestamp, occurrence count, authenticated-current-session scope,
  and bounded app version metadata validation. These rows are client diagnostic
  evidence, not server-side error Source of Truth.

These writes are fail-soft. If the ops log insert fails, the original player API
response is preserved. Request bodies, raw Google tokens, Authorization headers,
raw idempotency keys, RNG material, probability data, full weight tables, raw
multiplier bps, and unrestricted stack traces are still not stored.

Frontend safe ingest:

- `POST /v1/client-errors`
- Player Bearer auth required.
- `Idempotency-Key` required; same key/same event batch returns
  `DUPLICATE_REPLAY`, same key/different event batch returns `409
  idempotency_conflict`.
- Top-level body accepts only `events`.
- Each event accepts only `kind`, `occurredAt`, `occurrenceCount`, `appVersion`,
  `appVersionCode`, and `scope=AUTHENTICATED_CURRENT_SESSION`.
- Max 25 events per request and max occurrence count 60 per event.
- Admin `GET /v1/admin/ops/error-logs?category=frontend` returns those
  sanitized rows with `collectionAvailable=true` in the local code path after
  `0b823d0`. DEV/PRD deployments only expose this collection after the matching
  deploy and migration path is approved and applied.
- Admin list/detail summaries expose nullable `appVersion` and
  `appVersionCode` from sanitized context for frontend rows. The list endpoint
  supports exact `appVersion` and `appVersionCode` filters before pagination.
- Pre-login or previous-account queued events are not attributed at upload time
  in this launch contract; Unity should only upload events explicitly scoped to
  the authenticated current session or drop/hold them locally.

`AUTHENTICATED_CURRENT_SESSION` means the event was captured after the current
account's Bearer session became active and before Unity replaced or cleared that
local account owner. Refresh rotation for the same stored account can keep the
same attribution, but a fresh login/restore after app restart should only upload
events Unity can still prove belong to the same local owner. Pre-login events,
previous-account events, and old process crash buffers are unsupported for
server ingestion in launch. The server ignores body account shortcuts and
derives account ownership only from the authenticated Bearer token.

Launch limits and retention:

- Route-level payload limits are fixed at 25 events/request and 60 occurrences
  per event.
- Technical launch default: max 12 new client error ingest batches per
  authenticated account per 10-minute rolling window. Duplicate replay is
  resolved before rate checks so a network retry of an already accepted ingest
  does not write duplicate rows and is not rejected by the rate limit.
- The route returns `429`, `Retry-After: 600`, `retryAfterSeconds: 600`, and
  `errorCode: client_error_ingest_rate_limited` when the account window is
  exhausted.
- The limit uses existing `idempotency_records` for operation
  `client_error_ingest`; no new table, migration, paid service, or remote config
  is required for the local slice. This is not a full public PRD anti-abuse
  policy; wider IP/device/WAF limits remain a release operations decision.
- Idempotency retention follows `idempotency_records`; no local TTL/purge job is
  implemented in this slice.
- Admin `category=frontend` is applied in the SQL `WHERE` clause before
  ordering and pagination; exact frontend app version/code filters use the same
  pre-pagination path. This is covered by `ops-log-search` unit tests and the
  client error ingest smoke.

Response-loss receipt lookup:

```text
GET /v1/client-errors/receipt
Idempotency-Key: <client-generated-key>
Authorization: Bearer <accessToken>
```

The receipt route accepts no body and no query parameters. It returns the
committed ingest id and accepted event count for the authenticated account, or
`client_error_ingest_receipt_not_found` when the key has no matching committed
ingest. It does not resend events or expose raw diagnostic payloads.

Backlog / decision queue:

- Queue-based async sink and optional R2 sanitized detail payloads.
- Wider automatic write integration for non-critical validation rejects,
  support notes, Admin API read model composition failures, and richer client
  diagnostics.

<a id="source-59ff0027b0008512-queues--d1-sink"></a>
##### Queues + D1 Sink

Workers emit sanitized error envelopes to a Cloudflare Queue. A consumer Worker
validates/masks them and writes searchable summaries to D1.

Pros:

- Keeps request path fail-soft and non-blocking.
- Buffers bursts and isolates logging failures from player/admin workflows.
- Lets the sink apply one consistent masking and schema policy.
- Good fit for Admin read-only filtering against D1 summaries.

Cons:

- Requires Queue binding and consumer deployment.
- Delivery is asynchronous, so UI is not instant.
- Needs dead-letter/poison-message policy later.

Use:

- Recommended first implementation direction for DEV once remote resources are
  approved.
- Pair with D1 summary rows. Add R2 payload refs only if safe raw-ish metadata
  is truly needed.

<a id="source-59ff0027b0008512-workers-logs-query--export"></a>
##### Workers Logs Query / Export

Use Cloudflare Workers Logs as the operational log source, either queried
directly or exported into another store.

Pros:

- Captures platform/runtime context without adding D1 writes to request paths.
- Useful for platform-level failures before application code can emit events.
- Good for deep operational investigation outside the game DB.

Cons:

- Admin API read-only filtering may need a separate ingestion/export pipeline.
- Retention, query latency, and access policy are Cloudflare-platform concerns.
- Must still enforce masking before exposing anything in Admin UI.

Use:

- Good secondary source for platform operations.
- Not the first Admin `/ops/error-logs` Source of Truth unless the team commits
  to a Workers Logs export/search pipeline.

<a id="source-59ff0027b0008512-r2-raw-payload-export--d1-search-summary"></a>
##### R2 Raw Payload Export + D1 Search Summary

Store searchable safe summaries in D1 and larger sanitized payloads in R2. D1
rows carry `payloadRef` / `traceRef` pointers.

Pros:

- D1 remains small and queryable.
- R2 can hold larger stack/trace/metadata documents.
- Lets OWNER-only detail views fetch extra metadata later.

Cons:

- Requires bucket lifecycle/retention policy.
- Higher masking risk if raw-ish payloads are not aggressively sanitized.
- More moving pieces for the first implementation.

Use:

- Recommended as phase 2 when operators need stack/trace context beyond the D1
  summary.
- Do not store raw request bodies, tokens, secrets, or raw RNG material in R2.

<a id="source-59ff0027b0008512-recommendation"></a>
#### Recommendation

For initial DEV/operational-console work, use:

1. `Queues + D1 sink` as the preferred write architecture once remote resources
   are explicitly approved.
2. D1 searchable summary as the Admin API read model.
3. Optional R2 sanitized detail payloads in a later phase, referenced by
   `payloadRef` / `traceRef`.
4. Workers Logs as a platform troubleshooting companion, not the initial Admin
   Source of Truth.

Reasoning:

- Runtime logging must be fail-soft. A player reward claim or gacha pull should
  not fail because an error-log write failed.
- Admin `/ops/error-logs` needs predictable filters and pagination, which D1
  summaries provide well.
- R2 raw/detail payloads are useful, but only after masking and retention policy
  are nailed down.

Direct D1 writes can remain useful for local tests and controlled development
fixtures, but the production-shaped path should be asynchronous.

<a id="source-59ff0027b0008512-stable-event-fields"></a>
#### Stable Event Fields

D1 searchable summary row:

```text
id
occurred_at
environment
service
severity
error_code
message_summary
request_id
account_id
run_id
reward_preview_id
reward_claim_id
gacha_batch_id
gacha_banner_id
retryable
masked
payload_ref
trace_ref
created_at
```

API field names:

```json
{
  "id": "ops_error_...",
  "occurredAt": "2026-08-27T00:00:00.000Z",
  "environment": "DEV",
  "service": "Game API",
  "severity": "error",
  "errorCode": "reward_claim_commit_failed",
  "messageSummary": "Reward claim commit failed after validation.",
  "requestId": "req_...",
  "accountId": "account_...",
  "runId": "run_...",
  "rewardPreviewId": "reward_preview_...",
  "rewardClaimId": null,
  "gachaBatchId": null,
  "gachaBannerId": null,
  "retryable": true,
  "masked": true,
  "payloadRef": null,
  "traceRef": null
}
```

Stable enum values:

- `environment`: `DEV`, `PRD`
- `service`: `admin_api`, `game_api`, `worker_cron`, `migration_verify`,
  `support_read_model`, `integrity_pipeline`
- `source`: `admin_api`, `game_api`, `worker_cron`, `migration_verify`,
  `support_read_model`, `integrity_pipeline`
- `severity`: `info`, `warning`, `error`, `critical`
- `status`: `open`, `acknowledged`, `resolved`, `ignored`

Unity client diagnostic events remain client-side correlation hints. They are
not authoritative server-side error Source of Truth unless a later sanitized
ingestion path explicitly writes a server-owned `ops_error_logs` summary row.

<a id="source-59ff0027b0008512-masking-policy"></a>
#### Masking Policy

Never store or display:

- `Authorization`
- access tokens
- refresh tokens
- Cloudflare Access JWTs
- service token client secrets
- API tokens
- Worker secrets
- raw request bodies
- raw response bodies that may contain secrets
- raw RNG material
- RNG seeds or seed preimages
- player credentials or identity-provider raw assertions

D1 summaries may store:

- structured `errorCode`
- a short `messageSummary`
- stable resource IDs needed for correlation
- `requestId`
- sanitized retryability and masking flags
- `payloadRef` / `traceRef` pointers

Stack and metadata policy:

- Store stack summaries only after path/token/request-body scrubbing.
- D1 should store at most a short stack fingerprint or summary.
- R2 detail payloads, if added, must be sanitized before write and should
  include `masked: true`.
- OWNER may view stack/metadata details in a future detail endpoint.
- OPERATOR/VIEWER should default to summary-only unless a later policy expands
  access.

If masking cannot be confidently applied, drop the field and keep only the
summary/error code.

<a id="source-59ff0027b0008512-retention-candidates"></a>
#### Retention Candidates

DEV:

- D1 summaries: 14 to 30 days.
- R2 sanitized detail payloads: 7 to 14 days.
- Workers Logs: platform default or short operational window.

PRD:

- D1 summaries: 90 days for support/integrity triage.
- R2 sanitized detail payloads: 14 to 30 days, shorter than summaries.
- Aggregated metrics derived from summaries may live longer if they do not
  contain personal data or sensitive payloads.

Open decision:

- Pick exact PRD retention after legal/privacy review and before production
  resource creation.

<a id="source-59ff0027b0008512-access-control"></a>
#### Access Control

Permission:

```text
OPS_ERROR_LOG_READ
```

Current read policy:

- OWNER: list + detail.
- OPERATOR: list + detail.
- VIEWER: denied by default for launch ops error logs.

The current implementation uses `ROLE_PERMISSION`; `admin_principals.role`
maps to `OPS_ERROR_LOG_READ`.

Do not use this log model for operator action auditing. Keep admin actions in
`admin_audit_logs`.

<a id="source-59ff0027b0008512-admin-api-endpoints"></a>
#### Admin API Endpoints

List:

```text
GET /v1/admin/ops/error-logs
```

Filters:

```text
environment=DEV|PRD
service=admin_api|game_api|worker_cron|migration_verify|support_read_model|integrity_pipeline
source=admin_api|game_api|worker_cron|migration_verify|support_read_model|integrity_pipeline
severity=info|warning|error|critical
status=open|acknowledged|resolved|ignored
errorCode=<stable-code>
requestId=req_...
accountId=account_...
runId=run_...
rewardPreviewId=reward_preview_...
rewardClaimId=reward_claim_...
offlineRewardClaimId=offline_claim_...
gachaBatchId=gacha_batch_...
gachaBannerId=banner_...
storyProgressId=opn_...
integrityEventId=integrity_...
supportTicketId=ticket_...
occurredFrom=<ISO UTC>
occurredTo=<ISO UTC>
limit=50
cursor=<offset-cursor>
```

List response:

```json
{
  "permission": "OPS_ERROR_LOG_READ",
  "permissionMode": "ROLE_PERMISSION",
  "items": [],
  "nextCursor": null
}
```

Detail:

```text
GET /v1/admin/ops/error-logs/:errorLogId
```

Detail response:

```json
{
  "item": {
    "id": "ops_error_...",
    "occurredAt": "2026-08-27T00:00:00.000Z",
    "environment": "DEV",
    "service": "game_api",
    "severity": "error",
    "errorCode": "gacha_result_persist_failed",
    "messageSummary": "Gacha result persistence failed.",
    "status": "open",
    "source": "game_api",
    "requestId": "req_...",
    "accountId": "account_...",
    "runId": null,
    "rewardPreviewId": null,
    "rewardClaimId": null,
    "offlineRewardClaimId": null,
    "gachaBatchId": "gacha_batch_...",
    "gachaBannerId": "banner_...",
    "storyProgressId": null,
    "integrityEventId": null,
    "supportTicketId": null,
    "idempotencyOperation": "gacha_pull",
    "idempotencyKeyHash": "sha256:...",
    "retryable": true,
    "masked": true,
    "payloadRef": null,
    "traceRef": null,
    "resolvedAt": null,
    "resolvedBy": null,
    "resolutionNote": null,
    "createdAt": "2026-08-27T00:00:00.000Z",
    "links": {
      "accountId": "account_...",
      "runId": null,
      "rewardClaimId": null,
      "offlineRewardClaimId": null,
      "gachaBatchId": "gacha_batch_...",
      "gachaBannerId": "banner_...",
      "integrityEventId": null,
      "supportTicketId": null
    },
    "correlation": {},
    "sanitizedContext": {}
  },
  "detail": null
}
```

`detail` remains `null` unless a later R2/trace detail source is approved and
permission-gated.

Request bodies on list/detail routes are rejected with `contract_violation`.
Missing details return `ops_error_log_not_found`.

<a id="source-59ff0027b0008512-correlation"></a>
#### Correlation

`requestId` should become mandatory for all server-side error events. For HTTP
routes, prefer:

1. incoming `x-request-id` if valid and safe;
2. generated `req_...` fallback.

Resource correlation candidates:

- `accountId`: from authenticated Game API account context or Admin principal
  action context when safe.
- `runId`: run finish, reward preview, run validation, integrity cases.
- `rewardPreviewId`: reward preview lookup/claim failures.
- `rewardClaimId`: committed claim or replay-related failures.
- `gachaBatchId`: committed or partially prepared gacha batch context.
- `gachaBannerId`: banner-scoped gacha failures.
- `requestId`: join key to Workers Logs and Admin API request context.

Admin links:

- `auditLogId` for operator action failures where an admin audit row exists.
- `rewardClaimId` link to reward claim/read model when available.
- `gachaBatchId` link to future gacha read-only admin screen.
- `accountId` link to player detail.
- `runId` link to run detail.

<a id="source-59ff0027b0008512-unity-diagnostic-source"></a>
#### Unity Diagnostic Source

Unity diagnostic payloads are client-side QA/debug events. Current Unity
boundary:

- fail-soft / non-blocking;
- no actual remote transport yet;
- no bearer token / refresh token / secret / raw RNG material;
- no Unity-side reward/probability/pity/guarantee/ledger/economy mutation.

Two modeling options:

1. Separate model: keep Unity diagnostic events in a future
   `client_diagnostic_logs` model and link only suspicious/relevant events to
   ops error logs.
2. Shared summary model: write sanitized client diagnostic summaries into the
   same ops summary table with `service = 'Unity Client Diagnostic'`.

Recommendation:

- For the first server design, use the shared summary model only for sanitized
  diagnostics that indicate a failed or degraded gacha/run flow.
- Keep Unity diagnostic transport fail-soft and never block gacha pull,
  idempotency replay, run finish, or reward claim UX.
- Do not treat client diagnostics as authoritative server error Source of
  Truth. They are correlation hints.

<a id="source-59ff0027b0008512-current-implementation-slice"></a>
#### Current Implementation Slice

Completed locally:

1. Added migration `0016_ops_error_logs.sql` for the D1 summary table only.
2. Added read-only Admin API list/detail contract with unit and local D1 smoke
   coverage.
3. Added permission `OPS_ERROR_LOG_READ`.
4. Added a local sanitized event insert envelope helper.
5. Deferred Queue/R2/Workers Logs bindings until explicit remote resource
   approval.

Remote DEV and PRD resource creation remain approval-gated.

<a id="source-59ff0027b0008512-open-decisions"></a>
#### Open Decisions

- Exact PRD retention periods.
- Whether VIEWER can access list summaries or only OWNER/OPERATOR can.
- Whether stack metadata is OWNER-only or hidden entirely until R2 detail
  payloads exist.
- Queue name, R2 bucket name, and lifecycle policies.
- Whether Workers Logs export becomes an official source or remains a platform
  troubleshooting companion.
- Whether Unity diagnostics use a separate table in the long term.

<!-- source-body:source-59ff0027b0008512:end -->


<a id="source-4a9273893fe00b6c"></a>
## Error log category and tracing

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/ops-log-tracing-policy.md`  
원문 SHA-256: `f9c33a7dcafecdf3122ae7bd2db1c820db4c7821972aa09a232c3d30726ae1a7`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-4a9273893fe00b6c:start -->
<a id="source-4a9273893fe00b6c-error-log-category-and-tracing"></a>
### Error log category and tracing

- Server: API, cron/worker, migration verification, support read model, integrity pipeline.
- Frontend: browser or Unity runtime failures. Pages Functions execute on the server.
- Frontend collection is connected through `POST /v1/client-errors` for bounded
  Unity diagnostics only. Admin `category=frontend` filters rows whose sanitized
  context has `clientDiagnosticCategory=frontend`; ordinary Game API failures do
  not become frontend events merely because a client triggered them.
- The frontend ingest stores only fixed client diagnostic kind, occurrence
  count, timestamp, authenticated-current-session scope, and app version
  metadata. Raw log text, stack traces, tokens, account shortcuts, arbitrary
  client payloads, and screenshots are rejected or omitted.
- `AUTHENTICATED_CURRENT_SESSION` is not a body-supplied account selector. The
  server binds frontend diagnostics to the Bearer-authenticated account only.
  Unity may upload events captured after the current account session became
  active and before that local owner was replaced or cleared. Same-account
  refresh rotation can keep the attribution; pre-login, previous-account, and old
  process buffers are unsupported for launch ingestion.
- The local code path returns frontend rows through
  `GET /v1/admin/ops/error-logs?category=frontend` with
  `collectionAvailable=true` after `0b823d0`. The same read model exposes
  nullable `appVersion` and `appVersionCode`, and supports exact
  `appVersion`/`appVersionCode` filters for frontend diagnostics. DEV/PRD
  environments require the matching approved deployment before Admin should
  display it as live.
- id is the immutable event tracking code. requestId correlates events within a
  request. Never manufacture a requestId for legacy events without one.
- service/source and environment identify the origin. Safe resource IDs and traceRef
  provide additional lookup references; a reference does not guarantee the resource exists.
- Producers must use stable errorCode values and sanitized messageSummary. Never put
  credentials, request bodies, session tokens or raw stack dumps in tracking IDs/messages.
- Search uses bound parameters and literal substring matching (max 200 chars)
  across safe summaries and references, before pagination. Environment, service,
  severity, status and occurredFrom/occurredTo combine with AND.
- Search/category filters are part of SQL `WHERE` before `ORDER BY`, `LIMIT`,
  and `OFFSET`; pagination is over the filtered result set. Frontend app
  version/code filters use the same path.
- Frontend diagnostic collection has a route-specific local server guard of 12
  new ingest batches per authenticated account per 10 minutes, plus 25
  events/request and 60 occurrences/event payload caps. This is a bounded
  diagnostic intake, not a complete public PRD abuse-control story; broader
  Cloudflare/WAF/IP/device controls remain a release operations decision.
- UI dates display KST; date filter inputs use browser-local time converted to ISO UTC.
- Tab/filter changes reset pagination and selected detail. Detail only renders safe
  reference fields; no resolve/acknowledge or other mutation actions are provided.

<!-- source-body:source-4a9273893fe00b6c:end -->


<a id="source-7ec22b0ea5cfd630"></a>
## Raid Boss Run Contract

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/raid-boss-run-contract.md`  
원문 SHA-256: `322874f7b20b4b65c9f69830ca67b56c86f719638dfde1fdcf5a2a010f89cf06`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-7ec22b0ea5cfd630:start -->
<a id="source-7ec22b0ea5cfd630-raid-boss-run-contract"></a>
### Raid Boss Run Contract

This draft captures the Act 1 raid impact on the existing run lifecycle.

Scope:

- Local contract only.
- No DEV/PRD/STAGING deploy, secret, or remote smoke.
- No server-owned animation timeline, combat replay, camera, VFX, SFX, or hit-stop state.
- Rail Choice Events remain backlog and are not part of the run finish payload.

<a id="source-7ec22b0ea5cfd630-decision"></a>
#### Decision

Use the existing run lifecycle and extend its metadata instead of opening a
raid-specific route for launch.

Reasons:

- `raid_raider_king_engine` still participates in the same account, run,
  idempotency, reward preview, first clear, story progress, and offline reward
  systems.
- Duplicate replay can continue to use `idempotency_records(operation =
  'run_finish')` and the stored `rewardPreviewId`.
- Unity can keep one result submit adapter and branch presentation by
  `encounterType`.

A future raid-specific route can be reconsidered only if raids need a different
server-owned lifecycle, matchmaking, seasonal state, or multi-attempt boss save
model.

<a id="source-7ec22b0ea5cfd630-encounter-types"></a>
#### Encounter Types

Launch encounter types:

- `ORDINARY_15_WAVE`: normal Act 1 operation. Completion is `VICTORY` with
  `completedWaveCount = 15`.
- `RAID_BOSS`: Act 1 boss-only raid. Completion is `VICTORY` with
  `raidResult.bossDefeated = true`.

Ordinary Act 1 operations remain 15-wave runs.

`raid_raider_king_engine` is a boss-only encounter. It starts with the ordinary
full build package: allowed operator slots plus the start-of-run augment/tactic
package that a 15-wave run would have accumulated before the boss fight.

<a id="source-7ec22b0ea5cfd630-run-start-metadata"></a>
#### Run Start Metadata

Run start/read metadata distinguishes the raid without requiring a raid-specific
route. The local launch route is:

```text
POST /v1/runs/start
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

The body contains only the server-created `operationPreviewId`. The response
returns server-authored refs from `operation_previews.preview_json`:

```json
{
  "operationId": "raid_raider_king_engine",
  "encounterType": "RAID_BOSS",
  "raidId": "raid_raider_king_engine",
  "bossId": "boss_raider_king_engine",
  "bossPhaseRefs": ["crown_chase", "trophy_break", "engine_stop"],
  "allowedOperatorSlots": 5,
  "startAugmentPackageRef": "act1_full_start_package_v1",
  "startTacticPackageRef": "act1_full_start_package_v1",
  "contentVersion": "act1.launch.content.v1"
}
```

The server may store refs/ids that let Unity select the correct authored
content. The server does not own frame-by-frame animation data, VFX timing,
camera choreography, hit stop, SFX playback, or combat cinematic timelines.

Admin/support read models may expose the same sanitized refs from
`operation_previews.preview_json` so operators can distinguish ordinary
15-wave operations from raid boss-only encounters. Raid-only refs are only
client-visible when `encounterType = RAID_BOSS`; ordinary previews must not leak
or reinterpret `raidId`, `bossId`, or `bossPhaseRefs` if malformed preview JSON
contains them.

Run start uses the same preview consume guard for raids as ordinary operations.
If a raid preview has already been consumed without a matching
`Idempotency-Key` replay record, the route returns `409
operation_preview_already_consumed` and does not create a second raid run. A
retry with the same account, operation `run_start`, key, and
`operationPreviewId` returns the stored `STARTED` response shape.

<a id="source-7ec22b0ea5cfd630-run-finish-payload"></a>
#### Run Finish Payload

Ordinary finish keeps the existing payload:

```json
{
  "runId": "run_...",
  "currentStage": "Complete",
  "currentWaveNumber": 0,
  "completedWaveCount": 15,
  "encounterType": "ORDINARY_15_WAVE",
  "outcome": "VICTORY",
  "runtimeStateJson": "{}",
  "tacticSelections": [],
  "clientCompletedAt": "2026-08-27T00:00:00.000Z"
}
```

Raid finish uses boss result metadata:

```json
{
  "runId": "run_...",
  "currentStage": "Complete",
  "currentWaveNumber": 0,
  "completedWaveCount": 0,
  "encounterType": "RAID_BOSS",
  "raidResult": {
    "raidId": "raid_raider_king_engine",
    "bossId": "boss_raider_king_engine",
    "bossDefeated": true,
    "completedPhaseCount": 3,
    "encounterDurationSeconds": 420
  },
  "outcome": "VICTORY",
  "runtimeStateJson": "{}",
  "tacticSelections": [],
  "clientCompletedAt": "2026-08-27T00:00:00.000Z"
}
```

For `RAID_BOSS`, `completedWaveCount` remains a storage-compatible final result
field and may be `0`; the server maps verified boss defeat to full completion
for reward, first clear, and story progress calculation.

The accepted run finish transaction stores a sanitized
`serverPreviewMetadata` snapshot in `run_final_results.payload_json`. It is a
support/debug snapshot of server-authored preview refs, not a new combat
simulation contract:

```json
{
  "serverPreviewMetadata": {
    "operationId": "raid_raider_king_engine",
    "actId": "act_01_wasteland_railway",
    "regionId": "region_wasteland_railway",
    "encounterType": "RAID_BOSS",
    "raidId": "raid_raider_king_engine",
    "bossId": "boss_raider_king_engine",
    "bossPhaseRefs": ["crown_chase", "trophy_break", "engine_stop"],
    "allowedOperatorSlots": 5,
    "startAugmentPackageRef": "act1_full_start_package_v1",
    "startTacticPackageRef": "act1_full_start_package_v1",
    "balanceVersion": "1",
    "contentVersion": "act1.launch.content.v1"
  }
}
```

Duplicate run finish replay returns the stored result/reward preview refs and
does not recompute this metadata.

<a id="source-7ec22b0ea5cfd630-story-raid-failure-stamina-refund"></a>
#### Story Raid Failure Stamina Refund

Act 1 story raid failure has a launch-only stamina refund commit. It is not
World Raid settlement and it does not share World Raid score/ranking/reward
tables.

For `raid_raider_king_engine`, a valid finish with:

- `encounterType = RAID_BOSS`;
- `outcome = FAILURE`;
- `raidResult.bossDefeated = false`;

returns and stores:

```json
{
  "staminaRefund": {
    "status": "COMMITTED",
    "refundType": "STAMINA",
    "reason": "STORY_RAID_FAILURE",
    "policyRef": "act1_story_raid_failure_stamina_refund_v1"
  }
}
```

The marker is safe for Unity display and support correlation. It deliberately
does not include refund amount, fuel balance, raw run start cost, or fuel
ledger ids. The accepted finish transaction stores the marker in
`run_final_results.payload_json.staminaRefund` and in
`run_reward_previews.reward_plan_json.calculation.staminaRefund`, then writes:

- `account_mutations.mutation_type = STORY_RAID_STAMINA_REFUND`;
- one `fuel_ledger` row with reason `STORY_RAID_FAILURE_REFUND`;
- a `fuel_state` update capped by `fuel_state.max_fuel`.

The refund amount is derived from the server balance config `fuel.runStartCost`.
The ledger delta records the effective applied fuel amount after the max-fuel
cap. If the policy or account fuel state is missing, the finish is rejected with
a stable state/policy error instead of accepting a partial finish.

Duplicate finish replay reads the stored payload marker and returns the same
stable marker. It must not recalculate, create another reward preview, or apply
stamina twice. Ordinary 15-wave operations, successful boss defeats, and
non-story raid scopes return `staminaRefund: null`.

<a id="source-7ec22b0ea5cfd630-reward--story--offline-effects"></a>
#### Reward / Story / Offline Effects

For `raid_raider_king_engine`:

- First clear eligibility uses `accountId + operationId + risk` as before.
- First clear applies when `outcome = VICTORY` and `raidResult.bossDefeated =
  true`.
- Failure stamina refund eligibility uses the story raid commit above and does
  not grant first clear, story progress, or offline progress.
- Reward preview generation uses the server run reward config and treats boss
  defeat as full completion.
- Story progress advances to `raid_raider_king_engine` only after verified boss
  defeat.
- Offline reward cap/multiplier later reads server-stored story progress, not
  Unity-supplied values.
- Duplicate run finish replay returns the stored `rewardPreviewId` and does not
  recalculate reward, first clear, story progress, or raid result metadata.

<a id="source-7ec22b0ea5cfd630-forbidden-payload--response-data"></a>
#### Forbidden Payload / Response Data

Do not submit, store as structured contract fields, or return:

- raw animation timeline;
- frame data;
- VFX/SFX/camera/hit-stop tracks;
- raw RNG material or seed preimage;
- probability/weighted internals;
- raw validator internals;
- token, secret, auth headers, or account shortcut identifiers.

Unity owns fully animated combat presentation. The server owns durable account,
run, reward, story, and economy truth.

<a id="source-7ec22b0ea5cfd630-tests"></a>
#### Tests

Contract smoke should cover:

- ordinary operation `VICTORY + completedWaveCount=15` remains unchanged;
- raid boss `VICTORY + bossDefeated=true + completedWaveCount=0` creates an
  active reward preview and first clear/story progress updates;
- ordinary operation rejects raid metadata;
- raid operation rejects missing raid metadata;
- duplicate replay returns the stored `rewardPreviewId`;
- story raid failure returns the committed `staminaRefund` marker, writes one
  fuel ledger row, updates fuel state, and duplicate replay does not write a
  second refund;
- forbidden animation/raw RNG/internal validator fields are rejected or never
  returned.

<a id="source-7ec22b0ea5cfd630-decision-queue"></a>
#### Decision Queue

- Final raid start metadata ids: `bossId`, `bossPhaseRefs`,
  `startAugmentPackageRef`, and `startTacticPackageRef`.
- Whether raid failure needs phase-specific reward scaling beyond the launch
  stamina refund marker.
- Whether raid completion needs a future seasonal/attempt table. Not launch
  scope unless explicitly approved.

<!-- source-body:source-7ec22b0ea5cfd630:end -->


<a id="source-8ca626676d8145d6"></a>
## Realtime Coop Combat Contract

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/realtime-coop.md`  
원문 SHA-256: `f6eeb7f888dd38e9ae8c04ddadf29bba2ee50dfd1c86d96d9eca54c7d994fa52`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-8ca626676d8145d6:start -->
<a id="source-8ca626676d8145d6-realtime-coop-combat-contract"></a>
### Realtime Coop Combat Contract

Status: first server skeleton, local contract only.

<a id="source-8ca626676d8145d6-launch-decision"></a>
#### Launch Decision

The two-player mode is real-time cooperative combat. The server owns validation
and result judgment. Unity owns presentation, input capture, animation, VFX,
SFX, camera, and local interpolation.

Recommended Cloudflare shape:

- one Durable Object per coop combat room/run
- hibernatable WebSocket transport
- room state and accepted input references stored in the Durable Object's
  SQLite storage
- Game API resolves Bearer auth before routing to the room object

Cloudflare's current Durable Objects WebSocket guidance recommends the
hibernation API for WebSocket server use because the object can sleep while
connections stay attached. The room object therefore uses
`DurableObjectState.acceptWebSocket(...)` rather than keeping an always-hot
in-memory server.

<a id="source-8ca626676d8145d6-current-endpoints"></a>
#### Current Endpoints

```text
POST /v1/realtime/coop/rooms
GET /v1/realtime/coop/rooms/:roomId/connect?runId=<runId>
```

`POST /v1/realtime/coop/rooms` requires:

- `Authorization: Bearer <access-token>`
- `Idempotency-Key`
- body `{ "runId": "run_..." }`

Response:

```json
{
  "accepted": true,
  "roomStatus": "CREATED",
  "realtimeMode": "COOP_2P",
  "roomId": "coop_room_run_...",
  "runId": "run_...",
  "maxPlayers": 2,
  "transport": {
    "kind": "WEBSOCKET",
    "connectPath": "/v1/realtime/coop/rooms/coop_room_run_.../connect?runId=run_..."
  },
  "serverAuthority": {
    "combatResult": "SERVER_VALIDATED",
    "reward": "SERVER_SOURCE_OF_TRUTH"
  },
  "errorCode": null,
  "message": null
}
```

If `REALTIME_COOP_ROOMS` is not configured, the route fails closed:

```json
{
  "accepted": false,
  "roomStatus": "REJECTED",
  "realtimeMode": "COOP_2P",
  "roomId": null,
  "runId": "",
  "maxPlayers": 2,
  "transport": {
    "kind": "WEBSOCKET",
    "connectPath": null
  },
  "serverAuthority": {
    "combatResult": "SERVER_VALIDATED",
    "reward": "SERVER_SOURCE_OF_TRUTH"
  },
  "errorCode": "realtime_coop_unavailable",
  "message": "Realtime coop room service is not configured."
}
```

`GET /v1/realtime/coop/rooms/:roomId/connect` requires a WebSocket upgrade and
Bearer auth. The Game API strips the raw `Authorization` header before
forwarding the request to the Durable Object and passes only server-resolved
internal connection context headers.

<a id="source-8ca626676d8145d6-client-message-envelope"></a>
#### Client Message Envelope

Current accepted client message types:

- `READY`
- `PLAYER_INPUT`
- `HEARTBEAT`
- `CLIENT_STATE_HINT`

Minimum client message:

```json
{
  "type": "PLAYER_INPUT",
  "clientMessageId": "input_001",
  "payload": {
    "inputRef": "move_slot_a"
  }
}
```

The first skeleton acknowledges accepted input as
`SERVER_INPUT_ACCEPTED`. It does not yet implement the full combat simulation,
matchmaking, reconnect resume model, or final result commit.

<a id="source-8ca626676d8145d6-forbidden-client-fields"></a>
#### Forbidden Client Fields

Realtime client payloads must not contain:

- `accountId` / `account_id`
- final combat result or outcome
- reward lines, currency grants, item grants, or economy mutation fields
- raw RNG, seed, probability, or weight internals
- animation/VFX/SFX/camera/hit-stop timeline payloads

Those remain server-owned or Unity-local presentation data and are not part of
the realtime server authority contract.

<a id="source-8ca626676d8145d6-current-boundaries"></a>
#### Current Boundaries

Implemented in this slice:

- Game API room create route
- Game API WebSocket connect route
- `RealtimeCoopRoom` Durable Object skeleton
- Durable Object SQLite schema for rooms, participants, and accepted input refs
- fail-closed behavior when the room binding is unavailable
- public response shape with `maxPlayers = 2`

Not implemented yet:

- matchmaking queue
- invite code lifecycle
- second-player join route
- reconnect token/resume protocol
- authoritative combat simulation
- final coop result commit into run finish/reward/story/offline systems
- Admin read model
- remote deploy or production Durable Object migration

<a id="source-8ca626676d8145d6-error-codes"></a>
#### Error Codes

- `auth_required`
- `access_token_invalid`
- `auth_session_invalid`
- `account_not_active`
- `idempotency_key_required`
- `contract_violation`
- `realtime_coop_unavailable`
- `realtime_websocket_required`
- `realtime_coop_room_full`

<a id="source-8ca626676d8145d6-follow-up-decision-queue"></a>
#### Follow-Up Decision Queue

- Whether coop rooms are created from an existing solo `runId` or a dedicated
  coop run-start route.
- Whether joining uses invite codes, friend invites, or match queue.
- Reconnect TTL and stale input replay rules.
- Server simulation tick rate and accepted input batching interval.
- How coop final results map to existing `POST /v1/runs/:runId/finish`.
- Whether Act 1 launch includes coop or keeps it as post-launch while single
  player remains the first release path.

<!-- source-body:source-8ca626676d8145d6:end -->


<a id="source-a3b56fbe44706c7f"></a>
## S09/S07 recurring-v1

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/recurring-objectives.md`  
원문 SHA-256: `cbdb4e390e46a04b5d4d47ae24a6f735f44b617c6a61287ea7a1a914d0fd212d`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-a3b56fbe44706c7f:start -->
<a id="source-a3b56fbe44706c7f-s09s07-recurring-v1"></a>
### S09/S07 recurring-v1

Local implementation; no remote migration, seed or deployment performed. Current code including bulk claims requires migrations0001–0035 in order on game-api and async-worker's same DB. Migration0033 adds immutable source events, versioned operation mappings/season definitions, earned rights, receipts, quest mail, checkout intents, entitlements and a job cursor;0035 adds durable bulk requests/members. It preserves existing crystal lots/ledger while adding QUEST provenance. It seeds no product, cosmetic, operation or season.

All routes authenticate the account, return Cache-Control:no-store and reject query parameters. POST bodies are strict; empty means zero bytes, not `{}`. Every mutation except sync requires printable ASCII Idempotency-Key (1–128 chars). Reuse a key only for an identical request. Existing successful keys return the original receipt, including after expiry/revocation; they do not establish current eligibility.

| Method/path | Body | Result |
| --- | --- | --- |
| GET /v1/recurring/state | empty | version, serverTime, periods, policies, awards, mail, grants, eventStatus, capabilities, progressSyncPending; never grants |
| POST /v1/recurring/login | empty | accepted, eventSource, dayStart, refreshRequired; one login source per account/server day |
| POST /v1/recurring/sync | empty | addedObjectives,lastEventId; derives rights/XP, does not pay resources |
| POST /v1/quests/:awardId/claim | empty | grant receipt; only current earned unclaimed right |
| POST /v1/quest-mail/:awardId/claim | empty | same underlying quest right; strictly before mail expiry |
| POST /v1/passes/:policyId/levels/claim | level:1–50,track:FREE or PREMIUM | same right/receipt used by settlement |
| POST /v1/passes/:policyId/checkout-intents | empty | intentId,expiresAt,purchaseEndsAt,seasonEndAt,productId |
| POST /v1/passes/:policyId/purchases/reconcile | intentId,receipt | premium,seasonEndAt,settlementRequired,xpGranted:0 |

Grant receipts contain accepted,rightKey,policyId,claimId,rewardTransactionId,accountMutationId,serverClaimedAt,rewardLines,wallet,refreshRequired. Error shape is accepted:false,errorCode; 400 malformed input,403 ownership/verification/entitlement rejection,409 changed state or ineligible period,503 missing configuration/binding/verifier. Clients refresh state after login/sync/grants and use returned policy/award IDs.

<a id="source-a3b56fbe44706c7f-source-qualification-and-periods"></a>
#### Source qualification and periods

Login and accepted run-finish sources use DB commit time, KST04 day boundaries. Natural monthly attendance shares the login source; recovery does not create a login or finish. Finish insertion is in the existing final-result transaction; replay cannot create another source or retime it. Raw source identifiers, content version, outcome, verified completion flag and timestamps remain for future authorized reconciliation.

Known six story operations support full success only. Story boss counts one end/zero waves only when the existing finish validator confirms bossDefeated; VICTORY text alone is insufficient. Ordinary15 uses an explicit operation_id/content_version mapping: success1end/15waves; normal failure1–14 completed waves counts1end/actual waves; zero-wave failure, abandon and invalid result count0. Story15 defeat remains UNSUPPORTED_STORY_DEFEAT pending cost/refund contract. World uses a separate score track. Unknown operation bindings remain OPERATION_BINDING_MISSING, never relabeled from story IDs.

The current finish producer is the existing run owner's committed result. Cooperative eligible original participants require an additional authoritative producer; capability is NOT_IMPLEMENTED and their progress is UNKNOWN_FOR_UNLINKED_ORIGINAL_PARTICIPANTS, not zero or an approved owner-only rule. Durable Object presence/damage is not eligibility evidence. No automatic retrospective grant is implemented.

Daily pass D1 login40/D2 one end80/D3 two ends80. Weekly W1 three logins/W2 six ends/W3 sixty waves each200. Pass weeks are season days1–7,8–14,15–21,22–28; no week5 XP. Ordinary quest weeks start Monday04 KST. Ordinary D2/D3 and W1/W2/W3 use the approved direct reward vectors in recurring-policy.ts. Same source feeds both systems independently; claim/mail never adds XP. Event time owns the period even if sync runs later.

<a id="source-a3b56fbe44706c7f-pass-mail-and-settlement"></a>
#### Pass, mail and settlement

PASS50 definitions cover exactly one service-calendar month. One level per100XP, displayed maximum5000XP/level50; earnedXp retains the uncapped objective audit sum. Daily200/weekly600 reaches level50 on day17. All50 free rows and exact5x premium extra rows are returned in rewardRows with xp threshold, freeState/premiumState, cosmeticSlot and nullable cosmeticItemId. Current premium requires an owned verified nonrevoked order. Cosmetic slots are frame30/operator skin40/train skin50; no actual IDs are supplied by this migration.

Ended earned unclaimed ordinary quest rights become mail at actual DB send time, expiring exactly14days later. Sending pays nothing; retries never renew expiry; expired unclaimed mail cannot be collected. Pass earned unclaimed rights instead settle directly without expiring mail. Manual and worker claims share the same right identity and atomic wallet/ledger receipt. Late source processing and verified old-season purchases remain eligible for that season's settlement, without next-season XP or entitlement.

async-worker scheduled handler is configured every5minutes in local/dev/prd configuration only. Each invocation visits up to10 accounts and pays up to20 pass rights per account. Durable cursor wraps, retaining failed/pending rights. This is not evidence of a deployed scheduler. Per-account historical source/definition scans and mail processing are not paginated; production volume/load validation remains necessary. settlementStatus distinguishes NOT_ENDED,SOURCE_SYNC_PENDING,SETTLEMENT_PENDING,SETTLED_FOR_RECORDED_SOURCES; the last does not assert missing cooperative sources are complete.

Checkout closes24hours before end. An intent lasts at most10minutes and cannot outlive the season. A trusted verifier must tie policyId, intentId, account, provider/app/product, environment, store transaction and purchase time to the proof. A valid purchase within the previously issued intent can reconcile after checkout/season end. No client assertion substitutes for PASS50_PURCHASE_VERIFIER; no deployed adapter is provided. Stored revoked orders reject new reconciliation, and premium grants recheck revocation at commit. Original success keys replay historical receipts.

<a id="source-a3b56fbe44706c7f-configuration-and-validation-boundary"></a>
#### Configuration and validation boundary

<a id="source-a3b56fbe44706c7f-additive-objectiveprogress-read-contract-objective-progress-v1"></a>
##### Additive objectiveProgress read contract (objective-progress-v1)

GET state now includes `objectiveProgress:{version,serverTime,appliedEventId,latestEventId,systems}`. This nested object uses one read-only DB snapshot for clock, events, definitions, earned rights and claim state. Its timestamp/cursors own these counters; do not combine counters with a separately fetched response or infer that the older top-level multi-query state is the same snapshot. No new migration, objective, reward, login event, sync or grant is introduced by GET.

systems has QUESTS and PASS50 entries. Each is `{system,policyId,status,objectives}`. An active registered definition returns all existing approved objectives, including unmet ones: QUESTS D2,D3,W1,W2,W3; PASS50 D1,D2,D3,W1,W2,W3. Missing current definition returns policyId:null,status:NOT_CONFIGURED_FOR_CURRENT_PERIOD,objectives:[]; the server does not invent a live season. Ended rights/mail remain in the existing state fields.

Each objective provides objectiveId,frequency,metric,target,periodId,periodStartAt,periodEndAt,calendar,current,observedCurrent,currentScope,coverage,unknownReasons,observedTargetReached,syncPending,awardId,status,xp,rewards. Specs/targets/rewards are the same constants consumed by award derivation. DAILY resets04KST; QUESTS weekly calendar MONDAY_04_KST; PASS50 weekly calendar SEASON_RELATIVE_7_DAYS. On season day29 onward the three pass weekly specs remain visible as NOT_SCHEDULED with null period/current values, no week5 reward or inferred zero progress.

Login current is the exact count of recorded eligible server login events in that period, with scope SERVER_RECORDED_LOGIN/coverage COMPLETE_FOR_RECORDED_LOGIN. Zero is valid within that explicit scope; it is not a claim about every unreported client session. Finish/wave total current is null and coverage PARTIAL_UNKNOWN because the cooperative original-participant producer is missing. observedCurrent is the exact eligible committed owner-source subtotal (a lower bound for the all-source objective), including a valid observed zero; never display it as a complete total. unknownReasons always includes COOPERATIVE_PARTICIPANT_PRODUCER_MISSING for these metrics and adds OPERATION_BINDING_MISSING or STORY_DEFEAT_COST_CONTRACT_PENDING when such source records exist in that period.

observedTargetReached can be true even with incomplete coverage: recorded eligible sources suffice to meet the unchanged threshold. If no persisted award exists, status is SYNC_PENDING and awardId:null; clients sync/refresh before enabling claim and never treat GET as a payout. Persisted awards use EARNED/CLAIMED for quests or XP_APPLIED for pass. Below threshold, status is UNKNOWN for incomplete finish coverage or IN_PROGRESS for recorded login. syncPending is separately true when period eligible sources exceed the applied cursor or a reached threshold lacks its award; UNKNOWN and pending can coexist. A persisted award remains earned while newer sources await sync. Counts follow immutable source event time, so delayed old-day sync does not increase today's daily counters.

<a id="source-a3b56fbe44706c7f-bulk-claims--approved-bulk-claim-v1"></a>
##### Bulk claims — approved bulk-claim-v1

The follow-up Plan delegation approved the per-right partial-success design and limits below. Requires migration0035 after0001–0034. It adds recurring_bulk_requests (immutable account/key/hash/body/list) and recurring_bulk_members (immutable action identity and terminal result). No seed, new reward policy, remote work or payment capability is added. Individual claim APIs and pass settlement retain the same rights.

POST `/v1/recurring/bulk-claims` requires auth and Idempotency-Key and creates/resumes the same frozen request. Strict body is exactly one of:

- `{kind:"QUEST",awardIds:[id,...]}`
- `{kind:"MAIL",awardIds:[id,...]}`
- `{kind:"PASS_LEVEL",policyId,track:"FREE"|"PREMIUM",levels:[1,...]}`

Each list has1–50 unique entries; duplicate, empty, oversized or mixed-kind fields return400. Pass levels are1–50, with one policy and track per request. Membership AND ordering are fixed: any changed body/list/order with the same account/key returns409idempotency_conflict. Items are not automatically added during retry. Idempotency key is scoped to the bulk request namespace, separately from individual claim keys.

GET `/v1/recurring/bulk-claims/:bulkId` authenticates ownership and returns a read-only single-snapshot response without processing. No query/body allowed; foreign/unknown bulk ID returns403bulk_claim_not_found. Both routes use Cache-Control:no-store. After an unknown response before bulkId is known, resend the exact POST/body/key; do not generate a replacement key automatically.

Response: `{accepted:true,version:"bulk-claim-v1",bulkId,request,createdAt,status,successCount,failureCount,pendingCount,totalCount,items,refreshRequired}`. Each item is `{index,action,status,receipt,errorCode}`. SUCCESS means this bulk member committed its grant; REPLAYED means an already committed underlying right receipt was reused. Both contribute to successCount. FAILED is a confirmed failure fixed permanently in this bulk, with errorCode and no receipt. RETRY_PENDING is unresolved, with optional errorCode, and can resume on the original key. Original successful/failed item data remains stable on reads/resumes.

Overall PARTIAL means at least one item is unresolved. COMPLETE means every item is terminal, **not that every item succeeded**: COMPLETE with failureCount50/successCount0 is valid. Clients must display counts and item outcomes separately. HTTP200 reports the accepted bulk state, including pending or failed members; request/schema/conflict/ownership/service failures use400/409/403/503. Global503 after an unknown DB response may still have persisted the request or grants; resume the original key to recover.

Each invocation starts at most10 pending members and stops starting more after a2.5second soft wall-clock budget. One in-flight member operation may finish beyond that budget. Pending members are ordered by attempt count then original index, so persistent transient failures do not starve untouched members. Further work requires the same POST/body/key; GET does not resume. No bulk background worker or automatic target expansion is introduced.

Each member uses the existing right's atomic full reward-vector transaction, including actual wallet/item/currency ledgers. A new grant and SUCCESS result commit together; previously granted rights store REPLAYED with the original receipt. Failures do not roll back other committed members. Before fixing failure, the server rechecks the underlying receipt under the account sequence fence. A pending-member guard also runs in the grant transaction so a racing attempt cannot grant after this bulk member became terminal. Individual, worker and concurrent bulk claims share the original right identity.

Confirmed eligibility/ownership/type failures are terminal, including unearned pass level, missing premium ownership and expired mail. A later changed condition requires a user-explicit retry using a new key. Service/configuration failures503, exhausted state contention and unknown exceptions stay RETRY_PENDING; MAIL awaiting actual mail creation is also pending. Resume checks receipts before eligibility/payout. Internal exception details are not exposed; unknown failures use temporary_processing_failure. Expiry is checked at each actual grant commit, never at the click or request-create time. A failed reward-vector item rolls back all items for that right; no expiry revival, no mixed quest/mail/pass processing.

Validation covers limits/duplicates/mixed fields, changed list/order conflict, frozen50-member bounded continuation, terminal failure remaining fixed after eligibility changes, explicit-new-key retries, atomic vector rollback with another successful member retained, individual/worker/bulk races, same-key concurrent resumes, grant-response loss, request-persistence response loss, owned read-only status and actual mail-expiry crossing. Full-suite/commit evidence is recorded in the handoff.

recurring_definitions policy_json is strict `{version:"recurring-v1",cosmetics:{FRAME:null|string,OPERATOR_SKIN:null|string,TRAIN_SKIN:null|string},purchase:null|{provider:"GOOGLE_PLAY"|"APP_STORE",appId,productId}}`. Definitions cannot overlap within each system or be updated/deleted. QUESTS definition edges are Monday04 KST. Missing actual cosmetic/SKU/adapter blocks checkout; fixture TEST identifiers in tests are not launch configuration. Existing S08 elite ticket and its pool remain separately unbound.

Validation covers actual HTTP false-boss-success exclusion, ordinary failures, independent weekly calendars, day29, login replay and attendance deduplication, concurrent sync/claims/settlement,50levels/premium5x, bounded repeated offline settlement, delayed source processing,14day mail send/expiry boundaries, late verified purchases, wrong ownership/proofs, revocation replay and migration with existing free/paid/revoked-order history. Test counts and immutable commit identity are recorded in the server handoff/report after final checks.

<!-- source-body:source-a3b56fbe44706c7f:end -->


<a id="source-7e409c4a47efc1a6"></a>
## Run combat requirements and original-run recovery

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/run-combat-requirements.md`  
원문 SHA-256: `1640d63c4e7c9b3a4c2d15c238f51ce682e301b94b7c471ecad760e57448dcd6`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-7e409c4a47efc1a6:start -->
<a id="source-7e409c4a47efc1a6-run-combat-requirements-and-original-run-recovery"></a>
### Run combat requirements and original-run recovery

This follow-up supersedes the public paid-start availability at commit
`4ced3496418070fc018e32d16f0315297c506aa3`. That commit verified departure admission,
debit and snapshot persistence (492 tests), not Unity combat loading. Actual
`RunStartResponseJsonAdapter` and `RunStartSnapshotBuilder` require additional
authoritative content/stat inputs that were not supplied. **New S11 paid starts
now return 409 `combat_not_ready` without mutation.** Already committed starts
replay their original response before the new gate. The gate itself needs no
migration; the subsequent [CE01 choice contract](49_SERVER_API_CONTRACTS.md#source-feaac556a37c1cd1) requires
migration0031 and advances diagnostics to v2. See the
[CE02 content/reward review](49_SERVER_API_CONTRACTS.md#source-d1c9e8663b08469b) for current fields.

<a id="source-7e409c4a47efc1a6-field-responsibility-and-evidence"></a>
#### Field responsibility and evidence

Inspected sources on 2026-09-10:

- Server `apps/game-api/src/index.ts`, `departure.ts`, migrations0003/0005/0014,
  and `packages/balance/src/index.ts`.
- Shared source read only:
  `E:/Projects/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/Run/RunStartResponseJsonAdapter.cs`,
  `RunStartSnapshotBuilder.cs`, `RunSnapshot.cs` and scoped Scripts/content searches.
- Canonical read only: `docs/design/train-combat-test-profile-v1.md` and
  `train-loadout-speed-detail-v1.md` under the OneDrive LASTRAIL project.

| Field | Owner / actual source | Type and version boundary | Missing behavior / remaining decision |
| --- | --- | --- | --- |
| runId, serverStartedAt | Server committed run/snapshot | string, ISO UTC; immutable original response | Preview has no run/start time; never use local clock as accepted start |
| operationId / encounter refs | Existing server six story IDs plus raid and existing launch package metadata | Existing strings, `act1.launch.content.v1`; version does not prove loader support | Unity must resolve this exact operation/content/package, not a fabricated fallback |
| balanceVersion | Server active balance at quote, pinned in run | string numeric version | Missing cost/reward policy remains 503; no inferred default |
| firstClearEligible | Server `account_first_clears` keyed account/operation/risk | boolean for new previews/admission; guarded and receipt-pinned | Missing row means potentially eligible; final reward still requires successful completion and finish validation. Old response missing this field stays missing |
| riskLevel / risk | Server risk tier plus explicit projection | Existing root `risk` and run `riskLevel` stay Roman strings; new combatContract.riskLevel is int1..5 | Unity adapter must explicitly map/read the versioned int. Unknown source tier reports null |
| allowedOperatorSlots | Existing server maximum count and actual S03 selection | Root remains number5; combatContract.operatorSlotCount=5; admission.formation.operators contains numeric slot/ID assignments | Legacy string[] semantic contract is unproven. Do not turn5 into invented slot-name strings |
| enemyFactionId | Content owner; no server operation/content-version binding found | null in requirements | Need approved operation+contentVersion→actual faction ID and resolver identity |
| wavePlanId | Content owner; `run_wave_plans` schema exists, no producer/catalog binding found | null in requirements | Need actual wave-plan ID/version/hash and ordinary/raid applicability; table existence is not a plan |
| rewardTableId | Server owns rewards via balance runRewards.operations[operationId]; no separate ID mapping exists | null in requirements; quote balanceVersion+operationId are the real current key | Agree whether Unity accepts that structured reward source or supply an approved actual table ID mapping; never rename operationId into a guessed table ID |
| seed | Server combat protocol owner; Unity only consumes uint/RngSeed | null; uint32 consumer alone is not an issuance protocol | Need battle-only purpose, RNG algorithm/version, generation/lifetime and replay rules. No generated dummy/deterministic constant; no reuse of gacha RNG or disclosure of its internals |
| selected operators/carriages | Server S03/S04 actual ownership and frozen selection | admission formation/loadout, revisions and persisted growth facts | Already provided; no default roster or unowned vehicle substitution |
| ultimateOperatorId | Server CE01 validated selection among owned active five | string or null, with ultimateSelectionRevision | Explicit selection POST/GET and departure-v2 pin; null blocks start, no Kyle/first-operator fallback |
| engineOutput | Balance/content profile owner; Unity TrainCoreCatalog has speed inputs, but canonical profile is explicitly a proposal | null | Canonical `train-combat-test-profile-v1` says RECOMMENDED TEST PROFILE, not production balance. Need approved profile version and level mapping; engine output is not attack |
| effective combat stats | Approved balance/content formulas and Unity resolver | No profile binding yet; persisted growth != resolved stats | Need approved profile/formula version, rounding and loader input contract. No copying candidate HP/ATK/armor values |
| catalog support | Existing client-config/version policy supplies admission and catalog identity; it does not bind these combat fields | requiredContent lists exact existing refs; bindingVersion/catalogHash remain null here | Unity compares/downloads/resolves via existing version flow. A valid catalog admission or client ready=true does not prove a missing combat binding |

No source was found authorizing a concrete faction/wave/reward ID or a battle seed
algorithm. References to seed in gacha logging/commitment policy are not a combat
seed contract. The canonical train profile explicitly prohibits activation by
inference; none of its numeric proposals are installed by this work.

<a id="source-7e409c4a47efc1a6-preview-additions"></a>
#### Preview additions

`POST /v1/operations/preview` remains a strict server quote request. It now returns
and stores `firstClearEligible:boolean` and this diagnostic requirements object:

```ts
type CombatContract = {
  version: "run-combat-requirements-v2";
  status: "BLOCKED";
  canStart: false;
  blockingReasons: string[];
  riskTier: "I" | "II" | "III" | "IV" | "V" | null;
  riskLevel: 1 | 2 | 3 | 4 | 5 | null;
  operatorSlotCount: 5 | null;
  firstClearEligible: boolean | null;
  enemyFactionId: null; wavePlanId: null; rewardTableId: null;
  seed: null; engineOutput: null; ultimateOperatorId: string | null;
  ultimateSelectionRevision: number | null;
  requiredContent: {
    operationId: string | null; contentVersion: string | null;
    startAugmentPackageRef: string | null; startTacticPackageRef: string | null;
    encounterType: string | null; raidId: string | null; bossId: string | null;
    bossPhaseRefs: string[];
    bindingVersion: null; catalogHash: null;
  };
};
```

The six unconditional blocking reasons are `enemy_faction_binding_missing`,
`wave_plan_binding_missing`, `reward_table_binding_missing`,
`combat_seed_contract_missing`, `engine_output_profile_unapproved`,
`effective_stats_profile_unapproved`. `ultimate_selection_required` additionally
appears when there is no valid CE01 admission pin.
These describe absent approved capabilities, not a transient network failure.
Malformed stored source returns 503 `combat_requirement_source_invalid`; database
failure remains a server error. It is not converted into a fake missing catalog,
404, or successful empty response.

First-clear state is rechecked in the preview transaction; a concurrent clear
causes re-read/retry. The exact response is pinned in the immutable preview receipt.
Same-key replay can show the previous true value after a later clear; that is an
old quote, not a new reward entitlement. A new key gets current truth. Retained
atomic start-core logic pins/rechecks this value as well; its public route is gated.

<a id="source-7e409c4a47efc1a6-new-start-gate-and-client-integration"></a>
#### New-start gate and client integration

The HTTP start route performs auth/idempotency replay first. For any uncommitted
S11 admission-version preview it reconstructs current requirements from server
capabilities and returns the existing rejected-start shape plus combatContract:

```json
{"accepted":false,"startStatus":"REJECTED","errorCode":"combat_not_ready"}
```

It does not trust a saved `combatContract.canStart`, request property, client flag
or arbitrary catalog ID as approval. No cost debit, ledger, preview consumption,
run, new start receipt, refund or state transition is written. This also covers
unconsumed S11 previews produced before this follow-up. Unknown/incomplete battle
inputs cannot become playable by editing a boolean.

Unity should inspect preview.combatContract before starting, display the explicit
unavailable requirements, and use the existing client-config/content download flow
to resolve requiredContent. Enabling READY requires a future reviewed server
binding contract and Unity loader agreement, including exact content version/hash
and a pre-start proof bound to that quote. No ready-ack endpoint or client boolean
bypass is implemented now because approved bindings do not yet exist.

Legacy NULL-marker metadata previews retain the preexisting no-new-debit path for
historical compatibility; they are not a supported workaround for real combat or
a new client producer. Old successful S11 and legacy receipts are replayable as
stored. The start gate does not retroactively declare old responses battle-ready.

<a id="source-7e409c4a47efc1a6-get-v1runsrunidstart-state"></a>
#### GET /v1/runs/:runId/start-state

If the client never received runId, use the read-only
[POST /v1/runs/start-result](49_SERVER_API_CONTRACTS.md#source-75e533c458b7b09f) with the original journal key and
previewId. Do not retransmit a start command to discover its result. NOT_FOUND
is indeterminate and does not allow a new key/start.

Bearer auth plus existing client admission; no query/body; Cache-Control:no-store.
ID uses existing safe-ID contract. Read only, account scoped:

```ts
type RecoveryResponse = {
  accepted: true;
  runId: string;
  runStatus: string; // current stored status, including completed/failed
  recoveryStatus: "AVAILABLE" | "SNAPSHOT_UNAVAILABLE";
  startResponse: Record<string, unknown> | null;
};
```

For S11 snapshots, startResponse is the original stored departureResponse exactly;
no current balance, new first-clear value, combat defaults or timestamps are added.
Legacy snapshots use the existing start replay projection. AVAILABLE means the
original response is recoverable, not that Unity has all battle inputs. Missing or
malformed snapshot is 200 SNAPSHOT_UNAVAILABLE with null startResponse. Missing or
foreign run is404 run_not_found; expired/revoked session follows existing401 auth.
Bad ID/query/body is400 contract_violation; database error is not treated as absence.

Resume the same run when its actual loader contract is supported. Never create a
second start or infer an automatic refund from recovery failure. This endpoint
does not alter run status, costs, rewards, ownership or snapshot. A retry of an
already committed start key still returns the original start response before the
combat gate; a fresh key cannot create another paid run.

<a id="source-7e409c4a47efc1a6-verification-scope"></a>
#### Verification scope

Six public-route SQLite tests cover requirements, zero-write rejection despite a
forged stored ready flag, first-clear receipt/commit behavior, original response
recovery/replay, foreign404/session expiry, missing snapshot and invalid source.
The prior twelve atomic departure tests now exercise the retained domain start
function directly through a test-only fixture; the public HTTP paid-start path is
intentionally blocked. Those tests remain transaction/finish compatibility evidence,
not evidence of combat support or a production gate bypass. There is no environment
switch enabling the core from HTTP. Existing legacy metadata tests remain separate.

Actual Unity combat entry remains incomplete pending the decisions in the table.
No new catalog, seed, ultimate selection, combat values, migration, deployment,
remote seed, refund or canonical write was performed.

<!-- source-body:source-7e409c4a47efc1a6:end -->


<a id="source-11d4e6e3b3c22eef"></a>
## Run Result Submit Contract Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/run-result-submit.md`  
원문 SHA-256: `a9fe4a40c39d891046f5c9cb2b20c8e76010cee4a73c8ed48ef8890c6a0d5a32`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-11d4e6e3b3c22eef:start -->
<a id="source-11d4e6e3b3c22eef-run-result-submit-contract-draft"></a>
### Run Result Submit Contract Draft

This draft is the server-side contract for Unity run result submission. The
first local-only route slice is implemented for accepted finish -> reward
preview creation. Full validator integration and final production balance
rollout remain follow-up work.

Reward preview and claim/commit follow-up is defined in `docs/api/run-reward-preview-claim.md`.

<a id="source-11d4e6e3b3c22eef-endpoint"></a>
#### Endpoint

Use the existing run resource shape from the server Source of Truth:

```text
POST /v1/runs/:runId/finish
```

`/v1/runs/result-submit` remains a Unity-side temporary candidate only. The server should prefer `/v1/runs/:runId/finish` because migration 0005 already models `run_final_results.run_id` as the final result primary key and architecture docs already list `POST /v1/runs/:id/finish`.

<a id="source-11d4e6e3b3c22eef-idempotency"></a>
#### Idempotency

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
- Reusing the same `Idempotency-Key` for a different run is a contract
  violation and must not return another run's stored `rewardPreviewId`.

<a id="source-11d4e6e3b3c22eef-request-body"></a>
#### Request Body

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
- Ordinary Act 1 operations use `encounterType=ORDINARY_15_WAVE` and require
  `VICTORY + completedWaveCount=15` for full completion effects.
- `raid_raider_king_engine` uses `encounterType=RAID_BOSS`; `VICTORY +
  raidResult.bossDefeated=true` is treated as full completion for first clear,
  story progress, and reward preview generation even when
  `completedWaveCount=0`.
- Raid/ordinary impact details are in `docs/api/raid-boss-run-contract.md`.
- `outcome` is `VICTORY`, `FAILURE`, or `ABANDONED`.
- Request must not include reward amounts, currency grants, or final reward decisions.
- Rail Choice Events are backlog for Act 1 launch. Request bodies must not
  include `railChoiceResults`, `railChoices`, `choiceResults`, or
  `optionalFlags`.
- `runtimeStateJson` is a stringified JSON payload produced by Unity. The server parses it as validation input only. It is not a reward source of truth.
- `tacticSelections` are client-selected tactical references, not reward
  claims. Launch selections come from server-authored start package refs; see
  `docs/api/run-start-augment-package.md`.
- Submitted `tacticSelections[].packageRef` must match the server-authored
  `startAugmentPackageRef` or `startTacticPackageRef` stored on the operation
  preview.
- Nested raw RNG, probability, reward, economy, ledger, or animation timeline
  data inside `tacticSelections` is a `contract_violation`.

<a id="source-11d4e6e3b3c22eef-response-body"></a>
#### Response Body

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
  "staminaRefund": null,
  "message": null,
  "errorCode": null
}
```

Use ISO UTC `Z` timestamps. Server may emit JavaScript `Date.toISOString()` millisecond precision.

`balanceVersion` is a string in the API contract to match Unity DTO handling and to avoid safe-integer ambiguity if version identifiers become non-numeric later. The server still stores current D1 `balance_config_version` values as INTEGER internally.

<a id="source-11d4e6e3b3c22eef-result-status"></a>
#### Result Status

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

<a id="source-11d4e6e3b3c22eef-http-status"></a>
#### HTTP Status

- `200`: Valid server response DTO, including `ACCEPTED`, `DUPLICATE_REPLAY`, and domain-level `REJECTED`.
- `400`: Contract violation response DTO where the request shape is invalid.
- `401`/`403`: Auth failures before run submission contract processing.
- `409`: Optional for run state conflicts, but prefer `200` + `REJECTED` + `run_state_conflict` for Unity policy consistency.
- `503`: Optional for validator unavailability, but prefer `200` + `REJECTED` + `validator_temporarily_unavailable` when the server handled the request.

<a id="source-11d4e6e3b3c22eef-error-codes"></a>
#### Error Codes

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

<a id="source-11d4e6e3b3c22eef-reward-reference-policy"></a>
#### Reward Reference Policy

Prefer `rewardPreviewId` for the first accepted result submit.

Rationale:

- Unity enters reward review after accepted submit.
- Reward amounts/final grants remain server-side.
- The player can review by fetching the reward preview.
- A later claim endpoint can convert preview to final `rewardClaimId`.

`rewardClaimId` should be reserved for a later claim/commit operation. Do not return final reward amounts in result submit.

<a id="source-11d4e6e3b3c22eef-story-raid-failure-stamina-refund"></a>
#### Story Raid Failure Stamina Refund

Act 1 story raid failure uses a small committed stamina refund marker in the
run finish response. For
`raid_raider_king_engine`, when the server receives a valid `RAID_BOSS`
finish with `outcome = FAILURE` and `raidResult.bossDefeated = false`, the
accepted response includes:

```json
{
  "staminaRefund": {
    "status": "COMMITTED",
    "refundType": "STAMINA",
    "reason": "STORY_RAID_FAILURE",
    "policyRef": "act1_story_raid_failure_stamina_refund_v1"
  }
}
```

Rules:

- The marker is server-authored and displayable by Unity.
- It does not include refund amount, fuel balance, raw cost, or ledger ids.
- It is stored in `run_final_results.payload_json.staminaRefund` and mirrored
  into `run_reward_previews.reward_plan_json.calculation.staminaRefund` as
  sanitized metadata.
- The same accepted finish transaction inserts
  `account_mutations.mutation_type = STORY_RAID_STAMINA_REFUND`, writes one
  `fuel_ledger` row with reason `STORY_RAID_FAILURE_REFUND`, and updates
  `fuel_state` capped by `max_fuel`.
- Refund quantity is server-owned and derived from `fuel.runStartCost`; the
  response does not expose the raw cost or resulting fuel balance.
- `VICTORY + raidResult.bossDefeated=true` does not receive a failure refund
  marker.
- Ordinary 15-wave operations do not receive this marker.
- Duplicate finish replay restores the stored marker and does not recalculate
  or reapply it, so no second fuel ledger row is written.
- Missing refund policy returns `story_raid_stamina_refund_policy_missing`.
- Missing or invalid fuel state returns `story_raid_stamina_refund_state_conflict`
  and may write a scrubbed ops error log.
- World Raid schedule, scoring, ranking, settlement, entry cost, and refund
  rules are separate future scope and must not share this contract.

Duplicate replay response:

- `accepted: false`
- `resultStatus: "DUPLICATE_REPLAY"`
- `rewardPreviewId: "<previous rewardPreviewId>"`
- `rewardClaimId: null`
- `staminaRefund: <stored marker or null>`
- `errorCode: "duplicate_replay"`
- no reward recalculation;
- no second `run_final_results`;
- no second `run_reward_previews` row.
- the stored reward preview must belong to the same path `:runId`.

<a id="source-11d4e6e3b3c22eef-run-version-snapshot-preservation"></a>
#### Run Version Snapshot Preservation

Accepted run finish stores two separate sanitized evidence snapshots in
`run_final_results.payload_json`:

- `runStartVersionSnapshot`: copied from `run_snapshots.snapshot_json`;
- `runFinishVersionSnapshot`: captured from the accepted finish request's
  client admission and current Worker runtime.

The legacy `runVersionSnapshot` field remains an alias for
`runStartVersionSnapshot`.

This preserves start-A / finish-B / retry-C evidence correctly: the start
snapshot remains the original run start evidence, the finish snapshot records
the deployment/app/content compatibility state that accepted the first finish,
and duplicate finish replay returns the stored finish snapshot without reading
current version policy again.

The stored snapshots are nullable for legacy/local runs. When present, each
contains
only sanitized fields:

- installed app `appVersion`, `versionCode`, `platform`, and `channel`;
- client admission status and minimum version code;
- environment and Cloudflare runtime deployment id;
- approved content `releaseId`, `contentVersion`, and `catalogHash`;
- capture timestamp.

It must not contain raw auth headers, tokens, raw request bodies, Google token
claims, raw Addressables catalog URL, RNG material, probability tables, full
balance weights, animation timeline, VFX/SFX/camera payload, reward amount
internals, or ledger dumps.

<a id="source-11d4e6e3b3c22eef-first-clear-boundary"></a>
#### First Clear Boundary

The local contract slice consumes first clear at accepted finish preview
creation time.

Rules:

- First clear scope is `accountId + operationId + risk`.
- The durable guard is `account_first_clears(operation_key, risk)`.
- Ordinary first clear requires `VICTORY` and `completedWaveCount = 15`.
- Raid first clear requires `VICTORY` and verified boss defeat metadata.
- When the guard has no existing row, accepted finish creates the first-clear
  reward line and inserts `account_first_clears` in the same D1 batch as
  `run_final_results`, `run_reward_previews`, and `idempotency_records`.
- Duplicate replay returns the stored `rewardPreviewId` and does not re-check or
  re-consume first clear.

<a id="source-11d4e6e3b3c22eef-server-source-of-truth"></a>
#### Server Source of Truth

The server owns:

- Run status transition.
- Final run validation.
- Reward calculation.
- Reward preview/claim creation.
- Economy mutation and ledger writes.
- Idempotency records.

The Unity result body is only validation input. It never finalizes rewards directly.

<!-- source-body:source-11d4e6e3b3c22eef:end -->


<a id="source-d4217c0e18e23cfd"></a>
## Run Reward Preview / Claim Contract Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/run-reward-preview-claim.md`  
원문 SHA-256: `6a50516922b2ef53f3f2a6ffe7c18051f07ae77cfec25878503e320aeeac62d4`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-d4217c0e18e23cfd:start -->
<a id="source-d4217c0e18e23cfd-run-reward-preview--claim-contract-draft"></a>
### Run Reward Preview / Claim Contract Draft

This draft defines the reward review and claim flow that follows an accepted
run finish submission.

It extends `docs/api/run-result-submit.md` without changing that contract:
`POST /v1/runs/:runId/finish` returns `rewardPreviewId` first, and does not
return reward amounts or final grants.

Launch-scope risk/reward calculation policy is drafted in
`docs/api/run-risk-reward.md`. This document defines the preview/claim flow;
the risk/reward draft defines how reward lines should be calculated before the
preview is stored.

<a id="source-d4217c0e18e23cfd-ownership-boundary"></a>
#### Ownership Boundary

The server owns reward calculation, reward preview state, claim commitment,
economy mutation, and ledger writes.

Unity may display reward preview data returned by the server, but Unity never
calculates final reward amounts and never sends reward amounts back to the
server as claim input.

<a id="source-d4217c0e18e23cfd-flow"></a>
#### Flow

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

<a id="source-d4217c0e18e23cfd-reward-preview-creation"></a>
#### Reward Preview Creation

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

<a id="source-d4217c0e18e23cfd-preview-state-model"></a>
#### Preview State Model

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

<a id="source-d4217c0e18e23cfd-preview-expiration"></a>
#### Preview Expiration

Initial policy:

- `expiresAt = createdAt + server runtime rewardPreviewTtlMillis`.
- DEV default recommendation: 24 hours.
- Expired previews cannot be claimed.
- Expiration does not delete the preview row.
- A player reopening a finished but unclaimed run after expiration should
  receive `REWARD_PREVIEW_EXPIRED`.

Do not use a cron job to rewrite every expired preview. Treat expiration as a
read/claim-time condition.

<a id="source-d4217c0e18e23cfd-preview-lookup-endpoint"></a>
#### Preview Lookup Endpoint

```text
GET /v1/runs/:runId/reward-preview/:rewardPreviewId
```

Response:

```json
{
  "runId": "run_...",
  "rewardPreviewId": "reward_preview_...",
  "status": "ACTIVE",
  "claimability": "CLAIMABLE",
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
- `claimability` is `CLAIMABLE`, `ALREADY_CLAIMED`, `EXPIRED`, or `INVALIDATED`.
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

<a id="source-d4217c0e18e23cfd-claim-endpoint"></a>
#### Claim Endpoint

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

<a id="source-d4217c0e18e23cfd-reward-claim-creation"></a>
#### Reward Claim Creation

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

<a id="source-d4217c0e18e23cfd-claim-response"></a>
#### Claim Response

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

<a id="source-d4217c0e18e23cfd-unity-dto-needs"></a>
#### Unity DTO Needs

Reward preview lookup DTO:

```text
runId: string
rewardPreviewId: string
status: ACTIVE | CLAIMED | EXPIRED | INVALIDATED
claimability: CLAIMABLE | ALREADY_CLAIMED | EXPIRED | INVALIDATED
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

<a id="source-d4217c0e18e23cfd-migration-impact"></a>
#### Migration Impact

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

<!-- source-body:source-d4217c0e18e23cfd:end -->


<a id="source-77f48c0fb91c5e38"></a>
## Run Risk / Reward Design Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/run-risk-reward.md`  
원문 SHA-256: `fd27bf6bfa62dbfbb220c87277a975be639e68ccde9558134cb2192367456741`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-77f48c0fb91c5e38:start -->
<a id="source-77f48c0fb91c5e38-run-risk--reward-design-draft"></a>
### Run Risk / Reward Design Draft

This draft defines the launch-scope server Source of Truth policy for run
risk, difficulty, and reward preview generation.

It is a design/contract draft only. It does not implement run finish, change
migrations, change API routes, mutate economy data, deploy Workers, create
Cloudflare resources, or change Unity reward calculation boundaries.

Offline rewards are a separate story progression system drafted in
`docs/api/offline-rewards.md`. Run risk does not multiply offline rewards.

<a id="source-77f48c0fb91c5e38-goals"></a>
#### Goals

- Make higher risk and harder operation progression produce higher rewards.
- Keep reward preview generation on the server.
- Keep Unity as display/UX only.
- Make `reward_plan_json` auditable enough for support and operations.
- Prevent low-cleared or abandoned runs from receiving full-clear rewards.
- Keep first-clear rewards separate from repeat-clear multiplier math.

<a id="source-77f48c0fb91c5e38-non-goals"></a>
#### Non-goals

- No implementation in this slice.
- No migration in this slice.
- No remote DEV secret, deploy, or smoke.
- No PRD/STAGING resource creation or deployment.
- No reward/economy mutation change.
- No Unity-side reward amount calculation.

<a id="source-77f48c0fb91c5e38-current-foundation"></a>
#### Current Foundation

Existing schema and contracts already provide the minimum anchors for a
server-side risk/reward calculation.

Risk and run context:

- `operation_previews.risk` stores selected run risk.
- `account_first_clears(operation_key, risk)` can store first clears per
  operation and risk tier.
- `runs.preview_id -> operation_previews.id` links a run back to its selected
  risk and objective preview.
- `runs.balance_config_version` freezes the balance version for the run.
- `run_wave_plans.wave_plan_json` stores the server generated 15-wave plan for
  ordinary operations.
- `raid_raider_king_engine` is a boss-only encounter and uses raid metadata
  instead of a 15-wave plan for completion validation.
- `run_final_results.outcome` stores `VICTORY`, `FAILURE`, or `ABANDONED`.
- `run_final_results.cleared_waves` stores `0..15`; raid boss-only completion
  may store `0` while the server maps verified boss defeat to full completion
  for reward calculation.

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

<a id="source-77f48c0fb91c5e38-ownership-boundary"></a>
#### Ownership Boundary

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

<a id="source-77f48c0fb91c5e38-recommended-formula"></a>
#### Recommended Formula

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

<a id="source-77f48c0fb91c5e38-launch-multiplier-draft"></a>
#### Launch Multiplier Draft

<a id="source-77f48c0fb91c5e38-risk-multipliers"></a>
##### Risk Multipliers

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

<a id="source-77f48c0fb91c5e38-operation-progression-multipliers"></a>
##### Operation Progression Multipliers

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

<a id="source-77f48c0fb91c5e38-outcome--wave-completion"></a>
##### Outcome / Wave Completion

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

<a id="source-77f48c0fb91c5e38-balance-config-reward-schema-candidate"></a>
#### Balance Config Reward Schema Candidate

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

<a id="source-77f48c0fb91c5e38-reward-line-scope"></a>
#### Reward Line Scope

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

<a id="source-77f48c0fb91c5e38-launch-economy-draft-fixture"></a>
#### Launch Economy Draft Fixture

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

Launch first-clear reward config now includes the approved non-repeat command
ticket path:

- `opn_03_water_stop`: `cur_command_ticket x1`.
- `opn_05_night_boarders`: `cur_command_ticket x1`.
- `opn_06_broken_line`: `cur_command_ticket x2` and `cur_special_core x1`
  first clear.
- `raid_raider_king_engine`: `cur_command_ticket x3` and
  `cur_special_core x2` first clear.

Together with `launch_login_bonus cur_command_ticket x5`, the Act 1 launch
path provides 12 non-repeat command tickets. The first-clear portion is emitted
by `calculateRunRewardPreview(...)` as `ITEM` `FIRST_CLEAR` reward lines and is
committed through `account_items`/`item_ledger` when the reward preview is
claimed. The login bonus portion is delivered through
`POST /v1/login-bonus/claim` using the `account_login_bonus_claims` non-repeat
guard and `idempotency_records.operation = 'login_bonus_claim'`.

This is a local launch balance/config contract. Remote production seed/config
deployment still requires separate explicit approval and compatibility smoke
with Unity/Admin no-calculation boundaries.

<a id="source-77f48c0fb91c5e38-run-finish-reward-preview-generation-contract"></a>
#### Run Finish Reward Preview Generation Contract

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

<a id="source-77f48c0fb91c5e38-first-clear-policy"></a>
#### First Clear Policy

First clear is scoped by:

```text
accountId + operationId + risk
```

Use `account_first_clears(operation_key, risk)` as the durable guard.

Rules:

- Ordinary first clear requires `VICTORY` and `clearedWaves = 15`.
- Raid first clear requires `VICTORY` and verified boss defeat metadata.
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

<a id="source-77f48c0fb91c5e38-integrity-restriction-policy"></a>
#### Integrity Restriction Policy

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

<a id="source-77f48c0fb91c5e38-unity-impact"></a>
#### Unity Impact

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

<a id="source-77f48c0fb91c5e38-admin--operations-impact"></a>
#### Admin / Operations Impact

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

<a id="source-77f48c0fb91c5e38-contract--smoke-test-candidates"></a>
#### Contract / Smoke Test Candidates

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

<a id="source-77f48c0fb91c5e38-implementation-candidate-order"></a>
#### Implementation Candidate Order

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

Completed fifth local-only slice:

- Added Act 1 story raid failure stamina refund contract metadata.
- `raid_raider_king_engine` failure with `encounterType = RAID_BOSS`,
  `outcome = FAILURE`, and `raidResult.bossDefeated = false` returns
  a server-authored `staminaRefund` marker.
- The marker uses stable display/support refs:
  `refundType = STAMINA`, `reason = STORY_RAID_FAILURE`, and
  `policyRef = act1_story_raid_failure_stamina_refund_v1`.
- The marker is stored in `run_final_results.payload_json.staminaRefund` and
  mirrored into `run_reward_previews.reward_plan_json.calculation.staminaRefund`
  as sanitized metadata.
- The follow-up launch slice commits the marker as
  `staminaRefund.status = COMMITTED`, inserts
  `account_mutations.mutation_type = STORY_RAID_STAMINA_REFUND`, writes one
  `fuel_ledger` row with reason `STORY_RAID_FAILURE_REFUND`, and updates
  `fuel_state` capped by `max_fuel`.
- Duplicate replay restores the stored committed marker and does not
  recalculate or apply stamina twice.
- The refund remains separate from reward lines, reward claim mutation, and
  World Raid scope.

Next implementation candidates:

1. Decide whether the DEV balance seed should include `runRewards` before run
   finish integration.
2. Add Admin read-only reward preview summary later.
3. Add Unity display metadata only after response contract is approved.
4. Add full validator integration for runtime export integrity.

<a id="source-77f48c0fb91c5e38-open-decisions"></a>
#### Open Decisions

- Exact base reward quantities per operation.
- Remote production deployment approval for the local launch balance/config
  contract and login bonus claim route.
- Whether `cur_command_ticket_fragment` is kept out of launch rewards entirely
  or reserved for weekly/event/pass rewards.
- Whether `SPECIAL_CORE` is repeatable at Risk IV/V or first-clear only.
- Whether abandoned runs give zero reward or a capped checkpoint reward.
- Whether item reward lines are needed before item-ledger claim support lands.
- Exact Admin metric names and thresholds for risk/reward imbalance.
- Whether story raid failure later needs phase-specific reward scaling beyond
  the launch stamina refund.

<!-- source-body:source-77f48c0fb91c5e38:end -->


<a id="source-ed0862e306d76eb4"></a>
## Run Start Augment Package Contract

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/run-start-augment-package.md`  
원문 SHA-256: `fcce406132ccfb018444ce8cf70bd75844ccadf1188419c1b5ca999d4f275128`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-ed0862e306d76eb4:start -->
<a id="source-ed0862e306d76eb4-run-start-augment-package-contract"></a>
### Run Start Augment Package Contract

Current S11 paid-start availability is gated by
[run-combat-requirements.md](49_SERVER_API_CONTRACTS.md#source-7e409c4a47efc1a6). Missing battle bindings
return409 combat_not_ready before debit/consume/run creation. Prior successful
receipts remain replayable; metadata/start persistence alone does not prove actual
Unity battle entry.

This document captures the Act 1 launch contract for augment/tactic selection.

Scope:

- Local contract only.
- No DEV/PRD/STAGING deploy, secret, or remote smoke.
- No reward/economy/story/offline/gacha truth is delegated to Unity.
- No server-owned animation frame, VFX, SFX, camera, hit-stop, or timeline
  payload.
- Rail Choice Events remain backlog.

<a id="source-ed0862e306d76eb4-decision"></a>
#### Decision

Act 1 launch uses a start-of-run full package instead of mid-wave choice
checkpoints.

Ordinary Act 1 operations remain `ORDINARY_15_WAVE` encounters, but the player
enters wave 1 with the authored launch package already available. The old
3/6/9/12/15 checkpoint-style augment/tactic model stays as schema/protocol
backlog and is not a launch gameplay contract.

Raid boss encounters use the same package model. `raid_raider_king_engine`
starts directly at the boss fight with the ordinary full build package refs.

The local launch Game API run start route is:

```text
POST /v1/runs/start
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

The request body contains only the server-created operation preview reference:

```json
{
  "operationPreviewId": "preview_..."
}
```

The route consumes an ACTIVE `operation_previews` row for the authenticated
account, creates an ACTIVE `runs` row, stores a sanitized `run_snapshots`
metadata payload, and returns server-authored start metadata for Unity.
`Idempotency-Key` is required for launch and is scoped by authenticated account
plus operation `run_start`. A retry with the same key and same
`operationPreviewId` returns the stored `STARTED` response shape without
creating another run. Reusing the same key with a different
`operationPreviewId` returns `contract_violation`.

Consumption and run/snapshot/idempotency persistence now share one guarded D1
transaction. An already consumed preview without matching replay is rejected;
commit failure rolls back consumption without a separate reopen. Legacy commit
failure returns `409 run_start_state_conflict` and writes a scrubbed observational
ops log. New S11 previews also validate owned formation/installation and debit
quoted fuel atomically; see [departure-admission.md](49_SERVER_API_CONTRACTS.md#source-8a83ec9bc4961811) for
the producer, admission DTO, errors and explicit legacy cost boundary. The response
must not include raw auth headers, tokens, raw request
body, RNG/probability data, internal weights, or animation timeline fields.

When `CLIENT_ADMISSION_MODE=ENFORCED`, accepted run start also stores a
nullable `runVersionSnapshot` in `run_snapshots.snapshot_json` and returns the
same object in the start response. This captures the server-accepted app/content
compatibility state at the logical run start, not whatever policy is current
when the result is later submitted.

```json
{
  "runVersionSnapshot": {
    "source": "server_accepted_run_start",
    "clientInstalled": {
      "appVersion": "1.0.0",
      "versionCode": "100",
      "platform": "android",
      "channel": "internal"
    },
    "clientAdmission": {
      "status": "ACCEPTED",
      "minimumVersionCode": "90",
      "updateRequired": false
    },
    "serverAcceptedDeployment": {
      "environment": "DEV",
      "runtimeDeploymentId": "cf-runtime-deployment-id"
    },
    "contentRelease": {
      "status": "APPROVED_COMPATIBLE",
      "releaseId": "content_act1_001",
      "contentVersion": "act1.2026-09-06.001",
      "catalogHash": "sha256:..."
    },
    "capturedAt": "2026-09-06T00:00:00.000Z"
  }
}
```

Legacy/local runs or disabled admission return/store `runVersionSnapshot: null`.
Idempotency replay returns the originally stored snapshot; it does not read the
current version policy again. The snapshot is diagnostic/support evidence only
and must not drive reward, story progress, offline reward, gacha, or economy
truth.

Run finish stores a separate `runFinishVersionSnapshot` with source
`server_accepted_run_finish`. The old `runVersionSnapshot` remains the start
snapshot alias for compatibility.

<a id="source-ed0862e306d76eb4-server-owned-metadata"></a>
#### Server-Owned Metadata

The server-authored run/operation preview metadata may include:

```json
{
  "encounterType": "ORDINARY_15_WAVE",
  "startAugmentPackageRef": "act1_full_start_package_v1",
  "startTacticPackageRef": "act1_full_start_package_v1",
  "allowedOperatorSlots": 5,
  "contentVersion": "act1.launch.content.v1"
}
```

Raid preview metadata may use the same refs:

```json
{
  "encounterType": "RAID_BOSS",
  "raidId": "raid_raider_king_engine",
  "bossId": "boss_raider_king_engine",
  "bossPhaseRefs": ["crown_chase", "trophy_break", "engine_stop"],
  "startAugmentPackageRef": "act1_full_start_package_v1",
  "startTacticPackageRef": "act1_full_start_package_v1",
  "allowedOperatorSlots": 5,
  "contentVersion": "act1.launch.content.v1"
}
```

`startAugmentPackageRef` and `startTacticPackageRef` are server-authored refs.
Unity may display/select concrete ids from the referenced authored package, but
client-supplied refs do not become economy, reward, unlock, story progress, or
offline reward Source of Truth.

Admin/support read models may expose these server-authored refs for
read-only investigation. `raidId`, `bossId`, and `bossPhaseRefs` remain
raid-only metadata; ordinary 15-wave previews must not inherit raid refs from
malformed preview JSON.

Rejected run start responses keep the same envelope with `accepted=false`,
`startStatus=REJECTED`, null refs, and a stable `errorCode`.

Current rejected codes include:

- `auth_required`
- `contract_violation`
- `operation_preview_not_found`
- `operation_preview_not_startable`
- `operation_preview_already_consumed`
- `run_start_state_conflict`

<a id="source-ed0862e306d76eb4-run-finish-selection-echo"></a>
#### Run Finish Selection Echo

Run finish may include selected refs in `tacticSelections` as a validation and
diagnostic echo:

```json
{
  "tacticSelections": [
    {
      "source": "START_PACKAGE",
      "packageRef": "act1_full_start_package_v1",
      "selectedAugmentId": "augment_rail_bulwark",
      "selectedTacticId": "tactic_cover_fire"
    }
  ]
}
```

Server responsibilities:

- verify any submitted `packageRef` matches the server-authored
  `startAugmentPackageRef` or `startTacticPackageRef`;
- preserve the final payload for support/integrity context;
- treat selected refs as legality/diagnostic input only;
- continue calculating reward, first clear, story progress, offline rewards,
  gacha, and economy from server-owned records.

Unity responsibilities:

- present authored choices;
- store local selection state for UX;
- submit selected refs as an echo when required;
- never calculate reward/economy/story/offline/gacha truth from selected refs.

<a id="source-ed0862e306d76eb4-launch-deferred-tables"></a>
#### Launch-Deferred Tables

Existing schema has launch-deferred structures:

- `run_checkpoints(checkpoint_wave IN 3,6,9,12,15)`;
- `run_augment_history(wave_index BETWEEN 1 AND 15)`;
- `run_tactical_ledger`.

These remain compatible storage/backlog surfaces, but launch ordinary Act 1
does not require mid-wave checkpoint selection. If future versions restore
mid-run drafting, add a new contract slice and compatibility tests instead of
silently reusing launch fields.

<a id="source-ed0862e306d76eb4-forbidden-data"></a>
#### Forbidden Data

Run start/read and finish/submit contracts must not include:

- raw Addressables catalog URL in `runVersionSnapshot` or Admin run evidence;
- raw RNG material or seed preimage;
- probability values or internal balance weights;
- reward amount, currency grant, item grant, economy grant, or ledger ids
  supplied by Unity;
- raw animation timeline, frame data, VFX/SFX/camera/hit-stop tracks;
- unrestricted validator internals;
- token, secret, raw auth header, or account shortcut identifiers.

<a id="source-ed0862e306d76eb4-tests"></a>
#### Tests

Launch contract tests should cover:

- ordinary 15-wave run accepts empty `tacticSelections` for backwards
  compatibility;
- selected package refs must match server-authored start package refs;
- nested raw RNG/reward/economy fields inside `tacticSelections` are rejected
  before DB reads;
- raid boss-only runs can use the same start package refs;
- duplicate replay returns the stored accepted result and does not re-validate
  or mutate selections.

<a id="source-ed0862e306d76eb4-decision-queue"></a>
#### Decision Queue

- Final `startAugmentPackageRef` and `startTacticPackageRef` stable ids.
- Whether selected ids need a server-side authored package catalog before
  launch, or whether package-ref legality is enough for the first Unity QA pass.
- Future mid-wave drafting restart criteria. Backlog, not launch.

<!-- source-body:source-ed0862e306d76eb4:end -->


<a id="source-75e533c458b7b09f"></a>
## Read-only run-start result lookup

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/run-start-result.md`  
원문 SHA-256: `4cd142a1d98a9dadfb40bee8f9d0b359c86d1708cf2aa0f518f8a24a77a31415`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-75e533c458b7b09f:start -->
<a id="source-75e533c458b7b09f-read-only-run-start-result-lookup"></a>
### Read-only run-start result lookup

Use this when the journal has the original start key and request body, but the
start response was lost before receiving runId. This endpoint queries an existing
receipt; it never calls start, consumes a preview, issues rewards/refunds or changes
run state. No migration. Public combat readiness restrictions remain unchanged.

<a id="source-75e533c458b7b09f-request"></a>
#### Request

```http
POST /v1/runs/start-result
Authorization: Bearer <current-session-token>
Content-Type: application/json
```

```json
{
  "startIdempotencyKey": "original-start-key-from-journal",
  "operationPreviewId": "original-preview-id-from-journal"
}
```

Both fields are required and the body is strict. Key:1–128 printable non-space
ASCII characters, unchanged from the original start. Preview ID uses existing
`[A-Za-z0-9_-]{1,100}`. Do not trim/recreate the key, replace previewId, or generate
a new start key. This is a POST solely to transport the lookup material in a bounded
body, not a mutation. No Idempotency-Key header is required for the lookup itself;
do not place the original key in a URL, query parameter or lookup header.
All queries are rejected. No response echoes the key, body or conflicting preview.
The route does not log receipt-query exceptions or SQL bind values. Client journals
and HTTP instrumentation must also exclude the body/key from diagnostics. Existing
Bearer authentication and client admission apply. Responses are `no-store`.

<a id="source-75e533c458b7b09f-fixed-response"></a>
#### Fixed response

```ts
type StartResult = {
  accepted: boolean; // lookup completed, NOT permission to start
  lookupStatus: "COMMITTED" | "NOT_FOUND" | "SNAPSHOT_UNAVAILABLE"
    | "CONFLICT" | "TEMPORARILY_UNAVAILABLE" | "INVALID_REQUEST";
  runId: string | null;
  runStatus: string | null;
  startResponse: Record<string, unknown> | null;
  errorCode: null | "start_preview_mismatch" | "start_lookup_unavailable"
    | "contract_violation";
};
```

| HTTP / lookupStatus | Meaning | Journal handling |
| --- | --- | --- |
| 200 COMMITTED | Matching account/key/preview, recoverable original response | Record the returned original runId/response and continue original-run recovery; never issue another start |
| 200 NOT_FOUND | No matching receipt visible in this read | Preserve original key/body. It may still be processing or visibility may lag. Requery this read-only endpoint; this is not definitive failure or authorization for a new key/start |
| 200 SNAPSHOT_UNAVAILABLE | Receipt exists, but original response cannot be restored or verified | Preserve journal. runId/status may be available when owned run exists; null if unresolved. Do not infer a refund, failed start or permission to create another run |
| 409 CONFLICT | The authenticated account's key points to a different preview | All result fields null, errorCode=start_preview_mismatch. Preserve original request; do not silently replace its preview/key. Actual conflicting preview is not disclosed |
| 503 TEMPORARILY_UNAVAILABLE | Receipt lookup/restore failed transiently or internally | All result fields null, errorCode=start_lookup_unavailable. Retry the same lookup; no conclusion about commit |
| 400 INVALID_REQUEST | Invalid/extra fields or query parameters | All result fields null, errorCode=contract_violation. Correct transport using unchanged original journal values |

Existing auth/client-admission failures retain their normal error envelope/status
(including401 for missing/expired/revoked sessions). Any other5xx or transport loss
is also indeterminate. No retry of the **start command** is required for lookup.
An approved combat loader is still required to use a recovered run; COMMITTED and
accepted:true do not mean combatReady.

Example after response loss:

```json
{
  "accepted": true,
  "lookupStatus": "COMMITTED",
  "runId": "run_existing",
  "runStatus": "ACTIVE",
  "startResponse": {
    "accepted": true,
    "startStatus": "STARTED",
    "runId": "run_existing",
    "operationPreviewId": "preview_existing"
  },
  "errorCode": null
}
```

The example abbreviates the original startResponse. Actual S11 stored
departureResponse is returned unchanged, with its original fields/versions/time;
no current first-clear, price, seed, stats or readiness defaults are added. Legacy
snapshots use the existing replay projection. Restored runId and previewId must
match the account-scoped receipt/run before COMMITTED is returned. Missing or
malformed snapshots are not treated as absence of a receipt.

<a id="source-75e533c458b7b09f-isolation-and-atomic-visibility"></a>
#### Isolation and atomic visibility

A single SELECT reads `idempotency_records` with operation=`run_start`, authenticated
account and exact key, joined to a run with the same account and its snapshot.
Another account's key is indistinguishable from NOT_FOUND. A corrupt receipt that
references a foreign run cannot expose that run's ID/status/response. Lookup never
searches another account, and never creates a replacement receipt or run.

Start's existing atomic transaction controls publication. A lookup concurrent with
start may return NOT_FOUND before commit, then COMMITTED afterward. The lookup
does not take ownership of the in-flight operation or claim it definitively failed.
Preserve the journal until the original result is resolved; retries may recover
already completed/failed runs with their current status and original start response.

<a id="source-75e533c458b7b09f-verification"></a>
#### Verification

Six signed-auth real SQLite tests cover response loss without client runId,
byte-identical database exports before/after lookup, account isolation/401/revoked
session, preview mismatch with no disclosure, not-found/concurrent commit, missing
or corrupt snapshots, URL/extra-field rejection and sanitized503 with no query log.
Historical committed starts are prepared through the retained domain-core test
fixture; no public ready bypass or new acquisition is enabled. Full-suite evidence
and exact same-code/document commit are in the owner handoff.

<!-- source-body:source-75e533c458b7b09f:end -->


<a id="source-253ca40b108e58e8"></a>
## Server version in error logs

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/server-version-log-policy.md`  
원문 SHA-256: `abab6f9071dc335a601fe710032ee073d8d90a90a76bfd99efce229c98bcb345`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-253ca40b108e58e8:start -->
<a id="source-253ca40b108e58e8-server-version-in-error-logs"></a>
### Server version in error logs

`serverVersion` in launch release planning identifies the LAST RAIL server
release version, for example `2026.09.06.1`. It is not a Unity app version,
minimum app version, balance version, content version, or Admin UI build.

For Cloudflare Workers, `CF_VERSION_METADATA.id` remains the actual runtime
deployment identity authored by Cloudflare at deploy time. The release evidence
record maps the LAST RAIL server version to the Worker deployment id. A code
rollback creates another deployment id; old logs retain the identity they were
written with.

The log writer supplies the runtime ID explicitly to the shared envelope builder.
Request headers, bodies and caller-provided context must not override it.
Store only scalar deployment identity in sanitized context; expose it to Admin as
read-only evidence. Do not store release metadata, branch names, deploy tags, or
runtime ids in a way that implies DB backup or DB rollback capability.
No DB migration or backfill is needed. Legacy/missing metadata is null (UI: 미기록).
Exact version filtering occurs before pagination with bound SQL parameters.

DEV Game API currently owns the active D1 log writer and receives the metadata binding.
Other producers must adopt the same binding when their log collection is connected.
PRD configuration changes require the existing release approval process.
This policy is diagnostic only and does not enforce API/client compatibility.
Compatibility enforcement remains the `minimumVersionCode`/client admission
policy.

<!-- source-body:source-253ca40b108e58e8:end -->


<a id="source-5277a087d9d6d375"></a>
## S06 shop-v1

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/shop.md`  
원문 SHA-256: `71637d932a62f93b38d3404c1983d25efcd64f02d6973a73f28fc546e4d7ba08`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-5277a087d9d6d375:start -->
<a id="source-5277a087d9d6d375-s06-shop-v1"></a>
### S06 shop-v1

Local code only, requires migrations0001–0034 and a valid active crystal policy. No remote migration, seed, Store connection or deployment performed. Authority: canonical docs/ops/launch-economy-v1.md, act1.economy.2026-09-09.001: command ticket normal100crystals, daily50 once per account/server day, FREE-first. No credit recruitment exchange.

GET `/v1/shop` authenticates the account, uses no body/query, returns Cache-Control:no-store and performs no mutations. Response: accepted,version:shop-v1,serverTime,dayStart,nextResetAt,policyVersion,wallet,ticketOffers,ticketUnavailableReason,crystalProducts,capabilities. Wallet contains separate FREE/PAID balances and spend order. NORMAL offer costs100 and retains configured maxTicketsPerRequest (not a daily limit). DAILY offer costs50 for one ticket, fixed offerFamily COMMAND_TICKET_DAILY,dailyLimit1,dailyRemaining0or1. Offers are exposed only when active ticketExchange uses actual cur_command_ticket and approved100 price. Otherwise list is empty with shop_launch_price_unconfigured; missing/invalid wallet policy returns503.

POST `/v1/shop/tickets/exchange` requires auth, printable ASCII Idempotency-Key1–128 and strict JSON `{policyVersion,offer:"NORMAL"|"DAILY",ticketCount,expectedDayStart}`. Use the policyVersion/dayStart from GET. DAILY requires ticketCount1 and the current KST04 service day; stale day409shop_day_changed. Same original key/body replays its receipt after reset without buying again; changed body409idempotency_conflict. New key after daily use409shop_daily_limit_reached. No unused-day carryover. Normal ticket count must fit configured per-request maximum.

Atomic transaction spends real FREE lots before PAID, updates actual cur_command_ticket inventory/item ledger, records crystal/reward receipts and daily use. Daily uniqueness is account/fixed family/day, independent of policy version; changing active policy cannot reset the quota. DB commit clock rejects a request crossing04KST and rolls back money/ticket/quota together. Response extends existing crystal operation receipt with ticketsGranted,ticketItemId,ticketBalanceAfter,offer,cost,dayStart,dailyRemaining (null for normal). Other failures:400invalid body/count;409insufficient funds or changed policy/conflict;503missing approved price. Reads are advisory snapshots; mutations revalidate all conditions.

crystalProducts contains only configured trusted productId/quantity/provider/appId. Cash price is null with priceSource STORE_REQUIRED; actual localized price must come from Store. purchaseAvailable requires the existing CRYSTAL_PURCHASE_VERIFIER capability; this implementation does not install an adapter or invent SKUs. Existing `/v1/wallet/crystals/purchases/verify` remains the verification route. Pass catalog points to `/v1/recurring/state`, governed by its own bindings/intent contract. Material shop is UNCONFIGURED because no approved actual product/price/limit table was provided. Existing generic `/v1/wallet/crystals/tickets/exchange` remains its versioned policy contract for compatibility; S06 uses the new approved-offer route.

Migration0034 adds shop_daily_ticket_claims and a transactional day guard, with no seeds. Tests cover read-only catalog, missing verifier/Store price, exact50/100 actual wallet spend, concurrent requests, policy rollover, original-key replay across reset, no carryover, DB boundary rollback and non100 configuration rejection. Full-suite counts/commit identity are reported in server handoff after final verification.

<!-- source-body:source-5277a087d9d6d375:end -->


<a id="source-378a824352e36d1b"></a>
## Story Progress Source of Truth / Read Model Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/story-progress.md`  
원문 SHA-256: `49b5e181e00cf9b07858aee47b76feec1c9289728fe22a7af59eb12dccb73532`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-378a824352e36d1b:start -->
<a id="source-378a824352e36d1b-story-progress-source-of-truth--read-model-draft"></a>
### Story Progress Source of Truth / Read Model Draft

This draft defines the server-side story progress Source of Truth and read
model candidates for LAST RAIL. It is a design/contract draft only. It does not
add migrations, API routes, runtime implementation, D1 seed data, Cloudflare
resources, Unity DTOs, or Admin integration.

<a id="source-378a824352e36d1b-goals"></a>
#### Goals

- Keep durable story progress and unlock truth on the server.
- Keep Unity responsible for narrative presentation and localized text
  playback.
- Keep Admin visibility read-only first.
- Make accepted run finish the future durable story progress update moment.
- Make story progress updates idempotent and replay-safe.
- Provide a server-owned story progress input for offline rewards.

<a id="source-378a824352e36d1b-non-goals"></a>
#### Non-Goals

- No full localized dialogue text storage in Game API.
- No combat callout timing or cutscene playback control.
- No briefing/result UI layout contract.
- No long-form codex prose payloads.
- No economy, auth, or combat contract dependency on narrative text.
- No migration or implementation in this slice.

<a id="source-378a824352e36d1b-ownership-boundary"></a>
#### Ownership Boundary

Server owns:

- account story progress;
- operation clear state;
- act and operation unlock truth;
- durable narrative milestone flags;
- first-clear state through existing `account_first_clears`;
- future accepted run finish story progress updates;
- optional narrative version metadata.

Unity owns:

- localized dialogue text playback;
- cutscene, callout, briefing, result, and codex presentation;
- local UI labels for server stable ids;
- client-side narrative sequencing that does not grant durable progress.

Admin owns:

- read-only operational visibility first;
- future support/debug views for story progress, unlocks, and narrative flags.

Unity request bodies must not become story progress Source of Truth. If a
client sends story progress, unlock state, narrative flags, or offline progress
keys to a write route, the server should reject the request with
`contract_violation` unless that route has an explicit validation-only field.

<a id="source-378a824352e36d1b-existing-server-foundations"></a>
#### Existing Server Foundations

Current migrations already provide related building blocks:

- `accounts(id)` is the account Source of Truth.
- `account_unlocks(account_id, unlock_id)` can store unlocked operations,
  features, train slots, or narrative capabilities.
- `progression_ledger` can record durable progression deltas.
- `account_first_clears(account_id, operation_key, risk)` stores first clear
  state by operation and risk tier.
- `operation_previews.objective` and `preview_json.operationId` identify the
  operation selected for a run.
- `runs.preview_id` links a run to the frozen operation preview.
- `run_final_results` stores accepted final run outcome and cleared waves.
- `idempotency_records` stores successful committed operation replay keys.

Gap:

- There is no dedicated story progress read model yet.
- `account_first_clears` is risk-scoped and should stay reward/first-clear
  focused, not become the account story progress table.
- `account_unlocks` can express individual unlocks, but it does not by itself
  provide a compact ordered Act 1 progress summary.

<a id="source-378a824352e36d1b-act-1-progression-order"></a>
#### Act 1 Progression Order

Use stable ids as the ordered story progress keys:

| Order | Story progress id | Meaning |
| ---: | --- | --- |
| 0 | `none` | No Act 1 operation clear yet |
| 1 | `opn_01_last_departure` | Last departure completed |
| 2 | `opn_02_first_signal` | First signal restored |
| 3 | `opn_03_water_stop` | Water stop secured |
| 4 | `opn_04_scrapstar_show` | Scrapstar market/show resolved |
| 5 | `opn_05_night_boarders` | Night boarders repelled |
| 6 | `opn_06_broken_line` | Broken line restored |
| 7 | `raid_raider_king_engine` | Act 1 raid milestone cleared |

The raid milestone can be a story progress key, but the exact unlock/clear
condition remains a future raid system decision.

<a id="source-378a824352e36d1b-read-model"></a>
#### Read Model

Migration `0014_story_progress.sql` adds the dedicated read model:

```sql
CREATE TABLE account_story_progress (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  act_id TEXT NOT NULL,
  highest_cleared_operation_id TEXT NOT NULL,
  highest_story_progress_id TEXT NOT NULL,
  cleared_operations_json TEXT NOT NULL,
  story_flags_json TEXT NOT NULL,
  narrative_version TEXT,
  last_progress_run_id TEXT,
  updated_at INTEGER NOT NULL
);

CREATE INDEX idx_account_story_progress_act
  ON account_story_progress(act_id, highest_story_progress_id);
```

Candidate field semantics:

- `account_id`: authenticated account.
- `act_id`: currently `act_01_wasteland_railway` for launch.
- `highest_cleared_operation_id`: highest ordinary operation clear, excluding
  raid unless raid is modeled as an operation.
- `highest_story_progress_id`: ordered story progress key, including raid
  milestone if reached.
- `cleared_operations_json`: stable set/list of cleared operation ids.
- `story_flags_json`: durable milestone flags, such as tutorial narration seen,
  major reveal acknowledged, or feature narration unlocked.
- `narrative_version`: optional content/narrative metadata version.
- `last_progress_run_id`: last accepted finish that advanced progress.
- `updated_at`: server timestamp.

Current recommendation:

- Use this dedicated read model instead of overloading `account_runtime_state`.
- Keep `account_unlocks` for granular unlock truth.
- Keep `progression_ledger` for immutable progression deltas.
- Keep `account_first_clears` risk-scoped for first-clear rewards.

<a id="source-378a824352e36d1b-unlock-model"></a>
#### Unlock Model

Use `account_unlocks` for granular unlocks:

```text
operation:opn_02_first_signal
operation:opn_03_water_stop
train_slot:3
train_car:car_cargo
feature:raid
narrative_flag:first_signal_restored
```

Unlock ids should be stable strings and should not store localized display text.

Future run finish story progress update can insert:

- operation unlocks for the next operation;
- train slot/car unlocks if the operation grants them;
- narrative flags for durable milestones;
- progression ledger rows that describe the server-side delta.

<a id="source-378a824352e36d1b-game-api-read-endpoint"></a>
#### Game API Read Endpoint

Current local Game API read endpoint:

```text
GET /v1/story-progress
Authorization: Bearer <access-token>
```

Rules:

- Player Bearer auth is required.
- The account id is resolved only from the authenticated session context.
- Request body is not accepted.
- `accountId` / `account_id` query parameters are rejected with
  `contract_violation`.
- Missing `account_story_progress` rows return an accepted default `none`
  progress response for Act 1.
- Existing rows are read from `account_story_progress`, with operation unlocks
  derived from `account_unlocks`.
- Full localized dialogue text, tokens, secrets, raw request bodies, raw RNG
  material, reward calculation internals, and account shortcut data are never
  returned.
- `narrativeVersion` is read-only diagnostic metadata. It does not drive reward
  eligibility, first clear eligibility, unlock truth, or offline reward
  multipliers.

Empty account response:

```json
{
  "accepted": true,
  "actId": "act_01_wasteland_railway",
  "operationId": null,
  "storyProgress": {
    "highestStoryProgressId": "none",
    "highestClearedOperationId": null,
    "order": 0
  },
  "firstClear": null,
  "clearedOperationIds": [],
  "narrativeFlags": [],
  "storyFlags": [],
  "unlockedOperations": [
    "opn_01_last_departure"
  ],
  "narrativeVersion": "act1.2026-08-27.001",
  "narrative": {
    "narrativeVersion": "act1.2026-08-27.001",
    "contentStatus": "CANDIDATE",
    "localizationStatus": "PENDING",
    "fixtureStatus": "PARTIAL",
    "missingContentStatus": "UNKNOWN"
  },
  "lastProgressRunId": null,
  "updatedAt": null,
  "errorCode": null,
  "message": null
}
```

Stored progress response:

```json
{
  "accepted": true,
  "actId": "act_01_wasteland_railway",
  "operationId": "opn_03_water_stop",
  "storyProgress": {
    "highestStoryProgressId": "opn_03_water_stop",
    "highestClearedOperationId": "opn_03_water_stop",
    "order": 3
  },
  "firstClear": null,
  "clearedOperationIds": [
    "opn_01_last_departure",
    "opn_02_first_signal",
    "opn_03_water_stop"
  ],
  "narrativeFlags": [
    "operation_cleared:opn_03_water_stop"
  ],
  "storyFlags": [
    "operation_cleared:opn_03_water_stop"
  ],
  "unlockedOperations": [
    "opn_01_last_departure",
    "opn_04_scrapstar_show"
  ],
  "narrativeVersion": "act1.2026-08-27.001",
  "narrative": {
    "narrativeVersion": "act1.2026-08-27.001",
    "contentStatus": "CANDIDATE",
    "localizationStatus": "PENDING",
    "fixtureStatus": "PARTIAL",
    "missingContentStatus": "UNKNOWN"
  },
  "lastProgressRunId": "run_01...",
  "updatedAt": "2026-08-27T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

Notes:

- `firstClear` is optional detail and remains risk-scoped.
- `storyProgress` is not risk-scoped.
- `unlockedOperations` is server-derived from `account_unlocks`, with
  `opn_01_last_departure` kept as the default Act 1 operation.
- The current endpoint returns `firstClear: null`; richer risk-scoped first
  clear detail remains a future read model candidate.

<a id="source-378a824352e36d1b-accepted-run-finish-update"></a>
#### Accepted Run Finish Update

The current local contract slice updates story progress during accepted
`POST /v1/runs/:runId/finish`.

Rules:

- Only accepted, validated, authenticated run finish can update story progress.
- Story progress update uses the server-frozen run context:
  - authenticated `accountId`;
  - `runs.preview_id`;
  - `operation_previews.objective`;
  - `operation_previews.preview_json.operationId`;
  - `run_final_results.outcome`;
  - `run_final_results.cleared_waves`.
- Ordinary `VICTORY` with `clearedWaves = 15` advances operation story
  progress.
- `raid_raider_king_engine` advances story progress after `VICTORY` with
  verified boss defeat metadata. It does not require 15 cleared waves because
  the raid is a boss-only encounter.
- `FAILURE` and `ABANDONED` do not advance ordinary story progress.
- Duplicate run finish replay must not recalculate or reapply story progress.
- Lower operation clears must not regress higher stored progress.
- Higher operation clears can backfill `cleared_operations_json` policy only if
  a future design explicitly allows skip progression. Launch recommendation is
  linear Act 1 progression only.

Current D1 batch for a progress-advancing finish:

1. Insert `run_final_results`.
2. Update `runs`.
3. Insert `run_reward_previews`.
4. Insert `account_first_clears` when eligible.
5. Upsert `account_story_progress` only if the new operation order is greater
   than the stored order.
6. Insert `account_unlocks` for newly unlocked operation/feature ids.
7. Insert `progression_ledger` rows for story progress/unlock deltas.
8. Insert `idempotency_records.operation = 'run_finish'`.

This must be one atomic committed operation for launch. If the story progress
write fails, the accepted finish should fail or roll back rather than create a
reward preview with missing story progress side effects.

Implemented local slice:

- Ordinary `VICTORY` with `clearedWaves = 15` upserts
  `account_story_progress` when the operation order is greater than the stored
  `highest_story_progress_id`.
- Raid boss `VICTORY` with verified boss defeat metadata uses the same
  idempotent story progress upsert path.
- New progress writes a `STORY_PROGRESS` `account_mutations` row and
  `progression_ledger` row.
- New progress inserts the next operation unlock with `INSERT OR IGNORE` into
  `account_unlocks`.
- Lower operation finishes do not allocate a story mutation and do not regress
  stored progress.
- Duplicate run finish replay returns the stored result and does not re-run
  story progress logic.
- Client-supplied story progress/unlock/narrative flag fields are rejected at
  the run finish parser boundary.

<a id="source-378a824352e36d1b-duplicate-replay-policy"></a>
#### Duplicate Replay Policy

Run finish idempotency currently replays by:

```text
accountId + operation = 'run_finish' + Idempotency-Key
```

Story progress must follow the same principle:

- replay returns the stored accepted response;
- no second `run_final_results`;
- no second reward preview;
- no second story progress update;
- no duplicate narrative flags;
- no duplicate unlock rows;
- no progress regression.

If `Idempotency-Key` was used for another run, the server should reject with
`contract_violation` and must not return another run's `rewardPreviewId` or
story progress result.

<a id="source-378a824352e36d1b-offline-reward-input"></a>
#### Offline Reward Input

Offline rewards should use server-stored story progress:

```text
account_story_progress.highest_story_progress_id
```

Do not use:

- Unity local story progress;
- Unity request body story progress;
- run risk;
- first-clear risk tier.

If story progress cannot be resolved:

- return `offline_story_progress_missing` for offline reward preview/claim;
- do not infer from client state.

<a id="source-378a824352e36d1b-contract--smoke-test-candidates"></a>
#### Contract / Smoke Test Candidates

Read model:

- New guest account starts at `highest_story_progress_id = 'none'` or receives a
  default read response with `none`.
- `opn_02_first_signal` clear after `opn_01_last_departure` advances
  `highest_story_progress_id` from `opn_01_last_departure` to
  `opn_02_first_signal`.
- `unlockedOperations` includes the next operation after progress advances.

Replay/idempotency:

- Replaying the same accepted finish does not duplicate flags.
- Replaying the same accepted finish does not duplicate unlock rows.
- Replaying the same accepted finish does not advance revision twice.
- Reusing an idempotency key for a different run is `contract_violation`.

Regression prevention:

- Clearing `opn_02_first_signal` after already clearing
  `opn_04_scrapstar_show` does not regress `highest_story_progress_id`.
- Story flags merge as a stable set and do not duplicate.

Client trust boundary:

- Client body `storyProgress` is rejected with `contract_violation`.
- Client body `highestStoryProgressId` is rejected with `contract_violation`.
- Client body `unlockedOperations` is rejected with `contract_violation`.
- Offline reward claim uses server stored story progress, not Unity-supplied
  value.

Offline linkage:

- Same story progress produces same offline multiplier regardless of Risk I or
  Risk V.
- `raid_raider_king_engine` progress produces a larger offline cap/multiplier
  than `opn_06_broken_line`.

<a id="source-378a824352e36d1b-unity-impact"></a>
#### Unity Impact

No current Unity behavior change is required by this design draft.

Future Unity DTO/display candidates:

- `actId`
- `operationId`
- `storyProgress.highestStoryProgressId`
- `storyProgress.order`
- `narrativeFlags`
- `unlockedOperations`
- `narrativeVersion`

Unity must keep:

- localized narrative text and playback client-owned;
- story progress display label only;
- no durable story progress calculation;
- no offline reward multiplier calculation;
- no unlock truth calculation.

<a id="source-378a824352e36d1b-admin--operations-impact"></a>
#### Admin / Operations Impact

Future Admin read-only candidates:

- account story progress summary;
- operation clear state;
- story flags;
- unlocked operation/feature list;
- last progress run id;
- narrative version;
- mismatch alerts when a run completed but story progress did not advance.

Current local Admin API read slice:

```text
GET /v1/admin/narrative/coverage?actId=act_01_wasteland_railway
GET /v1/admin/story-progress
GET /v1/admin/story-progress/:accountId
```

The endpoint pair is read-only and uses Admin principal authentication, not Game
API player bearer auth. It requires stable permission id `STORY_PROGRESS_READ`.
Because the current Admin API permission model is role-string based, OWNER /
OPERATOR / VIEWER all map to this read-only permission. Detail reads include
operation unlocks from `account_unlocks`; list reads stay compact.

`GET /v1/admin/narrative/coverage` is an Act 1 readiness read model for Admin
run detail and support context. It is not story progress truth and does not
drive rewards, unlocks, offline rewards, or first-clear eligibility. The launch
freeze candidate version is `act1.2026-08-30.001`; rows stay in pending states
until a final fixture export and version stamp is accepted after Unity/internal
testing QA.

Current coverage rows use stable mission ids for `opn_01_last_departure`
through `opn_06_broken_line` plus `raid_raider_king_engine`, with Korean display
names only. They expose readiness codes such as `draft_exists`,
`localization_pending`, `fixture_pending`, and `version_pending`. They do not
return full localized dialogue text, author notes, raw unpublished notes, or
editorial payloads.

`GET /v1/admin/dashboard/launch-summary` also includes a story/narrative
aggregate:

- `accountsWithStoryProgressCount`
- `highestProgressDistribution[]`
- `contentMissingCount`
- `localizationPendingCount`
- `fixturePendingCount`
- `versionMismatchCount`
- `latestNarrativeVersion`
- `serverCalculatedAt`

The aggregate combines server-owned `account_story_progress` counts with the
static Act 1 narrative coverage candidate set. Empty semantics are stable:
counts are `0`, distributions are `[]`, and missing latest versions are `null`.

The customer support account summary draft in `docs/api/support.md` can compose
this story progress read model as one read-only section. It must not add story
progress mutation, unlock mutation, narrative editing, or localized dialogue
payloads.

Ops error code candidates:

```text
story_progress_missing
story_progress_policy_missing
story_progress_state_conflict
story_progress_regression_blocked
story_progress_idempotency_mismatch
story_unlock_policy_missing
```

Admin audit remains for operator actions. Runtime story progress failures belong
in ops error logs or integrity/progression events.
The launch minimal ops error write integration records
`story_progress_state_conflict` only when an accepted full-clear run finish
cannot safely commit the server-owned story progress update batch. Duplicate
finish replay, lower-operation non-regression, and client-supplied story
progress rejection do not create ops error rows by default.

<a id="source-378a824352e36d1b-open-decisions"></a>
#### Open Decisions

- Exact unlock ids for train slot/car/feature unlocks.
- Whether raid story progress requires clear, participation, or season claim.
- Whether `narrativeVersion` is global, act-scoped, or operation-scoped.
- Whether Admin needs a separate story progress history table beyond
  `progression_ledger`.
- Whether future full validator integration can add extra story integrity gates
  before the existing run finish story progress update.

<!-- source-body:source-378a824352e36d1b:end -->


<a id="source-b678a199237246c2"></a>
## Customer Support Account Summary Contract Draft

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/support.md`  
원문 SHA-256: `15c5c27c69e65d363c0ea9dbf0cf32eb655c46aab86372a52bc54ead073e94c8`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-b678a199237246c2:start -->
<a id="source-b678a199237246c2-customer-support-account-summary-contract-draft"></a>
### Customer Support Account Summary Contract Draft

This document defines the first read-only customer support account summary
contract for LAST RAIL launch operations.

The read-only Admin API summary route is implemented. This contract does not
create repair endpoints, reward grants, Cloudflare resources, DEV deploys,
remote smoke tests, or PRD/STAGING resources.

<a id="source-b678a199237246c2-goals"></a>
#### Goals

- Give customer support one account-centered investigation view.
- Keep launch support read-only by default.
- Reuse existing server Source of Truth tables and read models.
- Preserve correlation/reference ids for follow-up investigation.
- Avoid exposing exploit-sensitive internals, credentials, raw request data, or
  reward/RNG calculation internals.

<a id="source-b678a199237246c2-non-goals"></a>
#### Non-goals

- No account status mutation.
- No reward repair or manual grant.
- No offline reward timestamp reset.
- No story progress mutation or unlock mutation.
- No gacha repair, pity edit, or economy correction.
- No localized dialogue text payloads.
- No token/session secret inspection.

<a id="source-b678a199237246c2-endpoint"></a>
#### Endpoint

```text
GET /v1/admin/support/accounts/:accountId
```

The endpoint is implemented as an Admin API read-only route. It uses the Admin
principal auth boundary, not Game API player bearer auth.

Request:

- Path `accountId` is the only account selector.
- Request body is not accepted.
- Query filters can be added later for section limits, but launch can start
  with stable defaults.
- Any body field that looks like account status mutation, reward grant, offline
  timestamp reset, story mutation, unlock mutation, gacha repair, economy
  correction, raw auth data, or ledger edit intent is `contract_violation`.

Response:

```ts
type SupportAccountSummary = {
  accountId: string;
  generatedAt: string;
  permission: "SUPPORT_READ";
  permissionMode: "ROLE_PERMISSION";
  account: SupportAccountStatusSection;
  auth: SupportAuthStatusSection;
  recentRuns: SupportRecentRunSummary[];
  runRewards: SupportRunRewardSummary[];
  offlineRewards: SupportOfflineRewardSummary;
  storyProgress: SupportStoryProgressSummary;
  gacha: SupportGachaSummary;
  loginBonus: SupportLoginBonusSummary;
  idempotency: SupportIdempotencySummary[];
  integrity: SupportIntegritySummary;
  opsErrors: SupportOpsErrorSummary[];
  supportNotes: SupportExplanationTemplate[];
  supportContext: {
    readOnly: true;
    mutationEndpointsEnabled: false;
    repairEndpointsEnabled: false;
    source: "support_account_summary";
  };
  errorCode: null;
  message: null;
};
```

Missing account:

```json
{
  "error": "account_not_found",
  "message": "Account was not found."
}
```

<a id="source-b678a199237246c2-player-inquiry-endpoint"></a>
#### Player Inquiry Endpoint

```text
POST /v1/support/tickets
```

Implemented as a Game API player-bearer route. The authenticated session is the
only account selector.

Request:

- Header `Authorization: Bearer <access token>` is required.
- Header `Idempotency-Key` is required, account-scoped, and max 128 chars.
- Query parameters are not accepted.
- Body must be a JSON object.
- `accountId` / `account_id` body shortcuts are `contract_violation`.
- `category` must be one of:
  - `ACCOUNT_LOGIN`
  - `RUN_REWARD`
  - `GACHA_RECRUITMENT`
  - `STORY_PROGRESS`
  - `BUG_OTHER`
- `body` is trimmed server-side and must be 10-2000 chars.
- Optional `appVersion` is trimmed and max 64 chars.
- Optional `appVersionCode` is a decimal string, max 19 chars; omitted or empty
  values are returned as `null`.
- Optional `clientComposedAt` and `clientSubmittedAt` are ISO timestamp strings.

`body` length is evaluated after server trim using the same string-length
semantics used by the route and SQLite guard. Unity should treat this as
10-2000 user-visible characters and keep any local counter conservative.

Response:

```ts
type SupportTicketCreateResponse = {
  accepted: boolean;
  ticketStatus: "RECEIVED" | "DUPLICATE_REPLAY" | "REJECTED";
  ticketId: string | null;
  category: "ACCOUNT_LOGIN" | "RUN_REWARD" | "GACHA_RECRUITMENT" | "STORY_PROGRESS" | "BUG_OTHER" | null;
  bodyPreview: string | null;
  appVersion: string | null;
  appVersionCode: string | null;
  clientComposedAt: string | null;
  clientSubmittedAt: string | null;
  serverReceivedAt: string | null;
  retryAfterSeconds: number | null;
  errorCode: null | "duplicate_replay" | "contract_violation" | "auth_required" | "support_ticket_invalid" | "idempotency_conflict" | "support_ticket_rate_limited";
  message: string | null;
};
```

Duplicate policy:

- Same account + `support_ticket_create` + same `Idempotency-Key` + same request
  hash returns `DUPLICATE_REPLAY` with the original `ticketId`.
- Same account + same key + different request hash returns `409
  idempotency_conflict`.
- Same key on a different authenticated account is independent.
- Duplicate replay is resolved before rate checks so a network retry of an
  already accepted ticket does not create another ticket and is not rejected by
  the rate limit.

Receipt lookup for response-loss recovery:

```text
GET /v1/support/tickets/receipt
Idempotency-Key: <client-generated-key>
Authorization: Bearer <accessToken>
```

The receipt route accepts no body and no query parameters. It looks up only the
authenticated account and returns either:

- `receiptStatus: FOUND` with `ticketId`, category, body preview, app version
  fields, and server timestamp;
- `receiptStatus: NOT_FOUND` with `support_ticket_receipt_not_found`.

It performs no writes and exposes no raw request body, auth token, session
secret, or account shortcut.

Rate limit:

- Technical launch default: max 3 new support tickets per authenticated account
  per 10-minute rolling window.
- The route returns `429`, `Retry-After: 600`, `retryAfterSeconds: 600`, and
  `errorCode: support_ticket_rate_limited` when the window is exhausted.
- The limit uses existing `support_tickets.created_at` rows; no new table,
  migration, paid service, or remote config is required for the local slice.
- This is a route-specific abuse/control guard, not a full public PRD rate
  policy. Wider IP/device/WAF limits remain a release operations decision.

Storage:

- `support_tickets` stores the support body and compact metadata needed for
  launch CS investigation.
- `idempotency_records.operation = support_ticket_create` links the accepted
  request to the ticket id.
- Retention/deletion policy is a launch operations decision and should be set
  before PRD data retention commitments are published. Current idempotency
  retention follows `idempotency_records`; no local TTL/purge job is implemented
  in this slice.
- Admin support ticket collection is local-code available after `edb44b3`; DEV
  and PRD should remain "API pending" until their matching migrations/deploys
  are explicitly approved and applied.

Do not return or store:

- raw tokens;
- raw auth headers;
- refresh tokens or refresh token hashes;
- raw client log bundles;
- screenshots or attachments;
- raw RNG/probability/full weight tables;
- repair/manual grant intent.

<a id="source-b678a199237246c2-admin-support-ticket-read-endpoints"></a>
#### Admin Support Ticket Read Endpoints

```text
GET /v1/admin/support/tickets
GET /v1/admin/support/tickets/:ticketId
```

Implemented as Admin API read-only routes with `SUPPORT_READ` and
`ROLE_PERMISSION`. OWNER / OPERATOR / VIEWER can read for launch, matching the
support account summary policy.

List filters:

- `accountId`
- `category`
- `status`
- `limit`
- `cursor`

List response:

```ts
type AdminSupportTicketListResponse = {
  permission: "SUPPORT_READ";
  permissionMode: "ROLE_PERMISSION";
  items: AdminSupportTicketSummary[];
  nextCursor: string | null;
  supportContext: {
    readOnly: true;
    mutationEndpointsEnabled: false;
    repairEndpointsEnabled: false;
    source: "admin_support_tickets";
  };
  errorCode: null;
  message: null;
};
```

Detail response:

```ts
type AdminSupportTicketDetailResponse = {
  permission: "SUPPORT_READ";
  permissionMode: "ROLE_PERMISSION";
  ticket: AdminSupportTicketSummary & {
    body: string;
    idempotencyReplayKnown: boolean;
  };
  supportContext: {
    readOnly: true;
    mutationEndpointsEnabled: false;
    repairEndpointsEnabled: false;
    source: "admin_support_ticket_detail";
  };
  errorCode: null;
  message: null;
};
```

Admin ticket endpoints are not repair endpoints. Launch does not expose ticket
close, assign, reply, manual compensation, reward correction, account merge, or
session revoke controls.

<a id="source-b678a199237246c2-permission"></a>
#### Permission

Candidate stable permission id:

```text
SUPPORT_READ
```

Current implementation:

- Use the existing role-string `ROLE_PERMISSION` mapping until the Admin
  permission grant table exists.
- OWNER / OPERATOR / VIEWER can all receive `SUPPORT_READ` for launch because
  the endpoint is read-only and contains no repair controls.
- If support summaries later include more sensitive detail views, split
  `SUPPORT_READ_DETAIL` or OWNER-gated fields instead of turning this endpoint
  into a mutation surface.

Relationship to existing read permissions:

- `PLAYERS_READ`: account/profile status.
- `RUNS_READ`: recent runs and run detail references.
- `STORY_PROGRESS_READ`: durable story progress and unlock truth.
- `OFFLINE_REWARD_READ`: offline reward state and latest claim references.
- Future `GACHA_READ`: gacha batches, pity state, and guarantee visibility.
- `OPS_ERROR_LOG_READ`: runtime/system error correlation.

Launch implements `SUPPORT_READ` as a coarse support permission that implies
the read-only sections above. It does not require all underlying read
permissions separately.

Recommendation:

- Start with `SUPPORT_READ` as the single endpoint permission.
- Internally build sections from existing read-model SQL.
- Keep the response self-contained but compact.
- Do not expose any section that would require a mutation or repair permission.

<a id="source-b678a199237246c2-sections"></a>
#### Sections

<a id="source-b678a199237246c2-accountauth-status"></a>
##### Account/Auth Status

Source candidates:

- `accounts`
- `player_profiles`
- `auth_identities`
- `auth_sessions`

Fields:

- `accountId`
- `accountStatus`
- `displayName`
- `createdAt`
- `updatedAt`
- `identityProviders`: provider names only, such as `GUEST` and `GOOGLE`
- `providerNames`
- `linkedProviders`
- `primaryProvider`: `GOOGLE` when linked, otherwise `GUEST` or the first
  server-owned provider
- `hasGoogleLink`
- `activeSessionCount`
- `latestSessionCreatedAt`
- `latestSessionAt`
- `latestSessionExpiresAt`
- `latestAuthAt`
- `lastLoginAt`
- `lastLoginAgeSeconds`
- `lastSeenAt`: null until a server-owned activity source exists
- `lastSeenSource`: `not_available_without_activity_source`
- `concurrentUsers`: null until a server-owned activity/heartbeat source exists
- `concurrentUsersPrecision`: `UNAVAILABLE`

Do not return:

- refresh token hashes;
- access tokens;
- refresh tokens;
- raw auth headers;
- Cloudflare Access JWTs;
- service token ids/secrets;
- session family raw diagnostics beyond sanitized counts/statuses.

Login/session metrics are drafted in `docs/api/admin-session-metrics.md`.
Support should treat `auth_sessions.created_at` as last session creation or
refresh rotation time, not precise gameplay last-seen time.

<a id="source-b678a199237246c2-recent-runs"></a>
##### Recent Runs

Source candidates:

- `runs`
- `run_snapshots`
- `run_final_results`
- `operation_previews`

Fields:

- `runId`
- `status`
- `operationId`
- `actId`
- `regionId`
- `encounterType`: `ORDINARY_15_WAVE` or `RAID_BOSS`
- `raidId`: raid-only, otherwise null
- `bossId`: raid-only, otherwise null
- `bossPhaseRefs`: raid-only refs, otherwise empty
- `allowedOperatorSlots`: nullable launch candidate metadata
- `startAugmentPackageRef`
- `startTacticPackageRef`
- `contentVersion`
- `risk`
- `balanceVersion`
- `startedAt`
- `finishedAt`
- `outcome`
- `clearedWaves`
- `rewardPreviewId`

Support usage:

- Answer whether a reported sortie exists.
- Correlate run finish idempotency and reward preview creation.
- Link to Admin run detail.
- Distinguish ordinary 15-wave operations from raid boss-only encounters using
  server-authored preview refs.

Do not return:

- full runtime export payload;
- combat validator internals;
- client-submitted reward fields;
- localized story dialogue text.
- raw animation timelines, frame data, VFX/SFX/camera/hit-stop tracks, or raid
  seasonal attempt internals.

<a id="source-b678a199237246c2-run-reward-previewsclaims"></a>
##### Run Reward Previews/Claims

Source candidates:

- `run_reward_previews`
- `reward_transactions`
- `economy_transactions`
- `currency_ledger`
- `idempotency_records`

Fields:

- `rewardPreviewId`
- `runId`
- `previewStatus`
- `claimability`
- `rewardClaimId`
- `claimedAt`
- `balanceVersion`
- `expiresAt`
- `rewardLineSummary`: sanitized ids/quantities only
- `idempotencyReplayKnown`

Support usage:

- Explain whether the server created a reward preview.
- Explain whether a claim was already recognized.
- Check if duplicate replay should sync client state instead of granting again.

Do not return:

- raw reward calculation metadata;
- raw multiplier bps;
- repair suggestions that expose exploit details.

<a id="source-b678a199237246c2-offline-rewards"></a>
##### Offline Rewards

Source candidates:

- `account_offline_reward_state`
- `account_story_progress`
- offline reward `reward_transactions` where `run_id IS NULL`
- `idempotency_records.operation = 'offline_reward_claim'`

Fields:

- `stateSource`: `account_offline_reward_state` or
  `accounts.created_at_fallback`
- `storyProgressId`
- `lastOfflineRewardClaimAt`
- `lastStoryProgressId`
- `lastBalanceConfigVersion`
- `lastNarrativeVersion`
- `lastPreviewedAt`
- `revision`
- `latestOfflineRewardClaimId`
- `latestClaimedAt`

Support usage:

- Explain why a player has no offline reward yet.
- Confirm whether an offline claim was committed.
- Confirm duplicate replay without issuing a second grant.

Do not return:

- raw story progress multiplier bps;
- server calculation internals;
- any endpoint that resets `last_offline_reward_claim_at`.

<a id="source-b678a199237246c2-story-progressunlocks"></a>
##### Story Progress/Unlocks

Source candidates:

- `account_story_progress`
- `account_unlocks`
- `progression_ledger`
- `account_first_clears`

Fields:

- `actId`
- `highestStoryProgressId`
- `highestClearedOperationId`
- `order`
- `clearedOperationIds`
- `storyFlags`
- `unlockedOperations`
- `narrativeVersion`
- `lastProgressRunId`
- `updatedAt`
- `firstClearKeys`

Support usage:

- Explain operation availability.
- Correlate story progress with offline reward growth.
- Check first-clear support context.

Do not return full localized dialogue text, cutscene timing, callout playback
timing, or narrative authoring data.

<a id="source-b678a199237246c2-gacha"></a>
##### Gacha

Source candidates:

- `gacha_banners`
- `player_gacha_pity`
- `gacha_pull_batches`
- `gacha_pull_results`
- `item_ledger`
- `account_items`
- `idempotency_records.operation = 'gacha_pull'`

Fields:

- `bannerId`
- `batchId`
- `pullCount`
- `resultStatus`
- `guaranteeType`
- `postPityState`
- `duplicateConversionSummary`
- `costItemId`
- `costQuantity`
- `createdAt`

Support usage:

- Confirm a pull batch exists.
- Confirm a guarantee triggered.
- Confirm duplicate conversion references.
- Confirm idempotency replay did not spend/grant twice.

Do not return raw RNG material, seed preimage, per-candidate probability, full
weight list, or weighted internal calculations.

<a id="source-b678a199237246c2-idempotency-replay-status"></a>
##### Idempotency Replay Status

Source candidates:

- `idempotency_records`
- operation-specific result refs.

Fields:

- `operation`
- `resultRef`
- `createdAt`
- `replaySafe`
- `linkedResourceType`

Relevant operations:

- `run_finish`
- `run_reward_claim`
- `offline_reward_claim`
- `gacha_pull`

Support usage:

- Explain duplicate replay outcomes.
- Correlate player retry reports after network errors.

Do not return raw request body hashes unless a future scrubbed request-hash
policy is explicitly designed and tested.

<a id="source-b678a199237246c2-integrity-disposition"></a>
##### Integrity Disposition

Source candidates:

- `integrity_events`
- `integrity_event_review_states`
- `suspicious_player_reviews`

Fields:

- `openSignals`
- `hardViolations`
- `latestDetectionAt`
- `accountReviewStatus`
- `integrityRewardRestriction`
- `primaryReason`

Support usage:

- Explain why rewards may be restricted or reviewed.
- Escalate to operations without applying sanctions from CS view.

Do not expose detector internals that would help bypass validation.

<a id="source-b678a199237246c2-ops-error-logs"></a>
##### Ops Error Logs

Source candidates:

- `ops_error_logs` D1 summary read model;
- Workers Logs as troubleshooting companion;
- sanitized Unity client diagnostic summaries if enabled later.

Fields:

- `errorLogId`
- `occurredAt`
- `environment`
- `service`
- `severity`
- `errorCode`
- `messageSummary`
- `requestId`
- related resource ids such as `runId`, `rewardPreviewId`,
  `offlineRewardClaimId`, `gachaBatchId`.

Support usage:

- Correlate support tickets with backend/runtime failures.
- Link to the read-only `/ops/error-logs` Admin surface.

Current support route status:

- `GET /v1/admin/support/accounts/:accountId` composes the latest 5
  account-scoped launch-critical ops error rows.
- Included launch error codes are `google_auth_config_missing`,
  `offline_reward_state_conflict`, `gacha_state_conflict`, and
  `story_progress_state_conflict`.
- Rows are ordered critical first, then by latest occurrence.
- Operators can still use `GET /v1/admin/ops/error-logs?accountId=...` for the
  dedicated ops log read model and broader filters, or
  `GET /v1/admin/ops/error-logs/summary` for aggregate counts.

Do not return raw payloads, raw request bodies, tokens, secrets, or unsanitized
stack traces in launch support summary.

<a id="source-b678a199237246c2-launch-login-bonus"></a>
##### Launch Login Bonus

Source candidates:

- `account_login_bonus_claims`
- `reward_transactions`
- `idempotency_records.operation = 'login_bonus_claim'`

Fields:

- `bonusId`
- `claimStatus`: `UNCLAIMED` or `CLAIMED`
- `claimedAt`
- `rewardTransactionId`
- `expectedRewardLines`
- `rewardLineSummary`
- `idempotencyReplayKnown`
- `readOnly`

Launch status:

- The support summary exposes read-only evidence for the Act 1 launch login
  bonus.
- The expected non-repeat launch reward is `cur_command_ticket` quantity `5`
  with source `LAUNCH_LOGIN_BONUS`.
- Claimed status is derived from server-owned `account_login_bonus_claims`;
  support never trusts client-side bonus state.
- `idempotencyReplayKnown` is a support hint from stored
  `login_bonus_claim` idempotency records.

This section does not open manual grant, replay, repair, reset, revoke, or
economy correction controls.

<a id="source-b678a199237246c2-escalation-hints"></a>
##### Escalation Hints

Source candidates:

- `docs/ops/customer-support-response-templates.md`;
- `docs/ops/customer-support-escalation-workflow.md`;
- future support escalation read model if one is explicitly approved later.

Launch status:

- The support summary may include safe support note/template hints.
- It does not create or update escalation state.
- `severity`, `category`, `recommendedAction`, and evidence package
  availability remain future read-only fields.

Allowed launch action labels remain operational guidance only:

- `respond_with_template`
- `request_more_player_context`
- `escalate_to_operator_review`
- `escalate_to_owner_review`
- `mark_as_known_issue_candidate`
- `attach_read_only_evidence`

The endpoint must not perform manual reward grants, currency correction,
offline timestamp reset, story progress mutation, unlock mutation, gacha
reroll, reward repair, economy repair, narrative editing, or localization
publishing.

<a id="source-b678a199237246c2-player-facing-explanation-templates"></a>
#### Player-facing Explanation Templates

Support responses should be safe, brief, and avoid exploit-sensitive details.

Examples:

- Offline reward empty: "오프라인 보상은 최소 누적 시간이 지나야 수령할 수 있습니다. 현재 서버 기록상 아직 수령 가능 시간이 부족합니다."
- Duplicate replay: "서버가 이전 요청을 이미 정상 처리한 기록이 있습니다. 앱을 새로고침하거나 보상/재화 상태 동기화를 시도해 주세요."
- Reward claim found: "서버 기록상 보상 수령이 완료되어 계정 장부에 반영된 상태입니다."
- Gacha pull found: "서버 기록상 해당 모집 요청이 처리되어 결과와 천장 상태가 저장되어 있습니다."
- Integrity escalation: "해당 기록은 운영 검토가 필요한 상태입니다. 상세 확인 후 별도로 안내드리겠습니다."

Avoid explaining:

- exact validator bypass triggers;
- RNG seed/probability internals;
- reward multiplier internals;
- token/session internals;
- manual repair steps that are not available in launch scope.

<a id="source-b678a199237246c2-security-and-masking"></a>
#### Security And Masking

The support summary must never return:

- full access tokens;
- refresh tokens or refresh token hashes;
- Cloudflare Access JWTs;
- service token secrets;
- raw `Authorization` headers;
- raw request bodies;
- raw RNG material;
- RNG seed preimages;
- per-candidate probability;
- full gacha weight lists;
- raw reward multiplier bps;
- internal calculation preimages;
- full localized dialogue text;
- account shortcut headers such as `X-Account-Id`.

Use stable ids, timestamps, counts, status codes, and sanitized references
instead.

<a id="source-b678a199237246c2-test-candidates"></a>
#### Test Candidates

Future contract/unit tests should cover:

- missing Admin principal returns the normal Admin auth error;
- missing `SUPPORT_READ` returns `ADMIN_PERMISSION_DENIED`;
- OWNER / OPERATOR / VIEWER read-only access according to the chosen permission
  policy;
- request body returns `contract_violation`;
- missing account returns `account_not_found`;
- no story/offline rows return stable fallback sections;
- recent run/reward/offline/gacha/idempotency sections preserve stable ids and
  raw status codes;
- response does not include tokens, raw request bodies, raw RNG, raw bps,
  reward calculation internals, full localized dialogue, or account shortcut
  data;
- endpoint performs no writes.

Future local D1 smoke should seed:

- one account with story progress;
- one completed run with reward preview/claim;
- one offline reward state and claim;
- one gacha batch with guarantee type;
- one idempotency replay record;
- one integrity signal;
- then verify `GET /v1/admin/support/accounts/:accountId` returns only
  sanitized references and leaves table counts unchanged.

<a id="source-b678a199237246c2-rollout-order"></a>
#### Rollout Order

1. Contract draft completed.
2. Admin Web support placeholder/contract types aligned by the Admin session.
3. Admin API unit contract tests for `SUPPORT_READ` added.
4. Local D1 smoke with seeded account support data added.
5. `GET /v1/admin/support/accounts/:accountId` read-only route implemented.
6. Add Admin Web read-only integration against the implemented route.
7. Consider separate read models only if direct joins become too expensive.

Remote DEV secret setup, DEV deploy, remote smoke, PRD resources, and
STAGING/PRD deploys remain approval-gated.

<a id="source-b678a199237246c2-open-decisions"></a>
#### Open Decisions

- Whether VIEWER can see every section or whether OWNER-only details are needed
  for stack/ops metadata later.
- Whether to add a separate `support_case_notes` table after launch.
- Whether to add an `offline_reward_claims` read model or rely on
  `reward_transactions` for launch.
- Exact section limits for recent runs, gacha batches, and ops errors.
- Whether to expose future support escalation `severity`, `category`,
  `recommendedAction`, and evidence package availability as read-only fields.

<!-- source-body:source-b678a199237246c2:end -->


<a id="source-e992dfe4e3dd5d24"></a>
## S04 owned train installation

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/train-loadout.md`  
원문 SHA-256: `787b3f5bc69ed863e8cc2216f9e0f7c658f776f932101b586bfa03f294b95399`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-e992dfe4e3dd5d24:start -->
<a id="source-e992dfe4e3dd5d24-s04-owned-train-installation"></a>
### S04 owned train installation

Local contract, migration `0029_train_loadout.sql`. No remote migration, seed or
deployment is implied. Existing `GET /v1/account/state` is unchanged; use this
separate resource for installation. All requests require the existing Bearer
authentication/client admission, have no query parameters, and return `no-store`.

<a id="source-e992dfe4e3dd5d24-identity-and-approved-scope"></a>
#### Identity and approved scope

`account_train` supplies the owned locomotive. `account_carriages` has primary key
`(account_id, carriage_id)`: a carriage ID denotes one existing owned row for that
account, not an unlimited definition that can be installed multiple times. No
acquisition, repair, stat calculation, grant, price, refund or deletion occurs.
Locomotive is separate from carriage slots; all unlocked slots must be filled.

| Existing ID | Type | Installation condition |
| --- | --- | --- |
| car_combat | COMBAT | Owned; exactly one required |
| car_gun | GUN | Owned |
| car_cargo | CARGO | Owned and server cleared `opn_03_water_stop` |
| car_defense | DEFENSE | Owned and server cleared both water stop and `opn_05_night_boarders` |
| car_medical | MEDICAL | Unsupported: `medical_unlock_unapproved` |
| Other owned ID | null | Unsupported: `carriage_definition_unsupported` |

Read the actual `account_story_progress.cleared_operations_json`. Default two
slots, water stop three, water stop plus night boarders four. No client milestone,
highest-progress string or owned inventory substitutes for this evidence. Night
without water reports `story_progress_inconsistent`; malformed history reports
`story_progress_invalid`; neither is silently normalized or granted unlocks.

Source type limits remain COMBAT exactly 1, CARGO at most 1, GUN/MEDICAL/DEFENSE at
most 2. This does not authorize creating a second instance. An approved
instance-to-definition contract is required before any additional IDs are accepted.
Medical's proposed water-stop swap is not approved. All owned turrets remain
visible but unsupported (`turret_installation_undefined`): target slots, counts,
compatibility and unlock rules are absent. Unsupported ownership is preserved.

<a id="source-e992dfe4e3dd5d24-get-v1trainsloadout"></a>
#### GET /v1/trains/loadout

```ts
type Selection = {
  rulesVersion: "train-loadout-v1";
  trainId: string;
  carriages: { slot: number; carriageId: string }[];
};
type Response = {
  rulesVersion: "train-loadout-v1";
  train: { trainId: string; level: number; revision: number } | null;
  ownedCarriages: {
    carriageId: string; level: number; revision: number;
    type: "COMBAT" | "GUN" | "CARGO" | "DEFENSE" | "MEDICAL" | null;
    installationSupported: boolean; installable: boolean; reason: string | null;
  }[];
  ownedTurrets: {
    turretId: string; level: number; revision: number;
    installationSupported: false; reason: "turret_installation_undefined";
  }[];
  unlockedSlotCount: 2 | 3 | 4 | null;
  progressError: null | "story_progress_invalid" | "story_progress_inconsistent";
  typeLimits: Record<"COMBAT" | "GUN" | "CARGO" | "DEFENSE" | "MEDICAL", { min: number; max: number }>;
  revision: number; // installation revision, 0 before first save
  loadout: Selection | null;
  loadoutError: string | null;
  openRun: boolean;
  updatedAt: string | null; // ISO UTC
};
```

One SQLite statement reads a consistent inventory/progress/selection snapshot.
GET does not initialize anything. Empty inventory is `train:null` and empty arrays;
empty installation is `revision:0,loadout:null,updatedAt:null`. Persisted selection
is retained if ownership/progress later changes, with `loadoutError` identifying
invalidity. Unsupported stored schema gives `loadout:null` and the schema error;
the database value is not repaired. `installable` describes that owned carriage's
catalog/progress eligibility, not sufficient slots or permission to mutate during
an open run; consult `openRun` and POST validation as well.

<a id="source-e992dfe4e3dd5d24-post-v1trainsloadout"></a>
#### POST /v1/trains/loadout

Header `Idempotency-Key`: 1–128 printable non-space ASCII characters. Body is strict;
unknown fields (including client stats, equipment, ownership or story flags) fail.

```json
{
  "rulesVersion": "train-loadout-v1",
  "trainId": "train_a",
  "expectedRevision": 0,
  "carriages": [
    {"slot": 0, "carriageId": "car_combat"},
    {"slot": 1, "carriageId": "car_gun"}
  ]
}
```

Use the actual train ID from GET, not the illustrative `train_a`. IDs use existing
`[A-Za-z0-9_-]{1,100}` contract. Slots are unique, contiguous from zero and filled
to the current slot count. IDs are unique. Row ordering is canonicalized by slot.
`expectedRevision` is a nonnegative safe integer below MAX_SAFE_INTEGER.

```json
{
  "accepted": true,
  "receiptId": "train_loadout_...",
  "revision": 1,
  "changed": true,
  "loadout": {
    "rulesVersion": "train-loadout-v1",
    "trainId": "train_a",
    "carriages": [{"slot":0,"carriageId":"car_combat"},{"slot":1,"carriageId":"car_gun"}]
  },
  "refreshRequired": true
}
```

Accepted exact same selection with a fresh key/current revision returns
`changed:false` with unchanged revision and timestamp, but persists its own receipt.
Same account/key/canonical body returns the original immutable response before
current ownership/revision/run checks, even after later state changes. A changed
body (including expectedRevision) with the same key fails `idempotency_conflict`.
On ambiguous transport failure retry the exact key/body; after success or conflict,
refresh GET and account state and replace the UI snapshot. Never increment a local
balance or revision based only on receipt replay.

An account mutation sequence is captured before reads. A single D1 transaction
checks current ownership, raw progress, selection revision and absence of ACTIVE,
FINISH_PENDING or QUARANTINED runs, then stores selection and immutable receipt.
CAS/unique races retry at most four attempts with receipt rechecks. Failed writes
roll back mutation, preference and receipt together. No currency/item ledger writes.

Error shape is `{accepted:false,errorCode:string}`:

| HTTP | Codes |
| --- | --- |
| 400 | contract_violation, train_slot_count_invalid |
| 401 / admission status | Existing auth/client admission errors |
| 403 | train_not_owned, carriage_not_owned |
| 409 | idempotency_conflict, train_loadout_revision_conflict, train_loadout_open_run, train_loadout_write_conflict, story_locked, story_progress_invalid, story_progress_inconsistent, medical_unlock_unapproved, carriage_definition_unsupported, train_type_limit_invalid |

`train_loadout_schema_unsupported` is a GET loadoutError for incompatible saved
data. S04 does not issue departure previews or freeze run state; S11 is separate.

<a id="source-e992dfe4e3dd5d24-verification"></a>
#### Verification

Ten signed-auth real SQLite tests cover reads/no grants, unsupported ownership,
strict slots, ownership, milestones, replay/conflict/no-op, all open run statuses,
commit-time ownership/progress/start races, concurrent installs and receipt failure
rollback. Initial HTTP tests failed 404 before implementation; the final targeted
run passed. Full-suite/typecheck/lint evidence is recorded in the owner handoff.

<!-- source-body:source-e992dfe4e3dd5d24:end -->


<a id="source-feaac556a37c1cd1"></a>
## CE01 explicit ultimate operator selection

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/api/ultimate-selection.md`  
원문 SHA-256: `fb83176da3e6f8af60146f1d0ceb15818329dcfc1910bef2ca6b30cb2b2a5590`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-feaac556a37c1cd1:start -->
<a id="source-feaac556a37c1cd1-ce01-explicit-ultimate-operator-selection"></a>
### CE01 explicit ultimate operator selection

Local implementation, migration `0031_ultimate_selection.sql`. This implements the
approved pre-start choice among the actual five; no battle effects, timings, stats,
seed or catalog policy is enabled. New public paid starts remain `combat_not_ready`.

<a id="source-feaac556a37c1cd1-read-and-save"></a>
#### Read and save

`GET /v1/formation/ultimate` requires bearer authentication and existing admission.
No query or body. Read-only, no lazy row creation. Every response is `no-store`.

```json
{"accepted":true,"rulesVersion":"ultimate-selection-v1","operatorId":null,"revision":0,"formationRevision":1,"selectionStatus":"UNSELECTED","changeLocked":false}
```

`selectionStatus`: `FORMATION_REQUIRED` (no active formation), `OWNERSHIP_INVALID`
(active five no longer all owned), `UNSELECTED`, or `SELECTED`. Missing formation
has formationRevision/revision 0 and operatorId null. Invalid persisted formation
or a selected ID outside its five yields 409, never an automatic replacement.
`changeLocked` reflects ACTIVE/FINISH_PENDING/QUARANTINED run when formation exists.
The dedicated GET is the authoritative selection read; existing S01 DTO is unchanged.

`POST /v1/formation/ultimate` requires `Idempotency-Key` (1–128 non-space printable
ASCII characters) and this strict JSON body, no query/extra fields:

```json
{"rulesVersion":"ultimate-selection-v1","operatorId":"op_example","expectedRevision":0,"expectedFormationRevision":1}
```

The example ID must be replaced by an actual owned active member. Explicit null
clears selection. Revisions are nonnegative safe integers below MAX_SAFE_INTEGER.
Response: accepted, rulesVersion, operatorId, revision, formationRevision,
selectionStatus (`SELECTED`/`UNSELECTED`) and refreshRequired=true. Successful
changed choice increments only ultimate revision. New-key no-op keeps revision;
it still has a committed receipt. Formation revision is not incremented by this POST.

401 is missing/invalid/revoked authentication. 400 `contract_violation` is malformed
input. 403 `ultimate_operator_not_eligible` is a target outside the active five or
an active member no longer owned. 409 codes: `formation_required`,
`formation_schema_unsupported`, `ultimate_selection_invalid`,
`formation_revision_conflict`, `ultimate_revision_conflict`, `ultimate_open_run`,
`idempotency_conflict`, `ultimate_write_conflict`.

Receipt namespace is account + ultimate-selection endpoint + key. Same original
body/key returns exact committed response before checking current revisions/run
lock; changed body with same key conflicts. After a lost response retain/retry the
original body and key. A receipt replay is historical, so GET current state before
another edit. A 409 retains the UI draft; refresh authoritative revisions before an
explicitly new command. Never silently turn a conflict into a new-key retry.

<a id="source-feaac556a37c1cd1-atomic-formation-and-departure-behavior"></a>
#### Atomic formation and departure behavior

Columns account_formation.ultimate_operator_id (nullable) and ultimate_revision
(default0) migrate existing formations without selecting anyone. No Kyle/slot0
fallback. POST uses the shared account mutation sequence and an atomic SQL batch
guard on formation JSON, both revisions, prior choice, all-five ownership and run
lock. Receipt insertion failure rolls back the update.

The migration's AFTER UPDATE OF formation_json trigger clears a choice absent
from the new five and increments ultimate revision in that same transaction.
It preserves a retained member, including reordering; inactive preset SAVE never
touches the active row. Failed activation rolls back both formation and clear.
Formation activation retains its existing run lock. Activate response still says
refreshRequired; clients must GET the choice after activation.

New strict `POST /v1/operations/preview` body additionally requires
`expectedUltimateRevision` (0 allowed); admission version is now `departure-v2`.
Preview validates the stored choice and all-five ownership, verifies the caller's
three revisions and transactionally pins:

```ts
admission.ultimateSelection = {
  rulesVersion: "ultimate-selection-v1",
  operatorId: string | null,
  revision: number
};
```

Null can be quoted to expose why combat is blocked; it cannot start in the domain
core. The retained core checks nonnull selection and compares the entire admission
against current state, including choice/revision; commit guards repeat these
checks. Successful internal-core run snapshots include immutable
`admission.ultimateSelection` and root `ultimateOperatorId`. HTTP paid start still
returns 409 combat_not_ready and never calls that core. This is not battle entry.

New diagnostics use `run-combat-requirements-v2`, ultimateOperatorId:string|null
and ultimateSelectionRevision:number|null. A valid pinned member removes only
`ultimate_selection_required`; six other blockers remain. Legacy previews without
CE01 pin return null and that blocker. Existing successful start/preview receipts
remain exact historical responses, not upgraded with current choices. Old
uncommitted departure-v1 previews are unsupported by the new core; obtain a new
explicit quote with the new schema. Original committed run recovery stays intact.

<a id="source-feaac556a37c1cd1-validation-and-scope"></a>
#### Validation and scope

Integration tests cover null/read-only, ownership, strict input, CAS, exact replay,
account isolation/auth, active selection preservation/clear, rollback, all three
run locks, concurrent CAS/activation/start/preview races, snapshot pinning and null
core rejection. Fixture choices are explicit test setup only. Migration and code
must be deployed together in a separately authorized remote operation; none was
performed here.

Verification on 2026-09-10: full 79 files / 516 tests passed (16.16 seconds),
including 12 CE01 tests. Game API TypeScript and source ESLint passed; independent
source review found no actionable defect. Full migration fixtures explicitly
expect 31 migrations; old committed run replay tests remain passing.

<!-- source-body:source-feaac556a37c1cd1:end -->


<a id="source-c2d7bd0327f4d353"></a>
## Data Ownership

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/architecture/data-ownership.md`  
원문 SHA-256: `ecbe5d86aae6855d0a6acf4642be1855797a67550c481bd3c1922311de577372`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-c2d7bd0327f4d353:start -->
<a id="source-c2d7bd0327f4d353-data-ownership"></a>
### Data Ownership

Unity client owns local combat frame simulation only. The server owns permanent data, run creation, wave plans, rewards, validation inputs, and deterministic result validation.

Routes must not write SQL directly and must not calculate costs or rewards directly. Apps call package-level services/adapters; packages must not import apps.

<!-- source-body:source-c2d7bd0327f4d353:end -->


<a id="source-d0978b2758d3a357"></a>
## Launch economy: configuration versus integration

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/architecture/launch-economy-integration-next.md`  
원문 SHA-256: `838a0e911561f9e3262d3830325c9e41dc7b8ccf3993a0243e01b78dfcec27c3`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-d0978b2758d3a357:start -->
<a id="source-d0978b2758d3a357-launch-economy-configuration-versus-integration"></a>
### Launch economy: configuration versus integration

Follow-up implemented locally on2026-09-09: see
[growth and launch implementation](../../../QA/DocumentIntegration-20260921/md-cleanup/semantic/archive.html#doc-312ba27fad6669ec8ca4).
The status table below describes the earlier config-only checkpoint and is historical.

2026-09-09 first unit: local versioned snapshot, strict parser and quote accessors.
Planning snapshot SHA256: C17BA2EB831AFB9C0C5F6EB55DBE13EC06D24A3A89C78708B409974EF065169A.
The source is canonical docs/ops/launch-economy-v1.json. Plan owns the numbers.

| Layer | Status |
| --- | --- |
| Exact snapshot and strict schema | Implemented locally |
| Risk-only victory EXP / exact Training Core quote | Implemented locally |
| Persisted immutable version activation and request version locking | Not connected; parser only verifies caller's expected version |
| Actual reward routing to new offline/run values | Not connected |
| Growth debit/refund/level mutation API | Not implemented |
| Equipment/research target activation | Blocked on authoritative catalogs |
| DEV/PRD deployment or DB seed | Not performed |

<a id="source-d0978b2758d3a357-next-independently-testable-units"></a>
#### Next independently testable units

1. Version storage/activation contract: bind immutable economy version plus content
   identity to the existing balance version model. A version string check alone
   does not prevent changing a payload under the same version. Store immutable
   checksum and source identity; mutation handlers read the pinned version, never
   trust client cost or a client-provided expected version as authority.
2. Reward integration: adapt offline and ordinary/story-raid paths with distinct
   materials and EXP formulas. Preserve outcome contracts; ordinary failure EXP is
   floor(riskEXP * 8000 * clearedWaves / 10000 / 15); no operation/risk multiplier
   on EXP. Story raid amounts are final. Preserve run-start balance locking and
   replays. Validate old and new pinned versions separately before activation.
3. Training Core level-up/sync-up/replace: existing operator_training_core_slots
   carries invested_operator_exp. Use current account ownership and 0–4 slot IDs;
   quote pinned costs, conditionally debit all resources and update progression
   and invested EXP in one account-sequenced transaction. Replace refunds stored
   actual investment100%, not a recalculated curve; no automatic reinvestment.
4. Train/skill/promotion: reuse authoritative ownership IDs and existing limits.
   account_train currently has no level column, so train level persistence requires
   an explicit additive migration; do not infer or rewrite existing levels.
   Skill/promotion columns exist. All spending requires server cost selection,
   progression and currency ledger linkage plus an immutable replay receipt.
5. Equipment/research: schema profiles are available, but they cannot authorize
   targets. Before API/migration design, obtain approved target catalog, effects,
   prerequisites and level caps. Do not create generic client-selected purchases.
6. First-clear budget and source replacement audit: current Game index.ts:6253
   queries account_first_clears by account_id+operation_key+risk, and :6406 inserts
   that same key. Audit material/core/ticket sources and per-risk upper bounds
   separately before converting rewards. Twelve launch tickets →1200FREEcrystals
   must replace the same unclaimed sources only; no extra grant to prior claimants.
   Missing wallet policy must fail closed. No replacement is part of this unit.

Every mutation unit needs replay-before-stale-version validation, same-key altered
payload rejection, insufficient-funds rollback, concurrent-spend safety, unknown
target no-spend, version mismatch, and actual-investment refund tests against the
local DB. Configuration unit tests do not claim these guarantees. Existing wallet
balances, investments and already-claimed source records remain unchanged.

Verification: initial targeted test run failed because module was absent; after
implementation10 tests pass. Full local suite68files/411tests passes. Game API
TypeScript and changed-file ESLint pass. Existing installed Vitest4.1.11 was invoked
by absolute CLI path because pnpm exec could not locate its binary in this worktree;
offline install stopped before modifying dependencies. No remote action occurred.

<!-- source-body:source-d0978b2758d3a357:end -->


<a id="source-6f5e52c4e100a74b"></a>
## LAST RAIL Server / Backend / Cloudflare 전체 구축 Master 작업지시

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/architecture/server.md`  
원문 SHA-256: `5574eab59d2416919b81185b6c85d36333b3ad9e6d37178220d3b0966f9305f5`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-6f5e52c4e100a74b:start -->
<a id="source-6f5e52c4e100a74b-last-rail-server--backend--cloudflare-전체-구축-master-작업지시"></a>
### LAST RAIL Server / Backend / Cloudflare 전체 구축 Master 작업지시

이 저장소에는 LAST RAIL 서버 작업이 아직 거의 또는 전혀 구현되어 있지 않다고 가정한다.

너는 이 지시문을 **LAST RAIL Server Source of Truth**로 사용하여 서버 프로젝트를 처음부터 구축한다.

기존 파일이 일부 존재한다면 먼저 전체를 읽고 현재 상태를 파악한 뒤, 아래 Source of Truth와 충돌하는 오래된 구조는 제거 또는 마이그레이션한다.

임의로 게임 기획을 변경하지 말고, 요구사항이 이미 명시된 영역은 다시 질문하지 말고 그대로 구현한다.

작업은 단계별로 진행하고 각 Phase 완료 시:

* 변경 파일
* 구현 내용
* 실행한 명령
* 테스트 결과
* 남은 문제
* 다음 Phase

를 짧게 보고한 뒤 다음 Phase를 진행한다.

단, **PRD에 실제 배포하거나 Production 데이터를 변경하는 행위는 내가 명시적으로 승인하기 전까지 금지한다.**

Cloudflare 로그인이나 외부 계정 승인처럼 사용자 개입이 반드시 필요한 경우에만 중단하고 정확히 무엇을 해야 하는지 알려준다.

---

<a id="source-6f5e52c4e100a74b-0-프로젝트-기본-정보"></a>
### 0. 프로젝트 기본 정보

프로젝트:

**LAST RAIL**

플랫폼:

**Unity Android**

서버 Runtime:

**Cloudflare Workers**

언어:

**TypeScript**

Router:

**Hono**

DB:

**Cloudflare D1**

Package Manager:

**pnpm**

Config:

**wrangler.jsonc**

Async:

**Cloudflare Queues**

장기 Object Storage:

**R2**

Analytics:

**Workers Analytics Engine**

ID:

**ULID TEXT**

Timestamp:

**UTC Unix Milliseconds INTEGER**

영구 Economy:

**INTEGER only**

모든 서버 Domain Integer는 JavaScript에서도:

```text
Number.isSafeInteger
```

검증한다.

---

<a id="source-6f5e52c4e100a74b-1-핵심-서버-책임"></a>
### 1. 핵심 서버 책임

전투 Frame Simulation은 서버에서 실행하지 않는다.

```text
Unity Client
= Combat Local Simulation

Server
= Permanent Data / Run 생성 / WavePlan / Reward / Validation
```

Server가 매 Frame 전투를 재시뮬레이션하지 않는다.

대신:

* Run Snapshot
* Balance Config Version
* Server WavePlan
* Augment Plan
* Augment History
* Tactical Ledger
* Action Ceiling
* DPS Envelope
* Deterministic Rule Validation

을 통해 결과 가능성을 검증한다.

---

<a id="source-6f5e52c4e100a74b-2-repository-구조"></a>
### 2. Repository 구조

Monorepo:

```text
lastrail-server/

apps/
├─ game-api
├─ admin-api
└─ async-worker

packages/
├─ domain
├─ db
├─ economy
├─ progression
├─ operation
├─ run
├─ integrity
├─ balance
├─ analytics
├─ contracts
└─ common

tests/
├─ fixtures
└─ integration

scripts/
├─ migration
├─ seed
├─ validation
└─ maintenance

docs/
├─ architecture
├─ api
├─ cloudflare
└─ run-protocol
```

각 Worker는 별도:

```text
wrangler.jsonc
```

를 가진다.

`packages/*`가 `apps/*`를 import하면 안 된다.

의존성은:

```text
apps
↓
domain services / adapters
↓
domain
```

방향으로 유지한다.

Route에서 SQL 직접 작성 금지.

Route에서 Reward/Cost 직접 계산 금지.

---

<a id="source-6f5e52c4e100a74b-3-환경-분리"></a>
### 3. 환경 분리

반드시:

```text
LOCAL
DEV
PRD
```

을 완전히 분리한다.

Workers:

```text
lastrail-game-api-dev
lastrail-game-api-prd

lastrail-admin-api-dev
lastrail-admin-api-prd

lastrail-async-worker-dev
lastrail-async-worker-prd
```

D1:

```text
lastrail-core-dev
lastrail-core-prd
```

DEV/PRD가 D1/Queue/R2/Secret을 공유하면 안 된다.

---

<a id="source-6f5e52c4e100a74b-4-공식-domain"></a>
### 4. 공식 Domain

공식 Domain:

```text
lastrail.app
```

환경별:

```text
DEV
dev-api.lastrail.app
dev-admin.lastrail.app

PRD
api.lastrail.app
admin.lastrail.app
```

`lastrail.app` 루트는 향후:

* 공식 홈페이지
* Privacy Policy
* Terms
* Support

용도로 사용한다.

async-worker는 외부 공개 Domain을 만들지 않는다.

Balance 전용 별도 서브도메인은 만들지 않는다.

Game API:

```text
GET /v1/balance-config/:version
```

으로 제공한다.

---

<a id="source-6f5e52c4e100a74b-5-admin-보안"></a>
### 5. Admin 보안

`admin.lastrail.app` 계열은 공개 Admin 페이지로 두지 않는다.

반드시:

```text
Cloudflare Access
↓
MFA
↓
Access JWT
↓
admin-api JWT 검증
↓
admin_principals
↓
OWNER / OPERATOR / VIEWER
```

구조를 사용한다.

Cloudflare Access 성공만으로 LAST RAIL Admin 권한을 부여하지 않는다.

JWT `sub`를:

```text
admin_principals.access_subject
```

와 매핑한다.

이메일만으로 권한을 부여하지 않는다.

직접 ID/Password 로그인 시스템을 새로 구현하지 않는다.

Cloudflare Access가 Authentication을 담당하고:

```text
admin_principals
```

가 Authorization을 담당한다.

Production Admin:

```text
MFA Required
```

정책.

최초 OWNER는 bootstrap script를 통해 명시적으로 등록한다.

마지막 ACTIVE OWNER를 제거하거나 Role을 내리는 요청은 막는다.

---

<a id="source-6f5e52c4e100a74b-6-admin-role"></a>
### 6. Admin Role

```text
OWNER
OPERATOR
VIEWER
```

Permission Matrix는 코드에서 관리한다.

최소 Permission:

```text
ACCOUNT_READ

ECONOMY_READ
ECONOMY_CORRECT

RUN_READ

INTEGRITY_READ
INTEGRITY_REVIEW
INTEGRITY_RESOLVE

ENFORCEMENT_READ
ENFORCEMENT_CREATE
ENFORCEMENT_LIFT
PERMANENT_BAN

COMPENSATION_READ
COMPENSATION_CREATE
COMPENSATION_PREPARE
COMPENSATION_EXECUTE

BALANCE_READ
BALANCE_DRAFT_CREATE
BALANCE_DRAFT_EDIT
BALANCE_DRAFT_DISCARD
BALANCE_VALIDATE
BALANCE_PUBLISH
BALANCE_ROLLBACK

VALIDATOR_READ
VALIDATOR_CONTROL

CLIENT_BUILD_READ
CLIENT_BUILD_CONTROL

ADMIN_PRINCIPAL_READ
ADMIN_PRINCIPAL_MANAGE

ADMIN_JOB_READ
ADMIN_JOB_EXECUTE

AUDIT_READ
```

Production 기준:

```text
BALANCE_PUBLISH
BALANCE_ROLLBACK
PERMANENT_BAN
COMPENSATION_EXECUTE
```

는 OWNER 전용으로 구현한다.

---

<a id="source-6f5e52c4e100a74b-7-위험-admin-작업"></a>
### 7. 위험 Admin 작업

고위험 작업은:

```text
Preview
↓
Confirmation Token
↓
Execute
```

절차를 사용한다.

대상:

* Balance Publish
* Balance Rollback
* Permanent Ban
* Economy Correction
* Compensation Execute
* Validator Runtime Control
* Client Build Block

Confirmation Token 원문은 DB에 저장하지 않는다.

Hash만 저장한다.

Confirmation:

* Principal 귀속
* Permission 귀속
* Payload Hash 귀속
* 짧은 TTL
* 1회용

Execute 순간 최신 Permission을 다시 확인한다.

Confirmation Consume과 실제 Domain Write는 반드시 같은 Transaction에 들어간다.

---

<a id="source-6f5e52c4e100a74b-8-모든-admin-write-audit"></a>
### 8. 모든 Admin Write Audit

Append-only:

```text
admin_audit_logs
```

사용.

기록:

* Principal
* Role
* Permission
* Action
* Resource
* Request ID
* Confirmation ID
* Payload Hash
* 변경 요약
* Account Mutation
* 결과
* Error Code
* Timestamp

금지:

* Access JWT 원문
* Confirmation Token
* Refresh Token
* Signing Secret
* Cloudflare Token
* Google Secret
* Ad Provider Secret

Audit 수정/삭제 금지.

---

<a id="source-6f5e52c4e100a74b-9-versioned-balance-config"></a>
### 9. Versioned Balance Config

Unity ScriptableObject는 수치형 Balance Source of Truth가 아니다.

```text
Unity ScriptableObject
= ID / Prefab / Behavior / Animation / FX / Audio / Sprite / Fallback

Server Balance Config
= Numeric Gameplay Balance Source of Truth
```

Server 구조:

```text
balance_config_drafts
balance_config_publish_claims
balance_config_versions
balance_config_activations
balance_config_active
```

Published Version은 immutable.

Admin 수정은 Draft에서만 한다.

Published Version 직접 Edit/Delete 기능 금지.

---

<a id="source-6f5e52c4e100a74b-10-balance-draft--publish"></a>
### 10. Balance Draft → Publish

Draft:

```text
baseVersion
schemaVersion
requiredContentVersion
config
checksum
revision
```

Draft 수정마다 revision 증가.

Publish는:

```text
draftId
expectedRevision
expectedChecksum
```

를 검증한다.

또한 Beta에서는:

```text
draft.baseVersion
==
현재 activeVersion
```

이어야 Publish 가능하게 한다.

아니면:

```text
BALANCE_DRAFT_STALE_BASE
```

실패.

Publish 성공 Transaction:

```text
새 balance_config_version INSERT

publish_claim INSERT

Draft → PUBLISHED

activation history INSERT
PUBLISH

balance_config_active 변경

Admin Confirmation consume

Admin Audit
```

전부 원자적으로 처리한다.

Publish 성공:

```text
Publish = Activate
```

이다.

---

<a id="source-6f5e52c4e100a74b-11-balance-rollback"></a>
### 11. Balance Rollback

Rollback은 Version을 수정하지 않는다.

예:

```text
105
106
107 ACTIVE
```

에서:

```text
107 → 106
```

으로 Active Pointer만 변경한다.

107은 그대로 영구 보존한다.

Rollback도:

```text
Preview
→ Confirmation
→ Execute
```

고위험 작업이다.

Activation History:

```text
PUBLISH
ROLLBACK
```

전부 append-only 기록.

---

<a id="source-6f5e52c4e100a74b-12-balance-config-필수-metadata"></a>
### 12. Balance Config 필수 Metadata

Published Version:

```text
version
schemaVersion
requiredContentVersion
checksum
configJson
publishedAt
publishedBy
```

Checksum:

```text
Canonical JSON
→ SHA-256
```

대상 Envelope:

```text
schemaVersion
requiredContentVersion
config
```

과거 Published Balance Version은 영구 보존한다.

---

<a id="source-6f5e52c4e100a74b-13-client-build--content-version"></a>
### 13. Client Build / Content Version

```text
client_builds
```

관리.

필드:

```text
platform
versionName
buildNumber
contentVersion
protocolVersion

ALLOWED
UPDATE_REQUIRED
BLOCKED
```

Balance:

```text
requiredContentVersion
```

과 Client:

```text
contentVersion
```

비교.

조건:

```text
client.contentVersion
>=
balance.requiredContentVersion
```

신규 Preview/Run에서 강제.

---

<a id="source-6f5e52c4e100a74b-14-client-balance-fetch"></a>
### 14. Client Balance Fetch

Boot/Lobby:

```text
GET /v1/bootstrap
```

에서:

```text
activeBalanceVersion
requiredContentVersion
schemaVersion
checksum
```

전달.

Local Cache Version이 다르면:

```text
GET /v1/balance-config/:version
```

Fetch.

Client는 checksum 검증 후 Cache 활성화.

Push를 사용해도:

```text
BALANCE_VERSION_CHANGED
```

알림만 전달한다.

실제 Balance Payload는 Push로 전달하지 않는다.

Push 유실 여부와 관계없이 Bootstrap/Run Start 전 Version을 확인한다.

---

<a id="source-6f5e52c4e100a74b-15-account-write-serialization"></a>
### 15. Account Write Serialization

D1의 pre-read Race Condition을 막기 위해:

```text
account_mutations
```

사용.

계정마다:

```text
sequence
```

단조 증가.

Critical Write는 최신 sequence=N을 읽고:

```text
N+1
```

INSERT를 Transaction 첫 statement로 실행한다.

두 요청이 동시에 동일한 N+1을 사용하면 한쪽은 UNIQUE 충돌로 전체 rollback.

실패한 요청은:

```text
State Reload
→ Validation
→ Cost 재계산
→ 새로운 Mutation으로 Retry
```

한다.

Mutation 번호만 바꿔서 Blind Retry 금지.

적용:

* Upgrade
* Training Core
* Preset
* Unlock
* Preview 생성
* Run Start
* Checkpoint
* Finish
* Reward
* Integrity 상태
* Enforcement
* Compensation Grant
* Ad Reward Grant
* Admin Player Correction

---

<a id="source-6f5e52c4e100a74b-16-authentication"></a>
### 16. Authentication

Beta:

**Guest Account 지원**

향후 Google Identity 연결 가능.

구조:

```text
accounts
auth_identities
auth_sessions
```

Refresh Token 원문 저장 금지.

저장:

```text
refresh_token_hash
family_id
generation
```

Refresh Token Rotation + Reuse Detection 지원.

Access Token:

* Signed
* Short TTL

Guest → Google 연결 시 기존 `account_id` 유지.

---

<a id="source-6f5e52c4e100a74b-17-bootstrap"></a>
### 17. Bootstrap

```text
GET /v1/bootstrap
```

한 번으로:

* Account
* Profile
* Currency/Resources
* Fuel
* Training Core
* Operators
* Train
* Carriages
* Turrets
* Commander Preset
* Unlock
* Tutorial
* Active Run
* Active Preview
* Compensation summary
* Active Balance Version
* Build compatibility

동기화.

Client Permanent Local Data는 Cache일 뿐 서버가 정답이다.

---

<a id="source-6f5e52c4e100a74b-18-permanent-economy-resource"></a>
### 18. Permanent Economy Resource

Canonical:

```text
CREDITS
MACHINE_PARTS
TACTICAL_DATA
SPECIAL_CORE
OPERATOR_EXP
```

`account_currencies`와 `currency_ledger`에 포함한다.

모든 Balance:

```text
INTEGER
>= 0
```

현재 Balance:

```text
account_currencies
```

역사:

```text
economy_transactions
currency_ledger
```

원칙:

```text
Balance
=
SUM(Ledger Delta)
```

초기 지급도 Balance를 직접 올리지 않고 Economy Transaction으로 처리한다.

Committed Ledger 수정/삭제 금지.

복구:

```text
Correction / Reversal Transaction
```

---

<a id="source-6f5e52c4e100a74b-19-idempotency"></a>
### 19. Idempotency

중요 Write:

* Run Start
* Run Finish
* Upgrade
* Training Core Change
* Reward
* Ad Claim
* Compensation
* 향후 Purchase/Gacha

Idempotency 필수.

`idempotency_records`는 성공한 committed operation만 저장.

Error 결과를 영구 Idempotency 결과로 저장하지 않는다.

Idempotency 하나만 믿지 않고 Domain UNIQUE도 반드시 둔다.

---

<a id="source-6f5e52c4e100a74b-20-fuel"></a>
### 20. Fuel

Fuel은 일반 Currency와 분리.

```text
fuel_state
fuel_ledger
```

Lazy Regeneration.

Cron으로 전 유저 Fuel Write 금지.

```text
last_regen_at
+
Server UTC
```

로 조회/소비 시 계산한다.

GET `/fuel` 때문에 Regen Write 금지.

Run Start 같은 실제 Write 순간에 Materialize.

Overflow 허용.

정상 Max 이상이면 자연 Regen만 정지.

---

<a id="source-6f5e52c4e100a74b-21-operator-permanent-progress--training-core"></a>
### 21. Operator Permanent Progress — Training Core

기존:

```text
모든 Operator가 개별 Permanent Level
Credits + Tactical Data Level Cost
```

구조는 폐기한다.

최종:

**Training Core 5 Slot + Sync Level**

---

<a id="source-6f5e52c4e100a74b-22-training-core"></a>
### 22. Training Core

계정:

```text
operator_training_core_slots
```

정확히 Slot Index:

```text
0
1
2
3
4
```

최대 5개.

Slot:

```text
slotIndex
operatorId
trainedLevel
investedOperatorExp
revision
lastMutationSequence
```

동일 Operator 중복 금지.

계정이 실제 보유한 Operator만 등록 가능.

Training Core 교체는 **Open Run이 없을 때만** 가능.

Open Run:

```text
ACTIVE
FINISH_PENDING
QUARANTINED
```

---

<a id="source-6f5e52c4e100a74b-23-operator-level-저장"></a>
### 23. Operator Level 저장

`account_operators`에서는 개별 Permanent `level`을 제거한다.

보존:

```text
operatorId
skillTraining
promotion
ownership metadata
revision
lastMutation
```

Training Core 밖 Operator에 Sync Level을 복제 저장하지 않는다.

---

<a id="source-6f5e52c4e100a74b-24-sync-level"></a>
### 24. Sync Level

5 Slot이 전부 채워진 경우:

```text
SyncLevel =
MIN(Training Core 5 trainedLevel)
```

Core Operator:

```text
EffectiveLevel = trainedLevel
```

Non-Core Operator:

```text
EffectiveLevel = SyncLevel
```

Sync 대상:

* Level 기반 HP
* Level 기반 ATK
* Heal Power
* Level 기반 Base Stat

Sync 제외:

* Skill Training
* Promotion
* Unlock
* Run Tactical Level
* Augment
* Skin

---

<a id="source-6f5e52c4e100a74b-25-training-core-초기-등록"></a>
### 25. Training Core 초기 등록

Tutorial 완료 후:

```text
Kyle
Reina
Eve
Sera
Mira
```

를 5 Slot에 자동 등록.

각:

```text
trainedLevel = 1
investedOperatorExp = 0
```

Sync 기능은 5 Slot 전부 채워진 경우 활성화.

---

<a id="source-6f5e52c4e100a74b-26-operator-exp"></a>
### 26. Operator EXP

Permanent Level:

```text
Operator EXP
```

만 사용.

Credits/Tactical Data Level Cost 폐기.

최종 성장 재화 역할:

```text
Permanent Operator Level
→ OPERATOR_EXP

Skill Training
→ Credits + Tactical Data

Promotion
→ Credits + Tactical Data + Special Core

Run Lv1~5
→ Tactical Supply

Commander Level
→ Commander XP
```

---

<a id="source-6f5e52c4e100a74b-27-operator-exp-reward"></a>
### 27. Operator EXP Reward

Versioned Balance Config:

```text
operatorExpByRisk

Risk I   700
Risk II  900
Risk III 1100
Risk IV  1300
Risk V   1500

tutorialOperatorExp = 500
```

Operation 종류는 Operator EXP에 영향 없음.

같은 Risk + 같은 결과는 같은 EXP.

Client가 Operator EXP Reward 값을 결정하지 않는다.

---

<a id="source-6f5e52c4e100a74b-28-실패-run-operator-exp"></a>
### 28. 실패 Run Operator EXP

Reference:

```text
FailureExp =
FullClearExp
× 0.8
× ClearedWaves / 15
```

정수 계산:

```text
floor(
  FullClearExp
  × 8000
  × ClearedWaves
  / 10000
  / 15
)
```

Final Clear:

100%.

현재 공식은 그대로 구현한다.

---

<a id="source-6f5e52c4e100a74b-29-training-core-exp-curve"></a>
### 29. Training Core EXP Curve

Balance Config:

```text
levelExpCurve
```

관리.

Reference 누적:

```text
Lv10  600
Lv20  1,800
Lv30  4,000
Lv40  7,000
Lv50 11,000
```

5명 전체:

```text
3,000
9,000
20,000
35,000
55,000
```

정확한 Level별 Curve는 Balance Config가 Source of Truth.

---

<a id="source-6f5e52c4e100a74b-30-exp-refund-정책"></a>
### 30. EXP Refund 정책

**실제 소비한 Operator EXP를 추적하는 방식으로 구현한다.**

```text
investedOperatorExp
```

를 Slot에 저장.

Training Core에서 Operator 제거:

```text
OPERATOR_EXP
+= investedOperatorExp
```

100% 반환.

Curve Patch로 Level Cost가 변경돼도 과거 투자량을 현재 Curve로 역산하지 않는다.

실제로 사용한 EXP를 반환한다.

Skill/Promotion Reset 금지.

새 Operator에게 Refund EXP 자동투자 금지.

새 Core Operator:

```text
trainedLevel = 1
investedOperatorExp = 0
```

---

<a id="source-6f5e52c4e100a74b-31-training-core-level-up"></a>
### 31. Training Core Level Up

Server가:

* 현재 Level
* Balance Version
* Level EXP Curve
* OPERATOR_EXP Balance

로 계산한다.

Client가 Cost를 보내더라도 신뢰하지 않는다.

Transaction:

```text
account_mutation

OPERATOR_EXP 차감
currency_ledger

training_core_slot level 증가
investedOperatorExp 증가

progression_ledger

idempotency
```

원자 처리.

---

<a id="source-6f5e52c4e100a74b-32-sync-up-api"></a>
### 32. SYNC UP API

예:

```text
POST /v1/operators/training-core/sync-up
```

동작:

```text
가장 낮은 trainedLevel Slot 선택

동률이면 낮은 slotIndex 우선

다음 Level 비용 조회

EXP 충분하면 Level Up

다시 반복

EXP 부족 또는 Max Level이면 종료
```

부분 EXP를 Slot에 저장하지 않는다.

남은 EXP는 Account OPERATOR_EXP Balance에 그대로 둔다.

---

<a id="source-6f5e52c4e100a74b-33-신규-operator"></a>
### 33. 신규 Operator

신규 Operator 획득:

```text
account_operators INSERT

Skill Training = 1
Promotion = 0
```

Permanent Level Initialization Cost 없음.

Core 밖이면:

```text
EffectiveLevel = SyncLevel
```

즉 Sync 37 상태에서 Vex 획득:

```text
Vex Lv37 SYNC
Skill 1
Promotion 0
```

---

<a id="source-6f5e52c4e100a74b-34-skill--promotion-비용"></a>
### 34. Skill / Promotion 비용

기존 Reference 유지.

Skill 1→5 총:

```text
Credits 3,040
Tactical Data 1,520
```

Promotion 0→III 총:

```text
Credits 4,160
Tactical Data 2,080
Special Core 3
```

Operator Permanent Level에서 Credits/Tactical Data 사용 금지.

---

<a id="source-6f5e52c4e100a74b-35-risk-permanent-spec"></a>
### 35. Risk Permanent Spec

출전 Operator 평균 Permanent Level 기준 폐기.

공식 Operator Permanent Level Spec:

```text
Sync Level
```

초기 Reference:

```text
Risk I     Sync 8~10
Risk II    Sync 16~20
Risk III   Sync 26~30
Risk IV    Sync 36~40
Risk V     Sync 46~50
```

Skill/Promotion은 별도 Spec Parameter.

---

<a id="source-6f5e52c4e100a74b-36-training-core와-deployment"></a>
### 36. Training Core와 Deployment

Training Core 5명과 실제 Squad 5명은 동일할 필요 없다.

Training Core 여부를 Deployment 조건으로 사용하지 않는다.

Deployment는 항상 최대 **5인 Squad**.

---

<a id="source-6f5e52c4e100a74b-37-operation-preview"></a>
### 37. Operation Preview

흐름:

```text
Objective / Risk 선택
↓
POST /v1/operations/preview
↓
Server
Region
Faction
Environment
Contract 후보
↓
Deployment
↓
POST /v1/runs/start
```

Preview:

* Fuel 미소비
* Balance Version 고정
* Generator Version 고정
* Seed 저장
* Generated Result가 Source of Truth
* TTL
* Account당 ACTIVE 최대 1
* 유효 ACTIVE Preview가 있으면 새 Preview 생성 금지
* Objective/Risk를 바꿔도 무료 리롤 금지
* Player Build 기반 Counter Preview 금지

만료는 Lazy Materialization.

---

<a id="source-6f5e52c4e100a74b-38-run-start"></a>
### 38. Run Start

Run Start Transaction:

```text
Idempotency
Account Mutation
Account/Open Run
Preview
Contract
Deployment
Client Build
Content Compatibility
Fuel
검증

Fuel Materialize/Consume

Run 생성

RunSnapshot 생성

WavePlan 생성

AugmentPlan 생성

Preview → CONSUMED

runtime.active_preview = NULL
runtime.active_run = Run

Idempotency
```

하나라도 실패하면 전체 rollback.

---

<a id="source-6f5e52c4e100a74b-39-run-balance-version"></a>
### 39. Run Balance Version

Run은:

```text
Preview.balance_config_version
```

을 그대로 계승한다.

금지:

```text
Run Start 때 최신 activeVersion으로 바꾸기
```

Run:

```text
balance_config_version
```

끝까지 고정.

진행 중 Balance Publish가 발생해도 현재 Run에는 영향 없음.

Reward와 Integrity 역시 **Run Balance Version 기준**.

---

<a id="source-6f5e52c4e100a74b-40-run-snapshot"></a>
### 40. Run Snapshot

출전 5명의 실제 Effective Level을 Snapshot으로 고정.

```text
operators:
[
  {
    operatorId,
    effectiveLevel,
    levelSource: TRAINED | SYNC,
    skillTrainingLevel,
    promotionTier
  }
]

operatorSyncLevel
```

Run 이후:

* Training Core 교체
* Core Level 증가
* Sync Level 증가
* Permanent Upgrade

현재 Run에 반영 금지.

---

<a id="source-6f5e52c4e100a74b-41-waveplan"></a>
### 41. WavePlan

전체 **15 Wave**를 Run Start에서 Server 생성.

Generated WavePlan이 Source of Truth.

Seed는 Audit/Reproduction 용.

Player Build를 보고 실시간 Counter Enemy/Wave 생성 금지.

Run 시작 후 Plan 수정 금지.

---

<a id="source-6f5e52c4e100a74b-42-augmentplan--offline-continue"></a>
### 42. AugmentPlan / Offline Continue

Offline Continue를 위해 Run Start에서 Augment 후보 흐름도 사전 승인한다.

```text
augment_plan_json
augment_generator_version
augment_seed
```

저장.

Offline 가능:

* Battle
* Tactical Upgrade
* Redeploy
* Command
* Ultimate
* 사전 승인 Augment

Offline 불가:

* 광고 Reroll
* Permanent Upgrade
* Fuel Claim
* 새 Run
* 최종 Reward 확정

---

<a id="source-6f5e52c4e100a74b-43-checkpoint"></a>
### 43. Checkpoint

Major Server Checkpoint:

```text
Wave 3
Wave 6
Wave 9
Wave 12
Wave 15
```

순서 강제.

Offline에서 앞서가도 재접속 시 순서대로 제출.

Canonical Accepted Checkpoint만:

```text
run_checkpoints
```

저장.

Rejected 제출은 Integrity Evidence.

Checkpoint 승인도 Account Mutation.

---

<a id="source-6f5e52c4e100a74b-44-checkpoint-제출-build"></a>
### 44. Checkpoint 제출 Build

Run 시작 Build와 실제 제출 Build를 모두 보존.

```text
runs.client_build_id
run_checkpoints.submission_client_build_id
run_final_results.submission_client_build_id
```

앱 업데이트 후 Run Resume한 경우도 재현 가능해야 한다.

---

<a id="source-6f5e52c4e100a74b-45-run-status"></a>
### 45. Run Status

```text
ACTIVE
FINISH_PENDING
FINISHED
FAILED
ABANDONED
EXPIRED
INVALID
QUARANTINED
```

`FAILED`:

정상 게임 패배.

Server Error 의미 아님.

Open Run:

```text
ACTIVE
FINISH_PENDING
QUARANTINED
```

계정당 최대 1개.

---

<a id="source-6f5e52c4e100a74b-46-run-status-history"></a>
### 46. Run Status History

현재 상태:

```text
runs.status
```

역사:

```text
run_status_history
```

append-only.

예:

```text
NULL → ACTIVE
ACTIVE → QUARANTINED
QUARANTINED → ACTIVE
ACTIVE → FINISHED
```

과거 Quarantine 이력을 잃지 않는다.

---

<a id="source-6f5e52c4e100a74b-47-run-finish"></a>
### 47. Run Finish

Client 제출:

* Outcome
* Wave Summary
* Tactical Summary
* Damage/Action Summary
* Final State

Client가 보내지 않는 것:

* CreditsEarned
* PartsEarned
* TacticalDataEarned
* SpecialCoreEarned
* OperatorExpEarned
* CommanderXP

Reward는 서버가 계산한다.

---

<a id="source-6f5e52c4e100a74b-48-reward"></a>
### 48. Reward

Run Reward:

```text
Wave
Final
Operation
Contract
Boss
First Clear
Achievement
Operator EXP
```

Server에서 계산.

Reward는:

```text
runs.balance_config_version
```

기준.

현재 Active Balance 사용 금지.

---

<a id="source-6f5e52c4e100a74b-49-result-ad-50"></a>
### 49. Result Ad +50%

광고 증가 대상:

```text
CREDITS
MACHINE_PARTS
TACTICAL_DATA
```

초기 정책상 광고 증가 제외:

```text
SPECIAL_CORE
OPERATOR_EXP
```

즉:

```text
adEligible SPECIAL_CORE = 0
adEligible OPERATOR_EXP = 0
```

DB/Application 양쪽에서 검증.

---

<a id="source-6f5e52c4e100a74b-50-ad-reward-ssv"></a>
### 50. Ad Reward SSV

Client 광고 완료 callback만으로 지급 금지.

흐름:

```text
Server Ad Intent
↓
Signed custom_data
↓
Rewarded Ad
↓
Provider SSV
↓
Provider Signature Verify
↓
LAST RAIL Binding Verify
↓
Business Validation
↓
VERIFIED
↓
Account Mutation
↓
Economy Grant
↓
GRANTED
```

Provider `reward_amount`를 LAST RAIL Currency 값으로 사용 금지.

Bonus:

```text
floor(base × 5000 / 10000)
```

Provider Transaction ID Replay 방지.

Base Reward당 Result Bonus 최대 1회.

Invalid Provider Signature나 invalid LAST RAIL Binding 요청으로 실제 Intent를 REJECT시키지 않는다.

---

<a id="source-6f5e52c4e100a74b-51-false-positive-후-광고"></a>
### 51. False Positive 후 광고

Validator 오탐/Server Defect로 Run Reward가 늦게 복원된 경우:

**정상 Run Reward 생성 시각부터 새로운 Ad Eligibility Window를 부여한다.**

서버 문제 때문에 광고 기회를 잃지 않게 한다.

Compensation 자체에는 Result Ad를 붙이지 않는다.

---

<a id="source-6f5e52c4e100a74b-52-integrity--anti-cheat"></a>
### 52. Integrity / Anti-Cheat

Frame 전체 재시뮬레이션 금지.

Validator 카테고리:

```text
PROTOCOL
PROGRESS
SUPPLY
UPGRADE
AUGMENT
ACTION
ULTIMATE
COMMAND
OPERATOR_DAMAGE
TURRET_DAMAGE
ENEMY_DAMAGE
BOSS
REWARD
```

판정:

```text
PASS
SOFT_ANOMALY
HARD_VIOLATION
```

PASS는 원칙적으로 Integrity Event 생성하지 않음.

---

<a id="source-6f5e52c4e100a74b-53-integrity-evidence"></a>
### 53. Integrity Evidence

```text
integrity_events
```

는 immutable.

기록:

* Run
* Balance Version
* Run Start Build
* Submission Build
* Original Validator
* Runtime Revision
* Evaluated Validator
* Evidence Code
* Evidence
* Fingerprint
* Classification

최초 HARD/SOFT 판정을 나중에 UPDATE해서 없애지 않는다.

---

<a id="source-6f5e52c4e100a74b-54-integrity-resolution"></a>
### 54. Integrity Resolution

별도:

```text
integrity_event_resolutions
integrity_event_review_states
```

사용.

Resolution:

```text
CONFIRMED_VIOLATION
FALSE_POSITIVE
SERVER_DEFECT
CLIENT_DEFECT
BUG_EXPLOITATION
RESOLVED_NO_ACTION
```

Revalidation은 새로운 Validator Version/Runtime Revision으로 별도 기록.

과거 Original Detection 보존.

---

<a id="source-6f5e52c4e100a74b-55-enforcement"></a>
### 55. Enforcement

Observation:

```text
NORMAL
WATCHED
```

실제 제재:

```text
REWARD_RESTRICTED
TEMP_SUSPENDED
PERMANENT_BAN
```

분리.

HARD_VIOLATION != Permanent Ban.

Gameplay DPS 하나만으로 자동 Permanent Ban 금지.

DB에서도:

```text
AUTOMATED + PERMANENT_BAN
```

생성 차단.

Permanent Ban:

OWNER + Admin Confirmation.

---

<a id="source-6f5e52c4e100a74b-56-false-positive-복구"></a>
### 56. False Positive 복구

순서:

```text
Integrity Resolution
↓
Run/Watch/Enforcement 복구
↓
원래 Run Reward 복원
↓
First Clear 복원
↓
필요하면 추가 Compensation
```

원래 Run Reward와 Compensation을 혼동하지 않는다.

---

<a id="source-6f5e52c4e100a74b-57-compensation"></a>
### 57. Compensation

```text
compensations
compensation_recipients
```

Campaign과 Recipient 분리.

DRAFT:

편집.

PREPARED:

* Reward Spec Freeze
* Recipient Population Freeze
* Recipient Snapshot Hash

EXECUTING:

Async Grant.

COMPLETED:

Pending 0.

CANCELLED:

새 Grant 시작 중단.

---

<a id="source-6f5e52c4e100a74b-58-compensation-grant"></a>
### 58. Compensation Grant

실제 지급 순간만 Account Mutation.

같은 Mutation으로:

```text
Currency
Fuel
Recipient GRANTED
```

을 묶는다.

Retry-safe.

PROCESSING 상태를 DB에 영속화하지 않는다.

기술 실패:

```text
PENDING 유지
attempt_count 증가
```

---

<a id="source-6f5e52c4e100a74b-59-compensation과-operator-exp"></a>
### 59. Compensation과 Operator EXP

Compensation Resource Shape에서도:

```text
OPERATOR_EXP
```

지원.

단 Compensation은 Balance Version에 따라 계산되는 게 아니라 운영 결정 자체가 Source of Truth.

---

<a id="source-6f5e52c4e100a74b-60-admin-jobs--queue"></a>
### 60. Admin Jobs / Queue

무거운 작업:

* Compensation Prepare/Execute
* Integrity Revalidation
* Economy Reconciliation
* Bulk Analysis
* Aggregate
* Alert

는:

```text
admin-api
↓
admin_jobs
↓
Queue
↓
async-worker
```

사용.

RUNNING Job은:

```text
lease_owner
lease_expires_at
```

필수.

Worker 죽으면 lease 만료 후 다른 Worker가 회수 가능.

Job Progress JSON은 Materialized Summary일 뿐 Domain Source of Truth가 아니다.

---

<a id="source-6f5e52c4e100a74b-61-compensation-cancel"></a>
### 61. Compensation Cancel

Cancel:

```text
새 Recipient Grant 시작 금지
```

의미.

이미 실제 D1 Grant Transaction에 진입한 in-flight 작업은 Commit될 수 있다.

기존 GRANTED 지급 자동 Reversal 금지.

---

<a id="source-6f5e52c4e100a74b-62-analytics"></a>
### 62. Analytics

Core D1과 Raw Analytics 분리.

Server-generated:

* Run Result
* Reward
* Upgrade
* Fuel
* Integrity
* Ad Result

Unity Telemetry:

* FPS
* UI
* Tutorial UX
* Network
* SDK Error
* Ad Load/Show Error

Enemy Attack 1회마다 Telemetry 전송 금지.

Wave/Run Summary 중심.

---

<a id="source-6f5e52c4e100a74b-63-body-limit"></a>
### 63. Body Limit

초기 정책:

```text
일반 API
32 KB

Telemetry
64 KB

Checkpoint / Finish
128 KB
```

플랫폼 최대치를 그대로 허용하지 않는다.

추가로:

* 배열 길이
* 문자열 길이
* JSON depth

제한한다.

---

<a id="source-6f5e52c4e100a74b-64-rate-limit--ddos"></a>
### 64. Rate Limit / DDoS

구조:

```text
Internet
↓
Cloudflare DDoS
↓
WAF
↓
Rate Limit
↓
Worker
↓
Account Rate Limit
↓
Body/Auth/Schema
↓
D1
```

가능하면 Worker CPU를 사용하기 전에 차단.

---

<a id="source-6f5e52c4e100a74b-65-protection-mode"></a>
### 65. Protection Mode

Server Runtime Config:

```text
NORMAL
ELEVATED
HIGH
LOCKDOWN
```

공격 시 우선 제한:

```text
Telemetry
↓
Guest Creation
↓
Preview
↓
New Run
```

기존 Run Finish/Reward는 최대한 보호한다.

---

<a id="source-6f5e52c4e100a74b-66-migration-원칙"></a>
### 66. Migration 원칙

Migration:

```text
0001_initial.sql
0002_economy.sql
0003_progression.sql
0004_operation.sql
0005_run.sql
0006_integrity.sql
0007_compensation.sql
0008_admin.sql
0009_ads.sql
0010_indexes.sql
```

Git 관리.

Production 직접 SQL 수정 금지.

원칙:

```text
Expand
→ Migrate
→ Contract
```

현재 아직 정식 서비스 이전이라면 기존 개발 Schema보다 이 Source of Truth를 우선한다.

---

<a id="source-6f5e52c4e100a74b-67-주요-d1-영역"></a>
### 67. 주요 D1 영역

최종 최소 테이블 책임:

```text
accounts
auth_identities
auth_sessions

admin_principals

player_profiles
account_runtime_state
account_mutations
account_tutorial_state

balance_config_drafts
balance_config_publish_claims
balance_config_versions
balance_config_activations
balance_config_active

client_builds

validator_versions
validator_runtime_configs
validator_runtime_active

account_currencies
economy_transactions
currency_ledger
fuel_state
fuel_ledger
reward_transactions
idempotency_records

account_operators
operator_training_core_slots
account_train
account_carriages
account_turrets
commander_presets
account_unlocks
progression_ledger
account_first_clears

operation_previews

runs
run_status_history
run_snapshots
run_wave_plans
run_checkpoints
run_wave_results
run_augment_history
run_tactical_ledger
run_final_results

integrity_events
integrity_event_review_states
integrity_event_resolutions
integrity_incidents
integrity_incident_events
account_integrity_state
account_enforcements

compensations
compensation_recipients

admin_confirmations
admin_audit_logs
admin_jobs

ad_reward_transactions
```

`account_daily_state` 같은 기능은 구체적 Daily Rule이 확정되지 않았으므로 임의 구현하지 않는다.

---

<a id="source-6f5e52c4e100a74b-68-api-기본-구조"></a>
### 68. API 기본 구조

비버전:

```text
GET /status
```

Game:

```text
/v1/*
```

최소:

```text
POST /v1/auth/guest
POST /v1/auth/refresh

GET /v1/bootstrap

GET /v1/balance-config/:version

GET /v1/profile
GET /v1/profile/display-name/check
POST /v1/profile/display-name
GET /v1/economy
GET /v1/fuel

GET /v1/operators
POST /v1/operators/training-core/:slot/level-up
POST /v1/operators/training-core/sync-up
POST /v1/operators/training-core/:slot/replace

POST /v1/operations/preview

POST /v1/runs/start
POST /v1/runs/:id/checkpoints
POST /v1/runs/:id/finish

POST /v1/ads/result-bonus/prepare

POST /v1/telemetry/batch
```

Provider:

```text
/v1/provider-callbacks/admob/rewarded
```

Admin API는 Game API와 별도 Worker.

Launch run start requires `Authorization: Bearer <access-token>` and
`Idempotency-Key: <client-generated-key>`. Replays are scoped to the
authenticated account plus operation `run_start`.

---

<a id="source-6f5e52c4e100a74b-69-server-runtime-config"></a>
### 69. Server Runtime Config

Balance가 아닌 운영 설정:

* Preview TTL
* Run Expiration
* Confirmation TTL
* Ad Eligibility TTL
* SSV Callback Grace
* Rate Limit
* Protection Mode
* Maintenance
* Body Limits

는 Balance Config에 넣지 않는다.

별도 Server Runtime Config 책임으로 관리한다.

---

<a id="source-6f5e52c4e100a74b-70-balance-simulation-tool"></a>
### 70. Balance Simulation Tool

Admin/Server 기능으로 설계한다.

Unity Editor가 Source of Truth가 아니다.

Draft 또는 Published Balance 대상으로 대량 Simulation 지원.

축:

```text
Risk

Permanent Spec Band
UNDER_SPEC
CHALLENGE_SPEC
RECOMMENDED_SPEC
COMFORT_SPEC

Augment Quality
AUGMENT_POOR
STANDARD
STRONG
HIGHROLL

Tactical Build Quality
TACTICAL_POOR
STANDARD
OPTIMAL

Representative Build
Balanced
Turret Firepower
Operator Strike
Anti-Boarding
Fortress
Greedy

다수 RNG Seed
```

주요 Acceptance Reference:

```text
RECOMMENDED + STANDARD
75~85%

COMFORT + POOR
100%
```

COMFORT 100% 기준은 Non-Sabotage Simulation Policy.

Balance가 목표에서 벗어나면 Permanent Spec Curve를 쉽게 바꾸기보다:

```text
Enemy
Wave
Threat
Hazard
Boss
```

측을 우선 조정하는 운영 원칙.

Simulation 결과:

* Clear Rate
* Failure Wave
* KPI
* Before/After
* Validation
* Stored Result

를 Admin에서 조회 가능하도록 서버 구조를 분리해 둔다.

이 Phase는 Core Game API가 안정된 뒤 구현한다.

---

<a id="source-6f5e52c4e100a74b-71-testing-필수"></a>
### 71. Testing 필수

모든 Domain Service는 Unit Test.

D1 Integration Test:

```text
Migration 전체 적용

PRAGMA foreign_key_check

PRAGMA quick_check
```

필수.

대표 Query:

```text
Bootstrap Open Run
Active Preview
Idempotency
Reward Duplicate
Checkpoint Retry
Integrity Lookup
Compensation Pending Batch
Admin Job Lease
Ad Provider Transaction
```

은:

```text
EXPLAIN QUERY PLAN
```

으로 Index 사용 확인.

불필요한 Index를 무작정 추가하지 않는다.

---

<a id="source-6f5e52c4e100a74b-72-race-condition-test"></a>
### 72. Race Condition Test

최소 다음 경쟁을 자동 테스트한다.

```text
Operator Level Up vs Level Up

Run Start vs Operator Level Up

Run Start vs Training Core Replace

Preview Create vs Preview Create

Checkpoint vs Checkpoint Retry

Checkpoint 15 vs Finish

Reward Retry

Compensation Retry

Ad SSV Retry

Admin Confirmation Retry

Balance Publish vs Publish

Balance Publish vs Rollback
```

한 요청만 Commit되고 다른 요청은 Reload/Recompute하도록 검증.

---

<a id="source-6f5e52c4e100a74b-73-cloudflare-local-검증"></a>
### 73. Cloudflare Local 검증

Dependency 설치 후:

```text
pnpm install
```

Wrangler Version 확인.

Local D1:

```text
0001~0010 migration apply
```

후:

```text
PRAGMA foreign_key_check
PRAGMA quick_check
```

Type:

```text
wrangler types
```

TypeScript:

```text
typecheck
```

Tests:

```text
unit
integration
```

Worker:

```text
deploy --dry-run
```

전부 수행한다.

---

<a id="source-6f5e52c4e100a74b-74-dev-cloudflare-연결"></a>
### 74. DEV Cloudflare 연결

Cloudflare 인증이 이미 되어 있거나 내가 로그인 승인을 완료한 경우에만 실제 DEV를 연결한다.

먼저:

```text
wrangler whoami
```

확인.

D1:

```text
lastrail-core-dev
```

생성.

반환된 실제 database_id를:

```text
game-api DEV
admin-api DEV
```

둘 다 같은 DEV Core DB에 연결한다.

단 DEV/PRD와 공유 금지.

DEV Remote Migration 적용.

Remote:

```text
foreign_key_check
quick_check
```

확인.

그다음:

```text
dev-api.lastrail.app
dev-admin.lastrail.app
```

연결을 준비한다.

---

<a id="source-6f5e52c4e100a74b-75-cloudflare-access-dev-admin"></a>
### 75. Cloudflare Access DEV Admin

`dev-admin.lastrail.app`도 가능하면 Cloudflare Access 보호.

허용 대상은 초기에는 내 계정만.

Production Access/MFA 구조와 동일한 방식.

Access 설정을 자동 수행할 권한/API가 없다면 임의 우회하지 말고:

```text
Cloudflare Dashboard에서 필요한 정확한 설정 절차
```

를 문서화한다.

---

<a id="source-6f5e52c4e100a74b-76-prd-금지선"></a>
### 76. PRD 금지선

내가 별도로 승인하기 전:

```text
lastrail-core-prd 생성

prd 배포

api.lastrail.app PRD 연결

admin.lastrail.app PRD 연결

PRD Balance Publish

PRD D1 Mutation
```

실행 금지.

Config와 문서만 준비한다.

---

<a id="source-6f5e52c4e100a74b-77-backup--migration-운영"></a>
### 77. Backup / Migration 운영

Production 도입 시:

* Cloudflare D1 복구 기능 활용
* Migration 전 Restore Point/Backup 확보
* 별도 R2 Backup 준비
* Daily / Weekly / Monthly 보존 정책 문서화

Production DB 전체 Restore는 최후수단.

경제 사고는:

```text
Ledger
Correction
Reversal
Compensation
```

을 우선한다.

---

<a id="source-6f5e52c4e100a74b-78-secret"></a>
### 78. Secret

Git에 Secret 절대 저장 금지.

로컬:

```text
.dev.vars
```

Production:

```text
Workers Secrets / Secrets Store
```

사용.

예:

```text
AUTH_SIGNING_KEY_CURRENT
AUTH_SIGNING_KEY_PREVIOUS

AD_BINDING_KEY_CURRENT
AD_BINDING_KEY_PREVIOUS
```

Signing Key Rotation을 고려한다.

---

<a id="source-6f5e52c4e100a74b-79-구현-phase"></a>
### 79. 구현 Phase

아래 순서로 진행한다.

<a id="source-6f5e52c4e100a74b-phase-1--repositoryfoundation"></a>
#### Phase 1 — Repository/Foundation

* Monorepo
* pnpm
* TypeScript
* Hono
* Wrangler
* ESLint
* Test setup
* Environment structure
* Common Result/Error/Logger/ULID/CanonicalJSON

<a id="source-6f5e52c4e100a74b-phase-2--d1-schema"></a>
#### Phase 2 — D1 Schema

* 0001~0010
* Training Core 반영
* OPERATOR_EXP 반영
* 전체 FK
* Index
* Local D1 검증

<a id="source-6f5e52c4e100a74b-phase-3--auth"></a>
#### Phase 3 — Auth

* Guest
* Refresh
* Access Token
* Refresh Rotation
* Bootstrap Admin Owner Script

<a id="source-6f5e52c4e100a74b-phase-4--balance"></a>
#### Phase 4 — Balance

* Draft
* Validation
* Publish
* Rollback
* Fetch API
* Content Version Gate

<a id="source-6f5e52c4e100a74b-phase-5--bootstrap--permanent-state"></a>
#### Phase 5 — Bootstrap / Permanent State

* Profile
* Economy
* Fuel
* Operators
* Training Core
* Train
* Turret
* Unlock
* Tutorial

<a id="source-6f5e52c4e100a74b-phase-6--progression"></a>
#### Phase 6 — Progression

* Core Level Up
* Core Replace
* Refund
* SYNC UP
* Skill
* Promotion
* Economy linkage

<a id="source-6f5e52c4e100a74b-phase-7--operation-preview"></a>
#### Phase 7 — Operation Preview

* Preview Generator
* TTL
* Anti-reroll
* Balance freeze

<a id="source-6f5e52c4e100a74b-phase-8--run-start"></a>
#### Phase 8 — Run Start

* Fuel
* Snapshot
* Effective Levels
* WavePlan
* AugmentPlan
* Account Mutation
* Version freeze

<a id="source-6f5e52c4e100a74b-phase-9--checkpoint--offline-continue"></a>
#### Phase 9 — Checkpoint / Offline Continue

* Checkpoint 3/6/9/12/15
* Wave Results
* Tactical Ledger
* Augment History
* Retry

<a id="source-6f5e52c4e100a74b-phase-10--finish--reward"></a>
#### Phase 10 — Finish / Reward

* Victory/Failure
* Operator EXP
* First Clear
* Reward
* FINISH_PENDING
* Run status history

<a id="source-6f5e52c4e100a74b-phase-11--integrity"></a>
#### Phase 11 — Integrity

* Fast Validator
* DPS Envelope
* Deterministic Validators
* Events
* Quarantine
* Resolution
* Revalidation
* Enforcement

<a id="source-6f5e52c4e100a74b-phase-12--ads"></a>
#### Phase 12 — Ads

* Ad Intent
* AdMob SSV
* Signed custom_data
* +50%
* Replay protection
* Retry

<a id="source-6f5e52c4e100a74b-phase-13--compensation"></a>
#### Phase 13 — Compensation

* Campaign
* Recipients
* Async Grant
* False Positive restoration path

<a id="source-6f5e52c4e100a74b-phase-14--admin-api"></a>
#### Phase 14 — Admin API

* Principal
* RBAC
* Confirmation
* Audit
* Jobs
* Balance operations
* Integrity
* Compensation

<a id="source-6f5e52c4e100a74b-phase-15--analytics--queue"></a>
#### Phase 15 — Analytics / Queue

* Analytics Engine
* Telemetry batch
* Queue
* Async Worker

<a id="source-6f5e52c4e100a74b-phase-16--cloudflare-security"></a>
#### Phase 16 — Cloudflare Security

* Access
* MFA
* WAF
* Rate Limits
* Body Limits
* Protection Mode

<a id="source-6f5e52c4e100a74b-phase-17--balance-simulation-tool"></a>
#### Phase 17 — Balance Simulation Tool

* Simulation profile
* Seeds
* Acceptance Matrix
* Stored results
* Draft/Published comparison

---

<a id="source-6f5e52c4e100a74b-80-구현-방식"></a>
### 80. 구현 방식

각 Phase는:

```text
Failing Test
↓
Implementation
↓
Unit Test
↓
Integration Test
↓
Typecheck
↓
Lint
↓
Review
↓
Commit
```

순으로 진행한다.

큰 Phase는 작은 Commit으로 나눈다.

기존 테스트가 깨지면 그냥 무시하지 말고 원인을 분석한다.

---

<a id="source-6f5e52c4e100a74b-81-금지사항"></a>
### 81. 금지사항

절대 하지 말 것:

```text
Unity ScriptableObject를 Balance 최종 Source of Truth로 사용

Published Balance 직접 수정

Published Balance 삭제

Run 진행 중 Balance Version 교체

Client Cost 신뢰

Client Reward 신뢰

Client Operator EXP Reward 신뢰

Route에서 SQL 직접 실행

Route에서 Balance 직접 수정

Route에서 Reward 계산

Account Currency 직접 UPDATE만 하고 Ledger 생략

Player Build 기반 실시간 Counter Enemy 생성

Raw Combat Frame 서버 재시뮬레이션

Admin 직접 ID/Password Auth 새로 구현

Admin Console에서 D1 직접 접근

Secret Git 저장

DEV/PRD DB 공유

Production 직접 SQL 수정

내 승인 없는 Production 배포
```

---

<a id="source-6f5e52c4e100a74b-82-문서"></a>
### 82. 문서

반드시 작성:

```text
README.md

docs/architecture/server.md
docs/architecture/data-ownership.md

docs/api/game-api.md
docs/api/admin-api.md

docs/run-protocol/run-protocol.md

docs/cloudflare/setup.md
docs/cloudflare/environments.md
docs/cloudflare/admin-access.md

docs/balance/versioned-balance.md
docs/balance/training-core.md

docs/integrity/integrity.md

docs/operations/backup-recovery.md
```

---

<a id="source-6f5e52c4e100a74b-83-완료-조건"></a>
### 83. 완료 조건

서버 전체 작업 완료라고 보고하려면 최소:

```text
pnpm install 성공

전체 TypeScript typecheck 통과

unit tests 통과

integration tests 통과

0001~0010 Local D1 Migration 통과

foreign_key_check clean

quick_check ok

P0 Query Plan index 확인

Game API /status 정상

Admin API /status 정상

Guest Auth 정상

Bootstrap 정상

Balance Draft/Publish/Rollback 테스트

Training Core / Sync Level 테스트

Operator EXP Level Up/Refund/SYNC UP 테스트

Operation Preview 테스트

Run Start 테스트

Offline Checkpoint 순서 테스트

Run Finish Reward 테스트

False Positive 복구 테스트

Ad Duplicate/SSV 테스트

Compensation Retry 테스트

Admin RBAC/Confirmation/Audit 테스트

wrangler deploy --dry-run 통과
```

가 있어야 한다.

---

<a id="source-6f5e52c4e100a74b-84-최초-작업-시작"></a>
### 84. 최초 작업 시작

지금 저장소가 비어 있다면 다음부터 바로 시작한다.

```text
1. Repository 상태 검사

2. 위 Source of Truth를
   docs/architecture/server.md에 정리

3. pnpm Monorepo 생성

4. game-api/admin-api/async-worker 생성

5. packages 구조 생성

6. 테스트 환경 구성

7. 0001~0010 Migration 작성

8. Local D1 전체 검증

9. 그다음 Auth/Bootstrap부터 구현
```

먼저 전체 저장소를 확인하고 **Phase 1 → Phase 2부터 실제 구현을 시작해라.**

내가 이미 확정한 위 요구사항을 다시 설계 질문으로 되돌리지 말 것.

다만 Cloudflare 로그인, Domain 인증, 결제/외부 계정 권한처럼 내 직접 승인이 필요한 경우만 작업을 중단하고 필요한 행동을 정확히 알려줘.

각 Phase 완료 때 실제 테스트 증거를 포함하여 보고하고 다음 Phase로 계속 진행해라.

<!-- source-body:source-6f5e52c4e100a74b:end -->


<a id="source-c44fa2775c2bfd30"></a>
## Training Core

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/balance/training-core.md`  
원문 SHA-256: `f4c76b0e674731907225fd14f17ab1a77ca9078bfddcead19f0443545b6bcab7`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-c44fa2775c2bfd30:start -->
<a id="source-c44fa2775c2bfd30-training-core"></a>
### Training Core

Operator permanent levels use Training Core 5 slots and OPERATOR_EXP. Slots 0 through 4 are unique per account and cannot contain duplicate operators.

Sync Level is enabled only when all 5 slots are filled and equals the minimum trained level. Removing a core operator refunds the stored `invested_operator_exp` exactly.

<!-- source-body:source-c44fa2775c2bfd30:end -->


<a id="source-de8c86b812b01d1c"></a>
## Versioned Balance

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/balance/versioned-balance.md`  
원문 SHA-256: `810e84453039b8070438ab61b3f12319788cf95885a4ae5da0ddb695e0db3236`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-de8c86b812b01d1c:start -->
<a id="source-de8c86b812b01d1c-versioned-balance"></a>
### Versioned Balance

Published balance versions are immutable. Admin edits happen only in drafts. Publish validates expected revision and checksum, inserts a new version, records activation history, and moves the active pointer atomically.

Rollback never edits a version; it changes the active pointer and appends activation history.

<a id="source-de8c86b812b01d1c-draft-creation"></a>
#### Draft Creation

Admin draft creation writes only to `balance_config_drafts`.

- OWNER and OPERATOR can create drafts.
- VIEWER cannot create drafts.
- The server canonicalizes the config JSON and computes the checksum.
- Draft creation does not publish, activate, or modify published versions.

<a id="source-de8c86b812b01d1c-draft-publish"></a>
#### Draft Publish

Admin draft publish writes the first operational versioned-balance flow:

- OWNER only.
- Request body must provide `expectedRevision` and `expectedChecksum`.
- A short-lived confirmation token must be created for the same publish payload before execute.
- The raw confirmation token is returned once and only its hash is stored.
- The draft must still be `DRAFT`.
- The expected revision and checksum must match the stored draft.
- During beta, `draft.baseVersion` must equal the current `balance_config_active.version`.

Publish success means publish and activate in the same operation:

- Consume the matching `admin_confirmations` row.
- Insert a new immutable `balance_config_versions` row.
- Insert `balance_config_publish_claims`.
- Mark the draft `PUBLISHED`.
- Insert `balance_config_activations` with `PUBLISH`.
- Move `balance_config_active` to the new version.
- Append `admin_audit_logs` with permission `BALANCE_PUBLISH`.

Failed publish attempts after admin authentication return structured error codes and append `admin_audit_logs` with `result = 'FAILURE'`. The raw confirmation token is never written to audit logs.

<a id="source-de8c86b812b01d1c-dev-seed"></a>
#### DEV Seed

DEV can be bootstrapped with `packages/db/seeds/dev/0001_balance_config_v1.sql`.

Apply it only to DEV:

```bash
pnpm d1:dev:seed:balance
```

The seed inserts published version `1`, records an activation, and points `balance_config_active` at version `1`. This is not a replacement for the Admin draft/publish workflow; it exists so Unity and Game API development can read a published payload before Admin publishing is implemented.

<!-- source-body:source-de8c86b812b01d1c:end -->


<a id="source-b6e25ed5e5579577"></a>
## Integrity

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/integrity/integrity.md`  
원문 SHA-256: `1d4b95b69b7be3ac7bfa0e6052d2d3f71246274887540485f0ccdfab50d5d81e`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-b6e25ed5e5579577:start -->
<a id="source-b6e25ed5e5579577-integrity"></a>
### Integrity

Integrity validation is deterministic rule validation, not full frame re-simulation. Events are immutable and resolutions are appended separately.

Automated permanent bans are blocked. Permanent ban requires OWNER permission and admin confirmation.

<!-- source-body:source-b6e25ed5e5579577:end -->


<a id="source-8c507d1c49464416"></a>
## Run Protocol

원출처: `Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server/docs/run-protocol/run-protocol.md`  
원문 SHA-256: `d6212122b4507a2fa90aea5c2a2468c0047724693d13c73e6f756476516800be`  
검토·적용 경계: Active server detailed implementation/API contract; category summaries do not contain field/error/idempotency/lifecycle equivalence.

<!-- source-body:source-8c507d1c49464416:start -->
<a id="source-8c507d1c49464416-run-protocol"></a>
### Run Protocol

Server does not replay every combat frame. Runs freeze balance version, snapshot, wave plan, augment plan/history, tactical ledger, action ceiling, and validation envelopes.

Checkpoint waves are 3, 6, 9, 12, and 15. Rewards are calculated by the server from the run balance version, not the current active balance.

Act 1 launch note:

- Ordinary operations remain 15-wave encounters.
- Augment/tactic choices are granted/selected from server-authored
  start-of-run package refs instead of mid-wave checkpoint drafts.
- `run_checkpoints`, `run_augment_history`, and `run_tactical_ledger` remain
  schema/backlog surfaces for future mid-run drafting.
- Raid boss-only encounters use the same full start package refs before the
  boss fight.
- Unity may echo selected refs in run finish for validation/diagnostics, but
  reward/economy/story/offline/gacha truth remains server-owned.

<!-- source-body:source-8c507d1c49464416:end -->

## 포탑 설치 미정 계약의 부분 해소 — 2026-09-22
[04 사용자확정](04_COMBAT_RUN.md#gun-carriage-two-turrets-v01)으로포격객차당고정2슬롯·빈칸허용·동종/이종허용이기획상확정됐다. 과거 turret_installation_undefined의수량까지현재전부미정이라고안내하지않는다. 실제보유수량/인스턴스·해금/설치DTO와지원바인딩은아직별도이므로문서승인만으로현재API의unsupported검사를제거하거나구현완료로선언하지않는다.

<a id="story-seven-implementation-audit-v01"></a>
## 스토리7사건 승인기획–실구현 대조 v01 — 2026-09-23

범위: [32 목표v01](32_NARRATIVE_REFERENCE.md#chapter-one-event-objectives-v01)의 사건1~6과 [결전v01](32_NARRATIVE_REFERENCE.md#raider-story-finale-v01)의 사용자 승인 이후 연결 점검. 아래는 로컬 현재 소스의 정적 대조다. 운영 장애/실제 오지급 발생이나 저장소 전체 기능 부재를 확정한 결과가 아니다. 코드·DB·계정·미술을 변경하지 않았다. 줄 번호는 조사 시점이며 담당자는 적용 전 최신 함수와 변경본을 다시 확인한다.

경로 약칭 S=`D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server`, U=`D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts`. Plan은 정책/인수조건, Server는 권위 계약·저장/정산, Unity는 실행/입력·복구, Direction은 독립 표현을 소유한다. 일반 솔로/일반 협동의 전투를 새로 서버 매틱 판정으로 변경하는 작업이 아니다.

### 1. 확인한 차이와 처리

| ID / 분류 | 확인 근거 | 승인기획과의 차이 / 필요한 변경 | 완료 증거 |
|---|---|---|---|
| ST-C01 / 확인된 종료 계약 차이 | S/apps/game-api/src/index.ts:1544 `isRunFullCompletion`: VICTORY와 raid bossDefeated 또는 completedWaveCount===15; :6147~6150 레이드 완료를 rewardClearedWaves=15로 환산 | 새12방문·사건1~6 목표·사건7 제동+해방은 이 조건만으로 판정 불가. START에 고정된 프로필별 종료 검증으로 분기하고 사건/노드/웨이브/페이즈를 분리. 12를15로 보고해 통과시키지 않음 | 우회승리의 스토리 진행0,목표미완료 승리거절,제동만/해방만 완료거절,구15wave/구raid원결과 보존 |
| ST-C02 / 확인된 진행 입력 차이 | S/apps/game-api/src/index.ts:1861 `createStoryProgressPlan`, :6239 호출. 입력 operationId로 순서를 구하고 `actOneOperationIds.slice(0,newOrder)`를 clearedOperations에 합침 | 새 사건 배정/개인 적격/성공근거 대신 작전ID만 전달하면 우회 일반승리·다른 참여자에게 선행 사건을 채울 위험. 새 경로는 해당 배정 사건1개만 원 적격성에 따라 확정,구계정 진행은 회귀/자동초기화하지 않음 | 높은 호스트 진행이 파트너에 복사0,사건없는 일반승리0,동일사건 재요청1회,구이력 정상조회 |
| ST-C03 / 확인된 권리 정체성 차이 | S/packages/db/migrations/0003_progression.sql:74 `account_first_clears` PK(account_id,operation_key,risk); index.ts:6151~6161 같은 키 조회 | 기존 위험도별 권리는 사건당 첫권리와 다름. 새 사건권리를 별도 명시 매핑하고 기존수령/미수령/미확인 상태를 이행. 위험도별 보상 삭제·자동재지급 금지 | 위험도변경에도 사건첫권리1회,구preview늦은claim 중복0,기존미지급 복구 |
| ST-C04 / 확인된 환급 경로 차이 | S/apps/game-api/src/index.ts:1441 `createStoryRaidStaminaRefundCandidate`: 같은 raidID/FAILURE/bossDefeated=false면 구 환급후보; :6200대 실제환급 계산 경로. U/Run/RunResultSubmitResponseJsonAdapter.cs:111 이하 환급 수신 | 신규 층12 결전은 별도입장/환급 추가 없음. 같은 ID 재사용만으로 구 환급을 적용하지 않도록 원START policy/version/encounter 기준 구분. 구환급이 확정된 run의 권리는 보존. 응답표시도 실제원결과만 소비 | 신규결전실패 추가환급0,구환급run 원결과재생,표시와서버원장 일치 |
| ST-C05 / 확인된 결과 표현 부족 | U/Run/RaidBossRunResult.cs:7~38 raidId/bossId/bossDefeated/completedPhaseCount/duration; U/Run/RunRuntimeStateResultSubmitJsonAdapter.cs:175~결과직렬화; S/index.ts:695/1316 raid결과 형태 | 이 DTO 단독으로 목표순서/열·냉각/제동창/정지·해방을 설명 못 함. 기존 bool을 임의 성공으로 채우지 말고 원 실행증거·checkpoint·정의버전/검증결과를 연결. 세부 payload와 검증 보장수준은 Server/Unity 계약 | 정지후재접속·창끝/300초/열100·중복냉각·호스트이관·늦은결과 제출 인수증거 |
| ST-C06 / 확인된 모드 표현 차이 | U/Run/RunEncounterType.cs enum/formatter: Ordinary15Wave, RaidBoss 두종류 | 새12방문 run 안의 story node 및 마지막story종료를 구15wave enum으로 포장하면 의미 혼동. 실제 wire enum 추가 또는 프로필필드 방식은 담당자가 선택,기존문자열의 뜻을 조용히변경하지 않음 | 새프로필 read/START/저장/복원/FINISH 연결,구클라이언트 미지원 명시,구run 호환 |
| ST-C07 / 확인된 포탑 설치 지원 간극 | S/apps/game-api/src/train-loadout.ts:95 ownedTurrets는 installationSupported:false / turret_installation_undefined | 첫신호/스크랩스타 신규해금 권리와 실제종류카탈로그/2칸설치가 별개. 이야기 성공이 임의 turretId나자동2기설치를 만들면 안 됨. 승인해금을 S-06 성장/실보유 계약과 연결 | 실제종류 매핑/첫권리1회/기존보유유지·개인별협동자격/장착지원 명시 |
| ST-C08 / 미확인 발행 의존 | 32 승인본문은 H/B를 같은위험도 원최종보스로 매핑하도록 요구. 이번검사에서는 7사건의 실제지원프로필/시설위치/조작범위/H/B 버전 매핑표를 확보하지 못함 | 기능 부재로 단정하지 않음. 담당자가 현재구현/카탈로그/QA 근거 제출. 없으면 승인조건으로 콘텐츠 프로필 제작. old raid baseReward나점수값을 H/B로 오인하지 않음 | 7ID→profile/version→목표/대상/영역/수치 표,6초창 실제이동·1.5초조작 가능성,미지원명시 |

구분: C01~C07은 읽은 경로의 계약 차이이며 새 경로가 다른 곳에 이미 연결됐는지는 담당 소비 증거로 확인해야 한다. C08은 미확인이다. 이번 점검만으로 운영 오지급/우회치트 성공을 주장하지 않는다. 기존 `bossDefeated`는 구계약에서 정당하므로 전체 삭제하지 않는다.

### 2. 구현 순서와 큐 연결

기존 S-02/S3-a~c 안에서 먼저 처리: (a) 원프로필/목표/종료·버전 구분과 C08 매핑, (b) 사건배정·개인성공근거/권리 및 C01~04, (c) 결과/저장·복구 계약과 C05~06. C07은 S-06과 동일 실종류 매핑을 공유한다. S-12의 실제참여/호스트복구 계약이 미완료인 협동 부분은 따로 대기하고 독립 가능한 솔로/구run호환 작업을 계속한다. 일반전투 검증을 위해 월드서버 시뮬레이터를 강제하지 않는다.

선행 완료 담당자는 계약 버전·실제변경 경로·QA와 기존/신규프로필 구분 근거를 기존 WAIT에 기록하고, 기존 후속 정의의 모든 선행이 충족될 때만 해당 세션 큐에 추가한다. 현재 U-02 등 대기 선행을 무시한 새 Unity 실행블록/중복 큐는 생성하지 않는다. 신규 시설/제동/열 표현이 필요한 Direction 후속은 Server 계약으로 필수 상태·입력영역이 확정된 뒤 WAIT에서 등록한다. 이미 완성한 지도 D-11/포격객차 D-10을 이 작업 때문에 제작미완료로 되돌리지 않는다.

Unity 후속은 모델 목표/시계·전투 실행, Controller 입력/호스트 명령 조정, View 표시/콜백을 분리한다. 화면에서보상·승리 판정을 직접 만들지 않는다. 결과 DTO 확장만으로 열/제동/위치 검증 실행완료라고 하지 않는다. Admin은 확정결과/미지급/실패 원인을 읽기전용으로 소비할 계약 차이가 있을 때 기존 후속범위에서 반영,수동 스토리클리어/환급 버튼을 새로 추가하지 않는다. Homepage 공개 변화는 현재필수없음; 최신공개정보 변경이확정될때만 후속 큐를 만든다.

### 3. 통합 마감 조건

32의 사건14사례와 결전14사례를 실제 원프로필/버전에서 검증하고 C01~08별 근거를 연결한다. 여기에 신/구run 동시존재,일반우회·사건해결후패배,협동개인자격,구환급/구preview,새종류해금의 회귀를 포함한다. 같은 테스트가 여러 항목을 보장하면 근거를 공유하며 실행수를 중복 과장하지 않는다. 실계정/운영 지급 없이도 가능한 격리검증부터 수행한다.

완료상태는 ‘정책 승인 / 계약차이 식별 / 담당 구현 / 연결검증 / 실기밸런스’를 구분한다. 현재는 앞의2단계까지, 코드수정/실행검증/미술/배포 아님. 추천 처리방향은 승인기획 반영과 구START 보존이며 새 밸런스 정책을 추가하지 않는다. C08 실제공간/수치에서 승인조건이 성립하지 않으면 변경안을 먼저 기록하고 조용히 수치를 바꾸지 않는다.

<a id="story-seven-profile-mapping-v01"></a>
## 스토리7사건 실제지원 매핑 조사 v01 — 2026-09-23

ST-C08 후속. 현재 로컬 Server/Unity 소스와 승인 기획을 읽은 정적 조사다. 약칭 S/U는 story-seven-implementation-audit-v01과 동일. 운영 배포/실DB/실행 전투를 조회하거나 변경하지 않았다. 등록된 이름·표현 fixture·시험용 실행 프로필·신규 스토리 출시 연결을 구분한다.

### 1. 확인된 원본과 지원 경계

- S/apps/game-api/src/departure.ts:118~120은 7개 작전 중 raid를 RAID_BOSS로,나머지를 ORDINARY_15_WAVE로 발행하며 raid에는 boss_raider_king_engine 및 crown_chase/trophy_break/engine_stop 3개 참조를 넣는다. 이것은 구 이름/메타데이터가 존재한다는 근거이며 새 장갑/방송/과열 페이즈가 구현됐다는 근거가 아니다. 구참조를 이름순서가 비슷하다고 새 정의로 덮어쓰지 않는다.
- S/apps/game-api/src/combat-readiness.ts:24~38 `describeCombatRequirements`는 BLOCKED/canStart:false와 enemy_faction_binding_missing/wave_plan_binding_missing/reward_table_binding_missing/combat_seed_contract_missing/engine_output_profile_unapproved/effective_stats_profile_unapproved 등을 반환하며 bindingVersion/catalogHash는 null이다. 이 조사 경로의 연결 미완료를 명시적으로 확인했다. 다른 전용 실행 경로까지 모두 시작 불가라고 확대하지 않는다.
- S/apps/game-api/src/ce03-local-schema.ts:65~70의 별도 시험 template은 opn_01_last_departure/SOLO/I/ORDINARY_15_WAVE,raid/boss null,15waves·3enemy로 제한된다. 존재하는 시험 실행은 보존하되 신규 사건1의2웨이브/승선·출발확인이나 나머지6사건 지원 증거로 사용하지 않는다.
- U/Battle/ProducerCombatAdapter.cs:98~100은 spawn.resolvedMaxHp·enemyDefinition.atk 등을 전투 actor에 전달한다. 값 연결의 기존 재사용 경계이지 스토리 시설/냉각/제동 실행 완료가 아니다. U/Narrative/ActOneMissionNarrativeFixtures.cs:151~165에는 약탈왕 대사/제목이 존재하나 목표 판정 코드와 별개다.
- S/packages/balance/src/index.ts:247~264의 18000은 raid 작전 보상 진행 배율(bps),:350~370의30000/12는 오프라인 진행 배율/보관시간이다. 어느 것도 보스HP/공격력/제한시간이 아니다.

### 2. 7사건 연결표

모든 행의 현재 출격 메타데이터는 위 departure 경로 기준이다. 실제 신규 story node의 profile/version·원 그래프·대상 위치/조작범위·checkpoint·성공근거 발행 연결은 이번 조사에서 확보하지 못했다. 없는 것으로 확정하지 않고 담당자는 기존 구현이 있으면 정확한 소비 경로와 QA를 제시한다.

| mission ID | 발견한 현재 연결 | 승인 신규 프로필에 반드시 연결할 대상/근거 | 현재 판정 |
|---|---|---|---|
| opn_01_last_departure | 구ordinary 메타 + 이 ID에 한정된15wave 시험schema | 2waves,승선지점1/출발준비1,순서/단계완료 | 구시험 존재 / 신규목표 미연결 확인 필요 |
| opn_02_first_signal | 구ordinary 메타/보상표 | 2waves,수신장치 활성화/원본회수,개인첫클리어 속사 해금 | 신규profile/시설·종류ID 미확인 |
| opn_03_water_stop | 구ordinary 메타/보상표 | 2waves,합의/보호/펌프 상태·사후수리/급수,기능정지0회 기록 | 시설HP/표적/상호작용 매핑 미확인 |
| opn_04_scrapstar_show | 구ordinary 메타/보상표 | 2waves,포로구역/방송 필수확인·선제 중계기/보호기록,곡사 해금 | 시설·단계/종류ID 미확인 |
| opn_05_night_boarders | 구ordinary 메타/보상표 | 내부2waves,안전확인·보호구역/집결안내 | 내부위치/목표 매핑 미확인 |
| opn_06_broken_line | 구ordinary 메타/보상표 | 2waves,선로연결·필수노선/보조기록·교량보호,우회연출 | 실제목표/시설 미확인;지도edge추가 권한 없음 |
| raid_raider_king_engine | 구raid/boss/3phase refs·표현fixture | 승인3phase 내구/열/냉각·제동창/재잠금·해방·선택5기록 | 명칭/표현 존재 / 신규정의·checkpoint·실행연결 미확인 |

### 3. H/B의 기획 기준값 확보와 구 패턴 분리

44_COMBAT_DETAIL_CONTRACTS.md:392~395에는 첫 비교 보스HP24000/장갑15%/기본 원시120이 있고,:655는 일반 최종보스도 이 승인 시험 모델을 공유한다고 명시한다. 따라서 32결전v01이 요구한 ‘같은 위험도의 원최종보스 H/B’의 **기획상 첫 시험 원본 후보는 확보**했다. 실제 발행버전/위험도별 해석은 확인하지 않았으므로 모든위험도에24000을 고정하거나 보상riskMultipliers를 전투HP배율로 쓰지 않는다.

해당 기준프로필 H=24000,B=120,장갑15%를 정확히 매핑할 때의 승인식 결과:
- 단계내구8400/8400/7200,합24000. 비교군36000/48000을 출시값으로 승격하지 않는다.
- 기본 원피해120,충돌2B=240. 각 원표적에서 공통 방어/피해전이 계산,열차와객차에240을 중복 직접 적용하지 않는다.
- 장갑15%를 단순감소로 보는 제한된 비교 산술의 EHP≈28235.29는 보스HP가 아니며 추가배율로 또곱하지 않는다.
- 최신 약탈왕v01은3초기본/500ms예고·12초충돌/2500ms예고·35/35/30·제동/열/300초를 소유한다. 구44의2.5초/700ms/60·30%경계·페이즈별피해/공속강화·14초A/B/C를 추가로 누적 실행하지 않는다. 일반최종보스/구START는 구프로필을 유지한다. 구필드의 회수시간 등 최신안이 명시하지 않은 항목은 임의0으로 지우지 않고 원프로필 상속/새 명시 중 실제 계약에서 기록한다.

이 절은 새 수치 승인이 아니라 승인 H/B 참조의 산술과 정책 우선순위 해석이다. ST-C08은 ‘기획기준 확보 / 실제버전·위험도·시설지원 미확인’으로 세분화하며 완료로 닫지 않는다.

### 4. 담당자가 채워야 할 발행 계약

Server 소유 매핑은 mission/정의version·기존run범위/신규적용범위·profile/원H/B/위험도계산·방어/시간정밀도·원enemy/target/phase·필수/선택단계·종결/개인권리·클라이언트지원버전·증거참조를 포함한다. Unity 소유는 각대상영역/위치·이동/조작가능범위·1.5초점유/취소·애니메이션과실행분리·저장/호스트복원이다. 공통 원전투 계약에 없는 행동은 fixture 성공bool로 대체하지 않는다.

수치가 필요한 잔여를 명시한다: 사건1~6 실제 적 스폰/HP/공격,보호시설의 HP/공격받는 경로/기능정지·사후복구,상호작용 배치/거리,약탈왕 최종타깃/위험도별H/B·회수/타깃선택·냉각/제동 이동거리. 기존 승인표에 실제 원본이 있으면 담당이 참조하고 없으면 Plan에 수치 후보 요청. 승인된 틀만으로 담당이 경제/실패조건을 새로 결정하지 않는다.

6초 제동창은 실제 이동최악시간+1.5초상호작용이6초보다 작다는 근거가 필요하다. 정확창끝은거절이므로 <=6으로 검사하지 않는다. 냉각에서제동으로전환하는 실제동선/전투다운복구도 검증하며 화면에가까워보인다는미술판정으로대체하지않는다. 구체좌표를Plan이실월드단위없이발명하지않는다.

기존S-02/S3 선행에본표를연결,새실행큐생성0. 담당이 매핑/지원근거를기록하면기존WAIT 선행조건을확인해Direction/Unity 후속을등록한다. 현재새7사건카탈로그발행·H/B운영반영·전체전투완료 선언은하지않는다. 다음독립기획은 아직원수치가없는보호시설/적편성기준을정리하되실행원본이있는항목을중복재설계하지않는것이다.

<a id="story-facility-runtime-contract-v01"></a>
## 스토리 시설 대상·행동·저장 인수 계약 v01 — 2026-09-23

[44 시설v01](44_COMBAT_DETAIL_CONTRACTS.md#story-facility-roster-v01) 사용자승인 반영. 신규밸런스 제안이 아니라 승인조건을 실행할 때 필요한 책임/검증 경계다. 정적 근거는 U= D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts, S= D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server. 조사경로 밖 전체구현 부재/운영버그를 주장하지 않는다.

### 1. 현재 경계와 변경 필요

| 확인한 원본 | 실제 제한 | 인수 요구 |
|---|---|---|
| U/Battle/BasicCombatSimulation.cs:8,42~47 | BasicCombatTarget은 적내/외부·요원·열차,적 actor는 Train/Operators만 허용 | 시설을Train/Operators로위장하지않고별도대상자격을표현. 구정의의뜻은보존 |
| 같은파일:127~140 | EffectTarget은allies/enemies에서조회,Heal/Shield는하위resolved처리,Repair는Train검사 | 상위효과수락/대상해석에서시설의치유·수리·실드·도발제외를검증. 이저수준함수만보고현재무단치유버그로단정하지않음 |
| U/Battle/CombatSkillCommands.cs:104 | SkillBusy는queued/casting스킬과RecoveryUntil검사 | 1.5초시설상호작용도동일행동점유계약에결속해야하며독립UI타이머로동시공격허용금지 |
| S/apps/game-api/src/ce03-local-schema.ts:18,47~48 | 시험target열거/적기본target은시설미포함 | 새지원정의·schema/version과실소비를함께연결,CE03기존15wave시험을광범위치환하지않음 |

### 2. 상태 소유와 데이터 의미

시설상태의권위있는소유자는해당모드전투도메인의목표상태다. 일반솔로는로컬실행,일반협동은서버지정단일호스트실행,서버는원정의/보고/진행·경제검증이라는승인경계를지킨다. UI/시설이미지는HP원본을소유하지않는다. 클래스수/새전역서비스/별도네트워크를강제하지않는다.

최소저장내용: run/node/사건definitionVersion,시설instance/definition참조,원최대HP/현재HP,기능정지여부·최초정지근거,선택실패,필수사후복구완료,마지막수락원키/sequence. 공격역할은원적spawn에결속하고현재역할·고정시설대상·예고완료/다음예약·미완료행동식별을함께저장한다. 같은시설을wave마다새로생성하거나공유객차HP를참조하지않는다.

기능정지와사후복구이후표시는단일상태전이로관리하며HP>0이면선택실패를자동해제하는추론을금지한다. 원정지이력과선택실패는복구후에도보존한다. 전투후수리의완료표시와전투용자동HP회복을분리한다. 노드종료후시설상태는결과근거에보존하되다음노드/계정영구손상으로적용하지않는다.

### 3. 명령/결과/시간 경계

시설입력은 View 의도→Controller의기존명령흐름→모드권위실행객체→상태통지 순서다. 수락때원run/node/시설·조작계정/요원권한·생존/범위·선행목표·행동점유·미종결·원version을검사한다. 시작/완료/취소는원행동ID로결속하고실공유상태만변경한다. wire명령이필요한지와이름은담당기존계약에맞추며새HTTP요청을매프레임강제하지않는다.

승인1.5초는게임전투시계로진행,범위이탈/다운/종료시부분진척0·원취소1회,단순피해는취소사유아님. 동시스킬/시설입력은원행동큐수락순서와점유규칙으로하나만실행한다. 재바인딩/버튼연타/응답유실은새행동으로증식하지않는다. 화면종료는자신의구독/표시만해제하며이미수락된도메인행동을자동완료시키지않는다.

시설공격미완료취소·HP실손실·기능정지·선택실패·정상전열복귀예약을동일원사건전이에결속한다. 정상전열기본은원주기전체뒤,옛시설공격대기시간을즉시전열추가타격으로바꾸지않는다. 시설피해의열차전이0과치유/보호막/자동재건제외는태그문자열하나가아니라효과적격계약으로검증한다.

중간필수확인대기는전투정지상태이므로戦闘tick기반1.5초채널을돌려교착시키지않는다. 승인44의‘전투중선택상호작용1.5초’와‘전투정지중필수확인’을구분:후자는수락된확인명령1회로단계를전이하고,다음2초웨이브간격부터전투시계를진행한다. 대사/표현애니메이션시간은성공근거가아니다. 사후필수확인도추가전투시간압박없이같은확인계약을사용한다.

### 4. 복구와 실패

체크포인트는시설HP/실패이력·wave/의무출현·전투정지hold·요원행동점유·진행중상호작용/예고/다음예약·원결과sequence를함께복원해야한다. 일반호스트이관이시설600·6초공격예약·1.5초진척/선택권을새로만들면안된다. 원스냅샷상태복구와실제새중단/범위이탈에의한취소를구분한다.

지원불가/알수없는시설/원정의소실은명시오류이며시설파괴나선택성공으로대체하지않는다. 작업1부분만구현되면전체지원을거절하거나지원표면을명확히제한하고가짜완료bool로보완하지않는다. 구START에는원프로필과해당소비자를사용한다. 새시설필드누락구데이터에600을묵시주입하지않는다.

### 5. 완료 증거·큐

시설을요원/열차로만들지않은대상계약,금지효과거절/전이0,1회입력1행동·상호작용과공격점유,두wave/DC01사이HP유지,시설정지후정상공격복귀,정지중필수확인교착없음,호스트/체크포인트복원,선택실패후필수성공·최종승리개인권리,구프로필회귀를관찰가능결과로검증한다. 데이터파일존재/직렬화왕복만으로실제전투완료라고하지않는다.

기존S-02/S3의시설/목표정의·보고/저장검증계약에인수,실일반협동은S-12근거와연결. Server완료근거와기존선행조건충족후Unity/필요Direction후속등록. 이번에는서버기존작업참조만추가하고새하위실행큐선등록0. 기존코드/JSON/미술/계정변경없음.

<a id="skill-auto-on-off-audit-v01"></a>
## 스킬 AUTO ON/OFF 기획·구현 대조 — 2026-09-28

사용자 요청: Sable v02·협동 v05 확정 후 오토 ON/OFF 차이 확인. 아래는 현재 로컬 Unity의 확인 경로와 승인기획 대조이며 실제기기/전체모드 실행검증이 아니다. U=D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts. 코드/화면은 변경하지 않았다.

### 1. 확인된 스위치 범위

U/Battle/ApprovedCombatOperatorCards.cs:25는 AUTO 버튼에서 driver.SetUltimateAuto를 호출한다. CombatLiveHudBinding.cs:54 이하의다른HUD도궁극기스위치에연결한다. CombatOperatorAutomation.cs:7~15는 generalSkillAuto와ultimateAuto를분리하고사용자SetUltimateAuto는후자만변경한다. generalSkillAuto는 ConfigureLocalOperatorAutomation(generalAutomatic)에서프로필로설정한다. 이사실은프로필에따라일반자동이비활성인시험도존재할수있음을뜻하며모든실행에서무조건true라고단정하지않는다.

| 항목 | 현재 AUTO ON | 현재 AUTO OFF | 근거/한계 |
|---|---|---|---|
| 요원 기본공격 | 기존자동공격 유지 | 동일 | 궁극기토글은기본공격플래그를변경하지않음 |
| 요원 일반스킬 | generalAuto 지원프로필이면자동 | 같은프로필이면계속자동 | CombatSkillCommands.cs:138은두플래그를독립검사. 승인44 일반자동과일치 |
| Sable 방어준비 | 승인조건으로자동수락해야함 | 동일 | 일반스킬이므로궁극기AUTO와독립. 실제Sable지원/효과적격연결은아래차이확인필요 |
| 개인 궁극기 | 충전/행동/대상조건충족시자동시도 | 새자동시도없음,수동입력필요 | CombatOperatorAutomation.cs:20~25. 조건충족즉시항상발사보장이아니며행동점유/예열유지 |
| 수동 궁극기 입력 | 입력가능,원명령경로사용 | 동일 | ApprovedCombatOperatorCards의개별궁극기버튼과ProcessSkillCommands수동처리. 같은시각중복소비회귀필요 |
| 이미수락된시전/발사된공격·기존효과 | 원수명대로진행 | OFF만으로취소/환급/효과제거안함 | SetUltimateAuto는플래그만변경,이미CASTING명령은기존해결 |
| 궁극기충전획득 | 원기본유효HP피해충전 | 동일 | ChargeBasic은ultimateAuto를검사하지않음. 상한/소비미지원은아래별도 |
| 포탑 자동액티브 | 승인04에따라자동CD형 | 궁극기토글로중지하는정책없음 | 기획상범위,3종신규실행연결까지검증한것은아님 |
| 냉각/제동/스토리시설조작 | 원수동입력유지 | 동일 | 승인32/44. AUTO는스토리목표자동완료기능아님 |

일반스킬을끄는새UI나OFF시일반스킬수동버튼을추가하는것은현재승인범위에없다. ‘스킬전체AUTO’로설명하면실제궁극기스위치와다르다. 2026-09-28 사용자 정정으로 버튼 문구는 AUTO 유지. 기존 명칭 변경 추천은 철회하며 기능 설명과 실제 동작 범위만 구분한다.

### 2. 실제 구현 차이·주의점

1. **효과적격부족:** AutoEligible은생존·행동·충전/CD·SkillTargets결과수>0를검사한다. 방벽여유/25%태세나실치유가능량을공통식에서검사하지않는다. Sable신규정의가이경로를사용하려면승인v02효과별수락조건을연결해야한다. 현재코드구성이초기5인시험을요구하므로이미Sable이잘못시전한다고단정하지않는다. 일반스킬인Sable수정이궁극기AUTO ON에서만작동하면안된다.
2. **충전상한/소비간극:** CombatOperatorAutomation.cs:18~20은충전상한100/==100 검사와소비시0설정이다. 승인O11상한200·사용당100차감과다르다. O11지원프로필에서는ON/OFF모두같은상한/차감정책을사용해야한다. OFF상태에서200저장후ON→한번100소비하고남은100유지,추가시전은행동/대상/원정책이허용할때만;같은틱무제한연사금지. 구100상한시험은구버전유지.
3. **표적소멸시재타깃:** CombatSkillCommands.cs ExecuteSkill은명단전원HP0이면SkillTargets로다시찾고예열을새로잡는경로가있다. 최신승인스킬의수락명단고정·대상소멸자동재타깃0과차이이며수동/AUTO공통이다. 적용스킬/버전별연결검증필요,이번작업에서임의삭제하지않음.
4. **토글/예약:** OFF는이미수락된자동시전을취소하지않고수동QUEUED명령을지우지도않는다. ON/OFF가CD/충전/원시전횟수를리셋하면안된다. 수동명령처리후자동궁극기→일반자동순으로실행하는현경로의행동점유를실기검증한다. 프레임간경합을보기좋은HUD만으로통과시키지않음.
5. **설정복원:** Capture/RestoreOperatorAutomation은generalAuto/ultimateAuto/sequence/개별charge를저장·복원한다. 복원검증도현재100상한이므로O11지원복구와함께확장필요. 새run의사용자설정기억범위·실협동역할별토글권한/호스트복원·월드소비완료는이번코드읽기로확인되지않았다. 월드단절중새궁극기자동대행금지등기존모드예외를전역토글로덮지않는다.

### 3. 인수와 시험

기존S-09/U-08/P-08에는Sable승인조건,기존일반전투/지휘실소비S-01/S-02/U-01/U-02에는O11충전·결과저장/스킬원명단,협동S-12/U-11에는토글권한/복구근거를연결한다. 실제작업등록은기존선행에따라처리하며범위를복제한새큐를만들지않는다.

필수비교: ON/OFF각일반스킬동일조건시전,OFF충전유지/수동시전,ON수동동시1회,CASTING중OFF후원시전완료,새ON즉시수락은행동적격때만,정지/종결중토글,100/200상한과재접속,일반Sable유효효과0대기,대상소멸재타깃0,스토리시설자동조작0,일반협동권한·월드단절예외. 실제테스트미실행. 토글이잘못된것인지미지원프로필을섞은것인지원version과함께판정한다.

## AUTO 대조 후 보완 인수 — 2026-09-28
[44 보완기준](44_COMBAT_DETAIL_CONTRACTS.md#skill-auto-correction-v01) 사용자보완지시반영. 공통수락·Sable조건,원상한100/200와>=100/100차감,원타깃/override/K고정,설정/충전복원·모드권한·기존 AUTO 표시 유지 기준을 구체화. 앞선조사에서발견한코드차이는실제수정/검증전미해결유지. 기존담당소스소유권/선행순서보존.

<a id="new-map-risk-reward-scope-v01"></a>
## 신규 12노드 위험도 보상안의 적용 경계 — 2026-09-28
[35 고위험 반복 보상 v01](35_CURRENT_ECONOMY_ANNEX.md#high-risk-repeat-rewards-v01)은 신규 I 기준 1배·II~V=1.2/1.4/1.65/2의 승인 대기 제안이다. 구 source-77f48c0fb91c5e38 및 출발에 고정된 launchRunRewardsConfig의 I=0.8/작전 진행도/15wave 산식을 덮어쓰지 않는다. 신규 표의 위험도 보상 배율은 적 HP/공격 배율이나 지원/해금 증거가 아니다. 신규 실제 프로필·원START·개인 결과·정산/claim 연결 후 발행하며 주간 코어 만료 예외의 별도 승인 대기는 유지한다.

<a id="scene-information-lifecycle-proposal-20261019"></a>
## 씬별 정보 보유·조회·확정 갱신 설계 — 2026-10-19 / 신규 제안
목표: 이미보유한값은즉시표시하고서버권한/최신결과가필요한절차만기존API를사용한다. 화면열기와데이터취득을동일시하지않는다. 전투정보는44#combat-info-start-choice-approved-20261019 승인유지. 아래비전투씬의갱신정책보완은검토안이며전체구현/운영지원확인아님.
근거:01 계정진입의 profile/story-progress,본49 account/state 계약,04 결과/복원,05 CONFIRMED/적용완료,06 보상영수증,13 원장착/미리보기 분리 관련절. API전수/실scene경로분석은미완료이며확인되지않은endpoint를신설하지않는다.

### 씬별 기준
| 씬/흐름 | 보유정보로 표시 | 서버 확인/갱신이 필요한 경계 | 실패/미확정 때 |
|---|---|---|---|
| 로그인·최초진입 | 준비된로컬정의/문구/자산 | 기존인증과 GET /v1/profile, /v1/story-progress, /v1/account/state 각기존용도 | 기존인증/초기복구정책,실패를신규계정/미보유로단정0 |
| 로비·계정요약 | 같은계정의검증된계정상태 | 최초보유없음·기존권위변경/유효성상실·명령결과불명확시원조회/복구 | 기존값이유효하면유지,미확정잔액으로소비성공표시0 |
| 요원·열차 성장/편성 | 보유목록·성장값·편성+로컬정의 | 훈련/승급/편성저장·활성화명령확정;충돌시최신원상태확인 | 후보는로컬초안,실패시확정값보존·임의재소비0 |
| 출격준비 | 보유편성/목표설명·계정정보 | 기존적격검사와사용자START,원출발snapshot수락 | 버튼미리보기는출격성공아님,기존START복구 |
| 작전선택·정비/장비 | 원후보/현재run장착·아이템정의 | 선택/교체/합성/시술의기존확정명령·실효과반영 | 선택강조만으로장비/수치확정0,원응답복구 |
| 결과·보상 | 이미확정된원결과/지급영수증 | 원종료검증·보상claim/권한확인·미확정영수증복구 | 원요청유지,새키/새추첨/가짜지급0 |
| 작전일지/기록 | 이미취득한해당기록·기존목록페이지 | 최초미취득상세/다음페이지·명시갱신·원미확정결과복구 | 조회실패를빈기록/기록삭제로표시0 |
| 공지·이벤트 | 현재유효한취득목록/콘텐츠 | 기존게시기간/대상계정/변경버전·명시조회 계약 | 기존공지정책준수,공지실패로전투정보조회실패유발0 |
| 상점·광고·구매 | 유효한상품설명/이미보유한표시데이터 | 실제가격/판매자격/광고·구매승인/결과검증 등기존거래경계 | 화면캐시로권한/지급확정0,원거래복구 |

위경계는씬마다새API를추가하라는뜻이아니다. account/state의소유객차/포탑목록을실편성위치라고오인하지않고기존 formation 원필드를쓴다. 런장비와계정보유items를같은인벤토리로합치지않는다. HTTP no-store와계정Model의현재상태보유는구별한다. 응답을임의오프라인권한캐시로쓰지않는다.

### 데이터 소유와 갱신
계정상태는기존계정Model,run상태는기존run Model,화면선택/스크롤은View/Controller 수명으로구분한다. 씬별로동일잔액/보유목록의별도수정본을만들지않는다. 성공응답이완전한변경결과를공급하면그결과를원owner에반영하고관련View에통지한다. 성공여부만있어필수상태가없는경우기존계약의조회로보완하며UI가차감/보상을추측하지않는다.
화면재진입만으로동일GET를항상발생시키지않는다. 계정/버전/원유효성조건이맞는정보를재사용한다. 외부변경/재접속/원변경통지등실제무효화사유와필드범위를정하고임의공통TTL·매씬전체재조회·상시폴링을신설하지않는다. 실제갱신이필요한화면에는해당기존조회/오류흐름을유지하며전투창의로딩제외를모든씬로딩금지로확대하지않는다.
계정전환/로그아웃은이전계정데이터/콜백을격리한다. run변경은옛작전값이새작전에보이지않게한다. mutationSequence/revision은각원계약범위로소비하고 serverTime을revision으로사용하지않는다. 서로다른owner를가짜단일revision으로합치지않는다.

### 확인해야 할 실제 연결 목록
각씬담당은기존QA에 필드→최초공급endpoint/저장snapshot→Model→View→갱신명령/통지→무효화조건을확인한다. API명은실라우트/실소비코드가확인된경우만확정하며단순기획목록을실구현완료로세지않는다. 부족한필드가응답에있으면Unity결속,응답에도없으면Server계약,아이콘누락이면자산결속으로분리한다.
검수:보유정보로재진입시불필요요청없음,확정명령후관련화면동시일치,미리보기취소불변,응답유실/충돌·중복/계정전환·run교체·해제후콜백. 요청횟수/상태변경은실관측으로확인하며수치절감률추정0. 명령재시도는원유스케이스의멱등/복구정책유지. Plan흐름/권위경계,Unity표시/Model결속,Server실API/권한·응답원본 담당. 신규타씬제안으로실행큐발행/코드/실Player검수 없음.

<a id="scene-information-lifecycle-approved-20261019"></a>
## 타 씬 정보 처리 승인·작전 인벤토리 범위 — 2026-10-19
사용자 ‘작전 인벤토리 기준으로 정리하고 타 씬 정보처리안도 승인 서버랑 유니티 작업 큐에’ 명시. #scene-information-lifecycle-proposal-20261019 채택. 기존데이터즉시표시/원확정결과갱신·권한검증/무효화·계정/run수명·필드별원천대조 승인. 장비/인벤토리는현재run의획득·장착·미장착장비를의미하며계정재화/items/영구요원보유와분리한다. 전투중읽기허용이장착교체/합성/시술허용을뜻하지않는다. 변경가능시점은기존안전정비/선택계약유지.
Server/Unity 각 SCENE-INFO-SOURCE:ROLE:20261019 독립실행단위등록. 기존API/Model재사용,데이터없는것을새API필요로추정0·권한검증/경제명령생략0. 신규응답변경은실누락근거와호환계약확정후후속,일괄새통합API발행권한아님.

<a id="scene-information-call-count-20261019"></a>
## 정보 재사용 전후 API 호출 이론 비교 — 2026-10-19
실네트워크측정/제품수정은없음. Unity현재호출코드와정보재사용설계로계산한조건부횟수다. 통신성공·유효세션·일반HTTP transport·계정/정보변경없음·fixture제외·재시도/중간자동갱신없음을가정한다. Ce03LoopbackTransport의동일경로는외부서버호출로세면안된다. 실제씬에서어떤transport를바인딩했는지포함한실측은담당잔여.

### 확인 가능한 작은 한 사이클
기준: 이미인증된로비에서 재화정보1회열닫기→열차화면1회열닫기→편성화면1회열닫기→로비. 성장/저장/출격/뽑기/보상변경없음. 초기필요정보미취득인경우와모두유효한경우를분리한다.
| 구간 | 현재코드의조회수 | 적용후최초취득 | 적용후기보유유효 |
| 재화정보 | 3 | 3 | 0 |
| 열차화면 | 4 | 1(열차배치만) | 0 |
| 편성화면 | 2 | 1(분대장선택만) | 0 |
| 합계 | 9 | 5 | 0 |
최초9→5는4회/약44.4%감소,이미모두유효하면이읽기구간9→0이다. 게임전체서버부하44.4%감소라는뜻아님. 유효성확인없는무조건캐시도아니다. 재화표시만을위해growth/config 최초취득을없애는추가최적화는이계산에넣지않아기존공유취득3종을보수적으로유지했다.
근거: LobbyCommandDeck.cs 재화버튼→RefreshSharedAsync. AccountScreenRuntime.cs RefreshSharedAsync→account/state·wallet/crystals·growth/config 각GET1회. LobbyDestinationPages.Open의Train→RefreshTrain→공유3+trains/loadout1. Formation→LobbyUltimate.RefreshUltimatePage→account/state1+formation/ultimate1. 모두동일계정의메모리보유후재진입에도현재이호출분기가있다. 서버실행/지연/응답크기/요금은측정하지않았다.

### 출격·전투·결과까지 포함할 때
위확인사이클에한작전을추가한총량은나머지원흐름호출을B라두면 현재 B+9,적용후 B+5(최초) 또는 B+0(유효보유)로비교할수있다. B는고정상수가아니라로그인준비/출격적격·START/선택횟수·정비거래/종료·claim·복구/원상태재확인의합이다. 이들응답이부족해필수재조회가필요하면삭제하지않는다. 각모드/선택횟수/서버transport가정해지지않은전체작전의정확총량은미확정이다.
확인된추가예: LobbyDeparture.RefreshDeparture는共有3+train loadout1+ultimate1의5조회뒤지원선택을localCommandClient.ReadSupportAsync로읽는다. 마지막LOCAL요청을외부서버GET1로더하지않으며quote/START경로·바인딩전수확인전출격전체호출수를5로단정하지않는다.
현재정보창의확인한Eve owner.Snapshot/Task.FromResult와Mira context.Capture는정보열기HTTP0. 따라서요원10회/장비30회클릭을가상의기존40API로세어절감률에추가하지않는다. 전투정보표시부분은확인경로기준0→0이며이번정책은일반run결속과향후불필요조회방지가핵심이다.
실측후속: 기존transport경계에서화면의도/endpoint/GETvs명령/시도vs논리요청/성공vs복구/LOCALvs외부를구분해동일사용순서로그대조. 비밀토큰/개인정보기록0. 실제변경후유효성/확정결과/권한검증보존을함께확인하며네트워크수감소만으로완료판정하지않는다.

<a id="full-cycle-api-count-model-20261019"></a>
## 출격부터 선택·보상까지 API 비교 기준 — 2026-10-19
사용자 전체 한사이클 전후 이론 비교 요청. 제품 코드 수정·실서버 START/claim·실측 없음. 기존 승인 정보 재사용 정책의 예상 호출 예산이며 현재 전체 전투가 이 순서로 통합 실행됨을 확인한 결과가 아니다.

### 고정 시나리오와 집계 경계
인증된 로비에서 시작. 동일 계정의 공유정보3종(account/state, wallet/crystals, growth/config), trains/loadout, formation/ultimate가 이미 유효함. 출격준비1회→preview1회→START1회→전투 선택1회 확정→승리 finish1회→결과창1회→기본보상 claim1회→로비복귀1회. 재뽑기/광고시청/장비거래/재접속/오류/토큰갱신/명시새로고침/포커스복귀 갱신 없음. HTTP transport에서 성공 요청당1회, 병렬 GET도 각각1회. 로컬 저장/복원/지원조회/프레임/정보창 클릭은 HTTP로 세지 않는다.
선택을 제외한 아래 HTTP 호출 지점의 합과, 아직 외부 연결 없는 선택 port 호출을 구별한다. 최초 인증·마스터/전투원천 부트스트랩은 기준 시작 전에 준비된 것으로 둔다. 실제 모드의 추가 노드보고/원천취득 등이 필요하면 별도 K에 더한다. 전 모드의 전체 API 전수 합계 아님.

| 단계 | 현 코드 기반 모델 | 승인정책 반영 목표 | 이유 |
|---|---:|---:|---|
| 출격준비 정보 | 5 | 0 | 이미 유효한 공유3+배치+궁극기선택 재사용 |
| 출격 preview 명령 | 1 | 1 | 서버 적격 확인 유지 |
| START 명령 | 1 | 1 | 출발 확정 유지 |
| START 후 상태 갱신 | 5 | 4 | 변하지 않은 growth/config만 제외, 나머지는 보수적으로 유지 |
| 선택1회 초기읽기·확정·후속읽기 | S | S | 내부 port3회, 실제 HTTP 수는 미확정 |
| 전투 요원/장비 정보 열기 | 0 | 0 | 확인된 로컬 읽기, 가상의 절감 추가 금지 |
| finish 명령 | 1 | 1 | 결과 검증 유지 |
| 결과창 첫 조회 | 4 | 4 | lifecycle+reward-preview+ad-skip+runtime-policy |
| 기본보상 claim 명령 | 1 | 1 | 지급 권한/멱등 유지 |
| 지급 후 lifecycle | 1 | 1 | 영수증 확인, CLAIMED면 preview 재조회 없음 |
| 지급 후 공유상태 갱신 | 3 | 2 | account/wallet 유지, 유효한 growth/config 재사용 |
| **보상 수령·상태 반영까지** | **22+S** | **15+S** | 동일한 누락범위 K는 양쪽에 추가 |
| 로비 복귀 | 3 | 0 | 방금 갱신한 상태 재사용, 중간 무효화 없음 |
| **로비 복귀까지** | **25+S** | **15+S** | 최초 부트스트랩 제외 |

선택 port의 Read→Execute→Read가 각각 HTTP1회로 연결된다고 가정할 때 S=3: 보상 반영까지25→18회(7회/28% 감소), 로비 복귀까지28→18회(10회/약35.7% 감소). 이는 조건부 계산이지 현재 실제 HTTP 측정치가 아니다. 최초5종을 보유하지 않은 출격진입은 목표에5회를 더해 보상25→23, 복귀28→23회. 전투중 선택 n회가 동일 계약이면 S=3n; 추가 경로 K가 있으면 그대로 양쪽에 더한다. 명령 확정응답만으로 모든 변경 필드를 충분히 받는지는 확인 전이므로 START/claim 뒤 필요한 상태 재조회까지 제거하지 않는다. config의 유효성이 상실되면 해당 GET도 다시 더한다.

### 확인 근거 및 미확인
Unity Assets/_LASTRAIL/Scripts 기준:
- Presentation/MissionShell/LobbyDeparture.cs RefreshDeparture → RefreshSharedAsync+RefreshTrainLoadoutAsync+RefreshUltimateAsync; 이후 ReadSupportAsync는 localCommandClient.
- DepartureRuntime.cs preview/start POST, 성공 START 후 공유3+배치+궁극기선택5. AccountScreenRuntime.cs 공유3종의 각각 GET, TrainLoadoutRuntime.cs·UltimateSelectionRuntime.cs 각 GET.
- Presentation/AugmentSelection/AugmentSelectionController.cs Open→ReadCurrent, Select→Execute→ReadCurrent. AugmentSelectionPorts.cs는 HTTP route/production adapter 없는 내부 경계라고 명시. 따라서 port3을 실제 API3으로 확정하지 않는다.
- Run/DurableRunClient.cs의 Restore는 로컬 journal; 정상 신규 Finish/Claim은 각각 POST1. 기존 확인 영수증/재시도 경로는 기준에서 제외.
- Presentation/MissionShell/RunResultRecovery.cs ReadRunResultsAsync의 lifecycle→CLAIMABLE일 때 reward-preview; ClaimRunRewardAsync의 claim→ReadRunResults→RefreshShared.
- LobbyRunLifecycle.cs 결과창 진입에서 RefreshAdSkipAsync 병행, 복귀에서 RefreshShared. AdSkipComposition.cs는 AdSkip.RefreshAsync와 RuntimeAds.RefreshAsync 둘 다 호출하며 각각 AdSkipAccountService.cs GET /v1/ad-skip, AdRuntimePolicyService.cs GET /v1/runtime-policy. 광고를 보지 않아도 현 경로의 조회2회 포함.

실측 수용 기준은 기존 SCENE-INFO-SOURCE:SERVER/UNITY 큐 범위 유지: 실제 transport/모드/원천 준비 상태, endpoint별 GET/명령, 논리요청/재시도, 정상/복구를 구별한 같은 시나리오의 전후 기록. 권한·멱등·계정/run 격리와 변경정보 일치를 함께 확인한다. 서버 내부 DB조회/CPU/전송량/지연 절감률은 이 수로 추정하지 않는다.

<a id="single-command-response-design-20261019"></a>
## 단일 확정 응답 상세기획과 메모리 모델 — 2026-10-19
사용자 승인 방향: 출격 확정1요청, 선택 확정·보상 수령 응답에 필요한정보 포함, 정상 성공뒤 추가조회0. 아래는 상세설계이며 wire필드명/버전·실제누락은 Server 검증 후 확정. 구현·실측 완료 아님. Plan정책/Server검증·응답/Unity반영·계측 책임, 기존 SCENE-INFO-SOURCE와 연결. 이전28→18은 최소변경 가정이지 최종목표 아님.

### 출격
준비화면은 기존 유효정보 표시. 출격버튼에서 작전/위험도/원편성·배치·궁극기선택 예상버전/원멱등키를 전달. 계정은 인증에서 결정하며 클라스탯·가격·보유량을 권위로 수용하지 않는다. 서버가 보유/해금/연료/비용/편성/진행run/콘텐츠지원과 기존복구 조건을 확인하고 원자적으로 비용처리·run생성·START snapshot·멱등응답을 저장한다. 검사와 쓰기 사이 상태변경 경합도 방어한다.
성공응답: runID/STARTED/콘텐츠·밸런스·출발버전/시각, 승인된 편성·배치·전투시작정보, 실제비용과 차감후잔액, 변경된 계정·활성run상태 및 각소유자 버전. 정적정의는 사전확인한 버전참조 사용, 전체아이템사전·이미지·공지·무관한계정목록 제외. 기존 전투에 필요한 원천을 공급/해석할 수 있는지 사전검증한다. 성공 후 account/wallet/loadout/ultimate GET0.
비용/편성이 사용자가 본 것과 다르면 과금없이 거절하고 재확인정보 제공. 자동으로 다른편성/비용 출격금지. 응답유실은 원키 재시도, 같은키 다른payload 거부·중복run/재차감0. 충돌/복구는 정상1회 예산 밖. 별도 preview는 새정상흐름에서 제거하되 기존preview필수 계약·옛pending·진행run·고정응답은 원버전 복구 유지, 원본삭제/재작성0.

### 선택·리롤
최초후보는 선택이벤트를 생성하는 기존전이응답에 포함 가능하면 공급. 미공급 경로의 최초조회까지 없어졌다고 주장하지 않는다. 리롤1회=명령1회, 응답에 새후보·후보버전·슬롯별남은횟수·변경재화 포함. 슬롯별무료3+추가1, 광고/구매는같은추가1의대체승인 원정책 유지. 리롤후후보GET0.
선택이벤트당확정1회. run/노드/이벤트/후보ID·버전/멱등키 검증, 폐기후보/타run 거부. 응답에 선택사실·적용상태·변경장비/능력/효과·소유자별전후버전·다음행동을 공급. CONFIRMED와 APPLIED 분리: 후행적용이면 확정된예약·적용ID/시점으로 표시하고 실제적용전최종능력으로위장0. 별도서버판정이필요한실효과경계는원계약확정필요, 필수명령삭제0. 정상확정후결과GET0. 선택없는노드에가짜요청0, 복수이벤트는각각집계. 정보창 클릭HTTP0.

### 최종 기본보상
작전종료 claim이며 노드작전장비획득과별개. run/preview/원키 검증, 원보상계획·권한·기한·미지급·광고경합을 원자검사/지급. 응답에 claim/transactionID, CLAIMED, 실제지급목록, 변경재화최종잔액·아이템최종수량, 관련진행/해금변경과각원버전·다음행동 공급. 기존 run-reward-settlement.ts에 일부 currency balanceAfter 있음: 재사용우선, 전재화/아이템/진행충족은미검증.
Unity는지급표시와owner반영을같은수락단위로처리, 임의덧셈대신서버확정후값/once-only delta 사용. 정상claim후 lifecycle/account/wallet GET0. 오래된영수증재생이최신잔액을롤백하지않도록원버전확인. 원응답유실·기준상태없음·버전불연속은원복구유지. finish결과응답완결/광고정책조회축소는별도범위이며이번승인만으로모두0으로계산하지않는다.

### 소유·수명·검증
계정Model/runModel/불변START snapshot/복구journal 각책임유지. View는표시/선택/스크롤만소유하고전체응답중복캐시금지. DTO검증→owner별버전대조/적용계획→수락→통지, 중간부분상태노출0. 부분갱신/전체대체/명시삭제를구분하고누락필드를0으로처리0. account/run교체·해제후응답격리. 처리후버퍼/JSON/DTO참조해제, GC즉시회수보장0. 필요한원영수증은기존디스크journal보존, RAM에전응답무한보관0. 서로의미가다른START와현재상태를절감명목으로합치지않는다.
검수: 실제transport 출격1/preview0/후속GET0, 선택확정1/후속GET0, 리롤1/후속후보GET0, claim1/후속GET0. 최초취득/노드거래/finish/광고/복구별도계수. 변조·충돌·동시클릭/기기·중복/유실·광고경합·구응답재생·버전불연속·CONFIRMED후행적용·계정전환 검증. port를외부HTTP로계산0. 기존완료기반큐규칙에따라Server계약검증뒤Unity소비/통합검수, 선행미완료큐신규등록0.

### 이론 메모리: 실측/실규모 추정 아님
클라이언트 기준. 정상상주=B(엔진/이미지/오디오등무관영역)+C(계정/run/START)+J(현재복구자료)+V(표시상태). 기존에도필요데이터를보유하므로같은정보·같은소유면상주는대체로동일. 새필드/영수증과중복제거분만차이. 피크=정상상주+H(구/신상태동시보유)+T(살아있는버퍼/문자열/JSON/DTO). 호출횟수만으로메모리감소계산불가.
임의설명예시: C+J+V=256KiB, 이전동시응답원문합48KiB, 이후단일응답64KiB, 임시live크기계수k=5를가정. B와공통H는표에서제외. 1KiB=1024bytes, k5는Unity보장/실측아님.
| 구분 | 전 | 후 |
| 정상상주 부분 | 256KiB | 256KiB |
| 임시 live | 48×5=240KiB | 64×5=320KiB |
| 부분피크 | 496KiB | 576KiB |
이경우피크+80KiB. 이전병렬응답합이96KiB라면전736KiB→후576KiB로160KiB감소. 어느쪽인지는실측필요. API가줄어도통합응답이커져피크가늘수있다. 매거래64KiB ASCII위주원문을UTF-16문자열로20개보관하면문자내용만약2.5MiB(객체헤더/JSON별도)가추가될수있어무한응답캐시금지.
같은기기/빌드/데이터/단계로payload bytes, live managed/native, 처리피크, GC누적할당/수집, 반복후retained를측정. reserved heap/프로세스/VRAM/누적할당을혼동하지않는다. 서버요청당메모리·DB조회는별도이며클라예시로서버부하절감률추정0.
<a id="single-command-approved-audit-20261019"></a>
## 단일 확정 상세 승인·Player 기본/리소스 메모리 검수 — 2026-10-19
사용자 #single-command-response-design-20261019 상세안 승인, Server/Unity 구현·실연결후API/메모리검수 지시. 출격확정1/새정상preview0, 선택·claim응답완결/정상후속GET0. 신규wire 필드명·버전은 Server가 기존계약대조로 확정, 기존진행run/원요청복구 유지. 기존 SCENE-INFO-SOURCE 부모에 승인추가, Server검증완료 뒤 의존Unity소비등록. Unity 독립계측·기준선은 지금 진행가능.
Unity 측정은 실제 Player 우선, Editor/Development build/profiler 부착/스냅샷 비용을 구분. 대상기기/OS/Unity·빌드버전/해상도·품질/그래픽API/캐시·사전로드/GC조건 기록. PC결과를모바일수치로전용0. 자동화fixture와실서버transport 분리.
기본상주 기준: 실제 bootstrap 안정직후(존재하는 시점만), 로비 로딩완료·유휴, 출격준비, 전투로딩완료, 선택/claim 처리순간피크, 종료후로비안정. '기본상주'가빈프로젝트엔진자체수치라는뜻아님. 측정가능한 런타임/코드·관리객체/네이티브·상시로드자산/렌더버퍼를 분리, 계측불가항목은미확인. 프로세스 private/working set, managed used/reserved, Unity native, GPU 추정/측정은 서로겹치므로합산해총량으로주장0.
리소스: 실제로드 texture/sprite atlas(UI·요원·배경), audio clip, mesh, animation/controller, material/shader, RenderTexture/렌더타깃·depth 등으로지원도구범위내분류. 디스크파일크기와CPU/GPU메모리구분, 읽기쓰기복사·mipmap·해상도·압축/오디오로드모드 및중복로드·참조보유 원인을큰항목부터대조. 공유atlas/texture중복계산0, inactive도참조보유면해제간주0. 실사용/상시공용/씬전용/검증불가 구분. 이번지시는조사/검수이며무관한자산삭제·압축변경·광범위최적화승인아님.
동일씬/편성/자산·선택/리롤수로변경전후비교,첫진입과warm반복분리. 기본반복5사이클의로비안정후retained·객체수추이와피크/GC할당을기록,증가시풀워밍/캐시/실참조누수를구분. 강제GC/Unload로정상문제를숨기지말고필요시별도진단실험표시. 과거동일빌드기준없으면현재baseline만실측,전수치를만들지않는다. 성능차이는프로파일러오버헤드포함여부명시.
API는기존모델의고정28/18에맞추지않고실제endpoint별외부HTTP/논리명령/재시도/local을계측. 노드거래/finish/광고/최초취득/복구별도,출격/선택/claim권한·멱등·버전/실UI일치와메모리함께검수. 구현부분완료와실연결검수완료분리,기존QA/인계에근거저장후총괄답장. 자동폴링0.

<a id="departure-change-notice-proposal-20261019"></a>
## 출격 준비 정보 변경 안내 상세안 — 2026-10-19 / 신규 제안
사용자6번상세기획요청. 승인single-command-response-design/approved-audit와04 리소스준비/복구원칙에연결. 정상출격START1회/사전서버preview0 유지. 아래변경분표시·수락범위세부는제안이며wire버전/필드명미확정·신규실행큐없음.

### 1. 사용자가 보고 누른 조건 고정
준비화면은검증된기존Model을표시. 출격클릭순간의작전/위험도·편성/배치·궁극기선택 및원owner버전·표시비용/비용정의참조를의도snapshot으로고정. 클라표시비용은서버검증용기대조건일뿐과금권위아님. 기존단일START 요청에기대비용/조건버전검사가빠져있으면Server계약보완필요;화면만고쳐안전보장0.
원로컬검사상필수자산/편성/명령미결이면기존사유로출격불가. 서버가자기원장/규칙으로재검증하고허용된동일조건일때만원자과금·run생성. 변경조회GET를출격전마다추가하지않는다.

### 2. 변경 분류와 화면 반응
| 차이 | 처리 |
| 비용인상·인하 | 새비용재확인,자동수락0 |
| 요원/슬롯·장비배치·궁극기선택변경 | 이전/현재식별가능표시,원확정편성과일치할때만새출격 |
| 전투에영향주는성장/적격·콘텐츠규칙변경 | 관련변경요약/새검증원천수락후재확인,가짜스탯차이계산0 |
| 보유연료만변경·비용동일·다른조건동일 | 충분하면서버현재잔액으로정상1회허용,부족하면불가/현재값표시 |
| 무관공지·외형표시·다른아이템·시간표시변경 | 출격조건을무효화하지않으면불필요팝업/재조회0 |
| 이미진행작전/원출격결과미확인 | 신규출격금지,기존작전확인/복구동선 |
| 점검/해금/콘텐츠미지원 | 원불가사유표시,사용자확인으로우회0 |
전체account revision변경만으로모든무관변경을출격차단으로확대하지않는다. 세부필드검증불가시거짓동일판정대신현재원계약의충돌처리유지. 콘텐츠version이변해도기존run snapshot은원버전유지.

### 3. 거절 응답과 변경 안내
서버가이번요청을미적용/미차감으로확정한경우원요청receipt/명확거절사유와다시표시할최신관련값·각원version·허용다음행동을반환. 단순409/timeout만으로미차감선언0. 이미원START가성공한재생이면변경안내가아니라같은run복구. 내부예외/응답형식오류는결과미확인복구로분리.
권위있는変更값이충분하면기존Model갱신·준비화면의변경된항목과출격버튼인근요약을표시. 별도전체화면/중첩확인팝업추가0. 예시문구: '출격 조건이 변경됐습니다. 변경 내용을 확인해 주세요.' 비용행은이전값→현재값,편성은변경슬롯/요원이름,불가조건은그이유를직접표시. '연료는차감되지않았습니다'는해당원요청미차감확정때만표시. 재시도중성공여부불명때는사용금액을클라가되돌리지않는다.
원인표시가여러개면한변경목록으로묶고거래불가사유를우선표시. UI색만으로전후구분0. 서버미공급값은미확인으로남기고필요한원조회로보완,불완전정보에재출격버튼활성화0. 이전값은현재시도비교용작은snapshot으로만보존,전체계정복사/변경이력무한누적0.

### 4. 확인·취소·재확정
다시누르는'변경된조건으로출격'이명시재확정이며별도확인팝업뒤START를또누르게하지않는다. 현재편성/작전수정도가능,새변경은원Model/버전검증과연결. 취소/뒤로가기는준비화면을벗어날뿐차감/run생성없음;이미진행중요청취소를서버거래취소로오인0.
서버미적용거절이확정된뒤수정된의도로새요청키사용. 응답유실/결과불명은절대로새키를발급해출격하지않고원키조회/재시도. 같은키다른본문거부. 새확정시다시조건이바뀌면다시거절·최신변경표시,자동수락루프/상시폴링0.
처리중중복출격차단,같은프레임다중클릭명령1회. 늦은응답은원계정/시도세대로분리,최신Model을옛version으로롤백0. 로컬변경을막은상태에서도서버외부변경경합은원자검사필수.

### 5. 자산준비·경로/API 검수
변경된편성/콘텐츠로예열세대를교체,현재공유자산은사용자있는동안유지·구작업취소. START성공뒤필수자산부족은동일run준비/복구로처리,새START/자동환불0. 준비완료표시는실로드검증과일치해야한다.
정상동일조건START1. 충돌1회후사용자가재확정하면거절1+성공1=2요청;최신정보충분하면변경GET0. 유실/정보미공급/복구는別계수. 비용변경감지하려고preview API를되살리지않는다.
시험: 비용상승/하락·연료충분/부족·무관계정변경·편성/성장/콘텐츠변경·동시기기·이미진행run·점검·再변경·키본문충돌·유실원키복구·같은클릭중복·취소/늦은응답·자산재준비. 미동의조건출격0/오차감0/정상1회·변경응답직접반영·가짜미차감문구0/이전현재읽힘검수.
Server권한/관련기대조건/거절payload, Unity원Controller와Model/表示/세대·資産수명,Plan변경기준/인수. 실제소스전수/실START/Player검수미실행. 본문새분류·표시안은승인전제안,기존승인단일START/필수검증업무는멈추지않는다.

<a id="departure-change-notice-approved-20261019"></a>
## 출격 준비 변경 안내 상세 승인 — 2026-10-19
사용자 #departure-change-notice-proposal-20261019 상세승인·Server/Unity작업명시. 해당제안대기해소. 기대조건/원자검사·비용상하/편성등중요변경재확인·무관변경불필요차단0·미적용확정응답으로변경표시·유실원키복구/거절후새키·정상START1/preview0·자산세대연결채택. 실제wire/Player검수완료아님. 기존단일START업무재사용,운영경제/배포승인없음.

<a id="data-lifecycle-code-audit-20261021"></a>
## 로그인 이후 데이터 조회·보관·해제 코드 점검 — 2026-10-21

사용자 요청: 실제 코드의 중복 상태/불필요 재조회 개선 후보, 로그인부터 조회 시점·초기화·메모리 해제 경로 정리. Plan 소유 분석이며 런타임 수정·타 역할 작업 지시 없음. Unity Scripts의 아래 연결 경로를 정적으로 읽었다. 전체 프로젝트/모든 API/모든 프리팹의 실행 경로 전수 감사나 Player 측정 결과가 아니다. 조회 실제 시각·GC/CPU/GPU 회수량·호출 실측은 이번에 수집하지 않았다. 새 정책 승인으로 승격하지 않으며 기존 single-command-approved-audit-20261019 구현 개선 근거로 사용한다.

### 1. 실제 데이터 원천 및 조회 시점
경로 기준 `D:/Project/LASTRAIL/unity-project/Assets/_LASTRAIL/Scripts/`.

| 시점 | 코드 및 원천 | 보관/재사용·초기화 |
|---|---|---|
| 자동 로그인 | Auth/PlayerAuthOwner.cs ReadStoredAsync → 보안 저장소 Read → refresh → CommitAsync | 인증 성공 저장 후 Session 교체. 계정 변경 때 Generation 증가. SignOutLocally/Dispose에서 Session=null; 보안 저장 데이터 삭제와 다름 |
| 비밀번호 로그인 | Auth/GameApiPlayerEntryClient.cs /v1/auth/password/login; PlayerEntryRuntime.Login | 저장된 계정과 다른 계정 전환은 별도 절차 요구. 로그인 응답 후 동일 Prepare 경로 |
| 로그인 준비 | PlayerEntryRuntime.PrepareAsync: GET /v1/profile → PrepareLobbyDataAsync: GET /v1/story-progress | Profile과 PlayerLobbySnapshot(Story, Catalog) 보관. Snapshot.Profile은 동일 객체 참조이며 복제본 아님. 재준비 시작 시 LobbySnapshot=null, 인증 무효화 시 Profile=null/CloseLobby |
| 로비 연결 | PlayerEntryRuntime.BindLobby → RefreshAccountWallet → RefreshSharedAsync | GET /v1/account/state, /v1/wallet/crystals, /v1/growth/config 병렬. BindLobby 호출마다 강제 refresh 경로이며 실제 재바인딩 빈도는 미측정 |
| 출석·임무 진입 후처리 | RefreshAccountWallet에서 EnterMonthlyAttendanceAsync → EnterRecurringAsync | 출석 current discovery(필요시)·monthly 조회, 조건부 자동출석; recurring-login 명령, sync 명령과 상태 조회. 날짜/보상 검증 경계는 유지 필요. 성공 응답 뒤 RefreshShared 반복 경로 존재 |
| 재화 정보 열기 | LobbyCommandDeck 통화 버튼 → EnsureSharedAsync | Ready면 요청0, 조회 중 Task 재사용. RefreshShared 호출자는 강제조회라 별도 |
| 모집 열기 | LobbyDestinationPages.LoadRecruitment | shared 3 GET + banners 목록 GET + 선택 배너 상세 GET(배너 존재 조건). 계정 상태가 유효해도 반복. 모집 결과 배치 상세는 원응답 완결 여부 대조 없이 삭제 금지 |
| 열차/출격 열기 | LoadTrain, LobbyDeparture.ReadDepartureForDisplay | shared는 Ensure 재사용 가능; loadout은 항상 GET, 출격은 ultimate도 항상 GET. 명시 refresh와 단순 화면 진입 분리 미완성 |
| 기록 열기 | StoryArchiveView.Create → RefreshRead → StoryArchiveReadModel.RefreshAsync | GET /v1/story-progress 재조회. 로그인 LobbySnapshot.Story와 별도 State에 보관, 공유 입력 경로 없음. 매번 새 View 생성 시 조회 |
| 편성/성장/장착/보급 등 성공 | AccountScreenRuntime.SubmitAsync:255–261 | Wallet/Offline 무효화, Account Loading, shared 3 GET + offline 조회. 종류별 loadout/ultimate/모집 추가 조회. 편성 저장까지 성장 정적 설정/보급을 일괄 재조회 |
| 출격 명령 후 | DepartureRuntime:131 | refresh 조건에서 shared 3 + loadout + ultimate GET. 정상 경로 응답 완결 소비 미완성 |
| 증강 | AugmentSelectionRuntime/Controller/Port 기존 분석 참조 | 원 선택 snapshot과 원응답/저널 사용. 획득 확정과 전투 applied 분리. 모든 실제 Player 연결 완료로 해석 금지 |
| 전투 요원 정보 | Presentation/Battle/OperatorInformationController | 주입된 committed-memory reader 사용, 동일 대상 선택 재조회 생략. Unbind에서 cancellation/target/snapshot/accepted/revisionFloors 정리. 인터페이스 Task가 곧 HTTP라는 뜻 아님 |
| 결과 열기 | RunResultRecovery.ReadRunResultsAsync | 로컬 Runs.RestoreAsync + GET lifecycle + CLAIMABLE일 때 GET reward-preview. 저장소 읽기와 API를 구분 |
| 보상 수령 후 | RunResultRecovery.ClaimRunRewardAsync | 기존 ClaimAsync/RetryAsync 후 ReadRunResultsAsync + shared3 GET. CLAIMED 정상 상태면 lifecycle1+shared3; preview 추가 여부는 상태 조건. ClaimCompleteAsync는 Scripts 검색에서 선언 외 호출 미발견 |

### 2. 메모리 수명: 확인된 해제와 잔류
- AccountScreenRuntime.SynchronizeSession: 세션 객체 참조 또는 Generation 변경 시 Account/Wallet/Growth/Offline 등 null, 요청 Task 참조 null, 각 하위 Reset; 늦은 응답은 Current/요청번호 검사로 게시 차단. 같은 계정 token refresh의 새 Session 객체도 이 초기화에 해당. 인증 자격 변경과 도메인 데이터 무효화를 분리할 후보이며 권한 변화/계정 경계 검증 유지 필수.
- AccountScreenRuntime.Dispose: 주요 데이터 null, Changed 등 구독 제거, 하위 Dispose/Reset. `accountRead/walletRead/growthRead`, owner, Formation 등은 이 메서드에서 명시 해제하지 않는 항목도 존재. 객체 자체의 참조 수명과 완료 Task 캡처는 Memory Profiler로 확인해야 하며 누수 확정 아님.
- LobbyDestinationPages.Close: SetActive(false); OnDisable은 장비 표시 연결 정리. content/art/chrome/backdrop.texture/skins는 일반 Close에서 전부 비우지 않는다. Clear/Rebuild 및 OnDestroy에서 생성 Sprite 정리. 숨겨진 화면 보존은 재사용 정책이 될 수 있으나 비활성=메모리 회수는 아니다.
- LobbyCommandDeck은 OnDisable에서 하위 Unbind, Account.Changed 구독은 OnDestroy에서 해제. 비활성 시에도 AccountChanged 호출 가능 경로가 있으며 일부 함수는 내부 표시 조건으로 막는다. 불필요 UI 처리량은 실제 측정 필요.
- PlayerEntryRuntime.InvalidateAuthentication: Owner.SignOutLocally → AccountScreens.SynchronizeSession, Profile=null, CloseLobby. CloseLobby만 호출할 때는 Profile 유지. Dispose도 Profile/세션 체크용 필드 전부 null 처리하는 방식이 아니므로 살아 있는 외부 참조 여부 대조 필요.
- DurableRunClient.Operations getter는 계정/Generation 불일치 시 빈 목록을 노출하지만 private operations를 그 시점에 비우지는 않는다. 다음 Publish/실패/Dispose까지 이전 객체 보관 가능. 표시 격리와 참조 해제를 구분한다.
- DurableRunClient.HandoffAsync는 durableSink 성공 후 RemoveConfirmedAsync 및 메모리 목록 삭제. 이번 Scripts 검색에서 `.HandoffAsync(` 소비 호출을 발견하지 못했다. EncryptedRunOperationJournal은 rows32 제한 및 파싱 문자열3Mi문자 제한이 있으므로 무한 보관이라고 표현하지 않는다. 실제 일반 종료/보상 경로의 인계 연결 여부와 용량 도달 처리가 우선 확인 대상. 복구 원기록 임의 삭제 금지.
- managed null/구독 해제/Destroy 요청은 회수 가능 조건이며 즉시 GC·프로세스 RSS 감소·GPU 텍스처 해제를 보장하지 않는다. 저장된 저널은 디스크 데이터이며 메모리와 별도다.

### 3. 개선 우선순위 (분석 제안)
1. 복구 기록의 완료 인계 소비 연결과 계정 종료 시 private cache 참조 정리: 원영수증 보존 후 안전한 제거, 32건 제한 도달/다음 작전 진입 검증. 코드 전수 호출 검색 범위를 넘는 동적 연결은 미확인.
2. 화면 진입의 Ensure화: 스토리 단일 owner로 로그인 데이터 공급; 모집 shared/열차loadout/출격ultimate를 버전·실제 무효화 기준으로 재사용. 이벤트 종료/날짜/배너 갱신 계약 보존.
3. 명령 응답 소비: 현재 FormationReceipt는 id/preset/kind/revision 중심이며 활성화 응답 operators도 보존하지 않음. 필요한 확정 DTO를 owner에 일괄 반영한 뒤 통지하고 불필요 shared/보급 GET 제거. 응답 부족을 그대로 둔 채 GET만 제거하지 않는다. 보상 ClaimCompleteAsync 실제 소비 연결도 동일.
4. account/wallet/growth 강제조회 중복 합치기: Ensure는 in-flight 재사용하지만 Refresh는 새 요청 시작. 같은 유효 범위의 동시 요청 공유와 refresh 이유 명시; 요청번호는 역순 게시만 막으며 네트워크 요청 자체를 줄이지 않는다.
5. 인증 세션과 데이터 버전 분리: 동일 계정 정상 토큰 갱신에서 유효 데이터 보존, 계정/권한 전환·불명확 상태에서는 즉시 격리/무효화. 오래된 응답 차단 유지.
6. UI 갱신 범위 축소: Account.Changed → LobbyCommandDeck.AccountChanged → Pages.RefreshAccountView → 열려 있는 여러 페이지 Rebuild → content/Sprite 재생성. 로딩/부분 응답별 광범위 rebuild 대신 관련 값/목록만 갱신. CPU 할당 후보이지 측정된 누수 아님.
7. JSON 복사 비용: RunRewardReview.Lines/VerifiedRunReceipt, LastRecurringGrant 등 getter의 DeepClone은 보호 목적이 있다. 불변 DTO 또는 1회 표시 변환을 검토하고 공유 가변 JSON 노출로 대체하지 않는다. START snapshot·현재 전투 상태·원요청 저널은 역할이 달라 정상적 복수 상태다. Profile의 두 참조와 Account/별도 crystal wallet도 복제 원장으로 단정하지 않는다.

### 4. 조회 시각 및 실제 메모리 검수 공백
검사한 owner들에서 데이터별 통합 fetchedAt/invalidatedAt/releasedAt 이력을 확인하지 못했다. 응답 serverTime은 서버 기준 시각이지 클라이언트 수신/해제 시각이 아니다. 따라서 이번 결과는 '어떤 동작에서 조회/초기화하는지'이며 '실제로 몇 시에 몇 MB가 비워졌는지'의 로그가 아니다.

개발 서버 전용 후속 계측 후보: 데이터 키·소유자·익명 scope/Generation·버전·조회 사유·요청/응답시각·memory-hit·in-flight 합침·무효화/참조 해제 사유·목록 개수. 토큰/원응답/사용자 식별정보 로깅 금지. 기존 진단을 확장하며 실서비스 사용자 UI 노출 금지. 로그인→로비→같은 화면 5회 왕복→변경 명령→출격/선택/보상→인증 무효화에서 논리 요청/전송/재시도, managed live/alloc/peak/반복 잔류, texture CPU/GPU를 분리 측정. 변경 전후 같은 환경·응답 크기로 대조. 승인된 기존 계측 정책을 유지하며 이번 턴 신규 실행/Player 측정 없음.

<a id="data-lifecycle-approved-20261021"></a>
## 데이터 재사용·조회/해제 계측 개선 승인 — 2026-10-21
사용자 위 data-lifecycle-code-audit-20261021 개선안 전체 승인 및 Server/Unity 구현·개발모드 계측·실제 API/메모리 전후 검수 지시. 버전 data.lifecycle.v1.20261021. 분석 당시 미확인은 사실 확정으로 승격하지 않는다. 기존 single-command-approved-audit-20261019 작업에 결속하며 중복 owner/진단 시스템을 만들지 않는다.

- Server: 기존 명령 응답의 최종 상태/변경분·소유자별 version·영수증을 확인해 부족분만 보완. 편성/성장/장착/보급/출격/작전보상 관련 불필요 후속 조회를 제거할 소비 계약 공급. 권한·원자성·멱등·부분 미공급/삭제 구분·구버전 복구 보존. 스토리/배너/성장 설정의 실제 갱신 원천과 버전/무효화 조건 명시. 원영수증 완료 인계 계약 확정, 복구 전 삭제 금지. 현재 지원되는 실제 endpoint/필드 기준으로 구현하고 임의 운영 경제 실행 없음.
- Unity: 현재 코드 재확인 후 단일 상태 소유자/로그인 Story 재사용, 화면진입 Ensure화, 강제조회 중복 합침, 동일계정 token 갱신과 데이터 유효성 분리, 명령 응답 일괄 반영/ClaimComplete 소비, 종료된 작전 기록 durable 인계·정리, 계정 종료 private cache 및 화면/구독/텍스처 수명 정리. 필요한 snapshot/저널/불변 복사 보존. 광범위 페이지 Rebuild 및 getter 반복 복사를 변경 영향 범위에서 줄인다. 타역할 계약 대기 부분만 보류하고 선행 없는 독립 구현 계속.
- 계측은 기존 DEV 서버 판정에 종속한다. 빌드 debug flag나 사용자 toggle만으로 PRD에서 노출하지 않는다. DEV에서 데이터 키/owner/익명 scope·generation/version, fetch reason·request/receive/invalidate/reference-release 시각, cache hit/inflight 합침/전송/재시도, 레코드 개수·수명 기록. 기존 진단 UI에 모바일에서도 전체 복사·텍스트 내보내기 가능한 요약 제공. 토큰·개인식별값·원응답 전체 기록 금지. PRD 일반 화면/개발 상세 추적 노출0, 기존 중요 오류 서버 로그 정책은 유지. 기록은 유한 버퍼와 명시 수명으로 관리하여 계측 자체의 누적 메모리 증가 방지.
- reference-release, Unity Destroy 요청, 실제 managed 객체 회수, texture CPU/GPU 메모리를 서로 다른 사건/측정으로 표시. 정확 수집 불가능한 항목은 미측정. 서버 time을 client receive time으로 대신하지 않음. 시간/이벤트 순서는 단조 시계와 측정 세대 기준도 보존.
- 변경 전 baseline을 먼저 저장하고 같은 환경/시나리오/데이터 크기로 변경 후 비교: 로그인→로비→모집/열차/출격/스토리 화면 각5회 왕복→허용 편성/성장/장착 변경→출격/증강/결과/보상→인증 무효화. 실제 경로 없는 행은 미실행 표시하고 합성시험과 분리. 원래 코드 baseline이 확보되지 않았으면 전후 감소율을 발명하지 않는다.
- API는 endpoint별 논리 명령/실제 전송/재시도/복구/로컬 저장소 읽기를 구분. 메모리는 Unity 기본상주·managed live/누적할당/peak/반복잔류 및 texture CPU/GPU를 나눔. 일반 GC 상태와 강제 GC 진단 결과를 혼동하지 않는다. 정확 회수 시점을 측정하지 못하면 참조 해제까지만 기록.
- 정상/동시조회/역순응답/실패/원키 재시도/세션전환/재바인딩/해제 후 응답/원영수증 보존을 검수. 서버·클라이언트 상태 일치와 기존 기능 보존 필수. 실제 Player/API trace/메모리 증거와 잔여를 기존 역할 QA/인계에 기록. 완료는 문서/단위테스트만으로 선언하지 않는다.
- 컷씬·음원·애니메이션 제작 제외. 기존 증강/흡혈 실제 연결 잔여를 완료로 처리하거나 폐기하지 않는다. 큐는 자기 독립 구현부터 실행, 양측 계약/소비 준비 후 통합 검수 등록. 자동 폴링 없음.


<a id="data-refresh-conflict-approved-20261021"></a>
## 갱신 중 화면 유지·버전 충돌 처리 승인 — 2026-10-21
사용자 직전 상세안 승인. data.refresh-conflict.v1.20261021. 기존 data.lifecycle.v1.20261021 및 응답완결 계약의 화면/수락 세부 보완.
1. 유효한 기존 데이터는 즉시 표시. 명시 갱신 중 기존 내용·선택·스크롤 유지, 갱신 영향을 받는 확정 입력만 잠금. 명령 중 마지막 확정값 유지 및 같은 명령 중복 차단. 관련 owner 상태를 일관 반영한 후 통지/입력 재개. 갱신 실패 때 기존 정보 유지와 필요한 작은 안내, 최신성이 필요한 실행만 제한. 계정/권한이 달라진 정보는 표시 보존 예외로 격리. 전투 요원/장비 정보창은 메모리 즉시 표시 기존승인 유지, 로딩·재시도 UI 신설0.
2. 각 owner의 실제 revision 계약으로만 비교. 늦은 낮은 버전 응답은 현재 상태를 되돌리지 않음. 동등버전·동등내용은 중복 반영0. 동등버전인데 의미있는 내용 불일치면 일관성 오류로 격리/원복구; 시각 등 비상태 필드를 무조건 비교하지 말고 Server가 상태 비교 범위를 명시. 서로 다른 owner revision은 크기 비교0.
3. 미공급/명시삭제/전체대체 구분. 기준버전 불일치 delta 임의적용0, 해당범위만 기존복구. 장착/능력 등 결속된 변경은 필요한 정보 검증 후 함께 게시. 최신 독립owner 상태를 과거 거래묶음으로 롤백0. 서버 성공 영수증 보관과 최신표시 적용 수락은 구분.
4. 명령 응답유실은 실패 확정 아님. 원키 복구 및 '수령 결과 확인 중' 등 해당행 안내, 새명령 중복0. 과거 영수증의 지급내역은 보여줄 수 있으나 과거 잔액으로 최신잔액 덮어쓰기0. 복구에 필요한 조회는 유지, 충분한 정상응답 후 중복GET0.
5. 수용시험: 변경성공 뒤 이전GET 도착, 중복영수증, 갱신실패 시 목록/스크롤 유지, 계정전환뒤 응답격리, 누락필드 0/삭제 금지, 정상왕복추가GET0. 개발서버에서 폐기/충돌/복구 사유 기록, 사용자 화면에 기술버전/메모리 노출0. API·메모리 전후 실제Player 검수에 위 사례를 합치며 측정전 개선수치 주장0.
Server 기존필드/원자·권한·멱등·복구와 수락계약 소유, Unity owner수락/표시·입력/수명 소유. 자기독립 구현은 진행, 미공급 계약 부분만 대기. 기존 DATA-LIFECYCLE 부모/후속조건 재사용. 새 날짜/이벤트만료·앱복귀 상세안은 아직 제안으로 별도 승인 필요.


<a id="data-time-resume-approved-20261021"></a>
## 날짜·이벤트 종료·앱 복귀 선택적 갱신 승인 — 2026-10-21
사용자 직전 상세안 승인. data.time-resume.v1.20261021. 기존 data.lifecycle 및 data.refresh-conflict 보완이며 시간경계가 무관한 전체계정 재조회 사유는 아니다.
- 출석/일일임무 기준일 변경은 해당 상태만, 이벤트/배너 기간 종료는 해당 이용가능 상태만 갱신. 기준일/종료시각은 콘텐츠별 서버계약 사용, 임의 기기자정/공통TTL/새 리셋시각 금지. 요원/장비/스토리 등 유효정보 유지.
- 현재 열린 화면은 관련 항목 갱신, 숨겨진 화면은 갱신필요 표시 후 실제필요 때 취득. 같은 데이터 요청 합침. 기존목록/선택/스크롤 유지; 기간민감 확정 입력만 제한. 참여종료/승인된 작전완료유예/보상수령마감은 각각 판정, 참여종료로 수령권 소거 금지.
- 앱복귀: 현재계정·인증 유효성 확인(이를 무조건 새조회로 해석하지 않음) → 미확정 원명령 원키복구 → 날짜/기간경계 식별 → 현재화면/실행에 필요한 정보만 취득 → 일관반영 뒤 입력재개. 단순복귀마다 로그인보상/전체조회 반복0. 필요한 인증갱신/재접속 검증은 유지. 갱신된 원명령 응답이 충분하면 뒤따른 같은GET0.
- 서버시각과 신뢰가능 경과시간으로 표시, 기기시간 변경으로 권한 생성0. 앱정지/프로세스복원으로 경과시간 기준이 불확실하면 정밀하다고 가정하지 않음. 기존정보 표시 가능하되 기간민감 실행은 서버판정. 시간표시로 최종입장/지급판정 대체0. 기존 진행run START 정책 보존.
- 수용조건: 날짜경계 전후 선택적조회, 참여종료 후 허용수령, 동시화면 요청합침, 명령응답유실 복귀 중복지급/소비0, 기기시간조작 권한불변, 반복복귀 타이머/구독/진단 누적방지. 기존 DEV서버 한정 계측으로 query reason/경계/캐시hit/복구 분리, 민감값0. 실제API·메모리 전후 Player검수 지속, baseline/합성/실측·미실행 구분.
담당 Server는 현재콘텐츠 시간/버전·기간권리 원천과 복구 계약, Unity는 무효화/화면수명/복귀순서 및 소비 소유. 독립작업 즉시진행, 계약미공급 부분만 대기. 기존DATA-LIFECYCLE 후속AND/고정키 재사용. 앞큐의 날짜/복귀 미승인 표기는 이 승인으로 대체. 다음 저메모리 캐시정리안은 신규제안이며 아직 미승인.


<a id="data-cache-pressure-approved-20261021"></a>
## 메모리 압박 캐시 정리·재진입 복원 승인 — 2026-10-21
사용자 직전 상세안 승인. data.cache-pressure.v1.20261021. 기존 DATA-LIFECYCLE 구현/계측 보완, Unity 소유.
- 정리순서: 사용종료 임시변환/진단초과분 → 닫힌화면 표시객체·재생성목록 → 사용자가 없는 미리보기/배경 등 자산참조 → 재취득가능 비활성 상세캐시. 선택ID/스크롤은 작고 유한한 복원정보로 보존. 현재계정·작전 필수상태, 전투상태, 진행명령/미확정영수증·복구원본은 정리대상 아님. 공유자산은 마지막 소비자 반환뒤 소유자가 해제. 기존 콘텐츠 pin/승인캐시 정책 보존, 디스크원본 삭제 권한 추가0.
- 재진입: 유효 owner메모리→필요 로컬자산 재로딩→데이터 부재/무효화 때 해당범위만 기존조회. 화면/texture 해제를 계정·장비 재조회사유로 삼지 않음. 선택ID 존재·권한 유효하면 선택/스크롤 복원, 없으면 해당선택만 해제. 정리된 화면의 과거 응답이 UI 재생성하지 않도록 수명세대 격리.
- 평시 재사용 유지, 메모리압박 신호에서 단계정리. 연속신호 중복정리/이중반환0, 재사용 직전 자산을 반복해제·재로딩하는 동작 방지. 임의 공통 MB한도 확정0, 기기실측으로 후속 결정. 전투중 즉시가능 참조정리와 안전한 전환시점의 무거운 작업 구분, 강제GC 상시실행을 기본으로 삼지 않음.
- 기존 DEV서버 전용 유한진단에 정리사유/대상개수/사용중 보존사유·복원원천(메모리/로컬자산/API)을 기록. 참조해제 요청과 실제 managed/texture 회수는 별도, 측정불가 표시. 사용자일반UI에 기술메모리정보 노출0.
- 실제Player 검수: 5회왕복 잔류/texture 증감, 정리후 선택·원상태 복원, 유효데이터 추가GET0, 해제중응답 UI부활0, 전투/복구/공유자산 보존, 메모리전후와 재진입지연 함께 비교. baseline/합성압박/실기기신호/미실행 구분. 무제한 캐시 제거·전체 재작성 권한 아님.
Unity 기존데이터/리소스 소유자 수명 경로로 구현. Server와 Unity의 DATA-LIFECYCLE 실제API/메모리 전후검수는 계속. 기존 후속AND/고정키 재사용, 미디어제작 제외. 다음 로그인 종료/계정전환 상세안은 신규제안이며 기존 인증기능 구현완료로 간주하지 않는다.


<a id="data-account-exit-approved-20261021"></a>
## 계정 종료·전환 데이터 수명 승인 — 2026-10-21
사용자 직전 상세안 승인. data.account-exit.v1.20261021. 01의 영속logout/account switch 기존 미완료와 로컬무효화 경계 유지, 승인 자체는 구현완료가 아니다.
- 계정종료: 새변경입력 차단→진행요청 원키/복구기록 안전저장 확인→구화면 구독/작업 해제 및 늦은응답 게시 차단→계정데이터/선택/전용캐시 참조정리→해당 인증계약 로그아웃과 로그인화면 이동. 보안 저장중 작업을 중단하지 않는다. 로그아웃은 서버처리된 거래취소가 아니다. 저장/로그아웃 실패를 성공으로 표시하지 않는다.
- 이전 재화/요원/장비/스토리표시 및 계정전용 선택/스크롤/상세창 초기화. 미확정원요청/영수증은 계정별 보안저장, 진행작전은 기존복구정책 보존. 공용자산/정적정의·기기설정은 실제소유범위대로 재사용. 게스트 복귀수단/연결정책 보존, 계정삭제/병합/조용한교체 권한 추가0.
- 늦은 원계정 응답은 새계정 Model/UI에 반영0. 필요한 원영수증 저장은 원계정/원요청 검증 후 그 원저장소에만 수행하며 로그아웃 이후 임의 인증재전송0. 새계정으로 구계정명령 재전송0. 원계정 재인증 때 그계정 기록만 복구.
- 동일계정 정상 토큰갱신은 전환이 아님. 계정/권한/데이터 유효하면 캐시보존; 권한변경/만료확인 시 영향범위 격리. 기존인증 검증/철회 계약 준수, 임의 새인증체계/TTL0.
- 실제검수: A→B 구정보 프레임노출0, A응답 B오염0, 미확정 지급 원키복구, 반복전환 구객체/구독/texture잔류, 정상token갱신 전체조회0, 저장실패 성공위장0. 계정별 journal/키 소유·이전세대 callback 검증. DEV서버 전용 기존 유한진단으로 API/메모리 전후 baseline 비교.
- 사용자 추가지시: Unity는 공급된 #data-lifecycle-time-server-supply-20261021의 실제서버응답을 연결해 실제Player API/메모리 전후 검수까지 계속한다. 로컬 HTTP 시험 memory-only/재시작새trial 및 지원allowlist 한계를 보존, 이를 영속 재시작복구나 운영START 증거로 대체0. Server 지원부분 재작성0, 미지원 부분만 분리.
Server는 인증/원계정 영수증 복구·권한경계 계약과 필요구현, Unity는 입력/수명/계정캐시/응답격리·실소비 소유. DATA-LIFECYCLE 후속조건/고정키 재사용. 신규 로컬저장실패 세부 UX안은 아직 제안.


<a id="data-save-failure-approved-20261021"></a>
## 저장 실패 대응 승인 — 2026-10-21
사용자 직전 저장실패 상세안 승인. data.save-failure.v1.20261021. 기존 DATA-LIFECYCLE/계정종료·원키복구 보완.
- 전송전 원요청저장 실패는 전송0, 저장재시도 안내. 응답유실은 저장된원키로 복구. 서버성공확인 뒤 영수증저장 실패는 서버성공 유지·로컬저장 재시도. 저장완료 후 화면실패는 확정상태로 화면만 재구성. 재실행시 기록읽기 실패는 원기록보존·관련변경 제한.
- 보상성공을 실패로 뒤집지 않음. 사용자 문구 '보상은 지급됐습니다. 기기 기록 저장을 다시 시도해 주세요.'는 검증된 실제지급확인에만 사용. 메모리 원응답있으면 저장만재시도/추가HTTP0, 원응답없으면 원키 서버복구. 새수령/새키0.
- 조회/설정 등 독립기능 허용. 해당복구기록 덮어쓰는 후속명령 제한. 필수작전선택/전투저장 실패는 기존 안전경계에서 진행보류. 원기록/계정 자동삭제0. 안전저장 자체실패를 해소했다는 이유로 사용자 요청하지 않은 명령 자동실행0.
- 저장재시도 단일실행, 무한자동반복/응답복사누적0. 대기원응답은 일반캐시정리 제외. 저장확인후 불필요임시참조 해제. 프로세스종료 이후 전송전 영속원요청으로 복구; memory-only 시험은 이 증거가 아님. 기존계약이 원요청영속복구를 제공하지 않으면 그행 미지원으로 명시하고 담당구현 필요.
- 수용시험: 전송전 저장실패 서버0회, 서버성공후 로컬재저장 추가API0회, 재실행 원키복구 중복지급/효과0, 공간부족/쓰기실패/손상 구분, 반복재시도 객체/작업/구독누적0, 실제처리와안내일치. DEV 기존유한진단으로 기록, 민감본문/토큰0. 검증된서버응답→원상태→로컬영속→화면의 단계구분.
Server 기존멱등/원영수증·재생/유효기한·충돌 계약 공급/필요구현, Unity journal/저장오류/제한/화면/원키복구 소유. 실제서버응답 연결 후 Player API·메모리 전후검수 계속, baseline/실서버/합성/미실행 구분. 기존DATA-LIFECYCLE 후속키 재사용, 신규 네트워크재시도 세부안은 아직 미승인. 미디어제작 제외.


<a id="data-network-retry-approved-20261021"></a>
## 네트워크 복구·자동 재시도 1회 승인 — 2026-10-21
사용자 직전 상세안 승인. data.network-retry.v1.20261021. 기존 DATA-LIFECYCLE·저장실패·원키복구 보완. 게임 데이터조회/변경명령 범위이며 콘텐츠다운로드의 동의·전송망/gate 계약을 임의변경하지 않는다.
- 조회단절은 유효 기존정보 유지/필요조회만복구. 변경전송후응답없음은 실패단정0·원키결과복구. Wi-Fi/모바일전환 자체로 전체조회/명령재전송0. 복수화면 동일조회 합침. 인증만료는 기존인증owner 복구에 합침, 화면별독립로그인재시도0. 명확한서버거절은 분류에맞게 처리, 무조건반복0.
- 같은 논리요청의 연결복구 자동시도는 최대1회. 최초전송과 별도 집계, 연결신호반복/화면재진입/인증복구 또는 transport 상하위 중첩으로 예산초기화·곱셈재시도0. 자동실패후 관련화면 수동 '다시 확인' 제공. 수동연타와 진행중자동시도는 하나에합침. 명령 수동복구도 원키/원payload 유지. 서버가 제공하는 유효한 재시도대기 조건 준수. 재생보장 없는 명령은 임의재전송하지 않고 원계약복구 유지.
- 기존목록/선택/스크롤 유지. 조회실패 '연결을 확인한 뒤 다시 확인해 주세요.', 결과불명확 '처리 결과를 확인하고 있습니다.', 복구실패 '결과를 아직 확인하지 못했습니다.' 등 해당기능 가까이에 안내. 결과불명확중 새지급/구매명령0. 전투진행/정지는 기존전투계약 적용.
- 화면닫기: 표시전용 불필요조회/콜백 정리; 전송된변경의 원요청·복구는 화면밖 소유자가 유지. 다음진입 확정결과있으면 재사용. 계정전환 구응답 새owner침투0. 연결관찰/타이머/재시도작업 수명은 기존owner가 관리.
- 수용검수: 반복연결신호 재시도폭주0, 자동+수동경합 중복전송0, 원키 중복지급/소비0, 닫힌화면 재생성0, 반복단절후 요청/타이머/구독 잔류누적0. DEV서버 한정 유한로그에서 논리요청/최초전송/자동1회/수동복구/원키재생을 구분. 실제서버응답 연결 및 실제Player API/메모리 전후검수 계속, baseline/합성/미실행 명시.
Server는 기존 오류/재생가능성·기한/원응답 계약 보완, Unity는 요청소유/자동1회/화면·수명 구현. 기존 DATA-LIFECYCLE 후속조건/키 재사용. 신규 다중화면 명령입력 상세안은 제안상태, 미디어제작 제외.


<a id="data-multiview-command-approved-20261021"></a>
## 다중 화면 명령 처리 승인 — 2026-10-21
사용자 직전 상세안 승인. data.multiview-command.v1.20261021. 기존 DATA-LIFECYCLE·MVC/명령소유/원키복구 보완.
- 동일업무 대상 명령은 화면밖 단일owner 진행상태 공유. 보상 수령: 실행가능 받기 → 전송 처리중 → 결과불명확 결과확인(원키복구) → 검증된지급확정/로컬저장대기 지급완료·기록저장중 → 완료 받음. 화면재진입으로 받기/새명령 복원0. 서버성공과 클라실적용·영속단계는 기존계약대로 구분.
- 같은보상ID 수령은 기존진행요청에 합침, 출격연타 새출격0. 편성저장중 다른선택의 확정은 현처리 먼저, 선택변경 자동후속전송0. 정상반복구매는 이전완료후 사용자새확정 때 새거래. 내용동일만으로 거래합침0. 업무대상/계정/run·명령의 실제identity를 기존계약으로 판정.
- 동일대상/충돌실행만 제한, 조회/스크롤/설정등 독립기능 유지. 실제journal/server 동시지원이 없는 범위의 기존직렬제한 유지, UI에서 임의병렬강제0. 여러기기·서로다른원키도 서버의 보상원권리/원자성으로 최종중복방지, 클라이언트잠금이 권한검증을 대체하지 않음.
- 모든소비화면 확정상태 갱신, 같은거래 팝업/토스트/보상연출 중복0. 재진입/원키복구만으로 이미표시된연출 반복0, 내역열람은 가능. 표현중복표시 관리와 지급원장 분리; 새화면마다 독립거래원장 만들지 않음. 계정/요청수명 범위의 유한기록과 해제 필요.
- 수용시험: 두화면 동일보상 동시입력 지급1, 연타/왕복/복구 원키유지, 서버완료후재진입 새요청0, 정상반복구매 별도거래, 닫힌화면구독해제/원복구유지, 완료임시입력·callback·표시객체 누적0. 실제서버응답 연결→Player API/메모리전후 검수 계속, baseline/합성/미실행 분리.
Server 기존명령identity/원권리·멱등/경합/원영수증 계약 담당, Unity 기존Controller입력→단일owner/표시·연출수명 담당. 기존 DATA-LIFECYCLE/WAIT 후속조건·키 재사용, 미디어제작 제외. 다음 목록갱신/선택/스크롤 세부안은 신규제안.


<a id="data-list-continuity-approved-20261021"></a>
## 목록 갱신 선택·스크롤·상세창 유지 승인 — 2026-10-21
사용자 직전 상세안 승인. data.list-continuity.v1.20261021. 기존 DATA-LIFECYCLE/화면유지/MVC 보완.
- 선택은 배열index가 아닌 실제요원/장비 고유ID로 유지. 앞삽입/정렬변화에도 동일대상상세 유지. 대상삭제는 선택해제·다른대상 자동선택0. 사용자필터로 숨김이면 선택해제, 필터복귀 자동재선택0.
- 데이터갱신 스크롤은 보던항목ID+화면내offset으로 유지. 위삽입은 위치유지, 수치/장착변경은 해당행만 갱신. 기준항목삭제는 기존순서의 가까운생존항목으로 위치복원(선택자동이동 아님). 사용자 정렬/필터 변경은 상단이동. 갱신실패 기존목록/위치유지. 해당화면 기존명시계약이 있으면 충돌을 기록하여 적용범위를 확인.
- pointer-down 대상과 pointer-up 대상이 달라지면 취소. 같은좌표 새항목 선택/장착0. 같은ID라도 실행조건 변경이면 최신조건 재표시·사용자 재확정, 기존입력 대리실행0.
- 부분페이지≠전체목록. 동일다음페이지 요청 합침. 필터/정렬변경 후 구query세대 응답은 새목록에 혼합0. 미공급항목을 삭제로 판단0. 유효목록 재진입 자체로 첫페이지 GET0. 기존실제 cursor/version/완결성계약을 사용하고 새전체목록 API를 무조건 만들지 않음.
- 보이는행 중심 표시객체 재사용, 재바인딩시 이전callback/이미지요청 해제·구세대 게시차단. 실제소유 데이터와 표시객체분리, 전체응답/목록 매갱신 복제·전체UI 재생성 억제. 원불변snapshot/필수복사 보존.
- 수용시험: 추가/삭제/정렬·필터/늦은페이지/누르는중갱신/5회왕복에서 동일대상상세/선택·위치, 정상추가GET0, 행구독/객체/texture잔류 검수. 실제서버응답 연결→Player API/메모리 전후 계속, baseline/합성/미지원행 구분.
Server 실제항목ID/목록부분·전체/페이지cursor·삭제/version 계약검증·필요공급, Unity 선택/스크롤/상세/행수명 소유. 자기독립구현부터 진행, 기존DATA-LIFECYCLE 후속조건/키 재사용. 신규 상세창 실행조건 변경 확인안은 제안상태. 미디어제작 제외.


<a id="data-command-conditions-approved-20261021"></a>
## 실행 조건 변경 재확정 승인 — 2026-10-21
사용자 직전 상세안 승인. data.command-conditions.v1.20261021. 기존 DATA-LIFECYCLE/원명령·버전/화면유지 보완.
- 설명/이미지 표시변경은 해당부분갱신. 보유재화 증가·실행조건동일은 잔액갱신/선택유지. 가격/소모재료/구매수량 변경은 변경표시후 사용자재확정. 장착대상/교체장비 변경은 전후재표시·재확정. 대상삭제/판매종료/권한상실은 실행차단·이유표시. 결과불명확은 새확정 대신원키복구.
- 확정요청에 기존계약 대상ID/수량/조건version 사용, Server가 권위원본과대조. 조건변경시 다른가격/재료로 임의실행0, 미실행결과+변경확인 필요정보 공급. 충분한변경응답 뒤 상세GET0. 실제필드 부족은 원응답에 최소보완, 고객표시값을권위로사용0.
- 변경안내는 기존상세창에 전후가격/필요재료/보유량 등 실제원천표시. 가능한선택유지, 새조건 사용자확정전 자동재전송0. 반복팝업0/대체재료·결제수단 임의전환0.
- 원미실행이확정되고 사용자가새조건동의하면 새명령. 결과불명확은 원키복구. 기존키에 새payload끼우기0. 이미성공한거래는 이후조건변경과무관하게 원영수증표시, 최신잔액역행금지 유지.
- 수용시험: 오래열린창 가격/재료/장착변경 의도외소비0, 클릭순간경합 서버보호, 충분한거절/변경응답후GET0, 확정미실행 새키와결과불명확 원키구분, 반복조건변경 객체/콜백/응답누적0. 실제서버응답소비→Player API/메모리전후검수 계속, baseline/실측/미지원 분리.
Server 조건검증/원자미실행·영수증/응답 계약, Unity 변경표시/재확정/원키복구·수명 소유. 기존 DATA-LIFECYCLE 후속조건/키 재사용, 다음 일괄받기 부분결과 상세안은 제안상태. 미디어제작 제외.


<a id="data-bulk-result-approved-20261021"></a>
## 일괄 받기 부분 결과·복구 승인 — 2026-10-21
사용자 직전 상세안 승인. data.bulk-result.v1.20261021. 기존 지급원자성/권리 및 DATA-LIFECYCLE 보완.
- 항목별 이번지급완료/이미지급/미지급확정/결과불명확/원요청대상외 구분. 이번지급만 획득합계, 이미받음 중복가산0. 미지급 이유/재수령조건 표시, 불명확은 원키복구. 응답누락을 실패/완료로 추정0.
- 사용자확정 때 원대상ID/수량 고정, 처리중새보상 자동편입0. 기존 원자적일괄은 전부성공/미실행 유지, 항목별부분처리 지원기능만 부분완료. 새경제/부분지급정책으로 확대0.
- 불명확항목 원요청복구 우선, 미지급확정항목만 사용자재확정후새수령. 완료항목 신규명령제외. 원키에 새목록 끼우기0. 개별/일괄 경합은 서버 동일권리·원자성으로 중복지급방지. 원거래재생과 현재최신잔액 적용 구분.
- 결과 예시 '8개 수령 완료 · 1개 수령 불가 · 1개 결과 확인 중'. 숫자는 원항목상태 근거. 확정완료 반영하되 미확정포함 전체성공연출0. 충분한응답이면 목록/재화 후속GET0. 실제응답부족/버전불연속 복구유지.
- 시험: 부분완료후단절 중복0, 개별/일괄동시 권리1회, 과거지급 이번합계제외, 저장실패/재실행 원목록·원키복구, 반복후 항목응답/표시객체/구독누적0. 실제서버응답→Player API/메모리전후 지속, baseline/합성·memory-only/실측미지원 구분.
Server 원항목/원자성·멱등/권리/결과영수증 공급, Unity 상태별표시/합계·복구·수명 담당. 기존DATA-LIFECYCLE 후속조건/고정키 사용. 다음 지급완료재화/미수령보상 표시안은 제안상태. 미디어제작 제외.


<a id="data-currency-display-approved-20261021"></a>
## 재화 표시 승인 — 2026-10-21
사용자 직전 상세안 승인. data.currency-display.v1.20261021. 기존 DATA-LIFECYCLE·일괄결과/원버전수락 보완.
- 상단재화는 서버확정 보유량만. 미수령은 별도 받을보상, 요청중은 마지막확정잔액+처리중, 지급확정/최신반영가능이면 확정잔액갱신. 지급확정후로컬저장실패는 확인잔액유지+지급완료·기록저장중. 결과불명확은 마지막확정잔액+결과확인중. 미수령/처리중을사용가능금액으로선가산0.
- owner 확정상태 먼저반영, 재화아이콘/획득수치 연출은 표현만. 연출종료callback에서재화가산0. 건너뛰기/닫기도동일잔액, 후속거래최신잔액을 과거연출이되돌리기0. 기존제작자산 재사용, 새미디어제작지시 아님.
- 예약/보류금액이 존재하면 실제서버계약으로만구분, 처리중요청액을클라임의차감해별도원장0. 불명확상태에서사용가능금액판정불가면 충돌소비만 기존명령계약대로제한.
- 거래획득량과현재잔액분리. 과거영수증내역 재열람 허용/현재잔액재가산0, 일괄합계 이번지급만. 표시소유자는기존공통owner, 독립UI잔액원장0. 충분확정응답후잔액GET0, 원버전불연속복구보존.
- 시험: 연출ON/OFF/skip/닫기 잔액동일, 미수령/불명확 가산0, 구영수증/연출역행0, 다중화면값일치, 정상GET0, 반복수령후연출객체/콜백/거래표시잔류누적0. 실제서버응답→Player API/메모리전후 지속, baseline/합성/미실행구분.
Server 기존최종잔액·버전/예약권위·원영수증 공급, Unity 단일owner 표시/연출수명 담당. DATA-LIFECYCLE/WAIT후속키 재사용. 신규 데이터/콘텐츠버전불일치안 미승인, 미디어제작 제외.


<a id="data-content-mismatch-approved-20261021"></a>
## 데이터·리소스 버전 불일치 승인 — 2026-10-21
사용자 직전 상세안 승인. data.content-mismatch.v1.20261021. 02의 기존콘텐츠승인/다운로드동의/release pin 정책유지, DATA-LIFECYCLE 보완.
- 아이콘만없고 이름/수치/규칙확인가능이면 공통대체아이콘+정확정보. 설명정의미공급은 추측0·해당상세제한. 가격/효과/장착조건 해석불가면 영향확정입력차단. 서버필수클라이언트업데이트는 기존흐름. 진행작전과새정의 버전차이는 원START정의·호환정책 유지.
- 정상계정응답인데 그림없음은 계정재조회사유아님. owner계정상태보존/필요자산만 승인된기존경로 준비/동일자산작업합침/완료후해당요소교체. 임의새전체카탈로그조회·무동의다운로드0.
- 미등록ID를다른아이템으로대체0. 동일ID의버전별효과 임의최신덮어쓰기0. 진행작전 원규칙/원투사체·효과·보상 재계산0. 보유원장과표시정의의소유권분리.
- 승인release/의존성확인→필요다운로드 기존망/동의정책→pin충돌 기존재승인→관련화면갱신. 실패시정상캐시/원복구저널 전체삭제0. 자산준비가 원사용자확정명령 자동재실행권한아님.
- 시험: 아이콘복구계정GET0, 정의불일치 잘못된가격/효과실행0, 동일자산중복작업/보관0, 닫힌화면늦은자산재생성0, 진행run/원영수증보존, 반복실패·재진입 handle/texture/callback잔류검수. 실제서버응답→Player API/메모리전후 계속, baseline/합성/미실행구분.
Server 실제정의version/지원호환·기존admission 계약 공급·필요수정, Unity 범위별표시/입력차단·리소스소유/해제 담당. 자기독립구현부터 진행, 기존 DATA-LIFECYCLE/WAIT후속키 사용. 신규 빠른화면전환 세부안 미승인, 미디어제작 제외.


<a id="data-fast-navigation-approved-20261021"></a>
## 빠른 화면 전환 승인 및 메모리 증가 원인 검수 — 2026-10-21
사용자 직전 상세안 승인. data.fast-navigation.v1.20261021. Unity 소유, 기존 DATA-LIFECYCLE·수명/공유조회 보완.
- 같은계정 유효조회응답은 owner반영가능/닫힌View갱신0. 전송된명령은 화면밖 결과/영수증처리 계속. 닫힌화면전용자산은 미표시/소유참조반환, 공용자산은 해당소비자 사용권만반환. 다른계정/run응답은 현재owner반영0·원복구계약 유지.
- A→B→A 각진입세대 구분, 이전A 콜백이 새A선택/스크롤덮어쓰기0. 여전히유효한 동일진행조회 공유, 완료데이터재사용, 재진입자체 추가GET0.
- 행A→행B 재사용시 이미지적용전 실제ID/리소스version/바인딩세대 검증. 늦은A자산 B적용0, 미사용참조반환. 닫기/비활성/파괴 반복에도 이중반환0·자기구독만해제·타소비자공유조회취소0·타이머/callback참조정리.
- 실제Player 수용: 빠른왕복 오표시0/공유조회GET, 전송직후닫기원기록보존, 반복종료멱등, 화면객체/콜백/handle/texture잔류 확인. 기존DATA-LIFECYCLE 후속키 사용, 미디어제작 제외.
- 사용자 추가지시: Server/Unity 실제서버응답연결 지속 및 메모리증가 원인확인. Unity #unity10-data-save-failure-player-20261021의 저장후/참조해제후 managed증가를 누수로 단정하거나 숨기지 않음. 동일빌드설정/같은시나리오/반복횟수·측정시점·자연GC조건에서 재현하고 retained참조/일시할당/heap예약·수거시점/로그계측비용을 분리. source/객체소유자와 reference chain 근거로 원인변수 하나씩 비교. 강제GC진단은 자연GC 결과와 분리, 임의상시GC0. texture CPU/GPU/processprivate 미측정은 0아님. baseline을훼손하지않고 비교근거 보존.
- Server는 실제응답크기/필드중복·필요version/원영수증 공급경계 확인 및 필요한수정, Unity는파싱/보관/화면/리소스/계측수명 원인조사와 근거있는수정. API감소를메모리절감으로환산0. 기존검증 재사용, 신규변경/실패/증가미해결 구간에 필요한 재검수. 전체Player/실기기·영속복구 미지원행 명시.
다음 DEV비교결과 화면 상세안은 신규제안이며 미승인. 기존DEV서버한정 계측/모바일복사는 승인유지.


<a id="dev-data-comparison-approved-20261021"></a>
## DEV API·메모리 비교 결과 화면 승인 — 2026-10-21
사용자 직전 상세안 승인. dev.data-comparison.v1.20261021. Unity 기존진단화면 확장, 별도진단시스템 중복신설0.
- DEV서버에서만표시, PRD노출0. 상단에 전후빌드/시나리오/기기환경/반복횟수·실제HTTP와합성응답·자연GC와강제GC진단·미지원/미측정 표시. 조건불일치는 참고비교, 동일조건 개선으로위장0.
- 각행 전→후→차이: API 논리요청/실전송/자동재시도/수동복구/조회사유, 메모리 기본상주/current/peak/누적할당/반복잔류, texture CPU/GPU·live객체/handle, 재진입/입력후표시시간. 미측정은 —,0환산0. 단위와측정경계명시, 사용량/예약량 혼동0.
- 증가행 상세에는 측정시점/실제확인객체·자산/참조소유/의도캐시·미해결잔류/원인확인·수정후재검수 상태 표시. 근거없는원인추정0, API감소와메모리증가 각각표시.
- 모바일 요약복사/선택항목복사/전체결과내보내기 제공, 비교조건/미검증범위 함께포함. 토큰/원응답/개인정보0. 기록상한/수명으로 진단자체증가방지, 내보내기 임시버퍼 해제.
- 판정은 검수통과/증가원인확인필요/기능오류/미실행·미측정. 기준없는성능수치 자동PASS0, 부분수치감소로전체초록0. 실제검수사례/근거와결속.
- 기존Server/Unity 실제응답연결/메모리증가원인확인/Player API메모리전후검수 계속. 신규화면구현을핵심실연결검수의선행대기로만들지않음. 기존QA/인계/후속AND키 재사용, baseline/합성시험/실기기·영속미지원 분리. 미디어제작 제외. 다음 출격준비상태 상세안 미승인.


<a id="data-departure-readiness-approved-20261021"></a>
## 출격 준비·실패 복구 승인 — 2026-10-21
사용자 직전 상세안 승인. data.departure-readiness.v1.20261021. 기존출격확정1회/원키복구/콘텐츠승인 규칙보완.
- 편성/장착은유효owner상태, 작전은기존정의/선택, 필수전투자산은승인version/로컬준비 상태로확인. 미확정원출격이있으면원복구먼저. 연료/입장권한/최종조건은확정시Server검증. 화면열기로전체계정/preview재조회0, 로컬준비가권한을대체0.
- 필수준비전 확정전송0. 준비중편성유지/전투준비중표시, 다운로드기존동의정책. 준비중편성/작전변경은필요부분재확인, 구세대완료가새선택준비완료로게시0.
- 확정응답의실제편성/version/작전과준비내용대조. 일치시진입, 추가준비필요시생성된원runId유지/필요부분만준비. 새START/중복연료소비0.
- 확정성공후화면진입실패는 '출격은 확정됐습니다. 전투 화면을 다시 준비합니다.' 해당상태에서만표시. 같은run화면/자산재시도, 재실행은원출격기록복귀. 실패위장/자동환불/새출격0, 포기는기존작전포기계약.
- 시험: 준비재진입추가조회/START0, 준비중선택변경오진입0, 연타/응답유실 run생성·연료소비1, 확정후자산실패/재실행동일run복귀, 반복준비취소 handle/callback/메모리잔류. 실제서버응답연결/증가원인/Player API메모리전후 지속, baseline/합성/memory-only·공개START미지원 구분. 지원안되는실원천을가짜로채우지않음.
Server 원START/원영수증·확정구성·권한/복구계약 공급·필요구현, Unity 준비수명/입력/원run전환·실소비 담당. 기존DATA-LIFECYCLE/WAIT후속키 사용, 다음 전투종료로비복귀 해제순서는 신규제안. 미디어제작제외.


<a id="data-lobby-return-approved-20261021"></a>
## 작전 종료·로비 복귀 승인 — 2026-10-21
사용자 직전 상세안 승인. data.lobby-return.v1.20261021. 중간노드종료와작전전체종료 구분, 기존결과/보상권리·자원수명계약 유지.
- 종료전투입력/새공격생성중단→결과확정필요원상태/원요청보존→서버확정응답검증·owner반영→결과표시/복구최소데이터를전투객체와분리→종료구독/타이머/투사체/효과/자산사용권정리→기존계정상태로로비표시. 중간노드에서다음전투에필요한작전상태는보존. 충분응답후로비진입만으로account/wallet/growth GET0, 부족/버전gap 범위만복구.
- 미확정원요청/영수증·결과요약·미수령보상·다음노드작전상태 보존. 종료입력/구독·일시전투객체·사용종료전투참조·재생성표시데이터 해제. 결과창때문에전체전투scene참조유지0. 공유자산마지막사용자반환 준수, 필수원검증/복구자료를최소요약으로임의대체0.
- 결과유실시 결과확인중/원키복구, 필요한원본안전분리뒤 표시전용자산정리가능. 로비이동은기존작전규칙, 불명확상태새출격허가0. 결과표시≠보상지급, 미수령권리보존.
- 로비표시실패는확정상태로화면만재시도, finish/claim재실행0·이중해제0·늦은종료전투callback로비오염0. 원기록의영속인계완료전에삭제0.
- 시험: 결과/claim/로비중복0, 충분결과왕복추가GET0, 중간노드상태유지, 반복출격종료복귀객체/구독/texture잔류, 증가참조/의도캐시구분, 복귀실패/재실행원결과·미수령보존. 기존실제서버응답연결·증가원인·Player API메모리 baseline/전후 지속, 실제HTTP+합성SQL/영속미지원/미측정구분.
Server 기존finish/claim/복구·응답완결/영수증 소유, Unity owner반영/결과분리/scene수명·로비복귀 소유. 기존DATA-LIFECYCLE/WAIT후속키 재사용. 다음 이전run→새run 상태분리 상세안은 신규제안, 미디어제작제외.


<a id="data-run-isolation-approved-20261021"></a>
## 다음 작전 상태 분리 승인 — 2026-10-21
사용자 직전 상세안 승인. data.run-isolation.v1.20261021. 기존 START/계정·run 소유권/원복구 정책보완.
- 계정요원/영구성장/재화는최신확정상태재사용, 공용자산/정적정의는승인version동일시재사용, 출격편성은기존정책유지. 작전증강/임시장비/효과 자동승계0. HP/보호막/cooldown/표적은새START계약초기화. 이전결과/미수령권리는원runId로보존. 동일요원/맵도별도작전.
- 준비중새작전활성화0. 서버확정응답검증/필수준비후새run owner일관연결. 준비실패/응답유실로이전기록삭제·임시runId생성0. 원START확정과클라이언트활성전환구분.
- 구run 결과/영수증은원기록반영, 계정재화는owner version수락계약. 구전투피해/회복/효과callback 새run적용0, 구화면완료연출새전투노출0. 기존run복구의합법원영수증처리는보존.
- pool 재사용시 원표적/runId/구독/타이머/임시효과참조정리. 반환후늦은callback은새lease/새run객체수정0. 공유자산 마지막사용자해제, 필수원검증/저널원본보존.
- 수용: 연속동일편성출격 구증강/HP/cooldown혼입0, 구보상새전투오염0, 출격실패원기록/미수령보존, 풀구표적/구callback0, 반복출격 owner/구독/객체누적0, 유효계정/공용자산 불필요GET·reload0. 실제서버응답소비·메모리증가원인/Player API메모리전후 지속, baseline/합성·실원천/미측정구분.
Server 원run/계정귀속·START/영수증/권한 경계, Unity owner활성전환/scene·pool세대·수명 소유. 기존DATA-LIFECYCLE/WAIT후속키 사용. 다음 진행run저장/서버대조 복원세부안은 신규제안, 미디어제작제외.


<a id="data-restore-reconcile-approved-20261021"></a>
## 저장·서버 상태 대조 복원 승인 — 2026-10-21
사용자 직전 상세안 승인. data.restore-reconcile.v1.20261021. 기존재접속/원START·checkpoint/원키복구 보완.
- 복원전 account/runId·원START정의/콘텐츠호환·checkpoint/명령기록일치·미확정SELECT/finish/claim·현재server작전상태 대조. 각권위owner계약 사용, 파일mtime최신만으로채택0.
- 유효동일기록은기존복원; 서버선택확정/로컬미적용은원확정근거로적용·저장완료후재개; 미확정원요청은원키복구후판단; 서버종료확정은전투재실행0/결과보상으로이동. 서버확인실패/NOT_FOUND를미실행·새START허가로단정0·원기록보존. 손상/해석불가버전은관련진행제한·원복구안내, 초기화0. 충분한복구응답후동일조회0.
- 복원객체 구성/원상태검증후 입력/전투재개. HP만복원되고증강/cooldown누락된중간상태실행0. 이미적용된효과/소비/지급재적용0. 원효과/checkpoint receipt 연속성 유지.
- 자료부재는검증된기존복구수단만사용. HP임의보충/직전선택취소/새작전덮어쓰기0. 복원불가원기록보존·기존문의복구흐름, 미확인손실/실패단정0.
- 검수: SELECT확정/적용/저장 각중단 중복효과0, finish/claim중종료 원키복구, account/run혼합0, 반복복원실패 객체/구독/임시데이터누적0. 정상/복구API와peak/잔류 분리. 실제서버응답연결·증가원인·Player전후 계속, baseline/실제HTTP+합성SQL/영속재시작미지원구분. 이전formation/view측정치를새run저장수정검수로대체0.
Server 원작전상태/영수증·원검증/호환계약, Unity 대조/단계적용/저장·재개/수명 담당. 기존DATA-LIFECYCLE/WAIT조건·키 재사용. 신규복원중취소/재중단 상세안 미승인, 미디어제작제외.


<a id="data-restore-interrupt-approved-20261021"></a>
## 복원 도중 취소·재중단 승인 — 2026-10-21
사용자 직전 상세안 승인. data.restore-interrupt.v1.20261021. 기존 DATA-LIFECYCLE/복원 대조·원키 복구·수명 계약 보완.
- 뒤로 가기는 복원 화면/준비 작업 종료이며 작전 포기·서버 명령 취소·환불·새 출격이 아니다. 원작전/미수령 권리/원요청 보존.
- 전송 전 불필요 준비 취소, 전송 후 결과 불명확은 원키 복구, 서버 확정 후 로컬 저장 전은 확정 결과 보존·저장 마무리. 저장의 원자적 교체 구간은 임의 중단하지 않으며 화면 종료와 저장 소유자의 수명을 구분한다.
- 리소스 로딩 취소는 해당 화면의 사용권만 반환. 공유 요청/공유 자산과 필수 원기록은 임의 해제하지 않는다. 복원 필수 상태/저장 확인 전 전투 입력 금지.
- 동일 run 복원 소유자는 하나. 재진입은 진행 작업에 연결하거나 마지막 검증된 단계부터 재개. 늦은 응답은 account/run/version 검증 후 유효 소유자에 반영하며 닫힌 View 재갱신·타계정/새run 적용 금지.
- 재종료 시 RAM 존재를 가정하지 않고 영속 원기록으로 판단. 이미 확정·적용된 선택/비용/보상 중복 금지, 불명확 상태를 처음부터 실행 금지. 임의 HP 초기화/자동 포기/신규 run 생성 금지. 기존 자동 재시도 1회 예산을 재진입으로 초기화하지 않는다.
- 요청 대기/확정 직후/저장/자산 로딩에서 이탈·재진입·프로세스 종료 검수. 중복 소비·지급·효과/닫힌 View 호출/불필요 GET 0. 반복 후 객체·구독·자산 사용권/순간최대·잔류메모리 측정. 실제 Player/영속 재시작 미지원은 미검증으로 명시.
Server 원명령/귀속/영수증·복구 계약과 부족 구현, Unity 취소수명/저장/복원 재진입·실제 응답 소비 담당. 기존 완료 기반 WAIT AND 조건/후속키 유지. 실제 응답 연결·메모리 증가 원인·Player 전후 검수를 우선 계속한다. 다음 복원 후 첫 입력/전투 재개 상세안은 신규 미승인 제안. 미디어 제작 제외.


<a id="data-restore-resume-approved-20261021"></a>
## 복원 후 첫 입력·전투 재개 승인 — 2026-10-21
사용자 직전 상세안 승인. data.restore-resume.v1.20261021. 기존 복원 대조/취소/전투 시간 계약 보완.
- 필수 상태·영속 저장·자산 연결 완료 후 진행 중 전투는 정지 전장과 '전투 계속' 표시. 선택 미확정은 선택 화면, 선택 확정/미적용은 적용·저장 후 해당 진행 화면, 종료 확정은 결과·보상 화면. 로딩 완료만으로 자동 전투 재개하지 않는다.
- 종료 전 눌린 터치/드래그/미실행 스킬 입력은 폐기하되 이미 확정된 명령은 원복구 계약 유지. 안내 닫기 입력을 뒤쪽 전장으로 전달하지 않으며 새 입력부터 수락. 재개 연타는 한번만 전이.
- 피해/회복/cooldown/지속효과/투사체는 동일한 복원 전투 시간 기준으로 재개. 화면 준비 시간을 전투 경과로 가산하지 않는다. 기존 오프라인/서버 만료 시간 계약은 별도 유지. 이미 적용된 공격 재발사·미처리 피해 임의 삭제 금지.
- 재개 직전 필수 상태/자산 유효성 및 account/run 세대를 확인. 변경/부족 시 정지 유지 후 필요한 범위만 복구. 유효한 상태에서 재개 버튼 자체로 추가 API 호출하지 않는다. 서버 명령의 권한 검증을 로컬 검사로 대체하지 않는다.
- 실제 Player에서 연타/길게 누르기/화면 이탈/재중단, 선택·결과 분기, 준비 시간, 중복 공격/효과 및 반복 복원 후 객체·구독·API·메모리 확인. 이전 formation/view 검수를 신규 run 재개 검수로 대체하지 않는다.
Server 원작전 lifecycle/확정 단계·시간/복구 계약의 실제 충분성 확인 및 필요한 부족 구현, Unity 상태 분기/입력 경계/정지→재개·실제 소비 담당. 기존 DATA-LIFECYCLE/WAIT 완료 기반 후속키 유지. 실제 서버 응답 연결·메모리 증가 원인 확인·Player API/메모리 전후 검수 우선 지속. 신규 복원 대기 중 만료·서버 종료 확정 전환 상세안은 미승인, 미디어 제작 제외.


<a id="data-restore-terminal-approved-20261021"></a>
## 복원 대기 중 작전 만료·종료 확정 처리 승인 — 2026-10-21
사용자 직전 상세안 승인. data.restore-terminal.v1.20261021. 기존 만료·보상 규칙/복원·원키 계약 보완이며 새 만료 정책 아님.
- 유효 작전 복원 완료는 전투 계속, 실제 서버 기준 작전 만료 경계 도달·결과 미확인은 재개 제한/작전 상태 확인 중, 서버 종료 확정은 사유·결과, 확인 실패는 확인 불가와 기존 복구 선택 제공. 기기 시각만으로 패배/권리 소멸 확정 금지. 이벤트 종료/작전 만료/보상 수령 기한 구분. reward.expiresAt이나 serverTime을 작전 만료 필드로 임의 해석하지 않으며 실제 원천·계약 부재는 미확인으로 남긴다.
- 상시 폴링 금지. 실제 만료 경계 또는 원정보 무효화 때만 필요한 상태 확인, 충분 종료 응답 뒤 GET 금지. 재진입/연타는 진행 확인에 합치고 기존 자동 재시도 예산 유지.
- 종료 확정 후 전장 준비/사용권 정리·결과 전환. 늦은 이전 복원/로딩 완료로 재개 활성화 금지. 종료와 재개 경합은 서버 확정 상태·순서/기존 권한 계약 준수, 클라이언트 클릭 시각으로 결과 결정 금지.
- 결과/미수령 권리는 기존 정책과 원run에 보존. 진행 SELECT/claim은 원요청/원키 복구. 화면 전환으로 새요청/자동수령·환불·재출격 금지.
- 시험: 만료 직전 재개, 확인 중 재종료, 늦은 복원 응답, 결과 재진입. 종료 전투 재개·중복 지급·권리 유실·불필요 반복 GET 금지. 준비 중단 후 객체·구독·자산 참조/메모리 확인.
Server 실제 lifecycle/만료 원천·권한/영수증 계약 확인 및 부족 구현, Unity 실제 응답 소비/재개 차단·결과 전환·수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 서버 연결·메모리 증가 원인 조사·Player API/메모리 전후 우선 지속, 합성 SQL/실HTTP/실Player/영속 재시작과 미지원 분리. 다음 복원 실패 안내·사용자 복구 선택지 상세안은 미승인. 미디어 제작 제외.


<a id="data-restore-recovery-ui-approved-20261021"></a>
## 복원 실패 안내·사용자 복구 선택지 승인 — 2026-10-21
사용자 직전 상세안 승인. data.restore-recovery-ui.v1.20261021. 기존 오류/저장/원키 복구/문의 계약 보완.
- 확인된 실패 단계별 주 행동: 서버 응답 불명확은 '작전 상태를 확인하지 못했습니다 / 다시 확인', 서버 확정 후 기기 저장 실패는 '진행 내용을 기기에 저장하지 못했습니다 / 저장 다시 시도', 자산·화면 준비 실패는 '전투 화면을 준비하지 못했습니다 / 화면 다시 준비'. 필수 업데이트 확인은 기존 업데이트 흐름, 기록 불일치·해석 불가는 기존 문의 흐름. 미확인 원인을 네트워크/용량 부족으로 단정하지 않는다.
- 실패 단계만 재시도: 저장은 로컬 저장, 화면은 필요한 준비, 원결과 불명확은 원요청/원키 확인. 진행 중 연타 병합, 자동 재시도 기존 예산 유지, 확정 효과/소비/보상 재실행 금지.
- 안전하게 원기록 보존 가능한 상태에서 로비로 제공. 이동≠포기/삭제. 미저장 상태를 저장 완료로 표시 금지. 작전과 충돌하는 출격·변경만 기존 규칙대로 제한.
- 기존 문의에 필요한 오류 식별 코드/발생 단계/앱 버전 연결. 일반 UI는 쉬운 안내·복사 가능한 문의 코드, 메모리/API/상세 진단은 DEV 서버 전용. 비밀값/개인정보/원저널 노출 금지. 기존 PRD 중요 오류 전송 규칙을 없애지 않으며 신규 원문로그 수집·업로드 권한으로 확대하지 않는다.
- 각 실패 실제 재현 후 단계별 버튼/왕복·연타 검수. 추가 소비·지급/불필요 조회/중복 구독 금지. 복구 성공 후 오류 상태·임시 참조 정리, 필요한 원영수증/복구 기록은 보존.
Server 기존 오류 의미/원결과·복구·문의 식별 계약 확인 및 부족 구현, Unity 실패별 표시/Controller 기존 재시도 연결·수명/실제 소비 담당. 기존 DATA-LIFECYCLE WAIT AND·후속키 유지. 실제 서버 연결·메모리 증가 원인·Player API/메모리 전후 검수 우선 지속. 신규 로비 중단 작전 발견·이어가기 상세안 미승인, 미디어 제작 제외.


<a id="data-lobby-resume-approved-20261021"></a>
## 로비 중단 작전 표시·이어가기 승인 — 2026-10-21
사용자 직전 상세안 승인. data.lobby-resume.v1.20261021. 기존 로비/복원/작전 입장 계약 보완.
- 기존 출격 영역에 중단 작전명·마지막 확인 진행 위치·현재 행동 표시, 로비마다 팝업 금지. 진행 전투=작전 이어가기, 선택 미확정=선택 이어가기, 원요청 불명확=작전 상태 확인, 확정결과 기기 저장 실패=저장 다시 시도, 종료 확정=결과 보기. 미확인 진행·보상 추측 표시 금지.
- 계정 소유 유효 작전 상태/복구 기록 재사용, 로비 왕복·버튼 표시만으로 추가 GET 금지. 로컬 기록만 있고 서버 대조 필요 시 이전 작전 기록 확인 필요 표시; 진행·종료 단정 금지.
- 로비/복구 안내/앱 복귀는 동일 Controller의 기존 복원 처리에 연결, 별도 요청·전투 객체 복제 금지. 연타는 진행 복원에 합침, 실패는 승인된 단계별 복구 선택지.
- 새 출격 가능 여부는 기존 동시 작전/입장 계약 유지. 제한 시 해당 영역에 이유·복구 버튼 표시. 이어가기≠새START/연료 재소비, 결과 보기≠보상 수령.
- 로비 반복 진입/계정 전환/연타/취소 재진입 시험. 불필요 GET·중복 START·타계정 작전 표시 금지. 표시용 요약과 필요한 복구 원기록 보존, 로비 표시만을 위해 전체 전투 scene 유지 금지.
Server 기존 작전 발견/계정 귀속·입장/복구 응답 충분성 확인 및 부족 구현, Unity 로비 표시/기존 복원 연결·수명/실제 소비 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 서버 연결·메모리 증가 원인·Player API/메모리 전후 검수 우선 지속, 새 UI가 핵심 실연결 선행 대기가 되지 않음. 신규 이어가기 중 편성·장비 변경의 표시·적용 기준안 미승인. 미디어 제작 제외.


<a id="data-resume-configuration-approved-20261021"></a>
## 이어가기 준비 중 편성·장비 변경 표시·적용 승인 — 2026-10-21
사용자 직전 상세안 승인. data.resume-configuration.v1.20261021. 기존 START snapshot/정비 적용 시점/복원·owner 정책 보완.
- 이어가기·복원 전투는 해당 작전 마지막 확정 구성 표시. 출격 고정 편성·능력은 원START, 작전 장비는 마지막 정비 확정, 증강·임시효과는 원run 확정, HP/보호막/cooldown은 검증 복원 상태. 기존 항목별 적용 시점 유지; 최신 로비값 덮어쓰기·모든값 START로 회귀 금지.
- 현재 계정 편성과 진행 편성이 다를 때만 '진행 중인 작전의 편성으로 이어갑니다' 안내. 정보보기는 진행 작전 기준 명시. 차이 표시 목적으로 전체계정 재조회/강제 비교 팝업 금지.
- 정비 결과 불명확은 원요청으로 복구 후 작전 상태/표시/저장 일치시켜 재개. 구장비·신장비 임의선택 금지. 충분 응답은 기존정보로 완료/추가GET0.
- 계정 편성 응답은 계정 owner, 작전 정비 응답은 원run owner 반영. 늦은 응답/자산완료로 서로 다른 구성의 아이콘·능력·장비 혼입 금지. 자산 공유 허용, 다른 구성으로 준비한 객체의 가변 상태 임의 재사용 금지.
- 시험: 로비 변경 후 이어가기, 정비 응답 유실 복원, 준비 중 변경/취소/재진입. 표시 장비=전투 적용 장비, 중복 정비/소비/불필요GET0, 교체 자산참조/메모리 잔류 확인.
Server 기존 START/정비·버전/원키 계약 실제 공급·부족 구현, Unity 구성 대조/표시·전투 소비/준비 세대 수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 응답 연결·메모리 증가 원인·Player API/메모리 전후 우선 계속. 신규 복원 전투 정보창·장비 상세·증강 표시 일치안 미승인, 미디어 제작 제외.


<a id="data-restored-info-approved-20261021"></a>
## 복원된 전투 정보 표시 일치 승인 — 2026-10-21
사용자 직전 상세안 승인. data.restored-info.v1.20261021.
- 요원/장비/인챈트는 원작전 확정 구성, HP/보호막/cooldown은 복원 전투 상태, 증강은 원run 확정 목록, 적용 대상·발동 조건은 현재 전투 구성/효과 상태를 사용. 로비 최신값 혼입·UI 독립 전투계산 원장 금지; 실제 상태 소유자의 표시 계약 사용.
- 복원 완료 정지 전장에서 기존 요원 정보보기/사거리/장비 상세 사용 가능. 열기·닫기·요원 전환 API0. 정보 확인 중 복원 정지 유지, 닫기와 전투 계속 입력 분리. 일반 전투 정보창의 기존 시간 정책을 임의 변경하지 않음.
- 복원 과정 정비·증강 적용 완료 시 장비 아이콘/상세/세트 충족/증강 적용 설명을 일관 갱신. 일부 구값·일부 신값 혼합 금지. 누락을0/미장착으로 변환 금지.
- 아이콘만 누락은 대체아이콘+확인된 이름. 필수 전투 상태 누락은 기존 복구 유지, 표시만 정상으로 꾸미지 않음. 일반 UI에 버전/메모리/진단값 노출 금지.
- 실제 Player 복원 전후 정보창/장비 상세/전투 적용값 일치, 정비·증강 변경/빠른 요원 전환/반복 열닫기 GET·구독·객체/메모리 확인. 미검수는 미검수로 유지.
Server 실제 필드/버전/부분응답·원복구 계약 공급 및 부족 구현, Unity 기존 owner/Controller 정보 표시·전투 소비/수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 응답 연결·메모리 증가 원인·Player API/메모리 전후 우선 지속, 새 UI를 핵심 연결 선행 대기로 삼지 않음. 신규 정비·증강 확정 시 열린 정보창 갱신 상세안 미승인. 미디어 제작 제외.


<a id="data-open-info-refresh-approved-20261021"></a>
## 정비·증강 확정 후 열린 정보창 갱신 승인 — 2026-10-21
사용자 직전 상세안 승인. data.open-info-refresh.v1.20261021. 기존 목록 연속성/원run 정보 표시 정책 보완.
- 같은 요원/장비가 유효하면 상세 대상·스크롤 유지, 변경 항목만 갱신. 동일 장비 능력/인챈트 변경은 상세 갱신, 장착 해제는 해당 장비 상세 유지·장착 해제됨 표시, 실제 삭제/소비 확정은 해당 상세 닫고 요원 정보 복귀. 부분 응답 누락을 삭제로 해석 금지. 슬롯 교체로 확인 중 상세 대상을 몰래 교체 금지.
- 증강 추가 시 목록/적용 대상/조건 설명 일관 갱신. owner가 장비/세트/증강 상태 일관 반영한 뒤 통지, UI별 독립 계산/추가GET 금지. HP 같은 실시간 값은 기존 전투 상태 통지 사용, 매번 전체 장비목록 생성 금지.
- pointer down/up 사이 대상 변경 시 입력 취소. 표시 갱신이 장착/해제/스킬 명령을 발생시키지 않음. 기존 Controller 처리·Bind/Unbind 수명 재사용.
- 현재 보는 장비/적용 조건의 유의미한 변경에만 정보가 갱신되었습니다 짧게 안내. HP 변화마다 안내/창 재개방 금지.
- 실제 장비 교체/해제/소비·증강 추가·응답 역순 시험. 대상/scroll 유지, 실제 전투와 표시 일치, 오입력/불필요GET0, 반복 갱신 객체·구독·자산/메모리 확인.
Server 기존 부분응답/명시삭제·원장비ID/버전 계약 공급·부족 구현, Unity owner 통지/열린 상세 갱신·입력·수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 서버 연결·메모리 증가 원인·Player API/메모리 전후 우선 계속. 신규 보상 수령 후 결과/로비재화/보관함 동시 표시 갱신안 미승인, 미디어 제외.


<a id="data-claim-display-approved-20261021"></a>
## 보상 수령 후 결과·로비 재화·보관함 표시 갱신 승인 — 2026-10-21
사용자 직전 상세안 승인. data.claim-display.v1.20261021. 기존 claim 응답완결/재화/원키·저장 정책 보완.
- 확정 응답 검증 후 보상 상태·재화·아이템 owner에 일관 반영하고 통지. 충분 응답 후 지급확인 GET 금지. 결과는 해당 영수증 실제 지급내역/완료, 로비는 최신 확정 잔액, 보관함은 확정 아이템 변경, 중단 작전 영역은 갱신된 작전/보상 상태. 획득량과 현재 잔액 구분.
- 연출은 확정 내역 표시만 담당. 연출 종료/skip/로비 이동에서 재화·아이템 추가 실행 금지. 실제 지급과 화면 연출 수명 분리.
- 과거 원영수증 복구는 당시 지급 확인 가능하나 최신 잔액 rollback 금지. owner별 버전/부분응답 계약 준수; 변경분 연속성 부족 시 해당 상태만 복구. 누락을 빈 목록/0/삭제로 해석 금지.
- 화면 종료 후 원요청·저장 소유자 유지; 유효 계정 owner에 반영하고 닫힌 View 재개방 금지. 지급 확정/기기 저장 실패는 저장만 재시도, 불명확은 원요청/원키 복구.
- 실Player 수령 연타/응답유실/연출skip/즉시로비/보관함재진입 시험. 지급1·충분응답후추가GET0·표시일치. 원문응답/연출객체/구독 잔류 및 API·메모리 전후 확인, 필요한 원영수증/복구기록 보존.
Server 실제 claim 완료 응답·owner별 버전/보상 상태 계약 공급·부족 구현, Unity ClaimComplete 실제 owner 반영/결과·로비·보관함 소비·저장/수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 응답 연결·메모리 증가 원인·Player 전후 우선 지속. 신규 보상 직후 재화 사용·아이템 변경 처리순서안 미승인. 미디어 제작 제외.

<a id="equipment-merge-detail-response-v2-20261021"></a>
### 열린 상세 갱신용 합성 응답 보완 — DATA-OPEN-INFO-REFRESH Server
기존 내부 EquipmentMergeStore 명령에 선택 responseVersion=equipment.merge-receipt.v2.20261021. 생략은 기존 v1/동일 원요청hash. v2영수증은 v1필드와 effectSnapshot(equipmentEffectStateReaderSchema: 결과장비 확정효과·인챈트)을 동일원자commit에 저장/재생한다. resultInstanceId 유지, consumedInstanceId 명시소비, resultLocation/vacatedLocation 및 instance/effect/inventoryRevision 기존의미 유지. 결과장비 하나의 부분갱신이며 전체목록·HP·최종착용능력 전체대체 아님; 미공급 항목 unchanged. v1은 효과상세 충분응답으로 승격하지 않음. 원키동일body/version 재생만 허용, 버전변경409. linked EquipmentHealthMergeStore는 동일선택필드를 중첩merge에서 사용하고 기존 HP 영수증과 함께 원자저장/재생. 운영 public route/source 미설치, 도메인시험 근거만 있음. 기존 인계 #data-open-info-refresh-server-result-20261021 참조.


<a id="data-post-claim-command-approved-20261021"></a>
## 보상 수령 직후 재화 사용·아이템 변경 처리 순서 승인 — 2026-10-21
사용자 직전 상세안 승인. data.post-claim-command.v1.20261021. 기존 지급/다중명령/저장/버전 정책 보완.
- 지급 확정 응답 검증·관련 owner 반영·필수 저장 완료 후 관련 재화/아이템 사용 가능. 보상 연출 종료는 선행 조건 아님. 결과 불명확은 관련 소비 제한·원키복구, 지급확정 저장실패는 저장 재시도 후 관련 소비 허용. 독립 재화·아이템 작업은 기존 조건으로 허용, 화면전체 임의잠금 금지.
- 처리 중 누른 강화/장착 등 소비 입력을 나중에 자동 실행하지 않음. 현재 진행중 안내 후 갱신된 비용/대상을 사용자가 다시 확인·명시 입력해야 함.
- 보상과 후속 강화/구매/장착은 각 명령·원요청 식별자 분리. 서버가 후속 명령 시 실제 잔액/소유권/조건 검증. 유효 응답이 충분하면 선행 확인GET 추가 금지. 원키복구와 새의도 명령 구분.
- 후속 소비 후 과거 보상 응답이 재도착해도 최신 잔액/장비 상태 rollback 금지. owner별 버전 연속성·원영수증 보존, gap/불명확 범위만 복구.
- 실Player 수령직후 강화/장착·연타·역순응답·저장실패 재시도 시험. 지급/소비 각각1회·자동소비0·최신상태유지·불필요GET0. 반복 후 원요청기록/콜백/임시객체 잔류와 메모리 전후 확인.
Server 실제 후속 명령 권한/원키·owner버전 계약 공급·부족 구현, Unity 관련명령 제한/명시입력·owner소비·저장·수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 서버 연결·증가 원인·Player API/메모리 전후 우선 지속. 신규 획득 아이템 보관함 찾기·확인안 미승인. 미디어 제외.


<a id="data-reward-item-locate-approved-20261021"></a>
## 획득 아이템 보관함 찾기·확인 승인 — 2026-10-21
사용자 직전 상세안 승인. data.reward-item-locate.v1.20261021.
- 지급 완료 아이템 상세에 실제 보관 대상이면 보관함에서 보기 제공. 개별장비는 지급된 instance ID, 중첩재료는 해당 항목+현재보유량, 재화는 획득내역. 별도 보관은 확정응답의 실제 위치로 연결. 작전 인벤토리/계정보관함 귀속 정책 유지, 이름으로 다른 물건 연결 금지.
- 필터에 숨겨지면 이유와 필터 해제하고 보기 제공, 사용자 필터 무단변경 금지. 이동 대상 짧게 강조, 자동장착/소비 금지.
- 과거 영수증은 당시 획득량, 현재 상세는 확인된 현재 상태/보유량. 소비·합성된 물건을 동명 다른물건으로 대체하지 않음.
- 유효 지급응답/보관함 상태 재사용. 페이지 미로드면 기존 지원 조회로 필요범위만 취득. 부분목록에 없음을 소비/삭제로 단정 금지. 지원 원ID/위치/조회 계약 부재는 미확인, 임의 매핑/API 추측 금지.
- 동일이름장비/필터숨김/중첩재료/소비후아이템 검수. 정확 대상/의도한 선택·필터/불필요GET0, 결과·보관함 왕복 구독·참조/메모리 확인.
Server 지급 원ID/보관귀속·현재상태/조회 계약 공급·부족 구현, Unity 기존 상세/보관함 이동·필터선택/수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 응답 연결·메모리 증가 원인·Player API/메모리 전후 우선 지속. 신규 결과→보관함→복귀 위치 유지 상세안 미승인. 미디어 제외.


<a id="data-result-inventory-return-approved-20261022"></a>
## 결과·보관함 왕복 위치 유지 승인 — 2026-10-22
사용자 직전 상세안 승인. data.result-inventory-return.v1.20261022.
- 결과 보상상세→보관함→뒤로는 동일 결과/보상항목/scroll 복귀. 지급연출 재생·수령 재전송 금지. 보관함 장착·합성·소비의 최신 상태 보존, 당시 획득내역 유지. 뒤로가기는 표시 위치 복원이며 경제상태 rollback 아님.
- 결과ID/선택항목/목록위치 등 작은 복귀정보만 소유. 전투scene/결과View 전체 참조 유지 금지. 유효 영수증·owner 재사용, 왕복 자체GET0.
- 계정 전환/명시 결과종료로 복귀대상 무효화 시 구화면 재개방 금지, 현재계정 정상흐름. 항목 실제부재는 유효 인접위치/자동 다른선택 금지. 앱재시작은 기존 복구흐름 적용.
- 반복왕복/장비변경/빠른뒤로/계정전환 실Player 시험. 위치·최신상태 유지, 중복지급/불필요GET0, 화면객체/구독/자산·메모리 누적 확인.
Server 기존 원결과/영수증·최신owner 계약 공급·부족 구현, Unity 기존 navigation/상태owner·복귀정보 수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지.
사용자 추가 우선지시: ClaimComplete 보상 응답 account/wallet/progress 실제 반영과 archive 소비 연결 계속. RunArchive319에서 원응답 문자열 생존/할당 증가 원인 확인: 실제 retained root와 QA 계측/async·task·로그/의도 보관 구분, 한 원인씩 최소 수정·동일조건 Player API/메모리 전후 검수. WeakReference 생존만으로 누수 확정 금지. 원영수증/영속복구 필수자료 삭제·무조건 GC로 개선 위장 금지. 실제 파일/QA crypto/메모리 pending과 실제기기·프로세스재시작 구분.
신규 재실행 후 영수증 반영·정리안 미승인. 새 UI를 핵심 연결 선행 대기로 삼지 않음. 미디어 제외.


<a id="data-receipt-restart-apply-approved-20261022"></a>
## 재실행 후 저장 보상 영수증 반영·정리 승인 — 2026-10-22
사용자 직전 상세안 승인. data.receipt-restart-apply.v1.20261022.
- 파일저장과 owner반영 완료 구분. account/run/응답version 검증 후 각 owner와 대조. 미반영 유효결과는 계약대로 반영, 동일결과는 재가산0, 더최신 owner는 유지/당시지급내역 보존, gap·해석불가는 필요한범위 기존복구.
- 유효 저장영수증으로 충분하면 재수령명령/확인GET0. 실제불명확·필수부족만 원요청/원키복구. 금융/진행 owner version 독립 판단.
- 일부 owner반영 후 종료 시 재실행 대조로 필요한부분만 적용. 완료표시만으로 필수반영 생략금지/표시없다고 재가산금지. 기존 snapshot/patch·영속성 계약 준수.
- 반영·저장완료 후 소비자없으면 원문문자열/파싱객체/버퍼 참조해제. 결과View에는 필요한 표시정보 제공. 영수증파일은 기존 보존·복구정책 유지, RAM절감 목적 필수복구파일삭제0.
- 저장직후/일부반영/완료 후 새프로세스 시험. 중복지급·최신상태역행·불필요API0. QAcrypto/실기기 구분. 원문 retained root/Task·로그·QA계측·의도보관을 구분해 최소수정/Player 동일조건 전후. 약참조생존만 누수확정·강제GC만 개선주장 금지.
Server 기존 원영수증·계정귀속·owner별 버전/복구 계약 공급·부족 구현, Unity archive 소비/ClaimComplete 실제owner·영속반영·참조수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자 우선지시 실제보상반영·원문메모리원인·Player API/메모리 검수 지속. 신규 손상/호환불가 영수증 복구안 미승인, 미디어 제외.


<a id="data-receipt-repair-approved-20261022"></a>
## 손상·호환 불가 영수증 복구 승인 — 2026-10-22
사용자 직전 상세안 승인. data.receipt-repair.v1.20261022.
- 파일 일시접근실패=원본유지·읽기재시도, 검증실패/손상=해당기록 적용중단·원본보존, 미지원version=기존호환/업데이트, 다른계정=현재계정미적용. 읽기실패를 미수령/보상소멸로 단정 금지.
- 별도 검증된 원요청/원키가 남으면 기존서버복구. 손상내용 추측ID·새수령키 금지. 원요청 확인불가면 기존문의, 자동재지급/초기화0.
- 복구응답 account/run/request 귀속검증→새기록 저장·재읽기검증→정상기록 사용. 중간실패 원본덮어쓰기0. 과거응답 최신owner rollback 금지, 기존 원본보존정책 유지.
- 문제있는 기록/충돌작업만 제한. 독립 정상기록/조회 보존. 일반UI는 안내·문의코드, 원문/암호자료 노출 금지.
- 잘린파일/검증불일치/미지원version/다른계정/복구저장중종료 시험. 중복지급·역행·원본유실0. 반복실패 원문/파싱/재시도 참조 누적 확인, 새프로세스 복구재읽기 검수. 기존 자동재시도 예산 유지.
Server 기존 원요청·귀속/복구 계약 공급·부족 구현, Unity 검증/원본보존·저장교체·오류분류·owner 반영/수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자 우선지시 ClaimComplete 실제반영·원문 retained원인 확인→최소수정→Player API/메모리 동일조건 전후 지속, QAcrypto/합성SQL/실기기·새프로세스 미검증 구분. 신규 복수 미처리 영수증 순서·실패범위안 미승인. 미디어 제외.


<a id="data-receipt-batch-recovery-approved-20261022"></a>
## 여러 미처리 영수증 복구 순서·실패 범위 승인 — 2026-10-22
사용자 직전 상세안 승인. data.receipt-batch-recovery.v1.20261022.
- 동일계정 기록만 처리. 금융/보관함/진행 owner별 버전·적용계약으로 판단, 파일날짜/배열순 가산 금지. 동일/더최신 상태 보존.
- 동일 상태 반영 직렬화, 변경분은 선행version 확인후 반영. snapshot도 범위/version검증, 부분응답 전체대체 금지. owner별 독립sequence를 전역순서로 비교하지 않음.
- 독립 정상기록 계속, 선행의존 변경분은 해당owner 후속만 대기. 손상/미지원 원본보존·기존복구, 완료기록 중복반영 없이 건너뜀. 오래됐다는 이유만 필수기록삭제 금지.
- 전체 원문 일괄전개 금지, 처리할 기록부터 읽고 반영·저장후 소비자없는 원문/파싱/버퍼 참조해제. 재중단은 실제owner/영속기록 재대조, 메모리순번만 완료근거 금지.
- 정상/중복/역순/손상혼합·중간종료 검수. 중복지급·최신역행0/독립작업진행. 기록수 증가에 따른 API/peak/잔류 비교, QA계측 문자열참조 별도 구분.
Server 기존 owner별 version/부분응답·원키 복구 계약 공급·부족 구현, Unity 복수 archive 소비/직렬반영·격리·영속/수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자 우선 실제 ClaimComplete 반영·원문 retained 원인→최소수정→Player API/메모리 동일조건 전후 지속. 실HTTP/합성SQL/QAcrypto/실기기·새프로세스미검증 구분. 신규 복구중 사용자 표시·이용가능 기능안 미승인. 미디어 제외.


<a id="data-recovery-availability-approved-20261022"></a>
## 복구 진행 중 표시·이용 가능 기능 승인 — 2026-10-22
사용자 직전 상세안 승인. data.recovery-availability.v1.20261022.
- 로비 이용 가능 시 관련 안내영역에 이전 진행 내용을 확인하고 있습니다 표시, 무관 조회 차단 금지. 정상처리=진행안내/관련소비영향=버튼 대기사유/일부실패=일부 진행 내용을 확인하지 못했습니다+기존복구/필수반영저장완료=관련제한해제.
- 모르는 퍼센트/ETA 생성 금지, 일반UI에 내부영수증개수·단계명 노출 금지. DEV만 기존진단 처리건수·대기원인/API·메모리 표시.
- 완료 시 현재상태만 갱신, 화면강탈/예약입력실행0. 필요 시 명시 결과보기 제공, 복구를 신규지급연출로 반복하지 않음.
- 안내닫기는 표시만 숨김, 확정처리·필수저장 취소 아님. 제한버튼에서 사유/복구 재확인 가능. 왕복으로 복구중복요청·자동재시도예산 초기화0.
- 복구중 다른화면/안내닫기/부분실패/완료순간입력 실Player 시험. 독립기능이용·관련제한/해제 정확, 자동소비/중복요청0, 반복안내 구독·객체·원문잔류 확인.
Server 기존 오류/원결과·권한 경계 공급·부족 구현, Unity 기존복구 owner 표시·관련입력제한/수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 ClaimComplete owner반영·archive소비/원문메모리원인→최소수정→Player API/메모리 전후 우선 계속. 신규 복구중 새보상 요청 처리안 미승인, 미디어 제외.


<a id="data-recovery-new-claim-approved-20261022"></a>
## 복구 중 새 보상 수령 요청 처리 승인 — 2026-10-22
사용자 직전 상세안 승인. data.recovery-new-claim.v1.20261022.
- 동일 지급건은 화면과 무관하게 기존복구에 연결/새키수령0. 다른보상이나 동일owner 선행복구 필요면 관련수령 제한. 독립·계약상안전은 허용. 영향범위 미확인은 해당수령만 확인전 제한.
- 제한중입력 자동예약수령0. 이전 보상 처리 확인 후 받을 수 있습니다 안내 후 복구완료 버튼활성, 새명시입력 필요. 이미전송은 취소로간주0/원요청복구.
- 새보상/구복구응답 혼합 시 owner별version/원영수증 계약, 최신잔액·아이템 rollback0. 지급내역과 현재상태 구분, 충분응답후GET0.
- 일괄받기 기존원자성/부분성공 계약 유지, 임의개별요청분할 금지. 실제지원되는 제외수령만 대상/제외사유 표시. 새부분지급기능 승인 아님.
- 동일보상연타/다른화면동시수령/복구중일괄/역순응답 시험. 중복지급·자동수령·역행·불필요GET0. 동시요청 증가 후 원문·대기작업·callback 해제/Player메모리 확인.
Server 기존 지급식별/원키·권한/일괄계약 공급·부족 구현, Unity 기존 commandowner 합침/관련제한·명시입력/실제owner·수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 ClaimComplete·archive 소비/원문 retained 원인→최소수정→Player API·메모리 전후 우선 지속. 신규 보상처리중 앱백그라운드·복귀 표시입력안 미승인. 미디어 제외.


<a id="data-claim-app-resume-approved-20261022"></a>
## 보상 처리 중 앱 백그라운드·복귀 승인 — 2026-10-22
사용자 직전 상세안 승인. data.claim-app-resume.v1.20261022.
- 백그라운드≠전송요청취소. 원요청/원키/확정결과 기존영속정책보존, OS중단가능하므로 백그라운드 완료 가정 금지.
- 복귀시 실제단계: 진행중은 기존작업연결, 지급확정·반영·필수저장완료는 최신표시, 저장미완료는 저장재시도, 불명확은 원키복구, 프로세스재시작은 저장영수증/owner대조. 복귀 자체 재수령/전체계정GET0.
- focus/활성화/네트워크 신호는 동일복구 합침, 기존자동재시도예산 유지. 이전 눌림입력 폐기, 새입력만 처리.
- 완료연출 강제재생0, 필요한 짧은안내/명시결과보기. 계정전환시 이전결과/재화/안내 새계정혼입0.
- 전송직후/응답직후/저장중 전환·빠른복귀·OS종료재실행 시험. 지급1·중복복구0·최신상태유지. 복귀구독/완료Task 원문참조 추적. 데스크톱focus vs 실제모바일 OS시험 구분.
Server 기존 인증/원결과·원키·권한 계약 공급·부족 구현, Unity 실제 lifecycle/중복신호합침·owner/영속·수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 실제 ClaimComplete·archive 소비와 원문 retained원인→최소수정→Player API/메모리 동일조건 전후 우선 지속. 신규 보상중 인증만료 복구안 미승인. 미디어 제외.


<a id="data-claim-auth-recovery-approved-20261022"></a>
## 보상 처리 중 인증 만료 복구 승인 — 2026-10-22
사용자 직전 상세안 승인. data.claim-auth-recovery.v1.20261022.
- 인증실패≠지급실패. 이전응답유실 원요청/원키보존. 동일계정갱신은 기존흐름연결, 재로그인필요는 관련변경제한/안내, 다른계정은 원기록격리. 지급확정 후 로컬저장만남으면 검증결과 기존저장유지.
- 같은세션 동시인증만료는 기존인증owner 진행작업 공유, 화면별갱신0. 인증성공으로 보상자동재시도예산초기화0·인증/네트워크 중복전송0. 기존 인증 갱신 계약 유지.
- 재로그인 원계정확인 후 기록/현재owner대조·필요복구. 새수령키0, 기존권한검증 실패기록 임의적용0.
- 일반UI 계정확인필요/로그인행동, 토큰/원문/내부오류노출0. 안내닫기 원기록삭제0, 구계정늦은응답 새계정View/owner혼입0.
- 전송전만료/지급후유실+만료/동시만료/타계정로그인 검수. 중복지급·새키재수령·계정혼입0. 인증/보상복구 API분리계측, 완료인증Task/callback 원문참조 확인.
Server 기존 인증·원키/권한 계약 공급·부족 구현, Unity 인증owner/복구·계정세대·실제owner/영속수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자 우선 ClaimComplete 실제반영·archive소비·원문 retained원인→최소수정→Player API/메모리 전후 지속. 신규 점검·서비스제한 중 보상복구안 미승인. 미디어 제외.


<a id="data-claim-maintenance-approved-20261022"></a>
## 점검·서비스 제한 중 보상 복구 승인 — 2026-10-22
사용자 직전 상세안 승인. data.claim-maintenance.v1.20261022.
- 명시점검/기능제한만 해당안내, 일반오류는 기존오류복구, 무응답은 결과불명확, 확정후저장실패는 로컬저장. 일반503을 점검/미지급 근거로 오인 금지.
- 전송전수령은 제한해제후 명시입력, 전송후는 원키보존/서버허용시점 복구. 점검전처리 가능성 보존, 새키재수령0.
- 실제서버 재시도시점/기존점검확인 규칙 준수, 안내왕복/앱복귀/연타 중복요청0. 예정시각경과≠재개확정/자동수령, 기존 자동재시도예산 유지.
- 검증된 확정결과/영수증 기존정책 저장, 실제서비스제한 준수. 점검이유삭제/연출재실행0. 기한연장/지급은 기존운영정책, 클라임의연장·소멸확정0.
- 요청전제한/지급직후점검/장기점검/예정시각변경/재실행 시험. 중복지급·원본유실·요청폭증0, 안내/타이머/복구Task 원문·callback 해제 확인.
Server 기존 점검·권한/원키 계약 공급·부족 구현, Unity 실제 응답분류/관련제한·복구/영속수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지.
사용자 우선지시 정상 v2 claim 발행→account/wallet/progress 실제 상태변경·저장·표시 연결 및 원문문자열 heap root/QA관찰영향 구분→근거있는 최소수정→Player API/메모리 전후. matching-only 수락/legacy정상경로/미제거GET을 완료로 승격 금지. version없는 관측 잔액 덮어쓰기 금지, 원요청 version/hash 고정·구영수증호환 유지. 충분 실제반영 검증전 fallbackGET만 제거 금지. 실제HTTP·합성SQL/QAcrypto/실Player·새프로세스·실기기 구분. 신규 정상수령·복구 공통반영 세부안 미승인, 기존 승인 실연결을 기다리게 하지 않음. 미디어 제외.


<a id="data-claim-common-apply-approved-20261022"></a>
## 정상 수령·재시도·재실행 복구 공통 반영 승인 — 2026-10-22
사용자 직전 상세안 승인. data.claim-common-apply.v1.20261022.
- 정상확정응답/원키복구응답/검증저장영수증은 동일 검증·owner반영·필수저장·통지 경로 재사용. 경로별 가산코드 복제 금지. 원천별 검증/신뢰경계는 보존.
- 잔액일치만 완료판정/불일치 무조건덮어쓰기 금지. owner별version/범위에 따라 반영필요·이미반영·더최신보유·복구필요 구분. 금융/진행 독립sequence 유지.
- 필수상태 검증 후 일관 반영·통지, 일부준비만 전체완료 표시0. 중간종료는 영속기록/현재owner 대조로 중복반영 방지.
- 기전송원요청의 version/hash/key 변경금지, 구영수증 부족은 기존복구. 충분v2 실제owner반영 검증후 중복GET제거, GET만 선삭제0.
- 동일보상 정상/유실복구/새프로세스복구 최종재화·아이템·진행 일치 시험. 경로별API/원문수명 분리측정. 필수영속자료보존하며 원문 heap-root/QA계측 원인구분·불필요참조만해제.
Server 실제원응답/owner별version·구호환 공급·부족 구현, Unity 정상v2발행/공통반영·archive소비/실제owner·영속·수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자 우선 정상v2 실제상태반영·원문메모리원인→최소수정→Player API/메모리 전후 지속. 신규 반영완료 후 결과표시실패안 미승인. 미디어 제외.


<a id="data-claim-display-failure-approved-20261022"></a>
## 보상 반영 완료 후 결과 화면 표시 실패 승인 — 2026-10-22
사용자 직전 상세안 승인. data.claim-display-failure.v1.20261022.
- 서버지급확인/실제owner반영/필수저장 완료 후 표시만 실패하면 보상은 반영됐습니다. 결과 화면을 표시하지 못했습니다 안내·다시보기/로비로 제공. 완료조건 미확인은 기존복구, 지급실패로위장0.
- 다시보기는 검증영수증/현재owner로 화면만 준비. claim/재가산/지급확인GET0. 아이콘누락은 대체+확인명칭수량, 표시문제로 독립정상보관함·재화이용 차단0.
- 실패View 소유구독/임시객체/자산사용권 정리후 재시도, 공유자산/필수영수증 보존. 연타는 준비1개로합침.
- 로비/계정전환후 구결과창 늦은재개방0, 이전준비는 자기임시자원반환. 당시획득과현재보유 구분.
- View생성실패/아이콘누락/준비중로비/반복재시도 Player시험. 재지급·중복반영·불필요API·닫힌화면재개방0. 실패View/완료Task 원문참조 및 반복객체/구독/메모리 확인.
Server 기존확정영수증·owner계약 공급·부족 구현, Unity 완료판정/표시전용복구·View수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자우선 정상v2 실제 account/wallet/progress 반영·archive소비·원문heap root/QA영향→최소수정→Player API/메모리 전후 지속. 신규 일부owner만 반영가능할 때 완료판정·복구범위 상세안 미승인. 미디어 제외.

<a id="crystal-wallet-observation-version-20261022"></a>
### 기존 wallet GET의 잔액 관측 버전 — 2026-10-22
정상 ClaimComplete 실제 소비 부족경계 보완. GET /v1/wallet/crystals 기존필드 유지, 추가 stateVersion={version:'crystal-wallet.snapshot.v1.20261022',accountId,mutationSequence,coverage:['BALANCES']}. 단일 SQLite SELECT로 활성정책/통화/free·paid잔액/MAX(account_mutations.sequence)를 함께 읽는다. sequence는 잔액 snapshot 관측 watermark이며 새 wallet mutation이나 전체account snapshot이 아니다. 동일account·currencyId·policyVersion의 잔액만 claim owners.wallet.sourceMutation.mutationSequence와 순서비교: 더최신 적용, 같으면 동일내용/no-op 대조, 과거는 최신잔액보존. 정책은 계정sequence 없이 바뀔 수 있으므로 policyVersion/교환설정 순서로 사용금지; 정책불일치는 기존 해당정책 복구. UNCHANGED 유지, legacy unversioned관측에 이버전을 추정부여하지 않는다. 충분한 유효 관측/영수증은 추가GET0, 실제무효화만조회. 기존 claim v2 원요청/version/hash/저장응답 불변. 검수/제한은 Server인계 #data-claim-wallet-version-server-result-20261022.


<a id="data-claim-partial-apply-approved-20261022"></a>
## 보상 응답 일부 반영 가능 시 완료 판정·복구 범위 승인 — 2026-10-22
사용자 직전 상세안 승인. data.claim-partial-apply.v1.20261022.
- 서버 지급확정≠클라전체반영완료. 필수반영 남으면 확인된 지급과 진행정보반영중을 구분, 재수령 유도0.
- owner별 유효신규=범위반영/동일=재적용0/더최신=유지/gap=관련복구/명시UNCHANGED=유지/필수누락·검증실패=완료보류. 미공급을변경없음·0으로해석0.
- 의존값은 필요한상태준비후 일관표시, 확정·저장된독립상태rollback0. 독립owner sequence 교차비교금지.
- 부족한진행만 복구가능시 정상재화·보관함재조회/재지급0. 기존API 최소지원범위사용. 모든필수상태반영 또는 검증된최신보존·필수저장 완료후 완료통지.
- 재화정상/진행누락·gap·최신잔액·부분반영중종료 시험. 중복지급/역행/성급완료0. 범위별 API계측, 완료부분때문 전체원문불필요보유 여부확인.
Server 기존필드·owner별version/coverage·부분복구 계약 공급·부족 구현, Unity 정상v2 실제반영/관련복구·완료판정/영속·수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자우선 정상v2 account/wallet/progress 실제변경·archive소비·원문heap-root/QA영향→최소수정→Player API메모리 전후 지속. 신규 재화정책변경 표시·사용안 미승인, 기존정상v2구현 선행대기 아님. 미디어 제외.


<a id="data-currency-policy-change-approved-20261022"></a>
## 재화 정책 변경 대응 승인 — 2026-10-22
사용자 직전 상세안 승인. data.currency-policy-change.v1.20261022. 가치/사용규칙 변경 승인 아님.
- 잔액최신≠정책최신. currency/policy와 BALANCES version 별도검증. 동일정책 유효잔액 반영, 명시정책변경은 관련사용조건갱신, 호환미확인은 관련소비만제한·확인, 과거영수증 당시지급보존.
- 지급재실행/임의재계산·재화변환0. 새정책 적용시점/기존재화처리는 실제서버계약.
- 실제변경·불일치근거 있을때 기존지원경로로 필요한정보만조회. 화면열기상시조회/잔액만으로정책최신승격/전체계정덤프0.
- 구매·강화확인중 비용/재화조건변경은 변경조건표시·새명시입력 필요. 기전송명령 원요청복구, 새조건자동재전송0.
- 잔액동일·정책변경/정책확인중구영수증/확인창조건변경 시험. 지급내역보존·구조건자동소비0·불필요GET0, 이전정책표시객체/구독·원문참조 확인.
Server 실제정책무효화/권한·원키/관측version 계약 공급·부족 구현, Unity 정책·잔액분리소비/확인입력·수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지.
사용자우선 진행정보 실제변경(progress projection metadata/order·독립원sequence) 반영, 모든필수owner완료·archive/새프로세스 및 원문heap-root/QA관찰영향 확인→최소수정→Player API/메모리 동일조건 전후. 정상v2 금융/아이템 및 versioned wallet실변경 부분완료 인정, VERIFIED_EMPTY를 실제progress변경근거로 대체0. fallbackGET은 충분실반영 검증후 필요한범위제거. 신규 다음구간해금 표시·진입안 미승인. 미디어 제외.


<a id="data-reward-unlock-entry-approved-20261022"></a>
## 보상 처리 후 다음 구간 해금 표시·진입 승인 — 2026-10-22
사용자 직전 상세안 승인. data.reward-unlock-entry.v1.20261022. 기존해금조건 유지.
- 검증된 progress변경·필수반영 완료후 잠금/진입표시 갱신, 보상연출종료를해금근거로사용0. 확인중은 기존확정상태/필요안내, 더최신progress유지, 누락/불일치는 관련복구.
- 구간보기는 해당준비화면 이동, 자동START/연료소비0. 실제출격 기존서버권한·확정절차.
- 결과/로비/구간목록은 동일progress owner통지 사용, UI별조건중복계산/재조회0. 보고있던다른구간 강제이동0.
- 원영수증 재시도/재실행 동일해금 반복연출0, 필요시 해금상태/구간보기만표시. 안내표시여부 불명확을 재지급·해금재실행 사유로사용0.
- 실제잠금변경 보상 정상/유실/새프로세스 검수. 상태·잠금일치/자동출격·중복해금0/충분응답GET0. 목록/결과View종료 객체·구독·원문메모리 확인.
Server 실제 progress 원sequence/표시정의·해금/권한 계약 공급·부족 구현, Unity 실제progress변경→표시·명시진입/영속수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자우선 실제진행변경·원문heap-root/QA영향→최소수정→Player API/메모리 동일조건 전후 지속. VERIFIED_EMPTY를 실해금근거로대체0. 신규 해금구간 리소스미준비 진입안 미승인. 미디어 제작 제외.

<a id="story-progress-observation-version-20261022"></a>
### 진행 원상태·독립 버전·표시 정의 공급 — 2026-10-22
기존 GET /v1/story-progress에 선택 header X-Story-Progress-Version: story.progress-observation.v1.20261022. 무header 기존응답 유지, 미지원header409 story_progress_version_conflict. 인증/계정검증 기존대로. 새응답은 기존 표시필드에 version, sourceOwner={accountId,kind:SNAPSHOT,beforeMutationSequence,mutationSequence,versionSource,contentHash,state:{story,unlocks}}, projectionDefinition을 추가한다. 원상태·progression_ledger에 연결된 STORY_PROGRESS account mutation은 단일SQL에서 읽음. sourceOwner의 계정귀속 외 나머지 구조/hash/독립sequence는 ClaimComplete owners.progress와 동일한 공유함수 사용. account 금융sequence로승격0. beforeMutationSequence=mutationSequence는snapshot원version이며PATCH basis아님.
projectionDefinition={version:'story.progress-projection.v1.20261022',narrativeVersion,coverage:['STORY_PROGRESS','OPERATION_UNLOCKS'],storyProgressOrder,operationIds,initialOperationId,metadata}. 기존 actOne정의/기존 createStoryProgressAcceptedResponse를 사용, 신규해금규칙 없음. order는 storyProgressOrder에서 highestStoryProgressId의 index, clearedOperationIds는 원clearedOperationsJson의 문자열목록을 operationIds순으로 중복제거·정렬, unlockedOperations는 initialOperationId와 raw unlockId의 operation: 접두사를제거한목록을 동일정렬. storyFlagsJson은 storyFlags/narrativeFlags에 사용, metadata는 기존 narrative표시메타데이터. raw story의 actId/highestClearedOperationId/narrativeVersion/lastProgressRunId/updatedAt 등은 기존표시응답과 대응(시각은millisecond→ISO). 원상태의 다른 unlock종류는 보존되지만 이projection의 표시범위아님. 정의를같은version/narrative범위에서재사용하며 무관화면왕복/성공claim직후GET강제0. 원내용hash는 source state만이며 메타데이터hash로사용0.
nonempty 원상태인데 실제원progressversion 부재/불일치, 문자열배열손상은503 story_progress_source_unavailable. 지원안되는act/narrative/highest/cleared/operation unlock은503 story_progress_projection_unavailable, 빈목록/order0으로추정0. 구데이터에독립sequence를발급하지않음. 새관측과claim raw원응답의 계정·정의·순서/hash/범위를검증한 뒤 실제owner에적용; 과거응답으로최신상태역행0/동일원본재가산0. 모든필수owner/저장완료는Unity실검수 필요. 기존 v2 필드집합/기전송version/key/hash/원저장영수증 변경없음. 근거 Server인계 #data-progress-observation-server-result-20261022.


<a id="data-unlock-resource-readiness-approved-20261022"></a>
## 해금 구간 필수 리소스 미준비 진입 승인 — 2026-10-22
사용자 직전 상세안 승인. data.unlock-resource-readiness.v1.20261022.
- 해금과자산준비 분리: 준비완료는 기존출격준비, 미준비는 추가다운로드필요/준비행동, 진행중은 실제준비표시, 실패는 해당단계재시도. 자산부재로해금취소/잠금회귀0.
- 구간보기는 기존정보/가벼운대체표시, 전체다운로드 자동개시0. 전투자산은 기존다운로드동의·네트워크정책. 미확인용량을확정값표시0.
- 필수파일/적용version 검증후출격확정, 구간변경시이전로딩완료 새선택오염0. 다운로드완료 자동START/연료소비0.
- 취소는소유작업/사용권정리·공유자산유지. 검증파일재사용/부족분준비, 자산실패로claim/progress재조회0.
- 다운로드중선택변경/취소재진입/검증실패/앱종료 시험. 해금유지/오진입·중복소비0. handle·로딩Task·버퍼 잔류 및 다운로드 vs 보상원문메모리 분리.
Server 기존콘텐츠/진행·입장계약 공급·부족 구현, Unity 자산준비·선택세대·입력/수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지.
사용자우선 49#story-progress-observation-version-20261022의 sourceOwner 독립seq/hash/raw + projectionDefinition 실제연결→nonempty progress/해금·필수owner/영속반영. 기존정의재사용으로claim후GET강제0, legacy호환/기전송key·version보존. 원문heap-root/QA영향원인확인→최소수정→Player API/메모리 동일조건 전후 계속. 신규 선택후진입조건변경안 미승인, 미디어 제작 제외.


<a id="data-entry-condition-change-approved-20261022"></a>
## 구간 선택 후 진입 조건 변경 대응 승인 — 2026-10-22
사용자 직전 상세안 승인. data.entry-condition-change.v1.20261022.
- 선택구간 유효시 선택/scroll 유지, 바뀐연료/편성/이용가능조건·버튼만갱신. 입장불가로다른구간자동선택0.
- 해금과입장가능 분리: 연료부족은부족안내/편성미충족은조건·편성행동/명시점검·기간종료는기존제한/필수자산version변경은필요준비/미확인은확정정보유지·관련출격보류. 기존응답·owner통지 재사용/상시조회0.
- 전송전변경은 새비용·대상표시후 명시입력. 전송후는 원요청복구, 새조건자동재전송/타구간대체0. 확정성공은원run복구.
- 구선택로딩완료가현재버튼활성화0. 공유검증자산재사용, 선택임시객체/구독정리.
- 연료변경/편성교체/기간종료/자산갱신/빠른선택 시험. 오진입·중복소비·자동재전송·불필요조회0. 로딩Task/callback 진행원문참조 확인.
Server 기존 입장·원키/조건version 계약 공급·부족 구현, Unity 선택유지/통지·입력세대/수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자우선 story-progress-observation-version-20261022 실제seq/hash/정의소비→nonempty진행·해금·영속반영 및 원문heap-root/QA영향→최소수정→Player API메모리 전후 지속. 신규 출격전송후대기 입력·화면이동안 미승인. 미디어 제외.


<a id="data-start-pending-navigation-approved-20261022"></a>
## 출격 확정 요청 후 대기 중 입력·화면 이동 승인 — 2026-10-22
사용자 직전 상세안 승인. data.start-pending-navigation.v1.20261022.
- 원구간/편성+출격확인중 표시, 연타 기존요청합침·새START0. 결과불명확 동안 원작전 충돌 명령만제한.
- 원요청 안전보존 후 로비이동 가능/출격확인은계속됩니다 안내. 화면닫기≠취소/환불. 보존실패는기존저장복구 우선.
- 준비화면에서성공은 필수상태·저장·준비후기존진입. 로비이동후성공은 작전이어가기/강제전투이동0. 명시미실행거절은사유·관련입력해제, 불명확은원키복구. timeout실패확정0.
- 다른구간열람이기전송대상변경0. 늦은응답원run반영·계정세대격리. 새출격은원결과정리·입장조건충족 후새명시입력.
- 연타/전송직후로비/유실/거절/늦은성공 시험. 생성·소비1/강제이동·오진입·중복0. 화면종료후필수기록보존·View/원문/callback불필요참조확인.
Server 기존 START/원키·거절/복구계약 공급·부족 구현, Unity 명령owner/영속보존·navigation/수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자우선 진행seq/hash/정의 실제nonempty변경·해금·영속/원문heap-root/QA영향→최소수정→Player API메모리전후 지속. 신규 출격확정후진입실패 로비표시안 미승인. 미디어 제외.


<a id="data-confirmed-entry-failure-approved-20261022"></a>
## 출격 확정 후 전투 진입 실패 시 로비 표시 승인 — 2026-10-22
사용자 직전 상세안 승인. data.confirmed-entry-failure.v1.20261022.
- 로비 원구간/확정편성 유지, 출격확정·전투화면준비필요 안내/작전이어가기. 결과불명확은 기존작전상태확인으로구분.
- 원run/확정정보로 실패준비만재시도, 새START/연료재소비0. 충분원정보 로비왕복/재시도GET0.
- 필수저장→필요자산→화면생성 등 실제선행순서로복구, 서버확인필요시원복구. 미완료단계를완료로표시0·전체처음부터재실행0.
- 진행작전은 원확정/작전내승인변경구성. 로비편성덮어쓰기0. 준비중로비이동은 소유작업/화면수명정리·원작전보존.
- 저장/자산/화면실패각재현·왕복/재실행 동일run·소비1·중복전투객체0. 실패View/로딩Task 원문·자산참조/Player메모리전후 확인.
Server 기존원START/원영수증·권한 계약 공급·부족 구현, Unity 로비표시·단계복구/원run·영속수명 담당. 기존 DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자우선 progress관측seq/hash/정의 실제nonempty해금·영속반영 및 원문heap-root/QA영향→최소수정→Player API메모리 전후 지속. 신규 반복실패 문의정보안 미승인, 미디어 제외.


<a id="data-entry-support-context-approved-20261022"></a>
## 반복 진입 실패 시 문의 정보 연결 승인 — 2026-10-22
사용자 직전 상세안 승인. data.entry-support-context.v1.20261022.
- 실패안내에 기존 다시준비/문의하기 제공, 실패횟수문의제한0. 문의열기 작전포기/원복구삭제0.
- 실제구간/실패단계/출격확정여부(불명확보존)/앱·콘텐츠version/기존오류식별코드 연결. 원문/토큰/암호파일 노출0·미확인원인손실 추정0.
- 짧은요약/문의정보복사, 성공한번안내·실패시직접선택가능. API/메모리 상세는DEV만.
- 문의표시/전송실패를보상·출격재실행으로연결0. 작성중복구완료는최신상태안내·본문보존/자동제출0. 접수≠자동환불·보상.
- 확정/불명확·저장/자산/화면실패 문의정보 정확성, 왕복/복사/전송실패 원작전보존·비밀노출/중복출격0. 문의요약때문 전체원문/전투객체 유지0.
Server 기존 문의/오류연결·권한 계약 확인·부족 구현, Unity 최소요약/기존문의 연결·모바일복사·수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지. 사용자우선 progress seq/hash/정의 실제nonempty해금·영속반영 및 원문heap-root/QA영향→최소수정→Player API메모리전후 지속. 신규 문의작성중복구/계정변경안 미승인, 미디어 제외.


<a id="data-support-draft-transition-approved-20261022"></a>
## 문의 작성 중 복구 완료·계정 변경 승인 — 2026-10-22
사용자 직전 상세안 승인. data.support-draft-transition.v1.20261022.
- 복구완료시 현재상태안내/작성본문보존. 진단요약은 작성당시/현재확인 구분. 자동제출/자동취소0.
- 문의계속/작전돌아가기 명시선택, 돌아가기 즉시전투재개0·기존준비복귀. 초안보존폐기는기존정책/임의장기저장0.
- 계정변경시 원계정초안·작전/요약격리·타계정제출0. 동일계정재인증 확인후이어가기. 구응답새문의갱신0.
- 제출시 본문+확인진단 확정, 이후복구완료로 접수내용자동변경0. 제출불명확 기존문의key/중복방지 계약, 게임복구키와분리.
- 작성중완료/제출직전변경/인증·타계정/종료후늦은응답 시험. 본문보존·자동제출/계정혼입0, 종료구독/초안객체 원문·작전참조해제 확인.
Server 기존문의·계정/원key 계약 공급·부족 구현, Unity_11 초안/상태통지·계정격리·수명 담당. DATA-LIFECYCLE WAIT AND/후속키 유지.
사용자 최신우선: 충분한 정상v2경로 후속GET 축소·owner별 부분복구/모든필수완료판정·새프로세스 Player검수 지속. Unity_10 실제progress해금 및 Regex원문root 최소수정 근거 재사용(해당변경범위), 전체메모리절감/전체통합완료로 승격0. GET만선삭제0/정상·필수복구API 구분·원영수증/버전/정책 보존. 담당 Unity_11 01a15f65-66e6-7f50-bc60-234c324bea1b, Unity_10 편집종료 인계. 신규 제한기능재활성화 세부안 미승인. 미디어 제외.


<a id="data-battle-entry-assets-approved-20261022"></a>
## 구간별 필수 자산·전투 진입 준비 계약 승인 — 2026-10-22
계약 data.battle-entry-assets.v1.20261022. 사용자 승인 및 Server/Unity_13 구현·한 구간 한 편성 실제 전투 Player API/메모리 검수 지시.
- 기존 승인 구간·편성 한 조합을 실제 지원 원천에서 식별한다. 전장/배경/객차/충돌, 편성 요원/드론/필수 동작, 해당 적, 필수 투사체/식별 효과/전투 UI의 실제 ID·버전·런타임 참조를 결속한다. 기존 승인 자산 재사용; 전체 요원 선로딩/신규 미디어 제작0.
- 준비 완료는 선택 구간·확정 편성 일치, 필수 파일/버전 검증, 실제 전투 객체 참조 및 상태·입력·카메라 연결을 모두 충족해야 한다. LobbyKey 검증/파일 존재를 전투 준비 완료로 대체하지 않는다. READY 강제 우회0.
- 기존 출격 조정 객체가 준비 진행을 소유하며 자산 로더·전투 구성 요소는 결과를 전달한다. View에 로딩/출격 규칙 복제0. 계정/run/구간/편성 세대 변경 후 구응답 격리, 공유 자산 마지막 소비자 해제 계약 보존.
- 실패 단계만 재시도한다. 자산 실패를 진행 재조회/재해금/새 START/재소비로 연결하지 않는다. 이미 확정된 run·원키·snapshot 보존, 불명확 결과는 기존 복구 계약 적용.
- Server는 기존 구간/편성/START snapshot·버전/권한/지원 범위를 확인하고 부족한 실제 공급을 구현한다. Unity_13은 실제 자산 매핑·입장 composition·전투 연결과 통합 검수 소유. 미공급 의존은 정확히 WAIT에 남기며 독립 구현을 진행한다.
- 수용: 한 구간 한 편성 준비→출격 확정→실제 전장 진입→기본공격/명중·상태 반영→종료/해제. DEV 시험에서 실제 지원 경로를 사용하며 임의 운영 경제 실행/배포 권한으로 확대하지 않는다. 실제 API 정상/복구·논리 요청/재시도, 상주/peak/할당/반복 후 잔류·텍스처/객체를 같은 조건에서 전후 측정. 합성/QA crypto/플랫폼 한계 명시. 기존 Player400/새 프로세스408과 Regex root 증거는 해당 범위만 재사용.
기존 DATA-LIFECYCLE AND/후속 등록키 유지. 실행 가능한 역할 작업만 큐 등록, 선행 미충족 통합 완료 항목 선등록0. 이전 제한 기능 재활성화안은 미승인 유지.


<a id="data-battle-first-frame-approved-20261022"></a>
## 첫 화면·첫 공격 시작 기준 승인 — 2026-10-22
계약 data.battle-first-frame.v1.20261022. 직전 제안 사용자 승인 및 Server_5/Unity_13 구현 지시.
- 요원·적·객차 배치 및 상태 연결 완료 전 공격/이동/전투 시간 진행 금지. 카메라/발 위치/HP·보호막·대상 표시가 같은 전투 상태와 일치한 뒤 전장 노출.
- 신규 전투는 기존 기본공격 초기 대기 규칙 적용, 로딩 시간을 공격 대기시간으로 누적하지 않음. 새 공격 수치/선제공격 정책 추가0.
- 중단 전투는 승인된 저장 전투 상태/남은 대기시간 복원, 진입연출로 공격·스킬 재발생0. 저장 미공급은 기본값 추정 대신 의존 기록.
- 기존 전투 조정 객체가 시작 신호를 한 번 전달. 재활성화/앱복귀/중복 콜백/구세대 응답으로 재시작0. View에 실행규칙 복제0.
- 시작 직전 실패는 진행 전 상태 유지·실패한 연결만 복구, 새 START/재소비0. 준비 중 피해0·첫 공격 중복0·복원 대기시간 보존·표시 추가GET0을 실제 Player에서 검수.
- 최우선 에디터 전용 resolver를 실제 Player용 자산 공급/로딩 경로로 연결해 한 구간 한 편성 실제 전투 및 API/메모리 전후 검수. UNITY_EDITOR_ASSET_DATABASE/playerBuildAllowed=false를 플래그만 바꿔 우회하지 않는다. 기존 승인 자산/명세/hash와 실제 빌드 포함·로드·전장 참조 검증 필요. Server 기존 지원 범위/원 START/시점·저장 공급, Unity 런타임 resolver/composition/시작·복원·해제 소유. 공개 미지원 구간 개방/배포/새 미디어 제작 제외.
- 기존 DATA-LIFECYCLE AND/후속키 유지, 확인된 서버 부분 근거 data-battle-entry-assets-server-result-20261022는 Hono in-process/SQL 합성 시험이며 실제 Player 검수 대체 불가. 비교 조건·API 정상/복구·상주/peak/할당/반복 잔류·리소스 메모리 구분. 전체 완료 미확인.


<a id="data-battle-exit-release-approved-20261022"></a>
## 전투 종료·자원 해제 검수 승인 — 2026-10-22
계약 data.battle-exit-release.v1.20261022. 직전 검수안 승인 및 Server_5/Unity_13 실제 전투 연결 계속 지시.
- 기존 종료 판정으로 신규 공격/탐색 중지, 진행 중 피해는 기존 승인 종료 규칙 적용. 새 승패/보상/지연피해 규칙을 만들지 않는다.
- 확정 결과 표시 데이터를 전달한 뒤 전장 연결 해제. 결과 View가 전투 객체 전체를 유지하지 않는다. 전투 전용 객체/구독/타이머/로딩 참조 반환, 로비·결과 등 다른 소비자가 쓰는 공유 자산 보존.
- 종료 전장의 늦은 콜백으로 탄 생성/HP/화면 갱신 금지. 필요한 저장·보상 복구는 기존 소유 객체가 이어간다. 새 전장/계정/run 세대 격리, 공유자산 소유권/lease 계약 준수.
- 같은 구간/편성 3회 진입→전투→종료 반복. 첫 로딩과 이후 재사용을 구분, 매회 진입 전/전투 중 최대/종료 후 메모리·객체·텍스처·API 측정. 원인검증 없이 초기값 미복귀=누수 또는 API 감소=메모리 감소 주장 금지. 계측불가 값은 0으로 보고하지 않는다.
- 재진입 시 이전 탄/대상/이벤트 구독 잔류 없음, 입력/공격1회. 정상/필수복구 API·논리 요청/재시도·상주/peak/할당/잔류 구분. 실제 종료·보상 정책을 지원하지 않는 시험경로는 해당 미검수 경계를 명시한다.
최우선: Unity#unity13-player-asset-binding-result-20261022 실제 artifact QA/OperatorInformation-20261019/BattleAssets424/manifest.json (SHA256 59e07dcc9cf4c2dc0c41b45871fa36740b7996e4a7f26e7247030fea4dbe4e7c)을 원천으로 Player 자산을 지원 구간/편성/확정 START snapshot·버전에 결속하는 실제 resolver/consumer 구현. Server는 지원 계약/명세 버전·호환/권한 결속, Unity는 실제 소비/전장 구성/공격·종료/Player 검수. artifact admissionGranted=false와 기존 Editor-only/public BLOCKED를 플래그만 바꿔 우회0. 원START 불변/원키·원hash 복구 유지, 새계약 필요시 명시 버전과 기존run 호환을 보존한다. 이미 제공된 정보 재사용, 새 GET 필요 추정0. 단위 자산 lease5회 검증은 실제전투/종료unload 증거 아님. 미디어 제작·운영 배포 제외, 기존 DATA-LIFECYCLE AND/후속키 유지.

<a id="ce03-player-asset-binding-server-v1-20261022"></a>
### CE03 로컬 Player 자산 START 결속 공급 v1 — 2026-10-22
DATA-BATTLE-EXIT-RELEASE Server 구현. 공개 START/B 지원 개방 아님. trusted createCe03LocalHost 옵션 playerAssetManifestJson으로만 공급, HTTP 요청필드 추가/GET 신설0. 원 artifact BattleAssets424/manifest.json exact UTF8 SHA256 59e07dcc9cf4c2dc0c41b45871fa36740b7996e4a7f26e7247030fea4dbe4e7c 고정. 변경 빌드는 새 source/version 검토 필요; caller hash를 신뢰하지 않음. 현재 옵션은 LEGACY_R4 기반 profile + 새Player표현이며 APPROVED_VIEW_V1/railJourney와 동시사용 거절 ce03_player_asset_mode_conflict. 기존 Editor 모드/과거run 자동업그레이드0.
/__local/ce03/prepare의 기존 준비정보에 playerAssetBinding 추가, prepareSha256에 포함. binding={version:'ce03.player-asset-binding.v1.20261022',manifestVersion:'combat-player-assets-v1',manifestSha256,unityVersion:'6000.3.22f1',configurationPath,formation:[op_kyle,op_reina,op_eve,op_sera,op_mira],admissionGranted:false}. 원formation slot순서 일치 필수. false는 원자산 artifact 자체가 입장권한을 부여하지 않는다는 뜻이며 보존. raw 전체manifest는 응답에 중복첨부하지 않음: Unity build binding에 포함된 원문+hash 사용. mismatch/순서불일치409 ce03_player_asset_manifest_mismatch/ce03_player_asset_formation_mismatch. prepare ready=false/status MISSING_EXECUTION_EVIDENCE 유지.
START 새 evidence: ce03-consumer-runner-report-v6, 기존 FULL_LOCAL_CONSUMER 및 draft/simulation/algorithm/vectors/checkpoint/command hashes에 playerAssetManifestSha256+prepareSha256 필수. 해당명령정책의 기존 전체 coverage에 PLAYER_ASSET_BINDINGS,PLAYER_FIRST_FRAME,PLAYER_EXIT_RELEASE 추가 필수. 각 case ID/count/PASS/no skip/hash 검증 후 기존 trusted ConsumerAttestationVerifier.verify 호출. verifier 요청에 playerAssetManifestJson/sha 및 prepareSha256 포함; 원코드/벡터/체크포인트·실Player 실행·첫화면/종료해제를 인증해야 함. generic true/default verifier 공급0. 기존v2~v5/틀린manifest/prepare/누락coverage로 Player 승인0. shape/hash와 합성보고서 자체는 실제실행 증거 아님.
검증을 통과한 새로컬 START combat.schemaVersion='ce03-accepted-player-combat-v1'; profile은 기존 지원 명령 버전 유지. contentManifest={schemaVersion:'ce03-player-content-v1',version:'ce03.player-content.v1.20261022',profileSha256,resolver:{mode:'UNITY_SERIALIZED_PLAYER_BINDING'},playerAssetBinding}, contentSha256=canonical hash. snapshot.version='ce03-player-snapshot-v1', 기존account/run/preview/prepare/profile/content/seed/ownedSelection/carriageOwnership/commandContract/attestation+playerAssetBinding, snapshotSha256 유지. 새resolver는 단순 playerBuildAllowed=true 전환이 아니며 정확 빌드binding과 실행evidence를 요구. 기존 원receipt/replay는 옵션·manifest·verifier가 사라져도 원바이트 반환, 변경body/원키충돌 기존거절. publicBLOCKED/실운영·배포 없음.
Server 최종56PASS0skip(합성 verifier/Hono), game-api type/lint 통과. 실제 Player consumer·v6 runner 검증자 등록/한구간한편성 전투3회·종료해제/API메모리 수용 미완료. 근거 Server#data-battle-player-binding-server-result-20261022.

<a id="data-player-asset-mismatch-approved-20261022"></a>
## 확정 출격·Player 자산 불일치 복구 승인 — 2026-10-22
data.player-asset-mismatch.v1.20261022. 사용자 직전 상세안 승인 및 실제 연결 계속 지시.
- 누락/손상은 해당 자산 준비만 재시도. 출격 재확정/연료 재소비0. 자산 버전 불일치에는 확정 출격 요구 버전 또는 명시적으로 검증된 호환 자산만 허용, 임의 최신 대체0.
- 앱이 요구 버전을 지원하지 않으면 원작전을 보존하고 업데이트 필요 안내, 같은 조건 자동 반복0. 현재 지원 경로에 다운로드가 없으면 다운로드 기능을 있다고 표시하지 않는다.
- 요원/전장/역할 연결 불일치는 전투 시작 차단. 기본 전장/다른 요원 자동대체0. 재시도 중 계정/run 변경은 구결과 격리 및 불필요 참조 반환.
- 사용자 안내는 전투 자료 준비 실패/업데이트 필요처럼 해결 행동 중심. 파일명/hash/내부 오류는 DEV 진단만. 누락/버전불일치 재현→공격0/중복출격·소비0/원작전보존/정상 준비후 이어가기 검수. 충분한 기존출격정보로 판정 가능하면 추가GET0.
- 최신 실행 원천 49#ce03-player-asset-binding-server-v1-20261022: prepare.playerAssetBinding/prepareSha256, runner-v6 및 PLAYER_ASSET_BINDINGS/PLAYER_FIRST_FRAME/PLAYER_EXIT_RELEASE 실증거, accepted-player-combat/content/snapshot을 Unity 실제 consumer에 연결. 기존 local 지원범위/LEGACY_R4 모드제약/편성순서/원START 불변/검증자 경계 보존. generic true verifier나 합성보고서로 실제Player 검수 대체0.
- Server_5 계약 소비 경계 지원/실행 검증자 연동·부족 구현, Unity_13 실제 빌드명세 검증/runner/consumer/전장 연결·실행 및 해제 소유. 한구간 한편성 전투 종료3회 반복 API·메모리 전후까지 계속. 소스/빌드·계측조건 차이와 합성/실제 범위 명시, 전체완료 미확인. 기존 DATA-LIFECYCLE AND/후속키 유지. 미디어/배포 제외.


<a id="data-player-three-cycle-approved-20261022"></a>
## Player 3회 반복 비교 조건·완료 판정 승인 — 2026-10-22
계약 data.player-three-cycle.v1.20261022. 직전 상세안 사용자 승인.
- 같은 빌드/구간/편성/전투조건/배속/측정시점 사용. 1회 새Player 실행 최초로딩→전투→종료 기준, 2회 동일프로세스 재진입 자산재사용/객체·구독잔류/재조회, 3회 같은조건 반복 증가·중복입력/공격 검수. 각 정상 새출격과 원요청 복구재시도 구분.
- API는 준비/확정/전투/종료/보상 단계별 정상·필수복구·중복조회 및 논리요청/재시도 분리. 메모리는 최초로딩/전투최대/종료후잔류·상주/할당/텍스처/객체 구분. 강제GC와 자연측정 혼합0, 계측불가를0으로 표시0.
- 응답유실/자산불일치/새프로세스 복원은 정상3회와 별도 기록. 필요한 저장·재접속 복구 및 원키·원snapshot·서버권한 유지.
- 완료는 실제 기본공격/명중/종료/해제 연결, 회차별 상태정확/중복소비·지급없음 및 검수근거가 필요. 미지원 단계는 미검수 유지. 합성 verifier/자산lease probe/파일존재만으로 전체완료0.
- 변경 전 동일경로 실행불가면 전후절감률 산출0, 최초 연결 기준값으로 기록. 측정치와 추정/미확인 구분. 최초로딩과 warm 재사용 차이를 코드개선 효과로 오인0.
Server_5/Unity_13 최우선 49#ce03-player-asset-binding-server-v1-20261022 새계약 실제 연결→한구간한편성 전투종료3회 및 API/메모리 검수 계속. Server 실제검증자/지원계약·상태/소비 근거, Unity 실제runner/consumer/전장·시작·종료/계측 담당. 원START·hash·편성순서/모드제약 및 기존정보 재사용 유지. 이미 승인된 구현을 새기획 대기로 중단0. DATA-LIFECYCLE 기존 AND/후속키 유지, 미디어/배포 제외.

<a id="ce03-player-executed-provider-v1-20261022"></a>
### Player runner-v6 실제 프로세스 검증자 연결 — 2026-10-22
Server scripts/local/ce03-executed-runner.ts executeCe03ConsumerRunner 확장. 기존 v2~v5 보존. 새 options.prepare.playerAssetBinding(원prepare값), options.player={manifestJson:BattleAssets424 exact원문,buildRoot:실제Player빌드폴더,buildManifestPath:sourceRoot상대경로}를 함께 공급. reviewedSourceBundleSha256 필수, buildManifestPath가 sourceFiles의 검토된 원소스bundle에 포함되어야 함. 기존 presentationManifest와 혼합0. 실제 entry executable은 buildRoot 안의 manifest 파일이어야 하며 출력은 buildRoot 밖. 호출자 buildArgs에 paths.playerAssetManifestPath 추가 공급; 이 경로는 exact원문을 D output에 저장한 입력이며 기존 outputDirectory/sourceBundlePath/preparePath도 유지. 실제 Player runner의 인수/진입점은 Unity 소유이며 추측 기본값 추가0.
빌드manifest canonicalJSON={version:'ce03.player-build.v1.20261022',files:[{path:'buildRoot상대경로',sha256:'소문자hex64'}...]}. 모든 실제 파일 포함(1~8192), path traversal/절대/대소문자 alias·symlink 거절. 완전파일목록+원바이트hash 실행전·실행후·verifier.verify 때 검사. 명세 자체는 reviewed source bundle hash에 포함. 이는 신뢰된 로컬빌드 디렉터리의 검증 시점 관측이며 파일 잠금/검사 사이 일시변경 방지나 패키지서명·remote attestation 아님.
Player는 runner-report-v6.json 산출 선택. source/executable/checkpoint/vectors/report 원문hash+draft/simulation/command/prepare/playerAssetManifestJson·hash가 일치하고 source/build가 현재도 같을 때만 verifier true. options.prepare/player는 실행 시작시 snapshot하여 이후 호출자 옵션변경 영향0. 반환 verifier는 실제 프로세스 exit0/원산출물에 결속되며 generic true 구현0. 실제coverage/정책은 기존 attestLocalConsumer v6가 독립검증; 실행 프로그램의 충분한 검수항목은 검토된 runner 소스·완전 의존목록의 책임. fake Node 실행 시험을 Unity Player로 간주0.
Server 기존 serve-ce03-local-host CLI의 Editor 실행모드는 이번에 Player로 추측전환하지 않음. Unity가 실제Player executable/빌드closure/검토source 목록·hash와 buildArgs 진입점을 공급하면 위실행adapter→attestLocalConsumer(playerContext)→createCe03LocalHost(playerAssetManifestJson,artifacts,verifier) 연결. 실제 전체 공급 미확인으로 신규run입장권한 확보완료 아님. 3회 비교는 같은 executable/source bundle(내부buildmanifest 포함)/prepare 정책 조건을 기준, 정상새출격과 원키복구 분리. 정상3회 실Player/종료·메모리 측정은 Unity 잔여. 근거 Server#data-player-mismatch-server-result-20261022.

<a id="data-battle-effect-attribution-approved-20261022"></a>
## 요원별 행동·효과 귀속 확인 승인 — 2026-10-22
계약 data.battle-effect-attribution.v1.20261022. 사용자 직전 제안 승인. 기존3회검수를 막는 선행조건 추가 아님.
- 공격한 요원/드론과 피해 소유 요원 연결을 확인한다. 이브 드론의 실행 주체와 이브 피해 귀속을 구분한다. 기존 승인 귀속 정책을 재사용하며 새 피해/흡혈/지원 수치를 정의하지 않는다.
- 실제 선택 대상에 명중하는지 확인, 대상 사망/이탈은 기존 재탐색 규칙 적용. 현재 지원하는 치료/보호막은 정확한 아군/원천 요원에 적용.
- 발사/명중 효과·피해 숫자·HP 변화가 동일 전투 사건을 표시. 기존 결과 집계와 실제 적용량을 대조, 중복집계/다른 요원 대상·효과·기록 혼입0.
- 첫연결 미지원 액티브/궁극기/증강은 미지원 유지, 기본공격 통과로 승격0. 사건식별자·상세진단은 DEV만, 사용자화면에 내부데이터 노출0. 기존 사건/상태 재사용·별도복제원장/표시GET 추가0.
최우선 Server5/Unity13 실제 Unity Player executable/빌드명세/검토source·의존목록/runner 인수를 49#ce03-player-executed-provider-v1-20261022 실행 adapter→attestLocalConsumer(playerContext)→host에 연결. Unity가 실제 진입점/산출물 공급, Server가 실행계약/검증자·host 연결 소유, 동일파일 동시편집0. 49#ce03-player-asset-binding-server-v1-20261022 제약·원START/키/hash/버전 보존. 한구간한편성 실제전투종료3회/API메모리 검수 계속. 합성Node47PASS는 실제Unity검수 아님. 선행미공급만WAIT, 기존 DATA-LIFECYCLE AND/후속키 유지. 미디어/배포 제외.


<a id="data-battle-info-consistency-approved-20261022"></a>
## 전투 중 요원 정보창 상태 일치 검수 승인 — 2026-10-22
계약 data.battle-info-consistency.v1.20261022. 사용자 직전 상세안 승인.
- 요원 선택 시 같은 요원의 사거리/장비아이콘/정보 표시. 피해/회복/보호막은 전장과 같은 상태 소유자를 사용하며 창 재열기 없이 갱신.
- 선택 변경 시 이전 요원 구독 해제, 새 요원 정보 전환. 장비 상세가 이전 선택에 남지 않음. 다운/복귀 기존 규칙 반영, 정보창 열기로 행동 재시작0.
- 닫기/전투종료 시 사거리와 해당 창 구독 해제, 늦은 응답의 닫힌 창 갱신 차단. 타소비자 구독/공유자산 일괄 해제0.
- 전투시작/선택확정 때 받은 정보와 현재 전투 상태 재사용. 창열기/장비상세/요원전환 때문에 추가API0. 실제 피해/회복 중 선택전환·상세열기닫기로 대상혼입0/GET0/반복 구독·객체 누적0 검수. 미지원 효과는 미검수 명시.
최우선 확정 START→실제 Player 소비/전투 연결, 동일조건 전투종료3회 API/메모리 및 새프로세스 복구 검수. 이 정보창 검수를 해당 핵심구현의 선행차단조건으로 추가하지 않는다. Server5 기존 응답/owner·버전 공급과 실제 host/검증자 연결, Unity14 실제consumer/화면/저장·복원/계측 통합 소유. 최신 계약 ce03-player-asset-binding-server-v1-20261022 및 ce03-player-executed-provider-v1-20261022 재사용. 실제 파일 저장포트 호출/프로세스 종료/새Player 원저장 읽기/원키·run·revision 대조가 필요하며 입장전 메모리 checkpoint 시험으로 대체0. QA 파일포트와 제품 보안저장 구분. 원START·키/hash/세대격리·필수복구 유지. 기존 DATA-LIFECYCLE AND/후속키 유지, 미디어/배포 제외.

<a id="ce03-player-host-install-server-v1-20261022"></a>
### 실제 Player 실행 후 host 설치 계약 — Server 부분
scripts/local/ce03-player-host.ts executeCe03PlayerHost(hostOptions,runnerOptions)는 host의 artifacts/verifier 외부공급을 금지하고, 동일 player manifest/prepare binding·호스트모드 확인→입력snapshot→executeCe03ConsumerRunner→attestLocalConsumer(playerContext)→createCe03LocalHost 순서로 반환한다. 실패 시 새host 반환0; 호출자는 성공반환 뒤만 설치한다. 각 새START는 기존 현재owner/prepare/source/build 검증을 다시 수행한다. 새REST/GET/원장/권한우회 없음. 기존원key영수증복구는 원문보존.
실제 caller 예제 QA/CommanderS1-20260922/player-host-actual-20261022/run.mts: Unity459 공급인수/전체build/source를 사용하며 reviewed hash는460의 93fc2eeb13aab6e18e7c19af0efca5e05f65558aeb431e473600c961d5ea5d93 고정. disposable DB/Hono START·원key동일원문/연료1회 검수용이며 실제소켓/확정후Player소비/새process파일복원 증거가 아니다. 이번 실행은 원본2파일 변경으로 ce03_runner_reviewed_source_mismatch에서 spawn전 차단. 현재소스를옛build로인증하거나기대hash자동갱신0. 변경 후 새검토source/hash/buildclosure와 실행인수가 필요. attempt.json에 정확경계보존.
<a id="data-battle-info-server-contract-20261022"></a>
### 정보창 원천·지원범위 공급
49 data.battle-info-consistency.v1.20261022 소비: 확정 combat.profile.baseProfile.operators(operatorId/slot/stats.maxHp/atk/armorBps/attackMs/reach)와 combat.snapshot.ownedSelection·accountId/runId/profileSha256/contentSha256/snapshotSha256를 원START에서 재사용. ce03-local-producer/schema상 reach는 front/any이고 variants.equipment=OFF; 이를 숫자사거리나 실제장비보유/아이콘 완료로 추측하지 않는다. 별도 승인된 Unity 실제표현 정의의 매핑/지원이 없으면 미지원으로 표시한다. 현재 HP/보호막은 frozen maxHp에서 재초기화하지 않고 기존 battle owner에서 읽고 통지한다. TS support-effects frame actor(entityId,hp,maxHp)와 statuses(sourceId,targetId,SHIELD,expiryTick), applied 원eventJson은 기존판정 계약이며 별도 UI ledger/GET을 추가하지 않는다. 실제Unity owner/선택구독교체/닫힌창갱신0·정보열람GET0 검수는 Unity14소유, 서버코드확인으로 전체화면완료 주장0.


<a id="data-restored-selection-approved-20261022"></a>
## 복원 직후 요원 선택·정보창 복귀 승인 — 2026-10-22
data.restored-selection.v1.20261022. 직전 상세안 사용자 승인.
- 같은 실행 중 화면복귀: 같은 계정/run 및 유효한 선택요원이면 선택·정보창 유지. 앱 완전종료 뒤 복원: 전투상태만 복원, 정보창·장비상세 닫힘 시작. 표시복원을 위한 새저장/GET 추가0.
- 선택요원 편성제외 시 선택·사거리해제/타요원 자동선택0. 다운이면 정보확인 유지·다운표시, 창복귀로 공격/스킬 실행0. 작전종료/계정변경은 이전선택·창 해제하고 기존결과/로비흐름.
- 다시열기 시 복원된 전투owner·해당run 장비정보 즉시표시. 로비최신편성을 기존run에 혼입0. 같은실행/새프로세스 분리하여 잘못된요원표시/추가조회/구독중복/자동행동0 검수. 현재 지원원천 없는 숫자사거리/장비는 지원완료 추정0.
최우선 사용자지시: 변경후 검토source/전체buildclosure/실executable/검증hash 일치 공급본으로 실제host→확정START→Player전투/3회 API메모리 및 QA파일실쓰기→종료→새프로세스복구 검수 계속. Unity14는 해당검수 단위 source편집 종료·정확 인수/경로/hash를 공유인계에 기록하고 Server5는 같은 공급본 검증후 실행. 실행중 같은 공급본 수정 금지, 변경필요시 기존증거보존·새빌드/재검토. 불일치 우회/옛verifier 재사용/검증hash만 최신으로 교체0. 상태폴링/확인메시지 대신 기존 인계의 실제 공급기록 사용. 계약 ce03-player-host-install-server-v1-20261022 및 ce03-player-executed-provider-v1-20261022 재사용. 기존정보/원키/hash/version/권한·모드제약 유지. 미디어/배포 제외, DATA-LIFECYCLE 기존 AND/후속키 유지.

<a id="data-restored-selection-server-contract-20261022"></a>
Server 복원 선택 계약: 정보창/선택은 UI 수명상태이며 새서버저장·GET을 추가하지 않는다. 같은계정/run의 기존원START snapshot.ownedSelection.formation.operators/operatorId와 복원된battle owner가 표시원천; 현재로비 account_formation을 진행run에 혼입하지 않는다. '요원제외'는 해당run의 유효선택집합 기준으로판정하며 다른시점로비편성만으로 원run편성 교체0. 새process창닫힘/같은실행유효선택유지/다운정보유지는 Unity 수명계약. 서버원receipt는 인증계정+원idempotency key+요청hash로찾아 run/snapshot원문검증후복구하며 충분한보존정보의 표시때마다 start-result호출하지 않는다. start-result는 원응답유실 등 필요한복구에만 유지. Player/기존호환스키마 그대로, 신규endpoint/원장0.


<a id="data-restored-first-hit-approved-20261022"></a>
## 復원 후 첫 명중 전투 연속성 검수 승인 — 2026-10-22
data.restored-first-hit.v1.20261022. 사용자 직전 제안 승인.
- 공격대기 중 중단: 저장된 남은 대기 유지, 앱종료 wallclock을 전투시간에 임의가산0. 발사후 명중전: 기존 투사체 복원정책 적용, 동일공격 새발사0.
- 피해직후: 이미 적용한 피해/흡혈/회복 재적용0. 대상사망직후: 부활/중복처치0·기존재탐색. 종료직후: 종료상태복귀·공격/보상중복0.
- 동일 전투조건의 연속실행과 저장후 새process복원 실행 비교. 이펙트 재생과 실제HP/보호막/귀속/처치결과 구분. 미지원 투사체/효과 미검수 유지, 신규규칙/저장필드 추측추가0.
최신 사용자 우선: Unity 출발UI→기존Controller/원journal→실통신→확정START→실제전투 연결 및 실제응답유실 복구. Server5는 서버commit후 응답유실 등 실제검수 경계/원key 멱등·권한·원상태 증거, Unity14는 제품입력/통신·저장 수명 및 Player 복구 소비. 요청미도달/서버확정후응답유실/클라저장실패 구분, 원key/body/version 보존·중복run/연료/보상0·기존1회자동재시도 규칙 유지. QA Node송신을 Unity UI 실송신으로 승격0. 통신 없는 수동원키replay만으로 유실검수 완료0.
Player465는 실제확정START/전투3회·파일새process복구 선행근거로 재사용하되 Node journal/QA plaintext/offscreen 한계 보존. managed after116596736/123940864/152002560 증가 원인을 보유참조·GC시점·누적할당·Development/JSON/checkpoint/계측부하로 구분 조사, 근거기반 작은수정 후 동일조건비교. 객체수일정만으로 누수없음0/강제GC결과와자연측정혼합0. 변경후source/build/hash 재검증 필요. 기존 DATA-LIFECYCLE AND/후속키 유지, 미디어/배포 제외.

<a id="data-restored-first-hit-server-supply-20261022"></a>
### START 실제 응답유실 Server 경계 검수
Server ce03-local-host.test.ts 실제loopback TCP 두case: (1) bridge가body수신후출격handler호출전 socket종료→run/fuel0, (2) 실제host START commit/원response200생성후 HTTP header/body전달전 socket종료→run/fuel1. Node httpRequest의 ECONNRESET 관측후 원key/body 그대로 정확1회재송신, 암묵자동재시도없음. 양case 최종run1/fuel RUN_START1/reward_transactions0, verifier1회, 요청총2와원bytes동일 확인. 후자는 원commit응답byte동일. 실제수신Authorization을기존host로전달, fixture인증헤더 서버합성0. 시험TCP bridge/Node클라+합성Player attestation이며 Unity UI/실Player 소비의 유실검수 대체0. 요청미도달 case는 출격handler미도달을 뜻하며 OS가TCP요청자체를수신하지않은시험은아님. 클라원journal 저장실패는별도Unity 경계, 서버가미확정으로되돌아간것으로해석0.
QA/CommanderS1-20260922/player-start-socket-loss-20261022.json 2PASS0FAIL26SKIP(targeted), lint/diff0. 신규제품API/재시도정책/원장0. 실제Player465 성공(20a3d048.../build463/원파일복원) 근거는 Unity#unity14-accepted-player-result-20261022 재사용; 이번TCP시험과결합해Unity UI완료로승격0. 기존 support-state 남은대기복원5PASS 및 effects 원이벤트복원15PASS는TS규칙증거만, 새process 첫명중/투사체/흡혈/처치 전체검수아님.


<a id="data-start-loss-ui-approved-20261022"></a>
## 출격 응답 유실 안내·입력 처리 승인 — 2026-10-22
data.start-loss-ui.v1.20261022. 사용자 직전 제안 승인.
- 요청중 출격 준비 중 표시, 연타는 동일논리요청으로 묶고 중복전송을 만들지 않는다. 응답유실/결과불명확은 출격 결과를 확인하고 있습니다 표시, 실패/연료미소비 단정0.
- 기존자동복구1회 후 불명확이면 다시 확인 제공, 원key/body/version으로 확인·새출격생성0. 명시적 다시확인과 자동재시도 횟수 구분, 화면왕복으로 자동예산 초기화0.
- 확정확인시 원run 자산준비/진입, 거절확정시 실제이유표시·준비입력복구. 불명확동안 원요청구간/편성고정, 로비복귀에도 원요청보존·충돌입력만제한.
- 확정응답후 클라저장실패는 출격실패로 표시0, 기존저장재시도/충분응답재조회0. 연타/왕복/실응답유실/저장실패 분리하여 원키/중복소비0/표시일치 확인.
최신 우선 Server5/Unity14 Unity출발UI→실통신→영속요청기록→유실복구/새프로세스복구 및 관리메모리원인검수 계속. Server 실제TCP handler전/commit후 유실2PASS는 원계약근거로 재사용하되 Node클라/합성attestation 한계 유지, UnityUI 근거로 승격0. 영속 원요청 실제읽기/쓰기·계정/run격리·원key/body/version·확정결과 반영 검수. managed증가는 참조root/GC시점/누적할당/JSON·checkpoint·계측부하 분리하여 근거기반 최소수정후 동일조건비교. 기존정보재사용·원START·보상멱등·source/build/hash 검수경계 유지. 기존 DATA-LIFECYCLE AND/후속키 유지, 미디어/배포 제외.

<a id="data-start-loss-ui-server-contract-20261022"></a>
### 出격 응답유실 안내 Server 계약 공급
ce03-local-host.ts 실제경로 재확인: start-result는 인증account+startIdempotencyKey+원body canonical hash로 원receipt를찾고, 원run owner/snapshot/response hash 검증후 COMMITTED와startResponse를 공급한다. NOT_FOUND/null이어도 newAttemptAuthorized=false이며 거절확정/연료미소비/새key허용이아니다. 원요청진행중·응답유실을조회부재만으로실패로바꾸지않는다. 실제확정startStatus STARTED 원응답을이미소유하면 UI표시/저장재시도를위해추가조회하지않는다.
오류표시에서 accepted:false 또는 HTTP409만으로거절확정을판정하지않는다: ce03_snapshot_unavailable는원receipt/run/hash불일치 또는commit후원snapshot조회실패에도발생, idempotency_conflict는같은key다른body/기존원요청과충돌이므로원확정취소증거가아니다. ce03_local_unavailable503/인증만료/통신단절도원출격실패로단정0. 실제거절원인과유효원요청상태를분리하고 미확정·보존된확정·권한복구를현재Controller/원journal에서관리한다. 이미확정된응답후클라저장실패는서버재출격실패가아니며원응답저장복구, 보존원응답소실시만기존원key복구.
자동1회예산·연타합침·로비왕복·원구간편성고정은원논리요청을소유한Unity Controller/journal책임이며 서버에화면별새예산/원장/저장필드추가0. 기존호스트는새START에권한/원owner검증·원receipt멱등을유지한다. 실제TCP handler전/commit후검수2PASS 및 Player465선행근거재사용, UnityUI실송신/저장복구검증으로승격0.


<a id="data-start-journal-save-approved-20261022"></a>
## 出격 요청 기록 저장 실패 처리 승인 — 2026-10-22
data.start-journal-save.v1.20261022. 사용자 직전 제안 승인.
- 최초 원요청 기록 저장 실패 시 START 전송0, 출격 준비 정보를 저장하지 못했습니다 안내. 같은 key/body/version으로 저장만 재시도, 저장성공 전 전송0.
- 전송전 취소는 전송미시작이 확인된 경우만 준비복귀. 송신시작과 취소 판정의 경합을 기존 명령조정자가 직렬화, 이미송신가능성이 있으면 미전송취소로 승격0.
- 전송후 기록갱신실패는 결과불명확 유지, 새요청/미출격단정0. 확정응답 보유후 저장실패는 원확정 유지·저장만재시도/추가GET·START0. 응답소실은 기존 원key/body/hash 복구계약.
- 저장owner가 처리, View는 상태/재시도 입력. 재실행은 영속 원요청을 기존복구로 처리. 전송전실패 HTTP0/전송후실패 원요청보존·중복소비0/반복요청응답 복사본 무한보관0 검수.
- Server 공급 data-start-loss-ui-server-contract-20261022 유지: NOT_FOUND/null/accepted:false/409 자체는 미출격확정·새요청허용 아님. snapshot_unavailable는 commit후에도 가능, keyconflict는 원요청취소 아님. 실제확정거절 근거 없는 입력복구/새START 금지.
최우선 Server5/Unity14 Unity출발UI 실제HTTP·영속journal/실응답유실/저장실패/새process복구 및 managed증가 참조root/GC/할당·JSON/checkpoint/계측분리 조사→최소수정·동일조건 검수. 기존TCP2PASS/Player465 증거 범위재사용·Node/QA평문/offscreen한계보존. source/build/hash 공급경계·원키/version/서버권한·기존정보재사용 유지. 기존 DATA-LIFECYCLE AND/후속키, 미디어/배포제외.


<a id="data-start-journal-retention-approved-20261022"></a>
## 완료된 출격 요청 기록 정리·보관 범위 승인 — 2026-10-22
data.start-journal-retention.v1.20261022. 사용자 직전 상세안 승인.
- 전송/결과불명확은 원key/body/version 보존, 화면닫기로삭제0. 확정후 작전저장미완료는 확정응답/원요청 복구정보 유지.
- 같은계정/run/요청의 작전저장완료 확인 후 처리대기목록 제외. 대기제외와 복구원본삭제 분리. 정리중 앱종료가 확정출격 재실행을 유발하지 않음.
- 진행중 기존복구정책에 필요한 원출격정보 보존, 화면별 전체응답복사본/임시파싱객체는 사용후참조해제. 필요한 불변snapshot까지 제거0.
- 종료후 기존영수증보관정책 적용, 임의새TTL/서버기록·역사자료삭제0. 저장완료전후 종료/새process 복구·반복출격 후 대기누적/전체응답root 확인. 필요한복구원본 삭제를 메모리개선으로 취급0.
Server5 서버 원영수증/멱등/복구 계약 검토·실제부족수정, Unity14 기존 저장owner의 durable완료→대기정리→수명관리/실UI 검수. 최우선 Unity출발UI 실제HTTP·영속journal/응답유실·저장실패/새process복구, managed증가 root/GC/누적할당/JSON·checkpoint·계측분리 조사→근거기반최소수정→동일조건비교 계속. 기존TCP/Player465 증거 범위재사용, 합성/Node/QA평문/offscreen한계보존. 원key/hash/version·계정run세대·기존정보재사용 및 source/build/hash검수경계 유지. DATA-LIFECYCLE 기존 AND/후속키 유지, 미디어/배포제외.

<a id="data-start-journal-retention-server-contract-20261022"></a>
Server 원receipt는account+originalKey+requestHash/run/responseHash의기존불변원본. ce03_start_receipts UPDATE/DELETE trigger거절은현재LOCAL계약이며 UI대기제외를서버삭제로연결하지않는다. 동일계정/run/원요청의durable완료판정·대기제외는기존Unity저장owner소유, 서버에완료알림/TTL/화면정리API추가0. 종료/claim후에도원START원문은보존되고 기존원key재생이새run/연료/보상을생성하지않음. 과거startStatus STARTED원문보존은현재작전재시작허가아님; 현재lifecycle의RETURN_TO_LOBBY/CLAIMED 등확정상태와구분, 충분한현재상태보유시재조회0·재접속상태부재시필수기존복구유지. QA completed-start-retention-server-20261022.json targeted1PASS28SKIP는합성LOCAL host/SQL경계이며Unity정리중종료/영속journal새process/메모리보유root검수아님.


<a id="data-start-reentry-dedup-approved-20261022"></a>
## 작전 저장·대기 정리 사이 중복 진입 방지 승인 — 2026-10-22
data.start-reentry-dedup.v1.20261022. 직전 제안 사용자 승인.
- 작전저장과 대기요청 동시존재: 계정/run/원key·버전 대조후 동일요청이면 저장작전에 연결하고 대기만정리. 대기만있으면 기존원요청복구·새출격0, 작전만있으면 기존이어가기·대기재생성0.
- 계정/run/version 충돌은 임의병합/삭제0, 필요한범위만 기존복구계약 확인. 과거STARTED 영수증은 현재종료run 재입장허가가 아님.
- 복구와 로비이어가기 동시실행도 전장진입1회. 늦은복구로 기존전장 재생성0. 저장직후/정리직전/정리직후 종료·재실행에서 원run유지/추가소비0/전장객체·입력구독중복0 검수. 유효저장 충분시 추가GET0.
최신 사용자 직접수정지시: Unity14 인계 unity14-start-retention-scope-20261022 / unity14-final488-successor-boundary-20261022의 receipt저장→pending해제→checkpoint저장 사이 durable대기key 공백, journal전체재파싱·receipts메모리누적을 실제 수정하고 Player검수. Unity15 기존 journal/confirmed receipt/checkpoint owner 수명과 durable인계를 결속, Server5 원영수증/현재lifecycle/멱등계약 지원·실부족수정. 필요복구원본 보존·전송전실패HTTP0/확정저장재시도HTTP0 유지, 임의TTL/서버기록삭제0. 메모리 원인·동일조건 개선 측정, 이전 자연managed 미감소/QA평문/offscreen 한계 유지. 478/488 증거는 변경영향 없는범위 재사용·변경source/build/hash 새검수. 기존 AND/후속키·미디어/배포제외.

<a id="data-start-reentry-server-contract-20261022"></a>
Server 재진입계약: combat-readiness.ts의 /v1/runs/:runId/lifecycle은인증account+run으로현재 runs/final_results/reward원천을읽는다. version=run-lifecycle-v1, nextAction은현재종료/지급이면RETURN_TO_LOBBY, 실제ACTIVE이고종료/보상없을때RECOVER_COMBAT_CONSUMER, FINISH_PENDING이면RESUME_FINISH, 판단불가/격리면REVIEW_REQUIRED. 원START snapshot/startResponse는불변과거원본이며현재lifecycle을덮지않는다. 모든읽기에grantsOnRead=false/newAttemptAuthorized=false, 새run/입장권한발급없음. 이미유효한현재owner상태보유시GET추가0; 새process/결과불명확으로실제무효화된범위만기존계약복구. Server는Unity checkpoint durable완료여부를알지못하므로 receipt존재를checkpoint완료나대기삭제허가로간주0. 새서버API/완료표시/TTL추가0.


<a id="data-start-recovery-acceptance-approved-20261022"></a>
## 출격 저장·복구 통합 완료 판정 승인 — 2026-10-22
data.start-recovery-acceptance.v1.20261022. 사용자 직전 통합판정안 승인. 기존 승인 검수 통합이며 중복시험체계/미충족후속 선등록 아님.
- 요청저장→전송: 저장실패 전송0/원요청유지. 서버확정→응답: 실유실복구 후 run/연료소비1회. 영수증→checkpoint durable저장: 중간종료/새process 원run복원. 저장완료→대기정리: 복구/이어가기 경합에도 전장·입력연결1회. 반복출격→종료: 완료대기/불필요receipt참조 무한누적0.
- 기능정확성과 메모리개선 별도판정. 복구통과·메모리원인미해결이면 해당항목미완료. 메모리감소·복구유실이면실패. 변경영향없는기존검수재사용/이번저장·정리·복원경계 영향시험만추가.
- 사용자 결과는 연결완료/복구완료/메모리확인/남은제한 분리, QA Node/평문/offscreen/미지원범위 명시. 실제 수행 근거 없이 전체완료승격0.
Server5/Unity15 저장공백·전체journal재파싱·receipts메모리누적 수정→실제Player 검수 계속. Unity15 journal/receipt/checkpoint owner 인계와 durable수명, Server5 원영수증/현재lifecycle/멱등계약·필요한부족구현만 소유. 서버receipt존재를 클라durable완료/대기삭제허가로 사용0. 원key/hash/version/계정run격리·기존정보재사용/충분확정저장재시도HTTP0 유지. 기존 TCP/478/488 등 유효범위 재사용·새source/build/hash 증거 확보. 기존 DATA-LIFECYCLE AND/후속키 유지, 미디어/배포제외.


<a id="data-recovery-record-isolation-approved-20261022"></a>
## 여러 계정·작전 저장 기록 복구 대상 격리 승인 — 2026-10-22
data.recovery-record-isolation.v1.20261022. 사용자 직전 제안 승인.
- 같은계정 과거종료run과 현재run 구분, 과거영수증으로 재전투0. 타계정 미처리기록은 현재복구에 사용0·기존보관정책격리. 복구중계정변경은 구결과표시/상태반영차단·필요원기록보존.
- 같은run 중복은 원key/version 동일성검증후1회처리. 소유계정불명/충돌은 임의병합/삭제/새출격0·해당범위보류. 현재복구에 필요한 기록만읽고 타계정/과거전체영수증 메모리상주0, 필요원본 기존저장소보존.
- 계정전환·앱재실행 조합에서 타계정표시/잘못된run진입/중복소비0·이전계정객체참조해제 검수.
최신 사용자실행지시: Server5/Unity15 제품 보안저장과 실제로비 연결 계속. 기존제품저장/인증/키수명 계약을 확인하고 QA평문포트와 분리해 기존확장점에 연결, 임의키/암호방식/평문fallback0. 실제로비 View→Controller→영속요청/실통신→전투/복구·종료 검수. 미공급 플랫폼키/권한 경계는 정확기록·가능한독립구현진행, 로컬제품경로 검수를 운영배포/실경제행동 승인으로 확대0.
Unity15#unity15-start-recovery-result-20261022/Player507은 로컬저장공백·중복진입·정리복구 선행완료 근거로재사용, 부모전체미완료. ComputeBuffer종료경고는 발생stack/생성·소유·해제/Editor·QA·제품 구분→근거있는최소수정→반복진입종료/프로세스종료 검수. managed/native/GPU/할당·잔류를 분리, 경고숨김/강제GC만으로수정완료0. offline.NET492 할당감소를 Unity자연상주절감으로 승격0. 기존원key/hash/version·계정run격리·정보재사용·source/build검증·AND/후속키 유지. 미디어/배포제외.

<a id="data-recovery-record-isolation-server-contract-20261022"></a>
Server identity 원천은검증된Bearer의accountId/sessionId와실제auth_sessions JOIN accounts. session ACTIVE/만료및account ACTIVE를매요청확인, client선택계정/파일이름으로권한대체0. 원START receipt는인증account+key+bodyhash, lifecycle은인증account+run 및LOCAL receipt소유권으로조회. 다른계정의같은key는NOT_FOUND/newAttemptAuthorized=false이며원영수증존재/응답노출0,타계정run lifecycle403. 계정전환후늦은응답/UI참조는Unity세대소유자에서격리, 서버가화면전환을자동인지한다고주장0.
49 기존1617 부근 제품인증규칙: 서버발급session secret만기기secure storage에저장가능, rawpassword/GoogleIDtoken 장기저장금지. 이것은journal/checkpoint용암호화키 공급계약이아님. auth token수명/refresh와플랫폼키수명을임의동일시하거나토큰을암호키로파생0, Server signing key배포/임의키·평문fallback0. 이번서버검수로플랫폼secure adapter/key lifecycle완료주장0; Unity15 기존제품저장확장점확인/필요플랫폼경계기록이담당범위.


<a id="data-secure-storage-failure-approved-20261022"></a>
## 보안 저장소 접근 실패 안내·복구 승인 — 2026-10-22
data.secure-storage-failure.v1.20261022. 사용자 직전 제안 승인.
- 일시접근불가는 원기록보존·저장 정보를 열지 못했습니다/재시도 제공. 최초원요청저장전 실패는 전송차단, 성공후 기존요청 진행. 확정후저장실패는 출격실패로되돌림0·원확정결과유지/저장재시도.
- 읽기/복호화실패를 빈기록으로 취급·덮어쓰기0. 손상/키접근불가 구분해 기존복구 적용. 계정변경중 구읽기완료는 새계정표시/반영0.
- 보안저장실패 평문fallback0. 무관한로비기능 유지·저장필요출격/복구만제한. 접근/읽기/쓰기실패 별도검수: 원본보존/중복출격0/재시도정상복귀/민감원문로그0.
최신 사용자우선 Server5/Unity16 실제제품로비→기존권한출격→전투/복구 연결, Android 실제Keystore·계정별파일저장/중단/새process복구 검수 및 전체전투메모리. 기존 제품원천/현재run 권위owner·run별pending 선택을 연결, publicBLOCKED/권한검증 플래그우회0. Server는 현재지원/입장·owner계약과실부족구현, Unity16 제품View/Controller/보안adapter·실기기/Player통합. 기기접근·키·환경 미공급은 정확기록하고 가능한독립구현계속, 무선포함 실제접속가능성 확인 전 Android부재단정0. 배포/실경제행동 승인으로확대0.
Unity16#unity16-account-storage-buffer-result-20261022:525테스트crypto99PASS/527buffer경고수정 검수는 해당범위재사용; 실Android/제품전체/전체메모리 대체0. managed/native/GPU·peak/할당/종료잔류를 전체전투 동일조건 측정, ComputeBuffer경고재발도확인. source/build/hash변경 새검증, 원key/hash/version/기존정보재사용·원본보존 유지. DATA-LIFECYCLE 기존 AND/후속키·미디어제외.

<a id="data-secure-storage-failure-server-contract-20261022"></a>
Server 제품연결확인: /v1/account/state는account/보유/편성원천이며현재run을공급하지않는다. 이미검증된원runId는 /v1/runs/:runId/lifecycle의인증owner/current상태복구를재사용;최신시간영수증을현재run으로임의선택0. 현재run선택과run별pending은기존클라권위owner계약으로결속하고충돌시임의병합0. public /v1/runs/start 신규입장은현재 combat_not_ready409이며 describeCombatRequirements의 enemy faction/wave/reward binding, seed, engine/effective stats와정책원본지원이충족되지않은진단계약; LOCAL검증을public지원으로승격0.
public /v1/runs/start-result는startIdempotencyKey+operationPreviewId/인증계정의기존idempotency원천, LOCAL /__local/ce03/start-result는prepareSha256/supportrevision까지원bodyhash와LOCALreceipt를사용한다. 두endpoint/스키마를주소교체만으로호환취급0. public key는공백없는ASCII1~128규칙,현재LOCAL허용key를무조건public재전송0. 둘다NOT_FOUND는새출격허용/미소비확정아님. 보안저장접근/복호화/쓰기실패는클라저장owner의구분이며서버401/409로치환0,원본보존·빈기록덮기/평문fallback0·확정응답후저장만재시도/추가HTTP0 유지.
<a id="ce03-embedded-source-capacity-20261022"></a>
실제부분부족수정: BattleBuffer527/source.json=1255files/8185550 UTF16characters/8220456bytes가기존1024files/8M문자한도를초과함을확인. scripts/local/ce03-executed-runner.ts source최대2048개, ce03-consumer-attestation.ts exported maxCe03AlgorithmSourceCharacters=16000000을runner/attestation공유. 파일당8MB·output32MB byte한도와다른artifact한도/원문동등성·hash·reviewed pin·전체buildclosure·verifier·공개입장차단유지. 한도증가는전체소스생략방지지원이며실제Player현재source인증아님,16M문자전부가32MB출력제한을통과한다는보장0. 테스트2048수용/2049거절·총18M거절·8185550문자attestation수용/16M초과거절. 실제Player새source/build/hash검수필요.


<a id="data-lobby-run-selection-approved-20261022"></a>
## 로비 현재 작전·복구 대기 기록 선택 기준 승인 — 2026-10-22
data.lobby-run-selection.v1.20261022. 사용자 직전 제안 승인.
- 현재run과 동일요청 복구기록은 이어가기1개/전장생성1회. 현재종료상태는 과거STARTED영수증보다 우선, 과거기록으로전투재개0.
- 결과불명확요청만있으면 출격확인중 표시·존재/미존재단정0. 다른run기록충돌은 최신파일순서로임의선택0·필요범위 기존복구확인. 타계정기록 제외.
- 기존현재작전상태owner 하나가 판정하고 로비카드/이어가기/복구안내가 같은결과사용. 유효정보보유시 로비열기GET0. 오래된영수증/현재run/미처리요청 혼합검수: 잘못된진입/중복표시/불필요GET0.
최신 사용자명시: 제품출격 연결에 필요한 계약부터 Server5/Unity16이 맞추고 실제로비/Android secure저장복구/전체전투메모리까지 구현·검수. Server공급 data-secure-storage-failure-server-contract-20261022 기준 account-state currentrun미공급, knownrun 기존인증lifecycle, public/LOCAL start-result body/key 차이 및 public combat_not_ready 경계를 먼저 추적. UI상태owner 생성으로 서버미공급원천을 발명0. 필요한 최소원천/지원구간·편성/명세·버전/hash/실행검증/START·재시도·복구요청 필드/응답·오류·호환을 기존49에 확정공급; 소비자가주소만바꿔사용0. 기존권한/검증을 충족하는 제품연결을 구현하며 BLOCKED플래그만우회0. 미결게임정책/운영개방은 별도미결로 남기고 확정독립구현 진행. 기존정보재사용·명령응답완결·원key/body/version불변·계정run격리 유지.
Android실기접근 가능여부/플랫폼키 의존은 사실기록, 제품보안저장·중단/새process복구와 managed/native/GPU/peak/할당/잔류 동일조건 검수. 525/527·capacity28PASS는 해당범위만재사용. source/build/hash변경재검증, 기존 AND/후속키·미디어/운영배포제외.

<a id="product-departure-minimum-contract-20261022"></a>
## 제품 출격 최소 연결 계약 — 현재 구현/지원 경계
DATA-LOBBY-RUN-SELECTION:SERVER:20261022. 아래는 실제소스에서확정한현재계약이며새제품입장지원선언이아니다. 제품소유 Server(departure/index/combat-readiness/combat-policy-binding), Unity(인증계정/currentrun owner/run별pending·secure journal/실runtime) 경계 유지.

| 단계 | public 제품 현재계약 | LOCAL 실증계약/호환조건 |
| --- | --- | --- |
| 구간·편성 | departure-v4 preview가실계정 formation/loadout/ultimate revision·보유상태·balance/content·정책을고정. 정책catalog combat.policy-catalog.2026-09-29.1은 Kyle/Reina/Eve/Sera/Mira·ordinary risk I의REQUIRED_NOT_EXECUTABLE 요구사항 | ce03.local.qa.20260911.001은 opn_01_last_departure/I/15wave,5요원·car_combat/car_gun·train/요원level1/promotion0/skill1의고정시험범위. 최신제품전범위/실능력승인으로확대0 |
| 원명세·실행 | preview.admission의combatPolicyRequirements·combatAuthority를제품원천으로사용. policy binding은현재EXECUTION_UNAVAILABLE·requiredEffectKinds별handler_missing, requirements는BLOCKED/canStartfalse | manifest59e07dcc9cf4c2dc0c41b45871fa36740b7996e4a7f26e7247030fea4dbe4e7c, prepareHash/profileHash/contentHash/snapshotHash+reviewed source/build/executable검증. runner-v6실행/attest를제품최신정책지원으로간주0 |
| START | POST /v1/runs/start, Bearer/Idempotency-Key, body는operationPreviewId만(추가필드400). 원key재생은현재gate보다먼저원snapshot복원. 새입장은현재combat_not_ready409 | /__local/ce03/start body operationPreviewId/expectedSupportRevision/prepareSha256; ce03-accepted-player-combat-v1 응답. public으로주소만치환0 |
| 결과불명확 복구 | POST /v1/runs/start-result body startIdempotencyKey/operationPreviewId, 인증account key조회. key공백없는ASCII1~128. COMMITTED/NOT_FOUND/SNAPSHOT_UNAVAILABLE/CONFLICT/TEMPORARILY_UNAVAILABLE,runId/runStatus/startResponse. 조회성공accepted는출격성공과구분 | LOCAL start-result는추가supportRevision/prepareHash원bodyhash검증·LOCALreceipt. 원body/version/key는해당endpoint계약대로보존; 기존LOCAL공백key를제품변환/재키잉0 |
| 현재run | account-state에는currentrun없음. knownrun의 GET /v1/runs/:runId/lifecycle는인증owner/currentrunStatus/finish/reward/nextAction 공급,읽기입장·지급0. 이미유효한currentowner/확정명령결과를표시때재사용 | 원key/receipt/run별pending과기존현재owner의동일성대조. latest file/영수증시간으로currentrun발명0. 충돌은필요한knownrun만기존lifecycle로복구,계정전체dump/상시GET신설0 |
| 저장·재시도 | 최초원요청secure저장완료후전송,전송미시작확인때만취소. 같은key/body/version한논리요청·기존자동1회. 확정후저장실패는원응답보존/저장만재시도HTTP0 | 기존488/507근거는QA구성한정. 제품secure/로비연결·실Android검수가필요,평문fallback0 |
| 오류 | 400 contract_violation/401인증/403owner권한/409 quote변경·combat_not_ready·원snapshot오류/503원천조회실패를분리. accepted:false/NOT_FOUND/409만으로미소비·새key허가0 | source/build/prepare/policy불일치는원증거보존후지원원본재공급·새검증,flag/구verifier/hash만교체우회0 |

제품입장까지남은것은대부분이미승인된정책의실행연결·증거부족이다. combat-policy-binding의SENTRY_CREATE/SENTRY_ENHANCE_OR_CREATE·회복/연속사격등최신kit요구,敵faction/wave/reward/seed·engine/effective-stats원본을지원runtime/asset/publication version/hash와결속하고실consumer/서버결과검증경계에연결해야한다. LOCAL의equipment OFF/research OFF/faction UNSUPPORTED·고정engine65/weight60/속도105:100을제품값으로채택하지않는다. 기존정책전체를새미결로되돌리지않는다. 신규미결은시험고정범위를제품지원으로별도제한개방할지/어떤미지원정책을제외할지같은정책변경이발생할때만정확기록한다. 현재그런예외승인없음.
제품공급완료수용: 실제지원원본/version/hash→권한있는preview/currentowner→원keySTART→실제consumer→동일원response복구/현재lifecycle→secure checkpoint대기해제까지기존owner로연결·검증. 이번문서는필드/호환원천공급이며public runtime구현/입장개방완료가아니다. 미디어/운영배포·실경제추가승인아님.



정정2026-10-22: 위최소계약의최신catalog를9월22일로잘못기록했다. 실제 captureCombatPolicyRequirements()는9월29일 currentCatalog이며9월22일은 readPinnedCombatPolicyRequirements의역사복구용이다. 이미확정된과거snapshot은변경하지않는다.
<a id="data-departure-support-ui-approved-20261022"></a>
## 제품 출격 지원 여부·차단 사유 표시 승인 — 2026-10-22
data.departure-support-ui.v1.20261022. 직전 제안 사용자 승인.
- 진행조건충족/실전투지원은 기존출격진행. 해금됐으나 앱전투미지원은 해금유지·출격제한/현재 버전에서는 출격할 수 없습니다 표시. 자산미준비는 전투 자료 준비 필요/기존준비 연결. 편성미충족은 실제부족조건/편성수정 연결. 결과불명확은 원요청확인우선·중복출격0.
- 해금/편성적격/자산준비/실출격지원 분리, 해금만으로진입0·자산실패로해금되돌림0. 기존유효정보표시/열람추가GET0, 서버최종권한검증유지. 내부계약/hash/오류코드 사용자노출0. 조건·버튼일치/해결경로 검수.
최신 우선 Server5/Unity16 현재작전선택 및 최신승인전투정책 제품연결 계속. 49#product-departure-minimum-contract-20261022 실제 public요청/복구·정책원천 계약 적용, PlayerEntryRuntime 최신시간영수증선택을 권위owner/run별선택·종료우선·충돌보존으로 수정. combat.policy-catalog.2026-09-22.1 최신kit/handler·faction/wave/reward/seed/engine/effective-stats 실제원천을 제품runtime/결과검증에 결속, LOCAL시험수치전용·publicflag우회0. 기존승인정책을새미승인으로되돌림0, 실제미공급만WAIT.
Android는 연결되면 실검수. 현재접속미확인을 전체장치부재로단정0·반복연결질문/자동폴링으로독립작업중단0. 기존535테스트/536빌드는 실행증거아님. 실행가능Player 제품경로와 전체전투 managed/native/GPU/peak/할당/잔류·buffer재발 검수, 제품전체미연결부분 미검수명시. 기존source/build/hash·원key/version/계정run격리·정보재사용 및 AND/후속키 유지. 미디어/운영배포제외.

<a id="data-departure-support-server-contract-20261022"></a>
製品지원표시확정공급/이전오류정정: captureCombatPolicyRequirements는현재combat.policy-catalog.2026-09-29.1(숫자DEF·shield/equipment timing포함),9월22일catalog는역사복구용. 49#product-departure-minimum-contract-20261022의잘못된최신버전표기를수정했다. 과거run의pinned9월22일snapshot은보존한다. 승인요청본문에9월22일이인용됐어도현재원천을과거버전으로되돌리지않는다.
기존preview.combatContract.policyBinding의policyVersion/status/canExecute/operators/requiredEffectKinds와blockingReasons를재사용한다. 유효5요원편성도EXECUTION_UNAVAILABLE/handler_missing가가능하므로편성미충족·해금취소와동일시0. 지원부재는현재버전출격미지원표시,실제편성오류는기존편성수정,클라asset준비실패는기존asset준비로분리. 앱의asset상태나해금이canStart를승격하지않으며내부hash/code는View사용자문구에그대로노출0. 이미받은preview/currentowner를열람때재사용·새GET0. 현재B CombatRuntimeRegistry는실제BCombatExecutableAdapter.prepare의owner검사+동일transaction fence를요구하며등록기본adapters=[]; catalog/지원bool/클라주장만으로등록·권한증명불가. 최신policy실consumer와원faction/wave/reward/seed/engine/effective-stats실행검증 결속은계속미완료이며새승인대기로치환0.


<a id="data-departure-multi-block-approved-20261023"></a>
## 복수 출격 차단 사유 표시·갱신 승인 — 2026-10-23
data.departure-multi-block.v1.20261023. 직전 상세안 승인.
대표안내1개+전체사유목록 표시. 대표순서: 출격결과확인중→인증/저장문제→앱버전미지원→편성/진행조건→자산준비. 한사유해소로 다른사유남은출격 활성화0. 편성/자산 등 상태통지로 갱신·추가API0, 사유별기존해결동작 연결. 클릭시현재조건 재검사·서버최종권한검증 유지. 복수사유부분해소/역순·늦은응답/계정run전환 시험: 잘못된활성화/깜박임/중복요청0.
Server5/Unity17 최신 실제계약버전 맞춤·제품전투연결 계속 우선. Server 정정: captureCombatPolicyRequirements 현재9/29.1, 9/22는역사복구용. 이전기획의9/22최신표기를 현행구현근거로 사용0. product-departure-minimum-contract-20261022/data-departure-support-server-contract-20261022 원천 및 현재policy/requirements/admission 실필드 대조·소비. Unity17 최신v3/v4 reader부분완료는실policy실행아님. 실제kit/handler/원stats·wave/reward/seed/engine·결과검증 결속 구현, publicflag/LOCAL수치우회0. Android연결시검수·독립구현계속/반복질문폴링0. 전체전투managed/native/GPU·peak/할당/잔류 검수·버퍼재발확인. 기존정보재사용/원key/version/owner·source-build검증·AND후속키/미디어배포제외 유지.


<a id="data-departure-unblock-input-approved-20261023"></a>
## 차단 해소 후 출격 입력 처리 승인 — 2026-10-23
data.departure-unblock-input.v1.20261023. 직전 제안 사용자 승인.
- 모든차단 해소시 버튼활성화만, 이전입력 예약실행/자동출격0. 사용자의 새클릭을 현재구간/편성/조건의 출격의사로 처리(별도확인팝업 신설 아님).
- 이미송신된요청은 새요청생성0·원요청확인/복구. 편성수정복귀·늦은클릭은 화면/선택세대검사, 표시갱신이 명령을 발생시키지 않음. 활성화 전후 버튼위치 유지.
- 자산완료순간연타/편성변경복귀/늦은응답/원요청확인중조건해소 검수: 자동출격0·중복소비0·사용자확인조건1회실행. 기존Controller 명령/owner 및원키복구 재사용, View업무중복0.
Server5/Unity17 최신실계약/현행policy9/29.1·requirements/admission 대조→실제품runtime/결과검증 연결 계속우선. 9/22역사복구보존/LOCAL수치전용·publicflag우회0. Android연결시실검수·독립구현계속/자동폴링0, 전체전투managed/native/GPU·peak/할당/잔류·buffer재발검수. 기존정보재사용·원key/hash/version/계정run세대·source/build검증경계·AND후속키/미디어운영배포제외 유지.

<a id="data-departure-unblock-server-contract-20261023"></a>
## 공개 START 현재 조건 재검사 연결 — 2026-10-23
DATA-DEPARTURE-UNBLOCK-INPUT Server 부분. 원키 확정 replay 우선 유지. 신규 비B 공개 START는 기존 departure state/admission 검증을 공통 사용: 만료/비ACTIVE operation_preview_not_startable, 현재 연료부족 insufficient_fuel, 편성/loadout/정책 등 원견적불일치 departure_quote_changed 및 기존 owner 오류. 실행지원 부족 combat_not_ready는 계속 유지. HTTP body/requirements v3/admission v4 변경0, 버튼활성화가 명령을 생성하지 않으며 표시용GET 추가0. 읽기재검사는 실제 START 원자성/권한/런타임 증명을 대체하지 않음. Server handoff #data-departure-unblock-server-result-20261023의 44PASS 분리근거 참조. 제품runtime·Unity 입력/실Player·Android/전체메모리 완료 아님.


<a id="data-departure-summary-approved-20261023"></a>
## 출격 준비 요약·확정 편성 일치 확인 승인 — 2026-10-23
data.departure-summary.v1.20261023. 직전 제안 사용자 승인.
준비화면 현재구간/요원/객차/비용을 기존정보로 표시. 클릭시 확인한선택·버전을 기존원요청/preview에 연결. 서버조건변경거절은 변경항목안내·준비갱신/자동재출격0. 확정후 전투/정보창은 해당run 확정편성·장비 사용, 로비변경 임의혼입0. 새확인팝업신설0·충분확정응답후편성/장비재조회0. 편성수정직후출격/늦은준비응답/서버조건변경/확정후로비변경 시험으로 표시→원요청→확정run 일치 확인.
Server5/Unity17 최신실계약 맞춤·제품전투연결 우선. data-departure-unblock-server-contract-20261023 현재조건공통검사/확정원키우선재생 근거 재사용, 현행9/29.1 policy·requirements/admission 실제버전 대조. 실제kit/handler·stats/wave/reward/seed/engine/결과검증 결속 계속, public차단flag/LOCAL수치우회0. Android연결시검수·독립진행/자동폴링0, 전체전투managed/native/GPU/peak/할당/잔류 확인. 기존원key/hash/version·owner/무효화·정보재사용·source/build검증 및 AND후속키/미디어운영배포제외 유지.

<a id="data-departure-summary-server-contract-20261023"></a>
## 출격 견적 변경항목 부가응답 — 2026-10-23
공개 POST /v1/runs/start 비B 현재견적불일치409 departure_quote_changed에 departureChange={version:"departure.quote-change.v1",operationPreviewId,changedFields:string[],newAttemptAuthorized:false} 추가. 항목 FORMATION/LOADOUT/OWNED_STATE/COST/POLICY/PROGRESSION/COMMANDER/ULTIMATE/MAP/ADMISSION. 기존원키 replay는 이 검사 이전이며 오류가 원preview/확정run을 교체하거나 새실행을 승인하지 않는다. 기존 state 검증의 연료부족/권리상실 등은 기존 errorCode 유지, 목록미공급을 변경없음으로 해석하지 않는다. 기존정보로 변경안내 후 필요한 준비무효화만 수행, 자동재출격0. 원 run의 성공응답 편성/장비를 현재로비값으로 덮지 않는 기존계약 유지. 서버29PASS와 정확범위는 Server#data-departure-summary-server-result-20261023, 실제 제품전투결속 완료 아님.


<a id="data-battle-transition-display-approved-20261023"></a>
## 전장 전환 초기 정보 표시 승인 — 2026-10-23
data.battle-transition-display.v1.20261023. 직전 제안 승인, 기존 첫화면/첫공격 정책의 표시 세부안.
요원/장비/HP/보호막 연결완료전 기존준비표시 유지. 이전run/임시기본값 먼저노출후교체0. 신규는 확정초기상태, 이어가기는 복원상태로 첫표시·HP임시최대치0. 전환실패는 기존준비/복구안내·새START/재소비0. 기존전투조정자가 표시준비와 시작신호결속·보이지않는중피격0. 신규/피해상태복원/다른편성다음출격/자산실패 검수: 순간잘못표시/중복전장/표시GET0. 지원원천미공급은미검수, 임의장비/HP값0.
최신지시 Server5/Unity17 조건변경안내 및최신제품계약 연결계속. data-departure-summary-server-contract-20261023의409 departure_quote_changed/departureChange version·previewId·changedFields·newAttemptAuthorized:false 소비. 모든오류에목록이있다고가정0, 기존 state오류코드 유지. 불변preview/원key/확정run 보존·자동재출격0. 현행policy9/29.1/requirements-admission 실제version과 handler/stats/wave/reward/seed/engine/결과검증 제품결속 우선. publicflag/LOCAL수치우회0. Android연결시검수·독립진행, 전체전투managed/native/GPU/peak/할당/잔류·buffer재발 확인. 기존정보재사용/owner·source-build검증/AND후속키·미디어운영배포제외 유지.


<a id="data-departure-change-return-approved-20261023"></a>
## 출격 조건 변경 안내 후 준비 복귀 승인 — 2026-10-23
data.departure-change-return.v1.20261023. 직전 제안 사용자 승인.
실제changedFields만 출격 조건이 변경되었습니다 아래 표시. 확정새값확보시 준비화면갱신/변경부분강조, 이름만공급시 수치추정0·무효화범위만 기존갱신. 개별오류는원code안내·목록가정0. 결과불명확/원요청존재는원복구유지·새출격허용추정0. 안내닫기/갱신완료 자동출격0, 사용자의새출격과기존요청처리·서버허용계약 확인후만새요청. 비용단독/복수변경/값미공급/늦은응답 검수, 무관정보삭제/전체계정재조회0.
최신 사용자명시 Server5 일반5인 최종전투능력치·실행원천 실제구현, Unity17 제품전투소비 계속. Server#data-battle-transition-server-boundary-20261023 근거 일반combat-owned-profile effectiveStats:null/영구성장까지만, B Kyle1인producer는대체불가. 기존승인현행policy·장비/연구/증강 등 실제적용범위·계산순서/원천버전을 대조하여 응집된owner에서 최종stats와 실행kit/handler·wave/reward/seed/engine/결과검증을 단계구현·공급. 기존올바른계산재사용·새밸런스/LOCAL시험수치/미공급0채움/단순지원flag우회0. 실제미결설정은정확분리하고 확정독립부분구현, 경계조사만반복종료0. Unity는정확공급버전 소비/원START·진행checkpoint분리·실Player검수. start-state로복원HP최대초기화0.
Android연결시검수/독립진행, 전체전투managed/native/GPU/peak/할당/잔류확인. 기존원key/version·정보재사용·source/build검증·AND후속키/미디어운영배포제외 유지.

<a id="ordinary-five-stat-owner-server-contract-20261023"></a>
## 일반5인 core stats 내부 실행계약 — 2026-10-23
combat.ordinary-five-core-stats.v1.20261023, scope MAX_HP_ATK_DEF_ONLY. Server combat-ordinary-stat-owner.ts prepareOrdinaryFiveStatOrigin(db,accountId,newRunId,expectedFormationRevision/expectedLoadoutRevision)은 실제영구성장/9/29.1 원본과 originHash/assertions를 반환; caller START원본고정·동일batch검증 필요(현재미설치). OrdinaryFiveStatOwner.fromOriginal(origin,신뢰된hash).resolve(state)는 accountId/runId/revision/inventoryRevision, 5actor operatorId/currentHp/정확3slots, research/commander/augments/temporary 각 sourceId/version/revision/NO_ACTIVE_STAT_MODIFIERS가 필수. 승인된HP/ATK/DEF장비 및SET01/04만 처리, 미지원효과를0으로간주0. 원본/최신state1개 수명, 버전역행·경합거절, 현재HP변경0/보호막권위0.
combat.ordinary-five-equipment-source.v1.20261023의 readOrdinaryFiveEquipmentSource는 실런inventory/effect 원장→5인장착입력 및ACTIVE run/inventory/effect CAS 반환. 원HP/기타modifier 증명과 함께 같은batch에서 적용해야 하며 별도public API 없음. 원START와 현재HP복원 구분·원hash 신뢰경계·원키회복을 대체하지 않는다. 초기HP최대값/미공급빈장비/미지원modifier발명 금지. 29PASS 및정확잔여는 Server#ordinary-five-stat-owner-server-result-20261023. 실제START/제품runtime전체/Player/Android/메모리완료 아님.


<a id="data-stat-source-verification-approved-20261023"></a>
## 최종 전투 능력치 원천별 적용 확인 승인 — 2026-10-23
data.stat-source-verification.v1.20261023. 사용자 직전상세안 승인·중단후 계속지시. 새밸런스 아님.
기본/성장 실제요원원천, 확정run장비/세트/인챈트 중복0, 연구/증강 해당run정책·획득 반영/제외·미지원·미공급 구분. 시작stats와 전투중 일시효과 분리/만료 기존규칙, 복원 중복가산0. DEV서버에서만 원천ID/version·단계값/최종값 복사가능, 사용자화면 필요한최종수치/설명만. 장비1개교체/효과1개추가해제/새process복원 비교, View자체계산으로원본덮어쓰기0.
Server5 ordinary-five-stat-owner-server-contract-20261023의 core stats/장비reader를 출격원본에 실제설치하고 현재HP·기타modifier producer/동일batch상태반영 및 남은효과·kit/timing/wave/reward/seed/engine/결과검증 실행원천 결속 계속. combat.ordinary-five-core-stats.v1.20261023 / equipment-source.v1.20261023은 한정지원이므로 전체effectiveStats완료승격0. NO_ACTIVE_STAT_MODIFIERS는 실제원천version/revision으로만 확정, 미공급을0/없음으로처리0. 기존승인9/29.1 계산순서/정책 재사용, B1인/LOCAL시험수치대체0. Unity17 공급정확버전 소비→제품전투/정보표시/Player검수, 원START와진행checkpoint구분.
Android연결시검수·독립작업계속, 전체전투managed/native/GPU/peak/할당/잔류확인. 기존유효근거재사용·변경source/build새검증, 원key/hash/version·단일owner/정보재사용·AND후속키/미디어운영배포제외 유지.


<a id="data-max-hp-consistency-approved-20261023"></a>
## 최대 HP 변경 시 전투 상태·표시 일치 검수 승인 — 2026-10-23
data.max-hp-consistency.v1.20261023. 사용자 직전제안 승인, 새회복/밸런스규칙 아님.
장비/증강 최대HP변경 및 일시효과만료는 기존정책 계산 currentHP/maxHP를 함께반영, 전장게이지/열린정보창 동일owner 결과사용. 보호막과HP 구분·임의환산0. 변경직후 저장/복원 중복가산·최대HP초기화0. 최대HP변경을치료로오인하여 흡혈/초과회복효과를 발생시키지 않는지 기존사건판정과대조. 미확정부분 비율유지/절대값유지 임의선택0·실제미결만기록. 상태표시갱신으로추가명령/GET0.
Server5/Unity17 출격원본 및 현재HP/modifier·남은효과실행원천 제품연결 계속우선. ordinary-five-stat-owner-server-contract-20261023 한정corestats 근거재사용, 실제START·동일batch·kit/timing/wave/reward/seed/engine/결과검증 결속. NO_ACTIVE실원천필수·미공급0처리/LOCAL수치대체/publicflag우회0. Unity정확버전소비·진행checkpoint분리·실Player검수. Android연결시/전체전투managed/native/GPU·peak/할당/잔류확인. 기존원키/version·단일owner/정보재사용·source/build검증 및AND후속키/미디어운영배포제외 유지.

<a id="ordinary-start-health-stat-server-contract-20261023"></a>
## 일반5인 원START·현재HP·최대HP 원거래 연결 (서버 한정)
combat.ordinary-five-stat-application.v1.20261023. Server#ordinary-start-health-stat-server-result-20261023 및 QA/CommanderS1-20260922/ordinary-max-hp-verified-20261023.json 참조.
startDeparture 비DISPOSABLE_LOCAL I 일반5인 원응답/snapshot의 ordinaryStatSource={origin,originHash}를 기존원START CAS와 함께 고정한다. 공개START 차단은 유지. readOrdinaryRunStatOrigin은 해당계정/run 원본만 반환하며 진행HP/legacy백필을 하지 않는다.
OrdinaryRunStatStore.apply 내부명령 {requestId,expectedRevision} → {version,runId,revision,healthRevision,stats}. 원키/본문충돌거절 및 immutable 영수증복구, 현재HP/장비effect/modifier 원검증과 source/result 저장을 동일batch. stats는 currentHp와 effectiveStats.maxHp/attack/defense 포함. source버전역행은 저장원천복원으로 검증. 같은 결과표시용 GET 불필요.
최대HP차이는 기존 health.max-hp-rebase.v1 후보→CombatHealthStore.prepareChanges 검증→같은batch 확정. 조건부DEF는 보정후현재HP 기준. 사건 MAX_HP_REBASE, 치료/보호막변경 아님. 실제 healthVerifier 및 modifier authority 미공급은 거절; 임의NO_ACTIVE/초기HP를 공급하지 않는다. 장착자체 명령은 별개기존owner, 이 store는 이미저장된 장착원천을 CAS 소비한다. 이번연결은 내부API이며 production caller/효과producer·전체runtime/Player/DEV복사UI 공급완료 아님.


<a id="data-inflight-effect-consistency-approved-20261023"></a>
## 효과 적용·만료와 진행 중 공격 판정 일치 검수 승인 — 2026-10-23
data.inflight-effect-consistency.v1.20261023. 사용자 직전제안 승인. 기존발사공격고정/명중방어적용 유지.
발사후공격효과만료는원탄공격값유지, 발사후획득은원탄소급0/이후공격반영. 명중전방어변경은명중시유효상태 평가. 동시만료/명중은기존사건순서·실행복원일치, 미정순서임의채택0. 발사후저장복원은원공격값/식별보존·중복피해0. 현재표시공격력과원탄공격값차이는 DEV에서만진단, 사용자피해숫자는실확정피해. 미지원효과/투사체미검수 유지.
Server5 ordinary-start-health-stat-server-contract-20261023 START/stat/health동일batch 기반을 실제production modifier producer·HP event verifier·전투loop caller에 결속, 이후 kit/timing/wave/reward/seed/engine/결과검증제품연결 계속. 합성verifier60PASS는실원천증거아님·임의NO_ACTIVE/HP생산0, 기존승인policy/health.max-hp-rebase재사용. Unity17 정확공급버전·실행사건을 제품runtime/표시/저장복원에 연결·Player검수. publicflag/LOCAL시험원천우회0. Android연결시검수·독립진행, 전체전투managed/native/GPU/peak/할당/잔류 확인. 기존정보재사용/owner/원key/version·source/build검증 및 AND후속키/미디어운영배포제외 유지.

<a id="ordinary-initial-modifier-server-contract-20261023"></a>
## 일반5인 최초 modifier 원START 원본 (현재효과 권위 아님)
combat.ordinary-initial-modifiers.v1.20261023. 기존일반5인 START 원거래가 ordinaryInitialModifiers={version,runId,originHash,stage:INITIAL_BEFORE_COMBAT,applicability,modifiers}를 공급. readOrdinaryInitialModifiers는 불변원본을 복원하며 진행중HP/효과·실행phase를 증명하지 않는다. 원본없는 과거run을 현재정의로백필하지 않는다. 현재 OrdinaryRunStatStore.apply는 여전히 실modifier source 필요; 초기pin/HP revision1/원장부재만으로 NO_ACTIVE를현재판정에 사용금지. 단순 테이블부재 기반 기본producer는 검토반례 RED 후 제거. 실제초기화/loop 전이의 원자phase 연결은 미완료. Server#ordinary-initial-modifier-server-result-20261023 / ordinary-initial-phase-verified-20261023.json 25PASS, 전체runtime/진행탄검수근거아님.


<a id="data-effect-once-approved-20261023"></a>
## 동일 효과 사건 중복 수신·복원 1회 적용 검수 승인 — 2026-10-23
data.effect-once.v1.20261023. 사용자 직전제안 승인, 기존중첩정책변경 아님.
동일원사건 재수신 능력치/HP/보호막재가산0. 서로다른정상발동은기존중첩/갱신대로처리. 동일사건ID·다른내용은충돌구분, 무시/덮어쓰기0. 적용후저장복원은원상태보존·재발동0. 만료후오래된적용은기존순서/version대로처리·부활0. 정상다단/반복치료차단0, 효과명만으로중복판정0·기존원천사건식별/발동순서사용. 중복기록owner/수명확인·무한누적0·필요복구기록임의삭제0.
Server5/Unity17 최신우선 실제전투시작phase·HP초기화/stat/loop최초실행의 권위owner/원자결속 및 진행중modifier·HP event verifier/loop caller 실제연결. ordinary-initial-modifier-server-contract-20261023 INITIAL_BEFORE_COMBAT는출격시점원본이며현재phase권위대체0. healthrev1/피해없음/원장없음으로전투전추정0. 구현미완료를사용자정책미결로돌림0·기존승인규칙재사용. 이후실발사/명중/만료/복원·제품결과검증 연결, 합성25PASS는실전투증거아님. Android연결시/전체전투managed/native/GPU/peak/할당/잔류검수 계속. 기존정보재사용/단일owner·원key/version·source/build검증 및 AND후속키/미디어운영배포제외 유지.

<a id="ordinary-phase-initialization-server-contract-20261023"></a>
## 일반5인 원자 초기화 phase 계약 (내부, 제품loop 미설치)
combat.ordinary-phase-initialization.v1.20261023. migration0141 및 실제 START가 PREPARED1 원상태생성. OrdinaryCombatLifecycle.activate(accountId,runId,{requestId,initialTargets:[5인 id,hp,maxHp]})는 기존필수 CombatHealthVerifier로원값증명, 실제장비/원modifier/stat계산 및 INITIALIZING2→HP/stat→RUNNING3/영수증 동일batch. 무기록/healthrev1으로stage추정0. verifier미공급거절, 과거phase없는run백필0.
응답 {version,runId,phase:RUNNING,health:{revision:1,state},stats:{revision:1,...}}. 원키원본문복구/내용충돌/새키재초기화거절. 후속stat expectedRevision1, HP변경기존revision계약. replay초기응답은현재HP복원값아님. RUNNING은DB실행barrier이며전체전투능력/실loop설치flag아님.
DB는PREPARED의HP초기화와전술/증강/host/wave기록을거절,HP초기화는동일초기화batch의INITIALIZING만허용,HP/stat수정은RUNNING. legacyowner경로보존. 기존INITIAL원본은이실제phase거래에서만최초stat원천으로소비; 진행modifier대체0. 제품loop caller/productionHP verifier·효과실행원천은미설치. 근거 Server#ordinary-phase-initialization-server-result-20261023, QA ordinary-phase-verified50/compatibility55/최종START8(중복포함).


<a id="data-effect-display-expiry-approved-20261023"></a>
## 효과 표시·실제 판정 종료 시점 일치 검수 승인 — 2026-10-23
data.effect-display-expiry.v1.20261023. 직전 제안 사용자 승인.
만료시 실제능력치적용종료와아이콘/잔여시간일치, 갱신중첩은실유효시간/중첩수표시. 정화/해제는규칙상제거효과만표시제거. 저장복원은복원전투시간기준·앱종료wallclock임의차감0. 요원교체/닫기 구표시·구독새대상혼입0. 아이콘애니메이션종료로효과판정변경0·실효과owner가판정/표시는소비. 종료잔상은활성오인방지. 배속/일시정지/복원/재획득검수·표시GET/복제원장0.
Server5/Unity17 실제제품loop caller/production HP초기화·event verifier/진행modifier producer 결속 계속우선. ordinary-phase-initialization-server-contract-20261023 PREPARED→INITIALIZING→RUNNING 단일batch는원천기반, RUNNING을제품전체실행flag로사용0. 합성HP검증자로실원천완료대체0·임의NO_ACTIVE/HP/LOCAL수치0. 최초loop event/실효과획득만료/진행탄/결과검증·Unity실제품소비 연결. Android연결시검수/독립진행·전체전투managed/native/GPU/peak/할당/잔류 확인. 기존원key/version·정보재사용·source/build검증·AND후속키/미디어운영배포제외 유지.

<a id="ordinary-initial-health-source-server-contract-20261023"></a>
## 일반5인 신규 초기HP 원천 (실제DB 계산, 진행HP 검증자 아님)
combat.ordinary-initial-health.v1.20261023. OrdinaryCombatLifecycle.activate 내부호출 {requestId,source:ORIGINAL_START}는 원START성장+실장비의기존최종max를 신규Actor초기HP로사용한다(기존 BasicCombatSimulation→CombatVitals 초기화규칙재사용). 진행중HP/복원/회복에는적용0. 실제INITIALIZE전용verifier가정확원snapshot/5인대상/INITIALIZING2/장비CAS를같은phase초기화batch에서검증. 호출자가HP값을보내거나testverifier를주입할필요없음. 기존명시 initialTargets 경로는기존verifier필수. 결과형식은 phase-init v1 유지, 원키복구는현재장비조회전.
장착계산은 categoryId/star/setTag만기존strict계산기에전달, 원instance/effect검증은기존reader/statowner에보존. source최초+이후stat의장착입력버그 RED→수정, Server#ordinary-initial-health-source-server-result-20261023 및 QA ordinary-initial-health-source-final40PASS. 진행HP event verifier·live modifier·loop caller/효과표시만료/Player는미설치·미검수.


<a id="data-effect-partial-stale-approved-20261023"></a>
## 효과 정보 누락·오래된 상태 처리 승인 — 2026-10-23
data.effect-partial-stale.v1.20261023. 직전제안 사용자 승인.
부분변경응답 효과미포함은삭제아님, 명시해제만해당효과제거·나머지유지. 역순/오래된응답으로최신효과·시간되돌림0. 필요한version불연속은기존복구계약으로필요범위만확인. 타요원/run상태혼입0. 효과없음/미공급/해제확정 구분, 유효기존상태재사용·실제무효화없는표시조회0. 부분갱신/명시해제/역순/복원뒤늦은응답 검수: 이유없는삭제/부활/시간연장0.
Server5/Unity17 진행HP event verifier·live modifier producer·제품loop caller/최초loop event/효과만료·명중·결과 실제연결 우선. ordinary-initial-health-source-server-contract-20261023 실제초기HP40PASS는 신규INITIALIZE 한정선행완료로재사용, 진행·복원HP를full초기화0·전체제품승격0. 실제원START/실장비/기존policy·계산재사용, 임의NO_ACTIVE/LOCAL수치/publicflag우회0. Unity정확버전제품소비·효과표시/시간/저장복원 Player검수. Android연결시/전체전투managed/native/GPU/peak/할당/잔류확인·독립진행. 기존owner/정보재사용·원key/version·source/build검증·AND후속키/미디어운영배포제외 유지.


<a id="data-combat-event-display-approved-20261023"></a>
## 동일 전투 사건 HP·보호막·효과 표시 일치 검수 승인 — 2026-10-23
data.combat-event-display.v1.20261023. 사용자 직전제안 승인.
보호막흡수/잔여HP피해는동일확정결과표시. 피해후흡혈회복은기존사건순서보존·표시중간값으로다운판정0. maxHP/currentHP/조건효과 같은반영단위는전장/정보창동일결과. 필수미공급시다른version혼합완료표시0. 복원상태먼저반영후이후사건표시. 게이지애니메이션과실판정분리·확정owner상태로피해/다운/효과판정. 기존원자변경은반영완료후통지, 별개피해/회복을임의합쳐순서변경0.
Server5/Unity17 진행HP event verifier/live modifier producer/제품loop caller·실효과만료/명중/결과 실제연결 계속우선. 실제초기health source한정근거재사용·합성/초기상태로진행원천대체0. 기존승인policy/원START·checkpoint분리·미공급0처리/LOCAL수치/publicflag우회0. Android연결시검수·독립진행, 전체전투managed/native/GPU/peak/할당/잔류확인. 기존owner/정보재사용·원key/version·source/build검증 및 AND후속키/미디어운영배포제외 유지.

<a id="ordinary-live-rebase-contract-20261023"></a>
## 진행 최대HP 검증 구현 범위 — 2026-10-23
DATA-COMBAT-EVENT-DISPLAY의 MAX_HP_REBASE 한정: OrdinaryRunStatStore.apply는 검증된 원START/실장비/필수현재modifier로 계산한 변경만 내부 HP verifier에 전달한다. RUNNING phase/account/run/originHash/snapshot 및 기존 HP·장비·modifier·derived CAS가 같은 transaction에 포함된다. 최대HP/currentHP/조건DEF와 healthRevision은 기존 combat.ordinary-five-stat-application.v1.20261023 응답으로 확정, 정상 추가GET 불필요. 보호막은 별도owner이며 이 명령에서 변경하지 않는다. 원key replay는 역사적 확정응답으로 현재HP 복원대체 금지. 일반피해·흡혈·효과획득/만료 및 제품loop 원천은 아직 미공급; 기본 modifier provider는 계속 없음. Server#ordinary-live-rebase-result-20261023/QA ordinary-live-rebase-final-20261023.json 41PASS는 실제 최대HP verifier+명시 fixture의 범위이며 Player 전체증명 아님.


<a id="data-combat-display-backlog-approved-20261023"></a>
## 전투 표시 지연 시 상태·연출 처리 승인 — 2026-10-23
data.combat-display-backlog.v1.20261023. 직전 제안 승인.
HP/보호막/효과표시는 최신확정상태를 따라가며 밀린게이지를모두재생해판정과괴리0. 실제피해/회복사건·결과집계는보존, 연출정리로실사건누락0. 반복숫자/효과는기존중첩가독성정책범위정리, 다운/복귀/보호막파괴 주요전환식별 유지. 정지/앱복귀/종료뒤 현재상태와맞지않는표시대기정리, 필요한주요전환과실사건을임의삭제0. 대기항목이전체응답/전투객체를무한참조하지않도록소유자수명관리. 배속/다단/다수피해에서 최신표시·집계보존·연출대기메모리 검수.
Server5/Unity17 일반피해·명중 event producer/live modifier 생성만료/제품loop caller 실제연결 우선. ordinary-live-rebase-contract-20261023 진행MAX_HP_REBASE41PASS는한정완료로재사용·fixture현재modifier/선행피해를실원천으로승격0. 기존발사공격고정/명중방어·원사건식별/순서/저장복원규칙재사용, 임의NO_ACTIVE/기본producer·publicflag/LOCAL우회0. Android연결시검수·독립진행, 전체전투managed/native/GPU/peak/할당/잔류확인. 기존정보재사용/owner·원key/version·source/build검증·AND후속키/미디어운영배포제외 유지.


## 2026-10-23 가로 D안 전환 우선 안내 — LANDSCAPE-D2-20261023
최신 사용자 승인 원본은 [44 가로 D안·2번/반대 방향](44_COMBAT_DETAIL_CONTRACTS.md#landscape-two-facing-approved-20261023), 제작기준은 [17 2방향](17_OPERATOR_COMMON_STYLE.md#landscape-d2-animation-approved-20261023). 본문의 세로고정/4방향 제작 및 화면고정풍향과 충돌하는 신규적용은 해당절을 우선한다. 역할/원명령/저장·경제/피해/정보재사용 규칙과 과거원본은 보존. 화면전환 승인과 실제제품 적용/검수 완료는 별개. 세부 수정체크리스트·미결·담당·완료조건은44 한곳에서 관리한다.

<a id="landscape-rail-hazard-owner-20261023"></a>
## 선로포격 원공간 상태 모델 — 부분공급
rail.artillery-hazard.v1.20261023 / RailArtilleryHazard. 승인44 landscape-curve-roof-artillery-20261023 근거. 새 공개API 없음. 원owner가 run/event/train/실객차목록·선로구간mm·preview시각·고정 SINGLE impact 또는 RESIDUAL start/end/interval/maximumImpacts·원damage를 공급. resolve는 예정 combat atMs의 모든객차 원선로span을 요구하고 identity(runId,eventId,impactIndex)·HIT/MISSED·rawDamageIntent·교차객차목록을 반환. 여러collider/객차 교차는 train1개 intent이며 방어/보호막/HP 적용은 기존 owner 몫. 화면좌표/클라보고는 원geometry 증명이 아님. nextImpactAtMs로 예정사건만 처리, 중간프레임으로 시간/피해 생성하지 않음. snapshot/restore 원입력과결과 재검증/동일사건멱등/역순거절. 원발행정의/저장hash/version과 HP동일거래는 연결 미완료이므로 이 모델단독으로 live피해권한/실운영연결 완료를 주장하지 않는다. 시험9PASS 근거 Server#landscape-rail-hazard-result-20261023. 실제collector·제품loop·shield/HP/save 연결과 Player후속은 잔여.

<a id="landscape-authority-owner-alignment-20261023"></a>
## 가로 전투 실제 실행 담당 경계 정합 — 2026-10-23
사용자 담당경계 정합/실제연결 계속 지시. combat-authority.ts captureCombatAuthority/readCombatAuthority 직접대조: combat.authority-split.v1, ORDINARY_SOLO=UNITY_LOCAL/LOCAL_LOGICAL, ORDINARY_COOP=UNITY_HOST/HOST_LOGICAL, WORLD_BOSS_SOLO/COOP=SERVER/SERVER_COMBAT. 모든모드 transactionOwner/resultVerificationOwner=SERVER, clientReportProvesCombat=false. 이파일은 정책정의이며 호스트배정/보고승인/엔진연결 완료증거 아님.
- Server: 최신5인 kit/profile 등 확정출격 입력·버전/hash/권한·거래/저장·원사건 sequence 결속 및 결과검증. 순수 rail-artillery-hazard 모델9PASS는 실geometry/clock/HP 적용완료가 아님. 보고수신/NOT_PERFORMED를 검증완료로승격0.
- Unity: 일반모드 기존 판정owner의 최신정책 handler/BasicCombatSimulation·CombatSceneDriver 제품binding/원사건 emit·HP/보호막/효과·표시/저장복구 연결. 일반 실행loop 전체를 Server공급대기로두지않음. 협동은 호스트권위 유지, 모든client가각자판정0.
- 동일한 사건의 독립된 서버HP진실을 새로 만들지 않음. 서버 검증용 계산과 런타임 판정은 목적/입력/버전/결과대조를 명시. 실행 위치가 다른 순수모델은 구현언어/사건계약과 공유fixture로 대응을 검증하고 'TS모듈있음=Unity연결됨'으로처리0.
- 원START→원실행입력→일반Unity판정/사건→기존저장/서버검증→표시/복원 경로의 실제caller를 각담당기존QA에 남긴다. 필드/검증부재만 구체대기, LOCAL샘플/정적정책을제품완료로승격0. 월드보스서버권위를 일반모드로확장하거나반대로변경0.
원근거 Server인계#landscape-rail-hazard-result-20261023 및 바로앞경계조사, Unity큐 unity17-landscape-base-result-20261023 부분기록. 기존 공통 데이터재사용·상태소유·원키복구 규칙 유지.

<a id="ordinary-kit-source-contract-20261023"></a>
## 일반5인 원START kit 입력 공급 — combat.ordinary-five-kit.v1.20261023
담당경계49 landscape-authority-owner-alignment 적용. 실제 startDeparture가 ordinaryStatSource를 생성하는 같은경로에서 ordinaryKitSource={packet,kitHash}를 원START응답·snapshot·멱등receipt에 추가. packet 필드: version/accountId/runId/originHash/policyVersion/policyHash/authority/execution/ operators/requiredEffectKinds. hash는 canonical JSON SHA256 소문자64hex(접두사없음), kitHash는packet전체, policyHash는같은origin.policy. originHash는이미고정된ordinaryStatSource.originHash. 독립성장원본/편성5slot/정확policy/ORDINARY_SOLO권위와일치필수. operators는기존binder의 operatorId/slot/role/passive/abilities(각2개)이며 수치·조건·스킬을새로계산하지않음. action/effect/timing정책과기본interval/skillMagnitude는같은origin policy/actors 재사용. packet중복정책전체덤프없음.
실행표식은 REQUIRES_MATCHING_UNITY_HANDLERS; 원START 입력확보는Unityhandler/engine/source-build/결과검증완료아님. 최초/복원은같은원START pin을 재사용하고 일반진행HP를최대HP로초기화하지않음. 원pin hash를독립신뢰경로에서받아대조; 자기신고hash만으로실행권한없음. readOrdinaryRunKitSource(db,accountId,runId)는해당run의원본만읽고누락이면 ordinary_kit_source_unavailable,변조면 ordinary_kit_source_invalid. 역사run을현재kit로백필하지않음. v1의참조정의/binder는보존하고변경시새identity/retainedreader필수. coophost·worldboss미확장. 새GET/API없으며기존성공응답/메모리재사용.
Server#ordinary-kit-result-20261023 최종고유32PASS/type/lint0(초기5초timeout 후제한30초 재검증근거분리). Unity 정확packet adapter/제품handler·원사건emit/Player 및 Server sequence/build/결과검증은잔여.

<a id="all-screens-landscape-server-compatibility-20261023"></a>
## 전체게임화면 가로 전환 — 서버 원데이터 호환
33#all-screens-landscape-approved-20261023이전투한정범위를대체. Server#all-screens-landscape-server-audit-20261023 직접코드대조범위에서 편성slot/물리BOUND_FRAME_LOCAL mm·frameToNative/선로arc interval은표시픽셀과분리됨. 가로전환시해당권위값회전·재발행/신규표시GET/HP진실추가없음. portrait 초상ID와cursor direction은화면방향아님.
기존 CE03 presentation/rail manifest는고정된역사/시험identity로보존. 160 frame=8actors×5states×4프레임이며4방향요구로해석금지. 신규5인70+Kyle2와현재가로UI자산은실공급manifest·renderer source/build를별도버전에결속해야하며 기존hash/rolecount를조용히바꾸지않음. 실제신규자산식별자/소비묶음미공급상태에서추측schema신설없음. Server활성계약변경필요를확인하지못해이번원기능수정/시험없음;전체화면가로/실Player검수완료선언아님. 일반Unity/월드보스Server실행권위와원key복구유지.

<a id="landscape-branch-completion-server-boundary-20261025"></a>
## 분기 이탈·완료 서버 기존 원본/미공급 경계 — 2026-10-25
LANDSCAPE-BRANCH-COMPLETION-v1.20261025 적용. Server 인계 #landscape-branch-completion-server-result-20261025 / QA/CommanderS1-20260922/branch-completion-result-boundary-20261025.json 31PASS. 기존 BResultReportStore.submit은 원START→readOriginalBRouteSource/readBNativeNodeSource의 instanceId/hp 전부와 보고를 대조한다. 적 누락/살아있는HP/새ID 대체/누락·살아있는승차병은 거절; 화면 밖·분리는 처치 근거가 아니다. 같은 원키 영수증/원본복구 유지, 기존 RESULT consistency를 전투 진실 증명으로 승격하지 않음.
현재 operation.map-plan.v1 edgeId/traversedEdgeIds는 노드 간 경로이며 적별 철도 잠금·재합류 source가 아니다. 기존 b-result-report.v1/v2.20261006의 ExternalVertical/2wave 제약을 새 가로 source로 묵시 변환하지 않는다. 새 연계에 필요한 원본: 콘텐츠 원목표·필수 enemy IDs 또는 원시간/도달점, rail topology/branch ID·재교전가능성 정의 및 버전/hash(Server START 고정 담당); 실행 중 동일 run/enemy generation/ID·HP/효과·원경로·잠금·사건단계/sequence·원종료키(Unity 실행/checkpoint 담당). Server는 실제 확정 버전에 원키/CAS·저장복원·결과대조를 연결한다. 지금은 이 신규 wire DTO와 원콘텐츠를 공급한 상태가 아니며 위 목록을 임의 승인된 payload로 사용하지 않는다. Unity 인계 #unity17-branch-completion-work-20261025 대조. 표시GET/일반서버HP진실 신설0, 역사source 보존. 부모/WAIT#landscape-branch-completion-followups-20261025 AND 후속 유지.
<a id="ordinary-kit-activation-binding-20261025"></a>
## ORIGINAL_START 초기화의 원kit binding — 2026-10-25
기존 combat.ordinary-phase-initialization.v1.20261023 영수증에 additive ordinaryKitBinding={version,originHash,kitHash}. version은 기존 packet.version(combat.ordinary-five-kit.v1.20261023), hash들은 START ordinaryKitSource와 동일. 새 ORIGINAL_START 초기화는 이미 읽은 original snapshot으로 원kit 검증 필수, HP/stat/phase와 같은 batch·snapshot fence에 영수증 저장. source 누락/변조면 초기화0. 기존 explicit initialTargets 경로/역사영수증은 해당 optional field 없음; 미공급을 빈kit으로 해석하지 않는다. 전체kit은 START 원본 재사용, 같은원키는 저장된 binding 반환. 원kit검증은 Unity handler/실행/전투진실 증명이 아니다.
근거 Server#ordinary-kit-activation-result-20261025 / QA/CommanderS1-20260922/ordinary-kit-activation-final-20261025.json 12PASS/type/lint0. 실제 activate 제품caller/HTTP는 미설치, START 공개 admission 차단 유지. 새가로 분기 원정의/checkpoint/result 및Player/API·메모리 검수는 별도 잔여.
<a id="ordinary-initialization-recovery-contract-20261025"></a>
## 기존 lifecycle 원초기화 상태 — 2026-10-25
기존 인증 GET /v1/runs/:runId/lifecycle에 additive ordinaryInitialization. 정확 version combat.ordinary-initialization-state.v1.20261025; 필드 status(PREPARED|RUNNING|UNAVAILABLE|NOT_APPLICABLE), revision(1|3|null), originHash(string|null), runtimeExecutionVerified:false. PREPARED1/RUNNING3은 run/account/원stat hash/phase의원START snapshot 일치필수. 초기화 원본/phase미공급 또는불일치는 UNAVAILABLE, ordinary 원본과행이없는역사모드만 NOT_APPLICABLE. snapshot전체부재도 UNAVAILABLE. 불가상태의revision/hash는null. 단일account-scoped SELECT에phase JOIN하여같은SQLsnapshot사용, 추가조회/쓰기없음.
초기화상태는 runtime실행허가/입력재개/HP초기화명령이 아니다. 기존 START/ordinaryKitBinding·소유checkpoint 재사용; RUNNING이라도 실제handler·checkpoint검증필수. 기존nextAction/증강/결과/보상계약보존; 해당필드없는구서버응답은미공급으로취급. Unity RunLifecycleSnapshot는추가필드를차단하지않지만 typed소비는아직미구현. 자기제품bootstrap/factory에연결할때현재원본·checkpoint와대조해야하며새화면GET사유없음.
실제 caller combat-readiness.ts registerRunRecoveryRoutes→projectOrdinaryInitialization, Server#ordinary-initialization-recovery-result-20261025 및 QA/CommanderS1-20260922/ordinary-initialization-recovery-final-20261025.json 32PASS/type/lint0. Hono인증route/SQL검증이며Player/TCP/메모리근거아님. publicSTART gate/activate제품caller/일반5인실행·원checkpoint/새분기원정의결과연결잔여.
<a id="ordinary-five-start-consumer-20261026"></a>
### Unity 원START 5인 typed 소비 경계 — 2026-10-26 / 부분 구현, 실행 factory 아님
Unity17 구현: `OrdinaryFiveStartInput.Read(originalStart, expectedAccount, expectedRun)`; packet version `combat.ordinary-five-kit.v1.20261023`, stat origin version `combat.ordinary-five-core-stats.v1.20261023`, policy `combat.policy-catalog.2026-09-29.1`. 원origin/kit canonical SHA256 재계산, account/run·admission formation/loadout revision·5slot/ID·policy/authority 대조. 각 kit는 기존 CombatDefinitionIntake 승인 publication의 operator projection hash와 일치해야 하며 재hash한 변경능력도 거절. slot순서 readonly Actor의 permanent HP/ATK/DEF/basicInterval/skillMagnitude와 능력·passive 입력을 보관. 이 값은 현재HP/진행효과가 아니며 ownedSourceHash/definitionHash의 실제 외부원본 전체 검증이나 실행허가를 뜻하지 않는다.
제품 소비 진입점 `AccountScreenRuntime.ReadOrdinaryCombatInput(runId)`는 기존 RunSelection Recovery+정확run만 허용하고 현재 account의 직접확정/lookup/lifecycle/보관 PendingAccountAction 원응답을 재사용한다. 같은run의 원originHash/kitHash 충돌은 거절; cached typed 자료도 다른account에 반환하지 않는다. 표시 GET/별도 네트워크 없음. 원영수증의 COMMITTED 상태·raw START는 kit검증실패로 삭제하지 않는다. 아직 이 메서드를 호출하는 실제 combat bootstrap은 미설치(소스에서 선언만 확인).
기존 typed OrdinaryInitialization은 앞선 Unity Threat687에서 이미 구현됨. 49#ordinary-initialization-recovery-contract-20261025의 ‘typed소비 아직미구현’은 당시 기록이며 현재는 해당 metadata소비 완료, 이번 원kit소비와 모두 실전투준비/권한과 구분한다.
**실제 factory/checkpoint/종료사건 계약은 아직 확정·구현되지 않음.** `BasicCombatSimulation.FromPreparedProfileForLocalTest`는 구 CE03 hashed LOCAL profile 전용, `CombatSeraCommands`/`BMiraActiveCommandOwner` 등 최신 부분owner와 동일한5인 action/HP/clock 소유자로 통합하는 Unity 자체작업 잔여. `CombatSceneDriver.ForRetainedStart`→`CombatCheckpointStore`는 `PendingRunOperation` 기반인데 실제 `AccountScreenRuntime.SubmitDepartureAsync`는 `PendingAccountAction` 기반이라 직접호환 아님. 기존 checkpoint 스키마를 새5인 runtime 지원버전으로 선언하지 않음. 새 factory/checkpoint schema/version/종료ID를 임의 발급하지 않았으며 Server caller를 이 typed reader만으로 실행 연결하면 안 됨.
외부원본 경계: 49#ordinary-kit-source-contract-20261023의 원kit에는 basic range/windup/operation enemy-wave geometry가 없고 49#landscape-branch-completion-server-boundary-20261025의 철도 branch/rejoin 신규원본도 미공급. 해당 내용만 외부계약대기이며 일반5인 handler/shared owner·제품저장접점 전체를 Server 대기로 분류하지 않음. 공개 gate/입장/real START 변경 없음.
검증: QA/OperatorInformation-20261019/Five688-red 0/8→Five689-green39PASS, Five690-integration86PASS, 검토 충돌재현 Five691-review-red1PASS2FAIL→Five692-final89PASS. 합성 START identity/stats + 실제 승인능력정의 기반 Editor 검수; 인증된 실START/제품 Player전투/3회메모리/새process복원 증거가 아님. Unity 기존 인계 #unity17-five-product-work-20261026 및 후속 결과 절 참고.
<a id="ordinary-five-sera-consumer-20261026"></a>
### Unity 원START 첫 세라 native 실행/저장 소비 — 2026-10-26
부분 capability: OrdinaryFiveSeraFactory `combat.ordinary-five-sera-factory.v1.20261026`, checkpoint owner `combat.ordinary-five-sera-owner.v1.20261026`, store binding `combat.ordinary-five-sera-store-binding.v1.20261026`. 원START typed5인+동일원천 core stat snapshot을 기존5 CombatVitals/clock와 세라 HEAL_ONE에 연결. 기본공격/기타active/ultimate/전장종료/제품입장 capability 아님. 종료사건ID 발급 없음.
제품 PendingAccountAction COMMITTED 원START→OrdinaryFiveSeraCheckpoint→기존 CombatCheckpointStore CAS 접점 구현. account/run/preview/request key/body/retained response/build/origin/kit/config 결속, missing 저장은 복구거부. 최초 생성은 명시적 Save0로서 실제 초기화 coordinator 미연결, 공용 CanStart gate 불변. 외부 store schema ce03-local-store-v1 유지, 새 nested identity로 분리. QA 평문포트 검증은 Android 암호화/실제품저장 인증근거 아님.
증거: Unity 인계#unity17-five-sera-result-20261026, Five69557PASS/Five70013PASS, Five702 build, Five703-player writer/reader(새process 세라component3건 tick4→8 연속상태 일치). 합성 START 시험이며 실제제품 Player 전투종료/API/메모리전체 검수는 남음. 전체5인효과/shared action/로비 bootstrap은 Unity 자체 잔여로 유지.
<a id="ordinary-basic-source-contract-20261026"></a>
## 승인 일반 BASIC 원본·START 결속 — 2026-10-26
정책 BASIC-REINA-SERA-v1.20261026(44 승인/proposal §1–4), BASIC-MIRA-EVE-A-v1.20261026. Server 원본 `server/apps/game-api/src/combat-ordinary-basic-source.ts`, version `combat.ordinary-basic.v1.20261026`. 실제 `startDeparture`의 기존 일반5인 분기에서 `ordinaryBasicSource={packet,basicHash}`를 기존 stat/kit와 같은 원START snapshot/receipt batch에 저장. 새 API/추가 GET/공개START 해제 없음. kit `combat.ordinary-five-kit.v1.20261023` 및 기존10능력 정의/hash 불변; 미라 실험 정의/scope 불변.
`packet`: version/accountId/runId/originHash/kitHash/scope=ORDINARY_SOLO_FIVE_APPROVED_BASIC/baseVersion/profiles 및 아래 정책. basicHash는 canonicalJson(packet)의 SHA256 소문자64hex(접두사 없음). 별도 새 basic 정의/원START source 결속 hash이며 기존 kitHash에 섞지 않는다. originHash→원stat, kitHash→원passive/active/ultimate; 새 ATK/피해 원장이나 현재 로비 값을 복제하지 않는다.
`profiles` 순서 Reina/Sera/Mira, formal abilityId `op_reina.basic`/`op_sera.basic`/기존 `op_mira.basic`, kind BASIC. Reina/Sera modes MELEE/RANGED 동일 투사체, Mira RANGED 한정. 각 range.physicalAdjacentCars=1/1/2, maximumDistanceMm=24000/18000/24000. ballistics.speedMmPerSecond=72000/36000/48000, radiusMm=35/80/60, maximumFlightDistanceMm=24000/18000/24000. maximumFlightMs는 exact rational `{numerator,denominator}`=1000/3,500/1,500/1; Unity 원step 올림이 비행거리 상한을 늘리지 않는다. timing={baseCycleMs,windupMs,recoveryMs}=1400/180/220,1200/240/260; Mira timing=null(이번 W/R 정의 미공급). originalBasicIntervalMs는 기존 base의1400/1200/1800을 재사용. 신규값은 승인 첫시험값이며 실공간/밸런스 실측치 아님.
`timingPolicy`의 appliesTo는 Reina/Sera 두 formal ID뿐. 기회시작 Ceff snapshot, W0/C0·R0/C0 비례, 원step 누적경계, W/R를 C에 가산하지 않음; 원 target/mode/generation 고정·Commit직전 생존/거리/LOS 재검사; 실패/취소 발사·명중·충전0 및 R회수/원nextAttack 보존, 재표적 준비 이전0. 실제 same-tick/상위중재는 Unity 기존 owner; Server에서 두번째 실행시계를 만들지 않음. 저장은 phase잔량/target세대/attackId/Commit/진행탄 원상태 유지 계약, 이 packet이 checkpoint 자체는 아님.
`damagePolicy`는 기존 피해/계수/발수 및 일반 치명·연구 modifier 유지, `opportunityPolicy`는 원기회/기존추가탄 보존. spatialPolicy는 실제 muzzle→hit anchor와 기존영역·LOS·칸 제한 교집합/같은Commit revision의 실제기하/고정방향sweep/발사공격·명중방어. 피해계수나 실제 actor hitbox/위치를 새로 발명하지 않음. seraSeparation은 단일공격탄≠즉시치료/초과치유생명탄·수정3개발수증가0. eveCarrier는 본인BASIC 매개만, independentAutoFire/independentHp/additionalDamageOrCharge=false; 기존active drone/ultimate 별도, ballistics/socket=null. 카일·이브 미승인 수치 미적용.
reader `readOrdinaryRunBasicSource(db,accountId,runId)`는 기존 계정범위 원START reader+frozen kit 검증을 재사용하고 예상packet 전체와 hash 일치검사. foreign→run_not_found, 누락→ordinary_basic_source_unavailable, 값/hash 함께변경·타version/타run/kit불일치→ordinary_basic_source_invalid. 이 내부 reader는 추가 조회 endpoint가 아님. `readOrdinaryBasicFromOriginal(original,kit)`는 호출자가 이미 검증한 원본/kit 재사용. ORIGINAL_START 활성화는 필드가 있는 경우 같은 snapshot fence로 검증하고 동일 초기화 batch receipt에 `ordinaryBasicBinding={version,originHash,kitHash,basicHash}`만 결속. 전체packet 복제/후속GET 없음. 원키 replay는 원receipt 반환. historical 필드없음은 기존 경로 유지하되 새 BASIC 지원으로 해석/자동보충 금지; null은 누락으로 인정하지 않고 거부. 다음정의는 새version과 retained reader를 추가하며 v1 함수를 제자리 변경하지 않는다.
검증: QA/CommanderS1-20260922/ordinary-basic-source-red-20261026.json(필드없음 RED1), ordinary-basic-source-verified-20261026.json(16PASS), ordinary-basic-compatibility-20261026.json(출격/기존미라26PASS). game-api tsc/변경4파일 eslint/scoped diff-check exit0. START/원키재생·초기화binding/현재로비변경무관·foreign·재hash변조거부/초기화0·historical미보충. 정적 독립리뷰 확정결함0(테스트 재실행 없음).
현재 canStart/runtimeSupported=false 및 missing 명시. 실제 geometry/소켓, Kyle/Eve BASIC 완정의/Mira W/R, Unity shared action/native projectile/체크포인트/result binding 미완료. Unity17 Basic706 산술26PASS·세라부분실행/새process Five703/704는 해당 범위 근거이며 이번3BASIC 실제 소비/제품API 검수 아님. 해당총구/기하 AND 미충족으로 WAIT 소비큐 선등록0. 실제5인 제품 Player/API 정상·복구/3회메모리/Android는 이번 작업에서 미실행.

<a id="ordinary-basic-v2-v3-contract-20261026"></a>
## 카일·이브 v2 / 미라 준비회수 v3 — 신규 START 최신 버전
승인 BASIC-KYLE-EVE-v1.20261026(44#kyle-eve-basic-ballistics-approved-20261026/proposal§1–4), BASIC-MIRA-TIMING-v1.20261026(44#mira-basic-timing-approved-20261026/proposal§1–4). 앞 ordinary-basic-source-contract의 v1은 불변 역사계약. 동일 combat-ordinary-basic-source.ts에 captureOrdinaryBasicSourceV2/V3 추가, 신규 startDeparture는 최종 `combat.ordinary-basic.v3.20261026` 생산. v2=`combat.ordinary-basic.v2.20261026`, v1/v2 원producer 본문 유지. version별 reader가 해당 원정의를 재구성·canonical 전체/hash 대조하며 unknownversion·계정/run/kit/내용변조/null 거부. 과거 snapshot의 미라 timing=null을 새W/R로 보충하지 않음. v1/v2 저장값/hash/초기화receipt 재생 보존; START/recovery 기존응답과 단일원본 사용, 추가GET/전체응답복제/새DB원장 없음. existing lifecycle의 optional ordinaryBasicBinding은 선택된 버전/hash를 같은 초기화fence/batch에 결속, 다른기능 실행지원 주장 아님.
v2: profiles 순서 Reina/Sera/Mira/Kyle/Eve. 기존3개 profile 그대로, formal `op_kyle.basic`,`op_eve.basic` 추가. Kyle modes MELEE/RANGED·attackForm MODE_DEPENDENT_CONTACT_OR_PROJECTILE. **Kyle profile.range/ballistics/launchSource는 RANGED 전용**(range.appliesTo=RANGED): 기존 physicalAdjacentCars0, 최대18000mm, 실손 발사점, speed30000mm/s/r100mm/flight18000mm/600ms rational600/1. **MELEE는 profile.melee**: body→hit anchor 최대1800mm AND 실제Commit fist sphere r120mm·단일선택적shape접촉·LOS, physicalAdjacentCars0·같은접근가능지붕/원근접적격. 거리만으로 원격주먹/몸전체AoE/주먹순간이동 금지, 공격·방어는 같은Commit 평가. 공유timing C1800/W300/R350, originalBasicIntervalMs는 기존base1800. kyleModePolicy는 Ready선택/준비mode고정/다음Ready재선택·공유주기, 접근불가틈/다른열차는 원원거리적격 필요, 요격원준비250/회수450·잔여BASIC정지재개 별도유지. 신규±1범위확장/과학tag0.
Eve modes MELEE/RANGED 동일BASIC비행체총구 단일탄. range physicalAdjacentCars1/최대24000mm와 이브본인원지원영역·LOS 교집합, speed60000mm/s/r45mm/flight24000mm/400ms rational400/1, C2000/W250/R300. eveCarrier.ballistics={profileAbilityId:op_eve.basic} 참조, socket=null(미공급), 새orbit좌표0. BASIC ATK/치명/흡혈/충전은 이브원사건1회, activeDrone 별도원owner/HP/수명. independentAutoFire/independentHp/additionalDamageOrCharge=false 유지. 준비끝원Commit1회·추가정렬250ms없음, 미정렬/차폐/미공급총구 불발회수·몸중심대체0, 적추격사거리연장0·장갑직접공격0. 다운후새BASIC0·기발사탄원수명유지·active생존을BASIC대체로해석0.
v2 spatialPolicy.appliesTo=PROJECTILE_MODES_ONLY로 Kyle melee의 별도규칙과 구분. timingPolicy.appliesTo에Kyle/Eve 추가, 기회시작 Ceff snapshot/W0,C0,R0비례/누적step·불발회수/원nextAttack/target-generation고정은 기존2요원계약 재사용. restore는mode/phase잔량/nextAttack/원attackId/Commit/진행탄/매개체ID 단일owner결속. 위 정책은 Server 정의이며 Unity action/physics 실행구현이 아님.
v3: v2의 다른4요원 정의불변. Mira profile.timing={baseCycleMs:1800,windupMs:280,recoveryMs:320}, approval=BASIC-MIRA-TIMING-v1.20261026, timingPolicy.appliesTo에op_mira.basic 추가. 기존24m/48m/s/r60/500ms·일반연구치명·원피해/추가탄 유지. miraEmissionPolicy.primaryShotsPerOpportunity=1; originalCountSources는 기존 BasicCombatSimulation.ResolveBasic(1 Damage/Charge후원attack제거), BMiraFirstProjectileOwner.Launch(MAIN1개 + 기존장착coeff기반 별도SECONDARY최대2). 이 대조는 현재소스 검토이며 일반5인새factory 발사완료 증거가 아님(generalRuntimeVerified=false/해당missing유지). 기본1발을기관총외형으로연사화0, 원EN17/증강·관통branch/subshotID 보존. 실제catalog의op_mira.active6발/ultimate16발을 각각보존; 문서의 느슨한16연사표현으로BASIC16발/active16발을발행하지않음. 실제action emission만섬광/음향/궤적을소비하고View callback피해0·동일사건중복0·복원시과거섬광/피해/충전재생0 계약.
v3 missing은 ACTUAL_GEOMETRY_AND_SOCKETS, UNITY_ACTION_CHECKPOINT_AND_RESULT_BINDING, GENERAL_MIRA_EMISSION_AND_ORIGINAL_SECONDARY_BRANCH_VERIFICATION. 기존 MIRA_ACTION_TIMING 미공급은 승인정의로해소, actualruntimeSupported/canStart=false는유지. 승인새지붕이동-공격재개안은 없음(roof-move-basic-resume-proposal 미승인)이며이번코드에반영0.
검증: v2 RED 구v1반환1FAIL→ordinary-basic-v2-verified-20261026.json18PASS + departure12PASS. v3 RED 구v2반환1FAIL→ordinary-basic-v3-verified-20261026.json32PASS(원START20+departure12), game-api tsc/3파일eslint/scopeddiff-check exit0(CRLF향후정규화경고있음). v1/v2원hash·초기화replay, v3다른profile불변·Mira수치/active6/ultimate16, 계정/run/kit/unknownversion/null/재hash변조거부. v1기존미라14PASS는코드불변근거재사용,32에중복합산0. 각v2/v3정적독립리뷰 확정결함0/테스트재실행0, v3발수대조는root직접확인(리뷰어독립재확인아님). 실제Unity실접촉/총구/공속·취소·요격/새프로세스/제품Player/API/3회메모리/Android는이번미검수, 후속AND미충족으로소비큐선등록0.

<a id="ordinary-basic-unity-consumer-20261026"></a>
### Unity BASIC 원본 v1/v2/v3 소비 및 기존 저장 결속 — 2026-10-26
OrdinaryFiveStartInput.Basic / OrdinaryBasicSource가 combat.ordinary-basic.v1/v2/v3.20261026를각정규화정의pin으로검증. readonly profile·actual원timing·Kyle melee별도값·v3Mira원주탄수소비, legacy미공급/null/지원버전구분. v1/v2미라timing 자동보충0. 제품보관START간Basic pin일치, 기존Sera factory/저장복구 구성결속 추가. 전체packet 중복저장/GET/공개입장변경0. 신규BASIC실행factory나전장완료capability발급아님.
근거 Unity인계#unity17-basic-source-result-20261026 / Basic71339PASS+별도시험skip1, Basic714과거저장새process복원1PASS. 원START→typed입력/기존부분factory저장pin까지 연결; 실제5인 shared action/탄도/HP/진행탄checkpoint/로비bootstrap/result는미완료. API/Player전체/3회메모리/Android검수와구분. 실제source정의와승인값공급은완료돼도실geometry/socket·Unity실행연결은별도경계.
<a id="ordinary-relocation-source-contract-20261026"></a>
## 승인 재배치 원계약과 저장 호환 경계 — 2026-10-26
ROOF-MOVE-BASIC-RESUME-v1.20261026 /44#roof-move-basic-resume-approved-20261026와proposal§1–5. Server원본 `server/apps/game-api/src/combat-ordinary-relocation-source.ts`, version `combat.ordinary-relocation.v1.20261026`. 신규 실제 startDeparture의 일반5인 branch에 `ordinaryRelocationSource={packet,relocationHash}`를 기존stat/kit/BASIC와같은snapshot/receipt batch로pin. BASIC는최신v3그대로이며v1/v2/v3함수본문·기존hash해석변경0. relocationHash=SHA256(canonicalJson(packet)) 소문자64hex,접두사없음.
packet={version,approval,accountId,runId,originHash,basicVersion,basicHash,scope:ORDINARY_SOLO_FIVE,admission,departure,moving,arrival,checkpoint,runtimeSupported:false,canStart:false,missing}. producer는검증된basic객체를재사용하고basicVersion=v3필수, 새기하/발사수치/이동속도생성0. formal이동원계약이며실이동상태/독립원장아님. admission은기존공간owner의경로·점유·생존·잠금검증/예약수락만입력,실패시기존공격유지·자동추격false. departure는Idle즉시적격이동/Windup현재기회Commit재검사및R종료/Commit-Recovery잔량보존·원다단마지막발수까지유지;기발사탄도착대기0·이동총구재부착0·수락이동대기중다음기회생성0. 다운/강제중단·취소/예약해제는기존owner,공격시계/충전초기화0.
moving.newBasicOrActive=false/missedOpportunitiesAccumulate=false/cooldownsContinue=true,원nextAttack전투시간보존. 이브BASIC소유자이동추종/새발사0,active원능력별도. arrival.order는같은step의확정위치·객차·기존지원영역·총구갱신→기존상위중재(보류궁극기등)→원nextAttack/기본적격→기한충족시새W한기회. 아직기한전이면잔량대기·기한후몰아치기/즉시Commit0,위치owner확정사건이며애니메이션callback판정0. supportScope는기존능력계약유지·암묵적범위이행금지. 새 relocation-support-scope 제안은반영하지않음.
checkpoint.owner=EXISTING_SINGLE_UNITY_CHECKPOINT;공간owner 위치/경로진행/수락목표/예약과action owner 기회/mode/phase/잔량/nextAttack/대상세대/Commit을동일저장경계로상호참조. 실제checkpoint구조/동작은Unity가소유하고Server가추측필드DTO/별도움직임원장을신설하지않음. 기존원START relocationHash참조재사용·정적packet복제0·미수락UI/미소비입력자동재실행0·재예약/중복도착으로기회2회0,계정run/sourceversion/hash격리계약.
`readOrdinaryRunRelocationSource(db,accountId,runId)`는기존원START범위와basic검증을재사용. foreign=run_not_found,누락=ordinary_relocation_source_unavailable; 재hash내용/order/버전/계정run/basicHash변조/null은ordinary_relocation_source_invalid. 내부reader이며새endpoint/추가표시GET아님. `readOrdinaryRelocationFromOriginal(original,basic)`는초기화가읽은원snapshot과검증basic재사용. ORIGINAL_START 활성화는필드존재시쓰기전에검증하여같은fence/batch receipt에 `ordinaryRelocationBinding={version,originHash,basicHash,relocationHash}`만추가,원키재생은그대로원receipt. historical누락은기존계약그대로두고보충하지않음,새재배치지원으로간주0. 과거v1/v2기록에는새relocation필드가없어야하며v3지원요구를묵시적용하지않음.
기존Server CombatCheckpointStore 실제코드확인: hostcontext/원START/CAS/원키검증에따른정확텍스트저장이며status STORED_UNVERIFIED/combatVerification NOT_PERFORMED. 이경로를단독일반전투움직임검증자/완전상태validator/새restoreselector로전용하지않고코드변경0. 실제단독checkpoint원소유는Unity제품경로. 이동예약/도착/현재HP및공격단일owner/전체복원검증이완료됐다는주장없음.
검증 QA/CommanderS1-20260922/ordinary-relocation-source-red-20261026.json 필드없음RED1→ordinary-relocation-source-verified-20261026.json34PASS0FAIL0SKIP(START22+departure12),game-api tsc/4파일eslint/scopeddiffcheck exit0(CRLF안내있음). 실제내부START동일pin/읽기DB무변경/원키·lobbyrevision후재생/foreign·내용재hash변조초기화쓰기0/기록누락미보충/구BASIC v1/v2보존을검증. 독립정적리뷰확정결함0(시험재실행0). 실제공간예약·이동/action중재·새프로세스checkpoint·전체Player/API정상복구/3회메모리·Android는이번미검수. WAIT AND미충족으로소비/통합선등록0.

<a id="ordinary-relocation-scope-contract-20261026"></a>
## 재배치 v2: 능력별 지원 범위·진행 효과
RELOCATION-SUPPORT-SCOPE-v1.20261026 /44#relocation-support-scope-approved-20261026/proposal§1–4. 신규 START는 `ordinaryRelocationSource.packet.version=combat.ordinary-relocation.v2.20261026` 생산. 기존필드명/relocationHash canonical SHA256규칙 유지, basic v3 및kit v1불변. captureOrdinaryRelocationSource(v1)본문보존, captureOrdinaryRelocationSourceV2(basic,kit)에서계정/run/originHash/kitHash연결검사후명시분류추가. reader는v1/v2저장버전별정확비교,원초기화가이미검증한kit/basic재사용(추가DB조회0),기존영수증compactbinding/원키/누락미보충 유지.
추가필드: kitHash, supportScopeApproval, basicScopes[5], skillScopes[10], passiveScopes[5], unclassifiedAbilityIds, crossing, ongoingEffects, scopeCheckpoint. 기존arrival.supportScope는v2에서 EXPLICIT_PER_ABILITY_MAPPING_UNCLASSIFIED_MIGRATION_FORBIDDEN. 각row는formal abilityId와basis/effectRetention/reference, static정의전체복제0. 기본5인은기존basicv3의physicalAdjacentCars를참조해실현재객차instance+실물리인접기준(각mode·월드거리·원대상·LOS교집합),편성slot/카드순서/장비owner/피해귀속은불변.
능력원정의대조: `combat-policy-binding.ts`, `combat-policy-catalog.ts`(currentCatalog의9/29변경은Eve방어/Kylepassive/장비·보호막규칙이며아래대상범위를바꾸지않음), `combat-shield-budget.ts`,44 six-skills§3–5/F2.
- op_kyle.ultimate: GLOBAL_LIVING_ROSTER,확정대상보호막잔량/만료유지.
- op_eve.active: INSTALL_CURRENT_LIVE_CARRIAGE_THEN_FIXED. op_eve.ultimate: EXISTING_DEVICE_OR_INSTALL_CURRENT_LIVE_CARRIAGE. 기존장치는원객차/HP/수명/다음발사보존;없을때만원설치계약. 기존44§3의시전자이동≠장치이동명시를소비하며BASIC추종매개체와혼합0.
- op_sera.active/ultimate: FORMATION_ROSTER_ORIGINAL_SLOT_TIEBREAK.44§4의편성요원집합/수락대상고정·원slot동률정렬유지. 치유범위를BASIC인접1칸으로제한/변경0,확정치유회수/재치유0·다운사건지원1회.
- op_mira.active: CURRENT_CARRIAGE_ORIGINAL_BASIC_RANGE.44§5의기본공격범위에서대상고정/매미발사생존·사거리검사,기발사탄유지. 새발수/발사간격없음.
- **미분류4개** op_kyle.active(보호막+전열도발복합범위원점), op_reina.active/ultimate(정식kit DIRECT_DAMAGE에명시원점/범위없음), op_mira.ultimate(현재사격가능적순환은있으나해당원점·범위의정식필드미확정). UNCLASSIFIED로명시하고추측공간형/편성형이행금지. 이는읽은현정식kit/관련원문범위의확인한계이며전체역사자료부재주장아님. 원범위필드/원점과모드별선택계약을소유원정의에서확정한뒤별도version이행필요.
패시브5개는원source event filter 그대로 ORIGINAL_OWNER_EVENT_FILTER_NOT_AURA/migration=NONE;새오라나범위보너스없음. crossing은실통로경계/단일측객차소속/동일geometry·observation revision·원권위event순서필수,목적지UI나View좌표로앞당겨갱신0. ongoingEffects는기발사탄귀속·snapshot·궤적·수명/확정HP/부착보호막·HoT대상잔량만료tick보존,원명시거리유지조건예외우선. 오라는원평가tick 수혜집합차분·출입지속리셋/즉시치유복제0,설치형고정/원계약이명시한추종형만추종. 새로운공통tick/수치/추종거리없음. 아직없는오라/HoT를5인kit에새능력으로추가하지않음.
scopeCheckpoint는현재위치/경로revision/객차instance/효과대상잔량만료다음tick/eventsequence 동일경계,복원재획득/과거tick몰아치기/중복적용0·원오프라인정책유지. 기존owner통지로표시갱신하며카드재정렬/장비재조회/표시GET0. 실제effect/state owner를새로만들지않음. runtimeSupported/canStart=false,missing에UNCLASSIFIED_ABILITY_SCOPE와ACTUAL_CROSSING_AND_EFFECT_CONSUMER_BINDING 유지. 새roof-route-invalidation 제안반영0.
검증 QA ordinary-relocation-scope-red-20261026.json 구v1반환RED1→ordinary-relocation-scope-verified-20261026.json36PASS0FAIL0SKIP(START24+departure12),game-api tsc/4파일eslint/scopeddiffcheck exit0. 능력목록대조·분류/미분류/편성불변/원예외·원tick보존,새scope/kit재hash변조거부,과거relocationv1의원scope/hash/초기화replay보존. 독립정적리뷰확정결함0(시험재실행0). 실제crossing/Commit경합/효과실소비·동일checkpoint새process/제품Player/API/3회메모리/Android미검수. WAIT AND미충족으로소비/통합큐선등록0.

<a id="ordinary-relocation-unity-consumer-20261026"></a>
### Unity relocation v1/v2 원본 소비 — 2026-10-26
OrdinaryFiveStartInput.Relocation/OrdinaryRelocationSource가 combat.ordinary-relocation.v1/v2.20261026의원account/run/origin/Basic v3 pin·v2 kit/nested참조/고정정의를검증한다. v2 passiveScopes는원kit배열순서대조후정의정규화. readonly능력분류/retention과미분류4건은그대로보존;미공급역사v1에범위정책자동주입0. 기존Sera factory/제품PendingAccountAction 저장pin에relocationHash결속,응답간원pin대조. 실제이동/action/효과평가/예약checkpoint/목적지UI/제품실행capability완료아님.
근거 Unity인계#unity17-relocation-intake-result-20261026, Relocation72143PASS/0FAIL/별도process시험skip1. 실제source함수+합성원kit fixture이며authenticated START/Player/전체새process이동복원/API/메모리/Android근거와구분. 기존공개입장gate변경0.
<a id="four-skill-scope-server-contract-20261026"></a>
## FOUR-SKILL-SCOPE 서버 원본 v3 — 2026-10-26
44#four-unclassified-skill-scope-approved-20261026/proposal §1–4 구현. ordinaryRelocationSource 신규 combat.ordinary-relocation.v3.20261026 / supportScopeApproval FOUR-SKILL-SCOPE-v1.20261026. producer captureOrdinaryRelocationSourceV3(basic,kit), 기존 departure 동일 START 원본/영수증 저장과 lifecycle compact relocationHash 결속. v1/v2 producer/reader 불변, 진행 START를 v3로 재생산하지 않는다. 기본v3/kit 원account/run/origin/hash 및 nested basic 참조 유지. 새 API/표시 GET/별도 상태 원장 없음.
각 skillScopes: Kyle active SELF 보호막과 CURRENT_CARRIAGE_FRONT_THREATS 도발 분리, body→hit 12000mm/최대2/최소1 수락·거리/원정렬/stableId·후보 고정/Commit 재검사·보충0·전원소실에도 본인 적격 보호막/환급0·부착효과 유지. Reina 두 능력 Commit 실제 권총총구→hit 24000mm와 앞뒤1객차/원LOS 교집합·원표적 generation 고정/대체0. Mira 궁극 매발사 현재총구 24000mm와 op_mira.basic v3/hash 지원영역·원LOS 교집합, 최소1 수락/16발/기존 회전·신규적 다음발/집중변경 다음회전/후보0 남은발 취소 후 회수. 원계수/시간·효과/기발사탄 보존, 기본탄속/폭/피해/충전필터 승계0. 각 checkpointFields는 기존 owner에 필요한 계약이며 실제 checkpoint 구현이 아니다.
unclassifiedAbilityIds=[]이나 runtimeSupported/canStart=false 유지. missing은 REINA_DIRECT_DAMAGE_RECIPIENT_CONTRACT 및 실제 공간/효과/checkpoint·Player 연결을 포함. 현재 retainedKits의 Reina DIRECT_DAMAGE는 계수/CD/W/R만 제공하고 단일/광역 피해집합·개수·분배를 공급하지 않는다. 과거 CE03 tests/fixtures/ce03/combat-stats.json에는 enemy_focus_any와 damage 1효과가 존재하고 CombatSkillCommands.SkillTargets의 해당 경로는 focus/첫 적 1명을 선택한다. 과거 LOCAL 계약 존재와 현재 ordinary 원정의 피해집합 미공급을 구분하며 새 광역 피해안/경로무효화안 미적용.
검증: QA/CommanderS1-20260922/four-skill-scope-red-20261026.json 24PASS/3FAIL(v3 미공급·신규필드 없음) → four-skill-scope-verified-20261026.json 39PASS/0FAIL(ordinary27+departure12). v1/v2 read/activation replay/hash 보존·재해시 scope/range/damage 변조 거부·foreign account·쓰기 전 거절. game-api tsc0/eslint0/diff-check0. 독립 정적 리뷰 중요발견0(별도 시험실행 없음).
Unity 최신 OrdinaryRelocationSource는 v1/v2만 소비하므로 v3 adapter/hash/실제 action·효과 연결이 필요하다. 구소비43PASS를 v3 지원으로 확대하지 않는다. 본 검증은 실제 내부 START producer+메모리DB+인증 Hono app.request이며 TCP/public START/Player/end-to-end 성공을 뜻하지 않는다.

<a id="reina-damage-server-contract-20261026"></a>
## 레이나 피해집합 정식 원본 / relocation v4 — 2026-10-26
44#reina-skill-damage-shape-approved-20261026/proposal §1–4 구현. captureOrdinaryRelocationSourceV4(basic,kit), ordinaryRelocationSource version combat.ordinary-relocation.v4.20261026. 네스킬 scope v3를 유지하고 Reina skillScopes.damageRecipients를 정식 객체로 공급. damageRecipients.approval=REINA-SKILL-DAMAGE-SHAPE-v1.20261026. v1–v3 producer/reader 불변, 새 START 동일 snapshot/원키 receipt만 v4, 기존 lifecycle compact relocationHash 결속/계정run격리. 신규 GET/별도원장/제2피해엔진 없음.
active: 원kit DIRECT_DAMAGE/40000bps 일치 검증 후 참조, maximumTargets=1/additional=null. ultimate: 원kit100000bps 참조, maximumTargets=4, additional.damageBps=30000/maximumTargets=3/radiusMm=3000, PRIMARY_TARGET_HIT_ANCHOR_AT_COMMIT 중심 3차원 hit anchor 거리≤/collider확장0·총구24000mm/physicalAdjacentCars1/각총구LOS·중심고형차폐, 중심거리→원전투정렬→stableId. 추가 미사용피해 재분배0/기본EN17·관통 자동승계0. rank는 추가에도 원궁극 배율, 반경/대상수 자동증가0.
같은 관측revision에서 피해 전에 집합확정/주대상 원identity·generation 고정과 Commit 재검사. 주대상소실 전체불발·대체/환급0, 면역주대상도 적격이면 중심가능/면역대상교체0, 집합확정 뒤 주대상사망에도 추가대상 유지. 원ATK snapshot1회/대상별 원방어·면역·보호막·치명·사망·표식 필터/원시간·소비 유지. 즉시 DIRECT_DAMAGE Commit, FX callback 피해0/새상태·넉백·표식0. checkpoint는 기존 effect owner에 원castID+targetgeneration subevent, 확정revision/집합/공격snapshot/처리progress 보존을 요구하며 재선정/중복피해/결과중복합산 금지. 실제 소비 구현이 아닌 고정 원본 계약이다.
missing에서 REINA_DIRECT_DAMAGE_RECIPIENT_CONTRACT만 해소. runtimeSupported/canStart=false, 실제 공간/효과/checkpoint/Player 남음. 기존 v3의 미공급 기록은 역사계약으로 보존. 새로운 미라궁극 flight 제안 및 경로무효화 미적용, 다른8스킬 scope 불변 시험.
QA/CommanderS1-20260922/reina-damage-red-20261026.json 26PASS3FAIL(v4/필드 미공급)→reina-damage-verified-20261026.json 41PASS0FAIL(ordinary29+departure12). v1–v3 읽기/activation replay/hash보존, 재해시 radius/cap/scope/damage 변조 거절·원응답 인증복구 포함. game-api tsc0/eslint0/diffcheck0, 독립 정적 리뷰 중요발견0(시험 재실행 없음). 대상1~5 실제HP/3m·24m geometry/차폐/Commit중 새process복원은 Unity 실제소비 검증으로 남으며 이 서버 정의시험을 대신 근거로 사용하지 않는다.

<a id="ordinary-scope-v3-v4-unity-consumer-20261026"></a>
### Unity scope v3·레이나recipient v4 소비 — 2026-10-26
OrdinaryRelocationSource가combat.ordinary-relocation.v3/v4.20261026를지원. 원account/run/origin/basic/kit·nested Mira BASIC참조·전체정의pin검증,ability별정의복사반환,기존원START캐시/factory·checkpoint relocationHash결속재사용. v2미분류/v3레이나미공급/v4피해집합을원버전대로유지. 의미실행/대상선정/현재HP피해/공개입장지원승격아님.
근거Unity인계#unity17-scope-v3-v4-result-20261026 및Scope72547PASS/0FAIL/명시process시험skip1. 원정의공급은완료,실geometry/action/효과진행·제품checkpoint/Player/API/3회메모리/Android검수는별도잔여.
<a id="mira-flight-server-contract-20261026"></a>
## 미라 궁극 고정 도착 원본 / relocation v5 — 2026-10-26
44#mira-ultimate-flight-approved-20261026/proposal §1–4 구현. captureOrdinaryRelocationSourceV5(basic,kit), ordinaryRelocationSource combat.ordinary-relocation.v5.20261026. skillScopes[op_mira.ultimate].projectile.approval=MIRA-ULTIMATE-FLIGHT-v1.20261026. 원kit CYCLING_TARGET_BURST/damageBps4000/shots16/interval250/W500/R400 검증·참조. 새 START 동일원본/receipt/hash결속, 역사v1–v4 producer/reader 불변. runtimeSupported/canStart false 유지.
projectile.mode=FIXED_TARGET_DELAYED_ARRIVAL, flightMs120, 원combat step 올림/최소1step, 실제발사tick+올림flight기한. 발사전 현재targeting 범위/LOS·회전 재검사, 발사뒤 physicalCollisionAfterLaunch=false/speedMmPerSecond=null/collisionRadiusMm=null, 새차폐·가로채기·관통·도탄·시각선폭 판정0. 각발 같은살아있는 targetgeneration/동일전장 유지시 거리이탈에도 도착; 사망/퇴장/세대교체 MISS·재표적0. 다운 미발사취소/기발사유지, 종결뒤피해0. 도착때 원면역·방어·보호막·hit/damage/흡혈필터, 발당1명/논리due사건1회·종료기한은impact deadline(BASIC500ms 수명 승계0).
첫실제발사 ATK1회 16발공유/발별원modifier 경계, 공속이 interval/flight를축소0·원clock pause/배속·BASIC충전/EN17승계0. 원마지막발 회수와 진행탄수명 분리/진행탄 때문에 기본공격무기한잠금0. checkpoint 기존 action owner 예약/순번/회전 및 projectile/effect owner 불변shot/impact진행 소유, RUN_CAST_ID_SHOT_INDEX_AND_TARGET_GENERATION 단일사건. 남은impact기한/발사sequence/공격snapshot/회전/처리진행 복원, 120ms·16발 재시작/피해재실행0, 결과불명확 원사건복구·자동재시전환급0, 현재진행탄만표시/지난섬광재생0.
QA/CommanderS1-20260922/mira-flight-red-20261026.json 28PASS3FAIL(v5/flight 미공급)→mira-flight-verified-20261026.json 43PASS0FAIL(ordinary31+departure12). 원key auth app.request/DB불변·v1–v4 read/activation replay/hash·재해시 flight/collision변조 거절·다른9스킬scope 불변/BASIC48000mm/s 유지. game-api tsc0/eslint0/diffcheck0, 독립읽기전용리뷰 중요발견0(시험재실행 없음). 기존ACTIVE120ms source는 수정없음. 실제 step 계산/거리이탈/도착HP/새process 진행탄복원은 Unity 실행검증 잔여; 정의시험을 해당실행근거로 대체하지 않는다.
현재Unity v3/v4 intake와원pin검증은인계상존재(47PASS/1skip), v5 실제궁극도착/회전·checkpoint 소비는미완료. 새로운 이브active 드론 flight 및 경로무효화 미승인안 적용0.

<a id="eve-drone-server-contract-20261026"></a>
## 이브 전투드론 원탄도 / relocation v6 — 2026-10-26
44#eve-active-drone-flight-approved-20261026/proposal §1–4 구현. captureOrdinaryRelocationSourceV6(basic,kit), ordinaryRelocationSource combat.ordinary-relocation.v6.20261026. skillScopes[op_eve.active].projectile 단일정의/ultimate.projectileReference=op_eve.active.projectile. approval EVE-ACTIVE-DRONE-FLIGHT-v1.20261026. 기존 kit active4000bps/800ms/12000ms/최대1/HP5000bps 및 ultimate6000bps/400ms/8000ms/nextScheduledShot PRESERVE 검증·재사용. 새 START만v6/원snapshot·receipt·compact activation hash/원키복구, v1–v5 producer/reader 불변.
실장치총구→원hit anchor 18000mm/48000mm/s/swept sphere r50mm/최대거리18000mm·375ms 원step올림(거리cap확장0). 원객차방어구역/대상유형/LOS 교집합·포대제외, 일반강화 공통 단일비유도탄/기본관통폭발도탄0. 이전→현재sweep 첫고형차폐 종료·첫다른적격적 1회피해후종료/아군비차단. 가로채기 적도 원유형·소속방어구역·발사18000mm영역 경계 만족. 원표적사망시 재유도0/현재직선 다른적격적 가능. 동시접촉은 기존권위collision순서.
생성효과확정 첫기회/기존800·400ms, 합법집중→객차공격실행·예고→원정렬ID. 총구정렬/LOS/타깃부적격은기회소진/몰아쏘기0, 1기회1탄·별도BASIC W/R·기회소비·추가충전0. 생성ATK와발사일반강화계수pin, 비행중강화변경 재계산0/도착원방어·실드·modifier필터. 강화nextShot보존/발사때모드후원간격, 생성포함만료제외/정확만료발사0. 기존장치HP/수명복구0/파괴뒤BASIC대체0.
Eve다운 새발사중지·기발사유지, 장치파괴·만료뒤기발사원수명/전투종결공통정책. Eve+실장치instance/generation+원skill/shotID 귀속, 장치해제후잔탄은필요불변귀속만소유/전체응답참조유지0. 기존 device owner의instance/객차/HP/만료/강화끝/nextShot/ATK/sequence 및 projectile owner의발사위치방향/남은거리시간/계수/귀속/처리progress checkpoint 유지. 새장치/첫사격/강화갱신/중복명중0.
QA/CommanderS1-20260922/eve-drone-red-20261026.json 30PASS3FAIL(v6/새필드미공급)→eve-drone-verified-20261026.json 45PASS0FAIL(ordinary33+departure12). 역사v1–v5 read/activation/hash, 신규드론속도·일정 재해시변조거절, BASIC60000mm/s/r45 및 다른8스킬 불변/원key auth복구. game-api tsc0/eslint0/diffcheck0, 독립정적리뷰 중요발견0(리뷰어실행0).
원정의 공급과 실실행 구분: runtimeSupported/canStart false. 실제 Unity총구/geometry·sweep/HP/사격경계/잔탄새process·Player/APItransport/3회메모리·Android 검수 미실시. 새 five-mixed-hit-integration 제안 및 경로무효화안 적용0. 서버 일반전투 엔진/API/원장 추가0.

<a id="ordinary-flight-v5-v6-unity-consumer-20261026"></a>
## Unity 미라/이브 원정의 V5/V6 소비 — 2026-10-26
OrdinaryRelocationSource가 combat.ordinary-relocation.v5/v6.20261026 원정의를 검증·수용. account/run/origin/basic/kit/nested BASIC 참조와 전체 고정정의 fingerprint를 대조하고 기존 START 캐시/factory·checkpoint relocationHash 결속 재사용. V4 미라 projectile 부재/V5 이브 projectile 부재 보존. 원정의 소비와 실제 발사/도착/장치/충돌 실행은 구분.
근거 Unity 인계 #unity17-flight-v5-v6-result-20261026, QA/OperatorInformation-20261019/Flight727/results.xml 55PASS/0FAIL/명시 별도process 시험skip1. 원정의 공급 대기는 해소; Unity 실제5인 action/HP/geometry·효과·제품 checkpoint/Player/API·3회 메모리/Android 검수는 잔여. 큐 전체 완료 아님.

<a id="five-mixed-finish-boundary-20261026"></a>
## 실제 5인 결과 경계: legacy finish 오수용 수정 — 2026-10-26
FIVE-MIXED-HIT-INTEGRATION-v1.20261026/44 승인 §1–3의 실제 caller 조사. index.ts public /v1/runs/start는 현재quote검사 후 combat_not_ready로 고정차단. ordinary START 원정의 생산은 startDeparture 내부경로, OrdinaryCombatLifecycle.activate의 제품caller는 apps/game-api/src 검색범위 없음. Unity v5/v6 typed 소비55PASS는정의/부분checkpoint 근거이며 전체5인action/HP/탄장치/종결 구현 근거 아님. gate를 clientflag/정의존재로열지않는다.
실제오류 재현: 최신 START producer→PREPARED→인증 app.request POST /v1/runs/:runId/finish(VICTORY/runtimeStateJson={})가 HTTP200/accepted=true/finalResults=1. QA five-mixed-finish-red-detail-20261026.json. 기존 assertClientFinishAuthority가 ORDINARY_SOLO를 모두 legacy로허용하여 최신원source/실전결과 검증경계를누락한것. 실제계정/운영실행이아닌시험DB 재현.
수정은 기존 combat-finish-authority.ts의 원snapshot 판정 1곳: outer/departureResponse의 ordinaryStatSource/ordinaryInitialModifiers/ordinaryKitSource/ordinaryBasicSource/ordinaryRelocationSource 존재면(명시null포함) combat_finish_verification_required 409. PREPARED/RUNNING 초기화 모두 terminal 검증이 아니다. 저장된원본으로만판정/클라이언트flags 무관. 기존 계정검사 뒤·모든결과쓰기 전차단. 역사source부재 ordinary는기존처리, 이미commit된finish의계정별 원키·동일body 영수증replay는기존index상 선행하여유지.
현재최신ordinary 승리/실패/포기 모두 새종료검증 연결 전거절되는의도된제한이다. 이번수정은 정상종료검증구현/전체연결완료가 아니다. 차단해제 선행: Unity 실제단일종결/효과·동일checkpoint 결과원본 및 Server 해당원본의run/source/hash·정합성 검증과기존보상 transaction 연결. 초기화상태/임의client JSON/LOCAL fixture를그증거로대체하지않는다. Server의실START composition/결과verification caller도잔여로유지, Unity전체대기로전가0.
QA five-mixed-finish-verified-20261026.json 46PASS(ordinary34+finishauthority12), five-mixed-finish-compatibility-20261026.json 17PASS(readiness+기존finishsmoke), 합계독립4files63PASS. 양phase·VICTORY/FAILURE/ABANDONED DBbytes불변/결과·보상0, nested/null전필드 downgrade거부, legacy source부재와원키재전송보존. game-api tsc0/eslint0/diffcheck0, 독립정적리뷰 확정결함0(리뷰어실행0). 실제Player/API수송/새프로세스·3회메모리/Android 수행0. app.request는수송시험아님.

<a id="ordinary-initialization-http-contract-20261026"></a>
## 기존 START 초기화 인증 호출 계약 — 2026-10-26 (검증 진행)
POST /v1/runs/:runId/ordinary-initialization, Authorization 기존계정인증, Idempotency-Key 원키(ASCII비공백1–128), body 정확 {}(최대256byte)/query금지. 서버 기존 OrdinaryCombatLifecycle.activate(accountId,runId,{requestId:원키,source:ORIGINAL_START}) 호출. body HP/stats/지원flag/다른source 주입금지. 원계정 START·kit/basic/relocation/장비/원초기modifier로 원initial health/stats 산출, 기존atomic phase PREPARED→RUNNING/receipt 보존.
200 {accepted:true,initializationReceipt:<기존activate반환>,runtimeExecutionVerified:false}. receipt는 runId/phase/health/stats/kit/basic/relocation binding을포함하는 최초초기화영수증. Unity는 최초factory의초기자료로만사용하고 원run/계정/sourcehash와대조해 기존owner에한번반영; 이미살아있는전투/복원checkpoint HP를이초기값으로덮지않는다. runtimeExecutionVerified=false/phase RUNNING은전체5인실행·공개START허가·종료검증이아님. 원키동일body 재요청은동일receipt·쓰기가산0; ACK유실/503 결과불명은원키재시도. 성공후확인GET불필요/화면왕복재요청0. 현재state는기존checkpoint owner가소유.
오류:401/403기존auth,400body/key/query오류,404외부계정/없는run 동일 run_not_found,409원source/phase/다른초기화key충돌,503 ordinary_combat_initialization_outcome_unknown. 기존공개STARTgate·legacyfinish차단그대로. 새명령경로는서버측기존초기화모듈의실caller이며Unity제품호출은별도연결필요. 전체Player검증완료아님.

ordinary-initialization-http-contract 검증 결과 2026-10-26: 구현/인증 route 등록 완료. QA/CommanderS1-20260922/ordinary-initialization-route-red-20261026.json 1FAIL(경로404)→focused1PASS→ordinary-initialization-route-verified-20261026.json 독립4files64PASS0FAIL. 인증401/잘못된키·HP·canStart주입400/없는run와타계정404·초기DBbytes무변경, batch commit뒤ACK유실+동시원키2요청 모두동일receipt200, 원키재전송DBbytes무변경·receipt/stat행각1/다른key409·결과행0. 기존STARTgate/finish차단·historicalfinish·readiness 회귀함께검증. game-api tsc0/eslint0/diffcheck0, 독립읽기전용review 중요발견0(리뷰어시험0).
검증은 내부START로준비한메모리DB와 실제Hono 인증handler app.request이다. publicSTART admission/TCP수송/Cloudflare실환경/Unitycaller/Player 검증으로 확대하지않는다. 초기화 route의 실제서버caller는이제존재하며 기존 'activate제품caller0'은 서버HTTP경계에한해해소, Unity제품요청연결과전체ready는잔여.

<a id="ordinary-initialization-tcp-restart-20261026"></a>
## 초기화 실제 TCP·별도 서버 프로세스 영속복구 검증 — 2026-10-26
기존 ordinary-initialization-http-contract 구현 그대로, 제품코드변경0. tests/helpers/ordinary-initialization-tcp-probe.ts 및 기존 ordinary-start 통합시험에 두 조건 추가. 실제 startDeparture가만든 시험 START DB를D LocalTemp 파일에export → 별도Node프로세스에서 production app.fetch/실인증/127.0.0.1 TCP POST초기화 → commit DB파일저장 → 첫서버프로세스완전종료 → 새PID/동일파일/원키POST재실행. 정상응답/commit후socket응답유실 두조건 모두 같은canonical영수증hash, 새프로세스복구 DBbytes불변·initialization receipt/stat행각1·결과행0.
조건별 logicalInitializationCommands1/실transportPosts2/recoveryPosts1/automaticRetries0/GET0. '정상'조건도후반1POST는프로세스종료후의의도적복구이며평상시확인조회가아님. 응답유실은서버commit/파일저장후소켓종료로유발. 전투checkpoint/Player프로세스가아닌 서버초기화 영수증의 SQL.js 파일복구시험이다. production Hono handler는사용했으나 Cloudflare D1/workerd 내구성·운영인증키/실계정/공개START입장시험은아님.
근거 QA/CommanderS1-20260922/ordinary-initialization-tcp-{normal,lost}-20261026.json:분리PID/원·복구hash/DB행수·무변경/source SHA-256/basic·relocation pin 기록. 토큰·원응답전체수집0. DB/compiled probe는D LocalTemp 시험산출물,실계정자료0. 초기tsx 실행실패는 Windows os.userInfo uv_os_get_passwd ENOMEM에서발생; API오류/실메모리측정으로해석0. 설치esbuild로CJS probe생성후재실행. 첫compiled검사 bytehash불일치는초기JSON순서와저장canonical순서 차이였고 canonicalJson내용비교로교정.
시험 tcp-first2FAIL(실행도구),tcp-compiled2FAIL(원문순서비교),tcp-final2PASS, tcp-suite37PASS/0FAIL. 독립리뷰HTTP대기취소누락P2를AbortSignal.timeout(10000)으로수정, tcp-bounded2PASS재확인/lint0/diffcheck0. 37과반복2를합산하지않음. 기본64PASS의제품route/finish/readiness근거는변경없어재사용; 이번엔제품소스수정0.
현재Unity원코드와대조: PendingRunKind.InitializeOrdinary의동일endpoint/body{}, DurableRunClient.InitializeOrdinaryAsync의원계정·원key·receiptvalidator, OrdinaryInitializationReceipt의accepted/runtimefalse·version/run/account·health↔stats/hash·kit/basic/relocation pin 확인, DepartureRuntime.InitializeOrdinaryCombatAsync의원pendingkey·run/session generation 경계 확인. 해당코드는별도Unity담당변경이며본시험은Unityclient를실행한것이아님. 제품factory/첫실전checkpoint/terminal검증·STARTgate 해제/Player·3회메모리·Android는잔여. 미승인 initialization-first-checkpoint 상세안은적용하지않음.

<a id="ordinary-initialization-unity-client-20261026"></a>
## 초기화 Unity 요청 경로 부분연결 — 2026-10-26
AccountScreenRuntime.InitializeOrdinaryCombatAsync가 보존 START/로컬 run journal을 사용해 DurableRunClient.InitializeOrdinaryAsync로 초기화 POST를 전달. 기존 인증 transport/영속 run 요청에 원key/body{} 보존, 확정 receipt 재사용, 결과불명 동일key 재시도, 계정세대 응답 격리. 초기화만 ASCII 비공백1–128 키. 원run/account/START 정의 pin·필수 stats 구조와health HP 일치 검증 후 확정. 초기화 영수증은 종료/출격허가/currentHP가 아님. 로비 confirmed initialization은 Recovery 유지.
근거 Unity 인계 #unity17-initialization-client-result-20261026 / Initialization734 71PASS. 시험용 transport/journal/합성 receipt 검증이며 실제TCP/Player/디스크 새process 아님. UI/전투 coordinator→전체5인factory·sourceState 검증·HP/효과/checkpoint·terminal 원본/결과 연결,3회메모리/Android는 미완료. 원START gate/legacy finish 차단 유지.

<a id="ordinary-initialization-source-state-20261026"></a>
### 초기화 영수증 원본 상태 공급 — 2026-10-26
Server 담당 추가: 새 ordinary-initialization 영수증의 sourceState는 기존 ordinary_run_stat_states.source_json과 동일한 초기 계산 입력(accountId/runId/revision/inventoryRevision/modifiers/actors)이다. 기존 stateSchema로 검증하여 같은 원자 거래의 영수증에 동봉하며 stats.sourceHash=SHA256(canonical sourceState). 새 API/GET/DB 원장 없음. 기존 v1 receipt 내부 additive 필드이며 과거 발급 영수증은 필드가 없어도 원바이트 의미 그대로 재전송; 현재 상태로 보충/재계산하지 않는다.
Unity OrdinaryFiveCoreStatSnapshot.Read(packet,sourceState,trustedPacketHash,account,run,origin)의 원본 검증 입력 누락을 해소한 공급 변경이다. 현재 Unity receipt 검증기는 내부 추가 필드를 허용하지만 실제 sourceState 소비/첫 durable checkpoint/5인 factory 연결은 Unity 소유 미완료다. sourceState는 초기값 증거이며 현재 HP나 손상 저장 복구 허가가 아니다. 필드 없는 역사 영수증에서 원본을 추측하지 말고 기존 복구 경계 유지. 정상 성공 후 확인 GET0, 불명확 원키 재전송, 원계정/run/hash 검증 유지.
검증: QA/CommanderS1-20260922/initialization-source-state-red-20261026.json 누락1FAIL → green 37PASS(실 TCP 정상/응답유실 서버 재시작 포함). 추가 역사 영수증 시험은 처음 fixture UPDATE가 불변 trigger에 거절됨(historical JSON); disposable fixture에서 구형 바이트를 seed하고 trigger 복구 후 final JSON 2PASS. 최종 schema parse 변경 후 focused 2PASS, game-api tsc/eslint/scoped diff-check exit0. 역사 원영수증 재전송은 손상된 후속 state도 쓰기/초기화하지 않음을 DB bytes로 확인. 전체37 재실행 합산0. 실제 Unity Player/디스크 첫 checkpoint/메모리 미검수. public START readiness·실 종료 source 검증/기존 finish 차단은 유지한다. 새5인부분구성 실패안은 사용자 승인 전달(44#five-actor-staging-failure-approved-20261026), 준비중 로비복귀 proposal은 미승인.
<a id="first-battle-encounter-source-contract-20261026"></a>
### 첫 전투 학습배치 원본 경계 v1 — 2026-10-26
승인44#first-battle-encounter-source-approved-20261026. 새 해당 ordinary START의 firstBattleEncounterSource={packet,sourceHash}; packet.version=combat.first-battle-encounter-source.v1.20261026, sourceHash=SHA256(canonical packet). 기존 START 거래/응답/저장에만 동봉, 별도 GET/DB/단계 API 없음. packet은 검증된 ordinaryStatSource의 accountId/runId/originHash/formationRevision에 결속; 편성 전체 복제/신규 지급 없음.
현재 v1은 명시적 비실행 계약: status=DEFINITION_NOT_ISSUED, eligibility=NOT_EVALUATED, definition=null, canStart=false, runtimeSupported=false. sequence는 SINGLE_APPROACH→SINGLE_RAIL_ARTILLERY→INDEPENDENT_BRANCH→ONE_SIDE_BOARDING→TWO_THREATS. references.enemies/artillery/rail/branch/threatBudget/entryConditions/resolutionConditions/eligibilityPolicy/missedTriggerPolicy는 모두 null(미공급)이며 빈 적목록·적격·단계완료로 해석 금지. 이것은 기존 ordinary 출격을 첫학습 대상으로 선정한 근거가 아니다. 실제 발행 definition ID/version/내용 및 그 schema는 승인 참조/수치/맵정책이 준비된 후 별도 버전에서 발행한다. 이 상태 계약 완료를 실행 정의 schema/시나리오 완료로 주장하지 않는다.
progressOwner=EXISTING_ENCOUNTER_CHECKPOINT, progressContents는 CURRENT_STAGE/ISSUED_THREAT_IDENTITIES_AND_STATE/ORIGINAL_TIMERS/BRANCH_SELECTION 의미 목록이다. 새 checkpoint wire schema를 발명하지 않으며 기존 Unity owner의 저장 계약으로 결속한다. 진행 중 원본 보존/단계 재발행0/정상 단계GET0, 단계해소≠승리. 업데이트는 새 출격부터.
readFirstBattleEncounterFromOriginal은 기존 원출격과 정확한 v1 계약을 대조해 해시를 다시 만든 임의 eligibility/참조/버전/scope 변경도409 first_battle_source_invalid로 거절. 속성 부재는 역사출격으로 null 반환, 명시null/불일치는 역사로 강등하지 않는다. 초기화는 원영수증 replay 이후 신규 초기화 전에 이 reader를 호출한다. 이미 확정한 원초기화 영수증의 재전송 보존. finish 기존 ordinary source presence guard에 추가해 단독/중첩null도 legacy 우회불가.
Unity 소비는 이 버전의 미발행 상태와 원scope를 인식하는 좁은 범위다. 실제5인 제품/terminal 우선 유지, dynamic segment 연장안 미승인 적용0. 실제 적격/배치/geometry/최저최고속 창·Player 복원 검수는 미완료.
<a id="first-battle-segment-server-boundary-20261026"></a>
### 단계별 구간 연결 승인 반영 / v1 불변 유지 — 2026-10-26
44#first-battle-segment-spacing-approved-20261026 및 proposal1~5/검수 확인. 단계별구간 방식은 이제 승인이나 실행맵 지원/유한연장 값 승인은 아니다. 이미 발행한 combat.first-battle-encounter-source.v1.20261026의 정확바이트 의미/reader는 수정하지 않는다. 기존 v1 DEFINITION_NOT_ISSUED와 missingTrigger 참조null은 그대로 유효하며 실행정의 발행으로 승격0. 이후 실행버전에서 승인조건/확정값/원맵 참조를 묶고 새출격에만 적용한다.
Unity 인계의 Unity17 학습 배치 소비 경로·소유권 확인 (2026-10-26) 절에서 지목한 기존 소유: BasicCombatSimulation 웨이브, CombatRailJourney 경계/offer/거리/분기, CombatRailArtillery 기발사탄, CombatLocalCheckpoint rail/clock/RNG/위협 저장. 해당 기록은 ce03-rail-headless-state-v1 경로 조사이며 신규제품 구간결정/공개경계 지원 증거가 아님. Server가 그 명칭만으로 지원을 가정하지 않는다.
맞춰야 할 경계: (1) 기존 맵owner가 공개·확정 전 전방구간 결정 가능 여부와 이후 불변성을 증명 (2) 기존 checkpoint가 확정구간ID/원정의버전/결정위치·진행/기발행위협을 한 원상태로 복원 (3) 실제 최고속/가속·전체점유길이로 다음 예고 대응거리 검수 (4) Plan 확정 유한연장 범위/최종교전구간과 기존 종료조건 참조 제공. 이들은 의미 요구사항이며 새 필드명/거리/시간/무제한 연장을 발행한 것이 아니다. 정상 단계GET0/기존출격정의 재사용, 공개지형 이동·기발사탄 삭제·가짜 제동·반복추첨0.
이번은 승인 경계 문서 반영이며 코드 변경/시험 실행0. 직전 비실행 상태계약64PASS 근거 재사용, 실제구간/회피/Player 검수 아님. 전체실행/통합 후속키 선등록0. 실제5인terminal 우선/해당 원본 공급후 기존 결과transaction 연결 Server 책임 유지. first-battle-completion-retry-proposal 미승인·경제변경0.
<a id="first-battle-completion-retry-boundary-20261026"></a>
### 학습 완료·재도전 경계 검증 — 부분완료
44#first-battle-completion-retry-approved-20261026 및 proposal1~6/검수 확인. 현재 firstBattleEncounterSource v1은 DEFINITION_NOT_ISSUED/NOT_EVALUATED이고 필수단계 원정의/실checkpoint producer 및 ordinary terminal validator가 없다. 새학습완료 필드/쓰기/원장 발행0. 필수단계진행과 기존 권위승리를 모두검증한 뒤 기존 결과·진행 owner에 반영한다는 승인조건만 유지한다. 안내skip/무궁극기/피격/다른유효대응을 새실패조건으로 사용하지 않으며 기존승패를 학습미완료 때문에 변경하지 않는다.
실제소스 확인: index.ts 결과 영수증/replay와 finishStatements가 원결과/진행/보상을 소유하고 objective-events.ts objectiveFinishEvent는 원출격과 기존 B map/progress를 검증한다. B노드진행을 학습단계로 재명명하지 않는다. 새 단계 producer 공급후 그 원정의·checkpoint/종료원기록을 scope/version/CAS로 검증해 기존 거래에 연결하는 Server 구현은 잔여다. 실제재도전은 기존 출발 적격/소유/유효 quote/연료비용 검증을 그대로 재사용해야 하며 도움말 표시에는 출격·보상 writer를 연결하지 않는다. 첫학습전투의 재도전 실행지원은 미확인, 무료모드/새권한/재화정책 없음.
추가 API 회귀시험: tests/integration/combat-ordinary-start-source.test.ts의 does not promote claimed learning... . 실제 내부 원START/초기화 후 클라이언트가 미경험 또는 모든단계완료, 안내skip/무궁극기/피격·VICTORY를 runtimeStateJson에 주장해도 둘다409 combat_finish_verification_required, DB bytes 전체무변경/결과0/보상preview0. 이 payload는 공격적 시험fixture이며 정식checkpoint schema가 아니다. QA/CommanderS1-20260922/first-battle-completion-boundary-20261026.json 1PASS/0FAIL; ESLint/diff-check exit0. 기존64PASS·출발비용/권한 회귀 재사용, production코드 변경0. 이 거부시험은 진짜 권위승리 후 skip/피격 허용이나 실제완료·재도전 Player 검증이 아니다. 실제제품/정상3·복구3/API메모리 미완료.
SERVER큐 유지/완료쓰기·전체후속등록0. 신규 참가조건제안 미승인 적용0. 기존 v1 불변/원키응답재사용/추가GET0·계정run격리 유지.
<a id="first-battle-entry-v2-contract-20261026"></a>
### 첫 학습 참가조건 v2 상태계약
승인44#first-battle-entry-requirements-approved-20261026. 신규 ordinary START의 firstBattleEncounterSource.packet.version=combat.first-battle-encounter-source.v2.20261026. v1 producer/reader 불변·버전별dispatch/원run복원 유지. v2는 기존source/account/run/origin/formation 결속과 동일 hash방식을 사용한다.
entryRequirements.approval=FIRST-BATTLE-ENTRY-REQUIREMENTS-v1.20261026. required=[SPEED_CONTROL,BRANCH_SELECTION,CONNECTED_ENCOUNTER_DEFINITION,REQUIRED_ASSETS], optional=[SHIELD_USE,TRAIN_FIRE_USE,ULTIMATE_USE], optionalUnlockOrChargeRequired=false, additionalOperatorRequirements=[]. 기존일반 출격의 실제보유/계정진행/비용/지원roster를 유지한다. 일시쿨다운은 기존준비·실행계약으로 구분, 선택기능 없이 실제속력/경로대응 최소1개 검수 필요. 미발행/제품미준비는 편성 잘못으로 분류하지 않는다. 신규지급/대여/해금/충전0, 추가preview GET0.
실제 기능해금/실행geometry 증거를 이번 DTO로 대신하지 않는다. status=DEFINITION_NOT_ISSUED,eligibility=NOT_EVALUATED,definition/null참조,canStart=false/runtimeSupported=false 유지. missing의 FIRST_ACCOUNT_ELIGIBILITY_AND_UNLOCK_POLICY는 v2에서 ACTUAL_FIRST_ACCOUNT_ADMISSION_AND_REQUIRED_FEATURE_EXECUTION_PROOF로 구체화(정책구분 승인과실계정검증 분리). 실제 실행 definition/적격 evaluator는 원천 미공급으로 미완료. 새밸런스/난이도검수제안 적용0.
<a id="first-playable-initialization-wire-source-20261026"></a>
### 첫 실행본 실제 초기화 응답 공급 / 부분진행
44#first-battle-playable-slice-approved-20261026 원proposal/수용 확인. 상태DTO 추가0/지원flag변경0. Unity 현재 Run/OrdinaryInitializationReceipt.Validate가 receipt.sourceState를 OrdinaryFiveCoreStatSnapshot.Read로 실제 전달하는 변경을 소스에서 확인(실행검증은아님). 서버 실제init HTTP와 소비자료를 연결했다.
시험 producer combat-ordinary-start-source.test.ts의 기존 실제 내부 startDeparture callback에 표준 startStatus:STARTED를 추가하고, 인증 original-key 초기화/동시요청/ACK유실/replay 검증 후 원START와 실제응답 그대로 QA/CommanderS1-20260922/ordinary-initialization-consumer-source-20261026.json으로 내보낸다. JSON 57405bytes, account=a/원runId·5actors·sourceState revision1·원hash/Server sourcefile SHA256 포함. 원키성공POST3/정상확인GET0(다른거절시험요청을총호출수에서숨긴값아님, successfulOriginalKeyPosts명시). auth토큰/secret없음. classification=SYNTHETIC_ACCOUNT_INTERNAL_START_PRODUCTION_INITIALIZATION_HTTP_NOT_PRODUCT_ADMISSION; productAdmissionVerified/unityConsumerVerified/playerVerified=false. 모의성장/보유fixture의 실제서버코드/HTTP응답이며 공개START 허용·실제품Player자료로 승격0. 기존실TCP근거는별도재사용, 이번app.request는in-processHTTP.
검증 consumer-source-verified JSON1PASS 후 횟수필드명을명확히한 consumer-source-final JSON1PASS, 최종eslint/diff0. 두실행합산0. Unity 소유production파일수정0, 실제소비·5인factory/Player 실행은미완료. 새엔진/가짜승리/보상0.
정확대기입력(새wire필드명아님): Plan 실행배치 원정의의 첫적 종류/원budget·접근/발행조건, 단일포대/선로참조와 원예고/발사 조건·수치(현재 first-bombardment-timing-proposal 미승인); Unity 실제맵/열차점유geometry·노즐/행동/HP/탄·5인제품factory 및 최종checkpoint/종료사건 producer; Server 실제계정 신규획득·해금 적격 증거와 위승인원정의producer→원START결속 및 terminal원본검증→기존거래 연결. 제품START의 readiness는이AND충족전해제하지않음. 본원응답공급을실행본완료로주장0, 부모큐유지·전체후속등록0.
<a id="first-playable-numeric-profile-contract-20261027"></a>
## 첫 배치 정식 수치 profile·START v3
정책44#first-playable-numeric-baseline-20261027. 원파일 server/apps/game-api/src/combat-first-playable-profile.ts firstPlayableProfile(). profileId=first_battle_forward_gunners_single_artillery, version=combat.first-playable-profile.v1.20261027, definitionHash=e890b2bdbbf388048d8a2c77d82eb4ecca37c60b4883cf6317a7aaf0a7c1a1ae (definitionHash 제외 canonicalJson의 SHA256). 기존 gunner 원정의/typeHash·원rawAction·공간/탄도 참조를 포함하고 학습override count2/baseHpEach240/baseHpTotal480/DEF53을 분리. 혼합B allocator 호출/수정0. 포대 appearanceRef=rail_artillery_emplacement_v1, 별도 SINGLE hazardVersion=rail.artillery-hazard.v1.20261023, baseHP1000/rawDamageIntent50·사건당1회/객차별피해false, 위험길이6000mm, preview기준 launch7500/impact8000/flight500ms. 잔여위협 모두해소 후 최소2000ms; 원5인/직선/clock/실회피geometry 선행. 발사전파괴 취소·후파괴 기발사유지, 원same-tick순서. 실제 사건/적instance 생성과 현재HP 원장은 소비 실행owner 소유.
scaling은 기존 combat.b-operation-risk.v1.20261003 hash/정책과 정확유리수 15개 risk/progress variants를 공급한다. HP는 risk만1회, raw피해는 risk/progress각1회, 방어·보호막 이전값이며 최종HP손실 아님. I/EARLY=각gunnerHP240/raw20·포대HP1000/raw50; V/LATE=552/42·2300/105. II/MIDDLE raw25.3/63.25도 조기floor0. 각variant multiplier와 결과를 함께 제공, consumer는 이미배율적용된 결과에 다시배율을곱하지 않는다. 실제선택/적용은 appliedScaling:null이며 run 결속완료가 아니다. 기존 readRiskRunBinding은 12node 실제visitDepth로 band를결정하며 0/12는null, 15wave지원안됨; 그 LOCAL fixture를 운영run증명으로사용0.
신규ordinary START firstBattleEncounterSource v3.20261027 definition에 위profile 포함, status=PLACEMENT_PROFILE_ISSUED_RUNTIME_UNBOUND. account/run/originHash/formationRevision 및 전체packet sourceHash 결속. readFirstBattleEncounterFromOriginal은 원version 재구성/전체canonical비교로 내용변조거절. v1/v2는 그대로복원, source없는역사run backfill0. 첫2배치 정의발행이며 전체5단계definition/eligibility/geometry/actualrun배율 미완료; canStart/runtimeSupported false 유지. 기존 authenticated initialization이v3를검증하고 원receipt/동시성/복구계약유지; finish guard유지, 가짜단계완료로결과/보상저장0. 새GET/실지급/배포0.
QA first-playable-numeric-green-20261027.json: ordinary40+departure12=52PASS; 추가정밀도/hash/원raw/65536byte한계 시험 final.json 1PASS(52에중복합산0). type/lint/diff exit0, 읽기전용리뷰 구체결함0. 기존 ordinary-initialization-consumer-source-20261026.json은 이번 원START v3/실production handler 초기화응답으로 갱신, compact START62164bytes. SYNTHETIC_ACCOUNT/INTERNAL_START 분류보존, Unity 실소비/제품입장/Player는미검증. AGENTS.md ‘기존 정보 재사용·데이터 갱신’ 기준 원START/receipt재사용·정상후GET0·원키복구·계정run/버전격리 유지. 실제운영rail/geometry·가감속/위험A/적열차속도/총구 및 원진행결속·전체terminal 잔여.
<a id="first-playable-initialization-binding-20261027"></a>
## 실제 초기화 영수증의 학습 원본 결속
기존 authenticated POST /v1/runs/:runId/ordinary-initialization의 동일 HP/stat/phase/receipt batch에서 firstBattleEncounterBinding={version,originHash,sourceHash}를 저장·반환한다. 값은 기존 readFirstBattleEncounterFromOriginal이 원START account/run/origin/hash 및 정의전체를검증한 결과만사용. sourceHash는 numeric profile/definitionHash를포함한전체packet을결속하므로 원문/별도profile 복사없음. 최초 초기화에서 변조된학습원본은쓰기전거절; 원키/응답유실/동시요청은저장된동일영수증복구, 정상직후GET0. v1/v2도원source있는신규초기화라면그버전binding을반환하며, 기존receipt는필드없어도그대로반환/backfill0. source없는역사START는새binding생성0. 초기화contractversion변경없이additive필드, 현재HP복구나배율/실행/terminal검증증거아님.
Unity OrdinaryInitializationReceipt.Validate의현재필드읽기는additive호환이나firstBattle typed검증은아직없다. 기존 numeric-consume 작업에서 원START version/originHash/sourceHash와영수증binding의동등검증을연결해야한다. 원receipt영속저장/복구경로재사용, 최신source로과거영수증보충금지. 기존 export ordinary-initialization-consumer-source-20261026.json 갱신; 과거Unity755 검증hash는이번export의검증근거가아님.
QA/CommanderS1-20260922/first-playable-initialization-binding-red-20261027.json: missing binding1FAIL. green40PASS(원키동시성/ACK·TCP/역사receipt·기존finish차단포함), final1PASS(추가학습포격횟수변조거절/DB무변경·source없는역사). 서로다른총41시험, game-api tsc/변경2파일lint/diff0. 배율선택·새진행정책적용0; 실제Unity/Player/terminal증거미공급을대체하는가짜결과consumer 없음.
<a id="first-playable-progress-scaling-contract-20261027"></a>
## 실제 도착 거래의 학습 진행 배율 결속
승인44#first-playable-progress-scaling-approved-20261027 / FIRST-PLAYABLE-PROGRESS-SCALING-v1.20261027. 신규START firstBattleEncounterSource v4.20261027에 원selectedRisk와 scalingPolicy 결속, response.risk도 원preview risk에서 공급. numeric profile v1/hash는불변. 기존 v1/v2/v3 생산/읽기/receipt는보존·도착시역주입0. START appliedScaling:null은 아직 encounter선택전임을뜻한다. 현재 ordinary stat 공급의riskI 제한/공개입장gate는변경하지않음.
실제호출: OperationMapStore.commit ARRIVE→기존 verifier.prepare proof+assertions→transitionMap ACTIVE→prepareFirstPlayableScaling→기존 state/revision+operation_map_receipts 동시batch. 새API/테이블/별도진행원장/매단계GET없음. CONFIRM에는결속없음. 실제node.encounterProfileRef가 first_battle_forward_gunners_single_artillery와일치하고 published content의version/전체definition이원STARTprofile과동일한 COMBAT depth1..11에서만선택. 비대상node는기존처리, depth0/이동중/보스12/15wave를임의EARLY로변환0. 출격profile목록존재만으로모든COMBAT를학습배치로치환하지않음.
새 map receipt optional firstPlayableScaling: version=combat.first-playable-scaling.v1.20261027, accountId/runId/encounterId(원ARRIVE eventId), nodeId/visitDepth/progressRevision/확정후progressStateHash, startSnapshotHash/initializationResponseHash/sourceHash, profileId/profileVersion/profileHash, riskDefinitionVersion/riskDefinitionHash, applied={risk,progress,hpMultiplier,riskDamageMultiplier,progressDamageMultiplier,gunnerHp,launcherHp,gunnerRawDamage,artilleryRawDamage}, hpRiskApplications=1/damageRiskApplications=1/damageProgressApplications=1, bindingHash(자기필드제외 canonical SHA256). exact값은{numerator,denominator}. HP=위험만, raw=위험·진행각1회, 방어/실드/최종반올림전. 이미계산된applied를consumer가다시곱하지않음.
권위: 원account/run immutableSTART 및source reader, 해당preview CONSUMED/account/risk/objective/balance/admission, 원초기화RUNNING/동일origin/원sourcebinding·responseHash, 원publication과확정node를읽음. 기존map CAS에preview/risk·초기화receipt/phase 조건도같은DBbatch assertion으로결속한다. 거래전risk변경/stale진행/종료/계정회수는기존guard거절. 원키replay는재계산전에저장receipt반환, 후속node진행해도old band변경0. 새성공응답자체가factory전최종source; 초기화receipt만으로전투적HP/포대를생성하지않음. 두사수와그뒤포격은같은encounterId/bindingHash를checkpoint에보존, 포격단계가별도node도착을호출하지않음.
QA: first-playable-scaling-red 1FAIL(미구현), green은초기snapshot risk미공급+TypeScript narrowing문제를확인해수정, green2 2PASS. final.json 관련4파일80PASS1FAIL(과거fixture가불변map snapshot UPDATE를시도), fixture를별도과거state seed로수정한 boundaries-final1PASS: 고유81시험통과범위이며한번의81PASS실행이라고주장하지않음. depth1..11/4→5/8→9·II 소수raw253/10·138/5, ACK유실원receipt, 위험도변경CAS거절/DB진행무변경, oldreceipt band유지, boss비대상/0/15wave거절·v3역주입0. type/lint/diff0, 읽기전용리뷰중요결함0. 원시나리오는내부START/SQL fixture·합성도착proof, 제품도착/Unity/Player증거아님. 기존init export는v4/riskI compact62471bytes로갱신.
제품잔여: 실제eligible learning node를공급하는실운영map/catalog/geometry·도착증거/실행handler, Unity typed v4/arrival receipt소비→원HP/action/포격/동일checkpoint, 실제terminal 검증/결과거래/Player. 새rail-impact-health-commit 상세안미적용. AGENTS.md ‘기존정보재사용·데이터갱신’ 원START/도착응답/원키복구/단일owner·현재HP보존/세대격리/추가GET0 유지.
<a id="rail-exact-hazard-contract-20261027"></a>
## 포격 정확유리수 원본·복원 계약 v2 / 부분 공급
RAIL-IMPACT-HEALTH-COMMIT 승인 및 UNITY-PROGRESS-RATIONAL-BOUNDARY-20261027 대응. server/apps/game-api/src/rail-artillery-hazard.ts ExactRailArtilleryHazard: rail.artillery-hazard.v2.20261027. v1의정수damage/숫자rawDamageIntent·SINGLE/RESIDUAL·snapshot복원은동결호환. 공통 RailHazard collision/시간/전체객차/receipt kernel 재사용, 새전투엔진없음. v2는 SINGLE만허용하며 definition.damage={numerator,denominator} 양수기약분수/문자열/기존ExactCombatValue 범위. 정수JSON·소수JSON·0·분모0·비기약분수거절. 나머지 definition/frame 필드·폐구간교차·정확시각·전체객차/중복span·run/train 계약은v1동일.
v2 result.version/ snapshot.version/definition.version 모두v2, rawDamageIntent만 exact fraction: HIT원damage1개, MISS {numerator:'0',denominator:'1'}. 여러객차/한객차splitspan중첩에도raw가산0. 기존scope·impactIndex·outcome·intersectingCarriageIds 보존. 같은관측replay는원result, 다른관측/잘못된시각거절. snapshot은독립사본, 정의damage와receipt raw의별칭도분리. restore는0~1개원영수증을원collision/time순서로재실행·정확결과비교; 변조거절. self-consistent snapshot은권위증거가아니며 실제checkpoint소비자는원발행definition/source와별도비교해야한다. replay result반환은HP재적용허가아님.
기존 resolveNumericDefenseBudget.base에raw fraction을그대로전달가능. II/MIDDLE 253/4→시험DEF1000의253/8→기존ordinaryBeforeShield31, 방어전63절삭없음. 방어/실드/실HP·무적·종료owner를hazard가소유하지않는다. 현재 CombatHealthStore 실제피해변경은권위verifier필수이며 임의client HP setter로공개되지않음; 이번소수계약을별도서버HP/실드원장으로변환0.
신규firstPlayableProfileV2: combat.first-playable-profile.v2.20261027/hash d8db615dfb8beef8aaca82a76544d3e2427a52970da15df9166f74af784ea4f3, 같은profileId first_battle_forward_gunners_single_artillery. 원base/15배율variant·시각/길이규칙은불변이고 artillery.hazardVersion만v2로연결. 신규START firstBattleEncounterSource v5.20261027이profilev2/참조를포함, v1~v4 producer/reader는그대로복원. 검증ARRIVE scaling은v4/v5 둘다각자원profile 전체/hash를대조하여기존CAS/원receipt로저장. oldv4/profilev1의v1hazard를오늘v2로역주입0.
QA rail-exact-hazard-red 9PASS2FAIL(새owner부재), green2 11PASS. 최종 rail-exact-source-final-20261027.json 53PASS0FAIL(ordinary42+hazard11), tsc/lint/diff0. 중간수정: 정확raw와definition 객체참조공유를분리해receipt변조가원definition을같이바꾸지않게했고, refined schema의incompatible safeExtend 타입오류를공통fields/window검증재사용으로수정. 읽기전용리뷰중요결함0(리뷰어시험실행/이전Git바이트대조없음). init export v5/profilev2 compact62472bytes 갱신, 과거Unity검수hash를신파일검수로승격0.
잔여: Unity typed v5/profilev2/exacthazard 소비→기존 실train damage/HP/실드와hazard해결 같은checkpoint, 중복피해0/부분반영정상저장0/HP0 terminal1회·실제원본을서버저장복원/결과검증경계에연결. 현재실checkpoint/terminal 원본미공급으로이제품통합은미완료. 실제rail/가감속/5인·새process/Player/API/메모리도미검수. 다음포대파괴/발사상세미승인안적용0, 기존AGENTS원응답재사용/단일owner·원키/역사호환·추가GET0.
<a id="rail-original-restore-train-source-20261027"></a>
## 원발사 잔탄 복원 대조·실보유 열차 원본 공급 — 부분 연결
ARTILLERY-LAUNCH-DESTRUCTION-RESTORE 승인. ExactRailArtilleryHazard.restoreBound(snapshot, originalDefinition)는 caller가제출checkpoint와별도로보유한 원발사definition 전체를검증/동등대조하고 기존v2 restore의시간/receipt재실행 검증을수행한다. run/event/train/좌표/시각/배율적용damage가달라도자체일관된snapshot을원위협으로대체할수없다. 기존 restore(input)는역사계약대로유지되며자기일관성검증일뿐권위증거아님. 새bound함수도원정의발행권위/최신checkpoint revision/소비여부/HP차감원자성을대신하지않음. 아직launcher/checkpoint/terminal 제품caller 미공급으로해당서버실caller 연결완료주장0. wire버전추가/발사상태DTO/매피격API없음.
별도실행원본 연결: 기존 readOwnedTrainCombatProfile·captureOwnedTrainBaseDefinitions에 train 성장/능력치원천이있다. calculateOwnedTrainPermanentStats(level)로원계산을한곳에두고기존intake와 START preparation이재사용. 신규파일 combat-ordinary-train-source.ts / packet.version=combat.ordinary-train-source.v1.20261027. 원START ordinaryTrainSource={packet,sourceHash}; packet은 accountId/runId/ordinaryOriginHash/ownedSourceHash/loadoutRevision, train={trainId,level,revision}, definitionVersion/definitionHash, permanentStats={maxHp,attack,defense,basicIntervalMs}. definition combat.owned-train-base.2026-10-01.1/hash 8bdf54659581a228e5a2846028a9c56ee31f975cd5e18927bcf071dc9d73245c. 원baseHP5000/ATK60/DEF111/주기1000ms, 기존레벨HP8%·출력6%와floor재사용(새밸런스아님).
prepareOrdinaryFiveStatOrigin이이미읽은 owned.profile.selectedTrain/원ownedSourceHash를재사용해별도조회없이capture, 동일 owned assertions/CAS로START저장. operator origin의동결본문/version/hash는변경하지않음. 초기화 readOrdinaryTrainFromOriginal이계정/run/원operatororigin/ownedHash/loadout·정의/영구능력치/hash를대조하고동일원자receipt ordinaryTrainBinding={version,ordinaryOriginHash,sourceHash}로결속. 현재계정레벨변경으로run재계산0, 원키복구영수증보존/과거field부재null·backfill0. 원source없고명시null/불일치면거절. 새train source만존재해도legacyfinish에대한실행증명으로승격하지않고기존guard거절.
permanentStats는현재HP/실드/일시효과적용후수치가아니다. 초기화 health targets는여전히5요원이며 sixth target/초기trainHP writer를이번에추가하지않음. 이부족은Server 원초기상태/Unity실train owner 연결잔여이며전체원천부재나모두Unity외부대기로전가하지않는다. 기존입장/지원false보존·실train 생성/factory caller·shield owner·동일checkpoint·HP0terminal1회·새process Player는미완료.
QA rail-original-restore-final12PASS(6종원정의변조·미해결/해결원receipt복원 포함). ordinary-train-source-final68PASS(ordinary43/hazard12/finish13), 추가 ordinary-train-regression19PASS(최신finish14/기존ownedtrain5), 중복13합산0. dtype/lint/diff0·읽기전용리뷰중요결함0/리뷰어실행0. 새source는실내부START/초기화HTTP·동시성/ACK복구와원계정성장후불변검증, 제품입장/Player증거아님. 기존export갱신compactSTART63184bytes, 이전Unity766 c13a... hash근거를이번파일로재사용0. AGENTS.md 원정보재사용/단일owner/원키복구·계정run·역사호환/추가GET0.
<a id="train-initial-health-contract-20261027"></a>
## TRAIN-INITIAL-HEALTH-RESUME 초기 거래 계약 — 2026-10-27
44#train-initial-health-resume-approved-20261027 적용. 새 ordinary START는 ordinaryTrainSource.packet.version=combat.ordinary-train-source.v2.20261027. 기존v1 필드/영구능력치에 initialization={policyVersion:TRAIN-INITIAL-HEALTH-RESUME-v1.20261027,departureEffects:NO_ACTIVE_DEPARTURE_EFFECTS}를 추가하고 전체 sourceHash 재계산. v1 producer/reader 및 필드없는 역사 START 보존, 새동적상태 역주입0. 현재 허용 ordinary 경계는 commander ranks 차단·연구 active catalog 없음·장비 run-owned(operator/turret만)·초기화거래 전 augment history/grants/originals 부재 CAS. 기본열차/차량장치 출력은 출격시 실드로 바꾸지 않음. 실제 출격효과 추가는 해당 원본/eligibility/version을 구현해야 하며 임의 신규 effect 목록/재hash source는 검증거절.
실제 POST ordinary-initialization ORIGINAL_START→OrdinaryCombatLifecycle→prepareOrdinaryInitialHealthSource→기존 CombatHealthStore의 동일 combat_damage_states 행에 5요원+trainId 총6 HP target 저장. train hp=maxHp=원START train permanentStats.maxHp는 위 효과부재 경계의 서버확정 유효초기값. UI의 복사/독립 HP 원장/새테이블/후속GET/매피격API 아님. 실제 열차ID가 요원ID와 충돌하면 거절. 같은 기존 phase PREPARED1→INITIALIZING2→RUNNING3/5인stats/HP/activation receipt 원자거래. 새v2에서 수동 initialTargets 명령은 ordinary_combat_original_initialization_required로 쓰기전 거절; 과거 테스트/계약만 기존 verifier 경로 유지.
기존 initializationReceipt.version은 유지, 선택필드 trainInitialState 추가: version=combat.ordinary-train-initial-state.v1.20261027,accountId,runId,trainId,ordinaryOriginHash,sourceHash,healthTargetId=trainId,healthRevision=1,stage=INITIAL_BEFORE_COMBAT,logicalTimeMs=0,activeShields=[]. HP 수치/최대HP·rebase anchor는 같은 receipt.health.state.targets의 해당 target이 소유하며 trainInitialState에 복제0. sourceHash는 ordinaryTrainBinding/source와 동일, 전체 원response_json/hash가 같은 거래에 불변저장. logicalTimeMs=0은 최초 전투 전 원점이며 step/선로속도/서버epoch를 발명하지 않음; 현재실드는 빈목록, 지속시간/효과 생성 없음.
재전송은 원영수증 그대로 반환하며 현재 HP를 다시 초기화하지 않음. 첫조회보다 replay가 먼저여서 역사영수증 필드복구/재구성0. 다른키는 phase충돌. 강화/교체 후에도 원START 값 유지. 진행HP/실드/효과시각은 검증된 진행checkpoint가 우선: initial receipt/healthRevision1을 다음node·이어가기에 덮어쓰기 금지. 소비자는 account/run/train/source/healthTarget/revision 및 초기시계와 첫checkpoint 결속을 검증해야 하며 필드없음을 0/만피로 보완하지 않음.
QA train-initial-health-reviewed-20261027.json 55PASS0FAIL(ordinary source48/stat7), tsc/lint 및 변경파일 diff-check0. 원HTTP/동시원키/ACK유실, 별도process TCP 정상·응답유실, 원키불변, 실제열차 초기행, 실패시 전체롤백, 강화후원값, v1/누락역사, 재hash 미지원effect거절, 수동v2거절, 피해후재전송 상태보존. 피해검증기는 합성fixture로 표시하며 제품피격 증명아님. 소비용 ordinary-initialization-consumer-source-20261026.json 갱신(v2+6target+trainInitialState), 합성계정/내부START·실인증초기화HTTP이며 제품admission/Player 아님.
잔여: 실제 Unity train owner/첫·진행checkpoint/포격shield HP 적용/선로5인·Player 연결, 진행저장·terminal의 실제원본 공급 및 Server 검증. CombatFinalReportStore.attachRecord는 여전히 combatVerification/sourceVerification NOT_PERFORMED 및 rewardsAuthorized=false; 초기 HP 공급으로 종료검증을 승격하지 않음. 새 train-first-checkpoint-interruption 상세미승인 정책 적용0. 범위내 account train/current HP 및 출격 경로에서 반대되는 작전밖 지속손상 상태를 발견하지 않았으며 전체과거정책 부재 주장 아님.
<a id="train-first-checkpoint-server-recovery-20261027"></a>
## 첫저장 중단복구: 기존 Server 경계 재사용 검증 — 2026-10-27
TRAIN-FIRST-CHECKPOINT-INTERRUPTION-v1.20261027/44 승인. 기존49#train-initial-health-contract-20261027의 초기화원키/불변영수증과현재 combat_damage_states는별도시점. 초기화 POST 성공은 첫클라이언트checkpoint/전투준비 증명이 아니며 runtimeExecutionVerified=false 유지. 해당응답은초기원상태를복구할뿐 진행state를조회/검증/수선하지않는다. 저장없음을미개시로추론하거나pending/늦은응답이현재owner를덮는 동작은 소비자가금지해야한다.
서버 실제 생산HTTP handler를 별도 child process로 두번실행하여 신규범위 검증. 원초기화저장/프로세스종료 뒤 기존CombatHealthStore에 합성피해 proof만 주입해 trainHP4900을저장한경우: 새process 원키재전송은 원HP5000 영수증 hash 동일반환, 실제진행행/파일 bytes는그대로4900. 진행행 state_json 손상사례도 응답유실→새process 원키복구는원receipt만반환, 손상bytes를그대로보존하며 실제health.read는combat_health_storage_invalid 거절. 초기화성공응답을진행복원성공으로간주금지. 손상된원영수증은 checksum불일치409 ordinary_combat_receipt_invalid, invalid JSON+맞는hash는503 ordinary_combat_initialization_outcome_unknown; 새영수증/초기HP재저장없음. 이503을실패확정/새key권한으로해석하지않음.
QA/CommanderS1-20260922/train-first-checkpoint-server-recovery-20261027.json 4PASS(기존정상/ACK유실2+새진행/손상2), train-first-checkpoint-corrupt-receipt-20261027.json 2PASS. 새진행 process PID62052→46236, 손상42212→79204, 각논리초기화1/transportPOST2(원키복구1)/GET0. 원초기화의55PASS/type/lint 근거는생산코드변경없어재사용, 신규test lint/diff0. 현재HTTP/SQLJS 파일재개검증이며 Unity Android암호저장/실Player·메모리/실피격권위 증명아님. 원증거 ordinary-initialization-tcp-damaged-20261026.json 및 -corrupt-20261026.json은기존harness파일명형식유지/실행일2026-10-27.
서버 CombatCheckpointStore는 internal exact-text STORED_UNVERIFIED/NOT_PERFORMED 저장소로실전 full-state validator/restore selector/public upload가아님. 여기에초기receipt fallback이나임의 train/launcher wire를추가하지않음. 실제 Unity CombatCheckpointStore/전체simulation에train·artillery/clock binding 연결은Unity17의현재착수범위(TrainInitial777 원export고정), 새v2 source소비코드작성중을확인했으나첫checkpoint/실선로5인/Player 완료근거미공급. 완성원본을받아 Server 진행저장/terminal 검증접점의다음필요변경을판정. 이번기존wire충분/별도 UNITY-CONSUME 추가불필요, 전체통합큐미등록.
<a id="train-operator-concurrent-server-boundary-20261027"></a>
## 열차·요원 동시피해: 기존 저장·종료 경계 — 2026-10-27
44 TRAIN-OPERATOR-CONCURRENT-DAMAGE-v1.20261027 승인. 기존49#train-initial-health-contract-20261027의 trainInitialState.healthTargetId/원train binding과 sourceState.actors[].operatorId/원5인 roster가 대상근거이며 배열위치/선택UI/적template ID로추론하지않음. CombatHealthStore는공유1행의타깃별 HP/rebase owner. 피해판정/실드/시계/순서는실제producer가검증하고prepareChanges의원eventId·targetId·연속expectedRevision으로전달; 이번승인으로독립서버전투엔진/보호막pool/매피격API를추가하지않음.
기존 prepareChanges는한기준checkpoint를읽어입력순서대로후보를만들고 모든proof+CAS 뒤 최종행1회/원영수증들을동일DB batch에저장. 논리receipt revision은각사건순서(시험2,3), 영속최종revision은3이며 전체tick을새거래로강제하는정책아님. 실제producer의사건prefix·clock/위협과조합은현재제품host 잔여. target이없거나다른계정run이면거절,같은eventId의다른target은충돌,이미있던prefix에새사건을붙여부분재실행하면partial_replay 거절. 원그룹재전송은원영수증과쓰기0.
QA train-operator-concurrent-server-20261027.json 3PASS. 실제원START/6target 초기화 후 test-only 권위proof로열차/요원 순서두경우시험: 후보준비중DB무변경, 두번째receipt 저장실패시 HP와첫receipt까지rollback, 양쪽0에도다른4인원HP보존, 입력대상순서와최종revision 일치, 중복/충돌/부분재생/미지원target/다른계정거절. 별도시험에서준비후runs.status=FAILED를합성경합으로변경하면기존CAS가후발group전체거절·DBbytes/HP무변경, 이후새명령도combat_run_not_active. 합성피격/terminal상태주입은실제포격/종료판정증거아님. 제품코드수정0/기존55PASS·복구6시험재사용, 이번test lint/diff0.
HP0만으로서버가승패/보상을발행하지않음. 실제원START에 assertClientFinishAuthority를적용하면 combat_finish_verification_required, run_final_results0. 이는요원다운·열차패배·동시적전멸의기존terminal판정을대체하는새우선순위가아님. 최신Unity인계#unity17-train-initial-checkpoint-player-20261027의6actor/Sera-only/새process Player범위는선행으로재사용하되 full5전투·포격snapshot/실드·선로/원terminal 미연결을완료로승격하지않음. 실제해당제품기록/호출부가공급되면 Server검증경계에연결, 이번기존wire충분으로추가UNITY-CONSUME/Plan통합등록없음. 미승인rail-impact-position-time 상세정책적용0.
<a id="rail-impact-position-time-server-boundary-20261027"></a>
## RAIL-IMPACT-POSITION-TIME Server 원계약 재사용/실연결 대기 — 2026-10-27
승인44#rail-impact-position-time-approved-20261027에 기존 #rail-exact-hazard-contract-20261027 및 #rail-original-restore-train-source-20261027 적용. RailHazard.resolve는 frame.atMs===nextImpactAtMs, run/train 일치, 원carriageIds 전부의span·각segment 비교, 폐구간교차/후미HIT·다객차raw1회·동일사건충돌검사를이미수행. Exact.restoreBound는별도로보존한원발사definition을대조후원관측/결과를재실행하며오늘목표속도/새위치로바꾸지않음. 원definition/receipt일치가 geometry발행권한·실motion 원시각 증명을대체하지않음.
이번 확인범위 server/src에서는 hazard의제품실행caller없음(profile의version참조와자체restore만), Unity Scripts 전체검색에서도 FirstPlayableArtillery 생성/ResolveImpact의외부caller 미발견. ctor는preview/launch/impact가logicalStepMs에정확히나뉘도록요구; 실제프레임이impactAt을넘을때motion을그시각에서나누는host는미연결. RailConsistMotion.Advance의실속도/가감속/거리잔여·원profile 참조는있으나현재확인된연결은WorldBossRailGeometryConsumer 계열이므로 ordinary rail profile/계정run/첫교전 geometry로임의이식하지않음. frame에는geometry definition version/hash가없어현재원계약만으로그출처를서버가검증했다고주장할수없음. 실제ordinary producer/원geometry binding/checkpoint가공급된뒤그경계에필요한최소변경을정하며추측 DTO/새물리엔진/선로ID·속도발명0.
기존 QA/CommanderS1-20260922/ordinary-train-source-final-20261027.json의 rail-artillery-hazard 12PASS0FAIL 근거재사용(원시각/완전편성/후미/분기/한번raw/복원). 이부분source변경없어반복시험안함. 이번신규제품코드/검증완료없음; 실제선로가감속·전체checkpoint의시계/위협revision·새process Player/API메모리·terminal은미완료. 기존동적초기state/소수/ARRIVE배율공급은유효하며초기wire대기로되돌리지않음. WAIT는기존계약재사용과정확의존만기록, 새소비키/Plan통합등록0.
<a id="ordinary-rail-first-profile-server-20261027"></a>
## Ordinary rail first profile — Server 부분 공급 2026-10-27
승인 44#ordinary-rail-first-profile-approved-20261027. combat.ordinary-rail-trial.v1.20261027: body12000/gap1000mm, min/cruise/max14000/20000/26000mm/s, accel/decel5000mm/s², 기존 속도 효과 유지. definitionHash=baa7449dea870eb678f4b04414fa4ad9014ec2b5041cea41055d107a0914f99c.
원 START ordinaryRailSource(combat.ordinary-rail-source.v1.20261027)는 기존 owned profile의 실제 selectedTrain/selectedCarriages와 기존 소유 CAS를 재사용한다. selectedCarriages는 논리 선택 2~4개이며 물리 기관차/객차 수의 확정이 아니다. status=TRIAL_PROFILE_ISSUED_GEOMETRY_UNBOUND; physicalConsist/geometry/initialMotion=null. N5/L64/A125~131 예시나 WorldBoss LOCAL을 운영 원본으로 발행하지 않는다.
초기화 receipt.ordinaryRailBinding={version,ordinaryOriginHash,sourceHash}; immutable START만 검증하고 현재 loadout GET/재계산 없음. 최초 receipt replay 우선, 과거 source 부재는 미공급 유지, 명시 null/변조는409 ordinary_rail_source_invalid. ordinary finish downgrade guard에 rail source 포함. 신규 API/DB table 없음.
QA/CommanderS1-20260922/ordinary-rail-profile-final-20261027.json:73PASS/0FAIL. 실제 선택 고정·이후 성장 독립·소유 CAS 충돌 무부분입장·재해시된 미발행 정의/geometry 거절·역사 부재·receipt replay 검증. typecheck 통과, 변경파일 eslint/diff-check 통과. consumer export ordinary-initialization-consumer-source-20261026.json은 합성 계정/내부 START+실제 초기화 HTTP 증거이며 제품 입장 증거가 아니다. 65536바이트 한도 검사는 해당 2차량 fixture만 검증했다.
기존 Assets/_LASTRAIL/Content/Battle/BPhysicalAuthoringV01 scene/prefab/측정/단위/rail endpoints 실재 확인. 저장 검수 JSON은 collider5/anchor85, runtimeAdmission=false/playerVerified=false. 10m 초안이며 승인된 12m 운영 원본으로 승격하지 않는다. Unity OrdinaryInitializationReceipt는 새 binding을 아직 검증하지 않는다. 실제 physical mapping/선로 길이·version/hash/초기 위치·속도, motion/명중 host/전체 checkpoint·Player는 미완료. WAIT 후속의 정식 geometry·편성 공급 선행이 미충족이므로 UNITY-CONSUME 키는 아직 등록하지 않고 Server 부모 큐 유지.
<a id="ordinary-physical-selection-binding-20261027"></a>
## ORDINARY-PHYSICAL-CONSIST — 원선택 결속 부분 구현 2026-10-27
44#ordinary-physical-consist-approved-20261027 적용. 기존 ordinaryRailSource v1 wire/definitionHash는 그대로이며 readOrdinaryRailFromOriginal이 불변 START admission.loadout/ownedState에서 선택 객차의 ID/slot/level/revision을 재구성해 원 rail packet과 대조한다. loadoutRevision·기관차 trainId/level/revision도 원 train source와 일치해야 한다. 선택 소유 누락/중복/개수 불일치 및 재해시한 선택 변조는 초기화 저장 전에409 ordinary_rail_source_invalid. 현재 로비 DB/추가GET/새선택API/응답필드 추가 없음. source 없는 역사 및 이미 발행된 초기화 원영수증 재전송 순서는 유지한다.
수정: server/apps/game-api/src/combat-ordinary-rail-source.ts와 기존 combat-ordinary-start-source.test.ts. red QA ordinary-physical-selection-red-20261027.json: 재해시 객차변경이 허용됨을1FAIL로 재현. final ordinary-physical-selection-final-20261027.json:73PASS/0FAIL(기존 test의8변형 추가: 객차ID/slot/level/revision, admission train/revision, 소유누락/중복). typecheck/eslint/범위diff-check exit0, 독립 읽기검토 중요한 결함0. 합성계정·내부START/실초기화HTTP 시험이며 운영 입장/실플레이 증거 아님.
미완료: 실제물리차량definition/instance·기관차포함/별도목록 매핑, geometryversion/hash·길이/간격·유한선로/초기좌표. Unity PhysicalConsist792 측정 착수 기록 확인; 아직 최종 공급본으로 수락하지 않음. 이 변경은 원선택 교차검증이며 물리결속 전체 완료 아님. null physicalConsist/geometry/initialMotion 유지. 10m→12m자산 변경/WorldBoss 대체/임의기관차 생성0. 확정된 Unity 매핑·측정/맵 근거 공급 후 같은 START 원본에 정식 geometry 결속하고 span/새process/Player/API메모리 검증 필요.
ORDINARY-CARRIAGE-12M 실측 수신 갱신 2026-10-27: 44#ordinary-carriage-12m-alignment-approved-20261027로12m변형 제작 승인됨(앞선 미승인 표기는 당시이력). PhysicalConsist793 measurement.json 원본4개해시 현재파일 일치 확인, 측정단위1000mm/unit/scale1/차체10×3.2×3m/선로100m. 실선택→template/기관차 매핑은null로 정식 START geometry 결속 근거가 아직 아님. sourceGeometry 발행0, 기존 ordinaryRailSource null/unbound 유지. 상세검증 Server#ordinary-12m-measurement-intake-20261027. 500m선로안은미승인, 현재100m를8초포격 지원가능으로 승격하지 않음. 기존73PASS 재사용·새API/GET0.
<a id="ordinary-finite-rail-policy-server-20261027"></a>
## ORDINARY-FINITE-RAIL-CAPACITY — Server 정책 고정 부분 구현 2026-10-27
44#ordinary-finite-rail-capacity-approved-20261027 승인 반영. 신규 owned START ordinaryRailSource는 combat.ordinary-rail-source.v2.20261027, definition은 combat.ordinary-rail-trial.v2.20261027. 기존 시험차체/속력수치는 유지하고 finiteRail={version:ORDINARY-FINITE-RAIL-CAPACITY-v1.20261027,minimumLengthMm:500000,initialTailArcMm:20000,previewLookaheadMs:10000,endMarginMm:20000,maximumTravelIncludesSpeedEffects:true,prePreviewTravelBudgetRequired:true}를 같은 원definitionHash/sourceHash에 고정. definitionHash=7376c5e63d357cef2519b7b481e0c4aae145fa90ffd6dc527b5659b5fee54ee8. 초기화 binding은 기존경로에서 v2 원sourceHash를 재사용한다.
원 v1 capture/버전별 reader 유지, 과거START/초기화 영수증 재발행·소급추가 없음. 소유/admission 교차검증 유지. 재해시한 minimumLength/speed-effect 요구변조도 저장전 거절. 추가API/GET/DB table 없음. 실제 geometry/physicalConsist/initialMotion=null, TRIAL_PROFILE_ISSUED_GEOMETRY_UNBOUND 유지. 이는 정책 상수 발행이며 실제500m선로 지원·용량 판정 구현완료가 아니다.
후속 실제원본 검산: 실제 L에서 initialFront=20000+L, length>=max(500000,20000+L+예고전이동예산+실효과포함10초최대이동+20000). 예고시 실제잔여>=실효과포함10초최대이동+20000. 실제편성/선로/이동 예산 공급 후 이 수식의 실행 판정과 실제 geometry version/hash를 같은 원START에 결속해야 한다. 500m 완주보장·무한연장·끝단승리 없음. 미승인기관차12m/총38m/선두58m는 발행하지 않음.
검증: QA/CommanderS1-20260922/ordinary-finite-rail-red-20261027.json v2 미발행1FAIL→ordinary-finite-rail-final-20261027.json 74PASS/0FAIL. 역사v1 유지 시험1개+기존변조시험에유한길이/효과요구2변형 추가, 기존성장/재입장/원키복구/권한/finish경계 함께 통과. typecheck/eslint/범위diff-check exit0; 읽기검토 중요결함0. consumer export ordinary-initialization-consumer-source-20261026.json 업데이트, 2객차fixture compact64703bytes(<65536), 모든계정크기 보장 아님. 합성계정/내부 START+실초기화HTTP 근거이며 실플레이/입장증거아님.
Carriage12m794 verification은12m/1mgap/scale1/재로드동등의 부분근거이며 variantHashes4개를 현재파일과직접대조일치. 실차종/기관차매핑 null/Player미검증. Unity FiniteRail795 실제500m 제작 착수기록 확인, 최종기하 수락 전. WAIT 후속선행 미충족으로 부모큐유지·소비/통합선등록0.
기관차 매핑 원천 확인 2026-10-27: 44#ordinary-engine-carriage-mapping-approved-20261027 승인 적용. Server#ordinary-engine-selection-owner-20261027에 account_train/train-loadout/departure 실제선택경로 및 지붕미확인근거 기록. admission.loadout.trainId는계정소유열차선택이며 carriages와별개, actual기관차역할매핑은새제작근거후1회결속. 현재ordinary 원START에 독립기관차instance/5인roof배치정보는없음; 별도BSelection/Refit의physicalSlot·occupant원천을무조건대체적용하지않음. FiniteRail795 3asset hash 현재원본일치확인했으나 actual편성/초기위치와미결속. 기존railv2 null/unbound·74PASS 유지. 38m/58m는승인규격의조건부결과이며실제제작/매핑검증전runtime발행0.
<a id="ordinary-physical-start-binding-v3-20261027"></a>
## 실제 기관차·2객차·500m START 결속 v3 — 2026-10-27
완료범위 ORDINARY-CARRIAGE-12M:SERVER-BIND:20261027 및 ORDINARY-FINITE-RAIL:SERVER-BIND:20261027의 정식기하/물리instance 발행. EngineConsist796 역할자산6개 및 FiniteRail795 3원본hash 직접대조, 새 combat-ordinary-physical-source.ts는 그측정/원선택/역할/단위·길이·간격/정의hash 참조를서버정적원본으로고정. 서버가 physicalinstance를직접발행했으며 Unity로책임전가하지않음.
신규 owned START의정확 train_a + slot0 car_combat + slot1 car_gun에만 ordinaryRailSource.packet.version=combat.ordinary-rail-source.v3.20261027 발행. 다른train/순서/객차수/차종은기존v2 GEOMETRY_UNBOUND를유지하며38m로강제하지않음. 같은owned profile/CAS와admission원선택/소유revision 대조 재사용, 추가API/GET/DB table0.
정적 정책은 v3에서 중복본문대신 definitionVersion=combat.ordinary-rail-trial.v2.20261027 및 definitionHash=7376c5e63d357cef2519b7b481e0c4aae145fa90ffd6dc527b5659b5fee54ee8로참조. 원정책 본문은49#ordinary-finite-rail-policy-server-20261027 및 captureOrdinaryRailSourceV2의definition. 소비자는버전/hash로기존정책을해석하며알수없는참조는준비실패로처리, 현재값/기본값대체금지. v1/v2원wire는그대로유지.
physicalConsist={version:ordinary.trial-consist-authoring.v1.20261027,definitionHash:c2e5e813adfc3fb74755124c7aed8d17de0b79002a6ae82ec7509cb7cb83f497,instanceIds:[Engine,Combat,Gun순서의실제ID문자열],totalLengthMm:38000,initialTailArcMm:20000,initialFrontArcMm:58000}. 배열에역할문자열을보내는것이아니라각32hex불투명ID를발행한다. 생성은canonical JSON(version ordinary.physical-instance.v1.20261027/accountId/runId/ordinaryOriginHash/order/selectionId/definitionHash)의SHA256앞32hex(128bit). integrity용sourceHash/definitionHash는전체SHA256유지. 같은원run재시도/복원은동일ID, 계정/run변경은별도ID. trainId/HPtarget과분리, 신규HPowner/actor생성없음.
geometry={version:ordinary.finite-rail-authoring.v1.20261027,definitionHash:e63bbc228c1f8c505e6dee6c3ef6dd5c054e1ef2034603bc8e23221f65da47cf,railSegmentId:ordinary-first-straight-v1}. 이segment ID는Server가이정의에발행한식별자이며Unity미발행null을사실값으로복제한것이아님. 원기하의 -25000→475000mm/+X/1000mm-unit/길이500000과같은참조. initial placement는실측Engine arc46000~58000/Combat33000~45000/Gun20000~32000, 각각차체12000/gap1000, 총38000. 1m틈을충돌body로메우지않음. 신규12m기관차시험규격의제작/측정근거이며기존미술실측으로주장0.
초기화 ordinaryRailBinding은같은v3 version/origin/sourceHash. readOrdinaryRailFromOriginal이원admission과기하·ID·위치를재구성하여전체packet대조: 재해시한타ID/위치/기하/정책참조변경도409 ordinary_rail_source_invalid/쓰기0. 기존최초영수증replay 우선·v1/v2역사소급변경없음. 현재status=GEOMETRY_BOUND_EXECUTION_UNREADY, initialMotion=null. 정적초기위치는확정됐지만실속력/예고전예산/roof배치·실시간시계/HP피해실행준비완료는아님. 3+2새배치안미승인·기관차지붕권한추가0.
검증 QA/CommanderS1-20260922/ordinary-physical-binding-compact-20261027.json 77PASS/0FAIL/0SKIP. 첫v3미발행red1FAIL, 중간UUID크기65552>65536 red1FAIL 및정책참조미발행red1FAIL 확인후보완. 현재compact START64664bytes, account_UUID형식길이투영64965bytes(<65536); 전체계정/모든최대값크기보장아님. 같은run/다른계정·run ID분리, 미지원선택v2유지, 역사v1/v2, 원START/ACK유실/새process/권한/finish보존, 물리ID/위치/정책재해시거절, 실자산hash/측정간격/초기유효범위 검증. Unity자산교차검사는로컬Unity경로없는서버전용환경에서skip(이번0skip). typecheck/eslint/범위diff-check exit0,독립읽기검토 중요결함0. consumer export ordinary-initialization-consumer-source-20261026.json 갱신(합성계정/내부START+실초기화HTTP,제품입장아님).
미완료는원본기하공급이아닌 Unity정식소비/실motion/span/hazard/5인roof배치/단일checkpoint·새Player/API전체메모리 및실예고전·효과포함이동예산 검산. 기하소비후속2건만등록,5인roof·전체통합 선등록0.
<a id="ordinary-roof-layout-source-v1-20261027"></a>
## 3+2 원요원ID 초기배치 source — 2026-10-27
44#ordinary-five-roof-three-two-approved-20261027 적용. 기존railv3 유지, 신규 combat-ordinary-roof-source.ts가검증된5인 origin과같은rail source를소비하여 별도 START ordinaryRoofSource 발행. 오직실physicalConsist가공급된첫 train_a/combat0/gun1에적용; v1/v2 unbound/다른편성에는발행하지않음. 원소유CAS/같은START거래/원확정명단 slot0~4 정렬사용, 새API/GET/배치원장0.
packet={version:combat.ordinary-roof-source.v1.20261027,definitionVersion:combat.ordinary-roof-layout.v1.20261027,definitionHash:1fd14d7a0390049b22e1689e59d4b839e1bead095e3fb624efe8acc3dde6bab4,railSourceHash:같은원START의ordinaryRailSource.sourceHash,operatorIds:원명단slot정렬5ID,status:LAYOUT_BOUND_ANCHORS_UNVERIFIED}; sourceHash는이packet의canonical SHA256. 계정/run/원기하·물리ID는railSourceHash로결속하며중복본문전송0. definition본문은 captureOrdinaryRoofDefinition(),approval ORDINARY-FIVE-ROOF-THREE-TWO-v1.20261027.
정의slots배열순서대로 operatorIds[i]를연결한다: combat-front=(physicalOrder1,X3000,Z0),combat-middle=(1,0,0),combat-rear=(1,-3000,0),gun-front=(2,3000,0),gun-rear=(2,-3000,0). physicalOrder는동일rail.packet.physicalConsist.instanceIds의인덱스이며0기관차배치없음. slotId는Server논리배치ID, 실제Transform경로/측정완료라고주장하지않음. Unity는이ID/차량/좌표에맞는실anchor를대조·공급한다. 실제발Y는MEASURED_ROOF_SURFACE_PLUS_ACTOR_FOOT_OFFSET이며서버는Y/총구위치를추정하지않음. body높이3200을발Y로복사0.
초기화 receipt.ordinaryRoofBinding={version,ordinaryOriginHash,sourceHash}를기존HP/stat/phase/receipt 원자저장에포함. reader는불변START·원rail·원명단으로기대packet재구성, 재해시한요원순서/중복/정의hash/railhash/READY변경/명시null을409 ordinary_roof_source_invalid로저장전거절. legacyfinish ordinary source guard에도roof필드포함(null/중첩우회차단). old source부재·기존최초영수증은소급보충/재배치하지않음. 로비formation revision이바뀌어도진행run원배치변경0.
적용한계: 현재 ordinary START에는별도명시배치입력/소유자가없음(별도BPlacement를자동대체적용하지않음). 이번은그신규출격기본배치. 기존원START/receipt재사용은검증했으나임의명시배치전부를소비하는기능을구현했다고주장하지않음. 명시배치owner 도입시그원본을우선결속할제거조건을source shortcut주석에기록. 진행checkpoint의실배치/이동/공격phase는그소유자가우선복원해야하며이초기source로덮지않는다. parent rail의initialMotion=null/EXECUTION_UNREADY와roof ANCHORS_UNVERIFIED는계약/검증범위이며전체전투개시권한아님.
검증 ordinary-roof-source-red-20261027.json authenticated3중1FAIL(미발행packet접근)→ordinary-roof-source-final-20261027.json80PASS/0FAIL/0SKIP. 원slot정렬/배열표현역순동등/로비revision독립/기관차배치0/추정Y없음/원기하참조/역사부재/재해시변조쓰기0, 기존원키·새process·권한·finish경계유지. typecheck/eslint/범위diff-check exit0, 읽기검토중요결함0. START65169bytes/계정형식길이투영65470bytes(<65536); 해당fixture범위이며모든최대입력크기보장아님. consumer export ordinary-initialization-consumer-source-20261026.json 갱신(내부START/합성계정+실초기화HTTP증거).
Unity RailRoof797 실anchor독립제작 착수기록확인, 최종측정공급은아직수락하지않음. 실제foot/총구/애니메이션·포격장치간섭/기존이동배치·5인화면/실motion/hazard/6HPcheckpoint/Player/API전체메모리는미완료. 미승인30초·42초/1200m/초기motion안발행0. 양측ID/실anchor근거충족후 WAIT#ordinary-three-two-followups-20261027의 ORDINARY-FIVE-ROOF:UNITY-CONSUME:20261027 등록; 현재선등록0·부모유지.

<a id="ordinary-roof-anchor-consumer-contract-20261028"></a>
### Ordinary roof v1 실anchor 소비 대조 — 2026-10-28
기존 roof v1 원계약 변경 없음. ordinaryRoofSource.packet.operatorIds[i]→captureOrdinaryRoofDefinition().slots[i]→원 rail.physicalConsist.instanceIds[physicalOrder] 사용. 실자산 ordinary.roof-spawn-authoring.v1.20261027 definition SHA256 30e299dc5cb9793433208f8f5062ecfc0da793b94ca96162b22725d5602109bc, physicalDefinitionHash c2e5e813adfc3fb74755124c7aed8d17de0b79002a6ae82ec7509cb7cb83f497. combat-front/middle/rear→Combat/SpawnFoot_0/1/2, gun-front/rear→Gun/SpawnFoot_3/4. formation순0..4, vehicleOrder1/1/1/2/2, X3000/0/-3000/3000/-3000mm,Z0, 실제roof상면3212.5→anchor3213mm. actor발보정/무기표시/동작간섭은 Unity 실검증이 별도 필요. 원명시배치/진행저장우선, 현재화면순/이름순 재배치0. 원source/receipt재사용·추가GET0. 이를 원packet status변경이나 motion실행허가로 취급하지 않음.
근거 Server#ordinary-roof-anchor-crosscheck-20261028 / QA/CommanderS1-20260922/ordinary-roof-anchor-crosscheck-20261028.json 및 Unity#unity17-rail-v3-roof-797-20261027. 1200m/초기motion 승인됐으나 실제authoring 공급·실속력효과 포함예산 수락은 별도 단계이며 이번새source발행0.


<a id="ordinary-player-budget-source-v4-20261028"></a>
### Ordinary rail v4: 1200m·초기 motion — 2026-10-28
승인44#ordinary-first-player-travel-budget-approved-20261028. 신규 원START ordinaryRailSource version combat.ordinary-rail-source.v4.20261028. 정적 policy definitionVersion combat.ordinary-rail-trial.v3.20261028 / definitionHash 981a453c3f58f5985d3774ba393593dd227415cc9af9051160481f141546e691. 정의 본문은 combat-ordinary-rail-source.ts captureOrdinaryFirstPlayerRailDefinition: 기존v2 정책에 firstPlayerTrial 승인/30+2+8+2=42초·1200000mm·productTimeLimit:false·초기20000mm/s/가감속0·출격효과/진행checkpoint우선·실속력효과용량검증필수 추가. 본문은 원START에 반복하지 않고 버전/hash참조.
geometry.version ordinary.finite-rail-authoring.v2.20261028, definitionHash eeeb0da41505f285ffe497ba789c5610f8c2fab38ec4d1950549cac91b2b00bc, railSegmentId ordinary-first-straight-v2. PlayerBudget798 실제1200m(+X,-25000..1175000mm,1000mm/unit) 정의/prefab/scene3hash 검증. 물리편성 정의/ID 생성규칙/38m·후미20m·선두58m는 v3와 동일.
initialMotion={speedMmPerSecond:20000,accelerationMmPerSecondSquared:0}. prepareOrdinaryFiveStatOrigin 기존owned CAS에서 검증된 trainv2 초기정책/NO_ACTIVE_DEPARTURE_EFFECTS와 같은account/run/originHash/ownedSourceHash/loadoutRevision/trainId일 때만 공급. proof없음/다른scope/미지원효과는 ordinary_rail_initial_motion_unavailable, reader의원본검증실패는 ordinary_rail_source_invalid. 효과를0으로치환하는 fallback없음. 현재 train_a/combat/gun 외편성은 기존v2 unbound유지.
v4에서 중복 ownedSourceHash 필드만생략: 원ordinaryOriginHash가결속한 origin.ownedSourceHash로 원선택을재구성하고전체packet/sourceHash비교. 원정의/소유권검증삭제아님. 초기화 ordinaryRailBinding은v4/version·원originHash·sourceHash, roofv1도새railSourceHash에결속. 새GET/저장owner/실행엔진없음.
status=MOTION_BOUND_CAPACITY_UNVERIFIED. 신규initialMotion은초기입력이며기존진행위치/속도/가감속/시계를덮는복원명령아님. 42초실속력효과적분/예고10초+끝20m/회피가능성·실행gate는미검증, source공급만으로제품개시허가0. 역사v1/v2/v3 byte정의·500m·null motion 보존/재발행0. 소비자는명시v4지원이필요하며기존v3전용reader로묵시수락금지.
근거 QA/CommanderS1-20260922/ordinary-player-budget-final-20261028.json 83PASS/0FAIL/0SKIP, Server#ordinary-player-budget-result-20261028. 최신 ordinary-initialization-consumer-source-20261026.json 갱신(합성계정/내부START + 실제인증초기화HTTP, 제품admission/Player아님). compact START65134bytes/account_UUID형식투영65435bytes≤65536; 모든최대ID/성장값조합보장아님.


<a id="ordinary-checkpoint-server-support-20261028"></a>
### Server 원버전 저장 지원 확인 — 2026-10-28
기존railv1/v2/v3 원버전재구성/새v4독립발행 계약유지. 초기화원키 replay는원계정/run/START 및영수증request/response hash검증후원응답을돌려주며현재HP/진행checkpoint초기화명령이아님. 실제TCP/SQLjs파일·새서버process에서구railv3/roofv1/500m/null motion 원binding 및 감소HP/손상상태 bytes불변검증. 손상진행health는오류로남고원초기receipt를복구기본값으로사용하지않음. 합성QA출처이며실제품구저장/Unity이동공격checkpoint변환증거아님. 등록된새변환쌍없음/마이그레이션없음.
Server#ordinary-checkpoint-history-result-20261028 / QA/CommanderS1-20260922/ordinary-checkpoint-history-20261028.json 6PASS,63선택제외. 기존83PASS의원버전/계정run/재해시변조·역사경계근거재사용. 지원불가원본자동삭제/최신값채움0·정상후속GET0. 첫노드목표새제안은미승인이며결과권한변경없음.


<a id="ordinary-first-objective-source-v6-20261028"></a>
### 첫 구간 목표 원정의 결속 — 2026-10-28
승인44#ordinary-first-encounter-objective-approved-20261028. 신규 firstBattleEncounterSource combat.first-battle-encounter-source.v6.20261028 / profile combat.first-playable-profile.v3.20261028 hash86b2aff8721ab355501da88011b922703a1b5723d5e01c5b456f942b1c110e74. profile.completion={version:combat.first-objective.v1.20261028,definitionHash:a5b558a3d1003bf2dd74cbde8430083a1b220d1995fc702e26f475c9d907b012}. 정적원본 combat-first-encounter-objective.ts firstEncounterObjectiveDefinition을참조; 본문을계정별응답에복제하지않음.
원사수2격파/원단발해소/열차생존이required. 원hazard 정상HIT/MISSED 또는기존열차기능의발사전정상취소, 미해결발사탄0; 미생성/누락해소0·발사후파괴잔탄대기·포대파괴필수0·소진후재발사0·요원지상포대공격0·자동파괴점수0. 최종피해commit후trainHP>0 및기존패배순서. 근거원소유는기존checkpoint: 원사수ID/격파, 원hazard종결,6target최종상태,미해결탄,전투시계,checkpoint revision,원node목표정의. client완료bool거절. 이목표와정식게시노드의전체필수목표가정확히일치하고기타위협없을때만node완료가능; 추가목표있으면진행유지·상위작전승리/보상별도·시간/끝단완료0.
신규departure→원START→초기화firstBattleEncounterBinding에v6/sourceHash, 원map ARRIVE의prepareFirstPlayableScaling은v6의실게시profile전체일치검증을유지. 기존sourcev1–v5/profilev1–v2 불변, 이전completion문구를현재승인거절근거로쓰지않음. 원정의hash변조를다시hash해도 reader거절. 새지원flag/완료영수증/finish해제없음.
현재공급은정적목표정의/원START·초기화/배율결속 검증이며 actual objective evidence verifier 또는 node COMPLETE adapter구현이아님. 기존OperationMapVerifier.prepare는원증거검증과동일operation_map_write_guard batch의assertions를요구하며 BRouteMapVerifier는B native pin/원wave계약전용이므로이번ordinary학습완료로명칭대체0. Unity801 component판정은정식사수ID/clock/revision제품producer아님. 해당원증거/게시node전체목표매핑의실caller가아직없어 source runtimeSupported:false/canStart:false 및finish검증요구유지.
QA ordinary-first-objective-final-20261028.json85PASS/0FAIL/0SKIP·tsc/lint/diff0. 최신서버export갱신. compact65208bytes/account_UUID투영65509bytes≤65536(여유27bytes, 모든최대필드조합보장아님). 기존원응답/정적정의참조/GET0.


<a id="ordinary-first-objective-evidence-v1-20261028"></a>
### Server 소유 첫 구간 완료 증거·기존 COMPLETE 어댑터 — 2026-10-28
44 first-objective/encounter-transition 승인 범위. 구현 원본 server/apps/game-api/src/combat-first-objective-verifier.ts의 firstObjectiveEvidenceSchema / firstObjectiveEncounterIdentities / OrdinaryFirstObjectiveVerifier. Unity 샘플을필드발행선행으로요구하지않음. 이번은새게임정책이아니라현재승인목표의검증입력/원식별/기존거래연결이며미승인completion-evidence-flow 제안적용과구분.
입력 version=combat.first-objective-evidence.v1.20261028. 필수:
- accountId/runId/nodeId/encounterId/evidenceRef/mapRevision: 기존계정/작전/현재노드·원ARRIVE eventId·원증거참조·COMPLETE expectedRevision.
- startSnapshotHash/sourceHash/profileHash: 기존보존원START bytes SHA256, firstsourcev6 sourceHash, profilev3 definitionHash. objective={version,definitionHash}는firstobjective v1/a5b558...b012. nodeObjectives는정식게시노드의전체필수목표참조목록이며이번지원범위는이objective1개와정확일치하는경우만.
- checkpoint={revision,hash,atMs}: 최종6target/효과·실드·잔탄·시계가같이commit된원checkpoint참조. hash만으로진실성승격0. 필수원본이없으면evidence owner가거절해야함.
- issuedGunnerIds[2], defeats[2]={gunnerId,eventId,atMs}, issuedHazardId, hazard={hazardId,eventId,outcome,launchedAtMs,resolvedAtMs}. outcome HIT/MISSED/CANCELLED_BEFORE_LAUNCH. 취소는launchedAtMs:null과기존열차기능권위증명필수. HIT/MISSED는launch<=resolved<=checkpoint. 사수2ID/격파사건중복0·사건시각checkpoint초과0·hazard미발급/미종결/발사후취소0.
- targets[6]={targetId,hp,maxHp}: 원5요원+원train ID정확집합/HP범위/열차hp>0. 전체실드/효과복사대신위checkpoint원본으로검증. unresolvedProjectileIds/remainingThreatIds는배열이며완료때둘다빈값필수. 누락을빈배열로채우지않음.
ID발행: firstObjectiveEncounterIdentities({accountId,runId,encounterId,sourceHash})가 canonical JSON {version:'combat.first-objective-identity.v1.20261028',accountId,runId,encounterId,sourceHash,role}의SHA256앞32hex를발행. role='gunner-0','gunner-1','single-artillery'. sourceHash는원firstsourcev6, encounterId는기존성공ARRIVE eventId. 신규ID원장/조회없음, 원출발·원도착에서동일값재현. 최신lobby/새arrival로진행ID재발급0. 증거의발급ID를같이바꿔자가일관하게만들어도서버재계산과불일치거절.
지원흐름: 기존 OperationMapStore(db,new OrdinaryFirstObjectiveVerifier(db,owner)).commit(accountId,runId,{kind:'COMPLETE',eventId,expectedRevision,nodeId,evidenceRef}). adapter는COMPLETE만처리; ARRIVE/RELEASE_HOLD는기존각검증자소유. 현재COMBAT/depth1..11/ACTIVE/원게시profile전체일치·원ARRIVE firstPlayableScaling receipt/source/profile/revision을대조. 성공은기존mapProof/holdIds와조건검사statements를반환하고기존MapStore가CAS/receipt/state를같은batch로확정. 추가목표/위협/잔탄이면ordinary_objective_incomplete로node상태불변. 별도구간완료원장/HP·motion초기화/작전결과/보상지급없음.
권위경계: FirstObjectiveEvidenceOwner.prepare(context)→{evidence,holdIds,assertions}. 이것은클라이언트JSON수락기가아닌신뢰Server adapter다. 원발급/격파·포격사실/발사전취소권한/최종checkpoint 진위와최신성/정식게시node전체목표/보상hold를원소유기록으로검증하고읽은모든row를동일 operation_map_write_guard batch의조건검사로fence해야함. 자기hash/클라이언트완료bool/무조건통과production assertion 금지. 현재operating evidence owner/ingress endpoint는설치하지않음: owner없으면ordinary_objective_evidence_unavailable. 기존host의미검증checkpoint 저장을권위증명으로이름만바꾸지않음.
주요오류409: ordinary_objective_command_unsupported/evidence_unavailable/evidence_invalid/source_conflict/version_unsupported/node_conflict/profile_conflict/scope_conflict/arrival_conflict/issuance_conflict/incomplete/survival_unproven/events_invalid/hazard_unresolved. 기존MapStore 원키충돌/경합오류는그계약유지. 충분한완료응답의state/revision/verificationId/holdIds로표시갱신, 추가GET0. 재전송은원receipt우선이며원증거재평가/피해·지급재실행0.


<a id="ordinary-completion-pending-server-boundary-20261028"></a>
### 완료 pending: 기존 서버 거래 복구와 실제 권위 입력 잔여 — 2026-10-28
44#ordinary-completion-pending-ui-approved-20261028 적용. 원 COMPLETE body(kind/nodeId/evidenceRef/eventId/expectedRevision)와 eventId를 보존하고 동일 요청으로 복구한다. 기존 OperationMapStore가 계정/run 소유 확인 후 원영수증을 먼저 반환하므로 복구 때 증거 재판정/새 상태 쓰기/추가 GET이 필요하지 않다. 같은 eventId의 변경 body는 operation_map_event_conflict, 타계정은 operation_map_run_not_found. 커밋 전 통신/DB 오류는 완료/패배 확정이 아니며 원요청 pending을 유지한다. 재시도 1회 한도·pending 영속·로비 이동·계정/화면 세대 처리는 Unity 소유; 서버에 새 UI 대기 API/폴링을 만들지 않았다.

실제 운영 owner/ingress 미완료를 다음과 같이 구분한다.
- 원 START·원 ARRIVE 영수증·발행 profile/목표 참조·사수/hazard 안정 ID는 이미 공급된다. ID 재현 가능성은 실제 처치/명중 증명이 아니다.
- Unity 실제 전투 owner에서 발행되어야 할 미검증 사실: 두 사수의 gunnerId/eventId/atMs와 처치 인과, 원 hazardId/eventId/outcome/launchedAtMs/resolvedAtMs·미발사 취소 권한, 미해결 발사체/잔여 위협, 같은 checkpoint revision/hash/atMs의 6target HP와 실드/효과 처리 완료. 실제 전체 nodeObjectives 및 reward/transaction hold는 각각 게시 노드·기존 거래 owner에서 확인해야 한다.
- combat-checkpoint-store.ts는 STORED_UNVERIFIED/NOT_PERFORMED, combat-final-report-store.ts도 source/combatVerification NOT_PERFORMED, B result observation은 combatTruth NOT_PROVEN. 로그인·저장 SHA·자기 보고만으로 위 사실을 검증한 운영 FirstObjectiveEvidenceOwner를 구성할 수 없다. 이 저장 경계를 무조건 승인 owner로 포장하지 않는다.
- 필요한 Server 작업은 원사건의 신뢰 가능한 판정 경로와 원시계·checkpoint 최신성 검증, 관찰한 원기록 전체에 대한 동일 COMPLETE batch 조건검사. 현재 operation-map-command-routes.ts의 공개 명령은 destination뿐이며 ordinary COMPLETE ingress/caller는 아직 없다. 이번 UI 승인으로 별도 완료 API를 새로 발명하지 않았으며 기존 evidence-flow의 실제 ingress 설계·구현 잔여를 유지한다. 미승인 적 공간 후보는 사용하지 않았다.

검증: QA/CommanderS1-20260922/ordinary-completion-pending-server-20261028.json, focused 1 PASS/0 FAIL/68 제외. 기존 실제 SQL COMPLETE 테스트에 커밋 전 오류 무변경, 동일 요청 재시도·커밋 후 ACK 유실 복구, 디스크 SQLite 저장/다시 읽기+새 store의 원영수증 재사용, 타계정·변경 body 거절/DB byte 불변을 추가. 합성 승인 evidence owner이며 실제 네트워크/새 OS process/Player 테스트 아님. ESLint 및 해당 파일 diff check exit0. 기존85PASS는 이전 범위 근거이며 이번 전체 재실행 아님.


<a id="ordinary-completion-ingress-v1-20261028"></a>
### ordinary 완료 명령 접수 — 2026-10-28
사용자/Plan 정정: 대기 UI용 새 조회 금지는 필수 완료 명령 금지가 아니다. 기존 operation-map-command-routes에 POST /v1/runs/{runId}/operation-map/completion 추가. 기존 destination 본문은 바꾸지 않는다. 앞 pending-server-boundary의 공개 destination만 존재/ingress 미공급 설명은 이 변경 전 상태이며 현재는 이 절을 따른다.

요청: 기존 Bearer 인증, Content-Type application/json, Idempotency-Key(ASCII 0x21–0x7e,1–128), query 없음, 최대4096 byte. strict JSON {nodeId:string(1–200),evidenceRef:string(1–200),expectedRevision:positive safe integer}. accountId/runId/사건사실/completed flag를 본문에 추가할 수 없다. runId는 경로, eventId는 원키, kind는 서버 COMPLETE 고정. 원요청 body/key 보존.
성공200 {accepted:true,receipt:<기존 operation.map-receipt.v1>}는 원 revision/state/holdIds/verificationId를 포함한다. 노드 READY를 작전 전체승리/지급완료로 해석하지 않는다. 일반 인증 오류는 기존 코드/상태, malformed400, 타계정run409 operation_map_run_not_found, 같은키 다른body409 operation_map_event_conflict. 새 COMPLETE에 trusted owner 없으면409 ordinary_objective_evidence_unavailable. 예기치 않은 DB/검증 서비스 오류503 ordinary_completion_outcome_unknown은 실패/패배 확정이 아니며 원요청 복구 대상. Cache-Control no-store.
원기록 접근은 OperationMapStore의 원영수증 replay 이후에만 지연 실행한다. 기존 영수증 복구에는 현재 evidence owner가 필요하지 않다. 신규 검증은 기존 OrdinaryFirstObjectiveVerifier/동일batch assertions/CAS/receipt를 사용한다. 실행 등록은 기존 index의 registerOperationMapCommandRoutes를 재사용, 새 GET/폴링/원장 없음.

운영 FirstObjectiveEvidenceOwner는 여전히 미설치다. 주입 factory는 서버 코드의 신뢰 경계이며 HTTP 입력에서 구현/증거를 공급받지 않는다. 기존 checkpoint/report NOT_PERFORMED를 승인 증거로 승격하지 않음. 실제 원사건 진위·최신성/Unity full5 producer/호출부/Player는 잔여.
검증: red는 추가 경로가 없어404(기대409)로 실패. focused 통과 후 ordinary-completion-ingress-final-20261028.json 85PASS/0FAIL, ordinary-completion-ingress-map-20261028.json 23PASS/0FAIL. actual app 인증의 미로그인401/유효타계정409/owner미설치409/변경필드400/원영수증200을 확인. 성공 신규 거래는 테스트 Hono+합성 SQL evidence owner이며 실제 auth 성공 replay와 구분. 503 원요청재시도/ACK유실/저장재로드 근거 포함. tsc/ESLint/명시git-C diff check exit0. 실TCP 신규완료/새processPlayer/API3회메모리는 미검증.


<a id="ordinary-enemy-spatial-definition-v1-20261028"></a>
### 첫 적 열차 공간 정의·ID 준비와 START 결속 제한 — 2026-10-28
승인44 ordinary-enemy-train-spatial / completion-ingress-trust 적용. Server combat-first-enemy-spatial.ts가 firstEnemyTrainSpatialDefinition()을 공급한다. version combat.first-enemy-spatial.v1.20261028, definitionHash d43e8ad7d306ee670e6660f9c48f452dddbe171e26def7e94863d20dc0cf0f2c. 실제 Unity EnemySpatial805 definition 2c212a13653c767cc4804fc1bbd94fdb9bf19c37507076ef1c4cbf8e0cda3fa1 /prefab5844aaa8e930bc2b97c0fd831fa0acddf2b7cf60900b1585670c5f2f5e992d02 /scene8d0f97ca1041e6667f6ded705a8d9d2b21408c412bab229789d6e6264b4c6b9d를 현재 파일에서 각각 대조 MATCH.
12×3×3.2m, 아군실선두~적후미4000mm, 실motion동행 상대속도0, 사수후미+2000/+6000mm. 측정roof3212.5mm/발anchor3213mm; 총구·hitVolume=null, executionVerified=false. 타격사거리/차폐 원규칙, 진행checkpoint 위치/HP/탄 복원우선. 원선두58m의 적후미62/front74/발64,68은 초기 실측 조건이지 모든 run에 상수로 덮는 위치가 아니다.
firstEnemyTrainInstanceId({accountId,runId,encounterId,sourceHash})는 strict 원ARRIVE scope를 받고 canonical SHA256({version:'combat.first-enemy-instance.v1.20261028',...scope,role:'enemy-train',definitionHash:<위 서버공간정의 hash>}) 첫32hex. 기존 사수2/hazard ID 알고리즘은 변경하지 않는다. 기존 아군 instance를 복사하지 않으며 이 함수 호출만으로 발급/원사건 진위를 승인하지 않는다.

현재 신규 START/ARRIVE receipt에는 아직 연결하지 않았다. QA ordinary-enemy-spatial-definition-20261028.json에 definition과 크기 실측 저장: 기존 export originalStart65208bytes, account_UUID길이투영65509bytes. 별도 firstEnemySpatial {version,definitionHash} 참조만 추가한 후보는65365/65666bytes, UUID투영은65536보다130bytes 초과. 이는 해당 fixture/후보 필드명의 실측이며 모든 가능한 표현/입력의 불가능성 증명 아님. 미승인 size-budget 상세안에 따른 필수원본삭제/한도확대/정적정의축약을 실행하지 않았다. 실제 신규 schema 원START결속·원ARRIVE field/version·소비자 호환은 잔여.
이론상 무버프26m/s·42초·적선두74m·끝단20m이면1186m/잔여14m; 실제효과/LOS/공격/Player 검증 아님. 실motion·총구·hitVolume는 Unity805에서도 미검증. 정적공간정의/ID 단위2PASS, tsc/ESLint0, 원3asset현재hash 일치. 기존 완료 API85+23PASS는 이전 유효근거 재사용; 운영 EvidenceOwner 진위·최신성/fence 입력은 여전히49#ordinary-completion-pending-server-boundary-20261028의 미공급 범위이며 raw보고서로 승격하지 않는다.


<a id="ordinary-start-size-source-v7-20261028"></a>
### START v7 정적 중복 축소·적 공간/ARRIVE 결속·저장 경계 — 2026-10-28
44 start-size-budget 및 definition-reference-lifetime 승인 범위. 새 출발 firstBattleEncounterSource=combat.first-battle-encounter-source.v7.20261028, profile=combat.first-playable-profile.v4.20261028/hash3d25996385d9ea5d7b1807c10f2fe575353284ae502b47cd2740cb359987ed75. 기존 source1–6/profile1–3 함수·읽기 유지, 진행원본 최신치환 없음. 신규 source shape hash(기존Unity 방식으로 accountId/runId/originHash/formationRevision/selectedRisk 제외)=ef33bc5cd576988d71bcd2c78f73e3706708a24f647864a89625f8db0983c802.
중복축소: profile.scaling.variants 15개 정적 조합행을 제거하고 기존 scaling.definitionVersion/definitionHash/정밀도·HP/피해 적용규칙을 보존. Server prepareFirstPlayableScaling은 보존된 firstPlayableProfileV3().scaling을 해결하고 원참조 version/hash 일치 후 해당 risk/progress 행만 ARRIVE applied로 제공한다. full v3 원정의는 제거하지 않음. source.references의 enemies/artillery는 동일 profile 본문과 중복, 나머지는 null placeholder뿐이었으므로 v7에서 references 객체 제거. 필수 소유편성/장비/HP/효과/원키/복구 원본은 제거하지 않았다.
적공간: profile.enemies.spatial={version:'combat.first-enemy-spatial.v1.20261028',definitionHash:'d43e8ad7d306ee670e6660f9c48f452dddbe171e26def7e94863d20dc0cf0f2c'}. 원START→게시profile 일치→verified ARRIVE 거래에서 firstPlayableScaling.version=combat.first-playable-scaling.v2.20261028 및 enemySpatial {version,definitionHash,geometryDefinitionHash,physicalInstanceId} 결속. geometryDefinitionHash=2c212a...3fa1, ID생성은49#ordinary-enemy-spatial-definition-v1-20261028 원ARRIVE 알고리즘. v2에는 enemySpatial 필수, 기존 v1에는 부재를 schema에서 강제. 기존 source4–6는 scalingv1 유지. hash에는 추가 결속 포함, 원receipt replay 보존. 이 정적결속은 실제 motion/LOS/총구 실행증거가 아니다.
정의 공급: QA/CommanderS1-20260922/ordinary-start-reference-definitions-20261028.json의 definitions에 신규profile·보존historicalProfileV3(15variants)·risk전체·enemySpatial전체와 hash를 공급. risk version combat.b-operation-risk.v1.20261003/hash sha256:7889e8553ee2a64b383294e8f30edf87f78691096935cc74abf0b34b6652a5ce. 원소유코드 combat-b-operation-risk.ts / combat-first-playable-profile.ts / combat-first-enemy-spatial.ts. 새서비스/캐시/다운로드API 없음. Unity BOperationRiskSource에는 동일 risk hash/정밀계산 경계가 이미 있으나 FirstBattleSource Shapes 및 Run/FirstPlayableScaling의 인라인variants 의존은 새 reader 연결 필요. Unity코드는 Server가 수정하지 않았으며 실제 Player 해결 완료를 주장하지 않는다. 이전 원정의·자산 및 구run 원본 삭제 없음.

중요 정정: 기존65208bytes/UUID65509bytes와27bytes여유는 원문 UTF-8 투영뿐이었다. Unity Run/PendingRunOperation.ValidateAccepted는 원문.Length와 JValue(response).ToString(None).Length 모두65536 이하를 검사한다(저장용 escaped문자열 포함). 기존 fixture의 저장표현은71644자로 이미초과; 이전 수치를 저장복구가능 근거로 사용하지 않는다. AccountActionJournal은 별도 원문Length 검사, FirstBattleSource packet은별도131072UTF8경계다. DB snapshot 전체·HTTP원문·Unity영속표현을 구분한다.
새 fixture 원문58527bytes/저장표현63761자, UUID계정투영58828/64062. accountId/runId각128 ASCII x 투영60208/65442는통과. 같은128 역슬래시투영62256/71586은초과: origin schema허용문자범위의 최대조합지원 잔여결함으로기록하며 자동 truncate/한도확대하지않음. 모든장비·효과최대조합지원 미검증. raw6681bytes 감소는fixture측정이며 메모리/API비용절감률 아님.
departure-start-size.ts: 최종 equipment/B preparation 응답 선택후 db.batch 직전에 rawUTF8 bytes와 저장표현(JSON.stringify(serialized).length)을각65536검사, 초과409 ordinary_start_response_too_large. 원문/반환응답에동일값사용, 기존확정receipt replay는과거bytes를그대로반환. 대형입력을출발비용소비후실패시키지않으며낙관적UI가성공으로표시하면안된다. 최대포괄지원·준비실패복구신규정책 승격 없음.
검증 ordinary-start-size-final-20261028.json116PASS, -departure-20261028.json47PASS, tsc/ESLint/diff check0. 원래 base만 검사하여 prepared 큰응답이커밋되는 리뷰P2는 재현RED후 최종response 검사로수정·재리뷰추가발견0. v6/v7 실제SQL ARRIVE/COMPLETE·원키복구·정의변조거절, exact/over/UTF8/escape 크기·후속준비초과DB무변경 포함. 기존TCP init 새process 검증은같은서버suite 범위, 실제full5 Player/새 reader/완료권위producer/운영EvidenceOwner/3회메모리는미완료.


<a id="ordinary-post-start-server-recovery-20261028"></a>
### 확정 START 이후 준비/저장 실패: 서버 재소비 방지 경계 — 2026-10-28
44#ordinary-post-start-resource-recovery-approved-20261028 적용. 기존 startDeparture 원키 replay·OrdinaryCombatLifecycle 원영수증을 재사용하며 새복구 API/생산코드 추가 없음. 클라이언트 자산 준비 실패는 확정 START 취소/재출격 사유가 아니며 같은 run/source/정의를 유지한다. 실제 서버 초기화 저장 실패는 HTTP503 ordinary_combat_initialization_outcome_unknown, 미확정 실패를 패배·작전 포기로 바꾸지 않고 원초기화 key/body로 복구한다.
기존 실제6target 저장테스트를 인증 HTTP 실패 경계로 보강: START 후 초기화영수증 INSERT 실패주입→503·PREPARED/HP상태쓰기0, START 원요청 replay가같은응답/원snapshot 유지, fuel_ledger 전체불변·runs1. 실패원인제거후원초기화키복구, 이어진피해4900HP는영수증replay로만피리셋되지않음, 초기화영수증1/연료원장불변. 피해proof는기존합성fixture이며 자산파일손상·Player 저장공간고갈 재현은 아님.
QA ordinary-post-start-recovery-server-20261028.json1PASS/0FAIL/72제외, ESLint/diffcheck0. 이전116+47PASS는코드변경없어재사용하고이번전체재실행으로표현하지않음. Unity 누락자산동일버전재확보/영속성/owner·계정·화면세대/개시1회/로비·새process 검수는잔여. 원v7정의와영수증 공급49#ordinary-start-size-source-v7-20261028 유지, 전체최대입력지원/운영EvidenceOwner/full5 Player 미완료. 미승인 storage-envelope-budget 상세는새정책으로적용하지않음.


<a id="ordinary-storage-envelope-source-v8-20261028"></a>
### 실제 저장 serializer 대조·source v8 원정의 참조 — 2026-10-28
44 storage-envelope-budget 승인 범위. 새 START는 combat.first-battle-encounter-source.v8.20261028. packet={version,accountId,runId,originHash,formationRevision,selectedRisk,definitionRef:{version,definitionHash}}. 동적소유/원run/계정·원hash는보존. definitionRef.version=combat.first-battle-static.v1.20261028/hash299947212708d7f581c3ccf87474248802ec51bc87025310680736d4079444ce. 원정의body는보존sourcev7 packet에서accountId/runId/originHash/formationRevision/selectedRisk만제외한정확본문이며 status/canStart/runtimeSupported/entryRequirements/sequence/profile/기존미확인조건을모두포함한다. 참조미해결을지원true로해석금지.
firstBattleStaticDefinition 및원v7 producer가정확본문을해결, reader는원START의동적scope+참조version/hash+sourceHash전체를재생성대조. resolveFirstBattleProfile은검증된source에서만구본문또는보존profilev4를제공. source1–7 원reader/producer/원영수증불변; source8도profilev4/scalingv2/동일ARRIVE ID결속/COMPLETE verifier사용. 새cache/CDN/정의조회API없음.
QA ordinary-start-reference-definitions-20261028.json는이제v8이며 definitions.firstBattleStatic에실원본문+hash, profilev4/historicalProfileV3/risk/enemySpatial도유지. sourceShapeHash(동적5필드제외)=81b2615bba640a9be0e61356a938fac412140e904f25921da997c99d16abdf79. 기존 consumer-source JSON도v8 START/실init응답이다. Unity는v7뿐아니라v8 decoder/정의참조해결을연결해야하며서버가Unity파일수정하지않았다. 구version자료를최신것으로치환하지않으며미지원/누락은검증실패로남긴다.

실측 v8 fixture: raw52624bytes/escaped57390자, UUID계정52925/57691, accountId+runId128ASCII54305/59071, 128역슬래시56353/65215. 최대128따옴표투영도테스트통과. 이전v7의128역슬래시71586초과사례는이축소에서해소됐으나모든장비·효과·숫자최대조합의전체지원완료는아니다. ID문자제한/truncate/증거삭제/상한확대없음.
중요 serializer보정: Newtonsoft StringEscapeHandling.Default는 U0085/U2028/U2029를각6자로escape, JS JSON.stringify는문자그대로라기존길이계산이작았다. 실제 Unity PackageCache의 Newtonsoft.Json.dll(assembly13.0.0.0, sha2561eac7841fe2c5727324e07f12205103ab2c6d28268ac795dd9710871ece805b4)을독립AssemblyLoadContext로로드해실행확인. 해당3문자각+5길이를commit전검사에반영;3종RED후수정. 실제Unity의동일JValue/JObject Formatting.None 직렬화에서52624/57390일치. 샘플1record wrapper57702자/UTF8bytes. 메타데이터는합성wrapper fixture이며 PendingRunOperation전체검증/Player실행은아님.
제한소유구분: PendingRunOperation 원응답/encoded각65536자, EncryptedRunOperationJournal 전체plaintext3145728자 및미확정응답예약, AndroidEncryptedReceiptFilePort run_operations ciphertext JSON UTF8파일16777216bytes. domain prefix/crypto payload/파일잔여공간은서로별도. 실제암호화/보안저장파일/단말여유공간은측정하지않았고가짜ciphertext로대체하지않음. QA ordinary-storage-envelope-serializer-20261028.json에실DLL경로/hash/수치/미검증명시.
QA ordinary-storage-envelope-final-20261028.json167PASS/0FAIL, tsc/ESLint/diffcheck0, 읽기전용리뷰중요발견0. v6/v7/v8 실제SQL ARRIVE/COMPLETE·원복구·정의변조거절,최대ID투영/3separator와후속prepared초과원자거절포함. 실제단말암호화·전체장비효과최대·Unityv8/shared정의수명/Player/운영EvidenceOwner미완료. 저장공간정리미승인제안은실행하지않음.


<a id="ordinary-storage-pressure-server-boundary-20261028"></a>
### 단말 공간 정리와 서버 복구 오류 구분 — 2026-10-28
44#ordinary-storage-pressure-recovery-approved-20261028 적용. 단말파일정리는Unity소유이며서버새삭제/정리API 없음. 기존원START·checkpoint·미결key/body·미반영receipt·원run정의참조보존; 공간부족을ABANDON/재출격/재지급명령으로변환하지않는다.
- ordinary_start_response_too_large(409)는최종서버응답의논리크기초과이고START batch이전거절이다. 디스크정리/권한재요청으로해결된다고안내하지않는다.
- ordinary_combat_initialization_outcome_unknown / ordinary_completion_outcome_unknown(503)은서버결과불명확이다. 로컬디스크부족의증거가아니며원요청/key로복구한다.
- 응답200수신후단말영속쓰기실패는서버확정과로컬미반영을구분한다. 유효한원pending이있으면동일run/key/body의원receipt를재사용하고,없으면이어가기/저장완료를보장하지않는다. 보안저장권한오류·실파일공간부족·journal논리한도는단말owner의실원인으로분류한다.
이범위에서생산코드/테스트추가없음: 기존초기화실패503→원START/fuel보존→원키복구1PASS 및저장envelope167PASS 근거재사용. 실제단말삭제/디스크부족주입/공유캐시수명·절감량/새Player재개는미검증. 프로젝트폴더·로그·자산삭제실행0, 미승인다계정정리/계정수제한적용0.

<a id="ordinary-shared-replay-host-v1-20261028"></a>
## 공유 C# 실행기 첫 경계 — 2026-10-28
44#ordinary-shared-combat-verifier-approved-20261028 / ORDINARY-SHARED-VERIFIER:SERVER:20261028. Server 소유 server/tools/ordinary-replay-host는 원Player815 Managed LastRail.Runtime.dll을 .NET8 console에서 직접 참조해 OrdinaryFiveStartInput.Read→OrdinaryFiveCombatFactory.FromInitialization→BasicCombatSimulation.RestoreLocalState/CaptureLocalState를 실행한다. 규칙복제/Unity소스수정/Unity창/배포 없음. 원코어 복원과 실제 동일어셈블리 실행이 가능함을 준비상태에서 입증한 부분구현이다.
실행입력은 로컬 서버운영자가 공급하는 기존 export(originalStart/accountId/runId/initializationResponse), 실제 Player checkpoint, 신뢰build의 Runtime/Newtonsoft SHA256 두 개. client 파일경로/업로드/API 아님. 1MiB/file 및 15초 별도프로세스 시험timeout은 로컬 검증도구 경계이며 게임규칙/운영호스트 선정 아님. 비어있지 않은 operations는 서버수락owner 없으므로 거절. current factory 고정seed1을 원서버시드로 오인하지 않으며 seedAuthorityVerified=false/acceptedInputVerified=false.
출력 ordinary.shared-replay-host.v1.20261028: REPLAYED_UNVERIFIED, completionAuthorized=false, executionVersion/phase/tick, 재현checkpoint/hash, elapsedMs/cpuMs/peakWorkingSetBytes/privateMemoryBytes. 거절은 exit1+REJECTED/false/예외종류이며 민감원본문 출력없음. exit0는 상태재현 성공만 의미. FirstObjectiveEvidenceOwner/COMPLETE로 승인증거 전달하지 않음.
검증 QA/CommanderS1-20260922/SharedReplay816/verification.json 8개관찰검사: 실제Player상태/별도process정확복원/미준비승인0/HP변조/타계정/실행pin불일치/구조버전/미수락operations거절. 세process 모두tick0 AWAITING_ENCOUNTER. runtime279d216246fccf2146a01f4ccc8bad5750198fc04ebb642b912d8550ecd356e6, Newtonsoft87fa9a30e8406b0ccee1ac7f44b529228b4d76f7a3549b1a3817fdda0f5b5b2a는원Player DLL과실행사본일치. 마지막실측wall284.24/269.48/270.89ms, CPU125/125/140.625ms, peak working set47390720/47341568/47345664bytes, private22134784/21999616/22016000bytes. 프로세스시작포함wall,코어호출구간CPU이며실전비용/누수/API3회측정아님.
잔여: 실제ARRIVE/원시드·수락입력/시간검증, Unity공유코어의native geometry/각행동/motion/hazard/완료계산, 검증결과→기존원본revision/hold fence→COMPLETE. 확인한현재core factory Geometry=null이므로실전재현불가; 이를Server방식승인대기로표현하지않음. Cloudflare game-api에서.NET직접실행 연결/운영host선정·배포는없음. 입력전송주기/신규원장/보관계약미발행, 부모큐유지.
<a id="ordinary-replay-prefix-v1-20261028"></a>
## 공유코어 정렬·서버prefix 재현 비교 — 2026-10-28
ORDINARY-REPLAY-INPUT-RESUME:SERVER:20261028 부분구현. 기존 server/tools/ordinary-replay-host는 이제 Unity SharedCore816/build3/LastRail.Combat.Core.dll을 참조한다. corehash3eba0e74edae6a752f27f4efe5edfec7085c44a6d21eb470bd16db597d1b7374, JSON1eac7841fe2c5727324e07f12205103ab2c6d28268ac795dd9710871ece805b4. 기존82원본+probe1개공유, UnityEngine참조/실행산출물0. SharedReplay816 과거Player전체어셈블리/QA는보존.
로컬CLI선택입력 server-prefix-json은신뢰서버owner가공급하는별도파일: version ordinary.replay-prefix.v1.20261028/accountId/runId/startSnapshotHash/configurationSha256/inputs[{sequence,tick,command:{kind,value}}]. 서버소유원본/실행pin과대조하며sequence1부터연속·tick==공유model현재tick·제출checkpoint.operations전체동일을검사. 현재 PAUSE/SPEED/ULTIMATE_AUTO만원도메인메서드호출;원method의값검사와전체state/operation결과대조. ADVANCE/공격/임의HP결과/추가필드는거절. seed1권위승격0.
응답prefixConsistencyVerified/matchedAcceptedSequence는공급된서버prefix와의비교만뜻함. acceptedInputVerified=false/seedAuthorityVerified=false/completionAuthorized=false 유지. prefix파일자체의진위는로컬호출자책임이며client가제출한prefix를신뢰하면안됨. 실제운영prefix수락·DB저장·CAS·ack복구owner는미연결,본문비교만으로영속멱등처리완료주장0. 동일전체요청은새model에서동일상태를재현;원기록/단말파일수정0. 실제checkpoint/API새필드발행/원장/전송주기없음.
QA SharedReplay817/verification.json17검사통과:실Player815준비state재현·3새process동일+합성준비control3명령prefix실행·재전송동일·HP/계정/버전/pin/미수락operations/sequence누락중복/tick미래/원본교체/명령본문변조/ADVANCE거절. unity-parity.json은Unity816 preparedState와.NET공유코어state정확일치. 준비tick0이므로실전/새process게임진행/API3회증거아님. .NET빌드0경고0오류,읽기전용리뷰중요결함없음. 전체공유전투/운영입력owner·원시드/교전·시간수락/완료검증은잔여, 부모큐유지.
<a id="ordinary-node-restart-v1-20261028"></a>
## 노드시작 재입장 서버 계약 — 2026-10-28
BATTLE-NODE-RESTART-OVERRIDE-20261028/44#battle-node-restart-user-correction-20261028 우선. 중단세부상태를다음시도로이어붙이는수용조건폐기. 기존SharedReplay816/817·prefix는동일시도검증용만보존,전체삭제/자동변환없음.
신규 OrdinaryNodeAttemptStore가 run/node별단일불변snapshot과generation소유. migration0142는신뢰진입owner가제공한원node개시snapshot_json/hash와원START결속을저장하며백필0. snapshot필드는원도메인version객체를그대로보존;실제6HP/장비증강/실드효과/쿨다운/배치/속도/적/정의·시드유효성은 OrdinaryNodeStartOwner.prepare의책임이며모든원천assertions를같은batch에공급. client snapshot API없음. 현재운영capture producer미연결:초기원본이없으면409 ordinary_node_snapshot_missing,현재HP/만피/작전최초HP대체없음. source발행이끝났다고주장하지않음.
POST /v1/runs/:runId/operation-map/restart: 실제인증/Cache-Control:no-store,JSON4KiB,Idempotency-Key ASCII1..128. body {nodeId,expectedRevision,expectedGeneration},snapshot추가필드거절. 내부requestId는header원키. 성공 {accepted:true,version:'ordinary.node-restart.v1.20261028',requestId,accountId,runId,nodeId,status:'RESTARTED',revision,generation,snapshot,snapshotHash}. 동일run/node/원snapshot을유지하고generation+1/maprevision+1/receipt를원자저장,지도선택/HPowner/경제원장변경0. snapshot은새host초기화입력이지현재서버HProw강제되돌리기명령아님. 같은키다른본문409request_conflict,변경없는같은키재전송원응답재사용,더새시도면409restart_superseded. 원본/경합불일치409,불명확503ordinary_node_restart_outcome_unknown은원키로복구.
원완료/패배등run종료·해당node완료/다음node확정시 {accepted:true,동일identity,status:'RESULT_AVAILABLE',runStatus,revision,state,generation:null};과거재시작receipt재전송이어도옛snapshot으로되돌리지않음. 해당node미진입/미존재는결과완료로오인하지않고거절. 미결완료요청은클라이언트가기존원키/receipt복구후재시작판정;현재서버에client미전송/미확정요청을추측하는새원장없음. RESULT_AVAILABLE은기존결과흐름으로복귀하라는지도/실행상태이며새지급영수증아님.
시도관리node의 COMPLETE body에 attemptGeneration 필수(기존미관리node역사호환optional). 서버prepared proof및first-objective evidence도동일generation필수. OperationMapStore가원generation과기존maprevision/state를동일batch에서조건검사하며재시작먼저확정시옛완료거절. 완료먼저확정시재시작은결과복귀/경합실패. attempt미생성시NOT EXISTS검사도포함하여capture와옛완료경합차단. 기존receipt재생이새검증보다앞선순서는보존.
현재부분범위: 불변snapshot저장/재시작API/완료세대중재까지. 실제node개시producer/ARRIVE응답의초기generation전달, Unity새host생성·옛callback격리·원nodeHP/장비증강적용,실피해/패배owner세대결속·원seed/geometry/실전완료는미완료. 런타임capture가없으므로현재운영node를신규API만호출해재시작할수없음. 재출격/추가소비/재지급/경제거래롤백경로추가0. 운영배포/마이그레이션실DB적용없음.
검증:node-restart-final-20261028.json30PASS(3suite),합성nodeentry owner로저장/경합원자성·다른계정/추가snapshot거절·실인증route검사. 이합성source를실6HP진위/Player증거로사용하지않음. 타입/ESLint0,리뷰중요결함없음. 전체회귀결과는담당인계추가기록.
<a id="ordinary-node-entry-source-v1-20261028"></a>
## 노드 진입 상태 원천·원자 ARRIVE 준비 — 2026-10-28
S-NODE-START-CAPTURE-20261028 / BATTLE-NODE-START-CAPTURE-v1.20261028 부분구현. ordinary-node-entry-source.ts의readOrdinaryNodeEntryOwners는실제 CombatHealthStore.read로6HP/maxHP·health revision/원ledger일관성을확인하고,run장비placement/effect owner·ordinary_run_stat_states/source/result·원5인편성/kit/train/rail/roof reader를대조한다. 저장된stat source와result일치/장비revision·slot일치/6개ID/maxHP를검사하며node진입HP로조건부스탯을다시계산하되기존HP/stat row를덮지않는다. 현재값읽기는신규node진입준비전용이며재시작은저장된node원본만사용한다.
반환owned entry: accountId/runId/startSnapshotHash; owners{healthRevision,statRevision,inventoryRevision}; actors[{operatorId,slot,hp,maxHp,slots,effectiveStats}]; train{trainId,hp,maxHp}; definitions{originHash,kitHash,trainHash,railHash,roofHash}; modifierSource. 마지막modifierSource는stat계산원천참조이지진행node현재효과없음증거아님. 모든관찰HP/stat/equipment/effect원문/revision과RUNNING/원START를samebatch SQL조건으로fence한다.
OrdinaryNodeEntrySource는위실원천+신뢰 OrdinaryNodeEntryRuntimeOwner를조합한다. runtime이확정해야할필드는executionVersion/effects/actions/spatial/enemies/determinism이며각도메인유효성·시작회복/쿨다운정책·modifier최신성·원시드검증및관찰fence가필수. 반환snapshot version ordinary.node-entry-state.v1.20261028에원6HP/장비/정의와nodeId/entryMapRevision/runtime을함께보존. 현재실runtime owner미구현이므로직접준비는ordinary_node_runtime_entry_unavailable로실패하며0실드/0쿨다운/빈위협/seed1을발행하지않음.
원자진입: OperationMapStore의명시적third nodeStartOwner가설치된검증ARRIVE는 OrdinaryNodeAttemptStore.prepareArrival을호출해TRAVELLING/확정edge→대상node/원maprevision을검사하고,진입상태·불변snapshot insert·generation1·원ARRIVE receipt를같은batch에서저장. receipt추가필드 nodeStart:{version:'ordinary.node-start.v1.20261028',accountId,runId,nodeId,generation:1,entryMapRevision,snapshot,snapshotHash}. 기존firstPlayableScaling동반유지,정상후속GET불필요. 응답유실은원ARRIVE eventId재전송으로원response재생하며source재캡처0. insert/receipt실패시snapshot/진입모두rollback. 비전투node에는nodeStart를생성하지않는다.
이새field는소비reader구현이필요한준비계약이다. 기존legacy/no-ownerARRIVE/과거receipt는자동업그레이드하지않으며nodeStart부재는새시도시작허가가아니다. 운영runtime producer/third-owner등록/제품ARRIVE caller는아직연결되지않음;기존capture(진입후별도쓰기)는보존했으나새진입은prepareArrival원자경계를사용해야한다. 실제피해·패배owner세대결속은여전히미완료, COMPLETE세대조건은기존구현유지.
검증 node-entry-capture-20261028.json122PASS/0FAIL/0SKIP(5suite),tsc/ESLint0,읽기전용리뷰중요결함없음. 실제START초기화owner의6HP/장비읽기와HP변경후700/4200보존·옛sourcefence거절검사(HP변경권위만합성);atomicARRIVE/snapshot/generation/receipt rollback·원키재생검사는합성fullsource. 실제전투/최초snapshot전체권위/Player재입장완료증거아님. WAIT U-NODE-START-CAPTURE-INTEGRATION-20261028의실제nodeentry완료선행은미충족이므로Unity소비실행큐신규등록없음.
<a id="ordinary-node-health-generation-v1-20261028"></a>
### 노드 시도별 HP 저장 검사 — 2026-10-28
내부 CombatHealthStore CHANGE 명령에 optional attempt:{nodeId,generation} 추가. node 원본이 있는 run의 ACTUAL_HP(DAMAGE/HEAL/REPAIR/RECOVERY) 신규 쓰기는 필수이며 기존 authoritative verifier 검증도 그대로 필요하다. 계정/run/원START/현재 ACTIVE node/hold 없음/동일 generation을 검사하고 동일 HP batch에서 지도 revision/state/plan 및 generation을 재검사한다. 원본 없는 기존 run은 기존 명령을 유지하되 준비 이후 node 원본 생성 경합도 NOT EXISTS fence로 거절한다. 명시 attempt가 있는 MAX_HP_REBASE에도 동일 검사를 적용하나 기존 장비 최대HP 재계산에 새 attempt 필드를 강제하지 않는다.
원요청 hash는 attempt까지 포함, 같은 eventId의 시도 바꾸기 금지. 이미 저장된 원영수증은 먼저 반환하며 새 피해/상태 갱신 권한으로 취급하지 않는다. 응답 receipt v2/ledger 형식은 유지하므로 소비자는 원요청 attempt/화면 세대를 함께 확인해야 한다. 원시도 이후 신규 쓰기409 ordinary_node_attempt_required/attempt_conflict; 준비 후 경합은 기존 combat_health_state_conflict. 실제 공격 증명 owner/공개 피해 API를 추가하거나 클라이언트 HP를 신뢰한 것이 아니다.
검증: QA/CommanderS1-20260922/node-health-generation-20261028.json 126PASS/0FAIL(4suite); 최종 추가 경합 tests는 node-health-generation-final-20261028.json 10PASS/0FAIL(중복 합산하지 않음). 타입/ESLint/diffcheck0. 합성 피해증명으로 실제 HP 저장/원영수증/재시작 경쟁을 검사. restart가 HP row를 초기화하는 기능 및 실제 전투 패배 확정은 이번 범위에 없음.
<a id="ordinary-node-arrival-call-binding-20261029"></a>
### 제품 ARRIVE 호출의 nodeStart owner 전달 — 2026-10-29
기존 POST /v1/runs/:runId/b-route/transition의 서버 구성 BRouteEvidenceProvider에 nodeStartForAuthenticatedContext(c,accountId):Promise<OrdinaryNodeStartOwner> 추가. HTTP body/클라이언트의 provider 지정은 허용하지 않는다. 설정된 owner는 OperationMapStore의 기존 세 번째 생성 인자로 전달하며 새 ARRIVE 원본 생성이 실제 필요할 때만 factory/prepare를 호출한다. 원영수증 replay 및 비전투 node는 runtime 재생성을 하지 않는다. 준비 오류는 도착/원본/초기generation 저장을 모두 막으며 기존 augment 후속 호출은 commit 이후 유지한다. index.ts에는 실제 runtime factory를 아직 등록하지 않았다.
OrdinaryNodeStartOwner.prepare context에 arrivalEventId와 firstPlayableScaling(해당 원본 버전/게시node에서 공급될 때)을 추가했다. 같은 ARRIVE에서 검증한 적ID/공간/배율 binding을 복사해 전달하고 기존 assertion을 동일 batch에 유지한다. 별도 GET/적 재발행/재계산을 요구하지 않는다. 기존 capture와 구원본에는 해당 정보가 없을 수 있으므로 optional이며, 실제 runtime owner는 자신이 필요한 원본 누락을 거절해야 한다. 기존 nodeStart 응답형식 변경 없음.
QA/CommanderS1-20260922/node-arrival-route-20261029.json 102PASS/0FAIL/0SKIP(3suite), tsc/ESLint/diffcheck0. RED는 configured owner를 무시해 준비불가인데 ARRIVE200으로 진행한 현상을 확인. 최종 실제 route handler 시험은 로컬 인증 callback·합성 accepted-source 및 node snapshot fixture이며 제품 GAME인증/실전 생성 proof가 아니다. 이는 호출 연결 부분완료이며 실제 DEV 교전 완료가 아니다.
<a id="ordinary-node-execution-origin-v1-20261029"></a>
### 노드 최초 원시드·실행버전 — 2026-10-29 사용자 직접 승인
사용자가 Server 질문 ‘첫 도착 확정시 서버 원시드1회 생성/실행버전저장, 같은노드 재입장재사용’에 ‘기존 상세안 적용’ 승인. 44#ordinary-encounter-origin-pin-proposal-20261028의 미승인 기록은 이전 상태이며 Plan 소유 원문 정정은 Plan이 담당한다.
신규 ordinary-node-execution-origin.ts: executionVersion=combat.ordinary-five-execution.v1.20261028 (현재 공유 OrdinaryFiveCombatFactory.Version). determinism={version:ordinary.node-random.v1.20261029,algorithm:XORSHIFT32_13_17_5_V1,seed:uint32,initialState:uint32-positive}; seed는 서버 crypto.getRandomValues, initialState는 seed0일 때 기존 RunRandom의0x6D2B79F5, 그 외 seed 그대로. 정수 JSON 값이며 byte-order 추론 필요없음. seed=1 고정/클라입력/시각연출 난수는 사용하지 않는다. 실행버전 pin은 fullCombatReady 증거가 아니다.
OrdinaryNodeAttemptStore.prepareArrival이 새 도착 원본 준비 context.executionOrigin에 후보를 전달. commit 실패 이전 후보는 권위 원본이 아니며 원자적으로 수락된 snapshot만 원본이다. OrdinaryNodeEntrySource는 runtime.executionVersion/determinism이 해당 원본과 정확히 일치해야 수락하며 원본 누락409 ordinary_node_execution_origin_unavailable, 바뀐seed/version409 ordinary_node_execution_origin_conflict. runtime producer에는 context 복사본을 전달해 입력 변조가 비교 기준을 바꾸지 않게 한다. 별도 seed 원장/새 API 없음. 원시드는 snapshot.runtime.determinism에 보존되므로 기존 nodeStart/restart wire 그대로 사용하고 재시작 시 생성함수 호출0. standalone 구capture는 이 실행원본을 새로 발명하지 않는다.
소비: Unity/shared runner는 현재 시험용 RunRandom(1)을 이 검증된 원seed로 대체해야 하며 원node/계정/run/START/시도 결속과 원configuration/version 검증을 유지한다. 이후 전투 난수 상태는 동일시도 공유코어 소유, 시각 랜덤 분리. 아직 실제 runtime provider/Geometry/full5 행동/도착 검증 producer가 없으므로 index 운영등록·실Player교전 완료가 아니다. 구run/역사checkpoint를 새seed로 변환하지 않는다.

<a id="node-leave-intent-server-v1-20261029"></a>
### 로비 이탈·권위 종료·재입장 구분 — Server
로비 이탈/일시정지 자체에 서버 포기/restart/GET/비용 명령 추가 없음. 기존 restart에서 FINISH_PENDING은 결과확정이 아니므로 기존 원종료 요청 복구를 요구하는409 ordinary_node_transaction_pending 반환; 과거 RESTARTED receipt가 있어도 새host 재생 권한으로 반환하지 않는다. 확정 COMPLETED/FAILED/ABANDONED이면 기존 RESULT_AVAILABLE 우선, 신규 COMPLETE는 run_not_active로 차단. 원finish receipt 재사용 경로와 기존 권위 finish guard/보상정책을 유지한다. 현재 ordinary source의 ABANDONED도 권위결과 검증이 필요하므로 단순 버튼 입력으로 guard를 해제하지 않았다. 실제 포기 원결과 producer 연결과 Player UI 검수는 미완료이며 새API 승인으로 해석하지 않는다.
<a id="ordinary-node-actions-v1-20261030"></a>
### 노드 시작 actions 및 실제 stat 보충 — 2026-10-30
NODE-INITIAL-ACTION-INPUT-v1.20261030 승인 적용. OrdinaryNodeEntryRuntimeOwner가 공급할 actions 입력은 strict {version:'ordinary.node-actions.v1.20261030',stepTimeSpanTicks,ultimateAuto,actors:[{operatorId,basicNextOpportunityInTicks,activeCooldownRemainingTicks,ultimateCharge}]}이다. stepTimeSpanTicks는 기존 공유코어의 양의 정수 ms(10000 TimeSpan tick 배수), 상대행동 tick은 음수없는 safe integer, charge는 공유코어 int 표현범위0..2147483647(게임 충전상한 규칙 아님). 원5인ID를 중복없이 정확히 포함하고 자동설정도 명시해야 한다. 누락/타인/중복/범위오류는409 ordinary_node_actions_source_conflict. 기본0/만충전/예시3초·60% fallback은 없다.
시간 필드는 해당 node 전투개시를 기준으로 남은 논리 step이다. 첫행동 기회를 제공하며 명령예약/탄/중단직전phase/prefix를 이어붙이는 자료가 아니다. 로딩/벽시계/준비 재시도로 값을 줄이지 않는다. 원래 충전단위를 보존하며 백분율로 임의변환하지 않는다. 실제 active cooldown/first opportunity/charge/ultimateAuto는 신뢰 행동·설정 owner의 확정값과 assertion으로 공급해야 한다. 이 스키마 자체는 그 producer나 새 초기화정책이 아니다.
서버는 output runtime.actions에 stats:[{operatorId,conditionalDefenseFlat,skillMagnitude}]만 추가한다. 값은 같은 entry HP로 계산된 기존 OrdinaryFiveStatOwner 결과이며 runtime provider는 이 필드를 지정/덮어쓸 수 없다. 나머지 currentHp=기존 snapshot.actors[].hp, equipment=slots, effectiveStats는 동일 actor 원본을 그대로 사용한다. 전체stat/장비JSON을 중복저장하거나 최초 초기화receipt의오래된값으로대체하지 않는다. 소비자는 두 원자료를 ID로 조합해 기존 ActorStats에 전달하고 스탯 규칙을 새로 계산하지 않는다. snapshot v1 외형/원seed 계약/원자 ARRIVE는 유지하며 actions 하위버전으로 구분한다. 구저장원문/구reader를 소급수정하지 않는다.
검증: node-actions-20261030.json90PASS/0FAIL/0SKIP; 마지막 중복 stat 제거·flat0 기대값 반영 후 node-actions-final-focused-20261030.json의 해당 source case 재검증. 타입/ESLint/diffcheck0. 실제owner의HP/장비/조건부스탯은 사용했지만 행동값은 명시 합성fixture이며 운영쿨다운/충전 정책이나 실제자동수동1회소비 증거가 아니다. 실제 runtime producer/공유core 소비/Player검수는 남아 있다.
<a id="node-target-policy-binding-20261030"></a>
### 노드시작 원스킬 단계 정의 연결 — 2026-10-30
SKILL-TARGET-LOSS-STAGE-v1.20261030 대조 결과, 기존 combat-ordinary-relocation-source v4 레이나 selection.primaryLost/primaryDiesAfterSelection, v5 미라 projectile.arrival, v6 이브 collision/lifetime에 승인된 단계별 규칙이 이미 있다. combat-policy-catalog/CombatUltimateState는 수락전 거절 미소비·수락후 무환급과 원명령 중복소비 방지를 소유한다. 단순 kit만으로 세부규칙 전체 공급을 주장하지 않는다.
OrdinaryNodeEntrySource가 동일 원START의 readOrdinaryRelocationFromOriginal을 기존 kit와 함께 검증·재사용한다. runtime.prepare 내부 context.relocation={packet,relocationHash}로 검증된 원정의 복사본을 전달하고 snapshot.definitions.relocationHash에 결속. 별도 GET/새 API/정의 전문 중복snapshot 저장 없음. 기존 v1–v6 reader 선택을 유지하므로 과거run을 v6로 소급하지 않는다. 원정의 누락/변조는 기존 source_unavailable/source_invalid로 거절한다. 새로운 target-loss schema/환급/재표적 규칙은 추가하지 않았다.
검증 node-target-policy-red-20261030.json은 hash 연결 부재 RED. node-target-policy-20261030.json90PASS/0FAIL/0SKIP, tsc/ESLint/diffcheck0. 실제 START 원정의가 runtime 준비 입력과 snapshot hash에 연결됨을 확인했으나 runtime callback의 행동/effects 값은 합성이다. 공유코어 단계별 실행/실피해/Player 완료의 증거가 아니다. 제품 runtime factory와 독립 도착 verifier는 아직 미등록이며 이 준비 경로를 운영등록 완료로 해석하지 않는다.

<a id="destination-time-source-20261030"></a>
### TRAVEL-ARRIVAL-TIME 서버 시간 원천 및 추가 계약 경계 — 2026-10-30
44#travel-arrival-time-verification-approved-20261030의 방향 승인 적용. 기존 operation_map_receipts에는 시각이 없어 0143_operation_map_destination_times를 추가했다. run_id/event_id가 원receipt 복합키를 참조하고 confirmed_at_ms는 CONFIRM 원자batch에서 DB UTC 시각으로 기록한다. UPDATE/DELETE 금지, 원키 replay는 원시각 유지, 과거receipt backfill 없음. 응답/API 변경·추가GET 없음. 해당 시각은 서버의 목적지 확정 관측이며 진행거리/일시정지/입력진위/도착권위를 증명하지 않는다. 현 승인에서는 상한 검증의 원천으로만 마련했으며 도착 verifier가 이 값을 소비한다고 주장하지 않는다. 실제 DB migration/배포는 하지 않았다.
현재 신뢰 원천: 원START/경로·edge·revision/목적지 명령 원receipt와 신규 확정시각. 물리적 재현: 기존 verifyBArrivalBoundaryReport는 각 ADVANCE/PAUSE/TRAVEL_THROTTLE 결과와 첫경계만 검증. 부족한 원천: 클라이언트 elapsed의 독립 시간 credit, 서버가 수락한 속도·정지 입력 순서·시각 및 원명령 identity. BOriginalJournal은 명시 ORIGINAL_UNITY_LOCAL_NOT_SERVER_ACCEPTANCE이며 hash 일치는 이 결손을 채우지 않는다. BOriginalArrivalStore의 capturedAtMs는 수신 감사시각으로 이동시간으로 쓰지 않는다.
추가 계약 후보(미승인): 기존 인증 명령 경로에서 이동 속도/정지 변경의 원commandId·순서·기대revision·값을 수락/저장하고 원응답으로 갱신. 주기 heartbeat 없이 입력 변경마다 최대1 논리요청이 추가되며 원키 retry는 별도, 최종도착은 기존보고 사용. 서버 수락시점과 로컬 입력시점 중 적용기준, 응답지연 중 표시/명령, 예고없는 연결유실·background 구간의 시간 인정/복구가 결정되어야 한다. 기존 endpoint 확장 여부/정확wire는 이 정책과 함께 별도확정 필요. 승인되지 않은 통신주기/허용오차/자동진행을 구현하지 않았다. 이 후보만으로 실제 Unity 입력기록이 서버수락이 되지는 않는다.
검증 destination-time-red-20261030.json 테이블부재 RED, 첫green에서 SQL.js 동기예외를 비동기로 기대한 시험문법1실패 수정 후 destination-time-20261030.json30PASS/0FAIL/0SKIP. 재전송불변·거절무기록·불변트리거·시각쓰기실패 시 map/receipt 전부 rollback, 기존도착경계 거절/원재현 회귀. tsc/ESLint/diffcheck0. fixture 도착권위는 합성이며 실제교전/API/메모리 검수가 아님.

<a id="travel-command-acceptance-v1-20261031"></a>
### 이동 입력·초기/복구/재개 exact 계약 — 2026-10-31
44 TRAVEL-COMMAND-ACCEPTANCE / TRAVEL-RECOVERY-BASELINE / TRAVEL-RESUME-PENDING 승인 적용. 아래는 Server 구현 계약이며 정책 승인대기가 아니다. 현재 코드 구현/시험 중, 실제 Unity 소비·Player 검수 미완료.
기존 POST /v1/runs/:runId/b-arrival-reports를 재사용. Content-Type: application/json, X-B-Arrival-Version: combat.b-travel-command.v1.20261031, Idempotency-Key=requestId. 인증 계정과 URL run이 권위 scope. 최대 body4096byte, query/If-Match 금지(기존 규칙). GET/heartbeat/새endpoint 없음.
공통 strict JSON: {version:'combat.b-travel-command.v1.20261031',requestId,destinationEventId,nodeId,mapRevision,expectedRevision,kind}. requestId/destinationEventId/nodeId는1–200자. mapRevision은 목적지 확정 지도 revision(양의 safe int), expectedRevision은 이동 수락 revision(최초0). kind='TRAVEL_THROTTLE'에는 bps, kind='PAUSE'에는 value:boolean, kind='RECOVER' 또는 'ARRIVE'에는 추가필드 없음. elapsed/좌표/클라시각/결과를 받지 않는다. 재개는 PAUSE,value:false이며 원정지 결과 확인 후 원키·본문을 단말에 먼저 영속화해야 한다. 새process 복귀는 미결 원키 결과를 먼저 복구한 뒤 새 RECOVER를 보내고, 그 확정응답 revision에서 사용자 의도로 PAUSE,false를 보낸다. 원키 replay 자체는 시간을 갱신하거나 새재개하지 않는다.
일반 성공 응답: {version,requestId,destinationEventId,nodeId,mapRevision,revision,acceptedAtMs,state:{tick,distanceMm,remainderUnits,debtTicks,paused,throttleBps}}. revision=기대값+1은 이 destination의 수락순서이기도 하다. acceptedAtMs=서버 UTC millisecond 관측, 기존확정값은 replay 불변. tick/debtTicks는 원 boundary definition.stepTicks/TimeSpan ticks 체계, remainderUnits는 기존 거리분모100000000000의 나머지다. 표시값 재계산/예상값을 권위로 대체하지 않는다. 원START bArrivalBoundarySource의 정확 step/config/거리/버전을 재사용. 이 executor는 기존 승인 linear fixed-step 범위이며 가감속 모델을 새로 발명하지 않는다.
초기 기준점: 해당 원 boundary source가 있는 신규 CONFIRM의 기존 map receipt에 travelInitial을 선택적으로 포함한다. 형식은 위 성공 응답과 같고 requestId/destinationEventId=CONFIRM eventId, revision=0, acceptedAtMs=동일 목적지시각 저장값, state=검증된 원source의 초기 논리상태/순항속력. 초기0은 기존 이동 executor 원시작 상태이지 클라 fallback이 아니다. 미공급/과거receipt에는 필드를 만들지 않으며 client가 초기값을 추정하면 안 된다. 20261030의 DB SQL시각 설명은 신규코드에서 동일 서버관측을 응답/DB에 함께 넣는 방식으로 보정: 기존 저장값을 재작성하지 않는다. 정상 초기화 추가GET0.
RECOVER는 가장 최신 확정 이동 상태에서 마지막 수락 이후 시간을 버리고 정지 상태를 확정한다. PAUSE,false는 새 서버 수락시점부터 시작한다. 이전 확정 거리/속력/논리 잔여는 유지하고 HP/장비/보상/seed는 수정하지 않는다. 속도변경 미결1개 및 정지의도 순차처리는 Unity 기존 Controller/지속명령 소유. 입력 같은 값은409 unchanged_input이며 별도 명령을 보내지 않는다.
복구 시 지도/도착/종료가 이미 진행된 경우: {version,requestId,status:'CURRENT_MAP',runStatus,mapRevision,mapState,mapReceipt}. 현 revision 원mapReceipt가 있으면 nodeStart 포함 그대로 재사용, 없으면 null(미공급이지 초기화 아님). 이동을 과거로 되돌리지 않는다. FINISH_PENDING은409 transaction_pending으로 원종료요청 복구가 우선. 같은 원키의 과거 수락영수증은 원결과 그대로 돌려주므로 소비자는 최신 revision/계정·화면 세대를 되돌리지 않는다.
ARRIVE 명령은 서버 수락시간·원속력/정지로 만들어진 기록을 기존 물리재현에 통과시키고 서버 original arrival capture 및 기존 arrival report를 공급한다. 성공응답에 arrival(기존 v2 receipt/evidenceRef)를 추가한다. 이는 지도전환 이전 원도착증거이며 클라가 후속 기존 b-route/transition ARRIVE에 evidenceRef를 전달해야 한다. 그 지도전환에서 nodeStart와 원자 확정한다. 정상 확인GET 없음. 입력·원증거·report 단계 사이 실패는 같은 ARRIVE 원키로 복구하고 클라가 새키로 재도착시키지 않는다.
주요409 code prefix combat_b_travel_: request_conflict(원키 다른본문), revision_conflict/state_conflict(경합), map_conflict/head_conflict/time_source_missing, clock_regressed(음수시간을0으로 숨기지 않음), unchanged_input, boundary_not_reached, arrival_report_required(다음입력 대신 도착요청 필요), arrival_already_prepared(원ARRIVE 결과 복구), history_capacity(새변경은거절하되 복구/재개/도착 공간 보존), transaction_pending. 형식오류400, body초과413는 기존route오류. 서버 시각 전진 불연속과 숨긴 offline 이력을 완벽 구별한다는 보장은 하지 않는다. 원시각만으로 자동도착0; 새효과/기하 없는 combat node는 등록된 OrdinaryNodeEntrySource가 거절하므로 실제5인 전투완료가 아니다.
저장:0144 b_travel_commands는 원키/hash·수락시각/순서/상태응답과 해당서버생성이벤트 immutable, b_travel_heads는 최신 bounded 재현 projection이며 원영수증 삭제/변경0. 무진행 중복 제어를 동일 결과로 축약하고 원최저속력의 남은 도착/복구 공간을 보존한다. 전체원장 반복메모리로드0; 원응답/원문은 복구정책상 DB 보존. 전투중 노즐/공격입력과 별개다.
revision_conflict 추가 응답:409 {accepted:false,errorCode:'combat_b_travel_revision_conflict',current:원최신이동응답|null}. current는 원서버 수락 상태이며 새수락/재개가 아니다. 기존 미결 원키부터 복구한 뒤 명시거절된 요청만 정리하고 이 revision에서 필요한 새 RECOVER를 생성할 수 있다. 정상후속GET0, 경합을성공/자동재개로해석0. 별도 새복구조회 endpoint를추가하지않는다.

실구현 검증근거(20261031): [Server 결과](../../handoffs/LAST_RAIL_SERVER_2-D-20260913.md#travel-command-server-result-20261031). 관련회귀61PASS와최종변경범위17PASS는중복합산하지않음. 실제등록된인증route/SQL.js/서버수락검증의시험이며source/시간은합성; 실제Player/네트워크/3회메모리미검수. nodeStartForAuthenticatedContext gate등록과실full5 runtime producer공급을구분. runtime미공급은정책승인대기가아니라구현잔여다.

<a id="arrival-control-order-server-20261031"></a>
### 도착 직전 정지 수락 보충 — ARRIVAL-CONTROL-ORDER-v1.20261031
기존 이동명령 v1 응답의 선택필드 pauseIntent:true 추가. PAUSE,value:true가 서버 수락시점까지 계산한 원이동에서 이미 boundary를 충족하면, 정지를 arrival_report_required로 거절하지 않고 같은 원키/수락순서로 원도착검증을 수행한다. 성공응답에는 기존 arrival/evidenceRef와 pauseIntent:true가 함께 있고 immutable receipt에 보존된다. state는 도착을 증명한 마지막 이동상태(paused:false)를 유지하므로 이 숫자를 전투자동시작 허가로 사용하지 않는다. 도착 이후 새 PAUSE 이동사건/시간차감/거리소급취소는 없다. 조건미충족이면 기존 state.paused=true로 수락하며 arrival은 없다.
소비자는 PAUSE 성공도 arrival을 포함할 수 있음을 처리하고 별도 이동 ARRIVE를 중복발행하지 않는다. 이후 기존 b-route/transition 지도ARRIVE는 여전히 필요하며 그 nodeStart 확정 전에는 전투를 시작하지 않는다. 저장된 pauseIntent/원사용자의 로컬정지의도는 Controller가 보존해 지도확정 뒤에도 전투개시를 보류한다. 계속하기는 기존 foreground/준비완료 owner에서한번만처리, 새nodeStart/HP초기화0. 지도ARRIVE 후 늦은 이동속도/정지는 거절하고 상태를 바꾸지 않는다. UI역순/재바인딩·새process정지의도 보존은Unity소유이며Server시험으로완료주장0.
같은 expectedRevision의 PAUSE/ARRIVE는 동일existing CAS로 최대1개만수락. authority증거발행과 지도ARRIVE/nodeStart확정은별개단계. 검증 arrival-control-20261031.json21PASS/0FAIL, 최종늦은정지case focused1PASS(중복합산0), tsc/ESLint/diffcheck0. 경계299/300/301ms는합성원경로fixture값이며새허용오차/운영밸런스아님.

<a id="travel-initial-http-projection-fix-20261031"></a>
### 목적지 HTTP 초기상태 전달 수정 — 2026-10-31
기존 POST /v1/runs/:runId/operation-map/destination의 헤더없는 v1 응답과 X-Map-Destination-Version:operation.map-destination.v2.20261019 응답 모두 최상위 travelInitial을 공급한다(원receipt에있을때만). 기존29 Server시험으로서로다른응답projection과원키재시도동등을검증. 수정전에는mapreceipt에는있어도둘다HTTP에서빠졌으며이번에해소했다. 새버전/새GET/초기값재생성없음.
projectDestinationResponse가 저장된 mapReceipt.travelInitial 한곳을재사용한다. v2 destinationSchema도선택필드를허용한다. 기존mapReceipt.destination 안에별도복사저장하지않으며, 과거원receipt 최상위에값이있으면같은원키응답에서도그값을복원한다. 원receipt에없으면미공급그대로이며과거run에새초기값을붙이지않는다. 원JSON해시/수락시각/정적source는재생성하지않음. 정확초기상태필드는travel-command-acceptance-v1-20261031 그대로다.

<a id="ordinary-node-enemies-v1-20261031"></a>
### Node-entry 적 원천 구성 — 2026-10-31 / 부분 구현
적 초기 발행은 `ordinary-node-enemies.ts:prepareOrdinaryNodeEnemies`가 원 START의 검증된 firstBattleEncounterSource/profile과 같은 ARRIVE의 firstPlayableScaling에서 구성한다. sourceHash/profileHash/실제 원 START hash/account/run/node/arrivalEventId/mapRevision 및 원 risk·depth 구간별 배율을 대조한다. 자가 hash를 다시 계산한 임의 HP/피해도 원 배율과 다르면 거절한다. 새 배율/별도 적 ID 원장은 만들지 않고 기존 `firstObjectiveEncounterIdentities`와 동일한 ID를 사용한다.
- `version: ordinary.node-enemies.v1.20261031`, `scalingBindingHash`: 원 ARRIVE 배율 결속.
- `actionDefinition`: 원 profile.enemies.definition 전체. 원 사수의 2000ms 주기와 `TARGETABLE_PLUS_FULL_PERIOD`, 사거리/LOS·발사 전후 표적소실 규칙 등 기존 정의 유지. 실제 표적/발사시각은 이 정의로부터 실행 owner가 판정해야 하며 진입시각=발사시각으로 만들지 않는다. 원 정의의 runtimeSupported:false도 임의 true로 바꾸지 않는다.
- `gunners[2]`: 기존 원 issuedGunnerIds 순서의 instanceId, 원 적용배율의 hp/maxHp/rawDamage(exact numerator/denominator), 원 defense. 이미 배율이 반영된 값이므로 소비자가 재적용하지 않는다.
- `artillery`: 기존 issuedHazardId를 hazardId로 사용, 원 launcherHp의 hp/maxHp, artilleryRawDamage, 원 artillery definition 및 minimumThreatClearToPreviewMs/previewRequires. 이는 단발 사건의 초기 입력이며 preview/발사/명중/종료를 발생시켰다는 증거가 아니다.
`OrdinaryNodeEntrySource`는 firstPlayableScaling이 공급된 노드에서 위 원천을 생성해 runtime owner의 `enemies` 입력으로 전달하고 반환한 state.enemies가 원값과 정확히 같은지 확인한다. 전달 객체는 clone하므로 runtime의 입력 변조가 비교 원본까지 바꾸지 못한다. 저장은 기존 완전한 node snapshot/ARRIVE batch 한 곳만 사용하며 별도 조회/명령/중간 원천 저장은 없다. restart는 저장된 원 nodeStart를 재사용한다. 이 호출은 기존 검증된 ARRIVE 내부용이며 임의 클라이언트 scaling의 권위 검증 API가 아니다.
현재 실제 runtime owner 미등록 gate는 유지된다. effects/actions/settings/spatial가 없는 상태에서 적 packet만으로 nodeStart를 확정하지 않는다. 새로운 AUTO OFF/작전 보존 제안은 미승인. 초기 shield/시간제 효과·현재 쿨다운/충전 상태와 actual targetability·총구/hit-volume·Unity Geometry 연결은 별도 잔여다. 기존 HP/stat 초기 영수증은 이 상태 전체의 부재/0을 증명하지 않는다. 실제 Player 교전/포격/종료 검수 완료를 뜻하지 않는다.
<a id="ordinary-run-actions-v1-20261031"></a>
### 일반 작전 AUTO·쿨다운·충전 원천 및 완료 이관 — 2026-10-31
ORDINARY-ULTIMATE-AUTO-OWNERSHIP / NODE-ACTION-CARRYOVER 승인 적용. `ordinary-run-automation.ts`가 기존 START/초기화/COMPLETE/nodeStart 경계에서 하나의 확정 상태를 소유한다. 신규 ordinary START transaction에서 migration0145의 ordinary_run_automation row(revision1/AUTO OFF/actor별 액티브준비완료·충전0)를 생성한다. 원 policy.firstActiveReadyMs=0/initialCharge=0을 확인하고 기존 CombatUltimateState.start의 실제 초기충전을 사용한다. START 원snapshot/응답에 대용량 중복을 추가하지 않고 기존 ORIGINAL_START 초기화 응답의 `ordinaryAutomation`으로 공급한다. 신규 첫 node-entry는 같은 row를 읽는다. 과거 row없음은 미공급이며 false/0으로 backfill하지 않는다.
`ordinaryAutomation` 및 nodeStart.snapshot.automation:
- version=`ordinary.run-actions.v1.20261031`, revision=양의 safe integer, ultimateAuto=boolean.
- actors=원편성5개 ID, 각각 operatorId/activeCooldownRemainingTicks/ultimateCharge. 잔여는 TimeSpan ticks(1ms=10000), 충전은 원개인충전. 승인용량100/200·사용100·O11 적용의 실제 적격성/소비 검증은 기존 권위 전투owner 책임이며 이 저장경계는 충전0..200 표현범위만 추가 검사한다. 기본공격 기회/현재 시전/실드/효과를 이 필드로 추측하지 않는다.
`OrdinaryNodeEntrySource`는 AUTO/액티브잔여/충전을 runtime.actions와 대조하고 다르면 거절하며 관찰 row의 revision/원본문을 ARRIVE batch에서 fence한다. 동일memory pause는 실제 실행 owner의 현재값, 동일노드 restart는 기존 immutable nodeStart를 재사용한다. 현재작전 row로 저장된 노드시작을 덮지 않는다.
정상 전투노드(COMBAT/ELITE/BOSS) COMPLETE의 **내부 신뢰 proof**에 ultimateAuto와 actions(위 actor배열)를 공급한다. FirstObjectiveEvidenceOwner의 기존 evidence에도 같은 optional 필드를 정의했으며 신규 row가 존재하면 누락은 ordinary_automation_source_conflict로 거절한다. 공개 COMPLETE 요청은 그대로이고 이 필드를 클라이언트가 임의 POST하는 경로는 추가하지 않았다. 실제검증 owner는 종료시점/시도세대/O11용량·소비/타이머와 최종값의 원기록을 증명하고 원기록 assertion을 제공해야 한다. 파싱/자가hash는 그 증명이 아니다.
확정 actions를 원 actorID로 맞추고 기존 row revision+본문을 fence하여 map COMPLETE/원영수증과 같은 batch에서1회 update한다. 완료 응답 receipt.ordinaryAutomation은 변경후 확정값이다. 원키재전달은 기존 영수증을 반환하고 재update/재소비하지 않는다. 다음node는 갱신된 row를 읽고, 정비·보상·로딩·wall time/travel acceptedAtMs만으로 쿨다운차감/충전가산0. 별도 endpoint/성공GET/토글당서버통신/폴링0. 단일 자동·수동 소비 및 ON/OFF의 실제 행동 적용은 공유 전투 실행 owner가 담당한다.
실제 공유 실행기의 종료기록 검증 producer, 완전한 runtime.actions의 기본공격 첫기회/시계, effects·기하/full5 host는 여전히 미완료다. 이번 필드와 합성 신뢰 evidence fixture를 실제 Player 동작으로 승격하지 않는다. migration0145는 로컬 fixture에만 적용했고 운영DB/배포는 하지 않았다.
<a id="ordinary-cooldown-unit-boundary-20261031"></a>
### node-actions / run-actions 쿨다운 단위 경계 수정 — 2026-10-31
Unity830 지적 확인: 기존 node-actions v1.20261030은 논리 step 수, run-actions v1.20261031(ordinaryAutomation)은 TimeSpan tick(1ms=10000)이다. entry-source의 직접 숫자비교는 결함이므로 수정했다. 두 버전의 의미를 바꾸거나 저장된 원본문을 다시 쓰지 않는다.
- 신뢰 runtime producer 입력 `entry.automation.actors[].activeCooldownRemainingTicks`는 TimeSpan tick이다. producer는 자신이 실제 사용하는 원 stepTimeSpanTicks로 `cooldownTimeSpanToNodeSteps`를 적용해 node-actions v1을 구성해야 한다. Server entry 대조도 같은 함수로 검사한다. 기본공격 first opportunity는 별도 원행동 source의 논리step이며 이 변환으로 새 기본공격값을 만들지 않는다.
- 변환은 ceil(remainingTimeSpanTicks / stepTimeSpanTicks), 정수 BigInt 연산이다. 50ms=500000 TimeSpan tick에서4초=40000000→80step, 1→1,499999→1,500000→1,500001→2. 0만0으로 유지한다. 기존 CombatSkillCommands.ToTicks와 OrdinaryFiveCombatExecution.T가 원시간을 논리step으로 올림하는 실행규칙을 재사용한 것이다. 조기준비완료 금지.
- 신뢰 전투 완료 producer는 실제 남은 논리step을 `cooldownNodeStepsToTimeSpan`(steps × 원stepTimeSpanTicks)으로 정규화한 후 기존 COMPLETE evidence.actions에 공급한다. 이미 run-actions 단위인 값을 또 곱하지 않는다. COMPLETE 저장 owner는 기존 TimeSpan 계약만 수락하며 단위를 추측해서 재변환하지 않는다. 검증된 producer가 아직 없다는 사실은 유지한다.
- 두 helper는 음수/소수/unsafe integer/0 또는 비정수ms step을 거절하고 역변환곱의 safe integer 초과도 거절한다. 잘라내기/overflow wrap0. 1step 미만 잔여의 최초 ceil은 원실행 양자화이며, 다시 TimeSpan→동일step 왕복하면 증가하지 않는다.
- restart는 이미 확정된 nodeStart v1의 논리step 원값을 그대로 읽는다. 현재 run-actions로 재구성하거나 저장 snapshot을 다시변환하지 않는다. 로딩/정비/벽시계/이동 acceptedAtMs를 끼워넣지 않는다.
검증: cooldown-units-red의 기존단위대조3FAIL+helper미구현7FAIL→cooldown-units-regression-20261031.json97PASS/0FAIL,별도cooldown-units-restart-20261031.json1PASS. v6/v7/v8 원배율 fixture에서 신뢰종료80step→40000000저장→node80step 통과/40000000을그대로node에넣으면거절. 재입장원값80과원키replay/두번째restart불변 확인. 이는SQL.js/합성runtime source이며UnityPlayer통과아님. Unity OrdinaryNodeActions reader의기존논리step 계약은유지; 실제consumer/완료producer 연결은해당소유자가같은계약으로적용해야한다.
<a id="ordinary-node-enemies-spatial-v2-20261031"></a>
### 실제 적 공간 원천 결속 — node-enemies v2 / 2026-10-31
COMBAT-ANCHOR-GEOMETRY-BINDING 승인 적용. `prepareOrdinaryNodeEnemies`는 원profile이 적공간을 포함한 firstBattle source v7/v8이면 신규노드에 `ordinary.node-enemies.v2.20261031`을 발행한다. 기존v1의필드를그대로유지하고 `spatial:{physicalInstanceId,definition}` 추가. definition은 기존 `firstEnemyTrainSpatialDefinition` 원문/definitionHash이며 차체12000×3000×3200mm, 앞열차간격4000mm, 사수후미offset2000/6000mm·footAnchorY3213mm/측정roof3212.5mm, 실제아군motion추종조건과原geometry hash를재사용한다. 새로운위치/총구/공통box를발명하지않음.
원profile의spatial version/hash, ARRIVE.enemySpatial의정의hash/geometryhash, 기존account/run/arrivalEventId/sourceHash에서재생성한physicalInstanceId를모두대조한다. 자체bindingHash를재계산해도다른기하/도착instance를허용하지않는다. 기존node-entry의runtime 입력/출력일치검사에도포함된다. v6이하처럼원적공간없는profile은v1/미공급그대로이고, 기존저장nodeStart v1/재입장snapshot은재발행/수정하지않는다.
주의할호환경계: 현재Unity OrdinaryNodeEnemies는v1 strict필드reader다. v1에필드를몰래추가하지않고v2로발행하므로Unity소유자가v1읽기를유지하면서v2소비를연결해야한다. 현재완전runtime gate는그대로이며현재v1reader가v2를지원한다고주장하지않는다. v2는원공간정의공급이지실기하adapter실행증거가아니다.
정확미공급: 원definition.muzzle와hitVolume은null, executionVerified=false. 발anchor를총구로겸용하거나발생점/피격영역을원시스템대신판정하지않는다. 실제능력별anchor/collider/반전·발사사건/LOS·사거리계산은Unity실행adapter/factory소유로연결·검수해야한다. 원차체/발anchor존재와총구/피격영역미공급을구분한다.
<a id="ordinary-native-geometry-registration-v1-20261031"></a>
### 첫 전장 측정 정의 등록·노드 원본 결속 — 2026-10-31
정확 schema/검증 구현: `ordinary-native-geometry.ts`, 저장 migration0146. 현재 등록된 제품 정의/승인 pin은 없음. 아래는 Unity 제작물이 공급될 때 사용할 내부 등록 경계이며 측정후보 승인/실행지원 선언이 아니다.
정의 JSON 필드(strict):
- version=`ordinary.native-geometry-definition.v1.20261031`, definitionId.
- units=`MILLIMETERS`, millimetersPerUnit=실측 Unity 단위환산계수(positive finite, 기본값없음), axis=`X_FORWARD_Y_UP_Z_SIDE`, coordinateSpace=`BOUND_FRAME_LOCAL`.
- source={scenePath,sceneHash}. path는실제Assets/... 경로, hash는원파일SHA256 64hex. scene/prefab 존재·내용측정은Unity/제작검수근거로확인하며서버가경로문자열만으로실파일검수했다고주장하지않음.
- entities[]: authoringId(정의내유일), role(OPERATOR/ENEMY/TRAIN/DEVICE/WORLD), definitionRef(원객체정의ID), framePath(원asset/scene안바인딩부모Transform경로), assetPath/assetHash, anchors[], shapes[]. authoringId는실행entity/physicalInstance ID가아니다.
- anchors[]: id(해당entity내유일), role(FOOT/EMISSION/HIT), positionMm=[x,y,z]. EMISSION만abilityId를필수로추가하며원능력ID와연결한다. 발좌표를발사점으로자동대체하지않는다. 방향/표적고정시점은원능력실행정책을사용하고이point데이터로새공통탄도를만들지않는다.
- shapes[]: id(해당entity내유일), role(HIT_VOLUME/OCCLUDER), kind=BOX이면centerMm=[x,y,z], sizeMm=[전체x길이,전체y길이,전체z길이](모두positive). kind=CAPSULE_Y이면centerMm/positive radiusMm/전체 heightMm(양끝반구포함, height>=2radius). 선언한bound frame에서축정렬인형상만이v1로표현한다. 기울어진/다른형상을서버에서이box/capsule로근사하지않는다. 형상은실측기존승인값으로공급하고공통box기본값없음.
점은finite수치이며서버반올림/화면픽셀역산0. world변환은실제frame의원Transform과단위환산계수를한번사용한다. 표현root반전1회/고정세계표적불변은기존계약을유지하고서버정의에카메라반전좌표를써넣지않는다. registry는구조유효성검사이며모든능력/요원에필수anchor/shape가있는지와실행연결은runtime owner가별도검증한다. 부분측정정의등록만으로gate통과불가.
등록은 `new OrdinaryNativeGeometryRegistry(db, approvedReference)`의내부register(definitionJson). approvedReference={definitionId,version,definitionHash}는측정후검토된서버설정에서독립공급하며클라이언트/후보JSON의자가hash를그대로승인pin으로삼지않는다. hash는Unity가공급한정확JSON UTF8 bytes SHA256이고서버가재직렬화한hash로대체하지않는다. 최대524288bytes. immutable (ID,version), 동일내용원키재등록가능/다른내용덮기거절/UPDATE·DELETE금지. 반환 executionVerified:false. 공개등록endpoint/키발급/후보자동승인/운영등록은추가하지않음.
노드연결: OrdinaryNodeEntrySource의내부세번째인자registry를구성한경우검증된원definition과아래binding을runtime.prepare의geometry에함께전달한다. runtime.spatial.definitionBinding은정확일치해야하며별도clone으로입력변조격리. registry원JSON/hash조건을ARRIVE와같은batch에서검사한다.
`definitionBinding={version:'ordinary.node-geometry-binding.v1.20261031',accountId,runId,nodeId,entryMapRevision,originGeneration:1,definitionRef:{definitionId,version,definitionHash}}`.
originGeneration은저장된첫node원본의세대다. restart의현재generation은기존node-restart응답/ordinary_node_attempts가권위소유하며원binding을2/3으로덮지않는다. 실제발사/접촉/HP사건은현재attempt wrapper와actor generation에결속해야하고originGeneration=1을현재시도로재사용하지않는다. physicalInstanceId는기존원rail/적v2 발행ID를사용하며Unity오브젝트생성순서로발행하지않는다. authoringId→실entity/physicalinstance 매핑·프레임수명은실runtime owner검증대상이며이번등록metadata가그검증을대체하지않음.
현재index의완전runtime owner/registry pin은주입하지않았다. 원제품측정정의가미공급인상태에서가짜nonnull Geometry나빈effects로release하지않는다. 새등록형식은기존적기하v2/구v1/원nodeStart를수정하지않고실제adapter소비와Player검수는남아있다.
<a id="ordinary-geometry-run-pin-v1-20261031"></a>
## 기하 원 START 선택 보존 — GEOMETRY-VERSION-RUN:SERVER:20261031
44#geometry-version-run-compatibility-approved-20261031 적용. 기존 native registry 등록계약 유지, 제품 pin 발행 없음.
- 출발 내부 owner `startDeparture`의 마지막 선택 인자 `ordinaryGeometry`는 독립 승인 registry다. 실제 ordinary 출발에서 검증된 원 bytes를 읽고 `ordinaryGeometryReference:{definitionId,version,definitionHash}`를 기존 START 응답 및 snapshot에 함께 저장한다. 등록 원 bytes/hash assertion은 출발 동일 transaction에 포함한다. createResponse로 이 필드를 주입하면 ordinary_geometry_source_conflict. public 입력/새 endpoint 없음.
- START 재시도는 현재 registry를 조회하기 전에 저장 응답을 반환한다. `OrdinaryNodeEntrySource(db,runtime)`는 더 이상 최신 registry 인자를 받지 않고 계정/run 검증된 원 START의 ordinaryGeometryReference로만 정의를 조회한다. 다음 노드도 같은 참조를 사용한다. 같은 노드 restart는 기존 immutable node origin 복원 경로 유지.
- 필드가 없는 과거 START는 부재를 유지한다. runtime이 임의 definitionBinding을 추가하거나 원 node/run/hash 결속을 변경하면 ordinary_node_geometry_binding_conflict. 등록 원본 누락/hash 불일치도 기존 registry 오류로 중단한다. 구 정의 덮어쓰기/삭제/저장 마이그레이션 없음.
- 현재 version 필드는 지원 wire schema인 ordinary.native-geometry-definition.v1.20261031이다. 이 schema에서 새 내용은 별도 definitionId와 hash로 게시하며 같은 ID/version 내용 변경은 계속 거절한다. 새 wire version 지원/제품 publication 설정/Player 지원목록 대조를 구현했다고 주장하지 않는다.
- 회귀: QA/CommanderS1-20260922/geometry-run-regression-20261031.json 94PASS/0FAIL, 실제 START SQL.js의 원응답 재시도·새 작전 새 참조·다음 노드 기존 참조·무참조 runtime 주입 거절 포함. 기존 registry/노드 재입장 시험 포함. tsc(game-api), scoped ESLint, diff check exit0. read-only review 중대결함 없음(리뷰어 시험 미실행).
- 제품 route에는 승인 registry/실 runtime 미설정 상태다. 835 후보 총구/사수캡슐 pin 등록0, 실 asset 검수/능력coverage·Player 동일정의 지원·actual factory/native공격/LOS/피해/effects/완료producer·실교전/API메모리 미완료. 이번 시험은 합성 정의이며 제품 기하 승인/전체 통합 검증이 아니다.

<a id="gunner-body-identity-boundary-20261102"></a>
## GUNNER-VISUAL-HIT-ROLE:SERVER:20261102 — 원 ID 조사 및 역할 경계
44#gunner-visual-hit-role-approved-20261102 적용. 실제 조회한 적열차 원본에는 별도 definitionId/definitionRef/trainId가 없다. 따라서 ‘기존 등록된 차체 ID가 있다’는 전제는 확인되지 않았으며 버전/hash/사수 ID를 차체 definitionRef로 공급하지 않는다.
원천 대조: unity-project/Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/definition.json 전체, SHA256 2c212a13653c767cc4804fc1bbd94fdb9bf19c37507076ef1c4cbf8e0cda3fa1; EnemySpatial805/verification.json 측정근거; Server combat-first-enemy-spatial.ts 전체. 원 authoring은 version/approval/측정/physicalInstanceId:null, Server spatial은 version/definitionHash/geometry.version/geometry.definitionHash를 소유한다. firstEnemyTrainInstanceId는 account/run/ARRIVE/sourceHash로 발급하는 전장별 물리 인스턴스이며 정적 definitionRef가 아니다.
사수 정의 ID는 combat-first-playable-profile.ts가 bScoutGunnerDefinition에서 찾는 enemy_spike_cart_gunner다. 사수 2명의 runtime instanceId는 firstObjectiveEncounterIdentities의 gunner-0/gunner-1 원 발급값이며 HP/처치를 소유한다. 포좌 표현을 독립 HP로 신설하지 않는다. 기존 단발 포격 hazardId/launcherHp는 별도 원 목표 계약으로, 사수의 장착 포좌와 혼동하지 않는다. 차체 physicalInstanceId에는 이 계약으로 HP/처치/보상을 추가하지 않는다.
ordinary-node-enemies.ts는 원 START profile/ARRIVE 검증 후 사수 2개와 별도 artillery, 선택 spatial 정의를 전달한다. 완료 verifier는 issuedGunnerIds에 속한 원 사수의 처치만 수용하고 hazard에는 원 issuedHazardId만 수용한다. 차체/사수/포격 ID 혼용은 거절하며 기존 owner를 재사용한다. 사수 사망 후 신규 발사 중지·표현/차폐 실행은 실제 runtime owner가 담당하며 이 서버 검증만으로 연결 완료가 아니다.
Unity836 train-bodies.definition.json exact SHA256 82e192999cbaeef1308ed395314eb2ae165e437da068a5c7a49c88ea2bfaa0f5 재확인. 아군 train_a/car_combat/car_gun 3차체 부분 산출물이며 제품 pin/적 차체 정의 등록 없음. 적 차체를 strict export에 추가하려면 정적 원 ID를 신규로 명시하는 소유 계약이 필요하다. 기존 hash 원본을 덮거나 추정 ID로 채우지 않는다. 이 누락은 등록 경계의 미해결 항목으로 보존한다.
실행 현황 재확인: index.ts는 OrdinaryNodeEntrySource(DB)로 runtime 미주입, operation-map-command-routes의 FirstObjectiveEvidenceOwner도 실제 원사건 producer가 필요하다. 새 ordinary-render-source-binding 제안은 적용하지 않았다. 노드 시작 원본 재입장 유지, 전투 중간 탄/위치 복원으로 변경 없음.

<a id="first-enemy-body-static-id-v1-20261102"></a>
## RENDER-SOURCE-BINDING:SERVER:20261102 — 적 차체 신규 정적 ID 발급/등록 연결
사용자 신규 ID 계약 지시에 따라 Server가 발급했다. 앞 절의 ‘신규 ID 계약 필요’는 본 절로 해소되며 Plan 재결정 대기가 아니다. 실제 server apps/packages 및 Unity _LASTRAIL의 ts/cs/json/sql에서 동일 ID 충돌 없음을 검색했다.
- 정적 ID: `enemy_train_first_battle`
- 정의 version: `combat.first-enemy-body.v1.20261102`, role: `TRAIN_BODY_OCCLUDER`
- 정의 canonical SHA256: `9aa18ad04449d1ef244738794ed1da991295583e28dc389fcec6307cff4d9864`
- 원 spatial reference: `combat.first-enemy-spatial.v1.20261028` / `d43e8ad7d306ee670e6660f9c48f452dddbe171e26def7e94863d20dc0cf0f2c`
- 원 authoring reference: `ordinary.enemy-train-authoring.v1.20261028` / `2c212a13653c767cc4804fc1bbd94fdb9bf19c37507076ef1c4cbf8e0cda3fa1`
- 원 asset: `Assets/_LASTRAIL/Content/Battle/OrdinaryEnemyTrainV01/OrdinaryEnemyTrainV01.prefab`, SHA256 `5844aaa8e930bc2b97c0fd831fa0acddf2b7cf60900b1585670c5f2f5e992d02` 직접 대조.
실제 생산 함수는 combat-first-enemy-spatial.ts의 firstEnemyTrainBodyDefinition(). 출력 전체는 QA/CommanderS1-20260922/enemy-body-catalog-20261102.json. 위 definitionHash는 hash 필드를 제외한 canonicalJson의 SHA256이며 보기용 JSON 파일 bytes hash와 혼동하지 않는다. 기존 firstEnemyTrainSpatialDefinition/START profile/original authoring bytes 변경 없음. 구 저장본에 새 ID를 덧씌우거나 sourceHash 재작성 없음.
Unity strict export의 적 차체 entity.definitionRef에 위 정적 ID를 사용하고 role=TRAIN, shape.role=OCCLUDER, 원 prefab path/hash를 공급한다. 형상은 최소1개, anchor는 필요한 실제 FOOT만 가능; 무기 EMISSION/HIT, 차체 HIT_VOLUME 또는 ENEMY 역할로 넣으면 ordinary_geometry_enemy_body_source_conflict. 동일 export 안의 이 정적 차체 entity는1개. 사수는 기존 enemy_spike_cart_gunner 정의와 사수 발급 instance를 유지하며 차체 ID로 교체하지 않는다. 사수 총구/몸체 기하 승인이나 새 HP/보상을 본 ID로 만들지 않는다.
OrdinaryNativeGeometryRegistry의 실제 register/read 공통 검증에 위 역할/원자산 대조를 연결했다. 독립 승인 전체 export pin 및 원bytes 해시 검증은 기존대로 유지한다. 카탈로그 ID 발급은 Unity836 아군3차체 부분 export나 835 총구/캡슐의 제품 pin 승격이 아니다. 등록 후보는 새 export ID/hash로 구분하고 기존 자료는 보존한다.
OrdinaryNodeEntrySource는 원 START geometry에 이 ID가 있고 현재 노드에 firstPlayable 적 구성이 있을 때 원 spatial/geometry hash와 대조한 후 runtime에 기존 geometry/enemies 입력을 전달한다. 적용 전장에서는 runtime이 이 정적 body와 enemies.spatial.physicalInstanceId를 연결해야 하며 물리instance를 정적ID로 사용하지 않는다. 다른 encounter에 첫 전장 enemy 구성을 강제하지 않는다. v6/v7/v8 구 source/노드 packet version 변경 없음. 이 ID 없는 기존 registry/START는 종전 검증 유지. 다른 형상으로 개정 시 새 정의/참조 계약 필요, 같은 원bytes 덮기 금지.
실행 연결 한계: index의 node runtime 및 실제 완료 evidence producer는 아직 공급되지 않았다. CombatCheckpointStore는 STORED_UNVERIFIED/NOT_PERFORMED 저장이며 이를 전투 검증으로 승격하지 않는다. native 공격/LOS/HP/effects 원사건을 생산하는 full5 owner와 승인 역할 기하·표현 결속이 실제 남은 의존이다. 기존 정보/노드시작 재입장 유지, 중간탄/위치복원으로 전환0.

<a id="ordinary-completion-health-owner-20261102"></a>
## GUNNER-DEFEAT-PRESENTATION:SERVER:20261102 — 완료 경계 실제 HP 소유자 결속
실제 operation-map completion route → OrdinaryFirstObjectiveVerifier → CombatHealthStore.read 경로를 연결했다. 원 사수/포격 사건 및 잔탄/위협 검증은 기존대로 유지한다. 완료 evidence의 targets6개(아군5+아군열차)는 실제 저장 원소유자의 현재 hp/maxHp와 모두 일치해야 하며 불일치/읽기경합은 ordinary_objective_health_source_conflict로 거절한다.
기존에는 trusted evidence owner 계약만으로 HP 상태일치를 위임했고 verifier는 대상ID/생존범위만 확인했다. 잘못된 targets HP가 수용되는 RED를 확인해 독립 경계 검증을 추가했다. 실제 CombatHealthStore의 account/run/START/schema 검증을 재사용하며 별도 HP 캐시/원장을 만들지 않는다.
COMPLETE 동일 transaction에 health row의 revision/initial_hash/start_snapshot_json/state_json/actor_state_json exact guard를 추가한다. 확인 이후 HP/원본이 변경되면 map 완료·automation/receipt 저장이 롤백된다. 같은 명령 재시도는 OperationMapStore의 기존 원응답 재생을 우선하므로 완료 후 현재HP 재읽기로 원응답을 바꾸지 않는다. 새로운 wire 필드/endpoint/GET 없음. 증거 producer는 HP를 실제 owner에 확정한 다음 그 revision의 최종값을 공급해야 한다. 미확정 HP 제안만 완료 evidence에 넣거나 guard에 HP 갱신 SQL을 섞어 허용받는 경로가 아니다.
범위 한계: targets는 사수HP가 아니다. 사수의 원피해/처치 사건 자체, 잔탄 해소, 별도 포격 결론을 생산하는 trusted owner는 여전히 필요하다. 단순 클라이언트 주장/표현종료/CombatCheckpointStore STORED_UNVERIFIED는 대체 원천이 아니다. 이 수정은 실제 HP 검증 호출 연결이며 full5 공격·LOS·사수 사건 producer가 만들어졌다는 뜻이 아니다.
검증: QA/CommanderS1-20260922/completion-health-owner-red-20261102.json stale HP 수용으로1FAIL → completion-health-owner-20261102.json v6/v7/v8 3PASS/0FAIL/73filtered. stale HP/원역할ID혼동/잔탄·위협/동시health revision변경/실패시원완료receipt없음/기존원key복구 포함. tsc(game-api), scoped ESLint, diff check exit0. 독립 읽기전용 리뷰 추가결함 없음(리뷰어 시험실행0).

<a id="ordinary-damage-producer-boundary-20261102"></a>
## GUNNER-DEATH-FIRE:SERVER:20261102 — 준비된 HP 연결점과 미공급 원사건
44#gunner-death-fire-boundary-approved-20261102 적용. HP 단일 소유권 구현은 승인완료이며 새 정책 승인 대기가 아니다. 실제 서버의 CombatHealthVerifier 구현/CombatHealthStore 호출부를 검색해 초기화·장비최대HP·lifesteal·WorldBossRailHealthAdapter를 추적했다. 일반5인 원발사/명중/사수처치 사건을 생산하는 운영 구현은 확인한 game-api 경로에 없다.
기존 연결점을 재사용한다: CombatHealthStore.prepareChanges(accountId,runId,commands). ordered command는 eventId/expectedRevision/targetId/change 및 attempt:{nodeId,generation}; ACTUAL_HP는 실제 판정 뒤의 최종HP를 받으며 독립 CombatHealthVerifier.prepare가 동일 원START/대상/원사건/HP조건을 증명해야 한다. prepareChanges는 권위 명중 판정기가 아니며 임의HPsetter로 공개하지 않는다. producer의 원사건 assertion과 returned statements를 같은 transaction으로 확정하고 반환된 health receipts를 사용한다. 같은 사건ID의 원응답 복구, 부분 재생 거절, 현재 attempt fence 및 최종 row guard를 제거하지 않는다.
현재 ordinary 초기화는 아군5+열차6대상이며 node entry/COMPLETE도 이 원소유6대상 계약이다. 사수2명/차체/별도포격을 이 배열에 임의 추가하면 초기hash·대상·재입장/완료 계약을 깨뜨린다. Unity B loop의 HP와 ordinary Vitals 통합은 해당 실행 소유자가 진행 중이며 서버의 독립검증/저장권한과 구별한다. 사수의 원피해/처치 결과를 서버에서 검증하려면 동일 원node/generation/발사·명중 사건을 재현·증명하는 실제 producer가 필요하다. 표시 HP나 사망 콜백은 그 증명이 아니다.
재사용하지 않은 경로의 이유: CombatDamageStore(v1)는 같은 combat_damage_states를 다른 schema로 소유하므로 현재 v2 상태 위에 초기화/덮어쓰기하지 않는다. WorldBossRailHealthAdapter는 LOCAL_TEST_PROFILE/전용 rail journal 계약이며 일반5인 실증거로 승격하지 않는다. CombatLifestealResolution도 별도 trusted source 주입이 필요하고 전체 사수/잔탄/포격 완료 producer가 아니다. CombatCheckpointStore의 STORED_UNVERIFIED 역시 원사건 대체 불가.
완료 입력은 기존 FirstObjectiveEvidenceOwner.prepare→OrdinaryFirstObjectiveVerifier→OperationMapStore COMPLETE 경로 유지. 원 issuedGunnerIds의2처치, 원 hazard 결과, unresolvedProjectileIds/remainingThreatIds, 최종6HP, 현재attempt, actions/AUTO 및 원 checkpoint 출처를 실제 owner가 증명해야 한다. 직전49#ordinary-completion-health-owner-20261102의 실제HP 대조/원자guard 연결은 유지한다. 새 사건순서·지연시간·HP원장·매명중API를 추가하지 않았다.
이번 조사에서 Unity838 factory/execution 동시편집을 하지 않았다. 실기하/공격/피해·처치/잔탄/포격 원사건 생성과 server 검증 가능한 수락 원본이 아직 없으므로 운영 runtime/evidence 인스턴스를 생성했다고 주장하지 않는다. 기존 정적 차체ID 및 GeometryExport838 부분형식 공급은 완료된 범위 그대로 재사용한다. 실제 Player/API/메모리 검수는 아직 실행할 준비가 되지 않았다.

<a id="ordinary-objective-result-acceptance-v1-20261102"></a>
## DAMAGE-RESULT-CONSUMER:SERVER:20261102 — 검증 결과 수락과 COMPLETE 원자 확정
실제 구현: ordinary-objective-result-store.ts의 OrdinaryObjectiveResultStore, migration0147 ordinary_objective_results. 기존 OperationMapStore.commit의 실제 COMPLETE transaction과 기존 공개 completion 원응답 재생 경로를 재사용한다. 클라이언트 HP/증거 upload endpoint는 만들지 않았다.
내부 실행 연결은 `new OrdinaryObjectiveResultStore(db, trustedExecutionEvidenceOwner).accept(context)`다. context는 기존 OperationMapVerifier의 accountId/runId/startSnapshot/plan/state/COMPLETE command. 실제 DB에서 구성한 context와 canonical hash가 일치해야 한다. source.prepare(context)는 독립 원발사·명중·처치·잔탄·포격을 검증/재현한 FirstObjectiveEvidenceOwner 결과와 현재 보류조건 assertions를 반환해야 한다. 원Unity 결과를 검사 없이 그대로 evidence로 바꾸는 adapter는 금지한다. 이 trusted execution/replay owner 및 운영 accept 호출은 아직 미공급이다.
새 writer는 기존 OrdinaryFirstObjectiveVerifier의 원START/profile/ARRIVE·사수2/포격·잔탄/위협·현재6HP 대조를 수행하고, 결과 record INSERT를 검증 assertions와 함께 OperationMapStore에 넘긴다. 이로써 원source/hold assertions, 실제HP, automation, ACTIVE계정/run, 원snapshot/map plan/state/revision, 현재node attempt, 결과 불변저장, COMPLETE/map/automation/원receipt가 모두 같은 transaction에 속한다. source의 현재 보류조건을 확인한 뒤 별도 단계에서 과거 holdIds를 재사용하지 않는다. 이미 확정된 HP를 재차 차감하지 않는다.
수락 응답: `{version:'ordinary.objective-result-acceptance.v1.20261102',evidenceRef,requestHash,receipt}`. receipt는 기존 COMPLETE의 실제 확정 응답이며 state/ordinaryAutomation 등을 그대로 소비한다. requestHash는 전체 context canonical SHA256으로 원 COMPLETE command의 eventId/expectedRevision/node/evidenceRef/attempt를 포함한다. 같은 run/evidenceRef의 다른 context는 ordinary_objective_result_conflict. 같은 context 재시도/응답유실은 원 COMPLETE receipt를 먼저 복구해 실행검증·HP·목표를 다시 적용하지 않는다. 과거 COMPLETE만 있고 acceptance record가 없는 경우 result_unavailable로 backfill을 거절한다.
기존 공개 COMPLETE route는 해당 원key/body로 이미 확정된 완료응답을 반환한다. 새 완료를 client claim만으로 시작하는 경로는 열지 않는다. 독립 evidence owner 미공급인 새 공개완료는 기존 evidence_unavailable 유지. UPDATE/DELETE 금지의 결과 record는 원evidence/holdIds audit이며 현재 HP나 현재보류의 별도원장이 아니다. 기존 source/노드시작원본 재작성 없음.
검토에서 초기 분리수락 방식의 hold freshness 문제가 발견되어 최종 구현은 위 원자 확정으로 교체했다. 초기 ordinary-result-acceptance-* QA는 해당 중간구현 기록이고 최종 근거는 ordinary-result-atomic-*다. 최종형은 별도 stored prepare/default completion owner를 사용하지 않는다.
미공급 경계는 원사건 실행/replay owner와 운영 accept 호출이다. 미검증 checkpoint, 준비화면/합성ARRIVE는 수락근거가 아니며 운영 accepted record는 아직 생성하지 않았다. 이 구현은 per-hit 클라이언트 HP통합이나 실제5인 공격/LOS/Player를 대신하지 않는다.
최종 검증: QA/CommanderS1-20260922/ordinary-result-atomic-regression-20261102.json 92PASS/0FAIL/0SKIP. source 미공급/staleHP/잘못된attempt/원source 경합/hold 경합의 전체rollback, 원자수락+완료후 응답유실 복구, 원HTTP 완료응답 재생·외부owner 미호출·중복완료0, immutable 결과/다른context 거절 포함. tsc(game-api)/scoped ESLint/diff check exit0. 리뷰 P2 hold freshness는 원자 확정으로 수정, 재리뷰 추가결함 없음. 원사건은 합성fixture라 운영 전투 재현·실Player 근거는 아니다.

<a id="ordinary-objective-http-acceptance-20261102"></a>
## BATTLE-INSTANCE-DISPOSAL:SERVER:20261102 — 기존 HTTP 완료 caller와 공통 원자 수락 연결
기존 registerOperationMapCommandRoutes의 COMPLETE는 이제 trusted createEvidenceOwner 경로에서도 OrdinaryObjectiveResultStore.prepare(context)를 사용한다. prepare는 원사건/HP/source/hold 검증과 acceptance INSERT를 같은 COMPLETE transaction에 제공하는 OperationMapVerifier이며, 이전 중간안의 ‘저장된 증거만 읽는 prepare’가 아니다. 내부 accept(context)도 동일 prepare를 사용한다. HTTP 완료가 accepted-result 기록을 건너뛰던 차이를 제거했다.
검증기 객체 생성은 OperationMapStore의 원key receipt 확인 뒤에 수행한다. 기존 원START/profile/도착/노드 검증 후 실제 source.prepare를1회 호출한다. 실행 owner가 종료돼 생성 시 예외가 나더라도 이미 확정된 동일key/body는 owner 생성/재실행 없이 원HTTP 응답을 반환한다. 현재attempt/map revision/HP/hold guard는 계속 최종transaction에 포함되고 종료된 시도의 새명령을 원응답 복구로 위장하지 않는다. 정보창/같은전투정지는 서버완료 명령으로 바꾸지 않았다.
검증: QA/CommanderS1-20260922/ordinary-result-route-red-20261102.json 기존HTTP의 accepted row0 RED; ordinary-result-route-regression-20261102.json92PASS/0FAIL/0SKIP. 추가 executionDisposed=true 생성기 예외 상태의 HTTP 원키복구: ordinary-result-disposed-replay-20261102.json3PASS/0FAIL/73filtered(위92와중복). tsc(game-api)/scopedESLint/diff check exit0, 독립read-only 리뷰추가결함없음.
운영 index의 trusted 실행 source factory 자체는 아직 미공급이다. 이번은 그 인자를 받는 기존 공개 caller와 원자수락 경계의 실제 코드 연결이며 운영 발사/명중/처치 replay owner 생성·실Player 완료가 아니다. Unity839 HP kernel 및 준비 Player 증거는 해당범위로 재사용, Unity/shared 파일 변경0. 기존 wire/endpoint/HP6대상/노드시작원본 재입장 유지.

<a id="ordinary-support-attribution-20261102"></a>
## FIRST-ENGAGEMENT-ATTRIBUTION v1 — 지원 행동 귀속 부분 검증
Server의 verifyOrdinarySupportAttribution(original, configurationHash, actions, rows)는 원START kit를 기존 reader로 검증하고 Unity OrdinaryFiveCombatExecution의 실제 ordinaryActions/RecordOrdinaryEffect 계약을 대조한다. 실행기가 독립 검증한 actions/configurationHash가 입력 전제이며 클라이언트 자기신고로 대체할 수 없다. 원run/actor/소유ability/command/1-based action index/확정target/APPLIED/due/연속 journalSequence/중복/기존 EffectIdentity SHA-256을 확인한다. HEAL_ONE→HEAL은 원5인 target, SHIELD_AND_TAUNT→SHIELD_ISSUE는 자기 target만 허용한다. actualResult는 형식·정수범위·shield addedAmount 일관성만 확인한다.
반환 scope=ORDINARY_SUPPORT_ATTRIBUTION_ONLY와 verifiedRows는 귀속 검증 건수다. 실제 회복량/생존·부상·최저HP 대상선정/쿨다운/HP 반영/전사건 누락 검증은 아니다. 물리탄·고정도착·근접 피해는 이 함수에 넣으면 거부되며 공통 명중판정으로 전환하지 않는다. 현재 운영 trusted owner caller에 미연결; 이 함수 단독으로 COMPLETE나 HP 변경을 승인하지 않는다. 기존 FirstObjectiveEvidenceOwner/원자수락 계약 유지, 새 endpoint 없음.
검증: QA/CommanderS1-20260922/ordinary-support-attribution-20261102.json 1PASS/0FAIL/76filtered(실START fixture, 지원행동 fixture). game-api tsc 및 scoped ESLint 통과, 읽기전용 리뷰 추가 구체 결함 없음. 실제5인 native 교전/Player는 미검증.

<a id="fresh-process-current-attempt-v1-20261102"></a>
## FRESH-PROCESS-CURRENT-ATTEMPT-v1.20261102 — 기존 작전맵 응답의 현재 시도
44#fresh-process-current-attempt-approved-20261102 적용. 기존 GET /v1/runs/:runId/operation-map 응답 operation.map-status.v1에 currentAttempt를 추가한다. 새 endpoint/자동 추가 GET 없음. 기존 runStatus/revision/progress/view와 동일 SQL read에서 원START에 묶인 ordinary_node_attempts metadata를 읽어 현재 node의 값을 선택한다. 과거 ARRIVE/restart 영수증이나 클라이언트 최대 generation을 읽지 않는다.
currentAttempt는 null 또는 {version:'ordinary.current-attempt.v1.20261102',accountId,runId,nodeId,generation,mapRevision,snapshotHash}. mapRevision은 응답 revision과 같고 snapshotHash는 불변 노드시작 원본 식별이다. 원본 전체/중간전투 상태는 이 응답에 복사하지 않는다. runStatus ACTIVE 및 progress.phase ACTIVE이고 현재 node 원본이 존재할 때만 객체를 제공한다. 원본 미공급·출발전/이동/완료 및 FINISH_PENDING은 null이며 generation1로 채우지 않는다. START 결속이나 metadata 형식이 깨졌으면409 operation_map_attempt_source_conflict. 기존 blockedByPendingTransactions는 별도로 준수한다. 이 읽기 응답은 공격/HP/완료 권한을 부여하지 않는다.
소비자는 충분한 기존 권위 응답을 재사용한다. 불명확 COMPLETE/restart 요청이 있으면 원키 복구 우선, 과거 영수증으로 현재 상태를 덮지 않는다. 전투 이탈 후 재입장은 기존 POST operation-map/restart에 현재 nodeId/expectedRevision=mapRevision/expectedGeneration=generation 및 새 요청키를 전달한다. 명령은 권위 상태를 다시 CAS 검증하며 성공 응답의 기존 snapshot/snapshotHash로 노드시작 HP/행동/seed를 구성한다. 기존 원키응답 유실 복구/세대 변경/restart_superseded/RESULT_AVAILABLE/transaction_pending 계약은 변경하지 않는다. 조회 자체로 restart 발급0, HP 및 seed 갱신0. 추가 migration 없음.
QA: current-attempt-red-20261102.json 기존 currentAttempt 누락 RED; QA/CommanderS1-20260922/current-attempt-regression-20261102.json31PASS/0FAIL/0SKIP. 실제 기존 HTTP 경로+SQL fixture에서 원본없음 null/현재1→3세대/옛2세대영수증불변/원키superseded/FINISH_PENDING·완료 null/조회무변경 확인. game-api tsc/scoped ESLint/scoped diff check exit0; 독립read-only 리뷰 구체결함 없음. 실제 앱 프로세스/Player/API 환경/3회메모리 검수는 이 서버 fixture 검증에 포함하지 않는다.

<a id="node-support-caller-v1-20261102"></a>
## 현재 node 지원귀속 → 기존 완료 검증 caller 연결
FirstObjectiveEvidenceOwner.prepare 내부 반환에 선택적 supportAttribution={configurationHash,actions,rows}를 추가했다. 이것은 공개 제출 body가 아니며 독립 재현 owner가 모든 지원결과와 원actions/configuration을 공급하고 기존 source assertions로 함께 fence해야 한다. 과거 owner 계약은 필드 부재를 보존한다. 신규 실제 지원사건을 실행한 owner는 이 필드를 생략해 검증을 우회하면 안 된다. 전체사건 누락/독립수치/기하 검증은 여전히 원owner 책임이다.
기존 OrdinaryFirstObjectiveVerifier.prepare가 실제로 verifyOrdinarySupportAttribution을 호출한다. 내부 support 필드 공급 시 유효한 shape 및 COMPLETE attemptGeneration이 필수, 미충족409 ordinary_objective_support_invalid. helper는 현재 nodeId/attemptGeneration/executorId=sourceId와 action·row healthReceipt를 대조한다. eventId/target/tick, HEAL hpGain=actualHpGain, SHIELD hpGain/derivedExcess=0 및 shieldAdded=0 또는 resultingRemaining을 검증한다. Unity CombatVitals.Shield 반환값은 추가 차이량이 아니라 적용한 층의 candidate이므로 shieldAdded를 addedAmount와 동일시하지 않는다. legacy non-node rows는 기존 계약 유지, node 부가정보를 검증할 attempt 없이 수용하지 않는다.
rows는 지원결과 부분열이며 원journalSequence를 변경하지 않고 엄격 증가를 요구한다. BASIC 피해가 중간에 있어도 연속번호로 다시 매기지 않는다. 원actions의 globalSequence는 그대로 대조한다. 다른 원공격·고정도착·근접 경로를 지원사건으로 변환하지 않는다. 이 검사는 귀속/상호일치이며 receipt.hpAfter/치료량이 실제 권위 상태라는 인증이나 full5 재현기가 아니다. 현재 운영 index의 trusted 전체 실행 owner는 아직 미구현. 기존 공개HTTP→원자수락→검증 caller 연결은 실제코드이며 미검증HP를 새로 쓰지 않는다.
검증: node-support-attribution-red-20261102.json 최신node rows 거부 RED, support-caller-red-20261102.json 불량 support가 무시되어 COMPLETE가 성공하는 RED. 중간 node-support-caller-20261102.json은 v6 5016ms timeout 실패를 보존. 실행 제한30초로 기존 두 통합suite 전체 수행한 node-support-caller-regression-20261102.json94PASS/0FAIL/0SKIP. game-api tsc/scoped ESLint/scoped diff check exit0, 독립read-only 리뷰 구체결함 없음. fixture의 합성 source는 실제 플레이 증거가 아니다.

<a id="reentry-cancel-retry-server-v1-20261102"></a>
## REENTRY-CANCEL-RETRY-OWNERSHIP — 서버 원요청·시도 재사용 경계
44#reentry-cancel-retry-ownership-approved-20261102 적용. 취소·재선택 자체는 서버 명령이 아니며 신규 restart키/세대를 만들지 않는다. 전송한 명령 결과 불명확은 같은 Idempotency-Key와 원body로 복구한다. 기존 restart 원영수증 재생은 현재시도와 맞을 때만 허용하고 superseded/완료/FINISH_PENDING은 기존 계약대로 구분한다. 이미 확정된 교전전 유효 시도/원snapshot 응답은 소비자가 재사용하고 충분한 정보 뒤 추가GET을 하지 않는다. 기존 currentAttempt=null을 generation1로 대체하지 않는다. 새로운 서버취소endpoint/준비중 상태/자동세대발급은 추가하지 않는다. 실제 전투 이탈 재입장과 UI 준비취소·재선택은 소비자의 수명/입력 경계에서 구별한다. 기존94건 회귀에 원키 재생·세대 및HP fencing 포함; 실제화면취소/늦은게시/driver 검수는 Unity 범위로 미검증.

<a id="ordinary-artillery-impact-replay-20261102"></a>
## 원포격 충돌·수치 독립 재계산 → 완료 검증 연결
기존 rail-artillery-hazard.ts의 verifyResolvedArtilleryImpact(originalDefinition, impact, stepMs)는 Unity FirstPlayableArtillery의 실제 impact {request:{frame,numeric,tick,protected},result:{collision,calculation,hpLoss,absorbed,hpAfter,trainDefeatedNow}}를 읽는다. 기존 ExactRailArtilleryHazard 및 resolveNumericDefenseBudget로 충돌과 수치결과를 새로 계산해 보고값과 대조한다. 원damage와 numeric.base, logical tick×step과 원impact시각을 확인하며 프레임의 전체 객차/선로/구간·중복·정확시각 검증은 기존 hazard에 위임한다. MISS/보호상태에서 hpLoss/absorbed 비영, 계산량보다 큰 피해합, 불가능한 trainDefeatedNow를 거부한다. Unity ToVitalsMagnitude checked Int32와 맞춰 MISS를 포함해 계산량2147483647 초과도 거부하며 clamp하지 않는다.
기존 trusted FirstObjectiveEvidenceOwner.prepare 내부 선택필드 artilleryReplay={definition,impact,stepMs}를 실제 OrdinaryFirstObjectiveVerifier caller에 연결했다. 제공 시 현재 attemptGeneration 필요, CANCELLED_BEFORE_LAUNCH에 충돌 impact를 넣을 수 없다. 원START의 profile/원ARRIVE에 결속된 run/train/issuedHazardId/rawDamage/위험구간길이/preview→launch→impact 시각과 완료evidence.outcome를 대조한다. 마지막 두사수 defeat 뒤 최소preview간격도 확인한다. 오류는409 ordinary_objective_artillery_invalid 또는 ordinary_objective_artillery_source_conflict. 기존 원자수락/HP fence/원키복구를 재사용한다.
원래 발행한 경고영역·프레임·방어/효과·step의 실제 원천과 권한, 발사 허용·전체위협해소·런처생존/발사전취소 증명은 trusted 원owner가 독립 확보하고 같은 assertions로 fence해야 한다. 선택필드는 역사 owner 호환용이며 실제 포격결과를 검증하는 신규owner는 이를 공급해야 한다. client JSON·self hash·이 helper 통과는 원천 인증이나 실제HP commit 증명이 아니다. HP 차감/새공개endpoint/운영owner설치 없음. 발사전취소는 기존 원사건owner의 취소 증명 책임을 유지한다. 아직 full5 실행원천owner는 미구현이다.
QA/CommanderS1-20260922/artillery-impact-red-20261102.json 미구현 RED; artillery-caller-red-20261102.json 불량필드무시 COMPLETE 성공 RED. artillery-replay-regression-20261102.json90PASS/0FAIL/0SKIP. 후속 정상caller/보고HIT-MISS불일치 시험 artillery-caller-final-20261102.json3PASS/74filtered(중복). 정수범위 RED artillery-magnitude-red-20261102.json 이후 artillery-impact-final-20261102.json13PASS/0FAIL(기존90과중복). tsc/game-api 및 scopedESLint 통과. 독립리뷰도 같은Int32 차이를 지적했으며 수정/RED-GREEN 근거와 일치. 실제Player/운영API/메모리 검수는 미수행.

<a id="reina-barrage-v1-20261102"></a>
## REINA-BARRAGE-DAMAGE-v1.20261102 — 서버 정의·재계산 계약
44#reina-barrage-damage-detail-approved-20261102 및 바로 앞 proposal 전체 적용. source는 server/apps/game-api/src/reina-barrage.ts reinaBarrageDefinition/replayReinaBarrage. 새 정의 ID combat.reina-barrage.v1.20261102, stable ability id op_reina.ultimate 유지, effect REINA_BARRAGE. cost100/W800ms/R300ms, commit 기준 offsets [1400,2200,3000,3800], 각회 primaryDamageBps25000/additionalDamageBps7500, maximumAdditionalTargets3/radiusMm3000/castRangeMm24000/adjacentCars1. 기존 basic/active 수치 변경 없음.
새 START는 ordinaryKitSource.packet.version=combat.ordinary-five-kit.v2.20261102, ordinaryRelocationSource.packet.version=combat.ordinary-relocation.v7.20261102를 실제 발급한다. 새 kit의 해당 ability 전체가 정의이며 scope의 barrage도 동일정의를 참조한다. kitHash/relocationHash는 원run source를 포함한 기존 canonical SHA 계약. 원kitv1·relocationv1–v6는 기존 reader·기존 단발1000%/추가300% 정의로 재구성하고 소급변경하지 않는다. 새 handler 지원으로 승격하지 않았으며 execution/런타임준비 gate는 유지. Unity 소비자는 v2/v7을 명시 지원해야 한다.

### 내부 논리 replay wire
version=combat.reina-barrage-replay.v1.20261102. {castId,nodeId,attemptGeneration,acceptedTick,stepTimeSpanTicks,primary:{id,generation},chargeAtAcceptance,casterAliveAtAcceptance,actionEligibleAtAcceptance,operations}. 이는 public API body가 아니라 독립 실행 owner의 입력이다. 예약/동시시전·충전소유권, 원focus·실공간/LOS·scope·frame 변환 관측은 기존 권위 owner가 확보하고 같은 transaction assertions로 fence해야 한다. booleans나 self hash만을 클라이언트 권한으로 수용하지 않는다.
- COMMIT: {kind,tick,casterAlive,actionEligible,charge,attack:{numerator,denominator},frame:{id,generation,centerMm:[x,y,z]},candidates}. candidate는 {id,generation,alive,kind:ENEMY|ARTILLERY|STRUCTURE,order,offsetMm:[x,y,z],muzzleDeltaMm:[x,y,z],castScopeEligible,muzzleClear,centerClear}. offsetMm는 commit 중심에 대한 원frame 좌표차, muzzleDeltaMm는 같은 관측의 실제 총구→anchor 차다. 주대상offset=0 필수. 실제LOS·앞뒤1객차 적격은 원공간 owner 공급이며 거리 계산은 서버 BigInt 3차원 제곱으로 재검증한다. eligible ENEMY만 대상으로 중심거리/전투순서/stableId 순 추가3명을 선택한다.
- IMPACT: {kind,tick,index:0..3,frame:{id,generation,exists},targets:[{id,generation,alive,offsetMm,centerClear}]}. 같은 원frame/generation·고정대상집합만 사용한다. 원generation 생존/3m/중심차폐를 각회 재검사하며 새 대상 추가 금지. 원세대 사망 뒤 같은세대 부활 입력 거부. 시전자 총구거리/LOS는 재검사하지 않는다.
- CANCEL: {kind,tick,reason:CASTER_DOWN|TARGET_INVALID|FRAME_LOST|INSTANCE_DISPOSED|FORCED_TERMINAL}. commit전 취소는 소비0. commit후 caster down/target invalid는 기존일정을 취소하지 않고 frame loss/인스턴스폐기/강제종료만 잔여일정 취소, 환급0. 실제 원취소원인의 진위와 종료 우선순위는 실행 owner 책임이다. 처리해야 할 회차를 건너뛴 뒤 늦은 cancel로 소거할 수 없다.

입력수락 적격/충전 확인→ceil(W/step)의 정확commit에서 재검증·100소비·ATK/고정frame/대상/4일정 확정. 각 offset은 commit에서 BigInt ceil을 한 번 적용하며 프레임 누적 없음. 반복/역순/시각불일치 impact 거부, 원frame을 다른frame으로 치환 거부. 반환은 phase/consumedCharge/recoveryUntilTick/frame/selected/impactTicks/pulses/unresolvedImpacts 및 cast/node/attempt/clock/step 결속이다. 각 pulse.hits에는 target/generation/damageBps/rawDamage exact 및 {nodeId,attemptGeneration,castId,index,targetId,targetGeneration} identity가 있다. rawDamage는 ATK×회차계수의 의도이며 최종 HP손실이 아니다. 실제 방어/면역/보호막/치명·표식·성장/흡혈·파생필터는 기존 실제탄착 수치/단일HP owner에서 별도 검증·소비한다. 이 replay가 새 HP/소비원장을 만들지 않는다. WINDUP 미해결예약도 완료불가로 표시한다. 노드재입장은 기존 원노드 초기상태이며 중간 replay를 클라이언트 복원으로 노출하지 않는다.

### 기존 완료 caller
FirstObjectiveEvidenceOwner 내부 반환 reinaBarrages?:[{input,result}]를 실제 OrdinaryFirstObjectiveVerifier에서 재계산한다. 원kitv2의 정확ability, 현재node/attempt, castId 중복, checkpoint 시각, 미해결회차0, 재계산 result 전체 일치를 요구한다. 오류 ordinary_objective_reina_barrage_invalid/source_conflict. 역사 owner의 필드 부재를 유지하되 신규 실제시전 owner는 모든시전과 input원천/실HP/소비·파생결과를 독립 검증해 공급해야 한다. 현재 전체 운영owner 미주입 상태는 유지하며 이 선택필드만으로 전체 전투검증을 완료하지 않는다.
QA: reina-barrage-kit-red/replay-red/caller-red-20261102.json. 초기 green/regression/regression2는 새kit와구scope충돌·타입/fixture변경 실패 보존. reina-barrage-regression3-20261102.json85PASS/0FAIL/0SKIP(새START·구kit/scope보존·기존실수락회귀), 후속 reina-barrage-final-20261102.json12PASS/75filtered(중복 포함,1~4명·경계/세대/차폐/선정·중복·pending완료거부). tsc game-api/scopedESLint/scoped diff check exit0, 독립read-only리뷰 구체 blocking 결함 없음. 실제Unity/Player·모드비교·흡혈·운영API·3회메모리 미검증.

### 기존 충전 owner 대조 추가 — 2026-11-02
reinaBarrages 항목은 {input,result,chargeTransition?:{before,after}}. commit 소비100인 항목은 CombatUltimateState의 실제 전후 snapshot이 필수이며, commit전 취소는 transition 미공급만 허용한다. 기존 owner를 private candidate로 restore하고 동일 cast를 실행해 전체 after를 대조한다. commandId는 reina-barrage: + SHA256(canonical {version:reina.barrage-charge.v1.20261102,accountId,runId,nodeId,attemptGeneration,castId}). before는 op_reina/commit charge 일치 및 해당 command 미소비 상태여야 한다. 신규 충전원장/충전 지급/운영 저장은 추가하지 않는다. 오류 ordinary_objective_reina_barrage_charge_conflict. 원 충전기록의 진위·이전 행동과 연속성·같은 transaction assertions는 독립 실행 owner 책임이며 테스트의 BASIC25회는 합성 fixture다.
검증: reina-charge-green-20261102.json10PASS, reina-charge-caller-20261102.json88PASS/0FAIL(중복 포함), game-api tsc/scopedESLint/scoped diff check exit0. 독립 read-only 리뷰 구체 blocker 없음. 실제 충전 저장·HP/흡혈 실행·운영 full5 owner/Player 미완료.
44#reina-cinematic-battle-audio-layout-approved-20261102 승인 확인. replay tick은 hold를 제외한 기존 전투 논리시간이며 음원 peak/skip은 COMMIT 또는 IMPACT 공급원이 아니다. 음원/Unity 파일 수정 및 실제 hold·청취 검수는 수행하지 않았다.

<a id="ordinary-gunner-health-replay-20261102"></a>
## 원사수 HP 순차 실행 최소단위 — 2026-11-02
서버 ordinary-gunner-health-replay.ts의 replayOrdinaryGunnerHealth(original,events)는 원발급 사수2명의 private 실행 candidate다. 별도 HP DB/공개 endpoint가 아니다. original={gunnerIds:[id,id],hp:exact}는 실제 완료 caller가 원ARRIVE scaling.applied.gunnerHp와 firstObjectiveEncounterIdentities에서 구성한다. 각 초기HP는 기존 Unity와 동일 floor/Int32 범위(1..2147483647). 클라이언트 초기HP를 받지 않는다.
events=[{eventId,targetId,atMs,numeric,protected,shieldRemaining:0}]. numeric은 기존 numericDefenseInputSchema 전체이며 기본값으로 빈효과를 채우지 않는다. 원실행 owner가 전체 사건순서/누락없음·원공격/효과·면역·보호막부재와 동일현재attempt를 독립검증하고 source assertions로 fence해야 한다. 첫단위는 보호막 없는 사건만 지원하며 누락/양수shieldRemaining은 거부. 새 원사건 공급 API/원충전/적HP 영속원장을 추가하지 않는다.
기존 resolveNumericDefenseBudget로 피해량을 계산, Int32초과 거부, 면역0/과잉피해clamp/HP0 이후 추가손실0, 동일사수 처치1회. 사건ID 중복·역순시간·미발급대상 거부. 같은시각 순서는 입력의 권위 순서를 유지한다. 반환 targets/receipts/defeats는 계산 결과이며 receipts={eventId,targetId,atMs,calculation,hpBefore,hpLoss,hpAfter,absorbed:0}.
FirstObjectiveEvidenceOwner 내부 gunnerHealthReplay?:{events,result}를 기존 OrdinaryFirstObjectiveVerifier에서 실행한다. 현재attempt 필수, 정확반환값·두사수HP0·원처치eventId/atMs 일치·checkpoint 이후사건없음을 확인한다. 오류 ordinary_objective_gunner_health_invalid/source_conflict. historical 필드부재 유지; 이 선택필드나 자체hash가 전체source 진위/누락방지 증명은 아니다. 서버 아군5+열차6대상 CombatHealthStore는 그대로, 흡혈resolver 대상계약을 임의 확장하지 않았다.
QA: gunner-health-red-20261102.json 모듈미구현 실패. caller-red는 fixture attempt불일치였고 caller-red2는 잘못된 replay를 무시해 성공하던 실제 caller 실패를 확인. gunner-health-regression-20261102.json90PASS/0FAIL, 후속 gunner-health-caster-final-20261102.json14PASS/0FAIL(중복 포함). game-api tsc/scopedESLint/scoped diff check exit0, 독립 read-only 리뷰 blocker 없음. 실제 운영owner 등록·Player/API는 미완료.
REINA-CASTER-ACTION 승인 확인: 기존 recoveryUntilTick과 impactTicks 분리 유지, 회수끝뒤 새 시전이 원시전 frame/일정을 덮지 않는 domain시험 추가. 이는 행동입력/실충전 적격·Player검수 완료가 아니다. partial-outcome 승인 표시와 실제HP손실을 분리하고 미승인 overlap표현은 구현하지 않았다.

<a id="ordinary-node-execution-binding-20261102"></a>
## 실제 공간 source의 원node/attempt/seed/정의 결속 — 2026-11-02
FirstObjectiveEvidenceOwner 내부 executionBinding?:{nodeId,attemptGeneration,snapshotHash,executionVersion,determinism,geometryBinding}. 공개API/추가GET/새원장 아님. 공급자는 현재 실행 adapter에서 실제 사용한 원node snapshot의 해시·executionVersion/determinism 및 runtime.spatial.definitionBinding을 전달하고 현재 attempt는 wrapper의 generation을 사용한다. geometryBinding의 originGeneration=1은 재입장 후에도 바꾸지 않는다. seed/정의hash만으로 실제총구/LOS/명중 진위가 증명되지는 않는다.
기존 OrdinaryFirstObjectiveVerifier가 prepareOrdinaryNodeExecutionBinding을 호출한다. 기존 ordinary_node_attempts 원행에서 start_snapshot_json 일치·현재 generation·원snapshot 해시를 확인하고, 원entry의 account/run/node/entryMapRevision·executionVersion/원random 전체·geometryBinding을 대조한다. geometry.definitionRef는 원START ordinaryGeometryReference와 정확히 일치해야 한다. reference schema는 기존 registry와공유한다. 별도 hash/seed 발급이나 후보pin 승격0. 원기하 없는 기록에서 이 검증을 요청하면 실패하며 더미정의를 채우지 않는다.
같은 완료 batch에 원node행 전체 JSON/hash/START/currentgeneration 조건부 assertion을 추가하므로 검증후 restart 경합을 거부한다. 현재map/account/hold는 기존 완료 경로가 소유한다. 오류 ordinary_execution_binding_conflict. 역사 source의 필드부재는 보존하되 실제 adapter는 자신이 소비한 원천으로 이 입력을 구성해야 한다. 이 단계는 identity 결속이며 공간관측/효과/충전/전체사건의 독립검증·운영 full5 owner를 대체하지 않는다.
QA: node-execution-binding-red 및 node-binding-caller-red-20261102.json에서 미구현/잘못된binding무시 실패 확인. SQLite 실제0142 migration으로 검증후generation변경 거부와 원seed/원geometry그대로 새generation수용 확인(node-binding-fence-final3PASS). 초기 전체회귀91PASS/3FAIL은 기존긴map통합시험의5초제한, 단독도같은제한실패. 20초제한 진단3PASS/실측5.8~6.4초 후 해당시험에만15초 적용. 최종 node-binding-final-20261102.json95PASS/0FAIL, game-api tsc/scopedESLint/scoped diff check 통과, 독립읽기리뷰 blocker없음. 앞선 실패파일은보존, 중첩시험합산0.
44 중첩표현 승인 §1–6 확인. 두시전 사건순서/원피해와 표현생략은분리하며 새offscreen제안은적용하지않았다. 실제 Unity 공간관측/총구·피격기하 소비·운영권위source와5인Player/API검수는아직미완료다.

<a id="ordinary-scene-observations-20261102"></a>
## Unity844 실제 공간 관측 형식 검증 — 2026-11-02
FirstObjectiveEvidenceOwner 내부 sceneObservations?는 Unity OrdinaryNodeSceneGeometry.Capture의 ordinary.node-scene-observation.v1.20261102 배열 그대로다. executionBinding 필수. 기존원node read결과(entry/원binding/assertions)를재사용하며추가node조회0,원등록 definition은 OrdinaryNativeGeometryRegistry.read로검증하고같은완료batch에registry assertion을추가한다. 운영source등록이나관측업로드API는추가하지않는다.
wire: {version,accountId,runId,nodeId,snapshotHash,attemptGeneration,attemptRevision,definitionHash,tick,revision,quantization:NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO,entities}. entity={id,authoringId,definitionRef,role,generation,present,active,frame:{originMm,basis},anchors,shapes}; shape의active이외로컬형상/anchor는원등록정의와정확히일치해야한다. 정수mm/±1e9/rigidbasis/오른손좌표와원Unity허용오차를대조한다.
원account/run/node/snapshotHash/currentattempt/definitionHash 대조. tick은원node actions.stepTimeSpanTicks로checkpoint시간이내인지확인한다. 첫revision1,새tick마다revision+1,역순금지,동일tick재독은전체불변. attemptRevision은원entry..현재map범위이며관측간고정; 원attempt wrapper/논리이동source의진위는실행owner책임. 원authoring전체coverage,고정runtime ID집합,원등록역할/정의/anchor/shape대조. OPERATOR는원편성ID/definitionRef,ENEMY는원발급사수ID+enemy_spike_cart_gunner,TRAIN은원train/rail physical/ARRIVE enemySpatial ID,DEVICE는원hazard,WORLD는authoringId. 입력이이ID를스스로발급하지않는다.
present=false는active/shapeactive=false이고이전관측이있으면원세대/보존frame동일,이후같은등록을present로되살리지않는다. active=false와present=false는구분하며renderer/camera정보로miss/cancel을만들지않는다. 반환 observations/lastTick는형식·일관성검증결과이고실제움직임/LOS/명중결과가아니다. 관측누락없음·원pose/tick·자산참조·효과/충전/보호막의진위는아직권위producer연결이필요하다. 오류 ordinary_objective_scene_observation_binding_required/scene_observation_invalid;원registry오류는기존계약.
QA: scene-observations-red 모듈미구현. scene-observations-regression95PASS2FAIL은기존무거운시험5초제한. scene-role-red6FAIL은새scope필드가strict스키마에없던준비실패이며role누락재현으로계산하지않는다. role-ID 누락은독립코드리뷰로확인후수정했고최종외부ID거부시험통과. 초기unused변수lint/rail reader인수누락tsc도수정. scene-observations-bounded-final-20261102.json102PASS0FAIL(testTimeout20초진단실행),최신실제caller positive/foreign-body 거부포함 scene-observations-caller-final-20261102.json10PASS75filtered(중복). tsc/scopedESLint/scoped diff check통과,후속읽기리뷰구체결함없음. 합성등록/SQL시험이며실5인Player/API미실시.
44 offscreen 원§1–6/승인확인. 화면밖과범위/원frame소멸을분리,새hazard-ui-readability제안미적용. 실제총구·피격자산등록/pose이동/효과producer·운영full5 owner/Player전체완료아님.

<a id="ordinary-obstacle-sequence-20261106"></a>
## 일반전 장애물 prefix 단계·원 웨이브 연결 v1 — 2026-11-06
승인 원문45#obstacle-product-node-approved-20261106 및 preceding 상세/수치. Server5 구현: `server/apps/game-api/src/ordinary-obstacle-sequence.ts`, 원 node-entry/scaling/execution-binding/objective-verifier. 새 공개 API/지도노드/보상/HP 저장소/시계 없음. 현재는 콘텐츠·단계 owner·기존 수락 경계 연결이며 운영 full5 execution owner 등록/Unity 제품 Player 완료가 아니다.

### 원 콘텐츠와 nodeStart
기존 START의 `operationMapContent.encounters[].definition.ordinaryObstacle`에 `ordinaryObstacleDefinition()`의 정확한 payload를 명시한다. 원 profile 나머지 필드는 그대로 대조한다. 등록 대상은 원 COMBAT만이며 노드별 배치/노출은 authored catalog/plan 소유자가 결정한다. 새 발행에서는 원 publicInfo/노드 상세에 장애물 포함을 공개해야 한다. 모든 COMBAT 자동등록·ARRIVE 재추첨 없음. 구 snapshot의 marker 부재는 미발행으로 유지한다. 콘텐츠를 변경하면 원 publication hash가 달라지며 기존 START를 갱신하지 않는다.
정의 version=`ordinary.falling-obstacle.v1.20261106`: hp100/defense0/shotDamage25/launchIntervalMs1650/preparationMs150/rangeMm100000/projectileSpeedMmPerSecond300000, zoneStartOffsetMm277000/zoneEndOffsetMm283000/hazardAtMs16000, horizontalBaseDamage600/fallingBaseDamage900, cruise/slow/fastSpeedMmPerSecond20000/14000/26000, accelerationMmPerSecondSquared5000. 구역 offset은 사건 경고 시점 원 선두 기준이다. 시험 600m/6m 소켓·낙하체체적/발높이를 실기하로 등록하는 값은 포함하지 않았다.
실제 `OrdinaryNodeEntrySource.prepare`는 원 publication+원 발급 enemies를 사용해 runtime owner에 `sequence`를 제공하고 반환값의 동일성을 요구한다. 기존 ARRIVE `nodeStart.snapshot.runtime.sequence`가 그대로 저장된다: `{version:'ordinary.obstacle-sequence.v1.20261106', obstacle:<위 정의>, nextWave:{kind:'ORIGINAL_NODE_ENEMIES', enemiesHash:<canonicalJson(runtime.enemies)의 SHA256>}}`. 원 적/포격 payload는 기존 `runtime.enemies` 하나만 소유한다. 새 적 목록/추가 웨이브를 만들지 않는다. 소비자는 처음 OBSTACLE에서 enemy train/원 적 spawning을 대기시키고 COMBAT 전이 때 원 enemies를 한 번만 개시한다. sequence를 지원하지 않는 runtime은 응답 불일치로 발급 차단한다.

### 현재 시도와 단계 전이
`OrdinaryObstacleSequence(pin, scope)`의 scope는 `{accountId,runId,nodeId,attemptGeneration,snapshotHash}`. capture 초기 `phase:OBSTACLE,revision:1,nextWave:null`. 기존 현재attempt wrapper의 generation을 사용하고 원 nodeStart의 generation1을 재사용하지 않는다. 재입장 POST `/v1/runs/:runId/operation-map/restart`는 기존 request key/expectedRevision/expectedGeneration 계약, 원 snapshot/seed/정의를 유지하고 시도만 증가한다. 같은 메모리 pause/resume는 재입장이 아니며 원 시계/모델 유지.
실행 owner의 내부 `resolve` 입력은 scope + `{eventId,atMs,outcome,obstacleHp,trainHp,tailBeyondZone,remainingHazards,unresolvedProjectiles,collisionCommitted}`. outcome=DESTROYED/PASSED/COLLISION/DEFEAT. 클라이언트 공개 업로드 명령이 아니다. tailBeyondZone은 실제 마지막 차량 swept 결과, 잔류 수는 미발사/향후 낙하도 포함한 원 사건 전체, collisionCommitted는 원 HP/적격효과/감속 확정 원천으로 owner가 제공하고 같은 transaction fence가 필요하다. 이 boolean/schema만으로 물리 진위를 증명하지 않는다.
생존 전이는 후미완전통과/잔류위해0/미해결탄0을 요구한다. DESTROYED는 HP0, PASSED/COLLISION은 HP>0, collisionCommitted는 COLLISION만 true. PASSED는 최소 원 위해16초 이전에 완료할 수 없으며 실제 원 낙하/잔류수명 종료는 source가 증명한다(45:2319). DEFEAT는 열차HP0이며 wave 없음. 응답 version=`ordinary.obstacle-transition.v1.20261106`, 원 scope/eventId/atMs + `phase:COMBAT|DEFEAT,revision:2,nextWave:<원 ref 또는 null>`. 원 event 동일 입력 재호출은 동일 영수증, 변경입력·다른 종료·옛 scope는 거부. spawn 소비자는 revision1→2 전이만 처리하고 영수증 재생을 새 spawn으로 취급하지 않는다. 이 owner는 HP/효과/속도/목표속도/선로/카메라를 변경하지 않는다.

### 원 완료 수락과 복구
기존 POST `/v1/runs/:runId/operation-map/completion` 및 Idempotency-Key 사용. 공개 body는 기존 `{expectedRevision,nodeId,evidenceRef,attemptGeneration}`이며 raw 피해/단계 boolean을 받지 않는다. 내부 `FirstObjectiveEvidenceOwner.prepare`가 `obstacleResolution`을 위 내부 wire로 제공한다. marker 노드는 기존 executionBinding/현재attempt/원 snapshot SQL fence가 필수. 저장 sequence를 원 publication/enemies에서 다시 대조하고 COMBAT 전이 및 원 적 처치/포격/최종 checkpoint와 시각 순서를 검증한다. marker 없는 노드에 resolution/sequence 주입은 거부한다. 원 COMPLETE/HP/automation/receipt atomic batch와 회복 우선순서 유지. 장애물만 종료한 순간은 지도 COMPLETE/보상/다음 지도노드가 아니다.
오류 접두사 `ordinary_obstacle_*`: definition_invalid/next_wave_unavailable/attempt_conflict/resolution_invalid/not_clear/outcome_conflict/collision_conflict/survival_conflict/already_resolved/event_conflict. 기존 완료 경계는 `ordinary_objective_obstacle_binding_required/obstacle_unissued/obstacle_sequence_conflict/obstacle_wave_order`.

### 로컬 실행 근거 및 정확한 미공급
서버 cwd `D:/Project/LASTRAIL/Worktrees/11ec/LASTRAIL/attendance-pass-20260907/server`, TEMP/TMP/TMPDIR=`D:/Project/LASTRAIL/LocalTemp`. `pnpm exec vitest run tests/integration/combat-ordinary-start-source.test.ts -t 'serves obstacle'`는 실제 loopback TCP를 열어 원 HTTP restart/completion handler+SQLite 및 nodeStart producer를 실행하고 종료 시 닫는다. QA `QA/CommanderS1-20260922/obstacle-local-http-20261106.json` 2PASS. 파괴/통과, 저장 nodeStart/seed 재사용, 옛시도·미공급 종료 거부, 원키 재시도, 완료1회 및 원천 장애 뒤 영수증 복구를 검증했다. 인증/이동/전투/기하는 명시적인 합성 fixture이므로 실제 Unity 제품 세션용 상주 서버/실사격 증거가 아니다.
미공급: 운영 catalog의 대상 노드 발행·원 public disclosure, 제품 runtime owner의 실제 actions/effects/scene/motion 및 원 적 파이프라인, 사건 포탑/낙하 체적/원 rail 거리예산·시각 결속, 충돌에 적격한 보호막 원필터·실피해/감속 기록, 독립 전체 사건검증 owner. 기존 index.ts 운영 evidence/runtime source 미등록 유지. 시험 shield300을 제품에 허용하지 않음. Unity 제품 caller/실 Player/세화면비/정지·배속·동시각·전체 next-wave 검수는 남음. 새 GET/가짜 지원flag로 준비 차단을 우회하지 않는다.

<a id="ordinary-local-live-execution-20261106"></a>
### 실제 로컬 실행 입력·공용 판정 경계 — 2026-11-06 구현 계약
45#obstacle-live-execution-approved-20261106 적용. SERVER_5는 지속 loopback/로컬DB/인증·입력 영수증·독립 공용판정 실행을, Unity_17은 실제 제품 source/caller/기하와 같은 공용 판정 함수를 소유한다. 아래 계약을 이번 구현의 고정 경계로 사용하며 성공결과/HP 업로드로 대체하지 않는다. 아직 실행 완료 기록은 아니다.
- 제품 공개 restart/completion/currentAttempt wire는 유지한다. 로컬 전용 단일 입력 경로 `POST /__local/ordinary-execution`는 기존 Bearer 인증+Idempotency-Key와 `{runId,nodeId,attemptGeneration,snapshotHash,expectedSequence,inputs:[{sequence,tick,command}]}`를 받는다. 원 nodeStart/계정/현재attempt 검증 후 서버가 수락한 입력 prefix를 DB에 저장하고 공용 판정으로 재실행한다. 클라이언트 HP/충돌성공/완료 boolean은 받지 않는다. 같은 key/body는 원응답, 변경 body/과거attempt/sequence 불연속은 거부. 실패/응답유실은 원key/body로 회복한다. 원 수명은 bootstrap의 절대 만료와 명시 stop이며 상태변경/실행 로그에는 token을 남기지 않는다.
- command 범위는 원 model의 `{kind:'PAUSE',value:bool}`, `{kind:'SPEED',value:2|3|4} (기존 speedHalfUnits: 1/1.5/2배속)`, `{kind:'THROTTLE',value:<원 허용 bps>}`, `{kind:'ULTIMATE_AUTO',value:bool}`, `{kind:'ADVANCE',elapsed:<TimeSpan ticks>,budget:<소비 상한>}`. sequence1부터, tick은 명령 직전 원 절대 model.Tick. 시간은 실제 수신 wall time 예산과 원 pause/speed 시뮬레이션 검증 둘 다 적용한다. 첫 세션 재시작 시 새시도는 빈 prefix; 원 snapshot seed는 동일. 결과는 원계정/run/node/attempt/마지막sequence·원checkpoint·장애물/원다음전투 사건 및 검증 가능한 결과를 응답하며 UI는 원응답 재사용.
- 공용 engine 입력 JSON version=`ordinary.local-execution-input.v1.20261106`, fields=`accountId,runId,originalStart,initializationResponse,nodeStart,firstPlayableScaling,currentAttempt,geometryDefinition,inputs`. 모두 inputs를 제외하면 서버 원 START/ARRIVE/registry/현재attempt에서 공급한다. `inputs`는 수락 대상 전체 prefix, 위 `{sequence,tick,command}` 목록이다. 판정은 매번 원 node에서 같은 seed로 복구하여 실행하고 원 사건/HP/효과/속도/절대tick/장애물단계/원 적 생성을 재현한다.
- Unity 공급 경계: engine-free 공용 빌드에 public `LastRail.Battle.OrdinaryLocalExecutionReplay.Execute(Newtonsoft.Json.Linq.JObject input)` → JObject를 제공한다. 제품에서 쓰는 실제 factory/Basic/Gunner/SceneMotion/장애물 owner를 같은 원 input으로 연결하는 adapter이며 별도 전투 공식 복제 금지. 결과 version=`ordinary.local-execution-result.v1.20261106`, `accountId,runId,nodeId,attemptGeneration,snapshotHash,lastSequence,tick,checkpoint` 필수. checkpoint는 원 CaptureLocalState JSON object. `obstacleResolution`은 실제 완료 시에만, `evidence`는 원 전투 목표 완료 때만 기존 FirstObjectiveEvidence schema로 생성한다(없으면 null). 각 결과의 실제geometry·damage/effect·defeat/잔탄 사건은 checkpoint 원기록으로 재현 가능해야 한다. 원 사격 source/권위 필터가 지원 안 되는 행동은 오류로 멈추고 dummy 성공을 만들지 않는다.
- Server host는 서버가 선택한 공용 DLL/Newtonsoft/기하 hash를 고정하고 위 고정 entry point만 호출한다. 클라이언트가 DLL/파일경로/출력증거를 선택하지 않는다. Unity 산출물이 생기면 원 파일 hash와 build를 고정해 local session에 연결한다. 최신816 preparation 전용 DLL/seed1은 이 실행에 사용하지 않는다. 실제 신규 measured geometry JSON은 독립 원시파일/definitionId/version/hash와 scene·asset SHA를 인계에 공급하고 로컬 registry에 등록한다.

#### 로컬 실제 효과 소유·공용 결과 보충
runtime.effects는 이번 local DB의 `ordinary_local_node_scope.entry_effects_json`으로 소유한다. 초기화는 새 격리 run의 첫 노드에서 어떤 전투명령도 수락하기 전에 한 번 수행하고 immutable 원행을 ARRIVE와 함께 fence한다. modifierSource를 빈 효과 증거로 대체하지 않는다. wire=`{version:'ordinary.local-entry-effects.v1.20261106',accountId,runId,nodeId,startSnapshotHash,originGeneration:1,revision:1,atTick:0,activeEffects:[],shields:[],interceptions:[]}`. 이 세 빈 목록은 새 로컬 효과 owner가 아직 아무 효과/보호막/요격을 생성하지 않은 원 tick0 상태이며 매 tick의 영구 무효과 선언이 아니다. 각 후속 사건은 같은 실제 model의 효과/보호막/요격 owner를 변경하고 공격시/명중시 관찰은 그 owner의 해당tick 상태를 읽는다. runtime.effects revision/원hash/범위 불일치 또는 해당owner 없는 callback은 거부한다. 새 로컬 origin은 이 빈 시작 상태만 지원하며 비어있지 않은 저장 효과를 임의 삭제/기본값화하지 않는다.
충돌 적격 보호막 목록은 원효과 owner의 실제 대상/피해원 필터로 선정한다. 초기 shields=[]이면 tick0 collision shield0이다. Sera 등 후속 능력의 보호막을 단순 존재/시험shield라는 이유로 충돌 적격으로 확대하지 않는다. 원세부 필터를 실행기가 지원하지 않는 경우 해당 행동 오류를 유지한다. interception도 동일하게 처음 빈 owner에서 실제 능력/사건이 생성·만료한 기록을 조회한다.
공용 Execute 결과에 `healthChanges:[{eventId,targetId,reason:'DAMAGE'|'HEAL'|'REPAIR'|'RECOVERY',hp}]`를 필수로 추가한다(변경없음은[]). 원시도 전체의 실제 아군5+열차 HP 사건 순서이며 동일 eventId는 한 번, eventId는 run/node/attempt에 결속. 적HP는 원 checkpoint/defeat owner가 소유한다. 중간 노드 HP는 원시도의 공용 model/checkpoint에서 소유하고 서버 run HP를 매 프레임 덮지 않는다. COMPLETE에서 동일 CombatHealthStore.prepareChanges를 사용해 이 원사건들·최종HP·원 COMPLETE/automation/receipt를 한 batch로 확정한다. 재입장은 따라서 원 node HP로 재생하며 미완료 다른 시도의 HP가 확정 run HP에 남지 않는다.
SPEED wire는 기존 FixedStepDebt.SetSpeed와 같은 정수 halfUnits 2/3/4(1/1.5/2배)이다. 앞 명시의 배율표현을 이 실제 코드단위로 정정했다. 입력 초기 open은 빈 inputs/expectedSequence0으로 수락 가능하며 이후 ADVANCE wall-time 예산의 시작점을 고정한다.
로컬 세션은 명시된 첫 노드의 내부 장애물→원 전투만 검수한다. `ordinary_local_node_scope.hold_id`는 세션 생성 때 실제 생성되는 종료 검토 hold이며 완료 응답에도 유지/fence되어 다음 지도 CONFIRM은 차단한다. 외부 거래/보상 API는 local allowlist에 노출하지 않는다. 원 다음 웨이브와 다음 지도노드는 구분한다.

#### 실제 연결 중간 검증 및 Player 소비 계약 — 2026-11-06
SERVER_5 실제 새 로컬 START/초기화/ARRIVE 생성까지 통과. 아직 상주 서버 정상 시작/Player 연결 완료 아님. source.originalStart는 departureResponse가 아닌 저장된 전체 START 객체, geometryDefinition은 원 byte hash 보존용 raw JSON 문자열이다. 새 ordinary mapped START는 canonicalJson으로 저장하여 Unity 원본 hash와 일치하며 기존 저장본은 다시 쓰지 않는다. host는 날짜 자동변환을 금지(DateParseHandling.None)하여 timestamp 원문과 원hash를 보존한다.
예정 GET /__local/ordinary-execution은 bootstrap.json.accessToken을 Bearer로 사용한다. bootstrap 정확 필드: baseUrl(소문자 l), accessToken, expiresAt(epoch ms), processId, accountId/runId/nodeId, sourcePath, databasePath, logsPath, geometrySha256, enginePin, restored. 토큰은 이 private 파일에서만 취득하며 채팅/QA 로그에 복사하지 않는다. GET 응답은 {accepted,status,source,lastSequence,result}; source는 위 engine 입력에서 inputs를 제외한 원천이다. 완료 후 source:null/status만 반환한다. POST는 위 확정 입력 contract이며 정상 응답 {accepted,version,requestId,evidenceRef,result}. 실행 상태는 이 응답으로 갱신하고 매 입력 뒤 GET을 반복하지 않는다. 실제 URL/파일 발급 전 준비완료로 소비하지 않는다.
START 용량 실측(raw UTF-8 64062 bytes, JSON 문자열 envelope 70607 UTF-16 code units): 최소 34노드/3선택 원맵과 필수 전투 source가 기존65536을 초과했다. 2026-11-06 Plan8 지시에 따라 departure-start-size.ts의 START 전용 raw/envelope 상한만131072로 변경, Unity pending/journal/read 동일 경계는 Unity 담당. 클라이언트 입력body65536/공용실행input1MiB 등 다른 제한은 확대하지 않는다. 원필드 삭제/맵구조 우회 없음. 시험 범위밖노드는 localUnavailable이며 첫 노드 종료hold로 다음 지도진행을 차단한다.
기하 v02 definitionId=ordinary-live-obstacle-and-five-v2, SHA256=642a36d851438d26b24103b28494d72a06d6bcf0c204e41ec4eadfcaa02b8bf2. 실제 scene/prefab/originalAssetHashes/entity asset 원파일을 등록 전 대조했다. 신규 로컬 판정기하이며 승인 미술 보정본 아님. 기존 core SHA=2d2a38464b522e6851118ce5835f3c2a6f3560c326a916570814c4764fa194a2에서 OrdinaryInitializationReceipt.Validate line30이 kit v1상수로 실제 v2를 거부하여 Unity 담당에 원실패입력을 공급했다. 재현 원본 QA/CommanderS1-20260922/OrdinaryLiveExecution-20261106/session-v02/engine-inputs/last-failed-input.json(토큰없음). evidence:null인 기존DLL의 목표완료 증거 생성도 미완료.
OS 세션 잠금 helper는 FileShare.None을 소유한다. 읽기리뷰에서 helper 상실 뒤 이전서버가 DB를 덮는 경합을 지적하여 DB 저장 자체도 잠금을 가진 helper의 SAVE/SAVED로 이동 중이다. 정상응답은 DB flush/rename 확인 후에만 전송한다. bootstrap/session.sqlite가 실제 발급되기 전 endpoint 완료를 주장하지 않는다.
#### SERVER_5 검증 갱신 — 2026-11-06 (실제 Player 연결 대기)
잠금 경합 수정 완료: parent는 DB 파일을 직접 저장하지 않고 잠금 보유 helper에 SAVE를 요청하며 flush(true)/rename 후 SAVED를 받아야 성공 응답한다. host-v06 빌드 경고0/오류0. QA/CommanderS1-20260922/OrdinaryLiveExecution-20261106/session-lock-check.json 실제 Windows 프로세스 검사5항목 PASS, 후속 읽기리뷰 추가 실질 결함 없음. game-api/local scripts tsc 및 변경범위 ESLint 통과. ordinary-live-regression-20261106.json 99PASS/2FAIL은 기존65536 기준 초과거부 시험값이 새한도 안으로 들어온 2건; 해당 입력을 새상한 초과로 수정한 ordinary-start-size-final-20261106.json 11PASS/0FAIL/75filtered, 새한도 START의 SQLite export/import·원key 재호출·중복소비0은 ordinary-start-size-restore-20261106.json 1PASS/0FAIL. 중첩 검사합산하지 않는다. 원99PASS의 재시도/HP원자확정/구snapshot 검사결과는 재사용한다.
재개 원본: QA/CommanderS1-20260922/OrdinaryLiveExecution-20261106/session-v02-config.json(현재 host-v06, 기하v02, 기존실패 core pin 유지), local-server.cjs(현 서버소스 bundle). Unity 소유 kit binding 원버전검사 수정 DLL을 받은 뒤 새원파일 SHA를 대조·별도host출력에고정하고 시작한다. 기존 실패기록/원시드는 덮거나 성공으로 바꾸지 않는다. 실제 Player 완료/원목표 evidence/정상상주 URL/세화면비는 아직 미검증. 사용자 seed '기존 상세안 적용' 유지, 새 seed정책/seed1 없음. 공통 데이터 재사용 원문 AGENTS.md#기존-정보-재사용·데이터-갱신-공통-규칙의 원천·소유자·무효화·원응답/원키 복구 계약 유지.
#### 실제 상주 세션 발급 — 2026-11-06 / 초기화 검증 범위
Unity 새 core 0a4c9ef17f1ba1913ddf0d0f3d78805a38736f640ed3d8499a482099031aae15 및 기존 Newtonsoft 원SHA 대조 후 host-v07에 고정했다. hidden node PID39092, baseUrl=http://127.0.0.1:61404, bootstrap=QA/CommanderS1-20260922/OrdinaryLiveExecution-20261106/session-v03/bootstrap.json. accessToken은 이 파일에서만 읽고 채팅/로그로 복사하지 않는다. 만료 expiresAt=1793976554667(epoch ms); TTL1시간, 종료 후 DB보존/재시작 인증회전. session-v03-config.json과 local-server.cjs가 실행 설정/번들이며 다른 DLL로 기존 run pin을 바꾸지 않는다.
실제 SQLite 영속세션 START/초기화/ARRIVE/공용 Execute(empty) 통과. source-check.json: unauth401, Origin403, 미노출경로404, Bearer GET200/no-store, tick0/lastSequence0, 원 full START/raw geometry 공급 확인. source-response.json은 토큰없는 실제원응답(222344bytes)이다. 실제 입력을 진행하지 않아 Player가 원시도 처음부터 연결할 수 있다. GET source는 읽기/접속복구용이며 각 명령 성공후 같은결과 재조회하지 않는다.
이번 core 범위는 초기화/연결준비이고 원사수 noTarget 이동/safe-stop/최종 목표 evidence는 Unity 후속수정 중이다. 현재 세션을 전체 전투 판정 완료로 사용하지 않는다. 새 최종DLL 공급 시 새 pin/새 격리세션을 만들며 진행run 원판정버전은 덮어쓰지 않는다. 실제 Player HTTP/HUD/전체 전투 인수는 아직 미검증.
#### Player-live-v01 동일 pin 세션 교체 — 2026-11-06
Unity core SHA256 cbf0993312864c564bc75707e0de1eaadfc0be26e8b362c389f03ca9b43648ba 및 기존 Newtonsoft SHA를 실제 원파일/host-v08 복사본에서 확인. host-v08 빌드 경고0/오류0. session-v04-config.json으로 새 격리DB/원START/원seed를 생성하고 기존 v03 진행run/pin은 변경하지 않았다.
현재 baseUrl=http://127.0.0.1:58251, hidden node PID20944. bootstrap=QA/CommanderS1-20260922/OrdinaryLiveExecution-20261106/session-v04/bootstrap.json, 토큰은 파일참조만. expiresAt=1793977754647, 2026-11-07 00:09:14 KST. 실제 source-check.json auth200/no-store/unauth401/Origin403/미노출404 통과, tick0/lastSequence0 유지, 원응답222775bytes는 source-response.json(토큰없음)에 보존. 다음 실제 3화면비×slow/fast 6사례는 Unity 제품 Player가 실행하며 현재 서버준비 검증과 구분한다. 각 restart는 원 snapshot/seed 재사용 + 새generation/빈prefix이며, 새generation의 첫 ADVANCE batch 전에 빈 inputs/expectedSequence0 POST를 보내 wall-time 시작점을 먼저 고정한다. 각 입력 원key/body는 결과불명확시 그대로 재전송하고 정상 응답 result로 갱신한다.
v03 PID39092는 본인 소유 PID를 확인한 로컬 debugger 연결에서 기존 SIGTERM handler를 호출(stop-v03.cjs), DB 저장/서버 close 흐름을 수행하고 debugger 연결 해제 후 프로세스 종료를 확인했다. Windows 강제종료/타프로세스 종료 없음. v03 bootstrap/DB/로그 및 이전 원본을 보존했다. 새 core에서 전체 작전종결/실제 6사례 Player 완료는 아직 확인하지 않았으며 이번 세션 준비를 그 완료로 승격하지 않는다.
#### 최종 연속화면 검수용 세션 — 2026-11-06
Unity 제공 player-live-v06-verification.json/html의 실제 HTTP Player 6사례 기록을 확인: 1280×720/1260×540/960×720 각각 slow/fast, 원v04 generation4–9·수락요청30–37·사수명중9–10·HP사건10–11. 이는 Unity 수행 근거이며 SERVER_5가 화면을 직접 시각검수했다는 뜻은 아니다. 기존 기능증거를 보존/재사용하고 전체작전종결과 구분한다.
최종View 표시projection core SHA44264ea702dddd76cf6c9680c40cce95993aa5d77623b6adddab50b9f70841ae 원파일/host-v09 복사본 및 Newtonsoft 기존SHA 일치. 새 session-v05-config.json으로 별도DB/run 생성, 기존run pin불변. baseUrl=http://127.0.0.1:57303, hidden node PID43312, bootstrap=QA/CommanderS1-20260922/OrdinaryLiveExecution-20261106/session-v05/bootstrap.json(accessToken 파일에서만 취득). expiresAt1793979808222 = 2026-11-07 00:43:28 KST. source-check.json의 auth200/no-store/unauth401/Origin403/미노출404·tick0/lastSequence0 확인, 토큰없는 원응답222775bytes source-response.json 보존. 새 16:9 slow/fast 연속프레임 및 다른2비율 시각검수는 Unity 수행 대기.
기존v04 PID20944만 로컬 debugger의 PID 확인 후 기존 SIGTERM handler 호출(stop-v04.cjs), DB flush/close 후 프로세스 종료확인. v04 DB/원영수증/로그/6사례 파일 삭제·덮어쓰기 없음. 새 세션 원인증/1시간TTL/현재attempt·원seed재입장·새시도 empty open·원키회복 경계 유지. 이번 교체에 제품소스/판정/HP규칙 수정 없음. host-v09 build0error/0warning. 실제 새시각검수/전체종결은 완료 주장하지 않는다.
#### 후미추격 표시 Player 검수용 동일 버전 세션 갱신 — 2026-11-07
요청된 동일 core44264ea702dddd76cf6c9680c40cce95993aa5d77623b6adddab50b9f70841ae/geometry642a36d851438d26b24103b28494d72a06d6bcf0c204e41ec4eadfcaa02b8bf2와 host-v09/Newtonsoft의 원pin을 대조하고 기존 local-server.cjs로 새 session-v06 격리DB/run을 생성했다. 제품소스·전투판정·기존run/seed/pin 변경 없음. 만료된 v05 PID43312가 실행 중이 아님을 Get-Process로 확인했고 과거DB/로그/검수자료는 보존했다(CIM조회는 접근불가였으며 종료여부는 Get-Process로 별도확인).
현재 baseUrl=http://127.0.0.1:54212, hidden node PID46600. bootstrap=QA/CommanderS1-20260922/OrdinaryLiveExecution-20261106/session-v06/bootstrap.json; accessToken은 이 파일에서만 취득. expiresAt1794024231444 = 2026-11-07 13:03:51 KST, TTL1시간. 설정 session-v06-config.json. 실제 source-check.json: auth200/no-store/unauth401/Origin403/미노출404·tick0/lastSequence0/raw geometry 확인. 토큰없는 실제 응답222775bytes는 source-response.json. 기존 restart/currentAttempt·원seed재사용·새generation empty open·원key복구 경계 그대로다. 초기화/원본공급만 이번에 재검증했고 이전6사례 기능증거는 재사용한다. 후미추격 표시의 새3비율 실제 Player 시각검수는 Unity 담당이며 이번 서버갱신이 그 완료를 뜻하지 않는다.
#### 전투순차01 2D원화 배치 검수용 세션 갱신 — 2026-11-07
Plan 요청에 따라 같은 core44264ea702dddd76cf6c9680c40cce95993aa5d77623b6adddab50b9f70841ae/geometry642a36d851438d26b24103b28494d72a06d6bcf0c204e41ec4eadfcaa02b8bf2를 유지했다. host-v09/Core/Newtonsoft/geometry 실SHA 대조 후 기존 local-server.cjs로 session-v07 새 격리DB/run 생성. v06 PID46600 미실행 확인, 과거DB/원영수증/검수자료 보존. 제품코드·운영배포·실사용자 상태 변경 없음.
baseUrl=http://127.0.0.1:63419, hidden node PID30768. bootstrap=QA/CommanderS1-20260922/OrdinaryLiveExecution-20261106/session-v07/bootstrap.json(accessToken 파일참조만). expiresAt1794031072777 = 2026-11-07 14:57:52 KST, TTL1시간. session-v07-config.json 설정. source-check.json 실제 auth200/no-store/unauth401/Origin403/미노출404·tick0/lastSequence0/raw geometry 통과, source-response.json 토큰없는 원응답222775bytes 보존. 기존 원restart/attempt/seed/empty-open/원key복구 유지. 초기화·인증·원본공급만 재검증했고 2D원화 시점/비율 배치 실제 Player 검수는 Unity_17 담당이다. 과거 기능증거 재사용, 새 시각검수/전체작전종결 완료 주장 없음.
<a id="nyx-five-intake-20261107"></a>
### 레이나·카일·이브·세라·닉스 실제 편성 원천 착수 — 2026-11-07
사용자 최신 편성 순서 op_reina/op_kyle/op_eve/op_sera/op_nyx. SERVER_5 소유: server checkout의 승인 Nyx 정의 및 원천 발행 경계/관련시험, 기존 인계·49. Unity_17이 실제 handler/Player/새 geometry를 소유, 해당 파일 편집0. 기존5인 run/원snapshot/hash는 보존하며 Mira ID의 Nyx 외형 대체0. 현재 shared-core44264…와 geometry642a…는 기존 Mira 편성이고 새 편성세션으로 재사용 불가.
승인 확인:44#extended-operators-vex-nyx-iris-v01(6850 사용자승인,6872 Nyx HP880/ATK47/1800ms,6882–6894 영역6/8초·tick1초·최대2/3체·총예산60/120%·지연15/25%·연속2회 실제피해 후 분배몫+10%),44#def-shield-effect-mapping-v12(사용자승인 기본DEF 고정표 Nyx53),44:6888–6889 11/04 장갑약화8/15%p 승인. 정식ID는17:630 op_nyx. 구fixture의 단일피해능력/주기1300ms를 현행영역능력으로 복원하지 않는다.
현재 코드 근거: combat-policy-catalog/binding·owned-base-definitions·ordinary-stat-owner는 기존5인만 발행, ordinary-basic-source는 Mira profile을 필수로 검사, ordinary-relocation-source는 기존10개ability만 정확대조. Unity OrdinaryFiveStartInput/Kits도 기존5인, Battle 검색상 Nyx는 표시/role range만 있고 실제 영역handler 미발견. 현재 geometry에는 Nyx 원자산/총구/피격등록 없음. 승인된 HP/ATK/DEF/능력 수치 원천은 별도 고정 정의로 준비할 수 있으나 아래 미결을 지우고 START를 성공시킬 수 없다.
정확한 미결:①Nyx 기본공격의 실제 contact/projectile·모드별mm거리/LOS·탄속/반경/수명·예열/commit/회수 계약(44 dual-range 다른요원세부는 제안; 슬롯범위2만으로 물리값 결정불가),②외부객차 영역의 선택범위/경계·멤버십·생성실패 계약(44#nyx-exterior-zone-proposal-20261018 및 마지막 담당승인절이 아직 제안, 병주 적차량은 시험범위 밖),③11/04 장갑 %p약화를 현재 numeric DEF 감소와 어떻게 연결할지 최신방어원천 계약(임의로 DEF8/15% 감소로 치환금지),④실제영역/지연/패시브·공유약화 handler와 Nyx 기하 원본. ①–③은 Plan 결정 원천,④는 실행구현/자산 의존으로 구분한다. 준비된 정의를 실행지원/완료snapshot으로 승격하지 않는다.
#### Nyx 승인 원천 준비 결과 — 2026-11-07 / 부분 완료
Plan8 확인으로 기본공격 세부/외부영역/장갑%p→숫자DEF 공백을 재확인했고 사용자 편성요청을 새 수치승인으로 확대하지 않는다. server/apps/game-api/src/combat-nyx-approved-source.ts의 captureNyxApprovedSource가 승인 고정 요구사항만 독립 발행한다. version=combat.nyx-approved-requirements.v1.20261107, sourceHash=ecf13573583877e76fffdc93f7c9c31c6b7b55394ed7e2fb5136afeae094d584. 실제 출력 QA/CommanderS1-20260922/nyx-approved-source-definition-20261107.json. execution.status=REQUIREMENTS_ONLY/canStart=false, 미결5종 명시. 이 정의는 실제 START/기존 policy catalog에 주입하지 않으며 승인수치의 고정/소비준비만 의미한다. 기존5인 definition/hash/현재run과 미라자산은 변경하지 않았다.
QA/CommanderS1-20260922/nyx-approved-source-20261107.json 5PASS/0FAIL: canonical hash/소비자변경 격리, 요청순서 원ID보존, 현재 미지원 편성 발행거부, 기존policy불변 및 기존binding회귀. game-api tsc/scopedESLint 통과. 실제 Nyx stat/kit/basic/relocation/roof snapshot·등록기하·공용handler·새 Player session 발급은 미완료이며 미라대체/누락효과 성공세션0. Plan 정책결정 후 새버전/실제원천/공용DLL/geometry 일치 검증을 거쳐 새격리세션을 발급한다. 원입력/성공응답 재사용·원key복구는 기존 AGENTS.md 데이터재사용절 유지, 현재 작업은 표시-only 조작으로 실행지원 검증을 대체하지 않는다.
#### Nyx 정책독립 검증 경계 착수 — 2026-11-07
Plan8 요청: 사용자결정 대기중인44#nyx-execution-intake-next-20261107 후보는 실행에 넣지 않는다. SERVER_5는 승인정의의 scope/요청5인slot binding adapter 및 실제owner가 공급할 영역 생성·tick·이탈·종료 trace의 순수 validator를 구현한다. 수명/교체/예산분배/연속피해패시브 검증은 승인정의에서 읽고 임의기하·탄도·DEF후감쇄 handler는 추가하지 않는다. trace 검증은 원사건형식/규칙 검증이며 실제membership/피해진위나실행권위가 아니다. 결과 canStart=false/원미결목록 유지, START·공용Unity실행기·기존source 수정0. 담당 파일 combat-nyx-intake.ts 및 unit tests; 정책후보 검산은 QA에만 보존한다.
<a id="nyx-source-consumer-contract-20261107"></a>
### Nyx 새 5인 소비 원천 계약 — 2026-11-07 / START 이전
최신 승인44#nyx-mitigation-zone-approved-20261107 및 #nyx-basic-trial-approved-20261107 적용. 앞의 기본탄도/감쇄계산 정책 미결 기록은 역사 상태이며 현재 미결이 아니다. Server5 소유 변경은 approved-source/owned-base/profile/policy/stat-owner/kit/basic/relocation 및 관련검사. Unity 원본 편집0. 기존 v1 bytes와 구run은 보존한다.
소비용 실제 출력: QA/CommanderS1-20260922/nyx-owned-source-contract-20261107.json. scope=LOCAL_SQLITE_OWNED_SOURCE_CONTRACT_ONLY/canStart=false. 로컬 SQLite의 보유/코어/편성/열차 원천을 실제 producer가 읽은 결과이며 실제계정 START·원도착·전투 결과가 아니다. 계정 a/run nyx-run의 연결검사용 해시를 실제 run에 복사하지 않는다. 실제 발행 때 동일 producer가 원계정/run/개정으로 다시 결속해야 한다.
- approved.packet.version=combat.nyx-approved-requirements.v2.20261107, sourceHash=753e8bca39e931f279afe5d7eeeafbbea36f9ff446cc4088bb429312b6b11d67. 기존 v1 hash ecf13573583877e76fffdc93f7c9c31c6b7b55394ed7e2fb5136afeae094d584 유지.
- policy=combat.policy-catalog.nyx.2026-11-07.1; owned base=combat.owned-base.nyx.2026-11-07.1. 슬롯0..4는 op_reina/op_kyle/op_eve/op_sera/op_nyx. Nyx level1 HP880/ATK47/DEF53/C1800, 기존 소유 성장 계산 재사용.
- ordinaryStatSource.origin.version=combat.ordinary-five-core-stats.v2.20261107; originHash=62ce246abc48f4cc901eb73c96394979f1471d03534d79f1db55f5458de1204b.
- ordinaryKitSource.packet.version=combat.ordinary-five-kit.v3.20261107; kitHash=635a143f916916cca9d757781f094ce3970b8a2475961772ba65f492457775ed. abilities의 op_nyx.active.effect=FIXED_ZONE_CREATE, ultimate.effect=FIXED_ZONE_REPLACE_ON_CREATION. 각 mitigationRateReductionBps=800/1500; armorWeakeningPercentagePoints 필드 없음.
- ordinaryBasicSource.packet.version=combat.ordinary-basic.v4.20261107; basicHash=d6c85c78176987bf65092ea178607db7040ccf96c070ef6d1a44a6dc3ad542d1. profiles는 요청 슬롯순서이며 Nyx 공용 W300/R300/속도48000mm/s/반경60mm/최대24000mm/500ms. 실제 원총구·피격/세대·기회 고정은 소비자의 검증 대상.
- ordinaryRelocationSource.packet.version=combat.ordinary-relocation.v8.20261107; relocationHash=76d085ddb40b782f493266b02c87e97807b399b0e14bf99bf43ba6e11206ab53. skillScopes에 Nyx2개, Mira0개. nyxZone에 승인된 고정영역/수명/패시브/감쇄 의미 포함. train/rail/roof 원천도 같은 출력파일에 있고 roof operatorIds 요청순서 일치. runtimeSupported/canStart=false 유지.
명시 효과 출처: approved.packet.zone.sourceKind=NYX_ZONE, effectKinds.mitigation=MITIGATION_RATE_REDUCTION, effectKinds.newBasicPeriod=NEW_BASIC_PERIOD_INCREASE. TAUNT/DEF_WEAKENING으로 위장 금지. live current-effects v1은 Unity OrdinaryLiveNumericAuthority.ValidateCurrent에서 TAUNT만 지원하므로 새 타입 검사와 현재영역 owner 투영을 Unity가 연결해야 한다. 이 정적 선언은 해당 live 검사 구현이나 실제 source 권위가 아니다. 원owner가 검증한 대상별 감쇄 행 {sourceId,valueBps:800|1500}만 숫자계산에 공급하며 별도 지연15/25%는 새로운 기본기회 예약에만 적용한다. 효과sourceId는 원run/node/attempt/zone generation에 결속하고 같은 효과 중복ID/다른값·다른수치그룹의source 재사용을 거절한다.
Unity 소비 대응: NyxZoneExecution.TimingSourceVersion은 확인 시 v1을 가리키므로 새 정의 v2/hash와 일치시킬 것. CommitCreatedZone은 실제 생성성공 이후만 호출; Step에는 원기하 owner의 살아있는 영역원천을 NyxZoneMember(Id,Generation,DelayImmune,Protected,MitigationRateImmune)로 전달한다. DelayImmune과 MitigationRateImmune은 별도이며 임의 false 기본을 실제면역 부재 증명으로 사용하지 않는다. ms→logical tick은 원step으로 정확변환, 누락 tick을 현재멤버로 되채우지 않는다. 새source 선언과 공유코어96검사(Unity 기존 QA)는 실제host/현재시도 내부상태복사/현재노드시작 재입장·Player 검증을 대체하지 않는다.
정확한 잔여: 원zoneId/owner generation/frame/carId/local boundary/선택거리/LOS 및 대상 id+generation/면역·보호/생성실패·이탈·종료 순서의 실제 owner; op_nyx 원asset/hash/FOOT·EMISSION·HIT/capsule/liveLayout. 능력 선택거리 수치는 확인된 승인절에서 확보하지 못했으며 기본탄도24m를 능력거리로 복사하지 않음. 기존10m 전장초안이나 Mira 기하를 Nyx 운영기하로 대체하지 않는다. 실제 departure 경로의 새 policy/kit/basic/relocation 선택, run equipment/stat effect 소비, START→ARRIVE·host/current-effects·현재시도 내부상태복사/현재노드시작 재입장·HUD와 새격리session/Player는 아직 미완료. 원격배포/실경제0.
검증: nyx-source-contract-20261107.json 16PASS/0FAIL(새소유원천연결·roundtrip·구버전거절·과거hash·영역trace·감쇄산술). 최초 연결검사 ordinary_relocation_basic_version_unsupported 실패를 확인하고 v4 전용 내부경로로 수정; 구public v1은 새basic거절 유지. 읽기전용 독립검토 Critical/Important0(리뷰어 재실행0). Server 산술은 HP/geometry 권위를 증명하지 않는다.검증 마감: 최종 v2 효과종류 추가 후 source-contract16PASS/0FAIL, game-api tsc exit0/대상ESLint exit0. 기존 source 회귀 nyx-retained-source-regression-20261107.json=85PASS/2FAIL, 실패2건은 5094/5024ms에 STACK_TRACE_ERROR만 반환(원인 확정 불가). 해당2건만 testTimeout=30000으로 분리 실행한 nyx-retained-source-recheck-20261107.json=2PASS/0FAIL, 나머지는 재실행하지 않음. 전체87개를 한 실행에서 모두통과했다고 주장하지 않는다. 변경대상 tracked diffcheck exit0; 전체작업트리 diffcheck는 무관한 activity-routes.ts:95 기존 EOF 공백으로 exit1이며 이 작업에서 수정하지 않음. untracked 파일은 diffcheck 범위 밖이므로 타입/lint/실행검증을 근거로 사용. 소비계약/해시·정확잔여는 Plan8에 결과전송했고 도구 threadId=01a19f6c-aa61-7c91-9501-3b14875127ad 반환 확인. 실제 START/Player 완결 아님.
<a id="nyx-node-restart-correction-20261107"></a>
### 사용자 정책 정정: 중간이탈은 전투초기화 — 2026-11-07
Plan8이 전달한 직접 사용자정정 우선: 중간전투 저장/재접속 이어하기 폐기. 현재노드 식별·작전진행/노드재시작 원계약만 재사용한다. 앞 Nyx 소비계약에서 checkpoint 복원을 실통합 필수잔여로 확대한 표현은 철회한다. 실제HP/tick/영역/시전/투사체 중간진행을 재접속 이어하기 원본으로 발행하지 않는다. 멱등성 영수증·거래안전·서버 검증/기존로그·역사snapshot은 이어하기와 구별해 보존한다.
확인된 확대: Nyx basic v4/relocation v8은 공용 구계약의 restore 문구와 checkpoint/Player restore 미완료 조건을 상속했다. 새 Nyx 중간저장 API/DB는 추가되지 않았으나 이 정적 소비계약이 잘못된 요구를 전달할 수 있어 새버전으로 철회했다. 기존 검사용 파일/해시와 구version reader는 역사자료 재현용 보존, 신규소비 금지. 다른 실행기/기존 거래저장 전체삭제 없음.
현재 소비용 파일은 QA/CommanderS1-20260922/nyx-node-restart-source-contract-20261107.json. approved v2/stat v2/kit v3 및 해당 해시는 앞 기록과 동일; basic=combat.ordinary-basic.v5.20261107/hash e50b682df671ff2d1656334fe4a49bf5fb564195611f7087b2312724b9361b8d; relocation=combat.ordinary-relocation.v9.20261107/hash ea7d728c01de42a147711bbf93125ca49013cefe3f87c3c4bd496eb4063982e1. v4/v8은 현재 신규소비 대상에서 제외한다.
신규 계약 모든 restore=RESTART_FROM_ORIGINAL_NODE_START_NO_MID_COMBAT_RESTORE, relocation.reentry=EXISTING_CURRENT_NODE_RESTART_FROM_ORIGINAL_NODE_START, midCombatPersistence=false. 상속된 checkpoint/필드목록은 현재시도 메모리 소유·동일시도 중복처리 방지 정보일 뿐이며 persistence=NONE_CURRENT_ATTEMPT_MEMORY_ONLY 명시. 이를 저장/재접속 복원 DTO로 직렬화·공급하지 않는다. 영역/발사체/시전의 실행중 수명 규칙은 유지하고 중간이탈 시 원노드시작으로 초기화한다. 새 missing 목록은 실제기하/현재시도행동·효과 연결만 요구하며 중간복원/Player restore 검수 요구를 제거했다.
검증 QA/CommanderS1-20260922/nyx-node-restart-contract-20261107.json 3PASS/0FAIL: 모든 중첩 restore 정책일치, midCombatPersistence=false, 원멱등성 규칙유지, 구basic v4/relocation v8의 정확해시보존, 새version reader roundtrip·구v8에새basic거절. 실제 START/Player는 미완료, canStart=false 유지. Unity 파일 편집0.
후속 인계 필독(2026-11-07): docs/superpowers/plans_categories/44_COMBAT_DETAIL_CONTRACTS.md#fresh-process-current-attempt-approved-20261102 및 #nyx-node-restart-correction-20261107, AGENTS.md 상단. 중간전투 영속이어하기는 폐기범위로 큐/선행/완료조건 재등록 금지. 현재 승인잔여의 host/상태복사는 현재시도 내부처리이며 재입장은 현재노드시작으로만 수행한다. 기존 node-origin-leave-20261029.json/nyx-node-restart-contract-20261107.json 검증 재사용; 문구정리 신규테스트0. 역사QA·구snapshot 보존.

<a id="nyx-zone-selection-48m-server-20261107"></a>
### Nyx 48m 선택 원천/검증 소비계약 — 2026-11-07
44#nyx-zone-selection-distance-approved-20261107 원문 확인·사용자 승인 적용. 선택거리 정책대기는 해소. 범위는 FOOT→선택zone대표중심 실제3D ≤48000mm 및 권위LOS, 아군침입구역 OR 적선택객차 한구역. 기본24m/칸범위·현재노드시작 재입장 유지. 기존 정책v2/statv2/kitv3/basicv5 수치해시는 바꾸지 않고 선택정의 companion을 신규 relocation에 결속한다. 정책v2 내 선택거리 미공급 표기는 역사적 제한이며 신규소비는 아래 v3 선택계약을 사용한다.
최신 전체 검사용 출력 QA/CommanderS1-20260922/nyx-48m-owned-source-contract-20261107.json; 선택정의 단독 QA/CommanderS1-20260922/nyx-zone-selection-source-20261107.json. approved=combat.nyx-approved-requirements.v3.20261107/hash19e9e69aca40fcf5ab60205ef1949b921c81ff32e8bb093adeccee269b12b6e4. selection=combat.nyx-zone-selection.v1.20261107/hash126e5387830a0e71c9dcc55dccf1f2fca9c5f33b02355e284204dad687b5999b. relocation=combat.ordinary-relocation.v10.20261107.
정확 relocationHash=ed500649ba3b14720c0d81a6407f63a8c1e1dbdfebbd95658b5ad2a7dd841351. packet.zoneSelectionSource={packet,sourceHash}에 approvedVersion/approvedSourceHash/geometry/scope/canStart=false/midCombatPersistence=false를 포함. approved.packet.zone.selectionGeometry와 동일 의미이며 기존v9의 원노드재시작/현재시도내부상태 제한을 그대로 상속. 이전 v4/v8/v9 출력과 해시 보존. basic v5/hash e50b682df671ff2d1656334fe4a49bf5fb564195611f7087b2312724b9361b8d 유지. stat/kit/기본공격 수치버전 재발행 없이 새 선택계약을 읽어야 한다.
Server combat-nyx-zone-selection.ts 입력 계약: scope{accountId,runId,nodeId,attemptGeneration,snapshotHash}, authorityRevision, footWorldMm{x,y,z}, lineOfSight, eligible, zone{zoneId,kind:ALLIED_TRAIN_INTRUDERS|ENEMY_SELECTED_CARRIAGE,carId,frameId,ownerGeneration,representativeCenterWorldMm,localBoundary{minMm,maxMm},valid}. 단위 정수mm·안전정수, 엄격 필드검사/실3D제곱거리 BigInt 비교. 이 함수는 신뢰된 기하owner의 관측을 검증할 경계이며 외부body/boolean을 권위기하로 승인하는 공개API가 아니다. acceptNyxZoneSelection에서 거리/LOS/적격/유효성 검사 후 원범위의 불변복사 보유. validateCreation({scope,zone})에서 같은 node/attempt·zoneId/kind/carId/frameId/ownerGeneration/localBoundary와 현재valid를 재검사. 이동한 대표중심에 대해 거리를 재선택하지 않고 다른구역으로 대체하지 않는다. 반환zone은 수락원본이며 현재세계좌표를 대신하지 않음; 현재위치는 기존frame owner가 소유한다. 불일치는 오류, 기존활성영역을 수정하지 않는다. 이클래스는 활성영역/피해·수명 owner가 아니므로 실제 생성성공후 교체와 생성실패시 구영역보존을 Unity의 실제dispatch에서 검증해야 한다.
검증: nyx-48m-source-contract-20261107.json7PASS/0FAIL(소유source·reader roundtrip/기존해시/3D거리·차폐·고정·단일구역), nyx-48m-intake-20261107.json10PASS/0FAIL(v3승인source intake 및 기존영역trace 포함, 중복합산하지 않음). 48000허용/48001거절·y28800/z38400=48000허용 및 높이+1거절, 가까운경계로 우회불가, 차폐/부적격/무효·복수구역·경계역전 거절, 수락후90m이동 허용하되 다른zone/frame/generation/attempt 거절, 입력/반환변조격리. game-api tsc/대상ESLint exit0. 실제world geometry/LOS 사실증명·실host 생성실패·Player 검증은 아님.
잔여는 실제기하/관측owner 및 departure 실제 START 원천발행·live host dispatcher/HUD/Player. 거리숫자 미승인을 더이상 선행으로 재등록하지 않는다. 실제zone representative center/경계/frame/generation/차폐와 op_nyx FOOT/EMISSION/HIT 원기하는 공급검증필수. 중간전투저장/재접속이어하기 추가0, canStart=false. 후속 필독은44#fresh-process-current-attempt-approved-20261102/#nyx-node-restart-correction-20261107 유지.
<a id="kyle-boarding-source-20261107"></a>
### 카일 실제 적격표적 준비 — 기존 승차적 생성원천 / 부분 구현
사용자 실제 카일공격·발/가림 검수의 Plan 지시. QA/ObstacleTrial-20261106/kyle-right-01/close-eligibility-verification.json 직접확인: ranged physicalAdjacentCars0, Kyle Combat slot2/사수slot0,13.416m여도부적격. 해당5PASS는부적격보존이며 발사통과아님. 범위/주기 변경·사수slot위장 금지.
실제 연결경계 확인: Unity OrdinaryLiveCombatSources는 원Enemies.Gunners2명만 authoring등록/ObserveBasic표적열거하고 모든ENEMY를slot0로 읽음. OrdinaryLiveExecution.StartOrdinaryLiveWave는 basic/gunner만 생성. 기존 BEnemyBoardingOwner는 별도로 존재하며 원BWaveBoardingPolicy/실Spatial을 통해 등록→Entrance→Approach→WaitingSocket→Attached→Climbing→Rooftop과 실제 점유/세대를 소유한다. 따라서 현재 host-v09+기하만 변경해서 카일 적격승차적 세션을 만들 수 없다. Unity파일은 Unity소유로 수정0, 필요한 소비구현 경계를 Plan에 전달했다.
Server 신규 ordinary-local-boarding-source.ts의 prepareOrdinaryLocalBoardingSource(original,nodeId,executionOrigin)는 실제 검증된 원stat/START·지도출판/rail/roof를 읽고 원계정/run/node/원START해시/위험도를 결속한다. 노드원seed에서 ORDINARY_LOCAL_BOARDING_V1 도메인분리SHA256으로 기존 BScoutGunnerSource를 사용하고 BBoardingEventSourceOwner.pin으로 승인정의·두웨이브수/구성/개별HP·실ID/entryGroup·cadence/승차단계를 발행한다. 새랜덤/seed1·단일표적임의HP/사수rename/슬롯프리셋없음. 원첫COMBAT노드 범위이며 실제실행/진행중노드 주입함수아님.
소비자료 QA/ObstacleTrial-20261106/kyle-right-01/boarding-source-v01.json, version ordinary.local-boarding-source.v1.20261107/sourceHash bc2ce839afcb18a4cf43c6b7f81f774e3c499996a1b234e5feddc6dcbb1c7d0e. 원자료는 기존 plan-pointer-retry/source-response.json의 실제 격리서버 START+nodeStart 원seed. 생성15명(8/7)을 source에 보존하며 wave수/타입/HP를 카일검수용으로 임의변경하지 않음. physicalHostIds는 원rail physicalConsist.instanceIds 그대로이고 boarding.originalSource.waves[].instances는 실제새ID와원definitionRef를 가진다. 실제 destination은 existing native owner가 살아있는요원차량의 배정수/차량순서로 결정; instance에slot/hostPhysicalId를 임의붙이지 않는다. 이 원자료는 기존run을변경하지 않았고 새세션발행시 새원START/노드원seed로 재생성한다.
Unity가 연결할 정확 기하·owner 계약: 원rail physicalHostIds의 차체별 frame/좌표계 및 Approach 경로, 좌/우 Climb socket, Roof 경로/도착면, 적 원asset/FOOT·EMISSION·HIT/hitVolume이 필요. native 등록사건은 instanceId/definitionRef/hostPhysicalId/frame/local좌표/generation/phase/접근면/socketId를 소유하고 실제소켓할당/반환·기하유효성으로 target kind(APPROACH/ATTACHED/CLIMBING/ROOFTOP) 및 지원영역 적격을 제공한다. 슬롯은 이 실제 physicalHost 매핑의 결과여야 하며 사수slot0 분기를값만바꾸면안됨. 현재원source는 boarding 정책의 GRACE targetable=false, APPROACH 이후targetable, 실제ROOFTOP부터 적attackEligible 및 full original period를 재사용한다. 카일 원0칸/18m 판정은 이같은차량원천·원LOS로계산. 캡슐좌표/승차완료시간을 Server에서발명하지 않음. 구B owner의실Spatial연결과 현재OrdinaryFactory/HP/basic대상/세대 연결은 Unity구현필요.
검증 boarding-source-verification.json9관찰항목PASS: 같은원천재현, 원physicalHostIds일치, 승인전체웨이브/실instance ID유일성·양타입/HP양수, slot미지정, 기존사수ID미재사용, origin변조/없는node거절, seed결속. 최초QA script 경로오류와 nested admission parse에서 plan제거 오류를 확인하고 각각 경로/허용된원admission보존으로수정 후실행성공. 원배치/실registration·점유·이동·발사/Player는 미검증. canStart/runtimeSupported=false, 새session발행0, 기존session/기하/원자료보존. 정책필독44#fresh-process-current-attempt-approved-20261102/#nyx-node-restart-correction-20261107 유지, 중간전투영속복원없음.
다음 Server범위: 실제기하/새core 소비계약과pin을확인한뒤 이원천을 신규local node-entry/bootstrap 경로에결속하고 새격리session발행·실권한/원source/카일발사근거확인. 현재일반목표의첫사수/장애물완료를승차웨이브완료로임의승격하지않는다. 원천준비를실공격가능검수원천통합완료로주장하지않음.
<a id="kyle-boarding-v2-preparation-20261107"></a>
### 새 승차기하·원START/원node seed 결속 준비자료 — 2026-11-07
Unity 공급 실제파일 unity-project/Assets/_LASTRAIL/Content/Battle/OrdinaryBoardingV01/definition.json SHA b3713ea09cbd0cba85b2c996942af12ae275363abd61ce3e83e34dc540b751ed 직접대조. Unity QA boarding-geometry-v01/verification.json의 저장/재로드·3차체6socket/2template/원12entity보존은 제공근거이며 Server가 Unity 재로드/시각검수를 재수행한 것은 아니다. art/combat/player=false 유지.
Server 구현: ordinary-native-geometry에 선택적 boardingGeometry 엄격schema(3D경로접속/고유car/socket/type/차체frame·layout참조), local bootstrap의 실제asset SHA검증에 template 포함. 복사된 적열차차체는 원asset hash provenance+새prefab hash를 독립pin에 결속하고 bootstrap에서 원definition.json 실제SHA와 모든원entity role/definitionRef/anchors/shapes 일치를 검사한다. 기존직접원asset 경로는 유지. 기존layout에 carOrder가 없는 실제자산이므로 신규boarding.cars 순서를원physicalHostIds와원노드입장에서결속하며 빈layout값을0으로추정하지않는다.
OrdinaryLocalNodeRuntime은 실제새START/geometry/원executionOrigin에서 companion을 생성해 runtime.boardingSourceHash만 원노드snapshot의불변pin으로저장한다. OrdinaryNodeEntrySource의strict runtime schema에선택적64hex필드추가. OrdinaryLocalExecutionStore.source는새원run/원node seed로companion재생성→독립pin정확일치확인후 ordinary.local-execution-input.v2.20261107 +boardingSource를공급한다. boarding기하/pin 어느한쪽만있거나pin비정상/재생성hash불일치면거절. 기존boarding없는기하는v1/companion없음 유지. 노드시작원본의hash를정식ARRIVE batch가 계산하므로nodeStart.snapshotHash/currentAttempt.snapshotHash가함께결속되며사후hash덮기없음. firstPlayableScaling.startSnapshotHash도새원START와일치; 그안의enemySpatial.geometryDefinitionHash는별도승인적차체정의hash라새native전체hash로위장하지않는다. 새nativehash는originalStart.ordinaryGeometryReference 및node.runtime.spatial원binding에결속.
준비자료 경로 QA/ObstacleTrial-20261106/kyle-right-01/boarding-session-preparation-v01/prepared-input.json. version=ordinary.local-execution-input.v2.20261107, inputs=[], 실제새 run_e9685d9b-37f3-4d40-8646-fa1bd9a20ac8/node n1-0/attempt1. node snapshotHash=03c9b25613796df0ba1404bc6a5cc8911078351284ff7f6b53deb87e2d63c9d3. boardingSourceHash=15096aac00a3d563a694baa04792a46e047883f92d09788bc8d5adb971de1283. 이번원시드승인전체wave=7/6; 이전run의8/7을고정해복사하지않음. 해당directory boarding-source.json/preparation.sqlite/verification.json/pin-verification.json은동일준비원본이다. prepared-input 파일SHA fe35c1e71a437f963804ffd7b26199a8c47580f82de47d11303d9ca08ceaf611, boarding-source 파일SHA24e697a7e00377cc1962771ee5e1c0584f1302c3bb255734f3c7d96f00d0a8d7; envelope canonical sourceHash와구분한다.
검증: verification.json11관찰항목PASS(새원START/node seed/pin/노드hash/원기하바이트·hash/실scope·원천재사용/접속단절·중복차체·타계정·틀린snapshot거절), pin-verification.json6항목PASS(새START→scaling/node결속·nativegeometry pin·pin누락/재hash된오류pin거절·원준비보존). pin오염주입은preparation.sqlite를읽은폐기용메모리DB의임시transaction에서만trigger를해제했고rollback후폐기; 파일/실행DB/제품guard변경0. 최초시도는기존불변trigger가업데이트자체를거절한것도확인. regression.json 기존geometry/localexecution5PASS/0FAIL, game-api tsc/대상ESLint exit0.
정직한 실행경계: engine pin은 PREPARATION_ONLY_SHARED_CORE_NOT_READY/runtimeSupported=false 표지이며 실행DLL pin아님. 준비generator는 replay 호출시실패하도록구성, executionCount0/liveSessionIssued=false. 준비DB를실행session으로복사해가짜corepin으로기동하지않는다. 새core 실제지원/해시검증후 새실행디렉터리에서 새원START·node seed로다시발행. 준비자료는Unity reader/HP/lifecycle통합검사입력이며 실제등록/접근/소켓/등반/카일발사/Player완료아님. 원자료/과거run/기존session보존, slot위장/수치변경0, 중간전투영속복원0. 후속필독44#fresh-process-current-attempt-approved-20261102/#nyx-node-restart-correction-20261107 유지.
<a id="boarding-interception-source-audit-20261107"></a>
### 승차적 요격 원천 추적 — 승인정의 존재 / 수락행동 권위 미연결
요청된 실제원천 추적결과, 새수치정책 미결이 아니다. 44#combat-b-intercept-duration-approved-v08-20261004 원문에 Kyle 준비250/회수450ms·자기객차, 요격가속만 적용/수락시고정·phase별올림이 승인돼 있고 combat-b-intercept-ability.ts bInterceptAbilityDefinition이 op_kyle.intercept exact named 정의를 이미 발행한다. BInterceptApprovedLocalPolicy 상수 대신 이 정식정의를 사용해야 한다. 기존 boarding v2의 interceptionTiming=null은 역사정의 그대로이며 v3 named정의가별도로 존재한다.
정식정의를 기존 producer로 출력한 boarding-session-preparation-v01/interception-definition.json은 Unity BInterceptDefinitionSnapshot의 ApprovedDefinitionHash=40224481e914e78ef103be378300600942588d50e121a89cf71cedf9edd94970 및 ApprovedPacketHash=ae963c15db6cc7ab33af417fe014082769aa0d9943d1b5fb1a15dba32acbcf8c와 정확일치 검증. 이것은 정적정의이며 실제요격수락 timing snapshot이 아니다. 이전prepared-input/원node pin은수정하지않았다(원파일SHA fe35c1e71a437f963804ffd7b26199a8c47580f82de47d11303d9ca08ceaf611 재확인).
실제누락근거: 현재prepared-input의 runtime에 buffSnapshot/originalHasteSources/totalHasteBps 없음. preparation.sqlite 읽기전용메모리조회에서 account a의 combat_b_account_catalog/combat_b_account_refit_sources/combat_b_selections 각0행. 현재START는 train_a+car_combat/car_gun의3차체·5요원이며 BCarriageBuffRuntime.departure는 각고유slot/physical instance에 한occupant 및 필수 BCB01..07 type/rank1..5/selectionRevision/rankRevision을요구한다. 이를 요원slot=물리차량slot으로 복사하거나 임의BCB03/rank/0가속으로채울근거없음. ordinary entry.effects의빈목록은B직접가속원천의완전성증명이아니다.
코드추적: BInterceptAbilitySourceOwner.pin은 기존 BBoardingEventSourceOwner+현재 BCarriageBuffRuntime+필수 BInterceptOriginalHasteOwner를받고, action 영수증의 kind/ability=INTERCEPT,timing=INTERCEPTION_PHASE,originalEligible=true,실car occupant와eventId를검사한다. originalHasteOwner.read의 account/run/nativeContext/operator/event 결속 및 sources[{sourceId,group:INTERCEPTION_HASTE,valueBps,appliedVersion:null}]를받아 BCB03 action.source.interceptionHasteBps와합한뒤 phases/timingHash/sourceHash를발행. apps/scripts의참조검색에서는 해당owner interface/정의자와named원천의 acceptedTiming:null 선언만발견, 실제ordinary owner구현/조립caller없음. tests의주입가속/ISOLATED_LOCAL_FIXTURE buff는실runtime공급원으로승격하지않는다.
최소미결필드·소유경계(추가수치승인요구아님):
1) 실제현재placement→buff 원소유자: account/selectionRevision/rankRevision,physical host/요원배치,각BCB type/rank의원권리·적용대상,현재 mode/body/actor 및operatingRevision. 현ordinary 3차체·5인과기존1occupant B모델을맞추는승인된원관계/adapter가필요하며 새가짜departure금지.
2) 실제 INTERCEPTION_HASTE 원source owner: account/run/nativeContext/operator/eventId에결속한완전목록·버전/무효화근거. 빈목록은원owner가실제로완전성확인할때만허용, 누락을0으로변환금지.
3) 실제네이티브 요격적격·수락행동 owner: 원탄/target generation·지원영역·현재actorReady와단일예약을검사한 eventId/slot/expectedRevision/INTERCEPT action영수증. 이것은tick0준비때임의로미리수락할자료가아니며각실수락시 current buff와가속원본을고정해야 한다. named definition/abilityHash/stepUs/합계/phases는기존검증기로계산가능하다.
검증: interception-source-audit.json 실제원준비/SQLite누락·정적정의hash확인. interception-contract-check.json 기존정의발행/원owner없음·잘못된holder/가속group·중복source거절2PASS/0FAIL(다른검사선택제외). 제품source/정책수치/이전prepared input변경0, actual accepted timing발행0/session0. 추정CLEAR/가속0/사전수락영수증을넣지않았고 canStart/runtimeSupported=false 유지. 독립승차lifecycle 검증은이누락과별개로진행가능. 현재노드시작재입장만유지/중간복원없음.
<a id="ordinary-five-car-source-audit-20261107"></a>
### 기관차 별도·요원별 5객차 원천 대조 및 소비 경계 — 2026-11-07
작업 승인: 44#ordinary-five-car-formation-correction-approved-20261107. Server_5는 저장 원천/선택/START binding을 소유, Unity는 새 6차체 기하/실배치와 Player를 소유한다. 이번 변경 범위는 기존 계약과 격리 준비DB 대조·소비 경계 기록이며 기존3차체 소스/진행run/QA를 수정하지 않는다. 다인BCB 확대 어댑터는 대상이 아니다.
확인 원본: combat-b-persisted-sources.ts의 PersistedBSelectionCatalog/PersistedBRefitSource, combat-b-refit-source.ts, combat-b-departure-assignment.ts. 선택 0..4·객차당 단일 OPERATOR/CARGO·실물 instanceId·보유 carriageId·공유 buff type별 rank·selection/rank/physical revision 및 원자 guard는 재사용 가능하다. 그러나 기존 prepareBDepartureAssignment는 op_kyle/op_sable 및 actors 최대2만 지원한다. 이를 5인 일반전투의 완성 연결로 재사용했다고 주장할 수 없고, 기존 일반전투 5인 성장/정의 소유자를 연결해야 한다.
실제 읽기전용 대조: QA/ObstacleTrial-20261106/kyle-right-01/boarding-session-preparation-v01/five-car-source-audit.json. 격리 account a/run_e9685d9b-37f3-4d40-8646-fa1bd9a20ac8의 combat_b_account_catalog, combat_b_account_refit_sources, combat_b_selections, combat_b_carriage_instances 모두0행. account_carriages에는 car_cargo/car_combat/car_defense/car_gun 각 level1/revision1만 존재한다. 종류4개 소유는 실물객차5개 보유 증거가 아니며 level을 BCB rank로 바꾸지 않는다. 저장소 내 발급 INSERT 검색 결과는 tests/helpers/combat-b-persisted-departure-fixture.ts의 LOCAL_KIND/physical-0/physical-4 시험자료뿐이며 운영/현재 계정 원본이 아니다. 운영DB는 조회하지 않았다.
Unity 소비 계약: geometry 차량순서0은 별도 기관차, 순서1..5는 선택 slot0..4와 대응한다. 각 slot은 같은 저장 원천의 instanceId/carriageId/physicalRevision 및 단일 occupant.operatorId와 선택된 type/rank/selectionRevision/rankRevision을 함께 가져야 한다. 일반전투 actor의 기존 operatorId에 결속하고 actor 배열 순서를 실물 ID로 대체하지 않는다. geometry의 authoringId/framePath는 표현 참조이며 instanceId 권리를 만들지 않는다. 현재 확정할 수 있는 실물5개 ID/선택 목록은 없으므로 구 train_a+car_combat/car_gun 또는 임의 suffix로 발행하지 않는다. 엔진+5객차의 실물/정의 원본과 새 기하 hash를 확보한 후 새 START/node snapshot에 원자 결속하며 과거 snapshot은 보존한다.
남은 입력: 해당 격리계정에 사용할 승인된 5개 실물 소유/배치·카탈로그 해금·BCB 선택/보유등급 원본 또는 이를 공급하는 실제 계정 경로. 새 권리/등급 임의 발급은 승인 범위 밖이다. 준비된 원본을 받아 일반5인 성장/행동 소유자와 결속하는 작업 및 새 6차체 rail/roof/native geometry 소비 변경이 남는다. 48m 영역/24m 기본 사거리 유지, 구45m 결과 전용0. 현재노드 원시작 재입장 유지. 실제 Player·카일 공격/접지·HUD/Nyx 입력은 미검증이다.
원시드 답변 재확인: 사용자 '기존 상세안 적용'은 이미49#ordinary-node-execution-origin-v1-20261029에 승인/구현으로 기록되어 있다. ordinary-node-execution-origin.ts의 crypto.getRandomValues 및 XORSHIFT32 초기상태 계약을 직접 확인했다. 중복 시드 소유자/별도 생성 흐름을 추가하지 않는다. 이번에는 기존 전투 검증을 재실행하지 않았다.
<a id="five-car-static-consumer-check-20261107"></a>
### 새 5객차 기하의 서버 정적 소비 검사 — 2026-11-07
Plan 전달 ordinary-five-car-boarding-v1 rawSHA e32639b12619b00e47cbc93abfefb0f4ec3db6fbb9d8910aa4a37cbc2737866e 직접 일치. QA/ObstacleTrial-20261106/kyle-right-01/verify-five-car-static.ts를 기존 esbuild로 묶어 실행(exit0), five-car-geometry-v01/server-static-verification.json 저장. scene/prefab/보존원본/실entity·enemyTemplate 참조의 고유9개 자산 바이트hash 일치, carOrder0..5, OPERATOR5명 Kyle/Reina/Eve/Sera/Mira, 각 formationSlot+1=carOrder 및 서로다른5객차 확인. Nyx 원천으로 간주하지 않는다. Unity verification의77m/front97m/tail20m·물리경로/재로드 결과는 Unity 검증이며 이번 서버 시험에서 Player/물리엔진으로 재실행하지 않았다.
실제 현재 ordinaryGeometryDefinitionSchema.safeParse는 source.formationGeometry 미지원으로 거절한다. 최상위 native version과 boarding version은 기존값이며 새 formationGeometry는 strict localSource에 없어 그대로 등록할 수 없다. 등록 시 필드제거/unknown무시로 우회하지 않았다. 후속 소비 변경에는 이 새 메타데이터의 strict 형식/기존 liveLayout·cars와 일치 검증 및 기존 버전 호환이 필요하다. bootstrap의 보존원본 definition.json 정확1개 요구도 이 원본의2개 history definition과 맞지 않으므로 원본 chain을 구분해야 한다(아직 실행/수정하지 않음). 정적 바이트 일치≠서버 등록≠실물권리/START binding≠준입/실Player 성공이다.
등록0/권리발급0/START변경0/기존자료수정0. 이전 #ordinary-five-car-source-audit-20261107의 실제5개 instance/선택/등급 공급 의존은 남는다. 이번 요청 범위인 독립 정적검사는 완료했고 서버 형식 미지원이 확인되었다. 구38m/45m 자료 전용없음, 노드시작 재입장 정책 유지.
<a id="five-car-schema-bootstrap-support-20261107"></a>
### 5객차 strict 형식·보존원본 검증 지원 — 2026-11-07
지원 범위: ordinary.native-geometry-definition.v1.20261031 / ordinary-five-car-boarding-v1 / rawSHA e32639b12619b00e47cbc93abfefb0f4ec3db6fbb9d8910aa4a37cbc2737866e. 제공 원본/version/hash와 Unity 파일은 수정하지 않았다. 앞선 #five-car-static-consumer-check-20261107의 schema 미지원은 이 변경으로 해소했다.
ordinary-native-geometry.ts: 선택적 formationGeometry를 명시 strict schema로 지원. 이 definitionId에는 필수이며 다른ID의 해당메타데이터는 거절한다. 6차체순서/고유 authoringId, 5단일요원/고유 slot·carOrder, 6*body+5*gap=전체길이=front-tail, obstacle/front+280m·Enemy/front+10m, 각 차체 중심/단위/기저/상대좌표/실BOX길이, 요원 frame하위·foot/roof/차체spawn·실world좌표를 일치검증한다. 기존 formationGeometry 없는3차체 형식 유지. unknown 무시/필드제거 없음.
ordinary-session-bootstrap.ts: 기존 자산검사를 verifyOrdinaryLocalGeometry 읽기전용 경계로 추출하여 준비경로에서 재사용. 자산 중복참조의 서로다른hash를 Map이 덮지 않고 거절한다. 이전 boarding 원본은 LiveGeometryV02 한역할, 새5객차는 LiveGeometryV02(ordinary-live-obstacle-and-five-v2/642a36d8...)와 BoardingV01(ordinary-live-boarding-v1/b3713ea0...) 두역할을 정확경로·ID·전체hash로 요구한다. 여분/누락/역할대체거절, 실제원본바이트hash 및 각원본의 보존자산/prefabhash 계승확인. 변경승인된 Engine/Combat/Gun 차체 교체만 별도 처리하고 기존 요원/적 등 anchor·shape 및 boarding enemyTemplate의 원정의를 대조한다. 첫항목 임의선택 없음.
등록/준입 구분: prepareOrdinaryLocalSession은 이새기하의 정적검증 뒤 local_five_car_ownership_binding_unavailable로 계정fixture/DB생성·등록/START 전에 명시차단. 정적검증과 격리등록은 지원하지만 현원계정의 실물5instance·선택/BCB권리는 만들어내지 않는다. 차단의 제거조건은 기존 #ordinary-five-car-source-audit-20261107 원천 실제결속이다.
검증: tests/integration/ordinary-five-car-geometry.test.ts 신규3시험 + 기존 ordinary-native-geometry.test.ts 3시험, 합6PASS/0FAIL. 새6차체·구LiveGeometryV02·구BoardingV01 실제바이트/자산검사→폐기용DB등록→readback 및 새편성 session차단, 형식 불일치13변형과 원본/자산/원entity 변조·누락/역할추가6변형 및 잘못된raw pin거절. QA/ObstacleTrial-20261106/kyle-right-01/five-car-geometry-v01/server-support-tests.json. game-api tsc --noEmit exit0, 변경3파일 ESLint exit0(초기시험 any 표기를 고친 후 재검사). server-static-verification.json도 새schema PASS. 원본3차체검증/기존run보존, 라이브session/권리발급/START/Player0. 실제통합에 남은 원권리필드는 5개instanceId/carriageId/physicalRevision, 단일occupant.operatorId, 선택type·보유rank·selectionRevision/rankRevision 및 catalog 해금/권리revision이며 실제공급원과 Unity 새기하 결속 후 별도 새START/준입 검증이 필요하다. 현재노드 처음재시작 유지.
<a id="party-numbered-position-wire-20261107"></a>
### 출발 준비 전투위치 번호: 기존 formation.slot 재사용 계약 — 2026-11-07
승인원본44#party-numbered-position-preview-approved-20261107. 결론: 저장·활성화·START/노드시작의 논리 전투순서는 기존 formation.operators[].slot(0..4)을 권위 원본으로 재사용 가능하며 UI 전투위치=slot+1이다. 별도combatPosition/새API/이중원장을 만들 필요가 없다. 다만 현재 고정원화배치와 Nyx 전용 순서 제한 때문에 자유배치 실Player 지원까지 완료된 것은 아니다.
실코드 추적: formation.ts의 strict {slot,operatorId}×5는 중복slot/요원·미보유를 거절한다. SAVE는 commander_presets만 변경, ACTIVATE는 원preset revision/계정formation revision·보유·진행run 부재를 원자검사 후 account_formation에 slot순 정렬하여 저장한다. 입력 JSON 배열 순서는 권위번호가 아니다. formation.command.v2.20261021 성공응답의 owners.account.preset/formation과 operators를 기존Model에 반영하며 정상성공 뒤 확인GET 불필요. 변경 ACTIVATE는 ULTIMATE_SELECTION/DEPARTURE_PREVIEW를 무효화한다. 편집중에는 draft, 출발확인에는 확정active formation을 사용하며 SAVE만으로 출발편성을 바꿨다고 표시하지 않는다.
출발→노드: departure.ts의 readState가 account_formation을 검증하고 admission.formation/formationRevision에 원본을 담는다. START에서 원formation_json/revision·소유 원자guard를 검사하고 ordinaryStatSource 및 run_snapshots에 고정한다. combat-owned-profile.ts는 formation slot순으로 정렬해 selected.slot을 그대로 반환하며 훈련레벨은 operatorId로 연결한다. combat-ordinary-stat-owner.ts가 actors[].slot을 보존, combat-ordinary-kit-source.ts가 admission/원actor의 ID+slot 정확동일을 검사하고 combat-policy-binding.ts가 해당선택을 slot순으로 능력정의에 결속한다. ordinary-node-entry-source.ts는 원START actors[].slot 그대로 node entry에 복사한다. ordinary-node-attempt.ts의 restart는 저장된 동일snapshot/hash를 재사용하므로 로비의 새편성으로 진행node를 덮지 않는다. Unity OrdinaryFiveStartInput.cs:74–77은 능력정의/원actor/admission의 slot일치를 확인하고 actors[slot]에 배치한다.
Unity UI 소비: 동일 slot→operatorId 매핑을 읽어 좌측적 preview의 화면좌→우 slot0,1,2,3,4(번호1..5), 우측적은 slot4,3,2,1,0(번호5..1)로만 표현한다. 원배열/slot값을 뒤집어 저장하지 않는다. 1/3/5 가까운깊이줄,2/4 먼줄은 표시규칙이며 새저장필드가 아니다. 실제적방향/월드좌표·사거리/LOS/어그로는 별도공간·전투소유자가 판정한다. 이계약은 번호1이 모든실전 상황에서 현재최단거리라는 보증이 아니고 승인된 최초배치 의도다.
서로다른 slot 의미: operator_training_core_slots.slot_index=육성코어위치, train loadout selectedCarriages.slot 및 B placements.slot=차량선택/권리위치, 장비 slots=장비장착칸, geometry formationSlot=작화초기배치참조, physicalOrder/carOrder/instanceId=실물차체순서/식별이다. formation.slot이 이권리를 변경하지 않는다. 기존 roof-source는 slot순 operatorIds를 고정3+2지붕위치에 연결하므로 새전투위치/5객차의물리매핑으로 그대로 전용하지 않는다.
확인된 실제 제한/최소대안: (1) Unity Battle/OrdinaryLiveCombatSources.cs:38은 liveLayout[operatorId].formationSlot==actor.Slot을 요구한다. 현재제공기하는 Kyle/Reina/Eve/Sera/Mira 고정배치이므로 교환저장 자체가 성공해도 자유교환한START의실기하소비는 거절된다. 번호 UI는 기존wire로 연결하되 실제배치는 원slot→승인된자리→실operator기하를 기존Unity배치owner에서 결속해야 한다. (2) combat-policy-catalog.ts captureCombatPolicyForFormation 및 combat-ordinary-stat-owner.ts Nyx v2 원천은 Reina/Kyle/Eve/Sera/Nyx 정확순서를 검사한다. 이번번호계약만으로 해당고정hash/구run규칙을 제거하지 않았다. Nyx 자유배치 실행지원에는 신규소비버전에서 같은원slot을 허용하도록 범위를맞추고 검증해야 한다. 새combatPosition필드는 두문제를 해결하지 않는다. (3) 새5객차 실물소유/BCB선택 원천누락은 별도의기존의존이며 번호기능으로 발급하지 않는다.
검증: formation.test.ts에 순서가뒤집힌입력배열/번호1·5 요원교환→SAVE→ACTIVATE→성공응답/실DB의slot별동일매핑 확인을 추가했다. 훈련코어/실물객차가 새로생성되지 않음도 확인. 전체 formation 시험13PASS/0FAIL, 해당파일 ESLint exit0. QA/ObstacleTrial-20261106/kyle-right-01/five-car-geometry-v01/formation-position-contract-tests.json. 출발→노드·Unity 경로는 이번에 위코드를 추적한 근거이며 실제교환편성 START→Player 통합시험을 실행한 것은 아니다. 제품wire/저장소스변경0, Unity수정0, 후보미술승격0. 현재노드 처음재시작 유지.
<a id="train-support-transition-server-20261107"></a>
### 설비 버프·열차 전용 요격 전환: 신규 준비 차단과 역사 보존 — 2026-11-07
최우선원본44#train-support-equipment-v01-20261107. 사용자확정은 설비제공버프/중간3객차 설치/요격열차전용·요원관련제외다. 신규Kyle/Sable 요격능력·요원INTERCEPTION_HASTE·기본공격대기/회복점유·요격HUD/패시브연계 공급작업 중단. 총3슬롯/요격포함/동종1개/6지원+1요격/효과범위/선두보너스제외는 권장안이며 신규정의·권리·수치로 발행하지 않는다. 앞선 #boarding-interception-source-audit-20261107의 요원 haste-owner 보충과 #ordinary-five-car-source-audit-20261107의 요원별BCB 결속을 신규진행해야한다는 결론은 대체된다. 실물5차체 소유원천은 여전히 필요하지만 구BCB선택/등급을 신규설비권리로 자동승격하지 않는다.
실소스 영향: combat-b-intercept-ability.ts는 구Kyle/Sable named timing/원가속/action receipt/기본공격동결·회복비용 정의·reader다. combat-b-named-operator-source.ts는 해당정의와 intercept capability/미공급 acceptedTiming을 내보내고, combat-b-carriage-buffs.ts는 BCB03 INTERCEPT/INTERCEPTION_PHASE에 haste를 계산한다. combat-ordinary-basic-source.ts의 구kyleModePolicy.interception은 공격잔여pause/resume을 명시한다. 새정책 소비버전에서 이능력/가속/점유/패시브·스킬연계 표시를 제외해야 하며 동일버전hash를 덮거나 train으로 rename하지 않는다. game-api src 검색상 new BInterceptAbilitySourceOwner 조립호출은 없었다. 구definition route는 runtimeSupported/canStart=false인 역사정의이며 이번에삭제하지 않았다. HUD는 Unity소유로 수정하지 않았다.
최소제품변경: ordinary-local-entry.ts OrdinaryLocalNodeRuntime.prepare는 boardingGeometry가 있는 새노드준비에 409 ordinary_train_support_definition_unavailable을 반환하여 구요원요격 원천생성/초기상태조립 전에 차단한다. ordinary-session-bootstrap.ts는 기존session원본복구 분기 이후, 새account fixture/START 생성 전에 동일차단을 둔다. 새5객차는 기존 실물권리미연결 차단도 유지한다. 원START/기존node snapshot을 읽는 ordinary-local-execution.source의 구pin재현 및 과거 boarding-source-v1 JSON은 그대로 보존한다. 새정책 실행지원과 과거원천 조회를 혼동하지 않는다. 원시드/현재node처음재시작 계약 변경없음.
검증: 새boarding bootstrap이 계정seed 전에거절, 런타임준비가 DB조회0으로거절하는 시험추가. 기존5객차/3차체 정적검증·격리등록회귀를 함께 실행하여7PASS/0FAIL. QA/ObstacleTrial-20261106/kyle-right-01/five-car-geometry-v01/train-support-transition-tests.json. game-api tsc exit0/변경3파일ESLint exit0. 과거준비자료/정의/구START/계정권리 변경0, 신규열차요격 정의/세션발행0. 실제Player/전체요원요격제거 완료를 주장하지 않는다.
남은전환: 승인된 설비정의·보유/장착·구권리이관 계약, 열차요격 탄종/범위/처리량·준비/회복/예약/파손·수치, 신규START policy/definition pin과 공유소비버전이 필요하다. 250/450·가속0·항상성공 대체금지. 기존전투위치1~5/좌우preview·독립이동작업은 계속가능하며 이차단은 해당UI저장을 막지 않는다.
<a id="system-durability-boundary-server-20261107"></a>
### 個別客차HP 폐기·설비 시스템3칸 정책 경계 — 2026-11-07
44#train-system-durability-approved-20261107을 확인했다. 확정은 개별객차HP/DEF/파손·복구의 신규소유 폐기와 열차HP 아래 시스템내구도 설비3칸이다. 실제5객차ID/차체·통로·충돌기하는 유지한다. 원요원HP/열차HP를5분할하거나구객차HP를설비HP로rename하지 않는다. 설비슬롯3칸 확정은 종류별값/피해·수리실행 상세승인이 아니다. v04-system 시안은 검토후보이며 Server 승격/자산편집없음.
현재소스 확인: ordinary-node-entry-source.ts는 원5요원과열차1개 기대target를 검사하고 actors[].hp/maxHp와train.hp/maxHp를각원health owner에서읽는다. 이번진행코드에새객차5HP를공급한변경은없으며 기존6target를설비3칸으로간주하지 않는다. combat-b-carriage-buffs.ts의구body NORMAL/BROKEN은 역사BCB정책상태로보존,신규설비상태owner로전환하지않는다. 앞선 #train-support-transition-server-20261107의 신규boarding prepare 차단 및 구START조회보존을유지한다. 새설비원본이없으므로시스템3칸HP를0/100/열차분할값으로발행하지않는다. 미공급은미연결이며빈슬롯/파손/내구도0과구분한다.
정확한남은의존성: 실제설비선택·instance/definition ID·설치slot0..2와중간물리객차참조·계정권리/선택revision, 설비최대내구도/초기상태·가동정의버전, 열차전용요격정의가필요하다. 이목록은원천필요사항이며새wire/schema발행아니다. 일반피해/설비표적피해분리·단일대상·보호막적격·파손예약처리·수리한개우선순위·양/시간/복구완료·다음node이월은현재권장상세로실행하지않는다. T03/T13/T18/T06은44가명시한설비복구재설계대상이며기존값·owner를자동복사하지않는다. 이번에는해당증강실행경로를수정하거나재설계완료로주장하지않았다.
번호1~5/좌우preview는 #party-numbered-position-wire-20261107의기존formation.slot 계약을유지한다. 번호와설비slot/객차ID는별개, 정상SAVE/ACTIVATE응답으로갱신하며API추가없음. 기존13PASS증거는변경없어재사용, 이번에는정책/인계만정합화했으므로새시험미실행. Unity HUD는열차HP/두번째줄시스템3칸·노즐/선로선택·확정유지의소비책임이며Server가Player연결완료라고주장하지않는다. 구원데이터/START/계정권리삭제0, 제품코드변경0, 현재node원시작재입장/중간HP·수리작업영속이어하기없음 유지.
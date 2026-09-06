# Customer Support Account Summary Contract Draft

This document defines the first read-only customer support account summary
contract for LAST RAIL launch operations.

The read-only Admin API summary route is implemented. This contract does not
create repair endpoints, reward grants, Cloudflare resources, DEV deploys,
remote smoke tests, or PRD/STAGING resources.

## Goals

- Give customer support one account-centered investigation view.
- Keep launch support read-only by default.
- Reuse existing server Source of Truth tables and read models.
- Preserve correlation/reference ids for follow-up investigation.
- Avoid exposing exploit-sensitive internals, credentials, raw request data, or
  reward/RNG calculation internals.

## Non-goals

- No account status mutation.
- No reward repair or manual grant.
- No offline reward timestamp reset.
- No story progress mutation or unlock mutation.
- No gacha repair, pity edit, or economy correction.
- No localized dialogue text payloads.
- No token/session secret inspection.

## Endpoint

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

## Player Inquiry Endpoint

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

## Admin Support Ticket Read Endpoints

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

## Permission

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

## Sections

### Account/Auth Status

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

### Recent Runs

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

### Run Reward Previews/Claims

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

### Offline Rewards

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

### Story Progress/Unlocks

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

### Gacha

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

### Idempotency Replay Status

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

### Integrity Disposition

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

### Ops Error Logs

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

### Launch Login Bonus

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

### Escalation Hints

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

## Player-facing Explanation Templates

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

## Security And Masking

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

## Test Candidates

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

## Rollout Order

1. Contract draft completed.
2. Admin Web support placeholder/contract types aligned by the Admin session.
3. Admin API unit contract tests for `SUPPORT_READ` added.
4. Local D1 smoke with seeded account support data added.
5. `GET /v1/admin/support/accounts/:accountId` read-only route implemented.
6. Add Admin Web read-only integration against the implemented route.
7. Consider separate read models only if direct joins become too expensive.

Remote DEV secret setup, DEV deploy, remote smoke, PRD resources, and
STAGING/PRD deploys remain approval-gated.

## Open Decisions

- Whether VIEWER can see every section or whether OWNER-only details are needed
  for stack/ops metadata later.
- Whether to add a separate `support_case_notes` table after launch.
- Whether to add an `offline_reward_claims` read model or rely on
  `reward_transactions` for launch.
- Exact section limits for recent runs, gacha batches, and ops errors.
- Whether to expose future support escalation `severity`, `category`,
  `recommendedAction`, and evidence package availability as read-only fields.

# Ops Error Logs Contract

This document defines the first server-side contract for runtime/system error
observability in LAST RAIL. It is separate from `admin_audit_logs`, which
records operator actions and permissioned admin mutations.

Current local implementation creates the D1 searchable summary table and
read-only Admin API list/detail routes. Game API also writes a minimal subset of
launch-critical server failures directly to D1 as a local/initial integration.
It does not create Queues, R2 buckets, Workers Logs exports, Cloudflare
resources, remote DEV deploys, PRD/STAGING resources, repair mutations, or
resolution workflows.

## Goals

- Give operators a read-only `/ops/error-logs` view for backend/runtime
  failures.
- Support support triage for reward claim, gacha pull, ledger, integrity,
  Admin API, Game API, Async Worker, Pages Function, and Unity diagnostic
  failures.
- Keep searchable summaries safe for D1 and Admin UI display.
- Keep sensitive raw data out of Git, D1 summaries, logs, and UI responses.
- Preserve correlation IDs so operators can jump from an error to audit logs,
  ledger rows, reward claims, runs, or gacha batches.

## Non-goals

- No production deployment, DNS, secret, or custom domain changes.
- No DEV remote deploy or smoke in this slice.
- No live Cloudflare Workers Logs, R2, or Queues integration yet.
- No raw request-body storage.
- No token, secret, Authorization header, raw RNG material, or raw client
  account credential storage.

## Source of Truth Options

### D1 Table Direct Writes

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
  per event. No additional per-account/minute server rate limiter is implemented
  in this local slice; add Cloudflare/WAF or server rate limiting before public
  PRD if abuse pressure requires it.
- Idempotency retention follows `idempotency_records`; no local TTL/purge job is
  implemented in this slice.
- Admin `category=frontend` is applied in the SQL `WHERE` clause before
  ordering and pagination; exact frontend app version/code filters use the same
  pre-pagination path. This is covered by `ops-log-search` unit tests and the
  client error ingest smoke.

Backlog / decision queue:

- Queue-based async sink and optional R2 sanitized detail payloads.
- Wider automatic write integration for non-critical validation rejects,
  support notes, Admin API read model composition failures, and richer client
  diagnostics.

### Queues + D1 Sink

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

### Workers Logs Query / Export

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

### R2 Raw Payload Export + D1 Search Summary

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

## Recommendation

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

## Stable Event Fields

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

## Masking Policy

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

## Retention Candidates

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

## Access Control

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

## Admin API Endpoints

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

## Correlation

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

## Unity Diagnostic Source

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

## Current Implementation Slice

Completed locally:

1. Added migration `0016_ops_error_logs.sql` for the D1 summary table only.
2. Added read-only Admin API list/detail contract with unit and local D1 smoke
   coverage.
3. Added permission `OPS_ERROR_LOG_READ`.
4. Added a local sanitized event insert envelope helper.
5. Deferred Queue/R2/Workers Logs bindings until explicit remote resource
   approval.

Remote DEV and PRD resource creation remain approval-gated.

## Open Decisions

- Exact PRD retention periods.
- Whether VIEWER can access list summaries or only OWNER/OPERATOR can.
- Whether stack metadata is OWNER-only or hidden entirely until R2 detail
  payloads exist.
- Queue name, R2 bucket name, and lifecycle policies.
- Whether Workers Logs export becomes an official source or remains a platform
  troubleshooting companion.
- Whether Unity diagnostics use a separate table in the long term.

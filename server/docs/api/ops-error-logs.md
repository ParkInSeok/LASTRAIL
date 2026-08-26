# Ops Error Logs Design Draft

This draft defines the first server-side design for runtime/system error
observability in LAST RAIL. It is separate from `admin_audit_logs`, which
records operator actions and permissioned admin mutations.

This document is a contract/design draft only. It does not create D1 tables,
Queues, R2 buckets, Workers Logs exports, API routes, or Cloudflare resources.

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

Candidate D1 searchable summary row:

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

Recommended API field names:

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

Stable enum candidates:

- `environment`: `DEV`, `PRD`
- `service`: `Admin API`, `Game API`, `Async Worker`, `Pages Function`,
  `Unity Client Diagnostic`
- `severity`: `warn`, `error`, `fatal`

`Unity Client Diagnostic` is a source label for fail-soft client diagnostic
events if a remote diagnostic transport is added later. It is not proof that
the Unity client caused a server fault.

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

Permission candidate:

```text
OPS_ERROR_LOG_READ
```

Recommended initial read policy:

- OWNER: list + detail, including sanitized stack/metadata when available.
- OPERATOR: list + detail summary, no raw/sanitized deep payload by default.
- VIEWER: list summary may be allowed if operational read scope permits; if in
  doubt, gate `/ops/error-logs` to OWNER/OPERATOR first.

Admin Web currently uses an interim permission. Server contract should introduce
`OPS_ERROR_LOG_READ` before real API wiring.

Do not use this log model for operator action auditing. Keep admin actions in
`admin_audit_logs`.

## Admin API Endpoint Candidates

List:

```text
GET /v1/admin/ops/error-logs
```

Filters:

```text
environment=DEV|PRD
service=Admin%20API|Game%20API|Async%20Worker|Pages%20Function|Unity%20Client%20Diagnostic
severity=warn|error|fatal
errorCode=<stable-code>
requestId=req_...
accountId=account_...
runId=run_...
rewardPreviewId=reward_preview_...
rewardClaimId=reward_claim_...
gachaBatchId=gacha_batch_...
gachaBannerId=banner_...
occurredFrom=<ISO UTC>
occurredTo=<ISO UTC>
limit=50
cursor=<opaque-cursor>
```

List response:

```json
{
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
    "service": "Game API",
    "severity": "error",
    "errorCode": "gacha_result_persist_failed",
    "messageSummary": "Gacha result persistence failed.",
    "requestId": "req_...",
    "accountId": "account_...",
    "runId": null,
    "rewardPreviewId": null,
    "rewardClaimId": null,
    "gachaBatchId": "gacha_batch_...",
    "gachaBannerId": "banner_...",
    "retryable": true,
    "masked": true,
    "payloadRef": null,
    "traceRef": null,
    "links": {
      "auditLogId": null,
      "rewardClaimId": null,
      "gachaBatchId": "gacha_batch_..."
    }
  },
  "detail": null
}
```

`detail` should remain `null` unless a later R2/trace detail source is approved
and permission-gated.

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

## First Implementation Slice Candidate

When implementation is approved:

1. Add migration for `ops_error_logs` D1 summary table only.
2. Add read-only Admin API list/detail contract with fixture-backed or D1-backed
   tests.
3. Add permission candidate `OPS_ERROR_LOG_READ`.
4. Add a local-only sanitized event insert helper for unit/integration tests.
5. Defer Queue/R2/Workers Logs bindings until explicit remote resource approval.

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

# Admin Session Metrics Contract Draft

This document defines the read-only Admin metrics contract for login, session,
concurrent-user, and last-login visibility.

Current local implementation composes account-level support metrics into
`GET /v1/admin/support/accounts/:accountId`. Dashboard aggregate/session-summary
routes, migrations, session heartbeats, Cloudflare resources, DEV deploys,
remote smoke tests, and PRD/STAGING resources are not implemented.

## Goals

- Give operations a quick view of account login/session health.
- Give support enough last-login context to answer account activity questions.
- Keep session/login metrics server-owned.
- Avoid exposing auth secrets, raw headers, refresh token hashes, or player
  tokens.
- Separate approximate launch metrics from future real-time concurrency.

## Non-goals

- No account mutation.
- No session revocation or repair.
- No login replay tooling.
- No user tracking beyond server-owned auth/session records.
- No client-supplied session/login data as Source of Truth.
- No production deployment or secret changes.

## Current Foundation

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

## Metric Definitions

### activeSessions

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

### concurrentUsers

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

### recentActiveAccounts5m / recentActiveAccounts30m

Recommended definition:

```text
count distinct account_id where last_server_activity_at >= now - window
```

Window candidates:

- 5 minutes: online/concurrency-like freshness.
- 30 minutes: short active-session cohort.

Launch note:

- Without a last activity source, do not backfill this from refresh token TTL.

### lastLoginAt

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

### lastLoginAgeSeconds

Definition:

```text
floor((server_now - lastLoginAt) / 1000)
```

Rules:

- Server computes it from server time.
- Admin may also display a relative label, but the Source of Truth timestamp is
  server-owned.
- Missing last login returns `null`, not `0`.

### lastLoginAgeBuckets

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

### dormantAccounts / returningAccounts

Draft definitions:

- `dormantAccounts`: accounts whose last login age is at least 30 days.
- `returningAccounts`: accounts with a new session in the selected window and
  previous last login at least 7 days earlier.

These require login history or prior-session comparison. If only current
`auth_sessions` rows are available, mark the metric as unavailable rather than
guessing.

## Recommended API Placement

### Dashboard Summary

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

### Account Detail / Support

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

## Future Storage Candidate

Only add storage if exact recent activity or login history becomes necessary.

### account_activity_summary

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

### account_activity_events

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

## Security And Masking

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

## Error Codes

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

## Admin Display Guidance

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

## Test Candidates

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

## Open Decisions

- Whether dashboard metrics should live under `/v1/admin/metrics/session-summary`
  or a broader `/v1/admin/dashboard/summary`.
- Whether VIEWER can see aggregate session metrics.
- Whether exact concurrency requires a heartbeat endpoint, request summary
  table, or Workers Analytics companion.
- Whether `lastLoginAt` should mean guest creation, refresh rotation, or only
  explicit login/reauth events.
- Whether dormant/returning buckets are needed for launch or later retention
  work.

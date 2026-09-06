# Error log category and tracing

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

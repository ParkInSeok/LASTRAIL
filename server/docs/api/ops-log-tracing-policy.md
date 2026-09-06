# Error log category and tracing

- Server: API, cron/worker, migration verification, support read model, integrity pipeline.
- Frontend: browser or Unity runtime failures. Pages Functions execute on the server.
- Current ops_error_logs schema only accepts server sources. Frontend collection is
  not connected; category=frontend returns no items and collectionAvailable=false.
  Do not classify a Game API error as frontend merely because a client triggered it.
- id is the immutable event tracking code. requestId correlates events within a
  request. Never manufacture a requestId for legacy events without one.
- service/source and environment identify the origin. Safe resource IDs and traceRef
  provide additional lookup references; a reference does not guarantee the resource exists.
- Producers must use stable errorCode values and sanitized messageSummary. Never put
  credentials, request bodies, session tokens or raw stack dumps in tracking IDs/messages.
- Server search uses bound parameters and literal substring matching (max 200 chars)
  across safe summaries and references, before pagination. Environment, service,
  severity, status and occurredFrom/occurredTo combine with AND.
- UI dates display KST; date filter inputs use browser-local time converted to ISO UTC.
- Tab/filter changes reset pagination and selected detail. Detail only renders safe
  reference fields; no resolve/acknowledge or other mutation actions are provided.

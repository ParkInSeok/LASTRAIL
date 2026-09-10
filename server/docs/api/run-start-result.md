# Read-only run-start result lookup

Use this when the journal has the original start key and request body, but the
start response was lost before receiving runId. This endpoint queries an existing
receipt; it never calls start, consumes a preview, issues rewards/refunds or changes
run state. No migration. Public combat readiness restrictions remain unchanged.

## Request

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

## Fixed response

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

## Isolation and atomic visibility

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

## Verification

Six signed-auth real SQLite tests cover response loss without client runId,
byte-identical database exports before/after lookup, account isolation/401/revoked
session, preview mismatch with no disclosure, not-found/concurrent commit, missing
or corrupt snapshots, URL/extra-field rejection and sanitized503 with no query log.
Historical committed starts are prepared through the retained domain-core test
fixture; no public ready bypass or new acquisition is enabled. Full-suite evidence
and exact same-code/document commit are in the owner handoff.

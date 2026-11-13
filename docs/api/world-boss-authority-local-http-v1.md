# Authenticated world-boss journal + health LOCAL HTTP v1

Contract `world-boss.authority-local-http.v1.20261002`. LOCAL_TEST_PROFILE / TRUSTED_LOCAL_TRIAL, phase1 TRAIN direct, one personal shield. Source contracts remain `world-boss.rail-action.v3.20261002`, `world-boss.rail-journal.v1.20261002`, `world-boss.rail-health.v1.20261002`. Standalone rail HTTP v1 and their original 33 pinned files are preserved.

## Environment and authority

Factory `createWorldBossAuthorityLocalHttp()` implements actual Hono Fetch handlers with existing signed access token/session/account authentication and actual SQLite/D1 repositories. It is not installed in the game worker. There is no listening TCP service, deployed URL, remote admission, or Unity Player transport claim. Test base URL is `http://127.0.0.1/_local/world-boss/attempts/trial/authority`; this is a request URL for in-process execution, not a running server address.

Require ENVIRONMENT=LOCAL and LOCAL_WORLD_RAIL_PROFILE=LOCAL_TEST_PROFILE. Only http localhost/127.0.0.1/[::1] request URLs are accepted. An Origin, if supplied, must exactly equal request origin; no cross-origin browser/CORS bridge is installed. No public credentials or browser game-token distribution is added. Actual Authorization Bearer is supplied privately by the test runner, never in exported fixtures. Existing ACTIVE DB session/account are checked at entrance and response; command batch also checks actual session/account status/expiry atomically. Train/account ownership and participant scope come from the immutable stored source definition. Operator may read; phase1 train route commands belong to train owner. Outsiders fail403. Session revoke/rotation/expiry fails401, account suspend403, including original receipt replay.

Install new LOCAL SQL `apps/game-api/local/world-boss-authority-http.sql` in a dedicated trial database already containing existing schema and rail/journal/health LOCAL SQL. No runtime bootstrap or operational migration is performed. Existing actual HP owner and explicitly marked original LOCAL health start must already be attached by an internal trial setup. Operational movement, actual attack/health chronology, effect/shield issuance and operating coefficients/target policy remain absent or unapproved.

## Requests

Both routes require these exact headers:

| Header | Value |
|---|---|
| X-World-Authority-Version | world-boss.authority-local-http.v1.20261002 |
| X-World-Rail-Version | world-boss.rail-action.v3.20261002 |
| X-World-Journal-Version | world-boss.rail-journal.v1.20261002 |
| X-World-Health-Version | world-boss.rail-health.v1.20261002 |
| X-World-Content-Version | stored source definition contentVersion (fixture LOCAL_GEO) |

GET `/authority` has no body/query and performs only authenticated repository reads. Response is `{transportVersion,status,inputProvenance,journal,health}`. journal is the existing validated original durable snapshot `{version,status,inputProvenance,ownerRevision,frame,rail,geometry,binding,entries}`. health is existing validated `{version,status,inputProvenance,runId,healthRevision,health,shieldRevision,shield,processingStatus,receipt}`. `processingStatus` is PENDING_SOURCE/PENDING_HEALTH/APPLIED. receipt is original processed world health result or null. It preserves physical ATTACK vs logical result time and source intent/result/entry revision. GET never ticks, advances geometry, applies damage or initializes HP. Source before/after equality rejects a concurrent change409; HP owner validates its own checkpoint. Distinct source/rail/health/shield revisions must not be interchanged.

POST `/authority/commands` requires application/json, Idempotency-Key=commandId (printable ASCII1..128), X-World-Journal-Revision=positive canonical safe integer original journal ownerRevision. Body is original strict railCommandSchema: `{version,attemptId,contentVersion,roundId,commandId,expectedRevision,action}`. `expectedRevision` is original rail revision, not journal revision. Attempt/content match route and header. Frame availability, sample, account, profile, damage, HP and health.apply are not client inputs. New command must use actual existing journal sample time equal to server Date.now; missing/stale actual producer fails503 rather than inventing movement/timing.

Response `{accepted:true,transportVersion,status:'LOCAL_TEST_PROFILE',requestJournalRevision,receipt}` retains original rail receipt. Transport metadata binds original key/body/owner header to immutable journal command entry, and is committed with journal/state/geometry under actual authorization/source version/hash/CAS guards in one batch. Metadata owns no domain state or HP. Exact replay validates current auth and original metadata/entry and returns original response before stale clock/owner checks. Same key changed body/header conflicts409. Old inherited or internally-created receipts with no original transport metadata fail409 (`authority_http_receipt_not_owned`) instead of inventing a header binding; original journal/read history remains readable. Failed metadata writes roll the entire command back, lost acknowledgement recovers original binding, new process/SQLite reopen returns original results. Transport metadata is SQL append-only.

No `/apply`, `/health/apply`, `/advance`, `/attach`, profile/upload or raw HP/damage/frame/sample endpoint is installed (404). `WorldBossRailHealthAdapter.apply` remains an internal trusted producer responsibility. Command never invokes it. A successful logical rail result can therefore remain PENDING_HEALTH until that producer commits.

## Errors and retry

All installed routes return Cache-Control:no-store and X-World-Authority-Transport. No cache/query pagination or retry timer is installed. 400: strict shape/body/query/key/revision formatting violation. 401: token/session invalid, revoked/expired. 403: inactive account, participant/role/origin forbidden. 404: disabled LOCAL boundary, unknown attempt/path. 409: exact version/content, rail/journal revision, changed key/header, role window/attempt state or composed-read conflict. 503: missing producer/sample clock/frame, journal/health/link/receipt corruption, unavailable DB, interrupted storage. Existing domain error codes are preserved; generic unclassified DB failures use authority_temporarily_unavailable. Invalid stored transport binding is unavailable, not successful replay. Never include token, secret or raw exception in response.

On a timeout/503 after a command, retry the exact original body/key/owner header while auth remains valid. On409, read current state and determine whether original result already exists; use a new command ID/current original rail and journal revisions for a new permitted action. Do not change an existing key's body/header. On401 reauthenticate through the existing supported private auth path; this adapter issues no token. On PENDING_HEALTH, read does not retry application; the internal producer owns it. The source intent remains pending if that processing transaction fails.

## Consumption boundary

Immediate narrow Unity non-View intake AND: this transport contract/source/credential-free fixtures pinned + Server actual authenticated in-process Hono request/SQL verification. Consume strict DTO/version/original source result+pending/applied semantics, original receipt replay and unsupported/error mappings through the existing nonvisual owner/controller boundary; do not duplicate adjudication or apply health in View. Operational producer, fullround/multilayer/TC02, listening socket/actual Player transport and final art are not prerequisites for that fixture/DTO intake, and are not represented as complete. Real deployed endpoint/Player communication needs a separate endpoint environment and producer contract. View still requires final approved art and a separately registered task. No A→B policy transition or operating damage number is approved here.
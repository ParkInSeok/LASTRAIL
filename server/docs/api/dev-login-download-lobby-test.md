# Opt-in real DEV login → download → lobby: server prerequisites

Updated 2026-09-07 KST. Status: **BLOCKED**, not a live test success.
Canonical contract: `docs/api/client-release-registry.md` in the planning checkout.
Server implementation: `C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/attendance-pass-20260907/server`.
Unity owns the opt-in suite and real HTTP/session adapter. This document supplies
its server prerequisite and assertion contract; it is not an executable test result.

## Latest narrow authorization update — 2026-09-07

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
registry deployment and real tester entitlement remain separate unsatisfied gates.

## Current evidence and stop conditions

| Prerequisite | Current observed state | Test decision |
| --- | --- | --- |
| Actual A05 public objects uploaded and HTTPS hashes verified | Upload previously rejected; Unity HTTPS report at11:28:09Z shows catalog404, no hash match | BLOCKED_ARTIFACT_PUBLICATION |
| versions/current.json activated by successful fresh-ETag CAS/readback | No activation performed by this server task; last observed legacy pointer had no contentRelease | BLOCKED_CONTENT_ACTIVATION |
| Registry endpoint implementation deployed | Local only; deployment and remote0024 outside this request | BLOCKED_REGISTRY_NOT_DEPLOYED for registry cases |
| Actual INTERNAL entitlement | Local synthetic fixtures only; no real grants | BLOCKED_TESTER_ENTITLEMENT for INTERNAL cases |
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

## Normal API sequence after prerequisites are satisfied

1. Use configured official DEV base `https://dev-api.lastrail.app` and actual installed
   App-Version/Version-Code/Platform/Channel headers. Do not spoof a higher code,
   use account-id authority, or disable admission. No PRD calls in this DEV suite.
2. GET `/v1/client-config` without credentials. Legacy/STABLE requires accepted=true,
   clientAdmission.status=ACCEPTED and contentRelease.status=APPROVED_COMPATIBLE.
   Registry INTERNAL may instead return accepted=false, AUTH_REQUIRED and
   bootstrap.canAuthenticate=true with every content identifier/URL/hash withheld.
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
   and expected releaseId/catalogHash; INTERNAL also requires server entitlement.
   No local fixture entitlement is evidence for this step. Keep the quoted
   registryRevision for revalidation and bind releaseId to this session.
7. Download only the authorized catalog/bundles. Do not forward API Bearer credentials
   to the public asset host. Verify approved catalog identity and required bundle
   completion. Preserve network consent and cancellation behavior; no Editor-local
   fallback can count as an actual remote download.
8. Revalidate the current authorized release after download; stop on revoked tester,
   disabled/update-required code, changed identity or session logout. Retain
   `X-LastRail-Content-Release-Id` for registry-protected gameplay requests. Enter Main
   and lobby only with the accepted session/profile/name/story and verified content.
   Record actual scene/lobby observations; a downloaded byte count alone is not lobby success.

The smoke does not claim login bonuses, attendance, pass rewards, crystals, purchases,
ticket exchanges or draws. The separately approved 11-ticket/1+10-draw budget remains
unchanged and is not seeded or consumed by a lobby test.

## Approval holds already recorded

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

## Verified local evidence, separate from remote

Server registry suite checkpoint:67 files /394 tests passed,4 TypeScript projects and
lint passed; actual local D1 migration0024, FK clean, quick_check OK, tester grants0.
Those results validate local contracts and guards only. This documentation checkpoint
adds no code changes and did not rerun that suite or claim remote advancement.

# Supported Subset Display Diagnostic

Date: 2026-09-07. Status: DIAGNOSTIC_SUPPORTED_SUBSET_NOT_SHIPPING.

## Provenance

`gacha-supported-subset-diagnostic.json` is an unchanged copy of SERVER_1's
`C:/Users/dlstj/.codex/worktrees/11ec/LASTRAIL/gacha-audit-20260907/server/audit-results/admin-projection-safe.json`.
SHA256: `fbdceb43b7650d50b8aa8ebb233b4b29b0dfd5785a1525715e43b9cf8d247ca3`.
Server reported generation through actual isolated Hono/SQLite Admin list/detail
routes. Admin inspected the source projection and consumed the captured response;
Admin did not independently replay the Server HTTP harness in this work unit.
Only synthetic local diagnostic accounts and safe references are included.

Game pull results are NOT the Admin projection. Admin resultId is a row identity,
reward.id is the candidate identity, and duplicateConversion has the persisted
token pattern. Do not feed Game results directly to the Admin detail mapper.

## Verified

- All fourteen distinct supported candidates map through the real Admin adapters.
- All sixteen list batch identities resolve to their captured detail response.
- Elite and rare duplicates retain original candidate identity and token x50/x30.
- Real GachaHistory component SSR with query cache populated by those adapters
  renders every candidate and both conversion strings, with no pending/loading.
- Unknown safe candidate/type/rarity/guarantee codes remain visible.
- Unsupported RARE_OPERATOR_TOKEN and TACTICAL_MATERIAL are not covered as eligible.
- No public name/icon catalog is available: current UI remains stable-ID display.

Initial diagnostic test run: 8 passed, 1 failed because the test assumed unknown
guarantee labels omit the existing raw-code suffix. Test expectation corrected to
check raw-code preservation; no product fix. Initial typecheck found implicit-any
in the new test; typed the mapped detail array. No production defect RED claimed.

Final `npm test -- --run`: 44 files / 246 tests passed.
`npm run build`: tsc and Vite passed, existing >500 kB warning remains.
Assets remain index-DLeeegRX.js and index-CX_sa0t9.css.

## Remaining Boundaries

This is not shipping pool approval, statistical proof or unsupported-type API PASS.
Name/icon verification requires an authoritative safe catalog. Initial/free five
eligibility is not inferred. The browser follow-up below supersedes the original
browser-pending checkpoint, but does not widen pool approval.
No probability logic, economy, remote pull, R2, Access, secret or deployment changed.
Unity-owned local-http-evidence.ts changes were preserved and not staged.

## Actual Live Hono And Browser Follow-up

Admin independently reran the existing Server focused test named
`reaches every currently supported draft candidate`: 1 passed, 9 skipped, 910ms.
SERVER_1 then reran that generation with GACHA_AUDIT_READ_BRIDGE=1, retaining
the same in-memory DB after Game pulls, exposing actual Admin Hono GET handlers
on loopback53702. No captured JSON replay, database rehydration or raw DB export.

Admin `local-gacha-read-bridge.mjs` served the existing built UI at loopback4178
and forwarded only allowed gacha GETs, with no incoming auth/cookies/Origin/Host.
Its bootstrap is synthetic LOCAL diagnostic shell, not an auth readiness test.
Run via `node scripts/local-gacha-read-bridge.mjs http://127.0.0.1:<server-port>`
after SERVER_1 provides the current ephemeral bridge URL. Stop with Ctrl+C.

Observed on 2026-09-07 around15:52 KST using IAB browser:
- Actual list200 and all16 detail200, fourteen distinct candidate IDs visible.
- Rare op_signal_runner token30 and elite op_vex token50 visible separately.
- Expanded rare detail: duplicate count1, replay known, guarantee NONE, no
  inferred cause. Screenshot inspected; no name/icon assets invented.
- Unknown account filter returned200/empty and displayed `뽑기 이력 없음`, not pending.
- Independent local HTTP assertions repeated16details/14IDs/30+50; missing
  batch404, proxy POST405, unrelated API404. POST was not forwarded upstream.
- Unknown status/type raw fallback remains adapter-test evidence only, not a
  newly seeded live DB case. No unsupported candidate was added to the DB.

Initial static path403 was caused by this new local host's double path separator;
normalized dist with resolve, then HTTP200 and IAB worked. Chrome initial navigation
reported ERR_BLOCKED_BY_CLIENT; no extension/security changes and no Chrome retry
success claimed. The failed Chrome and IAB tabs plus successful IAB tab were closed.
Admin proxy stopped; SERVER_1 was asked to stop its bridge. Full suite/build were
not repeated in this follow-up: only local script/evidence changed, product unchanged.

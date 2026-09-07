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

This is not shipping pool approval, statistical proof, unsupported-type API PASS,
an independent browser check, or a new remote deployment. Name/icon verification
requires an authoritative safe catalog. Actual browser/Hono re-execution remains
separate work; SSR does not replace it. Initial/free five eligibility is not inferred.
No probability logic, economy, remote pull, R2, Access, secret or deployment changed.
Unity-owned local-http-evidence.ts changes were preserved and not staged.

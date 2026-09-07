# Attendance and pass local implementation plan

Goal: implement authenticated, server-configured attendance and pass foundations without inventing launch economics or enabling a store.

Authorization: user start approval relayed by Plan on 2026-09-07; Plan explicitly confirmed fail-closed local foundations after no policy source was found. No remote migration, seed, deployment, purchase or secret configuration.

Architecture: separate policy validation, transactional service and Hono registration. Immutable DB definitions contain explicit campaign windows, reset offset, rewards, thresholds and run XP rules. There are no default definitions or launch seeds. Existing account mutations serialize economic writes; unique business receipts and request fingerprints prevent repeated grants and changed-body replay. Claim batches include state, inventory, economy/item ledgers and reward receipts. Existing login bonus remains separate.

Unresolved launch decisions: attendance reset offset, campaign dates, cumulative counting model and milestones, daily/milestone rewards, pass dates and claim grace period, XP thresholds and eligible server events/amounts, free/premium rewards, store/provider/app/product identifiers, purchase ownership verification and revocation integration. Recommendations are not approved launch policy. Test fixtures supply all values explicitly.

Only the explicitly selected DISTINCT_DAYS attendance model is supported initially; unknown models fail validation. A pass may explicitly configure XP for server-accepted run outcomes; client XP amounts are never accepted. No solo/co-op condition. Purchase verification is an injected server adapter with no default implementation; claims accept only persisted, time-valid, unrevoked verified entitlements. Actual Google/Apple verification, refunds and production notifications require later provider policy/integration. No client entitlement flag.

- [x] RED: real migrated SQL.js DB/API tests for missing configuration, auth, exact reset/end boundaries, cumulative gaps, duplicate/concurrent requests, rollback, changed idempotency payload, XP event ownership/replay, free/premium gating.
- [x] GREEN: add 0022 migration with immutable definition tables, attendance visits, activity claim receipts, pass XP events and verified entitlements.
- [x] GREEN: implement `server/apps/game-api/src/activity-policy.ts`, `activity-service.ts`, `activity-routes.ts`; register routes after existing admission middleware using existing auth resolver.
- [x] Verify focused tests, existing integration suite, four TypeScript projects and lint; adjust migration-count assertions to 22 only.
- [x] Review independent bounded changes and document local-only API/config contracts and remaining policy decisions. Update canonical SERVER_1 handoff before completion.

Isolation: `attendance-pass-20260907`, branch `codex/attendance-pass-20260907`, base `65b0263524bfd21ed42e597f1205cc81723fd5b9`. Existing frozen release and runtime source are preserved. Uses existing frozen dependencies through a local node_modules junction; no dependency updates.

Final local evidence: 63 files / 376 tests PASS; four TypeScript projects and lint PASS; actual Wrangler local D1 0001-0022 PASS, FK clean, quick_check ok, definitions0. Independent review findings fixed and rechecked. See server/docs/api/attendance-pass.md for confirmed04KST and recovery/currency steering and launch limits. Source remains local/uncommitted.

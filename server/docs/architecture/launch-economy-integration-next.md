# Launch economy: configuration versus integration

Follow-up implemented locally on2026-09-09: see
[growth and launch implementation](growth-launch-implementation-2026-09-09.md).
The status table below describes the earlier config-only checkpoint and is historical.

2026-09-09 first unit: local versioned snapshot, strict parser and quote accessors.
Planning snapshot SHA256: C17BA2EB831AFB9C0C5F6EB55DBE13EC06D24A3A89C78708B409974EF065169A.
The source is canonical docs/ops/launch-economy-v1.json. Plan owns the numbers.

| Layer | Status |
| --- | --- |
| Exact snapshot and strict schema | Implemented locally |
| Risk-only victory EXP / exact Training Core quote | Implemented locally |
| Persisted immutable version activation and request version locking | Not connected; parser only verifies caller's expected version |
| Actual reward routing to new offline/run values | Not connected |
| Growth debit/refund/level mutation API | Not implemented |
| Equipment/research target activation | Blocked on authoritative catalogs |
| DEV/PRD deployment or DB seed | Not performed |

## Next independently testable units

1. Version storage/activation contract: bind immutable economy version plus content
   identity to the existing balance version model. A version string check alone
   does not prevent changing a payload under the same version. Store immutable
   checksum and source identity; mutation handlers read the pinned version, never
   trust client cost or a client-provided expected version as authority.
2. Reward integration: adapt offline and ordinary/story-raid paths with distinct
   materials and EXP formulas. Preserve outcome contracts; ordinary failure EXP is
   floor(riskEXP * 8000 * clearedWaves / 10000 / 15); no operation/risk multiplier
   on EXP. Story raid amounts are final. Preserve run-start balance locking and
   replays. Validate old and new pinned versions separately before activation.
3. Training Core level-up/sync-up/replace: existing operator_training_core_slots
   carries invested_operator_exp. Use current account ownership and 0–4 slot IDs;
   quote pinned costs, conditionally debit all resources and update progression
   and invested EXP in one account-sequenced transaction. Replace refunds stored
   actual investment100%, not a recalculated curve; no automatic reinvestment.
4. Train/skill/promotion: reuse authoritative ownership IDs and existing limits.
   account_train currently has no level column, so train level persistence requires
   an explicit additive migration; do not infer or rewrite existing levels.
   Skill/promotion columns exist. All spending requires server cost selection,
   progression and currency ledger linkage plus an immutable replay receipt.
5. Equipment/research: schema profiles are available, but they cannot authorize
   targets. Before API/migration design, obtain approved target catalog, effects,
   prerequisites and level caps. Do not create generic client-selected purchases.
6. First-clear budget and source replacement audit: current Game index.ts:6253
   queries account_first_clears by account_id+operation_key+risk, and :6406 inserts
   that same key. Audit material/core/ticket sources and per-risk upper bounds
   separately before converting rewards. Twelve launch tickets →1200FREEcrystals
   must replace the same unclaimed sources only; no extra grant to prior claimants.
   Missing wallet policy must fail closed. No replacement is part of this unit.

Every mutation unit needs replay-before-stale-version validation, same-key altered
payload rejection, insufficient-funds rollback, concurrent-spend safety, unknown
target no-spend, version mismatch, and actual-investment refund tests against the
local DB. Configuration unit tests do not claim these guarantees. Existing wallet
balances, investments and already-claimed source records remain unchanged.

Verification: initial targeted test run failed because module was absent; after
implementation10 tests pass. Full local suite68files/411tests passes. Game API
TypeScript and changed-file ESLint pass. Existing installed Vitest4.1.11 was invoked
by absolute CLI path because pnpm exec could not locate its binary in this worktree;
offline install stopped before modifying dependencies. No remote action occurred.

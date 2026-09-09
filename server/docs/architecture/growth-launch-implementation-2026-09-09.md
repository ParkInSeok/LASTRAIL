# Local growth and launch entitlement implementation

User-approved local code/SQLite integration scope. Plan baseline remains
`act1.economy.2026-09-09.001`; no numeric redesign, remote query/deployment/seed,
Store, ticket-inventory conversion or historical payout adjustment.

## Actual implementation status

| Component | Local implementation | Remote activation |
| --- | --- | --- |
| Immutable economy version/hash and balance-version binding | O, migration0025; empty registry | X |
| Training Core level-up/sync-up/replace | O, actual debit/refund and DB receipts | X |
| Owned train/skill/promotion | O, server costs and atomic resource/progression ledgers | X |
| Equipment/research cost profiles | O | X |
| Equipment/research target purchases | X, approved catalogs absent; reject without spend | X |
| Launch source dedup and prospective FREE crystal replacement | O, migrations0026–0027 | X |
| New offline/ordinary repeat reward-table adapters | X, separate follow-up | X |

Growth service commit: f20d63d6cdd2ace132869086ecf9d705b3edecc9. HTTP registration
and launch integration are the subsequent commit containing this report.

## Growth contract

`GET /v1/growth/config` returns bound version/hash and costs, equipment/research
disabled flags. Mutations require authentication, `Idempotency-Key`, and JSON
`economyVersion`; server ignores no cost inputs because unknown fields reject.

- `/v1/operators/training-core/:slot/level-up`: `targetLevel`, slot0–4.
- `/v1/operators/training-core/sync-up`: spends available EXP on the lowest-level
  slot, ties by slot index; stops at insufficient EXP or all capped. Requires5slots.
- `/v1/operators/training-core/:slot/replace`: `operatorId`, actually owned and
  not already slotted. Can fill an empty slot. Refunds stored invested EXP100%,
  resets new slot to1/0; no automatic reinvestment.
- `/v1/trains/:targetId/level-up`, `/v1/operators/:targetId/skill/level-up`,
  `/v1/operators/:targetId/promotion`: one server-selected step on owned target.
- Equipment/research level-up routes return `unknown_growth_target`; profiles
  alone do not authorize a target or synthesize its effects.

All mutations reject ACTIVE/FINISH_PENDING/QUARANTINED runs, including a run
created between planning and writing. A unique account mutation sequence fences
state reads; an in-batch assertion checks active balance version/open runs. Receipt,
all resource deltas and levels commit together. Success replay precedes current
version/level validation; same key with altered action rejects. A second receipt
read handles concurrent commits during validation. Existing stored EXP investments
and high levels are preserved; only an explicit replacement refunds investment.

Migration0025 adds train `level DEFAULT1` because prior schema has no train level.
It does not infer progression from spending history. The immutable registry protects
UPDATE, DELETE and INSERT OR REPLACE. Binding a version already referenced by runs
is prohibited so an old pinned run cannot be reinterpreted as new economy content.
Hash is checked when read. No endpoint publishes/activates arbitrary client config.

## Launch source budget and compatibility

| Stable family | Ticket-equivalent cap | New unclaimed grant | Special cores |
| --- | ---: | ---: | ---: |
| launch_login_bonus_act1_v1 | 5 | 500 FREE crystals | 0 |
| opn_03_water_stop | 1 | 100 FREE crystals | 0 |
| opn_05_night_boarders | 1 | 100 FREE crystals | 0 |
| opn_06_broken_line | 2 | 200 FREE crystals | 1 |
| raid_raider_king_engine | 3 | 300 FREE crystals | 2 |
| Total | 12 | 1200 FREE crystals | 3 |

Each account/source-family/reward has one receipt, independent of risk, version,
run ID or retry. Existing claimed `reward_transactions.reward_json.rewardLines`
joined to first-clear rows consume the source across any risk; prior login claim
records consume login. Finish-only/unclaimed records are not treated as paid.
No backfill is awarded or deducted. Already-held tickets remain unchanged.

Unbound legacy pinned runs retain tickets; bound versions convert only their
unclaimed launch ticket source. Core entitlement remains independent. A race between
an old ticket preview and a new crystal preview can grant the same source only once.
Normal material first clears remain account+operation+risk. Same-currency base and
first-clear lines accumulate correctly instead of the latter overwriting the former.

Preview and claim both filter consumed sources; claim stores its effective plan,
so subsequent preview reflects the actual grant. Existing duplicate claim response
keeps its empty-new-rewards convention. Replay precedes wallet-policy validation.
Wallet policy is captured for conversion and enforced during planning and batch;
missing policy returns503 and policy races409 without grants. Login rejects bodies,
including body content without Content-Length. Claim checks owner and changed target.

Migration0026 rebuilds crystal lots/ledger to add FREE `LAUNCH_ENTITLEMENT` provenance,
copies every row, restores indexes/order guard, and passes local FK checks.0027 adds
finite source receipts. These are local migrations only; no existing remote table
was rebuilt and no historical account was inspected remotely.

## Verification

- Growth SQLite/HTTP integration13tests: exact five-slot70000EXP, historical777EXP
  refund, owned target changes, multi-resource insufficiency, concurrent duplicate
  and competing debit, invalid target, incomplete slots, active-run and version races,
  injected last-write failure rollback, immutable hashes including REPLACE regression.
- Launch SQLite/API integration17tests: finite12/core3 budget, partial historical
  sources, legacy/new pinned races, ordinary material preservation, same-key different
  run rejection, independent source concurrency, preview consistency, policy races,
  failclosed, injected rollback, schema row preservation and body contract.
- Final whole suite70files441tests PASS; all4TypeScript projects and whole ESLint PASS.
- Independent review found and verified fixes for SQLite REPLACE bypass, stale wallet
  sequence capture, second-replay target race and double-read policy mismatch.

Tests use actual local SQLite via SQL.js/D1 atomic batch adapter with concurrent
request scheduling and injected interleavings. They do not assert remote D1 execution,
remote activation, client UI integration or catalog completeness.

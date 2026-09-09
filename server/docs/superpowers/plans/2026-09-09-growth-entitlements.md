# Local growth and launch source integration

Approved scope: actual local mutation APIs and DB integration tests. No remote
deployment/seed, existing-account payouts/repricing, Store, or invented catalogs.

1. Add immutable economy version/hash bindings to existing balance versions.
2. TDD Training Core/train/skill/promotion service and HTTP contract using current
   account ownership, sequence-fenced atomic D1 batches, active-run guard, stored
   actual EXP refunds and replay receipts. Add train level migration without
   recalculating historical levels. Unknown research/equipment targets reject.
3. Independently implement stable launch source entitlements and prospective
   unclaimed-source ticket replacement; preserve legacy pinned reward types.
4. Test local SQLite: immutable hashes, version races, same-key replay/conflict,
   cross-spend concurrency, insufficient balances, injected rollback, unknown
   targets, open runs, historical investments and reward records.
5. Commit isolated steps, run full regression/typecheck/lint and independent review.
   Update canonical role handoff with actual outcomes and remaining catalog gaps.

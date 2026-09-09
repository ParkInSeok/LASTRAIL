# Launch economy configuration first unit

Scope: implement Plan's canonical launch-economy-v1.json as a versioned, strictly
validated local configuration. No automatic activation, balance seed, deployment,
existing-balance rewrite, or invented equipment/research catalog.

1. Preserve the exact planning snapshot in packages/balance/src/launch-economy-v1.json.
2. Write failing unit tests for strict shape, safe integers, dimensions, version
   locking, independent risk EXP and the exact 49-step training curve.
3. Implement a separate launch-economy.ts schema and read-only accessors; export
   through balance index. Do not change existing live reward routing in this unit.
4. Run targeted Vitest, TypeScript and lint; review diff and commit only own files.
5. Record exact tests, snapshot hash and remaining API work in role handoff.

Follow-on API design requires authoritative operator/equipment/research targets,
version-pinned cost snapshots, account mutation sequencing, atomic conditional
debit plus progression/receipt ledgers, replay-first idempotency and actual-invested
EXP refunds. Existing architecture is the starting contract. Equipment/research
activation remains blocked without approved catalogs. Plan is still writing the
consumption linkage and final budget verification; do not invent these contracts.
The later mutation unit must exercise insufficient funds, concurrency, unknown
targets, stale versions and replay. Pure configuration tests cannot prove DB atomicity.

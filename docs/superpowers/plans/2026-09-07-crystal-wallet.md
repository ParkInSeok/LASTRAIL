# Crystal wallet local implementation

Authorized scope: implement the accepted 2026-09-07 crystal-source-policy locally.
One displayed currency, separate FREE/PAID balances, free-first spending, store-order
provenance, FREE attendance/both-pass-track grants, premium-pass order linkage,
atomic ticket exchange/recovery, verified refunds once, no automatic punitive action.
Offline/tutorial grants remain excluded. No final currency ID, exchange price,
recovery cost, season table or SKU price is inferred. No remote mutation/deployment.

Architecture: immutable server wallet policy plus active pointer, order-linked grant
lots and per-lot ledger allocations, immutable request receipts and refund summaries.
Account mutation sequence serializes wallet writes with existing economy writes.
Activity claims compose wallet writes into their existing D1 batch. Public APIs
read balance, exchange configured tickets and verify an explicitly configured store
purchase via a trusted injected adapter with no production default. Refund
reconciliation is a server-only verified adapter boundary; no public refund trigger.

- [x] RED/GREEN wallet migration and real SQL.js/API tests for missing config, FREE/PAID
  separation, free-first spend, insufficient/concurrent funds, replay and rollback.
- [x] Connect CRYSTAL activity reward/recovery variants; premium rewards record verified
  order; attendance/free-pass cannot fabricate PAID grants. Legacy ITEM tests remain.
- [x] Trusted store purchase/order provenance and verified one-time reconciliation;
  reclaim only unspent lots tied to that order, record spent deficits and downstream
  ticket references, no unrelated currency/ticket/operator confiscation.
- [x] Document actual DTOs and fail-closed semantics for Unity. Preserve prior login
  ticket receipt keys; numeric ticket-to-crystal reward promotion remains pending.
- [x] Independent review, focused/full tests, four TS checks/lint, local D1 migration,
  canonical handoff. No A05 upload workaround while automatic approval is blocked.

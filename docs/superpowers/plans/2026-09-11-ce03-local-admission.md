# CE03 local admission and support selection implementation plan

> **For agentic workers:** Use superpowers:subagent-driven-development for the independent support storage unit and execute the host unit locally with TDD.

**Goal:** Implement disposable authenticated local admission with original snapshot/seed replay and explicit ordered support selection.

**Architecture:** Keep public start closed. A separately composed Hono host reads real owned selection, requires trusted executed consumer attestation, and uses the existing departure transaction with additional atomic selection/receipt guards. Store local support selection separately with owner/CAS/immutable receipts; do not change release unlocks or invent missing effects.

**Tech Stack:** TypeScript, Hono, Zod, D1 SQL batch, sql.js disposable tests, Vitest.

**Spec:** C:/Users/dlstj/OneDrive/문서/ChatGPT/LASTRAIL/docs/ops/combat-hud-completion-assignment-20260911.md and docs/design/combat-command-loadout-and-leader-presentation-20260911.md in that canonical project.

## Constraints

- Existing role assignment and local implementation explicitly authorized; no new approval loop.
- No remote database, deployment, real account or public readiness change.
- Actual consumer provider/artifacts required; synthetic acceptance is test evidence only.
- Preserve original request key and accepted response/seed on retry; atomically debit/save or roll back.
- Support exactly3 distinct ordered IDs from trusted10-entry local catalog; no implicit migrated selection or effects from caller.
- Preserve historical r4 profile identity. New support/leader semantics require a separately versioned contract and evidence binding.
- Leader4% is explicit opt-in local QA only; no production adoption claim. Undefined effects remain unavailable.

## Units

- [x] Support persistence: new ce03-support-loadout.ts and integration tests, exported local DDL/read/save/guard. RED absent behavior then CAS/owner/order/replay/race/rollback verification. Independent worker owns only these files.
- [x] Local host: new ce03-local-host.ts and integration tests; add narrow additional atomic statements hook to departure.ts. Authenticate and isolate all routes; start replay lookup before current provider/owner revisions; immutable local receipt tied to request hash.
- [x] Frozen contract: add versioned support/leader envelope and strict evidence binding while preserving historical r4; resolve actual Unity IDs/config through direct coordination. Missing effect or actual evidence rejects.
- [x] Wire prepare/support/recovery; exercise seed generation only for candidate accepted transaction and original stored seed replay, rollback/lost-response/concurrency/foreign owner/public route isolation.
- [ ] Run focused and full regression, types/lint, independent bounded review; export reviewable local request/response evidence without claiming real Unity conformance. Commit and report actual results/limits to Unity/Plan, handoff readback before/after sends.

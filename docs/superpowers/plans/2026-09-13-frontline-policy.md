# CE03 Frontline Policy Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task. The existing isolated server worktree is retained.

**Goal:** Explicitly prepare new frontline-first profiles while preserving default legacy bytes and accepted runs.

**Architecture:** Keep command contract v1. Pin the new targeting identity inside the hashed draft. A trusted producer/host construction option selects it; request bodies cannot change policy. Unity owns actual targeting, checkpoint identity and legacy restoration.

**Tech Stack:** TypeScript, Zod, Hono, Vitest, disposable SQL.js fixtures.

**Spec:** Approved Unity/Plan_4 coordination and docs/handoffs/LAST_RAIL_SERVER_1.md factory-boundary analysis; new policy semantics documented in server/docs/api/ce03-frontline-first-v1.md.

## Global Constraints

- ID ce03.frontline-first-v1; execution version ce03.execution.2026-09-13.001; policy hash 08cd3350fde47c5d819836bf6c6be981467e01ea5f814ee9cd56a42e17d0f052.
- Default old P2/8914 policy and absence of targetingPolicyId remain unchanged.
- No remote deployment, actual admission, START, finish or claim. New fixtures in a new audit directory.
- Preserve logical front [3,4], taunt precedence, cursor, skills, reserved impacts and other policy stages.

## Task 1: Versioned producer and schema

Files: server/apps/game-api/src/ce03-targeting-policy.ts (identity/spec); ce03-local-schema.ts (closed new field and cross-identity checks); ce03-local-producer.ts (optional selection, source pin); tests/integration/ce03-frontline-policy.test.ts.

- [x] Write tests for legacy bytes, new canonical identity, otherwise unchanged content, unsupported options and mixed identities; run red.
- [x] Add minimum implementation, keeping omitted option and old schema fields byte identical.
- [x] Run focused tests and review rejection boundaries.

## Task 2: Normal prepare route and delivery

Files: server/apps/game-api/src/ce03-local-host.ts (captured construction option); producer registration; tests/integration/ce03-frontline-policy.test.ts; scripts/validation/verify-ce03-frontline-policy.ts; docs/api/ce03-frontline-first-v1.md.

- [x] Test configured host prepare hash, v1 commands, no DB mutations, frozen option and request-body rejection; run red before implementation.
- [x] Implement both prepare entry points and fixture generator using a new disposable DB and exclusive new output folder.
- [x] Run focused producer/host/schema/attestation tests, typecheck and scoped lint.
- [x] Generate separate new prepared fixture, preserve old file hash, report exact identities and limits to Unity/Plan_4 after handoff readback.

Validation: 37 focused tests passed; game-api TypeScript and scoped ESLint passed. Independent read-only reviewer found no actionable defect; suggested cross-profile identity regression added and passed. New fixture ce03-frontline-policy-UtU7yJ read back, original draft raw hash preserved. Internal reporting follows the commit.

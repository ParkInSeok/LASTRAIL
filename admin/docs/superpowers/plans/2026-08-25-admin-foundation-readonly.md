# Admin Foundation Readonly Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first read-only LAST RAIL Admin Console vertical slice.

**Architecture:** Create a TypeScript npm workspace with shared Zod contracts, a Fastify local backend, and a React/Vite admin web app. Browser code talks only to the local backend; remote server access and secrets stay in the backend.

**Tech Stack:** TypeScript, React, Vite, React Router, TanStack Query, Fastify, Zod, Pino, Vitest.

**Spec:** `docs/superpowers/specs/2026-08-25-admin-foundation-readonly-design.md`

## Global Constraints

- Browser must not expose Cloudflare API token, Cloudflare Access secret, production admin secret, DB credentials, or signing secrets.
- Local Admin Backend binds to `127.0.0.1` by default.
- Write/Ban/Balance Publish features are out of scope for this batch.
- Production UI must not rely on fake fallback data.
- Operator progression must use Training Core / Sync structure only.
- Permission checks must use a permission registry, not direct role string checks.

---

### Task 1: Workspace And Contract Package

**Files:**
- Create: `package.json`
- Create: `lastrail-admin-console/package.json`
- Create: `lastrail-admin-console/tsconfig.base.json`
- Create: `lastrail-admin-console/packages/admin-contracts/package.json`
- Create: `lastrail-admin-console/packages/admin-contracts/tsconfig.json`
- Create: `lastrail-admin-console/packages/admin-contracts/vitest.config.ts`
- Create: `lastrail-admin-console/packages/admin-contracts/src/index.ts`
- Create: `lastrail-admin-console/packages/admin-contracts/src/permissions.ts`
- Test: `lastrail-admin-console/packages/admin-contracts/src/contracts.test.ts`

**Interfaces:**
- Produces: `BootstrapResponseSchema`, `PlayerSearchResponseSchema`, `PlayerDetailSchema`, `RunSearchResponseSchema`, `RunDetailSchema`, `rolePermissions`, `hasPermission`.

- [ ] Write failing contract tests for bootstrap permissions and Training Core / Sync progression fields.
- [ ] Run `npm test --workspace @lastrail/admin-contracts` and verify tests fail because implementation is missing.
- [ ] Implement schemas and permission registry.
- [ ] Run `npm test --workspace @lastrail/admin-contracts` and verify tests pass.

### Task 2: Local Backend Readonly Proxy

**Files:**
- Create: `lastrail-admin-console/apps/admin-backend/package.json`
- Create: `lastrail-admin-console/apps/admin-backend/tsconfig.json`
- Create: `lastrail-admin-console/apps/admin-backend/vitest.config.ts`
- Create: `lastrail-admin-console/apps/admin-backend/src/config.ts`
- Create: `lastrail-admin-console/apps/admin-backend/src/upstream.ts`
- Create: `lastrail-admin-console/apps/admin-backend/src/server.ts`
- Create: `lastrail-admin-console/apps/admin-backend/src/index.ts`
- Test: `lastrail-admin-console/apps/admin-backend/src/server.test.ts`

**Interfaces:**
- Consumes: shared contract schemas from `@lastrail/admin-contracts`.
- Produces: `buildServer(options)` and local `/api/*` routes.

- [ ] Write failing route tests for `/api/bootstrap` and `/api/players`.
- [ ] Run `npm test --workspace @lastrail/admin-backend` and verify tests fail.
- [ ] Implement config loading, request ID propagation, response validation, and normalized errors.
- [ ] Run `npm test --workspace @lastrail/admin-backend` and verify tests pass.

### Task 3: Admin Web Shell And Readonly Pages

**Files:**
- Create: `lastrail-admin-console/apps/admin-web/package.json`
- Create: `lastrail-admin-console/apps/admin-web/tsconfig.json`
- Create: `lastrail-admin-console/apps/admin-web/vite.config.ts`
- Create: `lastrail-admin-console/apps/admin-web/index.html`
- Create: `lastrail-admin-console/apps/admin-web/src/main.tsx`
- Create: `lastrail-admin-console/apps/admin-web/src/app/App.tsx`
- Create: `lastrail-admin-console/apps/admin-web/src/app/routes.tsx`
- Create: `lastrail-admin-console/apps/admin-web/src/shared/api/client.ts`
- Create: `lastrail-admin-console/apps/admin-web/src/shared/auth/AdminContext.tsx`
- Create: `lastrail-admin-console/apps/admin-web/src/shared/navigation/navigation.ts`
- Create: `lastrail-admin-console/apps/admin-web/src/pages/*`
- Test: `lastrail-admin-console/apps/admin-web/src/shared/navigation/navigation.test.ts`

**Interfaces:**
- Consumes: backend `/api/*` endpoints and shared contracts.
- Produces: Vite app rendering read-only admin pages.

- [ ] Write failing tests for permission-filtered navigation.
- [ ] Run `npm test --workspace @lastrail/admin-web` and verify tests fail.
- [ ] Implement app shell, API client, bootstrap context, and read-only pages.
- [ ] Run `npm test --workspace @lastrail/admin-web` and verify tests pass.

### Task 4: Profiles And Verification

**Files:**
- Create: `lastrail-admin-console/start-admin-staging.cmd`
- Create: `lastrail-admin-console/start-admin-production.cmd`
- Create: `lastrail-admin-console/README.md`

**Interfaces:**
- Consumes: `ADMIN_API_BASE_URL`, Cloudflare Access credentials, admin bearer token.
- Produces: separate staging/production launchers.

- [ ] Add launchers that set `LASTRAIL_ADMIN_PROFILE` and run the local backend/web dev profile.
- [ ] Document required environment variables without committing secrets.
- [ ] Run `npm run typecheck --workspace @lastrail/admin-contracts`.
- [ ] Run `npm run typecheck --workspace @lastrail/admin-backend`.
- [ ] Run `npm run typecheck --workspace @lastrail/admin-web`.
- [ ] Run `npm test --workspaces`.
- [ ] Run `npm run build --workspace @lastrail/admin-web`.


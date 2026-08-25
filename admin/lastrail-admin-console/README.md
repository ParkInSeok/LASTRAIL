# LAST RAIL Admin Console

Internal read-only admin console foundation.

## Workspaces

- `apps/admin-web`: React/Vite browser admin UI.
- `apps/admin-backend`: Fastify local backend bound to `127.0.0.1`.
- `packages/admin-contracts`: shared Zod schemas and types.

## Required Environment

Set these outside source control before launching the backend:

- `ADMIN_API_BASE_URL`
- `CF_ACCESS_CLIENT_ID`
- `CF_ACCESS_CLIENT_SECRET`
- `ADMIN_API_TOKEN`
- `ADMIN_LOCAL_ACCESS_SUBJECT` for local `ADMIN_AUTH_MODE=LOCAL_HEADER` development only

The browser must only call the local backend under `/api`. Secrets stay in the backend process.

The local backend maps browser routes to the versioned Admin API:

- `/api/bootstrap` -> `/v1/admin/me`
- `/api/players` -> `/v1/admin/players`
- `/api/players/:accountId` -> `/v1/admin/players/:accountId`
- `/api/runs` -> `/v1/admin/runs`
- `/api/runs/:runId` -> `/v1/admin/runs/:runId`

## Commands

```cmd
npm install
npm test
npm run typecheck
npm run build:web
```

Staging and production use separate launchers:

```cmd
lastrail-admin-console\start-admin-staging.cmd
lastrail-admin-console\start-admin-production.cmd
```

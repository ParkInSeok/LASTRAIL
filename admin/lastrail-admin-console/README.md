# LAST RAIL Admin Console

Internal Admin console. Operational reads are the default; the explicitly
authorized app-version policy export is a separate permission-gated write.

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

The browser calls `/api` only. Local development uses the local backend; deployed
Pages uses Pages Functions. Secrets never belong in browser source.

The local backend maps browser routes to the versioned Admin API:

- `/api/bootstrap` -> `/v1/admin/me`
- `/api/players` -> `/v1/admin/players`
- `/api/players/:accountId` -> `/v1/admin/players/:accountId`
- `/api/runs` -> `/v1/admin/runs`
- `/api/runs/:runId` -> `/v1/admin/runs/:runId`

## Commands

```cmd
cd packages/admin-contracts
npm ci
npm test
npm run typecheck
cd ../../apps/admin-web
npm ci
npm test
npm run typecheck
npm run build
```

Run these commands from `lastrail-admin-console` in the order shown. The root
package has no test/build scripts. Lockfiles pin the installed graph; do not use
an unrelated parent node_modules as build evidence. Verified runtime: Node
22.22.3 / npm 10.9.8. `scripts/local-http-evidence.md` describes optional local
Hono/sql.js browser checks; these also require the Server toolchain.

The 2026-08-25 foundation specification is historical design, not a current
claim that the repository is empty or that remote Pages is unsupported.
Current architecture uses Pages Functions plus the Server Admin API. Read the
canonical Admin handoff before selecting a deploy snapshot or assuming approvals.

## Cloudflare Pages

`apps/admin-web` deploys the React console to Cloudflare Pages. Static assets are built to `dist`, and Pages Functions proxy browser `/api/*` requests to the versioned Admin API under `/v1/admin/*`.

Protect the Pages preview hostname with Cloudflare Access before using the remote Admin API. The Pages Function forwards the authenticated Pages user JWT to Admin API as `X-Lastrail-User-Access-Jwt`, while its service token only proves the proxy is allowed through the Admin API Access application.

Required Pages environment variables:

- `ADMIN_API_BASE_URL`
- `ADMIN_PROFILE`
- `CF_ACCESS_CLIENT_ID`
- `CF_ACCESS_CLIENT_SECRET`
- `ADMIN_API_TOKEN` if the Admin API environment requires it

Deploy commands from `apps/admin-web`:

```cmd
npm run deploy:preview
npm run deploy:prd
```

Local DEV and PRD-mode launchers:

```cmd
lastrail-admin-console\start-admin-dev.cmd
lastrail-admin-console\start-admin-production.cmd
```

PRD is kept as a code/config axis only. Do not run PRD deploys, set production secrets, connect the official domain, or finalize Access without an explicit release instruction.

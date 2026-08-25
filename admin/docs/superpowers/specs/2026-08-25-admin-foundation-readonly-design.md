# Admin Foundation Readonly Design

## Goal

Build the first LAST RAIL internal Admin Console slice: local execution, bootstrap identity and permissions, admin shell, players search/detail, progression inspector, runs list/detail, all read-only and backed by explicit Admin API contracts.

## Current Repository State

The repository is empty except for Git metadata. There is no frontend, local backend, server Admin API code, authentication code, database schema, route table, or test suite in this workspace.

## Architecture

The project is a TypeScript npm workspace under `lastrail-admin-console`.

- `apps/admin-web`: React, Vite, React Router, TanStack Query. It renders the admin shell and read-only pages.
- `apps/admin-backend`: Fastify local backend bound to `127.0.0.1`. It owns secrets, proxies to the remote Admin API, validates responses, normalizes errors, and propagates request IDs.
- `packages/admin-contracts`: shared Zod schemas and TypeScript types for browser/backend contracts.

Browser code only talks to the local backend. The local backend is the security boundary and does not duplicate business source of truth.

## Data Flow

1. Browser calls `/api/bootstrap`, `/api/players`, `/api/players/:accountId`, `/api/runs`, and `/api/runs/:runId` on the local backend.
2. Local backend attaches Cloudflare Access/Admin headers from process environment.
3. Local backend calls the configured remote Admin API URL.
4. Local backend validates responses with shared Zod schemas.
5. Browser consumes validated JSON through a typed API client.

Missing upstream server contracts are represented as explicit backend proxy contracts, not permanent fake production data.

## Auth And Permissions

Roles are `OWNER`, `OPERATOR`, and `VIEWER`. UI action visibility is derived from a permission registry, not direct role string checks. The first slice includes read permissions only: `ACCOUNT_READ`, `RUN_READ`, and `AUDIT_READ` scaffolding.

## Readonly Pages

The shell navigation follows the source of truth hierarchy and initially enables:

- Dashboard
- Players
- Player Detail
- Player Progression
- Runs
- Run Detail

Progression uses the Training Core / Sync model only. It must not introduce legacy per-operator permanent level growth.

## Error Handling

Backend errors normalize to `{ error: { code, message, requestId } }`. Unknown upstream write outcomes are not part of this first read-only batch.

## Testing

Use Vitest for shared contracts, backend route behavior, and frontend pure permission/navigation logic. Run TypeScript build/typecheck for all workspaces before reporting a batch complete.


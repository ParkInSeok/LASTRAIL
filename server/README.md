# LAST RAIL Server

Cloudflare Workers backend for LAST RAIL.

Official domain: `lastrail.app`.

Environment URLs:

| Environment | Game API | Admin API |
| --- | --- | --- |
| DEV | `https://dev-api.lastrail.app` | `https://dev-admin.lastrail.app` |
| STAGING | `https://staging-api.lastrail.app` | `https://staging-admin.lastrail.app` |
| PRODUCTION | `https://api.lastrail.app` | `https://admin.lastrail.app` |

The root `https://lastrail.app` is reserved for the future official website, privacy policy, terms, and support. The async worker has no public domain.

## Commands

```bash
pnpm install
pnpm test
pnpm d1:local:verify
pnpm typecheck
pnpm lint
```

## Source of Truth

The server source of truth is tracked in `docs/architecture/server.md`.

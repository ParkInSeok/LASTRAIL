# Admin API

Base domains:

- DEV: `https://dev-admin.lastrail.app`
- STAGING: `https://staging-admin.lastrail.app`
- PRODUCTION: `https://admin.lastrail.app`

Admin domains must be protected by Cloudflare Access. LAST RAIL authorization maps Access JWT `sub` to `admin_principals.access_subject` and then applies OWNER / OPERATOR / VIEWER permissions.

## Auth Skeleton

`GET /v1/admin/me` is the first protected Admin route.

Current behavior:

- `LOCAL` uses `ADMIN_AUTH_MODE=LOCAL_HEADER` for local tests and development only.
- DEV/STAGING/PRODUCTION use `ADMIN_AUTH_MODE=ACCESS_JWT_REQUIRED`.
- Non-local modes validate `Cf-Access-Jwt-Assertion` with the configured Cloudflare Access issuer and AUD tag.
- If issuer or AUD is not configured, protected routes return `ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED`.
- Local header mode reads `Cf-Access-Subject`, maps it to active `admin_principals.access_subject`, and returns the principal role.

Do not seed real admin principals or expose operational Admin routes until Cloudflare Access is applied and DEV `ADMIN_ACCESS_ISSUER` / `ADMIN_ACCESS_AUD` are configured.

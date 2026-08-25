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

## Balance Config Drafts

`POST /v1/admin/balance-config/drafts` creates a draft row in `balance_config_drafts`.

Required body:

- `schemaVersion`: positive integer
- `requiredContentVersion`: non-negative integer
- `config`: JSON object

Optional body:

- `baseVersion`: positive integer or `null`

OWNER and OPERATOR can create drafts. VIEWER is denied. The server canonicalizes `config`, computes a `sha256:<hex>` checksum, stores revision `1`, and returns draft metadata. This endpoint does not publish, activate, or mutate existing published versions.

## Balance Config Publish

`POST /v1/admin/balance-config/drafts/:draftId/publish` publishes a draft and immediately activates the new published version.

Required body:

- `expectedRevision`: positive integer
- `expectedChecksum`: checksum returned by the draft endpoint

OWNER only. OPERATOR and VIEWER are denied.

Publish validates:

- The draft exists and is still `DRAFT`.
- `expectedRevision` equals the draft revision.
- `expectedChecksum` equals the draft checksum.
- The draft `baseVersion` equals the current active balance version.

On success, the server inserts an immutable row in `balance_config_versions`, records a publish claim, marks the draft `PUBLISHED`, inserts activation history with `PUBLISH`, and moves `balance_config_active` to the new version in one D1 batch.

Current beta limitation: Admin Confirmation Token consumption and `admin_audit_logs` append are still pending and must be added before production operational use.

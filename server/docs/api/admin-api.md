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

## Read-only Operations

`GET /v1/admin/players` returns account search rows for the Admin Console.

Query:

- `q`: optional account id or display name search text

Response:

- `items[].accountId`
- `items[].displayName`
- `items[].status`
- `items[].syncLevel`
- `items[].lastSeenAt`

`GET /v1/admin/players/:accountId` returns account detail with the Training Core / Sync progression inspector. `syncLevel` is the minimum `trained_level` across exactly 5 Training Core slots. Non-core operators use `levelSource: "SYNC"` and `effectiveLevel: syncLevel`. Core operators use `levelSource: "TRAINED"` and their direct `trainedLevel`.

`GET /v1/admin/runs` returns run search rows.

Query:

- `accountId`: optional account filter

Response includes run balance version, result, risk tier, and validator version when available.

`GET /v1/admin/runs/:runId` returns run detail with frozen `run_snapshots.snapshot_json`, run balance version, current active balance version, client build, content version, validator version, and validator runtime revision.

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

`POST /v1/admin/balance-config/drafts/:draftId/publish/confirmations` creates a short-lived confirmation token for a publish payload.

Required body:

- `expectedRevision`: positive integer
- `expectedChecksum`: checksum returned by the draft endpoint

OWNER only. OPERATOR and VIEWER are denied.

The response returns the raw `token` once. The server stores only `token_hash`; never persist or log the raw token.

`POST /v1/admin/balance-config/drafts/:draftId/publish` publishes a draft and immediately activates the new published version.

Required body:

- `expectedRevision`: positive integer
- `expectedChecksum`: checksum returned by the draft endpoint
- `confirmationToken`: raw confirmation token returned by the confirmation endpoint

OWNER only. OPERATOR and VIEWER are denied. Execute-time permission is checked again before token consumption and domain writes.

Confirmation creation and publish both validate:

- The draft exists and is still `DRAFT`.
- `expectedRevision` equals the draft revision.
- `expectedChecksum` equals the draft checksum.
- The draft `baseVersion` equals the current active balance version.

On success, the server consumes the matching confirmation, inserts an immutable row in `balance_config_versions`, records a publish claim, marks the draft `PUBLISHED`, inserts activation history with `PUBLISH`, moves `balance_config_active` to the new version, and appends `admin_audit_logs` in one D1 batch.

Failed publish attempts after admin authentication append `admin_audit_logs` with `result = 'FAILURE'` and a structured error code. The raw confirmation token is never written to audit logs.

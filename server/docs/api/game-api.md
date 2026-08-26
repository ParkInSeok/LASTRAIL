# Game API

Base domains:

- DEV: `https://dev-api.lastrail.app`
- PRD: `https://api.lastrail.app`

The server currently uses `dev` and `prd` environment axes only. PRD resources and deploys remain approval-gated.

Unversioned: `GET /status`.

Versioned routes use `/v1/*`.

## Account Identity

Game API player writes use LAST RAIL account auth, not Cloudflare Access.
Cloudflare Access is for Admin API only.

Production and DEV remote Source of Truth:

- The client sends `Authorization: Bearer <access-token>`.
- The access token is issued by LAST RAIL auth, initially through
  `POST /v1/auth/guest` and later refreshable through `POST /v1/auth/refresh`.
- The server verifies token signature, expiry, token family/session status, and
  account status before resolving `accountId`.
- The resolved `accountId` from auth middleware is the only account identity
  available to Game API write routes.
- Request bodies must not include `accountId`.
- Client-supplied account headers are not trusted.

DEV/local policy:

- Remote DEV must use the same account-auth path as PRD for economy-impacting
  writes.
- Until `POST /v1/auth/guest` / access-token verification exists, new
  account-scoped commit routes remain placeholder-only and must not mutate D1.
- Unit and integration tests may inject a test account context through test
  harnesses/mocks only; that path must not be exposed by Wrangler DEV/PRD
  config or public HTTP routes.
- Do not add a remote DEV `X-Account-Id`, `X-Lastrail-Account-Id`, or similar
  shortcut for gacha/economy writes.

Idempotency linkage:

- Idempotency lookup is scoped by the authenticated `accountId`.
- Game API writes use `idempotency_records(account_id, operation,
  idempotency_key)`.
- For gacha pulls, replay lookup is:
  `accountId + operation = 'gacha_pull' + Idempotency-Key`.
- The `accountId` used for idempotency must match all written batch, result,
  item ledger, progression ledger, and mutation rows in the same transaction.

## Auth

`POST /v1/auth/guest` creates a guest account and returns the initial token
pair.

Response:

```json
{
  "tokenType": "Bearer",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:15:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:00:00.000Z"
}
```

Server writes:

- `accounts`
- `auth_identities` with provider `GUEST`
- `auth_sessions` with the refresh token hash only
- `player_profiles`
- `account_runtime_state`
- `account_tutorial_state`

Security notes:

- Access tokens are signed by `AUTH_SIGNING_KEY_CURRENT`.
- Store `AUTH_SIGNING_KEY_CURRENT` as a Worker secret or local `.dev.vars`
  value. Do not commit the secret to Git.
- Refresh token plaintext is returned once and never stored in D1.
- `auth_sessions.refresh_token_hash` stores only a SHA-256 hash.
- Access token TTL is currently 15 minutes.
- Guest refresh token/session TTL is currently 30 days.

`POST /v1/auth/refresh` rotates a refresh token and returns a new token pair.

Request:

```json
{
  "refreshToken": "refresh_..."
}
```

Response:

```json
{
  "tokenType": "Bearer",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:30:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:15:00.000Z"
}
```

Refresh rules:

- Lookup uses `sha256(refreshToken)` against
  `auth_sessions.refresh_token_hash`.
- Only `ACTIVE` sessions for `ACTIVE` accounts can refresh.
- Successful refresh marks the old session `ROTATED`.
- Successful refresh inserts a new `ACTIVE` session in the same family with
  `generation + 1`.
- Reusing an already `ROTATED` refresh token marks active/rotated sessions in
  that family as `REUSED` and returns `refresh_token_reused`.

## Client Config

`GET /v1/client-config` returns public Unity client configuration:

- `environment`
- `apiBaseUrl`
- `balanceConfigEndpointTemplate`
- `minimumClientVersion`
- `maintenance`

This response must not include secrets, tokens, Cloudflare credentials, Admin API URLs, or Access credentials. API domains are public client configuration, not a secret. If tamper resistance is needed later, add a signed config payload instead of encrypting a value that the Unity client must decrypt.

## Balance Config

Balance payloads remain on Game API: `GET /v1/balance-config/:version`.

The endpoint reads published versions from D1 `balance_config_versions` and returns:

- `version`
- `schemaVersion`
- `requiredContentVersion`
- `checksum`
- `publishedAt`
- `payload`

Only positive integer versions are accepted. Missing/unpublished versions return `BALANCE_CONFIG_NOT_PUBLISHED`. Invalid stored JSON returns `BALANCE_CONFIG_PAYLOAD_INVALID`.

## Run Finish / Reward

Run finish uses `POST /v1/runs/:runId/finish`. See `docs/api/run-result-submit.md`.

Accepted run finish responses return `rewardPreviewId` first. Reward review and
claim/commit follow `docs/api/run-reward-preview-claim.md`.

## Gacha

Gacha pull history, banner pity state, duplicate conversion, and ticket/material
ledger linkage are drafted in `docs/api/gacha-pity-result-history.md`.

Pull execution uses:

```text
POST /v1/gacha/banners/:bannerId/pulls
Idempotency-Key: <client-generated-key>
```

Contract baseline:

- Request body is `{ "pullCount": 1 }` or `{ "pullCount": 10 }`.
- `bannerId` is path-only; sending `bannerId` in the body is
  `contract_violation`.
- The server returns `resultStatus` as `ACCEPTED`, `DUPLICATE_REPLAY`, or
  `REJECTED`.
- Success and duplicate replay responses include stored pull results and
  `postPityState`.
- `postPityState` is the state Unity should display after the pull.
- Cost is server-calculated as `cur_command_ticket` with quantity equal to
  `pullCount`.
- Pull probability, pity, duplicate conversion, ticket spend, grants, and ledger
  writes are server Source of Truth.

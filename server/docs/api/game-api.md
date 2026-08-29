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

Minimal ops error write integration:

- Missing Google verifier/audience config writes a scrubbed
  `google_auth_config_missing` ops error with `severity = critical`.
- The response remains `google_auth_not_configured`; raw Google ID tokens and
  full claims are not stored.
- Offline reward state conflicts write scrubbed
  `offline_reward_state_conflict` ops errors.
- Gacha pull commit batch conflicts write scrubbed `gacha_state_conflict` ops
  errors.
- Accepted full-clear run finish story progress commit conflicts write scrubbed
  `story_progress_state_conflict` ops errors.
- Normal validation rejects, insufficient resources, inactive banners, and
  duplicate replay outcomes are not ops error writes.
- Ops error writes are fail-soft and must not worsen the player-facing API
  response.

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
- `account_offline_reward_state` with:
  - `last_offline_reward_claim_at = account.created_at`
  - `last_story_progress_id = 'none'`
  - `last_balance_config_version = NULL`
  - `last_narrative_version = 'act1.2026-08-27.001'`
  - `last_previewed_at = NULL`
  - `revision = 0`

Security notes:

- Access tokens are signed by `AUTH_SIGNING_KEY_CURRENT`.
- Store `AUTH_SIGNING_KEY_CURRENT` as a Worker secret or local `.dev.vars`
  value. Do not commit the secret to Git.
- Refresh token plaintext is returned once and never stored in D1.
- `auth_sessions.refresh_token_hash` stores only a SHA-256 hash.
- Access token TTL is currently 15 minutes.
- Guest refresh token/session TTL is currently 30 days.

### Google Sign-In / Guest Link

Launch includes Google Sign-In for Unity. The server verifies Google ID tokens
and then issues the normal LAST RAIL `Bearer` access/refresh token pair. The
token bundle shape intentionally stays compatible with `POST /v1/auth/guest`
and `POST /v1/auth/refresh`.

Implementation note: production verification must follow the current Google
Identity / OpenID Connect guidance at implementation/deploy time:

- Verify the ID token signature against Google public keys from the Google JWKS
  endpoint.
- Verify `aud` against the configured Google OAuth client id allow-list.
- Verify `iss` is `accounts.google.com` or `https://accounts.google.com`.
- Verify the token has not expired.
- Use the Google `sub` claim as the provider subject. Do not key identity by
  email.
- Cache Google public keys according to the HTTP `Cache-Control` max-age
  response header and refresh when the cached key set expires or the key id is
  unavailable.

The actual Google OAuth client id / audience values are decision-queue items
and must not be hardcoded into source. The route uses
`GOOGLE_AUTH_AUDIENCES` as the allow-list source. Local tests may still inject
`GOOGLE_ID_TOKEN_VERIFIER` as a fake verifier, but production can use the
default Worker/WebCrypto verifier adapter behind the same
`verifyIdToken(idToken, audiences)` abstraction.

Current safe adapter boundary:

- Accepts only JWTs with `alg = RS256` and a non-empty `kid`.
- Fetches public keys from `https://www.googleapis.com/oauth2/v3/certs`.
- Verifies the RS256 signature with WebCrypto.
- Rejects missing or unconfigured audiences.
- Rejects unknown audience, unsupported issuer, expired token, missing subject,
  missing key id, unavailable JWKS, or invalid signature.
- Player-facing invalid token failures use stable generic
  `google_id_token_invalid` without exposing the internal verifier reason.
- JWKS fetch/parse failures use `google_auth_temporarily_unavailable` and may
  write a scrubbed `google_auth_verifier_unavailable` ops error for operations.
- Returns only the Google `sub` as `subject` plus optional scrub-safe email
  fields for future use; route persistence continues to use only `subject`.

`POST /v1/auth/google` signs in or restores an account through a verified
Google identity.

Request:

```json
{
  "idToken": "google-id-token"
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
  "accessTokenExpiresAt": "2026-08-26T00:15:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:00:00.000Z"
}
```

`POST /v1/auth/google/link` links the authenticated guest/current account to a
verified Google identity.

Headers:

```text
Authorization: Bearer <access-token>
```

Request:

```json
{
  "idToken": "google-id-token"
}
```

Response:

```json
{
  "tokenType": "Bearer",
  "linkStatus": "LINKED",
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

Link status:

- `LINKED`: this account is newly linked to the Google provider subject.
- `ALREADY_LINKED`: the same account already owns that provider subject; the
  response is replay-safe/idempotent success.

Conflict and boundary rules:

- `auth_identities(provider, provider_subject)` remains the unique identity
  boundary. For Google, `provider = 'GOOGLE'` and `provider_subject = sub`.
- Automatic account merge is not available at launch.
- A Google identity already linked to another account returns
  `google_identity_already_linked`.
- Request body/query/header `accountId`, `account_id`, `X-Account-Id`, or
  `X-Lastrail-Account-Id` style shortcuts are not accepted as Source of Truth.
- Raw Google ID tokens are never stored in D1, logs, or responses.
- Raw Google claims/full claim dumps are not stored or returned. If future
  support visibility needs provider context, use scrubbed metadata only.
- Refresh token plaintext storage remains forbidden; only
  `auth_sessions.refresh_token_hash` is persisted.

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
Launch risk/reward preview generation policy is drafted in
`docs/api/run-risk-reward.md`.

Raid boss-only run lifecycle impact is captured in
`docs/api/raid-boss-run-contract.md`. Launch keeps one run finish route and
distinguishes ordinary 15-wave operations from boss-only raids with
`encounterType`.

Act 1 launch starts ordinary and raid encounters with server-authored full
augment/tactic package refs. See `docs/api/run-start-augment-package.md`.

## Story Progress

Story progress Source of Truth and read model policy is drafted in
`docs/api/story-progress.md`.

Current local Game API read endpoint:

```text
GET /v1/story-progress
Authorization: Bearer <access-token>
```

Baseline policy:

- Server owns durable story progress and unlock truth.
- Unity owns narrative presentation and localized text playback.
- Accepted run finish is the durable story progress update moment for the
  current local contract slice.
- Client-supplied story progress, unlock state, and narrative flags are not
  Source of Truth.
- Account id is resolved only from the authenticated player session.
- Query/body account id is rejected with `contract_violation`.
- Empty accounts return accepted Act 1 `none` progress and default
  `opn_01_last_departure` unlock.

## Offline Rewards

Offline reward progression is defined in `docs/api/offline-rewards.md`.
The current local contract slices add server-side balance schema parsing, a
pure preview calculator, a read-only preview endpoint, and a claim endpoint for
launch currency reward lines:

```text
GET /v1/offline-rewards/preview
Authorization: Bearer <access-token>
```

```text
POST /v1/offline-rewards/claim
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

The preview endpoint resolves account id from the authenticated player session
only. It rejects query/body Source of Truth fields with `contract_violation`,
returns server-calculated display `rewardLines`, and does not write ledger rows,
grant rewards, or consume offline claim state.

Migration `0015_account_offline_reward_state.sql` adds the durable offline
reward baseline table. Preview reads
`account_offline_reward_state.last_offline_reward_claim_at` first and falls
back to `accounts.created_at` for older accounts without a row.

The claim endpoint rejects query/body Source of Truth fields, requires
`Idempotency-Key`, calculates rewards from server-stored story/offline state,
writes account/economy/currency ledger rows atomically, and stores successful
`idempotency_records.operation = 'offline_reward_claim'`. Duplicate replay
returns the stored claim id without advancing state or granting again.

Launch offline claim only grants currency reward lines supported by
`account_currencies`/`currency_ledger`. Item grants and full command-ticket,
command-ticket-fragment, or special-core offline loops remain closed.

Baseline policy:

- Offline rewards scale with story progression, not run risk.
- Risk rewards remain active-run-only.
- Unity must not calculate offline reward amounts, multipliers, storage caps, or
  economy grants.

## Gacha

Gacha pull history, banner pity state, duplicate conversion, and ticket/material
ledger linkage are drafted in `docs/api/gacha-pity-result-history.md`.
Future gacha PRD rarity selection is drafted in
`docs/api/gacha-prd-rarity-selection.md`; it keeps the current response shape
and hard `guaranteeType` semantics unchanged.

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
- Launch data has a `TESTING_TUNING_DRAFT` fixture in
  `launchGachaDataDraft`. It is used for local response stability smoke only and
  is not a production seed/config promotion.

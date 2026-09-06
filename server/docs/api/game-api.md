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

The expanded three-path launch auth design is tracked in
`auth-account-linking.md`. That draft covers guest restore, ID/password
registration/login/link, Google link, and provider-owned-by-other-account
conflict preview/confirm. Local ID/password register/login/link and password
reset route skeletons are implemented behind provider-neutral recovery email
verification. A Gmail API recovery email sender adapter is available behind the
same sender contract; real Gmail OAuth credentials, sender authorization, and
real-send smoke remain approval/setup-gated.
Account-switch endpoints are not implemented yet. ID/password registration and
link completion require an explicitly submitted and server-verified recovery
email; Google claim email is not auto-promoted into password recovery state.

Realtime two-player cooperative combat starts in `realtime-coop.md`. The first
server slice exposes room creation and WebSocket connection skeletons behind a
Durable Object binding, with server validation/judgment as the authority model.

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

## Profile / Display Name

`accountId` is the immutable server primary key. It is not the player-facing
nickname and must not be supplied by the client in profile request bodies,
query parameters, or shortcut headers.

Current local endpoints:

```text
GET /v1/profile
GET /v1/profile/display-name/check?displayName=<candidate>
POST /v1/profile/display-name
```

All routes require `Authorization: Bearer <access-token>` and resolve the
account from the authenticated session context.

`GET /v1/profile` returns:

```json
{
  "accepted": true,
  "account": {
    "id": "account_..."
  },
  "profile": {
    "displayName": "Rail Captain",
    "commanderXp": 120,
    "displayNameUpdatedAt": "2026-08-27T00:00:00.000Z",
    "nextDisplayNameChangeAvailableAt": "2026-09-03T00:00:00.000Z"
  },
  "errorCode": null,
  "message": null
}
```

Display name policy:

- `player_profiles.display_name` stores the original player-facing display
  value.
- `player_profiles.display_name_normalized` stores the uniqueness key.
- Normalization uses Unicode NFKC, trims leading/trailing whitespace, collapses
  repeated whitespace to a single space, and compares case-insensitively.
- Length is 2 to 16 Unicode code points.
- Allowed characters are letters, numbers, space, `_`, and `-`.
- Reserved system-like names such as `admin`, `system`, `support`, and names
  starting with `account_`, `admin_`, `gm_`, or `system_` are rejected.
- Display names are globally unique by normalized value.
- Re-submitting the current normalized display name for the same account is
  idempotent success.
- Changing to a different display name is blocked for 7 days after the previous
  display name update.

`GET /v1/profile/display-name/check` returns `available: true` when the
normalized candidate is unused or already belongs to the authenticated account.
Invalid candidates return `invalid_display_name`; candidates owned by another
account return `display_name_taken`.

`POST /v1/profile/display-name` accepts:

```json
{
  "displayName": "Rail Captain"
}
```

Success returns `changeStatus: "UPDATED"` or `"UNCHANGED"`. Error codes:

- `auth_required`
- `access_token_invalid`
- `auth_session_invalid`
- `account_not_active`
- `contract_violation`
- `invalid_display_name`
- `display_name_taken`
- `display_name_change_cooldown`
- `profile_not_found`

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

The actual Google OAuth client id / audience values are PRD/app-registration
inputs and must not be hardcoded into source. DEV real-Google smoke is not a
launch blocker and may remain `google_auth_not_configured` until PRD values are
available. The route uses `GOOGLE_AUTH_AUDIENCES` as the allow-list source.
Local tests may still inject `GOOGLE_ID_TOKEN_VERIFIER` as a fake verifier, but
production can use the default Worker/WebCrypto verifier adapter behind the same
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

Run start uses a local launch contract route:

```text
POST /v1/runs/start
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

Request body:

```json
{
  "operationPreviewId": "preview_..."
}
```

The account id is resolved only from the authenticated player session. Query
parameters and client-supplied source-of-truth fields such as `accountId`,
`risk`, `riskLevel`, `storyProgress`, reward amounts, economy grants, or
animation timeline data are rejected with `contract_violation`.

Accepted response shape:

```json
{
  "accepted": true,
  "startStatus": "STARTED",
  "runId": "run_...",
  "operationPreviewId": "preview_...",
  "operationId": "opn_03_water_stop",
  "actId": "act_01_wasteland_railway",
  "regionId": "region_wasteland_railway",
  "encounterType": "ORDINARY_15_WAVE",
  "riskLevel": "III",
  "risk": "III",
  "balanceVersion": "501",
  "contentVersion": "act1.launch.content.v1",
  "ordinary": {
    "totalWaves": 15,
    "startWaveNumber": 1
  },
  "raid": null,
  "raidId": null,
  "bossId": null,
  "bossPhaseRefs": [],
  "allowedOperatorSlots": 5,
  "startAugmentPackageRef": "act1_full_start_package_v1",
  "startTacticPackageRef": "act1_full_start_package_v1",
  "serverStartedAt": "2026-08-27T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

Raid boss-only previews use the same route and return `encounterType =
RAID_BOSS`, `ordinary = null`, and raid refs under both the compact top-level
fields and the `raid` object. Ordinary previews must not leak `raidId`,
`bossId`, or `bossPhaseRefs` from malformed preview JSON.

Run start consumes `operation_previews` with an affected-row guard before
creating the run snapshot. `Idempotency-Key` is required for launch. A retry
with the same account, operation `run_start`, key, and `operationPreviewId`
returns the stored `STARTED` response shape and must not create a second
`runs` row or `run_snapshots` row. Reusing the same key for a different
`operationPreviewId` returns `contract_violation`. A consumed preview without a
matching idempotency record returns `409` with
`operation_preview_already_consumed`.

If preview consumption succeeds but run/snapshot/idempotency persistence fails,
the route returns `409 run_start_state_conflict`, attempts to reopen the preview
to `ACTIVE`, and writes a scrubbed fail-soft ops error log. The client should
retry with the same `Idempotency-Key` after fetching/confirming preview state.

Run finish uses `POST /v1/runs/:runId/finish`. See `docs/api/run-result-submit.md`.

Accepted run finish responses return `rewardPreviewId` first. Reward review and
claim/commit follow `docs/api/run-reward-preview-claim.md`.
Launch risk/reward preview generation policy is drafted in
`docs/api/run-risk-reward.md`.

Raid boss-only run lifecycle impact is captured in
`docs/api/raid-boss-run-contract.md`. Launch keeps one run finish route and
distinguishes ordinary 15-wave operations from boss-only raids with
`encounterType`.

Act 1 story raid failure returns a server-authored `staminaRefund` marker with
`status = COMMITTED`, `refundType = STAMINA`, `reason = STORY_RAID_FAILURE`,
and `policyRef = act1_story_raid_failure_stamina_refund_v1`. The accepted
finish transaction writes `STORY_RAID_STAMINA_REFUND` mutation evidence, one
`fuel_ledger` row with reason `STORY_RAID_FAILURE_REFUND`, and a capped
`fuel_state` update. The response does not expose refund amount, raw run start
cost, or fuel balances. This is Act 1 story raid only and is not World Raid
settlement.

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
- Launch command ticket acquisition is promoted into local launch balance
  contract as a non-repeat 12-ticket path: login bonus 5, `opn_03` first clear
  1, `opn_05` first clear 1, `opn_06` first clear 2, and story raid first clear
  3. The login bonus tickets are claimed through `POST /v1/login-bonus/claim`.
  The first-clear tickets are emitted as server-owned `ITEM` `rewardLines` and
  committed through `account_items`/`item_ledger` during reward claim. Remote
  production seed/config deployment still requires explicit approval.

## Launch Login Bonus

```text
POST /v1/login-bonus/claim
Authorization: Bearer <access-token>
Idempotency-Key: <client-generated-key>
```

Contract baseline:

- Request body and query parameters are not accepted.
- Account identity comes only from verified Game API bearer auth.
- The launch bonus id is `launch_login_bonus_act1_v1`.
- The server grants one non-repeat `ITEM` reward line:
  `cur_command_ticket x5`, `source = 'LAUNCH_LOGIN_BONUS'`.
- Success writes `account_mutations`, `economy_transactions`,
  `reward_transactions`, `account_items`, `item_ledger`,
  `account_login_bonus_claims`, and `idempotency_records.operation =
  'login_bonus_claim'` in one committed batch.
- Same account + same `Idempotency-Key` returns `DUPLICATE_REPLAY` and does not
  write ledger or grant again.
- Same account + different key after the bonus was claimed returns
  `ALREADY_CLAIMED` and does not write ledger or grant again.
- The response does not include raw ledger dumps, tokens, auth headers, RNG
  material, probability data, full weight tables, or account shortcut data.


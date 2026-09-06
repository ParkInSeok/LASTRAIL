# Auth Account Linking Contract Draft

Status: draft, local contract only. This document closes the current Unity-facing
auth dependency without enabling remote migrations, real Google OAuth values,
email delivery, or production account writes.

## Scope

Act 1 login has three player-facing entry paths:

- Guest start
- Login ID and password
- Google Sign-In

`accountId` remains the immutable server primary key. `loginId` and
`displayName` are player-facing/account-facing identifiers with different
purposes:

- `accountId`: server-generated primary key, never chosen by the player.
- `displayName`: nickname shown to players, globally unique by normalized value.
- `loginId`: credential identifier for ID/password auth, not a nickname and not
  a profile display field.

Unity may store only the server-issued session secret material in secure device
storage. It must not store raw passwords or Google ID tokens long-term.

## Current Implemented Baseline

Already implemented:

- `POST /v1/auth/guest`
- `POST /v1/auth/refresh`
- `POST /v1/auth/google`
- `POST /v1/auth/google/link`
- `POST /v1/auth/password/recovery-email/verify/start` (local
  provider-neutral sender contract only)
- `POST /v1/auth/password/recovery-email/verify/confirm`
- `POST /v1/auth/password/register`
- `POST /v1/auth/password/login`
- `POST /v1/auth/password/link`
- `POST /v1/auth/password/reset/start` (generic response, local
  provider-neutral sender contract only)
- `POST /v1/auth/password/reset/confirm`
- `GET /v1/profile`
- `GET /v1/profile/display-name/check`
- `POST /v1/profile/display-name`

`auth_identities.provider` remains `GUEST`/`GOOGLE`. ID/password credentials are
stored separately in `auth_password_credentials` and use
`auth_recovery_emails` plus `auth_email_verification_challenges` for verified
recovery ownership.

## Guest Restore

Guest creation returns the normal token bundle:

- `tokenType`
- `account.id`
- `account.status`
- `accessToken`
- `accessTokenExpiresAt`
- `refreshToken`
- `refreshTokenExpiresAt`

Unity should persist the refresh token using Android Keystore-backed secure
storage. App restart should attempt `POST /v1/auth/refresh` before offering a
new guest. Timeout/offline must not delete the stored session or create a new
guest. Only explicit invalid/expired/reused session responses should move the
player to recovery or login UI.

## Login ID / Password Candidate Contract

Local endpoints:

```text
POST /v1/auth/password/register
POST /v1/auth/password/login
POST /v1/auth/password/link
POST /v1/auth/password/recovery-email/verify/start
POST /v1/auth/password/recovery-email/verify/confirm
POST /v1/auth/password/reset/start
POST /v1/auth/password/reset/confirm
GET /v1/auth/links
```

`GET /v1/auth/links` is still a future read-only helper candidate; the runtime
ID/password routes above are implemented locally without enabling production
email delivery.

`POST /v1/auth/password/register` creates a new account with an ID/password
credential. `POST /v1/auth/password/link` requires Bearer auth and attaches an
unused login ID credential to the current account, preserving `accountId`,
inventory, story progress, rewards, and sessions.

Both register and link require an explicit recovery email and completed
server-side email ownership verification before the password credential becomes
usable. Pending registration/link state must not replace the current guest or
Google session. Abandon, timeout, resend, or retry keeps the existing account
state and must not create duplicate accounts or credentials.

`POST /v1/auth/password/login` signs in to the account that owns the login ID
and returns the same token bundle shape as guest and Google auth.

Candidate request:

```json
{
  "loginId": "rail_commander",
  "password": "player supplied secret",
  "recoveryEmail": "player@example.com",
  "verificationChallengeId": "email_challenge_..."
}
```

Do not accept `accountId`, `displayName`, reward fields, story progress fields,
or provider subject shortcuts in request bodies, query parameters, or headers.

Register response:

```json
{
  "tokenType": "Bearer",
  "accepted": true,
  "registerStatus": "REGISTERED",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:15:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

Login response uses the same token bundle and adds
`signInStatus: "SIGNED_IN"`. Password link response uses the same token bundle
and adds `linkStatus: "LINKED"`.

Recovery email verification start response:

```json
{
  "accepted": true,
  "verificationStatus": "PENDING",
  "challengeId": "email_challenge_...",
  "expiresAt": "2026-09-06T00:15:00.000Z",
  "retryAfterSeconds": 60,
  "errorCode": null,
  "message": null
}
```

Recovery email verification confirm response:

```json
{
  "accepted": true,
  "verificationStatus": "VERIFIED",
  "challengeId": "email_challenge_...",
  "expiresAt": "2026-09-06T00:15:00.000Z",
  "retryAfterSeconds": null,
  "errorCode": null,
  "message": null
}
```

Password reset start response is account-existence agnostic:

```json
{
  "accepted": true,
  "resetStatus": "EMAIL_SENT_IF_AVAILABLE",
  "errorCode": null,
  "message": null
}
```

The reset start HTTP response intentionally does not include `challengeId` or
any proof, because it must not reveal whether the account exists. The email
delivery payload contains two separate pieces:

- `challengeId`: non-secret reset reference. It may be embedded in a deep link
  or shown as a reference code.
- `verificationCode`: the secret one-use code. It should be typed by the
  player and must not be placed in app logs, diagnostics, URLs, or query
  strings.

Recommended Unity UX: open reset confirm from a deep link or pasted reference
that carries only `challengeId`, then ask the player to enter
`verificationCode` and the new password. Link click alone does not authenticate,
does not rotate sessions, and does not complete the reset.

Password reset confirm response does not log the player in and does not return
tokens:

```json
{
  "accepted": true,
  "resetStatus": "RESET_CONFIRMED",
  "errorCode": null,
  "message": "Password was reset. Please sign in again."
}
```

Link status values:

- `LINKED`: current account newly owns the login ID credential.
- `ALREADY_LINKED`: current account already owns that credential; idempotent
  success.
- `CONFLICT_REQUIRES_CONFIRMATION`: credential belongs to another account and
  account switching must use the conflict preview/confirm flow.

Recovery email rules:

- Guest start and Google-only login do not require a recovery email.
- Google claim email is not automatically registered as the password recovery
  email. The player must explicitly submit and verify a recovery email.
- Recovery email is separate from `loginId`, `displayName`, and `accountId`.
- Matching email addresses do not link, merge, switch, or recover accounts by
  themselves.
- UI/support surfaces should use masked email only when a recovery flow needs
  it; public profile and account-conflict comparison must not expose email.

Candidate verification defaults:

- Email verification challenge TTL: 15 minutes.
- Resend cooldown: 60 seconds per account/email/purpose.
- Verification and reset proofs are one-use, cryptographically random, hashed
  at rest, and bound to purpose, account or pending transaction, and expiry.
- Reset request responses are generic whether or not the login ID/email exists.
- Reset request or link click alone must not sign the player in or revoke
  current sessions.
- Password reset confirm invalidates existing active sessions only after the
  new password verifier is stored. Linked Google identity is not deleted.
- Recovery email change requires recent re-authentication plus verification of
  the new address before replacing the stored recovery email.
- If the old recovery address is inaccessible, launch support must not promise
  manual recovery unless a separate approved support policy exists.

## Login ID Policy Recommendation

This is a recommendation, not a user-finalized value:

- Normalize with Unicode NFKC, trim, and compare case-insensitively.
- Length 4 to 32 code points.
- Allow letters, numbers, `_`, `-`, and `.`.
- Reject reserved system-like values and values that look like internal ids.
- Return generic login failures for unknown ID or wrong password to reduce
  account enumeration.

## Password Storage Recommendation

Use a proven password hashing implementation, not a custom algorithm. OWASP
recommends Argon2id when available, or scrypt/bcrypt/PBKDF2 with appropriate
costs. NIST requires salted password verifier storage and recommends storing
algorithm/cost metadata so work factors can migrate over time. The current
Workers-compatible local route skeleton uses WebCrypto `PBKDF2_SHA256` with
per-password random salt and stored cost metadata until a stronger Workers-safe
password hashing provider is approved.

Candidate local schema:

```sql
CREATE TABLE auth_password_credentials (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  login_id_normalized TEXT NOT NULL,
  password_hash TEXT NOT NULL,
  password_hash_algorithm TEXT NOT NULL,
  password_hash_params_json TEXT NOT NULL,
  recovery_email_normalized TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE UNIQUE INDEX idx_auth_password_credentials_login_id_unique
  ON auth_password_credentials(login_id_normalized);

CREATE TABLE auth_recovery_emails (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  email_normalized TEXT NOT NULL,
  email_masked TEXT NOT NULL,
  status TEXT NOT NULL,
  verified_at INTEGER,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE auth_email_verification_challenges (
  id TEXT PRIMARY KEY,
  account_id TEXT REFERENCES accounts(id) ON DELETE CASCADE,
  email_normalized TEXT NOT NULL,
  purpose TEXT NOT NULL,
  verifier_hash TEXT NOT NULL,
  status TEXT NOT NULL,
  expires_at INTEGER NOT NULL,
  consumed_at INTEGER,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);
```

If Workers runtime/package constraints block Argon2id, stop and report a
decision gate rather than weakening storage silently. PBKDF2-HMAC-SHA-256 using
WebCrypto is a possible compatibility fallback, but its iteration count and
latency must be measured before launch approval.

## Google Link Baseline

Current Google rules remain:

- provider is `GOOGLE`
- provider subject is Google `sub`
- same account link is `ALREADY_LINKED`
- unused provider is `LINKED`
- provider owned by another account returns `google_identity_already_linked`
- automatic account merge is not available

Future conflict preview/confirm can generalize Google and login ID conflicts,
but the existing Google route should not silently switch accounts.

## Conflict Preview / Confirm Candidate

When the current guest tries to link a login ID or Google identity that already
belongs to another account, the server must not merge accounts or overwrite
either account.

Candidate endpoints:

```text
POST /v1/auth/account-switch/preview
POST /v1/auth/account-switch/confirm
```

Preview requires:

- current Bearer auth for the guest/current account
- provider proof for the target credential
- no account ID shortcut

Preview response:

```json
{
  "accepted": true,
  "switchStatus": "PREVIEW_READY",
  "switchRef": "account_switch_...",
  "expiresAt": "2026-08-26T00:05:00.000Z",
  "currentAccount": {
    "displayName": "Guest Commander",
    "accountType": "GUEST",
    "storyProgressId": "opn_03_water_stop",
    "lastLoginAt": "2026-08-26T00:00:00.000Z",
    "hasRecoveryCredential": false
  },
  "targetAccount": {
    "displayName": "Rail Captain",
    "accountType": "GOOGLE",
    "storyProgressId": "opn_06_broken_line",
    "lastLoginAt": "2026-08-20T00:00:00.000Z",
    "hasRecoveryCredential": true
  },
  "mergeAvailable": false,
  "allowedActions": ["KEEP_CURRENT", "SWITCH_TO_TARGET"],
  "warningCode": "guest_without_recovery_may_be_lost",
  "errorCode": null,
  "message": null
}
```

Confirm request:

```json
{
  "switchRef": "account_switch_...",
  "action": "SWITCH_TO_TARGET"
}
```

Confirm success returns:

```json
{
  "accepted": true,
  "switchStatus": "ACCOUNT_SWITCHED",
  "tokenType": "Bearer",
  "account": {
    "id": "account_...",
    "status": "ACTIVE"
  },
  "accessToken": "lst1...",
  "accessTokenExpiresAt": "2026-08-26T00:15:00.000Z",
  "refreshToken": "refresh_...",
  "refreshTokenExpiresAt": "2026-09-25T00:00:00.000Z",
  "errorCode": null,
  "message": null
}
```

`KEEP_CURRENT` cancels the switch and preserves the current guest session.
`SWITCH_TO_TARGET` creates a new target-account session; Unity must replace the
securely stored refresh token only after the new token bundle is durably stored.

## Conflict Safety Rules

- Progress comparison is shown only after successful target credential proof.
- Safe summary must not expose raw `accountId`, full inventory, ledger detail,
  raw tokens, password hashes, Google ID tokens, or full Google claims.
- Progress is never auto-merged, max-selected, copied, or repaired.
- If both accounts have zero progress, the provider-owned-by-other-account case
  is still a conflict.
- Preview refs are short-lived and bound to current account, target provider,
  intended action, and snapshot revision.
- Stale refs, replayed confirms, different provider proofs, or changed target
  identity revisions return stable rejection.
- Combat, reward claim, run finish, gacha pull, or offline reward claim in
  progress should block account switching until the transaction is complete.
- Timeout/cancel keeps the existing guest session.
- Keystore save failure after confirm is a client persistence failure; Unity
  should keep the previous session available until it can safely retry or ask
  the player to sign in again. The server must not create another guest.

Candidate error codes:

- `login_id_taken`
- `login_id_invalid`
- `password_invalid`
- `password_auth_failed`
- `password_auth_unavailable`
- `auth_identity_conflict`
- `account_switch_preview_required`
- `account_switch_ref_expired`
- `account_switch_ref_invalid`
- `account_switch_revision_conflict`
- `account_switch_blocked_by_active_transaction`
- `guest_recovery_missing`

## Recovery / Consent Decision Queue

Not finalized:

- Terms/privacy consent storage and minimum signup-complete gate.
- Whether account switch requires re-authentication immediately before confirm.
- Same recovery email on multiple accounts: currently allowed by the draft
  schema to avoid treating email equality as identity/merge truth; decide
  whether to keep this or add a stricter policy.
- Account lookup UX: whether players start recovery from `loginId`, recovery
  email, or both, while preserving account-enumeration resistance.
- Email provider: Cloudflare Email Service Workers binding is a platform-native
  candidate, but sender domain onboarding, DNS, credentials, deliverability, and
  cost approval are not complete.
- Whether to keep WebCrypto `PBKDF2_SHA256` for launch or approve a stronger
  Workers-safe password hashing service/library before production.

Do not claim the ID/password system is production-ready until these gates are
resolved.

## Unity Contract Notes

Unity may implement local UI/state for:

- guest restore first
- login selection
- link status display
- conflict preview display
- keep-current vs switch-to-target confirmation
- secure session owner replacement after successful confirm

Unity must not:

- generate account IDs
- infer signup completion from nickname alone
- merge accounts
- calculate story/reward/economy state
- store raw passwords or Google ID tokens long-term
- replace the active local session from preview alone

# Versioned Balance

Published balance versions are immutable. Admin edits happen only in drafts. Publish validates expected revision and checksum, inserts a new version, records activation history, and moves the active pointer atomically.

Rollback never edits a version; it changes the active pointer and appends activation history.

## Draft Creation

Admin draft creation writes only to `balance_config_drafts`.

- OWNER and OPERATOR can create drafts.
- VIEWER cannot create drafts.
- The server canonicalizes the config JSON and computes the checksum.
- Draft creation does not publish, activate, or modify published versions.

## Draft Publish

Admin draft publish writes the first operational versioned-balance flow:

- OWNER only.
- Request body must provide `expectedRevision` and `expectedChecksum`.
- A short-lived confirmation token must be created for the same publish payload before execute.
- The raw confirmation token is returned once and only its hash is stored.
- The draft must still be `DRAFT`.
- The expected revision and checksum must match the stored draft.
- During beta, `draft.baseVersion` must equal the current `balance_config_active.version`.

Publish success means publish and activate in the same operation:

- Consume the matching `admin_confirmations` row.
- Insert a new immutable `balance_config_versions` row.
- Insert `balance_config_publish_claims`.
- Mark the draft `PUBLISHED`.
- Insert `balance_config_activations` with `PUBLISH`.
- Move `balance_config_active` to the new version.
- Append `admin_audit_logs` with permission `BALANCE_PUBLISH`.

Failed publish attempts currently return structured error codes; failure audit rows remain the next implementation step before production operational use.

## DEV Seed

DEV can be bootstrapped with `packages/db/seeds/dev/0001_balance_config_v1.sql`.

Apply it only to DEV:

```bash
pnpm d1:dev:seed:balance
```

The seed inserts published version `1`, records an activation, and points `balance_config_active` at version `1`. This is not a replacement for the Admin draft/publish workflow; it exists so Unity and Game API development can read a published payload before Admin publishing is implemented.

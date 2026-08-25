# Versioned Balance

Published balance versions are immutable. Admin edits happen only in drafts. Publish validates expected revision and checksum, inserts a new version, records activation history, and moves the active pointer atomically.

Rollback never edits a version; it changes the active pointer and appends activation history.

## Draft Creation

Admin draft creation writes only to `balance_config_drafts`.

- OWNER and OPERATOR can create drafts.
- VIEWER cannot create drafts.
- The server canonicalizes the config JSON and computes the checksum.
- Draft creation does not publish, activate, or modify published versions.

## DEV Seed

DEV can be bootstrapped with `packages/db/seeds/dev/0001_balance_config_v1.sql`.

Apply it only to DEV:

```bash
pnpm d1:dev:seed:balance
```

The seed inserts published version `1`, records an activation, and points `balance_config_active` at version `1`. This is not a replacement for the Admin draft/publish workflow; it exists so Unity and Game API development can read a published payload before Admin publishing is implemented.

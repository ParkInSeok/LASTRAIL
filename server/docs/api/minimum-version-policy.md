# Minimum supported app version

The DEV Admin version document at `versions/current.json` stores `version`,
`versionCode`, and `minimumVersionCode`. Codes are decimal integer strings,
compared numerically, in the range 1 through 2147483647.

An app code below `minimumVersionCode` requires an update; equality is supported.
The minimum must not exceed the current deployment's `versionCode`.
Raise it only when compatibility requires dropping older clients, not on every release.
Additive DB/API changes that old clients can ignore do not require a forced app
update by themselves.

Legacy documents without the minimum remain readable and are displayed as unset.
Every new export must explicitly include a valid minimum. A stale client omitting
the field is rejected so it cannot silently remove an existing policy.
Only a DEV OWNER can export this document.

Rollback must use a compatible minimum no higher than the rollback version code.
Lowering the minimum is an explicit operator decision; do not lower it automatically.
This policy is independent of retention: it does not delete old artifacts or data.
Version history and rollback execution are separate work; current export overwrites
the current document and does not provide an archive or database rollback.

Game API `GET /v1/client-config` now consumes the current version document for
launch admission and compares the installed `X-LastRail-App-Version-Code`
against server-owned `minimumVersionCode`. When `CLIENT_ADMISSION_MODE` is
`ENFORCED`, protected Game API `/v1/*` routes run the same check before auth or
gameplay handling. Saving the document is still not the same as a Worker deploy,
content activation approval, or database rollback.
DB-sensitive releases must follow
`docs/cloudflare/release-db-api-compatibility-policy.md`.

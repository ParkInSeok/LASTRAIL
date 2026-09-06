# Release DB and API compatibility policy

This policy is the launch rule for app, API, database, and content release
compatibility. It separates four version axes:

- App build: the installed Unity app `versionCode` and display version.
- Server code: Cloudflare Worker deployment versions.
- Database state: D1 migrations and durable data.
- Content release: immutable Addressables/catalog manifests and active pointers.

Server code releases also carry a LAST RAIL server version such as
`2026.09.06.1`. This is separate from Unity app versions and Cloudflare's
runtime deployment id.

## Confirmed Policy

Compatibility-breaking DB-changing updates are protected by the minimum
supported app version policy. Additive DB changes that old clients and old
Workers can ignore do not require a forced app update by themselves.

API-only updates must be designed to remain rollbackable whenever possible.

D1 migrations are treated as forward-moving operational changes, not ordinary
rollback targets. If a DB migration creates an issue, the default recovery path
is a new forward migration, correction record, or audited repair record. Do not
offer an Admin "rollback DB" action in the launch surface.

Cloudflare Worker code rollback remains available only when the database shape
and data semantics are still compatible with the previous Worker version.

Do not deploy a Worker without an explicit, unique server version. Reusing a
server version for a later deployment is forbidden because support, Admin
evidence, and ops error logs must be able to identify the exact runtime.

Before a deploy, preserve the verified source with a release branch whose name
includes the server version, for example
`release/YYYY-MM-DD-2026.09.06.1`. Create immutable service-specific deploy tags
for each Worker, for example `game-api-2026.09.06.1` and
`admin-api-2026.09.06.1`. Branches and tags preserve source state; they are not
database backups.

## Migration Classes

Compatible migrations may ship before a minimum app version raise:

- add a new table;
- add a nullable column;
- add an index;
- add a read-only table used only by newer code;
- add rows or metadata that older code ignores safely.

Compatibility-breaking migrations require an enforced minimum app version gate
before they become active:

- remove or rename a table or column;
- make a nullable field required for an existing route;
- change the meaning, enum domain, or validation semantics of existing data;
- require a client request field that old apps cannot send;
- stop returning a field that old apps require.

Destructive cleanup should be a later release after old app versions are blocked
by an actual server-side admission gate and release verification confirms the
cleanup is safe. A stored `minimumVersionCode` document alone is not enough.

## Deploy Order

Use this order for DB-sensitive changes:

1. Add backward-compatible schema/data first.
2. Choose a new unique server version and verify that it has not been used for
   the target environment/service pair.
3. Run local regression, config diff, migration compatibility review, and
   Wrangler dry-run.
4. Create the versioned release branch and service deploy tags only after the
   source is clean and verified.
5. Deploy server code that can read old and new shapes.
6. Release the new Unity app through the approved app distribution flow.
7. Activate or raise `minimumVersionCode` only after the new app is available
   and the Game API admission check consumes the policy.
8. Drain or tolerate existing sessions, cached offline flows, and retry traffic
   from old clients during the compatibility window.
9. Apply compatibility-breaking behavior only after the minimum app gate is
   active and verification confirms old clients cannot reach the incompatible
   contract path.
10. Do destructive cleanup in a separate explicitly approved migration.

If any step fails, rollback Worker code only when the current D1 state remains
compatible with the target rollback version. Otherwise, roll forward. Before a
Worker rollback, verify that the rollback target can safely read current schema,
current enum values, current idempotency records, current ledger records, and
current content/version pointers.

The actual Cloudflare Worker deployment id remains runtime truth after deploy.
The LAST RAIL server version is the human/support release identity that links
source, tests, deploy tags, smoke results, and Admin evidence. Store only
sanitized deployment metadata; never store secrets, raw auth headers, raw
request bodies, Google tokens, RNG material, or raw probability/weight tables.

## API Change Policy

API changes should remain rollbackable by default:

- Add fields instead of replacing fields.
- Make new response fields nullable or optional first.
- Keep existing field names, types, and meanings stable.
- Add new endpoints rather than changing old endpoint behavior when possible.
- Keep compatibility windows for old clients until the minimum app gate is
  active.

Breaking API changes require the same enforced minimum app version protection as
compatibility-breaking DB changes. Startup update prompts are useful UX, but
they do not replace server-side admission for direct API calls, stale sessions,
offline retry, or tampered client payloads.

## Admin Version Management Boundary

The Admin version-management surface may manage:

- current app display version and `versionCode`;
- `minimumVersionCode` policy;
- approved content release manifests and active content pointers;
- compatibility/readiness status for app, server, DB, and content axes.

It must not:

- run D1 rollback;
- delete old content as a side effect of a minimum version raise;
- treat a version document save as Worker deploy success;
- treat a version document save as content pointer activation unless a separate
  active-pointer operation exists;
- lower `minimumVersionCode` automatically during rollback.

Content rollback means moving an active content pointer to a previously approved
compatible manifest. Server Worker rollback uses Cloudflare Worker version
rollback. Database recovery uses forward migrations or audited corrections.

Admin may show server deployment history and current deployment pointers as
read-only evidence. A successful deploy whose deployment-history write fails
must be marked degraded until the actual Worker id and server history pointer
are reconciled. It must not trigger an automatic second deploy or a D1 rollback.

## Server Deployment Metadata

Server deployment metadata should be written as immutable history records plus a
small current pointer, separate from the app/content version document used by
`GET /v1/client-config`.

Candidate R2 keys:

- `server-deployments/<env>/<service>/<serverVersion>.json`
- `server-deployments/<env>/<service>/current.json`

The immutable record should contain only sanitized fields:

- `releaseId`
- `environment`
- `service`
- `serverVersion`
- verified `commitSha`
- versioned `releaseBranch`
- immutable service deploy tag
- Cloudflare Worker deployment id and previous known-good deployment id
- DB compatibility class and `minimumVersionCode` when required
- linked content `releaseId`/`contentVersion`, not the full manifest body
- local regression, dry-run, remote smoke, and current-pointer status

The current pointer may move only after deploy and smoke verification.
Successful deploys are written to immutable deployment history. Failed deploy or
smoke attempts are written to separate sanitized failure-attempt records and do
not become current. If deploy succeeds and R2 current-pointer write fails, the
runtime remains live and the record must show a degraded evidence state until
manually reconciled through the approved release process.

The local implementation exposes a deployment-history helper that writes the
immutable history object with an R2 conditional `If-None-Match: *` precondition
and moves the current pointer with an `If-Match` / `If-None-Match` conditional
write. A duplicate write with the same sanitized evidence is treated as an
idempotent replay. A different object at the immutable history key or a stale
current-pointer etag is a degraded evidence conflict, not a trigger for another
deploy or a D1 rollback.

The helper is covered with a local fake R2-compatible store. Real R2 writes are
still a deploy-operation step and require explicit environment approval.

The local release-flow model also verifies ordering with fake deploy and smoke
executors: immutable success history and the current pointer are written only
after preflight accepts, deploy succeeds, and remote smoke passes. Deploy or
smoke failure writes a sanitized failure-attempt record under:

- `server-deployments/<env>/<service>/<serverVersion>/failures/<attemptId>.json`

Failure attempts do not write the success history key and do not move the
current pointer. A pointer write conflict after successful deploy/smoke is
degraded evidence that must be reconciled through the approved release process;
it must not trigger automatic redeploy or D1 rollback.

## Game API Admission

Game API `GET /v1/client-config` evaluates an app build code supplied by the
client in `X-LastRail-App-Version-Code` against the server-owned
`minimumVersionCode`. The app display version string is for humans and must not
be the authoritative comparison key.

The admission check is server-owned and returns explicit fail-closed states when
version policy or approved compatible content release metadata is missing. It
must not trust a display version string, R2 folder name, or Unity-only update
prompt as the only guard.

Compatibility-breaking migrations still require a release plan that verifies
the current version document, available app build, content release pointer, and
rollback/roll-forward path before activation.

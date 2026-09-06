# Release DB and API compatibility policy

This policy is the launch rule for app, API, database, and content release
compatibility. It separates four version axes:

- App build: the installed Unity app `versionCode` and display version.
- Server code: Cloudflare Worker deployment versions.
- Database state: D1 migrations and durable data.
- Content release: immutable Addressables/catalog manifests and active pointers.

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
2. Deploy server code that can read old and new shapes.
3. Release the new Unity app through the approved app distribution flow.
4. Activate or raise `minimumVersionCode` only after the new app is available
   and the Game API admission check consumes the policy.
5. Drain or tolerate existing sessions, cached offline flows, and retry traffic
   from old clients during the compatibility window.
6. Apply compatibility-breaking behavior only after the minimum app gate is
   active and verification confirms old clients cannot reach the incompatible
   contract path.
7. Do destructive cleanup in a separate explicitly approved migration.

If any step fails, rollback Worker code only when the current D1 state remains
compatible with the target rollback version. Otherwise, roll forward. Before a
Worker rollback, verify that the rollback target can safely read current schema,
current enum values, current idempotency records, current ledger records, and
current content/version pointers.

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

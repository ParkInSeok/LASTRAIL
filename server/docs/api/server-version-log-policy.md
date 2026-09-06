# Server version in error logs

serverVersion identifies the executing service deployment, not an app version,
minimum app version, balance version or the Admin UI build.
For Workers use CF_VERSION_METADATA.id, authored by Cloudflare at deploy time.
The tuple environment + service + serverVersion identifies the runtime release.
A code rollback creates another deployment; old logs retain their original IDs.

The log writer supplies the runtime ID explicitly to the shared envelope builder.
Request headers, bodies and caller-provided context must not override it.
Store it in sanitized_context_json.serverVersion; expose only the scalar value to Admin.
No DB migration or backfill is needed. Legacy/missing metadata is null (UI: 미기록).
Exact version filtering occurs before pagination with bound SQL parameters.

DEV Game API currently owns the active D1 log writer and receives the metadata binding.
Other producers must adopt the same binding when their log collection is connected.
PRD configuration changes require the existing release approval process.
This policy is diagnostic only and does not enforce API/client compatibility.

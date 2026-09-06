# Server version in error logs

`serverVersion` in launch release planning identifies the LAST RAIL server
release version, for example `2026.09.06.1`. It is not a Unity app version,
minimum app version, balance version, content version, or Admin UI build.

For Cloudflare Workers, `CF_VERSION_METADATA.id` remains the actual runtime
deployment identity authored by Cloudflare at deploy time. The release evidence
record maps the LAST RAIL server version to the Worker deployment id. A code
rollback creates another deployment id; old logs retain the identity they were
written with.

The log writer supplies the runtime ID explicitly to the shared envelope builder.
Request headers, bodies and caller-provided context must not override it.
Store only scalar deployment identity in sanitized context; expose it to Admin as
read-only evidence. Do not store release metadata, branch names, deploy tags, or
runtime ids in a way that implies DB backup or DB rollback capability.
No DB migration or backfill is needed. Legacy/missing metadata is null (UI: 미기록).
Exact version filtering occurs before pagination with bound SQL parameters.

DEV Game API currently owns the active D1 log writer and receives the metadata binding.
Other producers must adopt the same binding when their log collection is connected.
PRD configuration changes require the existing release approval process.
This policy is diagnostic only and does not enforce API/client compatibility.
Compatibility enforcement remains the `minimumVersionCode`/client admission
policy.

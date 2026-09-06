# Client Admission and Content Release Contract

Status: local launch contract. No remote R2 writes, PRD secrets, DNS, or
production activation are part of this slice.

`GET /v1/client-config` is the Game API admission/readiness entrypoint for the
installed Unity app. The same admission check is also applied to protected
`/v1/*` Game API routes when `CLIENT_ADMISSION_MODE=ENFORCED`, so a stale client
cannot skip client-config and call auth, run, gacha, reward, or profile routes
directly.

The client sends its installed build metadata in headers:

- `X-LastRail-App-Version`: optional human display version.
- `X-LastRail-App-Version-Code`: decimal Android/Unity version code.
- `X-LastRail-App-Platform`: optional platform label such as `android`.
- `X-LastRail-App-Channel`: optional release channel such as `internal`.

The server reads the current version document from the configured version R2
bucket and compares the installed code with server-owned
`minimumVersionCode`. Frontend-only checks are not authoritative.

The same version document may include a single approved content release pointer.
The Game API returns it only when the content release is approved and compatible
with the installed app version code. The pointer is read-only metadata for
Addressables/catalog bootstrap; it is not a DB rollback mechanism.

Missing version policy or missing approved compatible content is explicit
fail-closed configuration state. Protected route failures use the same scrubbed
response shape and return `400` for missing app version, `426` for update
required, or `503` for server policy/content configuration failures. The
response must not include R2 credentials, secrets, raw deployment internals, or
unpublished manifest bodies.

Allowed exceptions:

- `GET /status`
- `GET /v1/client-config`
- password recovery verification/reset routes
- frontend diagnostic `client-errors` routes

Recommended response fields:

- `accepted`
- `clientAdmission.status`
- `clientAdmission.installedVersionCode`
- `clientAdmission.minimumVersionCode`
- `clientAdmission.updateRequired`
- `contentRelease.status`
- `contentRelease.releaseId`
- `contentRelease.contentVersion`
- `contentRelease.catalogUrl`
- `contentRelease.catalogHash`
- `contentRelease.compatibility`
- `errorCode`
- `message`

Status values:

- `ACCEPTED`
- `APP_VERSION_REQUIRED`
- `UPDATE_REQUIRED`
- `VERSION_POLICY_UNCONFIGURED`
- `CONTENT_RELEASE_UNCONFIGURED`
- `CONTENT_RELEASE_NOT_APPROVED`
- `CONTENT_RELEASE_INCOMPATIBLE`

Admin version management may save draft/current documents, but save success does
not equal production activation. DB schema/data rollback remains a roll-forward
or audited correction operation, not an Admin version rollback.

Legacy Admin minimum-version exports that omit `contentRelease` must preserve
the existing approved content pointer. Future content pointer writes need an
explicit precondition/revision contract before concurrent pointer updates are
enabled.

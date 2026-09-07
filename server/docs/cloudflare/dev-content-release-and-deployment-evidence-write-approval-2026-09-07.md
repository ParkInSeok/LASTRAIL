# DEV content release and deployment evidence write approval plan - 2026-09-07

Status: approval plan only. This document does not authorize or execute R2
writes, Worker redeploys, secrets, PRD operations, videos, Rail Choice Events,
World Raid scope, or policy relaxation.

## Current verified DEV state

DEV Worker deploys already happened from the preserved source snapshot below:

- Source commit: `57d2a109134f99d2feb577cd4db23aa3dd7e08fd`
- Human server version: `2026.09.07.1`
- Release branch: `release/2026-09-07-dev-2026.09.07.1`
- Game API tag: `game-api-2026.09.07.1`
- Admin API tag: `admin-api-2026.09.07.1`
- Game API Worker route: `https://dev-api.lastrail.app`
- Admin API Worker route: `https://dev-admin.lastrail.app`
- Game API deployment id: `d465732a-7513-44d9-8644-a73531bbb705`
- Previous Game API deployment id: `a32552e5-6346-46d6-8fb6-93b511f26278`
- Game API Worker version id observed at deploy time:
  `733d149d-041f-4cea-8488-20f3ac00875e`
- Admin API deployment id: `2fc17463-738e-4262-a7b5-2aef2ead1610`
- Previous Admin API deployment id: `04b3f6ab-1376-4926-8860-7b034c6f3b27`
- Admin API Worker version id observed at deploy time:
  `1817e669-215a-4fe1-b2c0-f0dfe922e66d`

DEV D1 target:

- Database: `lastrail-core-dev`
- Database id: `14c31f63-3b9d-46b9-bd4a-8e093aaba436`
- Applied migration range: `0001-0020`
- Post-apply checks observed: `PRAGMA quick_check` returned `ok`;
  `PRAGMA foreign_key_check` returned no rows.

Current remote R2 version document:

- Bucket: `lastrail-versions-dev`
- Key: `versions/current.json`
- Current value:

```json
{
  "version": "0.1.0",
  "versionCode": "1",
  "minimumVersionCode": "1",
  "contentRelease": null
}
```

Current remote R2 deployment evidence:

- `server-deployments/dev/game-api/current.json`: missing
- `server-deployments/dev/admin-api/current.json`: missing
- The version document and server deployment evidence are separate axes.

## Why DEV currently blocks client boot

The deployed Game API has `CLIENT_ADMISSION_MODE=ENFORCED`. With the current
`versions/current.json` value above, `GET /v1/client-config` correctly returns a
fail-closed `content_release_unconfigured` shape. This is not a Worker deploy
failure and not a D1 migration regression. It means the DEV content release
pointer is not approved/configured yet.

## Content release pointer approval item

The minimum content write is a single R2 object update:

- Bucket: `lastrail-versions-dev`
- Key: `versions/current.json`
- Preserve existing policy fields:
  - `version: "0.1.0"`
  - `versionCode: "1"`
  - `minimumVersionCode: "1"`
- Add only `contentRelease` after the actual Unity Android candidate is
  approved and hosted at an HTTPS catalog URL.

Candidate payload shape:

```json
{
  "version": "0.1.0",
  "versionCode": "1",
  "minimumVersionCode": "1",
  "contentRelease": {
    "status": "APPROVED",
    "releaseId": "<UNITY_APPROVED_CONTENT_RELEASE_ID>",
    "contentVersion": "<UNITY_APPROVED_CONTENT_VERSION>",
    "catalogUrl": "https://<approved-host>/<approved-path>/catalog_0.1.0.bin",
    "catalogHash": "sha256:<64 lowercase hex sha256 of hosted catalog>",
    "minimumVersionCode": "1"
  }
}
```

Do not write this object until all missing values are known:

- `UNITY_APPROVED_CONTENT_RELEASE_ID`
- `UNITY_APPROVED_CONTENT_VERSION`
- final hosted HTTPS `catalogUrl`
- `catalogHash` for that exact hosted catalog object

The local Unity Addressables baselines currently prove build/archive
repeatability, but they do not yet provide a server-usable hosted candidate:

- `qa_20260906_145829_45697f95_A`: 20 files, 153,620,801 bytes
- `qa_20260906_145829_45697f95_B`: 20 files, 153,620,801 bytes
- `qa_20260906_145829_45697f95_updateA`: 16 files, 85,574,000 bytes
- Baseline A catalog object in local archive:
  - `remote/catalog_0.1.0.bin`
  - SHA-256:
    `0c564b6b30c3ec50350c00cebb633b0f7075a2589c8f4201ae6a43fd7b196871`

That local catalog hash must not be promoted to `versions/current.json` until
the same object is uploaded to the approved DEV content host and the resulting
HTTPS URL is validated.

### Required content pointer guard

`saveVersion(...)` currently writes `versions/current.json` without an R2
conditional compare-and-swap guard. For this launch operation, use one of these
approved-safe paths:

1. Preferred: add or use a CAS-capable version-pointer writer that reads the
   current object etag and writes with `If-Match`. Abort if the current object
   changed.
2. Acceptable only with explicit approval: a guarded one-shot operation that
   reads `versions/current.json` immediately before writing and aborts unless
   the object still exactly matches the current value shown above.

Failure policy:

- If the precondition fails, do not write anything.
- If the R2 write fails, keep the current version pointer unchanged.
- Do not lower `minimumVersionCode`.
- Do not change server deployment evidence as a side effect of this content
  pointer write.

### Content pointer verification

After the pointer write, run only scrubbed DEV checks:

- `GET https://dev-api.lastrail.app/v1/client-config`
- Expected: `accepted=true`, `contentRelease.status=APPROVED_COMPATIBLE`, and
  the approved `releaseId`, `contentVersion`, and `catalogHash`.
- The response must not expose secrets, tokens, raw auth headers, raw request
  bodies, raw RNG, probability tables, or full bundle manifests.

## Deployment evidence approval item

Deployment evidence should be written to R2 only after the relevant deploy and
remote smoke state is honestly known.

Because current Game API smoke is blocked by `content_release_unconfigured`,
success history/current pointers should not be written yet. A blocked smoke
attempt may be written now if explicitly approved. Success evidence can be
written after the content pointer is approved and launch smoke passes.

### Optional blocked-smoke evidence now

These failure-attempt records do not move the current pointer:

```text
server-deployments/dev/game-api/2026.09.07.1/failures/attempt-content-release-unconfigured-20260907.json
server-deployments/dev/admin-api/2026.09.07.1/failures/attempt-admin-access-credential-blocked-20260907.json
```

Game API blocked-smoke record:

```json
{
  "releaseId": "act1-dev-2026-09-07",
  "environment": "dev",
  "service": "game-api",
  "serverVersion": "2026.09.07.1",
  "commitSha": "57d2a109134f99d2feb577cd4db23aa3dd7e08fd",
  "releaseBranch": "release/2026-09-07-dev-2026.09.07.1",
  "deploymentTag": "game-api-2026.09.07.1",
  "failureStage": "SMOKE",
  "errorCode": "deployment_smoke_blocked",
  "cloudflareDeploymentId": "d465732a-7513-44d9-8644-a73531bbb705",
  "database": {
    "compatibility": "ADDITIVE_COMPATIBLE",
    "migrationRange": "0001-0020"
  },
  "api": {
    "compatibility": "ROLLBACK_COMPATIBLE",
    "rollbackVerified": true
  },
  "contentRelease": null,
  "verification": {
    "localRegression": "PASSED",
    "dryRun": "PASSED",
    "remoteSmoke": "BLOCKED"
  },
  "createdAt": "<ISO_TIMESTAMP_AT_EVIDENCE_WRITE>"
}
```

Admin API blocked-smoke record:

```json
{
  "releaseId": "act1-dev-2026-09-07",
  "environment": "dev",
  "service": "admin-api",
  "serverVersion": "2026.09.07.1",
  "commitSha": "57d2a109134f99d2feb577cd4db23aa3dd7e08fd",
  "releaseBranch": "release/2026-09-07-dev-2026.09.07.1",
  "deploymentTag": "admin-api-2026.09.07.1",
  "failureStage": "SMOKE",
  "errorCode": "deployment_smoke_blocked",
  "cloudflareDeploymentId": "2fc17463-738e-4262-a7b5-2aef2ead1610",
  "database": {
    "compatibility": "ADDITIVE_COMPATIBLE",
    "migrationRange": "0001-0020"
  },
  "api": {
    "compatibility": "ROLLBACK_COMPATIBLE",
    "rollbackVerified": true
  },
  "contentRelease": null,
  "verification": {
    "localRegression": "PASSED",
    "dryRun": "PASSED",
    "remoteSmoke": "BLOCKED"
  },
  "createdAt": "<ISO_TIMESTAMP_AT_EVIDENCE_WRITE>"
}
```

Write rule:

- Use `If-None-Match: *` for each failure key.
- If the key already exists with identical sanitized content, treat it as
  idempotent evidence.
- If the key exists with different content, stop with an evidence conflict.
- Do not write or move `server-deployments/dev/<service>/current.json`.

### Success evidence after content pointer and smoke pass

After content pointer approval and passing remote smoke, write immutable history
plus current pointer for each service.

Game API success keys:

```text
server-deployments/dev/game-api/2026.09.07.1.json
server-deployments/dev/game-api/current.json
```

Game API success history payload:

```json
{
  "releaseId": "act1-dev-2026-09-07",
  "environment": "dev",
  "service": "game-api",
  "serverVersion": "2026.09.07.1",
  "commitSha": "57d2a109134f99d2feb577cd4db23aa3dd7e08fd",
  "releaseBranch": "release/2026-09-07-dev-2026.09.07.1",
  "deploymentTag": "game-api-2026.09.07.1",
  "cloudflareDeploymentId": "d465732a-7513-44d9-8644-a73531bbb705",
  "previousDeploymentId": "a32552e5-6346-46d6-8fb6-93b511f26278",
  "database": {
    "compatibility": "ADDITIVE_COMPATIBLE",
    "migrationRange": "0001-0020"
  },
  "api": {
    "compatibility": "ROLLBACK_COMPATIBLE",
    "rollbackVerified": true
  },
  "contentRelease": {
    "releaseId": "<UNITY_APPROVED_CONTENT_RELEASE_ID>",
    "contentVersion": "<UNITY_APPROVED_CONTENT_VERSION>"
  },
  "verification": {
    "localRegression": "PASSED",
    "dryRun": "PASSED",
    "remoteSmoke": "PASSED"
  },
  "pointerStatus": "CURRENT",
  "createdAt": "<ISO_TIMESTAMP_AT_EVIDENCE_WRITE>"
}
```

Game API current pointer payload:

```json
{
  "environment": "dev",
  "service": "game-api",
  "serverVersion": "2026.09.07.1",
  "historyKey": "server-deployments/dev/game-api/2026.09.07.1.json",
  "releaseId": "act1-dev-2026-09-07",
  "cloudflareDeploymentId": "d465732a-7513-44d9-8644-a73531bbb705",
  "updatedAt": "<SAME_ISO_TIMESTAMP_AS_HISTORY>"
}
```

Admin API success keys:

```text
server-deployments/dev/admin-api/2026.09.07.1.json
server-deployments/dev/admin-api/current.json
```

Admin API success history payload:

```json
{
  "releaseId": "act1-dev-2026-09-07",
  "environment": "dev",
  "service": "admin-api",
  "serverVersion": "2026.09.07.1",
  "commitSha": "57d2a109134f99d2feb577cd4db23aa3dd7e08fd",
  "releaseBranch": "release/2026-09-07-dev-2026.09.07.1",
  "deploymentTag": "admin-api-2026.09.07.1",
  "cloudflareDeploymentId": "2fc17463-738e-4262-a7b5-2aef2ead1610",
  "previousDeploymentId": "04b3f6ab-1376-4926-8860-7b034c6f3b27",
  "database": {
    "compatibility": "ADDITIVE_COMPATIBLE",
    "migrationRange": "0001-0020"
  },
  "api": {
    "compatibility": "ROLLBACK_COMPATIBLE",
    "rollbackVerified": true
  },
  "contentRelease": {
    "releaseId": "<UNITY_APPROVED_CONTENT_RELEASE_ID>",
    "contentVersion": "<UNITY_APPROVED_CONTENT_VERSION>"
  },
  "verification": {
    "localRegression": "PASSED",
    "dryRun": "PASSED",
    "remoteSmoke": "PASSED"
  },
  "pointerStatus": "CURRENT",
  "createdAt": "<ISO_TIMESTAMP_AT_EVIDENCE_WRITE>"
}
```

Admin API current pointer payload:

```json
{
  "environment": "dev",
  "service": "admin-api",
  "serverVersion": "2026.09.07.1",
  "historyKey": "server-deployments/dev/admin-api/2026.09.07.1.json",
  "releaseId": "act1-dev-2026-09-07",
  "cloudflareDeploymentId": "2fc17463-738e-4262-a7b5-2aef2ead1610",
  "updatedAt": "<SAME_ISO_TIMESTAMP_AS_HISTORY>"
}
```

Success write guards:

- History object: write with `If-None-Match: *`.
- Current pointer:
  - current object absent: write with `If-None-Match: *`;
  - current object present: read etag first and write with `If-Match: <etag>`.
- Duplicate history with identical content is idempotent.
- Duplicate history with different content is
  `deployment_history_immutable_conflict`.
- Stale current pointer etag is `deployment_current_pointer_conflict`.
- Any conflict leaves the current pointer unchanged.

## Required approval text

Use this exact minimum approval when Unity content values are ready:

```text
Approve DEV R2 writes only for bucket lastrail-versions-dev: update versions/current.json by preserving version=0.1.0, versionCode=1, minimumVersionCode=1 and adding the approved Unity Android contentRelease fields; write server deployment success evidence for source 57d2a109134f99d2feb577cd4db23aa3dd7e08fd, serverVersion 2026.09.07.1, Game API deployment d465732a-7513-44d9-8644-a73531bbb705, Admin API deployment 2fc17463-738e-4262-a7b5-2aef2ead1610 under server-deployments/dev/{game-api,admin-api}/2026.09.07.1.json and current.json only after scrubbed DEV remote smoke passes. Use conditional writes/CAS; leave current pointers unchanged on conflicts or failed smoke. No Worker redeploy, no D1 migration, no secrets, no PRD, no Rail Choice Events, no World Raid.
```

If the user wants to record the current blocked smoke state before content is
ready, use this separate approval:

```text
Approve DEV R2 blocked-smoke evidence writes only for bucket lastrail-versions-dev under server-deployments/dev/{game-api,admin-api}/2026.09.07.1/failures/*.json with sanitized deployment_smoke_blocked records. Do not write success history/current pointers, do not update versions/current.json, no redeploy, no D1 migration, no secrets, no PRD.
```

## Follow-up owners

- Unity must provide the approved hosted Android content candidate values:
  `releaseId`, `contentVersion`, final HTTPS `catalogUrl`, and `catalogHash`.
- Server can execute only the approved R2 pointer/evidence write after those
  values exist.
- Admin should treat missing server deployment current/history as evidence
  pending, not deploy failure, until the approved R2 evidence write is done.


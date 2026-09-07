# DEV Release Approval Plan - 2026-09-07

Status: approval candidate only. This document does not authorize or execute
remote deploys, D1 migrations, R2 writes, secrets, domain changes, Access
changes, real mail sends, video/store operations, PRD operations, Rail Choice
Events, or World Raid scope.

## Source Snapshot

Server source snapshot proposed for DEV deploy:

- Repository HEAD: `57d2a10`
- Server commits included:
  - `f463154 feat(ops): verify deployment release flow`
  - `57d2a10 fix(ops): record deployment failure attempts`
- Server path status at plan time: clean.
- Whole repository status at plan time: dirty outside `server/`, including
  Admin Web and Unity work. If release branch/tag operations require a clean
  whole repository, use a clean release worktree or wait until those unrelated
  changes are committed/shelved by their owners.
- Do not include uncommitted Admin/Unity files in the server deploy snapshot.

## Recommended DEV Scope

Deploy only these DEV API targets after explicit approval:

| Service | Worker | Route | Proposed serverVersion | Proposed tag |
| --- | --- | --- | --- | --- |
| Game API | `lastrail-game-api-dev` | `dev-api.lastrail.app` | `2026.09.07.1` | `game-api-2026.09.07.1` |
| Admin API | `lastrail-admin-api-dev` | `dev-admin.lastrail.app` | `2026.09.07.1` | `admin-api-2026.09.07.1` |

Async Worker is not in the recommended first DEV API deploy unless queue-backed
Admin jobs must be tested in the same approval. If included later:

| Service | Worker | Proposed serverVersion | Proposed tag |
| --- | --- | --- | --- |
| Async Worker | `lastrail-async-worker-dev` | `2026.09.07.1` | `async-worker-2026.09.07.1` |

Recommended release branch for a clean worktree:

```text
release/2026-09-07-dev-2026.09.07.1
```

## Known DEV Configuration From Local Source

Game API DEV config:

- Worker: `lastrail-game-api-dev`
- Route: `dev-api.lastrail.app`
- D1: `lastrail-core-dev`
- D1 id: `14c31f63-3b9d-46b9-bd4a-8e093aaba436`
- D1 migrations dir: `server/packages/db/migrations`
- R2 binding: `VERSION_BUCKET`
- R2 bucket: `lastrail-versions-dev`
- `CLIENT_ADMISSION_MODE=ENFORCED`
- `CF_VERSION_METADATA` binding enabled

Admin API DEV config:

- Worker: `lastrail-admin-api-dev`
- Route: `dev-admin.lastrail.app`
- D1: `lastrail-core-dev`
- D1 id: `14c31f63-3b9d-46b9-bd4a-8e093aaba436`
- R2 binding: `VERSION_BUCKET`
- R2 bucket: `lastrail-versions-dev`
- Queue producer: `lastrail-admin-jobs-dev`
- `ADMIN_AUTH_MODE=ACCESS_JWT_REQUIRED`
- DEV Access issuer/AUD are configured in source, but live Access policy and
  usable admin credentials remain runtime checks.

Async Worker DEV config:

- Worker: `lastrail-async-worker-dev`
- D1: `lastrail-core-dev`
- D1 id: `14c31f63-3b9d-46b9-bd4a-8e093aaba436`
- Queue consumer: `lastrail-admin-jobs-dev`

## Remote State Classification

Local source can identify intended targets, but cannot prove current remote
state without approved remote read commands.

| Area | Current status |
| --- | --- |
| Last deployed Game API Worker id | Remote read required; currently unconfirmed in this plan |
| Last deployed Admin API Worker id | Remote read required; currently unconfirmed in this plan |
| DEV D1 applied migrations | Remote migration list required; currently unconfirmed in this plan |
| DEV R2 deployment evidence current/history | Remote R2 read required; currently unconfirmed in this plan |
| DEV Worker secret presence | `wrangler secret list --env dev` required; values must not be printed |
| DEV Access usable credentials | User/browser/service credential dependent; currently unconfirmed |

## Migration Plan

Local migrations present at source snapshot:

```text
0001_initial.sql
0002_economy.sql
0003_progression.sql
0004_operation.sql
0005_run.sql
0006_integrity.sql
0007_compensation.sql
0008_admin.sql
0009_ads.sql
0010_indexes.sql
0011_suspicious_player_reviews.sql
0012_run_reward_previews.sql
0013_gacha_pity_result_history.sql
0014_story_progress.sql
0015_account_offline_reward_state.sql
0016_ops_error_logs.sql
0017_account_login_bonus_claims.sql
0018_profile_display_name_policy.sql
0019_auth_password_recovery_email.sql
0020_support_tickets.sql
```

Compatibility classification for the current DEV release candidate:

- `0011` to `0020` are expected to be additive/read-model or new-table policy
  changes from the current launch line.
- A remote migration list must be checked before applying anything.
- Apply only pending migrations explicitly approved for DEV.
- If `0018` to `0020` are not already approved/applied remotely, include them
  as a separate explicit DEV migration approval item before Worker deploy.
- No D1 rollback command is part of this plan. DB recovery uses forward
  migration or audited correction under a separate approval.

Safe remote read commands for approval:

```bash
pnpm exec wrangler d1 migrations list lastrail-core-dev --remote --config apps/game-api/wrangler.jsonc
pnpm exec wrangler d1 execute lastrail-core-dev --remote --command "PRAGMA quick_check;" --config apps/game-api/wrangler.jsonc
pnpm exec wrangler d1 execute lastrail-core-dev --remote --command "PRAGMA foreign_key_check;" --config apps/game-api/wrangler.jsonc
```

Migration apply command candidate, only after pending list is reviewed and DEV
migration approval is explicit:

```bash
pnpm exec wrangler d1 migrations apply lastrail-core-dev --remote --config apps/game-api/wrangler.jsonc
```

## Secret And Binding Presence Checks

Secret checks must confirm names only and must not print values.

Game API DEV required or conditional names:

- `AUTH_SIGNING_KEY_CURRENT`
- `GOOGLE_AUTH_AUDIENCES`: not a DEV blocker; DEV real Google OAuth smoke is
  intentionally deferred unless PRD/app-registration values are approved.
- Password recovery mail, only if DEV recovery email is expected to send real
  Gmail messages:
  - `RECOVERY_EMAIL_PROVIDER`
  - `GMAIL_RECOVERY_SENDER_EMAIL`
  - `GMAIL_RECOVERY_OAUTH_CLIENT_ID`
  - `GMAIL_RECOVERY_OAUTH_CLIENT_SECRET`
  - `GMAIL_RECOVERY_OAUTH_REFRESH_TOKEN`

Admin API DEV required runtime config:

- `ADMIN_ACCESS_ISSUER`
- `ADMIN_ACCESS_AUD`
- `ADMIN_DELEGATED_ACCESS_ISSUER`
- `ADMIN_DELEGATED_ACCESS_AUD`

Presence-check command candidates:

```bash
pnpm --filter @lastrail/game-api wrangler secret list --env dev
pnpm --filter @lastrail/admin-api wrangler secret list --env dev
pnpm --filter @lastrail/async-worker wrangler secret list --env dev
```

Do not inject, rotate, delete, or print secrets as part of this approval plan.

## Admission And Version Policy

Game API DEV has `CLIENT_ADMISSION_MODE=ENFORCED`.

Before full smoke:

- Confirm `VERSION_BUCKET=lastrail-versions-dev` is bound.
- Confirm `GET /v1/client-config` returns an accepted or intentional
  fail-closed shape.
- If current version policy is missing from DEV R2, that is an approval item for
  a version document/current-pointer operation, not a Worker deploy workaround.
- Do not lower `minimumVersionCode` automatically during rollback.

## Local Preflight Commands

Run from `server/` before any remote action:

```bash
pnpm test
pnpm typecheck
pnpm lint
pnpm d1:local:verify
pnpm deploy:server:dry-run -- --plan <dev-plan.json> --service game-api --env dev --history <used-history.json>
pnpm deploy:server:dry-run -- --plan <dev-plan.json> --service admin-api --env dev --history <used-history.json>
```

If Async Worker is included:

```bash
pnpm deploy:server:dry-run -- --plan <dev-plan.json> --service async-worker --env dev --history <used-history.json>
```

The dry-run wrapper refuses actual deploy mode. It validates target
environment/service, serverVersion reuse, release branch, deploy tags, DB
compatibility, and local verification fields before Wrangler is called.

## Deploy Commands Requiring Approval

Run only after local preflight, DEV migration state, and secret/config presence
are confirmed:

```bash
pnpm --filter @lastrail/game-api wrangler deploy --env dev
pnpm --filter @lastrail/admin-api wrangler deploy --env dev
```

If Async Worker is separately included:

```bash
pnpm --filter @lastrail/async-worker wrangler deploy --env dev
```

No PRD deploy/domain/secret/custom Access policy change is included.

## Smoke Plan And Data Writes

Smoke must scrub Authorization headers, raw tokens, Google ID tokens/full
claims, refresh token hashes, raw request bodies, raw RNG material, probability
tables, full gacha weights, secrets, and R2 credentials.

Read-only smoke:

- Game API `GET /status`
- Game API `GET /v1/client-config`
- Admin API protected read endpoints only if DEV Access/admin credential is
  available:
  - `/v1/admin/me`
  - `/v1/admin/dashboard/launch-summary`
  - `/v1/admin/server-deployments/current?service=game-api`
  - `/v1/admin/server-deployments/history/2026.09.07.1?service=game-api`

Expected read-only deployment-evidence result before R2 evidence write:

- current/history may return `server_deployment_current_not_found` or
  `server_deployment_history_not_found`.
- Treat these as expected before evidence write approval; do not create R2
  objects from Admin read smoke.

Game API write smoke, only with a fresh DEV smoke account/ids:

- guest auth and refresh
- login bonus claim and replay
- operation preview seed for ordinary run
- `POST /v1/runs/start` with `Idempotency-Key`
- ordinary 15-wave finish/replay
- reward preview/claim
- story raid preview/start/finish with stamina refund evidence
- offline preview/claim
- story progress read
- deterministic DEV gacha banner seed, 1-pull, 10-pull, replay, insufficient
  ticket shape
- selected critical ops error write/read only when safe and non-destructive

Known data-write impact of the existing DEV smoke script:

- Creates a fresh guest account/session.
- Writes launch-safe operation preview rows for ordinary and story raid runs.
- Commits run start/finish, reward preview/claim, story progress, login bonus,
  offline claim, stamina/fuel refund, gacha pull batches/results, ledgers, and
  idempotency records for the smoke account.
- Seeds a deterministic `dev_smoke_banner_<suffix>` with a single `op_vex`
  pickup candidate for repeatable gacha smoke. This is not production gacha
  probability evidence.

## R2 Deployment Evidence Approval Item

R2 deployment evidence write is separate from Worker deploy approval unless the
user explicitly includes it.

Success keys:

```text
server-deployments/dev/game-api/2026.09.07.1.json
server-deployments/dev/game-api/current.json
server-deployments/dev/admin-api/2026.09.07.1.json
server-deployments/dev/admin-api/current.json
```

Failure attempt keys:

```text
server-deployments/dev/<service>/2026.09.07.1/failures/<attemptId>.json
```

Rules:

- Write success history/current only after deploy and remote smoke pass.
- Write sanitized failure-attempt records for deploy or smoke failure.
- Never write secrets, raw auth, raw request bodies, Google tokens, RNG
  material, probability/full weights, or R2 credentials.
- R2 write failure after successful deploy is degraded evidence and must not
  trigger automatic redeploy or D1 rollback.

## Rollback Constraints

- Worker rollback is allowed only if current D1 schema/data semantics remain
  compatible with the rollback Worker.
- D1 rollback is not part of Admin version management and is not part of this
  DEV plan.
- Breaking DB/API changes require server-side minimum app version admission
  before incompatible behavior becomes active.
- Additive DB/API changes can deploy without forced app update if old clients
  and old Workers can safely ignore them.
- Content/Addressables rollback is a content pointer decision, not a DB rollback
  or server Worker rollback.

## Approval Boundary To Ask User

Ask for approval using this exact scope before execution:

```text
Approve DEV Game API + Admin API deploy plan for source 57d2a10, serverVersion 2026.09.07.1, branch release/2026-09-07-dev-2026.09.07.1, tags game-api-2026.09.07.1/admin-api-2026.09.07.1; remote read checks for DEV D1 migration state, quick_check, foreign_key_check, Worker secret-name presence; apply only explicitly approved pending DEV D1 migrations; run DEV Worker deploys; run scrubbed DEV remote smoke with fresh test data. R2 deployment evidence write remains separate unless explicitly approved.
```

Separate approval items:

- Include Async Worker DEV deploy.
- Apply pending DEV migrations `0018` to `0020` if remote list shows them
  missing and they were not already explicitly approved.
- Write R2 deployment success/failure evidence under the keys above.
- Send any real recovery email smoke.

Still excluded:

- PRD deploys, PRD secrets, production domain/Access policy changes, store or
  signing operations, Google real OAuth values, Rail Choice Events, World Raid,
  destructive cleanup, DB rollback, manual grants, repair, replay/reprocess, or
  economy correction.

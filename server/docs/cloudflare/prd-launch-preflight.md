# PRD Launch Preflight

This checklist defines the server-side preflight for an Act 1 PRD launch. It is
not a deployment record and does not authorize creating secrets, production
resources, DNS routes, tags, or releases without the concrete inputs below being
present and explicitly confirmed.

## Scope

- DEV Google OAuth smoke is not a launch blocker. DEV may keep returning
  `google_auth_not_configured` for real Google routes.
- Real Google OAuth audience/client id configuration is PRD/app-registration
  scope only.
- Do not invent, hardcode, print, commit, or log OAuth values or Worker secrets.
- Rail Choice Events and World Raid remain outside Act 1 launch scope.

## Release Shape

Before PRD deployment, prepare and verify:

- Release branch: `release/YYYY-MM-DD-act1`.
- Release tag: `act1-launch-YYYY-MM-DD`.
- LAST RAIL server version: a unique calendar version such as
  `2026.09.06.1`. Do not deploy a Worker without one, and do not reuse one for
  the same environment/service pair.
- Service deploy tags: immutable tags that include both service and server
  version, for example `game-api-2026.09.06.1`,
  `admin-api-2026.09.06.1`, and `async-worker-2026.09.06.1` when those
  services are included.
- Final local regression: `pnpm test`, `pnpm typecheck`, `pnpm lint`,
  `pnpm d1:local:verify`.
- Config diff review for `apps/game-api/wrangler.jsonc`,
  `apps/admin-api/wrangler.jsonc`, and `apps/async-worker/wrangler.jsonc`.
- PRD migration plan for `packages/db/migrations/0001` through current HEAD,
  including whether every migration is additive/rollback-compatible or requires
  an enforced `minimumVersionCode` gate.
- Rollback note: keep the previous Worker version id for each deployed Worker;
  rollback uses Wrangler Worker version rollback after confirming no migration
  rollback is needed. D1 schema rollback must be a separate approved plan.
- Source preservation note: release branches and tags preserve the verified
  source. They are not DB backups and do not authorize D1 rollback.
- Server deployment evidence note: the actual Cloudflare Worker deployment id is
  runtime truth. The LAST RAIL server version links that runtime to source,
  tests, smoke results, and Admin evidence.
- Compatibility note: a stored minimum version document or Unity update popup is
  not by itself a safe guard for compatibility-breaking DB/API changes. The Game
  API admission path must consume the policy before incompatible routes rely on
  it.

## Required PRD Inputs

| Area | Required input | Current local status | Blocker |
| --- | --- | --- | --- |
| D1 | `lastrail-core-prd` database id | `__PRD_D1_DATABASE_ID__` placeholder in all PRD Worker configs | Create/confirm PRD D1, then replace placeholder |
| Queue | `lastrail-admin-jobs-prd` queue | Config references name, remote list currently shows only DEV queue | Create/confirm PRD queue before async/admin PRD deploy |
| Game API secret | `AUTH_SIGNING_KEY_CURRENT` | Cannot exist until `lastrail-game-api-prd` Worker exists | Generate/provide PRD signing key and inject as Worker secret after Worker/resource approval |
| Game API config | `GOOGLE_AUTH_AUDIENCES` | Not in PRD vars yet | Provide Google OAuth client id/audience allow-list from app registration |
| Admin Access | `ADMIN_ACCESS_ISSUER`, `ADMIN_ACCESS_AUD` | PRD placeholders | Create/confirm PRD Cloudflare Access app for `admin.lastrail.app` |
| Admin delegated Access | `ADMIN_DELEGATED_ACCESS_ISSUER`, `ADMIN_DELEGATED_ACCESS_AUD` | PRD placeholders | Confirm Pages/Admin delegated Access path or disable delegated path until ready |
| Admin principal | `admin_principals.access_subject` for PRD owner/operator | No PRD D1 yet | Seed only after PRD Access login subject is known |
| Domains | `api.lastrail.app`, `admin.lastrail.app` | Configured as PRD routes, not verified here | Confirm DNS/custom domain approval and Access protection for admin host |

Notes:

- Google server verification uses the Google `sub` as provider subject after ID
  token verification. Email is not the identity key.
- `GOOGLE_AUTH_AUDIENCES` is configuration, not a secret, but it must come from
  the real PRD app registration and must not be guessed.
- No Google client secret is currently required by the Game API ID-token verify
  flow; if the app-registration flow introduces one later, add it as a Worker
  secret and do not expose it in D1, logs, or docs.

## PRD Preflight Commands

Run only after the required inputs above are confirmed.

```bash
pnpm test
pnpm typecheck
pnpm lint
pnpm d1:local:verify
pnpm wrangler:types
pnpm --filter @lastrail/game-api wrangler deploy --env prd --dry-run
pnpm --filter @lastrail/admin-api wrangler deploy --env prd --dry-run
pnpm --filter @lastrail/async-worker wrangler deploy --env prd --dry-run
```

Before actual deploy, validate the server deployment plan with the common
release-policy schema. The plan must include the new server version, clean
source commit, versioned release branch, all service deploy tags, DB
compatibility class, and local regression/dry-run success. This validation is a
local preflight; it does not create branches, tags, remote resources, secrets,
or R2 objects by itself.

The local preflight entrypoint is:

```bash
pnpm preflight:server-deploy -- --plan <plan.json> --service game-api --env prd --history <used-history.json>
```

The supported launch-safe deploy dry-run entrypoint wraps the same mandatory
preflight before invoking Wrangler:

```bash
pnpm deploy:server:dry-run -- --plan <plan.json> --service game-api --env prd --history <used-history.json>
```

Run it once per deployed service before Wrangler dry-run and before actual
deploy. The optional history file is a sanitized list of prior
`environment/service/serverVersion` tuples and is used to reject version reuse.
The dry-run wrapper refuses actual deploy mode until a separate approved release
operation wires that path explicitly. Invalid plans, target mismatches, and
server-version reuse fail before Wrangler is called. The command prints only
scrubbed release identity and never prints secrets, tokens, raw request bodies,
Google tokens, RNG material, raw probability tables, or R2 credentials.

The shared release-flow helper models the approved production order with local
fake executors: preflight, deploy, remote smoke, then immutable success
history/current pointer writes. It writes success history only after deploy
execution and remote smoke both pass. Deploy failure or smoke failure writes a
separate sanitized failure-attempt record and leaves success history/current
untouched; current-pointer conflicts are degraded evidence states, not automatic
redeploys or D1 rollback triggers. Real deploy execution, remote smoke, and R2
history writes still require explicit environment approval.

Remote PRD checks must be value-scrubbed:

```bash
pnpm exec wrangler d1 migrations list lastrail-core-prd --remote --config apps/game-api/wrangler.jsonc
pnpm exec wrangler d1 execute lastrail-core-prd --remote --command "PRAGMA quick_check;" --config apps/game-api/wrangler.jsonc
pnpm exec wrangler d1 execute lastrail-core-prd --remote --command "PRAGMA foreign_key_check;" --config apps/game-api/wrangler.jsonc
pnpm --filter @lastrail/game-api wrangler secret list --env prd
pnpm --filter @lastrail/admin-api wrangler secret list --env prd
pnpm --filter @lastrail/async-worker wrangler secret list --env prd
```

Do not print secret values. Secret list output should be used only to confirm
presence by name.

## Deploy Order Candidate

1. Create/confirm PRD D1 and PRD Queue.
2. Replace PRD config placeholders with the approved PRD resource ids and Access
   issuer/audience values.
3. Regenerate Worker types and run the final local regression.
4. Apply PRD D1 migrations after explicit PRD migration approval. If a migration
   is compatibility-breaking, first confirm new app availability, server-side
   minimum-version admission, old-session/retry tolerance, and rollback target
   compatibility with the current D1 state.
5. Deploy PRD Workers with dry-run first, then actual deploy after approval.
6. Inject/rotate PRD Worker secrets only through Wrangler secret storage.
7. Run scrubbed PRD smoke for public Game API, protected Admin API, and async
   worker queue health where credentials permit.
8. Write immutable sanitized server deployment history and update the current
   pointer only after deploy and smoke pass. If deployment succeeds but the R2
   evidence write fails, mark the release evidence as degraded and reconcile the
   pointer; do not auto-deploy again and do not run D1 rollback.
9. Tag the verified release commit as `act1-launch-YYYY-MM-DD`.

## PRD Smoke Candidate

- `GET /status` and `GET /v1/client-config` on Game API.
- Guest auth and refresh.
- Google sign-in/link with real PRD Google ID token only after app registration
  values are configured.
- Run start/finish/replay, story raid failure stamina refund, reward
  preview/claim, login bonus claim, offline preview/claim, story progress, and
  gacha 1/10 pull with PRD-safe test accounts.
- Admin read-only support, ops error logs, story progress, offline reward, runs,
  and players only through PRD Cloudflare Access and authorized admin principal.

Keep smoke logs free of raw tokens, Authorization headers, raw request bodies,
Google ID tokens/full claims, RNG material, probability/full weight tables,
raw multiplier internals, and secrets.

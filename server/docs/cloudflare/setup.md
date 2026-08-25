# Cloudflare Setup

LAST RAIL uses Cloudflare Workers, D1, Queues, R2, Analytics Engine, and Cloudflare Access.

Current DEV resources:

- D1: `lastrail-core-dev`
- Queue: `lastrail-admin-jobs-dev`
- Analytics Engine dataset bindings are pending until Analytics Engine is enabled in the Cloudflare account.

R2 and Analytics Engine are part of the backend architecture, but the Cloudflare account must enable them in the Dashboard before DEV bindings can be deployed. Do not add deployed R2 or Analytics Engine bindings until the target product is enabled.

## Custom Domains

Game API custom domains:

- DEV: `dev-api.lastrail.app`
- STAGING: `staging-api.lastrail.app`
- PRODUCTION: `api.lastrail.app`

Admin API custom domains:

- DEV: `dev-admin.lastrail.app`
- STAGING: `staging-admin.lastrail.app`
- PRODUCTION: `admin.lastrail.app`

The root `lastrail.app` is reserved for the public website, Privacy Policy, Terms, and Support. Do not attach it to the server Workers by default. The async worker has no public custom domain.

## DNS / Custom Domain Procedure

1. Add `lastrail.app` to Cloudflare as the authoritative zone.
2. Confirm registrar nameservers point to Cloudflare.
3. Deploy only the approved environment Worker.
4. Add the Worker custom domain from Wrangler config or Dashboard: Worker > Settings > Domains & Routes > Add > Custom Domain.
5. Verify Cloudflare creates the DNS record and certificate.
6. For admin hostnames, create a Cloudflare Access self-hosted application before exposing operational Admin functionality.

Do not create STAGING or PRODUCTION DNS/custom domains until explicitly approved.

## DEV Admin Access Procedure

Dashboard path:

1. Open Cloudflare Dashboard > Zero Trust > Access controls > Applications.
2. Create a new Self-hosted public hostname application.
3. Set application name to `LAST RAIL Admin DEV`.
4. Set public hostname to `dev-admin.lastrail.app`.
5. Add an Allow policy named `LAST RAIL Admin DEV Owner`.
6. Include only the owner email or approved test operator emails.
7. Require MFA when the selected IdP can report MFA, or configure Cloudflare Access independent MFA if available.
8. Save the application and test from a fresh browser session.
9. Copy the Access team domain as `ADMIN_ACCESS_ISSUER`, for example `https://<team-name>.cloudflareaccess.com`.
10. Copy the application AUD tag as `ADMIN_ACCESS_AUD`.
11. Put the DEV values into `apps/admin-api/wrangler.jsonc`, regenerate types, dry-run, and deploy DEV admin-api.

Keep `dev-api.lastrail.app` public. Do not put Game API traffic behind Cloudflare Access.

## DEV Resource Procedure

1. Confirm auth with `pnpm exec wrangler whoami`.
2. Create only DEV resources unless explicitly approved:
   - `pnpm exec wrangler d1 create lastrail-core-dev`
   - `pnpm exec wrangler queues create lastrail-admin-jobs-dev`
3. Apply D1 migrations with `pnpm exec wrangler d1 migrations apply lastrail-core-dev --remote`.
4. Verify DEV D1 with:
   - `pnpm exec wrangler d1 execute lastrail-core-dev --remote --command "PRAGMA foreign_key_check;"`
   - `pnpm exec wrangler d1 execute lastrail-core-dev --remote --command "PRAGMA quick_check;"`
5. Generate types with `pnpm wrangler:types`.
6. Run `pnpm typecheck`, `pnpm test`, and DEV deploy dry-runs.
7. Optional DEV-only balance bootstrap: `pnpm d1:dev:seed:balance`.

When R2 is enabled, create a DEV-only bucket first, then add the matching `r2_buckets` binding to the affected Worker configs. When Analytics Engine is enabled, add per-Worker DEV datasets via `analytics_engine_datasets`. Keep STAGING/PRODUCTION names as config placeholders until those environments are explicitly approved.

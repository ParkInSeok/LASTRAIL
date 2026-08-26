# Admin Access

Admin domains are Cloudflare Access protected:

- `dev-admin.lastrail.app`
- `admin.lastrail.app`

Cloudflare Access authenticates the user. LAST RAIL authorizes the user by mapping the Access JWT `sub` to `admin_principals.access_subject`.

Do not grant admin authorization from email alone. Do not implement a direct ID/password admin login.

Production admin requires MFA. Configure either IdP-reported MFA or Cloudflare Access independent MFA with an authentication duration of Require every login.

`api.lastrail.app` is public Game API traffic and must not be protected by Cloudflare Access.

## DEV Access Application

`dev-admin.lastrail.app` should be protected with a Cloudflare Access self-hosted application before real operational Admin features are exposed.

Target DEV application:

- Application type: Self-hosted public hostname
- Application name: `LAST RAIL Admin DEV`
- Application domain: `dev-admin.lastrail.app`
- Session duration: short-lived for testing, for example 8 hours or less
- Policy name: `LAST RAIL Admin DEV Owner`
- Policy action: Allow
- Include selector: owner email or approved test operator emails only
- Require selector: MFA when the configured IdP can report MFA, or Cloudflare Access independent MFA when available
- Admin API authorization still requires `admin_principals.access_subject`

Validation:

1. Visit `https://dev-admin.lastrail.app/status` in a fresh browser session.
2. Confirm Cloudflare Access prompts for authentication before reaching the Worker response.
3. Confirm an unauthorized account is denied.
4. Confirm `https://dev-api.lastrail.app/status` remains public and is not behind Access.
5. Check Zero Trust Access logs for the allow and deny events.

Rollback:

1. Disable the `LAST RAIL Admin DEV` Access application or remove the DEV policy.
2. Re-test `https://dev-admin.lastrail.app/status`.
3. Do not change `api.lastrail.app` or any Game API hostname during rollback.

Wrangler does not manage Access applications. To automate this later, use a Cloudflare API token with Zero Trust Access application and policy write permissions. Do not store that token in Git.

The current Wrangler OAuth login has Workers/D1/Queues permissions but does not expose Access application write operations. If automation is required, create a scoped Cloudflare API token outside Git and provide it only through a local environment variable for the current shell.

Admin API sets `ADMIN_AUTH_MODE=ACCESS_JWT_REQUIRED` for DEV/PRD. Protected Admin routes validate `Cf-Access-Jwt-Assertion` against:

- `ADMIN_ACCESS_ISSUER`: `https://<team-name>.cloudflareaccess.com`
- `ADMIN_ACCESS_AUD`: the Application Audience (AUD) tag from the Access application

`ADMIN_ACCESS_ISSUER` and `ADMIN_ACCESS_AUD` are not secrets, but they must match the actual Cloudflare Access application. DEV remains blocked with `ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED` until these values are configured.

## Apply DEV Access Values

After the DEV Access application exists, copy:

- Team domain: `https://<team-name>.cloudflareaccess.com`
- Application Audience (AUD) tag

Then apply them locally without storing any token:

```bash
DEV_ADMIN_ACCESS_ISSUER="https://<team-name>.cloudflareaccess.com" \
DEV_ADMIN_ACCESS_AUD="<application-aud-tag>" \
pnpm access:dev:apply-vars
```

On Windows PowerShell:

```powershell
$env:DEV_ADMIN_ACCESS_ISSUER = "https://<team-name>.cloudflareaccess.com"
$env:DEV_ADMIN_ACCESS_AUD = "<application-aud-tag>"
pnpm access:dev:apply-vars
```

Follow with:

```bash
pnpm wrangler:types
pnpm typecheck
pnpm test
pnpm dry-run:admin-api:dev
pnpm --filter @lastrail/admin-api wrangler deploy --env dev
```

## Register DEV Admin Principal

After signing in through the DEV Access application, call `GET /v1/admin/bootstrap/access-subject` to read the verified Access JWT `sub` that should become the DEV admin owner. This DEV-only bootstrap response also includes the verified JWT `email` claim when Cloudflare Access provides one, so account routing can be diagnosed without copying raw JWTs. This endpoint is available only when `ENVIRONMENT=DEV`; it must not be exposed for PRD.

Do not copy raw Access JWTs, inspect browser cookies, invent a subject, or authorize by email alone. The server authorization source remains `admin_principals.access_subject`.

Apply the DEV principal with:

```bash
DEV_ADMIN_ACCESS_SUB="<access-jwt-sub>" \
DEV_ADMIN_PRINCIPAL_ID="dev-owner" \
DEV_ADMIN_DISPLAY_NAME="DEV Owner" \
pnpm admin:dev:upsert-principal
```

On Windows PowerShell:

```powershell
$env:DEV_ADMIN_ACCESS_SUB = "<access-jwt-sub>"
$env:DEV_ADMIN_PRINCIPAL_ID = "dev-owner"
$env:DEV_ADMIN_DISPLAY_NAME = "DEV Owner"
pnpm admin:dev:upsert-principal
```

This writes only to `lastrail-core-dev`. Do not use the local `access|local-owner` seed subject for remote DEV Access authorization.

# Admin Access

Admin domains are Cloudflare Access protected:

- `dev-admin.lastrail.app`
- `staging-admin.lastrail.app`
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

Admin API currently sets `ADMIN_AUTH_MODE=ACCESS_JWT_REQUIRED` for DEV/STAGING/PRODUCTION. Protected Admin routes remain blocked with `ADMIN_ACCESS_JWT_VERIFICATION_NOT_CONFIGURED` until Access JWT verification is implemented.

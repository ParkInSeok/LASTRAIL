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

Recommended DEV policy:

- Application domain: `dev-admin.lastrail.app`
- Session duration: short-lived for testing
- Policy action: Allow only the project owner or approved test operators
- Admin API authorization still requires `admin_principals.access_subject`

Wrangler does not manage Access applications. To automate this later, use a Cloudflare API token with Zero Trust Access application and policy write permissions. Do not store that token in Git.

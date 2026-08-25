# Admin API

Base domains:

- DEV: `https://dev-admin.lastrail.app`
- STAGING: `https://staging-admin.lastrail.app`
- PRODUCTION: `https://admin.lastrail.app`

Admin domains must be protected by Cloudflare Access. LAST RAIL authorization maps Access JWT `sub` to `admin_principals.access_subject` and then applies OWNER / OPERATOR / VIEWER permissions.

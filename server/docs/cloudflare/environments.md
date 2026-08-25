# Cloudflare Environments

LOCAL, DEV, STAGING, and PRODUCTION must not share remote D1, Queues, R2 buckets, Secrets, or Worker names.

| Environment | Game Worker | Admin Worker | Async Worker | D1 | Queue |
| --- | --- | --- | --- | --- | --- |
| DEV | `lastrail-game-api-dev` | `lastrail-admin-api-dev` | `lastrail-async-worker-dev` | `lastrail-core-dev` | `lastrail-admin-jobs-dev` |
| STAGING | `lastrail-game-api-staging` | `lastrail-admin-api-staging` | `lastrail-async-worker-staging` | `lastrail-core-staging` | `lastrail-admin-jobs-staging` |
| PRODUCTION | `lastrail-game-api-production` | `lastrail-admin-api-production` | `lastrail-async-worker-production` | `lastrail-core-production` | `lastrail-admin-jobs-production` |

DEV may be connected when `wrangler whoami` succeeds. STAGING and PRODUCTION resource creation or deployment requires explicit approval.

R2 buckets and Analytics Engine datasets must also be environment-separated. DEV bindings are pending until those products are enabled in the Cloudflare account.

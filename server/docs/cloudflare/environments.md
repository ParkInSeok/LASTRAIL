# Cloudflare Environments

LOCAL, DEV, and PRD must not share remote D1, Queues, R2 buckets, Secrets, or Worker names.

| Environment | Game Worker | Admin Worker | Async Worker | D1 | Queue |
| --- | --- | --- | --- | --- | --- |
| DEV | `lastrail-game-api-dev` | `lastrail-admin-api-dev` | `lastrail-async-worker-dev` | `lastrail-core-dev` | `lastrail-admin-jobs-dev` |
| PRD | `lastrail-game-api-prd` | `lastrail-admin-api-prd` | `lastrail-async-worker-prd` | `lastrail-core-prd` | `lastrail-admin-jobs-prd` |

DEV may be connected when `wrangler whoami` succeeds. PRD resource creation or deployment requires explicit approval.

R2 buckets and Analytics Engine datasets must also be environment-separated. DEV bindings are pending until those products are enabled in the Cloudflare account. STAGING is currently outside scope.

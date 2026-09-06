# Local server deployment evidence

Run the existing local HTTP harness from server with `--deployments`.
This uses actual Admin Hono routes, an ephemeral SQLite principal, and a
read-only in-memory bucket. It does not use remote R2, Pages, or Access.
The bucket's verification values are synthetic, not remote verification claims.

Verified on 2026-09-06:
- Current pointer and referenced history: Game API and Admin API HTTP 200.
- Admin API fixture DEGRADED shown as reconciliation needed.
- Async Worker current and missing history: HTTP 404, no record.
- Invalid history: HTTP 502, stored-record format classification.
- PRD query against the local DEV target: HTTP 400.
- Chrome /versions: automatic referenced history, manual invalid version,
  service switch clears selection/input, missing Async Worker evidence.
- No deployment, rollback, storage write, or environment-switch control added.

Permission-denied rendering is covered by component tests. Real Cloudflare
Access/Pages and DEV storage are NOT verified by this localhost harness.
Remote deployment and evidence population require separate approval.
No history list endpoint is assumed; only current and explicit version reads.

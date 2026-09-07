# Exact version-code content registry local implementation

User-approved 2026-09-07 scope: explicit INTERNAL/STABLE/DISABLED entries within
existing DEV/PRD, no inferred channels, exact code lookup, safe pre-auth handshake,
server tester authorization, immutable artifact promotion, independent minimum,
whole-document CAS/audit/rollback and release pinning. No live policy/tester grants,
remote R2/PRD writes, deployment, secrets, new environment or A05 retry.

Architecture: optional `clientReleaseRegistry` in the existing version document;
absence retains legacy behavior. Registry entries are immutable in artifact identity,
status changes are explicit. Same R2 object ETag protects every mutation. Immutable
history candidates are written before CAS; only pointer-linked history is committed.
Rollback restores a prior registry while preserving the explicitly set current
minimum. Legacy exporter must reject registry documents to prevent silent stripping.

Game bootstrap: existing GET /v1/client-config returns safe AUTH_REQUIRED state for
INTERNAL without an authorized tester. Exact bootstrap auth routes remain usable;
authenticated profile/nickname/story hydration is allowed, gameplay/economy remains
blocked. GET /v1/client-config/authorized requires a verified session and exact tester
entitlement. Stable pre-auth content behavior remains; registered gameplay requires
matching release-ID header. Revocation/expiry and registration rechecked each request.

Tester foundation: empty D1 table with exact account/environment/version code, status,
expiry and revocation. No public tester management endpoint or launch/tester seed;
OWNER-reviewed management workflow remains a documented follow-up. Local tests only.

- [x] Common parsing, exact-code resolution, legacy compatibility and artifact identity tests.
- [x] Game bootstrap/auth/metadata/content/gameplay gates, pinning, expired/revoked testers.
- [x] Admin GET and register/status/promote/minimum/rollback with target equality,
  OWNER-only mutation, ETag409, immutable audit candidate and legacy-export protection.
- [x] Exact DTO docs for Admin/Unity, test matrix, local migration/type/lint/regression,
  independent review and canonical checkpoint before completion.

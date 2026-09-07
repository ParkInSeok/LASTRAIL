# Client Registry Local Verification

Date: 2026-09-07. Scope: local Admin UI only, not a DEV/PRD deployment.

## Contract

Canonical contract: planning repository `docs/api/client-release-registry.md`.
Server implementation is in the separate `attendance-pass-20260907/server`
worktree; the previously deployed DEV API does not prove this contract is live.

- Exact DEV/PRD target; server target echo and capabilities required.
- GET `/api/client-releases?target=DEV|PRD` through the existing Pages proxy.
- Register, status, promote and minimum commands match the documented POST DTOs.
- Whole-document quoted revision pinned when editing starts; stale/unavailable
  reads and unresolved failures disable saving. No automatic write retry.
- Registration starts INTERNAL and acknowledges unregistered legacy-code blocking.
  First LEGACY registration sends `legacyTransitionConfirmation:ENABLE_EXACT_LOOKUP`;
  subsequent CONFIGURED registrations omit it. Independent review found the missing
  field against newer Server source; RED reproduced and the builder was corrected.
- Artifact fields are immutable for existing rows. Status editing cannot set
  STABLE; promotion requires the same server artifact identity and confirmation.
- Minimum change has its own confirmation; promotion never includes minimum.
- Tester management and rollback UI are not implemented. No entitlement guess.
- Structural metadata validation is not artifact-byte verification.
- Legacy export is removed when registry is CONFIGURED and blocked until registry
  read readiness is known. Server-side rejection remains authoritative.

## Automated Evidence

RED observed: missing registry API methods/component; unsupported STABLE via status
did not throw; registry endpoint 404 was incorrectly labelled DEV fixture/ID missing.
GREEN after implementation: `npm test -- --run`: 51 files / 269 tests.
`npm run typecheck` and `npm run build`: PASS. Existing >500 kB chunk warning remains.

Tests cover response whitelisting/unknown status, exact mutation body/path,
target/revision/permission/capability guard, separate confirmations, reused code,
missing revision, read states and legacy writer exclusion. Mock fetch asserts one
POST on conflict, no automatic retry. Server/storage authorization is not replaced
by these client-side guards.

## Interactive Synthetic Browser Evidence

Local harness: `npm run dev -- --port 5183`, then
`http://127.0.0.1:5183/scripts/registry-ui-qa.html`.
The harness replaces fetch without retaining/forwarding the native fetch function.
All writes mutate local memory only; unknown paths fail locally. No credentials,
server writes, uploads, content download or new DEV accounts were used.

Observed in the in-app browser:

1. Promotion is disabled before confirmation.
2. Editing at revision local-1 then external local-2 refresh disables saving.
3. Explicit reload discards the previous draft and confirmation.
4. Synthetic 409 submits once (POST attempts 1), retains failure evidence and
   disables editing/saving until explicit reload. No automatic retry.
5. Register code 2 shows a new INTERNAL row and resets form after fresh read.
6. Promote code 2 shows STABLE and unchanged artifact/compatibility; minimum
   remains 1 until a separately confirmed minimum change updates it to 2.
7. Code 1 status changes to DISABLED without replacing its content metadata.
8. Read-only capability disables the form. DEV -> PRD shows target mismatch and
   blocked saving; returning to DEV discards the prior draft.
9. A failed refresh preserves readable cached evidence but disables saving.
10. Screenshots inspected at desktop 1280x900 and mobile 390x844: two-column form
    becomes single-column; table scroll stays local. Mobile document clientWidth
    and scrollWidth both 375 (scrollbar excluded); no page-wide overflow.
11. Post-review strict LEGACY fixture rejects missing transition confirmation.
    Fresh registration succeeded with `ENABLE_EXACT_LOOKUP`, became CONFIGURED
    at local-2, and the legacy form disappeared. Initial fixture now starts LEGACY;
    earlier multi-row scenarios were exercised before this stricter fixture update.

Screenshots were inspected through the browser tool, not saved as durable image
files. The synthetic server is not an authoritative Server integration test and
does not claim artifact download/hash verification. QA server/tab were stopped.
The QA HTML is not a Vite production entry and must not appear in dist.

## Release Boundary

No Pages deployment, R2 write, migration, secret, Access/domain change, remote
registry mutation or tester grant. Existing preview remains source 00afd4c.
Reproducible fixed-commit archive verification is recorded in the canonical Admin
handoff after commit; do not treat this working-tree build as a remote rollout.

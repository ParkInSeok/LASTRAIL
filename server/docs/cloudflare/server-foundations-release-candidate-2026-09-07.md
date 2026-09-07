# Server foundations release candidate and completion ledger

2026-09-07; SERVER_1. Candidate source is based on deployed65b0263524bfd21ed42e597f1205cc81723fd5b9.
The accompanying external package manifest records the frozen commit, archive and
bundle hashes, exact verification outputs and current readiness. This is a DEV
candidate preparation record, not deployment evidence or approval to mutate remote state.

## Completed local slice

| Slice | Implemented / verified boundary |
| --- | --- |
| Attendance/pass0022 | Explicit empty immutable policies; 04:00 KST day boundary; visit/milestone/recovery receipts; owned completed-run XP; premium verifier boundary; atomic ledger/replay/commit-time expiry guards |
| Crystal0023 | FREE/PAID lots, FREE-first spending, ticket exchange/recovery composition, verified order provenance, premium FREE linkage, idempotent verified refund/revocation and spent-deficit record |
| Registry0024 | Exact INTERNAL/STABLE/DISABLED; auth-first tester approval; limited hydration; gameplay release pin; Admin CAS/audit/status/promotion/minimum/rollback; empty tester table |
| Release hardening | Rollback reserves removed versions as DISABLED; conflicting releaseId metadata rejected; first legacy-to-registry registration requires explicit ENABLE_EXACT_LOOKUP confirmation |
| Consumer contracts | server/docs/api/client-release-registry.md, attendance-pass.md, crystal-wallet.md and dev-login-download-lobby-test.md |

Independent reviews covered the activity/premium clock boundary, historical refund
policy independence, registry rollback identity and final additive migration/rollback
gates. Reproduced findings were fixed and tested. See final package verification for
the frozen-source full-suite count; earlier394 is superseded by the added cutover test.

## Admin DTO questions resolved

The authoritative API contract is `../api/client-release-registry.md` (canonical copy
in planning docs/api). No response/endpoint remains intentionally unspecified for
the authorized local registry UI:

- GET `/v1/admin/client-releases?target=DEV|PRD`; target must equal the serving environment.
- Rows contain version/versionCode/status/contentRelease/artifactIdentity and explicit
  STRUCTURAL_ONLY validation. It is not artifact-byte, device or store validation.
- `revision` is the quoted whole-document ETag; every mutation takes expectedRevision
  and reason. Read unavailable/target mismatch/missing revision/readonly capabilities
  must disable writes.409 means refresh/review, never force or automatic resave.
- Registration starts INTERNAL. Editing means status change only; artifact/versionCode
  cannot be replaced. First LEGACY registration additionally requires
  legacyTransitionConfirmation=`ENABLE_EXACT_LOOKUP` because it is an admission cutover.
- Promotion requires expectedArtifactIdentity and PROMOTE_UNCHANGED_ARTIFACT; server
  uses the stored artifact. Minimum change is another endpoint with confirmMinimumChange=true.
- Rollback requires committed historyId and RESTORE_REGISTRY; current minimum remains,
  newer version identities remain reserved as DISABLED. auditHead is returned; no
  history-listing API is claimed. Persist/use reviewed returned IDs as needed.
- Read roles follow existing active Admin authentication; only OWNER mutates.
  `manageTesters:false`; no entitlement management endpoint or local eligibility inference.
- PRD is a supported contract target only when correctly configured; current PRD
  infrastructure is incomplete. Local DTO/mock coverage is not a PRD readiness claim.

The Admin owner retains its frontend implementation and Pages rollout. Server changes
do not prove its UI completed, deployed or exercised actual writes. No rejected
cross-task disclosure is retried through another channel.

## One consolidated remote approval scope (NOT EXECUTED)

No new credential destination approval is requested: the dedicated DEV session to
`https://dev-api.lastrail.app/v1/client-config/authorized` is already authorized.
The existing one guest and11-ticket/1+10-draw/replay budget is unchanged.

The minimum additional **infrastructure rollout** decision is whether to permit the
following exact DEV-only candidate scope after review of its manifest:

1. On `lastrail-core-dev` (`14c31f63-3b9d-46b9-bd4a-8e093aaba436`), verify current migration
   state and recoverability, then apply only0022_attendance_pass.sql,
   0023_crystal_wallet.sql,0024_tester_entitlements.sql in order. These add tables/indexes/
   triggers and the nullable pass_entitlements.order_id; no policy, entitlement or
   economy seed is included. Preserve all existing gacha receipts and balances.
2. Deploy the frozen candidate Game API `lastrail-game-api-dev` and Admin API
   `lastrail-admin-api-dev` with their existing bindings/Access/admission settings.
   Reserve a new unique human server version using verified target history, then
   create versioned release branch/service tags. Do not reuse2026.09.07.3. Archive
   and record prior Worker identities, new runtime IDs and approved read-only smoke.
3. Leave the current version pointer in its legacy state during infrastructure smoke.
   No registry registration, minimum change, content activation or policy seed is
   implied by deploying these Workers. New economy endpoints fail closed while empty.

**Separate optional activation scope**, required for a real INTERNAL case, must name:
the exact approved app version code and immutable content artifact; deliberate first
registry cutover timing and compatible Unity/Admin consumers; one dedicated account
identifier once created; DEV-only exact version entitlement expiry and revocation
owner. A minimal grant is one ACTIVE row in0024 with those values and an audit actor,
then explicit revocation after the agreed window. No wildcard tester, open-ended
expiry, arbitrary account, public grant endpoint or PRD permission is included.
The current absence of an account identifier is not permission to create another.

A05's separately approved public-file upload and guarded legacy content activation
remain blocked by normal tool review/actual public404 evidence. Do not conflate that
scope with the new registry cutover or retry through a rejected alternative tool.

## Impact, order and recovery

Migrations0022–0024 are additive but operationally forward-only once new ledgers,
orders/entitlements exist. Keep them during Worker recovery; do not drop/reverse them.
Preserve new receipts and provenance even if a feature is disabled. No destructive
cleanup, balance conversion or automatic confiscation is packaged.

Before registry activation, legacy client admission is retained; new policy-driven
features stay unconfigured. Rollback to the old Worker still requires checking actual
current data/policy semantics; no remote rollback verification is claimed.
After the first registry write, **pre-registry Game/Admin Workers are forbidden rollback
targets**: they do not enforce tester/status/pins or preserve registry writes. Recover
with a compatible fixed Worker and guarded registry history operations. Do not remove
the registry to evade the gate, and never lower minimum automatically during rollback.

First registration creates only an INTERNAL entry and immediately blocks unregistered
legacy clients. The explicit confirmation prevents accidental ordinary-row activation;
it does not provide zero-downtime migration. Coordinate supported versions, consumers
and tester permissions before the cutover. Promotion alone never changes minimum.

Wrangler DEV dry-run reports an existing missing env.dev Durable Object inheritance
warning for realtime co-op; this candidate leaves that configuration unchanged. Do
not claim realtime room binding readiness from the successful bundle. PRD remains
blocked by D1/Access placeholders and missing VERSION_BUCKET bindings; no PRD bundle
or deployment readiness is claimed. Async Worker source/deployment is unchanged.

## Final unfinished-work ledger

| Category | Item / owner / next action |
| --- | --- |
| Slice complete | Server activity/wallet/registry source, local SQL/API tests, independent fixes/review, DEV bundle preparation and frozen package |
| Policy pending | Attendance dates/tables/milestones/recovery price/cap; pass season/XP/level/free-premium tables; currency ID/name/exchange rate/SKUs; publish only approved immutable policies |
| Policy pending | Offline current compiled100/50/30/20 differs from old24/2/1/80 draft; current usage proven, final launch approval unresolved. Fuel100/cost10/recharge300 are DEV seed values, not inferred launch approval |
| Implementation plus external setup | Real store purchase/refund adapters, authenticated webhook handling and product/app ownership; synthetic verifiers are not production implementation. No real billing until separate setup/policy |
| External permission/readiness | New DEV0022–0024/Worker rollout; optional exact tester entitlement/cutover; A05 publication/activation normal-review gate |
| Consumer implementation | Admin local registry UI and Unity actual authorized adapter/session pin/test runner; owners report actual fixed source and tests. Narrow DEV session consent already supplied |
| Real verification pending | Actual A05 hashes/activation → sole guest/profile/name → download/lobby; separate approved11draw/replay/DB evidence. No local fixture is promoted into this column |
| Deferred backlog | Tester management UI/API, public audit-history listing, unsupported source-less Admin metrics, Rail Choice/World Raid/exact concurrent-user work; no silent expansion into this candidate |
| PRD/app distribution | PRD infra/Access/D1/R2, Google/store registration, signed Android build/device test remain outside this DEV candidate |

Preserve existing login/first-clear claim identities if a future policy changes ticket
rewards to crystals. No retroactive claim reset, free regrant, inventory conversion,
gacha probability/pity change or unapproved launch numeric default is included.

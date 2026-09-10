# Screen integration owner audit — 2026-09-10

CE01 follow-up: explicit GET/POST /v1/formation/ultimate, independent revision/CAS,
immutable receipt, active-run lock and atomic removal clear (migration0031).
Departure-v2 requires expectedUltimateRevision and pins the actual selection;
run-combat-requirements-v2 reflects it. See docs/api/ultimate-selection.md and
combat-content-resolver-review.md. Actual battle and remote deployment remain incomplete.

Owner source: attendance-pass-20260907/server, base904ce840. Current S01/S03/S10
code and API documents are one local verification/commit unit, not deployed.
New account-state.ts, formation.ts, offline-economy.ts; index.ts registrations and
offline route integration; migration0028 and related integration tests. See
docs/api/account-state.md, formation.md and offline-economy-binding.md. S01-only
fixed reconstruction and source-manifest hash are documented in account-state.md.

| Slice | Existing/reused | Concrete remaining implementation or dependency |
| --- | --- | --- |
| S01 | Authenticated account-state GET, existing profile/crystal/growth GETs; S03 active squad now nested under formation | Strict S01 clients use the fixed S01 snapshot until adopting documented S03 additions; owner has not executed Unity runtime |
| S02 | State GET supplies owned skill/promotion/core levels/invested EXP; growth/config and all existing growth POSTs reused | Combat/effective stat catalog and formulas are not provided by this state DTO; equipment/research targets still disabled |
| S03 | Implemented POST formation/presets/:presetId/save and /activate, owned unique five-slot validation, CAS/idempotent receipts, frozen active selection and common-state refresh | Operator-only slice. Train/equipment/ultimate/package preset fields, delete, coop readiness and run snapshot admission remain separate; no optimal-power autofill policy |
| S04 | Implemented GET/POST trains/loadout for actual owned basic four IDs; server 2/3/4-slot milestones, immutable receipts, revision/ownership/progress/open-run transaction guards; migration0029, docs/api/train-loadout.md | Medical unlock unapproved; second-instance identity and turret installation rules undefined. Those owned rows stay visible with reasons. No acquisition or prices; departure snapshot remains S11 |
| S05 | Existing pulls reused; added active banners/detail, current pity/base+next odds, owned history/batch GET; sparse result recovery from receipts/result rows/ledgers | Local implementation validated74files470tests; docs/api/gacha-reads.md. No banner activation/seed or remote validation; unknown historical quantities stay incomplete |
| S06 | Existing crystal ticket exchange and Store verification routes | General shop listing/purchase/limit/grant route absent. Need authoritative product ID, quantity, currency/price, period limit, unlock and version; optional car prices are undecided in canonical |
| S07 | No general quest/mail routes found in owner game API | Add committed-event producer and per-period entitlement, direct claim, atomic mail transfer, 14-day expiry, per-right receipt; D2/D3/W1/W2/W3 only per canonical mail policy |
| S08 | GET attendance/:activityId and POST check-in/recover/milestone claim | Existing policy uses one repeated daily reward + milestones, not a sequential 28-cell calendar. Needs versioned 28-cell rewards and server check-in orchestration; launch bonus is separate |
| S09 | GET passes/:activityId and POST rewards/claim, xp/claim, purchases/verify | Current policy uses run-outcome XP; daily/relative-week objectives, automatic season settlement and 24h checkout cutoff are not implemented. Existing explicit claimUntil is not automatic settlement |
| S10 | Existing preview/claim extended with verified active economy adapter24/2/1/20; policy fence, pinned receipt/reference and early sequence; legacy unbound behavior retained | No implicit activation/seed; future sub-minute minimum policy unsupported by existing minute calculator. Remote behavior remains unverified |
| S11 | Preview/owned atomic admission core and original-run GET recovery implemented; firstClearEligible pinned; explicit combatContract known/missing/version refs; docs/api/run-combat-requirements.md | Actual Unity combat binding is incomplete. Public new paid start is409combat_not_ready with zero writes; prior successful replay preserved. 492-test admission success at4ced349 was not battle entry. Need content IDs, battle seed protocol, engine/stat profile and ultimate choice; no fake fallback |

This table is not completion of every S03–S11 feature. Missing implementation remains
assigned work; catalog uncertainty is not a blanket blocker for all local work.
S04 basic installation, S05 reads and S11 departure admission core are locally implemented.
S11 read-only POST runs/start-result also recovers original start results when the
journal has the original key/previewId but no runId; see docs/api/run-start-result.md.
Public new paid starts remain combat_not_ready until actual combat bindings exist.
S06–S09 still require assigned implementation; uncertain catalogs are scoped
dependencies rather than blanket blockers.

## Planning decisions to resolve precisely

1. Authoritative operation category/catalog IDs and current progression mapping:
   existing source has six ordinary story operation IDs plus raid, while the
   Plan confirmed four means categories, not replacement story nodes. Existing
   six IDs remain unchanged. Four catalog IDs remain unknown and are not created.
2. Train instance ownership: schema currently has one row per carriage ID; the
   canonical source audit permits count limits but does not prove second Gun,
   Medical or Defense ownership/grants. Confirm stable instance IDs and acquisition
   policy before installation/purchase can reference them.
3. Approve or identify actual shop and achievement catalogs (IDs, exact rewards,
   prices/limits, eligibility, version). Optional-car acquisition is explicitly a
   proposal with undecided prices. Empty UI fixtures are not a production catalog.

## Proposed batch-claim contract before implementation

Separate endpoints by system (ordinary quests, mail, pass), never one implicit
cross-system claim. Request includes a bounded list of entitlement IDs and a
batch idempotency key. Each entitlement has its own durable unique receipt shared
with individual claims. Response includes per-ID CLAIMED / ALREADY_CLAIMED /
NOT_ELIGIBLE / EXPIRED / RETRYABLE_FAILURE, receipt ID when present, and a
refreshRequired flag. Atomicity is per complete entitlement reward bundle; a batch
may partially succeed. Retry uses the same IDs/key; successful rights cannot pay
again. A batch receipt must not freeze a transient failure as permanently complete.
This is a proposed contract, not implemented or verified behavior.

## Verification / boundaries

S01 and S03 HTTP tests first failed on absent routes; S10 tests first showed old
100/50/30/20 and missing policy fencing. Added5 state,11 formation,5 bound-offline
SQLite tests. The initial S01 full suite was71files446tests; final S03/S10 full run
passed73files462tests in14.67s. Its commit is recorded in the handoff. All4 TypeScript projects and source ESLint
pass. Blanket ESLint includes old untracked Wrangler bundles under apps/*/audit-results
and fails on357 generated-code diagnostics; those files were preserved. Source
lint excludes audit outputs. Independent source review found no formation must-fix
issue and identified ambiguous offline historical binding lookup; migration0028's
direct receipt reference resolves it. Reviewer did not independently rerun tests.

Remote migrations/Worker deployment and OneDrive canonical write remain blocked
by previous automatic review. Canonical documents were read, not synchronized.
This report is local; no new authorization inferred from delegated instructions.

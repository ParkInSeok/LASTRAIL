# Screen integration owner audit — 2026-09-10

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
| S04 | State GET exposes train level, owned cars/turrets, unlocks; existing train level-up POST | No equipment installation route/position schema. Canonical train-loadout-speed-detail-v1 is design/source audit, not ownership authorization; Medical/second-instance grants and prices explicitly unresolved |
| S05 | POST /v1/gacha/banners/:bannerId/pulls | Banner window/probability/pity/history GET absent; use existing server banner policy and pull receipts, no UI fixture conversion |
| S06 | Existing crystal ticket exchange and Store verification routes | General shop listing/purchase/limit/grant route absent. Need authoritative product ID, quantity, currency/price, period limit, unlock and version; optional car prices are undecided in canonical |
| S07 | No general quest/mail routes found in owner game API | Add committed-event producer and per-period entitlement, direct claim, atomic mail transfer, 14-day expiry, per-right receipt; D2/D3/W1/W2/W3 only per canonical mail policy |
| S08 | GET attendance/:activityId and POST check-in/recover/milestone claim | Existing policy uses one repeated daily reward + milestones, not a sequential 28-cell calendar. Needs versioned 28-cell rewards and server check-in orchestration; launch bonus is separate |
| S09 | GET passes/:activityId and POST rewards/claim, xp/claim, purchases/verify | Current policy uses run-outcome XP; daily/relative-week objectives, automatic season settlement and 24h checkout cutoff are not implemented. Existing explicit claimUntil is not automatic settlement |
| S10 | Existing preview/claim extended with verified active economy adapter24/2/1/20; policy fence, pinned receipt/reference and early sequence; legacy unbound behavior retained | No implicit activation/seed; future sub-minute minimum policy unsupported by existing minute calculator. Remote behavior remains unverified |
| S11 | POST runs/start consumes an existing account-owned operation preview; finish/preview/claim exist | No game operation-preview creation route. Need approved operation catalog, owned formation/equipment, frozen quote and atomic cost validation. Existing start consumes preview separately before batch; race/failure behavior needs review before new producer |

This table is not completion of every S03–S11 feature. Missing implementation remains
assigned work; catalog uncertainty is not a blanket blocker for all local work.
Next dependencies are S04 owned equipment/instance installation and S11 departure
admission; S05 read paths and the approved S06 products remain implementable work.

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

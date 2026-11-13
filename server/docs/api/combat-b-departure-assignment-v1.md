# B persisted departure assignment v1

`combat.b-departure-assignment.v1.20261006`; prepared admission `departure-b-preparation-v1.20261006`; persisted source `combat.b-persisted-source.v1.20261006`.

## Actual owners and storage

Migration `0107_b_selection_sources.sql` provides existing BSelectionStore/BRefitStore selection, immutable receipt and guard tables plus account catalog, carriage physical instances and original refit/rank sources. There are NO seeds, account initialization, unlock defaults, rank defaults, physical ID creation or old-run rewrites. Migration was executed only by disposable DB tests; it is not deployed.

`PersistedBSelectionCatalog` reads explicit account catalog version/revision/eligibility/unlockedSlotCount; no row means 503 combat_b_catalog_unavailable. `PersistedBRefitSource` reads saved B selection and explicit original rank/slot source in one SQL snapshot and verifies every instance belongs to that account/kind in combat_b_carriage_instances. Original source/candidate/definition hashes, exact documents, account physical rows and ownership are guarded in the caller's original transaction. Source status PERSISTED_ACCOUNT_SOURCE means persisted provenance, not operating approval or runtime support. Old LOCAL readers still explicitly require ISOLATED_LOCAL_FIXTURE; old snapshots/receipt bytes are preserved.

The original B stores still own selection/refit validation and writes. No public account provisioning/selection/refit endpoint is enabled by this change. Account policy/rank/instance issuance must come from an approved producer; source tables stay empty when no producer has supplied them.

## Versioned actual departure preparation

`departure.ts` exports `prepareBDepartureSnapshot(db, authenticatedPrincipal, runId, freshBaseJson, originalCombatSourceOwner)`. It calls the real persisted assignment producer, replaces fresh candidate admission with the B preparation version and frozen assignment, then calls the existing combat-pin owner. Its returned assertions MUST be in the SAME original run-creation transaction. It does not write or spend fuel.

The existing `startDeparture` B-binding branch is now evaluated before legacy A state. Its trusted internal optional bPreparation invokes this exact preparation path; it then retains combat_b_operation_runtime_unsupported. Public routes supply no bPreparation and START remains closed. Actual preflight invocation with A formation/loadout absent is tested, with no new run or fuel debit. The immutable original transaction is exercised in disposable SQL using existing run snapshot/map/idempotency transaction mechanics, not operating admission.

The old public preview request still uses its old A selection contract. A new operating B quote/commit and all reachable executor support are not declared complete. This preparation is a real reusable departure-code path and persisted-source owner, not a LOCAL-only supplier API.

## Supported named preparation

Original selected OPERATOR IDs bind explicitly to fixed B slot and physical instance ID/revision; CARGO gets no actor and holes are preserved. Kyle/Sable reuse bNamedOperatorDefinition and calculateOwnedOperatorStats, including original promotion, skill-training and training-core growth. Supported selected actors may be one or two; there is no A formationRevision/loadoutRevision or five-selected-actor requirement. Original completeness of five training-core slots is a growth rule shared with the existing owned source; absent/inconsistent cores return combat_b_assignment_training_unavailable instead of guessed level 1. Named operators outside this bounded producer return combat_b_assignment_operator_unsupported.

Actors contain original physical binding, source revisions/hashes, permanent predeparture HP/ATK/DEF/basic interval, skill magnitude and approved basic/interception/skill definitions. These are unscaled permanent/predeparture stats; risk, original B chassis HP/DEF and live run equipment/accepted effects are not fabricated. Equipment/research predeparture applicability reuses the existing common lifecycle policy; no empty runtime equipment claim is made. Selected commander effects are explicitly combat_b_assignment_commander_effects_unsupported until their execution owner is connected.

`predepartureStatsIncluded=true`; `effectiveCombatStatsIncluded=false`; `runtimeBindingIncluded=false`; `canStart=false`. Original combat/profile hashes seal source identities; they do not prove an executor consumed these actor assignments. Active/ultimate/native geometry/timing and complete runtime effects are not supported by this supplier merely because their approved definitions are present.

## GAME original lookup

GET `/v1/runs/:runId/b-departure-assignment`

Required GAME Bearer and `X-B-Assignment-Version: combat.b-departure-assignment.v1.20261006`. No query/body. Read-only own account/run; auth rechecked; Cache-Control no-store. Response is the complete original assignment plus origin/departurePinHash/runtimeProfileHash/startSnapshotHash/packetHash. ETag is packetHash; optional If-Match must match. Version mismatch 409 combat_b_native_version_conflict; stale If-Match/source 409 combat_b_native_source_stale or combat_b_assignment_source_stale; invalid request 400 contract_violation; unauthenticated/revoked session 401. Original pin authority/account-state errors are retained. Missing historical assignment is 409 combat_b_assignment_source_unavailable; no current inventory/selection backfill.

Reader validates original snapshot/pin and assignment hash without querying today's catalog/rank/growth. After current source deletion, original GET remains available for an authenticated owner. Existing b-departure-runtime-input v1 remains the A five-formation/separate-carriage contract; do not reinterpret it as B.

No persistent new DEV environment/URL is claimed. Actual GAME TCP verification uses an isolated temporary listener that is closed after verification. Sample QA includes complete original packet/snapshot with explicit synthetic persisted account sources, not a live START or supported executor.

## Verification and residuals

Related 10 test files: 197 PASS / 0 FAIL; new persisted file 12 PASS. Final scoped type/lint exit 0; fresh read-only review no concrete P1/P2, reviewer executed no tests. Real GAME TCP GET/ETag/no-store/If-Match/query/revoked-session checks; version rejection also verified through actual GAME app.request; actual departure B preflight; migration-backed store save; original SQL commit and restore after current sources disappear; growth/physical/catalog/refit changes prevent new run. Kyle/Sable and explicit unlock/missing source/unsupported actor are verified. Unity execution 0; operating migration/START/fuel/reward/deploy 0. Prior unrelated full-suite 75 failures are preserved, not claimed fixed or baseline-proven.

QA: D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-persisted-departure-final-20261006.json, SHA256 6faadb0271d64c0ebce0caf4775b2cde5019ece0fc19ac5ef7d470d0dd05cf59. Async adapter type repair delta: combat-b-persisted-departure-delta-20261006.json, 12 PASS. Original TCP sample: combat-b-assignment-sample-20261006.json, SHA256 aec0162c5acd0b99048d2072783a6ae0b8b37f245d886b292125aa230d1b3737.

Actual remaining inputs: approved policy/issuance producer for initial unlock/chassis values and real account instance/rank sources; a B-only operating quote/commit; actual executor consuming frozen assignment and original risk/vehicle/geometry/actions/accepted timing; runtime equipped-item and selected commander effects; adaptation of result-validation admission from legacy A to this original B assignment where actual executor is available. These do not invalidate or block the independent persisted named preparation/read contract completed here. ARRIVE criterion remains separately pending.
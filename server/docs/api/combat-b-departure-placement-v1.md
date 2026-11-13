# B original placement departure binding v1

Contract: `combat.b-departure-placement.v1.20261006`.

Approved source: 44_COMBAT_DETAIL_CONTRACTS.md#combat-b-mainline-approved-20261002 and #combat-b-continuation-boundary-20261002. One OPERATOR per occupied carriage or CARGO; at most five total placements, fixed wire slots 0..4. No automatic filling or compression. Initial unlock counts and B chassis numeric proposals remain unapproved.

## Owners and prepared transaction

`BSelectionStore.captureForDeparture(auth)` reads original saved B selection, checks current GAME session/account, saved selection revision and exact document, catalog revision, slot eligibility and selected ownership. It provides exact selection/session/selected-ownership and original catalog CAS statements. Current catalog and tables are isolated LOCAL fixtures, not operating migrations/providers.

Optional `BRefitStore.captureForDeparture(auth)` reuses original persisted refit validation and source fingerprint; supplies original instance IDs, shared rank source revision, refit revision, definitions/source hashes, source and saved-refit CAS statements. Absence does not prevent selection-only preparation. Supplying an invalid/stale refit owner fails explicitly; it is not silently omitted.

`prepareBPlacementDeparture(store, auth, runId, freshBaseJson, originalCombatOwner, optionalRefitStore)` seals the original selection fragment and optional refit snapshot under `admission.bDeparturePlacement`. It verifies exact original refit slot/carriage-kind/occupant and selection revision. It mirrors admission into departureResponse where originally present, and calls the existing original combat-pin preparation. The original pin seals the resulting base snapshot and original profile/source hashes. It does not assert that profile actors match the selected occupants.

The caller MUST execute all returned `atomicAssertions` in its SAME original departure transaction. No write or START occurs in this preparation function. Unsupported parent executor remains rejected. It neither installs an executor nor enables operating START. Legacy departure-v4's five-operator formation and separate 2–4 carriage loadout remain retained facts; they are not used to infer B choices.

## Restore and availability

`readBPlacementDeparture(originalSnapshotJson, {accountId,runId,expectedPinHash?})` validates the existing original pin, fragment hash and ownership identity. It restores frozen source/revision/profile/pin identities without querying today's selection/catalog/refit. Missing historical fragment returns null, including an old pinned run; no backfill. An unpinned fragment, modified original snapshot or wrong identity fails 409 `combat_b_departure_placement_conflict` or the original pin error. Trusted original SQL snapshot only; no untrusted client snapshot intake.

The packet retains `effectiveCombatStatsIncluded=false`, `runtimeOccupantBindingIncluded=false`, `canStart=false`. Without optional refit it explicitly lists missing physical-instance, refit/rank and effective-profile sources; with original refit only effective occupant profile binding remains. Profile hash presence is not proof of that binding.

There is no new public endpoint, persistent DEV URL, operating provider registration or live account migration in this change. The existing b-departure-runtime-input v1 still describes legacy A admission. Do not label its five formation slots as B carriage slots.

## Remaining minimal producer contract

Operating catalog/unlocks must supply account-scoped source version/revision and actual unlocked capacity/eligible carriage kinds with a same-batch guard; no default all-five unlock.
Physical-instance/rank source must supply original selectionRevision, sourceRevision, per fixed slot instanceId and carriage kind, shared BCB01..07 ranks and exact ownership guards. The existing refit owner then supplies saved refitRevision/definitionHash/sourceHash.
Effective occupant profile producer must supply actual selected operator ID keyed to the selected physical instance/fixed slot, original operator growth/skill/equipment/commander source revisions and hashes, original final HP/ATK/DEF/action definitions and source/executor versions, plus same-batch assertions. CARGO must remain no operator. Actual selections may contain 1..5 placements and holes; they must not require legacy A formationRevision/loadoutRevision=1 or fixed five operators. Missing values must not become zero, QA stats or guessed B chassis values.
Only after an executor actually consumes this assignment can runtime binding be claimed and an API consumer successor be registered. Effects/geometry/motion do not block the independent persisted-selection freeze implemented here.

## Verification

QA: D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-departure-placement-final-20261006.json, SHA256 86dc395b4943884e191e0e183ca22ec1a4eb9d323b3e9b4ee6f345c1a7cacc5a. Related selection/refit/placement: 69 PASS, 0 FAIL; new placement tests 11. Original disposable SQL transaction includes saved choices/refit → frozen original pin → successful persistence → restore after live sources deleted; late selection/catalog/ownership/session/refit/rank/physical-instance changes reject without a new run. No operating START/economy/rewards/deployment. Scoped typecheck and lint exit 0. Fresh read-only review found no P1/P2 and ran no tests. Unity execution was not performed. Previous full-suite 75 failures remain separate, not claimed fixed or proven baseline.
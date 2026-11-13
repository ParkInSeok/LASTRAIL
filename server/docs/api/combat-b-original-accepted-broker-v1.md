# Original accepted B broker — 2026-10-06

Internal contract: combat.b-original-accepted-observation.v1.20261006.
This is a bridge from an ORIGINAL SERVER result-verification owner to existing BRouteEvidenceOwner and OriginalSafeRuntimeSource. It is not a public report API, a new simulator, or proof inferred from client flags. Default game provider remains uninstalled/409.

## Original authority and actual gap
combat.authority-split.v1 sets ordinary combatOwner UNITY_LOCAL/UNITY_HOST, transactionOwner SERVER, resultVerificationOwner SERVER and clientReportProvesCombat:false. combat-authority.ts explicitly describes policy, not execution/report approval. combat-host-report-store.ts and combat-final-report-store.ts retain NOT_PERFORMED envelopes/evidence and cannot serve as accepted proof.
Unity two-wave evidence SHA c96c52e870205add10d05450d3f8ac9179f8bfb4675b90175708b5ad79e12b08 shows LOCAL victory and restored owners; it does not contain a SERVER-verified result witness. Runtime source records say INSTALLED_LOCAL_EXECUTORS_NOT_SERVER_TRUSTED and canPrepareTrustedMaintenance:false.
Original command resume-step-362, commandRevision507, nativeSequence1063, checkpoint sha256:0b3a391f51e2509339ee7df6010b3285693bc91b78ec97b0c17fad1b80432dfd remain distinct from server maprevision2 TRAVELLING. Terminal stop does not supply an original ARRIVE event. Hash equality is integrity, not truth.

## Internal source contract
BOriginalVerifiedObservationOwner.prepare receives {accountId,runId,nodeId,mapRevision,kind,evidenceRef}; kind is ARRIVE/WAVE_COMPLETE/COMPLETE/RELEASE_HOLD/SAFE_BETWEEN_NODES. evidenceRef is original accepted source reference or null for safe source lookup.
It returns {status:"UNSUPPORTED",missingSources:string[]} or {status:"VERIFIED",observation,assertions:D1PreparedStatement[]}.
This owner must authenticate original runtime assignment, verify checkpoint/journal/native binding/event facts and hazard registry completeness, and provide SAME-BATCH guards for every original source revision/record/assignment/lifecycle/clock/hazard/transaction inspected. Immutable original event+revision must not mean new bytes. No HTTP field or unchecked digest implements this port.
Strict observation:
- version/accountId/runId/nodeId/mapRevision; authorityMode ORDINARY_SOLO, sourceVerification ORIGINAL_OWNER_VERIFIED, verificationOwner SERVER are INTERNAL verification output only.
- nativeContextId, departurePinHash, nodeSourceHash, operationBindingHash (plain 64hex); nativeBindingHash and checkpointHash (sha256:64hex).
- originalEventId/sourceRevision/sequence/evidenceRef.
- registeredHazardOwners:string[], hazards:{ownerId,pending}[], unknownHazards:string[]. Unique/exact registered-owner coverage; original owner must independently verify full registry, not accept client list.
- combatStopped/travelStopped as verified original facts.
- arrival:{eventId,sequence}|null; waves:{eventId,sequence,ordinal,hazardsSettled:true}[]; completion:{eventId,sequence}|null.
- holdIds:string[] and holdSettlements:{eventId,sequence,holdId}[] from original transaction owner. No automatic release from terminal/completion.
All native event sequences <= original checkpoint sequence. Wave ordinals/holdIDs/registry unique. Arbitrary extra report/verified/paused fields rejected by schema.

## Existing owners and actual transaction paths
BOriginalAcceptedBroker(db,originalOwner) implements both interfaces; createBOriginalAcceptedRouteProvider(originalOwner) is INTERNAL composition for existing registerBRouteRoutes. No index/production owner install/public endpoint change.
Broker matches active original SQL account/run/preview/snapshot/map, actual selected/current node, frozen native pin/context/nodeSource/operationBinding hashes and exact evidenceRef. It never accepts runId as nativeContextId or checkpoint/native sequence as map revision.
ARRIVE requires original accepted arrival event and stopped travel while original map TRAVELLING.
WAVE_COMPLETE requires accepted ordinal event and settled corresponding wave facts; original BOperationProgressStore still enforces order/count/current node.
COMPLETE requires ACTIVE node, both original ordinary wave events in order, accepted completion and stopped clocks/no unknown or pending registered hazards; original MapStore still enforces its stored wave receipts.
RELEASE_HOLD requires exact original hold settlement event. Node victory, empty hazards or a client release flag do not substitute.
Proof maps original eventId/sequence/sourceRevision to existing operation.b-route-original-evidence.v1.20261005 and hashes the complete verified observation for sourceHash. Original assertions plus exact account/preview/snapshot/map guard run in SAME original transaction. Only original MapStore/WaveStore owns mutations/receipts/replay.
BETWEEN_NODES connects RunSafeMaintenanceStore only at original READY current node after verified completion, stopped clocks and fully known settled registry. Safe sourceId is B_VERIFIED:+SHA256 of FULL verified observation, so changing checkpoint or native binding with same event/revision invalidates prior safe eligibility. Original source assertions and original SQL fence use equipment_safe_write_guard.
SAFE_EVENT remains unsupported until its independent original permission source exists. No broader allowed safe events or equipment policy introduced.
Original native supplier only supports ordinary source; legacy/nonordinary source remains unsupported. World/COOP are not made supported by this SOLO broker; original HOST policy is preserved.

## Consumer input and missing decisions
Existing consumer routes/body remain operation.b-route.v1.20261005, original GAME Bearer, X-B-Route-Version and Idempotency-Key=command.eventId. evidenceRef must come from an original verifier-issued reference; Unity cannot mint one from originalEventId or a diagnostic receipt.
The supplied LOCAL sample is exact INTERNAL owner output plus actual SQL receipts, marked PRIVATE_LOCAL_VERIFIED_OWNER_SQL_FIXTURE_NOT_ACTUAL_UNITY_RESULT_VERIFICATION. It is not a client submission example or real source verification.
Unity may prepare original checkpoint/journal/context/event/revision/native sequence export and lifetime/retry handling independently. No public original report/checkpoint upload or accepted receipt issuance contract exists yet; do not implement network calls against a guessed API.

Required Plan decisions, within current approved original authority:
1. Who implements SERVER result verification, how original Unity owner assignment/version and complete checkpoint+command journal are authenticated and validated, and how verifier issues immutable opaque references. Recommendation: a server-controlled verifier reuses the original approved native owner/restore/replay implementation, without a new simulator, and issues scoped witnesses only after validation. This is a proposal, not approval; signing/digests alone do not prove combat.
2. Original accepted ARRIVE semantics/source: present LOCAL fixture has no actual accepted arrival, only terminal travel stop. Specify original node-entry/travel-arrival event producer; do not retroactively call VICTORY arrival or invent operating distance/speed.
3. Witness maps original reportOriginMapRevision to each current transactional map revision after an original ARRIVE receipt; retain historical report origin separately. Controller commandRevision/nativeSequence/server maprevision are different owners.
4. Original witness payload/reference recovery, transport/body size/storage/lifetime and report denial/replay contract; no oversized full checkpoint endpoint invented.
5. Original hold transaction/safe-event permission producers remain separate. No reward or new safe policy implied.

## Verification
Final scoped six-file suite78PASS/0FAIL/0pending:9new+69related; scopedtypecheck/lint exit0.
Actual original SQL ARRIVE/two waves/COMPLETE/hold RELEASE/safe ENTER/prepare and historical map replay tested with LOCAL verified-owner record table only. Native source/checkpoint substitution, source same-batch deletion rollback, unknown/incomplete hazard registry, wave order, missing hold/safe-event, safe checkpoint change and real raw Unity observation refusal covered.
Safe checkpoint identity RED7PASS1FAIL before hash binding, then GREEN. Fresh read-only review and final delta P1/P2none; reviewer did not execute tests.
One route-composition test uses explicit LOCAL auth callback, not real operating GAME auth/TCP. No new real TCP test or original Unity rerun performed. Existing auth/HTTP source evidence preserved.
QA root D:/Project/LASTRAIL/QA/CommanderS1-20260922/: combat-b-original-broker-validation-20261006.json, combat-b-original-broker-local-sample-20261006.json, combat-b-original-broker-final2-20261006.json.
Production source: apps/game-api/src/combat-b-original-accepted-broker.ts only. No migration/public API/index/source registration, old source/Unity file changes, real START/account/economy/reward/deployment.

## Current follow-up / v2 result-consistency owner — 2026-10-06

The v1 record above is historical. Its restore/replay recommendation is not an approved prerequisite. The concrete server owner and authenticated report/receipt path are now installed for the preserved Unity v1 ordinary solo export; see [current contract](combat-b-result-report-v1.md). `combat.b-original-accepted-observation.v2.20261006` uses `RESULT_CONSISTENCY_VERIFIED` and `combatTruth=NOT_PROVEN`; original v1 records/tests retain their meaning. Report-origin/current map revisions are joined by the existing immutable map receipt chain. Separate actual ARRIVE, original selection input, SAFE_EVENT/hold sources and newly supplied Unity v2 journal adaptation remain distinguished there. No old report is relabelled as full combat truth or operating acceptance.
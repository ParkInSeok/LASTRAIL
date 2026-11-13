# B original route adapter — 2026-10-05

Contract: operation.b-route.v1.20261005. Internal accepted evidence: operation.b-route-original-evidence.v1.20261005.
Registered in original game app; existing GAME Bearer authentication, account-scoped run ownership, authentication rechecked before response/write. Cache-Control no-store. No new public START. Ordinary SOLO UNITY_LOCAL and existing HOST policy are preserved; this adapter does not recompute combat. Current B binding only supports original supported SOLO snapshots.

## Original selection and read
Use existing POST /v1/runs/:runId/operation-map/destination with existing operation.map-destination.v1 contract, body {expectedRevision,edgeId}, original Idempotency-Key. CONFIRM records TRAVELLING/pendingEdge, not arrival.
GET /v1/runs/:runId/b-route-binding requires X-B-Route-Version: operation.b-route.v1.20261005, no query/body. Optional If-Match equals packetHash; ETag is packetHash.
Response: version/accountId/runId/operationId/selectedRisk/runStatus, bindingHash/startSnapshotHash, mapRevision/phase/holdIds, currentNode/pendingNode/pendingEdgeId, nativeContextId/departurePinHash/originalNativeOrigin, nativeNodeSource/nodeSourceStatus, canStart:false/originalRuntimeVerifierRequired:true/packetHash.
Nodes carry original frozen plan refs, eventKind, encounterVersion/definition/hash and original completedWaveContract. Unknown elite/boss counts remain null, not zero.
nativeContextId comes only from original departure source, never runId. Legacy missing pin remains null. nativeNodeSource is supplied only from original pinned bScoutGunnerSource matching selected/current node and strict existing schema/version/hash/origin. Later-node sources are not rerolled or regenerated from current catalog. packetHash is canonical JSON hash excluding packetHash; startSnapshotHash hashes original stored snapshot bytes.
ETag/If-Match is GET-only. Source changes during read or auth cause conflict.

## Actual mutation, distinct from diagnostic safe-transition
POST /v1/runs/:runId/b-route/transition
POST /v1/runs/:runId/b-route/wave-complete
Headers: original GAME Bearer, Content-Type application/json, X-B-Route-Version as above, Idempotency-Key exactly command.eventId. No query/If-Match. Strict envelope {version,command}; extra flags/account/provider/pause/hazard fields rejected.
Transition command reuses original mapCommand:
- ARRIVE: {kind:"ARRIVE",eventId,expectedRevision,edgeId,evidenceRef}.
- COMPLETE: {kind:"COMPLETE",eventId,expectedRevision,nodeId,evidenceRef}.
- RELEASE_HOLD: {kind:"RELEASE_HOLD",eventId,expectedRevision,holdId,evidenceRef}.
CONFIRM on this new path is 400 combat_b_route_use_original_destination.
Wave command reuses original exported bOperationWaveCommandSchema: {eventId,nodeId,waveOrdinal,expectedMapRevision,evidenceRef}.
Successful actual commit/replay returns {version,committed:true,receipt:<original map/wave receipt>}; it writes through original OperationMapStore/BOperationProgressStore, not a diagnostic receipt.
Original wave ordering/count requirement, event ID/body conflict, hold blocking/release, terminated historical replay, snapshot/plan preservation remain original owner responsibilities. UI/controller must preserve command identity for retries, reread after stale revision, reject old run/node responses and dispose former node runtime before replacement.

## Internal trusted owner; operating implementation not installed
Only server-composed BRouteEvidenceProvider/BRouteEvidenceOwner may prepare proof and original SQL assertions. HTTP cannot install one.
Owner receives version/accountId/runId/nodeId/eventKind/mapRevision/bindingHash/departurePinHash/nativeContextId/kind/eventId/evidenceRef/waveOrdinal/holdId.
Strict proof repeats exact context plus originalEventId/sourceRevision/originalSequence/sourceHash/eligibility/holdIds. Eligibility is ARRIVAL_ACCEPTED, OBJECTIVES_AND_HAZARDS_SETTLED, HOLD_TRANSACTION_SETTLED or WAVE_AND_HAZARDS_SETTLED according to kind. Holds are unique; only COMPLETE may produce holds. 1..128 D1 prepared assertions must fence every original accepted travel/event/checkpoint/objective/hazard/settlement source inspected in the same original commit batch.
Adapter additionally fences inspected run/preview identity and preview account/objective/risk, active account/run, original snapshot, map state/revision/plan. Race failure rolls back original receipt.
Operating travel arrival, wave+all-hazard settlement, safe-event permission and hold transaction source broker remain missing. Source-shaped flags or client pause are not original accepted evidence. Departure pin and selected node refs alone do not prove acceptance.
Default registered routes have no operating evidence provider: actual new mutations return 409 combat_b_route_original_source_unavailable with missingSources (e.g. ARRIVE_ORIGINAL_ACCEPTED_SOURCE). Existing original selection and GET are usable independently. Unknown operating speed/acceleration is separate from typed node binding/controller lifetime work. Equipment safe authorization remains ungranted.

## Errors, replay and environment
400: contract_violation, combat_b_route_request_invalid, idempotency_key_conflict, combat_b_route_use_original_destination. Existing GAME auth errors retain original status (invalid Bearer 401). 409: version/source stale, profile unsupported, native context unavailable, proof invalid/origin conflict, missing original source, original state/event/wave/hold conflicts. Unexpected stored-source corruption propagates as failure; not a successful null/fallback.
Bounded body uses original readBoundedBody. No automatic mutation retry with new eventId or changed body. Original successful result can replay after run termination with original owner authorization; historical receipt is not new eligibility.
Validation environment: ephemeral real loopback TCP using original GAME auth and isolated SQL. Closed after validation; not a standing DEV endpoint/deployment. Private positive commits use explicit LOCAL accepted-event SQL fixtures, not actual Unity runtime or operating START/economy/reward.

## Verification
Final scoped six-file suite: 89 PASS / 0 FAIL / 0 pending (14 new, 75 related). Scoped typecheck and lint exit0. Real loopback TCP 9 PASS. Private LOCAL original selection/arrival/two waves/completion/hold release committed and snapshot preserved.
Fresh read-only review found departure-preview same-batch fence omission; regression RED 13 PASS/1 FAIL before fix, final suite GREEN after joining/fencing original preview fields. Reviewer did not run tests.
QA root: D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-route-validation-20261005.json and combat-b-route-sample-20261005.json.

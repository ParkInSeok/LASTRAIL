# Accepted map lineage verification — equipment.safe-transition-verification.v1.20261005

Scope: WORK_QUEUE_SERVER.md#s29-safe-transition-verifier-next-20261005. Original approved maintenance timing stays between nodes or eligible safe events with combat/travel stopped. Existing UNITY_LOCAL/LOCAL_LOGICAL or UNITY_HOST/HOST_LOGICAL authority is preserved. This supplier verifies committed SERVER map/hold/source lineage; it never recalculates combat or promotes authenticated client pause/hazard claims to facts.

## Consumer flow and environment

1. GET `/v1/runs/:runId/equipment-safe-transition` using existing GAME Bearer. Response has `version`, `contextHash`, `authority`, current `phase`, `mapEventId:string|null`, `journalMode`, `lineage`, `accepted:false`, `runtimeSupported:false`, `authorization:'NOT_GRANTED_RUNTIME_UNSUPPORTED'`, `runtimeEvidence:null`. ETag=contextHash; no-store. No private snapshot bytes or credentials are returned. All server source hashes come from the original SQL snapshot. GET with query/body is rejected. GET can expose mapEventId=null when the current map revision has no internally verified original receipt, such as initial entry or destination selection. That is insufficient for verification.
2. Use the returned lineage directly to construct POST below. Do not compute a server snapshot hash from a departure response, equate Unity observation/source/checkpoint revision with map or journal revision, or invent mapEventId/holds.
3. Strictly validate response version/hash/identity and preserve explicit runtime unsupported flags. A HTTP200 is success supplying an immutable verification result; it is not an accepted ENTER/EXIT or equipment command. Continue independent integration/model work while actual travel/all-hazard source is unavailable. Do not call equipment mutation based on this packet.

These paths are registered in the existing game app. Validation uses an ephemeral real loopback TCP server and isolated SQL with explicit LOCAL original MapVerifier. The test listener is closed afterward; this is not a standing DEV URL, deployed environment, actual operating START or published admission.

## POST request

`POST /v1/runs/:runId/equipment-safe-transition/verify`
Headers: existing GAME Bearer, Content-Type application/json, `X-Equipment-Safe-Transition-Version: equipment.safe-transition-verification.v1.20261005`, `Idempotency-Key` exactly equal to body commandId (1..128 printable ASCII). No query or If-Match. Body <=18000 bytes via existing boundary; strict extra fields including paused/hazards/accountId/client authority are rejected.

ENTER body:
```json
{"version":"equipment.safe-transition-verification.v1.20261005","commandId":"client-intent-001","kind":"ENTER","reason":"BETWEEN_NODES","nodeId":"<GET lineage.nodeId>","expectedMapRevision":4,"expectedJournalRevision":0,"expectedHoldIds":["<exact original hold IDs>"],"mapEventId":"<GET mapEventId>","expectedAdmissionHash":"<GET lineage.admissionHash>","expectedSnapshotHash":"<GET lineage.snapshotHash>"}
```
SAFE_EVENT is the other ENTER reason. EXIT uses identical common fields with kind EXIT and omits reason. IDs <=200, revisions safe integers (map positive/journal >=0), hashes lowercase SHA256, hold IDs unique and exact original order. The example revisions and placeholders are descriptive only; the actual context/request/response sample is in the TCP QA file.

## Server verification and response

SafeTransitionVerifier owns all checks: active authenticated run owner/account; matching root/departureResponse admission and original pinned authority; matching original admission and complete SQL snapshot hashes; original immutable map plan/start snapshot; current node/map revision/exact hold IDs; exact current journal revision. BETWEEN_NODES requires READY after a traversed node; SAFE_EVENT requires ACTIVE noncombat node. EXIT requires original SAFE journal for the same node/map revision. These are structural requirements, not proof that runtime clock/hazards are stopped. Completed-node reward/augment/other holds remain separate original owners; no hold is released or interpreted as completed by this route.

mapEventId must resolve the immutable original OperationMapStore receipt whose revision and full state equal the current map state and whose verificationId is non-null. This proves a server-committed original transition lineage; ARRIVE alone does not prove safe-event authorization. Original OperationMapVerifier/BOperationWaveVerifier interfaces continue to own actual arrival/objective/settlement facts. Their operating adapters are not installed by this supplier.

POST response fields are `version,commandId,kind,reason,requestHash,accepted:false,serverTransitionVerified:true,runtimeSupported:false,authorization:'NOT_GRANTED_RUNTIME_UNSUPPORTED',journalChanged:false,clockChanged:false,authority,lineage,originalMapReceipt,runtimeEvidence:null,missingRuntime,clientObservationVerification:'NOT_PERFORMED',transitionHash`. `lineage` includes accountId/runId/nodeId/mapRevision/journalRevision/holdIds/admissionHash/snapshotHash/mapPlanHash. reason is null for EXIT. Missing runtime always includes ORIGINAL_COMBAT_TRAVEL_ALL_HAZARDS; SAFE_EVENT additionally includes ORIGINAL_ACCEPTED_SAFE_EVENT. An existing pause, READY, ACTIVE or client observation hash is never substituted. Stored packet hash is SHA256 canonicalJSON excluding transitionHash. Response ETag=transitionHash and no-store.

Before insertion, original auth is rechecked. In the original DB batch, active account/run, unchanged SQL snapshot, full map state/plan/revision, exact original map receipt bytes and current exact journal bytes/absence are fenced using a CHECK guard; then only a diagnostic immutable transition receipt is inserted. Failure writes nothing. Original safe journal/map/holds/clocks/HP/inventory are unchanged. Migration0104 creates only this receipt table/guard and UPDATE/DELETE rejection triggers; no legacy/backfill seeds.

Same commandId+same strict request replays original stored result after map movement or termination, without current source recalculation. Same key different body conflicts. Replay still requires original account ownership and active account access. A historical diagnostic receipt is never current eligibility. New verification on a terminated run fails. No fresh observation is backfilled into old requests.

## Errors and restoration

Existing auth failures retain their actual status (including401 and unavailable500/503). Shape errors400 equipment_safe_transition_request_invalid; query/content-type/If-Match contract_violation400; header version409 equipment_safe_transition_version_conflict; key400 idempotency_key_conflict. Other conflicts409 use equipment_safe_transition_ prefix: run_not_found/account_inactive/run_not_active/snapshot_invalid/admission_conflict/source_conflict/authority_unsupported/map_source_conflict/map_conflict/journal_conflict/already_safe/phase_ineligible/map_receipt_missing/map_receipt_conflict/command_conflict/receipt_conflict/state_conflict/auth_changed. Corrupt original SQL or infrastructure errors are not hidden as successful packets.

Recover uncertain POST results by the same command ID and unchanged original body. Do not replace its payload with refreshed GET values; refreshed values need a new user/runtime intent and key. Store the original packet/hash as diagnostic lineage, retain original Unity accepted command/full native observation separately, and do not reconstruct missing travel or hazard state.

## Unity source and actual remaining integration

Unity existing BSafeMaintenanceObservationSource / CombatSavedMaintenanceObservationSource LOCAL21PASS (QA SHAe61f3e7e9f778e5302db1940370de80864ef8040c8885602c015f26871603be5) remains valid independent evidence. B sourceRevision, saved checkpointRevision and server map/journal revisions have different owners. The observed B travelStopped/residualHazards/safeEventId are currently null; separate saved rail does not prove B travel. This contract is now available for strict GET/POST consumer connection, source pin/restoration/errors and unsupported-state handling; those tasks need not wait for full S29 runtime support.

Actual granting through original RunSafeMaintenanceStore/OriginalSafeRuntimeSource still requires real B accepted travel stop/resume and all original hazard terminal/pending sources plus accepted safe-event provenance and original samebatch fencing. A diagnostic receipt cannot implement that adapter. Existing Unity travel-owner queue covers independent producer implementation; joint equipment mutation testing follows when both original runtime evidence and accepted transition binding are ready. No new execution permission/timing policy, server combat owner, scene/art, operating START/economy or deployment is introduced.
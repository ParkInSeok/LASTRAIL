# B ordinary solo result consistency report — v1 / 2026-10-06

Current server contract: `combat.b-result-report.v1.20261006`; receipt: `combat.b-result-consistency-receipt.v1.20261006`; internal broker observation: `combat.b-original-accepted-observation.v2.20261006`.

Authority is the approved `combat.authority-split.v1`: Unity executes ordinary solo combat; Server validates original authority, pinned source, progression and result consistency. Full native replay is not required. This contract does not prove all reported damage, skills, geometry, input chronology or absence of cheating. It does not run a new simulator or authorize rewards, operating START or deployment.

## HTTP

- `POST /v1/runs/:runId/b-result-reports`: existing GAME Bearer authentication and recheck; `Content-Type: application/json`, `X-B-Result-Version: combat.b-result-report.v1.20261006`, `Idempotency-Key: requestId`.
- `GET /v1/runs/:runId/b-result-reports/:evidenceRef`: same GAME authentication/recheck and version header. No request body/query/If-Match. Encode the opaque reference as a path component. Original owning account only; termination does not erase the original receipt/report.
- No browser/public/web-session credentials or unauthenticated route. Responses use `Cache-Control: no-store`.
- Request stream UTF-8 limit **1,800,000 bytes**, plus the same canonical record budget at the store boundary. This is a technical local implementation limit allowing the original ~1.667 MB compact Unity export and leaving receipt/identity-column headroom below D1's 2 MiB row limit. It is not an approved retention period or chunk/upload policy. Oversized reports require an explicit supported transport extension; they are not silently truncated.

Strict request envelope:

```typescript
const request = {
  version: 'combat.b-result-report.v1.20261006',
  requestId: persistedRequestId,
  reportOriginMapRevision: originalExport.runtimeSources.mapRevision,
  checkpointHash: originalExport.runtimeSources.checkpointHash,
  nodeSession: originalExport.runtimeSources.nodeSession
};
```

`nodeSession` is the actual export `unity.combat-b.node-controller-local.v1.20261005`; its `runtime` is `unity.combat-b.travel-native-local.v1.20261005`, normal-speed profile. No separately manufactured event/witness or `verified:true` field. Native command revision is the complete command array length, native sequence is its original event cursor, and report origin revision is the captured server map revision. These are different owners/counters.

The parser validates the supported result fields and command journal; it is **not** a complete Unity strict-restore schema or an assertion that every passthrough native field was verified. Raw metadata such as canStart, LOCAL provenance, client authority flags, hashes and diagnostic receipts never issue authority by themselves.

## Receipt and checks

Receipt fields: version, opaque server `evidenceRef`, original requestId/reportOriginMapRevision, originalEventId, commandRevision, nativeSequence, checkpointHash, completedWaves, verificationLevel=`STRUCTURE_SOURCE_RESULT_CONSISTENCY`, combatTruth=`NOT_PROVEN`, fullNativeReplayPerformed=false, arrivalAccepted=false, progressAuthorized, missingSources, rewardsAuthorized=false.

Server checks against the actual frozen departure and deterministic node source: account/run/node/context/source; whole supplied node source equality; canonical native record hash; command uniqueness/cursor correspondence; approved ADVANCE/PAUSE/ORDINARY_MOVE/WAVE_BASIC forms; native ADVANCE maximumSteps 0..10000; operator-only attacks and admitted movement actor/slot; incremental logical-time bounds/pause/debt/terminal behavior; original generated enemy identities and maximum HP; complete planned/dead enemy and boarding sets; two ordered wave clear events, original 2-second minimum gap, final ordinal/cursor/tick; projectile/interception terminal state; exact installed five-owner hazard registry and no unknown/pending owners. No non-ADVANCE time movement or movement while paused. Terminal PAUSE/ADVANCE retain the victory tick.

Progress authorization additionally requires original snapshot `admission.formation`, `loadout` and `ownedState` to match every reported operator/carriage/train. Missing original admission produces a recoverable receipt with progressAuthorized=false and `ORIGINAL_DEPARTURE_FORMATION`. A present but mismatched original selection rejects the report; current mutable account inventory cannot repair old departure data. Reported allied HP bounds are checked for internal consistency, not represented as verified execution of all effects or damage formulas.

Canonical source/record hashing identifies bytes only. It is not combat truth, a signature of an authenticated native binary, full damage verification or accepted interception timing. Existing host/final/diagnostic NOT_PERFORMED receipts keep their old meaning.

Reports/receipts are immutable SQL records. Same request and body replay the original receipt; changed body under that request conflicts. A new request may not replace an already accepted node-scoped original event or command cursor. Same-batch guards bind account/run status, original preview/operation/risk, snapshot, map revision/state/plan. Concurrent writes either replay the original same request or reject; no partial receipt or map mutation is published.

Errors: 401 original GAME authentication; 400 invalid UTF-8/JSON/schema/content/query/idempotency header; 404 unknown private reference; 413 byte budget; 409 version/source/checkpoint/cursor/clock/actor/formation/event/revision/lineage conflicts and missing route source. Existing account denial follows the original authentication/source owner. Retry only the original same request after a transient network failure and recover its opaque reference. Refresh original map state after 409; never fabricate a new cursor/arrival/event or alter a historic report to bypass conflict.

## Original progression bridge and compatibility

`BResultConsistencyOwner` is installed for existing b-route mutations through `BOriginalAcceptedBroker`. New v2 internal sourceVerification=`RESULT_CONSISTENCY_VERIFIED`, combatTruth=`NOT_PROVEN` makes the verification level explicit. v1 internal schema/tests remain compatible and historical; no old receipt/status is relabelled.

A progress-authorized reference supplies original reported wave/completion checks to the existing WaveStore/MapStore; map revisions after the report origin must form a continuous original receipt chain for the same node. Actual original arrival is a separate producer. `VICTORY`/TERMINAL travel stop are **never** retroactively ARRIVE evidence. ARRIVE and RELEASE_HOLD remain explicit missing-source errors until their original owner supplies its contract.

Completion records a reference link inside the original node transaction, not another progress or reward journal. BETWEEN_NODES consumers can use that exact completion report link via the internal safe owner; SAFE_EVENT and real hold settlement permissions are separate unsupported sources. A diagnostic receipt cannot mint either permission.

No new operating runtime, supported departure registry, permanent DEV URL, or public website endpoint is deployed. Existing b-route request version/DTO/auth headers are unchanged; a client must first receive the server reference and use current expected map revision, not substitute commandRevision/nativeSequence/reportOriginMapRevision.

## Evidence and remaining dependency

QA and actual counts are in `D:/Project/LASTRAIL/QA/CommanderS1-20260922/combat-b-result-report-validation-20261006.json`. The real TCP test receives and recovers the **unchanged** original Unity nodeSession from `QA/CombatBMigration-20261004/BNativeTwoWave-20261005/two-wave-http-result.json` with existing GAME authentication. Its SQL original departure is a LOCAL transaction fixture missing full formation: progress remains blocked, SQL stays TRAVELLING, snapshot unchanged. Unity was not rerun and no approved operating motion/timing was inferred.

Positive wave/node progression tests use an explicitly LOCAL compatible frozen admission and an adjusted LOCAL report, then a separate LOCAL original arrival receipt. They are transaction/contract evidence, not unchanged actual Unity execution or operating acceptance. The real consumer still needs a matching original departure selection/runtime input and its separate arrival producer. Full replay is not a remaining prerequisite. Unity can now implement this report transport/reference recovery and original wave-reference/arrival work in its existing queue; final View linkage remains separately gated by approved art.
### Newly received Unity v2 journal (separate pending server adaptation)

Unity's `unity.combat-b.travel-native-local.v2.20261006` and `unity.combat-b.original-event-journal.v1.20261006` producer were subsequently received in the existing Unity handoff (`unity-b-original-event-producer-result-20261006`, manifest SHA256 `f68c247d96db39224f10f4eed37aba4d26932fada9de069d6e00dde908d89089`). This endpoint currently accepts the preserved v1 runtime only; sending v2 rejects at schema validation. No v2-to-v1 conversion or retrofilled journal is supported. The new journal's originalCommand/commandRevision, rawEvent.sequence and checkpoint.sequence must remain distinct. Its wave eventRef is a Unity LOCAL immutable reference, not this server's opaque evidenceRef. Server adaptation of the supplied v2 journal is actionable independently of the unresolved ARRIVE policy; the original v1 transport/regression evidence is not proof of v2 acceptance.
### Current v2 follow-up implemented — 2026-10-06

The preceding v2-pending note is historical. Versioned v2 original-journal intake, complete Unicode-safe split persistence/recovery, actual original event-reference broker binding and frozen departure-input read API are now implemented; see [current v2 contract](combat-b-result-report-v2.md). V1 wire, meanings and old receipts remain unchanged. V1 captures are not retrofilled/upgraded to v2; a v2 journal cannot overwrite a previously accepted original event/cursor. The separate actual ARRIVE and effective profile/occupant binding dependencies still apply.
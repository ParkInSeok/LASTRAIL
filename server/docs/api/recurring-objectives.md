# S09/S07 recurring-v1

Local implementation; no remote migration, seed or deployment performed. Current code including bulk claims requires migrations0001–0035 in order on game-api and async-worker's same DB. Migration0033 adds immutable source events, versioned operation mappings/season definitions, earned rights, receipts, quest mail, checkout intents, entitlements and a job cursor;0035 adds durable bulk requests/members. It preserves existing crystal lots/ledger while adding QUEST provenance. It seeds no product, cosmetic, operation or season.

All routes authenticate the account, return Cache-Control:no-store and reject query parameters. POST bodies are strict; empty means zero bytes, not `{}`. Every mutation except sync requires printable ASCII Idempotency-Key (1–128 chars). Reuse a key only for an identical request. Existing successful keys return the original receipt, including after expiry/revocation; they do not establish current eligibility.

| Method/path | Body | Result |
| --- | --- | --- |
| GET /v1/recurring/state | empty | version, serverTime, periods, policies, awards, mail, grants, eventStatus, capabilities, progressSyncPending; never grants |
| POST /v1/recurring/login | empty | accepted, eventSource, dayStart, refreshRequired; one login source per account/server day |
| POST /v1/recurring/sync | empty | addedObjectives,lastEventId; derives rights/XP, does not pay resources |
| POST /v1/quests/:awardId/claim | empty | grant receipt; only current earned unclaimed right |
| POST /v1/quest-mail/:awardId/claim | empty | same underlying quest right; strictly before mail expiry |
| POST /v1/passes/:policyId/levels/claim | level:1–50,track:FREE or PREMIUM | same right/receipt used by settlement |
| POST /v1/passes/:policyId/checkout-intents | empty | intentId,expiresAt,purchaseEndsAt,seasonEndAt,productId |
| POST /v1/passes/:policyId/purchases/reconcile | intentId,receipt | premium,seasonEndAt,settlementRequired,xpGranted:0 |

Grant receipts contain accepted,rightKey,policyId,claimId,rewardTransactionId,accountMutationId,serverClaimedAt,rewardLines,wallet,refreshRequired. Error shape is accepted:false,errorCode; 400 malformed input,403 ownership/verification/entitlement rejection,409 changed state or ineligible period,503 missing configuration/binding/verifier. Clients refresh state after login/sync/grants and use returned policy/award IDs.

## Source qualification and periods

Login and accepted run-finish sources use DB commit time, KST04 day boundaries. Natural monthly attendance shares the login source; recovery does not create a login or finish. Finish insertion is in the existing final-result transaction; replay cannot create another source or retime it. Raw source identifiers, content version, outcome, verified completion flag and timestamps remain for future authorized reconciliation.

Known six story operations support full success only. Story boss counts one end/zero waves only when the existing finish validator confirms bossDefeated; VICTORY text alone is insufficient. Ordinary15 uses an explicit operation_id/content_version mapping: success1end/15waves; normal failure1–14 completed waves counts1end/actual waves; zero-wave failure, abandon and invalid result count0. Story15 defeat remains UNSUPPORTED_STORY_DEFEAT pending cost/refund contract. World uses a separate score track. Unknown operation bindings remain OPERATION_BINDING_MISSING, never relabeled from story IDs.

The current finish producer is the existing run owner's committed result. Cooperative eligible original participants require an additional authoritative producer; capability is NOT_IMPLEMENTED and their progress is UNKNOWN_FOR_UNLINKED_ORIGINAL_PARTICIPANTS, not zero or an approved owner-only rule. Durable Object presence/damage is not eligibility evidence. No automatic retrospective grant is implemented.

Daily pass D1 login40/D2 one end80/D3 two ends80. Weekly W1 three logins/W2 six ends/W3 sixty waves each200. Pass weeks are season days1–7,8–14,15–21,22–28; no week5 XP. Ordinary quest weeks start Monday04 KST. Ordinary D2/D3 and W1/W2/W3 use the approved direct reward vectors in recurring-policy.ts. Same source feeds both systems independently; claim/mail never adds XP. Event time owns the period even if sync runs later.

## Pass, mail and settlement

PASS50 definitions cover exactly one service-calendar month. One level per100XP, displayed maximum5000XP/level50; earnedXp retains the uncapped objective audit sum. Daily200/weekly600 reaches level50 on day17. All50 free rows and exact5x premium extra rows are returned in rewardRows with xp threshold, freeState/premiumState, cosmeticSlot and nullable cosmeticItemId. Current premium requires an owned verified nonrevoked order. Cosmetic slots are frame30/operator skin40/train skin50; no actual IDs are supplied by this migration.

Ended earned unclaimed ordinary quest rights become mail at actual DB send time, expiring exactly14days later. Sending pays nothing; retries never renew expiry; expired unclaimed mail cannot be collected. Pass earned unclaimed rights instead settle directly without expiring mail. Manual and worker claims share the same right identity and atomic wallet/ledger receipt. Late source processing and verified old-season purchases remain eligible for that season's settlement, without next-season XP or entitlement.

async-worker scheduled handler is configured every5minutes in local/dev/prd configuration only. Each invocation visits up to10 accounts and pays up to20 pass rights per account. Durable cursor wraps, retaining failed/pending rights. This is not evidence of a deployed scheduler. Per-account historical source/definition scans and mail processing are not paginated; production volume/load validation remains necessary. settlementStatus distinguishes NOT_ENDED,SOURCE_SYNC_PENDING,SETTLEMENT_PENDING,SETTLED_FOR_RECORDED_SOURCES; the last does not assert missing cooperative sources are complete.

Checkout closes24hours before end. An intent lasts at most10minutes and cannot outlive the season. A trusted verifier must tie policyId, intentId, account, provider/app/product, environment, store transaction and purchase time to the proof. A valid purchase within the previously issued intent can reconcile after checkout/season end. No client assertion substitutes for PASS50_PURCHASE_VERIFIER; no deployed adapter is provided. Stored revoked orders reject new reconciliation, and premium grants recheck revocation at commit. Original success keys replay historical receipts.

## Configuration and validation boundary

### Additive objectiveProgress read contract (objective-progress-v1)

GET state now includes `objectiveProgress:{version,serverTime,appliedEventId,latestEventId,systems}`. This nested object uses one read-only DB snapshot for clock, events, definitions, earned rights and claim state. Its timestamp/cursors own these counters; do not combine counters with a separately fetched response or infer that the older top-level multi-query state is the same snapshot. No new migration, objective, reward, login event, sync or grant is introduced by GET.

systems has QUESTS and PASS50 entries. Each is `{system,policyId,status,objectives}`. An active registered definition returns all existing approved objectives, including unmet ones: QUESTS D2,D3,W1,W2,W3; PASS50 D1,D2,D3,W1,W2,W3. Missing current definition returns policyId:null,status:NOT_CONFIGURED_FOR_CURRENT_PERIOD,objectives:[]; the server does not invent a live season. Ended rights/mail remain in the existing state fields.

Each objective provides objectiveId,frequency,metric,target,periodId,periodStartAt,periodEndAt,calendar,current,observedCurrent,currentScope,coverage,unknownReasons,observedTargetReached,syncPending,awardId,status,xp,rewards. Specs/targets/rewards are the same constants consumed by award derivation. DAILY resets04KST; QUESTS weekly calendar MONDAY_04_KST; PASS50 weekly calendar SEASON_RELATIVE_7_DAYS. On season day29 onward the three pass weekly specs remain visible as NOT_SCHEDULED with null period/current values, no week5 reward or inferred zero progress.

Login current is the exact count of recorded eligible server login events in that period, with scope SERVER_RECORDED_LOGIN/coverage COMPLETE_FOR_RECORDED_LOGIN. Zero is valid within that explicit scope; it is not a claim about every unreported client session. Finish/wave total current is null and coverage PARTIAL_UNKNOWN because the cooperative original-participant producer is missing. observedCurrent is the exact eligible committed owner-source subtotal (a lower bound for the all-source objective), including a valid observed zero; never display it as a complete total. unknownReasons always includes COOPERATIVE_PARTICIPANT_PRODUCER_MISSING for these metrics and adds OPERATION_BINDING_MISSING or STORY_DEFEAT_COST_CONTRACT_PENDING when such source records exist in that period.

observedTargetReached can be true even with incomplete coverage: recorded eligible sources suffice to meet the unchanged threshold. If no persisted award exists, status is SYNC_PENDING and awardId:null; clients sync/refresh before enabling claim and never treat GET as a payout. Persisted awards use EARNED/CLAIMED for quests or XP_APPLIED for pass. Below threshold, status is UNKNOWN for incomplete finish coverage or IN_PROGRESS for recorded login. syncPending is separately true when period eligible sources exceed the applied cursor or a reached threshold lacks its award; UNKNOWN and pending can coexist. A persisted award remains earned while newer sources await sync. Counts follow immutable source event time, so delayed old-day sync does not increase today's daily counters.

### Bulk claims — approved bulk-claim-v1

The follow-up Plan delegation approved the per-right partial-success design and limits below. Requires migration0035 after0001–0034. It adds recurring_bulk_requests (immutable account/key/hash/body/list) and recurring_bulk_members (immutable action identity and terminal result). No seed, new reward policy, remote work or payment capability is added. Individual claim APIs and pass settlement retain the same rights.

POST `/v1/recurring/bulk-claims` requires auth and Idempotency-Key and creates/resumes the same frozen request. Strict body is exactly one of:

- `{kind:"QUEST",awardIds:[id,...]}`
- `{kind:"MAIL",awardIds:[id,...]}`
- `{kind:"PASS_LEVEL",policyId,track:"FREE"|"PREMIUM",levels:[1,...]}`

Each list has1–50 unique entries; duplicate, empty, oversized or mixed-kind fields return400. Pass levels are1–50, with one policy and track per request. Membership AND ordering are fixed: any changed body/list/order with the same account/key returns409idempotency_conflict. Items are not automatically added during retry. Idempotency key is scoped to the bulk request namespace, separately from individual claim keys.

GET `/v1/recurring/bulk-claims/:bulkId` authenticates ownership and returns a read-only single-snapshot response without processing. No query/body allowed; foreign/unknown bulk ID returns403bulk_claim_not_found. Both routes use Cache-Control:no-store. After an unknown response before bulkId is known, resend the exact POST/body/key; do not generate a replacement key automatically.

Response: `{accepted:true,version:"bulk-claim-v1",bulkId,request,createdAt,status,successCount,failureCount,pendingCount,totalCount,items,refreshRequired}`. Each item is `{index,action,status,receipt,errorCode}`. SUCCESS means this bulk member committed its grant; REPLAYED means an already committed underlying right receipt was reused. Both contribute to successCount. FAILED is a confirmed failure fixed permanently in this bulk, with errorCode and no receipt. RETRY_PENDING is unresolved, with optional errorCode, and can resume on the original key. Original successful/failed item data remains stable on reads/resumes.

Overall PARTIAL means at least one item is unresolved. COMPLETE means every item is terminal, **not that every item succeeded**: COMPLETE with failureCount50/successCount0 is valid. Clients must display counts and item outcomes separately. HTTP200 reports the accepted bulk state, including pending or failed members; request/schema/conflict/ownership/service failures use400/409/403/503. Global503 after an unknown DB response may still have persisted the request or grants; resume the original key to recover.

Each invocation starts at most10 pending members and stops starting more after a2.5second soft wall-clock budget. One in-flight member operation may finish beyond that budget. Pending members are ordered by attempt count then original index, so persistent transient failures do not starve untouched members. Further work requires the same POST/body/key; GET does not resume. No bulk background worker or automatic target expansion is introduced.

Each member uses the existing right's atomic full reward-vector transaction, including actual wallet/item/currency ledgers. A new grant and SUCCESS result commit together; previously granted rights store REPLAYED with the original receipt. Failures do not roll back other committed members. Before fixing failure, the server rechecks the underlying receipt under the account sequence fence. A pending-member guard also runs in the grant transaction so a racing attempt cannot grant after this bulk member became terminal. Individual, worker and concurrent bulk claims share the original right identity.

Confirmed eligibility/ownership/type failures are terminal, including unearned pass level, missing premium ownership and expired mail. A later changed condition requires a user-explicit retry using a new key. Service/configuration failures503, exhausted state contention and unknown exceptions stay RETRY_PENDING; MAIL awaiting actual mail creation is also pending. Resume checks receipts before eligibility/payout. Internal exception details are not exposed; unknown failures use temporary_processing_failure. Expiry is checked at each actual grant commit, never at the click or request-create time. A failed reward-vector item rolls back all items for that right; no expiry revival, no mixed quest/mail/pass processing.

Validation covers limits/duplicates/mixed fields, changed list/order conflict, frozen50-member bounded continuation, terminal failure remaining fixed after eligibility changes, explicit-new-key retries, atomic vector rollback with another successful member retained, individual/worker/bulk races, same-key concurrent resumes, grant-response loss, request-persistence response loss, owned read-only status and actual mail-expiry crossing. Full-suite/commit evidence is recorded in the handoff.

recurring_definitions policy_json is strict `{version:"recurring-v1",cosmetics:{FRAME:null|string,OPERATOR_SKIN:null|string,TRAIN_SKIN:null|string},purchase:null|{provider:"GOOGLE_PLAY"|"APP_STORE",appId,productId}}`. Definitions cannot overlap within each system or be updated/deleted. QUESTS definition edges are Monday04 KST. Missing actual cosmetic/SKU/adapter blocks checkout; fixture TEST identifiers in tests are not launch configuration. Existing S08 elite ticket and its pool remain separately unbound.

Validation covers actual HTTP false-boss-success exclusion, ordinary failures, independent weekly calendars, day29, login replay and attendance deduplication, concurrent sync/claims/settlement,50levels/premium5x, bounded repeated offline settlement, delayed source processing,14day mail send/expiry boundaries, late verified purchases, wrong ownership/proofs, revocation replay and migration with existing free/paid/revoked-order history. Test counts and immutable commit identity are recorded in the server handoff/report after final checks.

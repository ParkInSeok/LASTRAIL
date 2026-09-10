# S09/S07 recurring-v1

Local implementation; no remote migration, seed or deployment performed. Requires migrations0001–0033 in order on game-api and async-worker's same DB. Migration0033 adds immutable source events, versioned operation mappings/season definitions, earned rights, receipts, quest mail, checkout intents, entitlements and a job cursor. It preserves existing crystal lots/ledger while adding QUEST provenance. It seeds no product, cosmetic, operation or season.

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

recurring_definitions policy_json is strict `{version:"recurring-v1",cosmetics:{FRAME:null|string,OPERATOR_SKIN:null|string,TRAIN_SKIN:null|string},purchase:null|{provider:"GOOGLE_PLAY"|"APP_STORE",appId,productId}}`. Definitions cannot overlap within each system or be updated/deleted. QUESTS definition edges are Monday04 KST. Missing actual cosmetic/SKU/adapter blocks checkout; fixture TEST identifiers in tests are not launch configuration. Existing S08 elite ticket and its pool remain separately unbound.

Validation covers actual HTTP false-boss-success exclusion, ordinary failures, independent weekly calendars, day29, login replay and attendance deduplication, concurrent sync/claims/settlement,50levels/premium5x, bounded repeated offline settlement, delayed source processing,14day mail send/expiry boundaries, late verified purchases, wrong ownership/proofs, revocation replay and migration with existing free/paid/revoked-order history. Test counts and immutable commit identity are recorded in the server handoff/report after final checks.

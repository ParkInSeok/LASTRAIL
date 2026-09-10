# S05 recruitment reads and sparse-result recovery

Local code/document verification unit based9c68601; not deployed. No new migration,
banner activation, fixture seed, price or probability policy. Existing gacha tables
through0021 are used; the complete current server still requires0028 for S03/S10.
All four GET routes use existing bearer/session/account authentication and client
admission. Cache-Control:no-store. GET never rolls RNG, spends tickets, grants an
item/operator or rewrites history. Server-owned policy/receipts are the only inputs.

## Active banners and detail

`GET /v1/gacha/banners?limit=20&afterBannerId=...`

Only current ACTIVE windows are listed: startsAt inclusive, endsAt exclusive;
endsAt:null means no scheduled end. Draft, disabled, future, ended and inaccessible
DEV diagnostic banners are omitted. limit1–50, default20. ID ascending keyset;
nextAfterBannerId:string|null points to a visible banner, never a hidden scope ID.
Only send afterBannerId when continuing a page; omitted means the beginning.

`GET /v1/gacha/banners/:bannerId`

Active, upcoming or ended published banners can be read; draft/disabled/missing or
inaccessible diagnostic ID returns404 banner_not_found. Diagnostic listing/detail
uses the existing DEV/account/expiry/pool scope verifier. Current account pity is
read alongside each banner snapshot; absent pity is zero/revision0, not a write.

Envelope: list has accepted:true, serverTime:UTC ISO, banners:array,
nextAfterBannerId:string|null. Detail has accepted:true, serverTime, banner:object.
An empty active list is banners:[] with null cursor, not a sample banner.

Each banner object has:

| Field | Type/meaning |
| --- | --- |
| bannerId, bannerType | string; STANDARD or PICKUP |
| balanceVersion | string; current banner policy version |
| pickupOperatorId | string or null |
| startsAt, endsAt | UTC ISO string; endsAt nullable |
| availability | ACTIVE, UPCOMING or ENDED |
| prices[] | `{pullCount:1 or10,itemId:"cur_command_ticket",quantity:1 or10}` |
| pityConfig | `{elite:number,pickup:number,rare:number}` actual existing parser thresholds/fallbacks |
| pityState | totalPullCount, elitePityCount, pickupElitePityCount, rareOrHigherPityCount, revision (numbers) |
| nextGuarantee | NONE, RARE_OR_HIGHER_10, ELITE_30 or PICKUP_ELITE_60; same enum/precedence as actual pull |
| probabilities[] | resultId/resultType/resultRarity strings, quantity:number, isPickup:boolean, baseProbability:number,nextPullProbability:number |

Probabilities are fractions0–1, not percentages. Multiply by100 for display.
baseProbability is the no-guarantee distribution; nextPullProbability applies the
current next guarantee. It is not a fixed probability for every position in a
ten-pull: each committed result changes pity. The code injects the existing pool
parser, guarantee eligibility/precedence and effective weight helpers used by POST.
Internal relative weights, RNG and policy JSON are not public response fields.
Invalid/unusable pools or malformed policy return503, never invented odds.
Ticket price is the actual existing pull cost. Crystal-to-ticket exchange remains
the separate wallet policy/endpoint and is not a new cash recruitment product.

Probability row example (illustrative type shape, not activated policy):

```json
{"resultId":"op_vex","resultType":"ELITE_OPERATOR","resultRarity":"ELITE","quantity":1,"isPickup":true,"baseProbability":0.005,"nextPullProbability":1}
```

## Owned history and batch recovery

`GET /v1/gacha/history?limit=20&beforeBatchId=...`

Returns accepted:true, batches:array, nextBeforeBatchId:string|null. Order is
createdAt DESC then batchId DESC, including equal timestamps. Cursor must refer
to this authenticated account's batch; invalid/foreign cursor400 invalid_history_cursor.
limit1–50, default20. No account selection parameter is accepted.

`GET /v1/gacha/batches/:batchId`

Returns accepted:true plus one batch at the response root. Unknown/foreign batch
returns404 gacha_batch_not_found. Owned historical receipts remain readable after
the banner ends, independently of new-pull eligibility; they do not expose the
current hidden policy or allow another roll. History is account-scoped, including
an account's own old diagnostic receipts, not an admin history viewer.

Each batch: batchId, bannerId, pullCount:number, balanceVersion:string|null,
cost:{itemId:string,quantity:number}, createdAt:UTC ISO, postPityState:object|null,
results:array, recoveryStatus:COMPLETE|INCOMPLETE, missingSequences:number[],
unknownQuantitySequences:number[]. New batches pin balanceVersion in the receipt;
older summaries without that field returnnull rather than relabel history with the
banner's current version. Invalid historical pity staysnull. No current pity is
substituted for the stored post-pull state.

Results retain existing pullSequence/resultRarity/resultType/resultId/isPickup/
guaranteeType/wasDuplicate/duplicateConversion/reward fields. Valid saved response
entries are merged by1-based sequence with committed result rows. Sparse or malformed
entries fall back to those rows and the account-matched result item-ledger delta.
Full saved rewards can recover a missing row. Conflicting known row quantities
are not overwritten from the summary. Only whitelisted public fields are returned.

Missing result rows plus missing saved entries appear in missingSequences. Missing
material quantity is reward.quantity:null, never guessed as1. If a duplicate's
conversion identity is missing/malformed, its reward is
`{kind:"UNKNOWN",id:null,quantity:null}`: a token amount must never become an
operator count. Such sequences appear in unknownQuantitySequences. COMPLETE refers
to recovered result coverage/quantities, not proof that the client received the
original network response. INCOMPLETE never creates replacement grants.

Example missing result (fixture identifiers, existing batch already paid):

```json
{"accepted":true,"batchId":"batch_example","bannerId":"banner_example","pullCount":1,"balanceVersion":null,"cost":{"itemId":"cur_command_ticket","quantity":1},"createdAt":"2026-09-10T06:00:00.000Z","postPityState":null,"results":[],"missingSequences":[1],"unknownQuantitySequences":[],"recoveryStatus":"INCOMPLETE"}
```

## Client reconciliation and compatibility

Existing POST /v1/gacha/banners/:bannerId/pulls remains the only draw operation.
For an uncertain POST, retry its original banner/count/idempotency key. Changed
count on the same key now409 idempotency_conflict. Replay uses the same sparse
recovery helper/ledger join and newly pinned balanceVersion; it does not spend or
grant again. Existing DUPLICATE_REPLAY envelope remains unchanged. Legacy POST
receipts without a version retain their prior banner-version fallback; the new
history route explicitly reportsnull for that unknown historical version.

When a successful/replayed response lacks slots or quantities, read its batch ID.
Replace that batch's displayed results with the recovered snapshot, do not add
them to inventory again. For INCOMPLETE retain an unresolved state and use normal
support/reconciliation; never reroll with a new key just to fill the animation.
Refresh S01 account state after confirmed completion. Apply the established
account/session generation and latest-request checks to all responses.

No GET body; only the documented pagination keys are accepted, duplicates rejected.
Malformed query/path400 contract_violation. Existing auth/admission errors remain.
No storefront names/images or unapproved future catalog fields are fabricated.

## Executed verification

Eight new real-SQLite/actual signed-session tests: visible windows/prices/no writes,
sparse seven-item recovery and POST replay without a second grant, equal-time
history pagination/account isolation, approved pool4% elite total and forced pickup
parity with the actual draw, unknown quantities, revoked auth/hidden cursor IDs,
missing duplicate conversion, and pinned versions/public field filtering. Existing
actual gacha1/10/guarantee/replay tests pass. Full suite74files470tests PASS;
Game API TypeScript and source ESLint PASS. Independent review found the missing
conversion identity issue; it was reproduced then fixed with UNKNOWN/INCOMPLETE.
No Unity/device or remote deployment verification is claimed by this server unit.

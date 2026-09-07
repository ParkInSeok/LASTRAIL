# Attendance and pass foundations — local only, 2026-09-07

Implemented in the isolated `codex/attendance-pass-20260907` worktree from
`65b0263524bfd21ed42e597f1205cc81723fd5b9`. No deployed API changes, remote
migration, release policy seed, store connection or real purchases in this work.

## Configuration and policy status

Migration 0022 creates empty immutable `activity_definitions` and account-scoped
claim/visit/XP/entitlement tables. A definition ID identifies one immutable policy;
updates/deletion are rejected. Missing or invalid configuration returns 503.
Definitions must be populated only by a separately approved release process.
There is deliberately no public definition or entitlement-writing endpoint.

Confirmed: daily attendance/quest/usage reset is **04:00 KST** (19:00 UTC), weekly
reset Monday 04:00 KST, and pass boundaries align with 04:00 KST. This module
implements the attendance/pass clock only; existing raid/quest/quota code and
offline elapsed-time calculations are not changed. Older 05:00 raid documentation
requires owner reconciliation, not silent promotion here.

Attendance supports explicitly configured `DISTINCT_DAYS`, `dailyRewards`,
milestones and optional recovery. Supported models are implementation capabilities,
not approved launch values. Natural visits and recovered visits share the same
account/campaign/day uniqueness and cumulative milestone receipts. Recovery requires
an explicit inventory item/cost/limit and `missedDaysOnly: true`; otherwise it is
disabled. It supports past missed campaign dates, excludes today/future dates and
expires at campaign end. These detailed recovery settings are a configurable
proposal awaiting launch policy approval. `recovery: null` disables it.

Pass definitions explicitly specify start/end/claimUntil, ascending cumulative XP
thresholds, free/premium reward arrays, eligible server run outcome XP and nullable
store product mapping. There are no default seasons, XP values or rewards. Run XP
requires the authenticated owner's server-accepted completed/failed run and final
result within the season; quarantined/unowned/client XP are rejected. No solo/co-op
requirement is added. This is an explicit claim endpoint; no automatic run-finish
hook or quest XP source is enabled.

## Routes

All routes use the existing verified session/account and client admission layers.
No caller account ID, server date, reward amount, XP amount or premium flag.
POST requires an ASCII nonblank Idempotency-Key of at most 128 characters; payload
stream is bounded to 18,000 bytes. Unknown JSON fields and query strings are rejected.

| Method/path | Body |
| --- | --- |
| GET `/v1/attendance/:activityId` | none |
| POST `/v1/attendance/:activityId/check-in` | none |
| POST `/v1/attendance/:activityId/milestones/claim` | `{ "days": integer }` |
| POST `/v1/attendance/:activityId/recover` | `{ "dayIndex": integer }` |
| GET `/v1/passes/:activityId` | none |
| POST `/v1/passes/:activityId/xp/claim` | `{ "runId": string }` |
| POST `/v1/passes/:activityId/rewards/claim` | `{ "level": integer, "track": "FREE" or "PREMIUM" }` |
| POST `/v1/passes/:activityId/purchases/verify` | `{ "receipt": string, "productId": string }` |

dayIndex is the server day integer displayed by GET, calculated as
floor((serverEpochMs - 68,400,000) / 86,400,000). Clients may select a missed day,
but the server checks campaign bounds, current day, prior visits and recovery cap.

Successful claims include immutable receipt IDs, reward lines/balances and server
time. Same account/operation/key and request fingerprint returns the original
response; changed input returns 409. Different keys cannot repeat the same business
claim. Historical replay remains available after expiry/revocation; it is a receipt,
not fresh eligibility. GET and each new premium claim check current entitlement.

The D1 batch includes account mutation, economy transaction, inventory updates,
cost/reward ledgers, reward receipt, activity state and idempotency record. The
account mutation sequence fences concurrent economic state. A DB-time guard checks
campaign, daily reset and premium entitlement expiry/revocation inside the batch;
failed eligibility rolls back all writes and retries with fresh state up to three
times. Store transactions are globally unique per provider/application.

## Purchase and currency limits

`PASS_PURCHASE_VERIFIER` is a trusted server-only adapter interface with **no
default implementation**. It must verify actual store identity/signature/API state,
account ownership, application/product/environment, timestamps and refund/revocation.
Absent adapter returns 503; a client receipt/flag alone grants nothing. Opaque
receipts are not persisted or logged by this module; their request digest is stored.
Persisted entitlements retain store transaction and purchase-verification receipt
links; premium claims join them by account/activity. The test adapter is not a real
store verifier. Real provider APIs, callbacks, refunds and regrant/renewal policy
remain unimplemented and must be designed before enabling purchases.

The user approved direction for one common premium currency with earned/purchased
accounting, order tracing and refund design. Attendance and both pass tracks grant
FREE/earned currency; premium-pass grants still need their purchase link. Offline
repeat rewards grant no new crystals. These decisions do not establish a currency
ID, price or exchange rate. The generic inventory recovery test path is not the
final earned/purchased wallet. Do not point production crystal spending at it.

The subsequent crystal wallet implementation is now complete locally; see
`crystal-wallet.md`. FREE-first spending and premium order linkage are confirmed
and implemented. The earlier wallet proposal below is historical, not outstanding
implementation work. Numeric release policy and real store adapters remain pending.

Original proposed wallet work: separate earned and purchased ledgers, explicit spend order,
atomic currency debit plus ticket credit and request replay, and immutable policy
version on grants/exchanges. Preserve existing tickets and every receipt. Keep
existing login bonus and first-clear uniqueness when changing future grant policy,
so previous recipients cannot claim a second converted reward. No retroactive
account conversion, ticket confiscation, pity or probability changes are included.
The proposed 100-unit exchange and 1,200-unit total remain unapproved examples.

## Remaining launch decisions

1. Attendance campaign duration/counting policy, full daily/milestone reward tables.
2. Recovery currency ID/price/cap/window and whether proposed missed-day rules are final.
3. Pass season dates, claim grace, XP sources/amounts/thresholds and both reward tables.
4. Currency name/ID, ticket exchange pricing and policy transition (FREE-first spend order is confirmed).
5. Store/provider/app/product ownership, refund/revocation/renewal and paid-pass grant tracking.
6. Separate review/approval for release definitions, remote migration/deployment and real store setup.

## Verification

RED demonstrated missing migration/API, then new recovery/clock contracts. Independent
review found awaited verification timestamps and premium expiry races; both received
reproducing tests and were fixed. Final local run: **63 files / 376 tests PASS**;
four TypeScript projects and eslint PASS. Actual Wrangler local D1 0001–0022 applies;
foreign_key_check empty, quick_check ok, activity definitions count zero. Tests cover
same/different request keys, concurrent visits/recovery, insufficient balance,
ledger failure rollback, 04:00 crossing, campaign/claim deadlines, run ownership and
event replay, invalid/expired/revoked premium and DB-execution-time revocation.

The DB batch behavior follows [Cloudflare D1's documented batch transactions](https://developers.cloudflare.com/d1/worker-api/d1-database/).
No Android/device or actual store verification is claimed.

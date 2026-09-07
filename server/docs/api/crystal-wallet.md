# Crystal wallet contracts — local implementation, no release policy

Accepted policy source: planning `docs/ops/crystal-source-policy.md`, 2026-09-07.
Migration 0023 and this API are local only. No currency identifier, rate, recovery
price, reward table or store SKU is seeded. No actual store adapter is installed.

## Unity DTOs

`GET /v1/wallet/crystals` requires the existing authenticated active account/session
and admission contract. Missing policy returns **503** with
`{ "accepted": false, "errorCode": "crystal_policy_unconfigured" }`.
Show unavailable/pending configuration, not a fabricated zero balance.

Configured response fields:

| Field | Type / meaning |
| --- | --- |
| currencyId | string, server-configured identifier; no final identifier chosen |
| policyVersion | string, immutable policy ID required by wallet mutations |
| freeBalance / paidBalance / totalBalance | nonnegative safe integers; total is FREE + PAID |
| spendOrder | `["FREE", "PAID"]` |
| ticketExchange | null or `{ticketItemId, crystalsPerTicket, maxTickets}` |
| purchaseConfigured | boolean: product mapping exists, not proof of a real store connection |

`POST /v1/wallet/crystals/tickets/exchange` body is exactly
`{policyVersion:string,ticketCount:positiveInteger}`.
`POST /v1/wallet/crystals/purchases/verify` body is exactly
`{policyVersion:string,productId:string,receipt:string}`.
Both require Idempotency-Key. Unknown fields, caller account/paid/amount flags,
query strings and oversized bodies are rejected. A missing exchange configuration
or purchase verifier returns 503. Mismatched policy, changed-body replay,
insufficient balance and competing spent balance return 409. Invalid store proof
returns 403. Request-body stream is bounded before JSON parsing.

Successful wallet mutation response fields are `accepted`, `operationId`,
`currencyId`, `policyVersion`, `freeBalance`, `paidBalance`, `totalBalance`,
`freeSpent`, `paidSpent`, `freeGranted`, `paidGranted`. Exchange adds
`ticketsGranted`, `ticketItemId`, `ticketBalanceAfter`. Purchase adds
`purchasedQuantity`, `purchaseLinked`. Paid spending is explicitly disclosed through
`paidSpent`. Retry the same request/key on uncertain completion; replay returns
the stored receipt even after configuration changes, not a new grant.

## Attendance and pass integration

Existing activity reward arrays now accept `{kind:"CRYSTAL",quantity:integer}`
alongside ITEM rewards. Currency ID comes only from the configured wallet policy.
Attendance, milestones, recovery rewards, free-pass and premium-pass crystal rewards
always create **FREE** lots. Premium-pass lots require the verified entitlement's
store order and expose `purchaseLinked:true`. A missing order link fails closed.
Only a verified configured store crystal purchase creates a PAID lot.

Crystal reward lines contain `kind:"CRYSTAL"`, `id`, `quantity`, `source:"FREE"`
and `purchaseLinked`. The claim adds `wallet` with the wallet mutation result.
Recovery can explicitly configure `{kind:"CRYSTAL",walletPolicyVersion,quantity,
maxPerCampaign,missedDaysOnly:true}`. Its `cost` response contains `kind`, `id`,
`quantity`, `freeSpent`, `paidSpent`. Entire cost, wallet grant, attendance state,
inventory rewards and all ledgers share one batch. The previous ITEM recovery is
a separate generic capability; it is not the new crystal wallet.

No tutorial/offline crystal grant or silent conversion of old login/first-clear
ticket receipts is added. Existing ticket inventory, grant history and gacha
probabilities/pity remain unchanged. Switching future reward policy requires
preserving the existing one-time claim identities to prevent duplicate compensation.

## Provenance and verification boundary

`crystal_lots` retains each FREE/PAID grant, source type/ref, optional order, initial
amount and remaining amount. `crystal_ledger` allocates each spend to concrete lots
and links downstream ticket exchanges/recovery. FREE lots are consumed before PAID.
Account mutation sequence uniqueness serializes economic writes and triggers batch
rollback/retry on concurrency; receipt fingerprint and business uniqueness prevent
double grants. The active policy is checked again inside the wallet batch.

`CRYSTAL_PURCHASE_VERIFIER` and `PASS_PURCHASE_VERIFIER` are trusted server adapters,
with no production default. They must verify real store identity/signature/API,
ownership, application/product/environment, current purchase/refund state and time.
Crystal purchase additionally requires a verified PURCHASED state. Store transactions
are globally unique per provider/application across pass and crystal purchases.
Opaque receipts are not persisted; request hashes and verified order identity are.

`reconcileCrystalOrder` is a **server-only** function requiring a trusted
`StoreRevocationVerifier`. There is no public refund route, configured webhook or
live store integration. Verified REFUNDED/REVOKED orders reconcile once: only
remaining lots linked to that order are removed, premium entitlement is revoked,
spent FREE/PAID amounts and downstream operations are recorded for support. It does
not remove exchanged tickets, recruited operators, unrelated rewards or impose a
restriction. Reconciliation uses historical order/lot policy provenance and works
when the current wallet policy is disabled, or when a pass had no crystal grants.

## Validation and limits

Local real SQL.js/API tests cover FREE/PAID separation, FREE premium order links,
free-first spend, replay/changed payload, insufficient/concurrent balance, atomic
recovery rollback, missing verifier, order replay, one-time refund/deficit retention,
disabled-policy historical refunds and entitlement-only revocation. Production
provider adapters, refunds/webhook authentication, release policies and automatic
restriction rules remain unconfigured/unimplemented. No remote migration/deploy.

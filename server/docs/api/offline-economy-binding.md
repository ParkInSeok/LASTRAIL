# S10 offline economy binding

The existing GET /v1/offline-rewards/preview and POST /v1/offline-rewards/claim now
read the immutable economy associated with the active balance version. For approved
act1.economy.2026-09-09.001 this is hourly credits24/parts2/data1/EXP20, existing
eight story progress multipliers, caps4/5/6/7/8/9/10/12h and minimum600seconds.
The six story operation IDs remain unchanged; four operation categories are a
separate unresolved catalog and do not remap these progression keys.

An absent active binding retains the legacy100/50/30/20 policy. This code does not
activate or seed the new version. A present invalid hash/config fails closed.
The adapter uses the existing minute-based calculator; future policies whose
minimumSeconds is not a positive whole minute are not supported by that calculator.
The approved600-second policy is supported and tested.

Preview and claim add nullable balanceVersion:number, economyVersion:string and
economyHash:string at the response root. Null economy fields identify the legacy
unbound path. Claim receipt JSON retains the same metadata and limits. Existing
DUPLICATE_REPLAY behavior (accepted:false, no new rewardLines) is preserved and
returns the stored policy metadata without consulting the current active policy.
This is a completed old claim, not a failed new grant.

The claim captures account mutation sequence before reading elapsed baseline and
balances, fences active balance/binding in the batch, and retries uniqueness races.
Policy races return409 offline_reward_policy_changed with no partial credit.
Existing minimum-duration and currency rounding behavior are unchanged. Claim
advances the baseline and writes currency/reward/idempotency ledgers atomically.

Migration0028 adds account_offline_reward_state.last_reward_transaction_id. New
claims link to their exact pinned receipt; historical rows stay NULL and are not
reinterpreted from a later binding. last_balance_config_version is also recorded.
The receipt is the authoritative historical economy version/hash, not a mutable
lookup of whatever binding currently exists.

Five new SQLite/actual-auth HTTP tests verify approved hourly values, receipt and
state linkage, replay after policy removal, legacy unbound behavior, policy race
rollback, concurrent different keys and the final story12-hour/3x cap. Existing
offline HTTP and unit contract tests also run. No remote schema, policy or deploy
was changed.

# Attendance elite-ticket evidence and unapproved next contract

Source inspection2026-09-10: server apps/packages and shared Unity Scripts have no
identified elite-ticket item/consumer. General gacha spends cur_command_ticket and
updates its ordinary/pickup pity state. That is not the missing guaranteed ticket.
Plan confirmed the missing contract; its decision proposal remains unapproved.

Existing duplicate code in game-api index.ts: getOperatorShardItemId returns
`cur_operator_token:${operatorId}`; getDuplicateConversionQuantity returns50 for
ELITE_OPERATOR (30 for other operator rarity). A duplicate adds that operator's
token to account_items and item_ledger reason GACHA_DUPLICATE_CONVERSION, while
response ID is cur_operator_token with separate operatorId and quantity50.
No re-roll/new-unowned guarantee follows. This is actual code evidence, not new
approval of a guaranteed-ticket pool or reuse of ordinary pity.

account_items (migration0013) stores account_id/item_id/quantity/updated_at and has
no expiry column/job in this inspected path. This proves only current generic
inventory persistence; it does not establish an approved expiration policy for a
new entitlement. Ordinary quest mailbox14days must not be copied to it.

Proposed identity, **not created or enabled**: `item_attendance_elite_ticket` with
entitlement policy `attendance-elite-v1`. Check full future catalog collisions
before adoption. Aggregate item balance alone cannot preserve multiple historic
pool versions. Use grant-receipt-linked lots carrying entitlement/pool/duplicate/
pity policy versions and remaining count; keep explicit version identity when
displaying aggregate stock. Suggested no expiry needs approval, not inference from
the absence of an account_items expiry field.

After user decision, a separate explicit consume command should select owned lot
and expected revision with idempotency key; one atomic batch consumes one right,
pins server RNG result and policy, grants actual operator or its approved50tokens,
and persists the response. Replay never draws again. Pool equality/initial-five
membership/no-pickup/no-ordinary-pity changes are Plan's current proposals only.
The consumer must neither debit cur_command_ticket nor silently update its pity.
No pool/member list, entropy policy, identity or entitlement table is implemented
by this review. S08 fixture item grants do not complete this pending consumer.

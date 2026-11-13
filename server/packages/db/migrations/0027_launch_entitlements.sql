-- Finite source receipts; no historical balance changes or retroactive grants.
CREATE TABLE account_launch_entitlements (
 account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
 source_key TEXT NOT NULL,
 reward_id TEXT NOT NULL CHECK(reward_id IN ('cur_command_ticket','cur_special_core')),
 quantity INTEGER NOT NULL CHECK(quantity > 0),
 claim_id TEXT NOT NULL,
 created_at INTEGER NOT NULL,
 PRIMARY KEY(account_id,source_key)
);

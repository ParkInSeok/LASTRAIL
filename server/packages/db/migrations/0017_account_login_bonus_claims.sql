CREATE TABLE account_login_bonus_claims (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  bonus_id TEXT NOT NULL,
  reward_transaction_id TEXT NOT NULL REFERENCES reward_transactions(id) ON DELETE RESTRICT,
  claimed_at INTEGER NOT NULL,
  idempotency_key TEXT NOT NULL,
  reward_json TEXT NOT NULL,
  PRIMARY KEY(account_id, bonus_id),
  UNIQUE(reward_transaction_id)
);

CREATE INDEX idx_account_login_bonus_claims_bonus_claimed
  ON account_login_bonus_claims(bonus_id, claimed_at);

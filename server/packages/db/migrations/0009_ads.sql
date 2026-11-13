CREATE TABLE ad_reward_transactions (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  run_id TEXT REFERENCES runs(id) ON DELETE SET NULL,
  provider TEXT NOT NULL CHECK (provider IN ('ADMOB')),
  provider_transaction_id TEXT NOT NULL,
  intent_payload_hash TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('INTENT_CREATED','VERIFIED','GRANTED','REJECTED')),
  base_reward_json TEXT NOT NULL,
  bonus_reward_json TEXT,
  mutation_id TEXT REFERENCES account_mutations(id) ON DELETE RESTRICT,
  created_at INTEGER NOT NULL,
  verified_at INTEGER,
  granted_at INTEGER,
  UNIQUE(provider, provider_transaction_id),
  UNIQUE(run_id, provider)
);

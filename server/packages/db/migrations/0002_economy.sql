CREATE TABLE account_currencies (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  currency TEXT NOT NULL CHECK (currency IN ('CREDITS','MACHINE_PARTS','TACTICAL_DATA','SPECIAL_CORE','OPERATOR_EXP')),
  balance INTEGER NOT NULL CHECK (balance >= 0),
  updated_at INTEGER NOT NULL,
  PRIMARY KEY(account_id, currency)
);

CREATE TABLE economy_transactions (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  reason TEXT NOT NULL,
  source_type TEXT NOT NULL,
  source_id TEXT,
  created_at INTEGER NOT NULL
);

CREATE TABLE currency_ledger (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  transaction_id TEXT NOT NULL REFERENCES economy_transactions(id) ON DELETE RESTRICT,
  currency TEXT NOT NULL CHECK (currency IN ('CREDITS','MACHINE_PARTS','TACTICAL_DATA','SPECIAL_CORE','OPERATOR_EXP')),
  delta INTEGER NOT NULL,
  balance_after INTEGER NOT NULL CHECK (balance_after >= 0),
  created_at INTEGER NOT NULL
);

CREATE TABLE fuel_state (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  fuel INTEGER NOT NULL CHECK (fuel >= 0),
  max_fuel INTEGER NOT NULL CHECK (max_fuel >= 0),
  last_regen_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE fuel_ledger (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  delta INTEGER NOT NULL,
  fuel_after INTEGER NOT NULL CHECK (fuel_after >= 0),
  reason TEXT NOT NULL,
  created_at INTEGER NOT NULL
);

CREATE TABLE reward_transactions (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  run_id TEXT,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  balance_config_version INTEGER,
  reward_json TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  UNIQUE(run_id)
);

CREATE TABLE idempotency_records (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  idempotency_key TEXT NOT NULL,
  operation TEXT NOT NULL,
  result_ref TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  UNIQUE(account_id, operation, idempotency_key)
);

CREATE TABLE account_operators (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  operator_id TEXT NOT NULL,
  skill_training INTEGER NOT NULL DEFAULT 1 CHECK (skill_training BETWEEN 1 AND 5),
  promotion INTEGER NOT NULL DEFAULT 0 CHECK (promotion BETWEEN 0 AND 3),
  acquired_at INTEGER NOT NULL,
  revision INTEGER NOT NULL DEFAULT 1 CHECK (revision > 0),
  last_mutation_id TEXT REFERENCES account_mutations(id) ON DELETE RESTRICT,
  PRIMARY KEY(account_id, operator_id)
);

CREATE TABLE operator_training_core_slots (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  slot_index INTEGER NOT NULL CHECK (slot_index BETWEEN 0 AND 4),
  operator_id TEXT NOT NULL,
  trained_level INTEGER NOT NULL DEFAULT 1 CHECK (trained_level >= 1),
  invested_operator_exp INTEGER NOT NULL DEFAULT 0 CHECK (invested_operator_exp >= 0),
  revision INTEGER NOT NULL DEFAULT 1 CHECK (revision > 0),
  last_mutation_id TEXT REFERENCES account_mutations(id) ON DELETE RESTRICT,
  PRIMARY KEY(account_id, slot_index),
  UNIQUE(account_id, operator_id),
  FOREIGN KEY(account_id, operator_id) REFERENCES account_operators(account_id, operator_id) ON DELETE RESTRICT
);

CREATE TABLE account_train (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  train_id TEXT NOT NULL,
  revision INTEGER NOT NULL DEFAULT 1,
  updated_at INTEGER NOT NULL
);

CREATE TABLE account_carriages (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  carriage_id TEXT NOT NULL,
  level INTEGER NOT NULL DEFAULT 1 CHECK (level >= 1),
  revision INTEGER NOT NULL DEFAULT 1,
  PRIMARY KEY(account_id, carriage_id)
);

CREATE TABLE account_turrets (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  turret_id TEXT NOT NULL,
  level INTEGER NOT NULL DEFAULT 1 CHECK (level >= 1),
  revision INTEGER NOT NULL DEFAULT 1,
  PRIMARY KEY(account_id, turret_id)
);

CREATE TABLE commander_presets (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  preset_json TEXT NOT NULL,
  revision INTEGER NOT NULL DEFAULT 1,
  updated_at INTEGER NOT NULL
);

CREATE TABLE account_unlocks (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  unlock_id TEXT NOT NULL,
  unlocked_at INTEGER NOT NULL,
  PRIMARY KEY(account_id, unlock_id)
);

CREATE TABLE progression_ledger (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  progression_type TEXT NOT NULL,
  target_id TEXT NOT NULL,
  delta_json TEXT NOT NULL,
  created_at INTEGER NOT NULL
);

CREATE TABLE account_first_clears (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  operation_key TEXT NOT NULL,
  risk TEXT NOT NULL,
  cleared_at INTEGER NOT NULL,
  run_id TEXT,
  PRIMARY KEY(account_id, operation_key, risk)
);

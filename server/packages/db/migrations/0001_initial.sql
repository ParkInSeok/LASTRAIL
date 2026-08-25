PRAGMA foreign_keys = ON;

CREATE TABLE accounts (
  id TEXT PRIMARY KEY,
  status TEXT NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE','SUSPENDED','BANNED','DELETED')),
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE auth_identities (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  provider TEXT NOT NULL CHECK (provider IN ('GUEST','GOOGLE')),
  provider_subject TEXT NOT NULL,
  linked_at INTEGER NOT NULL,
  UNIQUE(provider, provider_subject)
);

CREATE TABLE auth_sessions (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  refresh_token_hash TEXT NOT NULL,
  family_id TEXT NOT NULL,
  generation INTEGER NOT NULL CHECK (generation >= 0),
  status TEXT NOT NULL CHECK (status IN ('ACTIVE','ROTATED','REUSED','REVOKED','EXPIRED')),
  created_at INTEGER NOT NULL,
  expires_at INTEGER NOT NULL,
  rotated_at INTEGER,
  UNIQUE(family_id, generation)
);

CREATE TABLE player_profiles (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  display_name TEXT,
  commander_xp INTEGER NOT NULL DEFAULT 0 CHECK (commander_xp >= 0),
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE account_runtime_state (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  protection_mode TEXT NOT NULL DEFAULT 'NORMAL' CHECK (protection_mode IN ('NORMAL','ELEVATED','HIGH','LOCKDOWN')),
  maintenance_until INTEGER,
  updated_at INTEGER NOT NULL
);

CREATE TABLE account_mutations (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  sequence INTEGER NOT NULL CHECK (sequence > 0),
  mutation_type TEXT NOT NULL,
  idempotency_key TEXT,
  created_at INTEGER NOT NULL,
  UNIQUE(account_id, sequence)
);

CREATE TABLE account_tutorial_state (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  status TEXT NOT NULL CHECK (status IN ('NOT_STARTED','IN_PROGRESS','COMPLETED')),
  completed_at INTEGER,
  updated_at INTEGER NOT NULL
);

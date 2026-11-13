-- Definitions are deliberately empty. Publishing approved policies is a separate operation.
CREATE TABLE activity_definitions (
  id TEXT PRIMARY KEY,
  policy_json TEXT NOT NULL CHECK (json_valid(policy_json))
);
CREATE TRIGGER activity_definitions_immutable_update BEFORE UPDATE ON activity_definitions
BEGIN SELECT RAISE(ABORT, 'activity definitions are immutable; use a new id'); END;
CREATE TRIGGER activity_definitions_immutable_delete BEFORE DELETE ON activity_definitions
BEGIN SELECT RAISE(ABORT, 'activity definitions are immutable'); END;

CREATE TABLE activity_claims (
  id TEXT PRIMARY KEY REFERENCES reward_transactions(id) ON DELETE RESTRICT,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  activity_id TEXT NOT NULL REFERENCES activity_definitions(id) ON DELETE RESTRICT,
  claim_key TEXT NOT NULL,
  operation TEXT NOT NULL,
  idempotency_key TEXT NOT NULL,
  request_hash TEXT NOT NULL,
  response_json TEXT NOT NULL CHECK (json_valid(response_json)),
  created_at INTEGER NOT NULL,
  UNIQUE(account_id, activity_id, claim_key),
  UNIQUE(account_id, operation, idempotency_key)
);

CREATE TABLE attendance_visits (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  activity_id TEXT NOT NULL REFERENCES activity_definitions(id) ON DELETE RESTRICT,
  day_index INTEGER NOT NULL,
  claim_id TEXT NOT NULL UNIQUE REFERENCES activity_claims(id) ON DELETE RESTRICT,
  visited_at INTEGER NOT NULL,
  PRIMARY KEY(account_id, activity_id, day_index)
);

CREATE TABLE pass_xp_events (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  activity_id TEXT NOT NULL REFERENCES activity_definitions(id) ON DELETE RESTRICT,
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE RESTRICT,
  xp INTEGER NOT NULL CHECK (xp > 0 AND xp <= 2147483647),
  claim_id TEXT NOT NULL UNIQUE REFERENCES activity_claims(id) ON DELETE RESTRICT,
  created_at INTEGER NOT NULL,
  PRIMARY KEY(account_id, activity_id, run_id)
);

CREATE TABLE pass_entitlements (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  activity_id TEXT NOT NULL REFERENCES activity_definitions(id) ON DELETE RESTRICT,
  provider TEXT NOT NULL,
  app_id TEXT NOT NULL,
  product_id TEXT NOT NULL,
  transaction_id TEXT NOT NULL,
  verified_at INTEGER NOT NULL,
  expires_at INTEGER NOT NULL,
  revoked_at INTEGER,
  claim_id TEXT NOT NULL UNIQUE REFERENCES activity_claims(id) ON DELETE RESTRICT,
  PRIMARY KEY(account_id, activity_id),
  UNIQUE(provider, app_id, transaction_id),
  CHECK (expires_at > verified_at)
);

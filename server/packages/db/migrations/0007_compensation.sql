CREATE TABLE compensations (
  id TEXT PRIMARY KEY,
  status TEXT NOT NULL CHECK (status IN ('DRAFT','PREPARED','EXECUTING','COMPLETED','CANCELLED')),
  title TEXT NOT NULL,
  reward_spec_json TEXT NOT NULL,
  recipient_snapshot_hash TEXT,
  prepared_at INTEGER,
  created_by TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE compensation_recipients (
  compensation_id TEXT NOT NULL REFERENCES compensations(id) ON DELETE CASCADE,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  status TEXT NOT NULL CHECK (status IN ('PENDING','GRANTED','FAILED','SKIPPED')),
  mutation_id TEXT REFERENCES account_mutations(id) ON DELETE RESTRICT,
  attempt_count INTEGER NOT NULL DEFAULT 0 CHECK (attempt_count >= 0),
  granted_at INTEGER,
  last_error_code TEXT,
  PRIMARY KEY(compensation_id, account_id)
);

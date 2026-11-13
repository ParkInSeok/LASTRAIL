-- Browser credentials have their own namespace and occupancy. No game auth_sessions row is created or revoked.
CREATE TABLE web_sessions (
  token_hash TEXT PRIMARY KEY,
  csrf_hash TEXT NOT NULL,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  created_at INTEGER NOT NULL,
  expires_at INTEGER NOT NULL,
  revoked_at INTEGER,
  CHECK (expires_at > created_at)
);
CREATE INDEX idx_web_sessions_account_active ON web_sessions(account_id, expires_at) WHERE revoked_at IS NULL;

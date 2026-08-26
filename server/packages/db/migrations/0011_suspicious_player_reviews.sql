CREATE TABLE suspicious_player_reviews (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  status TEXT NOT NULL CHECK (status IN (
    'NEW',
    'REVIEWING',
    'CLEARED',
    'SCORE_EXCLUDED',
    'REWARD_HELD',
    'ESCALATED',
    'ACTIONED',
    'FALSE_POSITIVE'
  )),
  assigned_to TEXT REFERENCES admin_principals(id) ON DELETE SET NULL,
  reason TEXT,
  updated_by TEXT NOT NULL REFERENCES admin_principals(id) ON DELETE RESTRICT,
  updated_at INTEGER NOT NULL,
  created_at INTEGER NOT NULL
);

CREATE TABLE suspicious_player_review_history (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  from_status TEXT CHECK (from_status IN (
    'NEW',
    'REVIEWING',
    'CLEARED',
    'SCORE_EXCLUDED',
    'REWARD_HELD',
    'ESCALATED',
    'ACTIONED',
    'FALSE_POSITIVE'
  )),
  to_status TEXT NOT NULL CHECK (to_status IN (
    'NEW',
    'REVIEWING',
    'CLEARED',
    'SCORE_EXCLUDED',
    'REWARD_HELD',
    'ESCALATED',
    'ACTIONED',
    'FALSE_POSITIVE'
  )),
  reason TEXT,
  created_by TEXT NOT NULL REFERENCES admin_principals(id) ON DELETE RESTRICT,
  created_at INTEGER NOT NULL,
  request_id TEXT NOT NULL,
  audit_log_id TEXT REFERENCES admin_audit_logs(id) ON DELETE SET NULL
);

CREATE INDEX idx_suspicious_player_reviews_status_updated
  ON suspicious_player_reviews(status, updated_at);

CREATE INDEX idx_suspicious_player_review_history_account_created
  ON suspicious_player_review_history(account_id, created_at);

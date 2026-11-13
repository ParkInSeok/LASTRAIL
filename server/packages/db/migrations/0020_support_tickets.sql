CREATE TABLE support_tickets (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  category TEXT NOT NULL CHECK (category IN (
    'ACCOUNT_LOGIN',
    'RUN_REWARD',
    'GACHA_RECRUITMENT',
    'STORY_PROGRESS',
    'BUG_OTHER'
  )),
  status TEXT NOT NULL CHECK (status IN ('OPEN', 'IN_REVIEW', 'CLOSED')),
  body_text TEXT NOT NULL CHECK (length(body_text) BETWEEN 10 AND 2000),
  body_preview TEXT NOT NULL CHECK (length(body_preview) BETWEEN 1 AND 200),
  body_length INTEGER NOT NULL CHECK (body_length BETWEEN 10 AND 2000),
  app_version TEXT CHECK (app_version IS NULL OR length(app_version) BETWEEN 1 AND 64),
  app_version_code TEXT CHECK (app_version_code IS NULL OR length(app_version_code) BETWEEN 1 AND 19),
  client_composed_at INTEGER,
  client_submitted_at INTEGER,
  server_received_at INTEGER NOT NULL,
  idempotency_key TEXT NOT NULL CHECK (length(idempotency_key) BETWEEN 1 AND 128),
  request_hash TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL,
  UNIQUE(account_id, idempotency_key)
);

CREATE INDEX idx_support_tickets_account_received
  ON support_tickets(account_id, server_received_at DESC);

CREATE INDEX idx_support_tickets_status_received
  ON support_tickets(status, server_received_at DESC);

CREATE INDEX idx_support_tickets_category_received
  ON support_tickets(category, server_received_at DESC);

CREATE TABLE ops_error_logs (
  id TEXT PRIMARY KEY,
  occurred_at INTEGER NOT NULL CHECK (occurred_at >= 0),
  environment TEXT NOT NULL CHECK (environment IN ('DEV','PRD')),
  service TEXT NOT NULL CHECK (
    service IN (
      'admin_api',
      'game_api',
      'worker_cron',
      'migration_verify',
      'support_read_model',
      'integrity_pipeline'
    )
  ),
  severity TEXT NOT NULL CHECK (severity IN ('info','warning','error','critical')),
  error_code TEXT NOT NULL,
  message_summary TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('open','acknowledged','resolved','ignored')),
  source TEXT NOT NULL CHECK (
    source IN (
      'admin_api',
      'game_api',
      'worker_cron',
      'migration_verify',
      'support_read_model',
      'integrity_pipeline'
    )
  ),
  request_id TEXT,
  account_id TEXT REFERENCES accounts(id) ON DELETE SET NULL,
  run_id TEXT REFERENCES runs(id) ON DELETE SET NULL,
  reward_preview_id TEXT REFERENCES run_reward_previews(id) ON DELETE SET NULL,
  reward_claim_id TEXT REFERENCES reward_transactions(id) ON DELETE SET NULL,
  offline_reward_claim_id TEXT REFERENCES reward_transactions(id) ON DELETE SET NULL,
  gacha_batch_id TEXT REFERENCES gacha_pull_batches(id) ON DELETE SET NULL,
  gacha_banner_id TEXT REFERENCES gacha_banners(id) ON DELETE SET NULL,
  story_progress_id TEXT,
  integrity_event_id TEXT REFERENCES integrity_events(id) ON DELETE SET NULL,
  support_ticket_id TEXT,
  idempotency_operation TEXT,
  idempotency_key_hash TEXT,
  correlation_json TEXT NOT NULL,
  sanitized_context_json TEXT NOT NULL,
  retryable INTEGER NOT NULL CHECK (retryable IN (0,1)),
  masked INTEGER NOT NULL CHECK (masked IN (0,1)),
  payload_ref TEXT,
  trace_ref TEXT,
  resolved_at INTEGER,
  resolved_by TEXT REFERENCES admin_principals(id) ON DELETE SET NULL,
  resolution_note TEXT,
  created_at INTEGER NOT NULL CHECK (created_at >= 0),
  CHECK (created_at >= occurred_at),
  CHECK (
    (status IN ('resolved','ignored') AND resolved_at IS NOT NULL)
    OR (status IN ('open','acknowledged') AND resolved_at IS NULL)
  ),
  CHECK (resolved_at IS NULL OR resolved_at >= occurred_at)
);

CREATE INDEX idx_ops_error_logs_status_severity_created
  ON ops_error_logs(status, severity, created_at);

CREATE INDEX idx_ops_error_logs_account_status_created
  ON ops_error_logs(account_id, status, created_at);

CREATE INDEX idx_ops_error_logs_request_id
  ON ops_error_logs(request_id);

CREATE INDEX idx_ops_error_logs_run_id
  ON ops_error_logs(run_id);

CREATE INDEX idx_ops_error_logs_gacha_batch_id
  ON ops_error_logs(gacha_batch_id);

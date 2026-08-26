CREATE TABLE run_reward_previews (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE,
  balance_config_version INTEGER NOT NULL REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  status TEXT NOT NULL CHECK (status IN ('ACTIVE','CLAIMED','EXPIRED','INVALIDATED')),
  reward_plan_json TEXT NOT NULL,
  expires_at INTEGER NOT NULL,
  created_at INTEGER NOT NULL,
  claimed_at INTEGER,
  reward_claim_id TEXT REFERENCES reward_transactions(id) ON DELETE SET NULL,
  CHECK (expires_at > created_at),
  CHECK (
    (status = 'CLAIMED' AND claimed_at IS NOT NULL AND reward_claim_id IS NOT NULL)
    OR (status != 'CLAIMED' AND claimed_at IS NULL AND reward_claim_id IS NULL)
  ),
  UNIQUE(run_id),
  UNIQUE(reward_claim_id)
);

CREATE INDEX idx_run_reward_previews_account_status_expires
  ON run_reward_previews(account_id, status, expires_at);

CREATE INDEX idx_run_reward_previews_run_status
  ON run_reward_previews(run_id, status);

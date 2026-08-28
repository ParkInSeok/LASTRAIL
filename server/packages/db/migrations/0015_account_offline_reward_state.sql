CREATE TABLE account_offline_reward_state (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  last_offline_reward_claim_at INTEGER NOT NULL,
  last_story_progress_id TEXT NOT NULL,
  last_balance_config_version INTEGER REFERENCES balance_config_versions(version) ON DELETE SET NULL,
  last_narrative_version TEXT,
  last_previewed_at INTEGER,
  revision INTEGER NOT NULL CHECK (revision >= 0),
  updated_at INTEGER NOT NULL,
  CHECK (last_offline_reward_claim_at >= 0),
  CHECK (last_previewed_at IS NULL OR last_previewed_at >= 0),
  CHECK (updated_at >= 0)
);

CREATE INDEX idx_account_offline_reward_state_last_claimed
  ON account_offline_reward_state(last_offline_reward_claim_at);

CREATE INDEX idx_account_offline_reward_state_story_progress
  ON account_offline_reward_state(last_story_progress_id);

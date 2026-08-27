CREATE TABLE account_story_progress (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  act_id TEXT NOT NULL,
  highest_cleared_operation_id TEXT NOT NULL,
  highest_story_progress_id TEXT NOT NULL,
  cleared_operations_json TEXT NOT NULL,
  story_flags_json TEXT NOT NULL,
  narrative_version TEXT,
  last_progress_run_id TEXT REFERENCES runs(id) ON DELETE SET NULL,
  updated_at INTEGER NOT NULL
);

CREATE INDEX idx_account_story_progress_act_progress
  ON account_story_progress(act_id, highest_story_progress_id);

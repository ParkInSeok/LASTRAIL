-- Add only two approved result types; retain every row, FK, UNIQUE and index.
-- No incoming references to gacha_pull_results in migrations0001-0020.
CREATE TABLE gacha_pull_results_v21 (
  id TEXT PRIMARY KEY,
  batch_id TEXT NOT NULL REFERENCES gacha_pull_batches(id) ON DELETE CASCADE,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  banner_id TEXT NOT NULL REFERENCES gacha_banners(id) ON DELETE RESTRICT,
  pull_sequence INTEGER NOT NULL CHECK (pull_sequence > 0),
  result_rarity TEXT NOT NULL CHECK (result_rarity IN ('COMMON','RARE','ELITE')),
  result_type TEXT NOT NULL CHECK (
    result_type IN (
      'ELITE_OPERATOR',
      'RARE_OPERATOR',
      'ELITE_OPERATOR_SHARD',
      'RARE_OPERATOR_SHARD',
      'OPERATOR_TOKEN',
      'TRAINING_MATERIAL',
      'WORK_SKILL_MATERIAL',
      'RARE_OPERATOR_TOKEN',
      'TACTICAL_MATERIAL'
    )
  ),
  result_id TEXT NOT NULL,
  is_pickup INTEGER NOT NULL CHECK (is_pickup IN (0,1)),
  guarantee_type TEXT NOT NULL DEFAULT 'NONE' CHECK (
    guarantee_type IN (
      'NONE',
      'RARE_OR_HIGHER_10',
      'ELITE_30',
      'PICKUP_ELITE_60'
    )
  ),
  was_duplicate INTEGER NOT NULL CHECK (was_duplicate IN (0,1)),
  duplicate_conversion_json TEXT,
  operator_id TEXT,
  item_ledger_id TEXT REFERENCES item_ledger(id) ON DELETE RESTRICT,
  progression_ledger_id TEXT REFERENCES progression_ledger(id) ON DELETE RESTRICT,
  created_at INTEGER NOT NULL,
  UNIQUE(batch_id, pull_sequence)
);
INSERT INTO gacha_pull_results_v21 (id, batch_id, account_id, banner_id, pull_sequence, result_rarity, result_type, result_id, is_pickup, guarantee_type, was_duplicate, duplicate_conversion_json, operator_id, item_ledger_id, progression_ledger_id, created_at) SELECT id, batch_id, account_id, banner_id, pull_sequence, result_rarity, result_type, result_id, is_pickup, guarantee_type, was_duplicate, duplicate_conversion_json, operator_id, item_ledger_id, progression_ledger_id, created_at FROM gacha_pull_results;
DROP TABLE gacha_pull_results;
ALTER TABLE gacha_pull_results_v21 RENAME TO gacha_pull_results;
CREATE INDEX idx_gacha_pull_results_banner_rarity_created ON gacha_pull_results(banner_id, result_rarity, created_at);
CREATE INDEX idx_gacha_pull_results_account_created ON gacha_pull_results(account_id, created_at);

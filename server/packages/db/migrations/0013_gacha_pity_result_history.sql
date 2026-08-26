CREATE TABLE account_items (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  item_id TEXT NOT NULL,
  quantity INTEGER NOT NULL CHECK (quantity >= 0),
  updated_at INTEGER NOT NULL,
  PRIMARY KEY(account_id, item_id)
);

CREATE TABLE item_ledger (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  item_id TEXT NOT NULL,
  delta INTEGER NOT NULL,
  quantity_after INTEGER NOT NULL CHECK (quantity_after >= 0),
  reason TEXT NOT NULL,
  source_type TEXT NOT NULL,
  source_id TEXT,
  created_at INTEGER NOT NULL
);

CREATE TABLE gacha_banners (
  id TEXT PRIMARY KEY,
  banner_type TEXT NOT NULL CHECK (banner_type IN ('STANDARD','PICKUP')),
  status TEXT NOT NULL CHECK (status IN ('DRAFT','ACTIVE','ENDED','DISABLED')),
  balance_config_version INTEGER NOT NULL REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  pickup_operator_id TEXT,
  starts_at INTEGER NOT NULL,
  ends_at INTEGER,
  pity_config_json TEXT NOT NULL,
  pool_snapshot_json TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL,
  CHECK (ends_at IS NULL OR ends_at > starts_at),
  CHECK (
    (banner_type = 'PICKUP' AND pickup_operator_id IS NOT NULL)
    OR (banner_type != 'PICKUP' AND pickup_operator_id IS NULL)
  )
);

CREATE TABLE player_gacha_pity (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  banner_id TEXT NOT NULL REFERENCES gacha_banners(id) ON DELETE RESTRICT,
  total_pull_count INTEGER NOT NULL DEFAULT 0 CHECK (total_pull_count >= 0),
  elite_pity_count INTEGER NOT NULL DEFAULT 0 CHECK (elite_pity_count >= 0),
  pickup_elite_pity_count INTEGER NOT NULL DEFAULT 0 CHECK (pickup_elite_pity_count >= 0),
  rare_or_higher_pity_count INTEGER NOT NULL DEFAULT 0 CHECK (rare_or_higher_pity_count >= 0),
  revision INTEGER NOT NULL DEFAULT 1 CHECK (revision > 0),
  last_batch_id TEXT,
  updated_at INTEGER NOT NULL,
  PRIMARY KEY(account_id, banner_id)
);

CREATE TABLE gacha_pull_batches (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  banner_id TEXT NOT NULL REFERENCES gacha_banners(id) ON DELETE RESTRICT,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  idempotency_key TEXT NOT NULL,
  pull_count INTEGER NOT NULL CHECK (pull_count IN (1,10)),
  cost_item_id TEXT NOT NULL,
  cost_quantity INTEGER NOT NULL CHECK (cost_quantity > 0),
  cost_ledger_id TEXT REFERENCES item_ledger(id) ON DELETE RESTRICT,
  pre_pity_json TEXT NOT NULL,
  post_pity_json TEXT NOT NULL,
  rng_seed_hash TEXT NOT NULL,
  result_summary_json TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  CHECK (cost_quantity = pull_count),
  UNIQUE(account_id, banner_id, idempotency_key)
);

CREATE TABLE gacha_pull_results (
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
      'WORK_SKILL_MATERIAL'
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

CREATE INDEX idx_item_ledger_account_item_created
  ON item_ledger(account_id, item_id, created_at);

CREATE INDEX idx_gacha_pull_batches_account_created
  ON gacha_pull_batches(account_id, created_at);

CREATE INDEX idx_gacha_pull_batches_banner_created
  ON gacha_pull_batches(banner_id, created_at);

CREATE INDEX idx_gacha_pull_results_banner_rarity_created
  ON gacha_pull_results(banner_id, result_rarity, created_at);

CREATE INDEX idx_gacha_pull_results_account_created
  ON gacha_pull_results(account_id, created_at);

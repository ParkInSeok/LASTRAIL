CREATE TABLE runs (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  preview_id TEXT REFERENCES operation_previews(id) ON DELETE RESTRICT,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  balance_config_version INTEGER NOT NULL REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  status TEXT NOT NULL CHECK (status IN ('ACTIVE','FINISH_PENDING','COMPLETED','FAILED','QUARANTINED','EXPIRED','ABANDONED')),
  started_at INTEGER NOT NULL,
  finished_at INTEGER,
  build_id TEXT REFERENCES client_builds(id) ON DELETE RESTRICT
);

CREATE TABLE run_status_history (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE,
  from_status TEXT,
  to_status TEXT NOT NULL,
  reason TEXT NOT NULL,
  created_at INTEGER NOT NULL
);

CREATE TABLE run_snapshots (
  run_id TEXT PRIMARY KEY REFERENCES runs(id) ON DELETE CASCADE,
  snapshot_json TEXT NOT NULL,
  created_at INTEGER NOT NULL
);

CREATE TABLE run_wave_plans (
  run_id TEXT PRIMARY KEY REFERENCES runs(id) ON DELETE CASCADE,
  wave_plan_json TEXT NOT NULL,
  checksum TEXT NOT NULL,
  created_at INTEGER NOT NULL
);

CREATE TABLE run_checkpoints (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE,
  checkpoint_wave INTEGER NOT NULL CHECK (checkpoint_wave IN (3,6,9,12,15)),
  payload_json TEXT NOT NULL,
  submitted_at INTEGER NOT NULL,
  UNIQUE(run_id, checkpoint_wave)
);

CREATE TABLE run_wave_results (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE,
  wave_index INTEGER NOT NULL CHECK (wave_index BETWEEN 1 AND 15),
  result_json TEXT NOT NULL,
  submitted_at INTEGER NOT NULL,
  UNIQUE(run_id, wave_index)
);

CREATE TABLE run_augment_history (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE,
  wave_index INTEGER NOT NULL CHECK (wave_index BETWEEN 1 AND 15),
  augment_json TEXT NOT NULL,
  created_at INTEGER NOT NULL
);

CREATE TABLE run_tactical_ledger (
  id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL REFERENCES runs(id) ON DELETE CASCADE,
  sequence INTEGER NOT NULL CHECK (sequence > 0),
  entry_json TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  UNIQUE(run_id, sequence)
);

CREATE TABLE run_final_results (
  run_id TEXT PRIMARY KEY REFERENCES runs(id) ON DELETE CASCADE,
  outcome TEXT NOT NULL CHECK (outcome IN ('VICTORY','FAILURE','ABANDONED')),
  cleared_waves INTEGER NOT NULL CHECK (cleared_waves BETWEEN 0 AND 15),
  payload_json TEXT NOT NULL,
  submitted_at INTEGER NOT NULL
);

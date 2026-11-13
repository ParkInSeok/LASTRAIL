CREATE TABLE validator_versions (
  id TEXT PRIMARY KEY,
  version TEXT NOT NULL UNIQUE,
  config_json TEXT NOT NULL,
  created_at INTEGER NOT NULL
);

CREATE TABLE validator_runtime_configs (
  id TEXT PRIMARY KEY,
  validator_version_id TEXT NOT NULL REFERENCES validator_versions(id) ON DELETE RESTRICT,
  runtime_revision INTEGER NOT NULL CHECK (runtime_revision > 0),
  config_json TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  UNIQUE(validator_version_id, runtime_revision)
);

CREATE TABLE validator_runtime_active (
  singleton_id INTEGER PRIMARY KEY CHECK (singleton_id = 1),
  runtime_config_id TEXT NOT NULL REFERENCES validator_runtime_configs(id) ON DELETE RESTRICT,
  updated_at INTEGER NOT NULL
);

CREATE TABLE integrity_events (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  run_id TEXT REFERENCES runs(id) ON DELETE SET NULL,
  balance_config_version INTEGER REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  run_start_build_id TEXT REFERENCES client_builds(id) ON DELETE RESTRICT,
  submission_build_id TEXT REFERENCES client_builds(id) ON DELETE RESTRICT,
  original_validator_version_id TEXT REFERENCES validator_versions(id) ON DELETE RESTRICT,
  runtime_revision INTEGER NOT NULL,
  evaluated_validator_version_id TEXT REFERENCES validator_versions(id) ON DELETE RESTRICT,
  category TEXT NOT NULL,
  evidence_code TEXT NOT NULL,
  evidence_json TEXT NOT NULL,
  fingerprint TEXT NOT NULL,
  classification TEXT NOT NULL CHECK (classification IN ('SOFT_ANOMALY','HARD_VIOLATION')),
  created_at INTEGER NOT NULL,
  UNIQUE(fingerprint)
);

CREATE TABLE integrity_event_review_states (
  event_id TEXT PRIMARY KEY REFERENCES integrity_events(id) ON DELETE CASCADE,
  state TEXT NOT NULL CHECK (state IN ('OPEN','IN_REVIEW','RESOLVED')),
  assigned_to TEXT,
  updated_at INTEGER NOT NULL
);

CREATE TABLE integrity_event_resolutions (
  id TEXT PRIMARY KEY,
  event_id TEXT NOT NULL REFERENCES integrity_events(id) ON DELETE CASCADE,
  resolution TEXT NOT NULL CHECK (resolution IN ('CONFIRMED_VIOLATION','FALSE_POSITIVE','SERVER_DEFECT','CLIENT_DEFECT','BUG_EXPLOITATION','RESOLVED_NO_ACTION')),
  resolved_by TEXT NOT NULL,
  notes TEXT,
  created_at INTEGER NOT NULL
);

CREATE TABLE integrity_incidents (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  status TEXT NOT NULL CHECK (status IN ('OPEN','RESOLVED')),
  severity TEXT NOT NULL CHECK (severity IN ('LOW','MEDIUM','HIGH','CRITICAL')),
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE integrity_incident_events (
  incident_id TEXT NOT NULL REFERENCES integrity_incidents(id) ON DELETE CASCADE,
  event_id TEXT NOT NULL REFERENCES integrity_events(id) ON DELETE CASCADE,
  PRIMARY KEY(incident_id, event_id)
);

CREATE TABLE account_integrity_state (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  observation TEXT NOT NULL DEFAULT 'NORMAL' CHECK (observation IN ('NORMAL','WATCHED')),
  updated_at INTEGER NOT NULL
);

CREATE TABLE account_enforcements (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  type TEXT NOT NULL CHECK (type IN ('REWARD_RESTRICTED','TEMP_SUSPENDED','PERMANENT_BAN')),
  source TEXT NOT NULL CHECK (source IN ('AUTOMATED','ADMIN')),
  status TEXT NOT NULL CHECK (status IN ('ACTIVE','LIFTED','EXPIRED')),
  reason TEXT NOT NULL,
  created_by TEXT,
  created_at INTEGER NOT NULL,
  expires_at INTEGER,
  lifted_at INTEGER,
  CHECK (NOT (source = 'AUTOMATED' AND type = 'PERMANENT_BAN'))
);

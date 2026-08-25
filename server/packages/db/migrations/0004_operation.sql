CREATE TABLE balance_config_drafts (
  id TEXT PRIMARY KEY,
  base_version INTEGER,
  schema_version INTEGER NOT NULL CHECK (schema_version > 0),
  required_content_version INTEGER NOT NULL CHECK (required_content_version >= 0),
  config_json TEXT NOT NULL,
  checksum TEXT NOT NULL,
  revision INTEGER NOT NULL DEFAULT 1 CHECK (revision > 0),
  status TEXT NOT NULL CHECK (status IN ('DRAFT','PUBLISHED','DISCARDED')),
  created_by TEXT,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE balance_config_versions (
  version INTEGER PRIMARY KEY CHECK (version > 0),
  schema_version INTEGER NOT NULL CHECK (schema_version > 0),
  required_content_version INTEGER NOT NULL CHECK (required_content_version >= 0),
  checksum TEXT NOT NULL UNIQUE,
  config_json TEXT NOT NULL,
  published_at INTEGER NOT NULL,
  published_by TEXT NOT NULL,
  draft_id TEXT REFERENCES balance_config_drafts(id) ON DELETE RESTRICT
);

CREATE TABLE balance_config_publish_claims (
  id TEXT PRIMARY KEY,
  draft_id TEXT NOT NULL REFERENCES balance_config_drafts(id) ON DELETE RESTRICT,
  expected_revision INTEGER NOT NULL,
  expected_checksum TEXT NOT NULL,
  published_version INTEGER REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  created_at INTEGER NOT NULL,
  UNIQUE(draft_id, expected_revision, expected_checksum)
);

CREATE TABLE balance_config_activations (
  id TEXT PRIMARY KEY,
  version INTEGER NOT NULL REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  activation_type TEXT NOT NULL CHECK (activation_type IN ('PUBLISH','ROLLBACK')),
  activated_by TEXT NOT NULL,
  activated_at INTEGER NOT NULL,
  reason TEXT
);

CREATE TABLE balance_config_active (
  singleton_id INTEGER PRIMARY KEY CHECK (singleton_id = 1),
  version INTEGER NOT NULL REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  activation_id TEXT NOT NULL REFERENCES balance_config_activations(id) ON DELETE RESTRICT,
  updated_at INTEGER NOT NULL
);

CREATE TABLE client_builds (
  id TEXT PRIMARY KEY,
  platform TEXT NOT NULL CHECK (platform IN ('ANDROID')),
  version_name TEXT NOT NULL,
  build_number INTEGER NOT NULL CHECK (build_number > 0),
  content_version INTEGER NOT NULL CHECK (content_version >= 0),
  protocol_version INTEGER NOT NULL CHECK (protocol_version > 0),
  status TEXT NOT NULL CHECK (status IN ('ALLOWED','UPDATE_REQUIRED','BLOCKED')),
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL,
  UNIQUE(platform, build_number)
);

CREATE TABLE operation_previews (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  objective TEXT NOT NULL,
  risk TEXT NOT NULL,
  balance_config_version INTEGER NOT NULL REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  preview_json TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('ACTIVE','CONSUMED','EXPIRED','CANCELLED')),
  expires_at INTEGER NOT NULL,
  created_at INTEGER NOT NULL
);

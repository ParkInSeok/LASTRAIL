-- Empty version registry: no activation, rewards or existing investments rewritten.
CREATE TABLE economy_versions (
  version TEXT PRIMARY KEY,
  sha256 TEXT NOT NULL CHECK(length(sha256)=64),
  config_json TEXT NOT NULL CHECK(json_valid(config_json)),
  created_at INTEGER NOT NULL
);
CREATE TRIGGER economy_versions_no_update BEFORE UPDATE ON economy_versions
BEGIN SELECT RAISE(ABORT,'economy version immutable'); END;
CREATE TRIGGER economy_versions_no_delete BEFORE DELETE ON economy_versions
BEGIN SELECT RAISE(ABORT,'economy version immutable'); END;
CREATE TRIGGER economy_versions_no_replace BEFORE INSERT ON economy_versions
WHEN EXISTS(SELECT 1 FROM economy_versions WHERE version=NEW.version)
BEGIN SELECT RAISE(ABORT,'economy version immutable'); END;
CREATE TABLE economy_version_bindings (
  balance_config_version INTEGER PRIMARY KEY REFERENCES balance_config_versions(version) ON DELETE RESTRICT,
  economy_version TEXT NOT NULL REFERENCES economy_versions(version) ON DELETE RESTRICT
);
CREATE TRIGGER economy_binding_no_update BEFORE UPDATE ON economy_version_bindings
BEGIN SELECT RAISE(ABORT,'economy binding immutable'); END;
CREATE TRIGGER economy_binding_no_delete BEFORE DELETE ON economy_version_bindings
BEGIN SELECT RAISE(ABORT,'economy binding immutable'); END;
CREATE TRIGGER economy_binding_no_replace BEFORE INSERT ON economy_version_bindings
WHEN EXISTS(SELECT 1 FROM economy_version_bindings WHERE balance_config_version=NEW.balance_config_version)
BEGIN SELECT RAISE(ABORT,'economy binding immutable'); END;
CREATE TRIGGER economy_binding_before_runs BEFORE INSERT ON economy_version_bindings
WHEN EXISTS(SELECT 1 FROM runs WHERE balance_config_version=NEW.balance_config_version)
BEGIN SELECT RAISE(ABORT,'economy binding requires unused balance version'); END;
ALTER TABLE account_train ADD COLUMN level INTEGER NOT NULL DEFAULT 1 CHECK(level>=1);
CREATE TABLE growth_receipts (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  idempotency_key TEXT NOT NULL,
  request_hash TEXT NOT NULL,
  economy_version TEXT NOT NULL REFERENCES economy_versions(version) ON DELETE RESTRICT,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  response_json TEXT NOT NULL CHECK(json_valid(response_json)),
  created_at INTEGER NOT NULL,
  UNIQUE(account_id,idempotency_key)
);
CREATE TRIGGER growth_receipts_no_update BEFORE UPDATE ON growth_receipts
BEGIN SELECT RAISE(ABORT,'growth receipt immutable'); END;
-- A transactional assertion table; inserted/deleted within each atomic batch.
CREATE TABLE growth_write_guard (valid INTEGER NOT NULL CHECK(valid=1));

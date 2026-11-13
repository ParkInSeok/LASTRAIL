-- No runtime is enabled or seeded by this migration. Internal executable adapters
-- must verify a profile and its actual sources before original START can use it.
CREATE TABLE combat_runtime_sources (
 environment TEXT NOT NULL,
 account_id TEXT NOT NULL REFERENCES accounts(id),
 preview_id TEXT NOT NULL REFERENCES operation_previews(id),
 source_json TEXT NOT NULL,
 source_hash TEXT NOT NULL CHECK(length(source_hash)=64),
 profile_hash TEXT NOT NULL CHECK(length(profile_hash)=64),
 revision INTEGER NOT NULL CHECK(revision=1),
 enabled INTEGER NOT NULL CHECK(enabled IN (0,1)),
 PRIMARY KEY(environment,account_id,preview_id)
);
CREATE TABLE combat_runtime_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER combat_runtime_source_immutable_update BEFORE UPDATE ON combat_runtime_sources
WHEN NEW.environment IS NOT OLD.environment OR NEW.account_id IS NOT OLD.account_id OR NEW.preview_id IS NOT OLD.preview_id
 OR NEW.source_json IS NOT OLD.source_json OR NEW.source_hash IS NOT OLD.source_hash OR NEW.profile_hash IS NOT OLD.profile_hash
 OR NEW.revision IS NOT OLD.revision OR NEW.enabled>OLD.enabled
BEGIN SELECT RAISE(ABORT,'combat_runtime_source_immutable'); END;
CREATE TRIGGER combat_runtime_source_immutable_delete BEFORE DELETE ON combat_runtime_sources
BEGIN SELECT RAISE(ABORT,'combat_runtime_source_immutable'); END;
CREATE TRIGGER combat_runtime_source_immutable_replace BEFORE INSERT ON combat_runtime_sources
WHEN EXISTS(SELECT 1 FROM combat_runtime_sources WHERE environment=NEW.environment AND account_id=NEW.account_id AND preview_id=NEW.preview_id
 AND (source_json<>NEW.source_json OR source_hash<>NEW.source_hash OR profile_hash<>NEW.profile_hash OR revision<>NEW.revision OR enabled<>NEW.enabled))
BEGIN SELECT RAISE(ABORT,'combat_runtime_source_immutable'); END;

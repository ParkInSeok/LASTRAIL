-- S1 owns trait presets only. Existing five-operator commander_presets are unchanged.
ALTER TABLE player_profiles ADD COLUMN commander_progression_revision INTEGER NOT NULL DEFAULT 1 CHECK(typeof(commander_progression_revision)='integer' AND commander_progression_revision BETWEEN 1 AND 9007199254740991);
CREATE TRIGGER commander_xp_insert BEFORE INSERT ON player_profiles WHEN typeof(NEW.commander_xp)<>'integer' OR NEW.commander_xp NOT BETWEEN 0 AND 9007199254740991 BEGIN SELECT RAISE(ABORT,'commander_xp_invalid'); END;
CREATE TRIGGER commander_xp_update BEFORE UPDATE OF commander_xp ON player_profiles WHEN typeof(NEW.commander_xp)<>'integer' OR NEW.commander_xp NOT BETWEEN 0 AND 9007199254740991 BEGIN SELECT RAISE(ABORT,'commander_xp_invalid'); END;
CREATE TRIGGER commander_xp_revision AFTER UPDATE OF commander_xp ON player_profiles WHEN NEW.commander_xp<>OLD.commander_xp BEGIN UPDATE player_profiles SET commander_progression_revision=OLD.commander_progression_revision+1 WHERE account_id=NEW.account_id; END;
CREATE TABLE commander_trait_presets(
 account_id TEXT NOT NULL REFERENCES accounts(id),slot_id INTEGER NOT NULL CHECK(slot_id BETWEEN 1 AND 3),revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
 rules_version TEXT NOT NULL,trait_definition_version TEXT NOT NULL,balance_version INTEGER NOT NULL REFERENCES balance_config_versions(version),ranks_json TEXT NOT NULL CHECK(json_valid(ranks_json)),updated_at INTEGER NOT NULL,
 PRIMARY KEY(account_id,slot_id)
);
CREATE TABLE commander_trait_selections(
 account_id TEXT PRIMARY KEY REFERENCES accounts(id),slot_id INTEGER NOT NULL,revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),updated_at INTEGER NOT NULL,
 FOREIGN KEY(account_id,slot_id) REFERENCES commander_trait_presets(account_id,slot_id)
);
CREATE TABLE commander_trait_receipts(
 account_id TEXT NOT NULL REFERENCES accounts(id),idempotency_key TEXT NOT NULL,command_hash TEXT NOT NULL,mutation_id TEXT NOT NULL REFERENCES account_mutations(id),response_json TEXT NOT NULL CHECK(json_valid(response_json)),created_at INTEGER NOT NULL,
 PRIMARY KEY(account_id,idempotency_key)
);
CREATE TRIGGER commander_receipt_update BEFORE UPDATE ON commander_trait_receipts BEGIN SELECT RAISE(ABORT,'commander_receipt_immutable'); END;
CREATE TRIGGER commander_receipt_delete BEFORE DELETE ON commander_trait_receipts BEGIN SELECT RAISE(ABORT,'commander_receipt_immutable'); END;
CREATE TABLE commander_trait_migrations(
 account_id TEXT NOT NULL REFERENCES accounts(id),slot_id INTEGER NOT NULL CHECK(slot_id BETWEEN 1 AND 3),source_definition_version TEXT NOT NULL,target_definition_version TEXT NOT NULL,
 before_revision INTEGER NOT NULL,after_revision INTEGER NOT NULL,refunded_points INTEGER NOT NULL CHECK(refunded_points>=0),receipt_key TEXT NOT NULL,
 PRIMARY KEY(account_id,slot_id,source_definition_version,target_definition_version),FOREIGN KEY(account_id,receipt_key) REFERENCES commander_trait_receipts(account_id,idempotency_key)
);
CREATE TRIGGER commander_migration_update BEFORE UPDATE ON commander_trait_migrations BEGIN SELECT RAISE(ABORT,'commander_migration_immutable'); END;
CREATE TRIGGER commander_migration_delete BEFORE DELETE ON commander_trait_migrations BEGIN SELECT RAISE(ABORT,'commander_migration_immutable'); END;
CREATE TABLE commander_trait_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER commander_guard_ok BEFORE INSERT ON commander_trait_guard WHEN NEW.valid=1 BEGIN SELECT RAISE(IGNORE); END;
CREATE TRIGGER commander_guard_no BEFORE INSERT ON commander_trait_guard WHEN NEW.valid<>1 OR NEW.valid IS NULL BEGIN SELECT RAISE(ABORT,'commander_state_changed'); END;

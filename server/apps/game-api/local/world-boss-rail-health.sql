-- Dedicated LOCAL trial database only; not an operational migration.
CREATE TABLE world_boss_rail_health_links(
 attempt_id TEXT PRIMARY KEY REFERENCES world_boss_rail_geometry_heads(attempt_id),
 run_id TEXT NOT NULL UNIQUE REFERENCES combat_damage_states(run_id), account_id TEXT NOT NULL REFERENCES accounts(id),
 profile_json TEXT NOT NULL, start_snapshot_json TEXT NOT NULL,
 shield_revision INTEGER NOT NULL CHECK(shield_revision>0), shield_json TEXT NOT NULL, initial_shield_json TEXT NOT NULL
);
CREATE TABLE world_boss_rail_health_receipts(
 attempt_id TEXT NOT NULL REFERENCES world_boss_rail_health_links(attempt_id), round_id TEXT NOT NULL,
 request_hash TEXT NOT NULL, response_json TEXT NOT NULL,
 PRIMARY KEY(attempt_id,round_id)
);
CREATE TABLE world_boss_rail_health_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER world_boss_rail_health_receipt_no_update BEFORE UPDATE ON world_boss_rail_health_receipts
BEGIN SELECT RAISE(ABORT,'rail_health_receipt_immutable'); END;
CREATE TRIGGER world_boss_rail_health_receipt_no_delete BEFORE DELETE ON world_boss_rail_health_receipts
BEGIN SELECT RAISE(ABORT,'rail_health_receipt_immutable'); END;
CREATE TRIGGER world_boss_rail_health_link_definition BEFORE UPDATE ON world_boss_rail_health_links
WHEN NEW.attempt_id<>OLD.attempt_id OR NEW.run_id<>OLD.run_id OR NEW.account_id<>OLD.account_id OR NEW.profile_json<>OLD.profile_json OR NEW.start_snapshot_json<>OLD.start_snapshot_json OR NEW.initial_shield_json<>OLD.initial_shield_json
BEGIN SELECT RAISE(ABORT,'rail_health_definition_immutable'); END;
-- Only new non-null B combat pins gain these immutable source fences. Legacy/null bytes are untouched.
CREATE TRIGGER b_combat_pin_no_update BEFORE UPDATE ON run_snapshots
WHEN json_valid(OLD.snapshot_json) AND json_extract(OLD.snapshot_json,'$.bCombatPin') IS NOT NULL
BEGIN SELECT RAISE(ABORT,'b_combat_pin_immutable'); END;
CREATE TRIGGER b_combat_pin_no_delete BEFORE DELETE ON run_snapshots
WHEN json_valid(OLD.snapshot_json) AND json_extract(OLD.snapshot_json,'$.bCombatPin') IS NOT NULL
BEGIN SELECT RAISE(ABORT,'b_combat_pin_immutable'); END;
CREATE TRIGGER b_combat_pin_no_replace BEFORE INSERT ON run_snapshots
WHEN EXISTS(SELECT 1 FROM run_snapshots WHERE run_id=NEW.run_id AND json_valid(snapshot_json) AND json_extract(snapshot_json,'$.bCombatPin') IS NOT NULL)
BEGIN SELECT RAISE(ABORT,'b_combat_pin_immutable'); END;
CREATE TRIGGER b_combat_pin_owner_no_update BEFORE UPDATE OF account_id,preview_id,balance_config_version ON runs
WHEN (NEW.account_id IS NOT OLD.account_id OR NEW.preview_id IS NOT OLD.preview_id OR NEW.balance_config_version IS NOT OLD.balance_config_version)
AND EXISTS(SELECT 1 FROM run_snapshots WHERE run_id=OLD.id AND json_valid(snapshot_json) AND json_extract(snapshot_json,'$.bCombatPin') IS NOT NULL)
BEGIN SELECT RAISE(ABORT,'b_combat_pin_owner_immutable'); END;
-- New ordinary STARTs own an explicit execution barrier. No legacy backfill.
CREATE TABLE ordinary_combat_phases (
 run_id TEXT PRIMARY KEY REFERENCES runs(id),
 account_id TEXT NOT NULL REFERENCES accounts(id),
 origin_hash TEXT NOT NULL,
 start_snapshot_json TEXT NOT NULL,
 phase TEXT NOT NULL CHECK(phase IN ('PREPARED','INITIALIZING','RUNNING')),
 revision INTEGER NOT NULL CHECK(revision BETWEEN 1 AND 3)
);
CREATE TABLE ordinary_combat_activation_receipts (
 run_id TEXT NOT NULL REFERENCES runs(id), request_id TEXT NOT NULL,
 request_hash TEXT NOT NULL, response_json TEXT NOT NULL, response_hash TEXT NOT NULL,
 PRIMARY KEY(run_id,request_id)
);
CREATE TRIGGER ordinary_activation_no_update BEFORE UPDATE ON ordinary_combat_activation_receipts
BEGIN SELECT RAISE(ABORT,'ordinary_activation_immutable'); END;
CREATE TRIGGER ordinary_activation_no_delete BEFORE DELETE ON ordinary_combat_activation_receipts
BEGIN SELECT RAISE(ABORT,'ordinary_activation_immutable'); END;
CREATE TRIGGER ordinary_phase_transition BEFORE UPDATE ON ordinary_combat_phases
WHEN NEW.run_id<>OLD.run_id OR NEW.account_id<>OLD.account_id OR NEW.origin_hash<>OLD.origin_hash
 OR NEW.start_snapshot_json<>OLD.start_snapshot_json
 OR NOT ((OLD.phase='PREPARED' AND NEW.phase='INITIALIZING' AND OLD.revision=1 AND NEW.revision=2)
 OR (OLD.phase='INITIALIZING' AND NEW.phase='RUNNING' AND OLD.revision=2 AND NEW.revision=3))
BEGIN SELECT RAISE(ABORT,'ordinary_phase_transition_invalid'); END;
CREATE TRIGGER ordinary_health_initialization_barrier BEFORE INSERT ON combat_damage_states
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase<>'INITIALIZING')
BEGIN SELECT RAISE(ABORT,'ordinary_phase_initialization_required'); END;
CREATE TRIGGER ordinary_health_change_barrier BEFORE UPDATE ON combat_damage_states
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase<>'RUNNING')
BEGIN SELECT RAISE(ABORT,'ordinary_phase_not_running'); END;
CREATE TRIGGER ordinary_stat_initialization_barrier BEFORE INSERT ON ordinary_run_stat_states
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase NOT IN ('INITIALIZING','RUNNING'))
BEGIN SELECT RAISE(ABORT,'ordinary_phase_not_running'); END;
CREATE TRIGGER ordinary_stat_change_barrier BEFORE UPDATE ON ordinary_run_stat_states
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase<>'RUNNING')
BEGIN SELECT RAISE(ABORT,'ordinary_phase_not_running'); END;
CREATE TRIGGER ordinary_tactical_barrier BEFORE INSERT ON run_tactical_ledger
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase<>'RUNNING')
BEGIN SELECT RAISE(ABORT,'ordinary_phase_not_running'); END;
CREATE TRIGGER ordinary_augment_barrier BEFORE INSERT ON run_augment_history
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase<>'RUNNING')
BEGIN SELECT RAISE(ABORT,'ordinary_phase_not_running'); END;
CREATE TRIGGER ordinary_augment_grant_barrier BEFORE INSERT ON solo_augment_grants
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase<>'RUNNING')
BEGIN SELECT RAISE(ABORT,'ordinary_phase_not_running'); END;
CREATE TRIGGER ordinary_augment_original_barrier BEFORE INSERT ON run_augment_effect_originals
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase<>'RUNNING')
BEGIN SELECT RAISE(ABORT,'ordinary_phase_not_running'); END;
CREATE TRIGGER ordinary_host_barrier BEFORE INSERT ON combat_host_report_states
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase<>'RUNNING')
BEGIN SELECT RAISE(ABORT,'ordinary_phase_not_running'); END;
CREATE TRIGGER ordinary_wave_result_barrier BEFORE INSERT ON run_wave_results
WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases WHERE run_id=NEW.run_id AND phase<>'RUNNING')
BEGIN SELECT RAISE(ABORT,'ordinary_phase_not_running'); END;

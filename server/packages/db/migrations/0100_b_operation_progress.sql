-- Metadata only: no catalog seed, capability, admission or economy changes.
CREATE TABLE b_operation_catalog(revision INTEGER PRIMARY KEY CHECK(revision>0),publication_json TEXT NOT NULL CHECK(json_valid(publication_json)));
CREATE TRIGGER b_operation_catalog_immutable_update BEFORE UPDATE ON b_operation_catalog BEGIN SELECT RAISE(ABORT,'b catalog immutable'); END;
CREATE TRIGGER b_operation_catalog_immutable_delete BEFORE DELETE ON b_operation_catalog BEGIN SELECT RAISE(ABORT,'b catalog immutable'); END;
CREATE TABLE b_operation_catalog_active(singleton_id INTEGER PRIMARY KEY CHECK(singleton_id=1),revision INTEGER NOT NULL REFERENCES b_operation_catalog(revision));
CREATE TABLE b_operation_wave_receipts(run_id TEXT NOT NULL REFERENCES runs(id),event_id TEXT NOT NULL,node_id TEXT NOT NULL,wave_ordinal INTEGER NOT NULL CHECK(wave_ordinal>0),request_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),PRIMARY KEY(run_id,event_id),UNIQUE(run_id,node_id,wave_ordinal));
CREATE TABLE b_operation_node_receipts(run_id TEXT NOT NULL REFERENCES runs(id),node_id TEXT NOT NULL,event_id TEXT NOT NULL,verification_id TEXT NOT NULL,binding_hash TEXT NOT NULL,PRIMARY KEY(run_id,node_id),UNIQUE(run_id,event_id));
CREATE TRIGGER b_wave_immutable_update BEFORE UPDATE ON b_operation_wave_receipts BEGIN SELECT RAISE(ABORT,'b wave immutable'); END;
CREATE TRIGGER b_wave_immutable_delete BEFORE DELETE ON b_operation_wave_receipts BEGIN SELECT RAISE(ABORT,'b wave immutable'); END;
CREATE TRIGGER b_node_immutable_update BEFORE UPDATE ON b_operation_node_receipts BEGIN SELECT RAISE(ABORT,'b node immutable'); END;
CREATE TRIGGER b_node_immutable_delete BEFORE DELETE ON b_operation_node_receipts BEGIN SELECT RAISE(ABORT,'b node immutable'); END;
-- Preserve the legacy 0..15 counter and all original event rows; B actual waves are separate.
ALTER TABLE objective_events ADD COLUMN b_completed_waves INTEGER CHECK(b_completed_waves BETWEEN 0 AND 1536);
CREATE TRIGGER b_objective_version_guard BEFORE INSERT ON objective_events
WHEN (NEW.b_completed_waves IS NOT NULL AND (NEW.mode<>'STORY_B_12' OR NEW.binding_version IS NOT 'operation.b-story12-binding.v1.20261004' OR NEW.kind<>'FINISH' OR NEW.waves<>0 OR (NEW.status<>'ELIGIBLE' AND NEW.b_completed_waves<>0))) OR (NEW.mode='STORY_B_12' AND NEW.b_completed_waves IS NULL)
BEGIN SELECT RAISE(ABORT,'b objective version conflict'); END;
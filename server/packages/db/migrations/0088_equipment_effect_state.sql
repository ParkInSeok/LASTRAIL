-- Current state is initialized only with a new verified original. No historical backfill.
CREATE TABLE run_equipment_effect_history (
  run_id TEXT NOT NULL REFERENCES runs(id),
  instance_id TEXT NOT NULL,
  effect_revision INTEGER NOT NULL CHECK(typeof(effect_revision)='integer' AND effect_revision>=1),
  instance_revision INTEGER NOT NULL CHECK(typeof(instance_revision)='integer' AND instance_revision>=1),
  source_ref TEXT NOT NULL,
  original_hash TEXT NOT NULL CHECK(length(original_hash)=64),
  snapshot_json TEXT NOT NULL CHECK(json_valid(snapshot_json)),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  PRIMARY KEY(run_id,instance_id,effect_revision)
);
CREATE TABLE run_equipment_effect_heads (
  run_id TEXT NOT NULL,
  instance_id TEXT PRIMARY KEY,
  revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision>=1),
  FOREIGN KEY(run_id,instance_id,revision) REFERENCES run_equipment_effect_history(run_id,instance_id,effect_revision)
);
CREATE TRIGGER equipment_effect_history_initial_guard BEFORE INSERT ON run_equipment_effect_history
BEGIN
  SELECT CASE WHEN NEW.effect_revision<>1 OR NEW.instance_revision<>1 OR NOT EXISTS (
    SELECT 1 FROM run_equipment_effect_originals o JOIN run_equipment_instances e ON e.instance_id=o.instance_id
    JOIN runs r ON r.id=o.run_id
    WHERE o.run_id=NEW.run_id AND o.instance_id=NEW.instance_id AND r.status='ACTIVE'
      AND e.run_id=o.run_id AND e.revision=1 AND o.source_item_id=NEW.source_ref AND o.payload_hash=NEW.original_hash
      AND json_extract(NEW.snapshot_json,'$.version')='equipment.effect-state.v1'
      AND json_remove(o.snapshot_json,'$.version')=json_remove(NEW.snapshot_json,'$.version')
  ) THEN RAISE(ABORT,'equipment_effect_initial_mismatch') END;
END;
CREATE TRIGGER equipment_effect_history_no_update BEFORE UPDATE ON run_equipment_effect_history
BEGIN SELECT RAISE(ABORT,'equipment_effect_history_immutable'); END;
CREATE TRIGGER equipment_effect_history_no_delete BEFORE DELETE ON run_equipment_effect_history
BEGIN SELECT RAISE(ABORT,'equipment_effect_history_immutable'); END;
CREATE TRIGGER equipment_effect_head_initial_guard BEFORE INSERT ON run_equipment_effect_heads
WHEN NEW.revision<>1
BEGIN SELECT RAISE(ABORT,'equipment_effect_initial_mismatch'); END;
-- Remove this guard only with the actual authorized effect-change transaction and history CAS.
CREATE TRIGGER equipment_effect_head_change_not_ready BEFORE UPDATE ON run_equipment_effect_heads
BEGIN SELECT RAISE(ABORT,'equipment_effect_change_not_ready'); END;
CREATE TRIGGER equipment_effect_head_no_delete BEFORE DELETE ON run_equipment_effect_heads
BEGIN SELECT RAISE(ABORT,'equipment_effect_head_immutable'); END;

-- Original effect provenance only. No legacy backfill or executable effect activation.
-- Do not cascade from the live item: consumed material provenance must survive a later merge.
CREATE TABLE run_equipment_effect_originals (
  instance_id TEXT PRIMARY KEY,
  run_id TEXT NOT NULL REFERENCES runs(id),
  source_item_id TEXT NOT NULL,
  snapshot_json TEXT NOT NULL CHECK(json_valid(snapshot_json)),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  UNIQUE(run_id,source_item_id)
);
CREATE TRIGGER equipment_effect_original_insert_guard BEFORE INSERT ON run_equipment_effect_originals
BEGIN
  SELECT CASE WHEN NOT EXISTS (
    SELECT 1 FROM run_equipment_instances e JOIN runs r ON r.id=e.run_id
    WHERE e.instance_id=NEW.instance_id AND e.run_id=NEW.run_id AND r.status='ACTIVE'
      AND e.source_item_id=NEW.source_item_id AND e.revision=1
      AND json_extract(NEW.snapshot_json,'$.version')='equipment.effect-original.v1'
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionId')=e.definition_id
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionVersion')=e.definition_version
      AND json_extract(NEW.snapshot_json,'$.equipment.star')=e.star
  ) THEN RAISE(ABORT,'equipment_effect_original_mismatch') END;
END;
CREATE TRIGGER equipment_effect_original_no_update BEFORE UPDATE ON run_equipment_effect_originals
BEGIN SELECT RAISE(ABORT,'equipment_effect_original_immutable'); END;
CREATE TRIGGER equipment_effect_original_no_delete BEFORE DELETE ON run_equipment_effect_originals
BEGIN SELECT RAISE(ABORT,'equipment_effect_original_immutable'); END;
CREATE TABLE equipment_effect_original_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

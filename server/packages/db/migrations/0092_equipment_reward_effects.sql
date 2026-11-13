-- Original resolved rewards fixed before claim; do not cascade from consumed live equipment.
CREATE TABLE run_equipment_reward_effects (
  run_id TEXT NOT NULL, item_id TEXT PRIMARY KEY REFERENCES run_equipment_reward_items(item_id),
  bundle_id TEXT NOT NULL, snapshot_json TEXT NOT NULL CHECK(json_valid(snapshot_json)),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  FOREIGN KEY(run_id,bundle_id) REFERENCES run_equipment_reward_bundles(run_id,bundle_id)
);
CREATE TRIGGER equipment_reward_effect_insert_guard BEFORE INSERT ON run_equipment_reward_effects
BEGIN
  SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM run_equipment_reward_items item JOIN runs r ON r.id=item.run_id
    WHERE item.run_id=NEW.run_id AND item.item_id=NEW.item_id AND item.bundle_id=NEW.bundle_id
      AND item.status='PENDING' AND r.status='ACTIVE'
      AND json_extract(NEW.snapshot_json,'$.version')='equipment.effect-original.v1'
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionId')=json_extract(item.item_json,'$.definitionId')
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionVersion')=json_extract(item.item_json,'$.definitionVersion')
      AND json_extract(NEW.snapshot_json,'$.equipment.star')=json_extract(item.item_json,'$.star'))
    THEN RAISE(ABORT,'equipment_reward_effect_mismatch') END;
END;
CREATE TRIGGER equipment_reward_effect_no_update BEFORE UPDATE ON run_equipment_reward_effects
BEGIN SELECT RAISE(ABORT,'equipment_reward_effect_immutable'); END;
CREATE TRIGGER equipment_reward_effect_no_delete BEFORE DELETE ON run_equipment_reward_effects
BEGIN SELECT RAISE(ABORT,'equipment_reward_effect_immutable'); END;

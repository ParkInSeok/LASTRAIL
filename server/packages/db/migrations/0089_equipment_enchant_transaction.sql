-- Extend the shared command namespace while preserving every original receipt.
CREATE TABLE run_equipment_receipts_next (
  run_id TEXT NOT NULL REFERENCES run_equipment_inventory(run_id) ON DELETE CASCADE,
  command_id TEXT NOT NULL CHECK(length(command_id) BETWEEN 1 AND 128),
  command_kind TEXT NOT NULL CHECK(command_kind IN ('SET_LOCK','CLAIM_ITEMS','DECLINE_REMAINING','PURCHASE_MERCHANT','EQUIP_OR_MOVE','ENCHANT_EQUIPMENT')),
  request_hash TEXT NOT NULL CHECK(length(request_hash)=64),
  response_json TEXT NOT NULL CHECK(json_valid(response_json)),
  PRIMARY KEY(run_id,command_id)
);
INSERT INTO run_equipment_receipts_next SELECT * FROM run_equipment_receipts;
DROP TABLE run_equipment_receipts;
ALTER TABLE run_equipment_receipts_next RENAME TO run_equipment_receipts;
CREATE TRIGGER run_equipment_receipt_immutable_update BEFORE UPDATE ON run_equipment_receipts
BEGIN SELECT RAISE(ABORT,'equipment_receipt_immutable'); END;
CREATE TRIGGER run_equipment_receipt_immutable_delete BEFORE DELETE ON run_equipment_receipts
BEGIN SELECT RAISE(ABORT,'equipment_receipt_immutable'); END;

CREATE TABLE equipment_enchant_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TABLE run_equipment_enchantment_changes (
  run_id TEXT NOT NULL REFERENCES runs(id),
  source_ref TEXT NOT NULL CHECK(length(source_ref) BETWEEN 1 AND 128),
  command_id TEXT NOT NULL,
  instance_id TEXT NOT NULL,
  effect_revision INTEGER NOT NULL CHECK(typeof(effect_revision)='integer' AND effect_revision>=2),
  instance_revision INTEGER NOT NULL CHECK(typeof(instance_revision)='integer' AND instance_revision>=2),
  inventory_revision INTEGER NOT NULL CHECK(typeof(inventory_revision)='integer' AND inventory_revision>=2),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  PRIMARY KEY(run_id,source_ref), UNIQUE(run_id,command_id), UNIQUE(run_id,instance_id,effect_revision)
);
CREATE TRIGGER equipment_enchantment_change_no_update BEFORE UPDATE ON run_equipment_enchantment_changes
BEGIN SELECT RAISE(ABORT,'equipment_enchantment_change_immutable'); END;
CREATE TRIGGER equipment_enchantment_change_no_delete BEFORE DELETE ON run_equipment_enchantment_changes
BEGIN SELECT RAISE(ABORT,'equipment_enchantment_change_immutable'); END;

DROP TRIGGER equipment_effect_history_initial_guard;
CREATE TRIGGER equipment_effect_history_initial_guard BEFORE INSERT ON run_equipment_effect_history
WHEN NEW.effect_revision=1
BEGIN
  SELECT CASE WHEN NEW.instance_revision<>1 OR NOT EXISTS (
    SELECT 1 FROM run_equipment_effect_originals o JOIN run_equipment_instances e ON e.instance_id=o.instance_id
    JOIN runs r ON r.id=o.run_id
    WHERE o.run_id=NEW.run_id AND o.instance_id=NEW.instance_id AND r.status='ACTIVE'
      AND e.run_id=o.run_id AND e.revision=1 AND o.source_item_id=NEW.source_ref AND o.payload_hash=NEW.original_hash
      AND json_extract(NEW.snapshot_json,'$.version')='equipment.effect-state.v1'
      AND json_remove(o.snapshot_json,'$.version')=json_remove(NEW.snapshot_json,'$.version')
  ) THEN RAISE(ABORT,'equipment_effect_initial_mismatch') END;
END;
-- Only an authorized enchant transaction may append a current-state revision.
-- Merge or other effect changes require their own explicit transaction and migration.
CREATE TRIGGER equipment_effect_history_enchant_guard BEFORE INSERT ON run_equipment_effect_history
WHEN NEW.effect_revision>1
BEGIN
  SELECT CASE WHEN NOT EXISTS (
    SELECT 1 FROM run_equipment_enchantment_changes c
    JOIN run_equipment_effect_heads h ON h.run_id=c.run_id AND h.instance_id=c.instance_id
    JOIN run_equipment_effect_history p ON p.run_id=h.run_id AND p.instance_id=h.instance_id AND p.effect_revision=h.revision
    JOIN run_equipment_instances e ON e.run_id=c.run_id AND e.instance_id=c.instance_id
    JOIN run_equipment_inventory i ON i.run_id=c.run_id
    JOIN runs r ON r.id=c.run_id JOIN accounts a ON a.id=r.account_id
    WHERE c.run_id=NEW.run_id AND c.instance_id=NEW.instance_id AND c.source_ref=NEW.source_ref
      AND c.effect_revision=NEW.effect_revision AND c.instance_revision=NEW.instance_revision
      AND c.payload_hash=NEW.payload_hash AND i.revision=c.inventory_revision
      AND h.revision=NEW.effect_revision-1 AND p.original_hash=NEW.original_hash
      AND e.revision=NEW.instance_revision AND e.locked=0 AND r.status='ACTIVE' AND a.status='ACTIVE'
      AND json_remove(p.snapshot_json,'$.enchantment')=json_remove(NEW.snapshot_json,'$.enchantment')
      AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar')=e.star
      AND json_extract(NEW.snapshot_json,'$.enchantment.effectId') IN
        ('EN01','EN02','EN03','EN04','EN05','EN06','EN07','EN08','EN09','EN10','EN11','EN12','EN13','EN14','EN15','EN16')
  ) THEN RAISE(ABORT,'equipment_effect_revision_invalid') END;
END;
DROP TRIGGER equipment_effect_head_change_not_ready;
CREATE TRIGGER equipment_effect_head_enchant_guard BEFORE UPDATE ON run_equipment_effect_heads
BEGIN
  SELECT CASE WHEN NEW.run_id<>OLD.run_id OR NEW.instance_id<>OLD.instance_id OR NEW.revision<>OLD.revision+1
    OR NOT EXISTS (SELECT 1 FROM run_equipment_effect_history n
      JOIN run_equipment_enchantment_changes c ON c.run_id=n.run_id AND c.instance_id=n.instance_id AND c.effect_revision=n.effect_revision
      JOIN run_equipment_instances e ON e.run_id=n.run_id AND e.instance_id=n.instance_id
      WHERE n.run_id=NEW.run_id AND n.instance_id=NEW.instance_id AND n.effect_revision=NEW.revision
        AND c.payload_hash=n.payload_hash AND e.revision=n.instance_revision)
    THEN RAISE(ABORT,'equipment_effect_revision_invalid') END;
END;

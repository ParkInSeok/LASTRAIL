CREATE TABLE run_equipment_receipts_next (
  run_id TEXT NOT NULL REFERENCES run_equipment_inventory(run_id) ON DELETE CASCADE,
  command_id TEXT NOT NULL CHECK(length(command_id) BETWEEN 1 AND 128),
  command_kind TEXT NOT NULL CHECK(command_kind IN ('SET_LOCK','CLAIM_ITEMS','DECLINE_REMAINING','PURCHASE_MERCHANT','EQUIP_OR_MOVE','ENCHANT_EQUIPMENT','MERGE_EQUIPMENT')),
  request_hash TEXT NOT NULL CHECK(length(request_hash)=64),
  response_json TEXT NOT NULL CHECK(json_valid(response_json)), PRIMARY KEY(run_id,command_id)
);
INSERT INTO run_equipment_receipts_next SELECT * FROM run_equipment_receipts;
DROP TABLE run_equipment_receipts;
ALTER TABLE run_equipment_receipts_next RENAME TO run_equipment_receipts;
CREATE TRIGGER run_equipment_receipt_immutable_update BEFORE UPDATE ON run_equipment_receipts
BEGIN SELECT RAISE(ABORT,'equipment_receipt_immutable'); END;
CREATE TRIGGER run_equipment_receipt_immutable_delete BEFORE DELETE ON run_equipment_receipts
BEGIN SELECT RAISE(ABORT,'equipment_receipt_immutable'); END;

CREATE TABLE equipment_merge_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TABLE run_equipment_merges (
  run_id TEXT NOT NULL REFERENCES runs(id), command_id TEXT NOT NULL,
  primary_instance_id TEXT NOT NULL, secondary_instance_id TEXT NOT NULL,
  primary_effect_revision INTEGER NOT NULL CHECK(primary_effect_revision>=1),
  secondary_effect_revision INTEGER NOT NULL CHECK(secondary_effect_revision>=1),
  instance_revision INTEGER NOT NULL CHECK(instance_revision>=2),
  inventory_revision INTEGER NOT NULL CHECK(inventory_revision>=2),
  result_snapshot_json TEXT NOT NULL CHECK(json_valid(result_snapshot_json)),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  PRIMARY KEY(run_id,command_id), UNIQUE(run_id,secondary_instance_id),
  UNIQUE(run_id,primary_instance_id,primary_effect_revision), CHECK(primary_instance_id<>secondary_instance_id)
);
CREATE TRIGGER equipment_merge_no_update BEFORE UPDATE ON run_equipment_merges
BEGIN SELECT RAISE(ABORT,'equipment_merge_immutable'); END;
CREATE TRIGGER equipment_merge_no_delete BEFORE DELETE ON run_equipment_merges
BEGIN SELECT RAISE(ABORT,'equipment_merge_immutable'); END;
-- Record both material bindings BEFORE consuming the auxiliary in the same transaction.
CREATE TRIGGER equipment_merge_material_guard BEFORE INSERT ON run_equipment_merges
BEGIN
  SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM run_equipment_instances a
    JOIN run_equipment_instances b ON b.run_id=a.run_id
    JOIN run_equipment_inventory i ON i.run_id=a.run_id
    JOIN run_equipment_effect_heads ah ON ah.run_id=a.run_id AND ah.instance_id=a.instance_id
    JOIN run_equipment_effect_heads bh ON bh.run_id=b.run_id AND bh.instance_id=b.instance_id
    JOIN run_equipment_effect_history p ON p.run_id=ah.run_id AND p.instance_id=ah.instance_id AND p.effect_revision=ah.revision
    JOIN runs r ON r.id=a.run_id JOIN accounts account ON account.id=r.account_id
    WHERE a.run_id=NEW.run_id AND a.instance_id=NEW.primary_instance_id AND b.instance_id=NEW.secondary_instance_id
      AND ah.revision=NEW.primary_effect_revision AND bh.revision=NEW.secondary_effect_revision
      AND a.revision=NEW.instance_revision-1 AND i.revision=NEW.inventory_revision-1
      AND a.locked=0 AND b.locked=0 AND a.star=b.star AND a.star<5 AND a.category_id=b.category_id
      AND r.status='ACTIVE' AND account.status='ACTIVE'
      AND json_extract(NEW.result_snapshot_json,'$.version')='equipment.effect-state.v1'
      AND json_extract(NEW.result_snapshot_json,'$.equipment.star')=a.star+1
      AND json_extract(NEW.result_snapshot_json,'$.equipment.definitionId')=a.definition_id
      AND json_extract(NEW.result_snapshot_json,'$.equipment.definitionVersion')=a.definition_version
      AND json_extract(NEW.result_snapshot_json,'$.enchantment') IS json_extract(p.snapshot_json,'$.enchantment'))
    THEN RAISE(ABORT,'equipment_merge_material_invalid') END;
END;

DROP TRIGGER equipment_effect_history_enchant_guard;
CREATE TRIGGER equipment_effect_history_enchant_guard BEFORE INSERT ON run_equipment_effect_history
WHEN NEW.effect_revision>1 AND NOT EXISTS(SELECT 1 FROM run_equipment_merges c
  WHERE c.run_id=NEW.run_id AND c.primary_instance_id=NEW.instance_id
    AND c.primary_effect_revision=NEW.effect_revision-1 AND c.command_id=NEW.source_ref)
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
CREATE TRIGGER equipment_effect_history_merge_guard BEFORE INSERT ON run_equipment_effect_history
WHEN EXISTS(SELECT 1 FROM run_equipment_merges c WHERE c.run_id=NEW.run_id
  AND c.primary_instance_id=NEW.instance_id AND c.primary_effect_revision=NEW.effect_revision-1 AND c.command_id=NEW.source_ref)
BEGIN
  SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM run_equipment_merges c
    JOIN run_equipment_instances e ON e.run_id=c.run_id AND e.instance_id=c.primary_instance_id
    JOIN run_equipment_inventory i ON i.run_id=c.run_id
    JOIN run_equipment_effect_heads h ON h.run_id=e.run_id AND h.instance_id=e.instance_id
    JOIN run_equipment_effect_history p ON p.run_id=h.run_id AND p.instance_id=h.instance_id AND p.effect_revision=h.revision
    WHERE c.run_id=NEW.run_id AND c.primary_instance_id=NEW.instance_id AND c.command_id=NEW.source_ref
      AND c.primary_effect_revision=h.revision AND h.revision=NEW.effect_revision-1
      AND c.instance_revision=e.revision AND e.revision=NEW.instance_revision
      AND c.inventory_revision=i.revision AND c.result_snapshot_json=NEW.snapshot_json
      AND c.payload_hash=NEW.payload_hash AND p.original_hash=NEW.original_hash
      AND e.star=json_extract(NEW.snapshot_json,'$.equipment.star')
      AND NOT EXISTS(SELECT 1 FROM run_equipment_instances b WHERE b.instance_id=c.secondary_instance_id))
    THEN RAISE(ABORT,'equipment_effect_revision_invalid') END;
END;
DROP TRIGGER equipment_effect_head_enchant_guard;
CREATE TRIGGER equipment_effect_head_change_guard BEFORE UPDATE ON run_equipment_effect_heads
BEGIN
  SELECT CASE WHEN NEW.run_id<>OLD.run_id OR NEW.instance_id<>OLD.instance_id OR NEW.revision<>OLD.revision+1
    OR NOT EXISTS(SELECT 1 FROM run_equipment_effect_history n
      JOIN run_equipment_instances e ON e.run_id=n.run_id AND e.instance_id=n.instance_id
      WHERE n.run_id=NEW.run_id AND n.instance_id=NEW.instance_id AND n.effect_revision=NEW.revision
        AND e.revision=n.instance_revision AND (
          EXISTS(SELECT 1 FROM run_equipment_enchantment_changes c WHERE c.run_id=n.run_id AND c.instance_id=n.instance_id
            AND c.effect_revision=n.effect_revision AND c.payload_hash=n.payload_hash)
          OR EXISTS(SELECT 1 FROM run_equipment_merges m WHERE m.run_id=n.run_id AND m.primary_instance_id=n.instance_id
            AND m.primary_effect_revision=n.effect_revision-1 AND m.payload_hash=n.payload_hash)))
    THEN RAISE(ABORT,'equipment_effect_revision_invalid') END;
END;

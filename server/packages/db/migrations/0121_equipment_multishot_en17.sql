-- Additive EN17 version dispatch. No existing records or hashes are rewritten.

DROP TRIGGER equipment_effect_original_insert_guard;

CREATE TRIGGER equipment_effect_original_insert_guard BEFORE INSERT ON run_equipment_effect_originals
BEGIN
  SELECT CASE WHEN NOT EXISTS (
    SELECT 1 FROM run_equipment_instances e JOIN runs r ON r.id=e.run_id
    WHERE e.instance_id=NEW.instance_id AND e.run_id=NEW.run_id AND r.status='ACTIVE'
      AND e.source_item_id=NEW.source_item_id AND e.revision=1
      AND json_extract(NEW.snapshot_json,'$.version') IN ('equipment.effect-original.v1','equipment.effect-original.v2.20261015')
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionId')=e.definition_id
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionVersion')=e.definition_version
      AND json_extract(NEW.snapshot_json,'$.equipment.star')=e.star
  ) THEN RAISE(ABORT,'equipment_effect_original_mismatch') END;
END;

DROP TRIGGER equipment_effect_history_initial_guard;

CREATE TRIGGER equipment_effect_history_initial_guard BEFORE INSERT ON run_equipment_effect_history
WHEN NEW.effect_revision=1
BEGIN
  SELECT CASE WHEN NEW.instance_revision<>1 OR NOT EXISTS (
    SELECT 1 FROM run_equipment_effect_originals o JOIN run_equipment_instances e ON e.instance_id=o.instance_id
    JOIN runs r ON r.id=o.run_id
    WHERE o.run_id=NEW.run_id AND o.instance_id=NEW.instance_id AND r.status='ACTIVE'
      AND e.run_id=o.run_id AND e.revision=1 AND o.source_item_id=NEW.source_ref AND o.payload_hash=NEW.original_hash
      AND ((json_extract(o.snapshot_json,'$.version')='equipment.effect-original.v1' AND json_extract(NEW.snapshot_json,'$.version')='equipment.effect-state.v1') OR (json_extract(o.snapshot_json,'$.version')='equipment.effect-original.v2.20261015' AND json_extract(NEW.snapshot_json,'$.version')='equipment.effect-state.v2.20261015'))
      AND json_remove(o.snapshot_json,'$.version')=json_remove(NEW.snapshot_json,'$.version')
  ) THEN RAISE(ABORT,'equipment_effect_initial_mismatch') END;
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
      AND json_remove(p.snapshot_json,'$.enchantment','$.version')=json_remove(NEW.snapshot_json,'$.enchantment','$.version')
      AND (json_extract(p.snapshot_json,'$.version')=json_extract(NEW.snapshot_json,'$.version') OR (json_extract(p.snapshot_json,'$.version')='equipment.effect-state.v1' AND json_extract(NEW.snapshot_json,'$.version')='equipment.effect-state.v2.20261015' AND json_extract(NEW.snapshot_json,'$.enchantment.effectId')='EN17'))
      AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar')=e.star
      AND json_extract(NEW.snapshot_json,'$.enchantment.effectId') IN
        ('EN01','EN02','EN03','EN04','EN05','EN06','EN07','EN08','EN09','EN10','EN11','EN12','EN13','EN14','EN15','EN16','EN17')
  ) THEN RAISE(ABORT,'equipment_effect_revision_invalid') END;
END;

DROP TRIGGER equipment_merge_material_guard;

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
      AND json_extract(NEW.result_snapshot_json,'$.version')=json_extract(p.snapshot_json,'$.version')
      AND json_extract(NEW.result_snapshot_json,'$.equipment.star')=a.star+1
      AND json_extract(NEW.result_snapshot_json,'$.equipment.definitionId')=a.definition_id
      AND json_extract(NEW.result_snapshot_json,'$.equipment.definitionVersion')=a.definition_version
      AND json_extract(NEW.result_snapshot_json,'$.enchantment') IS json_extract(p.snapshot_json,'$.enchantment'))
    THEN RAISE(ABORT,'equipment_merge_material_invalid') END;
END;

CREATE TRIGGER run_equipment_effect_originals_en17_envelope_guard BEFORE INSERT ON run_equipment_effect_originals
BEGIN
 SELECT CASE WHEN COALESCE((json_extract(NEW.snapshot_json,'$.version') IN ('equipment.effect-original.v1','equipment.effect-original.v2.20261015')
  AND (COALESCE(json_extract(NEW.snapshot_json,'$.enchantment.effectId'),'')<>'EN17' OR (
   json_extract(NEW.snapshot_json,'$.version')='equipment.effect-original.v2.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionVersion')='equipment.multishot-catalog.v1.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.version')='multishot-enchant-v1.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar') BETWEEN 1 AND json_extract(NEW.snapshot_json,'$.equipment.star')
   AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar')=json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.appliedStar')
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.coefficientBps')=CASE json_extract(NEW.snapshot_json,'$.enchantment.appliedStar') WHEN 1 THEN 1500 WHEN 2 THEN 2500 WHEN 3 THEN 4000 WHEN 4 THEN 6000 WHEN 5 THEN 8500 END
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.maxSecondaryTargets')=2
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.radiusL.numerator')=1
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.radiusL.denominator')=2
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.attackEvaluation')='AT_FIRE'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.targetDefenseEvaluation')='AT_IMPACT'
  ))),0)=0 THEN RAISE(ABORT,'equipment_effect_envelope_invalid') END;
END;

CREATE TRIGGER run_equipment_effect_history_en17_envelope_guard BEFORE INSERT ON run_equipment_effect_history
BEGIN
 SELECT CASE WHEN COALESCE((json_extract(NEW.snapshot_json,'$.version') IN ('equipment.effect-state.v1','equipment.effect-state.v2.20261015')
  AND (COALESCE(json_extract(NEW.snapshot_json,'$.enchantment.effectId'),'')<>'EN17' OR (
   json_extract(NEW.snapshot_json,'$.version')='equipment.effect-state.v2.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionVersion')='equipment.multishot-catalog.v1.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.version')='multishot-enchant-v1.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar') BETWEEN 1 AND json_extract(NEW.snapshot_json,'$.equipment.star')
   AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar')=json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.appliedStar')
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.coefficientBps')=CASE json_extract(NEW.snapshot_json,'$.enchantment.appliedStar') WHEN 1 THEN 1500 WHEN 2 THEN 2500 WHEN 3 THEN 4000 WHEN 4 THEN 6000 WHEN 5 THEN 8500 END
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.maxSecondaryTargets')=2
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.radiusL.numerator')=1
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.radiusL.denominator')=2
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.attackEvaluation')='AT_FIRE'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.targetDefenseEvaluation')='AT_IMPACT'
  ))),0)=0 THEN RAISE(ABORT,'equipment_effect_envelope_invalid') END;
END;

CREATE TRIGGER run_equipment_merges_en17_envelope_guard BEFORE INSERT ON run_equipment_merges
BEGIN
 SELECT CASE WHEN COALESCE((json_extract(NEW.result_snapshot_json,'$.version') IN ('equipment.effect-state.v1','equipment.effect-state.v2.20261015')
  AND (COALESCE(json_extract(NEW.result_snapshot_json,'$.enchantment.effectId'),'')<>'EN17' OR (
   json_extract(NEW.result_snapshot_json,'$.version')='equipment.effect-state.v2.20261015'
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.definitionVersion')='equipment.multishot-catalog.v1.20261015'
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.definitionSnapshot.version')='multishot-enchant-v1.20261015'
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.appliedStar') BETWEEN 1 AND json_extract(NEW.result_snapshot_json,'$.equipment.star')
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.appliedStar')=json_extract(NEW.result_snapshot_json,'$.enchantment.definitionSnapshot.appliedStar')
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.definitionSnapshot.coefficientBps')=CASE json_extract(NEW.result_snapshot_json,'$.enchantment.appliedStar') WHEN 1 THEN 1500 WHEN 2 THEN 2500 WHEN 3 THEN 4000 WHEN 4 THEN 6000 WHEN 5 THEN 8500 END
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.definitionSnapshot.maxSecondaryTargets')=2
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.definitionSnapshot.radiusL.numerator')=1
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.definitionSnapshot.radiusL.denominator')=2
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.definitionSnapshot.attackEvaluation')='AT_FIRE'
   AND json_extract(NEW.result_snapshot_json,'$.enchantment.definitionSnapshot.targetDefenseEvaluation')='AT_IMPACT'
  ))),0)=0 THEN RAISE(ABORT,'equipment_effect_envelope_invalid') END;
END;

-- Isolated LOCAL trial START only. Running ordinary START records remain unchanged.
CREATE TABLE equipment_multishot_trial_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TABLE run_equipment_multishot_trial_starts(
 run_id TEXT NOT NULL REFERENCES runs(id),trial_id TEXT NOT NULL,account_id TEXT NOT NULL REFERENCES accounts(id),
 request_hash TEXT NOT NULL CHECK(length(request_hash)=64),source_hash TEXT NOT NULL CHECK(length(source_hash)=64),
 original_json TEXT NOT NULL CHECK(json_valid(original_json)),PRIMARY KEY(run_id,trial_id),UNIQUE(run_id,trial_id,source_hash)
);
CREATE TABLE run_equipment_multishot_trial_sources(
 run_id TEXT NOT NULL,trial_id TEXT NOT NULL,source_hash TEXT NOT NULL CHECK(length(source_hash)=64),
 source_json TEXT NOT NULL CHECK(json_valid(source_json)),PRIMARY KEY(run_id,trial_id),
 FOREIGN KEY(run_id,trial_id,source_hash) REFERENCES run_equipment_multishot_trial_starts(run_id,trial_id,source_hash)
);
CREATE TRIGGER equipment_multishot_trial_start_no_update BEFORE UPDATE ON run_equipment_multishot_trial_starts BEGIN SELECT RAISE(ABORT,'equipment_multishot_trial_immutable'); END;
CREATE TRIGGER equipment_multishot_trial_start_no_delete BEFORE DELETE ON run_equipment_multishot_trial_starts BEGIN SELECT RAISE(ABORT,'equipment_multishot_trial_immutable'); END;
CREATE TRIGGER equipment_multishot_trial_source_no_update BEFORE UPDATE ON run_equipment_multishot_trial_sources BEGIN SELECT RAISE(ABORT,'equipment_multishot_trial_immutable'); END;
CREATE TRIGGER equipment_multishot_trial_source_no_delete BEFORE DELETE ON run_equipment_multishot_trial_sources BEGIN SELECT RAISE(ABORT,'equipment_multishot_trial_immutable'); END;

DROP TRIGGER equipment_merchant_effect_insert_guard;
CREATE TRIGGER equipment_merchant_effect_insert_guard BEFORE INSERT ON run_equipment_merchant_effects
BEGIN
  SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM run_equipment_merchant_stock st
    JOIN runs r ON r.id=st.run_id WHERE st.run_id=NEW.run_id AND st.stock_id=NEW.stock_id AND st.offer_id=NEW.offer_id
      AND r.status='ACTIVE' AND json_extract(NEW.snapshot_json,'$.version') IN ('equipment.effect-original.v1','equipment.effect-original.v2.20261015')
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionId')=json_extract(st.stock_json,'$.definitionId')
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionVersion')=json_extract(st.stock_json,'$.definitionVersion')
      AND json_extract(NEW.snapshot_json,'$.equipment.star')=json_extract(st.stock_json,'$.star')
      AND json_type(NEW.snapshot_json,'$.enchantment')='null')
    THEN RAISE(ABORT,'equipment_merchant_effect_mismatch') END;
END;

DROP TRIGGER equipment_reward_effect_insert_guard;
CREATE TRIGGER equipment_reward_effect_insert_guard BEFORE INSERT ON run_equipment_reward_effects
BEGIN
  SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM run_equipment_reward_items item JOIN runs r ON r.id=item.run_id
    WHERE item.run_id=NEW.run_id AND item.item_id=NEW.item_id AND item.bundle_id=NEW.bundle_id
      AND item.status='PENDING' AND r.status='ACTIVE'
      AND json_extract(NEW.snapshot_json,'$.version') IN ('equipment.effect-original.v1','equipment.effect-original.v2.20261015')
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionId')=json_extract(item.item_json,'$.definitionId')
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionVersion')=json_extract(item.item_json,'$.definitionVersion')
      AND json_extract(NEW.snapshot_json,'$.equipment.star')=json_extract(item.item_json,'$.star'))
    THEN RAISE(ABORT,'equipment_reward_effect_mismatch') END;
END;

CREATE TRIGGER run_equipment_merchant_effects_en17_envelope_guard BEFORE INSERT ON run_equipment_merchant_effects
BEGIN
 SELECT CASE WHEN COALESCE((json_extract(NEW.snapshot_json,'$.version') IN ('equipment.effect-original.v1','equipment.effect-original.v2.20261015')
  AND (COALESCE(json_extract(NEW.snapshot_json,'$.enchantment.effectId'),'')<>'EN17' OR (
   json_extract(NEW.snapshot_json,'$.version')='equipment.effect-original.v2.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionVersion')='equipment.multishot-catalog.v1.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.version')='multishot-enchant-v1.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar') BETWEEN 1 AND json_extract(NEW.snapshot_json,'$.equipment.star')
   AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar')=json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.appliedStar')
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.coefficientBps')=CASE json_extract(NEW.snapshot_json,'$.enchantment.appliedStar') WHEN 1 THEN 1500 WHEN 2 THEN 2500 WHEN 3 THEN 4000 WHEN 4 THEN 6000 WHEN 5 THEN 8500 END
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.maxSecondaryTargets')=2
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.radiusL.numerator')=1
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.radiusL.denominator')=2
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.attackEvaluation')='AT_FIRE'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.targetDefenseEvaluation')='AT_IMPACT'
  ))),0)=0 THEN RAISE(ABORT,'equipment_effect_envelope_invalid') END;
END;

CREATE TRIGGER run_equipment_reward_effects_en17_envelope_guard BEFORE INSERT ON run_equipment_reward_effects
BEGIN
 SELECT CASE WHEN COALESCE((json_extract(NEW.snapshot_json,'$.version') IN ('equipment.effect-original.v1','equipment.effect-original.v2.20261015')
  AND (COALESCE(json_extract(NEW.snapshot_json,'$.enchantment.effectId'),'')<>'EN17' OR (
   json_extract(NEW.snapshot_json,'$.version')='equipment.effect-original.v2.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionVersion')='equipment.multishot-catalog.v1.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.version')='multishot-enchant-v1.20261015'
   AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar') BETWEEN 1 AND json_extract(NEW.snapshot_json,'$.equipment.star')
   AND json_extract(NEW.snapshot_json,'$.enchantment.appliedStar')=json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.appliedStar')
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.coefficientBps')=CASE json_extract(NEW.snapshot_json,'$.enchantment.appliedStar') WHEN 1 THEN 1500 WHEN 2 THEN 2500 WHEN 3 THEN 4000 WHEN 4 THEN 6000 WHEN 5 THEN 8500 END
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.maxSecondaryTargets')=2
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.radiusL.numerator')=1
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.radiusL.denominator')=2
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.attackEvaluation')='AT_FIRE'
   AND json_extract(NEW.snapshot_json,'$.enchantment.definitionSnapshot.targetDefenseEvaluation')='AT_IMPACT'
  ))),0)=0 THEN RAISE(ABORT,'equipment_effect_envelope_invalid') END;
END;

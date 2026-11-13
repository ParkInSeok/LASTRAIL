-- Original resolved results, fixed with stock BEFORE purchase. Not live item state or a legacy backfill.
CREATE TABLE run_equipment_merchant_effects (
  run_id TEXT NOT NULL, stock_id TEXT PRIMARY KEY REFERENCES run_equipment_merchant_stock(stock_id),
  offer_id TEXT NOT NULL, snapshot_json TEXT NOT NULL CHECK(json_valid(snapshot_json)),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  FOREIGN KEY(run_id,offer_id) REFERENCES run_equipment_merchant_offers(run_id,offer_id)
);
CREATE TRIGGER equipment_merchant_effect_insert_guard BEFORE INSERT ON run_equipment_merchant_effects
BEGIN
  SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM run_equipment_merchant_stock st
    JOIN runs r ON r.id=st.run_id WHERE st.run_id=NEW.run_id AND st.stock_id=NEW.stock_id AND st.offer_id=NEW.offer_id
      AND r.status='ACTIVE' AND json_extract(NEW.snapshot_json,'$.version')='equipment.effect-original.v1'
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionId')=json_extract(st.stock_json,'$.definitionId')
      AND json_extract(NEW.snapshot_json,'$.equipment.definitionVersion')=json_extract(st.stock_json,'$.definitionVersion')
      AND json_extract(NEW.snapshot_json,'$.equipment.star')=json_extract(st.stock_json,'$.star')
      AND json_type(NEW.snapshot_json,'$.enchantment')='null')
    THEN RAISE(ABORT,'equipment_merchant_effect_mismatch') END;
END;
CREATE TRIGGER equipment_merchant_effect_no_update BEFORE UPDATE ON run_equipment_merchant_effects
BEGIN SELECT RAISE(ABORT,'equipment_merchant_effect_immutable'); END;
CREATE TRIGGER equipment_merchant_effect_no_delete BEFORE DELETE ON run_equipment_merchant_effects
BEGIN SELECT RAISE(ABORT,'equipment_merchant_effect_immutable'); END;

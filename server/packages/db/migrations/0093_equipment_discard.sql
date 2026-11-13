-- Preserve original receipts while adding the explicit single-item v19 command.
CREATE TABLE run_equipment_receipts_next (
  run_id TEXT NOT NULL REFERENCES run_equipment_inventory(run_id) ON DELETE CASCADE,
  command_id TEXT NOT NULL CHECK(length(command_id) BETWEEN 1 AND 128),
  command_kind TEXT NOT NULL CHECK(command_kind IN ('SET_LOCK','CLAIM_ITEMS','DECLINE_REMAINING','PURCHASE_MERCHANT','EQUIP_OR_MOVE','ENCHANT_EQUIPMENT','MERGE_EQUIPMENT','DISCARD_EQUIPMENT')),
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
-- Effect originals/history, reward and merchant records are retained on live-instance deletion.

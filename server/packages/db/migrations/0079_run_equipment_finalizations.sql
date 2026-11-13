-- Equipment cleanup receipt for a trusted terminal result batch; no historical backfill.
CREATE TABLE run_equipment_finalizations (
  run_id TEXT PRIMARY KEY REFERENCES run_equipment_inventory(run_id) ON DELETE CASCADE,
  account_id TEXT NOT NULL REFERENCES accounts(id),
  outcome TEXT NOT NULL CHECK(outcome IN ('VICTORY','FAILURE','ABANDONED')),
  final_payload_hash TEXT NOT NULL CHECK(length(final_payload_hash)=64),
  inventory_revision INTEGER NOT NULL CHECK(typeof(inventory_revision)='integer' AND inventory_revision>=2),
  receipt_json TEXT NOT NULL CHECK(json_valid(receipt_json))
);
CREATE TRIGGER run_equipment_finalization_immutable_update BEFORE UPDATE ON run_equipment_finalizations
BEGIN SELECT RAISE(ABORT,'equipment_finalization_immutable'); END;
CREATE TRIGGER run_equipment_finalization_immutable_delete BEFORE DELETE ON run_equipment_finalizations
BEGIN SELECT RAISE(ABORT,'equipment_finalization_immutable'); END;
CREATE TABLE equipment_finalization_write_guard(valid INTEGER NOT NULL);
CREATE TRIGGER equipment_finalization_guard_invalid BEFORE INSERT ON equipment_finalization_write_guard
WHEN NEW.valid IS NOT 1
BEGIN SELECT RAISE(ABORT,'equipment_finalization_state_conflict'); END;

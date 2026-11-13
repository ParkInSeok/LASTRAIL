-- Derived original-runtime eligibility only. No synthetic safe state or clock backfill.
CREATE TABLE equipment_safe_states(run_id TEXT PRIMARY KEY REFERENCES runs(id),state_json TEXT NOT NULL CHECK(json_valid(state_json)));
CREATE TABLE equipment_safe_receipts(run_id TEXT NOT NULL REFERENCES runs(id),event_id TEXT NOT NULL,request_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),PRIMARY KEY(run_id,event_id));
CREATE TABLE equipment_safe_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER equipment_safe_receipts_no_update BEFORE UPDATE ON equipment_safe_receipts BEGIN SELECT RAISE(ABORT,'equipment_safe_receipt_immutable'); END;
CREATE TRIGGER equipment_safe_receipts_no_delete BEFORE DELETE ON equipment_safe_receipts BEGIN SELECT RAISE(ABORT,'equipment_safe_receipt_immutable'); END;
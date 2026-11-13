-- Original server transition verification only; never safe-maintenance authorization.
CREATE TABLE equipment_safe_transition_receipts(run_id TEXT NOT NULL REFERENCES runs(id),command_id TEXT NOT NULL,request_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),PRIMARY KEY(run_id,command_id));
CREATE TABLE equipment_safe_transition_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER equipment_safe_transition_no_update BEFORE UPDATE ON equipment_safe_transition_receipts BEGIN SELECT RAISE(ABORT,'equipment_safe_transition_immutable'); END;
CREATE TRIGGER equipment_safe_transition_no_delete BEFORE DELETE ON equipment_safe_transition_receipts BEGIN SELECT RAISE(ABORT,'equipment_safe_transition_immutable'); END;
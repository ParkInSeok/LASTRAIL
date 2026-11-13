ALTER TABLE combat_host_report_states ADD COLUMN recovery_json TEXT CHECK(recovery_json IS NULL OR json_valid(recovery_json));
CREATE TABLE combat_host_recovery_receipts (
 run_id TEXT NOT NULL REFERENCES combat_host_report_states(run_id),
 request_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,request_id)
);
CREATE TRIGGER combat_host_recovery_no_update BEFORE UPDATE ON combat_host_recovery_receipts BEGIN SELECT RAISE(ABORT,'immutable_recovery_receipt'); END;
CREATE TRIGGER combat_host_recovery_no_delete BEFORE DELETE ON combat_host_recovery_receipts BEGIN SELECT RAISE(ABORT,'immutable_recovery_receipt'); END;

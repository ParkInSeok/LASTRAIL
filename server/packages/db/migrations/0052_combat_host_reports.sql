CREATE TABLE combat_host_report_states (
 run_id TEXT PRIMARY KEY REFERENCES runs(id),
 revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
 initial_hash TEXT NOT NULL,
 start_snapshot_json TEXT NOT NULL CHECK(json_valid(start_snapshot_json)),
 context_json TEXT NOT NULL CHECK(json_valid(context_json))
);
CREATE TABLE combat_host_report_receipts (
 run_id TEXT NOT NULL REFERENCES combat_host_report_states(run_id),
 request_id TEXT NOT NULL,
 sender_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 report_json TEXT NOT NULL CHECK(json_valid(report_json)),
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,request_id)
);
CREATE TRIGGER combat_host_receipt_no_update BEFORE UPDATE ON combat_host_report_receipts BEGIN SELECT RAISE(ABORT,'immutable_host_receipt'); END;
CREATE TRIGGER combat_host_receipt_no_delete BEFORE DELETE ON combat_host_report_receipts BEGIN SELECT RAISE(ABORT,'immutable_host_receipt'); END;
CREATE TABLE combat_host_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

CREATE TABLE combat_checkpoints (
 run_id TEXT NOT NULL REFERENCES combat_host_report_states(run_id),
 checkpoint_id TEXT NOT NULL,
 checkpoint_text TEXT NOT NULL,
 sha256 TEXT NOT NULL,
 context_json TEXT NOT NULL CHECK(json_valid(context_json)),
 start_snapshot_json TEXT NOT NULL CHECK(json_valid(start_snapshot_json)),
 PRIMARY KEY(run_id,checkpoint_id)
);
CREATE TABLE combat_checkpoint_receipts (
 run_id TEXT NOT NULL REFERENCES combat_host_report_states(run_id),
 request_id TEXT NOT NULL,
 sender_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,request_id)
);
CREATE TRIGGER combat_checkpoint_no_update BEFORE UPDATE ON combat_checkpoints BEGIN SELECT RAISE(ABORT,'immutable_checkpoint'); END;
CREATE TRIGGER combat_checkpoint_no_delete BEFORE DELETE ON combat_checkpoints BEGIN SELECT RAISE(ABORT,'immutable_checkpoint'); END;
CREATE TRIGGER combat_checkpoint_receipt_no_update BEFORE UPDATE ON combat_checkpoint_receipts BEGIN SELECT RAISE(ABORT,'immutable_checkpoint_receipt'); END;
CREATE TRIGGER combat_checkpoint_receipt_no_delete BEFORE DELETE ON combat_checkpoint_receipts BEGIN SELECT RAISE(ABORT,'immutable_checkpoint_receipt'); END;

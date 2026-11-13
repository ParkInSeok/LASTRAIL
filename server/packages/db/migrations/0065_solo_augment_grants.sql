CREATE TABLE solo_augment_grants (
 run_id TEXT NOT NULL REFERENCES runs(id),event_id TEXT NOT NULL,stage_id TEXT NOT NULL,
 account_id TEXT NOT NULL REFERENCES accounts(id),environment TEXT NOT NULL,issue_hash TEXT NOT NULL,
 start_snapshot_json TEXT NOT NULL CHECK(json_valid(start_snapshot_json)),
 initial_json TEXT NOT NULL CHECK(json_valid(initial_json)),checkpoint_json TEXT NOT NULL CHECK(json_valid(checkpoint_json)),
 write_revision INTEGER NOT NULL CHECK(typeof(write_revision)='integer' AND write_revision BETWEEN 1 AND 9007199254740991),
 PRIMARY KEY(run_id,event_id),UNIQUE(run_id,stage_id)
);
CREATE TABLE solo_augment_receipts (
 run_id TEXT NOT NULL,event_id TEXT NOT NULL,request_id TEXT NOT NULL,request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,event_id,request_id),
 FOREIGN KEY(run_id,event_id) REFERENCES solo_augment_grants(run_id,event_id)
);
CREATE TRIGGER solo_augment_receipt_no_update BEFORE UPDATE ON solo_augment_receipts BEGIN SELECT RAISE(ABORT,'immutable_augment_receipt'); END;
CREATE TRIGGER solo_augment_receipt_no_delete BEFORE DELETE ON solo_augment_receipts BEGIN SELECT RAISE(ABORT,'immutable_augment_receipt'); END;
CREATE TABLE solo_augment_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

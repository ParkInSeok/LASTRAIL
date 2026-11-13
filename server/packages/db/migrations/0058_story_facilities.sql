CREATE TABLE story_facility_states (
 run_id TEXT NOT NULL REFERENCES runs(id),
 node_id TEXT NOT NULL,
 revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
 initial_hash TEXT NOT NULL,
 start_snapshot_json TEXT NOT NULL CHECK(json_valid(start_snapshot_json)),
 state_json TEXT NOT NULL CHECK(json_valid(state_json)),
 PRIMARY KEY(run_id,node_id)
);
CREATE TABLE story_facility_receipts (
 run_id TEXT NOT NULL,
 node_id TEXT NOT NULL,
 event_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,node_id,event_id),
 FOREIGN KEY(run_id,node_id) REFERENCES story_facility_states(run_id,node_id)
);
CREATE TRIGGER story_facility_receipt_no_update BEFORE UPDATE ON story_facility_receipts BEGIN SELECT RAISE(ABORT,'immutable_facility_receipt'); END;
CREATE TRIGGER story_facility_receipt_no_delete BEFORE DELETE ON story_facility_receipts BEGIN SELECT RAISE(ABORT,'immutable_facility_receipt'); END;
CREATE TABLE story_facility_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

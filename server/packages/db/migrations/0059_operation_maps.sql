-- No legacy backfill. Only an explicitly pinned ordinary twelve-node START may initialize.
CREATE TABLE operation_map_states (
 run_id TEXT PRIMARY KEY REFERENCES runs(id),
 revision INTEGER NOT NULL CHECK(revision>0 AND revision<=9007199254740991),
 plan_json TEXT NOT NULL CHECK(json_valid(plan_json)),
 start_snapshot_json TEXT NOT NULL CHECK(json_valid(start_snapshot_json)),
 state_json TEXT NOT NULL CHECK(json_valid(state_json))
);
CREATE TABLE operation_map_receipts (
 run_id TEXT NOT NULL REFERENCES operation_map_states(run_id),
 event_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,event_id)
);
CREATE TRIGGER operation_map_plan_immutable BEFORE UPDATE OF plan_json,start_snapshot_json ON operation_map_states BEGIN SELECT RAISE(ABORT,'operation_map_plan_immutable'); END;
CREATE TRIGGER operation_map_receipt_no_update BEFORE UPDATE ON operation_map_receipts BEGIN SELECT RAISE(ABORT,'operation_map_receipt_immutable'); END;
CREATE TRIGGER operation_map_receipt_no_delete BEFORE DELETE ON operation_map_receipts BEGIN SELECT RAISE(ABORT,'operation_map_receipt_immutable'); END;
CREATE TABLE operation_map_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

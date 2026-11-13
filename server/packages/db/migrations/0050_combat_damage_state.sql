-- Runtime damage checkpoint is separate from the immutable departure snapshot.
CREATE TABLE combat_damage_states (
 run_id TEXT PRIMARY KEY REFERENCES runs(id),
 revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
 initial_hash TEXT NOT NULL,
 start_snapshot_json TEXT NOT NULL CHECK(json_valid(start_snapshot_json)),
 state_json TEXT NOT NULL CHECK(json_valid(state_json))
);
CREATE TABLE combat_damage_receipts (
 run_id TEXT NOT NULL REFERENCES combat_damage_states(run_id), event_id TEXT NOT NULL,
 request_hash TEXT NOT NULL, response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,event_id)
);
CREATE TRIGGER combat_damage_receipt_immutable_update BEFORE UPDATE ON combat_damage_receipts
 BEGIN SELECT RAISE(ABORT,'combat_damage_receipt_immutable'); END;
CREATE TRIGGER combat_damage_receipt_immutable_delete BEFORE DELETE ON combat_damage_receipts
 BEGIN SELECT RAISE(ABORT,'combat_damage_receipt_immutable'); END;
CREATE TABLE combat_damage_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

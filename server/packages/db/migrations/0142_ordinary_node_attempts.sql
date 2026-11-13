-- No backfill: a node origin must be captured by its verified entry owner.
CREATE TABLE ordinary_node_attempts (
 run_id TEXT NOT NULL REFERENCES runs(id), node_id TEXT NOT NULL,
 start_snapshot_json TEXT NOT NULL, snapshot_json TEXT NOT NULL, snapshot_hash TEXT NOT NULL,
 generation INTEGER NOT NULL CHECK(generation BETWEEN 1 AND 9007199254740991),
 PRIMARY KEY(run_id,node_id)
);
CREATE TRIGGER ordinary_node_origin_immutable BEFORE UPDATE ON ordinary_node_attempts
WHEN NEW.run_id<>OLD.run_id OR NEW.node_id<>OLD.node_id OR NEW.start_snapshot_json<>OLD.start_snapshot_json
 OR NEW.snapshot_json<>OLD.snapshot_json OR NEW.snapshot_hash<>OLD.snapshot_hash OR NEW.generation<>OLD.generation+1
BEGIN SELECT RAISE(ABORT,'ordinary_node_origin_immutable'); END;
CREATE TRIGGER ordinary_node_origin_no_delete BEFORE DELETE ON ordinary_node_attempts
BEGIN SELECT RAISE(ABORT,'ordinary_node_origin_immutable'); END;
CREATE TABLE ordinary_node_restart_receipts (
 run_id TEXT NOT NULL REFERENCES runs(id), request_id TEXT NOT NULL, request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL, PRIMARY KEY(run_id,request_id)
);
CREATE TRIGGER ordinary_node_restart_no_update BEFORE UPDATE ON ordinary_node_restart_receipts
BEGIN SELECT RAISE(ABORT,'ordinary_node_restart_receipt_immutable'); END;
CREATE TRIGGER ordinary_node_restart_no_delete BEFORE DELETE ON ordinary_node_restart_receipts
BEGIN SELECT RAISE(ABORT,'ordinary_node_restart_receipt_immutable'); END;

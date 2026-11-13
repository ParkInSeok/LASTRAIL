-- Local supervisor only; never a production migration or imported client database.
CREATE TABLE IF NOT EXISTS ordinary_local_node_scope (
 account_id TEXT NOT NULL, run_id TEXT NOT NULL REFERENCES runs(id), node_id TEXT NOT NULL,
 revision INTEGER NOT NULL CHECK(revision=1), entry_effects_json TEXT NOT NULL, hold_id TEXT NOT NULL, engine_pin_json TEXT NOT NULL,
 PRIMARY KEY(run_id,node_id)
);
CREATE TRIGGER IF NOT EXISTS ordinary_local_scope_no_update BEFORE UPDATE ON ordinary_local_node_scope
BEGIN SELECT RAISE(ABORT,'ordinary_local_scope_immutable'); END;
CREATE TRIGGER IF NOT EXISTS ordinary_local_scope_no_delete BEFORE DELETE ON ordinary_local_node_scope
BEGIN SELECT RAISE(ABORT,'ordinary_local_scope_immutable'); END;
CREATE TABLE IF NOT EXISTS ordinary_local_execution (
 account_id TEXT NOT NULL, run_id TEXT NOT NULL REFERENCES runs(id), node_id TEXT NOT NULL,
 generation INTEGER NOT NULL, snapshot_hash TEXT NOT NULL, sequence INTEGER NOT NULL,
 started_at INTEGER NOT NULL, elapsed_ticks INTEGER NOT NULL, inputs_json TEXT NOT NULL, result_json TEXT NOT NULL,
 PRIMARY KEY(run_id,node_id,generation)
);
CREATE TABLE IF NOT EXISTS ordinary_local_execution_receipts (
 account_id TEXT NOT NULL, run_id TEXT NOT NULL REFERENCES runs(id), request_id TEXT NOT NULL,
 request_hash TEXT NOT NULL, response_json TEXT NOT NULL,
 PRIMARY KEY(run_id,request_id)
);
CREATE TRIGGER IF NOT EXISTS ordinary_local_receipt_no_update BEFORE UPDATE ON ordinary_local_execution_receipts
BEGIN SELECT RAISE(ABORT,'ordinary_local_receipt_immutable'); END;
CREATE TRIGGER IF NOT EXISTS ordinary_local_receipt_no_delete BEFORE DELETE ON ordinary_local_execution_receipts
BEGIN SELECT RAISE(ABORT,'ordinary_local_receipt_immutable'); END;

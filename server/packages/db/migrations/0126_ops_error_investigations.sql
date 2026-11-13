-- Investigation records have their own lifetime; retention of diagnostic events
-- must not cascade-delete the investigation, command receipts or operator audit.
CREATE TABLE ops_error_investigations (
 error_log_id TEXT PRIMARY KEY,
 version INTEGER NOT NULL CHECK(version > 0),
 state_json TEXT NOT NULL CHECK(json_valid(state_json)),
 updated_at INTEGER NOT NULL
);
CREATE TABLE ops_error_investigation_receipts (
 principal_id TEXT NOT NULL,
 request_key TEXT NOT NULL,
 payload_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 created_at INTEGER NOT NULL,
 PRIMARY KEY(principal_id,request_key)
);
CREATE TRIGGER ops_investigation_receipt_immutable BEFORE UPDATE ON ops_error_investigation_receipts
BEGIN SELECT RAISE(ABORT,'immutable investigation receipt'); END;
CREATE TABLE ops_error_investigation_guard(valid INTEGER NOT NULL CHECK(valid=1));

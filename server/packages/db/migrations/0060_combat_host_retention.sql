-- No backfill: a disconnect is not an actual suspension.
ALTER TABLE combat_host_report_states ADD COLUMN retention_json TEXT CHECK(retention_json IS NULL OR json_valid(retention_json));
CREATE TABLE combat_host_retention_receipts (
 run_id TEXT NOT NULL REFERENCES combat_host_report_states(run_id),
 request_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,request_id)
);
CREATE TRIGGER combat_host_retention_no_update BEFORE UPDATE ON combat_host_retention_receipts BEGIN SELECT RAISE(ABORT,'immutable_retention_receipt'); END;
CREATE TRIGGER combat_host_retention_no_delete BEFORE DELETE ON combat_host_retention_receipts BEGIN SELECT RAISE(ABORT,'immutable_retention_receipt'); END;

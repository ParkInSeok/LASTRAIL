-- Derived core stats only. HP/equipment/effect authority remains with original owners.
CREATE TABLE ordinary_run_stat_states (
  run_id TEXT PRIMARY KEY REFERENCES runs(id),
  account_id TEXT NOT NULL REFERENCES accounts(id),
  revision INTEGER NOT NULL CHECK(revision > 0),
  origin_hash TEXT NOT NULL,
  source_json TEXT NOT NULL,
  result_json TEXT NOT NULL
);
CREATE TABLE ordinary_run_stat_receipts (
  run_id TEXT NOT NULL REFERENCES runs(id),
  request_id TEXT NOT NULL,
  account_id TEXT NOT NULL REFERENCES accounts(id),
  request_hash TEXT NOT NULL,
  response_json TEXT NOT NULL,
  response_hash TEXT NOT NULL,
  PRIMARY KEY(run_id, request_id)
);
CREATE TRIGGER ordinary_stat_receipt_no_update BEFORE UPDATE ON ordinary_run_stat_receipts
BEGIN SELECT RAISE(ABORT, 'ordinary_stat_receipt_immutable'); END;
CREATE TRIGGER ordinary_stat_receipt_no_delete BEFORE DELETE ON ordinary_run_stat_receipts
BEGIN SELECT RAISE(ABORT, 'ordinary_stat_receipt_immutable'); END;

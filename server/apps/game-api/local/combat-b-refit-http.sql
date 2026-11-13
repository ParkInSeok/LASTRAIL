-- Isolated LOCAL refit selection/receipts. Not an operating migration or growth issuance.
CREATE TABLE IF NOT EXISTS combat_b_refit_selections(
 account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
 transport_version TEXT NOT NULL,definition_hash TEXT NOT NULL,source_hash TEXT NOT NULL,
 refit_json TEXT NOT NULL CHECK(json_valid(refit_json)),revision INTEGER NOT NULL CHECK(revision>0),updated_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS combat_b_refit_receipts(
 account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,idempotency_key TEXT NOT NULL,
 request_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),created_at INTEGER NOT NULL,
 PRIMARY KEY(account_id,idempotency_key)
);
CREATE TRIGGER IF NOT EXISTS combat_b_refit_receipt_immutable BEFORE UPDATE ON combat_b_refit_receipts BEGIN SELECT RAISE(ABORT,'combat_b_refit_receipt_immutable'); END;
CREATE TABLE IF NOT EXISTS combat_b_refit_guard(valid INTEGER NOT NULL CHECK(valid=1));
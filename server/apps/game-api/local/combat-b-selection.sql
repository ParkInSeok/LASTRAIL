-- Isolated LOCAL selection trial only. Not an operational migration.
CREATE TABLE combat_b_fixture_catalog(
 account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
 revision INTEGER NOT NULL CHECK(revision>0),
 catalog_json TEXT NOT NULL CHECK(json_valid(catalog_json))
);
CREATE TABLE IF NOT EXISTS combat_b_selections(
 account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
 transport_version TEXT NOT NULL,
 selection_json TEXT NOT NULL CHECK(json_valid(selection_json)),
 catalog_version TEXT NOT NULL, catalog_revision INTEGER NOT NULL CHECK(catalog_revision>0),
 revision INTEGER NOT NULL CHECK(revision>0), updated_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS combat_b_selection_receipts(
 account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
 idempotency_key TEXT NOT NULL, request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)), created_at INTEGER NOT NULL,
 PRIMARY KEY(account_id,idempotency_key)
);
CREATE TABLE IF NOT EXISTS combat_b_selection_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER IF NOT EXISTS combat_b_receipt_immutable_update BEFORE UPDATE ON combat_b_selection_receipts BEGIN SELECT RAISE(ABORT,'combat_b_receipt_immutable'); END;
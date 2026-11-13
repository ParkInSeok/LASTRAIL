ALTER TABLE operation_previews ADD COLUMN admission_version TEXT;
CREATE TABLE departure_preview_receipts (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  idempotency_key TEXT NOT NULL,
  request_hash TEXT NOT NULL,
  preview_id TEXT NOT NULL REFERENCES operation_previews(id) ON DELETE RESTRICT,
  response_json TEXT NOT NULL CHECK(json_valid(response_json)),
  PRIMARY KEY(account_id,idempotency_key)
);
CREATE TRIGGER departure_preview_receipts_no_update BEFORE UPDATE ON departure_preview_receipts
BEGIN SELECT RAISE(ABORT,'departure receipt immutable'); END;
CREATE TRIGGER departure_preview_receipts_no_replace BEFORE INSERT ON departure_preview_receipts
WHEN EXISTS(SELECT 1 FROM departure_preview_receipts WHERE account_id=NEW.account_id AND idempotency_key=NEW.idempotency_key)
BEGIN SELECT RAISE(ABORT,'departure receipt immutable'); END;
CREATE TABLE departure_write_guard (valid INTEGER NOT NULL CHECK(valid=1));

CREATE TABLE client_error_receipts (
 account_id TEXT NOT NULL, operation TEXT NOT NULL DEFAULT 'client_error_ingest' CHECK(operation='client_error_ingest'),
 idempotency_key TEXT NOT NULL, ingest_id TEXT NOT NULL,
 version TEXT NOT NULL CHECK(version='client-errors.v2.20261019'),
 accepted_event_count INTEGER NOT NULL CHECK(accepted_event_count BETWEEN 1 AND 25),
 received_at INTEGER NOT NULL, environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 PRIMARY KEY(account_id,idempotency_key),
 FOREIGN KEY(account_id,operation,idempotency_key) REFERENCES idempotency_records(account_id,operation,idempotency_key) ON DELETE CASCADE
);
CREATE TRIGGER client_error_receipt_no_update BEFORE UPDATE ON client_error_receipts
BEGIN SELECT RAISE(ABORT,'client_error_receipt_immutable'); END;
CREATE TABLE client_error_ingest_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

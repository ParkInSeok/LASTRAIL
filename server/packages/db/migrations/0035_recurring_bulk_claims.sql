CREATE TABLE recurring_bulk_requests (
 id TEXT PRIMARY KEY, account_id TEXT NOT NULL REFERENCES accounts(id),
 idempotency_key TEXT NOT NULL, request_hash TEXT NOT NULL,
 request_json TEXT NOT NULL CHECK(json_valid(request_json)), created_at INTEGER NOT NULL,
 UNIQUE(account_id,idempotency_key)
);
CREATE TRIGGER recurring_bulk_request_immutable BEFORE UPDATE ON recurring_bulk_requests
BEGIN SELECT RAISE(ABORT,'bulk request immutable'); END;
CREATE TABLE recurring_bulk_members (
 bulk_id TEXT NOT NULL REFERENCES recurring_bulk_requests(id), member_index INTEGER NOT NULL CHECK(member_index BETWEEN 0 AND 49),
 action_json TEXT NOT NULL CHECK(json_valid(action_json)),
 status TEXT NOT NULL CHECK(status IN ('SUCCESS','REPLAYED','FAILED','RETRY_PENDING')),
 receipt_json TEXT CHECK(receipt_json IS NULL OR json_valid(receipt_json)), error_code TEXT, attempts INTEGER NOT NULL DEFAULT 0,
 PRIMARY KEY(bulk_id,member_index)
);
CREATE TRIGGER recurring_bulk_member_identity BEFORE UPDATE ON recurring_bulk_members
WHEN NEW.bulk_id<>OLD.bulk_id OR NEW.member_index<>OLD.member_index OR NEW.action_json<>OLD.action_json OR OLD.status<>'RETRY_PENDING'
BEGIN SELECT RAISE(ABORT,'bulk member immutable'); END;

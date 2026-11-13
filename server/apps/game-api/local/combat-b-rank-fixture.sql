-- Private LOCAL rank test owner only, never an operating migration/public write API.
CREATE TABLE combat_b_local_rank_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TABLE combat_b_local_rank_receipts(account_id TEXT NOT NULL REFERENCES accounts(id),command_key TEXT NOT NULL,request_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),PRIMARY KEY(account_id,command_key));
CREATE TRIGGER combat_b_local_rank_receipt_immutable BEFORE UPDATE ON combat_b_local_rank_receipts BEGIN SELECT RAISE(ABORT,'LOCAL rank receipt immutable'); END;
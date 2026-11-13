CREATE TABLE run_complete_claim_receipts (
 transaction_id TEXT PRIMARY KEY REFERENCES reward_transactions(id),
 account_id TEXT NOT NULL REFERENCES accounts(id),run_id TEXT NOT NULL REFERENCES runs(id),
 preview_id TEXT NOT NULL REFERENCES run_reward_previews(id),command_key TEXT NOT NULL,
 version TEXT NOT NULL CHECK(version='run.reward-claim.v2.20261019'),
 payload_json TEXT NOT NULL CHECK(json_valid(payload_json)),payload_hash TEXT NOT NULL,
 UNIQUE(account_id,command_key)
);
CREATE TRIGGER run_complete_claim_no_update BEFORE UPDATE ON run_complete_claim_receipts BEGIN SELECT RAISE(ABORT,'immutable_complete_claim'); END;
CREATE TRIGGER run_complete_claim_no_delete BEFORE DELETE ON run_complete_claim_receipts BEGIN SELECT RAISE(ABORT,'immutable_complete_claim'); END;
CREATE TABLE run_complete_claim_guard(valid INTEGER NOT NULL CHECK(valid=1));

-- Proposal revisions only. No entitlement, execution, target membership or payout owner.
CREATE TABLE admin_reward_draft_revisions(
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 batch_id TEXT NOT NULL,revision INTEGER NOT NULL CHECK(revision>0),
 status TEXT NOT NULL CHECK(status='DRAFT'),document_json TEXT NOT NULL CHECK(json_valid(document_json)),
 created_by TEXT NOT NULL REFERENCES admin_principals(id),created_at INTEGER NOT NULL,
 PRIMARY KEY(environment,batch_id,revision)
);
CREATE TABLE admin_reward_draft_heads(
 environment TEXT NOT NULL,batch_id TEXT NOT NULL,revision INTEGER NOT NULL,
 PRIMARY KEY(environment,batch_id),
 FOREIGN KEY(environment,batch_id,revision) REFERENCES admin_reward_draft_revisions(environment,batch_id,revision)
);
CREATE TABLE admin_reward_draft_receipts(
 principal_id TEXT NOT NULL REFERENCES admin_principals(id),environment TEXT NOT NULL,request_key TEXT NOT NULL,
 payload_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(principal_id,environment,request_key)
);
CREATE TABLE admin_reward_draft_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER admin_reward_draft_revision_no_update BEFORE UPDATE ON admin_reward_draft_revisions BEGIN SELECT RAISE(ABORT,'immutable_reward_draft'); END;
CREATE TRIGGER admin_reward_draft_revision_no_delete BEFORE DELETE ON admin_reward_draft_revisions BEGIN SELECT RAISE(ABORT,'immutable_reward_draft'); END;
CREATE TRIGGER admin_reward_draft_receipt_no_update BEFORE UPDATE ON admin_reward_draft_receipts BEGIN SELECT RAISE(ABORT,'immutable_reward_draft_receipt'); END;
CREATE TRIGGER admin_reward_draft_receipt_no_delete BEFORE DELETE ON admin_reward_draft_receipts BEGIN SELECT RAISE(ABORT,'immutable_reward_draft_receipt'); END;

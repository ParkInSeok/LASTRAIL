-- Empty moderation schema. No operating grant, sanction, notice, evidence purge or publication seed.
CREATE TABLE community_moderation_commands(
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),actor_id TEXT NOT NULL,request_key TEXT NOT NULL,
 resource_type TEXT NOT NULL CHECK(resource_type IN ('REPORT','CONTENT')),resource_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),audit_id TEXT NOT NULL REFERENCES admin_audit_logs(id),
 PRIMARY KEY(environment,actor_id,request_key)
);
CREATE TRIGGER community_moderation_command_no_update BEFORE UPDATE ON community_moderation_commands BEGIN SELECT RAISE(ABORT,'community_moderation_receipt_immutable'); END;
CREATE TRIGGER community_moderation_command_no_delete BEFORE DELETE ON community_moderation_commands BEGIN SELECT RAISE(ABORT,'community_moderation_receipt_immutable'); END;
-- Metadata and current state hash only: no historical title/body copies. Same lifetime conflict as existing active renderer proof; no purge owner is introduced here.
CREATE TABLE community_content_moderation_proof(
 environment TEXT NOT NULL,actor_id TEXT NOT NULL,request_key TEXT NOT NULL,content_id TEXT NOT NULL REFERENCES community_contents(id),
 previous_revision INTEGER NOT NULL,next_revision INTEGER NOT NULL CHECK(next_revision=previous_revision+1),
 action TEXT NOT NULL CHECK(action IN ('HIDE','RESTORE')),next_state_hash TEXT NOT NULL,created_at TEXT NOT NULL,
 renderer_version TEXT NOT NULL,retention_policy_version TEXT NOT NULL,
 PRIMARY KEY(content_id,next_revision),FOREIGN KEY(environment,actor_id,request_key) REFERENCES community_moderation_commands(environment,actor_id,request_key)
);
CREATE INDEX community_report_moderation_queue ON community_report_cases(status,submitted_at DESC,case_id COLLATE BINARY DESC);
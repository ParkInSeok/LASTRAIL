-- S-16 additive edit audit. Preserve 0108 deletion schema/receipts byte semantics.
-- No original or updated body/title is copied into action history.
CREATE TABLE community_content_edit_audit (
 account_id TEXT NOT NULL, command_id TEXT NOT NULL, content_id TEXT NOT NULL,
 action TEXT NOT NULL CHECK(action='AUTHOR_EDIT'), previous_revision INTEGER NOT NULL,
 next_revision INTEGER NOT NULL, next_state_hash TEXT NOT NULL,
 created_at TEXT NOT NULL, retain_until TEXT NOT NULL, retention_policy_version TEXT NOT NULL,
 PRIMARY KEY(account_id,command_id), FOREIGN KEY(account_id,command_id)
 REFERENCES community_content_receipts(account_id,command_id)
);

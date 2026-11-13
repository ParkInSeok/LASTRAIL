-- Additive POST creation only. Preserve COMMENT audit constraint and old receipts.
CREATE TABLE community_post_details(content_id TEXT PRIMARY KEY REFERENCES community_contents(id),details_json TEXT NOT NULL CHECK(json_valid(details_json)));
CREATE TABLE community_post_create_audit(
 account_id TEXT NOT NULL,command_id TEXT NOT NULL,content_id TEXT NOT NULL UNIQUE,
 action TEXT NOT NULL CHECK(action='CREATE_POST'),next_revision INTEGER NOT NULL CHECK(next_revision=1),
 next_state_hash TEXT NOT NULL,created_at TEXT NOT NULL,retain_until TEXT NOT NULL,retention_policy_version TEXT NOT NULL,
 PRIMARY KEY(account_id,command_id),FOREIGN KEY(account_id,command_id) REFERENCES community_content_receipts(account_id,command_id)
);
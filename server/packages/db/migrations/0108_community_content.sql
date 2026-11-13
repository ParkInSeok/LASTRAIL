-- Isolated S-16 active-content deletion boundary. No seed, publish or automatic purge.
CREATE TABLE community_contents (
 id TEXT PRIMARY KEY, account_id TEXT NOT NULL, revision INTEGER NOT NULL CHECK(revision>=1),
 state_json TEXT NOT NULL CHECK(json_valid(state_json))
);
CREATE TABLE community_content_receipts (
 account_id TEXT NOT NULL, command_id TEXT NOT NULL, content_id TEXT NOT NULL,
 request_hash TEXT NOT NULL, response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(account_id,command_id), FOREIGN KEY(content_id) REFERENCES community_contents(id)
);
-- Minimal action metadata only; no original body/title, email, token or IP copies.
CREATE TABLE community_content_audit (
 account_id TEXT NOT NULL, command_id TEXT NOT NULL, content_id TEXT NOT NULL,
 action TEXT NOT NULL CHECK(action='AUTHOR_DELETE'), previous_revision INTEGER NOT NULL,
 next_revision INTEGER NOT NULL, created_at TEXT NOT NULL, retain_until TEXT NOT NULL,
 retention_policy_version TEXT NOT NULL,
 PRIMARY KEY(account_id,command_id), FOREIGN KEY(account_id,command_id)
 REFERENCES community_content_receipts(account_id,command_id)
);
CREATE TABLE community_content_write_guard (valid INTEGER NOT NULL CHECK(valid=1));

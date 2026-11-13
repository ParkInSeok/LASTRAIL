-- S-16 new creation sources only. No legacy timestamps/default rows or public activation.
CREATE TABLE community_public_authors(account_id TEXT PRIMARY KEY,public_author_id TEXT NOT NULL UNIQUE);
CREATE TABLE community_content_creation(
 content_id TEXT PRIMARY KEY REFERENCES community_contents(id),account_id TEXT NOT NULL,
 kind TEXT NOT NULL CHECK(kind IN ('POST','COMMENT')),parent_id TEXT,created_at INTEGER NOT NULL CHECK(created_at>=0)
);
CREATE INDEX idx_community_creation_parent ON community_content_creation(parent_id,created_at,content_id);
CREATE INDEX idx_community_creation_owner_rate ON community_content_creation(account_id,kind,created_at);
CREATE TABLE community_content_create_audit(
 account_id TEXT NOT NULL,command_id TEXT NOT NULL,content_id TEXT NOT NULL,
 action TEXT NOT NULL CHECK(action='CREATE_COMMENT'),next_revision INTEGER NOT NULL CHECK(next_revision=1),
 next_state_hash TEXT NOT NULL,created_at TEXT NOT NULL,retain_until TEXT NOT NULL,retention_policy_version TEXT NOT NULL,
 PRIMARY KEY(account_id,command_id),FOREIGN KEY(account_id,command_id) REFERENCES community_content_receipts(account_id,command_id)
);
CREATE INDEX idx_community_contents_owner ON community_contents(account_id,id);
CREATE UNIQUE INDEX idx_community_create_audit_content ON community_content_create_audit(content_id);

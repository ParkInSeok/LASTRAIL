-- Additive body-free current proof for approved POST comment lock/unlock. No grant, publication or operating seed.
CREATE TABLE community_content_comment_lock_proof(
 environment TEXT NOT NULL,actor_id TEXT NOT NULL,request_key TEXT NOT NULL,content_id TEXT NOT NULL REFERENCES community_contents(id),
 previous_revision INTEGER NOT NULL,next_revision INTEGER NOT NULL CHECK(next_revision=previous_revision+1),
 action TEXT NOT NULL CHECK(action IN ('LOCK_COMMENTS','UNLOCK_COMMENTS')),next_state_hash TEXT NOT NULL,created_at TEXT NOT NULL,
 renderer_version TEXT NOT NULL,retention_policy_version TEXT NOT NULL,
 PRIMARY KEY(content_id,next_revision),FOREIGN KEY(environment,actor_id,request_key) REFERENCES community_moderation_commands(environment,actor_id,request_key)
);
-- Active proof retirement remains bound to the existing approved source/receipt lifecycle, not an automatic purge here.
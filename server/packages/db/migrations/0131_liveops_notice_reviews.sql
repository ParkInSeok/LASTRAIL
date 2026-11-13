-- Review metadata only. Notice revisions/commands remain the execution owner.
CREATE TABLE liveops_notice_reviews(
 review_id TEXT PRIMARY KEY,
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 notice_id TEXT NOT NULL,
 revision INTEGER NOT NULL,
 document_json TEXT NOT NULL,
 principal_id TEXT NOT NULL REFERENCES admin_principals(id),
 request_key TEXT NOT NULL,
 payload_hash TEXT NOT NULL,
 response_json TEXT NOT NULL,
 created_at INTEGER NOT NULL,
 UNIQUE(principal_id,environment,request_key),
 FOREIGN KEY(environment,notice_id,revision) REFERENCES notice_revisions(environment,notice_id,revision)
);
CREATE TRIGGER liveops_notice_reviews_no_update BEFORE UPDATE ON liveops_notice_reviews
 BEGIN SELECT RAISE(ABORT,'immutable_liveops_review'); END;
CREATE TRIGGER liveops_notice_reviews_no_delete BEFORE DELETE ON liveops_notice_reviews
 BEGIN SELECT RAISE(ABORT,'immutable_liveops_review'); END;

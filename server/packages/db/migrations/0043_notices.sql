-- Notice content has one immutable revision history and separate draft/public references.
-- No production notice or environment seed is created.
CREATE TABLE notice_revisions (
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 notice_id TEXT NOT NULL,
 revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
 status TEXT NOT NULL CHECK(status IN ('DRAFT','SCHEDULED','PUBLISHED','WITHDRAWN')),
 document_json TEXT NOT NULL CHECK(json_valid(document_json)),
 updated_at INTEGER NOT NULL,
 PRIMARY KEY(environment,notice_id,revision)
);
CREATE TABLE notice_heads (
 environment TEXT NOT NULL, notice_id TEXT NOT NULL,
 revision INTEGER NOT NULL, published_revision INTEGER,
 PRIMARY KEY(environment,notice_id),
 FOREIGN KEY(environment,notice_id,revision) REFERENCES notice_revisions(environment,notice_id,revision),
 FOREIGN KEY(environment,notice_id,published_revision) REFERENCES notice_revisions(environment,notice_id,revision)
);
CREATE TABLE notice_command_receipts (
 principal_id TEXT NOT NULL REFERENCES admin_principals(id),
 environment TEXT NOT NULL,request_key TEXT NOT NULL,payload_hash TEXT NOT NULL,
 audit_id TEXT NOT NULL REFERENCES admin_audit_logs(id),
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(principal_id,environment,request_key)
);
CREATE TABLE notice_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER notice_revision_no_update BEFORE UPDATE ON notice_revisions
BEGIN SELECT RAISE(ABORT,'notice_revision_immutable'); END;
CREATE TRIGGER notice_revision_no_delete BEFORE DELETE ON notice_revisions
BEGIN SELECT RAISE(ABORT,'notice_revision_immutable'); END;
CREATE TRIGGER notice_receipt_no_update BEFORE UPDATE ON notice_command_receipts
BEGIN SELECT RAISE(ABORT,'notice_receipt_immutable'); END;
CREATE TRIGGER notice_receipt_no_delete BEFORE DELETE ON notice_command_receipts
BEGIN SELECT RAISE(ABORT,'notice_receipt_immutable'); END;

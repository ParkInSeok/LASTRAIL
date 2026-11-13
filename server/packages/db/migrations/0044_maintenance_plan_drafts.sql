-- Drafts only. This migration does not install a gate, schedule, status publication, or policy seed.
CREATE TABLE maintenance_plan_revisions (
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 maintenance_id TEXT NOT NULL,
 draft_revision INTEGER NOT NULL CHECK(typeof(draft_revision)='integer' AND draft_revision BETWEEN 1 AND 9007199254740991),
 document_json TEXT NOT NULL CHECK(json_valid(document_json)),
 updated_at INTEGER NOT NULL,
 PRIMARY KEY(environment,maintenance_id,draft_revision)
);
CREATE TABLE maintenance_plan_heads (
 environment TEXT NOT NULL,maintenance_id TEXT NOT NULL,draft_revision INTEGER NOT NULL,
 PRIMARY KEY(environment,maintenance_id),
 FOREIGN KEY(environment,maintenance_id,draft_revision) REFERENCES maintenance_plan_revisions(environment,maintenance_id,draft_revision)
);
CREATE TABLE maintenance_plan_receipts (
 principal_id TEXT NOT NULL REFERENCES admin_principals(id),
 environment TEXT NOT NULL,request_key TEXT NOT NULL,payload_hash TEXT NOT NULL,
 audit_id TEXT NOT NULL REFERENCES admin_audit_logs(id),
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(principal_id,environment,request_key)
);
CREATE TABLE maintenance_plan_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER maintenance_plan_revision_no_update BEFORE UPDATE ON maintenance_plan_revisions
BEGIN SELECT RAISE(ABORT,'maintenance_plan_revision_immutable'); END;
CREATE TRIGGER maintenance_plan_revision_no_delete BEFORE DELETE ON maintenance_plan_revisions
BEGIN SELECT RAISE(ABORT,'maintenance_plan_revision_immutable'); END;
CREATE TRIGGER maintenance_plan_receipt_no_update BEFORE UPDATE ON maintenance_plan_receipts
BEGIN SELECT RAISE(ABORT,'maintenance_plan_receipt_immutable'); END;
CREATE TRIGGER maintenance_plan_receipt_no_delete BEFORE DELETE ON maintenance_plan_receipts
BEGIN SELECT RAISE(ABORT,'maintenance_plan_receipt_immutable'); END;

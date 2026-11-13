-- No operational seed. New writers require an explicitly initialized policy.
CREATE TABLE maintenance_runtime_revisions (
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 application_id TEXT NOT NULL CHECK(length(application_id) BETWEEN 1 AND 80),
 revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
 state TEXT NOT NULL CHECK(state IN ('NORMAL','SCHEDULED','ACTIVE','COMPLETED','CANCELLED')),
 maintenance_id TEXT, draft_revision INTEGER,
 drain_starts_at INTEGER, starts_at INTEGER, estimated_end_at INTEGER, ended_at INTEGER,
 actor_id TEXT NOT NULL REFERENCES admin_principals(id), reason TEXT NOT NULL,
 created_at INTEGER NOT NULL CHECK(typeof(created_at)='integer' AND created_at BETWEEN 0 AND 9007199254740991),
 PRIMARY KEY(environment,application_id,revision),
 FOREIGN KEY(environment,maintenance_id,draft_revision) REFERENCES maintenance_plan_revisions(environment,maintenance_id,draft_revision),
 CHECK((state='NORMAL' AND revision=1 AND maintenance_id IS NULL AND draft_revision IS NULL AND starts_at IS NULL AND drain_starts_at IS NULL AND estimated_end_at IS NULL AND ended_at IS NULL)
 OR (state<>'NORMAL' AND maintenance_id IS NOT NULL AND draft_revision IS NOT NULL AND typeof(draft_revision)='integer' AND draft_revision>0
 AND starts_at IS NOT NULL AND typeof(starts_at)='integer' AND starts_at BETWEEN 0 AND 9007199254740991
 AND (drain_starts_at IS NULL OR (typeof(drain_starts_at)='integer' AND drain_starts_at BETWEEN 0 AND starts_at))
 AND (estimated_end_at IS NULL OR (typeof(estimated_end_at)='integer' AND estimated_end_at>starts_at AND estimated_end_at<=9007199254740991))
 AND ((state IN ('SCHEDULED','ACTIVE') AND ended_at IS NULL) OR (state IN ('COMPLETED','CANCELLED') AND ended_at IS NOT NULL AND typeof(ended_at)='integer' AND ended_at BETWEEN 0 AND 9007199254740991))))
);
CREATE TABLE maintenance_runtime_heads (
 environment TEXT NOT NULL, application_id TEXT NOT NULL, revision INTEGER NOT NULL,
 PRIMARY KEY(environment,application_id),
 FOREIGN KEY(environment,application_id,revision) REFERENCES maintenance_runtime_revisions(environment,application_id,revision)
);
CREATE TRIGGER maintenance_head_revision BEFORE UPDATE ON maintenance_runtime_heads
WHEN NEW.environment<>OLD.environment OR NEW.application_id<>OLD.application_id OR NEW.revision<>OLD.revision+1
BEGIN SELECT RAISE(ABORT,'maintenance_revision_conflict'); END;
CREATE TRIGGER maintenance_head_no_delete BEFORE DELETE ON maintenance_runtime_heads
BEGIN SELECT RAISE(ABORT,'maintenance_head_immutable'); END;
CREATE TRIGGER maintenance_revision_no_update BEFORE UPDATE ON maintenance_runtime_revisions
BEGIN SELECT RAISE(ABORT,'maintenance_revision_immutable'); END;
CREATE TRIGGER maintenance_revision_no_delete BEFORE DELETE ON maintenance_runtime_revisions
BEGIN SELECT RAISE(ABORT,'maintenance_revision_immutable'); END;
CREATE TABLE maintenance_runtime_receipts (
 principal_id TEXT NOT NULL REFERENCES admin_principals(id),environment TEXT NOT NULL,request_key TEXT NOT NULL,
 payload_hash TEXT NOT NULL,audit_id TEXT NOT NULL REFERENCES admin_audit_logs(id),response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(principal_id,environment,request_key)
);
CREATE TRIGGER maintenance_receipt_no_update BEFORE UPDATE ON maintenance_runtime_receipts
BEGIN SELECT RAISE(ABORT,'maintenance_receipt_immutable'); END;
CREATE TRIGGER maintenance_receipt_no_delete BEFORE DELETE ON maintenance_runtime_receipts
BEGIN SELECT RAISE(ABORT,'maintenance_receipt_immutable'); END;
CREATE TABLE maintenance_runtime_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER maintenance_runtime_guard_ignore BEFORE INSERT ON maintenance_runtime_guard WHEN NEW.valid=1
BEGIN SELECT RAISE(IGNORE); END;
CREATE TABLE maintenance_admission_guard(code TEXT NOT NULL);
CREATE TRIGGER maintenance_admission_ok BEFORE INSERT ON maintenance_admission_guard WHEN NEW.code='OK'
BEGIN SELECT RAISE(IGNORE); END;
CREATE TRIGGER maintenance_admission_blocked BEFORE INSERT ON maintenance_admission_guard WHEN NEW.code='BLOCKED'
BEGIN SELECT RAISE(ABORT,'maintenance_new_request_blocked'); END;
CREATE TRIGGER maintenance_admission_unavailable BEFORE INSERT ON maintenance_admission_guard WHEN NEW.code IS NULL OR NEW.code NOT IN ('OK','BLOCKED')
BEGIN SELECT RAISE(ABORT,'maintenance_policy_unavailable'); END;

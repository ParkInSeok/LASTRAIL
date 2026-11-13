-- admin_jobs remains the sole execution-state owner. No operating profiles or runners are seeded.
CREATE TABLE unity_build_requests (
 job_id TEXT PRIMARY KEY REFERENCES admin_jobs(id),
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 platform TEXT NOT NULL, build_number INTEGER NOT NULL CHECK(build_number>0),
 profile_json TEXT NOT NULL CHECK(json_valid(profile_json)),
 UNIQUE(environment,platform,build_number)
);
CREATE TABLE unity_build_receipts (
 principal_id TEXT NOT NULL REFERENCES admin_principals(id),
 environment TEXT NOT NULL, request_key TEXT NOT NULL,
 payload_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(principal_id,environment,request_key)
);
CREATE TRIGGER unity_build_request_immutable BEFORE UPDATE ON unity_build_requests BEGIN SELECT RAISE(ABORT,'unity_build_request_immutable'); END;
CREATE TRIGGER unity_build_request_no_delete BEFORE DELETE ON unity_build_requests BEGIN SELECT RAISE(ABORT,'unity_build_request_immutable'); END;
CREATE TRIGGER unity_build_receipt_immutable BEFORE UPDATE ON unity_build_receipts BEGIN SELECT RAISE(ABORT,'unity_build_receipt_immutable'); END;
CREATE TRIGGER unity_build_receipt_no_delete BEFORE DELETE ON unity_build_receipts BEGIN SELECT RAISE(ABORT,'unity_build_receipt_immutable'); END;
CREATE TABLE unity_build_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
-- Attempt identity is immutable; current stage/result remains in admin_jobs.
CREATE TABLE unity_build_attempts (
 attempt_id TEXT PRIMARY KEY,job_id TEXT NOT NULL UNIQUE REFERENCES admin_jobs(id),
 runner_id TEXT NOT NULL,environment TEXT NOT NULL,created_at INTEGER NOT NULL,
 source_commit TEXT NOT NULL,profile_hash TEXT NOT NULL
);
CREATE TRIGGER unity_build_attempt_immutable BEFORE UPDATE ON unity_build_attempts BEGIN SELECT RAISE(ABORT,'unity_build_attempt_immutable'); END;
CREATE TRIGGER unity_build_attempt_no_delete BEFORE DELETE ON unity_build_attempts BEGIN SELECT RAISE(ABORT,'unity_build_attempt_immutable'); END;
CREATE TABLE unity_build_runner_receipts (
 attempt_id TEXT NOT NULL REFERENCES unity_build_attempts(attempt_id),
 event_id TEXT NOT NULL,payload_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(attempt_id,event_id)
);
CREATE TRIGGER unity_build_runner_receipt_immutable BEFORE UPDATE ON unity_build_runner_receipts BEGIN SELECT RAISE(ABORT,'unity_build_runner_receipt_immutable'); END;
CREATE TRIGGER unity_build_runner_receipt_no_delete BEFORE DELETE ON unity_build_runner_receipts BEGIN SELECT RAISE(ABORT,'unity_build_runner_receipt_immutable'); END;

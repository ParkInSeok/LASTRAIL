CREATE TABLE combat_host_resume_receipts (
 run_id TEXT NOT NULL REFERENCES combat_host_report_states(run_id),
 request_id TEXT NOT NULL,
 sender_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,request_id)
);
CREATE TRIGGER combat_host_resume_no_update BEFORE UPDATE ON combat_host_resume_receipts BEGIN SELECT RAISE(ABORT,'immutable_resume_receipt'); END;
CREATE TRIGGER combat_host_resume_no_delete BEFORE DELETE ON combat_host_resume_receipts BEGIN SELECT RAISE(ABORT,'immutable_resume_receipt'); END;

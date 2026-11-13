CREATE TABLE combat_final_evidence (
 run_id TEXT NOT NULL REFERENCES combat_host_report_states(run_id),
 record_id TEXT NOT NULL,
 report_json TEXT NOT NULL CHECK(json_valid(report_json)),
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,record_id)
);
CREATE TABLE combat_final_submissions (
 run_id TEXT NOT NULL,
 sender_id TEXT NOT NULL,
 request_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 record_id TEXT NOT NULL,
 PRIMARY KEY(run_id,sender_id,request_id),
 FOREIGN KEY(run_id,record_id) REFERENCES combat_final_evidence(run_id,record_id)
);
CREATE TRIGGER combat_final_evidence_no_update BEFORE UPDATE ON combat_final_evidence BEGIN SELECT RAISE(ABORT,'immutable_final_evidence'); END;
CREATE TRIGGER combat_final_evidence_no_delete BEFORE DELETE ON combat_final_evidence BEGIN SELECT RAISE(ABORT,'immutable_final_evidence'); END;
CREATE TRIGGER combat_final_submission_no_update BEFORE UPDATE ON combat_final_submissions BEGIN SELECT RAISE(ABORT,'immutable_final_submission'); END;
CREATE TRIGGER combat_final_submission_no_delete BEFORE DELETE ON combat_final_submissions BEGIN SELECT RAISE(ABORT,'immutable_final_submission'); END;
CREATE TABLE combat_final_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

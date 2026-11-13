-- Review clock is independent of suspended combat retention; never a reward or terminal result.
CREATE TABLE combat_result_reviews (
 run_id TEXT PRIMARY KEY REFERENCES combat_host_report_states(run_id),
 state_json TEXT NOT NULL CHECK(json_valid(state_json))
);
CREATE TABLE combat_result_review_receipts (
 run_id TEXT NOT NULL REFERENCES combat_result_reviews(run_id),
 request_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,request_id)
);
CREATE TRIGGER combat_result_review_no_update BEFORE UPDATE ON combat_result_reviews BEGIN SELECT RAISE(ABORT,'immutable_review_origin'); END;
CREATE TRIGGER combat_result_review_no_delete BEFORE DELETE ON combat_result_reviews BEGIN SELECT RAISE(ABORT,'immutable_review_origin'); END;
CREATE TRIGGER combat_result_review_receipt_no_update BEFORE UPDATE ON combat_result_review_receipts BEGIN SELECT RAISE(ABORT,'immutable_review_receipt'); END;
CREATE TRIGGER combat_result_review_receipt_no_delete BEFORE DELETE ON combat_result_review_receipts BEGIN SELECT RAISE(ABORT,'immutable_review_receipt'); END;
CREATE TABLE combat_review_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

-- First valid envelope receipt time only; no invented timestamps for historical evidence.
CREATE TABLE combat_final_recovery_origins (
 run_id TEXT PRIMARY KEY REFERENCES combat_host_report_states(run_id),
 first_received_at INTEGER NOT NULL CHECK(first_received_at>=0 AND first_received_at<=9007199168340991)
);
CREATE TRIGGER combat_final_origin_no_update BEFORE UPDATE ON combat_final_recovery_origins BEGIN SELECT RAISE(ABORT,'immutable_final_recovery_origin'); END;
CREATE TRIGGER combat_final_origin_no_delete BEFORE DELETE ON combat_final_recovery_origins BEGIN SELECT RAISE(ABORT,'immutable_final_recovery_origin'); END;

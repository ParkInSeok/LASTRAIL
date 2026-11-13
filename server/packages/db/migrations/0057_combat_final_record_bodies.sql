CREATE TABLE combat_final_record_bodies (
 run_id TEXT NOT NULL,
 record_id TEXT NOT NULL,
 -- JSON string encoding preserves leading BOM and embedded control characters.
 record_text TEXT NOT NULL CHECK(json_valid(record_text) AND json_type(record_text)='text'),
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(run_id,record_id),
 FOREIGN KEY(run_id,record_id) REFERENCES combat_final_evidence(run_id,record_id)
);
CREATE TRIGGER combat_final_record_body_no_update BEFORE UPDATE ON combat_final_record_bodies BEGIN SELECT RAISE(ABORT,'immutable_final_record_body'); END;
CREATE TRIGGER combat_final_record_body_no_delete BEFORE DELETE ON combat_final_record_bodies BEGIN SELECT RAISE(ABORT,'immutable_final_record_body'); END;

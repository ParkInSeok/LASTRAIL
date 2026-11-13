-- Combat occupancy only. Existing run status, immutable evidence and resource journals stay intact.
CREATE TABLE combat_review_releases (
 run_id TEXT PRIMARY KEY REFERENCES combat_result_reviews(run_id),
 request_id TEXT NOT NULL,
 request_hash TEXT NOT NULL,
 response_json TEXT NOT NULL CHECK(json_valid(response_json))
);
CREATE TRIGGER combat_review_release_no_update BEFORE UPDATE ON combat_review_releases BEGIN SELECT RAISE(ABORT,'immutable_review_release'); END;
CREATE TRIGGER combat_review_release_no_delete BEFORE DELETE ON combat_review_releases BEGIN SELECT RAISE(ABORT,'immutable_review_release'); END;
CREATE VIEW active_combat_runs AS SELECT r.* FROM runs r
 WHERE r.status IN ('ACTIVE','FINISH_PENDING','QUARANTINED')
 AND NOT EXISTS(SELECT 1 FROM combat_review_releases x JOIN combat_result_reviews v ON v.run_id=x.run_id WHERE x.run_id=r.id);

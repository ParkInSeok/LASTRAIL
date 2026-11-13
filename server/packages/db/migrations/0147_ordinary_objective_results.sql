CREATE TABLE ordinary_objective_results (
 run_id TEXT NOT NULL REFERENCES runs(id), evidence_ref TEXT NOT NULL,
 account_id TEXT NOT NULL REFERENCES accounts(id), request_hash TEXT NOT NULL,
 evidence_json TEXT NOT NULL CHECK(json_valid(evidence_json)),
 holds_json TEXT NOT NULL CHECK(json_valid(holds_json)),
 PRIMARY KEY(run_id,evidence_ref)
);
CREATE TRIGGER ordinary_objective_result_no_update BEFORE UPDATE ON ordinary_objective_results
BEGIN SELECT RAISE(ABORT,'ordinary_objective_result_immutable'); END;
CREATE TRIGGER ordinary_objective_result_no_delete BEFORE DELETE ON ordinary_objective_results
BEGIN SELECT RAISE(ABORT,'ordinary_objective_result_immutable'); END;

-- Independent original verification receipts. No operating source/clock seeded.
CREATE TABLE b_arrival_originals(
 run_id TEXT NOT NULL REFERENCES runs(id),
 account_id TEXT NOT NULL REFERENCES accounts(id),
 source_ref TEXT NOT NULL,
 original_event_id TEXT NOT NULL,
 destination_event_id TEXT NOT NULL,
 input_hash TEXT NOT NULL,
 proof_json TEXT NOT NULL CHECK(json_valid(proof_json)),
 receipt_json TEXT NOT NULL CHECK(json_valid(receipt_json)),
 PRIMARY KEY(run_id,source_ref),
 UNIQUE(run_id,original_event_id),
 UNIQUE(run_id,destination_event_id)
);
CREATE TRIGGER b_arrival_original_no_update BEFORE UPDATE ON b_arrival_originals
 BEGIN SELECT RAISE(ABORT,'immutable_b_arrival_original'); END;
CREATE TRIGGER b_arrival_original_no_delete BEFORE DELETE ON b_arrival_originals
 BEGIN SELECT RAISE(ABORT,'immutable_b_arrival_original'); END;
CREATE TABLE b_arrival_original_revocations(
 run_id TEXT NOT NULL,
 source_ref TEXT NOT NULL,
 reason TEXT NOT NULL,
 PRIMARY KEY(run_id,source_ref),
 FOREIGN KEY(run_id,source_ref) REFERENCES b_arrival_originals(run_id,source_ref)
);
CREATE TRIGGER b_arrival_original_revocation_no_update BEFORE UPDATE ON b_arrival_original_revocations
 BEGIN SELECT RAISE(ABORT,'immutable_b_arrival_original_revocation'); END;
CREATE TRIGGER b_arrival_original_revocation_no_delete BEFORE DELETE ON b_arrival_original_revocations
 BEGIN SELECT RAISE(ABORT,'immutable_b_arrival_original_revocation'); END;

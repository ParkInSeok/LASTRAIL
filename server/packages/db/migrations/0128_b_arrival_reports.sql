-- Original local checkpoint diagnostics are NOT accepted travel arrival evidence.
CREATE TABLE b_arrival_reports (
 run_id TEXT NOT NULL REFERENCES runs(id), account_id TEXT NOT NULL REFERENCES accounts(id),
 evidence_ref TEXT NOT NULL, request_id TEXT NOT NULL, request_hash TEXT NOT NULL,
 report_json TEXT NOT NULL, receipt_json TEXT NOT NULL,
 PRIMARY KEY(run_id,evidence_ref), UNIQUE(run_id,request_id)
);
CREATE TRIGGER b_arrival_reports_no_update BEFORE UPDATE ON b_arrival_reports BEGIN SELECT RAISE(ABORT,'b_arrival_report_immutable'); END;
CREATE TRIGGER b_arrival_reports_no_delete BEFORE DELETE ON b_arrival_reports BEGIN SELECT RAISE(ABORT,'b_arrival_report_immutable'); END;
CREATE TABLE b_arrival_report_revocations (
 run_id TEXT NOT NULL, evidence_ref TEXT NOT NULL, reason TEXT NOT NULL,
 PRIMARY KEY(run_id,evidence_ref), FOREIGN KEY(run_id,evidence_ref) REFERENCES b_arrival_reports(run_id,evidence_ref)
);
CREATE TRIGGER b_arrival_revocations_no_update BEFORE UPDATE ON b_arrival_report_revocations BEGIN SELECT RAISE(ABORT,'b_arrival_revocation_immutable'); END;
CREATE TRIGGER b_arrival_revocations_no_delete BEFORE DELETE ON b_arrival_report_revocations BEGIN SELECT RAISE(ABORT,'b_arrival_revocation_immutable'); END;

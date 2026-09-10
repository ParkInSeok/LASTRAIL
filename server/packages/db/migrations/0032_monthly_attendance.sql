-- Empty policy registry is reused from activity_definitions. No fixture activation.
CREATE TABLE monthly_attendance_cycles (
 account_id TEXT NOT NULL REFERENCES accounts(id), cycle_id TEXT NOT NULL,
 activity_id TEXT NOT NULL REFERENCES activity_definitions(id), revision INTEGER NOT NULL CHECK(revision>0),
 PRIMARY KEY(account_id,cycle_id)
);
CREATE TABLE monthly_attendance_visits (
 account_id TEXT NOT NULL, cycle_id TEXT NOT NULL, day_index INTEGER NOT NULL CHECK(day_index BETWEEN 0 AND 30),
 claim_id TEXT NOT NULL REFERENCES activity_claims(id),
 PRIMARY KEY(account_id,cycle_id,day_index),
 FOREIGN KEY(account_id,cycle_id) REFERENCES monthly_attendance_cycles(account_id,cycle_id)
);
CREATE TABLE monthly_attendance_cells (
 account_id TEXT NOT NULL, cycle_id TEXT NOT NULL, cell INTEGER NOT NULL CHECK(cell BETWEEN 1 AND 28),
 source TEXT NOT NULL CHECK(source IN ('NATURAL','RECOVERY')),
 day_index INTEGER NOT NULL CHECK(day_index BETWEEN 0 AND 30),
 claim_id TEXT NOT NULL UNIQUE REFERENCES activity_claims(id),
 PRIMARY KEY(account_id,cycle_id,cell), UNIQUE(account_id,cycle_id,day_index),
 FOREIGN KEY(account_id,cycle_id) REFERENCES monthly_attendance_cycles(account_id,cycle_id)
);
CREATE TABLE monthly_attendance_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TABLE monthly_attendance_requests (
 account_id TEXT NOT NULL REFERENCES accounts(id), operation TEXT NOT NULL,
 idempotency_key TEXT NOT NULL, request_hash TEXT NOT NULL,
 claim_id TEXT NOT NULL REFERENCES activity_claims(id),
 PRIMARY KEY(account_id,operation,idempotency_key)
);
CREATE TRIGGER monthly_request_immutable BEFORE UPDATE ON monthly_attendance_requests
BEGIN SELECT RAISE(ABORT,'monthly request immutable'); END;
CREATE TRIGGER monthly_request_no_replace BEFORE INSERT ON monthly_attendance_requests
WHEN EXISTS(SELECT 1 FROM monthly_attendance_requests WHERE account_id=NEW.account_id AND operation=NEW.operation AND idempotency_key=NEW.idempotency_key)
BEGIN SELECT RAISE(ABORT,'monthly request immutable'); END;

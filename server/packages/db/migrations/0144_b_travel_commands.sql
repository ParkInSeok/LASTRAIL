-- Accepted noncombat travel inputs. No client elapsed or clock is persisted as authority.
CREATE TABLE b_travel_commands (
 run_id TEXT NOT NULL, destination_event_id TEXT NOT NULL,
 revision INTEGER NOT NULL CHECK(revision>0), request_id TEXT NOT NULL,
 request_hash TEXT NOT NULL, accepted_at_ms INTEGER NOT NULL CHECK(accepted_at_ms>=0),
 response_json TEXT NOT NULL, events_json TEXT NOT NULL, report_json TEXT,
 PRIMARY KEY(run_id,destination_event_id,revision), UNIQUE(run_id,request_id),
 FOREIGN KEY(run_id,destination_event_id) REFERENCES operation_map_destination_times(run_id,event_id)
);
CREATE TRIGGER b_travel_commands_immutable BEFORE UPDATE ON b_travel_commands
BEGIN SELECT RAISE(ABORT,'b_travel_commands_immutable'); END;
CREATE TRIGGER b_travel_commands_no_delete BEFORE DELETE ON b_travel_commands
BEGIN SELECT RAISE(ABORT,'b_travel_commands_immutable'); END;
-- Bounded replay projection; immutable command receipts remain the audit source.
CREATE TABLE b_travel_heads (
 run_id TEXT NOT NULL, destination_event_id TEXT NOT NULL,
 revision INTEGER NOT NULL CHECK(revision>0), journal_json TEXT NOT NULL,
 PRIMARY KEY(run_id,destination_event_id),
 FOREIGN KEY(run_id,destination_event_id,revision) REFERENCES b_travel_commands(run_id,destination_event_id,revision)
);

-- Server observation only: no elapsed credit, accepted input or arrival proof.
-- Historical receipts have no trustworthy time and are intentionally not backfilled.
CREATE TABLE operation_map_destination_times (
 run_id TEXT NOT NULL,
 event_id TEXT NOT NULL,
 confirmed_at_ms INTEGER NOT NULL CHECK(confirmed_at_ms >= 0),
 PRIMARY KEY(run_id,event_id),
 FOREIGN KEY(run_id,event_id) REFERENCES operation_map_receipts(run_id,event_id)
);
CREATE TRIGGER operation_map_destination_time_immutable BEFORE UPDATE ON operation_map_destination_times
BEGIN SELECT RAISE(ABORT,'operation_map_destination_time_immutable'); END;
CREATE TRIGGER operation_map_destination_time_no_delete BEFORE DELETE ON operation_map_destination_times
BEGIN SELECT RAISE(ABORT,'operation_map_destination_time_immutable'); END;

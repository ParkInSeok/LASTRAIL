-- Immutable original repair stock for an entered maintenance station.
CREATE TABLE run_maintenance_offers (
  run_id TEXT NOT NULL REFERENCES run_supplies_state(run_id) ON DELETE CASCADE,
  station_id TEXT NOT NULL CHECK(length(station_id) BETWEEN 1 AND 128),
  source_ref TEXT NOT NULL CHECK(length(source_ref) BETWEEN 1 AND 128),
  source_version TEXT NOT NULL CHECK(length(source_version) BETWEEN 1 AND 128),
  services_json TEXT NOT NULL CHECK(json_valid(services_json)),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  PRIMARY KEY(run_id,station_id),
  UNIQUE(run_id,source_ref)
);
CREATE TRIGGER run_maintenance_offer_immutable_update BEFORE UPDATE ON run_maintenance_offers
BEGIN SELECT RAISE(ABORT,'run_maintenance_offer_immutable'); END;
CREATE TRIGGER run_maintenance_offer_immutable_delete BEFORE DELETE ON run_maintenance_offers
BEGIN SELECT RAISE(ABORT,'run_maintenance_offer_immutable'); END;
CREATE TABLE run_maintenance_offer_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

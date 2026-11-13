-- Capacity-only state for newly pinned v18 STARTs. No historical backfill or item issuance.
CREATE TABLE run_equipment_inventory (
  run_id TEXT PRIMARY KEY REFERENCES runs(id) ON DELETE CASCADE,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
  capacity_version TEXT NOT NULL CHECK(capacity_version='equipment.inventory-capacity.v18'),
  do05_rank INTEGER NOT NULL CHECK(typeof(do05_rank)='integer' AND do05_rank BETWEEN 0 AND 4),
  capacity_slots INTEGER NOT NULL CHECK(typeof(capacity_slots)='integer' AND capacity_slots=30+6*do05_rank)
);
CREATE TRIGGER run_equipment_inventory_start_guard BEFORE INSERT ON run_equipment_inventory
BEGIN
  SELECT CASE WHEN NEW.revision<>1 OR NOT EXISTS (
    SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id
    WHERE r.id=NEW.run_id AND r.account_id=NEW.account_id AND r.status='ACTIVE'
      AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')=NEW.capacity_version
      AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')=NEW.do05_rank
      AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.slots')=NEW.capacity_slots
  ) THEN RAISE(ABORT,'equipment_inventory_start_mismatch') END;
END;
CREATE TRIGGER run_equipment_inventory_pin_immutable
BEFORE UPDATE OF account_id,capacity_version,do05_rank,capacity_slots ON run_equipment_inventory
BEGIN SELECT RAISE(ABORT,'equipment_inventory_pin_immutable'); END;

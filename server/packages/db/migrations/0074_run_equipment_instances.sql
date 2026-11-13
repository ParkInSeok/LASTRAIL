-- Physical identity/location core only. No producer or client issuance path is installed.
CREATE TABLE run_equipment_instances (
  instance_id TEXT PRIMARY KEY CHECK(length(instance_id) BETWEEN 1 AND 128),
  run_id TEXT NOT NULL REFERENCES run_equipment_inventory(run_id) ON DELETE CASCADE,
  source_item_id TEXT NOT NULL CHECK(length(source_item_id) BETWEEN 1 AND 128),
  definition_id TEXT NOT NULL CHECK(length(definition_id) BETWEEN 1 AND 128),
  definition_version TEXT NOT NULL CHECK(length(definition_version) BETWEEN 1 AND 128),
  category_id TEXT NOT NULL CHECK(length(category_id) BETWEEN 1 AND 128),
  set_tag TEXT NOT NULL CHECK(set_tag IN ('SET01','SET02','SET03','SET04','SET05','SET06')),
  star INTEGER NOT NULL CHECK(typeof(star)='integer' AND star BETWEEN 1 AND 5),
  locked INTEGER NOT NULL CHECK(typeof(locked)='integer' AND locked IN (0,1)),
  location_kind TEXT NOT NULL CHECK(location_kind IN ('STORED','EQUIPPED')),
  wearer_kind TEXT,
  wearer_id TEXT,
  slot_index INTEGER,
  revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
  UNIQUE(run_id,source_item_id),
  CHECK((location_kind='STORED' AND wearer_kind IS NULL AND wearer_id IS NULL AND slot_index IS NULL)
     OR (location_kind='EQUIPPED' AND wearer_kind IN ('operator','turret')
       AND wearer_id IS NOT NULL AND length(wearer_id) BETWEEN 1 AND 128
       AND typeof(slot_index)='integer' AND slot_index BETWEEN 0 AND 2))
);
CREATE UNIQUE INDEX run_equipment_one_per_slot ON run_equipment_instances(run_id,wearer_kind,wearer_id,slot_index)
  WHERE location_kind='EQUIPPED';
CREATE INDEX run_equipment_by_run ON run_equipment_instances(run_id,instance_id);
CREATE TRIGGER run_equipment_instance_revision_guard BEFORE UPDATE ON run_equipment_instances
WHEN NEW.revision<>OLD.revision+1
BEGIN SELECT RAISE(ABORT,'equipment_instance_revision_invalid'); END;
CREATE TRIGGER run_equipment_instance_identity_guard
BEFORE UPDATE OF instance_id,run_id,source_item_id ON run_equipment_instances
BEGIN SELECT RAISE(ABORT,'equipment_instance_identity_immutable'); END;

-- Original node reward identities are reserved here; pending items are not physical inventory.
CREATE TABLE run_equipment_reward_bundles (
  bundle_id TEXT PRIMARY KEY CHECK(length(bundle_id) BETWEEN 1 AND 128),
  run_id TEXT NOT NULL REFERENCES run_equipment_inventory(run_id) ON DELETE CASCADE,
  node_id TEXT NOT NULL CHECK(length(node_id) BETWEEN 1 AND 128),
  source_ref TEXT NOT NULL CHECK(length(source_ref) BETWEEN 1 AND 128),
  source_version TEXT NOT NULL CHECK(length(source_version) BETWEEN 1 AND 128),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  UNIQUE(run_id,node_id),
  UNIQUE(run_id,bundle_id)
);
CREATE TABLE run_equipment_reward_items (
  item_id TEXT PRIMARY KEY CHECK(length(item_id) BETWEEN 1 AND 128),
  run_id TEXT NOT NULL,
  bundle_id TEXT NOT NULL,
  instance_id TEXT NOT NULL UNIQUE CHECK(length(instance_id) BETWEEN 1 AND 128),
  sort_index INTEGER NOT NULL CHECK(typeof(sort_index)='integer' AND sort_index>=0),
  status TEXT NOT NULL CHECK(status IN ('PENDING','CLAIMED','FORFEITED','RUN_CLOSED')),
  item_json TEXT NOT NULL CHECK(json_valid(item_json)),
  FOREIGN KEY(run_id,bundle_id) REFERENCES run_equipment_reward_bundles(run_id,bundle_id),
  UNIQUE(bundle_id,sort_index),
  UNIQUE(run_id,item_id)
);
CREATE TRIGGER run_equipment_reward_bundle_immutable_update BEFORE UPDATE ON run_equipment_reward_bundles
BEGIN SELECT RAISE(ABORT,'equipment_reward_bundle_immutable'); END;
CREATE TRIGGER run_equipment_reward_bundle_immutable_delete BEFORE DELETE ON run_equipment_reward_bundles
BEGIN SELECT RAISE(ABORT,'equipment_reward_bundle_immutable'); END;
CREATE TRIGGER run_equipment_reward_item_identity_guard
BEFORE UPDATE OF item_id,run_id,bundle_id,instance_id,sort_index,item_json ON run_equipment_reward_items
BEGIN SELECT RAISE(ABORT,'equipment_reward_item_identity_immutable'); END;
CREATE TABLE equipment_reward_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

-- Immutable original merchant stock; purchase state and physical issuance are separate transactions.
CREATE TABLE run_equipment_merchant_offers (
  offer_id TEXT PRIMARY KEY CHECK(length(offer_id) BETWEEN 1 AND 128),
  run_id TEXT NOT NULL REFERENCES run_equipment_inventory(run_id) ON DELETE CASCADE,
  node_id TEXT NOT NULL CHECK(length(node_id) BETWEEN 1 AND 128),
  source_ref TEXT NOT NULL CHECK(length(source_ref) BETWEEN 1 AND 128),
  source_version TEXT NOT NULL CHECK(length(source_version) BETWEEN 1 AND 128),
  visit_depth INTEGER NOT NULL CHECK(typeof(visit_depth)='integer' AND visit_depth BETWEEN 1 AND 11),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  UNIQUE(run_id,node_id),
  UNIQUE(run_id,offer_id)
);
CREATE TABLE run_equipment_merchant_stock (
  stock_id TEXT PRIMARY KEY CHECK(length(stock_id) BETWEEN 1 AND 128),
  run_id TEXT NOT NULL,
  offer_id TEXT NOT NULL,
  sort_index INTEGER NOT NULL CHECK(typeof(sort_index)='integer' AND sort_index BETWEEN 0 AND 2),
  stock_json TEXT NOT NULL CHECK(json_valid(stock_json)),
  FOREIGN KEY(run_id,offer_id) REFERENCES run_equipment_merchant_offers(run_id,offer_id),
  UNIQUE(offer_id,sort_index)
);
CREATE TRIGGER run_equipment_merchant_offer_immutable_update BEFORE UPDATE ON run_equipment_merchant_offers
BEGIN SELECT RAISE(ABORT,'equipment_merchant_offer_immutable'); END;
CREATE TRIGGER run_equipment_merchant_offer_immutable_delete BEFORE DELETE ON run_equipment_merchant_offers
BEGIN SELECT RAISE(ABORT,'equipment_merchant_offer_immutable'); END;
CREATE TRIGGER run_equipment_merchant_stock_immutable_update BEFORE UPDATE ON run_equipment_merchant_stock
BEGIN SELECT RAISE(ABORT,'equipment_merchant_stock_immutable'); END;
CREATE TRIGGER run_equipment_merchant_stock_immutable_delete BEFORE DELETE ON run_equipment_merchant_stock
BEGIN SELECT RAISE(ABORT,'equipment_merchant_stock_immutable'); END;
CREATE TABLE equipment_merchant_offer_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

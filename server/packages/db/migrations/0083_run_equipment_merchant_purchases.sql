-- Keep merchant buys in the same run-wide equipment command namespace.
CREATE TABLE run_equipment_receipts_next (
  run_id TEXT NOT NULL REFERENCES run_equipment_inventory(run_id) ON DELETE CASCADE,
  command_id TEXT NOT NULL CHECK(length(command_id) BETWEEN 1 AND 128),
  command_kind TEXT NOT NULL CHECK(command_kind IN ('SET_LOCK','CLAIM_ITEMS','DECLINE_REMAINING','PURCHASE_MERCHANT')),
  request_hash TEXT NOT NULL CHECK(length(request_hash)=64),
  response_json TEXT NOT NULL CHECK(json_valid(response_json)),
  PRIMARY KEY(run_id,command_id)
);
INSERT INTO run_equipment_receipts_next SELECT * FROM run_equipment_receipts;
DROP TABLE run_equipment_receipts;
ALTER TABLE run_equipment_receipts_next RENAME TO run_equipment_receipts;
CREATE TRIGGER run_equipment_receipt_immutable_update BEFORE UPDATE ON run_equipment_receipts
BEGIN SELECT RAISE(ABORT,'equipment_receipt_immutable'); END;
CREATE TRIGGER run_equipment_receipt_immutable_delete BEFORE DELETE ON run_equipment_receipts
BEGIN SELECT RAISE(ABORT,'equipment_receipt_immutable'); END;

CREATE TABLE run_equipment_merchant_purchases (
  run_id TEXT NOT NULL,
  offer_id TEXT NOT NULL,
  stock_id TEXT NOT NULL PRIMARY KEY REFERENCES run_equipment_merchant_stock(stock_id),
  command_id TEXT NOT NULL CHECK(length(command_id) BETWEEN 1 AND 128),
  instance_id TEXT NOT NULL UNIQUE CHECK(length(instance_id) BETWEEN 1 AND 128),
  price INTEGER NOT NULL CHECK(typeof(price)='integer' AND price>=1),
  supplies_revision INTEGER NOT NULL CHECK(typeof(supplies_revision)='integer' AND supplies_revision>=2),
  inventory_revision INTEGER NOT NULL CHECK(typeof(inventory_revision)='integer' AND inventory_revision>=2),
  FOREIGN KEY(run_id,offer_id) REFERENCES run_equipment_merchant_offers(run_id,offer_id),
  UNIQUE(run_id,command_id)
);
CREATE INDEX run_equipment_merchant_purchases_offer ON run_equipment_merchant_purchases(run_id,offer_id);
CREATE TRIGGER run_equipment_merchant_purchase_guard BEFORE INSERT ON run_equipment_merchant_purchases
BEGIN
  SELECT CASE WHEN NOT EXISTS (
    SELECT 1 FROM run_equipment_merchant_stock st
    JOIN run_equipment_instances e ON e.run_id=NEW.run_id AND e.source_item_id=st.stock_id
    JOIN run_supplies_state w ON w.run_id=NEW.run_id
    JOIN run_equipment_inventory i ON i.run_id=NEW.run_id
    JOIN run_supplies_ledger l ON l.run_id=NEW.run_id AND l.source_kind='MERCHANT_PURCHASE'
      AND l.source_ref=st.stock_id
    WHERE st.stock_id=NEW.stock_id AND st.run_id=NEW.run_id AND st.offer_id=NEW.offer_id
      AND e.instance_id=NEW.instance_id AND e.location_kind='STORED'
      AND w.revision=NEW.supplies_revision AND i.revision=NEW.inventory_revision
      AND l.revision=w.revision AND l.delta=-NEW.price AND l.balance_after=w.balance
  ) THEN RAISE(ABORT,'equipment_merchant_purchase_incomplete') END;
END;
CREATE TRIGGER run_equipment_merchant_purchase_immutable_update BEFORE UPDATE ON run_equipment_merchant_purchases
BEGIN SELECT RAISE(ABORT,'equipment_merchant_purchase_immutable'); END;
CREATE TRIGGER run_equipment_merchant_purchase_immutable_delete BEFORE DELETE ON run_equipment_merchant_purchases
BEGIN SELECT RAISE(ABORT,'equipment_merchant_purchase_immutable'); END;
CREATE TABLE equipment_merchant_purchase_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

-- Execute as one migration transaction; preserve every existing lot and ledger.
-- EVENT is FREE only. No active policy, currency ID, entitlement or grant is seeded.
CREATE TABLE event_lots_backup AS SELECT * FROM crystal_lots;
CREATE TABLE event_ledger_backup AS SELECT * FROM crystal_ledger;
DROP TABLE crystal_ledger;
DROP TABLE crystal_lots;
CREATE TABLE crystal_lots (
 id TEXT PRIMARY KEY, account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
 currency_id TEXT NOT NULL, source TEXT NOT NULL CHECK(source IN ('FREE','PAID')),
 source_type TEXT NOT NULL CHECK(source_type IN ('ATTENDANCE','PASS_FREE','PASS_PREMIUM','STORE_PURCHASE','LAUNCH_ENTITLEMENT','QUEST','EVENT')),
 source_ref TEXT NOT NULL, order_id TEXT REFERENCES verified_store_orders(id) ON DELETE RESTRICT,
 granted INTEGER NOT NULL CHECK(granted>0 AND granted<=2147483647),
 remaining INTEGER NOT NULL CHECK(remaining>=0 AND remaining<=granted),
 operation_id TEXT NOT NULL REFERENCES crystal_operations(id) ON DELETE RESTRICT, created_at INTEGER NOT NULL,
 CHECK((source_type='STORE_PURCHASE' AND source='PAID' AND order_id IS NOT NULL)
  OR (source_type='PASS_PREMIUM' AND source='FREE' AND order_id IS NOT NULL)
  OR (source_type IN ('ATTENDANCE','PASS_FREE','LAUNCH_ENTITLEMENT','QUEST','EVENT') AND source='FREE' AND order_id IS NULL))
);
CREATE INDEX idx_crystal_lots_balance ON crystal_lots(account_id,currency_id,source,created_at,id);
CREATE INDEX idx_crystal_lots_order ON crystal_lots(order_id);
CREATE TABLE crystal_ledger (
 id TEXT PRIMARY KEY, lot_id TEXT NOT NULL REFERENCES crystal_lots(id) ON DELETE RESTRICT,
 account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
 operation_id TEXT NOT NULL REFERENCES crystal_operations(id) ON DELETE RESTRICT,
 delta INTEGER NOT NULL CHECK(delta<>0), remaining_after INTEGER NOT NULL CHECK(remaining_after>=0),
 reason TEXT NOT NULL, downstream_ref TEXT, created_at INTEGER NOT NULL
);
CREATE INDEX idx_crystal_ledger_operation ON crystal_ledger(operation_id);
INSERT INTO crystal_lots SELECT * FROM event_lots_backup;
INSERT INTO crystal_ledger SELECT * FROM event_ledger_backup;
DROP TABLE event_ledger_backup;
DROP TABLE event_lots_backup;
CREATE TRIGGER crystal_lot_order_guard BEFORE INSERT ON crystal_lots
WHEN NEW.order_id IS NOT NULL AND NOT EXISTS (
 SELECT 1 FROM verified_store_orders o WHERE o.id=NEW.order_id AND o.account_id=NEW.account_id
 AND o.revoked_at IS NULL AND o.product_kind=CASE WHEN NEW.source_type='PASS_PREMIUM' THEN 'PASS' ELSE 'CRYSTAL' END
)
BEGIN SELECT RAISE(ABORT,'crystal order invalid'); END;

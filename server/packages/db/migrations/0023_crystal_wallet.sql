-- No currency, exchange rate or store product is seeded by this migration.
CREATE TABLE crystal_policies (id TEXT PRIMARY KEY, policy_json TEXT NOT NULL CHECK(json_valid(policy_json)));
CREATE TRIGGER crystal_policy_immutable_update BEFORE UPDATE ON crystal_policies
BEGIN SELECT RAISE(ABORT, 'crystal policy immutable'); END;
CREATE TRIGGER crystal_policy_immutable_delete BEFORE DELETE ON crystal_policies
BEGIN SELECT RAISE(ABORT, 'crystal policy immutable'); END;
CREATE TABLE crystal_policy_active (
  singleton_id INTEGER PRIMARY KEY CHECK(singleton_id = 1),
  policy_id TEXT NOT NULL REFERENCES crystal_policies(id) ON DELETE RESTRICT
);

CREATE TABLE verified_store_orders (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  provider TEXT NOT NULL CHECK(provider IN ('GOOGLE_PLAY','APP_STORE')),
  app_id TEXT NOT NULL,
  product_id TEXT NOT NULL,
  transaction_id TEXT NOT NULL,
  environment TEXT NOT NULL CHECK(environment IN ('LOCAL','DEV','PRD')),
  product_kind TEXT NOT NULL CHECK(product_kind IN ('PASS','CRYSTAL')),
  verification_ref TEXT NOT NULL,
  purchased_at INTEGER NOT NULL,
  verified_at INTEGER NOT NULL,
  revoked_at INTEGER,
  UNIQUE(provider, app_id, transaction_id)
);
ALTER TABLE pass_entitlements ADD COLUMN order_id TEXT REFERENCES verified_store_orders(id) ON DELETE RESTRICT;

CREATE TABLE crystal_operations (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  policy_id TEXT REFERENCES crystal_policies(id) ON DELETE RESTRICT,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  operation TEXT NOT NULL,
  idempotency_key TEXT,
  request_hash TEXT,
  response_json TEXT NOT NULL CHECK(json_valid(response_json)),
  created_at INTEGER NOT NULL,
  UNIQUE(account_id, operation, idempotency_key),
  CHECK(operation = 'REFUND' OR policy_id IS NOT NULL)
);

CREATE TABLE crystal_lots (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  currency_id TEXT NOT NULL,
  source TEXT NOT NULL CHECK(source IN ('FREE','PAID')),
  source_type TEXT NOT NULL CHECK(source_type IN ('ATTENDANCE','PASS_FREE','PASS_PREMIUM','STORE_PURCHASE')),
  source_ref TEXT NOT NULL,
  order_id TEXT REFERENCES verified_store_orders(id) ON DELETE RESTRICT,
  granted INTEGER NOT NULL CHECK(granted > 0 AND granted <= 2147483647),
  remaining INTEGER NOT NULL CHECK(remaining >= 0 AND remaining <= granted),
  operation_id TEXT NOT NULL REFERENCES crystal_operations(id) ON DELETE RESTRICT,
  created_at INTEGER NOT NULL,
  CHECK((source_type = 'STORE_PURCHASE' AND source = 'PAID' AND order_id IS NOT NULL)
    OR (source_type = 'PASS_PREMIUM' AND source = 'FREE' AND order_id IS NOT NULL)
    OR (source_type IN ('ATTENDANCE','PASS_FREE') AND source = 'FREE' AND order_id IS NULL))
);
CREATE INDEX idx_crystal_lots_balance ON crystal_lots(account_id, currency_id, source, created_at, id);
CREATE INDEX idx_crystal_lots_order ON crystal_lots(order_id);
CREATE TRIGGER crystal_lot_order_guard BEFORE INSERT ON crystal_lots
WHEN NEW.order_id IS NOT NULL AND NOT EXISTS (
  SELECT 1 FROM verified_store_orders o WHERE o.id = NEW.order_id AND o.account_id = NEW.account_id
    AND o.revoked_at IS NULL AND o.product_kind = CASE WHEN NEW.source_type = 'PASS_PREMIUM' THEN 'PASS' ELSE 'CRYSTAL' END
)
BEGIN SELECT RAISE(ABORT, 'crystal order invalid'); END;

CREATE TABLE crystal_ledger (
  id TEXT PRIMARY KEY,
  lot_id TEXT NOT NULL REFERENCES crystal_lots(id) ON DELETE RESTRICT,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  operation_id TEXT NOT NULL REFERENCES crystal_operations(id) ON DELETE RESTRICT,
  delta INTEGER NOT NULL CHECK(delta <> 0),
  remaining_after INTEGER NOT NULL CHECK(remaining_after >= 0),
  reason TEXT NOT NULL,
  downstream_ref TEXT,
  created_at INTEGER NOT NULL
);
CREATE INDEX idx_crystal_ledger_operation ON crystal_ledger(operation_id);

CREATE TABLE crystal_order_reconciliations (
  order_id TEXT PRIMARY KEY REFERENCES verified_store_orders(id) ON DELETE RESTRICT,
  operation_id TEXT NOT NULL UNIQUE REFERENCES crystal_operations(id) ON DELETE RESTRICT,
  verified_status TEXT NOT NULL CHECK(verified_status IN ('REFUNDED','REVOKED')),
  verified_at INTEGER NOT NULL,
  reclaimed_free INTEGER NOT NULL CHECK(reclaimed_free >= 0),
  reclaimed_paid INTEGER NOT NULL CHECK(reclaimed_paid >= 0),
  spent_free INTEGER NOT NULL CHECK(spent_free >= 0),
  spent_paid INTEGER NOT NULL CHECK(spent_paid >= 0),
  needs_support INTEGER NOT NULL CHECK(needs_support IN (0,1)),
  response_json TEXT NOT NULL CHECK(json_valid(response_json))
);

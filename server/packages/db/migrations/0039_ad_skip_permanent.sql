-- No catalog, SKU, duration or ownership is seeded. Execute as one migration transaction.
PRAGMA defer_foreign_keys=ON;
DROP TRIGGER crystal_lot_order_guard;
CREATE TABLE verified_store_orders_next (
 id TEXT PRIMARY KEY, account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
 provider TEXT NOT NULL CHECK(provider IN ('GOOGLE_PLAY','APP_STORE')), app_id TEXT NOT NULL,
 product_id TEXT NOT NULL, transaction_id TEXT NOT NULL,
 environment TEXT NOT NULL CHECK(environment IN ('LOCAL','DEV','PRD')),
 product_kind TEXT NOT NULL CHECK(product_kind IN ('PASS','CRYSTAL','AD_SKIP_PERMANENT')),
 verification_ref TEXT NOT NULL, purchased_at INTEGER NOT NULL, verified_at INTEGER NOT NULL, revoked_at INTEGER,
 UNIQUE(provider,app_id,transaction_id)
);
INSERT INTO verified_store_orders_next SELECT * FROM verified_store_orders;
DROP TABLE verified_store_orders;
ALTER TABLE verified_store_orders_next RENAME TO verified_store_orders;
CREATE TRIGGER crystal_lot_order_guard BEFORE INSERT ON crystal_lots
WHEN NEW.order_id IS NOT NULL AND NOT EXISTS (
 SELECT 1 FROM verified_store_orders o WHERE o.id=NEW.order_id AND o.account_id=NEW.account_id
 AND o.revoked_at IS NULL AND o.product_kind=CASE WHEN NEW.source_type='PASS_PREMIUM' THEN 'PASS' ELSE 'CRYSTAL' END
)
BEGIN SELECT RAISE(ABORT,'crystal order invalid'); END;
CREATE TABLE ad_skip_catalogs (
 environment TEXT PRIMARY KEY CHECK(environment IN ('LOCAL','DEV','PRD')),
 config_json TEXT NOT NULL CHECK(json_valid(config_json))
);
CREATE TABLE ad_skip_checkout_intents (
 id TEXT PRIMARY KEY, account_id TEXT NOT NULL REFERENCES accounts(id), environment TEXT NOT NULL,
 catalog_json TEXT NOT NULL CHECK(json_valid(catalog_json)), binding TEXT NOT NULL UNIQUE,
 created_at INTEGER NOT NULL, purchase_start_before INTEGER NOT NULL CHECK(purchase_start_before>created_at)
);
CREATE TABLE account_ad_skip_entitlements (
 account_id TEXT NOT NULL REFERENCES accounts(id), environment TEXT NOT NULL,
 status TEXT NOT NULL CHECK(status IN ('ACTIVE','REVOKED')), revision INTEGER NOT NULL CHECK(revision>0),
 order_id TEXT NOT NULL REFERENCES verified_store_orders(id), updated_at INTEGER NOT NULL,
 PRIMARY KEY(account_id,environment)
);
CREATE TABLE ad_skip_revocations (
 provider TEXT NOT NULL, app_id TEXT NOT NULL, transaction_id TEXT NOT NULL,
 account_id TEXT NOT NULL REFERENCES accounts(id), environment TEXT NOT NULL,
 verified_at INTEGER NOT NULL, PRIMARY KEY(provider,app_id,transaction_id)
);
CREATE TABLE ad_skip_request_receipts (
 account_id TEXT NOT NULL REFERENCES accounts(id), environment TEXT NOT NULL, operation TEXT NOT NULL,
 request_key TEXT NOT NULL, request_hash TEXT NOT NULL, response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(account_id,environment,operation,request_key)
);
CREATE TABLE ad_skip_guard(valid INTEGER NOT NULL CHECK(valid=1));
-- An old generic crystal refund writer must not partially revoke a new product.
CREATE TRIGGER ad_skip_order_revoke_guard BEFORE UPDATE OF revoked_at ON verified_store_orders
WHEN OLD.product_kind='AD_SKIP_PERMANENT' AND (
 (OLD.revoked_at IS NOT NULL AND NEW.revoked_at IS NULL)
 OR (NEW.revoked_at IS NOT NULL AND NOT EXISTS(SELECT 1 FROM ad_skip_revocations r
  WHERE r.provider=OLD.provider AND r.app_id=OLD.app_id AND r.transaction_id=OLD.transaction_id
   AND r.account_id=OLD.account_id AND r.environment=OLD.environment)))
BEGIN SELECT RAISE(ABORT,'ad_skip_revocation_required'); END;
PRAGMA defer_foreign_keys=OFF;

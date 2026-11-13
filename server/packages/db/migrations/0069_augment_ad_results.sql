-- Expand the existing single per-slot EXTRA approval; preserve all purchase history.
CREATE TABLE augment_extra_approvals_next (
 id TEXT PRIMARY KEY,run_id TEXT NOT NULL,event_id TEXT NOT NULL,
 slot_index INTEGER NOT NULL CHECK(slot_index BETWEEN 0 AND 2),
 account_id TEXT NOT NULL REFERENCES accounts(id),environment TEXT NOT NULL,
 target_json TEXT NOT NULL CHECK(json_valid(target_json)),
 cause TEXT NOT NULL CHECK(cause IN ('PURCHASE_ENTITLEMENT','AD_SSV')),
 order_id TEXT REFERENCES verified_store_orders(id),entitlement_revision INTEGER,
 approved_at INTEGER NOT NULL,
 ad_intent_id TEXT UNIQUE REFERENCES augment_ad_sessions(id),provider_transaction_id TEXT,
 FOREIGN KEY(run_id,event_id) REFERENCES solo_augment_grants(run_id,event_id),
 UNIQUE(run_id,event_id,slot_index),
 CHECK((cause='PURCHASE_ENTITLEMENT' AND order_id IS NOT NULL AND entitlement_revision IS NOT NULL AND entitlement_revision>0 AND ad_intent_id IS NULL AND provider_transaction_id IS NULL)
 OR(cause='AD_SSV' AND order_id IS NULL AND entitlement_revision IS NULL AND ad_intent_id IS NOT NULL AND provider_transaction_id IS NOT NULL))
);
INSERT INTO augment_extra_approvals_next SELECT *,NULL,NULL FROM augment_extra_approvals;
DROP TABLE augment_extra_approvals;
ALTER TABLE augment_extra_approvals_next RENAME TO augment_extra_approvals;
CREATE TRIGGER augment_extra_no_update BEFORE UPDATE ON augment_extra_approvals BEGIN SELECT RAISE(ABORT,'immutable_augment_extra'); END;
CREATE TRIGGER augment_extra_no_delete BEFORE DELETE ON augment_extra_approvals BEGIN SELECT RAISE(ABORT,'immutable_augment_extra'); END;
CREATE TABLE augment_ad_results (
 intent_id TEXT PRIMARY KEY REFERENCES augment_ad_sessions(id),
 provider_transaction_id TEXT NOT NULL,
 result_json TEXT NOT NULL CHECK(json_valid(result_json))
);
CREATE TRIGGER augment_ad_result_no_update BEFORE UPDATE ON augment_ad_results BEGIN SELECT RAISE(ABORT,'immutable_augment_ad_result'); END;
CREATE TRIGGER augment_ad_result_no_delete BEFORE DELETE ON augment_ad_results BEGIN SELECT RAISE(ABORT,'immutable_augment_ad_result'); END;

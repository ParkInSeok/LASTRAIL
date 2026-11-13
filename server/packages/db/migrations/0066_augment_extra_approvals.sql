-- First supported extra approval source: verified permanent ad-skip entitlement.
-- No ownership or active policy is seeded.
CREATE TABLE augment_extra_approvals (
 id TEXT PRIMARY KEY,run_id TEXT NOT NULL,event_id TEXT NOT NULL,
 slot_index INTEGER NOT NULL CHECK(slot_index BETWEEN 0 AND 2),
 account_id TEXT NOT NULL REFERENCES accounts(id),environment TEXT NOT NULL,
 target_json TEXT NOT NULL CHECK(json_valid(target_json)),
 cause TEXT NOT NULL CHECK(cause='PURCHASE_ENTITLEMENT'),
 order_id TEXT NOT NULL REFERENCES verified_store_orders(id),entitlement_revision INTEGER NOT NULL CHECK(entitlement_revision>0),
 approved_at INTEGER NOT NULL,
 FOREIGN KEY(run_id,event_id) REFERENCES solo_augment_grants(run_id,event_id),
 UNIQUE(run_id,event_id,slot_index)
);
CREATE TRIGGER augment_extra_no_update BEFORE UPDATE ON augment_extra_approvals BEGIN SELECT RAISE(ABORT,'immutable_augment_extra'); END;
CREATE TRIGGER augment_extra_no_delete BEFORE DELETE ON augment_extra_approvals BEGIN SELECT RAISE(ABORT,'immutable_augment_extra'); END;

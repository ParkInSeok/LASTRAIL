-- Preserve the single cross-consumer provider transaction namespace.
-- No ad policy, intent, approval or entitlement is issued by this migration.
CREATE TABLE ad_provider_receipts_next (
 provider TEXT NOT NULL, transaction_id TEXT NOT NULL,
 consumer TEXT NOT NULL CHECK(consumer IN ('LEGACY','OFFLINE','COMBAT','AUGMENT')),
 intent_id TEXT NOT NULL, account_id TEXT NOT NULL REFERENCES accounts(id),
 PRIMARY KEY(provider,transaction_id), UNIQUE(consumer,intent_id)
);
INSERT INTO ad_provider_receipts_next SELECT provider,transaction_id,consumer,intent_id,account_id FROM ad_provider_receipts;
DROP TABLE ad_provider_receipts;
ALTER TABLE ad_provider_receipts_next RENAME TO ad_provider_receipts;

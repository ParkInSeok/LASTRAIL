-- One transaction required. No active game policy or ownership is seeded.
PRAGMA defer_foreign_keys=ON;
CREATE TABLE purchase_reward_policies(environment TEXT NOT NULL,kind TEXT NOT NULL CHECK(kind IN ('OFFLINE','COMBAT')),
 config_json TEXT NOT NULL CHECK(json_valid(config_json)),PRIMARY KEY(environment,kind));
CREATE TABLE reward_opportunity_approvals(
 consumer TEXT NOT NULL CHECK(consumer IN ('OFFLINE','COMBAT')),intent_id TEXT NOT NULL,
 account_id TEXT NOT NULL REFERENCES accounts(id),environment TEXT NOT NULL,
 cause TEXT NOT NULL CHECK(cause IN ('AD_SSV','PURCHASE_ENTITLEMENT')),approved_at INTEGER NOT NULL,
 order_id TEXT REFERENCES verified_store_orders(id),entitlement_revision INTEGER,provider_transaction_id TEXT,
 PRIMARY KEY(consumer,intent_id),
 CHECK((cause='AD_SSV' AND provider_transaction_id IS NOT NULL AND order_id IS NULL AND entitlement_revision IS NULL)
 OR(cause='PURCHASE_ENTITLEMENT' AND provider_transaction_id IS NULL AND order_id IS NOT NULL AND entitlement_revision>0))
);
INSERT INTO reward_opportunity_approvals SELECT 'OFFLINE',i.id,i.account_id,i.environment,'AD_SSV',i.verified_at,NULL,NULL,i.provider_transaction_id
 FROM offline_ad_intents i WHERE i.status<>'AD_PENDING';
INSERT INTO reward_opportunity_approvals SELECT 'COMBAT',i.id,i.account_id,i.environment,'AD_SSV',i.created_at,NULL,NULL,i.provider_transaction_id
 FROM combat_ad_intents i WHERE i.status<>'AD_PENDING';
INSERT INTO ad_skip_guard SELECT CASE WHEN NOT EXISTS(
 SELECT 1 FROM reward_opportunity_approvals a WHERE NOT EXISTS(SELECT 1 FROM ad_provider_receipts p
 WHERE p.consumer=a.consumer AND p.intent_id=a.intent_id AND p.account_id=a.account_id AND p.provider='ADMOB' AND p.transaction_id=a.provider_transaction_id)
) THEN 1 ELSE 0 END;
DELETE FROM ad_skip_guard;
CREATE TRIGGER reward_approval_immutable_update BEFORE UPDATE ON reward_opportunity_approvals BEGIN SELECT RAISE(ABORT,'approval immutable'); END;
CREATE TRIGGER reward_approval_immutable_delete BEFORE DELETE ON reward_opportunity_approvals BEGIN SELECT RAISE(ABORT,'approval immutable'); END;

CREATE TABLE offline_ad_intents_next (
 id TEXT PRIMARY KEY,account_id TEXT NOT NULL REFERENCES accounts(id),environment TEXT NOT NULL CHECK(environment IN ('LOCAL','DEV','PRD')),
 create_key TEXT NOT NULL,baseline_ms INTEGER NOT NULL,baseline_revision INTEGER NOT NULL,created_at INTEGER NOT NULL,
 expires_at INTEGER,callback_deadline INTEGER,policy_json TEXT NOT NULL,base_json TEXT NOT NULL,metadata_json TEXT NOT NULL,
 story_progress_id TEXT NOT NULL,narrative_version TEXT NOT NULL,user_binding TEXT UNIQUE,challenge TEXT UNIQUE,
 status TEXT NOT NULL CHECK(status IN ('AD_PENDING','AD_VERIFIED','CLAIMED')),provider_transaction_id TEXT UNIQUE,
 verified_at INTEGER,spin_key TEXT,response_json TEXT,
 origin TEXT NOT NULL CHECK(origin IN ('AD_REQUEST','PURCHASE_ONLY')),write_revision INTEGER NOT NULL DEFAULT 0,
 UNIQUE(account_id,create_key),UNIQUE(account_id,baseline_ms,baseline_revision),UNIQUE(account_id,spin_key),
 CHECK((origin='AD_REQUEST' AND expires_at>created_at AND callback_deadline>=expires_at AND user_binding IS NOT NULL AND challenge IS NOT NULL)
 OR(origin='PURCHASE_ONLY' AND expires_at IS NULL AND callback_deadline IS NULL AND user_binding IS NULL AND challenge IS NULL)),
 CHECK((status='AD_PENDING' AND verified_at IS NULL AND response_json IS NULL)
 OR(status='AD_VERIFIED' AND verified_at IS NOT NULL AND response_json IS NULL)
 OR(status='CLAIMED' AND verified_at IS NOT NULL AND response_json IS NOT NULL AND spin_key IS NOT NULL))
);
INSERT INTO offline_ad_intents_next SELECT *, 'AD_REQUEST',1 FROM offline_ad_intents;
DROP TABLE offline_ad_intents;
ALTER TABLE offline_ad_intents_next RENAME TO offline_ad_intents;
CREATE INDEX idx_offline_ad_account_created ON offline_ad_intents(account_id,created_at);
CREATE TABLE combat_ad_intents_next (
 id TEXT PRIMARY KEY,account_id TEXT NOT NULL REFERENCES accounts(id),run_id TEXT NOT NULL REFERENCES runs(id),
 preview_id TEXT NOT NULL UNIQUE REFERENCES run_reward_previews(id),environment TEXT NOT NULL,created_at INTEGER NOT NULL,
 expires_at INTEGER,callback_deadline INTEGER,policy_json TEXT NOT NULL CHECK(json_valid(policy_json)),base_json TEXT NOT NULL,balance_version INTEGER NOT NULL,
 challenge TEXT UNIQUE,user_binding TEXT UNIQUE,status TEXT NOT NULL CHECK(status IN ('AD_PENDING','AD_VERIFIED','RESULT_FIXED','CLAIMED')),
 provider_transaction_id TEXT UNIQUE,multiplier INTEGER CHECK(multiplier IN (2,3,4)),result_json TEXT,claim_id TEXT REFERENCES reward_transactions(id),
 origin TEXT NOT NULL CHECK(origin IN ('AD_REQUEST','PURCHASE_ONLY')),write_revision INTEGER NOT NULL DEFAULT 0,
 CHECK((origin='AD_REQUEST' AND expires_at>created_at AND callback_deadline>=expires_at AND user_binding IS NOT NULL AND challenge IS NOT NULL)
 OR(origin='PURCHASE_ONLY' AND expires_at IS NULL AND callback_deadline IS NULL AND user_binding IS NULL AND challenge IS NULL)),
 CHECK((status IN ('AD_PENDING','AD_VERIFIED') AND multiplier IS NULL) OR(status IN ('RESULT_FIXED','CLAIMED') AND multiplier IS NOT NULL)),
 CHECK((status='CLAIMED' AND result_json IS NOT NULL AND claim_id IS NOT NULL) OR(status<>'CLAIMED' AND result_json IS NULL AND claim_id IS NULL))
);
INSERT INTO combat_ad_intents_next SELECT *, 'AD_REQUEST',1 FROM combat_ad_intents;
DROP TABLE combat_ad_intents;
ALTER TABLE combat_ad_intents_next RENAME TO combat_ad_intents;
CREATE INDEX combat_ad_account_day ON combat_ad_intents(account_id,environment,created_at);
CREATE TRIGGER offline_intent_writer_insert BEFORE INSERT ON offline_ad_intents
WHEN NEW.write_revision<>1 BEGIN SELECT RAISE(ABORT,'reward_writer_upgrade_required'); END;
CREATE TRIGGER offline_intent_writer_update BEFORE UPDATE ON offline_ad_intents
WHEN NEW.write_revision<>OLD.write_revision+1 OR NEW.origin<>OLD.origin
BEGIN SELECT RAISE(ABORT,'reward_writer_upgrade_required'); END;
CREATE TRIGGER combat_intent_writer_insert BEFORE INSERT ON combat_ad_intents
WHEN NEW.write_revision<>1 BEGIN SELECT RAISE(ABORT,'reward_writer_upgrade_required'); END;
CREATE TRIGGER combat_intent_writer_update BEFORE UPDATE ON combat_ad_intents
WHEN NEW.write_revision<>OLD.write_revision+1 OR NEW.origin<>OLD.origin
BEGIN SELECT RAISE(ABORT,'reward_writer_upgrade_required'); END;
CREATE TRIGGER offline_intent_approval_update BEFORE UPDATE ON offline_ad_intents
WHEN NEW.status<>'AD_PENDING' AND NOT EXISTS(SELECT 1 FROM reward_opportunity_approvals a
 WHERE a.consumer='OFFLINE' AND a.intent_id=NEW.id AND a.account_id=NEW.account_id AND a.environment=NEW.environment)
BEGIN SELECT RAISE(ABORT,'reward_approval_required'); END;
CREATE TRIGGER combat_intent_approval_update BEFORE UPDATE ON combat_ad_intents
WHEN NEW.status<>'AD_PENDING' AND NOT EXISTS(SELECT 1 FROM reward_opportunity_approvals a
 WHERE a.consumer='COMBAT' AND a.intent_id=NEW.id AND a.account_id=NEW.account_id AND a.environment=NEW.environment)
BEGIN SELECT RAISE(ABORT,'reward_approval_required'); END;
PRAGMA defer_foreign_keys=OFF;

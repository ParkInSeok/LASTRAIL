-- No active policy or operating values are seeded.
CREATE TABLE ad_provider_receipts (
  provider TEXT NOT NULL, transaction_id TEXT NOT NULL,
  consumer TEXT NOT NULL CHECK(consumer IN ('LEGACY','OFFLINE','COMBAT')),
  intent_id TEXT NOT NULL, account_id TEXT NOT NULL REFERENCES accounts(id),
  PRIMARY KEY(provider,transaction_id), UNIQUE(consumer,intent_id)
);
-- Deliberately fail on historical cross-consumer reuse instead of hiding it.
INSERT INTO ad_provider_receipts SELECT provider,provider_transaction_id,'LEGACY',id,account_id
  FROM ad_reward_transactions WHERE status IN ('VERIFIED','GRANTED');
INSERT INTO ad_provider_receipts SELECT 'ADMOB',provider_transaction_id,'OFFLINE',id,account_id
  FROM offline_ad_intents WHERE provider_transaction_id IS NOT NULL;
CREATE TABLE combat_ad_policies (
  version TEXT PRIMARY KEY, environment TEXT NOT NULL CHECK(environment IN ('LOCAL','DEV','PRD')),
  status TEXT NOT NULL CHECK(status IN ('ACTIVE','RETIRED')), config_json TEXT NOT NULL CHECK(json_valid(config_json))
);
CREATE UNIQUE INDEX combat_ad_active_policy ON combat_ad_policies(environment) WHERE status='ACTIVE';
CREATE TABLE combat_ad_intents (
  id TEXT PRIMARY KEY, account_id TEXT NOT NULL REFERENCES accounts(id),
  run_id TEXT NOT NULL REFERENCES runs(id), preview_id TEXT NOT NULL UNIQUE REFERENCES run_reward_previews(id),
  environment TEXT NOT NULL, created_at INTEGER NOT NULL, expires_at INTEGER NOT NULL,
  callback_deadline INTEGER NOT NULL, policy_json TEXT NOT NULL CHECK(json_valid(policy_json)),
  base_json TEXT NOT NULL, balance_version INTEGER NOT NULL,
  challenge TEXT NOT NULL UNIQUE, user_binding TEXT NOT NULL UNIQUE,
  status TEXT NOT NULL CHECK(status IN ('AD_PENDING','AD_VERIFIED','RESULT_FIXED','CLAIMED')),
  provider_transaction_id TEXT UNIQUE, multiplier INTEGER CHECK(multiplier IN (2,3,4)),
  result_json TEXT, claim_id TEXT REFERENCES reward_transactions(id),
  CHECK(expires_at>created_at AND callback_deadline>=expires_at),
  CHECK((status='AD_PENDING' AND provider_transaction_id IS NULL AND multiplier IS NULL)
    OR (status='AD_VERIFIED' AND provider_transaction_id IS NOT NULL AND multiplier IS NULL)
    OR (status IN ('RESULT_FIXED','CLAIMED') AND provider_transaction_id IS NOT NULL AND multiplier IS NOT NULL)),
  CHECK((status='CLAIMED' AND result_json IS NOT NULL AND claim_id IS NOT NULL)
    OR (status<>'CLAIMED' AND result_json IS NULL AND claim_id IS NULL))
);
CREATE INDEX combat_ad_account_day ON combat_ad_intents(account_id,environment,created_at);
CREATE TABLE combat_ad_create_receipts (
  account_id TEXT NOT NULL, idempotency_key TEXT NOT NULL, intent_id TEXT NOT NULL REFERENCES combat_ad_intents(id),
  PRIMARY KEY(account_id,idempotency_key)
);
CREATE TABLE combat_ad_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TABLE combat_ad_spin_receipts (
  account_id TEXT NOT NULL, idempotency_key TEXT NOT NULL, intent_id TEXT NOT NULL REFERENCES combat_ad_intents(id),
  PRIMARY KEY(account_id,idempotency_key)
);

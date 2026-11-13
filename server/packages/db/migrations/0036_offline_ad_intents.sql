-- No operating policy or tester entitlement is seeded here.
CREATE TABLE offline_ad_intents (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id),
  environment TEXT NOT NULL CHECK(environment IN ('LOCAL','DEV','PRD')),
  create_key TEXT NOT NULL,
  baseline_ms INTEGER NOT NULL,
  baseline_revision INTEGER NOT NULL,
  created_at INTEGER NOT NULL,
  expires_at INTEGER NOT NULL,
  callback_deadline INTEGER NOT NULL,
  policy_json TEXT NOT NULL,
  base_json TEXT NOT NULL,
  metadata_json TEXT NOT NULL,
  story_progress_id TEXT NOT NULL,
  narrative_version TEXT NOT NULL,
  user_binding TEXT NOT NULL UNIQUE,
  challenge TEXT NOT NULL UNIQUE,
  status TEXT NOT NULL CHECK(status IN ('AD_PENDING','AD_VERIFIED','CLAIMED')),
  provider_transaction_id TEXT UNIQUE,
  verified_at INTEGER,
  spin_key TEXT,
  response_json TEXT,
  UNIQUE(account_id,create_key),
  UNIQUE(account_id,baseline_ms,baseline_revision),
  UNIQUE(account_id,spin_key),
  CHECK(expires_at > created_at AND callback_deadline >= expires_at),
  CHECK((status='AD_PENDING' AND provider_transaction_id IS NULL AND verified_at IS NULL AND response_json IS NULL)
     OR (status='AD_VERIFIED' AND provider_transaction_id IS NOT NULL AND verified_at IS NOT NULL AND response_json IS NULL)
     OR (status='CLAIMED' AND provider_transaction_id IS NOT NULL AND verified_at IS NOT NULL AND response_json IS NOT NULL AND spin_key IS NOT NULL))
);
CREATE INDEX idx_offline_ad_account_created ON offline_ad_intents(account_id,created_at);
CREATE TABLE offline_ad_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TABLE offline_ad_create_receipts (
  account_id TEXT NOT NULL REFERENCES accounts(id),
  idempotency_key TEXT NOT NULL,
  intent_id TEXT NOT NULL REFERENCES offline_ad_intents(id),
  PRIMARY KEY(account_id,idempotency_key)
);
CREATE TABLE offline_ad_policies (
  version TEXT PRIMARY KEY,
  environment TEXT NOT NULL CHECK(environment IN ('LOCAL','DEV','PRD')),
  status TEXT NOT NULL CHECK(status IN ('ACTIVE','RETIRED')),
  config_json TEXT NOT NULL CHECK(json_valid(config_json)),
  approved_at INTEGER NOT NULL,
  approved_by TEXT NOT NULL
);
CREATE UNIQUE INDEX idx_offline_ad_active_policy ON offline_ad_policies(environment) WHERE status='ACTIVE';

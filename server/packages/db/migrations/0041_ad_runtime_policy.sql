-- No operational policy seed. Absence means UNKNOWN, never implicit ON.
CREATE TABLE ad_runtime_policies (
 environment TEXT PRIMARY KEY,
 revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
 ads_enabled INTEGER NOT NULL CHECK(ads_enabled IN (0,1))
);
CREATE TRIGGER ad_runtime_policy_revision BEFORE UPDATE ON ad_runtime_policies
WHEN NEW.environment <> OLD.environment OR NEW.revision <> OLD.revision + 1
BEGIN SELECT RAISE(ABORT,'ad_runtime_policy_revision_conflict'); END;
-- Fail closed at the same write boundary as intent creation, including older writers.
CREATE TRIGGER offline_ad_runtime_gate BEFORE INSERT ON offline_ad_intents
WHEN NEW.origin='AD_REQUEST' AND NOT EXISTS(
 SELECT 1 FROM ad_runtime_policies WHERE environment=NEW.environment AND ads_enabled=1
 AND typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991)
BEGIN SELECT RAISE(ABORT,'ad_runtime_policy_blocked'); END;
CREATE TABLE ad_start_approvals (
 consumer TEXT NOT NULL CHECK(consumer IN ('OFFLINE','COMBAT')),
 intent_id TEXT NOT NULL,account_id TEXT NOT NULL REFERENCES accounts(id),environment TEXT NOT NULL,
 request_key TEXT NOT NULL, runtime_policy_revision INTEGER NOT NULL,
 approved_at INTEGER NOT NULL,expires_at INTEGER NOT NULL,
 PRIMARY KEY(consumer,intent_id), UNIQUE(account_id,environment,request_key)
);
CREATE TRIGGER ad_start_runtime_gate BEFORE INSERT ON ad_start_approvals
WHEN NOT EXISTS(SELECT 1 FROM ad_runtime_policies WHERE environment=NEW.environment
 AND revision=NEW.runtime_policy_revision AND ads_enabled=1
 AND typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991)
BEGIN SELECT RAISE(ABORT,'ad_runtime_policy_blocked'); END;
CREATE TRIGGER ad_start_intent_gate BEFORE INSERT ON ad_start_approvals
WHEN NOT EXISTS(SELECT 1 FROM accounts WHERE id=NEW.account_id AND status='ACTIVE')
 OR NOT (
 (NEW.consumer='OFFLINE' AND EXISTS(SELECT 1 FROM offline_ad_intents i
 JOIN accounts a ON a.id=i.account_id LEFT JOIN account_offline_reward_state s ON s.account_id=a.id
 WHERE i.id=NEW.intent_id AND i.account_id=NEW.account_id AND i.environment=NEW.environment
 AND i.origin='AD_REQUEST' AND i.status='AD_PENDING' AND i.expires_at=NEW.expires_at
 AND i.baseline_ms=COALESCE(s.last_offline_reward_claim_at,a.created_at) AND i.baseline_revision=COALESCE(s.revision,0)
 AND NEW.approved_at BETWEEN i.created_at AND i.expires_at))
 OR (NEW.consumer='COMBAT' AND EXISTS(SELECT 1 FROM combat_ad_intents i
 JOIN run_reward_previews p ON p.id=i.preview_id AND p.account_id=i.account_id
 JOIN runs r ON r.id=i.run_id AND r.account_id=i.account_id
 JOIN run_final_results f ON f.run_id=r.id
 WHERE i.id=NEW.intent_id AND i.account_id=NEW.account_id AND i.environment=NEW.environment
 AND i.origin='AD_REQUEST' AND i.status='AD_PENDING' AND i.expires_at=NEW.expires_at
 AND p.status='ACTIVE' AND r.status='COMPLETED' AND f.outcome='VICTORY'
 AND p.reward_plan_json=i.base_json AND p.balance_config_version=i.balance_version
 AND NEW.approved_at BETWEEN i.created_at AND i.expires_at AND p.expires_at>NEW.approved_at))
 ) OR NEW.expires_at<CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)
BEGIN SELECT RAISE(ABORT,'ad_start_not_available'); END;
CREATE TRIGGER ad_start_no_update BEFORE UPDATE ON ad_start_approvals
BEGIN SELECT RAISE(ABORT,'ad_start_immutable'); END;
CREATE TRIGGER ad_start_no_delete BEFORE DELETE ON ad_start_approvals
BEGIN SELECT RAISE(ABORT,'ad_start_immutable'); END;
-- Existing confirmed approvals are preserved. A new SSV approval needs a start grant.
CREATE TRIGGER ad_ssv_requires_start BEFORE INSERT ON reward_opportunity_approvals
WHEN NEW.cause='AD_SSV' AND NOT EXISTS(SELECT 1 FROM ad_start_approvals s
 WHERE s.consumer=NEW.consumer AND s.intent_id=NEW.intent_id
 AND s.account_id=NEW.account_id AND s.environment=NEW.environment)
BEGIN SELECT RAISE(ABORT,'ad_start_not_approved'); END;
CREATE TRIGGER combat_ad_runtime_gate BEFORE INSERT ON combat_ad_intents
WHEN NEW.origin='AD_REQUEST' AND NOT EXISTS(
 SELECT 1 FROM ad_runtime_policies WHERE environment=NEW.environment AND ads_enabled=1
 AND typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991)
BEGIN SELECT RAISE(ABORT,'ad_runtime_policy_blocked'); END;

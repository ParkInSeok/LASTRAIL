-- Preserve old opportunities with null pins; never backfill from current source policies.
CREATE TABLE reward_disclosure_guard(valid INTEGER NOT NULL);
CREATE TRIGGER reward_disclosure_guard_reject BEFORE INSERT ON reward_disclosure_guard WHEN NEW.valid<>1
BEGIN SELECT RAISE(ABORT,'reward_disclosure_changed'); END;
ALTER TABLE offline_ad_intents ADD COLUMN probability_disclosure_id TEXT REFERENCES probability_disclosures(disclosure_id);
ALTER TABLE offline_ad_intents ADD COLUMN probability_source_digest TEXT;
CREATE TRIGGER offline_disclosure_required BEFORE INSERT ON offline_ad_intents
WHEN NEW.probability_disclosure_id IS NULL OR NEW.probability_source_digest IS NULL
BEGIN SELECT RAISE(ABORT,'reward_disclosure_required'); END;
CREATE TRIGGER offline_disclosure_immutable BEFORE UPDATE ON offline_ad_intents
WHEN NEW.probability_disclosure_id IS NOT OLD.probability_disclosure_id
 OR NEW.probability_source_digest IS NOT OLD.probability_source_digest OR NEW.policy_json IS NOT OLD.policy_json
BEGIN SELECT RAISE(ABORT,'reward_disclosure_pin_immutable'); END;
ALTER TABLE combat_ad_intents ADD COLUMN probability_disclosure_id TEXT REFERENCES probability_disclosures(disclosure_id);
ALTER TABLE combat_ad_intents ADD COLUMN probability_source_digest TEXT;
CREATE TRIGGER combat_disclosure_required BEFORE INSERT ON combat_ad_intents
WHEN NEW.probability_disclosure_id IS NULL OR NEW.probability_source_digest IS NULL
BEGIN SELECT RAISE(ABORT,'reward_disclosure_required'); END;
CREATE TRIGGER combat_disclosure_immutable BEFORE UPDATE ON combat_ad_intents
WHEN NEW.probability_disclosure_id IS NOT OLD.probability_disclosure_id
 OR NEW.probability_source_digest IS NOT OLD.probability_source_digest OR NEW.policy_json IS NOT OLD.policy_json
BEGIN SELECT RAISE(ABORT,'reward_disclosure_pin_immutable'); END;

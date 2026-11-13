-- Separate durable random decision from retryable payout. No policy seed/backfill.
-- Historical CLAIMED rows retain their original response; VERIFIED rows fix on spin.
CREATE TABLE offline_ad_fixed_results (
  intent_id TEXT PRIMARY KEY REFERENCES offline_ad_intents(id),
  multiplier INTEGER NOT NULL CHECK(multiplier IN (2,3,4)),
  policy_json TEXT NOT NULL CHECK(json_valid(policy_json)),
  base_json TEXT NOT NULL CHECK(json_valid(base_json))
);

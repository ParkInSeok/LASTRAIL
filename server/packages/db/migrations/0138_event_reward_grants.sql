-- Only an execution receipt. Achievement and cosmetic ownership remain source-owned.
CREATE TABLE event_reward_write_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TABLE event_reward_grants (
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 account_id TEXT NOT NULL REFERENCES accounts(id),
 round_id TEXT NOT NULL, reward_row_id TEXT NOT NULL,
 decision_json TEXT NOT NULL CHECK(json_valid(decision_json)),
 decision_hash TEXT NOT NULL, claim_id TEXT NOT NULL UNIQUE REFERENCES reward_transactions(id),
 response_json TEXT NOT NULL CHECK(json_valid(response_json)), response_hash TEXT NOT NULL,
 PRIMARY KEY(environment,account_id,round_id,reward_row_id)
);
CREATE TRIGGER event_reward_grants_immutable_update BEFORE UPDATE ON event_reward_grants
BEGIN SELECT RAISE(ABORT,'event reward receipt immutable'); END;
CREATE TRIGGER event_reward_grants_immutable_delete BEFORE DELETE ON event_reward_grants
BEGIN SELECT RAISE(ABORT,'event reward receipt immutable'); END;

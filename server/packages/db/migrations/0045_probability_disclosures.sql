-- Immutable approved multiplier disclosures; no changes to source policies or runtime activation.
CREATE TABLE probability_disclosures (
  disclosure_id TEXT PRIMARY KEY,
  environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
  payload_json TEXT NOT NULL CHECK(json_valid(payload_json)),
  payload_digest TEXT NOT NULL
);
CREATE TRIGGER probability_disclosure_no_update BEFORE UPDATE ON probability_disclosures
BEGIN SELECT RAISE(ABORT,'probability_disclosure_immutable'); END;
CREATE TRIGGER probability_disclosure_no_delete BEFORE DELETE ON probability_disclosures
BEGIN SELECT RAISE(ABORT,'probability_disclosure_immutable'); END;
CREATE TABLE probability_disclosure_receipts (
  principal_id TEXT NOT NULL, environment TEXT NOT NULL, request_key TEXT NOT NULL,
  payload_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),
  PRIMARY KEY(principal_id,environment,request_key)
);
CREATE TABLE probability_disclosure_guard(valid INTEGER NOT NULL CHECK(valid=1));

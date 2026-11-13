-- Separate optional recovery success from the base combat-node credit.
CREATE TABLE run_supplies_recovery_credits (
  run_id TEXT NOT NULL REFERENCES run_supplies_state(run_id) ON DELETE CASCADE,
  node_id TEXT NOT NULL CHECK(length(node_id) BETWEEN 1 AND 128),
  source_ref TEXT NOT NULL CHECK(length(source_ref) BETWEEN 1 AND 128),
  source_version TEXT NOT NULL CHECK(length(source_version) BETWEEN 1 AND 128),
  tier TEXT NOT NULL CHECK(tier IN ('STANDARD','HIGH_RISK')),
  amount INTEGER NOT NULL CHECK(amount=CASE tier WHEN 'STANDARD' THEN 25 ELSE 40 END),
  revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision>=2),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  receipt_json TEXT NOT NULL CHECK(json_valid(receipt_json)),
  PRIMARY KEY(run_id,node_id),
  UNIQUE(run_id,source_ref)
);
CREATE TRIGGER run_supplies_recovery_credit_guard BEFORE INSERT ON run_supplies_recovery_credits
BEGIN
  SELECT CASE WHEN NOT EXISTS (
    SELECT 1 FROM run_supplies_state w JOIN run_supplies_ledger l ON l.run_id=w.run_id
    WHERE w.run_id=NEW.run_id AND w.revision=NEW.revision
      AND l.source_kind='RECOVERY_NODE' AND l.source_ref=NEW.node_id
      AND l.revision=NEW.revision AND l.delta=NEW.amount AND l.balance_after=w.balance
  ) THEN RAISE(ABORT,'run_supplies_recovery_credit_incomplete') END;
END;
CREATE TRIGGER run_supplies_recovery_credit_immutable_update BEFORE UPDATE ON run_supplies_recovery_credits
BEGIN SELECT RAISE(ABORT,'run_supplies_recovery_credit_immutable'); END;
CREATE TRIGGER run_supplies_recovery_credit_immutable_delete BEFORE DELETE ON run_supplies_recovery_credits
BEGIN SELECT RAISE(ABORT,'run_supplies_recovery_credit_immutable'); END;
CREATE TABLE run_supplies_recovery_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

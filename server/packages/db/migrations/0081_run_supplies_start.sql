-- New 12-node solo START supply state; historical runs are not backfilled.
CREATE TABLE run_supplies_state (
  run_id TEXT PRIMARY KEY REFERENCES runs(id) ON DELETE CASCADE,
  account_id TEXT NOT NULL REFERENCES accounts(id),
  policy_version TEXT NOT NULL CHECK(policy_version='run.supplies.v02'),
  do04_rank INTEGER NOT NULL CHECK(typeof(do04_rank)='integer' AND do04_rank BETWEEN 0 AND 4),
  initial_balance INTEGER NOT NULL CHECK(initial_balance=30+5*do04_rank),
  balance INTEGER NOT NULL CHECK(typeof(balance)='integer' AND balance>=0),
  revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991)
);
CREATE TRIGGER run_supplies_start_guard BEFORE INSERT ON run_supplies_state
BEGIN
  SELECT CASE WHEN NEW.revision<>1 OR NEW.balance<>NEW.initial_balance OR NOT EXISTS (
    SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id
    WHERE r.id=NEW.run_id AND r.account_id=NEW.account_id AND r.status='ACTIVE'
      AND json_extract(s.snapshot_json,'$.encounterType')='ORDINARY_12_NODE'
      AND json_extract(s.snapshot_json,'$.runSupplies.version')=NEW.policy_version
      AND json_extract(s.snapshot_json,'$.runSupplies.do04Rank')=NEW.do04_rank
      AND json_extract(s.snapshot_json,'$.runSupplies.initialBalance')=NEW.initial_balance
  ) THEN RAISE(ABORT,'run_supplies_start_mismatch') END;
END;
CREATE TRIGGER run_supplies_pin_immutable BEFORE UPDATE OF account_id,policy_version,do04_rank,initial_balance ON run_supplies_state
BEGIN SELECT RAISE(ABORT,'run_supplies_pin_immutable'); END;
CREATE TABLE run_supplies_ledger (
  run_id TEXT NOT NULL REFERENCES run_supplies_state(run_id) ON DELETE CASCADE,
  source_kind TEXT NOT NULL CHECK(length(source_kind) BETWEEN 1 AND 64),
  source_ref TEXT NOT NULL CHECK(length(source_ref) BETWEEN 1 AND 128),
  revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision>=1),
  delta INTEGER NOT NULL CHECK(typeof(delta)='integer'),
  balance_after INTEGER NOT NULL CHECK(typeof(balance_after)='integer' AND balance_after>=0),
  PRIMARY KEY(run_id,source_kind,source_ref),
  UNIQUE(run_id,revision)
);
CREATE TRIGGER run_supplies_ledger_immutable_update BEFORE UPDATE ON run_supplies_ledger
BEGIN SELECT RAISE(ABORT,'run_supplies_ledger_immutable'); END;
CREATE TRIGGER run_supplies_ledger_immutable_delete BEFORE DELETE ON run_supplies_ledger
BEGIN SELECT RAISE(ABORT,'run_supplies_ledger_immutable'); END;

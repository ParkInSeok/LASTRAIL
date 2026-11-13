-- One original combat-node supply credit per run/node, bound to the shared run ledger.
CREATE TABLE run_supplies_node_credits (
  run_id TEXT NOT NULL REFERENCES run_supplies_state(run_id) ON DELETE CASCADE,
  node_id TEXT NOT NULL CHECK(length(node_id) BETWEEN 1 AND 128),
  source_ref TEXT NOT NULL CHECK(length(source_ref) BETWEEN 1 AND 128),
  source_version TEXT NOT NULL CHECK(length(source_version) BETWEEN 1 AND 128),
  payload_hash TEXT NOT NULL CHECK(length(payload_hash)=64),
  receipt_json TEXT NOT NULL CHECK(json_valid(receipt_json)),
  PRIMARY KEY(run_id,node_id)
);
CREATE TRIGGER run_supplies_node_credit_immutable_update BEFORE UPDATE ON run_supplies_node_credits
BEGIN SELECT RAISE(ABORT,'run_supplies_node_credit_immutable'); END;
CREATE TRIGGER run_supplies_node_credit_immutable_delete BEFORE DELETE ON run_supplies_node_credits
BEGIN SELECT RAISE(ABORT,'run_supplies_node_credit_immutable'); END;
CREATE TRIGGER run_supplies_revision_step BEFORE UPDATE OF balance,revision ON run_supplies_state
WHEN NEW.revision<>OLD.revision+1 OR NEW.balance<0
BEGIN SELECT RAISE(ABORT,'run_supplies_revision_conflict'); END;
CREATE TABLE run_supplies_credit_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

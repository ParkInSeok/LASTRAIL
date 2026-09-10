-- Explicit preference only; no operator grant or battle-effect policy.
ALTER TABLE account_formation ADD COLUMN ultimate_operator_id TEXT;
ALTER TABLE account_formation ADD COLUMN ultimate_revision INTEGER NOT NULL DEFAULT 0 CHECK(ultimate_revision>=0);

-- Runs in the same activation transaction. Inactive preset saves never touch this row.
CREATE TRIGGER formation_clear_removed_ultimate AFTER UPDATE OF formation_json ON account_formation
WHEN NEW.ultimate_operator_id IS NOT NULL AND NOT EXISTS(
  SELECT 1 FROM json_each(NEW.formation_json,'$.operators')
  WHERE json_extract(value,'$.operatorId')=NEW.ultimate_operator_id)
BEGIN
  UPDATE account_formation SET ultimate_operator_id=NULL,ultimate_revision=ultimate_revision+1
  WHERE account_id=NEW.account_id;
END;

CREATE TABLE ultimate_selection_receipts (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  idempotency_key TEXT NOT NULL,
  request_hash TEXT NOT NULL,
  response_json TEXT NOT NULL CHECK(json_valid(response_json)),
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id),
  PRIMARY KEY(account_id,idempotency_key)
);
CREATE TRIGGER ultimate_receipts_no_update BEFORE UPDATE ON ultimate_selection_receipts
BEGIN SELECT RAISE(ABORT,'ultimate receipt immutable'); END;
CREATE TRIGGER ultimate_receipts_no_replace BEFORE INSERT ON ultimate_selection_receipts
WHEN EXISTS(SELECT 1 FROM ultimate_selection_receipts WHERE account_id=NEW.account_id AND idempotency_key=NEW.idempotency_key)
BEGIN SELECT RAISE(ABORT,'ultimate receipt immutable'); END;
CREATE TABLE ultimate_write_guard (valid INTEGER NOT NULL CHECK(valid=1));

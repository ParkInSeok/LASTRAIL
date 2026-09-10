-- Installation preferences only: existing account+carriage_id remains identity.
CREATE TABLE account_train_loadout (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  loadout_json TEXT NOT NULL CHECK(json_valid(loadout_json)),
  revision INTEGER NOT NULL CHECK(revision>0),
  updated_at INTEGER NOT NULL
);
CREATE TABLE train_loadout_receipts (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  idempotency_key TEXT NOT NULL,
  request_hash TEXT NOT NULL,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  response_json TEXT NOT NULL CHECK(json_valid(response_json)),
  created_at INTEGER NOT NULL,
  UNIQUE(account_id,idempotency_key)
);
CREATE TRIGGER train_loadout_receipts_no_update BEFORE UPDATE ON train_loadout_receipts
BEGIN SELECT RAISE(ABORT,'train loadout receipt immutable'); END;
CREATE TRIGGER train_loadout_receipts_no_replace BEFORE INSERT ON train_loadout_receipts
WHEN EXISTS(SELECT 1 FROM train_loadout_receipts WHERE id=NEW.id OR (account_id=NEW.account_id AND idempotency_key=NEW.idempotency_key))
BEGIN SELECT RAISE(ABORT,'train loadout receipt immutable'); END;
CREATE TABLE train_loadout_write_guard (valid INTEGER NOT NULL CHECK(valid=1));

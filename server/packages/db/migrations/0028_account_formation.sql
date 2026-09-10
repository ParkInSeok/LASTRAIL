-- Selection preferences only. No ownership, prices, inventory or run grants.
CREATE UNIQUE INDEX commander_presets_account_id_id ON commander_presets(account_id,id);
CREATE TABLE account_formation (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  preset_id TEXT NOT NULL,
  preset_revision INTEGER NOT NULL CHECK(preset_revision>0),
  formation_json TEXT NOT NULL CHECK(json_valid(formation_json)),
  revision INTEGER NOT NULL CHECK(revision>0),
  updated_at INTEGER NOT NULL,
  FOREIGN KEY(account_id,preset_id) REFERENCES commander_presets(account_id,id) ON DELETE RESTRICT
);
CREATE TABLE formation_receipts (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  idempotency_key TEXT NOT NULL,
  request_hash TEXT NOT NULL,
  mutation_id TEXT NOT NULL REFERENCES account_mutations(id) ON DELETE RESTRICT,
  response_json TEXT NOT NULL CHECK(json_valid(response_json)),
  created_at INTEGER NOT NULL,
  UNIQUE(account_id,idempotency_key)
);
CREATE TRIGGER formation_receipts_no_update BEFORE UPDATE ON formation_receipts
BEGIN SELECT RAISE(ABORT,'formation receipt immutable'); END;
CREATE TRIGGER formation_receipts_no_replace BEFORE INSERT ON formation_receipts
WHEN EXISTS(SELECT 1 FROM formation_receipts WHERE id=NEW.id OR (account_id=NEW.account_id AND idempotency_key=NEW.idempotency_key))
BEGIN SELECT RAISE(ABORT,'formation receipt immutable'); END;
CREATE TABLE formation_write_guard (valid INTEGER NOT NULL CHECK(valid=1));

-- Link new offline claims to their exact persisted policy metadata. Old rows are
-- left NULL; do not infer/reprice their grants from a later version binding.
ALTER TABLE account_offline_reward_state ADD COLUMN last_reward_transaction_id TEXT
  REFERENCES reward_transactions(id) ON DELETE SET NULL;

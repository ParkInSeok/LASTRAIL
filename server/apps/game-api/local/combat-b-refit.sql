-- ISOLATED LOCAL fixture source only. Not an operating migration or free rank issuance.
-- Original B selection/receipts and old run snapshots remain unchanged.
CREATE TABLE combat_b_fixture_refits (
 account_id TEXT PRIMARY KEY REFERENCES accounts(id),
 revision INTEGER NOT NULL CHECK(revision BETWEEN 1 AND 9007199254740990),
 selection_revision INTEGER NOT NULL CHECK(selection_revision BETWEEN 1 AND 9007199254740990),
 source_json TEXT NOT NULL CHECK(json_valid(source_json))
);
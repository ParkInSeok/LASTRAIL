-- Personal-only relations. No operating identity seeding, reciprocal sanction, or game state changes.
-- Retired public identities remain removable; deliberately no FK to their mutable public mapping.
CREATE TABLE community_block_state(account_id TEXT PRIMARY KEY REFERENCES accounts(id),revision INTEGER NOT NULL CHECK(revision>=0));
CREATE TABLE community_blocks(account_id TEXT NOT NULL REFERENCES community_block_state(account_id),public_author_id TEXT NOT NULL,PRIMARY KEY(account_id,public_author_id));
CREATE TABLE community_block_receipts(account_id TEXT NOT NULL REFERENCES community_block_state(account_id),command_id TEXT NOT NULL,request_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),PRIMARY KEY(account_id,command_id));
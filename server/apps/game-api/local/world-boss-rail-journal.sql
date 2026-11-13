-- Isolated LOCAL trial schema. Never included in operational migrations.
CREATE TABLE world_boss_rail_geometry_heads (
 attempt_id TEXT PRIMARY KEY REFERENCES world_boss_local_rail_attempts(attempt_id),
 journal_version TEXT NOT NULL CHECK(journal_version='world-boss.rail-journal.v1.20261002'),
 owner_revision INTEGER NOT NULL CHECK(owner_revision>0),
 rail_json TEXT NOT NULL, frame_json TEXT NOT NULL, geometry_json TEXT NOT NULL,
 binding_json TEXT NOT NULL, last_hash TEXT NOT NULL
);
CREATE TABLE world_boss_rail_geometry_journal (
 attempt_id TEXT NOT NULL REFERENCES world_boss_rail_geometry_heads(attempt_id),
 owner_revision INTEGER NOT NULL CHECK(owner_revision>0),
 entry_key TEXT NOT NULL, entry_json TEXT NOT NULL, entry_hash TEXT NOT NULL,
 PRIMARY KEY(attempt_id,owner_revision), UNIQUE(attempt_id,entry_key)
);
CREATE TABLE world_boss_rail_journal_guard(valid INTEGER NOT NULL CHECK(valid=1));
-- Attachment transfers local write coordination to the journal. Existing source remains rail owner.
-- Head is updated first within the SAME atomic batch. A standalone source writer is rejected.
CREATE TRIGGER world_boss_rail_journal_fence BEFORE UPDATE ON world_boss_local_rail_attempts
WHEN EXISTS(SELECT 1 FROM world_boss_rail_geometry_heads WHERE attempt_id=OLD.attempt_id)
AND NOT EXISTS(SELECT 1 FROM world_boss_rail_geometry_heads WHERE attempt_id=NEW.attempt_id
 AND owner_revision=NEW.owner_revision AND rail_json=NEW.rail_json AND frame_json=NEW.frame_json)
BEGIN SELECT RAISE(ABORT,'rail_journal_writer_required'); END;
-- Original receipt/sample/event provenance cannot be removed between load and commit.
CREATE TRIGGER world_boss_rail_journal_no_update BEFORE UPDATE ON world_boss_rail_geometry_journal
BEGIN SELECT RAISE(ABORT,'rail_journal_append_only'); END;
CREATE TRIGGER world_boss_rail_journal_no_delete BEFORE DELETE ON world_boss_rail_geometry_journal
BEGIN SELECT RAISE(ABORT,'rail_journal_append_only'); END;
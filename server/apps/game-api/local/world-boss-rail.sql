-- Isolated LOCAL_TEST_PROFILE storage only. Never part of the operational migration chain.
CREATE TABLE world_boss_local_rail_attempts (
 attempt_id TEXT PRIMARY KEY,
 profile_status TEXT NOT NULL CHECK(profile_status='LOCAL_TEST_PROFILE'),
 owner_revision INTEGER NOT NULL CHECK(owner_revision>0),
 rail_json TEXT NOT NULL,
 frame_json TEXT NOT NULL
);
CREATE TABLE world_boss_local_rail_write_guard (valid INTEGER NOT NULL CHECK(valid=1));
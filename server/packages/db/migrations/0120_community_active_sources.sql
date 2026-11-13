-- Explicit isolated current-service capture only; no automatic backfill, cutover, purge or operating seed.
CREATE TABLE community_active_sources(
 content_id TEXT PRIMARY KEY REFERENCES community_contents(id),
 snapshot_json TEXT NOT NULL CHECK(json_valid(snapshot_json))
);
-- One current row, replaced by strict source verification plus content/source CAS. No history/body/receipt copy.

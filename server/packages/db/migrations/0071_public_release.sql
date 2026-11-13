-- Immutable public projections and revisioned current pointer. No catalog is seeded here.
CREATE TABLE public_catalog_snapshots (
  snapshot_id TEXT PRIMARY KEY,
  environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
  payload_json TEXT NOT NULL CHECK(json_valid(payload_json)),
  payload_digest TEXT NOT NULL
);
CREATE TRIGGER public_catalog_snapshots_no_update BEFORE UPDATE ON public_catalog_snapshots
BEGIN SELECT RAISE(ABORT,'public_catalog_snapshot_immutable'); END;
CREATE TRIGGER public_catalog_snapshots_no_delete BEFORE DELETE ON public_catalog_snapshots
BEGIN SELECT RAISE(ABORT,'public_catalog_snapshot_immutable'); END;
CREATE TABLE public_release_indices (
  index_id TEXT PRIMARY KEY,
  environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
  channel TEXT NOT NULL,
  app_version_code TEXT NOT NULL,
  revision INTEGER NOT NULL CHECK(revision>0),
  payload_json TEXT NOT NULL CHECK(json_valid(payload_json)),
  payload_digest TEXT NOT NULL,
  UNIQUE(environment,channel,app_version_code,revision)
);
CREATE TRIGGER public_release_indices_no_update BEFORE UPDATE ON public_release_indices
BEGIN SELECT RAISE(ABORT,'public_release_index_immutable'); END;
CREATE TRIGGER public_release_indices_no_delete BEFORE DELETE ON public_release_indices
BEGIN SELECT RAISE(ABORT,'public_release_index_immutable'); END;
CREATE TABLE public_release_heads (
  environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
  channel TEXT NOT NULL,
  app_version_code TEXT NOT NULL,
  index_id TEXT NOT NULL REFERENCES public_release_indices(index_id),
  revision INTEGER NOT NULL CHECK(revision>0),
  PRIMARY KEY(environment,channel,app_version_code)
);
CREATE TABLE public_release_guard (valid INTEGER NOT NULL CHECK(valid=1));






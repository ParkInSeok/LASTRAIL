-- Extend the SAME runtime owner. Copy all original 0048 columns/rows exactly;
-- plans, receipts, audit and historical revisions are not rewritten or seeded.
CREATE TABLE maintenance_runtime_revisions_v2 (
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 application_id TEXT NOT NULL CHECK(length(application_id) BETWEEN 1 AND 80),
 revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991),
 state TEXT NOT NULL CHECK(state IN ('NORMAL','SCHEDULED','ACTIVE','COMPLETED','CANCELLED','IMMEDIATE')),
 maintenance_id TEXT, draft_revision INTEGER,
 drain_starts_at INTEGER, starts_at INTEGER, estimated_end_at INTEGER, ended_at INTEGER,
 actor_id TEXT NOT NULL REFERENCES admin_principals(id), reason TEXT NOT NULL,
 created_at INTEGER NOT NULL CHECK(typeof(created_at)='integer' AND created_at BETWEEN 0 AND 9007199254740991),
 PRIMARY KEY(environment,application_id,revision),
 FOREIGN KEY(environment,maintenance_id,draft_revision) REFERENCES maintenance_plan_revisions(environment,maintenance_id,draft_revision),
 CHECK((state='NORMAL' AND revision=1 AND maintenance_id IS NULL AND draft_revision IS NULL AND starts_at IS NULL AND drain_starts_at IS NULL AND estimated_end_at IS NULL AND ended_at IS NULL)
 OR (state IN ('SCHEDULED','ACTIVE','COMPLETED','CANCELLED') AND maintenance_id IS NOT NULL AND draft_revision IS NOT NULL AND typeof(draft_revision)='integer' AND draft_revision>0
 AND starts_at IS NOT NULL AND typeof(starts_at)='integer' AND starts_at BETWEEN 0 AND 9007199254740991
 AND (drain_starts_at IS NULL OR (typeof(drain_starts_at)='integer' AND drain_starts_at BETWEEN 0 AND starts_at))
 AND (estimated_end_at IS NULL OR (typeof(estimated_end_at)='integer' AND estimated_end_at>starts_at AND estimated_end_at<=9007199254740991))
 AND ((state IN ('SCHEDULED','ACTIVE') AND ended_at IS NULL) OR (state IN ('COMPLETED','CANCELLED') AND ended_at IS NOT NULL AND typeof(ended_at)='integer' AND ended_at BETWEEN 0 AND 9007199254740991)))
 OR (state='IMMEDIATE' AND revision>1 AND maintenance_id IS NULL AND draft_revision IS NULL AND starts_at IS NULL AND drain_starts_at IS NULL AND ended_at IS NULL
 AND (estimated_end_at IS NULL OR (typeof(estimated_end_at)='integer' AND estimated_end_at>created_at AND estimated_end_at<=9007199254740991))))
);
INSERT INTO maintenance_runtime_revisions_v2 SELECT * FROM maintenance_runtime_revisions;
CREATE TABLE maintenance_runtime_heads_v2 (
 environment TEXT NOT NULL, application_id TEXT NOT NULL, revision INTEGER NOT NULL,
 PRIMARY KEY(environment,application_id),
 FOREIGN KEY(environment,application_id,revision) REFERENCES maintenance_runtime_revisions_v2(environment,application_id,revision)
);
INSERT INTO maintenance_runtime_heads_v2 SELECT * FROM maintenance_runtime_heads;
DROP TABLE maintenance_runtime_heads;
DROP TABLE maintenance_runtime_revisions;
ALTER TABLE maintenance_runtime_revisions_v2 RENAME TO maintenance_runtime_revisions;
ALTER TABLE maintenance_runtime_heads_v2 RENAME TO maintenance_runtime_heads;
CREATE TRIGGER maintenance_head_revision BEFORE UPDATE ON maintenance_runtime_heads
WHEN NEW.environment<>OLD.environment OR NEW.application_id<>OLD.application_id OR NEW.revision<>OLD.revision+1
BEGIN SELECT RAISE(ABORT,'maintenance_revision_conflict'); END;
CREATE TRIGGER maintenance_head_no_delete BEFORE DELETE ON maintenance_runtime_heads
BEGIN SELECT RAISE(ABORT,'maintenance_head_immutable'); END;
CREATE TRIGGER maintenance_revision_no_update BEFORE UPDATE ON maintenance_runtime_revisions
BEGIN SELECT RAISE(ABORT,'maintenance_revision_immutable'); END;
CREATE TRIGGER maintenance_revision_no_delete BEFORE DELETE ON maintenance_runtime_revisions
BEGIN SELECT RAISE(ABORT,'maintenance_revision_immutable'); END;
CREATE TABLE maintenance_immediate_modes (
 environment TEXT NOT NULL, application_id TEXT NOT NULL, revision INTEGER NOT NULL,
 game_mode TEXT NOT NULL CHECK(game_mode IN ('NORMAL','DRAINING','MAINTENANCE')),
 -- Existing-session WEB filtering is not implemented: those choices stay rejected.
 web_mode TEXT NOT NULL CHECK(web_mode='NORMAL'),
 PRIMARY KEY(environment,application_id,revision),
 FOREIGN KEY(environment,application_id,revision) REFERENCES maintenance_runtime_revisions(environment,application_id,revision)
);
CREATE TRIGGER maintenance_immediate_source BEFORE INSERT ON maintenance_immediate_modes
WHEN NOT EXISTS(SELECT 1 FROM maintenance_runtime_revisions r WHERE r.environment=NEW.environment
 AND r.application_id=NEW.application_id AND r.revision=NEW.revision AND r.state='IMMEDIATE')
BEGIN SELECT RAISE(ABORT,'maintenance_immediate_source_conflict'); END;
CREATE TRIGGER maintenance_immediate_no_update BEFORE UPDATE ON maintenance_immediate_modes
BEGIN SELECT RAISE(ABORT,'maintenance_revision_immutable'); END;
CREATE TRIGGER maintenance_immediate_no_delete BEFORE DELETE ON maintenance_immediate_modes
BEGIN SELECT RAISE(ABORT,'maintenance_revision_immutable'); END;

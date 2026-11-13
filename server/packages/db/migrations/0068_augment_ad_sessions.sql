-- Internal trusted session reservation only; no operational timing/ad policy seed.
CREATE TABLE augment_ad_sessions (
 id TEXT PRIMARY KEY, run_id TEXT NOT NULL,event_id TEXT NOT NULL,
 session_json TEXT NOT NULL CHECK(json_valid(session_json)),
 callback_deadline INTEGER NOT NULL,
 status TEXT NOT NULL CHECK(status IN ('AD_PENDING','EXPIRED','APPLIED')),
 FOREIGN KEY(run_id,event_id) REFERENCES solo_augment_grants(run_id,event_id)
);
CREATE UNIQUE INDEX augment_ad_one_pending ON augment_ad_sessions(run_id,event_id) WHERE status='AD_PENDING';
CREATE TRIGGER augment_ad_origin_immutable BEFORE UPDATE ON augment_ad_sessions
WHEN NEW.id<>OLD.id OR NEW.run_id<>OLD.run_id OR NEW.event_id<>OLD.event_id OR NEW.session_json<>OLD.session_json OR NEW.callback_deadline<>OLD.callback_deadline
 OR OLD.status<>'AD_PENDING' OR NEW.status='AD_PENDING'
BEGIN SELECT RAISE(ABORT,'augment_ad_immutable'); END;
CREATE TRIGGER augment_ad_no_delete BEFORE DELETE ON augment_ad_sessions BEGIN SELECT RAISE(ABORT,'augment_ad_immutable'); END;
CREATE TRIGGER augment_grant_pending BEFORE UPDATE ON solo_augment_grants
WHEN EXISTS(SELECT 1 FROM augment_ad_sessions a WHERE a.run_id=OLD.run_id AND a.event_id=OLD.event_id AND a.status='AD_PENDING')
BEGIN SELECT RAISE(ABORT,'augment_ad_pending'); END;

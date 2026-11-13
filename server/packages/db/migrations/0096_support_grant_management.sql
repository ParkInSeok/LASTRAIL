-- Preserve existing explicit grants. Never seed a new processor during migration.
-- Revision tombstones have no principal FK: deletion/recreation must not reset them.
CREATE TABLE support_grant_revisions (
 principal_id TEXT PRIMARY KEY,
 revision INTEGER NOT NULL CHECK(typeof(revision)='integer' AND revision BETWEEN 1 AND 9007199254740991)
);
INSERT INTO support_grant_revisions SELECT principal_id,1 FROM support_ticket_processor_grants;
CREATE TRIGGER support_grant_revision_no_reset BEFORE UPDATE ON support_grant_revisions
WHEN NEW.principal_id<>OLD.principal_id OR NEW.revision<>OLD.revision+1
BEGIN SELECT RAISE(ABORT,'support_grant_revision_conflict'); END;
CREATE TRIGGER support_grant_revision_no_delete BEFORE DELETE ON support_grant_revisions
BEGIN SELECT RAISE(ABORT,'support_grant_revision_immutable'); END;

CREATE TRIGGER support_grant_insert_revision AFTER INSERT ON support_ticket_processor_grants
BEGIN
 INSERT INTO support_grant_revisions VALUES(NEW.principal_id,1)
 ON CONFLICT(principal_id) DO UPDATE SET revision=revision+1;
END;
CREATE TRIGGER support_grant_delete_revision AFTER DELETE ON support_ticket_processor_grants
BEGIN
 INSERT INTO support_grant_revisions VALUES(OLD.principal_id,1)
 ON CONFLICT(principal_id) DO UPDATE SET revision=revision+1;
END;
CREATE TRIGGER support_grant_no_update BEFORE UPDATE ON support_ticket_processor_grants
BEGIN SELECT RAISE(ABORT,'support_grant_update_forbidden'); END;

CREATE TABLE support_grant_commands (
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 actor_id TEXT NOT NULL,target_id TEXT NOT NULL,
 request_key TEXT NOT NULL CHECK(length(request_key) BETWEEN 1 AND 128),
 request_hash TEXT NOT NULL,
 action TEXT NOT NULL CHECK(action IN ('GRANT','REVOKE')),
 reason TEXT NOT NULL CHECK(length(trim(reason)) BETWEEN 1 AND 500),
 before_granted INTEGER NOT NULL CHECK(before_granted IN (0,1)),
 after_granted INTEGER NOT NULL CHECK(after_granted IN (0,1)),
 before_revision INTEGER NOT NULL CHECK(typeof(before_revision)='integer' AND before_revision BETWEEN 0 AND 9007199254740991),
 after_revision INTEGER NOT NULL CHECK(typeof(after_revision)='integer' AND after_revision BETWEEN 0 AND 9007199254740991),
 audit_id TEXT NOT NULL REFERENCES admin_audit_logs(id),
 response_json TEXT NOT NULL CHECK(json_valid(response_json)),created_at INTEGER NOT NULL,
 PRIMARY KEY(environment,actor_id,request_key),
 CHECK(after_revision=before_revision+CASE WHEN before_granted<>after_granted THEN 1 ELSE 0 END),
 CHECK((action='GRANT' AND after_granted=1) OR (action='REVOKE' AND after_granted=0))
);
CREATE TRIGGER support_grant_command_guard BEFORE INSERT ON support_grant_commands
BEGIN
 SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM admin_principals WHERE id=NEW.actor_id AND status='ACTIVE' AND role='OWNER')
 THEN RAISE(ABORT,'support_grant_forbidden') END;
 SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM admin_principals WHERE id=NEW.target_id)
 THEN RAISE(ABORT,'support_grant_target_missing') END;
 SELECT CASE WHEN NEW.action='GRANT' AND NOT EXISTS(SELECT 1 FROM admin_principals WHERE id=NEW.target_id AND status='ACTIVE' AND role IN ('OWNER','OPERATOR'))
 THEN RAISE(ABORT,'support_grant_target_ineligible') END;
 SELECT CASE WHEN COALESCE((SELECT revision FROM support_grant_revisions WHERE principal_id=NEW.target_id),0)<>NEW.before_revision
 OR EXISTS(SELECT 1 FROM support_ticket_processor_grants WHERE principal_id=NEW.target_id)<>NEW.before_granted
 THEN RAISE(ABORT,'support_grant_revision_conflict') END;
END;
CREATE TRIGGER support_grant_command_apply AFTER INSERT ON support_grant_commands
BEGIN
 DELETE FROM support_ticket_processor_grants WHERE principal_id=NEW.target_id AND NEW.action='REVOKE';
 INSERT INTO support_ticket_processor_grants SELECT NEW.target_id,'SUPPORT_TICKET_PROCESS',NEW.created_at
 WHERE NEW.action='GRANT' AND NEW.before_granted=0;
END;
CREATE TRIGGER support_grant_command_no_update BEFORE UPDATE ON support_grant_commands
BEGIN SELECT RAISE(ABORT,'support_grant_command_immutable'); END;
CREATE TRIGGER support_grant_command_no_delete BEFORE DELETE ON support_grant_commands
BEGIN SELECT RAISE(ABORT,'support_grant_command_immutable'); END;

-- Shared DB boundary covers Admin principal upserts and any future role/status service.
-- Trigger-origin invalidations use SYSTEM attribution; do not invent an initiating OWNER.
CREATE TRIGGER support_grant_principal_invalidation AFTER UPDATE OF role,status ON admin_principals
WHEN (NEW.status<>'ACTIVE' OR NEW.role NOT IN ('OWNER','OPERATOR'))
 AND EXISTS(SELECT 1 FROM support_ticket_processor_grants WHERE principal_id=NEW.id)
BEGIN
 DELETE FROM support_ticket_processor_grants WHERE principal_id=NEW.id;
 INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,change_summary_json,result,created_at)
 VALUES('support_invalidation_'||lower(hex(randomblob(16))),NULL,'SYSTEM','SUPPORT_TICKET_PROCESS','SUPPORT_GRANT_INVALIDATE','admin_principal',NEW.id,
 'principal_transition_'||lower(hex(randomblob(16))),json_object('environment',COALESCE((SELECT environment FROM support_grant_commands WHERE target_id=NEW.id ORDER BY created_at DESC,rowid DESC LIMIT 1),'LEGACY_DATABASE_BOUND'),'cause','PRINCIPAL_INELIGIBLE','beforeGranted',1,'afterGranted',0,'beforeRevision',(SELECT revision-1 FROM support_grant_revisions WHERE principal_id=NEW.id),'beforeRole',OLD.role,'afterRole',NEW.role,'beforeStatus',OLD.status,'afterStatus',NEW.status,'afterRevision',(SELECT revision FROM support_grant_revisions WHERE principal_id=NEW.id)),'SUCCESS',NEW.updated_at);
END;
CREATE TRIGGER support_grant_principal_deletion BEFORE DELETE ON admin_principals
WHEN EXISTS(SELECT 1 FROM support_ticket_processor_grants WHERE principal_id=OLD.id)
BEGIN
 DELETE FROM support_ticket_processor_grants WHERE principal_id=OLD.id;
 INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,change_summary_json,result,created_at)
 VALUES('support_invalidation_'||lower(hex(randomblob(16))),NULL,'SYSTEM','SUPPORT_TICKET_PROCESS','SUPPORT_GRANT_INVALIDATE','admin_principal',OLD.id,
 'principal_transition_'||lower(hex(randomblob(16))),json_object('environment',COALESCE((SELECT environment FROM support_grant_commands WHERE target_id=OLD.id ORDER BY created_at DESC,rowid DESC LIMIT 1),'LEGACY_DATABASE_BOUND'),'cause','PRINCIPAL_DELETED','beforeGranted',1,'afterGranted',0,'beforeRevision',(SELECT revision-1 FROM support_grant_revisions WHERE principal_id=OLD.id),'afterRevision',(SELECT revision FROM support_grant_revisions WHERE principal_id=OLD.id)),'SUCCESS',CAST((julianday('now')-2440587.5)*86400000 AS INTEGER));
END;

-- Ineligible legacy rows must not become effective again on future reactivation.
-- Their old grant/revision evidence remains in the audit and revision tombstone.
INSERT INTO admin_audit_logs(id,principal_id,role,permission,action,resource_type,resource_id,request_id,change_summary_json,result,created_at)
SELECT 'support_migration_'||lower(hex(randomblob(16))),NULL,'SYSTEM','SUPPORT_TICKET_PROCESS','SUPPORT_GRANT_INVALIDATE','admin_principal',g.principal_id,
 'migration_0096_'||lower(hex(randomblob(16))),json_object('cause','INELIGIBLE_LEGACY_GRANT','environment','LEGACY_DATABASE_BOUND','beforeGranted',1,'afterGranted',0,'beforeRevision',r.revision,'afterRevision',r.revision+1),'SUCCESS',CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)
FROM support_ticket_processor_grants g JOIN support_grant_revisions r USING(principal_id)
JOIN admin_principals p ON p.id=g.principal_id WHERE p.status<>'ACTIVE' OR p.role NOT IN ('OWNER','OPERATOR');
DELETE FROM support_ticket_processor_grants WHERE principal_id IN (SELECT id FROM admin_principals WHERE status<>'ACTIVE' OR role NOT IN ('OWNER','OPERATOR'));

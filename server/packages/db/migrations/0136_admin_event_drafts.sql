-- Draft definitions only. No scheduling, entitlement, settlement or publication state.
CREATE TABLE admin_event_drafts (
 environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
 round_id TEXT NOT NULL,revision INTEGER NOT NULL CHECK(revision>0 AND revision<=9007199254740991),
 definition_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 saved_by TEXT NOT NULL REFERENCES admin_principals(id),saved_at INTEGER NOT NULL,
 PRIMARY KEY(environment,round_id,revision)
);
CREATE TABLE admin_event_draft_receipts (
 environment TEXT NOT NULL,principal_id TEXT NOT NULL REFERENCES admin_principals(id),
 request_key TEXT NOT NULL,payload_hash TEXT NOT NULL,response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(environment,principal_id,request_key)
);
CREATE TRIGGER admin_event_draft_no_update BEFORE UPDATE ON admin_event_drafts BEGIN SELECT RAISE(ABORT,'immutable_event_draft_revision'); END;
CREATE TRIGGER admin_event_draft_no_delete BEFORE DELETE ON admin_event_drafts BEGIN SELECT RAISE(ABORT,'immutable_event_draft_revision'); END;
CREATE TRIGGER admin_event_draft_receipt_no_update BEFORE UPDATE ON admin_event_draft_receipts BEGIN SELECT RAISE(ABORT,'immutable_event_draft_receipt'); END;
CREATE TRIGGER admin_event_draft_receipt_no_delete BEFORE DELETE ON admin_event_draft_receipts BEGIN SELECT RAISE(ABORT,'immutable_event_draft_receipt'); END;
CREATE TABLE admin_event_draft_write_guard(valid INTEGER NOT NULL CHECK(valid=1));

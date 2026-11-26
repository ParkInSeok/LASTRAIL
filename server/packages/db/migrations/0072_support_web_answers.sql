-- Processing data is separate from the immutable player submission and receipt key.
-- Deliberately empty on migration: an Admin grant workflow must explicitly provision it.
CREATE TABLE support_ticket_processor_grants (
  principal_id TEXT PRIMARY KEY REFERENCES admin_principals(id) ON DELETE CASCADE,
  permission TEXT NOT NULL CHECK (permission = 'SUPPORT_TICKET_PROCESS'),
  created_at INTEGER NOT NULL
);

CREATE TABLE support_ticket_processing (
  ticket_id TEXT PRIMARY KEY REFERENCES support_tickets(id) ON DELETE CASCADE,
  revision INTEGER NOT NULL CHECK (revision > 0),
  assignee_principal_id TEXT REFERENCES admin_principals(id) ON DELETE SET NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE support_ticket_commands (
  id TEXT PRIMARY KEY,
  ticket_id TEXT NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE,
  principal_id TEXT NOT NULL REFERENCES admin_principals(id) ON DELETE RESTRICT,
  idempotency_key TEXT NOT NULL CHECK (length(idempotency_key) BETWEEN 1 AND 128),
  request_hash TEXT NOT NULL,
  expected_revision INTEGER NOT NULL CHECK (expected_revision >= 0),
  resulting_revision INTEGER NOT NULL CHECK (resulting_revision = expected_revision + 1),
  next_status TEXT NOT NULL CHECK (next_status IN ('OPEN', 'IN_REVIEW', 'CLOSED')),
  assignee_principal_id TEXT REFERENCES admin_principals(id) ON DELETE SET NULL,
  public_reply_text TEXT CHECK (public_reply_text IS NULL OR length(public_reply_text) BETWEEN 1 AND 2000),
  internal_note_text TEXT CHECK (internal_note_text IS NULL OR length(internal_note_text) BETWEEN 1 AND 2000),
  created_at INTEGER NOT NULL,
  UNIQUE(ticket_id, principal_id, idempotency_key),
  UNIQUE(ticket_id, resulting_revision)
);

CREATE INDEX idx_support_ticket_commands_public_reply
  ON support_ticket_commands(ticket_id, resulting_revision DESC)
  WHERE public_reply_text IS NOT NULL;

CREATE TRIGGER support_ticket_command_guard
BEFORE INSERT ON support_ticket_commands
BEGIN
  SELECT CASE WHEN NOT EXISTS (
    SELECT 1 FROM admin_principals principal
    JOIN support_ticket_processor_grants grant_row ON grant_row.principal_id = principal.id
    WHERE principal.id = NEW.principal_id AND principal.status = 'ACTIVE'
      AND principal.role IN ('OWNER', 'OPERATOR')
      AND grant_row.permission = 'SUPPORT_TICKET_PROCESS'
  ) THEN RAISE(ABORT, 'support_write_forbidden') END;
  SELECT CASE WHEN COALESCE((
    SELECT revision FROM support_ticket_processing WHERE ticket_id = NEW.ticket_id
  ), 0) <> NEW.expected_revision
  THEN RAISE(ABORT, 'support_revision_conflict') END;
END;

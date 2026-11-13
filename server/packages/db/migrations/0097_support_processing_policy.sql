-- Enforce the approved policy for new commands only. Existing commands and
-- original receipts remain intact, including historical closes without replies.
-- Keep 0072's independent permission/revision guard in force.
-- Match ECMAScript String.trim whitespace, including Unicode spaces and BOM.
CREATE TRIGGER support_ticket_transition_policy_guard
BEFORE INSERT ON support_ticket_commands
BEGIN
  SELECT CASE WHEN (
    SELECT status FROM support_tickets WHERE id = NEW.ticket_id
  ) = 'CLOSED' AND NEW.next_status <> 'CLOSED'
  THEN RAISE(ABORT, 'support_closed_reopen_forbidden') END;

  SELECT CASE WHEN (
    SELECT status FROM support_tickets WHERE id = NEW.ticket_id
  ) IN ('OPEN', 'IN_REVIEW') AND NEW.next_status = 'CLOSED'
    AND (NEW.public_reply_text IS NULL OR
      length(trim(NEW.public_reply_text, char(9,10,11,12,13,32,160,5760,
        8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,
        8232,8233,8239,8287,12288,65279))) = 0)
  THEN RAISE(ABORT, 'support_final_reply_required') END;

  SELECT CASE WHEN (
    SELECT status FROM support_tickets WHERE id = NEW.ticket_id
  ) = 'IN_REVIEW' AND NEW.next_status = 'OPEN'
    AND (NEW.internal_note_text IS NULL OR
      length(trim(NEW.internal_note_text, char(9,10,11,12,13,32,160,5760,
        8192,8193,8194,8195,8196,8197,8198,8199,8200,8201,8202,
        8232,8233,8239,8287,12288,65279))) = 0)
  THEN RAISE(ABORT, 'support_return_reason_required') END;
END;

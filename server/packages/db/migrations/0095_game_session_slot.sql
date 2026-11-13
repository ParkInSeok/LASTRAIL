-- New verified game login wins. Browser sessions use their independent web_sessions table.
-- Retain all historical rows; choose the latest legacy ACTIVE row deterministically.
UPDATE auth_sessions SET status='REVOKED'
WHERE status='ACTIVE' AND id <> (
  SELECT latest.id FROM auth_sessions latest
  WHERE latest.account_id=auth_sessions.account_id AND latest.status='ACTIVE'
  ORDER BY latest.created_at DESC,latest.generation DESC,latest.id DESC LIMIT 1
);

CREATE UNIQUE INDEX auth_sessions_single_active_account
ON auth_sessions(account_id) WHERE status='ACTIVE';

CREATE TRIGGER game_session_verified_insert
BEFORE INSERT ON auth_sessions WHEN NEW.status='ACTIVE'
BEGIN
  SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM accounts WHERE id=NEW.account_id AND status='ACTIVE')
    THEN RAISE(ABORT,'game_session_account_inactive') END;
  -- Rotation is valid only if the exact previous generation was rotated in this transaction.
  SELECT CASE WHEN NEW.generation>0 AND NOT EXISTS(
    SELECT 1 FROM auth_sessions previous WHERE previous.account_id=NEW.account_id
      AND previous.family_id=NEW.family_id AND previous.generation=NEW.generation-1
      AND previous.status='ROTATED' AND previous.rotated_at=NEW.created_at
  ) THEN RAISE(ABORT,'game_session_rotation_stale') END;
  UPDATE auth_sessions SET status='REVOKED'
    WHERE account_id=NEW.account_id AND status IN ('ACTIVE','ROTATED') AND NEW.generation=0;
END;

CREATE TRIGGER game_session_no_reactivation
BEFORE UPDATE OF status ON auth_sessions WHEN NEW.status='ACTIVE' AND OLD.status<>'ACTIVE'
BEGIN
  SELECT RAISE(ABORT,'game_session_reactivation_forbidden');
END;

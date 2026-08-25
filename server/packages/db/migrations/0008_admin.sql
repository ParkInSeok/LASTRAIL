CREATE TABLE admin_principals (
  id TEXT PRIMARY KEY,
  access_subject TEXT NOT NULL UNIQUE,
  role TEXT NOT NULL CHECK (role IN ('OWNER','OPERATOR','VIEWER')),
  status TEXT NOT NULL CHECK (status IN ('ACTIVE','DISABLED')),
  display_name TEXT,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TABLE admin_confirmations (
  id TEXT PRIMARY KEY,
  principal_id TEXT NOT NULL REFERENCES admin_principals(id) ON DELETE RESTRICT,
  permission TEXT NOT NULL,
  payload_hash TEXT NOT NULL,
  token_hash TEXT NOT NULL,
  expires_at INTEGER NOT NULL,
  consumed_at INTEGER,
  created_at INTEGER NOT NULL,
  UNIQUE(token_hash)
);

CREATE TABLE admin_audit_logs (
  id TEXT PRIMARY KEY,
  principal_id TEXT REFERENCES admin_principals(id) ON DELETE SET NULL,
  role TEXT NOT NULL,
  permission TEXT NOT NULL,
  action TEXT NOT NULL,
  resource_type TEXT NOT NULL,
  resource_id TEXT,
  request_id TEXT NOT NULL,
  confirmation_id TEXT REFERENCES admin_confirmations(id) ON DELETE SET NULL,
  payload_hash TEXT,
  change_summary_json TEXT NOT NULL,
  account_mutation_id TEXT REFERENCES account_mutations(id) ON DELETE SET NULL,
  result TEXT NOT NULL CHECK (result IN ('SUCCESS','FAILURE')),
  error_code TEXT,
  created_at INTEGER NOT NULL
);

CREATE TABLE admin_jobs (
  id TEXT PRIMARY KEY,
  job_type TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('PENDING','RUNNING','SUCCEEDED','FAILED','CANCELLED')),
  payload_json TEXT NOT NULL,
  progress_json TEXT,
  lease_owner TEXT,
  lease_expires_at INTEGER,
  created_by TEXT REFERENCES admin_principals(id) ON DELETE SET NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE TRIGGER prevent_last_active_owner_disable
BEFORE UPDATE OF role, status ON admin_principals
WHEN OLD.role = 'OWNER' AND OLD.status = 'ACTIVE' AND (NEW.role <> 'OWNER' OR NEW.status <> 'ACTIVE')
BEGIN
  SELECT (CASE WHEN (SELECT COUNT(*) FROM admin_principals WHERE role = 'OWNER' AND status = 'ACTIVE' AND id <> OLD.id) = 0
    THEN RAISE(ABORT, 'LAST_ACTIVE_OWNER_REQUIRED') END);
END;

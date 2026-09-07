-- No tester account is granted by this migration. Provisioning is a separate owner action.
CREATE TABLE account_tester_entitlements (
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  environment TEXT NOT NULL CHECK(environment IN ('DEV','PRD')),
  version_code TEXT NOT NULL,
  status TEXT NOT NULL CHECK(status IN ('ACTIVE','REVOKED')),
  expires_at INTEGER NOT NULL,
  revoked_at INTEGER,
  granted_by TEXT NOT NULL,
  revision INTEGER NOT NULL DEFAULT 1 CHECK(revision > 0),
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL,
  PRIMARY KEY(account_id,environment,version_code)
);

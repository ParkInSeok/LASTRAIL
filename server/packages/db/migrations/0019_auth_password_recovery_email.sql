CREATE TABLE auth_recovery_emails (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  email_normalized TEXT NOT NULL,
  email_masked TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('PENDING','VERIFIED','REPLACEMENT_PENDING','REMOVED')),
  verified_at INTEGER,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE INDEX idx_auth_recovery_emails_email
  ON auth_recovery_emails(email_normalized, status);

CREATE TABLE auth_password_credentials (
  account_id TEXT PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
  login_id_normalized TEXT NOT NULL,
  password_hash TEXT NOT NULL,
  password_hash_algorithm TEXT NOT NULL CHECK (password_hash_algorithm IN ('ARGON2ID','SCRYPT','BCRYPT','PBKDF2_SHA256')),
  password_hash_params_json TEXT NOT NULL,
  recovery_email_normalized TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE UNIQUE INDEX idx_auth_password_credentials_login_id_unique
  ON auth_password_credentials(login_id_normalized);

CREATE TABLE auth_email_verification_challenges (
  id TEXT PRIMARY KEY,
  account_id TEXT REFERENCES accounts(id) ON DELETE CASCADE,
  email_normalized TEXT NOT NULL,
  purpose TEXT NOT NULL CHECK (purpose IN ('PASSWORD_REGISTER','PASSWORD_LINK','PASSWORD_RESET','RECOVERY_EMAIL_CHANGE')),
  verifier_hash TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('PENDING','CONSUMED','EXPIRED','CANCELLED')),
  expires_at INTEGER NOT NULL,
  consumed_at INTEGER,
  created_at INTEGER NOT NULL,
  updated_at INTEGER NOT NULL
);

CREATE INDEX idx_auth_email_verification_challenges_account_status
  ON auth_email_verification_challenges(account_id, status, expires_at);

CREATE INDEX idx_auth_email_verification_challenges_email_purpose
  ON auth_email_verification_challenges(email_normalized, purpose, status, expires_at);

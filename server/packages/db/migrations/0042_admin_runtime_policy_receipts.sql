-- Reuse 0041 ad_runtime_policies and the existing admin audit ledger.
CREATE TABLE admin_runtime_policy_receipts (
 principal_id TEXT NOT NULL REFERENCES admin_principals(id),
 environment TEXT NOT NULL,request_key TEXT NOT NULL,payload_hash TEXT NOT NULL,
 audit_id TEXT NOT NULL REFERENCES admin_audit_logs(id),response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(principal_id,environment,request_key)
);
CREATE TABLE admin_runtime_policy_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TRIGGER admin_runtime_policy_receipt_no_update BEFORE UPDATE ON admin_runtime_policy_receipts
BEGIN SELECT RAISE(ABORT,'admin_runtime_policy_receipt_immutable'); END;
CREATE TRIGGER admin_runtime_policy_receipt_no_delete BEFORE DELETE ON admin_runtime_policy_receipts
BEGIN SELECT RAISE(ABORT,'admin_runtime_policy_receipt_immutable'); END;

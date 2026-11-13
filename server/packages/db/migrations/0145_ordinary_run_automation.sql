-- New ordinary STARTs only. Historical runs are not assigned a default.
CREATE TABLE ordinary_run_automation (
 run_id TEXT PRIMARY KEY REFERENCES runs(id), account_id TEXT NOT NULL REFERENCES accounts(id),
 revision INTEGER NOT NULL CHECK(revision>0), ultimate_auto INTEGER NOT NULL CHECK(ultimate_auto IN (0,1)),
 actions_json TEXT NOT NULL CHECK(json_valid(actions_json))
);
CREATE TRIGGER ordinary_run_automation_revision BEFORE UPDATE ON ordinary_run_automation
WHEN NEW.run_id<>OLD.run_id OR NEW.account_id<>OLD.account_id OR NEW.revision<>OLD.revision+1
BEGIN SELECT RAISE(ABORT,'ordinary_automation_revision'); END;
CREATE TRIGGER ordinary_run_automation_no_delete BEFORE DELETE ON ordinary_run_automation
BEGIN SELECT RAISE(ABORT,'ordinary_automation_immutable_identity'); END;

-- No operation/category, cosmetic, SKU or season definitions are seeded.
CREATE TABLE objective_operation_bindings (
 operation_id TEXT NOT NULL, content_version TEXT NOT NULL, version TEXT NOT NULL,
 mode TEXT NOT NULL CHECK(mode IN ('ORDINARY_15','STORY_15','STORY_BOSS','WORLD')),
 PRIMARY KEY(operation_id,content_version)
);
CREATE TRIGGER objective_binding_immutable BEFORE UPDATE ON objective_operation_bindings
BEGIN SELECT RAISE(ABORT,'objective binding immutable'); END;
CREATE TRIGGER objective_binding_no_delete BEFORE DELETE ON objective_operation_bindings
BEGIN SELECT RAISE(ABORT,'objective binding immutable'); END;
CREATE TABLE objective_events (
 id INTEGER PRIMARY KEY AUTOINCREMENT, account_id TEXT NOT NULL REFERENCES accounts(id),
 kind TEXT NOT NULL CHECK(kind IN ('LOGIN','FINISH')), source_id TEXT NOT NULL,
 occurred_at INTEGER NOT NULL, mode TEXT NOT NULL, binding_version TEXT,
 status TEXT NOT NULL, ends INTEGER NOT NULL CHECK(ends BETWEEN 0 AND 1),
 waves INTEGER NOT NULL CHECK(waves BETWEEN 0 AND 15), source_json TEXT NOT NULL CHECK(json_valid(source_json)),
 UNIQUE(account_id,kind,source_id)
);
CREATE INDEX objective_events_account_time ON objective_events(account_id,occurred_at,id);
CREATE TRIGGER objective_event_immutable BEFORE UPDATE ON objective_events
BEGIN SELECT RAISE(ABORT,'objective event immutable'); END;
CREATE TRIGGER objective_event_no_delete BEFORE DELETE ON objective_events
BEGIN SELECT RAISE(ABORT,'objective event immutable'); END;
CREATE TABLE recurring_definitions (
 id TEXT PRIMARY KEY, system TEXT NOT NULL CHECK(system IN ('PASS50','QUESTS')),
 start_at INTEGER NOT NULL, end_at INTEGER NOT NULL CHECK(end_at>start_at), policy_json TEXT NOT NULL CHECK(json_valid(policy_json))
);
CREATE TRIGGER recurring_definition_overlap BEFORE INSERT ON recurring_definitions
WHEN EXISTS(SELECT 1 FROM recurring_definitions WHERE system=NEW.system AND start_at<NEW.end_at AND end_at>NEW.start_at)
BEGIN SELECT RAISE(ABORT,'recurring policy overlap'); END;
CREATE TRIGGER recurring_definition_immutable BEFORE UPDATE ON recurring_definitions
BEGIN SELECT RAISE(ABORT,'recurring policy immutable'); END;
CREATE TRIGGER recurring_definition_no_delete BEFORE DELETE ON recurring_definitions
BEGIN SELECT RAISE(ABORT,'recurring policy immutable'); END;
CREATE TABLE recurring_awards (
 id TEXT PRIMARY KEY, account_id TEXT NOT NULL REFERENCES accounts(id), policy_id TEXT NOT NULL REFERENCES recurring_definitions(id),
 system TEXT NOT NULL, period_id TEXT NOT NULL, objective TEXT NOT NULL,
 earned_at INTEGER NOT NULL, period_end INTEGER NOT NULL, xp INTEGER NOT NULL CHECK(xp>=0),
 rewards_json TEXT NOT NULL CHECK(json_valid(rewards_json)),
 UNIQUE(account_id,system,period_id,objective)
);
CREATE TRIGGER recurring_award_immutable BEFORE UPDATE ON recurring_awards
BEGIN SELECT RAISE(ABORT,'recurring award immutable'); END;
CREATE TABLE recurring_sync_state (
 account_id TEXT PRIMARY KEY REFERENCES accounts(id), last_event_id INTEGER NOT NULL
);
CREATE TABLE recurring_guard(valid INTEGER NOT NULL CHECK(valid=1));
CREATE TABLE recurring_grants (
 account_id TEXT NOT NULL REFERENCES accounts(id), right_key TEXT NOT NULL,
 policy_id TEXT NOT NULL REFERENCES recurring_definitions(id), claim_id TEXT NOT NULL UNIQUE REFERENCES reward_transactions(id),
 response_json TEXT NOT NULL CHECK(json_valid(response_json)), PRIMARY KEY(account_id,right_key)
);
CREATE TABLE recurring_requests (
 account_id TEXT NOT NULL REFERENCES accounts(id), operation TEXT NOT NULL, idempotency_key TEXT NOT NULL,
 request_hash TEXT NOT NULL, response_json TEXT NOT NULL CHECK(json_valid(response_json)),
 PRIMARY KEY(account_id,operation,idempotency_key)
);
CREATE TABLE quest_mail (
 award_id TEXT PRIMARY KEY REFERENCES recurring_awards(id), account_id TEXT NOT NULL REFERENCES accounts(id),
 sent_at INTEGER NOT NULL, expires_at INTEGER NOT NULL CHECK(expires_at=sent_at+1209600000)
);
CREATE TABLE pass_checkout_intents (
 id TEXT PRIMARY KEY, account_id TEXT NOT NULL REFERENCES accounts(id), policy_id TEXT NOT NULL REFERENCES recurring_definitions(id),
 created_at INTEGER NOT NULL, expires_at INTEGER NOT NULL, CHECK(expires_at>created_at)
);
CREATE TABLE pass50_entitlements (
 account_id TEXT NOT NULL REFERENCES accounts(id), policy_id TEXT NOT NULL REFERENCES recurring_definitions(id),
 intent_id TEXT NOT NULL UNIQUE REFERENCES pass_checkout_intents(id), order_id TEXT NOT NULL UNIQUE REFERENCES verified_store_orders(id),
 PRIMARY KEY(account_id,policy_id)
);
CREATE TABLE recurring_job_cursor (singleton_id INTEGER PRIMARY KEY CHECK(singleton_id=1), account_id TEXT NOT NULL);

-- Preserve all existing lots and ledger rows while expanding launch source provenance.
CREATE TABLE recurring_lots_backup AS SELECT * FROM crystal_lots;
CREATE TABLE recurring_ledger_backup AS SELECT * FROM crystal_ledger;
DROP TABLE crystal_ledger;
DROP TABLE crystal_lots;
CREATE TABLE crystal_lots (
  id TEXT PRIMARY KEY,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  currency_id TEXT NOT NULL,
  source TEXT NOT NULL CHECK(source IN ('FREE','PAID')),
  source_type TEXT NOT NULL CHECK(source_type IN ('ATTENDANCE','PASS_FREE','PASS_PREMIUM','STORE_PURCHASE','LAUNCH_ENTITLEMENT','QUEST')),
  source_ref TEXT NOT NULL,
  order_id TEXT REFERENCES verified_store_orders(id) ON DELETE RESTRICT,
  granted INTEGER NOT NULL CHECK(granted > 0 AND granted <= 2147483647),
  remaining INTEGER NOT NULL CHECK(remaining >= 0 AND remaining <= granted),
  operation_id TEXT NOT NULL REFERENCES crystal_operations(id) ON DELETE RESTRICT,
  created_at INTEGER NOT NULL,
  CHECK((source_type = 'STORE_PURCHASE' AND source = 'PAID' AND order_id IS NOT NULL)
    OR (source_type = 'PASS_PREMIUM' AND source = 'FREE' AND order_id IS NOT NULL)
    OR (source_type IN ('ATTENDANCE','PASS_FREE','LAUNCH_ENTITLEMENT','QUEST') AND source = 'FREE' AND order_id IS NULL))
);
CREATE INDEX idx_crystal_lots_balance ON crystal_lots(account_id, currency_id, source, created_at, id);
CREATE INDEX idx_crystal_lots_order ON crystal_lots(order_id);
CREATE TABLE crystal_ledger (
  id TEXT PRIMARY KEY,
  lot_id TEXT NOT NULL REFERENCES crystal_lots(id) ON DELETE RESTRICT,
  account_id TEXT NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
  operation_id TEXT NOT NULL REFERENCES crystal_operations(id) ON DELETE RESTRICT,
  delta INTEGER NOT NULL CHECK(delta <> 0),
  remaining_after INTEGER NOT NULL CHECK(remaining_after >= 0),
  reason TEXT NOT NULL,
  downstream_ref TEXT,
  created_at INTEGER NOT NULL
);
CREATE INDEX idx_crystal_ledger_operation ON crystal_ledger(operation_id);


INSERT INTO crystal_lots SELECT * FROM recurring_lots_backup;
INSERT INTO crystal_ledger SELECT * FROM recurring_ledger_backup;
DROP TABLE recurring_ledger_backup;
DROP TABLE recurring_lots_backup;

CREATE TRIGGER crystal_lot_order_guard BEFORE INSERT ON crystal_lots
WHEN NEW.order_id IS NOT NULL AND NOT EXISTS (
  SELECT 1 FROM verified_store_orders o WHERE o.id = NEW.order_id AND o.account_id = NEW.account_id
    AND o.revoked_at IS NULL AND o.product_kind = CASE WHEN NEW.source_type = 'PASS_PREMIUM' THEN 'PASS' ELSE 'CRYSTAL' END
)
BEGIN SELECT RAISE(ABORT, 'crystal order invalid'); END;

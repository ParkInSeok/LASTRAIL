import { describe, expect, it } from "vitest";
import initSqlJs from "sql.js";
import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { sha256Hex } from "@lastrail/common";
import snapshot from "../../packages/balance/src/launch-economy-v1.json";
import { mutateGrowth } from "../../apps/game-api/src/growth-service";
import { readEconomyBinding } from "../../apps/game-api/src/economy-binding";
import { SqlJsD1Database } from "../helpers/growth-d1";
import { Hono } from "hono";
import { registerGrowthRoutes } from "../../apps/game-api/src/growth-routes";

const version = snapshot.version;
const level = { kind: "CORE_LEVEL" as const, economyVersion: version, slot: 0, targetLevel: 2 };
async function setup(exp = 1000) {
  const SQL = await initSqlJs(); const sql = new SQL.Database();
  sql.create_function("unixepoch", () => Date.now() / 1000);
  sql.run("PRAGMA foreign_keys=ON");
  for (const file of readdirSync("packages/db/migrations").filter(f => /^\d+_.+\.sql$/u.test(f)).sort()) {
    sql.run(readFileSync(join("packages/db/migrations", file), "utf8"));
  }
  sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0)");
  for (const currency of ["CREDITS", "MACHINE_PARTS", "TACTICAL_DATA", "SPECIAL_CORE", "OPERATOR_EXP"]) {
    sql.run("INSERT INTO account_currencies VALUES('a',?,?,0)", [currency, currency === "OPERATOR_EXP" ? exp : 10000]);
  }
  sql.run("INSERT INTO balance_config_versions(version,schema_version,required_content_version,checksum,config_json,published_at,published_by) VALUES(1,1,0,'test','{}',0,'test')");
  sql.run("INSERT INTO balance_config_activations(id,version,activation_type,activated_by,activated_at,reason) VALUES('test',1,'PUBLISH','test',0,'test')");
  sql.run("INSERT INTO balance_config_active(singleton_id,version,activation_id,updated_at) VALUES(1,1,'test',0)");
  const json = JSON.stringify(snapshot);
  sql.run("INSERT INTO economy_versions VALUES(?,?,?,0)", [version, await sha256Hex(json), json]);
  sql.run("INSERT INTO economy_version_bindings VALUES(1,?)", [version]);
  for (let i = 0; i < 6; i++) sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a',?,0)", [`owned${i}`]);
  for (let i = 0; i < 5; i++) sql.run("INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id) VALUES('a',?,?)", [i, `owned${i}`]);
  sql.run("INSERT INTO account_train(account_id,train_id,updated_at) VALUES('a','owned-train',0)");
  const adapter = new SqlJsD1Database(sql); const db = adapter as unknown as D1Database;
  const call = (action: Parameters<typeof mutateGrowth>[1], key = crypto.randomUUID()) => mutateGrowth({ db, accountId: "a" }, action, key);
  const scalar = (query: string) => Number(sql.exec(query)[0]?.values[0]?.[0] ?? 0);
  return { sql, adapter, db, call, scalar };
}

describe("actual growth transactions", () => {
  it("retains all resource balances when a multi-resource purchase is unaffordable", async () => {
    const { sql, call, scalar } = await setup();
    sql.run("UPDATE account_currencies SET balance=0 WHERE currency='SPECIAL_CORE'");
    await expect(call({ kind: "PROMOTION", economyVersion: version, targetId: "owned0" })).rejects.toMatchObject({ code: "insufficient_balance" });
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(10000);
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='TACTICAL_DATA'")).toBe(10000);
    expect(scalar("SELECT promotion FROM account_operators WHERE operator_id='owned0'")).toBe(0);
    expect(scalar("SELECT COUNT(*) FROM currency_ledger")).toBe(0);
  });
  it("spends exactly70000 to max five cores and preserves existing high levels", async () => {
    const { call, sql, scalar } = await setup(70003);
    await call({ kind: "CORE_SYNC", economyVersion: version });
    expect(scalar("SELECT MIN(trained_level) FROM operator_training_core_slots")).toBe(50);
    expect(scalar("SELECT SUM(invested_operator_exp) FROM operator_training_core_slots")).toBe(70000);
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(3);
    sql.run("UPDATE operator_training_core_slots SET trained_level=60,invested_operator_exp=18000 WHERE slot_index=0");
    await call({ kind: "CORE_SYNC", economyVersion: version });
    expect(scalar("SELECT trained_level FROM operator_training_core_slots WHERE slot_index=0")).toBe(60);
    await expect(call({ ...level, slot: 5 })).rejects.toMatchObject({ code: "contract_violation" });
  });
  it("fills an empty slot only with an actually owned operator and rejects duplicate assignment", async () => {
    const { call, sql, scalar } = await setup();
    sql.run("DELETE FROM operator_training_core_slots WHERE slot_index=4");
    await expect(call({ kind: "CORE_SYNC", economyVersion: version })).rejects.toMatchObject({ code: "training_core_incomplete" });
    await expect(call({ kind: "CORE_REPLACE", economyVersion: version, slot: 4, operatorId: "owned0" })).rejects.toMatchObject({ code: "operator_already_in_core" });
    await call({ kind: "CORE_REPLACE", economyVersion: version, slot: 4, operatorId: "owned5" });
    expect(scalar("SELECT COUNT(*) FROM operator_training_core_slots")).toBe(5);
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(1000);
  });
  it("serves the authenticated HTTP contract and rejects injected route fields", async () => {
    const { db, scalar } = await setup();
    const app = new Hono<{ Bindings: Env }>();
    registerGrowthRoutes(app, async () => ({ accountId: "a" }));
    const request = (body: object) => app.request("/v1/operators/training-core/0/level-up", {
      method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() }, body: JSON.stringify(body),
    }, { DB: db } as Env);
    expect((await request({ economyVersion: version, targetLevel: 2, cost: 0 })).status).toBe(400);
    expect((await request({ economyVersion: version, targetLevel: 2, slot: 1 })).status).toBe(400);
    expect((await request({ economyVersion: version, targetLevel: 2 })).status).toBe(200);
    expect(scalar("SELECT trained_level FROM operator_training_core_slots WHERE slot_index=0")).toBe(2);
  });
  it("rejects a version activation race and all open-run statuses", async () => {
    const first = await setup();
    first.adapter.beforeBatch = () => { first.adapter.beforeBatch = undefined; first.sql.run("DELETE FROM balance_config_active"); };
    await expect(first.call(level)).rejects.toMatchObject({ code: "economy_unconfigured" });
    expect(first.scalar("SELECT COUNT(*) FROM growth_receipts")).toBe(0);
    for (const status of ["ACTIVE", "FINISH_PENDING", "QUARANTINED"]) {
      const { sql, call, scalar } = await setup();
      sql.run("INSERT INTO account_mutations VALUES('r','a',1,'run',NULL,0)");
      sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('r','a','r',1,?,0)", [status]);
      await expect(call({ kind: "CORE_REPLACE", economyVersion: version, slot: 0, operatorId: "owned5" })).rejects.toMatchObject({ code: "growth_open_run" });
      expect(scalar("SELECT COUNT(*) FROM growth_receipts")).toBe(0);
    }
  });
  it("debits EXP, records invested cost and replays before stale active config", async () => {
    const { call, sql, scalar } = await setup();
    const receipt = await call(level, "same");
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(960);
    expect(scalar("SELECT invested_operator_exp FROM operator_training_core_slots WHERE slot_index=0")).toBe(40);
    sql.run("DELETE FROM balance_config_active");
    expect(await call(level, "same")).toEqual(receipt);
    await expect(call({ ...level, targetLevel: 3 }, "same")).rejects.toMatchObject({ code: "idempotency_conflict" });
    expect(scalar("SELECT COUNT(*) FROM growth_receipts")).toBe(1);
  });
  it("syncs lowest slot with deterministic ties and refunds historical investment without reinvestment", async () => {
    const { call, sql, scalar } = await setup(95);
    await call({ kind: "CORE_SYNC", economyVersion: version });
    expect(scalar("SELECT trained_level FROM operator_training_core_slots WHERE slot_index=0")).toBe(2);
    expect(scalar("SELECT trained_level FROM operator_training_core_slots WHERE slot_index=1")).toBe(2);
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(15);
    sql.run("UPDATE operator_training_core_slots SET invested_operator_exp=777 WHERE slot_index=0");
    await call({ kind: "CORE_REPLACE", economyVersion: version, slot: 0, operatorId: "owned5" });
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(792);
    expect(scalar("SELECT trained_level FROM operator_training_core_slots WHERE slot_index=0")).toBe(1);
    expect(scalar("SELECT invested_operator_exp FROM operator_training_core_slots WHERE slot_index=0")).toBe(0);
  });
  it("mutates owned train, skill and promotion with actual resource ledgers", async () => {
    const { call, scalar } = await setup();
    await call({ kind: "TRAIN", economyVersion: version, targetId: "owned-train" });
    await call({ kind: "SKILL", economyVersion: version, targetId: "owned0" });
    await call({ kind: "PROMOTION", economyVersion: version, targetId: "owned0" });
    expect(scalar("SELECT level FROM account_train")).toBe(2);
    expect(scalar("SELECT skill_training FROM account_operators WHERE operator_id='owned0'")).toBe(2);
    expect(scalar("SELECT promotion FROM account_operators WHERE operator_id='owned0'")).toBe(1);
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(9000);
    expect(scalar("SELECT COUNT(*) FROM currency_ledger")).toBe(7);
  });
  it("serializes duplicates and competing spends without overspending", async () => {
    const { call, scalar } = await setup(60);
    const duplicates = await Promise.all([call(level, "same"), call(level, "same")]);
    expect(duplicates[0]).toEqual(duplicates[1]);
    await expect(call({ ...level, slot: 1 })).rejects.toMatchObject({ code: "insufficient_balance" });
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(20);
    const second = await setup(60);
    const outcomes = await Promise.allSettled([second.call(level), second.call({ ...level, slot: 1 })]);
    expect(outcomes.filter(o => o.status === "fulfilled")).toHaveLength(1);
    expect(second.scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(20);
  });
  it("rejects unknown targets and unavailable catalogs without changing balances", async () => {
    const { call, scalar } = await setup();
    for (const kind of ["TRAIN", "SKILL", "PROMOTION", "EQUIPMENT", "RESEARCH"] as const) {
      await expect(call({ kind, economyVersion: version, targetId: "unknown" })).rejects.toMatchObject({ code: "unknown_growth_target" });
    }
    await expect(call({ kind: "CORE_REPLACE", economyVersion: version, slot: 0, operatorId: "unknown" })).rejects.toMatchObject({ code: "unknown_growth_target" });
    expect(scalar("SELECT COUNT(*) FROM growth_receipts")).toBe(0);
    expect(scalar("SELECT SUM(balance) FROM account_currencies")).toBe(41000);
  });
  it("rejects stale versions and open runs, including an open-run race", async () => {
    const { call, sql, adapter, scalar } = await setup();
    await expect(call({ ...level, economyVersion: "old" })).rejects.toMatchObject({ code: "economy_version_mismatch" });
    const insertRun = () => {
      sql.run("INSERT INTO account_mutations VALUES('run-m','a',1,'run',NULL,0)");
      sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at) VALUES('run','a','run-m',1,'ACTIVE',0)");
    };
    adapter.beforeBatch = () => { adapter.beforeBatch = undefined; insertRun(); };
    await expect(call(level)).rejects.toMatchObject({ code: "growth_open_run" });
    expect(scalar("SELECT COUNT(*) FROM growth_receipts")).toBe(0);
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(1000);
  });
  it("rolls back every write on downstream failure", async () => {
    const { call, sql, scalar } = await setup();
    sql.run("CREATE TRIGGER reject_receipt BEFORE INSERT ON growth_receipts BEGIN SELECT RAISE(ABORT,'injected'); END");
    await expect(call(level)).rejects.toThrow("injected");
    expect(scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(1000);
    expect(scalar("SELECT trained_level FROM operator_training_core_slots WHERE slot_index=0")).toBe(1);
    expect(scalar("SELECT COUNT(*) FROM account_mutations")).toBe(0);
  });
  it("enforces immutable storage and verifies stored hash", async () => {
    const { sql, db } = await setup();
    expect((await readEconomyBinding(db))?.version).toBe(version);
    expect(() => sql.run("UPDATE economy_versions SET config_json='{}'")).toThrow();
    expect(() => sql.run("DELETE FROM economy_version_bindings")).toThrow();
    expect(() => sql.run("INSERT OR REPLACE INTO economy_version_bindings VALUES(1,?)", [version])).toThrow();
    expect(() => sql.run("INSERT OR REPLACE INTO economy_versions VALUES(?,?,?,0)", [version, "0".repeat(64), "{}"])).toThrow();
    sql.run("DROP TRIGGER economy_versions_no_update");
    sql.run("UPDATE economy_versions SET sha256=?", ["0".repeat(64)]);
    await expect(readEconomyBinding(db)).rejects.toMatchObject({ code: "economy_binding_invalid" });
  });
});

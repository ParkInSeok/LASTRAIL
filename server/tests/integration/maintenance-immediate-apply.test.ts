import { expect, it } from "vitest";
import { writeFile } from "node:fs/promises";
import { runtimeFixture } from "../helpers/maintenance-runtime-fixture";
import { MaintenanceImmediateReview } from "../../apps/admin-api/src/maintenance-immediate-review";
import { MaintenanceRuntimePolicyStore } from "../../apps/admin-api/src/maintenance-runtime-policy-store";
import { MaintenancePlanStore } from "../../apps/admin-api/src/maintenance-plan-store";
import { maintenanceAdmissionStatement, maintenanceLoginStatement } from "../../apps/game-api/src/maintenance-admission";
import { setup } from "../helpers/departure-fixture";
import admin from "../../apps/admin-api/src/index";
import game from "../../apps/game-api/src/index";
import initSqlJs from "sql.js";
import { readFileSync, readdirSync } from "node:fs";
import { SqlJsD1Database } from "../helpers/growth-d1";
const context = { environment: "DEV" as const, applicationId: "LASTRAIL" };
const body = { expectedRevision: 1, gameMode: "MAINTENANCE", webMode: "NORMAL", reason: "test" };
async function make() {
  const f = await runtimeFixture(); await f.store.transition(f.actor, { command: "INITIALIZE", expectedRevision: null }, "test", "init");
  return { ...f, current: new MaintenanceImmediateReview(f.db, context) };
}
it("atomically applies explicit axes without draft/times, reads them and gates new issuance", async () => {
  const f = await make(), draftCount = f.count("maintenance_plan_revisions");
  const result = await f.store.changeImmediately(f.actor, body, "apply1");
  expect(result).toMatchObject({ applied: true, policyRevision: 2, gameMode: "MAINTENANCE", webMode: "NORMAL" });
  expect(await f.current.read(f.actor)).toMatchObject({ storedState: "IMMEDIATE", gameMode: "MAINTENANCE", webMode: "NORMAL",
    gameLoginAllowed: false, webLoginAllowed: false, activeReservation: null });
  expect(f.count("maintenance_plan_revisions")).toBe(draftCount);
  const row = f.sql.exec("SELECT starts_at,drain_starts_at,maintenance_id,draft_revision FROM maintenance_runtime_revisions WHERE revision=2")[0]!.values[0];
  expect(row).toEqual([null, null, null, null]);
  for (const channel of ["GAME", "WEB"] as const) await expect(f.db.batch([maintenanceLoginStatement(f.db, context, channel)]))
    .rejects.toThrow("maintenance_new_request_blocked");
  await expect(f.db.batch([maintenanceAdmissionStatement(f.db, context)])).rejects.toThrow("maintenance_new_request_blocked");
  await writeFile("D:/Project/LASTRAIL/QA/CommanderS1-20260922/maintenance-immediate-apply-observed-local-20261001.json",
    JSON.stringify({ environment: "LOCAL_SQL_TEST_NOT_REMOTE", command: body, receipt: result, current: await f.current.read(f.actor),
      sourceTimes: row, draftCount, appliedModes: f.sql.exec("SELECT * FROM maintenance_immediate_modes")[0] }, null, 2));
});
it("changes DRAINING and NORMAL explicitly without automatic completion and rejects unsupported WEB choices", async () => {
  const f = await make(); await f.store.changeImmediately(f.actor, { ...body, gameMode: "DRAINING" }, "drain");
  for (const channel of ["GAME", "WEB"] as const) await f.db.batch([maintenanceLoginStatement(f.db, context, channel)]);
  await expect(f.db.batch([maintenanceAdmissionStatement(f.db, context)])).rejects.toThrow();
  await f.store.changeImmediately(f.actor, { ...body, expectedRevision: 2, gameMode: "NORMAL" }, "normal");
  await f.db.batch([maintenanceAdmissionStatement(f.db, context)]);
  expect(await f.current.read(f.actor)).toMatchObject({ gameMode: "NORMAL", policyRevision: 3, automaticCompletion: false });
  for (const webMode of ["READ_ONLY", "MAINTENANCE"]) await expect(f.store.changeImmediately(f.actor, { ...body, expectedRevision: 3, webMode }, webMode))
    .rejects.toMatchObject({ code: "maintenance_mode_unsupported" });
  expect(f.count("maintenance_runtime_revisions")).toBe(3);
});
it("recovers a lost response, replays stale revision keys and rejects changed payload", async () => {
  const f = await make(), batch = f.db.batch.bind(f.db); let fail = true;
  f.db.batch = async statements => { const result = await batch(statements); if (fail) { fail = false; throw new Error("response lost"); } return result; };
  const first = await f.store.changeImmediately(f.actor, body, "lost");
  await f.store.changeImmediately(f.actor, { ...body, gameMode: "NORMAL", expectedRevision: 2 }, "next");
  expect(await f.store.changeImmediately(f.actor, body, "lost")).toEqual(first);
  await expect(f.store.changeImmediately(f.actor, { ...body, reason: "changed" }, "lost")).rejects.toMatchObject({ code: "idempotency_conflict" });
  expect(f.count("maintenance_immediate_modes")).toBe(2);
});
it("rejects revision races and rolls back every write when audit fails", async () => {
  const f = await make();
  const results = await Promise.allSettled([f.store.changeImmediately(f.actor, body, "race1"), f.store.changeImmediately(f.actor, body, "race2")]);
  expect(results.filter(r => r.status === "fulfilled")).toHaveLength(1);
  expect(f.count("maintenance_runtime_revisions")).toBe(2);
  const g = await make(); g.sql.run("CREATE TRIGGER fail_immediate_audit BEFORE INSERT ON admin_audit_logs WHEN NEW.action='MAINTENANCE_IMMEDIATE_CHANGE' BEGIN SELECT RAISE(ABORT,'audit failed'); END");
  await expect(g.store.changeImmediately(g.actor, body, "fail")).rejects.toThrow();
  expect(g.count("maintenance_immediate_modes")).toBe(0); expect(g.count("maintenance_runtime_revisions")).toBe(1);
  expect(g.count("maintenance_runtime_receipts")).toBe(1);
});
it("checks authority and reservation conflict again at commit", async () => {
  const f = await make(), batch = f.db.batch.bind(f.db);
  f.sql.run("INSERT INTO admin_principals VALUES('backup','backup','OWNER','ACTIVE',NULL,0,0)");
  f.db.batch = async statements => { f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='owner'"); return batch(statements); };
  await expect(f.store.changeImmediately(f.actor, body, "revoked")).rejects.toMatchObject({ code: "ADMIN_PERMISSION_DENIED" });
  expect(f.count("maintenance_immediate_modes")).toBe(0);
  const g = await make(); await g.store.transition(g.actor, { command: "SCHEDULE", expectedRevision: 1, maintenanceId: "m", draftRevision: 1 }, "test", "scheduled");
  await expect(g.store.changeImmediately(g.actor, { ...body, expectedRevision: 2 }, "blocked")).rejects.toMatchObject({ code: "maintenance_schedule_conflict" });
  expect(g.count("maintenance_immediate_modes")).toBe(0);
});
it("login issuance sees a transition committed after guard preparation and rolls back its own work", async () => {
  const f = await make(); f.sql.run("CREATE TABLE issuance_probe(id TEXT)");
  const guard = maintenanceLoginStatement(f.db, context, "WEB"); await f.store.changeImmediately(f.actor, body, "block");
  await expect(f.db.batch([f.db.prepare("INSERT INTO issuance_probe VALUES('new')"), guard])).rejects.toThrow("maintenance_new_request_blocked");
  expect(f.count("issuance_probe")).toBe(0);
});
it("retains existing run source/receipt and original START replay while blocking new requests", async () => {
  const f = await setup(true), actor = { id: "maintenance_test", role: "OWNER" as const };
  const preview = await (await f.preview()).json(), first = await f.start(preview.operationPreviewId, "old-start");
  expect(first.status).toBe(200); const receipt = await first.json(), before = f.sql.exec("SELECT * FROM runs");
  const store = new MaintenanceRuntimePolicyStore(f.env.DB, context); await store.changeImmediately(actor, body, "maintenance");
  expect(f.sql.exec("SELECT * FROM runs")).toEqual(before);
  expect(await (await f.start(preview.operationPreviewId, "old-start")).json()).toEqual(receipt);
  expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(20);
});
it("provides authenticated apply HTTP with original-key receipt and restricts unsupported axes", async () => {
  const f = await make(), env = { DB: f.db, ENVIRONMENT: "DEV", ADMIN_AUTH_MODE: "LOCAL_HEADER", ALLOWED_ADMIN_ORIGINS: "http://localhost:5173" } as unknown as Env;
  const post = (key: string | null, data: object = body) => admin.request("/v1/admin/maintenance/immediate-apply", { method: "POST",
    headers: { "Cf-Access-Subject": "owner", "Content-Type": "application/json", ...(key ? { "Idempotency-Key": key } : {}) }, body: JSON.stringify(data) }, env);
  expect((await post(null)).status).toBe(400);
  const first = await post("http"); expect(first.status).toBe(200); const result = await first.json();
  expect(await (await post("http")).json()).toEqual(result);
  expect((await post("http", { ...body, reason: "changed" })).status).toBe(409);
  expect((await post("unsupported", { ...body, expectedRevision: 2, webMode: "READ_ONLY" })).status).toBe(409);
  expect(await (await admin.request("/v1/admin/maintenance-runtime", { headers: { "Cf-Access-Subject": "owner" } }, env)).json())
    .toMatchObject({ contractVersion: "admin-maintenance-runtime-v2", policy: { state: "IMMEDIATE" } });
  expect(await (await game.request("/v1/public/maintenance/login", {}, env)).json()).toMatchObject({ policyRevision: 2,
    gameLoginAllowed: false, webLoginAllowed: false, startsAt: null });
});

it("upgrades 0048 rows/heads/receipts unchanged with foreign keys on and preserves immutability", async () => {
  const SQL = await initSqlJs(), sql = new SQL.Database();
  for (const name of readdirSync("packages/db/migrations").filter(n => n.endsWith(".sql") && n < "0098").sort())
    sql.run(readFileSync("packages/db/migrations/" + name, "utf8"));
  sql.run("PRAGMA foreign_keys=ON; INSERT INTO admin_principals VALUES('owner','owner','OWNER','ACTIVE',NULL,0,0)");
  const db = new SqlJsD1Database(sql) as unknown as D1Database, store = new MaintenanceRuntimePolicyStore(db, context);
  const actor = { id: "owner", role: "OWNER" as const };
  await store.transition(actor, { command: "INITIALIZE", expectedRevision: null }, "historic", "historic-init");
  await new MaintenancePlanStore(db, "DEV").command(actor, "historic", { command: "SAVE_DRAFT", environment: "DEV", expectedDraftRevision: null, reason: "historic",
    plan: { applicationId: "LASTRAIL", serviceScope: "GAME", webMode: null, title: "historic", summary: "historic", noticeId: null,
      startsAt: 2, drainStartsAt: 1, estimatedEndAt: 3, existingRunPolicy: "DRAIN_THEN_PRESERVE", statusFreshnessSeconds: 60, retryAfterSeconds: 15 } }, "historic-draft");
  await store.transition(actor, { command: "SCHEDULE", expectedRevision: 1, maintenanceId: "historic", draftRevision: 1 }, "historic", "historic-schedule");
  const tables = ["maintenance_runtime_revisions", "maintenance_runtime_heads", "maintenance_runtime_receipts", "admin_audit_logs", "maintenance_plan_revisions", "maintenance_plan_receipts"];
  const before = tables.map(t => sql.exec(`SELECT * FROM ${t}`));
  sql.run(readFileSync("packages/db/migrations/0098_maintenance_immediate_modes.sql", "utf8"));
  expect(tables.map(t => sql.exec(`SELECT * FROM ${t}`))).toEqual(before);
  expect(sql.exec("PRAGMA foreign_key_check")).toEqual([]);
  expect(() => sql.run("DELETE FROM maintenance_runtime_revisions")).toThrow();
  expect(() => sql.run("UPDATE maintenance_runtime_heads SET revision=1")).toThrow();
  await expect(store.changeImmediately(actor, { ...body, expectedRevision: 2 }, "conflict-after-upgrade")).rejects.toMatchObject({ code: "maintenance_schedule_conflict" });
  await store.transition(actor, { command: "CANCEL", expectedRevision: 2 }, "explicit cancellation", "historic-cancel");
  const receipt = await store.changeImmediately(actor, { ...body, expectedRevision: 3 }, "after-upgrade");
  expect(receipt.policyRevision).toBe(4); expect(sql.exec("PRAGMA foreign_key_check")).toEqual([]);
});

it("same-key races return one original result and a last-receipt failure rolls back policy/axes/audit", async () => {
  const f = await make(); const results = await Promise.all([f.store.changeImmediately(f.actor, body, "same"), f.store.changeImmediately(f.actor, body, "same")]);
  expect(results[0]).toEqual(results[1]); expect(f.count("maintenance_immediate_modes")).toBe(1);
  const g = await make(), beforeAudit = g.count("admin_audit_logs");
  g.sql.run("CREATE TRIGGER fail_immediate_receipt BEFORE INSERT ON maintenance_runtime_receipts WHEN NEW.request_key='fail-last' BEGIN SELECT RAISE(ABORT,'receipt failed'); END");
  await expect(g.store.changeImmediately(g.actor, body, "fail-last")).rejects.toThrow();
  expect(g.count("maintenance_immediate_modes")).toBe(0); expect(g.count("admin_audit_logs")).toBe(beforeAudit);
  expect(await g.current.read(g.actor)).toMatchObject({ policyRevision: 1, gameMode: "NORMAL" });
});

it("DB clock controls optional guidance without making it an automatic end", async () => {
  const f = await make(); let julian = 2440588.5; f.sql.create_function("julianday", (value: string) => value === "now" ? julian : NaN);
  const receipt = await f.store.changeImmediately(f.actor, { ...body, estimatedEndAt: 86401000 }, "clock");
  expect(receipt.appliedAt).toBe(86400000); julian = 2440589.5;
  expect(await f.current.read(f.actor)).toMatchObject({ gameMode: "MAINTENANCE", estimatedEndAt: 86401000, automaticCompletion: false });
  await expect(f.db.batch([maintenanceLoginStatement(f.db, context, "WEB")])).rejects.toThrow();
});

it.each(["SCHEDULE", "ACTIVATE"] as const)("retains the existing %s path after explicit immediate NORMAL", async command => {
  const f = await make(); await f.store.changeImmediately(f.actor, { ...body, gameMode: "NORMAL" }, "normal");
  expect(await f.store.transition(f.actor, { command, expectedRevision: 2, maintenanceId: "m", draftRevision: 1 }, "test", "legacy"))
    .toMatchObject({ policyRevision: 3 });
  const blocked = await make(); await blocked.store.changeImmediately(blocked.actor, body, "maintenance");
  await expect(blocked.store.transition(blocked.actor, { command, expectedRevision: 2, maintenanceId: "m", draftRevision: 1 }, "test", "implicit-clear"))
    .rejects.toMatchObject({ code: "maintenance_transition_conflict" });
});

it("retains a reservation committed between inspection and immediate write", async () => {
  const f = await make(), batch = f.db.batch.bind(f.db); let once = true;
  f.db.batch = async statements => {
    if (once) { once = false; await f.store.transition(f.actor, { command: "SCHEDULE", expectedRevision: 1, maintenanceId: "m", draftRevision: 1 }, "test", "reserved-first"); }
    return batch(statements);
  };
  await expect(f.store.changeImmediately(f.actor, body, "manual")).rejects.toMatchObject({ code: "maintenance_revision_conflict" });
  expect(f.count("maintenance_immediate_modes")).toBe(0);
  expect(await f.current.read(f.actor)).toMatchObject({ policyRevision: 2, storedState: "SCHEDULED", activeReservation: { maintenanceId: "m" } });
});

it("fails closed when unsupported immediate axes are restored with constraints bypassed", async () => {
  const f = await make(); await f.store.changeImmediately(f.actor, body, "maintenance");
  f.sql.run("DROP TRIGGER maintenance_immediate_no_update; PRAGMA ignore_check_constraints=ON; UPDATE maintenance_immediate_modes SET web_mode='READ_ONLY'; PRAGMA ignore_check_constraints=OFF;");
  await expect(f.current.read(f.actor)).rejects.toMatchObject({ code: "maintenance_policy_unavailable" });
  for (const channel of ["GAME", "WEB"] as const) await expect(f.db.batch([maintenanceLoginStatement(f.db, context, channel)]))
    .rejects.toThrow("maintenance_policy_unavailable");
  await expect(f.db.batch([maintenanceAdmissionStatement(f.db, context)])).rejects.toThrow("maintenance_policy_unavailable");
});

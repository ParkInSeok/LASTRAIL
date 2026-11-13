import { expect, it } from "vitest";
import { runtimeFixture } from "../helpers/maintenance-runtime-fixture";
import { MaintenanceImmediateReview } from "../../apps/admin-api/src/maintenance-immediate-review";
import admin from "../../apps/admin-api/src/index";
import { MaintenancePlanStore } from "../../apps/admin-api/src/maintenance-plan-store";
import { writeFile } from "node:fs/promises";

async function make() {
  const f = await runtimeFixture(); await f.store.transition(f.actor, { command: "INITIALIZE", expectedRevision: null }, "test", "init");
  const review = new MaintenanceImmediateReview(f.db, { environment: "DEV", applicationId: "LASTRAIL" });
  return { ...f, review };
}
const command = { expectedRevision: 1, gameMode: "MAINTENANCE", webMode: "NORMAL", reason: "test" };
it("reads current effective axes and reviews immediate changes without creating a fake schedule", async () => {
  const f = await make(); const current = await f.review.read(f.actor);
  expect(current).toMatchObject({ contractVersion: "admin-maintenance-current-state-v2", policyRevision: 1,
    gameMode: "NORMAL", webMode: "NORMAL", gameLoginAllowed: true, webLoginAllowed: true, activeReservation: null });
  const before = [f.count("maintenance_plan_revisions"), f.count("maintenance_runtime_revisions"), f.count("admin_audit_logs")];
  expect(await f.review.review(f.actor, command)).toMatchObject({ contractVersion: "admin-maintenance-immediate-review-v2",
    canApply: true, applied: false, requested: { gameMode: "MAINTENANCE", webMode: "NORMAL" },
    loginIfApplied: { gameLoginAllowed: false, webLoginAllowed: false }, automaticCompletion: false });
  expect([f.count("maintenance_plan_revisions"), f.count("maintenance_runtime_revisions"), f.count("admin_audit_logs")]).toEqual(before);
});
it("does not treat a missing policy as NORMAL and checks OWNER/revision/schema before review", async () => {
  const f = await runtimeFixture(), r = new MaintenanceImmediateReview(f.db, { environment: "DEV", applicationId: "LASTRAIL" });
  await expect(r.read(f.actor)).rejects.toMatchObject({ code: "maintenance_policy_unavailable" });
  await f.store.transition(f.actor, { command: "INITIALIZE", expectedRevision: null }, "test", "init");
  await expect(r.review({ ...f.actor, role: "VIEWER" }, command)).rejects.toMatchObject({ code: "ADMIN_PERMISSION_DENIED" });
  await expect(r.review(f.actor, { ...command, expectedRevision: 2 })).rejects.toMatchObject({ code: "maintenance_revision_conflict" });
  await expect(r.review(f.actor, { ...command, startsAt: 999 })).rejects.toMatchObject({ code: "contract_violation" });
});
it("rejects manual change over an active reservation and retains the explicit cancellation source", async () => {
  const f = await make(); await f.store.transition(f.actor, { command: "SCHEDULE", expectedRevision: 1, maintenanceId: "m", draftRevision: 1 }, "test", "schedule");
  expect(await f.review.read(f.actor)).toMatchObject({ activeReservation: { maintenanceId: "m", draftRevision: 1, policyRevision: 2 },
    gameMode: "MAINTENANCE", webMode: "NORMAL", gameLoginAllowed: false, webLoginAllowed: false });
  await expect(f.review.review(f.actor, { ...command, expectedRevision: 2 })).rejects.toMatchObject({ code: "maintenance_schedule_conflict" });
  await f.store.transition(f.actor, { command: "CANCEL", expectedRevision: 2 }, "test", "cancel");
  expect(await f.review.review(f.actor, { ...command, expectedRevision: 3 })).toMatchObject({ canApply: true });
});
it("estimated end is guidance and cannot auto-normalize current maintenance", async () => {
  const f = await make(); await f.store.transition(f.actor, { command: "ACTIVATE", expectedRevision: 1, maintenanceId: "m", draftRevision: 1 }, "test", "activate");
  expect(await f.review.read(f.actor)).toMatchObject({ gameMode: "MAINTENANCE", estimatedEndAt: 3, automaticCompletion: false });
});
it("checks live permission again when policy is read and preserves an unavailable projection", async () => {
  const f = await make(); f.sql.run("INSERT INTO admin_principals VALUES('backup','backup','OWNER','ACTIVE',NULL,0,0)");
  f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='owner'");
  await expect(f.review.read(f.actor)).rejects.toMatchObject({ code: "ADMIN_PERMISSION_DENIED" });
  await expect(f.review.review(f.actor, command)).rejects.toMatchObject({ code: "ADMIN_PERMISSION_DENIED" });
});

it("serves authenticated read/review HTTP and requires a key for the separate apply endpoint", async () => {
  const f = await make(), env = { DB: f.db, ENVIRONMENT: "DEV", ADMIN_AUTH_MODE: "LOCAL_HEADER", ALLOWED_ADMIN_ORIGINS: "http://localhost:5173" } as unknown as Env;
  const headers = { "Cf-Access-Subject": "owner", "Content-Type": "application/json" };
  const current = await admin.request("/v1/admin/maintenance/current-state", { headers }, env);
  expect(current.status).toBe(200); expect(current.headers.get("Cache-Control")).toBe("no-store");
  const reviewed = await admin.request("/v1/admin/maintenance/immediate-review", { method: "POST", headers, body: JSON.stringify(command) }, env);
  expect(reviewed.status).toBe(200); const result = await reviewed.json(); expect(result).toMatchObject({ canApply: true, applied: false });
  expect((await admin.request("/v1/admin/maintenance/immediate-review", { method: "POST", headers, body: "{" }, env)).status).toBe(400);
  f.sql.run("INSERT INTO admin_principals VALUES('viewer','viewer','VIEWER','ACTIVE',NULL,0,0)");
  expect((await admin.request("/v1/admin/maintenance/immediate-review", { method: "POST", headers: { ...headers, "Cf-Access-Subject": "viewer" }, body: JSON.stringify(command) }, env)).status).toBe(403);
  expect((await admin.request("/v1/admin/maintenance/current-state?environment=PRD", { headers }, env)).status).toBe(400);
  expect((await admin.request("/v1/admin/maintenance/immediate-apply", { method: "POST", headers, body: JSON.stringify(command) }, env)).status).toBe(400);
  await writeFile("D:/Project/LASTRAIL/QA/CommanderS1-20260922/maintenance-immediate-observed-local-v2-20261001.json",
    JSON.stringify({ environment: "LOCAL_SQL_TEST_AUTHENTICATED_HANDLER_NOT_REMOTE", current: await current.json(), review: result,
      policyRowsAfterReview: f.count("maintenance_runtime_revisions"), realApplyEndpoint: "SUPPORTED_GAME_AXES_WITH_WEB_NORMAL_ONLY" }, null, 2));
});

it("keeps stored WEB mode separate from unverified enforcement", async () => {
  const f = await make(), plan = new MaintenancePlanStore(f.db, "DEV"), draft = (await plan.read("m"))!;
  await plan.command(f.actor, "web", { command: "SAVE_DRAFT", environment: "DEV", expectedDraftRevision: null, reason: "test",
    plan: { ...draft.document, serviceScope: "GAME_AND_WEB", webMode: "READ_ONLY" } }, "web-draft");
  await f.store.transition(f.actor, { command: "ACTIVATE", expectedRevision: 1, maintenanceId: "web", draftRevision: 1 }, "test", "web-active");
  expect(await f.review.read(f.actor)).toMatchObject({ webMode: null, policyWebMode: "READ_ONLY", webModeStatus: "ENFORCEMENT_UNVERIFIED",
    capabilities: { canApply: false, existingWebModeControl: false } });
});

// Local-only browser smoke: real handlers, ephemeral SQLite, no remote bindings.
import { createServer } from "node:http";
import { createRequire } from "node:module";
import { readFileSync, readdirSync } from "node:fs";
import { resolve, extname, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { randomUUID } from "node:crypto";
import assert from "node:assert/strict";
import gameApi from "../../../../../server/apps/game-api/src/index.ts";
import adminApi from "../../../../../server/apps/admin-api/src/index.ts";
import { rolePermissions } from "../../../packages/admin-contracts/src/index.ts";
import { seedGameReadEvidence } from "./local-game-fixtures.ts";
import { deploymentBucket } from "./local-deployment-fixtures.ts";
import { mapDeployment } from "../src/shared/release/serverDeployment.ts";
import { seedRunVersionDisplayCases } from "./local-run-version-fixtures.ts";
import { localVersionEditBucket } from "./local-version-edit-fixtures.ts";

const root = fileURLToPath(new URL("../../../../../server/", import.meta.url));
const versionEditMode = process.argv.includes("--version-edits");
const deploymentMode = versionEditMode || process.argv.includes("--deployments");
const supportErrorsOnly = deploymentMode || process.argv.includes("--support-errors");
const require = createRequire(resolve(root, "package.json"));
const SQL = await require("sql.js")();
const db = new SQL.Database();
db.run("PRAGMA foreign_keys = ON");
const migrations = resolve(root, "packages/db/migrations");
for (const name of readdirSync(migrations).filter(n => /^\d+_.+\.sql$/.test(n)).sort()) {
  db.run(readFileSync(resolve(migrations, name), "utf8"));
}
class Statement {
  values: unknown[] = [];
  constructor(readonly sql: string) {}
  bind(...values: unknown[]) { this.values = values; return this; }
  async all() {
    const statement = db.prepare(this.sql);
    try {
      statement.bind(this.values);
      const results = [];
      while (statement.step()) results.push(statement.getAsObject());
      return { results };
    } finally { statement.free(); }
  }
  async first() { return (await this.all()).results[0] ?? null; }
  run() { db.run(this.sql, this.values); return { success: true, meta: { changes: db.getRowsModified() } }; }
}
const d1 = {
  prepare: (sql: string) => new Statement(sql),
  async batch(statements: Statement[]) {
    db.run("BEGIN");
    try {
      statements.forEach(statement => statement.run());
      db.run("COMMIT");
      return statements.map(() => ({ success: true }));
    } catch (error) { db.run("ROLLBACK"); throw error; }
  }
};
const now = Date.now();
db.run("INSERT INTO admin_principals (id,access_subject,role,status,display_name,created_at,updated_at) VALUES (?,?,?,?,?,?,?)",
  ["local-evidence-owner", "local-evidence-subject", "OWNER", "ACTIVE", "LOCAL Synthetic Owner", now, now]);
const localVersionPolicy = { version: "1.0.2-local", versionCode: "200", minimumVersionCode: "90", contentRelease: { status: "APPROVED", releaseId: "local-content", contentVersion: "local-content-1", catalogUrl: "https://example.invalid/local-catalog.json", catalogHash: "sha256:" + "a".repeat(64), minimumVersionCode: "90", maximumVersionCode: "200" } };
const gameEnv = { ENVIRONMENT: "DEV", DB: d1, AUTH_SIGNING_KEY_CURRENT: randomUUID() + randomUUID(), CF_VERSION_METADATA: { id: "local-runtime-start" }, CLIENT_ADMISSION_MODE: supportErrorsOnly ? "DISABLED" : "ENFORCED", VERSION_BUCKET: { get: async () => ({ json: async () => localVersionPolicy }), put: async () => { throw new Error("Read-only local version policy"); } } };
const adminEnv = { ENVIRONMENT: deploymentMode ? "DEV" : "LOCAL", DB: d1, ADMIN_AUTH_MODE: "LOCAL_HEADER", ALLOWED_ADMIN_ORIGINS: "http://127.0.0.1:4174", ...(deploymentMode ? { VERSION_BUCKET: versionEditMode ? localVersionEditBucket(localVersionPolicy) : deploymentBucket(localVersionPolicy) } : {}) };
const dist = fileURLToPath(new URL("../dist/", import.meta.url));
const base = "http://127.0.0.1:4174";
const types: Record<string, string> = { ".js": "text/javascript", ".css": "text/css", ".html": "text/html", ".svg": "image/svg+xml", ".png": "image/png" };
const server = createServer(async (incoming, outgoing) => {
  try {
    const url = new URL(incoming.url ?? "/", base);
    if (url.pathname.startsWith("/v1/") || url.pathname.startsWith("/api/")) {
      const chunks = [];
      for await (const chunk of incoming) chunks.push(chunk);
      const body = Buffer.concat(chunks);
      const headers = new Headers();
      for (const [name, value] of Object.entries(incoming.headers)) if (typeof value === "string") headers.set(name, value);
      const admin = url.pathname.startsWith("/api/");
      const localVersionWrite = versionEditMode && url.pathname === "/api/versions/export" && incoming.method === "POST";
      if (admin && incoming.method !== "GET" && !localVersionWrite) { outgoing.writeHead(405); outgoing.end(); return; }
      const bootstrap = url.pathname === "/api/bootstrap";
      if (admin) {
        url.pathname = bootstrap ? "/v1/admin/me" : url.pathname.replace(/^\/api/, "/v1/admin");
        headers.set("Cf-Access-Subject", "local-evidence-subject");
      }
      const request = new Request(url, { method: incoming.method, headers, ...(body.length ? { body } : {}) });
      let response = await (admin ? adminApi : gameApi).fetch(request, (admin ? adminEnv : gameEnv) as never);
      if (bootstrap && response.ok) {
        const { principal } = await response.json() as any;
        response = Response.json({ environment: "local", admin: { id: principal.id, displayName: principal.displayName, role: principal.role, permissions: rolePermissions[principal.role as keyof typeof rolePermissions] } });
      }
      outgoing.writeHead(response.status, { "content-type": response.headers.get("content-type") ?? "application/json" });
      outgoing.end(Buffer.from(await response.arrayBuffer()));
      console.log(`${incoming.method} ${url.pathname}${url.search} ${response.status}`);
      return;
    }
    const target = resolve(dist, "." + decodeURIComponent(url.pathname));
    if (target !== resolve(dist) && !target.startsWith(dist.endsWith(sep) ? dist : dist + sep)) { outgoing.writeHead(403); outgoing.end(); return; }
    let bytes: Buffer;
    let type = types[extname(target)] ?? "application/octet-stream";
    try { bytes = readFileSync(target); } catch { bytes = readFileSync(resolve(dist, "index.html")); type = "text/html"; }
    outgoing.writeHead(200, { "content-type": type }); outgoing.end(bytes);
  } catch (error) {
    console.error(error instanceof Error ? error.message : "Local harness failure");
    outgoing.writeHead(500, { "content-type": "application/json" }); outgoing.end('{"errorCode":"local_harness_failure"}');
  }
});
await new Promise<void>((yes, no) => { server.once("error", no); server.listen(4174, "127.0.0.1", yes); });
async function post(path: string, payload?: unknown, token?: string, key = randomUUID(), extraHeaders: Record<string, string> = {}) {
  const response = await fetch(base + path, { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": key, "X-LastRail-App-Version-Code": "100", "X-LastRail-App-Platform": "android", "X-LastRail-App-Channel": "internal", ...extraHeaders, ...(token ? { Authorization: `Bearer ${token}` } : {}) }, ...(payload ? { body: JSON.stringify(payload) } : {}) });
  assert.equal(response.status, 200, `${path}: HTTP ${response.status}`);
  return response.json() as Promise<any>;
}
try {
  if (deploymentMode) {
    for (const service of ["game-api", "admin-api"] as const) {
      const pointer = await fetch(base + `/api/server-deployments/current?service=${service}`);
      assert.equal(pointer.status, 200);
      const current = mapDeployment(await pointer.json(), service, "current");
      const response = await fetch(base + `/api/server-deployments/history/${current.serverVersion}?service=${service}`);
      assert.equal(response.status, 200);
      const history = mapDeployment(await response.json(), service, "history", current.serverVersion);
      assert.equal(history.pointerStatus, service === "admin-api" ? "DEGRADED" : "CURRENT");
    }
    for (const [path, status] of [["current?service=async-worker", 404], ["history/missing?service=game-api", 404], ["history/invalid?service=game-api", 502], ["current?service=game-api&environment=PRD", 400]] as const) {
      assert.equal((await fetch(base + "/api/server-deployments/" + path)).status, status);
    }
    console.log("Deployment HTTP assertions PASS: current/history, degraded, absent, invalid, cross-env rejected; fake local bucket only");
  }
  const guest = await post("/v1/auth/guest");
  const time = new Date().toISOString();
  const ticket = await post("/v1/support/tickets", { category: "BUG_OTHER", body: "LOCAL HTTP synthetic support evidence <b>plain text</b>", appVersion: "1.0.0-local", appVersionCode: "42", clientComposedAt: time, clientSubmittedAt: time }, guest.accessToken);
  await post("/v1/client-errors", { events: [{ kind: "UNITY_EXCEPTION", occurredAt: time, occurrenceCount: 1, appVersion: "1.0.0-local", appVersionCode: "42", scope: "AUTHENTICATED_CURRENT_SESSION" }] }, guest.accessToken);
  const list = await (await fetch(base + "/api/support/tickets")).json() as any;
  assert.equal(list.items[0].ticketId, ticket.ticketId);
  const detail = await (await fetch(base + "/api/support/tickets/" + ticket.ticketId)).json() as any;
  assert.equal(detail.ticket.body, "LOCAL HTTP synthetic support evidence <b>plain text</b>");
  for (const [code, count] of [["42", 1], ["43", 0]] as const) {
    const logs = await (await fetch(base + "/api/ops/error-logs?category=frontend&appVersion=1.0.0-local&appVersionCode=" + code)).json() as any;
    assert.equal(logs.items.length, count);
  }
  // Extra synthetic rows exercise pagination without hitting player ingest limits.
  const original = (await d1.prepare("SELECT * FROM ops_error_logs LIMIT 1").first())!;
  for (const category of ["server", "frontend"]) {
    for (let i = 0; i < 26; i++) {
      const row = { ...original, id: `local_${category}_${i.toString().padStart(2, "0")}` };
      if (category === "server") Object.assign(row, { sanitized_context_json: "{}", error_code: "local_server_evidence", message_summary: "Synthetic legacy server pagination evidence" });
      const fields = Object.keys(row);
      db.run(`INSERT INTO ops_error_logs (${fields.join(",")}) VALUES (${fields.map(() => "?").join(",")})`, Object.values(row));
    }
  }
  for (const category of ["server", "frontend"]) {
    const ids = new Set<string>();
    let cursor: string | null = null;
    do {
      const query = new URLSearchParams({ category, limit: "25", ...(cursor ? { cursor } : {}) });
      const page = await (await fetch(base + "/api/ops/error-logs?" + query)).json() as any;
      for (const row of page.items) {
        assert.equal(row.errorCode, category === "server" ? "local_server_evidence" : "frontend_unity_exception");
        assert.equal(ids.has(row.id), false);
        ids.add(row.id);
      }
      cursor = page.nextCursor;
    } while (cursor);
    assert.equal(ids.size, category === "server" ? 26 : 27);
  }
  if (!supportErrorsOnly) {
  seedGameReadEvidence(db, guest.account.id);
  const started = await post("/v1/runs/start", { operationPreviewId: "local-read-preview" }, guest.accessToken, "local-run-start", { "X-LastRail-App-Version": "1.0.0-local", "X-LastRail-App-Version-Code": "100" });
  assert.equal(started.accepted, true);
  const unfinished = await (await fetch(base + "/api/runs/" + started.runId)).json() as any;
  assert.equal(unfinished.runFinishVersionSnapshotStatus, "NOT_FINISHED");
  gameEnv.CF_VERSION_METADATA.id = "local-runtime-finish";
  const finishPayload = { runId: started.runId, currentStage: "Complete", currentWaveNumber: 0, completedWaveCount: 15, outcome: "VICTORY", runtimeStateJson: "{}", tacticSelections: [], clientCompletedAt: new Date().toISOString() };
  const finished = await post("/v1/runs/" + started.runId + "/finish", finishPayload, guest.accessToken, "local-run-finish", { "X-LastRail-App-Version": "1.0.1-local", "X-LastRail-App-Version-Code": "101" });
  assert.equal(finished.accepted, true);
  const detailRun = await (await fetch(base + "/api/runs/" + started.runId)).json() as any;
  assert.equal(detailRun.runStartVersionSnapshot.clientInstalled.versionCode, "100");
  assert.equal(detailRun.runFinishVersionSnapshot.clientInstalled.versionCode, "101");
  assert.equal(detailRun.runStartVersionSnapshot.serverAcceptedDeployment.runtimeDeploymentId, "local-runtime-start");
  assert.equal(detailRun.runFinishVersionSnapshot.serverAcceptedDeployment.runtimeDeploymentId, "local-runtime-finish");
  await post("/v1/runs/" + started.runId + "/finish", finishPayload, guest.accessToken, "local-run-finish", { "X-LastRail-App-Version-Code": "102" });
  const replayRun = await (await fetch(base + "/api/runs/" + started.runId)).json() as any;
  assert.deepEqual(replayRun.runFinishVersionSnapshot, detailRun.runFinishVersionSnapshot);
  if (process.argv.includes("--run-version-cases")) await seedRunVersionDisplayCases(db, d1, started.runId);
  const pull = await post("/v1/gacha/banners/local-read-banner/pulls", { pullCount: 10 }, guest.accessToken, "local-pull");
  assert.equal(pull.accepted, true);
  const batch = await (await fetch(base + "/api/gacha/pull-batches/" + pull.batchId)).json() as any;
  assert.equal(batch.item.results.length, 10);
  assert.equal(batch.item.causeEvidence.source, "gacha_pull_batch_server_records");
  const replayPull = await post("/v1/gacha/banners/local-read-banner/pulls", { pullCount: 10 }, guest.accessToken, "local-pull");
  assert.equal(replayPull.resultStatus, "DUPLICATE_REPLAY");
  assert.equal(replayPull.batchId, pull.batchId);
  const afterReplay = await (await fetch(base + "/api/gacha/pull-batches/" + pull.batchId)).json() as any;
  assert.deepEqual(afterReplay.item.results, batch.item.results);
  console.log(JSON.stringify({ ready: true, base, accountId: guest.account.id, ticketId: ticket.ticketId, runId: started.runId, batchId: pull.batchId, networkAssertions: "PASS", storage: "ephemeral sql.js; actual server handlers; synthetic local principal; NOT Cloudflare Access/Pages E2E" }));
  } else {
    console.log(JSON.stringify({ ready: true, base, scope: "support-errors", runGachaChecks: "NOT_RUN", networkAssertions: "PASS", accountId: guest.account.id, ticketId: ticket.ticketId }));
  }
} catch (error) { server.close(); db.close(); throw error; }
process.on("SIGINT", () => server.close(() => { db.close(); process.exit(0); }));
process.on("SIGTERM", () => server.close(() => { db.close(); process.exit(0); }));

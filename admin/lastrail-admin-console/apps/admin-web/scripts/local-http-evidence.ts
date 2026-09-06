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

const root = fileURLToPath(new URL("../../../../../server/", import.meta.url));
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
  run() { db.run(this.sql, this.values); }
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
const gameEnv = { ENVIRONMENT: "DEV", DB: d1, AUTH_SIGNING_KEY_CURRENT: randomUUID() + randomUUID() };
const adminEnv = { ENVIRONMENT: "LOCAL", DB: d1, ADMIN_AUTH_MODE: "LOCAL_HEADER", ALLOWED_ADMIN_ORIGINS: "http://127.0.0.1:4174" };
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
      if (admin && incoming.method !== "GET") { outgoing.writeHead(405); outgoing.end(); return; }
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
async function post(path: string, payload?: unknown, token?: string, key = randomUUID()) {
  const response = await fetch(base + path, { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": key, ...(token ? { Authorization: `Bearer ${token}` } : {}) }, ...(payload ? { body: JSON.stringify(payload) } : {}) });
  assert.equal(response.status, 200, `${path}: HTTP ${response.status}`);
  return response.json() as Promise<any>;
}
try {
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
  console.log(JSON.stringify({ ready: true, base, accountId: guest.account.id, ticketId: ticket.ticketId, networkAssertions: "PASS", storage: "ephemeral sql.js; actual server handlers; synthetic local principal; NOT Cloudflare Access/Pages E2E" }));
} catch (error) { server.close(); db.close(); throw error; }
process.on("SIGINT", () => server.close(() => { db.close(); process.exit(0); }));
process.on("SIGTERM", () => server.close(() => { db.close(); process.exit(0); }));

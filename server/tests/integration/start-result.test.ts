import { Buffer } from "node:buffer";
import { describe, expect, it } from "vitest";
import app from "../../apps/game-api/src/index";
import { setup } from "../helpers/departure-fixture";

const path = "/v1/runs/start-result";
describe("read-only start receipt lookup without run ID", () => {
  it("recovers a committed response lost by the client without another start or any database change", async () => {
    const f = await setup(true), p = await (await f.preview()).json();
    await f.start(p.operationPreviewId, "lost-response"); // Journal never obtained the run ID.
    const original = JSON.parse(String(f.scalar("SELECT snapshot_json FROM run_snapshots"))).departureResponse;
    const before = f.sql.export();
    const response = await f.post(path, { startIdempotencyKey: "lost-response", operationPreviewId: p.operationPreviewId });
    expect(response.status).toBe(200); expect(response.headers.get("Cache-Control")).toBe("no-store");
    expect(await response.json()).toEqual({ accepted: true, lookupStatus: "COMMITTED", runId: original.runId, runStatus: "ACTIVE", startResponse: original, errorCode: null });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1); expect(f.scalar("SELECT fuel FROM fuel_state")).toBe(20);
  });
  it("distinguishes unknown/in-flight lookup from a later commit and never starts on lookup", async () => {
    const f = await setup(true), p = await (await f.preview()).json();
    const body = { startIdempotencyKey: "pending", operationPreviewId: p.operationPreviewId };
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "NOT_FOUND", runId: null, startResponse: null });
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(0);
    const pending = f.start(p.operationPreviewId, "pending");
    const during = await (await f.post(path, body)).json(); expect(["NOT_FOUND", "COMMITTED"]).toContain(during.lookupStatus);
    await pending;
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "COMMITTED" });
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
  });
  it("isolates account keys and rejects missing/revoked authentication", async () => {
    const f = await setup(true), p = await (await f.preview()).json(); await f.start(p.operationPreviewId, "private-key");
    f.sql.run("UPDATE idempotency_records SET account_id='b' WHERE operation='run_start'");
    const body = { startIdempotencyKey: "private-key", operationPreviewId: p.operationPreviewId };
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "NOT_FOUND", runId: null });
    f.sql.run("UPDATE idempotency_records SET account_id='a' WHERE operation='run_start'");
    f.sql.run("UPDATE runs SET account_id='b'");
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "SNAPSHOT_UNAVAILABLE", runId: null, runStatus: null, startResponse: null });
    expect((await app.request(path, { method: "POST", body: JSON.stringify(body) }, f.env)).status).toBe(401);
    f.sql.run("UPDATE auth_sessions SET status='REVOKED'"); expect((await f.post(path, body)).status).toBe(401);
  });
  it("does not disclose another preview or stored response on same-key mismatch", async () => {
    const f = await setup(true), p = await (await f.preview()).json(); await f.start(p.operationPreviewId, "secret-key");
    const before = f.sql.export();
    const response = await f.post(path, { startIdempotencyKey: "secret-key", operationPreviewId: "different_preview" });
    expect(response.status).toBe(409);
    expect(await response.json()).toEqual({ accepted: false, lookupStatus: "CONFLICT", runId: null, runStatus: null, startResponse: null, errorCode: "start_preview_mismatch" });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  });
  it("reports missing/corrupt snapshots distinctly from no receipt", async () => {
    const f = await setup(true), p = await (await f.preview()).json(); await f.start(p.operationPreviewId, "committed");
    const body = { startIdempotencyKey: "committed", operationPreviewId: p.operationPreviewId };
    f.sql.run("UPDATE run_snapshots SET snapshot_json='invalid'");
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "SNAPSHOT_UNAVAILABLE", startResponse: null });
    f.sql.run("DELETE FROM run_snapshots");
    expect(await (await f.post(path, body)).json()).toMatchObject({ lookupStatus: "SNAPSHOT_UNAVAILABLE", startResponse: null });
    expect(f.scalar("SELECT COUNT(*) FROM runs")).toBe(1);
  });
  it("returns sanitized temporary failure and rejects URL keys and extra fields", async () => {
    const f = await setup(); const body = { startIdempotencyKey: "never-log-this-key", operationPreviewId: "preview_x" };
    expect((await f.post(path + "?startIdempotencyKey=secret", body)).status).toBe(400);
    expect((await f.post(path, { ...body, accountId: "b" })).status).toBe(400);
    const db = f.env.DB, prepare = db.prepare.bind(db);
    db.prepare = ((sql: string) => {
      if (sql.includes("FROM idempotency_records")) throw new Error("temporary failure never-log-this-key");
      return prepare(sql);
    }) as typeof db.prepare;
    const response = await f.post(path, body); expect(response.status).toBe(503);
    const text = await response.text(); expect(text).toContain('"lookupStatus":"TEMPORARILY_UNAVAILABLE"'); expect(text).not.toContain(body.startIdempotencyKey);
    expect(f.scalar("SELECT COUNT(*) FROM ops_error_logs")).toBe(0);
  });
});

import { afterEach, describe, expect, it, vi } from "vitest";
import { Buffer } from "node:buffer";
import { Hono } from "hono";
import { setup as baseSetup } from "../helpers/departure-fixture";
import { registerMonthlyAttendanceRoutes } from "../../apps/game-api/src/monthly-attendance";
const now = Date.parse("2026-09-09T19:00:00Z"), start = Date.parse("2026-08-31T19:00:00Z"), end = Date.parse("2026-09-30T19:00:00Z");
afterEach(() => vi.restoreAllMocks());
async function setup() {
  vi.spyOn(Date, "now").mockReturnValue(now);
  const f = await baseSetup(), app = new Hono<{ Bindings: Env }>();
  registerMonthlyAttendanceRoutes(app, async c => c.req.header("Authorization") ? { accountId: c.req.header("Authorization")! } : { status: 401, errorCode: "auth", message: "auth" });
  const get = (account = "a", query = "") => app.request(`/v1/attendance/monthly/current${query}`, { headers: { Authorization: account } }, f.env);
  const define = (id: string, elite: string | null = null, startAt = start, endAt = end) => f.sql.run("INSERT INTO activity_definitions VALUES(?,?)", [id, JSON.stringify({ kind: "MONTHLY_ATTENDANCE", version: "monthly-attendance-v1", startAt, endAt, eliteTicketItemId: elite })]);
  return { ...f, app, get, define };
}
describe("authenticated monthly attendance discovery", () => {
  it("distinguishes missing, outside-window and reward-binding-unavailable without mutation", async () => {
    const f = await setup();
    f.sql.run("INSERT INTO activity_definitions VALUES('legacy','{\"kind\":\"ATTENDANCE\"}')");
    expect(await (await f.get()).json()).toMatchObject({ status: "NOT_CONFIGURED", activityId: null });
    f.define("future", null, end, Date.parse("2026-10-31T19:00:00Z"));
    expect(await (await f.get()).json()).toMatchObject({ status: "OUTSIDE_WINDOW" });
    f.define("current"); const before = f.sql.export();
    const response = await f.get(); expect(response.headers.get("Cache-Control")).toBe("no-store");
    expect(await response.json()).toMatchObject({ status: "REWARD_BINDING_UNAVAILABLE", activityId: "current", policyVersion: "current", cycleId: "2026-09", canAttend: false });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  });
  it("rejects multiple valid definitions deterministically and preserves only the requesting account pin", async () => {
    const f = await setup(); f.define("z"); f.define("a");
    f.sql.run("INSERT INTO monthly_attendance_cycles VALUES('a','2026-09','z',1)");
    const own = await f.get(); expect(own.status).toBe(409);
    expect(await own.json()).toMatchObject({ status: "AMBIGUOUS_CONFIGURATION", activityId: "z", pinned: true, candidateCount: 2 });
    expect(await (await f.get("b")).json()).toMatchObject({ status: "AMBIGUOUS_CONFIGURATION", activityId: null, pinned: false });
  });
  it("keeps a pinned unavailable policy rather than silently switching to another definition", async () => {
    const f = await setup(); f.define("old", null, Date.parse("2026-07-31T19:00:00Z"), start); f.define("new");
    f.sql.run("INSERT INTO monthly_attendance_cycles VALUES('a','2026-09','old',1)");
    expect(await (await f.get()).json()).toMatchObject({ status: "PINNED_POLICY_UNAVAILABLE", activityId: "old", canAttend: false });
  });
  it("uses the DB clock at rollover and returns new cycle identity instead of reusing a stale response", async () => {
    const f = await setup(); f.define("sep", "TEST_ONLY_ELITE"); f.define("oct", null, end, Date.parse("2026-10-31T19:00:00Z"));
    expect(await (await f.get()).json()).toMatchObject({ status: "AVAILABLE", cycleId: "2026-09", activityId: "sep" });
    vi.spyOn(Date, "now").mockReturnValue(end);
    expect(await (await f.get()).json()).toMatchObject({ status: "REWARD_BINDING_UNAVAILABLE", cycleId: "2026-10", activityId: "oct" });
    const stale = await f.app.request("/v1/attendance/sep/auto-attend", { method: "POST", headers: { Authorization: "a", "Idempotency-Key": "stale" }, body: JSON.stringify({ policyVersion: "sep", cycleId: "2026-09", expectedRevision: 0 }) }, f.env);
    expect(stale.status).toBe(409);
  });
  it("exposes a concurrent new definition as ambiguity and rejects untrusted query/auth", async () => {
    const f = await setup(); f.define("one");
    expect((await f.get()).status).toBe(200);
    f.define("two"); expect((await f.get()).status).toBe(409);
    expect((await f.get("a", "?accountId=b")).status).toBe(400);
    expect((await f.get("")).status).toBe(401);
  });
  it("rolls back attendance when another active definition appears just before commit", async () => {
    const f = await setup(); f.define("one", "TEST_ONLY_ELITE");
    f.sql.run("INSERT INTO crystal_policies VALUES('wallet','{\"currencyId\":\"TEST_CRYSTAL\",\"ticketExchange\":null,\"purchase\":null}')");
    f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'wallet')");
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.define("two", "TEST_ONLY_ELITE"); };
    const response = await f.app.request("/v1/attendance/one/auto-attend", { method: "POST", headers: { Authorization: "a", "Idempotency-Key": "race" }, body: JSON.stringify({ policyVersion: "one", cycleId: "2026-09", expectedRevision: 0 }) }, f.env);
    expect(response.status).toBe(409); expect(await response.json()).toMatchObject({ errorCode: "attendance_definition_ambiguous" });
    expect(f.scalar("SELECT COUNT(*) FROM monthly_attendance_cells")).toBe(0);
    expect(f.scalar("SELECT COUNT(*) FROM crystal_lots")).toBe(0);
  });
});

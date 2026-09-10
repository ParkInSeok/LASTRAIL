import { afterEach, describe, expect, it, vi } from "vitest";
import { Buffer } from "node:buffer";
import { Hono } from "hono";
import { setup as departureSetup } from "../helpers/departure-fixture";
import { registerMonthlyAttendanceRoutes } from "../../apps/game-api/src/monthly-attendance";
import { attendanceMonth } from "../../apps/game-api/src/monthly-attendance-policy";
import { planCrystalChange } from "../../apps/game-api/src/crystal-wallet";
import { objectiveLoginEvent } from "../../apps/game-api/src/objective-events";

const start = Date.parse("2026-08-31T19:00:00Z"), dayMs = 86400000;
afterEach(() => vi.restoreAllMocks());
async function setup(elite: string | null = "TEST_ONLY_ELITE_TICKET") {
  let now = start;
  vi.spyOn(Date, "now").mockImplementation(() => now);
  const f = await departureSetup();
  f.sql.run("UPDATE accounts SET created_at=?", [start]);
  f.sql.run("INSERT INTO activity_definitions VALUES('monthly-test',?)", [JSON.stringify({ kind: "MONTHLY_ATTENDANCE", version: "monthly-attendance-v1", startAt: start, endAt: Date.parse("2029-04-30T19:00:00Z"), eliteTicketItemId: elite })]);
  f.sql.run("INSERT INTO crystal_policies VALUES('wallet-test',?)", [JSON.stringify({ currencyId: "TEST_CRYSTAL", ticketExchange: null, purchase: null })]);
  f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'wallet-test')");
  const app = new Hono<{ Bindings: Env }>();
  registerMonthlyAttendanceRoutes(app, async c => c.req.header("Authorization") ? { accountId: c.req.header("Authorization")! } : { status: 401, errorCode: "authentication_required", message: "Auth required" });
  const request = (suffix: string, body?: object, key = crypto.randomUUID(), account = "a") => app.request(`/v1/attendance/monthly-test/${suffix}`, {
    method: body ? "POST" : "GET", headers: { Authorization: account, "Idempotency-Key": key }, ...(body ? { body: JSON.stringify(body) } : {}),
  }, f.env);
  const read = async () => (await request("monthly")).json();
  const command = async (recover = false, key?: string, override?: object) => {
    const state = await read();
    return request(recover ? "recover-next" : "auto-attend", { policyVersion: "monthly-test", cycleId: state.cycleId, expectedRevision: state.revision,
      ...(recover ? { walletPolicyVersion: "wallet-test", confirmedPaid: state.recovery.quote.paidSpent } : {}), ...override }, key);
  };
  const fund = async (free: number, paid = 0) => {
    const mid = crypto.randomUUID(), oid = crypto.randomUUID();
    const seq = Number(f.scalar("SELECT COALESCE(MAX(sequence),0)+1 FROM account_mutations WHERE account_id='a'"));
    f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES(?,'a',?,'TEST',?)", [mid, seq, now]);
    if (paid) f.sql.run("INSERT INTO verified_store_orders(id,account_id,provider,app_id,product_id,transaction_id,environment,product_kind,verification_ref,purchased_at,verified_at) VALUES(?,'a','GOOGLE_PLAY','TEST','TEST',?,'DEV','CRYSTAL','TEST',?,?)", [oid, oid, now, now]);
    const p = await planCrystalChange({ db: f.env.DB, accountId: "a", operationId: crypto.randomUUID(), mutationId: mid, operation: "TEST", now,
      grants: [...(free ? [{ quantity: free, sourceType: "ATTENDANCE" as const, sourceRef: mid, orderId: null }] : []), ...(paid ? [{ quantity: paid, sourceType: "STORE_PURCHASE" as const, sourceRef: mid, orderId: oid }] : [])] });
    await f.env.DB.batch(p.statements);
  };
  return { ...f, request, read, command, fund, set: (day: number, offset = 0) => { now = start + day * dayMs + offset; }, setTime: (time: number) => { now = time; } };
}
describe("monthly sequential attendance", () => {
  it("reads 28 ordered cells without grants and fails closed for missing elite binding", async () => {
    const f = await setup(null), before = f.sql.export();
    const response = await f.request("monthly"); expect(response.headers.get("Cache-Control")).toBe("no-store");
    const state = await response.json(); expect(state.cells).toHaveLength(28); expect(state.complete).toBe(false);
    expect(state.unavailableReason).toBe("elite_ticket_binding_missing");
    expect((await f.command()).status).toBe(503);
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  });
  it("pays the next cell once per service day and never skips cells for missed days", async () => {
    const f = await setup();
    const first = await (await f.command(false, "first")).json(); expect(first.cell).toBe(1);
    expect(await (await f.command(false, "new-key")).json()).toEqual(first);
    f.set(5); const next = await (await f.command()).json(); expect(next.cell).toBe(2);
    expect(next.rewardLines).toMatchObject([{ id: "cur_credits", quantity: 1000 }]);
    expect((await f.read()).recovery.recoverableCount).toBe(4);
  });
  it("caps all month lengths at 28 rewards with exact economy totals and one fixture elite", async () => {
    const f = await setup();
    for (const month of ["2026-09-01", "2026-10-01", "2027-02-01", "2028-02-01"]) {
      const cycle = attendanceMonth(Date.parse(`${month}T00:00:00Z`));
      for (let time = cycle.startAt; time < cycle.endAt; time += dayMs) { f.setTime(time); expect((await f.command()).status).toBe(200); }
      expect((await f.read()).completedCells).toBe(28);
    }
    expect(f.scalar("SELECT quantity FROM account_items WHERE item_id='TEST_ONLY_ELITE_TICKET'")).toBe(4);
    for (const [id, total] of [["CREDITS", 14000], ["MACHINE_PARTS", 350], ["OPERATOR_EXP", 700], ["TACTICAL_DATA", 190]] as const) expect(f.scalar(`SELECT balance FROM account_currencies WHERE currency='${id}'`)).toBe(total * 4);
    expect(f.scalar("SELECT COUNT(*) FROM account_items WHERE item_id IN ('cur_credits','cur_machine_parts','cur_operator_exp','cur_tactical_data')")).toBe(0);
    expect(f.scalar("SELECT SUM(granted) FROM crystal_lots")).toBe(650 * 4);
  });
  it("requires today's automatic attendance before recovery, then FREE-first exact PAID confirmation", async () => {
    const f = await setup(); f.set(3); await f.fund(10, 300);
    expect((await f.command(true)).status).toBe(409);
    await f.command(); // Gives 50 FREE before a separately confirmed recovery.
    expect((await f.command(true, undefined, { confirmedPaid: 0 })).status).toBe(409);
    const recovered = await (await f.command(true)).json();
    expect(recovered).toMatchObject({ cell: 2, sourceDayIndex: 0, wallet: { freeSpent: 60, paidSpent: 40 } });
    expect(f.scalar("SELECT COUNT(*) FROM pass_xp_events")).toBe(0);
    await f.env.DB.batch([objectiveLoginEvent(f.env.DB, "a")]);
    expect(f.scalar("SELECT COUNT(*) FROM objective_events WHERE kind='LOGIN'")).toBe(1);
    expect((await f.read()).recovery.used).toBe(1);
  });
  it("never finances a crystal reward using that same uncommitted reward", async () => {
    const f = await setup();
    for (let d = 0; d < 6; d++) { f.set(d); await f.command(); }
    f.set(8); await f.command(); // Natural cell7 crystal milestone committed.
    f.sql.run("UPDATE crystal_lots SET remaining=0");
    const before = f.sql.export();
    // Cell8 rewards50 crystals, but wallet must hold100 before this command.
    expect((await f.command(true)).status).toBe(409);
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  });
  it("restricts recovery to fully elapsed post-registration days and monthly quota seven", async () => {
    const f = await setup(); f.sql.run("UPDATE accounts SET created_at=? WHERE id='a'", [start + dayMs * 10 + 3600000]);
    f.set(11); await f.command(); expect((await f.read()).recovery.recoverableCount).toBe(0);
    f.set(25); await f.command(); await f.fund(2000);
    for (let i = 0; i < 7; i++) expect((await f.command(true)).status).toBe(200);
    expect((await f.command(true)).status).toBe(409);
    expect((await f.read()).recovery.used).toBe(7);
  });
  it("rejects stale month quotes but replays prior successful receipts across rollover", async () => {
    const f = await setup();
    const body = { policyVersion: "monthly-test", cycleId: "2026-09", expectedRevision: 0 };
    const original = await (await f.request("auto-attend", body, "old")).json();
    const aliasBody = { ...body, expectedRevision: 1 };
    expect(await (await f.request("auto-attend", aliasBody, "alias")).json()).toEqual(original);
    f.set(30); expect(await (await f.request("auto-attend", body, "old")).json()).toEqual(original);
    expect(await (await f.request("auto-attend", aliasBody, "alias")).json()).toEqual(original);
    expect((await f.request("auto-attend", body, "new")).status).toBe(409);
    expect((await f.read()).completedCells).toBe(0);
  });
  it("retries a day-boundary transaction and rejects a month-boundary transaction without grants", async () => {
    const f = await setup(); f.set(0, dayMs - 1);
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.set(1); };
    const response = await (await f.command()).json(); expect(response.sourceDayIndex).toBe(1);
    f.set(29, dayMs - 1);
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.set(30); };
    expect((await f.command()).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM monthly_attendance_cells")).toBe(1);
  });
  it("serializes competing recovery/natural calls and rolls back wallet on failed receipt", async () => {
    const f = await setup(); f.set(10); await f.command(); await f.fund(500);
    const quote = await f.read(), body = { policyVersion: "monthly-test", cycleId: quote.cycleId, expectedRevision: quote.revision, walletPolicyVersion: "wallet-test", confirmedPaid: 0 };
    const responses = await Promise.all([f.request("recover-next", body), f.request("recover-next", body), f.command()]);
    expect(responses.map(r => r.status).sort()).toEqual([200, 200, 409]);
    expect((await f.read()).completedCells).toBe(2);
    f.sql.run("CREATE TRIGGER TEST_failure BEFORE INSERT ON activity_claims BEGIN SELECT RAISE(ABORT,'CHECK constraint failed: valid=1'); END");
    const before = f.sql.export(); expect((await f.command(true)).status).toBe(409);
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  });
  it("does not share account progress or accept client cell/XP/date fields", async () => {
    const f = await setup(); await f.command();
    expect((await (await f.request("monthly", undefined, undefined, "b")).json()).completedCells).toBe(0);
    expect((await f.command(true, undefined, { cell: 28, xp: 1000 })).status).toBe(400);
    expect((await f.request("monthly", undefined, undefined, "")).status).toBe(401);
  });
  it("reaches fixture cell28 through legitimate recovery once and never grants it on later visits", async () => {
    const f = await setup();
    for (let d = 0; d < 24; d++) { f.set(d); await f.command(); }
    f.set(27); await f.command();
    for (let i = 0; i < 3; i++) expect((await f.command(true)).status).toBe(200);
    expect((await f.read()).complete).toBe(true);
    expect((await f.command(true)).status).toBe(409);
    f.set(29); expect((await (await f.command()).json()).cell).toBe(null);
    expect(f.scalar("SELECT quantity FROM account_items WHERE item_id='TEST_ONLY_ELITE_TICKET'")).toBe(1);
  });
});

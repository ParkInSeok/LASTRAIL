import { afterEach, describe, expect, it, vi } from "vitest";
import { Buffer } from "node:buffer";
import { Hono } from "hono";
import { setup as baseSetup } from "../helpers/departure-fixture";
import { registerShopRoutes } from "../../apps/game-api/src/shop-routes";
import { planCrystalChange } from "../../apps/game-api/src/crystal-wallet";
const start = Date.parse("2026-09-09T19:00:00Z"), day = 86400000;
afterEach(() => vi.restoreAllMocks());
async function setup(price = 100) {
  let now = start; vi.spyOn(Date, "now").mockImplementation(() => now);
  const f = await baseSetup();
  const policy = { currencyId: "TEST_CRYSTAL", ticketExchange: { ticketItemId: "cur_command_ticket", crystalsPerTicket: price, maxTickets: 10 },
    purchase: { provider: "GOOGLE_PLAY", appId: "TEST_APP", products: [{ productId: "TEST_SKU", quantity: 100 }] } };
  f.sql.run("INSERT INTO crystal_policies VALUES('p',?)", [JSON.stringify(policy)]); f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'p')");
  const app = new Hono<{ Bindings: Env }>();
  registerShopRoutes(app, async c => c.req.header("Authorization") ? { accountId: c.req.header("Authorization")! } : { status: 401, errorCode: "auth", message: "auth" });
  const read = () => app.request("/v1/shop", { headers: { Authorization: "a" } }, f.env);
  const command = (offer = "DAILY", key = crypto.randomUUID(), override = {}) => app.request("/v1/shop/tickets/exchange", {
    method: "POST", headers: { Authorization: "a", "Idempotency-Key": key },
    body: JSON.stringify({ policyVersion: "p", offer, ticketCount: 1, expectedDayStart: start, ...override }),
  }, f.env);
  const fund = async (free = 300, paid = 0) => {
    const mid = crypto.randomUUID(), seq = Number(f.scalar("SELECT COALESCE(MAX(sequence),0)+1 FROM account_mutations WHERE account_id='a'"));
    f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES(?,'a',?,'TEST',?)", [mid, seq, now]);
    if (paid) f.sql.run("INSERT INTO verified_store_orders(id,account_id,provider,app_id,product_id,transaction_id,environment,product_kind,verification_ref,purchased_at,verified_at) VALUES('order','a','GOOGLE_PLAY','TEST_APP','TEST_SKU','tx','DEV','CRYSTAL','test',?,?)", [now, now]);
    const plan = await planCrystalChange({ db: f.env.DB, accountId: "a", operationId: crypto.randomUUID(), mutationId: mid, operation: "TEST", now,
      grants: [...(free ? [{ quantity: free, sourceType: "ATTENDANCE" as const, sourceRef: mid, orderId: null }] : []),
        ...(paid ? [{ quantity: paid, sourceType: "STORE_PURCHASE" as const, sourceRef: mid, orderId: "order" }] : [])] });
    await f.env.DB.batch(plan.statements);
  };
  return { ...f, policy, read, command, fund, set: (at: number) => { now = at; } };
}
describe("approved shop ticket offers", () => {
  it("reads only configured products without writes or invented store prices", async () => {
    const f = await setup(), before = f.sql.export();
    const response = await f.read(); expect(response.headers.get("Cache-Control")).toBe("no-store");
    const state = await response.json(); expect(state.ticketOffers.map((o: { crystalsPerTicket: number }) => o.crystalsPerTicket)).toEqual([100, 50]);
    expect(state.crystalProducts).toMatchObject([{ productId: "TEST_SKU", quantity: 100, price: null, purchaseAvailable: false }]);
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  });
  it("charges daily50 FREE-first once and normal100 with actual ticket ledger", async () => {
    const f = await setup(); await f.fund(30, 200);
    const daily = await f.command(); expect(daily.status).toBe(200);
    expect(await daily.json()).toMatchObject({ freeSpent: 30, paidSpent: 20, cost: 50, ticketsGranted: 1 });
    expect((await f.command()).status).toBe(409);
    expect((await f.command("NORMAL")).status).toBe(200);
    expect(f.scalar("SELECT quantity FROM account_items WHERE item_id='cur_command_ticket'")).toBe(2);
    expect(f.scalar("SELECT SUM(remaining) FROM crystal_lots")).toBe(80);
    expect(f.scalar("SELECT SUM(delta) FROM item_ledger WHERE reason='CRYSTAL_TICKET_EXCHANGE'")).toBe(2);
  });
  it("serializes concurrent daily requests and keeps the limit across policy changes", async () => {
    const f = await setup(); await f.fund();
    const responses = await Promise.all([f.command(), f.command()]); expect(responses.map(r => r.status).sort()).toEqual([200, 409]);
    f.sql.run("INSERT INTO crystal_policies VALUES('p2',?)", [JSON.stringify(f.policy)]); f.sql.run("UPDATE crystal_policy_active SET policy_id='p2'");
    expect((await f.command("DAILY", undefined, { policyVersion: "p2" })).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM shop_daily_ticket_claims")).toBe(1);
  });
  it("recovers an identical receipt committed after the initial replay read but before quota validation", async () => {
    const f = await setup(); await f.fund(50);
    const prepare = f.adapter.prepare.bind(f.adapter);
    let inject = true, committed: unknown;
    vi.spyOn(f.adapter, "prepare").mockImplementation(sql => {
      const statement = prepare(sql);
      if (inject && sql.startsWith("SELECT 1 FROM shop_daily_ticket_claims")) {
        inject = false;
        const first = statement.first.bind(statement);
        statement.first = async <T>() => {
          const response = await f.command("DAILY", "same"); expect(response.status).toBe(200);
          committed = await response.json(); return first<T>();
        };
      }
      return statement;
    });
    const response = await f.command("DAILY", "same"); expect(response.status).toBe(200);
    expect(await response.json()).toEqual(committed);
    expect(f.scalar("SELECT COUNT(*) FROM shop_daily_ticket_claims")).toBe(1);
    expect(f.scalar("SELECT SUM(remaining) FROM crystal_lots")).toBe(0);
  });
  it("replays the original day receipt and gives no carryover after reset", async () => {
    const f = await setup(); await f.fund();
    const original = await (await f.command("DAILY", "old")).json(); f.set(start + day * 3);
    expect(await (await f.command("DAILY", "old")).json()).toEqual(original);
    expect((await f.command()).status).toBe(409);
    expect((await f.command("DAILY", undefined, { expectedDayStart: start + day * 3, ticketCount: 2 })).status).toBe(400);
    expect((await f.command("DAILY", undefined, { expectedDayStart: start + day * 3 })).status).toBe(200);
    expect(f.scalar("SELECT COUNT(*) FROM shop_daily_ticket_claims")).toBe(2);
  });
  it("rolls back a daily purchase crossing DB reset without consuming money or quota", async () => {
    const f = await setup(); await f.fund(); f.set(start + day - 1);
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.set(start + day); };
    expect((await f.command()).status).toBe(409);
    expect(f.scalar("SELECT COUNT(*) FROM shop_daily_ticket_claims")).toBe(0);
    expect(f.scalar("SELECT SUM(remaining) FROM crystal_lots")).toBe(300);
  });
  it("does not activate launch offers for unapproved configured prices", async () => {
    const f = await setup(11); await f.fund();
    expect((await (await f.read()).json()).ticketOffers).toEqual([]);
    expect((await f.command()).status).toBe(503);
  });
});

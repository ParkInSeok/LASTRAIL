import { z } from "zod";
import type { Context, Hono } from "hono";
import { ActivityError, activityId } from "./activity-policy";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { claimCrystals } from "./crystal-service";
import { readCrystalPolicy, readCrystalWallet } from "./crystal-wallet";
import { objectivePeriods } from "./recurring-policy";
import type { CrystalPurchaseVerifier } from "./crystal-orders";
const command = z.strictObject({ policyVersion: activityId, offer: z.enum(["NORMAL", "DAILY"]),
  ticketCount: z.number().int().positive().max(2147483647), expectedDayStart: z.number().int().nonnegative() });
export function registerShopRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>) {
  const handler = (write: boolean) => async (c: Context<{ Bindings: Env }>) => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c); if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      if (new URL(c.req.url).search) throw new ActivityError(400, "contract_violation");
      const body = await readBoundedBody(c);
      if (write) {
        const key = c.req.header("Idempotency-Key");
        if (!key || !/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "idempotency_key_required");
        let parsed: z.infer<typeof command>; try { parsed = command.parse(JSON.parse(body)); } catch { throw new ActivityError(400, "contract_violation"); }
        return c.json(await claimCrystals({ db: c.env.DB, accountId: auth.accountId, environment: c.env.ENVIRONMENT }, { kind: "SHOP_EXCHANGE", ...parsed }, key));
      }
      if (body.length) throw new ActivityError(400, "contract_violation");
      const policy = await readCrystalPolicy(c.env.DB), wallet = await readCrystalWallet(c.env.DB, auth.accountId);
      if (wallet.policyVersion !== policy.policyVersion) throw new ActivityError(409, "crystal_policy_changed");
      const serverTime = Date.now(), period = objectivePeriods(serverTime);
      const used = await c.env.DB.prepare("SELECT 1 FROM shop_daily_ticket_claims WHERE account_id=? AND offer_family='COMMAND_TICKET_DAILY' AND day_start=?").bind(auth.accountId, period.dayStart).first();
      const ready = policy.ticketExchange?.crystalsPerTicket === 100;
      const verifier = Boolean((c.env as Env & { CRYSTAL_PURCHASE_VERIFIER?: CrystalPurchaseVerifier }).CRYSTAL_PURCHASE_VERIFIER);
      return c.json({ accepted: true, version: "shop-v1", serverTime, dayStart: period.dayStart, nextResetAt: period.dayEnd,
        policyVersion: policy.policyVersion, wallet, ticketOffers: ready ? [
          { offer: "NORMAL", ticketItemId: "cur_command_ticket", crystalsPerTicket: 100, maxTicketsPerRequest: policy.ticketExchange!.maxTickets, dailyLimit: null },
          { offer: "DAILY", offerFamily: "COMMAND_TICKET_DAILY", ticketItemId: "cur_command_ticket", crystalsPerTicket: 50, maxTicketsPerRequest: 1, dailyLimit: 1, dailyRemaining: used ? 0 : 1 },
        ] : [], ticketUnavailableReason: ready ? null : "shop_launch_price_unconfigured",
        crystalProducts: policy.purchase?.products.map(p => ({ ...p, provider: policy.purchase!.provider, appId: policy.purchase!.appId,
          price: null, priceSource: "STORE_REQUIRED", purchaseAvailable: verifier })) ?? [],
        capabilities: { materialShop: "UNCONFIGURED", cashPriceSource: "STORE_REQUIRED", crystalPurchaseVerifierAvailable: verifier,
          passCatalog: "/v1/recurring/state", creditRecruitmentExchange: "NOT_SUPPORTED" } });
    } catch (error) { if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status); throw error; }
  };
  app.get("/v1/shop", handler(false));
  app.post("/v1/shop/tickets/exchange", handler(true));
}

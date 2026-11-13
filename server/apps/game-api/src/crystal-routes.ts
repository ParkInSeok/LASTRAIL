import { z } from "zod";
import type { Context, Hono } from "hono";
import { ActivityError, activityId } from "./activity-policy";
import { readBoundedBody } from "./activity-routes";
import type { ActivityAuthResult } from "./activity-routes";
import { readCrystalWallet } from "./crystal-wallet";
import { claimCrystals } from "./crystal-service";
import type { CrystalPurchaseVerifier } from "./crystal-orders";

const exchange = z.object({ policyVersion: activityId, ticketCount: z.number().int().positive().max(2147483647) }).strict();
const purchase = z.object({ policyVersion: activityId, productId: z.string().min(1).max(200), receipt: z.string().min(1).max(16384) }).strict();
type Ctx = Context<{ Bindings: Env }>;
export function registerCrystalRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Ctx) => Promise<ActivityAuthResult>) {
  const handler = (kind?: "EXCHANGE" | "PURCHASE") => async (c: Ctx) => {
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      if (new URL(c.req.url).search !== "") throw new ActivityError(400, "contract_violation");
      if (!kind) return c.json(await readCrystalWallet(c.env.DB, auth.accountId));
      const key = c.req.header("Idempotency-Key");
      if (!key || !/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "idempotency_key_required");
      const text = await readBoundedBody(c);
      let body: unknown;
      try { body = JSON.parse(text); } catch { throw new ActivityError(400, "contract_violation"); }
      const env = c.env as Env & { CRYSTAL_PURCHASE_VERIFIER?: CrystalPurchaseVerifier };
      const options = { db: c.env.DB, accountId: auth.accountId, environment: c.env.ENVIRONMENT,
        ...(env.CRYSTAL_PURCHASE_VERIFIER ? { verifier: env.CRYSTAL_PURCHASE_VERIFIER } : {}) };
      if (kind === "EXCHANGE") {
        const parsed = exchange.safeParse(body);
        if (!parsed.success) throw new ActivityError(400, "contract_violation");
        return c.json(await claimCrystals(options, { kind, ...parsed.data }, key));
      }
      const parsed = purchase.safeParse(body);
      if (!parsed.success) throw new ActivityError(400, "contract_violation");
      return c.json(await claimCrystals(options, { kind, ...parsed.data }, key));
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  };
  app.get("/v1/wallet/crystals", handler());
  app.post("/v1/wallet/crystals/tickets/exchange", handler("EXCHANGE"));
  app.post("/v1/wallet/crystals/purchases/verify", handler("PURCHASE"));
}

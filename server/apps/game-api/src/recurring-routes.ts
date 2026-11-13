import type { Context, Hono } from "hono";
import { z } from "zod";
import { ActivityError, activityId } from "./activity-policy";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { objectiveLoginEvent } from "./objective-events";
import { readRecurringState, syncRecurringAccount } from "./recurring-progress";
import { claimRecurringRight, requestReceipt, requestReplay } from "./recurring-grants";
import { recurringRetryable } from "./recurring-progress";
import { objectivePeriods } from "./recurring-policy";
import { purchasePass50, type Pass50Verifier } from "./pass50-purchase";
import { bulkClaimSchema, claimRecurringBulk, readBulkClaim } from "./recurring-bulk";
const passClaim = z.strictObject({ level: z.number().int().min(1).max(50), track: z.enum(["FREE", "PREMIUM"]) });
const reconciliation = z.strictObject({ intentId: activityId, receipt: z.string().min(1).max(16384) });
export function registerRecurringRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Context<{ Bindings: Env }>) => Promise<ActivityAuthResult>) {
  const handler = (kind: "READ" | "LOGIN" | "SYNC" | "QUEST" | "MAIL" | "PASS_LEVEL" | "INTENT" | "RECONCILE" | "BULK" | "BULK_READ") => async (c: Context<{ Bindings: Env }>) => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c); if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      if (new URL(c.req.url).search) throw new ActivityError(400, "contract_violation");
      const raw = await readBoundedBody(c), key = c.req.header("Idempotency-Key") ?? "";
      if (["READ", "LOGIN", "SYNC", "QUEST", "MAIL", "INTENT", "BULK_READ"].includes(kind) && raw.length) throw new ActivityError(400, "contract_violation");
      if (kind === "BULK_READ") {
        const id = c.req.param("bulkId"); if (!activityId.safeParse(id).success) throw new ActivityError(400, "contract_violation");
        return c.json(await readBulkClaim(c.env.DB, auth.accountId, id!));
      }
      if (kind === "READ") return c.json(await readRecurringState(c.env.DB, auth.accountId));
      if (kind === "LOGIN") {
        if (!/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "idempotency_key_required");
        for (let i = 0; i < 4; i++) {
          const prior = await requestReplay(c.env.DB, auth.accountId, "recurring_login", key, "login-v1"); if (prior) return c.json(prior);
          const period = objectivePeriods(Date.now()), response = { accepted: true, eventSource: "SERVER_LOGIN", dayStart: period.dayStart, refreshRequired: true };
          try {
            await c.env.DB.batch([c.env.DB.prepare("INSERT INTO recurring_guard(valid) SELECT CASE WHEN CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER)>=? AND CAST(ROUND(unixepoch('subsec')*1000) AS INTEGER)<? THEN 1 ELSE 0 END").bind(period.dayStart, period.dayEnd),
              objectiveLoginEvent(c.env.DB, auth.accountId), requestReceipt(c.env.DB, auth.accountId, "recurring_login", key, "login-v1", JSON.stringify(response)), c.env.DB.prepare("DELETE FROM recurring_guard")]);
            return c.json(response);
          } catch (error) { if (recurringRetryable(error) && i < 3) continue; throw error; }
        }
        throw new ActivityError(409, "recurring_state_changed");
      }
      if (kind === "SYNC") return c.json(await syncRecurringAccount(c.env.DB, auth.accountId));
      if (!/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "idempotency_key_required");
      if (kind === "BULK") {
        let body: z.infer<typeof bulkClaimSchema>; try { body = bulkClaimSchema.parse(JSON.parse(raw)); } catch { throw new ActivityError(400, "contract_violation"); }
        return c.json(await claimRecurringBulk(c.env.DB, auth.accountId, body, key));
      }
      if (kind === "QUEST" || kind === "MAIL") {
        const awardId = c.req.param("awardId"); if (!activityId.safeParse(awardId).success) throw new ActivityError(400, "contract_violation");
        return c.json(await claimRecurringRight(c.env.DB, auth.accountId, { kind, awardId: awardId! }, key));
      }
      const policyId = c.req.param("policyId"); if (!activityId.safeParse(policyId).success) throw new ActivityError(400, "contract_violation");
      if (kind === "PASS_LEVEL") {
        let body: z.infer<typeof passClaim>; try { body = passClaim.parse(JSON.parse(raw)); } catch { throw new ActivityError(400, "contract_violation"); }
        return c.json(await claimRecurringRight(c.env.DB, auth.accountId, { kind, policyId: policyId!, ...body }, key));
      }
      const env = c.env as Env & { PASS50_PURCHASE_VERIFIER?: Pass50Verifier };
      const options = { db: c.env.DB, accountId: auth.accountId, environment: c.env.ENVIRONMENT, ...(env.PASS50_PURCHASE_VERIFIER ? { verifier: env.PASS50_PURCHASE_VERIFIER } : {}) };
      if (kind === "INTENT") return c.json(await purchasePass50(options, { kind, policyId: policyId! }, key));
      let body: z.infer<typeof reconciliation>; try { body = reconciliation.parse(JSON.parse(raw)); } catch { throw new ActivityError(400, "contract_violation"); }
      return c.json(await purchasePass50(options, { kind, policyId: policyId!, ...body }, key));
    } catch (error) { if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status); throw error; }
  };
  app.get("/v1/recurring/state", handler("READ"));
  app.post("/v1/recurring/bulk-claims", handler("BULK"));
  app.get("/v1/recurring/bulk-claims/:bulkId", handler("BULK_READ"));
  app.post("/v1/recurring/login", handler("LOGIN"));
  app.post("/v1/recurring/sync", handler("SYNC"));
  app.post("/v1/quests/:awardId/claim", handler("QUEST"));
  app.post("/v1/quest-mail/:awardId/claim", handler("MAIL"));
  app.post("/v1/passes/:policyId/levels/claim", handler("PASS_LEVEL"));
  app.post("/v1/passes/:policyId/checkout-intents", handler("INTENT"));
  app.post("/v1/passes/:policyId/purchases/reconcile", handler("RECONCILE"));
}

import type { Context, Hono } from "hono";
import { ActivityError } from "./activity-policy";
import { readBoundedBody } from "./activity-routes";
import type { ActivityAuthResult } from "./activity-routes";
import { growthActionSchema, mutateGrowth } from "./growth-service";
import { readEconomyBinding } from "./economy-binding";

type Ctx = Context<{ Bindings: Env }>;
export function registerGrowthRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Ctx) => Promise<ActivityAuthResult>) {
  const handle = (kind?: string) => async (c: Ctx) => {
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      if (new URL(c.req.url).search !== "") throw new ActivityError(400, "contract_violation");
      if (!kind) {
        const binding = await readEconomyBinding(c.env.DB);
        if (!binding) throw new ActivityError(503, "economy_unconfigured");
        return c.json({ economyVersion: binding.version, economyHash: binding.sha256, balanceVersion: binding.balanceVersion,
          growth: binding.config.growth, equipmentEnabled: false, researchEnabled: false });
      }
      let body: unknown;
      try { body = JSON.parse(await readBoundedBody(c)); } catch { throw new ActivityError(400, "contract_violation"); }
      if (!body || typeof body !== "object" || Array.isArray(body)
        || ["kind", "slot", "targetId"].some(field => field in body)) throw new ActivityError(400, "contract_violation");
      const pathSlot = c.req.param("slot"), targetId = c.req.param("targetId");
      if (pathSlot !== undefined && !/^[0-4]$/u.test(pathSlot)) throw new ActivityError(400, "contract_violation");
      const action = growthActionSchema.safeParse({ ...body, kind, ...(pathSlot !== undefined ? { slot: Number(pathSlot) } : {}),
        ...(targetId !== undefined ? { targetId } : {}) });
      if (!action.success) throw new ActivityError(400, "contract_violation");
      return c.json(await mutateGrowth({ db: c.env.DB, accountId: auth.accountId }, action.data, c.req.header("Idempotency-Key") ?? ""));
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  };
  app.get("/v1/growth/config", handle());
  app.post("/v1/operators/training-core/:slot/level-up", handle("CORE_LEVEL"));
  app.post("/v1/operators/training-core/sync-up", handle("CORE_SYNC"));
  app.post("/v1/operators/training-core/:slot/replace", handle("CORE_REPLACE"));
  app.post("/v1/trains/:targetId/level-up", handle("TRAIN"));
  app.post("/v1/operators/:targetId/skill/level-up", handle("SKILL"));
  app.post("/v1/operators/:targetId/promotion", handle("PROMOTION"));
  app.post("/v1/equipment/:targetId/level-up", handle("EQUIPMENT"));
  app.post("/v1/research/:targetId/level-up", handle("RESEARCH"));
}

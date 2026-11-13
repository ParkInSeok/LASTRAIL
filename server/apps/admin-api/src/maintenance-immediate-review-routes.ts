import type { Context, Hono } from "hono";
import { MaintenancePlanError, maintenanceEnvironmentSchema, type MaintenancePrincipal } from "../../../packages/common/src/maintenance-plan-policy";
import { readBoundedBody } from "../../game-api/src/activity-routes";
import { ActivityError } from "../../game-api/src/activity-policy";
import { MaintenanceImmediateReview } from "./maintenance-immediate-review";
import { MaintenanceRuntimePolicyStore } from "./maintenance-runtime-policy-store";
type Ctx = Context<{ Bindings: Env }>;
export function registerMaintenanceImmediateReviewRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Ctx) => Promise<MaintenancePrincipal | Response>) {
  const handle = (mode: "read" | "review" | "apply") => async (c: Ctx) => {
    c.header("Cache-Control", "no-store");
    const actor = await authenticate(c); if (actor instanceof Response) return actor;
    try {
      const environment = maintenanceEnvironmentSchema.safeParse(c.env.ENVIRONMENT);
      if (!environment.success) throw new MaintenancePlanError(503, "maintenance_environment_unavailable");
      if (new URL(c.req.url).searchParams.size) throw new MaintenancePlanError(400, "contract_violation");
      const service = new MaintenanceImmediateReview(c.env.DB, { environment: environment.data, applicationId: "LASTRAIL" });
      const raw = await readBoundedBody(c);
      if (mode === "read") { if (raw !== "") throw new MaintenancePlanError(400, "contract_violation"); return c.json(await service.read(actor)); }
      let body: unknown; try { body = JSON.parse(raw); } catch { throw new MaintenancePlanError(400, "contract_violation"); }
      if (mode === "review") return c.json(await service.review(actor, body));
      const key = c.req.header("Idempotency-Key") ?? "";
      if (!/^[\x21-\x7e]{1,128}$/u.test(key)) throw new MaintenancePlanError(400, "idempotency_key_required");
      return c.json(await new MaintenanceRuntimePolicyStore(c.env.DB, { environment: environment.data, applicationId: "LASTRAIL" }).changeImmediately(actor, body, key));
    } catch (error) {
      if (error instanceof MaintenancePlanError || error instanceof ActivityError) return c.json({ error: error.code }, error.status);
      return c.json({ error: "maintenance_policy_unavailable" }, 503);
    }
  };
  app.get("/v1/admin/maintenance/current-state", handle("read"));
  app.post("/v1/admin/maintenance/immediate-review", handle("review"));
  app.post("/v1/admin/maintenance/immediate-apply", handle("apply"));
}

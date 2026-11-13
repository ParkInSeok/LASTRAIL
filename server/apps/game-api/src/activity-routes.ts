import type { Context, Hono } from "hono";
import { z } from "zod";
import { ActivityError, activityId } from "./activity-policy";
import { claimActivity, readActivity } from "./activity-service";
import type { ActivityAction, PassPurchaseVerifier } from "./activity-service";

const milestone = z.object({ days: z.number().int().min(1).max(2147483647) }).strict();
const recovery = z.object({ dayIndex: z.number().int().min(0).max(100000000) }).strict();
const passReward = z.object({ level: z.number().int().min(1).max(1000), track: z.enum(["FREE", "PREMIUM"]) }).strict();
const xp = z.object({ runId: activityId }).strict();
const purchase = z.object({ receipt: z.string().min(1).max(16384), productId: z.string().min(1).max(200) }).strict();
type Ctx = Context<{ Bindings: Env }>;
export type ActivityAuthResult = { accountId: string } | { status: 400 | 401 | 403 | 409 | 500 | 503; errorCode: string; message: string };

export async function readBoundedBody(c: Ctx, maxBytes = 18000): Promise<string> {
  if (!Number.isSafeInteger(maxBytes) || maxBytes <= 0) throw new ActivityError(503, 'body_limit_unavailable');
  const reader = c.req.raw.body?.getReader();
  if (!reader) return "";
  const chunks: Uint8Array[] = [];
  let length = 0;
  try {
    while (true) {
      const part = await reader.read();
      if (part.done) break;
      length += part.value.byteLength;
      if (length > maxBytes) {
        await reader.cancel();
        throw new ActivityError(400, "contract_violation");
      }
      chunks.push(part.value);
    }
  } finally { reader.releaseLock(); }
  const body = new Uint8Array(length);
  let offset = 0;
  for (const chunk of chunks) { body.set(chunk, offset); offset += chunk.byteLength; }
  return new TextDecoder().decode(body);
}

export function registerActivityRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Ctx) => Promise<ActivityAuthResult>): void {
  const handler = (kind: "ATTENDANCE" | "PASS", actionKind?: ActivityAction["kind"]) => async (c: Ctx) => {
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode, message: auth.message }, auth.status);
    try {
      const parsedId = activityId.safeParse(c.req.param("activityId"));
      if (!parsedId.success || new URL(c.req.url).search !== "") throw new ActivityError(400, "contract_violation");
      const env = c.env as Env & { PASS_PURCHASE_VERIFIER?: PassPurchaseVerifier };
      const options = { db: c.env.DB, accountId: auth.accountId, activityId: parsedId.data, environment: c.env.ENVIRONMENT,
        ...(env.PASS_PURCHASE_VERIFIER ? { verifier: env.PASS_PURCHASE_VERIFIER } : {}) };
      if (!actionKind) return c.json(await readActivity(options, kind));
      const key = c.req.header("Idempotency-Key");
      if (!key || !/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "idempotency_key_required");
      const text = await readBoundedBody(c);
      let body: unknown = {};
      if (text.length) { try { body = JSON.parse(text); } catch { throw new ActivityError(400, "contract_violation"); } }
      let action: ActivityAction;
      if (actionKind === "CHECK_IN") {
        if (text.length !== 0) throw new ActivityError(400, "contract_violation");
        action = { kind: actionKind };
      } else if (actionKind === "MILESTONE") {
        const parsed = milestone.safeParse(body);
        if (!parsed.success) throw new ActivityError(400, "contract_violation");
        action = { kind: actionKind, ...parsed.data };
      } else if (actionKind === "RECOVER") {
        const parsed = recovery.safeParse(body);
        if (!parsed.success) throw new ActivityError(400, "contract_violation");
        action = { kind: actionKind, ...parsed.data };
      } else if (actionKind === "PASS_REWARD") {
        const parsed = passReward.safeParse(body);
        if (!parsed.success) throw new ActivityError(400, "contract_violation");
        action = { kind: actionKind, ...parsed.data };
      } else if (actionKind === "PASS_XP") {
        const parsed = xp.safeParse(body);
        if (!parsed.success) throw new ActivityError(400, "contract_violation");
        action = { kind: actionKind, ...parsed.data };
      } else {
        const parsed = purchase.safeParse(body);
        if (!parsed.success) throw new ActivityError(400, "contract_violation");
        action = { kind: actionKind, ...parsed.data };
      }
      return c.json(await claimActivity(options, action, key));
    } catch (error) {
      if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
      throw error;
    }
  };
  app.get("/v1/attendance/:activityId", handler("ATTENDANCE"));
  app.post("/v1/attendance/:activityId/check-in", handler("ATTENDANCE", "CHECK_IN"));
  app.post("/v1/attendance/:activityId/recover", handler("ATTENDANCE", "RECOVER"));
  app.post("/v1/attendance/:activityId/milestones/claim", handler("ATTENDANCE", "MILESTONE"));
  app.get("/v1/passes/:activityId", handler("PASS"));
  app.post("/v1/passes/:activityId/rewards/claim", handler("PASS", "PASS_REWARD"));
  app.post("/v1/passes/:activityId/xp/claim", handler("PASS", "PASS_XP"));
  app.post("/v1/passes/:activityId/purchases/verify", handler("PASS", "PURCHASE"));
}


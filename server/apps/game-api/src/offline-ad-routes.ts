import type { Context, Hono } from "hono";
import { ActivityError, activityId } from "./activity-policy";
import { readBoundedBody, type ActivityAuthResult } from "./activity-routes";
import { createAdmobKeyResolver } from "./admob-keys";
import { verifyAdmobCallback } from "./admob-ssv";
import { parseOfflineAdPolicy } from "./offline-ad-policy";
import { createOfflineAdIntent, readOfflineAdCreationReplay, readOfflineAdIntent, recordOfflineAdVerification, spinOfflineAd } from "./offline-ad-service";
import { recordCombatAdVerification } from "./combat-ad-service";
import { parseCombatAdPolicy } from "./combat-ad-policy";
import {requireRewardClient,readRewardOpportunity,rewardContract} from './reward-opportunity-view';
import {adStartContract} from './ad-start-approval';

type Ctx = Context<{ Bindings: Env }>;
function rejected(c: Ctx, error: unknown) {
  if (error instanceof ActivityError) return c.json({ accepted: false, errorCode: error.code }, error.status);
  const code = error instanceof Error ? error.message : "";
  if (code === "ad_reward_policy_unconfigured") return c.json({ accepted: false, errorCode: code }, 503);
  if (["ad_ssv_malformed", "ad_ssv_verification_failed", "ad_ssv_binding_mismatch", "ad_ssv_unknown_key"].includes(code)) {
    return c.json({ accepted: false, errorCode: code }, 403);
  }
  // No raw query, challenge, account binding or low-level DB error is disclosed.
  return c.json({ accepted: false, errorCode: "ad_reward_temporarily_unavailable" }, 503);
}

export function registerOfflineAdRoutes(app: Hono<{ Bindings: Env }>, authenticate: (c: Ctx) => Promise<ActivityAuthResult>,
  trustedKeys = createAdmobKeyResolver()) {
  const handler = (action: "CREATE" | "READ" | "SPIN") => async (c: Ctx) => {
    c.header("Cache-Control", "no-store");
    const auth = await authenticate(c);
    if ("errorCode" in auth) return c.json({ accepted: false, errorCode: auth.errorCode }, auth.status);
    try {
      if (new URL(c.req.url).search || (await readBoundedBody(c)) !== "") throw new ActivityError(400, "contract_violation");
      const id = c.req.param("intentId") ?? "";
      if (action !== "CREATE" && !activityId.safeParse(id).success) throw new ActivityError(400, "contract_violation");
      if (action !== "CREATE") {
        const owner = await c.env.DB.prepare("SELECT environment FROM offline_ad_intents WHERE id=? AND account_id=?").bind(id, auth.accountId).first<{ environment: string }>();
        if (!owner || owner.environment !== c.env.ENVIRONMENT) throw new ActivityError(403, "ad_intent_not_found");
      }
      if(action!=='CREATE')await requireRewardClient(c.env.DB,'OFFLINE',id,c.req.header('X-Reward-Contract'));
      if (action === "READ") return c.json(c.req.header('X-Reward-Contract')===rewardContract?
        await readRewardOpportunity(c.env.DB,auth.accountId,c.env.ENVIRONMENT,'OFFLINE',id):await readOfflineAdIntent(c.env.DB, auth.accountId, id));
      const key = c.req.header("Idempotency-Key") ?? "";
      if (!/^[\x21-\x7e]{1,128}$/u.test(key)) throw new ActivityError(400, "idempotency_key_required");
      if (action === "SPIN") return c.json(await spinOfflineAd(c.env.DB, auth.accountId, id, key));
      if(c.req.header('X-Ad-Contract')!==adStartContract)throw new ActivityError(409,'ad_start_contract_required');
      const replay = await readOfflineAdCreationReplay(c.env.DB, auth.accountId, c.env.ENVIRONMENT, key);
      if (replay) {await requireRewardClient(c.env.DB,'OFFLINE',replay.intentId,c.req.header('X-Reward-Contract'));
        return c.json(c.req.header('X-Reward-Contract')===rewardContract?await readRewardOpportunity(c.env.DB,auth.accountId,c.env.ENVIRONMENT,'OFFLINE',replay.intentId):replay);}
      const row = await c.env.DB.prepare("SELECT version,config_json FROM offline_ad_policies WHERE environment=? AND status='ACTIVE'").bind(c.env.ENVIRONMENT).first<{ version: string; config_json: string }>();
      if (!row) throw new ActivityError(503, "ad_reward_policy_unconfigured");
      const policy = parseOfflineAdPolicy(JSON.parse(row.config_json));
      if (policy.version !== row.version) throw new ActivityError(503, "ad_reward_policy_unconfigured");
      const created=await createOfflineAdIntent(c.env.DB, auth.accountId, c.env.ENVIRONMENT, policy, key);
      await requireRewardClient(c.env.DB,'OFFLINE',created.intentId,c.req.header('X-Reward-Contract'));
      return c.json(c.req.header('X-Reward-Contract')===rewardContract?await readRewardOpportunity(c.env.DB,auth.accountId,c.env.ENVIRONMENT,'OFFLINE',created.intentId):created);
    } catch (error) { return rejected(c, error); }
  };
  app.post("/v1/offline-rewards/ad-intents", handler("CREATE"));
  app.get("/v1/offline-rewards/ad-intents/:intentId", handler("READ"));
  app.post("/v1/offline-rewards/ad-intents/:intentId/spin", handler("SPIN"));
  app.get("/v1/admob/ssv", async (c) => {
    c.header("Cache-Control", "no-store");
    try {
      const rawQuery = c.req.url.split("?").slice(1).join("?");
      if (rawQuery.length > 8192 || (await readBoundedBody(c)) !== "") throw new ActivityError(400, "contract_violation");
      const params = new URLSearchParams(rawQuery), challenge = params.get("custom_data");
      if (!challenge || challenge.length > 256) throw new ActivityError(403, "ad_intent_not_found");
      const offline = await c.env.DB.prepare("SELECT id,environment,user_binding,challenge,policy_json FROM offline_ad_intents WHERE challenge=?").bind(challenge).first<{
        id: string; environment: string; user_binding: string; challenge: string; policy_json: string;
      }>();
      const row = offline ?? await c.env.DB.prepare("SELECT id,environment,user_binding,challenge,policy_json FROM combat_ad_intents WHERE challenge=?").bind(challenge).first<{
        id: string; environment: string; user_binding: string; challenge: string; policy_json: string;
      }>();
      if (!row || row.environment !== c.env.ENVIRONMENT || params.get("user_id") !== row.user_binding) throw new ActivityError(403, "ad_intent_not_found");
      const policy = offline ? parseOfflineAdPolicy(JSON.parse(row.policy_json)) : parseCombatAdPolicy(JSON.parse(row.policy_json));
      const callback = await verifyAdmobCallback(rawQuery, trustedKeys, { adUnit: policy.adUnit, customData: row.challenge,
        userId: row.user_binding, rewardAmount: policy.rewardAmount, rewardItem: policy.rewardItem });
      return c.json(offline ? await recordOfflineAdVerification(c.env.DB, row.id, callback) : await recordCombatAdVerification(c.env.DB,row.id,callback));
    } catch (error) { return rejected(c, error); }
  });
}

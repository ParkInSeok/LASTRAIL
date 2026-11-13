import type { Context,Hono } from "hono";
import { ActivityError,activityId } from "./activity-policy";
import { readBoundedBody,type ActivityAuthResult } from "./activity-routes";
import { createCombatAdIntent,readCombatAdIntent,spinCombatAd } from "./combat-ad-service";
import {requireRewardClient,readRewardOpportunity,rewardContract} from './reward-opportunity-view';
import {adStartContract} from './ad-start-approval';
type Ctx=Context<{Bindings:Env}>;
export function registerCombatAdRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<ActivityAuthResult>){
  const handler=(action:"CREATE"|"READ"|"SPIN")=>async(c:Ctx)=>{
    c.header("Cache-Control","no-store");
    const auth=await authenticate(c);if("errorCode" in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
    try{
      if(new URL(c.req.url).search||(await readBoundedBody(c))!=="")throw new ActivityError(400,"contract_violation");
      const run=c.req.param("runId")!,preview=c.req.param("rewardPreviewId")!,intent=c.req.param("intentId")??"";
      for(const id of [run,preview,...(action==="CREATE"?[]:[intent])])if(!activityId.safeParse(id).success)throw new ActivityError(400,"contract_violation");
      if(action!=='CREATE'){
        await readCombatAdIntent(c.env.DB,auth.accountId,c.env.ENVIRONMENT,run,preview,intent);
        await requireRewardClient(c.env.DB,'COMBAT',intent,c.req.header('X-Reward-Contract'));
      }
      if(action==="READ")return c.json(c.req.header('X-Reward-Contract')===rewardContract?
        await readRewardOpportunity(c.env.DB,auth.accountId,c.env.ENVIRONMENT,'COMBAT',intent):await readCombatAdIntent(c.env.DB,auth.accountId,c.env.ENVIRONMENT,run,preview,intent));
      const key=c.req.header("Idempotency-Key")??"";
      if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,"idempotency_key_required");
      if(action==='SPIN')return c.json(await spinCombatAd(c.env.DB,auth.accountId,c.env.ENVIRONMENT,run,preview,intent,key));
      if(c.req.header('X-Ad-Contract')!==adStartContract)throw new ActivityError(409,'ad_start_contract_required');
      const created=await createCombatAdIntent(c.env.DB,auth.accountId,c.env.ENVIRONMENT,run,preview,key);
      await requireRewardClient(c.env.DB,'COMBAT',created.intentId,c.req.header('X-Reward-Contract'));
      return c.json(c.req.header('X-Reward-Contract')===rewardContract?await readRewardOpportunity(c.env.DB,auth.accountId,c.env.ENVIRONMENT,'COMBAT',created.intentId):created);
    }catch(error){
      if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);
      if(error instanceof Error&&error.message==="ad_reward_policy_unconfigured")return c.json({accepted:false,errorCode:error.message},503);
      return c.json({accepted:false,errorCode:"ad_reward_temporarily_unavailable"},503);
    }
  };
  const base="/v1/runs/:runId/reward-preview/:rewardPreviewId/ad-intents";
  app.post(base,handler("CREATE"));app.get(base+"/:intentId",handler("READ"));app.post(base+"/:intentId/spin",handler("SPIN"));
}

import {maintenanceAdmissionStatement,maintenanceContext,maintenanceAdmissionError} from './maintenance-admission';
import {prepareRewardDisclosure,rewardDisclosureView,type RewardDisclosurePinRow} from './reward-disclosure-pin';
import { ActivityError } from "./activity-policy";
import {requireNewAd} from './ad-runtime-policy';
import {requireAdStartForCallback} from './ad-start-approval';
import { parseCombatAdPolicy,parseCombatRewardPolicy } from "./combat-ad-policy";
import {readRewardApproval,ssvApprovalStatement} from './reward-opportunity-approval';
import { combatRouletteMultiplier, multiplyCombatRewards, isCombatRouletteEligible, type RewardLine } from "./combat-roulette-policy";
import { providerReceiptStatement,existingProviderReceipt } from "./ad-provider-receipts";
import { settleRunReward, type SettlementPreview } from "./run-reward-settlement";
import type { VerifiedAdmobCallback } from "./admob-ssv";

type Preview=SettlementPreview & {status:string;outcome:string;run_status:string};
export type CombatIntent=RewardDisclosurePinRow & {id:string;account_id:string;run_id:string;preview_id:string;environment:string;
  created_at:number;expires_at:number|null;callback_deadline:number|null;policy_json:string;base_json:string;balance_version:number;
  challenge:string|null;user_binding:string|null;status:string;provider_transaction_id:string|null;multiplier:2|3|4|null;result_json:string|null;origin:'AD_REQUEST'|'PURCHASE_ONLY'};
const fail=(code:string):never=>{throw new ActivityError(409,code);};
export async function retryCombatAd<T>(action:()=>Promise<T>):Promise<T>{
  for(let n=0;n<4;n++) try{return await action();}catch(error){
    if(String(error).includes('reward_disclosure_changed'))throw new ActivityError(409,'reward_disclosure_policy_changed');
    if(String(error).includes('ad_runtime_policy_blocked'))throw new ActivityError(409,'ad_runtime_policy_changed');
    if(!/UNIQUE constraint failed|CHECK constraint failed: valid=1/iu.test(String(error)))throw error;
    if(n===3)fail("ad_reward_state_conflict");
  }
  return fail("ad_reward_state_conflict");
}
async function preview(db:D1Database,account:string,run:string,id:string){
  const row=await db.prepare(`SELECT p.*,f.outcome,r.status AS run_status FROM run_reward_previews p
    JOIN runs r ON r.id=p.run_id AND r.account_id=p.account_id
    JOIN run_final_results f ON f.run_id=p.run_id WHERE p.id=? AND p.run_id=? AND p.account_id=?`)
    .bind(id,run,account).first<Preview>();
  if(!row)throw new ActivityError(403,"reward_preview_not_found");
  return row;
}
function requireAvailable(p:Preview){
  if(p.status!=="ACTIVE")fail("reward_preview_not_claimable");
  if(p.expires_at<=Date.now())fail("reward_preview_expired");
  if(p.outcome!=="VICTORY"||p.run_status!=="COMPLETED")fail("ad_clear_required");
}
export async function readCombatAdIntent(db:D1Database,account:string,environment:string,run:string,previewId:string,id:string){
  const row=await db.prepare("SELECT * FROM combat_ad_intents WHERE id=? AND account_id=? AND environment=? AND run_id=? AND preview_id=?")
    .bind(id,account,environment,run,previewId).first<CombatIntent>();
  if(!row)throw new ActivityError(403,"ad_intent_not_found");
  const policy=parseCombatRewardPolicy(JSON.parse(row.policy_json));
  const p=await preview(db,account,run,previewId);
  const status=row.status==="CLAIMED"?"CLAIMED":p.status==="CLAIMED"?"REWARD_CONSUMED":
    p.status!=="ACTIVE"||p.outcome!=="VICTORY"||p.run_status!=="COMPLETED"?"UNAVAILABLE":p.expires_at<=Date.now()||(row.status==="AD_PENDING"&&row.callback_deadline!==null&&Date.now()>row.callback_deadline)?"EXPIRED":
    p.reward_plan_json!==row.base_json||p.balance_config_version!==row.balance_version?"PREVIEW_CHANGED":row.status;
  const baseLines=(JSON.parse(row.base_json) as {rewardLines:RewardLine[]}).rewardLines;
  return {accepted:true,probabilityDisclosure:rewardDisclosureView(row),intentId:row.id,runId:run,rewardPreviewId:previewId,status,
    eligibleBaseRewardLines:baseLines.filter(isCombatRouletteEligible),excludedBaseRewardLines:baseLines.filter(line=>!isCombatRouletteEligible(line)),
    canClaimBasic:p.status==="ACTIVE"&&p.expires_at>Date.now(),
    canSpin:status==="AD_VERIFIED"||status==="RESULT_FIXED",createdAt:row.created_at,expiresAt:row.expires_at,
    callbackDeadline:row.callback_deadline,serverNow:Date.now(),userId:row.user_binding,customData:row.challenge,
    adUnit:row.origin==='AD_REQUEST'?parseCombatAdPolicy(JSON.parse(row.policy_json)).adUnit:null,policyVersion:policy.version,probabilities:[{multiplier:2,weightBps:6000},{multiplier:3,weightBps:3000},{multiplier:4,weightBps:1000}],
    result:row.result_json?JSON.parse(row.result_json):null};
}
export async function createCombatAdIntent(db:D1Database,account:string,environment:string,run:string,id:string,key:string,purchasePolicy?:unknown):Promise<Awaited<ReturnType<typeof readCombatAdIntent>>>{
  return retryCombatAd(async()=>{
    const prior=await db.prepare(`SELECT i.* FROM combat_ad_create_receipts c JOIN combat_ad_intents i ON i.id=c.intent_id WHERE c.account_id=? AND c.idempotency_key=?`).bind(account,key).first<CombatIntent>();
    if(prior){
      if(prior.run_id!==run||prior.preview_id!==id||prior.environment!==environment)fail("idempotency_conflict");
      return readCombatAdIntent(db,account,environment,run,id,prior.id);
    }
    const p=await preview(db,account,run,id);requireAvailable(p);
    const existing=await db.prepare("SELECT * FROM combat_ad_intents WHERE preview_id=? AND account_id=?").bind(id,account).first<CombatIntent>();
    if(existing){
      if(existing.environment!==environment)throw new ActivityError(403,"ad_intent_not_found");
      await db.batch([db.prepare("INSERT INTO combat_ad_create_receipts VALUES(?,?,?)").bind(account,key,existing.id)]);
      return readCombatAdIntent(db,account,environment,run,id,existing.id);
    }
    try{
    if(!purchasePolicy)await requireNewAd(db,environment);
    const active=purchasePolicy?{version:parseCombatRewardPolicy(purchasePolicy).version,config_json:JSON.stringify(purchasePolicy)}:
      await db.prepare("SELECT version,config_json FROM combat_ad_policies WHERE environment=? AND status='ACTIVE'").bind(environment).first<{version:string;config_json:string}>();
    if(!active)throw new ActivityError(503,"ad_reward_policy_unconfigured");
    const policy=parseCombatRewardPolicy(JSON.parse(active.config_json)),delivery=purchasePolicy?null:parseCombatAdPolicy(JSON.parse(active.config_json));
    if(policy.version!==active.version||policy.environment!==environment)throw new ActivityError(503,"ad_reward_policy_unconfigured");
    const plan=JSON.parse(p.reward_plan_json);
    if(!Array.isArray(plan.rewardLines))fail("ad_reward_snapshot_invalid");
    const doubled=multiplyCombatRewards(plan.rewardLines,2);
    if(!doubled.some((line,index)=>line.quantity>plan.rewardLines[index].quantity))fail("ad_reward_not_eligible");
    const now=Date.now(),expires=delivery?now+delivery.challengeLifetimeMs:null,deadline=delivery?expires!+delivery.callbackGraceMs:null;
    if(delivery&&(!Number.isSafeInteger(deadline)||deadline!>=p.expires_at))fail("ad_preview_lifetime_insufficient");
    const offset=(delivery?.dayStartUtcMinutes??0)*60000,day=Math.floor((now-offset)/86400000)*86400000+offset;
    const count=await db.prepare("SELECT count(*) AS n FROM combat_ad_intents WHERE account_id=? AND environment=? AND created_at>=? AND origin='AD_REQUEST'").bind(account,environment,day).first<{n:number}>();
    if(delivery&&count!.n>=delivery.maxAdsPerDay)fail("ad_daily_limit_reached");
    const disclosure=await prepareRewardDisclosure(db,environment,'COMBAT',policy,purchasePolicy?'PURCHASE_ONLY':'AD_REQUEST');
    const intent="combat_ad_"+crypto.randomUUID();
    await db.batch([
      maintenanceAdmissionStatement(db,maintenanceContext(environment)),
      disclosure.guard,
      ...(delivery?[db.prepare(`INSERT INTO combat_ad_guard SELECT CASE WHEN
        (SELECT count(*) FROM combat_ad_intents WHERE account_id=? AND environment=? AND created_at>=? AND origin='AD_REQUEST')<?
        AND EXISTS(SELECT 1 FROM run_reward_previews WHERE id=? AND account_id=? AND status='ACTIVE' AND reward_plan_json=? AND expires_at>?)
        AND EXISTS(SELECT 1 FROM combat_ad_policies WHERE version=? AND environment=? AND status='ACTIVE' AND config_json=?)
        THEN 1 ELSE 0 END`).bind(account,environment,day,delivery.maxAdsPerDay,id,account,p.reward_plan_json,deadline,active.version,environment,active.config_json)]:[]),
      db.prepare(`INSERT INTO combat_ad_intents(id,account_id,run_id,preview_id,environment,created_at,expires_at,callback_deadline,policy_json,base_json,balance_version,challenge,user_binding,status,origin,write_revision,probability_disclosure_id,probability_source_digest)
        VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,'AD_PENDING',?,1,?,?)`).bind(intent,account,run,id,environment,now,expires,deadline,JSON.stringify(policy),p.reward_plan_json,p.balance_config_version,delivery?crypto.randomUUID():null,delivery?crypto.randomUUID():null,delivery?'AD_REQUEST':'PURCHASE_ONLY',disclosure.disclosureId,disclosure.sourceDigest),
      db.prepare("INSERT INTO combat_ad_create_receipts VALUES(?,?,?)").bind(account,key,intent),
      db.prepare("DELETE FROM combat_ad_guard"),
      db.prepare("DELETE FROM reward_disclosure_guard"),
    ]);
    return readCombatAdIntent(db,account,environment,run,id,intent);
    }catch(error){
      const committed=await db.prepare("SELECT i.* FROM combat_ad_create_receipts c JOIN combat_ad_intents i ON i.id=c.intent_id WHERE c.account_id=? AND c.idempotency_key=?")
        .bind(account,key).first<CombatIntent>();
      if(committed){
        if(committed.run_id!==run||committed.preview_id!==id||committed.environment!==environment)fail('idempotency_conflict');
        return readCombatAdIntent(db,account,environment,run,id,committed.id);
      }
      const winner=await db.prepare("SELECT * FROM combat_ad_intents WHERE preview_id=? AND account_id=?").bind(id,account).first<CombatIntent>();
      if(winner){
        if(winner.environment!==environment||winner.run_id!==run)throw new ActivityError(403,'ad_intent_not_found');
        await db.prepare("INSERT OR IGNORE INTO combat_ad_create_receipts VALUES(?,?,?)").bind(account,key,winner.id).run();
        return createCombatAdIntent(db,account,environment,run,id,key,purchasePolicy);
      }
      throw maintenanceAdmissionError(error)??error;
    }
  });
}
/** Internal only: verified cryptographic callback from the SSV adapter. */
export async function recordCombatAdVerification(db:D1Database,id:string,callback:VerifiedAdmobCallback){
  return retryCombatAd(async()=>{
    const row=await db.prepare("SELECT * FROM combat_ad_intents WHERE id=?").bind(id).first<CombatIntent>();
    if(!row)throw new ActivityError(403,"ad_intent_not_found");
    const policy=parseCombatAdPolicy(JSON.parse(row.policy_json));
    if(callback.provider!=="ADMOB"||callback.adUnit!==policy.adUnit||callback.userId!==row.user_binding||callback.customData!==row.challenge)fail("ad_ssv_binding_mismatch");
    if(await existingProviderReceipt(db,callback.transactionId,'COMBAT',id,row.account_id))return {accepted:true};
    if(row.provider_transaction_id===callback.transactionId)return {accepted:true};
    const approval=await readRewardApproval(db,'COMBAT',id),observation=approval?.cause==='PURCHASE_ENTITLEMENT';
    if(row.status!=="AD_PENDING"&&!observation)fail("ad_intent_already_verified");
    if(row.expires_at===null||row.callback_deadline===null||callback.timestamp<row.created_at||callback.timestamp>row.expires_at||Date.now()>row.callback_deadline)fail("ad_verification_expired");
    await requireAdStartForCallback(db,'COMBAT',id,row.account_id,row.environment,callback.timestamp);
    await db.batch([
      db.prepare(`INSERT INTO combat_ad_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_ad_intents WHERE id=? AND (status='AD_PENDING' OR EXISTS(SELECT 1 FROM reward_opportunity_approvals a WHERE a.consumer='COMBAT' AND a.intent_id=combat_ad_intents.id AND a.cause='PURCHASE_ENTITLEMENT'))
        AND callback_deadline>=CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)) THEN 1 ELSE 0 END`).bind(id),
      providerReceiptStatement(db,callback.transactionId,"COMBAT",id,row.account_id),
      ...(!observation?[ssvApprovalStatement(db,'COMBAT',id,row.account_id,row.environment,callback.transactionId),
        db.prepare("UPDATE combat_ad_intents SET status='AD_VERIFIED',provider_transaction_id=?,write_revision=write_revision+1 WHERE id=?").bind(callback.transactionId,id)]:[]),
      db.prepare("DELETE FROM combat_ad_guard"),
    ]);
    return {accepted:true};
  });
}
export async function spinCombatAd(db:D1Database,account:string,environment:string,run:string,id:string,intent:string,key:string){
  return retryCombatAd(async()=>{
    const state=await readCombatAdIntent(db,account,environment,run,id,intent);
    const prior=await db.prepare("SELECT intent_id FROM combat_ad_spin_receipts WHERE account_id=? AND idempotency_key=?").bind(account,key).first<{intent_id:string}>();
    if(prior&&prior.intent_id!==intent)fail("idempotency_conflict");
    if(!prior)await db.batch([db.prepare("INSERT INTO combat_ad_spin_receipts VALUES(?,?,?)").bind(account,key,intent)]);
    if(state.status==="CLAIMED")return state.result;
    if(!state.canSpin)fail(state.status==="AD_PENDING"?"ad_verification_pending":"ad_reward_not_claimable");
    const p=await preview(db,account,run,id);requireAvailable(p);
    if(state.status==="AD_VERIFIED"){
      const sample=new Uint32Array(1);do{crypto.getRandomValues(sample);}while(sample[0]!>=4294960000);
      // Persist before settlement; a failed payout or new key must never redraw.
      await db.batch([db.prepare("UPDATE combat_ad_intents SET status='RESULT_FIXED',multiplier=?,write_revision=write_revision+1 WHERE id=? AND status='AD_VERIFIED'")
        .bind(combatRouletteMultiplier(sample[0]!%10000),intent)]);
    }
    await settleRunReward(db,p,"combat-ad:"+intent,intent);
    const restored=await readCombatAdIntent(db,account,environment,run,id,intent);
    if(restored.status!=="CLAIMED")fail("ad_reward_already_consumed");
    // The intent is the stable logical operation; a new key cannot redraw.
    return restored.result;
  });
}


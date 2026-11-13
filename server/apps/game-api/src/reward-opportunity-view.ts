import { ActivityError } from './activity-policy';
import {readOfflineAdIntent} from './offline-ad-service';
import {readCombatAdIntent} from './combat-ad-service';
import {readRewardApproval,type RewardConsumer} from './reward-opportunity-approval';
export const rewardContract='reward-opportunity-v2';
export async function readRewardOpportunity(db:D1Database,account:string,environment:string,consumer:RewardConsumer,intent:string){
 const table=consumer==='OFFLINE'?'offline_ad_intents':'combat_ad_intents';
 const row=await db.prepare(`SELECT * FROM ${table} WHERE id=? AND account_id=? AND environment=?`).bind(intent,account,environment)
  .first<{origin:string;created_at:number;expires_at:number|null;callback_deadline:number|null;user_binding:string|null;challenge:string|null;
    base_json:string;policy_json:string;run_id:string;preview_id:string}>();
 if(!row)throw new ActivityError(403,'ad_intent_not_found');
 const view=consumer==='OFFLINE'?await readOfflineAdIntent(db,account,intent):await readCombatAdIntent(db,account,environment,row.run_id,row.preview_id,intent);
 const approval=await readRewardApproval(db,consumer,intent);
 const {userId,customData,adUnit,...common}=view;
 return {...common,contractVersion:rewardContract,
  approval:approval?{cause:approval.cause,approvedAt:approval.approved_at}:null,
  adContext:row.origin==='AD_REQUEST'?{userId,customData,adUnit,
   expiresAt:row.expires_at,callbackDeadline:row.callback_deadline}:null,
  ...(consumer==='OFFLINE'?{baseRewardLines:JSON.parse(row.base_json) as Array<{id:string;quantity:number;kind?:string;source?:string}>}:{}),
 };
}
export async function requireRewardClient(db:D1Database,consumer:RewardConsumer,id:string,contract:string|undefined){
 const table=consumer==='OFFLINE'?'offline_ad_intents':'combat_ad_intents';
 const row=await db.prepare(`SELECT origin FROM ${table} WHERE id=?`).bind(id).first<{origin:string}>();
 if(row?.origin==='PURCHASE_ONLY'&&contract!==rewardContract)throw new ActivityError(409,'reward_contract_required');
}

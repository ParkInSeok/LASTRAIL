import {ActivityError} from './activity-policy';
import {parseOfflineRewardPolicy} from './offline-ad-policy';
import {parseCombatRewardPolicy} from './combat-ad-policy';
/** Shared authoritative purchase-policy validation for execution and public projection. */
export function parsePurchaseRewardPolicy(value:unknown,environment:string,kind:'OFFLINE'|'COMBAT'){
 try{
  const p=kind==='OFFLINE'?parseOfflineRewardPolicy(value):parseCombatRewardPolicy(value);
  if(p.environment!==environment||p.weightsBps.join(',')!=='6000,3000,1000')throw new Error('invalid');
  return p;
 }catch{throw new ActivityError(503,'ad_reward_policy_unconfigured');}
}
export async function readPurchaseRewardPolicy(db:D1Database,environment:string,kind:'OFFLINE'|'COMBAT'){
 const row=await db.prepare('SELECT config_json FROM purchase_reward_policies WHERE environment=? AND kind=?').bind(environment,kind).first<{config_json:string}>();
 if(!row)return null;
 let value:unknown;
 try{value=JSON.parse(row.config_json);}catch{throw new ActivityError(503,'ad_reward_policy_unconfigured');}
 return parsePurchaseRewardPolicy(value,environment,kind);
}

import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {readAdSkipEntitlement} from './ad-skip-entitlement';
import {adSkipHash,adSkipReplay,adSkipReceipt,type AdSkipActor} from './ad-skip-storage';
import {createOfflineAdIntent,readOfflineAdCreationReplay} from './offline-ad-service';
import {createCombatAdIntent} from './combat-ad-service';
import {authorizeRewardOpportunity} from './reward-opportunity-approval';
import {readRewardOpportunity} from './reward-opportunity-view';
import {readPurchaseRewardPolicy} from './purchase-reward-policy';
const id=z.string().min(1).max(200);
export const skipOpportunitySchema=z.discriminatedUnion('kind',[
 z.strictObject({kind:z.literal('OFFLINE'),intentId:id.optional()}),
 z.strictObject({kind:z.literal('COMBAT'),runId:id,rewardPreviewId:id,intentId:id.optional()}),
 z.strictObject({kind:z.literal('REROLL')})]);
type Opportunity=z.infer<typeof skipOpportunitySchema>;
export async function useAdSkip(o:AdSkipActor,opportunity:Opportunity,key:string){
 if(opportunity.kind==='REROLL')throw new ActivityError(503,'reroll_contract_unavailable');
 const kind=opportunity.kind,hash=await adSkipHash({...opportunity,intentId:opportunity.intentId??null});
 const replay=await adSkipReplay(o,'AUTHORIZE',key,hash);
 if(replay){await authorizeRewardOpportunity(o,kind,String(replay.intentId));return readRewardOpportunity(o.db,o.accountId,o.environment,kind,String(replay.intentId));}
 let intent=opportunity.intentId;
 if(!intent){
  const entitlement=await readAdSkipEntitlement(o);
  // Persist a creation receipt before approval: retry cannot drift to a new baseline.
  if(kind==='OFFLINE')intent=(await readOfflineAdCreationReplay(o.db,o.accountId,o.environment,'purchase:'+key))?.intentId;
  if(!intent){
   if(!entitlement||entitlement.status!=='ACTIVE')throw new ActivityError(403,entitlement?'ad_skip_entitlement_revoked':'ad_skip_entitlement_required');
   const policy=await readPurchaseRewardPolicy(o.db,o.environment,kind);
   if(!policy)throw new ActivityError(503,'ad_reward_policy_unconfigured');
   intent=kind==='OFFLINE'?(await createOfflineAdIntent(o.db,o.accountId,o.environment,policy,'purchase:'+key,'PURCHASE_ONLY')).intentId:
    (await createCombatAdIntent(o.db,o.accountId,o.environment,opportunity.runId,opportunity.rewardPreviewId,'purchase:'+key,policy)).intentId;
  }
 }
 if(kind==='COMBAT'){
  const row=await o.db.prepare('SELECT 1 FROM combat_ad_intents WHERE id=? AND run_id=? AND preview_id=? AND account_id=? AND environment=?')
   .bind(intent,opportunity.runId,opportunity.rewardPreviewId,o.accountId,o.environment).first();
  if(!row)throw new ActivityError(403,'ad_intent_not_found');
 }
 try{await o.db.batch([adSkipReceipt(o,'AUTHORIZE',key,hash,{intentId:intent})]);}
 catch(e){if(!await adSkipReplay(o,'AUTHORIZE',key,hash))throw e;}
 await authorizeRewardOpportunity(o,kind,intent);
 return readRewardOpportunity(o.db,o.accountId,o.environment,kind,intent);
}

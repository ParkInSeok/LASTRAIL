import {z} from 'zod';
import {approvedKyleShieldAbilities} from './combat-shield-budget';
const id=z.string().min(1).max(400),nat=z.number().int().nonnegative().safe();
const request=z.strictObject({recipientId:id,atMs:nat,layers:z.array(z.strictObject({layerId:id,scope:z.literal('PERSONAL'),recipientId:id,
 sourceId:id,creationEventId:id,amount:nat,expiresAtMs:nat.nullable()})).max(1000),ownerOrder:z.array(id).max(1000).nullable()});
/** Pure order projection of an ALREADY damage-kind/consume-eligible personal set
 * supplied by its original owner. Does not grant, spend or authenticate a layer.
 * Shared pipeline ordering, source caps and reapplication remain original-owner
 * contracts. Null expiry is accepted only when actually supplied by that owner. */
export function orderPersonalShieldLayers(input:unknown):string[]{
 const c=request.parse(input),ids=new Set(c.layers.map(l=>l.layerId));
 if(ids.size!==c.layers.length||c.layers.some(l=>l.recipientId!==c.recipientId))throw Error('shield_personal_set_invalid');
 const live=c.layers.filter(l=>l.amount>0&&(l.expiresAtMs===null||l.expiresAtMs>c.atMs));
 if(c.ownerOrder!==null){
  if(c.ownerOrder.length!==ids.size||new Set(c.ownerOrder).size!==ids.size||c.ownerOrder.some(id=>!ids.has(id)))throw Error('shield_owner_order_invalid');
  const liveIds=new Set(live.map(l=>l.layerId));return c.ownerOrder.filter(id=>liveIds.has(id));
 }
 const seen=new Set<string>();
 for(const l of live){const key=JSON.stringify([l.expiresAtMs,l.creationEventId]);if(seen.has(key))throw Error('shield_order_source_ambiguous');seen.add(key);}
 return live.sort((a,b)=>{
  if(a.expiresAtMs!==b.expiresAtMs)return a.expiresAtMs===null?1:b.expiresAtMs===null?-1:a.expiresAtMs-b.expiresAtMs;
  return a.creationEventId<b.creationEventId?-1:a.creationEventId>b.creationEventId?1:0;
 }).map(l=>l.layerId);
}
export function captureShieldLayerContract(){return {
 version:'combat.personal-shield-order.v1.20261019',approval:'31#shield-layer-approved-20261019',runtimeIntegrated:false,
 input:'ORIGINAL_OWNER_FILTERED_PERSONAL_SET',ordering:'OWNER_EXPLICIT_ELSE_EXPIRY_THEN_CREATION_EVENT',
 globalCap:null,globalCapMeaning:'NO_NEW_GLOBAL_CAP_OR_INFERRED_SOURCE_CAP',sharedPriority:'ORIGINAL_DEFENSE_PIPELINE',
 sameSource:'ORIGINAL_DEFINITION_REAPPLICATION',kyle:structuredClone(approvedKyleShieldAbilities),
 kyleSameSource:'REPLACE_IF_NEW_AMOUNT_AT_LEAST_REMAINING_OTHERWISE_NO_CHANGE',
 o15:{personalOnly:true,remainingBps:2000,actorMaxHpCapBps:500,baseAttackBudgetCapBps:5000,O02Eligible:true,
  otherDerived:'EXPLICIT_ORIGINAL_CONTRACT_REQUIRED',consumeAt:'ORIGINAL_FIRE_COMMIT',missRefund:false},
 contribution:{HIT_ABSORPTION:'DEFENSE',O15_CONSUMPTION:'ATTACK_ENHANCEMENT',EXPIRY:'NONE',TRANSFER:'PRESERVE_ORIGIN_NOT_NEW_GRANT'},
 orderingAuthority:'ORIGINAL_SEQUENCE_ATOMIC_REMAINING',restore:'ORIGINAL_REMAINING_EXPIRY_AND_RECEIPT_NOT_REISSUE',
 missingBindings:['ALL_SOURCE_CAPS_AND_REAPPLICATION','ORIGINAL_DAMAGE_KIND_ELIGIBILITY','RUNTIME_LAYER_OWNER_AND_JOURNAL','PLAYER_VALIDATION']
} as const;}

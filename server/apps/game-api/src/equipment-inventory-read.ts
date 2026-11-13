import type {Context,Hono} from 'hono';
import {ActivityError} from './activity-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {EquipmentInventoryStore} from './equipment-inventory-start';
import {EquipmentEffectStateStore} from './equipment-effect-state';
import {calculateEquipmentCoreStats,equipmentCoreStatVersion} from './equipment-run-stat-owner';
import {parseMultishotDefinition} from './equipment-multishot-definition';
import names from './equipment-presentation-names';

export const inventoryDisplayVersion='equipment.inventory-display.v1.20261111';
const categories:Record<string,{name:string;iconKey:string}>={
 HP:{name:'체력 장비',iconKey:'equipment.category.hp'},
 ATK:{name:'공격력 장비',iconKey:'equipment.category.atk'},
 DEF:{name:'방어력 장비',iconKey:'equipment.category.def'},
};
export async function readEquipmentInventory(db:D1Database,accountId:string,runId:string){
 const inventory=new EquipmentInventoryStore(db),effects=new EquipmentEffectStateStore(db);
 const value=await inventory.readInventory(accountId,runId);
 if(!value)throw new ActivityError(409,'equipment_inventory_unavailable');
 const items=[];
 for(const item of value.items){
  const label=categories[item.category_id],state=await effects.read(accountId,runId,item.instance_id);
  if(state&&(state.inventoryRevision!==value.revision||state.instanceRevision!==item.revision))throw new ActivityError(409,'equipment_inventory_changed');
  const descriptions:string[]=[];
  // Only the original implemented stat version supplies numerical descriptions.
  if(label&&item.definition_version===equipmentCoreStatVersion){
   const stats=calculateEquipmentCoreStats({maxHp:10000,attack:{numerator:'1',denominator:'1'},defense:{numerator:'1',denominator:'1'}},[{categoryId:item.category_id,star:item.star,setTag:'SET06'}]);
   descriptions.push(label.name.replace(' 장비','')+' +'+stats.additiveBps[item.category_id as 'HP'|'ATK'|'DEF']/100+'%');
  }else descriptions.push('기본 효과 설명 확인 필요');
  if(state){
   for(const effect of state.snapshot.uniqueEffects)descriptions.push((names as Record<string,string>)[effect.effectId]+' · 등급 '+effect.grade);
   const enchant=state.snapshot.enchantment;
   if(enchant?.effectId==='EN17')descriptions.push('다중 발사 · 추가 피해 '+parseMultishotDefinition(enchant.definitionSnapshot).coefficientBps/100+'% · 시술 당시 ★'+enchant.appliedStar);
   else if(enchant)descriptions.push((names as Record<string,string>)[enchant.effectId]+' · 시술 당시 ★'+enchant.appliedStar);
   if(!state.snapshot.uniqueEffects.length)descriptions.push('고유효과 없음');
   if(!enchant)descriptions.push('인챈트 없음');
  }else descriptions.push('고유효과·인챈트 정보 확인 필요');
  items.push({...item,presentation:{name:label?.name??'종류 미확인 장비',iconKey:label?.iconKey??null,effects:descriptions,effectRevision:state?.effectRevision??null}});
 }
 const after=await inventory.readPinnedCapacity(accountId,runId);
 if(after?.revision!==value.revision)throw new ActivityError(409,'equipment_inventory_changed');
 return {version:inventoryDisplayVersion,accountId,runId,...value,items};
}
export function registerEquipmentInventoryRead(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){
 app.get('/v1/runs/:runId/equipment',async c=>{
  c.header('Cache-Control','no-store');const auth=await authenticate(c);if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{
   if(new URL(c.req.url).search||(await readBoundedBody(c)).length)throw new ActivityError(400,'contract_violation');
   const result=await readEquipmentInventory(c.env.DB,auth.accountId,c.req.param('runId'));
   const current=await authenticate(c);if('errorCode' in current)return c.json({accepted:false,errorCode:current.errorCode},current.status);
   if(current.accountId!==auth.accountId)throw new ActivityError(409,'equipment_inventory_auth_changed');
   return c.json(result);
  }catch(error){if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);throw error;}
 });
}

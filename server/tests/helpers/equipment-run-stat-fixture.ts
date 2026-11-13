import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {namedFixture} from './combat-b-named-operator-fixture';
import {EquipmentEffectOriginalStore} from '../../apps/game-api/src/equipment-effect-original';
import {EquipmentPlacementStore} from '../../apps/game-api/src/equipment-placement';
import {EquipmentHealthPlacementStore} from '../../apps/game-api/src/equipment-health-placement';
import {CombatHealthStore} from '../../apps/game-api/src/combat-health-store';
import {RunEquipmentStatOwner,equipmentCoreStatVersion} from '../../apps/game-api/src/equipment-run-stat-owner';
export async function equipmentStatFixture(){
 const f=await namedFixture();const base=await f.capture();
 const q=z.object({operationPreviewId:z.string().min(1)}).parse(await(await f.preview({operationId:'opn_01_last_departure',risk:'I',expectedFormationRevision:1,expectedLoadoutRevision:1,expectedUltimateRevision:0})).json());const result=z.object({runId:z.string().min(1)}).parse(await(await f.start(q.operationPreviewId,'LOCAL_STAT_START')).json());const runId=result.runId;
 const originalSnapshot=String(f.scalar('SELECT snapshot_json FROM run_snapshots'));
 const origin={version:equipmentCoreStatVersion,accountId:'a',runId,stage:'ORIGINAL_PRE_EQUIPMENT_RUN_BASE',actors:base.actors.map(a=>({wearerKind:'operator',wearerId:a.actorId,targetId:a.actorId,base:{maxHp:a.permanentStats!.maxHp,attack:a.permanentStats!.attack,defense:{numerator:String(a.permanentStats!.defense),denominator:'1'}}}))};
 const originalHash=await sha256Hex(canonicalJson(origin));
 const guard=()=>f.env.DB.prepare(`INSERT INTO equipment_placement_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id='a' AND r.status='ACTIVE' AND s.snapshot_json=?) THEN 1 ELSE 0 END`).bind(runId,originalSnapshot);
 // LOCAL original base/eligibility proof; no operating START adapter is installed.
 const owner=new RunEquipmentStatOwner(f.env.DB,{prepare:async(accountId,id)=>{if(accountId!=='a'||id!==runId)throw new Error('LOCAL_ORIGINAL_OWNER_MISMATCH');return {origin:structuredClone(origin),originalHash,assertions:[guard()]};}});
 const equipment=new EquipmentPlacementStore(f.env.DB,{prepare:async()=>({assertions:[guard()]})});
 const health=new CombatHealthStore(f.env.DB,{prepare:async()=>({assertions:[f.env.DB.prepare(`INSERT INTO combat_damage_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs WHERE id=? AND account_id='a' AND status='ACTIVE') THEN 1 ELSE 0 END`).bind(runId)]})});
 await health.initialize('a',runId,[{id:'op_kyle',hp:700,maxHp:1400},{id:'op_sable',hp:0,maxHp:1600}]);
 const issue=async(instanceId:string,categoryId:string,setTag='SET05',star=1)=>{
  const original={version:'equipment.effect-original.v1',equipment:{definitionId:'LOCAL_'+categoryId,definitionVersion:equipmentCoreStatVersion,star},uniqueEffects:star===3?[{effectId:'EQ01',grade:1,definitionVersion:equipmentCoreStatVersion,definitionSnapshot:{definitionOnly:true}}]:[],enchantment:null};
  const effects=new EquipmentEffectOriginalStore(f.env.DB,{prepare:async()=>({snapshot:original,assertions:[f.env.DB.prepare('INSERT INTO equipment_effect_original_write_guard(valid) VALUES(1)')]})});
  const prepared=await effects.prepare('a',runId,{instanceId,sourceItemId:'LOCAL_SOURCE_'+instanceId,expectedInventoryRevision:Number(f.scalar('SELECT revision FROM run_equipment_inventory'))});
  await f.env.DB.batch([f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision) VALUES(?,?,?,?,?,?,?,?,0,'STORED',1)`).bind(instanceId,runId,'LOCAL_SOURCE_'+instanceId,'LOCAL_'+categoryId,equipmentCoreStatVersion,categoryId,setTag,star),...prepared.statements]);
 };
 await issue('hp-1','HP','SET04');await issue('atk-1','ATK','SET01');await issue('hp-2','HP','SET04');
 const store=new EquipmentHealthPlacementStore(f.env.DB,equipment,health,owner);
 const move=(instanceId:string,commandId:string,target:{kind:'operator';wearerId:string;slotIndex:number}|null,expectedInventoryRevision=1,expectedInstanceRevision=1,expectedHealthRevision=1)=>({placement:{commandId,instanceId,expectedInventoryRevision,expectedInstanceRevision,target,expectedTargetInstanceId:null},expectedHealthRevision,expectedStatVersion:equipmentCoreStatVersion});
 const item=(instanceId:string)=>{const row=f.sql.exec("SELECT definition_id,definition_version,category_id,set_tag,star FROM run_equipment_instances WHERE instance_id='"+instanceId+"'")[0]!.values[0]!;return {instanceId,definitionId:String(row[0]),definitionVersion:String(row[1]),categoryId:String(row[2]),setTag:String(row[3]),star:Number(row[4])};};
 return {...f,runId,originalSnapshot,origin,owner,equipment,health,store,move,item,issue};
}
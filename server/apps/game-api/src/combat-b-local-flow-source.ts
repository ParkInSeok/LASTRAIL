import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readOwnedCombatProfile} from './combat-owned-profile';
import {publishCombatDefinitionContract} from './combat-definition-contract';
import {buffDepartureSchema} from './combat-b-carriage-buffs';
export const localSourceVersion='combat.b-local-flow-source.v1.20261003';
const json=(x:unknown)=>canonicalJson(x as Parameters<typeof canonicalJson>[0]);
const digest=async(x:unknown)=>'sha256:'+await sha256Hex(json(x));
export type RefitRead=Awaited<ReturnType<import('./combat-b-refit-store').BRefitStore['read']>>;
type Exact={numerator:string;denominator:string};
type Entity={id:string;kind:string;definitionId:string;slot:number;maxHp:number;attack:Exact|null;defense:Exact;basicIntervalMicroseconds:number;attackSources:unknown[];defenseSources:unknown[];effects:unknown[];weakening:unknown[];penetration:unknown[];reductions:{equipment:unknown[];guardStance:unknown[];independent:unknown[]};allowedTargets:string[]};
type Geometry={sourceId:string;targetId:string;sourceSlot:number;targetSlot:number;distanceMm:number;maximumDistanceMm:number;observedTick:number;validUntilTick:number;eligible:boolean};
export type LocalReference={template:{version:string;provenance:string;stepTimeSpanTicks:number;entities:Entity[];geometry:Geometry[];initialShields:unknown[];sourceEvidence:{sourceId:string;sha256:string}[]};sha256:string};
/** Explicit trusted LOCAL reference injection; no operating stat/catalog fallback. */
export async function captureLocalFlowPacket(db:D1Database,current:RefitRead,reference:LocalReference){
 if(!current.refit||current.eligibilityError)throw new ActivityError(409,'combat_b_source_refit_ineligible');
 const departure=buffDepartureSchema.parse(current.refit),accountId=departure.accountId;
 const profile=await readOwnedCombatProfile(db,accountId,{expectedFormationRevision:1,expectedLoadoutRevision:1});
 const definition=await publishCombatDefinitionContract();
 const cars=[...departure.cars].sort((a,b)=>a.slot-b.slot);
 if(cars.some((c,i)=>c.slot!==i))throw new ActivityError(409,'combat_b_source_geometry_slots_unsupported');
 const original=reference.template;
 const formation={version:'unity.combat-b.formation-local.v1',trainId:profile.selectedTrain.trainId,mode:'Solo',battlefield:'ExternalVertical',internalNodeId:null,slots:cars.map(c=>({carriageId:c.instanceId,operatorId:c.occupant.kind==='OPERATOR'?c.occupant.operatorId:null}))};
 const entities:Entity[]=[];
 const template=(kind:string)=>{const x=original.entities.find((e)=>e.kind===kind);if(!x)throw new ActivityError(503,'combat_b_source_reference_invalid');return structuredClone(x);};
 const train=template('TRAIN');train.id=formation.trainId;train.definitionId='LOCAL_REFERENCE_TRAIN';entities.push(train);
 for(const car of cars){const body=template('CARRIAGE');body.id=car.instanceId;body.slot=car.slot;body.definitionId='LOCAL_REFERENCE_CARRIAGE';entities.push(body);
  if(car.occupant.kind==='OPERATOR'){
   if(car.occupant.operatorId!=='op_eve')throw new ActivityError(503,'combat_b_source_action_unavailable');
   const operatorId=car.occupant.operatorId;
   const stat=profile.operators.find(o=>o.operatorId===operatorId)?.permanentStats;
   if(!stat)throw new ActivityError(503,'combat_b_source_owned_stats_unavailable');
   const entity=template('OPERATOR');entity.id=car.occupant.operatorId;entity.definitionId=entity.id;entity.slot=car.slot;entity.maxHp=stat.maxHp;entity.attack=stat.attack;entity.defense={numerator:String(stat.defense),denominator:'1'};entity.basicIntervalMicroseconds=stat.basicIntervalMs*1000;
   // Old Mira runtime bonus is never imported. Existing entity arrays are explicit empty reference inputs.
   entities.push(entity);
  }
 }
 const enemy=template('LOCAL_ENEMY');entities.push(enemy);
 const geometry:Geometry[]=[];
 const frame=original.geometry.find((g)=>g.sourceId==='enemy');
 if(!frame)throw new ActivityError(503,'combat_b_source_reference_invalid');
 for(const target of entities.filter(e=>e.kind!=='LOCAL_ENEMY'))geometry.push({...structuredClone(frame),sourceId:enemy.id,targetId:target.id,sourceSlot:enemy.slot,targetSlot:target.slot});
 for(const op of entities.filter(e=>e.kind==='OPERATOR'))geometry.push({...structuredClone(frame),sourceId:op.id,targetId:enemy.id,sourceSlot:op.slot,targetSlot:enemy.slot});
 const ownedProfileHash=await digest(profile),attack={version:'unity.combat-b.direct-local.v2',provenance:original.provenance,formation,ownedProfileHash,definitionHash:definition.contentHash,stepTimeSpanTicks:original.stepTimeSpanTicks,entities,geometry,initialShields:structuredClone(original.initialShields),sourceEvidence:[...structuredClone(original.sourceEvidence),{sourceId:'REFERENCE_ONLY:AttackConsumer.local-original-input',sha256:reference.sha256}]};
 const attackHash=await digest(attack),contextId='LOCAL_FLOW:'+await sha256Hex(json({accountId,refitRevision:current.revision,sourceHash:current.sourceHash,attackHash}));
 const initial={contextId,mode:'EXTERIOR',cars:cars.map(c=>({slot:c.slot,body:'NORMAL',actor:c.occupant.kind==='CARGO'?'ABSENT':'UP'}))};
 const targetCategories=[{targetId:enemy.id,category:'ENEMY'}];
 const buffBinding={version:'unity.combat-b.direct-buff-binding-local.v1',provenance:'BOUNDED_LOCAL_NOT_FULL_RUN',attackInputHash:attackHash,definitionHash:'sha256:'+current.definitionHash,departure,initial,targetCategories};
 const routes=entities.filter(e=>e.kind==='OPERATOR').map(e=>({sourceId:e.id,targetId:enemy.id}));
 const loopBinding={version:'unity.combat-b.basic-loop-binding-local.v1',provenance:'BOUNDED_LOCAL_NOT_FULL_RUN',compositionHash:await digest(buffBinding),targets:routes};
 const payload={version:localSourceVersion,provenance:'ISOLATED_LOCAL_FIXTURE',accountId,selectionRevision:current.selectionRevision,refitRevision:current.revision,rankRevision:current.rankSource.revision,sourceHash:current.sourceHash,definitionHash:current.definitionHash,departure,ownedProfile:profile,ownedProfileHash,definition,attack,attackHash,buffBinding,buffBindingHash:await digest(buffBinding),routes,loopBinding,loopBindingHash:await digest(loopBinding),targetCategories,contextId,startReady:false};
 return {...payload,packetHash:await digest(payload)};
}
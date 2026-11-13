import {z} from 'zod';
import {canonicalJson} from '@lastrail/common';
import {ActivityError} from './activity-policy';

export const overflowPulseBudgetVersion='combat.overflow-pulse-budget.v1.20261020';
const id=z.string().min(1).max(200),nat=z.number().int().nonnegative().safe(),rev=nat.min(1);
const parentSchema=z.strictObject({runId:id,nodeId:id,actorId:id,actorGeneration:rev,parentId:id,
 definitionVersion:z.literal('augment.utility-definitions.v8.20261020'),applicationId:id,
 attackAtCast:z.strictObject({numerator:nat,denominator:rev}),o02ApplicationId:id.nullable()});
const healSchema=z.strictObject({eventId:id,targetId:id,targetGeneration:rev,
 sourceKind:z.enum(['ORIGINAL_ACTIVE_DIRECT_HP_HEAL','ORIGINAL_ULTIMATE_DIRECT_HP_HEAL','ORIGINAL_SKILL_HEAL_OVER_TIME_TICK','FORMALLY_MAPPED_INNATE_PASSIVE_HP_HEAL']),
 // Already resolved by the ORIGINAL ability/HP owner at effect time. No cast-time
 // predicted deficit, generic healing multiplier, or new target selection is used.
 outcome:z.enum(['APPLIED','INVALID_TARGET','CANCELED','HEAL_FORBIDDEN']),effectiveAmount:nat,
 beforeHp:nat,maxHp:rev,afterHp:nat,
 redistribution:z.strictObject({eventId:id,actualHeal:nat}).nullable()
}).refine(h=>h.beforeHp<=h.maxHp&&h.afterHp<=h.maxHp);
const scopeSchema=parentSchema.omit({attackAtCast:true,o02ApplicationId:true});
const pulseSchema=z.strictObject({eventId:id,parentId:id,origin:scopeSchema,tick:nat,sequence:rev,
 // The original owner closes the whole pulse once, after all recipients and O06.
 heals:z.array(healSchema).max(1000),
 launch:z.discriminatedUnion('kind',[
  z.strictObject({kind:z.literal('RESERVED'),projectileId:id,targetId:id,targetGeneration:rev}),
  z.strictObject({kind:z.enum(['NO_TARGET','CANNOT_FIRE'])})
 ])});
type Parent=z.infer<typeof parentSchema>;
type Pulse=z.infer<typeof pulseSchema>;
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const fail=(s:string):never=>{throw new ActivityError(409,'overflow_pulse_'+s);};
const safe=(n:bigint)=>{if(n>BigInt(Number.MAX_SAFE_INTEGER))return fail('amount_range');return Number(n);};
const requireCondition=(v:boolean,s:string)=>{if(!v)fail(s);};

/** INTERNAL original-effect owner component, not a client proof/HP endpoint.
 * Caller supplies verified APPLIED O23 parent and complete original HP/O06 receipts;
 * it commits this checkpoint and projectile reservation with those receipts in its
 * existing authority transaction. No production authority adapter is installed here.
 * The O06 owner must establish Sera's original ultimate, one parent transfer and
 * eligible secondary target/current modifiers; the 50% consistency bound below
 * cannot establish those permissions. sourceKind is validated metadata, not proof.
 * O02 request is not actual shield: its original owner validates recipient/cap,
 * replacement/stacking/expiry. Unaccepted shield and attack shares never compensate
 * one another. O15 later consumption cannot recalculate this immutable allocation.
 * One instance owns one cast/cycle. It neither retargets nor applies healing/shields.
 */
export class OverflowPulseBudget {
 private readonly parent:Parent;
 private readonly cap:number;
 private spent=0;
 private readonly journal:{pulse:Pulse;receipt:OverflowPulseReceipt}[]=[];
 constructor(originalParent:unknown){
  this.parent=parentSchema.parse(originalParent);
  this.cap=safe(BigInt(this.parent.attackAtCast.numerator)*15000n/(BigInt(this.parent.attackAtCast.denominator)*10000n));
 }
 allocateFromOriginalReceipts(input:unknown):OverflowPulseReceipt&{replayed:boolean}{
  const p=pulseSchema.parse(input),old=this.journal.find(e=>e.pulse.eventId===p.eventId);
  if(old){requireCondition(json(old.pulse)===json(p),'event_conflict');return {...structuredClone(old.receipt),replayed:true};}
  const {attackAtCast,o02ApplicationId,...origin}=this.parent;void attackAtCast;void o02ApplicationId;
  requireCondition(p.parentId===this.parent.parentId&&json(p.origin)===json(origin),'parent_conflict');
  const last=this.journal.at(-1)?.pulse;
  requireCondition(!last||(p.sequence>last.sequence&&p.tick>last.tick),'pulse_order_conflict');
  if(this.journal.length>=10000)return fail('journal_capacity');
  const priorHealIds=new Set(this.journal.flatMap(e=>e.pulse.heals.flatMap(h=>[h.eventId,...h.redistribution?[h.redistribution.eventId]:[]])));
  const ids=p.heals.flatMap(h=>[h.eventId,...h.redistribution?[h.redistribution.eventId]:[]]);
  requireCondition(new Set(ids).size===ids.length&&ids.every(e=>!priorHealIds.has(e)),'heal_event_conflict');
  const healing=p.heals.map(h=>{
   if(h.outcome!=='APPLIED'){
    requireCondition(h.afterHp===h.beforeHp&&h.redistribution===null,'invalid_heal_receipt');
    return {eventId:h.eventId,targetId:h.targetId,actualHeal:0,actualRedistribution:0,remainingExcess:0,o02ShieldRequest:0};
   }
   requireCondition(h.beforeHp>0,'dead_target_heal');
   const actual=Math.min(h.effectiveAmount,h.maxHp-h.beforeHp);
   requireCondition(h.afterHp-h.beforeHp===actual,'effect_deficit_conflict');
   const excess=h.effectiveAmount-actual,redistributed=h.redistribution?.actualHeal??0;
   requireCondition(redistributed<=Math.floor(excess/2),'redistribution_conflict');
   const remaining=excess-redistributed;
   // O02 receives half the remaining excess and converts at its original 50%.
   // This is a request for its existing shield owner, never a created shield.
   const shield=this.parent.o02ApplicationId?safe(BigInt(remaining)*2500n/10000n):0;
   return {eventId:h.eventId,targetId:h.targetId,actualHeal:actual,actualRedistribution:redistributed,remainingExcess:remaining,o02ShieldRequest:shield};
  });
  const excess=healing.reduce((sum,h)=>sum+BigInt(h.remainingExcess),0n);
  const requested=safe(excess*(this.parent.o02ApplicationId?3000n:6000n)/10000n);
  const allocated=Math.min(requested,this.cap-this.spent),nextSpent=this.spent+allocated;
  const projectile=allocated>0&&p.launch.kind==='RESERVED'?{...p.launch,damageBeforeDefense:allocated}:null;
  if(projectile)requireCondition(!this.journal.some(e=>e.receipt.projectile?.projectileId===projectile.projectileId),'projectile_conflict');
  const receipt:OverflowPulseReceipt={version:overflowPulseBudgetVersion,parentId:p.parentId,origin:structuredClone(p.origin),eventId:p.eventId,tick:p.tick,
   sequence:p.sequence,healing,requestedDamage:requested,allocatedDamage:allocated,discardedDamage:projectile?0:allocated,
   remainingParentBudget:this.cap-nextSpent,projectile};
  // Charge at allocation, BEFORE target availability/fire/impact; there is no
  // refund API. A later pulse must provide its own new original excess receipts.
  this.spent=nextSpent;this.journal.push({pulse:structuredClone(p),receipt:structuredClone(receipt)});
  return {...structuredClone(receipt),replayed:false};
 }
 snapshot(){return {version:overflowPulseBudgetVersion,parent:structuredClone(this.parent),cap:this.cap,spent:this.spent,journal:structuredClone(this.journal)};}
 /** Stage without changing this owner. The existing authority must persist the
  * candidate checkpoint with its original heal/O06/O02 and projectile transaction,
  * then restore that committed checkpoint. Failure/unknown commit is recovered by
  * the original pulse key, never by applying a second HP heal or refunding budget. */
 prepareFromOriginalReceipts(input:unknown){
  const candidate=OverflowPulseBudget.restore(this.parent,this.snapshot());
  const receipt=candidate.allocateFromOriginalReceipts(input);
  return {receipt,checkpoint:candidate.snapshot()};
 }
 /** expectedParent must be the independently pinned original ability parent. */
 static restore(expectedParent:unknown,input:unknown){
  const s=z.strictObject({version:z.literal(overflowPulseBudgetVersion),parent:parentSchema,cap:nat,spent:nat,
   journal:z.array(z.strictObject({pulse:pulseSchema,receipt:z.json()})).max(10000)}).parse(input);
  requireCondition(json(s.parent)===json(parentSchema.parse(expectedParent)),'restore_parent_conflict');
  const owner=new OverflowPulseBudget(expectedParent);
  for(const e of s.journal){const {replayed,...receipt}=owner.allocateFromOriginalReceipts(e.pulse);requireCondition(!replayed&&json(receipt)===json(e.receipt),'restore_receipt_conflict');}
  requireCondition(json(owner.snapshot())===json(s),'restore_state_conflict');return owner;
 }
}
export type OverflowPulseReceipt={version:typeof overflowPulseBudgetVersion;parentId:string;origin:z.infer<typeof scopeSchema>;eventId:string;tick:number;sequence:number;
 healing:{eventId:string;targetId:string;actualHeal:number;actualRedistribution:number;remainingExcess:number;o02ShieldRequest:number}[];
 requestedDamage:number;allocatedDamage:number;discardedDamage:number;remainingParentBudget:number;
 projectile:{kind:'RESERVED';projectileId:string;targetId:string;targetGeneration:number;damageBeforeDefense:number}|null};

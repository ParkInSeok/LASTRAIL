import { numericDefenseInputSchema, numericDefenseBudgetVersion, resolveNumericDefenseBudget } from "./combat-numeric-defense";
import { damageActorsSchema, initializeDamageActors, creditDamageActor } from "./combat-damage-actors";
import { directDamageInputSchema, directDamageBudgetVersion, resolveDirectDamageBudget } from "./combat-damage-budget";
import { exactCombatValueSchema, ExactCombatValue } from "./combat-exact-value";
import { z } from "zod";
import { canonicalJson, sha256Hex } from "@lastrail/common";
import { ActivityError } from "./activity-policy";
export const combatDamageStateVersion = "combat.damage-state.v1";
const id=z.string().min(1).max(200),nat=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER);
const target=z.strictObject({id,hp:nat,maxHp:nat.min(1)}).refine(t=>t.hp<=t.maxHp);
const targets=z.array(target).min(1).max(1000).refine(t=>new Set(t.map(x=>x.id)).size===t.length);
const entry=z.strictObject({eventId:id,targetId:id,hpBefore:nat,hpLoss:nat,hpAfter:nat}).refine(e=>e.hpAfter===e.hpBefore-e.hpLoss);
const stateSchema=z.strictObject({version:z.literal(combatDamageStateVersion),initialTargets:targets,targets,ledger:z.array(entry).max(100000)}).superRefine((s,ctx)=>{
 const hp=new Map(s.initialTargets.map(t=>[t.id,t.hp]));const seen=new Set<string>();
 for(const e of s.ledger){if(seen.has(e.eventId)||hp.get(e.targetId)!==e.hpBefore)ctx.addIssue({code:"custom",message:"invalid damage ledger"});seen.add(e.eventId);hp.set(e.targetId,e.hpAfter);}
 if(s.targets.length!==s.initialTargets.length||s.targets.some(t=>hp.get(t.id)!==t.hp||s.initialTargets.find(i=>i.id===t.id)?.maxHp!==t.maxHp))ctx.addIssue({code:"custom",message:"damage state mismatch"});
});
const command=z.strictObject({eventId:id,expectedRevision:nat.min(1).max(Number.MAX_SAFE_INTEGER-1),targetId:id,hpLoss:nat});
const calculationSchema=z.strictObject({version:z.literal(directDamageBudgetVersion),exact:exactCombatValueSchema,ordinaryBeforeShield:z.string().regex(/^(0|[1-9][0-9]*)$/).max(256)})
 .refine(c=>ExactCombatValue.from(c.exact).floor()===c.ordinaryBeforeShield);
const numericCalculationSchema=z.strictObject({version:z.literal(numericDefenseBudgetVersion),effectiveDefense:exactCombatValueSchema,exact:exactCombatValueSchema,ordinaryBeforeShield:z.string().regex(/^(0|[1-9][0-9]*)$/).max(256)})
 .refine(c=>ExactCombatValue.from(c.exact).floor()===c.ordinaryBeforeShield);
const numericReceipt=z.strictObject({...entry.shape,version:z.literal("combat.calculated-damage-receipt.v2"),revision:nat.min(2),calculation:numericCalculationSchema});
const numericOperatorReceipt=numericReceipt.extend({version:z.literal("combat.operator-hit-receipt.v2"),actorCharge:z.strictObject({actorId:id,charge:nat.max(200).multipleOf(4),gained:z.union([z.literal(0),z.literal(4)])})});
const validNumericLoss=(e:Pick<z.infer<typeof numericReceipt>,"hpAfter"|"hpBefore"|"hpLoss"|"calculation">)=>e.hpAfter===e.hpBefore-e.hpLoss && BigInt(e.hpLoss)===(BigInt(e.calculation.ordinaryBeforeShield)<BigInt(e.hpBefore)?BigInt(e.calculation.ordinaryBeforeShield):BigInt(e.hpBefore));
const calculatedReceiptVersion="combat.calculated-damage-receipt.v1";
const responseSchema=z.union([
 numericReceipt.refine(validNumericLoss),numericOperatorReceipt.refine(validNumericLoss),
 z.strictObject({...entry.shape,version:z.literal("combat.operator-hit-receipt.v1"),revision:nat.min(2),calculation:calculationSchema,actorCharge:z.strictObject({actorId:id,charge:nat.max(200).multipleOf(4),gained:z.union([z.literal(0),z.literal(4)])})}).refine(e=>e.hpAfter===e.hpBefore-e.hpLoss && BigInt(e.hpLoss)===(BigInt(e.calculation.ordinaryBeforeShield)<BigInt(e.hpBefore)?BigInt(e.calculation.ordinaryBeforeShield):BigInt(e.hpBefore))),
 z.strictObject({...entry.shape,version:z.literal(combatDamageStateVersion),revision:nat.min(2)}).refine(e=>e.hpAfter===e.hpBefore-e.hpLoss),
 z.strictObject({...entry.shape,version:z.literal(calculatedReceiptVersion),revision:nat.min(2),calculation:calculationSchema})
  .refine(e=>e.hpAfter===e.hpBefore-e.hpLoss && BigInt(e.hpLoss)===(BigInt(e.calculation.ordinaryBeforeShield)<BigInt(e.hpBefore)?BigInt(e.calculation.ordinaryBeforeShield):BigInt(e.hpBefore)))
]);
// Reuse the receipt contract at internal evidence boundaries; this is not an authority proof.
export {responseSchema as combatDamageReceiptSchema};
const directCommand=z.strictObject({eventId:id,expectedRevision:nat.min(1).max(Number.MAX_SAFE_INTEGER-1),targetId:id,damage:directDamageInputSchema});
const operatorCommand=directCommand.extend({sourceActorId:id,actionId:id,hitOrdinal:nat,sourceKind:z.enum(["BASIC","SKILL","SUMMON"])});
const numericDirectCommand=directCommand.extend({damage:numericDefenseInputSchema});
const numericOperatorCommand=operatorCommand.extend({damage:numericDefenseInputSchema});
type NumericCommand=(z.infer<typeof numericDirectCommand>&{kind:"ORDINARY_NUMERIC_DIRECT_UNSHIELDED"})|(z.infer<typeof numericOperatorCommand>&{kind:"ORDINARY_NUMERIC_OPERATOR_HIT"});
type DamageCommand=z.infer<typeof command>|(z.infer<typeof directCommand>&{kind:"ORDINARY_DIRECT_UNSHIELDED"});
type OperatorCommand=z.infer<typeof operatorCommand>&{kind:"ORDINARY_OPERATOR_HIT"};
type Row={revision:number;initial_hash:string;start_snapshot_json:string;state_json:string;actor_state_json:string|null};
const fail=(code:string):never=>{throw new ActivityError(409,code);};
/** Internal persistence boundary only. Caller must authorize resolved HP loss; never mount as a raw client damage API. */
export class CombatDamageStore {
 constructor(private readonly db:D1Database){}
 private async owner(accountId:string,runId:string){
  id.parse(accountId);id.parse(runId);
  const run=await this.db.prepare("SELECT r.status,s.snapshot_json FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=?").bind(runId,accountId).first<{status:string;snapshot_json:string}>();
  if(!run)return fail("combat_run_not_found");return run;
 }
 private row(runId:string){return this.db.prepare("SELECT revision,initial_hash,start_snapshot_json,state_json,actor_state_json FROM combat_damage_states WHERE run_id=?").bind(runId).first<Row>();}
 async read(accountId:string,runId:string){await this.owner(accountId,runId);const row=await this.row(runId);if(!row)return fail("combat_damage_state_missing");return {revision:row.revision,state:stateSchema.parse(JSON.parse(row.state_json)),...(row.actor_state_json?{actorState:damageActorsSchema.parse(JSON.parse(row.actor_state_json))}:{})};}
 async lookup(accountId:string,runId:string,eventId:string){
  id.parse(eventId);await this.owner(accountId,runId);
  const receipt=await this.db.prepare("SELECT response_json FROM combat_damage_receipts WHERE run_id=? AND event_id=?").bind(runId,eventId).first<{response_json:string}>();
  return receipt?responseSchema.parse(JSON.parse(receipt.response_json)):null;
 }
 async initialize(accountId:string,runId:string,input:unknown,actorDefinition?:unknown){
  const initialTargets=targets.parse(input).sort((a,b)=>a.id<b.id?-1:a.id>b.id?1:0),run=await this.owner(accountId,runId);
  const actorState=actorDefinition===undefined?undefined:initializeDamageActors(actorDefinition,initialTargets);
  const initialHash=await sha256Hex(canonicalJson({initialTargets,startSnapshot:run.snapshot_json,...(actorState?{actorState}:{})}));
  const existing=await this.row(runId);
  if(existing){if(existing.initial_hash!==initialHash)return fail("combat_damage_initialization_conflict");return this.read(accountId,runId);}
  if(run.status!=="ACTIVE")return fail("combat_run_not_active");
  const state=stateSchema.parse({version:combatDamageStateVersion,initialTargets,targets:initialTargets,ledger:[]});
  try{await this.db.batch([
   this.db.prepare("INSERT INTO combat_damage_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND s.snapshot_json=?) AND NOT EXISTS(SELECT 1 FROM combat_damage_states WHERE run_id=?) THEN 1 ELSE 0 END").bind(runId,accountId,run.snapshot_json,runId),
   this.db.prepare("INSERT INTO combat_damage_states(run_id,revision,initial_hash,start_snapshot_json,state_json,actor_state_json) VALUES(?,1,?,?,?,?)").bind(runId,initialHash,run.snapshot_json,canonicalJson(state),actorState?canonicalJson(actorState):null),
   this.db.prepare("DELETE FROM combat_damage_write_guard")]);
  }catch(error){const raced=await this.row(runId);if(raced?.initial_hash!==initialHash)throw error;}
  return this.read(accountId,runId);
 }
 async commit(accountId:string,runId:string,input:unknown){return this.persist(accountId,runId,command.parse(input));}
 /** Authoritative host must confirm that this target has no active shield and resolve source eligibility first. */
 async commitUnshieldedDirectDamage(accountId:string,runId:string,input:unknown){
  return this.persist(accountId,runId,{...directCommand.parse(input),kind:"ORDINARY_DIRECT_UNSHIELDED"});
 }
 /** Internal ordinary, unshielded hit only. Host owns provenance, shield eligibility and stable event IDs.
  * Definitions are pinned at explicit initialization; never inferred for historical checkpoints. */
 async commitOperatorHit(accountId:string,runId:string,input:unknown){
  return this.persist(accountId,runId,{...operatorCommand.parse(input),kind:"ORDINARY_OPERATOR_HIT"});
 }
 /** Explicit new calculation contract; caller resolves approved DEF/effects from original
  * definitions and proves source/target eligibility and absence of shields. Never a raw client API.
  * This does not migrate historical state or switch the default legacy producer. */
 async commitNumericUnshieldedDirectDamage(accountId:string,runId:string,input:unknown){
  return this.persist(accountId,runId,{...numericDirectCommand.parse(input),kind:"ORDINARY_NUMERIC_DIRECT_UNSHIELDED"});
 }
 async commitNumericOperatorHit(accountId:string,runId:string,input:unknown){
  return this.persist(accountId,runId,{...numericOperatorCommand.parse(input),kind:"ORDINARY_NUMERIC_OPERATOR_HIT"});
 }
 private async persist(accountId:string,runId:string,c:DamageCommand|OperatorCommand|NumericCommand){
  const run=await this.owner(accountId,runId),hash=await sha256Hex(canonicalJson(c));
  const replay=async()=>{const r=await this.db.prepare("SELECT request_hash,response_json FROM combat_damage_receipts WHERE run_id=? AND event_id=?").bind(runId,c.eventId).first<{request_hash:string;response_json:string}>();
   if(!r)return null;if(r.request_hash!==hash)return fail("combat_damage_event_conflict");return responseSchema.parse(JSON.parse(r.response_json));};
  const original=await replay();if(original)return original;
  if(run.status!=="ACTIVE")return fail("combat_run_not_active");
  const row=await this.row(runId);if(!row)return fail("combat_damage_state_missing");
  if(row.revision!==c.expectedRevision)return fail("combat_damage_revision_conflict");
  if(row.start_snapshot_json!==run.snapshot_json)return fail("combat_damage_start_snapshot_changed");
  const state=stateSchema.parse(JSON.parse(row.state_json)),t=state.targets.find(t=>t.id===c.targetId);
  if(!t)return fail("combat_damage_target_unknown");
  const calculation="kind" in c?(c.kind==="ORDINARY_NUMERIC_DIRECT_UNSHIELDED"||c.kind==="ORDINARY_NUMERIC_OPERATOR_HIT"?resolveNumericDefenseBudget(c.damage):resolveDirectDamageBudget(c.damage)):undefined;
  // Bound in bigint before converting: the final loss cannot exceed safe-integer HP.
  const calculated=calculation?BigInt(calculation.ordinaryBeforeShield):0n;
  const hpLoss="hpLoss" in c?c.hpLoss:Number(calculated<BigInt(t.hp)?calculated:BigInt(t.hp));
  if(hpLoss>t.hp)return fail("combat_damage_loss_invalid");
  const e={eventId:c.eventId,targetId:c.targetId,hpBefore:t.hp,hpLoss,hpAfter:t.hp-hpLoss};
  const actorState=row.actor_state_json?damageActorsSchema.parse(JSON.parse(row.actor_state_json)):undefined;
  const actorCharge="kind" in c && (c.kind==="ORDINARY_OPERATOR_HIT"||c.kind==="ORDINARY_NUMERIC_OPERATOR_HIT") ? (actorState?creditDamageActor(actorState,state.targets,c,hpLoss):fail("combat_damage_actor_state_missing")):undefined;
  t.hp=e.hpAfter;state.ledger.push(e);stateSchema.parse(state);
  const result=responseSchema.parse({...e,version:actorCharge?(calculation?.version===numericDefenseBudgetVersion?"combat.operator-hit-receipt.v2":"combat.operator-hit-receipt.v1"):calculation?(calculation.version===numericDefenseBudgetVersion?"combat.calculated-damage-receipt.v2":calculatedReceiptVersion):combatDamageStateVersion,revision:row.revision+1,...(calculation?{calculation}:{}),...(actorCharge?{actorCharge}:{})});
  try{await this.db.batch([
   this.db.prepare("INSERT INTO combat_damage_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_snapshots s ON s.run_id=r.id JOIN combat_damage_states d ON d.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND s.snapshot_json=? AND d.revision=? AND d.state_json=? AND d.actor_state_json IS ?) THEN 1 ELSE 0 END").bind(runId,accountId,run.snapshot_json,row.revision,row.state_json,row.actor_state_json),
   this.db.prepare("UPDATE combat_damage_states SET revision=?,state_json=?,actor_state_json=? WHERE run_id=? AND revision=?").bind(result.revision,canonicalJson(state),actorState?canonicalJson(actorState):null,runId,row.revision),
   this.db.prepare("INSERT INTO combat_damage_receipts(run_id,event_id,request_hash,response_json) VALUES(?,?,?,?)").bind(runId,c.eventId,hash,canonicalJson(result)),
   this.db.prepare("DELETE FROM combat_damage_write_guard")]);
  }catch(error){const completed=await replay();if(completed)return completed;if(String(error).includes("valid=1"))return fail("combat_damage_state_conflict");throw error;}
  return result;
 }
}

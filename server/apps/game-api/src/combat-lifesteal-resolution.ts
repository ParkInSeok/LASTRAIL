import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {CombatHealthStore,combatHealthReceiptSchema,readCombatHealthReceipt} from './combat-health-store';
const id=z.string().regex(/^[\x21-\x7e]{1,128}$/u),nat=z.number().int().nonnegative().safe();
const contextSchema=z.strictObject({accountId:id,runId:id,eventId:id});
const sourceSchema=z.strictObject({nodeId:id,actorGeneration:nat,targetGeneration:nat,parentId:id,sequence:nat.min(1),
 actorId:id,targetId:id,hpLoss:nat,
 appliedEffect:z.strictObject({applicationId:id,definitionVersion:z.enum(['augment.utility-definitions.v6.20261019','augment.utility-definitions.v7.20261019','augment.utility-definitions.v8.20261020']),
  definitionHash:z.string().regex(/^[a-f0-9]{64}$/u)}).nullable(),
 healingForbidden:z.boolean(),healingMultiplier:z.strictObject({numerator:z.string().regex(/^(0|[1-9][0-9]{0,127})$/u),denominator:z.string().regex(/^[1-9][0-9]{0,127}$/u)})
}).refine(s=>s.actorId!==s.targetId);
type Source=z.infer<typeof sourceSchema>;
type Health=Awaited<ReturnType<CombatHealthStore['read']>>;
/** An authenticated original combat producer, never a client damage/hasO22 port.
 * Must verify applied ownership (or authoritative absence), original action/parent
 * definition and actor attribution/generation, ENEMY target, confirmed loss, current
 * healing conditions, and sequence against the supplied HP checkpoint. Assertions
 * healingMultiplier is the original owner's exact, already filtered LIFESTEAL
 * result, not a product of all buffs. Skill-only and prior damage multipliers
 * must not be inherited. No rounding before final HP; missing filter is an error.
 * must revalidate ALL mutable inputs and reserve this original event in the same
 * transaction. Expired/exited generations must be resolved by the original owner;
 * they must never be redirected to a replacement actor. Missing proof is an error.
 * The original authority must fence/reserve its next committed sequence (including
 * its existing tie rule) in assertions, and expose the next event only after this
 * complete damage/heal receipt commits. HP revision alone is not sequence proof.
 * Each child/target/tick is one original actual-loss event; a parent summary or
 * display aggregate must never be admitted as another damage event. Projectile
 * survival after down is decided by that original source, not by this resolver.
 * Original termination must fence its cutoff with the same transaction: include
 * an already accepted final hit's recovery before fixing the final snapshot, but
 * never admit cancelled projectiles/future ticks. A result projection consumes
 * the committed HP/receipt, not a recomputed heal or unverified client summary.
 * This port does not itself install a production mode/source adapter.
 */
export interface LifestealResolutionSource {
 prepare(context:z.infer<typeof contextSchema>&{health:Health}):Promise<{source:Source;assertions:D1PreparedStatement[]}>;
}
const outcomeSchema=z.enum(['HEALED','UNOWNED','DAMAGE_ZERO','DOWN','HEAL_FORBIDDEN','FULL_HP','MODIFIED_ZERO']);
function recovery(source:Source,hp:number,maxHp:number){
 const outcome=!source.appliedEffect?'UNOWNED':source.hpLoss===0?'DAMAGE_ZERO':hp===0?'DOWN':source.healingForbidden?'HEAL_FORBIDDEN':hp===maxHp?'FULL_HP':null;
 if(outcome)return {actual:0,outcome};
 const amount=BigInt(source.hpLoss)*1200n*BigInt(source.healingMultiplier.numerator)/(10000n*BigInt(source.healingMultiplier.denominator));
 const actual=Number(amount<BigInt(maxHp-hp)?amount:BigInt(maxHp-hp));
 return {actual,outcome:actual===0?'MODIFIED_ZERO':'HEALED'};
}
const resultSchema=z.strictObject({version:z.literal('combat.lifesteal-resolution.v1.20261019'),...contextSchema.shape,
 source:sourceSchema,hpLoss:nat,actualRecovery:nat,outcome:outcomeSchema,healthRevision:nat.min(3),
 healthReceipts:z.tuple([combatHealthReceiptSchema,combatHealthReceiptSchema])
}).superRefine((r,c)=>{
 for(const receipt of r.healthReceipts)readCombatHealthReceipt(receipt);
 const [damage,heal]=r.healthReceipts,expected=recovery(r.source,heal.before.hp,heal.before.maxHp);
 if(damage.targetId!==r.source.targetId||heal.targetId!==r.source.actorId||damage.before.hp-damage.after.hp!==r.hpLoss||r.hpLoss!==r.source.hpLoss
  ||damage.change.kind!=='ACTUAL_HP'||damage.change.reason!=='DAMAGE'||heal.change.kind!=='ACTUAL_HP'||heal.change.reason!=='HEAL'
  ||heal.after.hp-heal.before.hp!==r.actualRecovery||r.actualRecovery!==expected.actual||r.outcome!==expected.outcome
  ||heal.revision!==damage.revision+1||r.healthRevision!==heal.revision)c.addIssue({code:'custom',message:'invalid_resolution_receipt'});
});
const fail=(code:string):never=>{throw new ActivityError(409,'lifesteal_resolution_'+code);};
/** Single atomic damage + recovery over CombatHealthStore's existing V2 HP row.
 * No extra HP store or public endpoint. Precommitted legacy V1 damage is NOT
 * backfilled: that path needs a durable original journal obligation and barrier.
 * Returns both final HP receipts; normal success requires no follow-up GET.
 */
export class CombatLifestealResolution {
 constructor(private readonly db:D1Database,private readonly source?:LifestealResolutionSource){}
 async resolve(accountId:string,runId:string,eventId:string){
  const context=contextSchema.parse({accountId,runId,eventId}),requestHash=await sha256Hex(canonicalJson(context));
  const key='lifesteal:'+requestHash,damageKey='ls-damage:'+requestHash,healKey='ls-heal:'+requestHash;
  if(!await this.db.prepare('SELECT 1 FROM runs WHERE id=? AND account_id=?').bind(runId,accountId).first())return fail('owner');
  const replay=async()=>{
   const row=await this.db.prepare('SELECT request_hash,response_json FROM combat_damage_receipts WHERE run_id=? AND event_id=?').bind(runId,key).first<{request_hash:string;response_json:string}>();
   if(!row)return null;
   if(row.request_hash!==requestHash)return fail('event_conflict');
   const result=resultSchema.parse(JSON.parse(row.response_json));
   if(result.accountId!==accountId||result.runId!==runId||result.eventId!==eventId||result.healthReceipts[0].eventId!==damageKey||result.healthReceipts[1].eventId!==healKey)return fail('receipt_conflict');
   return result;
  };
  const old=await replay();if(old)return old;
  try {
  if(!this.source)return fail('source_required');
  const current=await new CombatHealthStore(this.db).read(accountId,runId);
  const prepared=await this.source.prepare({...context,health:structuredClone(current)});
  if(!prepared||!Array.isArray(prepared.assertions)||prepared.assertions.length===0)return fail('proof_required');
  const source=sourceSchema.parse(prepared.source),target=current.state.targets.find(t=>t.id===source.targetId),actor=current.state.targets.find(t=>t.id===source.actorId);
  if(!target||!actor||source.hpLoss>target.health.hp)return fail('source_conflict');
  const healing=recovery(source,actor.health.hp,actor.health.maxHp);
  const commands=[{eventId:damageKey,expectedRevision:current.revision,targetId:target.id,change:{kind:'ACTUAL_HP' as const,reason:'DAMAGE' as const,hp:target.health.hp-source.hpLoss}},
   {eventId:healKey,expectedRevision:current.revision+1,targetId:actor.id,change:{kind:'ACTUAL_HP' as const,reason:'HEAL' as const,hp:actor.health.hp+healing.actual}}];
  const owner=new CombatHealthStore(this.db,{prepare:async c=>{
   const actualCommand=c.command;
   if(c.kind!=='CHANGE'||!actualCommand||c.accountId!==accountId||c.runId!==runId||c.batchBaseRevision!==current.revision||!commands.some(command=>canonicalJson(command)===canonicalJson(z.json().parse(actualCommand))))return fail('proof_context');
   // Source assertions can reserve an event: execute them once for the group.
   // Each command is bound above; this fence retains its original HP checkpoint.
   return {assertions:[this.db.prepare('INSERT INTO combat_damage_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_damage_states WHERE run_id=? AND revision=?) THEN 1 ELSE 0 END').bind(runId,current.revision)]};
  }});
  const group=await owner.prepareChanges(accountId,runId,commands);
  // A missing outer receipt with existing HP commands is not a new healing grant.
  if(group.statements.length===0)return fail('partial_recovery');
  const result=resultSchema.parse({version:'combat.lifesteal-resolution.v1.20261019',...context,source,hpLoss:source.hpLoss,
   actualRecovery:healing.actual,outcome:healing.outcome,healthRevision:current.revision+2,healthReceipts:group.receipts});
  await this.db.batch([...prepared.assertions,...group.statements,this.db.prepare('INSERT INTO combat_damage_receipts(run_id,event_id,request_hash,response_json) VALUES(?,?,?,?)').bind(runId,key,requestHash,canonicalJson(result))]);
  return result;
  } catch(error){const committed=await replay();if(committed)return committed;throw error;}
 }
}

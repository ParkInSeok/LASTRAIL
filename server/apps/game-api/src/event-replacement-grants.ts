import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {planActivityGrant,type ActivityGrantReward} from './activity-grant-plan';
import {walletId} from './crystal-wallet';
import {recurringSequence,recurringRetryable} from './recurring-progress';
import {eventCosmeticKind,eventCosmeticReplacementBundle} from './event-cosmetic-replacement';
const id=z.string().trim().min(1).max(128),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const scope=z.strictObject({environment:z.enum(['DEV','PRD']),accountId:id,roundId:id,rewardRowId:id});
const decisionSchema=scope.extend({branch:z.literal('REPLACEMENT'),cosmeticId:id,cosmeticKind:eventCosmeticKind,
 definitionHash:hash,originalDecisionId:id,tableVersion:id,tableHash:hash});
type Scope=z.infer<typeof scope>;
export interface EventReplacementRightSource{
 /** Internal original entitlement/first-branch owner, never request projections.
  * Freeze the branch/table in commitStatements and fence achievement, ownership,
  * version and concurrent acquisition in assertions in THIS same batch. */
 prepare(context:Readonly<Scope>):Promise<{decision:unknown;assertions:readonly D1PreparedStatement[];commitStatements:readonly D1PreparedStatement[]}>;
}
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const identity=(v:Scope)=>({environment:v.environment,accountId:v.accountId,roundId:v.roundId,rewardRowId:v.rewardRowId});
const fail=(status:403|409|503,code:string):never=>{throw new ActivityError(status,'event_replacement_'+code);};
export class EventReplacementGrantStore{
 constructor(private readonly db:D1Database,private readonly environment:'DEV'|'PRD',private readonly source?:EventReplacementRightSource){}
 private async authorize(c:Scope){
  if(c.environment!==this.environment)return fail(409,'environment_conflict');
  if(!await this.db.prepare("SELECT 1 FROM accounts WHERE id=? AND status='ACTIVE'").bind(c.accountId).first())return fail(403,'account_forbidden');
 }
 private async replay(c:Scope){
  const row=await this.db.prepare('SELECT * FROM event_reward_grants WHERE environment=? AND account_id=? AND round_id=? AND reward_row_id=?')
   .bind(c.environment,c.accountId,c.roundId,c.rewardRowId).first<{decision_json:string;decision_hash:string;response_json:string;response_hash:string}>();
  if(!row)return null;
  if(await sha256Hex(row.decision_json)!==row.decision_hash||await sha256Hex(row.response_json)!==row.response_hash)return fail(503,'receipt_corrupt');
  const decision=decisionSchema.parse(JSON.parse(row.decision_json));
  if(json(identity(decision))!==json(c))return fail(503,'receipt_corrupt');
  return JSON.parse(row.response_json) as Record<string,unknown>;
 }
 async grant(input:Scope):Promise<Record<string,unknown>>{
  const c=scope.parse(input);
  for(let attempt=0;attempt<4;attempt++){
   await this.authorize(c);
   const old=await this.replay(c);if(old)return old;
   if(!this.source)return fail(503,'original_owner_required');
   try{
    const sequence=await recurringSequence(this.db,c.accountId),original=await this.source.prepare(structuredClone(c));
    const decision=decisionSchema.parse(original.decision);
    if(json(identity(decision))!==json(c))return fail(409,'original_scope_conflict');
    const statements=[...original.assertions,...original.commitStatements];
    if(!original.assertions.length||statements.length>48||statements.some(s=>typeof s?.bind!=='function'))return fail(503,'original_fence_required');
    const bundle=await eventCosmeticReplacementBundle(decision.tableVersion,decision.cosmeticKind);
    if(bundle.tableHash!==decision.tableHash)return fail(409,'table_conflict');
    const rewards:ActivityGrantReward[]=bundle.lines.map(line=>line.resource==='FREE_CRYSTAL'
     ?{kind:'CRYSTAL',id:'FREE_CRYSTAL',quantity:line.quantity}
     :{kind:'CURRENCY',id:line.resource,quantity:line.quantity});
    const now=Date.now(),claimId=walletId('event_claim'),mutationId=walletId('event_mutation'),economyId=walletId('event_economy');
    const plan=await planActivityGrant({db:this.db,accountId:c.accountId,mutationId,claimId,economyId,rewards,operation:'EVENT_REPLACEMENT',now,sourceType:'EVENT'});
    const response={accepted:true,contractVersion:'event.replacement-grant.v1.20261020',...c,branch:decision.branch,
     cosmeticId:decision.cosmeticId,originalDecisionId:decision.originalDecisionId,tableVersion:bundle.tableVersion,tableHash:bundle.tableHash,
     claimId,rewardTransactionId:claimId,accountMutationId:mutationId,serverClaimedAt:now,rewardLines:plan.lines,wallet:plan.wallet,refreshRequired:false};
    const decisionJson=json(decision),responseJson=json(response);
    await this.db.batch([
     this.db.prepare("INSERT INTO event_reward_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM accounts WHERE id=? AND status='ACTIVE') THEN 1 ELSE 0 END").bind(c.accountId),
     this.db.prepare("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES(?,?,?,'EVENT_REPLACEMENT',?)").bind(mutationId,c.accountId,sequence,now),
     ...statements,
     this.db.prepare("INSERT INTO economy_transactions VALUES(?,?,?,'EVENT_REPLACEMENT','EVENT',?,?)").bind(economyId,c.accountId,mutationId,claimId,now),
     ...plan.statements,
     this.db.prepare('INSERT INTO reward_transactions(id,account_id,run_id,mutation_id,balance_config_version,reward_json,created_at) VALUES(?,?,NULL,?,NULL,?,?)').bind(claimId,c.accountId,mutationId,responseJson,now),
     this.db.prepare('INSERT INTO event_reward_grants VALUES(?,?,?,?,?,?,?,?,?)').bind(c.environment,c.accountId,c.roundId,c.rewardRowId,decisionJson,await sha256Hex(decisionJson),claimId,responseJson,await sha256Hex(responseJson)),
     this.db.prepare('DELETE FROM event_reward_write_guard')
    ]);
    return response;
   }catch(error){
    await this.authorize(c);
    const winner=await this.replay(c);if(winner)return winner;
    if(recurringRetryable(error)&&attempt<3)continue;
    throw error;
   }
  }
  return fail(409,'state_changed');
 }
}

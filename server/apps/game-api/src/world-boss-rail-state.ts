import { z } from 'zod';
import { canonicalJson } from '@lastrail/common';
import { worldBossPatterns, parseRailDefinition, railAvailabilitySchema, railCommandSchema, railDefinitionSchema, railVersion, type RailDefinition } from './world-boss-rail-contract';
const id=z.string().min(1).max(200), integer=z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER);
const route=z.enum(['ENTRY','LEFT','RIGHT']);
const attackSchema=z.strictObject({eventId:z.string().min(1).max(3000),reason:z.enum(['RAIL_FAILURE','INTERNAL_FAILURE']),attackRef:id,targetRef:id,
  defensePipeline:z.literal('SUPPORTED_DEF_REDUCTION_SHIELD'),carriageTransfer:z.literal(false),penalty:z.literal(false)});
const resultSchema=z.strictObject({roundId:id,patternId:id,outcome:z.enum(['FULL_SUCCESS','PARTIAL_SUCCESS','FAILURE','CANCELLED','SKIPPED']),
  route,railSuccess:z.boolean(),internalSuccess:z.boolean(),resolvedAt:integer,failureAttacks:z.array(attackSchema).max(1),directScore:z.literal(0)});
const buffSchema=z.strictObject({roundId:id,multiplierBps:integer,startsAt:integer,endsAt:integer});
const receiptSchema=z.strictObject({commandId:id,roundId:id,acceptedRevision:integer,acceptedAt:integer,route,internalAction:id.nullable()});
const snapshotSchema=z.strictObject({version:z.literal(railVersion),attemptId:id,definition:railDefinitionSchema,revision:integer,now:integer,
  rounds:z.array(z.strictObject({roundId:id,status:z.enum(['PENDING','OPEN','FINISHED']),route,confirmed:z.boolean(),internalAction:id.nullable()})).length(6),
  results:z.array(resultSchema).max(6),buff:buffSchema.nullable(),receipts:z.array(z.strictObject({accountId:id,commandId:id,digest:z.string(),receipt:receiptSchema})).max(12)});
type Snapshot=z.infer<typeof snapshotSchema>;
type Availability=z.infer<typeof railAvailabilitySchema>;
/** Local domain only. Caller owns authenticated role resolution, authoritative clock/event order,
 * durable atomic receipt+state+health intent commit, and verified movement/health adapters.
 * Do not expose this class directly to client-supplied availability, definitions or snapshots.
 */
export class WorldBossRailAttempt {
  private state:Snapshot;
  constructor(attemptId:string,input:unknown) {
    const definition=parseRailDefinition(input);
    this.state={version:railVersion,attemptId:id.parse(attemptId),definition,revision:0,now:definition.startedAt,
      rounds:definition.rounds.map(r=>({roundId:r.roundId,status:'PENDING',route:'ENTRY',confirmed:false,internalAction:null})),results:[],buff:null,receipts:[]};
  }
  static restore(input:unknown) {
    const s=snapshotSchema.parse(input);
    if(s.now<s.definition.startedAt || s.rounds.some((r,i)=>r.roundId!==s.definition.rounds[i]!.roundId))throw new Error('rail_snapshot_invalid');
    if(new Set(s.results.map(r=>r.roundId)).size!==s.results.length || new Set(s.receipts.map(r=>JSON.stringify([r.accountId,r.commandId]))).size!==s.receipts.length)throw new Error('rail_snapshot_invalid');
    const invalid=()=>{throw new Error('rail_snapshot_invalid');};
    for(const result of s.results)if(!s.rounds.some(r=>r.roundId===result.roundId&&r.status==='FINISHED'))invalid();
    for(const [i,current] of s.rounds.entries()){
      const result=s.results.find(r=>r.roundId===current.roundId),config=s.definition.rounds[i]!;
      if((current.status==='FINISHED')!==Boolean(result))invalid();
      if(!current.confirmed&&current.route!=='ENTRY')invalid();
      if(result){
        if(result.route!==current.route||result.patternId!==worldPattern(s.definition,config.phase)||result.resolvedAt>s.now)invalid();
        const normal=!['CANCELLED','SKIPPED'].includes(result.outcome);
        const rail=normal&&(current.route==='ENTRY'?config.entrySafe:config.safeSides.includes(current.route));
        const internal=normal&&(config.phase===1||current.internalAction===config.internalActions[current.route]);
        if(result.railSuccess!==rail||result.internalSuccess!==internal)invalid();
        if(normal){const full=rail&&internal,expected=full?'FULL_SUCCESS':rail||(config.phase>1&&internal)?'PARTIAL_SUCCESS':'FAILURE';
          if(result.outcome!==expected||result.failureAttacks.length!==(full?0:1))invalid();
          const attack=result.failureAttacks[0];if(attack&&(attack.eventId!==JSON.stringify([s.attemptId,config.roundId,'failure'])||attack.reason!==(!rail?'RAIL_FAILURE':'INTERNAL_FAILURE')||attack.attackRef!==(!rail?config.railAttackRef:config.internalAttackRef)||attack.targetRef!==config.targetRef))invalid();
        }else if(result.failureAttacks.length!==0)invalid();
      }
    }
    for(const record of s.receipts){
      let request:z.infer<typeof railCommandSchema>;
      try{request=railCommandSchema.parse(JSON.parse(record.digest));}catch{invalid();}
      const c=request!,receipt=record.receipt,config=s.definition.rounds.find(r=>r.roundId===c.roundId);
      if(!config||canonicalJson(c)!==record.digest||c.attemptId!==s.attemptId||c.contentVersion!==s.definition.contentVersion||c.commandId!==record.commandId||receipt.commandId!==record.commandId||receipt.roundId!==c.roundId||receipt.acceptedRevision!==c.expectedRevision+1||receipt.acceptedRevision>s.revision||receipt.acceptedAt>s.now)invalid();
      if(c.action.kind==='CONFIRM_ROUTE'){
        if(record.accountId!==s.definition.trainAccountId||receipt.route!==c.action.side||receipt.internalAction!==null||receipt.acceptedAt<s.definition.startedAt+config!.previewAt||receipt.acceptedAt>=s.definition.startedAt+config!.lockAt)invalid();
      }else if(record.accountId!==s.definition.operatorAccountId||receipt.internalAction!==c.action.actionId||receipt.route!==s.rounds.find(r=>r.roundId===c.roundId)!.route||receipt.acceptedAt<s.definition.startedAt+config!.internalOpenAt||receipt.acceptedAt>=s.definition.startedAt+config!.internalCloseAt)invalid();
    }
    if(s.buff){const result=s.results.find(r=>r.roundId===s.buff!.roundId),config=s.definition.rounds.find(r=>r.roundId===s.buff!.roundId);
      if(!result||!config||result.outcome!=='FULL_SUCCESS'||s.buff.startsAt!==result.resolvedAt||s.buff.multiplierBps!==config.buffBps||s.buff.endsAt!==Math.min(s.buff.startsAt+config.buffDurationMs,s.definition.startedAt+180000))invalid();}
    const attempt=new WorldBossRailAttempt(s.attemptId,s.definition);attempt.state=structuredClone(s);return attempt;
  }
  snapshot():Snapshot {return structuredClone(this.state);}
  private checkTime(now:number) {integer.parse(now);if(now<this.state.now)throw new Error('rail_clock_regression');}
  private config(roundId:string) {const r=this.state.definition.rounds.find(r=>r.roundId===roundId);if(!r)throw new Error('rail_round_not_found');return r;}
  private internalNeeded(r:{phase:number}) {return r.phase>1;}
  private finish(index:number,outcome:'CANCELLED'|'SKIPPED'|null,at:number) {
    const s=this.state,r=s.definition.rounds[index]!,current=s.rounds[index]!;
    if(current.status==='FINISHED')return;
    const railSuccess=outcome===null&&(current.route==='ENTRY'?r.entrySafe:r.safeSides.includes(current.route));
    const internalSuccess=outcome===null&&(!this.internalNeeded(r)||current.internalAction===r.internalActions[current.route]);
    const full=railSuccess&&internalSuccess;
    const failureAttacks:z.infer<typeof attackSchema>[]=[];
    // A failed route already accounts for the same forecast attack; never add transfer/penalty.
    // Internal failure remains independently visible in the result, not a second damage budget.
    if(!outcome&&!full){const reason=!railSuccess?'RAIL_FAILURE':'INTERNAL_FAILURE';failureAttacks.push({eventId:JSON.stringify([s.attemptId,r.roundId,'failure']),reason,attackRef:!railSuccess?r.railAttackRef:r.internalAttackRef,targetRef:r.targetRef,defensePipeline:'SUPPORTED_DEF_REDUCTION_SHIELD',carriageTransfer:false,penalty:false});}
    const patternId=worldPattern(s.definition,r.phase);
    s.results.push({roundId:r.roundId,patternId,outcome:outcome??(full?'FULL_SUCCESS':railSuccess||(this.internalNeeded(r)&&internalSuccess)?'PARTIAL_SUCCESS':'FAILURE'),route:current.route,railSuccess,internalSuccess,resolvedAt:at,failureAttacks,directScore:0});
    current.status='FINISHED';
    if(full&&(!s.buff||s.buff.endsAt<=at||r.buffBps>=s.buff.multiplierBps))s.buff={roundId:r.roundId,multiplierBps:r.buffBps,startsAt:at,endsAt:Math.min(at+r.buffDurationMs,s.definition.startedAt+180000)};
    s.revision++;
  }
  advance(now:number,availability:Availability) {
    this.checkTime(now);const a=railAvailabilitySchema.parse(availability),s=this.state,elapsed=now-s.definition.startedAt;
    s.now=now;
    if(elapsed>=180000){s.rounds.forEach((r,i)=>{if(r.status!=='FINISHED')this.finish(i,r.status==='PENDING'?'SKIPPED':'CANCELLED',s.definition.startedAt+180000)});return this.snapshot();}
    s.rounds.forEach((current,i)=>{
      const r=s.definition.rounds[i]!;
      if(current.status==='PENDING'&&elapsed>=r.previewAt){
        // Scheduler lateness is observable, never a client-triggered cancellation or catch-up bonus.
        if(elapsed>=r.lockAt||!a.trainAvailable||!a.trainConnected||(this.internalNeeded(r)&&(!a.operatorAvailable||!a.operatorConnected))){this.finish(i,'SKIPPED',now);return;}
        current.status='OPEN';s.revision++;
      }
      if(current.status!=='OPEN')return;
      if(elapsed>=r.resolveAt){this.finish(i,null,s.definition.startedAt+r.resolveAt);return;}
      if(!a.trainAvailable||(this.internalNeeded(r)&&!a.operatorAvailable)){this.finish(i,'CANCELLED',now);return;}
      // Disconnection alone does not cancel an issued round. No automated input is generated.

    });
    return this.snapshot();
  }
  command(input:unknown,authenticatedAccountId:string,now:number,availability:Availability) {
    const c=railCommandSchema.parse(input),s=this.state,accountId=id.parse(authenticatedAccountId);
    if(c.attemptId!==s.attemptId||c.contentVersion!==s.definition.contentVersion)throw new Error('rail_definition_conflict');
    const digest=canonicalJson(c),old=s.receipts.find(r=>r.accountId===accountId&&r.commandId===c.commandId);
    if(old){if(old.digest!==digest)throw new Error('rail_command_key_conflict');return structuredClone(old.receipt);}
    const a=railAvailabilitySchema.parse(availability);
    const train=c.action.kind==='CONFIRM_ROUTE';
    if(accountId!==(train?s.definition.trainAccountId:s.definition.operatorAccountId))throw new Error('rail_role_forbidden');
    if(c.expectedRevision!==s.revision)throw new Error('rail_revision_conflict');
    this.checkTime(now);const elapsed=now-s.definition.startedAt;
    if(elapsed>=180000)throw new Error('rail_attempt_ended');
    const r=this.config(c.roundId),current=s.rounds.find(x=>x.roundId===c.roundId)!;
    if(current.status!=='OPEN')throw new Error('rail_round_not_open');
    if(c.action.kind==='CONFIRM_ROUTE'){
      if(elapsed<r.previewAt||elapsed>=r.lockAt)throw new Error('rail_window_closed');
      if(current.confirmed)throw new Error('rail_route_already_confirmed');
      if(!a.trainAvailable||!a.trainConnected)throw new Error('rail_actor_unavailable');
      current.route=c.action.side;current.confirmed=true;
    }else{
      if(!this.internalNeeded(r)||elapsed<r.internalOpenAt||elapsed>=r.internalCloseAt)throw new Error('rail_window_closed');
      if(current.internalAction!==null)throw new Error('rail_internal_already_accepted');
      if(!a.operatorAvailable||!a.operatorConnected||!a.trainAvailable)throw new Error('rail_actor_unavailable');
      current.internalAction=c.action.actionId;
    }
    s.now=now;s.revision++;
    const receipt={commandId:c.commandId,roundId:c.roundId,acceptedRevision:s.revision,acceptedAt:now,route:current.route,internalAction:current.internalAction};
    s.receipts.push({accountId,commandId:c.commandId,digest,receipt});return structuredClone(receipt);
  }
  activeBuff(now:number) {this.checkTime(now);const b=this.state.buff;return b&&now>=b.startsAt&&now<b.endsAt&&now<this.state.definition.startedAt+180000?structuredClone(b):null;}
}

const worldPattern=(d:RailDefinition,phase:number)=>worldBossPatterns[d.bossId][phase-1]!;
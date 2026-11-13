import {z} from 'zod';

const id = z.string().min(1).max(200);
const revision = z.number().int().nonnegative().max(Number.MAX_SAFE_INTEGER - 1);
const actorSchema = z.strictObject({accountId:id, environment:id});
const issueSchema = actorSchema.extend({runId:id,eventId:id,stageId:id,catalogVersion:id,selectionOwner:id,
  eligibleIds:z.array(id).min(1).max(1000)}).superRefine((v,c)=>{
  if(v.accountId!==v.selectionOwner || new Set(v.eligibleIds).size!==v.eligibleIds.length)
    c.addIssue({code:'custom',message:'invalid_solo_issue'});
});
const slotSchema = z.strictObject({slotIndex:z.number().int().min(0).max(2), candidateId:id,
  freeRemaining:z.number().int().min(0).max(3),extraRemaining:z.number().int().min(0).max(1)});
const snapshotSchema = z.strictObject({runId:id,eventId:id,stageId:id,catalogVersion:id,grantRevision:revision,
  state:z.enum(['SELECTING','CONFIRMED']),slots:z.array(slotSchema).min(1).max(3),selectedCandidateId:id.nullable()});
const selectSchema = z.object({eventId:id,expectedRevision:revision,slotIndex:z.number().int().min(0).max(2),candidateId:id});
const rerollSchema = selectSchema.extend({mode:z.enum(['FREE','EXTRA'])}).strict();
const targetSchema = actorSchema.extend({runId:id,eventId:id,stageId:id,catalogVersion:id,slotIndex:z.number().int().min(0).max(2),
  candidateId:id,grantRevision:revision,extraOrdinal:z.literal(1)});
const approvalSchema = z.strictObject({target:targetSchema,approvalId:id,cause:z.enum(['AD_SSV','PURCHASE_ENTITLEMENT'])});
const resultSchema = z.strictObject({outcome:z.enum(['REROLLED','NO_CANDIDATE','SELECTED']),snapshot:snapshotSchema,
  approval:approvalSchema.nullable()});
const checkpointSchema = z.strictObject({version:z.literal('solo-augment-selection-v1'),issue:issueSchema,snapshot:snapshotSchema,
  receipts:z.array(z.strictObject({key:id,body:z.string(),result:resultSchema}))});

export type ExtraTarget = z.infer<typeof targetSchema>;
export type ExtraApproval = z.infer<typeof approvalSchema>;
/** Trusted server adapter only. It must verify/serialize revocation and globally reserve
 * provider transactions for this exact target, returning the same irrevocable approval
 * on retry. Client bools/SDK completion signals are not implementations of this port.
 * No production adapter, provider receipt storage or network route is supplied here. */
export interface ExtraApprovalPort {authorize(target:ExtraTarget):Promise<ExtraApproval>}
export type SelectionSnapshot = z.infer<typeof snapshotSchema>;
export type SelectionResult = z.infer<typeof resultSchema>;
export type RerollCommand = z.infer<typeof rerollSchema>;
export type SelectCommand = z.infer<typeof selectSchema>;
type Actor = z.infer<typeof actorSchema>;
type Issue = z.infer<typeof issueSchema>;
// Trusted store supplies the actual approver after checking shared input authority.
// Grant ownership and budgets remain on the original issuance.
type Dependencies = {drawIndex?:(exclusiveMax:number)=>number;extraApproval:ExtraApprovalPort;approvalAccountId?:string};
type Receipt = {key:string;body:string;result:SelectionResult};

function uniformIndex(n:number):number {
  const range=0x100000000, limit=range-(range%n);
  let value:number;
  do {value=element(crypto.getRandomValues(new Uint32Array(1)),0);} while(value>=limit);
  return value%n;
}
const same = (a:unknown,b:unknown) => JSON.stringify(a)===JSON.stringify(b);
function element<T>(values:ArrayLike<T>,index:number):T {
  const value=values[index]; if(value===undefined)throw Error('invalid_selection_state');return value;
}

/** One server-owned solo grant. Single in-process owner only; not a distributed lock.
 * Issuer must enforce run/stage uniqueness and supply the actual eligible catalog.
 * checkpoint() is an INTERNAL persistence boundary, never a client restore payload.
 * CONFIRMED records a choice only, not execution of its effect or a reward grant. */
export class SoloAugmentSelection {
  private readonly pending=new Map<string,{body:string;result:Promise<SelectionResult>}>();
  private constructor(private readonly issuance:Issue, private snapshot:SelectionSnapshot,
    private readonly receipts:Map<string,Receipt>, private readonly dependencies:Dependencies) {}

  static issue(input:unknown, dependencies:Dependencies):SoloAugmentSelection {
    const issuance=issueSchema.parse(input);
    const result=new SoloAugmentSelection(issuance,{runId:issuance.runId,eventId:issuance.eventId,
      stageId:issuance.stageId,catalogVersion:issuance.catalogVersion,grantRevision:0,state:'SELECTING',slots:[],selectedCandidateId:null},new Map(),dependencies);
    const pool=[...issuance.eligibleIds];
    // Zero candidates belong to the map event owner's atomic no-award completion,
    // not an empty SELECTING grant. That production owner is not supplied here.
    for(let i=0;i<Math.min(3,issuance.eligibleIds.length);i++) {
      const candidateId=element(pool.splice(result.draw(pool.length),1),0);
      result.snapshot.slots.push({slotIndex:i,candidateId,freeRemaining:3,extraRemaining:1});
    }
    return result;
  }

  static restore(checkpoint:string, dependencies:Dependencies):SoloAugmentSelection {
    const state=checkpointSchema.parse(JSON.parse(checkpoint));
    const snapshots=[state.snapshot,...state.receipts.map(r=>r.result.snapshot)];
    for(const s of snapshots) {
      if(s.runId!==state.issue.runId || s.eventId!==state.issue.eventId || s.stageId!==state.issue.stageId ||
        s.catalogVersion!==state.issue.catalogVersion || s.slots.length!==Math.min(3,state.issue.eligibleIds.length) ||
        new Set(s.slots.map(x=>x.candidateId)).size!==s.slots.length ||
        s.slots.some((x,i)=>x.slotIndex!==i || !state.issue.eligibleIds.includes(x.candidateId) || (x.extraRemaining===0 && x.freeRemaining!==0)) ||
        (s.state==='SELECTING' ? s.selectedCandidateId!==null : !s.slots.some(x=>x.candidateId===s.selectedCandidateId)) ||
        s.grantRevision>state.snapshot.grantRevision) throw Error('invalid_checkpoint');
    }
    if(new Set(state.receipts.map(r=>r.key)).size!==state.receipts.length)throw Error('invalid_checkpoint');
    return new SoloAugmentSelection(state.issue,state.snapshot,new Map(state.receipts.map(r=>[r.key,r])),dependencies);
  }

  read():SelectionSnapshot {return structuredClone(this.snapshot);}
  checkpoint():string {return JSON.stringify({version:'solo-augment-selection-v1',issue:this.issuance,
    snapshot:this.snapshot,receipts:[...this.receipts.values()]});}

  async reroll(actorInput:Actor,key:string,commandInput:RerollCommand):Promise<SelectionResult> {
    this.checkActor(actorInput); id.parse(key);
    const command=rerollSchema.parse(commandInput), body=JSON.stringify({operation:'REROLL',command});
    const replay=this.replay(key,body); if(replay)return replay;
    const pending=this.pending.get(key);
    if(pending) {
      if(pending.body!==body)throw Error('request_conflict');
      return structuredClone(await pending.result);
    }
    const result=Promise.resolve().then(()=>this.executeReroll(key,body,command));
    this.pending.set(key,{body,result});
    try {return structuredClone(await result);} finally {this.pending.delete(key);}
  }

  private rerollContext(command:RerollCommand) {
    this.checkCurrent(command);
    const slot=element(this.snapshot.slots,command.slotIndex);
    if(command.mode==='FREE' && slot.freeRemaining===0)throw Error('free_exhausted');
    if(command.mode==='EXTRA' && slot.freeRemaining!==0)throw Error('free_remaining');
    if(command.mode==='EXTRA' && slot.extraRemaining===0)throw Error('extra_exhausted');
    const excluded=new Set(this.snapshot.slots.map(x=>x.candidateId));
    const pool=this.issuance.eligibleIds.filter(x=>!excluded.has(x));
    return {slot,pool};
  }
  previewExtra(actor:Actor,commandInput:SelectCommand):ExtraTarget|null {
    this.checkActor(actor);
    const command=selectSchema.parse(commandInput);
    const {pool}=this.rerollContext({...command,mode:'EXTRA'});
    if(pool.length===0)return null;
    return {accountId:id.parse(this.dependencies.approvalAccountId??this.issuance.accountId),environment:this.issuance.environment,
      runId:this.issuance.runId,eventId:this.issuance.eventId,stageId:this.issuance.stageId,catalogVersion:this.issuance.catalogVersion,
      slotIndex:command.slotIndex,candidateId:command.candidateId,grantRevision:command.expectedRevision,extraOrdinal:1};
  }

  private async executeReroll(key:string,body:string,command:RerollCommand):Promise<SelectionResult> {
    const {slot,pool}=this.rerollContext(command);
    if(pool.length===0)return this.remember(key,body,'NO_CANDIDATE',null);

    let approval:ExtraApproval|null=null;
    if(command.mode==='EXTRA') {
      const target:ExtraTarget={accountId:id.parse(this.dependencies.approvalAccountId??this.issuance.accountId),environment:this.issuance.environment,
        runId:this.issuance.runId,eventId:this.issuance.eventId,stageId:this.issuance.stageId,catalogVersion:this.issuance.catalogVersion,
        slotIndex:command.slotIndex,candidateId:command.candidateId,grantRevision:command.expectedRevision,extraOrdinal:1};
      approval=approvalSchema.parse(await this.dependencies.extraApproval.authorize(structuredClone(target)));
      if(!same(approval.target,target))throw Error('approval_binding_mismatch');
      const completed=this.replay(key,body); if(completed)return completed;
      // A different reroll or final selection can win while authority is awaited.
      this.checkCurrent(command);
    }
    const candidate=element(pool,this.draw(pool.length));
    this.snapshot.slots[command.slotIndex]={...slot,candidateId:candidate,
      freeRemaining:slot.freeRemaining-(command.mode==='FREE'?1:0),extraRemaining:slot.extraRemaining-(command.mode==='EXTRA'?1:0)};
    this.snapshot.grantRevision++;
    return this.remember(key,body,'REROLLED',approval);
  }

  select(actorInput:Actor,key:string,commandInput:SelectCommand):SelectionResult {
    this.checkActor(actorInput); id.parse(key);
    const command=selectSchema.parse(commandInput), body=JSON.stringify({operation:'SELECT',command});
    const replay=this.replay(key,body); if(replay)return replay;
    if(this.pending.has(key))throw Error('request_conflict');
    this.checkCurrent(command);
    this.snapshot={...this.snapshot,state:'CONFIRMED',selectedCandidateId:command.candidateId,grantRevision:this.snapshot.grantRevision+1};
    return this.remember(key,body,'SELECTED',null);
  }

  private checkActor(input:Actor):void {
    const actor=actorSchema.parse(input);
    if(actor.accountId!==this.issuance.selectionOwner || actor.environment!==this.issuance.environment)throw Error('owner_mismatch');
  }
  private checkCurrent(command:SelectCommand):void {
    if(command.eventId!==this.issuance.eventId)throw Error('event_mismatch');
    if(this.snapshot.state!=='SELECTING')throw Error('selection_closed');
    if(command.expectedRevision!==this.snapshot.grantRevision)throw Error('revision_conflict');
    if(command.candidateId!==this.snapshot.slots[command.slotIndex]?.candidateId)throw Error('candidate_mismatch');
  }
  private draw(n:number):number {
    const value=(this.dependencies.drawIndex??uniformIndex)(n);
    if(!Number.isInteger(value)||value<0||value>=n)throw Error('invalid_random_index');
    return value;
  }
  private replay(key:string,body:string):SelectionResult|null {
    const receipt=this.receipts.get(key); if(!receipt)return null;
    if(receipt.body!==body)throw Error('request_conflict');
    return structuredClone(receipt.result);
  }
  private remember(key:string,body:string,outcome:SelectionResult['outcome'],approval:ExtraApproval|null):SelectionResult {
    const result={outcome,snapshot:this.read(),approval};
    this.receipts.set(key,{key,body,result:structuredClone(result)});
    return structuredClone(result);
  }
}

export {issueSchema as soloSelectionIssueSchema,resultSchema as soloSelectionResultSchema,selectSchema as soloSelectSchema,rerollSchema as soloRerollSchema};

// Shared exact approval binding; transport adapters must not maintain a divergent shape.
export const augmentExtraTargetSchema = targetSchema;

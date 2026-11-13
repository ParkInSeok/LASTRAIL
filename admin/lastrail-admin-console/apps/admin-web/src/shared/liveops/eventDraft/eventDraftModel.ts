import {eventDefinitionDraftSchema,type EventDefinitionDraft} from "../eventPreview/eventPreviewContract";
import {eventDraftSavedSchema,eventDraftListSchema,eventDraftSaveSchema,eventDraftRequestKey,assertEventDraftReceipt,type EventDraftSaved,type EventDraftSave,type EventDraftList,type EventDraftPort} from "./eventDraftContract";
import type {EventPreviewScope} from "../eventPreview/eventPreviewModel";
export type EventDraftPending=Readonly<{roundId:string;key:string;command:EventDraftSave;phase:"pending"|"uncertain"}>;
export type EventDraftSnapshot=Readonly<{phase:"idle"|"loading"|"ready"|"saving"|"uncertain"|"conflict"|"error"|"unauthorized"|"disposed";roundId:string|null;current:EventDraftSaved|null;editor:EventDefinitionDraft|null;lastReceipt:EventDraftSaved|null;pending:EventDraftPending|null;list:EventDraftList|null;error:string|null}>;
function freeze<T>(value:T):T{if(value&&typeof value==="object"){for(const child of Object.values(value))freeze(child);Object.freeze(value);}return value;}
function problem(error:unknown):{status?:number;errorCode?:string}{return error&&typeof error==="object"?error:{};}
export class EventDraftModel {
 readonly scope:EventPreviewScope;
 private state:EventDraftSnapshot=Object.freeze({phase:"idle",roundId:null,current:null,editor:null,lastReceipt:null,pending:null,list:null,error:null});
 private listeners=new Set<()=>void>();private epoch=0;private selection=0;private listEpoch=0;
 private cache=new Map<string,EventDraftSaved>();private floors=new Map<string,number>();private loaded=new Set<string>();
 private reads=new Map<string,Promise<EventDraftSaved|undefined>>();private listCache=new Map<string,EventDraftList>();
 constructor(scope:EventPreviewScope,private readonly permissions:readonly string[],private readonly port:EventDraftPort){if(!scope.principalId||!scope.authorizationGeneration)throw new Error("EVENT_DRAFT_SCOPE_REQUIRED");this.scope=Object.freeze({...scope});this.permissions=Object.freeze([...permissions]);}
 snapshot(){return this.state;}
 get canWrite(){return this.active()&&this.permissions.includes("EVENT_CONFIG_WRITE");}
 private active(){return this.state.phase!=="disposed"&&this.state.phase!=="unauthorized";}
 matchesScope(scope:EventPreviewScope){return this.active()&&this.scope.principalId===scope.principalId&&this.scope.environment===scope.environment&&this.scope.authorizationGeneration===scope.authorizationGeneration;}
 private assertActive(){if(this.state.phase==="disposed")throw new Error("EVENT_DRAFT_MODEL_DISPOSED");if(this.state.phase==="unauthorized")throw new Error("EVENT_DRAFT_AUTHORIZATION_INVALIDATED");}
 subscribe(listener:()=>void){this.assertActive();this.listeners.add(listener);return ()=>{this.listeners.delete(listener);};}
 private update(patch:Partial<EventDraftSnapshot>){this.state=Object.freeze({...this.state,...patch});for(const listener of this.listeners)listener();}
 private purge(){this.epoch++;this.selection++;this.cache.clear();this.floors.clear();this.loaded.clear();this.reads.clear();this.listCache.clear();this.update({phase:"unauthorized",roundId:null,current:null,editor:null,lastReceipt:null,pending:null,list:null,error:"EVENT_DRAFT_AUTHORIZATION_INVALIDATED"});}
 private denial(error:unknown){if(this.active()&&[401,403].includes(problem(error).status??0)){this.purge();return true;}return false;}
 private rememberRound(roundId:string,revision:number){
  const floor=Math.max(this.floors.get(roundId)??0,revision);this.floors.delete(roundId);this.floors.set(roundId,floor);
  // Bounded lightweight authority metadata; retire old projections together.
  while(this.floors.size>64){const retired=[...this.floors.keys()].find(id=>id!==this.state.roundId&&id!==this.state.pending?.roundId);if(retired===undefined)break;
   this.floors.delete(retired);this.loaded.delete(retired);this.cache.delete(retired);this.reads.delete(retired);
   for(const [key,list] of this.listCache)if(list.items.some(row=>row.roundId===retired))this.listCache.delete(key);
   if(this.state.list?.items.some(row=>row.roundId===retired))this.state=Object.freeze({...this.state,list:null});
  }
 }
 private validateCurrent(raw:unknown,roundId:string){const value=eventDraftSavedSchema.parse(raw);if(value.environment!==this.scope.environment||value.roundId!==roundId)throw new Error("EVENT_DRAFT_SCOPE_MISMATCH");return value;}
 private accept(value:EventDraftSaved,receipt=false){
  const floor=this.floors.get(value.roundId)??0;if(value.revision<floor){if(receipt)return this.cache.get(value.roundId);throw new Error("EVENT_DRAFT_REVISION_REGRESSION");}
  const previous=this.cache.get(value.roundId);if(previous&&previous.revision===value.revision&&previous.definitionHash!==value.definitionHash)throw new Error("EVENT_DRAFT_IMMUTABLE_REVISION_MISMATCH");
  const saved=previous?.revision===value.revision?previous:freeze(value);this.cache.set(value.roundId,saved);this.rememberRound(value.roundId,value.revision);this.loaded.add(value.roundId);while(this.cache.size>20){const retired=[...this.cache.keys()].find(id=>id!==this.state.roundId&&id!==this.state.pending?.roundId);if(retired===undefined)break;this.cache.delete(retired);}return saved;
 }
 async open(roundId:string,refresh=false){
  this.assertActive();roundId=eventDefinitionDraftSchema.shape.roundId.parse(roundId);const changed=roundId!==this.state.roundId;if(changed)this.selection++;const selection=this.selection;
  const cached=this.cache.get(roundId),valid=cached&&cached.revision>=(this.floors.get(roundId)??0);
  this.update({roundId,current:valid?cached:null,...(changed?{editor:null}:{}),error:null});
  if(!refresh&&valid){this.update({phase:"ready"});return cached;}
  this.update({phase:"loading"});
  try{const value=await this.readCurrent(roundId);if(this.active()&&selection===this.selection&&this.state.roundId===roundId)this.update({phase:"ready",current:value??null});return value;}
  catch(error){if(this.active()&&selection===this.selection)this.update({phase:"error",error:problem(error).errorCode??(error instanceof Error?error.message:"EVENT_DRAFT_READ_UNAVAILABLE")});throw error;}
 }
 private readCurrent(roundId:string):Promise<EventDraftSaved|undefined>{
  if(this.reads.has(roundId))return this.reads.get(roundId)!;const epoch=this.epoch;
  const promise=this.port.read(roundId).then(raw=>{if(!this.active()||epoch!==this.epoch||this.reads.get(roundId)!==promise)return undefined;return this.accept(this.validateCurrent(raw,roundId));}).catch(error=>{
   if(epoch!==this.epoch||!this.active())throw error;if(this.denial(error))throw error;if(this.reads.get(roundId)!==promise)return undefined;
   if(problem(error).status===404&&problem(error).errorCode==="event_draft_not_found"&&!(this.floors.get(roundId)??0)){this.rememberRound(roundId,0);this.loaded.add(roundId);return undefined;}throw error;
  }).finally(()=>{if(this.reads.get(roundId)===promise)this.reads.delete(roundId);});this.reads.set(roundId,promise);return promise;
 }
 async loadList(after="",limit=20,refresh=false){
  this.assertActive();const key=JSON.stringify([after,limit]);if(!refresh&&this.listCache.has(key)){const list=this.listCache.get(key)!;this.update({list});return list;}const epoch=this.epoch,listEpoch=this.listEpoch;
  try{const raw=await this.port.list(after,limit);if(!this.active()||epoch!==this.epoch||listEpoch!==this.listEpoch)return undefined;const list=eventDraftListSchema.parse(raw);if(list.environment!==this.scope.environment)throw new Error("EVENT_DRAFT_SCOPE_MISMATCH");
   for(const row of list.items){const floor=this.floors.get(row.roundId)??0;if(row.revision<floor)throw new Error("EVENT_DRAFT_REVISION_REGRESSION");this.rememberRound(row.roundId,row.revision);}
   const immutable=freeze(list);this.listCache.set(key,immutable);while(this.listCache.size>20)this.listCache.delete(this.listCache.keys().next().value!);this.update({list:immutable,...(this.state.current&&this.state.current.revision<(this.floors.get(this.state.current.roundId)??0)?{current:null,error:"EVENT_DRAFT_CURRENT_REFRESH_REQUIRED"}:{} )});return immutable;
  }catch(error){if(epoch===this.epoch)this.denial(error);throw error;}
 }
 edit(input:EventDefinitionDraft){this.assertActive();if(this.state.pending)throw new Error("EVENT_DRAFT_RECOVERY_REQUIRED");const definition=eventDefinitionDraftSchema.parse(input);if(definition.environment!==this.scope.environment||definition.roundId!==this.state.roundId)throw new Error("EVENT_DRAFT_SCOPE_MISMATCH");this.update({editor:freeze(definition),error:null});}
 async save(reason:string,key:string){
  this.assertActive();if(!this.canWrite)throw new Error("EVENT_CONFIG_WRITE_REQUIRED");if(this.state.pending)throw new Error("EVENT_DRAFT_RECOVERY_REQUIRED");const {roundId,editor,current}=this.state;
  if(!roundId||!editor||!this.loaded.has(roundId)||((this.floors.get(roundId)??0)!==(current?.revision??0)))throw new Error("EVENT_DRAFT_CURRENT_REQUIRED");
  const command=eventDraftSaveSchema.parse({operation:"SAVE_DRAFT",reason,expectedRevision:current?.revision??null,definition:editor});if(editor.revision!==(command.expectedRevision??0)+1)throw new Error("EVENT_DRAFT_PROPOSED_REVISION_MISMATCH");eventDraftRequestKey.parse(key);
  this.update({phase:"saving",pending:freeze({roundId,key,command,phase:"pending"}),error:null});return this.execute();
 }
 async recover(){this.assertActive();if(!this.canWrite)throw new Error("EVENT_CONFIG_WRITE_REQUIRED");if(this.state.pending?.phase!=="uncertain")throw new Error("EVENT_DRAFT_NO_UNCERTAIN_REQUEST");this.update({phase:"saving",pending:freeze({...this.state.pending,phase:"pending"})});return this.execute();}
 private async execute(){
  const pending=this.state.pending!,epoch=this.epoch;
  try{const raw=await this.port.save(pending.roundId,pending.command,pending.key);if(!this.active()||epoch!==this.epoch)return undefined;
   const receipt=freeze(assertEventDraftReceipt(raw,pending.command,this.scope.principalId)),priorFloor=this.floors.get(receipt.roundId)??0,current=this.accept(receipt,true);if(receipt.revision>priorFloor){this.listEpoch++;this.listCache.clear();}
   this.update({phase:"ready",lastReceipt:receipt,pending:null,list:receipt.revision>priorFloor?null:this.state.list,error:null,...(this.state.roundId===pending.roundId?{current:current??null,...(current?.revision===receipt.revision?{editor:null}:{})}:{})});return receipt;
  }catch(error){
   if(!this.active()||epoch!==this.epoch)throw error;if(this.denial(error))throw error;const details=problem(error);
   if(details.status===409&&details.errorCode==="event_revision_conflict"){
    this.update({pending:null,phase:"conflict",error:"event_revision_conflict"});
    try{const current=await this.readCurrent(pending.roundId);if(this.active()&&this.state.roundId===pending.roundId)this.update({phase:"conflict",current:current??null,error:"event_revision_conflict"});}catch(readError){if(this.active())this.update({phase:"conflict",error:"EVENT_DRAFT_CONFLICT_CURRENT_UNAVAILABLE"});}
   }else if([400,404,409].includes(details.status??0))this.update({pending:null,phase:"error",error:details.errorCode??"EVENT_DRAFT_COMMAND_REJECTED"});
   else this.update({pending:freeze({...pending,phase:"uncertain"}),phase:"uncertain",error:details.errorCode??"EVENT_DRAFT_OUTCOME_UNKNOWN"});throw error;
  }
 }
 dispose(){if(this.state.phase==="disposed")return;this.epoch++;this.selection++;this.cache.clear();this.floors.clear();this.loaded.clear();this.reads.clear();this.listCache.clear();this.state=Object.freeze({phase:"disposed",roundId:null,current:null,editor:null,lastReceipt:null,pending:null,list:null,error:null});this.listeners.clear();}
}

import {eventDefinitionDraftSchema,eventDefinitionPreviewSchema,eventDefinitionKey,type EventDefinitionDraft,type EventDefinitionPreview} from "./eventPreviewContract";
export type EventPreviewScope=Readonly<{principalId:string;environment:"DEV"|"PRD";authorizationGeneration:string}>;
export type EventPreviewSnapshot=Readonly<{phase:"empty"|"draft"|"loading"|"ready"|"error"|"unauthorized"|"disposed";draft:EventDefinitionDraft|null;preview:EventDefinitionPreview|null;error:string|null}>;
export type EventPreviewPort=(input:EventDefinitionDraft)=>Promise<unknown>;
function immutable<T>(value:T):T{if(value&&typeof value==="object"){for(const child of Object.values(value))immutable(child);Object.freeze(value);}return value;}
function status(error:unknown){return error&&typeof error==="object"&&"status" in error?error.status:undefined;}
/** One bounded preview belongs to one authenticated scope and exact proposed round/revision/content.
 * The POST is read-only. Reopening uses a valid preview; refresh is an explicit read, never execution.
 */
export class EventPreviewModel {
 readonly scope:EventPreviewScope;
 private state:EventPreviewSnapshot=Object.freeze({phase:"empty",draft:null,preview:null,error:null});
 private listeners=new Set<()=>void>();
 private generation=0;
 private key:string|null=null;
 private inflight:Promise<EventDefinitionPreview|undefined>|null=null;
 constructor(scope:EventPreviewScope,private readonly request:EventPreviewPort){if(!scope.principalId||!scope.authorizationGeneration)throw new Error("EVENT_SCOPE_REQUIRED");this.scope=Object.freeze({...scope});}
 snapshot(){return this.state;}
 matchesScope(scope:EventPreviewScope){return !["disposed","unauthorized"].includes(this.state.phase)&&this.scope.principalId===scope.principalId&&this.scope.environment===scope.environment&&this.scope.authorizationGeneration===scope.authorizationGeneration;}
 private assertActive(){if(this.state.phase==="disposed")throw new Error("EVENT_MODEL_DISPOSED");if(this.state.phase==="unauthorized")throw new Error("EVENT_AUTHORIZATION_INVALIDATED");}
 private replace(next:EventPreviewSnapshot){this.state=Object.freeze(next);for(const listener of this.listeners)listener();}
 subscribe(listener:()=>void){this.assertActive();this.listeners.add(listener);return ()=>{this.listeners.delete(listener);};}
 setDraft(raw:EventDefinitionDraft){
  this.assertActive();const draft=eventDefinitionDraftSchema.parse(raw);if(draft.environment!==this.scope.environment)throw new Error("EVENT_ENVIRONMENT_MISMATCH");
  const key=eventDefinitionKey(draft);if(key===this.key)return;
  this.generation++;this.key=key;this.inflight=null;this.replace({phase:"draft",draft:immutable(draft),preview:null,error:null});
 }
 async preview(refresh=false):Promise<EventDefinitionPreview|undefined>{
  this.assertActive();if(!this.state.draft)throw new Error("EVENT_DRAFT_REQUIRED");
  if(!refresh&&this.state.phase==="ready"&&this.state.preview)return this.state.preview;
  if(this.inflight)return this.inflight;
  const generation=this.generation,key=this.key!,draft=this.state.draft;
  this.replace({phase:"loading",draft,preview:null,error:null});
  const promise=this.request(draft).then(raw=>{
   if(generation!==this.generation||["disposed","unauthorized"].includes(this.state.phase))return undefined;
   const preview=eventDefinitionPreviewSchema.parse(raw);
   if(eventDefinitionKey(preview.definition)!==key)throw new Error("EVENT_PREVIEW_REQUEST_MISMATCH");
   const result=immutable(preview);this.replace({phase:"ready",draft,preview:result,error:null});return result;
  }).catch(error=>{
   if(["disposed","unauthorized"].includes(this.state.phase))throw error;
   if(status(error)===401||status(error)===403){this.generation++;this.key=null;this.inflight=null;this.replace({phase:"unauthorized",draft:null,preview:null,error:"EVENT_AUTHORIZATION_INVALIDATED"});}
   else if(generation===this.generation)this.replace({phase:"error",draft,preview:null,error:error instanceof Error?error.message:"EVENT_PREVIEW_UNAVAILABLE"});
   throw error;
  }).finally(()=>{if(this.inflight===promise)this.inflight=null;});
  this.inflight=promise;return promise;
 }
 dispose(){if(this.state.phase==="disposed")return;this.generation++;this.key=null;this.inflight=null;this.state=Object.freeze({phase:"disposed",draft:null,preview:null,error:null});this.listeners.clear();}
}

import {assertOpsReceipt,opsEventId,opsRequestKey,opsInvestigationCommandSchema,opsInvestigationReadSchema,opsInvestigationStateSchema,type OpsInvestigationCommand,type OpsInvestigationReceipt,type OpsInvestigationRead,type OpsInvestigationState} from "./opsInvestigationContract";
export type OpsInvestigationScope = Readonly<{principalId:string;environment:string;authorizationRevision:string}>;
export interface OpsInvestigationTransport {
  read(id:string):Promise<unknown>;
  write(id:string,command:OpsInvestigationCommand,key:string):Promise<unknown>;
}
export type OpsInvestigationSnapshot = Readonly<{investigation:OpsInvestigationState;recurrence:OpsInvestigationRead["recurrence"]|null}>;
export type OpsPendingCommand = Readonly<{errorLogId:string;key:string;command:OpsInvestigationCommand;phase:"pending"|"uncertain"}>;
function immutable<T>(value:T):T {
  if(value && typeof value === "object") { for(const child of Object.values(value))immutable(child);Object.freeze(value); }
  return value;
}
function failure(error:unknown):{status?:number;errorCode?:string;current?:unknown} {
  return error && typeof error === "object" ? error : {};
}
/** One instance belongs to one authenticated principal/environment/authorization revision.
 * Keep it above detail Views for reuse; dispose when that scope changes. No polling or TTL.
 * In-memory pending requests survive View close/reopen, not process loss; do not claim durable recovery.
 */
export class OpsInvestigationModel {
  readonly scope:OpsInvestigationScope;
  private readonly entries = new Map<string,OpsInvestigationSnapshot>();
  private readonly loads = new Map<string,Promise<OpsInvestigationSnapshot|undefined>>();
  private readonly listeners = new Set<()=>void>();
  private active = true;
  private authorized = true;
  private generation = 0;
  private pendingValue:OpsPendingCommand|null = null;
  private receiptValue:OpsInvestigationReceipt|null = null;
  get pending(){return this.pendingValue;}
  get lastReceipt(){return this.receiptValue;}
  constructor(scope:OpsInvestigationScope,private readonly transport:OpsInvestigationTransport){
    if(!scope.principalId||!scope.environment||!scope.authorizationRevision)throw new Error("OPS_SCOPE_REQUIRED");
    this.scope=Object.freeze({...scope});
  }
  matchesScope(scope:OpsInvestigationScope){return this.active && this.authorized && Object.keys(this.scope).every(key=>this.scope[key as keyof OpsInvestigationScope]===scope[key as keyof OpsInvestigationScope]);}
  private assertActive(){if(!this.active)throw new Error("OPS_MODEL_DISPOSED");if(!this.authorized)throw new Error("OPS_AUTHORIZATION_INVALIDATED");}
  private notify(){if(this.active)for(const listener of this.listeners)listener();}
  subscribe(listener:()=>void){this.assertActive();this.listeners.add(listener);return ()=>{this.listeners.delete(listener);};}
  get(id:string){return this.entries.get(id);}
  private accept(investigation:OpsInvestigationState,recurrence:OpsInvestigationRead["recurrence"]|null){
    const previous=this.entries.get(investigation.errorLogId);
    if(previous && (previous.investigation.version>investigation.version || previous.investigation.version===investigation.version&&recurrence===null))return previous;
    const entry=immutable({investigation,recurrence});
    this.entries.set(investigation.errorLogId,entry);
    // Bound confirmed detail cache; unresolved request is stored independently.
    while(this.entries.size>50)this.entries.delete(this.entries.keys().next().value!);
    return entry;
  }
  load(id:string,refresh=false):Promise<OpsInvestigationSnapshot|undefined>{
    this.assertActive();opsEventId.parse(id);
    if(!refresh&&this.entries.has(id))return Promise.resolve(this.entries.get(id));
    if(this.loads.has(id))return this.loads.get(id)!;
    const generation=this.generation;
    const promise=this.transport.read(id).then(raw=>{
      if(!this.active||generation!==this.generation)return undefined;
      const result=opsInvestigationReadSchema.parse(raw);
      if(result.investigation.errorLogId!==id)throw new Error("OPS_INVESTIGATION_ID_MISMATCH");
      const entry=this.accept(result.investigation,result.recurrence);this.notify();return entry;
    }).catch(error=>{if(this.active&&generation===this.generation&&(failure(error).status===401||failure(error).status===403))this.clearProtected();throw error;}).finally(()=>{if(this.loads.get(id)===promise)this.loads.delete(id);});
    this.loads.set(id,promise);return promise;
  }
  async submit(id:string,input:OpsInvestigationCommand,key:string):Promise<OpsInvestigationReceipt|undefined>{
    this.assertActive();if(this.pendingValue)throw new Error("OPS_PENDING_RECOVERY_REQUIRED");
    opsEventId.parse(id);opsRequestKey.parse(key);const command=opsInvestigationCommandSchema.parse(input);
    this.pendingValue=immutable({errorLogId:id,key,command,phase:"pending"});this.notify();
    return this.execute();
  }
  async recover(){
    this.assertActive();if(!this.pendingValue||this.pendingValue.phase!=="uncertain")throw new Error("OPS_NO_UNCERTAIN_REQUEST");
    this.pendingValue=immutable({...this.pendingValue,phase:"pending"});this.notify();return this.execute();
  }
  private async execute():Promise<OpsInvestigationReceipt|undefined>{
    const pending=this.pendingValue!,generation=this.generation;
    try{
      const raw=await this.transport.write(pending.errorLogId,pending.command,pending.key);
      if(!this.active||generation!==this.generation)return undefined;
      const receipt=immutable(assertOpsReceipt(raw,pending.errorLogId,pending.command));
      this.receiptValue=receipt;this.accept(receipt.investigation,null);this.pendingValue=null;this.notify();return receipt;
    }catch(error){
      if(!this.active||generation!==this.generation)throw error;
      const problem=failure(error);
      if(problem.status===401||problem.status===403){this.clearProtected();throw error;}
      if(problem.status===409&&problem.errorCode==="ops_investigation_version_conflict"){
        const parsed=opsInvestigationStateSchema.safeParse(problem.current);
        if(parsed.success&&parsed.data.errorLogId===pending.errorLogId){this.accept(parsed.data,null);this.pendingValue=null;}
        else this.pendingValue=immutable({...pending,phase:"uncertain"});
      }else if(problem.status===400||problem.status===404||problem.status===409){this.pendingValue=null;}
      else this.pendingValue=immutable({...pending,phase:"uncertain"});
      this.notify();throw error;
    }
  }
  private clearProtected(){this.authorized=false;this.generation++;this.entries.clear();this.loads.clear();this.pendingValue=null;this.receiptValue=null;this.notify();}
  dispose(){if(!this.active)return;this.active=false;this.generation++;this.entries.clear();this.loads.clear();this.pendingValue=null;this.receiptValue=null;this.listeners.clear();}
}

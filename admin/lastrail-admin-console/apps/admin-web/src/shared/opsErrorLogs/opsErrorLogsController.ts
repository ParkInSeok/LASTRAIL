import {OpsInvestigationModel,type OpsInvestigationScope} from "./opsInvestigationModel";
import type {OpsInvestigationCommand,OpsInvestigationRead,OpsInvestigationReceipt,OpsGroups} from "./opsInvestigationContract";
import type {OpsErrorLogReadFiltersPreview,OpsErrorLogListResponse,OpsErrorLogSummaryResponse,OpsErrorLogDetailResponse} from "./opsErrorLogContract";
type Filters=Partial<OpsErrorLogReadFiltersPreview>;
export interface OpsErrorLogsApi {
  opsErrorLogs(filters:Filters):Promise<OpsErrorLogListResponse>;
  opsErrorLogSummary(filters:Filters):Promise<OpsErrorLogSummaryResponse>;
  opsErrorLogGroups(filters:Filters):Promise<OpsGroups>;
  opsErrorLogDetail(id:string):Promise<OpsErrorLogDetailResponse>;
  opsErrorLogInvestigation(id:string):Promise<OpsInvestigationRead>;
  opsErrorLogInvestigationCommand(id:string,command:OpsInvestigationCommand,key:string):Promise<OpsInvestigationReceipt>;
}
function keyFor(kind:string,input:Filters|string){return JSON.stringify([kind,typeof input==="string"?input:Object.entries(input).filter(([,value])=>value!==undefined&&value!=="").sort(([a],[b])=>a.localeCompare(b))]);}
function freeze<T>(value:T):T {if(value&&typeof value==="object"){for(const child of Object.values(value))freeze(child);Object.freeze(value);}return value;}
function forbidden(error:unknown){return error!==null&&typeof error==="object"&&"status" in error&&(error.status===401||error.status===403);}
/** Cache read projections; original-event status never becomes investigation authority. */
export class OpsErrorLogReadModel {
  private active=true;
  private generation=0;
  private cache=new Map<string,unknown>();
  private inflight=new Map<string,Promise<unknown>>();
  constructor(private api:OpsErrorLogsApi,private changed:()=>void,private revoked:()=>void){}
  peek(kind:"list"|"summary"|"groups"|"detail",input:Filters|string){return this.cache.get(keyFor(kind,input));}
  private async acquire<T>(key:string,load:()=>Promise<T>,refresh:boolean):Promise<T|undefined>{
    if(!this.active)throw new Error("OPS_MODEL_DISPOSED");
    if(!refresh&&this.cache.has(key))return this.cache.get(key) as T;
    if(this.inflight.has(key))return this.inflight.get(key) as Promise<T|undefined>;
    const generation=this.generation;
    const promise=load().then(value=>{
      if(!this.active||generation!==this.generation)return undefined;
      this.cache.set(key,freeze(value));while(this.cache.size>50)this.cache.delete(this.cache.keys().next().value!);this.changed();return value;
    }).catch(error=>{if(this.active&&generation===this.generation&&forbidden(error))this.revoked();throw error;}).finally(()=>{if(this.inflight.get(key)===promise)this.inflight.delete(key);});
    this.inflight.set(key,promise);return promise;
  }
  list(filters:Filters,refresh=false){const input={...filters};return this.acquire(keyFor("list",input),()=>this.api.opsErrorLogs(input),refresh);}
  summary(filters:Filters,refresh=false){const input={...filters};return this.acquire(keyFor("summary",input),()=>this.api.opsErrorLogSummary(input),refresh);}
  groups(filters:Filters,refresh=false){const input={...filters};return this.acquire(keyFor("groups",input),()=>this.api.opsErrorLogGroups(input),refresh);}
  detail(id:string,refresh=false){return this.acquire(keyFor("detail",id),()=>this.api.opsErrorLogDetail(id),refresh);}
  dispose(){this.active=false;this.generation++;this.cache.clear();this.inflight.clear();}
}
/** Own above Views; replace and dispose on principal/environment/auth revision change.
 * Permissions are supplied by authenticated bootstrap; no role-derived grants here.
 */
export class OpsErrorLogsController {
  readonly scope:OpsInvestigationScope;
  readonly reads:OpsErrorLogReadModel;
  readonly investigations:OpsInvestigationModel;
  private readonly listeners=new Set<()=>void>();
  private active=true;
  private writeGranted:boolean;
  private unbind:()=>void;
  constructor(scope:OpsInvestigationScope,permissions:readonly string[],api:OpsErrorLogsApi){
    this.scope=Object.freeze({...scope});this.writeGranted=permissions.includes("OPS_ERROR_LOG_WRITE");
    const revoke=()=>{const listeners=[...this.listeners];this.dispose();for(const listener of listeners)listener();};
    const guard=async<T>(request:()=>Promise<T>)=>{try{return await request();}catch(error){if(forbidden(error)&&this.active)revoke();throw error;}};
    this.reads=new OpsErrorLogReadModel(api,()=>this.notify(),revoke);
    this.investigations=new OpsInvestigationModel(scope,{read:id=>guard(()=>api.opsErrorLogInvestigation(id)),write:(id,command,key)=>guard(()=>api.opsErrorLogInvestigationCommand(id,command,key))});
    this.unbind=this.investigations.subscribe(()=>this.notify());
  }
  get canInvestigate(){return this.active&&this.writeGranted;}
  matchesScope(scope:OpsInvestigationScope){return this.active&&this.investigations.matchesScope(scope);}
  private notify(){if(this.active)for(const listener of this.listeners)listener();}
  subscribe(listener:()=>void){if(!this.active)throw new Error("OPS_MODEL_DISPOSED");this.listeners.add(listener);return ()=>{this.listeners.delete(listener);};}
  async submit(id:string,command:OpsInvestigationCommand,key:string){if(!this.canInvestigate)throw new Error("OPS_ERROR_LOG_WRITE_REQUIRED");return this.investigations.submit(id,command,key);}
  async recover(){if(!this.canInvestigate)throw new Error("OPS_ERROR_LOG_WRITE_REQUIRED");return this.investigations.recover();}
  dispose(){if(!this.active)return;this.active=false;this.writeGranted=false;this.unbind();this.reads.dispose();this.investigations.dispose();this.listeners.clear();}
}

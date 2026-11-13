import {z} from 'zod';
import {immediateRequestSchema,type ImmediateReviewRequest} from './maintenanceImmediateReview';
import {currentV2Schema,reviewV2Schema,immediateReceiptSchema,validateImmediateReceipt,type ImmediateReceipt} from './maintenanceCommand';
const timestamp=z.number().int().nonnegative().safe(),revision=z.number().int().positive().safe();
export const maintenanceLoginSchema=z.object({contractVersion:z.literal('maintenance-login-v1'),serverTime:timestamp,policyRevision:revision,gameLoginAllowed:z.boolean(),webLoginAllowed:z.boolean(),title:z.string().nullable(),summary:z.string().nullable(),startsAt:timestamp.nullable(),estimatedEndAt:timestamp.nullable(),retryAfterSeconds:z.number().int().positive().safe().nullable()});
export const maintenanceRuntimeSchema=z.object({contractVersion:z.literal('admin-maintenance-runtime-v1'),environment:z.enum(['DEV','PRD']),applicationId:z.literal('LASTRAIL'),serverTime:timestamp,
 policy:z.object({revision,state:z.enum(['NORMAL','SCHEDULED','ACTIVE','COMPLETED','CANCELLED']),maintenanceId:z.string().min(1).nullable(),draftRevision:revision.nullable(),drainStartsAt:timestamp.nullable(),startsAt:timestamp.nullable(),estimatedEndAt:timestamp.nullable(),endedAt:timestamp.nullable(),lastChange:z.object({principalId:z.string().min(1),reason:z.string().trim().min(1).max(500),at:timestamp})}).refine(p=>p.state==='NORMAL'?p.revision===1&&[p.maintenanceId,p.draftRevision,p.drainStartsAt,p.startsAt,p.estimatedEndAt,p.endedAt].every(v=>v===null):p.maintenanceId!==null&&p.draftRevision!==null&&p.startsAt!==null&&(p.drainStartsAt===null||p.drainStartsAt<=p.startsAt)&&(p.estimatedEndAt===null||p.estimatedEndAt>p.startsAt)&&(['SCHEDULED','ACTIVE'].includes(p.state)?p.endedAt===null:p.endedAt!==null)),
 capabilities:z.object({canTransition:z.literal(false),gameLoginControl:z.literal(false),webModeControl:z.literal(false)})});
export type ReadResult<T>={kind:'ready';data:T}|{kind:'unavailable';confirmed:false;status?:number;code?:string}|{kind:'denied';status:401|403};
const unavailable=()=>({kind:'unavailable' as const,confirmed:false as const});
/** Only credential issuance errors may confirm a block; status reads never infer it. */
export function classifyLoginFailure(status:number,payload:unknown){const blocked=z.object({contractVersion:z.literal('maintenance-login-v1'),error:z.literal('maintenance_login_blocked'),confirmed:z.literal(true)});return status===503&&blocked.safeParse(payload).success?{kind:'blocked' as const,confirmed:true as const}:unavailable();}
export class MaintenanceReadAdapter {
 private readonly publicUrl:string;
 constructor(private readonly environment:'DEV'|'PRD',gameOrigin:string,private readonly fetcher:typeof fetch=fetch){const url=new URL(gameOrigin);if(!['http:','https:'].includes(url.protocol)||url.username||url.password||url.search||url.hash||url.pathname!=='/')throw new Error('Game origin required');this.publicUrl=new URL('/v1/public/maintenance/login',url).href;}
 login(signal:AbortSignal){return this.read(this.publicUrl,maintenanceLoginSchema,signal,'omit');}
 async runtime(signal:AbortSignal){const result=await this.read('/api/maintenance-runtime',maintenanceRuntimeSchema,signal,'same-origin');return result.kind==='ready'&&result.data.environment!==this.environment?unavailable():result;}
 async current(signal:AbortSignal){const result=await this.read('/api/maintenance/current-state',currentV2Schema,signal,'same-origin');return result.kind==='ready'&&result.data.environment!==this.environment?unavailable():result;}
 async review(input:ImmediateReviewRequest,signal:AbortSignal){const request=immediateRequestSchema.parse(input);const result=await this.read('/api/maintenance/immediate-review',reviewV2Schema,signal,'same-origin',request);if(result.kind==='ready'&&(result.data.current.environment!==this.environment||JSON.stringify(result.data.requested)!==JSON.stringify(request)))return unavailable();return result;}
 async apply(input:ImmediateReviewRequest,key:string):Promise<ReadResult<ImmediateReceipt>>{if(this.environment!=='DEV')return {kind:'denied',status:403};const request=immediateRequestSchema.parse(input);if(request.webMode!=='NORMAL'||!/^[\x21-\x7e]{1,128}$/.test(key))throw new Error('Unsupported command');const result=await this.read('/api/maintenance/immediate-apply',immediateReceiptSchema,new AbortController().signal,'same-origin',request,key);if(result.kind==='ready'){try{return {kind:'ready',data:validateImmediateReceipt(result.data,this.environment,request)};}catch{return unavailable();}}return result;}
 private async read<T>(url:string,schema:z.ZodType<T>,signal:AbortSignal,credentials:RequestCredentials,body?:unknown,key?:string):Promise<ReadResult<T>>{
  try {const response=await this.fetcher(url,{method:body?'POST':'GET',credentials,cache:'no-store',signal,headers:{accept:'application/json',...(body?{'content-type':'application/json'}:{}),...(key?{'Idempotency-Key':key}:{})},...(body?{body:JSON.stringify(body)}:{})});if(signal.aborted)return unavailable();if([401,403].includes(response.status))return {kind:'denied',status:response.status as 401|403};if(!response.ok){let code:string|undefined;try{const value=await response.json();if(typeof value.error==='string')code=value.error;}catch{/* Non-JSON failures remain unconfirmed. */}return {...unavailable(),status:response.status,code};}if(!response.headers.get('cache-control')?.split(',').some(v=>v.trim().toLowerCase()==='no-store'))return unavailable();const parsed=schema.safeParse(await response.json());return parsed.success&&!signal.aborted?{kind:'ready',data:parsed.data}:unavailable();}catch{return unavailable();}
 }
}
type Login=z.infer<typeof maintenanceLoginSchema>;
type ReadPort<T>={read:(signal:AbortSignal)=>Promise<ReadResult<T>>};
type Snapshot<T>={phase:'idle'|'loading'}|{phase:'ready';result:ReadResult<T>};
function freeze<T>(value:T):T{if(value&&typeof value==='object'){Object.values(value).forEach(freeze);Object.freeze(value);}return value;}
/** One selected read owns cancellation/state; callers keep public and authenticated reads separate. */
export class MaintenanceReadSession<T=Login> {
 private state:Snapshot<T>=Object.freeze({phase:'idle'});private generation=0;private pending?:AbortController;private listeners=new Set<()=>void>();
 constructor(private readonly port:ReadPort<T>){}
 snapshot=()=>this.state;
 subscribe=(listener:()=>void)=>{this.listeners.add(listener);return()=>{this.listeners.delete(listener);};};
 private set(state:Snapshot<T>){this.state=freeze(structuredClone(state));for(const listener of this.listeners)listener();}
 async refresh(){this.pending?.abort();const controller=new AbortController();this.pending=controller;const generation=++this.generation;this.set({phase:'loading'});let result:ReadResult<T>;try{result=await this.port.read(controller.signal);}catch{result=unavailable();}if(generation!==this.generation||controller.signal.aborted)return;this.pending=undefined;this.set({phase:'ready',result});}
 cancel(){++this.generation;this.pending?.abort();this.pending=undefined;this.set({phase:'idle'});}
 dispose(){this.listeners.clear();this.cancel();}
}

import {AdminApiError} from '../api/client';
import {noticeListSchema,noticeDetailSchema,noticeIdSchema,publicationStatus,noticeReceiptSchema,type NoticeDocument} from '../operations/noticeContract';
import {maintenanceListSchema,maintenanceDetailSchema,maintenanceReceiptSchema,type MaintenanceDocument} from '../operations/maintenanceContract';
export type LiveOpsKind='NOTICE'|'MAINTENANCE_PLAN';
export type LiveOpsRow=Readonly<{id:string;title:string;revision:number;status:string;updatedAt:number}>;
type List=Readonly<{items:readonly LiveOpsRow[];nextAfter:string|null;serverTime:number}>;
export type LiveOpsDetail=Readonly<{kind:LiveOpsKind;id:string;title:string;revision:number;status:string;document:NoticeDocument|MaintenanceDocument;publication:string|null;applied:boolean|null}>;
type Read<T>=Readonly<{phase:'idle'|'loading'|'ready'|'error';data?:T}>;
export type LiveOpsSnapshot=Readonly<{confirmed?:Readonly<{kind:LiveOpsKind;id:string;revision:number;status:string;auditId:string}>;phase:'active'|'revoked'|'disposed';kind:LiveOpsKind;selected:string|null;after:string;list:Read<List>;detail:Read<LiveOpsDetail>}>;
export type LiveOpsReadPort={list:(kind:LiveOpsKind,after:string)=>Promise<unknown>;detail:(kind:LiveOpsKind,id:string)=>Promise<unknown>};
type Entry={value?:unknown;pending?:Promise<void>};
function freeze<T>(value:T):T{if(value&&typeof value==='object'){Object.freeze(value);for(const v of Object.values(value))freeze(v);}return value;}
/** One authenticated actor/environment owns bounded read projections; View owns no API or domain commands. */
export class LiveOpsWorkspace {
 private state:LiveOpsSnapshot=freeze({phase:'active',kind:'NOTICE',selected:null,after:'',list:{phase:'idle'},detail:{phase:'idle'}});
 private cache=new Map<string,Entry>();private versions=new Map<string,number>();private listeners=new Set<()=>void>();
 constructor(readonly actor:string,readonly environment:'DEV'|'PRD',private readonly port:LiveOpsReadPort){if(!actor)throw new Error('운영자 확인 필요');}
 snapshot=()=>this.state;
 subscribe=(listener:()=>void)=>{if(this.state.phase!=='active')return()=>{};this.listeners.add(listener);return()=>{this.listeners.delete(listener);};};
 private publish(next:LiveOpsSnapshot){this.state=freeze(next);for(const fn of this.listeners)fn();}
 private key(kind:LiveOpsKind,id?:string){return kind+(id===undefined?':list:':':detail:')+(id??this.state.after);}
 private bounded(key:string,entry:Entry){this.cache.delete(key);this.cache.set(key,entry);while(this.cache.size>8)this.cache.delete(this.cache.keys().next().value!);}
 private known(kind:LiveOpsKind,id:string){const receipt=this.state.confirmed;return Math.max(this.versions.get(kind+':'+id)??0,receipt?.kind===kind&&receipt.id===id?receipt.revision:0);}
 private remember(kind:LiveOpsKind,id:string,revision:number){if(revision<this.known(kind,id))throw new Error('원본 버전이 확인된 변경보다 이전입니다.');const key=kind+':'+id;this.versions.delete(key);this.versions.set(key,revision);while(this.versions.size>64)this.versions.delete(this.versions.keys().next().value!);}
 private observeRows(kind:LiveOpsKind,rows:LiveOpsRow[]){for(const row of rows){const before=this.known(kind,row.id),key=this.key(kind,row.id),cached=this.cache.get(key),old=cached?.value as LiveOpsDetail|undefined;this.remember(kind,row.id,row.revision);if((old&&old.revision<row.revision)||(cached?.pending&&row.revision>before&&before>0)){this.cache.delete(key);if(this.state.kind===kind&&this.state.selected===row.id)this.publish({...this.state,detail:{phase:'idle'}});}}return rows;}
 private parseList(kind:LiveOpsKind,raw:unknown):List{
  if(kind==='NOTICE'){const r=noticeListSchema.parse(raw);if(r.environment!==this.environment)throw new Error('환경 불일치');return freeze({items:this.observeRows(kind,r.items.map(v=>({id:v.noticeId,title:v.document.title,revision:v.revision,status:v.status,updatedAt:v.updatedAt}))),nextAfter:r.nextAfter,serverTime:r.serverTime});}
  const r=maintenanceListSchema.parse(raw);if(r.environment!==this.environment)throw new Error('환경 불일치');return freeze({items:this.observeRows(kind,r.items.map(v=>({id:v.maintenanceId,title:v.document.title,revision:v.draftRevision,status:'DRAFT_NOT_APPLIED',updatedAt:v.updatedAt}))),nextAfter:r.nextAfter,serverTime:r.serverTime});
 }
 private parseDetail(kind:LiveOpsKind,id:string,raw:unknown):LiveOpsDetail{
  if(kind==='NOTICE'){const r=noticeDetailSchema.parse(raw);if(r.environment!==this.environment||r.notice.noticeId!==id||(r.publication&&r.publication.noticeId!==id))throw new Error('공지 응답 불일치');this.remember(kind,id,r.notice.revision);return freeze({kind,id,title:r.notice.document.title,revision:r.notice.revision,status:r.notice.effectiveStatus,document:r.notice.document,publication:publicationStatus(r.publication,r.serverTime),applied:null});}
  const r=maintenanceDetailSchema.parse(raw);if(r.environment!==this.environment||r.plan.maintenanceId!==id)throw new Error('점검 계획 응답 불일치');this.remember(kind,id,r.plan.draftRevision);return freeze({kind,id,title:r.plan.document.title,revision:r.plan.draftRevision,status:'DRAFT_NOT_APPLIED',document:r.plan.document,publication:null,applied:false});
 }
 private activeKey(key:string,detail:boolean){return this.state.phase==='active'&&key===(detail?this.state.selected?this.key(this.state.kind,this.state.selected):'':this.key(this.state.kind));}
 private async load(key:string,detail:boolean,read:()=>Promise<unknown>,parse:(v:unknown)=>unknown,refresh:boolean){
  if(refresh)this.cache.delete(key);let entry=this.cache.get(key);
  const apply=(data:unknown)=>{if(this.activeKey(key,detail))this.publish({...this.state,[detail?'detail':'list']:{phase:'ready',data}});};
  if(entry?.value!==undefined){this.bounded(key,entry);apply(entry.value);return;}
  if(entry?.pending){await entry.pending;if(entry.value!==undefined&&this.cache.get(key)===entry)apply(entry.value);return;}
  entry={};this.bounded(key,entry);const own=entry;
  if(this.activeKey(key,detail))this.publish({...this.state,[detail?'detail':'list']:{phase:'loading'}});
  own.pending=(async()=>{try{const raw=await read();if(this.state.phase!=='active'||this.cache.get(key)!==own)return;const value=parse(raw);own.value=value;apply(value);}catch(error){if(this.state.phase!=='active'||this.cache.get(key)!==own)return;this.cache.delete(key);if(error instanceof AdminApiError&&[401,403].includes(error.status)){this.revoke();return;}if(this.activeKey(key,detail))this.publish({...this.state,[detail?'detail':'list']:{phase:'error'}});}finally{own.pending=undefined;}})();await own.pending;
 }
 async open(kind:LiveOpsKind,refresh=false,after=''){if(this.state.phase!=='active')return;if(kind!=='NOTICE'&&kind!=='MAINTENANCE_PLAN')throw new Error('지원하지 않는 운영 영역');if(after)noticeIdSchema.parse(after);const changed=kind!==this.state.kind||after!==this.state.after;if(changed)this.publish({...this.state,kind,after,selected:null,list:{phase:'idle'},detail:{phase:'idle'}});return this.load(this.key(kind),false,()=>this.port.list(kind,after),raw=>this.parseList(kind,raw),refresh);}
 async select(id:string,refresh=false){if(this.state.phase!=='active')return;noticeIdSchema.parse(id);const kind=this.state.kind;if(this.state.selected!==id)this.publish({...this.state,selected:id,detail:{phase:'idle'}});return this.load(this.key(kind,id),true,()=>this.port.detail(kind,id),raw=>this.parseDetail(kind,id,raw),refresh);}
 /** Receipt is the original confirmed outcome; fields absent from it never become an invented current document. */
 acceptReceipt(kind:LiveOpsKind,raw:unknown){
  if(this.state.phase!=='active')return;
  const r=kind==='NOTICE'?noticeReceiptSchema.parse(raw):maintenanceReceiptSchema.parse(raw);
  if(r.environment!==this.environment)throw new Error('확정 응답 환경 불일치');
  const id='noticeId'in r?r.noticeId:r.maintenanceId,revision='revision'in r?r.revision:r.draftRevision,status='status'in r?r.status:'DRAFT_NOT_APPLIED';
  const cached=this.cache.get(this.key(kind,id))?.value as LiveOpsDetail|undefined,previous=this.state.confirmed;
  if(revision<Math.max(this.known(kind,id),cached?.revision??0,previous?.kind===kind&&previous.id===id?previous.revision:0))return;this.remember(kind,id,revision);
  if(!cached||revision>cached.revision)this.invalidate(kind,id);
  this.publish({...this.state,confirmed:{kind,id,revision,status,auditId:r.auditId}});
 }
 closeDetail(){if(this.state.phase==='active')this.publish({...this.state,selected:null,detail:{phase:'idle'}});}
 /** Related server-confirmed change, version discontinuity or ambiguous result. Never automatically sends GET. */
 invalidate(kind:LiveOpsKind,id:string){if(this.state.phase!=='active')return;noticeIdSchema.parse(id);for(const key of this.cache.keys())if(key.startsWith(kind+':list:')||key===this.key(kind,id))this.cache.delete(key);if(this.state.kind===kind)this.publish({...this.state,list:{phase:'idle'},...(this.state.selected===id?{detail:{phase:'idle' as const}}:{})});}
 revoke(){if(this.state.phase!=='active')return;this.cache.clear();this.versions.clear();this.publish({...this.state,phase:'revoked',confirmed:undefined,selected:null,list:{phase:'idle'},detail:{phase:'idle'}});}
 dispose(){this.cache.clear();this.versions.clear();this.listeners.clear();this.state=freeze({...this.state,phase:'disposed',confirmed:undefined,selected:null,list:{phase:'idle'},detail:{phase:'idle'}});}
}



import type {Context,Hono} from 'hono';
import {ZodError} from 'zod';
import {ActivityError} from './activity-policy';
import type {ActivityAuthResult} from './activity-routes';
import {BResultReportStore,BResultBodyError,bResultReportVersion,bResultReportV2Version,bResultBodyLimit,bResultV2BodyLimit} from './combat-b-result-report';
type Ctx=Context<{Bindings:Env}>;
async function readReportBody(c:Ctx,limit:number){
 const reader=c.req.raw.body?.getReader();if(!reader)return '';const chunks:Uint8Array[]=[];let size=0;
 try{for(;;){const part=await reader.read();if(part.done)break;size+=part.value.byteLength;if(size>limit){await reader.cancel();throw new BResultBodyError();}chunks.push(part.value);}}finally{reader.releaseLock();}
 const bytes=new Uint8Array(size);let offset=0;for(const chunk of chunks){bytes.set(chunk,offset);offset+=chunk.byteLength;}
 try{return new TextDecoder('utf-8',{fatal:true,ignoreBOM:true}).decode(bytes);}catch{throw new ActivityError(400,'combat_b_result_request_invalid');}
}
export function registerBResultReportRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<ActivityAuthResult>){
 for(const method of ['POST','GET'] as const){const path='/v1/runs/:runId/b-result-reports'+(method==='GET'?'/:evidenceRef':'');app.on(method,path,async c=>{
 c.header('Cache-Control','no-store');const auth=await authenticate(c);if('errorCode'in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
 try{
 const runId=c.req.param('runId');if(!runId||runId.length>200||new URL(c.req.url).search||c.req.header('If-Match')!==undefined)throw new ActivityError(400,'contract_violation');
 const version=c.req.header('X-B-Result-Version');if(version!==bResultReportVersion&&version!==bResultReportV2Version)throw new ActivityError(409,'combat_b_result_version_conflict');
 let body:unknown;const text=await readReportBody(c,version===bResultReportV2Version?bResultV2BodyLimit:bResultBodyLimit);if(method==='POST'){
 if(c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
 try{body=JSON.parse(text);}catch{throw new ActivityError(400,'combat_b_result_request_invalid');}
 if(!body||typeof body!=='object'||Array.isArray(body)||!('requestId'in body)||c.req.header('Idempotency-Key')!==body.requestId)throw new ActivityError(400,'idempotency_key_conflict');
 if(!('version' in body)||body.version!==version)throw new ActivityError(409,'combat_b_result_version_conflict');
 }else if(text)throw new ActivityError(400,'contract_violation');
 const current=await authenticate(c);if('errorCode'in current)return c.json({accepted:false,errorCode:current.errorCode},current.status);if(current.accountId!==auth.accountId)throw new ActivityError(409,'combat_b_result_auth_changed');
 const store=new BResultReportStore(c.env.DB);if(method==='POST')return c.json(await store.submit(auth.accountId,runId,body));
 const ref=c.req.param('evidenceRef');if(!ref||ref.length>200)throw new ActivityError(400,'contract_violation');const result=await store.read(auth.accountId,runId,ref);if(!result)return c.json({accepted:false,errorCode:'combat_b_result_not_found'},404);return c.json({receipt:result.receipt,report:result.report});
 }catch(e){if(e instanceof BResultBodyError)return c.json({accepted:false,errorCode:e.code},e.status);if(e instanceof ZodError)return c.json({accepted:false,errorCode:'combat_b_result_request_invalid'},400);if(e instanceof ActivityError)return c.json({accepted:false,errorCode:e.code},e.status);throw e;}
 });}
}
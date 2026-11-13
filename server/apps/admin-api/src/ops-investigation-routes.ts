import type {Context,Hono} from 'hono';
import type {RuntimePolicyPrincipal} from './runtime-policy';
import {OpsInvestigationStore,OpsInvestigationError,canWriteOps,opsCommand,opsWritePermission} from './ops-investigation';

type Ctx=Context<{Bindings:Env}>;
export function registerOpsInvestigationRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<RuntimePolicyPrincipal|Response>){
 const handler=(write:boolean)=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  const actor=await authenticate(c);if(actor instanceof Response)return actor;
  // Explicit WRITE policy; it happens to have the same approved roles as READ.
  const allowed=write?canWriteOps(actor):actor.role==='OWNER'||actor.role==='OPERATOR';
  if(!allowed)return c.json({error:'ADMIN_PERMISSION_DENIED'},403);
  try{
   if(new URL(c.req.url).searchParams.size)throw new OpsInvestigationError(400,'contract_violation');
   const id=c.req.param('errorLogId')??'';if(!/^[A-Za-z0-9_-]{1,128}$/u.test(id))throw new OpsInvestigationError(400,'contract_violation');
   const store=new OpsInvestigationStore(c.env.DB);
   if(!write){if(c.req.header('Content-Length')&&c.req.header('Content-Length')!=='0')throw new OpsInvestigationError(400,'contract_violation');return c.json(await store.detail(id));}
   const key=c.req.header('Idempotency-Key')??'';if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new OpsInvestigationError(400,'idempotency_key_required');
   const reader=c.req.raw.body?.getReader();let size=0,raw='';const decoder=new TextDecoder();
   if(reader){try{while(true){const chunk=await reader.read();if(chunk.done)break;size+=chunk.value.byteLength;if(size>8192){await reader.cancel();throw new OpsInvestigationError(400,'contract_violation');}raw+=decoder.decode(chunk.value,{stream:true});}raw+=decoder.decode();}finally{reader.releaseLock();}}
   let json:unknown;try{json=JSON.parse(raw);}catch{throw new OpsInvestigationError(400,'contract_violation');}
   const parsed=opsCommand.safeParse(json);if(!parsed.success)throw new OpsInvestigationError(400,'contract_violation');
   const result=await store.command(actor,id,parsed.data,key);
   return c.json({...result,permission:opsWritePermission});
  }catch(error){
   if(error instanceof OpsInvestigationError)return c.json({error:error.code,...(error.current?{current:error.current}:{})},error.status);
   return c.json({error:'ops_investigation_unavailable'},503);
  }
 };
 app.get('/v1/admin/ops/error-logs/:errorLogId/investigation',handler(false));
 app.post('/v1/admin/ops/error-logs/:errorLogId/investigation',handler(true));
}

import {readAdDisclosureReadiness} from './ad-disclosure-readiness';
import type {Context,Hono} from 'hono';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {readAdminRuntimePolicy,saveAdminRuntimePolicy,runtimePolicyWriteSchema,RuntimePolicyError,
 runtimePolicyReadPermission,runtimePolicyWritePermission,type RuntimePolicyPrincipal} from './runtime-policy';
type Ctx=Context<{Bindings:Env}>;
export function registerAdminRuntimePolicyRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<RuntimePolicyPrincipal|Response>,
 permitted:(principal:RuntimePolicyPrincipal,permission:string)=>boolean) {
 const handler=(write:boolean,readiness=false)=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  const principal=await authenticate(c);if(principal instanceof Response)return principal;
  if(!permitted(principal,write?runtimePolicyWritePermission:runtimePolicyReadPermission))return c.json({error:'ADMIN_PERMISSION_DENIED'},403);
  try {
   if(c.env.ENVIRONMENT!=='DEV'&&c.env.ENVIRONMENT!=='PRD')throw new RuntimePolicyError(400,'runtime_policy_environment_mismatch');
   if(new URL(c.req.url).search)throw new RuntimePolicyError(400,'contract_violation');
   const raw=await readBoundedBody(c);
   if(!write){if(raw!=='')throw new RuntimePolicyError(400,'contract_violation');return c.json(readiness?await readAdDisclosureReadiness(c.env.DB,c.env.ENVIRONMENT):await readAdminRuntimePolicy(c.env.DB,c.env.ENVIRONMENT,principal));}
   const key=c.req.header('Idempotency-Key')??'';
   if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new RuntimePolicyError(400,'idempotency_key_required');
   let body:unknown;try{body=JSON.parse(raw);}catch{throw new RuntimePolicyError(400,'contract_violation');}
   const parsed=runtimePolicyWriteSchema.safeParse(body);if(!parsed.success)throw new RuntimePolicyError(400,'contract_violation');
   return c.json(await saveAdminRuntimePolicy(c.env.DB,c.env.ENVIRONMENT,principal,parsed.data,key));
  }catch(error){if(error instanceof RuntimePolicyError || error instanceof ActivityError)return c.json({error:error.code},error.status);
   return c.json({error:'runtime_policy_unavailable'},503);}
 };
 app.get('/v1/admin/runtime-policy/disclosure-readiness',handler(false,true));
 app.get('/v1/admin/runtime-policy',handler(false));app.post('/v1/admin/runtime-policy',handler(true));
}

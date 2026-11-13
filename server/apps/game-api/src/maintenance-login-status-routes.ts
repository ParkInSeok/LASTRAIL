import type {Hono} from 'hono';
import {z} from 'zod';
import {maintenanceDocumentSchema} from '../../../packages/common/src/maintenance-plan-policy';
import {maintenanceContext,maintenanceLoginReadStatement} from './maintenance-admission';
import {ActivityError} from './activity-policy';
import {readBoundedBody} from './activity-routes';
const path='/v1/public/maintenance/login';
const decision=z.enum(['OK','BLOCKED','UNAVAILABLE']);
const snapshot=z.object({game_code:decision,web_code:decision,revision:z.number().int().positive().safe().nullable(),
 document_json:z.string().nullable(),estimated_end_at:z.number().int().nonnegative().safe().nullable(),checked_at:z.number().int().nonnegative().safe()});
export function isMaintenanceLoginStatusRoute(method:string,urlPath:string){return urlPath===path&&['GET','OPTIONS'].includes(method);}
export function registerMaintenanceLoginStatusRoutes(app:Hono<{Bindings:Env}>){
 app.options(path,c=>{c.header('Cache-Control','no-store');c.header('Access-Control-Allow-Origin','*');c.header('Access-Control-Allow-Methods','GET, OPTIONS');return c.body(null,204);});
 app.get(path,async c=>{
  c.header('Cache-Control','no-store');c.header('Access-Control-Allow-Origin','*');
  try{
   if(new URL(c.req.url).searchParams.size||(await readBoundedBody(c))!=='')throw new ActivityError(400,'contract_violation');
   const raw=await maintenanceLoginReadStatement(c.env.DB,maintenanceContext(c.env.ENVIRONMENT)).first();
   const row=snapshot.parse(raw);
   if(row.game_code==='UNAVAILABLE'||row.web_code==='UNAVAILABLE'||row.revision===null)throw new ActivityError(503,'maintenance_policy_unavailable');
   const document=row.document_json===null?null:maintenanceDocumentSchema.parse(JSON.parse(row.document_json));
   return c.json({contractVersion:'maintenance-login-v1',serverTime:row.checked_at,policyRevision:row.revision,
    gameLoginAllowed:row.game_code==='OK',webLoginAllowed:row.web_code==='OK',
    title:document?.title??null,summary:document?.summary??null,startsAt:document?.startsAt??null,
    estimatedEndAt:row.estimated_end_at,retryAfterSeconds:document?.retryAfterSeconds??null});
  }catch(error){
   if(error instanceof ActivityError)return c.json({error:error.code,confirmed:false},error.status);
   return c.json({error:'maintenance_policy_unavailable',confirmed:false},503);
  }
 });
}

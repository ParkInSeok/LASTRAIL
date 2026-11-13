import type {Context,Hono} from 'hono';
import {ActivityError} from '../../game-api/src/activity-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {previewEventDefinition} from './admin-event-definition';
import {previewEventOutage} from './admin-event-outage';
import {captureEventCosmeticReplacement} from '../../game-api/src/event-cosmetic-replacement';
type Ctx=Context<{Bindings:Env}>;
type Principal={id:string;role:'OWNER'|'OPERATOR'|'VIEWER'};
/** Existing SUPPORT_READ for policy reads/previews; no event mutations. */
export function registerAdminEventDefinitionRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<Principal|Response>){
 const handle=(project:typeof previewEventDefinition|typeof previewEventOutage|'replacement-table')=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  const actor=await authenticate(c);if(actor instanceof Response)return actor;
  try{
   const allowed=await c.env.DB.prepare("SELECT 1 FROM admin_principals WHERE id=? AND status='ACTIVE' AND role IN ('OWNER','OPERATOR','VIEWER')").bind(actor.id).first();
   if(!allowed)throw new ActivityError(403,'ADMIN_PERMISSION_DENIED');
   if(new URL(c.req.url).search||c.req.header('If-Match')!==undefined)throw new ActivityError(400,'contract_violation');
   const text=await readBoundedBody(c);
   if(project==='replacement-table'){
    if(text!=='')throw new ActivityError(400,'contract_violation');
    if(c.env.ENVIRONMENT!=='DEV'&&c.env.ENVIRONMENT!=='PRD')throw new ActivityError(503,'event_environment_unavailable');
    return c.json({contractVersion:'admin.event-cosmetic-replacement-table.v1.20261020',permission:'SUPPORT_READ',
     environment:c.env.ENVIRONMENT,definition:await captureEventCosmeticReplacement(),
     sourceAuthorityVerified:false,executeSupported:false,publicationSupported:false,persisted:false,
     sourceReadiness:{replacementDefinition:'APPROVED',growthCurrencies:'EXISTING_DIRECT_CURRENCY_UNITS',
      permanentCosmeticOwner:'NOT_CONNECTED',eventFirstGrantOwner:'NOT_CONNECTED',
      eventFreeCrystalSourceAndActiveWalletBinding:'NOT_CONNECTED',originalLineReceiptRecovery:'NOT_CONNECTED'}});
   }
   if(c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
   let input:unknown;
   try{input=JSON.parse(text);}catch{throw new ActivityError(400,'contract_violation');}
   return c.json(await project(c.env.ENVIRONMENT,input));
  }catch(error){
   if(error instanceof ActivityError)return c.json({error:error.code},error.status);
   return c.json({error:'event_definition_preview_unavailable'},503);
  }
 };
 app.post('/v1/admin/events/definition-preview',handle(previewEventDefinition));
 app.post('/v1/admin/events/outage-preview',handle(previewEventOutage));
 app.get('/v1/admin/events/cosmetic-replacement-table',handle('replacement-table'));
}

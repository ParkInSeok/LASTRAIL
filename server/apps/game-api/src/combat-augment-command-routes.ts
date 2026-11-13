import type {Context,Hono} from 'hono';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {SoloAugmentStore} from './solo-augment-store';
import {soloSelectSchema,soloRerollSchema} from './solo-augment-selection';
import {AugmentNodeSelectionGuard} from './augment-node-selection-guard';
import {captureAugmentTargetReadout} from './augment-target-readout';
export const augmentSelectVersion='combat.augment-select.v1.20261019';
export const augmentRerollVersion='combat.augment-reroll.v1.20261021';
const body=z.strictObject({version:z.literal(augmentSelectVersion),command:soloSelectSchema.strict()});
const rerollBody=z.strictObject({version:z.literal(augmentRerollVersion),command:soloRerollSchema});
const key=z.string().regex(/^[\x21-\x7e]{1,128}$/u);
/** Public intent only. Candidate issuance, effect application and node completion
 * remain separate authoritative owners; no client grant/profile/proof is accepted. */
export function registerAugmentCommandRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){
 for(const operation of ['SELECT','REROLL'] as const)app.post('/v1/runs/:runId/augment-grants/:eventId/'+(operation==='SELECT'?'select':'reroll'),async c=>{
  c.header('Cache-Control','no-store');const auth=await authenticate(c);
  if('errorCode' in auth)return c.json({accepted:false,errorCode:auth.errorCode},auth.status);
  try{
   const runId=c.req.param('runId'),eventId=c.req.param('eventId'),requestId=key.safeParse(c.req.header('Idempotency-Key'));
   if(!runId||runId.length>200||!eventId||eventId.length>200||!requestId.success||new URL(c.req.url).search||c.req.header('Content-Type')?.split(';')[0]?.trim().toLowerCase()!=='application/json')throw new ActivityError(400,'contract_violation');
   let input:unknown;try{input=JSON.parse(await readBoundedBody(c));}catch{throw new ActivityError(400,'contract_violation');}
   const parsed=(operation==='SELECT'?body:rerollBody).safeParse(input);if(!parsed.success||parsed.data.command.eventId!==eventId)throw new ActivityError(400,'contract_violation');
   // Extra approval remains with the existing SSV/purchase owners. This route
   // never accepts a caller assertion that an ad or purchase was approved.
   if(operation==='REROLL'&&(!('mode' in parsed.data.command)||parsed.data.command.mode!=='FREE'))throw new ActivityError(409,'augment_extra_approval_route_required');
   const result=await new SoloAugmentStore(c.env.DB,undefined,'ORDINARY_SOLO',new AugmentNodeSelectionGuard(c.env.DB))
    .execute({accountId:auth.accountId,environment:c.env.ENVIRONMENT},runId,eventId,requestId.data,{operation,command:parsed.data.command});
   return operation==='SELECT'?c.json({version:augmentSelectVersion,committed:true,result}):c.json({version:augmentRerollVersion,committed:true,result,targetReadout:await captureAugmentTargetReadout(result.snapshot)});
  }catch(error){
   if(error instanceof ActivityError)return c.json({accepted:false,errorCode:error.code},error.status);
   if(error instanceof Error&&['selection_closed','revision_conflict','candidate_mismatch','request_conflict','free_exhausted'].includes(error.message))return c.json({accepted:false,errorCode:'augment_selection_'+error.message},409);
   throw error;
  }
 });
}

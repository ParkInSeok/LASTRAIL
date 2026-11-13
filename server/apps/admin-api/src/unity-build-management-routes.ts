import type {Context,Hono} from 'hono';
import {z} from 'zod';
import type {RuntimePolicyPrincipal} from './runtime-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {UnityBuildManagement,UnityBuildError,unityBuildReadPermission,unityBuildWritePermission,type UnityBuildProfile} from './unity-build-management';
type Ctx=Context<{Bindings:Env}>;
export function registerUnityBuildManagementRoutes(app:Hono<{Bindings:Env}>,
 authenticate:(c:Ctx)=>Promise<RuntimePolicyPrincipal|Response>,
 hasPermission:(actor:RuntimePolicyPrincipal,permission:string)=>boolean,
 profiles:readonly UnityBuildProfile[]=[]){
 const handler=(mode:'capabilities'|'list'|'detail'|'request'|'cancel'|'logs'|'artifacts'|'download')=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');
  const actor=await authenticate(c);if(actor instanceof Response)return actor;
  const write=mode==='request'||mode==='cancel';
  if(!hasPermission(actor,write?unityBuildWritePermission:unityBuildReadPermission))return c.json({error:'admin_permission_denied'},403);
  try{
   if(c.env.ENVIRONMENT!=='DEV'&&c.env.ENVIRONMENT!=='PRD')throw new UnityBuildError(503,'unity_build_environment_unavailable');
   const service=new UnityBuildManagement(c.env.DB,c.env.ENVIRONMENT,profiles),params=new URL(c.req.url).searchParams;
   const allowed=mode==='list'?['after','limit']:mode==='logs'?['after']:[];
   for(const name of params.keys())if(!allowed.includes(name)||params.getAll(name).length!==1)throw new UnityBuildError(400,'contract_violation');
   const raw=await readBoundedBody(c),jobId=c.req.param('jobId')??'';
   if(write){
    if(c.req.header('If-Match')!==undefined||c.req.header('Content-Type')?.split(';')[0]?.trim()!=='application/json')throw new UnityBuildError(400,'contract_violation');
    let body:unknown;try{body=JSON.parse(raw);}catch{throw new UnityBuildError(400,'contract_violation');}
    const key=c.req.header('Idempotency-Key')??'';
    return c.json(mode==='request'?await service.request(actor,body,key):await service.cancel(actor,jobId,body,key));
   }
   if(raw)throw new UnityBuildError(400,'contract_violation');
   if(mode==='capabilities')return c.json({...service.capabilities(),canRequest:hasPermission(actor,unityBuildWritePermission)&&profiles.some(p=>p.environment===c.env.ENVIRONMENT)});
   if(mode==='detail')return c.json(await service.read(jobId));
   if(mode==='logs'){
    const after=params.get('after')??'0';if(!/^(0|[1-9][0-9]*)$/u.test(after))throw new UnityBuildError(400,'contract_violation');
    return c.json(await service.logs(jobId,Number(after)));
   }
   if(mode==='artifacts')return c.json(await service.artifacts(jobId));
   if(mode==='download')return await service.download(jobId,c.req.param('artifactId')??'');
   const limit=params.get('limit')??'20';if(!/^[1-9][0-9]*$/u.test(limit))throw new UnityBuildError(400,'contract_violation');
   return c.json(await service.list(params.get('after')??'',Number(limit)));
  }catch(error){
   if(error instanceof UnityBuildError||error instanceof ActivityError)return c.json({error:error.code},error.status);
   if(error instanceof z.ZodError)return c.json({error:'contract_violation'},400);
   return c.json({error:'unity_build_management_unavailable'},503);
  }
 };
 app.get('/v1/admin/unity-builds/capabilities',handler('capabilities'));
 app.get('/v1/admin/unity-builds',handler('list'));
 app.post('/v1/admin/unity-builds',handler('request'));
 app.get('/v1/admin/unity-builds/:jobId',handler('detail'));
 app.post('/v1/admin/unity-builds/:jobId/cancel',handler('cancel'));
 app.get('/v1/admin/unity-builds/:jobId/logs',handler('logs'));
 app.get('/v1/admin/unity-builds/:jobId/artifacts',handler('artifacts'));
 app.get('/v1/admin/unity-builds/:jobId/artifacts/:artifactId/download',handler('download'));
}

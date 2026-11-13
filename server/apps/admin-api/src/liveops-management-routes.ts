import type {Context,Hono} from 'hono';
import {NoticeError,noticeEnvironment,noticeIdSchema,type NoticePrincipal} from '../../../packages/common/src/notice-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {NoticeStore} from '../../game-api/src/notice-store';
import {LiveopsManagement,liveopsCapabilities,liveopsCommand,liveopsVersion} from './liveops-management';
type Ctx=Context<{Bindings:Env}>;
export function registerLiveopsManagementRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<NoticePrincipal|Response>){
 const handler=(mode:'capabilities'|'list'|'detail'|'history'|'command')=>async(c:Ctx)=>{
  c.header('Cache-Control','no-store');const actor=await authenticate(c);if(actor instanceof Response)return actor;
  try{
   const environment=noticeEnvironment.safeParse(c.env.ENVIRONMENT);if(!environment.success)throw new NoticeError(503,'liveops_environment_unavailable');
   const id=c.req.param('noticeId'),params=new URL(c.req.url).searchParams;
   if(['detail','history','command'].includes(mode)&&!noticeIdSchema.safeParse(id).success)throw new NoticeError(400,'contract_violation');
   const raw=await readBoundedBody(c),service=new LiveopsManagement(c.env.DB,environment.data),store=new NoticeStore(c.env.DB,environment.data);
   if(mode==='command'){
    if(params.size||c.req.header('If-Match')!==undefined||c.req.header('Content-Type')?.split(';')[0]?.trim()!=='application/json')throw new NoticeError(400,'contract_violation');
    const key=c.req.header('Idempotency-Key')??'';if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new NoticeError(400,'idempotency_key_required');
    let input:unknown;try{input=JSON.parse(raw);}catch{throw new NoticeError(400,'contract_violation');}
    const parsed=liveopsCommand.safeParse(input);if(!parsed.success)throw new NoticeError(400,'contract_violation');
    return c.json(await service.command(actor,id!,parsed.data,key));
   }
   if(raw!=='')throw new NoticeError(400,'contract_violation');
   const allowed=mode==='list'?['after','limit']:mode==='history'?['before','limit']:[];
   for(const key of params.keys())if(!allowed.includes(key)||params.getAll(key).length!==1)throw new NoticeError(400,'contract_violation');
   if(mode==='capabilities')return c.json(liveopsCapabilities(actor));
   if(mode==='detail'){
    const notice=await store.read(id!);if(!notice)throw new NoticeError(404,'notice_not_found');
    const review=await c.env.DB.prepare('SELECT response_json FROM liveops_notice_reviews WHERE environment=? AND notice_id=? AND revision=? ORDER BY created_at DESC,review_id DESC LIMIT 1').bind(environment.data,id,notice.revision).first<{response_json:string}>();
    return c.json({contractVersion:liveopsVersion,environment:environment.data,notice,publication:await store.read(id!,true),review:review?JSON.parse(review.response_json):null});
   }
   const limit=params.get('limit')??'20';if(!/^[1-9][0-9]*$/u.test(limit)||Number(limit)>50)throw new NoticeError(400,'contract_violation');
   if(mode==='history'){
    const before=params.get('before')??String(Number.MAX_SAFE_INTEGER);if(!/^[1-9][0-9]*$/u.test(before)||!Number.isSafeInteger(Number(before)))throw new NoticeError(400,'contract_violation');
    return c.json(await service.history(id!,Number(before),Number(limit)));
   }
   const after=params.get('after')??'';if(after!==''&&!noticeIdSchema.safeParse(after).success)throw new NoticeError(400,'contract_violation');
   const rows=await store.adminList(after,Number(limit)+1),items=rows.slice(0,Number(limit));
   return c.json({contractVersion:liveopsVersion,environment:environment.data,items,nextAfter:rows.length>Number(limit)?items.at(-1)!.noticeId:null});
  }catch(error){
   if(error instanceof NoticeError||error instanceof ActivityError)return c.json({error:error.code},error.status);
   return c.json({error:'liveops_management_unavailable'},503);
  }
 };
 app.get('/v1/admin/liveops/capabilities',handler('capabilities'));
 app.get('/v1/admin/liveops/notices',handler('list'));
 app.get('/v1/admin/liveops/notices/:noticeId',handler('detail'));
 app.get('/v1/admin/liveops/notices/:noticeId/history',handler('history'));
 app.post('/v1/admin/liveops/notices/:noticeId/commands',handler('command'));
}

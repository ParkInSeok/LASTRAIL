import type {Context,Hono} from 'hono';
import {NoticeError,noticeCommandSchema,noticeEnvironment,noticeIdSchema,hasNoticePermission,commandPermission,effectiveNoticeStatus,type NoticePrincipal} from '../../../packages/common/src/notice-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {NoticeStore} from '../../game-api/src/notice-store';
import {saveNoticeCommand} from './notice-service';

type Ctx=Context<{Bindings:Env}>;
export function registerNoticeAdminRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Ctx)=>Promise<NoticePrincipal|Response>) {
  const handle=(mode:'list'|'detail'|'command')=>async(c:Ctx)=>{
    c.header('Cache-Control','no-store');
    const actor=await authenticate(c);if(actor instanceof Response)return actor;
    try{
      const environment=noticeEnvironment.parse(c.env.ENVIRONMENT);
      const id=c.req.param('noticeId');
      if(mode!=='list'&&!noticeIdSchema.safeParse(id).success)throw new NoticeError(400,'contract_violation');
      const params=new URL(c.req.url).searchParams;
      const raw=await readBoundedBody(c);
      if(mode==='command'){
        if(params.size)throw new NoticeError(400,'contract_violation');
        const key=c.req.header('Idempotency-Key')??'';
        if(!/^[\x21-\x7e]{1,128}$/u.test(key))throw new NoticeError(400,'idempotency_key_required');
        let body:unknown;try{body=JSON.parse(raw);}catch{throw new NoticeError(400,'contract_violation');}
        const parsed=noticeCommandSchema.safeParse(body);if(!parsed.success)throw new NoticeError(400,'contract_violation');
        if(!hasNoticePermission(actor,commandPermission(parsed.data)))throw new NoticeError(403,'ADMIN_PERMISSION_DENIED');
        return c.json(await saveNoticeCommand(c.env.DB,environment,actor,id!,parsed.data,key));
      }
      if(raw!=='')throw new NoticeError(400,'contract_violation');
      const permissions={canEdit:hasNoticePermission(actor,'NOTICE_EDIT'),canPublish:hasNoticePermission(actor,'NOTICE_PUBLISH'),canWithdraw:hasNoticePermission(actor,'NOTICE_WITHDRAW')};
      const store=new NoticeStore(c.env.DB,environment),serverTime=Date.now();
      if(mode==='detail'){
        if(params.size)throw new NoticeError(400,'contract_violation');
        const notice=await store.read(id!);if(!notice)throw new NoticeError(404,'notice_not_found');
        const publication=await store.read(id!,true);
        return c.json({contractVersion:'admin-notice-v1',environment,serverTime,permissions,notice:{...notice,effectiveStatus:effectiveNoticeStatus(notice,serverTime)},publication});
      }
      for(const key of params.keys())if(!['after','limit'].includes(key)||params.getAll(key).length!==1)throw new NoticeError(400,'contract_violation');
      const after=params.get('after')??'',limit=params.get('limit')??'20';
      if((after!==''&&!noticeIdSchema.safeParse(after).success)||!/^[1-9][0-9]*$/u.test(limit)||Number(limit)>50)throw new NoticeError(400,'contract_violation');
      const rows=await store.adminList(after,Number(limit)+1),items=rows.slice(0,Number(limit));
      return c.json({contractVersion:'admin-notice-list-v1',environment,serverTime,permissions,items,
        nextAfter:rows.length>Number(limit)?items.at(-1)!.noticeId:null});
    }catch(error){
      if(error instanceof NoticeError||error instanceof ActivityError)return c.json({error:error.code},error.status);
      return c.json({error:'notice_unavailable'},503);
    }
  };
  app.get('/v1/admin/notices',handle('list'));
  app.get('/v1/admin/notices/:noticeId',handle('detail'));
  app.post('/v1/admin/notices/:noticeId/commands',handle('command'));
}

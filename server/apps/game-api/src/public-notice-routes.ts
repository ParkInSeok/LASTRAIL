import {publicReadAccess} from './public-read-access';
import type {Context,Hono} from 'hono';
import {NoticeError,noticeEnvironment,noticeIdSchema,parseNoticeQuery,noticeCursor,publicNotice} from '../../../packages/common/src/notice-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {ActivityError} from './activity-policy';
import {NoticeStore} from './notice-store';

// Only these read methods bypass game build admission. The DEV tester gate below still applies.
export function isPublicNoticeRead(method:string,path:string) {
  return method==='GET'&&/^\/v1\/public\/notices(?:\/[A-Za-z0-9][A-Za-z0-9_-]{0,79})?$/u.test(path);
}
export function registerPublicNoticeRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>) {
  const handle=(list:boolean)=>async(c:Context<{Bindings:Env}>)=>{
    c.header('Cache-Control','no-store');
    try {
      const environment=noticeEnvironment.safeParse(c.env.ENVIRONMENT);
      if(!environment.success)throw new NoticeError(503,'notice_environment_unavailable');
      const denied=await publicReadAccess(c,environment.data,authenticate);if(denied)return denied;
      if((await readBoundedBody(c))!=='')throw new NoticeError(400,'contract_violation');
      const query=parseNoticeQuery(new URL(c.req.url).searchParams,environment.data,list),serverTime=Date.now();
      const store=new NoticeStore(c.env.DB,environment.data);
      if(!list){
        const id=c.req.param('noticeId');if(!noticeIdSchema.safeParse(id).success)throw new NoticeError(400,'contract_violation');
        const stored=await store.read(id!,true),notice=stored?publicNotice(stored,query,serverTime):null;
        if(!notice)throw new NoticeError(404,'notice_not_found');
        return c.json({contractVersion:'public-notice-v1',environment:environment.data,serverTime,notice});
      }
      const rows=await store.publicList(query,serverTime);
      const projected=rows.map(row=>publicNotice(row,query,serverTime));
      if(projected.some(item=>item===null))throw new NoticeError(503,'notice_storage_invalid');
      const items=projected.slice(0,query.limit);
      return c.json({contractVersion:'public-notice-list-v1',environment:environment.data,serverTime,items,
        nextCursor:rows.length>query.limit?noticeCursor(query,rows[query.limit-1]!.noticeId):null});
    }catch(error){
      if(error instanceof NoticeError||error instanceof ActivityError)return c.json({error:error.code},error.status);
      return c.json({error:'notice_unavailable'},503);
    }
  };
  app.get('/v1/public/notices',handle(true));
  app.get('/v1/public/notices/:noticeId',handle(false));
}

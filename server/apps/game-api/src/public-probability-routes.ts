import type {Context,Hono} from 'hono';
import {ProbabilityDisclosureError as Failure,disclosureEnvironment,disclosureIdSchema,parseProbabilityHistoryQuery,probabilityHistoryCursor} from '../../../packages/common/src/probability-disclosure-policy';
import {readBoundedBody,type ActivityAuthResult} from './activity-routes';
import {ActivityError} from './activity-policy';
import {publicReadAccess} from './public-read-access';
import {readProbabilityDisclosure,listProbabilityDisclosures} from './probability-disclosure-store';
export function isPublicProbabilityRead(method:string,path:string){
  return method==='GET'&&/^\/v1\/public\/probability-disclosures(?:\/pd_[a-f0-9]{64})?$/u.test(path);
}
export function registerPublicProbabilityRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<ActivityAuthResult>){
  const handle=(list:boolean)=>async(c:Context<{Bindings:Env}>)=>{
    c.header('Cache-Control','no-store');
    try{
      const environment=disclosureEnvironment.safeParse(c.env.ENVIRONMENT);
      if(!environment.success)throw new Failure(503,'probability_environment_unavailable');
      const denied=await publicReadAccess(c,environment.data,authenticate);if(denied)return denied;
      if((await readBoundedBody(c))!=='')throw new Failure(400,'contract_violation');
      const params=new URL(c.req.url).searchParams;
      if(list){
        const query=parseProbabilityHistoryQuery(params,environment.data);
        const rows=await listProbabilityDisclosures(c.env.DB,environment.data,query.after,query.limit+1);
        const items=rows.slice(0,query.limit);
        return c.json({contractVersion:'public-probability-list-v1',scope:'PUBLISHED_HISTORY',environment:environment.data,
          serverTime:Date.now(),items,nextCursor:rows.length>query.limit?probabilityHistoryCursor(environment.data,items.at(-1)!.disclosureId):null});
      }
      const id=c.req.param('disclosureId');
      if(!disclosureIdSchema.safeParse(id).success||params.size)throw new Failure(400,'contract_violation');
      const payload=await readProbabilityDisclosure(c.env.DB,environment.data,id!);
      if(!payload)throw new Failure(404,'probability_disclosure_not_found');
      return c.json(payload);
    }catch(error){
      if(error instanceof Failure||error instanceof ActivityError)return c.json({error:error.code},error.status);
      return c.json({error:'probability_disclosure_unavailable'},503);
    }
  };
  app.get('/v1/public/probability-disclosures',handle(true));
  app.get('/v1/public/probability-disclosures/:disclosureId',handle(false));
}

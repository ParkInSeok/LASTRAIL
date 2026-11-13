import {readProbabilityDisclosure} from '../../game-api/src/probability-disclosure-store';
import type {Context,Hono} from 'hono';
import {ProbabilityDisclosureError as Failure,disclosureEnvironment,disclosureConsumer,disclosureSourceKind,policyVersionSchema,publishDisclosureSchema,type DisclosurePrincipal} from '../../../packages/common/src/probability-disclosure-policy';
import {readBoundedBody} from '../../game-api/src/activity-routes';
import {ActivityError} from '../../game-api/src/activity-policy';
import {readDisclosureSource} from '../../game-api/src/probability-disclosure-source';
import {publishProbabilityDisclosure} from './probability-disclosure-service';
export function registerProbabilityDisclosureAdminRoutes(app:Hono<{Bindings:Env}>,authenticate:(c:Context<{Bindings:Env}>)=>Promise<DisclosurePrincipal|Response>){
  const handler=(publish:boolean)=>async(c:Context<{Bindings:Env}>)=>{
    c.header('Cache-Control','no-store');
    const actor=await authenticate(c);if(actor instanceof Response)return actor;
    try{
      const environment=disclosureEnvironment.safeParse(c.env.ENVIRONMENT);
      if(!environment.success)throw new Failure(503,'probability_environment_unavailable');
      const params=new URL(c.req.url).searchParams,raw=await readBoundedBody(c);
      if(publish){
        if(actor.role!=='OWNER')throw new Failure(403,'ADMIN_PERMISSION_DENIED');
        const key=c.req.header('Idempotency-Key')??'';
        if(!/^[\x21-\x7e]{1,128}$/u.test(key)||params.size)throw new Failure(400,'contract_violation');
        let body:unknown;try{body=JSON.parse(raw);}catch{throw new Failure(400,'contract_violation');}
        const parsed=publishDisclosureSchema.safeParse(body);if(!parsed.success)throw new Failure(400,'contract_violation');
        return c.json(await publishProbabilityDisclosure(c.env.DB,environment.data,actor,parsed.data,key));
      }
      if(raw!=='')throw new Failure(400,'contract_violation');
      for(const key of params.keys())if(!['consumer','policyVersion','sourceKind'].includes(key)||params.getAll(key).length!==1)throw new Failure(400,'contract_violation');
      const consumer=disclosureConsumer.safeParse(params.get('consumer')),version=policyVersionSchema.safeParse(params.get('policyVersion'));
      const sourceKind=disclosureSourceKind.safeParse(params.get('sourceKind')??'AD_POLICY');
      if(!consumer.success||!version.success||!sourceKind.success)throw new Failure(400,'contract_violation');
      const source=await readDisclosureSource(c.env.DB,environment.data,consumer.data,version.data,sourceKind.data);
      const published=await readProbabilityDisclosure(c.env.DB,environment.data,source.disclosureId);
      return c.json({...source.projection,contractVersion:'admin-probability-source-v1',disclosureId:source.disclosureId,
        sourceStatus:source.status,publicationStatus:published?'PUBLISHED':'UNPUBLISHED',canPublish:actor.role==='OWNER'&&source.publishable&&!published});
    }catch(error){
      if(error instanceof Failure||error instanceof ActivityError)return c.json({error:error.code},error.status);
      return c.json({error:'probability_disclosure_unavailable'},503);
    }
  };
  app.get('/v1/admin/probability-disclosures/source',handler(false));
  app.post('/v1/admin/probability-disclosures',handler(true));
}

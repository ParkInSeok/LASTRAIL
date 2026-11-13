import {describe,it,expect,vi} from 'vitest';
import game from '../../apps/game-api/src/dev-deployment';
import admin from '../../apps/admin-api/src/dev-deployment';
import worker from '../../apps/async-worker/src/dev-deployment';
import {createDevDeploymentConfig} from '../../scripts/cloudflare/dev-deployment-config';
const ctx={waitUntil:vi.fn(),passThroughOnException:vi.fn()};
const env=(state:unknown,environment='DEV')=>new Proxy({ENVIRONMENT:environment,DEPLOYMENT_STATE:state},{get(target,key){if(key in target)return Reflect.get(target,key);throw Error('Unexpected binding access: '+String(key));}});
describe('DEV deployment entry gate',()=>{
 for(const [name,app] of [['game',game],['admin',admin]] as const){
  it.each([undefined,'','paused','PAUSED','BROKEN'])(name+' blocks %s before bindings',async(state)=>{
   const response=await app.fetch(new Request('https://test/v1/admob/ssv'),env(state) as never,ctx as never);
   expect(response.status).toBe(503);expect(response.headers.get('Cache-Control')).toBe('no-store');
  });
  it(name+' cannot open PRD with RUNNING',async()=>{expect((await app.fetch(new Request('https://test/'),env('RUNNING','PRD') as never,ctx as never)).status).toBe(503);});
 }
 it('does not start cron work while paused',()=>{expect(()=>worker.scheduled({} as never,env('PAUSED') as never,ctx as never)).not.toThrow();expect(ctx.waitUntil).not.toHaveBeenCalled();});
 it('unexpected paused queue delivery fails without acknowledging',()=>{const ack=vi.fn();expect(()=>worker.queue({ackAll:ack} as never,env('PAUSED') as never,ctx as never)).toThrow('deployment_queue_delivery_not_paused');expect(ack).not.toHaveBeenCalled();});
 it('new instance with the same paused state stays blocked',async()=>{for(let i=0;i<2;i++)expect((await game.fetch(new Request('https://test/status'),env('PAUSED') as never,ctx as never)).status).toBe(503);});
});
describe('DEV config preparation',()=>{
 const config={main:'src/index.ts',env:{dev:{name:'lastrail-game-api-dev',vars:{ENVIRONMENT:'DEV',CLIENT_ADMISSION_MODE:'ENFORCED'}},prd:{name:'prd'}}};
 const evidence={environment:'DEV',queueName:'lastrail-admin-jobs-dev',deliveryPaused:true,checkedAt:1000};
 it('preserves dev bindings and creates an explicit paused entry',()=>{const result=createDevDeploymentConfig(config,'game-api','PAUSED',evidence,1001);expect(result.main).toBe('src/dev-deployment.ts');expect(result.env.dev.vars).toEqual({ENVIRONMENT:'DEV',CLIENT_ADMISSION_MODE:'ENFORCED',DEPLOYMENT_STATE:'PAUSED'});expect(config.main).toBe('src/index.ts');expect(result.env).not.toHaveProperty('prd');});
 it.each([undefined,'','BROKEN'])('rejects missing or invalid state %s',(state)=>{expect(()=>createDevDeploymentConfig(config,'game-api',state,evidence,1001)).toThrow();});
 it.each([{...evidence,deliveryPaused:false},{...evidence,queueName:'prd'},{...evidence,environment:'PRD'},{...evidence,checkedAt:2000}])('rejects invalid queue observation',(ev)=>{expect(()=>createDevDeploymentConfig(config,'game-api','PAUSED',ev,1001)).toThrow();});
 it('rejects stale evidence',()=>expect(()=>createDevDeploymentConfig(config,'game-api','PAUSED',evidence,400001)).toThrow());
 it('supports explicit RUNNING preparation while queue delivery is still paused',()=>expect(createDevDeploymentConfig(config,'game-api','RUNNING',evidence,1001).env.dev.vars.DEPLOYMENT_STATE).toBe('RUNNING'));
});

it('RUNNING delegates existing HTTP handlers exactly once',async()=>{
 const gameOriginal=(await import('../../apps/game-api/src/index')).default;
 const adminOriginal=(await import('../../apps/admin-api/src/index')).default;
 for(const [guard,original] of [[game,gameOriginal],[admin,adminOriginal]] as const){
  const spy=vi.spyOn(original,'fetch').mockResolvedValue(new Response('existing'));
  try{const response=await guard.fetch(new Request('https://test/status'),env('RUNNING') as never,ctx as never);expect(await response.text()).toBe('existing');expect(spy).toHaveBeenCalledTimes(1);}finally{spy.mockRestore();}
 }
});
it('RUNNING delegates cron and queue once without replacing their lifecycle',async()=>{
 const original=(await import('../../apps/async-worker/src/index')).default;
 const scheduled=vi.spyOn(original,'scheduled').mockImplementation(()=>{}),queue=vi.spyOn(original,'queue').mockImplementation(()=>{});
 try{worker.scheduled({} as never,env('RUNNING') as never,ctx as never);worker.queue({} as never,env('RUNNING') as never,ctx as never);expect(scheduled).toHaveBeenCalledTimes(1);expect(queue).toHaveBeenCalledTimes(1);}finally{scheduled.mockRestore();queue.mockRestore();}
});
it('refuses DEV Durable Objects that the HTTP gate cannot quiesce',()=>{
 expect(()=>createDevDeploymentConfig({env:{dev:{name:'lastrail-game-api-dev',vars:{ENVIRONMENT:'DEV'},durable_objects:{bindings:[{name:'ROOM'}]}}}},'game-api','PAUSED',{environment:'DEV',queueName:'lastrail-admin-jobs-dev',deliveryPaused:true,checkedAt:1000},1001)).toThrow('deployment_durable_objects_not_supported');
});
it('overrides environment main so PAUSED cannot deploy the unguarded app',()=>{
 const result=createDevDeploymentConfig({env:{dev:{name:'lastrail-game-api-dev',main:'src/index.ts',vars:{ENVIRONMENT:'DEV'}}}},'game-api','PAUSED',{environment:'DEV',queueName:'lastrail-admin-jobs-dev',deliveryPaused:true,checkedAt:1000},1001);
 expect(result.env.dev.main).toBe('src/dev-deployment.ts');
});
it('preparation CLI writes alongside the source config and refuses overwrite',async()=>{
 const {mkdtemp,mkdir,writeFile,readFile}=await import('node:fs/promises');const {join}=await import('node:path');
 const {prepareDevDeployment}=await import('../../scripts/cloudflare/prepare-dev-deployment');
 const base=await mkdtemp(join(process.env.TEMP!,'deployment-cli-'));
 const folder=join(base,'game-api');await mkdir(folder);
 const config=join(folder,'wrangler.jsonc'),observation=join(base,'observation.json');
 await writeFile(config,JSON.stringify({env:{dev:{name:'lastrail-game-api-dev',vars:{ENVIRONMENT:'DEV'},d1_databases:[{binding:'DB',migrations_dir:'../../packages/db/migrations'}]}}}));
 await writeFile(observation,JSON.stringify({environment:'DEV',queueName:'lastrail-admin-jobs-dev',deliveryPaused:true,checkedAt:Date.now()}));
 const args=['--config',config,'--service','game-api','--state','PAUSED','--queue-observation',observation];
 const result=await prepareDevDeployment(args);expect(result.output).toBe(join(folder,'wrangler.dev-deployment.jsonc'));expect(result.remoteOperations).toBe(0);
 const output=JSON.parse(await readFile(result.output,'utf8'));expect(output.env.dev.vars.DEPLOYMENT_STATE).toBe('PAUSED');expect(output.env.dev.d1_databases[0].migrations_dir).toBe('../../packages/db/migrations');
 await expect(prepareDevDeployment(args)).rejects.toThrow();await expect(prepareDevDeployment([...args,'--remote','true'])).rejects.toThrow('invalid_deployment_arguments');
});

import {afterEach,beforeAll,describe,expect,it,vi} from 'vitest';
import admin from '../../apps/admin-api/src/dev-deployment';
import {runtimeFixture} from '../helpers/maintenance-runtime-fixture';
const path='/v1/admin/maintenance-runtime/initialize';
const issuer='https://test-only.cloudflareaccess.com',aud='test-only-admin';
let keys:CryptoKeyPair,jwk:JsonWebKey;
const encode=(value:Uint8Array)=>btoa(String.fromCharCode(...value)).replaceAll('+','-').replaceAll('/','_').replaceAll('=','');
const json=(value:unknown)=>encode(new TextEncoder().encode(JSON.stringify(value)));
beforeAll(async()=>{keys=await crypto.subtle.generateKey({name:'RSASSA-PKCS1-v1_5',modulusLength:2048,publicExponent:new Uint8Array([1,0,1]),hash:'SHA-256'},true,['sign','verify']);jwk=await crypto.subtle.exportKey('jwk',keys.publicKey);});
afterEach(()=>vi.unstubAllGlobals());
async function fixture(){
 const f=await runtimeFixture();
 vi.stubGlobal('fetch',vi.fn(async(url:unknown)=>{expect(String(url)).toBe(issuer+'/cdn-cgi/access/certs');return Response.json({keys:[{...jwk,kid:'test'}]});}));
 const token=async(sub='owner',audience=aud)=>{const input=json({alg:'RS256',kid:'test'})+'.'+json({iss:issuer,aud:audience,sub,exp:Math.floor(Date.now()/1000)+300});return input+'.'+encode(new Uint8Array(await crypto.subtle.sign('RSASSA-PKCS1-v1_5',keys.privateKey,new TextEncoder().encode(input))));};
 const env={DB:f.db,ENVIRONMENT:'DEV',DEPLOYMENT_STATE:'PAUSED',ADMIN_AUTH_MODE:'ACCESS_JWT_REQUIRED',ADMIN_ACCESS_ISSUER:issuer,ADMIN_ACCESS_AUD:aud,ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''} as unknown as Env & {DEPLOYMENT_STATE:string};
 const send=async(body:unknown={reason:'TEST_ONLY initialize'},options:{url?:string;method?:string;key?:string;token?:string;env?:object}={})=>admin.fetch(new Request('https://test.invalid'+(options.url??path),{method:options.method??'POST',headers:{'Content-Type':'application/json','Idempotency-Key':options.key??'init','Cf-Access-Jwt-Assertion':options.token??await token()},...((options.method??'POST')==='GET'?{}:{body:JSON.stringify(body)})}),{...env,...options.env},{waitUntil(){},passThroughOnException(){}} as ExecutionContext);
 return {...f,env,token,send};
}
describe('paused DEV maintenance initialization',()=>{
 it('initializes with signed owner auth and replays without adding state or audit',async()=>{
  const f=await fixture(),first=await f.send();expect(first.status).toBe(200);expect(first.headers.get('Cache-Control')).toBe('no-store');
  expect(await first.json()).toEqual({accepted:true,environment:'DEV',applicationId:'LASTRAIL',policyRevision:1});
  expect((await f.send()).status).toBe(200);expect(f.count('maintenance_runtime_revisions')).toBe(1);expect(f.count('maintenance_runtime_receipts')).toBe(1);
  expect(f.sql.exec("SELECT count(*) FROM admin_audit_logs WHERE action='MAINTENANCE_RUNTIME_TRANSITION'")[0]!.values[0]![0]).toBe(1);
  expect((await f.send({reason:'changed'})).status).toBe(409);expect((await f.send(undefined,{key:'other'})).status).toBe(409);
 });
 it('keeps other paused routes, methods, path suffixes and environment states closed',async()=>{
  const f=await fixture();
  for(const url of ['/health','/v1/admin/maintenance-plans',path+'/other',path+'/'])expect((await f.send(undefined,{url})).status).toBe(503);
  expect((await f.send(undefined,{method:'GET'})).status).toBe(503);
  for(const env of [{ENVIRONMENT:'PRD'},{ENVIRONMENT:'LOCAL'},{DEPLOYMENT_STATE:'RUNNING'},{DEPLOYMENT_STATE:undefined},{DEPLOYMENT_STATE:'paused'}])expect((await f.send(undefined,{env})).status).toBe(503);
  expect(f.count('maintenance_runtime_heads')).toBe(0);
 });
 it('rejects missing or wrong JWT and LOCAL_HEADER without writes',async()=>{
  const f=await fixture();expect((await f.send(undefined,{token:''})).status).toBe(401);
  expect((await f.send(undefined,{token:await f.token('owner','wrong')})).status).toBe(403);
  expect((await f.send(undefined,{env:{ADMIN_AUTH_MODE:'LOCAL_HEADER'}})).status).toBe(503);
  expect(f.count('maintenance_runtime_heads')).toBe(0);
 });
 it('rejects non-owner, disabled owner and authority revoked after receipt creation',async()=>{
  const f=await fixture();f.sql.run("INSERT INTO admin_principals VALUES('backup','backup','OWNER','ACTIVE',NULL,0,0)");f.sql.run("UPDATE admin_principals SET role='VIEWER' WHERE id='owner'");expect((await f.send()).status).toBe(403);
  f.sql.run("UPDATE admin_principals SET role='OWNER' WHERE id='owner'");expect((await f.send()).status).toBe(200);
  f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='owner'");expect((await f.send()).status).toBe(403);
  expect(f.count('maintenance_runtime_revisions')).toBe(1);
 });
 it('accepts only a bounded reason and key; no client context or operational commands',async()=>{
  const f=await fixture();
  for(const body of [{},{reason:''},{reason:'x'.repeat(501)},{reason:'ok',environment:'PRD'},{reason:'ok',applicationId:'OTHER'},{reason:'ok',command:'ACTIVATE'},{reason:'ok',expectedRevision:null}])expect((await f.send(body)).status).toBe(400);
  expect((await f.send(undefined,{key:''})).status).toBe(400);expect((await f.send(undefined,{url:path+'?mode=normal'})).status).toBe(400);
  expect(f.count('maintenance_runtime_heads')).toBe(0);
 });
 it('rolls back a failed audit and hides storage errors',async()=>{
  const f=await fixture();f.sql.run("CREATE TRIGGER fail_init BEFORE INSERT ON admin_audit_logs WHEN NEW.action='MAINTENANCE_RUNTIME_TRANSITION' BEGIN SELECT RAISE(ABORT,'private storage detail'); END");
  const r=await f.send();expect(r.status).toBe(503);expect(await r.json()).toEqual({error:'maintenance_policy_unavailable'});
  for(const table of ['maintenance_runtime_heads','maintenance_runtime_revisions','maintenance_runtime_receipts'])expect(f.count(table)).toBe(0);
 });
});
it('does not reset a later active policy when replaying the initialization receipt',async()=>{
 const f=await fixture();expect((await f.send()).status).toBe(200);
 await f.store.transition(f.actor,{command:'ACTIVATE',expectedRevision:1,maintenanceId:'m',draftRevision:1},'TEST_ONLY','activate');
 expect(await (await f.send()).json()).toMatchObject({policyRevision:1});expect(f.count('maintenance_runtime_revisions')).toBe(2);
 expect(f.sql.exec('SELECT revision FROM maintenance_runtime_heads')[0]!.values[0]![0]).toBe(2);
});
it('rejects oversized requests and missing schema without exposing storage details',async()=>{
 const f=await fixture();expect((await f.send({reason:'x'.repeat(70000)})).status).toBe(400);
 f.sql.run('DROP TABLE maintenance_runtime_receipts');const r=await f.send();expect(r.status).toBe(503);expect(await r.json()).toEqual({error:'maintenance_policy_unavailable'});
});
it('rechecks OWNER at the actual write boundary',async()=>{
 const f=await fixture();f.sql.run("INSERT INTO admin_principals VALUES('backup','backup','OWNER','ACTIVE',NULL,0,0)");
 const batch=f.db.batch.bind(f.db);f.db.batch=async statements=>{f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='owner'");return batch(statements);};
 expect((await f.send()).status).toBe(403);expect(f.count('maintenance_runtime_heads')).toBe(0);
});

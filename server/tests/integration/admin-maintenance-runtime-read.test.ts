import {describe,expect,it} from 'vitest';
import admin from '../../apps/admin-api/src/index';
import {runtimeFixture} from '../helpers/maintenance-runtime-fixture';

async function fixture(){
 const f=await runtimeFixture();
 const env={DB:f.db,ENVIRONMENT:'DEV',ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173'} as unknown as Env;
 const read=(suffix='',subject='owner',overrides:object={})=>admin.request('/v1/admin/maintenance-runtime'+suffix,{headers:{'Cf-Access-Subject':subject}},{...env,...overrides});
 return {...f,read};
}
describe('Admin applied maintenance policy read',()=>{
 it('does not fabricate NORMAL before initialization and reads the current policy afterwards',async()=>{
  const f=await fixture();expect((await f.read()).status).toBe(503);
  await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'Initial policy','init');
  const response=await f.read();expect(response.status).toBe(200);expect(response.headers.get('Cache-Control')).toBe('no-store');
  expect(await response.json()).toMatchObject({contractVersion:'admin-maintenance-runtime-v1',environment:'DEV',applicationId:'LASTRAIL',
   policy:{revision:1,state:'NORMAL',maintenanceId:null,lastChange:{principalId:'owner',reason:'Initial policy'}},
   capabilities:{canTransition:false,gameLoginControl:false,webModeControl:false}});
  expect(f.count('maintenance_runtime_revisions')).toBe(1);
 });
 it('returns the pinned current revision without automatically completing an expired schedule',async()=>{
  const f=await fixture();await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'init','init');
  await f.store.transition(f.actor,{command:'SCHEDULE',expectedRevision:1,maintenanceId:'m',draftRevision:1},'Scheduled','schedule');
  expect(await (await f.read()).json()).toMatchObject({policy:{revision:2,state:'SCHEDULED',maintenanceId:'m',draftRevision:1,estimatedEndAt:3}});
  expect(f.count('maintenance_runtime_revisions')).toBe(2);
  expect((await f.read('', 'owner',{ENVIRONMENT:'PRD'})).status).toBe(503);
 });
 it('rejects untrusted scope, absent auth and disabled admins; does not expose operational writes',async()=>{
  const f=await fixture();await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'init','init');
  expect((await f.read('?environment=PRD')).status).toBe(400);
  expect((await f.read('', 'missing')).status).toBe(403);
  f.sql.run("INSERT INTO admin_principals VALUES('backup','backup','OWNER','ACTIVE',NULL,0,0)");
  f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='owner'");
  expect((await f.read()).status).toBe(403);
  expect(f.count('maintenance_runtime_revisions')).toBe(1);
 });
 it('allows existing read roles but reports unsupported controls and rejects malformed stored policy',async()=>{
  const f=await fixture();await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'init','init');
  f.sql.run("INSERT INTO admin_principals VALUES('viewer','viewer','VIEWER','ACTIVE',NULL,0,0)");
  expect((await f.read('', 'viewer')).status).toBe(200);
  expect((await f.read('', 'owner',{ENVIRONMENT:'LOCAL'})).status).toBe(503);
  const prepare=f.db.prepare.bind(f.db);
  f.db.prepare=(query:string)=>{
   const statement=prepare(query);
   if(query.startsWith('SELECT r.* FROM maintenance_runtime_heads')){
    const first=statement.first.bind(statement);
    statement.first=async <T>()=>({...await first<Record<string,unknown>>(),starts_at:1}) as T;
   }
   return statement;
  };
  expect((await f.read()).status).toBe(503);
  expect(f.count('maintenance_runtime_revisions')).toBe(1);
 });
});

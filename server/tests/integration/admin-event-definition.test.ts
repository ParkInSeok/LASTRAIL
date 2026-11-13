import {afterEach,expect,it} from 'vitest';
import {Hono} from 'hono';
import {setup} from '../helpers/departure-fixture';
import {registerAdminEventDefinitionRoutes} from '../../apps/admin-api/src/admin-event-definition-routes';
import admin from '../../apps/admin-api/src/index';
import {captureEventCosmeticReplacement} from '../../apps/game-api/src/event-cosmetic-replacement';
const cleanup:(()=>void)[]=[];afterEach(()=>{for(const close of cleanup.splice(0))close();});
it('serves the approved replacement table through actual Admin SUPPORT_READ without implying execution readiness',async()=>{
 const f=await setup(false,true,null);cleanup.push(()=>f.sql.close());
 f.sql.run("INSERT INTO admin_principals VALUES('table-reader','table-reader','VIEWER','ACTIVE',NULL,0,0)");
 const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',
  ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 const path='/v1/admin/events/cosmetic-replacement-table';
 const request=(subject?:string,suffix='')=>admin.request(path+suffix,{headers:subject?{'Cf-Access-Subject':subject}:{}},env);
 const before=Buffer.from(f.sql.export()),response=await request('table-reader');
 expect(response.status).toBe(200);expect(response.headers.get('Cache-Control')).toBe('no-store');
 const body=await response.json();
 expect(body).toMatchObject({contractVersion:'admin.event-cosmetic-replacement-table.v1.20261020',permission:'SUPPORT_READ',environment:'DEV',
  sourceAuthorityVerified:false,executeSupported:false,publicationSupported:false,persisted:false});
 expect(body.definition).toEqual(await captureEventCosmeticReplacement());expect(body.definition.rows).toHaveLength(4);
 expect(Buffer.from(f.sql.export()).equals(before)).toBe(true);
 expect([401,403]).toContain((await request()).status);
 expect((await request('table-reader','?environment=PRD')).status).toBe(400);
 const withBody=new Request('http://localhost'+path,{method:'POST',headers:{'Cf-Access-Subject':'table-reader'},body:'{}'});
 Object.defineProperty(withBody,'method',{value:'GET'});
 expect((await admin.fetch(withBody,env)).status).toBe(400);
 f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='table-reader'");
 expect([401,403]).toContain((await request('table-reader')).status);
});
it('authenticates proposal-only preview, rechecks SUPPORT_READ, and never changes stored state',async()=>{
 const f=await setup(false,true,null);cleanup.push(()=>f.sql.close());
 f.sql.run("INSERT INTO admin_principals VALUES('event-reader','event-reader','VIEWER','ACTIVE',NULL,0,0)");
 const app=new Hono<{Bindings:Env}>();
 registerAdminEventDefinitionRoutes(app,async()=>({id:'event-reader',role:'VIEWER'}));
 const input={environment:'DEV',roundId:'r',revision:1,policyVersion:null,title:'Event',theme:'Theme',
  schedule:{publishAt:1000,startAt:1000,endAt:1209601000,claimUntil:null,giftOpenAt:null},
  rewards:[{rightId:'r',kind:'PARTICIPATION',condition:'Login',producerId:null,threshold:null,itemId:null,quantity:1,perAccountCount:1,policyVersion:null}]};
 const request=()=>app.request('/v1/admin/events/definition-preview',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(input)},f.env);
 const before=Buffer.from(f.sql.export()),response=await request();
 expect(response.status).toBe(200);expect(await response.json()).toMatchObject({persisted:false,scheduleSupported:false});
 expect(Buffer.from(f.sql.export()).equals(before)).toBe(true);
 f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='event-reader'");
 expect((await request()).status).toBe(403);
 expect((await app.request('/v1/admin/events/schedule',{method:'POST'},f.env)).status).toBe(404);
});
it('exposes authenticated outage recommendations without mutating an event or granting command authority',async()=>{
 const f=await setup(false,true,null);cleanup.push(()=>f.sql.close());
 f.sql.run("INSERT INTO admin_principals VALUES('event-reader','event-reader','VIEWER','ACTIVE',NULL,0,0)");
 const app=new Hono<{Bindings:Env}>();
 registerAdminEventDefinitionRoutes(app,async()=>({id:'event-reader',role:'VIEWER'}));
 const day=86400000,T=20*day;
 const input={environment:'DEV',roundId:'r',revision:2,expectedRevision:2,definitionHash:'a'.repeat(64),targetScope:'same scope',
  observedAt:T+day,claimOpensAt:day,alreadyAppliedImpactIds:[],
  schedule:{startAt:day,participationEndsAt:T,completionGraceEndsAt:T+day,manualClaimEndsAt:T+7*day},
  impacts:[{impactId:'impact',incidentId:'incident',feature:'PARTICIPATION',status:'CONFIRMED',startAt:T-day,endAt:T,evidenceRef:'evidence'}]};
 const request=()=>app.request('/v1/admin/events/outage-preview',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(input)},f.env);
 const before=Buffer.from(f.sql.export()),response=await request();
 expect(response.status).toBe(200);
 expect(await response.json()).toMatchObject({executeSupported:false,sourceAuthorityVerified:false,persisted:false,
  recommendedSchedule:{participationEndsAt:T+day,completionGraceEndsAt:T+2*day,manualClaimEndsAt:T+8*day}});
 expect(Buffer.from(f.sql.export()).equals(before)).toBe(true);
 f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='event-reader'");
 expect((await request()).status).toBe(403);
});
it('serves the registered Admin route only to active authenticated readers with approved derived deadlines',async()=>{
 const f=await setup(false,true,null);cleanup.push(()=>f.sql.close());
 f.sql.run("INSERT INTO admin_principals VALUES('event-reader','event-reader','VIEWER','ACTIVE',NULL,0,0)");
 const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ALLOWED_ADMIN_ORIGINS:'http://localhost:5173',
  ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
 const input={environment:'DEV',roundId:'r',revision:2,policyVersion:null,title:'Event',theme:'Theme',
  schedule:{publishAt:1000,startAt:1000,endAt:1209601000,claimUntil:null,giftOpenAt:null},
  rewards:[{rightId:'r',kind:'PARTICIPATION',condition:'Login',producerId:null,threshold:null,itemId:null,quantity:1,perAccountCount:1,policyVersion:null}]};
 const request=(subject?:string)=>admin.request('/v1/admin/events/definition-preview',{
  method:'POST',headers:{'Content-Type':'application/json',...(subject?{'Cf-Access-Subject':subject}:{})},body:JSON.stringify(input)},env);
 const response=await request('event-reader');
 expect(response.status).toBe(200);
 expect(response.headers.get('Cache-Control')).toBe('no-store');
 expect(await response.json()).toMatchObject({environment:'DEV',roundId:'r',revision:2,persisted:false,scheduleSupported:false,
  timePolicy:{participationEndsAt:1209601000,completionGraceEndsAt:1296001000,manualClaimEndsAt:1814401000,
   participationEndExclusive:true,completionGraceEndExclusive:true,manualClaimEndExclusive:true,settlementExecuted:false}});
 expect([401,403]).toContain((await request()).status);
 expect([401,403]).toContain((await request('unregistered-reader')).status);
 f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='event-reader'");
 expect([401,403]).toContain((await request('event-reader')).status);
});

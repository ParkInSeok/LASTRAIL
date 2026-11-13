import {writeFileSync} from 'node:fs';
import {setup} from '../../tests/helpers/departure-fixture';
import app from '../../apps/admin-api/src/index';
const f=await setup(false,true,null);
f.sql.run("INSERT INTO admin_principals VALUES('OWNER','OWNER','OWNER','ACTIVE',NULL,0,0)");
const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
const plan={applicationId:'LASTRAIL',serviceScope:'GAME_AND_WEB',webMode:'READ_ONLY',title:'TEST_ONLY 점검 미리보기',summary:'실제 점검 상태가 아닙니다.',noticeId:null,drainStartsAt:1000,startsAt:2000,estimatedEndAt:3000,existingRunPolicy:'DRAIN_THEN_PRESERVE',statusFreshnessSeconds:60,retryAfterSeconds:15};
const samples:Record<string,unknown>={};
async function capture(name:string,path:string,body?:object,key='draft'){
 const response=await app.request(path,{method:body?'POST':'GET',headers:{'Cf-Access-Subject':'OWNER','Content-Type':'application/json','Idempotency-Key':key},...(body?{body:JSON.stringify(body)}:{})},env);
 samples[name]={status:response.status,cacheControl:response.headers.get('Cache-Control'),body:await response.json()};
}
const route='/v1/admin/maintenance-plans';
const command={environment:'DEV',command:'SAVE_DRAFT',expectedDraftRevision:null,reason:'TEST_ONLY internal reason',plan};
await capture('emptyUnconnected',route);
await capture('draftSaved',route+'/test_plan/commands',command);
await capture('readDraft',route+'/test_plan');
await capture('beforeDrain',route+'/test_plan/preview?at=999');
await capture('drainingPreview',route+'/test_plan/preview?at=1000');
await capture('maintenancePreview',route+'/test_plan/preview?at=2000');
await capture('afterEstimatedEndStillMaintenance',route+'/test_plan/preview?at=4000');
await capture('activationUnavailable',route+'/test_plan/commands',{environment:'DEV',command:'ACTIVATE',expectedDraftRevision:1,reason:'TEST_ONLY attempt'},'activate');
await capture('saveReplay',route+'/test_plan/commands',command);
writeFileSync('D:/Project/LASTRAIL/QA/MaintenancePlanServer-20260921/http-fixtures.json',JSON.stringify({scope:'TEST_ONLY_LOCAL_HONO_SQLJS',samples},null,2)+'\n');
f.sql.close();
import {writeFileSync} from 'node:fs';
import {setup} from '../../tests/helpers/departure-fixture';
import app from '../../apps/admin-api/src/index';

// Actual local Hono responses, isolated SQL.js with synthetic Admin authentication.
const f=await setup(false,true,null);
f.sql.run("INSERT INTO admin_principals VALUES('OWNER','OWNER','OWNER','ACTIVE',NULL,0,0)");
const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
const samples:Record<string,unknown>={};
const command={environment:'DEV',adsEnabled:false,expectedRuntimePolicyRevision:null,reason:'TEST_ONLY turn advertising off'};
async function capture(name:string,body?:unknown,key='save',path='/v1/admin/runtime-policy'){
 const r=await app.request(path,{method:body?'POST':'GET',headers:{'Cf-Access-Subject':'OWNER','Content-Type':'application/json','Idempotency-Key':key},...(body?{body:JSON.stringify(body)}:{})},env);
 samples[name]={status:r.status,cacheControl:r.headers.get('Cache-Control'),body:await r.json()};
}
await capture('unknown');
await capture('saved',command);
await capture('off');
await capture('replay',command);
await capture('stale',command,'stale');
await capture('onBlocked',{...command,expectedRuntimePolicyRevision:1,adsEnabled:true},'on');
await capture('audit',undefined,'unused','/v1/admin/audit?action=RUNTIME_POLICY_UPDATE&resourceId=DEV');
writeFileSync('D:/Project/LASTRAIL/QA/AdminRuntimePolicy-20260921/http-fixtures.json',JSON.stringify({scope:'TEST_ONLY_LOCAL_HONO_SQLJS',samples},null,2)+'\n');
f.sql.close();

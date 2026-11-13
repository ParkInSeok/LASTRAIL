import {writeFileSync} from 'node:fs';
import {setup} from '../../tests/helpers/departure-fixture';
import admin from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';
const f=await setup(false,true,null);
f.sql.run("INSERT INTO admin_principals VALUES('OWNER','OWNER','OWNER','ACTIVE',NULL,0,0)");
const env={...f.env,ENVIRONMENT:'PRD',ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:'',CLIENT_ADMISSION_MODE:'ENFORCED'};
const path='/v1/public/probability-disclosures',samples:Record<string,unknown>={};
async function capture(name:string,response:Response,expected:number){
 const body=await response.json();if(response.status!==expected)throw new Error(name+': unexpected status '+response.status);
 samples[name]={status:response.status,cacheControl:response.headers.get('Cache-Control'),body};
 return body as {items:Array<{disclosureId:string}>;nextCursor:string|null};
}
await capture('emptyHistory',await game.request(path,{},env),200);
for(const [i,consumer] of ['OFFLINE','COMBAT','OFFLINE'].entries()){
 const version='TEST_HISTORY_'+i,table=consumer==='OFFLINE'?'offline_ad_policies':'combat_ad_policies';
 f.sql.run("UPDATE "+table+" SET status='RETIRED'");
 const config={version,environment:'PRD',placement:consumer==='OFFLINE'?'OFFLINE_SUPPLY':'COMBAT_CLEAR',weightsBps:consumer==='OFFLINE'?[1,0,9999]:[6000,3000,1000]};
 f.sql.run(consumer==='OFFLINE'?"INSERT INTO offline_ad_policies VALUES(?,'PRD','ACTIVE',?,0,'TEST_ONLY')":"INSERT INTO combat_ad_policies VALUES(?,'PRD','ACTIVE',?)",[version,JSON.stringify(config)]);
 const r=await admin.request('/v1/admin/probability-disclosures/source?consumer='+consumer+'&policyVersion='+version,{headers:{'Cf-Access-Subject':'OWNER'}},env);
 if(r.status!==200)throw new Error('source failed');
 const source=await r.json() as {sourceDigest:string};
 const published=await admin.request(path.replace('/public/','/admin/'),{method:'POST',headers:{'Cf-Access-Subject':'OWNER','Content-Type':'application/json','Idempotency-Key':'TEST_HISTORY_'+i},body:JSON.stringify({environment:'PRD',consumer,policyVersion:version,expectedSourceDigest:source.sourceDigest,reason:'TEST_ONLY local history sample'})},env);
 if(published.status!==200)throw new Error('publish failed');
}
const first=await capture('firstPage',await game.request(path+'?limit=2',{},env),200);
await capture('lastPage',await game.request(path+'?limit=2&cursor='+encodeURIComponent(first.nextCursor!),{},env),200);
await capture('firstPermalink',await game.request(path+'/'+first.items[0]!.disclosureId,{},env),200);
await capture('invalidLimit',await game.request(path+'?limit=51',{},env),400);
await capture('foreignCursor',await game.request(path+'?cursor=DEV.'+first.items[0]!.disclosureId,{},env),400);
await capture('devUnauthenticated',await game.request(path,{}, {...env,ENVIRONMENT:'DEV'}),401);
f.sql.run("UPDATE offline_ad_policies SET status='RETIRED'");f.sql.run("UPDATE combat_ad_policies SET status='RETIRED'");
await capture('retiredHistory',await game.request(path,{},env),200);
writeFileSync('D:/Project/LASTRAIL/QA/ProbabilityHistoryServer-20260921/http-fixtures.json',JSON.stringify({scope:'TEST_ONLY_LOCAL_HONO_SQLJS',samples},null,2)+'\n');
f.sql.close();

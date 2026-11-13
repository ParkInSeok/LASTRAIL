import {writeFileSync} from 'node:fs';
import {setup} from '../../tests/helpers/departure-fixture';
import admin from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';
const f=await setup(false,true,null);
f.sql.run("INSERT INTO admin_principals VALUES('OWNER','OWNER','OWNER','ACTIVE',NULL,0,0)");
const env={...f.env,ENVIRONMENT:'PRD',ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:'',CLIENT_ADMISSION_MODE:'ENFORCED'};
for(const consumer of ['OFFLINE','COMBAT']){
 const version='TEST_'+consumer;
 const config={version,environment:'PRD',placement:consumer==='OFFLINE'?'OFFLINE_SUPPLY':'COMBAT_CLEAR',weightsBps:consumer==='OFFLINE'?[1,0,9999]:[6000,3000,1000]};
 f.sql.run(consumer==='OFFLINE'?"INSERT INTO offline_ad_policies VALUES(?,'PRD','ACTIVE',?,0,'TEST_ONLY')":"INSERT INTO combat_ad_policies VALUES(?,'PRD','ACTIVE',?)",[version,JSON.stringify(config)]);
}
const samples:Record<string,unknown>={};
async function capture(name:string,response:Response,expected:number){
 const body=await response.json();if(response.status!==expected)throw new Error(name+': unexpected status '+response.status);
 samples[name]={status:response.status,cacheControl:response.headers.get('Cache-Control'),body};
 return body as Record<string,string>;
}
for(const consumer of ['OFFLINE','COMBAT']){
 const source=await capture(consumer+'Source',await admin.request('/v1/admin/probability-disclosures/source?consumer='+consumer+'&policyVersion=TEST_'+consumer,{headers:{'Cf-Access-Subject':'OWNER'}},env),200);
 const path='/v1/public/probability-disclosures/'+source.disclosureId;
 await capture(consumer+'Unpublished',await game.request(path,{},env),404);
 const init={method:'POST',headers:{'Cf-Access-Subject':'OWNER','Content-Type':'application/json','Idempotency-Key':'publish-'+consumer},body:JSON.stringify({environment:'PRD',consumer,policyVersion:'TEST_'+consumer,expectedSourceDigest:source.sourceDigest,reason:'TEST_ONLY sample approval'})};
 await capture(consumer+'Published',await admin.request('/v1/admin/probability-disclosures',init,env),200);
 await capture(consumer+'Public',await game.request(path,{},env),200);
 await capture(consumer+'Replay',await admin.request('/v1/admin/probability-disclosures',init,env),200);
 await capture(consumer+'AlreadyPublished',await admin.request('/v1/admin/probability-disclosures/source?consumer='+consumer+'&policyVersion=TEST_'+consumer,{headers:{'Cf-Access-Subject':'OWNER'}},env),200);
}
writeFileSync('D:/Project/LASTRAIL/QA/ProbabilityDisclosureServer-20260921/http-fixtures.json',JSON.stringify({scope:'TEST_ONLY_LOCAL_HONO_SQLJS',samples},null,2)+'\n');
f.sql.close();

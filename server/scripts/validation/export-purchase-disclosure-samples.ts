import {writeFileSync} from 'node:fs';
import {setup} from '../../tests/helpers/departure-fixture';
import admin from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';
const f=await setup(false,true,null);
f.sql.run("INSERT INTO admin_principals VALUES('OWNER','OWNER','OWNER','ACTIVE',NULL,0,0)");
const env={...f.env,ENVIRONMENT:'PRD',ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:'',CLIENT_ADMISSION_MODE:'ENFORCED'};
const samples:Record<string,unknown>={};
async function capture(name:string,response:Response,expected:number){
 const body=await response.json();if(response.status!==expected)throw new Error(name+': unexpected status '+response.status);
 samples[name]={status:response.status,cacheControl:response.headers.get('Cache-Control'),body};
 return body as {sourceDigest:string;disclosureId:string};
}
for(const consumer of ['OFFLINE','COMBAT']){
 const version='TEST_PURCHASE_'+consumer;
 const config={version,environment:'PRD',placement:consumer==='OFFLINE'?'OFFLINE_SUPPLY':'COMBAT_CLEAR',weightsBps:[6000,3000,1000]};
 f.sql.run("INSERT INTO purchase_reward_policies VALUES('PRD',?,?)",[consumer,JSON.stringify(config)]);
 if(consumer==='OFFLINE')f.sql.run("INSERT INTO offline_ad_policies VALUES(?,'PRD','ACTIVE',?,0,'TEST_ONLY')",[version,JSON.stringify(config)]);
 const source=await capture(consumer+'PurchaseSource',await admin.request('/v1/admin/probability-disclosures/source?sourceKind=PURCHASE_POLICY&consumer='+consumer+'&policyVersion='+version,{headers:{'Cf-Access-Subject':'OWNER'}},env),200);
 const init={method:'POST',headers:{'Cf-Access-Subject':'OWNER','Content-Type':'application/json','Idempotency-Key':'purchase-'+consumer},body:JSON.stringify({environment:'PRD',consumer,sourceKind:'PURCHASE_POLICY',policyVersion:version,expectedSourceDigest:source.sourceDigest,reason:'TEST_ONLY local purchase publication'})};
 await capture(consumer+'Publish',await admin.request('/v1/admin/probability-disclosures',init,env),200);
 await capture(consumer+'Public',await game.request('/v1/public/probability-disclosures/'+source.disclosureId,{},env),200);
 if(consumer==='OFFLINE'){
  const ad=await capture('SameBytesAdSource',await admin.request('/v1/admin/probability-disclosures/source?consumer=OFFLINE&policyVersion='+version,{headers:{'Cf-Access-Subject':'OWNER'}},env),200);
  if(ad.sourceDigest!==source.sourceDigest||ad.disclosureId===source.disclosureId)throw new Error('source provenance not isolated');
 }
 f.sql.run("DELETE FROM purchase_reward_policies WHERE kind=?",[consumer]);
 await capture(consumer+'ReplayAfterSourceDeletion',await admin.request('/v1/admin/probability-disclosures',init,env),200);
}
await capture('History',await game.request('/v1/public/probability-disclosures',{},env),200);
writeFileSync('D:/Project/LASTRAIL/QA/PurchaseDisclosureServer-20260921/http-fixtures.json',JSON.stringify({scope:'TEST_ONLY_LOCAL_HONO_SQLJS',samples},null,2)+'\n');
f.sql.close();

import {writeFileSync} from 'node:fs';
import {setup} from '../../tests/helpers/departure-fixture';
import admin from '../../apps/admin-api/src/index';
import game from '../../apps/game-api/src/index';
const f=await setup();
f.sql.run("INSERT INTO admin_principals VALUES('OWNER','OWNER','OWNER','ACTIVE',NULL,0,0)");
const env={...f.env,ADMIN_AUTH_MODE:'LOCAL_HEADER',ADMIN_ACCESS_ISSUER:'',ADMIN_ACCESS_AUD:'',ADMIN_DELEGATED_ACCESS_ISSUER:'',ADMIN_DELEGATED_ACCESS_AUD:''};
const policy={version:'TEST_PIN',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[6000,3000,1000],challengeLifetimeMs:60000,callbackGraceMs:60000,maxAdsPerDay:10,dayStartUtcMinutes:0,adUnit:'TEST_UNIT',appId:'TEST_APP',rewardItem:'TEST_ITEM',rewardAmount:'1'};
f.sql.run("INSERT INTO offline_ad_policies VALUES(?,'DEV','ACTIVE',?,0,'TEST_ONLY')",[policy.version,JSON.stringify(policy)]);
const samples:Record<string,unknown>={};
async function capture(name:string,response:Response,expected:number){
 const body=await response.json();if(response.status!==expected)throw new Error(name+': unexpected status '+response.status);
 samples[name]={status:response.status,cacheControl:response.headers.get('Cache-Control'),body};
 return body as {sourceDigest:string;intentId:string;probabilityDisclosure:{publicPath:string}};
}
const headers={...f.headers,'X-Reward-Contract':'reward-opportunity-v2','Idempotency-Key':'TEST_CREATE'};
const create=()=>game.request('/v1/offline-rewards/ad-intents',{method:'POST',headers},env);
await capture('blockedBeforePublication',await create(),503);
const source=await capture('sourcePreview',await admin.request('/v1/admin/probability-disclosures/source?consumer=OFFLINE&policyVersion=TEST_PIN',{headers:{'Cf-Access-Subject':'OWNER'}},env),200);
await capture('approvedPublication',await admin.request('/v1/admin/probability-disclosures',{method:'POST',headers:{'Cf-Access-Subject':'OWNER','Idempotency-Key':'TEST_PUBLISH','Content-Type':'application/json'},body:JSON.stringify({environment:'DEV',consumer:'OFFLINE',policyVersion:'TEST_PIN',expectedSourceDigest:source.sourceDigest,reason:'TEST_ONLY prerequisite'})},env),200);
const created=await capture('pinnedOpportunity',await create(),200);
f.sql.run('DELETE FROM offline_ad_policies');
await capture('replayAfterSourceDeletion',await create(),200);
await capture('readAfterSourceDeletion',await game.request('/v1/offline-rewards/ad-intents/'+created.intentId,{headers},env),200);
f.sql.run("INSERT INTO account_tester_entitlements VALUES('a','DEV','123','ACTIVE',?,NULL,'TEST_ONLY',1,0,0)",[Date.now()+60000]);
await capture('pinnedPublicDocument',await game.request(created.probabilityDisclosure.publicPath,{headers:{...f.headers,'X-LastRail-App-Version-Code':'123'}},{...env,CLIENT_ADMISSION_MODE:'ENFORCED'}),200);
writeFileSync('D:/Project/LASTRAIL/QA/RewardDisclosurePinServer-20260921/http-fixtures.json',JSON.stringify({scope:'TEST_ONLY_LOCAL_HONO_SQLJS',samples},null,2)+'\n');
f.sql.close();

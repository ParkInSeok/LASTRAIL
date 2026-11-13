import {writeFileSync} from 'node:fs';
import {setup} from '../../tests/helpers/departure-fixture';
import app from '../../apps/game-api/src/index';

// Actual local Hono responses over isolated SQL.js. No network/real account/START.
const f=await setup(false,true,null);
const samples:Record<string,unknown>={};
async function capture(name:string,path:string,key?:string){
 const r=await app.request(path,{method:key?'POST':'GET',headers:{...f.headers,...(key?{'Idempotency-Key':key}:{})}},f.env);
 samples[name]={status:r.status,body:await r.json()};
 return samples[name] as {status:number;body:{intentId:string}};
}
await capture('unknown','/v1/runtime-policy');
f.sql.run("INSERT INTO ad_runtime_policies VALUES('DEV',1,1)");
await capture('on','/v1/runtime-policy');
const policy={version:'TEST_ONLY',environment:'DEV',placement:'OFFLINE_SUPPLY',weightsBps:[6000,3000,1000],challengeLifetimeMs:60000,
 callbackGraceMs:60000,maxAdsPerDay:2,dayStartUtcMinutes:0,adUnit:'TEST_UNIT',appId:'TEST_APP',rewardItem:'TEST',rewardAmount:'1'};
f.sql.run("INSERT INTO offline_ad_policies VALUES('TEST_ONLY','DEV','ACTIVE',?,0,'TEST_ONLY')",[JSON.stringify(policy)]);
const legacy=await app.request('/v1/offline-rewards/ad-intents',{method:'POST',headers:{Authorization:f.headers.Authorization,'Idempotency-Key':'legacy'}},f.env);
samples.legacyCreate={status:legacy.status,body:await legacy.json()};
const created=await capture('created','/v1/offline-rewards/ad-intents','create');
const path=`/v1/ad-opportunities/OFFLINE/${created.body.intentId}/start`;
await capture('started',path,'start');
f.sql.run("UPDATE ad_runtime_policies SET revision=2,ads_enabled=0 WHERE environment='DEV'");
await capture('off','/v1/runtime-policy');
await capture('startReplayWhileOff',path,'start');
await capture('createReplayWhileOff','/v1/offline-rewards/ad-intents','create');
await capture('entitlementWhileOff','/v1/ad-skip');
await capture('basicClaimWhileOff','/v1/offline-rewards/claim','basic');
await capture('newAdWhileOff','/v1/offline-rewards/ad-intents','new-after-basic');
writeFileSync('D:/Project/LASTRAIL/QA/AdRuntimePolicy-20260921/http-fixtures.json',JSON.stringify({scope:'TEST_ONLY_LOCAL_HONO',samples},null,2)+'\n');
f.sql.close();

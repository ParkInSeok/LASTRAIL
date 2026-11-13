// Disposable in-memory SQL.js fixture only. No provider, persisted DB or network.
import {mkdirSync,writeFileSync} from 'node:fs';
import {join} from 'node:path';
import {setup} from '../../tests/helpers/departure-fixture';
import app from '../../apps/game-api/src/index';
import {revokeAdSkipOrder} from '../../apps/game-api/src/ad-skip-entitlement';
const output='D:/Project/LASTRAIL/QA/AdSkipServer-20260917/Fixtures';
mkdirSync(output,{recursive:true});
const f=await setup();
const env={...f.env,AD_SKIP_PURCHASE_VERIFIER:async(input:{receipt:string})=>JSON.parse(input.receipt)};
const headers={...f.headers,'X-Reward-Contract':'reward-opportunity-v2'};
async function capture(name:string,path:string,body?:object,expected=200){
 const response=await app.request(path,{method:body?'POST':'GET',headers:{...headers,'Idempotency-Key':name},...(body?{body:JSON.stringify(body)}:{})},env);
 const json=await response.json();if(response.status!==expected)throw new Error(name+': '+JSON.stringify(json));
 writeFileSync(join(output,name+'.json'),JSON.stringify(json,null,2)+'\n');return json;
}
await capture('01-unconfigured','/v1/ad-skip');
await capture('02-purchase-disabled','/v1/ad-skip/checkout-intents',{catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'},503);
f.sql.run("INSERT INTO ad_skip_catalogs VALUES('DEV',?)",[JSON.stringify({version:'TEST_ONLY',provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.permanent',environment:'DEV',checkoutLifetimeMs:60000})]);
await capture('03-configured-unowned','/v1/ad-skip');
const checkout=await capture('04-checkout','/v1/ad-skip/checkout-intents',{catalogVersion:'TEST_ONLY',provider:'GOOGLE_PLAY'}) as {checkoutIntentId:string;storeAccountBinding:string};
const proof={accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',productId:'test.permanent',transactionId:'TEST_ONLY_TRANSACTION',state:'PURCHASED',revoked:false,purchasedAt:Date.now(),verifiedAt:Date.now(),binding:checkout.storeAccountBinding};
await capture('05-verified','/v1/ad-skip/purchases/verify',{checkoutIntentId:checkout.checkoutIntentId,receipt:JSON.stringify(proof)});
await capture('06-restored','/v1/ad-skip/purchases/restore',{provider:'GOOGLE_PLAY',receipt:JSON.stringify(proof)});
for(const [kind,placement] of [['OFFLINE','OFFLINE_SUPPLY'],['COMBAT','COMBAT_CLEAR']])f.sql.run("INSERT INTO purchase_reward_policies VALUES('DEV',?,?)",[kind!,JSON.stringify({version:'TEST_ONLY',environment:'DEV',placement,weightsBps:[6000,3000,1000]})]);
await capture('07-owned-available','/v1/ad-skip');
const offline=await capture('08-offline-approved','/v1/ad-skip/uses/authorize',{opportunity:{kind:'OFFLINE'}}) as {intentId:string};
await capture('09-offline-read','/v1/offline-rewards/ad-intents/'+offline.intentId);
const spin=async(name:string,path:string)=>{
 const r=await app.request(path,{method:'POST',headers:{...headers,'Idempotency-Key':name}},env);
 const data=await r.json();if(r.status!==200)throw new Error(JSON.stringify(data));writeFileSync(join(output,name+'.json'),JSON.stringify(data,null,2)+'\n');
};
await spin('10-offline-spin','/v1/offline-rewards/ad-intents/'+offline.intentId+'/spin');
await capture('11-offline-claimed','/v1/offline-rewards/ad-intents/'+offline.intentId);
f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES('test-run','a',100,'TEST_FIXTURE',0)");
f.sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at,finished_at) VALUES('test-run','a','test-run',1,'COMPLETED',0,1)");
f.sql.run("INSERT INTO run_final_results VALUES('test-run','VICTORY',15,'{}',1)");
const plan={calculation:{operationId:'opn_01_last_departure',outcome:'VICTORY'},rewardLines:[{kind:'CURRENCY',id:'cur_credits',quantity:240,source:'RUN_CLEAR'},{kind:'CURRENCY',id:'cur_credits',quantity:10,source:'FIRST_CLEAR'}]};
f.sql.run("INSERT INTO run_reward_previews(id,account_id,run_id,balance_config_version,status,reward_plan_json,expires_at,created_at) VALUES('test-preview','a','test-run',1,'ACTIVE',?,?,?)",[JSON.stringify(plan),Date.now()+3600000,Date.now()]);
const combat=await capture('12-combat-approved','/v1/ad-skip/uses/authorize',{opportunity:{kind:'COMBAT',runId:'test-run',rewardPreviewId:'test-preview'}}) as {intentId:string};
const path='/v1/runs/test-run/reward-preview/test-preview/ad-intents/'+combat.intentId;
await capture('13-combat-read',path);await spin('14-combat-spin',path+'/spin');await capture('15-combat-claimed',path);
await capture('16-reroll-unsupported','/v1/ad-skip/uses/authorize',{opportunity:{kind:'REROLL'}},503);
await revokeAdSkipOrder({db:env.DB,accountId:'a',environment:'DEV',verificationToken:'TEST_ONLY',verifier:async()=>({accountId:'a',environment:'DEV',provider:'GOOGLE_PLAY',appId:'test.app',transactionId:'TEST_ONLY_TRANSACTION',status:'REVOKED',verifiedAt:Date.now()})});
await capture('17-revoked','/v1/ad-skip');
f.sql.close();
console.log('Exported 17 actual local response fixtures to '+output);

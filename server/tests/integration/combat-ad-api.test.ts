import {publishConfiguredDisclosuresForTest} from '../helpers/published-disclosure-fixture';
import {createStartedOfflineAdIntent as createOfflineAdIntent, seedStartedAdForTest} from '../helpers/started-ad-fixture';
import { describe, expect, it,vi } from "vitest";
import { setup } from "../helpers/departure-fixture";
import app from "../../apps/game-api/src/index";
import { Hono } from "hono";
import { generateKeyPairSync,sign } from "node:crypto";
import { registerOfflineAdRoutes } from "../../apps/game-api/src/offline-ad-routes";
import {recordOfflineAdVerification } from "../../apps/game-api/src/offline-ad-service";
import { createAccessToken } from "../../apps/game-api/src/auth";
import { readFileSync,readdirSync } from "node:fs";
import { join } from "node:path";
import initSqlJs from "sql.js";

const policy={version:"TEST_COMBAT_ONLY",weightsBps:[6000,3000,1000],challengeLifetimeMs:60000,callbackGraceMs:60000,
  maxAdsPerDay:2,dayStartUtcMinutes:0,adUnit:"TEST_COMBAT_UNIT",appId:"TEST_APP",placement:"COMBAT_CLEAR",
  environment:"DEV",rewardItem:"TEST_REWARD",rewardAmount:"1"};
type Intent={intentId:string;userId:string;customData:string;createdAt:number;expiresAt:number};
async function configured(){
  const f=await fixture();
  f.sql.run("INSERT INTO combat_ad_policies VALUES(?,'DEV','ACTIVE',?)",[policy.version,JSON.stringify(policy)]);
  await publishConfiguredDisclosuresForTest(f.env.DB);
  const pair=generateKeyPairSync("ec",{namedCurve:"prime256v1"});
  const provider=new Hono<{Bindings:Env}>();
  registerOfflineAdRoutes(provider,async()=>({accountId:"a"}),async id=>id==="7"?pair.publicKey.export({type:"spki",format:"der"}).toString("base64"):null);
  const verify=(i:Intent,tx="test-transaction",tamper=false)=>{
    const raw=`ad_network=1&ad_unit=TEST_COMBAT_UNIT&custom_data=${encodeURIComponent(i.customData)}&reward_amount=1&reward_item=TEST_REWARD&timestamp=${i.createdAt}&transaction_id=${tx}&user_id=${i.userId}`;
    const url=`/v1/admob/ssv?${raw}&signature=${sign("sha256",Buffer.from(raw),pair.privateKey).toString("base64url")}&key_id=7`;
    return provider.request(tamper?url.replace("reward_amount=1","reward_amount=2"):url,{},f.env);
  };
  const create=async()=>{const response=await f.request(base,"POST","create");expect(response.status).toBe(200);const i=await response.json() as Intent;await seedStartedAdForTest(f.env.DB,'COMBAT',i.intentId);return i;};
  return {...f,create,verify};
}

const base = "/v1/runs/run-ad/reward-preview/preview-ad/ad-intents";
async function fixture() {
  const f=await setup();
  f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,idempotency_key,created_at) VALUES('ad-run-mut','a',100,'RUN_START','ad-run-start',0)");
  f.sql.run("INSERT INTO runs(id,account_id,mutation_id,balance_config_version,status,started_at,finished_at) VALUES('run-ad','a','ad-run-mut',1,'COMPLETED',0,1)");
  f.sql.run("INSERT INTO run_final_results VALUES('run-ad','VICTORY',15,'{}',1)");
  const plan={calculation:{operationId:"opn_01_last_departure",outcome:"VICTORY"},rewardLines:[
    {kind:"CURRENCY",id:"cur_credits",quantity:240,source:"RUN_CLEAR"},
    {kind:"CURRENCY",id:"cur_operator_exp",quantity:450,source:"RUN_CLEAR"},
    {kind:"CURRENCY",id:"cur_credits",quantity:10,source:"FIRST_CLEAR"},
  ]};
  f.sql.run("INSERT INTO run_reward_previews(id,account_id,run_id,balance_config_version,status,reward_plan_json,expires_at,created_at) VALUES('preview-ad','a','run-ad',1,'ACTIVE',?,?,?)",[JSON.stringify(plan),Date.now()+3600000,Date.now()]);
  const request=(path:string,method="GET",key="test-key")=>app.request(path,{method,headers:{...f.headers,"Idempotency-Key":key}},f.env);
  return {...f,request};
}
describe("combat clear ad routes",()=>{
  it("isolates roulette settlement keys from existing basic claim keys",async()=>{
    const f=await configured();try{
      const i=await f.create();await f.verify(i);
      f.sql.run("INSERT INTO idempotency_records(id,account_id,idempotency_key,operation,result_ref,created_at) VALUES('other-basic','a',?,'run_reward_claim','other-result',0)",["combat-ad:"+i.intentId]);
      expect((await f.request(`${base}/${i.intentId}/spin`,"POST","spin")).status).toBe(200);
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
    }finally{f.sql.close();}
  });
  it("denies another account, another environment and client-supplied multipliers",async()=>{
    const f=await configured();try{
      const i=await f.create();
      f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('session-b','b','hb','fb',0,'ACTIVE',?,?)",[Date.now(),Date.now()+60000]);
      const token=await createAccessToken({secret:"departure-test-secret-at-least-32-chars",accountId:"b",sessionId:"session-b",nowMs:Date.now(),ttlMs:60000});
      expect((await app.request(`${base}/${i.intentId}`,{headers:{Authorization:`Bearer ${token}`}},f.env)).status).toBe(403);
      expect((await app.request(`${base}/${i.intentId}`,{headers:f.headers},{...f.env,ENVIRONMENT:"PRD"})).status).toBe(403);
      expect((await f.post(`${base}/${i.intentId}/spin`,{multiplier:4},"bad")).status).toBe(400);
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
    }finally{f.sql.close();}
  });
  it("rejects expired callbacks and restores pending expiry without affecting basic rewards",async()=>{
    const f=await configured();try{
      const i=await f.create();
      f.sql.run("UPDATE auth_sessions SET expires_at=? WHERE id='s'",[Date.now()+600000]);
      f.headers.Authorization=`Bearer ${await createAccessToken({secret:"departure-test-secret-at-least-32-chars",accountId:"a",sessionId:"s",nowMs:Date.now(),ttlMs:600000})}`;
      const clock=vi.spyOn(Date,"now").mockReturnValue(i.expiresAt+policy.callbackGraceMs+1);
      try{
        expect((await f.verify(i)).status).toBe(409);
        expect(await (await f.request(`${base}/${i.intentId}`)).json()).toMatchObject({status:"EXPIRED",canSpin:false});
      }finally{clock.mockRestore();}
      expect((await f.post("/v1/runs/run-ad/reward-preview/preview-ad/claim",{runId:"run-ad",rewardPreviewId:"preview-ad"},"basic")).status).toBe(200);
    }finally{f.sql.close();}
  });
  it("restores the same creation under a new key and uses pinned policy after retirement",async()=>{
    const f=await configured();try{
      const i=await f.create();
      f.sql.run("UPDATE combat_ad_policies SET status='RETIRED'");
      expect(await (await f.request(base,"POST","another-key")).json()).toMatchObject({intentId:i.intentId});
      await f.verify(i);
      expect((await f.request(`${base}/${i.intentId}/spin`,"POST","spin")).status).toBe(200);
      expect(f.scalar("SELECT count(*) FROM combat_ad_intents")).toBe(1);
    }finally{f.sql.close();}
  });
  it("basic claim and spin race consume the preview once",async()=>{
    const f=await configured();try{
      const i=await f.create();await f.verify(i);
      await Promise.all([
        f.request(`${base}/${i.intentId}/spin`,"POST","spin"),
        f.post("/v1/runs/run-ad/reward-preview/preview-ad/claim",{runId:"run-ad",rewardPreviewId:"preview-ad"},"basic"),
      ]);
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
      const raw=f.scalar("SELECT reward_json FROM reward_transactions") as string;
      const plan=JSON.parse(raw);
      expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(plan.rewardLines.filter((l:{id:string})=>l.id==="cur_credits").reduce((sum:number,l:{quantity:number})=>sum+l.quantity,0));
    }finally{f.sql.close();}
  });
  it("does not pay a changed snapshot after a verified ad",async()=>{
    const f=await configured();try{
      const i=await f.create();await f.verify(i);
      f.sql.run("UPDATE run_reward_previews SET reward_plan_json='{}'");
      expect((await f.request(`${base}/${i.intentId}/spin`,"POST","spin")).status).toBe(409);
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
    }finally{f.sql.close();}
  });
  it("backfills provider ownership and fails migration on historical cross-consumer reuse",async()=>{
    const SQL=await initSqlJs(),sql=new SQL.Database();
    try{
      const dir="packages/db/migrations";
      for(const name of readdirSync(dir).filter(n=>/^\d+_.+\.sql$/u.test(n)&&n<"0037").sort())sql.run(readFileSync(join(dir,name),"utf8"));
      sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0)");
      sql.run("INSERT INTO ad_reward_transactions(id,account_id,provider,provider_transaction_id,intent_payload_hash,status,base_reward_json,created_at) VALUES('legacy','a','ADMOB','same','hash','VERIFIED','[]',0)");
      sql.run(`INSERT INTO offline_ad_intents(id,account_id,environment,create_key,baseline_ms,baseline_revision,created_at,expires_at,callback_deadline,policy_json,base_json,metadata_json,story_progress_id,narrative_version,user_binding,challenge,status,provider_transaction_id,verified_at)
        VALUES('off','a','DEV','k',0,0,0,100,200,'{}','[]','{}','none','v','u','c','AD_VERIFIED','same',1)`);
      const migration=readFileSync(join(dir,"0037_combat_ad_intents.sql"),"utf8");
      sql.run("BEGIN");expect(()=>sql.run(migration)).toThrow(/UNIQUE/);sql.run("ROLLBACK");
      sql.run("UPDATE offline_ad_intents SET provider_transaction_id='different'");
      sql.run(migration);
      expect(sql.exec("SELECT consumer,transaction_id FROM ad_provider_receipts ORDER BY consumer")[0]?.values).toEqual([["LEGACY","same"],["OFFLINE","different"]]);
    }finally{sql.close();}
  });
  it("verifies a signed callback, grants one total multiplier and restores exact result",async()=>{
    const f=await configured();try{
      const i=await f.create(),spin=()=>f.request(`${base}/${i.intentId}/spin`,"POST","spin");
      expect(i).toMatchObject({canClaimBasic:true,eligibleBaseRewardLines:[
        {id:"cur_credits",quantity:240},{id:"cur_operator_exp",quantity:450}],excludedBaseRewardLines:[{source:"FIRST_CLEAR",quantity:10}]});
      expect(await (await spin()).json()).toMatchObject({errorCode:"ad_verification_pending"});
      expect((await f.verify(i,"tx",true)).status).toBe(403);
      expect((await f.verify(i,"tx")).status).toBe(200);
      expect((await f.verify(i,"tx")).status).toBe(200);
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
      const response=await spin();expect(response.status).toBe(200);
      const result=await response.json();expect([2,3,4]).toContain(result.multiplier);
      expect(result.status).toBe("CLAIMED");
      expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(240*result.multiplier+10);
      expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='OPERATOR_EXP'")).toBe(450*result.multiplier);
      expect(await (await spin()).json()).toEqual(result);
      expect((await (await f.request(`${base}/${i.intentId}`)).json()).result).toEqual(result);
      expect(await (await f.request(`${base}/${i.intentId}`)).json()).toMatchObject({canClaimBasic:false,canSpin:false});
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
      expect(f.scalar("SELECT count(*) FROM ad_provider_receipts")).toBe(1);
    }finally{f.sql.close();}
  });
  it("does not multiply or repay after basic claim wins while callback is delayed",async()=>{
    const f=await configured();try{
      const i=await f.create();
      expect((await f.post("/v1/runs/run-ad/reward-preview/preview-ad/claim",{runId:"run-ad",rewardPreviewId:"preview-ad"},"basic")).status).toBe(200);
      expect((await f.verify(i)).status).toBe(200);
      expect((await f.request(`${base}/${i.intentId}/spin`,"POST")).status).toBe(409);
      expect(await (await f.request(`${base}/${i.intentId}`)).json()).toMatchObject({status:"REWARD_CONSUMED",canSpin:false});
      expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(250);
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
    }finally{f.sql.close();}
  });
  it("concurrent spin keys share the pinned result and grant once",async()=>{
    const f=await configured();try{
      const i=await f.create();await f.verify(i);
      const responses=await Promise.all([f.request(`${base}/${i.intentId}/spin`,"POST","one"),f.request(`${base}/${i.intentId}/spin`,"POST","two")]);
      expect(responses.map(r=>r.status)).toEqual([200,200]);
      const results=await Promise.all(responses.map(r=>r.json()));expect(results[0]).toEqual(results[1]);
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(1);
    }finally{f.sql.close();}
  });
  it("retains the chosen result across a failed payout and retries with no redraw",async()=>{
    const f=await configured();try{
      const i=await f.create();await f.verify(i);
      f.sql.run("CREATE TRIGGER fail_combat_payout BEFORE INSERT ON reward_transactions BEGIN SELECT RAISE(ABORT,'TEST_FAILURE'); END");
      expect((await f.request(`${base}/${i.intentId}/spin`,"POST","one")).status).toBe(503);
      const multiplier=f.scalar("SELECT multiplier FROM combat_ad_intents");expect([2,3,4]).toContain(multiplier);
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
      f.sql.run("DROP TRIGGER fail_combat_payout");
      expect(await (await f.request(`${base}/${i.intentId}/spin`,"POST","two")).json()).toMatchObject({multiplier,status:"CLAIMED"});
    }finally{f.sql.close();}
  });
  it.each(["offline-first","combat-first"])("blocks cross-placement provider replay: %s",async order=>{
    const f=await configured();try{
      const i=await f.create();
      const offlinePolicy={...policy,version:"TEST_OFFLINE",placement:"OFFLINE_SUPPLY",adUnit:"TEST_OFFLINE_UNIT"};
      const o=await createOfflineAdIntent(f.env.DB,"a","DEV",offlinePolicy,"offline");
      const callback={provider:"ADMOB" as const,transactionId:"shared-tx",timestamp:o.createdAt,userId:o.userId,customData:o.customData,adUnit:offlinePolicy.adUnit};
      if(order==="offline-first"){
        await recordOfflineAdVerification(f.env.DB,o.intentId,callback);
        expect((await f.verify(i,"shared-tx")).status).not.toBe(200);
      }else{
        expect((await f.verify(i,"shared-tx")).status).toBe(200);
        await expect(recordOfflineAdVerification(f.env.DB,o.intentId,callback)).rejects.toThrow();
      }
      expect(f.scalar("SELECT count(*) FROM ad_provider_receipts")).toBe(1);
      expect(f.scalar("SELECT count(*) FROM reward_transactions")).toBe(0);
    }finally{f.sql.close();}
  });
  it.each(["FAILURE","ABANDONED"])("rejects non-clear %s",async outcome=>{
    const f=await configured();try{
      f.sql.run("UPDATE run_final_results SET outcome=?",[outcome]);
      expect((await f.request(base,"POST")).status).toBe(409);
      expect(f.scalar("SELECT count(*) FROM combat_ad_intents")).toBe(0);
    }finally{f.sql.close();}
  });
  it("fails closed without operating policy and leaves normal claim usable",async()=>{
    const f=await fixture();
    try {
      const response=await f.request(base,"POST");
      expect(response.status).toBe(503);
      expect(await response.json()).toMatchObject({errorCode:"ad_reward_policy_unconfigured"});
      const claim=await f.post("/v1/runs/run-ad/reward-preview/preview-ad/claim",{runId:"run-ad",rewardPreviewId:"preview-ad"},"basic");
      expect(claim.status).toBe(200);
      expect(f.scalar("SELECT balance FROM account_currencies WHERE currency='CREDITS'")).toBe(250);
    } finally {f.sql.close();}
  });
});

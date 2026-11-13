import {canonicalJson,sha256Hex} from '@lastrail/common';
import {readDisclosureSource} from '../../apps/game-api/src/probability-disclosure-source';
import {parseOfflineRewardPolicy} from '../../apps/game-api/src/offline-ad-policy';
import {createOfflineAdIntent} from '../../apps/game-api/src/offline-ad-service';
import {createCombatAdIntent} from '../../apps/game-api/src/combat-ad-service';
/** Explicit TEST_ONLY approved-publication prerequisite for legacy SSV/economy tests.
 * Uses .run, not .batch, so the test's beforeBatch hook still targets the operation under test. */
export async function publishConfiguredDisclosuresForTest(db:D1Database){
 for(const consumer of ['OFFLINE','COMBAT'] as const){
  const table=consumer==='OFFLINE'?'offline_ad_policies':'combat_ad_policies';
  for(const sourceKind of ['AD_POLICY','PURCHASE_POLICY'] as const){
   const rows=sourceKind==='AD_POLICY'
    ?await db.prepare("SELECT environment,config_json FROM "+table+" WHERE status='ACTIVE'").all<{environment:'DEV'|'PRD';config_json:string}>()
    :await db.prepare('SELECT environment,config_json FROM purchase_reward_policies WHERE kind=?').bind(consumer).all<{environment:'DEV'|'PRD';config_json:string}>();
   for(const row of rows.results){
    const version=(JSON.parse(row.config_json) as {version:string}).version;
    const source=await readDisclosureSource(db,row.environment,consumer,version,sourceKind);
    const payload=canonicalJson({...source.projection,disclosureId:source.disclosureId,contractVersion:'probability-disclosure-v1',publishedAt:0});
    await db.prepare('INSERT OR IGNORE INTO probability_disclosures VALUES(?,?,?,?)').bind(source.disclosureId,row.environment,payload,await sha256Hex(payload)).run();
   }
  }
 }
}
export async function createPublishedOfflineForTest(...args:Parameters<typeof createOfflineAdIntent>){
 const [db,,environment,input]=args,p=parseOfflineRewardPolicy(input),raw=JSON.stringify(input);
 if(args[5]==='PURCHASE_ONLY'){
  await db.prepare("INSERT INTO purchase_reward_policies VALUES(?,'OFFLINE',?) ON CONFLICT(environment,kind) DO UPDATE SET config_json=excluded.config_json").bind(environment,raw).run();
 }else{
  await db.prepare("UPDATE offline_ad_policies SET status='RETIRED' WHERE environment=? AND version<>?").bind(environment,p.version).run();
  await db.prepare("INSERT INTO offline_ad_policies VALUES(?,?,'ACTIVE',?,0,'TEST_ONLY') ON CONFLICT(version) DO UPDATE SET config_json=excluded.config_json,status='ACTIVE'").bind(p.version,environment,raw).run();
 }
 await publishConfiguredDisclosuresForTest(db);
 return createOfflineAdIntent(...args);
}
export async function createPublishedCombatForTest(...args:Parameters<typeof createCombatAdIntent>){
 await publishConfiguredDisclosuresForTest(args[0]);
 return createCombatAdIntent(...args);
}

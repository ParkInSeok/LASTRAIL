import {expect,it} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {EventReplacementGrantStore} from '../../apps/game-api/src/event-replacement-grants';
import {eventCosmeticReplacementBundle,eventCosmeticReplacementVersion} from '../../apps/game-api/src/event-cosmetic-replacement';
import {readFileSync} from 'node:fs';
import {planCrystalChange} from '../../apps/game-api/src/crystal-wallet';
async function fixture(){
 const f=await setup();
 f.sql.run("INSERT INTO crystal_policies VALUES('event-wallet',?)",[JSON.stringify({currencyId:'TEST_CRYSTAL',ticketExchange:null,purchase:null})]);
 f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'event-wallet')");
 f.sql.run('CREATE TABLE local_event_original(id TEXT PRIMARY KEY,revision INTEGER NOT NULL)');
 f.sql.run("INSERT INTO local_event_original VALUES('earned-first-branch',1)");
 const context={environment:'DEV' as const,accountId:'a',roundId:'rerun-2',rewardRowId:'skin-goal'};
 const bundle=await eventCosmeticReplacementBundle(eventCosmeticReplacementVersion,'OPERATOR_SKIN');
 let reads=0;
 const source={prepare:async()=>{reads++;return {decision:{...context,branch:'REPLACEMENT',cosmeticId:'TEST_WHOLE_SKIN',
  cosmeticKind:'OPERATOR_SKIN',definitionHash:'a'.repeat(64),originalDecisionId:'earned-first-branch',
  tableVersion:bundle.tableVersion,tableHash:bundle.tableHash},assertions:[f.env.DB.prepare("INSERT INTO event_reward_write_guard SELECT CASE WHEN EXISTS(SELECT 1 FROM local_event_original WHERE id='earned-first-branch' AND revision=1) THEN 1 ELSE 0 END")],commitStatements:[]};}};
 return {...f,context,source,reads:()=>reads,store:new EventReplacementGrantStore(f.env.DB,'DEV',source)};
}
it('grants the exact whole replacement through existing wallets once and reuses its receipt without rereading ownership',async()=>{
 const f=await fixture();try{
  const result=await f.store.grant(f.context);
  expect(result).toMatchObject({accepted:true,branch:'REPLACEMENT',wallet:{freeGranted:1500,paidGranted:0},refreshRequired:false});
  expect(result.rewardLines).toHaveLength(4);
  expect(f.scalar("SELECT SUM(granted) FROM crystal_lots WHERE source_type='EVENT' AND source='FREE'")).toBe(1500);
  expect(f.scalar("SELECT balance FROM account_currencies WHERE account_id='a' AND currency='CREDITS'")).toBe(10000);
  f.sql.run('UPDATE local_event_original SET revision=2');
  expect(await f.store.grant(f.context)).toEqual(result);expect(f.reads()).toBe(1);
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(1);
 }finally{f.sql.close();}
});
it('requires an independent original decision and an active wallet policy before any effect',async()=>{
 const f=await fixture();try{
  await expect(new EventReplacementGrantStore(f.env.DB,'DEV').grant(f.context)).rejects.toThrow('event_replacement_original_owner_required');
  f.sql.run('DELETE FROM crystal_policy_active');
  await expect(f.store.grant(f.context)).rejects.toThrow('crystal_policy_unconfigured');
  expect(f.scalar('SELECT count(*) FROM event_reward_grants')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM reward_transactions')).toBe(0);
 }finally{f.sql.close();}
});
it('rolls back all lines on a withdrawn source and on a failed final receipt',async()=>{
 const f=await fixture();try{
  f.sql.run('UPDATE local_event_original SET revision=2');
  await expect(f.store.grant(f.context)).rejects.toThrow();
  expect(f.scalar('SELECT count(*) FROM crystal_lots')).toBe(0);
  f.sql.run('UPDATE local_event_original SET revision=1');
  f.sql.run("CREATE TRIGGER local_receipt_failure BEFORE INSERT ON event_reward_grants BEGIN SELECT RAISE(ABORT,'simulated_receipt_failure'); END;");
  await expect(f.store.grant(f.context)).rejects.toThrow('simulated_receipt_failure');
  expect(f.scalar('SELECT count(*) FROM crystal_lots')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM currency_ledger')).toBe(0);
 }finally{f.sql.close();}
});
it('converges concurrent claims and denies disabled accounts even for receipt recovery',async()=>{
 const f=await fixture();try{
  const results=await Promise.all([f.store.grant(f.context),f.store.grant(f.context)]);
  expect(results[0]).toEqual(results[1]);expect(f.scalar('SELECT count(*) FROM event_reward_grants')).toBe(1);
  f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");
  await expect(f.store.grant(f.context)).rejects.toThrow('event_replacement_account_forbidden');
 }finally{f.sql.close();}
});
it('preserves populated historical lots and ledger while enabling FREE-only EVENT provenance',async()=>{
 const f=await setup(false,true,true,'0136');try{
  f.sql.run("INSERT INTO crystal_policies VALUES('old-wallet',?)",[JSON.stringify({currencyId:'TEST_CRYSTAL',ticketExchange:null,purchase:null})]);
  f.sql.run("INSERT INTO crystal_policy_active VALUES(1,'old-wallet')");
  f.sql.run("INSERT INTO account_mutations(id,account_id,sequence,mutation_type,created_at) VALUES('old-mut','a',100,'TEST',0)");
  const old=await planCrystalChange({db:f.env.DB,accountId:'a',operationId:'old-claim',mutationId:'old-mut',operation:'TEST',now:0,
   grants:[{quantity:19,sourceType:'QUEST',sourceRef:'original-quest',orderId:null}]});
  await f.env.DB.batch(old.statements);
  const lots=f.sql.exec('SELECT * FROM crystal_lots'),ledger=f.sql.exec('SELECT * FROM crystal_ledger');
  f.sql.run('BEGIN');
  f.sql.run(readFileSync('packages/db/migrations/0137_event_crystal_source.sql','utf8'));f.sql.run('COMMIT');
  expect(f.sql.exec('SELECT * FROM crystal_lots')).toEqual(lots);
  expect(f.sql.exec('SELECT * FROM crystal_ledger')).toEqual(ledger);
  expect(f.sql.exec('PRAGMA foreign_key_check')).toEqual([]);
  expect(()=>f.sql.run("INSERT INTO crystal_lots SELECT 'illegal-event',account_id,currency_id,'PAID','EVENT',source_ref,NULL,granted,remaining,operation_id,created_at FROM crystal_lots LIMIT 1")).toThrow();
 }finally{f.sql.close();}
});
it('recovers the committed original after an acknowledgement is lost without replaying grant lines',async()=>{
 const f=await fixture();try{
  const originalBatch=f.env.DB.batch.bind(f.env.DB);let lose=true;
  f.env.DB.batch=async statements=>{const result=await originalBatch(statements);if(lose){lose=false;throw Error('lost_ack_after_commit');}return result;};
  const result=await f.store.grant(f.context);
  expect(result.accepted).toBe(true);
  expect(await f.store.grant(f.context)).toEqual(result);
  expect(f.reads()).toBe(1);expect(f.scalar('SELECT count(*) FROM crystal_lots')).toBe(1);
 }finally{f.sql.close();}
});
it('rejects mismatched original scope, changed table and unfenced sources without grants',async()=>{
 const f=await fixture();try{
  for(const change of [{accountId:'b'},{tableHash:'b'.repeat(64)}]){
   const source={prepare:async()=>{const value=await f.source.prepare();return {...value,decision:{...value.decision,...change}};}};
   await expect(new EventReplacementGrantStore(f.env.DB,'DEV',source).grant(f.context)).rejects.toThrow();
  }
  const unfenced={prepare:async()=>({...await f.source.prepare(),assertions:[]})};
  await expect(new EventReplacementGrantStore(f.env.DB,'DEV',unfenced).grant(f.context)).rejects.toThrow('original_fence_required');
  expect(f.scalar('SELECT count(*) FROM event_reward_grants')).toBe(0);
 }finally{f.sql.close();}
});
it('fences account suspension at the actual write boundary',async()=>{
 const f=await fixture();try{
  f.adapter.beforeBatch=()=>{f.adapter.beforeBatch=undefined;f.sql.run("UPDATE accounts SET status='SUSPENDED' WHERE id='a'");};
  await expect(f.store.grant(f.context)).rejects.toThrow('account_forbidden');
  expect(f.scalar('SELECT count(*) FROM crystal_lots')).toBe(0);
 }finally{f.sql.close();}
});

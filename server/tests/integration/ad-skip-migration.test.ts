import {it,expect} from 'vitest';
import initSqlJs from 'sql.js';
import {readFileSync,readdirSync} from 'node:fs';
import {join} from 'node:path';
it('rolls back an inconsistent approval migration and preserves historical receipts and fixed draws',async()=>{
 const SQL=await initSqlJs(),db=new SQL.Database(),dir='packages/db/migrations';db.run('PRAGMA foreign_keys=ON');
 for(const f of readdirSync(dir).filter(f=>/^\d+_.+\.sql$/u.test(f)&&f<'0040').sort())db.run(readFileSync(join(dir,f),'utf8'));
 db.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0)");
 db.run(`INSERT INTO offline_ad_intents(id,account_id,environment,create_key,baseline_ms,baseline_revision,created_at,expires_at,callback_deadline,
 policy_json,base_json,metadata_json,story_progress_id,narrative_version,user_binding,challenge,status,provider_transaction_id,verified_at,spin_key,response_json)
 VALUES('old','a','DEV','old',0,0,1,2,3,'{}','[]','{}','none','test','binding','challenge','CLAIMED','tx',2,'spin','{"historical":true}')`);
 db.run("INSERT INTO offline_ad_create_receipts VALUES('a','create','old')");
 db.run("INSERT INTO offline_ad_fixed_results VALUES('old',4,'{}','[]')");
 const before=db.exec('SELECT * FROM offline_ad_intents'),fixed=db.exec('SELECT * FROM offline_ad_fixed_results');
 const migration=readFileSync(join(dir,'0040_reward_purchase_approvals.sql'),'utf8');
 db.run('BEGIN');expect(()=>db.run(migration)).toThrow();db.run('ROLLBACK');
 expect(db.exec('SELECT * FROM offline_ad_intents')).toEqual(before);
 expect(db.exec("SELECT count(*) FROM sqlite_master WHERE name='reward_opportunity_approvals'")[0]!.values).toEqual([[0]]);
 db.run("INSERT INTO ad_provider_receipts VALUES('ADMOB','tx','OFFLINE','old','a')");
 db.run('BEGIN');try{db.run(migration);db.run('COMMIT');}catch(e){db.run('ROLLBACK');throw e;}
 expect(db.exec('SELECT response_json FROM offline_ad_intents')[0]!.values).toEqual([['{"historical":true}']]);
 expect(db.exec('SELECT * FROM offline_ad_fixed_results')).toEqual(fixed);
 expect(db.exec('PRAGMA foreign_key_check')).toEqual([]);
 expect(()=>db.run("UPDATE offline_ad_intents SET response_json='{}' WHERE id='old'")).toThrow('reward_writer_upgrade_required');
 expect(db.exec('SELECT cause FROM reward_opportunity_approvals')[0]!.values).toEqual([['AD_SSV']]);db.close();
});
it('preserves populated store history, references and rollback across permanent-kind migration',async()=>{
  const SQL=await initSqlJs(),db=new SQL.Database(),dir='packages/db/migrations';
  db.run('PRAGMA foreign_keys=ON');
  for(const f of readdirSync(dir).filter(f=>/^\d+_.+\.sql$/u.test(f)&&f<'0039').sort())db.run(readFileSync(join(dir,f),'utf8'));
  db.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0)");
  db.run("INSERT INTO verified_store_orders VALUES('o','a','GOOGLE_PLAY','app','product','tx','DEV','PASS','ref',0,0,NULL)");
  // Same FK shape used by existing pass/crystal references, with a populated parent.
  db.run('CREATE TABLE migration_reference(order_id TEXT NOT NULL REFERENCES verified_store_orders(id) ON DELETE RESTRICT)');
  db.run("INSERT INTO migration_reference VALUES('o')");
  const before=db.exec('SELECT * FROM verified_store_orders');
  const migration=readFileSync(join(dir,'0039_ad_skip_permanent.sql'),'utf8');
  db.run('BEGIN');
  try{db.run(migration);db.run('COMMIT');}catch(e){db.run('ROLLBACK');throw e;}
  expect(db.exec('SELECT * FROM verified_store_orders')).toEqual(before);
  expect(db.exec('PRAGMA foreign_key_check')).toEqual([]);
  db.run("INSERT INTO verified_store_orders VALUES('new','a','GOOGLE_PLAY','app','permanent','tx2','DEV','AD_SKIP_PERMANENT','ref',0,0,NULL)");
  expect(db.exec('SELECT count(*) FROM verified_store_orders')[0]!.values).toEqual([[2]]);
  const snapshot=db.export();
  db.run('BEGIN');
  try{db.run('DROP TABLE ad_skip_guard; CREATE TABLE rollback_probe(x); INSERT INTO missing_table VALUES(1)');}catch{db.run('ROLLBACK');}
  expect(db.exec("SELECT count(*) FROM sqlite_master WHERE name='rollback_probe'")[0]!.values).toEqual([[0]]);
  expect(db.exec("SELECT count(*) FROM sqlite_master WHERE name='ad_skip_guard'")[0]!.values).toEqual([[1]]);
  expect(snapshot.length).toBeGreaterThan(0);db.close();
});

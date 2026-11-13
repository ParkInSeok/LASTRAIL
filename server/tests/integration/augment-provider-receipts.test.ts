import {it,expect} from 'vitest';
import {readFileSync,readdirSync} from 'node:fs';
import initSqlJs from 'sql.js';
import {setup} from '../helpers/departure-fixture';
import {existingProviderReceipt,providerReceiptStatement} from '../../apps/game-api/src/ad-provider-receipts';
it('reserves augment receipts once and validates original intent/account replay',async()=>{
 const f=await setup(true);await f.env.DB.batch([providerReceiptStatement(f.env.DB,'tx','AUGMENT','intent','a')]);
 expect(await existingProviderReceipt(f.env.DB,'tx','AUGMENT','intent','a')).toBe(true);
 await expect(existingProviderReceipt(f.env.DB,'tx','AUGMENT','other','a')).rejects.toThrow('ad_reward_state_conflict');
 await expect(existingProviderReceipt(f.env.DB,'tx','AUGMENT','intent','b')).rejects.toThrow('ad_reward_state_conflict');
 await expect(f.env.DB.batch([providerReceiptStatement(f.env.DB,'tx2','AUGMENT','intent','a')])).rejects.toThrow();
 expect(f.scalar('SELECT count(*) FROM ad_provider_receipts')).toBe(1);
});
it.each(['LEGACY','OFFLINE','COMBAT'])('prevents reuse of %s receipts for augment approval',async consumer=>{
 const f=await setup(true);f.sql.run("INSERT INTO ad_provider_receipts VALUES('ADMOB','used',?,'old','a')",[consumer]);
 await expect(f.env.DB.batch([providerReceiptStatement(f.env.DB,'used','AUGMENT','new','a')])).rejects.toThrow();
 await expect(existingProviderReceipt(f.env.DB,'used','AUGMENT','new','a')).rejects.toThrow('ad_reward_state_conflict');
 expect(f.scalar('SELECT consumer FROM ad_provider_receipts')).toBe(consumer);
});
it.each(['OFFLINE','COMBAT'] as const)('prevents augment receipt reuse by %s',async consumer=>{
 const f=await setup(true);await f.env.DB.batch([providerReceiptStatement(f.env.DB,'used','AUGMENT','new','a')]);
 await expect(f.env.DB.batch([providerReceiptStatement(f.env.DB,'used',consumer,'other','a')])).rejects.toThrow();
 expect(f.scalar('SELECT consumer FROM ad_provider_receipts')).toBe('AUGMENT');
});
it('rolls back a provider reservation with a failed consumer transaction',async()=>{
 const f=await setup(true);await expect(f.env.DB.batch([providerReceiptStatement(f.env.DB,'tx','AUGMENT','intent','a'),f.env.DB.prepare('INSERT INTO solo_augment_write_guard VALUES(0)')])).rejects.toThrow();
 expect(f.scalar('SELECT count(*) FROM ad_provider_receipts')).toBe(0);
 await f.env.DB.batch([providerReceiptStatement(f.env.DB,'tx','AUGMENT','intent','a')]);expect(f.scalar('SELECT count(*) FROM ad_provider_receipts')).toBe(1);
});
it('preserves all existing receipt rows across migration and rollback',async()=>{
 const SQL=await initSqlJs(),sql=new SQL.Database(),dir='packages/db/migrations';try{
 sql.run('PRAGMA foreign_keys=ON');for(const file of readdirSync(dir).filter(n=>/^\d+_.+\.sql$/u.test(n)&&n<'0067').sort())sql.run(readFileSync(`${dir}/${file}`,'utf8'));
 sql.run("INSERT INTO accounts(id,created_at,updated_at) VALUES('a',0,0)");
 for(const consumer of ['LEGACY','OFFLINE','COMBAT'])sql.run("INSERT INTO ad_provider_receipts VALUES('ADMOB',?,?,?,'a')",[consumer,consumer,consumer]);
 const original=sql.exec('SELECT * FROM ad_provider_receipts ORDER BY consumer');const migration=readFileSync(`${dir}/0067_augment_provider_receipts.sql`,'utf8');
 sql.run('BEGIN');sql.run(migration);sql.run('ROLLBACK');expect(sql.exec('SELECT * FROM ad_provider_receipts ORDER BY consumer')).toEqual(original);
 expect(()=>sql.run("INSERT INTO ad_provider_receipts VALUES('ADMOB','new','AUGMENT','new','a')")).toThrow();
 sql.run('BEGIN');sql.run(migration);sql.run('COMMIT');expect(sql.exec('SELECT * FROM ad_provider_receipts ORDER BY consumer')).toEqual(original);
 sql.run("INSERT INTO ad_provider_receipts VALUES('ADMOB','new','AUGMENT','new','a')");expect(sql.exec('PRAGMA foreign_key_check')).toEqual([]);
 expect(()=>sql.run("INSERT INTO ad_provider_receipts VALUES('ADMOB','bad','AUGMENT','bad','missing')")).toThrow();
 }finally{sql.close();}
});

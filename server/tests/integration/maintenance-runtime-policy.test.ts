import {canonicalJson} from '@lastrail/common';
import {describe,it,expect} from 'vitest';
import {runtimeFixture} from '../helpers/maintenance-runtime-fixture';
describe('maintenance runtime storage',()=>{
 it('initializes explicitly, replays, transitions and preserves immutable history',async()=>{
  const f=await runtimeFixture(); const init={command:'INITIALIZE' as const,expectedRevision:null};
  const first=await f.store.transition(f.actor,init,'test','init');expect(first.policyRevision).toBe(1);
  expect(await f.store.transition(f.actor,init,'test','init')).toEqual(first);
  await f.store.transition(f.actor,{command:'ACTIVATE',expectedRevision:1,maintenanceId:'m',draftRevision:1},'test','start');
  expect(f.count('maintenance_runtime_revisions')).toBe(2);
  await expect(f.store.transition(f.actor,{command:'SCHEDULE',expectedRevision:2,maintenanceId:'m',draftRevision:1},'test','overlap')).rejects.toThrow();
  await f.store.transition(f.actor,{command:'COMPLETE',expectedRevision:2},'test','end');
  expect(f.count('maintenance_runtime_receipts')).toBe(3);
  expect(()=>f.sql.run('DELETE FROM maintenance_runtime_revisions')).toThrow();
  expect(()=>f.sql.run('UPDATE maintenance_runtime_heads SET revision=1')).toThrow();
 });
 it('rejects authority, wrong revision and key conflicts',async()=>{
  const f=await runtimeFixture();
  await expect(f.store.transition({...f.actor,role:'VIEWER'},{command:'INITIALIZE',expectedRevision:null},'test','init')).rejects.toThrow();
  await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'test','init');
  await expect(f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'other','init')).rejects.toThrow('idempotency_conflict');
  await expect(f.store.transition(f.actor,{command:'ACTIVATE',expectedRevision:8,maintenanceId:'m',draftRevision:1},'test','bad')).rejects.toThrow();
  expect(f.count('maintenance_runtime_revisions')).toBe(1);
 });
 it('rolls back head, revision and receipt on audit failure',async()=>{
  const f=await runtimeFixture();f.sql.run("CREATE TRIGGER reject_runtime_audit BEFORE INSERT ON admin_audit_logs WHEN NEW.action='MAINTENANCE_RUNTIME_TRANSITION' BEGIN SELECT RAISE(ABORT,'audit down'); END");
  await expect(f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'test','init')).rejects.toThrow();
  for(const table of ['maintenance_runtime_heads','maintenance_runtime_revisions','maintenance_runtime_receipts'])expect(f.count(table)).toBe(0);
 });
 it('checks current owner inside the write boundary',async()=>{
  const f=await runtimeFixture(),batch=f.db.batch.bind(f.db);
  f.db.batch=async statements=>{f.sql.run("UPDATE admin_principals SET status='DISABLED' WHERE id='owner'");return batch(statements);};
  await expect(f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'test','init')).rejects.toThrow();
  expect(f.count('maintenance_runtime_heads')).toBe(0);
 });
 it('recovers an INITIALIZE committed after first receipt lookup',async()=>{
  const f=await runtimeFixture(),prepare=f.db.prepare.bind(f.db);let once=true;
  f.db.prepare=(query:string)=>{const stmt=prepare(query);if(query.startsWith('SELECT r.*')){const first=stmt.first.bind(stmt);stmt.first=async <T>()=>{if(once){once=false;await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'test','init');}return first<T>();};}return stmt;};
  expect(await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'test','init')).toMatchObject({policyRevision:1});
  expect(f.count('maintenance_runtime_revisions')).toBe(1);
 });
 it('rejects a future immediate activation and changed source without partial history',async()=>{
  const f=await runtimeFixture();await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'test','init');
  f.sql.run("INSERT INTO maintenance_plan_revisions VALUES('DEV','future',1,?,0)",[canonicalJson({applicationId:'LASTRAIL',serviceScope:'GAME',webMode:null,title:'TEST',summary:'TEST',noticeId:null,drainStartsAt:Date.now()+60000,startsAt:Date.now()+120000,estimatedEndAt:null,existingRunPolicy:'DRAIN_THEN_PRESERVE',statusFreshnessSeconds:60,retryAfterSeconds:15})]);
  f.sql.run("INSERT INTO maintenance_plan_heads VALUES('DEV','future',1)");
  await expect(f.store.transition(f.actor,{command:'ACTIVATE',expectedRevision:1,maintenanceId:'future',draftRevision:1},'test','future')).rejects.toThrow();
  expect(f.count('maintenance_runtime_revisions')).toBe(1);
 });

});

import {canonicalJson} from '@lastrail/common';
import {describe,it,expect} from 'vitest';
import {runtimeFixture} from '../helpers/maintenance-runtime-fixture';
import {maintenanceAdmissionStatement,maintenanceContext,maintenanceAdmissionError} from '../../apps/game-api/src/maintenance-admission';
const context={environment:'DEV' as const,applicationId:'LASTRAIL'};
describe('atomic maintenance admission',()=>{
 it('fails closed on missing or wrong policy context and leaves no writes',async()=>{
  const f=await runtimeFixture();f.sql.run('CREATE TABLE probe(id TEXT)');
  const attempt=()=>f.db.batch([maintenanceAdmissionStatement(f.db,context),f.db.prepare("INSERT INTO probe VALUES('new')")]);
  await expect(attempt()).rejects.toThrow('maintenance_policy_unavailable');expect(f.count('probe')).toBe(0);
  await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'test','init');
  await attempt();expect(f.count('probe')).toBe(1);expect(f.count('maintenance_admission_guard')).toBe(0);
  await expect(f.db.batch([maintenanceAdmissionStatement(f.db,{...context,environment:'PRD'})])).rejects.toThrow('maintenance_policy_unavailable');
  expect(()=>maintenanceContext('LOCAL')).toThrow();
 });
 it('evaluates current head after a prior read and preserves atomic rollback',async()=>{
  const f=await runtimeFixture();await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'test','init');
  const stmt=maintenanceAdmissionStatement(f.db,context);
  await f.store.transition(f.actor,{command:'ACTIVATE',expectedRevision:1,maintenanceId:'m',draftRevision:1},'test','start');
  f.sql.run('CREATE TABLE probe(id TEXT)');
  await expect(f.db.batch([f.db.prepare("INSERT INTO probe VALUES('rollback')"),stmt])).rejects.toThrow('maintenance_new_request_blocked');
  expect(f.count('probe')).toBe(0);
  await f.store.transition(f.actor,{command:'COMPLETE',expectedRevision:2},'test','end');
  await f.db.batch([maintenanceAdmissionStatement(f.db,context)]);
 });
 it('maps only known errors, never treats unrelated DB failures as maintenance',()=>{
  expect(maintenanceAdmissionError(new Error('D1_ERROR: maintenance_new_request_blocked'))?.code).toBe('maintenance_new_request_blocked');
  expect(maintenanceAdmissionError(new Error('D1_ERROR: disk failure'))).toBeNull();
 });
 it('rejects a corrupt normal revision even if database checks were bypassed',async()=>{
  const f=await runtimeFixture();
  f.sql.run("PRAGMA ignore_check_constraints=ON; INSERT INTO maintenance_runtime_revisions VALUES('DEV','LASTRAIL',1.5,'NORMAL',NULL,NULL,NULL,NULL,NULL,NULL,'owner','TEST_ONLY',0); INSERT INTO maintenance_runtime_heads VALUES('DEV','LASTRAIL',1.5); PRAGMA ignore_check_constraints=OFF;");
  await expect(f.db.batch([maintenanceAdmissionStatement(f.db,context)])).rejects.toThrow('maintenance_policy_unavailable');
 });
 it('uses the DB clock at the exact scheduled cutoff and never auto completes',async()=>{
  const f=await runtimeFixture();await f.store.transition(f.actor,{command:'INITIALIZE',expectedRevision:null},'test','init');
  // Controlled SQLite clock, not a JavaScript Date mock. 2440588.5 is exactly one day.
  let julian=2440588.5;f.sql.create_function('julianday',(value:string)=>value==='now'?julian:NaN);
  const cutoff=86400000;
  f.sql.run("INSERT INTO maintenance_plan_revisions VALUES('DEV','clock',1,?,0)",[canonicalJson({applicationId:'LASTRAIL',serviceScope:'GAME',webMode:null,title:'TEST',summary:'TEST',noticeId:null,drainStartsAt:cutoff,startsAt:cutoff+1000,estimatedEndAt:cutoff+2000,existingRunPolicy:'DRAIN_THEN_PRESERVE',statusFreshnessSeconds:60,retryAfterSeconds:15})]);
  f.sql.run("INSERT INTO maintenance_plan_heads VALUES('DEV','clock',1)");
  await f.store.transition(f.actor,{command:'SCHEDULE',expectedRevision:1,maintenanceId:'clock',draftRevision:1},'test','clock');
  julian=2440588;expect(f.sql.exec("SELECT CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)")[0]!.values[0]![0]).toBe(43200000);await f.db.batch([maintenanceAdmissionStatement(f.db,context)]);
  julian=2440588.5;await expect(f.db.batch([maintenanceAdmissionStatement(f.db,context)])).rejects.toThrow('maintenance_new_request_blocked');
  julian=2440589.5;await expect(f.db.batch([maintenanceAdmissionStatement(f.db,context)])).rejects.toThrow('maintenance_new_request_blocked');
 });

});

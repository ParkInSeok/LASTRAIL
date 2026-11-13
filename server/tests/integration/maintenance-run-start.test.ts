import {describe,it,expect} from 'vitest';
import {setup} from '../helpers/departure-fixture';
import {testMaintenancePolicy} from '../helpers/maintenance-policy-fixture';
describe('maintenance START boundary',()=>{
 it.each([false,true])('blocks new START atomically, core=%s',async core=>{
  const f=await setup(core);const p=await (await f.preview()).json();
  if(!core)f.sql.run('UPDATE operation_previews SET admission_version=NULL');
  testMaintenancePolicy(f.sql,true);
  const result=await f.start(p.operationPreviewId,'new-start');expect(result.status).toBe(core?503:409);
  expect(await result.json()).toMatchObject({errorCode:core?'maintenance_new_request_blocked':'departure_quote_changed'});
  expect(f.scalar('SELECT count(*) FROM runs')).toBe(0);expect(f.scalar('SELECT fuel FROM fuel_state')).toBe(30);
  expect(f.scalar('SELECT status FROM operation_previews')).toBe('ACTIVE');
 });
 it('retains original successful result after maintenance begins',async()=>{
  const f=await setup(true);testMaintenancePolicy(f.sql);const p=await (await f.preview()).json();
  const first=await f.start(p.operationPreviewId,'same-start');expect(first.status).toBe(200);const expected=await first.json();
  testMaintenancePolicy(f.sql,true);
  expect(await (await f.start(p.operationPreviewId,'same-start')).json()).toEqual(expected);
  expect(f.scalar('SELECT count(*) FROM runs')).toBe(1);expect(f.scalar('SELECT fuel FROM fuel_state')).toBe(20);
 });
 it('checks policy switched immediately before commit',async()=>{
  const f=await setup(true);testMaintenancePolicy(f.sql);const p=await (await f.preview()).json();
  f.env.DB.beforeBatch=()=>{f.env.DB.beforeBatch=undefined;testMaintenancePolicy(f.sql,true);};
  expect((await f.start(p.operationPreviewId,'race-start')).status).toBe(503);
  expect(f.scalar('SELECT count(*) FROM fuel_ledger')).toBe(0);
 });
});

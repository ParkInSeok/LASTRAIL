import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { plan } from '../helpers/operation-map-fixture';
import { OperationMapStore } from '../../apps/game-api/src/operation-map-store';
import { OrdinaryNodeAttemptStore } from '../../apps/game-api/src/ordinary-node-attempt';
import app from '../../apps/game-api/src/index';
import { CombatHealthStore } from '../../apps/game-api/src/combat-health-store';

async function fixture(arrive = true) {
  const f = await setup(true), q = await (await f.preview()).json(), r = await (await f.start(q.operationPreviewId)).json();
  const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  snapshot.encounterType = 'ORDINARY_12_NODE'; snapshot.admission.operationMapPlan = plan();
  snapshot.departureResponse.encounterType = snapshot.encounterType; snapshot.departureResponse.admission.operationMapPlan = plan();
  f.sql.run('UPDATE run_snapshots SET snapshot_json=?', [JSON.stringify(snapshot)]);
  const map = new OperationMapStore(f.env.DB, { prepare: async ({ command }) => ({
    proof: { kind: command.kind, verificationId: 'synthetic', holdIds: [],
      ...('attemptGeneration' in command ? { attemptGeneration: command.attemptGeneration } : {}) },
    assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')],
  }) });
  await map.initialize('a', r.runId);
  await map.commit('a', r.runId, { kind: 'CONFIRM', eventId: 'choose', expectedRevision: 1, edgeId: 'entry:n1-0' });
  if (arrive) await map.commit('a', r.runId, { kind: 'ARRIVE', eventId: 'arrive', expectedRevision: 2, edgeId: 'entry:n1-0', evidenceRef: 'arrival' });
  // Synthetic node-entry owner; no claim of actual combat or six-actor producer verification.
  const initial = { node: 'n1-0', hp: [1300, 700, 850, 800, 900, 4200], gear: ['earned-before-node'], enemies: 'initial' };
  const attempts = new OrdinaryNodeAttemptStore(f.env.DB);
  const capture = () => attempts.capture('a', r.runId, 'n1-0', { prepare: async () => ({ snapshot: initial,
    assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')] }) });
  return { ...f, runId: r.runId, map, attempts, capture, initial };
}
const restart = { requestId: 'restart', nodeId: 'n1-0', expectedRevision: 3, expectedGeneration: 1 };
it('supplies current attempt through the existing map response without replaying an old restart or writing', async () => {
  const f = await fixture();
  const read = async () => {
    const response = await app.request(`/v1/runs/${f.runId}/operation-map`, { headers: f.headers }, f.env);
    expect(response.status).toBe(200);
    return response.json();
  };
  expect((await read()).currentAttempt).toBeNull();
  await f.capture();
  const first = await read();
  expect(first.currentAttempt).toEqual({ version: 'ordinary.current-attempt.v1.20261102',
    accountId: 'a', runId: f.runId, nodeId: 'n1-0', generation: 1, mapRevision: 3,
    snapshotHash: f.scalar('SELECT snapshot_hash FROM ordinary_node_attempts') });
  const old = await f.attempts.restart('a', f.runId, restart);
  await f.attempts.restart('a', f.runId, { ...restart, requestId: 'next', expectedRevision: 4, expectedGeneration: 2 });
  const before = f.sql.export();
  const current = await read();
  expect(current.currentAttempt).toEqual({ ...first.currentAttempt, generation: 3, mapRevision: 5 });
  expect(old.generation).toBe(2);
  expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  await expect(new OrdinaryNodeAttemptStore(f.env.DB).restart('a', f.runId, restart)).rejects.toThrow('restart_superseded');
  f.sql.run("UPDATE runs SET status='FINISH_PENDING'");
  expect((await read()).currentAttempt).toBeNull();
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await read()).toMatchObject({ runStatus: 'COMPLETED', currentAttempt: null });
});
it('preserves nonzero node cooldown steps across repeated restart without reconverting stored units', async () => {
  const f = await fixture();
  const snapshot = { ...f.initial, runtime: { actions: { version: 'ordinary.node-actions.v1.20261030',
    stepTimeSpanTicks: 500000, actors: [{ operatorId: 'actor', activeCooldownRemainingTicks: 80 }] } } };
  await f.attempts.capture('a', f.runId, 'n1-0', { prepare: async () => ({ snapshot,
    assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')] }) });
  const first = await f.attempts.restart('a', f.runId, restart);
  expect(first).toMatchObject({ status: 'RESTARTED', snapshot });
  expect(await f.attempts.restart('a', f.runId, restart)).toEqual(first);
  expect(await f.attempts.restart('a', f.runId, { ...restart, requestId: 'restart-again',
    expectedRevision: 4, expectedGeneration: 2 })).toMatchObject({ status: 'RESTARTED', snapshot });
});
it('issues the execution origin once at accepted arrival and reuses it on restart',async()=>{
  const f=await fixture(false);let issued:unknown;let calls=0;
  const map=new OperationMapStore(f.env.DB,{prepare:async({command})=>({
    proof:{kind:command.kind,verificationId:'synthetic-arrival',holdIds:[]},
    assertions:[f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')],
  })},{prepare:async context=>{
    calls++;issued=context.executionOrigin;
    expect(issued).toMatchObject({executionVersion:'combat.ordinary-five-execution.v1.20261028',
      determinism:{version:'ordinary.node-random.v1.20261029',algorithm:'XORSHIFT32_13_17_5_V1',seed:expect.any(Number),initialState:expect.any(Number)}});
    return {snapshot:{execution:issued},assertions:[f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')]};
  }});
  const command={kind:'ARRIVE',eventId:'seeded-arrival',expectedRevision:2,edgeId:'entry:n1-0',evidenceRef:'synthetic'};
  const receipt=await map.commit('a',f.runId,command);
  expect(await new OperationMapStore(f.env.DB).commit('a',f.runId,command)).toEqual(receipt);
  expect((await f.attempts.restart('a',f.runId,restart)).snapshot).toEqual({execution:issued});expect(calls).toBe(1);
});
it('requires original finish recovery while pending instead of claiming a final result or replaying a restart',async()=>{
  const f=await fixture();await f.capture();
  await f.attempts.restart('a',f.runId,restart);
  f.sql.run("UPDATE runs SET status='FINISH_PENDING'");
  await expect(f.attempts.restart('a',f.runId,restart)).rejects.toThrow('transaction_pending');
  await expect(f.attempts.restart('a',f.runId,{...restart,requestId:'another',expectedRevision:4,expectedGeneration:2})).rejects.toThrow('transaction_pending');
  const response=await app.request(`/v1/runs/${f.runId}/operation-map/restart`,{method:'POST',
    headers:{...f.headers,'Content-Type':'application/json','Idempotency-Key':'restart'},
    body:JSON.stringify({nodeId:'n1-0',expectedRevision:3,expectedGeneration:1})},f.env);
  expect(response.status).toBe(409);expect(await response.json()).toMatchObject({accepted:false,errorCode:'ordinary_node_transaction_pending'});
  expect(f.scalar('SELECT generation FROM ordinary_node_attempts')).toBe(2);
  expect(f.scalar('SELECT count(*) FROM ordinary_node_restart_receipts')).toBe(1);
});
it.each(['COMPLETED','FAILED','ABANDONED'])('prioritizes confirmed %s over saved restart and new completion',async status=>{
  const f=await fixture();await f.capture();await f.attempts.restart('a',f.runId,restart);
  f.sql.run('UPDATE runs SET status=?',[status]);
  expect(await f.attempts.restart('a',f.runId,restart)).toMatchObject({status:'RESULT_AVAILABLE',runStatus:status,generation:null});
  await expect(f.map.commit('a',f.runId,{kind:'COMPLETE',eventId:'late-complete',expectedRevision:4,
    nodeId:'n1-0',evidenceRef:'synthetic',attemptGeneration:2})).rejects.toThrow('run_not_active');
  expect(f.scalar('SELECT generation FROM ordinary_node_attempts')).toBe(2);
  expect(await f.map.lookup('a',f.runId,'late-complete')).toBeNull();
});
it('fences actual HP writes against node restart and preserves original receipt recovery', async () => {
  const f = await fixture(); await f.capture();
  // Synthetic damage proof exercises the real HP transaction, not a gameplay producer.
  const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [
    f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)'),
  ] }) });
  await health.initialize('a', f.runId, [{ id: 'actor', hp: 70, maxHp: 100 }]);
  const damage = { eventId: 'hit', expectedRevision: 1, targetId: 'actor',
    change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 25 }, attempt: { nodeId: 'n1-0', generation: 1 } };
  const unbound = { eventId: damage.eventId, expectedRevision: 1, targetId: damage.targetId, change: damage.change };
  await expect(health.commit('a', f.runId, unbound)).rejects.toThrow('attempt_required');
  const receipt = await health.commit('a', f.runId, damage);
  const pending = await health.prepareChange('a', f.runId, { ...damage, eventId: 'late', expectedRevision: 2,
    change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 0 } });
  await f.attempts.restart('a', f.runId, restart);
  await expect(f.env.DB.batch(pending.statements)).rejects.toThrow();
  expect(await health.commit('a', f.runId, damage)).toEqual(receipt);
  await expect(health.commit('a', f.runId, { ...damage, eventId: 'old', expectedRevision: 2 })).rejects.toThrow('attempt_conflict');
  expect((await health.read('a', f.runId)).state.targets[0].health.hp).toBe(25);
  expect(f.scalar("SELECT count(*) FROM combat_damage_receipts WHERE event_id='late'")).toBe(0);
  await health.commit('a', f.runId, { ...damage, eventId: 'current', expectedRevision: 2,
    attempt: { nodeId: 'n1-0', generation: 2 }, change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 0 } });
  await f.map.commit('a', f.runId, { kind: 'COMPLETE', eventId: 'done', expectedRevision: 4,
    attemptGeneration: 2, nodeId: 'n1-0', evidenceRef: 'synthetic' });
  await expect(health.commit('a', f.runId, { ...damage, eventId: 'after-result', expectedRevision: 3,
    attempt: { nodeId: 'n1-0', generation: 2 }, change: { kind: 'ACTUAL_HP', reason: 'HEAL', hp: 70 } })).rejects.toThrow('attempt_conflict');
});
it('rejects an unbound HP write prepared before the node origin was captured', async () => {
  const f = await fixture();
  const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [
    f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)'),
  ] }) });
  await health.initialize('a', f.runId, [{ id: 'actor', hp: 70, maxHp: 100 }]);
  const pending = await health.prepareChange('a', f.runId, { eventId: 'pre-entry-hit', expectedRevision: 1,
    targetId: 'actor', change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 25 } });
  await f.capture();
  await expect(f.env.DB.batch(pending.statements)).rejects.toThrow();
  expect((await health.read('a', f.runId)).state.targets[0].health.hp).toBe(70);
});
it('returns the exact node entry, never full HP or current damaged HP, and retries once', async () => {
  const f = await fixture(); await f.capture();
  const beforeFuel = f.scalar('SELECT count(*) FROM fuel_ledger');
  const receipt = await f.attempts.restart('a', f.runId, restart);
  expect(receipt).toMatchObject({ status: 'RESTARTED', revision: 4, generation: 2, snapshot: f.initial });
  expect(await f.attempts.restart('a', f.runId, restart)).toEqual(receipt);
  expect(f.scalar('SELECT count(*) FROM fuel_ledger')).toBe(beforeFuel);
  expect((await f.map.read('a', f.runId)).state.phase).toBe('ACTIVE');
  await expect(f.attempts.restart('a', f.runId, { ...restart, expectedGeneration: 2 })).rejects.toThrow('request_conflict');
});
it('never invents a missing node origin or accepts a foreign account', async () => {
  const f = await fixture();
  await expect(f.attempts.restart('a', f.runId, restart)).rejects.toThrow('snapshot_missing');
  await f.capture();
  await expect(f.attempts.restart('foreign', f.runId, restart)).rejects.toThrow('run_not_found');
});
it('fences old completion after restart, including a caller that changes only map revision', async () => {
  const f = await fixture(); await f.capture(); await f.attempts.restart('a', f.runId, restart);
  const complete = { kind: 'COMPLETE', eventId: 'done', expectedRevision: 4, nodeId: 'n1-0', evidenceRef: 'proof', attemptGeneration: 1 };
  await expect(f.map.commit('a', f.runId, complete)).rejects.toThrow('attempt_conflict');
  await f.map.commit('a', f.runId, { ...complete, attemptGeneration: 2 });
  const result = await f.attempts.restart('a', f.runId, { ...restart, requestId: 'later', expectedRevision: 5, expectedGeneration: 2 });
  expect(result.status).toBe('RESULT_AVAILABLE');
  expect((await f.attempts.restart('a', f.runId, restart)).status).toBe('RESULT_AVAILABLE');
});
it('rolls back generation if receipt storage fails and keeps the same original snapshot', async () => {
  const f = await fixture(); await f.capture();
  f.sql.run("CREATE TRIGGER fail_restart BEFORE INSERT ON ordinary_node_restart_receipts BEGIN SELECT RAISE(ABORT,'test failure'); END");
  await expect(f.attempts.restart('a', f.runId, restart)).rejects.toThrow('test failure');
  expect((await f.map.read('a', f.runId)).revision).toBe(3);
  expect(f.scalar('SELECT generation FROM ordinary_node_attempts')).toBe(1);
  f.sql.run('DROP TRIGGER fail_restart');
  expect((await f.attempts.restart('a', f.runId, restart)).generation).toBe(2);
});
it('lets completed state win a restart race without changing generation', async () => {
  const f = await fixture(); await f.capture();
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    const state = JSON.parse(String(f.scalar('SELECT state_json FROM operation_map_states'))); state.phase = 'READY';
    f.sql.run('UPDATE operation_map_states SET revision=4,state_json=?', [JSON.stringify(state)]);
  };
  await expect(f.attempts.restart('a', f.runId, restart)).rejects.toThrow('state_conflict');
  expect(f.scalar('SELECT generation FROM ordinary_node_attempts')).toBe(1);
  expect((await f.attempts.restart('a', f.runId, restart)).status).toBe('RESULT_AVAILABLE');
});
it('serves restart through actual authenticated route and excludes client snapshots', async () => {
  const f = await fixture(); await f.capture();
  const send = (body: unknown, headers = f.headers) => app.request(`/v1/runs/${f.runId}/operation-map/restart`, {
    method: 'POST', headers: { ...headers, 'Content-Type': 'application/json', 'Idempotency-Key': 'http-restart' }, body: JSON.stringify(body),
  }, f.env);
  const body = { nodeId: 'n1-0', expectedRevision: 3, expectedGeneration: 1 };
  expect((await send({ ...body, snapshot: f.initial })).status).toBe(400);
  expect((await send(body, {} as typeof f.headers)).status).toBe(401);
  const response = await send(body); expect(response.status).toBe(200);
  expect(response.headers.get('Cache-Control')).toBe('no-store');
  const receipt = await response.json(); expect(receipt).toMatchObject({ accepted: true, status: 'RESTARTED', generation: 2, snapshot: f.initial });
  expect(await (await send(body)).json()).toEqual(receipt);
});
it('rejects completion when restart changes the attempt after proof was prepared', async () => {
  const f = await fixture(); await f.capture();
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run('UPDATE ordinary_node_attempts SET generation=2');
    f.sql.run('UPDATE operation_map_states SET revision=4');
  };
  await expect(f.map.commit('a', f.runId, { kind: 'COMPLETE', eventId: 'old-complete', expectedRevision: 3,
    attemptGeneration: 1, nodeId: 'n1-0', evidenceRef: 'old-proof' })).rejects.toThrow('state_conflict');
  expect(await f.map.lookup('a', f.runId, 'old-complete')).toBeNull();
  expect((await f.map.read('a', f.runId)).state.phase).toBe('ACTIVE');
});
it('commits verified arrival, node origin, generation and response together or none', async () => {
  const f = await fixture(false); let captures = 0;
  const map = new OperationMapStore(f.env.DB, { prepare: async ({ command }) => ({
    proof: { kind: command.kind, verificationId: 'synthetic-arrival', holdIds: [] },
    assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')],
  }) }, { prepare: async context => {
    captures++; expect(context).toMatchObject({ nodeId: 'n1-0', mapRevision: 3 });
    return { snapshot: f.initial, assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')] };
  } });
  const command = { kind: 'ARRIVE', eventId: 'atomic-entry', expectedRevision: 2, edgeId: 'entry:n1-0', evidenceRef: 'arrival' };
  f.sql.run("CREATE TRIGGER fail_arrival BEFORE INSERT ON operation_map_receipts BEGIN SELECT RAISE(ABORT,'arrival failure'); END");
  await expect(map.commit('a', f.runId, command)).rejects.toThrow('arrival failure');
  expect(f.scalar('SELECT count(*) FROM ordinary_node_attempts')).toBe(0);
  expect((await map.read('a', f.runId)).revision).toBe(2);
  f.sql.run('DROP TRIGGER fail_arrival');
  const receipt = await map.commit('a', f.runId, command);
  expect(receipt).toMatchObject({ revision: 3, nodeStart: { generation: 1, nodeId: 'n1-0', snapshot: f.initial } });
  expect(captures).toBe(2);
  expect(await new OperationMapStore(f.env.DB).commit('a', f.runId, command)).toEqual(receipt);
  expect(captures).toBe(2);
  expect((await f.attempts.restart('a', f.runId, restart)).snapshot).toEqual(f.initial);
});

import { expect, it } from 'vitest';
import { createServer } from 'node:http';
import { OrdinaryObstacleSequence, ordinaryObstacleDefinition } from '../../apps/game-api/src/ordinary-obstacle-sequence';
import { readOrdinaryNodeEntryOwners, OrdinaryNodeEntrySource } from '../../apps/game-api/src/ordinary-node-entry-source';
import { prepareOrdinaryNodeEnemies } from '../../apps/game-api/src/ordinary-node-enemies';
import { cooldownNodeStepsToTimeSpan } from '../../apps/game-api/src/ordinary-node-actions';
import { OrdinaryNativeGeometryRegistry } from '../../apps/game-api/src/ordinary-native-geometry';
import { readOrdinaryRunAutomation } from '../../apps/game-api/src/ordinary-run-automation';
import { issueOrdinaryNodeExecutionOrigin, ordinaryNodeExecutionOriginSchema } from '../../apps/game-api/src/ordinary-node-execution-origin';
import { readOrdinaryNodeActions } from '../../apps/game-api/src/ordinary-node-actions';
import { Hono } from 'hono';
import { registerOperationMapCommandRoutes } from '../../apps/game-api/src/operation-map-command-routes';
import { captureFirstBattleEncounterSource, captureFirstBattleEncounterSourceV2, captureFirstBattleEncounterSourceV3, captureFirstBattleEncounterSourceV4, captureFirstBattleEncounterSourceV5, captureFirstBattleEncounterSourceV6, captureFirstBattleEncounterSourceV7, readFirstBattleEncounterFromOriginal } from '../../apps/game-api/src/combat-first-battle-source';
import { firstEnemyTrainInstanceId, firstEnemyTrainBodyDefinition } from '../../apps/game-api/src/combat-first-enemy-spatial';
import { captureFirstBattleEncounterSourceV8, firstBattleStaticDefinition, resolveFirstBattleProfile } from '../../apps/game-api/src/combat-first-battle-source';
import { assertOrdinaryStartResponseSize } from '../../apps/game-api/src/departure-start-size';
import { firstPlayableProfileV3 } from '../../apps/game-api/src/combat-first-playable-profile';
import { plan as mapFixture } from '../helpers/operation-map-fixture';
import { OperationMapPublisher } from '../../apps/game-api/src/operation-map-publisher';
import { OperationMapStore } from '../../apps/game-api/src/operation-map-store';
import { captureOrdinaryTrainSource, readOrdinaryTrainFromOriginal } from '../../apps/game-api/src/combat-ordinary-train-source';
import { fork } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdtempSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { buildSync } from 'esbuild';
import initSqlJs from 'sql.js';
import { SqlJsD1Database } from '../helpers/growth-d1';
import { assertClientFinishAuthority } from '../../apps/game-api/src/combat-finish-authority';
import { captureOrdinaryRailSource, captureOrdinaryRailSourceV2, captureOrdinaryRailSourceV3, captureOrdinaryRailSourceV4, captureOrdinaryFirstPlayerRailDefinition, readOrdinaryRailFromOriginal } from '../../apps/game-api/src/combat-ordinary-rail-source';
import { captureOrdinaryTrialPhysicalDefinition } from '../../apps/game-api/src/combat-ordinary-physical-source';
import { captureOrdinaryRoofDefinition, captureOrdinaryRoofSource, readOrdinaryRoofFromOriginal } from '../../apps/game-api/src/combat-ordinary-roof-source';
import { ce03ProducerFixture } from '../helpers/ce03-producer-fixture';
import { startDeparture } from '../../apps/game-api/src/departure';
import { readOrdinaryRunStatOrigin } from '../../apps/game-api/src/combat-ordinary-run-source';
import { verifyOrdinarySupportAttribution } from '../../apps/game-api/src/ordinary-support-attribution';
import { OrdinaryRunStatStore } from '../../apps/game-api/src/combat-ordinary-stat-store';
import { CombatHealthStore } from '../../apps/game-api/src/combat-health-store';
import { readOrdinaryInitialModifiers } from '../../apps/game-api/src/combat-ordinary-initial-modifiers';
import { OrdinaryCombatLifecycle, projectOrdinaryInitialization } from '../../apps/game-api/src/combat-ordinary-combat-lifecycle';
import { EquipmentEffectOriginalStore } from '../../apps/game-api/src/equipment-effect-original';
import { readOrdinaryRunKitSource } from '../../apps/game-api/src/combat-ordinary-kit-source';
import app from '../../apps/game-api/src/index';
import { OrdinaryFirstObjectiveVerifier, firstObjectiveEncounterIdentities } from '../../apps/game-api/src/combat-first-objective-verifier';
import { firstEncounterObjectiveDefinition } from '../../apps/game-api/src/combat-first-encounter-objective';
import { createAccessToken } from '../../apps/game-api/src/auth';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { captureOrdinaryBasicSource, captureOrdinaryBasicSourceV2, readOrdinaryRunBasicSource } from '../../apps/game-api/src/combat-ordinary-basic-source';
import { captureOrdinaryRelocationSource, captureOrdinaryRelocationSourceV2, captureOrdinaryRelocationSourceV3, captureOrdinaryRelocationSourceV4, captureOrdinaryRelocationSourceV5, readOrdinaryRunRelocationSource } from '../../apps/game-api/src/combat-ordinary-relocation-source';

it.each([
  { lostResponse: false, progress: 'none' }, { lostResponse: true, progress: 'none' },
  { lostResponse: false, progress: 'damaged' }, { lostResponse: true, progress: 'corrupt' },
  { lostResponse: false, progress: 'damaged', historicalRail: true },
  { lostResponse: true, progress: 'corrupt', historicalRail: true },
])('recovers initialization over actual TCP after process exit ($lostResponse, $progress, $historicalRail)', async ({ lostResponse, progress, historicalRail }) => {
  const f = await fixture(true);
  const directory = mkdtempSync('D:/Project/LASTRAIL/LocalTemp/ordinary-initialization-tcp-');
  const database = join(directory, 'trial.sqlite');
  const probe = join(directory, 'probe.cjs');
  const processes: ReturnType<typeof launch>[] = [];
  type Event = { kind: string; pid: number; requests?: number; baseUrl?: string; responseHash?: string; unchanged?: boolean;
    initializationReceipts?: number; statRows?: number; finalRows?: number; method?: string; status?: number };
  function launch(runId: string, dropResponse: boolean) {
    const child = fork(probe, [], {
      execArgv: [], silent: true, windowsHide: true,
      env: { ...process.env, NODE_PATH: resolve('node_modules') },
    });
    const events: Event[] = [];
    child.on('message', value => events.push(value as Event));
    child.stdout?.resume(); child.stderr?.resume();
    const exited = new Promise<void>(done => child.once('exit', () => done()));
    const next = async (kind: string, index = 0) => {
      const until = Date.now() + 20000;
      while (Date.now() < until) {
        const value = events.filter(event => event.kind === kind)[index];
        if (value) return value;
        if (events.some(event => event.kind === 'ERROR') || child.exitCode !== null) throw new Error('probe_failed');
        await new Promise(done => setTimeout(done, 20));
      }
      throw new Error('probe_timeout');
    };
    const stop = async () => { if (child.exitCode === null && child.signalCode === null) child.kill(); await exited; };
    child.send({ path: database, runId, secret: f.env.AUTH_SIGNING_KEY_CURRENT, dropResponse });
    const handle = { next, stop }; processes.push(handle); return handle;
  }
  try {
    buildSync({ entryPoints: [resolve('tests/helpers/ordinary-initialization-tcp-probe.ts')], outfile: probe,
      bundle: true, platform: 'node', format: 'cjs', packages: 'external', tsconfig: resolve('tsconfig.base.json') });
    const started = await f.startCore(), runId = String(started.runId);
    if (historicalRail) {
      // Disposable v3 QA original, not a migration of a product player's saved run.
      const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId), p = started.ordinaryRailSource.packet;
      started.ordinaryRailSource = await captureOrdinaryRailSourceV3({ accountId: p.accountId, runId,
        trainId: p.trainId, ordinaryOriginHash: p.ordinaryOriginHash, ownedSourceHash: original.origin.ownedSourceHash,
        loadoutRevision: p.loadoutRevision, selectedCarriages: p.selectedCarriages });
      started.ordinaryRoofSource = await captureOrdinaryRoofSource(original.origin, original.originHash, started.ordinaryRailSource);
      const snapshot = JSON.parse(original.startSnapshotJson);
      snapshot.ordinaryRailSource = started.ordinaryRailSource;
      snapshot.ordinaryRoofSource = started.ordinaryRoofSource;
      const bytes = JSON.stringify(snapshot);
      f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [bytes, runId]);
      f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
      f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, bytes]);
    }
    writeFileSync(database, f.sql.export());
    const first = launch(runId, lostResponse), ready = await first.next('READY');
    const post = (base: string) => fetch(`${base}/v1/runs/${runId}/ordinary-initialization`, {
      method: 'POST', headers: { ...f.headers, 'Idempotency-Key': 'tcp-original' }, body: '{}', signal: AbortSignal.timeout(10000) });
    let normalHash: string | undefined;
    if (lostResponse) await expect(post(ready.baseUrl!)).rejects.toThrow();
    else {
      const response = await post(ready.baseUrl!); expect(response.status).toBe(200);
      normalHash = createHash('sha256').update(canonicalJson(await response.json())).digest('hex');
    }
    const original = await first.next('REQUEST');
    expect(original).toMatchObject({ requests: 1, status: 200, initializationReceipts: 1, statRows: 1, finalRows: 0, unchanged: false });
    if (normalHash) expect(original.responseHash).toBe(normalHash);
    await first.stop();
    let progressBytes: Buffer | undefined;
    if (progress !== 'none') {
      const SQL = await initSqlJs(), saved = new SQL.Database(readFileSync(database));
      saved.create_function('unixepoch', () => Date.now() / 1000);
      const db = new SqlJsD1Database(saved) as unknown as D1Database;
      try {
        // Synthetic damage proof supplies only the event; the production HP owner persists it.
        const health = new CombatHealthStore(db, { prepare: async () => ({
          assertions: [db.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
        await health.commit('a', runId, { eventId: 'tcp-train-damage', expectedRevision: 1,
          targetId: started.ordinaryTrainSource.packet.train.trainId,
          change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 4900 } });
        if (progress === 'corrupt') saved.run("UPDATE combat_damage_states SET state_json='{}'");
        progressBytes = Buffer.from(saved.export()); writeFileSync(database, progressBytes);
      } finally { saved.close(); }
    }
    const second = launch(runId, false), restored = await second.next('READY');
    expect(restored.pid).not.toBe(ready.pid);
    const response = await post(restored.baseUrl!); expect(response.status).toBe(200);
    const text = await response.text(), receipt = JSON.parse(text);
    expect(createHash('sha256').update(canonicalJson(receipt)).digest('hex')).toBe(original.responseHash);
    expect(receipt).toMatchObject({ accepted: true, runtimeExecutionVerified: false,
      initializationReceipt: { ordinaryRelocationBinding: { version: 'combat.ordinary-relocation.v7.20261102' } } });
    expect(receipt.initializationReceipt.ordinaryRailBinding).toMatchObject({
      version: historicalRail ? 'combat.ordinary-rail-source.v3.20261027' : 'combat.ordinary-rail-source.v4.20261028',
      sourceHash: started.ordinaryRailSource.sourceHash });
    expect(receipt.initializationReceipt.ordinaryRoofBinding.sourceHash).toBe(started.ordinaryRoofSource.sourceHash);
    const recovered = await second.next('REQUEST');
    expect(recovered).toMatchObject({ requests: 1, status: 200, initializationReceipts: 1, statRows: 1, finalRows: 0, unchanged: true });
    await second.stop();
    if (progressBytes) {
      expect(readFileSync(database).equals(progressBytes)).toBe(true);
      const SQL = await initSqlJs(), saved = new SQL.Database(readFileSync(database));
      try {
        const health = new CombatHealthStore(new SqlJsD1Database(saved) as unknown as D1Database);
        if (progress === 'corrupt') await expect(health.read('a', runId)).rejects.toThrow('combat_health_storage_invalid');
        else expect((await health.read('a', runId)).state.targets.find(t => t.id === started.ordinaryTrainSource.packet.train.trainId)!.health.hp).toBe(4900);
        expect(receipt.initializationReceipt.health.state.targets.find((t: { id: string }) => t.id === started.ordinaryTrainSource.packet.train.trainId).health.hp).toBe(5000);
      } finally { saved.close(); }
    }
    const sources = ['apps/game-api/src/index.ts', 'apps/game-api/src/combat-ordinary-initialization-routes.ts',
      'apps/game-api/src/combat-ordinary-combat-lifecycle.ts', 'apps/game-api/src/combat-ordinary-relocation-source.ts'];
    writeFileSync(`D:/Project/LASTRAIL/QA/CommanderS1-20260922/ordinary-initialization-tcp-${historicalRail ? 'historical-v3-' : ''}${progress === 'none' ? lostResponse ? 'lost' : 'normal' : progress}-20261026.json`, JSON.stringify({
      classification: 'SYNTHETIC_INTERNAL_START_AUTHENTICATED_PRODUCTION_HANDLER_REAL_TCP_SQLJS_FILE_RESTART_NOT_PLAYER',
      logicalInitializationCommands: 1, transportPosts: 2, automaticRetries: 0, recoveryPosts: 1, getRequests: 0,
      lostResponse, progress, progressStorageUnchanged: progressBytes ? true : null,
      railBinding: receipt.initializationReceipt.ordinaryRailBinding, roofBinding: receipt.initializationReceipt.ordinaryRoofBinding,
      historicalRail: historicalRail === true, sourceGeometry: started.ordinaryRailSource.packet.geometry,
      sourceInitialMotion: started.ordinaryRailSource.packet.initialMotion, migrationPerformed: false,
      damageVerification: progressBytes ? 'SYNTHETIC_TEST_PROOF_NOT_PRODUCT_HIT' : 'NOT_APPLICABLE',
      processIds: [ready.pid, restored.pid], distinctProcesses: true,
      original, recovered, basicBinding: receipt.initializationReceipt.ordinaryBasicBinding,
      relocationBinding: receipt.initializationReceipt.ordinaryRelocationBinding,
      sourceHashes: Object.fromEntries(sources.map(path => [path, createHash('sha256').update(readFileSync(path)).digest('hex')])),
      database: 'DISPOSABLE_SQLJS_FILE_NOT_CLOUDFLARE_D1', memory: 'NOT_MEASURED', player: 'NOT_RUN',
    }, null, 2));
  } finally { for (const process of processes) await process.stop(); f.sql.close(); }
}, 60000);

it('initializes through authenticated original-START command once and recovers its original receipt', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const request = (key = 'initialize-original', body = {}, headers = f.headers, id = runId) => app.request(
      `http://localhost/v1/runs/${id}/ordinary-initialization`, { method: 'POST',
        headers: { ...headers, 'Idempotency-Key': key }, body: JSON.stringify(body) }, f.env);
    const now = Date.now();
    f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('sb','b','hb','fb',0,'ACTIVE',?,?)", [now, now + 60000]);
    const foreign = { ...f.headers, Authorization: `Bearer ${await createAccessToken({ secret: f.env.AUTH_SIGNING_KEY_CURRENT,
      accountId: 'b', sessionId: 'sb', nowMs: now, ttlMs: 60000 })}` };
    const before = f.sql.export();
    expect((await request('', {})).status).toBe(400);
    expect((await request('bad-hp', { initialTargets: [] })).status).toBe(400);
    expect((await request('bad-ready', { canStart: true })).status).toBe(400);
    expect((await request('no-auth', {}, {} as typeof f.headers)).status).toBe(401);
    expect((await request('missing-run', {}, f.headers, 'missing')).status).toBe(404);
    const denied = await request('foreign', {}, foreign);
    expect(denied.status).toBe(404);expect(await denied.json()).toEqual({ accepted: false, errorCode: 'run_not_found' });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    const batch = f.env.DB.batch.bind(f.env.DB);
    f.env.DB.batch = async statements => { await batch(statements); throw new Error('test-only initialization ACK lost'); };
    const [first, concurrent] = await Promise.all([request(), request()]);
    f.env.DB.batch = batch;
    expect(first.status).toBe(200);
    const body = await first.json();
    expect(concurrent.status).toBe(200);expect(await concurrent.json()).toEqual(body);
    expect(body).toMatchObject({ accepted: true, runtimeExecutionVerified: false,
      initializationReceipt: { runId, phase: 'RUNNING', ordinaryBasicBinding: { version: 'combat.ordinary-basic.v3.20261026' },
        ordinaryRelocationBinding: { version: 'combat.ordinary-relocation.v7.20261102' } } });
    const after = f.sql.export();
    const initialization = body.initializationReceipt;
    expect(started.ordinaryRailSource.packet.definitionVersion).toBe('combat.ordinary-rail-trial.v3.20261028');
    expect(started.ordinaryRailSource.packet.definitionHash).toMatch(/^[a-f0-9]{64}$/u);
    expect(started.ordinaryRailSource.packet).not.toHaveProperty('definition');
    expect(started.ordinaryRailSource.packet.geometry).toMatchObject({
      version: 'ordinary.finite-rail-authoring.v2.20261028', railSegmentId: 'ordinary-first-straight-v2' });
    expect(started.ordinaryRailSource.packet.physicalConsist).toMatchObject({
      version: 'ordinary.trial-consist-authoring.v1.20261027', totalLengthMm: 38000,
      initialTailArcMm: 20000, initialFrontArcMm: 58000 });
    const vehicleIds = started.ordinaryRailSource.packet.physicalConsist.instanceIds;
    expect(vehicleIds).toHaveLength(3); expect(new Set(vehicleIds).size).toBe(3);
    expect(vehicleIds).not.toContain(started.ordinaryTrainSource.packet.train.trainId);
    expect(started.ordinaryRailSource.packet.initialMotion).toEqual({ speedMmPerSecond: 20000, accelerationMmPerSecondSquared: 0 });
    expect(started.ordinaryRailSource.packet.selectedCarriages).toHaveLength(2);
    expect(initialization.ordinaryRailBinding).toEqual({ version: started.ordinaryRailSource.packet.version,
      ordinaryOriginHash: started.ordinaryRailSource.packet.ordinaryOriginHash, sourceHash: started.ordinaryRailSource.sourceHash });
    expect(started.ordinaryRoofSource.packet).toMatchObject({ version: 'combat.ordinary-roof-source.v1.20261027',
      definitionVersion: 'combat.ordinary-roof-layout.v1.20261027', railSourceHash: started.ordinaryRailSource.sourceHash,
      status: 'LAYOUT_BOUND_ANCHORS_UNVERIFIED',
      operatorIds: [...started.ordinaryStatSource.origin.actors].sort((a, b) => a.slot - b.slot).map(a => a.operatorId) });
    expect(initialization.ordinaryRoofBinding).toEqual({ version: started.ordinaryRoofSource.packet.version,
      ordinaryOriginHash: started.ordinaryStatSource.originHash, sourceHash: started.ordinaryRoofSource.sourceHash });
    expect(Buffer.byteLength(JSON.stringify(started))).toBeLessThanOrEqual(65536);
    expect(initialization.trainInitialState).toMatchObject({
      version: 'combat.ordinary-train-initial-state.v1.20261027', accountId: 'a', runId,
      trainId: started.ordinaryTrainSource.packet.train.trainId, healthRevision: 1,
      healthTargetId: started.ordinaryTrainSource.packet.train.trainId,
      activeShields: [], stage: 'INITIAL_BEFORE_COMBAT', logicalTimeMs: 0,
    });
    expect(initialization.health.state.targets).toHaveLength(6);
    expect(initialization.health.state.targets.find((t: { id: string }) => t.id === initialization.trainInitialState.healthTargetId).health)
      .toMatchObject({ hp: 5000, maxHp: 5000 });
    expect((await new CombatHealthStore(f.env.DB).read('a', runId))).toEqual(initialization.health);
    expect(initialization.ordinaryTrainBinding).toEqual({ version: started.ordinaryTrainSource.packet.version,
      ordinaryOriginHash: started.ordinaryTrainSource.packet.ordinaryOriginHash, sourceHash: started.ordinaryTrainSource.sourceHash });
    expect(initialization.firstBattleEncounterBinding).toEqual({
      version: started.firstBattleEncounterSource.packet.version,
      originHash: started.firstBattleEncounterSource.packet.originHash,
      sourceHash: started.firstBattleEncounterSource.sourceHash,
    });
    expect(initialization.sourceState).toBeDefined();
    expect(await sha256Hex(canonicalJson(initialization.sourceState))).toBe(initialization.stats.sourceHash);
    expect(initialization.sourceState).toMatchObject({ accountId: 'a', runId, revision: 1 });
    expect(initialization.sourceState.actors).toHaveLength(5);
    const replay = await request();
    expect(replay.status).toBe(200);expect(await replay.json()).toEqual(body);
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(after))).toBe(true);
    expect((await request('another-initialization')).status).toBe(409);
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_combat_activation_receipts')).toBe(1);
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_run_stat_states')).toBe(1);
    expect(f.scalar('SELECT COUNT(*) FROM run_final_results')).toBe(0);
    writeFileSync(resolve(process.cwd(), '../../../../../QA/CommanderS1-20260922/ordinary-initialization-consumer-source-20261026.json'),
      canonicalJson({ classification: 'SYNTHETIC_ACCOUNT_INTERNAL_START_PRODUCTION_INITIALIZATION_HTTP_NOT_PRODUCT_ADMISSION',
        accountId: 'a', runId, originalStart: started, initializationResponse: body,
        sourceFiles: Object.fromEntries(['departure.ts', 'combat-ordinary-combat-lifecycle.ts', 'combat-ordinary-initialization-routes.ts']
          .map(file => [file, createHash('sha256').update(readFileSync(resolve(process.cwd(), 'apps/game-api/src', file))).digest('hex')])),
        requests: { successfulOriginalKeyPosts: 3, normalConfirmationGets: 0 },
        productAdmissionVerified: false, unityConsumerVerified: false, playerVerified: false }));
  } finally { f.sql.close(); }
});

it('keeps historical initialization receipts immutable instead of reconstructing them from later state', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    const owner = new OrdinaryCombatLifecycle(f.env.DB);
    const command = { requestId: 'historical-init', source: 'ORIGINAL_START' };
    const receipt = await owner.activate('a', runId, command);
    delete receipt.sourceState;
    delete receipt.firstBattleEncounterBinding;
    delete receipt.ordinaryTrainBinding;
    delete receipt.ordinaryRailBinding;
    delete receipt.ordinaryRoofBinding;
    delete receipt.trainInitialState;
    const serialized = canonicalJson(receipt);
    // Seed pre-addition bytes in this disposable fixture, then restore the production immutability guard.
    f.sql.run('DROP TRIGGER ordinary_activation_no_update');
    f.sql.run('UPDATE ordinary_combat_activation_receipts SET response_json=?,response_hash=? WHERE run_id=?',
      [serialized, await sha256Hex(serialized), runId]);
    f.sql.run("CREATE TRIGGER ordinary_activation_no_update BEFORE UPDATE ON ordinary_combat_activation_receipts BEGIN SELECT RAISE(ABORT,'ordinary_activation_immutable'); END");
    // Corrupt local state is not evidence that combat never started; replay must not repair/reset it.
    f.sql.run("UPDATE ordinary_run_stat_states SET source_json='{}',revision=2 WHERE run_id=?", [runId]);
    const before = f.sql.export();
    expect(await owner.activate('a', runId, command)).toEqual(receipt);
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  } finally { f.sql.close(); }
});

it('does not accept client finish as verification of a new five-actor START before or after initialization', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    for (const initialized of [false, true]) {
      if (initialized) await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'init', source: 'ORIGINAL_START' });
      const before = f.sql.export();
      for (const outcome of ['VICTORY', 'FAILURE', 'ABANDONED']) {
        const response = await app.request(`http://localhost/v1/runs/${runId}/finish`, {
          method: 'POST', headers: { ...f.headers, 'Idempotency-Key': `finish-${initialized}-${outcome}` },
          body: JSON.stringify({ runId, currentStage: 'Complete', currentWaveNumber: 0, completedWaveCount: 15,
            outcome, runtimeStateJson: '{}', tacticSelections: [], clientCompletedAt: new Date().toISOString() }),
        }, f.env);
        const result = await response.json();
        expect(response.status, JSON.stringify({ accepted: result.accepted, errorCode: result.errorCode,
          finalResults: f.scalar('SELECT COUNT(*) FROM run_final_results') })).toBe(409);
        expect(result).toMatchObject({ errorCode: 'combat_finish_verification_required' });
      }
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
      expect(f.scalar('SELECT COUNT(*) FROM run_final_results')).toBe(0);
      expect(f.scalar('SELECT COUNT(*) FROM run_reward_previews')).toBe(0);
    }
  } finally { f.sql.close(); }
});

it('preserves current Unity BASIC definition identities through authenticated original-key recovery', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const kit = await readOrdinaryRunKitSource(f.env.DB, 'a', runId);
    const basic = await readOrdinaryRunBasicSource(f.env.DB, 'a', runId);
    // Independently published identities consumed by Unity OrdinaryBasicSource.
    const sources = [await captureOrdinaryBasicSource(kit), await captureOrdinaryBasicSourceV2(kit), basic];
    const unityDefinitions = ['dd99d2e4b4660a05b551bd0b226dbf3a7fb0fcf3abc26fbf3cb45345a556acf3',
      '29857155020d715f504bae5a03ec4613fa93fdbf67e48944afb8f322d77e5923',
      '6c0a9cc956222256db975c2441cef314a4d48a5a035656d9cb7f8b1a9882225c'];
    for (const [index, source] of sources.entries()) {
      const definition: Record<string, unknown> = structuredClone(source.packet);
      for (const key of ['accountId', 'runId', 'originHash', 'kitHash']) delete definition[key];
      expect(await sha256Hex(canonicalJson(definition as Parameters<typeof canonicalJson>[0]))).toBe(unityDefinitions[index]);
    }
    const before = f.sql.export();
    const lookup = await app.request('http://localhost/v1/runs/start-result', {
      method: 'POST', headers: f.headers,
      body: JSON.stringify({ operationPreviewId: f.quote.operationPreviewId, startIdempotencyKey: 'original-start' }),
    }, f.env);
    expect(lookup.status).toBe(200);
    const found = await lookup.json();
    const lifecycle = await app.request(`http://localhost/v1/runs/${runId}/lifecycle`, { headers: f.headers }, f.env);
    expect(lifecycle.status).toBe(200);
    const recovered = await lifecycle.json();
    expect(recovered.startResponse).toEqual(found.startResponse);
    expect(recovered.runStatus).toBe('ACTIVE');
    expect(found.runStatus).toBe('ACTIVE');
    expect(recovered.startResponse).toEqual(started);
    expect(recovered.startResponse).toMatchObject({ accepted: true, runId,
      ordinaryStatSource: started.ordinaryStatSource, ordinaryKitSource: kit, ordinaryBasicSource: basic,
      ordinaryRelocationSource: started.ordinaryRelocationSource });
    expect(recovered.ordinaryInitialization).toMatchObject({ status: 'PREPARED', revision: 1, runtimeExecutionVerified: false });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
  } finally { f.sql.close(); }
});

it('pins classified support scope in a new relocation version without guessing unclassified skills', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore();
    const source = await captureOrdinaryRelocationSourceV2(
      await readOrdinaryRunBasicSource(f.env.DB, 'a', String(started.runId)),
      await readOrdinaryRunKitSource(f.env.DB, 'a', String(started.runId)));
    expect(source.packet.version).toBe('combat.ordinary-relocation.v2.20261026');
    if (!('skillScopes' in source.packet)) throw new Error('scope contract missing');
    const kit = await readOrdinaryRunKitSource(f.env.DB, 'a', String(started.runId));
    expect(source.packet.kitHash).toBe(kit.kitHash);
    expect(source.packet.skillScopes.map(s => s.abilityId).sort()).toEqual(kit.packet.operators.flatMap(o => o.abilities.map(a => a.id)).sort());
    expect(source.packet.basicScopes).toHaveLength(5);
    expect(source.packet.passiveScopes).toHaveLength(5);
    expect(source.packet.unclassifiedAbilityIds).toEqual(['op_kyle.active', 'op_reina.active', 'op_reina.ultimate', 'op_mira.ultimate']);
    expect(source.packet.skillScopes.find(s => s.abilityId === 'op_sera.active')!.basis).toBe('FORMATION_ROSTER_ORIGINAL_SLOT_TIEBREAK');
    expect(source.packet.skillScopes.find(s => s.abilityId === 'op_kyle.ultimate')!.basis).toBe('GLOBAL_LIVING_ROSTER');
    expect(source.packet.skillScopes.find(s => s.abilityId === 'op_eve.active')!.basis).toBe('INSTALL_CURRENT_LIVE_CARRIAGE_THEN_FIXED');
    expect(source.packet.skillScopes.find(s => s.abilityId === 'op_mira.active')!.basis).toBe('CURRENT_CARRIAGE_ORIGINAL_BASIC_RANGE');
    expect(source.packet.crossing.immutable).toContain('FORMATION_SLOT');
    expect(source.packet.crossing.immutable).toContain('CARD_ORDER');
    expect(source.packet.ongoingEffects.attachedShieldOrHot).toContain('UNLESS_ORIGINAL_MAINTAINED_RANGE_CONDITION');
    expect(source.packet.ongoingEffects.aura).toContain('ORIGINAL_EVALUATION_TICK');
    expect(source.packet.runtimeSupported).toBe(false);
  } finally { f.sql.close(); }
});

it('pins the approved relocation contract alongside the existing v3 basic source', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore();
    expect(started).toHaveProperty('ordinaryRelocationSource');
    const runId = String(started.runId), before = f.sql.export();
    const source = await readOrdinaryRunRelocationSource(f.env.DB, 'a', runId);
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(source).toEqual(started.ordinaryRelocationSource);
    const basic = await readOrdinaryRunBasicSource(f.env.DB, 'a', runId);
    expect(source.packet).toMatchObject({ version: 'combat.ordinary-relocation.v7.20261102',
      basicVersion: basic.packet.version, basicHash: basic.basicHash, originHash: basic.packet.originHash,
      admission: { automaticChase: false, failure: 'KEEP_EXISTING_ATTACK' },
      moving: { newBasicOrActive: false, missedOpportunitiesAccumulate: false, cooldownsContinue: true,
        eveActive: 'SEPARATE_ORIGINAL_DRONE_POLICY' },
      arrival: { late: 'NO_CATCHUP_OR_IMMEDIATE_COMMIT', supportScope: 'EXPLICIT_PER_ABILITY_MAPPING_UNCLASSIFIED_MIGRATION_FORBIDDEN' },
      checkpoint: { owner: 'EXISTING_SINGLE_UNITY_CHECKPOINT' }, runtimeSupported: false, canStart: false });
    const command = { requestId: 'relocation-activate', source: 'ORIGINAL_START' };
    const receipt = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command);
    expect(receipt.ordinaryRelocationBinding).toEqual({ version: source.packet.version, originHash: source.packet.originHash,
      basicHash: basic.basicHash, relocationHash: source.relocationHash });
    expect(await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command)).toEqual(receipt);
    f.sql.run('UPDATE account_formation SET revision=revision+1');
    expect(await f.startCore()).toEqual(started);
    await expect(readOrdinaryRunRelocationSource(f.env.DB, 'b', runId)).rejects.toThrow('run_not_found');
  } finally { f.sql.close(); }
});

it('supplies the four approved scopes without inventing Reina damage recipients or skill ballistics', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore();
    const source = await captureOrdinaryRelocationSourceV3(
      await readOrdinaryRunBasicSource(f.env.DB, 'a', String(started.runId)),
      await readOrdinaryRunKitSource(f.env.DB, 'a', String(started.runId)));
    expect(source.packet).toMatchObject({ version: 'combat.ordinary-relocation.v3.20261026',
      unclassifiedAbilityIds: [], runtimeSupported: false, canStart: false });
    const packet = JSON.parse(JSON.stringify(source.packet));
    const scopes = packet.skillScopes;
    expect(scopes.find((s: { abilityId: string }) => s.abilityId === 'op_kyle.active')).toMatchObject({
      basis: 'COMPOSITE_SELF_SHIELD_AND_CURRENT_CARRIAGE_FRONT_THREATS',
      shield: { basis: 'SELF', independentOfSurvivingTauntTargets: true },
      taunt: { maximumDistanceMm: 12000, maximumTargets: 2, minimumAcceptanceTargets: 1,
        replacementAtCommit: false, origin: 'ACTUAL_CASTER_BODY_ANCHOR' } });
    for (const abilityId of ['op_reina.active', 'op_reina.ultimate']) {
      expect(scopes.find((s: { abilityId: string }) => s.abilityId === abilityId)).toMatchObject({
        primaryTarget: { maximumDistanceMm: 24000, physicalAdjacentCars: 1,
          origin: 'ACTUAL_PISTOL_MUZZLE_AT_COMMIT', replacementAtCommit: false },
        damageRecipients: 'CURRENT_ORDINARY_DIRECT_DAMAGE_CONTRACT_UNSUPPLIED',
        inheritBasicBallistics: false });
    }
    expect(scopes.find((s: { abilityId: string }) => s.abilityId === 'op_mira.ultimate')).toMatchObject({
      basis: 'CURRENT_CARRIAGE_ORIGINAL_BASIC_RANGE',
      targeting: { maximumDistanceMm: 24000, evaluation: 'EACH_SHOT_CURRENT_CANDIDATES',
        emptyCandidates: 'CANCEL_REMAINING_SHOTS_THEN_RECOVERY', maximumShots: 16 },
      inheritBasicBallistics: false });
    expect(packet.missing).toContain('REINA_DIRECT_DAMAGE_RECIPIENT_CONTRACT');
    expect(packet.missing).not.toContain('UNCLASSIFIED_ABILITY_SCOPE');
  } finally { f.sql.close(); }
});

it('preserves approved Reina recipient coefficients and recovery in v4', async () => {
  const f = await fixture(true, false, true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const source = await captureOrdinaryRelocationSourceV4(await readOrdinaryRunBasicSource(f.env.DB, 'a', runId),
      await readOrdinaryRunKitSource(f.env.DB, 'a', runId));
    const packet = JSON.parse(JSON.stringify(source.packet));
    expect(packet.version).toBe('combat.ordinary-relocation.v4.20261026');
    const [active, ultimate] = ['op_reina.active', 'op_reina.ultimate'].map(id =>
      packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === id).damageRecipients);
    expect(active).toMatchObject({ effect: 'DIRECT_DAMAGE', primaryDamageBps: 40000, maximumTargets: 1,
      additional: null, redistributeUnusedDamage: false });
    expect(ultimate).toMatchObject({ effect: 'DIRECT_DAMAGE', primaryDamageBps: 100000, maximumTargets: 4,
      redistributeUnusedDamage: false, additional: { damageBps: 30000, maximumTargets: 3, radiusMm: 3000,
        maximumMuzzleDistanceMm: 24000, physicalAdjacentCars: 1, dimension: 'THREE_DIMENSIONAL_HIT_ANCHOR',
        muzzleLosRequired: true, centerSolidOcclusionCheckRequired: true, expandByCollider: false } });
    expect(ultimate.selection).toMatchObject({ revision: 'SAME_OBSERVATION_REVISION_BEFORE_ANY_DAMAGE',
      primaryLost: 'WHOLE_CAST_MISFIRE_NO_REPLACEMENT_OR_REFUND', primaryDiesAfterSelection: 'KEEP_SELECTED_ADDITIONAL_TARGETS',
      immunePrimaryMayAnchor: true, replaceImmuneTargets: false });
    expect(ultimate.checkpoint).toMatchObject({ owner: 'EXISTING_EFFECT_OWNER',
      identity: 'ORIGINAL_CAST_ID_AND_TARGET_GENERATION_SUBEVENT', restore: 'RESUME_PROGRESS_NO_RESELECT_OR_REPEAT_DAMAGE' });
    expect(ultimate.modifiers.additionalRankScaling).toBe('SAME_ORIGINAL_ULTIMATE_RANK_MULTIPLIER');
    expect(packet.missing).not.toContain('REINA_DIRECT_DAMAGE_RECIPIENT_CONTRACT');
    expect(packet.runtimeSupported).toBe(false);
    const historical = await captureOrdinaryRelocationSourceV3(await readOrdinaryRunBasicSource(f.env.DB, 'a', runId),
      await readOrdinaryRunKitSource(f.env.DB, 'a', runId));
    for (const scope of packet.skillScopes.filter((s: { abilityId: string }) => !s.abilityId.startsWith('op_reina.'))) {
      expect(scope).toEqual(historical.packet.skillScopes.find(s => s.abilityId === scope.abilityId));
    }
  } finally { f.sql.close(); }
});

it('pins Mira ultimate fixed arrival separately from BASIC and ACTIVE without resetting shot recovery', async () => {
  const f = await fixture(true, false, true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const source = await captureOrdinaryRelocationSourceV5(await readOrdinaryRunBasicSource(f.env.DB, 'a', runId),
      await readOrdinaryRunKitSource(f.env.DB, 'a', runId));
    const packet = JSON.parse(JSON.stringify(source.packet));
    expect(packet.version).toBe('combat.ordinary-relocation.v5.20261026');
    const mira = packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_mira.ultimate');
    expect(mira.projectile).toMatchObject({ flightMs: 120, mode: 'FIXED_TARGET_DELAYED_ARRIVAL',
      stepRounding: 'CEILING_TO_ORIGINAL_COMBAT_STEP_MINIMUM_ONE_STEP', physicalCollisionAfterLaunch: false,
      speedMmPerSecond: null, collisionRadiusMm: null,
      arrival: { target: 'SAME_LIVING_TARGET_GENERATION_IN_SAME_BATTLE', rangeExit: 'KEEP_COMMITTED_SHOT',
        lostTarget: 'MISS_NO_RETARGET', casterDown: 'CANCEL_UNFIRED_KEEP_COMMITTED', battleEnded: 'NO_DAMAGE' },
      snapshot: { attack: 'FIRST_ACTUAL_SHOT_ONCE_SHARED_BY_ALL_16', attackSpeedChangesIntervalOrFlight: false },
      checkpoint: { restore: 'KEEP_REMAINING_DEADLINE_SEQUENCE_SNAPSHOT_AND_CYCLE_NO_RESTART_OR_REPLAY' } });
    expect(mira.targeting.maximumShots).toBe(16);
    const basic = await readOrdinaryRunBasicSource(f.env.DB, 'a', runId);
    expect(basic.packet.profiles.find(p => p.operatorId === 'op_mira')!.ballistics.speedMmPerSecond).toBe(48000);
    const previous = await captureOrdinaryRelocationSourceV4(basic, await readOrdinaryRunKitSource(f.env.DB, 'a', runId));
    for (const scope of packet.skillScopes.filter((s: { abilityId: string }) => s.abilityId !== 'op_mira.ultimate')) {
      expect(scope).toEqual(previous.packet.skillScopes.find(s => s.abilityId === scope.abilityId));
    }
  } finally { f.sql.close(); }
});

it('pins Eve device physical shots independently of BASIC and preserves enhancement scheduling', async () => {
  const f = await fixture(true, false, true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const source = await readOrdinaryRunRelocationSource(f.env.DB, 'a', runId);
    const packet = JSON.parse(JSON.stringify(source.packet));
    expect(packet.version).toBe('combat.ordinary-relocation.v6.20261026');
    const active = packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_eve.active');
    expect(active.projectile).toMatchObject({ mode: 'SINGLE_UNGUIDED_PHYSICAL_PROJECTILE', origin: 'ACTUAL_DEVICE_MUZZLE',
      speedMmPerSecond: 48000, radiusMm: 50, maximumFlightDistanceMm: 18000, maximumFlightMs: 375,
      eligibility: 'ORIGINAL_DEVICE_CARRIAGE_DEFENSE_ZONE_TARGET_TYPE_LOS_AND_18000_MM_INTERSECTION',
      collision: { sweep: 'PREVIOUS_TO_CURRENT_SWEPT_SPHERE', firstSolidOcclusion: 'TERMINATE',
        firstEligibleEnemy: 'ONE_DAMAGE_THEN_TERMINATE', friendlyBlocking: false, retargetOnOriginalDeath: false },
      schedule: { normal: { damageBps: 4000, intervalMs: 800 }, enhanced: { damageBps: 6000, intervalMs: 400, enhancementMs: 8000 },
        preserveNextShotOnEnhance: true, exactExpiryTickFires: false, basicCharge: false },
      snapshot: { attack: 'ORIGINAL_DEVICE_CREATION_ATK', coefficient: 'PIN_NORMAL_OR_ENHANCED_AT_LAUNCH' },
      lifetime: { casterDown: 'PAUSE_NEW_SHOTS_KEEP_COMMITTED', deviceDestroyedOrExpired: 'KEEP_COMMITTED_UNTIL_ORIGINAL_SHOT_EXPIRY' },
      checkpoint: { restore: 'NO_NEW_DEVICE_SPAWN_SHOT_ENHANCEMENT_REFRESH_OR_DUPLICATE_HIT' } });
    expect(packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_eve.ultimate').projectileReference)
      .toBe('op_eve.active.projectile');
    const basic = await readOrdinaryRunBasicSource(f.env.DB, 'a', runId);
    expect(basic.packet.profiles.find(p => p.operatorId === 'op_eve')!.ballistics).toMatchObject({speedMmPerSecond: 60000, radiusMm: 45});
    const previous = await captureOrdinaryRelocationSourceV5(basic, await readOrdinaryRunKitSource(f.env.DB, 'a', runId));
    for (const scope of packet.skillScopes.filter((s: { abilityId: string }) => !s.abilityId.startsWith('op_eve.'))) {
      expect(scope).toEqual(previous.packet.skillScopes.find(s => s.abilityId === scope.abilityId));
    }
  } finally { f.sql.close(); }
});

it.each([1, 2, 3, 4, 5])('retains relocation v%s without upgrading its support scope or hash', async version => {
  const f = await fixture(true, false, true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const basic = await readOrdinaryRunBasicSource(f.env.DB, 'a', runId);
    const old = version === 1 ? await captureOrdinaryRelocationSource(basic)
      : version === 2 ? await captureOrdinaryRelocationSourceV2(basic, await readOrdinaryRunKitSource(f.env.DB, 'a', runId))
      : version === 3 ? await captureOrdinaryRelocationSourceV3(basic, await readOrdinaryRunKitSource(f.env.DB, 'a', runId))
      : version === 4 ? await captureOrdinaryRelocationSourceV4(basic, await readOrdinaryRunKitSource(f.env.DB, 'a', runId))
      : await captureOrdinaryRelocationSourceV5(basic, await readOrdinaryRunKitSource(f.env.DB, 'a', runId));
    expect(old.packet.version).toBe(`combat.ordinary-relocation.v${version}.20261026`);
    const snapshot = JSON.parse(original.startSnapshotJson);
    snapshot.ordinaryRelocationSource = old;
    const value = JSON.stringify(snapshot);
    f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [value, runId]);
    f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
    f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, value]);
    expect(await readOrdinaryRunRelocationSource(f.env.DB, 'a', runId)).toEqual(old);
    const command = { requestId: 'old-relocation-v1', source: 'ORIGINAL_START' };
    const receipt = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command);
    expect(receipt.ordinaryRelocationBinding).toMatchObject({ version: old.packet.version, relocationHash: old.relocationHash });
    expect(await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command)).toEqual(receipt);
    expect(f.scalar('SELECT snapshot_json FROM run_snapshots')).toBe(value);
  } finally { f.sql.close(); }
});

it('rejects rehashed relocation changes before writes and leaves older STARTs without relocation intact', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const snapshot = JSON.parse(original.startSnapshotJson), valid = structuredClone(snapshot.ordinaryRelocationSource);
    const replace = () => {
      const value = JSON.stringify(snapshot);
      f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [value, runId]);
      f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
      f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, value]);
    };
    for (const mutation of ['order', 'version', 'account', 'run', 'basic', 'kit', 'scope', 'range', 'damage', 'radius', 'cap', 'flight', 'collision', 'droneSpeed', 'droneSchedule', 'null']) {
      snapshot.ordinaryRelocationSource = structuredClone(valid);
      const packet = snapshot.ordinaryRelocationSource.packet;
      if (mutation === 'order') packet.arrival.order.reverse();
      if (mutation === 'version') packet.version = 'unsupported';
      if (mutation === 'account') packet.accountId = 'b';
      if (mutation === 'run') packet.runId = 'other';
      if (mutation === 'basic') packet.basicHash = '0'.repeat(64);
      if (mutation === 'kit') packet.kitHash = '0'.repeat(64);
      if (mutation === 'scope') packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_reina.active').basis = 'CURRENT_CARRIAGE';
      if (mutation === 'range') packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_kyle.active').taunt.maximumDistanceMm++;
      if (mutation === 'damage') packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_reina.active').damageRecipients = 'ALL_ENEMIES';
      if (mutation === 'radius') packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_reina.ultimate').barrage.radiusMm++;
      if (mutation === 'cap') packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_reina.ultimate').barrage.maximumAdditionalTargets++;
      if (mutation === 'flight') packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_mira.ultimate').projectile.flightMs++;
      if (mutation === 'collision') packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_mira.ultimate').projectile.physicalCollisionAfterLaunch = true;
      if (mutation === 'droneSpeed') packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_eve.active').projectile.speedMmPerSecond++;
      if (mutation === 'droneSchedule') packet.skillScopes.find((s: { abilityId: string }) => s.abilityId === 'op_eve.active').projectile.schedule.preserveNextShotOnEnhance = false;
      snapshot.ordinaryRelocationSource.relocationHash = await sha256Hex(canonicalJson(packet));
      if (mutation === 'null') snapshot.ordinaryRelocationSource = null;
      replace();
      await expect(readOrdinaryRunRelocationSource(f.env.DB, 'a', runId)).rejects.toThrow('relocation_source_invalid');
      await expect(new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'bad-relocation', source: 'ORIGINAL_START' }))
        .rejects.toThrow('relocation_source_invalid');
      expect(f.scalar('SELECT COUNT(*) FROM ordinary_combat_activation_receipts')).toBe(0);
      expect(f.scalar('SELECT COUNT(*) FROM ordinary_run_stat_states')).toBe(0);
    }
    delete snapshot.ordinaryRelocationSource;
    replace();
    await expect(readOrdinaryRunRelocationSource(f.env.DB, 'a', runId)).rejects.toThrow('relocation_source_unavailable');
    const receipt = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'old-without-relocation', source: 'ORIGINAL_START' });
    expect(receipt).not.toHaveProperty('ordinaryRelocationBinding');
    expect(receipt.ordinaryBasicBinding!.version).toBe('combat.ordinary-basic.v3.20261026');
    expect(JSON.parse(f.scalar('SELECT snapshot_json FROM run_snapshots') as string)).not.toHaveProperty('ordinaryRelocationSource');
  } finally { f.sql.close(); }
});

it('pins approved Mira timing without converting basic fire to active or ultimate bursts', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore();
    const source = await readOrdinaryRunBasicSource(f.env.DB, 'a', String(started.runId));
    expect(source.packet.version).toBe('combat.ordinary-basic.v3.20261026');
    expect(source.packet.profiles.find(p => p.operatorId === 'op_mira')).toMatchObject({
      timing: { baseCycleMs: 1800, windupMs: 280, recoveryMs: 320 },
      ballistics: { speedMmPerSecond: 48000, radiusMm: 60, maximumFlightDistanceMm: 24000,
        maximumFlightMs: { numerator: 500, denominator: 1 } } });
    expect(source.packet).toMatchObject({ miraEmissionPolicy: { primaryShotsPerOpportunity: 1, generalRuntimeVerified: false,
      viewAuthority: 'NO_ANIMATION_CALLBACK_DAMAGE_OR_FAKE_BURST',
      skillSeparation: 'ACTIVE_AND_ULTIMATE_KEEP_THEIR_OWN_SHOT_COUNTS_AND_COMMANDS' } });
    expect(source.packet.timingPolicy.appliesTo).toContain('op_mira.basic');
    const kit = await readOrdinaryRunKitSource(f.env.DB, 'a', String(started.runId));
    const abilities = kit.packet.operators.find(p => p.operatorId === 'op_mira')!.abilities;
    expect(abilities.find(p => p.id === 'op_mira.active')).toMatchObject({ shots: 6 });
    expect(abilities.find(p => p.id === 'op_mira.ultimate')).toMatchObject({ shots: 16 });
    const previous = await captureOrdinaryBasicSourceV2(kit);
    expect(source.basicHash).not.toBe(previous.basicHash);
    expect(source.packet.profiles.filter(p => p.operatorId !== 'op_mira'))
      .toEqual(previous.packet.profiles.filter(p => p.operatorId !== 'op_mira'));
  } finally { f.sql.close(); }
});

it('retains the v2 Kyle and Eve definition with Mira timing unspecified', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore();
    const source = await captureOrdinaryBasicSourceV2(await readOrdinaryRunKitSource(f.env.DB, 'a', String(started.runId)));
    expect(source.packet.version).toBe('combat.ordinary-basic.v2.20261026');
    expect(source.packet.profiles.map(p => p.operatorId)).toEqual(['op_reina', 'op_sera', 'op_mira', 'op_kyle', 'op_eve']);
    expect(source.packet.profiles.find(p => p.operatorId === 'op_mira')!.timing).toBeNull();
    expect(source.packet.profiles.find(p => p.operatorId === 'op_kyle')).toMatchObject({ abilityId: 'op_kyle.basic',
      range: { appliesTo: 'RANGED', physicalAdjacentCars: 0, maximumDistanceMm: 18000 },
      ballistics: { speedMmPerSecond: 30000, radiusMm: 100, maximumFlightDistanceMm: 18000,
        maximumFlightMs: { numerator: 600, denominator: 1 } },
      melee: { maximumDistanceMm: 1800, contactRadiusMm: 120, contactSource: 'ACTUAL_FIST_ANCHOR_AT_COMMIT',
        damageEvaluation: 'ATTACK_AND_DEFENSE_AT_SAME_COMMIT' },
      timing: { baseCycleMs: 1800, windupMs: 300, recoveryMs: 350 } });
    expect(source.packet.profiles.find(p => p.operatorId === 'op_eve')).toMatchObject({ abilityId: 'op_eve.basic',
      launchSource: 'ACTUAL_BASIC_CARRIER_MUZZLE', range: { physicalAdjacentCars: 1, maximumDistanceMm: 24000 },
      ballistics: { speedMmPerSecond: 60000, radiusMm: 45, maximumFlightMs: { numerator: 400, denominator: 1 } },
      timing: { baseCycleMs: 2000, windupMs: 250, recoveryMs: 300 } });
    expect(source.packet).toMatchObject({
      spatialPolicy: { appliesTo: 'PROJECTILE_MODES_ONLY' },
      kyleModePolicy: { reselection: 'NEXT_READY_ONLY_NO_MID_WINDUP_SWITCH_OR_CYCLE_RESET',
        interception: 'SEPARATE_ORIGINAL_INTERCEPTION_TIMING_AND_BASIC_REMAINDER_PAUSE_RESUME' },
      eveCarrier: { independentAutoFire: false, independentHp: false, additionalDamageOrCharge: false,
        ballistics: { profileAbilityId: 'op_eve.basic' }, socket: null,
        unavailableMuzzle: 'FAIL_AND_RECOVER_NO_BODY_CENTER_FALLBACK' } });
  } finally { f.sql.close(); }
});

it('pins approved ordinary basic profiles without changing the frozen skill kit', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore();
    expect(started).toHaveProperty('ordinaryBasicSource');
    const runId = String(started.runId);
    const source = await readOrdinaryRunBasicSource(f.env.DB, 'a', runId);
    expect(started.ordinaryBasicSource).toEqual(source);
    expect(source.packet.profiles.map(p => p.operatorId)).toEqual(['op_reina', 'op_sera', 'op_mira', 'op_kyle', 'op_eve']);
    expect(source.packet.profiles[0]).toMatchObject({ abilityId: 'op_reina.basic',
      range: { physicalAdjacentCars: 1, maximumDistanceMm: 24000 },
      ballistics: { speedMmPerSecond: 72000, radiusMm: 35, maximumFlightMs: { numerator: 1000, denominator: 3 } },
      timing: { baseCycleMs: 1400, windupMs: 180, recoveryMs: 220 } });
    expect(source.packet.profiles[1]).toMatchObject({ abilityId: 'op_sera.basic',
      range: { physicalAdjacentCars: 1, maximumDistanceMm: 18000 },
      ballistics: { speedMmPerSecond: 36000, radiusMm: 80, maximumFlightMs: { numerator: 500, denominator: 1 } },
      timing: { baseCycleMs: 1200, windupMs: 240, recoveryMs: 260 } });
    expect(source.packet.profiles[2]).toMatchObject({ abilityId: 'op_mira.basic', modes: ['RANGED'],
      ballistics: { speedMmPerSecond: 48000, radiusMm: 60, maximumFlightDistanceMm: 24000 },
      timing: { baseCycleMs: 1800, windupMs: 280, recoveryMs: 320 } });
    expect(source.packet.eveCarrier).toMatchObject({ independentAutoFire: false, independentHp: false,
      additionalDamageOrCharge: false, ballistics: { profileAbilityId: 'op_eve.basic' }, socket: null });
    expect(source.packet.damagePolicy).toContain('GENERAL_CRITICAL_RESEARCH');
    expect(source.packet.runtimeSupported).toBe(false);
    expect(source.packet.canStart).toBe(false);
    const kit = await readOrdinaryRunKitSource(f.env.DB, 'a', String(started.runId));
    expect(kit.packet.version).toBe('combat.ordinary-five-kit.v2.20261102');
    expect(kit.packet.operators.flatMap(o => o.abilities)).toHaveLength(10);
    const command = { requestId: 'basic-activate', source: 'ORIGINAL_START' };
    const receipt = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command);
    expect(receipt.ordinaryBasicBinding).toEqual({ version: source.packet.version, originHash: source.packet.originHash,
      kitHash: kit.kitHash, basicHash: source.basicHash });
    f.sql.run("UPDATE account_operators SET promotion=3 WHERE operator_id='op_reina'");
    expect(await f.startCore()).toEqual(started);
    expect(await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command)).toEqual(receipt);
    expect(await readOrdinaryRunBasicSource(f.env.DB, 'a', runId)).toEqual(source);
    await expect(readOrdinaryRunBasicSource(f.env.DB, 'b', runId)).rejects.toThrow('run_not_found');
  } finally { f.sql.close(); }
});

it.each([1, 2])('retains the v%i basic packet and activation hash without interpreting it as v3', async versionNumber => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const kit = await readOrdinaryRunKitSource(f.env.DB, 'a', runId);
    const old = await (versionNumber === 1 ? captureOrdinaryBasicSource : captureOrdinaryBasicSourceV2)(kit);
    expect(old.packet.version).toBe(`combat.ordinary-basic.v${versionNumber}.20261026`);
    expect(old.packet.profiles).toHaveLength(versionNumber === 1 ? 3 : 5);
    expect(old.packet.profiles.find(p => p.operatorId === 'op_mira')!.timing).toBeNull();
    const snapshot = JSON.parse(original.startSnapshotJson);
    snapshot.ordinaryBasicSource = old;
    delete snapshot.ordinaryRelocationSource;
    const value = JSON.stringify(snapshot);
    f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [value, runId]);
    f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
    f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, value]);
    expect(await readOrdinaryRunBasicSource(f.env.DB, 'a', runId)).toEqual(old);
    const command = { requestId: 'retained-v1', source: 'ORIGINAL_START' };
    const receipt = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command);
    expect(receipt.ordinaryBasicBinding).toMatchObject({ version: old.packet.version, basicHash: old.basicHash });
    expect(await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command)).toEqual(receipt);
    expect(f.scalar('SELECT snapshot_json FROM run_snapshots')).toBe(value);
  } finally { f.sql.close(); }
});

it('rejects rehashed basic changes before initialization and does not backfill historical STARTs', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const snapshot = JSON.parse(original.startSnapshotJson);
    const replace = () => {
      const value = JSON.stringify(snapshot);
      f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [value, runId]);
      // Persisted phase snapshots are immutable; construct each corruption fixture explicitly.
      f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
      f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, value]);
    };
    const valid = JSON.parse(JSON.stringify(snapshot.ordinaryBasicSource));
    for (const mutation of ['radius', 'version', 'account', 'run', 'kit', 'null']) {
      snapshot.ordinaryBasicSource = structuredClone(valid);
      const packet = snapshot.ordinaryBasicSource.packet;
      if (mutation === 'radius') packet.profiles[3].melee.contactRadiusMm = 999;
      if (mutation === 'version') packet.version = 'combat.ordinary-basic.v99';
      if (mutation === 'account') packet.accountId = 'other';
      if (mutation === 'run') packet.runId = 'other-run';
      if (mutation === 'kit') packet.kitHash = '0'.repeat(64);
      snapshot.ordinaryBasicSource.basicHash = await sha256Hex(canonicalJson(packet));
      if (mutation === 'null') snapshot.ordinaryBasicSource = null;
      replace();
      await expect(readOrdinaryRunBasicSource(f.env.DB, 'a', runId)).rejects.toThrow('basic_source_invalid');
    }
    await expect(new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'bad-basic', source: 'ORIGINAL_START' }))
      .rejects.toThrow('basic_source_invalid');
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_run_stat_states')).toBe(0);
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_combat_activation_receipts')).toBe(0);
    expect(f.scalar('SELECT phase FROM ordinary_combat_phases')).toBe('PREPARED');
    delete snapshot.ordinaryBasicSource;
    delete snapshot.ordinaryRelocationSource;
    replace();
    await expect(readOrdinaryRunBasicSource(f.env.DB, 'a', runId)).rejects.toThrow('basic_source_unavailable');
    const receipt = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'historic', source: 'ORIGINAL_START' });
    expect(receipt).not.toHaveProperty('ordinaryBasicBinding');
    expect(JSON.parse(f.scalar('SELECT snapshot_json FROM run_snapshots') as string)).not.toHaveProperty('ordinaryBasicSource');
  } finally { f.sql.close(); }
});

it('distinguishes an unavailable original from a historical mode without ordinary initialization', async () => {
  expect((await projectOrdinaryInitialization('a', 'run', null, null)).status).toBe('UNAVAILABLE');
  expect((await projectOrdinaryInitialization('a', 'run', '{}', null)).status).toBe('NOT_APPLICABLE');
  expect((await projectOrdinaryInitialization('a', 'run', '{"ordinaryStatSource":null}', null)).status).toBe('UNAVAILABLE');
});

it('projects the original initialization phase through the existing authenticated lifecycle without writes', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const read = async () => {
      const before = f.sql.export();
      const response = await app.request(`/v1/runs/${runId}/lifecycle`, { headers: f.headers }, f.env);
      expect(response.status).toBe(200);
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
      return response.json();
    };
    expect((await read()).ordinaryInitialization).toEqual({ version: 'combat.ordinary-initialization-state.v1.20261025',
      status: 'PREPARED', revision: 1, originHash: original.originHash, runtimeExecutionVerified: false });
    f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
    expect((await read()).ordinaryInitialization).toMatchObject({ status: 'UNAVAILABLE', revision: null, originHash: null });
    f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, original.startSnapshotJson]);
    await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'activate-route', source: 'ORIGINAL_START' });
    expect((await read()).ordinaryInitialization).toMatchObject({ status: 'RUNNING', revision: 3, originHash: original.originHash, runtimeExecutionVerified: false });
    const tampered = JSON.parse(original.startSnapshotJson);tampered.ordinaryStatSource.originHash = '0'.repeat(64);
    f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [JSON.stringify(tampered), runId]);
    expect((await read()).ordinaryInitialization).toMatchObject({ status: 'UNAVAILABLE', revision: null, originHash: null });
  } finally { f.sql.close(); }
});

async function equipFixtureHp(f: Awaited<ReturnType<typeof fixture>>, runId: string) {
  // Synthetic acquisition evidence, actual persisted equipment/effect readers.
  const definitionVersion = 'equipment.run-hp-atk-def.v1.20261005';
  const effects = new EquipmentEffectOriginalStore(f.env.DB, { prepare: async () => ({
    snapshot: { version: 'equipment.effect-original.v1', equipment: { definitionId: 'fixture-hp', definitionVersion, star: 1 }, uniqueEffects: [], enchantment: null },
    assertions: [f.env.DB.prepare('INSERT INTO equipment_effect_original_write_guard(valid) VALUES(1)')] }) });
  const prepared = await effects.prepare('a', runId, { instanceId: 'fixture-hp', sourceItemId: 'fixture-source', expectedInventoryRevision: 1 });
  await f.env.DB.batch([f.env.DB.prepare(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,wearer_kind,wearer_id,slot_index,revision)
    VALUES('fixture-hp',?,'fixture-source','fixture-hp',?,'HP','SET04',1,0,'EQUIPPED','operator','op_kyle',0,1)`).bind(runId, definitionVersion), ...prepared.statements]);
}

it('rebases live equipped HP using the stat transaction verifier and preserves the restored anchor', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'activate', source: 'ORIGINAL_START' });
    // Damage and ongoing modifiers remain explicit test authorities; the maximum
    // event itself must use production START/equipment/phase verification.
    const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
    await health.commit('a', runId, { eventId: 'damage', expectedRevision: 1, targetId: 'op_kyle', change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 699 } });
    await equipFixtureHp(f, runId);
    f.sql.run('UPDATE run_equipment_inventory SET revision=revision+1');
    const proof = { sourceId: 'test-live-modifier', version: 'v1', revision: 1, status: 'NO_ACTIVE_STAT_MODIFIERS' };
    const modifiers = { prepare: async () => ({ modifiers: { research: proof, commander: proof, augments: proof, temporary: proof }, assertions: [f.env.DB.prepare('INSERT INTO departure_write_guard(valid) VALUES(1)')] }) };
    const command = { requestId: 'live-equip', expectedRevision: 1 };
    const store = new OrdinaryRunStatStore(f.env.DB, modifiers);
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
      f.sql.run('UPDATE run_equipment_inventory SET revision=revision+1'); };
    await expect(store.apply('a', runId, command)).rejects.toThrow('source_changed');
    expect((await health.read('a', runId)).revision).toBe(2);
    f.sql.run("CREATE TRIGGER test_live_receipt_failure BEFORE INSERT ON ordinary_run_stat_receipts BEGIN SELECT RAISE(ABORT,'test receipt failure'); END");
    await expect(store.apply('a', runId, command)).rejects.toThrow('outcome_unknown');
    expect((await health.read('a', runId)).revision).toBe(2);
    expect(f.scalar('SELECT revision FROM ordinary_run_stat_states')).toBe(1);
    f.sql.run('DROP TRIGGER test_live_receipt_failure');
    const batch = f.env.DB.batch.bind(f.env.DB);
    f.env.DB.batch = async statements => { await batch(statements); throw new Error('test lost acknowledgement'); };
    const result = await store.apply('a', runId, command);
    f.env.DB.batch = batch;
    expect(result.stats.actors[0]).toMatchObject({ currentHp: 810, effectiveStats: { maxHp: 1624 }, conditionalDefenseFlat: 83 });
    const saved = await health.read('a', runId);
    expect(saved.state.targets.find(t => t.id === 'op_kyle')!.health).toMatchObject({ hp: 810, maxHp: 1624, anchorHp: 699, anchorMaxHp: 1400 });
    expect(saved.revision).toBe(result.healthRevision);
    expect(saved.state.ledger.map(e => e.change.kind)).toEqual(['ACTUAL_HP', 'MAX_HP_REBASE']);
    expect(await new OrdinaryRunStatStore(f.env.DB).apply('a', runId, command)).toEqual(result);
    expect((await health.read('a', runId)).state).toEqual(saved.state);
  } finally { f.sql.close(); }
});

it('commits maximum and anchored HP with stats, without a healing event, and replays after reopening', async () => {
  const f = await fixture();
  try {
    const runId = String((await f.startCore()).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    // Synthetic prior maximum, explicitly authorized by a test-only verifier.
    const verifier = { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) };
    const health = new CombatHealthStore(f.env.DB, verifier);
    await health.initialize('a', runId, original.origin.actors.map(a => ({ id: a.operatorId,
      hp: a.operatorId === 'op_kyle' ? 1483 : a.operatorId === 'op_eve' ? 0 : a.permanentStats.maxHp,
      maxHp: a.operatorId === 'op_kyle' ? 2968 : a.operatorId === 'op_eve' ? 1 : a.permanentStats.maxHp })));
    const proof = { sourceId: 'test-only-effect-owner', version: 'v1', revision: 1, status: 'NO_ACTIVE_STAT_MODIFIERS' };
    const modifiers = { prepare: async () => ({ modifiers: { research: proof, commander: proof, augments: proof, temporary: proof }, assertions: [f.env.DB.prepare('INSERT INTO departure_write_guard(valid) VALUES(1)')] }) };
    const command = { requestId: 'rebase', expectedRevision: 0 };
    // Phase-less historical fixtures cannot use the production maximum verifier.
    await expect(new OrdinaryRunStatStore(f.env.DB, modifiers).apply('a', runId, command)).rejects.toThrow('source_changed');
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_run_stat_states')).toBe(0);
    f.sql.run("CREATE TRIGGER test_stat_receipt_failure BEFORE INSERT ON ordinary_run_stat_receipts BEGIN SELECT RAISE(ABORT,'test receipt failure'); END");
    await expect(new OrdinaryRunStatStore(f.env.DB, modifiers, verifier).apply('a', runId, command)).rejects.toThrow('outcome_unknown');
    expect((await health.read('a', runId)).revision).toBe(1);
    expect(f.scalar('SELECT COUNT(*) FROM combat_damage_receipts')).toBe(0);
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_run_stat_states')).toBe(0);
    f.sql.run('DROP TRIGGER test_stat_receipt_failure');
    const batch = f.env.DB.batch.bind(f.env.DB);
    f.env.DB.batch = async statements => { await batch(statements); throw new Error('test-only lost committed acknowledgement'); };
    const result = await new OrdinaryRunStatStore(f.env.DB, modifiers, verifier).apply('a', runId, command);
    expect(result.stats.actors[0]).toMatchObject({ currentHp: 699, effectiveStats: { maxHp: 1400 }, conditionalDefenseFlat: 83 });
    const saved = await health.read('a', runId);
    expect(saved.revision).toBe(result.healthRevision);
    expect(saved.state.targets.find(t => t.id === 'op_kyle')!.health).toMatchObject({ hp: 699, maxHp: 1400, anchorHp: 1483, anchorMaxHp: 2968 });
    expect(saved.state.targets.find(t => t.id === 'op_eve')!.health.hp).toBe(0);
    expect(saved.state.ledger.map(e => e.change.kind)).toEqual(['MAX_HP_REBASE', 'MAX_HP_REBASE']);
    expect(await new OrdinaryRunStatStore(f.env.DB).apply('a', runId, command)).toEqual(result);
    expect((await health.read('a', runId)).revision).toBe(saved.revision);
    f.env.DB.batch = batch;
    const next = await new OrdinaryRunStatStore(f.env.DB, modifiers).apply('a', runId, { requestId: 'after-restore', expectedRevision: 1 });
    expect(next.stats.actors).toEqual(result.stats.actors);
    expect(next.healthRevision).toBe(result.healthRevision);
    expect((await health.read('a', runId)).state.ledger).toEqual(saved.state.ledger);
  } finally { f.sql.close(); }
});

it('reads node entry HP from the live six-target owner and never supplies missing runtime state', async () => {
  const f = await fixture(true), start = await f.startCore(), runId = String(start.runId);
  const activation = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'entry-init', source: 'ORIGINAL_START' });
  const before = await readOrdinaryNodeEntryOwners(f.env.DB, 'a', runId);
  expect(before.entry.automation).toMatchObject({ revision: 1, ultimateAuto: false });
  expect(activation.ordinaryAutomation).toEqual(before.entry.automation);
  expect(before.entry.automation!.actors).toEqual(before.entry.actors.map(a => ({ operatorId: a.operatorId,
    activeCooldownRemainingTicks: 0, ultimateCharge: 0 })));
  await expect(readOrdinaryRunAutomation(f.env.DB, 'other', runId)).rejects.toThrow('automation_source_conflict');
  const pinnedRelocation = JSON.parse(before.startSnapshot).ordinaryRelocationSource;
  expect(before.entry.definitions).toMatchObject({ relocationHash: pinnedRelocation.relocationHash });
  expect(before.entry.actors).toHaveLength(5); expect(before.entry.train.hp).toBe(5000);
  // Synthetic damage authorization exercises the real HP owner; not actual enemy damage evidence.
  const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
  await health.commit('a', runId, { eventId: 'prior-train', expectedRevision: 1, targetId: 'train_a', change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 4200 } });
  await health.commit('a', runId, { eventId: 'prior-kyle', expectedRevision: 2, targetId: 'op_kyle', change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 700 } });
  const after = await readOrdinaryNodeEntryOwners(f.env.DB, 'a', runId);
  expect(after.entry.train.hp).toBe(4200); expect(after.entry.actors.find(a => a.operatorId === 'op_kyle')!.hp).toBe(700);
  expect(after.entry.owners.healthRevision).toBe(3);
  expect(after.entry.actors.map(a => a.slots)).toEqual(before.entry.actors.map(a => a.slots));
  await expect(new OrdinaryNodeEntrySource(f.env.DB).prepare({ accountId: 'a', runId, nodeId: 'n1-0',
    mapRevision: 3, startSnapshot: (await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId)).startSnapshotJson })).rejects.toThrow('runtime_entry_unavailable');
  const context = { accountId: 'a', runId, nodeId: 'n1-0', mapRevision: 3, arrivalEventId: 'original-arrival',
    startSnapshot: after.startSnapshot, executionOrigin: issueOrdinaryNodeExecutionOrigin() };
  let replaceSeed = true, injectGeometry = false;
  const actions = { version: 'ordinary.node-actions.v1.20261030', stepTimeSpanTicks: 500000, ultimateAuto: false,
    actors: after.entry.actors.map(actor => ({ operatorId: actor.operatorId, basicNextOpportunityInTicks: 7,
      activeCooldownRemainingTicks: 0, ultimateCharge: 0 })) };
  const source = new OrdinaryNodeEntrySource(f.env.DB, { prepare: async input => {
    expect(input.relocation).toEqual(pinnedRelocation);
    expect(input.geometry).toBeUndefined();
    const origin = input.executionOrigin!;
    if (replaceSeed) origin.determinism.seed = (origin.determinism.seed + 1) >>> 0;
    return { state: { executionVersion: origin.executionVersion, determinism: origin.determinism,
      effects: { synthetic: true }, actions, spatial: { synthetic: true, ...(injectGeometry ? { definitionBinding: { unapproved: true } } : {}) }, enemies: { synthetic: true } },
    assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')] };
  } });
  const originalSeed = context.executionOrigin.determinism.seed;
  await expect(source.prepare(context)).rejects.toThrow('execution_origin_conflict');
  expect(context.executionOrigin.determinism.seed).toBe(originalSeed);
  replaceSeed = false;
  injectGeometry = true;
  await expect(source.prepare(context)).rejects.toThrow('geometry_binding_conflict');
  injectGeometry = false;
  const prepared = await source.prepare(context);
  expect(prepared.snapshot.runtime).toMatchObject(context.executionOrigin);
  expect(prepared.snapshot.runtime.actions).toMatchObject({ ...actions,
    stats: expect.arrayContaining([expect.objectContaining({ operatorId: 'op_kyle',
      conditionalDefenseFlat: 0, skillMagnitude: expect.any(Object) })]) });
  const wrongActor = actions.actors[0].operatorId;
  actions.actors[0].operatorId = 'foreign-actor';
  await expect(source.prepare(context)).rejects.toThrow('actions_source_conflict');
  actions.actors[0].operatorId = wrongActor;
  const actorIds = after.entry.actors.map(a => a.operatorId);
  actions.ultimateAuto = true;
  await expect(source.prepare(context)).rejects.toThrow('automation_source_conflict');
  actions.ultimateAuto = false;
  actions.actors[0].activeCooldownRemainingTicks = 1;
  await expect(source.prepare(context)).rejects.toThrow('actions_source_conflict');
  actions.actors[0].activeCooldownRemainingTicks = 0;
  for (const field of ['ultimateAuto', 'stepTimeSpanTicks', 'actors']) {
    const incomplete = { ...actions }; delete incomplete[field as keyof typeof incomplete];
    expect(() => readOrdinaryNodeActions(incomplete, actorIds)).toThrow('actions_source_conflict');
  }
  const duplicate = structuredClone(actions); duplicate.actors[0].operatorId = duplicate.actors[1].operatorId;
  expect(() => readOrdinaryNodeActions(duplicate, actorIds)).toThrow('actions_source_conflict');
  const overflow = structuredClone(actions); overflow.actors[0].ultimateCharge = 2147483648;
  expect(() => readOrdinaryNodeActions(overflow, actorIds)).toThrow('actions_source_conflict');
  expect((await source.prepare(context)).snapshot.runtime.actions).toEqual(prepared.snapshot.runtime.actions);
  const zero = { ...context.executionOrigin, determinism: { ...context.executionOrigin.determinism, seed: 0, initialState: 0x6d2b79f5 } };
  expect(ordinaryNodeExecutionOriginSchema.safeParse(zero).success).toBe(true);
  expect(ordinaryNodeExecutionOriginSchema.safeParse({ ...zero, determinism: { ...zero.determinism, initialState: 0 } }).success).toBe(false);
  // Capture must reject if the exact inspected HP owner changes before the caller's batch.
  await expect(f.env.DB.batch(before.assertions)).rejects.toThrow();
  expect(await f.env.DB.prepare('SELECT 1 FROM ordinary_node_attempts WHERE run_id=?').bind(runId).first()).toBeNull();
});

it('binds a registered definition to the node origin without allowing runtime scope mutation', async () => {
  const f = await fixture(true);
  try {
    // Deliberately partial synthetic asset: registration does not claim full five-actor execution support.
    const body = JSON.stringify({ version: 'ordinary.native-geometry-definition.v1.20261031', definitionId: 'test-foot',
      units: 'MILLIMETERS', millimetersPerUnit: 1000, axis: 'X_FORWARD_Y_UP_Z_SIDE', coordinateSpace: 'BOUND_FRAME_LOCAL',
      source: { scenePath: 'Assets/Test.unity', sceneHash: 'a'.repeat(64) },
      entities: [{ authoringId: 'kyle', role: 'OPERATOR', definitionRef: 'op_kyle', framePath: 'Root/Kyle',
        assetPath: 'Assets/Test.prefab', assetHash: 'b'.repeat(64),
        anchors: [{ id: 'foot', role: 'FOOT', positionMm: [0,0,0] }], shapes: [] }] });
    const registry = new OrdinaryNativeGeometryRegistry(f.env.DB, { definitionId: 'test-foot',
      version: 'ordinary.native-geometry-definition.v1.20261031', definitionHash: await sha256Hex(body) });
    await registry.register(body);
    const started = await f.startCore(registry);
    const runId = String(started.runId);
    await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'geometry-init', source: 'ORIGINAL_START' });
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    expect(JSON.parse(original.startSnapshotJson).ordinaryGeometryReference).toEqual(started.ordinaryGeometryReference);
    expect(started.ordinaryGeometryReference).toMatchObject({ definitionHash: await sha256Hex(body) });
    // A later publication cannot replace the saved run's selection, even on START retry.
    const newerBody = body.replace('test-foot', 'test-foot-revision-2');
    const newer = new OrdinaryNativeGeometryRegistry(f.env.DB, { definitionId: 'test-foot-revision-2',
      version: 'ordinary.native-geometry-definition.v1.20261031', definitionHash: await sha256Hex(newerBody) });
    await newer.register(newerBody);
    expect(await f.startCore(newer)).toEqual(started);
    const next = await fixture(true);
    try {
      const nextRegistry = new OrdinaryNativeGeometryRegistry(next.env.DB, (await newer.read()).reference);
      await nextRegistry.register(newerBody);
      const nextStart = await next.startCore(nextRegistry);
      expect(nextStart.ordinaryGeometryReference).toEqual((await newer.read()).reference);
    } finally { next.sql.close(); }
    let corrupt = false;
    const source = new OrdinaryNodeEntrySource(f.env.DB, { prepare: async input => {
      expect(input.geometry!.definition).toEqual(JSON.parse(body));
      if (corrupt) input.geometry!.binding.nodeId = 'other-node';
      return { state: { ...input.executionOrigin!, effects: { synthetic: true }, enemies: { synthetic: true },
        spatial: { definitionBinding: input.geometry!.binding }, actions: {
          version: 'ordinary.node-actions.v1.20261030', stepTimeSpanTicks: 500000, ultimateAuto: false,
          actors: input.entry.actors.map(a => ({ operatorId: a.operatorId, basicNextOpportunityInTicks: 1,
            activeCooldownRemainingTicks: 0, ultimateCharge: 0 })) } },
      assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')] };
    } });
    const context = { accountId: 'a', runId, nodeId: 'n1-0', mapRevision: 3, arrivalEventId: 'fixture-arrival',
      startSnapshot: original.startSnapshotJson, executionOrigin: issueOrdinaryNodeExecutionOrigin() };
    const prepared = await source.prepare(context);
    expect(prepared.snapshot.runtime.spatial.definitionBinding).toMatchObject({ nodeId: 'n1-0', originGeneration: 1,
      accountId: 'a', runId, entryMapRevision: 3, definitionRef: { definitionHash: await sha256Hex(body) } });
    expect((await source.prepare({ ...context, nodeId: 'n2-0', mapRevision: 5 })).snapshot.runtime.spatial.definitionBinding)
      .toMatchObject({ nodeId: 'n2-0', definitionRef: started.ordinaryGeometryReference });
    corrupt = true;
    await expect(source.prepare(context)).rejects.toThrow('geometry_binding_conflict');
    expect(context.nodeId).toBe('n1-0');
  } finally { f.sql.close(); }
});

async function fixture(keepPhase = false, historicalTrain = false, historicalKit = false) {
  const f = await ce03ProducerFixture();
  const quote = await (await f.preview()).json();
  const row = (await f.env.DB.prepare('SELECT * FROM operation_previews WHERE id=?').bind(quote.operationPreviewId)
    .first<Parameters<typeof startDeparture>[2]>())!;
  const start = async (geometry?: OrdinaryNativeGeometryRegistry) => {
    const result = await startDeparture(f.env.DB, 'a', row, 'original-start', async (p, runId) => ({
    accepted: true, startStatus: 'STARTED', runId, operationPreviewId: p.id, encounterType: 'ORDINARY_15_WAVE',
    }), undefined, f.env.ENVIRONMENT, undefined, undefined, undefined, geometry);
    if (historicalKit) {
      const { captureOrdinaryKitSource } = await import('../../apps/game-api/src/combat-ordinary-kit-source');
      const { captureOrdinaryBasicSourceV3 } = await import('../../apps/game-api/src/combat-ordinary-basic-source');
      const { captureOrdinaryRelocationSourceV6 } = await import('../../apps/game-api/src/combat-ordinary-relocation-source');
      const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', String(result.runId));
      const snapshot = JSON.parse(original.startSnapshotJson);
      const kit = await captureOrdinaryKitSource(original.origin, original.originHash, snapshot.admission);
      const basic = await captureOrdinaryBasicSourceV3(kit);
      const sources = { ordinaryKitSource: kit, ordinaryBasicSource: basic,
        ordinaryRelocationSource: await captureOrdinaryRelocationSourceV6(basic, kit) };
      Object.assign(result, sources); Object.assign(snapshot, sources); Object.assign(snapshot.departureResponse, sources);
      const bytes = JSON.stringify(snapshot);
      f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [bytes, result.runId]);
      f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [result.runId]);
      f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [result.runId, 'a', original.originHash, bytes]);
    }
    if (historicalTrain) {
      const runId = String(result.runId), original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
      const snapshot = JSON.parse(original.startSnapshotJson);
      delete snapshot.ordinaryTrainSource;
      delete snapshot.ordinaryRailSource;
      delete snapshot.ordinaryRoofSource;
      const bytes = JSON.stringify(snapshot);
      f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [bytes, runId]);
      f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
      f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, bytes]);
    }
    // Older focused HP/stat tests model a historical run without the new phase
    // owner. New lifecycle tests keep the actual START-owned phase row.
    if (!keepPhase) f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [String(result.runId)]);
    return result;
  };
  return { ...f, quote, startCore: start };
}

it('atomically initializes real phase, HP and initial stats with source verification and original-key recovery', async () => {
  const f = await fixture(true, true);
  try {
    const runId = String((await f.startCore()).runId);
    const origin = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    expect(f.scalar('SELECT phase FROM ordinary_combat_phases')).toBe('PREPARED');
    const initialTargets = origin.origin.actors.map(a => ({ id: a.operatorId, hp: a.permanentStats.maxHp - 1, maxHp: a.permanentStats.maxHp }));
    const verifier = { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) };
    // The verifier is synthetic; no product starting-HP rule is invented here.
    const command = { requestId: 'activate', initialTargets };
    await expect(new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command)).rejects.toThrow('verification_required');
    await expect(new CombatHealthStore(f.env.DB, verifier).initialize('a', runId, initialTargets)).rejects.toThrow();
    expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
    expect(() => f.sql.run('INSERT INTO run_tactical_ledger VALUES(?,?,?,?,?)', ['too-early', runId, 1, '{}', 1])).toThrow('phase_not_running');
    f.sql.run("CREATE TRIGGER fail_activation_receipt BEFORE INSERT ON ordinary_combat_activation_receipts BEGIN SELECT RAISE(ABORT,'test activation failure'); END");
    await expect(new OrdinaryCombatLifecycle(f.env.DB, verifier).activate('a', runId, command)).rejects.toThrow('outcome_unknown');
    expect(f.scalar('SELECT phase FROM ordinary_combat_phases')).toBe('PREPARED');
    expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
    expect(f.scalar('SELECT count(*) FROM ordinary_run_stat_states')).toBe(0);
    f.sql.run('DROP TRIGGER fail_activation_receipt');
    const batch = f.env.DB.batch.bind(f.env.DB);
    f.env.DB.batch = async statements => { await batch(statements); throw new Error('test-only activation ACK lost'); };
    const result = await new OrdinaryCombatLifecycle(f.env.DB, verifier).activate('a', runId, command);
    f.env.DB.batch = batch;
    expect(result.phase).toBe('RUNNING');
    expect(result.stats.actors[0]!.currentHp).toBe(1399);
    expect(f.scalar('SELECT phase FROM ordinary_combat_phases')).toBe('RUNNING');
    expect(await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, command)).toEqual(result);
    await expect(new OrdinaryCombatLifecycle(f.env.DB, verifier).activate('a', runId, { ...command, initialTargets: initialTargets.map(t => ({ ...t, hp: 0 })) })).rejects.toThrow('request_conflict');
    await expect(new OrdinaryCombatLifecycle(f.env.DB, verifier).activate('a', runId, { ...command, requestId: 'second' })).rejects.toThrow('phase_conflict');
    expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(1);
    const health = new CombatHealthStore(f.env.DB, verifier);
    const damage = { eventId: 'hit-1', expectedRevision: 1, targetId: 'op_kyle', change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 1200 } };
    const hit = await health.commit('a', runId, damage);
    const heal = { eventId: 'heal-1', expectedRevision: 2, targetId: 'op_kyle', change: { kind: 'ACTUAL_HP', reason: 'HEAL', hp: 1250 } };
    await health.commit('a', runId, heal);
    expect(await new CombatHealthStore(f.env.DB).commit('a', runId, damage)).toEqual(hit);
    expect((await health.read('a', runId)).state.targets.find(t => t.id === 'op_kyle')!.health.hp).toBe(1250);
    await expect(health.commit('a', runId, { ...damage, change: { ...damage.change, hp: 1199 } })).rejects.toThrow('event_conflict');
    await health.commit('a', runId, { ...heal, eventId: 'heal-2', expectedRevision: 3, change: { ...heal.change, hp: 1300 } });
    expect((await health.read('a', runId)).revision).toBe(4);
  } finally { f.sql.close(); }
});

it('pins initial modifiers but never treats the START artifact as proof of current effects', async () => {
  const f = await fixture();
  try {
    const runId = String((await f.startCore()).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const initial = await readOrdinaryInitialModifiers(f.env.DB, 'a', runId);
    expect(initial).toMatchObject({ stage: 'INITIAL_BEFORE_COMBAT', originHash: original.originHash,
      modifiers: { research: { status: 'NO_ACTIVE_STAT_MODIFIERS' } } });
    // HP initialization verifier remains explicitly synthetic.
    const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
    await health.initialize('a', runId, original.origin.actors.map(a => ({ id: a.operatorId, hp: a.permanentStats.maxHp, maxHp: a.permanentStats.maxHp })));
    const store = new OrdinaryRunStatStore(f.env.DB), command = { requestId: 'initial-real-modifiers', expectedRevision: 0 };
    // No HP event can also mean combat is already running before its first hit.
    // An immutable START pin cannot attest the CURRENT execution phase.
    await expect(store.apply('a', runId, command)).rejects.toThrow('modifier_source_unavailable');
    await expect(store.apply('a', runId, { requestId: 'ongoing-no-source', expectedRevision: 1 })).rejects.toThrow('modifier_source_unavailable');
  } finally { f.sql.close(); }
});

it('accepts only one concurrent activation key for the actual START phase', async () => {
  const f = await fixture(true, true);
  try {
    const runId = String((await f.startCore()).runId);
    const origin = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const initialTargets = origin.origin.actors.map(a => ({ id: a.operatorId, hp: a.permanentStats.maxHp, maxHp: a.permanentStats.maxHp }));
    const verifier = { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) };
    const results = await Promise.allSettled(['one', 'two'].map(requestId =>
      new OrdinaryCombatLifecycle(f.env.DB, verifier).activate('a', runId, { requestId, initialTargets })));
    expect(results.filter(r => r.status === 'fulfilled')).toHaveLength(1);
    expect(results.filter(r => r.status === 'rejected')).toHaveLength(1);
    expect(f.scalar('SELECT count(*) FROM ordinary_combat_activation_receipts')).toBe(1);
    expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(1);
    expect(f.scalar('SELECT count(*) FROM ordinary_run_stat_states')).toBe(1);
    expect(f.scalar('SELECT phase FROM ordinary_combat_phases')).toBe('RUNNING');
  } finally { f.sql.close(); }
});

it('initializes fresh five-operator HP through the actual START source without a test verifier', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    await equipFixtureHp(f, runId);
    f.sql.run("UPDATE account_operators SET promotion=3,revision=revision+1 WHERE operator_id='op_kyle'");
    const lifecycle = new OrdinaryCombatLifecycle(f.env.DB);
    const command = { requestId: 'real-initial-hp', source: 'ORIGINAL_START' };
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
      f.sql.run('UPDATE run_equipment_inventory SET revision=revision+1'); };
    await expect(lifecycle.activate('a', runId, command)).rejects.toThrow('initialization_conflict');
    expect(f.scalar('SELECT phase FROM ordinary_combat_phases')).toBe('PREPARED');
    expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
    // Restore the test fixture after the externally injected version race.
    f.sql.run('UPDATE run_equipment_inventory SET revision=1');
    const result = await lifecycle.activate('a', runId, command);
    const kit = await readOrdinaryRunKitSource(f.env.DB, 'a', runId);
    expect(result.ordinaryKitBinding).toEqual({ version: kit.packet.version, originHash: kit.packet.originHash, kitHash: kit.kitHash });
    expect(result.health.state.targets.find(t => t.id === 'op_kyle')!.health).toMatchObject({ hp: 1624, maxHp: 1624 });
    expect(result.stats.actors).toHaveLength(5);
    expect(result.stats.actors.every(a => a.currentHp === a.effectiveStats.maxHp)).toBe(true);
    const proof = { sourceId: 'test-live-modifier', version: 'v1', revision: 1, status: 'NO_ACTIVE_STAT_MODIFIERS' };
    const live = new OrdinaryRunStatStore(f.env.DB, { prepare: async () => ({
      modifiers: { research: proof, commander: proof, augments: proof, temporary: proof },
      assertions: [f.env.DB.prepare('INSERT INTO departure_write_guard(valid) VALUES(1)')] }) });
    expect((await live.apply('a', runId, { requestId: 'equipped-next', expectedRevision: 1 })).stats.actors[0]!.currentHp).toBe(1624);
    // Recovery must return the original event before reading today's equipment.
    f.sql.run('UPDATE run_equipment_inventory SET revision=revision+1');
    expect(await lifecycle.activate('a', runId, command)).toEqual(result);
    await expect(lifecycle.activate('a', runId, { ...command, requestId: 'not-a-restore' })).rejects.toThrow('phase_conflict');
  } finally { f.sql.close(); }
});

it('keeps initial HP and phase untouched when the pinned START kit is absent or altered', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    for (const missing of [true, false]) {
      const snapshot = JSON.parse(original.startSnapshotJson);
      if (missing) delete snapshot.ordinaryKitSource;
      else snapshot.ordinaryKitSource.kitHash = '0'.repeat(64);
      const text = JSON.stringify(snapshot);
      // Seed a corrupt PREPARED fixture without disabling the immutable transition trigger.
      f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [text, runId]);
      f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
      f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, text]);
      await expect(new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId,
        { requestId: 'invalid-kit', source: 'ORIGINAL_START' }))
        .rejects.toThrow(missing ? 'kit_source_unavailable' : 'kit_source_invalid');
      expect(f.scalar('SELECT phase FROM ordinary_combat_phases')).toBe('PREPARED');
      expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
      expect(f.scalar('SELECT count(*) FROM ordinary_run_stat_states')).toBe(0);
      expect(f.scalar('SELECT count(*) FROM ordinary_combat_activation_receipts')).toBe(0);
    }
  } finally { f.sql.close(); }
});

it('preserves the initial modifier artifact after progression without authorizing current calculation', async () => {
  const f = await fixture();
  try {
    const runId = String((await f.startCore()).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
    await health.initialize('a', runId, original.origin.actors.map(a => ({ id: a.operatorId, hp: a.permanentStats.maxHp, maxHp: a.permanentStats.maxHp })));
    const initial = await readOrdinaryInitialModifiers(f.env.DB, 'a', runId);
    await health.commit('a', runId, { eventId: 'first-damage', expectedRevision: 1, targetId: 'op_kyle', change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 600 } });
    f.sql.run('INSERT INTO run_tactical_ledger VALUES(?,?,?,?,?)', ['first-event', runId, 1, '{}', 1]);
    const store = new OrdinaryRunStatStore(f.env.DB), command = { requestId: 'initial-race', expectedRevision: 0 };
    expect(await readOrdinaryInitialModifiers(f.env.DB, 'a', runId)).toEqual(initial);
    await expect(readOrdinaryInitialModifiers(f.env.DB, 'b', runId)).rejects.toThrow('run_not_found');
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_run_stat_states')).toBe(0);
    await expect(store.apply('a', runId, command)).rejects.toThrow('modifier_source_unavailable');
  } finally { f.sql.close(); }
});

it('checks ordinary support attribution against the original five without authorizing damage or HP', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const configuration = 'a'.repeat(64);
    const actions = [
      { id: 'heal', actor: 'op_sera', ability: 'op_sera.active', target: 'op_kyle', targets: ['op_kyle'], due: 8, status: 'APPLIED' },
      { id: 'shield', actor: 'op_kyle', ability: 'op_kyle.active', target: 'op_kyle', targets: ['op_kyle'], due: 7, status: 'APPLIED' },
    ];
    const rows = await Promise.all(actions.map(async (action, index) => {
      const kind = index === 0 ? 'HEAL' : 'SHIELD_ISSUE';
      return { runId, profileSha256: configuration, configurationSha256: configuration,
        journalSequence: index + 1, globalSequence: index + 1, commandId: action.id, abilityId: action.ability,
        effectIndex: 0, targetId: action.target, pulseIndex: 0, kind, tick: 8, sourceId: action.actor, sourceKind: 'SKILL',
        actualResult: index === 0 ? { actualHpGain: 20 } : { previousRemaining: 0, resultingRemaining: 30,
          addedAmount: 30, previousExpiry: 0, expiry: 128, outcome: 'ADD' },
        id: await sha256Hex(canonicalJson([runId, configuration, index + 1, action.id, action.ability, 0, action.target, 0, kind])) };
    }));
    expect(await verifyOrdinarySupportAttribution(original, configuration, actions, rows))
      .toEqual({ scope: 'ORDINARY_SUPPORT_ATTRIBUTION_ONLY', verifiedRows: 2 });
    const attempt = { nodeId: 'n1-0', generation: 3 };
    const receipts = actions.map((action, i) => ({ eventId: action.id, targetId: action.target, tick: 8,
      hpAfter: 100, hpLoss: 0, absorbed: 0, hpGain: i === 0 ? 20 : 0, derivedExcess: 0, shieldAdded: i === 1 ? 30 : 0 }));
    const nodeActions = actions.map((action, i) => ({ ...action, healthReceipt: receipts[i] }));
    const nodeRows = rows.map((row, i) => ({ ...row, nodeId: attempt.nodeId, attemptGeneration: 3,
      executorId: row.sourceId, healthReceipt: receipts[i] }));
    expect(await verifyOrdinarySupportAttribution(original, configuration, nodeActions, nodeRows, attempt))
      .toMatchObject({ verifiedRows: 2 });
    expect(await verifyOrdinarySupportAttribution(original, configuration, nodeActions, [nodeRows[0], {
      ...nodeRows[1], journalSequence: 4, actualResult: { previousRemaining: 20, resultingRemaining: 30,
        addedAmount: 10, previousExpiry: 100, expiry: 128, outcome: 'REFRESH' },
    }], attempt)).toMatchObject({ verifiedRows: 2 });
    for (const change of [{ nodeId: 'other' }, { attemptGeneration: 2 }, { executorId: 'other' },
      { healthReceipt: { ...receipts[0], hpGain: 21 } }, { healthReceipt: { ...receipts[0], eventId: 'other' } }]) {
      await expect(verifyOrdinarySupportAttribution(original, configuration, nodeActions, [{ ...nodeRows[0], ...change }], attempt))
        .rejects.toThrow('ordinary_support_attribution_conflict');
    }
    for (const change of [{ sourceId: 'op_reina' }, { abilityId: 'op_sera.ultimate' }, { targetId: 'train' },
      { runId: 'other' }, { globalSequence: 2 }, { journalSequence: 2 }, { tick: 7 }, { id: 'b'.repeat(64) },
      { kind: 'DAMAGE' }, { actualResult: { actualHpGain: -1 } }, { configurationSha256: 'b'.repeat(64) }]) {
      await expect(verifyOrdinarySupportAttribution(original, configuration, actions, [{ ...rows[0], ...change }]))
        .rejects.toThrow('ordinary_support_attribution_conflict');
    }
    await expect(verifyOrdinarySupportAttribution(original, configuration, actions, [rows[0], { ...rows[0], journalSequence: 2 }]))
      .rejects.toThrow('ordinary_support_attribution_conflict');
    await expect(verifyOrdinarySupportAttribution(original, configuration, [{ ...actions[0], status: 'CANCELLED_DOWN' }, actions[1]], rows))
      .rejects.toThrow('ordinary_support_attribution_conflict');
    await expect(verifyOrdinarySupportAttribution(original, configuration, [actions[0], { ...actions[1], id: 'heal' }], rows))
      .rejects.toThrow('ordinary_support_attribution_conflict');
    expect(f.scalar('SELECT count(*) FROM combat_damage_states')).toBe(0);
  } finally { f.sql.close(); }
});

it('pins Kyle taunt v2 at START and reads retained two-target sources without rewriting them', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const { readOrdinaryKitFromOriginal } = await import('../../apps/game-api/src/combat-ordinary-kit-source');
    const { approvedKyleShieldAbilities } = await import('../../apps/game-api/src/combat-shield-budget');
    const current = await readOrdinaryKitFromOriginal(original);
    const kyle = current.packet.operators.find(o => o.operatorId === 'op_kyle')!;
    expect(kyle.abilities[0]).toMatchObject({ maximumFrontTargets: 3, tauntPolicyVersion: 'kyle-taunt.v2.20261109',
      tauntRangeMm: 12000, tauntLineOfSightRequired: true, tauntScope: 'CURRENT_CARRIAGE_THREATS',
      tauntTargetOrder: 'DISTANCE_THEN_ORIGINAL_ORDER_THEN_ID', tauntDurationMs: 4000,
      shieldCasterHpBps: 1800, shieldDurationMs: 6000, cooldownMs: 18000, windupMs: 350, recoveryMs: 300 });
    const { slot: _slot, ...projection } = kyle;
    expect(await sha256Hex(canonicalJson(projection))).toBe('ba76bacc1052176533104b6ed28164e4df6735b7163b284b43ffa96cbb8b5b11');
    const retained = structuredClone(current);
    retained.packet.operators.find(o => o.operatorId === 'op_kyle')!.abilities = approvedKyleShieldAbilities.map(a => ({ ...a }));
    retained.kitHash = await sha256Hex(canonicalJson(retained.packet));
    const snapshot = JSON.parse(original.startSnapshotJson);snapshot.ordinaryKitSource = retained;
    const legacy = { ...original, startSnapshotJson: JSON.stringify(snapshot) };
    const before = legacy.startSnapshotJson;
    expect(await readOrdinaryKitFromOriginal(legacy)).toEqual(retained);
    expect(legacy.startSnapshotJson).toBe(before);
    expect((await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId)).startSnapshotJson).toBe(original.startSnapshotJson);
    for (const change of [{ maximumFrontTargets: 4 }, { tauntRangeMm: 12001 }, { tauntPolicyVersion: 'unknown' }]) {
      const altered = structuredClone(current);Object.assign(altered.packet.operators.find(o => o.operatorId === 'op_kyle')!.abilities[0]!, change);
      altered.kitHash = await sha256Hex(canonicalJson(altered.packet));snapshot.ordinaryKitSource = altered;
      await expect(readOrdinaryKitFromOriginal({ ...original, startSnapshotJson: JSON.stringify(snapshot) })).rejects.toThrow('ordinary_kit_source_invalid');
    }
  } finally { f.sql.close(); }
});

it('pins the approved Reina barrage for new START while retaining the old single-hit kit', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const { captureOrdinaryKitSource, readOrdinaryKitFromOriginal } = await import('../../apps/game-api/src/combat-ordinary-kit-source');
    const snapshot = JSON.parse(original.startSnapshotJson);
    const current = await readOrdinaryKitFromOriginal(original);
    expect(current.packet.version).toBe('combat.ordinary-five-kit.v2.20261102');
    expect(current.packet.operators.find(o => o.operatorId === 'op_reina')!.abilities[1]).toMatchObject({
      id: 'op_reina.ultimate', effect: 'REINA_BARRAGE', cost: 100, windupMs: 800, recoveryMs: 300,
      impactOffsetsMs: [1400, 2200, 3000, 3800], primaryDamageBps: 25000, additionalDamageBps: 7500,
    });
    const legacy = await captureOrdinaryKitSource(original.origin, original.originHash, snapshot.admission);
    snapshot.ordinaryKitSource = legacy;
    expect(await readOrdinaryKitFromOriginal({ ...original, startSnapshotJson: JSON.stringify(snapshot) })).toEqual(legacy);
    expect(legacy.packet.operators.find(o => o.operatorId === 'op_reina')!.abilities[1]).toMatchObject({ effect: 'DIRECT_DAMAGE', damageBps: 100000 });
  } finally { f.sql.close(); }
});

it('pins all ten current kits to actual START authority and rejects foreign or altered sources', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const kit = await readOrdinaryRunKitSource(f.env.DB, 'a', runId);
    expect(started.ordinaryKitSource).toEqual(kit);
    expect(kit.packet.authority.combatOwner).toBe('UNITY_LOCAL');
    expect(kit.packet.operators).toHaveLength(5);
    expect(kit.packet.operators.flatMap(o => o.abilities)).toHaveLength(10);
    expect(kit.packet.operators.find(o => o.operatorId === 'op_kyle')!.passive).toMatchObject({ group: 'CONDITIONAL_FLAT_DEF_AFTER_STAT_MULTIPLIERS', valueFlat: 83 });
    expect(kit.packet.execution).toBe('REQUIRES_MATCHING_UNITY_HANDLERS');
    f.sql.run("UPDATE account_operators SET promotion=3 WHERE operator_id='op_kyle'");
    expect(await f.startCore()).toEqual(started);
    expect(await readOrdinaryRunKitSource(f.env.DB, 'a', runId)).toEqual(kit);
    await expect(readOrdinaryRunKitSource(f.env.DB, 'b', runId)).rejects.toThrow('run_not_found');
    const row = f.sql.exec('SELECT snapshot_json FROM run_snapshots')[0]!.values[0]![0] as string;
    const changed = JSON.parse(row);
    changed.ordinaryKitSource.packet.operators[0].abilities[0].cooldownMs = 1;
    f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [JSON.stringify(changed), runId]);
    await expect(readOrdinaryRunKitSource(f.env.DB, 'a', runId)).rejects.toThrow('kit_source_invalid');
    delete changed.ordinaryKitSource;
    f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [JSON.stringify(changed), runId]);
    await expect(readOrdinaryRunKitSource(f.env.DB, 'a', runId)).rejects.toThrow('kit_source_unavailable');
  } finally { f.sql.close(); }
});

it('freezes the actual five-operator origin in the same START and replays without current lobby reads', async () => {
  const f = await fixture();
  try {
    const result = await f.startCore(), runId = String(result.runId);
    expect(result).toHaveProperty('ordinaryStatSource');
    const first = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    expect(first.origin.actors).toHaveLength(5);
    expect(first.origin.actors[0]!.permanentStats.maxHp).toBe(1400);
    f.sql.run("UPDATE account_operators SET promotion=3 WHERE operator_id='op_kyle'");
    f.sql.run("UPDATE account_formation SET revision=revision+1");
    expect(await f.startCore()).toEqual(result);
    expect(await (await f.start(f.quote.operationPreviewId, 'original-start')).json()).toEqual(result);
    expect(await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId)).toEqual(first);
    await expect(readOrdinaryRunStatOrigin(f.env.DB, 'b', runId)).rejects.toThrow('run_not_found');
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(1);
  } finally { f.sql.close(); }
});

it('rejects a growth mutation at START commit without a run or fuel debit', async () => {
  const f = await fixture();
  try {
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
      f.sql.run("UPDATE account_operators SET promotion=3,revision=revision+1 WHERE operator_id='op_kyle'"); };
    await expect(f.startCore()).rejects.toThrow();
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(0);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(0);
  } finally { f.sql.close(); }
});

it('applies derived stats from actual current health and equipment in one batch, and recovers the original receipt', async () => {
  const f = await fixture();
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    // Explicit internal HP/effect authority doubles; not a production combat event verifier.
    const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
    await health.initialize('a', runId, original.origin.actors.map(a => ({ id: a.operatorId, hp: a.permanentStats.maxHp, maxHp: a.permanentStats.maxHp })));
    await health.commit('a', runId, { eventId: 'damage', expectedRevision: 1, targetId: 'op_kyle', change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 699 } });
    const proof = { sourceId: 'test-only-effect-owner', version: 'v1', revision: 2, status: 'NO_ACTIVE_STAT_MODIFIERS' };
    const store = new OrdinaryRunStatStore(f.env.DB, { prepare: async () => ({ modifiers: { research: proof, commander: proof, augments: proof, temporary: proof }, assertions: [f.env.DB.prepare('INSERT INTO departure_write_guard(valid) VALUES(1)')] }) });
    const command = { requestId: 'apply', expectedRevision: 0 };
    const batch = f.env.DB.batch.bind(f.env.DB); let loseAck = true;
    f.env.DB.batch = async statements => { const result = await batch(statements);
      if (loseAck) { loseAck = false; throw new Error('test-only lost committed acknowledgement'); } return result; };
    const result = await store.apply('a', runId, command);
    expect(result.stats.actors[0]).toMatchObject({ operatorId: 'op_kyle', currentHp: 699, effectiveStats: { defense: { numerator: '333', denominator: '1' } } });
    expect((await health.read('a', runId)).state.targets.find(t => t.id === 'op_kyle')!.health.hp).toBe(699);
    expect(await new OrdinaryRunStatStore(f.env.DB).apply('a', runId, command)).toEqual(result);
    const staleProof = { ...proof, revision: 1 };
    const reopened = new OrdinaryRunStatStore(f.env.DB, { prepare: async () => ({ modifiers: { research: proof, commander: proof, augments: staleProof, temporary: proof }, assertions: [f.env.DB.prepare('INSERT INTO departure_write_guard(valid) VALUES(1)')] }) });
    await expect(reopened.apply('a', runId, { requestId: 'old-modifier', expectedRevision: 1 })).rejects.toThrow('source_revision_stale');
    await expect(new OrdinaryRunStatStore(f.env.DB).apply('a', runId, { requestId: 'missing', expectedRevision: 1 })).rejects.toThrow('modifier_source_unavailable');
    const racing = new OrdinaryRunStatStore(f.env.DB, { prepare: async () => {
      await health.commit('a', runId, { eventId: 'more-damage', expectedRevision: 2, targetId: 'op_kyle', change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 600 } });
      return { modifiers: { research: proof, commander: proof, augments: proof, temporary: proof }, assertions: [f.env.DB.prepare('INSERT INTO departure_write_guard(valid) VALUES(1)')] };
    } });
    await expect(racing.apply('a', runId, { requestId: 'racing', expectedRevision: 1 })).rejects.toThrow('source_changed');
    expect(f.scalar('SELECT revision FROM ordinary_run_stat_states')).toBe(1);
    expect(f.scalar('SELECT count(*) FROM ordinary_run_stat_receipts')).toBe(1);
    expect((await health.read('a', runId)).state.targets.find(t => t.id === 'op_kyle')!.health.hp).toBe(600);
    f.sql.run("UPDATE runs SET status='FINISH_PENDING'");
    expect(await new OrdinaryRunStatStore(f.env.DB).apply('a', runId, command)).toEqual(result);
    await expect(store.apply('a', runId, { requestId: 'inactive', expectedRevision: 1 })).rejects.toThrow('source_changed');
    expect(f.scalar('SELECT revision FROM ordinary_run_stat_states')).toBe(1);
  } finally { f.sql.close(); }
});

it('restores a START above the former envelope limit without consuming twice', async () => {
  const f = await fixture(true);
  let restored: InstanceType<Awaited<ReturnType<typeof initSqlJs>>['Database']> | undefined;
  try {
    const row = (await f.env.DB.prepare('SELECT * FROM operation_previews WHERE id=?').bind(f.quote.operationPreviewId)
      .first<Parameters<typeof startDeparture>[2]>())!;
    const started = await startDeparture(f.env.DB, 'a', row, 'larger-start', async (p, runId) => ({
      accepted: true, startStatus: 'STARTED', runId, operationPreviewId: p.id, encounterType: 'ORDINARY_15_WAVE',
      existingResponseValue: 'a'.repeat(30000),
    }), undefined, f.env.ENVIRONMENT);
    expect(JSON.stringify(JSON.stringify(started)).length).toBeGreaterThan(65536);
    expect(() => assertOrdinaryStartResponseSize(started)).not.toThrow();
    const SQL = await initSqlJs(); restored = new SQL.Database(f.sql.export());
    const db = new SqlJsD1Database(restored) as unknown as D1Database;
    const before = Buffer.from(restored.export());
    const recovered = await startDeparture(db, 'a', row, 'larger-start', async () => { throw new Error('must replay stored receipt'); }, undefined, f.env.ENVIRONMENT);
    expect(recovered).toEqual(started);
    expect(Buffer.from(restored.export()).equals(before)).toBe(true);
  } finally { restored?.close(); f.sql.close(); }
});

it.each(['base', 'prepared'])('rejects an oversized START before any consumption or receipt is committed (%s)', async stage => {
  const f = await fixture(true);
  try {
    const row = (await f.env.DB.prepare('SELECT * FROM operation_previews WHERE id=?').bind(f.quote.operationPreviewId)
      .first<Parameters<typeof startDeparture>[2]>())!;
    const before = f.sql.export();
    await expect(startDeparture(f.env.DB, 'a', row, 'oversized-start', async (p, runId) => ({
      accepted: true, startStatus: 'STARTED', runId, operationPreviewId: p.id, encounterType: 'ORDINARY_15_WAVE',
      ...(stage === 'base' ? { existingResponseValue: '가'.repeat(44000) } : {}),
    }), undefined, f.env.ENVIRONMENT, undefined, stage === 'prepared' ? {
      prepare: async (_accountId, _runId, snapshotJson) => {
        const snapshot = JSON.parse(snapshotJson);
        const response = { ...snapshot.departureResponse, existingPreparedValue: '가'.repeat(44000) };
        return { snapshotJson: JSON.stringify({ ...snapshot, departureResponse: response }), response, assertions: [] };
      },
    } : undefined)).rejects.toThrow('ordinary_start_response_too_large');
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(0);
  } finally { f.sql.close(); }
});

it('resolves v8 static references, preserves v6/v7 and fits escaped maximum origin identity lengths', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', started.runId);
    const source = await captureFirstBattleEncounterSourceV7(original.origin, original.originHash, 'I');
    const compact = await captureFirstBattleEncounterSourceV8(original.origin, original.originHash, 'I');
    expect(await readFirstBattleEncounterFromOriginal(original)).toEqual(compact);
    expect(await readFirstBattleEncounterFromOriginal({ ...original, startSnapshotJson: JSON.stringify({
      ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: source }) })).toEqual(source);
    const staticDefinition = await firstBattleStaticDefinition(original.origin, original.originHash, 'I');
    expect(compact.packet.definitionRef).toEqual({ version: staticDefinition.version, definitionHash: staticDefinition.definitionHash });
    expect(await resolveFirstBattleProfile(compact)).toEqual(source.packet.definition);
    const changedReference = structuredClone(compact); changedReference.packet.definitionRef.definitionHash = '0'.repeat(64);
    changedReference.sourceHash = await sha256Hex(canonicalJson(changedReference.packet));
    await expect(readFirstBattleEncounterFromOriginal({ ...original, startSnapshotJson: JSON.stringify({
      ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: changedReference }) })).rejects.toThrow('first_battle_source_invalid');
    expect(source.packet.definition.scaling).not.toHaveProperty('variants');
    expect(source.packet).not.toHaveProperty('references');
    const historical = await captureFirstBattleEncounterSourceV6(original.origin, original.originHash, 'I');
    expect(await readFirstBattleEncounterFromOriginal({ ...original, startSnapshotJson: JSON.stringify({
      ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: historical }) })).toEqual(historical);
    expect(source.packet.definition.scaling.definitionHash).toBe((await firstPlayableProfileV3()).scaling.definitionHash);
    for (const identity of ['x'.repeat(128), '\\'.repeat(128), '"'.repeat(128)]) {
      const maximumIdentityProjection = JSON.parse(JSON.stringify(started, (key, value) =>
        ['accountId', 'runId'].includes(key) ? identity : value));
      expect(() => assertOrdinaryStartResponseSize(maximumIdentityProjection)).not.toThrow();
    }
    const bad = structuredClone(source);
    bad.packet.definition.enemies.spatial.definitionHash = '0'.repeat(64);
    const { definitionHash: ignored, ...definition } = bad.packet.definition; void ignored;
    bad.packet.definition.definitionHash = await sha256Hex(canonicalJson(definition));
    bad.sourceHash = await sha256Hex(canonicalJson(bad.packet));
    await expect(readFirstBattleEncounterFromOriginal({ ...original, startSnapshotJson: JSON.stringify({
      ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: bad }) })).rejects.toThrow('first_battle_source_invalid');
  } finally { f.sql.close(); }
});

it('binds the numeric learning profile to START while retaining historical contracts and execution gates', async () => {
  const f = await fixture(true);
  try {
    const body = await f.startCore();
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', body.runId);
    const source = await captureFirstBattleEncounterSourceV6(original.origin, original.originHash, 'I');
    expect(source.packet).toMatchObject({ accountId: 'a', runId: body.runId, originHash: original.originHash,
      formationRevision: original.origin.formationRevision, status: 'PLACEMENT_PROFILE_ISSUED_RUNTIME_UNBOUND',
      eligibility: 'NOT_EVALUATED', canStart: false });
    expect(source.sourceHash).toBe(await sha256Hex(canonicalJson(source.packet)));
    expect(await readFirstBattleEncounterFromOriginal({ ...original, startSnapshotJson: JSON.stringify({
      ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: source }) })).toEqual(source);
    expect(source.packet.entryRequirements).toMatchObject({
      required: ['SPEED_CONTROL', 'BRANCH_SELECTION', 'CONNECTED_ENCOUNTER_DEFINITION', 'REQUIRED_ASSETS'],
      optional: ['SHIELD_USE', 'TRAIN_FIRE_USE', 'ULTIMATE_USE'],
      optionalUnlockOrChargeRequired: false, additionalOperatorRequirements: [],
    });
    const v5 = await captureFirstBattleEncounterSourceV5(original.origin, original.originHash, 'I');
    expect(await readFirstBattleEncounterFromOriginal({ ...original,
      startSnapshotJson: JSON.stringify({ ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: v5 }) })).toEqual(v5);
    expect(v5.packet.definition.completion).toBe('SLICE_ONLY_NOT_FULL_LEARNING_VICTORY_OR_REWARD_AUTHORITY');
    const v4 = await captureFirstBattleEncounterSourceV4(original.origin, original.originHash, 'I');
    expect(await readFirstBattleEncounterFromOriginal({ ...original,
      startSnapshotJson: JSON.stringify({ ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: v4 }) })).toEqual(v4);
    const v3 = await captureFirstBattleEncounterSourceV3(original.origin, original.originHash);
    expect(await readFirstBattleEncounterFromOriginal({ ...original,
      startSnapshotJson: JSON.stringify({ ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: v3 }) })).toEqual(v3);
    const v2 = await captureFirstBattleEncounterSourceV2(original.origin, original.originHash);
    expect(await readFirstBattleEncounterFromOriginal({ ...original,
      startSnapshotJson: JSON.stringify({ ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: v2 }) })).toEqual(v2);
    expect(source.packet.definition.profileId).toBe('first_battle_forward_gunners_single_artillery');
    expect(source.packet.definition.completion).toMatchObject({ version: 'combat.first-objective.v1.20261028' });
    const objective = await firstEncounterObjectiveDefinition();
    expect(source.packet.definition.completion.definitionHash).toBe(objective.definitionHash);
    expect(objective.required).toEqual(['TWO_ORIGINAL_GUNNERS_DEFEATED', 'ORIGINAL_SINGLE_ARTILLERY_RESOLVED', 'TRAIN_ALIVE']);
    expect(objective.artillery).toMatchObject({ resolvedOutcomes: ['HIT', 'MISSED', 'CANCELLED_BEFORE_LAUNCH'],
      unresolvedLaunchedProjectiles: 0, absentHazardIsResolved: false, launcherDestructionRequired: false,
      exhaustedLauncherMayFireAgain: false, operatorDirectAttack: false });
    expect(objective.timeOrRailEndCompletesObjective).toBe(false);
    expect(objective.evidence.clientCompletedBooleanAccepted).toBe(false);
    const changedObjective = structuredClone(source);
    changedObjective.packet.definition.completion.definitionHash = '0'.repeat(64);
    const { definitionHash: ignoredHash, ...changedDefinition } = changedObjective.packet.definition;
    void ignoredHash;
    changedObjective.packet.definition.definitionHash = await sha256Hex(canonicalJson(changedDefinition));
    changedObjective.sourceHash = await sha256Hex(canonicalJson(changedObjective.packet));
    await expect(readFirstBattleEncounterFromOriginal({ ...original,
      startSnapshotJson: JSON.stringify({ ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: changedObjective }) })).rejects.toThrow('first_battle_source_invalid');

    expect(source.packet.definition.enemies).toMatchObject({ count: 2, baseHpEach: 240, baseHpTotal: 480, defense: 53 });
    expect(source.packet.definition.artillery).toMatchObject({ baseHp: 1000, baseRawDamageIntent: 50, damageApplicationsPerEvent: 1,
      hazardVersion: 'rail.artillery-hazard.v2.20261027', dangerLengthMm: 6000, launchAfterPreviewMs: 7500, impactAfterPreviewMs: 8000, flightMs: 500 });
    expect(source.packet.definition.minimumThreatClearToPreviewMs).toBe(2000);
    expect(source.packet.definition.scaling.variants).toHaveLength(15);
    expect(source.packet.appliedScaling).toBeNull();
    expect(source.packet.runtimeSupported).toBe(false);
    const { definitionHash, ...profileDefinition } = source.packet.definition;
    expect(definitionHash).toBe(await sha256Hex(canonicalJson(profileDefinition)));
    expect(source.packet.definition.enemies.definition).toMatchObject({ definitionId: 'enemy_spike_cart_gunner',
      rawAction: { value: { numerator: '20', denominator: '1' }, riskAppliedVersion: null, progressAppliedVersion: null } });
    expect(Buffer.byteLength(canonicalJson(body), 'utf8')).toBeLessThanOrEqual(65536);
    expect(source.packet.definition.scaling.variants.find(v => v.risk === 'II' && v.progress === 'MIDDLE')).toMatchObject({
      gunnerHp: { numerator: '300', denominator: '1' }, gunnerRawDamage: { numerator: '253', denominator: '10' },
      artilleryRawDamage: { numerator: '253', denominator: '4' } });
    expect(source.packet.definition.scaling.variants.find(v => v.risk === 'I' && v.progress === 'EARLY')).toMatchObject({
      gunnerHp: { numerator: '240', denominator: '1' }, gunnerRawDamage: { numerator: '20', denominator: '1' },
      launcherHp: { numerator: '1000', denominator: '1' }, artilleryRawDamage: { numerator: '50', denominator: '1' } });
    expect(source.packet.definition.scaling.variants.find(v => v.risk === 'V' && v.progress === 'LATE')).toMatchObject({
      gunnerHp: { numerator: '552', denominator: '1' }, gunnerRawDamage: { numerator: '42', denominator: '1' },
      launcherHp: { numerator: '2300', denominator: '1' }, artilleryRawDamage: { numerator: '105', denominator: '1' } });
    const modified = structuredClone(source); modified.packet.definition.enemies.baseHpEach = 999;
    modified.sourceHash = await sha256Hex(canonicalJson(modified.packet));
    await expect(readFirstBattleEncounterFromOriginal({ ...original,
      startSnapshotJson: JSON.stringify({ firstBattleEncounterSource: modified }) })).rejects.toThrow('first_battle_source_invalid');
    const v1 = await captureFirstBattleEncounterSource(original.origin, original.originHash);
    expect(await readFirstBattleEncounterFromOriginal({ ...original,
      startSnapshotJson: JSON.stringify({ ...JSON.parse(original.startSnapshotJson), firstBattleEncounterSource: v1 }) })).toEqual(v1);
    const historical = JSON.parse(original.startSnapshotJson); delete historical.firstBattleEncounterSource;
    expect(await readFirstBattleEncounterFromOriginal({ ...original, startSnapshotJson: JSON.stringify(historical) })).toBeNull();
    for (const forged of [null, { ...source, packet: { ...source.packet, eligibility: 'ELIGIBLE', canStart: true } }]) {
      if (forged) forged.sourceHash = await sha256Hex(canonicalJson(forged.packet));
      await expect(readFirstBattleEncounterFromOriginal({ ...original,
        startSnapshotJson: JSON.stringify({ ...historical, firstBattleEncounterSource: forged }) })).rejects.toThrow('first_battle_source_invalid');
    }
  } finally { f.sql.close(); }
});

it('rejects changed learning profiles before any initialization write and preserves source-absent history', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const snapshot = JSON.parse(original.startSnapshotJson);
    const replace = () => {
      const value = JSON.stringify(snapshot);
      f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [value, runId]);
      f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
      f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, value]);
    };
    snapshot.firstBattleEncounterSource = await captureFirstBattleEncounterSourceV7(original.origin, original.originHash, snapshot.risk);
    snapshot.firstBattleEncounterSource.packet.definition.artillery.damageApplicationsPerEvent = 5;
    snapshot.firstBattleEncounterSource.sourceHash = await sha256Hex(canonicalJson(snapshot.firstBattleEncounterSource.packet));
    replace();
    const before = f.sql.export();
    await expect(new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId,
      { requestId: 'bad-learning-init', source: 'ORIGINAL_START' })).rejects.toThrow('first_battle_source_invalid');
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_combat_activation_receipts')).toBe(0);
    delete snapshot.firstBattleEncounterSource;
    replace();
    const receipt = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId,
      { requestId: 'historical-learning-absent', source: 'ORIGINAL_START' });
    expect(receipt).not.toHaveProperty('firstBattleEncounterBinding');
  } finally { f.sql.close(); }
});

it('does not promote claimed learning steps or help choices into authoritative completion or rewards', async () => {
  const f = await fixture(true);
  try {
    const runId = String((await f.startCore()).runId);
    await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'learning-init', source: 'ORIGINAL_START' });
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const staticDefinition = await firstBattleStaticDefinition(original.origin, original.originHash, JSON.parse(original.startSnapshotJson).risk);
    const before = f.sql.export();
    for (const completedStages of [[], staticDefinition.body.sequence]) {
      // Deliberately untrusted client claims, not a proposed checkpoint schema or real stage proof.
      const response = await app.request(`http://localhost/v1/runs/${runId}/finish`, {
        method: 'POST', headers: { ...f.headers, 'Idempotency-Key': `learning-finish-${completedStages.length}` },
        body: JSON.stringify({ runId, currentStage: 'Complete', currentWaveNumber: 0, completedWaveCount: 15,
          outcome: 'VICTORY', runtimeStateJson: JSON.stringify({ completedStages, helpSkipped: true,
            ultimateUsed: false, damageTaken: 1 }), tacticSelections: [], clientCompletedAt: new Date().toISOString() }),
      }, f.env);
      expect(response.status).toBe(409);
      expect(await response.json()).toMatchObject({ errorCode: 'combat_finish_verification_required' });
    }
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.scalar('SELECT COUNT(*) FROM run_final_results')).toBe(0);
    expect(f.scalar('SELECT COUNT(*) FROM run_reward_previews')).toBe(0);
  } finally { f.sql.close(); }
});

it.each([6, 7, 8])('binds approved scaling in the verified arrival transaction and replays the original encounter band v%s', async version => {
  const f = await fixture(true);
  try {
    let bodyRegistry: OrdinaryNativeGeometryRegistry | undefined;
    if (version === 8) {
      const body = await firstEnemyTrainBodyDefinition();
      const bytes = JSON.stringify({ version: 'ordinary.native-geometry-definition.v1.20261031', definitionId: 'synthetic-body-source',
        units: 'MILLIMETERS', millimetersPerUnit: 1000, axis: 'X_FORWARD_Y_UP_Z_SIDE', coordinateSpace: 'BOUND_FRAME_LOCAL',
        source: { scenePath: 'Assets/Test.unity', sceneHash: 'a'.repeat(64) },
        entities: [{ authoringId: 'EnemyBody', role: 'TRAIN', definitionRef: body.definitionId, framePath: 'Root/EnemyBody',
          assetPath: body.assetPath, assetHash: body.assetHash, anchors: [],
          shapes: [{ id: 'body', role: 'OCCLUDER', kind: 'BOX', centerMm: [0,1600,0], sizeMm: [12000,3200,3000] }] }] });
      bodyRegistry = new OrdinaryNativeGeometryRegistry(f.env.DB, { definitionId: 'synthetic-body-source',
        version: 'ordinary.native-geometry-definition.v1.20261031', definitionHash: await sha256Hex(bytes) });
      await bodyRegistry.register(bytes);
    }
    const runId = String((await f.startCore(bodyRegistry)).runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    await expect(new OperationMapStore(f.env.DB).initialize('a', runId)).rejects.toThrow('operation_map_profile_unsupported');
    const source = await (version === 8 ? captureFirstBattleEncounterSourceV8 : version === 7 ? captureFirstBattleEncounterSourceV7 : captureFirstBattleEncounterSourceV6)(original.origin, original.originHash, 'II');
    const profile = (await resolveFirstBattleProfile(source))!, plan = mapFixture();
    for (const node of plan.nodes) { node.encounterProfileRef = node.eventKind === 'COMBAT' ? profile.profileId : `fixture-${node.eventKind}`; node.rewardPlanRef = `fixture-reward-${node.eventKind}`; }
    const kinds = [...new Set(plan.nodes.map(n => n.eventKind))];
    const publication = await new OperationMapPublisher({ read: async () => ({ version: 'operation.map-catalog.v1',
      catalogId: 'SYNTHETIC_AUTHORED_LEARNING_TEST', revision: 1, definitionVersion: plan.definitionVersion,
      encounters: kinds.map(eventKind => ({ ref: eventKind === 'COMBAT' ? profile.profileId : `fixture-${eventKind}`,
        version: eventKind === 'COMBAT' ? profile.version : 'fixture-v1', profile: 'ORDINARY_12_NODE', eventKind,
        definition: eventKind === 'COMBAT' ? profile : {} })),
      rewards: kinds.map(eventKind => ({ ref: `fixture-reward-${eventKind}`, version: 'fixture-v1', profile: 'ORDINARY_12_NODE', eventKind, definition: {} })) }) }).publish(plan);
    const snapshot = { ...JSON.parse(original.startSnapshotJson), risk: 'II', encounterType: 'ORDINARY_12_NODE',
      firstBattleEncounterSource: source, operationMapContent: publication };
    const preview = await f.env.DB.prepare('SELECT objective,balance_config_version FROM operation_previews WHERE id=(SELECT preview_id FROM runs WHERE id=?)')
      .bind(runId).first<{ objective: string; balance_config_version: number }>();
    snapshot.operationId = preview!.objective; snapshot.balanceVersion = preview!.balance_config_version;
    snapshot.admission.operationMapPlan = plan;
    const text = JSON.stringify(snapshot);
    f.sql.run('UPDATE operation_previews SET risk=? WHERE id=(SELECT preview_id FROM runs WHERE id=?)', ['II', runId]);
    f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [text, runId]);
    f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
    f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, text]);
    await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'scaling-init', source: 'ORIGINAL_START' });
    // This SQL fixture supplies synthetic arrival evidence, never a product movement proof.
    const store = new OperationMapStore(f.env.DB, { prepare: async context => ({
      proof: { kind: context.command.kind, verificationId: 'fixture-arrival-proof', holdIds: [] },
      assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')] }) });
    await store.initialize('a', runId);
    const entryBytes = f.sql.export();
    await expect(store.commit('a', runId, { kind: 'ARRIVE', edgeId: plan.edges[0]!.edgeId,
      evidenceRef: 'fixture', eventId: 'unvisited', expectedRevision: 1 })).rejects.toThrow('operation_map_phase_invalid');
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(entryBytes))).toBe(true);
    let current = plan.entryNodeId; const traversed: string[] = [];
    for (let depth = 1; depth <= 11; depth++) {
      const edge = plan.edges.find(e => e.fromNodeId === current && e.toNodeId === `n${depth}-0`)!;
      const state = { version: 'operation.map-progress.v1', traversedEdgeIds: [...traversed], pendingEdgeId: edge.edgeId, phase: 'TRAVELLING', holdIds: [] };
      f.sql.run('UPDATE operation_map_states SET revision=?,state_json=? WHERE run_id=?', [depth * 2, canonicalJson(state), runId]);
      const command = { kind: 'ARRIVE', edgeId: edge.edgeId, evidenceRef: 'fixture', eventId: `arrive-${depth}`, expectedRevision: depth * 2 };
      const batch = f.env.DB.batch.bind(f.env.DB);
      if (depth === 8) {
        f.env.DB.batch = async statements => {
          f.sql.run('UPDATE operation_previews SET risk=? WHERE id=(SELECT preview_id FROM runs WHERE id=?)', ['III', runId]);
          return batch(statements);
        };
        try { await expect(store.commit('a', runId, command)).rejects.toThrow('operation_map_state_conflict'); }
        finally { f.env.DB.batch = batch; }
        expect(await store.lookup('a', runId, command.eventId)).toBeNull();
        expect((await store.read('a', runId)).revision).toBe(depth * 2);
        f.sql.run('UPDATE operation_previews SET risk=? WHERE id=(SELECT preview_id FROM runs WHERE id=?)', ['II', runId]);
      }
      if (depth === 5) f.env.DB.batch = async statements => { await batch(statements); throw new Error('committed_response_lost'); };
      const receipt = await store.commit('a', runId, command).finally(() => { f.env.DB.batch = batch; });
      if (version >= 7) expect(receipt.firstPlayableScaling!.enemySpatial?.physicalInstanceId).toBe(await firstEnemyTrainInstanceId({
        accountId: 'a', runId, encounterId: command.eventId, sourceHash: source.sourceHash }));
      else expect(receipt.firstPlayableScaling).not.toHaveProperty('enemySpatial');
      if (depth === 1) {
        const completion = { kind: 'COMPLETE', nodeId: 'n1-0', evidenceRef: 'objective-proof', eventId: 'first-complete', expectedRevision: receipt.revision };
        const noEvidence = new OperationMapStore(f.env.DB, new OrdinaryFirstObjectiveVerifier(f.env.DB));
        const authNow = Date.now();
        f.sql.run("INSERT INTO auth_sessions(id,account_id,refresh_token_hash,family_id,generation,status,created_at,expires_at) VALUES('completion-sb','b','completion-hb','completion-fb',0,'ACTIVE',?,?)", [authNow, authNow + 60000]);
        const unchanged = f.sql.export();
        const completionBody = { nodeId: completion.nodeId, evidenceRef: completion.evidenceRef, expectedRevision: completion.expectedRevision };
        const completionRequest = (target = app, body: unknown = completionBody, headers = f.headers) => target.request(
          `http://localhost/v1/runs/${runId}/operation-map/completion`, { method: 'POST',
            headers: { ...headers, 'Content-Type': 'application/json', 'Idempotency-Key': completion.eventId }, body: JSON.stringify(body) }, f.env);
        const unavailable = await completionRequest();
        expect(unavailable.status).toBe(409);
        expect(await unavailable.json()).toMatchObject({ accepted: false, errorCode: 'ordinary_objective_evidence_unavailable' });
        expect((await completionRequest(app, { ...completionBody, completed: true })).status).toBe(400);
        const anonymous = await app.request(`http://localhost/v1/runs/${runId}/operation-map/completion`, {
          method: 'POST', headers: { 'Content-Type': 'application/json', 'Idempotency-Key': completion.eventId }, body: JSON.stringify(completionBody) }, f.env);
        expect(anonymous.status).toBe(401);
        const foreignHeaders = { ...f.headers, Authorization: `Bearer ${await createAccessToken({ secret: f.env.AUTH_SIGNING_KEY_CURRENT,
          accountId: 'b', sessionId: 'completion-sb', nowMs: authNow, ttlMs: 60000 })}` };
        const foreignResponse = await completionRequest(app, completionBody, foreignHeaders);
        expect(foreignResponse.status).toBe(409);
        expect(await foreignResponse.json()).toMatchObject({ errorCode: 'operation_map_run_not_found' });
        await expect(noEvidence.commit('a', runId, completion)).rejects.toThrow('ordinary_objective_evidence_unavailable');
        expect(Buffer.from(f.sql.export()).equals(Buffer.from(unchanged))).toBe(true);
        const objective = await firstEncounterObjectiveDefinition();
        const objectiveRef = { version: objective.version, definitionHash: objective.definitionHash };
        const issued = await firstObjectiveEncounterIdentities({ accountId: 'a', runId, encounterId: command.eventId, sourceHash: source.sourceHash });
        const healthBefore = (await f.env.DB.prepare('SELECT state_json FROM combat_damage_states WHERE run_id=?').bind(runId).first<{ state_json: string }>())!.state_json;
        const targets = JSON.parse(healthBefore).targets.map((t: { id: string; health: { hp: number; maxHp: number } }) => ({ targetId: t.id, hp: t.health.hp, maxHp: t.health.maxHp }));
        const evidence = { version: 'combat.first-objective-evidence.v1.20261028', accountId: 'a', runId, nodeId: 'n1-0',
          encounterId: command.eventId, evidenceRef: completion.evidenceRef, mapRevision: receipt.revision,
          startSnapshotHash: await sha256Hex(text), sourceHash: source.sourceHash, profileHash: profile.definitionHash,
          objective: objectiveRef, nodeObjectives: [objectiveRef], checkpoint: { revision: 7, hash: 'a'.repeat(64), atMs: 20000 },
          ...issued, defeats: [{ gunnerId: issued.issuedGunnerIds[0], eventId: 'defeat-a', atMs: 1000 }, { gunnerId: issued.issuedGunnerIds[1], eventId: 'defeat-b', atMs: 2000 }],
          hazard: { hazardId: issued.issuedHazardId, eventId: 'impact', outcome: 'MISSED', launchedAtMs: 19500, resolvedAtMs: 20000 },
          targets, unresolvedProjectileIds: [], remainingThreatIds: [], ultimateAuto: true,
          actions: original.origin.actors.map(a => ({ operatorId: a.operatorId,
            activeCooldownRemainingTicks: cooldownNodeStepsToTimeSpan(80, 500000), ultimateCharge: 70 })) };
        // Synthetic accepted evidence owner, explicitly not a client-proof production adapter.
        f.sql.run('CREATE TABLE test_objective_evidence (id INTEGER PRIMARY KEY, original_json TEXT)');
        const evidenceText = JSON.stringify(evidence);
        f.sql.run('INSERT INTO test_objective_evidence VALUES(1,?)', [evidenceText]);
        f.sql.run('CREATE TABLE test_objective_holds (pending INTEGER)');
        f.sql.run('INSERT INTO test_objective_holds VALUES(1)');
        let candidate: unknown = evidence, calls = 0, supportAttribution: unknown, artilleryReplay: unknown, reinaBarrages: unknown, gunnerHealthReplay: unknown, executionBinding: unknown, sceneObservations: unknown;
        const evidenceOwner = { prepare: async () => {
          calls++;
          return { evidence: structuredClone(candidate), ...(supportAttribution === undefined ? {} : { supportAttribution }),
            ...(gunnerHealthReplay === undefined ? {} : { gunnerHealthReplay }),
            ...(executionBinding === undefined ? {} : { executionBinding }),
            ...(sceneObservations === undefined ? {} : { sceneObservations }),
            ...(artilleryReplay === undefined ? {} : { artilleryReplay }), ...(reinaBarrages === undefined ? {} : { reinaBarrages }), holdIds: ['pending-reward'], assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM test_objective_evidence WHERE id=1 AND original_json=?) THEN 1 ELSE 0 END').bind(evidenceText),
            f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN (SELECT pending FROM test_objective_holds)=1 THEN 1 ELSE 0 END')] };
        } };
        const verifier = new OrdinaryFirstObjectiveVerifier(f.env.DB, evidenceOwner);
        const complete = new OperationMapStore(f.env.DB, verifier);
        supportAttribution = { configurationHash: 'a'.repeat(64), actions: [], rows: [{ sourceId: 'forged' }] };
        await expect(complete.commit('a', runId, completion)).rejects.toThrow('ordinary_objective_support_invalid');
        expect(await complete.lookup('a', runId, completion.eventId)).toBeNull();
        supportAttribution = undefined;
        artilleryReplay = {};
        await expect(complete.commit('a', runId, completion)).rejects.toThrow('ordinary_objective_artillery_invalid');
        expect(await complete.lookup('a', runId, completion.eventId)).toBeNull();
        artilleryReplay = undefined;
        reinaBarrages = [{}];
        await expect(complete.commit('a', runId, completion)).rejects.toThrow('ordinary_objective_reina_barrage_invalid');
        expect(await complete.lookup('a', runId, completion.eventId)).toBeNull();
        reinaBarrages = undefined;
        // Body occlusion and artillery issuance never count as either original gunner's defeat.
        const bodyInstanceId = await firstEnemyTrainInstanceId({ accountId: 'a', runId,
          encounterId: command.eventId, sourceHash: source.sourceHash });
        for (const mutation of ['stale-hp', 'body-as-gunner', 'artillery-as-gunner', 'gunner-as-artillery', 'account', 'encounter', 'source', 'issued', 'additional', 'projectile', 'threat', 'dead', 'duplicate', 'foreign-target', 'future', 'hazard-missing', 'cancel-launched']) {
          const bad = JSON.parse(evidenceText);
          if (mutation === 'stale-hp') bad.targets[0].hp--;
          if (mutation === 'body-as-gunner') bad.defeats[0].gunnerId = bodyInstanceId;
          if (mutation === 'artillery-as-gunner') bad.defeats[0].gunnerId = issued.issuedHazardId;
          if (mutation === 'gunner-as-artillery') bad.hazard.hazardId = issued.issuedGunnerIds[0];
          if (mutation === 'account') bad.accountId = 'other';
          if (mutation === 'encounter') bad.encounterId = 'other-arrival';
          if (mutation === 'source') bad.sourceHash = '0'.repeat(64);
          if (mutation === 'issued') { bad.issuedGunnerIds[0] = 'invented'; bad.defeats[0].gunnerId = 'invented'; }
          if (mutation === 'additional') bad.nodeObjectives.push({ ...objectiveRef, version: 'another-objective' });
          if (mutation === 'projectile') bad.unresolvedProjectileIds.push('in-flight');
          if (mutation === 'threat') bad.remainingThreatIds.push('alive');
          if (mutation === 'dead') bad.targets.find((t: { targetId: string }) => t.targetId === 'train_a').hp = 0;
          if (mutation === 'duplicate') bad.defeats[1] = bad.defeats[0];
          if (mutation === 'foreign-target') bad.targets[0].targetId = 'foreign';
          if (mutation === 'future') bad.hazard.resolvedAtMs++;
          if (mutation === 'hazard-missing') bad.hazard = null;
          if (mutation === 'cancel-launched') bad.hazard.outcome = 'CANCELLED_BEFORE_LAUNCH';
          candidate = bad; const before = f.sql.export();
          await expect(complete.commit('a', runId, completion)).rejects.toThrow('ordinary_objective_');
          expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
        }
        for (const field of ['ultimateAuto', 'actions']) {
          const missing = JSON.parse(evidenceText); delete missing[field]; candidate = missing;
          const before = f.sql.export();
          await expect(complete.commit('a', runId, completion)).rejects.toThrow('automation_source_conflict');
          expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
        }
        const ctx = { accountId: 'a', runId, startSnapshot: text, plan, state: receipt.state,
          command: { ...completion, kind: 'COMPLETE' as const } };
        const supportReceipt = { eventId: 'heal', targetId: 'op_kyle', tick: 8, hpAfter: 100,
          hpLoss: 0, absorbed: 0, hpGain: 20, derivedExcess: 0, shieldAdded: 0 };
        const supportRow = { id: await sha256Hex(canonicalJson([runId, 'a'.repeat(64), 1, 'heal', 'op_sera.active', 0, 'op_kyle', 0, 'HEAL'])),
          runId, profileSha256: 'a'.repeat(64), configurationSha256: 'a'.repeat(64), journalSequence: 1,
          globalSequence: 1, commandId: 'heal', abilityId: 'op_sera.active', effectIndex: 0, targetId: 'op_kyle',
          pulseIndex: 0, kind: 'HEAL', tick: 8, sourceId: 'op_sera', sourceKind: 'SKILL', actualResult: { actualHpGain: 20 },
          nodeId: completion.nodeId, attemptGeneration: 1, executorId: 'op_sera', healthReceipt: supportReceipt };
        supportAttribution = { configurationHash: 'a'.repeat(64), actions: [{ id: 'heal', actor: 'op_sera', ability: 'op_sera.active',
          target: 'op_kyle', targets: ['op_kyle'], due: 8, status: 'APPLIED', healthReceipt: supportReceipt }], rows: [supportRow] };
        candidate = { ...evidence, attemptGeneration: 1 };
        const supportContext = { ...ctx, command: { ...ctx.command, attemptGeneration: 1 } };
        expect((await verifier.prepare(supportContext)).proof.kind).toBe('COMPLETE');
        supportRow.executorId = 'other';
        await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_support_attribution_conflict');
        supportAttribution = undefined;
        const { ExactRailArtilleryHazard } = await import('../../apps/game-api/src/rail-artillery-hazard');
        const { resolveNumericDefenseBudget } = await import('../../apps/game-api/src/combat-numeric-defense');
        const hazardDefinition = { version: 'rail.artillery-hazard.v2.20261027', runId,
          eventId: issued.issuedHazardId, trainId: targets.find((t: { targetId: string }) => !original.origin.actors.some(a => a.operatorId === t.targetId))!.targetId,
          railSegmentId: 'main', carriageIds: ['synthetic-tail'], startMm: 100, endMm: 6100, previewAtMs: 12000,
          timing: { kind: 'SINGLE', impactAtMs: 20000 }, damage: receipt.firstPlayableScaling!.applied.artilleryRawDamage };
        const frame = { runId, trainId: hazardDefinition.trainId, atMs: 20000,
          spans: [{ carriageId: 'synthetic-tail', railSegmentId: 'branch', startMm: 100, endMm: 300 }] };
        const numeric = { version: 'combat.numeric-defense-budget.v1', base: hazardDefinition.damage, effects: [],
          defense: { numerator: '0', denominator: '1' }, weakening: [], penetration: [],
          reductions: { equipment: [], guardStance: [], independent: [] } };
        const impact = { request: { frame, numeric, tick: 400, protected: false }, result: {
          collision: new ExactRailArtilleryHazard(hazardDefinition).resolve(frame), calculation: resolveNumericDefenseBudget(numeric),
          hpLoss: 0, absorbed: 0, hpAfter: 100, trainDefeatedNow: false } };
        artilleryReplay = { definition: hazardDefinition, impact, stepMs: 50 };
        expect((await verifier.prepare(supportContext)).proof.kind).toBe('COMPLETE');
        candidate = { ...evidence, attemptGeneration: 1, hazard: { ...evidence.hazard, outcome: 'HIT' } };
        await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_objective_artillery_source_conflict');
        artilleryReplay = undefined;
        for (const outcome of ['HIT', 'MISSED', 'CANCELLED_BEFORE_LAUNCH']) {
          candidate = { ...evidence, hazard: { ...evidence.hazard, outcome, launchedAtMs: outcome === 'CANCELLED_BEFORE_LAUNCH' ? null : 19500 } };
          expect((await verifier.prepare(ctx)).proof.kind).toBe('COMPLETE');
        }
        const { replayReinaBarrage, reinaBarrageChargeCommandId } = await import('../../apps/game-api/src/reina-barrage');
        const barrageInput = { version: 'combat.reina-barrage-replay.v1.20261102', castId: 'fixture-cast', nodeId: completion.nodeId,
          attemptGeneration: 1, acceptedTick: 0, stepTimeSpanTicks: 500000, primary: { id: issued.issuedGunnerIds[0], generation: 1 },
          chargeAtAcceptance: 100, casterAliveAtAcceptance: true, actionEligibleAtAcceptance: true,
          operations: [{ kind: 'CANCEL', tick: 1, reason: 'CASTER_DOWN' }] };
        const barrage = { input: barrageInput, result: replayReinaBarrage(barrageInput) };
        candidate = { ...evidence, attemptGeneration: 1 }; reinaBarrages = [barrage];
        expect((await verifier.prepare(supportContext)).proof.kind).toBe('COMPLETE');
        reinaBarrages = [barrage, barrage];
        await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_objective_reina_barrage_source_conflict');
        const pending = { ...barrageInput, operations: [] };
        reinaBarrages = [{ input: pending, result: replayReinaBarrage(pending) }];
        await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_objective_reina_barrage_source_conflict');
        const committed = { ...barrageInput, operations: [
          { kind: 'COMMIT', tick: 16, casterAlive: true, actionEligible: true, charge: 100,
            attack: { numerator: '100', denominator: '1' }, frame: { id: 'fixture-frame', generation: 1, centerMm: [0, 0, 0] },
            candidates: [{ ...barrageInput.primary, alive: true, kind: 'ENEMY', order: 0, offsetMm: [0, 0, 0],
              muzzleDeltaMm: [1000, 0, 0], castScopeEligible: true, muzzleClear: true, centerClear: true }] },
          { kind: 'CANCEL', tick: 20, reason: 'FRAME_LOST' } ] };
        const committedResult = replayReinaBarrage(committed);
        reinaBarrages = [{ input: committed, result: committedResult }];
        await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_objective_reina_barrage_charge_conflict');
        const { CombatUltimateState } = await import('../../apps/game-api/src/combat-ultimate-state');
        const chargeOwner = CombatUltimateState.start('op_reina');
        for (let i = 0; i < 25; i++) chargeOwner.credit({ actionId: `fixture-basic-${i}`, hitOrdinal: 0,
          sourceActorId: 'op_reina', sourceAlive: true, sourceKind: 'BASIC', mode: 'ORDINARY', resolvedDamage: 1 });
        const before = chargeOwner.snapshot();
        chargeOwner.cast(await reinaBarrageChargeCommandId({ accountId: 'a', runId }, committedResult), true);
        const chargeTransition = { before, after: chargeOwner.snapshot() };
        reinaBarrages = [{ input: committed, result: committedResult, chargeTransition }];
        expect((await verifier.prepare(supportContext)).proof.kind).toBe('COMPLETE');
        reinaBarrages = [{ input: committed, result: committedResult, chargeTransition: { before, after: before } }];
        await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_objective_reina_barrage_charge_conflict');
        reinaBarrages = undefined;
        candidate = evidence;
        gunnerHealthReplay = { events: [], result: {} };
        await expect(verifier.prepare(ctx)).rejects.toThrow('ordinary_objective_gunner_health');
        const { replayOrdinaryGunnerHealth } = await import('../../apps/game-api/src/ordinary-gunner-health-replay');
        const gunnerEvents = evidence.defeats.map(d => ({ eventId: d.eventId, targetId: d.gunnerId, atMs: d.atMs,
          protected: false, shieldRemaining: 0, numeric: { version: 'combat.numeric-defense-budget.v1',
            base: { numerator: '2147483647', denominator: '1' }, defense: { numerator: '0', denominator: '1' },
            effects: [], weakening: [], penetration: [], reductions: { equipment: [], guardStance: [], independent: [] } } }));
        const gunnerResult = replayOrdinaryGunnerHealth({ gunnerIds: issued.issuedGunnerIds,
          hp: receipt.firstPlayableScaling!.applied.gunnerHp }, gunnerEvents);
        candidate = { ...evidence, attemptGeneration: 1 };
        gunnerHealthReplay = { events: gunnerEvents, result: gunnerResult };
        expect((await verifier.prepare(supportContext)).proof.kind).toBe('COMPLETE');
        executionBinding = {};
        await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_execution_binding');
        executionBinding = undefined;
        sceneObservations = [];
        await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_objective_scene_observation_binding_required');
        sceneObservations = undefined;
        if (bodyRegistry) {
          const registered = await bodyRegistry.read(), geometryBinding = { version: 'ordinary.node-geometry-binding.v1.20261031',
            accountId: 'a', runId, nodeId: completion.nodeId, entryMapRevision: receipt.revision, originGeneration: 1,
            definitionRef: registered.reference };
          const execution = issueOrdinaryNodeExecutionOrigin();
          const nodeJson = canonicalJson({ version: 'ordinary.node-entry-state.v1.20261028', accountId: 'a', runId,
            nodeId: completion.nodeId, entryMapRevision: receipt.revision, runtime: { ...execution,
              spatial: { definitionBinding: geometryBinding }, actions: { stepTimeSpanTicks: 500000 } } });
          const snapshotHash = await sha256Hex(nodeJson), body = registered.definition.entities[0]!;
          executionBinding = { ...execution, nodeId: completion.nodeId, attemptGeneration: 1, snapshotHash, geometryBinding };
          const capture = { version: 'ordinary.node-scene-observation.v1.20261102', accountId: 'a', runId,
            nodeId: completion.nodeId, snapshotHash, attemptGeneration: 1, attemptRevision: receipt.revision,
            definitionHash: registered.reference.definitionHash, tick: 1, revision: 1,
            quantization: 'NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO', entities: [{ id: receipt.firstPlayableScaling!.enemySpatial!.physicalInstanceId,
              authoringId: body.authoringId, definitionRef: body.definitionRef, role: body.role, generation: 1,
              present: true, active: true, frame: { originMm: [0,0,0], basis: [[1,0,0],[0,1,0],[0,0,1]] },
              anchors: body.anchors, shapes: body.shapes.map(s => ({ ...s, active: true })) }] };
          f.sql.run('SAVEPOINT native_observation_fixture');
          try {
            f.sql.run('INSERT INTO ordinary_node_attempts VALUES(?,?,?,?,?,?)', [runId, completion.nodeId, text, nodeJson, snapshotHash, 1]);
            sceneObservations = [capture];
            expect((await verifier.prepare(supportContext)).proof.kind).toBe('COMPLETE');
            sceneObservations = [{ ...capture, entities: capture.entities.map(e => ({ ...e, id: 'foreign-body' })) }];
            await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_objective_scene_observation_invalid');
          } finally {
            f.sql.run('ROLLBACK TO native_observation_fixture'); f.sql.run('RELEASE native_observation_fixture');
            sceneObservations = undefined; executionBinding = undefined;
          }
        }
        candidate = { ...evidence, attemptGeneration: 1, defeats: evidence.defeats.map(d => ({ ...d, atMs: d.atMs + 1 })) };
        await expect(verifier.prepare(supportContext)).rejects.toThrow('ordinary_objective_gunner_health_source_conflict');
        gunnerHealthReplay = undefined;
        candidate = evidence;
        f.env.DB.batch = async statements => { f.sql.run("UPDATE test_objective_evidence SET original_json='stale'"); return batch(statements); };
        await expect(complete.commit('a', runId, completion)).rejects.toThrow('operation_map_state_conflict');
        f.env.DB.batch = batch;
        expect(await complete.lookup('a', runId, completion.eventId)).toBeNull();
        expect((await complete.read('a', runId)).revision).toBe(receipt.revision);
        f.sql.run('UPDATE test_objective_evidence SET original_json=?', [evidenceText]);
        const healthRevision = (await new CombatHealthStore(f.env.DB).read('a', runId)).revision;
        f.env.DB.batch = async statements => {
          f.sql.run('UPDATE combat_damage_states SET revision=revision+1 WHERE run_id=?', [runId]);
          return batch(statements);
        };
        await expect(complete.commit('a', runId, completion)).rejects.toThrow('operation_map_state_conflict');
        f.env.DB.batch = batch;
        expect(await complete.lookup('a', runId, completion.eventId)).toBeNull();
        expect((await complete.read('a', runId)).revision).toBe(receipt.revision);
        f.sql.run('UPDATE combat_damage_states SET revision=? WHERE run_id=?', [healthRevision, runId]);
        const { OrdinaryObjectiveResultStore } = await import('../../apps/game-api/src/ordinary-objective-result-store');
        const acceptedResults = new OrdinaryObjectiveResultStore(f.env.DB, evidenceOwner);
        await expect(new OrdinaryObjectiveResultStore(f.env.DB).accept(ctx)).rejects.toThrow('evidence_unavailable');
        candidate = { ...evidence, targets: evidence.targets.map((t: { hp: number }, i: number) => i === 0 ? { ...t, hp: t.hp - 1 } : t) };
        await expect(acceptedResults.accept(ctx)).rejects.toThrow('health_source_conflict');
        expect(f.scalar('SELECT COUNT(*) FROM ordinary_objective_results')).toBe(0);
        candidate = evidence;
        candidate = { ...evidence, attemptGeneration: 1 };
        await expect(acceptedResults.accept({ ...ctx, command: { ...ctx.command, attemptGeneration: 1 } })).rejects.toThrow('attempt_conflict');
        candidate = evidence;
        f.env.DB.batch = async statements => {
          f.sql.run("UPDATE test_objective_evidence SET original_json='changed-before-accept'");
          return batch(statements);
        };
        await expect(acceptedResults.accept(ctx)).rejects.toThrow();
        f.env.DB.batch = batch;
        expect(f.scalar('SELECT COUNT(*) FROM ordinary_objective_results')).toBe(0);
        f.sql.run('UPDATE test_objective_evidence SET original_json=?', [evidenceText]);
        f.env.DB.batch = async statements => {
          f.sql.run('UPDATE test_objective_holds SET pending=0');
          return batch(statements);
        };
        await expect(acceptedResults.accept(ctx)).rejects.toThrow('operation_map_state_conflict');
        f.env.DB.batch = batch;
        expect(f.scalar('SELECT COUNT(*) FROM ordinary_objective_results')).toBe(0);
        expect(await complete.lookup('a', runId, completion.eventId)).toBeNull();
        expect((await complete.read('a', runId)).revision).toBe(receipt.revision);
        f.sql.run('UPDATE test_objective_holds SET pending=1');
        const beforeUncertain = f.sql.export();
        f.env.DB.batch = async () => { throw new Error('completion_transport_unavailable'); };
        try { await expect(complete.commit('a', runId, completion)).rejects.toThrow('completion_transport_unavailable'); }
        finally { f.env.DB.batch = batch; }
        expect(await complete.lookup('a', runId, completion.eventId)).toBeNull();
        expect(Buffer.from(f.sql.export()).equals(Buffer.from(beforeUncertain))).toBe(true);
        const testApp = new Hono<{ Bindings: Env }>();
        let executionDisposed = false;
        registerOperationMapCommandRoutes(testApp, async () => ({ accountId: 'a' }), () => {
          if (executionDisposed) throw new Error('execution_disposed');
          return evidenceOwner;
        });
        f.env.DB.batch = async () => { throw new Error('completion_transport_unavailable'); };
        const uncertainResponse = await completionRequest(testApp).finally(() => { f.env.DB.batch = batch; });
        expect(uncertainResponse.status).toBe(503);
        expect(await uncertainResponse.json()).toEqual({ accepted: false, errorCode: 'ordinary_completion_outcome_unknown' });
        expect(Buffer.from(f.sql.export()).equals(Buffer.from(beforeUncertain))).toBe(true);
        f.env.DB.batch = async statements => { await batch(statements); throw new Error('completion_ack_lost'); };
        const acceptedResponse = await completionRequest(testApp).finally(() => { f.env.DB.batch = batch; });
        expect(acceptedResponse.status).toBe(200);
        expect(f.scalar('SELECT COUNT(*) FROM ordinary_objective_results')).toBe(1);
        const accepted = await new OrdinaryObjectiveResultStore(f.env.DB).accept(ctx), acceptedCalls = calls;
        expect(await new OrdinaryObjectiveResultStore(f.env.DB).accept(ctx)).toEqual(accepted);
        expect(calls).toBe(acceptedCalls);
        await expect(acceptedResults.accept({ ...ctx, command: { ...ctx.command, expectedRevision: ctx.command.expectedRevision + 1 } }))
          .rejects.toThrow('result_conflict');
        expect(() => f.sql.run('DELETE FROM ordinary_objective_results')).toThrow();
        const completedResponse = await completionRequest();
        expect(completedResponse.status).toBe(200);
        const completed = (await completedResponse.json() as { receipt: Awaited<ReturnType<OperationMapStore['commit']>> }).receipt;
        expect(completed).toEqual(accepted.receipt);
        expect(completed.state.phase).toBe('READY');
        expect(completed.ordinaryAutomation).toMatchObject({ revision: 2, ultimateAuto: true });
        expect(completed.ordinaryAutomation!.actors).toEqual(evidence.actions);
        expect((await readOrdinaryRunAutomation(f.env.DB, 'a', runId))?.state).toEqual(completed.ordinaryAutomation);
        expect(completed.state.holdIds).toEqual(['pending-reward']);
        const callCount = calls, completedBytes = f.sql.export();
        executionDisposed = true;
        const replayResponse = await completionRequest(testApp);
        expect(replayResponse.status).toBe(200);
        expect(await replayResponse.json()).toMatchObject({ accepted: true, receipt: completed });
        expect((await completionRequest(app, { ...completionBody, evidenceRef: 'different' })).status).toBe(409);
        expect(await complete.commit('a', runId, completion)).toEqual(completed);
        expect(calls).toBe(callCount);
        expect(Buffer.from(f.sql.export()).equals(Buffer.from(completedBytes))).toBe(true);
        // Disk round-trip with a new store; not a new OS process or a production evidence owner.
        const recoveryDirectory = mkdtempSync('D:/Project/LASTRAIL/LocalTemp/ordinary-completion-recovery-');
        const recoveryPath = join(recoveryDirectory, 'receipt.sqlite');
        writeFileSync(recoveryPath, completedBytes);
        const SQL = await initSqlJs();
        const recoveredSql = new SQL.Database(readFileSync(recoveryPath));
        try {
          const recovered = new OperationMapStore(new SqlJsD1Database(recoveredSql) as unknown as D1Database);
          expect(await recovered.commit('a', runId, completion)).toEqual(completed);
          await expect(recovered.commit('other-account', runId, completion)).rejects.toThrow('operation_map_run_not_found');
          await expect(recovered.commit('a', runId, { ...completion, evidenceRef: 'changed-proof' }))
            .rejects.toThrow('operation_map_event_conflict');
          expect(Buffer.from(recoveredSql.export()).equals(Buffer.from(completedBytes))).toBe(true);
          expect(calls).toBe(callCount);
        } finally { recoveredSql.close(); }
        expect((await f.env.DB.prepare('SELECT state_json FROM combat_damage_states WHERE run_id=?').bind(runId).first<{ state_json: string }>())!.state_json).toBe(healthBefore);
        expect(f.scalar('SELECT COUNT(*) FROM run_final_results')).toBe(0);
      }
      expect(receipt.firstPlayableScaling).toMatchObject({ nodeId: `n${depth}-0`, visitDepth: depth,
        progressRevision: depth * 2 + 1, sourceHash: source.sourceHash, profileHash: profile.definitionHash,
        applied: { risk: 'II', progress: depth < 5 ? 'EARLY' : depth < 9 ? 'MIDDLE' : 'LATE',
          gunnerHp: { numerator: '300', denominator: '1' },
          gunnerRawDamage: depth < 5 ? { numerator: '23', denominator: '1' } : depth < 9
            ? { numerator: '253', denominator: '10' } : { numerator: '138', denominator: '5' } } });
      const { bindingHash, ...binding } = receipt.firstPlayableScaling!;
      expect(bindingHash).toBe(await sha256Hex(canonicalJson(binding)));
      const enemyContext = { accountId: 'a', runId, nodeId: binding.nodeId, mapRevision: receipt.revision,
        arrivalEventId: receipt.eventId, startSnapshot: text, firstPlayableScaling: receipt.firstPlayableScaling };
      const enemyOriginal = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
      const enemies = await prepareOrdinaryNodeEnemies(enemyOriginal, enemyContext);
      const identities = await firstObjectiveEncounterIdentities({ accountId: 'a', runId,
        encounterId: receipt.eventId, sourceHash: source.sourceHash });
      expect(enemies.gunners.map(g => g.instanceId)).toEqual(identities.issuedGunnerIds);
      expect(enemies.gunners.map(g => g.hp)).toEqual([binding.applied.gunnerHp, binding.applied.gunnerHp]);
      expect(enemies.gunners[0].rawDamage).toEqual(binding.applied.gunnerRawDamage);
      expect(enemies.actionDefinition.spatial.projectile?.firstLaunch).toBe('TARGETABLE_PLUS_FULL_PERIOD');
      expect(enemies.actionDefinition.basicPeriodMs).toBe(2000);
      expect(enemies.artillery.hazardId).toBe(identities.issuedHazardId);
      expect(enemies.artillery.rawDamage).toEqual(binding.applied.artilleryRawDamage);
      if (version >= 7) {
        expect(enemies.version).toBe('ordinary.node-enemies.v2.20261031');
        expect(enemies.spatial?.physicalInstanceId).toBe(binding.enemySpatial!.physicalInstanceId);
        expect(enemies.spatial?.definition.geometry.definitionHash).toBe(binding.enemySpatial!.geometryDefinitionHash);
        expect(enemies.spatial?.definition.placement.gunnerRearOffsetsMm).toEqual([2000, 6000]);
        expect(enemies.spatial?.definition.muzzle).toBeNull();
        const wrongGeometry = structuredClone(receipt.firstPlayableScaling!);
        wrongGeometry.enemySpatial!.geometryDefinitionHash = '0'.repeat(64);
        const { bindingHash: previousHash, ...body } = wrongGeometry; void previousHash;
        wrongGeometry.bindingHash = await sha256Hex(canonicalJson(body));
        await expect(prepareOrdinaryNodeEnemies(enemyOriginal, { ...enemyContext, firstPlayableScaling: wrongGeometry }))
          .rejects.toThrow('enemy_source_conflict');
      } else { expect(enemies.version).toBe('ordinary.node-enemies.v1.20261031'); expect(enemies).not.toHaveProperty('spatial'); }
      expect(await prepareOrdinaryNodeEnemies(enemyOriginal, enemyContext)).toEqual(enemies);
      await expect(prepareOrdinaryNodeEnemies(enemyOriginal, { ...enemyContext, nodeId: 'foreign-node' }))
        .rejects.toThrow('enemy_source_conflict');
      const altered = structuredClone(receipt.firstPlayableScaling!);
      altered.applied.gunnerHp = { numerator: '99999', denominator: '1' };
      const { bindingHash: oldHash, ...alteredBody } = altered; void oldHash;
      altered.bindingHash = await sha256Hex(canonicalJson(alteredBody));
      await expect(prepareOrdinaryNodeEnemies(enemyOriginal, { ...enemyContext, firstPlayableScaling: altered }))
        .rejects.toThrow('enemy_source_conflict');
      if (depth === 1) {
        let corruptEnemies = false, wrongCooldownUnits = false;
        const nodeSource = new OrdinaryNodeEntrySource(f.env.DB, { prepare: async input => {
          expect(input.enemies).toEqual(input.firstPlayableScaling ? enemies : undefined);
          if (corruptEnemies) input.enemies!.gunners[0].hp.numerator = '1';
          return { state: { ...input.executionOrigin!, enemies: input.enemies ?? { syntheticOtherEncounter: true },
            effects: { synthetic: true }, spatial: { synthetic: true, ...(input.geometry ? { definitionBinding: input.geometry.binding } : {}) }, actions: {
              version: 'ordinary.node-actions.v1.20261030', stepTimeSpanTicks: 500000, ultimateAuto: input.entry.automation!.ultimateAuto,
              actors: input.entry.actors.map(a => ({ operatorId: a.operatorId, basicNextOpportunityInTicks: 7,
                activeCooldownRemainingTicks: Math.ceil(input.entry.automation!.actors.find(x => x.operatorId === a.operatorId)!.activeCooldownRemainingTicks / (wrongCooldownUnits ? 1 : 500000)),
                ultimateCharge: input.entry.automation!.actors.find(x => x.operatorId === a.operatorId)!.ultimateCharge })) } },
          assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')] };
        } });
        const nodeContext = { ...enemyContext, executionOrigin: issueOrdinaryNodeExecutionOrigin() };
        if (bodyRegistry) {
          // A run-wide catalog does not force first-battle actors into another encounter.
          expect((await nodeSource.prepare({ ...nodeContext, nodeId: 'other-encounter', firstPlayableScaling: undefined })).snapshot.runtime.enemies)
            .toEqual({ syntheticOtherEncounter: true });
          expect((await nodeSource.prepare(nodeContext)).snapshot.runtime.spatial.definitionBinding).toBeDefined();
        }
        expect((await nodeSource.prepare(nodeContext)).snapshot.runtime.enemies).toEqual(enemies);
        expect((await nodeSource.prepare(nodeContext)).snapshot.runtime.actions.actors[0].activeCooldownRemainingTicks).toBe(80);
        wrongCooldownUnits = true;
        await expect(nodeSource.prepare(nodeContext)).rejects.toThrow('actions_source_conflict');
        wrongCooldownUnits = false;
        corruptEnemies = true;
        await expect(nodeSource.prepare(nodeContext)).rejects.toThrow('enemy_source_conflict');
        expect(enemies.gunners[0].hp).toEqual(binding.applied.gunnerHp);
      }
      expect(receipt.firstPlayableScaling!.progressStateHash).toBe(await sha256Hex(canonicalJson(receipt.state)));
      const bytes = f.sql.export();
      expect(await store.commit('a', runId, command)).toEqual(receipt);
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(bytes))).toBe(true);
      traversed.push(edge.edgeId); current = edge.toNodeId;
    }
    const originalBand = await store.lookup('a', runId, 'arrive-1');
    expect(originalBand!.firstPlayableScaling!.applied.progress).toBe('EARLY');
    const bossEdge = plan.edges.find(e => e.fromNodeId === current && e.toNodeId === 'boss')!;
    f.sql.run('UPDATE operation_map_states SET revision=24,state_json=? WHERE run_id=?', [canonicalJson({
      version: 'operation.map-progress.v1', traversedEdgeIds: traversed, pendingEdgeId: bossEdge.edgeId, phase: 'TRAVELLING', holdIds: [] }), runId]);
    expect(await store.commit('a', runId, { kind: 'ARRIVE', edgeId: bossEdge.edgeId,
      evidenceRef: 'fixture', eventId: 'boss-arrival', expectedRevision: 24 })).not.toHaveProperty('firstPlayableScaling');
    // Historical v3 runs retain their null binding even on a matching profile node.
    snapshot.firstBattleEncounterSource = await captureFirstBattleEncounterSourceV3(original.origin, original.originHash);
    const oldSnapshot = JSON.stringify(snapshot), firstEdge = plan.edges.find(e => e.fromNodeId === plan.entryNodeId)!;
    f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [oldSnapshot, runId]);
    // Re-seed isolated historical fixture state; production frozen START updates must remain forbidden.
    f.sql.run('DELETE FROM operation_map_states WHERE run_id=?', [runId]);
    f.sql.run('INSERT INTO operation_map_states VALUES(?,30,?,?,?)', [runId, canonicalJson(plan), oldSnapshot,
      canonicalJson({ version: 'operation.map-progress.v1', traversedEdgeIds: [], pendingEdgeId: firstEdge.edgeId, phase: 'TRAVELLING', holdIds: [] })]);
    expect(await store.commit('a', runId, { kind: 'ARRIVE', edgeId: firstEdge.edgeId,
      evidenceRef: 'fixture', eventId: 'historical-arrival', expectedRevision: 30 })).not.toHaveProperty('firstPlayableScaling');
  } finally { f.sql.close(); }
}, 15000); // Full map traversal plus completion/recovery now exceeds the default five seconds.

it('freezes the actual owned train baseline in START without inventing current health or shields', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const source = (await readOrdinaryTrainFromOriginal(original))!;
    const owned = await f.env.DB.prepare('SELECT train_id,level,revision FROM account_train WHERE account_id=?').bind('a')
      .first<{ train_id: string; level: number; revision: number }>();
    expect(source).toEqual(started.ordinaryTrainSource);
    expect(source.packet.train).toEqual({ trainId: owned!.train_id, level: owned!.level, revision: owned!.revision });
    expect(source.packet.permanentStats).toEqual({ maxHp: 5000 * (10000 + 800 * (owned!.level - 1)) / 10000,
      attack: Math.floor(60 * (10000 + 600 * (owned!.level - 1)) / 10000), defense: 111, basicIntervalMs: 1000 });
    expect(source.packet).not.toHaveProperty('currentHp');
    expect(source.packet).not.toHaveProperty('shields');
    f.sql.run("UPDATE account_train SET level=9,revision=revision+1 WHERE account_id='a'");
    expect(await readOrdinaryTrainFromOriginal(original)).toEqual(source);
    const snapshot = JSON.parse(original.startSnapshotJson);
    snapshot.ordinaryTrainSource.packet.permanentStats.maxHp++;
    snapshot.ordinaryTrainSource.sourceHash = await sha256Hex(canonicalJson(snapshot.ordinaryTrainSource.packet));
    await expect(readOrdinaryTrainFromOriginal({ ...original, startSnapshotJson: JSON.stringify(snapshot) })).rejects.toThrow('ordinary_train_source_invalid');
    delete snapshot.ordinaryTrainSource;
    expect(await readOrdinaryTrainFromOriginal({ ...original, startSnapshotJson: JSON.stringify(snapshot) })).toBeNull();
  } finally { f.sql.close(); }
});

it.each(['absent', 'v1', 'unsupported-effect'] as const)('preserves train initialization boundary for %s START', async variant => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const snapshot = JSON.parse(original.startSnapshotJson), packet = snapshot.ordinaryTrainSource.packet;
    if (variant === 'absent') { delete snapshot.ordinaryTrainSource; delete snapshot.ordinaryRailSource; delete snapshot.ordinaryRoofSource; }
    else if (variant === 'v1') {
      snapshot.ordinaryTrainSource = await captureOrdinaryTrainSource({
      accountId: packet.accountId, runId, ordinaryOriginHash: packet.ordinaryOriginHash,
      ownedSourceHash: packet.ownedSourceHash, loadoutRevision: packet.loadoutRevision, train: packet.train });
      const p = snapshot.ordinaryRailSource.packet;
      snapshot.ordinaryRailSource = await captureOrdinaryRailSourceV3({ accountId: p.accountId, runId,
        trainId: p.trainId, ordinaryOriginHash: p.ordinaryOriginHash, ownedSourceHash: packet.ownedSourceHash,
        loadoutRevision: p.loadoutRevision, selectedCarriages: p.selectedCarriages });
      snapshot.ordinaryRoofSource = await captureOrdinaryRoofSource(original.origin, original.originHash, snapshot.ordinaryRailSource);
    }
    else {
      packet.initialization.departureEffects = [{ amount: 400, expiresAtMs: 6000 }];
      snapshot.ordinaryTrainSource.sourceHash = await sha256Hex(canonicalJson(packet));
    }
    // Disposable historical/corrupt original fixture, never a production START migration.
    const bytes = JSON.stringify(snapshot);
    f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [bytes, runId]);
    f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
    f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, bytes]);
    const before = f.sql.export();
    const activation = new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'init-train-history', source: 'ORIGINAL_START' });
    if (variant === 'unsupported-effect') {
      await expect(activation).rejects.toThrow('ordinary_train_source_invalid');
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    } else {
      const receipt = await activation;
      expect(receipt).not.toHaveProperty('trainInitialState');
      expect(receipt.health.state.targets).toHaveLength(5);
    }
  } finally { f.sql.close(); }
});

it('commits train health atomically and never restores initial HP over progressed health on replay', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId), trainId = started.ordinaryTrainSource.packet.train.trainId;
    const departureLedger = JSON.stringify(f.sql.exec('SELECT * FROM fuel_ledger ORDER BY id'));
    const originalStart = (await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId)).startSnapshotJson;
    const owner = new OrdinaryCombatLifecycle(f.env.DB), command = { requestId: 'init-train-progress', source: 'ORIGINAL_START' };
    f.sql.run("UPDATE account_train SET level=9,revision=revision+1 WHERE account_id='a'");
    f.sql.run("CREATE TRIGGER fail_train_receipt BEFORE INSERT ON ordinary_combat_activation_receipts BEGIN SELECT RAISE(ABORT,'test train receipt failure'); END");
    const failed = await app.request(`http://localhost/v1/runs/${runId}/ordinary-initialization`, {
      method: 'POST', headers: { ...f.headers, 'Idempotency-Key': command.requestId }, body: '{}',
    }, f.env);
    expect(failed.status).toBe(503);
    expect(await failed.json()).toMatchObject({ accepted: false, errorCode: 'ordinary_combat_initialization_outcome_unknown' });
    expect(f.scalar('SELECT COUNT(*) FROM combat_damage_states')).toBe(0);
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_run_stat_states')).toBe(0);
    expect(f.scalar('SELECT phase FROM ordinary_combat_phases')).toBe('PREPARED');
    expect(await f.startCore()).toEqual(started);
    expect((await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId)).startSnapshotJson).toBe(originalStart);
    expect(JSON.stringify(f.sql.exec('SELECT * FROM fuel_ledger ORDER BY id'))).toBe(departureLedger);
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(1);
    f.sql.run('DROP TRIGGER fail_train_receipt');
    const receipt = await owner.activate('a', runId, command);
    expect(receipt.health.state.targets.find(t => t.id === trainId)!.health).toMatchObject({ hp: 5000, maxHp: 5000 });
    // Synthetic damage proof tests store/replay isolation, not product hit authority.
    const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({
      assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
    await health.commit('a', runId, { eventId: 'test-train-damage', expectedRevision: 1, targetId: trainId,
      change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 4900 } });
    const progressed = await health.read('a', runId), bytes = f.sql.export();
    expect(await owner.activate('a', runId, command)).toEqual(receipt);
    expect(await health.read('a', runId)).toEqual(progressed);
    expect(progressed.state.targets.find(t => t.id === trainId)!.health.hp).toBe(4900);
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(bytes))).toBe(true);
    expect(await f.startCore()).toEqual(started);
    expect(JSON.stringify(f.sql.exec('SELECT * FROM fuel_ledger ORDER BY id'))).toBe(departureLedger);
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_combat_activation_receipts')).toBe(1);
    await expect(owner.activate('a', runId, { ...command, requestId: 'new-key' })).rejects.toThrow('phase_conflict');
  } finally { f.sql.close(); }
});

it('rejects five-only manual activation of a train-v2 START even with an internal verifier', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const verifier = { prepare: async () => ({ assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) };
    const before = f.sql.export();
    await expect(new OrdinaryCombatLifecycle(f.env.DB, verifier).activate('a', runId, { requestId: 'manual-train-v2',
      initialTargets: original.origin.actors.map(a => ({ id: a.operatorId, hp: a.permanentStats.maxHp, maxHp: a.permanentStats.maxHp }))
    })).rejects.toThrow('ordinary_combat_original_initialization_required');
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
  } finally { f.sql.close(); }
});

it.each(['checksum', 'invalid-json'] as const)('does not replace a corrupt original initialization receipt (%s)', async corruption => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const request = () => app.request(`http://localhost/v1/runs/${runId}/ordinary-initialization`, { method: 'POST',
      headers: { ...f.headers, 'Idempotency-Key': 'corrupt-original-receipt' }, body: '{}' }, f.env);
    expect((await request()).status).toBe(200);
    f.sql.run('DROP TRIGGER ordinary_activation_no_update');
    const text = '{';
    f.sql.run('UPDATE ordinary_combat_activation_receipts SET response_json=?,response_hash=? WHERE run_id=?',
      [text, corruption === 'checksum' ? '0'.repeat(64) : await sha256Hex(text), runId]);
    f.sql.run("CREATE TRIGGER ordinary_activation_no_update BEFORE UPDATE ON ordinary_combat_activation_receipts BEGIN SELECT RAISE(ABORT,'ordinary_activation_immutable'); END");
    const before = f.sql.export(), response = await request();
    expect(response.status).toBe(corruption === 'checksum' ? 409 : 503);
    expect(await response.json()).toEqual({ accepted: false,
      errorCode: corruption === 'checksum' ? 'ordinary_combat_receipt_invalid' : 'ordinary_combat_initialization_outcome_unknown' });
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_combat_activation_receipts')).toBe(1);
  } finally { f.sql.close(); }
});

it.each([true, false])('isolates train/operator ordered HP commits with original receipts (train first=%s)', async trainFirst => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const initial = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'mixed-init', source: 'ORIGINAL_START' });
    const trainId = initial.trainInitialState!.healthTargetId;
    const ids = trainFirst ? [trainId, 'op_kyle'] : ['op_kyle', trainId];
    const commands = ids.map((targetId, i) => ({ eventId: `test-mixed-${i}`, expectedRevision: i + 1, targetId,
      change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 0 } }));
    // Tests the real storage boundary; hit eligibility, shields and tick order remain producer-owned.
    const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({
      assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
    await expect(new CombatHealthStore(f.env.DB).prepareChanges('a', runId, commands)).rejects.toThrow('combat_health_verification_required');
    await expect(health.prepareChanges('b', runId, commands)).rejects.toThrow('combat_run_not_found');
    const prepared = await health.prepareChanges('a', runId, commands);
    expect(await health.read('a', runId)).toEqual(initial.health);
    f.sql.run("CREATE TRIGGER fail_second_mixed_receipt BEFORE INSERT ON combat_damage_receipts WHEN NEW.event_id='test-mixed-1' BEGIN SELECT RAISE(ABORT,'test mixed receipt failure'); END");
    await expect(f.env.DB.batch(prepared.statements)).rejects.toThrow('test mixed receipt failure');
    expect(await health.read('a', runId)).toEqual(initial.health);
    expect(f.scalar('SELECT COUNT(*) FROM combat_damage_receipts')).toBe(0);
    f.sql.run('DROP TRIGGER fail_second_mixed_receipt');
    await f.env.DB.batch(prepared.statements);
    const after = await health.read('a', runId);
    expect(after.revision).toBe(3);
    expect(after.state.ledger.map(e => e.targetId)).toEqual(ids);
    expect(after.state.targets.filter(t => ids.includes(t.id)).map(t => t.health.hp)).toEqual([0, 0]);
    expect(after.state.targets.filter(t => !ids.includes(t.id))).toEqual(initial.health.state.targets.filter(t => !ids.includes(t.id)));
    expect(prepared.receipts.map(r => [r.targetId, r.after.hp, r.revision])).toEqual(ids.map((id, i) => [id, 0, i + 2]));
    const replay = await health.prepareChanges('a', runId, commands);
    expect(replay.statements).toEqual([]); expect(replay.receipts).toEqual(prepared.receipts);
    await expect(health.prepareChanges('a', runId, [{ ...commands[0], targetId: ids[1] }, commands[1]])).rejects.toThrow('combat_health_event_conflict');
    await expect(health.prepareChanges('a', runId, [commands[0], { ...commands[1], eventId: 'unseen-second' }])).rejects.toThrow('combat_health_partial_replay');
    await expect(health.prepareChanges('a', runId, [{ ...commands[0], eventId: 'unknown-target', expectedRevision: 3, targetId: 'enemy_template' }])).rejects.toThrow('combat_health_target_unknown');
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    expect(() => assertClientFinishAuthority(original.startSnapshotJson)).toThrow('combat_finish_verification_required');
    expect(await f.env.DB.prepare('SELECT status FROM runs WHERE id=?').bind(runId).first()).toEqual({ status: 'ACTIVE' });
    expect(f.scalar('SELECT COUNT(*) FROM run_final_results')).toBe(0);
  } finally { f.sql.close(); }
});

it('rejects a prepared mixed-target commit after terminal status changes without reversing it', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const initial = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'terminal-mixed-init', source: 'ORIGINAL_START' });
    const health = new CombatHealthStore(f.env.DB, { prepare: async () => ({
      assertions: [f.env.DB.prepare('INSERT INTO combat_damage_write_guard(valid) VALUES(1)')] }) });
    const commands = [initial.trainInitialState!.healthTargetId, 'op_kyle'].map((targetId, i) => ({
      eventId: `test-late-${i}`, expectedRevision: i + 1, targetId, change: { kind: 'ACTUAL_HP', reason: 'DAMAGE', hp: 0 } }));
    const prepared = await health.prepareChanges('a', runId, commands);
    // Simulated existing terminal-owner write, not a new ordinary outcome adjudicator.
    f.sql.run("UPDATE runs SET status='FAILED' WHERE id=?", [runId]);
    const before = f.sql.export();
    await expect(f.env.DB.batch(prepared.statements)).rejects.toThrow();
    expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    expect(await health.read('a', runId)).toEqual(initial.health);
    await expect(health.prepareChanges('a', runId, commands)).rejects.toThrow('combat_run_not_active');
  } finally { f.sql.close(); }
});

it('pins the actual selected carriage source and approved trial independently of later loadout growth', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId), train = await readOrdinaryTrainFromOriginal(original);
    const rail = (await readOrdinaryRailFromOriginal(original, train))!;
    const owned = await f.env.DB.prepare(`SELECT c.carriage_id AS carriageId,json_extract(j.value,'$.slot') AS slot,c.level,c.revision
      FROM account_train_loadout l JOIN json_each(l.loadout_json,'$.carriages') j
      JOIN account_carriages c ON c.account_id=l.account_id AND c.carriage_id=json_extract(j.value,'$.carriageId')
      WHERE l.account_id=? ORDER BY slot`).bind('a').all();
    expect(rail.packet.selectedCarriages).toEqual(owned.results);
    expect(rail.packet.trainId).toBe(train!.packet.train.trainId);
    expect(rail).toEqual(started.ordinaryRailSource);
    // Size projection only: the production account_ UUID format must fit the existing client input limit.
    const uuidSized = JSON.stringify(started, (key, value) => key === 'accountId' ? 'account_00000000-0000-4000-8000-000000000001' : value);
    expect(Buffer.byteLength(uuidSized)).toBeLessThanOrEqual(65536);
    expect(rail.packet.version).toBe('combat.ordinary-rail-source.v4.20261028');
    expect(rail.packet).toMatchObject({ definitionVersion: 'combat.ordinary-rail-trial.v3.20261028',
      status: 'MOTION_BOUND_CAPACITY_UNVERIFIED' });
    expect(rail.packet).not.toHaveProperty('definition');
    f.sql.run("UPDATE account_carriages SET level=level+1,revision=revision+1 WHERE account_id='a'");
    f.sql.run("UPDATE account_train_loadout SET revision=revision+1 WHERE account_id='a'");
    expect(await f.startCore()).toEqual(started);
    expect(await readOrdinaryRailFromOriginal(original, train)).toEqual(rail);
    const initialized = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'rail-original-init', source: 'ORIGINAL_START' });
    expect(initialized.ordinaryRailBinding!.sourceHash).toBe(rail.sourceHash);
  } finally { f.sql.close(); }
});

it('rejects rehashed unissued rail definitions and geometry before initialization, retaining source-absent history', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId), pristine = JSON.parse(original.startSnapshotJson);
    const replace = (snapshot: unknown) => {
      const bytes = JSON.stringify(snapshot);
      f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [bytes, runId]);
      f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
      f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, bytes]);
    };
    for (const variant of ['policy-hash', 'train', 'geometry', 'version', 'null', 'carriage-id', 'carriage-slot',
      'carriage-level', 'carriage-revision', 'admission-train', 'admission-revision', 'owned-missing', 'owned-duplicate', 'policy-version', 'physical-id', 'physical-position', 'speed', 'acceleration', 'ready']) {
      const snapshot = structuredClone(pristine), source = snapshot.ordinaryRailSource;
      if (variant === 'policy-hash') source.packet.definitionHash = '0'.repeat(64);
      if (variant === 'train') source.packet.trainId = 'foreign-train';
      if (variant === 'geometry') source.packet.geometry = { railSegmentId: 'invented', lengthMm: 999999 };
      if (variant === 'version') source.packet.version = 'unissued';
      if (variant === 'carriage-id') source.packet.selectedCarriages[0].carriageId = 'foreign-carriage';
      if (variant === 'carriage-slot') source.packet.selectedCarriages.reverse().forEach((c: { slot: number }, i: number) => { c.slot = i; });
      if (variant === 'carriage-level') source.packet.selectedCarriages[0].level++;
      if (variant === 'carriage-revision') source.packet.selectedCarriages[0].revision++;
      if (variant === 'admission-train') snapshot.admission.loadout.trainId = 'foreign-train';
      if (variant === 'admission-revision') snapshot.admission.loadoutRevision++;
      if (variant === 'owned-missing') snapshot.admission.ownedState.carriages.shift();
      if (variant === 'owned-duplicate') snapshot.admission.ownedState.carriages.push(snapshot.admission.ownedState.carriages[0]);
      if (variant === 'policy-version') source.packet.definitionVersion = 'unissued-policy';
      if (variant === 'physical-id') source.packet.physicalConsist.instanceIds[0] = 'foreign-instance';
      if (variant === 'physical-position') source.packet.physicalConsist.initialFrontArcMm++;
      if (variant === 'speed') source.packet.initialMotion.speedMmPerSecond++;
      if (variant === 'acceleration') source.packet.initialMotion.accelerationMmPerSecondSquared++;
      if (variant === 'ready') source.packet.status = 'READY';
      source.sourceHash = await sha256Hex(canonicalJson(source.packet));
      if (variant === 'null') snapshot.ordinaryRailSource = null;
      replace(snapshot);
      const before = f.sql.export();
      await expect(new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId,
        { requestId: 'invalid-rail-' + variant, source: 'ORIGINAL_START' })).rejects.toThrow('ordinary_rail_source_invalid');
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    }
    delete pristine.ordinaryRailSource; delete pristine.ordinaryRoofSource; replace(pristine);
    const historical = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId,
      { requestId: 'historical-rail-absent', source: 'ORIGINAL_START' });
    expect(historical).not.toHaveProperty('ordinaryRailBinding');
  } finally { f.sql.close(); }
});

it.each([1, 2, 3])('preserves historical rail v%i without retroactive new geometry or motion', async version => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId), train = await readOrdinaryTrainFromOriginal(original);
    const snapshot = JSON.parse(original.startSnapshotJson), p = snapshot.ordinaryRailSource.packet;
    const capture = version === 3 ? captureOrdinaryRailSourceV3 : version === 1 ? captureOrdinaryRailSource : captureOrdinaryRailSourceV2;
    const historical = await capture({ accountId: p.accountId, runId: p.runId, trainId: p.trainId,
      ordinaryOriginHash: p.ordinaryOriginHash, ownedSourceHash: original.origin.ownedSourceHash,
      loadoutRevision: p.loadoutRevision, selectedCarriages: p.selectedCarriages });
    snapshot.ordinaryRailSource = historical;
    const read = await readOrdinaryRailFromOriginal({ ...original, startSnapshotJson: JSON.stringify(snapshot) }, train);
    expect(read).toEqual(historical);
    expect(read!.packet.version).toBe(`combat.ordinary-rail-source.v${version}.20261027`);
    expect(read!.packet.initialMotion).toBeNull();
    if (version < 3) {
      expect('definition' in read!.packet && Object.hasOwn(read!.packet.definition, 'finiteRail')).toBe(version === 2);
      expect(read!.packet.physicalConsist).toBeNull(); expect(read!.packet.geometry).toBeNull();
    } else expect(read!.packet.geometry!.version).toBe('ordinary.finite-rail-authoring.v1.20261027');
  } finally { f.sql.close(); }
});

it('binds only the measured selection and issues stable account/run-scoped physical instances', async () => {
  const input = { accountId: 'a', runId: 'run-one', trainId: 'train_a', ordinaryOriginHash: 'a'.repeat(64),
    ownedSourceHash: 'sha256:' + 'b'.repeat(64), loadoutRevision: 1,
    selectedCarriages: [{ carriageId: 'car_combat', slot: 0, level: 1, revision: 1 },
      { carriageId: 'car_gun', slot: 1, level: 1, revision: 1 }] };
  const bound = await captureOrdinaryRailSourceV3(input);
  expect(await captureOrdinaryRailSourceV3(input)).toEqual(bound);
  const ids = bound.packet.physicalConsist!.instanceIds;
  for (const changed of [{ ...input, runId: 'run-two' }, { ...input, accountId: 'another' }]) {
    const other = await captureOrdinaryRailSourceV3(changed);
    expect(other.packet.physicalConsist!.instanceIds.some(id => ids.includes(id))).toBe(false);
  }
  const reversed = input.selectedCarriages.map(c => ({ ...c, slot: 1 - c.slot }));
  for (const changed of [{ ...input, trainId: 'another' }, { ...input, selectedCarriages: reversed },
    { ...input, selectedCarriages: [...input.selectedCarriages, { carriageId: 'car_cargo', slot: 2, level: 1, revision: 1 }] }]) {
    const unsupported = await captureOrdinaryRailSourceV3(changed);
    expect(unsupported.packet.version).toBe('combat.ordinary-rail-source.v2.20261027');
    expect(unsupported.packet.physicalConsist).toBeNull(); expect(unsupported.packet.geometry).toBeNull();
  }
  expect(bound.packet.status).toBe('GEOMETRY_BOUND_EXECUTION_UNREADY');
  expect(bound.packet.initialMotion).toBeNull();
});

it('pins the approved three-two initial layout to original actor IDs without guessing foot height or rewriting prior sources', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const rail = (await readOrdinaryRailFromOriginal(original, await readOrdinaryTrainFromOriginal(original)))!;
    const roof = (await readOrdinaryRoofFromOriginal(original, rail))!;
    expect(roof).toEqual(started.ordinaryRoofSource);
    expect(roof.packet.operatorIds).toEqual([...original.origin.actors].sort((a, b) => a.slot - b.slot).map(a => a.operatorId));
    const definition = captureOrdinaryRoofDefinition();
    expect(definition.slots.map(s => [s.physicalOrder, s.localXmm, s.localZmm])).toEqual([
      [1, 3000, 0], [1, 0, 0], [1, -3000, 0], [2, 3000, 0], [2, -3000, 0] ]);
    expect(definition.slots.every(s => !Object.hasOwn(s, 'localYmm'))).toBe(true);
    expect(definition.footHeightSource).toBe('MEASURED_ROOF_SURFACE_PLUS_ACTOR_FOOT_OFFSET');
    expect(definition.existingPlacementAndCheckpointTakePrecedence).toBe(true);
    expect(new Set(definition.slots.map(s => rail.packet.physicalConsist!.instanceIds[s.physicalOrder])).size).toBe(2);
    expect(definition.slots.every(s => s.physicalOrder !== 0)).toBe(true);
    expect(await captureOrdinaryRoofSource({ ...original.origin, actors: [...original.origin.actors].reverse() }, original.originHash, rail)).toEqual(roof);
    f.sql.run("UPDATE account_formation SET revision=revision+1 WHERE account_id='a'");
    expect(await f.startCore()).toEqual(started);
    expect(await readOrdinaryRoofFromOriginal(original, rail)).toEqual(roof);
    expect(await captureOrdinaryRoofSource(original.origin, original.originHash, null)).toBeNull();
    await expect(captureOrdinaryRoofSource(original.origin, '0'.repeat(64), rail)).rejects.toThrow('ordinary_roof_source_invalid');
  } finally { f.sql.close(); }
});

it('rejects rehashed roof actor/definition/rail changes before initialization and retains source-absent history', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId), pristine = JSON.parse(original.startSnapshotJson);
    const replace = (snapshot: unknown) => {
      const bytes = JSON.stringify(snapshot);
      f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [bytes, runId]);
      f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
      f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, bytes]);
    };
    for (const variant of ['order', 'duplicate', 'definition', 'rail', 'null', 'ready']) {
      const snapshot = structuredClone(pristine), roof = snapshot.ordinaryRoofSource;
      if (variant === 'order') roof.packet.operatorIds.reverse();
      if (variant === 'duplicate') roof.packet.operatorIds[0] = roof.packet.operatorIds[1];
      if (variant === 'definition') roof.packet.definitionHash = '0'.repeat(64);
      if (variant === 'rail') roof.packet.railSourceHash = '0'.repeat(64);
      if (variant === 'ready') roof.packet.status = 'READY';
      roof.sourceHash = await sha256Hex(canonicalJson(roof.packet));
      if (variant === 'null') snapshot.ordinaryRoofSource = null;
      replace(snapshot); const before = f.sql.export();
      await expect(new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId,
        { requestId: 'roof-invalid-' + variant, source: 'ORIGINAL_START' })).rejects.toThrow('ordinary_roof_source_invalid');
      expect(Buffer.from(f.sql.export()).equals(Buffer.from(before))).toBe(true);
    }
    delete pristine.ordinaryRoofSource; replace(pristine);
    const historical = await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId,
      { requestId: 'roof-historical', source: 'ORIGINAL_START' });
    expect(historical).not.toHaveProperty('ordinaryRoofBinding');
  } finally { f.sql.close(); }
});

// Local source verification; server-only checkouts do not carry the Unity assets.
it.skipIf(!existsSync('D:/Project/LASTRAIL/unity-project'))('pins physical definitions to the actual measured Unity assets and preserves carriage gaps', () => {
  const root = 'D:/Project/LASTRAIL/unity-project', definition = captureOrdinaryTrialPhysicalDefinition();
  const read = (path: string) => readFileSync(join(root, path));
  const consistBytes = read('Assets/_LASTRAIL/Content/Battle/OrdinaryTrialConsistV01/definition.json');
  const measured = JSON.parse(consistBytes.toString('utf8'));
  expect(createHash('sha256').update(consistBytes).digest('hex')).toBe(definition.definitionHash);
  expect(definition.selectedLoadout).toEqual(measured.selectedLoadout);
  expect(definition.initialFrontArcMm).toBe(measured.measuredComposition.headArcMm);
  expect(definition.initialTailArcMm).toBe(measured.measuredComposition.tailArcMm);
  expect(definition.totalLengthMm).toBe(measured.measuredComposition.totalLengthMm);
  definition.vehicles.forEach((v, order) => {
    expect(createHash('sha256').update(read(v.assetPath)).digest('hex')).toBe(v.assetHash);
    const actual = measured.measuredComposition.vehicles[order];
    expect(actual.selectionId).toBe(v.selectionId); expect(actual.role).toBe(v.role);
    expect(actual.frontArcMm - actual.rearArcMm).toBe(v.bodyLengthMm);
    const following = measured.measuredComposition.vehicles[order + 1];
    expect(following ? actual.rearArcMm - following.frontArcMm : 0).toBe(v.gapAfterMm);
  });
  const railBytes = read('Assets/_LASTRAIL/Content/Battle/OrdinaryFiniteRail500mV01/definition.json');
  const rail = JSON.parse(railBytes.toString('utf8'));
  expect(createHash('sha256').update(railBytes).digest('hex')).toBe(definition.rail.definitionHash);
  expect(definition.rail.startMm).toEqual(rail.measurement.startMm);
  expect(definition.rail.endMm).toEqual(rail.measurement.endMm);
  expect(definition.rail.lengthMm).toBe(rail.measurement.lengthMm);
  expect(definition.initialTailArcMm).toBeGreaterThanOrEqual(0);
  expect(definition.initialFrontArcMm).toBeLessThanOrEqual(definition.rail.lengthMm);
});

it('rejects changed owned carriages at the rail START commit without partial admission', async () => {
  const f = await fixture(true);
  try {
    f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
      f.sql.run("UPDATE account_carriages SET level=level+1,revision=revision+1 WHERE account_id='a'"); };
    await expect(f.startCore()).rejects.toThrow();
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(0);
    expect(f.scalar("SELECT COUNT(*) FROM fuel_ledger WHERE reason='RUN_START'")).toBe(0);
  } finally { f.sql.close(); }
});


it('requires original departure effect proof for initial motion and keeps QA budget separate from runtime capacity', async () => {
  const f = await fixture(true);
  try {
    const started = await f.startCore(), original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', String(started.runId));
    const train = (await readOrdinaryTrainFromOriginal(original))!, p = started.ordinaryRailSource.packet;
    const input = { accountId: p.accountId, runId: p.runId, trainId: p.trainId, ordinaryOriginHash: p.ordinaryOriginHash,
      ownedSourceHash: original.origin.ownedSourceHash, loadoutRevision: p.loadoutRevision, selectedCarriages: p.selectedCarriages };
    const policy = await captureOrdinaryFirstPlayerRailDefinition(input);
    expect(policy.firstPlayerTrial).toMatchObject({ observationMs: 42000, authoredLengthMm: 1200000,
      gunnerObservationMs: 30000, preWarningDelayMs: 2000, warningMs: 8000, postImpactObservationMs: 2000,
      productTimeLimit: false, departureEffectsTakePrecedence: true, progressCheckpointTakesPrecedence: true,
      capacityRequiresOriginalSpeedEffects: true });
    expect(p.definitionHash).toBe(await sha256Hex(canonicalJson(policy)));
    expect(p.status).toBe('MOTION_BOUND_CAPACITY_UNVERIFIED');
    expect(p).not.toHaveProperty('ownedSourceHash');
    const legacy = await captureOrdinaryTrainSource({ accountId: input.accountId, runId: input.runId,
      ordinaryOriginHash: input.ordinaryOriginHash, ownedSourceHash: input.ownedSourceHash,
      loadoutRevision: input.loadoutRevision, train: train.packet.train });
    for (const missing of [null, legacy]) await expect(captureOrdinaryRailSourceV4(input, missing)).rejects.toThrow('ordinary_rail_initial_motion_unavailable');
    const foreign = structuredClone(train); foreign.packet.runId = 'foreign';
    await expect(captureOrdinaryRailSourceV4(input, foreign)).rejects.toThrow('ordinary_rail_initial_motion_unavailable');
    const changed = structuredClone(train);
    Object.assign(changed.packet, { initialization: { departureEffects: 'UNKNOWN' } });
    await expect(captureOrdinaryRailSourceV4(input, changed)).rejects.toThrow('ordinary_rail_initial_motion_unavailable');
  } finally { f.sql.close(); }
});

it.skipIf(!existsSync('D:/Project/LASTRAIL/unity-project'))('verifies the new 1200m geometry against supplied assets without replacing 500m history', () => {
  const root = 'D:/Project/LASTRAIL/unity-project/';
  const report = JSON.parse(readFileSync('D:/Project/LASTRAIL/QA/OperatorInformation-20261019/PlayerBudget798/verification.json', 'utf8'));
  for (const [path, hash] of Object.entries(report.assetHashes)) {
    expect(createHash('sha256').update(readFileSync(root + path)).digest('hex')).toBe(hash);
  }
  const bytes = readFileSync(root + 'Assets/_LASTRAIL/Content/Battle/OrdinaryFiniteRail1200mV01/definition.json');
  expect(createHash('sha256').update(bytes).digest('hex')).toBe('eeeb0da41505f285ffe497ba789c5610f8c2fab38ec4d1950549cac91b2b00bc');
  const measured = JSON.parse(bytes.toString('utf8'));
  expect(measured.measurement).toMatchObject({ lengthMm: 1200000, startMm: [-25000, 0, 0], endMm: [1175000, 0, 0], rootScale: [1, 1, 1] });
  expect(measured.capacityPolicy).toMatchObject({ qaObservationMs: 42000, productTimeLimit: false });
  expect(captureOrdinaryTrialPhysicalDefinition().rail.lengthMm).toBe(500000);
});

it.each(['DESTROYED', 'PASSED'] as const)('serves obstacle %s restart and completion over local TCP with explicit synthetic execution sources', async outcome => {
  const f = await fixture(true);
  let http: ReturnType<typeof createServer> | undefined;
  try {
    const geometryBytes = JSON.stringify({ version: 'ordinary.native-geometry-definition.v1.20261031', definitionId: 'obstacle-test-geometry',
      units: 'MILLIMETERS', millimetersPerUnit: 1000, axis: 'X_FORWARD_Y_UP_Z_SIDE', coordinateSpace: 'BOUND_FRAME_LOCAL',
      source: { scenePath: 'Assets/Synthetic.unity', sceneHash: 'a'.repeat(64) }, entities: [{ authoringId: 'foot', role: 'OPERATOR',
        definitionRef: 'op_kyle', framePath: 'Root/Kyle', assetPath: 'Assets/Synthetic.prefab', assetHash: 'b'.repeat(64),
        anchors: [{ id: 'foot', role: 'FOOT', positionMm: [0, 0, 0] }], shapes: [] }] });
    const registry = new OrdinaryNativeGeometryRegistry(f.env.DB, { definitionId: 'obstacle-test-geometry',
      version: 'ordinary.native-geometry-definition.v1.20261031', definitionHash: await sha256Hex(geometryBytes) });
    await registry.register(geometryBytes);
    const started = await f.startCore(registry), runId = String(started.runId);
    const original = await readOrdinaryRunStatOrigin(f.env.DB, 'a', runId);
    const source = await captureFirstBattleEncounterSourceV8(original.origin, original.originHash, 'II');
    const profile = (await resolveFirstBattleProfile(source))!, plan = mapFixture();
    for (const n of plan.nodes) { n.encounterProfileRef = n.eventKind === 'COMBAT' ? profile.profileId : `test-${n.eventKind}`; n.rewardPlanRef = `reward-${n.eventKind}`; }
    const kinds = [...new Set(plan.nodes.map(n => n.eventKind))];
    const publication = await new OperationMapPublisher({ read: async () => ({ version: 'operation.map-catalog.v1',
      catalogId: 'SYNTHETIC_OBSTACLE_TCP_TEST', revision: 1, definitionVersion: plan.definitionVersion,
      encounters: kinds.map(eventKind => ({ ref: eventKind === 'COMBAT' ? profile.profileId : `test-${eventKind}`,
        version: eventKind === 'COMBAT' ? profile.version : 'test', profile: 'ORDINARY_12_NODE', eventKind,
        definition: eventKind === 'COMBAT' ? { ...profile, ordinaryObstacle: ordinaryObstacleDefinition() } : {} })),
      rewards: kinds.map(eventKind => ({ ref: `reward-${eventKind}`, version: 'test', profile: 'ORDINARY_12_NODE', eventKind, definition: {} })) }) }).publish(plan);
    const snapshot = { ...JSON.parse(original.startSnapshotJson), risk: 'II', encounterType: 'ORDINARY_12_NODE',
      firstBattleEncounterSource: source, operationMapContent: publication };
    const preview = await f.env.DB.prepare('SELECT objective,balance_config_version FROM operation_previews WHERE id=(SELECT preview_id FROM runs WHERE id=?)')
      .bind(runId).first<{ objective: string; balance_config_version: number }>();
    snapshot.operationId = preview!.objective; snapshot.balanceVersion = preview!.balance_config_version;
    snapshot.admission.operationMapPlan = plan;
    const text = JSON.stringify(snapshot);
    f.sql.run('UPDATE operation_previews SET risk=? WHERE id=(SELECT preview_id FROM runs WHERE id=?)', ['II', runId]);
    f.sql.run('UPDATE run_snapshots SET snapshot_json=? WHERE run_id=?', [text, runId]);
    f.sql.run('DELETE FROM ordinary_combat_phases WHERE run_id=?', [runId]);
    f.sql.run("INSERT INTO ordinary_combat_phases VALUES(?,?,?,?, 'PREPARED',1)", [runId, 'a', original.originHash, text]);
    await new OrdinaryCombatLifecycle(f.env.DB).activate('a', runId, { requestId: 'obstacle-init', source: 'ORIGINAL_START' });
    // Synthetic state provider: validates real capture/storage/HTTP plumbing, not physical execution authority.
    const nodeOwner = new OrdinaryNodeEntrySource(f.env.DB, { prepare: async input => ({ state: { ...input.executionOrigin!,
      sequence: input.sequence, enemies: input.enemies!, spatial: { definitionBinding: input.geometry!.binding }, effects: {},
      actions: { version: 'ordinary.node-actions.v1.20261030', stepTimeSpanTicks: 500000, ultimateAuto: false,
        actors: input.entry.actors.map(a => ({ operatorId: a.operatorId, basicNextOpportunityInTicks: 0,
          activeCooldownRemainingTicks: 0, ultimateCharge: 0 })) } },
      assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')] }) });
    const mapStore = new OperationMapStore(f.env.DB, { prepare: async context => ({
      proof: { kind: context.command.kind, verificationId: 'synthetic-travel', holdIds: [] },
      assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')] }) }, nodeOwner);
    await mapStore.initialize('a', runId);
    const edge = plan.edges.find(e => e.toNodeId === 'n1-0')!;
    f.sql.run('UPDATE operation_map_states SET revision=2,state_json=? WHERE run_id=?', [canonicalJson({ version: 'operation.map-progress.v1',
      traversedEdgeIds: [], pendingEdgeId: edge.edgeId, phase: 'TRAVELLING', holdIds: [] }), runId]);
    const arrivalCommand = { kind: 'ARRIVE', eventId: 'obstacle-arrival', expectedRevision: 2, edgeId: edge.edgeId, evidenceRef: 'synthetic' };
    const arrival = await mapStore.commit('a', runId, arrivalCommand), nodeStart = arrival.nodeStart!;
    expect(await mapStore.commit('a', runId, arrivalCommand)).toEqual(arrival);
    const runtime = nodeStart.snapshot.runtime as Record<string, unknown>;
    expect(runtime.sequence).toMatchObject({ obstacle: { hp: 100 }, nextWave: { kind: 'ORIGINAL_NODE_ENEMIES' } });
    const objective = await firstEncounterObjectiveDefinition(), objectiveRef = { version: objective.version, definitionHash: objective.definitionHash };
    const issued = await firstObjectiveEncounterIdentities({ accountId: 'a', runId, encounterId: arrivalCommand.eventId, sourceHash: source.sourceHash });
    const owned = await readOrdinaryNodeEntryOwners(f.env.DB, 'a', runId);
    let ownerUnavailable = false, obstacleResolution: unknown;
    const local = new Hono<{ Bindings: Env }>();
    registerOperationMapCommandRoutes(local, async () => ({ accountId: 'a' }), () => ({ prepare: async context => {
      if (ownerUnavailable) throw new Error('synthetic-owner-offline');
      return { evidence: { version: 'combat.first-objective-evidence.v1.20261028', accountId: 'a', runId, nodeId: 'n1-0',
        encounterId: arrivalCommand.eventId, evidenceRef: context.command.evidenceRef, mapRevision: context.command.expectedRevision,
        startSnapshotHash: await sha256Hex(text), sourceHash: source.sourceHash, profileHash: profile.definitionHash,
        objective: objectiveRef, nodeObjectives: [objectiveRef], checkpoint: { revision: 7, hash: 'c'.repeat(64), atMs: 30000 }, ...issued,
        defeats: issued.issuedGunnerIds.map((gunnerId, i) => ({ gunnerId, eventId: `defeat-${i}`, atMs: 24000 + i * 1000 })),
        hazard: { hazardId: issued.issuedHazardId, eventId: 'hazard', outcome: 'MISSED', launchedAtMs: 29000, resolvedAtMs: 30000 },
        targets: [...owned.entry.actors.map(a => ({ targetId: a.operatorId, hp: a.hp, maxHp: a.maxHp })),
          { targetId: 'train_a', hp: owned.entry.train.hp - 25, maxHp: owned.entry.train.maxHp }],
        unresolvedProjectileIds: [], remainingThreatIds: [], attemptGeneration: 2, ultimateAuto: false,
        actions: owned.entry.actors.map(a => ({ operatorId: a.operatorId, activeCooldownRemainingTicks: 0, ultimateCharge: 0 })) },
      executionBinding: { executionVersion: runtime.executionVersion, determinism: runtime.determinism, nodeId: 'n1-0',
        attemptGeneration: 2, snapshotHash: nodeStart.snapshotHash, geometryBinding: (runtime.spatial as Record<string, unknown>).definitionBinding },
      obstacleResolution, healthChanges: [{ eventId: 'synthetic-wave-damage-attempt-2', targetId: 'train_a', reason: 'DAMAGE', hp: owned.entry.train.hp - 25 }],
      holdIds: [], assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_node_attempts WHERE run_id=? AND generation=2) THEN 1 ELSE 0 END').bind(runId)] };
    } }));
    http = createServer(async (request, response) => {
      try {
        const chunks: Buffer[] = []; for await (const chunk of request) chunks.push(Buffer.from(chunk));
        const result = await local.request(`http://127.0.0.1${request.url}`, { method: request.method,
          headers: request.headers as Record<string, string>, body: Buffer.concat(chunks).toString() }, f.env);
        response.writeHead(result.status, Object.fromEntries(result.headers)); response.end(await result.text());
      } catch { response.writeHead(500); response.end(); }
    });
    await new Promise<void>(resolve => http!.listen(0, '127.0.0.1', resolve));
    const address = http.address(); if (!address || typeof address === 'string') throw new Error('local server not bound');
    const post = async (path: string, key: string, body: unknown) => {
      const result = await fetch(`http://127.0.0.1:${address.port}/v1/runs/${runId}/operation-map/${path}`, { method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Idempotency-Key': key }, body: JSON.stringify(body) });
      return { status: result.status, body: await result.json() as Record<string, unknown> };
    };
    const restartBody = { nodeId: 'n1-0', expectedRevision: 3, expectedGeneration: 1 };
    const restart = await post('restart', 'restart-obstacle', restartBody);
    expect(restart.status).toBe(200); expect(restart.body.snapshot).toEqual(nodeStart.snapshot);
    expect(await post('restart', 'restart-obstacle', restartBody)).toEqual(restart);
    const scope = { accountId: 'a', runId, nodeId: 'n1-0', attemptGeneration: 2, snapshotHash: nodeStart.snapshotHash };
    const resolution = { ...scope, eventId: 'obstacle-clear', atMs: outcome === 'DESTROYED' ? 22243 : 16320,
      outcome, obstacleHp: outcome === 'DESTROYED' ? 0 : 25, trainHp: owned.entry.train.hp,
      tailBeyondZone: true, remainingHazards: 0, unresolvedProjectiles: 0, collisionCommitted: false };
    const phase = new OrdinaryObstacleSequence(runtime.sequence, scope);
    expect(phase.resolve(resolution).phase).toBe('COMBAT');
    expect(phase.resolve(resolution).revision).toBe(2);
    const completeBody = { nodeId: 'n1-0', evidenceRef: 'obstacle-proof', expectedRevision: restart.body.revision, attemptGeneration: 2 };
    expect((await post('completion', 'complete-obstacle', completeBody)).status).toBe(409);
    expect((await new CombatHealthStore(f.env.DB).read('a', runId)).state.targets.find(t => t.id === 'train_a')!.health.hp).toBe(owned.entry.train.hp);
    obstacleResolution = { ...resolution, attemptGeneration: 1 };
    expect((await post('completion', 'complete-obstacle', completeBody)).body.errorCode).toBe('ordinary_obstacle_attempt_conflict');
    obstacleResolution = resolution;
    const completed = await post('completion', 'complete-obstacle', completeBody);
    expect(completed.status).toBe(200);
    expect((await new CombatHealthStore(f.env.DB).read('a', runId)).state.targets.find(t => t.id === 'train_a')!.health.hp).toBe(owned.entry.train.hp - 25);
    ownerUnavailable = true;
    expect(await post('completion', 'complete-obstacle', completeBody)).toEqual(completed);
    expect(f.scalar('SELECT COUNT(*) FROM ordinary_objective_results')).toBe(1);
    expect(f.scalar('SELECT COUNT(*) FROM run_final_results')).toBe(0);
  } finally {
    if (http) await new Promise<void>((resolve, reject) => http!.close(error => error ? reject(error) : resolve()));
    f.sql.close();
  }
}, 20000);

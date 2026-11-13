import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { runSuppliesStartStatements } from '../../apps/game-api/src/run-supplies-start';
import { RunMaintenanceOfferStore, type RunMaintenanceSourceVerifier } from '../../apps/game-api/src/run-maintenance-offer';
import { OperationMapStore } from '../../apps/game-api/src/operation-map-store';
import { plan } from '../helpers/operation-map-fixture';

const stationId = 'n1-1', sourceRef = 'arrival-proof-1';

async function fixture(withSupplies = true, eventKind: 'MAINTENANCE' | 'EQUIPMENT' = 'MAINTENANCE') {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'maintenance-offer-start')).json();
  const runId = result.runId as string;
  if (withSupplies) {
    const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
    snapshot.encounterType = 'ORDINARY_12_NODE';
    snapshot.runSupplies = { version: 'run.supplies.v02', do04Rank: 0, initialBalance: 30 };
    const map = plan();
    map.nodes.find(node => node.nodeId === stationId)!.eventKind = eventKind;
    if (eventKind === 'MAINTENANCE') map.enabledEventKinds.push('MAINTENANCE');
    snapshot.admission.operationMapPlan = map;
    snapshot.departureResponse.admission.operationMapPlan = map;
    snapshot.departureResponse.encounterType = 'ORDINARY_12_NODE';
    const bytes = JSON.stringify(snapshot);
    f.sql.run('UPDATE run_snapshots SET snapshot_json=?', [bytes]);
    await f.env.DB.batch(runSuppliesStartStatements(f.env.DB, 'a', runId, bytes));
    const mapStore = new OperationMapStore(f.env.DB, { prepare: async ({ command }) => ({
      proof: { kind: command.kind, verificationId: sourceRef, holdIds: [] },
      assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')],
    }) });
    await mapStore.initialize('a', runId);
    await mapStore.commit('a', runId, { kind: 'CONFIRM', eventId: 'choose-station', expectedRevision: 1, edgeId: `entry:${stationId}` });
    await mapStore.commit('a', runId, { kind: 'ARRIVE', eventId: 'arrive-station', expectedRevision: 2,
      edgeId: `entry:${stationId}`, evidenceRef: 'synthetic-arrival' });
  }
  return { ...f, runId };
}

function source(db: D1Database): RunMaintenanceSourceVerifier {
  return { prepare: async () => ({ sourceVersion: 'maintenance-node-v1',
    assertions: [db.prepare('INSERT INTO run_maintenance_offer_write_guard(valid) VALUES(1)')] }) };
}

it('fails closed without a station proof and fixes only three approved repair services', async () => {
  const f = await fixture();
  await expect(new RunMaintenanceOfferStore(f.env.DB).publish('a', f.runId, stationId, sourceRef))
    .rejects.toThrow('run_maintenance_source_required');
  const store = new RunMaintenanceOfferStore(f.env.DB, source(f.env.DB));
  const first = await store.publish('a', f.runId, stationId, sourceRef);
  expect(first).toEqual({ version: 'run.maintenance-offer.v1', stationId,
    sourceRef, sourceVersion: 'maintenance-node-v1', services: [
      { kind: 'LOCOMOTIVE_REPAIR', price: 20, stock: 2 },
      { kind: 'CARRIAGE_REPAIR', price: 15, stock: 2 },
      { kind: 'CARRIAGE_RESTORE', price: 30, stock: 1 },
    ] });
  expect(f.scalar('SELECT count(*) FROM run_maintenance_offers')).toBe(1);
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await store.publish('a', f.runId, stationId, sourceRef)).toEqual(first);
  await expect(store.publish('a', f.runId, stationId, 'different'))
    .rejects.toThrow('run_maintenance_source_conflict');
  await expect(store.read('b', f.runId, stationId)).rejects.toThrow('run_supplies_run_not_found');
});

it('does not duplicate a station under another ID or backfill an old run', async () => {
  const f = await fixture(), store = new RunMaintenanceOfferStore(f.env.DB, source(f.env.DB));
  await store.publish('a', f.runId, stationId, sourceRef);
  await expect(store.publish('a', f.runId, 'station-8', sourceRef))
    .rejects.toThrow('run_maintenance_source_conflict');
  const legacy = await fixture(false);
  await expect(new RunMaintenanceOfferStore(legacy.env.DB, source(legacy.env.DB))
    .publish('a', legacy.runId, stationId, sourceRef))
    .rejects.toThrow('run_supplies_legacy_unsupported');
  expect(legacy.scalar('SELECT count(*) FROM run_maintenance_offers')).toBe(0);
});

it('rejects source races, missing assertions and damaged original stock without silent refill', async () => {
  const f = await fixture();
  const empty = new RunMaintenanceOfferStore(f.env.DB, { prepare: async () => ({
    sourceVersion: 'maintenance-node-v1', assertions: [],
  }) });
  await expect(empty.publish('a', f.runId, stationId, sourceRef))
    .rejects.toThrow('run_maintenance_source_required');
  f.sql.run('CREATE TABLE maintenance_source_test(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO maintenance_source_test VALUES(1)');
  const guarded = new RunMaintenanceOfferStore(f.env.DB, { prepare: async () => ({
    sourceVersion: 'maintenance-node-v1',
    assertions: [f.env.DB.prepare('INSERT INTO run_maintenance_offer_write_guard(valid) SELECT valid FROM maintenance_source_test')],
  }) });
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE maintenance_source_test SET valid=0'); };
  await expect(guarded.publish('a', f.runId, stationId, sourceRef))
    .rejects.toThrow('run_maintenance_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_maintenance_offers')).toBe(0);
  f.sql.run('UPDATE maintenance_source_test SET valid=1');
  const original = await guarded.publish('a', f.runId, stationId, sourceRef);
  expect(original.services).toHaveLength(3);
  f.sql.run('DROP TRIGGER run_maintenance_offer_immutable_update');
  f.sql.run("UPDATE run_maintenance_offers SET services_json='[]'");
  await expect(guarded.read('a', f.runId, stationId)).rejects.toThrow('run_maintenance_storage_invalid');
});

it('rolls back original offer if its insert fails', async () => {
  const f = await fixture(), store = new RunMaintenanceOfferStore(f.env.DB, source(f.env.DB));
  f.sql.run("CREATE TRIGGER fail_maintenance_offer BEFORE INSERT ON run_maintenance_offers BEGIN SELECT RAISE(ABORT,'injected-offer'); END");
  await expect(store.publish('a', f.runId, stationId, sourceRef)).rejects.toThrow('injected-offer');
  expect(f.scalar('SELECT count(*) FROM run_maintenance_offers')).toBe(0);
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
});

it('rejects a non-maintenance node, a mismatched arrival receipt and a changed map state', async () => {
  const other = await fixture(true, 'EQUIPMENT');
  await expect(new RunMaintenanceOfferStore(other.env.DB, source(other.env.DB))
    .publish('a', other.runId, stationId, sourceRef)).rejects.toThrow('run_maintenance_station_unavailable');
  const f = await fixture(), store = new RunMaintenanceOfferStore(f.env.DB, source(f.env.DB));
  await expect(store.publish('a', f.runId, stationId, 'invented-arrival'))
    .rejects.toThrow('run_maintenance_arrival_unverified');
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run('UPDATE operation_map_states SET revision=revision+1'); };
  await expect(store.publish('a', f.runId, stationId, sourceRef))
    .rejects.toThrow('run_maintenance_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_maintenance_offers')).toBe(0);
});

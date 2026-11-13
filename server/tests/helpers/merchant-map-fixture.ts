import { OperationMapStore } from '../../apps/game-api/src/operation-map-store';
import { setup } from './departure-fixture';
import { plan } from './operation-map-fixture';

export const merchantNodeId = 'n1-1';

/** Synthetic arrival only; it does not establish a production node verifier. */
export async function enterMerchantMap(f: Awaited<ReturnType<typeof setup>>, runId: string,
  eventKind: 'MERCHANT' | 'EQUIPMENT' = 'MERCHANT') {
  const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  const map = plan();
  map.nodes.find(node => node.nodeId === merchantNodeId)!.eventKind = eventKind;
  if (eventKind === 'MERCHANT') map.enabledEventKinds.push('MERCHANT');
  snapshot.encounterType = 'ORDINARY_12_NODE';
  snapshot.admission.operationMapPlan = map;
  snapshot.departureResponse.admission.operationMapPlan = map;
  snapshot.departureResponse.encounterType = 'ORDINARY_12_NODE';
  f.sql.run('UPDATE run_snapshots SET snapshot_json=?', [JSON.stringify(snapshot)]);
  const store = new OperationMapStore(f.env.DB, { prepare: async ({ command }) => ({
    proof: { kind: command.kind, verificationId: 'synthetic-merchant-arrival', holdIds: [] },
    assertions: [f.env.DB.prepare('INSERT INTO operation_map_write_guard(valid) VALUES(1)')],
  }) });
  await store.initialize('a', runId);
  await store.commit('a', runId, { kind: 'CONFIRM', eventId: 'choose-merchant', expectedRevision: 1,
    edgeId: `entry:${merchantNodeId}` });
  await store.commit('a', runId, { kind: 'ARRIVE', eventId: 'arrive-merchant', expectedRevision: 2,
    edgeId: `entry:${merchantNodeId}`, evidenceRef: 'synthetic-merchant-arrival' });
}

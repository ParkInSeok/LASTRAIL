import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { plan } from '../helpers/operation-map-fixture';
import { OperationMapPublisher } from '../../apps/game-api/src/operation-map-publisher';
import { startDeparture } from '../../apps/game-api/src/departure';
import { initialRunSupplies, RunSuppliesStore } from '../../apps/game-api/src/run-supplies-start';

async function fixture() {
  const f = await setup(true), quote = await (await f.preview()).json();
  const p = JSON.parse(String(f.scalar('SELECT preview_json FROM operation_previews')));
  p.encounterType = 'ORDINARY_12_NODE';
  p.admission.operationMapPlan = plan();
  for (const node of p.admission.operationMapPlan.nodes) {
    node.encounterProfileRef = `encounter-${node.eventKind}`;
    node.rewardPlanRef = `reward-${node.eventKind}`;
  }
  const kinds = ['COMBAT', 'EQUIPMENT', 'ELITE', 'BOSS'];
  p.operationMapContent = await new OperationMapPublisher({ read: async () => ({
    version: 'operation.map-catalog.v1', catalogId: 'synthetic-only', revision: 1,
    definitionVersion: 'test-def',
    encounters: kinds.map(eventKind => ({ ref: `encounter-${eventKind}`, version: '1', profile: 'ORDINARY_12_NODE', eventKind, definition: { executor: 'fixture-only' } })),
    rewards: kinds.map(eventKind => ({ ref: `reward-${eventKind}`, version: '1', profile: 'ORDINARY_12_NODE', eventKind, definition: { privateReward: 'fixture-only' } })),
  }) }).publish(p.admission.operationMapPlan);
  f.sql.run('UPDATE operation_previews SET preview_json=?', [JSON.stringify(p)]);
  const start = async (key: string) => {
    const row = await f.env.DB.prepare('SELECT * FROM operation_previews WHERE id=?').bind(quote.operationPreviewId).first<Parameters<typeof startDeparture>[2]>();
    return startDeparture(f.env.DB, 'a', row!, key,
      async (_preview, runId) => ({ accepted: true, runId, encounterType: 'ORDINARY_12_NODE' }),
      undefined, f.env.ENVIRONMENT);
  };
  return { ...f, start };
}

it('pins the approved solo opening supply and replays the original START without another credit', async () => {
  expect([0, 1, 2, 3, 4].map(initialRunSupplies)).toEqual([30, 35, 40, 45, 50]);
  for (const rank of [-1, 5, 1.5]) expect(() => initialRunSupplies(rank)).toThrow('run_supplies_rank_invalid');
  const f = await fixture(), result = await f.start('supplies-start');
  expect(result.accepted).toBe(true);
  const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  expect(snapshot.runSupplies).toEqual({ version: 'run.supplies.v02', do04Rank: 0, initialBalance: 30 });
  expect(result.runSupplies).toBeUndefined();
  expect(await new RunSuppliesStore(f.env.DB).read('a', result.runId as string)).toEqual({
    version: 'run.supplies.v02', balance: 30, revision: 1, initialBalance: 30, do04Rank: 0,
  });
  expect(await f.start('supplies-start')).toEqual(result);
  expect(f.scalar('SELECT count(*) FROM run_supplies_ledger')).toBe(1);
  await expect(new RunSuppliesStore(f.env.DB).read('b', result.runId as string)).rejects.toThrow('run_supplies_run_not_found');
});

it('rolls back the complete departure when initial supply storage fails', async () => {
  const f = await fixture();
  f.sql.run("CREATE TRIGGER fail_run_supplies BEFORE INSERT ON run_supplies_state BEGIN SELECT RAISE(ABORT,'injected'); END");
  await expect(f.start('supplies-failure')).rejects.toThrow('run_start_state_conflict');
  expect(f.scalar('SELECT count(*) FROM runs')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM operation_map_states')).toBe(0);
  expect(f.scalar('SELECT fuel FROM fuel_state')).toBe(30);
});

it('leaves an old fifteen-wave START without a supplies state or backfill', async () => {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'old-supplies')).json();
  expect(await new RunSuppliesStore(f.env.DB).read('a', result.runId as string)).toBeNull();
  expect(f.scalar('SELECT count(*) FROM run_supplies_state')).toBe(0);
});

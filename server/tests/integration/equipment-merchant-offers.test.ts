import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { EquipmentMerchantOfferStore, type EquipmentMerchantSourceVerifier } from '../../apps/game-api/src/equipment-merchant-offers';
import { enterMerchantMap, merchantNodeId } from '../helpers/merchant-map-fixture';

const sourceRef = 'merchant-node-result-1';
const stock = [{ stockId: 'merchant-stock-1', definitionId: 'gear-a', definitionVersion: 'catalog-v15',
  categoryId: 'ATK', setTag: 'SET01', star: 2, roll: 95 }];

async function fixture(verifier?: EquipmentMerchantSourceVerifier, eventKind: 'MERCHANT' | 'EQUIPMENT' = 'MERCHANT') {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'merchant-start')).json();
  await enterMerchantMap(f, result.runId as string, eventKind);
  return { ...f, runId: result.runId as string, store: new EquipmentMerchantOfferStore(f.env.DB, verifier) };
}

function syntheticSource(db: D1Database, items = stock): EquipmentMerchantSourceVerifier {
  return { prepare: async () => ({ sourceVersion: 'catalog-v15', visitDepth: 1, stock: items,
    assertions: [db.prepare('INSERT INTO equipment_merchant_offer_write_guard(valid) VALUES(1)')] }) };
}

it('fails closed without a producer and saves fixed original stock for replay', async () => {
  const f = await fixture();
  await expect(f.store.publish('a', f.runId, merchantNodeId, sourceRef)).rejects.toThrow('equipment_merchant_source_required');
  const store = new EquipmentMerchantOfferStore(f.env.DB, syntheticSource(f.env.DB));
  const first = await store.publish('a', f.runId, merchantNodeId, sourceRef);
  expect(first).toMatchObject({ version: 'equipment.merchant-offer.v1', nodeId: merchantNodeId,
    visitDepth: 1, stock: [{ stockId: 'merchant-stock-1', star: 2, basePrice: 16 }] });
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await store.publish('a', f.runId, merchantNodeId, sourceRef)).toEqual(first);
  await expect(store.publish('a', f.runId, merchantNodeId, 'different')).rejects.toThrow('equipment_merchant_source_conflict');
  await expect(store.read('b', f.runId, first.offerId)).rejects.toThrow('equipment_run_not_found');
});

it('rejects invalid count, identities, grades and unsupported enchantment fields', async () => {
  const f = await fixture();
  for (const items of [[], Array(4).fill(stock[0]), [stock[0], stock[0]],
    [{ ...stock[0], star: 4 }], [{ ...stock[0], star: 3 }],
    [{ ...stock[0], independentEnchantmentId: 'unapproved' }]]) {
    const store = new EquipmentMerchantOfferStore(f.env.DB, syntheticSource(f.env.DB, items));
    await expect(store.publish('a', f.runId, merchantNodeId, sourceRef)).rejects.toThrow('equipment_merchant_stock_invalid');
  }
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_offers')).toBe(0);
});

it('rolls back all stock when the original proof changes or a stock insert fails', async () => {
  const f = await fixture();
  f.sql.run('CREATE TABLE merchant_source_test(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO merchant_source_test VALUES(1)');
  const verifier: EquipmentMerchantSourceVerifier = { prepare: async () => ({ sourceVersion: 'catalog-v15',
    visitDepth: 1, stock, assertions: [f.env.DB.prepare('INSERT INTO equipment_merchant_offer_write_guard(valid) SELECT valid FROM merchant_source_test')] }) };
  const store = new EquipmentMerchantOfferStore(f.env.DB, verifier);
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE merchant_source_test SET valid=0'); };
  await expect(store.publish('a', f.runId, merchantNodeId, sourceRef)).rejects.toThrow('equipment_merchant_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_offers')).toBe(0);
  f.sql.run('UPDATE merchant_source_test SET valid=1');
  f.sql.run("CREATE TRIGGER fail_merchant_stock BEFORE INSERT ON run_equipment_merchant_stock BEGIN SELECT RAISE(ABORT,'injected-stock'); END");
  await expect(store.publish('a', f.runId, merchantNodeId, sourceRef)).rejects.toThrow('injected-stock');
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_offers')).toBe(0);
});

it('rejects another node, wrong map depth, missing arrival and a map race', async () => {
  const other = await fixture(undefined, 'EQUIPMENT');
  await expect(new EquipmentMerchantOfferStore(other.env.DB, syntheticSource(other.env.DB))
    .publish('a', other.runId, merchantNodeId, sourceRef)).rejects.toThrow('equipment_merchant_node_unavailable');
  const f = await fixture();
  const store = new EquipmentMerchantOfferStore(f.env.DB, syntheticSource(f.env.DB));
  await expect(store.publish('a', f.runId, 'n1-0', sourceRef)).rejects.toThrow('equipment_merchant_node_unavailable');
  const wrongDepth = new EquipmentMerchantOfferStore(f.env.DB, { prepare: async () => ({
    sourceVersion: 'catalog-v15', visitDepth: 9, stock: [{ ...stock[0]!, roll: 49 }],
    assertions: [f.env.DB.prepare('INSERT INTO equipment_merchant_offer_write_guard(valid) VALUES(1)')],
  }) });
  await expect(wrongDepth.publish('a', f.runId, merchantNodeId, sourceRef)).rejects.toThrow('equipment_merchant_depth_conflict');
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run('UPDATE operation_map_states SET revision=revision+1'); };
  await expect(store.publish('a', f.runId, merchantNodeId, sourceRef)).rejects.toThrow('equipment_merchant_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_offers')).toBe(0);
  f.sql.run('UPDATE operation_map_states SET revision=revision-1');
  f.sql.run('DROP TRIGGER operation_map_receipt_no_delete');
  f.sql.run("DELETE FROM operation_map_receipts WHERE event_id='arrive-merchant'");
  await expect(store.publish('a', f.runId, merchantNodeId, sourceRef)).rejects.toThrow('equipment_merchant_arrival_unverified');
});

import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { enterMerchantMap, merchantNodeId } from '../helpers/merchant-map-fixture';
import { EquipmentMerchantOfferStore } from '../../apps/game-api/src/equipment-merchant-offers';
import { EquipmentMerchantEffectSource, captureMerchantEffects } from '../../apps/game-api/src/equipment-merchant-effects';
import { EquipmentEffectOriginalStore } from '../../apps/game-api/src/equipment-effect-original';
import { EquipmentEffectStateStore } from '../../apps/game-api/src/equipment-effect-state';
import { EquipmentMerchantPurchaseStore } from '../../apps/game-api/src/equipment-merchant-purchase';
import { runSuppliesStartStatements } from '../../apps/game-api/src/run-supplies-start';
import { EquipmentDiscardStore } from '../../apps/game-api/src/equipment-discard';

const stock = [{ stockId: 'stock-1', definitionId: 'gear-a', definitionVersion: 'fixture-catalog-v1',
  categoryId: 'ATK', setTag: 'SET01', star: 2, roll: 95, basePrice: 16 }];
async function fixture(v2=false) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const start = await (await f.start(quote.operationPreviewId, 'merchant-effects-start')).json(), runId = start.runId as string;
  const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  snapshot.encounterType = 'ORDINARY_12_NODE';
  snapshot.runSupplies = { version: 'run.supplies.v02', do04Rank: 0, initialBalance: 30 };
  f.sql.run('UPDATE run_snapshots SET snapshot_json=?', [JSON.stringify(snapshot)]);
  await f.env.DB.batch(runSuppliesStartStatements(f.env.DB, 'a', runId, JSON.stringify(snapshot)));
  await enterMerchantMap(f, runId);
  let calls = 0;
  const store = new EquipmentMerchantOfferStore(f.env.DB, { prepare: async () => {
    calls++;
    const { basePrice: _, ...item } = stock[0]; void _;
    return { sourceVersion: 'fixture-source-v1', visitDepth: 1, stock: [item],effects:v2?[{stockId:item.stockId,snapshot:{version:'equipment.effect-original.v2.20261015',equipment:{definitionId:item.definitionId,definitionVersion:item.definitionVersion,star:item.star},uniqueEffects:[],enchantment:null}}]:undefined,
      assertions: [f.env.DB.prepare('INSERT INTO equipment_merchant_offer_write_guard(valid) VALUES(1)')] };
  } });
  return { ...f, runId, store, calls: () => calls };
}

it('pins low-tier empty effects with the original stock and replays without creating or redrawing', async () => {
  const f = await fixture(), first = await f.store.publish('a', f.runId, merchantNodeId, 'original-stock');
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_effects')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(await f.store.publish('a', f.runId, merchantNodeId, 'original-stock')).toEqual(first);
  expect(f.calls()).toBe(1);
  expect(() => f.sql.run('UPDATE run_equipment_merchant_effects SET snapshot_json=\'{}\''))
    .toThrow('equipment_merchant_effect_immutable');
});

it('uses stored provenance as issuance source and initializes the live effect state in the caller batch', async () => {
  const f = await fixture();
  await f.store.publish('a', f.runId, merchantNodeId, 'original-stock');
  const source = new EquipmentMerchantEffectSource(f.env.DB);
  const original = new EquipmentEffectOriginalStore(f.env.DB, source);
  const context = { instanceId: 'new-gear', sourceItemId: 'stock-1', expectedInventoryRevision: 1 };
  await expect(original.prepare('b', f.runId, context)).rejects.toThrow('equipment_run_not_found');
  const prepared = await original.prepare('a', f.runId, context);
  await expect(f.env.DB.batch(prepared.statements)).rejects.toThrow(); // No purchase provenance yet.
  const offerId = String(f.scalar('SELECT offer_id FROM run_equipment_merchant_offers'));
  const buyer = new EquipmentMerchantPurchaseStore(f.env.DB, { prepare: async () => ({
    assertions: [f.env.DB.prepare('INSERT INTO equipment_merchant_purchase_write_guard(valid) VALUES(1)')] }) }, source);
  const receipt = await buyer.buy('a', f.runId, { commandId: 'buy-fixed', offerId, stockId: 'stock-1', expectedPrice: 16,
    expectedInventoryRevision: 1, expectedSuppliesRevision: 1 });
  expect(await new EquipmentEffectStateStore(f.env.DB).read('a', f.runId, receipt.instanceId)).toMatchObject({
    snapshot: { equipment: { star: 2 }, uniqueEffects: [], enchantment: null } });
});

it('preserves the original merchant offer through discard and requires an explicit purchase with the new revision', async () => {
  const f = await fixture(), offer = await f.store.publish('a', f.runId, merchantNodeId, 'original-stock');
  for (let i = 0; i < 30; i++) f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,
    definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision)
    VALUES(?, ?, ?, 'gear-a','fixture-v1','ATK','SET01',1,0,'STORED',1)`, [`old-${i}`, f.runId, `old-source-${i}`]);
  const source = new EquipmentMerchantEffectSource(f.env.DB);
  const buyer = new EquipmentMerchantPurchaseStore(f.env.DB, { prepare: async () => ({
    assertions: [f.env.DB.prepare('INSERT INTO equipment_merchant_purchase_write_guard(valid) VALUES(1)')] }) }, source);
  const buy = { commandId: 'buy-after-discard', offerId: offer.offerId, stockId: 'stock-1', expectedPrice: 16,
    expectedInventoryRevision: 1, expectedSuppliesRevision: 1 };
  await expect(buyer.buy('a', f.runId, buy)).rejects.toThrow('equipment_capacity_exceeded');
  const records = () => ['run_equipment_merchant_stock', 'run_equipment_merchant_effects', 'run_supplies_ledger', 'run_supplies_state', 'operation_map_states']
    .map(table => f.sql.exec(`SELECT * FROM ${table}`));
  const before = records();
  const discard = new EquipmentDiscardStore(f.env.DB, { prepare: async () => ({
    assertions: [f.env.DB.prepare('INSERT INTO equipment_inventory_write_guard(valid) VALUES(1)')] }) });
  await discard.discard('a', f.runId, { commandId: 'discard-for-merchant', instanceId: 'old-0', expectedInventoryRevision: 1 });
  expect(records()).toEqual(before);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(0);
  expect(await f.store.publish('a', f.runId, merchantNodeId, 'original-stock')).toEqual(offer);
  expect(f.calls()).toBe(1);
  await expect(buyer.buy('a', f.runId, buy)).rejects.toThrow('equipment_revision_conflict');
  const receipt = await buyer.buy('a', f.runId, { ...buy, expectedInventoryRevision: 2 });
  expect(receipt).toMatchObject({ price: 16, balanceAfter: 14, inventoryRevision: 3, suppliesRevision: 2 });
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(30);
  expect(f.calls()).toBe(1);
});

it('rolls back offer and stock if fixed effect insertion fails', async () => {
  const f = await fixture();
  f.sql.run("CREATE TRIGGER fail_stock_effect BEFORE INSERT ON run_equipment_merchant_effects BEGIN SELECT RAISE(ABORT,'injected-stock-effect'); END");
  await expect(f.store.publish('a', f.runId, merchantNodeId, 'original-stock')).rejects.toThrow('injected-stock-effect');
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_offers')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_stock')).toBe(0);
});

it('never backfills missing history and rejects corrupted stored effect data', async () => {
  const f = await fixture(), source = new EquipmentMerchantEffectSource(f.env.DB);
  const context = { accountId: 'a', runId: f.runId, instanceId: 'new-gear', sourceItemId: 'stock-1', expectedInventoryRevision: 1 };
  await expect(source.prepare(context)).rejects.toThrow('equipment_merchant_effect_not_found');
  await f.store.publish('a', f.runId, merchantNodeId, 'original-stock');
  f.sql.run('DROP TRIGGER equipment_merchant_effect_no_update');
  f.sql.run('UPDATE run_equipment_merchant_effects SET payload_hash=?', ['0'.repeat(64)]);
  await expect(source.prepare(context)).rejects.toThrow('equipment_merchant_effect_invalid');
});

it('requires a fixed resolved grade-I original for three-star stock and validates exact identities', () => {
  const three = [{ ...stock[0], star: 3 }];
  expect(() => captureMerchantEffects(three, undefined)).toThrow('equipment_merchant_effect_required');
  const snapshot = { version: 'equipment.effect-original.v1', equipment: { definitionId: 'gear-a', definitionVersion: 'fixture-catalog-v1', star: 3 },
    uniqueEffects: [{ effectId: 'EQ02', grade: 1, definitionVersion: 'fixture-effect-v1', definitionSnapshot: { fixtureOnly: true, coefficient: 123 } }], enchantment: null };
  expect(captureMerchantEffects(three, [{ stockId: 'stock-1', snapshot }])[0].snapshot).toEqual(snapshot);
  for (const invalid of [[{ stockId: 'wrong', snapshot }], [{ stockId: 'stock-1', snapshot }, { stockId: 'stock-1', snapshot }],
    [{ stockId: 'stock-1', snapshot: { ...snapshot, equipment: { ...snapshot.equipment, star: 2 } } }]]) {
    expect(() => captureMerchantEffects(three, invalid)).toThrow('equipment_merchant_effect_invalid');
  }
});

it('fences a fixed effect record change during purchase and rolls back payment and issuance', async () => {
  const f = await fixture(), offer = await f.store.publish('a', f.runId, merchantNodeId, 'original-stock');
  const buyer = new EquipmentMerchantPurchaseStore(f.env.DB, { prepare: async () => ({
    assertions: [f.env.DB.prepare('INSERT INTO equipment_merchant_purchase_write_guard(valid) VALUES(1)')] }) },
  new EquipmentMerchantEffectSource(f.env.DB));
  f.adapter.beforeBatch = () => {
    f.adapter.beforeBatch = undefined;
    f.sql.run('DROP TRIGGER equipment_merchant_effect_no_update');
    f.sql.run('UPDATE run_equipment_merchant_effects SET payload_hash=?', ['0'.repeat(64)]);
  };
  await expect(buyer.buy('a', f.runId, { commandId: 'race-buy', offerId: offer.offerId, stockId: 'stock-1', expectedPrice: 16,
    expectedInventoryRevision: 1, expectedSuppliesRevision: 1 })).rejects.toThrow('equipment_merchant_state_conflict');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(0);
});

it('does not recreate missing effect history when an old offer is replayed', async () => {
  const f = await fixture(), offer = await f.store.publish('a', f.runId, merchantNodeId, 'original-stock');
  f.sql.run('DROP TRIGGER equipment_merchant_effect_no_delete');
  f.sql.run('DELETE FROM run_equipment_merchant_effects');
  expect(await f.store.publish('a', f.runId, merchantNodeId, 'original-stock')).toEqual(offer);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_effects')).toBe(0);
  await expect(new EquipmentMerchantEffectSource(f.env.DB).prepare({ accountId: 'a', runId: f.runId,
    instanceId: 'new-gear', sourceItemId: 'stock-1', expectedInventoryRevision: 2 })).rejects.toThrow('equipment_merchant_effect_not_found');
});

it('publishes v2 no-enchantment stock and purchases it with the pinned original version',async()=>{
 const f=await fixture(true),offer=await f.store.publish('a',f.runId,merchantNodeId,'v2-stock');
 const buyer=new EquipmentMerchantPurchaseStore(f.env.DB,{prepare:async()=>({assertions:[f.env.DB.prepare('INSERT INTO equipment_merchant_purchase_write_guard VALUES(1)')]})},new EquipmentMerchantEffectSource(f.env.DB));
 const receipt=await buyer.buy('a',f.runId,{commandId:'v2-buy',offerId:offer.offerId,stockId:'stock-1',expectedPrice:16,expectedInventoryRevision:1,expectedSuppliesRevision:1});
 expect((await new EquipmentEffectStateStore(f.env.DB).read('a',f.runId,receipt.instanceId))?.snapshot.version).toBe('equipment.effect-state.v2.20261015');
});

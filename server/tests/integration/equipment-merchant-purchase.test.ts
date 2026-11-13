import { expect, it } from 'vitest';
import { setup } from '../helpers/departure-fixture';
import { runSuppliesStartStatements, RunSuppliesStore } from '../../apps/game-api/src/run-supplies-start';
import { EquipmentInventoryStore } from '../../apps/game-api/src/equipment-inventory-start';
import { EquipmentMerchantOfferStore } from '../../apps/game-api/src/equipment-merchant-offers';
import { EquipmentMerchantPurchaseStore, type EquipmentMerchantPurchaseVerifier } from '../../apps/game-api/src/equipment-merchant-purchase';
import { EquipmentMerchantStatusStore } from '../../apps/game-api/src/equipment-merchant-status';
import { enterMerchantMap, merchantNodeId } from '../helpers/merchant-map-fixture';
import { fixtureEquipmentEffectSource } from '../helpers/equipment-effect-source-fixture';
import { EquipmentEffectStateStore } from '../../apps/game-api/src/equipment-effect-state';
import { equipmentEffectOriginalSchema } from '../../apps/game-api/src/equipment-effect-snapshot';

const stock = [0, 1, 2].map(index => ({ stockId: `merchant-item-${index}`, definitionId: `gear-${index}`,
  definitionVersion: 'catalog-v15', categoryId: 'ATK', setTag: 'SET01' as const, star: 2, roll: 95 }));

async function fixture(ranks: Record<string, number> = {}) {
  const f = await setup(true), quote = await (await f.preview()).json();
  const result = await (await f.start(quote.operationPreviewId, 'merchant-purchase-start')).json();
  const runId = result.runId as string;
  const snapshot = JSON.parse(String(f.scalar('SELECT snapshot_json FROM run_snapshots')));
  snapshot.encounterType = 'ORDINARY_12_NODE';
  snapshot.runSupplies = { version: 'run.supplies.v02', do04Rank: ranks.DO04 ?? 0, initialBalance: 30 + (ranks.DO04 ?? 0) * 5 };
  snapshot.admission.commanderTraits = { ranks };
  const bytes = JSON.stringify(snapshot);
  f.sql.run('UPDATE run_snapshots SET snapshot_json=?', [bytes]);
  await f.env.DB.batch(runSuppliesStartStatements(f.env.DB, 'a', runId, bytes));
  await enterMerchantMap(f, runId);
  const offer = await new EquipmentMerchantOfferStore(f.env.DB, { prepare: async () => ({ sourceVersion: 'catalog-v15', visitDepth: 1,
    stock, assertions: [f.env.DB.prepare('INSERT INTO equipment_merchant_offer_write_guard(valid) VALUES(1)')] }) })
    .publish('a', runId, merchantNodeId, 'merchant-original');
  return { ...f, runId, offer };
}

function syntheticVerifier(db: D1Database): EquipmentMerchantPurchaseVerifier {
  return { prepare: async () => ({ assertions: [db.prepare('INSERT INTO equipment_merchant_purchase_write_guard(valid) VALUES(1)')] }) };
}

function command(offerId: string, stockId = stock[0]!.stockId, price = 16, revision = 1, commandId = 'buy-1') {
  return { commandId, offerId, stockId, expectedPrice: price,
    expectedSuppliesRevision: revision, expectedInventoryRevision: revision };
}

it('requires an effect source before issuing a newly purchased physical item', async () => {
  const f = await fixture();
  await expect(new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB))
    .buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_effect_source_required');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
});

it('initializes merchant effects atomically and preserves them during original receipt replay', async () => {
  const f = await fixture(), source = fixtureEquipmentEffectSource(f.env.DB);
  let calls = 0;
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), { prepare: async context => {
    calls++; return source.prepare(context);
  } });
  const receipt = await store.buy('a', f.runId, command(f.offer.offerId));
  const state = await new EquipmentEffectStateStore(f.env.DB).read('a', f.runId, receipt.instanceId);
  expect(state).toMatchObject({ effectRevision: 1, instanceRevision: 1, inventoryRevision: 2,
    snapshot: { equipment: { definitionId: 'gear-0', star: 2 }, uniqueEffects: [], enchantment: null } });
  expect(await store.buy('a', f.runId, command(f.offer.offerId))).toEqual(receipt);
  expect(calls).toBe(1);
});

it('rolls back merchant payment, sale and instance when current effect initialization fails', async () => {
  const f = await fixture();
  f.sql.run("CREATE TRIGGER fail_purchase_head BEFORE INSERT ON run_equipment_effect_heads BEGIN SELECT RAISE(ABORT,'injected-effect-head'); END");
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  await expect(store.buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('injected-effect-head');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  for (const table of ['run_equipment_instances', 'run_equipment_merchant_purchases', 'run_equipment_effect_originals', 'run_equipment_effect_history', 'run_equipment_receipts']) {
    expect(f.scalar(`SELECT count(*) FROM ${table}`)).toBe(0);
  }
});

it('rejects enchantment in a newly purchased merchant item before charging', async () => {
  const f = await fixture(), source = fixtureEquipmentEffectSource(f.env.DB);
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), { prepare: async context => {
    const proof = await source.prepare(context);
    return { ...proof, snapshot: { ...equipmentEffectOriginalSchema.parse(proof.snapshot), enchantment: {
      effectId: 'EN08', appliedStar: 2, definitionVersion: 'fixture-enchant-v1', definitionSnapshot: { fixtureOnly: true } } } };
  } });
  await expect(store.buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_merchant_enchantment_invalid');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
});

it('requires a real purchase-state proof and atomically spends, sells and stores once', async () => {
  const f = await fixture();
  await expect(new EquipmentMerchantPurchaseStore(f.env.DB).buy('a', f.runId, command(f.offer.offerId)))
    .rejects.toThrow('equipment_merchant_purchase_verification_required');
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  const first = await store.buy('a', f.runId, command(f.offer.offerId));
  expect(first).toMatchObject({ version: 'equipment.merchant-purchase.v1', stockId: stock[0]!.stockId,
    price: 16, balanceAfter: 14, suppliesRevision: 2, inventoryRevision: 2 });
  expect((await new RunSuppliesStore(f.env.DB).read('a', f.runId))?.balance).toBe(14);
  expect((await new EquipmentInventoryStore(f.env.DB).readInventory('a', f.runId))?.usedSlots).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(1);
  await expect(store.buy('a', f.runId, command(f.offer.offerId, stock[0]!.stockId, 16, 2, 'buy-2')))
    .rejects.toThrow('equipment_merchant_sold_out');
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  expect(await store.buy('a', f.runId, command(f.offer.offerId))).toEqual(first);
  await expect(store.buy('b', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_run_not_found');
});

it('uses only successful purchases for X08 and never silently changes the confirmed price', async () => {
  const f = await fixture({ DO04: 4, DO03: 4, X08: 2 });
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  await expect(store.buy('a', f.runId, command(f.offer.offerId, stock[0]!.stockId, 16)))
    .rejects.toThrow('equipment_merchant_price_changed');
  const first = await store.buy('a', f.runId, command(f.offer.offerId, stock[0]!.stockId, 14));
  const second = await store.buy('a', f.runId, command(f.offer.offerId, stock[1]!.stockId, 14, 2, 'buy-2'));
  const third = await store.buy('a', f.runId, command(f.offer.offerId, stock[2]!.stockId, 15, 3, 'buy-3'));
  expect([first.price, second.price, third.price]).toEqual([14, 14, 15]);
  expect((await new RunSuppliesStore(f.env.DB).read('a', f.runId))?.balance).toBe(7);
  await expect(store.buy('a', f.runId, command(f.offer.offerId, stock[2]!.stockId, 15, 3, 'buy-4')))
    .rejects.toThrow('equipment_merchant_sold_out');
});

it('leaves money, stock and instances unchanged on insufficient funds, stale revisions and key conflict', async () => {
  const f = await fixture(), store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  await expect(store.buy('a', f.runId, command(f.offer.offerId, stock[0]!.stockId, 16, 2)))
    .rejects.toThrow('equipment_revision_conflict');
  await expect(store.buy('a', f.runId, command(f.offer.offerId, stock[0]!.stockId, 32)))
    .rejects.toThrow('equipment_merchant_price_changed');
  const first = await store.buy('a', f.runId, command(f.offer.offerId));
  await expect(store.buy('a', f.runId, command(f.offer.offerId, stock[1]!.stockId, 16, 2, 'buy-2')))
    .rejects.toThrow('run_supplies_insufficient');
  await expect(store.buy('a', f.runId, command(f.offer.offerId, stock[1]!.stockId, 16, 2)))
    .rejects.toThrow('equipment_request_conflict');
  expect(first.balanceAfter).toBe(14);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(1);
});

it('rejects a full physical storage before any supply debit or stock sale', async () => {
  const f = await fixture();
  for (let n = 0; n < 30; n++) {
    f.sql.run(`INSERT INTO run_equipment_instances(instance_id,run_id,source_item_id,definition_id,
      definition_version,category_id,set_tag,star,locked,location_kind,revision)
      VALUES(?,?,?,?,?,'ATK','SET01',1,0,'STORED',1)`, [`existing-${n}`, f.runId, `existing-source-${n}`, 'existing-gear', 'catalog-v15']);
  }
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  await expect(store.buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_capacity_exceeded');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(30);
});

it('rolls back wallet, stock and instance when proof changes or receipt insertion fails', async () => {
  const f = await fixture();
  f.sql.run('CREATE TABLE merchant_buy_test(valid INTEGER NOT NULL)');
  f.sql.run('INSERT INTO merchant_buy_test VALUES(1)');
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, { prepare: async () => ({
    assertions: [f.env.DB.prepare('INSERT INTO equipment_merchant_purchase_write_guard(valid) SELECT valid FROM merchant_buy_test')],
  }) }, fixtureEquipmentEffectSource(f.env.DB));
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined; f.sql.run('UPDATE merchant_buy_test SET valid=0'); };
  await expect(store.buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_merchant_state_conflict');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  f.sql.run('UPDATE merchant_buy_test SET valid=1');
  f.sql.run("CREATE TRIGGER fail_merchant_purchase BEFORE INSERT ON run_equipment_merchant_purchases BEGIN SELECT RAISE(ABORT,'injected-buy'); END");
  await expect(store.buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('injected-buy');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
});

it('projects original stock, paid-purchase price progression and current revisions for its owner', async () => {
  const f = await fixture({ DO04: 4, DO03: 4, X08: 2 });
  const status = new EquipmentMerchantStatusStore(f.env.DB);
  const initial = await status.read('a', f.runId, f.offer.offerId);
  expect(initial).toMatchObject({ version: 'equipment.merchant-status.v1', runStatus: 'ACTIVE',
    supplies: { balance: 50, revision: 1 }, inventory: { usedSlots: 0, capacitySlots: 30, revision: 1 } });
  expect(initial.stock[0]).toMatchObject({ stockId: stock[0]!.stockId, sold: false, currentPrice: 14 });
  const buyer = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  await buyer.buy('a', f.runId, command(f.offer.offerId, stock[0]!.stockId, 14));
  const afterOne = await status.read('a', f.runId, f.offer.offerId);
  expect(afterOne).toMatchObject({ supplies: { balance: 36, revision: 2 }, inventory: { usedSlots: 1, revision: 2 } });
  expect(afterOne.stock[0]).toMatchObject({ stockId: stock[0]!.stockId, sold: true, currentPrice: null });
  expect(afterOne.stock[1]).toMatchObject({ stockId: stock[1]!.stockId, sold: false, currentPrice: 14 });
  await buyer.buy('a', f.runId, command(f.offer.offerId, stock[1]!.stockId, 14, 2, 'buy-2'));
  const afterTwo = await status.read('a', f.runId, f.offer.offerId);
  expect(afterTwo.stock[2]).toMatchObject({ sold: false, currentPrice: 15 });
  expect(afterTwo.stock.map(item => item.basePrice)).toEqual([16, 16, 16]);
});

it('keeps owner-bound historical sale projection after close and rejects corrupt price history', async () => {
  const f = await fixture({ DO04: 4, DO03: 4, X08: 2 });
  const buyer = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  const status = new EquipmentMerchantStatusStore(f.env.DB);
  await buyer.buy('a', f.runId, command(f.offer.offerId, stock[0]!.stockId, 14));
  f.sql.run("UPDATE runs SET status='COMPLETED'");
  const closed = await status.read('a', f.runId, f.offer.offerId);
  expect(closed.runStatus).toBe('COMPLETED');
  expect(closed.stock[0]).toMatchObject({ stockId: stock[0]!.stockId, sold: true, currentPrice: null });
  await expect(status.read('b', f.runId, f.offer.offerId)).rejects.toThrow('equipment_run_not_found');
  f.sql.run('DROP TRIGGER run_equipment_merchant_purchase_immutable_update');
  f.sql.run('UPDATE run_equipment_merchant_purchases SET price=13');
  await expect(status.read('a', f.runId, f.offer.offerId)).rejects.toThrow('equipment_merchant_storage_invalid');
});

it('rejects new purchases after leaving the merchant but replays a prior purchase', async () => {
  const f = await fixture({ DO04: 4 });
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  const original = await store.buy('a', f.runId, command(f.offer.offerId));
  const state = JSON.parse(String(f.scalar('SELECT state_json FROM operation_map_states')));
  state.phase = 'READY';
  f.sql.run('UPDATE operation_map_states SET revision=revision+1,state_json=?', [JSON.stringify(state)]);
  expect(await store.buy('a', f.runId, command(f.offer.offerId))).toEqual(original);
  await expect(store.buy('a', f.runId, command(f.offer.offerId, stock[1]!.stockId, 16, 2, 'after-leaving')))
    .rejects.toThrow('equipment_merchant_node_unavailable');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(34);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(1);
});

it('rolls back a new purchase if the map or arrival receipt changes during its transaction', async () => {
  const f = await fixture();
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run('UPDATE operation_map_states SET revision=revision+1'); };
  await expect(store.buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_merchant_state_conflict');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
  f.sql.run('UPDATE operation_map_states SET revision=revision-1');
  f.sql.run('DROP TRIGGER operation_map_receipt_no_delete');
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run("DELETE FROM operation_map_receipts WHERE event_id='arrive-merchant'"); };
  await expect(store.buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_merchant_state_conflict');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it('requires the current arrival receipt even when original merchant stock exists', async () => {
  const f = await fixture();
  f.sql.run('DROP TRIGGER operation_map_receipt_no_delete');
  f.sql.run("DELETE FROM operation_map_receipts WHERE event_id='arrive-merchant'");
  await expect(new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB))
    .buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_merchant_arrival_unverified');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(0);
});

it('does not purchase when the current supply ledger disappears after preparation', async () => {
  const f = await fixture();
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  f.sql.run('DROP TRIGGER run_supplies_ledger_immutable_delete');
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run('DELETE FROM run_supplies_ledger'); };
  await expect(store.buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_merchant_state_conflict');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(30);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
});

it('rejects a same-revision balance change before any purchase writes', async () => {
  const f = await fixture();
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  // Simulate damaged storage; the normal DB revision trigger already rejects this mutation.
  f.sql.run('DROP TRIGGER run_supplies_revision_step');
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run('UPDATE run_supplies_state SET balance=31'); };
  await expect(store.buy('a', f.runId, command(f.offer.offerId))).rejects.toThrow('equipment_merchant_state_conflict');
  expect(f.scalar('SELECT count(*) FROM run_supplies_ledger')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(0);
  expect(f.scalar('SELECT count(*) FROM run_equipment_receipts')).toBe(0);
});

it('keeps the START supply entry required after later purchases', async () => {
  const f = await fixture({ DO04: 4 });
  const store = new EquipmentMerchantPurchaseStore(f.env.DB, syntheticVerifier(f.env.DB), fixtureEquipmentEffectSource(f.env.DB));
  await store.buy('a', f.runId, command(f.offer.offerId));
  f.sql.run('DROP TRIGGER run_supplies_ledger_immutable_update');
  f.adapter.beforeBatch = () => { f.adapter.beforeBatch = undefined;
    f.sql.run("UPDATE run_supplies_ledger SET delta=delta+1 WHERE source_kind='START'"); };
  await expect(store.buy('a', f.runId, command(f.offer.offerId, stock[1]!.stockId, 16, 2, 'buy-2')))
    .rejects.toThrow('equipment_merchant_state_conflict');
  expect(f.scalar('SELECT balance FROM run_supplies_state')).toBe(34);
  expect(f.scalar('SELECT count(*) FROM run_equipment_merchant_purchases')).toBe(1);
  expect(f.scalar('SELECT count(*) FROM run_equipment_instances')).toBe(1);
});

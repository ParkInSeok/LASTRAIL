import { canonicalJson, sha256Hex } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';
import { walletId } from './crystal-wallet';
import { EquipmentEffectOriginalStore, type EquipmentEffectOriginalSource } from './equipment-effect-original';
import { captureRewardEffects, rewardEffectHash } from './equipment-reward-effects';

const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const rewardItem = z.strictObject({
  itemId: id, instanceId: id, definitionId: id, definitionVersion: id,
  categoryId: id, setTag: z.enum(['SET01', 'SET02', 'SET03', 'SET04', 'SET05', 'SET06']),
  star: z.number().int().min(1).max(5),
});
const preparedSource = z.strictObject({
  sourceVersion: id, items: z.array(rewardItem).min(1),
});
const claimCommand = z.strictObject({
  commandId: id, bundleId: id, itemIds: z.array(id).min(1).max(100),
  expectedInventoryRevision: z.number().int().min(1).max(Number.MAX_SAFE_INTEGER - 1),
});
export const claimReceipt = z.strictObject({
  version: z.literal('equipment.claim-receipt.v1'), commandId: id, bundleId: id,
  claimedItemIds: z.array(id).min(1), inventoryRevision: z.number().int().positive(),
});
export const declineReceipt = z.strictObject({
  version: z.literal('equipment.decline-receipt.v1'), commandId: id, bundleId: id,
  declinedItemIds: z.array(id).min(1), inventoryRevision: z.number().int().positive(),
});
type Item = z.infer<typeof rewardItem>;
type Bundle = { version: 'equipment.reward-bundle.v1'; bundleId: string; nodeId: string;
  sourceRef: string; sourceVersion: string; items: (Item & { status: string })[] };
type BundleRow = { bundle_id: string; run_id: string; node_id: string; source_ref: string;
  source_version: string; payload_hash: string };
type ItemRow = { item_json: string; status: string };

export interface EquipmentRewardSourceVerifier {
  /** The real node completion/reward producer must supply same-batch assertions proving
   * the original result and its version. No production verifier or public route is installed. */
  prepare(context: { accountId: string; runId: string; nodeId: string; sourceRef: string }):
    Promise<{ sourceVersion: string; items: Item[];
      /** Original supported resolved results, mandatory for every high-tier item. */
      effects?: { itemId: string; snapshot: unknown }[]; assertions: D1PreparedStatement[] }>;
}

export interface EquipmentRewardResolutionVerifier {
  /** Must prove this node still permits item resolution in the same batch.
   * No production verifier or route is installed. */
  prepare(context: { kind: 'CLAIM_ITEMS' | 'DECLINE_REMAINING'; accountId: string;
    runId: string; bundleId: string; itemIds: string[] }):
    Promise<{ assertions: D1PreparedStatement[] }>;
}

/** Internal reward ledger. Physical issuance requires an original bundle and claim proof. */
export class EquipmentRewardBundleStore {
  constructor(private readonly db: D1Database, private readonly source?: EquipmentRewardSourceVerifier,
    private readonly resolutionVerifier?: EquipmentRewardResolutionVerifier,
    private readonly effectSource?: EquipmentEffectOriginalSource) {}

  private async owner(accountId: string, runId: string): Promise<void> {
    const pinned = await new EquipmentInventoryStore(this.db).readPinnedCapacity(accountId, runId);
    if (!pinned) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
  }

  private async original(runId: string, nodeId: string): Promise<BundleRow | null> {
    return this.db.prepare('SELECT * FROM run_equipment_reward_bundles WHERE run_id=? AND node_id=?')
      .bind(runId, nodeId).first<BundleRow>();
  }

  private normalizeSelection(raw: unknown, errorCode: string): z.infer<typeof claimCommand> {
    const parsed = claimCommand.safeParse(raw);
    if (!parsed.success || new Set(parsed.data.itemIds).size !== parsed.data.itemIds.length) {
      throw new ActivityError(400, errorCode);
    }
    return { ...parsed.data, itemIds: [...parsed.data.itemIds].sort() };
  }

  private async originalReceipt<T>(runId: string, commandId: string, kind: string, hash: string,
    schema: z.ZodType<T>): Promise<T | null> {
    const row = await this.db.prepare('SELECT command_kind,request_hash,response_json FROM run_equipment_receipts WHERE run_id=? AND command_id=?')
      .bind(runId, commandId).first<{ command_kind: string; request_hash: string; response_json: string }>();
    if (!row) return null;
    if (row.command_kind !== kind || row.request_hash !== hash) throw new ActivityError(409, 'equipment_request_conflict');
    try { return schema.parse(JSON.parse(row.response_json)); }
    catch { throw new ActivityError(503, 'equipment_receipt_invalid'); }
  }

  private async requireActiveRun(accountId: string, runId: string): Promise<void> {
    const run = await this.db.prepare('SELECT status FROM runs WHERE id=? AND account_id=?')
      .bind(runId, accountId).first<{ status: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');
  }

  private async resolutionProof(kind: 'CLAIM_ITEMS' | 'DECLINE_REMAINING', accountId: string,
    runId: string, bundleId: string, itemIds: string[]): Promise<D1PreparedStatement[]> {
    const errorCode = kind === 'CLAIM_ITEMS' ? 'equipment_claim_verification_required' : 'equipment_resolution_verification_required';
    if (!this.resolutionVerifier) throw new ActivityError(409, errorCode);
    const proof = await this.resolutionVerifier.prepare({ kind, accountId, runId, bundleId, itemIds });
    if (!proof || !Array.isArray(proof.assertions) || proof.assertions.length === 0) {
      throw new ActivityError(409, errorCode);
    }
    return proof.assertions;
  }

  async read(accountId: string, runId: string, bundleId: string): Promise<Bundle> {
    await this.owner(accountId, runId);
    const row = await this.db.prepare('SELECT * FROM run_equipment_reward_bundles WHERE run_id=? AND bundle_id=?')
      .bind(runId, bundleId).first<BundleRow>();
    if (!row) throw new ActivityError(409, 'equipment_reward_bundle_not_found');
    const rows = (await this.db.prepare('SELECT item_json,status FROM run_equipment_reward_items WHERE run_id=? AND bundle_id=? ORDER BY sort_index')
      .bind(runId, bundleId).all<ItemRow>()).results;
    try {
      const originalItems = rows.map(r => rewardItem.parse(JSON.parse(r.item_json)));
      const items = rows.map((row, index) => ({ ...originalItems[index]!, status: row.status }));
      const hash = await sha256Hex(canonicalJson({ sourceVersion: row.source_version, items: originalItems }));
      if (hash !== row.payload_hash || items.length === 0) throw new Error('reward checksum');
      return { version: 'equipment.reward-bundle.v1', bundleId, nodeId: row.node_id,
        sourceRef: row.source_ref, sourceVersion: row.source_version, items };
    } catch { throw new ActivityError(503, 'equipment_reward_storage_invalid'); }
  }

  async publishBase(accountId: string, runId: string, nodeId: string, sourceRef: string): Promise<Bundle> {
    if (!id.safeParse(nodeId).success || !id.safeParse(sourceRef).success) throw new ActivityError(400, 'equipment_reward_request_invalid');
    await this.owner(accountId, runId);
    const existing = await this.original(runId, nodeId);
    if (existing) {
      if (existing.source_ref !== sourceRef) throw new ActivityError(409, 'equipment_reward_source_conflict');
      return this.read(accountId, runId, existing.bundle_id);
    }
    const run = await this.db.prepare('SELECT status FROM runs WHERE id=? AND account_id=?').bind(runId, accountId).first<{ status: string }>();
    if (run?.status !== 'ACTIVE') throw new ActivityError(409, 'equipment_run_not_active');
    if (!this.source) throw new ActivityError(409, 'equipment_reward_source_required');
    const supplied = await this.source.prepare({ accountId, runId, nodeId, sourceRef });
    const parsed = preparedSource.safeParse({ sourceVersion: supplied?.sourceVersion, items: supplied?.items });
    if (!parsed.success || new Set(parsed.data.items.map(i => i.itemId)).size !== parsed.data.items.length
      || new Set(parsed.data.items.map(i => i.instanceId)).size !== parsed.data.items.length) {
      throw new ActivityError(409, 'equipment_reward_items_invalid');
    }
    if (!Array.isArray(supplied.assertions) || supplied.assertions.length === 0) throw new ActivityError(409, 'equipment_reward_source_required');
    const { sourceVersion, items } = parsed.data;
    const bundleId = walletId('equipment_bundle');
    const effects = captureRewardEffects(items, supplied.effects);
    const effectStatements: D1PreparedStatement[] = [];
    for (const effect of effects) {
      effectStatements.push(this.db.prepare(`INSERT INTO run_equipment_reward_effects(run_id,item_id,bundle_id,snapshot_json,payload_hash)
        VALUES(?,?,?,?,?)`).bind(runId, effect.itemId, bundleId, canonicalJson(effect.snapshot),
        await rewardEffectHash(runId, bundleId, effect.itemId, effect.instanceId, sourceVersion, effect.snapshot)));
    }
    const hash = await sha256Hex(canonicalJson({ sourceVersion, items }));
    try {
      await this.db.batch([
        ...supplied.assertions,
        this.db.prepare(`INSERT INTO equipment_reward_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')=i.capacity_version
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')=i.do05_rank
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.slots')=i.capacity_slots
              AND NOT EXISTS(SELECT 1 FROM run_equipment_reward_bundles b WHERE b.run_id=r.id AND b.node_id=?))
            THEN 1 ELSE 0 END`).bind(runId, accountId, nodeId),
        this.db.prepare('INSERT INTO run_equipment_reward_bundles(bundle_id,run_id,node_id,source_ref,source_version,payload_hash) VALUES(?,?,?,?,?,?)')
          .bind(bundleId, runId, nodeId, sourceRef, sourceVersion, hash),
        ...items.map((item, index) => this.db.prepare(`INSERT INTO run_equipment_reward_items
          (item_id,run_id,bundle_id,instance_id,sort_index,status,item_json) VALUES(?,?,?,?,?,'PENDING',?)`)
          .bind(item.itemId, runId, bundleId, item.instanceId, index, canonicalJson(item))),
        ...effectStatements,
        this.db.prepare('DELETE FROM equipment_reward_write_guard'),
      ]);
    } catch (error) {
      const raced = await this.original(runId, nodeId);
      if (raced) {
        if (raced.source_ref !== sourceRef || raced.payload_hash !== hash) throw new ActivityError(409, 'equipment_reward_source_conflict');
        return this.read(accountId, runId, raced.bundle_id);
      }
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'equipment_reward_state_conflict');
      throw error;
    }
    return this.read(accountId, runId, bundleId);
  }

  async claimItems(accountId: string, runId: string, raw: unknown): Promise<z.infer<typeof claimReceipt>> {
    const command = this.normalizeSelection(raw, 'equipment_claim_command_invalid');
    const hash = await sha256Hex(canonicalJson(command));
    await this.owner(accountId, runId);
    const replay = () => this.originalReceipt(runId, command.commandId, 'CLAIM_ITEMS', hash, claimReceipt);
    const original = await replay();
    if (original) return original;
    await this.requireActiveRun(accountId, runId);
    const bundle = await this.read(accountId, runId, command.bundleId);
    const selected = command.itemIds.map(itemId => bundle.items.find(item => item.itemId === itemId));
    if (selected.some(item => !item || item.status !== 'PENDING')) throw new ActivityError(409, 'equipment_reward_item_not_pending');
    const inventory = await new EquipmentInventoryStore(this.db).readInventory(accountId, runId);
    if (!inventory) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    if (inventory.revision !== command.expectedInventoryRevision) throw new ActivityError(409, 'equipment_revision_conflict');
    if (inventory.usedSlots + selected.length > inventory.capacitySlots) throw new ActivityError(409, 'equipment_capacity_exceeded');
    const assertions = await this.resolutionProof('CLAIM_ITEMS', accountId, runId, command.bundleId, command.itemIds);
    const receipt = claimReceipt.parse({ version: 'equipment.claim-receipt.v1', commandId: command.commandId,
      bundleId: command.bundleId, claimedItemIds: command.itemIds, inventoryRevision: inventory.revision + 1 });
    const effectStore = new EquipmentEffectOriginalStore(this.db, this.effectSource);
    const effectStatements: D1PreparedStatement[] = [];
    for (const item of selected) {
      const prepared = await effectStore.prepare(accountId, runId, { instanceId: item!.instanceId,
        sourceItemId: item!.itemId, expectedInventoryRevision: receipt.inventoryRevision });
      effectStatements.push(...prepared.statements);
    }
    const placeholders = command.itemIds.map(() => '?').join(',');
    try {
      await this.db.batch([
        ...assertions,
        this.db.prepare(`INSERT INTO equipment_reward_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE'
              AND i.revision=? AND i.capacity_slots >= ? +
                (SELECT count(*) FROM run_equipment_instances e WHERE e.run_id=r.id AND e.location_kind='STORED')
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')=i.capacity_version
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')=i.do05_rank
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.slots')=i.capacity_slots
              AND ?=(SELECT count(*) FROM run_equipment_reward_items ri
                WHERE ri.run_id=r.id AND ri.bundle_id=? AND ri.status='PENDING'
                  AND ri.item_id IN (${placeholders}))) THEN 1 ELSE 0 END`)
          .bind(runId, accountId, inventory.revision, selected.length, selected.length, command.bundleId, ...command.itemIds),
        ...selected.map(item => this.db.prepare(`UPDATE run_equipment_reward_items SET status='CLAIMED'
          WHERE run_id=? AND bundle_id=? AND item_id=? AND status='PENDING'`)
          .bind(runId, command.bundleId, item!.itemId)),
        ...selected.map(item => this.db.prepare(`INSERT INTO run_equipment_instances
          (instance_id,run_id,source_item_id,definition_id,definition_version,category_id,set_tag,star,locked,location_kind,revision)
          VALUES(?,?,?,?,?,?,?,?,0,'STORED',1)`)
          .bind(item!.instanceId, runId, item!.itemId, item!.definitionId, item!.definitionVersion,
            item!.categoryId, item!.setTag, item!.star)),
        this.db.prepare('UPDATE run_equipment_inventory SET revision=? WHERE run_id=? AND revision=?')
          .bind(receipt.inventoryRevision, runId, inventory.revision),
        ...effectStatements,
        this.db.prepare(`INSERT INTO run_equipment_receipts(run_id,command_id,command_kind,request_hash,response_json)
          VALUES(?,?,'CLAIM_ITEMS',?,?)`).bind(runId, command.commandId, hash, canonicalJson(receipt)),
        this.db.prepare('DELETE FROM equipment_reward_write_guard'),
      ]);
    } catch (error) {
      const completed = await replay();
      if (completed) return completed;
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'equipment_state_conflict');
      throw error;
    }
    return receipt;
  }

  async declineRemaining(accountId: string, runId: string, raw: unknown): Promise<z.infer<typeof declineReceipt>> {
    const command = this.normalizeSelection(raw, 'equipment_decline_command_invalid');
    const hash = await sha256Hex(canonicalJson(command));
    await this.owner(accountId, runId);
    const replay = () => this.originalReceipt(runId, command.commandId, 'DECLINE_REMAINING', hash, declineReceipt);
    const original = await replay();
    if (original) return original;
    await this.requireActiveRun(accountId, runId);
    const bundle = await this.read(accountId, runId, command.bundleId);
    const pending = bundle.items.filter(item => item.status === 'PENDING').map(item => item.itemId).sort();
    if (pending.length !== command.itemIds.length || pending.some((itemId, index) => itemId !== command.itemIds[index])) {
      throw new ActivityError(409, 'equipment_reward_remaining_changed');
    }
    const inventory = await new EquipmentInventoryStore(this.db).readInventory(accountId, runId);
    if (!inventory) throw new ActivityError(409, 'equipment_inventory_legacy_unsupported');
    if (inventory.revision !== command.expectedInventoryRevision) throw new ActivityError(409, 'equipment_revision_conflict');
    const assertions = await this.resolutionProof('DECLINE_REMAINING', accountId, runId, command.bundleId, command.itemIds);
    const receipt = declineReceipt.parse({ version: 'equipment.decline-receipt.v1', commandId: command.commandId,
      bundleId: command.bundleId, declinedItemIds: command.itemIds, inventoryRevision: inventory.revision + 1 });
    const placeholders = command.itemIds.map(() => '?').join(',');
    try {
      await this.db.batch([
        ...assertions,
        this.db.prepare(`INSERT INTO equipment_reward_write_guard(valid)
          SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id
            JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
            WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND i.revision=?
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')=i.capacity_version
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')=i.do05_rank
              AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.slots')=i.capacity_slots
              AND ?=(SELECT count(*) FROM run_equipment_reward_items ri
                WHERE ri.run_id=r.id AND ri.bundle_id=? AND ri.status='PENDING')
              AND ?=(SELECT count(*) FROM run_equipment_reward_items ri
                WHERE ri.run_id=r.id AND ri.bundle_id=? AND ri.status='PENDING'
                  AND ri.item_id IN (${placeholders}))) THEN 1 ELSE 0 END`)
          .bind(runId, accountId, inventory.revision, command.itemIds.length, command.bundleId,
            command.itemIds.length, command.bundleId, ...command.itemIds),
        ...command.itemIds.map(itemId => this.db.prepare(`UPDATE run_equipment_reward_items SET status='FORFEITED'
          WHERE run_id=? AND bundle_id=? AND item_id=? AND status='PENDING'`)
          .bind(runId, command.bundleId, itemId)),
        this.db.prepare('UPDATE run_equipment_inventory SET revision=? WHERE run_id=? AND revision=?')
          .bind(receipt.inventoryRevision, runId, inventory.revision),
        this.db.prepare(`INSERT INTO run_equipment_receipts(run_id,command_id,command_kind,request_hash,response_json)
          VALUES(?,?,'DECLINE_REMAINING',?,?)`).bind(runId, command.commandId, hash, canonicalJson(receipt)),
        this.db.prepare('DELETE FROM equipment_reward_write_guard'),
      ]);
    } catch (error) {
      const completed = await replay();
      if (completed) return completed;
      if (/valid=1|UNIQUE constraint failed/iu.test(String(error))) throw new ActivityError(409, 'equipment_state_conflict');
      throw error;
    }
    return receipt;
  }
}

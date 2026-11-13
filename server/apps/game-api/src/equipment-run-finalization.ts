import { sha256Hex, canonicalJson } from '@lastrail/common';
import { z } from 'zod';
import { ActivityError } from './activity-policy';
import { EquipmentInventoryStore } from './equipment-inventory-start';

const finalInput = z.strictObject({
  outcome: z.enum(['VICTORY', 'FAILURE', 'ABANDONED']),
  payloadJson: z.string().min(2),
  submittedAt: z.number().int().min(0).max(Number.MAX_SAFE_INTEGER),
});
const finalReceipt = z.strictObject({
  version: z.literal('equipment.run-finalization.v1'),
  runId: z.string().min(1), outcome: finalInput.shape.outcome,
  finalPayloadHash: z.string().regex(/^[a-f0-9]{64}$/u),
  closedItemIds: z.array(z.string().min(1)),
  inventoryRevision: z.number().int().min(2),
});

type Receipt = z.infer<typeof finalReceipt>;
type FinalRow = { account_id: string; outcome: string; final_payload_hash: string;
  inventory_revision: number; receipt_json: string };

/** Read the immutable equipment cleanup result; this never recreates terminal gear. */
export async function readEquipmentRunFinalization(db: D1Database, accountId: string, runId: string): Promise<Receipt | null> {
  const pinned = await new EquipmentInventoryStore(db).readPinnedCapacity(accountId, runId);
  if (!pinned) return null;
  const row = await db.prepare('SELECT account_id,outcome,final_payload_hash,inventory_revision,receipt_json FROM run_equipment_finalizations WHERE run_id=?')
    .bind(runId).first<FinalRow>();
  if (!row) return null;
  try {
    const receipt = finalReceipt.parse(JSON.parse(row.receipt_json));
    if (row.account_id !== accountId || receipt.runId !== runId || receipt.outcome !== row.outcome
      || receipt.finalPayloadHash !== row.final_payload_hash || receipt.inventoryRevision !== row.inventory_revision) throw new Error('mismatch');
    return receipt;
  } catch { throw new ActivityError(503, 'equipment_finalization_storage_invalid'); }
}

/** Append these statements AFTER the trusted run_final_results insert and terminal runs update
 * in that SAME batch. No production final-result producer calls this yet. A separate post-finish
 * call cannot prove atomic settlement and must not use this builder as a substitute. */
export async function prepareEquipmentRunFinalization(db: D1Database, accountId: string, runId: string,
  raw: unknown): Promise<{ receipt: Receipt; statements: D1PreparedStatement[] } | null> {
  const parsed = finalInput.safeParse(raw);
  if (!parsed.success) throw new ActivityError(400, 'equipment_finalization_input_invalid');
  try { JSON.parse(parsed.data.payloadJson); }
  catch { throw new ActivityError(400, 'equipment_finalization_input_invalid'); }
  const pinned = await new EquipmentInventoryStore(db).readPinnedCapacity(accountId, runId);
  if (!pinned) return null; // Preserve pre-v18 START semantics.
  if (pinned.revision >= Number.MAX_SAFE_INTEGER) throw new ActivityError(409, 'equipment_revision_exhausted');
  const run = await db.prepare('SELECT status FROM runs WHERE id=? AND account_id=?')
    .bind(runId, accountId).first<{ status: string }>();
  if (run?.status !== 'ACTIVE' && run?.status !== 'FINISH_PENDING') throw new ActivityError(409, 'equipment_run_not_finalizable');
  if (await readEquipmentRunFinalization(db, accountId, runId)) throw new ActivityError(409, 'equipment_finalization_exists');
  const rows = (await db.prepare("SELECT item_id FROM run_equipment_reward_items WHERE run_id=? AND status='PENDING' ORDER BY item_id")
    .bind(runId).all<{ item_id: string }>()).results;
  const closedItemIds = rows.map(row => row.item_id);
  const hash = await sha256Hex(parsed.data.payloadJson);
  const receipt = finalReceipt.parse({ version: 'equipment.run-finalization.v1', runId,
    outcome: parsed.data.outcome, finalPayloadHash: hash,
    closedItemIds, inventoryRevision: pinned.revision + 1 });
  const terminalStatus = parsed.data.outcome === 'VICTORY' ? 'COMPLETED'
    : parsed.data.outcome === 'FAILURE' ? 'FAILED' : 'ABANDONED';
  const itemIdsJson = canonicalJson(closedItemIds);
  return { receipt, statements: [
    db.prepare(`INSERT INTO equipment_finalization_write_guard(valid)
      SELECT CASE WHEN EXISTS(SELECT 1 FROM runs r JOIN run_final_results f ON f.run_id=r.id
        JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id
        WHERE r.id=? AND r.account_id=? AND r.status=? AND f.outcome=?
          AND f.payload_json=? AND f.submitted_at=? AND i.revision=?
          AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.version')=i.capacity_version
          AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.do05Rank')=i.do05_rank
          AND json_extract(s.snapshot_json,'$.equipmentInventoryCapacity.slots')=i.capacity_slots
          AND NOT EXISTS(SELECT 1 FROM run_equipment_finalizations x WHERE x.run_id=r.id)
          AND ?=(SELECT count(*) FROM run_equipment_reward_items ri WHERE ri.run_id=r.id AND ri.status='PENDING')
          AND ?=(SELECT count(*) FROM run_equipment_reward_items ri WHERE ri.run_id=r.id
            AND ri.status='PENDING' AND ri.item_id IN (SELECT value FROM json_each(?)))) THEN 1 ELSE 0 END`)
      .bind(runId, accountId, terminalStatus, parsed.data.outcome, parsed.data.payloadJson,
        parsed.data.submittedAt, pinned.revision, closedItemIds.length, closedItemIds.length, itemIdsJson),
    db.prepare("UPDATE run_equipment_reward_items SET status='RUN_CLOSED' WHERE run_id=? AND status='PENDING'").bind(runId),
    db.prepare('UPDATE run_equipment_inventory SET revision=? WHERE run_id=? AND revision=?')
      .bind(receipt.inventoryRevision, runId, pinned.revision),
    db.prepare(`INSERT INTO run_equipment_finalizations
      (run_id,account_id,outcome,final_payload_hash,inventory_revision,receipt_json) VALUES(?,?,?,?,?,?)`)
      .bind(runId, accountId, parsed.data.outcome, hash, receipt.inventoryRevision, canonicalJson(receipt)),
    db.prepare('DELETE FROM equipment_finalization_write_guard'),
  ] };
}

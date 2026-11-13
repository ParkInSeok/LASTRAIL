import { canonicalJson } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { calculateEquipmentCoreStats } from './equipment-run-stat-owner';
import type { CombatHealthVerifier } from './combat-health-store';
import type { readOrdinaryRunStatOrigin } from './combat-ordinary-run-source';
import type { readOrdinaryFiveEquipmentSource } from './combat-ordinary-equipment-source';
import type { readOrdinaryTrainFromOriginal } from './combat-ordinary-train-source';

export const ordinaryInitialHealthVersion = 'combat.ordinary-initial-health.v1.20261023';

/** Existing fresh-actor initialization: BasicCombatSimulation.Actor constructs
 * CombatVitals(maxHp, ...), whose initial HP and anchor equal maxHp. This is
 * neither checkpoint restoration nor a heal. Values come from the pinned owned
 * profile and actual run equipment, not the engine's local fixture numbers.
 * Inputs are server-owned reads; this factory must never accept request JSON. */
export function prepareOrdinaryInitialHealthSource(db: D1Database,
  original: Awaited<ReturnType<typeof readOrdinaryRunStatOrigin>>,
  equipment: Awaited<ReturnType<typeof readOrdinaryFiveEquipmentSource>>,
  trainSource: Awaited<ReturnType<typeof readOrdinaryTrainFromOriginal>> = null) {
  const { accountId, runId } = original.origin;
  if (equipment.accountId !== accountId || equipment.runId !== runId) throw new ActivityError(409, 'ordinary_initial_health_scope_conflict');
  const targets: { id: string; hp: number; maxHp: number }[] = original.origin.actors.map(actor => {
    const slots = equipment.actors.find(a => a.operatorId === actor.operatorId)?.slots;
    if (!slots || slots.some(i => i && (i.uniqueEffects.length !== 0 || i.enchantment !== null)))
      throw new ActivityError(409, 'ordinary_initial_health_effect_unsupported');
    const base = actor.permanentStats;
    const { maxHp } = calculateEquipmentCoreStats({ maxHp: base.maxHp, attack: base.attack,
      defense: { numerator: String(base.defense), denominator: '1' } }, slots.filter(i => i !== null)
      .map(i => ({ categoryId: i.categoryId, star: i.star, setTag: i.setTag })));
    return { id: actor.operatorId, hp: maxHp, maxHp };
  }).sort((a, b) => a.id < b.id ? -1 : a.id > b.id ? 1 : 0);
  let trainInitialState;
  if (trainSource?.packet.version === 'combat.ordinary-train-source.v2.20261027') {
    const { packet, sourceHash } = trainSource, trainId = packet.train.trainId;
    if (packet.accountId !== accountId || packet.runId !== runId || packet.ordinaryOriginHash !== original.originHash
      || targets.some(t => t.id === trainId)) throw new ActivityError(409, 'ordinary_initial_train_scope_conflict');
    targets.push({ id: trainId, hp: packet.permanentStats.maxHp, maxHp: packet.permanentStats.maxHp });
    targets.sort((a, b) => a.id < b.id ? -1 : a.id > b.id ? 1 : 0);
    // A receipt points at the same HP owner; it is never a second current-HP store.
    trainInitialState = { version: 'combat.ordinary-train-initial-state.v1.20261027', accountId, runId, trainId,
      ordinaryOriginHash: original.originHash, sourceHash, healthTargetId: trainId, healthRevision: 1,
      stage: 'INITIAL_BEFORE_COMBAT' as const, logicalTimeMs: 0, activeShields: [] as never[] };
  }
  const expected = canonicalJson(targets);
  const verifier: CombatHealthVerifier = { prepare: async context => {
    if (context.kind !== 'INITIALIZE' || context.accountId !== accountId || context.runId !== runId
      || context.startSnapshotJson !== original.startSnapshotJson
      || canonicalJson(context.initialTargets ?? []) !== expected)
      throw new ActivityError(409, 'ordinary_initial_health_source_conflict');
    return { assertions: [...equipment.assertions, db.prepare(`INSERT INTO combat_damage_write_guard(valid)
      SELECT CASE WHEN EXISTS(SELECT 1 FROM ordinary_combat_phases p JOIN runs r ON r.id=p.run_id
      JOIN run_snapshots s ON s.run_id=r.id JOIN accounts a ON a.id=r.account_id
      WHERE p.run_id=? AND p.account_id=? AND p.phase='INITIALIZING' AND p.revision=2 AND p.origin_hash=?
      AND p.start_snapshot_json=? AND s.snapshot_json=p.start_snapshot_json AND r.status='ACTIVE' AND a.status='ACTIVE')
      THEN 1 ELSE 0 END`).bind(runId, accountId, original.originHash, original.startSnapshotJson)] };
  } };
  return { version: ordinaryInitialHealthVersion, targets: structuredClone(targets), verifier, trainInitialState };
}

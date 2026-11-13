import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { prepareOwnedCombatProfile } from './combat-owned-profile';
import { readCommanderDeparture, commanderDepartureGuards } from './commander-departure';
import { captureCombatPolicyRequirements, readPinnedCombatPolicyRequirements, nyxPolicyVersion, nyxFormationOrder } from './combat-policy-catalog';
import { capturePredepartureEffectApplicability } from './combat-owned-train-profile';
import { calculateEquipmentCoreStats, equipmentCoreStatVersion } from './equipment-run-stat-owner';
import { ExactCombatValue, exactCombatValueSchema } from './combat-exact-value';
import { captureOrdinaryTrainSourceV2 } from './combat-ordinary-train-source';
import { captureOrdinaryRailSourceV4 } from './combat-ordinary-rail-source';
import { captureOrdinaryRoofSource } from './combat-ordinary-roof-source';

export const ordinaryFiveStatVersion = 'combat.ordinary-five-core-stats.v1.20261023';
export const ordinaryNyxStatVersion = 'combat.ordinary-five-core-stats.v2.20261107';
const id = z.string().regex(/^[\x21-\x7e]{1,128}$/u);
const revision = z.number().int().positive().safe();
const roster = z.enum(['op_kyle', 'op_reina', 'op_eve', 'op_sera', 'op_mira', 'op_nyx']);
const json = (value: unknown) => canonicalJson(z.json().parse(value));
const digest = (value: unknown) => sha256Hex(json(value));
const fail = (reason: string): never => { throw new ActivityError(409, `ordinary_stat_${reason}`); };
const originSchema = z.strictObject({
  version: z.enum([ordinaryFiveStatVersion, ordinaryNyxStatVersion]), accountId: id, runId: id,
  policy: z.json(), ownedSourceHash: z.string().regex(/^sha256:[a-f0-9]{64}$/u),
  definitionVersion: id, definitionHash: z.string().regex(/^sha256:[a-f0-9]{64}$/u),
  formationRevision: revision, loadoutRevision: revision, applicability: z.json(),
  actors: z.array(z.strictObject({ operatorId: roster, slot: z.number().int().min(0).max(4),
    permanentStats: z.strictObject({ maxHp: revision, attack: exactCombatValueSchema,
      defense: z.number().int().nonnegative().safe(), basicIntervalMs: revision }),
    skillMagnitude: exactCombatValueSchema })).length(5),
}).refine(o => new Set(o.actors.map(a => a.operatorId)).size === 5 && new Set(o.actors.map(a => a.slot)).size === 5
  && (o.version === ordinaryFiveStatVersion ? o.actors.every(a=>a.operatorId!=='op_nyx')
    : [...o.actors].sort((a,b)=>a.slot-b.slot).every((a,i)=>a.operatorId===nyxFormationOrder[i])));

export async function readOrdinaryFiveStatOrigin(input: unknown, expectedHash: string) {
  const origin = originSchema.parse(input);
  if (!/^[a-f0-9]{64}$/u.test(expectedHash) || await digest(origin) !== expectedHash) return fail('origin_hash');
  const version = z.object({ version: z.string() }).parse(origin.policy).version;
  if(version !== (origin.version===ordinaryNyxStatVersion ? nyxPolicyVersion : 'combat.policy-catalog.2026-09-29.1'))return fail('policy_conflict');
  if (json(readPinnedCombatPolicyRequirements(version)) !== json(origin.policy)) return fail('policy_conflict');
  return structuredClone(origin);
}

/** Internal START preparation, using actual owned sources and their existing CAS.
 * The caller must freeze origin/hash and execute assertions in its START batch.
 * This does not authorize START or initialize current HP/equipment/augment state. */
export async function prepareOrdinaryFiveStatOrigin(db: D1Database, accountId: string, runId: string, selection: unknown) {
  id.parse(accountId); id.parse(runId);
  if (await db.prepare('SELECT 1 FROM runs WHERE id=?').bind(runId).first()) return fail('existing_run_requires_original');
  const owned = await prepareOwnedCombatProfile(db, accountId, selection);
  const commander = await readCommanderDeparture(db, accountId);
  if (commander.snapshot && Object.keys(commander.snapshot.ranks).length) return fail('commander_handler_unavailable');
  const p = owned.profile, policy = p.policyVersion === nyxPolicyVersion
    ? readPinnedCombatPolicyRequirements(nyxPolicyVersion)! : captureCombatPolicyRequirements();
  if (p.policyVersion !== policy.version) return fail('policy_conflict');
  if (p.operators.some(a => !a.permanentStats)) return fail('growth_source_unavailable');
  const origin = originSchema.parse({ version: policy.version===nyxPolicyVersion ? ordinaryNyxStatVersion : ordinaryFiveStatVersion, accountId, runId, policy,
    ownedSourceHash: p.sourceHash, definitionVersion: p.definitionVersion, definitionHash: p.definitionHash,
    formationRevision: p.formationRevision, loadoutRevision: p.loadoutRevision,
    applicability: capturePredepartureEffectApplicability(),
    actors: p.operators.map(a => ({ operatorId: a.operatorId, slot: a.slot,
      permanentStats: a.permanentStats, skillMagnitude: a.skillMagnitude })) });
  const originHash = await digest(origin);
  const trainSource = await captureOrdinaryTrainSourceV2({ accountId, runId, ordinaryOriginHash: originHash,
    ownedSourceHash: p.sourceHash, loadoutRevision: p.loadoutRevision,
    train: { trainId: p.selectedTrain.trainId, level: p.selectedTrain.level, revision: p.selectedTrain.revision } });
  const railSource = await captureOrdinaryRailSourceV4({ accountId, runId, trainId: p.selectedTrain.trainId,
    ordinaryOriginHash: originHash, ownedSourceHash: p.sourceHash, loadoutRevision: p.loadoutRevision,
    selectedCarriages: p.selectedCarriages.map(({ carriageId, slot, level, revision }) => ({ carriageId, slot, level, revision })) }, trainSource);
  const roofSource = await captureOrdinaryRoofSource(origin, originHash, railSource);
  return { origin, originHash, trainSource, railSource, roofSource, assertions: [...owned.assertions,
    ...commanderDepartureGuards(db, accountId, commander),
    db.prepare('INSERT INTO departure_write_guard(valid) SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM runs WHERE id=?) THEN 1 ELSE 0 END').bind(runId)] };
}

const noModifiers = z.strictObject({ sourceId: id, version: id, revision,
  status: z.literal('NO_ACTIVE_STAT_MODIFIERS') });
const item = z.strictObject({ instanceId: id, instanceRevision: revision, effectRevision: revision,
  definitionVersion: z.literal(equipmentCoreStatVersion), categoryId: z.enum(['HP', 'ATK', 'DEF']),
  star: z.number().int().min(1).max(5), setTag: z.enum(['SET01', 'SET04']),
  uniqueEffects: z.array(z.never()).length(0), enchantment: z.null() });
export const stateSchema = z.strictObject({ accountId: id, runId: id, revision, inventoryRevision: revision,
  modifiers: z.strictObject({ research: noModifiers, commander: noModifiers, augments: noModifiers, temporary: noModifiers }),
  actors: z.array(z.strictObject({ operatorId: roster, currentHp: z.number().int().nonnegative().safe(),
    slots: z.tuple([item.nullable(), item.nullable(), item.nullable()]) })).length(5),
}).refine(s => new Set(s.actors.map(a => a.operatorId)).size === 5);
export type SourceState = z.infer<typeof stateSchema>;
type StatResult = Awaited<ReturnType<OrdinaryFiveStatOwner['calculate']>>;

/** Run-scoped core stat resolver. Input must come from the combat/equipment owners,
 * never an HTTP body. Explicit empty proofs are required; missing is not empty.
 * Only supported core-stat inputs are accepted. This owns neither HP nor shields,
 * action timing, outgoing damage, skill handlers, inventory, or START permission. */
export class OrdinaryFiveStatOwner {
  private committed: { state: SourceState; inputHash: string; result: StatResult } | null = null;
  private constructor(private readonly origin: z.infer<typeof originSchema>, private readonly originHash: string) {}
  static async fromOriginal(input: unknown, expectedHash: string) {
    return new OrdinaryFiveStatOwner(await readOrdinaryFiveStatOrigin(input, expectedHash), expectedHash);
  }
  async resolve(input: unknown) {
    // Serialize no async owner updates: publish only after rechecking the captured predecessor.
    const before = this.committed, state = stateSchema.parse(input);
    if (state.accountId !== this.origin.accountId || state.runId !== this.origin.runId) return fail('scope_conflict');
    const inputHash = await digest(state);
    if (this.committed !== before) return fail('concurrent_update');
    if (before) {
      if (state.revision < before.state.revision || state.inventoryRevision < before.state.inventoryRevision) return fail('revision_stale');
      if (state.revision === before.state.revision) {
        if (inputHash !== before.inputHash) return fail('revision_conflict');
        return structuredClone(before.result);
      }
      const placement = (s: SourceState) => s.actors.map(a => ({ operatorId: a.operatorId,
        slots: a.slots.map(i => { if (!i) return null; const { effectRevision, ...rest } = i; void effectRevision; return rest; }) })).sort((a, b) => a.operatorId.localeCompare(b.operatorId));
      if (state.inventoryRevision === before.state.inventoryRevision && json(placement(state)) !== json(placement(before.state))) return fail('inventory_revision_conflict');
      const priorItems = new Map(before.state.actors.flatMap(a => a.slots.filter(i => i !== null).map(i => [i.instanceId, i] as const)));
      for (const i of state.actors.flatMap(a => a.slots.filter(i => i !== null))) {
        const old = priorItems.get(i.instanceId);
        if (!old) continue;
        if (i.instanceRevision < old.instanceRevision || i.effectRevision < old.effectRevision) return fail('item_revision_stale');
        const identity = (v: typeof i) => ({ definitionVersion: v.definitionVersion, categoryId: v.categoryId, star: v.star, setTag: v.setTag });
        if (i.instanceRevision === old.instanceRevision && json(identity(i)) !== json(identity(old))) return fail('item_revision_conflict');
      }
      for (const key of ['research', 'commander', 'augments', 'temporary'] as const) {
        const old = before.state.modifiers[key], next = state.modifiers[key];
        if (old.sourceId === next.sourceId && old.version === next.version && next.revision < old.revision) return fail('source_revision_stale');
      }
    }
    const result = await this.calculate(state);
    if (this.committed !== before) return fail('concurrent_update');
    this.committed = { state, inputHash, result };
    return structuredClone(result);
  }
  private async calculate(state: SourceState) {
    const seen = new Set<string>();
    const actors = this.origin.actors.map(original => {
      const current = state.actors.find(a => a.operatorId === original.operatorId)!;
      const items = current.slots.filter((i): i is NonNullable<typeof i> => i !== null);
      for (const i of items) { if (seen.has(i.instanceId)) return fail('duplicate_instance'); seen.add(i.instanceId); }
      const b = original.permanentStats;
      const equipment = calculateEquipmentCoreStats({ maxHp: b.maxHp, attack: b.attack,
        defense: { numerator: String(b.defense), denominator: '1' } }, items.map(i => ({ categoryId: i.categoryId, star: i.star, setTag: i.setTag })));
      if (current.currentHp > equipment.maxHp) return fail('health_transition_required');
      // This current version explicitly pins Kyle's approved flat DEF after multipliers.
      const policy = this.origin.policy as ReturnType<typeof captureCombatPolicyRequirements>;
      const passive = policy.passives.find(p => p.operatorId === original.operatorId);
      const flat = passive && 'valueFlat' in passive && BigInt(current.currentHp) * 2n < BigInt(equipment.maxHp) ? passive.valueFlat : 0;
      const defense = ExactCombatValue.from(equipment.defense).add(ExactCombatValue.from({ numerator: String(flat), denominator: '1' })).snapshot();
      return { operatorId: original.operatorId, slot: original.slot, currentHp: current.currentHp,
        equipment: structuredClone(current.slots), effectiveStats: { maxHp: equipment.maxHp, attack: equipment.attack, defense },
        conditionalDefenseFlat: flat, skillMagnitude: structuredClone(original.skillMagnitude) };
    });
    const body = { version: ordinaryFiveStatVersion, accountId: state.accountId, runId: state.runId,
      revision: state.revision, inventoryRevision: state.inventoryRevision, originHash: this.originHash,
      sourceHash: await digest(state), scope: 'MAX_HP_ATK_DEF_ONLY' as const, actors };
    return { ...body, packetHash: await digest(body) };
  }
}

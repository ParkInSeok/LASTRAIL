import { z } from 'zod';
import { ExactCombatValue, exactCombatValueSchema } from './combat-exact-value';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { CombatUltimateState } from './combat-ultimate-state';

export function reinaBarrageDefinition() {
  return { id: 'op_reina.ultimate', definitionId: 'combat.reina-barrage.v1.20261102',
    effect: 'REINA_BARRAGE', cost: 100, windupMs: 800, recoveryMs: 300,
    impactOffsetsMs: [1400, 2200, 3000, 3800], primaryDamageBps: 25000, additionalDamageBps: 7500,
    maximumAdditionalTargets: 3, radiusMm: 3000, castRangeMm: 24000, adjacentCars: 1,
    consumption: 'COMMIT', attackSnapshot: 'COMMIT', targetSet: 'COMMIT',
    center: 'ORIGINAL_FRAME_LOCAL', impactEligibility: 'IDENTITY_GENERATION_ALIVE_RADIUS_CENTER_OCCLUSION',
    afterCasterDown: 'KEEP_COMMITTED', afterFrameLost: 'CANCEL_REMAINING_NO_REFUND',
    reentry: 'ORIGINAL_NODE_START_NO_MID_BARRAGE_RESTORE' };
}

const nat = z.number().int().nonnegative().safe(), positive = nat.min(1), id = z.string().min(1).max(200);
const vector = z.tuple([z.number().int().safe(), z.number().int().safe(), z.number().int().safe()]);
const identity = z.strictObject({ id, generation: positive });
const observed = identity.extend({ alive: z.boolean(), offsetMm: vector, centerClear: z.boolean() });
const candidate = observed.extend({ kind: z.enum(['ENEMY', 'ARTILLERY', 'STRUCTURE']), order: nat,
  muzzleDeltaMm: vector, castScopeEligible: z.boolean(), muzzleClear: z.boolean() });
const operation = z.discriminatedUnion('kind', [
  z.strictObject({ kind: z.literal('COMMIT'), tick: nat, casterAlive: z.boolean(), actionEligible: z.boolean(), charge: nat,
    attack: exactCombatValueSchema, frame: identity.extend({ centerMm: vector }), candidates: z.array(candidate).max(1000) }),
  z.strictObject({ kind: z.literal('IMPACT'), tick: nat, index: nat.max(3), frame: identity.extend({ exists: z.boolean() }),
    targets: z.array(observed).max(4) }),
  z.strictObject({ kind: z.literal('CANCEL'), tick: nat,
    reason: z.enum(['CASTER_DOWN', 'TARGET_INVALID', 'FRAME_LOST', 'INSTANCE_DISPOSED', 'FORCED_TERMINAL']) }),
]);
const replaySchema = z.strictObject({ version: z.literal('combat.reina-barrage-replay.v1.20261102'), castId: id,
  nodeId: id, attemptGeneration: positive, acceptedTick: nat, stepTimeSpanTicks: positive, primary: identity,
  chargeAtAcceptance: nat, casterAliveAtAcceptance: z.boolean(), actionEligibleAtAcceptance: z.boolean(),
  operations: z.array(operation).max(10000) });
const square = (v: [number, number, number]) => v.reduce((s, x) => s + BigInt(x) * BigInt(x), 0n);
const fail = (): never => { throw new Error('reina_barrage_replay_conflict'); };

/** Domain replay, not client proof acceptance. The execution owner must independently
 * supply eligibility/LOS and frame-local observations and fence their original sources.
 * Returned damage is an intent; existing impact-time defense/shields/HP owners apply it. */
export function replayReinaBarrage(input: unknown) {
  const r = replaySchema.parse(input), d = reinaBarrageDefinition();
  if (!r.casterAliveAtAcceptance || !r.actionEligibleAtAcceptance || r.chargeAtAcceptance < d.cost) return fail();
  const plus = (tick: number, ms: number) => {
    const step = BigInt(r.stepTimeSpanTicks), value = BigInt(tick) + (BigInt(ms) * 10000n + step - 1n) / step;
    if (value > BigInt(Number.MAX_SAFE_INTEGER)) return fail();
    return Number(value);
  };
  const commitDue = plus(r.acceptedTick, d.windupMs);
  let phase: 'WINDUP' | 'COMMITTED' | 'CANCELLED' | 'RESOLVED' = 'WINDUP';
  let lastTick = r.acceptedTick, consumedCharge = 0, recoveryUntilTick: number | null = null;
  let attack: ExactCombatValue | undefined, frame: z.infer<typeof identity> & { centerMm: [number, number, number] } | undefined;
  let selected: z.infer<typeof identity>[] = [], impactTicks: number[] = [];
  const dead = new Set<string>();
  const pulses: { index: number; tick: number; hits: { targetId: string; targetGeneration: number; damageBps: number;
    rawDamage: ReturnType<ExactCombatValue['snapshot']>; identity: { nodeId: string; attemptGeneration: number;
      castId: string; index: number; targetId: string; targetGeneration: number } }[] }[] = [];
  for (const op of r.operations) {
    if (op.tick < lastTick || phase === 'CANCELLED' || phase === 'RESOLVED') return fail();
    if (phase === 'COMMITTED' && op.tick > impactTicks[pulses.length]!) return fail();
    lastTick = op.tick;
    if (op.kind === 'CANCEL') {
      if (phase === 'WINDUP') { if (op.tick > commitDue) return fail(); phase = 'CANCELLED'; }
      else if (op.reason === 'FRAME_LOST' || op.reason === 'INSTANCE_DISPOSED' || op.reason === 'FORCED_TERMINAL') phase = 'CANCELLED';
      continue;
    }
    if (op.kind === 'COMMIT') {
      if (phase !== 'WINDUP' || op.tick !== commitDue || new Set(op.candidates.map(t => t.id)).size !== op.candidates.length) return fail();
      const eligible = (t: z.infer<typeof candidate>) => t.alive && t.kind === 'ENEMY' && t.castScopeEligible
        && t.muzzleClear && t.centerClear && square(t.offsetMm) <= BigInt(d.radiusMm) ** 2n
        && square(t.muzzleDeltaMm) <= BigInt(d.castRangeMm) ** 2n;
      const primary = op.candidates.find(t => t.id === r.primary.id && t.generation === r.primary.generation);
      if (!op.casterAlive || !op.actionEligible || op.charge < d.cost || !primary || !eligible(primary)) { phase = 'CANCELLED'; continue; }
      if (square(primary.offsetMm) !== 0n) return fail();
      const additional = op.candidates.filter(t => t.id !== primary.id && eligible(t)).sort((a, b) => {
        const distance = square(a.offsetMm) - square(b.offsetMm);
        return distance < 0n ? -1 : distance > 0n ? 1 : a.order - b.order || (a.id < b.id ? -1 : a.id > b.id ? 1 : 0);
      }).slice(0, d.maximumAdditionalTargets);
      selected = [primary, ...additional].map(t => ({ id: t.id, generation: t.generation }));
      attack = ExactCombatValue.from(op.attack); frame = op.frame;
      impactTicks = d.impactOffsetsMs.map(ms => plus(op.tick, ms));
      recoveryUntilTick = plus(op.tick, d.recoveryMs); consumedCharge = d.cost; phase = 'COMMITTED';
      continue;
    }
    if (phase !== 'COMMITTED' || op.index !== pulses.length || op.tick !== impactTicks[op.index]) return fail();
    if (op.frame.id !== frame!.id || op.frame.generation !== frame!.generation) return fail();
    if (!op.frame.exists) { phase = 'CANCELLED'; continue; }
    if (new Set(op.targets.map(t => t.id)).size !== op.targets.length || op.targets.some(t => !selected.some(s => s.id === t.id))) return fail();
    const hits: (typeof pulses)[number]['hits'] = [];
    for (const [index, target] of selected.entries()) {
      const now = op.targets.find(t => t.id === target.id);
      if (!now || now.generation !== target.generation) continue;
      if (!now.alive) { dead.add(target.id); continue; }
      if (dead.has(target.id)) return fail();
      if (!now.centerClear || square(now.offsetMm) > BigInt(d.radiusMm) ** 2n) continue;
      const damageBps = index === 0 ? d.primaryDamageBps : d.additionalDamageBps;
      hits.push({ targetId: target.id, targetGeneration: target.generation, damageBps,
        rawDamage: attack!.multiply(ExactCombatValue.from({ numerator: String(damageBps), denominator: '10000' })).snapshot(),
        identity: { nodeId: r.nodeId, attemptGeneration: r.attemptGeneration, castId: r.castId,
          index: op.index, targetId: target.id, targetGeneration: target.generation } });
    }
    pulses.push({ index: op.index, tick: op.tick, hits });
    if (pulses.length === d.impactOffsetsMs.length) phase = 'RESOLVED';
  }
  return { definitionId: d.definitionId, nodeId: r.nodeId, attemptGeneration: r.attemptGeneration, castId: r.castId,
    clockTick: lastTick, stepTimeSpanTicks: r.stepTimeSpanTicks,
    phase, consumedCharge, recoveryUntilTick, frame: frame ?? null, selected, impactTicks, pulses,
    unresolvedImpacts: phase === 'COMMITTED' ? impactTicks.length - pulses.length : phase === 'WINDUP' ? d.impactOffsetsMs.length : 0 };
}

export async function reinaBarrageChargeCommandId(scope: { accountId: string; runId: string }, replay: ReturnType<typeof replayReinaBarrage>) {
  const context = z.strictObject({ accountId: id, runId: id }).parse(scope);
  const cast = z.object({ nodeId: id, attemptGeneration: positive, castId: id }).parse(replay);
  return 'reina-barrage:' + await sha256Hex(canonicalJson({ version: 'reina.barrage-charge.v1.20261102', ...context, ...cast }));
}

/** Reuse the original charge owner on a private candidate. These independently
 * owned before/after records need the execution source's transaction assertions. */
export async function verifyReinaBarrageChargeTransition(scope: { accountId: string; runId: string },
  replay: ReturnType<typeof replayReinaBarrage>, chargeAtCommit: number | undefined, input: unknown) {
  if (replay.consumedCharge === 0) { if (input !== undefined) return fail(); return null; }
  const transition = z.strictObject({ before: z.unknown(), after: z.unknown() }).parse(input);
  const owner = CombatUltimateState.restore(transition.before), before = owner.snapshot();
  const commandId = await reinaBarrageChargeCommandId(scope, replay);
  if (replay.consumedCharge !== reinaBarrageDefinition().cost || before.actorId !== 'op_reina'
    || before.charge !== chargeAtCommit || before.casts.some(c => c.commandId === commandId)) return fail();
  const receipt = owner.cast(commandId, true);
  if (!receipt.accepted || receipt.before - receipt.after !== replay.consumedCharge
    || canonicalJson(owner.snapshot()) !== canonicalJson(CombatUltimateState.restore(transition.after).snapshot())) return fail();
  return receipt;
}

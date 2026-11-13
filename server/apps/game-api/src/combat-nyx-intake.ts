import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ExactCombatValue, exactCombatValueSchema } from './combat-exact-value';
import { captureNyxApprovedSource, captureNyxApprovedSourceV2, captureNyxApprovedSourceV3 } from './combat-nyx-approved-source';

const id = z.string().min(1).max(200), nat = z.number().int().nonnegative().safe();
const scopeSchema = z.strictObject({ accountId: id, runId: id, nodeId: id, attemptGeneration: nat.min(1), snapshotHash: z.string().regex(/^[a-f0-9]{64}$/u) });
const formationSchema = z.array(z.strictObject({ operatorId: id, slot: nat.max(4) })).length(5);
const expectedOrder = ['op_reina', 'op_kyle', 'op_eve', 'op_sera', 'op_nyx'];
const base = { zoneId: id, atMs: nat };
const eventsSchema = z.array(z.discriminatedUnion('kind', [
  z.strictObject({ ...base, kind: z.literal('CREATE'), ability: z.enum(['ACTIVE', 'ULTIMATE']), attack: exactCombatValueSchema, skillMagnitude: exactCombatValueSchema }),
  z.strictObject({ ...base, kind: z.literal('TICK'), index: nat.min(1), eligibleTargetIds: z.array(id).max(1000), focusTargetId: id.nullable(),
    targets: z.array(z.strictObject({ targetId: id, attackBudget: exactCombatValueSchema, hpLoss: exactCombatValueSchema })).max(3) }),
  z.strictObject({ ...base, kind: z.literal('LEAVE'), targetId: id }),
  z.strictObject({ ...base, kind: z.literal('RESERVE'), attackId: id, basePeriod: exactCombatValueSchema,
    period: exactCombatValueSchema, inZone: z.boolean(), controlImmune: z.boolean(), otherPeriodIncreaseBps: nat }),
  z.strictObject({ ...base, kind: z.literal('END'), reason: z.enum(['EXPIRY', 'CASTER_DOWN', 'NODE_END', 'ZONE_REMOVAL']) }),
])).max(4096);
type Approved = Awaited<ReturnType<typeof captureNyxApprovedSource> | ReturnType<typeof captureNyxApprovedSourceV2>>;
type Scope = z.infer<typeof scopeSchema>;
const json = (v: unknown) => canonicalJson(z.json().parse(v));
const need = (condition: boolean) => { if (!condition) throw new Error('nyx_intake_conflict'); };
const rational = (numerator: number, denominator = 1) => ExactCombatValue.from({ numerator: String(numerator), denominator: String(denominator) });

class NyxIntake {
  constructor(private readonly scope: Scope, private readonly formation: z.infer<typeof formationSchema>,
    private readonly source: Approved, readonly bindingHash: string) {}
  get packet() { return { version: 'combat.nyx-intake.v1.20261107', scope: structuredClone(this.scope), formation: structuredClone(this.formation),
    approvedSource: structuredClone(this.source), canStart: false, missing: [...this.source.packet.execution.missing] }; }

  /** Checks a supplied trace, not the truth of membership, damage, geometry, or execution support. */
  validate(input: unknown) {
    const trace = z.strictObject({ scope: scopeSchema, events: eventsSchema }).parse(input);
    need(json(trace.scope) === json(this.scope));
    type Create = Extract<z.infer<typeof eventsSchema>[number], { kind: 'CREATE' }>;
    let active: { creation: Create; index: number; lastTickAt: number | null; streaks: Map<string, number> } | null = null;
    let time = 0, validatedTicks = 0, nodeEnded = false, lastTickAt: number | null = null;
    const created = new Set<string>(), reservations = new Set<string>();
    for (const event of trace.events) {
      need(!nodeEnded && event.atMs >= time); time = event.atMs;
      if (active) {
        const rules = this.source.packet[active.creation.ability === 'ACTIVE' ? 'active' : 'ultimate'];
        const due = active.creation.atMs + rules.firstTickAfterCreationMs + active.index * rules.tickIntervalMs;
        need(Number.isSafeInteger(due) && event.atMs <= (active.index < rules.ticks ? due : active.creation.atMs + rules.durationMs));
      }
      if (event.kind === 'CREATE') {
        need(!created.has(event.zoneId) && lastTickAt !== event.atMs);
        if (active) { const rules = this.source.packet[active.creation.ability === 'ACTIVE' ? 'active' : 'ultimate'];
          need(event.atMs <= active.creation.atMs + rules.durationMs && active.lastTickAt !== event.atMs); }
        created.add(event.zoneId); active = { creation: event, index: 0, lastTickAt: null, streaks: new Map() }; continue;
      }
      need(active !== null && active.creation.zoneId === event.zoneId);
      const state = active!, rules = this.source.packet[state.creation.ability === 'ACTIVE' ? 'active' : 'ultimate'];
      const expiresAt = state.creation.atMs + rules.durationMs;
      need(Number.isSafeInteger(expiresAt) && event.atMs <= expiresAt);
      if (event.kind === 'END') {
        if (event.reason === 'EXPIRY') need(event.atMs === expiresAt && state.index === rules.ticks);
        else need(state.lastTickAt !== event.atMs);
        nodeEnded = event.reason === 'NODE_END'; active = null; continue;
      }
      if (event.kind === 'LEAVE') { state.streaks.delete(event.targetId); continue; }
      if (event.kind === 'RESERVE') {
        need(!reservations.has(event.attackId) && BigInt(event.basePeriod.numerator) > 0n);
        const increase = Math.max(event.otherPeriodIncreaseBps, event.inZone && !event.controlImmune ? rules.newBasicPeriodIncreaseBps : 0);
        need(Number.isSafeInteger(10000 + increase));
        const expected = ExactCombatValue.from(event.basePeriod).multiply(rational(10000 + increase, 10000));
        need(json(expected.snapshot()) === json(ExactCombatValue.from(event.period).snapshot()));
        reservations.add(event.attackId); continue;
      }
      need(event.index === state.index + 1 && event.index <= rules.ticks
        && event.atMs === state.creation.atMs + rules.firstTickAfterCreationMs + (event.index - 1) * rules.tickIntervalMs);
      need(new Set(event.eligibleTargetIds).size === event.eligibleTargetIds.length);
      const candidates = [...event.eligibleTargetIds].sort();
      if (event.focusTargetId && candidates.includes(event.focusTargetId)) {
        candidates.splice(candidates.indexOf(event.focusTargetId), 1); candidates.unshift(event.focusTargetId);
      }
      const selected = candidates.slice(0, rules.maximumTargetsPerTick);
      need(json(event.targets.map(t => t.targetId)) === json(selected));
      const next = new Map<string, number>();
      for (const target of event.targets) {
        const prior = state.streaks.get(target.targetId) ?? 0;
        let budget = ExactCombatValue.from(state.creation.attack).multiply(ExactCombatValue.from(state.creation.skillMagnitude))
          .multiply(rational(rules.totalTickAttackBps, 10000 * selected.length));
        if (prior >= this.source.packet.zone.passive.precedingConsecutiveHpLossTicks)
          budget = budget.multiply(rational(10000 + this.source.packet.zone.passive.selectedShareBonusBps, 10000));
        need(json(ExactCombatValue.from(target.attackBudget).snapshot()) === json(budget.snapshot()));
        if (BigInt(target.hpLoss.numerator) > 0n) next.set(target.targetId, Math.min(2, prior + 1));
      }
      state.streaks = next; state.index = event.index; state.lastTickAt = event.atMs; lastTickAt = event.atMs; validatedTicks++;
    }
    return { canStart: false, validation: 'SUPPLIED_TRACE_RULES_ONLY', validatedTicks, activeZoneId: active?.creation.zoneId ?? null };
  }
}

export async function prepareNyxIntake(input: unknown) {
  const parsed = z.strictObject({ scope: scopeSchema, formation: formationSchema, approvedSource: z.json() }).parse(input);
  need(parsed.formation.every((actor, slot) => actor.slot === slot && actor.operatorId === expectedOrder[slot]));
  const identity = z.object({ packet: z.object({ version: z.string() }) }).parse(parsed.approvedSource);
  const approved = identity.packet.version === 'combat.nyx-approved-requirements.v3.20261107'
    ? await captureNyxApprovedSourceV3() : identity.packet.version === 'combat.nyx-approved-requirements.v2.20261107'
    ? await captureNyxApprovedSourceV2() : await captureNyxApprovedSource();
  need(json(parsed.approvedSource) === json(approved));
  return new NyxIntake(parsed.scope, parsed.formation, approved, await sha256Hex(json(parsed)));
}

export function validateNyxZoneTrace(intake: NyxIntake, input: unknown) { return intake.validate(input); }

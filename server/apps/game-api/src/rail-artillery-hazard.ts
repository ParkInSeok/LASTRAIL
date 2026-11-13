import { z } from 'zod';
import { canonicalJson } from '@lastrail/common';
import { ExactCombatValue, exactCombatValueSchema, type ExactCombatSnapshot } from './combat-exact-value';
import { resolveNumericDefenseBudget } from './combat-numeric-defense';

const version = 'rail.artillery-hazard.v1.20261023' as const;
export const exactRailArtilleryHazardVersion = 'rail.artillery-hazard.v2.20261027' as const;
const id = z.string().min(1).max(128), natural = z.number().int().nonnegative().safe();
// Storage capacity, not a published gameplay interval or damage balance.
const maximumStoredImpacts = 1024;
const timing = z.discriminatedUnion('kind', [
  z.strictObject({ kind: z.literal('SINGLE'), impactAtMs: natural }),
  z.strictObject({ kind: z.literal('RESIDUAL'), startAtMs: natural, endAtMs: natural,
    intervalMs: natural.min(1), maximumImpacts: natural.min(1).max(maximumStoredImpacts) })
    .refine(t => t.startAtMs < t.endAtMs),
]);
const definitionFields = z.strictObject({ version: z.literal(version), runId: id, eventId: id, trainId: id,
  railSegmentId: id, carriageIds: z.array(id).min(1).max(128).refine(a => new Set(a).size === a.length),
  startMm: natural, endMm: natural, previewAtMs: natural, timing, damage: natural.min(1) });
const validWindow = (d: z.infer<typeof definitionFields> | { startMm: number; endMm: number; previewAtMs: number; timing: z.infer<typeof timing> }) =>
  d.startMm < d.endMm && d.previewAtMs <= (d.timing.kind === 'SINGLE' ? d.timing.impactAtMs : d.timing.startAtMs);
const definitionSchema = definitionFields.refine(validWindow);
const exactDefinitionSchema = definitionFields.extend({ version: z.literal(exactRailArtilleryHazardVersion),
  timing: z.strictObject({ kind: z.literal('SINGLE'), impactAtMs: natural }),
  damage: exactCombatValueSchema.refine(value => BigInt(value.numerator) > 0n
    && json(value) === json(ExactCombatValue.from(value).snapshot())) }).refine(validWindow);
const spanSchema = z.strictObject({ carriageId: id, railSegmentId: id, startMm: natural, endMm: natural })
  .refine(s => s.startMm <= s.endMm);
const frameSchema = z.strictObject({ runId: id, trainId: id, atMs: natural,
  spans: z.array(spanSchema).min(1).max(1024) });
type Definition = z.infer<typeof definitionSchema>;
type Frame = z.infer<typeof frameSchema>;
type Result<T, V extends string> = { version: V; identity: { runId: string; eventId: string; impactIndex: number };
  trainId: string; atMs: number; outcome: 'HIT' | 'MISSED'; rawDamageIntent: T; intersectingCarriageIds: string[] };
type Receipt<T, V extends string> = { input: Frame; result: Result<T, V> };
const json = (value: unknown) => canonicalJson(z.json().parse(value));
const fail = (reason: string): never => { throw new Error(`rail_hazard_${reason}`); };

/** Owns one launched rail hazard, its immutable announcement and impact identities.
 * The original motion owner must supply complete carriage spans at the exact
 * scheduled combat time (including split segments), never camera coordinates.
 * This internal model does not authenticate network geometry, invent a clock,
 * calculate defense/shield absorption or subtract HP. Its raw damage intent and checkpoint
 * must be committed atomically by the existing train damage/transaction owner.
 * A destroyed launcher does not cancel an already launched hazard.
 */
class RailHazard<T, V extends string> {
  private readonly receipts: Receipt<T, V>[] = [];
  protected constructor(private readonly definition: Omit<Definition, 'version' | 'damage'> & { version: V; damage: T },
    private readonly zero: T) {}

  nextImpactAtMs(): number | null {
    const t = this.definition.timing, count = this.receipts.length;
    if (t.kind === 'SINGLE') return count === 0 ? t.impactAtMs : null;
    if (count >= t.maximumImpacts) return null;
    const at = BigInt(t.startAtMs) + BigInt(count) * BigInt(t.intervalMs);
    return at < BigInt(t.endAtMs) ? Number(at) : null;
  }

  resolve(input: unknown): Result<T, V> {
    const frame = frameSchema.parse(input), d = this.definition;
    if (frame.runId !== d.runId || frame.trainId !== d.trainId) return fail('scope');
    const actual = new Set(frame.spans.map(s => s.carriageId));
    if (actual.size !== d.carriageIds.length || d.carriageIds.some(id => !actual.has(id))) return fail('incomplete_consist');
    if (new Set(frame.spans.map(json)).size !== frame.spans.length) return fail('duplicate_span');
    // Input order is not a different physical observation.
    frame.spans.sort((a, b) => json(a) < json(b) ? -1 : json(a) > json(b) ? 1 : 0);
    const old = this.receipts.find(r => r.input.atMs === frame.atMs);
    if (old) {
      if (json(old.input) !== json(frame)) return fail('event_conflict');
      return structuredClone(old.result);
    }
    if (frame.atMs !== this.nextImpactAtMs()) return fail('impact_time');
    const intersectingCarriageIds = [...new Set(frame.spans.filter(s => s.railSegmentId === d.railSegmentId
      && s.startMm <= d.endMm && s.endMm >= d.startMm).map(s => s.carriageId))].sort();
    const result: Result<T, V> = { version: d.version, identity: { runId: d.runId, eventId: d.eventId, impactIndex: this.receipts.length },
      trainId: d.trainId, atMs: frame.atMs, outcome: intersectingCarriageIds.length ? 'HIT' : 'MISSED',
      rawDamageIntent: structuredClone(intersectingCarriageIds.length ? d.damage : this.zero), intersectingCarriageIds };
    this.receipts.push({ input: frame, result });
    return structuredClone(result);
  }

  snapshot() { return structuredClone({ version: this.definition.version, definition: this.definition, receipts: this.receipts }); }
}

export class RailArtilleryHazard extends RailHazard<number, typeof version> {
  constructor(input: unknown) { super(definitionSchema.parse(input), 0); }

  /** Reconstruct and compare every stored transition, never trust a saved total.
   * Persistence must separately bind this snapshot to the original announcement.
   * A self-consistent client snapshot alone is not authority to deal damage. */
  static restore(input: unknown) {
    const saved = z.strictObject({ version: z.literal(version), definition: definitionSchema,
      receipts: z.array(z.strictObject({ input: frameSchema, result: z.json() })).max(maximumStoredImpacts) }).parse(input);
    const owner = new RailArtilleryHazard(saved.definition);
    for (const receipt of saved.receipts) {
      if (receipt.input.atMs !== owner.nextImpactAtMs()) return fail('restore_order');
      if (json(owner.resolve(receipt.input)) !== json(receipt.result)) return fail('restore_result');
    }
    return owner;
  }
}

/** Same collision/receipt owner, with exact raw values for the original defense pipeline. No HP execution. */
export class ExactRailArtilleryHazard extends RailHazard<ExactCombatSnapshot, typeof exactRailArtilleryHazardVersion> {
  constructor(input: unknown) { super(exactDefinitionSchema.parse(input), { numerator: '0', denominator: '1' }); }

  /** The caller retains the authoritative launched definition independently of the submitted checkpoint. */
  static restoreBound(input: unknown, originalDefinition: unknown) {
    const original = exactDefinitionSchema.parse(originalDefinition);
    const saved = z.object({ definition: z.unknown() }).parse(input);
    if (json(saved.definition) !== json(original)) return fail('original_definition_conflict');
    return ExactRailArtilleryHazard.restore(input);
  }

  static restore(input: unknown) {
    const saved = z.strictObject({ version: z.literal(exactRailArtilleryHazardVersion), definition: exactDefinitionSchema,
      receipts: z.array(z.strictObject({ input: frameSchema, result: z.json() })).max(1) }).parse(input);
    const owner = new ExactRailArtilleryHazard(saved.definition);
    for (const receipt of saved.receipts) {
      if (receipt.input.atMs !== owner.nextImpactAtMs()) return fail('restore_order');
      if (json(owner.resolve(receipt.input)) !== json(receipt.result)) return fail('restore_result');
    }
    return owner;
  }
}

/** Recompute the node's actual impact format. The caller must independently fence
 * the issued definition, frame and numeric sources; this does not authorize HP writes. */
export function verifyResolvedArtilleryImpact(originalDefinition: unknown, input: unknown, stepMs: number) {
  const definition = exactDefinitionSchema.parse(originalDefinition);
  const amount = natural.max(2147483647);
  const impact = z.strictObject({ request: z.strictObject({ frame: frameSchema, numeric: z.unknown(),
    tick: natural, protected: z.boolean() }), result: z.strictObject({ collision: z.unknown(), calculation: z.unknown(),
    hpLoss: amount, absorbed: amount, hpAfter: amount, trainDefeatedNow: z.boolean() }) }).parse(input);
  natural.min(1).parse(stepMs);
  if (BigInt(impact.request.tick) * BigInt(stepMs) !== BigInt(definition.timing.impactAtMs)) return fail('impact_time');
  const collision = new ExactRailArtilleryHazard(definition).resolve(impact.request.frame);
  const numeric = z.object({ base: exactCombatValueSchema }).parse(impact.request.numeric);
  if (json(numeric.base) !== json(definition.damage)) return fail('damage_source_conflict');
  const calculation = resolveNumericDefenseBudget(impact.request.numeric);
  // Unity's existing Vitals conversion is checked Int32, including misses.
  if (BigInt(calculation.ordinaryBeforeShield) > 2147483647n) return fail('impact_magnitude_unsupported');
  const result = impact.result;
  if (json(result.collision) !== json(collision) || json(result.calculation) !== json(calculation)) return fail('impact_result_conflict');
  if (BigInt(result.hpLoss) + BigInt(result.absorbed) > BigInt(calculation.ordinaryBeforeShield)
    || ((collision.outcome === 'MISSED' || impact.request.protected) && (result.hpLoss !== 0 || result.absorbed !== 0))
    || (result.trainDefeatedNow && (result.hpAfter !== 0 || result.hpLoss === 0))) return fail('impact_result_conflict');
  return { definition, collision, calculation };
}

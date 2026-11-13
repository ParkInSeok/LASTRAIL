import { z } from 'zod';
import { ExactCombatValue, exactCombatValueSchema } from './combat-exact-value';
import { numericDefenseInputSchema, resolveNumericDefenseBudget } from './combat-numeric-defense';

const id = z.string().min(1).max(200), time = z.number().int().nonnegative().safe();
const originalSchema = z.strictObject({ gunnerIds: z.array(id).length(2).refine(ids => new Set(ids).size === 2),
  hp: exactCombatValueSchema });
const eventsSchema = z.array(z.strictObject({ eventId: id, targetId: id, atMs: time,
  numeric: numericDefenseInputSchema, protected: z.boolean(), shieldRemaining: z.literal(0) })).max(100000);

/** Private execution candidate for the original two gunners, never another HP store.
 * The trusted execution source must prove event completeness, timing, numeric effects,
 * protection and absence of shields. Unsupported shield events fail, never default to zero. */
export function replayOrdinaryGunnerHealth(originalInput: unknown, eventsInput: unknown) {
  const original = originalSchema.parse(originalInput), events = eventsSchema.parse(eventsInput);
  const maximum = BigInt(ExactCombatValue.from(original.hp).floor());
  if (maximum < 1n || maximum > 2147483647n) throw new Error('ordinary_gunner_health_range');
  const targets = original.gunnerIds.map(targetId => ({ targetId, hp: Number(maximum), maxHp: Number(maximum) }));
  const seen = new Set<string>(), defeats: { gunnerId: string; eventId: string; atMs: number }[] = [];
  let previousTime = 0;
  const receipts = events.map(event => {
    const target = targets.find(t => t.targetId === event.targetId);
    if (!target || seen.has(event.eventId) || event.atMs < previousTime) throw new Error('ordinary_gunner_event_conflict');
    const calculation = resolveNumericDefenseBudget(event.numeric);
    const magnitude = BigInt(calculation.ordinaryBeforeShield);
    if (magnitude > 2147483647n) throw new Error('ordinary_gunner_damage_range');
    const hpBefore = target.hp;
    const hpLoss = event.protected ? 0 : Math.min(hpBefore, Number(magnitude));
    target.hp -= hpLoss;
    if (hpBefore > 0 && target.hp === 0) defeats.push({ gunnerId: target.targetId, eventId: event.eventId, atMs: event.atMs });
    seen.add(event.eventId); previousTime = event.atMs;
    return { eventId: event.eventId, targetId: target.targetId, atMs: event.atMs,
      calculation, hpBefore, hpLoss, hpAfter: target.hp, absorbed: 0 };
  });
  return { targets, receipts, defeats };
}

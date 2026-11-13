import { z } from 'zod';
import { ActivityError } from './activity-policy';

const ticks = z.number().int().nonnegative().safe();
const step = ticks.positive().refine(n => n % 10000 === 0);
const actionsSchema = z.strictObject({
  version: z.literal('ordinary.node-actions.v1.20261030'),
  stepTimeSpanTicks: step,
  ultimateAuto: z.boolean(),
  actors: z.array(z.strictObject({
    operatorId: z.string().min(1).max(200),
    basicNextOpportunityInTicks: ticks,
    activeCooldownRemainingTicks: ticks,
    ultimateCharge: z.number().int().min(0).max(2147483647),
  })).length(5),
});

function timeInputs(value: number, stepTimeSpanTicks: number) {
  if (!ticks.safeParse(value).success || !step.safeParse(stepTimeSpanTicks).success)
    throw new ActivityError(409, 'ordinary_node_actions_clock_conflict');
  return { value: BigInt(value), step: BigInt(stepTimeSpanTicks) };
}

/** run-actions v1 stores TimeSpan ticks; node-actions v1 uses remaining logical steps. */
export function cooldownTimeSpanToNodeSteps(remainingTimeSpanTicks: number, stepTimeSpanTicks: number) {
  const x = timeInputs(remainingTimeSpanTicks, stepTimeSpanTicks);
  return Number((x.value + x.step - 1n) / x.step);
}

/** At verified completion only: convert the execution owner's remaining steps before storing run-actions. */
export function cooldownNodeStepsToTimeSpan(remainingSteps: number, stepTimeSpanTicks: number) {
  const x = timeInputs(remainingSteps, stepTimeSpanTicks), duration = x.value * x.step;
  if (duration > BigInt(Number.MAX_SAFE_INTEGER)) throw new ActivityError(409, 'ordinary_node_actions_clock_conflict');
  return Number(duration);
}

/** Relative to node battle start, never wall time. The runtime owner supplies every value. */
export function readOrdinaryNodeActions(input: unknown, actorIds: readonly string[]) {
  const parsed = actionsSchema.safeParse(input);
  if (!parsed.success || actorIds.length !== 5 || new Set(actorIds).size !== 5
    || new Set(parsed.data.actors.map(a => a.operatorId)).size !== 5
    || parsed.data.actors.some(a => !actorIds.includes(a.operatorId)))
    throw new ActivityError(409, 'ordinary_node_actions_source_conflict');
  return parsed.data;
}

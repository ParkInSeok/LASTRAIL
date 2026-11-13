import { z } from 'zod';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { ActivityError } from './activity-policy';
import { capturePublishedOperationMap } from './operation-map-publisher';
import { readDepartureMap } from './operation-map-departure';

const json = (value: unknown) => canonicalJson(z.json().parse(value));
const fail = (reason: string): never => { throw new ActivityError(409, 'ordinary_obstacle_' + reason); };
const id = z.string().min(1).max(200), hash = z.string().regex(/^[a-f0-9]{64}$/u);
const natural = z.number().int().nonnegative().safe();
const definition = {
  version: 'ordinary.falling-obstacle.v1.20261106', hp: 100, defense: 0,
  shotDamage: 25, launchIntervalMs: 1650, preparationMs: 150, rangeMm: 100000, projectileSpeedMmPerSecond: 300000,
  zoneStartOffsetMm: 277000, zoneEndOffsetMm: 283000, hazardAtMs: 16000,
  horizontalBaseDamage: 600, fallingBaseDamage: 900,
  cruiseSpeedMmPerSecond: 20000, slowSpeedMmPerSecond: 14000, fastSpeedMmPerSecond: 26000,
  accelerationMmPerSecondSquared: 5000,
} as const;
/** Approved first comparison content only. Geometry, sockets and eligible shields remain owned sources. */
export function ordinaryObstacleDefinition() { return structuredClone(definition); }
/** Preserve the original combat contract while recognizing only this explicitly versioned prefix stage. */
export function ordinaryCombatDefinitionMatches(input: Record<string, unknown>, original: unknown) {
  if (!Object.hasOwn(input, 'ordinaryObstacle')) return json(input) === json(original);
  const { ordinaryObstacle, ...combat } = input;
  return json(ordinaryObstacle) === json(definition) && json(combat) === json(original);
}
const sequenceSchema = z.strictObject({ version: z.literal('ordinary.obstacle-sequence.v1.20261106'),
  obstacle: z.record(z.string(), z.json()), nextWave: z.strictObject({ kind: z.literal('ORIGINAL_NODE_ENEMIES'), enemiesHash: hash }) });
export type OrdinaryObstacleSequencePin = z.infer<typeof sequenceSchema>;
function capturePin(input: unknown) {
  const parsed = sequenceSchema.safeParse(input);
  if (!parsed.success || json(parsed.data.obstacle) !== json(definition)) return fail('definition_invalid');
  return parsed.data;
}

/** Only an explicit original catalog entry opts in. Never inject hazards into historical or unselected nodes. */
export async function readOrdinaryObstacleDefinition(start: Record<string, unknown>, nodeId: string) {
  if (!Object.hasOwn(start, 'operationMapContent')) return undefined;
  const plan = readDepartureMap(start);
  if (!plan) return fail('plan_missing');
  const content = await capturePublishedOperationMap(start.operationMapContent, plan);
  if (!content.encounters.some(e => Object.hasOwn(e.definition, 'ordinaryObstacle'))) return undefined;
  const node = plan.nodes.find(n => n.nodeId === nodeId);
  const binding = content.bindings.find(b => b.nodeId === nodeId);
  const encounter = content.encounters.find(e => e.ref === binding?.encounterRef);
  if (!node || !encounter) return fail('node_invalid');
  if (!Object.hasOwn(encounter.definition, 'ordinaryObstacle')) return undefined;
  if (node.eventKind !== 'COMBAT' || json(encounter.definition.ordinaryObstacle) !== json(definition)) return fail('definition_invalid');
  return ordinaryObstacleDefinition();
}
export async function prepareOrdinaryObstacleSequence(start: Record<string, unknown>, nodeId: string, enemies: unknown) {
  const obstacle = await readOrdinaryObstacleDefinition(start, nodeId);
  if (!obstacle) return undefined;
  const originalEnemies = z.object({ version: z.string(), gunners: z.array(z.unknown()).min(1), artillery: z.object({ hazardId: id }) }).safeParse(enemies);
  if (!originalEnemies.success) return fail('next_wave_unavailable');
  return capturePin({ version: 'ordinary.obstacle-sequence.v1.20261106', obstacle,
    nextWave: { kind: 'ORIGINAL_NODE_ENEMIES', enemiesHash: await sha256Hex(json(enemies)) } });
}

const scopeSchema = z.strictObject({ accountId: id, runId: id, nodeId: id,
  attemptGeneration: natural.refine(n => n > 0), snapshotHash: hash });
const resolutionSchema = scopeSchema.extend({ eventId: id, atMs: natural,
  outcome: z.enum(['DESTROYED', 'PASSED', 'COLLISION', 'DEFEAT']), obstacleHp: natural.max(100),
  trainHp: natural.max(2147483647), tailBeyondZone: z.boolean(), remainingHazards: natural,
  unresolvedProjectiles: natural, collisionCommitted: z.boolean() });
type Scope = z.infer<typeof scopeSchema>;
type Resolution = z.infer<typeof resolutionSchema>;
type Receipt = Scope & { version: 'ordinary.obstacle-transition.v1.20261106'; eventId: string; atMs: number;
  phase: 'COMBAT' | 'DEFEAT'; revision: 2; nextWave: OrdinaryObstacleSequencePin['nextWave'] | null };

/** Attempt-local phase owner. The existing execution owner supplies verified geometry/HP/effect results;
 * this class neither accepts public checkpoints nor owns a second HP, rail or simulation clock.
 * Consumers spawn the referenced original wave only on revision 1 -> 2, never on receipt replay. */
export class OrdinaryObstacleSequence {
  private readonly pin: OrdinaryObstacleSequencePin;
  private readonly scope: Scope;
  private result?: { resolution: Resolution; receipt: Receipt };
  constructor(pin: unknown, scope: unknown) { this.pin = capturePin(pin); this.scope = scopeSchema.parse(scope); }
  capture() {
    return this.result ? structuredClone(this.result.receipt)
      : { ...this.scope, version: 'ordinary.obstacle-transition.v1.20261106' as const, phase: 'OBSTACLE' as const,
        revision: 1 as const, nextWave: null };
  }
  resolve(input: unknown): Receipt {
    const parsed = resolutionSchema.safeParse(input);
    if (!parsed.success) return fail('resolution_invalid');
    const event = parsed.data;
    if (Object.entries(this.scope).some(([key, value]) => event[key as keyof Scope] !== value)) return fail('attempt_conflict');
    if (this.result) {
      if (event.eventId !== this.result.resolution.eventId) return fail('already_resolved');
      if (json(event) !== json(this.result.resolution)) return fail('event_conflict');
      return structuredClone(this.result.receipt);
    }
    if (event.outcome === 'DEFEAT' ? event.trainHp !== 0 : event.trainHp === 0) return fail('survival_conflict');
    if (event.outcome !== 'DEFEAT') {
      if (!event.tailBeyondZone || event.remainingHazards || event.unresolvedProjectiles) return fail('not_clear');
      if (event.outcome === 'DESTROYED' ? event.obstacleHp !== 0 : event.obstacleHp === 0) return fail('outcome_conflict');
      if (event.collisionCommitted !== (event.outcome === 'COLLISION')) return fail('collision_conflict');
      // A passed obstacle still finishes its original fall; tail clearance does not cancel future danger.
      if (event.outcome === 'PASSED' && event.atMs < definition.hazardAtMs) return fail('not_clear');
    }
    const receipt: Receipt = { ...this.scope, version: 'ordinary.obstacle-transition.v1.20261106',
      eventId: event.eventId, atMs: event.atMs, phase: event.outcome === 'DEFEAT' ? 'DEFEAT' : 'COMBAT', revision: 2,
      nextWave: event.outcome === 'DEFEAT' ? null : this.pin.nextWave };
    this.result = { resolution: structuredClone(event), receipt: structuredClone(receipt) };
    return structuredClone(receipt);
  }
}

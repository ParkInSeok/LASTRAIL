import { z } from 'zod';
import { canonicalJson } from '@lastrail/common';
import { ordinaryGeometryDefinitionSchema } from './ordinary-native-geometry';

const id = z.string().min(1).max(200), nat = z.number().int().nonnegative().safe(), positive = nat.min(1);
const coordinate = z.number().int().min(-1000000000).max(1000000000);
const point = z.tuple([coordinate, coordinate, coordinate]);
const vector = z.tuple([z.number().finite(), z.number().finite(), z.number().finite()]);
const frame = z.strictObject({ originMm: point, basis: z.tuple([vector, vector, vector]) });
const entity = z.strictObject({ id, authoringId: id, definitionRef: id, role: z.string(), generation: positive,
  present: z.boolean(), active: z.boolean(), frame,
  anchors: z.array(z.record(z.string(), z.json())).max(128), shapes: z.array(z.record(z.string(), z.json())).max(128) });
const observation = z.strictObject({ version: z.literal('ordinary.node-scene-observation.v1.20261102'),
  accountId: id, runId: id, nodeId: id, snapshotHash: z.string(), attemptGeneration: positive, attemptRevision: positive,
  definitionHash: z.string(), tick: nat, revision: positive, quantization: z.literal('NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO'),
  entities: z.array(entity).min(1).max(1000) });
const scopeSchema = observation.pick({ accountId: true, runId: true, nodeId: true, snapshotHash: true,
  attemptGeneration: true, definitionHash: true }).extend({ entryMapRevision: positive, mapRevision: positive,
  stepTimeSpanTicks: positive, checkpointAtMs: nat, operatorIds: z.array(id), gunnerIds: z.array(id),
  trainIds: z.array(id), deviceIds: z.array(id) });
const fail = (): never => { throw new Error('ordinary_scene_observation_conflict'); };
const json = (value: unknown) => canonicalJson(z.json().parse(value));
const dot = (a: number[], b: number[]) => a.reduce((s, x, i) => s + x * b[i]!, 0);

/** Checks registered local geometry and pose sequence, not native measurement authenticity
 * or movement/LOS. The original source must fence the complete observations and pose owner. */
export function verifyOrdinarySceneObservations(definitionInput: unknown, scopeInput: unknown, input: unknown) {
  const definition = ordinaryGeometryDefinitionSchema.parse(definitionInput), scope = scopeSchema.parse(scopeInput);
  const observations = z.array(observation).min(1).max(10000).parse(input);
  let previous: z.infer<typeof observation> | undefined, count = 0;
  for (const current of observations) {
    for (const key of ['accountId', 'runId', 'nodeId', 'snapshotHash', 'attemptGeneration', 'definitionHash'] as const)
      if (current[key] !== scope[key]) return fail();
    if (current.attemptRevision < scope.entryMapRevision || current.attemptRevision > scope.mapRevision
      || BigInt(current.tick) * BigInt(scope.stepTimeSpanTicks) > BigInt(scope.checkpointAtMs) * 10000n) return fail();
    if (previous && current.tick === previous.tick) { if (json(current) !== json(previous)) return fail(); continue; }
    if (current.revision !== (previous?.revision ?? 0) + 1 || previous && (current.tick < previous.tick
      || current.attemptRevision !== previous.attemptRevision)) return fail();
    if (new Set(current.entities.map(e => e.id)).size !== current.entities.length) return fail();
    for (const expected of definition.entities)
      if (!current.entities.some(e => e.authoringId === expected.authoringId)) return fail();
    if (previous && json(current.entities.map(e => e.id)) !== json(previous.entities.map(e => e.id))) return fail();
    for (const e of current.entities) {
      const registered = definition.entities.find(d => d.authoringId === e.authoringId);
      if (!registered || registered.role !== e.role || registered.definitionRef !== e.definitionRef
        || e.role === 'OPERATOR' && e.id !== e.definitionRef || json(e.anchors) !== json(registered.anchors)) return fail();
      const identityMatches = e.role === 'OPERATOR' ? scope.operatorIds.includes(e.id)
        : e.role === 'ENEMY' ? scope.gunnerIds.includes(e.id) && e.definitionRef === 'enemy_spike_cart_gunner'
        : e.role === 'TRAIN' ? scope.trainIds.includes(e.id)
        : e.role === 'DEVICE' ? scope.deviceIds.includes(e.id)
        : e.role === 'WORLD' && e.id === e.authoringId;
      if (!identityMatches || !e.active && e.shapes.some(s => s.active)) return fail();
      for (const anchor of e.anchors) point.parse(anchor.positionMm);
      for (const shape of e.shapes) {
        point.parse(shape.centerMm);
        if (shape.kind === 'BOX') point.parse(shape.sizeMm);
        else { coordinate.parse(shape.radiusMm); coordinate.parse(shape.heightMm); }
      }
      const shapes = e.shapes.map(s => {
        if (typeof s.active !== 'boolean') return fail();
        const geometry = { ...s }; delete geometry.active; return geometry;
      });
      if (json(shapes) !== json(registered.shapes)) return fail();
      const [x, y, zAxis] = e.frame.basis;
      const cross = [x[1] * y[2] - x[2] * y[1], x[2] * y[0] - x[0] * y[2], x[0] * y[1] - x[1] * y[0]];
      if ([x, y, zAxis].some(v => Math.abs(dot(v, v) - 1) > 1e-5)
        || Math.abs(dot(x, y)) > 1e-5 || Math.abs(dot(x, zAxis)) > 1e-5 || Math.abs(dot(y, zAxis)) > 1e-5
        || dot(cross, zAxis) < .99999) return fail();
      const old = previous?.entities.find(p => p.id === e.id);
      if (old && (old.authoringId !== e.authoringId || old.definitionRef !== e.definitionRef || old.role !== e.role)) return fail();
      if (!e.present) {
        if (e.active || e.shapes.some(s => s.active) || old && (e.generation !== old.generation
          || json(e.frame) !== json(old.frame))) return fail();
      } else if (old && !old.present) return fail();
    }
    previous = current; count++;
  }
  return { observations: count, lastTick: previous!.tick };
}

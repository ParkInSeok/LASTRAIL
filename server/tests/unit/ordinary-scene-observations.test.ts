import { expect, it } from 'vitest';
import { verifyOrdinarySceneObservations } from '../../apps/game-api/src/ordinary-scene-observations';
const entity = { authoringId: 'a', role: 'OPERATOR', definitionRef: 'op_reina', framePath: 'frame', assetPath: 'Assets/a.prefab',
  assetHash: 'a'.repeat(64), anchors: [{ id: 'muzzle', role: 'EMISSION', abilityId: 'op_reina.ultimate', positionMm: [0, 1, 0] }], shapes: [] };
const definition = { version: 'ordinary.native-geometry-definition.v1.20261031', definitionId: 'geometry', units: 'MILLIMETERS',
  millimetersPerUnit: 1000, axis: 'X_FORWARD_Y_UP_Z_SIDE', coordinateSpace: 'BOUND_FRAME_LOCAL',
  source: { scenePath: 'Assets/a.unity', sceneHash: 'b'.repeat(64) }, entities: [entity] };
const scope = { accountId: 'a', runId: 'r', nodeId: 'n', snapshotHash: 'c'.repeat(64), attemptGeneration: 2,
  definitionHash: 'd'.repeat(64), entryMapRevision: 3, mapRevision: 5, stepTimeSpanTicks: 500000, checkpointAtMs: 100,
  operatorIds: ['op_reina'], gunnerIds: ['gunner'], trainIds: ['train'], deviceIds: ['hazard'] };
const sample = () => ({ version: 'ordinary.node-scene-observation.v1.20261102', accountId: 'a', runId: 'r', nodeId: 'n',
  snapshotHash: scope.snapshotHash, attemptGeneration: 2, attemptRevision: 5, definitionHash: scope.definitionHash,
  tick: 1, revision: 1, quantization: 'NEAREST_MM_MIDPOINT_AWAY_FROM_ZERO', entities: [{ id: 'op_reina', authoringId: 'a',
    definitionRef: 'op_reina', role: 'OPERATOR', generation: 1, present: true, active: true,
    frame: { originMm: [0, 0, 0], basis: [[1, 0, 0], [0, 1, 0], [0, 0, 1]] }, anchors: entity.anchors, shapes: [] }] });
it('accepts immutable same-tick reads and later frame disappearance', () => {
  const first = sample(), absent = structuredClone(first);
  absent.tick = 2; absent.revision = 2; absent.entities[0]!.present = false; absent.entities[0]!.active = false;
  expect(verifyOrdinarySceneObservations(definition, scope, [first, first, absent])).toEqual({ observations: 2, lastTick: 2 });
});
it('rejects mixed attempts, changed local geometry, same-tick mutation and stale pose order', () => {
  for (const kind of ['attempt', 'anchor', 'same-tick', 'revision', 'future', 'missing', 'basis']) {
    const first = sample(), second = structuredClone(first); second.tick = 2; second.revision = 2;
    if (kind === 'attempt') second.attemptGeneration = 1;
    if (kind === 'anchor') second.entities[0]!.anchors[0]!.positionMm[0] = 2;
    if (kind === 'same-tick') second.tick = 1;
    if (kind === 'revision') second.revision = 1;
    if (kind === 'future') second.tick = 3;
    if (kind === 'missing') second.entities = [];
    if (kind === 'basis') second.entities[0]!.frame.basis[0]![0] = 2;
    expect(() => verifyOrdinarySceneObservations(definition, scope, [first, second])).toThrow();
  }
});
it('keeps disabled geometry distinct from disappearance and rejects a changed registered collider', () => {
  const shape = { id: 'box', role: 'OCCLUDER', kind: 'BOX', centerMm: [0, 0, 0], sizeMm: [2, 4, 6] };
  const registered = { ...definition, entities: [{ ...entity, shapes: [shape] }] };
  const first = { ...sample(), entities: [{ ...sample().entities[0]!, shapes: [{ ...shape, active: true }] }] };
  const disabled = structuredClone(first); disabled.tick = 2; disabled.revision = 2;
  disabled.entities[0]!.active = false; disabled.entities[0]!.shapes[0]!.active = false;
  expect(verifyOrdinarySceneObservations(registered, scope, [first, disabled]).observations).toBe(2);
  const changed = structuredClone(disabled); changed.entities[0]!.shapes[0]!.sizeMm[0] = 3;
  expect(() => verifyOrdinarySceneObservations(registered, scope, [first, changed])).toThrow();
  const absent = structuredClone(disabled); absent.entities[0]!.present = false;
  expect(verifyOrdinarySceneObservations(registered, scope, [first, absent]).lastTick).toBe(2);
  absent.entities[0]!.frame.originMm[0] = 5;
  expect(() => verifyOrdinarySceneObservations(registered, scope, [first, absent])).toThrow();
});
it.each(['ENEMY', 'TRAIN', 'DEVICE', 'WORLD'])('rejects foreign runtime identity for %s', role => {
  const definitionRef = role === 'ENEMY' ? 'enemy_spike_cart_gunner' : 'definition';
  const registered = { ...definition, entities: [{ ...entity, role, definitionRef }] };
  const observed = sample(); Object.assign(observed.entities[0]!, { id: 'foreign', role, definitionRef });
  expect(() => verifyOrdinarySceneObservations(registered, scope, [observed])).toThrow();
  observed.entities[0]!.id = ({ ENEMY: 'gunner', TRAIN: 'train', DEVICE: 'hazard', WORLD: 'a' } as Record<string, string>)[role]!;
  expect(verifyOrdinarySceneObservations(registered, scope, [observed]).observations).toBe(1);
});

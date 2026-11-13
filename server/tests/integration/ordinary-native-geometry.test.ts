import { expect, it } from 'vitest';
import { sha256Hex } from '@lastrail/common';
import { commanderFixture } from '../helpers/commander-db-fixture';
import { OrdinaryNativeGeometryRegistry } from '../../apps/game-api/src/ordinary-native-geometry';

// Synthetic measured artifact, not approved product geometry.
const candidate = () => ({ version: 'ordinary.native-geometry-definition.v1.20261031', definitionId: 'test-only',
  units: 'MILLIMETERS', millimetersPerUnit: 1000, axis: 'X_FORWARD_Y_UP_Z_SIDE', coordinateSpace: 'BOUND_FRAME_LOCAL',
  source: { scenePath: 'Assets/TestOnly.unity', sceneHash: 'a'.repeat(64) },
  entities: [{ authoringId: 'operator-test', role: 'OPERATOR', definitionRef: 'op_test', framePath: 'Root/Actor',
    assetPath: 'Assets/TestOnly.prefab', assetHash: 'b'.repeat(64),
    anchors: [{ id: 'foot', role: 'FOOT', positionMm: [0,0,0] },
      { id: 'emission', role: 'EMISSION', abilityId: 'op_test.basic', positionMm: [100,1000,0] }],
    shapes: [{ id: 'body', role: 'HIT_VOLUME', kind: 'CAPSULE_Y', centerMm: [0,900,0], radiusMm: 200, heightMm: 1800 }] }] });

it('registers only independently pinned measured bytes and preserves immutable recovery', async () => {
  const f = await commanderFixture();
  try {
    const body = JSON.stringify(candidate()), reference = { definitionId: 'test-only',
      version: 'ordinary.native-geometry-definition.v1.20261031', definitionHash: await sha256Hex(body) };
    const store = new OrdinaryNativeGeometryRegistry(f.db, reference);
    await expect(store.read()).rejects.toThrow('definition_unavailable');
    const receipt = await store.register(body);
    expect(receipt).toEqual({ ...reference, executionVerified: false });
    expect(await store.register(body)).toEqual(receipt);
    expect((await store.read()).definition).toEqual(candidate());
    const changed = candidate(); changed.entities[0].anchors[1].positionMm[0]++;
    await expect(store.register(JSON.stringify(changed))).rejects.toThrow('definition_conflict');
    const other = new OrdinaryNativeGeometryRegistry(f.db, { ...reference, definitionHash: await sha256Hex(JSON.stringify(changed)) });
    await expect(other.register(JSON.stringify(changed))).rejects.toThrow('publication_conflict');
    expect(() => f.sql.run('DELETE FROM ordinary_native_geometry_definitions')).toThrow();
  } finally { f.sql.close(); }
});

it('rejects malformed roles, duplicate anchors, unsafe paths and invalid shapes even when their hash is pinned', async () => {
  const f = await commanderFixture();
  try {
    for (const mutate of [
      (x: ReturnType<typeof candidate>) => { x.entities[0].anchors.push(x.entities[0].anchors[0]); },
      (x: ReturnType<typeof candidate>) => { x.entities[0].assetPath = 'Assets/../Other.prefab'; },
      (x: ReturnType<typeof candidate>) => { x.entities[0].shapes[0].heightMm = 1; },
      (x: ReturnType<typeof candidate>) => { delete x.entities[0].anchors[1].abilityId; },
    ]) {
      const value = candidate(); mutate(value); const body = JSON.stringify(value);
      const store = new OrdinaryNativeGeometryRegistry(f.db, { definitionId: value.definitionId,
        version: value.version, definitionHash: await sha256Hex(body) });
      await expect(store.register(body)).rejects.toThrow('definition_invalid');
    }
  } finally { f.sql.close(); }
});


it('registers the named enemy body only as original body occlusion, not a gunner or weapon', async () => {
  const { firstEnemyTrainBodyDefinition, firstEnemyTrainSpatialDefinition } = await import('../../apps/game-api/src/combat-first-enemy-spatial');
  const bodyDefinition = await firstEnemyTrainBodyDefinition();
  const original = await firstEnemyTrainSpatialDefinition();
  expect(bodyDefinition.definitionId).toBe('enemy_train_first_battle');
  expect(bodyDefinition.spatialReference).toEqual({ version: original.version, definitionHash: original.definitionHash });
  expect(original).not.toHaveProperty('definitionId');
  expect(original.geometry.definitionHash).toBe('2c212a13653c767cc4804fc1bbd94fdb9bf19c37507076ef1c4cbf8e0cda3fa1');
  const f = await commanderFixture();
  try {
    const make = () => ({ ...candidate(), entities: [{ authoringId: 'EnemyBody', role: 'TRAIN',
      definitionRef: bodyDefinition.definitionId, framePath: 'Root/EnemyBody',
      assetPath: bodyDefinition.assetPath, assetHash: bodyDefinition.assetHash, anchors: [],
      shapes: [{ id: 'body', role: 'OCCLUDER', kind: 'BOX', centerMm: [0,1600,0], sizeMm: [12000,3200,3000] }] }] });
    const register = async (value: ReturnType<typeof make>) => {
      const bytes = JSON.stringify(value);
      const registry = new OrdinaryNativeGeometryRegistry(f.db, { definitionId: value.definitionId,
        version: value.version, definitionHash: await sha256Hex(bytes) });
      return registry.register(bytes);
    };
    for (const mutate of [
      (x: ReturnType<typeof make>) => { x.entities[0].role = 'ENEMY'; },
      (x: ReturnType<typeof make>) => { x.entities[0].shapes[0].role = 'HIT_VOLUME'; },
      (x: ReturnType<typeof make>) => { x.entities[0].assetHash = '0'.repeat(64); },
    ]) {
      const value = make(); mutate(value);
      await expect(register(value)).rejects.toThrow('enemy_body_source_conflict');
    }
    expect((await register(make())).executionVerified).toBe(false);
  } finally { f.sql.close(); }
});

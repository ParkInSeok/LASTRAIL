import { expect, it } from 'vitest';
import { firstEnemyTrainSpatialDefinition, firstEnemyTrainInstanceId } from '../../apps/game-api/src/combat-first-enemy-spatial';
import { firstObjectiveEncounterIdentities } from '../../apps/game-api/src/combat-first-objective-verifier';
import { canonicalJson, sha256Hex } from '@lastrail/common';

it('publishes the approved measured enemy geometry without inventing muzzle or combat authority', async () => {
  const definition = await firstEnemyTrainSpatialDefinition();
  expect(definition.geometry).toMatchObject({ lengthMm: 12000, widthMm: 3000, heightMm: 3200,
    definitionHash: '2c212a13653c767cc4804fc1bbd94fdb9bf19c37507076ef1c4cbf8e0cda3fa1' });
  expect(definition.placement).toEqual({ gapFromPlayerFrontMm: 4000, gunnerRearOffsetsMm: [2000, 6000],
    footAnchorYmm: 3213, measuredRoofYmm: 3212.5 });
  expect(definition.motion).toEqual({ source: 'PLAYER_ACTUAL_MOTION', relativeSpeedMmPerSecond: 0 });
  expect(definition.muzzle).toBeNull();
  expect(definition.hitVolume).toBeNull();
  expect(definition.executionVerified).toBe(false);
  const { definitionHash, ...body } = definition;
  expect(definitionHash).toBe(await sha256Hex(canonicalJson(body)));
});

it('reproduces enemy vehicle identity and separates accounts, runs, encounters and other issued roles', async () => {
  const original = { accountId: 'account', runId: 'run', encounterId: 'arrive', sourceHash: 'a'.repeat(64) };
  const id = await firstEnemyTrainInstanceId(original);
  expect(id).toMatch(/^[a-f0-9]{32}$/u);
  expect(await firstEnemyTrainInstanceId({ ...original })).toBe(id);
  const issued = await firstObjectiveEncounterIdentities(original);
  expect([...issued.issuedGunnerIds, issued.issuedHazardId]).not.toContain(id);
  for (const field of ['accountId', 'runId', 'encounterId', 'sourceHash']) {
    expect(await firstEnemyTrainInstanceId({ ...original, [field]: field === 'sourceHash' ? 'b'.repeat(64) : 'other' })).not.toBe(id);
  }
  await expect(firstEnemyTrainInstanceId({ ...original, sourceHash: 'unverified' })).rejects.toThrow();
  await expect(firstEnemyTrainInstanceId({ ...original, physicalInstanceId: 'friendly' })).rejects.toThrow();
});

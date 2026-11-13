import { describe, expect, it } from 'vitest';
import { OrdinaryObstacleSequence, ordinaryCombatDefinitionMatches, ordinaryObstacleDefinition, prepareOrdinaryObstacleSequence } from '../../apps/game-api/src/ordinary-obstacle-sequence';
import { OperationMapPublisher } from '../../apps/game-api/src/operation-map-publisher';
import { captureCombatAuthority } from '../../apps/game-api/src/combat-authority';
import { plan } from '../helpers/operation-map-fixture';

const scope = { accountId: 'a', runId: 'r', nodeId: 'n', attemptGeneration: 1, snapshotHash: 'a'.repeat(64) };
const pin = { version: 'ordinary.obstacle-sequence.v1.20261106', obstacle: ordinaryObstacleDefinition(),
  nextWave: { kind: 'ORIGINAL_NODE_ENEMIES', enemiesHash: 'b'.repeat(64) } };
const resolution = { ...scope, eventId: 'clear', atMs: 22243, outcome: 'DESTROYED', obstacleHp: 0,
  trainHp: 5000, tailBeyondZone: true, remainingHazards: 0, unresolvedProjectiles: 0,
  collisionCommitted: false };

describe('ordinary obstacle to original combat sequence', () => {
  it('releases the original wave once and recovers the exact event receipt', () => {
    const state = new OrdinaryObstacleSequence(pin, scope);
    expect(state.capture().phase).toBe('OBSTACLE');
    const receipt = state.resolve(resolution);
    expect(receipt).toMatchObject({ phase: 'COMBAT', revision: 2, nextWave: pin.nextWave });
    expect(state.resolve(resolution)).toEqual(receipt);
    expect(() => state.resolve({ ...resolution, eventId: 'second' })).toThrow('already_resolved');
    expect(() => state.resolve({ ...resolution, trainHp: 4999 })).toThrow('event_conflict');
  });
  it.each([
    { attemptGeneration: 2 }, { snapshotHash: 'c'.repeat(64) }, { tailBeyondZone: false },
    { remainingHazards: 1 }, { unresolvedProjectiles: 1 }, { obstacleHp: 1 },
    { outcome: 'COLLISION', obstacleHp: 25, collisionCommitted: false },
    { outcome: 'PASSED', obstacleHp: 25, atMs: 15999 },
  ])('rejects incomplete or foreign resolution %j without advancing', change => {
    const state = new OrdinaryObstacleSequence(pin, scope);
    expect(() => state.resolve({ ...resolution, ...change })).toThrow();
    expect(state.capture()).toMatchObject({ phase: 'OBSTACLE', revision: 1 });
  });
  it('keeps death terminal and does not release a wave', () => {
    const state = new OrdinaryObstacleSequence(pin, scope);
    expect(state.resolve({ ...resolution, outcome: 'DEFEAT', trainHp: 0, obstacleHp: 25,
      tailBeyondZone: false, remainingHazards: 1 })).toMatchObject({ phase: 'DEFEAT', nextWave: null });
    expect(() => state.resolve(resolution)).toThrow();
  });
  it.each(['PASSED', 'COLLISION'])('accepts safe %s only after the owned outcome and clearance', outcome => {
    const state = new OrdinaryObstacleSequence(pin, scope);
    expect(state.resolve({ ...resolution, outcome, obstacleHp: 25, collisionCommitted: outcome === 'COLLISION' }).phase).toBe('COMBAT');
  });
  it('preserves historical absence without generating an obstacle', async () => {
    expect(await prepareOrdinaryObstacleSequence({}, 'n', {})).toBeUndefined();
    expect(ordinaryObstacleDefinition()).toMatchObject({ hp: 100, shotDamage: 25, launchIntervalMs: 1650, hazardAtMs: 16000 });
  });
  it('captures only the original published opt-in and rejects changed or incomplete content', async () => {
    const map = plan();
    for (const n of map.nodes) { n.encounterProfileRef = `e-${n.eventKind}`; n.rewardPlanRef = `r-${n.eventKind}`; }
    const kinds = [...new Set(map.nodes.map(n => n.eventKind))];
    const catalog = { version: 'operation.map-catalog.v1', catalogId: 'synthetic-obstacle-test', revision: 1,
      definitionVersion: map.definitionVersion,
      encounters: kinds.map(eventKind => ({ ref: `e-${eventKind}`, version: 'test', profile: 'ORDINARY_12_NODE', eventKind,
        definition: eventKind === 'COMBAT' ? { ordinaryObstacle: ordinaryObstacleDefinition() } : {} })),
      rewards: kinds.map(eventKind => ({ ref: `r-${eventKind}`, version: 'test', profile: 'ORDINARY_12_NODE', eventKind, definition: {} })) };
    const publication = await new OperationMapPublisher({ read: async () => catalog }).publish(map);
    const start = { encounterType: 'ORDINARY_12_NODE', admission: { operationMapPlan: map,
      combatAuthority: captureCombatAuthority('ORDINARY_SOLO') }, operationMapContent: publication };
    const enemies = { version: 'test-only', gunners: [{ instanceId: 'original-gunner' }], artillery: { hazardId: 'original-hazard' } };
    const issued = await prepareOrdinaryObstacleSequence(start, 'n1-0', enemies);
    expect(issued?.obstacle).toEqual(ordinaryObstacleDefinition());
    expect(issued?.nextWave.enemiesHash).toMatch(/^[a-f0-9]{64}$/u);
    expect(await prepareOrdinaryObstacleSequence(start, 'n1-1', enemies)).toBeUndefined();
    await expect(prepareOrdinaryObstacleSequence(start, 'n1-0', {})).rejects.toThrow('next_wave_unavailable');
    expect(ordinaryCombatDefinitionMatches({ base: 1, ordinaryObstacle: ordinaryObstacleDefinition() }, { base: 1 })).toBe(true);
    expect(ordinaryCombatDefinitionMatches({ base: 2, ordinaryObstacle: ordinaryObstacleDefinition() }, { base: 1 })).toBe(false);
    publication.encounters.find(e => e.eventKind === 'COMBAT')!.definition.ordinaryObstacle = { ...ordinaryObstacleDefinition(), hp: 99 };
    await expect(prepareOrdinaryObstacleSequence(start, 'n1-0', enemies)).rejects.toThrow();
  });
});

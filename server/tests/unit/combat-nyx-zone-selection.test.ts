import { expect, it } from 'vitest';
import { writeFileSync } from 'node:fs';
import { acceptNyxZoneSelection, captureNyxZoneSelectionSource } from '../../apps/game-api/src/combat-nyx-zone-selection';
import { captureNyxApprovedSourceV2, captureNyxApprovedSourceV3 } from '../../apps/game-api/src/combat-nyx-approved-source';
import { prepareNyxIntake } from '../../apps/game-api/src/combat-nyx-intake';

const observation = () => ({ scope: { accountId: 'a', runId: 'r', nodeId: 'n', attemptGeneration: 1, snapshotHash: 'a'.repeat(64) },
  authorityRevision: 1, footWorldMm: { x: 0, y: 0, z: 0 }, lineOfSight: true, eligible: true,
  zone: { zoneId: 'enemy-car', kind: 'ENEMY_SELECTED_CARRIAGE', carId: 'car', frameId: 'frame', ownerGeneration: 2,
    representativeCenterWorldMm: { x: 48000, y: 0, z: 0 },
    localBoundary: { minMm: { x: -6000, y: -1000, z: -1500 }, maxMm: { x: 6000, y: 1000, z: 1500 } }, valid: true } });

it('uses inclusive 48m center distance in all three dimensions, never nearest boundary', async () => {
  const value = observation();
  await expect(acceptNyxZoneSelection(value)).resolves.toBeDefined();
  value.zone.representativeCenterWorldMm.x = 48001;
  await expect(acceptNyxZoneSelection(value)).rejects.toThrow('nyx_zone_selection_conflict');
  value.zone.representativeCenterWorldMm = { x: 0, y: 28800, z: 38400 };
  await expect(acceptNyxZoneSelection(value)).resolves.toBeDefined();
  value.zone.representativeCenterWorldMm.y++;
  await expect(acceptNyxZoneSelection(value)).rejects.toThrow('nyx_zone_selection_conflict');
});

it('requires clear LOS, eligible registered observation and exactly one valid zone', async () => {
  for (const key of ['lineOfSight', 'eligible'] as const) {
    const value = observation(); value[key] = false;
    await expect(acceptNyxZoneSelection(value)).rejects.toThrow();
  }
  const value = observation(); value.zone.valid = false;
  await expect(acceptNyxZoneSelection(value)).rejects.toThrow();
  await expect(acceptNyxZoneSelection({ ...observation(), zone: [observation().zone, observation().zone] })).rejects.toThrow();
  const inverted = observation(); inverted.zone.localBoundary.maxMm.x = -7000;
  await expect(acceptNyxZoneSelection(inverted)).rejects.toThrow();
});

it('locks acceptance and rejects changed zone/frame/generation/attempt without returning creation authority', async () => {
  const value = observation(), selected = await acceptNyxZoneSelection(value);
  const original = structuredClone(value);
  value.zone.zoneId = 'caller-mutated';
  const current = { scope: original.scope, zone: structuredClone(original.zone) };
  current.zone.representativeCenterWorldMm.x = 90000;
  expect(selected.validateCreation(current).zone.zoneId).toBe('enemy-car');
  for (const patch of [{ zoneId: 'other' }, { ownerGeneration: 3 }, { frameId: 'other' }, { valid: false }]) {
    expect(() => selected.validateCreation({ ...current, zone: { ...current.zone, ...patch } })).toThrow();
  }
  expect(() => selected.validateCreation({ ...current, scope: { ...current.scope, attemptGeneration: 2 } })).toThrow();
  const returned = selected.validateCreation(current); returned.zone.zoneId = 'mutated-return';
  expect(selected.validateCreation(current).zone.zoneId).toBe('enemy-car');
});

it('publishes separately versioned 48m selection while retaining previous definition and 24m basics', async () => {
  expect((await captureNyxApprovedSourceV2()).sourceHash).toBe('753e8bca39e931f279afe5d7eeeafbbea36f9ff446cc4088bb429312b6b11d67');
  const approved = await captureNyxApprovedSourceV3(), selection = await captureNyxZoneSelectionSource();
  const intake = await prepareNyxIntake({ scope: observation().scope, approvedSource: approved,
    formation: ['op_reina', 'op_kyle', 'op_eve', 'op_sera', 'op_nyx'].map((operatorId,slot)=>({operatorId,slot})) });
  expect(intake.packet.canStart).toBe(false);
  expect(approved.packet.basic.range.maximumDistanceMm).toBe(24000);
  expect(selection.packet.geometry.maximumDistanceMm).toBe(48000);
  expect(selection.packet.canStart).toBe(false);
  expect(selection.packet.midCombatPersistence).toBe(false);
  if (process.env.NYX_SELECTION_QA) writeFileSync(process.env.NYX_SELECTION_QA, JSON.stringify({ approved, selection,
    scope: 'SUPPLIED_OBSERVATION_RULES_ONLY_NOT_REGISTERED_GEOMETRY' }, null, 2));
});

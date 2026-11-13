import { expect, it } from 'vitest';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { captureNyxApprovedSource, captureNyxApprovedSourceV2 } from '../../apps/game-api/src/combat-nyx-approved-source';
import { captureCombatPolicyRequirements } from '../../apps/game-api/src/combat-policy-catalog';
import { bindCombatPolicyRequirements } from '../../apps/game-api/src/combat-policy-binding';

it('publishes approved mitigation and ballistics in v2 while preserving historical v1 bytes', async () => {
  expect((await captureNyxApprovedSource()).sourceHash).toBe('ecf13573583877e76fffdc93f7c9c31c6b7b55394ed7e2fb5136afeae094d584');
  const source = await captureNyxApprovedSourceV2();
  expect(source.sourceHash).toBe(await sha256Hex(canonicalJson(source.packet)));
  expect(source.packet.active.mitigationRateReductionBps).toBe(800);
  expect(source.packet.ultimate.mitigationRateReductionBps).toBe(1500);
  expect(source.packet.active).not.toHaveProperty('armorWeakeningPercentagePoints');
  expect(source.packet.zone).not.toHaveProperty('armorWeakening');
  expect(source.packet.zone.sourceKind).toBe('NYX_ZONE');
  expect(source.packet.zone.effectKinds).toEqual({ mitigation: 'MITIGATION_RATE_REDUCTION', newBasicPeriod: 'NEW_BASIC_PERIOD_INCREASE' });
  expect(source.packet.basic.ballistics.maximumFlightMs).toBe(500);
  expect(source.packet.execution.canStart).toBe(false);
});

it('pins approved requirements without mutating later captures or authorizing an incomplete formation', async () => {
  const before = captureCombatPolicyRequirements();
  const source = await captureNyxApprovedSource();
  expect(source.sourceHash).toBe(await sha256Hex(canonicalJson(source.packet)));
  expect(source.packet.execution.canStart).toBe(false);
  source.packet.active.totalTickAttackBps = 999999;
  expect((await captureNyxApprovedSource()).sourceHash).toBe(source.sourceHash);
  const requested = ['op_reina', 'op_kyle', 'op_eve', 'op_sera', 'op_nyx'];
  const admission = { version: 'departure-v4', combatPolicyRequirements: before,
    formation: { operators: requested.map((operatorId, slot) => ({ operatorId, slot })) } };
  const result = bindCombatPolicyRequirements(admission, 'I');
  expect(result.canExecute).toBe(false);
  expect(result.blockingReasons).toContain('combat_policy_operator_unsupported');
  expect(result.operators).toEqual([]);
  expect(admission.formation.operators.map(o => o.operatorId)).toEqual(requested);
  expect(captureCombatPolicyRequirements()).toEqual(before);
});

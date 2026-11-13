import { expect, it } from 'vitest';
import { writeFileSync } from 'node:fs';
import { ce03ProducerFixture } from '../helpers/ce03-producer-fixture';
import { prepareOrdinaryFiveStatOrigin, readOrdinaryFiveStatOrigin } from '../../apps/game-api/src/combat-ordinary-stat-owner';
import { captureCombatPolicyForFormation, captureCombatPolicyRequirements, nyxFormationOrder } from '../../apps/game-api/src/combat-policy-catalog';
import { captureCombatAuthority } from '../../apps/game-api/src/combat-authority';
import { captureOrdinaryKitSourceV3, captureOrdinaryKitSourceV2, readOrdinaryKitFromOriginal } from '../../apps/game-api/src/combat-ordinary-kit-source';
import { approvedKyleShieldAbilities } from '../../apps/game-api/src/combat-shield-budget';
import { canonicalJson, sha256Hex } from '@lastrail/common';
import { captureOrdinaryBasicSourceV4, captureOrdinaryBasicSourceV5 } from '../../apps/game-api/src/combat-ordinary-basic-source';
import { captureOrdinaryRelocationSourceV8, captureOrdinaryRelocationSourceV9, captureOrdinaryRelocationSourceV10, captureOrdinaryRelocationSource, readOrdinaryRelocationFromOriginal } from '../../apps/game-api/src/combat-ordinary-relocation-source';
import { captureNyxApprovedSourceV3 } from '../../apps/game-api/src/combat-nyx-approved-source';

it('derives Nyx stats and kit from actual owned slots without changing retained five-actor definitions', async () => {
  const f = await ce03ProducerFixture();
  try {
    const historical = captureCombatPolicyRequirements();
    f.sql.run("INSERT INTO account_operators(account_id,operator_id,acquired_at) VALUES('a','op_nyx',0)");
    f.sql.run("DELETE FROM operator_training_core_slots WHERE account_id='a'");
    nyxFormationOrder.forEach((id, slot) => f.sql.run("INSERT INTO operator_training_core_slots(account_id,slot_index,operator_id) VALUES('a',?,?)", [slot, id]));
    const operators = nyxFormationOrder.map((operatorId, slot) => ({ operatorId, slot }));
    f.sql.run("UPDATE account_formation SET formation_json=? WHERE account_id='a'", [JSON.stringify({ schemaVersion: 1, operators })]);
    const prepared = await prepareOrdinaryFiveStatOrigin(f.env.DB, 'a', 'nyx-run', { expectedFormationRevision: 1, expectedLoadoutRevision: 1 });
    expect(prepared.origin.actors.map(a => a.operatorId)).toEqual(nyxFormationOrder);
    expect(prepared.origin.actors[4]!.permanentStats).toMatchObject({ maxHp: 880, defense: 53, basicIntervalMs: 1800 });
    const attack = prepared.origin.actors[4]!.permanentStats.attack;
    expect(BigInt(attack.numerator)).toBe(47n * BigInt(attack.denominator));
    const admission = { version: 'departure-v4', combatAuthority: captureCombatAuthority('ORDINARY_SOLO'),
      combatPolicyRequirements: captureCombatPolicyForFormation(operators), formation: { operators } };
    const kit = await captureOrdinaryKitSourceV3(prepared.origin, prepared.originHash, admission);
    expect(kit.packet.operators.map(a => a.operatorId)).toEqual(nyxFormationOrder);
    expect(kit.packet.operators[4]!.abilities.map(a => a.effect)).toEqual(['FIXED_ZONE_CREATE', 'FIXED_ZONE_REPLACE_ON_CREATION']);
    expect(kit.kitHash).toBe(await sha256Hex(canonicalJson(kit.packet)));
    expect(kit.packet.operators.find(o=>o.operatorId==='op_kyle')!.abilities[0]).toMatchObject({maximumFrontTargets:3,tauntRangeMm:12000});
    const retainedKit=structuredClone(kit);
    retainedKit.packet.operators.find(o=>o.operatorId==='op_kyle')!.abilities=approvedKyleShieldAbilities.map(a=>({...a}));
    retainedKit.kitHash=await sha256Hex(canonicalJson(retainedKit.packet));
    const retainedOriginal={origin:prepared.origin,originHash:prepared.originHash,
      startSnapshotJson:JSON.stringify({admission,ordinaryKitSource:retainedKit})};
    expect(await readOrdinaryKitFromOriginal(retainedOriginal)).toEqual(retainedKit);
    expect(await readOrdinaryKitFromOriginal({...retainedOriginal,startSnapshotJson:JSON.stringify({admission,ordinaryKitSource:kit})})).toEqual(kit);
    const retainedBasic=await captureOrdinaryBasicSourceV4(retainedKit);
    const retainedRelocation=await captureOrdinaryRelocationSourceV8(retainedBasic,retainedKit);
    expect(retainedBasic.basicHash).toBe('d6c85c78176987bf65092ea178607db7040ccf96c070ef6d1a44a6dc3ad542d1');
    expect(retainedRelocation.relocationHash).toBe('76d085ddb40b782f493266b02c87e97807b399b0e14bf99bf43ba6e11206ab53');
    const basic=await captureOrdinaryBasicSourceV5(kit);
    expect(basic.packet.profiles.map(p=>p.operatorId)).toEqual(nyxFormationOrder);
    expect(basic.packet.profiles[4]).toMatchObject({operatorId:'op_nyx',timing:{baseCycleMs:1800,windupMs:300,recoveryMs:300},
      ballistics:{speedMmPerSecond:48000,radiusMm:60,maximumFlightDistanceMm:24000}});
    expect((await captureOrdinaryRelocationSourceV9(await captureOrdinaryBasicSourceV5(retainedKit),retainedKit)).relocationHash).toBe('ea7d728c01de42a147711bbf93125ca49013cefe3f87c3c4bd496eb4063982e1');
    const relocation=await captureOrdinaryRelocationSourceV10(basic,kit);
    expect(relocation.packet.zoneSelectionSource.packet.geometry.maximumDistanceMm).toBe(48000);
    expect(basic.packet.timingPolicy.restore).toBe('RESTART_FROM_ORIGINAL_NODE_START_NO_MID_COMBAT_RESTORE');
    expect(relocation.packet.midCombatPersistence).toBe(false);
    const restores: unknown[]=[];
    const collect=(value: unknown):void=>{
      if(!value||typeof value!=='object')return;
      for(const [key,child] of Object.entries(value)){if(key==='restore')restores.push(child);collect(child);}
    };
    collect(relocation.packet);
    expect(restores.length).toBeGreaterThan(0);
    expect(new Set(restores)).toEqual(new Set(['RESTART_FROM_ORIGINAL_NODE_START_NO_MID_COMBAT_RESTORE']));
    expect(relocation.packet.checkpoint.idempotency).toBe(retainedRelocation.packet.checkpoint.idempotency);
    await expect(captureOrdinaryRelocationSourceV8(basic,kit)).rejects.toThrow('basic_version_unsupported');
    expect(relocation.packet.skillScopes.filter(s=>s.abilityId.startsWith('op_nyx.'))).toHaveLength(2);
    expect(relocation.packet.skillScopes.some(s=>s.abilityId.startsWith('op_mira.'))).toBe(false);
    expect(relocation.packet.runtimeSupported).toBe(false);
    await expect(captureOrdinaryRelocationSource(basic)).rejects.toThrow('basic_version_unsupported');
    const original = { origin: prepared.origin, originHash: prepared.originHash,
      startSnapshotJson: JSON.stringify({ ordinaryRelocationSource: relocation }) };
    expect(await readOrdinaryRelocationFromOriginal(original, basic, kit)).toEqual(relocation);
    expect(prepared.roofSource?.packet.operatorIds).toEqual(nyxFormationOrder);
    expect(await readOrdinaryFiveStatOrigin(prepared.origin, prepared.originHash)).toEqual(prepared.origin);
    await expect(captureOrdinaryKitSourceV2(prepared.origin, prepared.originHash, admission)).rejects.toThrow('version_scope_conflict');
    expect(captureCombatPolicyRequirements()).toEqual(historical);
    expect(f.scalar('SELECT COUNT(*) FROM runs')).toBe(0);
    if (process.env.NYX_SOURCE_QA) writeFileSync(process.env.NYX_SOURCE_QA, JSON.stringify({
      scope: 'LOCAL_SQLITE_OWNED_SOURCE_CONTRACT_ONLY', canStart: false,
      approved: await captureNyxApprovedSourceV3(), admission,
      ordinaryStatSource: { origin: prepared.origin, originHash: prepared.originHash },
      ordinaryTrainSource: prepared.trainSource, ordinaryRailSource: prepared.railSource,
      ordinaryRoofSource: prepared.roofSource, ordinaryKitSource: kit,
      ordinaryBasicSource: basic, ordinaryRelocationSource: relocation,
    }, null, 2));
  } finally { f.sql.close(); }
});

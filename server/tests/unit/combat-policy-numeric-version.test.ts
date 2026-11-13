import {expect,it} from "vitest";
import {captureCombatPolicyRequirements,readPinnedCombatPolicyRequirements} from "../../apps/game-api/src/combat-policy-catalog";
import {bindCombatPolicyRequirements} from "../../apps/game-api/src/combat-policy-binding";
it("publishes approved numeric DEF and new speed curves without legacy armor or CDR caps",()=>{
 const p=captureCombatPolicyRequirements();expect(p.version).toBe("combat.policy-catalog.2026-09-29.1");
 expect(p.passives.find(x=>x.operatorId==="op_kyle")).toEqual({operatorId:"op_kyle",source:"SELF_HP_STRICTLY_BELOW_HALF",group:"CONDITIONAL_FLAT_DEF_AFTER_STAT_MULTIPLIERS",valueFlat:83});
 expect(p.numericDefense.operatorBase).toEqual({op_kyle:250,op_reina:53,op_eve:53,op_sera:87,op_mira:111});
 expect(p.numericDefense.operatorLevelGrowth).toBe("FIXED");expect(p.numericDefense.budgetVersion).toBe("combat.numeric-defense-budget.v1");
 expect(p.skills[0]).not.toHaveProperty("armorBps");expect(p.skills[0]).toHaveProperty("defense",0);
 expect(p.equipmentTiming.skillHasteByStarBps).toEqual([500,1200,3000,7000,15000]);expect(p.equipmentTiming.attackSpeedByStarBps).toEqual([500,1200,3000,7000,15000]);
 expect(p.equipmentTiming.equipmentAttackSpeedCapBps).toBeNull();expect(p.equipmentTiming.skillHasteCapBps).toBeNull();expect(p.status).toBe("REQUIRED_NOT_EXECUTABLE");
});
it("dispatches exact pinned legacy definitions and rejects forged/unknown versions",()=>{
 const legacy=readPinnedCombatPolicyRequirements("combat.policy-catalog.2026-09-22.1")!;
 expect(legacy.passives[0]).toMatchObject({group:"ARMOR_PERCENTAGE_POINTS",valueBps:500});expect(legacy).not.toHaveProperty("numericDefense");
 const admission={version:"departure-v4",combatPolicyRequirements:legacy,formation:{operators:legacy.scope.roster.map((operatorId,slot)=>({slot,operatorId}))}};
 expect(bindCombatPolicyRequirements(admission,"I")).toMatchObject({status:"EXECUTION_UNAVAILABLE",policyVersion:legacy.version});
 legacy.ultimate.cost=999;expect(bindCombatPolicyRequirements(admission,"I").blockingReasons).toContain("combat_policy_catalog_mismatch");
 expect(readPinnedCombatPolicyRequirements("unknown")).toBeNull();expect(readPinnedCombatPolicyRequirements("combat.policy-catalog.2026-09-22.1")?.ultimate.cost).toBe(100);
});

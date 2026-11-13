import { expect,it } from "vitest";
import { captureCombatPolicyRequirements } from "../../apps/game-api/src/combat-policy-catalog";
import { describeCombatRequirements } from "../../apps/game-api/src/combat-readiness";
const ids=["op_kyle","op_reina","op_eve","op_sera","op_mira"];
function preview() { return {allowedOperatorSlots:5,admission:{version:"departure-v4",combatPolicyRequirements:captureCombatPolicyRequirements(),ultimateSelection:{rulesVersion:"ultimate-selection-v1",operatorId:null,revision:0},formation:{operators:ids.map((operatorId,slot)=>({operatorId,slot}))}}}; }
it("binds all five selected operators to their latest abilities and passive requirements",()=>{
 const r=describeCombatRequirements(JSON.stringify(preview()),"I");
 expect(r.policyBinding).toMatchObject({version:"combat.policy-binding.v1",status:"EXECUTION_UNAVAILABLE",canExecute:false});
 expect(r.policyBinding.operators).toHaveLength(5);
 expect(r.policyBinding.operators.map(o=>o.abilities.length)).toEqual([2,2,2,2,2]);
 expect(r.policyBinding.operators[2]?.abilities[0]).toMatchObject({id:"op_eve.active",effect:"SENTRY_CREATE"});
 expect(r.policyBinding.operators[3]?.passive).toMatchObject({source:"OWN_HEAL"});
 expect(r.blockingReasons).not.toContain("ultimate_selection_required");
 expect(r.canStart).toBe(false);
});
it("does not silently substitute the old catalog for changed definitions",()=>{
 const p=preview();p.admission.combatPolicyRequirements.skills[0]!.effect="OLD_HEAL";
 const r=describeCombatRequirements(JSON.stringify(p),"I");
 expect(r.policyBinding.blockingReasons).toContain("combat_policy_catalog_mismatch");
 expect(r.policyBinding.operators).toEqual([]);
});
it("rejects unknown actors, duplicate formation identities and unsupported risk",()=>{
 const p=preview();p.admission.formation.operators[0]!.operatorId="unknown";
 expect(describeCombatRequirements(JSON.stringify(p),"I").policyBinding.blockingReasons).toContain("combat_policy_operator_unsupported");
 p.admission.formation.operators[0]!.operatorId="op_reina";
 expect(describeCombatRequirements(JSON.stringify(p),"I").policyBinding.blockingReasons).toContain("combat_policy_formation_invalid");
 expect(describeCombatRequirements(JSON.stringify(preview()),"II").policyBinding.blockingReasons).toContain("combat_policy_risk_unsupported");
});
it("leaves historical admission on its historical policy instead of injecting new abilities",()=>{
 const p=preview();p.admission.version="departure-v3";
 expect(describeCombatRequirements(JSON.stringify(p),"I").policyBinding).toMatchObject({status:"HISTORICAL",operators:[],canExecute:false});
});

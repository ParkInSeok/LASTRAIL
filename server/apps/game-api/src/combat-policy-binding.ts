import { z } from "zod";
import { canonicalJson } from "@lastrail/common";
import { captureCombatPolicyRequirements, readPinnedCombatPolicyRequirements } from "./combat-policy-catalog";
import { approvedKyleShieldAbilities } from "./combat-shield-budget";
type Json = Parameters<typeof canonicalJson>[0];
type Ability = { id: string; effect: string } & Record<string, Json>;
type OperatorBinding = { operatorId: string; slot: number; role: "OPERATOR";
  passive: ReturnType<typeof captureCombatPolicyRequirements>["passives"][number]; abilities: Ability[] };
const admissionSchema = z.object({ version: z.literal("departure-v4"), combatPolicyRequirements: z.unknown(),
  formation: z.object({ operators: z.array(z.object({ slot: z.number().int().min(0).max(4), operatorId: z.string().min(1) })).length(5) }) });
// Unchanged initial Kyle/Reina kits from 44 F2; no historical Eve/Sera/Mira kit import.
const retainedKits: Ability[] = [
 ...approvedKyleShieldAbilities,
 {id:"op_reina.active",effect:"DIRECT_DAMAGE",cooldownMs:14000,windupMs:450,recoveryMs:300,damageBps:40000},
 {id:"op_reina.ultimate",effect:"DIRECT_DAMAGE",cost:100,windupMs:800,recoveryMs:300,damageBps:100000},
];
/** Bind requirements only. Runtime handler evidence is absent and must never be inferred from catalog presence. */
export function bindCombatPolicyRequirements(admission: unknown, risk: string) {
 const result={version:"combat.policy-binding.v1",status:"INVALID",canExecute:false,policyVersion:null as string|null,
  operators:[] as OperatorBinding[],requiredEffectKinds:[] as string[],blockingReasons:[] as string[]};
 const version=z.object({version:z.string()}).safeParse(admission);
 if(!version.success || ["departure-v1","departure-v2","departure-v3"].includes(version.data.version)) return {...result,status:"HISTORICAL"};
 const parsed=admissionSchema.safeParse(admission);
 if(!parsed.success) return {...result,blockingReasons:["combat_policy_formation_invalid"]};
 const identity=z.object({version:z.string()}).safeParse(parsed.data.combatPolicyRequirements);
 const catalog=identity.success?readPinnedCombatPolicyRequirements(identity.data.version):null;
 if(!catalog)return {...result,blockingReasons:["combat_policy_catalog_mismatch"]};
 let matches=false;
 try {matches=canonicalJson(parsed.data.combatPolicyRequirements as Json)===canonicalJson(catalog);} catch { /* Invalid stored JSON shape remains blocked. */ }
 if(!matches) return {...result,blockingReasons:["combat_policy_catalog_mismatch"]};
 result.policyVersion=catalog.version;
 const operators=parsed.data.formation.operators;
 if(new Set(operators.map(o=>o.slot)).size!==5 || new Set(operators.map(o=>o.operatorId)).size!==5)
  result.blockingReasons.push("combat_policy_formation_invalid");
 if(operators.some(o=>!catalog.scope.roster.includes(o.operatorId))) result.blockingReasons.push("combat_policy_operator_unsupported");
 if(risk!==catalog.scope.ordinaryRisk) result.blockingReasons.push("combat_policy_risk_unsupported");
 if(result.blockingReasons.length) return result;
 const abilities: Ability[]=[...retainedKits,...catalog.skills];
 result.operators=[...operators].sort((a,b)=>a.slot-b.slot).map(o=>({...o,role:"OPERATOR",passive:{...catalog.passives.find(p=>p.operatorId===o.operatorId)!},
  abilities:abilities.filter(a=>a.id===`${o.operatorId}.active` || a.id===`${o.operatorId}.ultimate`).map(a=>({...a}))}));
 result.requiredEffectKinds=[...new Set(result.operators.flatMap(o=>o.abilities.map(a=>a.effect)))].sort();
 result.status="EXECUTION_UNAVAILABLE";
 result.blockingReasons=result.requiredEffectKinds.map(kind=>`combat_effect_handler_missing:${kind}`);
 return result;
}

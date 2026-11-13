import { z } from "zod";
import { ActivityError } from "./activity-policy";
import { readCombatAuthority } from "./combat-authority";
const admission=z.object({combatAuthority:z.unknown().optional()});
const snapshot=z.object({admission:admission.optional(),departureResponse:z.object({admission:admission.optional()}).passthrough().optional()}).passthrough();
/** The legacy client finish route cannot verify current five-actor execution, coop lineage or world results.
 * Invoke after original finish receipt replay and account access checks, before any new writes.
 * Absence preserves historical handling; explicit malformed/unsupported contracts never fall back.
 */
export function assertClientFinishAuthority(snapshotJson:string|null){
 if(snapshotJson===null)return;
 let parsed:z.infer<typeof snapshot>;
 try{parsed=snapshot.parse(JSON.parse(snapshotJson));}catch{throw new ActivityError(409,"combat_authority_contract_invalid");}
 const bindings=[readCombatAuthority(parsed.admission?.combatAuthority),readCombatAuthority(parsed.departureResponse?.admission?.combatAuthority)];
 if(bindings.some(b=>b.status==="INVALID"))throw new ActivityError(409,"combat_authority_contract_invalid");
 // Source presence or RUNNING initialization is not a verified terminal combat result.
 const ordinarySources=['ordinaryRoofSource','ordinaryRailSource','ordinaryTrainSource','ordinaryStatSource','ordinaryInitialModifiers','ordinaryKitSource','ordinaryBasicSource','ordinaryRelocationSource','firstBattleEncounterSource'];
 if([parsed,parsed.departureResponse].some(value=>value&&ordinarySources.some(key=>Object.hasOwn(value,key))))
  throw new ActivityError(409,"combat_finish_verification_required");
 if(bindings.some(b=>b.status==="PINNED"&&b.contract.mode!=="ORDINARY_SOLO"))throw new ActivityError(409,"combat_finish_verification_required");
}

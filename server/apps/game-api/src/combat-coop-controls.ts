import { z } from "zod";
import { ActivityError } from "./activity-policy";
import { readHostContext } from "./combat-host-report";
const actionSchema=z.discriminatedUnion("kind",[
 z.strictObject({kind:z.literal("TRAIN_COMBAT")}),
 z.strictObject({kind:z.literal("OPERATOR_COMBAT")}),
 z.strictObject({kind:z.literal("SHARED_PROGRESS")}),
 z.strictObject({kind:z.literal("SHARED_GRANT"),ownerAccountId:z.string().min(1).max(200)}),
 z.strictObject({kind:z.literal("PERSONAL_TRANSACTION"),ownerAccountId:z.string().min(1).max(200)})
]);
/** Actor/source ownership stays pinned to START. This checks run control only, never item ownership,
 * balances, grant eligibility, ad validation, target/skill eligibility, or an action's execution. */
export function requireRunControl(input:ReturnType<typeof readHostContext>,sender:string,action:unknown){
 const c=readHostContext(input),a=actionSchema.parse(action),active=c.activeAccountIds??c.participants.map(p=>p.accountId);
 const participant=c.participants.find(p=>p.accountId===sender);
 if(!participant||!active.includes(sender))throw new ActivityError(403,"combat_control_inactive_participant");
 if(a.kind==="PERSONAL_TRANSACTION"){
  if(a.ownerAccountId!==sender)throw new ActivityError(403,"combat_control_personal_owner_forbidden");
  return;
 }
 if(a.kind==="SHARED_GRANT"&&!c.participants.some(p=>p.accountId===a.ownerAccountId))throw new ActivityError(403,"combat_control_grant_owner_invalid");
 if(active.length===1)return;
 if(a.kind==="SHARED_GRANT"){
  if(sender!==a.ownerAccountId)throw new ActivityError(403,"combat_control_grant_owner_forbidden");
  return;
 }
 // Existing grant owner/revision rules are required for two-player shared choices; no host shortcut.
 if(a.kind==="SHARED_PROGRESS")throw new ActivityError(409,"combat_control_grant_required");
 if((a.kind==="TRAIN_COMBAT"&&participant.role!=="TRAIN")||(a.kind==="OPERATOR_COMBAT"&&participant.role!=="OPERATOR"))throw new ActivityError(403,"combat_control_role_forbidden");
}

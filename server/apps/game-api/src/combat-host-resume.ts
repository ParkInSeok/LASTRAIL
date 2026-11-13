import type {AugmentRestoreJournal} from "./combat-augment-restore";
import { ActivityError } from "./activity-policy";
import { readHostContext } from "./combat-host-report";
/** Stage and verify full checkpoint + confirmed transaction restoration, without resuming combat.
 * Only successful CAS publication authorizes the staged host generation. Missing adapter fails closed.
 */
export interface HostRestoreVerifier {
 /** Prepare only: never resume the host or spend/release resources here.
  * Assertions must fence every checkpoint and transaction source inspected by this adapter
  * until the caller's atomic batch publishes the generation. Use the caller DB and
  * combat_host_write_guard(valid CHECK(valid=1)); no standalone execution authorization.
  * An unconditional guard is only suitable for explicit synthetic test fixtures.
  */
 prepare(input:Readonly<{runId:string;revision:number;requestId:string;startSnapshotJson:string;currentContextJson:string;nextContextJson:string;augmentJournal:AugmentRestoreJournal}>):Promise<{evidence:unknown;assertions:D1PreparedStatement[]}>;
}
export function planHostResume(context:ReturnType<typeof readHostContext>,disconnected:readonly string[],sender:string,mode:"REJOIN"|"SOLO"){
 const active=context.activeAccountIds??context.participants.map(p=>p.accountId);
 // The active list is authoritative: a survivor reconnect never re-adds the departed peer.
 if(!active.includes(sender))throw new ActivityError(403,"combat_host_survivor_invalid");
 if(active.length===1 && mode==="SOLO")throw new ActivityError(409,"combat_host_already_solo");
 if(mode==="SOLO" && (disconnected.includes(sender)||!active.some(id=>id!==sender&&disconnected.includes(id))))throw new ActivityError(409,"combat_host_survivor_invalid");
 if(context.hostGeneration===Number.MAX_SAFE_INTEGER)throw new ActivityError(409,"combat_host_generation_exhausted");
 return {...context,activeAccountIds:mode==="SOLO"?[sender]:[...active].sort(),hostAccountId:mode==="SOLO"?sender:context.hostAccountId,hostGeneration:context.hostGeneration+1};
}

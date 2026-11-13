import {ActivityError} from './activity-policy';
import {createBDepartureCombatSourceOwner} from './combat-runtime-registry';
import {loadPersistedBNativeO22Executors} from './combat-b-native-o22-executor';
import {createPersistedBNativeO22OriginalSourceOwner} from './combat-b-native-o22-original-source';
import {createMeasuredBNativeO22OriginalSourceOwner,type BMeasuredPhysicalAuthoringOwner} from './combat-b-native-o22-physical-composer';
import type {SelectionPrincipal} from './combat-b-selection-store';

/** Server composition only: the authoring owner cannot be supplied by HTTP JSON.
 * Original persisted formation is captured before physical composition, and the
 * registered source must still match the recomposed source and all source CAS.
 * This installs neither authoring data nor the all-reachable-node START gate. */
export async function createAuthenticatedBNativeO22DepartureRuntime(
 db:D1Database,environment:string,accountId:string,previewId:string,auth:SelectionPrincipal,
 physicalAuthoring?:BMeasuredPhysicalAuthoringOwner,
){
 if(auth.accountId!==accountId)throw new ActivityError(403,'combat_b_assignment_account_conflict');
 const original=physicalAuthoring
  ?createMeasuredBNativeO22OriginalSourceOwner(db,auth,physicalAuthoring)
  :createPersistedBNativeO22OriginalSourceOwner(db,auth);
 return createBDepartureCombatSourceOwner(db,environment,
  await loadPersistedBNativeO22Executors(db,environment,accountId,previewId,original));
}

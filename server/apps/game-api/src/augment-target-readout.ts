import {augmentUtilityDefinitionVersion,captureAugmentUtilityDefinitions} from './augment-utility-definition';
import type {SelectionSnapshot} from './solo-augment-selection';

// Card scope is distinct from actual effect recipients (e.g. T05 rear operators).
// Only registered definitions are classified; do not infer scope from arbitrary IDs.
const scopes:Readonly<Record<string,'OPERATOR'|'TRAIN'>>={T05:'TRAIN',T16:'TRAIN',O06:'OPERATOR',O08:'OPERATOR',O22:'OPERATOR',O23:'OPERATOR'};
/** Read-only projection of an authoritative snapshot. No DB, eligibility check,
 * transient HP filtering, or new ownership. Retain this with its grant revision.
 * Resolved names require an authoritative ability/formation mapping not supplied
 * by this grant. null means unknown, never an empty eligible-recipient list.
 */
export async function captureAugmentTargetReadout(snapshot:SelectionSnapshot){
 const catalog=snapshot.catalogVersion===augmentUtilityDefinitionVersion?await captureAugmentUtilityDefinitions():null;
 const slots=snapshot.state==='CONFIRMED'?snapshot.slots.filter(slot=>slot.candidateId===snapshot.selectedCandidateId):snapshot.slots;
 return {version:'augment.target-readout.v1.20261021' as const,runId:snapshot.runId,eventId:snapshot.eventId,
  catalogVersion:snapshot.catalogVersion,grantRevision:snapshot.grantRevision,
  cards:slots.map(slot=>{
   const definition=catalog?.definitions.find(d=>d.id===slot.candidateId)??null;
   return {slotIndex:slot.slotIndex,candidateId:slot.candidateId,scope:definition?scopes[slot.candidateId]??null:null,
    definition,definitionStatus:definition?'AVAILABLE' as const:'SOURCE_NOT_SUPPLIED' as const,
    targets:null,targetResolution:'SOURCE_NOT_SUPPLIED' as const};
  })};
}

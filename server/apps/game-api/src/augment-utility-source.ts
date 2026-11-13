import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import {captureAugmentSelectionSource,readPinnedAugmentSelectionSource,type AugmentSelectionSource} from './combat-augment-selection-source';
import {captureAugmentUtilityDefinitions} from './augment-utility-definition';

export const augmentUtilitySourceVersion='combat.utility-selection-definition.v1.20261019';
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
const fail=(code:string):never=>{throw new ActivityError(409,'utility_selection_'+code);};
/** Internal read-only definition binding from the existing authoritative grant.
 * The generic grant store has no production catalog issuer. A catalog version
 * and selected ID do not prove installed triggers, cumulative owned effects,
 * live profile capabilities or permission to start. No public route/GET added.
 */
export async function captureUtilitySelectionDefinition(db:D1Database,accountId:string,request:unknown){
 const source=await captureAugmentSelectionSource(db,accountId,request);
 return bindUtilitySelectionDefinition(source);
}
/** Internal composition: source must come from the verified grant owner. */
export async function bindUtilitySelectionDefinition(source:AugmentSelectionSource){
 const catalog=await captureAugmentUtilityDefinitions();
 if(source.catalogVersion!==catalog.version)return fail('catalog_unsupported');
 const definitions=new Map(catalog.definitions.map(entry=>[entry.id,entry]));
 if(source.selection.slots.some(slot=>!definitions.has(slot.candidateId)))return fail('definition_missing');
 const selectedId=source.selection.selectedCandidateId;
 const selectedDefinition=selectedId===null?null:definitions.get(selectedId);
 if(selectedDefinition===undefined)return fail('definition_missing');
 const body={version:augmentUtilitySourceVersion,accountId:source.accountId,environment:source.environment,
  runId:source.runId,eventId:source.eventId,startSnapshotHash:source.startSnapshotHash,
  selectionSource:source,definitionCatalog:{version:catalog.version,definitionHash:catalog.definitionHash},selectedDefinition,
  candidateDefinitions:source.selection.state==='SELECTING'?source.selection.slots.map(slot=>definitions.get(slot.candidateId)!):[],
  composition:catalog.composition,lifecycle:catalog.lifecycle,resistance:catalog.resistance,eligibility:catalog.eligibility,
  bindingStatus:selectedDefinition?'CONFIRMED_SELECTION_DEFINITION_ONLY':'SELECTING_NO_EFFECT_BINDING',
  effectOwnershipEstablished:false as const,applied:false as const,runtimeSupported:false as const,canStart:false as const,
  missingBindings:catalog.missingBindings};
 // Return the captured version; a later response must never overwrite a newer
 // storeRevision. This is not an additional read after a successful command.
 return {...body,packetHash:await sha256Hex(json(body))};
}
export type UtilitySelectionDefinition=Awaited<ReturnType<typeof captureUtilitySelectionDefinition>>;
/** The checkpoint owner supplies an independently persisted original and hash.
 * No lookup of today's registry/grant: old runs retain their exact definition.
 * This validates a pin; it does not create the missing combat checkpoint writer.
 */
export async function readPinnedUtilitySelectionDefinition(input:unknown,original:UtilitySelectionDefinition,expectedHash:unknown,trust:unknown):Promise<UtilitySelectionDefinition>{
 const expected=z.string().regex(/^[a-f0-9]{64}$/u).parse(expectedHash),frozen=structuredClone(original);
 let encoded:string;try{encoded=json(input);}catch{return fail('original_conflict');}
 if(encoded.length>2*1024*1024)return fail('capacity');
 const {packetHash,...body}=frozen;
 if(frozen.version!==augmentUtilitySourceVersion||packetHash!==expected||await sha256Hex(json(body))!==expected||encoded!==json(frozen))return fail('original_conflict');
 await readPinnedAugmentSelectionSource(frozen.selectionSource,frozen.selectionSource,frozen.selectionSource.packetHash,trust);
 return frozen;
}

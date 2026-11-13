import type {BRefitStore} from './combat-b-refit-store';
import {buffDepartureSchema} from './combat-b-carriage-buffs';
import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {bPlacementRequestSchema} from './combat-b-placement';
import type {BSelectionStore,SelectionPrincipal} from './combat-b-selection-store';
import {prepareBDepartureCombatSnapshot,readBDepartureCombatPin,type BDepartureCombatSourceOwner} from './combat-b-departure-combat-pin';
export const bDeparturePlacementVersion='combat.b-departure-placement.v1.20261006';
const id=z.string().min(1).max(200),revision=z.number().int().positive().safe(),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const payloadSchema=z.strictObject({version:z.literal(bDeparturePlacementVersion),accountId:id,runId:id,selectionRevision:revision,
 catalog:z.strictObject({version:id,revision,status:z.literal('ISOLATED_LOCAL_FIXTURE')}),selection:bPlacementRequestSchema,refitSnapshot:z.strictObject({revision,definitionHash:hash,sourceHash:hash,refit:buffDepartureSchema}).optional()});
const sealedSchema=payloadSchema.extend({selectionHash:hash});
const json=(value:unknown)=>canonicalJson(value as Parameters<typeof canonicalJson>[0]);
const fail=():never=>{throw new ActivityError(409,'combat_b_departure_placement_conflict');};
/** Fresh candidate only; guards must join the original atomic departure transaction.
 * Pins saved choices and optional original refit instances; no effective stats or runtime admission.
 * LOCAL catalog remains LOCAL. No operating provider or executor is installed here.
 */
export async function prepareBPlacementDeparture(store:BSelectionStore,auth:SelectionPrincipal,runId:string,baseJson:string,owner:BDepartureCombatSourceOwner,refitStore?:BRefitStore){
 const captured=await store.captureForDeparture(auth),refit=refitStore?await refitStore.captureForDeparture(auth):undefined;
 if(refit&&(refit.refit.accountId!==auth.accountId||refit.refit.selectionRevision!==captured.selectionRevision||json(refit.refit.cars.map(({slot,carriageId,occupant})=>({slot,carriageId,occupant})))!==json(captured.selection.placements)))return fail();
 const refitSnapshot=refit?{revision:refit.revision,definitionHash:refit.definitionHash,sourceHash:refit.sourceHash,refit:refit.refit}:undefined;
 const payload=payloadSchema.parse({version:bDeparturePlacementVersion,accountId:auth.accountId,runId,selectionRevision:captured.selectionRevision,catalog:captured.catalog,selection:captured.selection,...(refitSnapshot?{refitSnapshot}:{})});
 const fragment={...payload,selectionHash:await sha256Hex(json(payload))};
 const base:Record<string,unknown>=JSON.parse(baseJson);
 if(!base||typeof base!=='object'||Array.isArray(base)||base.runId!==runId||Object.hasOwn(base,'bCombatPin'))return fail();
 const admission=base.admission,response=base.departureResponse;
 if(!admission||typeof admission!=='object'||Array.isArray(admission)||!response||typeof response!=='object'||Array.isArray(response)||Object.hasOwn(admission,'bDeparturePlacement'))return fail();
 // Preserve legacy admission fields as historical facts; never interpret their slots as B occupants.
 const nextAdmission={...admission,bDeparturePlacement:fragment};
 const nextResponse:Record<string,unknown>={...response};
 if(Object.hasOwn(nextResponse,'admission')){if(json(nextResponse.admission)!==json(admission))return fail();nextResponse.admission=nextAdmission;}
 const prepared=await prepareBDepartureCombatSnapshot(auth.accountId,runId,JSON.stringify({...base,admission:nextAdmission,departureResponse:nextResponse}),owner);
 return {...prepared,atomicAssertions:[...captured.atomicAssertions,...(refit?.atomicAssertions??[]),...prepared.atomicAssertions]};
}
/** Trusted immutable original snapshot only: does not query or backfill current selections. */
export async function readBPlacementDeparture(snapshotJson:string,trust:{accountId:string;runId:string;expectedPinHash?:string}){
 const base=JSON.parse(snapshotJson) as {admission?:{bDeparturePlacement?:unknown}};
 const raw=base?.admission?.bDeparturePlacement;
 const pin=await readBDepartureCombatPin(snapshotJson,trust);
 if(raw===undefined)return null;
 if(!pin)return fail();
 const parsed=sealedSchema.safeParse(raw);if(!parsed.success)return fail();
 const {selectionHash,...payload}=parsed.data;
 if(payload.accountId!==trust.accountId||payload.runId!==trust.runId||selectionHash!==await sha256Hex(json(payload)))return fail();
 return {...parsed.data,origin:pin.origin,pinHash:pin.pinHash,runtimeProfileHash:pin.runtimeProfileHash,sourceHash:pin.sourceHash,
  effectiveCombatStatsIncluded:false as const,runtimeOccupantBindingIncluded:false as const,canStart:false as const,
  missingSources:payload.refitSnapshot?['ORIGINAL_OCCUPANT_EFFECTIVE_PROFILE_BINDING']:['ORIGINAL_PHYSICAL_INSTANCE_ID','ORIGINAL_REFIT_REVISION_AND_RANK','ORIGINAL_OCCUPANT_EFFECTIVE_PROFILE_BINDING']};
}
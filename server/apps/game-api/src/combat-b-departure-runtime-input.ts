import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {formationSelection} from './formation';
import {trainSelection} from './train-loadout';
import {readBDepartureCombatPinForRun} from './combat-b-departure-combat-pin';
export const bDepartureInputVersion='combat.b-departure-runtime-input.v1.20261006';
const id=z.string().min(1).max(200),rev=z.number().int().positive().safe(),nat=z.number().int().nonnegative().safe();
const original=z.object({version:z.literal('departure-v4'),formationRevision:rev,loadoutRevision:rev,formation:formationSelection,loadout:trainSelection,ownedState:z.object({operators:z.array(z.object({operatorId:id,revision:rev,skillTraining:nat,promotion:nat}).passthrough()).length(5),train:z.object({trainId:id,level:rev,revision:rev}).passthrough(),carriages:z.array(z.object({carriageId:id,level:rev,revision:rev}).passthrough()).min(2).max(4)}).passthrough()}).passthrough();
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
/** Original departure producer owns selection. This reader supplies its frozen facts
 * only; five formation slots are not carriage occupant bindings or effective stats. */
export async function readBDepartureRuntimeInput(db:D1Database,accountId:string,runId:string){
 const pin=await readBDepartureCombatPinForRun(db,accountId,runId);const row=await db.prepare('SELECT snapshot_json FROM run_snapshots WHERE run_id=?').bind(runId).first<{snapshot_json:string}>();if(!row||await sha256Hex(row.snapshot_json)!==pin.startSnapshotHash)throw new ActivityError(409,'combat_b_departure_selection_stale');const x={pin:pin.pin,row};if(!x.pin)throw new ActivityError(409,'combat_b_departure_selection_unavailable');
 const parsed=original.safeParse(JSON.parse(x.row.snapshot_json).admission);if(!parsed.success)throw new ActivityError(409,'combat_b_departure_selection_unavailable');
 const a=parsed.data,operators=new Set(a.ownedState.operators.map(v=>v.operatorId)),cars=new Set(a.ownedState.carriages.map(v=>v.carriageId));
 if(operators.size!==5||cars.size!==a.ownedState.carriages.length||a.loadout.trainId!==a.ownedState.train.trainId||a.formation.operators.some(v=>!operators.has(v.operatorId))||a.loadout.carriages.some(v=>!cars.has(v.carriageId)))throw new ActivityError(409,'combat_b_departure_selection_conflict');
 const body={version:bDepartureInputVersion,accountId,runId,originalNativeOrigin:structuredClone(x.pin.origin),nativeContextId:x.pin.source.contextId,departurePinHash:x.pin.pinHash,startSnapshotHash:await sha256Hex(x.row.snapshot_json),originalDeparture:{version:a.version,formationRevision:a.formationRevision,loadoutRevision:a.loadoutRevision,formation:structuredClone(a.formation),loadout:structuredClone(a.loadout),ownedState:structuredClone(a.ownedState)},effectiveCombatStatsIncluded:false,runtimeOccupantBindingIncluded:false,selectionSlotIsCarriageSlot:false,canStart:false,missingSources:['ORIGINAL_EFFECTIVE_COMBAT_PROFILE_AND_CARRIAGE_OCCUPANT_BINDING']};
 const packet={...body,packetHash:await sha256Hex(json(body))},latest=await readBDepartureCombatPinForRun(db,accountId,runId);if(json(latest)!==json(pin))throw new ActivityError(409,'combat_b_departure_selection_stale');return packet;
}

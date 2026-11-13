import { z } from 'zod';
import { parseBPlacementRequest, validateBPlacementV2 } from './combat-b-placement';
import { buffTypeSchema, buffVersion } from './combat-b-carriage-buff-definitions';
import { buffDepartureSchema, type BRefitDeparture } from './combat-b-carriage-buffs';
const id=z.string().min(1).max(128),slot=z.number().int().min(0).max(4),revision=z.number().int().min(1).max(Number.MAX_SAFE_INTEGER-1);
const sourceSchema=z.strictObject({status:z.enum(['ISOLATED_LOCAL_FIXTURE','PERSISTED_ACCOUNT_SOURCE']),
 ranks:z.array(z.strictObject({type:buffTypeSchema,rank:z.number().int().min(1).max(5)})).length(7),
 instances:z.array(z.strictObject({slot,instanceId:id,carriageId:id})).min(1).max(5),
}).refine(s=>new Set(s.ranks.map(r=>r.type)).size===7&&new Set(s.instances.map(i=>i.slot)).size===s.instances.length&&new Set(s.instances.map(i=>i.instanceId)).size===s.instances.length);
export const bRefitSourceCaptureSchema=z.strictObject({revision,selectionRevision:revision,source:sourceSchema,selection:z.unknown(),ownedCarriageIds:z.array(id).max(1000),ownedOperatorIds:z.array(id).max(1000)});
export type BRefitSourceCapture=z.infer<typeof bRefitSourceCaptureSchema>;
export interface BRefitSource { capture(accountId:string):Promise<BRefitSourceCapture>; }
export class UnavailableBRefitSource implements BRefitSource{async capture():Promise<BRefitSourceCapture>{throw new Error('combat_b_refit_source_unavailable');}}
/** Account identity must already be authorized by a caller's internal auth boundary.
 * Single SQLite statement captures selection, ranks, physical instances and ownership atomically.
 * No source defaults or writes; fixture source is explicitly not an operating catalog.
 */
export class LocalBRefitSource implements BRefitSource {
 constructor(private readonly db:D1Database){}
 async capture(accountId:string){
  id.parse(accountId);
  const row=await this.db.prepare(`SELECT s.revision AS saved_revision,s.selection_json,r.revision,r.selection_revision,r.source_json,
   (SELECT json_group_array(carriage_id) FROM account_carriages WHERE account_id=s.account_id) AS cars,
   (SELECT json_group_array(operator_id) FROM account_operators WHERE account_id=s.account_id) AS operators
   FROM combat_b_selections s JOIN combat_b_fixture_refits r ON r.account_id=s.account_id
   JOIN accounts a ON a.id=s.account_id WHERE s.account_id=?`).bind(accountId).first<{saved_revision:number;selection_json:string;revision:number;selection_revision:number;source_json:string;cars:string;operators:string}>();
  if(!row)throw new Error('combat_b_refit_source_unavailable');
  if(row.saved_revision!==row.selection_revision)throw new Error('combat_b_refit_selection_conflict');
  try{if(JSON.parse(row.source_json).status!=='ISOLATED_LOCAL_FIXTURE')throw new Error('status');return bRefitSourceCaptureSchema.parse({revision:row.revision,selectionRevision:row.selection_revision,source:JSON.parse(row.source_json),selection:JSON.parse(row.selection_json),ownedCarriageIds:JSON.parse(row.cars),ownedOperatorIds:JSON.parse(row.operators)});}
  catch{throw new Error('combat_b_refit_source_invalid');}
 }
}
const requestSchema=z.strictObject({version:z.literal(buffVersion),expectedSelectionRevision:revision,expectedRankRevision:revision,
 refits:z.array(z.strictObject({slot,type:buffTypeSchema,rank:z.number().int().min(1).max(5)})).min(1).max(5),
}).refine(s=>new Set(s.refits.map(r=>r.slot)).size===s.refits.length);
/** Read-only validated departure candidate, not START or refit persistence. */
export async function captureBRefitSelection(provider:BRefitSource,accountId:string,input:unknown):Promise<BRefitDeparture>{
 id.parse(accountId);const request=requestSchema.parse(input),source=bRefitSourceCaptureSchema.parse(await provider.capture(accountId));
 if(source.revision!==request.expectedRankRevision)throw new Error('combat_b_rank_revision_conflict');
 if(source.selectionRevision!==request.expectedSelectionRevision)throw new Error('combat_b_refit_selection_conflict');
 const selection=parseBPlacementRequest(source.selection);
 if(source.source.instances.length!==selection.placements.length||request.refits.length!==selection.placements.length)throw new Error('combat_b_refit_slot_mismatch');
 if(selection.placements.some(c=>!source.ownedCarriageIds.includes(c.carriageId)||(c.occupant.kind==='OPERATOR'&&!source.ownedOperatorIds.includes(c.occupant.operatorId))))throw new Error('combat_b_refit_ownership_invalid');
 validateBPlacementV2(selection,{accountId,selectedCarriages:source.source.instances.map(({slot,carriageId})=>({slot,carriageId})),ownedOperatorIds:source.ownedOperatorIds});
 const cars=selection.placements.map(c=>{
  const instance=source.source.instances.find(i=>i.slot===c.slot&&i.carriageId===c.carriageId),refit=request.refits.find(r=>r.slot===c.slot);
  if(!instance||!refit)throw new Error('combat_b_refit_slot_mismatch');
  const shared=source.source.ranks.find(r=>r.type===refit.type)!;
  if(refit.rank!==shared.rank)throw new Error('combat_b_rank_conflict');
  return {...c,instanceId:instance.instanceId,type:refit.type,rank:shared.rank};
 });
 return buffDepartureSchema.parse({version:buffVersion,accountId,selectionRevision:source.selectionRevision,rankRevision:source.revision,sourceStatus:source.source.status,cars});
}
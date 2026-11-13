import {canonicalJson} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {LocalBRefitSource,type BRefitSourceCapture} from './combat-b-refit-source';
export type BoundRefitSource={capture:BRefitSourceCapture;sourceDocument:string;selectionDocument:string};
export interface RefitPersistenceSource {
 capture(db:D1Database,accountId:string):Promise<BoundRefitSource>;
 commitGuard(db:D1Database,accountId:string,source:BoundRefitSource):D1PreparedStatement;
}
export class UnavailableRefitPersistenceSource implements RefitPersistenceSource {
 async capture():Promise<BoundRefitSource>{throw new ActivityError(503,'combat_b_refit_source_unavailable');}
 commitGuard():D1PreparedStatement{throw new ActivityError(503,'combat_b_refit_source_unavailable');}
}
const json=(v:unknown)=>canonicalJson(v as Parameters<typeof canonicalJson>[0]);
/** Existing source parser remains frozen. Capture its raw row around validated intake,
 * then guard exact captured documents in the new transaction (no source writes).
 */
export class LocalRefitPersistenceSource implements RefitPersistenceSource {
 async capture(db:D1Database,accountId:string){
  const raw=()=>db.prepare(`SELECT r.revision,r.selection_revision,r.source_json,s.revision AS saved_revision,s.selection_json
   FROM combat_b_fixture_refits r JOIN combat_b_selections s ON s.account_id=r.account_id WHERE r.account_id=?`).bind(accountId).first<{revision:number;selection_revision:number;source_json:string;saved_revision:number;selection_json:string}>();
  for(let attempt=0;attempt<4;attempt++){
   const before=await raw();if(!before)throw new ActivityError(503,'combat_b_refit_source_unavailable');
   let capture:BRefitSourceCapture;
   try{capture=await new LocalBRefitSource(db).capture(accountId);}catch(e){if(e instanceof Error&&e.message==='combat_b_refit_selection_conflict')throw new ActivityError(409,e.message);throw new ActivityError(503,'combat_b_refit_source_invalid');}
   const after=await raw();if(json(before)!==json(after))continue;
   if(capture.revision!==before.revision||capture.selectionRevision!==before.saved_revision||json(capture.selection)!==json(JSON.parse(before.selection_json))||json(capture.source)!==json(JSON.parse(before.source_json)))continue;
   return {capture,sourceDocument:before.source_json,selectionDocument:before.selection_json};
  }
  throw new ActivityError(409,'combat_b_refit_source_read_conflict');
 }
 commitGuard(db:D1Database,accountId:string,s:BoundRefitSource){
  return db.prepare(`INSERT INTO combat_b_refit_guard(valid) SELECT CASE WHEN EXISTS(
   SELECT 1 FROM combat_b_fixture_refits r JOIN combat_b_selections c ON c.account_id=r.account_id
   WHERE r.account_id=? AND r.revision=? AND r.selection_revision=? AND c.revision=?
   AND json(r.source_json)=json(?) AND json(c.selection_json)=json(?)
   AND (SELECT COUNT(*) FROM account_carriages WHERE account_id=?1)=?
   AND NOT EXISTS(SELECT 1 FROM json_each(?) j LEFT JOIN account_carriages o ON o.account_id=?1 AND o.carriage_id=j.value WHERE o.carriage_id IS NULL)
   AND (SELECT COUNT(*) FROM account_operators WHERE account_id=?1)=?
   AND NOT EXISTS(SELECT 1 FROM json_each(?) j LEFT JOIN account_operators o ON o.account_id=?1 AND o.operator_id=j.value WHERE o.operator_id IS NULL)
   ) THEN 1 ELSE 0 END`).bind(accountId,s.capture.revision,s.capture.selectionRevision,s.capture.selectionRevision,s.sourceDocument,s.selectionDocument,
    s.capture.ownedCarriageIds.length,JSON.stringify(s.capture.ownedCarriageIds),s.capture.ownedOperatorIds.length,JSON.stringify(s.capture.ownedOperatorIds));
 }
}
import {ActivityError} from './activity-policy';
import {bSelectionCatalogSchema,type BSelectionCatalog,type BSelectionCatalogProvider} from './combat-b-selection-catalog';
import {bRefitSourceCaptureSchema} from './combat-b-refit-source';
import type {BoundRefitSource,RefitPersistenceSource} from './combat-b-refit-persistence-source';
export const bPersistedSourceVersion='combat.b-persisted-source.v1.20261006';
/** Read explicit account policy only. Migration never creates eligibility or capacity. */
export class PersistedBSelectionCatalog implements BSelectionCatalogProvider{
 async capture(db:D1Database,accountId:string):Promise<BSelectionCatalog>{
  const row=await db.prepare('SELECT revision,catalog_json FROM combat_b_account_catalog WHERE account_id=?').bind(accountId).first<{revision:number;catalog_json:string}>();
  if(!row)throw new ActivityError(503,'combat_b_catalog_unavailable');
  try{const data=bSelectionCatalogSchema.parse(JSON.parse(row.catalog_json));if(data.status!=='PERSISTED_ACCOUNT_SOURCE'||!Number.isSafeInteger(row.revision)||row.revision<1)throw new Error('source');return {...data,revision:row.revision,sourceDocument:row.catalog_json};}catch{throw new ActivityError(503,'combat_b_catalog_invalid');}
 }
 commitGuard(db:D1Database,accountId:string,c:BSelectionCatalog){return db.prepare('INSERT INTO combat_b_selection_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_b_account_catalog WHERE account_id=? AND revision=? AND catalog_json=?) THEN 1 ELSE 0 END').bind(accountId,c.revision,c.sourceDocument);}
}
export const bPhysicalSourceQuery=`SELECT json_group_array(json_object('instanceId',instance_id,'carriageId',carriage_id,'revision',revision)) AS value FROM (SELECT * FROM combat_b_carriage_instances WHERE account_id=?1 ORDER BY instance_id)`;
const sourceQuery=`SELECT r.revision,r.selection_revision,r.source_json,s.revision AS saved_revision,s.selection_json,
 (SELECT json_group_array(carriage_id) FROM (SELECT carriage_id FROM account_carriages WHERE account_id=s.account_id ORDER BY carriage_id)) AS cars,
 (SELECT json_group_array(operator_id) FROM (SELECT operator_id FROM account_operators WHERE account_id=s.account_id ORDER BY operator_id)) AS operators,
 (SELECT value FROM (${bPhysicalSourceQuery.replaceAll('?1','s.account_id')})) AS physical
 FROM combat_b_account_refit_sources r JOIN combat_b_selections s ON s.account_id=r.account_id WHERE r.account_id=?`;
type Source=BoundRefitSource&{physicalDocument:string};
/** Captures saved choices plus real physical ownership/rank source in one SQL snapshot. */
export class PersistedBRefitSource implements RefitPersistenceSource{
 async capture(db:D1Database,accountId:string):Promise<Source>{
  const r=await db.prepare(sourceQuery).bind(accountId).first<{revision:number;selection_revision:number;source_json:string;saved_revision:number;selection_json:string;cars:string;operators:string;physical:string}>();
  if(!r)throw new ActivityError(503,'combat_b_refit_source_unavailable');
  if(r.selection_revision!==r.saved_revision)throw new ActivityError(409,'combat_b_refit_selection_conflict');
  try{const capture=bRefitSourceCaptureSchema.parse({revision:r.revision,selectionRevision:r.saved_revision,source:JSON.parse(r.source_json),selection:JSON.parse(r.selection_json),ownedCarriageIds:JSON.parse(r.cars),ownedOperatorIds:JSON.parse(r.operators)});
   const physical=JSON.parse(r.physical) as {instanceId:string;carriageId:string;revision:number}[];
   if(capture.source.status!=='PERSISTED_ACCOUNT_SOURCE'||capture.source.instances.some(i=>!physical.some(p=>p.instanceId===i.instanceId&&p.carriageId===i.carriageId&&Number.isSafeInteger(p.revision)&&p.revision>0)))throw new Error('physical');
   return {capture,sourceDocument:r.source_json,selectionDocument:r.selection_json,physicalDocument:r.physical};
  }catch{throw new ActivityError(503,'combat_b_refit_source_invalid');}
 }
 commitGuard(db:D1Database,accountId:string,source:BoundRefitSource){const s=source as Source;if(!s.physicalDocument)throw new ActivityError(503,'combat_b_refit_source_invalid');
  return db.prepare(`INSERT INTO combat_b_refit_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_b_account_refit_sources r JOIN combat_b_selections c ON c.account_id=r.account_id WHERE r.account_id=?1 AND r.revision=?2 AND r.selection_revision=?3 AND c.revision=?3 AND r.source_json=?4 AND c.selection_json=?5)
   AND (SELECT value FROM (${bPhysicalSourceQuery}))=?6
   AND (SELECT COUNT(*) FROM account_carriages WHERE account_id=?1)=?7 AND NOT EXISTS(SELECT 1 FROM json_each(?8) j LEFT JOIN account_carriages o ON o.account_id=?1 AND o.carriage_id=j.value WHERE o.carriage_id IS NULL)
   AND (SELECT COUNT(*) FROM account_operators WHERE account_id=?1)=?9 AND NOT EXISTS(SELECT 1 FROM json_each(?10) j LEFT JOIN account_operators o ON o.account_id=?1 AND o.operator_id=j.value WHERE o.operator_id IS NULL)
   THEN 1 ELSE 0 END`).bind(accountId,s.capture.revision,s.capture.selectionRevision,s.sourceDocument,s.selectionDocument,s.physicalDocument,s.capture.ownedCarriageIds.length,JSON.stringify(s.capture.ownedCarriageIds),s.capture.ownedOperatorIds.length,JSON.stringify(s.capture.ownedOperatorIds));
 }
}
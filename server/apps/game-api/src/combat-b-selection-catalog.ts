import {z} from 'zod';
import {ActivityError} from './activity-policy';
const id=z.string().min(1).max(128);
export const bSelectionCatalogSchema=z.strictObject({version:id,status:z.enum(['ISOLATED_LOCAL_FIXTURE','PERSISTED_ACCOUNT_SOURCE']),
 unlockedSlotCount:z.number().int().min(0).max(5),
 carriages:z.array(z.strictObject({carriageId:id,occupants:z.array(z.enum(['OPERATOR','CARGO'])).min(1).max(2)
  .refine(a=>new Set(a).size===a.length)})).max(1000),
}).refine(c=>new Set(c.carriages.map(x=>x.carriageId)).size===c.carriages.length);
export type BSelectionCatalog = z.infer<typeof bSelectionCatalogSchema> & {revision:number;sourceDocument:string};
export interface BSelectionCatalogProvider {
 capture(db:D1Database,accountId:string):Promise<BSelectionCatalog>;
 commitGuard(db:D1Database,accountId:string,catalog:BSelectionCatalog):D1PreparedStatement;
}
export class UnavailableBSelectionCatalog implements BSelectionCatalogProvider {
 async capture():Promise<BSelectionCatalog>{throw new ActivityError(503,'combat_b_catalog_unavailable');}
 commitGuard():D1PreparedStatement{throw new ActivityError(503,'combat_b_catalog_unavailable');}
}
/** Read explicit trial policy only. No default operating car IDs, unlocks or stats. */
export class LocalFixtureBSelectionCatalog implements BSelectionCatalogProvider {
 async capture(db:D1Database,accountId:string){
  const row=await db.prepare('SELECT revision,catalog_json FROM combat_b_fixture_catalog WHERE account_id=?').bind(accountId).first<{revision:number;catalog_json:string}>();
  if(!row)throw new ActivityError(503,'combat_b_catalog_unavailable');
  try{
   if(JSON.parse(row.catalog_json).status!=='ISOLATED_LOCAL_FIXTURE')throw new Error('status');
   if(!Number.isSafeInteger(row.revision)||row.revision<1)throw new Error('revision');
   return {...bSelectionCatalogSchema.parse(JSON.parse(row.catalog_json)),revision:row.revision,sourceDocument:row.catalog_json};
  }catch{throw new ActivityError(503,'combat_b_catalog_invalid');}
 }
 commitGuard(db:D1Database,accountId:string,catalog:BSelectionCatalog){
  const {revision,sourceDocument}=catalog;
  return db.prepare(`INSERT INTO combat_b_selection_guard(valid) SELECT CASE WHEN EXISTS(
   SELECT 1 FROM combat_b_fixture_catalog WHERE account_id=? AND revision=?
   AND json(catalog_json)=json(?)) THEN 1 ELSE 0 END`).bind(accountId,revision,sourceDocument);
 }
}
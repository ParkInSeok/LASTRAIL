import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {bPlacementRequestSchema,parseBPlacementRequest,validateBPlacementV2} from './combat-b-placement';
import type {BSelectionCatalog,BSelectionCatalogProvider} from './combat-b-selection-catalog';
export class BSelectionAuthError extends Error { readonly status=401; constructor(readonly code:string){super(code);} }
export const selectionVersion='combat.b-selection-http.v1.20261002';
const int=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER-1),id=z.string().min(1).max(128);
const requestSchema=z.strictObject({version:id,expectedRevision:int,catalogVersion:id,
 expectedCatalogRevision:int.refine(n=>n>0),selection:bPlacementRequestSchema});
const responseSchema=z.strictObject({accepted:z.literal(true),version:z.literal(selectionVersion),
 receiptId:id,revision:int,changed:z.boolean(),selection:bPlacementRequestSchema,
 savedCatalog:z.strictObject({version:id,revision:int.refine(n=>n>0)}),startReady:z.literal(false)});
type RequestBody=z.infer<typeof requestSchema>;
export type SelectionPrincipal={accountId:string;sessionId:string;accessExpiresAt:number};
const json=(input:unknown)=>canonicalJson(input as Parameters<typeof canonicalJson>[0]);
const ownedSchema=z.strictObject({carriages:z.array(id),operators:z.array(id)});
type Saved={transport_version:string;selection_json:string;catalog_version:string;catalog_revision:number;revision:number};
export class BSelectionStore {
 constructor(private readonly db:D1Database,private readonly catalog:BSelectionCatalogProvider){}
 parse(input:unknown){
  const p=requestSchema.safeParse(input);
  if(!p.success)throw new ActivityError(400,'combat_b_request_invalid');
  if(p.data.version!==selectionVersion)throw new ActivityError(409,'combat_b_transport_version_conflict');
  return {...p.data,selection:parseBPlacementRequest(p.data.selection)};
 }
 private async principal(auth:SelectionPrincipal){
  const r=await this.db.prepare('SELECT a.status AS account_status,s.status AS session_status,s.expires_at FROM accounts a JOIN auth_sessions s ON s.account_id=a.id WHERE a.id=? AND s.id=?').bind(auth.accountId,auth.sessionId).first<{account_status:string;session_status:string;expires_at:number}>();
  if(!r||r.session_status!=='ACTIVE'||r.expires_at<=Date.now()||auth.accessExpiresAt<=Date.now())throw new BSelectionAuthError('auth_session_invalid');
  if(r.account_status!=='ACTIVE')throw new ActivityError(403,'account_not_active');
 }
 private async ownership(accountId:string){
  const row=await this.db.prepare(`SELECT json_object(
   'carriages',json((SELECT json_group_array(carriage_id) FROM (SELECT carriage_id FROM account_carriages WHERE account_id=?1 ORDER BY carriage_id))),
   'operators',json((SELECT json_group_array(operator_id) FROM (SELECT operator_id FROM account_operators WHERE account_id=?1 ORDER BY operator_id)))
  ) AS value`).bind(accountId).first<{value:string}>();
  try{return ownedSchema.parse(JSON.parse(row?.value??'null'));}catch{throw new ActivityError(503,'combat_b_ownership_source_invalid');}
 }
 private eligible(selection:RequestBody['selection'],accountId:string,source:z.infer<typeof ownedSchema>,catalog:BSelectionCatalog){
  if(selection.placements.some(p=>!source.carriages.includes(p.carriageId)))throw new ActivityError(403,'combat_b_carriage_not_owned');
  if(selection.placements.some(p=>p.slot>=catalog.unlockedSlotCount))throw new ActivityError(409,'combat_b_slot_locked');
  if(selection.placements.some(p=>!catalog.carriages.some(c=>c.carriageId===p.carriageId&&c.occupants.includes(p.occupant.kind))))throw new ActivityError(409,'combat_b_carriage_ineligible');
  return validateBPlacementV2(selection,{accountId,selectedCarriages:selection.placements.map(p=>({slot:p.slot,carriageId:p.carriageId})),ownedOperatorIds:source.operators});
 }
 private async saved(accountId:string){
  const row=await this.db.prepare('SELECT transport_version,selection_json,catalog_version,catalog_revision,revision FROM combat_b_selections WHERE account_id=?').bind(accountId).first<Saved>();
  if(!row)return null;
  try{
   if(row.transport_version!==selectionVersion||!Number.isSafeInteger(row.revision)||row.revision<1||!Number.isSafeInteger(row.catalog_revision)||row.catalog_revision<1||!id.safeParse(row.catalog_version).success)throw new Error('metadata');
   const selection=parseBPlacementRequest(JSON.parse(row.selection_json));
   if(json(selection)!==row.selection_json)throw new Error('canonical');
   validateBPlacementV2(selection,{accountId,selectedCarriages:selection.placements.map(p=>({slot:p.slot,carriageId:p.carriageId})),ownedOperatorIds:selection.placements.flatMap(p=>p.occupant.kind==='OPERATOR'?[p.occupant.operatorId]:[])});
   return {row,selection};
  }catch{throw new ActivityError(503,'combat_b_selection_state_invalid');}
 }
 async read(auth:SelectionPrincipal){
  const capture=async()=>({catalog:await this.catalog.capture(this.db,auth.accountId),
   source:await this.ownership(auth.accountId),saved:await this.saved(auth.accountId)});
  // Optimistic source/revision verification; never present a mixed capture as current.
  for(let attempt=0;attempt<4;attempt++){
   await this.principal(auth);
   const before=await capture(),after=await capture();
   if(json(before)!==json(after))continue;
   const {catalog,source,saved}=after;
   let eligibilityError:string|null=null;
   if(saved){try{this.eligible(saved.selection,auth.accountId,source,catalog);}catch(e){if(e instanceof ActivityError)eligibilityError=e.code;else throw e;}}
   await this.principal(auth);
   return {version:selectionVersion,revision:saved?.row.revision??0,selection:saved?.selection??null,
    savedCatalog:saved?{version:saved.row.catalog_version,revision:saved.row.catalog_revision}:null,
    catalog:{version:catalog.version,status:catalog.status,unlockedSlotCount:catalog.unlockedSlotCount,carriages:catalog.carriages,revision:catalog.revision},ownedSource:source,eligibilityError,startReady:false};
  }
  throw new ActivityError(409,'combat_b_selection_read_conflict');
 }
 async captureForDeparture(auth:SelectionPrincipal){
  const current=await this.read(auth),catalog=await this.catalog.capture(this.db,auth.accountId),saved=await this.saved(auth.accountId);
  if(!current.selection||!saved)throw new ActivityError(409,'combat_b_departure_selection_missing');
  if(current.eligibilityError)throw new ActivityError(current.eligibilityError.includes('not_owned')?403:409,current.eligibilityError);
  if(current.revision!==saved.row.revision||json(current.selection)!==saved.row.selection_json||current.catalog.revision!==catalog.revision||current.catalog.version!==catalog.version||json(current.catalog)!==json((({sourceDocument,...value})=>{void sourceDocument;return value;})(catalog)))throw new ActivityError(409,'combat_b_departure_selection_read_conflict');
  if(saved.row.catalog_version!==catalog.version||saved.row.catalog_revision!==catalog.revision)throw new ActivityError(409,'combat_b_departure_catalog_revision_conflict');
  const placements=json(current.selection.placements);
  const guard=this.db.prepare(`INSERT INTO combat_b_selection_guard(valid) SELECT CASE WHEN
   EXISTS(SELECT 1 FROM auth_sessions s JOIN accounts a ON a.id=s.account_id WHERE s.id=? AND s.account_id=? AND s.status='ACTIVE' AND s.expires_at>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER) AND a.status='ACTIVE')
   AND ?>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)
   AND EXISTS(SELECT 1 FROM combat_b_selections WHERE account_id=? AND transport_version=? AND revision=? AND selection_json=? AND catalog_version=? AND catalog_revision=?)
   AND NOT EXISTS(SELECT 1 FROM json_each(?) j LEFT JOIN account_carriages c ON c.account_id=? AND c.carriage_id=json_extract(j.value,'$.carriageId') WHERE c.carriage_id IS NULL)
   AND NOT EXISTS(SELECT 1 FROM json_each(?) j LEFT JOIN account_operators o ON o.account_id=? AND o.operator_id=json_extract(j.value,'$.occupant.operatorId') WHERE json_extract(j.value,'$.occupant.kind')='OPERATOR' AND o.operator_id IS NULL)
   THEN 1 ELSE 0 END`).bind(auth.sessionId,auth.accountId,auth.accessExpiresAt,auth.accountId,selectionVersion,saved.row.revision,saved.row.selection_json,catalog.version,catalog.revision,placements,auth.accountId,placements,auth.accountId);
  return {selection:current.selection,selectionRevision:current.revision,catalog:{version:catalog.version,revision:catalog.revision,status:catalog.status},
   atomicAssertions:[guard,this.catalog.commitGuard(this.db,auth.accountId,catalog),this.db.prepare('DELETE FROM combat_b_selection_guard')]};
 }
 async save(auth:SelectionPrincipal,input:unknown,key:string){
  const body=this.parse(input),hash=await sha256Hex(json(body)),definition=json(body.selection);
  const replay=async()=>{
   await this.principal(auth);
   const row=await this.db.prepare('SELECT request_hash,response_json FROM combat_b_selection_receipts WHERE account_id=? AND idempotency_key=?').bind(auth.accountId,key).first<{request_hash:string;response_json:string}>();
   if(!row)return null;
   if(row.request_hash!==hash)throw new ActivityError(409,'idempotency_conflict');
   try{
    const response=responseSchema.parse(JSON.parse(row.response_json));
    if(json(response.selection)!==definition||response.savedCatalog.version!==body.catalogVersion||response.savedCatalog.revision!==body.expectedCatalogRevision||response.revision!==body.expectedRevision+(response.changed?1:0))throw new Error('binding');
    await this.principal(auth);
    return response;
   }catch(e){if(e instanceof ActivityError||e instanceof BSelectionAuthError)throw e;throw new ActivityError(503,'combat_b_receipt_invalid');}
  };
  for(let attempt=0;attempt<4;attempt++){
   const original=await replay();if(original)return original;
   const catalog=await this.catalog.capture(this.db,auth.accountId);
   if(catalog.version!==body.catalogVersion||catalog.revision!==body.expectedCatalogRevision)throw new ActivityError(409,'combat_b_catalog_revision_conflict');
   const source=await this.ownership(auth.accountId);
   this.eligible(body.selection,auth.accountId,source,catalog);
   const saved=await this.saved(auth.accountId);
   if((saved?.row.revision??0)!==body.expectedRevision)throw new ActivityError(409,'combat_b_selection_revision_conflict');
   const changed=saved?.row.selection_json!==definition||saved.row.catalog_version!==catalog.version||saved.row.catalog_revision!==catalog.revision;
   const revision=body.expectedRevision+(changed?1:0);
   const response={accepted:true as const,version:selectionVersion,receiptId:crypto.randomUUID(),revision,changed,
    selection:body.selection,savedCatalog:{version:catalog.version,revision:catalog.revision},startReady:false as const};
   const now=Date.now(), db=this.db, placements=json(body.selection.placements);
   const guard=db.prepare(`INSERT INTO combat_b_selection_guard(valid) SELECT CASE WHEN
    EXISTS(SELECT 1 FROM auth_sessions s JOIN accounts a ON a.id=s.account_id WHERE s.id=? AND s.account_id=? AND s.status='ACTIVE' AND s.expires_at>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER) AND a.status='ACTIVE')
    AND ?>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)
    AND COALESCE((SELECT revision FROM combat_b_selections WHERE account_id=?),0)=?
    AND NOT EXISTS(SELECT 1 FROM json_each(?) j LEFT JOIN account_carriages c ON c.account_id=? AND c.carriage_id=json_extract(j.value,'$.carriageId') WHERE c.carriage_id IS NULL)
    AND NOT EXISTS(SELECT 1 FROM json_each(?) j LEFT JOIN account_operators o ON o.account_id=? AND o.operator_id=json_extract(j.value,'$.occupant.operatorId') WHERE json_extract(j.value,'$.occupant.kind')='OPERATOR' AND o.operator_id IS NULL)
    THEN 1 ELSE 0 END`).bind(auth.sessionId,auth.accountId,auth.accessExpiresAt,auth.accountId,body.expectedRevision,placements,auth.accountId,placements,auth.accountId);
   try{
    await db.batch([guard,this.catalog.commitGuard(db,auth.accountId,catalog),
     db.prepare(`INSERT INTO combat_b_selections VALUES(?,?,?,?,?,?,?) ON CONFLICT(account_id) DO UPDATE SET transport_version=excluded.transport_version,selection_json=excluded.selection_json,catalog_version=excluded.catalog_version,catalog_revision=excluded.catalog_revision,revision=excluded.revision,updated_at=excluded.updated_at`).bind(auth.accountId,selectionVersion,definition,catalog.version,catalog.revision,revision,now),
     db.prepare('INSERT INTO combat_b_selection_receipts VALUES(?,?,?,?,?)').bind(auth.accountId,key,hash,json(response),now),
     db.prepare('DELETE FROM combat_b_selection_guard')]);
    return response;
   }catch(e){
    if(/UNIQUE constraint failed|CHECK constraint failed: valid=1/iu.test(String(e))){
     if(attempt<3)continue;
     throw new ActivityError(409,'combat_b_selection_write_conflict');
    }
    throw e;
   }
  }
  throw new ActivityError(409,'combat_b_selection_write_conflict');
 }
}
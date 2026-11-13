import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {BSelectionStore,BSelectionAuthError,type SelectionPrincipal} from './combat-b-selection-store';
import type {BSelectionCatalogProvider} from './combat-b-selection-catalog';
import {buffTypeSchema,buffVersion,captureDefinitions} from './combat-b-carriage-buff-definitions';
import {buffDepartureSchema} from './combat-b-carriage-buffs';
import {captureBRefitSelection} from './combat-b-refit-source';
import type {BoundRefitSource,RefitPersistenceSource} from './combat-b-refit-persistence-source';
export const refitHttpVersion='combat.b-refit-http.v1.20261002';
const json=(v:unknown)=>canonicalJson(v as Parameters<typeof canonicalJson>[0]);
export async function getRefitDefinitionHash(){return sha256Hex(json({version:buffVersion,definitions:captureDefinitions()}));}
const id=z.string().min(1).max(128),revision=z.number().int().min(0).max(Number.MAX_SAFE_INTEGER-1),hashSchema=z.string().regex(/^[a-f0-9]{64}$/u);
const refsSchema=z.array(z.strictObject({slot:z.number().int().min(0).max(4),type:buffTypeSchema,rank:z.number().int().min(1).max(5)})).min(1).max(5).refine(r=>new Set(r.map(c=>c.slot)).size===r.length);
const requestSchema=z.strictObject({version:id,expectedRevision:revision,selectionRevision:revision.min(1),rankRevision:revision.min(1),definitionHash:hashSchema,sourceHash:hashSchema,refits:refsSchema});
const responseSchema=z.strictObject({accepted:z.literal(true),version:z.literal(refitHttpVersion),receiptId:id,revision:revision.min(1),changed:z.boolean(),definitionHash:hashSchema,sourceHash:hashSchema,refit:buffDepartureSchema,startReady:z.literal(false)});
type Body=z.infer<typeof requestSchema>;
type SavedRow={transport_version:string;definition_hash:string;source_hash:string;refit_json:string;revision:number};
const refs=(candidate:z.infer<typeof buffDepartureSchema>)=>candidate.cars.map(({slot,type,rank})=>({slot,type,rank}));
function sourceError(e:unknown):never{
 if(e instanceof ActivityError)throw e;
 if(e instanceof Error&&e.message.startsWith('combat_b_'))throw new ActivityError(e.message.includes('ownership')?403:e.message.includes('source_')?503:409,e.message);
 throw new ActivityError(503,'combat_b_refit_source_invalid');
}
/** Owns only new refit selection+immutable receipts; never mutates B placement or rank source. */
export class BRefitStore {
 constructor(private readonly db:D1Database,private readonly source:RefitPersistenceSource,private readonly catalog:BSelectionCatalogProvider){}
 parse(input:unknown){const r=requestSchema.safeParse(input);if(!r.success)throw new ActivityError(400,'combat_b_refit_request_invalid');if(r.data.version!==refitHttpVersion)throw new ActivityError(409,'combat_b_refit_transport_version_conflict');return {...r.data,refits:r.data.refits.sort((a,b)=>a.slot-b.slot)};}
 private async principal(auth:SelectionPrincipal){
  const r=await this.db.prepare('SELECT a.status AS account_status,s.status AS session_status,s.expires_at FROM accounts a JOIN auth_sessions s ON s.account_id=a.id WHERE a.id=? AND s.id=?').bind(auth.accountId,auth.sessionId).first<{account_status:string;session_status:string;expires_at:number}>();
  if(!r||r.session_status!=='ACTIVE'||r.expires_at<=Date.now()||auth.accessExpiresAt<=Date.now())throw new BSelectionAuthError('auth_session_invalid');
  if(r.account_status!=='ACTIVE')throw new ActivityError(403,'account_not_active');
 }
 private async saved(accountId:string){
  const row=await this.db.prepare('SELECT transport_version,definition_hash,source_hash,refit_json,revision FROM combat_b_refit_selections WHERE account_id=?').bind(accountId).first<SavedRow>();
  if(!row)return null;
  try{const refit=buffDepartureSchema.parse(JSON.parse(row.refit_json));if(row.transport_version!==refitHttpVersion||!revision.min(1).safeParse(row.revision).success||!hashSchema.safeParse(row.definition_hash).success||!hashSchema.safeParse(row.source_hash).success||refit.accountId!==accountId||json(refit)!==row.refit_json)throw new Error('binding');return {row,refit};}
  catch{throw new ActivityError(503,'combat_b_refit_state_invalid');}
 }
 private async capture(auth:SelectionPrincipal){
  const selected=await new BSelectionStore(this.db,this.catalog).read(auth);
  if(!selected.selection)throw new ActivityError(409,'combat_b_refit_selection_missing');
  if(selected.eligibilityError)throw new ActivityError(selected.eligibilityError.includes('not_owned')?403:409,selected.eligibilityError);
  const bound=await this.source.capture(this.db,auth.accountId);
  const catalog=await this.catalog.capture(this.db,auth.accountId);
  if(selected.revision!==bound.capture.selectionRevision||json(selected.selection)!==json(bound.capture.selection)||selected.catalog.version!==catalog.version||selected.catalog.revision!==catalog.revision)throw new ActivityError(409,'combat_b_refit_source_read_conflict');
  return {bound,catalog};
 }
 private async fingerprint(accountId:string,capture:Awaited<ReturnType<BRefitStore['capture']>>){
  const {sourceDocument,...catalog}=capture.catalog;void sourceDocument;return sha256Hex(json({version:refitHttpVersion,accountId,source:capture.bound.capture,catalog}));
 }
 private async candidate(accountId:string,bound:BoundRefitSource,body:Body){
  try{return await captureBRefitSelection({capture:async()=>bound.capture},accountId,{version:buffVersion,expectedSelectionRevision:body.selectionRevision,expectedRankRevision:body.rankRevision,refits:body.refits});}catch(e){sourceError(e);}
 }
 async read(auth:SelectionPrincipal){
  for(let attempt=0;attempt<4;attempt++){
   await this.principal(auth);let first,second;
   try{first={...await this.capture(auth),saved:await this.saved(auth.accountId)};second={...await this.capture(auth),saved:await this.saved(auth.accountId)};}catch(e){if(e instanceof ActivityError&&e.code==='combat_b_refit_source_read_conflict'&&attempt<3)continue;throw e;}
   if(json(first)!==json(second))continue;
   const definitionHash=await getRefitDefinitionHash(),sourceHash=await this.fingerprint(auth.accountId,{bound:second.bound,catalog:second.catalog}),saved=second.saved;
   let eligibilityError:string|null=null;
   if(saved){try{if(saved.row.definition_hash!==definitionHash)throw new ActivityError(409,'combat_b_refit_definition_conflict');const eligible=await this.candidate(auth.accountId,second.bound,{version:refitHttpVersion,expectedRevision:saved.row.revision,selectionRevision:saved.refit.selectionRevision,rankRevision:saved.refit.rankRevision,definitionHash,sourceHash,refits:refs(saved.refit)});if(saved.row.source_hash!==sourceHash||json(eligible)!==json(saved.refit))throw new ActivityError(409,'combat_b_refit_source_changed');}catch(e){if(e instanceof ActivityError)eligibilityError=e.code;else throw e;}}
   await this.principal(auth);
   return {version:refitHttpVersion,buffVersion,definitionHash,sourceHash,definitions:captureDefinitions(),revision:saved?.row.revision??0,refit:saved?.refit??null,savedDefinitionHash:saved?.row.definition_hash??null,savedSourceHash:saved?.row.source_hash??null,
    selectionRevision:second.bound.capture.selectionRevision,rankSource:{revision:second.bound.capture.revision,...second.bound.capture.source},eligibilityError,startReady:false};
  }
  throw new ActivityError(409,'combat_b_refit_read_conflict');
 }
 async captureForDeparture(auth:SelectionPrincipal){
  const current=await this.read(auth),captured=await this.capture(auth),saved=await this.saved(auth.accountId);
  if(!current.refit||!saved)throw new ActivityError(409,'combat_b_departure_refit_missing');
  if(current.eligibilityError)throw new ActivityError(409,current.eligibilityError);
  if(current.revision!==saved.row.revision||json(current.refit)!==saved.row.refit_json||current.sourceHash!==await this.fingerprint(auth.accountId,captured)||current.definitionHash!==await getRefitDefinitionHash())throw new ActivityError(409,'combat_b_departure_refit_read_conflict');
  const verified=await this.candidate(auth.accountId,captured.bound,{version:refitHttpVersion,expectedRevision:current.revision,selectionRevision:current.refit.selectionRevision,rankRevision:current.refit.rankRevision,definitionHash:current.definitionHash,sourceHash:current.sourceHash,refits:refs(current.refit)});
  if(json(verified)!==saved.row.refit_json||saved.row.source_hash!==current.sourceHash||saved.row.definition_hash!==current.definitionHash)throw new ActivityError(409,'combat_b_departure_refit_read_conflict');
  const guard=this.db.prepare(`INSERT INTO combat_b_refit_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_b_refit_selections WHERE account_id=? AND transport_version=? AND revision=? AND definition_hash=? AND source_hash=? AND refit_json=?) THEN 1 ELSE 0 END`).bind(auth.accountId,refitHttpVersion,saved.row.revision,saved.row.definition_hash,saved.row.source_hash,saved.row.refit_json);
  return {revision:current.revision,definitionHash:current.definitionHash,sourceHash:current.sourceHash,refit:current.refit,
   atomicAssertions:[guard,this.source.commitGuard(this.db,auth.accountId,captured.bound),this.catalog.commitGuard(this.db,auth.accountId,captured.catalog),this.db.prepare('DELETE FROM combat_b_refit_guard'),this.db.prepare('DELETE FROM combat_b_selection_guard')]};
 }
 async save(auth:SelectionPrincipal,input:unknown,key:string){
  const body=this.parse(input),requestHash=await sha256Hex(json(body));
  const replay=async()=>{
   await this.principal(auth);
   const old=await this.db.prepare('SELECT request_hash,response_json FROM combat_b_refit_receipts WHERE account_id=? AND idempotency_key=?').bind(auth.accountId,key).first<{request_hash:string;response_json:string}>();
   if(!old)return null;if(old.request_hash!==requestHash)throw new ActivityError(409,'idempotency_conflict');
   try{const response=responseSchema.parse(JSON.parse(old.response_json));if(response.definitionHash!==body.definitionHash||response.sourceHash!==body.sourceHash||response.refit.accountId!==auth.accountId||response.refit.selectionRevision!==body.selectionRevision||response.refit.rankRevision!==body.rankRevision||json(refs(response.refit))!==json(body.refits)||response.revision!==body.expectedRevision+(response.changed?1:0))throw new Error('binding');await this.principal(auth);return response;}
   catch(e){if(e instanceof ActivityError||e instanceof BSelectionAuthError)throw e;throw new ActivityError(503,'combat_b_refit_receipt_invalid');}
  };
  for(let attempt=0;attempt<4;attempt++){
   const original=await replay();if(original)return original;
   if(body.definitionHash!==await getRefitDefinitionHash())throw new ActivityError(409,'combat_b_refit_definition_conflict');
   const captured=await this.capture(auth),{bound,catalog}=captured;
   if(body.sourceHash!==await this.fingerprint(auth.accountId,captured))throw new ActivityError(409,'combat_b_refit_source_hash_conflict');
   const refit=await this.candidate(auth.accountId,bound,body),definition=json(refit),saved=await this.saved(auth.accountId);
   if((saved?.row.revision??0)!==body.expectedRevision){const late=await replay();if(late)return late;throw new ActivityError(409,'combat_b_refit_revision_conflict');}
   const changed=saved?.row.refit_json!==definition||saved.row.definition_hash!==body.definitionHash||saved.row.source_hash!==body.sourceHash;
   const newRevision=body.expectedRevision+(changed?1:0),response={accepted:true as const,version:refitHttpVersion,receiptId:crypto.randomUUID(),revision:newRevision,changed,definitionHash:body.definitionHash,sourceHash:body.sourceHash,refit,startReady:false as const};
   const now=Date.now(),db=this.db,cars=json(refit.cars);
   const guard=db.prepare(`INSERT INTO combat_b_refit_guard(valid) SELECT CASE WHEN
    EXISTS(SELECT 1 FROM auth_sessions s JOIN accounts a ON a.id=s.account_id WHERE s.id=? AND s.account_id=? AND s.status='ACTIVE' AND s.expires_at>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER) AND a.status='ACTIVE')
    AND ?>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER)
    AND COALESCE((SELECT revision FROM combat_b_refit_selections WHERE account_id=?),0)=?
    AND NOT EXISTS(SELECT 1 FROM json_each(?) j LEFT JOIN account_carriages c ON c.account_id=? AND c.carriage_id=json_extract(j.value,'$.carriageId') WHERE c.carriage_id IS NULL)
    AND NOT EXISTS(SELECT 1 FROM json_each(?) j LEFT JOIN account_operators o ON o.account_id=? AND o.operator_id=json_extract(j.value,'$.occupant.operatorId') WHERE json_extract(j.value,'$.occupant.kind')='OPERATOR' AND o.operator_id IS NULL)
    THEN 1 ELSE 0 END`).bind(auth.sessionId,auth.accountId,auth.accessExpiresAt,auth.accountId,body.expectedRevision,cars,auth.accountId,cars,auth.accountId);
   try{
    await db.batch([guard,this.source.commitGuard(db,auth.accountId,bound),this.catalog.commitGuard(db,auth.accountId,catalog),
     db.prepare(`INSERT INTO combat_b_refit_selections VALUES(?,?,?,?,?,?,?) ON CONFLICT(account_id) DO UPDATE SET transport_version=excluded.transport_version,definition_hash=excluded.definition_hash,source_hash=excluded.source_hash,refit_json=excluded.refit_json,revision=excluded.revision,updated_at=excluded.updated_at`).bind(auth.accountId,refitHttpVersion,body.definitionHash,body.sourceHash,definition,newRevision,now),
     db.prepare('INSERT INTO combat_b_refit_receipts VALUES(?,?,?,?,?)').bind(auth.accountId,key,requestHash,json(response),now),db.prepare('DELETE FROM combat_b_refit_guard'),db.prepare('DELETE FROM combat_b_selection_guard')]);return response;
   }catch(e){if(/UNIQUE constraint failed|CHECK constraint failed: valid=1/iu.test(String(e))){if(attempt<3)continue;throw new ActivityError(409,'combat_b_refit_write_conflict');}throw e;}
  }
  throw new ActivityError(409,'combat_b_refit_write_conflict');
 }
}
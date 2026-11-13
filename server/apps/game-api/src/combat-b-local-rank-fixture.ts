import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {BSelectionAuthError,type SelectionPrincipal} from './combat-b-selection-store';
import {BRefitStore} from './combat-b-refit-store';
import {LocalRefitPersistenceSource} from './combat-b-refit-persistence-source';
import {LocalFixtureBSelectionCatalog} from './combat-b-selection-catalog';
import {buffTypeSchema} from './combat-b-carriage-buff-definitions';
export const rankFixtureVersion='combat.b-local-rank-fixture.v1.20261003';
const rank=z.strictObject({type:buffTypeSchema,rank:z.number().int().min(1).max(5)}),revision=z.number().int().min(1).max(Number.MAX_SAFE_INTEGER-1);
const request=z.strictObject({version:z.literal(rankFixtureVersion),expectedRankRevision:revision,expectedSourceHash:z.string().regex(/^[a-f0-9]{64}$/u),changes:z.array(rank).min(1).max(7)}).refine(r=>new Set(r.changes.map(x=>x.type)).size===r.changes.length);
const response=z.strictObject({version:z.literal(rankFixtureVersion),accepted:z.literal(true),accountId:z.string(),rankRevision:revision,changed:z.boolean(),beforeSourceHash:z.string().regex(/^[a-f0-9]{64}$/u),ranks:z.array(rank).length(7)});
const json=(x:unknown)=>canonicalJson(x as Parameters<typeof canonicalJson>[0]);
/** Constructed ONLY by trusted private LOCAL fixture supervisor. Never registered as an HTTP service.
 * This object owns mutation/receipt; consumers receive normal read/refit/packet endpoints only.
 */
export class LocalRankFixtureOwner{
 private readonly source=new LocalRefitPersistenceSource();private readonly catalog=new LocalFixtureBSelectionCatalog();private readonly refit:BRefitStore;
 constructor(private readonly db:D1Database){this.refit=new BRefitStore(db,this.source,this.catalog);}
 private async auth(p:SelectionPrincipal){const r=await this.db.prepare("SELECT a.status account_status,s.status session_status,s.expires_at FROM accounts a JOIN auth_sessions s ON s.account_id=a.id WHERE a.id=? AND s.id=?").bind(p.accountId,p.sessionId).first<{account_status:string;session_status:string;expires_at:number}>();if(!r||r.session_status!=='ACTIVE'||r.expires_at<=Date.now()||p.accessExpiresAt<=Date.now())throw new BSelectionAuthError('auth_session_invalid');if(r.account_status!=='ACTIVE')throw new ActivityError(403,'account_not_active');}
 async transition(p:SelectionPrincipal,input:unknown,key:string){
  const parsed=request.safeParse(input);if(!parsed.success||!['a','b'].includes(p.accountId)||!/^[\x21-\x7e]{1,128}$/u.test(key))throw new ActivityError(400,'combat_b_rank_fixture_request_invalid');
  const body={...parsed.data,changes:[...parsed.data.changes].sort((a,b)=>a.type.localeCompare(b.type))},hash=await sha256Hex(json(body));
  const replay=async()=>{await this.auth(p);const row=await this.db.prepare('SELECT request_hash,response_json FROM combat_b_local_rank_receipts WHERE account_id=? AND command_key=?').bind(p.accountId,key).first<{request_hash:string;response_json:string}>();if(!row)return null;if(row.request_hash!==hash)throw new ActivityError(409,'idempotency_conflict');try{const value=response.parse(JSON.parse(row.response_json));if(value.accountId!==p.accountId||value.beforeSourceHash!==body.expectedSourceHash||value.rankRevision!==body.expectedRankRevision+(value.changed?1:0)||body.changes.some(c=>!value.ranks.some(r=>r.type===c.type&&r.rank===c.rank))||new Set(value.ranks.map(r=>r.type)).size!==7)throw Error('binding');await this.auth(p);return value;}catch(e){if(e instanceof ActivityError||e instanceof BSelectionAuthError)throw e;throw new ActivityError(503,'combat_b_rank_fixture_receipt_invalid');}};
  const original=await replay();if(original)return original;
  const sourceConflict=async()=>{const late=await replay();if(late)return late;throw new ActivityError(409,'combat_b_rank_fixture_source_conflict');};
  const before=await this.refit.read(p),bound=await this.source.capture(this.db,p.accountId),catalog=await this.catalog.capture(this.db,p.accountId),after=await this.refit.read(p);
  if(json(before)!==json(after)||json(bound.capture.source)!==json({status:after.rankSource.status,ranks:after.rankSource.ranks,instances:after.rankSource.instances})||bound.capture.revision!==after.rankSource.revision||bound.capture.selectionRevision!==after.selectionRevision)return sourceConflict();
  if(after.rankSource.revision!==body.expectedRankRevision||after.sourceHash!==body.expectedSourceHash)return sourceConflict();
  const ranks=bound.capture.source.ranks.map(r=>body.changes.find(c=>c.type===r.type)??r),changed=json(ranks)!==json(bound.capture.source.ranks),next=body.expectedRankRevision+(changed?1:0);
  const result=response.parse({version:rankFixtureVersion,accepted:true,accountId:p.accountId,rankRevision:next,changed,beforeSourceHash:before.sourceHash,ranks});
  const guard=this.db.prepare(`INSERT INTO combat_b_local_rank_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM auth_sessions s JOIN accounts a ON a.id=s.account_id WHERE s.id=? AND s.account_id=? AND s.status='ACTIVE' AND s.expires_at>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER) AND a.status='ACTIVE') AND ?>CAST((julianday('now')-2440587.5)*86400000 AS INTEGER) THEN 1 ELSE 0 END`).bind(p.sessionId,p.accountId,p.accessExpiresAt);
  try{await this.db.batch([guard,this.source.commitGuard(this.db,p.accountId,bound),this.catalog.commitGuard(this.db,p.accountId,catalog),this.db.prepare('UPDATE combat_b_fixture_refits SET revision=?,source_json=? WHERE account_id=? AND ?').bind(next,json({...bound.capture.source,ranks}),p.accountId,changed?1:0),this.db.prepare('INSERT INTO combat_b_local_rank_receipts VALUES(?,?,?,?)').bind(p.accountId,key,hash,json(result)),this.db.prepare('DELETE FROM combat_b_local_rank_guard'),this.db.prepare('DELETE FROM combat_b_refit_guard'),this.db.prepare('DELETE FROM combat_b_selection_guard')]);return result;}
  catch(e){const late=await replay();if(late)return late;if(/UNIQUE constraint failed|CHECK constraint failed: valid=1/iu.test(String(e)))throw new ActivityError(409,'combat_b_rank_fixture_source_conflict');throw new ActivityError(503,'combat_b_rank_fixture_storage_unavailable');}
 }
}
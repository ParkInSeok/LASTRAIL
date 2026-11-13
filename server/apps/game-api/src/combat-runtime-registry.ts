import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {bDepartureCombatSourceVersion,type BDepartureCombatSource,type BDepartureCombatContext,type BDepartureCombatSourceOwner} from './combat-b-departure-combat-pin';
import {operationRiskVersion,operationRiskRawSchema} from './combat-b-operation-risk';

const id=z.string().min(1).max(200),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const profile=z.strictObject({id,version:id,executorVersion:id,definition:z.record(z.string(),z.json())});
const sourceSchema=z.strictObject({version:z.literal(bDepartureCombatSourceVersion),binding:z.strictObject({accountId:id,runId:id,operationPreviewId:id,operationId:id,selectedRisk:z.enum(['I','II','III','IV','V']),operationDefinitionVersion:id,operationPlanHash:hash,operationPublicationHash:hash}),contextId:id,profile,
 riskApplicationVersion:z.literal(operationRiskVersion),rawProfiles:z.array(z.strictObject({actorRef:id,hp:operationRiskRawSchema.shape.hp,actions:z.array(operationRiskRawSchema.shape.damage).min(1).max(32).refine(a=>new Set(a.map(x=>x.sourceId)).size===a.length)})).min(1).max(128).refine(a=>new Set(a.map(x=>x.actorRef)).size===a.length)});
const identitySchema=z.strictObject({profileId:id,profileVersion:id,executorVersion:id,profileHash:hash});
const json=(value:unknown)=>canonicalJson(z.json().parse(value));
const digest=(value:unknown)=>sha256Hex(json(value));
const fail=(reason:string):never=>{throw new ActivityError(409,'combat_runtime_'+reason);};
type Row={source_json:string;source_hash:string;profile_hash:string;revision:number;enabled:number};

/** Internal executable binding, supplied by server composition, never request JSON.
 * Implementations must inspect the actual host/profile/raw-source owner and return
 * SAME-transaction CAS statements for every mutable execution/formation dependency.
 * An identifier or a client-supported flag is not an implementation of this contract.
 */
export interface BCombatExecutableAdapter {
 readonly profileId:string; readonly profileVersion:string; readonly executorVersion:string; readonly profileHash:string;
 prepare(source:Readonly<BDepartureCombatSource>,context:Readonly<BDepartureCombatContext>):Promise<readonly D1PreparedStatement[]>;
}

/** Persisted, preview-scoped authoritative source registry. No seeded supported
 * profiles: the current Unity source bundle explicitly has CanStart=false.
 * Registration is INTERNAL ONLY; it does not authenticate a public body.
 */
export class CombatRuntimeRegistry implements BDepartureCombatSourceOwner {
 private readonly adapters:readonly BCombatExecutableAdapter[];
 constructor(private readonly db:D1Database,private readonly environment:string,adapters:readonly BCombatExecutableAdapter[]=[]){
  id.parse(environment);
  this.adapters=adapters.map(adapter=>{const {profileId,profileVersion,executorVersion,profileHash}=adapter;const identity=identitySchema.parse({profileId,profileVersion,executorVersion,profileHash});return Object.freeze({...identity,prepare:adapter.prepare.bind(adapter)});});
  if(new Set(this.adapters.map(a=>a.profileHash)).size!==this.adapters.length)fail('duplicate_executor');
 }
 private adapter(source:BDepartureCombatSource,profileHash:string){
  return this.adapters.find(a=>a.profileId===source.profile.id&&a.profileVersion===source.profile.version&&a.executorVersion===source.profile.executorVersion&&a.profileHash===profileHash)??fail('executor_unavailable');
 }
 /** Authenticated preflight lookup only; this does not assert all reachable node
  * executors, geometry, original START or Player execution are ready. */
 private async assertUnusedCandidate(runId:string){
  if(await this.db.prepare("SELECT 1 FROM runs WHERE id=?").bind(runId).first())return fail('candidate_run_used');
 }
 async inspectPreview(accountId:string,previewId:string){
  id.parse(accountId);id.parse(previewId);
  const row=await this.db.prepare(`SELECT s.source_json,s.source_hash,s.profile_hash FROM combat_runtime_sources s
   JOIN operation_previews p ON p.id=s.preview_id AND p.account_id=s.account_id JOIN accounts a ON a.id=s.account_id
   WHERE s.environment=? AND s.account_id=? AND s.preview_id=? AND s.enabled=1 AND a.status='ACTIVE' AND p.status='ACTIVE'`)
   .bind(this.environment,accountId,previewId).first<{source_json:string;source_hash:string;profile_hash:string}>();
  if(!row)return fail('source_unavailable');
  const source=sourceSchema.parse(JSON.parse(row.source_json));
  if(source.binding.accountId!==accountId||source.binding.operationPreviewId!==previewId||await sha256Hex(row.source_json)!==row.source_hash||await digest(source.profile)!==row.profile_hash)return fail('source_corrupt');
  this.adapter(source,row.profile_hash);
  await this.assertUnusedCandidate(source.binding.runId);
  return {candidateRunId:source.binding.runId,sourceHash:row.source_hash,profileHash:row.profile_hash,profileId:source.profile.id,profileVersion:source.profile.version,executorVersion:source.profile.executorVersion};
 }
 /** Write-once per authenticated preview; retries return the exact original.
  * A changed source requires a fresh original preview, never an in-place rebind. */
 async register(input:{accountId:string;operationPreviewId:string;source:unknown}){
  const accountId=id.parse(input.accountId),previewId=id.parse(input.operationPreviewId),source=sourceSchema.parse(structuredClone(input.source));
  if(source.binding.accountId!==accountId||source.binding.operationPreviewId!==previewId||source.contextId===source.binding.runId)fail('source_conflict');
  const profileHash=await digest(source.profile);this.adapter(source,profileHash);
  const text=json(source),sourceHash=await sha256Hex(text);
  await this.db.batch([
   this.db.prepare(`INSERT INTO combat_runtime_write_guard(valid) SELECT CASE WHEN EXISTS(
    SELECT 1 FROM operation_previews p JOIN accounts a ON a.id=p.account_id
    WHERE p.id=? AND p.account_id=? AND a.status='ACTIVE' AND p.status='ACTIVE' AND p.objective=? AND p.risk=?
   ) AND NOT EXISTS(SELECT 1 FROM runs WHERE id=?) THEN 1 ELSE 0 END`).bind(previewId,accountId,source.binding.operationId,source.binding.selectedRisk,source.binding.runId),
   this.db.prepare(`INSERT INTO combat_runtime_sources(environment,account_id,preview_id,source_json,source_hash,profile_hash,revision,enabled)
    VALUES(?,?,?,?,?,?,1,1) ON CONFLICT(environment,account_id,preview_id) DO NOTHING`).bind(this.environment,accountId,previewId,text,sourceHash,profileHash),
   this.db.prepare(`INSERT INTO combat_runtime_write_guard(valid) SELECT CASE WHEN EXISTS(SELECT 1 FROM combat_runtime_sources
    WHERE environment=? AND account_id=? AND preview_id=? AND source_json=? AND source_hash=? AND profile_hash=? AND enabled=1) THEN 1 ELSE 0 END`)
    .bind(this.environment,accountId,previewId,text,sourceHash,profileHash),
   this.db.prepare('DELETE FROM combat_runtime_write_guard')
  ]);
  return {sourceHash,profileHash,revision:1};
 }
 private async capture(context:Readonly<BDepartureCombatContext>){
  const row=await this.db.prepare(`SELECT s.source_json,s.source_hash,s.profile_hash,s.revision,s.enabled FROM combat_runtime_sources s
   JOIN operation_previews p ON p.id=s.preview_id AND p.account_id=s.account_id JOIN accounts a ON a.id=s.account_id
   WHERE s.environment=? AND s.account_id=? AND s.preview_id=? AND a.status='ACTIVE' AND p.status='ACTIVE'`)
   .bind(this.environment,context.accountId,context.operationPreviewId).first<Row>();
  if(!row||row.enabled!==1) return fail('source_unavailable');
  const source=sourceSchema.parse(JSON.parse(row.source_json));
  if(await sha256Hex(row.source_json)!==row.source_hash||await digest(source.profile)!==row.profile_hash)return fail('source_corrupt');
  const b=source.binding;
  if(b.runId!==context.runId||b.accountId!==context.accountId||b.operationPreviewId!==context.operationPreviewId||b.operationId!==context.operationId||b.selectedRisk!==context.selectedRisk||b.operationDefinitionVersion!==context.definitionVersion||b.operationPlanHash!==context.planHash||b.operationPublicationHash!==context.publicationHash||source.contextId===context.runId)return fail('source_conflict');
  await this.assertUnusedCandidate(context.runId);
  return {row,source,adapter:this.adapter(source,row.profile_hash)};
 }
 async prepare(context:Readonly<BDepartureCombatContext>){
  const {row,source,adapter}=await this.capture(context);
  const fences=Array.from(await adapter.prepare(structuredClone(source),structuredClone(context)));
  if(!fences.length||fences.length>120||fences.some(s=>typeof s?.bind!=='function'))return fail('executor_fence_missing');
  const assertion=this.db.prepare(`INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(
   SELECT 1 FROM combat_runtime_sources s JOIN operation_previews p ON p.id=s.preview_id AND p.account_id=s.account_id JOIN accounts a ON a.id=s.account_id
   WHERE s.environment=? AND s.account_id=? AND s.preview_id=? AND s.source_json=? AND s.source_hash=? AND s.profile_hash=? AND s.revision=? AND s.enabled=1
    AND a.status='ACTIVE' AND (p.status='ACTIVE' OR (p.status='CONSUMED' AND EXISTS(SELECT 1 FROM runs r WHERE r.id=? AND r.preview_id=p.id AND r.account_id=p.account_id)))
  ) THEN 1 ELSE 0 END`).bind(this.environment,context.accountId,context.operationPreviewId,row.source_json,row.source_hash,row.profile_hash,row.revision,context.runId);
  return {source,assertions:[assertion,...fences]};
 }
 async supports(source:Readonly<BDepartureCombatSource>,context:Readonly<BDepartureCombatContext>){
  const captured=await this.capture(context);
  return json(source)===json(captured.source);
 }
}

export function createBDepartureCombatSourceOwner(db:D1Database,environment:string,adapters:readonly BCombatExecutableAdapter[]=[]):CombatRuntimeRegistry {
 return new CombatRuntimeRegistry(db,environment,adapters);
}

import {z} from 'zod';
import {canonicalJson,sha256Hex} from '@lastrail/common';
import {ActivityError} from './activity-policy';
import {readBPhysicalSceneMeasurement,type BMeasuredPhysicalAuthoringOwner} from './combat-b-native-o22-physical-composer';
const id=z.string().min(1).max(200);
const envelopeSchema=z.strictObject({bodyJson:z.string().min(1).max(2000000),signature:z.string().regex(/^[a-f0-9]{64}$/u)});
const bodySchema=z.strictObject({
 version:z.literal('combat.b-physical-publication.v1.20261020'),environment:z.enum(['DEV','PRD']),publicationId:id,keyId:id,
 authoring:z.record(z.string(),z.json()),
 measurements:z.array(z.strictObject({packet:z.json(),geometryCanonicalJson:z.string().max(131072),packetCanonicalJson:z.string().max(131072)})).max(5)
});
const scopeSchema=z.object({context:z.object({accountId:id,runId:id,operationPreviewId:id}).passthrough(),assignmentHash:z.string().regex(/^[a-f0-9]{64}$/u)});
const json=(v:unknown)=>canonicalJson(z.json().parse(v));
const fail=(reason:string):never=>{throw new ActivityError(409,'combat_b_physical_publication_'+reason);};
type Row={key_id:string;body_json:string;body_hash:string;signature:string};
/** Server composition supplies an independently configured HMAC verification key.
 * No client key selection, key provisioning, public publication endpoint, or
 * automatic publication of draft assets. This authenticates source bytes only:
 * MeasuredBNativeO22PhysicalComposer still validates complete execution meaning,
 * actual owned formation/raw values, original rail and exact catalog contract.
 * A new owner must be configured on key rotation; old receipts do not bypass it.
 */
export class BPhysicalPublicationStore implements BMeasuredPhysicalAuthoringOwner{
 private readonly keyId:string;
 private readonly key:CryptoKey;
 constructor(private readonly db:D1Database,private readonly environment:'DEV'|'PRD',trust:{keyId:string;key:CryptoKey},private readonly publicationId:string){
  this.keyId=id.parse(trust.keyId);this.key=trust.key;id.parse(publicationId);z.enum(['DEV','PRD']).parse(environment);
  const algorithm=this.key.algorithm as {name:string;hash?:{name:string}};
  if(this.key.type!=='secret'||algorithm.name!=='HMAC'||algorithm.hash?.name!=='SHA-256'||!this.key.usages.includes('verify'))fail('key_invalid');
 }
 private async verified(input:unknown){
  const e=envelopeSchema.parse(input),bytes=new TextEncoder().encode(e.bodyJson);
  if(bytes.byteLength>524288)return fail('too_large');
  const body=bodySchema.parse(JSON.parse(e.bodyJson));
  if(body.environment!==this.environment||body.publicationId!==this.publicationId||body.keyId!==this.keyId)return fail('trust_conflict');
  const signature=Uint8Array.from(e.signature.match(/../gu)!,v=>parseInt(v,16));
  if(!await crypto.subtle.verify('HMAC',this.key,signature,bytes))return fail('signature_invalid');
  scopeSchema.parse(body.authoring);
  for(const m of body.measurements)await readBPhysicalSceneMeasurement(m);
  return {e,body,hash:await sha256Hex(e.bodyJson)};
 }
 private async row(){
  return this.db.prepare('SELECT key_id,body_json,body_hash,signature FROM b_physical_publications WHERE environment=? AND publication_id=?')
   .bind(this.environment,this.publicationId).first<Row>();
 }
 private async active(){
  if(await this.db.prepare('SELECT 1 FROM b_physical_publication_revocations WHERE environment=? AND publication_id=?')
   .bind(this.environment,this.publicationId).first())return fail('revoked');
 }
 private receipt(hash:string){return {publicationId:this.publicationId,environment:this.environment,keyId:this.keyId,bodyHash:hash,executionApproved:false as const};}
 /** Idempotent immutable ingest. A lost acknowledgement recovers these exact
  * bytes; a different body needs a new publication ID, never an in-place update. */
 async publish(input:unknown){
  const v=await this.verified(input);
  const replay=async()=>{
   const row=await this.row();if(!row)return null;
   await this.active();
   if(row.key_id!==this.keyId||row.body_json!==v.e.bodyJson||row.body_hash!==v.hash||row.signature!==v.e.signature)return fail('publication_conflict');
   return this.receipt(row.body_hash);
  };
  const old=await replay();if(old)return old;
  try{
   await this.db.prepare('INSERT INTO b_physical_publications(environment,publication_id,key_id,body_json,body_hash,signature) VALUES(?,?,?,?,?,?)')
    .bind(this.environment,this.publicationId,this.keyId,v.e.bodyJson,v.hash,v.e.signature).run();
  }catch(error){const recovered=await replay();if(recovered)return recovered;throw error;}
  await this.active();
  return this.receipt(v.hash);
 }
 async capture(context:Parameters<BMeasuredPhysicalAuthoringOwner['capture']>[0],owned:Parameters<BMeasuredPhysicalAuthoringOwner['capture']>[1]){
  const row=await this.row();if(!row)return fail('missing');
  await this.active();
  const v=await this.verified({bodyJson:row.body_json,signature:row.signature});
  if(row.body_hash!==v.hash||row.key_id!==this.keyId)return fail('storage_conflict');
  const scope=scopeSchema.parse(v.body.authoring);
  if(json(scope.context)!==json(context)||scope.assignmentHash!==owned.assignmentHash
   ||owned.accountId!==context.accountId||owned.runId!==context.runId)return fail('scope_conflict');
  // Each authored dependency belongs to the same immutable signed source.
  // Live owned-state guards are independently appended by the persisted owner.
  const fence=()=>[this.db.prepare(`INSERT INTO departure_write_guard(valid) SELECT CASE WHEN EXISTS(
   SELECT 1 FROM b_physical_publications WHERE environment=? AND publication_id=? AND key_id=? AND body_json=? AND body_hash=? AND signature=?)
   AND NOT EXISTS(SELECT 1 FROM b_physical_publication_revocations WHERE environment=? AND publication_id=?) THEN 1 ELSE 0 END`)
   .bind(this.environment,this.publicationId,this.keyId,row.body_json,row.body_hash,row.signature,this.environment,this.publicationId)];
  return {authoring:structuredClone(v.body.authoring),authoringHash:await sha256Hex(json(v.body.authoring)),measurements:structuredClone(v.body.measurements),
   assertions:{profile:fence(),rawSources:fence(),formation:fence(),geometry:fence(),nativeSeed:fence(),execution:fence()}};
 }
 /** Internal publication owner withdrawal; it cannot undo a committed START.
  * Every prepared future START fences this immutable revocation in its batch. */
 async revoke(reason:string){
  id.parse(reason);
  if(!await this.row())return fail('missing');
  await this.db.prepare('INSERT INTO b_physical_publication_revocations(environment,publication_id,reason) VALUES(?,?,?) ON CONFLICT(environment,publication_id) DO NOTHING')
   .bind(this.environment,this.publicationId,reason).run();
  return {publicationId:this.publicationId,revoked:true as const};
 }
}

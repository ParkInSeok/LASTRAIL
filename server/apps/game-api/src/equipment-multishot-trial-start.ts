import {canonicalJson,sha256Hex} from '@lastrail/common';
import {z} from 'zod';
import {ActivityError} from './activity-policy';
import type {MiraBasicSource} from './combat-b-mira-basic-source';
import {captureFormalEquippedMultishot12Source,equippedMultishotLocalProfile,type FormalEquippedMultishot12Source} from './equipment-multishot-equipped-source';
const id=z.string().regex(/^[\x21-\x7e]{1,128}$/u),hash=z.string().regex(/^[a-f0-9]{64}$/u);
const commandSchema=z.strictObject({runId:id,trialId:id,profile:z.literal(equippedMultishotLocalProfile),environment:z.literal('LOCAL'),
 expectedInventoryRevision:z.number().int().positive().safe(),expectedDeparturePinHash:hash,expectedBindingHash:hash.optional()});
const originalSchema=z.strictObject({version:z.literal('equipment.multishot-local-trial-start.v1.20261015'),accountId:id,runId:id,trialId:id,
 profile:z.literal(equippedMultishotLocalProfile),environment:z.literal('LOCAL'),requestHash:hash,originalMiraHash:hash,sourceHash:hash,originalRunSnapshotHash:hash,startHash:hash});
const json=(v:unknown)=>canonicalJson(z.json().parse(v)),digest=(v:unknown)=>sha256Hex(json(v));
function fail(code:string):never{throw new ActivityError(409,'equipment_multishot_trial_'+code);}
function assertion(db:D1Database,sql:string,values:(string|number|null)[]){return db.prepare('INSERT INTO equipment_multishot_trial_write_guard(valid) SELECT CASE WHEN '+sql+' THEN 1 ELSE 0 END').bind(...values);}
type Result=z.infer<typeof originalSchema>&{source:FormalEquippedMultishot12Source};
/** Immutable departure for an explicit isolated LOCAL test. Not a public route,
 * ordinary operating START, inventory grant or backfill of a running run snapshot. */
export class EquippedMultishotTrialStartStore{
 constructor(private readonly db:D1Database){}
 private async owner(accountId:string,runId:string){
  if(!id.safeParse(accountId).success||!id.safeParse(runId).success)throw new ActivityError(400,'equipment_multishot_trial_request_invalid');
  if(!await this.db.prepare('SELECT 1 AS found FROM runs WHERE id=? AND account_id=?').bind(runId,accountId).first())return fail('run_not_found');
 }
 async read(accountId:string,runId:string,trialId:string,expectedStartHash?:string):Promise<Result|null>{
  await this.owner(accountId,runId);if(!id.safeParse(trialId).success||(expectedStartHash!==undefined&&!hash.safeParse(expectedStartHash).success))throw new ActivityError(400,'equipment_multishot_trial_request_invalid');
  const row=await this.db.prepare(`SELECT t.original_json,s.source_json FROM run_equipment_multishot_trial_starts t LEFT JOIN run_equipment_multishot_trial_sources s ON s.run_id=t.run_id AND s.trial_id=t.trial_id WHERE t.run_id=? AND t.trial_id=?`).bind(runId,trialId).first<{original_json:string;source_json:string|null}>();
  if(!row)return null;
  try{
   if(!row.source_json||new TextEncoder().encode(row.source_json).length>1024*1024)return fail('storage_invalid');
   const original=originalSchema.parse(JSON.parse(row.original_json)),source:FormalEquippedMultishot12Source=JSON.parse(row.source_json);
   const {startHash,...body}=original,{packetHash,...sourceBody}=source;
   if(original.accountId!==accountId||original.runId!==runId||original.trialId!==trialId||await digest(body)!==startHash||await digest(sourceBody)!==packetHash||packetHash!==original.sourceHash
    ||source.accountId!==accountId||source.runId!==runId||source.profile!==original.profile||source.environment!=='LOCAL'||source.startBinding!=='IMMUTABLE_LOCAL_TRIAL_START'||!source.staticStatsReady)return fail('storage_invalid');
   if(expectedStartHash!==undefined&&expectedStartHash!==startHash)return fail('original_conflict');
   return structuredClone({...original,source});
  }catch(error){if(error instanceof ActivityError)throw error;return fail('storage_invalid');}
 }
 async start(accountId:string,input:unknown,originalMira:MiraBasicSource,expectedMiraHash:unknown):Promise<Result>{
  const parsed=commandSchema.safeParse(input);if(!parsed.success||!hash.safeParse(expectedMiraHash).success)throw new ActivityError(400,'equipment_multishot_trial_request_invalid');
  const {trialId,...request}=parsed.data,runId=request.runId;
  await this.owner(accountId,runId);
  const requestHash=await digest({command:parsed.data,originalMiraHash:expectedMiraHash});
  const replay=async()=>{const prior=await this.read(accountId,runId,trialId);if(prior&&prior.requestHash!==requestHash)return fail('request_conflict');return prior;};
  const prior=await replay();if(prior)return prior;
  const candidate=await captureFormalEquippedMultishot12Source(this.db,accountId,request,originalMira,expectedMiraHash);
  if(!candidate.staticStatsReady||candidate.blocked.length||candidate.unsupported.length)return fail('source_unsupported');
  const {packetHash,...candidateBody}=candidate;void packetHash;
  const sourceBody={...candidateBody,startBinding:'IMMUTABLE_LOCAL_TRIAL_START'},source={...sourceBody,packetHash:await digest(sourceBody)};
  const run=await this.db.prepare('SELECT snapshot_json FROM run_snapshots WHERE run_id=?').bind(runId).first<{snapshot_json:string}>();
  if(!run||await digest(run.snapshot_json)!==candidate.originalComponent.runSnapshotHash)return fail('state_conflict');
  const body={version:'equipment.multishot-local-trial-start.v1.20261015' as const,accountId,runId,trialId,profile:request.profile,environment:request.environment,
   requestHash,originalMiraHash:originalMira.packetHash,sourceHash:source.packetHash,originalRunSnapshotHash:candidate.originalComponent.runSnapshotHash};
  const original=originalSchema.parse({...body,startHash:await digest(body)});
  const guards=[assertion(this.db,`EXISTS(SELECT 1 FROM runs r JOIN accounts a ON a.id=r.account_id JOIN run_snapshots s ON s.run_id=r.id JOIN run_equipment_inventory i ON i.run_id=r.id WHERE r.id=? AND r.account_id=? AND r.status='ACTIVE' AND a.status='ACTIVE' AND s.snapshot_json=? AND i.account_id=? AND i.revision=?)`,[runId,accountId,run.snapshot_json,accountId,candidate.inventoryRevision])];
  for(const slot of source.slots){if(!slot)continue;const i=slot.instance,effect=slot.effect!;
   guards.push(assertion(this.db,`EXISTS(SELECT 1 FROM run_equipment_instances e JOIN run_equipment_effect_heads h ON h.run_id=e.run_id AND h.instance_id=e.instance_id JOIN run_equipment_effect_history l ON l.run_id=h.run_id AND l.instance_id=h.instance_id AND l.effect_revision=h.revision JOIN run_equipment_effect_originals o ON o.run_id=e.run_id AND o.instance_id=e.instance_id WHERE e.run_id=? AND e.instance_id=? AND e.revision=? AND e.source_item_id=? AND e.definition_id=? AND e.definition_version=? AND e.category_id=? AND e.set_tag=? AND e.star=? AND e.locked=? AND e.location_kind='EQUIPPED' AND e.wearer_kind='operator' AND e.wearer_id='op_mira' AND e.slot_index=? AND h.revision=? AND l.snapshot_json=? AND l.original_hash=? AND o.payload_hash=l.original_hash AND l.payload_hash=? AND l.source_ref=?)`,[runId,i.instance_id,i.revision,i.source_item_id,i.definition_id,i.definition_version,i.category_id,i.set_tag,i.star,i.locked,i.slot_index!,effect.effectRevision,json(effect.snapshot),slot.originalHash,slot.payloadHash,slot.sourceRef]));
  }
  guards.push(assertion(this.db,`(SELECT count(*) FROM run_equipment_instances WHERE run_id=? AND location_kind='EQUIPPED' AND wearer_kind='operator' AND wearer_id='op_mira')=?`,[runId,source.slots.filter(x=>x!==null).length]));
  try{await this.db.batch([...guards,
   this.db.prepare('INSERT INTO run_equipment_multishot_trial_starts(run_id,trial_id,account_id,request_hash,source_hash,original_json) VALUES(?,?,?,?,?,?)').bind(runId,trialId,accountId,requestHash,source.packetHash,json(original)),
   this.db.prepare('INSERT INTO run_equipment_multishot_trial_sources(run_id,trial_id,source_hash,source_json) VALUES(?,?,?,?)').bind(runId,trialId,source.packetHash,json(source)),
   this.db.prepare('DELETE FROM equipment_multishot_trial_write_guard')]);}
  catch(error){const completed=await replay();if(completed)return completed;if(/valid=1|UNIQUE constraint failed/iu.test(String(error)))return fail('state_conflict');throw error;}
  return {...original,source};
 }
}
